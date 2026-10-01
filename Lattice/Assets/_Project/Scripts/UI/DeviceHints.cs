using Lattice.Core;

namespace Lattice.UI
{
    /// <summary>
    /// Button hints in the words of the device the player last touched: pad glyphs
    /// for a pad, keys for the keyboard, never both at once. Surfaces re-render
    /// these on <see cref="PromptService.Changed"/>.
    /// </summary>
    public static class DeviceHints
    {
        static bool Pad => PromptService.Device == PromptDevice.Gamepad;

        /// <summary>Dialogue advances on Submit, and on the world's Interact key.</summary>
        public static string Continue() =>
            (Pad ? PromptService.SubmitTag() : PromptService.Tag("Interact") + " / " + PromptService.SubmitTag()) + "   CONTINUE";

        // Bound keys by name, separated: the prompt sheet is absent, so adjacent key
        // tags would otherwise run together as plain labels ("WS").
        static string Keys(string action, string a, string b) => PromptService.Label(action, a) + " / " + PromptService.Label(action, b);
        public static string Navigate() => Pad ? PromptService.TagMoveVertical() : Keys("Move", "up", "down");
        public static string Adjust() => Pad ? PromptService.TagMoveHorizontal() : Keys("Move", "left", "right");

        public static string TitleMenu() =>
            PromptService.SubmitTag() + "   CONFIRM     ·     " + Navigate() + "   NAVIGATE";

        public static string Pages() => PromptService.Tag("MenuPrev") + " / " + PromptService.Tag("MenuNext");

        public static string AudioControls() => Pad
            ? "Left/right adjusts the selected channel. Up/down selects a channel. " + Pages() + " changes tabs."
            : Adjust() + " adjusts the selected channel. " + Navigate() + " selects a channel. " + Pages() + " changes tabs.";
    }
}
