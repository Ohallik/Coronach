using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The replay driver's hands for chapter 10 (ContinuousReview.Mooring.cs), each played through the driver's own virtual pad, nothing called on an actor: a dock and a launch
    /// made at a berth that has moved since it was built (Mother of Doors' fourth shelf swings on her strain), flown and walked to where its pad and its launch stand now; and a marking
    /// laid by a boss nobody strikes (Mother of Doors in her own room) rolled out of as it lands. Each is shown failing first, in the same case, as the driver was without the hand (the
    /// ship flown to where the pad was built, the walk to where the launch was, the marking sat in).</summary>
    public sealed class MooringReplayHandsTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;bool finished;readonly List<string> log=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Heeded={"BERTH_","QUALITY_BERTH_GOAL","QUALITY_UNSTRUCK_ROLL","MOTHER_WAKES","COUPLING_"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("chapter10");
            fixture=new GameObject("Mooring replay fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
            log.Clear();finished=false;Application.logMessageReceived+=Logged;HandsOff(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;HandsOff(false);
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(fixture!=null)Object.Destroy(fixture);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the driver, by its private parts (as HomeReplayHandsTests drives it)
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        List<string> Failures=>Get<List<string>>("failures");
        /// <summary>The driver without its chapter 10 hands, for the broken state of a case: put back by every case and by the tear-down.</summary>
        static void HandsOff(bool off)=>typeof(ContinuousReview).GetField("mooringHandsOff",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,off);
        IEnumerator Play(QualityRoute route,float most,System.Action each=null)
        {
            if(Get<Gamepad>("pad")==null)Set("pad",InputSystem.AddDevice<Gamepad>("MooringReplayPad"));
            Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);var clock=Get<System.Diagnostics.Stopwatch>("clock");if(!clock.IsRunning)clock.Start();
            var run=(IEnumerator)Call("RunSteps");finished=false;Failures.Clear();
            for(float end=Time.unscaledTime+most;Time.unscaledTime<end;)
            {
                if(!run.MoveNext()){finished=true;break;}
                each?.Invoke();Call("InputUpdate");yield return null;
            }
            Set("recording",false);Call("InputUpdate");
        }

        // ------------------------------------------------------------------ fixtures
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=Lead);
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();bool was=body.enabled;body.enabled=false;
            if(actor.TryGetComponent<FlightMotor>(out var ship))ship.Halt();if(actor.TryGetComponent<GroundMotor>(out var feet))feet.Halt();
            actor.transform.position=at;body.enabled=was;Physics.SyncTransforms();
        }
        static IEnumerator Zone(string zone,bool keepEnemies=false)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))if(keepEnemies)enemy.Passive=true;else Object.Destroy(enemy.gameObject);
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+900;
            yield return new WaitForSecondsRealtime(.6f);
        }

        // ================================================================== a berth that has moved
        // BerthTests' shelf (14 x 10, its floor 1.2 m under the plane), declared in play, its floor under the berth so the two move as one; then driven 16 m east and turned about, as
        // her strain swings the fourth shelf (IBerth.Drive). The route was written where it was built.
        const float Plane=1,FloorTop=-.2f;
        static readonly Vector3 Centre=new(150,FloorTop+.5f,150),Pad=new(150,Plane,140),Launch=new(145,FloorTop,152),Foot=new(150,FloorTop,147),FlightAt=new(150,Plane,131);
        const string Id="MovedShelf";
        static Berth MovedShelf()
        {
            var berth=Berth.Declare(Id,"The moved shelf",ZoneKind.GroundCombat,new Bounds(Centre,new Vector3(14,1,10)),Pad,Launch,Foot,FlightAt,"MovedShelfPad",new Vector3(142,Plane,133),true,true);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Moved shelf floor";floor.transform.position=new Vector3(150,FloorTop-.5f,150);floor.transform.localScale=new Vector3(14,1,10);
            floor.transform.SetParent(berth.transform,true);
            Assert.IsTrue(berth.Drive(Centre+new Vector3(16,0,0),Quaternion.Euler(0,180,0)),"fixture: the shelf may be moved while nobody stands on it");
            Physics.SyncTransforms();return berth;
        }
        // As mooring_routes writes them: the form said a step after the change, once the bodies have folded into it.
        static QualityStep[] DockSteps()=>new[]{
            new QualityStep{name="dock at the moved shelf",seconds=10,navigate=true,point=Pad,tolerance=1.2f,until="docked:"+Id,stopWhen="docked:"+Id,buttons=new[]{"South"},pulseSeconds=.3f},
            new QualityStep{name="stand on the moved shelf",seconds=.6f,expectedForm="Shaped"}};
        static QualityStep[] LaunchSteps()=>new[]{
            new QualityStep{name="to the launch",seconds=8,navigate=true,point=Launch,tolerance=.4f,until="arrived",stopWhen="launched:"+Id},
            new QualityStep{name="settle before the launch",seconds=.5f},
            new QualityStep{name="launch from the moved shelf",seconds=4,buttons=new[]{"South"},pulseSeconds=.6f,until="launched:"+Id},
            new QualityStep{name="release the launch",seconds=.6f,expectedForm="Flight"}};
        [UnityTest] public IEnumerator ADockAndALaunchAtABerthThatHasMovedAreMadeWhereItStandsNow()
        {
            IEnumerator DockOnce(bool broken)
            {
                HandsOff(broken);yield return Zone("Arena_Flight");var berth=MovedShelf();
                Partner.GetComponent<PartnerBrain>().enabled=false;Put(Partner,new Vector3(120,Plane,110));Put(Lead,new Vector3(150,Plane,124));yield return Frames(3);
                Assert.Greater(Flat(berth.pad.transform.position,Pad),15,"fixture: the pad has moved well out of reach of where it was built");
                yield return Play(new QualityRoute{name="the moved pad",scene="Arena_Flight",steps=DockSteps()},14);
                HandsOff(false);
            }
            // As the driver was: flown to where the pad was built, pressing at nothing.
            yield return DockOnce(true);
            Assert.IsNull(Berth.Current,"fixture: flown to where the pad was built, the party docked all the same: the check below proves nothing");
            CollectionAssert.IsNotEmpty(Failures,"fixture: the dock at nothing didn't fail its step");
            log.Clear();
            yield return DockOnce(false);
            Assert.IsTrue(finished,"the dock never ended");
            CollectionAssert.IsEmpty(Failures,"the dock at the moved pad failed a step: "+string.Join(" | ",log));
            Assert.AreEqual(Id,Berth.Current!=null?Berth.Current.id:null,"the driver never docked at the pad where it stands now: "+string.Join(" | ",log));
            Assert.IsTrue(log.Any(l=>l.StartsWith("QUALITY_BERTH_GOAL "+Id+" pad")),"the driver never said where the pad stood: "+string.Join(" | ",log));

            IEnumerator LaunchOnce(bool broken)
            {
                HandsOff(broken);yield return Zone("Arena_Flight");var berth=MovedShelf();
                Partner.GetComponent<PartnerBrain>().enabled=false;Put(Lead,berth.pad.transform.position);yield return Frames(3);
                yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);
                Assert.IsTrue(berth.pad.Available,"fixture: the moved shelf's pad takes the party");berth.pad.Interact();
                float end=Time.unscaledTime+3;while((Berth.Changing||Berth.Current!=berth)&&Time.unscaledTime<end)yield return null;
                Assert.AreSame(berth,Berth.Current,"fixture: the party stands on the moved shelf");
                Assert.Greater(Flat(berth.launch.transform.position,Launch),10,"fixture: the launch has moved well out of reach of where it was built");
                yield return Play(new QualityRoute{name="the moved launch",scene="Arena_Flight",steps=LaunchSteps()},16);
                HandsOff(false);
            }
            yield return LaunchOnce(true);
            Assert.IsNotNull(Berth.Current,"fixture: walked toward where the launch was built, the party launched all the same: the check below proves nothing");
            log.Clear();
            yield return LaunchOnce(false);
            Assert.IsTrue(finished,"the launch never ended");
            CollectionAssert.IsEmpty(Failures,"the launch from the moved shelf failed a step: "+string.Join(" | ",log));
            Assert.IsNull(Berth.Current,"the driver never launched from the launch where it stands now: "+string.Join(" | ",log));
            Assert.IsTrue(Lead.flight,"launched, the lead isn't flying");
        }

        // ================================================================== what the fights' words ask of a thumb (the pad as the driver leaves it, frame by frame)
        static bool Has(UnityEngine.InputSystem.LowLevel.GamepadState pad,UnityEngine.InputSystem.LowLevel.GamepadButton b)=>(pad.buttons&(1u<<(int)b))!=0;
        /// <summary>The driver's chapter 10 hands on a pad holding these buttons, for this many seconds of frames: how many frames each button came through.</summary>
        IEnumerator Thumb(float seconds,UnityEngine.InputSystem.LowLevel.GamepadButton[] held,Dictionary<UnityEngine.InputSystem.LowLevel.GamepadButton,int> through,System.Action each=null)
        {
            var clock=Get<System.Diagnostics.Stopwatch>("clock");if(!clock.IsRunning)clock.Start();
            foreach(var b in held)through[b]=0;through[UnityEngine.InputSystem.LowLevel.GamepadButton.DpadRight]=0;
            for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;)
            {
                each?.Invoke();var pad=new UnityEngine.InputSystem.LowLevel.GamepadState();foreach(var b in held)pad=pad.WithButton(b);
                var after=(UnityEngine.InputSystem.LowLevel.GamepadState)Call("MooringHands",pad,PartyController.Current,false,false);
                foreach(var b in through.Keys.ToList())if(Has(after,b))through[b]++;
                yield return null;
            }
        }
        /// <summary>Meret's craft with its dome up: the fight steps press Fire and the Pulse, and a shot that reaches the dome comes back. With the hand neither goes through.</summary>
        [UnityTest] public IEnumerator UnderMeretsDomeTheDriverHoldsItsFire()
        {
            var fire=new[]{UnityEngine.InputSystem.LowLevel.GamepadButton.South,UnityEngine.InputSystem.LowLevel.GamepadButton.RightShoulder,UnityEngine.InputSystem.LowLevel.GamepadButton.West};
            var through=new Dictionary<UnityEngine.InputSystem.LowLevel.GamepadButton,int>();
            yield return Zone("Arena_Flight");
            var meret=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MeretShip"),Lead.transform.position+new Vector3(0,0,18));meret.Passive=false;yield return Frames(6);
            var boss=meret.GetComponent<BossController>();float until=Time.unscaledTime+3;while(!boss.ShipShieldUp&&Time.unscaledTime<until)yield return null;
            Assert.IsTrue(boss.ShipShieldUp,"fixture: her dome is up");
            HandsOff(true);yield return Thumb(.5f,fire,through);HandsOff(false);
            Assert.Greater(through[fire[0]],0,"fixture: without the hand Fire went through under her dome all the same: the check below proves nothing");
            if(!boss.ShipShieldUp)Assert.Inconclusive("her dome dropped during the fixture");
            yield return Thumb(.5f,fire,through);
            Assert.IsTrue(boss.ShipShieldUp,"fixture: her dome stayed up");
            foreach(var b in fire)Assert.AreEqual(0,through[b],b+" went through under her dome");
        }
        /// <summary>Meret's guard up in the well (<c>HER GUARD IS UP · DON'T MASH</c>): the fight steps press attack every .46 s, and her counter punishes a string of them. With the hand
        /// the first press goes through for a tenth of a second and no more until her guard drops.</summary>
        [UnityTest] public IEnumerator IntoMeretsGuardTheDriverStrikesOnceAndStops()
        {
            var strike=new[]{UnityEngine.InputSystem.LowLevel.GamepadButton.South};var through=new Dictionary<UnityEngine.InputSystem.LowLevel.GamepadButton,int>();
            yield return Zone("Arena_Ground");
            var meret=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MeretWell"),Lead.transform.position+new Vector3(0,0,4));meret.Passive=true;yield return Frames(4);
            var boss=meret.GetComponent<BossController>();typeof(BossController).GetField("wellGuarding",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(boss,true);
            Lead.target=meret.Health;Assert.IsTrue(boss.WellGuarding,"fixture: her guard is up");
            HandsOff(true);yield return Thumb(.6f,strike,through);HandsOff(false);int blind=through[strike[0]];
            Assert.Greater(blind,10,"fixture: without the hand the attack held into her guard didn't go through: the check below proves nothing");
            yield return Thumb(.6f,strike,through);
            Assert.Greater(through[strike[0]],0,"the driver never struck her guard once");
            Assert.Less(through[strike[0]],blind/3,"the driver kept pressing into her guard");
        }
        /// <summary>A Clamp Tender holding the Tollkeeper's gate motor (a blow on the motor takes a quarter while one does): on the arm, with no Bridle clamp in the room for chapter 7's hand
        /// to read, the driver flicks the lock across to the tender with the d-pad.</summary>
        [UnityTest] public IEnumerator OnTheArmWithATenderUpTheDriverFlicksTheLockToIt()
        {
            var none=new UnityEngine.InputSystem.LowLevel.GamepadButton[0];var through=new Dictionary<UnityEngine.InputSystem.LowLevel.GamepadButton,int>();
            yield return Zone("Arena_Flight");var berth=MovedShelf();
            Partner.GetComponent<PartnerBrain>().enabled=false;Put(Lead,berth.pad.transform.position);yield return Frames(3);
            yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);berth.pad.Interact();
            float end=Time.unscaledTime+3;while((Berth.Changing||Berth.Current!=berth)&&Time.unscaledTime<end)yield return null;
            Assert.AreSame(berth,Berth.Current,"fixture: the party stands on the shelf");
            var tender=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("StillwaterTender"),Lead.transform.position+new Vector3(2,.05f,0));tender.Passive=true;yield return Frames(3);
            Assert.IsTrue(Tended.IsTender(tender.Health),"fixture: a Clamp Tender");
            // The lock on something else of the deck's that isn't a tender, as it is on the Tollkeeper's motor.
            var other=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("StillwaterWarden"),Lead.transform.position+new Vector3(-2,.05f,0));other.Passive=true;yield return Frames(3);
            Assert.IsFalse(Tended.IsTender(other.Health),"fixture: the other body is no tender");
            void OnTheOther()=>Lead.target=other.Health;
            HandsOff(true);yield return Thumb(1.2f,none,through,OnTheOther);HandsOff(false);
            Assert.AreEqual(0,through[UnityEngine.InputSystem.LowLevel.GamepadButton.DpadRight],"fixture: without the hand the lock was flicked all the same: the check below proves nothing");
            yield return Thumb(1.2f,none,through,OnTheOther);
            Assert.Greater(through[UnityEngine.InputSystem.LowLevel.GamepadButton.DpadRight],0,"with a tender up on the berth the driver never flicked the lock to it");
        }
        /// <summary>Meret's gantry with an arm overreached: its collar's seat lit beside her shoulder. A fight step goes to it, takes the collar's weight with the interact button and
        /// lets the pad be (a swing or a roll lets go). Without the hand the step chases and swings, and never takes it.</summary>
        [UnityTest] public IEnumerator WithAnArmOverreachedTheDriverTakesItsCollarAndHoldsStill()
        {
            IEnumerator Once(bool broken)
            {
                HandsOff(broken);yield return Zone("Arena_Ground");
                Partner.GetComponent<PartnerBrain>().enabled=false;Put(Partner,Lead.transform.position+new Vector3(-30,0,-30));
                var go=new GameObject("Overreached collar (fixture)");go.transform.position=Lead.transform.position+new Vector3(3,0,3);var coupling=go.AddComponent<MeretCoupling>();
                yield return Frames(2);coupling.Offer(true);Assert.IsTrue(coupling.Available,"fixture: the collar is offered");
                var fight=new QualityStep{name="fight at the gantry",seconds=6,buttons=new[]{"South"},pulseSeconds=.46f,navigate=true,approachTarget=true,tolerance=2.3f,rangedTolerance=12,evadeTelegraphs=true};
                yield return Play(new QualityRoute{name="the collar",scene="Arena_Ground",steps=new[]{fight}},8);
                Object.Destroy(go);yield return null;HandsOff(false);
            }
            yield return Once(true);
            Assert.IsFalse(log.Any(l=>l.StartsWith("COUPLING_TAKEN")),"fixture: without the hand the collar was taken all the same: the check below proves nothing");
            log.Clear();
            yield return Once(false);
            Assert.IsTrue(log.Any(l=>l.StartsWith("COUPLING_TAKEN")),"the driver never took the overreached arm's collar: "+string.Join(" | ",log));
            Assert.IsFalse(log.Any(l=>l.StartsWith("COUPLING_DROPPED")),"the driver took the collar and let it go: "+string.Join(" | ",log));
        }

        // ================================================================== a boss nobody strikes
        /// <summary>Mother of Doors woken in her own room (her four clamps in the scene, so nothing strikes her and nothing locks her), and asleep: one lane of hers laid across a ship west of
        /// the crater's middle, its tell 1.4 s. The older rule reads only the locked boss's tells, and she can't be locked: the ship sat in it. With the hand it rolls out as it lands.</summary>
        [UnityTest] public IEnumerator AMarkingOfABossNobodyStrikesIsRolledOutOfAsItLands()
        {
            bool inside=false;int rolls=0;
            IEnumerator Once(bool broken)
            {
                HandsOff(broken);yield return Zone("SorrelSecondSky");
                var mother=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MotherOfDoors"),new Vector3(0,1,330));yield return Frames(4);
                var boss=mother.GetComponent<BossController>();Assert.IsNotNull(boss,"fixture: Mother of Doors is a boss");
                Assert.IsTrue(boss.MotherRoom,"fixture: Sorrel has her clamps: she is in her own room");
                mother.Passive=true;yield return Frames(2);
                Assert.IsFalse(mother.Health.Damageable,"fixture: in her room nothing strikes her");
                Partner.GetComponent<PartnerBrain>().enabled=false;Put(Partner,new Vector3(-30,1,270));
                var at=new Vector3(-10.3f,1,301.8f);Put(Lead,at);yield return Frames(3);
                int before=Lead.GetComponent<FlightMotor>().DashSequence;
                var lane=Telegraph.Line(at+Vector3.left*12,Vector3.right,24,6,1.4f,Telegraph.Threat);lane.owner=boss;
                var hold=new QualityStep{name="hold in her lane",seconds=2.2f,leftTrigger=1,evadeTelegraphs=true,evadeProjectiles=true};
                inside=true;
                yield return Play(new QualityRoute{name="her lane",scene="SorrelSecondSky",steps=new[]{hold}},4,()=>{if(lane!=null&&!lane.Released&&lane.Charged)inside=lane.Contains(Lead.transform.position);});
                rolls=Lead.GetComponent<FlightMotor>().DashSequence-before;
                if(lane!=null)Object.Destroy(lane.gameObject);Object.Destroy(mother.gameObject);yield return null;HandsOff(false);
            }
            yield return Once(true);
            Assert.AreEqual(0,rolls,"fixture: without the hand the driver rolled out of her lane all the same: the check below proves nothing");
            Assert.IsTrue(inside,"fixture: without the hand the ship wasn't in her lane as it landed");
            log.Clear();
            yield return Once(false);
            Assert.IsTrue(finished,"the hold never ended");
            Assert.Greater(rolls,0,"the driver never rolled as her lane's tell ran out: "+string.Join(" | ",log));
            Assert.IsFalse(inside,"the driver rolled and was still in her lane as it landed");
            Assert.IsTrue(log.Any(l=>l.StartsWith("QUALITY_UNSTRUCK_ROLL MotherOfDoors")),"the driver never said whose marking it rolled out of: "+string.Join(" | ",log));
        }
    }
}
