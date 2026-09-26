using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.Data;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Lattice.UI
{
    /// <summary>Opt-in continuous ordinary-input replay, with no simulation pauses
    /// or direct actor/combat calls. Capture runs are separate from timing runs.</summary>
    [DefaultExecutionOrder(2000)]
    public sealed class ContinuousReview : MonoBehaviour
    {
        struct Sample
        {
            public double elapsed;
            public float ms, gameDelta, health, partnerHealth, reportedSpeed, pelvisYaw, chestYaw, animationTime;
            public Vector3 player, camera, forward;
            public string scene, hero, clip, prompt, speaker;
            public BodyForm form;
            public ActorState state;
            public ActorState partnerState;
            public int step, collections, lines;
            public long allocation, mainThread, renderThread, batches, memory;
            public bool focus, paused, blocked;
        }
        [Serializable] sealed class Report
        {
            public string route, sourceRevision, input = "continuous agent-directed virtual gamepad replay", build, unity, cpu, gpu, quality, gpuTiming = "UNAVAILABLE", vrr = "UNVERIFIED";
            public int width, height, frameCap, vSync, samples;
            public double refreshHz, seconds;
            public bool captured, valid;
            public string[] failures;
        }
        readonly List<Sample> samples = new(45000);
        readonly List<string> failures = new();
        readonly Dictionary<Animator, Transform[]> bones = new();
        readonly System.Diagnostics.Stopwatch clock = new();
        QualityRoute route;
        Gamepad pad;
        GamepadState held;
        QualityStep step;
        int stepIndex;
        bool recording, arrived;
        double stepStart, previous;
        string folder;
        ProfilerRecorder allocations, mainThread, renderThread, batches, memory;
        QualityCapture capture;
        Vector3 measuredMotion;
        bool arrows;

        public static QualityRoute ReadRoute()
        {
            string save = DevArgs.Value("-savepath");
            if (string.IsNullOrWhiteSpace(save)) throw new InvalidOperationException("Quality replay requires an explicit isolated -savepath");
            var result = JsonUtility.FromJson<QualityRoute>(File.ReadAllText(DevArgs.Value("-quality-route")));
            if (result?.steps == null || result.steps.Length == 0 || string.IsNullOrEmpty(result.scene)) throw new InvalidOperationException("Empty quality route");
            foreach (var s in result.steps)
                if (string.IsNullOrEmpty(s.name) || s.seconds <= 0 || s.seconds > 180) throw new InvalidOperationException("Invalid quality step");
            return result;
        }
        IEnumerator Start()
        {
            route = ReadRoute();
            folder = Path.GetFullPath(DevArgs.Value("-quality-output"));
            arrows = DevArgs.Has("-quality-arrows");
            Directory.CreateDirectory(folder);
            pad = InputSystem.AddDevice<Gamepad>("CoronachContinuousReview");
            InputSystem.onBeforeUpdate += InputUpdate;
            while (PartyController.Current == null || SceneFlow.Current.Loading) yield return null;
            Application.targetFrameRate = route.frameCap;
            QualitySettings.vSyncCount = 0;
            yield return new WaitForSecondsRealtime(route.settleSeconds);
            if (DialogueSystem.Current != null) DialogueSystem.Current.AutoAdvance = false;
            allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            memory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            renderThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread");
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            if (DevArgs.Has("-quality-ffmpeg"))
            {
                capture = Camera.main.gameObject.AddComponent<QualityCapture>();
                capture.Begin(folder, DevArgs.Value("-quality-ffmpeg"));
            }
            clock.Start(); recording = true;
            Debug.Log("QUALITY_REPLAY_BEGIN " + route.name);
            for (stepIndex = 0; stepIndex < route.steps.Length; stepIndex++)
            {
                step = route.steps[stepIndex]; stepStart = clock.Elapsed.TotalSeconds; arrived = false;
                Debug.Log("QUALITY_CHECKPOINT_BEGIN " + stepIndex + " " + step.name);
                while (clock.Elapsed.TotalSeconds - stepStart < step.seconds)
                {
                    if (!string.IsNullOrEmpty(step.until) && Condition(step.until)) break;
                    yield return null;
                }
                ValidateStep();
                if (capture != null) ScreenCapture.CaptureScreenshot(Path.Combine(folder, stepIndex.ToString("D3") + ".png"));
            }
            recording = false; held = default;
            yield return null;
            if (capture != null) yield return capture.Finish();
            WriteEvidence();
            Debug.Log(failures.Count == 0 ? "QUALITY_REPLAY_OK " + route.name : "QUALITY_REPLAY_REJECTED " + string.Join("; ", failures));
            Application.Quit(failures.Count == 0 ? 0 : 1);
        }
        void InputUpdate()
        {
            held = default;
            if (recording && step != null && PartyController.Current != null)
            {
                Vector2 direction = new(step.x, step.y);
                if (step.navigate)
                {
                    var actor = PartyController.Current.Active;
                    Vector3 goal = step.approachPartner && PartyController.Current.members.Length > 1
                        ? PartyController.Current.members[1-PartyController.Current.index].transform.position : step.point;
                    Vector3 difference = goal - actor.transform.position; difference.y = 0;
                    if (difference.magnitude <= step.tolerance) arrived = true;
                    if (!arrived)
                    {
                        var local = Quaternion.Euler(0, -ZoneController.Current.definition.cameraProfile.yaw, 0) * difference.normalized;
                        direction = new Vector2(local.x, local.z) * step.magnitude;
                    }
                }
                held = new GamepadState { leftStick = Vector2.ClampMagnitude(direction, 1), leftTrigger = step.leftTrigger, rightTrigger = step.rightTrigger };
                bool press = step.pulseSeconds <= 0 || (clock.Elapsed.TotalSeconds - stepStart) % step.pulseSeconds < .1;
                if (press && step.buttons != null) foreach (var button in step.buttons)
                {
                    if (!Enum.TryParse<GamepadButton>(button, true, out var parsed)) throw new InvalidOperationException("Invalid replay button " + button);
                    held = held.WithButton(parsed);
                }
                pad.MakeCurrent();
            }
            if (pad != null) InputSystem.QueueStateEvent(pad, held);
        }
        void LateUpdate()
        {
            if (!recording || PartyController.Current == null) return;
            var actor = PartyController.Current.Active;
            double now = clock.Elapsed.TotalSeconds;
            if (samples.Count > 0 && samples[^1].hero == actor.character)
                measuredMotion = (actor.transform.position - samples[^1].player) / Mathf.Max(.001f, (float)(now - previous));
            var animator = actor.GetComponentInChildren<Animator>();
            float pelvis = float.NaN, chest = float.NaN, animationTime = 0;
            if (animator != null && animator.isHuman)
            {
                if (!bones.TryGetValue(animator, out var b))
                {
                    b = new[] { animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg), animator.GetBoneTransform(HumanBodyBones.RightUpperLeg), animator.GetBoneTransform(HumanBodyBones.LeftUpperArm), animator.GetBoneTransform(HumanBodyBones.RightUpperArm) };
                    bones[animator] = b;
                }
                pelvis = SkeletalYaw(b[0], b[1]); chest = SkeletalYaw(b[2], b[3]);
                animationTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            }
            var driver = actor.GetComponentInChildren<GeneratedAnimator>();
            var party = PartyController.Current;
            var partner = party.members.Length > 1 ? party.members[1-party.index] : null;
            samples.Add(new Sample {
                elapsed = now, ms = (float)((now - previous) * 1000), gameDelta = Time.deltaTime,
                player = actor.transform.position, camera = Camera.main.transform.position, forward = actor.transform.forward,
                reportedSpeed = actor.motor.Velocity.magnitude, health = actor.Health.integrity,
                partnerHealth = partner != null ? partner.Health.integrity : -1,
                partnerState = partner != null ? partner.State : ActorState.Idle,
                pelvisYaw = pelvis, chestYaw = chest, animationTime = animationTime,
                scene = SceneFlow.Current.Zone, hero = actor.character, form = actor.GetComponent<FormController>().Current,
                state = actor.State, clip = driver != null ? driver.CurrentAnimation : "ship",
                prompt = PromptService.Interaction?.prompt ?? "", speaker = DialogueSystem.Current?.Speaker ?? "",
                lines = DialogueSystem.Current?.LinesPresented ?? 0, step = stepIndex,
                collections = GC.CollectionCount(0), allocation = allocations.Valid ? allocations.LastValue : -1,
                mainThread = mainThread.Valid ? mainThread.LastValue : -1, renderThread = renderThread.Valid ? renderThread.LastValue : -1,
                batches = batches.Valid ? batches.LastValue : -1, memory = memory.Valid ? memory.LastValue : -1,
                focus = Application.isFocused, paused = GameTime.Paused, blocked = GameInput.Current.Blocked
            });
            previous = now;
        }
        void OnGUI()
        {
            if (!arrows || !recording || samples.Count == 0) return;
            var sample = samples[^1];
            GUI.Box(new Rect(480,10,610,66), "DIAGNOSTIC: white actor / green travel / yellow pelvis / cyan shoulders\nBone spans are independent of heading; calibration pending visual review.");
            Vector3 origin = sample.player + Vector3.up * 1.05f;
            Arrow(origin, sample.forward, Color.white);
            Arrow(origin, measuredMotion.normalized, Color.green);
            if (!float.IsNaN(sample.pelvisYaw)) Arrow(origin, Quaternion.Euler(0,sample.pelvisYaw,0) * Vector3.forward, Color.yellow);
            if (!float.IsNaN(sample.chestYaw)) Arrow(origin, Quaternion.Euler(0,sample.chestYaw,0) * Vector3.forward, Color.cyan);
        }
        static void Arrow(Vector3 origin, Vector3 direction, Color color)
        {
            if (direction.sqrMagnitude < .01f) return;
            Vector3 a = Camera.main.WorldToScreenPoint(origin), b = Camera.main.WorldToScreenPoint(origin + direction * 2.5f);
            Vector2 p = new(a.x, Screen.height-a.y), q = new(b.x, Screen.height-b.y);
            var saved = GUI.matrix; var tint = GUI.color; GUI.color = color;
            float angle = Mathf.Atan2(q.y-p.y,q.x-p.x)*Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle,p);
            GUI.DrawTexture(new Rect(p.x,p.y,Vector2.Distance(p,q),3),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(p.x+Vector2.Distance(p,q)-7,p.y-4,7,10),Texture2D.whiteTexture);
            GUI.matrix=saved; GUI.color=tint;
        }
        static float SkeletalYaw(Transform left, Transform right)
        {
            if (left == null || right == null) return float.NaN;
            Vector3 forward = Vector3.Cross(right.position - left.position, Vector3.up);
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }
        void ValidateStep()
        {
            if (!string.IsNullOrEmpty(step.until) && !Condition(step.until)) failures.Add(step.name + ": condition timeout " + step.until);
            if (step.navigate && !arrived) failures.Add(step.name + ": navigation checkpoint missed");
            string expected = string.IsNullOrEmpty(step.expectedScene) ? route.scene : step.expectedScene;
            if (SceneFlow.Current.Zone != expected) failures.Add(step.name + ": scene " + SceneFlow.Current.Zone + " expected " + expected);
            if (!string.IsNullOrEmpty(step.expectedCharacter) && PartyController.Current.Active.character != step.expectedCharacter) failures.Add(step.name + ": character mismatch");
            if (step.expectedUi == "dialogue" && !DialogueSystem.Current.Running) failures.Add(step.name + ": dialogue did not open");
            if (step.expectedUi == "world" && GameInput.Current.Blocked) failures.Add(step.name + ": UI did not close");
            if (step.expectedUi == "shop" && FindFirstObjectByType<ShopUi>()?.IsOpen != true) failures.Add(step.name + ": shop did not open");
        }
        static bool Condition(string name)
        {
            var p = PartyController.Current;
            if (p == null || p.members.Length < 2) return false;
            return name switch {
                "partnerDown" => !p.members[1-p.index].Health.Alive && p.Active.Health.Alive,
                "partyAlive" => p.members[0].Health.Alive && p.members[1].Health.Alive,
                _ => throw new InvalidOperationException("Unknown replay condition " + name)
            };
        }
        void WriteEvidence()
        {
            if (samples.Count < 2) failures.Add("missing samples");
            foreach (var s in samples) if (!s.focus) { failures.Add("focus lost"); break; }
            foreach (var s in samples) if (s.paused) { failures.Add("simulation paused"); break; }
            if (Screen.width != 1920 || Screen.height != 1080) failures.Add("wrong resolution");
            if (capture != null && !string.IsNullOrEmpty(capture.Failure)) failures.Add(capture.Failure);
            using (var writer = new StreamWriter(Path.Combine(folder, "frames.csv")))
            {
                writer.WriteLine("elapsed,ms,step,scene,hero,form,state,clip,animationTime,x,y,z,cameraX,cameraY,cameraZ,forwardX,forwardZ,pelvisYaw,chestYaw,reportedSpeed,integrity,focus,paused,blocked,gameDelta,gcCollections,gcBytes,mainThreadNs,renderThreadNs,batches,memoryBytes,dialogueLines,prompt,speaker,partnerHealth,partnerState");
                foreach (var s in samples) writer.WriteLine(FormattableString.Invariant($"{s.elapsed:F6},{s.ms:F4},{s.step},{s.scene},{s.hero},{s.form},{s.state},{s.clip},{s.animationTime:F4},{s.player.x:F5},{s.player.y:F5},{s.player.z:F5},{s.camera.x:F5},{s.camera.y:F5},{s.camera.z:F5},{s.forward.x:F5},{s.forward.z:F5},{s.pelvisYaw:F3},{s.chestYaw:F3},{s.reportedSpeed:F4},{s.health:F1},{s.focus},{s.paused},{s.blocked},{s.gameDelta:F6},{s.collections},{s.allocation},{s.mainThread},{s.renderThread},{s.batches},{s.memory},{s.lines},\"{s.prompt.Replace("\"", "\"\"")}\",{s.speaker},{s.partnerHealth:F1},{s.partnerState}"));
            }
            var report = new Report { route = route.name, sourceRevision = route.sourceRevision, build = Debug.isDebugBuild ? "Development" : "Release", unity = Application.unityVersion,
                cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                width = Screen.width, height = Screen.height, frameCap = Application.targetFrameRate, vSync = QualitySettings.vSyncCount,
                refreshHz = Screen.currentResolution.refreshRateRatio.value, samples = samples.Count, seconds = samples.Count > 0 ? samples[^1].elapsed : 0,
                captured = capture != null, failures = failures.ToArray(), valid = failures.Count == 0 };
            File.WriteAllText(Path.Combine(folder, "run.json"), JsonUtility.ToJson(report, true));
            File.WriteAllText(Path.Combine(folder, "route.json"), JsonUtility.ToJson(route, true));
        }
        void OnDestroy()
        {
            InputSystem.onBeforeUpdate -= InputUpdate;
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            allocations.Dispose(); mainThread.Dispose(); renderThread.Dispose(); batches.Dispose(); memory.Dispose();
        }
    }
}
