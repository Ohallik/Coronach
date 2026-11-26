using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.Data;
using Lattice.Rpg;
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
    public sealed partial class ContinuousReview : MonoBehaviour
    {
        struct Sample
        {
            public double elapsed;
            public float ms, gameDelta, health, partnerHealth, reportedSpeed, pelvisYaw, chestYaw, animationTime;
            public Vector3 player, camera, forward;
            /// <summary>Where the partner is on the plan (frames.csv's partnerX and partnerZ, the last columns): her state alone said she sat still, never where (nov20-whole s17, s54).</summary>
            public Vector3 partnerAt;
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
            // The driver is answering a defeat at a boss's door: the one pause it takes for itself.
            public bool retry;
        }
        [Serializable] sealed class Report
        {
            public string route, sourceRevision, input = "continuous agent-directed virtual gamepad replay", build, unity, cpu, gpu, quality, gpuTiming = "UNAVAILABLE", vrr = "UNVERIFIED";
            public int width, height, frameCap, vSync, samples, damageTraceVersion, releaseTraceVersion;
            public double refreshHz, seconds;
            public bool captured, profiled, headroom, census, motion, valid;
            // A reconnaissance run (D290) is never valid; what it found is in reconnaissancePassed, which only another reconnaissance run reads.
            public bool reconnaissance, reconnaissancePassed;
            public bool frameTimingRequested, frameTimingEnabled;
            public ulong frameTimingCpuFrequency;
            public string[] failures;
            public double loadWaitSeconds, settleSeconds;
            public InteractionResponse[] interactions;
            public BossAttempt[] bossAttempts;
            public StockRecord[] stocks;
        }
        /// <summary>One try at a boss from its door, won or lost: a segment that passed on its third try says so, and says where the first two fell.</summary>
        [Serializable] sealed class BossAttempt
        {
            public string boss, outcome, lastStepName;
            public int attempt, doorStep, lastStep;
            public double from, to;
            // The share of the boss's Integrity still standing when the attempt ended; -1 when the boss wasn't there to read.
            public float bossIntegrity = -1;
            // What the lost attempt's steps failed. They leave the run's failures: the game gave the fight back whole, and every step from the door was played and judged again.
            public string[] failures;
        }
        /// <summary>What a "stock" step bought or made, so a pouch that came up short of its number is on the record and not found out at a boss.</summary>
        [Serializable] sealed class StockRecord
        {
            public int step, before, after, target, presses;
            public string item;
            public bool refused;
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
        // A defeat screen the driver is retrying a boss from is allowed like a menu a step declares: the game put it up, and the retry is on the record.
        bool AllowedPause(Sample sample) => !headroom && sample.step >= 0 && sample.step < route.steps.Length &&
            (sample.ui == "bench" || sample.ui == "pause" || sample.ui == "defeat") && (route.steps[sample.step].pauseUi == sample.ui || sample.retry && sample.ui == "defeat");
        double loadWaitSeconds;
        readonly Dictionary<Animator, Transform[]> bones = new();
        readonly System.Diagnostics.Stopwatch clock = new();
        QualityRoute route;
        Gamepad pad;
        GamepadState held;
        QualityStep step;
        int stepIndex;
        bool recording, arrived, endedByCondition, rowReached, pickHeld, pickPaged; GamepadButton pickButton;
        /// <summary>Reaching a fallen hero goes round what's between: steered straight, a column, a crew body or the yielded captain stopped Taren short, nobody was revived, and the talk
        /// that waits for it never came (QA-035). The baked floor gives the way round the room; a body isn't in the bake, so a hero who stops gaining on the goal side-steps, a side at a time.</summary>
        // Made at its first use, not here: a NavMeshPath made in a field initializer throws in the editor and in a development player
        // ("InitializeNavMeshPath is not allowed to be called from a MonoBehaviour constructor"), which failed every driver test at its set-up.
        UnityEngine.AI.NavMeshPath revivePath;
        Vector3[] reviveCorners;
        /// <summary>Where the kept way leads. Two walks can ask in one frame (a sluice's wheel, then the fallen partner): kept by time alone, the second took the first's way, and
        /// with Idra down across the Foundry's heart Taren stepped side to side on the way to a wheel for six minutes, turning nothing and raising nobody (nov23-recon-s-49/s49b).</summary>
        Vector3 revivePathGoal;
        double revivePathAt=-1, reviveGainedAt, detourUntil=-1;
        float reviveBest=float.PositiveInfinity, detourSide=1;
        Health stepTarget;
        double stepStart, previous, lastSwapPress=-1;
        /// <summary>The driver's own presses (autoHeal, useItem, useExtra), one tap at a time: down at tapDown, up at tapUp, and nothing else of its own until tapOver.
        /// What it wants is thought about ten times a second, not every frame: a thumb doesn't decide faster, and the looking costs.</summary>
        enum Tap { None, ItemUp, ItemDown, Item, Extra }
        Tap tap;
        double tapDown, tapUp, tapOver = -1, nextThought = -1, nextItem = -1, nextExtra = -1;
        string wantItem;
        CombatActor raise;
        bool extraDue, itemForced;
        /// <summary>How near the lead stands to a fallen partner to raise them: inside the 2 m the game asks, with room for a shove.</summary>
        const float ReviveStand = 1.2f;
        /// <summary>A "stock" step: what it is filling the pouch with, to how many, and whether the last press bought nothing.</summary>
        ConsumableDef stockItem;
        int stockTarget, stockBefore, stockAtPress, stockPresses;
        bool stockRefused, stockDown;
        double stockSince;
        const double StockBeat = .5, StockHold = .12;
        /// <summary>How many times more the driver takes a boss from its door after falling there (the QA chains were resumed by hand for this, twice), and how long
        /// it reads the defeat screen first, as a person does.</summary>
        public const int BossRetries = 2;
        const double DefeatRead = 1.2, RetryLoad = 40;
        static readonly string[] RetryPress = { "South" };
        int doorStep = -1, doorAttempt, doorFailures;
        string doorBoss, doorFlag;
        bool retrying, retried;
        BossAttempt attempt;
        readonly List<BossAttempt> bossAttempts = new();
        readonly List<StockRecord> stocks = new();
        string folder;
        ProfilerRecorder allocations, mainThread, renderThread, batches, memory, audioVoices, sceneObjects, objects;
        ProfilerRecorder activeCpu,activeRender,gpuWork,capWait;
        readonly FrameTiming[] frameTimings = new FrameTiming[1];
        bool frameTimingEnabled;
        bool gpuClocks;
        long windowsQpcFrequency;
        struct GpuClockSample
        {
            public int sample;
            public long utcTicks, qpc, windowsQpc;
            public ulong frameStart, present, complete;
            public double mainMs, renderMs, presentWaitMs;
        }
        readonly List<GpuClockSample> gpuClockSamples = new();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool QueryPerformanceCounter(out long value);
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool QueryPerformanceFrequency(out long value);
#endif
        static long WindowsClock(bool frequency)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            long value;
            return (frequency?QueryPerformanceFrequency(out value):QueryPerformanceCounter(out value))?value:-1;
#else
            return -1;
#endif
        }
        QualityCapture capture;
        NativeGpuClock nativeGpu;
        GroundPoseCapture groundPose;
        QualityDamageCapture damageCapture;
        ReleaseGeometryCapture releaseGeometry;
        double lastScreenshot=-1000;
        Vector3 measuredMotion;
        bool arrows, profiled, tracing, headroom;
        QualityTrace segmentedTrace;

        public static QualityRoute ReadRoute()
        {
            string save = DevArgs.Value("-savepath");
            if (string.IsNullOrWhiteSpace(save)) throw new InvalidOperationException("Quality replay requires an explicit isolated -savepath");
            var result = JsonUtility.FromJson<QualityRoute>(File.ReadAllText(DevArgs.Value("-quality-route")));
            if (result?.steps == null || result.steps.Length == 0 || string.IsNullOrEmpty(result.scene)) throw new InvalidOperationException("Empty quality route");
            if (result.scene == "Title" && (result.starterParty || !string.IsNullOrEmpty(result.loadout)))
                throw new InvalidOperationException("Title replay must use starterParty=false and no development loadout");
            if (!Difficulty.TryNamed(result.difficulty, out _)) throw new InvalidOperationException("Unknown route difficulty '" + result.difficulty + "': Gentle, Standard or Demanding");
            foreach (var s in result.steps)
            {
                if (string.IsNullOrEmpty(s.name) || s.seconds <= 0 || s.seconds > 180) throw new InvalidOperationException("Invalid quality step");
                if (!string.IsNullOrEmpty(s.stock) && (string.IsNullOrEmpty(s.pickRow) || s.stock != "cap" && !(int.TryParse(s.stock, out int most) && most > 0)))
                    throw new InvalidOperationException("Invalid stock on step '" + s.name + "': \"cap\" or a count, on a step whose pickRow names the consumable's row");
            }
            return result;
        }
        IEnumerator Start()
        {
            route = ReadRoute();
            folder = Path.GetFullPath(DevArgs.Value("-quality-output"));
            arrows = DevArgs.Has("-quality-arrows");
            profiled = DevArgs.Has("-quality-profile");
            string segmentFrames = DevArgs.Value("-quality-profile-segments");
            if (!string.IsNullOrEmpty(segmentFrames))
            {
                profiled = true;
                segmentedTrace = new QualityTrace(folder, int.Parse(segmentFrames, CultureInfo.InvariantCulture));
            }
            headroom = DevArgs.Has("-quality-headroom");
            bool headroomClocks = DevArgs.Has("-quality-gpu-clocks");
            gpuClocks = headroomClocks || DevArgs.Has("-quality-frame-clocks");
            if (headroomClocks && !headroom) throw new InvalidOperationException("GPU clock diagnostics require headroom mode");
            if (gpuClocks) windowsQpcFrequency = WindowsClock(true);
            census = DevArgs.Has("-quality-census");
            motion = DevArgs.Has("-quality-motion");
            Directory.CreateDirectory(folder);
            if(DevArgs.Has("-quality-native-gpu"))
            {nativeGpu=gameObject.AddComponent<NativeGpuClock>();nativeGpu.Initialize(folder);}
            pad = InputSystem.AddDevice<Gamepad>("CoronachContinuousReview");
            reconnaissance = DevArgs.Has("-quality-reconnaissance");
            if (reconnaissance) Reconnoitre();
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
            if(headroom || gpuClocks)
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
            if(profiled && segmentedTrace == null)
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
                damageCapture=new QualityDamageCapture(folder,()=>clock.Elapsed.TotalSeconds,()=>stepIndex);
                releaseGeometry=new ReleaseGeometryCapture(folder);
            }
            nativeGpu?.Begin();
            if(DevArgs.Has("-quality-ground-pose"))groundPose=new GroundPoseCapture(folder);
            clock.Start(); recording = true;
            Debug.Log("QUALITY_REPLAY_BEGIN " + route.name);
            var steps = RunSteps();
            while (steps.MoveNext()) yield return steps.Current;
            recording = false; held = default;
            damageCapture?.Finish();
            releaseGeometry?.Finish();
            StopProfile();
            segmentedTrace?.Finish();
            GameHud.MeasureCosts=false;
            yield return null;
            if (capture != null) yield return capture.Finish();
            if (nativeGpu != null) yield return nativeGpu.Finish();
            groundPose?.Finish();
            WriteEvidence();
            // A reconnaissance run never says QUALITY_REPLAY_OK: nothing that looks for a passed replay may find one in its log.
            if (reconnaissance) Debug.Log(failures.Count == 0 ? "QUALITY_RECONNAISSANCE_OK " + route.name : "QUALITY_RECONNAISSANCE_FAILED " + string.Join("; ", failures));
            else
            Debug.Log(failures.Count == 0 ? "QUALITY_REPLAY_OK " + route.name : "QUALITY_REPLAY_REJECTED " + string.Join("; ", failures));
            // Out through the orderly door: the capture let its target go this frame, and D3D12 has crashed tearing down with that still in flight (QA-031).
            OrderlyQuit.Quit(failures.Count == 0 ? 0 : 1);
        }
        /// <summary>The route's steps, in order. A method of its own so a check can play steps without a player's launch arguments, and so a boss lost at its door
        /// can be taken again from the door's step.</summary>
        IEnumerator RunSteps()
        {
            for (stepIndex = 0; stepIndex < route.steps.Length; stepIndex++)
            {
                BeginStep(route.steps[stepIndex]);
                bool fromDoor = false;
                while (clock.Elapsed.TotalSeconds - stepStart < step.seconds)
                {
                    if (FellAtTheDoor())
                    {
                        var retry = RetryFromDoor();
                        while (retry.MoveNext()) yield return retry.Current;
                        fromDoor = retried; break;
                    }
                    if (!string.IsNullOrEmpty(step.until) && Condition(step.until)) {endedByCondition=true;break;}
                    if (!string.IsNullOrEmpty(step.stopWhen) && Condition(step.stopWhen)) {endedByCondition=true;break;}
                    // ensureCharacter asked for a lead it can't have, and has said why: the step ends here, failed, not when its seconds run out.
                    if (EnsureGaveUp) break;
                    // wearBest: the best spare parts put on, out of any fight and any talk (ContinuousReview.Gear.cs).
                    WearWhenSafe();
                    yield return null;
                }
                // Back at the door, the step the party fell in isn't judged: it is played again.
                if (fromDoor) { stepIndex = doorStep - 1; continue; }
                // Judged first, then put on record: the record lets go of the item the step was stocking, and judged after it "until":"stocked" read
                // false for a pouch that had just been filled, so every stock step failed with 'condition timeout stocked'.
                ValidateStep();
                NoteStock();
                if (capture != null && clock.Elapsed.TotalSeconds-lastScreenshot>=route.screenshotInterval)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder, stepIndex.ToString("D3") + ".png"));
                    lastScreenshot=clock.Elapsed.TotalSeconds;
                }
            }
            // The route ran out with a boss's door still open: its last attempt says how it stood.
            if (doorStep >= 0) { CloseAttempt(BossDown() ? "won" : "unfinished"); doorStep = -1; }
        }
        void BeginStep(QualityStep next)
        {
            step = next; stepStart = clock.Elapsed.TotalSeconds; arrived = false;interaction=null;endedByCondition=false;rowReached=false;pickHeld=false;pickPaged=false;savesAtStep=SaveSystem.Asked;
            reviveCorners=null;revivePathAt=-1;detourUntil=-1;reviveBest=float.PositiveInfinity;reviveGainedAt=stepStart;
            var lead = PartyController.Current != null ? PartyController.Current.Active : null;stepTarget = lead != null && lead.TargetLocked ? lead.target : null;
            // A step that ends when what it locked falls needs something locked that can fall. The lock press is a toggle: pressed while a lock was already on (the
            // d-pad had brought one round to a broken boss on the walk over) it let go instead, and the step chased the boss's body for its whole time (the Bellows'
            // east organ, November 22). With nothing locked, or the boss's own body, the step is for the part the route walked the lead up to: the nearest one standing.
            if (lead != null && step.until == "targetDown")
            {
                if (stepTarget == null || !stepTarget.Alive || stepTarget.GetComponent<BossController>() != null) stepTarget = NearestPart(lead) ?? stepTarget;
                Debug.Log("QUALITY_STEP_TARGET " + step.name + ": " + (stepTarget != null ? stepTarget.name : "nothing") + " (lock on " + (lead.TargetLocked && lead.target != null ? lead.target.name : "nothing") + ")");
            }
            itemForced = false; wantItem = null; raise = null; extraDue = false; nextThought = -1;
            NameTheLead();
            BeginStock();
            // A route that names its pair ("deployed") is held to it at the first step that has a party.
            CheckPair();
            Debug.Log("QUALITY_CHECKPOINT_BEGIN " + stepIndex + " " + step.name);
            if (!string.IsNullOrEmpty(step.bossDoor) && !(doorStep == stepIndex && doorBoss == step.bossDoor)) OpenDoor();
        }

        // ---------- a boss's door: the fight the game gives back whole ----------
        static bool Flag(string key) => GameServices.Current != null && GameServices.Current.State.flags.TryGetValue(key, out bool set) && set;
        bool BossDown() => doorFlag != null && Flag(doorFlag);
        void OpenDoor()
        {
            if (doorStep >= 0) CloseAttempt(BossDown() ? "won" : "unfinished");
            doorStep = -1;
            // A save that is already past this boss has no fight here to lose.
            if (Flag("bossdown." + step.bossDoor)) return;
            doorStep = stepIndex; doorBoss = step.bossDoor; doorFlag = "bossdown." + doorBoss; doorAttempt = 0; doorFailures = failures.Count;
            OpenAttempt();
        }
        void OpenAttempt()
        {
            doorAttempt++;
            attempt = new BossAttempt { boss = doorBoss, attempt = doorAttempt, doorStep = doorStep, from = clock.Elapsed.TotalSeconds };
            bossAttempts.Add(attempt);
            Debug.Log("QUALITY_BOSS_ATTEMPT " + doorBoss + " " + doorAttempt + " of " + (BossRetries + 1) + " from step " + doorStep + " " + route.steps[doorStep].name);
        }
        void CloseAttempt(string outcome)
        {
            if (attempt == null) return;
            int at = Mathf.Clamp(stepIndex, 0, route.steps.Length - 1);
            attempt.outcome = outcome; attempt.to = clock.Elapsed.TotalSeconds; attempt.lastStep = at; attempt.lastStepName = route.steps[at].name;
            foreach (var body in Health.All) if (body != null && !body.friendly && body.id == doorBoss && body.maximum > 0) attempt.bossIntegrity = Mathf.Clamp01(body.integrity / body.maximum);
            if (outcome == "won") attempt.bossIntegrity = 0;
            Debug.Log(FormattableString.Invariant($"QUALITY_BOSS_ATTEMPT_END {attempt.boss} {attempt.attempt} {outcome} step={at} seconds={attempt.to - attempt.from:F1} bossIntegrity={attempt.bossIntegrity:F2}"));
            attempt = null;
        }
        /// <summary>Watches a boss's fight from its door step on: closes the attempt when the boss is down, and says when the party has fallen with a retry still in hand.
        /// With none left the defeat stands, and the run is rejected as it always was.</summary>
        bool FellAtTheDoor()
        {
            if (doorStep < 0) return false;
            if (BossDown()) { CloseAttempt("won"); doorStep = -1; return false; }
            if (ObserveUi() != "defeat") return false;
            if (doorAttempt <= BossRetries) return true;
            CloseAttempt("defeat"); doorStep = -1;
            failures.Add(doorBoss + ": the party fell at its door " + (BossRetries + 1) + " times");
            return false;
        }
        /// <summary>The party has fallen at a boss's door: read the screen, take Retry with the pad, and wait to be standing at the door again. The lost attempt and
        /// what its steps failed go on the record, and the route goes back to the door's step.</summary>
        IEnumerator RetryFromDoor()
        {
            retried = false; retrying = true;
            var lost = attempt; var fellIn = step; double fell = clock.Elapsed.TotalSeconds;
            CloseAttempt("defeat");
            if (lost != null && failures.Count > doorFailures)
            {
                lost.failures = failures.GetRange(doorFailures, failures.Count - doorFailures).ToArray();
                failures.RemoveRange(doorFailures, failures.Count - doorFailures);
            }
            Debug.Log("QUALITY_BOSS_RETRY " + doorBoss + " after attempt " + doorAttempt + ", fell in step " + stepIndex + " " + fellIn.name);
            if (capture != null) ScreenCapture.CaptureScreenshot(Path.Combine(folder, "retry-" + doorBoss + "-" + doorAttempt + ".png"));
            // Nothing held while the screen is read. Then Retry, the row it opens on, found by name all the same and pressed once a second until the screen goes.
            step = new QualityStep { name = "retry " + doorBoss + " from its door", seconds = 60 };
            rowReached = false; pickHeld = false; pickPaged = false; interaction = null; stockItem = null; stockPart = null;
            while (clock.Elapsed.TotalSeconds - fell < DefeatRead) yield return null;
            step.pickRow = "Retry"; step.buttons = RetryPress; step.pulseSeconds = 1; stepStart = clock.Elapsed.TotalSeconds;
            while (ObserveUi() == "defeat" && clock.Elapsed.TotalSeconds - stepStart < 10) yield return null;
            step.pickRow = null; step.buttons = null;
            while (!Standing() && clock.Elapsed.TotalSeconds - stepStart < RetryLoad) yield return null;
            bool back = Standing();
            // A breath at the door before the route takes the pad again: the arrival's own hold on the input has to lapse.
            double stoodAt = clock.Elapsed.TotalSeconds;
            while (back && clock.Elapsed.TotalSeconds - stoodAt < .6) yield return null;
            step = fellIn; retrying = false;
            if (!back) { failures.Add(doorBoss + ": the retry from its door never came back to the world"); doorStep = -1; yield break; }
            OpenAttempt(); retried = true;
        }
        bool Standing()
        {
            var party = PartyController.Current;
            if (SceneFlow.Current.Loading || party == null || party.members == null || ObserveUi() != "world") return false;
            foreach (var member in party.members) if (member == null || !member.Health.Alive) return false;
            return true;
        }

        // ---------- a stock step: the pouch filled to a number, not a count of presses ----------
        static int Carried(ConsumableDef def) => GameServices.Current.State.consumables.TryGetValue(def.id, out int count) ? count : 0;
        // A part's row is stocked the same way, to a count of that part (ContinuousReview.Gear.cs: StockHeld, BeginPartStock).
        bool Stocked => (stockItem != null || stockPart != null) && (stockRefused || StockHeld() >= stockTarget);
        void BeginStock()
        {
            stockItem = null; stockPart = null; stockRefused = false; stockDown = false; stockPresses = 0; stockSince = -1;
            if (string.IsNullOrEmpty(step.stock)) return;
            foreach (var def in GameCatalog.All<ConsumableDef>()) if (def != null && (def.displayName == step.pickRow || def.id == step.pickRow)) stockItem = def;
            if (stockItem == null && BeginPartStock()) return;
            if (stockItem == null) { failures.Add(step.name + ": stock is for a consumable's or a part's row, and '" + step.pickRow + "' names none"); return; }
            int cap = Inventory.Cap(stockItem);
            stockTarget = step.stock == "cap" ? cap : Math.Min(cap, int.Parse(step.stock, CultureInfo.InvariantCulture));
            stockBefore = stockAtPress = Carried(stockItem);
        }
        /// <summary>Whether the stock step's button is down this frame: a press every half second while the pouch is short of its number, each checked against the
        /// last. A press that bought nothing is the stall or the bench saying no (no scrip, no materials), and it is the last one sent.</summary>
        bool StockPress()
        {
            if (stockRefused) return false;
            int carried = StockHeld();
            if (carried >= stockTarget) { stockDown = false; return false; }
            double now = clock.Elapsed.TotalSeconds;
            // The row has only just come under the cursor: a moment to read it, as before any buy.
            if (stockSince < 0) stockSince = now + .25;
            bool down = now >= stockSince && (now - stockSince) % StockBeat < StockHold;
            if (down && !stockDown)
            {
                if (stockPresses > 0 && carried <= stockAtPress) { stockRefused = true; return false; }
                stockAtPress = carried; stockPresses++;
            }
            stockDown = down;
            return down;
        }
        void NoteStock()
        {
            if (stockItem == null && stockPart == null) return;
            var record = new StockRecord { step = stepIndex, item = stockPart != null ? stockPart.id : stockItem.id, before = stockBefore, after = StockHeld(), target = stockTarget == int.MaxValue ? -1 : stockTarget, presses = stockPresses, refused = stockRefused };
            stocks.Add(record);
            Debug.Log("QUALITY_STOCK " + record.item + " " + record.before + " -> " + record.after + " of " + (record.target < 0 ? "no cap" : record.target.ToString(CultureInfo.InvariantCulture)) + " presses=" + record.presses + (record.refused ? " refused" : ""));
            PartStockShort(record.after);
            stockItem = null; stockPart = null;
        }

        // ---------- the driver's own hands: autoHeal, useItem, useExtra ----------
        /// <summary>The driver may use the pad for itself on this step: the route asks for it, the lead is up in a zone with fights, nothing is on screen for the pad
        /// to be steering, and the step hasn't said to leave the pouch alone.</summary>
        bool HandsFree(PartyController party) => route != null && !retrying && (route.autoHeal || route.useExtra || !string.IsNullOrEmpty(step.useItem)) && step.useItem != "none" &&
            party != null && party.members != null && party.Active != null && party.Active.Health.Alive && !party.Active.Recovering &&
            ZoneController.Current != null && ZoneController.Current.Combat && !GameInput.Current.Blocked && !GameTime.Paused && !SceneFlow.Current.Loading &&
            string.IsNullOrEmpty(step.pickRow) && string.IsNullOrEmpty(step.pauseUi);
        bool StepHoldsSkillMod()
        {
            if (step.buttons != null) foreach (var button in step.buttons) if (string.Equals(button, "RightShoulder", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        /// <summary>What the driver wants of the pad right now (ReplayHands decides; this only asks where the route lets it). The pouch answers on foot only: in flight
        /// LT is the brake, and the game has no quick item there.</summary>
        void Think(PartyController party)
        {
            double now = clock.Elapsed.TotalSeconds;
            if (now < nextThought) return;
            nextThought = now + .1;
            var lead = party.Active; var pouch = GameServices.Current.State;
            wantItem = null; raise = null;
            if (!lead.flight && !string.IsNullOrEmpty(step.useItem)) wantItem = itemForced ? null : step.useItem;
            else if (!lead.flight && route.autoHeal && step.leftTrigger <= 0)
            {
                wantItem = ReplayHands.ItemFor(party, lead, pouch);
                var fallen = ReplayHands.Fallen(party);
                if (fallen != null && ReplayHands.HeldRevive(pouch) == null) raise = fallen;
            }
            extraDue = route.useExtra && ReplayHands.ExtraDue(lead);
        }
        /// <summary>Which way the d-pad brings the quick item round to this one in fewer presses, as PlayerBrain walks the pouch: 1 is up, -1 down, 0 when the pouch
        /// has never held it.</summary>
        static int CycleToward(GameState pouch, string want)
        {
            int count = pouch.consumables.Count, at = -1, goal = -1, index = 0;
            foreach (var held in pouch.consumables) { if (held.Key == pouch.quickItem) at = index; if (held.Key == want) goal = index; index++; }
            if (goal < 0) return 0;
            if (at < 0) return 1;
            int up = (goal - at + count) % count;
            return up <= count - up ? 1 : -1;
        }
        void BeginTap(Tap kind, double leadIn, double hold, double rest)
        {
            double now = clock.Elapsed.TotalSeconds;
            tap = kind; tapDown = now + leadIn; tapUp = tapDown + hold; tapOver = tapUp + rest;
        }
        /// <summary>The driver's own presses, laid over the step's: one tap at a time, each pressed and let go as a thumb does, and never begun across a dodge.
        /// The quick item: d-pad to the one wanted, then LT. An EXTRA: everything else let go, then RB and LT together, so the step's own attack button can't make a
        /// skill of the RB.</summary>
        GamepadState Hands(GamepadState state, bool free, bool evading)
        {
            double now = clock.Elapsed.TotalSeconds;
            if (now >= tapOver)
            {
                tap = Tap.None;
                if (!free || evading) return state;
                var pouch = GameServices.Current.State;
                if (extraDue && now >= nextExtra)
                {
                    extraDue = false; nextExtra = now + 1.5;
                    BeginTap(Tap.Extra, .08, .14, .12);
                    Debug.Log("QUALITY_EXTRA_PRESS " + PartyController.Current.Active.character);
                }
                else if (wantItem != null && now >= nextItem && !StepHoldsSkillMod())
                {
                    int way = pouch.quickItem == wantItem ? 0 : CycleToward(pouch, wantItem);
                    if (pouch.quickItem == wantItem)
                    {
                        Debug.Log("QUALITY_ITEM_PRESS " + wantItem + (string.IsNullOrEmpty(step.useItem) ? "" : " (the step's)"));
                        if (!string.IsNullOrEmpty(step.useItem)) itemForced = true;
                        wantItem = null; nextItem = now + .8;
                        BeginTap(Tap.Item, 0, .12, .3);
                    }
                    else if (way != 0) BeginTap(way > 0 ? Tap.ItemUp : Tap.ItemDown, 0, .1, .14);
                    else
                    {
                        // Only a step can ask for what the pouch has never held: say so once, and leave it.
                        failures.Add(step.name + ": useItem " + wantItem + " isn't in the pouch");
                        itemForced = true; wantItem = null;
                    }
                }
                if (tap == Tap.None) return state;
            }
            bool down = now >= tapDown && now < tapUp;
            switch (tap)
            {
                case Tap.Extra:
                    state.buttons = 0; state.leftTrigger = down ? 1 : 0;
                    if (down) state = state.WithButton(GamepadButton.RightShoulder);
                    break;
                case Tap.Item:
                    // RB and LT together are the EXTRA: while LT is down for the pouch, a step's RB waits.
                    if (down) { state.buttons &= ~(1u << (int)GamepadButton.RightShoulder); state.leftTrigger = 1; }
                    break;
                case Tap.ItemUp: if (down) state = state.WithButton(GamepadButton.DpadUp); break;
                case Tap.ItemDown: if (down) state = state.WithButton(GamepadButton.DpadDown); break;
            }
            return state;
        }
        void InputUpdate()
        {
            // A hit-stop is a few held frames in which nothing moves and no thumb reacts: the pad stays as it was. Read afresh, a tell's time left against a clock standing at 0
            // let go of the dodge for those frames and pressed it again after.
            if (GameTime.Stopped && recording && step != null) { if (pad != null) InputSystem.QueueStateEvent(pad, held); return; }
            held = default;
            if (recording && step != null)
            {
                Vector2 direction = new(step.x, step.y);bool inReach=true,seekingBoss=false,cutterFlying=false;wheelPress=false;clampPress=false;saintPress=false;
                var party = PartyController.Current;
                if (step.navigate && party != null && party.Active != null)
                {
                    var actor = PartyController.Current.Active;
                    var chased = step.approachTarget && actor.target != null && actor.target.Alive ? actor.target : null;
                    // The partner by role (ContinuousReview.Party.cs): by place it was members[1-index], which a lead in a third place reads off the array's front.
                    var beside = step.approachPartner ? Partner(party) : null;
                    // Nothing up to chase, and something under a ringed hide: a person reads the ring and steps into it, which is the only way an ambusher's fight goes on.
                    // The chase stood idle beside three occupied hides for two minutes (the Braided River's Reedstalkers, November 13).
                    var hide = step.approachTarget && chased == null && !actor.flight ? HideToDraw(actor.transform.position) : null;
                    // A boss that has gone further off than anything can be locked from (a leap to another join of the hull) leaves the chase nothing: it sat where the
                    // boss had been for four minutes (the Hulljack, November 14). The objective's arrow points at a fighting boss, and a person follows it: toward the
                    // boss, brake off, until it is near enough to lock again. One that is near and can't be hurt (burrowed, collared) is left as it always was.
                    var lost = step.approachTarget && chased == null && hide == null ? BossController.Focus(actor.transform.position, LostBossWithin) : null;
                    Vector3 goal = chased != null ? chased.transform.position : hide != null ? hide.transform.position : lost != null ? lost.transform.position : beside != null ? beside.transform.position : BerthGoal(step);
                    Vector3 difference = goal - actor.transform.position; difference.y = 0;
                    float reach = chased != null && step.rangedTolerance > 0 && ShootsOnFoot(actor) ? step.rangedTolerance : step.tolerance;
                    // A Clamp Tender is half a person tall: a blade reaches it from nearer than the step's reach (ContinuousReview.Oriel.cs).
                    reach = TenderReach(actor, chased, reach);
                    bool drawing = hide != null && difference.magnitude > Mathf.Max(.4f, hide.radius - 1f);
                    // On foot, reach is to the body and not to its middle. The Floodcrown is wider than a blade is long: the lead stood against its flank for a whole round,
                    // never within 2.3 m of its centre and so never swinging (the Walking Canopy, November 13). A body no wider than chapters 1 and 2's is measured as it was.
                    float away = chased != null && !actor.flight ? Mathf.Min(difference.magnitude, BodyGap(chased, actor.transform.position) + NarrowBody) : difference.magnitude;
                    if (hide == null && away <= reach) arrived = true;
                    if (step.approachTarget) inReach = chased != null && away <= reach;
                    seekingBoss = lost != null && difference.magnitude > LostBossBeyond;
                    if (step.approachTarget ? chased != null && !inReach || drawing || seekingBoss : !arrived)
                    {
                        var heading = step.approachPartner && chased == null && !actor.flight ? ReviveHeading(actor, goal, difference)
                            : step.findWay && chased == null && hide == null && !actor.flight ? ReviveHeading(actor, goal, difference, false)
                            : chased != null && !actor.flight ? ChaseHeading(actor, chased, difference) : difference;
                        // In flight, a walk or a chase with something solid in the way goes round it (ContinuousReview.Cover.cs).
                        if (actor.flight && !step.approachPartner && (chased != null || !step.approachTarget)) heading = WalkRound(actor, goal, heading, chased != null ? reach : 0);
                        var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * heading.normalized;
                        direction = new Vector2(local.x, local.z) * step.magnitude;
                    }
                    else if (chased != null && actor.flight && difference.sqrMagnitude > .01f && Vector3.Angle(actor.motor.Facing, difference) > 5)
                    {
                        // A roll changes the nose. At firing range, keep a small
                        // ordinary stick correction toward the visible target;
                        // otherwise a chase resumes firing sideways after a roll.
                        var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * difference.normalized;
                        // .25 clears the Input System's radial dead zone and the
                        // motor's facing threshold. Release once aligned, so
                        // staying at range does not become a slow forward thrust.
                        direction = new Vector2(local.x, local.z) * .25f;
                    }
                    // The Debris Cutter keeps its distance among its lanes: flown after with the brake off and fired on wherever its shots reach (ContinuousReview.Home.cs).
                    if (step.approachTarget && CutterHeading(actor, chased, difference, out cutterFlying, out bool cutterFiring) is Vector2 cutter) { direction = cutter; inReach = cutterFiring; }
                    // Meret's craft is fought from the open lane in front of her shelter (ContinuousReview.Meret.cs).
                    if (step.approachTarget && MeretShipHeading(actor, chased, out bool meretFlying, out bool meretFiring) is Vector2 meretShip) { direction = meretShip; inReach = meretFiring; arrived |= meretFiring; cutterFlying |= meretFlying; }
                    // A guardian is plated to the front: a blow from inside that arc lands at a fifth. The chase stood in front of a Drowned Sentry for two minutes, swung
                    // two hundred and seventy times and took 39 off it (the Drowned Waystation, November 13). A person goes round it, close, toward the nearer flank, and
                    // swings once well past the plate's edge: no swing is pressed until then.
                    if (chased != null && !actor.flight && Plated(chased, actor.transform.position, out var round))
                    {
                        var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * round;
                        direction = new Vector2(local.x, local.z); inReach = false;
                    }
                    // A boss nothing of which can be hurt, in a room with a sluice's seat ring showing: a person goes and turns the wheel (ContinuousReview.Rooms.cs).
                    var wheel = step.approachTarget && !actor.flight ? WheelToWork(actor) : null;
                    if (wheel != null) { direction = WorkWheel(actor, wheel); inReach = false; }
                    // The Last Warden's road held at a line, its clamps' seats lit: a person goes and takes a clamp's weight, beside its fibre (ContinuousReview.Oriel.cs).
                    var clamp = step.approachTarget && !actor.flight ? ClampToWork(actor) : null;
                    if (clamp != null) { direction = WorkClamp(actor, clamp); inReach = false; }
                    // The Foundry Saint: a planted arm's collar struck, and with three arms down the climb to the press's back (ContinuousReview.Home.cs).
                    var saintArm = step.approachTarget && !actor.flight ? SaintArmToWork(actor) : null;
                    if (saintArm != null) { direction = WorkSaint(actor, saintArm); inReach = false; arrived |= saintAtWork; }
                    // What a shut boss's bar names, standing beyond the lock's reach: walked toward until the lock can take it (ContinuousReview.Chase.cs).
                    var named = step.approachTarget && !actor.flight && wheel == null && clamp == null && saintArm == null ? TowardNamed(actor) : null;
                    if (named.HasValue) { direction = named.Value; inReach = false; }
                    // Meret's gantry with an arm overreached: a person goes and takes the collar's weight at its lit seat (ContinuousReview.Mooring.cs).
                    if (!actor.flight) direction = MooringHeading(actor, step, direction, ref inReach);
                    // An ordinary fight with nothing in the lock: toward its nearest awake body until the lock takes it (ContinuousReview.Chase.cs).
                    var toward = step.approachTarget && !actor.flight && chased == null && hide == null && lost == null && wheel == null && clamp == null && saintArm == null && !named.HasValue ? TowardFight(actor) : null;
                    if (toward.HasValue) { direction = toward.Value; inReach = false; }
                    // In flight the same, brake off, for a body of the fight beyond the lock (ContinuousReview.Tollkeeper.cs).
                    if (step.approachTarget && actor.flight && chased == null && lost == null && FlightTowardFight(actor) is Vector2 fly) { direction = fly; inReach = false; seekingBoss = true; }
                    // In flight, the lock on something behind cover, every shot sparking off a hull: the lock flicked to a body in the open, else aside for a line (ContinuousReview.Cover.cs).
                    if (CoverHeading(actor, step.approachTarget ? chased : null, inReach) is Vector2 aside) { direction = aside; inReach = false; seekingBoss = true; }
                }
                // What the driver does of its own accord, where the route lets it (autoHeal, useItem, useExtra). With the partner down and no Spike to raise them, it is
                // the lead's own feet: to their side and still, as the game asks (2 m for 2 s), with the step's attack waiting and a dodge not. Never in place of a walk
                // to a point, whose seconds are counted for the walk.
                bool free = HandsFree(party) && !AtTheWheel(party) && !AtAClamp(party) && !AtACoupling(party), raising = false;
                if (free) Think(party);
                if (free && raise != null && !raise.Health.Alive && (!step.navigate || step.approachTarget) && RaiseWithin(party.Active, raise))
                {
                    var actor = party.Active;
                    Vector3 gap = raise.transform.position - actor.transform.position; gap.y = 0;
                    raising = true; direction = Vector2.zero;
                    if (gap.magnitude > ReviveStand)
                    {
                        var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * ReviveHeading(actor, raise.transform.position, gap).normalized;
                        direction = new Vector2(local.x, local.z);
                    }
                }
                bool evading = false;
                var lead = party != null ? party.Active : null;
                if (step.evadeTelegraphs && lead != null && lead.target != null && lead.target.Alive)
                {
                    // Something the boss's bar names (a lure-stalk) is struck under that boss's tells (ContinuousReview.Chase.cs).
                    var carrier = BarTells(lead, TellsOf(lead.target.reactionOwner != null ? lead.target.reactionOwner : lead.target));
                    var boss = carrier.GetComponent<BossController>();
                    var threat = carrier.GetComponent<EnemyBrain>();
                    var offset = lead.transform.position - carrier.transform.position; offset.y = 0;
                    evading = boss != null && boss.Telegraphing && boss.TelegraphRemaining < .16f * Time.timeScale && offset.sqrMagnitude < 18 * 18 ||
                        threat != null && threat.Telegraphing && threat.TelegraphRemaining < .16f * Time.timeScale && offset.sqrMagnitude < 5 * 5;
                    if (evading)
                    {
                        // Read the visible tell and send an ordinary lateral dodge.
                        // Release at the end of the tell; do not call actor actions
                        // or alter damage, cooldowns, time, or the fight's outcome.
                        // Never off the ground the lead stands on: the other side, or through what he is striking, where a roll aside would leave a plinth's top (ContinuousReview.Chase.cs).
                        var lateral = RollWay(lead, Vector3.Cross(Vector3.up, offset.normalized));
                        var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * lateral;
                        direction = new Vector2(local.x, local.z);
                    }
                }
                // In flight a marked lane is read whoever laid it. Only the locked target's tell was watched, and only inside 5 m, so each of a wing of strafers ran its
                // marked lane through a ship that never left it (chapter 3's first barge: eight passes in a minute, 250 of the lead's 300). A pilot rolls out of a
                // marking sideways as its tell runs out. A boss's markings stay with the rule above, which its fights were timed with.
                if (step.evadeTelegraphs && !evading && lead != null && lead.flight && lead.Health.Alive && !lead.Recovering && !GameTime.Paused && !GameInput.Current.Blocked)
                {
                    var here = lead.transform.position;
                    foreach (var marking in Telegraph.Live)
                    {
                        if (marking == null || marking.Released || !(marking.owner is Component layer) || layer == null || !marking.Contains(here)) continue;
                        if (marking.Remaining > .3f * Time.timeScale || layer.GetComponentInParent<BossController>() != null) continue;
                        var body = layer.GetComponentInParent<Health>(); if (body == null || body.friendly) continue;
                        var way = marking.WayOut(here); way.y = 0; if (way.sqrMagnitude < .0001f) continue;
                        var local = Quaternion.Euler(0, -ZoneController.Current.CameraProfile.yaw, 0) * way.normalized;
                        direction = new Vector2(local.x, local.z); evading = true; break;
                    }
                }
                // A boss nobody strikes is locked by nobody, so the rule above never read its tells: its markings are rolled out of as they land (ContinuousReview.Mooring.cs).
                if (step.evadeTelegraphs && !evading && lead != null && OutOfAnUnstruckBoss(lead) is Vector2 unstruck) { direction = unstruck; evading = true; }
                // Meret's markings that come from no move of hers (her shield's answer, a clamp closing under a hero who stood still) are read off the floor too (ContinuousReview.Meret.cs).
                if (step.evadeTelegraphs && !evading && lead != null && OutOfMeretsMarking(lead) is Vector2 meretWay) { direction = meretWay; evading = true; }
                if(step.evadeProjectiles&&lead!=null&&lead.flight&&lead.Health.Alive&&!lead.Recovering&&
                    !GameTime.Paused&&!GameInput.Current.Blocked)
                {
                    var escape=IncomingShotDirection(lead);
                    if(escape.HasValue)
                    {
                        evading=true;
                        var local=Quaternion.Euler(0,-ZoneController.Current.CameraProfile.yaw,0)*escape.Value;
                        direction=new Vector2(local.x,local.z);
                    }
                }
                held = new GamepadState { leftStick = Vector2.ClampMagnitude(direction, 1), leftTrigger = seekingBoss || cutterFlying ? 0 : step.leftTrigger, rightTrigger = step.rightTrigger };
                // Swap, half a second apart, until the hero named leads: as ever, but it gives up with its reason behind a hero who can't lead, and brings a
                // resting one out through the Party page where the game allows it (Ensure, in ContinuousReview.Party.cs).
                held = Ensure(held, party);
                // And the lead the route named, taken back once he has fallen and been raised (D429, ContinuousReview.Party.cs).
                // Asked every frame, since it is also where his fall is seen: asked only between a raise's frames and a dodge's, it never saw him down.
                held = TakeBack(held, party, free && !evading && !raising);
                if (evading) held = held.WithButton(GamepadButton.East);
                bool press = step.pulseSeconds <= 0 || (clock.Elapsed.TotalSeconds - stepStart) % step.pulseSeconds < .1;
                // Once there it stays there: the press can take the row away, by moving a part or leaving the screen, and a d-pad held after that would cycle the quick item in the world.
                if (!string.IsNullOrEmpty(step.pickRow) && !rowReached)
                {
                    if (OnRow(step.pickRow)) rowReached = true;
                    else
                    {
                        press = false;
                        // A row on a later page: on the pager, turn the page once and go on down, which wraps to the new page's top. A route that counted presses
                        // broke whenever a recipe was added above its row (the bench's filter row and the trophies' parts, November 10).
                        // A stop on Sela's chart ("Stop_Lanternwick") isn't in a list to walk down: the chart says which way a thumb goes next to reach it, by the cursor's own links.
                        bool on = (clock.Elapsed.TotalSeconds - stepStart) % .24 < .12, pager = OnRow("Pager");
                        if (on && !pickHeld) { pickHeld = true; pickButton = pager && !pickPaged ? GamepadButton.South : RouteChart.Toward(step.pickRow) ?? GamepadButton.DpadDown; if (pickButton == GamepadButton.South) pickPaged = true; }
                        if (!on) pickHeld = false;
                        if (!pager && !on) pickPaged = false;
                        if (on) held = held.WithButton(pickButton);
                    }
                }
                // Nothing to chase after a kill: a person presses lock and looks for the next one, where this stood idle while the last scavenger shot at it.
                if (step.approachTarget && party != null && party.Active != null && (party.Active.target == null || !party.Active.target.Alive) && (clock.Elapsed.TotalSeconds - stepStart) % 1 < .12)
                    held = held.WithButton(GamepadButton.LeftShoulder);
                // A stock step's presses are counted against the pouch, not the clock: on the row, one at a time, until it holds its number or one buys nothing.
                if ((stockItem != null || stockPart != null) && rowReached) press = StockPress();
                // A lone Swap press that names who leads afterwards means "take the lead as them": pressed only while somebody else leads. Pressed blind it handed
                // the lead away whenever an earlier fight had gone differently (Taren stopped falling in the haul, so the outpost save led with him: November 13).
                if (press && step.buttons != null && step.buttons.Length == 1 && string.Equals(step.buttons[0], "North", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(step.pauseUi)
                    && !string.IsNullOrEmpty(step.expectedCharacter) && party != null && party.Active != null && party.Active.character == step.expectedCharacter) press = false;
                if (!evading && !raising && press && inReach && step.buttons != null) foreach (var button in step.buttons)
                {
                    if (!Enum.TryParse<GamepadButton>(button, true, out var parsed)) throw new InvalidOperationException("Invalid replay button " + button);
                    held = held.WithButton(parsed);
                }
                // In a sluice's seat: the wheel, by the interact button (ContinuousReview.Rooms.cs). A body that takes nothing with a part beside it that can be hurt: the lock flicked across.
                if (wheelPress && !evading && !raising) held = held.WithButton(GamepadButton.South);
                if (step.approachTarget && !evading && party != null && party.Active != null && !party.Active.flight && SwitchToPart(party.Active)) held = held.WithButton(GamepadButton.DpadRight);
                // In a clamp's seat: its weight, by the interact button; a Clamp Tender up: the lock flicked across to it (ContinuousReview.Oriel.cs).
                if (clampPress && !evading && !raising) held = held.WithButton(GamepadButton.South);
                if (saintPress && !evading && !raising) held = held.WithButton(GamepadButton.South);
                if (step.approachTarget && !evading && party != null && party.Active != null && !party.Active.flight && TenderToStop(party.Active)) held = held.WithButton(GamepadButton.DpadRight);
                // What the boss's bar says to strike, the lock flicked round to it: an opener while it is shut, its body while it is open (ContinuousReview.Chase.cs).
                if (step.approachTarget && !evading && party != null && party.Active != null && ChaseFlick(party.Active) is GamepadButton flick) held = held.WithButton(flick);
                // In flight, the lock flicked off something behind cover to a body in the open (ContinuousReview.Cover.cs).
                if (step.navigate && step.approachTarget && !evading && CoverFlick is GamepadButton coverPress) held = held.WithButton(coverPress);
                held = Hands(held, free, evading);
                // Hands on a clamp: the pad let go, whatever the step says, until it opens (ContinuousReview.Oriel.cs).
                if (AtAClamp(party) && !MotherLetsGo(party, evading)) held = default;
                // Hands on a tread-hall coupling's jack: the pad let go until it is mated (ContinuousReview.Home.cs).
                if (AtACoupling(party)) held = default;
                // Chapter 10's fights read off the screen: hold fire under Meret's dome, her collars, her guard, a tender on the Tollkeeper's arm (ContinuousReview.Mooring.cs).
                held = MooringHands(held, party, evading, raising);
                // Under Meret's dome, the lock to a lit relay of hers (ContinuousReview.Meret.cs).
                held = MeretHands(held, party, evading);
                // A talk up in the middle of a fight: read through, and nothing else pressed (ContinuousReview.Chase.cs).
                if (TalkInAFight(out var reading)) held = reading;
                if(interaction==null&&press&&step.buttons?.Length>0&&!string.IsNullOrEmpty(step.expectedUi))
                {
                    interaction=new InteractionResponse{step=stepIndex,expectedUi=step.expectedUi,inputSeconds=clock.Elapsed.TotalSeconds};
                    interactions.Add(interaction);
                }
                pad.MakeCurrent();
            }
            if (pad != null) InputSystem.QueueStateEvent(pad, held);
        }
        /// <summary>Which way to push the stick toward a fallen hero on foot: along the baked floor's path where there is one, and sideways a while when the hero has stopped gaining,
        /// alternating sides, since a body standing in the way isn't in the bake. Only the stick: the walk is still the motor's own.</summary>
        Vector3 ReviveHeading(CombatActor actor, Vector3 goal, Vector3 straight, bool sidestep = true)
        {
            double now = clock.Elapsed.TotalSeconds; var at = actor.transform.position; var heading = straight;
            if (GroundNavigation.Current != null)
            {
                // The lead's end on his own ground (ContinuousReview.Chase.cs): the nearest navigation to a body in the gap behind a plinth is the plinth's top. The goal on its own
                // ground only when it is a body (the fallen hero: sidestep); a findWay walk's point, a wheel's or a collar's spot is a plan point, taken as it always was.
                if (now - revivePathAt > .25 || (goal - revivePathGoal).sqrMagnitude > .25f) { revivePathAt = now; revivePathGoal = goal; revivePath ??= new UnityEngine.AI.NavMeshPath(); reviveCorners = (chaseHandsOff ? GroundNavigation.Current.FindPath(at, goal, goal, revivePath) : FloorPath(at, goal, revivePath, out _, out _, sidestep)) ? revivePath.corners : null; }
                if (reviveCorners != null) foreach (var corner in reviveCorners) { var gap = corner - at; gap.y = 0; if (gap.magnitude > .6f) { heading = gap; break; } }
            }
            // A way round a wall leads away from its end for a while: only a walk to a body (which the bake doesn't hold) steps aside when the straight gap stops closing.
            if (!sidestep) { heading.y = 0; return heading; }
            float distance = straight.magnitude;
            if (distance < reviveBest - .25f) { reviveBest = distance; reviveGainedAt = now; }
            else if (now - reviveGainedAt > .7 && now >= detourUntil) { detourUntil = now + .8; detourSide = -detourSide; reviveGainedAt = now; }
            heading.y = 0;
            if (now < detourUntil && heading.sqrMagnitude > .0001f) heading = Vector3.Cross(Vector3.up, heading.normalized) * detourSide + heading.normalized * .3f;
            return heading;
        }
        /// <summary>A chase on foot that has stopped closing finds a way. The straight push at a target on raised ground (a lure-stalk on its plinth, whose ramp faces the
        /// hall's middle) stood against the plinth's back for forty seconds while the Rotbloom struck the lead down to an eighth of his bar, twice running (first whole chain,
        /// November 14; the one earlier pass had happened to come at it from the ramp's side). A person walks round to the ramp: the baked floor's way to the target for a few
        /// seconds, then straight again, and round again if that still doesn't close.</summary>
        const float ChaseGain = .5f, ChaseStall = 1f, ChaseByPath = 4f;
        Health chaseWho; Vector3 chaseFrom; double chaseSince = -1, chaseByPathUntil = -1;
        bool ChaseBlocked(CombatActor actor, Health chased)
        {
            double now = clock.Elapsed.TotalSeconds; var at = actor.transform.position;
            if (chased != chaseWho) { chaseWho = chased; chaseFrom = at; chaseSince = now; chaseByPathUntil = -1; return false; }
            if (now < chaseByPathUntil) return true;
            var moved = at - chaseFrom; moved.y = 0;
            if (moved.magnitude > ChaseGain) { chaseFrom = at; chaseSince = now; return false; }
            if (now - chaseSince < ChaseStall) return false;
            chaseFrom = at; chaseSince = now; chaseByPathUntil = now + ChaseByPath; return true;
        }
        /// <summary>How far off a fighting boss is looked for when there is nothing to chase, and how far off it has to be before the chase goes after it: just inside the
        /// thirty metres the game locks from.</summary>
        const float LostBossWithin = 240, LostBossBeyond = 26;
        /// <summary>How far past the edge of a guardian's plate the lead goes before it swings (the guardian turns after it, about 85 degrees a second, for as long as the swing
        /// takes), and how near it keeps while it goes round: close enough that a run outpaces the turn.</summary>
        const float FlankPast = 35, FlankAt = 2f;
        /// <summary>Is the lead in front of a guardian's raised plate? If so, <paramref name="round"/> is the way a person goes instead of swinging at it: along the circle about
        /// it toward the nearer flank, closing to <see cref="FlankAt"/>. Not while its plate is down (broken, or its long move just made): then it is hit where it stands.</summary>
        static bool Plated(Health target, Vector3 from, out Vector3 round)
        {
            round = default;
            var owner = target.reactionOwner != null ? target.reactionOwner : target;
            if (!owner.TryGetComponent<EnemyBrain>(out var brain) || brain.definition == null || !(brain.Behaviour is EnemyGuardian guard) || !guard.Guarding) return false;
            var off = from - owner.transform.position; off.y = 0; float distance = off.magnitude; if (distance < .05f) return false;
            float arc = brain.definition.tune.arc > 0 ? Mathf.Clamp(brain.definition.tune.arc, 10, 340) : 150;
            float turned = Vector3.SignedAngle(owner.transform.forward, off, Vector3.up);
            if (Mathf.Abs(turned) > arc * .5f + FlankPast) return false;
            var along = Vector3.Cross(Vector3.up, off / distance) * (turned >= 0 ? 1 : -1);
            var inward = -(off / distance) * Mathf.Clamp(distance - FlankAt, -1, 1);
            round = (along + inward * .7f).normalized; return true;
        }
        /// <summary>Half the width of the widest body whose middle a blade reaches (D258's 1.4 m): a gap to a body is counted as if it were this thick, so nothing narrower changes.</summary>
        const float NarrowBody = .7f;
        Health gapOf; Collider[] gapShapes;
        /// <summary>How far, on the plan, a point is from the nearest part of what is chased that a blade can land on: the shapes its hurtboxes are on (a weak spot's knot, a
        /// gate's slab, a body's capsule), or any hurtbox of the body it is part of when it has none of its own. A sensor or a floor trigger on the same body is no part of it.
        /// With no hurtbox anywhere the answer is "nowhere near", and the reach falls back to the middle.</summary>
        float BodyGap(Health target, Vector3 from)
        {
            if (!ReferenceEquals(target, gapOf))
            {
                gapOf = target; var shapes = new List<Collider>();
                var root = target.reactionOwner != null ? target.reactionOwner : target;
                var boxes = root.GetComponentsInChildren<Hurtbox>();
                foreach (var box in boxes) if (box != null && box.owner == target) shapes.AddRange(box.GetComponents<Collider>());
                if (shapes.Count == 0) foreach (var box in boxes) if (box != null) shapes.AddRange(box.GetComponents<Collider>());
                gapShapes = shapes.ToArray();
            }
            float least = float.PositiveInfinity;
            foreach (var shape in gapShapes)
            {
                if (shape == null || !shape.gameObject.activeInHierarchy) continue;
                Vector3 near;
                if (shape is CharacterController body)
                {
                    // A character's capsule answers no closest-point query, enabled or not: its middle and its radius.
                    var middle = body.transform.TransformPoint(body.center) - from; middle.y = 0;
                    var scale = body.transform.lossyScale;
                    least = Mathf.Min(least, Mathf.Max(0, middle.magnitude - body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)))); continue;
                }
                if (!shape.enabled || shape is MeshCollider mesh && !mesh.convex || !(shape is BoxCollider || shape is SphereCollider || shape is CapsuleCollider || shape is MeshCollider)) continue;
                near = shape.ClosestPoint(from) - from; near.y = 0;
                least = Mathf.Min(least, near.magnitude);
            }
            return least;
        }
        /// <summary>The nearest hide with something under it (its ring is showing), within what a person standing in the fight can see of the floor.</summary>
        static AmbushSpot HideToDraw(Vector3 from)
        {
            AmbushSpot best = null; float least = 32 * 32;
            foreach (var spot in AmbushSpot.All)
            {
                if (spot == null || !spot.Shown) continue;
                var gap = spot.transform.position - from; gap.y = 0;
                if (gap.sqrMagnitude < least) { least = gap.sqrMagnitude; best = spot; }
            }
            return best;
        }
        static Vector3? IncomingShotDirection(CombatActor actor)
        {
            var camera=Camera.main;if(camera==null)return null;
            float radius=actor.GetComponent<CharacterController>().radius+.3f,nearest=float.PositiveInfinity;
            float motionScale=actor.MotorDelta/Mathf.Max(.000001f,Time.unscaledDeltaTime);
            Vector3? escape=null;
            foreach(var shot in Projectile.Live)
            {
                if(!shot.Threatens(actor.Health.friendly))continue;
                var visible=camera.WorldToViewportPoint(shot.transform.position);
                if(visible.z<=0||visible.x<0||visible.x>1||visible.y<0||visible.y>1)continue;
                var velocity=shot.Velocity*Time.timeScale-actor.motor.Velocity*motionScale;velocity.y=0;
                if(velocity.sqrMagnitude<.01f)continue;
                var offset=actor.transform.position-shot.transform.position;offset.y=0;
                float time=Vector3.Dot(offset,velocity)/velocity.sqrMagnitude;
                if(time<0)continue;
                var miss=offset-velocity*time;if(miss.sqrMagnitude>radius*radius)continue;
                time=Mathf.Max(0,time-Mathf.Sqrt(Mathf.Max(0,radius*radius-miss.sqrMagnitude)/velocity.sqrMagnitude));
                if(time>.35f||time>=nearest)continue;
                nearest=time;var across=Vector3.Cross(Vector3.up,velocity.normalized);
                escape=miss.sqrMagnitude>.01f&&Vector3.Dot(miss,across)<0?-across:across;
            }
            return escape;
        }
        void LateUpdate()
        {
            // A route plays at its own difficulty, Standard unless it names one, and never at whatever the save it continues was left at. Continue and a Retry both
            // replace the game, so it is held every frame, from the first: before the settle, when a zone a route starts in makes its enemies.
            if (route != null && Difficulty.TryNamed(route.difficulty, out var routeDifficulty)) Difficulty.Current = routeDifficulty;
            if (!recording) return;
            var actor = PartyController.Current != null ? PartyController.Current.Active : null;
            string observedUi = ObserveUi();
            double now = clock.Elapsed.TotalSeconds;
            segmentedTrace?.Sample(samples.Count, Time.frameCount, stepIndex, now);
            nativeGpu?.Mark(samples.Count,Time.frameCount,stepIndex,now);
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
            // By role, not members[1-index]; and a third body out, were there ever one, goes to the captures after the two (null with two: nothing is made).
            var partner = Partner(party);
            damageCapture?.Watch(actor!=null?actor.Health:null);damageCapture?.Watch(partner!=null?partner.Health:null);
            groundPose?.Record(samples.Count,Time.frameCount,now,stepIndex,actor,partner,Others(party,actor,partner));
            releaseGeometry?.Record(samples.Count,Time.frameCount,now,stepIndex,actor,partner,Camera.main,Others(party,actor,partner));
            FrameTiming timing = default;
            bool hasTiming = false;
            if (headroom || gpuClocks)
            {
                // Direct API access alongside ProfilerRecorder, not a second
                // hardware clock. The latest completed frame is asynchronous;
                // retain its timestamp so the analyzer can detect cached rows.
                FrameTimingManager.CaptureFrameTimings();
                hasTiming = FrameTimingManager.GetLatestTimings(1, frameTimings) > 0;
                if (hasTiming) timing = frameTimings[0];
            }
            if (gpuClocks) gpuClockSamples.Add(new GpuClockSample {
                sample = samples.Count, utcTicks = DateTime.UtcNow.Ticks, qpc = System.Diagnostics.Stopwatch.GetTimestamp(),
                windowsQpc = WindowsClock(false), frameStart = timing.frameStartTimestamp,
                present = timing.cpuTimePresentCalled, complete = timing.cpuTimeFrameComplete,
                mainMs = timing.cpuMainThreadFrameTime, renderMs = timing.cpuRenderThreadFrameTime,
                presentWaitMs = timing.cpuMainThreadPresentWaitTime });
            samples.Add(new Sample {
                elapsed = now, ms = (float)((now - previous) * 1000), gameDelta = Time.deltaTime,
                player = actor != null ? actor.transform.position : Vector3.zero, camera = Camera.main.transform.position, forward = actor != null ? actor.transform.forward : Vector3.zero,
                reportedSpeed = actor != null ? actor.motor.Velocity.magnitude : 0, health = actor != null ? actor.Health.integrity : -1,
                partnerHealth = partner != null ? partner.Health.integrity : -1,
                partnerState = partner != null ? partner.State : ActorState.Idle, partnerAt = partner != null ? partner.transform.position : Vector3.zero,
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
                focus = Application.isFocused, paused = GameTime.Paused, blocked = GameInput.Current.Blocked, retry = retrying
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
            // A leg toward a point that names a stopWhen is finished by whichever comes first: flown until a radio call opens, it has not timed out on arriving.
            // (Every 'fly on until the hail' and 'walk on until the talk starts' of chapters 3 to 6 failed here on its first run in a player, November 13.)
            bool cutShort = endedByCondition && step.until == "arrived" && !string.IsNullOrEmpty(step.stopWhen) && Condition(step.stopWhen);
            if (!string.IsNullOrEmpty(step.until) && !Condition(step.until) && !cutShort) failures.Add(step.name + ": condition timeout " + step.until);
            // A chase that its outcome ended (the target fell first), or a walk a stopWhen cut short (the party fell on the
            // way), has nothing left to reach. Nor has a wait the machine ended before its point (ContinuousReview.Oriel.cs, WaitedOut).
            if (step.navigate && !arrived && !(endedByCondition && (step.approachTarget || !string.IsNullOrEmpty(step.stopWhen) || WaitedOut(step)))) failures.Add(step.name + ": navigation checkpoint missed");
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
        /// <summary>The cursor stands on this row: by the object's name (a save's slot), or the words it's found by starting so (a spare part, where a worn one's wearer comes first).
        /// A list row in columns is found by its key, which starts as its one string did (ListRow), so every route's pickRow finds the row it always found.</summary>
        static bool OnRow(string want)
        {
            var system = UnityEngine.EventSystems.EventSystem.current; var row = system != null ? system.currentSelectedGameObject : null;
            return ListRow.Found(row, want);
        }
        int savesAtStep;
        bool Condition(string name)
        {
            // The enemy locked when the step began has fallen.
            if (name == "targetDown") return stepTarget != null && !stepTarget.Alive;
            // A pickRow step is done once its row is focused, however far down or over however many pages it was.
            if (name == "rowReached") return rowReached;
            // The step's navigation goal is reached: a walk takes as long as it takes.
            if (name == "arrived") return arrived;
            // A stock step has nothing left to press: the pouch holds its number, or the last press bought nothing.
            if (name == "stocked") return Stocked;
            // A save was taken since the step began: a cradle pressed on the frame the party stops can be pressed before its prompt is in reach, and nothing
            // said so until the next route loaded a save from before the last talk (chapter-18's end, November 13).
            if (name == "saved") return SaveSystem.Asked > savesAtStep;
            if (name.StartsWith("flag:")) return GameServices.Current.Flags.GetBool(name.Substring(5));
            if (name.StartsWith("ui:")) return UiMatches(name.Substring(3), ObserveUi());
            if (name.StartsWith("scene:")) return !SceneFlow.Current.Loading && ActualScene == name.Substring(6);
            if (name.StartsWith("row:")) return OnRow(name.Substring(4));
            // That hero leads and the world has the pad: where a step that brings a hero out or swaps to them ends.
            if (name.StartsWith("lead:")) return Leads(name.Substring(5));
            // The way from the lead through these points is clear of what the room does on a clock, setting off now (ContinuousReview.Rooms.cs, ReplayWays).
            if (name.StartsWith("clear:")) return ClearAhead(name.Substring(6));
            // The Orrery: the gate nearest the lead on rail n is open in its moon's lee (ContinuousReview.Oriel.cs).
            if (name.StartsWith("lee:")) return InTheLee(name.Substring(4));
            // Chapter 10's berth: the party stands on it, or is back in the zone's own form beside it, with the change over (ContinuousReview.Berth.cs).
            if (name.StartsWith("docked:")) return DockedAt(name.Substring(7));
            if (name.StartsWith("launched:")) return LaunchedFrom(name.Substring(9));
            // The screen offers the lead a prompt with these words: somebody who has to walk to their place first (ContinuousReview.Home.cs).
            if (name.StartsWith("prompt:")) return Offered(name.Substring(7));
            var p = PartyController.Current;
            // Both by role (ContinuousReview.Party.cs): the hero beside the lead is down, and everybody out is standing, wherever in the party's array the lead is.
            return name switch {
                "partnerDown" => PartnerDown(p),
                "partyAlive" => PartyAlive(p),
                _ => throw new InvalidOperationException("Unknown replay condition " + name)
            };
        }
        void WriteEvidence()
        {
            if (samples.Count < 2) failures.Add("missing samples");
            // Reconnaissance runs behind other windows on purpose; the trace still says, frame by frame, when it had the focus.
            if (!reconnaissance) foreach (var s in samples) if (!s.focus) { failures.Add("focus lost"); break; }
            foreach (var s in samples) if (s.paused && !AllowedPause(s)) { failures.Add("simulation paused"); break; }
            if (Screen.width != 1920 || Screen.height != 1080) failures.Add("wrong resolution");
            if (capture != null && !string.IsNullOrEmpty(capture.Failure)) failures.Add(capture.Failure);
            if (nativeGpu != null && !string.IsNullOrEmpty(nativeGpu.Failure)) failures.Add(nativeGpu.Failure);
            if (groundPose != null && !string.IsNullOrEmpty(groundPose.Failure)) failures.Add(groundPose.Failure);
            if (releaseGeometry != null && !string.IsNullOrEmpty(releaseGeometry.Failure)) failures.Add(releaseGeometry.Failure);
            using (var writer = new StreamWriter(Path.Combine(folder, "frames.csv")))
            {
                writer.WriteLine("elapsed,ms,step,scene,hero,form,state,clip,animationTime,x,y,z,cameraX,cameraY,cameraZ,forwardX,forwardZ,pelvisYaw,chestYaw,reportedSpeed,integrity,focus,paused,blocked,gameDelta,gcCollections,gcBytes,mainThreadNs,renderThreadNs,batches,memoryBytes,dialogueLines,prompt,speaker,partnerHealth,partnerState,hudNs,hudBytes,audioVoices,sceneObjects,objects,activeCpuNs,activeRenderNs,gpuWorkNs,capWaitNs,ftmTimestamp,ftmGpuMs,ftmCpuMs,ui,retry,partnerX,partnerZ");
                foreach (var s in samples) writer.WriteLine(FormattableString.Invariant($"{s.elapsed:F6},{s.ms:F4},{s.step},{s.scene},{s.hero},{s.form},{s.state},{s.clip},{s.animationTime:F4},{(double)s.player.x:R},{(double)s.player.y:R},{(double)s.player.z:R},{(double)s.camera.x:R},{(double)s.camera.y:R},{(double)s.camera.z:R},{s.forward.x:F5},{s.forward.z:F5},{s.pelvisYaw:F3},{s.chestYaw:F3},{s.reportedSpeed:F4},{s.health:F1},{s.focus},{s.paused},{s.blocked},{s.gameDelta:F6},{s.collections},{s.allocation},{s.mainThread},{s.renderThread},{s.batches},{s.memory},{s.lines},\"{s.prompt.Replace("\"", "\"\"")}\",{s.speaker},{s.partnerHealth:F1},{s.partnerState},{s.hudNs},{s.hudBytes},{s.audioVoices},{s.sceneObjects},{s.objects},{s.activeCpu},{s.activeRender},{s.gpuWork},{s.capWait},{s.ftmTimestamp},{s.ftmGpuMs:F6},{s.ftmCpuMs:F6},{s.ui},{s.retry},{s.partnerAt.x:F3},{s.partnerAt.z:F3}"));
            }
            var report = new Report { route = route.name, sourceRevision = route.sourceRevision, build = Debug.isDebugBuild ? "Development" : "Release", unity = Application.unityVersion,
                cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                width = Screen.width, height = Screen.height, frameCap = Application.targetFrameRate, vSync = QualitySettings.vSyncCount,
                refreshHz = Screen.currentResolution.refreshRateRatio.value, samples = samples.Count, seconds = samples.Count > 0 ? samples[^1].elapsed : 0,
                releaseTraceVersion=releaseGeometry!=null?1:0, damageTraceVersion=damageCapture!=null?2:0, captured = capture != null, profiled=profiled, headroom=headroom, census=census, motion=motion, failures = failures.ToArray(), valid = !reconnaissance && failures.Count == 0,
                reconnaissance = reconnaissance, reconnaissancePassed = reconnaissance && failures.Count == 0,
                frameTimingRequested=headroom||gpuClocks,frameTimingEnabled=frameTimingEnabled,
                frameTimingCpuFrequency=headroom||gpuClocks?FrameTimingManager.GetCpuTimerFrequency():0,
                loadWaitSeconds=loadWaitSeconds,settleSeconds=route.settleSeconds,interactions=interactions.ToArray(),
                bossAttempts=bossAttempts.ToArray(),stocks=stocks.ToArray() };
            if(headroom||gpuClocks)report.gpuTiming=samples.Exists(s=>s.gpuWork>0)?"GPU Frame Time counter (nanoseconds; asynchronous)":"UNAVAILABLE (counter produced no positive samples)";
            File.WriteAllText(Path.Combine(folder, "run.json"), JsonUtility.ToJson(report, true));
            File.WriteAllText(Path.Combine(folder, "route.json"), JsonUtility.ToJson(route, true));
            if (gpuClocks)
            {
                using var clocks = new StreamWriter(Path.Combine(folder, "gpu-clocks.csv"));
                clocks.WriteLine("sample,utcTicks,qpc,cpuTimePresentCalled,cpuTimeFrameComplete,windowsQpc,frameStartTimestamp,cpuMainThreadFrameMs,cpuRenderThreadFrameMs,cpuMainThreadPresentWaitMs");
                foreach (var s in gpuClockSamples) clocks.WriteLine(FormattableString.Invariant($"{s.sample},{s.utcTicks},{s.qpc},{s.present},{s.complete},{s.windowsQpc},{s.frameStart},{s.mainMs:F6},{s.renderMs:F6},{s.presentWaitMs:F6}"));
                File.WriteAllText(Path.Combine(folder, "gpu-clocks.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    cleanTimingEligible = false, utcTickEpoch = "0001-01-01T00:00:00Z", ticksPerSecond = TimeSpan.TicksPerSecond,
                    qpcFrequency = System.Diagnostics.Stopwatch.Frequency, windowsQpcFrequency, cpuFrequency = FrameTimingManager.GetCpuTimerFrequency(),
                    gpuFrequency = FrameTimingManager.GetGpuTimerFrequency(), graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                    note = "The legacy qpc column is Stopwatch.GetTimestamp (process-relative in this Mono player), not raw Windows QPC. windowsQpc comes from QueryPerformanceCounter; -1 means unavailable. Check clock epochs before alignment. Raw diagnostic clocks never replace invalid GPU durations."
                }, Newtonsoft.Json.Formatting.Indented));
            }
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
            damageCapture?.Dispose();
            releaseGeometry?.Dispose();
            groundPose?.Dispose();
            StopProfile();
            segmentedTrace?.Stop();
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
