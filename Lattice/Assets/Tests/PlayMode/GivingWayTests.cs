using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>On a plain walk the lead was carried off his heading and left standing: three first attempts of the windowless play of routes 35 to 62 (nov14-recon35,
    /// November 15) stopped so, route 55's walk back from Bryn's chart, route 42's from the worn seat and route 56's first stretch of the trodden way, and the same steps slid
    /// the same way in the plays before that and happened to come free. Each began alike: the companion stood at her place (three metres behind the lead and two to his right)
    /// when he turned back past her, and she walked straight at him to get behind him again. The bodies met, and the lead slid along whatever he touched at his full pace
    /// in a fixed direction while facing his point: the motor turns its heading a step a frame and the wall took the step back off it every frame, so it kept going up to
    /// 150 degrees from the stick (off the island's edge in route 42, round the point in route 56); stopped head on, each pushed into the other for good. These walk each of the
    /// three as its route did, with the companion's own brain and the motor's own input (the driver's straight push at the point), and one bare body along a wall.</summary>
    public sealed class GivingWayTests
    {
        [UnitySetUp] public IEnumerator Boot(){GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);}
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameInput.Current!=null)GameInput.Current.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the routes, read
        static string Folder=>Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
        static QualityRoute Route(int number)
        {
            var file=Directory.GetFiles(Folder,$"chapter-{number:00}-*.json").FirstOrDefault();
            return file==null?null:JsonUtility.FromJson<QualityRoute>(File.ReadAllText(file));
        }
        /// <summary>Every flag the chapter's routes have set by the time this one reaches its step, as a chain arrives there.</summary>
        static string[] FlagsBefore(int first,int number,int step)
        {
            var flags=new List<string>();
            for(int n=first;n<=number;n++)
            {
                var route=Route(n);if(route==null)continue;
                for(int i=0;i<route.steps.Length&&(n<number||i<step);i++)
                {
                    var s=route.steps[i];
                    if(!string.IsNullOrEmpty(s.expectedFlag))flags.Add(s.expectedFlag);
                    if(!string.IsNullOrEmpty(s.until)&&s.until.StartsWith("flag:"))flags.Add(s.until.Substring(5));
                }
            }
            return flags.Distinct().ToArray();
        }

        // ------------------------------------------------------------------ the place and the bodies
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        static float Radius(CombatActor hero)=>hero.GetComponent<CharacterController>().radius;
        static float OffTheLine(Vector3 p,Vector3 a,Vector3 b){var d=new Vector2(b.x-a.x,b.z-a.z);var q=new Vector2(p.x-a.x,p.z-a.z);float t=Mathf.Clamp01(Vector2.Dot(q,d)/Mathf.Max(.0001f,d.sqrMagnitude));return (q-d*t).magnitude;}
        static IEnumerator Enter(string scene)
        {
            SceneFlow.Current.LoadZone(scene,"Arrival");float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,scene+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
        }
        /// <summary>A hero stood on the floor at a place, turned along a heading. A place off the floor at the lead's level is taken nearer him along the way back.</summary>
        static void Stand(CombatActor hero,Vector3 at,Vector3 facing,Vector3 fallback)
        {
            if(NavMesh.SamplePosition(at,out var floor,1f,NavMesh.AllAreas)&&Mathf.Abs(floor.position.y-at.y)<1)at=floor.position;
            else if(NavMesh.SamplePosition(fallback,out floor,1f,NavMesh.AllAreas))at=floor.position;
            var body=hero.GetComponent<CharacterController>();body.enabled=false;
            hero.transform.SetPositionAndRotation(at,Quaternion.LookRotation(Flat(facing),Vector3.up));body.enabled=true;
            // Her place is read off the motor's facing, not the body's turn: it has to face where he stands facing.
            if(hero.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Flat(facing));}
            Physics.SyncTransforms();
        }
        /// <summary>The lead standing: no stick, as the pad is let go.</summary>
        static IEnumerator Hold(CombatActor lead,float seconds)
        {
            float until=Time.time+seconds,deadline=Time.unscaledTime+seconds+5;
            while(Time.time<until&&Time.unscaledTime<deadline){lead.motor.Move(Vector2.zero,false,false);lead.MoveIntent=Vector3.zero;yield return null;}
        }

        // ------------------------------------------------------------------ the walk
        sealed class Trace
        {
            public bool arrived;public float took,against,offLine,closest=float.PositiveInfinity,ahead,across,fastestAgainst;public Vector3 start,end,partnerStart;
            public readonly System.Text.StringBuilder path=new();
        }
        /// <summary>The driver's walk (ContinuousReview, a step with "navigate" and no findWay): the stick pushed straight at the point every frame until the lead is within the
        /// tolerance, through the motor's own input. Read each frame: the motor's own measure of what he moved (the replay's reportedSpeed), against the stick he was given.</summary>
        static IEnumerator Drive(CombatActor lead,CombatActor partner,Vector3 to,float tolerance,float seconds,Trace trace)
        {
            var from=lead.transform.position;var way=Flat(to-from).normalized;var right=Vector3.Cross(Vector3.up,way);
            var rel=Flat(partner.transform.position-from);trace.start=from;trace.partnerStart=partner.transform.position;
            trace.ahead=Vector3.Dot(rel,way);trace.across=Vector3.Dot(rel,right);
            float radii=Radius(lead)+Radius(partner),began=Time.time,note=0,deadline=Time.unscaledTime+seconds+5;
            while(Time.time-began<seconds&&Time.unscaledTime<deadline&&lead.Health.Alive)
            {
                var gap=Flat(to-lead.transform.position);
                if(gap.magnitude<=tolerance){trace.arrived=true;break;}
                var stick=gap.normalized;
                lead.motor.Move(new Vector2(stick.x,stick.z),false,false);lead.MoveIntent=stick;
                var moved=Flat(lead.motor.Velocity);
                if(moved.magnitude>.5f)trace.against=Mathf.Min(trace.against,Vector3.Dot(moved,stick));
                if(Vector3.Dot(moved,stick)<-.5f)trace.fastestAgainst=Mathf.Max(trace.fastestAgainst,moved.magnitude);
                float t=Time.time-began;
                if(t>=note)
                {
                    note+=.1f;var p=lead.transform.position;var q=partner.transform.position;
                    trace.path.Append($" t={t:0.0} lead=({p.x:0.00},{p.y:0.00},{p.z:0.00}) v=({moved.x:0.0},{moved.z:0.0}) stick=({stick.x:0.00},{stick.z:0.00}) {partner.character}=({q.x:0.00},{q.z:0.00}) {partner.State};");
                }
                yield return null;
                trace.offLine=Mathf.Max(trace.offLine,OffTheLine(lead.transform.position,from,to));
                trace.closest=Mathf.Min(trace.closest,Flat(partner.transform.position-lead.transform.position).magnitude-radii);
            }
            trace.took=Time.time-began;trace.end=lead.transform.position;
            lead.motor.Move(Vector2.zero,false,false);lead.MoveIntent=Vector3.zero;
        }

        /// <summary>Her place as she follows (PartnerBrain): three metres behind the lead's facing and two to his right.</summary>
        static Vector3 Place(Vector3 lead,Vector3 facing){facing=Flat(facing).normalized;return lead-facing*3+Vector3.Cross(Vector3.up,facing)*2;}
        /// <summary>Where she may be standing when he sets off: at her place, or anywhere a metre round it (she stops 1.2 m short of it, from whichever side she came).</summary>
        static IEnumerable<Vector3> Starts(Vector3 place){yield return place;for(int i=0;i<8;i++)yield return place+Quaternion.Euler(0,i*45,0)*Vector3.forward;}

        /// <summary>A route's walk, played from where the route stood: the zone in the state the chapter's routes leave it, Taren leading and Idra out, the lead where the replay's
        /// frames have him as the walk began and facing as he faced (off the talk, or as the load stood him). Which side of her place she had come to rest on the frames can't say,
        /// and it decides whether the bodies meet head on (the first plays of the same walks slid as far and came free; these stopped): so the walk is made from each of nine of
        /// her standing places, with her brain deciding what she does once he moves. Every one must reach the point in the step's own seconds without being carried.</summary>
        IEnumerator Replay(string loadout,int first,int number,string scene,string stepName,Vector3 from,Vector3 facing)
        {
            DevLoadout.Apply(loadout);
            var route=Route(number);Assert.IsNotNull(route,$"fixture: no route {number}");
            int at=System.Array.FindIndex(route.steps,s=>s.name==stepName);Assert.GreaterOrEqual(at,0,$"fixture: route {number} has no step '{stepName}'");
            var step=route.steps[at];
            foreach(string flag in FlagsBefore(first,number,at))GameServices.Current.Flags.SetBool(flag,true);
            yield return Enter(scene);
            var party=PartyController.Current;
            if(party.Active.character!="Taren")Assert.IsTrue(party.Swap(),"fixture: Taren can't take the lead");
            var lead=party.Active;var partner=party.Partner;
            Assert.IsNotNull(partner,"fixture: nobody out beside the lead");Assert.AreEqual("Idra",partner.character,"fixture: the route is played with Taren and Idra out");
            lead.GetComponent<PlayerBrain>().AutoPilot=true;
            // Nothing here is about harm: the Terminator's dawn closes on a party stood still nine times over, and a fallen party stops the clock.
            foreach(var hero in party.members)hero.Health.Damageable=false;
            var faults=new List<string>();var all=new System.Text.StringBuilder();int inWay=0,walked=0;
            foreach(var start in Starts(Place(from,facing)))
            {
                Stand(lead,from,facing,from);
                if(!NavMesh.SamplePosition(start,out var floor,.6f,NavMesh.AllAreas)||Mathf.Abs(floor.position.y-lead.transform.position.y)>.6f)continue;
                Stand(partner,floor.position,facing,floor.position);
                // The camera comes to him, and she stands as her brain has her: at her place she waits for him.
                yield return Hold(lead,1.5f);
                var trace=new Trace();
                yield return Drive(lead,partner,step.point,step.tolerance,step.seconds,trace);
                walked++;if(trace.ahead>0&&trace.ahead<5&&Mathf.Abs(trace.across)<2.5f)inWay++;
                string told=$"Idra from {trace.partnerStart:F2} ({trace.ahead:F2} m ahead, {trace.across:F2} m across): Taren {(trace.arrived?"arrived":"ended at "+trace.end.ToString("F2"))} "+
                    $"after {trace.took:F2} s of {step.seconds:F2}, {trace.offLine:F2} m off his line at most, against his stick at {-trace.against:F2} m/s at most, the bodies within {trace.closest:F2} m";
                all.Append("\n  "+told);
                if(!trace.arrived||trace.against<-.5f||trace.offLine>=.7f)faults.Add(told+";"+trace.path);
            }
            string head=$"route {number} '{stepName}' ({scene}): Taren from {from:F2} facing {Flat(facing).normalized:F2} to {step.point:F2} (tolerance {step.tolerance})";
            Debug.Log("GIVING_WAY "+head+all+(faults.Count>0?"\nFAULTS:\n  "+string.Join("\n  ",faults):""));
            Assert.GreaterOrEqual(walked,6,"fixture: fewer than six of her standing places are on the floor: "+head+all);
            // The state every stop began in: she stood ahead of him, near his way, as he set off.
            Assert.GreaterOrEqual(inWay,walked/2,"fixture: she seldom stood on his way as he set off, so these aren't the walks that stopped: "+head+all);
            Assert.IsEmpty(faults,$"from {faults.Count} of her {walked} standing places the lead was carried off his heading or left short of his point (not arriving, moving against his stick faster than .5 m/s, or more than a body's width off his line): "+head+"\n  "+string.Join("\n  ",faults));
        }

        // Where the replay's frames have the lead as each walk began (nov14-recon35 s55 step 61, s42 step 128, s56 step 7), and facing as he faced.
        [UnityTest] public IEnumerator BackFromBrynsChartTheLeadWalksPastIdraToTheStairTop()=>
            Replay("chapter9",53,55,"ManyHands","down to the raft bay 1/9",new Vector3(-1.19f,3.68f,44.72f),new Vector3(-.03f,0,1));
        [UnityTest] public IEnumerator BackFromTheWornSeatTheLeadWalksPastIdraToTheLiftsNode()=>
            Replay("chapter7",38,42,"OrielSkyfields","back to the lift's high node",new Vector3(-60.78f,18.09f,156.51f),new Vector3(-.92f,0,.39f));
        [UnityTest] public IEnumerator OffTheTerminatorsArrivalTheLeadRunsPastIdraDownTheTroddenWay()=>
            Replay("chapter9",53,56,"Terminator","south along the trodden way 2",new Vector3(0,.12f,740),Vector3.forward);

        /// <summary>A bare body against a wall, on the motor alone: running along it with the stick a little into it keeps nearly its pace; with the stick turned back past the
        /// wall's line (135 degrees from where it slid) it goes no further the old way and slides the way the stick leans, at the stick's share along the wall.</summary>
        [UnityTest] public IEnumerator ABodyAgainstAWallGoesTheWayItsStickLeans()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(300,-.5f,300);floor.transform.localScale=new Vector3(120,1,120);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(302,1.5f,300);wall.transform.localScale=new Vector3(1,3,80);
            var root=new GameObject("along a wall",typeof(CharacterController),typeof(GroundMotor));
            var cc=root.GetComponent<CharacterController>();cc.radius=.35f;cc.height=1.9f;cc.center=Vector3.up*.95f;
            root.transform.position=new Vector3(301.1f,.08f,320);var motor=root.GetComponent<GroundMotor>();Physics.SyncTransforms();
            try
            {
                float top=motor.speed;
                // Along it, the stick twenty degrees into it.
                var shallow=new Vector3(Mathf.Sin(20*Mathf.Deg2Rad),0,-Mathf.Cos(20*Mathf.Deg2Rad));
                float start=Time.time,pace=0,touching=float.PositiveInfinity;
                while(Time.time-start<1){motor.Move(new Vector2(shallow.x,shallow.z),false,false);yield return null;if(Time.time-start>.6f){pace=Flat(motor.Velocity).magnitude;touching=Mathf.Min(touching,301.5f-.35f-root.transform.position.x);}}
                Assert.Less(touching,.12f,$"fixture: the body isn't against the wall ({root.transform.position:F2})");
                // Turned back past the wall's line: north-east, 135 degrees from the slide south.
                var back=new Vector3(Mathf.Sin(45*Mathf.Deg2Rad),0,Mathf.Cos(45*Mathf.Deg2Rad));
                float z0=root.transform.position.z,lowest=z0,along=0;start=Time.time;
                while(Time.time-start<.8f){motor.Move(new Vector2(back.x,back.z),false,false);yield return null;lowest=Mathf.Min(lowest,root.transform.position.z);along=motor.Velocity.z;}
                Debug.Log($"GIVING_WAY_WALL top={top:F2} shallowPace={pace:F2} touching={touching:F3} wentOnSouth={z0-lowest:F2} endVelocityZ={along:F2} at={root.transform.position:F2}");
                Assert.GreaterOrEqual(pace,top*.9f,"control: a body running along a wall with the stick a little into it sheds its pace");
                Assert.Less(z0-lowest,.3f,$"with the stick turned 135 degrees back, the body went on {z0-lowest:F2} m the way it had been sliding");
                Assert.That(along,Is.EqualTo(top*Mathf.Cos(45*Mathf.Deg2Rad)).Within(.6f),"the body doesn't slide the way the stick leans, at the stick's share along the wall");
            }
            finally{Object.Destroy(root);Object.Destroy(wall);Object.Destroy(floor);}
        }
    }
}
