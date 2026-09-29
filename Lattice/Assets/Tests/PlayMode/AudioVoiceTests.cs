using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Lattice.Tests.PlayMode
{
    public sealed class AudioVoiceTests
    {
        GameObject emitter,second;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            emitter=new GameObject("Audio owner");second=new GameObject("Other audio owner");
            foreach(AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))AudioMix.Current.SetLevel(bus,1,false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Paused=false;Object.Destroy(emitter);Object.Destroy(second);SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        [UnityTest] public IEnumerator PriorityStealsWithinTheBoundedPoolWithoutStaleLeaseCleanup()
        {
            var leases=new List<AudioManager.Loop>();
            for(int i=0;i<24;i++)leases.Add(AudioManager.StartLoop("spaceEngineLow_000",emitter.transform,.03f,AudioBus.SFX,200));
            Assert.AreEqual(24,AudioManager.Current.ActiveVoices);Assert.AreEqual(24,AudioManager.Current.VoiceCount);
            var important=AudioManager.StartLoop("spaceEngineLow_000",emitter.transform,.03f,AudioBus.SFX,16);
            Assert.IsNotNull(important);Assert.IsTrue(important.Active);Assert.IsFalse(leases[0].Active,"oldest low-priority voice was not replaced");
            leases[0].Dispose();Assert.IsTrue(important.Active,"stale owner stopped a reused sound");
            Assert.AreEqual(24,AudioManager.Current.VoiceCount);Assert.AreEqual(24,AudioManager.Current.ActiveVoices);
            foreach(var loop in leases)loop.Dispose();important.Dispose();yield return null;
            Assert.AreEqual(0,AudioManager.Current.ActiveVoices);
        }
        [UnityTest] public IEnumerator PerEmitterCooldownKeepsOtherCharactersAudibleAndVariationsDoNotRepeat()
        {
            var started=new List<string>();System.Action<string,Transform> onCue=(key,_)=>started.Add(key);AudioManager.CueStarted+=onCue;
            try
            {
                AudioManager.Family("hit_armor",3,emitter.transform,Vector3.zero,.2f,128,.1f);
                AudioManager.Family("hit_armor",3,emitter.transform,Vector3.zero,.2f,128,.1f);
                AudioManager.Family("hit_armor",3,second.transform,Vector3.zero,.2f,128,.1f);
                Assert.AreEqual(2,started.Count,"cooldown should suppress only the repeated cue on the same owner");
                yield return new WaitForSecondsRealtime(.12f);started.Clear();
                for(int i=0;i<15;i++)AudioManager.Family("hit_armor",3,emitter.transform,Vector3.zero,.2f,128,0);
                Assert.AreEqual(15,started.Count);
                for(int i=1;i<started.Count;i++)Assert.AreNotEqual(started[i-1],started[i],"immediate repeated impact variation");
                Assert.LessOrEqual(AudioManager.Current.VoiceCount,24);
            }
            finally{AudioManager.CueStarted-=onCue;}
        }
        [UnityTest] public IEnumerator WorldLoopsFollowOwnersAndPauseWithoutConsumingNewWorldCues()
        {
            var loop=AudioManager.StartLoop("spaceEngineLow_000",emitter.transform,.1f);
            emitter.transform.position=new Vector3(7,2,-4);yield return null;
            var source=AudioManager.Current.GetComponentsInChildren<AudioSource>().Single(s=>s.loop&&s.clip!=null&&s.outputAudioMixerGroup.name=="SFX");
            Assert.AreEqual(emitter.transform.position,source.transform.position);Assert.AreEqual(.35f,source.spatialBlend);
            GameTime.Paused=true;yield return new WaitForSecondsRealtime(.2f);Assert.IsTrue(loop.Active);Assert.IsFalse(source.isPlaying);
            int count=AudioManager.Current.ActiveVoices;AudioManager.PlayAt("impactMetal_000",emitter.transform,emitter.transform.position);Assert.AreEqual(count,AudioManager.Current.ActiveVoices);
            AudioManager.PlayUi("ui_confirm");
            Assert.IsTrue(AudioManager.Current.GetComponentsInChildren<AudioSource>().Any(s=>s.clip!=null&&s.outputAudioMixerGroup.name=="UI"&&s.ignoreListenerPause));
            GameTime.Paused=false;yield return null;Assert.IsTrue(source.isPlaying);
            Object.Destroy(emitter);yield return null;yield return null;Assert.IsFalse(loop.Active,"destroyed emitter left a sound loop running");
        }
        [UnityTest] public IEnumerator ReturningToTitleReleasesWorldLoopsAndRoomTone()
        {
            SceneFlow.Current.LoadZone("Hub_Decks");while(SceneFlow.Current.Loading)yield return null;
            if(emitter!=null)Object.Destroy(emitter);emitter=new GameObject("Station audio owner");
            var loop=AudioManager.StartLoop("spaceEngineLow_000",emitter.transform,.1f);
            Assert.IsTrue(AudioManager.Current.GetComponents<AudioSource>().Any(s=>s.outputAudioMixerGroup.name=="Ambience"&&s.clip!=null));
            SceneFlow.Current.LoadZone("Title");while(SceneFlow.Current.Loading)yield return null;
            Assert.IsFalse(loop.Active);Assert.AreEqual(0,AudioManager.Current.ActiveVoices);
            Assert.IsFalse(AudioManager.Current.GetComponents<AudioSource>().Any(s=>s.outputAudioMixerGroup.name=="Ambience"&&s.isPlaying));
        }
        [UnityTest] public IEnumerator PriorityCueDuckingRestoresSavedMusicGainOnMutePauseAndRelease()
        {
            AudioMix.Current.SetLevel(AudioBus.Music,.6f,false);var mixer=AudioMix.Group(AudioBus.Music).audioMixer;
            var loop=AudioManager.StartLoop("spaceEngineLow_000",emitter.transform,.1f,AudioBus.SFX,32);
            yield return new WaitForSecondsRealtime(.3f);Assert.IsTrue(mixer.GetFloat("MusicVolume",out float db));
            Assert.AreEqual(20*Mathf.Log10(.6f*.65f),db,.05f);Assert.AreEqual(.6f,AudioMix.Current.Level(AudioBus.Music));
            AudioMix.Current.SetLevel(AudioBus.SFX,0,false);yield return new WaitForSecondsRealtime(.4f);mixer.GetFloat("MusicVolume",out db);Assert.AreEqual(20*Mathf.Log10(.6f),db,.05f,"inaudible effects still duck music");
            AudioMix.Current.SetLevel(AudioBus.SFX,1,false);yield return new WaitForSecondsRealtime(.3f);
            GameTime.Paused=true;yield return new WaitForSecondsRealtime(.4f);mixer.GetFloat("MusicVolume",out db);Assert.AreEqual(20*Mathf.Log10(.6f),db,.05f);
            GameTime.Paused=false;loop.Dispose();yield return new WaitForSecondsRealtime(.4f);mixer.GetFloat("MusicVolume",out db);Assert.AreEqual(20*Mathf.Log10(.6f),db,.05f);
        }
    }
}
