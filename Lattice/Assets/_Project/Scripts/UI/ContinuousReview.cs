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
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Lattice.UI
{
    /// <summary>Opt-in continuous ordinary-input replay, with no simulation pauses
    /// or direct actor/combat calls. Declared, observed menus may pause a traversal;
    /// timing runs always reject pauses. Capture runs are separate from timing runs.</summary>
    [DefaultExecutionOrder(2000)]
    public sealed class ContinuousReview : MonoBehaviour
    {
        struct Sample
        {
            public double elapsed;
            public float ms, gameDelta, health, partnerHealth, reportedSpeed, pelvisYaw, chestYaw, animationTime;
            public Vector3 player, camera, forward;
            public string scene, hero, clip, prompt, speaker, ui;
            public BodyForm form;
            public ActorState state;
            public ActorState partnerState;
            public int step, collections, lines;
            public long allocation, mainThread, renderThread, batches, memory, hudNs, hudBytes, audioVoices, sceneObjects, objects;
            public long activeCpu, activeRender, gpuWork, capWait;
            public ulong ftmTimestamp;
            public double ftmGpuMs, ftmCpuMs;
            public bool focus, paused, blocked;
        }
        [Serializable] sealed class Report
        {
            public string route, sourceRevision, input = "continuous agent-directed virtual gamepad replay", build, unity, cpu, gpu, quality, gpuTiming = "UNAVAILABLE", vrr = "UNVERIFIED";
            public int width, height, frameCap, vSync, samples;
            public double refreshHz, seconds;
            public bool captured, profiled, headroom, census, motion, valid;
            public bool frameTimingRequested, frameTimingEnabled;
            public ulong frameTimingCpuFrequency;
            public string[] failures;
            public double loadWaitSeconds, settleSeconds;
            public InteractionResponse[] interactions;
        }
        [Serializable] sealed class InteractionResponse
        {
            public int step;
            public string expectedUi;
            public double inputSeconds, visibleResponseMs=-1;
        }
        readonly List<Sample> samples = new(45000);
        readonly List<string> failures = new();
        readonly List<InteractionResponse> interactions=new();
        struct CensusSample {public double seconds,milliseconds;public string scene;public int objects,sources,playingSources,loopingSources;}
        readonly List<CensusSample> growth=new();
        bool census;double nextCensus;
        struct MotionSample
        {
            public double seconds;public int step;public string hero,form,clip;
            public float phase,stride,leftGroundY,rightGroundY;public bool transitioning,reverse,locked;
            public Vector3 leftHeel,leftToe,rightHeel,rightToe,target;
        }
        bool motion;
        readonly List<MotionSample> motionSamples=new();
        InteractionResponse interaction;
        DialoguePanel dialoguePanel;
        ShopUi shopUi;
        PauseMenu pauseMenu;
        DefeatPanel defeatPanel;
        TitleScreen titleScreen;
        ZoneController uiOwner;
        string uiScene;
        static string ActualScene => !string.IsNullOrEmpty(SceneFlow.Current?.Zone) ? SceneFlow.Current.Zone :
            SceneManager.GetSceneByName("Title").isLoaded ? "Title" : "_Boot";
        string ObserveUi()
        {
            if (SceneFlow.Current != null && SceneFlow.Current.Loading) return "loading";
            string scene = ActualScene;
            if (uiScene != scene || uiOwner != ZoneController.Current)
            {
                uiScene = scene; uiOwner = ZoneController.Current;
                // One discovery per loaded zone, never repeated absent-type searches
                // on every uncapped frame. All zone UI lives on ArenaRuntime's root.
                dialoguePanel = uiOwner != null ? uiOwner.GetComponent<DialoguePanel>() : null;
                shopUi = uiOwner != null ? uiOwner.GetComponent<ShopUi>() : null;
                pauseMenu = uiOwner != null ? uiOwner.GetComponent<PauseMenu>() : null;
                defeatPanel = uiOwner != null ? uiOwner.GetComponent<DefeatPanel>() : null;
                titleScreen = scene == "Title" ? FindFirstObjectByType<TitleScreen>() : null;
            }
            if (scene == "Title") return titleScreen == null ? "unavailable" : titleScreen.SavesOpen ? "saves" : titleScreen.SettingsOpen ? "settings" : "title";
            if (defeatPanel != null && defeatPanel.IsOpen) return "defeat";
            if (pauseMenu != null && pauseMenu.IsOpen) return pauseMenu.AtBench ? "bench" : "pause";
            if (shopUi != null && shopUi.IsOpen) return "shop";
            if (dialoguePanel != null && dialoguePanel.OptionsVisible) return "options";
            if (dialoguePanel != null && dialoguePanel.IsVisible) return "dialogue";
            return PartyController.Current != null && !GameInput.Current.Blocked ? "world" : "unavailable";
        }
        static bool UiMatches(string expected, string actual) => expected == actual || expected == "dialogue" && actual == "options";
        bool AllowedPause(Sample sample) => !headroom && sample.step >= 0 && sample.step < route.steps.Length &&
            (sample.ui == "bench" || sample.ui == "pause" || sample.ui == "defeat") && route.steps[sample.step].pauseUi == sample.ui;
        double loadWaitSeconds;
        readonly Dictionary<Animator, Transform[]> bones = new();
        readonly System.Diagnostics.Stopwatch clock = new();
        QualityRoute route;
        Gamepad pad;
        GamepadState held;
        QualityStep step;
        int stepIndex;
        bool recording, arrived, endedByCondition;
        Health stepTarget;
        double stepStart, previous;
        string folder;
        ProfilerRecorder allocations, mainThread, renderThread, batches, memory, audioVoices, sceneObjects, objects;
        ProfilerRecorder activeCpu,activeRender,gpuWork,capWait;
        readonly FrameTiming[] frameTimings = new FrameTiming[1];
        bool frameTimingEnabled;
        QualityCapture capture;
        double lastScreenshot=-1000;
        Vector3 measuredMotion;
        bool arrows, profiled, tracing, headroom;

        public static QualityRoute ReadRoute()
        {
            string save = DevArgs.Value("-savepath");
            if (string.IsNullOrWhiteSpace(save)) throw new InvalidOperationException("Quality replay requires an explicit isolated -savepath");
            var result = JsonUtility.FromJson<QualityRoute>(File.ReadAllText(DevArgs.Value("-quality-route")));
            if (result?.steps == null || result.steps.Length == 0 || string.IsNullOrEmpty(result.scene)) throw new InvalidOperationException("Empty quality route");
            if (result.scene == "Title" && (result.starterParty || !string.IsNullOrEmpty(result.loadout)))
                throw new InvalidOperationException("Title replay must use starterParty=false and no development loadout");
            foreach (var s in result.steps)
                if (string.IsNullOrEmpty(s.name) || s.seconds <= 0 || s.seconds > 180) throw new InvalidOperationException("Invalid quality step");
            return result;
        }
        IEnumerator Start()
        {
            route = ReadRoute();
            folder = Path.GetFullPath(DevArgs.Value("-quality-output"));
            arrows = DevArgs.Has("-quality-arrows");
            profiled = DevArgs.Has("-quality-profile");
            headroom = DevArgs.Has("-quality-headroom");
            census = DevArgs.Has("-quality-census");
            motion = DevArgs.Has("-quality-motion");
            Directory.CreateDirectory(folder);
            pad = InputSystem.AddDevice<Gamepad>("CoronachContinuousReview");
            InputSystem.onBeforeUpdate += InputUpdate;
            double loadStart=Time.realtimeSinceStartupAsDouble;
            while (SceneFlow.Current.Loading || (route.scene == "Title" ? !SceneManager.GetSceneByName("Title").isLoaded : PartyController.Current == null)) yield return null;
            loadWaitSeconds=Time.realtimeSinceStartupAsDouble-loadStart;
            // Ordinary reference-display routes measure the player's startup
            // policy. Otherwise this harness can hide a broken VSync default.
            // Only explicit headroom / alternate-rate diagnostics override it.
            if(route.frameCap!=60)
            {
                Application.targetFrameRate=route.frameCap;
                QualitySettings.vSyncCount=0;
            }
            yield return new WaitForSecondsRealtime(route.settleSeconds);
            if (DialogueSystem.Current != null) DialogueSystem.Current.AutoAdvance = false;
            var available=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(available);
            using(var catalog=new StreamWriter(Path.Combine(folder,"available-counters.txt")))
                foreach(var handle in available)
                {
                    var description=ProfilerRecorderHandle.GetDescription(handle);
                    catalog.WriteLine(description.Category.Name+" / "+description.Name+" / "+description.UnitType);
                }
            allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            memory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            renderThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread");
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            audioVoices = ProfilerRecorder.StartNew(ProfilerCategory.Audio, "Audio Voices");
            sceneObjects = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Scene Object Count");
            objects = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Object Count");
            if(headroom)
            {
                frameTimingEnabled = FrameTimingManager.IsFeatureEnabled();
                // Categories come from this player's available-counter catalog.
                // These FrameTiming counters exclude CPU presentation/cap waits;
                // the older Main Thread column deliberately retains those waits.
                activeCpu=ProfilerRecorder.StartNew(ProfilerCategory.Render,"CPU Main Thread Frame Time");
                activeRender=ProfilerRecorder.StartNew(ProfilerCategory.Render,"CPU Render Thread Frame Time");
                gpuWork=ProfilerRecorder.StartNew(ProfilerCategory.Render,"GPU Frame Time");
                capWait=ProfilerRecorder.StartNew(new ProfilerCategory("VSync"),"WaitForTargetFPS");
            }
            GameHud.MeasureCosts = profiled;
            if(profiled)
            {
                UnityEngine.Profiling.Profiler.logFile=Path.Combine(folder,"cpu-ui-render.raw");
                UnityEngine.Profiling.Profiler.enableBinaryLog=true;
                UnityEngine.Profiling.Profiler.enabled=true;
                tracing=true;
            }
            if (DevArgs.Has("-quality-ffmpeg"))
            {
                capture = Camera.main.gameObject.AddComponent<QualityCapture>();
                capture.Begin(folder, DevArgs.Value("-quality-ffmpeg"));
            }
            clock.Start(); recording = true;
            Debug.Log("QUALITY_REPLAY_BEGIN " + route.name);
            for (stepIndex = 0; stepIndex < route.steps.Length; stepIndex++)
            {
                step = route.steps[stepIndex]; stepStart = clock.Elapsed.TotalSeconds; arrived = false;interaction=null;endedByCondition=false;
                var lead = PartyController.Current != null ? PartyController.Current.Active : null;stepTarget = lead != null && lead.TargetLocked ? lead.target : null;
                Debug.Log("QUALITY_CHECKPOINT_BEGIN " + stepIndex + " " + step.name);
                while (clock.Elapsed.TotalSeconds - stepStart < step.seconds)
                {
                    if (!string.IsNullOrEmpty(step.until) && Condition(step.until)) {endedByCondition=true;break;}
                    if (!string.IsNullOrEmpty(step.stopWhen) && Condition(step.stopWhen)) {endedByCondition=true;break;}
                    yield return null;
                }
                ValidateStep();
                if (capture != null && clock.Elapsed.TotalSeconds-lastScreenshot>=route.screenshotInterval)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder, stepIndex.ToString("D3") + ".png"));
                    lastScreenshot=clock.Elapsed.TotalSeconds;
                }
            }
            recording = false; held = default;
            StopProfile();
            GameHud.MeasureCosts=false;
            yield return null;
            if (capture != null) yield return capture.Finish();
            WriteEvidence();
            Debug.Log(failures.Count == 0 ? "QUALITY_REPLAY_OK " + route.name : "QUALITY_REPLAY_REJECTED " + string.Join("; ", failures));
            Application.Quit(failures.Count == 0 ? 0 : 1);
        }
        void InputUpdate()
        {
            held = default;
            if (recording && step != null)
            {
                Vector2 direction = new(step.x, step.y);bool inReach=true;
                var party = PartyController.Current;
                if (step.navigate && party != null && party.Active != null)
                {
                    var actor = PartyController.Current.Active;
                    var chased = step.approachTarget && actor.target != null && actor.target.Alive ? actor.target : null;
                    Vector3 goal = chased != null ? chased.transform.position : step.approachPartner && PartyController.Current.members.Length > 1
                        ? PartyController.Current.members[1-PartyController.Current.index].transform.position : step.point;
                    Vector3 difference = goal - actor.transform.position; difference.y = 0;
                    float reach = chased != null && step.rangedTolerance > 0 && actor.character == "Sela" ? step.rangedTolerance : step.tolerance;
                    if (difference.magnitude <= reach) arrived = true;
                    if (step.approachTarget) inReach = chased != null && difference.magnitude <= reach;
                    if (step.approachTarget ? chased != null && !inReach : !arrived)
                    {
                        var local = Quaternion.Euler(0, -ZoneController.Current.definition.cameraProfile.yaw, 0) * difference.normalized;
                        direction = new Vector2(local.x, local.z) * step.magnitude;
                    }
                    else if (chased != null && actor.flight && difference.sqrMagnitude > .01f && Vector3.Angle(actor.motor.Facing, difference) > 5)
                    {
                        // A roll changes the nose. At firing range, keep a small
                        // ordinary stick correction toward the visible target;
                        // otherwise a chase resumes firing sideways after a roll.
                        var local = Quaternion.Euler(0, -ZoneController.Current.definition.cameraProfile.yaw, 0) * difference.normalized;
                        // .25 clears the Input System's radial dead zone and the
                        // motor's facing threshold. Release once aligned, so
                        // staying at range does not become a slow forward thrust.
                        direction = new Vector2(local.x, local.z) * .25f;
                    }
                }
                bool evading = false;
                var lead = party != null ? party.Active : null;
                if (step.evadeTelegraphs && lead != null && lead.target != null && lead.target.Alive)
                {
                    var boss = lead.target.GetComponent<BossController>();
                    var threat = lead.target.GetComponent<EnemyBrain>();
                    var offset = lead.transform.position - lead.target.transform.position; offset.y = 0;
                    evading = boss != null && boss.Telegraphing && boss.TelegraphRemaining < .16f && offset.sqrMagnitude < 18 * 18 ||
                        threat != null && threat.Telegraphing && threat.TelegraphRemaining < .16f && offset.sqrMagnitude < 5 * 5;
                    if (evading)
                    {
                        // Read the visible tell and send an ordinary lateral dodge.
                        // Release at the end of the tell; do not call actor actions
                        // or alter damage, cooldowns, time, or the fight's outcome.
                        var lateral = Vector3.Cross(Vector3.up, offset.normalized);
                        var local = Quaternion.Euler(0, -ZoneController.Current.definition.cameraProfile.yaw, 0) * lateral;
                        direction = new Vector2(local.x, local.z);
                    }
                }
                held = new GamepadState { leftStick = Vector2.ClampMagnitude(direction, 1), leftTrigger = step.leftTrigger, rightTrigger = step.rightTrigger };
                if (evading) held = held.WithButton(GamepadButton.East);
                bool press = step.pulseSeconds <= 0 || (clock.Elapsed.TotalSeconds - stepStart) % step.pulseSeconds < .1;
                if (!evading && press && inReach && step.buttons != null) foreach (var button in step.buttons)
                {
                    if (!Enum.TryParse<GamepadButton>(button, true, out var parsed)) throw new InvalidOperationException("Invalid replay button " + button);
                    held = held.WithButton(parsed);
                }
                if(interaction==null&&press&&step.buttons?.Length>0&&!string.IsNullOrEmpty(step.expectedUi))
                {
                    interaction=new InteractionResponse{step=stepIndex,expectedUi=step.expectedUi,inputSeconds=clock.Elapsed.TotalSeconds};
                    interactions.Add(interaction);
                }
                pad.MakeCurrent();
            }
            if (pad != null) InputSystem.QueueStateEvent(pad, held);
        }
        void LateUpdate()
        {
            if (!recording) return;
            var actor = PartyController.Current != null ? PartyController.Current.Active : null;
            string observedUi = ObserveUi();
            double now = clock.Elapsed.TotalSeconds;
            if(census&&now>=nextCensus)
            {
                long started=System.Diagnostics.Stopwatch.GetTimestamp();
                var entry=new CensusSample{seconds=now,scene=SceneFlow.Current.Zone,
                    objects=FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length};
                foreach(var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {entry.sources++;if(source.isPlaying)entry.playingSources++;if(source.loop&&source.isPlaying)entry.loopingSources++;}
                entry.milliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
                growth.Add(entry);nextCensus=now+5;
            }
            if(interaction!=null&&interaction.visibleResponseMs<0)
            {
                bool visible = UiMatches(interaction.expectedUi, observedUi);
                if(visible)interaction.visibleResponseMs=(now-interaction.inputSeconds)*1000;
            }
            // Keep the binary trace bounded while retaining the first conversation,
            // options and shop. HUD counters continue throughout the diagnostic run.
            if(tracing && now>45)StopProfile();
            if (actor != null && samples.Count > 0 && samples[^1].hero == actor.character)
                measuredMotion = (actor.transform.position - samples[^1].player) / Mathf.Max(.001f, (float)(now - previous));
            var animator = actor != null ? actor.GetComponentInChildren<Animator>() : null;
            var driver = actor != null ? actor.GetComponentInChildren<GeneratedAnimator>() : null;
            float pelvis = float.NaN, chest = float.NaN, animationTime = 0;
            if (animator != null && animator.isHuman)
            {
                if (!bones.TryGetValue(animator, out var b))
                {
                    b = new[] { animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg), animator.GetBoneTransform(HumanBodyBones.RightUpperLeg), animator.GetBoneTransform(HumanBodyBones.LeftUpperArm), animator.GetBoneTransform(HumanBodyBones.RightUpperArm) };
                    bones[animator] = b;
                }
                pelvis = SkeletalYaw(b[0], b[1]); chest = SkeletalYaw(b[2], b[3]);
                animationTime = animator.GetCurrentAnimatorStateInfo(driver!=null?driver.ActionLayer:0).normalizedTime;
            }
            if(motion&&animator!=null&&animator.isHuman&&driver!=null&&driver.strideProfile!=null)
            {
                var calibration=driver.strideProfile;
                Vector3 leftToe=animator.GetBoneTransform(HumanBodyBones.LeftToes).TransformPoint(calibration.leftToe);
                Vector3 rightToe=animator.GetBoneTransform(HumanBodyBones.RightToes).TransformPoint(calibration.rightToe);
                motionSamples.Add(new MotionSample{seconds=now,step=stepIndex,hero=actor.character,
                    form=actor.GetComponent<FormController>().Current.ToString(),clip=driver.LocomotionAnimation,
                    phase=animator.GetCurrentAnimatorStateInfo(0).normalizedTime,stride=driver.StrideScale,transitioning=animator.IsInTransition(0),reverse=driver.ReverseLocomotion,
                    locked=actor.TargetLocked,target=actor.target!=null?actor.target.transform.position:new Vector3(float.NaN,float.NaN,float.NaN),
                    leftHeel=animator.GetBoneTransform(HumanBodyBones.LeftFoot).TransformPoint(calibration.leftHeel),
                    rightHeel=animator.GetBoneTransform(HumanBodyBones.RightFoot).TransformPoint(calibration.rightHeel),
                    leftToe=leftToe,rightToe=rightToe,leftGroundY=SurfaceY(leftToe),rightGroundY=SurfaceY(rightToe)});
            }
            var party = PartyController.Current;
            var partner = party != null && party.members.Length > 1 ? party.members[1-party.index] : null;
            FrameTiming timing = default;
            bool hasTiming = false;
            if (headroom)
            {
                // Direct API access alongside ProfilerRecorder, not a second
                // hardware clock. The latest completed frame is asynchronous;
                // retain its timestamp so the analyzer can detect cached rows.
                FrameTimingManager.CaptureFrameTimings();
                hasTiming = FrameTimingManager.GetLatestTimings(1, frameTimings) > 0;
                if (hasTiming) timing = frameTimings[0];
            }
            samples.Add(new Sample {
                elapsed = now, ms = (float)((now - previous) * 1000), gameDelta = Time.deltaTime,
                player = actor != null ? actor.transform.position : Vector3.zero, camera = Camera.main.transform.position, forward = actor != null ? actor.transform.forward : Vector3.zero,
                reportedSpeed = actor != null ? actor.motor.Velocity.magnitude : 0, health = actor != null ? actor.Health.integrity : -1,
                partnerHealth = partner != null ? partner.Health.integrity : -1,
                partnerState = partner != null ? partner.State : ActorState.Idle,
                pelvisYaw = pelvis, chestYaw = chest, animationTime = animationTime,
                scene = ActualScene, ui = observedUi, hero = actor != null ? actor.character : "", form = actor != null ? actor.GetComponent<FormController>().Current : default,
                state = actor != null ? actor.State : ActorState.Idle, clip = driver != null ? driver.CurrentAnimation : actor != null ? "ship" : "none",
                prompt = PromptService.Interaction?.prompt ?? "", speaker = DialogueSystem.Current?.Speaker ?? "",
                lines = DialogueSystem.Current?.LinesPresented ?? 0, step = stepIndex,
                collections = GC.CollectionCount(0), allocation = allocations.Valid ? allocations.LastValue : -1,
                mainThread = mainThread.Valid ? mainThread.LastValue : -1, renderThread = renderThread.Valid ? renderThread.LastValue : -1,
                batches = batches.Valid ? batches.LastValue : -1, memory = memory.Valid ? memory.LastValue : -1,
                hudNs=profiled?GameHud.LastUpdateNanoseconds:-1, hudBytes=profiled?GameHud.LastAllocatedBytes:-1,
                audioVoices=audioVoices.Valid?audioVoices.LastValue:-1,
                sceneObjects=sceneObjects.Valid?sceneObjects.LastValue:-1, objects=objects.Valid?objects.LastValue:-1,
                activeCpu=Positive(activeCpu),activeRender=Positive(activeRender),gpuWork=Positive(gpuWork),capWait=Positive(capWait),
                ftmTimestamp=hasTiming?timing.frameStartTimestamp:0,
                ftmGpuMs=hasTiming?timing.gpuFrameTime:-1,ftmCpuMs=hasTiming?timing.cpuFrameTime:-1,
                focus = Application.isFocused, paused = GameTime.Paused, blocked = GameInput.Current.Blocked
            });
            previous = now;
        }
        static long Positive(ProfilerRecorder recorder)=>recorder.Valid&&recorder.LastValue>0?recorder.LastValue:-1;
        static float SurfaceY(Vector3 sole)=>Physics.Raycast(sole+Vector3.up*.5f,Vector3.down,out var hit,1.5f,~0,QueryTriggerInteraction.Ignore)?hit.point.y:float.NaN;
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
            // A chase that its outcome ended (the target fell first) has nothing left to reach.
            if (step.navigate && !arrived && !(step.approachTarget && endedByCondition)) failures.Add(step.name + ": navigation checkpoint missed");
            string expected = string.IsNullOrEmpty(step.expectedScene) ? route.scene : step.expectedScene;
            if (ActualScene != expected) failures.Add(step.name + ": scene " + ActualScene + " expected " + expected);
            if (!string.IsNullOrEmpty(step.expectedFlag) && !GameServices.Current.Flags.GetBool(step.expectedFlag)) failures.Add(step.name + ": missing flag " + step.expectedFlag);
            string observedUi = ObserveUi();
            if (!string.IsNullOrEmpty(step.expectedUi) && !UiMatches(step.expectedUi, observedUi)) failures.Add(step.name + ": UI " + observedUi + " expected " + step.expectedUi);
            if (PartyController.Current == null || PartyController.Current.Active == null)
            {
                if (expected != "Title" || ActualScene != "Title" || !string.IsNullOrEmpty(step.expectedCharacter) || !string.IsNullOrEmpty(step.expectedForm))
                    failures.Add(step.name + ": party unavailable");
                return;
            }
            if (!string.IsNullOrEmpty(step.expectedCharacter) && PartyController.Current.Active.character != step.expectedCharacter) failures.Add(step.name + ": character mismatch");
            if (!string.IsNullOrEmpty(step.expectedForm) && PartyController.Current.Active.GetComponent<FormController>().Current.ToString() != step.expectedForm) failures.Add(step.name + ": form mismatch");

        }
        bool Condition(string name)
        {
            // The enemy locked when the step began has fallen.
            if (name == "targetDown") return stepTarget != null && !stepTarget.Alive;
            if (name.StartsWith("flag:")) return GameServices.Current.Flags.GetBool(name.Substring(5));
            if (name.StartsWith("ui:")) return UiMatches(name.Substring(3), ObserveUi());
            if (name.StartsWith("scene:")) return !SceneFlow.Current.Loading && ActualScene == name.Substring(6);
            var p = PartyController.Current;
            return name switch {
                "partnerDown" => p != null && p.members.Length > 1 && !p.members[1-p.index].Health.Alive && p.Active.Health.Alive,
                "partyAlive" => p != null && p.members.Length > 1 && p.members[0].Health.Alive && p.members[1].Health.Alive,
                _ => throw new InvalidOperationException("Unknown replay condition " + name)
            };
        }
        void WriteEvidence()
        {
            if (samples.Count < 2) failures.Add("missing samples");
            foreach (var s in samples) if (!s.focus) { failures.Add("focus lost"); break; }
            foreach (var s in samples) if (s.paused && !AllowedPause(s)) { failures.Add("simulation paused"); break; }
            if (Screen.width != 1920 || Screen.height != 1080) failures.Add("wrong resolution");
            if (capture != null && !string.IsNullOrEmpty(capture.Failure)) failures.Add(capture.Failure);
            using (var writer = new StreamWriter(Path.Combine(folder, "frames.csv")))
            {
                writer.WriteLine("elapsed,ms,step,scene,hero,form,state,clip,animationTime,x,y,z,cameraX,cameraY,cameraZ,forwardX,forwardZ,pelvisYaw,chestYaw,reportedSpeed,integrity,focus,paused,blocked,gameDelta,gcCollections,gcBytes,mainThreadNs,renderThreadNs,batches,memoryBytes,dialogueLines,prompt,speaker,partnerHealth,partnerState,hudNs,hudBytes,audioVoices,sceneObjects,objects,activeCpuNs,activeRenderNs,gpuWorkNs,capWaitNs,ftmTimestamp,ftmGpuMs,ftmCpuMs,ui");
                foreach (var s in samples) writer.WriteLine(FormattableString.Invariant($"{s.elapsed:F6},{s.ms:F4},{s.step},{s.scene},{s.hero},{s.form},{s.state},{s.clip},{s.animationTime:F4},{s.player.x:F5},{s.player.y:F5},{s.player.z:F5},{s.camera.x:F5},{s.camera.y:F5},{s.camera.z:F5},{s.forward.x:F5},{s.forward.z:F5},{s.pelvisYaw:F3},{s.chestYaw:F3},{s.reportedSpeed:F4},{s.health:F1},{s.focus},{s.paused},{s.blocked},{s.gameDelta:F6},{s.collections},{s.allocation},{s.mainThread},{s.renderThread},{s.batches},{s.memory},{s.lines},\"{s.prompt.Replace("\"", "\"\"")}\",{s.speaker},{s.partnerHealth:F1},{s.partnerState},{s.hudNs},{s.hudBytes},{s.audioVoices},{s.sceneObjects},{s.objects},{s.activeCpu},{s.activeRender},{s.gpuWork},{s.capWait},{s.ftmTimestamp},{s.ftmGpuMs:F6},{s.ftmCpuMs:F6},{s.ui}"));
            }
            var report = new Report { route = route.name, sourceRevision = route.sourceRevision, build = Debug.isDebugBuild ? "Development" : "Release", unity = Application.unityVersion,
                cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                width = Screen.width, height = Screen.height, frameCap = Application.targetFrameRate, vSync = QualitySettings.vSyncCount,
                refreshHz = Screen.currentResolution.refreshRateRatio.value, samples = samples.Count, seconds = samples.Count > 0 ? samples[^1].elapsed : 0,
                captured = capture != null, profiled=profiled, headroom=headroom, census=census, motion=motion, failures = failures.ToArray(), valid = failures.Count == 0,
                frameTimingRequested=headroom,frameTimingEnabled=frameTimingEnabled,
                frameTimingCpuFrequency=headroom?FrameTimingManager.GetCpuTimerFrequency():0,
                loadWaitSeconds=loadWaitSeconds,settleSeconds=route.settleSeconds,interactions=interactions.ToArray() };
            if(headroom)report.gpuTiming=samples.Exists(s=>s.gpuWork>0)?"GPU Frame Time counter (nanoseconds; asynchronous)":"UNAVAILABLE (counter produced no positive samples)";
            File.WriteAllText(Path.Combine(folder, "run.json"), JsonUtility.ToJson(report, true));
            File.WriteAllText(Path.Combine(folder, "route.json"), JsonUtility.ToJson(route, true));
            // Read-only snapshot: Save() would mutate savedAtUtc and create an
            // autosave the player did not request.
            File.WriteAllText(Path.Combine(folder, "final-state.json"), Newtonsoft.Json.JsonConvert.SerializeObject(GameServices.Current.State, Newtonsoft.Json.Formatting.Indented));
            if(census)
            {
                using var writer=new StreamWriter(Path.Combine(folder,"census.csv"));
                writer.WriteLine("seconds,scene,sceneGameObjects,audioSources,playingSources,loopingSources,samplingMs");
                foreach(var s in growth)writer.WriteLine(FormattableString.Invariant($"{s.seconds:F6},{s.scene},{s.objects},{s.sources},{s.playingSources},{s.loopingSources},{s.milliseconds:F6}"));
            }
            if(motion)
            {
                using var writer=new StreamWriter(Path.Combine(folder,"motion.csv"));
                writer.WriteLine("elapsed,step,hero,form,clip,phase,stride,transitioning,reverse,locked,leftHeelX,leftHeelY,leftHeelZ,leftToeX,leftToeY,leftToeZ,rightHeelX,rightHeelY,rightHeelZ,rightToeX,rightToeY,rightToeZ,targetX,targetY,targetZ,leftGroundY,rightGroundY");
                foreach(var s in motionSamples)writer.WriteLine(FormattableString.Invariant($"{s.seconds:F6},{s.step},{s.hero},{s.form},{s.clip},{s.phase:F6},{s.stride:F6},{s.transitioning},{s.reverse},{s.locked},{s.leftHeel.x:F6},{s.leftHeel.y:F6},{s.leftHeel.z:F6},{s.leftToe.x:F6},{s.leftToe.y:F6},{s.leftToe.z:F6},{s.rightHeel.x:F6},{s.rightHeel.y:F6},{s.rightHeel.z:F6},{s.rightToe.x:F6},{s.rightToe.y:F6},{s.rightToe.z:F6},{s.target.x:F6},{s.target.y:F6},{s.target.z:F6},{s.leftGroundY:F6},{s.rightGroundY:F6}"));
            }
        }
        void OnDestroy()
        {
            StopProfile();
            InputSystem.onBeforeUpdate -= InputUpdate;
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            allocations.Dispose(); mainThread.Dispose(); renderThread.Dispose(); batches.Dispose(); memory.Dispose(); audioVoices.Dispose(); sceneObjects.Dispose(); objects.Dispose();
            activeCpu.Dispose();activeRender.Dispose();gpuWork.Dispose();capWait.Dispose();
            GameHud.MeasureCosts=false;
        }
        void StopProfile()
        {
            if(!tracing)return;
            tracing=false;
            UnityEngine.Profiling.Profiler.enabled=false;
            UnityEngine.Profiling.Profiler.logFile="";
        }
    }
}
