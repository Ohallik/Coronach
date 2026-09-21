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
        Synced, // Ozzie's tech-mode persona
        // ---- extended set (P65): shared vocabulary, per-character nuance. Each
        // character wears these their own way (Amused is Ozzie's belly-laugh but
        // Martok's fought-off smirk). A character with no honest cell for a slot
        // has NO entry for it — Get() falls back to Neutral rather than showing
        // a wrong-valence face. Append-only: ordinals are serialized in assets.
        Amused,
        Smug,
        Curious,
        Flustered,
        Tender,
        Determined,
        Playful,
        Special, // the character-signature cell: Ozzie's fetched wrench, Martok's
                 // border grief, Roka's scent-lock, Shaza's concealment
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
