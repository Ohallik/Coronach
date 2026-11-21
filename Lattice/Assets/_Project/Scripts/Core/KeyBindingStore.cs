using UnityEngine;
namespace Lattice.Core
{
    /// <summary>Where the keyboard's rebinds are kept. A player's are in their preferences. An isolated run (the test runner, a replay, a capture: anything given
    /// -audio-settings-path or -savepath) keeps them beside its own settings, as the sound levels and the impact settings are, and so starts from the default keys
    /// whatever anyone else has chosen. Editor preferences are one registry key for every copy of the project on this machine: two test mirrors running at once
    /// shared it, and a fixture in one restored the other's half-finished rebind (attack, dodge and guard moved to H, G and Y) for every run after it.</summary>
    public static class KeyBindingStore
    {
        const string Key="keyboardBindings";
        static string File
        {
            get{string folder=DevArgs.Value("-audio-settings-path")??DevArgs.Value("-savepath");return string.IsNullOrEmpty(folder)?null:System.IO.Path.Combine(System.IO.Path.GetFullPath(folder),"keyboard-bindings.json");}
        }
        /// <summary>The saved overrides as InputActionAsset JSON, or empty for the default keys.</summary>
        public static string Load()
        {
            string file=File;if(file==null)return PlayerPrefs.GetString(Key,"");
            try{return System.IO.File.Exists(file)?System.IO.File.ReadAllText(file):"";}
            catch(System.IO.IOException e){Debug.LogWarning("Keyboard bindings could not be read: "+e.Message);return "";}
        }
        /// <summary>Keeps these overrides; empty goes back to the default keys.</summary>
        public static void Save(string json)
        {
            string file=File;
            if(file==null){if(string.IsNullOrEmpty(json))PlayerPrefs.DeleteKey(Key);else PlayerPrefs.SetString(Key,json);return;}
            try
            {
                if(string.IsNullOrEmpty(json)){if(System.IO.File.Exists(file))System.IO.File.Delete(file);return;}
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));System.IO.File.WriteAllText(file,json);
            }
            catch(System.IO.IOException e){Debug.LogWarning("Keyboard bindings could not be saved: "+e.Message);}
        }
    }
}
