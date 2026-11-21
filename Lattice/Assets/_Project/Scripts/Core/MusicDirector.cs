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
        // Boss encounters override the location cue until they end or are removed.
        readonly List<(Object owner, string track)> encounters = new();
        // Ordinary fights awake in a zone that names a fight cue (ZoneDef.fightMusicId): under any boss's, whichever began first.
        readonly HashSet<Object> fights = new();
        string location, locationTrack, fightTrack, shopTrack = MarketTrack;
        int target;
        const float Level = .52f, FadeSeconds = 1.1f;
        /// <summary>What plays where nothing names anything else: with a shop open, and through a boss.</summary>
        public const string MarketTrack = "Moonbase Market", BossTrack = "Alien Boss Battle";

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
                source.outputAudioMixerGroup = AudioMix.Group(AudioBus.Music);
            }
        }

        public static void SetLocation(string zone, bool combat)
        {
            if (Current == null) return;
            if (Current.location != zone) { Current.shops.Clear(); Current.encounters.Clear(); Current.fights.Clear(); }
            Current.location = zone;
            Current.locationTrack = Authored(zone, combat);
            // A zone with no line of its own below plays what its definition names: every zone since chapter 3. The first thirteen keep theirs,
            // silences included (the yard in a safe pocket), whatever their definitions say.
            var definition = zone == "Title" ? null : GameCatalog.Find<Lattice.Data.ZoneDef>(zone);
            if (definition != null && System.Array.IndexOf(AuthoredZones, zone) < 0)
            {
                bool pocket = !combat && (definition.kind == Lattice.Data.ZoneKind.GroundCombat || definition.kind == Lattice.Data.ZoneKind.SpaceCombat);
                Current.locationTrack = Named(pocket && !string.IsNullOrEmpty(definition.pocketMusicId) ? definition.pocketMusicId : definition.musicId);
                if (Current.locationTrack == null) Debug.LogWarning("MUSIC_UNSET " + zone + ": its ZoneDef names no musicId, so it plays in silence");
            }
            Current.fightTrack = definition != null ? Named(definition.fightMusicId) : null;
            Current.shopTrack = definition != null ? Named(definition.shopMusicId) ?? MarketTrack : MarketTrack;
            Current.Refresh();
        }

        static string Named(string track) => string.IsNullOrWhiteSpace(track) ? null : track;

        /// <summary>The zones whose cues are written out in <see cref="Authored"/>. Nothing is added here: a new zone names its music on its ZoneDef.</summary>
        static readonly string[] AuthoredZones =
        {
            "Hub_CinderHalo", "Hub_Decks", "Sorrel_Ridges", "Hushwell", "Gullet_Tunnel", "TallowApproach", "TallowDrift", "Arena_Ground", "Arena_Flight",
            "WickReach", "Lanternwick", "LanternGorge", "SunkenRelay",
        };

        /// <summary>The first two chapters' cues, as they were written before a zone could name its own.</summary>
        static string Authored(string zone, bool combat)
        {
            return zone == "Title" ? "Title Theme" :
                (zone == "Sorrel_Ridges" || zone == "Arena_Ground") && combat ? "Adventure Awaits" :
                zone == "Hub_Decks" || zone == "Hub_CinderHalo" || zone == "TallowDrift" ||
                zone == "TallowApproach" || zone == "Sorrel_Ridges" ? "Hub Town Groove" :
                zone == "Gullet_Tunnel" || zone == "Arena_Flight" ? "Starfight" :
                zone == "Hushwell" ? "Moon Caverns" :
                // Chapter 2 (D202, per the atlas music map): the ancient town has its own theme; the Wick Reach is
                // space combat; the gorge and the ruin under it are adventure ground.
                zone == "Lanternwick" ? "Old Village" :
                zone == "WickReach" ? "Starfight" :
                zone == "LanternGorge" ? "Adventure Awaits" :
                zone == "SunkenRelay" ? "Moon Caverns" : null;
        }

        /// <summary>The next time this track is asked for it starts from its beginning, not where it was left: the credits play the title's theme from its first bar, wherever the title
        /// or chapter 9's launch last left it.</summary>
        public static void FromTheTop(string track)
        {
            if (Current != null && !string.IsNullOrEmpty(track)) Current.positions.Remove(track);
        }

        public static void Shop(Object owner, bool open)
        {
            if (Current == null || owner == null) return;
            if (open) Current.shops.Add(owner); else Current.shops.Remove(owner);
            Current.Refresh();
        }

        public static void Encounter(Object owner, string track, bool active)
        {
            if (Current == null || owner == null) return;
            Current.encounters.RemoveAll(e => e.owner == owner);
            if (active) Current.encounters.Add((owner, track));
            Current.Refresh();
        }

        /// <summary>An ordinary fight is awake, or over. It plays the zone's fight cue where the zone names one, and nothing new where it doesn't;
        /// the zone's own music comes back when the last of them ends, as it does after a boss.</summary>
        public static void Fight(Object owner, bool active)
        {
            if (Current == null || owner == null) return;
            if (active ? !Current.fights.Add(owner) : !Current.fights.Remove(owner)) return;
            Current.Refresh();
        }

        void Refresh()
        {
            shops.RemoveWhere(owner => owner == null);
            encounters.RemoveAll(e => e.owner == null);
            fights.RemoveWhere(owner => owner == null);
            string wanted = shops.Count > 0 ? shopTrack : encounters.Count > 0 ? encounters[encounters.Count - 1].track :
                fights.Count > 0 && fightTrack != null ? fightTrack : locationTrack;
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
            // An encounter owner destroyed without ending (retry, unload) releases its cue.
            if (encounters.Exists(e => e.owner == null)) Refresh();
            else if (fights.Count > 0) foreach (var owner in fights) if (owner == null) { Refresh(); break; }
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
