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
    /// <summary>Chapter 7's replay routes (tools/oriel_routes.py), checked against the built zones: the driver walks and flies straight lines with no pathfinding, so every leg
    /// must cross navigation on foot and open space in flight. A boss's door step is checked from its checkpoint spawn as well, where a retry starts it. The routes are played
    /// with Taren and Idra out, so every leg in flight is flown at Idra's hull (2.17 m). Each zone loads as the routes walk it: Ishen's crossing on the dock and the hull's warp
    /// to the Orrery open, the Skyfields' three junctions seated (their decks across), the Choir's three stays eased (their ribs up).</summary>
    public sealed class OrielRouteTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter7");
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
        /// <summary>The flags the routes have set by the time they walk each zone's legs, set before it loads: chapter 6 told and Ishen heard (his crossing on the dock, the hull's
        /// warp to the Orrery), the Skyfields' junctions seated (each deck across, each rim's rails gone), the Choir's release read and its three stays eased (each rib lifted
        /// and the membrane under it gone).</summary>
        static readonly string[] AsWalked={"chapter6.complete","oriel.charted","oriel.lightsRead","oriel.crossed","oriel.ruleSeen","oriel.gardenSeen","oriel.junctionRead",
            "oriel.junction1","oriel.junction1.seat","oriel.followerSeen","oriel.junction2","oriel.junction2.seat","oriel.junction3","oriel.junction3.seat","oriel.doorReached",
            "oriel.choirSeen","oriel.releaseLearned","oriel.stay1","oriel.stay2","oriel.stay3","oriel.wardenMet"};
        static readonly string[] Routes={"chapter-38-orrery","chapter-39-skyfields","chapter-40-glass-choir","chapter-41-last-warden","chapter-42-oriel-errands","chapter-43-the-far-end"};
        /// <summary>One straight leg: from where (unknown after a chase and at a zone's first leg in flight), to where, the boss whose door it is (checked again from that
        /// door's checkpoint spawn), and the hull it is flown with.</summary>
        sealed class Leg{public string route,name,scene,door;public Vector3? from;public Vector3 to;public float tolerance,hull;public bool first;}
        static List<Leg> Legs()
        {
            var legs=new List<Leg>();string root=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
            float pair=Mathf.Max(HeroCollision.HullRadius("Taren"),HeroCollision.HullRadius("Idra"));
            foreach(string name in Routes)
            {
                string file=System.IO.Path.Combine(root,name+".json");Assert.IsTrue(System.IO.File.Exists(file),"fixture: "+file+" (python tools/oriel_routes.py writes it)");
                var route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));string scene=null;Vector3? at=null;bool arrived=false;
                float hull=route.deployed!=null&&route.deployed.Length>0?route.deployed.Max(h=>HeroCollision.HullRadius(h)):pair;
                foreach(var step in route.steps)
                {
                    if(step.expectedScene!=scene){scene=step.expectedScene;at=null;arrived=true;}
                    if(!step.navigate)continue;
                    // After a chase the party is wherever the fight left it, and the walk from there finds its way over the baked floor (findWay): judged at its end only.
                    if(step.approachTarget||step.approachPartner){at=null;arrived=false;continue;}
                    legs.Add(new Leg{route=name,name=step.name,scene=scene,from=at,to=step.point,tolerance=step.tolerance,hull=hull,first=arrived,door=string.IsNullOrEmpty(step.bossDoor)?null:step.bossDoor});at=step.point;arrived=false;
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
            var volume=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(v=>v.name==boss||v.encounterId==boss);
            if(volume==null||string.IsNullOrEmpty(volume.checkpointSpawn))return null;
            var spawn=Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(s=>s.id==volume.checkpointSpawn);
            return spawn!=null?spawn.transform.position:null;
        }
        /// <summary>Every zone the chapter 7 routes walk or fly, one case each, so a zone's own run (focused.ps1 -Zones) reaches its legs.</summary>
        static readonly string[] LegZones={"ManyHands","ManyHandsHull","Orrery","OrielSkyfields","GlassChoir"};
        [Test] public void TheChapterSevenLegCheckHasACaseForEveryZoneItsRoutesWalk()
            =>CollectionAssert.AreEquivalent(Legs().Select(l=>l.scene).Distinct().ToArray(),LegZones,"a chapter 7 route walks a zone the leg check has no case for, or the other way round");
        /// <summary>Every route here is played with the pair chapter 6 leaves out (chapter-37-long-sleep): a run continuing that save with another pair is rejected at its first step.</summary>
        [Test] public void EveryChapterSevenRouteIsPlayedWithTarenAndIdraOut()
        {
            string root=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
            foreach(string name in Routes.Prepend("chapter-37-long-sleep"))
            {
                var route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(System.IO.Path.Combine(root,name+".json")));
                CollectionAssert.AreEquivalent(new[]{"Taren","Idra"},route.deployed??new string[0],name+" isn't played with Taren and Idra out");
            }
        }
        [UnityTest] public IEnumerator EveryWalkOnTheChapterSevenRoutesFollowsFloorABodyCanCross([ValueSource(nameof(LegZones))] string only)
        {
            var legs=Legs();Assert.IsNotEmpty(legs.Where(l=>l.scene==only).ToArray(),"fixture: the chapter 7 routes walk "+only);
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
                    if(start==null&&leg.first){var spawn=spawns.OrderBy(s=>Flat(s.transform.position,leg.to)).FirstOrDefault();if(spawn!=null&&Flat(spawn.transform.position,leg.to)<25)start=spawn.transform.position;}
                    if(!NavMesh.SamplePosition(leg.to+Vector3.up*.5f,out var end,2.5f,NavMesh.AllAreas)||Flat(end.position,leg.to)>Mathf.Max(.6f,leg.tolerance)){faults.Add($"{where} ends at {leg.to}, off the floor a body walks"+(NavMesh.SamplePosition(leg.to,out var nearest,6,NavMesh.AllAreas)?$" (the nearest floor is at {nearest.position})":""));continue;}
                    if(start==null)continue;
                    if(!NavMesh.SamplePosition(start.Value+Vector3.up*.5f,out var begin,2.5f,NavMesh.AllAreas)){faults.Add($"{where} starts at {start.Value}, off the floor");continue;}
                    if(!NavMesh.Raycast(begin.position,end.position,out var cut,NavMesh.AllAreas))continue;
                    var near=Physics.OverlapSphere(cut.position+Vector3.up*.6f,1.1f,~0,QueryTriggerInteraction.Ignore).Where(c=>!(c is CharacterController)&&c.GetComponentInParent<CombatActor>()==null&&Mathf.Max(c.bounds.size.x,c.bounds.size.z)<30)
                        .Select(c=>c.transform.root==c.transform?c.name:c.transform.root.name+"/"+c.name).Distinct().ToArray();
                    var path=new NavMeshPath();bool joined=NavMesh.CalculatePath(begin.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                    // The floor's own shortest way is the judge: if it is the straight line itself (no corner between the ends), the straight walk is on floor; anything standing on
                    // the line, or an edge it clips, bends that way. A corner the bake leaves where a deck or a road meets an island lies on the line itself (within 10 cm).
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
            Assert.IsEmpty(faults,"the chapter 7 routes walk or fly where a body can't:\n"+string.Join("\n",faults.Distinct().Take(40)));
        }
        /// <summary>A boss fight behind an open gate begins only once every hero out stands a body's radius and half a metre past the gate's line (EncounterVolume.TryEnter), and
        /// the partner follows three metres behind the lead (PartnerBrain). The Last Warden's seal stands 2.2 m inside its fight's box, so a door walk "three metres inside the box"
        /// (RouteFightTests) left Idra on the seal's line and the fight unbegun for seven minutes (route 41's first run in a player, November 14). Measured here as TryEnter
        /// measures it, from every open gate of the fight, with the lead stopping up to its step's tolerance short and the partner a metre short of her place.</summary>
        [UnityTest] public IEnumerator TheLastWardensDoorWalkLeavesThePartnerRoomPastItsSeal()
        {
            foreach(string flag in AsWalked)Flag(flag);
            yield return Zone("GlassChoir");
            var fight=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(v=>v.encounterId=="LastWarden");
            Assert.IsNotNull(fight,"fixture: the engine hall's fight");Assert.IsNotEmpty(fight.membranes,"fixture: the Warden's fight has its seal");
            var door=Legs().Single(l=>l.door=="LastWarden");
            // Built wrong on purpose (ORIEL_HANDS_BROKEN=1, as OrielReplayHandsTests): the door walk as route 41 first had it.
            if(System.Environment.GetEnvironmentVariable("ORIEL_HANDS_BROKEN")=="1"){door.to.z=281;door.tolerance=1.5f;}
            float radius=PartyController.Current.members.Max(m=>m.GetComponent<CharacterController>().radius);
            foreach(var seal in fight.membranes)
            {
                var inward=fight.transform.position-seal.transform.position;inward.y=0;inward.Normalize();
                var end=door.to;end.y=seal.transform.position.y;
                float lead=Vector3.Dot(end-seal.transform.position,inward)-door.tolerance,partner=lead-3-1;
                Assert.GreaterOrEqual(partner,radius+.5f,$"'{door.name}' stops the lead {lead:0.0} m past {seal.name}: the partner three metres behind, a metre short of her place, stands {partner:0.0} m past it, and the fight waits for {radius+.5f:0.0}");
            }
        }
        /// <summary>A wait on "lee:n" ends the moment the lee comes, wherever the ship is between where the wait began and its waiting place (ContinuousReview.WaitedOut), so the
        /// leg after it may start as far back as the leg before it ended. Budgeted from the waiting place, the Lamplighter's ring passed slowly (braked, 2 m/s) started 22 m short
        /// of its point with 8 s for it, ended 3.8 m short and was never read (route 42 in a player, November 14). Every leg after a wait has its seconds for the whole way from the
        /// wait's own start at its pace, braked or not, with a fifth and a second to spare.</summary>
        [Test] public void EveryLegAfterALeeWaitHasTheSecondsForTheWayFromWhereTheWaitBegan()
        {
            string root=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");var faults=new List<string>();int judged=0;
            const float Flying=11,Braked=2;
            foreach(string name in new[]{"chapter-38-orrery","chapter-42-oriel-errands"})
            {
                var steps=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(System.IO.Path.Combine(root,name+".json"))).steps;
                for(int i=0;i<steps.Length;i++)
                {
                    if(steps[i].until==null||!steps[i].until.StartsWith("lee:"))continue;
                    var before=Enumerable.Range(0,i).Reverse().Select(k=>steps[k]).FirstOrDefault(s=>s.navigate&&s.expectedScene==steps[i].expectedScene);
                    var after=Enumerable.Range(i+1,steps.Length-i-1).Select(k=>steps[k]).FirstOrDefault(s=>s.navigate);
                    if(before==null||after==null)continue;
                    float way=Flat(before.point,after.point),pace=after.leftTrigger>0?Braked:Flying,need=way/pace*1.2f+1;judged++;
                    if(after.seconds<need)faults.Add($"{name} '{after.name}': {after.seconds:0.0} s, and from where '{steps[i].name}' began ({before.point.x:0.0}, {before.point.z:0.0}) it is {way:0.0} m at {pace} m/s: it wants {need:0.0}");
                }
            }
            Assert.AreEqual(27,judged,"fixture: the chapter 7 routes wait in a moon's lee nine times in and eighteen on the lap");
            Assert.IsEmpty(faults,"a leg after a lee wait has too few seconds for where the wait may have ended:\n"+string.Join("\n",faults));
        }
        /// <summary>The Orrery is crossed on its own time: every crossing of a rail is a wait short of its middle gate on "lee:n" and then the walk through, and each of the nine is
        /// the rail the wait names. A route that crossed a rail on a clock, or named the wrong rail, would force gates as often as not.</summary>
        [UnityTest] public IEnumerator EveryOrreryCrossingWaitsInTheLeeOfTheRailItCrosses()
        {
            yield return Zone("Orrery");
            var rails=Object.FindObjectsByType<OrreryRail>(FindObjectsSortMode.None).OrderByDescending(r=>r.radius).ToArray();
            Assert.AreEqual(9,rails.Length,"fixture: the Orrery's nine rails");
            string root=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");var faults=new List<string>();int waits=0;
            foreach(string name in new[]{"chapter-38-orrery","chapter-42-oriel-errands"})
            {
                var route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(System.IO.Path.Combine(root,name+".json")));
                var steps=route.steps;
                for(int i=0;i<steps.Length;i++)
                {
                    if(steps[i].expectedScene!="Orrery"||!steps[i].navigate)continue;
                    var p=steps[i].point;
                    // Which rail's moon path the walk crosses, if any: from the point before it to this one. The wait is the walk before it, or (a named ring passed slowly,
                    // inward) the one before that.
                    var walked=Enumerable.Range(0,i).Reverse().Select(k=>steps[k]).Where(s=>s.navigate&&s.expectedScene=="Orrery").Take(2).ToArray();
                    if(walked.Length==0)continue;
                    var before=walked[0];var wait=walked.FirstOrDefault(s=>!string.IsNullOrEmpty(s.until)&&s.until.StartsWith("lee:"));
                    foreach(var (rail,n) in rails.Select((r,k)=>(r,k+1)))
                    {
                        float a=rail.FromCentre(before.point)-rail.radius,b=rail.FromCentre(p)-rail.radius;
                        if(Mathf.Sign(a)==Mathf.Sign(b))continue;
                        // A crossing of rail n: just before it, the wait in its lee, naming it.
                        if(wait==null||wait.until!="lee:"+n)faults.Add($"{name} '{steps[i].name}' crosses {rail.railName} (rail {n}) after '{before.name}' with no wait on lee:{n} before it"+(wait!=null?$" ('{wait.name}' waits on {wait.until})":""));
                        else if(Mathf.Abs(rail.FromCentre(wait.point)-rail.GateRadius)>ContinuousReview.LeeWithin-2)faults.Add($"{name} '{wait.name}' waits {Mathf.Abs(rail.FromCentre(wait.point)-rail.GateRadius):0.0} m off {rail.railName}'s gate row, further than its lee is read");
                        else waits++;
                    }
                }
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
            Assert.AreEqual(9+18,waits,"the chapter 7 routes cross the Orrery's rails nine times in and eighteen on the lap");
        }
    }
}
