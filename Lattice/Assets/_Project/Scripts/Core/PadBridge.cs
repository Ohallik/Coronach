using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.LowLevel;

namespace Lattice.Core
{
    /// <summary>
    /// Everything the bridge reads off a generic joystick, in pad terms. Pure data so the
    /// mapping (<see cref="PadBridge.Translate"/>) is testable without a device.
    /// </summary>
    public struct PadSnapshot
    {
        /// <summary>Main X/Y stick, -1..1, up = +1 (the HID layout already flips Y).</summary>
        public Vector2 Stick;
        /// <summary>Secondary axes when the device has them (Z/Rz or Rx/Ry), up = +1.</summary>
        public Vector2 RightStick;
        /// <summary>Hat switch as a direction vector; only meaningful when <see cref="HasHat"/>.</summary>
        public Vector2 Hat;
        public bool HasHat;
        /// <summary>Bit n-1 set = HID button n pressed (1-based, as the HID layout numbers them).</summary>
        public uint Buttons;

        public bool Button(int number) =>
            number >= 1 && number <= 32 && (Buttons & (1u << (number - 1))) != 0;

        public PadSnapshot WithButton(int number, bool down = true)
        {
            if (number < 1 || number > 32)
                return this;
            uint bit = 1u << (number - 1);
            Buttons = down ? Buttons | bit : Buttons & ~bit;
            return this;
        }
    }

    /// <summary>
    /// How one DirectInput-class pad's numbered buttons and axes map onto the standard
    /// gamepad. The default is the ordering Logitech (and most DirectInput pads) use:
    /// 1 2 3 4 are the left/bottom/right/top face buttons (PlayStation order), 5-8 the
    /// shoulders and triggers, 9/10 select/start, 11/12 the stick clicks.
    /// </summary>
    public sealed class PadProfile
    {
        public string Name = "DirectInput";
        public int West = 1, South = 2, East = 3, North = 4;
        public int LeftShoulder = 5, RightShoulder = 6, LeftTrigger = 7, RightTrigger = 8;
        public int Select = 9, Start = 10, LeftStickPress = 11, RightStickPress = 12;
        /// <summary>Digital pads report the d-pad on the X/Y axes and have no hat switch;
        /// with this set the main stick also drives the d-pad.</summary>
        public bool StickIsDpad;
        /// <summary>HID control names that carry the second stick (null when absent).</summary>
        public string RightStickX = "z", RightStickY = "rz";
    }

    /// <summary>
    /// Turns any generic HID joystick — a DirectInput-class pad the Input System does not
    /// recognise as a <see cref="Gamepad"/> (the Logitech Precision, Dual Action and
    /// Rumblepad family, most no-name USB pads) — into a virtual <see cref="Gamepad"/>, so
    /// every &lt;Gamepad&gt; binding in the game (GameInput, the UI module's
    /// Navigate/Submit/Cancel, PromptService's device watch and glyphs) works unchanged.
    /// XInput, DualShock/DualSense and Switch Pro pads are already Gamepads and are
    /// left alone.
    ///
    /// Mechanics: the bridge listens to <see cref="InputSystem.onEvent"/>. Each state
    /// event of a bridged joystick is read through its <see cref="PadProfile"/> into a
    /// <see cref="GamepadState"/> and queued for the virtual pad from INSIDE the same
    /// update (InputManager appends events queued while its stream is open), so there is
    /// no added frame of latency, and the virtual pad becomes <see cref="Gamepad.current"/>
    /// exactly when a real button moves. Devices come and go through onDeviceChange.
    /// </summary>
    public static class PadBridge
    {
        const int MaxButtons = 16;
        const float DpadThreshold = 0.5f;

        sealed class Link
        {
            public Joystick Joystick;
            public Gamepad Pad;
            public PadProfile Profile;
            public StickControl Stick;
            public Vector2Control Hat;
            public AxisControl RightX, RightY;
            public bool RightYInvert;
            public readonly ButtonControl[] Buttons = new ButtonControl[MaxButtons + 1];
            public int ButtonCount;
            public GamepadState Last;
        }

