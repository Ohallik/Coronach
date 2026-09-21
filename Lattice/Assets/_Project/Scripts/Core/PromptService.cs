using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lattice.Core
{
    public enum PromptDevice { Keyboard, Gamepad }

    /// <summary>
    /// POLISH-58: the single authority for "which button does this" in player-facing
    /// text. Watches the devices (last-input-wins, runtime-switchable), resolves each
    /// gameplay action's ACTIVE binding — rebinds included — to a glyph from the
    /// generated sheet (Resources/UI/Glyphs for Image consumers, the LatticeSprites
    /// TMP sheet for inline tags), and raises <see cref="Changed"/> when the device or
    /// the bindings move. Every prompt surface composes through here; a literal key
    /// name in UI text is a lint failure (Polish58PromptLint). Degrades gracefully:
    /// with the sheet missing, tags fall back to the binding's display string.
    /// </summary>
    public static class PromptService
    {
        public static PromptDevice Device { get; private set; } = PromptDevice.Keyboard;

        /// <summary>Raised when the active device flips or a binding is rebound.</summary>
        public static event Action Changed;

        static PromptDevice? _forced;
        static readonly Dictionary<string, Sprite> SpriteCache = new();
        static readonly Dictionary<string, string> TagCache = new();

        static PromptService()
        {
            // The controller-only acceptance drive: -padprompts pins the service to
            // pad glyphs so an autopilot run (which presses nothing) captures the
            // gamepad surfaces. Never set on ordinary smoke runs — their frames stay
            // keyboard-deterministic.
            if (Environment.GetCommandLineArgs().Contains("-padprompts"))
            {
                _forced = PromptDevice.Gamepad;
                Device = PromptDevice.Gamepad;
            }
        }

        /// <summary>Test/capture pin; null returns to live device watching.</summary>
        public static void ForceDevice(PromptDevice? device)
        {
            _forced = device;
            var target = device ?? Device;
            if (target != Device)
            {
                Device = target;
                RaiseChanged();
            }
            else if (device != null)
            {
                RaiseChanged(); // re-render surfaces built before the pin
            }
        }

        const float MouseMoveThresholdSq = 9f; // 3 px per frame

        /// <summary>Any gamepad on the bus — real or bridged (PadBridge). Hints that only
        /// matter to a pad player (the rebind listen's pad exit) show when this is true.</summary>
        public static bool PadConnected => Gamepad.all.Count > 0;

        /// <summary>Per-frame device watch (GameManager.Update). Cheap: two device reads.</summary>
        public static void Poll()
        {
            if (_forced != null)
                return;
            var pad = Gamepad.current;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool padActive = pad != null && !pad.CheckStateIsAtDefaultIgnoringNoise();
            // Controller pass: moving the mouse counts as reaching for it — the cursor
            // (hidden while the pad speaks, PadSupport) must come back on a nudge, not
            // only on a click. A few pixels of travel, so desk vibration cannot flip it.
            bool kbActive = (kb != null && kb.anyKey.isPressed)
                || (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed
                                      || mouse.middleButton.isPressed
                                      || mouse.delta.ReadValue().sqrMagnitude > MouseMoveThresholdSq));
            if (Device == PromptDevice.Keyboard && padActive && !kbActive)
            {
                Device = PromptDevice.Gamepad;
                RaiseChanged();
            }
            else if (Device == PromptDevice.Gamepad && kbActive && !padActive)
            {
                Device = PromptDevice.Keyboard;
                RaiseChanged();
            }
        }

        /// <summary>KeyRebind / reset-to-defaults: bindings moved — re-render everything.</summary>
        public static void NotifyBindingsChanged() => RaiseChanged();

        static void RaiseChanged()
        {
            TagCache.Clear();
            Changed?.Invoke();
        }

        // ==================== composition ====================

        /// <summary>Inline glyph tag for the action's active binding on the active device.</summary>
        public static string Tag(string actionName)
        {
            // Before Game.Ensure the input asset doesn't exist — compose the honest
            // fallback but never CACHE it (the first capture run shipped a permanent
            // "?" chip because an Awake-time compose froze into the cache).
            if (GameInput.Current == null)
                return ComposeTag(actionName, null, Device);
            string key = $"{actionName}|{Device}";
            if (TagCache.TryGetValue(key, out var cached))
                return cached;
            string tag = ComposeTag(actionName, null, Device);
            TagCache[key] = tag;
            return tag;
        }

        /// <summary>Explicit-device tag — the Help page shows both devices side by side.</summary>
        public static string TagFor(string actionName, PromptDevice device) =>
            ComposeTag(actionName, null, device);

        /// <summary>Explicit-device movement cluster (Help page rows).</summary>
        public static string MoveTagFor(PromptDevice device) =>
            device == PromptDevice.Gamepad
                ? SheetTag("pad_ls", "d-pad / stick")
                : ComposeTag("Move", "up", device) + ComposeTag("Move", "left", device)
                  + ComposeTag("Move", "down", device) + ComposeTag("Move", "right", device);

        /// <summary>Four-way choose hint (world map pins): WASD cluster / the stick.</summary>
        public static string TagMoveAll() => MoveTagFor(Device);

        /// <summary>Vertical choose hint: W S keycaps (or their rebinds) / the left stick.</summary>
        public static string TagMoveVertical() =>
            Device == PromptDevice.Gamepad
                ? SheetTag("pad_ls", "d-pad / stick")
                : ComposeTag("Move", "up", Device) + ComposeTag("Move", "down", Device);

        /// <summary>Horizontal choose hint: A D keycaps (or their rebinds) / the left stick.</summary>
        public static string TagMoveHorizontal() =>
            Device == PromptDevice.Gamepad
                ? SheetTag("pad_ls", "d-pad / stick")
                : ComposeTag("Move", "left", Device) + ComposeTag("Move", "right", Device);

        /// <summary>
        /// Menu confirm (the EventSystem submit path, not rebindable): Enter / (A).
        /// Distinct from Interact — menus answer to Submit, the world answers to Interact.
        /// </summary>
        public static string SubmitTag() => SubmitTagFor(Device);

        /// <summary>Explicit-device confirm glyph (the Help page shows both devices).</summary>
        public static string SubmitTagFor(PromptDevice device) =>
            device == PromptDevice.Gamepad ? SheetTag("pad_a", "A") : SheetTag("key_enter", "Enter");

        /// <summary>The rebind listen line's cancel key — always the literal Esc keycap
        /// (the listen is keyboard-only by design).</summary>
        public static string EscTag() => SheetTag("key_esc", "Esc");

        /// <summary>Standalone glyph sprite (HUD chip keycap, Help page rows). Null when
        /// the binding has no sheet glyph — callers fall back to <see cref="Label"/>.</summary>
        public static Sprite GlyphSprite(string actionName, string compositePart = null)
        {
            string glyph = ActiveGlyphName(actionName, compositePart, Device);
            return glyph == null ? null : LoadSprite(glyph);
        }

        /// <summary>Human-readable binding label ("E", "Left Shift", "A") — the fallback
        /// when no glyph art exists for a binding.</summary>
        public static string Label(string actionName, string compositePart = null)
            => Label(actionName, compositePart, Device);

        static string Label(string actionName, string compositePart, PromptDevice device)
        {
            var action = FindAction(actionName);
            if (action == null)
                return "?";
            int index = BindingIndexFor(action, device, compositePart);
            if (index < 0)
                return "?";
            return action.GetBindingDisplayString(index,
                InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }

        // ==================== resolution ====================

        static string ComposeTag(string actionName, string compositePart, PromptDevice device)
        {
            string glyph = ActiveGlyphName(actionName, compositePart, device);
            if (glyph != null)
                return SheetTag(glyph, Label(actionName, compositePart, device));
            // No art for this binding (exotic rebind): show the display string, styled.
            return $"<b>{Label(actionName, compositePart, device)}</b>";
        }

        /// <summary>Sheet-backed inline tag with graceful text fallback (UiSkin pattern —
        /// duplicated here because Core cannot reference the UI assembly).</summary>
        static string SheetTag(string sprite, string fallback)
        {
            var sheet = TMP_Settings.defaultSpriteAsset;
            if (sheet != null && sheet.name == "LatticeSprites"
                && SheetHasSprite(sheet, sprite))
                return $"<sprite name=\"{sprite}\">";
            return fallback;
        }

        static readonly Dictionary<string, bool> SheetLookup = new();

        static bool SheetHasSprite(TMP_SpriteAsset sheet, string name)
        {
            if (SheetLookup.TryGetValue(name, out bool has))
                return has;
            has = sheet.GetSpriteIndexFromName(name) >= 0;
            SheetLookup[name] = has;
            return has;
        }

        static InputAction FindAction(string actionName) =>
            GameInput.Current != null ? GameInput.Current.Find(actionName) : null;

        static string ActiveGlyphName(string actionName, string compositePart, PromptDevice device)
        {
            var action = FindAction(actionName);
            if (action == null)
                return null;
            int index = BindingIndexFor(action, device, compositePart);
            if (index < 0)
                return null;
            var b = action.bindings[index];
            string path = string.IsNullOrEmpty(b.overridePath) ? b.path : b.overridePath;
            return GlyphName(path);
        }

        /// <summary>First binding of the action for the device; composite parts by name on
        /// keyboard, the whole-stick binding on pad.</summary>
        static int BindingIndexFor(InputAction action, PromptDevice device, string compositePart)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (b.isComposite)
                    continue;
                string path = string.IsNullOrEmpty(b.overridePath) ? b.path : b.overridePath;
                bool kb = path.StartsWith("<Keyboard>");
                bool pad = path.StartsWith("<Gamepad>");
                if (device == PromptDevice.Keyboard && kb)
                {
                    if (compositePart == null && !b.isPartOfComposite)
                        return i;
                    if (compositePart != null && b.isPartOfComposite
                        && string.Equals(b.name, compositePart, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
                else if (device == PromptDevice.Gamepad && pad)
                {
                    return i; // pad bindings are never composites in this project
                }
            }
            return -1;
        }

        /// <summary>Control path → glyph sheet name, or null when no art covers it.
        /// Public for the Polish58 mapping tests.</summary>
        public static string GlyphName(string controlPath)
        {
            int slash = controlPath.IndexOf('/');
            if (slash < 0)
                return null;
            string device = controlPath.Substring(0, slash);
            string control = controlPath.Substring(slash + 1).ToLowerInvariant();
            if (device.Contains("Gamepad"))
                return control switch
                {
                    "buttonsouth" => "pad_a",
                    "buttoneast" => "pad_b",
                    "buttonwest" => "pad_x",
                    "buttonnorth" => "pad_y",
                    "start" => "pad_start",
                    "select" => "pad_view",
                    "leftshoulder" => "pad_lb",
                    "rightshoulder" => "pad_rb",
                    "lefttrigger" => "pad_lt",
                    "righttrigger" => "pad_rt",
                    "leftstickpress" => "pad_l3",
                    "rightstickpress" => "pad_r3",
                    "leftstick" => "pad_ls",
                    "rightstick" => "pad_rs",
                    "dpad" => "pad_dpad",
                    "dpad/up" => "pad_dpad_up",
                    "dpad/down" => "pad_dpad_down",
                    "dpad/left" => "pad_dpad_left",
                    "dpad/right" => "pad_dpad_right",
                    _ => null,
                };
            if (!device.Contains("Keyboard"))
                return null;
            if (control.Length == 1 && (char.IsLetterOrDigit(control[0])))
                return $"key_{control}";
            if (control.StartsWith("numpad") && control.Length == 7 && char.IsDigit(control[6]))
                return $"key_{control[6]}";
            return control switch
            {
                "leftshift" or "rightshift" => "key_shift",
                "leftctrl" or "rightctrl" => "key_ctrl",
                "leftalt" or "rightalt" => "key_alt",
                "escape" => "key_esc",
                "enter" or "numpadenter" => "key_enter",
                "space" => "key_space",
                "tab" => "key_tab",
                "backspace" => "key_backspace",
                "delete" => "key_delete",
                "insert" => "key_insert",
                "home" => "key_home",
                "end" => "key_end",
                "pageup" => "key_pageup",
                "pagedown" => "key_pagedown",
                "capslock" => "key_caps",
                "uparrow" => "key_up",
                "downarrow" => "key_down",
                "leftarrow" => "key_left",
                "rightarrow" => "key_right",
                "semicolon" => "key_semicolon",
                "quote" => "key_quote",
                "comma" => "key_comma",
                "period" => "key_period",
                "slash" => "key_slash",
                "backslash" => "key_backslash",
                "minus" => "key_minus",
                "equals" => "key_equals",
                "leftbracket" => "key_leftbracket",
                "rightbracket" => "key_rightbracket",
                "backquote" => "key_backquote",
                "f1" or "f2" or "f3" or "f4" or "f5" or "f6" or "f7" or "f8" or "f9"
                    or "f10" or "f11" or "f12" => $"key_{control}",
                _ => null,
            };
        }

        static Sprite LoadSprite(string name)
        {
            if (SpriteCache.TryGetValue(name, out var sprite))
                return sprite;
            sprite = Resources.Load<Sprite>("UI/Glyphs/" + name);
            SpriteCache[name] = sprite; // cache nulls too
            return sprite;
        }

        /// <summary>Editor/tests teardown safety — drop caches and any device pin.</summary>
        public static void Reset()
        {
            _forced = null;
            Device = PromptDevice.Keyboard;
            SpriteCache.Clear();
            TagCache.Clear();
            SheetLookup.Clear();
        }
    }
}
