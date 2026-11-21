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
    /// <summary>The replay driver's hands for chapter 7's two machines (ContinuousReview.Oriel.cs), each done with the pad and nothing else. The Orrery: a route waits short of a
    /// rail's gate until "lee:n" holds, which is only ever while that gate is green with its moon going away from it, for long enough to go through, and comes on every turn of
    /// every rail; green alone is not that. The Last Warden: at a held line the driver walks to a lit seat, stands beside its fibre, takes the clamp's weight with the interact
    /// button, keeps still until the staff is down and the clamp opens in its hands; with a Clamp Tender up it flicks the lock onto the tender instead.
    /// <para>Run with ORIEL_HANDS_BROKEN=1 and each test builds its broken state (the lee read as green alone; the driver without its clamp hand) and fails for its own reason,
    /// which is how they were proved.</para></summary>
    public sealed class OrielReplayHandsTests
    {
        static bool Broken=>System.Environment.GetEnvironmentVariable("ORIEL_HANDS_BROKEN")=="1";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic,Hidden=BindingFlags.Static|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;readonly List<string> log=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        void Logged(string text,string stack,LogType type){if(text.StartsWith("QUALITY_")||text.StartsWith("CLAMP_")||text.StartsWith("WARDEN_"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            // Headless has no focused Game view. Route virtual devices into play.
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("starter");BridleClamp.Slack=false;
            // Never active: the driver's Start wants a player's launch arguments. Its steps, its pad and its decisions are driven from here, a frame at a time.
            fixture=new GameObject("Oriel hands fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
            log.Clear();Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;SetHandsOff(false);SetLeeAsGreen(false);SetLeeWaitsMissed(false);BridleClamp.Slack=false;
            // Unity does not call OnDestroy on a never-active fixture: its pad is taken away here, with whatever it held.
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(fixture!=null)Object.Destroy(fixture);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the driver, by its private parts
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        static void SetHandsOff(bool off)=>typeof(ContinuousReview).GetField("clampHandsOff",Hidden).SetValue(null,off);
        static void SetLeeAsGreen(bool green)=>typeof(ContinuousReview).GetField("leeAsGreen",Hidden).SetValue(null,green);
        static void SetLeeWaitsMissed(bool missed)=>typeof(ContinuousReview).GetField("leeWaitsMissed",Hidden).SetValue(null,missed);
        List<string> Failures=>Get<List<string>>("failures");
        IEnumerator Hands(QualityRoute route)
        {
            Set("pad",InputSystem.AddDevice<Gamepad>("OrielHandsPad"));
            Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);Get<System.Diagnostics.Stopwatch>("clock").Start();
        }
        void Step(QualityStep step)=>Call("BeginStep",step);
        IEnumerator Drive(float seconds,System.Func<bool> until=null,System.Action each=null)
        {
            for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end&&(until==null||!until());){each?.Invoke();Call("InputUpdate");yield return null;}
        }

        // ------------------------------------------------------------------ fixtures
        static CombatActor Lead=>PartyController.Current.Active;
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var hero in PartyController.Current.members)hero.GetComponent<PartnerBrain>().enabled=false;
            yield return new WaitForSecondsRealtime(.7f);
        }
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground)ground.Halt();else if(actor.motor is FlightMotor ship)ship.Halt();
            actor.transform.position=position;body.enabled=true;Physics.SyncTransforms();
        }

        // ------------------------------------------------------------------ the Orrery
        /// <summary>A point of the plan at the lane's middle, this far outside a rail's gate row (negative: inside it).</summary>
        static Vector3 OffTheRow(OrreryRail rail,float outside){var p=rail.On(0,rail.GateRadius+outside);return new Vector3(p.x,rail.plane,p.y);}
        /// <summary>The lee as the driver reads it, or (broken) green alone.</summary>
        static bool Read(OrreryRail rail,Vector3 at,float moonAt)=>Broken?!rail.GateShut(rail.NearestGate(rail.Along(at,rail.GateRadius)),moonAt):ContinuousReview.InLee(rail,at,moonAt);
        [UnityTest] public IEnumerator EveryRailsLeeComesEachTurnAndHoldsItsGateOpenLongEnoughToGoThrough()
        {
            yield return Zone("Orrery");
            var rails=Object.FindObjectsByType<OrreryRail>(FindObjectsSortMode.None).OrderByDescending(r=>r.radius).ToArray();
            Assert.AreEqual(9,rails.Length,"fixture: the Orrery's nine rails");
            var faults=new List<string>();
            foreach(var rail in rails)
            {
                var waiting=OffTheRow(rail,6);int gate=rail.NearestGate(rail.Along(waiting,rail.GateRadius));
                Assert.AreEqual(0,rail.gateAt[gate],.01f,"fixture: the lane's middle is the middle ring's");
                float inLee=0,worstOpen=float.PositiveInfinity;
                for(float t=0;t<OrreryRail.Period;t+=.02f)
                {
                    float at=rail.MoonAt(t);if(!Read(rail,waiting,at))continue;
                    inLee+=.02f;
                    // From a lee, the gate stays open while a ship crosses the row and the moon is still going away from where it crosses the rail.
                    float open=0;while(open<12&&!rail.GateShut(gate,rail.MoonAt(t+open)))open+=.05f;
                    worstOpen=Mathf.Min(worstOpen,open);
                    if(rail.AheadOfMoon(rail.GateAlongRail(gate),at)>=0)faults.Add($"{rail.railName}: read in the lee at {t:0.00} s with its moon still to come");
                }
                if(inLee<2)faults.Add($"{rail.railName}: in its moon's lee only {inLee:0.0} s of each {OrreryRail.Period} s turn");
                if(worstOpen<10)faults.Add($"{rail.railName}: read in the lee with its gate shutting {worstOpen:0.0} s later");
                // Read only near the row and in the lane: a ship 60 m off, or out by the wall, is waiting at nothing.
                for(float t=0;t<OrreryRail.Period;t+=.1f)
                {
                    if(ContinuousReview.InLee(rail,OffTheRow(rail,60),rail.MoonAt(t))){faults.Add($"{rail.railName}: its lee read from 60 m off its gate row");break;}
                    var outside=OffTheRow(rail,6);outside.x=rail.laneHalf+3;
                    if(ContinuousReview.InLee(rail,outside,rail.MoonAt(t))){faults.Add($"{rail.railName}: its lee read from outside the lane");break;}
                }
            }
            Assert.IsEmpty(faults,"what the driver reads as a moon's lee isn't the rule's:\n"+string.Join("\n",faults));
        }
        /// <summary>"until":"lee:n" names rail n of the zone outermost first, the way a ship meets them flying in, and is the arithmetic above for the gate nearest the lead: never true
        /// while that gate is green with its moon still to come, which a turn of the machine shows. Built wrong on purpose, the driver reads green alone.</summary>
        [UnityTest] public IEnumerator ARouteWaitingOnLeeOneEndsOnTheStewardsLeeAndNoOtherRails()
        {
            yield return Zone("Orrery");
            var rails=Object.FindObjectsByType<OrreryRail>(FindObjectsSortMode.None).OrderByDescending(r=>r.radius).ToArray();
            var steward=rails[0];Assert.AreEqual("The Steward",steward.railName,"fixture: the outermost rail is the Steward's");
            Assert.IsTrue(Lead.flight,"fixture: the lead flies in the Orrery");
            yield return Hands(new QualityRoute{name="oriel hands",scene="Orrery",steps=new QualityStep[0]});
            Step(new QualityStep{name="wait in the Steward's lee",seconds=60,navigate=true,point=OffTheRow(steward,6),tolerance=1.5f,until="lee:1",leftTrigger=1});
            var waiting=OffTheRow(steward,6);Place(Lead,waiting);SetLeeAsGreen(Broken);
            int agreed=0,read=0,greenOnly=0;var faults=new List<string>();int gate=steward.NearestGate(steward.Along(waiting,steward.GateRadius));
            yield return Drive(OrreryRail.Period+2,()=>faults.Count>3,()=>
            {
                bool driver=(bool)Call("Condition","lee:1"),rule=ContinuousReview.InLee(steward,Lead.transform.position,steward.Moon);
                if(driver==rule)agreed++;else faults.Add($"at {steward.Clock:0.00} s the driver read {driver} and the rule {rule}");
                if(driver)read++;
                if(!rule&&!steward.GateShut(gate,steward.Moon))greenOnly++;
                if((bool)Call("Condition","lee:2"))faults.Add("waiting at the Steward's gate, the Weigher's lee was read");
            });
            Assert.IsEmpty(faults,string.Join("\n",faults));
            Assert.Greater(read,0,"in a whole turn of the machine the driver never read the Steward's lee");Assert.Greater(agreed,100,"fixture: the driver was asked too seldom");
            Assert.Greater(greenOnly,30,"fixture: in a whole turn the Steward's gate was never green outside its moon's lee, so nothing here could tell the lee from green");
            int middle=steward.NearestGate(steward.Along(waiting,steward.GateRadius));Assert.AreEqual(0,steward.gateAt[middle],.01f,"fixture: the lane's middle is the middle ring's");
            var said=log.Where(l=>l.StartsWith("QUALITY_LEE ")).Distinct().ToArray();
            CollectionAssert.AreEqual(new[]{"QUALITY_LEE 1 The Steward gate "+middle},said,"the lee read isn't on the record as the Steward's middle ring, and only that");
            Assert.LessOrEqual(log.Count(l=>l.StartsWith("QUALITY_LEE ")),2,"the lee is put on the record every frame it holds, not once each time it comes");
        }

        /// <summary>On every rail after the first the green wave brings the lee while the ship is still braking toward its waiting place (route 38's first two runs in a player:
        /// eight of nine waits ended on their lee about eight metres short, and each was counted a missed checkpoint). The waiting place is only where to hold if the lee hasn't
        /// come: a wait the lee ends short of it is done, as a walk a stopWhen cuts short is. Built wrong on purpose, the driver judges it as it did.</summary>
        [UnityTest] public IEnumerator AWaitTheLeeEndsShortOfItsWaitingPlaceIsNoMissedCheckpoint()
        {
            yield return Zone("Orrery");
            var steward=Object.FindObjectsByType<OrreryRail>(FindObjectsSortMode.None).OrderByDescending(r=>r.radius).First();
            Assert.IsTrue(Lead.flight,"fixture: the lead flies in the Orrery");
            var waiting=OffTheRow(steward,6);Place(Lead,OffTheRow(steward,16));
            for(float end=Time.unscaledTime+OrreryRail.Period+2;Time.unscaledTime<end&&!ContinuousReview.InLee(steward,Lead.transform.position,steward.Moon);)yield return null;
            Assert.IsTrue(ContinuousReview.InLee(steward,Lead.transform.position,steward.Moon),"fixture: in a whole turn the Steward's lee never came to a ship sixteen metres short of its row");
            SetLeeWaitsMissed(Broken);
            var route=new QualityRoute{name="oriel hands",scene="Orrery",steps=new[]{new QualityStep{name="wait in the Steward's lee",expectedScene="Orrery",seconds=30,navigate=true,point=waiting,tolerance=1.5f,until="lee:1",leftTrigger=1}}};
            yield return Hands(route);var run=(IEnumerator)Call("RunSteps");
            for(float end=Time.unscaledTime+10;Time.unscaledTime<end&&run.MoveNext();){Call("InputUpdate");yield return null;}
            Assert.IsTrue(Get<bool>("endedByCondition"),"fixture: the wait ended on its lee");
            Assert.Greater(Flat(Lead.transform.position,waiting),1.5f,"fixture: the lee came before the ship was at its waiting place");
            Assert.IsEmpty(Failures,"a wait the lee ended short of its waiting place was judged a failure");
        }

        /// <summary>Route 41 as the player plays it, by its own steps: from the heart landing's checkpoint down the stair, the Warden's office, the door walk, the lock and the first
        /// round, through the driver, with Taren and Idra out at the chapter's Sync and Idra's own brain following. In its first run in a player the door walk stopped the lead 4.7 m
        /// past the hall's seal, Idra followed three metres behind and stood on its line, and the fight (which waits until every hero is through) never began: the lead stood idle
        /// with nothing to lock for twelve rounds (November 14). Built wrong on purpose, the door walk ends where it did.</summary>
        [UnityTest] public IEnumerator RouteFortyOnesOwnApproachBeginsTheWardensFightAndTheLeadFightsIt()
        {
            DevLoadout.Apply("chapter7");
            foreach(string flag in new[]{"oriel.doorReached","oriel.choirSeen","oriel.releaseLearned","oriel.stay1","oriel.stay2","oriel.stay3","clear.Choir_Sentinels","clear.Choir_Fibres","clear.Choir_Tenders"})
                GameServices.Current.Flags.SetBool(flag,true);
            SceneFlow.Current.LoadZone("GlassChoir","Door");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: the Glass Choir never finished loading");
            deadline=Time.unscaledTime+10;while(!Lattice.Dialogue.DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.8f);
            CollectionAssert.AreEquivalent(new[]{"Taren","Idra"},PartyController.Current.members.Where(m=>m.gameObject.activeInHierarchy).Select(m=>m.character).ToArray(),"fixture: Taren and Idra are out");
            var fight=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(v=>v.encounterId=="LastWarden");
            Assert.IsFalse(fight.Started,"fixture: the Warden's fight hasn't begun at the checkpoint");
            // The route's own steps, from the lead taken after the load to the end of its first round.
            string file=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes","chapter-41-last-warden.json");
            var route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));
            int from=System.Array.FindIndex(route.steps,s=>s.ensureCharacter=="Taren"),to=System.Array.FindIndex(route.steps,s=>s.name=="fight LastWarden round 1");
            Assert.That(from>=0&&to>from,"fixture: route 41 leads with Taren and has a first round");
            var steps=route.steps.Skip(from).Take(to-from+1).ToArray();
            var doorStep=steps.First(s=>s.bossDoor=="LastWarden");if(Broken){doorStep.point.z=281;doorStep.tolerance=1.5f;}
            route.steps=steps;
            yield return Hands(route);var run=(IEnumerator)Call("RunSteps");
            var lead=Lead;var states=new HashSet<ActorState>();double roundFrom=-1;var clock=Get<System.Diagnostics.Stopwatch>("clock");
            for(float end=Time.unscaledTime+120;Time.unscaledTime<end&&run.MoveNext();)
            {
                Call("InputUpdate");
                if(Get<int>("stepIndex")==steps.Length-1)
                {
                    if(roundFrom<0)roundFrom=clock.Elapsed.TotalSeconds;
                    states.Add(lead.State);
                    if(fight.Started&&states.Contains(ActorState.Attack)||clock.Elapsed.TotalSeconds-roundFrom>15)break;
                }
                yield return null;
            }
            Assert.Greater(roundFrom,0,"the route never reached its first round: "+string.Join(", ",Failures));
            Assert.IsTrue(fight.Started,"fifteen seconds into its first round the Warden's fight hadn't begun: a hero is still on the seal's side of the hall ("
                +string.Join(", ",PartyController.Current.members.Where(m=>m.gameObject.activeInHierarchy).Select(m=>$"{m.character} at z {m.transform.position.z:0.0}"))+")");
            Assert.IsTrue(states.Contains(ActorState.Attack)||states.Contains(ActorState.Move),"in the fight's first round the lead neither moved nor swung: "+string.Join(",",states));
        }

        // ------------------------------------------------------------------ the Last Warden's clamps
        const float X=230;
        static Vector3 At(float x,float z)=>new(X+x,.05f,z);
        static void Floor(){var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Oriel hands floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        /// <summary>The engine hall as its contract has it (OrielCombatTests.Hall): four clamps' seats 11 m out on bearings 45, 315, 225, 135 facing the middle, and twelve fibres from
        /// 2.6 to 17.5 m every thirty degrees from 15, the middle one of each three under its clamp's seat.</summary>
        static BridleClamp[] Hall(Vector3 centre)
        {
            Vector3 On(float bearing,float r)=>centre+Quaternion.Euler(0,bearing,0)*Vector3.forward*r;
            var bearings=new[]{45f,315f,225f,135f};var clamps=new BridleClamp[4];
            for(int i=0;i<4;i++)
            {
                var go=new GameObject("Bridle clamp "+(i+1));go.SetActive(false);var seat=On(bearings[i],11);go.transform.SetPositionAndRotation(seat,Quaternion.LookRotation((centre-seat).normalized));
                var clamp=go.AddComponent<BridleClamp>();clamp.index=i+1;clamp.settledBy="LastWarden";clamp.openedFlag="test.clamp"+(i+1);go.SetActive(true);clamps[i]=clamp;
            }
            for(int k=0;k<12;k++)
            {
                float bearing=15+30*k;int owner=bearing<90?1:bearing<180?4:bearing<270?3:2;
                var go=new GameObject("Fixture fibre");go.SetActive(false);go.transform.position=On(bearing,2.6f);var line=go.AddComponent<ForceLine>();line.end=On(bearing,17.5f);line.clamp=owner;go.SetActive(true);
            }
            return clamps;
        }
        /// <summary>The Warden at the hall's middle, standing still, fought down to its first line: the road held, the four seats lit.</summary>
        IEnumerator HeldLine(Vector3 centre)
        {
            var warden=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("LastWarden"),centre+Vector3.up*.05f);warden.Passive=true;yield return Frames(3);
            var body=warden.Health;body.Receive(new DamagePacket{source=Lead.Health,amount=body.maximum,type=System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance)});
            yield return Frames(3);
            Assert.IsTrue(warden.GetComponent<BossController>().RoadHeld,"fixture: fought down to its first line, the Warden holds the road");
            Assert.IsTrue(BridleClamp.All.All(c=>c.Available),"fixture: at its line every clamp is offered");
            Lead.target=body;Lead.TargetLocked=true;
        }
        static QualityStep FightRound()=>new(){name="fight the Last Warden",seconds=120,buttons=new[]{"South"},pulseSeconds=.46f,navigate=true,approachTarget=true,tolerance=2.3f,
            rangedTolerance=12,evadeTelegraphs=true,stopWhen="flag:bossdown.LastWarden",point=Vector3.zero};
        static float OffItsFibre(BridleClamp clamp,Vector3 centre,Vector3 at)
        {
            var along=clamp.transform.position-centre;along.y=0;along.Normalize();var off=at-centre;off.y=0;return Vector3.Cross(along,off).magnitude;
        }
        [UnityTest] public IEnumerator AtAHeldLineTheDriverTakesAClampsWeightBesideItsFibreAndHoldsStillUntilItOpensInItsHands()
        {
            yield return Zone("Arena_Ground");Floor();var centre=At(0,0);var clamps=Hall(centre);yield return Frames(2);
            foreach(var hero in PartyController.Current.members)Place(hero,hero==Lead?At(0,-6):At(-50,-50));
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+300;
            yield return HeldLine(centre);
            // Built wrong on purpose: the driver as it was, which chases the Warden and swings at a line that takes nothing.
            SetHandsOff(Broken);
            yield return Hands(new QualityRoute{name="oriel hands",scene="Arena_Ground",steps=new QualityStep[0]});Step(FightRound());
            yield return Drive(12,()=>BridleClamp.Held!=null);
            var held=BridleClamp.Held;
            Assert.IsNotNull(held,"at the Warden's held line, its four seats lit, the driver never took a clamp's weight: "+string.Join(", ",log));
            Assert.AreEqual(Lead,held.Holder,"the lead's hands aren't on the clamp");Assert.IsTrue(log.Any(l=>l.StartsWith("CLAMP_TAKEN "+held.index)),"the clamp's weight wasn't taken by its prompt");
            Assert.LessOrEqual(Flat(Lead.transform.position,held.transform.position),held.range,"the lead took the weight from outside its seat");
            Assert.Greater(OffItsFibre(held,centre,Lead.transform.position),.9f+Telegraph.BodyAllowance,"the lead stands on the clamp's fibre, where every strike down it lands");
            // Held: nothing pressed, no step taken, through a whole pull and past it.
            var states=new HashSet<ActorState>();var from=Lead.transform.position;
            yield return Drive(2.5f,null,()=>states.Add(Lead.State));
            Assert.AreEqual(Lead,held.Holder,"the driver let go of the clamp before its slack: "+string.Join(",",states));
            Assert.IsFalse(states.Contains(ActorState.Attack)||states.Contains(ActorState.Skill)||states.Contains(ActorState.Dodge),"holding a clamp, the lead swung, rolled or used a skill: "+string.Join(",",states));
            Assert.Less(Flat(Lead.transform.position,from),.3f,"holding a clamp, the lead walked");
            Assert.AreEqual(1,held.Weight,.01f,"held for 2.5 s, its weight isn't taken");Assert.AreEqual(BridleClamp.Hold.Shut,held.State,"it opened against the pull");
            // The staff down (a Draw on nobody, a Toll's ringing, a break): the clamp eases and opens in the lead's hands.
            var warden=Health.All.First(h=>h!=null&&h.id=="LastWarden");warden.Expose(3);
            yield return Drive(3,()=>held.State!=BridleClamp.Hold.Shut);
            Assert.AreEqual(BridleClamp.Hold.Open,held.State,"held through the slack, the clamp didn't open");Assert.AreEqual(Lead,held.OpenedBy,"the clamp opened in other hands");
        }
        [UnityTest] public IEnumerator WithAClampTenderUpTheDriverLocksOntoItAndTakesNoClampUntilItIsStopped()
        {
            yield return Zone("Arena_Ground");Floor();var centre=At(0,0);var clamps=Hall(centre);yield return Frames(2);
            foreach(var hero in PartyController.Current.members)Place(hero,hero==Lead?At(0,-6):At(-50,-50));
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+300;
            yield return HeldLine(centre);
            var seat=clamps[3].transform.position;var tender=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ClampTender"),seat+clamps[3].transform.right*2.5f);tender.Passive=true;yield return Frames(3);
            // Built wrong on purpose: no hand for clamps or tenders.
            SetHandsOff(Broken);
            yield return Hands(new QualityRoute{name="oriel hands",scene="Arena_Ground",steps=new QualityStep[0]});Step(FightRound());
            yield return Drive(4,()=>Lead.target==tender.Health&&Lead.TargetLocked);
            Assert.AreEqual(tender.Health,Lead.target,"with a Clamp Tender up beside a clamp, the lock stayed on the Warden, whose held line takes nothing");
            Assert.IsNull(BridleClamp.Held,"the driver took a clamp's weight with a tender up to close it");
            // Stopped (it is fragile): the seats are the driver's again.
            tender.Health.Receive(new DamagePacket{source=Lead.Health,amount=tender.Health.maximum*10,type=DamageType.Kinetic});yield return Frames(2);
            yield return Drive(12,()=>BridleClamp.Held!=null);
            Assert.IsNotNull(BridleClamp.Held,"with the tender stopped, the driver never went back to a clamp");
        }
    }
}
