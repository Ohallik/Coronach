using System;
using System.IO;
using Newtonsoft.Json;

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
            if(File.Exists(path)) File.Replace(path+".tmp",path,path+".bak");
            else File.Move(path+".tmp",path);
            UnityEngine.Debug.Log($"SAVE_OK slot={slot} zone={state.zone}");
        }
        public GameState Load(string slot)
        {
            var path=PathForSlot(slot);
            try {
                if(!File.Exists(path)) return null;
                var state=JsonConvert.DeserializeObject<GameState>(File.ReadAllText(path));
                return state!=null && state.version==1 && state.party?.Count>0 ? state : null;
            } catch(JsonException) { return null; } catch(IOException) { return null; }
        }
        public bool Exists(string slot) => Load(slot)!=null;
    }
}
