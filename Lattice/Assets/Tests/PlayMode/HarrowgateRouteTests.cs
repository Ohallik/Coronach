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
    /// <summary>Chapter 8's replay routes (tools/harrowgate_routes.py), checked against the built zones: the driver walks and flies straight lines with no pathfinding, so every leg
    /// must cross navigation on foot and open space in flight. A boss's door step is checked from its checkpoint spawn as well, where a retry starts it. The routes are played with
    /// Taren and Idra out, so every flight leg is flown at Idra's hull (2.17 m, the bigger). Each zone loads as the routes walk it: the plan heard (the hull's warp open), the lock
    /// taken apart (its seal gone), both wheels turned (the tap runs crusted and their seals gone), the parley vouched (the harbour gate's leaves open), the verdict given (the armoury
    /// gate's grille open). And every wait a route makes on a room's clock ("until":"clear:...") is a way the leg after it walks or flies: the same points, in order.</summary>
    public sealed class HarrowgateRouteTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter8");
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
        /// <summary>The flags the routes have set by the time they walk each zone's legs, set before it loads.</summary>
        static readonly string[] AsWalked={"vask.brekkWord","vask.lockAdmitted","vask.picketRead","vask.picketCrossed","vask.lock1","vask.lock2","vask.lock3","vask.lock4","vask.lockRemoved",
            "vask.crustRead","vask.tongueCrossed","vask.garrisonSeen","vask.unswornMet","met.Tovra","vask.sluiceRead","vask.sluice1","vask.sluice2","vask.vessaMet","vask.cageSeen",
            "vask.juvenileFreed","vask.roadSaid","vask.sistersHeard","vask.parley","vask.pipMet","pip.fieldTalked","vask.gallerySeen","pip.galleryTalked","vask.casesOpen","vask.odrunMet"};
        static readonly string[] Routes={"chapter-44-harrowgate-way-in","chapter-45-picket-of-teeth","chapter-46-scoria-flows","chapter-47-scoria-errands","chapter-48-bridle-foundry",
            "chapter-49-slag-colossus","chapter-50-harrowgate","chapter-51-harrowgate-errands","chapter-52-the-warmaster"};
        /// <summary>The flags that change a zone's floor while the chapter is walked: the armoury gate's grille is shut, solid and carving, until the town has answered. A leg is walked in
        /// the zone as it stands with those of them its route has asked for by then (every step's expectedFlag before it), so the walk through the gate is judged with the gate open and
        /// every walk before the verdict with it shut.</summary>
        static readonly string[] FloorFlags={"vask.deposed"};
        /// <summary>One straight leg: from where (unknown after a chase, a zone's spawn at its first leg), to where, the boss whose door it is (checked again from that door's checkpoint
        /// spawn), the hull it is flown with, and which of <see cref="FloorFlags"/> are set as it is walked.</summary>
        sealed class Leg{public string route,name,scene,door,floor="";public Vector3? from;public Vector3 to;public float tolerance,hull;public bool afterChase;}
        static QualityRoute Read(string name)
        {
            string file=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes",name+".json");Assert.IsTrue(System.IO.File.Exists(file),"fixture: "+file+" (python tools/harrowgate_routes.py writes it)");
            return JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));
        }
        static List<Leg> Legs()
        {
            var legs=new List<Leg>();float hull=Mathf.Max(HeroCollision.HullRadius("Taren"),HeroCollision.HullRadius("Idra"));
            foreach(string name in Routes)
            {
                var route=Read(name);string scene=null;Vector3? at=null;bool chased=false;var seen=new HashSet<string>();
                foreach(var step in route.steps)
                {
                    if(!string.IsNullOrEmpty(step.expectedFlag))seen.Add(step.expectedFlag);
                    if(step.expectedScene!=scene){scene=step.expectedScene;at=null;chased=false;}
                    if(!step.navigate)continue;
                    if(step.approachTarget||step.approachPartner){at=null;chased=true;continue;}
                    legs.Add(new Leg{route=name,name=step.name,scene=scene,from=at,afterChase=at==null&&chased,to=step.point,tolerance=step.tolerance,hull=hull,door=string.IsNullOrEmpty(step.bossDoor)?null:step.bossDoor,
                        floor=string.Join(",",FloorFlags.Where(seen.Contains))});
                    at=step.point;chased=false;
                }
            }
            return legs;
        }
        static float OffTheLine(Vector3 p,Vector3 a,Vector3 b){var d=new Vector2(b.x-a.x,b.z-a.z);var q=new Vector2(p.x-a.x,p.z-a.z);float t=Mathf.Clamp01(Vector2.Dot(q,d)/Mathf.Max(.0001f,d.sqrMagnitude));return (q-d*t).magnitude;}
        static bool Flying(string scene){var zone=GameCatalog.Find<ZoneDef>(scene);return zone!=null&&(zone.kind==ZoneKind.SpaceCombat||zone.kind==ZoneKind.SpaceSafe);}
        /// <summary>Where a retry from a boss's door starts: the spawn its encounter names as its checkpoint.</summary>
        static Vector3? Checkpoint(string boss)
        {
            var volume=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(v=>v.encounterId==boss);
            if(volume==null||string.IsNullOrEmpty(volume.checkpointSpawn))return null;
            var spawn=Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(s=>s.id==volume.checkpointSpawn);
            return spawn!=null?spawn.transform.position:null;
        }
        static readonly string[] LegZones={"ManyHands","ManyHandsHull","PicketOfTeeth","ScoriaFlows","BridleFoundry","Harrowgate","HarrowgateHarbour"};
        [Test] public void TheChapterEightLegCheckHasACaseForEveryZoneItsRoutesWalk()
            =>CollectionAssert.AreEquivalent(Legs().Select(l=>l.scene).Distinct().ToArray(),LegZones,"a chapter 8 route walks a zone the leg check has no case for, or the other way round");
        /// <summary>A wait on a room's clock is for the way the route then takes: its points are the walks that follow it, in order, so the driver never waits on one way and walks another.
        /// Each route is read for it; and the routes of a room that times its floor (the Picket, the Flows, the Foundry) all wait somewhere.</summary>
        [Test] public void EveryWaitOnARoomsClockIsTheWayTheRouteThenTakes()
        {
            var faults=new List<string>();var waited=new HashSet<string>();
            foreach(string name in Routes)
            {
                var steps=Read(name).steps;
                for(int i=0;i<steps.Length;i++)
                {
                    if(string.IsNullOrEmpty(steps[i].until)||!steps[i].until.StartsWith("clear:"))continue;
                    waited.Add(steps[i].expectedScene);
                    var way=ReplayWays.Points(steps[i].until.Substring(6));
                    if(way.Count==0){faults.Add($"{name} '{steps[i].name}': its way '{steps[i].until}' reads as no way at all");continue;}
                    if(steps[i].navigate)faults.Add($"{name} '{steps[i].name}': a wait that steers the stick");
                    var walked=steps.Skip(i+1).Where(s=>s.navigate&&!s.approachTarget&&!s.approachPartner).Take(way.Count).Select(s=>new Vector2(s.point.x,s.point.z)).ToList();
                    for(int k=0;k<way.Count;k++)if(k>=walked.Count||Vector2.Distance(walked[k],way[k])>.01f)
                        {faults.Add($"{name} '{steps[i].name}': waits for the way through {way[k]}, and the walk after it goes to {(k<walked.Count?walked[k].ToString():"nothing")}");break;}
                }
            }
            foreach(string zone in new[]{"PicketOfTeeth","ScoriaFlows","BridleFoundry"})if(!waited.Contains(zone))faults.Add("no route waits on "+zone+"'s clock: it is crossed on luck");
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
        /// <summary>Each leg on the zone as loaded: a door step again from its checkpoint; on foot along the bake's floor (a zone's first walk from the spawn it arrives at, a walk after a chase
        /// from nowhere known, judged at its end), in flight through open space by the hull.</summary>
        static void Walk(string only,List<Leg> mine,List<string> faults)
        {
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
                    // A zone's first walk starts at the spawn the route arrived at: the nearest, when one is near enough to be it. A walk after a chase starts wherever the fight left it.
                    if(start==null&&!leg.afterChase){var spawn=spawns.OrderBy(s=>Flat(s.transform.position,leg.to)).FirstOrDefault();if(spawn!=null&&Flat(spawn.transform.position,leg.to)<25)start=spawn.transform.position;}
                    if(!NavMesh.SamplePosition(leg.to+Vector3.up*.5f,out var end,2.5f,NavMesh.AllAreas)||Flat(end.position,leg.to)>Mathf.Max(.6f,leg.tolerance)){faults.Add($"{where} ends at {leg.to}, off the floor a body walks"+(NavMesh.SamplePosition(leg.to,out var nearest,6,NavMesh.AllAreas)?$" (the nearest floor is at {nearest.position})":""));continue;}
                    if(start==null)continue;
                    if(!NavMesh.SamplePosition(start.Value+Vector3.up*.5f,out var begin,2.5f,NavMesh.AllAreas)){faults.Add($"{where} starts at {start.Value}, off the floor");continue;}
                    if(!NavMesh.Raycast(begin.position,end.position,out var cut,NavMesh.AllAreas))continue;
                    var near=Physics.OverlapSphere(cut.position+Vector3.up*.6f,1.1f,~0,QueryTriggerInteraction.Ignore).Where(c=>!(c is CharacterController)&&c.GetComponentInParent<CombatActor>()==null&&Mathf.Max(c.bounds.size.x,c.bounds.size.z)<30)
                        .Select(c=>c.transform.root==c.transform?c.name:c.transform.root.name+"/"+c.name).Distinct().ToArray();
                    var path=new NavMeshPath();bool joined=NavMesh.CalculatePath(begin.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
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
        }
        [UnityTest] public IEnumerator EveryWalkOnTheChapterEightRoutesFollowsFloorABodyCanCross([ValueSource(nameof(LegZones))] string only)
        {
            var legs=Legs();Assert.IsNotEmpty(legs.Where(l=>l.scene==only).ToArray(),"fixture: the chapter 8 routes walk "+only);
            Assert.IsNotNull(GameCatalog.Find<ZoneDef>(only),"fixture: a route walks in "+only+", which is no zone");
            var faults=new List<string>();
            foreach(string flag in AsWalked)Flag(flag);
            // The zone as it stands for each state of its floor its legs are walked in, fewest flags first (a flag set stays set).
            foreach(var state in legs.Where(l=>l.scene==only).GroupBy(l=>l.floor).OrderBy(g=>g.Key.Length))
            {
                foreach(string flag in state.Key.Split(',',System.StringSplitOptions.RemoveEmptyEntries))Flag(flag);
                yield return Zone(only);
                Walk(only,state.ToList(),faults);
            }
            Assert.IsEmpty(faults,"the chapter 8 routes walk or fly where a body can't:\n"+string.Join("\n",faults.Distinct().Take(40)));
        }
    }
}
