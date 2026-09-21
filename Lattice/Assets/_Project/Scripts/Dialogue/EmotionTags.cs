using System;
using Lattice.Data;

namespace Lattice.Dialogue
{
    /// <summary>
    /// Parses the per-line emotion convention: Yarn lines carry a hashtag like
    /// "#emotion:sad" and the dialogue UI swaps the speaker's portrait accordingly.
    /// No tag = Neutral. Pure logic, EditMode-testable.
    /// </summary>
    public static class EmotionTags
    {
        public const string Prefix = "emotion:";

        public static PortraitEmotion Parse(string[] metadata)
        {
            if (metadata == null)
                return PortraitEmotion.Neutral;
            foreach (var raw in metadata)
            {
                if (string.IsNullOrEmpty(raw))
                    continue;
                var tag = raw[0] == '#' ? raw.Substring(1) : raw;
                if (!tag.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                var value = tag.Substring(Prefix.Length).Trim();
                if (Enum.TryParse<PortraitEmotion>(value, ignoreCase: true, out var emotion))
                    return emotion;
                UnityEngine.Debug.LogWarning($"[Lattice] unknown emotion tag '{raw}' — using Neutral");
                return PortraitEmotion.Neutral;
            }
            return PortraitEmotion.Neutral;
        }
    }
}
