using System.IO;
using System.Linq;
using Lattice.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace Lattice.Tests.EditMode
{
    /// <summary>
    /// CONTROLLER PASS (2026-09-17) — the pure half.
    ///
    /// The Input System hands a DirectInput-class pad (usage Joystick, no XInput) to the
    /// game as a generic <c>Joystick</c> with <c>stick</c>, <c>trigger</c> and
    /// <c>button2..N</c>, never as a Gamepad — so every &lt;Gamepad&gt; binding, the UI
    /// module's Navigate/Submit/Cancel and the prompt glyphs were dead for the Logitech
    /// Precision plugged into the dev machine. PadBridge translates such a device into a
    /// virtual Gamepad; this suite pins the translation table and the profile lookup, and
    /// the shape of the UI map that replaces the package defaults.
    /// </summary>
    public class PadSupportTests
    {
        // ---------------- the translation table ----------------

        [TestCase(1, GamepadButton.West)]
        [TestCase(2, GamepadButton.South)]
        [TestCase(3, GamepadButton.East)]
        [TestCase(4, GamepadButton.North)]
        [TestCase(5, GamepadButton.LeftShoulder)]
        [TestCase(6, GamepadButton.RightShoulder)]
        [TestCase(9, GamepadButton.Select)]
        [TestCase(10, GamepadButton.Start)]
        [TestCase(11, GamepadButton.LeftStick)]
        [TestCase(12, GamepadButton.RightStick)]
        public void DirectInputProfile_MapsNumberedButtonsInLogitechOrder(int hidButton, GamepadButton expected)
        {
            var snap = new PadSnapshot().WithButton(hidButton);
            var state = PadBridge.Translate(snap, new PadProfile());
            Assert.IsTrue((state.buttons & (1u << (int)expected)) != 0,
                $"HID button {hidButton} must land on {expected}");
            Assert.AreEqual(1u << (int)expected, state.buttons, "exactly one gamepad button may be down");
        }

        [Test]
        public void DirectInputProfile_TriggersAreTheFloatFields()
        {
            var lt = PadBridge.Translate(new PadSnapshot().WithButton(7), new PadProfile());
            var rt = PadBridge.Translate(new PadSnapshot().WithButton(8), new PadProfile());
            Assert.AreEqual(1f, lt.leftTrigger); Assert.AreEqual(0f, lt.rightTrigger);
            Assert.AreEqual(1f, rt.rightTrigger); Assert.AreEqual(0f, rt.leftTrigger);
            Assert.AreEqual(0u, lt.buttons, "a trigger is not a face button");
        }

        [Test]
        public void HatSwitch_DrivesTheDpad_AndStickPassesThrough()
        {
            var snap = new PadSnapshot { Stick = new Vector2(0.3f, -0.8f), Hat = new Vector2(1f, 0f), HasHat = true };
            var state = PadBridge.Translate(snap, new PadProfile());
            Assert.AreEqual(snap.Stick, state.leftStick);
            Assert.IsTrue((state.buttons & (1u << (int)GamepadButton.DpadRight)) != 0, "hat right → dpad right");
            Assert.IsFalse((state.buttons & (1u << (int)GamepadButton.DpadDown)) != 0,
                "with a hat present the stick must NOT also press the dpad");
        }

        [Test]
        public void PrecisionProfile_TheStickIsTheDpad()
        {
            // The Logitech Precision has no hat and no analog stick: X/Y ARE the d-pad.
            var profile = new PadProfile { StickIsDpad = true };
            var up = PadBridge.Translate(new PadSnapshot { Stick = new Vector2(0f, 1f) }, profile);
            var left = PadBridge.Translate(new PadSnapshot { Stick = new Vector2(-1f, 0f) }, profile);
            Assert.IsTrue((up.buttons & (1u << (int)GamepadButton.DpadUp)) != 0);
            Assert.IsTrue((left.buttons & (1u << (int)GamepadButton.DpadLeft)) != 0);
            Assert.AreEqual(new Vector2(0f, 1f), up.leftStick, "the stick still reads as a stick (Move binds both)");

            var plain = PadBridge.Translate(new PadSnapshot { Stick = new Vector2(0f, 1f) }, new PadProfile());
            Assert.AreEqual(0u, plain.buttons, "an analog pad's stick must not press the dpad");
        }

        [Test]
        public void Snapshot_ButtonBitsAreOneBased()
        {
            var s = new PadSnapshot().WithButton(1).WithButton(10);
            Assert.IsTrue(s.Button(1) && s.Button(10));
            Assert.IsFalse(s.Button(2) || s.Button(9) || s.Button(0) || s.Button(33));
            Assert.IsFalse(s.WithButton(10, false).Button(10));
        }

        // ---------------- profile lookup ----------------

        const string PrecisionLayout = @"{
            ""name"": ""LatticeEditModePrecision"",
            ""extend"": ""Joystick"",
            ""device"": { ""interface"": ""LatticeEditMode"", ""product"": ""Logitech.*Precision.*"" },
            ""controls"": [ { ""name"": ""button2"", ""layout"": ""Button"", ""format"": ""BIT"", ""offset"": 0, ""bit"": 5, ""sizeInBits"": 1 } ]
        }";

        [Test]
        public void ResolveProfile_KnowsTheLogitechPrecisionByProductName()
        {
            InputSystem.RegisterLayout(PrecisionLayout);
            InputDevice device = null;
            try
            {
                device = InputSystem.AddDevice(new InputDeviceDescription
                {
                    interfaceName = "LatticeEditMode",
                    product = "Logitech(R) Precision(TM) Gamepad",
                });
                Assert.IsInstanceOf<Joystick>(device, "the test device must be a generic joystick");
                var profile = PadBridge.ResolveProfile(device);
                Assert.AreEqual("LogitechPrecision", profile.Name);
                Assert.IsTrue(profile.StickIsDpad, "the Precision's X/Y axes are its d-pad");
                Assert.AreEqual(2, profile.South, "Logitech order: 2 is the bottom face button");
            }
            finally
            {
                if (device != null)
                    InputSystem.RemoveDevice(device);
                InputSystem.RemoveLayout("LatticeEditModePrecision");
            }
        }

        [Test]
        public void ResolveProfile_FallsBackToDirectInputOrder()
        {
            InputDevice device = null;
            try
            {
                device = InputSystem.AddDevice<Joystick>();
                var profile = PadBridge.ResolveProfile(device);
                Assert.AreEqual("DirectInput", profile.Name);
                Assert.IsFalse(profile.StickIsDpad, "an unknown pad keeps an analog stick analog");
            }
            finally
            {
                if (device != null)
                    InputSystem.RemoveDevice(device);
            }
        }

        // ---------------- the UI map ----------------

        [Test]
        public void UiActions_SpeakOnlyKeyboardPointerAndGamepad()
        {
            UiActions.Ensure();
            Assert.IsTrue(UiActions.Asset.enabled, "the map is enabled by UiActions, never by a module");
            var map = UiActions.Asset.FindActionMap("UI");
            Assert.IsNotNull(map);
            foreach (var name in new[] { "Navigate", "Submit", "Cancel", "Point", "Click", "ScrollWheel", "MiddleClick", "RightClick" })
                Assert.IsNotNull(map.FindAction(name), $"UI map lacks {name} (the module resolves by name)");
            var paths = map.bindings.Select(b => b.path).ToList();
            Assert.IsFalse(paths.Any(p => p.Contains("<Joystick>")),
                "a Joystick binding here is exactly the double-Submit the bridge exists to prevent");
            Assert.IsFalse(paths.Any(p => p.Contains("{Submit}") || p.Contains("{Cancel}")),
                "usage wildcards pick up joystick button 1 as Submit");
            CollectionAssert.Contains(paths, "<Gamepad>/buttonSouth");
            CollectionAssert.Contains(paths, "<Gamepad>/buttonEast");
            CollectionAssert.Contains(paths, "<Gamepad>/leftStick");
            CollectionAssert.Contains(paths, "<Gamepad>/dpad");
            CollectionAssert.Contains(paths, "<Keyboard>/enter");
            CollectionAssert.Contains(paths, "<Keyboard>/escape");
            CollectionAssert.Contains(paths, "<Mouse>/position");
            CollectionAssert.Contains(paths, "<Mouse>/leftButton");
        }

        [Test]
        public void MenuPageActions_HaveGlyphsOnBothDevices()
        {
            Assert.AreEqual("pad_lb", PromptService.GlyphName("<Gamepad>/leftShoulder"));
            Assert.AreEqual("pad_rb", PromptService.GlyphName("<Gamepad>/rightShoulder"));
            Assert.AreEqual("key_q", PromptService.GlyphName("<Keyboard>/q"));
            Assert.AreEqual("key_e", PromptService.GlyphName("<Keyboard>/e"));
            var input = new GameInput();
            try
            {
                Assert.IsNotNull(input.Find("MenuPrev"));
                Assert.IsNotNull(input.Find("MenuNext"));
                Assert.IsTrue(input.Find("MenuPrev").bindings.Any(b => b.path == "<Gamepad>/leftShoulder"));
                Assert.IsTrue(input.Find("MenuNext").bindings.Any(b => b.path == "<Gamepad>/rightShoulder"));
            }
            finally
            {
                input.Dispose();
            }
        }

        // ---------------- source law ----------------

        [Test]
        public void NoSceneBuilderMakesAnEventSystemWithoutTheInputSystemModule()
        {
            // PadSupport re-points every InputSystemUIInputModule at UiActions on scene load;
            // a StandaloneInputModule would silently keep the legacy Input Manager path,
            // which this project has switched off (activeInputHandler = 1).
            string root = Path.Combine(Application.dataPath, "_Project", "Scripts");
            var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => File.ReadAllText(f).Contains("StandaloneInputModule"))
                .Select(Path.GetFileName)
                .ToList();
            Assert.IsEmpty(offenders, "StandaloneInputModule is the legacy path:\n  " + string.Join("\n  ", offenders));
        }
    }
}
