using System.Collections;
using System.Linq;
using Lattice.Core;
using Lattice.Combat;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Lattice.Tests.PlayMode
{
    public class CorePlayTests
    {
        Gamepad pad;
        InputSettings.BackgroundBehavior previous;
        [UnitySetUp]public IEnumerator Boot()
        {
            previous=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Time.timeScale=1;SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            GameServices.Current.State=new GameState();pad=InputSystem.AddDevice<Gamepad>();
            yield return Load("Arena_Ground");
        }
        IEnumerator Load(string scene)
        {
            SceneFlow.Current.LoadZone(scene);float until=Time.realtimeSinceStartup+8;
            while((SceneFlow.Current.Loading||PartyController.Current==null)&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"zone load timeout");Assert.AreEqual(scene,SceneFlow.Current.Zone);
            yield return new WaitForSecondsRealtime(.7f);
            foreach(var e in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))e.Passive=true;
            foreach(var a in PartyController.Current.members){a.GetComponent<PlayerBrain>().AutoPilot=true;a.GetComponent<PartnerBrain>().enabled=false;}
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
            InputSystem.settings.backgroundBehavior=previous;Time.timeScale=1;
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Press(Gamepad pad,GamepadButton button)
        {InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return null;yield return null;InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;}
        [UnityTest]public IEnumerator GroundFormMotorAndAttackKillAnEnemy()
        {
            var actor=PartyController.Current.Active;
            Assert.AreEqual(BodyForm.Shaped,actor.GetComponent<FormController>().Current);
            Assert.IsInstanceOf<GroundMotor>(actor.motor);Assert.IsTrue(GameInput.Current.Ground.enabled);
            var enemy=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)[0];
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=enemy.transform.position-Vector3.forward*2;cc.enabled=true;
            actor.motor.Move(Vector2.up,false,false);actor.target=enemy.Health;
            float until=Time.realtimeSinceStartup+5;
            while(enemy!=null&&enemy.Health.Alive&&Time.realtimeSinceStartup<until){actor.Attack();yield return new WaitForSecondsRealtime(.32f);}
            Assert.IsTrue(enemy==null||!enemy.Health.Alive,"real attack volumes must kill the enemy");Assert.Greater(actor.Kills,0);
        }
        [UnityTest]public IEnumerator FlightFormMotorFireAndEightMetreLunge()
        {
            yield return Load("Arena_Flight");var actor=PartyController.Current.Active;
            Assert.AreEqual(BodyForm.Flight,actor.GetComponent<FormController>().Current);Assert.IsInstanceOf<FlightMotor>(actor.motor);
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=new Vector3(0,1,-10);cc.enabled=true;
            var start=actor.transform.position;actor.motor.Dash(Vector3.forward,8);
            float until=Time.realtimeSinceStartup+.17f;while(Time.realtimeSinceStartup<until){actor.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.That(actor.transform.position.z-start.z,Is.InRange(7.5f,8.5f),"normal cruise clamp must not truncate a lunge");
            var enemy=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)[0];cc.enabled=false;actor.transform.position=enemy.transform.position-Vector3.forward*5;cc.enabled=true;
            actor.target=enemy.Health;actor.motor.Move(Vector2.up,false,true);until=Time.realtimeSinceStartup+5;
            while(enemy!=null&&enemy.Health.Alive&&Time.realtimeSinceStartup<until){actor.Attack();yield return new WaitForSecondsRealtime(.2f);}
            Assert.IsTrue(enemy==null||!enemy.Health.Alive,"pooled emitter projectiles must hit");
        }
        [UnityTest]public IEnumerator SwapChangesBrainAndReviveRequiresNearbyTime()
        {
            var party=PartyController.Current;var first=party.Active;Assert.IsTrue(party.Swap());Assert.AreNotSame(first,party.Active);
            Assert.IsFalse(first.GetComponent<PlayerBrain>().enabled);Assert.IsTrue(party.Active.GetComponent<PlayerBrain>().enabled);
            first.Health.integrity=0;var cc=first.GetComponent<CharacterController>();cc.enabled=false;first.transform.position=party.Active.transform.position+Vector3.right;cc.enabled=true;
            yield return new WaitForSecondsRealtime(1);Assert.IsFalse(first.Health.Alive);
            yield return new WaitForSecondsRealtime(1.3f);Assert.IsTrue(first.Health.Alive);
        }
        [UnityTest]public IEnumerator DockAndLockedWarpUseAdditiveZoneFlow()
        {
            var dock=new GameObject("TestDock").AddComponent<DockingPad>();dock.scene="Arena_Flight";dock.Interact();
            while(SceneFlow.Current.Loading)yield return null;
            Assert.AreEqual("Arena_Flight",SceneFlow.Current.Zone);Assert.IsTrue(SceneManager.GetSceneByName("_Boot").isLoaded);Assert.IsFalse(SceneManager.GetSceneByName("Arena_Ground").isLoaded);
            var warp=new GameObject("TestWarp").AddComponent<WarpBeacon>();warp.scene="Arena_Ground";warp.Interact();yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);Assert.AreEqual("Arena_Flight",SceneFlow.Current.Zone);
            GameServices.Current.Flags.SetBool("warpkey",true);warp.Interact();while(SceneFlow.Current.Loading)yield return null;
            Assert.AreEqual("Arena_Ground",SceneFlow.Current.Zone);Assert.IsTrue(GameInput.Current.Ground.enabled);Assert.IsTrue(dock==null,"departed zone objects must unload");
        }
        [UnityTest]public IEnumerator YarnPresentsLinesOptionsAndOpensBench()
        {
            var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=true;dialogue.StartNode("ArenaGuide","Hal");
            float until=Time.realtimeSinceStartup+8;
            while(dialogue.Running&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsFalse(dialogue.Running);Assert.GreaterOrEqual(dialogue.LinesPresented,2);
            var menu=Object.FindFirstObjectByType<PauseMenu>();Assert.IsTrue(menu.IsOpen,"selected Yarn option must execute bench command");Assert.IsTrue(menu.AtBench);menu.Close();
        }
        [UnityTest]public IEnumerator PadAdvancesTypewriterDialogueWithoutAutoAdvance()
        {
            var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=false;dialogue.StartNode("ArenaGuide","Hal");
            yield return new WaitForSecondsRealtime(.15f);
            for(int i=0;i<35&&dialogue.Running;i++){yield return Press(pad,GamepadButton.South);yield return new WaitForSecondsRealtime(.09f);}
            Assert.IsFalse(dialogue.Running,"short A taps must not disappear inside typewriter waits");
            Assert.GreaterOrEqual(dialogue.LinesPresented,2);var menu=Object.FindFirstObjectByType<PauseMenu>();Assert.IsTrue(menu.IsOpen);menu.Close();
        }
        [UnityTest]public IEnumerator PauseAllTabsKeepPadSelectionAndClose()
        {
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open();yield return null;
            Assert.IsNotNull(EventSystem.current.currentSelectedGameObject);
            for(int i=0;i<6;i++){yield return Press(pad,GamepadButton.RightShoulder);Assert.IsTrue(menu.IsOpen);Assert.IsNotNull(EventSystem.current.currentSelectedGameObject);}
            yield return Press(pad,GamepadButton.East);Assert.IsFalse(menu.IsOpen);Assert.IsFalse(GameInput.Current.Blocked);Assert.AreEqual(1,Time.timeScale);
        }
        [UnityTest]public IEnumerator EveryZoneAutosaveResumesThroughTheTitle()
        {
            var state=GameServices.Current.State;state.flags["resumeProbe"]=true;state.activeMember=1;state.party[1].level=3;
            foreach(string zone in new[]{"Hub_CinderHalo","Hub_Decks","Sorrel_Ridges","Gullet_Tunnel","TallowDrift"})
            {
                yield return Load(zone);var saved=GameServices.Current.Saves.Load("autosave");
                Assert.IsNotNull(saved);Assert.AreEqual(zone,saved.zone);Assert.AreEqual(2,saved.party.Count);
                SceneFlow.Current.LoadZone("Title");while(SceneFlow.Current.Loading)yield return null;yield return null;
                var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title);title.StartGame(true);
                float until=Time.realtimeSinceStartup+10;while(SceneFlow.Current.Loading&&Time.realtimeSinceStartup<until)yield return null;
                Assert.IsFalse(SceneFlow.Current.Loading);Assert.AreEqual(zone,SceneFlow.Current.Zone);
                Assert.AreEqual(2,PartyController.Current.members.Length);Assert.AreEqual("Sela",PartyController.Current.Active.character);
                Assert.AreEqual(3,GameServices.Current.State.party[1].level);Assert.IsTrue(GameServices.Current.Flags.GetBool("resumeProbe"));
            }
        }
        [UnityTest]public IEnumerator DefeatRetryRestoresTheSavedPartyWithoutAnotherDefeat()
        {
            var saved=GameServices.Current.State;
            foreach(var member in saved.party)member.integrity=Levels.MaxIntegrity(member.level);
            GameServices.Current.Saves.Save("autosave",saved);
            var defeated=PartyController.Current;
            foreach(var actor in defeated.members)actor.Health.integrity=0;
            yield return null;yield return null;
            Assert.IsTrue(GameTime.Paused);Assert.AreEqual("Retry",EventSystem.current.currentSelectedGameObject.name);
            yield return Press(pad,GamepadButton.South);
            float until=Time.realtimeSinceStartup+8;
            while((SceneFlow.Current.Loading||PartyController.Current==defeated)&&Time.realtimeSinceStartup<until)yield return null;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.IsFalse(SceneFlow.Current.Loading);Assert.AreNotSame(defeated,PartyController.Current);
            Assert.IsTrue(PartyController.Current.members.All(a=>a.Health.Alive),"departing dead actors must not overwrite the loaded save during the fade");
            Assert.IsFalse(GameTime.Paused);Assert.IsFalse(GameInput.Current.Blocked);
        }
        [UnityTest]public IEnumerator CantorHasMovingWeakPointSegmentsAndThreePhases()
        {
            yield return Load("Arena_Flight");
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(0,1,10));enemy.Passive=true;
            yield return null;yield return null;
            var weak=enemy.GetComponentsInChildren<Hurtbox>().Where(h=>h.multiplier==2).ToArray();Assert.GreaterOrEqual(weak.Length,4);
            var segment=enemy.transform.Find("WeakSegment_0");Assert.IsNotNull(segment);Vector3 before=segment.position;
            yield return new WaitForSecondsRealtime(.35f);Assert.Greater((segment.position-before).magnitude,.01f,"the serpent chain must deform in real frames");
            enemy.Health.integrity=enemy.Health.maximum*.25f;yield return null;
            Assert.AreEqual(3,enemy.GetComponent<BossController>().Phase);
        }
        [UnityTest]public IEnumerator PackLungeAndDrifterVolleyUseTheirRealAttackPatterns()
        {
            var actor=PartyController.Current.Active;var cc=actor.GetComponent<CharacterController>();
            cc.enabled=false;actor.transform.position=new Vector3(0,0,-10);cc.enabled=true;
            var hunter=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),new Vector3(0,0,-4.5f));
            float until=Time.realtimeSinceStartup+4;
            while(!hunter.Telegraphing&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsTrue(hunter.Telegraphing);Vector3 windup=hunter.transform.position;
            while(hunter.Telegraphing&&Time.realtimeSinceStartup<until)yield return null;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.Greater((hunter.transform.position-windup).magnitude,2.2f,"a pack attack must lunge, not merely resume walking");
            Object.Destroy(hunter.gameObject);yield return null;
            var drifter=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDrifter"),new Vector3(0,0,-2));
            until=Time.realtimeSinceStartup+4;
            while(!drifter.Telegraphing&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsTrue(drifter.Telegraphing);int before=Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;
            while(drifter.Telegraphing&&Time.realtimeSinceStartup<until)yield return null;
            Assert.GreaterOrEqual(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length,before+3,"a Drifter volley must create three live projectiles");
        }
    }
}
