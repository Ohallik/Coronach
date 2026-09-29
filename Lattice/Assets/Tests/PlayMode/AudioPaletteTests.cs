using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
            // Every staged clip is accounted for by a committed provenance record,
            // and every recorded clip is staged; the original set keeps its 32.
            var expected = new HashSet<string>();
            foreach (var file in Directory.GetFiles(Path.Combine(Application.dataPath, "_Project/Audio/Palettes"), "*-candidate.json"))
            {
                var keys = Regex.Matches(File.ReadAllText(file), "\"key\":\\s*\"([a-z0-9_]+)\"").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
                if (Path.GetFileName(file) == "movement-combat-ui-candidate.json") Assert.AreEqual(32, keys.Count);
                foreach (var key in keys) Assert.IsTrue(expected.Add(key), "sound key staged by two sets: " + key);
            }
            CollectionAssert.AreEquivalent(expected, clips.Select(c => c.name));
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