        static readonly Dictionary<int, Link> Links = new();
        static readonly List<(Func<InputDevice, bool> match, Func<PadProfile> make)> Profiles = new();
        static bool _installed;

        public static bool Installed => _installed;
        public static int LinkCount => Links.Count;

        static PadBridge()
        {
            // Logitech Precision Gamepad (VID 046D PID C21A): usage Joystick, X/Y are the
            // d-pad (0/127/255, no hat, no analog sticks), ten buttons in Logitech order.
            RegisterProfile(d => Hid(d, 0x046D, 0xC21A) || Product(d, "Precision.*Gamepad"),
                () => new PadProfile
                {
                    Name = "LogitechPrecision", StickIsDpad = true, RightStickX = null, RightStickY = null,
                });
            // Logitech Dual Action / Rumblepad 2 / Cordless Rumblepad 2 — and the F310/F510/
            // F710 with their switch on D, which identify as those older pads. Same button
            // order; the right stick rides Z/Rz.
            RegisterProfile(d => Hid(d, 0x046D, 0xC216) || Hid(d, 0x046D, 0xC218) || Hid(d, 0x046D, 0xC219)
                                 || Product(d, "Dual Action|Rumble ?Pad"),
                () => new PadProfile { Name = "LogitechDualAction" });
        }

        /// <summary>Add a profile for a specific pad. Later registrations win.</summary>
        public static void RegisterProfile(Func<InputDevice, bool> match, Func<PadProfile> make) =>
            Profiles.Insert(0, (match, make));

        public static PadProfile ResolveProfile(InputDevice device)
        {
            foreach (var (match, make) in Profiles)
            {
                bool hit;
                try { hit = match(device); }
                catch (Exception) { hit = false; }
                if (hit)
                    return make();
            }
            return new PadProfile();
        }

        // A HID-backed joystick is a plain Joystick instance (HIDSupport builds its layout
        // on Joystick, not on HID), so the ids live in the description's capabilities JSON.
        static bool TryHidIds(InputDevice d, out int vendorId, out int productId)
        {
            vendorId = productId = 0;
            var desc = d.description;
            if (desc.interfaceName != "HID" || string.IsNullOrEmpty(desc.capabilities))
                return false;
            try
            {
                var hid = HID.HIDDeviceDescriptor.FromJson(desc.capabilities);
                vendorId = hid.vendorId;
                productId = hid.productId;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static bool Hid(InputDevice d, int vendorId, int productId) =>
            TryHidIds(d, out int vid, out int pid) && vid == vendorId && pid == productId;

        static bool Product(InputDevice d, string pattern) =>
            !string.IsNullOrEmpty(d.description.product)
            && Regex.IsMatch(d.description.product, pattern, RegexOptions.IgnoreCase);

        // ---------- lifecycle ----------

        public static void Install()
        {
            if (_installed)
                return;
            _installed = true;
            InputSystem.onDeviceChange += OnDeviceChange;
            InputSystem.onEvent += OnEvent;
            foreach (var device in InputSystem.devices)
                TryAttach(device);
            Debug.Log($"[Lattice] PAD_BRIDGE_OK devices={InputSystem.devices.Count} " +
                      $"gamepads={Gamepad.all.Count} joysticks={Joystick.all.Count} bridged={Links.Count}");
        }

        public static void Uninstall()
        {
            if (!_installed)
                return;
            InputSystem.onDeviceChange -= OnDeviceChange;
            InputSystem.onEvent -= OnEvent;
            foreach (var id in new List<int>(Links.Keys))
                Detach(id);
            _installed = false;
        }

        /// <summary>Tests: the Input System was reset under us — rehook and rescan.</summary>
        public static void Reinstall()
        {
            Uninstall();
            Install();
        }

        /// <summary>The virtual pad standing in for a joystick, or null.</summary>
        public static Gamepad PadFor(InputDevice joystick) =>
            joystick != null && Links.TryGetValue(joystick.deviceId, out var link) ? link.Pad : null;

        public static bool IsBridgedPad(InputDevice device)
        {
            if (device == null)
                return false;
            foreach (var link in Links.Values)
                if (ReferenceEquals(link.Pad, device))
                    return true;
            return false;
        }

        public static string ProfileNameFor(InputDevice joystick) =>
            joystick != null && Links.TryGetValue(joystick.deviceId, out var link) ? link.Profile.Name : null;

        static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                case InputDeviceChange.Enabled:
                    TryAttach(device);
                    break;
                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                case InputDeviceChange.Disabled:
                    Detach(device.deviceId);
                    break;
            }
        }

