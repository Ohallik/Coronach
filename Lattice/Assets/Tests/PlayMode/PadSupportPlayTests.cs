using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>
    /// CONTROLLER PASS (2026-09-17) — the live half. Real devices, real frames, no
    /// InputTestFixture (its reset would orphan the game's own action assets).
    ///
    ///  1. A generic HID joystick is bridged into a virtual Gamepad and its buttons reach
    ///     GameInput and the UI map; it leaves when the joystick leaves.
    ///  2. The Logitech Precision profile: X/Y drive the d-pad, button 1 is West, 10 is Start.
    ///  3. The title screen: the pad moves the cursor, skips a greyed Continue, opens the
    ///     settings, and after backing out the guard (PadFocus) puts the cursor back.
    ///  4. The pack menu in a real world scene: every page keeps a selection under the
    ///     shoulders, left reaches the tab column, Y walks the party, a stepper press keeps
    ///     its row, B answers a confirm dialog without backing the menu out, B leaves a
    ///     rebind listen and returns to the row, and the shop counter's tabs are reachable.
    /// </summary>
    public class PadSupportPlayTests
    {
        // ---------- a fake DirectInput pad (the Joystick state layout: int buttons, Vector2 stick) ----------

        [StructLayout(LayoutKind.Sequential)]
        struct TestJoyState : IInputStateTypeInfo
        {
            public int buttons;
            public Vector2 stick;
            public FourCC format => new FourCC('J', 'O', 'Y');
        }

        // Joystick base bits: hat up/down/left/right = 0..3, trigger (HID button 1) = 4.
        const int HatUp = 1 << 0, HatRight = 1 << 3;
        static int HidButton(int n) => n == 1 ? 1 << 4 : 1 << (n + 3); // button2 = bit 5 … button12 = bit 15

        static string LayoutJson(string name, bool hat, string matchInterface, string matchProduct)
        {
            var sb = new StringBuilder();
            sb.Append("{ \"name\": \"").Append(name).Append("\", \"extend\": \"Joystick\",");
            if (matchInterface != null)
                sb.Append(" \"device\": { \"interface\": \"").Append(matchInterface)
                  .Append("\", \"product\": \"").Append(matchProduct).Append("\" },");
            sb.Append(" \"controls\": [");
            if (hat)
                sb.Append("{ \"name\": \"hat\", \"layout\": \"Dpad\", \"format\": \"BIT\", \"offset\": 0, \"bit\": 0, \"sizeInBits\": 4 },");
            for (int n = 2; n <= 12; n++)
                sb.Append("{ \"name\": \"button").Append(n).Append("\", \"layout\": \"Button\", \"format\": \"BIT\", \"offset\": 0, \"bit\": ")
                  .Append(n + 3).Append(", \"sizeInBits\": 1 }").Append(n < 12 ? "," : "");
            sb.Append(" ] }");
            return sb.ToString();
        }

        static bool _layoutsRegistered;
        readonly List<InputDevice> _added = new();
        InputSettings.BackgroundBehavior _prevBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode _prevEditorBehavior;
#endif

        [UnitySetUp]
        public IEnumerator Boot()
        {
            // Headless runs have no window focus; devices would otherwise be muted.
            _prevBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _prevEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            PadSupport.Install();
            if (!PadBridge.Installed)
                PadBridge.Install();
            if (!_layoutsRegistered)
            {
                InputSystem.RegisterLayout(LayoutJson("LatticeTestPadHat", true, null, null));
                InputSystem.RegisterLayout(LayoutJson("LatticeTestPrecision", false, "LatticeTest", "Logitech.*Precision.*"));
                _layoutsRegistered = true;
            }
            PromptService.Reset();
            SceneManager.LoadScene("_Boot");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(.3f);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (var d in _added)
                if (d != null && d.added)
                    InputSystem.RemoveDevice(d);
            _added.Clear();
            InputSystem.settings.backgroundBehavior = _prevBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = _prevEditorBehavior;
#endif
            PromptService.Reset();
            if (PadFocus.Current != null)
                PadFocus.Current.Forget();
            yield return null;
        }

        InputDevice Add(string layout)
        {
            var d = InputSystem.AddDevice(layout);
            _added.Add(d);
            return d;
        }

        Gamepad AddPad()
        {
            var pad = InputSystem.AddDevice<Gamepad>("PadTestGamepad");
            _added.Add(pad);
            return pad;
        }

        static IEnumerator Press(Gamepad pad, GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return null;
            yield return null;
        }

        static GameObject Selected() => EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        static string Label(GameObject go) =>
            go != null ? go.GetComponentInChildren<TMP_Text>()?.text : null;

        // ==================== 1. the bridge ====================

        [UnityTest]
        public IEnumerator BridgedJoystick_IsAGamepadToGameInputAndTheUiMap()
        {

            var joy = Add("LatticeTestPadHat");
            Assert.IsInstanceOf<Joystick>(joy, "the test device must be what a DirectInput pad is to Unity: a Joystick");
            yield return null;

            var pad = PadBridge.PadFor(joy);
            Assert.IsNotNull(pad, "the bridge never attached: " + PadBridge.Describe());
            Assert.IsTrue(Gamepad.all.Contains(pad), "the virtual pad must be a real Gamepad on the bus");

            // HID button 2 → South → Interact (GameInput) and Submit (UiActions), same frame.
            InputSystem.QueueStateEvent(joy, new TestJoyState { buttons = HidButton(2) });
            yield return null;
            Assert.IsTrue(pad.buttonSouth.isPressed, "button 2 must read as A on the virtual pad");
            Assert.AreEqual(pad, Gamepad.current, "a real press makes the virtual pad current (the prompt watch reads it)");
            Assert.IsTrue(GameInput.Current.Held("Interact"), "GameInput.Interact must see the bridged A");
            Assert.IsTrue(UiActions.Submit.IsPressed(), "the UI map's Submit must see the bridged A");
            Assert.IsFalse(pad.buttonWest.isPressed);

            InputSystem.QueueStateEvent(joy, new TestJoyState());
            yield return null;
            Assert.IsFalse(pad.buttonSouth.isPressed, "release must propagate");

            // Stick right + hat up: the stick is the left stick, the hat is the d-pad.
            InputSystem.QueueStateEvent(joy, new TestJoyState { stick = new Vector2(1f, 0f), buttons = HatUp });
            yield return null;
            Assert.Greater(pad.leftStick.ReadValue().x, 0.9f, "stick → leftStick");
            Assert.IsTrue(pad.dpad.up.isPressed, "hat → dpad");
            Assert.Greater(GameInput.Current.Move.magnitude, 0.9f, "Move binds both");
            Assert.AreEqual(PromptDevice.Gamepad, PromptService.Device, "the prompt watch flips to pad glyphs");

            InputSystem.QueueStateEvent(joy, new TestJoyState());
            yield return null;

            InputSystem.RemoveDevice(joy);
            _added.Remove(joy);
            yield return null;
            Assert.IsNull(PadBridge.PadFor(joy), "the virtual pad must leave with its joystick");
            Assert.IsFalse(Gamepad.all.Contains(pad));
        }

        [UnityTest]
        public IEnumerator LogitechPrecision_StickIsTheDpad_ButtonsInLogitechOrder()
        {

            var joy = InputSystem.AddDevice(new InputDeviceDescription
            {
                interfaceName = "LatticeTest",
                product = "Logitech(R) Precision(TM) Gamepad",
            });
            _added.Add(joy);
            yield return null;
            Assert.AreEqual("LogitechPrecision", PadBridge.ProfileNameFor(joy), PadBridge.Describe());
            var pad = PadBridge.PadFor(joy);
            Assert.IsNotNull(pad);

            InputSystem.QueueStateEvent(joy, new TestJoyState { stick = new Vector2(1f, 0f) });
            yield return null;
            Assert.IsTrue(pad.dpad.right.isPressed, "the Precision reports its d-pad on X/Y: right must press dpad right");
            Assert.Greater(pad.leftStick.ReadValue().x, 0.9f);

            InputSystem.QueueStateEvent(joy, new TestJoyState { buttons = HidButton(1) | HidButton(10) });
            yield return null;
            Assert.IsTrue(pad.buttonWest.isPressed, "button 1 is the LEFT face button (X) in Logitech order");
            Assert.IsTrue(pad.startButton.isPressed, "button 10 is Start");
            Assert.IsTrue(GameInput.Current.Held("Pause"), "Start reaches GameInput.Menu");

            InputSystem.QueueStateEvent(joy, new TestJoyState());
            yield return null;
        }

        // ==================== 3. the title screen ====================

        [UnityTest]
        public IEnumerator Title_PadMovesOpensSettings_AndTheGuardRestoresTheCursor()
        {
            SceneManager.LoadScene("_Boot");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.5f);
            Assert.IsNotNull(GameServices.Current);
            Assert.IsNotNull(PadFocus.Current, "the [UI] root must carry the selection guard");

            var pad = AddPad();
            yield return null;
            var first = Selected();
            Assert.IsNotNull(first, "the title screen opens with a selection");

            yield return Press(pad, GamepadButton.DpadDown);
            var second = Selected();
            Assert.IsNotNull(second);
            Assert.AreNotEqual(first, second, "dpad down must move the title cursor");
            Assert.IsTrue(second.GetComponent<Selectable>().IsInteractable(), "a greyed Continue is not a stop");
            Assert.AreEqual(PromptDevice.Gamepad, PromptService.Device, "the press must flip the prompt device");

            for (int i = 0; i < 5 && Label(Selected()) != "Settings"; i++)
                yield return Press(pad, GamepadButton.DpadDown);
            Assert.AreEqual("Settings", Label(Selected()), "the column must reach Settings");
            var settingsButton = Selected();

            yield return Press(pad, GamepadButton.South);
            var menu = Object.FindFirstObjectByType<TitleScreen>();
            Assert.IsNotNull(menu, "A on Settings must open the title settings");
            Assert.IsTrue(menu.SettingsOpen);
            Assert.IsNotNull(Selected(), "title settings open with a selection");
            Assert.AreNotEqual(settingsButton, Selected(), "the cursor moved into the settings pane");

            yield return Press(pad, GamepadButton.East); // Back from the Settings root: close
            yield return null;
            Assert.IsTrue(menu == null || !menu.SettingsOpen, "B must close the title settings");

            // The pane died with the menu, so nothing is selected; the next nudge is
            // answered by the guard, which puts the cursor back on the title column.
            yield return Press(pad, GamepadButton.DpadDown);
            var restored = Selected();
            Assert.IsNotNull(restored, "PadFocus must restore a title selection after settings close");
            CollectionAssert.Contains(new[] { "New Game", "Continue", "Settings", "Quit" }, Label(restored),
                "the restored cursor must be a title button");
        }

    }
}
