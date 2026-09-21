using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lattice.Data
{
    public enum PortraitEmotion
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Shocked,
        Worried,
        Intense,
        Synced,
        // Append-only: ordinals are serialized in the generated 4x4 sheets.
        Amused,
        Smug,
        Curious,
        Flustered,
        Tender,
        Determined,
        Playful,
        Special,
    }

    /// <summary>
    /// All 2D anime portraits for one character. Dialogue/UI requests (character, emotion)
    /// and never touches sprite files directly — the swappable-art rule.
    /// </summary>
    [CreateAssetMenu(menuName = "Lattice/Art/Portrait Set", fileName = "PortraitSet")]
    public sealed class PortraitSet : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public PortraitEmotion emotion;
            public Sprite sprite;
        }

        public string characterName;
        public List<Entry> entries = new();

        public Sprite Get(PortraitEmotion emotion)
        {
            foreach (var e in entries)
                if (e.emotion == emotion && e.sprite != null)
                    return e.sprite;
            // Fall back to neutral, then to anything present.
            foreach (var e in entries)
                if (e.emotion == PortraitEmotion.Neutral && e.sprite != null)
                    return e.sprite;
            return entries.Count > 0 ? entries[0].sprite : null;
        }
    }
}
