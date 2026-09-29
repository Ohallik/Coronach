using System.Collections;
using System.Linq;
using Lattice.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Lattice.Tests.PlayMode
{
    public sealed class AudioRoutingTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);}
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Paused=false;SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        [UnityTest] public IEnumerator MusicAndAmbienceHaveIndependentMixerGains()
        {
            MusicDirector.SetLocation("Hub_Decks",false);AudioManager.Zone(false);yield return null;
            var music=MusicDirector.Current.ActiveSource;
            Assert.IsNotNull(music.outputAudioMixerGroup,"music bypasses its volume control");
            Assert.AreEqual("Music",music.outputAudioMixerGroup.name);
            var ambient=GameServices.Current.GetComponents<AudioSource>().Single(s=>s.clip!=null&&s.clip.name=="computerNoise_000");
            Assert.IsNotNull(ambient.outputAudioMixerGroup,"room tone bypasses its volume control");
            Assert.AreEqual("Ambience",ambient.outputAudioMixerGroup.name);
            Assert.AreSame(music.outputAudioMixerGroup.audioMixer,ambient.outputAudioMixerGroup.audioMixer);
            foreach(string bus in new[]{"Master","Music","SFX","Ambience","UI"})
                Assert.IsTrue(music.outputAudioMixerGroup.audioMixer.GetFloat(bus+"Volume",out _),"missing saved gain: "+bus);
        }
        [UnityTest] public IEnumerator OrdinaryEffectsAreRoutedToTheirOwnBus()
        {
            AudioManager.Play("impactMetal_000");yield return null;
            var voices=GameServices.Current.GetComponentsInChildren<AudioSource>().Where(s=>!s.loop&&s.clip!=null).ToArray();
            Assert.IsNotEmpty(voices,"ordinary effect has no owned playback voice");
            foreach(var voice in voices)
            {Assert.IsNotNull(voice.outputAudioMixerGroup,"ordinary effect bypasses its volume control");Assert.AreEqual("SFX",voice.outputAudioMixerGroup.name);}
        }
    }
}
