using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 4's replay routes (tools/wendmire_routes.py), checked against the built zones: the driver walks and flies straight lines with no pathfinding, so every leg
    /// must cross navigation on foot and open space in flight. Each zone loads as the routes walk it: the tide turned (Driftreed's swinging walks across to the gate), and the
    /// Waystation's pumps run (its floors drained and their deep-water blockers gone).</summary>
    public sealed class WendmireRouteTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter4");
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
        static readonly string[] AsWalked={"wend.charted","wend.lineHeld","wend.onaHeard","wend.tideTurned","wend.lureLearned","wend.pump1","wend.pump2","wend.pimFound","wend.pump3","wend.pump4"};
        static readonly string[] Routes={"chapter-20-spore-veil","chapter-21-driftreed","chapter-22-driftreed-errands","chapter-23-drowned-waystation"};
        sealed class Leg{public string route,name,scene;public Vector3? from;public Vector3 to;public float tolerance;}
        static List<Leg> Legs()
        {
            var legs=new List<Leg>();string root=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
            foreach(string name in Routes)
            {
                string file=System.IO.Path.Combine(root,name+".json");Assert.IsTrue(System.IO.File.Exists(file),"fixture: "+file+" (python tools/wendmire_routes.py writes it)");
                var route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));string scene=null;Vector3? at=null;
                foreach(var step in route.steps)
                {
                    if(step.expectedScene!=scene){scene=step.expectedScene;at=null;}
                    if(!step.navigate)continue;
                    if(step.approachTarget||step.approachPartner){at=null;continue;}
                    legs.Add(new Leg{route=name,name=step.name,scene=scene,from=at,to=step.point,tolerance=step.tolerance});at=step.point;
                }
            }
            return legs;
        }
        static bool Flying(string scene){var zone=GameCatalog.Find<ZoneDef>(scene);return zone!=null&&(zone.kind==ZoneKind.SpaceCombat||zone.kind==ZoneKind.SpaceSafe);}
        /// <summary>Every zone the chapter 4 routes walk or fly, one case each, so a zone's own run (focused.ps1 -Zones) reaches its legs.</summary>
        static readonly string[] LegZones={"TallowDrift","TallowApproach","SporeVeil","Driftreed","DrownedWaystation"};
        [Test] public void TheChapterFourLegCheckHasACaseForEveryZoneItsRoutesWalk()
            =>CollectionAssert.AreEquivalent(Legs().Select(l=>l.scene).Distinct().ToArray(),LegZones,"a chapter 4 route walks a zone the leg check has no case for, or the other way round");
        [UnityTest] public IEnumerator EveryWalkOnTheChapterFourRoutesFollowsFloorABodyCanCross([ValueSource(nameof(LegZones))] string only)
        {
            var legs=Legs();Assert.IsNotEmpty(legs.Where(l=>l.scene==only).ToArray(),"fixture: the chapter 4 routes walk "+only);
            var faults=new List<string>();float hull=Mathf.Max(HeroCollision.HullRadius("Taren"),HeroCollision.HullRadius("Sela"));
            // On foot first, so a fault in a flight zone can't hide what the walks did.
            foreach(string scene in new[]{only})
            {
                Assert.IsNotNull(GameCatalog.Find<ZoneDef>(scene),"fixture: a route walks in "+scene+", which is no zone");
                var mine=legs.Where(l=>l.scene==scene).ToArray();
                if(Flying(scene))Assert.IsEmpty(faults,"the chapter 4 routes walk where a body can't:\n"+string.Join("\n",faults.Distinct().Take(40)));
                foreach(string flag in AsWalked)Flag(flag);
                yield return Zone(scene);
                if(!Flying(scene))
                {
                    var spawns=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
                    foreach(var leg in mine)
                    {
                        string where=$"{leg.route} '{leg.name}' ({scene})";var start=leg.from;
                        // A zone's first walk starts at the spawn the route arrived at: the nearest, when one is near enough to be it.
                        if(start==null){var spawn=spawns.OrderBy(s=>Flat(s.transform.position,leg.to)).FirstOrDefault();if(spawn!=null&&Flat(spawn.transform.position,leg.to)<25)start=spawn.transform.position;}
                        if(!NavMesh.SamplePosition(leg.to+Vector3.up*.5f,out var end,2.5f,NavMesh.AllAreas)||Flat(end.position,leg.to)>Mathf.Max(.6f,leg.tolerance)){faults.Add($"{where} ends at {leg.to}, off the floor a body walks"+(NavMesh.SamplePosition(leg.to,out var nearest,6,NavMesh.AllAreas)?$" (the nearest floor is at {nearest.position})":""));continue;}
                        if(start==null)continue;
                        if(!NavMesh.SamplePosition(start.Value+Vector3.up*.5f,out var begin,2.5f,NavMesh.AllAreas)){faults.Add($"{where} starts at {start.Value}, off the floor");continue;}
                        if(!NavMesh.Raycast(begin.position,end.position,out var cut,NavMesh.AllAreas))continue;
                        var near=Physics.OverlapSphere(cut.position+Vector3.up*.6f,1.1f,~0,QueryTriggerInteraction.Ignore).Where(c=>!(c is CharacterController)&&c.GetComponentInParent<CombatActor>()==null&&Mathf.Max(c.bounds.size.x,c.bounds.size.z)<30)
                            .Select(c=>c.transform.root==c.transform?c.name:c.transform.root.name+"/"+c.name).Distinct().ToArray();
                        var path=new NavMeshPath();bool joined=NavMesh.CalculatePath(begin.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                            // The ray stops at a seam where a walk's deck meets a raft as readily as at a wall. The floor's own shortest way is the judge: if it is the straight line itself
                            // (no corner between the ends), the straight walk is on floor; anything standing on the line, or a rim it clips, bends that way.
                            if(joined&&path.corners.Length<=2)continue;
                        float walked=0;for(int i=1;i<path.corners.Length;i++)walked+=Vector3.Distance(path.corners[i-1],path.corners[i]);
                        faults.Add($"{where}: the straight walk from {start.Value} to {leg.to} is cut at {cut.position}; within a metre there: {(near.Length>0?string.Join(", ",near):"nothing solid (an edge of the floor)")}; "
                            +(joined?$"the floor joins the ends by {walked:0.0} m against {Vector3.Distance(begin.position,end.position):0.0} straight, by {string.Join(" ",path.corners.Select(c=>$"({c.x:0.0},{c.z:0.0})"))}":"the floor doesn't join the ends at all"));
                    }
                    continue;
                }
                foreach(var leg in mine)
                {
                    var to=leg.to;to.y=1;var met=Physics.OverlapSphere(to+Vector3.up*.8f,hull,~0,QueryTriggerInteraction.Ignore).AsEnumerable();
                    if(leg.from is Vector3 start){start.y=1;var way=to-start;if(way.magnitude>.01f)met=met.Concat(Physics.SphereCastAll(start+Vector3.up*.8f,hull,way.normalized,way.magnitude,~0,QueryTriggerInteraction.Ignore).Select(h=>h.collider));}
                    foreach(var c in met.Where(c=>c!=null&&!(c is CharacterController)&&c.GetComponentInParent<Health>()==null).Distinct())faults.Add($"{leg.route} '{leg.name}' ({scene}) flies into {c.name} on the way to {leg.to}");
                }
            }
            Assert.IsEmpty(faults,"the chapter 4 routes walk or fly where a body can't:\n"+string.Join("\n",faults.Distinct().Take(40)));
        }
    }
}
