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
    /// <summary>Chapter 6's replay routes (tools/hoarfell_routes.py), checked against the built zones: the driver walks and flies straight lines with no pathfinding, so every leg
    /// must cross navigation on foot and open space in flight. A boss's door step is checked from its checkpoint spawn as well, where a retry starts it. A route played with Idra
    /// out flies at her hull (2.17 m, the biggest). Each zone loads as the routes walk it: the chart for Hoarfell read, the Frozen Host's three rigs run (its plugs melted).</summary>
    public sealed class HoarfellRouteTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter6");
        }
        [UnityTearDown] public IEnumerator Cleanup(){GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.8f);
        }
        /// <summary>The flags the routes have set by the time they walk each zone's legs, set before it loads: the chart read (the hull's warp open), the town's talks before the
        /// Reach, and the three rigs run (the plugs over the dig's ramps melted, which the walk down goes through).</summary>
        static readonly string[] AsWalked={"hoar.charted","hoar.idraHailed","hoar.ribsSeen","hoar.ternHeard","hoar.pensSeen","hoar.planTaken","hoar.rig1","hoar.rig2","hoar.rig3"};
        static readonly string[] Routes={"chapter-32-aurora-shear","chapter-33-lastlight","chapter-34-lastlight-errands","chapter-35-frozen-host","chapter-36-host-marshal","chapter-37-long-sleep"};
        /// <summary>One straight leg: from where (unknown after a chase and at a zone's first leg in flight), to where, the boss whose door it is (checked again from that
        /// door's checkpoint spawn), and the hull it is flown with.</summary>
        sealed class Leg{public string route,name,scene,door;public Vector3? from;public Vector3 to;public float tolerance,hull;}
        static List<Leg> Legs()
        {
            var legs=new List<Leg>();string root=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
            float pair=Mathf.Max(HeroCollision.HullRadius("Taren"),HeroCollision.HullRadius("Sela"));
            foreach(string name in Routes)
            {
                string file=System.IO.Path.Combine(root,name+".json");Assert.IsTrue(System.IO.File.Exists(file),"fixture: "+file+" (python tools/hoarfell_routes.py writes it)");
                var route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));string scene=null;Vector3? at=null;
                float hull=route.deployed!=null&&route.deployed.Length>0?route.deployed.Max(h=>HeroCollision.HullRadius(h)):pair;
                foreach(var step in route.steps)
                {
                    if(step.expectedScene!=scene){scene=step.expectedScene;at=null;}
                    // Idra's tow (CutterTow) carries the ship from her hail to her lane by its own points, with no stick: the next leg starts where it lets go, not where the hail began.
                    if(step.until=="flag:hoar.towed")at=null;
                    if(!step.navigate)continue;
                    if(step.approachTarget||step.approachPartner){at=null;continue;}
                    legs.Add(new Leg{route=name,name=step.name,scene=scene,from=at,to=step.point,tolerance=step.tolerance,hull=hull,door=string.IsNullOrEmpty(step.bossDoor)?null:step.bossDoor});at=step.point;
                }
            }
            return legs;
        }
        /// <summary>How far a point lies off the straight line between two others, on the plan.</summary>
        static float OffTheLine(Vector3 p,Vector3 a,Vector3 b){var d=new Vector2(b.x-a.x,b.z-a.z);var q=new Vector2(p.x-a.x,p.z-a.z);float t=Mathf.Clamp01(Vector2.Dot(q,d)/Mathf.Max(.0001f,d.sqrMagnitude));return (q-d*t).magnitude;}
        static bool Flying(string scene){var zone=GameCatalog.Find<ZoneDef>(scene);return zone!=null&&(zone.kind==ZoneKind.SpaceCombat||zone.kind==ZoneKind.SpaceSafe);}
        /// <summary>Where a retry from a boss's door starts: the spawn its encounter names as its checkpoint.</summary>
        static Vector3? Checkpoint(string boss)
        {
            var volume=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(v=>v.name==boss);
            if(volume==null||string.IsNullOrEmpty(volume.checkpointSpawn))return null;
            var spawn=Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(s=>s.id==volume.checkpointSpawn);
            return spawn!=null?spawn.transform.position:null;
        }
        /// <summary>Every zone the chapter 6 routes walk or fly, one case each, so a zone's own run (focused.ps1 -Zones) reaches its legs.</summary>
        static readonly string[] LegZones={"ManyHands","ManyHandsHull","AuroraShear","Lastlight","FrozenHost"};
        [Test] public void TheChapterSixLegCheckHasACaseForEveryZoneItsRoutesWalk()
            =>CollectionAssert.AreEquivalent(Legs().Select(l=>l.scene).Distinct().ToArray(),LegZones,"a chapter 6 route walks a zone the leg check has no case for, or the other way round");
        [UnityTest] public IEnumerator EveryWalkOnTheChapterSixRoutesFollowsFloorABodyCanCross([ValueSource(nameof(LegZones))] string only)
        {
            var legs=Legs();Assert.IsNotEmpty(legs.Where(l=>l.scene==only).ToArray(),"fixture: the chapter 6 routes walk "+only);
            Assert.IsNotNull(GameCatalog.Find<ZoneDef>(only),"fixture: a route walks in "+only+", which is no zone");
            var faults=new List<string>();var mine=legs.Where(l=>l.scene==only).ToList();
            foreach(string flag in AsWalked)Flag(flag);
            yield return Zone(only);
            // A door step is walked again from its checkpoint by every retry: that is a leg of its own.
            foreach(var leg in mine.Where(l=>l.door!=null).ToArray())
            {
                var from=Checkpoint(leg.door);
                if(from==null){faults.Add($"{leg.route} '{leg.name}' ({only}) is {leg.door}'s door, and no encounter of that name here names a checkpoint spawn there is");continue;}
                mine.Add(new Leg{route=leg.route,name=leg.name+" (retried from "+leg.door+"'s checkpoint)",scene=only,from=from,to=leg.to,tolerance=leg.tolerance,hull=leg.hull});
            }
            if(!Flying(only))
            {
                var spawns=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
                foreach(var leg in mine)
                {
                    string where=$"{leg.route} '{leg.name}' ({only})";var start=leg.from;
                    // A zone's first walk starts at the spawn the route arrived at: the nearest, when one is near enough to be it.
                    if(start==null){var spawn=spawns.OrderBy(s=>Flat(s.transform.position,leg.to)).FirstOrDefault();if(spawn!=null&&Flat(spawn.transform.position,leg.to)<25)start=spawn.transform.position;}
                    if(!NavMesh.SamplePosition(leg.to+Vector3.up*.5f,out var end,2.5f,NavMesh.AllAreas)||Flat(end.position,leg.to)>Mathf.Max(.6f,leg.tolerance)){faults.Add($"{where} ends at {leg.to}, off the floor a body walks"+(NavMesh.SamplePosition(leg.to,out var nearest,6,NavMesh.AllAreas)?$" (the nearest floor is at {nearest.position})":""));continue;}
                    if(start==null)continue;
                    if(!NavMesh.SamplePosition(start.Value+Vector3.up*.5f,out var begin,2.5f,NavMesh.AllAreas)){faults.Add($"{where} starts at {start.Value}, off the floor");continue;}
                    if(!NavMesh.Raycast(begin.position,end.position,out var cut,NavMesh.AllAreas))continue;
                    var near=Physics.OverlapSphere(cut.position+Vector3.up*.6f,1.1f,~0,QueryTriggerInteraction.Ignore).Where(c=>!(c is CharacterController)&&c.GetComponentInParent<CombatActor>()==null&&Mathf.Max(c.bounds.size.x,c.bounds.size.z)<30)
                        .Select(c=>c.transform.root==c.transform?c.name:c.transform.root.name+"/"+c.name).Distinct().ToArray();
                    var path=new NavMeshPath();bool joined=NavMesh.CalculatePath(begin.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                    // The floor's own shortest way is the judge: if it is the straight line itself (no corner between the ends), the straight walk is on floor; anything standing on
                    // the line, or an edge it clips, bends that way.
                    // A corner the bake leaves where a plank's deck meets a raft lies on the straight line itself (within 10 cm): that path is the line, as one with no corner is.
                    if(joined&&path.corners.Skip(1).Take(path.corners.Length-2).All(c=>OffTheLine(c,begin.position,end.position)<.1f))continue;
                    float walked=0;for(int i=1;i<path.corners.Length;i++)walked+=Vector3.Distance(path.corners[i-1],path.corners[i]);
                    faults.Add($"{where}: the straight walk from {start.Value} to {leg.to} is cut at {cut.position}; within a metre there: {(near.Length>0?string.Join(", ",near):"nothing solid (an edge of the floor)")}; "
                        +(joined?$"the floor joins the ends by {walked:0.0} m against {Vector3.Distance(begin.position,end.position):0.0} straight, by {string.Join(" ",path.corners.Select(c=>$"({c.x:0.0},{c.z:0.0})"))}":"the floor doesn't join the ends at all"));
                }
            }
            else foreach(var leg in mine)
            {
                var to=leg.to;to.y=1;var met=Physics.OverlapSphere(to+Vector3.up*.8f,leg.hull,~0,QueryTriggerInteraction.Ignore).AsEnumerable();
                if(leg.from is Vector3 start){start.y=1;var way=to-start;if(way.magnitude>.01f)met=met.Concat(Physics.SphereCastAll(start+Vector3.up*.8f,leg.hull,way.normalized,way.magnitude,~0,QueryTriggerInteraction.Ignore).Select(h=>h.collider));}
                foreach(var c in met.Where(c=>c!=null&&!(c is CharacterController)&&c.GetComponentInParent<Health>()==null).Distinct())faults.Add($"{leg.route} '{leg.name}' ({only}, a {leg.hull:0.00} m hull) flies into {c.name} on the way to {leg.to}");
            }
            Assert.IsEmpty(faults,"the chapter 6 routes walk or fly where a body can't:\n"+string.Join("\n",faults.Distinct().Take(40)));
        }
    }
}
