using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lattice.Core
{
    /// <summary>Ported slot interface and JSON round-trip; atomic replace, restricted slot names.</summary>
    public sealed class SaveSystem
    {
        readonly string directory;
        public SaveSystem(string directory) { this.directory=directory; }
        public string PathForSlot(string slot)
        {
            if(slot!="autosave" && slot!="slot1" && slot!="slot2" && slot!="slot3")
                throw new ArgumentException("Unknown save slot",nameof(slot));
            return Path.Combine(directory,slot+".json");
        }
        public void Save(string slot,GameState state)
        {
            var path=PathForSlot(slot); Directory.CreateDirectory(directory);
            state.savedAtUtc=DateTime.UtcNow.ToString("o");
            File.WriteAllText(path+".tmp",JsonConvert.SerializeObject(state,Formatting.Indented));
            if(File.Exists(path)) ReplaceWithBriefRetry(path);
            else File.Move(path+".tmp",path);
            UnityEngine.Debug.Log($"SAVE_OK slot={slot} zone={state.zone}");
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
            try {
                if(!File.Exists(path)) return null;
                // Keep serialized timestamp strings byte-for-byte as values;
                // JObject's default date parsing would rewrite their formatting.
                using var reader=new JsonTextReader(new StringReader(File.ReadAllText(path))){DateParseHandling=DateParseHandling.None};
                var document=JObject.Load(reader);
                if(document["version"]?.Type!=JTokenType.Integer)return null;
                var state=document.ToObject<GameState>(JsonSerializer.Create(new JsonSerializerSettings{ObjectCreationHandling=ObjectCreationHandling.Replace}));
                return ChapterProgress.Migrate(state)?state:null;
            } catch(JsonException) { return null; } catch(IOException) { return null; }
        }
        public bool Exists(string slot) => Load(slot)!=null;
    }
}
