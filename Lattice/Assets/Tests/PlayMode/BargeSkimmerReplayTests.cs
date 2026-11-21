using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Barge three's skimmers in Xylos' nearspace (route 17, tools/xylos_routes.py), in the zone as built. The first whole run of route 17 stopped at "Barge3_Skimmers cleared" twice
    /// (nov15-recon38 s17, nov20-recon40 s17): the lead sat braked beside the barge for seventy seconds with the last skimmer alive, and nobody took a blow. Played through the driver's own pad
    /// with the route's own steps six times, the same stop came twice: the last skimmer, circling the lead with the hull between them, ran into the hull and was held there (once against its
    /// far side, twice wedged between the bow and the bow's knot), never seeing the ship and so never making its pass; and the lead, its lock on that skimmer 11.8 m off (inside the step's
    /// reach of 12), held the brake and fired four hundred shots into the hull. Both are fixed and each is shown failing first, in its own case, at the place it happened: the skimmer
    /// (EnemyStrafer.ComeRound; EnemyStrafer.ComesRoundOff for the strafer as it was) and the driver's hands (ContinuousReview.Cover.cs; coverHandsOff for the driver as it was). So is the
    /// fault that ended November 15's run after the stop: the walk to the flank knot from the hull's far side ground the lead against the hull until she fell (roundHandsOff). Then the
    /// whole stretch, from the third swarm's end past Teo's sight of the claim drones, both of barge three's fights and on to the delta beacon, played three times through the pad. What the lead
    /// has locked, where every skimmer is and what it is doing, and what the lead's fire lands on, are written every second (BARGE3_SAMPLE).</summary>
    public sealed class BargeSkimmerReplayTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;bool finished;readonly List<string> log=new();readonly List<string> errors=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Heeded={"QUALITY_TOWARD","QUALITY_CHASE","QUALITY_LOCK","QUALITY_COVER","QUALITY_FLIES","ENCOUNTER_","COMBAT_KILL","BREAK_OK","WALL_CONTACT","STRAFER_","BARGE_","ANCHOR_","DEFEAT","SWAP_OK"};
        void Logged(string text,string stack,LogType type)
        {
            if(type==LogType.Exception||type==LogType.Error){errors.Add(text+" "+(stack??"").Split('\n').FirstOrDefault());return;}
            foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add($"{Time.unscaledTime-began:0.0} {text}");return;}
        }
        float began;
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("chapter3");
            fixture=new GameObject("Barge skimmer replay fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
            log.Clear();errors.Clear();finished=false;began=Time.unscaledTime;Application.logMessageReceived+=Logged;AsItWas(false,false);WalkAsItWas(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;AsItWas(false,false);WalkAsItWas(false);
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(fixture!=null)Object.Destroy(fixture);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        /// <summary>The broken states, for a case's first half: the strafer before it came round, the driver before its cover hands. Put back by every case and the tear-down.</summary>
        static void AsItWas(bool strafer,bool driver)
        {
            EnemyStrafer.ComesRoundOff=strafer;
            typeof(ContinuousReview).GetField("coverHandsOff",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,driver);
        }

        // ------------------------------------------------------------------ the driver, by its private parts (as MooringBossReplayTests drives it)
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        List<string> Failures=>Get<List<string>>("failures");
        string Ui()=>(string)Call("ObserveUi");
        IEnumerator Play(QualityRoute route,float most,System.Action each=null,System.Func<bool> enough=null)
        {
            if(Get<Gamepad>("pad")==null)Set("pad",InputSystem.AddDevice<Gamepad>("BargeSkimmerPad"));
            Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);var clock=Get<System.Diagnostics.Stopwatch>("clock");if(!clock.IsRunning)clock.Start();
            var run=(IEnumerator)Call("RunSteps");finished=false;Failures.Clear();
            for(float end=Time.unscaledTime+most;Time.unscaledTime<end;)
            {
                if(enough!=null&&enough())break;
                if(!run.MoveNext()){finished=true;break;}
                each?.Invoke();Call("InputUpdate");yield return null;
            }
            Set("recording",false);Call("InputUpdate");
        }

        // ------------------------------------------------------------------ route 17, read
        const string Route="chapter-17-xylos-nearspace";
        const string From="fly on until Teo's sight of the claim drones",Lock="lock Barge3_Skimmers",Pursue="pursue Barge3_Skimmers round 1",Lights="the delta beacon lights";
        static string Folder=>System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
        static QualityRoute Read()=>JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(System.IO.Path.Combine(Folder,Route+".json")));
        /// <summary>Route 17 from the named step to the named one, its own hands (autoHeal, useExtra, wearBest) kept.</summary>
        static QualityRoute Slice(string from,string to)
        {
            var whole=Read();int a=System.Array.FindIndex(whole.steps,s=>s.name==from);Assert.GreaterOrEqual(a,0,$"fixture: route 17 has no step '{from}'");
            int b=System.Array.FindIndex(whole.steps,a,s=>s.name==to);Assert.GreaterOrEqual(b,a,$"fixture: route 17 has no step '{to}' after '{from}'");
            whole.steps=whole.steps.Skip(a).Take(b-a+1).ToArray();whole.scene="XylosNearspace";whole.name+=" from "+from;return whole;
        }
        /// <summary>Every flag route 17 has asserted or waited on before the step: the third swarm cleared, barges one and two free, the voices heard.</summary>
        static string[] FlagsBefore(string step)
        {
            var flags=new HashSet<string>{"xylos.charted"};
            foreach(var s in Read().steps)
            {
                if(s.name==step)return flags.ToArray();
                if(!string.IsNullOrEmpty(s.expectedFlag))flags.Add(s.expectedFlag);
                if(!string.IsNullOrEmpty(s.until)&&s.until.StartsWith("flag:"))flags.Add(s.until.Substring(5));
            }
            Assert.Fail("fixture: route 17 has no step '"+step+"'");return null;
        }
        static readonly string[] Later={"clear.Barge3_Skimmers","clear.Barge3_Knots","clear.Barge3_Claim","xylos.barge3","xylos.bargesFree","xylos.claimSeen","xylos.teoBarge3","xylos.teoLanding"};

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.FirstOrDefault(m=>m!=Lead);
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static void Put(CombatActor actor,Vector3 at,Vector3 facing)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(facing);}
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static EncounterVolume Fight(string id)=>Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(e=>e.encounterId==id);
        static DamageType Neutral(Health body)=>System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
        /// <summary>Xylos' nearspace with every flag route 17 set before the step, and barge three's fights and what follows them cleared: loaded, Taren leading.</summary>
        IEnumerator Nearspace(string step,params string[] alsoSet)
        {
            foreach(string flag in Later)GameServices.Current.Flags.SetBool(flag,false);
            foreach(string flag in FlagsBefore(step).Concat(alsoSet))GameServices.Current.Flags.SetBool(flag,true);
            State.questSteps["SeedRoad"]=1;
            SceneFlow.Current.LoadZone("XylosNearspace","Arrival");float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: XylosNearspace never finished loading");
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(1f);
            for(int i=0;i<4&&Lead.character!="Taren";i++){PartyController.Current.Swap();yield return Frames(30);}
            Assert.AreEqual("Taren",Lead.character,"fixture: Taren leads");
            foreach(var hero in PartyController.Current.members){hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=0;hero.target=null;hero.TargetLocked=false;hero.GetComponent<PlayerBrain>().AutoPilot=false;hero.GetComponent<PartnerBrain>().enabled=hero!=Lead;}
        }
        /// <summary>Where the lead stood as route 17's flight past the claim drones' line began, in both runs that stopped: the third swarm's ground.</summary>
        static readonly Vector3 LaneTwoEnd=new(528.5f,1,1022.5f);
        static readonly Vector3 TowardTheLine=new Vector3(717.6f-528.5f,0,1098.8f-1022.5f).normalized;
        /// <summary>The pair at the third swarm's end, where the run had them, whole.</summary>
        IEnumerator AtTheThirdSwarmsEnd()
        {
            yield return Nearspace(From);
            Put(Lead,LaneTwoEnd,TowardTheLine);Put(Partner,LaneTwoEnd-TowardTheLine*3+Vector3.Cross(Vector3.up,TowardTheLine)*2,TowardTheLine);
            yield return new WaitForSecondsRealtime(.5f);
        }

        // ------------------------------------------------------------------ the stop, staged where it happened
        /// <summary>Where the lead sat in the two stops of the six plays: on the lane side of barge three, beside its middle. Where the last skimmer came from (scattered to the hull's far side)
        /// and where it was held, between the bow and the bow's knot, 11.8 m off and out of his sight.</summary>
        static readonly Vector3 BesideTheHull=new(761.2f,1,1147.87f),FarSide=new(746.2f,1,1156.9f),Wedged=new(760.3f,1,1159.6f),BowKnot=new(761.9f,1,1159.8f);
        /// <summary>Where the lead sat in the player's run today (nov20-recon40 s17: 71 s at (758.69, 1141.89), his nose on (-0.346, 0.938)): beside the stern on the lane side. The skimmer it
        /// locked was held against the hull's far side on that bearing, 11 to 12 m off; here it starts beside that face.</summary>
        static readonly Vector3 BesideTheStern=new(758.69f,1,1141.89f),OffTheFarFace=new(753.6f,1,1153.4f);
        static readonly FieldInfo StraferSide=typeof(EnemyStrafer).GetField("side",Private),StraferState=typeof(EnemyStrafer).GetField("state",Private);
        static string StateOf(EnemyBrain brain)=>brain!=null&&brain.Behaviour is EnemyStrafer strafer?StraferState.GetValue(strafer).ToString():brain!=null&&brain.Behaviour!=null?brain.Behaviour.GetType().Name:"-";
        static bool Sees(EnemyBrain skimmer,CombatActor lead)=>CombatCover.Clear(skimmer.transform.position+Vector3.up,lead.transform.position+Vector3.up*.9f);
        /// <summary>Barge three as the two stops of the six plays had it: its wing's fight over but for one skimmer, the knots' fight begun and the bow's knot the only one left, the lead beside the
        /// hull's middle (whole, his bar held), Sela out of it far up the lane, and the last skimmer on the hull's far side going round toward the bow, as it went both times.</summary>
        /// <remarks>With <paramref name="knotsAwake"/> false, it is the player's run of today instead: the lead beside the stern, the skimmer beside the hull's far face going round the same
        /// way, and the knots' fight held from beginning (it waits on a flag nobody sets), all three knots on show, asleep and not to be locked, as a barge's are before its fight: nothing is
        /// awake in the open but the skimmer behind the hull.</remarks>
        IEnumerator TheLastSkimmer(System.Action<EnemyBrain> made,bool knotsAwake=true)
        {
            yield return Nearspace(From,"xylos.claimSeen","clear.Barge3_Skimmers");
            var lead=Lead;var partner=Partner;
            partner.GetComponent<PartnerBrain>().enabled=false;Put(partner,new Vector3(700,1,1090),Vector3.forward);
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+900;
            var knots=Fight("Barge3_Knots");Assert.IsNotNull(knots,"fixture: barge three has its knots");
            // Suspended, an encounter takes its show down (OnDisable); held by a flag nobody sets, it keeps the knots on show and never begins.
            if(!knotsAwake)knots.requiredFlag="fixture.knotsHeld";
            var stand=knotsAwake?BesideTheHull:BesideTheStern;var from=knotsAwake?FarSide:OffTheFarFace;
            lead.GetComponent<PlayerBrain>().AutoPilot=true;Put(lead,stand,knotsAwake?Wedged-stand:new Vector3(-.346f,0,.938f));
            if(knotsAwake)
            {
                knots.Begin();yield return Frames(3);
                foreach(var knot in Health.All.Where(h=>h!=null&&!h.friendly&&h.Alive&&h.id=="SnagvineNode"&&Flat(h.transform.position,BowKnot)>2&&Flat(h.transform.position,BesideTheHull)<20).ToArray())
                    knot.Receive(new DamagePacket{source=lead.Health,amount=knot.maximum*20,type=Neutral(knot)});
            }
            yield return new WaitForSecondsRealtime(.5f);
            var left=Health.All.Where(h=>h!=null&&!h.friendly&&h.Alive&&h.id=="SnagvineNode"&&Flat(h.transform.position,BesideTheHull)<20).ToArray();
            if(knotsAwake){Assert.AreEqual(1,left.Length,"fixture: barge three keeps its bow knot alone");Assert.Less(Flat(left[0].transform.position,BowKnot),1,"fixture: the knot left is the bow's");}
            else{Assert.IsFalse(knots.Started,"fixture: the knots' fight began");Assert.AreEqual(3,left.Length,"fixture: barge three shows its three knots");Assert.IsFalse(left.Any(k=>k.Targetable),"fixture: a knot on show can be locked before its fight");}
            var skimmer=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Podskimmer"),from);skimmer.Passive=false;yield return Frames(2);
            Assert.IsTrue(skimmer.Behaviour is EnemyStrafer,"fixture: a Podskimmer is a strafer");
            // Going round toward the bow, as it went in all three stops (the ring's other way takes it round the stern).
            StraferSide.SetValue(skimmer.Behaviour,-1);
            made(skimmer);
        }
        /// <summary>The lead held where he is, as a pilot with the stick at rest: the motor is driven every frame.</summary>
        static IEnumerator Hold(float seconds,System.Func<bool> done=null)
        {
            for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end&&(done==null||!done());){if(Lead.Health.Alive)Lead.motor.Move(Vector2.zero,false,false);yield return null;}
        }
        /// <summary>Watches the skimmer while the lead stands: whether it marked a lane, whether that lane ran through him, and how far it went in the last seconds of the watch.</summary>
        sealed class Watched{public bool told,throughHim;public float lastMoved=float.PositiveInfinity;public string state="",at="";public bool sees;public readonly List<(float t,Vector3 p)> path=new();}
        IEnumerator WatchSkimmer(EnemyBrain skimmer,float seconds,Watched w)
        {
            float from=Time.unscaledTime;
            yield return Hold(seconds,()=>
            {
                if(skimmer==null||!skimmer.Health.Alive)return true;
                w.path.Add((Time.unscaledTime-from,skimmer.transform.position));
                if(skimmer.Telegraphing&&skimmer.AttackMark!=null){w.told=true;w.throughHim|=skimmer.AttackMark.Contains(Lead.transform.position);return true;}
                return false;
            });
            if(skimmer!=null&&w.path.Count>0)
            {
                float t=w.path[w.path.Count-1].t;var end=w.path[w.path.Count-1].p;var then=w.path.LastOrDefault(p=>p.t<=t-4).p;
                w.lastMoved=w.path.Any(p=>p.t<=t-4)?Flat(end,then):float.PositiveInfinity;w.state=StateOf(skimmer);w.at=$"({end.x:0.0}, {end.z:0.0})";w.sees=Sees(skimmer,Lead);
            }
        }

        /// <summary>The skimmer at the stop, the lead held still beside the hull. As it was it runs round the far side into the wedge between the bow and its knot and is held there, out of his
        /// sight, marking nothing (the fixture's proof: nothing below says anything if it doesn't). Now, held there with him out of sight, it goes the other way round (STRAFER_COMES_ROUND) and
        /// comes round the barge to mark its pass through him.</summary>
        [UnityTest,Timeout(600000)] public IEnumerator HeldAgainstTheBargeOutOfTheShipsSightTheLastSkimmerComesRoundAndMakesItsPass()
        {
            EnemyBrain skimmer=null;
            // As it was: the ring runs it into the hull and it stays there.
            AsItWas(true,false);
            yield return TheLastSkimmer(s=>skimmer=s);var broken=new Watched();yield return WatchSkimmer(skimmer,16,broken);
            TestContext.WriteLine($"BARGE3_WEDGE asItWas=True told={broken.told} at={broken.at} state={broken.state} sees={broken.sees} movedLast4s={broken.lastMoved:0.00} "+string.Join(" ",broken.path.Where((p,i)=>i%30==0).Select(p=>$"{p.t:0.0}:({p.p.x:0.0},{p.p.z:0.0})")));
            Assert.IsFalse(broken.told,"fixture: as it was, the skimmer marked its pass at the lead all the same: the check below proves nothing");
            Assert.Less(broken.lastMoved,1f,$"fixture: as it was, the skimmer was never held against the barge (it ended at {broken.at}, {broken.state}): the check below proves nothing");
            Assert.IsFalse(broken.sees,"fixture: as it was, the skimmer was held where it could see the lead");
            Object.Destroy(skimmer.gameObject);log.Clear();
            // As it is.
            AsItWas(false,false);
            yield return TheLastSkimmer(s=>skimmer=s);var fixedUp=new Watched();yield return WatchSkimmer(skimmer,16,fixedUp);
            TestContext.WriteLine($"BARGE3_WEDGE asItWas=False told={fixedUp.told} throughHim={fixedUp.throughHim} at={fixedUp.at} state={fixedUp.state} "+string.Join(" ",fixedUp.path.Where((p,i)=>i%30==0).Select(p=>$"{p.t:0.0}:({p.p.x:0.0},{p.p.z:0.0})")));
            foreach(var line in log)TestContext.WriteLine("LOG "+line);
            Assert.IsTrue(fixedUp.told,$"held against barge three out of the lead's sight, the skimmer never came round to make its pass: it ended at {fixedUp.at}, {fixedUp.state}");
            Assert.IsTrue(fixedUp.throughHim,"the skimmer came round and marked a lane that doesn't run through the ship it was circling");
        }

        /// <summary>The driver at the stop, the skimmer as it was (held in the wedge behind the bow, out of sight): the route's own lock and first round, through the pad. As it was the lead locks
        /// the skimmer, holds the brake and fires into the hull for the whole round, and lands nothing on it (the fixture's proof). With the cover hands he flicks to the bow's knot in the open,
        /// or flies aside for a line, and the skimmer is down inside the round, by his fire.</summary>
        [UnityTest,Timeout(600000)] public IEnumerator WithTheLockOnASkimmerBehindTheHullTheLeadFlicksToTheKnotInTheOpenAndGetsItInsideTheRound()
        {
            yield return DriverAtTheStop(true,"QUALITY_COVER_FLICK");
        }
        /// <summary>The same with nothing awake in the open (the knots on show and asleep, as before their fight): with the cover hands the lead flies round, the brake off, to a place his
        /// shots reach the skimmer from (QUALITY_COVER_ASIDE), and has it down inside the round.</summary>
        [UnityTest,Timeout(600000)] public IEnumerator WithNothingAwakeInTheOpenTheLeadFliesRoundForALineToTheSkimmerBehindTheHull()
        {
            yield return DriverAtTheStop(false,"QUALITY_COVER_ASIDE");
        }
        /// <summary>Route 17's lock and first round on the skimmer held behind the hull (the strafer as it was), first with the driver as it was, which must fire and land nothing all round, then
        /// with the cover hands, which must have it down inside the round by the lead's fire, by the hand named.</summary>
        IEnumerator DriverAtTheStop(bool knotsAwake,string hand)
        {
            var round=Slice(Lock,Pursue);
            foreach(var s in round.steps)s.stopWhen=null;
            IEnumerator Round(bool asItWas,System.Action<bool,float,int,bool> done)
            {
                EnemyBrain skimmer=null;
                AsItWas(true,asItWas);
                yield return TheLastSkimmer(s=>skimmer=s,knotsAwake);
                var held=new Watched();yield return WatchSkimmer(skimmer,14,held);
                Assert.IsFalse(held.told,"fixture: the skimmer as it was marked its pass before the round");Assert.Less(held.lastMoved,1f,$"fixture: the skimmer as it was is held behind the hull before the round (it is at {held.at})");
                float landed=0;var body=skimmer.Health;System.Action<Health,DamagePacket,float> count=(_,packet,amount)=>{if(packet.source==Lead.Health)landed+=amount;};body.Damaged+=count;
                Lead.GetComponent<PlayerBrain>().AutoPilot=false;int shots=Lead.AttackSequence;
                float next=0;began=Time.unscaledTime;log.Clear();
                yield return Play(round,40,()=>
                {
                    if(Time.unscaledTime<next)return;next=Time.unscaledTime+2;
                    var t=Lead.target;
                    TestContext.WriteLine($"BARGE3_ROUND asItWas={asItWas} {Time.unscaledTime-began:0} s step={Get<int>("stepIndex")} lead ({Lead.transform.position.x:0.0},{Lead.transform.position.z:0.0}) {Lead.State} target={(t!=null?t.id+(t.Alive?"":" dead"):"none")} skimmer {(body.Alive?$"({skimmer.transform.position.x:0.0},{skimmer.transform.position.z:0.0}) {StateOf(skimmer)} hp={body.integrity:0}":"down")} landed={landed:0}");
                },()=>!body.Alive);
                body.Damaged-=count;
                bool used=log.Any(l=>l.Contains(hand));
                foreach(var line in log)TestContext.WriteLine("LOG "+line);log.Clear();
                done(body.Alive,landed,Lead.AttackSequence-shots,used);
                if(skimmer!=null)Object.Destroy(skimmer.gameObject);
            }
            bool alive=false,usedHand=false;float onIt=0;int fired=0;
            yield return Round(true,(a,l,f,u)=>{alive=a;onIt=l;fired=f;usedHand=u;});
            TestContext.WriteLine($"BARGE3_DRIVER knotsAwake={knotsAwake} asItWas=True alive={alive} landed={onIt:0} shots={fired}");
            Assert.IsTrue(alive&&onIt<=0,$"fixture: as it was, the lead's fire reached the skimmer behind the hull ({onIt:0} landed, alive={alive}): the check below proves nothing");
            Assert.Greater(fired,20,"fixture: as it was, the lead never fired at the skimmer behind the hull");
            yield return Round(false,(a,l,f,u)=>{alive=a;onIt=l;fired=f;usedHand=u;});
            TestContext.WriteLine($"BARGE3_DRIVER knotsAwake={knotsAwake} asItWas=False alive={alive} landed={onIt:0} shots={fired} {hand}={usedHand}");
            Assert.IsFalse(alive,"with the lock on the skimmer behind barge three's hull, the lead never got it inside the round");
            Assert.Greater(onIt,0,"the skimmer behind the hull went down, but nothing of the lead's fire landed on it");
            Assert.IsTrue(usedHand,$"the skimmer behind the hull went down without the hand this case is for ({hand})");
        }

        /// <summary>Where the chase left the lead in the run of November 15 and in a play of the whole lane through the pad (Taren down at barge two, Sela leading): on barge three's far side,
        /// where the walk to the flank knot held her against the hull, a scrape every 0.6 s, until she fell.</summary>
        static readonly Vector3 AgainstTheFarSide=new(752.65f,1,1150.9f);
        static void WalkAsItWas(bool on)=>typeof(ContinuousReview).GetField("roundHandsOff",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,on);
        /// <summary>Route 17's walk to barge three's flank knot ("beside barge three's flank knot", tolerance 2 m) from the hull's far side, the knots on show and asleep and nothing else
        /// awake, the lead's bar held so a scrape costs nothing here and is only counted. As it was the walk runs straight at the point, through the hull, and grinds against it for the
        /// walk's 24 s without arriving (the fixture's proof). Now, a second without gaining, it flies round the barge (QUALITY_FLIES_ROUND) and arrives, scraping at most three times (it starts against the hull, and scrapes every 0.6 s until it turns).</summary>
        [UnityTest,Timeout(600000)] public IEnumerator AWalkFromTheHullsFarSideFliesRoundTheBargeInsteadOfGrindingAgainstIt()
        {
            var walk=Slice("beside barge three's flank knot","beside barge three's flank knot");var goal=walk.steps[0].point;
            IEnumerator Walk(bool asItWas,System.Action<bool,int,bool> done)
            {
                WalkAsItWas(asItWas);
                yield return Nearspace(From,"xylos.claimSeen","clear.Barge3_Skimmers");
                var knots=Fight("Barge3_Knots");knots.requiredFlag="fixture.knotsHeld";
                var partner=Partner;partner.GetComponent<PartnerBrain>().enabled=false;Put(partner,new Vector3(700,1,1090),Vector3.forward);
                foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+900;
                Put(Lead,AgainstTheFarSide,goal-AgainstTheFarSide);yield return new WaitForSecondsRealtime(.5f);
                log.Clear();began=Time.unscaledTime;
                yield return Play(walk,40);
                int scrapes=log.Count(l=>l.Contains("WALL_CONTACT")&&l.Contains("Seed barge 3"));
                bool arrived=Failures.Count==0&&Flat(Lead.transform.position,goal)<=walk.steps[0].tolerance+.5f,round=log.Any(l=>l.Contains("QUALITY_FLIES_ROUND"));
                TestContext.WriteLine($"BARGE3_WALK asItWas={asItWas} arrived={arrived} at=({Lead.transform.position.x:0.0},{Lead.transform.position.z:0.0}) scrapes={scrapes} round={round} failures={string.Join(" | ",Failures)}");
                foreach(var line in log)TestContext.WriteLine("LOG "+line);
                done(arrived,scrapes,round);log.Clear();WalkAsItWas(false);
            }
            bool there=false,flewRound=false;int scraped=0;
            yield return Walk(true,(a,s,r)=>{there=a;scraped=s;flewRound=r;});
            Assert.IsFalse(there,"fixture: as it was, the walk from the hull's far side arrived all the same: the check below proves nothing");
            Assert.GreaterOrEqual(scraped,10,"fixture: as it was, the walk from the hull's far side never ground against the hull");
            yield return Walk(false,(a,s,r)=>{there=a;scraped=s;flewRound=r;});
            Assert.IsTrue(there,"from barge three's far side, the walk to the flank knot never got there");
            Assert.IsTrue(flewRound,"the walk from barge three's far side got there without going round the barge");
            Assert.LessOrEqual(scraped,3,$"from barge three's far side, the walk to the flank knot scraped the hull {scraped} times");
        }

        /// <summary>Where a play of the whole lane through the pad had Taren when he took the bow's knot up from beyond the stern, 20 m off with the hull lying along the line to it: the chase
        /// slid him down the hull's side, seven scrapes in four seconds, and with the freed barge's drift he fell.</summary>
        static readonly Vector3 BeyondTheStern=new(754.6f,1,1140.7f);
        /// <summary>Route 17's lock and first round on barge three's knots with the bow's knot the only one left and the lead beyond the stern (his bar held, scrapes only counted), until he has
        /// it in the step's reach. As it was the chase runs straight at it and slides down the hull (at least four scrapes: the fixture's proof); now it flies round the hull and scrapes it
        /// twice at most.</summary>
        [UnityTest,Timeout(600000)] public IEnumerator AChaseAfterTheBowKnotFromBeyondTheSternFliesRoundTheHull()
        {
            var round=Slice("lock Barge3_Knots","pursue Barge3_Knots round 1");float reach=round.steps.Last().tolerance;
            IEnumerator Chase(bool asItWas,System.Action<bool,int> done)
            {
                WalkAsItWas(asItWas);
                yield return Nearspace(From,"xylos.claimSeen","clear.Barge3_Skimmers");
                var partner=Partner;partner.GetComponent<PartnerBrain>().enabled=false;Put(partner,new Vector3(700,1,1090),Vector3.forward);
                foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+900;
                Put(Lead,BeyondTheStern,BowKnot-BeyondTheStern);
                var knots=Fight("Barge3_Knots");knots.Begin();yield return Frames(3);
                foreach(var knot in Health.All.Where(h=>h!=null&&!h.friendly&&h.Alive&&h.id=="SnagvineNode"&&Flat(h.transform.position,BowKnot)>2&&Flat(h.transform.position,BesideTheHull)<20).ToArray())
                    knot.Receive(new DamagePacket{source=Lead.Health,amount=knot.maximum*20,type=Neutral(knot)});
                yield return new WaitForSecondsRealtime(.5f);
                var bow=Health.All.FirstOrDefault(h=>h!=null&&!h.friendly&&h.Alive&&h.id=="SnagvineNode"&&Flat(h.transform.position,BowKnot)<1);
                Assert.IsNotNull(bow,"fixture: the bow's knot is left");
                Put(Lead,BeyondTheStern,BowKnot-BeyondTheStern);Lead.target=null;Lead.TargetLocked=false;yield return Frames(2);
                log.Clear();began=Time.unscaledTime;
                yield return Play(round,20,null,()=>!bow.Alive||Flat(Lead.transform.position,bow.transform.position)<=reach);
                int scrapes=log.Count(l=>l.Contains("WALL_CONTACT")&&l.Contains("Seed barge 3"));bool inReach=bow.Alive&&Flat(Lead.transform.position,bow.transform.position)<=reach||!bow.Alive;
                TestContext.WriteLine($"BARGE3_CHASE asItWas={asItWas} inReach={inReach} at=({Lead.transform.position.x:0.0},{Lead.transform.position.z:0.0}) target={(Lead.target!=null?Lead.target.id:"none")} scrapes={scrapes}");
                foreach(var line in log)TestContext.WriteLine("LOG "+line);
                done(inReach,scrapes);log.Clear();WalkAsItWas(false);
            }
            bool there=false;int scraped=0;
            yield return Chase(true,(a,s)=>{there=a;scraped=s;});
            Assert.GreaterOrEqual(scraped,4,"fixture: as it was, the chase after the bow's knot from beyond the stern never slid down the hull: the check below proves nothing");
            yield return Chase(false,(a,s)=>{there=a;scraped=s;});
            Assert.IsTrue(there,"from beyond barge three's stern, the chase never had the bow's knot in reach");
            Assert.LessOrEqual(scraped,2,$"from beyond barge three's stern, the chase after the bow's knot scraped the hull {scraped} times");
        }

        // ------------------------------------------------------------------ the whole stretch, second by second
        sealed class Account
        {
            public float lastSample=-1,fightFrom=-1,lastBlowOnSkimmers=-1,longestQuiet;
            public readonly Dictionary<Health,float> landed=new();public readonly HashSet<Health> hooked=new();
            public int shots,lastSequence=-1;public readonly List<string> lines=new();
        }
        static string Between(Vector3 from,Vector3 to)
        {
            if(CombatCover.Clear(from,to))return "clear";
            return CombatCover.Sweep(from,to,.01f,out var hit)?"blocked by "+hit.collider.name:"blocked";
        }
        void Watch(Account a)
        {
            var party=PartyController.Current;if(party==null||party.Active==null)return;var lead=party.Active;
            foreach(var body in Health.All)
            {
                if(body==null||body.friendly||!a.hooked.Add(body))continue;
                var h=body;body.Damaged+=(_,packet,amount)=>{if(amount<=0)return;
                    if(packet.source==Lead.Health){a.landed[h]=(a.landed.TryGetValue(h,out float t)?t:0)+amount;}
                    if(h.id=="Podskimmer"&&a.fightFrom>=0)a.lastBlowOnSkimmers=Time.unscaledTime;};
            }
            if(lead.AttackSequence!=a.lastSequence){if(a.lastSequence>=0&&a.fightFrom>=0)a.shots+=lead.AttackSequence-a.lastSequence;a.lastSequence=lead.AttackSequence;}
            int index=Get<int>("stepIndex");var step=Get<QualityStep>("step");
            var fight=Fight("Barge3_Skimmers");
            if(a.fightFrom<0&&fight!=null&&fight.Started){a.fightFrom=Time.unscaledTime;a.lastBlowOnSkimmers=Time.unscaledTime;}
            float now=Time.unscaledTime;
            if(a.fightFrom>=0&&!Flag("clear.Barge3_Skimmers")){float quiet=now-a.lastBlowOnSkimmers;if(quiet>a.longestQuiet)a.longestQuiet=quiet;}
            if(a.fightFrom<0||now-a.lastSample<1||Flag("clear.Barge3_Skimmers")&&Flag("clear.Barge3_Knots"))return;
            a.lastSample=now;
            var eye=lead.transform.position+Vector3.up*.9f;
            string target="none";
            if(lead.target!=null)
            {
                var t=lead.target;var off=t.transform.position-lead.transform.position;off.y=0;
                target=$"{t.id}@({t.transform.position.x:0.0},{t.transform.position.z:0.0}) {off.magnitude:0.0}m nose{Vector3.Angle(lead.motor.Facing,off):0} {(t.Alive?"":"dead ")}{(lead.TargetLocked?"locked":"held")} {Between(eye,t.transform.position+Vector3.up*.75f)} landed={(a.landed.TryGetValue(t,out float got)?got:0):0}";
            }
            var bodies=Health.All.Where(h=>h!=null&&!h.friendly&&h.Alive&&(h.id=="Podskimmer"||h.id=="SnagvineNode")&&Flat(h.transform.position,lead.transform.position)<45).Select(h=>
            {
                var brain=h.GetComponent<EnemyBrain>();var off=h.transform.position-lead.transform.position;off.y=0;
                string sees=brain!=null&&h.id=="Podskimmer"?Between(h.transform.position+Vector3.up,lead.transform.position+Vector3.up*.9f):"";
                return $"{h.id}@({h.transform.position.x:0.0},{h.transform.position.z:0.0}) {off.magnitude:0.0}m {StateOf(brain)}{(brain!=null&&brain.Passive?" passive":"")}{(h.Targetable?"":" untargetable")} sees:{sees} hp={h.integrity:0}/{h.maximum:0}";
            });
            var partner=party.members.FirstOrDefault(m=>m!=lead);
            a.lines.Add($"{now-a.fightFrom:0} s step={index} '{(step!=null?step.name:"")}' lead={lead.character} ({lead.transform.position.x:0.00},{lead.transform.position.z:0.00}) {lead.State} {lead.Health.integrity:0} shots={a.shots} target={target} | partner={(partner!=null?$"{partner.character} ({partner.transform.position.x:0.0},{partner.transform.position.z:0.0}) {partner.State} {partner.Health.integrity:0}":"-")} | {string.Join("; ",bodies)}");
        }
        void Dump(string label,Account a)
        {
            TestContext.WriteLine("==== "+label);
            foreach(var line in a.lines)TestContext.WriteLine("BARGE3_SAMPLE "+line);
            foreach(var line in log)TestContext.WriteLine("LOG "+line);
            foreach(var line in errors.Distinct().Take(30))TestContext.WriteLine("ERROR "+line);
            foreach(var f in Failures)TestContext.WriteLine("STEP_FAILED "+f);
            log.Clear();errors.Clear();
        }
        /// <summary>How long the skimmers' fight may go with no blow landing on any of them, through the pad: the six plays before the fix that didn't stop ran 3.2 to 8.4 s.</summary>
        const float QuietMost=20;
        /// <summary>Route 17 from the third swarm's end, past Teo's sight of the claim drones, through both of barge three's fights to the delta beacon, three times through the pad. Before the fix two
        /// plays in six stopped at the skimmers (88 s with no blow landing); each play must now clear both fights, fail no step and never leave the skimmers 20 s without a blow.</summary>
        [UnityTest,Timeout(1500000)] public IEnumerator BargeThreesFightsAreFlownThroughThePadToTheDeltaBeacon()
        {
            var plays=new List<string>();var faults=new List<string>();
            for(int n=1;n<=3;n++)
            {
                yield return AtTheThirdSwarmsEnd();
                var a=new Account();began=Time.unscaledTime;
                yield return Play(Slice(From,Lights),330,()=>Watch(a),()=>Ui()=="defeat");
                string said=$"play {n}: skimmers={Flag("clear.Barge3_Skimmers")} knots={Flag("clear.Barge3_Knots")} beacon={Flag("xylos.bargesFree")} finished={finished} longestQuiet={a.longestQuiet:0.0}s shots={a.shots} ui={Ui()} " +
                    $"comesRound={log.Count(l=>l.Contains("STRAFER_COMES_ROUND"))} breaksOut={log.Count(l=>l.Contains("STRAFER_BREAKS_OUT"))} coverFlicks={log.Count(l=>l.Contains("QUALITY_COVER_FLICK"))} coverAside={log.Count(l=>l.Contains("QUALITY_COVER_ASIDE"))} failures={string.Join(" | ",Failures)}";
                plays.Add(said);TestContext.WriteLine("BARGE3_PLAY "+said);
                if(Ui()=="defeat")faults.Add($"play {n}: the party fell");
                if(!Flag("clear.Barge3_Skimmers")||!Flag("clear.Barge3_Knots"))faults.Add($"play {n}: barge three's fights weren't both cleared");
                if(Failures.Count>0)faults.Add($"play {n}: a step failed: "+string.Join(" | ",Failures));
                if(a.longestQuiet>QuietMost)faults.Add($"play {n}: the skimmers went {a.longestQuiet:0} s with no blow landing on any of them");
                Dump("play "+n,a);
            }
            Assert.IsEmpty(faults,string.Join("\n",faults)+"\n"+string.Join("\n",plays));
        }
    }
}
