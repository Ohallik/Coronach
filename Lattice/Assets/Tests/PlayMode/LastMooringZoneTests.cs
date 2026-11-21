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
    /// <summary>Last Mooring as a player meets it, in the built scene (chapter 10; rule: arenas divided by anchor chains). Places are the map brief's (docs/maps/LastMooring.md), which are the
    /// builder's own (LastMooringBuilder.Mooring): the ways in and out and when each is drawn, the arenas a hull flies in and the chains that divide them (solid but at their gaps), the gate
    /// shut until the Tollkeeper's motor dies, Many Hands' galley quarter struck in the frame of the crossing that strikes it and Maru answering after the bark, the islands and the arm as berths
    /// walked from their foot spawns, nothing over the plane on the camera's side of a fight but the chains, the fights where the map says with mouths every hero can enter, the convoy as every
    /// place the player knows, and its first hulls going through once the gate opens.</summary>
    public sealed class LastMooringZoneTests
    {
        readonly List<string> log=new();
        static readonly string[] Heeded={"LEAVES_ALONG","BERTH_","DISCOVERY"};
        void Logged(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();Time.timeScale=1;SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter10");log.Clear();Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;Application.logMessageReceived-=Logged;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the zone's places (LastMooringBuilder.Mooring; docs/maps/LastMooring.md)
        const string Mooring="LastMooring";const float Plane=1,OuterX=58,SouthZ=-18,GateZ=470,GateHalf=40,Idra=2.17f,ShelfY=-2;
        /// <summary>Each chain line across the anchorage: its z and its one gap.</summary>
        static readonly (string id,float z,float from,float to)[] Lines={("A",60,-8,8),("B",160,12,30),("C",270,-30,-12),("D",386,-8,8)};
        static readonly Vector3 Arrival=new(0,1,0),HomeWarp=new(-10,1,-4),Checkpoint=new(0,1,372),DockAt=new(0,1,524),Crossing=new(0,1,172),Galley=new(21,-12,186);
        static readonly Vector3 Lane1=new(0,1,110),Lane2=new(0,1,326),GunWest=new(-35,1,267),GunEast=new(-7,1,267);

        // ------------------------------------------------------------------ fixtures
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Zone(string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(Mooring,spawn);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Mooring+" never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            // Out of the way of every check of the air: two hulls parked over the anchorage, far above it.
            foreach(var actor in PartyController.Current.members)actor.transform.position+=Vector3.up*300;
            yield return null;yield return null;Physics.SyncTransforms();
        }
        /// <summary>Whether a hull of Idra's reach, at the plane, meets anything solid here (her sphere's middle is .8 over the ship's root). Bodies (a gun, an enemy) aren't the air's.</summary>
        static bool Solid(Vector3 at)=>Physics.OverlapSphere(new Vector3(at.x,Plane+.8f,at.z),Idra,~0,QueryTriggerInteraction.Ignore).Any(c=>c.GetComponentInParent<Health>()==null);
        /// <summary>The points a ship flies through: each arena on a 6 m grid, kept a hull and a chain's half width (2.5) off every chain line, the pylons and the warp.</summary>
        static IEnumerable<(string where,Vector3 at)> Arenas()
        {
            var lines=new[]{SouthZ}.Concat(Lines.Select(l=>l.z)).Concat(new[]{GateZ}).ToArray();
            for(int k=1;k<lines.Length;k++)
            {
                string name=k==lines.Length-1?"the Tollkeeper's arena":"arena "+k;
                for(float z=lines[k-1]+Idra+3;z<=lines[k]-Idra-3;z+=6)for(float x=-OuterX+Idra+3;x<=OuterX-Idra-3;x+=6)
                {
                    var p=new Vector3(x,Plane,z);
                    if(Flat(p,HomeWarp)<Idra+2)continue;
                    // The Tollkeeper's dividing chains, x -18 and 18 from z 396 to 416, with their pylons.
                    if(z>393-Idra&&z<419+Idra&&(Mathf.Abs(x+18)<Idra+2.6f||Mathf.Abs(x-18)<Idra+2.6f))continue;
                    yield return (name,p);
                }
            }
        }
        /// <summary>Whether a point is in the frame the flight camera shows of a ship at <paramref name="ship"/> (pitch 48, distance 26, 30 degrees tall at 16:9), well inside its edges.</summary>
        static bool InFrame(Vector3 ship,Vector3 point)
        {
            var go=new GameObject("Frame probe",typeof(Camera));var camera=go.GetComponent<Camera>();camera.enabled=false;camera.fieldOfView=30;camera.aspect=16f/9;camera.farClipPlane=1000;
            var rotation=Quaternion.Euler(48,0,0);camera.transform.SetPositionAndRotation(ship-rotation*Vector3.forward*26,rotation);
            var v=camera.WorldToViewportPoint(point);Object.Destroy(go);
            return v.z>0&&v.x>.08f&&v.x<.92f&&v.y>.08f&&v.y<.92f;
        }

        // ------------------------------------------------------------------ the ways in and out
        [UnityTest] public IEnumerator LastMooringOpensInFlightWithTheWayHomeInFrameAndTheDockDrawnOnlyOnceTheGateIsOpen()
        {
            yield return Zone();
            Assert.AreEqual(ZoneKind.SpaceCombat,ZoneController.Current.definition.kind,"Last Mooring is a flight zone");
            var home=All<WarpBeacon>().FirstOrDefault(w=>w.scene=="ManyHandsHull");
            Assert.IsNotNull(home,"no way home to Many Hands' hull");Assert.AreEqual("Mooring",home.spawn,"the way home lands at the hull's Mooring spawn");
            Assert.IsTrue(string.IsNullOrEmpty(home.requiredFlag),"the way home is always open");
            Assert.Less(Flat(home.transform.position,Arrival),14,"the way home stands in the arrival's frame");Assert.Greater(Flat(home.transform.position,Arrival),6.5f,"the arrival is inside the way home's reach");
            foreach(var id in new[]{"Arrival","Gate","Continuance","Island1Pad","Island2Pad","ArmReturn"})Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id==id),"Last Mooring has no spawn "+id);
            var dock=All<DockingPad>().FirstOrDefault(d=>d.scene=="Continuance");
            Assert.IsNotNull(dock,"no dock at the Continuance");Assert.AreEqual("Spine",dock.spawn,"the dock doesn't land at the spine's foot");Assert.AreEqual("Dock — The Continuance",dock.prompt);
            Assert.IsFalse(dock.gameObject.activeInHierarchy,"the dock is drawn before the gate is open (D283: a way that doesn't exist yet isn't drawn)");
            Assert.IsTrue(Solid(new Vector3(0,Plane,GateZ)),"the gate doesn't close the way north before the Tollkeeper's motor dies");
            Assert.IsTrue(Solid(new Vector3(GateHalf-6,Plane,GateZ)),"the gate is open at its edge before the motor dies");
            Flag("bossdown.Tollkeeper");Flag("end.gateOpen");yield return Zone("Continuance");
            dock=All<DockingPad>().First(d=>d.scene=="Continuance");
            Assert.IsTrue(dock.gameObject.activeInHierarchy,"the dock isn't drawn once the gate is open");Assert.Less(Flat(dock.transform.position,DockAt),1,"the dock isn't at the spine's head");
            Assert.IsFalse(Solid(new Vector3(0,Plane,GateZ)),"the gate still stands once the Tollkeeper's motor has died");
            var back=All<SpawnPoint>().First(s=>s.id=="Continuance");
            Assert.Less(Flat(back.transform.position,DockAt),16,"the launch from the Continuance doesn't land beside its dock");Assert.Greater(Flat(back.transform.position,DockAt),dock.range+1,"a party back from the Continuance lands inside the dock's reach");
            Assert.IsFalse(Solid(back.transform.position),"the spawn beside the dock is inside something solid");
        }

        [UnityTest] public IEnumerator EveryArenaIsOpenAirForIdrasHullAndEveryChainIsSolidButAtItsGap()
        {
            yield return Zone();
            // The check first, on a box stood in the first arena: it must be found.
            var probe=GameObject.CreatePrimitive(PrimitiveType.Cube);probe.transform.position=new Vector3(30,Plane,100);probe.transform.localScale=new Vector3(2,4,2);Physics.SyncTransforms();
            Assert.IsTrue(Arenas().Any(p=>Solid(p.at)),"fixture: a box in the first arena isn't found by the check");Object.Destroy(probe);yield return null;Physics.SyncTransforms();
            var blocked=Arenas().Where(p=>Solid(p.at)).Select(p=>p.where+" at "+p.at).ToList();
            CollectionAssert.IsEmpty(blocked.Take(12),"a hull meets something solid in an arena's open air ("+blocked.Count+" points)");
            foreach(var (id,z,from,to) in Lines)
            {
                for(float x=-OuterX+3;x<=OuterX-3;x+=4)
                {
                    if(x>from-1&&x<to+1)continue;
                    Assert.IsTrue(Physics.CheckSphere(new Vector3(x,Plane+.8f,z),1,~0,QueryTriggerInteraction.Ignore),"chain "+id+" lets a ship through at x "+x);
                }
                // Its gap takes two hulls side by side: open for Idra's either side of its middle.
                float middle=(from+to)*.5f;
                foreach(float side in new[]{-1f,1f})Assert.IsFalse(Solid(new Vector3(middle+side*2.2f,Plane,z)),"chain "+id+"'s gap doesn't take two hulls side by side");
            }
            foreach(var (where,at) in new[]{("west of the anchorage",new Vector3(-OuterX,Plane,200)),("east of the anchorage",new Vector3(OuterX,Plane,200)),("behind the arrival",new Vector3(0,Plane,SouthZ))})
                Assert.IsTrue(Physics.CheckSphere(new Vector3(at.x,Plane+.8f,at.z),1.2f,~0,QueryTriggerInteraction.Ignore),"nothing stops a ship "+where);
        }

        // ------------------------------------------------------------------ the section struck
        [UnityTest] public IEnumerator ManyHandsGalleyQuarterIsStruckInTheCrossingsFrameAndMaruAnswersAfterTheBark()
        {
            yield return Zone();
            var crossing=All<DiscoveryPoint>().FirstOrDefault(p=>p.flag=="end.sectionStruck");
            Assert.IsNotNull(crossing,"nothing sets end.sectionStruck");Assert.IsTrue(crossing.startsOnCrossing,"the section isn't struck by crossing");
            Assert.AreEqual("sectionhit",crossing.situation,"the lead doesn't say sectionhit as the section is struck");Assert.IsTrue(string.IsNullOrEmpty(crossing.dialogueNode),"the crossing opens a talk before the bark");
            var box=crossing.GetComponent<BoxCollider>().bounds;
            Assert.LessOrEqual(box.min.x,-OuterX+2,"the crossing can be flown round to the west");Assert.GreaterOrEqual(box.max.x,OuterX-2,"the crossing can be flown round to the east");
            Assert.IsTrue(box.min.y<=Plane&&box.max.y>=Plane,"the crossing isn't at the plane");
            Assert.IsTrue(box.center.z>160+4&&box.center.z<160+20,"the crossing isn't just past chain B's gap");
            var breach=All<Transform>().FirstOrDefault(t=>t.name=="The galley quarter's breach");Assert.IsNotNull(breach,"nothing is struck");
            Assert.Less(Flat(breach.position,Galley),3,"what is struck isn't Many Hands' galley quarter");
            // In the frame of a ship crossing anywhere through chain B's gap. The check first: from the arrival it is not.
            Assert.IsFalse(InFrame(Arrival,breach.position),"fixture: the frame check finds the galley from the arrival");
            foreach(float x in new[]{14f,21,28})Assert.IsTrue(InFrame(new Vector3(x,Plane,box.center.z),breach.position),"the galley quarter isn't in the frame of a ship crossing at x "+x);
            var maru=All<AfterTalk>().FirstOrDefault(t=>t.node=="MaruSectionHeld");
            Assert.IsNotNull(maru,"nothing plays MaruSectionHeld");Assert.AreEqual("end.sectionStruck",maru.flag);Assert.AreEqual("end.sectionHeld",maru.heardFlag);Assert.AreEqual("Maru",maru.speaker);
            Assert.GreaterOrEqual(maru.delay,3.5f,"Maru speaks over the lead's bark (a bark is on the panel 3.5 s)");
            Assert.IsTrue(All<RadioHail>().Any(h=>h.speaker=="Maru"&&h.situations.Contains("hail.mats")&&h.needs.Contains("end.sectionHeld")),"nobody hears the mats holding after it");
            Assert.IsFalse(breach.gameObject.activeInHierarchy,"the galley quarter is struck before the crossing");
            Flag("end.sectionStruck");yield return null;yield return null;
            Assert.IsTrue(breach.gameObject.activeInHierarchy,"the galley quarter isn't struck as the flag is set");
            var air=All<Transform>().First(t=>t.name=="The air going");var mats=All<Transform>().First(t=>t.name=="Driftreed's mats over the breach");
            Assert.IsTrue(air.gameObject.activeInHierarchy,"no air goes out of the breach");Assert.IsFalse(mats.gameObject.activeInHierarchy,"the mats are on before Maru's weavers lay them");
            Flag("end.sectionHeld");yield return null;yield return null;
            Assert.IsFalse(air.gameObject.activeInHierarchy,"the air still goes once the section holds");Assert.IsTrue(mats.gameObject.activeInHierarchy,"the mats aren't over the breach once it holds");
        }

        // ------------------------------------------------------------------ the berths
        [UnityTest] public IEnumerator EachIslandAndTheArmIsABerthUnderThePlaneWalkedFromItsFootSpawn()
        {
            yield return Zone();
            // A ship over a shelf is not in it: the box's top stays under Idra's hull flying over (its sphere's bottom at y -.37). The check first, on a box reaching y 0.
            bool UnderHulls(Bounds b)=>b.max.y<Plane+.8f-Idra;
            Assert.IsFalse(UnderHulls(new Bounds(new Vector3(0,-1,0),new Vector3(4,2,4))),"fixture: a box reaching y 0 passes for under the hulls");
            foreach(var (id,inFight) in new[]{("Island1",false),("Island2",false),("Arm",true)})
            {
                var berth=Berth.Find(id);Assert.IsNotNull(berth,id+" has no berth");Assert.AreEqual(inFight,berth.inFight,id+"'s pad answers in a fight the wrong way");
                Assert.IsTrue(UnderHulls(new Bounds(berth.transform.position,berth.boxSize)),id+"'s box reaches the plane: a ship over it would be in it");
                Assert.Less(Flat(berth.pad.transform.position,berth.transform.position),.5f,id+"'s pad isn't over its middle");Assert.AreEqual(Plane,berth.pad.transform.position.y,.01f,id+"'s pad isn't at the plane");
                Assert.IsTrue(NavMesh.SamplePosition(berth.foot.position,out var from,1.5f,NavMesh.AllAreas),id+"'s foot spawn has no floor in the bake");
                foreach(var goal in new[]{berth.launch.transform.position,berth.transform.position})
                {
                    Assert.IsTrue(NavMesh.SamplePosition(goal,out var to,1.5f,NavMesh.AllAreas),id+" has no floor at "+goal);
                    var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,id+": "+goal+" can't be walked to from the foot spawn");
                }
                Assert.Greater(Flat(berth.foot.position,berth.launch.transform.position),3.5f,id+"'s foot spawn is inside its launch's reach");
                Assert.Greater(Flat(berth.flight.position,berth.pad.transform.position),7,id+"'s flight place is inside its pad's reach");
                Assert.IsFalse(Solid(berth.flight.position),id+"'s flight place is inside something solid");
                Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id==berth.returnSpawn),id+"'s return spawn isn't in the zone");
            }
            var cradle=All<RepairBay>().FirstOrDefault(r=>Berth.Find("Island1").Contains(r.transform.position+Vector3.up*.3f));
            Assert.IsNotNull(cradle,"island 1 has no cradle on it");Assert.IsTrue(cradle.free,"island 1's cradle isn't free");Assert.AreEqual("Island1Pad",cradle.saveSpawn,"a save at the cradle doesn't name the island's own return spawn");
            var held=All<EncounterVolume>().FirstOrDefault(e=>e.encounterId=="Mooring_Island");Assert.IsNotNull(held,"island 2 has no fight");
            var heldBox=held.GetComponent<BoxCollider>().bounds;Assert.IsTrue(UnderHulls(heldBox),"island 2's fight starts as a ship flies over it");
            Assert.IsFalse(heldBox.Contains(Berth.Find("Island2").foot.position),"island 2's fight starts as the party is set down");
            Assert.IsTrue(held.spawners.All(s=>Berth.Find("Island2").Contains(s.transform.position+Vector3.up*.5f)),"island 2's pair doesn't stand on the island");
            Assert.IsNotNull(All<Transform>().FirstOrDefault(t=>t.name=="Gate motor seat"&&t.IsChildOf(Berth.Find("Arm").transform.parent)),"the arm has no seat for its gate motor under the root the fight drives");
        }

        // ------------------------------------------------------------------ the camera's side
        [UnityTest] public IEnumerator NothingStandsOverThePlaneSouthOfTheGateButTheChainsAndTheConvoyHangsUnderIt()
        {
            yield return Zone();
            // South of the gate line no drawn thing stands more than two metres over the plane (the chains' links and the count on them, the pylons' clamps, the warp's beacon are lower);
            // the cage at the gate is the gate. Fighters are their own. Every hull of the convoy hangs at least two metres under the plane.
            List<string> Over()
            {
                var found=new List<string>();
                foreach(var r in All<Renderer>())
                {
                    if(!r.gameObject.activeInHierarchy||!r.enabled||r is ParticleSystemRenderer)continue;
                    if(r.GetComponentInParent<CombatActor>(true)!=null||r.GetComponentInParent<TMPro.TMP_Text>()!=null||r.transform.root.name=="The Tollkeeper's gate")continue;
                    var b=r.bounds;if(b.min.z>GateZ-1||b.max.y<=Plane+2)continue;
                    if(b.max.z<SouthZ-1||Mathf.Abs(b.center.x)>OuterX+6)continue;
                    found.Add(r.name+" at "+b.center+" to y "+b.max.y.ToString("0.0"));
                }
                return found;
            }
            var tooth=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tooth.name="A post on the camera's side";tooth.transform.position=new Vector3(10,3,280);tooth.transform.localScale=new Vector3(2,3,2);
            Assert.IsTrue(Over().Any(f=>f.StartsWith("A post")),"fixture: a post over the plane south of the gate isn't found");Object.Destroy(tooth);yield return null;
            CollectionAssert.IsEmpty(Over(),"something stands over the plane on the camera's side of the anchorage's fights");
            var convoy=All<Transform>().Where(t=>t.parent==null&&(t.name=="The convoy, moored"||t.name=="Many Hands, moored")).ToList();Assert.AreEqual(2,convoy.Count,"fixture: the convoy and Many Hands aren't in the scene");
            // A particle's renderer is bounded by what it has emitted, not by where it stands (an idle one reads as the world's origin): left out, as in the check above.
            var high=convoy.SelectMany(t=>t.GetComponentsInChildren<Renderer>()).Where(r=>!(r is ParticleSystemRenderer)&&r.bounds.max.y>Plane-2).Select(r=>r.name+" to y "+r.bounds.max.y.ToString("0.0")).ToList();
            CollectionAssert.IsEmpty(high.Take(10),"a hull of the convoy comes within two metres of the plane, where a ship would seem to meet it");
        }

        // ------------------------------------------------------------------ the fights
        [UnityTest] public IEnumerator TheFightsAreWhereTheMapSaysAndEveryHeroCanEnterThem()
        {
            yield return Zone();
            EncounterVolume Fight(string id){var e=All<EncounterVolume>().FirstOrDefault(v=>v.encounterId==id);Assert.IsNotNull(e,"no fight "+id);return e;}
            // A fight's mouth: the longest run of its south face at the plane a hull can fly in through. The check first, on a box whose mouth is walled but for 4 m.
            float Mouth(Bounds b){float run=0,best=0;for(float x=b.min.x+Idra;x<=b.max.x-Idra;x+=1){if(Solid(new Vector3(x,Plane,b.min.z+Idra+.5f))){run=0;continue;}run+=1;best=Mathf.Max(best,run);}return best;}
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(-6,Plane,95);wall.transform.localScale=new Vector3(100,4,2);
            var narrow=GameObject.CreatePrimitive(PrimitiveType.Cube);narrow.transform.position=new Vector3(48,Plane,95);narrow.transform.localScale=new Vector3(8,4,2);Physics.SyncTransforms();
            Assert.Less(Mouth(new Bounds(new Vector3(0,Plane,110),new Vector3(110,8,40))),9,"fixture: a box walled but for a gap a hull can't take passes for open");
            Object.Destroy(wall);Object.Destroy(narrow);yield return null;Physics.SyncTransforms();
            foreach(var (id,at) in new[]{("Mooring_Lane1",Lane1),("Mooring_Lane2",Lane2)})
            {
                var e=Fight(id);var b=e.GetComponent<BoxCollider>().bounds;
                Assert.Less(Flat(b.center,at),1,id+" isn't where the map says");Assert.IsTrue(b.min.y<Plane&&b.max.y>Plane,id+"'s box doesn't span the plane");
                Assert.GreaterOrEqual(Mouth(b),9,id+"'s mouth won't take two hulls");Assert.AreEqual(2,e.spawners.Sum(s=>s.count),id+" isn't a pair");
            }
            var gap=Fight("Mooring_Gap");var g=gap.GetComponent<BoxCollider>().bounds;
            Assert.IsTrue(g.Contains(new Vector3(-21,Plane,270)),"the guns' fight doesn't hold chain C's gap");Assert.AreEqual(2,gap.spawners.Length,"the gap isn't held by two guns");
            var west=gap.spawners.OrderBy(s=>s.transform.position.x).First();var east=gap.spawners.OrderBy(s=>s.transform.position.x).Last();
            Assert.Less(Vector3.Distance(west.transform.position,GunWest),.5f);Assert.Less(Vector3.Distance(east.transform.position,GunEast),.5f);
            Assert.IsTrue(west.faceAsPlaced&&east.faceAsPlaced,"the guns don't stand as placed");
            Assert.Greater(Vector3.Dot(west.transform.forward,Vector3.right),.9f,"the west gun isn't turned toward the east one");Assert.Greater(Vector3.Dot(east.transform.forward,Vector3.left),.9f,"the east gun isn't turned toward the west one");
            Assert.AreEqual(2,Fight("Mooring_Island").spawners.Sum(s=>s.count),"island 2 isn't held by a pair");
        }

        // ------------------------------------------------------------------ the convoy
        [UnityTest] public IEnumerator TheConvoyIsEveryPlaceThePlayerKnowsMooredOnTheChains()
        {
            yield return Zone();
            Transform Named(string name)=>All<Transform>().FirstOrDefault(t=>t.name==name&&t.GetComponentInChildren<Renderer>()!=null);
            Assert.IsNull(Named("Convoy: Nothing"),"fixture: the finder finds what isn't there");
            var missing=new[]{"District: the market","District: the repair quarter","District: the residential crescent","District: the tug dock","Many Hands","Convoy: TallowStationHull","Convoy: SeedBarge",
                "Convoy: ReedRaft","Convoy: CrawlTread","Convoy: LanternTower","Convoy: KeptLonghouse","Convoy: RibArch","Convoy: PatrolCutter"}.Where(n=>Named(n)==null).ToList();
            Assert.IsTrue(Named("Convoy: RingSegmentA")!=null||Named("Convoy: RingSegmentB")!=null,"the Halo's ring segments aren't in the convoy");
            CollectionAssert.IsEmpty(missing,"a place the player knows isn't in the convoy");
            var lines=All<MeshFilter>().FirstOrDefault(f=>f.name=="Mooring lines");Assert.IsNotNull(lines,"nothing moors the convoy to the chains");Assert.Greater(lines.sharedMesh.vertexCount,400,"too few mooring lines");
        }

        [UnityTest] public IEnumerator TheFirstHullsGoThroughOnceTheMotorDiesAndAZoneOpenedAfterHasThemGone()
        {
            yield return Zone("Gate");
            var talk=All<AfterTalk>().FirstOrDefault(t=>t.node=="OrrinGateOpen");
            Assert.IsNotNull(talk,"nothing plays OrrinGateOpen");Assert.AreEqual("bossdown.Tollkeeper",talk.flag);Assert.AreEqual("end.gateOpen",talk.heardFlag);Assert.AreEqual("Orrin",talk.speaker);
            var going=All<LeavesAlong>().Where(l=>l.flag=="bossdown.Tollkeeper").ToList();Assert.GreaterOrEqual(going.Count,3,"fewer than three hulls go through the gate");
            var first=going.OrderBy(l=>l.after).First();Assert.AreEqual("District: the market",first.name,"the market district isn't first through (Orrin: \"Market district first\")");
            Assert.IsTrue(going.All(l=>l.gameObject.activeInHierarchy),"a hull to go through isn't there before the gate opens");
            foreach(var l in going)Assert.IsTrue(l.path.All(p=>p.y<Plane-2.5f),l.name+" goes through at the plane, where the party is");
            var home=going.ToDictionary(l=>l,l=>l.transform.position);
            Time.timeScale=4;Flag("bossdown.Tollkeeper");
            float deadline=Time.unscaledTime+3.5f;while(Time.unscaledTime<deadline)yield return null;
            Assert.Greater(first.transform.position.z,home[first].z+4,"the market district isn't going north through the gate");
            deadline=Time.unscaledTime+16;while(going.Any(l=>l.gameObject.activeInHierarchy)&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(going.Any(l=>l.gameObject.activeInHierarchy),"the hulls are still going through a minute after the gate opens");
            Time.timeScale=1;yield return Zone("Gate");
            Assert.IsFalse(All<LeavesAlong>().Any(l=>l.flag=="bossdown.Tollkeeper"&&l.gameObject.activeInHierarchy),"a zone opened after the gate has the first hulls still waiting at chain D");
        }
    }
}
