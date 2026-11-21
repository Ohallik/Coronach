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
    /// <summary>
    /// The Continuance as a player meets it (docs/maps/Continuance.md; ch10-wiring sections 1, 2, 6.1.1, 6.5): docked at the spine's foot with the way home there, every deck walked to
    /// from it once the works' last door opens and not before; Idra's line across the whole top of the spine with step 5's arrow on it; the captain's line across the whole of deck four,
    /// his steps aside and his crew's going; the works sealing behind the party and their north door opening only on the crews' defeat; the bay's empty cradle putting the party over the
    /// hull's back in flight and back on foot, with Meret's hail across the whole berth before her door, whose mouth is clear and whose checkpoint is the bay; nothing above the plane on
    /// the camera's side of the lanes; and Meret at the gantry's rail, and the dock to it, only after her craft. Each check is first run against the broken state, inside the test.
    /// Flags are set directly where a talk or a fight would set them: the script's nodes are the dialogue tests', the fights the combat tests'.
    /// </summary>
    public sealed class ContinuanceTests
    {
        const string Zone="Continuance";
        // The builder's numbers (LastMooringBuilder.Continuance), copied: the tests can't see the editor assembly.
        const float Up=0,Down=-3,Foot=-5,Plane=19;
        static readonly Vector3 SpineFootAt=new(0,Foot,-76),SpineTop=new(0,Up,-12.5f),CaptainAt=new(0,Down,92),CaptainAside=new(-15,Down,96),Pad=new(2,Down,238),LaunchBay=new(5,Down,230),
            HullFlight=new(-8,Plane,182),GantryRail=new(27,17.6f,251.2f),GantryDock=new(24,Plane,247.5f);
        static readonly Bounds HullBox=new(new Vector3(-4,Plane,212),new Vector3(88,3,104));

        [ContentRegistry.Expectations] static void ContinuanceExpectations(ContentRegistry.Book book)
        {
            // Nothing yet: the Continuance's ways are drawn when the story has a reason (the dock to the gantry from end.meretShip) or always open (the pad home, the berth), so no
            // lock needs words; its only door that waits is a fight's seal.
        }

        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter10");
            // The operation as far as the Continuance's dock: the gate is open.
            foreach(var flag in new[]{"end.planHeard","end.planSaid","end.sorrelSeen","end.motherFreed","end.sectionHeld","bossdown.Tollkeeper","end.gateOpen"})Flag(flag);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Until(System.Func<bool> done,float seconds){float end=Time.unscaledTime+seconds;while(!done()&&Time.unscaledTime<end)yield return null;}
        static IEnumerator Load(string spawn="Spine")
        {
            SceneFlow.Current.LoadZone(Zone,spawn);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Zone+" never finished loading");Assert.AreEqual(Zone,SceneManager.GetActiveScene().name);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var hero in PartyController.Current.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.Health.InvulnerableUntil=GameTime.Now+900;}
            yield return new WaitForSecondsRealtime(.6f);
        }
        static void Put(Component body,Vector3 at)
        {
            var controller=body.GetComponent<CharacterController>();bool on=controller.enabled;controller.enabled=false;
            if(body.TryGetComponent<FlightMotor>(out var ship))ship.Halt();if(body.TryGetComponent<GroundMotor>(out var feet))feet.Halt();
            body.transform.position=at;controller.enabled=on;Physics.SyncTransforms();
        }
        static bool Reach(Vector3 from,Vector3 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(from,out var start,2.5f,NavMesh.AllAreas)&&NavMesh.SamplePosition(to,out var end,2,NavMesh.AllAreas)&&
                NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
        static EncounterVolume Encounter(string id){var found=All<EncounterVolume>().Where(e=>e.encounterId==id).ToArray();Assert.AreEqual(1,found.Length,id+" is not in the Continuance exactly once");return found[0];}
        static DiscoveryPoint Point(string flag){var found=All<DiscoveryPoint>().Where(p=>p.flag==flag).ToArray();Assert.AreEqual(1,found.Length,"the point for "+flag+" is not in the Continuance exactly once");return found[0];}
        static Bounds Trigger(Component c){var box=c.GetComponent<BoxCollider>();Assert.IsNotNull(box,c.name+" has no box");return new Bounds(c.transform.TransformPoint(box.center),Vector3.Scale(box.size,c.transform.lossyScale));}
        /// <summary>Walkable floor across a line at <paramref name="z"/> from <paramref name="x0"/> to <paramref name="x1"/> that a crossing's box doesn't take in: a way round it.</summary>
        static List<float> WaysRound(Bounds box,float z,float x0,float x1,float y)
        {
            var round=new List<float>();
            for(float x=x0;x<=x1;x+=.5f)
                if(NavMesh.SamplePosition(new Vector3(x,y,z),out var on,.4f,NavMesh.AllAreas)&&Mathf.Abs(on.position.x-x)<.3f&&!(on.position.x>box.min.x&&on.position.x<box.max.x))round.Add(x);
            return round;
        }
        /// <summary>Anything drawn above the plane on the camera's side of the berth's lanes: within the berth's width, from 18 m south of its south edge to the lanes' north end
        /// (the shelter's wings and the gantry stand north of z 246, their faces on its line). Heroes, enemies and words are not scenery.</summary>
        static List<string> OverTheLanes(float north=245.5f)
        {
            var found=new List<string>();
            foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy||r is ParticleSystemRenderer||r.GetComponentInParent<CombatActor>()!=null||r.GetComponentInParent<EnemyBrain>()!=null||r.GetComponent<TMPro.TMP_Text>()!=null)continue;
                var b=r.bounds;if(b.max.y<=Plane+.3f)continue;
                if(b.max.x<HullBox.min.x||b.min.x>HullBox.max.x||b.max.z<HullBox.min.z-18||b.min.z>north)continue;
                // Only what stands in the berth's own air (a deck far below the plane is not over the lanes).
                if(b.max.y<Plane-4)continue;
                found.Add($"{r.name} {b.center} to y {b.max.y:0.0}");
            }
            return found;
        }

        // ================================================================== the way in, and every deck from it
        [UnityTest] public IEnumerator DockedAtTheSpinesFootTheWayHomeIsThereAndEveryDeckIsWalkedToOnceTheWorksLastDoorOpens()
        {
            yield return Load();
            Assert.AreEqual(ZoneKind.GroundCombat,ZoneController.Current.definition.kind);
            foreach(var id in new[]{"Arrival","Spine","LaunchBay","DeckFour"})Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id==id),"the Continuance has no spawn "+id);
            var spine=All<SpawnPoint>().Single(s=>s.id=="Spine");Assert.Less(Flat(spine.transform.position,SpineFootAt),1.5f,"Last Mooring's dock lands the party somewhere other than the spine's foot");
            Assert.Less(Flat(Lead.transform.position,SpineFootAt),3,"the party wasn't set down at the spine's foot");
            var home=All<DockingPad>().Where(d=>d.scene=="LastMooring").ToArray();Assert.AreEqual(1,home.Length,"there is no single way home to Last Mooring");
            Assert.AreEqual("Launch — Last Mooring",home[0].prompt);Assert.AreEqual("Continuance",home[0].spawn,"the pad home doesn't land beside Last Mooring's dock (its spawn Continuance)");
            Assert.Less(Flat(home[0].transform.position,SpineFootAt),8,"the way home isn't at the spine's foot");Assert.IsTrue(home[0].gameObject.activeInHierarchy,"the way home isn't open on arriving");
            // Every deck from the foot, the works' north door open.
            var north=Encounter("Continuance_Works").membranes.Single(m=>m.transform.position.z>160);
            var places=new (string what,Vector3 at)[]{("the spine's top",SpineTop),("the crew deck's mess",new(16,Up,24)),("deck four",new(0,Down,70)),("where the captain steps to",CaptainAside),
                ("the anchor works",new(-8,Down,140)),("the passage north",new(0,Down,186)),("the command deck's chart",new(18.6f,Down,190)),("the bay's empty cradle",Pad),("the bay's cradle",new(7.4f,Down,216))};
            // Broken first: with the works' north door shut (its obstacle carving the way), the bay can't be walked to.
            Assert.IsFalse(north.Open,"fixture: the works' north door is open before its fight");
            yield return Frames(3);
            Assert.IsFalse(Reach(Lead.transform.position,Pad),"the bay's cradle can be walked to through the works' shut north door");
            north.SetOpen(true);yield return Frames(3);
            foreach(var (what,at) in places)Assert.IsTrue(Reach(Lead.transform.position,at),"from the spine's foot nobody can walk to "+what+" at "+at);
        }

        // ================================================================== the spine's top
        [UnityTest] public IEnumerator IdrasLineIsAcrossTheWholeTopOfTheSpineAndStepFivesArrowIsOnIt()
        {
            yield return Load();
            var top=Point("end.spineTop");
            Assert.AreEqual("IdraSpineBraced",top.dialogueNode);Assert.AreEqual("Idra",top.dialogueSpeaker);Assert.AreEqual("Spine",top.report,"the spine's top doesn't report Spine (step 5's arrow)");
            Assert.IsTrue(top.startsOnCrossing,"Idra's line isn't a crossing");
            var box=Trigger(top);Assert.Less(Flat(box.center,SpineTop),1.5f,"Idra's line isn't at the top of the spine");
            Assert.That(box.min.y,Is.LessThanOrEqualTo(Up+.2f),"the crossing's box starts above the spine's floor");
            // A way round it is walkable floor across the landing outside the box. Broken first: a box half as wide leaves one.
            var narrow=new Bounds(box.center,new Vector3(box.size.x*.5f,box.size.y,box.size.z));
            Assert.IsNotEmpty(WaysRound(narrow,SpineTop.z,-6,6,Up),"fixture: the check finds no way round a box half the landing's width");
            Assert.IsEmpty(WaysRound(box,SpineTop.z,-6,6,Up),"the spine's top can be walked past Idra's line at x "+string.Join(", ",WaysRound(box,SpineTop.z,-6,6,Up)));
            // Step 5's arrow: the panel aims at the spine's top while it is wanted, and not once it is passed.
            State.questSteps["TurnBack"]=5;yield return new WaitForSecondsRealtime(.6f);
            var hud=Object.FindFirstObjectByType<ObjectiveHud>();Assert.IsNotNull(hud,"fixture: there is no objective panel");
            Assert.IsTrue(hud.Goal.HasValue&&Flat(hud.Goal.Value,top.transform.position)<2,"step 5's arrow isn't on the spine's top (it aims at "+hud.Goal+")");
        }

        // ================================================================== deck four
        [UnityTest] public IEnumerator TheCaptainHoldsDeckFourAcrossItsWholeWidthUntilHisTalkThenStepsAsideAndHisCrewGo()
        {
            yield return Load("DeckFour");
            var line=Point("end.captainPassed");
            Assert.AreEqual("PatrolLetsPass",line.dialogueNode);Assert.AreEqual("Patrol",line.dialogueSpeaker);Assert.IsTrue(line.startsOnCrossing);
            var box=Trigger(line);
            var narrow=new Bounds(box.center,new Vector3(box.size.x*.5f,box.size.y,box.size.z));
            Assert.IsNotEmpty(WaysRound(narrow,box.center.z,-26,26,Down),"fixture: the check finds no way round a box half the deck's width");
            Assert.IsEmpty(WaysRound(box,box.center.z,-26,26,Down),"deck four can be crossed round the captain's line at x "+string.Join(", ",WaysRound(box,box.center.z,-26,26,Down)));
            var captain=All<Guide>().Single(g=>g.name.StartsWith("The captain"));var crew=All<LeavesAfter>().Where(l=>l.flag=="end.captainPassed").ToArray();
            Assert.AreEqual(4,crew.Length,"the captain's crew aren't four, each leaving on his line");
            Assert.Less(Flat(captain.transform.position,CaptainAt),1,"the captain doesn't stand across the way before his talk");
            Assert.Greater(box.center.z,DeckFourStart+4,"fixture");Assert.Less(box.center.z,captain.transform.position.z,"the captain's line is past him: a walker meets him before his talk");
            var hail=All<RadioHail>().Single(h=>h.speaker=="Patrol");CollectionAssert.AreEqual(new[]{"hail.aside"},hail.situations);CollectionAssert.Contains(hail.needs,"end.captainPassed");
            // His line said: he walks aside and holds the deck from there; his crew go out by the east door and are gone.
            Flag("end.captainPassed");
            yield return Until(()=>Flat(captain.transform.position,CaptainAside)<1&&crew.All(c=>!c.gameObject.activeInHierarchy),50);
            Assert.Less(Flat(captain.transform.position,CaptainAside),1,"the captain never stepped aside (at "+captain.transform.position+")");
            foreach(var c in crew)Assert.IsFalse(c.gameObject.activeInHierarchy,c.name+" never left (at "+c.transform.position+")");
            Assert.IsTrue(Reach(Lead.transform.position,new Vector3(0,Down,100)),"the deck's way on isn't walkable once he has stepped aside");
        }
        const float DeckFourStart=59;

        // ================================================================== the anchor works
        [UnityTest] public IEnumerator TheWorksSealBehindThePartnerAndTheirNorthDoorOpensOnlyOnceTheCrewsAreDown()
        {
            yield return Load("DeckFour");
            var works=Encounter("Continuance_Works");Assert.AreEqual(2,works.membranes.Length,"the works' fight doesn't hold its two doors");
            var south=works.membranes.OrderBy(m=>m.transform.position.z).First();var north=works.membranes.OrderBy(m=>m.transform.position.z).Last();
            Assert.IsTrue(south.Open,"the works' south door is shut before anyone has gone in");Assert.IsFalse(north.Open,"the works' north door is open before the fight");
            var box=Trigger(works);
            Assert.GreaterOrEqual(box.min.z-south.transform.position.z,3,"the works' fight begins less than three metres inside its door: the partner is caught in the doorway");
            Assert.GreaterOrEqual(Mathf.Abs(Vector3.Scale(south.GetComponent<BoxCollider>().size,south.transform.lossyScale).x),3,"the works' south door opens less than three metres");
            var berthPad=Berth.Find("HullBerth").pad;Assert.Greater(berthPad.transform.position.z,north.transform.position.z,"the bay's pad isn't past the works' last door");
            // The crews down: a load with the fight won opens both.
            Flag("clear.Continuance_Works");yield return Load("DeckFour");
            works=Encounter("Continuance_Works");
            foreach(var m in works.membranes)Assert.IsTrue(m.Open,m.name+" is shut with the works' crews down");
        }

        // ================================================================== the flight berth round the hull
        [UnityTest] public IEnumerator TheBaysEmptyCradlePutsThePartyOverTheHullsBackAndMeretsHailIsAcrossTheWholeBerthBeforeHerDoor()
        {
            Flag("clear.Continuance_Works");
            yield return Load("LaunchBay");
            var berth=Berth.Find("HullBerth");Assert.IsNotNull(berth,"the Continuance declares no HullBerth");
            Assert.AreEqual(ZoneKind.SpaceCombat,berth.kind);Assert.AreEqual("Launch — After the director's craft",berth.pad.prompt);
            Assert.IsTrue(NavMesh.SamplePosition(berth.pad.transform.position,out _,1.5f,NavMesh.AllAreas),"the berth's pad isn't on the bay's walkable floor");
            Assert.IsTrue(berth.Contains(berth.launch.transform.position),"the way back isn't inside the berth");
            // The door, its checkpoint and its mouth.
            var door=Encounter("MeretShip");
            Assert.AreEqual("end.meretHailed",door.requiredFlag);Assert.AreEqual("LaunchBay",door.checkpointSpawn,"a defeat over the hull doesn't go back to the bay");
            var checkpoint=All<SpawnPoint>().Single(s=>s.id=="LaunchBay");
            Assert.Less(Vector3.Distance(checkpoint.transform.position,door.transform.position),60,"the door's checkpoint is a long way from it");
            Assert.IsTrue(NavMesh.SamplePosition(checkpoint.transform.position,out _,1.5f,NavMesh.AllAreas),"the door's checkpoint isn't on walkable floor");
            var doorBox=Trigger(door);Assert.That(doorBox.min.y,Is.LessThan(Plane-1));Assert.That(doorBox.max.y,Is.GreaterThan(Plane+1),"the door's box doesn't span the plane");
            // The mouth (its south face) is clear of anything solid at the plane for at least nine metres. Broken first: a wall across it is found.
            bool Clear(){int run=0,best=0;for(float x=doorBox.min.x+.5f;x<doorBox.max.x;x+=1){bool open=!Physics.CheckSphere(new Vector3(x,Plane+.8f,doorBox.min.z),2.2f,~0,QueryTriggerInteraction.Ignore);run=open?run+1:0;best=Mathf.Max(best,run);}return best>=9;}
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(doorBox.center.x,Plane,doorBox.min.z);wall.transform.localScale=new Vector3(doorBox.size.x+4,6,1);Physics.SyncTransforms();
            Assert.IsFalse(Clear(),"fixture: a wall across the door's mouth isn't found");Object.DestroyImmediate(wall);Physics.SyncTransforms();
            Assert.IsTrue(Clear(),"the door's mouth has no nine metres clear at the plane");
            // The hail: across the whole berth, between where the party comes out and the door.
            var hail=Point("end.meretHailed");Assert.AreEqual("MeretShipToShip",hail.dialogueNode);Assert.AreEqual("Meret",hail.dialogueSpeaker);Assert.IsTrue(hail.startsOnCrossing);
            var hb=Trigger(hail);Assert.LessOrEqual(hb.min.x,HullBox.min.x+.1f,"Meret's hail can be flown round on the west");Assert.GreaterOrEqual(hb.max.x,HullBox.max.x-.1f,"Meret's hail can be flown round on the east");
            Assert.Greater(hb.min.z,HullFlight.z,"the party comes out past Meret's hail");Assert.Less(hb.max.z,doorBox.min.z,"Meret's hail is inside her door");
            Assert.That(hb.min.y,Is.LessThan(Plane));Assert.That(hb.max.y,Is.GreaterThan(Plane),"the hail's box doesn't span the plane");
            // Launch: both heroes over the hull at the plane in its box, seen with the flight camera; then back on foot in the bay.
            Assert.IsFalse(Lead.flight,"fixture: the party is already flying in the bay");
            Put(Lead,berth.pad.transform.position+Vector3.back*1.2f);yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);
            PromptService.FindInteraction(Lead.transform.position);Assert.AreSame(berth.pad,PromptService.Interaction,"the bay's cradle doesn't offer \""+berth.pad.prompt+"\" (offered \""+(PromptService.Interaction?.prompt??"nothing")+"\")");
            Assert.IsTrue(PromptService.TryInteract(),"A didn't take the cradle's prompt");
            yield return Until(()=>Berth.Docked&&!Berth.Changing,4);Assert.IsTrue(Berth.Docked,"the cradle didn't put the party over the hull");
            foreach(var hero in PartyController.Current.members)
            {
                Assert.IsTrue(hero.flight,hero.character+" isn't flying over the hull");Assert.AreEqual(Plane,hero.transform.position.y,.1f,hero.character+" isn't at the berth's plane");
                Assert.IsTrue(berth.Contains(hero.transform.position),hero.character+" came out outside the berth");Assert.Less(hero.transform.position.z,hb.min.z,hero.character+" came out past Meret's hail");
            }
            Assert.AreEqual("Flight",ZoneController.Current.CameraProfile.name,"the berth isn't seen with the flight camera");
            Put(Lead,berth.launch.transform.position+Vector3.back*2);yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);
            PromptService.FindInteraction(Lead.transform.position);Assert.AreSame(berth.launch,PromptService.Interaction,"the launch hatch doesn't offer the way back");
            Assert.IsTrue(PromptService.TryInteract());yield return Until(()=>!Berth.Docked&&!Berth.Changing,4);
            Assert.IsFalse(Berth.Docked,"the way back didn't land the party");Assert.IsFalse(Lead.flight,"back from the berth the lead is still flying");
            Assert.Less(Flat(Lead.transform.position,berth.foot.position),3,"back from the berth the party isn't set down in the bay");
        }

        /// <summary>Her fight holds the bay: from her door's start the works' north door, the only way from the bay back into the ship, is shut and the bay can't be walked out of; and the
        /// bay's pad, her fight's own, still takes a party that launched back down into it up into her fight. Its launch answers through her fight as every launch does, and the pad
        /// refused for as long as her craft fought within reach of the bay, which is all of her fight: shown first with the pad made one that isn't her fight's.</summary>
        [UnityTest] public IEnumerator HerFightHoldsTheBayShutAndItsPadTakesAPartyBackUpIntoIt()
        {
            Flag("clear.Continuance_Works");Flag("end.meretHailed");
            yield return Load("LaunchBay");
            var berth=Berth.Find("HullBerth");var door=Encounter("MeretShip");var works=Encounter("Continuance_Works");
            var north=works.membranes.OrderBy(m=>m.transform.position.z).Last();
            Assert.IsTrue(door.membranes!=null&&door.membranes.Contains(north),"her door holds no door: mid-fight the bay is walked out of, 300 m to the pad home");
            Assert.IsTrue(north.Open,"fixture: the works' north door is shut before her fight, with their crews down");
            var works2=new Vector3(-8,Down,140);Assert.IsTrue(Reach(LaunchBay,works2),"fixture: the bay can't be walked out of into the works before her fight");
            Put(Lead,berth.pad.transform.position+Vector3.back*1.2f);
            // Her body was on show before her door (Load quiets every brain it finds, hers among them): her fight is woken as her door wakes it.
            door.Begin();yield return Frames(2);
            foreach(var boss in Object.FindObjectsByType<BossController>(FindObjectsSortMode.None))boss.GetComponent<EnemyBrain>().Passive=false;
            yield return Frames(2);yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.3f);
            Assert.IsTrue(BossController.AnyFightUnderway,"fixture: her fight is under way over the bay");
            Assert.IsFalse(north.Open,"her fight began and the works' north door stands open behind the bay");
            Assert.IsFalse(Reach(LaunchBay,works2),"her fight is under way and the bay can still be walked out of into the works");
            bool built=berth.inFight;
            berth.inFight=false;Assert.IsFalse(berth.pad.Available,"fixture: a pad that isn't her fight's is offered in the middle of it");
            berth.inFight=built;
            Assert.IsTrue(berth.pad.Available,"a party back in the bay in the middle of her fight is refused the way back up into it");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
        }

        // ================================================================== the camera over the hull
        [UnityTest] public IEnumerator NothingOfTheHullStandsAboveThePlaneOnTheCamerasSideOfTheBerthsLanes()
        {
            yield return Load("LaunchBay");
            // Broken first: a mast stood in the lanes is found.
            var mast=GameObject.CreatePrimitive(PrimitiveType.Cube);mast.name="A test mast";mast.transform.position=new Vector3(0,Plane+2,200);mast.transform.localScale=new Vector3(2,6,2);
            yield return null;Assert.IsTrue(OverTheLanes().Any(f=>f.StartsWith("A test mast")),"fixture: a mast over the lanes isn't found");Object.DestroyImmediate(mast);yield return null;
            var over=OverTheLanes();Assert.IsEmpty(over,"drawn above the plane between the flight camera and the berth's lanes:\n"+string.Join("\n",over.Take(20)));
            // North of the lanes stand the shelter's wings, the superstructure and the gantry: they are what stops a ship there, and they are solid at the plane.
            foreach(var at in new[]{new Vector3(-14,Plane,252),new Vector3(6,Plane,252),new Vector3(-4,Plane,263.5f),new Vector3(16,Plane,256),new Vector3(32,Plane,256)})
                Assert.IsTrue(Physics.CheckSphere(at,.5f,~0,QueryTriggerInteraction.Ignore),"nothing solid at the plane at "+at+" north of the lanes");
        }

        // ================================================================== after her craft
        [UnityTest] public IEnumerator MeretIsAtTheGantrysRailOnlyAfterHerCraftAndTheDockToItOnlyAfterHerLine()
        {
            Flag("clear.Continuance_Works");
            yield return Load("LaunchBay");
            DockingPad Gantry()=>All<DockingPad>().SingleOrDefault(d=>d.scene=="StillwaterChamber");
            GameObject Meret()=>All<Transform>().Select(t=>t.gameObject).SingleOrDefault(g=>g.name=="Meret at the gantry's rail");
            var gantry=Gantry();Assert.IsNotNull(gantry,"there is no dock to the frame gantry");Assert.AreEqual("Gantry",gantry.spawn);Assert.AreEqual("Dock — The Anchor Chamber's frame gantry",gantry.prompt,"the dock to the gantry doesn't name the zone it leads to");
            Assert.Less(Flat(gantry.transform.position,GantryDock),2,"the dock isn't at the gantry's rail");Assert.AreEqual(Plane,gantry.transform.position.y,.5f,"the gantry's dock isn't at the plane");
            var meret=Meret();Assert.IsNotNull(meret,"Meret isn't stood at the gantry's rail for after her craft");
            Assert.IsFalse(gantry.gameObject.activeInHierarchy,"the dock to the gantry is drawn before Meret has spoken from it");
            Assert.IsFalse(meret.activeInHierarchy,"Meret is at the gantry's rail before her craft is down");
            var home=All<DockingPad>().Single(d=>d.scene=="LastMooring");Assert.IsTrue(home.gameObject.activeInHierarchy,"fixture: the way home is shut on arriving");
            var talk=All<AfterTalk>().Single(t=>t.node=="MeretGantry");Assert.AreEqual("bossdown.MeretShip",talk.flag);Assert.AreEqual("Meret",talk.speaker);Assert.AreEqual("end.meretShip",talk.heardFlag);
            Assert.IsTrue(All<BossExit>().Any(e=>e.boss=="MeretShip"&&Flat(e.transform.position,GantryDock)<15),"her craft has no way out at the gantry");
            // Her line heard, then her craft down (in that order, so the talk isn't played here): the dock is drawn, she stands at the rail, and the way home is gone.
            Flag("end.meretShip");Flag("bossdown.MeretShip");yield return new WaitForSecondsRealtime(.6f);
            Assert.IsTrue(Gantry().gameObject.activeInHierarchy,"the dock to the gantry isn't drawn once Meret has spoken");
            Assert.IsTrue(Meret().activeInHierarchy,"Meret isn't at the gantry's rail once her craft is down");
            Assert.Less(Flat(Meret().transform.position,GantryRail),2,"Meret isn't at the gantry's rail");
            Assert.IsFalse(home.gameObject.activeInHierarchy,"the way home to Last Mooring is still open after phase one");
        }
    }
}
