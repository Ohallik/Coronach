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
    /// <summary>The replay driver's hands for chapter 8's rooms (ContinuousReview.Rooms.cs, ReplayWays), each played through the driver's own virtual pad on the built zone or the proving
    /// yard, nothing called on an actor: the Picket of Teeth's quiet route flown pocket to pocket, each leg set off on only once the way is clear of the beams
    /// (<c>"until":"clear:..."</c>), and never seen; the Scoria Flows' first tongue crossed only once the crust on the way will hold, and nobody thrown; and the Slag Colossus's heart
    /// worked as a person works it, a sluice's wheel turned, the lock flicked across to the plate it darkens, the plate broken. Each is shown failing first, in the same case, as the
    /// driver was without the hand: the legs flown straight on, the tongue walked at once, the fight chased at its body.</summary>
    public sealed class HarrowgateReplayTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;bool finished;readonly List<string> log=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Heeded={"PICKET_","CRUST_THROWN","SLUICE_","PLATE_","QUALITY_WAIT","QUALITY_CLEAR","COLOSSUS_","WALL_CONTACT","CURTAIN_BURST","LOCK_","STRAFER_","QUALITY_TOWARD_FIGHT","QUALITY_READS_TALK"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("chapter8");
            fixture=new GameObject("Harrowgate replay fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
            log.Clear();finished=false;Application.logMessageReceived+=Logged;HandsOff(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;HandsOff(false);ChaseOff(false);
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(fixture!=null)Object.Destroy(fixture);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the driver, by its private parts (as ReplayHandsTests drives it)
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        List<string> Failures=>Get<List<string>>("failures");
        /// <summary>The driver without its room hands, for the broken state of a case: put back by every case and by the tear-down.</summary>
        static void HandsOff(bool off)=>typeof(ContinuousReview).GetField("roomHandsOff",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,off);
        IEnumerator Hands(QualityRoute route)
        {
            if(Get<Gamepad>("pad")==null)Set("pad",InputSystem.AddDevice<Gamepad>("HarrowgateReplayPad"));
            Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);var clock=Get<System.Diagnostics.Stopwatch>("clock");if(!clock.IsRunning)clock.Start();
        }
        /// <summary>Plays a route's steps as the player does (RunSteps), a frame at a time, calling <paramref name="each"/> every frame.</summary>
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
        static GameState State=>GameServices.Current.State;
        static CombatActor Lead=>PartyController.Current.Active;
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship)ship.Halt();else if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.8f);
        }
        static string Way(params Vector2[] points)=>"clear:"+string.Join(";",points.Select(p=>p.x.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+","+p.y.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)));
        static QualityStep Walk(string name,Vector2 to,float y,float tolerance,float seconds,bool brake=false)=>
            new(){name=name,seconds=seconds,navigate=true,point=new Vector3(to.x,y,to.y),tolerance=tolerance,until="arrived",leftTrigger=brake?1:0};

        // ================================================================== the Picket of Teeth: shadow to shadow
        /// <summary>The quiet route as the map brief has it (HarrowgateBuilder.Picket.Quiet: pockets and the points a leg bends round a rock at).</summary>
        static readonly (Vector2 at,bool pocket)[] Quiet=
        {
            (new(0,110),false),(new(2,160),true),(new(-26,190),true),(new(-21.7f,200),false),(new(-6,228),true),(new(0,300),false),(new(-22,322),true),(new(-6,346),true),
            (new(12.1f,365.4f),false),(new(13.5f,368.1f),false),(new(14,372),true),(new(-4,398),true),(new(-20,424),true),(new(-21,429.4f),false),(new(-19.2f,433.9f),false),
            (new(0,448),true),(new(-.3f,452.6f),false),(new(1.3f,456.4f),false),(new(20,474),true),(new(2.5f,490.8f),false),(new(1,495.1f),false),(new(2,500),true),
            (new(-22,526),true),(new(-24.7f,528.7f),false),(new(-26,531.3f),false),(new(-36,574),true),
        };
        /// <summary>The route's steps over the quiet route from (0, 110): for each leg, a wait in the pocket until the way to the next is clear (unless <paramref name="blind"/>),
        /// the leg's bends and its pocket at speed, and a braked settle into the pocket.</summary>
        static QualityStep[] Legs(bool blind)
        {
            var steps=new List<QualityStep>();var leg=new List<Vector2>();int n=0;
            for(int i=1;i<Quiet.Length;i++)
            {
                leg.Add(Quiet[i].at);if(!Quiet[i].pocket)continue;n++;
                if(!blind)steps.Add(new QualityStep{name=$"wait for leg {n}",seconds=40,leftTrigger=1,until=Way(leg.ToArray())});
                for(int k=0;k<leg.Count-1;k++)steps.Add(Walk($"leg {n} bend {k+1}",leg[k],1,2,12));
                steps.Add(Walk($"leg {n} to its pocket",leg[^1],1,2,14));
                steps.Add(Walk($"settle into pocket {n}",leg[^1],1,.8f,8,true));
                leg.Clear();
            }
            return steps.ToArray();
        }
        IEnumerator FlyTheQuietRoute(bool blind,System.Action each,System.Func<bool> enough=null)
        {
            Flag("vask.brekkWord");Flag("vask.lockAdmitted");Flag("vask.picketRead");State.questSteps["Harrowgate"]=2;
            yield return Zone("PicketOfTeeth");
            var partner=PartyController.Current.members.First(m=>m!=Lead);partner.GetComponent<PartnerBrain>().enabled=false;Put(partner,new Vector3(-40,1,20));
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+900;
            Put(Lead,new Vector3(0,1,110));yield return Frames(3);
            Assert.IsNotNull(PicketAlarm.Current,"fixture: the Picket has its alarm");Assert.AreEqual(0,PicketAlarm.Current.Stage,"fixture: the picket starts quiet");
            yield return Play(new QualityRoute{name="the quiet route",scene="PicketOfTeeth",steps=Legs(blind)},blind?150:420,each,enough);
        }
        [UnityTest] public IEnumerator TheQuietRouteFlownByTheDriverWaitsInEachPocketForTheWayToClearAndIsNeverSeen()
        {
            // Built as the driver was without the wait: each leg flown the moment the last pocket is reached. The beams find the ship.
            bool seen=false;
            yield return FlyTheQuietRoute(true,()=>seen|=PicketAlarm.Current!=null&&PicketAlarm.Current.Seen,()=>seen);
            Assert.IsTrue(seen,"fixture: the quiet route flown leg after leg without a wait was never lit: the check below proves nothing");
            // Each leg set off on only once the way to the next pocket is clear: never in a beam, never a stage, and every pocket reached.
            log.Clear();seen=false;float heard=0;var where=new List<string>();
            yield return FlyTheQuietRoute(false,()=>{var alarm=PicketAlarm.Current;if(alarm==null)return;if(alarm.Seen&&!seen)where.Add($"{Lead.transform.position.x:0.0},{Lead.transform.position.z:0.0} in step {Get<int>("stepIndex")}");seen|=alarm.Seen;heard=Mathf.Max(heard,alarm.Heard);});
            Assert.IsTrue(finished,"the quiet route never ended");
            CollectionAssert.IsEmpty(Failures,"the driver's quiet route failed a step");
            Assert.IsFalse(seen,"the ship was seen on the quiet route though every leg waited for the way to clear, first at "+string.Join(" ",where)+" (most heard "+heard.ToString("0.00")+")");
            Assert.AreEqual(0,PicketAlarm.Current.Stage,"the picket rose a stage on the quiet route");
            Assert.Less(Flat(Lead.transform.position,new Vector3(-36,1,574)),1.5f,"the ship didn't reach the last pocket, under B4's west flank");
            Assert.Greater(log.Count(l=>l.StartsWith("QUALITY_WAIT")),3,"fixture: the driver never had to wait for a beam: the waits proved nothing");
        }

        // ================================================================== the Picket of Teeth with Idra out: route 45's own steps
        static QualityRoute Route(string name)
        {
            string file=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes",name+".json");Assert.IsTrue(System.IO.File.Exists(file),"fixture: "+file+" (python tools/harrowgate_routes.py writes it)");
            return JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));
        }
        /// <summary>The steps of a route from one named step to another, both kept, less any named.</summary>
        static QualityStep[] Between(QualityRoute route,string first,string last,params string[] without)
        {
            int a=System.Array.FindIndex(route.steps,s=>s.name==first),b=System.Array.FindIndex(route.steps,s=>s.name==last);
            Assert.GreaterOrEqual(a,0,"fixture: "+route.name+" has no step '"+first+"'");Assert.GreaterOrEqual(b,a,"fixture: "+route.name+" has no step '"+last+"' after '"+first+"'");
            return route.steps.Skip(a).Take(b-a+1).Where(s=>!without.Contains(s.name)).ToArray();
        }
        /// <summary>The Picket as route 45 flies it: past Idra's lesson, with the pair chapter 8 is played with out (Taren and Idra).</summary>
        static void Picketed()
        {
            Flag("vask.brekkWord");Flag("vask.lockAdmitted");Flag("vask.picketRead");State.questSteps["Harrowgate"]=2;
            ContinuousReview.ApplyPair(new QualityRoute{name="the Picket",scene="PicketOfTeeth",deployed=new[]{"Taren","Idra"}});
        }
        static IEnumerator TarenLeads()
        {
            for(int i=0;i<4&&Lead.character!="Taren";i++){PartyController.Current.Swap();yield return Frames(30);}
            Assert.AreEqual("Taren",Lead.character,"fixture: Taren leads the Picket");
            Assert.AreEqual("Idra",PartyController.Current.members.First(m=>m!=Lead).character,"fixture: Idra is out beside him");
        }
        /// <summary>Where Idra keeps when she follows (PartnerBrain): three metres behind the lead's nose and two to its right.</summary>
        static Vector3 PlaceBehind(CombatActor lead){var nose=lead.motor.Facing;return lead.transform.position-nose*3+Vector3.Cross(Vector3.up,nose).normalized*2;}

        /// <summary>Pocket 9 as reconnaissance s45 left it (November 14): the settle ran past the pocket and came back to it, so the nose points south, back the way leg 9
        /// came, and Idra is in her place behind it. Then route 45's own steps up to leg 10's first bend (the nose's turn, the moment Idra is given to come round behind it,
        /// the wait for the beams), less the first two if <paramref name="asItWas"/>: and where Idra is as the leg sets off, whether across its first stretch (inside the
        /// two hulls of the line from the lead to the bend, ahead of him). Then the leg itself.</summary>
        readonly List<string> beforeLeg=new();
        IEnumerator LegTen(bool asItWas,bool[] across)
        {
            Picketed();
            yield return Zone("PicketOfTeeth");yield return TarenLeads();
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+900;
            var partner=PartyController.Current.members.First(m=>m!=Lead);
            Put(Lead,new Vector3(.18f,1,448.57f));((FlightMotor)Lead.motor).KeepFacing(Vector3.back);
            Put(partner,PlaceBehind(Lead));yield return Frames(20);
            Assert.IsTrue(partner.GetComponent<PartnerBrain>().enabled,"fixture: Idra follows the lead");
            var route=Route("chapter-45-picket-of-teeth");
            var before=asItWas?Between(route,"wait in the shadow for leg 10","wait in the shadow for leg 10"):Between(route,"turn the nose down leg 10","wait in the shadow for leg 10");
            yield return Play(new QualityRoute{name="before leg 10",scene="PicketOfTeeth",steps=before},60);
            beforeLeg.Clear();beforeLeg.AddRange(Failures);
            var bend=route.steps.First(s=>s.name=="leg 10 round the rock 1").point;
            Vector2 from=new(Lead.transform.position.x,Lead.transform.position.z),to=new(bend.x,bend.z),her=new(partner.transform.position.x,partner.transform.position.z);
            var way=(to-from).normalized;float ahead=Vector2.Dot(her-from,way),off=Mathf.Abs(Vector2.Dot(her-from,new Vector2(way.y,-way.x)));
            across[0]=ahead>0&&off<HeroCollision.HullRadius("Taren")+HeroCollision.HullRadius("Idra");
            TestContext.WriteLine($"leg 10 sets off from {from} with Idra at {her}: {ahead:0.00} m ahead along it and {off:0.00} m off it");
            yield return Play(new QualityRoute{name="leg 10",scene="PicketOfTeeth",steps=Between(route,"leg 10 round the rock 1","settle into pocket 10")},60);
        }
        [UnityTest] public IEnumerator InAPocketTheNoseIsTurnedDownTheNextLegSoIdraIsNeverAcrossIt()
        {
            // As route 45 flew it: off down leg 10 with the nose the wrong way. Idra is in its place behind the nose, across the leg, and the lead slid round her hull into Rock 8.
            var across=new bool[1];
            yield return LegTen(true,across);
            Assert.IsTrue(across[0],"fixture: with the nose turned back south Idra wasn't across leg 10: the check below proves nothing");
            log.Clear();
            yield return LegTen(false,across);
            Assert.IsFalse(across[0],"Idra was across leg 10 as it set off, though the nose was turned down it and she was given a moment to come round");
            CollectionAssert.IsEmpty(beforeLeg,"the nose's turn or the wait before leg 10 failed a step");
            Assert.IsTrue(finished,"leg 10 never ended");
            CollectionAssert.IsEmpty(Failures,"leg 10 failed a step with the nose turned down it first");
            Assert.IsFalse(log.Any(l=>l.StartsWith("WALL_CONTACT")&&l.Contains("Rock ")),"the lead struck a rock on leg 10 with the nose turned down it first: "+string.Join(" | ",log.Where(l=>l.StartsWith("WALL_CONTACT"))));
            Assert.Less(Flat(Lead.transform.position,new Vector3(20,1,474)),1.5f,"the lead never reached pocket 10");
        }

        /// <summary>The lock as route 45 worked it before (reconnaissance s45, November 14): from z 629.4, in the flak curtain's front row, the walks to the last three pieces braked
        /// from end to end on the budget of an unbraked one. Its hunter wing's rounds are left out: there was never a wing to chase.</summary>
        static QualityStep[] TheLockAsItWas()
        {
            var steps=new List<QualityStep>();
            void Press(string name,string flag)
            {
                steps.Add(new QualityStep{name="settle before "+name,seconds=.5f});
                steps.Add(new QualityStep{name=name,seconds=4,buttons=new[]{"South"},pulseSeconds=.6f,until="flag:"+flag,expectedFlag=flag});
                steps.Add(new QualityStep{name="release "+name,seconds=.5f});
            }
            var stand=new Vector2(-4,629.4f);
            steps.Add(Walk("to the port collar",stand,1,2,23.82f));steps.Add(Walk("settle at the port collar",stand,1,.8f,6,true));Press("take the port collar off","vask.lock1");
            steps.Add(Walk("south out of the flak gates' reach",new(0,592),1,3,7.47f));steps.Add(Walk("back toward the lock",new(0,612),1,3,23.82f));
            foreach(var (name,at,flag,seconds) in new[]{("starboard collar",new Vector2(4,629.4f),"vask.lock2",4.6f),("keel collar",new Vector2(0,629.4f),"vask.lock3",2.58f),("keeper bar",new Vector2(0,629.4f),"vask.lock4",2f)})
            {
                var walk=Walk("to the "+name,at,1,.8f,seconds,true);walk.evadeTelegraphs=true;steps.Add(walk);
                Press("take the "+name+(name.Contains("bar")?" out":" off"),flag);
            }
            return steps.ToArray();
        }
        /// <summary>From where the lock in sight stops the ship (z 606, just past its line) with Idra behind it, the given steps, nobody spared: what the curtain does to the lead,
        /// and the least the lead's and Idra's hulls came to.</summary>
        IEnumerator AtTheLock(QualityStep[] steps,float[] least,bool sturdy=false)
        {
            Picketed();Flag("vask.picketCrossed");Flag("clear.Buoy_4");
            // A lock whole again for each case: its pieces load as their flags say.
            foreach(string flag in LockFlags.Append("vask.lockRemoved"))GameServices.Current.Flags.SetBool(flag,false);
            yield return Zone("PicketOfTeeth","Lock");yield return TarenLeads();
            // The flak gates are on show from the load, asleep until the first piece is off: Zone() put every brain it found to rest, and these must fire.
            int gates=0;foreach(var brain in Object.FindObjectsByType<EnemyBrain>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(brain.definition!=null&&brain.definition.id=="FlakGate"){brain.Passive=false;gates++;}
            Assert.AreEqual(2,gates,"fixture: the lock's two flak gates are on show");
            var partner=PartyController.Current.members.First(m=>m!=Lead);
            Put(Lead,new Vector3(0,1,606));((FlightMotor)Lead.motor).KeepFacing(Vector3.forward);Put(partner,PlaceBehind(Lead));yield return Frames(20);
            // Whole to begin with; sturdy (the case as it was), deep enough that no volley ends the run, so what struck the lead is still counted.
            var lead=Lead;var bodies=new[]{lead.Health,partner.Health};var maxima=bodies.Select(b=>b.maximum).ToArray();
            foreach(var body in bodies){if(sturdy)body.maximum=100000;body.integrity=body.maximum;}
            least[0]=bodies[0].integrity;least[1]=bodies[1].integrity;
            yield return Play(new QualityRoute{name="the lock",scene="PicketOfTeeth",steps=steps},150,()=>{least[0]=Mathf.Min(least[0],bodies[0].integrity);least[1]=Mathf.Min(least[1],bodies[1].integrity);});
            // Put back as they were, the least each came to counted down from its own whole.
            if(sturdy)for(int i=0;i<2;i++){least[i]-=bodies[i].maximum-maxima[i];bodies[i].maximum=maxima[i];bodies[i].integrity=maxima[i];}
        }
        static readonly string[] LockFlags={"vask.lock1","vask.lock2","vask.lock3","vask.lock4"};
        [UnityTest] public IEnumerator TheLockIsWorkedFromItsLineWhereTheFlakCurtainNeverReaches()
        {
            // As route 45 worked it: in the curtain's front row, rolled back by every volley, braked walks too slow to get there.
            var least=new float[2];
            yield return AtTheLock(TheLockAsItWas(),least,true);
            bool all=LockFlags.All(f=>GameServices.Current.Flags.GetBool(f));
            Assert.IsTrue(!all||least[0]<Lead.Health.maximum||Failures.Count>0,"fixture: the lock worked from the curtain's front row came off whole, on time, and the lead was never struck: the check below proves nothing");
            TestContext.WriteLine("as it was: "+(all?"every piece off":"pieces left on")+$", the lead's least {least[0]:0} of {Lead.Health.maximum:0}, "+Failures.Count+" failed: "+string.Join(" | ",Failures.Take(6)));
            log.Clear();
            // Route 45's own steps now: the lock's line, the four pieces, Idra's word, through the gap.
            var route=Route("chapter-45-picket-of-teeth");
            yield return AtTheLock(Between(route,"toward the port collar","through the gap"),least);
            Assert.IsTrue(finished,"the lock never ended");
            CollectionAssert.IsEmpty(Failures,"the lock failed a step");
            foreach(string flag in LockFlags.Append("vask.lockRemoved"))Assert.IsTrue(GameServices.Current.Flags.GetBool(flag),"the lock worked from its line never set "+flag);
            Assert.IsTrue(log.Any(l=>l.StartsWith("CURTAIN_BURST")),"fixture: the flak gates never fired: the line was never tested");
            Assert.AreEqual(Lead.Health.maximum,least[0],.01f,"the lead was struck working the lock from its line (the curtain or the seal): "+string.Join(" | ",log.Where(l=>l.StartsWith("CURTAIN_BURST")||l.StartsWith("WALL_CONTACT"))));
            var partner=PartyController.Current.members.First(m=>m!=Lead);
            Assert.IsTrue(partner.Health.Alive,"Idra went down at the lock (her least "+least[1].ToString("0")+")");
            TestContext.WriteLine($"from the line: the lead's least {least[0]:0} of {Lead.Health.maximum:0}, Idra's {least[1]:0} of {partner.Health.maximum:0}");
            Assert.Less(PicketAlarm.Current.Stage,PicketAlarm.Carrier,"the picket rose to its carrier at the lock");
            Assert.Greater(Lead.transform.position.z,640f,"the lead never went through the gap");
        }

        // ================================================================== the Scoria Flows: the crust tells you
        /// <summary>The first tongue's crossing on the trodden line: from the south bank to the north, over F2 (HarrowgateBuilder.Scoria.Ways).</summary>
        static readonly Vector2 SouthBank=new(5,40),NorthBank=new(4,53);
        IEnumerator CrossTheFirstTongue(bool blind)
        {
            Flag("vask.crustRead");Flag("vask.brekkHail1");
            yield return Zone("ScoriaFlows");
            var partner=PartyController.Current.members.First(m=>m!=Lead);partner.GetComponent<PartnerBrain>().enabled=false;Put(partner,new Vector3(9,0,6));
            Put(Lead,new Vector3(SouthBank.x,.05f,SouthBank.y));yield return Frames(3);
            var field=CrustField.Current;Assert.IsNotNull(field,"fixture: the Flows have their crust");
            // The plate under the line's middle set to open as the walk reaches it: a body leaving the bank now is on it as it goes.
            var middle=new Vector3(4.5f,-.3f,47);var plate=field.At(middle);Assert.IsNotNull(plate,"fixture: no plate of F2 under the trodden line at z 47");
            field.Clock=CrustField.ClockFor(plate,plate.dark+plate.seam-.6f);
            var steps=new List<QualityStep>();
            if(!blind)steps.Add(new QualityStep{name="wait on the bank for the crust",seconds=30,until=Way(NorthBank)});
            steps.Add(Walk("over the first tongue",NorthBank,0,.6f,12));
            yield return Play(new QualityRoute{name="the first tongue",scene="ScoriaFlows",steps=steps.ToArray()},45);
        }
        [UnityTest] public IEnumerator OnTheFlowsTheDriverWaitsAtTheBankUntilTheCrustWillHoldAndNobodyIsThrown()
        {
            // Built as the driver was without the wait: walked at once, onto a plate as it opens.
            yield return CrossTheFirstTongue(true);
            Assert.IsTrue(log.Any(l=>l.StartsWith("CRUST_THROWN")),"fixture: the walk straight over a plate as it opened wasn't thrown: the check below proves nothing");
            log.Clear();
            yield return CrossTheFirstTongue(false);
            Assert.IsTrue(finished,"the crossing never ended");
            Assert.IsFalse(log.Any(l=>l.StartsWith("CRUST_THROWN")),"the driver waited for the crust and was thrown all the same: "+string.Join(" | ",log));
            Assert.IsTrue(log.Any(l=>l.StartsWith("QUALITY_WAIT")),"fixture: the driver never had to wait at the bank");
            Assert.Less(Flat(Lead.transform.position,new Vector3(NorthBank.x,0,NorthBank.y)),1.2f,"the walk over the first tongue didn't reach the north bank");
            CollectionAssert.IsEmpty(Failures,"the crossing failed a step");
        }

        // ================================================================== a fight's last body, and a talk in a fight (ContinuousReview.Chase.cs)
        /// <summary>The driver without the chase's hands (RotbloomDriverTests' switch), for a case's broken state: put back by every case and by the tear-down.</summary>
        static void ChaseOff(bool off)=>typeof(ContinuousReview).GetField("chaseHandsOff",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,off);
        const float Yard=-230;
        static Vector3 InYard(float x,float y,float z)=>new(Yard+x,y,z);
        IEnumerator ProvingYard()
        {
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+25;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Yard fixture floor";floor.transform.position=new Vector3(Yard,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.6f);
            var partner=PartyController.Current.members.First(m=>m!=Lead);partner.GetComponent<PartnerBrain>().enabled=false;Put(partner,InYard(50,.05f,50));
            foreach(var hero in PartyController.Current.members){hero.Health.InvulnerableUntil=GameTime.Now+900;hero.charge=100;}
        }
        /// <summary>A fight as a route writes one round of it: chase, strike, dodge what's marked.</summary>
        static QualityStep Round(string name,float seconds)=>new(){name=name,seconds=seconds,buttons=new[]{"South"},pulseSeconds=.46f,navigate=true,approachTarget=true,tolerance=2.3f,
            rangedTolerance=12,evadeTelegraphs=true};
        /// <summary>The gallery's last body as reconnaissance s48 left it (November 14): a pour drone on its rail 24 m behind a lead who has just killed the other one, so nothing is in front
        /// of him, and the lock press takes nothing behind him past 21 m (PlayerBrain.FindTarget); the drone keeps its rail and never comes.</summary>
        IEnumerator LastDrone(EnemyBrain[] drone)
        {
            yield return ProvingYard();
            Put(Lead,InYard(0,.05f,0));((GroundMotor)Lead.motor).KeepFacing(Vector3.forward);Lead.transform.rotation=Quaternion.identity;Lead.target=null;Lead.TargetLocked=false;
            drone[0]=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("PourDrone"),InYard(0,.1f,-24));drone[0].transform.rotation=Quaternion.Euler(0,90,0);
            yield return Frames(5);
            Assert.IsTrue(drone[0].enabled&&!drone[0].Passive,"fixture: the drone is awake");
        }
        [UnityTest] public IEnumerator InAnOrdinaryFightTheDriverWalksToTheLastBodyTheLockCannotSee()
        {
            var drone=new EnemyBrain[1];
            // As the driver was: nothing locked, nothing in front, and it stands where it is.
            yield return LastDrone(drone);ChaseOff(true);
            yield return Play(new QualityRoute{name="the last drone",scene="Arena_Ground",steps=new[]{Round("fight the last drone",15)}},20);
            ChaseOff(false);
            Assert.IsTrue(drone[0]!=null&&drone[0].Health.Alive,"fixture: the drone behind the lead died without the walk to it: the check below proves nothing");
            Assert.Less(Flat(Lead.transform.position,InYard(0,0,0)),3f,"fixture: without the walk the lead went somewhere all the same");
            log.Clear();
            yield return LastDrone(drone);
            yield return Play(new QualityRoute{name="the last drone",scene="Arena_Ground",steps=new[]{Round("fight the last drone",60)}},70,null,()=>drone[0]==null||!drone[0].Health.Alive);
            Assert.IsTrue(drone[0]==null||!drone[0].Health.Alive,"the lead never killed the drone 24 m behind him: he ended at "+Lead.transform.position);
        }

        /// <summary>The gallery's drones are a fight a person can finish on foot from wherever it leaves him: under each rail is floor joined to where the lead stood when the chase
        /// stopped (reconnaissance s48: by the east rail, the west drone alive), so walking to the other is all it asks.</summary>
        [UnityTest] public IEnumerator EachGalleryDroneRailHasFloorUnderItJoinedToTheOther()
        {
            yield return Zone("BridleFoundry");
            var rails=All<ForceLine>().Where(l=>l.name.StartsWith("Drone rail")).ToArray();
            Assert.AreEqual(2,rails.Length,"fixture: the gallery has its two drone rails");
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(new Vector3(11.09f,-4.92f,147.88f),out var stood,1.5f,UnityEngine.AI.NavMesh.AllAreas),"fixture: where the lead stood (s48's frames) is floor");
            foreach(var rail in rails)
            {
                var under=Vector3.Lerp(rail.From,rail.To,.4f);
                Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(under,out var below,4,UnityEngine.AI.NavMesh.AllAreas)&&Flat(below.position,under)<1.5f,rail.name+" has no floor under its middle");
                var path=new UnityEngine.AI.NavMeshPath();
                Assert.IsTrue(UnityEngine.AI.NavMesh.CalculatePath(stood.position,below.position,UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete,
                    "no floor joins where the lead stood by the east rail to under "+rail.name);
            }
        }

        /// <summary>Vessa Korl in the proving yard, struck once so her fight is underway, the lead 12 m off her (out of reach, so a round's own presses don't land), and her fall-back
        /// talk opened as the room's AfterTalk opens it in the middle of the fight.</summary>
        IEnumerator TalkUp(EnemyBrain[] vessa)
        {
            yield return ProvingYard();
            Put(Lead,InYard(0,.05f,-12));
            vessa[0]=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("VessaKorl"),InYard(0,.1f,0));vessa[0].transform.rotation=Quaternion.Euler(0,180,0);
            yield return Frames(5);
            vessa[0].Health.Receive(new DamagePacket{amount=1,type=DamageType.Kinetic,source=Lead.Health});yield return Frames(3);
            Assert.IsTrue(BossController.AnyFightUnderway,"fixture: Vessa's fight is underway");
            Assert.IsTrue(Npc.Say("VessaFallsBack","Vessa"),"fixture: nobody hears the talk");
            yield return Frames(30);
            Assert.AreEqual("dialogue",Call("ObserveUi"),"fixture: her talk is up");
        }
        [UnityTest] public IEnumerator ATalkThatOpensInTheMiddleOfAFightIsReadThrough()
        {
            var vessa=new EnemyBrain[1];
            // As the driver was: a round of the fight goes on under the talk, pressing nothing, and the talk stands open.
            yield return TalkUp(vessa);ChaseOff(true);
            yield return Play(new QualityRoute{name="the talk",scene="Arena_Ground",steps=new[]{Round("fight Vessa",14)}},20);
            ChaseOff(false);
            Assert.AreEqual("dialogue",Call("ObserveUi"),"fixture: the talk closed without being read: the check below proves nothing");
            log.Clear();
            yield return TalkUp(vessa);
            yield return Play(new QualityRoute{name="the talk",scene="Arena_Ground",steps=new[]{Round("fight Vessa",40)}},50,null,()=>(string)Call("ObserveUi")!="dialogue");
            Assert.AreNotEqual("dialogue",Call("ObserveUi"),"the driver never read Vessa's talk through in the fight");
        }

        // ================================================================== the Foundry's heart: the wheel, the plate
        const float X=230;
        static Vector3 At(float x,float y,float z)=>new(X+x,y,z);
        static CoolantSluice Sluice(int index,Vector3 seat,Vector3 centre)
        {
            var go=new GameObject("Coolant sluice "+index);go.SetActive(false);var toward=centre-seat;toward.y=0;go.transform.SetPositionAndRotation(seat,Quaternion.LookRotation(toward.normalized));
            var sluice=go.AddComponent<CoolantSluice>();sluice.index=index;sluice.settledBy="SlagColossus";
            var wheel=new GameObject("Wheel");wheel.transform.SetParent(go.transform,false);wheel.transform.localPosition=new Vector3(0,1.2f,-1.4f);sluice.wheel=wheel.transform;
            var spout=new GameObject("Spout");spout.transform.SetParent(go.transform,false);spout.transform.localPosition=new Vector3(0,2.6f,-3.5f);sluice.spout=spout.transform;
            go.SetActive(true);return sluice;
        }
        /// <summary>The heart as its contract has it, on a bare floor in the proving yard: three seats 15 m out on bearings 200, 80 and 320, the Colossus at the middle.</summary>
        static CoolantSluice[] Heart(Vector3 centre)
        {
            Vector3 On(float bearing,float r)=>centre+Quaternion.Euler(0,bearing,0)*Vector3.forward*r;
            return new[]{Sluice(1,On(200,15),centre),Sluice(2,On(80,15),centre),Sluice(3,On(320,15),centre)};
        }
        /// <summary>The fight as a route writes it: chase, strike, dodge what's marked.</summary>
        static QualityStep Fight(float seconds)=>new(){name="fight the Colossus",seconds=seconds,buttons=new[]{"South"},pulseSeconds=.46f,navigate=true,approachTarget=true,tolerance=2.3f,
            rangedTolerance=12,evadeTelegraphs=true,stopWhen="flag:bossdown.SlagColossus"};
        [UnityTest] public IEnumerator InTheHeartTheDriverTurnsAWheelFlicksTheLockToTheDarkPlateAndBreaksIt()
        {
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+25;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Heart fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.6f);
            var partner=PartyController.Current.members.First(m=>m!=Lead);partner.GetComponent<PartnerBrain>().enabled=false;Put(partner,At(-50,.05f,-50));
            foreach(var hero in PartyController.Current.members){hero.Health.InvulnerableUntil=GameTime.Now+900;hero.charge=100;}
            var heart=Heart(At(0,0,0));
            // Still: this is about reading the room, not surviving it (the replay is the fight).
            var colossus=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("SlagColossus"),At(0,.1f,0));colossus.transform.rotation=Quaternion.Euler(0,180,0);colossus.Passive=true;
            yield return Frames(3);var boss=colossus.GetComponent<BossController>();
            Assert.AreEqual(3,boss.PlatesWorn,"fixture: in a room with sluices it wears its three plates");Assert.IsTrue(heart.All(s=>s.Available),"fixture: its sluices are offered");
            Put(Lead,At(0,.05f,-9));yield return Frames(2);
            // Built as the driver was without the two hands: it chases the glowing body and strikes at nothing.
            HandsOff(true);
            yield return Play(new QualityRoute{name="the heart",scene="Arena_Ground",steps=new[]{Fight(14)}},20);
            Assert.IsFalse(log.Any(l=>l.StartsWith("SLUICE_OPEN")),"fixture: without the hands a sluice was opened all the same: the check below proves nothing");
            Assert.AreEqual(0,boss.PlatesShed,"fixture: without the hands a plate was shed");
            HandsOff(false);log.Clear();
            yield return Play(new QualityRoute{name="the heart",scene="Arena_Ground",steps=new[]{Fight(100)}},110,null,()=>boss.PlatesShed>0);
            Assert.IsTrue(log.Any(l=>l.StartsWith("SLUICE_TURNING")&&l.Contains("by="+Lead.character)),"the driver never put the lead's hands on a wheel: "+string.Join(" | ",log));
            Assert.IsTrue(log.Any(l=>l.StartsWith("SLUICE_OPEN")),"the driver put its hands on a wheel and never turned it open: "+string.Join(" | ",log));
            Assert.IsTrue(log.Any(l=>l.StartsWith("PLATE_DARK")),"a wheel turned open darkened no plate");
            Assert.Greater(boss.PlatesShed,0,"the driver turned the wheels and never broke a dark plate: "+string.Join(" | ",log));
        }
    }
}
