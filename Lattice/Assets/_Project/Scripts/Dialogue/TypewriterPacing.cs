namespace Lattice.Dialogue
{
    /// <summary>
    /// Letter-by-letter reveal cadence: a base characters-per-second rate with breath
    /// pauses after punctuation (and a long hang on ellipses, which the CAPS human-speech
    /// convention leans on heavily). Pure logic, EditMode-testable.
    /// </summary>
    public static class TypewriterPacing
    {
        public const float CharsPerSecond = 38f;
        public const float CommaPause = 0.10f;
        public const float SentencePause = 0.18f;
        public const float EllipsisPause = 0.28f;

        /// <summary>Settings-menu text speed: 1 = normal, &gt;1 faster. Applied on top of the cadence.</summary>
        public static float SpeedMultiplier = 1f;

        /// <summary>
        /// Seconds to wait after revealing the character at <paramref name="index"/> of
        /// <paramref name="text"/> (plain text, no markup). Ellipsis pause fires once,
        /// on the last dot of a run.
        /// </summary>
        public static float DelayAfter(string text, int index) =>
            RawDelayAfter(text, index) / System.Math.Max(0.25f, SpeedMultiplier);

        static float RawDelayAfter(string text, int index)
        {
            float baseDelay = 1f / CharsPerSecond;
            if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length)
                return baseDelay;

            char c = text[index];
            bool lastOfRun = index + 1 >= text.Length || text[index + 1] != c;
            switch (c)
            {
                case '.':
                    if (!lastOfRun)
                        return baseDelay; // middle of an ellipsis run
                    bool wasRun = index > 0 && text[index - 1] == '.';
                    return wasRun ? EllipsisPause : SentencePause;
                case '…': // single-char ellipsis
                    return EllipsisPause;
                case '!':
                case '?':
                    return lastOfRun ? SentencePause : baseDelay;
                case ',':
                case ';':
                case '—': // em dash
                    return CommaPause;
                default:
                    return baseDelay;
            }
        }

        /// <summary>Should revealing this character fire a voice blip? (Skips space/punctuation.)</summary>
        public static bool IsBlipChar(char c) => char.IsLetterOrDigit(c);
    }
}
