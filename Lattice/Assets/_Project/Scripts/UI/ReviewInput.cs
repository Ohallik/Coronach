#if LATTICE_DEV || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Lattice.UI
{
    /// <summary>Opt-in, local review controller. Sends ordinary input, never gameplay commands.
    /// Simulation waits between decisions; this is not evidence of human real-time pad feel.</summary>
    public sealed class ReviewInput : MonoBehaviour
    {
        [Serializable] public sealed class Command
        {
            public int id;
            public float x, y, leftTrigger, rightTrigger, seconds = .15f;
            public string[] buttons;
        }
        [Serializable] sealed class Observation
        {
            public int id;
            public string scene, character, interaction, target;
            public Vector3 position, velocity;
            public float integrity, charge, thrust;
            public bool blocked;
        }
        string folder;
        Gamepad pad;
        GamepadState held;
        int lastId = -1;
        bool busy;
        bool gamePaused;

        void Start()
        {
            folder = Path.GetFullPath(DevArgs.Value("-review"));
            Directory.CreateDirectory(folder);
            pad = InputSystem.AddDevice<Gamepad>("LatticeReviewController");
            InputSystem.onBeforeUpdate += InputUpdate;
            StartCoroutine(Ready());
        }
        IEnumerator Ready()
        {
            busy = true;
            yield return new WaitForSecondsRealtime(2);
            gamePaused = GameTime.Paused;
            GameTime.Paused = true;
            yield return Capture(0);
            lastId = 0;
            busy = false;
            Debug.Log("REVIEW_INPUT_READY virtual-gamepad decision-paused");
        }
        void InputUpdate()
        {
            if (pad != null) InputSystem.QueueStateEvent(pad, held);
        }
        void Update()
        {
            if (busy) return;
            string path = Path.Combine(folder, "command.json");
            if (!File.Exists(path)) return;
            Command command;
            try { command = JsonUtility.FromJson<Command>(File.ReadAllText(path)); }
            catch (IOException) { return; }
            catch (ArgumentException) { return; }
            if (command == null || command.id <= lastId) return;
            lastId = command.id;
            StartCoroutine(Step(command));
        }
        IEnumerator Step(Command command)
        {
            busy = true;
            held = new GamepadState { leftStick = Vector2.ClampMagnitude(new Vector2(command.x, command.y), 1), leftTrigger = Mathf.Clamp01(command.leftTrigger), rightTrigger = Mathf.Clamp01(command.rightTrigger) };
            foreach (string button in command.buttons ?? Array.Empty<string>())
                if (Enum.TryParse<GamepadButton>(button, true, out var parsed)) held = held.WithButton(parsed);
                else Debug.LogError("REVIEW_INVALID_BUTTON " + button);
            pad.MakeCurrent();
            GameTime.Paused = gamePaused;
            yield return new WaitForSecondsRealtime(Mathf.Clamp(command.seconds, .08f, 5));
            held = default;
            // Deliver releases before capturing, including a rendered frame after the event.
            yield return null;
            yield return null;
            gamePaused = GameTime.Paused;
            GameTime.Paused = true;
            yield return Capture(command.id);
            busy = false;
        }
        IEnumerator Capture(int id)
        {
            yield return new WaitForEndOfFrame();
            var actor = PartyController.Current != null ? PartyController.Current.Active : null;
            var observation = new Observation { id = id, scene = ZoneController.Current != null ? ZoneController.Current.definition.id : "Title", blocked = GameInput.Current?.Blocked ?? false };
            if (actor != null)
            {
                observation.character = actor.character;
                observation.position = actor.transform.position;
                observation.velocity = actor.motor != null ? actor.motor.Velocity : Vector3.zero;
                observation.integrity = actor.Health.integrity;
                observation.charge = actor.charge;
                observation.thrust = actor.thrust;
                observation.interaction = PromptService.Interaction != null ? PromptService.Interaction.prompt : "";
                observation.target = actor.target != null ? actor.target.id : "";
            }
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder, id.ToString("D3") + ".png"), texture.EncodeToPNG());
            Destroy(texture);
            File.WriteAllText(Path.Combine(folder, id.ToString("D3") + ".json"), JsonUtility.ToJson(observation, true));
            Debug.Log("REVIEW_STEP " + id + " " + observation.scene + " " + observation.position);
        }
        void OnDestroy()
        {
            InputSystem.onBeforeUpdate -= InputUpdate;
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            GameTime.Paused = gamePaused;
        }
    }
}
#endif
