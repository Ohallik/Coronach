using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
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
    /// <summary>Chapter 4's combat map, the Drowned Waystation (docs/maps/DrownedWaystation.md): the mire walked island to island, the station drained a stage at a time by its pumps and
    /// walked down into as each stage gives its floor back, the pockets off it, the flags its talks and pumps set, a stage kept in the save, the great hall built to the boss's contract,
    /// and no wall between the fixed camera and any fight. Flags are set directly where a conversation would set them: the script's nodes are tested by the dialogue tests.</summary>
    public sealed class WaystationTests
    {
        const string Station="DrownedWaystation";
        // The builder's numbers (WendmireBuilder.Waystation), copied: the tests can't see the editor assembly.
        const float StageOne=-2.7f,StageTwo=-4.5f,GreatFull=-1.15f,GreatEmpty=-2.3f,PlinthTop=-.85f,ArenaFloor=-1.6f;
        static readonly Vector2 C=new(0,289);
        readonly List<string> log=new();
        void Heard(string text,string stack,LogType type){if(text.StartsWith("WATER_")||text.StartsWith("PUMP_RUN")||text.StartsWith("DISCOVERY")||text.StartsWith("LEAVES ")||text.StartsWith("GONE "))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter4");log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what this chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Waystation(ContentRegistry.Book book)
        {
            // The camera's wall-foot check needs no exemption here: a later zone with no wall foot a hero can reach on the camera's line passes by rule (QaPlanEarlyTests), and the
            // Waystation's line meets only wall tops and the drowned rooms.
        }

        // ------------------------------------------------------------------ fixtures
        static void Set(params string[] keys){foreach(var key in keys)GameServices.Current.Flags.SetBool(key,true);}
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Zone(string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(Station,spawn);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Station+" never finished loading");Assert.AreEqual(Station,SceneManager.GetActiveScene().name);
            Hold();yield return new WaitForSecondsRealtime(.6f);
        }
        /// <summary>The heroes stand where they are put and nobody wanders into a crossing.</summary>
        static void Hold(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}}
        /// <summary>The ground under a plan point: the first solid thing a ray from above meets that isn't deep water's blocker (a Membrane's, standing in the water while it is deep).</summary>
        static Vector3 Ground(Vector2 p)
        {
            var hits=Physics.RaycastAll(new Vector3(p.x,40,p.y),Vector3.down,90,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<Membrane>()==null).OrderBy(h=>h.distance).ToArray();
            Assert.IsNotEmpty(hits,"fixture: there is ground at "+p);
            return hits[0].point;
        }
        static Vector3 Hero=>PartyController.Current.Active.transform.position;
        static bool Reach(Vector3 from,Vector2 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(from,out var start,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(Ground(to),out var end,1.5f,NavMesh.AllAreas)&&
                NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
        static StagedWater Staged=>Object.FindFirstObjectByType<StagedWater>();
        static WaterVolume Water(string drainFlag)=>Object.FindObjectsByType<WaterVolume>(FindObjectsSortMode.None).First(w=>w.drainFlag==drainFlag);
        static DiscoveryPoint Point(System.Func<DiscoveryPoint,bool> which,string what)
        {var found=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(which).ToArray();Assert.AreEqual(1,found.Length,what+" is not in the station exactly once");return found[0];}
        /// <summary>Waits for the station's water to settle at a stage: the surface there, and the step's blockers gone (the paths carve a frame or two after).</summary>
        static IEnumerator Settle(int stage,float seconds=8)
        {
            float deadline=Time.unscaledTime+seconds;var water=Staged.water;
            while((Staged.Stage!=stage||water.Moving)&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
        }

        // ------------------------------------------------------------------ the mire
        [UnityTest] public IEnumerator TheMireIsWalkedFromTheRaftToThePorticoAndTheStationStopsAtItsWater()
        {
            yield return Zone();var start=Hero;
            foreach(var (what,at) in new[]{("the strider pocket",new Vector2(2,32)),("the ford's far side",new Vector2(20,52)),("the lure lesson",new Vector2(28,60)),("the lure posts",new Vector2(26,72)),
                ("the bell-buoy",new Vector2(55.6f,69.6f)),("the leech channel",new Vector2(10.5f,93)),("the dry spit's cache",new Vector2(-18.2f,93.2f)),("the third island",new Vector2(-4,120)),
                ("the causeway",new Vector2(0,146)),("the portico",new Vector2(0,160)),("the hall's door",new Vector2(0,174)),("pump one",new Vector2(12.1f,182.9f)),("the bench",new Vector2(-8.4f,157.5f))})
                Assert.IsTrue(Reach(start,at),"from the raft nobody can walk to "+what+" at "+at);
            // Wading: the ford, the leech channel and the causeway are water a body walks through, and the water round the islands isn't.
            foreach(var (what,at) in new[]{("the ford",new Vector2(16,42)),("the leech channel",new Vector2(10.5f,93)),("the causeway",new Vector2(.5f,147))})
            {float depth=WaterVolume.DepthAt(Ground(at));Assert.That(depth,Is.InRange(.12f,.55f),what+" is not wading water ("+depth.ToString("0.00")+" deep)");}
            Assert.Greater(WaterVolume.DepthAt(Ground(new Vector2(-20,50))),.55f,"the open mire west of the second island is shallow enough to walk");
            Assert.IsFalse(Reach(start,new Vector2(-20,50)),"a body can walk out into the deep mire");
            // Until a pump has run the station stops at its water: the hall's sunk floor, the gallery and the great hall's landing are out of reach.
            Assert.AreEqual(0,Staged.Stage,"fixture: no pump has run");
            foreach(var (what,at) in new[]{("the hall's sunk floor",new Vector2(-4,188)),("the terrace",new Vector2(14,222)),("the low floor",new Vector2(-7,228)),("the landing",new Vector2(0,261.5f))})
                Assert.IsFalse(Reach(start,at),what+" can be walked to before any pump has run");
        }

        // ------------------------------------------------------------------ the pumps: the zone's mechanic
        [UnityTest] public IEnumerator EachPumpTakesTheStationDownAStageAndWhatItUncoversCanBeWalked()
        {
            yield return Zone("Portico");var start=Hero;var water=Staged.water;
            Assert.AreEqual(0,water.Level,.02f,"the station's water doesn't start at the mire's level");
            // The check has to be able to see a station that doesn't drain: with its staging switched off, the first pump's flag leaves the water standing and the terrace out of reach.
            Staged.enabled=false;Set("wend.pump1");yield return new WaitForSecondsRealtime(5);
            Assert.AreEqual(0,water.Level,.02f,"fixture: with no staging the water still moved");Assert.IsFalse(Reach(start,new Vector2(14,222)),"fixture: the check can't see a station that doesn't drain");
            GameServices.Current.Flags.SetBool("wend.pump1",false);Staged.enabled=true;yield return null;yield return null;
            Assert.AreEqual(0,Staged.Stage,"fixture: the station is back at rest");
            // Pump one, worked by its hand: the hall, the stair and the terrace come out of the water, and nothing below them does.
            var one=Point(p=>p.flag=="wend.pump1","pump one's hand");Assert.AreEqual("Pump",one.report);Assert.AreEqual("SelaPumpOne",one.dialogueNode,"pump one doesn't play its talk before it turns");
            Set("wend.pump1");yield return Settle(1);
            Assert.AreEqual(StageOne,water.Level,.03f,"pump one doesn't take the station down to the first stage");
            Assert.IsTrue(log.Any(l=>l.StartsWith("PUMP_RUN wend.pump1")),"pump one's crank doesn't turn and say so");
            Assert.IsTrue(log.Any(l=>l.StartsWith("WATER_STAGED wend.pump1")),"the first stage never settles");
            foreach(var (what,at) in new[]{("the hall's sunk floor",new Vector2(-4,188)),("the hall's alcoves",new Vector2(-9,192.6f)),("the stair",new Vector2(12,199)),("the terrace",new Vector2(14,222)),
                ("the vault passage",new Vector2(25.3f,217.9f)),("pump two",new Vector2(17,246.2f))})Assert.IsTrue(Reach(start,at),"after pump one nobody can walk to "+what);
            foreach(var (what,at) in new[]{("the low floor",new Vector2(-7,228)),("the landing",new Vector2(0,261.5f)),("Pim's grille",new Vector2(-26.5f,221.5f))})
                Assert.IsFalse(Reach(start,at),what+" can be walked to after pump one alone");
            // Pump two, by its hand: the low floor, the great stair and Pim's stair come out; the sumps stay wet; Pim's grille and the great hall's stay shut.
            var two=Point(p=>p.flag=="wend.pump2","pump two's hand");Assert.IsTrue(two.Available,"pump two can't be worked once the terrace is dry");
            two.Interact();Assert.IsTrue(Flag("wend.pump2"),"working pump two sets nothing");yield return Settle(2);
            Assert.AreEqual(StageTwo,water.Level,.03f,"pump two doesn't take the station down to the second stage");
            foreach(var (what,at) in new[]{("the low floor",new Vector2(-7,228)),("the ramp",new Vector2(4,245)),("the west alcoves",new Vector2(-23,209.5f)),("Pim's grille",new Vector2(-26.5f,221.5f)),
                ("the great stair",new Vector2(0,255)),("the landing",new Vector2(0,261.5f)),("pump three",new Vector2(-9,262.5f))})Assert.IsTrue(Reach(start,at),"after pump two nobody can walk to "+what);
            foreach(var at in new[]{new Vector2(-12,214.5f),new Vector2(-6.5f,232)}){float depth=WaterVolume.DepthAt(Ground(at));Assert.That(depth,Is.InRange(.25f,.55f),"a sump at "+at+" doesn't stay wet for the leeches ("+depth.ToString("0.00")+")");}
            Assert.IsFalse(Reach(start,new Vector2(-32,224)),"Pim's pocket can be walked into through the grille Pim tied");
            Assert.IsFalse(Reach(start,C),"the great hall can be walked into before pump three");
            // Pump three waits for Pim. Then it lifts the great hall's grille.
            var three=Point(p=>p.flag=="wend.pump3","pump three's hand");Assert.AreEqual("wend.pimFound",three.requiredFlag);
            Assert.IsFalse(three.Available,"pump three can be worked before Pim is found");
            Set("wend.pimFound");yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(Reach(start,new Vector2(-32,224)),"Pim's grille stays shut once Pim is found");
            Assert.IsTrue(three.Available,"pump three can't be worked once Pim is found");
            three.Interact();Assert.IsTrue(Flag("wend.pump3"));yield return new WaitForSecondsRealtime(.6f);
            Assert.IsTrue(Reach(start,C),"pump three doesn't open the great hall's door");
            Assert.AreEqual(GreatFull,Water("bossdown.Rotbloom").Level,.03f,"pump three drains the great hall: that is the Rotbloom's to do");
            // Pump four (the Fourth Pump, at its step) drains the vault's sump.
            Assert.IsFalse(Reach(start,new Vector2(42,218)),"the vault can be walked into before its sump is drained");
            var sump=Water("wend.pump4");Assert.Greater(sump.Level-Ground(new Vector2(33.3f,218)).y,.55f,"fixture: the vault's sump is deep");
            Set("wend.pump4");float until=Time.unscaledTime+8;while(!sump.Drained&&Time.unscaledTime<until)yield return null;yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(sump.Drained,"pump four doesn't drain the vault's sump");Assert.IsTrue(Reach(start,new Vector2(42,218)),"the vault can't be walked into once its sump is drained");
        }

        [UnityTest] public IEnumerator AStageIsKeptInTheSaveAndLoadsWithNothingPlayed()
        {
            Set("wend.tideTurned","wend.lureLearned","wend.pump1","wend.pump2");
            yield return Zone("Door");
            var saved=GameServices.Current.Saves.Load("autosave");Assert.AreEqual(Station,saved.zone,"fixture: arriving autosaves");Assert.IsTrue(saved.flags["wend.pump2"],"fixture: the save has pump two");
            // The check has to be able to see a save that lost its stage: the same save with the pumps' flags taken out continues into a station at rest.
            var stripped=GameServices.Current.Saves.Load("autosave");stripped.flags.Remove("wend.pump1");stripped.flags.Remove("wend.pump2");GameServices.Current.Saves.Save("autosave",stripped);
            yield return Continue();
            Assert.AreEqual(0,Staged.Stage,"fixture: the check can't see a save that lost its stage");Assert.AreEqual(0,Staged.water.Level,.02f,"fixture: the check can't see a save that lost its stage");
            var kept=GameServices.Current.Saves.Load("autosave");kept.flags["wend.pump1"]=kept.flags["wend.pump2"]=true;kept.zone=Station;kept.spawn="Door";GameServices.Current.Saves.Save("autosave",kept);
            log.Clear();yield return Continue();
            Assert.AreEqual(2,Staged.Stage,"the station loads at another stage than the save's");Assert.AreEqual(StageTwo,Staged.water.Level,.03f,"the station's water loads where the save left it");
            Assert.IsFalse(Staged.water.Moving,"a loaded stage plays its drain again");
            Assert.IsFalse(log.Any(l=>l.StartsWith("WATER_STAGE ")||l.StartsWith("PUMP_RUN")),"a loaded stage plays its drain again: "+string.Join(", ",log));
            yield return new WaitForSecondsRealtime(.6f);
            Assert.IsTrue(Reach(Hero,new Vector2(-7,228)),"the low floor can't be walked on after a load at the second stage");
        }

        /// <summary>Title, then Continue from the autosave, as a player does.</summary>
        IEnumerator Continue()
        {
            SceneFlow.Current.LoadZone("Title");while(SceneFlow.Current.Loading)yield return null;yield return null;
            Object.FindFirstObjectByType<TitleScreen>().StartGame(true);
            float until=Time.realtimeSinceStartup+30;while(SceneFlow.Current.Loading&&Time.realtimeSinceStartup<until)yield return null;
            Assert.AreEqual(Station,SceneFlow.Current.Zone,"the save doesn't resume in the station");Hold();yield return null;yield return null;
        }

        // ------------------------------------------------------------------ what the script expects of the world
        [UnityTest] public IEnumerator TheTalksPumpsAndPocketsSetTheFlagsTheScriptExpects()
        {
            yield return Zone();
            // (node, flag, needs, report): the manifest's section 2 and the main quest's steps.
            foreach(var (node,flag,needs,report) in new[]{("MaruMireGate","wend.mireEntered","",""),("MaruLureLesson","wend.lureLearned","","LureLesson"),("MaruPortico","wend.porticoReached","",""),
                ("SelaWaystationHall","wend.hallSeen","",""),("SelaPumpOne","wend.pump1","","Pump"),("SelaBloomDoor","wend.bloomSeen","wend.pump3",""),("MaruAfterBloom","wend.lampCarried","wend.rotbloomDead","")})
            {
                var point=Point(p=>p.dialogueNode==node,node);
                Assert.AreEqual(flag,point.flag,node+" sets the wrong flag");Assert.AreEqual(needs,point.requiredFlag??"",node+" waits on the wrong flag");Assert.AreEqual(report,point.report??"",node+" reports the wrong thing");
            }
            foreach(var node in new[]{"MaruMireGate","MaruLureLesson","MaruPortico","SelaWaystationHall","SelaBloomDoor","MaruAfterBloom"})Assert.IsTrue(Crosses(Point(p=>p.dialogueNode==node,node)),node+" isn't a crossing");
            var bell=Point(p=>p.report=="BellBuoy","the bell-buoy");Assert.AreEqual(("SchoolBell",0,"SelaBellBuoy"),(bell.requiredQuest,bell.requiredStep,bell.dialogueNode),"the bell-buoy is offered at the wrong step");
            var four=Point(p=>p.report=="FourthPump","pump four");Assert.AreEqual(("FourthPump",0,"wend.pump4"),(four.requiredQuest,four.requiredStep,four.flag),"pump four is offered at the wrong step");
            var marks=Point(p=>p.report=="VaultMark","the wall of marks");Assert.AreEqual(("FourthPump",1,"SelaVaultMark"),(marks.requiredQuest,marks.requiredStep,marks.dialogueNode),"the wall of marks is offered at the wrong step");
            // Pumps one and two count for one step, pump three for its own (QaPlanWorldTests.NoQuestMovesOnOutOfTurn: the same report on two steps lets one step's work do the other's).
            var pumps=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            Assert.AreEqual(2,pumps.Count(p=>p.report=="Pump"),"the main quest counts the first two pumps by their report");
            Assert.AreEqual("wend.pump3",pumps.Single(p=>p.report=="LastPump").flag,"the last pump doesn't report for its own step");
            var lesson=Point(p=>p.dialogueNode=="MaruLureLesson","the lure lesson");Assert.IsNotNull(lesson.previewEncounter,"the lures wake before Maru has taught the count");
            Assert.AreEqual("Mire_Lures",lesson.previewEncounter.encounterId);
            var after=Object.FindObjectsByType<AfterTalk>(FindObjectsSortMode.None).SingleOrDefault(a=>a.node=="SelaBloomDead");
            Assert.IsNotNull(after,"nothing plays SelaBloomDead after the Rotbloom");Assert.AreEqual(("bossdown.Rotbloom","wend.rotbloomDead"),(after.flag,after.heardFlag));
            var pims=Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(n=>n.speaker=="Pim").ToArray();
            Assert.AreEqual(2,pims.Length,"Pim stands in the dry pocket and at the portico");Assert.IsTrue(pims.Any(n=>n.firstNode=="PimFound"),"nobody plays PimFound");
            Assert.IsTrue(pims.Single(n=>n.firstNode=="PimFound").repeatNode=="PimFound","Pim in the pocket can offer a line meant for somewhere else");
            Assert.IsTrue(pims.Single(n=>n.firstNode!="PimFound").repeatNode=="PimPortico","Pim at the portico doesn't play PimPortico");
            var marus=Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(n=>n.speaker=="Maru").ToArray();
            Assert.AreEqual(2,marus.Length,"Maru stands on the first island and at the portico");Assert.IsTrue(marus.All(n=>n.repeatNode=="MaruMireRepeat"),"Maru's mire repeat isn't hers out here");
            // Glowspore for Dov's order: six wanted, more than six to find.
            int caps=Object.FindObjectsByType<MineNode>(FindObjectsSortMode.None).Where(m=>m.material=="Glowspore").Sum(m=>m.amount);Assert.GreaterOrEqual(caps,6,"the mire holds too little Glowspore for Dov's order");
            foreach(var id in new[]{"wend.mire.bell","wend.mire.spit","wend.station.pim","wend.station.vault"})Assert.IsTrue(Object.FindObjectsByType<LootCache>(FindObjectsSortMode.None).Any(c=>c.cacheId==id),"the cache "+id+" is missing");
        }

        [UnityTest] public IEnumerator MaruAndPimAreWhereTheChapterHasThem()
        {
            Set("wend.tideTurned");
            yield return Zone();
            GameObject Who(string speaker,float z)=>Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(n=>n.speaker==speaker&&Mathf.Abs(n.transform.position.z-z)<12).gameObject;
            Assert.IsTrue(Who("Maru",22).activeInHierarchy,"Maru isn't on the first island once the tide has turned");Assert.IsFalse(Who("Maru",162).activeInHierarchy,"Maru waits at the portico before the lure lesson");
            Set("wend.lureLearned");yield return new WaitForSecondsRealtime(.3f);
            Assert.IsFalse(Who("Maru",22).activeInHierarchy,"Maru stays on the first island after the lesson");Assert.IsTrue(Who("Maru",162).activeInHierarchy,"Maru hasn't gone on to the portico after the lesson");
            Assert.IsFalse(Who("Pim",161).activeInHierarchy,"Pim is at the portico before being found");Assert.IsTrue(Who("Pim",221).activeInHierarchy,"Pim isn't in the dry pocket");
            var pocket=Who("Pim",221);
            Set("wend.pimFound");yield return null;yield return null;
            Assert.IsFalse(pocket.GetComponent<Npc>().enabled,"Pim in the pocket can still be talked to once found");
            float until=Time.unscaledTime+40;while(pocket.activeInHierarchy&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(pocket.activeInHierarchy,"Pim doesn't leave the pocket once found");
            Assert.IsTrue(log.Any(l=>l.StartsWith("LEAVES ")),"Pim was gone without walking out");
            Assert.IsTrue(Who("Pim",161).activeInHierarchy,"Pim doesn't go out to Maru once found");
            Set("wend.rotbloomDead","wend.lampCarried");yield return new WaitForSecondsRealtime(.3f);
            Assert.IsFalse(Who("Pim",161).activeInHierarchy,"Pim is still at the portico once the Rotbloom is dead");
            yield return Zone("Portico");
            Assert.IsFalse(Who("Pim",221).activeInHierarchy,"Pim is still in the pocket after being found");Assert.IsFalse(Who("Maru",162).activeInHierarchy,"Maru is still at the portico after carrying the lamp home");
        }

        [UnityTest] public IEnumerator EveryKeptLampKeepsItsCountAndALureNeverHoldsALong()
        {
            var probe=new GameObject("Count probe").AddComponent<RhythmLamp>();
            List<float> Pulses(string count,float seconds)
            {
                probe.count=count;var pulses=new List<float>();float run=0;
                for(float t=0;t<seconds;t+=.01f){if(probe.Brightness(t)>=.99f)run+=.01f;else if(run>0){pulses.Add(run);run=0;}}
                return pulses;
            }
            var ssl=Pulses("SSL",probe.Period-.01f);Assert.AreEqual(3,ssl.Count,"two short, one long isn't three pulses");
            Assert.Less(ssl[0],probe.longOn*.5f);Assert.Less(ssl[1],probe.longOn*.5f);Assert.Greater(ssl[2],probe.shortOn*2.5f,"the third pulse of two short, one long isn't long");
            var lls=Pulses("LLS",probe.Period-.01f);Assert.Greater(lls[0],probe.shortOn*2.5f);Assert.Greater(lls[1],probe.shortOn*2.5f);Assert.Less(lls[2],probe.longOn*.5f,"long, long, short ends long");
            var lure=Pulses("",60);Assert.Greater(lure.Count,40,"a lure doesn't flicker");Assert.Less(lure.Max(),probe.longOn*.6f,"a lure held a long, which nothing hungry does");
            Object.Destroy(probe.gameObject);
            yield return Zone();
            var lamps=Object.FindObjectsByType<RhythmLamp>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            Assert.GreaterOrEqual(lamps.Count(l=>l.count=="SSL"),5,"the mire's kept lamps and Maru's don't keep Driftreed's count");
            Assert.AreEqual(1,lamps.Count(l=>l.count=="LLS"),"Pim's glow-spots don't say still afloat");
            Assert.IsTrue(lamps.Any(l=>l.count==""&&l.darkWhen=="clear.Mire_Lures"),"the near lure post doesn't flicker until its lures are dead");
        }

        // ------------------------------------------------------------------ the great hall and its door
        [UnityTest] public IEnumerator TheGreatHallIsBuiltToTheBossContract()
        {
            Set("wend.tideTurned","wend.lureLearned","wend.pump1","wend.pump2","wend.pimFound","wend.pump3");
            yield return Zone("Door");
            var duel=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="Rotbloom");
            var post=duel.spawners.Single();Assert.AreEqual(C.x,post.transform.position.x,.1f);Assert.AreEqual(C.y,post.transform.position.z,.1f,"the boss's spawner isn't at C");
            Assert.IsTrue(post.faceAsPlaced);Assert.Less(Vector3.Angle(post.transform.forward,Vector3.back),5,"the boss doesn't face the door");
            Assert.AreEqual(("wend.bloomSeen","Door",1),(duel.requiredFlag,duel.checkpointSpawn,duel.membranes.Length),"the door isn't the contract's");
            var box=duel.GetComponent<BoxCollider>();Assert.That(box.size.x,Is.InRange(36f,40f));Assert.That(box.size.z,Is.InRange(36f,40f));
            Assert.IsNotNull(duel.previewPoint,"the Rotbloom isn't shown from the door");Assert.GreaterOrEqual(duel.previewRadius,Vector3.Distance(duel.previewPoint.position,post.transform.position)-.5f,"the preview's radius doesn't take in C");
            // The floor: level within r 17 but for the plinths, nothing solid on it inside r 12.5, and the water wading-deep over all of it.
            float floor=Ground(C).y;var hall=Object.FindObjectsByType<WaterVolume>(FindObjectsSortMode.None).Single(w=>w.drainFlag=="bossdown.Rotbloom");
            for(float x=-17;x<=17;x+=1)for(float z=-17;z<=17;z+=1)
            {
                var p=C+new Vector2(x,z);if(new Vector2(x,z).magnitude>17)continue;bool plinth=new[]{45f,135f,225f,315f}.Any(b=>Vector2.Distance(p,Plinth(b))<2.6f+3.4f);if(plinth)continue;
                var at=Ground(p);Assert.AreEqual(floor,at.y,.3f,"the great hall's floor isn't near level at "+p);
                if((p-C).magnitude<12.5f){Assert.IsTrue(Physics.Raycast(at+Vector3.up*6,Vector3.down,out var hit,8,~0,QueryTriggerInteraction.Ignore));Assert.AreEqual(at.y,hit.point.y,.05f,"something solid stands on the floor at "+p+": "+hit.collider.name);}
                float depth=hall.Level-at.y;Assert.That(depth,Is.InRange(.25f,.6f),"the hall's water isn't wading-deep at "+p+" ("+depth.ToString("0.00")+")");
            }
            Assert.Less(hall.drainedLevel,floor-.3f,"the hall's empty level isn't under its floor");
            var basin=Object.FindFirstObjectByType<FloodBasin>();Assert.IsNotNull(basin,"the hall has no FloodBasin");Assert.IsFalse(basin.followsGates);Assert.AreEqual(hall,basin.water);
            Assert.AreSame(basin,ArenaWater.Current,"the hall's water isn't the arena's");Assert.IsTrue(ArenaWater.Submerged(Ground(C)),"C isn't under water at full");
            // The plinths: dry flat tops 2.6 across at the contract's bearings, walkable from C.
            foreach(float bearing in new[]{45f,135f,225f,315f})
            {
                var top=Plinth(bearing);float y=Ground(top).y;
                Assert.Greater(y,hall.Level+.15f,"the plinth at "+bearing+" isn't dry ground at full water");Assert.AreEqual(PlinthTop,y,.08f,"the plinth at "+bearing+" isn't at its height");
                for(float a=0;a<360;a+=45)Assert.AreEqual(y,Ground(top+new Vector2(Mathf.Sin(a*Mathf.Deg2Rad),Mathf.Cos(a*Mathf.Deg2Rad))*2.2f).y,.08f,"the plinth at "+bearing+" isn't flat across its top");
                Assert.IsTrue(Reach(Ground(C),top),"the plinth at "+bearing+" can't be walked up from C");
            }
            // Where the rot-pods are seeded, the floor is open.
            foreach(float bearing in new[]{0f,90f,180f,270f})
            {
                var at=C+new Vector2(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad))*12;Assert.IsTrue(Physics.Raycast(new Vector3(at.x,10,at.y),Vector3.down,out var hit,20,~0,QueryTriggerInteraction.Ignore));
                Assert.AreEqual(floor,hit.point.y,.3f,"a rot-pod's place at bearing "+bearing+" isn't open floor ("+hit.collider.name+")");
            }
            // The checkpoint: outside the seal, on the paths, within 60 m, with the free cradle beside it.
            var spawn=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Single(s=>s.id=="Door");var seal=duel.membranes[0];
            Assert.Less(spawn.transform.position.z,seal.transform.position.z-1,"the checkpoint is inside the seal");Assert.Less(Vector3.Distance(spawn.transform.position,seal.transform.position),60);
            Assert.IsTrue(NavMesh.SamplePosition(spawn.transform.position,out _,1.5f,NavMesh.AllAreas),"the checkpoint isn't on the paths");
            var bay=Object.FindObjectsByType<RepairBay>(FindObjectsSortMode.None).OrderBy(r=>Vector3.Distance(r.transform.position,spawn.transform.position)).First();
            Assert.IsTrue(bay.free&&bay.saveSpawn=="Door"&&Vector3.Distance(bay.transform.position,spawn.transform.position)<20,"there is no free cradle at the door");
            Assert.IsTrue(seal.Open,"the seal is shut before the fight");
        }

        [UnityTest] public IEnumerator TheGreatHallDoorIsACheckpointAndShutsBehindTheParty()
        {
            Set("wend.tideTurned","wend.lureLearned","wend.pump1","wend.pump2","wend.pimFound","wend.pump3","wend.bloomSeen");
            yield return Zone("Door");
            var duel=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="Rotbloom");
            GameServices.Current.State.spawn="Portico";duel.Begin();yield return null;
            Assert.AreEqual("Door",GameServices.Current.State.spawn,"starting the fight doesn't make the door the checkpoint");
            Assert.AreEqual("Door",GameServices.Current.Saves.Load("autosave").spawn,"starting the fight doesn't save at the door");
            Assert.IsFalse(duel.membranes[0].Open,"the seal doesn't shut behind the party");
        }
        [UnityTest] public IEnumerator AWonHallLoadsDrainedWithNoBasinToRefillIt()
        {
            Set("wend.tideTurned","wend.lureLearned","wend.pump1","wend.pump2","wend.pimFound","wend.pump3","wend.bloomSeen","bossdown.Rotbloom","clear.Rotbloom");
            yield return Zone("Door");yield return new WaitForSecondsRealtime(.5f);
            var hall=Object.FindObjectsByType<WaterVolume>(FindObjectsSortMode.None).Single(w=>w.drainFlag=="bossdown.Rotbloom");
            Assert.AreEqual(GreatEmpty,hall.Level,.03f,"a won hall loads flooded");Assert.IsNull(Object.FindFirstObjectByType<FloodBasin>(),"a won hall's basin is still there to refill it");
            Assert.IsFalse(ArenaWater.Submerged(Ground(C)),"C is under water in a won hall");
        }

        // ------------------------------------------------------------------ the camera
        [UnityTest] public IEnumerator NoWallOrTallPieceStandsBetweenTheCameraAndAFight()
        {
            Set("wend.tideTurned","wend.lureLearned","wend.pump1","wend.pump2","wend.pimFound","wend.pump3");
            yield return Zone("Door");
            var toCamera=-(Quaternion.Euler(SightlineAudit.Pitch,SightlineAudit.Yaw,0)*Vector3.forward);var hidden=new List<string>();int samples=0;
            foreach(var fight in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
            {
                var box=fight.GetComponent<BoxCollider>().bounds;
                for(float x=box.min.x+1;x<box.max.x;x+=2)for(float z=box.min.z+1;z<box.max.z;z+=2)
                {
                    // A point of the fight's own floor: navigation within a hand of the ground, at the fight's level (the paths baked on wall tops are no one's floor).
                    if(!Physics.Raycast(new Vector3(x,box.max.y+8,z),Vector3.down,out var floor,40,~0,QueryTriggerInteraction.Ignore)||!NavMesh.SamplePosition(floor.point,out var on,.4f,NavMesh.AllAreas))continue;
                    if(Mathf.Abs(on.position.y-(box.center.y-1.5f))>1.6f)continue;
                    samples++;var chest=on.position+Vector3.up*SightlineAudit.Chest;
                    foreach(var hit in Physics.RaycastAll(chest,toCamera,SightlineAudit.Distance-SightlineAudit.NearIgnore,~0,QueryTriggerInteraction.Ignore))
                    {
                        if(hit.collider.GetComponentInParent<Health>()!=null||hit.collider.GetComponentInParent<CharacterController>()!=null)continue;
                        // The house rule (TallOverAFight): nothing over three metres that is broad enough to hide a body. A post or a lamp is neither.
                        var size=hit.collider.bounds.size;if(hit.collider.bounds.max.y-on.position.y<3||size.x<1.6f&&size.z<1.6f)continue;
                        hidden.Add(fight.encounterId+" at ("+x.ToString("0")+", "+z.ToString("0")+") behind "+hit.collider.name);break;
                    }
                }
            }
            Assert.Greater(samples,400,"fixture: the fights' floors were sampled");
            Assert.IsEmpty(hidden,hidden.Count+" of "+samples+" fight floor points are hidden from the camera: "+string.Join("; ",hidden.Take(12)));
        }
        /// <summary>The ground camera slides in along its line wherever a sphere .4 m across cast from the hero's chest back toward it meets scenery (CameraRig.GroundDistance), and
        /// dives to its nearest. Against the lower gallery's west alcove it dived into the masonry in front of it, and the frame was the back of the ground with space through it (s26
        /// 301 to 306, November 20: the lead at (-24.16, 210.45)). Every place a hero can stand against a station wall, every stage drained, is cast as the camera casts, and nothing
        /// of the landform may meet the sphere: then the lens stays where it hangs and never comes near the masonry.</summary>
        [UnityTest] public IEnumerator TheCamerasSphereClearsTheStationsWallsFromEveryPlaceAHeroStandsAgainstThem()
        {
            Set("wend.tideTurned","wend.lureLearned","wend.pump1","wend.pump2","wend.pimFound","wend.pump3","wend.pump4");
            yield return Zone("Door");
            var back=-(Quaternion.Euler(SightlineAudit.Pitch,SightlineAudit.Yaw,0)*Vector3.forward);
            var land=Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c=>c.name=="Mire and waystation").ToArray();
            Assert.AreEqual(1,land.Length,"fixture: the station's landform collider");
            // A place against a wall: the paths' edge (a body's radius in from the wall) where the paths join the station's floors, and the capture's own place in the west alcove.
            // The bake also lays islands of path on flat wall tops that no path reaches: nobody stands there.
            var stands=new List<Vector3>();var door=Ground(new Vector2(-2.5f,261.5f));Assert.IsTrue(NavMesh.SamplePosition(door,out var from,2,NavMesh.AllAreas),"fixture: the paths at the great hall's door");
            var path=new NavMeshPath();int islands=0,looked=0;
            for(float x=-38;x<=50;x+=.25f)for(float z=166;z<=273;z+=.25f)
            {
                if(!NavMesh.SamplePosition(new Vector3(x,-1,z),out var on,4,NavMesh.AllAreas)||Mathf.Abs(on.position.x-x)>.05f||Mathf.Abs(on.position.z-z)>.05f)continue;
                if(!NavMesh.FindClosestEdge(on.position,out var edge,NavMesh.AllAreas)||edge.distance>.15f)continue;
                if(!Physics.Raycast(on.position+Vector3.up*2,Vector3.down,out var floor,4,~0,QueryTriggerInteraction.Ignore)||floor.collider!=land[0])continue;
                if(++looked%200==0)yield return null;
                if(!NavMesh.CalculatePath(from.position,on.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete){islands++;continue;}
                stands.Add(floor.point);
            }
            Debug.Log("WAYSTATION_WALL_PLACES "+stands.Count+" walkable, "+islands+" on islands of path no floor reaches");
            Assert.IsTrue(Physics.Raycast(new Vector3(-24.16f,4,210.45f),Vector3.down,out var alcove,12,~0,QueryTriggerInteraction.Ignore)&&alcove.collider==land[0],"fixture: the west alcove's floor");
            stands.Add(alcove.point);
            Assert.Greater(stands.Count,1500,"fixture: the places against the station's walls were found");
            var met=new List<string>();
            foreach(var at in stands)
            {
                if(at.z<166)continue;
                foreach(var hit in Physics.SphereCastAll(at+Vector3.up*1.2f,.4f,back,SightlineAudit.Distance,~0,QueryTriggerInteraction.Ignore))
                    if(hit.collider==land[0]&&hit.distance>0){met.Add("("+at.x.ToString("0.0")+", "+at.z.ToString("0.0")+") at "+hit.distance.ToString("0.0")+" m");break;}
            }
            Assert.IsEmpty(met,met.Count+" of "+stands.Count+" places against the station's walls send the camera into the masonry: "+string.Join("; ",met.Take(40)));
        }
        /// <summary>How much of the frame over a hero standing here is wall top well in front of them: landform more than a body's waist over their floor and more than 2.5 m nearer the
        /// lens than they are, along the camera's own heading. Rays through 48 by 27 points of the screen from where the ground camera hangs (40 and 20 degrees, 19.5 m, 30 degrees
        /// high, 16 by 9), counted against every ray that meets the landform. A wall behind the hero, which is the frame's backdrop, isn't counted.</summary>
        static float Looming(Collider land,Vector3 floor)
        {
            var rotation=Quaternion.Euler(SightlineAudit.Pitch,SightlineAudit.Yaw,0);var chest=floor+Vector3.up*SightlineAudit.Chest;var eye=chest-rotation*Vector3.forward*SightlineAudit.Distance;
            var heading=new Vector3(Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),0,Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));float hero=Vector3.Dot(chest-eye,heading);
            float tan=Mathf.Tan(15*Mathf.Deg2Rad),aspect=16f/9;int met=0,looming=0;
            for(int j=0;j<27;j++)for(int i=0;i<48;i++)
            {
                var ray=new Ray(eye,rotation*new Vector3(((i+.5f)/48*2-1)*tan*aspect,((j+.5f)/27*2-1)*tan,1).normalized);
                if(!land.Raycast(ray,out var hit,90))continue;met++;
                if(hit.point.y>floor.y+1.2f&&Vector3.Dot(hit.point-eye,heading)<hero-2.5f)looming++;
            }
            return met==0?0:(float)looming/met;
        }
        /// <summary>The most of the frame a wall top in front of the hero may take where a fight is: a quarter.</summary>
        const float MostLooming=.25f;
        /// <summary>The low floor's Drowned Sentries are fought with the floor in front of them. In the west wall's alcoves a Guardian's back was taken from inside the niche, and the
        /// camera framed that corner from the south-west: the top of the masonry beside it filled two thirds of the screen and the fight was a strip down one side (s26 301 to 311,
        /// November 22). From where each sentry stands, little of the frame is wall top between the lens and the party.</summary>
        [UnityTest] public IEnumerator TheLowFloorsSentriesAreFoughtWithTheFloorInFrontOfThem()
        {
            Set("wend.tideTurned","wend.lureLearned","wend.pump1","wend.pump2");
            yield return Zone("Portico");
            var land=Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c=>c.name=="Mire and waystation").ToArray();Assert.AreEqual(1,land.Length,"fixture: the station's landform collider");
            // The check sees the fault: the west alcove the first sentry stood in until November 22, where the captured frame was mostly the wall beside it.
            float before=Looming(land[0],Ground(new Vector2(-23.2f,209.5f)));Debug.Log("WAYSTATION_LOOMING west alcove "+before.ToString("0.00"));
            Assert.Greater(before,MostLooming,"fixture: the check can't see the west alcove's wall filling the frame");
            var fight=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="Gallery_Floor");
            var sentries=fight.spawners.Where(s=>s.definition!=null&&s.definition.id=="DrownedSentry").ToArray();Assert.AreEqual(2,sentries.Length,"fixture: the low floor's two sentries");
            var box=fight.GetComponent<BoxCollider>().bounds;
            foreach(var sentry in sentries)
            {
                var at=sentry.transform.position;float share=Looming(land[0],Ground(new Vector2(at.x,at.z)));Debug.Log("WAYSTATION_LOOMING sentry "+at.ToString("0.0")+" "+share.ToString("0.00"));
                Assert.IsTrue(box.Contains(new Vector3(at.x,box.center.y,at.z)),"a sentry at "+at+" stands outside the fight's box");
                Assert.LessOrEqual(share,MostLooming,"from a sentry's place at "+at+" wall top in front of the party takes "+(share*100).ToString("0")+"% of the frame");
            }
        }
        /// <summary>Sela's words in the hall after the Rotbloom stop the game's clock while they last, and a boss's body falls, lies and goes on that clock: opened three seconds after
        /// the kill, the talk froze the bloom on its side with the heroes standing in its petals for every line (s26 469 and 470, November 20). The body must be gone first.</summary>
        [UnityTest] public IEnumerator TheRotbloomsBodyIsGoneBeforeTheHallsTalkAfterItOpens()
        {
            Set("wend.tideTurned","wend.lureLearned","wend.pump1","wend.pump2","wend.pimFound","wend.pump3");
            yield return Zone("Door");
            var definition=GameCatalog.Find<Lattice.Data.EnemyDef>("Rotbloom");Assert.IsNotNull(definition,"fixture: the Rotbloom is defined");
            var bloom=ActorFactory.Enemy(definition,Ground(C)+Vector3.up*.1f);bloom.Passive=true;yield return null;yield return null;
            var hero=PartyController.Current.Active;
            bloom.Health.Receive(new DamagePacket{source=hero.Health,amount=bloom.Health.maximum*20,type=Lattice.Data.DamageType.Kinetic});
            Assert.IsFalse(bloom.Health.Alive,"fixture: the kill");Assert.IsTrue(Flag("bossdown.Rotbloom"),"fixture: the kill is counted");
            float deadline=Time.unscaledTime+12;
            while(!GameServices.Current.Input.Blocked&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(GameServices.Current.Input.Blocked,"the hall's talk after the Rotbloom never opened");
            Assert.IsTrue(bloom==null,"the talk after the Rotbloom opened with its body still lying in the hall");
        }
        static bool Crosses(DiscoveryPoint point)=>point.startsOnCrossing&&point.TryGetComponent<BoxCollider>(out var box)&&box.isTrigger;
        static Vector2 Plinth(float bearing)=>C+new Vector2(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad))*15;
    }
}
