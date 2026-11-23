using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The partner is never a solid to the lead (D354). In one evening she blocked him on a Picket leg, at the Tollkeeper's gate motor, at a regroup, and then in the Orrery: in the
    /// whole-game capture of November 22 (nov22-whole s42c, three plays of three) the lead waited thirteen seconds in the Steward's lee at its middle gate with Idra parked 4.3 m behind his nose,
    /// in the mouth of the gate he was waiting to go through, and when it went green he opened up to full speed and stayed where he was for five seconds, sliding along her hull, until the leg
    /// timed out. Each time a rule about when she gives way had been added for the last place. Here the lead goes through her: at the Steward's gate as the capture had them, and driven straight
    /// through where she rests, in flight and on foot, with her brain running and with her held there, as fast and as straight as with nobody there at all.</summary>
    public sealed class PartnerNeverSolidTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;bool finished;readonly List<string> log=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        void Logged(string text,string stack,LogType type){if(text.StartsWith("QUALITY_")||text.StartsWith("WALL_CONTACT"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            fixture=new GameObject("Partner never solid fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
            log.Clear();finished=false;Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(fixture!=null)Object.Destroy(fixture);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the driver, by its private parts (as PartnerFlightRejoinTests drives it)
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        List<string> Failures=>Get<List<string>>("failures");
        IEnumerator Play(QualityRoute route,float most,System.Action each=null)
        {
            if(Get<Gamepad>("pad")==null)Set("pad",InputSystem.AddDevice<Gamepad>("PartnerNeverSolidPad"));
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

        // ------------------------------------------------------------------ routes and fixtures
        static string Folder=>System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
        static QualityRoute Read(string route)=>JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(System.IO.Path.Combine(Folder,route+".json")));
        /// <summary>The route's steps from the first named <paramref name="from"/> after the step named <paramref name="after"/> (a name can come twice: the Steward's lee is waited in
        /// at steps 218 and 221) to the next named <paramref name="to"/>.</summary>
        static QualityRoute Slice(string route,string scene,string after,string from,string to)
        {
            var whole=Read(route);int past=System.Array.FindIndex(whole.steps,s=>s.name==after);Assert.GreaterOrEqual(past,0,$"fixture: {route} has no step '{after}'");
            int a=System.Array.FindIndex(whole.steps,past,s=>s.name==from);Assert.GreaterOrEqual(a,0,$"fixture: {route} has no step '{from}' after '{after}'");
            int b=System.Array.FindIndex(whole.steps,a,s=>s.name==to);Assert.GreaterOrEqual(b,a,$"fixture: {route} has no step '{to}' after '{from}'");
            whole.steps=whole.steps.Skip(a).Take(b-a+1).ToArray();whole.scene=scene;whole.name+=" from "+from;return whole;
        }
        static IEnumerable<string> FlagsOf(QualityStep s)
        {
            if(!string.IsNullOrEmpty(s.expectedFlag))yield return s.expectedFlag;
            if(!string.IsNullOrEmpty(s.until)&&s.until.StartsWith("flag:"))yield return s.until.Substring(5);
        }
        static void FlagsAt(string route,string after,string step)
        {
            var steps=Read(route).steps;int past=System.Array.FindIndex(steps,s=>s.name==after);int at=past<0?-1:System.Array.FindIndex(steps,past,s=>s.name==step);
            Assert.GreaterOrEqual(at,0,$"fixture: {route} has no step '{step}' after '{after}'");
            foreach(string flag in steps.Skip(at).SelectMany(FlagsOf))GameServices.Current.Flags.SetBool(flag,false);
            foreach(string flag in steps.Take(at).SelectMany(FlagsOf))GameServices.Current.Flags.SetBool(flag,true);
        }
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.FirstOrDefault(m=>m!=Lead&&m.gameObject.activeInHierarchy);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static Vector3 Planar(Vector3 v){v.y=0;return v;}
        static float OffTheLine(Vector3 p,Vector3 a,Vector3 b){var d=new Vector2(b.x-a.x,b.z-a.z);var q=new Vector2(p.x-a.x,p.z-a.z);float t=Mathf.Clamp01(Vector2.Dot(q,d)/Mathf.Max(.0001f,d.sqrMagnitude));return (q-d*t).magnitude;}
        static void Put(CombatActor actor,Vector3 at,Vector3 facing)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(facing);}
            else if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(facing);}
            actor.transform.SetPositionAndRotation(at,Quaternion.LookRotation(Planar(facing).normalized,Vector3.up));body.enabled=true;Physics.SyncTransforms();
        }
        IEnumerator Zone(string loadout,string zone,string leader,string partner)
        {
            DevLoadout.Apply(loadout);
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(1f);
            for(int i=0;i<4&&Lead.character!=leader;i++){PartyController.Current.Swap();for(int f=0;f<30;f++)yield return null;}
            Assert.AreEqual(leader,Lead.character,"fixture: "+leader+" leads");Assert.AreEqual(partner,Partner.character,"fixture: "+partner+" is out beside him");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var hero in PartyController.Current.members)
            {
                hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=GameTime.Now+900;hero.target=null;hero.TargetLocked=false;
                hero.GetComponent<PlayerBrain>().AutoPilot=false;hero.GetComponent<PartnerBrain>().enabled=hero!=Lead;
            }
            yield return null;
        }

        // ================================================================== the Orrery: the Steward's middle gate (route 42, steps 221 and 222)
        const string Route42="chapter-42-oriel-errands",NamedRing="through The Steward's named ring on green, slowly",StewardsLee="wait in The Steward's lee at its middle gate";
        /// <summary>Where the capture had them through the wait (nov22-whole s42c, frames from 334.6 s): the lead at the waiting place, his nose south (he had come through the named ring), and Idra
        /// at rest 4.3 m behind it, in the mouth of the middle gate (the gate row's solid stands at z 150.9).</summary>
        static readonly Vector3 LeadWaits=new(0,0,146.56f),IdraParked=new(-2,0,150.83f);
        /// <summary>Route 42's wait in the Steward's lee and the walk through its middle gate on green, through the pad, from where the capture had the pair: the lead has to go through on green in
        /// the step's own 5.8 seconds. In the capture he stayed at z 147.3 for five seconds at full speed, 4.0 m from her middle, and the leg timed out (three plays of three).</summary>
        [UnityTest,Timeout(600000)] public IEnumerator AtTheStewardsMiddleGateTheLeadGoesThroughOnGreenWithIdraParkedInItsMouth()
        {
            FlagsAt(Route42,NamedRing,StewardsLee);
            ContinuousReview.ApplyPair(new QualityRoute{name="the Orrery",scene="Orrery",deployed=new[]{"Taren","Idra"}});
            yield return Zone("chapter7","Orrery","Taren","Idra");
            Assert.IsTrue(Lead.flight,"fixture: the lead flies in the Orrery");
            float plane=Lead.transform.position.y;var lead=LeadWaits;lead.y=plane;var idra=IdraParked;idra.y=plane;
            var partner=Partner;Put(Lead,lead,Vector3.back);Put(partner,idra,Vector3.back);
            yield return new WaitForSecondsRealtime(.8f);
            Assert.Less(Flat(partner.transform.position,IdraParked),.6f,$"fixture: Idra doesn't stay parked where the capture had her (she is at {partner.transform.position:F2})");
            var route=Slice(Route42,"Orrery",NamedRing,StewardsLee,"through The Steward's middle gate on green");
            Assert.AreEqual(2,route.steps.Length,"fixture: the slice is the wait and the walk through the gate");
            Vector3? atGreen=null,herAtGreen=null;float stuckFor=0,stuckFrom=-1;var stalls=new List<string>();var samples=new System.Text.StringBuilder();float note=0;
            yield return Play(route,80,()=>
            {
                if(Get<int>("stepIndex")<1)return;
                if(!atGreen.HasValue){atGreen=Lead.transform.position;herAtGreen=partner.transform.position;}
                // Opened up and going nowhere: what the capture had for five seconds.
                float speed=Planar(Lead.motor.Velocity).magnitude,now=Time.unscaledTime;
                if(speed>6&&Lead.GetComponent<CharacterController>().velocity.magnitude<1){if(stuckFrom<0)stuckFrom=now;stuckFor=Mathf.Max(stuckFor,now-stuckFrom);}else stuckFrom=-1;
                if(now>=note){note=now+.2f;samples.Append($" lead ({Lead.transform.position.x:0.00},{Lead.transform.position.z:0.00}) v={speed:0.0} | Idra ({partner.transform.position.x:0.00},{partner.transform.position.z:0.00}) {partner.State} apart={Flat(Lead.transform.position,partner.transform.position):0.00};");}
            });
            string told=$"the lee came with the lead at {(atGreen.HasValue?atGreen.Value.ToString("F2"):"never")} and Idra at {(herAtGreen.HasValue?herAtGreen.Value.ToString("F2"):"?")}; finished={finished} step={Get<int>("stepIndex")} "+
                $"lead ends at {Lead.transform.position:F2}; at full speed and going nowhere for {stuckFor:0.0} s; failures={string.Join(" | ",Failures)}; {string.Join(" | ",log.Where(l=>l.Contains("WALL_CONTACT "+Lead.name)||l.StartsWith("QUALITY_FLIES")))}";
            TestContext.WriteLine("STEWARDS_GATE "+told+"\n"+samples);
            Assert.IsTrue(atGreen.HasValue,"fixture: the Steward's lee never came: "+told);
            Assert.Less(Flat(herAtGreen.Value,IdraParked),1,"fixture: when the gate went green Idra was not parked in its mouth as the capture had her: "+told);
            Assert.IsTrue(finished,"the walk through the Steward's middle gate never finished: "+told);
            CollectionAssert.IsEmpty(Failures,"with Idra parked in the gate's mouth the lead didn't go through the Steward's middle gate on green in the step's own time: "+told);
            Assert.Less(stuckFor,.3f,"opened up to full speed, the lead went nowhere: "+told);
        }

        // ================================================================== the lead driven straight through where she rests
        /// <summary>Facing so that her place (3 m behind his nose, 2 m to his right) is due north of him, 3.6 m off; he then goes due north, through it.</summary>
        static readonly Vector3 Facing=new(-.5547f,0,-.832f);
        sealed class Drive
        {
            public bool arrived;public float took,offLine,slowest=float.PositiveInfinity,closest=float.PositiveInfinity;public int touched;public Vector3 end;public string scraped="";
            public readonly System.Text.StringBuilder path=new();
            public override string ToString()=>$"{(arrived?"arrived":"never arrived, ended at "+end.ToString("F2"))} in {took:0.000} s, {offLine:0.000} m off his line at most, {touched} frames touching something side on, the middles {closest:0.00} m apart at the nearest{scraped}";
        }
        /// <summary>The stick pushed straight at the point every frame through the motor's own input, as the driver's walk does, until the lead is within half a metre of it.</summary>
        static IEnumerator Go(CombatActor lead,CombatActor partner,Vector3 to,float seconds,Drive drive)
        {
            var from=lead.transform.position;var hull=lead.GetComponent<CharacterController>();float began=Time.time,deadline=Time.unscaledTime+seconds+5,note=0;
            float scrapeBefore=lead.motor is FlightMotor ship?ship.LastScrape:0;
            while(Time.time-began<seconds&&Time.unscaledTime<deadline)
            {
                var gap=Planar(to-lead.transform.position);
                if(gap.magnitude<=.5f){drive.arrived=true;break;}
                var stick=gap.normalized;lead.motor.Move(new Vector2(stick.x,stick.z),false,false);lead.MoveIntent=stick;
                if((hull.collisionFlags&CollisionFlags.Sides)!=0)drive.touched++;
                float t=Time.time-began;
                if(t>=note){note+=.1f;var p=lead.transform.position;var q=partner.transform.position;drive.path.Append($" t={t:0.0} lead=({p.x:0.00},{p.z:0.00}) {partner.character}=({q.x:0.00},{q.z:0.00}) {partner.State};");}
                yield return null;
                drive.offLine=Mathf.Max(drive.offLine,OffTheLine(lead.transform.position,from,to));
                drive.closest=Mathf.Min(drive.closest,Flat(lead.transform.position,partner.transform.position));
            }
            drive.took=Time.time-began;drive.end=lead.transform.position;
            if(lead.motor is FlightMotor flown&&flown.LastScrape!=scrapeBefore)drive.scraped=", and the hull felt a scrape";
            lead.motor.Move(Vector2.zero,false,true);lead.MoveIntent=Vector3.zero;
        }
        static IEnumerator Still(CombatActor lead,float seconds)
        {
            float until=Time.time+seconds,deadline=Time.unscaledTime+seconds+5;
            while(Time.time<until&&Time.unscaledTime<deadline){lead.motor.Move(Vector2.zero,false,true);lead.MoveIntent=Vector3.zero;yield return null;}
        }
        /// <summary>Three drives from one place to one point: with the partner far off (the time and the line with nobody there), then with her at rest where her brain brings her, on his line, and
        /// then with her held where she rests (her brain off, as a partner down, rooted in a skill or bracing can't move). Each of the last two must arrive as fast as the first, as straight, and
        /// never touching anything side on.</summary>
        IEnumerator Through(string zone,float length)
        {
            yield return Zone("starter",zone,"Taren","Sela");
            var lead=Lead;var partner=Partner;var brain=partner.GetComponent<PartnerBrain>();lead.GetComponent<PlayerBrain>().AutoPilot=true;
            bool flight=lead.flight;float level=lead.transform.position.y;
            var start=new Vector3(0,level,flight?-16:-14);var place=start-Facing*3+Vector3.Cross(Vector3.up,Facing)*2;
            Assert.Less(Mathf.Abs(place.x-start.x),.01f,"fixture: her place is due north of him");
            // With nobody there.
            brain.enabled=false;Put(partner,start+new Vector3(40,0,-30),Facing);Put(lead,start,Facing);yield return Still(lead,.6f);
            var alone=new Drive();yield return Go(lead,partner,start+Vector3.forward*length,6,alone);
            TestContext.WriteLine($"THROUGH {zone} alone: {alone}");
            Assert.IsTrue(alone.arrived&&alone.touched==0&&alone.offLine<.05f,"fixture: with nobody there the lead doesn't go straight to his point: "+alone+alone.path);
            var faults=new List<string>();
            foreach(bool held in new[]{false,true})
            {
                Put(lead,start,Facing);
                if(held){Put(partner,place,Facing);brain.enabled=false;}
                else{Put(partner,place+Vector3.forward*2.5f,Facing);brain.enabled=true;}
                yield return Still(lead,held?.6f:2f);
                var rest=partner.transform.position;
                Assert.Less(Flat(rest,place),2,$"fixture: she doesn't rest near her place behind him ({rest:F2}, her place {place:F2})");
                Assert.Less(Planar(partner.motor.Velocity).magnitude,.6f,"fixture: she isn't at rest as he sets off");
                // Straight through her middle, wherever her brain brought her to rest.
                var way=Planar(rest-start).normalized;var to=start+way*length;
                var drive=new Drive();yield return Go(lead,partner,to,6,drive);
                string what=held?"held where she rests":"at rest where her brain brought her";
                TestContext.WriteLine($"THROUGH {zone} with Sela {what} at {rest:F2}: {drive}");
                if(!drive.arrived||drive.took>alone.took*1.04f+.1f||drive.offLine>.05f||drive.touched>0||drive.scraped!="")
                    faults.Add($"with Sela {what} ({Flat(rest,start):0.00} m ahead of him on his line) the lead {drive} (alone: {alone.took:0.000} s);"+drive.path);
                brain.enabled=false;Put(partner,start+new Vector3(40,0,-30),Facing);yield return null;
            }
            Assert.IsEmpty(faults,$"in {zone}, driven straight through where his partner rests, the lead is stopped, slowed or turned by her:\n"+string.Join("\n",faults));
        }
        [UnityTest,Timeout(300000)] public IEnumerator InFlightTheLeadFliesStraightThroughWhereHisPartnerRestsAsFastAsAlone()=>Through("Arena_Flight",22);
        [UnityTest,Timeout(300000)] public IEnumerator OnFootTheLeadWalksStraightThroughWhereHisPartnerRestsAsFastAsAlone()=>Through("Arena_Ground",14);
    }
}
