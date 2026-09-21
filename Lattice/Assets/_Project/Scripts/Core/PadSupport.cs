using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lattice.Core
{
    /// <summary>
    /// The one switch for controller support. Installs before the first scene loads and
    /// stays for the process:
    ///  - <see cref="PadBridge"/> turns generic HID joysticks (DirectInput pads) into Gamepads;
    ///  - <see cref="UiActions"/> replaces every scene EventSystem's default UI map;
    ///  - the mouse cursor hides while the pad is the active device (PromptService) and
    ///    returns the moment the mouse moves or a key is pressed.
    /// Nothing here depends on Game.Ensure, so the title screen, tests and -loadscene
    /// runs all get the same behaviour.
    /// </summary>
    public static class PadSupport
    {
        static bool _installed;

        public static bool Installed => _installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() => Install();

        public static void Install()
        {
            if (_installed)
                return;
            _installed = true;
            PadBridge.Install();
            UiActions.Ensure();
            int attached = UiActions.AttachAll();
            SceneManager.sceneLoaded += OnSceneLoaded;
            PromptService.Changed += ApplyCursor;
            Application.quitting += Uninstall;
            ApplyCursor();
            Debug.Log($"[Lattice] PAD_SUPPORT_OK modules={attached} bridged={PadBridge.LinkCount}");
        }

        public static void Uninstall()
        {
            if (!_installed)
                return;
            _installed = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            PromptService.Changed -= ApplyCursor;
            Application.quitting -= Uninstall;
            PadBridge.Uninstall();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            int attached = UiActions.AttachAll();
            Debug.Log($"[Lattice] PAD_UI_ATTACH scene={scene.name} modules={attached}");
        }

        /// <summary>The pointer is a keyboard-and-mouse affordance: it hides while the pad
        /// speaks and comes straight back on any mouse movement or click.</summary>
        static void ApplyCursor()
        {
            if (Application.isBatchMode)
                return;
            Cursor.visible = PromptService.Device != PromptDevice.Gamepad;
        }
    }
}