        /// <summary>Bridge a joystick if it is one and is not bridged yet.</summary>
        public static bool TryAttach(InputDevice device)
        {
            if (!_installed || device is not Joystick joystick || Links.ContainsKey(device.deviceId))
                return false;
            var profile = ResolveProfile(joystick);
            var link = new Link { Joystick = joystick, Profile = profile };
            link.Stick = joystick.stick ?? joystick.TryGetChildControl<StickControl>("stick");
            link.Hat = joystick.TryGetChildControl<Vector2Control>("hat") ?? joystick.hatswitch;
            if (profile.RightStickX != null)
                link.RightX = joystick.TryGetChildControl<AxisControl>(profile.RightStickX);
            if (profile.RightStickY != null)
                link.RightY = joystick.TryGetChildControl<AxisControl>(profile.RightStickY);
            if (link.RightX == null && link.RightY == null && profile.RightStickX != null)
            {
                // Some pads carry the second stick on Rx/Ry instead of Z/Rz.
                link.RightX = joystick.TryGetChildControl<AxisControl>("rx");
                link.RightY = joystick.TryGetChildControl<AxisControl>("ry");
            }
            // The HID layout flips Y-like axes (Y, Ry) so up reads +1, but treats Rz like X.
            link.RightYInvert = link.RightY != null
                && string.Equals(link.RightY.name, "rz", StringComparison.OrdinalIgnoreCase);
            for (int n = 1; n <= MaxButtons; n++)
            {
                link.Buttons[n] = joystick.TryGetChildControl<ButtonControl>(n == 1 ? "trigger" : $"button{n}");
                if (link.Buttons[n] != null)
                    link.ButtonCount = n;
            }
            if (link.Stick == null && link.Hat == null && link.ButtonCount == 0)
                return false; // nothing a pad could use

            link.Pad = InputSystem.AddDevice<Gamepad>($"{joystick.displayName} (bridged)");
            Links[device.deviceId] = link;
            Push(link, Snapshot(link, default), -1);

            string vidPid = TryHidIds(joystick, out int vid, out int pid)
                ? $"vid={vid:X4} pid={pid:X4}"
                : "vid=n/a pid=n/a";
            Debug.Log($"PAD_BRIDGE_ATTACH {vidPid} profile={profile.Name}");
            Debug.Log($"[Lattice] PAD_BRIDGE_DEVICE joystick=\"{joystick.displayName}\" layout={joystick.layout} " +
                      $"{vidPid} profile={profile.Name} buttons={link.ButtonCount} stick={link.Stick != null} " +
                      $"hat={link.Hat != null} rightStick={link.RightX != null} pad=\"{link.Pad.name}\"");
            return true;
        }

        static void Detach(int joystickId)
        {
            if (!Links.Remove(joystickId, out var link))
                return;
            if (link.Pad != null && link.Pad.added)
                InputSystem.RemoveDevice(link.Pad);
            Debug.Log($"[Lattice] PAD_BRIDGE_DETACH joystick=\"{link.Joystick?.displayName}\"");
        }

        // ---------- the translation ----------

        static void OnEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (device == null || !Links.TryGetValue(device.deviceId, out var link))
                return;
            var type = eventPtr.type;
            if (type != StateEvent.Type && type != DeltaStateEvent.Type)
                return;
            Push(link, Snapshot(link, eventPtr), eventPtr.time);
        }

        static void Push(Link link, PadSnapshot snapshot, double time)
        {
            var state = Translate(snapshot, link.Profile);
            if (Same(state, link.Last))
                return;
            link.Last = state;
            if (time >= 0)
                InputSystem.QueueStateEvent(link.Pad, state, time);
            else
                InputSystem.QueueStateEvent(link.Pad, state);
        }

