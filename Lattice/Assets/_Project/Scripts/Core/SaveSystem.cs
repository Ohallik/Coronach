using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lattice.Core
{
    /// <summary>Ported slot interface and JSON round-trip; atomic replace, restricted slot names.</summary>
    public sealed class SaveSystem
    {
        readonly string directory;
        public SaveSystem(string directory) { this.directory=directory; }
        /// <summary>The save a defeat returns to: the autosave, or a slot just loaded from the title until the next real
        /// autosave takes over. Without this, a defeat in an old slot would jump to the other campaign's autosave.</summary>
        public string RetrySlot {get;private set;}="autosave";
        public void ResumeFrom(string slot){SlotFile(slot);RetrySlot=slot;}
        /// <summary>A save was written, for the "Saved" mark.</summary>
        public static event Action<string> Written;
        // ---------- writing behind the frame ----------
        static bool? writeBehind;
        /// <summary>Whether the disk work of a save (the file, its flush through to the disk and the replace) runs on a worker while the game goes on. On the frame
        /// that saved, that work cost 5 to 25 ms: a checkpoint at a boss door, a conversation's autosave and a manual save each dropped a frame to 40 ms in the
        /// final chain (QA-034). What's written is made on the frame that saves, so a later change can't leak into it, and writes keep their order.
        /// It's on in a player. The editor's suites read and delete save files around every save, so there it stays on the frame unless a check turns it on.</summary>
        public static bool WriteBehind{get=>writeBehind??=!UnityEngine.Application.isEditor;set=>writeBehind=value;}
        static readonly object queue=new();
        static Task last=Task.CompletedTask;
        static int waiting;
        static bool quitHooked;
        /// <summary>Saves still on their way to the disk.</summary>
        public static int Pending=>Volatile.Read(ref waiting);
        /// <summary>Waits until every queued save is on the disk. Anything that reads a save file waits for this first, and so does quitting.</summary>
        public static void Drain(){Task tail;lock(queue)tail=last;tail.Wait();}
        public string PathForSlot(string slot)
        {
            // Whoever asks where a slot's file is is about to read it: a save still queued for it reaches the disk first.
            var path=SlotFile(slot);Drain();return path;
        }
        string SlotFile(string slot)
        {
            if(slot!="autosave" && slot!="slot1" && slot!="slot2" && slot!="slot3")
                throw new ArgumentException("Unknown save slot",nameof(slot));
            return Path.Combine(directory,slot+".json");
        }
        public void Save(string slot,GameState state)
        {
            var path=SlotFile(slot);
            // A save with nobody standing would make every retry an instant defeat (QA: the conversation that outlived a wipe).
            if(slot=="autosave"&&state.party!=null&&state.party.Count>0&&state.party.TrueForAll(m=>m.integrity<=0))
            {UnityEngine.Debug.Log($"SAVE_REFUSED party down zone={state.zone}");return;}
            state.savedAtUtc=DateTime.UtcNow.ToString("o");
            string json=JsonConvert.SerializeObject(state,Formatting.Indented),zone=state.zone,folder=directory;
            if(!WriteBehind)
            {
                Write(folder,path,json);
                if(slot=="autosave")RetrySlot="autosave";
                UnityEngine.Debug.Log($"SAVE_OK slot={slot} zone={zone}");
                Written?.Invoke(slot);
                return;
            }
            lock(queue)
            {
                if(!quitHooked){quitHooked=true;UnityEngine.Application.quitting+=Drain;}
                Interlocked.Increment(ref waiting);
                last=last.ContinueWith(_=>
                {
                    // The log is the disk's word, so it waits for the disk; a failure is reported and the next save tries again.
                    try{Write(folder,path,json);UnityEngine.Debug.Log($"SAVE_OK slot={slot} zone={zone}");}
                    catch(Exception error){UnityEngine.Debug.LogError($"SAVE_FAILED slot={slot} zone={zone} {error.GetType().Name}: {error.Message}");}
                    finally{Interlocked.Decrement(ref waiting);}
                },CancellationToken.None,TaskContinuationOptions.None,TaskScheduler.Default);
            }
            if(slot=="autosave")RetrySlot="autosave";
            Written?.Invoke(slot);
        }
        static void Write(string folder,string path,string json)
        {
            Directory.CreateDirectory(folder);
            // Through to the disk before the replace: a power cut must not leave an empty save over a good one.
            using(var stream=new FileStream(path+".tmp",FileMode.Create,FileAccess.Write,FileShare.None))
            {
                using var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false),4096,true);
                writer.Write(json);writer.Flush();stream.Flush(true);
            }
            if(File.Exists(path)) ReplaceWithBriefRetry(path);
            else File.Move(path+".tmp",path);
        }
        static void ReplaceWithBriefRetry(string path)
        {
            int lastError=0;
            for(int retry=0;;retry++)
            {
                try
                {
                    File.Replace(path+".tmp",path,path+".bak");
                    if(retry>0)UnityEngine.Debug.Log($"SAVE_REPLACE_RECOVERED retries={retry} error=0x{lastError:X8}");
                    return;
                }
                catch(IOException error) when(retry<7&&
                    (error.HResult==unchecked((int)0x80070020)||error.HResult==unchecked((int)0x80070497)))
                {
                    // Windows sharing denial / held backup. Both retain the original
                    // names. Never delete a save to make replacement succeed; other
                    // errors (including partially moved files) must propagate.
                    lastError=error.HResult;
                    Thread.Sleep(1<<retry); // 127 ms requested sleep total; OS scheduling adds latency.
                }
            }
        }
        public GameState Load(string slot)
        {
            var path=PathForSlot(slot);
            // Every save keeps the previous one beside it; a damaged or missing file falls back to that copy.
            foreach(var candidate in new[]{path,path+".bak",path+".tmp"})
            {
                var state=Read(candidate);if(state==null)continue;
                if(candidate!=path)UnityEngine.Debug.Log("SAVE_RECOVERED "+slot+" from "+Path.GetFileName(candidate));
                ChapterProgress.Repair(state);return state;
            }
            return null;
        }
        /// <summary>The slot has a file but nothing in it can be read: "Damaged", not "Empty".</summary>
        public bool Damaged(string slot)=>File.Exists(PathForSlot(slot))&&Load(slot)==null;
        /// <summary>Damaged, for a slot whose Load has just come back empty: the title's rows read each save once, not twice.</summary>
        public bool DamagedAfterEmptyLoad(string slot)=>File.Exists(PathForSlot(slot));
        static GameState Read(string path)
        {
            try {
                if(!File.Exists(path)) return null;
                // Keep serialized timestamp strings byte-for-byte as values;
                // JObject's default date parsing would rewrite their formatting.
                using var reader=new JsonTextReader(new StringReader(File.ReadAllText(path))){DateParseHandling=DateParseHandling.None};
                var document=JObject.Load(reader);
                if(document["version"]?.Type!=JTokenType.Integer)return null;
                var state=document.ToObject<GameState>(JsonSerializer.Create(new JsonSerializerSettings{ObjectCreationHandling=ObjectCreationHandling.Replace}));
                return ChapterProgress.Migrate(state)?state:null;
            } catch(JsonException) { return null; } catch(IOException) { return null; } catch(UnauthorizedAccessException) { return null; }
        }
        public bool Exists(string slot) => Load(slot)!=null;
    }
}
