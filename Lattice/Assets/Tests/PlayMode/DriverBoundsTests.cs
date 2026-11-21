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
    /// <summary>Where the replay driver walks of its own accord, and how far (ContinuousReview.Chase.cs: TowardFight, RaiseWithin), played through its own pad on the built zones:
    /// route 56's third coupling in the Terminator, re-mated with Idra down 160 m back up the run (the walk to raise her took the lead off the seat and into the dawn,
    /// reconnaissance s56, November 14); a fight step with the one awake enemy 55 m off and in no fight of the lead's, which he leaves alone; and route 35's third rank in the
    /// Frozen Host fought to its end. Each bound is shown failing first, in the same case, with the walks as they were first written (ContinuousReview.boundsOff).</summary>
    public sealed class DriverBoundsTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;bool finished;readonly List<string> log=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Heeded={"QUALITY_TOWARD_FIGHT","COUPLING_","DAWN_","COMBAT_KILL","ENCOUNTER_CLEAR","LEAPER_","AMBUSH_","QUALITY_CHASE","QUALITY_LOCK_FOR"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            fixture=new GameObject("Driver bounds fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
            log.Clear();finished=false;Application.logMessageReceived+=Logged;BoundsOff(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;BoundsOff(false);
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
        static void BoundsOff(bool off)=>typeof(ContinuousReview).GetField("boundsOff",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,off);
        IEnumerator Play(QualityRoute route,float most,System.Action each=null,System.Func<bool> enough=null)
        {
            if(Get<Gamepad>("pad")==null)Set("pad",InputSystem.AddDevice<Gamepad>("DriverBoundsPad"));
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

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=Lead);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship)ship.Halt();else if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static Vector3 Floor(Vector3 at){return UnityEngine.AI.NavMesh.SamplePosition(at,out var hit,4,UnityEngine.AI.NavMesh.AllAreas)?hit.position:at;}
        static IEnumerator Zone(string zone,string spawn)
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            yield return new WaitForSecondsRealtime(.8f);
        }
        static QualityRoute Route(string name)
        {
            string file=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes",name+".json");Assert.IsTrue(System.IO.File.Exists(file),"fixture: "+file);
            return JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));
        }
        static QualityStep[] Between(QualityRoute route,int first,int last)=>route.steps.Skip(first).Take(last-first+1).ToArray();
        static IEnumerator Leads(string hero)
        {
            for(int i=0;i<4&&Lead.character!=hero;i++){PartyController.Current.Swap();yield return Frames(30);}
            Assert.AreEqual(hero,Lead.character,"fixture: "+hero+" leads");
        }

        // ================================================================== the Terminator: coupling 3, with Idra down far behind
        /// <summary>Route 56 at its third coupling as reconnaissance s56 had it: couplings 1 and 2 mated, the lead come down the run to just north of coupling 3, Idra down where she fell on
        /// the run's twenty-ninth walk, 160 m back. Then the route's own steps 53 to 58: the last walk, the seat, the press and the hold, the walk to the engine car's board.</summary>
        IEnumerator CouplingThree(bool asItWas)
        {
            BoundsOff(asItWas);
            DevLoadout.Apply("chapter9");
            foreach(string flag in new[]{"home.crawlAsked","home.coupling1","home.coupling2"})GameServices.Current.Flags.SetBool(flag,true);
            GameServices.Current.Flags.SetBool("home.coupling3",false);
            ContinuousReview.ApplyPair(new QualityRoute{name="the Terminator",scene="Terminator",deployed=new[]{"Taren","Idra"}});
            yield return Zone("Terminator","Kitchens");yield return Leads("Taren");
            Lead.Health.InvulnerableUntil=GameTime.Now+900;
            Put(Lead,Floor(new Vector3(2,0,166)));Put(Partner,Floor(new Vector3(-1.2f,0,324.2f)));yield return Frames(5);
            Partner.Health.Receive(new DamagePacket{amount=100000,type=DamageType.Kinetic});yield return Frames(30);
            Assert.IsFalse(Partner.Health.Alive,"fixture: Idra is down");
            var route=Route("chapter-56-the-terminator");
            Assert.AreEqual("re-mate coupling 3",route.steps[56].name,"fixture: route 56's step 56 is coupling 3's press");
            // The route as it is played (its pouch and its EXTRA), from step 53.
            route.steps=Between(route,53,58);route.scene="Terminator";
            yield return Play(route,45);
            BoundsOff(false);
        }
        [UnityTest] public IEnumerator TheThirdCouplingIsReMatedWithIdraDownFarBackUpTheRun()
        {
            // As the driver was: the walk to raise her has no bound, and takes the lead off the seat back up the run.
            yield return CouplingThree(true);
            Assert.IsFalse(Flag("home.coupling3"),"fixture: with the walk to Idra unbounded the coupling was mated all the same: the check below proves nothing");
            log.Clear();
            yield return CouplingThree(false);
            CollectionAssert.IsEmpty(Failures,"route 56's steps 53 to 58 failed with Idra down far back: "+string.Join(" | ",log.Take(20)));
            Assert.IsTrue(Flag("home.coupling3"),"coupling 3 was never mated");
        }

        // ================================================================== a fight step with nothing near but one awake body far off
        const float Yard=-230;
        static Vector3 InYard(float x,float y,float z)=>new(Yard+x,y,z);
        IEnumerator FarDrone(bool asItWas,EnemyBrain[] drone)
        {
            BoundsOff(asItWas);
            DevLoadout.Apply("chapter8");
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+25;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Yard fixture floor";floor.transform.position=new Vector3(Yard,-.5f,0);floor.transform.localScale=new Vector3(260,1,260);Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.6f);
            Partner.GetComponent<PartnerBrain>().enabled=false;Put(Partner,InYard(60,.05f,60));
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+900;
            Put(Lead,InYard(0,.05f,0));((GroundMotor)Lead.motor).KeepFacing(Vector3.forward);Lead.target=null;Lead.TargetLocked=false;
            drone[0]=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("PourDrone"),InYard(0,.1f,-55));drone[0].transform.rotation=Quaternion.Euler(0,90,0);
            yield return Frames(5);
            yield return Play(new QualityRoute{name="far off",scene="Arena_Ground",steps=new[]{new QualityStep{name="a round with nothing near",seconds=10,buttons=new[]{"South"},pulseSeconds=.46f,
                navigate=true,approachTarget=true,tolerance=2.3f,rangedTolerance=12,evadeTelegraphs=true}}},15);
            BoundsOff(false);
        }
        [UnityTest] public IEnumerator AFightStepLeavesAnAwakeBodyBeyondAShortWalkAndOutsideItsFightAlone()
        {
            var drone=new EnemyBrain[1];
            // As the walk was first written: anything awake within 60 m. The drone is 55 m off, behind the lead, and in no fight: he walks off toward it.
            yield return FarDrone(true,drone);
            Assert.Greater(Flat(Lead.transform.position,InYard(0,0,0)),5f,"fixture: with the walk unbounded the lead never went toward the drone 55 m off: the check below proves nothing");
            log.Clear();
            yield return FarDrone(false,drone);
            Assert.Less(Flat(Lead.transform.position,InYard(0,0,0)),1.5f,"the lead walked off toward an enemy 55 m away in no fight of his: "+string.Join(" | ",log));
            Assert.IsFalse(log.Any(l=>l.StartsWith("QUALITY_TOWARD_FIGHT")),"the driver chose an enemy 55 m away in no fight of the lead's");
        }

        // ================================================================== the Frozen Host's third rank
        /// <summary>Route 35 at the third rank as reconnaissance s35 had it: the dig cleared to there, the lead come along the third level; then the route's own steps from the walk to rig
        /// three to "Dig_Rank3 cleared": the thaw, the lock, five rounds. What the lead has locked and where the rank's bodies are is written every two seconds.</summary>
        IEnumerator ThirdRank(bool asItWas)
        {
            ContinuousReviewHands(asItWas);
            DevLoadout.Apply("chapter6");
            foreach(string flag in new[]{"clear.Dig_Motes","clear.Dig_Rank","clear.Dig_Second","clear.Dig_Third","clear.Reach_Camp","clear.Reach_Stalkers","clear.Reach_Whiteout","hoar.herdMet",
                "hoar.herdPassed","hoar.offerHeard","hoar.ranksSeen","hoar.reachEntered","hoar.rig1","hoar.rig2","hoar.vessaHeard","hoar.whiteoutLifted","hoar.whiteoutMet","iru.lampsLit"})GameServices.Current.Flags.SetBool(flag,true);
            foreach(string flag in new[]{"hoar.rig3","clear.Dig_Rank3"})GameServices.Current.Flags.SetBool(flag,false);
            yield return Zone("FrozenHost","Arrival");yield return Leads("Taren");
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+900;
            Put(Lead,Floor(new Vector3(146,-13,224)));Put(Partner,Floor(new Vector3(143,-13,223)));yield return Frames(10);
            var route=Route("chapter-35-frozen-host");
            Assert.AreEqual("to rig three",route.steps[362].name,"fixture: route 35's step 362 is the walk to rig three");
            Assert.AreEqual("Dig_Rank3 cleared",route.steps[393].name,"fixture: route 35's step 393 is the rank's end");
            float next=0;
            route.steps=Between(route,362,393);route.scene="FrozenHost";
            yield return Play(route,200,()=>
            {
                if(Time.unscaledTime<next)return;next=Time.unscaledTime+2;
                var near=Health.All.Where(h=>h!=null&&!h.friendly&&h.Alive&&Flat(h.transform.position,Lead.transform.position)<30).Select(h=>
                    $"{h.name}@({h.transform.position.x:0.0},{h.transform.position.y:0.0},{h.transform.position.z:0.0}){(h.TryGetComponent<EnemyBrain>(out var b)?(b.enabled?"":" off")+(b.Passive?" passive":""):"")}{(h.Targetable?"":" untargetable")}");
                TestContext.WriteLine($"step {Get<int>("stepIndex")} lead ({Lead.transform.position.x:0.0},{Lead.transform.position.z:0.0}) target {(Lead.target!=null?Lead.target.name:"none")}: {string.Join(", ",near)}");
            });
            ContinuousReviewHands(false);
        }
        static void ContinuousReviewHands(bool off)=>typeof(ContinuousReview).GetField("chaseHandsOff",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,off);
        [UnityTest] public IEnumerator TheFrozenHostsThirdRankIsFoughtToItsEnd()
        {
            yield return ThirdRank(false);
            ContinuousReviewHands(false);
            CollectionAssert.IsEmpty(Failures,"route 35's third rank failed a step: "+string.Join(" | ",log.Take(30)));
            Assert.IsTrue(Flag("clear.Dig_Rank3"),"the third rank was never cleared: "+string.Join(" | ",log.Take(30)));
        }
    }
}
