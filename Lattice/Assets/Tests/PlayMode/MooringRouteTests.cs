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
    /// <summary>Chapter 10's replay routes (tools/mooring_routes.py), checked against the built zones in the state each route walks them in: every leg clear of everything solid by
    /// Idra's hull in flight, or on the baked floor on foot (on a shelf or an island too: a leg between a dock and its launch is on foot in a flight zone), a boss's door leg from its
    /// checkpoint as well and the first walk into its box three metres in; every press made where a body can stand, with what it is for in reach and the one the game offers there
    /// (a dock at its berth's pad, from the plane; a launch or a cradle with the party standing on that berth, docked by its own pad); every fight locked from inside its box. A zone is
    /// loaded once for each stretch a route walks it in, with the flags the routes have set by then. Each check is first run on a broken item and has to find it.</summary>
    public sealed class MooringRouteTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter10");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static bool Flying(string scene){var zone=GameCatalog.Find<ZoneDef>(scene);return zone!=null&&(zone.kind==ZoneKind.SpaceCombat||zone.kind==ZoneKind.SpaceSafe);}
        static float Hull=>Mathf.Max(HeroCollision.HullRadius("Sela"),Mathf.Max(HeroCollision.HullRadius("Taren"),HeroCollision.HullRadius("Idra")));
        /// <summary>RouteFightTests' fifteen centimetres: how far inside a reach a route must stand; and how far into a boss's box its door walk ends (the partner behind the lead).</summary>
        const float Inside=.15f,PartnerRoom=3;
        const int First=62;
        /// <summary>A step that loads its zone again: what follows is a new stretch, loaded with the flags as they then are (the passage's turn reloads at Turn).</summary>
        static readonly string[] Restages={"end.turnedBack"};

        // ------------------------------------------------------------------ the routes, read
        static string Folder=>System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
        static string[] RouteNames()=>System.IO.Directory.GetFiles(Folder,"chapter-*.json").Select(System.IO.Path.GetFileNameWithoutExtension)
            .Where(n=>int.TryParse(n.Split('-')[1],out int k)&&k>=First&&k<First+20).OrderBy(n=>n,System.StringComparer.Ordinal).ToArray();
        static QualityRoute Read(string name)=>JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(System.IO.Path.Combine(Folder,name+".json")));
        /// <summary>A leg: where it starts (a point, or a berth's own foot or flight spawn, where a dock or a launch sets the party down, or unknown), where it ends, on which berth.</summary>
        /// <summary>Each leg and press carries its place in the route (<c>order</c>): a stretch is judged in that order, so each is judged with the flags set before it and none of
        /// those set after. Judged all legs first, every press saw the stretch's last flags: Idra's plan was judged heard before it was pressed for, the handrail mended, Orrin quiet
        /// and the tug already on its line, and each check said the game offered nothing (or the chart) where a player is offered the very thing the route presses for.</summary>
        sealed class Leg{public string route,name,door,berth,fromFoot,fromFlight;public Vector3? from;public Vector3 to;public float tolerance;public string[] flags;public int order;}
        sealed class Press{public string route,name,flag,to,after,dock,launch,berth,words;public bool talk,waited,saves;public Vector3 at;public float tolerance;public string[] flags;public int order;}
        sealed class Lock{public string route,name,id,after;public Vector3 at;public float tolerance;public bool boss;}
        sealed class Stretch{public string route,scene;public int index;public string[] flags;public readonly List<Leg> legs=new();public readonly List<Press> presses=new();public readonly List<Lock> locks=new();}
        static string After(string condition,string form)=>condition!=null&&condition.StartsWith(form)?condition.Substring(form.Length):null;
        static List<Stretch> Stretches()
        {
            var all=new List<Stretch>();var flags=new HashSet<string>();int order=0;
            foreach(string name in RouteNames())
            {
                var route=Read(name);Stretch here=null;Vector3? at=null;string after=null,door=null,doorIn=null,on=null,fromFoot=null,fromFlight=null,words=null;float tolerance=0;bool waited=false;
                for(int i=0;i<route.steps.Length;i++)
                {
                    var step=route.steps[i];string scene=string.IsNullOrEmpty(step.expectedScene)?route.scene:step.expectedScene;
                    // A press that takes the party to another zone (a warp, a pad) is made in the zone it leaves: the generator files it under the one it arrives in, and so did
                    // HomeRouteTests, which never judged a way out at all.
                    if(here!=null&&After(step.until,"scene:")!=null&&!step.navigate)scene=here.scene;
                    if(here==null||scene!=here.scene){here=new Stretch{route=name,scene=scene,index=all.Count,flags=flags.ToArray()};all.Add(here);at=null;door=null;doorIn=null;on=null;fromFoot=fromFlight=null;}
                    string docks=After(step.until,"docked:")??After(step.stopWhen,"docked:"),launches=After(step.until,"launched:");
                    if(step.navigate)
                    {
                        if(step.approachTarget||step.approachPartner){at=null;door=null;fromFoot=fromFlight=null;continue;}
                        var leg=new Leg{route=name,name=step.name,from=step.findWay?null:at,to=step.point,tolerance=step.tolerance,door=string.IsNullOrEmpty(step.bossDoor)?null:step.bossDoor,berth=on,fromFoot=fromFoot,fromFlight=fromFlight,flags=flags.ToArray(),order=order++};
                        here.legs.Add(leg);
                        // The first walk after a boss's door that ends in its box: three metres in, so the partner behind the lead is through too.
                        if(doorIn!=null&&leg.door==null){here.locks.Add(new Lock{route=name,name=step.name+" (into the box)",id=doorIn,after=step.name,at=step.point,tolerance=step.tolerance,boss=true});doorIn=null;}
                        if(leg.door!=null)doorIn=leg.door;
                        at=step.point;after=step.name;tolerance=step.tolerance;door=leg.door;waited=false;words=null;fromFoot=fromFlight=null;
                        // A dock is made from the walk: the press is the step's own, at its pad, and the party stands on the berth after it (at its foot spawn).
                        if(docks!=null){here.presses.Add(new Press{route=name,name=step.name,dock=docks,after=step.name,at=step.point,tolerance=step.tolerance,flags=flags.ToArray(),order=order++});on=docks;at=null;fromFoot=docks;}
                    }
                    else if((at is Vector3 stand)&&scene!="Title")
                    {
                        if(!string.IsNullOrEmpty(step.until)&&step.until.StartsWith("prompt:")){waited=true;words=step.until.Substring(7);}
                        if(step.name.StartsWith("lock ")&&step.buttons!=null&&step.buttons.Contains("LeftShoulder"))doorIn=null;
                        if(step.name.StartsWith("lock ")&&step.buttons!=null&&step.buttons.Contains("LeftShoulder"))
                            here.locks.Add(new Lock{route=name,name=step.name,id=door??step.name.Substring(5),after=after,at=stand,tolerance=tolerance,boss=door!=null});
                        bool south=step.buttons!=null&&step.buttons.Length==1&&step.buttons[0]=="South"&&string.IsNullOrEmpty(step.pauseUi)&&string.IsNullOrEmpty(step.pickRow);
                        bool talk=step.expectedUi=="dialogue"||step.until=="ui:dialogue";
                        string wants=After(step.until,"flag:"),to=After(step.until,"scene:");
                        if(south&&(talk||wants!=null||to!=null||step.until=="saved"||launches!=null||docks!=null))
                        {
                            string read=talk?route.steps.Skip(i+1).FirstOrDefault(s=>s.name.StartsWith("finish "))?.expectedFlag:null;
                            here.presses.Add(new Press{route=name,name=step.name,flag=wants??read,to=to,launch=launches,dock=docks,berth=on,words=words,talk=talk,waited=waited,saves=step.until=="saved",after=after,at=stand,tolerance=tolerance,flags=flags.ToArray(),order=order++});
                        }
                    }
                    if(docks!=null&&!step.navigate){on=docks;at=null;fromFoot=docks;fromFlight=null;}
                    if(launches!=null&&!step.navigate){on=null;at=null;fromFlight=launches;fromFoot=null;}
                    // A ride the route waited for (the chamber's lift: its prompt came, its arrival saves) leaves the party somewhere the next walk doesn't start from.
                    if(!step.navigate&&step.until=="saved"&&words!=null){at=null;words=null;}
                    if(!string.IsNullOrEmpty(step.expectedFlag))flags.Add(step.expectedFlag);
                    if(!string.IsNullOrEmpty(step.until)&&step.until.StartsWith("flag:"))flags.Add(step.until.Substring(5));
                    if(Restages.Contains(After(step.until,"flag:"))){here=null;}
                }
            }
            return all;
        }
        public static IEnumerable<string> Zones()=>Stretches().Select(s=>s.scene).Where(s=>s!="Title").Distinct().OrderBy(s=>s,System.StringComparer.Ordinal);

        // ------------------------------------------------------------------ a zone, as the route finds it
        static IEnumerator Load(Stretch s)
        {
            foreach(string flag in s.flags)GameServices.Current.Flags.SetBool(flag,true);
            SceneFlow.Current.LoadZone(s.scene,"Arrival");float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,s.scene+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var hero in PartyController.Current.members){hero.Health.InvulnerableUntil=GameTime.Now+900;var brain=hero.GetComponent<PartnerBrain>();if(brain!=null)brain.enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
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
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();bool was=body.enabled;body.enabled=false;
            if(actor.TryGetComponent<FlightMotor>(out var ship))ship.Halt();if(actor.TryGetComponent<GroundMotor>(out var feet))feet.Halt();
            actor.transform.position=at;body.enabled=was;Physics.SyncTransforms();
        }

        // ------------------------------------------------------------------ the judgements
        /// <summary>Where a leg begins: its point, or the berth's spawn the dock or launch before it set the party down at, or unknown.</summary>
        static Vector3? Start(Leg leg)
        {
            if(leg.from!=null)return leg.from;
            var berth=Berth.Find(leg.fromFoot??leg.fromFlight??"");if(berth==null)return null;
            bool inside=leg.fromFoot!=null;return inside==berth.Flight?berth.flight.position:berth.foot.position;
        }
        /// <summary>What is wrong with a leg: in flight, anything solid within a hull of the way (at the berth's plane, for a flight berth); on foot, its end off the baked floor or the
        /// straight walk cut.</summary>
        static string JudgeLeg(string scene,Leg leg)
        {
            string where=$"{leg.route} '{leg.name}' ({scene}{(leg.berth!=null?", on "+leg.berth:"")})";
            bool flying=Flying(scene)!=(leg.berth!=null);var from=Start(leg);
            if(flying)
            {
                float plane=leg.berth!=null?leg.to.y:1;var to=leg.to;to.y=plane;var met=Physics.OverlapSphere(to+Vector3.up*.8f,Hull,~0,QueryTriggerInteraction.Ignore).AsEnumerable();
                if(from is Vector3 start){start.y=plane;var way=to-start;if(way.magnitude>.01f)met=met.Concat(Physics.SphereCastAll(start+Vector3.up*.8f,Hull,way.normalized,way.magnitude,~0,QueryTriggerInteraction.Ignore).Select(h=>h.collider));}
                var hit=met.FirstOrDefault(c=>c!=null&&!(c is CharacterController)&&c.GetComponentInParent<Health>()==null);
                return hit!=null?$"{where}, a {Hull:0.00} m hull, flies into {hit.name} on the way to {leg.to}":null;
            }
            // On a deck the floor found must be the deck's own (a shelf is 3 m under the plane, over nothing a body walks).
            if(!NavMesh.SamplePosition(leg.to+Vector3.up*.5f,out var end,2.5f,NavMesh.AllAreas)||Flat(end.position,leg.to)>Mathf.Max(.6f,leg.tolerance)||leg.berth!=null&&Mathf.Abs(end.position.y-leg.to.y)>1.2f)
                return $"{where} ends at {leg.to}, off the floor a body walks"+(NavMesh.SamplePosition(leg.to,out var nearest,6,NavMesh.AllAreas)?$" (the nearest floor is at {nearest.position})":"");
            if(from==null)return null;
            if(!NavMesh.SamplePosition(from.Value+Vector3.up*.5f,out var begin,2.5f,NavMesh.AllAreas))return $"{where} starts at {from.Value}, off the floor";
            if(!NavMesh.Raycast(begin.position,end.position,out var cut,NavMesh.AllAreas))return null;
            var path=new NavMeshPath();bool joined=NavMesh.CalculatePath(begin.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
            if(joined&&path.corners.Skip(1).Take(path.corners.Length-2).All(c=>OffTheLine(c,begin.position,end.position)<.1f))return null;
            return $"{where}: the straight walk from {from.Value} to {leg.to} is cut at {cut.position}; "+(joined?$"the floor joins the ends by {string.Join(" ",path.corners.Select(c=>$"({c.x:0.0},{c.z:0.0})"))}":"the floor doesn't join the ends at all");
        }
        static string Field(InteractionPrompt p,string name){var f=p.GetType().GetField(name);return f!=null&&f.FieldType==typeof(string)?(string)f.GetValue(p):null;}
        static bool For(InteractionPrompt p,Press press)
        {
            if(press.words!=null)return p.prompt!=null&&p.prompt.StartsWith(press.words);
            if(press.dock!=null)return p is BerthPrompt way&&!way.wayOut&&way.berth!=null&&way.berth.id==press.dock;
            if(press.launch!=null)return p is BerthPrompt back&&back.wayOut&&back.berth!=null&&back.berth.id==press.launch;
            if(press.to!=null)return Field(p,"scene")==press.to;
            if(press.saves)return p.prompt!=null&&p.prompt.Contains("save");
            return press.flag!=null&&Field(p,"flag")==press.flag;
        }
        static InteractionPrompt[] Prompts()=>Object.FindObjectsByType<InteractionPrompt>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.range>0).ToArray();
        static bool AnyPromptSets(string flag)=>flag!=null&&Prompts().Any(p=>Field(p,"flag")==flag);
        /// <summary>What is wrong with a press: where it is made isn't floor (on foot) or the plane (in flight); nothing it is for is in reach with the walk's tolerance and fifteen
        /// centimetres to spare; or (unless the route waited for the prompt to come) the game offers something else there, or nothing. A press on a berth is judged with the party on it.</summary>
        static string JudgePress(string scene,Press press)
        {
            string where=$"{press.route} '{press.name}' after '{press.after}' ({scene}{(press.berth!=null?", on "+press.berth:"")}, within {press.tolerance:0.##} of ({press.at.x:0.0}, {press.at.z:0.0}))";
            bool flying=Flying(scene)!=(press.berth!=null);
            if(!flying&&(!NavMesh.SamplePosition(press.at+Vector3.up*.5f,out var floor,2.5f,NavMesh.AllAreas)||Flat(floor.position,press.at)>.6f))return where+": no floor a body can stand on there";
            var prompts=Prompts();
            bool known=press.words!=null||press.dock!=null||press.launch!=null||press.to!=null||press.saves||AnyPromptSets(press.flag);
            if(!known&&!press.talk&&press.flag!=null)return null;
            var meant=prompts.Where(p=>known?For(p,press):press.talk?p is Npc:true).ToArray();
            if(meant.Length==0)return where+": the zone has nothing this press is for ("+(press.dock!=null?"the pad of "+press.dock:press.launch!=null?"the launch of "+press.launch:press.to??press.flag??"a talk")+")";
            float Over(InteractionPrompt p){var gap=p.transform.position-press.at;gap.y=0;return gap.magnitude+press.tolerance+Inside-p.range;}
            var nearest=meant.OrderBy(Over).First();
            if(Over(nearest)>0)return $"{where}: '{nearest.name}' at ({nearest.transform.position.x:0.0}, {nearest.transform.position.z:0.0}) reaches {nearest.range:0.##} m, {Over(nearest):0.00} m too far to be surely offered";
            if(press.waited)return null;
            var stand=press.at;stand.y=flying?(press.berth!=null?press.at.y:1):(NavMesh.SamplePosition(press.at+Vector3.up*.5f,out var f2,2.5f,NavMesh.AllAreas)?f2.position.y:press.at.y);
            // A route launches with the deck's fight behind it: what a dock woke on the deck is stood down, as Load stands down the zone's (D404: an awake foe on the deck holds the launch).
            if(press.launch!=null)foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            PromptService.FindInteraction(stand);var offered=PromptService.Interaction;
            Debug.Log($"MOORING_PRESS {press.route} '{press.name}' offered '{(offered!=null?offered.name:"nothing")}', the arrow on '{(PromptService.Wanted!=null?PromptService.Wanted.name:"nothing")}'");
            if(offered==null)return $"{where}: the game offers nothing there (the meant '{nearest.name}' is in reach but not offered: hidden, refused, or waiting on something)";
            if(!(known?For(offered,press):!press.talk||offered is Npc||offered is DiscoveryPoint))return $"{where}: the game offers '{offered.name}' ('{offered.prompt}') there, not '{nearest.name}'";
            return null;
        }
        static string JudgeLock(string scene,Lock l)
        {
            var volume=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(v=>v.encounterId==l.id);
            if(volume==null)return $"{l.route} '{l.name}': {scene} has no fight called {l.id}";
            var box=volume.GetComponent<BoxCollider>();if(box==null)return $"{l.route} '{l.name}': {l.id} has no box to walk into";
            var local=box.transform.InverseTransformPoint(new Vector3(l.at.x,box.transform.position.y,l.at.z))-box.center;var scale=box.transform.lossyScale;
            float outside=Mathf.Max(Mathf.Abs(local.x*scale.x)-box.size.x*Mathf.Abs(scale.x)/2,Mathf.Abs(local.z*scale.z)-box.size.z*Mathf.Abs(scale.z)/2)+l.tolerance+(l.boss?PartnerRoom:0);
            return outside>0?$"{l.route} '{l.name}': after '{l.after}' the party stands within {l.tolerance:0.#} m of ({l.at.x:0.0}, {l.at.z:0.0}), {outside:0.0} m short of surely inside {l.id}'s box{(l.boss?" by three metres":"")}":null;
        }
        /// <summary>The party onto a berth by its own pad, as A does (or off it by its launch), the change waited out. Null, or what refused it.</summary>
        static IEnumerator Change(Berth berth,bool onto,List<string> said)
        {
            var lead=PartyController.Current.Active;
            if(onto){if(Berth.Current==berth)yield break;if(Berth.Current!=null)yield return Change(Berth.Current,false,said);Put(lead,berth.pad.transform.position);}
            else if(Berth.Current!=berth)yield break;
            yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);
            var prompt=onto?berth.pad:berth.launch;
            if(!prompt.Available){said.Add($"{berth.id}'s {(onto?"pad":"launch")} refuses the party (nothing awake near it, no fight under way)");yield break;}
            prompt.Interact();float end=Time.unscaledTime+3;
            while((Berth.Changing||Berth.Current!=(onto?berth:null))&&Time.unscaledTime<end)yield return null;
            if(Berth.Current!=(onto?berth:null))said.Add($"{berth.id}: the {(onto?"dock":"launch")} never finished");
        }

        /// <summary>The flags a leg is walked with set, and every fight they say is won stood begun (its seals open as a won fight's do when it is entered). Whether anything changed.</summary>
        static bool Won(string[] flags)
        {
            bool changed=false;var state=GameServices.Current.Flags;
            foreach(string flag in flags??new string[0])if(!state.GetBool(flag)){state.SetBool(flag,true);changed=true;}
            foreach(var volume in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
                if(!volume.Started&&!string.IsNullOrEmpty(volume.encounterId)&&state.GetBool("clear."+volume.encounterId)){volume.Begin();changed=true;}
            return changed;
        }

        // ------------------------------------------------------------------ the cases
        [Test] public void EveryChapterTenRouteIsPlayedWithTarenAndIdraOutAndSaysItUsesTheExtra()
        {
            var names=RouteNames();Assert.IsNotEmpty(names,"fixture: no chapter 10 route is written (python tools/mooring_routes.py)");
            foreach(string name in names){var route=Read(name);bool epilogue=route.steps.First(s=>s.pickRow=="autosave").expectedScene=="NeighbourhoodAfloat";
                if(!(epilogue&&(route.deployed==null||route.deployed.Length==0)))CollectionAssert.AreEqual(new[]{"Taren","Idra"},route.deployed,name+" is played with another pair");Assert.IsTrue(route.useExtra,name+" leaves the EXTRA alone");}
        }
        [Test] public void TheChapterTenRoutesHandOnOneToTheNextFromChapterNinesLast()
        {
            var names=RouteNames();var flags=new HashSet<string>{"chapter9.complete"};string lastScene="ManyHands";var faults=new List<string>();
            foreach(string name in names)
            {
                var steps=Read(name).steps;var load=steps.First(s=>s.pickRow=="autosave");
                if(!flags.Contains(load.expectedFlag))faults.Add($"{name} resumes on {load.expectedFlag}, which no route before it sets");
                if(load.expectedScene!=lastScene)faults.Add($"{name} resumes in {load.expectedScene}, and the route before it ended in {lastScene}");
                foreach(var s in steps){if(!string.IsNullOrEmpty(s.expectedFlag))flags.Add(s.expectedFlag);if(s.until!=null&&s.until.StartsWith("flag:"))flags.Add(s.until.Substring(5));}
                lastScene=steps.Last().expectedScene;
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
        /// <summary>A berth is left by its launch before its zone is left or its route ends: a party left on a shelf is a save that names the shelf's own spawn, and a route after it that
        /// starts at the pad it never launched from. Each dock's launch is the same berth's, and nothing docks twice without launching between. Shown first on a route built wrong.</summary>
        [Test] public void EveryChapterTenRouteLaunchesFromEachBerthItDocksAt()
        {
            static List<string> Faults(string name,QualityStep[] steps)
            {
                var faults=new List<string>();string on=null,scene=null;
                foreach(var s in steps)
                {
                    string here=s.expectedScene;
                    if(After(s.until,"scene:")!=null)on=null;
                    if(on!=null&&here!=scene)faults.Add($"{name} '{s.name}' leaves {scene} still on {on}");
                    scene=here;
                    string docks=After(s.until,"docked:"),launches=After(s.until,"launched:");
                    if(docks!=null){if(on!=null&&on!=docks)faults.Add($"{name} '{s.name}' docks at {docks} still on {on}");on=docks;}
                    if(launches!=null){if(on!=launches)faults.Add($"{name} '{s.name}' launches from {launches}, and the party stands on {on??"nothing"}");on=null;}
                }
                if(on!=null)faults.Add($"{name} ends still on {on}");
                return faults;
            }
            var broken=new[]{new QualityStep{name="dock",expectedScene="SorrelSecondSky",until="docked:Clamp1"},new QualityStep{name="work",expectedScene="SorrelSecondSky"}};
            Assert.IsNotEmpty(Faults("(broken)",broken),"fixture: a route that ends on a shelf passes the check: it proves nothing");
            var names=RouteNames();Assert.IsNotEmpty(names,"fixture: no chapter 10 route is written");
            var all=names.SelectMany(n=>Faults(n,Read(n).steps)).ToList();
            Assert.IsEmpty(all,string.Join("\n",all));
        }
        [UnityTest] public IEnumerator EveryLegPressDockAndFightOfTheChapterTenRoutesIsOneABodyCanMake([ValueSource(nameof(Zones))] string zone)
        {
            var stretches=Stretches().Where(s=>s.scene==zone).ToList();
            Assert.IsNotEmpty(stretches,"fixture: no chapter 10 route walks "+zone);
            Assert.IsNotNull(GameCatalog.Find<ZoneDef>(zone),"fixture: a route walks in "+zone+", which is no zone");
            var faults=new List<string>();int legs=0,presses=0,locks=0,berths=0;bool proved=false;
            foreach(var s in stretches)
            {
                yield return Load(s);
                if(!proved)
                {
                    // Each check shown to find a broken item first, in this zone: a leg ended in something solid (in flight) or 200 m off the floor; on a berth, 30 m off its deck; a press
                    // made 40 m from where its walk ended; a lock made 60 m out of its fight's box.
                    proved=true;var leg=s.legs.FirstOrDefault(l=>l.berth==null)??stretches.SelectMany(t=>t.legs).First();
                    var solid=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.enabled&&!c.isTrigger&&!(c is CharacterController)&&c.GetComponentInParent<Health>()==null&&c.bounds.size.x<12&&c.bounds.size.z<12
                        &&c.bounds.min.y<1.8f&&c.bounds.max.y>.2f).OrderBy(c=>Flat(c.bounds.center,leg.to)).FirstOrDefault();
                    var broken=new Leg{route="(broken)",name="a leg into what stands there",from=leg.to,to=Flying(zone)&&solid!=null?solid.bounds.center:leg.to+new Vector3(200,0,0),tolerance=.5f};
                    if(!Flying(zone)||solid!=null)Assert.IsNotNull(JudgeLeg(zone,broken),"fixture: the leg check passes a leg "+(Flying(zone)?"into "+solid.name:"200 m off the floor")+" in "+zone+": it proves nothing");
                    var deck=stretches.SelectMany(t=>t.legs).FirstOrDefault(l=>l.berth!=null);
                    if(deck!=null&&Berth.Find(deck.berth)!=null&&!Berth.Find(deck.berth).Flight)Assert.IsNotNull(JudgeLeg(zone,new Leg{route="(broken)",name="off the deck",berth=deck.berth,from=null,to=deck.to+new Vector3(30,0,0),tolerance=.4f}),
                        "fixture: the leg check passes a walk 30 m off "+deck.berth+"'s deck in "+zone+": it proves nothing");
                    var press=stretches.SelectMany(t=>t.presses).FirstOrDefault(p=>p.berth==null);
                    if(press!=null)Assert.IsNotNull(JudgePress(zone,new Press{route="(broken)",name=press.name,after=press.after,flag=press.flag,to=press.to,dock=press.dock,talk=press.talk,saves=press.saves,at=press.at+new Vector3(40,0,0),tolerance=press.tolerance}),
                        "fixture: the press check passes a press made 40 m from its stand in "+zone+": it proves nothing");
                    var l=stretches.SelectMany(t=>t.locks).FirstOrDefault(k=>!k.boss);
                    if(l!=null)Assert.IsNotNull(JudgeLock(zone,new Lock{route="(broken)",name=l.name,id=l.id,after=l.after,at=l.at+new Vector3(60,0,0),tolerance=l.tolerance}),"fixture: the lock check passes a lock made 60 m out of its box in "+zone);
                }
                // In the zone's own form first, in the route's own order (each leg and press with the flags set before it and none set after), then each berth in the order the
                // route stands on it, the party docked there by its own pad.
                var inOrder=s.legs.Where(g=>g.berth==null).Select(g=>(g.order,(object)g)).Concat(s.presses.Where(p=>p.berth==null).Select(p=>(p.order,(object)p))).OrderBy(t=>t.order).Select(t=>t.Item2).ToList();
                foreach(object item in inOrder)
                {
                    if(item is Leg leg)
                    {
                        legs++;if(Won(leg.flags))yield return new WaitForSecondsRealtime(.4f);
                        var fault=JudgeLeg(zone,leg);if(fault!=null)faults.Add(fault);
                        if(leg.door==null)continue;
                        var from=Checkpoint(leg.door);
                        if(from==null){faults.Add($"{leg.route} '{leg.name}' is {leg.door}'s door, and no fight of that name here names a checkpoint spawn there is");continue;}
                        fault=JudgeLeg(zone,new Leg{route=leg.route,name=leg.name+" (retried from its checkpoint)",from=from,to=leg.to,tolerance=leg.tolerance});if(fault!=null)faults.Add(fault);
                    }
                    else if(item is Press press)
                    {
                        presses++;foreach(string flag in press.flags)GameServices.Current.Flags.SetBool(flag,true);
                        if(press.dock!=null&&Berth.Find(press.dock)==null){faults.Add($"{press.route} '{press.name}' docks at {press.dock}, and {zone} has no berth of that name");continue;}
                        yield return null;var fault=JudgePress(zone,press);if(fault!=null)faults.Add(fault);
                    }
                }
                foreach(var id in s.legs.Select(g=>g.berth).Concat(s.presses.Select(p=>p.berth)).Where(b=>b!=null).Distinct().ToList())
                {
                    var berth=Berth.Find(id);if(berth==null){faults.Add($"{s.route} stands on {id}, and {zone} has no berth of that name");continue;}
                    // Docked with the flags as the route stood at its dock, and none set after: Mother of Doors' shelves take the party only in their turn, and with every clamp the
                    // route opens already open, the first three said "next" and took nobody (November 15).
                    var dockedBy=s.presses.FirstOrDefault(p=>p.dock==id);
                    if(dockedBy!=null)
                    {
                        var after=new HashSet<string>(s.legs.SelectMany(g=>g.flags).Concat(s.presses.SelectMany(p=>p.flags)));after.ExceptWith(dockedBy.flags);
                        foreach(string flag in after)GameServices.Current.Flags.SetBool(flag,false);foreach(string flag in dockedBy.flags)GameServices.Current.Flags.SetBool(flag,true);
                    }
                    berths++;var said=new List<string>();yield return Change(berth,true,said);faults.AddRange(said.Select(w=>s.route+": "+w));if(said.Count>0)continue;
                    foreach(var leg in s.legs.Where(g=>g.berth==id)){legs++;var fault=JudgeLeg(zone,leg);if(fault!=null)faults.Add(fault);}
                    foreach(var press in s.presses.Where(p=>p.berth==id))
                    {
                        presses++;foreach(string flag in press.flags)GameServices.Current.Flags.SetBool(flag,true);
                        yield return null;var fault=JudgePress(zone,press);if(fault!=null)faults.Add(fault);
                    }
                    said.Clear();yield return Change(berth,false,said);faults.AddRange(said.Select(w=>s.route+": "+w));
                }
                foreach(var l in s.locks){locks++;var fault=JudgeLock(zone,l);if(fault!=null)faults.Add(fault);}
            }
            Debug.Log($"MOORING_ROUTE_CHECK {zone} stretches={stretches.Count} legs={legs} presses={presses} berths={berths} locks={locks} faults={faults.Count}");
            Assert.IsEmpty(faults,$"the chapter 10 routes walk, press, dock or fight in {zone} where a body can't:\n"+string.Join("\n",faults.Distinct().Take(40)));
        }
    }
}
