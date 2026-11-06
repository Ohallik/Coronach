using Lattice.Core;
using UnityEngine.InputSystem;

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

        /// <summary>At a choice the same buttons pick the selected option.</summary>
        public static string Choose() =>
            (Pad ? PromptService.SubmitTag() : PromptService.Tag("Interact") + " / " + PromptService.SubmitTag()) + "   CHOOSE";

        // Bound keys by name, separated: the prompt sheet is absent, so adjacent key
        // tags would otherwise run together as plain labels ("WS").
        static string Keys(string action, string a, string b) => PromptService.Label(action, a) + " / " + PromptService.Label(action, b);
        public static string Navigate() => Pad ? PromptService.TagMoveVertical() : Keys("Move", "up", "down");
        public static string Adjust() => Pad ? PromptService.TagMoveHorizontal() : Keys("Move", "left", "right");

        // Cycling has no button of its own: the arrow keys, the d-pad, or on a Precision, whose only directions walk, LB and a direction.
        static string PadCycle(string directions)
        {
            var pad = Gamepad.current;
            return pad != null && PadBridge.StickIsDpad(pad) ? PromptService.Tag("LockOn") + " + " + directions : "D-pad " + directions;
        }
        public static string CycleItem() => Pad ? PadCycle("up/down") : Keys("CycleItem", "Positive", "Negative");
        static string targetKeys;
        /// <summary>The pair under one word ("Left/Right Arrow"): the target line shares its row with the break gauge and the lock. A real
        /// keyboard's keys go by the OS's names ("Left") and an absent one's by the layout's ("Left Arrow"), so the word goes back on either
        /// way: left and right alone mean A and D here. The HUD asks every frame and no menu rebinds these keys, so they're looked up once.</summary>
        public static string CycleTarget()
        {
            if (Pad) return PadCycle("left/right");
            if (targetKeys != null) return targetKeys;
            const string arrow = " Arrow";
            string Bare(string key) => key.EndsWith(arrow) ? key.Substring(0, key.Length - arrow.Length) : key;
            return targetKeys = Bare(PromptService.Label("CycleTarget", "Negative")) + "/" + Bare(PromptService.Label("CycleTarget", "Positive")) + arrow;
        }

        public static string TitleMenu() =>
            PromptService.SubmitTag() + "   CONFIRM     ·     " + Navigate() + "   NAVIGATE";

        public static string Pages() => PromptService.Tag("MenuPrev") + " / " + PromptService.Tag("MenuNext");

        public static string AudioControls() => Pad
            ? "Left/right adjusts the selected channel. Up/down selects a channel. " + Pages() + " changes tabs."
            : Adjust() + " adjusts the selected channel. " + Navigate() + " selects a channel. " + Pages() + " changes tabs.";
    }
}
