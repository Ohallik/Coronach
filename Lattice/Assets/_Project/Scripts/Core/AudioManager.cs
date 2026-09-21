using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Core
{
    public sealed class AudioManager:MonoBehaviour
    {
        static AudioManager instance;
        AudioSource effects,ambient;
        readonly Dictionary<string,AudioClip> clips=new();
        readonly Dictionary<string,float> next=new();
        void Awake()
        {
            instance=this;effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.spatialBlend=0;
            ambient=gameObject.AddComponent<AudioSource>();ambient.playOnAwake=false;ambient.loop=true;ambient.volume=.055f;
        }
        AudioClip Clip(string key){if(!clips.TryGetValue(key,out var clip)){clip=Resources.Load<AudioClip>("Audio/SFX/"+key);clips[key]=clip;}return clip;}
        public static void Play(string key,float volume=.25f)
        {
            if(instance==null)return;if(instance.next.TryGetValue(key,out float until)&&Time.unscaledTime<until)return;instance.next[key]=Time.unscaledTime+.06f;
            var clip=instance.Clip(key);if(clip!=null)instance.effects.PlayOneShot(clip,volume);
        }
        public static void Zone(bool flight)
        {
            if(instance==null)return;var clip=instance.Clip(flight?"spaceEngineLow_000":"computerNoise_000");
            if(instance.ambient.clip==clip&&instance.ambient.isPlaying)return;instance.ambient.clip=clip;if(clip!=null)instance.ambient.Play();
        }
        void OnDestroy(){if(instance==this)instance=null;}
    }
}
