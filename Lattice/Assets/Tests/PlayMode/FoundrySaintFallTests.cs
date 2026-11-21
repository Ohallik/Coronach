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
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The Foundry Saint's fall in the built press hall (docs/maps/SeparationWorks.md, "Boss arena contract"), with the heroes where the fight really leaves them: in the press's frame
    /// behind its collars, against its back under a collar whose arm is still raised, in the wedge between two fallen arms, up on a fallen arm where the rig's last blow is struck, at a fallen
    /// arm's foot, against the door's seal. As the press settles (its frame turns solid, its last three arms come down, the seal opens) nobody is left inside anything, on top of anything with no
    /// way down, or wedged: each hero then walks away as a person does (down off an arm, then by the hall's own paths, the partner's) to the lift home, or out through the door, and ends on the
    /// floor there. And in the fight itself, a hero who has stepped in behind the collars before their arms come down isn't shut in by them.
    /// Route 59 of the chain's reconnaissance (Builds/quality/nov14-recon25/s59) found both: Taren's Extra carried him into the press's frame behind collars 2 and 3 at 21.7 s, the two arms came
    /// down 3 s later and shut him in for the rest of the fight (the stick held toward a road's foot, Idle, 135 s), and when the press settled its box pushed him under arm 2's high end, where
    /// he never moved again, an autosave just written. The Saint is held passive and the heroes can't be hurt: this is about where they stand, not about its moves.
    /// Shown failing first (November 14, Builds/quality/focused/saintfall-broken2) on the press as route 59 played it, behind a switch since removed: five of the six, the in-fight case
    /// stuck at (-2.12, 193.64) as route 59's lead was, the partner wedged against the settled press under arm 5, and the others held against fallen arms the paths ran through.</summary>
    public sealed class FoundrySaintFallTests
    {
        const string Works="SeparationWorks";
        const float Floor=-6;
        /// <summary>Where a walk away ends: at the lift's prompt (it answers from 3.6 m on the plan), on the landing through the door, on the hall's floor in front of the press.</summary>
        static readonly Vector3 LiftStand=new(19.6f,Floor,208.6f),Landing=new(0,Floor,165),HallFloor=new(0,Floor,180);
        readonly List<string> log=new();
        void Heard(string text,string stack,LogType type){if(text.StartsWith("SAINT_")||text.StartsWith("BOSS_DOWN"))log.Add(text);}
        bool Said(string start)=>log.Any(l=>l.StartsWith(start));

        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter9");log.Clear();Application.logMessageReceived+=Heard;
            // The three critical joints worked and joint 2's press freed: the press hall's door is open to the party.
            foreach(var key in new[]{"home.meetingHeld","home.worksRead","home.joint1","home.joint2","home.joint3","clear.Works_ControlRig"})GameServices.Current.Flags.SetBool(key,true);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;if(DialogueSystem.Current!=null)DialogueSystem.Current.AutoAdvance=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.Partner;
        static CombatActor[] Heroes=>new[]{Lead,Partner};
        static Vector3 Plan(Vector2 p)=>new(p.x,Floor,p.y);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;actor.target=null;actor.TargetLocked=false;Physics.SyncTransforms();
        }
        /// <summary>One frame of every hero's motor, as their brains drive it each frame: still, but for the one walking. A body's collisions are only resolved by a motor that moves it
        /// (route 59's lead was pushed out of the settled press by his own idle stance, and back into it by his first step).</summary>
        static void Drive(CombatActor walker,Vector3 heading)
        {
            foreach(var hero in Heroes)
            {
                if(hero==null)continue;var way=hero==walker?heading:Vector3.zero;way.y=0;
                hero.motor.Move(way.sqrMagnitude>.0001f?new Vector2(way.x,way.z).normalized:Vector2.zero,false,false);
            }
        }
        static IEnumerator Hold(float seconds,CombatActor walker=null,System.Func<Vector3> heading=null,System.Func<bool> until=null)
        {
            for(float end=Time.time+seconds,cap=Time.unscaledTime+seconds*2+2;Time.time<end&&Time.unscaledTime<cap&&(until==null||!until());){Drive(walker,heading!=null?heading():Vector3.zero);yield return null;}
        }
        /// <summary>The solid things a hero's body touches where he stands, his own aside: what holds him, for the message.</summary>
        static string Touching(CombatActor hero)
        {
            var body=hero.GetComponent<CharacterController>();var at=hero.transform.position;float r=body.radius+body.skinWidth+.05f;
            var bottom=at+body.center+Vector3.down*(body.height*.5f-body.radius)+Vector3.up*.12f;var top=at+body.center+Vector3.up*(body.height*.5f-body.radius);
            var names=Physics.OverlapCapsule(bottom,top,r,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(hero.transform)).Select(c=>c.name).Distinct().ToArray();
            return names.Length==0?"nothing":string.Join(", ",names);
        }

        BossController saint;Health saintBody;EncounterVolume door;
        /// <summary>The press hall's fight begun from its door (both heroes three metres in), the Saint woken with the hall's six arms, then held still; the heroes can't be hurt, and every
        /// conversation reads itself through.</summary>
        /// <summary>The Works loaded at the press hall's checkpoint, the heroes the test's to move.</summary>
        static IEnumerator Enter()
        {
            SceneFlow.Current.LoadZone(Works,"PressHall");float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Works+" never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}
            yield return new WaitForSecondsRealtime(.6f);
        }
        IEnumerator Fight()
        {
            yield return Enter();
            door=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="FoundrySaint");
            Put(Lead,Plan(new(-1,178)));Put(Partner,Plan(new(1.4f,178)));
            yield return Hold(4,until:()=>door.Started);Assert.IsTrue(door.Started,"fixture: the Saint's fight begins with both heroes three metres into the hall");
            yield return Hold(4,until:()=>(saint=Object.FindObjectsByType<BossController>(FindObjectsSortMode.None).FirstOrDefault(b=>b.GetComponent<Health>().id=="FoundrySaint"))!=null&&saint.SaintArms.Count==6);
            Assert.IsTrue(saint!=null&&saint.SaintArms.Count==6,"fixture: the Saint wakes with the hall's six arms");
            saintBody=saint.GetComponent<Health>();saint.GetComponent<EnemyBrain>().Passive=true;
            foreach(var hero in Heroes)hero.Health.InvulnerableUntil=GameTime.Now+900;
            DialogueSystem.Current.AutoAdvance=true;
        }
        SaintArm Arm(int index)=>saint.SaintArms.First(a=>a.index==index);
        /// <summary>These arms released as a player releases them (each collar unlocked and struck through), in this order, and come to rest as roads.</summary>
        IEnumerator Release(params int[] arms)
        {
            foreach(int index in arms)
            {
                var arm=Arm(index);if(arm.IsReleased)continue;arm.Unlock(3);
                Assert.Greater(arm.Collar.Receive(new DamagePacket{source=Lead.Health,amount=arm.Collar.maximum*20,type=DamageType.Pulse}),0,$"fixture: arm {index}'s unlocked collar takes the blow");
                yield return Hold(.05f);
            }
            yield return Hold(4,until:()=>arms.All(i=>Arm(i).Resting));
            foreach(int index in arms)Assert.IsTrue(Arm(index).Resting,$"fixture: arm {index} has come down");
        }
        /// <summary>The last blow, on the rig from up on the arms: the press settles, its last arms come down, and Orrin's talk on its speaker is read through.</summary>
        IEnumerator Fall()
        {
            saintBody.integrity=Mathf.Min(saintBody.integrity,10);
            saintBody.Receive(new DamagePacket{source=Lead.Health,amount=1000,type=DamageType.Kinetic,tag=Health.CutsPlate});
            Assert.IsFalse(saintBody.Alive,"fixture: the last blow fells the Saint");
            yield return Hold(6,until:()=>SaintArm.All.Where(a=>a!=null&&a.settledBy=="FoundrySaint").All(a=>a.Resting));
            Assert.IsTrue(Said("SAINT_SETTLES"),"fixture: the Saint settles");Assert.IsTrue(SaintArm.All.Where(a=>a!=null&&a.settledBy=="FoundrySaint").All(a=>a.Resting),"fixture: every arm lies still once it has settled");
            yield return Hold(30,until:()=>GameServices.Current.Flags.GetBool("home.saintReleased"));
            Assert.IsTrue(GameServices.Current.Flags.GetBool("home.saintReleased"),"fixture: Orrin's talk on the press's speaker plays through after the fall");
            Assert.IsTrue(door.membranes.All(m=>m==null||m.Open),"fixture: the press hall's seal opens at the fall");
            yield return Hold(.5f);
        }

        /// <summary>Where the fight leaves a hero: walked from a point of open floor toward another for a while, stopping where the room stops him (<see cref="early"/>: before the arms
        /// come down, as Taren's Extra carried him), or stood up on a fallen arm's road; and where he walks to afterwards.</summary>
        public sealed class Stand
        {
            public string what;public Vector2 from,toward;public float walk=1.2f;public int road;public float up;public bool early;public Vector3 then=LiftStand;public float within=2.4f;
        }
        static Stand Walked(string what,Vector2 from,Vector2 toward,bool early=false,float walk=1.2f)=>new(){what=what,from=from,toward=toward,walk=walk,early=early};
        static Stand Climbing(string what,int road,float up)=>new(){what=what,road=road,up=up};
        static Stand Behind(string what,float x)=>Walked(what,new(x*1.1f,191.6f),new(x,194.6f),true);
        IEnumerator Take(CombatActor hero,Stand stand)
        {
            if(stand.road>0)
            {
                var road=Arm(stand.road);Put(hero,road.RoadPoint(stand.up)+Vector3.up*.08f);yield return Hold(.4f);
                Assert.IsTrue(road.OnRoad(hero.transform.position)&&hero.transform.position.y-Floor>SaintArm.Tune.RoadTopHeight*stand.up-.4f,$"fixture: {hero.character} stands {stand.what} (at {hero.transform.position:F2})");
                yield break;
            }
            Put(hero,Plan(stand.from)+Vector3.up*.05f);yield return Hold(.2f);
            // Toward it until he is there or stopped by what stands in the way: walked on, he slid along the press's back and round its corner.
            var goal=Plan(stand.toward);yield return Hold(stand.walk,hero,()=>goal-hero.transform.position,()=>Flat(hero.transform.position,goal)<.75f);
            yield return Hold(.3f);
        }
        /// <summary>A hero walks away as a person does: down off a fallen arm he stands on, then by the hall's own paths (what the partner walks by) to where he is going, steering at the next
        /// corner. What stopped him, if anything, goes in <paramref name="faults"/>: stuck three seconds, not there in twenty-five, or not on the floor at the end.</summary>
        IEnumerator WalkAway(CombatActor hero,Stand stand,List<string> faults)
        {
            var start=hero.transform.position;string who=$"{hero.character} ({stand.what})";
            var on=SaintArm.All.FirstOrDefault(a=>a!=null&&a.Resting&&a.OnRoad(hero.transform.position)&&hero.transform.position.y-a.Floor>.5f);
            if(on!=null)yield return Hold(4,hero,()=>on.Outward,()=>hero.transform.position.y-Floor<.3f);
            var path=new NavMeshPath();Vector3[] corners=null;float traceAt=-1;string stuck=null;var trail=new List<(float t,Vector3 at)>();var trace=new List<string>();
            // The hero who walked first stands where he stopped, at the edge of where this one is going and now and then on her very line: in the whole suite of November 20 Taren
            // stopped 2.4 m short of the lift's stand, Idra was held .42 m behind him for three seconds, and that was counted as stuck where the fall left her. Up against a hero who
            // is himself there, she has arrived, as the second of two people at a lift has.
            bool Arrived(Vector3 at)=>Flat(at,stand.then)<=stand.within||Heroes.Any(h=>h!=null&&h!=hero&&Flat(h.transform.position,stand.then)<=stand.within&&Flat(h.transform.position,at)<=.9f);
            for(float end=Time.time+25,cap=Time.unscaledTime+60;Time.time<end&&Time.unscaledTime<cap;)
            {
                var at=hero.transform.position;if(Arrived(at))break;
                // The way afresh each frame, steered at the next corner after where he stands (or first onto the paths, from beside them). Kept for 0.4 s, the path's own start was
                // soon a stride behind him and he turned back for it every other moment.
                corners=NavMesh.SamplePosition(at,out var from,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(stand.then,out var to,2,NavMesh.AllAreas)&&
                    NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)&&path.status!=NavMeshPathStatus.PathInvalid?path.corners:null;
                var heading=stand.then-at;
                if(corners!=null&&corners.Length>0)
                {
                    var onto=corners[0]-at;onto.y=0;
                    if(onto.magnitude>1)heading=onto;
                    else for(int i=1;i<corners.Length;i++){var gap=corners[i]-at;gap.y=0;if(gap.magnitude>.3f){heading=gap;break;}}
                }
                if(Time.unscaledTime-traceAt>.5f)
                {
                    traceAt=Time.unscaledTime;
                    trace.Add($"t={Time.time:0.0} scale={Time.timeScale:0.00} at={at:F2} heading={heading.normalized:F2} corners={(corners!=null?corners.Length:0)} state={hero.State} alive={hero.Health.Alive} body={hero.GetComponent<CharacterController>().enabled} blocked={GameServices.Current.Input.Blocked} touching={Touching(hero)}");
                }
                // Gone less than half a metre in the last three seconds, however it shook on the spot: route 59's lead jumped half a metre each way and stayed.
                trail.Add((Time.time,at));while(trail.Count>1&&trail[0].t<Time.time-3)trail.RemoveAt(0);
                if(Time.time-trail[0].t>2.9f&&Flat(trail[0].at,at)<.5f){stuck=$"stuck at {at:F2}, touching {Touching(hero)}";break;}
                Drive(hero,heading);yield return null;
            }
            TestContext.WriteLine($"SAINT_FALL_TRACE {who}\n  "+string.Join("\n  ",trace));
            var last=hero.transform.position;
            TestContext.WriteLine($"SAINT_FALL_WALK {who} from {start:F2} to {last:F2}{(stuck!=null?" "+stuck:"")}");
            if(stuck!=null)faults.Add($"{who} can't walk away from {start:F2}: {stuck}");
            else if(!Arrived(last))faults.Add($"{who} never got from {start:F2} to {stand.then:F1} in 25 s (at {last:F2})");
            else if(Mathf.Abs(last.y-Floor)>.35f)faults.Add($"{who} walked to {stand.then:F1} but isn't on the floor there ({last:F2})");
        }

        // ------------------------------------------------------------------ in the fight
        /// <summary>Route 59 at 21.7 s: Taren's Extra carried him in behind collars 2 and 3, between the press's legs, and arms 3 and 2 came down 3 s later. He stood there, the stick held at
        /// a road's foot, for the rest of the fight. A hero who has stepped in behind the collars (either side of the press's middle) as three arms come down walks out onto the hall's floor.</summary>
        [UnityTest] public IEnumerator AHeroBehindTheCollarsIsNotShutInByTheArmsComingDown()
        {
            yield return Fight();
            var placed=new[]{(Lead,Behind("behind collars 2 and 3",-2f)),(Partner,Behind("behind collars 4 and 5",2f))};
            foreach(var (hero,stand) in placed){stand.then=HallFloor;stand.within=2;yield return Take(hero,stand);}
            var stood=placed.Select(p=>$"{p.Item1.character} {p.Item2.what} at {p.Item1.transform.position:F2}").ToArray();
            yield return Release(3,2,4);yield return Hold(.5f);
            var faults=new List<string>();
            foreach(var (hero,stand) in placed)yield return WalkAway(hero,stand,faults);
            Assert.IsEmpty(faults,"with arms 3, 2 and 4 down:\n"+string.Join("\n",faults)+"\nwhere they had stood: "+string.Join("; ",stood));
        }

        // ------------------------------------------------------------------ the fall
        /// <summary>Where the fight leaves the two heroes at the last blow, a pair a fall. Arms 3, 2 and 4 are down (route 59's order); 1, 5 and 6 come down as the press settles.</summary>
        static readonly Dictionary<string,(Stand lead,Stand partner)> Settles=new()
        {
            ["route 59: behind collars 2 and 3; against the back under collar 5"]=(Behind("in the press's frame behind collars 2 and 3",-2f),Walked("against the press's back under collar 5",new(3f,190.6f),new(2.6f,193.8f))),
            ["against the back under collars 1 and 6"]=(Walked("against the press's back under collar 1",new(-4.9f,190.8f),new(-4.4f,193.8f)),Walked("against the press's back under collar 6",new(4.9f,190.8f),new(4.4f,193.8f))),
            ["up on fallen arms 3 and 4 where the rig is struck"]=(Climbing("at the top of fallen arm 3",3,.95f),Climbing("at the top of fallen arm 4, beside arm 5",4,.95f)),
            ["at the top of fallen arm 2; at the foot of arm 4"]=(Climbing("at the top of fallen arm 2, beside arm 1",2,.97f),Climbing("at the foot of fallen arm 4",4,.15f)),
            ["in the wedge between arms 3 and 4; against the door's seal"]=(Walked("as far into the wedge between fallen arms 3 and 4 as a body goes",new(0,186),new(0,193.4f),false,1.6f),
                new Stand{what="against the door's seal, inside",from=new(1.5f,175.2f),toward=new(1.5f,170),walk=1,then=Landing,within=2}),
        };
        static IEnumerable<string> Falls()=>Settles.Keys;
        /// <summary>The press settles with the heroes where the fight left them: each then walks away, down off an arm and by the hall's paths, to the lift home (or out through the door), and
        /// ends on the floor there.</summary>
        [UnityTest] public IEnumerator AsThePressSettlesNobodyIsLeftWhereTheyCantWalkAway([ValueSource(nameof(Falls))] string fall)
        {
            var (leadStand,partnerStand)=Settles[fall];
            yield return Fight();
            var placed=new[]{(Lead,leadStand),(Partner,partnerStand)};
            // Off the press's back while arms they don't stand by come down; in first, whoever went in before them.
            foreach(var (hero,stand) in placed)if(!stand.early)Put(hero,Plan(hero==Lead?new Vector2(-12,181):new Vector2(12,181)));
            foreach(var (hero,stand) in placed)if(stand.early)yield return Take(hero,stand);
            yield return Release(3,2,4);Assert.IsTrue(saint.SaintBare,"fixture: three arms down and the rig bare");
            foreach(var (hero,stand) in placed)if(!stand.early)yield return Take(hero,stand);
            var stood=placed.Select(p=>$"{p.Item1.character} {p.Item2.what} at {p.Item1.transform.position:F2}").ToArray();
            yield return Fall();
            foreach(var (hero,stand) in placed)TestContext.WriteLine($"SAINT_FALL_STANDS {hero.character} ({stand.what}) at {hero.transform.position:F2}, touching {Touching(hero)}");
            var faults=new List<string>();
            foreach(var (hero,stand) in placed)yield return WalkAway(hero,stand,faults);
            Assert.IsEmpty(faults,"after the Saint settled:\n"+string.Join("\n",faults)+"\nwhere they stood at the last blow: "+string.Join("; ",stood));
        }

        // ------------------------------------------------------------------ after a reload
        /// <summary>The press's footprint on the plan (P, 10.1 by 4.9, facing north), a hand inside its faces: what no path may cross.</summary>
        static bool InPress(Vector3 p)=>Mathf.Abs(p.x)<5.05f-.05f&&p.z>196-2.45f+.05f&&p.z<196+2.45f-.05f;
        /// <summary>A save made after the fall loads the hall with the room's own settled press standing at P (the Saint's body went with its encounter) and every arm down. The hall's bake has
        /// floor under the press, so here too its footprint has to be out of the paths: from the door's side of the hall, south-west of the press, where the straight way to the lift runs
        /// through it, the partner's path to the lift (and the route's walk, which is the same path) goes round the press, and each hero walks it to the lift. Shown failing first on the
        /// scene built before the room's press carved (D316; Builds/quality/focused/saintfall-reload-before): both paths ran through it at (-5, 194.6).</summary>
        [UnityTest] public IEnumerator AfterAReloadThePathsGoRoundTheSettledPress()
        {
            foreach(var key in new[]{"bossdown.FoundrySaint","clear.FoundrySaint","home.saintReleased"})GameServices.Current.Flags.SetBool(key,true);
            yield return Enter();
            Assert.IsFalse(Object.FindObjectsByType<BossController>(FindObjectsSortMode.None).Any(b=>b.GetComponent<Health>().id=="FoundrySaint"),"fixture: the Saint's body stands in a hall loaded after its fall");
            var settled=Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene==SceneManager.GetActiveScene()&&t.name=="The Foundry Saint, settled").Select(t=>t.gameObject).Single();
            Assert.IsTrue(settled.activeInHierarchy,"fixture: the room's settled press stands at P after the fall");
            Assert.IsTrue(SaintArm.All.Where(a=>a!=null&&a.settledBy=="FoundrySaint").All(a=>a.Resting),"fixture: every arm lies still after the fall");
            yield return Hold(.5f);
            var placed=new[]{(Lead,new Stand{what="south-west of the press, west of arm 1",from=new(-9,191)}),(Partner,new Stand{what="south-west of the press, between the bolts and arm 1",from=new(-7.5f,186.5f)})};
            var faults=new List<string>();var path=new NavMeshPath();
            foreach(var (hero,stand) in placed)
            {
                Put(hero,Plan(stand.from)+Vector3.up*.05f);yield return Hold(.3f);
                Assert.IsTrue(NavMesh.SamplePosition(hero.transform.position,out var from,1.5f,NavMesh.AllAreas)&&NavMesh.SamplePosition(LiftStand,out var to,2,NavMesh.AllAreas)&&
                    NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,$"fixture: the hall's paths lead from {stand.from} to the lift");
                var corners=path.corners;Vector3? through=null;
                for(int i=1;i<corners.Length&&through==null;i++)for(float k=0,length=Flat(corners[i-1],corners[i]);k<=length;k+=.1f){var p=Vector3.Lerp(corners[i-1],corners[i],length>0?k/length:1);if(InPress(p)){through=p;break;}}
                TestContext.WriteLine($"SAINT_RELOAD_PATH {hero.character} from {stand.from}: "+string.Join(" ",corners.Select(c=>$"({c.x:0.0},{c.z:0.0})")));
                if(through is Vector3 inside)faults.Add($"the paths from {stand.what} to the lift run through the settled press, at {inside:F1}: the partner (and the route's walk) would walk into it");
            }
            foreach(var (hero,stand) in placed){Put(hero,Plan(stand.from)+Vector3.up*.05f);yield return Hold(.3f);yield return WalkAway(hero,stand,faults);}
            Assert.IsEmpty(faults,"in the hall reloaded after the fall:\n"+string.Join("\n",faults));
        }
    }
}
