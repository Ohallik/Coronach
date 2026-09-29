using System.Collections;
using Lattice.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class AudioPaletteTests
    {
        [UnityTest] public IEnumerator ImportedPaletteRetainsPreparedHeadroom()
        {
            var clips = Resources.LoadAll<AudioClip>("Audio/Palette");
            Assert.AreEqual(32, clips.Length);
            foreach (var clip in clips)
            {
                clip.LoadAudioData();
                float deadline = Time.realtimeSinceStartup + 5;
                while (clip.loadState == AudioDataLoadState.Loading && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.AreEqual(AudioDataLoadState.Loaded, clip.loadState, clip.name);
                Assert.AreEqual(48000, clip.frequency, clip.name);
                Assert.AreEqual(1, clip.channels, clip.name);
                var samples = new float[clip.samples * clip.channels];
                Assert.IsTrue(clip.GetData(samples, 0), clip.name);
                float peak = 0;
                foreach (float sample in samples) peak = Mathf.Max(peak, Mathf.Abs(sample));
                Debug.Log("AUDIO_IMPORTED_PEAK " + clip.name + " " + peak.ToString("R"));
                Assert.That(peak, Is.InRange(.30f, .318f), clip.name + " prepared -10 dBFS peak changed on import");
            }
        }
    }
}