        static PadSnapshot Snapshot(Link link, InputEventPtr e)
        {
            var s = new PadSnapshot();
            if (link.Stick != null)
                s.Stick = ReadVec(link.Stick, e);
            if (link.Hat != null)
            {
                s.Hat = ReadVec(link.Hat, e);
                s.HasHat = true;
            }
            if (link.RightX != null)
                s.RightStick.x = ReadAxis(link.RightX, e);
            if (link.RightY != null)
                s.RightStick.y = ReadAxis(link.RightY, e) * (link.RightYInvert ? -1f : 1f);
            for (int n = 1; n <= link.ButtonCount; n++)
            {
                var b = link.Buttons[n];
                if (b != null && ReadAxis(b, e) >= b.pressPointOrDefault)
                    s.Buttons |= 1u << (n - 1);
            }
            return s;
        }

        static float ReadAxis(InputControl<float> c, InputEventPtr e) =>
            e.valid && c.ReadValueFromEvent(e, out float v) ? v : c.ReadValue();

        static Vector2 ReadVec(InputControl<Vector2> c, InputEventPtr e) =>
            e.valid && c.ReadValueFromEvent(e, out Vector2 v) ? v : c.ReadValue();

        /// <summary>The pure mapping: a joystick snapshot through a profile to a gamepad state.</summary>
        public static GamepadState Translate(in PadSnapshot s, PadProfile p)
        {
            var g = new GamepadState { leftStick = s.Stick, rightStick = s.RightStick };
            Vector2 d = s.HasHat ? s.Hat : (p.StickIsDpad ? s.Stick : Vector2.zero);
            if (d.y > DpadThreshold) g = g.WithButton(GamepadButton.DpadUp);
            if (d.y < -DpadThreshold) g = g.WithButton(GamepadButton.DpadDown);
            if (d.x < -DpadThreshold) g = g.WithButton(GamepadButton.DpadLeft);
            if (d.x > DpadThreshold) g = g.WithButton(GamepadButton.DpadRight);
            if (s.Button(p.South)) g = g.WithButton(GamepadButton.South);
            if (s.Button(p.East)) g = g.WithButton(GamepadButton.East);
            if (s.Button(p.West)) g = g.WithButton(GamepadButton.West);
            if (s.Button(p.North)) g = g.WithButton(GamepadButton.North);
            if (s.Button(p.LeftShoulder)) g = g.WithButton(GamepadButton.LeftShoulder);
            if (s.Button(p.RightShoulder)) g = g.WithButton(GamepadButton.RightShoulder);
            if (s.Button(p.Select)) g = g.WithButton(GamepadButton.Select);
            if (s.Button(p.Start)) g = g.WithButton(GamepadButton.Start);
            if (s.Button(p.LeftStickPress)) g = g.WithButton(GamepadButton.LeftStick);
            if (s.Button(p.RightStickPress)) g = g.WithButton(GamepadButton.RightStick);
            // The Gamepad layout reads its triggers from the float fields.
            g.leftTrigger = s.Button(p.LeftTrigger) ? 1f : 0f;
            g.rightTrigger = s.Button(p.RightTrigger) ? 1f : 0f;
            return g;
        }

        static bool Same(in GamepadState a, in GamepadState b) =>
            a.buttons == b.buttons
            && a.leftStick == b.leftStick && a.rightStick == b.rightStick
            && Mathf.Approximately(a.leftTrigger, b.leftTrigger)
            && Mathf.Approximately(a.rightTrigger, b.rightTrigger);

        /// <summary>One line per bridged device, for logs and tests.</summary>
        public static string Describe()
        {
            if (Links.Count == 0)
                return "(no bridged joysticks)";
            var lines = new List<string>();
            foreach (var link in Links.Values)
                lines.Add($"{link.Joystick?.displayName} -> {link.Pad?.name} [{link.Profile.Name}]");
            return string.Join("; ", lines);
        }
    }
}
