using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Lattice.Core
{
    /// <summary>
    /// The UI action map every InputSystemUIInputModule in the game runs on, built in code
    /// like GameInput (diffs cleanly, no importer). It replaces the Input System package's
    /// DefaultInputActions that the scene builders serialized into 108 EventSystems:
    /// those bind Submit to <c>*/{Submit}</c>, which on a generic HID joystick is its
    /// button 1 — the LEFT face button on every Logitech pad — so a bridged pad would
    /// confirm on X as well as A. This map speaks only Keyboard, Mouse/Pen/Touch and
    /// Gamepad; <see cref="PadBridge"/> makes every joystick a Gamepad first.
    ///
    /// Keyboard parity with the defaults is deliberate: WASD/arrows navigate, Enter
    /// submits, Escape cancels. The one addition is numpad Enter.
    /// </summary>
    public static class UiActions
    {
        /// <summary>Navigation auto-repeat while a direction is held (seconds). Unified
        /// with the battle menus' feel (BattleUi.NavAxis) so menus do not change gait.</summary>
        public const float RepeatDelay = 0.4f;
        public const float RepeatRate = 0.11f;

        public static InputActionAsset Asset { get; private set; }
        public static InputAction Navigate { get; private set; }
        public static InputAction Submit { get; private set; }
        public static InputAction Cancel { get; private set; }
        public static InputAction Point { get; private set; }
        public static InputAction Click { get; private set; }
        public static InputAction RightClick { get; private set; }
        public static InputAction MiddleClick { get; private set; }
        public static InputAction ScrollWheel { get; private set; }

        static InputActionReference _navigateRef, _submitRef, _cancelRef, _pointRef, _clickRef,
            _rightClickRef, _middleClickRef, _scrollRef;

        public static bool Ready => Asset != null;

        public static void Ensure()
        {
            if (Asset != null)
                return;
            Asset = ScriptableObject.CreateInstance<InputActionAsset>();
            Asset.name = "LatticeUi";
            Asset.hideFlags = HideFlags.HideAndDontSave;
            // Map and action names match DefaultInputActions so a module whose serialized
            // references still point at the package asset re-resolves by name when the
            // asset is swapped (InputSystemUIInputModule.actionsAsset does that lookup).
            var map = Asset.AddActionMap("UI");

            Navigate = map.AddAction("Navigate", InputActionType.PassThrough);
            Navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");
            Navigate.AddBinding("<Gamepad>/leftStick");
            Navigate.AddBinding("<Gamepad>/dpad");

            Submit = map.AddAction("Submit", InputActionType.Button);
            Submit.AddBinding("<Keyboard>/enter");
            Submit.AddBinding("<Keyboard>/numpadEnter");
            Submit.AddBinding("<Gamepad>/buttonSouth");

            Cancel = map.AddAction("Cancel", InputActionType.Button);
            Cancel.AddBinding("<Keyboard>/escape");
            Cancel.AddBinding("<Gamepad>/buttonEast");

            Point = map.AddAction("Point", InputActionType.PassThrough);
            Point.AddBinding("<Mouse>/position");
            Point.AddBinding("<Pen>/position");
            Point.AddBinding("<Touchscreen>/touch*/position");

            Click = map.AddAction("Click", InputActionType.PassThrough);
            Click.AddBinding("<Mouse>/leftButton");
            Click.AddBinding("<Pen>/tip");
            Click.AddBinding("<Touchscreen>/touch*/press");

            ScrollWheel = map.AddAction("ScrollWheel", InputActionType.PassThrough);
            ScrollWheel.AddBinding("<Mouse>/scroll");

            MiddleClick = map.AddAction("MiddleClick", InputActionType.PassThrough);
            MiddleClick.AddBinding("<Mouse>/middleButton");

            RightClick = map.AddAction("RightClick", InputActionType.PassThrough);
            RightClick.AddBinding("<Mouse>/rightButton");

            // Enabled here and never by a module: the module only disables actions it
            // enabled itself, so a dying scene's EventSystem cannot switch the map off
            // under the next scene's.
            Asset.Enable();
        }

        /// <summary>Point one module at the shared map (idempotent).</summary>
        public static void Attach(InputSystemUIInputModule module)
        {
            if (module == null)
                return;
            Ensure();
            if (module.actionsAsset != Asset)
                module.actionsAsset = Asset;
            module.point = Ref(ref _pointRef, Point);
            module.move = Ref(ref _navigateRef, Navigate);
            module.submit = Ref(ref _submitRef, Submit);
            module.cancel = Ref(ref _cancelRef, Cancel);
            module.leftClick = Ref(ref _clickRef, Click);
            module.middleClick = Ref(ref _middleClickRef, MiddleClick);
            module.rightClick = Ref(ref _rightClickRef, RightClick);
            module.scrollWheel = Ref(ref _scrollRef, ScrollWheel);
            module.trackedDevicePosition = null;
            module.trackedDeviceOrientation = null;
            module.moveRepeatDelay = RepeatDelay;
            module.moveRepeatRate = RepeatRate;
        }

        /// <summary>Every module currently loaded (active or not). Returns how many.</summary>
        public static int AttachAll()
        {
            var modules = Object.FindObjectsByType<InputSystemUIInputModule>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var module in modules)
                Attach(module);
            return modules.Length;
        }

        public static bool IsAttached(InputSystemUIInputModule module) =>
            module != null && Asset != null && module.actionsAsset == Asset;

        static InputActionReference Ref(ref InputActionReference cache, InputAction action)
        {
            if (cache == null || cache.action != action)
            {
                cache = InputActionReference.Create(action);
                cache.hideFlags = HideFlags.HideAndDontSave;
            }
            return cache;
        }
    }
}
