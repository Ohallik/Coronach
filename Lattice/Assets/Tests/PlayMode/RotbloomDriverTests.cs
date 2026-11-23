using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The Rotbloom fought by the replay driver on the built Drowned Waystation, through the route's own steps (tools/wendmire_routes.py, chapter-23, from "Taren leads" at the
    /// great hall's door to "the Rotbloom dies"), with the route's own knobs, the party at the boss sheet's Sync 14 in the chapter's arrival parts, nothing called on an actor: the
    /// driver's virtual pad and nothing else. What a person does in this hall: up the nearest plinth's ramp, burn its lure-stalk, strike the mouth that gapes, burn a second, and
    /// fight the grounded bloom to the end, inside the sheet's two to five minutes. Shown failing first with the driver as it was (ContinuousReview.chaseHandsOff): the chase
    /// pushed straight at the nearest stalk into the gap behind its plinth, and with one stalk burned it ground the shut bloom at 30% instead of burning a second.</summary>
    public sealed class RotbloomDriverTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        const string Station="DrownedWaystation",Route="chapter-23-drowned-waystation";
        // The builder's numbers (WendmireBuilder.Waystation), copied: the tests can't see the editor assembly.
        static readonly Vector2 C=new(0,289);
        const float PlinthTop=-.85f,PlinthOut=15,PlinthRadius=2.6f;
        GameObject fixture;ContinuousReview replay;readonly List<string> log=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Heeded={"ROTBLOOM_","STALK_BURNED","BOSS_","QUALITY_BOSS","QUALITY_LOCK_FOR","QUALITY_CHASE","QUALITY_CHECKPOINT_BEGIN","EXTRA_OK","REVIVE_OK"};
        readonly List<float> gapes=new();
        void Logged(string text,string stack,LogType type)
        {
            if(text.StartsWith("ROTBLOOM_GAPES stalk"))gapes.Add(Time.time);
            foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add($"{Time.time:0.0} {text}");return;}
        }
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("chapter4");
            log.Clear();Application.logMessageReceived+=Logged;HandsOff(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;HandsOff(false);PlanGoalsOnTheirLevel(false);Drop();
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
        /// <summary>The driver as it was before its chase hands (ContinuousReview.Chase.cs), for the broken state: put back by the case and by the tear-down.</summary>
        static void HandsOff(bool off)=>typeof(ContinuousReview).GetField("chaseHandsOff",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,off);
        /// <summary>The driver of 14:26 on November 14 (a route's point taken on its own level, like a body's), for the broken state of the mire walk: put back by the case and the tear-down.</summary>
        static void PlanGoalsOnTheirLevel(bool on)=>typeof(ContinuousReview).GetField("planGoalsOnTheirLevel",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,on);
        /// <summary>A driver of its own for each fight: never active (its Start wants a player's launch arguments), its steps and pad worked from here a frame at a time.</summary>
        void Fresh(){Drop();fixture=new GameObject("Rotbloom driver fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();}
        void Drop()
        {
            // Unity does not call OnDestroy on a never-active fixture: its pad is taken away here.
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(fixture!=null)Object.Destroy(fixture);fixture=null;replay=null;
        }
        IEnumerator Play(QualityRoute route,float most,System.Action each,System.Func<bool> enough)
        {
            Set("pad",InputSystem.AddDevice<Gamepad>("RotbloomDriverPad"));
            Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);Get<System.Diagnostics.Stopwatch>("clock").Start();
            var run=(IEnumerator)Call("RunSteps");
            for(float end=Time.unscaledTime+most;Time.unscaledTime<end;)
            {
                if(enough())break;
                if(!run.MoveNext())break;
                each();Call("InputUpdate");yield return null;
            }
            Set("recording",false);Call("InputUpdate");
        }

        // ------------------------------------------------------------------ the hall, as the route stands in it
        static GameState State=>GameServices.Current.State;
        static CombatActor Lead=>PartyController.Current!=null?PartyController.Current.Active:null;
        static Vector2 Plan(Vector3 p)=>new(p.x,p.z);
        static Vector2 Plinth(float bearing)=>C+new Vector2(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad))*PlinthOut;
        static readonly float[] Bearings={45,135,225,315};
        /// <summary>The plinth a point stands nearest, by its bearing.</summary>
        static float NearestPlinth(Vector3 p)=>Bearings.OrderBy(b=>Vector2.Distance(Plan(p),Plinth(b))).First();
        /// <summary>On a plinth's top: at its height and inside its edge.</summary>
        static bool OnATop(Vector3 p)=>p.y>PlinthTop-.15f&&Vector2.Distance(Plan(p),Plinth(NearestPlinth(p)))<PlinthRadius+.1f;
        /// <summary>On the floor behind a plinth: below its top, at its foot, and further from the hall's middle than its centre (the gap between it and the wall).</summary>
        static bool BehindAPlinth(Vector3 p)
        {
            var plinth=Plinth(NearestPlinth(p));
            return p.y<PlinthTop-.3f&&Vector2.Distance(Plan(p),plinth)<PlinthRadius+1.2f&&Vector2.Distance(Plan(p),C)>PlinthOut+.5f;
        }
        /// <summary>In the plinth's ramp: the side facing the hall's middle, where its slope is a hero's (WendmireBuilder.Waystation.ArenaAt: full ramp within about 26 degrees of
        /// the line to C, steep beyond 52).</summary>
        static bool OnARamp(Vector3 p)
        {
            var plinth=Plinth(NearestPlinth(p));var off=Plan(p)-plinth;if(off.magnitude<PlinthRadius-.2f||off.magnitude>PlinthRadius+2.6f)return false;
            return Vector2.Dot(off.normalized,(C-plinth).normalized)>.62f;
        }
        /// <summary>The flags the route has set by the time it stands at the great hall's door (WendmireRouteTests.AsWalked and the door's own crossing).</summary>
        static readonly string[] AtTheDoor={"wend.charted","wend.lineHeld","wend.onaHeard","wend.tideTurned","wend.lureLearned","wend.pump1","wend.pump2","wend.pimFound","wend.pump3","wend.pump4","wend.bloomSeen"};
        static readonly string[] StalkFlags={"wend.stalk.ne","wend.stalk.nw","wend.stalk.sw","wend.stalk.se"};
        static int StalksBurned=>StalkFlags.Count(f=>GameServices.Current.Flags.GetBool(f));
        /// <summary>The route's own steps from "Taren leads" at the door to "the Rotbloom dies", with the route's own knobs (autoHeal, useExtra).</summary>
        static QualityRoute FightSteps()
        {
            string file=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes",Route+".json");
            Assert.IsTrue(System.IO.File.Exists(file),"fixture: "+file+" (python tools/wendmire_routes.py writes it)");
            var route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));
            int door=System.Array.FindIndex(route.steps,s=>s.bossDoor=="Rotbloom");Assert.Greater(door,0,"fixture: the route has a step at the Rotbloom's door");
            int from=door-1;Assert.AreEqual("Taren",route.steps[from].ensureCharacter,"fixture: Taren is made lead before the door");
            int to=System.Array.FindIndex(route.steps,door,s=>s.name=="the Rotbloom dies");Assert.Greater(to,door,"fixture: the route has 'the Rotbloom dies' after its door");
            Assert.IsTrue(route.autoHeal&&route.useExtra,"fixture: the route mends and spends its EXTRA");
            return new QualityRoute{name=route.name,scene=Station,autoHeal=route.autoHeal,useExtra=route.useExtra,steps=route.steps.Skip(from).Take(to-from+1).ToArray()};
        }
        /// <summary>The station as the route stands in it at the great hall's door: every pump it ran, the door's crossing heard, the party at Sync 14 (the boss sheet's) in the
        /// chapter's arrival parts (DevLoadout chapter4, what the shared length test measures), both heroes whole, at the door's spawn, nothing of an earlier fight in the save.</summary>
        static IEnumerator AtTheGreatHallsDoor()
        {
            foreach(string key in AtTheDoor)GameServices.Current.Flags.SetBool(key,true);
            // A fight already won here (the first of two) leaves the bloom down, its encounter cleared and its stalks settled: all of it is put back.
            foreach(string key in StalkFlags.Concat(new[]{"bossdown.Rotbloom","clear.Rotbloom","wend.rotbloomDead"}))State.flags.Remove(key);
            SceneFlow.Current.LoadZone(Station,"Door");float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+Station+" never finished loading");
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.8f);
            foreach(var member in State.party)member.level=14;PartyController.Current.RefreshStats();
            foreach(var hero in PartyController.Current.members)hero.Health.integrity=hero.Health.maximum;
            ItemUse.Reset();yield return null;
            Assert.AreEqual(4,LureStalk.All.Count(s=>s.Standing),"fixture: four lure-stalks stand in the hall");
            Assert.IsFalse(GameServices.Current.Flags.GetBool("bossdown.Rotbloom"),"fixture: the Rotbloom is still to fight");
        }

        /// <summary>What one fight did, read frame by frame and off the log.</summary>
        sealed class Watch
        {
            public float roundOne=-1,firstTop=-1,firstBurn=-1,secondBurn=-1,won=-1,deepestBehind;public Vector3 behindAt;public bool behind,upByRamp;
            public int mouthBlows,leadBlowsOnStalks;public readonly List<string> trail=new();public float nextTrail;
            public Health body;public readonly HashSet<Health> stalksStruck=new();
            // The last two seconds of the lead, frame by frame, and what they were when he was first behind a plinth.
            public readonly Queue<string> recent=new();public string howBehind="";
            // Read once the fight is over: its length off the log, retries, what the route's steps failed, what the log said.
            public float duration=-1;public int retries;public List<string> failed=new(),said=new();public string told="";
        }
        System.Action Watching(Watch w)
        {
            void Hit(Health h,DamagePacket packet,float amount)
            {
                var lead=Lead;if(lead==null||packet.source!=lead.Health||amount<=0)return;
                if(h.Broken&&gapes.Any(t=>Time.time>=t&&Time.time<=t+BossController.RotbloomTune.Gape+.2f))w.mouthBlows++;
            }
            void Stalk(Health h,DamagePacket packet,float amount){var lead=Lead;if(lead!=null&&packet.source==lead.Health&&amount>0){w.leadBlowsOnStalks++;w.stalksStruck.Add(h);}}
            var heard=new HashSet<LureStalk>();
            return ()=>
            {
                var lead=Lead;if(lead==null)return;var at=lead.transform.position;
                string step=Get<int>("stepIndex")>=0&&Get<int>("stepIndex")<Get<QualityRoute>("route").steps.Length?Get<QualityRoute>("route").steps[Get<int>("stepIndex")].name:"";
                if(w.roundOne<0&&step=="fight the Rotbloom round 1")w.roundOne=Time.time;
                // The bloom's body, again after a retry's reload, and each stalk's: who strikes them.
                if(w.body==null){var bloom=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).FirstOrDefault(e=>e.definition!=null&&e.definition.id=="Rotbloom");if(bloom!=null){w.body=bloom.Health;w.body.Damaged+=Hit;}}
                foreach(var stalk in LureStalk.All)if(stalk!=null&&heard.Add(stalk))stalk.Health.Damaged+=Stalk;
                int burned=StalksBurned;
                if(burned>=1&&w.firstBurn<0)w.firstBurn=Time.time;if(burned>=2&&w.secondBurn<0)w.secondBurn=Time.time;
                w.recent.Enqueue($"{Time.time:0.00} {lead.State} ({at.x:0.00},{at.y:0.00},{at.z:0.00}) stick={Get<UnityEngine.InputSystem.LowLevel.GamepadState>("held").leftStick} buttons={Get<UnityEngine.InputSystem.LowLevel.GamepadState>("held").buttons}");while(w.recent.Count>120)w.recent.Dequeue();
                if(w.roundOne>=0&&w.firstBurn<0&&BehindAPlinth(at))
                {
                    if(!w.behind)w.howBehind="\n-- the two seconds before he was behind a plinth --\n"+string.Join("\n",w.recent);
                    w.behind=true;float r=Vector2.Distance(Plan(at),C);if(r>w.deepestBehind){w.deepestBehind=r;w.behindAt=at;}
                }
                if(w.roundOne>=0&&w.firstTop<0&&OnATop(at))w.firstTop=Time.time;
                if(w.roundOne>=0&&w.firstTop<0&&OnARamp(at)&&at.y>-1.45f)w.upByRamp=true;
                if(w.won<0&&GameServices.Current.Flags.GetBool("bossdown.Rotbloom"))w.won=Time.time;
                if(Time.time>=w.nextTrail){w.nextTrail=Time.time+1;w.trail.Add($"{Time.time:0} {step} {lead.character} {lead.State} ({at.x:0.0},{at.y:0.00},{at.z:0.0}) hp={lead.Health.integrity:0} on={(lead.target!=null?lead.target.name:"-")} bloom={(w.body!=null?w.body.integrity:0):0}");}
            };
        }
        string Told(Watch w)=>"\n"+string.Join("\n",w.trail.TakeLast(40))+"\n-- log --\n"+string.Join("\n",log.Where(l=>!l.Contains("QUALITY_CHECKPOINT_BEGIN")).TakeLast(60));

        /// <summary>The fight as the route plays it: from the door, up the nearest ramp in a few seconds, never into the gap behind a plinth; a stalk burned by the lead's own blade;
        /// blows on the mouth that gapes for it; a second stalk gone after; the hall drained; the bloom down, as long as the bar's work and no shorter. And the driver as it was, first,
        /// fails it: into the gap behind the south-east plinth, or (if the boss's drag pulls it out) the shut bloom ground at 30% with no second stalk a minute after the first.</summary>
        // A whole boss fight from its door, the driver as it was before it, and the party's flat-out rate after it: longer than the framework's default 180 s.
        [UnityTest,Timeout(900000)] public IEnumerator TheDriverTakesTheRampBurnsTheStalksStrikesTheGapingMouthAndFightsTheBloomDown()
        {
            var steps=FightSteps();var sheet=ContentRegistry.Boss("Rotbloom");

            // ---- the driver as it was
            yield return AtTheGreatHallsDoor();Fresh();HandsOff(true);log.Clear();gapes.Clear();
            var before=new Watch();var watchBefore=Watching(before);
            yield return Play(steps,150,watchBefore,()=>before.behind||before.firstBurn>0&&Time.time-before.firstBurn>60||before.secondBurn>0||before.won>0);
            string was=Told(before);
            TestContext.WriteLine("BEFORE behind="+before.behind+" at "+before.behindAt+" firstTop="+(before.firstTop-before.roundOne).ToString("0.0")+" firstBurn="+(before.firstBurn-before.roundOne).ToString("0.0")+" secondBurn="+(before.secondBurn>0?(before.secondBurn-before.firstBurn).ToString("0.0"):"none")+was);
            Assert.IsTrue(before.behind||before.firstBurn>0&&(before.secondBurn<0||before.secondBurn-before.firstBurn>60),
                "fixture: the driver as it was neither went behind a plinth nor left the second stalk standing a minute after the first: the check below proves nothing"+was);
            HandsOff(false);Drop();

            // ---- the driver with its chase hands. Its length is measured and printed for the sheet (ROTBLOOM_DRIVER_SHARE: the shared length test holds the two to five
            // minutes through the sheet's share). It moves a good deal with what the bloom chooses: at 68,000, five fights on November 14 lasted 100.5, 102.9, 119.0, 119.5 and
            // 150.1 s. What is asserted here is that the fight is the bar's work and no bargain: never shorter than the shortest of those less a tenth (with stalks at a tenth of
            // its integrity, a quarter of its bar each, the whole fight was 74 s), and never longer than the sheet's ceiling. Those lengths were the 68,000 bar's: the floor goes
            // with the bar (D312 made it 61,500, where the same floor is 81 s and the bargain fight 67 s; a fixed 90 s failed an 86 s fight that was the bar's work).
            float integrity=GameCatalog.Find<EnemyDef>("Rotbloom").integrity;
            float NoBargain=90f*integrity/68000f;
            var w=new Watch();yield return Fight(steps,w);
            TestContext.WriteLine($"ROTBLOOM_DRIVER_FIGHT integrity={integrity:0} duration={w.duration:0.0}s retries={w.retries} toTop={w.firstTop-w.roundOne:0.0}s firstBurn={w.firstBurn-w.roundOne:0.0}s secondBurn={(w.secondBurn>0?(w.secondBurn-w.roundOne).ToString("0.0"):"none")} won={w.won-w.roundOne:0.0}s mouthBlows={w.mouthBlows} leadBlowsOnStalks={w.leadBlowsOnStalks} stalksStruck={w.stalksStruck.Count} behind={w.behind}");
            Debug.Log($"ROTBLOOM_DRIVER_FIGHT integrity={integrity:0} duration={w.duration:0.0} retries={w.retries}");
            foreach(var line in w.trail)TestContext.WriteLine("TRAIL "+line);foreach(var line in w.said)TestContext.WriteLine("LOG "+line);
            Judge(w);
            // What the sheet's share is measured from: the fight's length against the party's flat-out rate, measured as the shared length test measures it, in the same run on
            // the same machine's load (a loaded machine has read 788 a second where a quieter one read 1,177).
            float rate=0;yield return FlatOut(r=>rate=r);
            TestContext.WriteLine($"ROTBLOOM_DRIVER_SHARE share={integrity/(rate*w.duration):0.000} rate={rate:0}/s duration={w.duration:0.0}s integrity={integrity:0} sheet={sheet.driverShare:0.00} bar={sheet.minSeconds:0}-{sheet.maxSeconds:0}s");
            Assert.That(w.duration,Is.InRange(NoBargain,sheet.maxSeconds),$"the Rotbloom's {integrity:0} lasted {w.duration:0} s for the driver: under {NoBargain:0} s its stalks are a bargain again, over {sheet.maxSeconds:0} s it is past its sheet's ceiling"+w.told);
        }
        /// <summary>One fight from the door, the driver with its hands, read into the watch.</summary>
        IEnumerator Fight(QualityRoute steps,Watch w)
        {
            yield return AtTheGreatHallsDoor();Fresh();log.Clear();gapes.Clear();
            yield return Play(steps,460,Watching(w),()=>w.won>0&&Time.time-w.won>1);
            // The route's last step ends on the flag as the bloom falls in its last round, and the watch never sees that frame.
            if(w.won<0&&GameServices.Current.Flags.GetBool("bossdown.Rotbloom"))w.won=Time.time;
            w.told=Told(w);w.said=log.ToList();w.failed=Failures.ToList();Drop();
            w.duration=w.said.Where(l=>l.Contains("BOSS_DURATION Rotbloom")).Select(l=>float.Parse(l.Substring(l.IndexOf("seconds=")+8),System.Globalization.CultureInfo.InvariantCulture)).DefaultIfEmpty(-1).Last();
            w.retries=w.said.Count(l=>l.Contains("QUALITY_BOSS_RETRY"));
        }
        /// <summary>What a person does in the hall, asked of one fight. The second stalk is gone after rather than required to burn: an opening is a break its shut mouths never
        /// touch, so blows on a gaping mouth and the EXTRA can carry the bar under half, and drain the hall, while the second stalk is being burned.</summary>
        static void Judge(Watch w)
        {
            string told=w.told;
            Assert.Greater(w.roundOne,0,"the route never reached its first round"+told);
            Assert.IsFalse(w.behind,$"the lead went round into the gap behind a plinth, at {w.behindAt}"+w.howBehind+told);
            Assert.Greater(w.firstTop,0,"the lead never stood on a plinth's top"+told);
            Assert.IsTrue(w.upByRamp,"the lead came onto a plinth's top other than by its ramp"+told);
            Assert.LessOrEqual(w.firstTop-w.roundOne,6,"the lead took longer than a few seconds from the door to a stalk's top"+told);
            Assert.Greater(w.firstBurn,0,"no stalk burned"+told);
            Assert.Greater(w.leadBlowsOnStalks,0,"the lead's own blade never landed on a stalk: it was burned by the partner alone"+told);
            Assert.Greater(w.mouthBlows,0,"the lead never struck a mouth gaping for a burned stalk"+told);
            Assert.IsTrue(w.secondBurn>0||w.stalksStruck.Count>=2,"the lead never went for a second stalk: the hall drained on blows through its shut mouths"+told);
            Assert.IsTrue(w.said.Any(l=>l.Contains("ROTBLOOM_DRAINS")),"the hall never drained"+told);
            Assert.Greater(w.won,0,"the Rotbloom never fell"+told);
            Assert.AreEqual(0,w.retries,"the party fell at the door: the length below isn't one fight's"+told);
            CollectionAssert.IsEmpty(w.failed,"the route's steps failed"+told);
            Assert.Greater(w.duration,0,"no BOSS_DURATION was logged"+told);
        }

        // ------------------------------------------------------------------ a findWay walk over the mire
        /// <summary>The route's own steps from "Mire_Leeches cleared" on the second island to "to the third island 2/2".</summary>
        static QualityRoute MireSteps()
        {
            string file=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes",Route+".json");
            var route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));
            int from=System.Array.FindIndex(route.steps,s=>s.name=="Mire_Leeches cleared");int to=System.Array.FindIndex(route.steps,s=>s.name=="to the third island 2/2");
            Assert.Greater(from,0,"fixture: the route clears Mire_Leeches");Assert.Greater(to,from,"fixture: the route walks to the third island after it");
            var walk=route.steps.First(s=>s.name=="to the third island 1/2");Assert.IsTrue(walk.findWay&&walk.navigate,"fixture: the walk to the third island finds its way");
            return new QualityRoute{name=route.name,scene=Station,autoHeal=route.autoHeal,useExtra=route.useExtra,steps=route.steps.Skip(from).Take(to-from+1).ToArray()};
        }
        /// <summary>The second island as the route leaves it: Mire_Leeches cleared, the lead where that fight ended in the reconnaissance (10.5, 91.6), the partner beside him,
        /// everything awake left still.</summary>
        IEnumerator OnTheSecondIsland()
        {
            foreach(string key in new[]{"wend.charted","wend.lineHeld","wend.onaHeard","wend.tideTurned","wend.lureLearned","clear.Mire_Leeches"})GameServices.Current.Flags.SetBool(key,true);
            SceneFlow.Current.LoadZone(Station,"Arrival");float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+Station+" never finished loading");
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.8f);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            var party=PartyController.Current;if(party.Active.character!="Taren")Assert.IsTrue(party.Swap(),"fixture: Taren leads");
            void Put(CombatActor actor,Vector3 at)
            {
                var shape=actor.GetComponent<CharacterController>();shape.enabled=false;if(actor.motor is GroundMotor ground)ground.Halt();
                actor.transform.position=at;shape.enabled=true;Physics.SyncTransforms();
            }
            Put(party.Active,new Vector3(10.5f,-.28f,91.6f));Put(party.members.First(m=>m!=party.Active),new Vector3(12f,-.28f,90.5f));
            for(int i=0;i<5;i++)yield return null;
        }
        /// <summary>A walk that finds its way (`"findWay": true`) goes to the route's point on the plan, wherever the floor's own height is there. The mire's third island rises 0.7 m
        /// over the route's point (-3, 101), and the 14:26 driver took that point on its own level: the way ended 1.5 m off on lower ground, and the lead ran about for the whole
        /// 17 s without arriving (nov14-recon15 s26: 91 m walked against 17). Built that way first, under the switch, and required to fail.</summary>
        [UnityTest] public IEnumerator AWalkThatFindsItsWayReachesItsPointOnTheThirdIslandsBank()
        {
            var steps=MireSteps();var goal=new Vector2(-3,101);
            float Off(){var at=Lead.transform.position;return Vector2.Distance(new Vector2(at.x,at.z),goal);}
            // ---- the driver as it was at 14:26
            yield return OnTheSecondIsland();Fresh();PlanGoalsOnTheirLevel(true);
            float startedAt=-1,closest=float.PositiveInfinity,ranBefore=0;Vector3 last=Lead.transform.position;
            yield return Play(steps,40,()=>
            {
                string name=Get<QualityRoute>("route").steps[Mathf.Clamp(Get<int>("stepIndex"),0,steps.steps.Length-1)].name;
                if(name!="to the third island 1/2")return;
                if(startedAt<0)startedAt=Time.time;var at=Lead.transform.position;var d=at-last;d.y=0;ranBefore+=d.magnitude;last=at;closest=Mathf.Min(closest,Off());
            },()=>false);
            var before=Failures.ToList();Drop();PlanGoalsOnTheirLevel(false);
            TestContext.WriteLine($"BEFORE ran={ranBefore:0.0} m closest={closest:0.00} m failures={string.Join(" | ",before)}");
            // The fault is the wandering: it ran four times the walk's seventeen metres and, most times, never arrived. Once in a while its wandering ends inside the point's
            // tolerance (closest 0.73 m after 72.1 m, the suite's evening run on November 14), and that run shows the fault as plainly as one that timed out.
            Assert.IsTrue(before.Any(f=>f.StartsWith("to the third island 1/2"))||ranBefore>45,$"fixture: the 14:26 driver walked to the third island's point as a person would (closest {closest:0.00} m, ran {ranBefore:0.0} m): the check below proves nothing");

            // ---- today's driver
            yield return OnTheSecondIsland();Fresh();
            startedAt=-1;closest=float.PositiveInfinity;float ran=0,arrivedAt=-1;last=Lead.transform.position;
            yield return Play(steps,40,()=>
            {
                string name=Get<QualityRoute>("route").steps[Mathf.Clamp(Get<int>("stepIndex"),0,steps.steps.Length-1)].name;
                if(name!="to the third island 1/2")return;
                if(startedAt<0){startedAt=Time.time;last=Lead.transform.position;}var at=Lead.transform.position;var d=at-last;d.y=0;ran+=d.magnitude;last=at;closest=Mathf.Min(closest,Off());
                if(arrivedAt<0&&Off()<=.8f)arrivedAt=Time.time;
            },()=>false);
            var failed=Failures.ToList();
            TestContext.WriteLine($"AFTER ran={ran:0.0} m closest={closest:0.00} m arrived in {(arrivedAt>0?arrivedAt-startedAt:-1):0.0} s failures={string.Join(" | ",failed)}");
            CollectionAssert.IsEmpty(failed,"the walk from the second island to the third failed a step");
            Assert.Greater(arrivedAt,0,$"the walk never came within its 0.8 m of (-3, 101): closest {closest:0.00} m");
            Assert.Less(ran,30,$"the walk to the third island ran {ran:0.0} m for a 17 m way");
            Assert.Less(arrivedAt-startedAt,10,"the walk to the third island took longer than its route allows a straight 17 m");
        }

        /// <summary>The party flat out on the Rotbloom standing still in the proving yard, exactly as QaPlanCombatTests.FlatOut measures BOSS_RATE for the shared length test: the
        /// chapter's arrival loadout at the sheet's Sync with skill uses cleared, Taren at its body, Sela (an emitter) eight metres back, both acting every frame for 20 s.</summary>
        static IEnumerator FlatOut(System.Action<float> measured)
        {
            const float Seconds=20;
            DevLoadout.Apply("chapter4");
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: the yard never finished loading");
            foreach(var left in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(left.gameObject);
            yield return new WaitForSecondsRealtime(.7f);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Rotbloom driver fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            foreach(var member in State.party){member.level=ContentRegistry.Boss("Rotbloom").sync;member.skillUses?.Clear();}PartyController.Current.RefreshStats();
            var party=PartyController.Current;if(party.Active.character!="Taren")Assert.IsTrue(party.Swap(),"fixture: the swap");
            foreach(var actor in party.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            var taren=party.Active;var sela=party.members.First(m=>m!=taren);
            Assert.AreNotEqual(GearSlot.Edge,DevLoadout.Weapon(sela.character),"fixture: the partner shoots, and stands back as the shared measure stands her");
            var at=new Vector3(200,.1f,0);var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Rotbloom"),at);enemy.Passive=true;
            for(int i=0;i<3;i++)yield return null;var body=enemy.Health;body.maximum=body.integrity=10000000;
            float reach=BossReach.Edge(enemy);var tarenAt=at+Vector3.back*reach;var selaAt=at+Vector3.back*8+Vector3.right*2;
            void Place(CombatActor actor,Vector3 position)
            {
                var shape=actor.GetComponent<CharacterController>();shape.enabled=false;if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}
                actor.transform.SetPositionAndRotation(position,Quaternion.identity);shape.enabled=true;actor.target=null;actor.TargetLocked=false;Physics.SyncTransforms();
            }
            Place(taren,tarenAt);Place(sela,selaAt);taren.target=sela.target=body;taren.charge=sela.charge=100;yield return null;
            float landed=0;body.Damaged+=(_,packet,amount)=>{if(packet.source==taren.Health||packet.source==sela.Health)landed+=amount;};
            bool Act(CombatActor hero)
            {
                if(!hero.CanAct)return false;
                for(int slot=0;slot<SkillBook.Slots;slot++)
                {
                    var skill=SkillBook.Equipped(hero.character,slot);
                    if(skill==null||hero.cooldowns[slot]>0||hero.charge<SkillBook.Cost(hero.character,skill))continue;
                    if(hero.Skill(slot))return true;
                }
                return hero.Attack();
            }
            for(float end=Time.unscaledTime+Seconds;Time.unscaledTime<end;)
            {
                foreach(var hero in new[]{taren,sela})
                {
                    hero.motor.Move(Vector2.zero,false,false);var home=hero==taren?tarenAt:selaAt;
                    var off=hero.transform.position-home;off.y=0;if(hero.CanAct&&off.magnitude>.6f)Place(hero,home);
                    hero.target=body;Act(hero);
                }
                yield return null;
            }
            Assert.Greater(landed,0,"fixture: the party is hitting it");measured(landed/Seconds);
            enemy.Passive=true;Object.Destroy(enemy.gameObject);
        }
    }
}
