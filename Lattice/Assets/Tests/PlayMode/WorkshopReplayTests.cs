using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class WorkshopReplayTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject fixture;
        ContinuousReview replay;
        Gamepad pad;
        static object Field(object obj, string name) => obj.GetType().GetField(name, Private).GetValue(obj);
        static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Private).SetValue(obj, value);
        static object Call(object obj, string name) => obj.GetType().GetMethod(name, Private).Invoke(obj, null);
        IList Samples => (IList)Field(replay, "samples");
        List<string> Failures => (List<string>)Field(replay, "failures");

        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset(); SceneManager.LoadScene("_Boot"); yield return null;
            yield return new WaitForSecondsRealtime(.8f);
            Assert.IsNull(PartyController.Current);
            fixture = new GameObject("Workshop replay fixture"); fixture.SetActive(false);
            replay = fixture.AddComponent<ContinuousReview>();
            pad = InputSystem.AddDevice<Gamepad>("WorkshopReplayRegression");
            Set(replay, "pad", pad);
            Set(replay, "route", new QualityRoute { scene = "Title", starterParty = false });
            Set(replay, "step", new QualityStep { name = "title", expectedUi = "title" });
            ((System.Diagnostics.Stopwatch)Field(replay, "clock")).Start();
            Call(replay, "InputUpdate"); yield return null; yield return null;
            Set(replay, "recording", true);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            if (fixture != null) Object.Destroy(fixture);
            GameTime.Reset(); SceneManager.LoadScene("_Boot"); yield return null;
            yield return new WaitForSecondsRealtime(.3f);
        }

        [UnityTest] public IEnumerator TitleConfirmStartsAnUnseededNewGame()
        {
            // The recorder must press the actual selected New Game button while
            // there is no party; no TitleScreen.StartGame or DevLoadout call.
            Set(replay, "step", new QualityStep { name = "New Game", buttons = new[] { "South" } });
            float deadline = Time.unscaledTime + 2;
            while (!SceneFlow.Current.Loading && Time.unscaledTime < deadline)
            { Call(replay, "InputUpdate"); yield return null; }
            Assert.IsTrue(SceneFlow.Current.Loading, "the recorder never submitted New Game on Title");
            Set(replay, "recording", false); Call(replay, "InputUpdate");
            deadline = Time.unscaledTime + 10;
            while (SceneFlow.Current.Loading && Time.unscaledTime < deadline) yield return null;
            Assert.AreEqual("Hub_CinderHalo", SceneFlow.Current.Zone);
            Assert.IsNotNull(PartyController.Current);
            var state = GameServices.Current.State;
            Assert.AreEqual(1, state.party.Count); Assert.AreEqual("Taren", state.party[0].id);
            Assert.AreEqual(1, state.party[0].level); Assert.AreEqual(120, state.scrip);
            Assert.AreEqual(3, state.consumables["RepairGel"]); Assert.IsEmpty(state.materials);
        }

        [UnityTest] public IEnumerator TitleAndSceneLoadingRemainInTheFrameRecord()
        {
            Call(replay, "LateUpdate");
            Assert.AreEqual(1, Samples.Count, "Title frames were silently dropped");
            Assert.AreEqual("Title", Samples[0].GetType().GetField("scene").GetValue(Samples[0]));
            Call(replay, "ValidateStep"); Assert.IsEmpty(Failures, "Title is valid without a party");
            GameServices.Current.NewGame(); SceneFlow.Current.LoadZone("Hub_CinderHalo");
            Call(replay, "LateUpdate");
            Assert.AreEqual(2, Samples.Count, "scene-loading frames were silently dropped");
            Assert.AreEqual("loading", Samples[1].GetType().GetField("ui")?.GetValue(Samples[1]));
            while (SceneFlow.Current.Loading) yield return null;
        }

        [UnityTest] public IEnumerator ActualBenchIsObservedAndWorldCannotMasqueradeAsAMenu()
        {
            GameServices.Current.NewGame(); SceneFlow.Current.LoadZone("Hub_Decks");
            while (SceneFlow.Current.Loading) yield return null;
            Set(replay, "route", new QualityRoute { scene = "Hub_Decks" });
            Set(replay, "step", JsonUtility.FromJson<QualityStep>("{\"name\":\"craft\",\"expectedUi\":\"bench\",\"pauseUi\":\"bench\"}"));
            var menu = Object.FindFirstObjectByType<PauseMenu>(); menu.Open(2, true);
            Call(replay, "LateUpdate");
            var last = Samples[Samples.Count - 1];
            Assert.AreEqual("bench", last.GetType().GetField("ui")?.GetValue(last), "pause declaration needs an independently observed menu");
            Call(replay, "ValidateStep"); Assert.IsEmpty(Failures);
            menu.Close(); yield return null;
            Call(replay, "ValidateStep");
            Assert.IsTrue(Failures.Exists(f => f.Contains("UI")), "declaring bench while in the world must reject");
        }

        [UnityTest] public IEnumerator UnknownUiExpectationDoesNotSilentlyPass()
        {
            GameServices.Current.NewGame(); SceneFlow.Current.LoadZone("Hub_Decks");
            while (SceneFlow.Current.Loading) yield return null;
            Set(replay, "route", new QualityRoute { scene = "Hub_Decks" });
            Set(replay, "step", new QualityStep { name = "mistyped expectation", expectedUi = "bnch" });
            Call(replay, "ValidateStep");
            Assert.IsTrue(Failures.Exists(f => f.Contains("UI")), "unknown UI expectations must fail closed");
        }
    }
}
