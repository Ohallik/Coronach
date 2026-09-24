using System.Collections.Generic;
using UnityEngine;

namespace Lattice.Core
{
    /// <summary>Persistent two-source music playback. Shops temporarily override the location cue.</summary>
    public sealed class MusicDirector : MonoBehaviour
    {
        public static MusicDirector Current { get; private set; }
        public string Track { get; private set; }
        public AudioSource ActiveSource => sources[target];
        readonly AudioSource[] sources = new AudioSource[2];
        readonly Dictionary<string, AudioClip> clips = new();
        readonly Dictionary<string, int> positions = new();
        readonly HashSet<Object> shops = new();
        string location, locationTrack;
        int target;
        const float Level = .52f, FadeSeconds = 1.1f;

        void Awake()
        {
            Current = this;
            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i] = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0;
                source.volume = 0;
                source.priority = 32;
            }
        }

        public static void SetLocation(string zone, bool combat)
        {
            if (Current == null) return;
            if (Current.location != zone) Current.shops.Clear();
            Current.location = zone;
            Current.locationTrack = zone == "Title" ? "Title Theme" :
                (zone == "Sorrel_Ridges" || zone == "Arena_Ground") && combat ? "Adventure Awaits" :
                zone == "Hub_Decks" || zone == "Hub_CinderHalo" || zone == "TallowDrift" ||
                zone == "TallowApproach" || zone == "Sorrel_Ridges" ? "Hub Town Groove" : null;
            Current.Refresh();
        }

        public static void Shop(Object owner, bool open)
        {
            if (Current == null || owner == null) return;
            if (open) Current.shops.Add(owner); else Current.shops.Remove(owner);
            Current.Refresh();
        }

        void Refresh()
        {
            shops.RemoveWhere(owner => owner == null);
            string wanted = shops.Count > 0 ? "Moonbase Market" : locationTrack;
            if (Track == wanted) return;
            Track = wanted;
            if (wanted == null) return;
            if (!clips.TryGetValue(wanted, out var clip))
            {
                clip = Resources.Load<AudioClip>("Audio/Music/" + wanted);
                clips[wanted] = clip;
            }
            if (clip == null) { Debug.LogError("MUSIC_MISSING " + wanted); return; }
            int existing = System.Array.FindIndex(sources, source => source.clip == clip);
            target = existing >= 0 ? existing : sources[0].volume <= sources[1].volume ? 0 : 1;
            var incoming = sources[target];
            if (incoming.clip != clip)
            {
                Remember(incoming);
                incoming.Stop();
                incoming.clip = clip;
                incoming.volume = 0;
            }
            if (!incoming.isPlaying)
            {
                incoming.timeSamples = positions.TryGetValue(wanted, out int sample) ? Mathf.Clamp(sample, 0, clip.samples - 1) : 0;
                incoming.Play();
            }
            Debug.Log("MUSIC_CUE " + wanted);
        }

        void Update()
        {
            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                float wanted = Track != null && i == target ? Level : 0;
                source.volume = Mathf.MoveTowards(source.volume, wanted, Level / FadeSeconds * Time.unscaledDeltaTime);
                if (wanted == 0 && source.volume == 0 && source.isPlaying) { Remember(source); source.Stop(); }
            }
        }

        void Remember(AudioSource source)
        {
            if (source.clip != null && source.isPlaying) positions[source.clip.name] = source.timeSamples;
        }

        void OnDestroy() { if (Current == this) Current = null; }
    }
}
