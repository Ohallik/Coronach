using System;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
namespace Lattice.Core
{
    public enum AudioBus { Master,Music,SFX,Ambience,UI }
    [Serializable] public sealed class AudioLevels
    {
        public int version=1;
        public float master=1,music=1,sfx=1,ambience=1,ui=1;
        public float Get(AudioBus bus)=>bus switch{AudioBus.Master=>master,AudioBus.Music=>music,AudioBus.SFX=>sfx,AudioBus.Ambience=>ambience,_=>ui};
        public void Set(AudioBus bus,float value)
        {
            value=float.IsNaN(value)||float.IsInfinity(value)?1:Mathf.Clamp01(value);
            switch(bus){case AudioBus.Master:master=value;break;case AudioBus.Music:music=value;break;case AudioBus.SFX:sfx=value;break;case AudioBus.Ambience:ambience=value;break;default:ui=value;break;}
        }
    }
    // Quality replays use their existing isolated save directory. Merely
    // loading defaults never writes preferences or migrates a human profile.
    public sealed class AudioPreferences
    {
        readonly string file;
        public string FilePath=>file;
        public AudioPreferences(string isolatedDirectory)
        {if(!string.IsNullOrEmpty(isolatedDirectory))file=Path.Combine(Path.GetFullPath(isolatedDirectory),"audio-settings.json");}
        public AudioLevels Load()
        {
            var levels=new AudioLevels();
            if(file!=null)
            {
                try
                {
                    if(File.Exists(file))
                    {
                        var saved=new AudioLevels();JsonUtility.FromJsonOverwrite(File.ReadAllText(file),saved);
                        if(saved.version==1)levels=saved;
                    }
                }
                catch(IOException e){Debug.LogWarning("Audio settings could not be read: "+e.Message);}
                catch(ArgumentException e){Debug.LogWarning("Audio settings were invalid: "+e.Message);}
            }
            else
                foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))
                    levels.Set(bus,PlayerPrefs.GetFloat("audio."+bus,bus==AudioBus.Master?PlayerPrefs.GetFloat("volume",1):1));
            foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))levels.Set(bus,levels.Get(bus));
            return levels;
        }
        public void Save(AudioLevels levels)
        {
            if(file!=null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));File.WriteAllText(file+".tmp",JsonUtility.ToJson(levels,true));
                // Keep the prior settings beside the replacement, matching the
                // save system's supported Windows/Mono replacement path.
                if(File.Exists(file))File.Replace(file+".tmp",file,file+".bak");else File.Move(file+".tmp",file);
                return;
            }
            foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))PlayerPrefs.SetFloat("audio."+bus,levels.Get(bus));
            PlayerPrefs.SetFloat("volume",levels.master);PlayerPrefs.Save();
        }
    }
    public sealed class AudioMix:MonoBehaviour
    {
        public static AudioMix Current{get;private set;}
        AudioMixer mixer;AudioMixerGroup[] groups;AudioLevels levels;AudioPreferences preferences;
        bool ready,dirty;float changedAt,duck=1;bool priorityCue;
        public string SettingsFile=>preferences.FilePath;
        public float Level(AudioBus bus)=>levels.Get(bus);
        public static AudioMixerGroup Group(AudioBus bus)=>Current!=null?Current.groups[(int)bus]:null;
        void Awake()
        {
            Current=this;mixer=Resources.Load<AudioMixer>("Audio/Coronach");
            if(mixer==null)throw new MissingReferenceException("Required Coronach audio mixer is absent");
            groups=new AudioMixerGroup[5];
            foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))
            {
                foreach(var candidate in mixer.FindMatchingGroups(bus.ToString()))if(candidate.name==bus.ToString())groups[(int)bus]=candidate;
                if(groups[(int)bus]==null)throw new MissingReferenceException("Required audio bus is absent: "+bus);
            }
            preferences=new AudioPreferences(DevArgs.Value("-audio-settings-path")??DevArgs.Value("-savepath"));levels=preferences.Load();
        }
        void Start()
        {
            // Unity's mixer requires SetFloat after Awake/OnEnable. Keep the
            // legacy listener neutral; all five saved gains live in the mixer.
            ready=true;AudioListener.volume=1;
            foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))Apply(bus);
        }
        public void SetLevel(AudioBus bus,float value,bool persist=true)
        {
            levels.Set(bus,value);if(ready)Apply(bus);
            if(persist){dirty=true;changedAt=Time.unscaledTime;}
        }
        void Apply(AudioBus bus)
        {
            float level=levels.Get(bus)*(bus==AudioBus.Music?duck:1),db=level<=.0001f?-80:20*Mathf.Log10(level);
            if(!mixer.SetFloat(bus+"Volume",db))Debug.LogError("Mixer gain unavailable: "+bus);
        }
        public void Flush(){if(!dirty)return;preferences.Save(levels);dirty=false;}
        public void SetCueDucking(bool active)=>priorityCue=active;
        void Update()
        {
            float wanted=priorityCue ? .65f : 1,next=Mathf.MoveTowards(duck,wanted,Time.unscaledDeltaTime*(priorityCue?7:1.75f));
            if(next!=duck){duck=next;if(ready)Apply(AudioBus.Music);}
            if(dirty&&Time.unscaledTime-changedAt>=.4f)Flush();
        }
        void OnDestroy(){Flush();if(Current==this)Current=null;}
    }
}
