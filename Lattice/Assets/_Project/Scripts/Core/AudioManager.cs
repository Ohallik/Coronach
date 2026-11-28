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
            public AudioSource source;public Transform emitter;public bool bound,active,paused,loop,through;
            public int token;public float started;public AudioBus bus;public string key;public Vector3 point;
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
            {if(Active){voice.source.volume=CueLevels.LoopVolume(voice.key,gain);voice.source.pitch=Mathf.Clamp(pitch,.5f,2);}}
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
            Current=this;ambient=gameObject.AddComponent<AudioSource>();ambient.playOnAwake=false;ambient.loop=true;ambient.volume=HumVolume;
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
        /// <summary>A short sample of a bus for its volume slider. It plays through a pause, which holds back every other sound on these
        /// buses, because the pause menu is where most players are on that page. A dragged slider asks for it every frame: four a second is enough.</summary>
        public static void Sample(string key,AudioBus bus,float volume=.35f)
        {
            if(Current==null)return;
            var state=Current.State(null,"sample/"+bus);if(Time.unscaledTime<state.next)return;state.next=Time.unscaledTime+.25f;
            var voice=Current.Emit(key,null,Vector3.zero,volume,1,bus,64,false);
            if(voice!=null&&voice.paused){voice.source.UnPause();voice.paused=false;}
        }
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
            Release(voice);voice.token++;voice.active=true;voice.emitter=emitter;voice.bound=emitter!=null;voice.loop=loop;voice.bus=bus;voice.started=Time.unscaledTime;voice.key=key;
            // The call's volume is a trim against its family's nominal; the family's level is CueLevels'.
            voice.point=point;var s=voice.source;s.transform.position=emitter!=null?Heard(point):point;s.clip=clip;s.loop=loop;s.volume=loop?CueLevels.LoopVolume(key,volume):CueLevels.Volume(key,volume);s.pitch=pitch;s.priority=priority;
            s.outputAudioMixerGroup=AudioMix.Group(bus);s.spatialBlend=emitter!=null?.35f:0;s.minDistance=8;s.maxDistance=80;s.rolloffMode=AudioRolloffMode.Logarithmic;
            s.dopplerLevel=0;s.ignoreListenerPause=bus==AudioBus.UI;s.Play();
            if(GameTime.Paused&&bus!=AudioBus.UI){s.Pause();voice.paused=true;}
            CueStarted?.Invoke(key,emitter);return voice;
        }
        static void Release(Voice voice)
        {if(voice.source!=null){voice.source.Stop();voice.source.clip=null;}voice.active=voice.paused=voice.loop=voice.bound=voice.through=false;voice.emitter=null;}
        void Update()
        {
            bool priorityCue=false;
            foreach(var voice in voices)
            {
                if(!voice.active)continue;
                if(voice.bound&&voice.emitter==null||!voice.paused&&!voice.source.isPlaying){Release(voice);continue;}
                if(voice.loop&&voice.bound)voice.source.transform.position=Heard(voice.emitter.position);
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
            foreach(var voice in voices)if(voice.active&&voice.bus!=AudioBus.UI&&!voice.through)
            {if(paused){voice.source.Pause();voice.paused=true;}else if(voice.paused){voice.source.UnPause();voice.paused=false;}}
        }
        void Unloaded(Scene _)
        {
            foreach(var voice in voices)if(!(voice.through&&voice.active))Release(voice);
            variations.Clear();placed.Clear();if(ambient!=null){ambient.Stop();ambient.clip=null;}if(AudioMix.Current!=null)AudioMix.Current.SetCueDucking(false);
        }
        const float HumVolume=CueLevels.OldHumVolume;
        /// <summary>A place with its own air plays that. A place with none plays a quiet room tone on foot and the ship's drive in flight,
        /// shaped noise with nothing pitched or repeating in it (CueLevels.RoomTone). Until round ten it was Kenney's computerNoise_000, five
        /// seconds of beeps and bloops on a loop under every deck and station, which Nathan heard as a constant beeping that makes no sense
        /// for someone wandering there; that clip is now only ever a machine's one-shot.</summary>
        public static void Zone(bool flight,AudioClip bed=null,float bedVolume=HumVolume)
        {
            if(Current==null)return;var clip=bed!=null?bed:CueLevels.AsRoundNine?Current.Clip(flight?"spaceEngineLow_000":"computerNoise_000"):Current.Bed(flight);
            Current.ambient.volume=bed!=null?CueLevels.BedVolume(bed,bedVolume):CueLevels.AsRoundNine?HumVolume:flight?CueLevels.FlightDroneVolume:CueLevels.RoomToneVolume;
            Current.TrimPlaced();
            if(Current.ambient.clip==clip&&Current.ambient.isPlaying)return;Current.ambient.clip=clip;if(clip!=null)Current.ambient.Play();
        }
        /// <summary>Round eleven: the beds a builder placed in a scene (a looping source on the ambience bus that plays on its own: the Many
        /// Hands forge's hammering and its herd) come down with the zone's bed, by CueLevels.PlacedVolume, at the volume their builder gave
        /// them, when the zone starts; one not yet come aboard (inactive) is trimmed too. Nothing is rebuilt.</summary>
        readonly Dictionary<AudioSource,float> placed=new();
        void TrimPlaced()
        {
            foreach(var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(source==null||source==ambient||source.transform.IsChildOf(transform)||!source.loop||!source.playOnAwake)continue;
                if(source.outputAudioMixerGroup==null||source.outputAudioMixerGroup.name!="Ambience")continue;
                if(!placed.TryGetValue(source,out float made)){made=source.volume;placed[source]=made;}
                source.volume=CueLevels.PlacedVolume(source.clip,made);
            }
        }
        /// <summary>A cue that outlives the zone's load (a warp's crossing): flat, on the effects bus, not cut when the scene it was started in
        /// unloads, and not held by a pause.</summary>
        public static void PlayThrough(string key,float volume=.3f)
        {
            if(Current==null||CueLevels.CuesAsRoundTen)return;
            var voice=Current.Emit(key,null,Vector3.zero,volume,1,AudioBus.SFX,32,false);if(voice==null)return;
            voice.through=true;if(voice.paused){voice.source.UnPause();voice.paused=false;}
        }
        /// <summary>The zone's bed at another level, its clip left playing where it is (a swap cuts it and starts it again): the anchor chamber's steel eased down once its load is gone.</summary>
        public static void BedLevel(float volume){if(Current!=null&&Current.ambient!=null)Current.ambient.volume=CueLevels.BedVolume(Current.ambient.clip,volume);}
        public static float BedVolume=>Current!=null&&Current.ambient!=null?Current.ambient.volume:0;
        readonly AudioClip[] beds=new AudioClip[2];
        AudioClip Bed(bool flight)
        {
            int i=flight?1:0;if(beds[i]!=null)return beds[i];
            var path="Audio/Beds/"+(flight?CueLevels.FlightDrone:CueLevels.RoomTone);beds[i]=Resources.Load<AudioClip>(path);
            if(beds[i]==null)Debug.LogWarning("AUDIO_BED_MISSING "+path);return beds[i];
        }
        /// <summary>Where the game listens from: the party's lead (the camera rig's target), turned as the lens is turned so left on the
        /// screen is left in the ear. Until round ten every voice was heard from the lens, 17 to 45 m from the fight and further in flight, so
        /// each sound's spatial part fell away as the camera pulled back for a boss. The AudioListener stays on the lens (the quality capture
        /// records the mix there, on the camera's own object), and each of these voices is placed where the lens hears it as the lead would:
        /// its point moved by the lens's offset from the lead's head. The party's own blows sound the same at any framing, and a foe's or a
        /// machine's fall away with their distance from the party. Sources placed in scenes by builders are heard from the lens as before.
        /// <see cref="CueLevels.AsRoundNine"/> hears everything from the lens again.</summary>
        CameraRig rig;Camera lens;Vector3 shift;
        /// <summary>Where the game's ears are in the world: the lead's head, or the lens where there is no lead.</summary>
        public Vector3 Ears=>lens!=null?lens.transform.position-shift:shift;
        Vector3 Heard(Vector3 point)=>point+shift;
        void LateUpdate()
        {
            var main=Camera.main;
            if(main!=lens)
            {
                lens=main;rig=lens!=null?lens.GetComponent<CameraRig>():null;
                // The sum of every voice is held under full scale where it is heard (D460).
                var ears=lens!=null?lens.GetComponent<AudioListener>():null;if(ears==null)ears=FindFirstObjectByType<AudioListener>();
                if(ears!=null)MasterLimiter.On(ears.gameObject);
            }
            if(lens!=null&&rig==null)rig=FindFirstObjectByType<CameraRig>();
            var target=rig!=null&&rig.target!=null&&rig.target.gameObject.activeInHierarchy?rig.target:null;
            shift=lens==null||target==null||CueLevels.AsRoundNine?Vector3.zero:lens.transform.position-(target.position+Vector3.up*1.2f);
            foreach(var voice in voices)
                if(voice.active&&voice.source.spatialBlend>0)voice.source.transform.position=Heard(voice.loop&&voice.bound&&voice.emitter!=null?voice.emitter.position:voice.point);
        }
        void OnDestroy()
        {GameTime.PauseChanged-=Pause;SceneManager.sceneUnloaded-=Unloaded;foreach(var voice in voices)Release(voice);if(AudioMix.Current!=null)AudioMix.Current.SetCueDucking(false);if(Current==this)Current=null;}
    }
}
