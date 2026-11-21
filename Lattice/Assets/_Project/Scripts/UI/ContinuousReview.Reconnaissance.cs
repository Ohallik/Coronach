using UnityEngine.InputSystem;

namespace Lattice.UI
{
    // Reconnaissance (D290): a replay started with -quality-reconnaissance finds out where a route stops while other work goes on at the same machine.
    // It is never evidence of a pass: its run.json says "reconnaissance", its "valid" is always false, and only another reconnaissance run may
    // resume from its saves. The focus-checked replay is untouched and is still the only thing that can pass a route.
    public sealed partial class ContinuousReview
    {
        bool reconnaissance;

        /// <summary>The driver's pad goes on reaching the game while its window is behind another, and no other device does. By default the input system
        /// switches a made-up pad off the moment the window loses focus (a conversation sat on its first line for minutes on November 14); a real pad or
        /// keyboard would then be heard from behind whatever else is being used, so everything but the driver's own pad is switched off.</summary>
        void Reconnoitre()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var device in InputSystem.devices) Deafen(device);
            InputSystem.onDeviceChange += (device, change) =>
            {
                if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected || change == InputDeviceChange.Enabled) Deafen(device);
            };
        }

        void Deafen(InputDevice device)
        {
            if (device != null && device != pad && device.enabled) InputSystem.DisableDevice(device);
        }
    }
}
