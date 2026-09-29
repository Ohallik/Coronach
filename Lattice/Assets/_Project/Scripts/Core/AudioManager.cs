using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Lattice.Core
{
    public sealed class AudioManager:MonoBehaviour
    {
        internal sealed class Voice
        {
            public AudioSource source;public Transform emitter;public bool bound,active,paused,loop;
            public int token;public float started;public AudioBus bus;
        }
        readonly struct CueKey:IEquatable<CueKey>
        {
            readonly int emitter;readonly string family;
            public CueKey(Transform owner,string key){emitter=owner!=null?owner.GetInstanceID():0;family=key;}
            public bool Equals(CueKey other)=>emitter==other.emitter&&family==other.family;
            public override bool Equals(object other)=>other is CueKey key&&Equals(key);
            public override int GetHashCode()=>HashCode.Combine(emitter,family);
        }
        sealed class Variation { public float next;public int previous=-1;public Transform owner;public bool bound; }
        public sealed class Loop:IDisposable
        {
            readonly Voice voice;readonly int token;
            internal Loop(Voice voice){this.voice=voice;token=voice.token;}
            public bool Active=>voice!=null&&voice.source!=null&&voice.active&&voice.token==token;
            public void Set(float gain,float pitch=1)
            {if(Active){voice.source.volume=Mathf.Clamp01(gain);voice.source.pitch=Mathf.Clamp(pitch,.5f,2);}}
            public void Dispose(){if(Active)Release(voice);}
        }
        public static AudioManager Current{get;private set;}
        public const int VoiceLimit=24;
        public int VoiceCount=>voices.Count;
        public int ActiveVoices{get{int count=0;foreach(var v in voices)if(v.active)count++;return count;}}
        public static event Action<string,Transform> CueStarted;
        readonly List<Voice> voices=new();readonly Dictionary<string,AudioClip> clips=new();
        readonly Dictionary<CueKey,Variation> variations=new();
        AudioSource ambient;float pruneAt;readonly List<CueKey> expired=new();
        void Awake()
        {
            Current=this;ambient=gameObject.AddComponent<AudioSource>();ambient.playOnAwake=false;ambient.loop=true;ambient.volume=.055f;
            ambient.outputAudioMixerGroup=AudioMix.Group(AudioBus.Ambience);ambient.priority=160;
            GameTime.PauseChanged+=Pause;SceneManager.sceneUnloaded+=Unloaded;
            foreach(var clip in Resources.LoadAll<AudioClip>("Audio/Palette")){clip.LoadAudioData();clips[clip.name]=clip;}
            foreach(var clip in Resources.LoadAll<AudioClip>("Audio/SFX")){clip.LoadAudioData();clips[clip.name]=clip;}
        }
        AudioClip Clip(string key)
        {
            if(clips.TryGetValue(key,out var clip))return clip;
            clip=Resources.Load<AudioClip>("Audio/Palette/"+key)??Resources.Load<AudioClip>("Audio/SFX/"+key);clips[key]=clip;
            if(clip==null)Debug.LogWarning("AUDIO_CUE_MISSING "+key);return clip;
        }
        Variation State(Transform emitter,string family)
        {var key=new CueKey(emitter,family);if(!variations.TryGetValue(key,out var state)){state=new Variation{owner=emitter,bound=emitter!=null};variations[key]=state;}return state;}
        public static void Play(string key,float volume=.25f)=>PlayAt(key,null,Vector3.zero,volume);
        public static void PlayUi(string key,float volume=.4f)=>PlayAt(key,null,Vector3.zero,volume,AudioBus.UI,16,.035f);
        public static void PlayAt(string key,Transform emitter,Vector3 point,float volume=.4f,AudioBus bus=AudioBus.SFX,int priority=128,float interval=.06f,float pitch=1)
        {
            if(Current==null||GameTime.Paused&&bus!=AudioBus.UI)return;
            var state=Current.State(emitter,key);if(Time.unscaledTime<state.next)return;state.next=Time.unscaledTime+interval;
            Current.Emit(key,emitter,point,volume,pitch,bus,priority,false);
        }
        public static void Family(string key,int count,Transform emitter,Vector3 point,float volume=.4f,int priority=128,float interval=.06f)
        {
            if(Current==null||GameTime.Paused||count<1)return;
            var state=Current.State(emitter,key);if(Time.unscaledTime<state.next)return;state.next=Time.unscaledTime+interval;
            int choice=count==1?0:UnityEngine.Random.Range(0,state.previous<0?count:count-1);
            if(state.previous>=0&&count>1&&choice>=state.previous)choice++;state.previous=choice;
            Current.Emit(key+"_"+choice,emitter,point,volume,UnityEngine.Random.Range(.97f,1.03f),AudioBus.SFX,priority,false);
        }
        public static Loop StartLoop(string key,Transform emitter,float volume=0,AudioBus bus=AudioBus.SFX,int priority=150)
        {
            if(Current==null||emitter==null)return null;
            var voice=Current.Emit(key,emitter,emitter.position,volume,1,bus,priority,true);
            return voice!=null?new Loop(voice):null;
        }
        Voice Emit(string key,Transform emitter,Vector3 point,float volume,float pitch,AudioBus bus,int priority,bool loop)
        {
            var clip=Clip(key);if(clip==null)return null;
            Voice voice=null;
            foreach(var candidate in voices)if(!candidate.active){voice=candidate;break;}
            if(voice==null&&voices.Count<VoiceLimit)
            {
                var go=new GameObject("Pooled sound");go.transform.SetParent(transform);var source=go.AddComponent<AudioSource>();source.playOnAwake=false;
                voice=new Voice{source=source};voices.Add(voice);
            }
            if(voice==null)
            {
                foreach(var candidate in voices)
                    if(voice==null||candidate.source.priority>voice.source.priority||candidate.source.priority==voice.source.priority&&candidate.started<voice.started)voice=candidate;
                if(voice.source.priority<priority)return null;
            }
            Release(voice);voice.token++;voice.active=true;voice.emitter=emitter;voice.bound=emitter!=null;voice.loop=loop;voice.bus=bus;voice.started=Time.unscaledTime;
            var s=voice.source;s.transform.position=point;s.clip=clip;s.loop=loop;s.volume=Mathf.Clamp01(volume);s.pitch=pitch;s.priority=priority;
            s.outputAudioMixerGroup=AudioMix.Group(bus);s.spatialBlend=emitter!=null?.35f:0;s.minDistance=8;s.maxDistance=80;s.rolloffMode=AudioRolloffMode.Logarithmic;
            s.dopplerLevel=0;s.ignoreListenerPause=bus==AudioBus.UI;s.Play();
            if(GameTime.Paused&&bus!=AudioBus.UI){s.Pause();voice.paused=true;}
            CueStarted?.Invoke(key,emitter);return voice;
        }
        static void Release(Voice voice)
        {if(voice.source!=null){voice.source.Stop();voice.source.clip=null;}voice.active=voice.paused=voice.loop=voice.bound=false;voice.emitter=null;}
        void Update()
        {
            bool priorityCue=false;
            foreach(var voice in voices)
            {
                if(!voice.active)continue;
                if(voice.bound&&voice.emitter==null||!voice.paused&&!voice.source.isPlaying){Release(voice);continue;}
                if(voice.loop&&voice.bound)voice.source.transform.position=voice.emitter.position;
                if(!voice.paused&&voice.bus==AudioBus.SFX&&voice.source.priority<=48&&voice.source.volume>.005f&&AudioMix.Current.Level(AudioBus.SFX)>.005f)priorityCue=true;
            }
            if(AudioMix.Current!=null)AudioMix.Current.SetCueDucking(priorityCue);
            if(Time.unscaledTime>=pruneAt)
            {
                pruneAt=Time.unscaledTime+5;expired.Clear();
                foreach(var pair in variations)if(pair.Value.bound&&pair.Value.owner==null)expired.Add(pair.Key);
                foreach(var key in expired)variations.Remove(key);
            }
        }
        void Pause(bool paused)
        {
            foreach(var voice in voices)if(voice.active&&voice.bus!=AudioBus.UI)
            {if(paused){voice.source.Pause();voice.paused=true;}else if(voice.paused){voice.source.UnPause();voice.paused=false;}}
        }
        void Unloaded(Scene _)
        {foreach(var voice in voices)Release(voice);variations.Clear();if(ambient!=null){ambient.Stop();ambient.clip=null;}if(AudioMix.Current!=null)AudioMix.Current.SetCueDucking(false);}
        public static void Zone(bool flight)
        {
            if(Current==null)return;var clip=Current.Clip(flight?"spaceEngineLow_000":"computerNoise_000");
            if(Current.ambient.clip==clip&&Current.ambient.isPlaying)return;Current.ambient.clip=clip;if(clip!=null)Current.ambient.Play();
        }
        void OnDestroy()
        {GameTime.PauseChanged-=Pause;SceneManager.sceneUnloaded-=Unloaded;foreach(var voice in voices)Release(voice);if(AudioMix.Current!=null)AudioMix.Current.SetCueDucking(false);if(Current==this)Current=null;}
    }
}
