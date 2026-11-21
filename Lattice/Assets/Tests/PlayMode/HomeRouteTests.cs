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
    /// <summary>Chapter 9's replay routes (tools/home_routes.py), checked against the built zones in the state each route walks them in: every leg on the baked floor (on foot)
    /// or clear of everything solid by Idra's hull (in flight), a boss's door leg from its checkpoint as well; every press made where a body can stand, with the prompt it is
    /// for in reach and, unless the route waited for it to come, the one the game offers there; every fight locked from inside its box (a boss's three metres in). A zone is
    /// loaded once for each stretch a route walks it in, with the flags the routes have set by then (the meeting's room stands only from its gathering; the Coronach's people
    /// from the call). Each check is first run on a broken item and has to find it.</summary>
    public sealed class HomeRouteTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter9");
        }
        [UnityTearDown] public IEnumerator Cleanup(){GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static bool Flying(string scene){var zone=GameCatalog.Find<ZoneDef>(scene);return zone!=null&&(zone.kind==ZoneKind.SpaceCombat||zone.kind==ZoneKind.SpaceSafe);}
        static float Hull=>Mathf.Max(HeroCollision.HullRadius("Taren"),HeroCollision.HullRadius("Idra"));
        /// <summary>RouteFightTests' fifteen centimetres: how far inside a reach a route must stand.</summary>
        const float Inside=.15f,PartnerRoom=3;
        /// <summary>A press whose flag loads the zone again (the town meeting gathers) or changes who stands in it from then on (the Coronach's call): what follows is a new stretch.</summary>
        static readonly string[] Restages={"home.meetingGathered","home.coronachCalled","home.rideDone"};
        /// <summary>Zones whose state a flag changes only at a load: the Works' joints give and its pinning press lifts as they are worked, and a load with the flag stands them so.</summary>
        static readonly string[] EveryFlagRestages={"SeparationWorks"};

        // ------------------------------------------------------------------ the routes, read
        static string Folder=>System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
        static string[] RouteNames()=>System.IO.Directory.GetFiles(Folder,"chapter-*.json").Select(System.IO.Path.GetFileNameWithoutExtension)
            .Where(n=>int.TryParse(n.Split('-')[1],out int k)&&k>=53&&k<=61).OrderBy(n=>n,System.StringComparer.Ordinal).ToArray();
        static QualityRoute Read(string name)=>JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(System.IO.Path.Combine(Folder,name+".json")));
        sealed class Leg{public string route,name,door;public Vector3? from;public Vector3 to;public float tolerance;public bool findsWay;}
        sealed class Press{public string route,name,flag,to,after;public bool talk,waited,saves;public Vector3 at;public float tolerance;public string[] flags;}
        sealed class Lock{public string route,name,id,after;public Vector3 at;public float tolerance;public bool boss;}
        /// <summary>One stretch of a zone as a route walks it: loaded once, with the flags set by the time the route got there.</summary>
        sealed class Stretch{public string route,scene;public int index;public string[] flags;public readonly List<Leg> legs=new();public readonly List<Press> presses=new();public readonly List<Lock> locks=new();}
        /// <summary>Each route's stretches, zone by zone. A press that takes the party to another zone (a warp, a pad, a dock, a way) is made in the zone it leaves, and is filed there; filed
        /// <paramref name="asBefore"/>, it went under the zone it arrives in (its step's expectedScene), and no way out of any chapter 9 zone was ever judged (D293).</summary>
        static List<Stretch> Stretches(bool asBefore=false)
        {
            var all=new List<Stretch>();var flags=new HashSet<string>();
            foreach(string name in RouteNames())
            {
                var route=Read(name);Stretch here=null;Vector3? at=null;string after=null,door=null;float tolerance=0;bool waited=false;
                for(int i=0;i<route.steps.Length;i++)
                {
                    var step=route.steps[i];string scene=string.IsNullOrEmpty(step.expectedScene)?route.scene:step.expectedScene;
                    if(!asBefore&&here!=null&&step.until!=null&&step.until.StartsWith("scene:")&&!step.navigate)scene=here.scene;
                    if(here==null||scene!=here.scene){here=new Stretch{route=name,scene=scene,index=all.Count,flags=flags.ToArray()};all.Add(here);at=null;door=null;}
                    if(step.navigate)
                    {
                        if(step.approachTarget||step.approachPartner){at=null;door=null;continue;}
                        here.legs.Add(new Leg{route=name,name=step.name,from=at,to=step.point,tolerance=step.tolerance,door=string.IsNullOrEmpty(step.bossDoor)?null:step.bossDoor,findsWay=step.findWay});
                        at=step.point;after=step.name;tolerance=step.tolerance;door=string.IsNullOrEmpty(step.bossDoor)?null:step.bossDoor;waited=false;
                    }
                    else if(at is Vector3 stand&&scene!="Title")
                    {
                        if(!string.IsNullOrEmpty(step.until)&&step.until.StartsWith("prompt:"))waited=true;
                        if(step.name.StartsWith("lock ")&&step.buttons!=null&&step.buttons.Contains("LeftShoulder"))
                            here.locks.Add(new Lock{route=name,name=step.name,id=door??step.name.Substring(5),after=after,at=stand,tolerance=tolerance,boss=door!=null});
                        bool south=step.buttons!=null&&step.buttons.Length==1&&step.buttons[0]=="South"&&string.IsNullOrEmpty(step.pauseUi)&&string.IsNullOrEmpty(step.pickRow);
                        bool talk=step.expectedUi=="dialogue"||step.until=="ui:dialogue";
                        string wants=step.until!=null&&step.until.StartsWith("flag:")?step.until.Substring(5):null;
                        string to=step.until!=null&&step.until.StartsWith("scene:")?step.until.Substring(6):null;
                        if(south&&(talk||wants!=null||to!=null||step.until=="saved"))
                        {
                            // A talk a prompt opens: the flag its reading ends on, if a prompt of the zone sets it (a hail, a door), is what the prompt is for.
                            string read=talk?route.steps.Skip(i+1).FirstOrDefault(s=>s.name.StartsWith("finish "))?.expectedFlag:null;
                            here.presses.Add(new Press{route=name,name=step.name,flag=wants??read,to=to,talk=talk,waited=waited,saves=step.until=="saved",after=after,at=stand,tolerance=tolerance,flags=flags.ToArray()});
                        }
                    }
                    bool added=false;
                    if(!string.IsNullOrEmpty(step.expectedFlag))added|=flags.Add(step.expectedFlag);
                    if(!string.IsNullOrEmpty(step.until)&&step.until.StartsWith("flag:"))added|=flags.Add(step.until.Substring(5));
                    if(added&&EveryFlagRestages.Contains(scene))here=null;
                    if(Restages.Contains(step.until?.Substring(step.until.IndexOf(':')+1))||Restages.Contains(step.expectedFlag)){here=null;}
                }
            }
            return all;
        }
        public static IEnumerable<string> Zones()=>Stretches().Select(s=>s.scene).Where(s=>s!="Title").Distinct().OrderBy(s=>s,System.StringComparer.Ordinal);

        // ------------------------------------------------------------------ a zone, as the route finds it
        static IEnumerator Load(Stretch s)
        {
            foreach(string flag in s.flags)GameServices.Current.Flags.SetBool(flag,true);
            yield return Enter(s.scene);
            // The Coronach walk at its fullest: every side story done, so everyone who can stand in it does (CoronachTests' worst case). A chain finishes the stories it plays and a
            // player any of them; each story's person stands where the build put them, so a walk with room for all of them has room whatever was done. Loaded with the route's
            // flags alone (no side story done), the walk had nobody on the dock office's front, and the first chain to play it walked into Jask there (route 60, November 14).
            var due=Stories().Where(f=>!GameServices.Current.Flags.GetBool(f)).ToArray();
            if(due.Length>0){foreach(string f in due)GameServices.Current.Flags.SetBool(f,true);yield return Enter(s.scene);}
        }
        /// <summary>Every story the walk's people stand for (each extra's <c>quest.Id.complete</c>), once the walk is on; nothing elsewhere.</summary>
        static string[] Stories()
        {
            var walk=Object.FindFirstObjectByType<CoronachWalk>();
            if(walk==null||!GameServices.Current.Flags.GetBool(walk.calledFlag))return System.Array.Empty<string>();
            return Object.FindObjectsByType<CoronachDoor>(FindObjectsInactive.Include,FindObjectsSortMode.None).SelectMany(d=>d.extras??System.Array.Empty<CoronachDoor.Extra>())
                .Select(e=>e.needs).Where(n=>!string.IsNullOrEmpty(n)).Distinct().ToArray();
        }
        static IEnumerator Enter(string scene)
        {
            SceneFlow.Current.LoadZone(scene,"Arrival");float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,scene+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            // A carving obstacle (the meeting's benches and people, the walk's people) cuts the floor a frame or two after it is shown.
            yield return new WaitForSecondsRealtime(1f);
        }
        static Vector3? Checkpoint(string boss)
        {
            var volume=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(v=>v.encounterId==boss);
            if(volume==null||string.IsNullOrEmpty(volume.checkpointSpawn))return null;
            var spawn=Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(p=>p.id==volume.checkpointSpawn);
            return spawn!=null?spawn.transform.position:null;
        }
        static float OffTheLine(Vector3 p,Vector3 a,Vector3 b){var d=new Vector2(b.x-a.x,b.z-a.z);var q=new Vector2(p.x-a.x,p.z-a.z);float t=Mathf.Clamp01(Vector2.Dot(q,d)/Mathf.Max(.0001f,d.sqrMagnitude));return (q-d*t).magnitude;}

        // ------------------------------------------------------------------ the three judgements
        /// <summary>What is wrong with a leg: on foot, its end off the baked floor or the straight walk cut; in flight, anything solid within a hull of the way.</summary>
        static string JudgeLeg(string scene,Leg leg)
        {
            string where=$"{leg.route} '{leg.name}' ({scene})";
            if(Flying(scene))
            {
                var to=leg.to;to.y=1;var met=Physics.OverlapSphere(to+Vector3.up*.8f,Hull,~0,QueryTriggerInteraction.Ignore).AsEnumerable();
                if(leg.from is Vector3 start){start.y=1;var way=to-start;if(way.magnitude>.01f)met=met.Concat(Physics.SphereCastAll(start+Vector3.up*.8f,Hull,way.normalized,way.magnitude,~0,QueryTriggerInteraction.Ignore).Select(h=>h.collider));}
                var hit=met.FirstOrDefault(c=>c!=null&&!(c is CharacterController)&&c.GetComponentInParent<Health>()==null);
                return hit!=null?$"{where}, a {Hull:0.00} m hull, flies into {hit.name} on the way to {leg.to}":null;
            }
            if(!NavMesh.SamplePosition(leg.to+Vector3.up*.5f,out var end,2.5f,NavMesh.AllAreas)||Flat(end.position,leg.to)>Mathf.Max(.6f,leg.tolerance))
                return $"{where} ends at {leg.to}, off the floor a body walks"+(NavMesh.SamplePosition(leg.to,out var nearest,6,NavMesh.AllAreas)?$" (the nearest floor is at {nearest.position})":"");
            if(leg.from==null)return null;
            if(!NavMesh.SamplePosition(leg.from.Value+Vector3.up*.5f,out var begin,2.5f,NavMesh.AllAreas))return $"{where} starts at {leg.from.Value}, off the floor";
            if(!NavMesh.Raycast(begin.position,end.position,out var cut,NavMesh.AllAreas))return null;
            var path=new NavMeshPath();bool joined=NavMesh.CalculatePath(begin.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
            if(joined&&path.corners.Skip(1).Take(path.corners.Length-2).All(c=>OffTheLine(c,begin.position,end.position)<.1f))return null;
            var near=Physics.OverlapSphere(cut.position+Vector3.up*.6f,1.1f,~0,QueryTriggerInteraction.Ignore).Where(c=>!(c is CharacterController)&&c.GetComponentInParent<CombatActor>()==null&&Mathf.Max(c.bounds.size.x,c.bounds.size.z)<30)
                .Select(c=>c.transform.root==c.transform?c.name:c.transform.root.name+"/"+c.name).Distinct().ToArray();
            return $"{where}: the straight walk from {leg.from.Value} to {leg.to} is cut at {cut.position}; within a metre there: {(near.Length>0?string.Join(", ",near):"nothing solid (an edge of the floor)")}; "
                +(joined?$"the floor joins the ends by {string.Join(" ",path.corners.Select(c=>$"({c.x:0.0},{c.z:0.0})"))}":"the floor doesn't join the ends at all");
        }
        static string Field(InteractionPrompt p,string name){var f=p.GetType().GetField(name);return f!=null&&f.FieldType==typeof(string)?(string)f.GetValue(p):null;}
        static bool For(InteractionPrompt p,Press press)
        {
            if(press.to!=null)return Field(p,"scene")==press.to;
            if(press.saves)return p.prompt!=null&&p.prompt.Contains("save");
            if(press.flag!=null&&Field(p,"flag")==press.flag)return true;
            return false;
        }
        /// <summary>A prompt a press can be made at: one with a reach (the meeting's turns are points of range 0 the arrow aims at, never offered).</summary>
        static InteractionPrompt[] Prompts()=>Object.FindObjectsByType<InteractionPrompt>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.range>0).ToArray();
        static bool AnyPromptSets(string flag)=>flag!=null&&Prompts().Any(p=>Field(p,"flag")==flag);
        /// <summary>What is wrong with a press: where it is made isn't floor (or open space, in flight); nothing it is for is in reach with the walk's tolerance and fifteen centimetres to
        /// spare; or (unless the route waited for the prompt to come) the game offers something else there, or nothing.</summary>
        static string JudgePress(string scene,Press press)
        {
            string where=$"{press.route} '{press.name}' after '{press.after}' ({scene}, within {press.tolerance:0.##} of ({press.at.x:0.0}, {press.at.z:0.0}))";
            if(!Flying(scene)&&(!NavMesh.SamplePosition(press.at+Vector3.up*.5f,out var floor,2.5f,NavMesh.AllAreas)||Flat(floor.position,press.at)>.6f))return where+": no floor a body can stand on there";
            var prompts=Prompts();
            // What it is for: a way to the zone it goes to, a prompt that sets its flag; a talk with no prompt of its own flag is somebody's (an Npc); a press for a flag the zone's own
            // script sets later (the ride's board, whose end sets home.rideDone) is for whatever is there.
            bool known=press.to!=null||press.saves||AnyPromptSets(press.flag);
            var meant=prompts.Where(p=>known?For(p,press):press.talk?p is Npc:true).ToArray();
            if(meant.Length==0)return where+": the zone has nothing this press is for ("+(press.to??press.flag??"a talk")+")";
            float Over(InteractionPrompt p){var gap=p.transform.position-press.at;gap.y=0;return gap.magnitude+press.tolerance+Inside-p.range;}
            var nearest=meant.OrderBy(Over).First();
            if(Over(nearest)>0)return $"{where}: '{nearest.name}' at ({nearest.transform.position.x:0.0}, {nearest.transform.position.z:0.0}) reaches {nearest.range:0.##} m, {Over(nearest):0.00} m too far to be surely offered";
            if(press.waited)return null;
            var stand=press.at;stand.y=Flying(scene)?1:(NavMesh.SamplePosition(press.at+Vector3.up*.5f,out var f2,2.5f,NavMesh.AllAreas)?f2.position.y:press.at.y);
            PromptService.FindInteraction(stand);var offered=PromptService.Interaction;
            if(offered==null)return $"{where}: the game offers nothing there (the meant '{nearest.name}' is in reach but not offered: hidden, or waiting on something)";
            if(!(known?For(offered,press):!press.talk||offered is Npc||offered is DiscoveryPoint))return $"{where}: the game offers '{offered.name}' ('{offered.prompt}') there, not '{nearest.name}'";
            return null;
        }
        /// <summary>What is wrong with a lock: the walk before it ends where the fight it names hasn't begun (outside its box by the walk's tolerance; a boss's three metres in).</summary>
        static string JudgeLock(string scene,Lock l)
        {
            var volume=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(v=>v.encounterId==l.id);
            if(volume==null)return $"{l.route} '{l.name}': {scene} has no fight called {l.id}";
            var box=volume.GetComponent<BoxCollider>();if(box==null)return $"{l.route} '{l.name}': {l.id} has no box to walk into";
            var local=box.transform.InverseTransformPoint(new Vector3(l.at.x,box.transform.position.y,l.at.z))-box.center;var scale=box.transform.lossyScale;
            float outside=Mathf.Max(Mathf.Abs(local.x*scale.x)-box.size.x*Mathf.Abs(scale.x)/2,Mathf.Abs(local.z*scale.z)-box.size.z*Mathf.Abs(scale.z)/2)+l.tolerance+(l.boss?PartnerRoom:0);
            return outside>0?$"{l.route} '{l.name}': after '{l.after}' the party stands within {l.tolerance:0.#} m of ({l.at.x:0.0}, {l.at.z:0.0}), {outside:0.0} m short of surely inside {l.id}'s box{(l.boss?" by three metres":"")}":null;
        }

        // ------------------------------------------------------------------ a hero's body on the way
        /// <summary>A hero's body on foot (ActorFactory: a capsule .35 m round and 1.9 m tall that steps up .35 m), and how often a straight walk is felt along its length.</summary>
        const float BodyRadius=.35f,BodyHeight=1.9f,BodyStep=.35f,Along=.25f;
        /// <summary>The floor under a plan point, at the height of the baked floor nearest it (the point itself, not the nearest floor: a carved hole round someone standing is
        /// still their floor).</summary>
        static Vector3? FloorAt(Vector3 p)=>NavMesh.SamplePosition(p+Vector3.up*.5f,out var hit,1.5f,NavMesh.AllAreas)?new Vector3(p.x,hit.position.y,p.z):null;
        /// <summary>What a hero's body would stand inside at this floor point, if anything: the capsule set on the floor, against everything solid but the party and what fights
        /// (they move), the town's walkers (they stop and stand aside for a body) and the floor itself.</summary>
        static Collider InTheWay(Vector3 floor)
        {
            var bottom=floor+Vector3.up*(BodyRadius+.05f);var top=floor+Vector3.up*(BodyHeight-BodyRadius);
            foreach(var c in Physics.OverlapCapsule(bottom,top,BodyRadius,~0,QueryTriggerInteraction.Ignore))
            {
                if(c==null||c is CharacterController||c.GetComponentInParent<CombatActor>()!=null||c.GetComponentInParent<Health>()!=null||c.GetComponentInParent<WalkLoop>()!=null)continue;
                if(!Underfoot(c,floor))return c;
            }
            return null;
        }
        /// <summary>Floor, or a step a body goes up: rays down through the body's round from over its head find its top, and nowhere more than a step up. What the rays never find (a wall,
        /// or someone, whose top is over the rays' start or beside them) is not floor.</summary>
        static bool Underfoot(Collider c,Vector3 floor)
        {
            bool any=false;
            for(int k=-1;k<8;k++)
            {
                var at=floor+(k<0?Vector3.zero:new Vector3(Mathf.Cos(k*Mathf.PI/4),0,Mathf.Sin(k*Mathf.PI/4))*BodyRadius*.9f);
                if(!c.Raycast(new Ray(at+Vector3.up*(BodyHeight+.6f),Vector3.down),out var hit,BodyHeight+1.2f))continue;
                if(hit.point.y>floor.y+BodyStep)return false;
                any=true;
            }
            return any;
        }
        static string Named(Collider c)
        {
            var names=new List<string>();for(var t=c.transform;t!=null&&names.Count<3;t=t.parent)names.Insert(0,t.name);
            return $"'{string.Join("/",names)}' ({c.GetType().Name} at ({c.bounds.center.x:0.00}, {c.bounds.center.z:0.00}), {c.bounds.size.x:0.00} by {c.bounds.size.z:0.00}, {c.bounds.size.y:0.00} tall)";
        }
        /// <summary>What is wrong with a leg on foot for a body: where it ends, a hero's body would stand inside something solid; or a body walking it straight from the last leg's end
        /// meets something solid on the way (felt every quarter metre). A walk that finds its way on the floor is felt only where it ends.</summary>
        static string JudgeRoom(string scene,Leg leg)
        {
            string where=$"{leg.route} '{leg.name}' ({scene})";
            if(FloorAt(leg.to) is not Vector3 end)return null;
            var met=InTheWay(end);
            if(met!=null)return $"{where} ends at ({leg.to.x:0.0#}, {leg.to.z:0.0#}), where a hero's body stands inside {Named(met)}";
            if(leg.from is not Vector3 from||leg.findsWay||FloorAt(from) is not Vector3 begin)return null;
            var way=end-begin;way.y=0;int steps=Mathf.CeilToInt(way.magnitude/Along);
            for(int k=1;k<steps;k++)
            {
                if(FloorAt(Vector3.Lerp(begin,end,k/(float)steps)) is not Vector3 floor)continue;
                met=InTheWay(floor);
                if(met!=null)return $"{where}: a hero's body walking straight from ({from.x:0.0#}, {from.z:0.0#}) to ({leg.to.x:0.0#}, {leg.to.z:0.0#}) meets {Named(met)} at ({floor.x:0.00}, {floor.z:0.00})";
            }
            return null;
        }
        public static IEnumerable<string> OnFoot()=>Zones().Where(z=>!Flying(z));

        // ------------------------------------------------------------------ the cases
        [Test] public void EveryChapterNineRouteIsPlayedWithTarenAndIdraOutAndSaysItUsesTheExtra()
        {
            var names=RouteNames();Assert.IsNotEmpty(names,"fixture: no chapter 9 route is written (python tools/home_routes.py)");
            foreach(string name in names){var route=Read(name);CollectionAssert.AreEqual(new[]{"Taren","Idra"},route.deployed,name+" is played with another pair");Assert.IsTrue(route.useExtra,name+" leaves the EXTRA alone");}
        }
        /// <summary>Every press that leaves a zone is filed under the zone it is pressed in, and so judged there against that zone's ways. Filed as before (under the zone it goes to) a
        /// way out was judged against the zone on the far side of it, where nothing of it stands: shown first, then the filing the judgement uses.</summary>
        [Test] public void EveryWayOutIsJudgedInTheZoneItIsPressedIn()
        {
            // Filed under the zone it goes to, a way's press began a stretch of its own, with nobody standing anywhere known in it: it was dropped, and nothing judged it.
            static int Ways(List<Stretch> all)=>all.Sum(s=>s.presses.Count(p=>p.to!=null));
            Assert.AreEqual(0,Ways(Stretches(true)),"fixture: filed as before, some way out was judged after all: the check below proves nothing");
            var stretches=Stretches();
            Assert.AreEqual(0,stretches.Sum(s=>s.presses.Count(p=>p.to!=null&&p.to==s.scene)),"a way out is filed under the zone it goes to");
            var names=RouteNames();var leaves=new List<string>();
            foreach(string name in names)
            {
                var steps=Read(name).steps;
                for(int i=1;i<steps.Length;i++)
                {
                    var step=steps[i];if(step.navigate||step.until==null||!step.until.StartsWith("scene:")||step.buttons==null||!step.buttons.Contains("South")||!string.IsNullOrEmpty(step.pauseUi))continue;
                    string from=steps[i-1].expectedScene,to=step.until.Substring(6);
                    bool judged=stretches.Any(s=>s.route==name&&s.scene==from&&s.presses.Any(p=>p.name==step.name&&p.to==to));
                    if(!judged)leaves.Add($"{name} '{step.name}' (from {from} to {to}) isn't judged in {from}");
                }
            }
            Assert.IsEmpty(leaves,string.Join("\n",leaves));
            Assert.Greater(stretches.Sum(s=>s.presses.Count(p=>p.to!=null)),8,"fixture: the chapter 9 routes leave almost no zone by a way: the check has nothing to judge");
        }
        [Test] public void TheChapterNineRoutesHandOnOneToTheNext()
        {
            // Each resumes from the save the one before it left: its first flag is one the earlier routes have set, in the zone the earlier route ended in.
            var names=RouteNames();var flags=new HashSet<string>{"home.turnedForNacre"};string lastScene="Harrowgate";var faults=new List<string>();
            foreach(string name in names)
            {
                var steps=Read(name).steps;var load=steps.First(s=>s.pickRow=="autosave");
                if(!flags.Contains(load.expectedFlag))faults.Add($"{name} resumes on {load.expectedFlag}, which no route before it sets");
                if(load.expectedScene!=lastScene)faults.Add($"{name} resumes in {load.expectedScene}, and the route before it ended in {lastScene}");
                foreach(var s in steps){if(!string.IsNullOrEmpty(s.expectedFlag))flags.Add(s.expectedFlag);if(s.until!=null&&s.until.StartsWith("flag:"))flags.Add(s.until.Substring(5));}
                lastScene=steps.Last().expectedScene;
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
            if(names.Length==9)Assert.IsTrue(flags.Contains("chapter9.complete"),"the last chapter 9 route never reaches chapter9.complete");
        }
        [UnityTest] public IEnumerator EveryLegPressAndFightOfTheChapterNineRoutesIsOneABodyCanMake([ValueSource(nameof(Zones))] string zone)
        {
            var stretches=Stretches().Where(s=>s.scene==zone).ToList();
            Assert.IsNotEmpty(stretches,"fixture: no chapter 9 route walks "+zone);
            Assert.IsNotNull(GameCatalog.Find<ZoneDef>(zone),"fixture: a route walks in "+zone+", which is no zone");
            var faults=new List<string>();int legs=0,presses=0,locks=0,ways=0;bool proved=false;
            foreach(var s in stretches)
            {
                yield return Load(s);
                if(!proved)
                {
                    // Each check shown to find a broken item first, in this zone: a leg ended in a warp's piece (in flight) or 200 m off the floor; a press made 40 m from where its
                    // walk ended; a lock made 60 m out of its fight's box.
                    proved=true;var leg=s.legs.FirstOrDefault()??stretches.SelectMany(t=>t.legs).First();
                    var solid=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.enabled&&!c.isTrigger&&!(c is CharacterController)&&c.GetComponentInParent<Health>()==null&&c.bounds.size.x<12&&c.bounds.size.z<12
                        &&c.bounds.min.y<1.8f&&c.bounds.max.y>.2f).OrderBy(c=>Flat(c.bounds.center,leg.to)).FirstOrDefault();
                    var broken=new Leg{route="(broken)",name="a leg into what stands there",from=leg.to,to=Flying(zone)&&solid!=null?solid.bounds.center:leg.to+new Vector3(200,0,0),tolerance=.5f};
                    if(!Flying(zone)||solid!=null)Assert.IsNotNull(JudgeLeg(zone,broken),"fixture: the leg check passes a leg "+(Flying(zone)?"into "+solid.name:"200 m off the floor")+" in "+zone+": it proves nothing");
                    var press=stretches.SelectMany(t=>t.presses).FirstOrDefault();
                    if(press!=null)Assert.IsNotNull(JudgePress(zone,new Press{route="(broken)",name=press.name,after=press.after,flag=press.flag,to=press.to,talk=press.talk,saves=press.saves,at=press.at+new Vector3(40,0,0),tolerance=press.tolerance}),
                        "fixture: the press check passes a press made 40 m from its stand in "+zone+": it proves nothing");
                    var l=stretches.SelectMany(t=>t.locks).FirstOrDefault(k=>!k.boss);
                    if(l!=null)Assert.IsNotNull(JudgeLock(zone,new Lock{route="(broken)",name=l.name,id=l.id,after=l.after,at=l.at+new Vector3(60,0,0),tolerance=l.tolerance}),"fixture: the lock check passes a lock made 60 m out of its box in "+zone);
                }
                foreach(var leg in s.legs)
                {
                    legs++;var fault=JudgeLeg(zone,leg);if(fault!=null)faults.Add(fault);
                    if(leg.door==null)continue;
                    var from=Checkpoint(leg.door);
                    if(from==null){faults.Add($"{leg.route} '{leg.name}' is {leg.door}'s door, and no fight of that name here names a checkpoint spawn there is");continue;}
                    fault=JudgeLeg(zone,new Leg{route=leg.route,name=leg.name+" (retried from its checkpoint)",from=from,to=leg.to,tolerance=leg.tolerance});if(fault!=null)faults.Add(fault);
                }
                foreach(var press in s.presses)
                {
                    presses++;if(press.to!=null)ways++;foreach(string flag in press.flags)GameServices.Current.Flags.SetBool(flag,true);
                    yield return null;
                    var fault=JudgePress(zone,press);if(fault!=null)faults.Add(fault);
                }
                foreach(var l in s.locks){locks++;var fault=JudgeLock(zone,l);if(fault!=null)faults.Add(fault);}
            }
            Debug.Log($"HOME_ROUTE_CHECK {zone} stretches={stretches.Count} legs={legs} presses={presses} ways={ways} locks={locks} faults={faults.Count}");
            Assert.IsEmpty(faults,$"the chapter 9 routes walk, press or fight in {zone} where a body can't:\n"+string.Join("\n",faults.Distinct().Take(40)));
        }
        /// <summary>Every walk on foot ends where a hero's body has room to stand, and gets there from the last walk's end without meeting anything solid, in the state the route walks it
        /// in (the Coronach's with every side story done: everyone who can stand in the walk does). The leg check above asks the baked floor, which knows nothing that isn't baked or
        /// carving; this asks the hero's own capsule. Route 60's walk to Oda ended inside Jask, standing on the dock office's front, and its walk back to Orrin's board stopped against
        /// him for good: the first chain to play the Coronach (November 14). Shown first to find a body set where a walk ends and one set on a walk's way, and to let a step be.</summary>
        [UnityTest] public IEnumerator EveryWalkOnFootEndsWhereAHeroHasRoomAndMeetsNothingSolidOnTheWay([ValueSource(nameof(OnFoot))] string zone)
        {
            var stretches=Stretches().Where(s=>s.scene==zone).ToList();
            Assert.IsNotEmpty(stretches,"fixture: no chapter 9 route walks "+zone);
            var faults=new List<string>();int legs=0;bool proved=false;
            foreach(var s in stretches)
            {
                yield return Load(s);
                // Shown on a walk with room: a body set where it ends, then on its way, has to be found, and a step where it ends must not be.
                var sample=proved?null:s.legs.FirstOrDefault(l=>l.from is Vector3 f&&!l.findsWay&&Flat(f,l.to)>=2&&FloorAt(f)!=null&&FloorAt(l.to)!=null&&JudgeRoom(zone,l)==null);
                if(sample!=null)
                {
                    proved=true;string before=null;
                    var end=FloorAt(sample.to).Value;var middle=FloorAt(Vector3.Lerp(sample.from.Value,sample.to,.5f))??Vector3.Lerp(sample.from.Value,sample.to,.5f);
                    var body=new GameObject("(a body standing in the way)",typeof(BoxCollider));var box=body.GetComponent<BoxCollider>();box.size=new Vector3(.7f,1.9f,.7f);box.center=Vector3.up*.95f;
                    try
                    {
                        body.transform.position=end;Physics.SyncTransforms();
                        StringAssert.Contains("(a body standing in the way)",JudgeRoom(zone,sample)??"","fixture: the room check passes a walk that ends inside someone standing there: it proves nothing");
                        body.transform.position=middle;Physics.SyncTransforms();
                        StringAssert.Contains("(a body standing in the way)",JudgeRoom(zone,sample)??"","fixture: the room check passes a walk through someone standing on its way: it proves nothing");
                        // And a step up (a .2 m block where the walk ends) is floor, not a fault.
                        body.transform.position=end;box.size=new Vector3(1,.2f,1);box.center=Vector3.up*.1f;Physics.SyncTransforms();
                        Assert.AreEqual(before,JudgeRoom(zone,sample),"fixture: the room check calls a step a body goes up something in the way");
                    }
                    finally{Object.DestroyImmediate(body);Physics.SyncTransforms();}
                }
                foreach(var leg in s.legs){legs++;var fault=JudgeRoom(zone,leg);if(fault!=null)faults.Add(fault);}
            }
            Assert.IsTrue(proved,"fixture: no straight walk of two metres or more in "+zone+" to show the room check finds a body on it");
            Debug.Log($"HOME_ROUTE_ROOM {zone} stretches={stretches.Count} legs={legs} faults={faults.Count}");
            Assert.IsEmpty(faults,$"a chapter 9 route walks on foot in {zone} where a hero's body has no room:\n"+string.Join("\n",faults.Distinct().Take(40)));
        }
    }
}
