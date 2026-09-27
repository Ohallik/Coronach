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
        [UnityTest]public IEnumerator PausedCombatHoldsPendingHitsProjectilesAndActionClock()
        {
            var actor=PartyController.Current.Active;
            var enemy=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)[0];
            float health=enemy.Health.integrity;
            GameTime.Paused=true;float now=GameTime.Now;
            var packet=new DamagePacket{source=actor.Health,amount=10,type=DamageType.Pulse};
            var hit=CombatActor.Strike(enemy.transform.position+Vector3.up*.8f,2,packet);
            Projectile.Fire(new Vector3(0,3,-20),Vector3.forward,packet);
            var projectile=Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Last();
            var start=projectile.transform.position;
            yield return new WaitForSecondsRealtime(.35f);
            Assert.AreEqual(health,enemy.Health.integrity,"a queued hit must not apply during a menu/review pause");
            Assert.That(GameTime.Now,Is.EqualTo(now).Within(.001f));
            Assert.AreEqual(start,projectile.transform.position);
            Assert.IsNotNull(hit);Assert.IsFalse(actor.Attack());
            GameTime.Paused=false;yield return null;yield return null;
            Assert.Less(enemy.Health.integrity,health,"the pending hit must resolve after resume");
            Assert.AreNotEqual(start,projectile.transform.position);
        }
        [UnityTest]public IEnumerator GroundFormMotorAndAttackKillAnEnemy()
        {
            var actor=PartyController.Current.Active;
            Assert.AreEqual(BodyForm.Shaped,actor.GetComponent<FormController>().Current);
            Assert.IsInstanceOf<GroundMotor>(actor.motor);Assert.IsTrue(GameInput.Current.Ground.enabled);
            var enemy=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)[0];
            // The arena AI has already approached during scene setup. Behind its
            // incidental position can be inside a crystal: controller depenetration
            // then moves the fighter out of reach. Isolate this motor/contact check
            // in the clear southern lane; cover and retreat have separate checks.
            yield return new WaitForSecondsRealtime(.4f);
            var enemyController=enemy.GetComponent<CharacterController>();enemyController.enabled=false;
            enemy.transform.position=new Vector3(0,-.12f,-10);enemyController.enabled=true;enemy.Health.ResetFull();
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=enemy.transform.position-Vector3.forward*2;cc.enabled=true;
            Physics.SyncTransforms();
            actor.motor.Move(Vector2.up,false,false);actor.target=enemy.Health;
            float until=Time.realtimeSinceStartup+5;
            while(enemy!=null&&enemy.Health.Alive&&Time.realtimeSinceStartup<until){actor.Attack();yield return new WaitForSecondsRealtime(.32f);}
            Assert.IsTrue(enemy==null||!enemy.Health.Alive,"real attack volumes must kill the enemy");Assert.Greater(actor.Kills,0);
        }
        [UnityTest]public IEnumerator ReapplyingCurrentFormDoesNotRestartTheTransformation()
        {
            var actor=PartyController.Current.Active;var form=actor.GetComponent<FormController>();
            Assert.IsFalse(form.Shaping);Assert.AreEqual(BodyForm.Shaped,form.Current);
            form.Set(form.Current);yield return null;
            Assert.IsFalse(form.Shaping,"party refresh/swap must not shrink an already settled body into the same form");
            Assert.That(Vector3.Distance(form.shaped.transform.localScale,Vector3.one),Is.LessThan(.001f));
        }
        [UnityTest]public IEnumerator GroundContactWaitsForSwingAndDodgeCancelsIt()
        {
            // Setup disables partner brains after entering the arena. Let their
            // already-started wind-ups finish, then clear those unrelated shots.
            yield return new WaitForSecondsRealtime(.6f);
            foreach(var projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))Object.Destroy(projectile.gameObject);
            foreach(var hit in Object.FindObjectsByType<HitVolume>(FindObjectsSortMode.None))Object.Destroy(hit.gameObject);
            yield return null;
            var actor=PartyController.Current.Active;var enemy=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)[0];
            // Test cancellation/resume against a clear, explicitly faced body.
            // Incidental arena AI placement is not a defined contact fixture.
            var enemyController=enemy.GetComponent<CharacterController>();enemyController.enabled=false;
            enemy.transform.position=new Vector3(0,-.12f,-10);enemyController.enabled=true;
            enemy.Health.maximum=enemy.Health.integrity=1000;
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;
            actor.transform.SetPositionAndRotation(enemy.transform.position-Vector3.forward*2,Quaternion.identity);cc.enabled=true;
            Physics.SyncTransforms();
            actor.motor.Move(Vector2.up,false,false);actor.target=enemy.Health;actor.damage=10;
            Debug.Log($"RESUMED_MELEE setup actor={actor.transform.position:F5} enemy={enemy.transform.position:F5} overlaps={string.Join(",",Physics.OverlapSphere(actor.transform.position+Vector3.up,.6f).Where(c=>c.gameObject.isStatic).Select(c=>c.name))}");
            float before=enemy.Health.integrity;Assert.IsTrue(actor.Attack());
            yield return new WaitForSecondsRealtime(.06f);Assert.AreEqual(before,enemy.Health.integrity,"damage must wait for the swing contact");
            Assert.IsTrue(actor.Dodge(Vector3.right));yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual(before,enemy.Health.integrity,"a cancelled wind-up must not leave a ghost hit; the motor was deliberately not advanced");
            Assert.IsTrue(actor.Attack());GameTime.Paused=true;yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(before,enemy.Health.integrity,"the wind-up must freeze during pause");
            GameTime.Paused=false;yield return new WaitForSecondsRealtime(.28f);
            var posed=actor.GetComponentInChildren<Animator>();var phase=posed.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"RESUMED_MELEE finish actor={actor.transform.position:F5} enemy={enemy.transform.position:F5} gap={Vector3.Distance(actor.transform.position,enemy.transform.position):F5} hp={enemy.Health.integrity} state={actor.State} clip={actor.VisualAction} phase={phase.normalizedTime:F5} transitioning={posed.IsInTransition(0)}");
            Assert.Less(enemy.Health.integrity,before,"an uninterrupted resumed swing must connect");
        }
        [UnityTest]public IEnumerator EarlyComboTapQueuesTheNextDistinctSwing()
        {
            var actor=PartyController.Current.Active;actor.GetComponent<PlayerBrain>().AutoPilot=false;
            yield return Press(pad,GamepadButton.South);Assert.AreEqual("Attack1",actor.VisualAction);
            yield return new WaitForSecondsRealtime(.12f);yield return Press(pad,GamepadButton.South);
            yield return new WaitForSecondsRealtime(.22f);
            Assert.AreEqual("Attack2",actor.VisualAction,"a tap during recovery must survive until the second swing");
            var animation=actor.GetComponent<FormController>().shaped.GetComponent<GeneratedAnimator>();
            Assert.AreEqual("Attack2",animation.CurrentAnimation,"the second strike must select its own motion");
        }
        [UnityTest]public IEnumerator ShapedVanesAreAttachedToAnimatedBones()
        {
            // Allow actions started by the partner during zone entry to recover.
            yield return new WaitForSecondsRealtime(.6f);
            foreach(var actor in PartyController.Current.members)
            {
                var shaped=actor.GetComponent<FormController>().shaped;var animator=shaped.GetComponentInChildren<Animator>();
                var vanes=shaped.GetComponent<GeneratedVanes>();Assert.AreEqual(4,vanes.vanes.Length);
                foreach(var vane in vanes.vanes)
                    Assert.AreEqual(animator.GetBoneTransform(vane.name.Contains("Upper")?HumanBodyBones.Chest:HumanBodyBones.Hips),vane.parent,"vanes cannot remain at rest while the torso swings");
                Assert.IsTrue(actor.Attack());
            }
            yield return new WaitForSecondsRealtime(.2f);
            foreach(var actor in PartyController.Current.members)
                Assert.IsNotNull(actor.GetComponent<FormController>().shaped.GetComponentInChildren<Animator>());
        }
        [UnityTest]public IEnumerator FlightUsesDistinctHorizontalShipsAndReturnsToBiped()
        {
            yield return Load("Arena_Flight");
            foreach(var actor in PartyController.Current.members)
            {
                var definition=GameCatalog.Find<CharacterDef>(actor.character);var form=actor.GetComponent<FormController>();
                Assert.AreNotSame(definition.shaped,definition.flight,"flight must have its own ship mesh");
                Assert.IsNotNull(form.flight.GetComponent<FlightShipMotion>());
                Assert.IsTrue(form.flight.activeSelf);Assert.IsFalse(form.shaped.activeSelf);Assert.IsFalse(form.natural.activeSelf);
                Assert.Less(Vector3.Angle(form.flight.transform.up,Vector3.up),30,"flight mesh must stay horizontal instead of rotating a standing humanoid");
            }
            yield return Load("Arena_Ground");var ground=PartyController.Current.Active.GetComponent<FormController>();
            Assert.IsTrue(ground.shaped.activeSelf);Assert.IsFalse(ground.flight.activeSelf);
            Assert.That(ground.shaped.transform.localScale.x,Is.EqualTo(1).Within(.01f));
        }
        [UnityTest]public IEnumerator MusicFollowsTitleTownShopAndMoonWithInterruptedFades()
        {
            yield return Load("Hub_Decks");Assert.AreEqual("Hub Town Groove",MusicDirector.Current.Track);
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.IsTrue(MusicDirector.Current.ActiveSource.isPlaying);Assert.Greater(MusicDirector.Current.ActiveSource.timeSamples,0);
            var shop=Object.FindFirstObjectByType<ShopUi>();Assert.IsNotNull(shop);
            shop.Open(null);Assert.AreEqual("Moonbase Market",MusicDirector.Current.Track);
            yield return new WaitForSecondsRealtime(.15f);shop.Close();
            yield return new WaitForSecondsRealtime(.15f);shop.Open(null);shop.Close();
            Assert.AreEqual("Hub Town Groove",MusicDirector.Current.Track);
            yield return new WaitForSecondsRealtime(1.3f);Assert.IsTrue(MusicDirector.Current.ActiveSource.isPlaying);
            Assert.Greater(MusicDirector.Current.ActiveSource.volume,.4f);
            yield return Load("Sorrel_Ridges");ZoneController.Current.SafePocket(false);
            Assert.AreEqual("Adventure Awaits",MusicDirector.Current.Track);
            ZoneController.Current.SafePocket(true);Assert.AreEqual("Hub Town Groove",MusicDirector.Current.Track);
            SceneFlow.Current.LoadZone("Title");while(SceneFlow.Current.Loading)yield return null;yield return null;
            Assert.AreEqual("Title Theme",MusicDirector.Current.Track);
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
        [UnityTest]public IEnumerator ShortLungeTapSurvivesHeldFireRecovery()
        {
            yield return Load("Arena_Flight");var actor=PartyController.Current.Active;
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=new Vector3(200,1,-200);cc.enabled=true;
            actor.target=null;actor.GetComponent<PlayerBrain>().AutoPilot=false;
            var start=actor.transform.position;
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
            Assert.Greater(actor.AttackSequence,0,"held A must start emitter recovery");
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South).WithButton(GamepadButton.West));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));
            yield return new WaitForSecondsRealtime(.4f);
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
            Assert.Greater(actor.transform.position.z-start.z,7.5f,"a released X tap must still execute its lunge after fire recovery");
        }
        [UnityTest]public IEnumerator FlightPartnerKeepsUpWithBoostBeforeSwap()
        {
            yield return Load("Arena_Flight");var party=PartyController.Current;var leader=party.Active;var partner=party.members[1-party.index];
            for(int i=0;i<party.members.Length;i++)
            {
                var cc=party.members[i].GetComponent<CharacterController>();cc.enabled=false;
                party.members[i].transform.position=new Vector3(200+i*2,1,-200-i*3);cc.enabled=true;
            }
            leader.target=null;partner.GetComponent<PartnerBrain>().enabled=true;
            var start=partner.transform.position;float until=Time.realtimeSinceStartup+2.5f;
            while(Time.realtimeSinceStartup<until){leader.motor.Move(Vector2.up,true,false);yield return null;}
            Assert.Greater(partner.transform.position.z-start.z,25,"a following flight partner must not fly with the brake permanently held");
            Assert.Less((leader.transform.position-partner.transform.position).magnitude,18,"boost must not leave the swap partner an encounter behind");
            Vector3 before=leader.transform.position;Assert.IsTrue(party.Swap());
            Assert.Less((party.Active.transform.position-before).magnitude,18);
        }
        [UnityTest]public IEnumerator CompanionEvadesTheBurrowerTelegraph()
        {
            var party=PartyController.Current;Assert.IsTrue(party.Swap());
            var companion=party.members[1-party.index];var leader=party.Active;
            foreach(var actor in party.members)
            {
                actor.GetComponent<PlayerBrain>().AutoPilot=true;
                var cc=actor.GetComponent<CharacterController>();cc.enabled=false;
                actor.transform.position=new Vector3(200+(actor==leader?5:0),0,-200);cc.enabled=true;
            }
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Burrower"),new Vector3(200,0,-194));
            leader.target=enemy.Health;companion.GetComponent<PartnerBrain>().enabled=true;
            float until=Time.realtimeSinceStartup+10;bool dodged=false;
            while(Time.realtimeSinceStartup<until&&companion.Health.Alive)
            {
                if(companion.State==ActorState.Dodge){dodged=true;break;}
                yield return null;
            }
            Assert.IsTrue(dodged,"a companion must defend against a telegraphed attack instead of repeatedly trading wind-ups for damage");
            Assert.IsTrue(companion.Health.Alive);
        }
        [UnityTest]public IEnumerator OpeningMenuCancelsBufferedLunge()
        {
            yield return Load("Arena_Flight");var actor=PartyController.Current.Active;
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=new Vector3(200,1,-200);cc.enabled=true;
            actor.target=null;actor.GetComponent<PlayerBrain>().AutoPilot=false;Vector3 before=actor.transform.position;
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.West));yield return null;yield return null;
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open();InputSystem.QueueStateEvent(pad,new GamepadState());
            yield return new WaitForSecondsRealtime(.15f);menu.Close();yield return new WaitForSecondsRealtime(.4f);
            Assert.Less((actor.transform.position-before).magnitude,.1f,"opening UI must cancel a pending lunge instead of executing it after close");
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
            var actor=PartyController.Current.Active;actor.GetComponent<PlayerBrain>().AutoPilot=false;Vector3 before=actor.transform.position;
            yield return Press(pad,GamepadButton.East);Assert.IsFalse(menu.IsOpen);Assert.IsFalse(GameInput.Current.Blocked);Assert.AreEqual(1,Time.timeScale);
            Assert.AreNotEqual(ActorState.Dodge,actor.State,"B closing the menu must not leak into a gameplay dodge");
            Assert.Less((actor.transform.position-before).magnitude,.1f);
            yield return Press(pad,GamepadButton.East);
            Assert.AreEqual(ActorState.Dodge,actor.State,"a fresh B press after release must dodge normally");
        }
        [UnityTest]public IEnumerator EveryZoneAutosaveResumesThroughTheTitle()
        {
            var state=GameServices.Current.State;state.flags["resumeProbe"]=true;state.activeMember=1;state.party[1].level=3;
            foreach(string zone in new[]{"Hub_CinderHalo","Hub_Decks","Sorrel_Ridges","Gullet_Tunnel","TallowApproach","TallowDrift"})
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
        [UnityTest]public IEnumerator SafePocketTracksTeleportAndActiveMemberSwap()
        {
            yield return Load("Sorrel_Ridges");var party=PartyController.Current;
            var cc=party.Active.GetComponent<CharacterController>();cc.enabled=false;party.Active.transform.position=new Vector3(0,0,18);cc.enabled=true;
            yield return new WaitForSecondsRealtime(.4f);Assert.AreEqual(BodyForm.Natural,party.Active.GetComponent<FormController>().Current);
            cc.enabled=false;party.Active.transform.position=new Vector3(0,0,32);cc.enabled=true;
            yield return new WaitForSecondsRealtime(.4f);Assert.AreEqual(BodyForm.Shaped,party.Active.GetComponent<FormController>().Current);
            var partner=party.members[1-party.index];cc=partner.GetComponent<CharacterController>();cc.enabled=false;partner.transform.position=new Vector3(0,0,18);cc.enabled=true;
            Assert.IsTrue(party.Swap());partner.GetComponent<PlayerBrain>().AutoPilot=true;
            yield return new WaitForSecondsRealtime(.4f);Assert.AreEqual(BodyForm.Natural,party.Active.GetComponent<FormController>().Current);
        }
        [UnityTest]public IEnumerator FlightWallContactHurtsOnlyInCombatZones()
        {
            foreach(string zone in new[]{"Hub_CinderHalo","Arena_Flight"})
            {
                yield return Load(zone);var actor=PartyController.Current.Active;
                var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=new Vector3(200,1,-200);cc.enabled=true;
                var wall=new GameObject("Collision test wall",typeof(BoxCollider));wall.transform.position=new Vector3(203,1,-200);wall.GetComponent<BoxCollider>().size=new Vector3(1,6,20);
                Physics.SyncTransforms();float before=actor.Health.integrity,furthest=actor.transform.position.x;
                float until=Time.realtimeSinceStartup+2;
                while(Time.realtimeSinceStartup<until){actor.motor.Move(Vector2.right,true,false);furthest=Mathf.Max(furthest,actor.transform.position.x);yield return null;}
                Assert.Less(furthest,202.6f,"flight must collide with the wall in either mode");
                if(zone=="Hub_CinderHalo")Assert.AreEqual(before,actor.Health.integrity,"civil flight collisions must be harmless");
                else Assert.Less(actor.Health.integrity,before,"combat wall contact must cause damage");
                if(zone=="Arena_Flight")
                {
                    // The same solid collider becomes a combatant body: contact alone
                    // must not apply the wall's 12 damage to the player.
                    wall.AddComponent<Health>();
                    cc.enabled=false;actor.transform.position=new Vector3(200,1,-200);cc.enabled=true;
                    before=actor.Health.integrity;furthest=200;until=Time.realtimeSinceStartup+2;
                    while(Time.realtimeSinceStartup<until){actor.motor.Move(Vector2.right,true,false);furthest=Mathf.Max(furthest,actor.transform.position.x);yield return null;}
                    Assert.Greater(furthest,201,"the actor must actually reach the body collider");
                    Assert.AreEqual(before,actor.Health.integrity,"combatant bodies must not inflict wall collision damage");
                }
                Object.Destroy(wall);yield return null;
            }
        }
        [UnityTest]public IEnumerator DefeatRetryRestoresTheSavedPartyWithoutAnotherDefeat()
        {
            var saved=GameServices.Current.State;
            foreach(var member in saved.party)member.integrity=Levels.MaxIntegrity(member.level);
            GameServices.Current.Saves.Save("autosave",saved);
            var defeated=PartyController.Current;
            foreach(var actor in defeated.members)
                actor.Health.Receive(new DamagePacket{amount=actor.Health.maximum*100,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsFalse(GameTime.Paused,"collapse must finish before retry pauses the world");
            Assert.IsTrue(GameInput.Current.Blocked,"defeat must own input throughout collapse");
            float menuDeadline=Time.realtimeSinceStartup+3;
            while(!GameTime.Paused&&Time.realtimeSinceStartup<menuDeadline)yield return null;
            Assert.IsTrue(GameTime.Paused);Assert.AreEqual("Retry",EventSystem.current.currentSelectedGameObject.name);
            yield return Press(pad,GamepadButton.South);
            float until=Time.realtimeSinceStartup+8;
            while((SceneFlow.Current.Loading||PartyController.Current==defeated)&&Time.realtimeSinceStartup<until)yield return null;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.IsFalse(SceneFlow.Current.Loading);Assert.AreNotSame(defeated,PartyController.Current);
            Assert.IsTrue(PartyController.Current.members.All(a=>a.Health.Alive),"departing dead actors must not overwrite the loaded save during the fade");
            Assert.IsFalse(GameTime.Paused);Assert.IsFalse(GameInput.Current.Blocked);
        }
        [UnityTest]public IEnumerator GroundBossSettlesWhileWaitingInAttackRange()
        {
            // A charge can step onto scenery or a body. At ranged distance the
            // boss must still fall, otherwise ground shots pass beneath it.
            var actor=PartyController.Current.Active;var cc=actor.GetComponent<CharacterController>();
            cc.enabled=false;actor.transform.position=new Vector3(0,0,-20);cc.enabled=true;
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Burrower"),new Vector3(0,2,-12));
            // Physics uses scaled time; a companion Flash during setup can slow it.
            yield return new WaitForSeconds(.7f);
            Assert.Less(boss.transform.position.y,.2f,"gravity must run while a ground enemy waits or telegraphs, not only while chasing");
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
            // Count only this enemy's volley, not a partner shot started during
            // setup that can expire or be reused from the pool mid-assertion.
            yield return new WaitForSecondsRealtime(.6f);
            foreach(var projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))Object.Destroy(projectile.gameObject);
            yield return null;
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
