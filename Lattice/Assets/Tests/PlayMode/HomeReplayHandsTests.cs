using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The replay driver's hands for chapter 9 (ContinuousReview.Home.cs), each played through the driver's own virtual pad, nothing called on an actor: a press that waits
    /// for its prompt to come (<c>"until":"prompt:..."</c>: Orrin walking to his board), a tread-hall coupling taken and held with the hands still until it is mated, a crossing of
    /// Nacre Grave's berth ring waited for until no wreck is coming (<c>"until":"clear:..."</c> in flight), and the Foundry Saint's planted collar struck. Each is shown failing first,
    /// in the same case, as the driver was without the hand (the wait over at once, the presses going on, the ring flown straight across, the fight chased at its body).</summary>
    public sealed class HomeReplayHandsTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;bool finished;readonly List<string> log=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Heeded={"COUPLING_","WRECK_STRUCK","QUALITY_WAIT","QUALITY_CLEAR","QUALITY_PROMPT","SAINT_","WALL_CONTACT","QUALITY_CUTTER_ROUND"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("chapter9");
            fixture=new GameObject("Home replay fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
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

        // ------------------------------------------------------------------ the driver, by its private parts (as HarrowgateReplayTests drives it)
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        List<string> Failures=>Get<List<string>>("failures");
        /// <summary>The driver without its chapter 9 hands, for the broken state of a case: put back by every case and by the tear-down.</summary>
        static void HandsOff(bool off)=>typeof(ContinuousReview).GetField("homeHandsOff",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,off);
        IEnumerator Hands(QualityRoute route)
        {
            if(Get<Gamepad>("pad")==null)Set("pad",InputSystem.AddDevice<Gamepad>("HomeReplayPad"));
            Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);var clock=Get<System.Diagnostics.Stopwatch>("clock");if(!clock.IsRunning)clock.Start();
        }
        IEnumerator Play(QualityRoute route,float most,System.Action each=null,System.Func<bool> enough=null)
        {
            yield return Hands(route);var run=(IEnumerator)Call("RunSteps");finished=false;Failures.Clear();
            for(float end=Time.unscaledTime+most;Time.unscaledTime<end;)
            {
                if(enough!=null&&enough())break;
                if(!run.MoveNext()){finished=true;break;}
                each?.Invoke();Call("InputUpdate");yield return null;
            }
            Set("recording",false);Call("InputUpdate");
        }

        // ------------------------------------------------------------------ fixtures
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=Lead);
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship)ship.Halt();else if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static IEnumerator Zone(string zone,bool keepEnemies=false)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))if(keepEnemies)enemy.Passive=true;else Object.Destroy(enemy.gameObject);
            yield return new WaitForSecondsRealtime(.6f);
        }
        const float X=230;
        static Vector3 At(float x,float y,float z)=>new(X+x,y,z);
        static IEnumerator Yard()
        {
            yield return Zone("Arena_Ground");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Home hands fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.3f);
            Partner.GetComponent<PartnerBrain>().enabled=false;Put(Partner,At(-50,.05f,-50));
        }
        static QualityStep Walk(string name,Vector3 to,float tolerance,float seconds,bool brake=false)=>new(){name=name,seconds=seconds,navigate=true,point=to,tolerance=tolerance,until="arrived",leftTrigger=brake?1:0};
        static QualityStep PressFor(string name,string flag,float seconds)=>new(){name=name,seconds=seconds,buttons=new[]{"South"},pulseSeconds=.6f,until="flag:"+flag};

        // ================================================================== a prompt that has to come
        /// <summary>The Coronach's Orrin is offered at his board only once he has walked there: a press made as the party arrives is made at nothing. Here a point that is offered three
        /// seconds after the party stands by it, and a route that waits for it (<c>prompt:Stop and listen</c>) then presses for its flag with a press's own few seconds.</summary>
        [UnityTest] public IEnumerator APressThatWaitsForItsPromptIsMadeOnceThePromptIsOfferedAndSetsItsFlag()
        {
            yield return Yard();
            IEnumerator Once(bool waits)
            {
                GameServices.Current.Flags.SetBool("test.orrinThere",false);GameServices.Current.Flags.SetBool("test.boardHeard",false);
                var go=new GameObject("Orrin's board (fixture)",typeof(DiscoveryPoint));go.transform.position=At(0,0,2.2f);
                var point=go.GetComponent<DiscoveryPoint>();point.prompt="Stop and listen";point.flag="test.boardHeard";point.requiredFlag="test.orrinThere";point.report="";point.situation="";point.dialogueNode="";point.range=3;
                Put(Lead,At(0,.05f,0));yield return Frames(3);
                var steps=new List<QualityStep>();
                if(waits)steps.Add(new QualityStep{name="wait until Orrin stands at his board",seconds=10,until="prompt:Stop and listen",useItem="none"});
                steps.Add(PressFor("stop and listen",point.flag,2));
                float shown=Time.unscaledTime+3;
                yield return Play(new QualityRoute{name="Orrin's board",scene="Arena_Ground",steps=steps.ToArray()},16,()=>{if(Time.unscaledTime>=shown)GameServices.Current.Flags.SetBool("test.orrinThere",true);});
                Object.Destroy(go);yield return null;
            }
            // Built as the driver was without the wait: pressed as the party arrives, for the press's two seconds, at nothing.
            yield return Once(false);
            Assert.IsFalse(Flag("test.boardHeard"),"fixture: pressed at once, the board was heard all the same: the check below proves nothing");
            CollectionAssert.IsNotEmpty(Failures,"fixture: the press at nothing didn't fail its step");
            yield return Once(true);
            Assert.IsTrue(finished,"the wait and the press never ended");
            CollectionAssert.IsEmpty(Failures,"the driver's wait for the prompt failed a step");
            Assert.IsTrue(Flag("test.boardHeard"),"the driver waited for the prompt and its press didn't set the flag");
            Assert.IsTrue(log.Any(l=>l.StartsWith("QUALITY_PROMPT Stop and listen")),"the driver never said the prompt came: "+string.Join(" | ",log));
        }

        // ================================================================== a tread-hall coupling
        /// <summary>A coupling is one press, then 3.2 s of hands with nothing else done: a press_for's next press is a swing, which lets it go. With the hand the pad is let go while the
        /// hands are on the jack, and the coupling is mated.</summary>
        [UnityTest] public IEnumerator ACouplingTakenByThePressIsHeldWithThePadStillUntilItIsMated()
        {
            yield return Yard();
            IEnumerator Once(bool broken)
            {
                HandsOff(broken);GameServices.Current.Flags.SetBool("test.coupling",false);
                var go=new GameObject("Re-mate the coupling (fixture)");go.SetActive(false);go.transform.position=At(0,0,1.2f);
                var coupling=go.AddComponent<TreadCoupling>();coupling.index=9;coupling.flag="test.coupling";coupling.requiredFlag="";go.SetActive(true);
                Put(Lead,At(0,.05f,0));yield return Frames(3);
                yield return Play(new QualityRoute{name="the coupling",scene="Arena_Ground",steps=new[]{PressFor("re-mate the coupling","test.coupling",9)}},12);
                Object.Destroy(go);yield return null;HandsOff(false);
            }
            yield return Once(true);
            Assert.IsTrue(log.Any(l=>l.StartsWith("COUPLING_TAKEN 9")),"fixture: the press never took the jack: "+string.Join(" | ",log));
            Assert.IsTrue(log.Any(l=>l.StartsWith("COUPLING_LET_GO 9")),"fixture: pressing on never let the jack go: the check below proves nothing");
            Assert.IsFalse(Flag("test.coupling"),"fixture: pressed on, the coupling was mated all the same");
            log.Clear();
            yield return Once(false);
            Assert.IsTrue(finished,"the coupling's press never ended");
            CollectionAssert.IsEmpty(Failures,"the coupling's press failed a step");
            Assert.IsTrue(Flag("test.coupling"),"the driver took the jack and never mated it: "+string.Join(" | ",log));
            Assert.IsFalse(log.Any(l=>l.StartsWith("COUPLING_LET_GO 9")),"the driver let the jack go: "+string.Join(" | ",log));
        }

        // ================================================================== Nacre Grave's berth ring
        static WreckTrack Ring()=>Object.FindObjectsByType<WreckTrack>(FindObjectsSortMode.None).FirstOrDefault(t=>t.closed&&Mathf.Abs(t.RadiusAt(0)-48)<2);
        static Vector3 OnBasin(float turn,float radius){float a=turn*Mathf.Deg2Rad;return new Vector3(Mathf.Sin(a)*radius,1,Mathf.Cos(a)*radius);}
        /// <summary>The crossing toward the navigators' pod (docs/maps/NacreGrave.md): from r 56 at turn 300 over the ring (r 48, four wrecks at 15 m/s) to r 40. The ring's clock is set so
        /// a wreck comes to the crossing as a ship leaving now reaches it; flown straight across, it is struck, and waited for it never is.</summary>
        IEnumerator CrossTheRing(bool blind)
        {
            yield return Zone("NacreGrave");
            var ring=Ring();Assert.IsNotNull(ring,"fixture: Nacre Grave has its berth ring");
            Partner.GetComponent<PartnerBrain>().enabled=false;Put(Partner,new Vector3(-14,1,160));
            Put(Lead,OnBasin(300,56));Lead.Health.InvulnerableUntil=0;Lead.Health.integrity=Lead.Health.maximum;yield return Frames(3);
            // The arc of the ring nearest the crossing, and the clock that brings wreck 0 to it a second from now.
            var over=OnBasin(300,48);float arc=0,least=float.PositiveInfinity;for(float s=0;s<ring.Total;s+=.5f){float d=(ring.PointAt(s)-over).sqrMagnitude;if(d<least){least=d;arc=s;}}
            ring.phase=Mathf.Repeat(ring.TimeTo(arc)-(ring.Clock+1.1f),ring.Traverse);
            var steps=new List<QualityStep>();
            var way=new[]{new Vector2(over.x,over.z),new Vector2(OnBasin(300,40).x,OnBasin(300,40).z)};
            if(!blind)steps.Add(new QualityStep{name="wait for the berth ring",seconds=30,leftTrigger=1,until="clear:"+string.Join(";",way.Select(p=>p.x.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+","+p.y.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)))});
            steps.Add(Walk("across the berth ring",over,2,8));steps.Add(Walk("inside the berth ring",OnBasin(300,40),2,8));
            yield return Play(new QualityRoute{name="the berth ring",scene="NacreGrave",steps=steps.ToArray()},45);
        }
        [UnityTest] public IEnumerator TheBerthRingIsCrossedOnlyOnceNoWreckIsComingAndTheShipIsNeverStruck()
        {
            yield return CrossTheRing(true);
            Assert.IsTrue(log.Any(l=>l.StartsWith("WRECK_STRUCK")),"fixture: flown straight across as a wreck came, the ship wasn't struck: the check below proves nothing");
            log.Clear();
            yield return CrossTheRing(false);
            Assert.IsTrue(finished,"the crossing never ended");
            CollectionAssert.IsEmpty(Failures,"the crossing failed a step");
            Assert.IsFalse(log.Any(l=>l.StartsWith("WRECK_STRUCK")),"the driver waited for the ring and was struck all the same: "+string.Join(" | ",log));
            Assert.IsTrue(log.Any(l=>l.StartsWith("QUALITY_WAIT")),"fixture: the driver never had to wait for a wreck");
        }

        // ================================================================== the Foundry Saint's collar
        static QualityStep Fight(float seconds)=>new(){name="fight the Saint",seconds=seconds,buttons=new[]{"South"},pulseSeconds=.46f,navigate=true,approachTarget=true,tolerance=2.3f,
            rangedTolerance=12,evadeTelegraphs=true,stopWhen="flag:bossdown.FoundrySaint"};
        /// <summary>A slam plants the two arms nearest the lead with their collars open for a few seconds, and the Saint's body takes nothing while its arms bear it: a person goes to an
        /// open collar and strikes it there, and the blows come off its bar (D281). Without the hand the driver chases the body, and stands nowhere near a collar. The lead starts west of
        /// the press, south of its west leg, so the slam takes arms 1 and 2 and the way to collar 1 is open floor.</summary>
        [UnityTest] public IEnumerator WithACollarOpenTheDriverGoesToItAndStrikesIt()
        {
            float atCollar=0,dropped=0;
            IEnumerator Once(bool broken)
            {
                HandsOff(broken);yield return Yard();atCollar=0;
                var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("FoundrySaint"),At(0,.02f,0));enemy.Passive=false;yield return Frames(3);
                var boss=enemy.GetComponent<BossController>();Assert.AreEqual(6,boss.SaintArms.Count,"fixture: the Saint stands its six arms");
                foreach(var hero in PartyController.Current.members){hero.Health.InvulnerableUntil=GameTime.Now+900;hero.charge=0;}
                Put(Lead,At(-9,.05f,-4));yield return Frames(2);
                Assert.IsTrue(boss.Perform("ArmSlam"),"fixture: the slam");
                float until=Time.unscaledTime+3;while(!boss.SaintArms.Any(a=>a.Unlocked)&&Time.unscaledTime<until)yield return null;
                Assert.IsTrue(boss.SaintArms.Any(a=>a.Unlocked),"fixture: the slam opened no collar");
                float before=enemy.Health.integrity;
                yield return Play(new QualityRoute{name="the Saint",scene="Arena_Ground",steps=new[]{new QualityStep{name="lock the Saint",seconds=.2f,buttons=new[]{"LeftShoulder"}},new QualityStep{name="release lock",seconds=.3f},Fight(BossController.SaintTune.SlamUnlock+1)}},
                    BossController.SaintTune.SlamUnlock+4,()=>{foreach(var arm in boss.SaintArms)if(arm.Unlocked&&Vector3.Distance(Flat(Lead.transform.position),Flat(arm.CollarFloor+arm.Outward*ContinuousReview.CollarStand))<.8f){atCollar+=Time.deltaTime;break;}});
                dropped=before-enemy.Health.integrity;HandsOff(false);
            }
            yield return Once(true);
            Assert.Less(atCollar,.5f,"fixture: without the hand the driver stood at an open collar all the same: the check below proves nothing");
            yield return Once(false);
            Assert.Greater(atCollar,1f,"the driver never went to an open collar and stood there to strike it");
            Assert.Greater(dropped,0,"the driver stood at an open collar and its blows took nothing off the Saint's bar");
        }
        static Vector3 Flat(Vector3 v)=>new(v.x,0,v.z);

        // ================================================================== the Debris Cutter
        /// <summary>Its fight as tools/home_routes.py's close_boss writes it: the lock, then rounds of pursuit (Fire held, braked, within 5 m), each closed by the Pulse.</summary>
        static QualityStep[] CutterRounds(int rounds)
        {
            const string stop="flag:bossdown.DebrisCutter";
            var steps=new List<QualityStep>{new(){name="lock DebrisCutter",seconds=.2f,buttons=new[]{"LeftShoulder"},leftTrigger=1},new(){name="release lock DebrisCutter",seconds=.3f,leftTrigger=1}};
            for(int i=1;i<=rounds;i++)
            {
                steps.Add(new(){name="pursue DebrisCutter round "+i,seconds=20,buttons=new[]{"South"},leftTrigger=1,navigate=true,approachTarget=true,tolerance=5,rangedTolerance=5,
                    evadeProjectiles=true,evadeTelegraphs=true,stopWhen=stop,point=new Vector3(0,1,0)});
                steps.Add(new(){name="pulse DebrisCutter round "+i,seconds=.18f,buttons=new[]{"RightShoulder","West"},leftTrigger=1,stopWhen=stop});
                steps.Add(new(){name="release pulse DebrisCutter round "+i,seconds=.45f,leftTrigger=1});
            }
            return steps.ToArray();
        }
        /// <summary>A pair a player can put out (the arrival pair is Taren leading, Idra out) at Sync 26 in the chapter's Tempered parts, each with a bar no sample can empty (blows still
        /// land and stagger, a tractor still reels).</summary>
        /// <paramref name="mastered"/>: every skill at its last mastery, as the player's party has them by the Cutter (Builds/quality/nov23-whole s61 and nov23-recon-C s61: Taren_3
        /// and Idra_1 at mastery 5, Idra_4 at 4 and 5, SKILL_KIND in player.log), for the fight; else at their first, as the shared length test's Sync leaves them, for the yardstick (D398).
        /// Left as the loadout had them (none used) the fight was played at the first mastery and ran 187 to 202 s where the player's ran 126.8 and 184.7 (D384).
        static IEnumerator Pair(string lead,string other,bool mastered=false)
        {
            foreach(var member in GameServices.Current.State.party)
            {
                member.level=26;member.skillUses??=new Dictionary<string,int>();member.skillUses.Clear();
                if(mastered)foreach(var skill in SkillBook.All(member.id))member.skillUses[skill.id]=SkillBook.MasteryUses[SkillBook.MasteryUses.Length-1];
            }
            Assert.IsTrue(Deployment.Set(GameServices.Current.State,new[]{lead,other}),$"fixture: {lead} and {other} can be put out");
            PartyField.Reform(PartyController.Current);yield return Frames(3);PartyController.Current.RefreshStats();yield return null;
            if(Lead.character!=lead)PartyController.Current.Swap();yield return Frames(3);
            Assert.AreEqual(lead,Lead.character,$"fixture: {lead} leads");
            foreach(var hero in PartyController.Current.members){hero.Health.maximum=hero.Health.integrity=50000;hero.Health.InvulnerableUntil=0;}
        }
        /// <summary>The pair flat out on a Cutter that never moves, as QaPlanCombatTests measures it (both ships 6 m off its stern, nose on it, every skill as it comes, else a shot), in
        /// the flight yard: what the driver's share is a share of. For sixty seconds at the first mastery since D398, as the shared length test reads it, which is the yardstick the
        /// sheet's share is on (2,225 a second when the player's runs were played; about 2,590 since D400 narrowed the flight pairs; the check is no longer a share of it, D403);
        /// fifteen read the same pair anywhere from 2,387 to 2,500.</summary>
        IEnumerator FlatOut(string lead,string other,System.Action<float> measured)
        {
            yield return Zone("Arena_Flight");yield return Pair(lead,other);
            var at=new Vector3(200,1,0);var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("DebrisCutter"),at);enemy.Passive=true;yield return Frames(3);
            enemy.GetComponent<BossController>().enabled=false;var body=enemy.Health;body.maximum=body.integrity=10000000;body.Holds=null;
            var heroes=new[]{Lead,Partner};var homes=new[]{at+new Vector3(-2,0,-6),at+new Vector3(2,0,-6)};
            void Home(int i){Put(heroes[i],homes[i]);if(heroes[i].motor is FlightMotor ship)ship.KeepFacing(at-homes[i]);}
            for(int i=0;i<heroes.Length;i++){heroes[i].GetComponent<PlayerBrain>().AutoPilot=true;heroes[i].GetComponent<PartnerBrain>().enabled=false;Home(i);heroes[i].target=body;heroes[i].charge=100;}
            yield return null;
            float landed=0;System.Action<Health,DamagePacket,float> count=(_,packet,amount)=>{if(heroes.Any(h=>packet.source==h.Health))landed+=amount;};body.Damaged+=count;
            const float seconds=60;int drawn=0;
            for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;drawn++)
            {
                for(int i=0;i<heroes.Length;i++)
                {
                    var hero=heroes[i];hero.motor.Move(Vector2.zero,false,false);
                    if(hero.CanAct&&Vector3.Distance(Flat(hero.transform.position),Flat(homes[i]))>.6f)Home(i);
                    hero.target=body;if(!hero.CanAct)continue;bool acted=false;
                    for(int slot=0;slot<SkillBook.Slots&&!acted;slot++){var skill=SkillBook.Equipped(hero.character,slot);if(skill==null||hero.cooldowns[slot]>0||hero.charge<SkillBook.Cost(hero.character,skill))continue;acted=hero.Skill(slot);}
                    if(!acted)hero.Attack();
                }
                yield return null;
            }
            body.Damaged-=count;Assert.Greater(landed,0,"fixture: the pair is hitting the Cutter");
            TestContext.WriteLine($"CUTTER_FLAT_OUT {lead}+{other} rate={landed/seconds:0}/s frames={drawn/seconds:0}/s");
            if(drawn/seconds<50)Assert.Inconclusive($"the Cutter's flat out ran at {drawn/seconds:0} frames a second, on a machine too busy to say how fast a pair is");
            measured(landed/seconds);
            foreach(var hero in heroes){hero.GetComponent<PlayerBrain>().AutoPilot=false;hero.GetComponent<PartnerBrain>().enabled=true;}
            Object.Destroy(enemy.gameObject);yield return Frames(3);
        }
        /// <summary>The Broken Ring at the Cutter's door, its districts on station (ring.formed), the pairs on the way cleared: the party at the checkpoint, inside the door's volume, so the
        /// fight begins as route 61's does. The Cutter as its door brings it out, awake.</summary>
        IEnumerator AtTheCuttersDoor(string lead,string other,System.Action<EnemyBrain> got)
        {
            foreach(string flag in new[]{"home.launched","home.coronachWalked","ring.brynHailed","ring.interceptorSeen","clear.Ring_Pair1","clear.Ring_Pair2","ring.formed"})GameServices.Current.Flags.SetBool(flag,true);
            // Felt by an earlier fight in the same case: whole again.
            foreach(string flag in new[]{"bossdown.DebrisCutter","home.cutterDisabled"})GameServices.Current.Flags.SetBool(flag,false);
            SceneFlow.Current.LoadZone("BrokenRing","Ring");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: the Broken Ring never finished loading");
            yield return new WaitForSecondsRealtime(.6f);yield return Pair(lead,other,true);
            EnemyBrain Cutter()=>Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).FirstOrDefault(e=>e.definition!=null&&e.definition.id=="DebrisCutter"&&e.Health.Alive);
            for(deadline=Time.unscaledTime+4;Cutter()==null&&Time.unscaledTime<deadline;)yield return null;
            if(Cutter()==null){var door=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="DebrisCutter");door.Begin();yield return Frames(3);}
            var cutter=Cutter();Assert.IsNotNull(cutter,"fixture: the Cutter's door brings it out");cutter.Passive=false;got(cutter);
        }
        /// <summary>The Cutter fought by the driver's pad as route 61 fights it, for so many rounds or until it falls: what it took a second, over how long, and whether it fell.</summary>
        IEnumerator Fight(string lead,string other,bool broken,int rounds,float flat,System.Action<float,float,bool> measured)
        {
            HandsOff(broken);EnemyBrain cutter=null;yield return AtTheCuttersDoor(lead,other,c=>cutter=c);
            var body=cutter.Health;float before=body.integrity,began=Time.unscaledTime;
            var route=new QualityRoute{name="the Cutter",scene="BrokenRing",autoHeal=true,useExtra=true,steps=CutterRounds(rounds)};
            // Where the fight's seconds go (D402): the lead braked near it, chasing within the Fire's reach, or beyond it; of the chase, the nose off it (a shot leaves along the nose)
            // and the line to it shut by a district's cover or the bound.
            float near=0,chase=0,far=0,unaimed=0,shut=0,firing=0,rolling=0,phaseAt=began,phaseBar=before;int pods=3;string mode="whole";var phases=new List<string>();
            var dealt=new Dictionary<string,float>();
            System.Action<Health,DamagePacket,float> count=(_,packet,amount)=>{string who=packet.source==Lead.Health?"lead":packet.source==Partner.Health?"partner":"other";string key=who+":"+(packet.tag??"");dealt[key]=(dealt.TryGetValue(key,out var had)?had:0)+amount;};
            body.Damaged+=count;var boss=cutter.GetComponent<BossController>();
            void Tally()
            {
                if(!body.Alive||Lead==null)return;float dt=Time.unscaledDeltaTime;var gap=Flat(body.transform.position-Lead.transform.position);float d=gap.magnitude;
                if(d<=ContinuousReview.CutterNear)near+=dt;else if(d<=ContinuousReview.CutterFire)chase+=dt;else far+=dt;
                if(d<=ContinuousReview.CutterFire&&Vector3.Angle(Lead.motor.Facing,gap)>=30)unaimed+=dt;
                if(d<=ContinuousReview.CutterFire&&!CombatCover.Clear(Lead.transform.position+Vector3.up*.9f,body.transform.position+Vector3.up*.75f,.09f))shut+=dt;
                var pad=Get<UnityEngine.InputSystem.LowLevel.GamepadState>("held");
                if((pad.buttons&(1u<<(int)UnityEngine.InputSystem.LowLevel.GamepadButton.South))!=0)firing+=dt;
                if((pad.buttons&(1u<<(int)UnityEngine.InputSystem.LowLevel.GamepadButton.East))!=0)rolling+=dt;
                if(boss.CutterPodsLeft!=pods){phases.Add($"{mode}:{Time.unscaledTime-phaseAt:0}s@{(phaseBar-body.integrity)/(Time.unscaledTime-phaseAt):0}/s");pods=boss.CutterPodsLeft;mode=boss.CutterMode;phaseAt=Time.unscaledTime;phaseBar=body.integrity;}
            }
            yield return Play(route,rounds*21.5f+5,Tally,()=>!body.Alive);
            body.Damaged-=count;phases.Add($"{mode}:{Time.unscaledTime-phaseAt:0}s@{(phaseBar-(body.Alive?body.integrity:0))/(Time.unscaledTime-phaseAt):0}/s");
            TestContext.WriteLine($"CUTTER_PHASES {string.Join(" ",phases)} firing={firing:0}s rolling={rolling:0}s dealt {string.Join(" ",dealt.OrderByDescending(p=>p.Value).Select(p=>$"{p.Key}={p.Value:0}"))}");
            float seconds=Time.unscaledTime-began,rate=(before-(body.Alive?body.integrity:0))/seconds;
            string line=$"CUTTER_DRIVER {lead}+{other} hands={(broken?"off":"on")} flatOut={flat:0}/s rate={rate:0}/s share={rate/flat:0.000} seconds={seconds:0.0} fell={!body.Alive} bar={(body.Alive?body.integrity/body.maximum:0):P0} scrapes={log.Count(l=>l.StartsWith("WALL_CONTACT "+Lead.name))}";
            TestContext.WriteLine(line);Debug.Log(line);
            TestContext.WriteLine($"CUTTER_SECONDS near={near:0} chase={chase:0} beyond={far:0} unaimed={unaimed:0} shut={shut:0} round={log.Count(l=>l.StartsWith("QUALITY_CUTTER_ROUND"))}");
            log.Clear();HandsOff(false);
            measured(rate,seconds,!body.Alive);
        }
        /// <summary>The Debris Cutter keeps 18 m off the lead and flies to another of its lanes, at 13 m/s, whenever he comes nearer; its hull takes a blow at any time. Played through the
        /// driver's pad in the Broken Ring as route 61 plays it (the arrival pair, Taren leading, the rounds close_boss writes), the bar it takes a second is a share of the same pair's flat
        /// out. Without the hand (chapter-61's first play: braked to the flight chase's 2.1 m/s, firing only from inside the step's 5 m) three rounds get .04 to .14 of flat out (the play
        /// got .05 over sixteen). With it the lead flies after it brake off, holds still near it and fires wherever it is in reach, and fells it inside a boss's two to five minutes at a
        /// pace inside the player's own runs (D403).</summary>
        [UnityTest,Timeout(900000)] public IEnumerator TheCutterIsFlownAfterAndFiredOnWhereverItKeepsItsDistance()
        {
            float flat=0,off=0,on=0,rate=0,seconds=0;bool fell=false;yield return FlatOut("Taren","Idra",r=>flat=r);
            yield return Fight("Taren","Idra",true,3,flat,(r,_,__)=>off=r/flat);
            float sheet=ContentRegistry.Boss("DebrisCutter").driverShare;
            Assert.Less(off,sheet,$"fixture: without the hand the driver got {off:0.00} of flat out, the sheet's {sheet:0.00} or more: the check below proves nothing");
            yield return Fight("Taren","Idra",false,14,flat,(r,s,down)=>{rate=r;on=r/flat;seconds=s;fell=down;});
            Assert.IsTrue(fell,$"with the hand the driver didn't fell the Cutter in fourteen rounds ({seconds:0} s, {on:0.00} of flat out)");
            // Held to the bar a boss is held to in the player, and to nothing tighter (D405). D403's band between the player's own runs (966 to 1,905 a second) stood at the edge of
            // this fight's own spread: alone in one editor the hand lands 1,013 to 1,214 a second, with the suite's four editors side by side 841 (285.5 s, suite nov24b), and the
            // player's build 1,328 to 1,569 on the same code (240,000 in 153.0, 163.9 and 180.7 s: Builds/quality/nov24-recon-C s61, nov24-recon-g2 s61, nov24-recon-g s61). A band
            // that fails by how busy the machine is says nothing about the hand; FromBehindADistrictTheCutterIsFlownRoundTheDistrictNotIntoIt says whether it flies as a person would.
            TestContext.WriteLine($"CUTTER_HAND rate={rate:0} seconds={seconds:0.0} on={on:0.000}");
            Assert.That(seconds,Is.InRange(120f,300f),$"with the hand the driver felled the Cutter in {seconds:0} s: a boss is two to five minutes");
        }
        /// <summary>The lead in the pocket between the market's cover and the west bound, the Cutter held at lane 6 (-30, 280) on the market's far side: the straight line to it lies
        /// through the market. Flown straight at it (the driver before D402) the lead ground into the market's west face, a scrape every 0.6 s, as the in-house fight did for five rounds
        /// (247 s, 36 scrapes) and the player's slower run did at the repair quarter; with the hand he flies round the district's end and comes within braking reach without touching it.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator FromBehindADistrictTheCutterIsFlownRoundTheDistrictNotIntoIt()
        {
            static void RoundOff(bool off)=>typeof(ContinuousReview).GetField("cutterRoundOff",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,off);
            float reachedIn=0;int scrapes=0;
            IEnumerator Once(bool broken)
            {
                RoundOff(broken);EnemyBrain cutter=null;yield return AtTheCuttersDoor("Taren","Idra",c=>cutter=c);
                cutter.Passive=true;cutter.GetComponent<BossController>().enabled=false;
                var held=cutter.GetComponent<CharacterController>();held.enabled=false;cutter.transform.position=new Vector3(-30,1,280);held.enabled=true;
                Put(Lead,new Vector3(-52,1,298));Put(Partner,new Vector3(-45,1,282));Physics.SyncTransforms();yield return Frames(2);
                Lead.target=cutter.Health;log.Clear();float began=Time.unscaledTime;bool reached=false;
                var route=new QualityRoute{name="round the market",scene="BrokenRing",steps=CutterRounds(1).Skip(2).Take(1).ToArray()};
                yield return Play(route,12,()=>reached|=Flat(cutter.transform.position-Lead.transform.position).magnitude<=ContinuousReview.CutterNear,()=>reached);
                reachedIn=reached?Time.unscaledTime-began:float.PositiveInfinity;
                // On the market (its cover from z 287.5, a hull and its sway off): what lay between. Taren's momentum out of the turn round its end can still brush the crescent's top
                // (z 272.5) 5 m on: logged, not counted (the flight model's drag, not the way chosen).
                static float Z(string l){int open=l.LastIndexOf(", "),close=l.LastIndexOf(')');return open>0&&close>open&&float.TryParse(l.Substring(open+2,close-open-2),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var z)?z:0;}
                var touched=log.Where(l=>l.StartsWith("WALL_CONTACT "+Lead.name)).ToList();scrapes=touched.Count(l=>Z(l)>=284);
                TestContext.WriteLine($"CUTTER_ROUND hand={(broken?"off":"on")} reached={reachedIn:0.0}s market={scrapes} scrapes={touched.Count} ways={log.Count(l=>l.StartsWith("QUALITY_CUTTER_ROUND"))}");
                RoundOff(false);
            }
            try
            {
                yield return Once(true);
                Assert.GreaterOrEqual(scrapes,2,$"fixture: flown straight at it the lead scraped the market {scrapes} times: the check below proves nothing");
                yield return Once(false);
                Assert.AreEqual(0,scrapes,$"with the hand the lead still scraped the market {scrapes} times on the way to the Cutter");
                Assert.Less(reachedIn,8f,$"with the hand the lead came within {ContinuousReview.CutterNear} m of the Cutter only after {reachedIn:0.0} s (28 m off, round a 17 m district)");
            }
            finally{RoundOff(false);}
        }
    }

    /// <summary>A boss as the player fought it (D403): the capture it was played in, its seconds there (<c>python tools/boss_durations.py &lt;folder&gt;</c>, BOSS_DURATION: woken to
    /// down, as the in-house accounts read it) and the integrity it was played at, so a fight resized since is compared by what it took a second, not by its seconds. An in-house fight
    /// played through the pad is held between the player's slowest and fastest run, widened by <see cref="Slack"/> each side: the player's own runs of one fight on one build spread as
    /// widely as the in-house fight's (the Cutter 127 and 185 s, the Mother 124 to 143, the Tollkeeper 114 to 142), so a one-sided floor at the edge of that spread was a coin toss.</summary>
    internal readonly struct PlayerRun
    {
        public const float Slack=.15f;
        public readonly string where;public readonly float seconds,integrity;
        public PlayerRun(string where,float seconds,float integrity){this.where=where;this.seconds=seconds;this.integrity=integrity;}
        public float Rate=>integrity/seconds;
        public static (float low,float high) Band(PlayerRun[] runs)=>(runs.Min(r=>r.Rate)*(1-Slack),runs.Max(r=>r.Rate)*(1+Slack));
        public static string Describe(PlayerRun[] runs)
        {
            var (low,high)=Band(runs);
            return $"the player's band {low:0} to {high:0} a second ({1-Slack:0.00} of the slowest, {1+Slack:0.00} of the fastest of: {string.Join("; ",runs.Select(r=>$"{r.where} {r.integrity:0} in {r.seconds:0.0} s = {r.Rate:0}/s"))})";
        }
    }
}
