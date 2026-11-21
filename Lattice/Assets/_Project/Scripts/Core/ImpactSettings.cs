using UnityEngine;
namespace Lattice.Core
{
    /// <summary>How much of a hit reaches the screen, the clock and the hand. They are the player's to turn down: a camera that kicks and a game that holds for a frame
    /// are what make a hit land, and some people can't look at either. Kept with the sound levels' keys in the player's preferences.</summary>
    public static class ImpactSettings
    {
        public enum ShakeLevel{Off,Reduced,Full}
        static bool loaded,enabled=true,hitStop=true,fullFlashes=true,rumble=true;
        static ShakeLevel shake=ShakeLevel.Full;
        /// <summary>A fixture's own settings: nothing is read from this machine's preferences or written to them until <see cref="Reset"/>.</summary>
        public static bool Volatile;
        [System.Serializable] sealed class Saved{public int version=1,shake=(int)ShakeLevel.Full;public bool hitStop=true,fullFlashes=true,rumble=true;}
        /// <summary>An isolated run (a replay, a capture, the test runner) keeps its choices beside its own saves, as the sound levels do, and so starts from the defaults whatever
        /// the person at this machine has chosen for themselves. Loading never writes.</summary>
        static string File
        {
            get{string folder=DevArgs.Value("-audio-settings-path")??DevArgs.Value("-savepath");return string.IsNullOrEmpty(folder)?null:System.IO.Path.Combine(System.IO.Path.GetFullPath(folder),"impact-settings.json");}
        }
        static void Load()
        {
            if(loaded)return;loaded=true;
            if(DevArgs.Has("-no-impact"))enabled=false;
            if(Volatile)return;
            string file=File;
            if(file!=null)
            {
                try
                {
                    if(!System.IO.File.Exists(file))return;
                    var saved=new Saved();JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(file),saved);
                    if(saved.version!=1)return;
                    shake=(ShakeLevel)Mathf.Clamp(saved.shake,0,2);hitStop=saved.hitStop;fullFlashes=saved.fullFlashes;rumble=saved.rumble;
                }
                catch(System.IO.IOException e){Debug.LogWarning("Impact settings could not be read: "+e.Message);}
                catch(System.ArgumentException e){Debug.LogWarning("Impact settings were invalid: "+e.Message);}
                return;
            }
            shake=(ShakeLevel)Mathf.Clamp(PlayerPrefs.GetInt("impact.shake",(int)ShakeLevel.Full),0,2);
            hitStop=PlayerPrefs.GetInt("impact.hitstop",1)!=0;fullFlashes=PlayerPrefs.GetInt("impact.flashes",1)!=0;rumble=PlayerPrefs.GetInt("impact.rumble",1)!=0;
        }
        static void Save()
        {
            if(Volatile)return;
            string file=File;
            if(file!=null)
            {
                try
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
                    System.IO.File.WriteAllText(file,JsonUtility.ToJson(new Saved{shake=(int)shake,hitStop=hitStop,fullFlashes=fullFlashes,rumble=rumble},true));
                }
                catch(System.IO.IOException e){Debug.LogWarning("Impact settings could not be saved: "+e.Message);}
                return;
            }
            PlayerPrefs.SetInt("impact.shake",(int)shake);PlayerPrefs.SetInt("impact.hitstop",hitStop?1:0);
            PlayerPrefs.SetInt("impact.flashes",fullFlashes?1:0);PlayerPrefs.SetInt("impact.rumble",rumble?1:0);PlayerPrefs.Save();
        }
        /// <summary>The whole layer at once: off for -no-impact, a probe, or a fixture measuring something else. It is not a player's setting and is never saved.</summary>
        public static bool Enabled{get{Load();return enabled;}set{Load();enabled=value;}}
        public static ShakeLevel Shake{get{Load();return shake;}set{Load();shake=value;Save();}}
        /// <summary>What every camera kick and tremor is multiplied by.</summary>
        public static float ShakeScale=>Shake==ShakeLevel.Full?1:Shake==ShakeLevel.Reduced?.5f:0;
        public static bool HitStop{get{Load();return hitStop;}set{Load();hitStop=value;Save();}}
        public static bool FullFlashes{get{Load();return fullFlashes;}set{Load();fullFlashes=value;Save();}}
        /// <summary>What a body's flash and the screen's edge are multiplied by.</summary>
        public static float FlashScale=>FullFlashes?1:.4f;
        public static bool Rumble{get{Load();return rumble;}set{Load();rumble=value;Save();}}
        /// <summary>Back to what is saved, read again on the next ask.</summary>
        public static void Reset(){loaded=false;Volatile=false;enabled=true;shake=ShakeLevel.Full;hitStop=fullFlashes=rumble=true;}
    }
}
