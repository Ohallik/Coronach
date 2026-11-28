using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 8's way in and its border as a player meets them, in the built scenes: Brekk's word on Many Hands' dock and Idra's plan at the chart table that start the chapter, the
    /// warp off the hull, and the Picket of Teeth (staged detection: the buoys' beams and the rocks' shadows, the alarm's three stages and what each brings, the gauge that says so, the quiet
    /// route shadow to shadow, the route lock taken apart in Idra's order). And the chapter's definitions and beats, which this engineer owns. The Flows, the Foundry and the town are their
    /// own engineers' and tested in their classes; what every zone gets from the shared checks is theirs and not repeated here. Places are the map brief's (docs/maps/PicketOfTeeth.md),
    /// which are the builder's own (HarrowgateBuilder.Picket).</summary>
    public sealed class HarrowgateWorldTests
    {
        readonly List<string> heard=new(),log=new();
        void Talked(string node,string speaker,System.Action<bool> done)=>heard.Add(node);
        void Looked(DiscoveryPoint point){if(!string.IsNullOrEmpty(point.dialogueNode))heard.Add(point.dialogueNode);}
        static readonly string[] Heeded={"PICKET_","LOCK_","BUOY_DOWN","DISCOVERY","AFTER_TALK"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter8");heard.Clear();log.Clear();
            Npc.TalkRequested+=Talked;DiscoveryPoint.DialogueRequested+=Looked;Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Talked;DiscoveryPoint.DialogueRequested-=Looked;Application.logMessageReceived-=Logged;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Harrowgate(ContentRegistry.Book book)
        {
            // The ways that wait on a story flag in the Picket and the hull, and what a refused press must name.
            book.Lock("vask.lockAdmitted","Idra","Picket");   // the hull's warp: "No lane to the Muster's border yet: Idra lays out the Picket at the chart table first"
            book.Lock("vask.lockRemoved","route lock");       // the Picket's way down to Scoria: "The lane to Scoria runs through the route lock: take it apart first"
            book.Lock("vask.parley","parley flag");           // the Picket's way down to Harrowgate's quay: "... walk in under a parley flag first, ..."
            book.Lock("bossdown.DraskSkiff","skiff");         // the War Harbour's way back to the field: "Stop the Warmaster's skiff first" (the boss's own name is in capitals, the prompt isn't)
            // The script's nodes have no choices, so none is given a branch: the four that end at a counter (BrekkCampStall, BrekkCampRepeat, OdrunKitchenReturn, OdrunShop) are read
            // as ending at the shop from their own lines (DialogueInputTests.EndingOf). Branched, the test waited for a choice that never came (the manifest's section 8.2 asked for it).
        }

        // ------------------------------------------------------------------ the Picket's places (HarrowgateBuilder.Picket; docs/maps/PicketOfTeeth.md)
        static readonly (string name,Vector2 at,float from,float to,float half,float reach,float period,float phase)[] Buoys=
        {
            ("B1",new(50,200),222,318,11,108,10,0),("B2",new(-50,360),50,130,11,108,10,.35f),("B3",new(50,470),230,310,11,108,10,.85f),("B4",new(0,570),140,220,13,62,8,.15f),
        };
        static readonly (Vector2 at,bool pocket)[] Quiet=
        {
            (new(0,110),false),(new(2,160),true),(new(-26,190),true),(new(-21.7f,200),false),(new(-6,228),true),(new(0,300),false),(new(-22,322),true),(new(-6,346),true),
            (new(12.1f,365.4f),false),(new(13.5f,368.1f),false),(new(14,372),true),(new(-4,398),true),(new(-20,424),true),(new(-21,429.4f),false),(new(-19.2f,433.9f),false),
            (new(0,448),true),(new(-.3f,452.6f),false),(new(1.3f,456.4f),false),(new(20,474),true),(new(2.5f,490.8f),false),(new(1,495.1f),false),(new(2,500),true),
            (new(-22,526),true),(new(-24.7f,528.7f),false),(new(-26,531.3f),false),(new(-36,574),true),(new(0,606),false),
        };
        /// <summary>The rocks the quiet route was designed on (HarrowgateBuilder.Picket.Shades); the lane has more, each only another shadow.</summary>
        static readonly List<(Vector2 centre,float radius)> Designed=new()
        {
            (new(10,172),6),(new(-14,196),5.5f),(new(8,226),6),(new(-14.1f,348.6f),5),(new(5.6f,370.4f),5),(new(-10.6f,392.6f),5),(new(-12.9f,428.7f),5),(new(7.8f,451.4f),5),
            (new(28.4f,472.9f),5),(new(9.2f,495.5f),5),(new(-18.2f,533.6f),5),
        };
        static readonly Vector3 LockSpawn=new(0,1,594),WingLaunch=new(44,1,520);
        static readonly (string flag,Vector3 at)[] Pieces={("vask.lock1",new(-5.5f,1,633)),("vask.lock2",new(5.5f,1,633)),("vask.lock3",new(0,1,631)),("vask.lock4",new(0,1,634))};
        const float WallZ=640,EdgeZ=78,LockSeenZ=604,Idra=2.17f;

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static int Step(string quest)=>State.questSteps.TryGetValue(quest,out var step)?step:-1;
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static float Flat(Vector3 a,Vector2 b)=>Vector2.Distance(new Vector2(a.x,a.z),b);
        static Vector3 On(Vector2 p)=>new(p.x,1,p.y);
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>The story at the Picket's door: the plan heard and the warp the next step.</summary>
        static void ToThePicket(){Flag("vask.brekkWord");Flag("vask.lockAdmitted");State.questSteps["Harrowgate"]=1;}
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            yield return Ready(zone);
        }
        static IEnumerator Ready(string zone)
        {
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            float deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Prepared,"fixture: "+zone+"'s conversations never finished preparing");
            yield return new WaitForSecondsRealtime(.7f);
        }
        static IEnumerator Arrive(string zone,float most=25)
        {
            float deadline=Time.unscaledTime+most;
            while((State.zone!=zone||SceneFlow.Current.Loading)&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual(zone,SceneFlow.Current.Zone,"the party never arrived in "+zone);Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            yield return Ready(zone);
        }
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship)ship.Halt();else if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static IEnumerator Settle()
        {
            float deadline=Time.unscaledTime+40;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running,"a conversation never ended");
            yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Discover(DiscoveryPoint point){DialogueSystem.Current.AutoAdvance=true;Assert.IsTrue(point.Available,$"\"{point.prompt}\" isn't offered");point.Interact();yield return null;yield return Settle();}
        static DiscoveryPoint Point(string flag)=>All<DiscoveryPoint>().Single(p=>p.flag==flag);
        static EncounterVolume Fight(string id)=>All<EncounterVolume>().SingleOrDefault(e=>e.encounterId==id);
        static Vector3? Goal()=>Object.FindFirstObjectByType<ObjectiveHud>().Goal;
        /// <summary>Flies the lead ship north across a line across the lane, from nine metres short of it, until a conversation starts or it is nine metres past.</summary>
        static IEnumerator FlyAcross(DiscoveryPoint line,float x=0)
        {
            DialogueSystem.Current.AutoAdvance=true;var at=new Vector3(x,1,line.transform.position.z);
            Put(Lead,at+Vector3.back*9);yield return null;
            float deadline=Time.unscaledTime+5;
            while(!DialogueSystem.Current.Running&&Lead.transform.position.z<at.z+9&&Time.unscaledTime<deadline){Lead.motor.Move(Vector2.up,false,false);yield return null;}
            if(Lead.motor is FlightMotor ship)ship.Halt();
            yield return Settle();
        }
        /// <summary>The scene's sensor shadows, as the beams are cut by them.</summary>
        static List<(Vector2 centre,float radius)> Shades()=>SensorShade.All.Where(s=>s!=null&&s.isActiveAndEnabled).Select(s=>(s.Centre,s.radius)).ToList();
        static SensorSweep Sweep(Vector2 at)=>SensorSweep.All.OrderBy(s=>Vector2.Distance(s.Origin,at)).First();
        /// <summary>Whether any beam sees this point at this moment of the zone's one clock.</summary>
        static bool Lit(IEnumerable<SensorSweep> sweeps,Vector2 p,float t,List<(Vector2,float)> shades)=>sweeps.Any(s=>SensorSweep.Sees(s.Origin,SensorSweep.YawAt(s.from,s.to,s.period,s.phase,t),s.half,s.reach,p,shades));
        /// <summary>The longest stretch of start times, over the beams' whole common cycle, from which a ship flying this line at this speed is never lit.</summary>
        static float Window(IList<SensorSweep> sweeps,IList<Vector2> line,float speed,List<(Vector2,float)> shades,float cycle=40,float step=.05f)
        {
            float length=0;for(int i=1;i<line.Count;i++)length+=Vector2.Distance(line[i-1],line[i]);float duration=length/speed;
            Vector2 At(float f)
            {
                float d=f*length;
                for(int i=1;i<line.Count;i++){float leg=Vector2.Distance(line[i-1],line[i]);if(d<=leg||i==line.Count-1)return Vector2.Lerp(line[i-1],line[i],leg<1e-4f?1:d/leg);d-=leg;}
                return line[line.Count-1];
            }
            int starts=Mathf.RoundToInt(cycle/step),samples=Mathf.Max(2,Mathf.CeilToInt(duration/.05f));var good=new bool[starts];
            for(int k=0;k<starts;k++)
            {
                bool clear=true;float t0=k*step;
                for(int i=0;i<=samples&&clear;i++){float f=i/(float)samples;if(Lit(sweeps,At(f),t0+duration*f,shades))clear=false;}
                good[k]=clear;
            }
            int best=0,run=0;for(int k=0;k<starts*2;k++){run=good[k%starts]?run+1:0;best=Mathf.Max(best,run);}
            return Mathf.Min(best,starts)*step;
        }
        static IEnumerable<IList<Vector2>> QuietLegs()
        {
            var leg=new List<Vector2>{Quiet[0].at};
            for(int i=1;i<Quiet.Length;i++){leg.Add(Quiet[i].at);if(Quiet[i].pocket||i==Quiet.Length-1){yield return leg;leg=new List<Vector2>{Quiet[i].at};}}
        }
        /// <summary>Holds the lead ship in a buoy's beam: put each frame where the beam points, at a distance it reaches and no rock shades.</summary>
        static IEnumerator HoldInBeam(SensorSweep sweep,System.Func<bool> until,float most)
        {
            float deadline=Time.time+most;
            while(!until()&&Time.time<deadline)
            {
                var d=SensorSweep.Direction(sweep.Yaw);Vector3? spot=null;
                foreach(float r in new[]{22f,16f,30f,12f,40f}){var p=sweep.transform.position+new Vector3(d.x,0,d.y)*r;p.y=1;if(sweep.Sees(p)){spot=p;break;}}
                if(spot.HasValue)Put(Lead,spot.Value);
                yield return null;
            }
        }
        static string GaugeTitle()=>All<TMP_Text>().Where(t=>t.name=="PicketTitle").Select(t=>t.text).FirstOrDefault()??"";
        static string GaugeBrings()=>All<TMP_Text>().Where(t=>t.name=="PicketBrings").Select(t=>t.text).FirstOrDefault()??"";
        static void KillAwake()
        {
            foreach(var brain in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
            {
                if(brain==null||!brain.enabled||brain.GetComponent<DormantEnemy>()!=null)continue;var health=brain.GetComponent<Health>();
                if(health!=null&&health.Alive)health.Receive(new DamagePacket{amount=1e7f,type=DamageType.Kinetic,breakPower=0});
            }
        }

        // ================================================================== Many Hands: the way in
        [UnityTest] public IEnumerator BrekksWordOnTheDockAndIdrasPlanStartTheChapterAndOpenTheWarpToThePicket()
        {
            Assert.IsTrue(Has("chapter7.complete"),"fixture: chapter 8's loadout hasn't finished chapter 7");Assert.Less(Step("Harrowgate"),0,"fixture: the chapter is begun already");
            yield return Zone("ManyHands");
            var brekk=All<Transform>().FirstOrDefault(t=>t.name=="Brekk with his tool roll");Assert.IsNotNull(brekk,"Brekk isn't on the dock");
            Assert.IsTrue(brekk.gameObject.activeInHierarchy,"Brekk isn't on the dock once chapter 7 is done");
            var word=Point("vask.brekkWord");Assert.AreEqual("BrekkScoriaWord",word.dialogueNode);Assert.IsTrue(word.startsOnCrossing,"Brekk's word can be walked round");
            Assert.IsFalse(Point("vask.lockAdmitted").Available,"Idra lays out the Picket before Brekk has asked");
            yield return Discover(word);
            Assert.AreEqual("BrekkScoriaWord",heard.LastOrDefault());Assert.AreEqual(0,Step("Harrowgate"),"Brekk's word didn't start the chapter");
            // The story's arrow goes to the chart table.
            yield return Frames(3);var goal=Goal();Assert.IsTrue(goal.HasValue,"the objective points at nothing on the plan's step");
            var plan=Point("vask.lockAdmitted");Assert.Less(Flat(goal.Value,plan.transform.position),3,$"the objective points at {goal}, not at the chart table");
            yield return Discover(plan);
            Assert.AreEqual("IdraPicketPlan",heard.LastOrDefault());Assert.IsTrue(Has("vask.lockAdmitted"),"Idra's plan didn't say the lock was Taren's");Assert.AreEqual(1,Step("Harrowgate"),"the plan didn't move the story to the Picket");
            yield return Zone("ManyHandsHull");
            var warp=All<WarpBeacon>().Single(w=>w.scene=="PicketOfTeeth");Assert.IsTrue(Has(warp.requiredFlag),"the warp to the Picket is still shut after the plan");
            StringAssert.Contains("Idra",warp.lockedPrompt,"the shut warp doesn't say who opens it");
            Put(Lead,warp.transform.position+Vector3.back*2);yield return Frames(3);warp.Interact();
            yield return Arrive("PicketOfTeeth");
            Assert.AreEqual(2,Step("Harrowgate"),"arriving at the Picket didn't move the story on to crossing it");
            Assert.Less(Flat(Lead.transform.position,new Vector2(0,6)),4,"the warp didn't arrive at the Picket's arrival");
        }

        // ================================================================== the Picket of Teeth
        [UnityTest] public IEnumerator ThePicketHasItsFourBuoysAndItsWaysAndEachShutWaySaysWhatOpensIt()
        {
            ToThePicket();yield return Zone("PicketOfTeeth");
            var zone=ZoneController.Current.definition;Assert.AreEqual(ZoneKind.SpaceCombat,zone.kind);Assert.AreEqual(8,zone.chapter);Assert.AreEqual(1,zone.enemyThreat,"the Picket's threat isn't 1");
            Assert.AreEqual("Picket of Teeth",zone.musicId,"the Picket plays Nathan's own track for it (round eleven; Galactic Conquest before)");Assert.AreEqual("Starfight",zone.fightMusicId);
            Assert.IsNotNull(PicketAlarm.Current,"the Picket has no alarm");
            Assert.AreEqual(Buoys.Length,SensorSweep.All.Count,"the Picket hasn't four buoys");
            foreach(var b in Buoys)
            {
                var s=Sweep(b.at);Assert.Less(Vector2.Distance(s.Origin,b.at),1,$"no buoy where the map has {b.name}");
                Assert.AreEqual(b.from,s.from,.01f,b.name);Assert.AreEqual(b.to,s.to,.01f,b.name);Assert.AreEqual(b.half,s.half,.01f,b.name);Assert.AreEqual(b.reach,s.reach,.01f,b.name);
                Assert.AreEqual(b.period,s.period,.01f,b.name);Assert.AreEqual(b.phase,s.phase,.01f,b.name);
                Assert.IsNotNull(s.beam,$"{b.name} draws no beam");Assert.IsNotNull(s.arc,$"{b.name} doesn't draw what it can sweep");Assert.IsFalse(s.Dark,$"{b.name} is dark from the start");
            }
            var spawns=All<SpawnPoint>().Select(s=>s.id).ToArray();foreach(string id in new[]{"Arrival","Scoria","Harrowgate","Lock"})CollectionAssert.Contains(spawns,id,"the Picket has no spawn "+id);
            var ways=All<WarpBeacon>();
            Assert.IsTrue(ways.Any(w=>w.scene=="ManyHandsHull"&&string.IsNullOrEmpty(w.requiredFlag)),"the Picket has no open way home");
            var down=ways.Single(w=>w.scene=="ScoriaFlows");Assert.AreEqual("vask.lockRemoved",down.requiredFlag);StringAssert.Contains("route lock",down.lockedPrompt);
            var town=ways.Single(w=>w.scene=="Harrowgate");Assert.AreEqual("vask.parley",town.requiredFlag);StringAssert.Contains("parley flag",town.lockedPrompt);
            Put(Lead,down.transform.position+Vector3.back*2);yield return Frames(3);down.Interact();yield return Frames(3);Assert.IsFalse(SceneFlow.Current.Loading,"the way down to Scoria opened with the lock still across the lane");
        }

        [UnityTest] public IEnumerator TheQuietRouteRunsShadowToShadowWithAWindowOnEveryLeg()
        {
            ToThePicket();yield return Zone("PicketOfTeeth");
            var sweeps=SensorSweep.All.ToList();var shades=Shades();
            Assert.GreaterOrEqual(shades.Count,11,"fixture: the lane's rocks aren't sensor shadows");
            // Every pocket is hidden from every buoy at every turn of its sweep.
            foreach(var (at,pocket) in Quiet.Where(q=>q.pocket))
                foreach(var s in sweeps)Assert.IsFalse(SensorSweep.EverSees(s.Origin,s.from,s.to,s.half,s.reach,at,shades),$"the pocket at {at} can be seen by the buoy at {s.Origin}");
            // And the check can fail: among the rocks the route was designed on, with the one that hides the first pocket taken away, it can be seen.
            var bare=Designed.Where(c=>Vector2.Distance(c.centre,new Vector2(10,172))>1).ToList();
            Assert.IsTrue(sweeps.Any(s=>SensorSweep.EverSees(s.Origin,s.from,s.to,s.half,s.reach,new Vector2(2,160),bare)),"fixture: the first pocket is hidden even without its rock: the check proves nothing");
            // Every leg between pockets can be flown at 10 m/s without being lit, if it is set off on at the right moment of the beams' cycle.
            var report=new List<string>();
            foreach(var leg in QuietLegs())
            {
                float window=Window(sweeps,leg,10,shades);report.Add($"{leg[0]}->{leg[leg.Count-1]} {window:0.0}s");
                Assert.GreaterOrEqual(window,1.5f,$"the quiet leg {string.Join(" ",leg)} has a window of {window:0.00} s in each cycle of the beams");
            }
            TestContext.WriteLine("QUIET_WINDOWS "+string.Join(", ",report));
            // A straight run up the lane's middle is lit for certain: the rule is there to be read.
            Assert.AreEqual(0,Window(sweeps,new[]{new Vector2(0,110),new Vector2(0,600)},12,shades),.001f,"a ship can fly straight up the middle of the Picket at 12 m/s and never be lit");
            // Every leg takes Idra's hull past every solid thing in the lane.
            foreach(var leg in QuietLegs())for(int i=1;i<leg.Count;i++)
            {
                var a=On(leg[i-1])+Vector3.up*.8f;var b=On(leg[i])+Vector3.up*.8f;
                foreach(var hit in Physics.SphereCastAll(a,Idra,(b-a).normalized,Vector3.Distance(a,b),~0,QueryTriggerInteraction.Ignore))
                    if(hit.collider.GetComponentInParent<CombatActor>()==null&&hit.collider.GetComponentInParent<Health>()==null)Assert.Fail($"the quiet leg {leg[i-1]}->{leg[i]} meets {hit.collider.name} with Idra's hull");
            }
        }

        [UnityTest] public IEnumerator AShipInARocksShadowIsNeverSeenAndTheGaugeSaysItIsHidden()
        {
            ToThePicket();yield return Zone("PicketOfTeeth");
            var alarm=PicketAlarm.Current;
            foreach(var pocket in new[]{new Vector2(2,160),new Vector2(-6,346),new Vector2(20,474)})
            {
                Put(Lead,On(pocket));float until=Time.time+10.5f;bool seen=false;
                while(Time.time<until){Put(Lead,On(pocket));seen|=alarm.Seen;yield return null;}
                Assert.IsFalse(seen,$"a ship holding in the shadow at {pocket} was seen over a whole sweep");Assert.AreEqual(0,alarm.Stage);Assert.AreEqual(0,alarm.Heard,.001f);
                StringAssert.Contains("HIDDEN",GaugeTitle(),$"the gauge doesn't say a ship in the shadow at {pocket} is hidden");
            }
            // In the open, a beam finds it, and the gauge says so.
            yield return HoldInBeam(Sweep(Buoys[0].at),()=>alarm.Seen,4);
            Assert.IsTrue(alarm.Seen,"a ship held in B1's beam isn't seen");yield return Frames(2);StringAssert.Contains("SEEN",GaugeTitle(),"the gauge doesn't say the ship is seen");
        }

        [UnityTest] public IEnumerator BeingSeenRaisesThePicketAStageAtATimeAndEachStageBringsWhatTheGaugeSays()
        {
            ToThePicket();yield return Zone("PicketOfTeeth");
            var alarm=PicketAlarm.Current;var b1=Sweep(Buoys[0].at);
            Assert.AreEqual(0,alarm.Stage);StringAssert.Contains("QUIET",GaugeTitle(),"the gauge doesn't show the picket quiet");
            // Amber: every lamp pulses amber and the beams sweep faster.
            yield return HoldInBeam(b1,()=>alarm.Stage>=1,5);
            Assert.AreEqual(1,alarm.Stage,"a ship held in a beam didn't bring the picket to amber");Assert.IsTrue(log.Any(l=>l.StartsWith("PICKET_STAGE 1")),"no stage logged");
            yield return Frames(2);
            foreach(var s in SensorSweep.All){Assert.AreEqual(1,s.Stage,"a buoy didn't go amber with the rest");Assert.Greater(s.Pace,1,"a buoy's beam doesn't sweep faster at amber");Assert.Greater(s.lamp.color.r,s.lamp.color.b*1.5f,"a buoy's lamp isn't amber at amber");}
            StringAssert.Contains("AMBER",GaugeTitle());Assert.AreEqual(PicketGauge.Brings(1),GaugeBrings());
            // The hunter wing: launched off the carrier's deck.
            yield return HoldInBeam(b1,()=>alarm.Stage>=2,5);
            Assert.AreEqual(2,alarm.Stage,"held in a beam at amber, the picket didn't send its hunters");
            Assert.IsNotNull(alarm.Wing,"stage two launched no wing");Assert.IsTrue(alarm.Wing.Started,"stage two's wing never woke");Assert.AreEqual("Hunters_1",alarm.Wing.encounterId);
            yield return Frames(3);
            var hunters=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.enabled&&Flat(e.transform.position,WingLaunch)<20).ToArray();Assert.GreaterOrEqual(hunters.Length,2,"the hunter wing isn't two ships off the carrier's deck");
            StringAssert.Contains("HUNTER WING",GaugeTitle());Assert.AreEqual(PicketGauge.Brings(2),GaugeBrings());
            // The carrier: it swings broadside, another wing comes, and its guns lay marked bursts round the lead in the field.
            var carrier=alarm.carrier;Assert.IsNotNull(carrier,"the Picket has no carrier");var rest=carrier.rotation;
            yield return HoldInBeam(b1,()=>alarm.Stage>=3,5);
            Assert.AreEqual(3,alarm.Stage,"held in a beam with the wing out, the carrier didn't turn");Assert.IsTrue(log.Any(l=>l.StartsWith("PICKET_CARRIER")));
            Assert.AreEqual("Hunters_2",alarm.Wing.encounterId,"the carrier didn't send a second wing");
            yield return Frames(2);StringAssert.Contains("CARRIER",GaugeTitle());Assert.AreEqual(PicketGauge.Brings(3),GaugeBrings());
            Put(Lead,new Vector3(-20,1,424));yield return new WaitForSeconds(3);
            Assert.Greater(Quaternion.Angle(rest,carrier.rotation),5,"the carrier hasn't swung at all three seconds after the third stage");
            // A burst marked, and landing on a ship inside its marking (the wings put down first, so nothing else lands on it meanwhile).
            KillAwake();yield return Frames(2);Lead.Health.InvulnerableUntil=0;float before=Lead.Health.integrity;
            // A new burst, not one laid while the ship was being put here.
            int bursts=log.Count(l=>l.StartsWith("PICKET_BURST"));
            float deadline=Time.time+6;while(log.Count(l=>l.StartsWith("PICKET_BURST"))<=bursts&&Time.time<deadline){if(Lead.motor is FlightMotor ship)ship.Halt();yield return null;}
            Assert.Greater(log.Count(l=>l.StartsWith("PICKET_BURST")),bursts,"the carrier's guns laid nothing round a ship in the field");
            Assert.IsTrue(All<Telegraph>().Any(t=>t!=null&&t.isActiveAndEnabled),"the burst wasn't marked before it landed");
            deadline=Time.time+3;while(Lead.Health.integrity>=before&&Time.time<deadline){if(Lead.motor is FlightMotor ship)ship.Halt();yield return null;}
            Assert.Less(Lead.Health.integrity,before,"a burst marked round a ship that held still never landed on it");
            Assert.Greater(Lead.Health.integrity,0,"a burst took the last of a ship's bar");
            Assert.LessOrEqual(before-Lead.Health.integrity,Lead.Health.maximum*.19f,"a burst took more than its share of the bar");
            // What the gauge says fits its box at every stage.
            var brings=All<TMP_Text>().First(t=>t.name=="PicketBrings");
            for(int stage=0;stage<=3;stage++){brings.text=PicketGauge.Brings(stage);brings.ForceMeshUpdate();Assert.LessOrEqual(brings.preferredWidth,brings.rectTransform.rect.width+1,$"stage {stage}'s words don't fit the gauge");}
        }

        /// <summary>The alarm's stages as the game's camera draws them, the HUD with them, saved for the eye under Builds/quality/focused/picket-stages (guide 0.1: a flight zone's rule is
        /// read from the screen, not from text): beside B3, where a buoy, its beam and the carrier's deck are in frame, and in the field's middle, where no buoy is. Nobody had seen a
        /// stage before November 14: LookDev draws the zone at rest.</summary>
        [UnityTest] public IEnumerator EachStageOfTheAlarmIsPhotographedAsTheGameDrawsIt()
        {
            ToThePicket();
            foreach(var (spot,at) in new[]{("b3",new Vector3(40,1,470)),("field",new Vector3(-4,1,300))})
            {
                // The third stage holds until the party leaves, so each spot starts from a zone freshly entered.
                yield return Zone("PicketOfTeeth");var alarm=PicketAlarm.Current;Assert.AreEqual(0,alarm.Stage);
                var partner=PartyController.Current.members.First(m=>m!=Lead);
                // B1's beam, which never reaches either spot (so whether it holds the ship never changes it): its brightest point, frame by frame.
                var b1=Sweep(Buoys[0].at);var peaks=new List<float>();
                IEnumerator Hold(float seconds){float until=Time.time+seconds;while(Time.time<until){Put(Lead,at);Put(partner,at+new Vector3(2.4f,0,-1.5f));yield return null;peaks.Add(b1.beam.sharedMesh.colors.Max(c=>c.a));}}
                yield return Hold(1.5f);Still(spot+"-0-quiet");
                // The measure on a quiet picket first: a beam that doesn't throb reads as steady.
                Assert.Less(peaks.Max()-peaks.Min(),.02f,"fixture: a quiet beam's brightness wanders");peaks.Clear();
                alarm.Raise(1,"still");yield return Hold(1.1f);Still(spot+"-1-amber");yield return Hold(.35f);Still(spot+"-1-amber-b");
                // Amber shows in the beam over the ship, not only at a lamp fifty metres off the lane's middle: it throbs on the lamp's heartbeat.
                Assert.Greater(peaks.Max()-peaks.Min(),.1f,"an amber beam burns as steady as a quiet one: amber shows only at lamps out of frame");
                alarm.Raise(2,"still");yield return Hold(3.5f);Still(spot+"-2-hunters");
                alarm.Raise(3,"still");yield return Hold(2.4f);Still(spot+"-3-carrier");yield return Hold(2.6f);Still(spot+"-3-carrier-b");
                Assert.AreEqual(3,alarm.Stage,"fixture: the picket isn't at its third stage");
            }
        }
        /// <summary>What the game's camera draws this frame with the HUD over it (the HUD's canvases drawn by that camera for the moment), as a 1920 by 1080 still.</summary>
        static void Still(string name)
        {
            const int W=1920,H=1080;var camera=Camera.main;Assert.IsNotNull(camera,"fixture: no game camera");
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var image=new Texture2D(W,H,TextureFormat.RGB24,false);var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;
                foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=camera.nearClipPlane+.1f;}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();
                string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/focused/picket-stages"));System.IO.Directory.CreateDirectory(folder);
                string file=System.IO.Path.Combine(folder,name+".png");System.IO.File.WriteAllBytes(file,image.EncodeToPNG());TestContext.WriteLine("PICKET_STILL "+file);
            }
            finally
            {
                foreach(var c in canvases)if(c!=null){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}
                camera.targetTexture=target;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(image);
            }
        }

        [UnityTest] public IEnumerator AmberCalmsUnseenAndABrokenWingTakesThePicketBackToAmber()
        {
            ToThePicket();yield return Zone("PicketOfTeeth");
            var alarm=PicketAlarm.Current;var pocket=On(new Vector2(-6,346));
            alarm.Raise(1,"test");Assert.AreEqual(1,alarm.Stage);
            float until=Time.time+10;while(alarm.Stage>0&&Time.time<until){Put(Lead,pocket);yield return null;}
            Assert.AreEqual(0,alarm.Stage,"amber never went quiet with the ship unseen in a shadow");Assert.IsTrue(log.Any(l=>l=="PICKET_STAGE 0 quiet"));
            alarm.Raise(2,"test");Assert.AreEqual(2,alarm.Stage);var wing=alarm.Wing;Assert.IsNotNull(wing,"no wing at stage two");yield return Frames(3);
            KillAwake();yield return Frames(3);
            Assert.IsTrue(wing.Cleared,"fixture: the wing isn't broken");
            Assert.AreEqual(1,alarm.Stage,"breaking the hunter wing didn't take the picket back to amber");Assert.IsTrue(log.Any(l=>l=="PICKET_STAGE 1 wing broken"));
            // The next time it rises, the next wing comes: a broken wing stays broken.
            alarm.Raise(2,"test");Assert.AreEqual("Hunters_2",alarm.Wing.encounterId,"the broken wing was launched again");
        }

        [UnityTest] public IEnumerator ABuoyShotDownGoesDarkForGoodAndGetsItsLastCallOff()
        {
            ToThePicket();yield return Zone("PicketOfTeeth");
            var alarm=PicketAlarm.Current;var b2=Sweep(Buoys[1].at);
            if(b2.encounter==null)Assert.Ignore("the Sensor Buoy isn't defined yet (the first combat engineer's): the buoys are drawn and can't be shot");
            Put(Lead,b2.transform.position+new Vector3(10,0,-6));yield return new WaitForSecondsRealtime(.6f);
            Assert.IsTrue(b2.encounter.Started,"a ship 12 m from a buoy didn't wake it");
            KillAwake();yield return Frames(3);
            Assert.IsTrue(b2.Dark,"a buoy shot down still sweeps");Assert.IsFalse(b2.beam.gameObject.activeSelf,"a dead buoy's beam is still drawn");
            Assert.IsTrue(log.Any(l=>l.StartsWith("BUOY_DOWN")),"no buoy down logged");Assert.AreEqual(1,alarm.Stage,"a buoy shot down didn't get its call off");
            Assert.IsFalse(b2.Sees(On(new Vector2(0,360))),"a dead buoy still sees");
        }

        [UnityTest] public IEnumerator CrossingThePicketHearsIdraAtTheEdgeAndAtTheLockAndMovesTheStory()
        {
            ToThePicket();yield return Zone("PicketOfTeeth");
            Assert.AreEqual(2,Step("Harrowgate"));
            var edge=Point("vask.picketRead");Assert.AreEqual("IdraPicketEdge",edge.dialogueNode);Assert.IsTrue(edge.startsOnCrossing);Assert.AreEqual(EdgeZ,edge.transform.position.z,.5f);
            yield return FlyAcross(edge,-30);Assert.IsTrue(heard.Contains("IdraPicketEdge"),"Idra didn't speak at the picket's edge");Assert.IsTrue(Has("vask.picketRead"));
            var seen=Point("vask.picketCrossed");Assert.AreEqual("IdraLockReached",seen.dialogueNode);Assert.AreEqual("PicketCrossed",seen.report);Assert.AreEqual(LockSeenZ,seen.transform.position.z,.5f);
            // The step's arrow points at the line before it's crossed.
            yield return Frames(3);var goal=Goal();Assert.IsTrue(goal.HasValue,"the objective points at nothing on the crossing step");
            yield return FlyAcross(seen,20);
            Assert.IsTrue(heard.Contains("IdraLockReached"),"Idra didn't speak with the lock in sight");Assert.IsTrue(Has("vask.picketCrossed"));
            Assert.AreEqual(3,Step("Harrowgate"),"crossing the picket didn't move the story on to the lock");
            Assert.AreEqual("Lock",State.spawn,"the lock in sight isn't a checkpoint");
            yield return Frames(3);goal=Goal();Assert.IsTrue(goal.HasValue,"the objective points at nothing at the lock");
            Assert.Less(Flat(goal.Value,Pieces[0].at),2,$"the objective points at {goal}, not at the port collar");
        }

        [UnityTest] public IEnumerator TheLockComesOffInIdrasOrderWithTheHuntersOutAndOpensTheLane()
        {
            ToThePicket();foreach(string f in new[]{"vask.picketRead","vask.picketCrossed"})Flag(f);State.questSteps["Harrowgate"]=3;
            yield return Zone("PicketOfTeeth","Lock");
            Assert.Less(Flat(Lead.transform.position,LockSpawn),4,"fixture: the party isn't at the lock's checkpoint");
            var routeLock=Object.FindFirstObjectByType<RouteLock>();Assert.IsNotNull(routeLock,"the Picket has no route lock");Assert.IsFalse(routeLock.Open);
            // Shut, the lane stops a hull at the lock.
            Put(Lead,new Vector3(0,1,628));for(float end=Time.time+2.2f;Time.time<end;){Lead.motor.Move(Vector2.up,false,false);yield return null;}
            Assert.Less(Lead.transform.position.z,WallZ-1,"a hull flew through the lock with every piece still on it");
            var hands=Pieces.Select(p=>Point(p.flag)).ToArray();
            for(int n=0;n<hands.Length;n++)Assert.Less(Flat(hands[n].transform.position,Pieces[n].at),1,$"piece {n+1}'s hand isn't where the map has it");
            Assert.IsTrue(hands.All(h=>h.report=="LockPiece"),"a piece of the lock doesn't answer the step");
            for(int n=0;n<hands.Length;n++)
            {
                yield return Frames(2);
                for(int m=0;m<hands.Length;m++)Assert.AreEqual(m==n,hands[m].Available,m==n?$"piece {n+1} isn't offered in its turn":$"piece {m+1} is offered while piece {n+1} is the one Idra has called");
                var goal=Goal();Assert.IsTrue(goal.HasValue&&Flat(goal.Value,hands[n].transform.position)<2,$"the objective doesn't point at piece {n+1}");
                Put(Lead,hands[n].transform.position+Vector3.back*2);yield return Frames(2);
                yield return Discover(hands[n]);
                Assert.IsTrue(Has(Pieces[n].flag),$"piece {n+1} didn't come off");Assert.IsTrue(log.Contains("LOCK_PIECE "+(n+1)),$"piece {n+1} came off with nothing said");
                if(n==0)
                {
                    Assert.GreaterOrEqual(PicketAlarm.Current.Stage,2,"taking the first collar off didn't trip the lock's alarm");
                    Assert.IsTrue(PicketAlarm.Current.Wing!=null&&PicketAlarm.Current.Wing.Started,"no hunters came for the lock");
                }
                Assert.AreEqual(n==3,routeLock.Open,n==3?"the lock isn't open with the keeper bar out":"the lock opened before the keeper bar was out");
            }
            Assert.IsTrue(log.Contains("LOCK_OPEN"));Assert.IsFalse(routeLock.blockers.activeSelf,"the lock's seal still stands");
            // Open, a hull flies through the gap.
            Put(Lead,new Vector3(0,1,628));for(float end=Time.time+2.5f;Time.time<end;){Lead.motor.Move(Vector2.up,false,false);yield return null;}
            Assert.Greater(Lead.transform.position.z,WallZ+4,"a hull can't fly through the open lock");
            // With nothing awake near the ship, Idra says it's done, and the way down opens.
            KillAwake();Put(Lead,new Vector3(0,1,700));DialogueSystem.Current.AutoAdvance=true;
            float deadline=Time.unscaledTime+15;while(!Has("vask.lockRemoved")&&Time.unscaledTime<deadline){KillAwake();yield return null;}
            yield return Settle();
            Assert.IsTrue(heard.Contains("IdraLockOff"),"Idra never said the lock was off");Assert.IsTrue(Has("vask.lockRemoved"));
            Assert.AreEqual(4,Step("Harrowgate"),"the lock off didn't move the story on to the Flows");
            Assert.IsTrue(Has(All<WarpBeacon>().Single(w=>w.scene=="ScoriaFlows").requiredFlag),"the way down to Scoria stays shut with the lock off");
        }

        // ================================================================== the chapter's definitions and beats
        [UnityTest] public IEnumerator TheChaptersQuestsPayInWhatTheBagHoldsAndSayItOnTheirCards()
        {
            yield return null;
            foreach(string id in new[]{"Harrowgate","QuietHands","RelayGlass","OdrunsKitchen","PipsOath"})
            {
                var quest=GameCatalog.Find<QuestDef>(id);Assert.IsNotNull(quest,id+" isn't defined");Assert.AreEqual(8,quest.chapter,id);
                Assert.AreEqual(quest.steps.Length,quest.stepText.Length,id+"'s steps and words differ");Assert.IsTrue(quest.stepText.All(t=>!string.IsNullOrWhiteSpace(t)),id+" has a blank step");
                foreach(var reward in quest.rewards)
                {
                    Assert.IsNull(GameCatalog.Find<ConsumableDef>(reward.id),$"{id} pays {reward.id}, a capped consumable");
                    Assert.IsTrue(GameCatalog.Find<MaterialDef>(reward.id)!=null||GameCatalog.Find<TechPartDef>(reward.id)!=null,$"{id} pays {reward.id}, which is neither a material nor a part");
                }
            }
            var main=GameCatalog.Find<QuestDef>("Harrowgate");Assert.IsTrue(main.main);Assert.AreEqual("chapter8.complete",main.cardFlag);
            Assert.AreEqual(main.steps.Length,main.stepTargets.Length,"the main quest's steps and targets differ");
            var stop=GameCatalog.Find<ChartStop>("Harrowgate");Assert.IsNotNull(stop,"Harrowgate isn't on the chart");Assert.AreEqual(8,stop.chapter);
            foreach(string speaker in new[]{"Drask","Tovra","Odrun","Pip","Bryn"})
            {
                var person=GameCatalog.Find<CharacterDef>(speaker);Assert.IsNotNull(person,speaker+" has no definition");Assert.IsNotNull(person.portraitNatural,speaker+" has no portrait set");
            }
        }

        [UnityTest] public IEnumerator ChapterEightsBeatsWaitForChapterEightAndHoldTheirOrder()
        {
            yield return null;
            var chapter8=new[]{"AbriSmiths","AbriThreeWords","AbriTwentySeven","PellAumiAnswers","TovraHarrowgateRoad","TovraQuietReturn","TovraCages","TovraQuietOffer","HessaWarmaster","HessaParley",
                "PipAfterDuel","OdrunKitchenReturn","OdrunSleepless","OdrunKitchenOffer"};
            // Chapter 7 as it arrives, on a fresh save (the fixture's own is chapter 8's): nobody says anything of chapter 8's.
            GameServices.Current.NewGame();DevLoadout.Apply("chapter7");State.zone="ManyHands";
            Assert.IsFalse(Has("chapter7.complete"),"fixture: chapter 7's loadout has chapter 7 done");
            foreach(string speaker in new[]{"Keeper","Pell","Tovra","Hessa","Pip","Odrun"}){var node=SideStories.NodeFor(speaker,State);Assert.IsFalse(chapter8.Contains(node),$"{node} wins in chapter 7");}
            // Chapter 7 done: Abri's twenty-seven days; the plan heard, her three words; the picket crossed, neither.
            Flag("chapter7.complete");Assert.AreEqual("AbriTwentySeven",SideStories.NodeFor("Keeper",State));
            Flag("vask.lockAdmitted");Assert.AreEqual("AbriThreeWords",SideStories.NodeFor("Keeper",State));
            Flag("vask.picketCrossed");Assert.IsFalse(chapter8.Contains(SideStories.NodeFor("Keeper",State)),"Abri's three words outlast the Picket");
            // The chapter done: Abri's smiths, and Pell's answer from Aumi once the glass has gone home.
            Flag("chapter8.complete");Assert.AreEqual("AbriSmiths",SideStories.NodeFor("Keeper",State));
            Assert.AreNotEqual("PellAumiAnswers",SideStories.NodeFor("Pell",State),"Aumi answers with the glass still in Harrowgate");
            Flag("quest.RelayGlass.complete");Assert.AreEqual("PellAumiAnswers",SideStories.NodeFor("Pell",State));
            // Tovra at the camp, never aboard: her road once the cage is open, ahead of her side story.
            State.zone="ScoriaFlows";Flag("met.Tovra");Flag("vask.unswornMet");
            Assert.AreEqual("TovraCages",SideStories.NodeFor("Tovra",State));Flag("tovra.cagesHeard");Assert.AreEqual("TovraQuietOffer",SideStories.NodeFor("Tovra",State));
            Flag("vask.juvenileFreed");Assert.AreEqual("TovraHarrowgateRoad",SideStories.NodeFor("Tovra",State));
            State.zone="ManyHands";Assert.IsNull(SideStories.NodeFor("Tovra",State),"Tovra's camp lines play aboard over her own");
        }
    }
}
