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
    /// The anchor chamber as a player meets it (docs/maps/StillwaterChamber.md; ch10-wiring sections 1, 2, 6.5, 6.6, 10): docked at the gantry, whose door is the checkpoint and whose lift
    /// says what opens it and carries the party down only once her frame is beaten, standing still through a conversation (D275); the well's door a checkpoint with her talk at the lift's
    /// foot; nothing tall between the camera and either floor's near edge; her yield standing, the crews walking in, the last collar opened only by a hold through a whole ease; the
    /// anchor letting go one clamp at a time and then together, the chain running out, the light turning, her talk and nothing barked over it; a save after it loading the anchor let go with
    /// nothing played; Ilo's custody and the launch. Every check is first run against the broken state, inside the test. Flags are set directly where a talk or a fight would set them.
    /// </summary>
    public sealed class StillwaterChamberTests
    {
        const string Zone="StillwaterChamber";
        // The builder's numbers (LastMooringBuilder.Chamber), copied: the tests can't see the editor assembly.
        static readonly Vector3 GantryAt=new(0,0,-21),ArrivalAt=new(0,0,-30.5f),MeretGantryAt=new(0,0,0),LiftTop=new(0,0,12.5f),LiftFoot=new(0,-16,46.4f),WellAt=new(0,-16,47.6f),W=new(0,-16,63),CollarSeat=new(0,-16,78.4f),A=new(0,-16,83.5f);
        const float WellRadius=12,DeckX=19,DeckZ0=-17;

        [ContentRegistry.Expectations] static void ChamberExpectations(ContentRegistry.Book book){}

        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter10");
            foreach(var flag in new[]{"end.planHeard","end.planSaid","end.sorrelSeen","end.motherFreed","end.sectionHeld","bossdown.Tollkeeper","end.gateOpen","end.spineTop","end.boarded",
                "end.captainPassed","clear.Continuance_Works","end.meretHailed","bossdown.MeretShip","end.meretShip"})Flag(flag);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Until(System.Func<bool> done,float seconds){float end=Time.unscaledTime+seconds;while(!done()&&Time.unscaledTime<end)yield return null;}
        static IEnumerator Load(string spawn)
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
            if(body.TryGetComponent<GroundMotor>(out var feet))feet.Halt();body.transform.position=at;controller.enabled=on;Physics.SyncTransforms();
        }
        static bool Reach(Vector3 from,Vector3 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(from,out var start,2.5f,NavMesh.AllAreas)&&NavMesh.SamplePosition(to,out var end,2,NavMesh.AllAreas)&&
                NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
        static EncounterVolume Encounter(string id){var found=All<EncounterVolume>().Where(e=>e.encounterId==id).ToArray();Assert.AreEqual(1,found.Length,id+" is not in the chamber exactly once");return found[0];}
        static Bounds Trigger(Component c){var box=c.GetComponent<BoxCollider>();Assert.IsNotNull(box,c.name+" has no box");return new Bounds(c.transform.TransformPoint(box.center),Vector3.Scale(box.size,c.transform.lossyScale));}
        static AfterTalk Talk(string node)=>All<AfterTalk>().SingleOrDefault(t=>t.node==node);
        /// <summary>The ground camera's lens for a hero standing at a point (CameraProfile Ground: pitch 40, yaw 20, 19.5 m), and whether anything solid stands between it and his chest.</summary>
        static bool Hidden(Vector3 feet,out string what)
        {
            var dir=Quaternion.Euler(40,20,0)*Vector3.forward;var chest=feet+Vector3.up*1.3f;var lens=chest-dir*19.5f;what="";
            foreach(var hit in Physics.RaycastAll(chest,lens-chest,Vector3.Distance(chest,lens)-.5f,~0,QueryTriggerInteraction.Ignore))
            {if(hit.collider is CharacterController||hit.collider.GetComponentInParent<CombatActor>()!=null||hit.collider.GetComponentInParent<EnemyBrain>()!=null||hit.collider.GetComponentInParent<Membrane>()!=null)continue;what=hit.collider.name+" at "+hit.point;return true;}
            return false;
        }

        // ================================================================== the gantry: the dock, the door, the lift's lock
        [UnityTest] public IEnumerator DockedAtTheGantryItsDoorIsACheckpointAndTheLiftSaysItWaitsForHerFrameAndOpensWithIt()
        {
            yield return Load("Gantry");
            Assert.AreEqual(ZoneKind.GroundCombat,ZoneController.Current.definition.kind);
            foreach(var id in new[]{"Arrival","Gantry","Well"})Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id==id),"the chamber has no spawn "+id);
            Assert.Less(Flat(Lead.transform.position,GantryAt),3,"the Continuance's dock doesn't set the party down at her door's checkpoint on the gangway");
            var first=All<SpawnPoint>().Single(s=>s.id=="Arrival").transform.position;Assert.Less(Flat(first,ArrivalAt),1,"the zone's arrival isn't on the landing");
            Assert.Greater(Flat(first,MeretGantryAt)-Flat(All<SpawnPoint>().Single(s=>s.id=="Gantry").transform.position,MeretGantryAt),5,"a fall on the gantry retries no nearer her than the zone's first arrival");
            var door=Encounter("MeretGantry");Assert.AreEqual("end.meretShip",door.requiredFlag);Assert.AreEqual("Gantry",door.checkpointSpawn);
            var box=Trigger(door);var checkpoint=All<SpawnPoint>().Single(s=>s.id=="Gantry").transform.position;
            Assert.IsFalse(box.Contains(checkpoint),"the gantry's checkpoint is inside its fight");Assert.Less(Vector3.Distance(checkpoint,MeretGantryAt),60,"the gantry's checkpoint is over 60 m from her");
            var seal=door.membranes.Single();Assert.GreaterOrEqual(box.min.z-seal.transform.position.z,2.9f,"the gantry's fight starts less than 3 m inside its seal: the partner would be shut out");
            var span=Vector3.Scale(seal.GetComponent<BoxCollider>().size,seal.transform.lossyScale);Assert.Greater(Mathf.Max(Mathf.Abs(span.x),Mathf.Abs(span.z)),3,"the gantry's seal is narrower than 3 m");
            var exit=All<BossExit>().Single(e=>e.boss=="MeretGantry");Assert.IsTrue(exit.sinks,"her way down doesn't go down");
            float toLift=Flat(exit.transform.position,MeretGantryAt);Assert.That(toLift,Is.InRange(10f,14f),"her way down is not 10 to 14 m from where she stands");
            // The lift: refused while her frame is up, saying so; the walk down shut with it.
            var lift=All<ChamberLift>().Single();Assert.AreEqual("Ride down — The Control Well",lift.prompt);
            Put(Lead,LiftTop+Vector3.back*1.5f);yield return Frames(3);
            // Broken first: with its gate held open by hand the well is walked to before her frame is beaten, which the lock exists to stop.
            lift.gate.SetOpen(true);yield return Frames(3);Assert.IsTrue(Reach(Lead.transform.position,W),"fixture: the well can't be walked to even with the lift's gate open");
            lift.gate.SetOpen(false);yield return Frames(3);
            Assert.IsFalse(Reach(Lead.transform.position,W),"the well can be walked to from the gantry before her frame is beaten");
            var before=Lead.transform.position;lift.Interact();yield return Frames(5);
            StringAssert.Contains("frame",lift.prompt,"the lift's refusal doesn't say her frame is what holds it");Assert.IsFalse(lift.Riding,"the lift ran with her frame still up");
            Assert.Less(Vector3.Distance(before,Lead.transform.position),.3f,"the refused lift moved the party");
            Flag("bossdown.MeretGantry");yield return Frames(4);
            Assert.AreEqual("Ride down — The Control Well",lift.prompt,"the lift's prompt still says it's refused once her frame is down");
            Assert.IsTrue(lift.gate.Open,"the walk down beside the lift is still shut once her frame is down");
            Assert.IsTrue(Reach(Lead.transform.position,W),"the well can't be walked to once the lift runs");
        }

        // ================================================================== the lift holds through a conversation (D275)
        [UnityTest] public IEnumerator TheLiftCarriesBothDownAndStandsStillWhileAConversationHoldsTheControls()
        {
            Flag("bossdown.MeretGantry");yield return Load("Gantry");
            var lift=All<ChamberLift>().Single();Put(Lead,LiftTop);yield return Frames(3);
            lift.Interact();Assert.IsTrue(lift.Riding,"the lift didn't take the party");
            yield return new WaitForSecondsRealtime(1.2f);Assert.Greater(lift.Travel,0,"the lift isn't moving");
            // Broken first: a lift that runs on through a talk carries the party off under it.
            lift.runsThroughTalks=true;Assert.IsTrue(Npc.Say("IloCustody","Ilo"),"fixture: no conversation could be started");yield return Frames(4);
            Assert.IsTrue(GameServices.Current.Input.Blocked,"fixture: the conversation doesn't hold the controls");
            float was=lift.Travel;yield return new WaitForSecondsRealtime(.8f);
            Assert.Greater(lift.Travel,was+.01f,"the broken lift stood still anyway: this check can't fail");
            // The rule: through a talk the cage and both heroes stand where they are.
            lift.runsThroughTalks=false;yield return Frames(2);was=lift.Travel;var lead=Lead.transform.position;
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.AreEqual(was,lift.Travel,.0001f,"the lift ran on while a conversation held the controls");Assert.Less(Vector3.Distance(lead,Lead.transform.position),.02f,"the lead was carried on through the talk");
            Assert.IsTrue(lift.Held,"the lift doesn't say it is held");
            DialogueSystem.Current.Cancel();GameServices.Current.Input.Blocked=false;
            yield return Until(()=>!lift.Riding,14);
            Assert.IsFalse(lift.Riding,"the lift never reached the foot once the talk ended");
            Assert.Less(Flat(Lead.transform.position,LiftFoot),3,"the lead isn't set down at the lift's foot");Assert.Less(Mathf.Abs(Lead.transform.position.y-LiftFoot.y),1,"the lead isn't at the well's level");
            foreach(var hero in PartyController.Current.members)Assert.IsTrue(hero.GetComponent<CharacterController>().enabled,hero.name+" was left off its own controller");
            Assert.AreEqual("Well",GameServices.Current.State.spawn,"riding down doesn't make the well's door the checkpoint");
        }

        // ================================================================== the well: its door, its talk at the lift's foot, its near rim
        [UnityTest] public IEnumerator TheWellsDoorIsACheckpointItsTalkWaitsAtTheLiftsFootAndNothingTallStandsBetweenTheCameraAndEitherFloor()
        {
            Flag("bossdown.MeretGantry");yield return Load("Well");
            var door=Encounter("MeretWell");Assert.AreEqual("end.wellEntered",door.requiredFlag);Assert.AreEqual("Well",door.checkpointSpawn);
            var box=Trigger(door);var checkpoint=All<SpawnPoint>().Single(s=>s.id=="Well").transform.position;Assert.IsFalse(box.Contains(checkpoint),"the well's checkpoint is inside its fight");
            Assert.IsTrue(Reach(checkpoint,W),"the well's checkpoint isn't on the floor that leads into it");
            var south=door.membranes.OrderBy(m=>m.transform.position.z).First();Assert.GreaterOrEqual(box.min.z-south.transform.position.z,2.9f,"the well's fight starts less than 3 m inside its door");
            var meets=All<DiscoveryPoint>().Single(p=>p.flag=="end.wellEntered");Assert.AreEqual("MeretWell",meets.dialogueNode);Assert.IsTrue(meets.startsOnCrossing,"her talk in the well isn't a crossing");
            Assert.Less(Flat(meets.transform.position,LiftFoot),10,"her talk is more than 10 m from the lift's foot: step 7's text would run behind the world");
            // Broken first: a hero sunk under the rim's level is hidden by the plating, so the check sees walls.
            var rim=W+new Vector3(-Mathf.Sin(20*Mathf.Deg2Rad),0,-Mathf.Cos(20*Mathf.Deg2Rad))*(WellRadius-.6f);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.localScale=new Vector3(4,6,1);wall.transform.position=rim+Quaternion.Euler(0,20,0)*Vector3.back*3+Vector3.up*3;Physics.SyncTransforms();
            Assert.IsTrue(Hidden(rim,out _),"fixture: the sightline check sees nothing through a wall six metres tall on the camera's side");Object.DestroyImmediate(wall);Physics.SyncTransforms();
            var hidden=new List<string>();
            for(float b=140;b<=300;b+=10){var p=W+new Vector3(Mathf.Sin(b*Mathf.Deg2Rad),0,Mathf.Cos(b*Mathf.Deg2Rad))*(WellRadius-.6f);if(Hidden(p,out var what))hidden.Add($"the well at {b} degrees: {what}");}
            for(float x=-DeckX+1;x<=DeckX-1;x+=3){var p=new Vector3(x,0,DeckZ0+.8f);if(Hidden(p,out var what))hidden.Add($"the gantry's south edge at x {x}: {what}");}
            for(float z=DeckZ0+1;z<=16;z+=3){var p=new Vector3(-DeckX+.8f,0,z);if(Hidden(p,out var what))hidden.Add($"the gantry's west edge at z {z}: {what}");}
            Assert.IsEmpty(hidden,"something tall stands between the camera and a floor where the heroes fight:\n"+string.Join("\n",hidden));
        }

        // ================================================================== her yield, the crews, the last collar
        [UnityTest] public IEnumerator SheYieldsStandingTheCrewsWalkInAndTheCollarOpensOnlyHeldThroughAWholeEase()
        {
            foreach(var f in new[]{"bossdown.MeretGantry","end.wellEntered","end.meretGantry","bossdown.MeretWell","clear.MeretWell"})Flag(f);
            yield return Load("Well");
            var stands=All<YieldsStanding>().Single();Assert.IsTrue(stands.body.activeInHierarchy,"she isn't standing in the well after her yield");
            foreach(var rig in stands.body.GetComponentsInChildren<Animator>())Assert.Greater(rig.speed,0,"her body is held in a frozen beaten pose");
            Assert.Less(Flat(stands.body.transform.position,W),12,"she stands outside the well");
            Assert.IsNotNull(Talk("MeretYields"),"nothing plays MeretYields");Assert.AreEqual("bossdown.MeretWell",Talk("MeretYields").flag);Assert.AreEqual("end.crewsOrdered",Talk("MeretYields").heardFlag);
            var guides=All<Guide>().Where(g=>g.legs.Length>1&&g.legs[1].flag=="end.crewsOrdered").ToArray();Assert.GreaterOrEqual(guides.Length,7,"the crews aren't there to walk in: four smiths, two of Idra's people, Ishen");
            Flag("end.crewsOrdered");GameServices.Current.Input.Blocked=false;
            yield return Until(()=>guides.All(g=>{var end=g.legs[1].path[^1];return Flat(g.transform.position,end)<.6f;}),20);
            foreach(var g in guides){var end=g.legs[1].path[^1];Assert.Less(Flat(g.transform.position,end),.6f,g.name+" never reached its post");}
            Assert.AreEqual("end.sequenceRead",Talk("IshenAnchorSequence").heardFlag);Assert.AreEqual("end.crewsOrdered",Talk("IshenAnchorSequence").flag);
            var collar=All<LastCollar>().Single();var anchor=AnchorRelease.Current;Assert.IsNotNull(anchor,"there is no anchor in the chamber");
            Assert.IsFalse(collar.Available,"the last collar is offered before Idra calls it");
            Flag("end.sequenceRead");yield return Frames(3);
            Assert.IsTrue(anchor.Easing,"Idra's \"Ease.\" isn't the anchor easing");Assert.IsTrue(collar.Available,"the last collar isn't offered on Idra's call");
            Assert.IsNotNull(All<DiscoveryPoint>().SingleOrDefault(p=>p.report=="LastCollar"&&p.Wanted),"step 9's arrow has nothing to find");
            Put(Lead,CollarSeat);yield return Frames(3);collar.Interact();Assert.IsNotNull(collar.Holder,"the collar wasn't taken");
            // Broken first: let go part way through an ease, and it stays shut.
            yield return Until(()=>!anchor.Easing,20);yield return Until(()=>anchor.Easing&&collar.RidingTheEase,20);
            Assert.IsTrue(collar.RidingTheEase,"fixture: the hold never rode an ease");
            yield return new WaitForSecondsRealtime(1);Put(Lead,CollarSeat+Vector3.back*4);yield return Until(()=>!anchor.Easing,6);yield return Frames(3);
            Assert.IsFalse(collar.IsOpen,"the collar opened though it was let go part way through the ease");Assert.IsFalse(Has("end.collarTaken"),"the collar's flag was set without a whole ease held");
            // Held through a whole one, it opens.
            Put(Lead,CollarSeat);yield return Frames(3);collar.Interact();
            yield return Until(()=>collar.IsOpen,40);
            Assert.IsTrue(collar.IsOpen&&Has("end.collarTaken"),"the collar never opened held through an ease");
        }

        // ================================================================== the anchor lets go
        [UnityTest] public IEnumerator TheAnchorLetsGoOneClampAtATimeThenTogetherTheChainRunsOutTheLightTurnsAndHerTalkPlaysWithNothingBarked()
        {
            foreach(var f in new[]{"bossdown.MeretGantry","end.wellEntered","end.meretGantry","bossdown.MeretWell","end.meretYielded","end.crewsOrdered","end.sequenceRead"})Flag(f);
            yield return Load("Well");Put(Lead,CollarSeat);yield return Frames(3);
            var anchor=AnchorRelease.Current;Assert.AreEqual(AnchorRelease.Stage.Holding,anchor.Now);Assert.AreEqual(4,anchor.jaws.Length,"the anchor hasn't four clamps");
            var rest=anchor.jaws.Select(j=>j.localRotation).ToArray();Assert.IsTrue(anchor.chain.gameObject.activeInHierarchy,"the chain isn't there before it runs");
            Assert.IsTrue(All<DockingPad>().Where(d=>d.scene=="ReturningPassage").All(d=>!d.gameObject.activeInHierarchy),"the launch is open before Ilo has her");
            Flag("end.collarTaken");float began=Time.unscaledTime;yield return Frames(2);
            Assert.AreEqual(AnchorRelease.Stage.Opening,anchor.Now,"taking the last collar doesn't begin the release");Assert.IsTrue(GameServices.Current.Input.Blocked,"the party isn't held through the release");
            // One by one: after the second has gone and before the fourth, two are open and two still hold.
            yield return Until(()=>anchor.Released>=2,4);int moved=anchor.jaws.Count(j=>Quaternion.Angle(j.localRotation,rest[System.Array.IndexOf(anchor.jaws,j)])>5);
            Assert.That(anchor.Released,Is.InRange(2,3),"the clamps didn't let go one at a time");Assert.Less(moved,4,"all four jaws moved at once");
            yield return Until(()=>anchor.Released==4&&anchor.ChainOut>0,8);
            for(int i=0;i<4;i++)Assert.Greater(Quaternion.Angle(anchor.jaws[i].localRotation,rest[i]),20,"clamp "+(i+1)+"'s jaw never opened");
            yield return Until(()=>anchor.Gone,14);Assert.IsTrue(anchor.Gone,"the chain never ran out");Assert.Greater(anchor.Warmth,.9f,"the light never turned");
            yield return Until(()=>DialogueSystem.Current!=null&&DialogueSystem.Current.Running,6);
            Assert.AreEqual("MeretSeesItOpen",DialogueSystem.Current.Node,"her talk doesn't play as the anchor opens");
            Assert.Less(Time.unscaledTime-began,16,"her talk waited too long after the collar");
            // Nothing barks from her "Yes." to Ilo.
            Flag("end.anchorReleased");DialogueSystem.Current.Cancel();yield return Frames(4);
            var said=new List<string>();void Heard(string who,string line)=>said.Add(who+": "+line);
            BarkService.Spoken+=Heard;BarkService.Play("Taren","anchor",true);yield return Frames(3);BarkService.Spoken-=Heard;
            Assert.IsEmpty(said,"a bark played between her \"Yes.\" and Ilo");
            Assert.AreEqual("end.custody",Talk("IloCustody").heardFlag);Assert.AreEqual("end.anchorReleased",Talk("IloCustody").flag);
            Flag("end.custody");yield return Frames(4);
            var launch=All<DockingPad>().Single(d=>d.scene=="ReturningPassage");Assert.IsTrue(launch.gameObject.activeInHierarchy,"the launch to the passage isn't there once Ilo has her");
            Assert.AreEqual("Arrival",launch.spawn);Assert.AreEqual("Launch — The Returning Passage",launch.prompt);
        }

        // ================================================================== the release, seen
        /// <summary>What the game's camera draws this frame with every canvas over it, as a 1920 by 1080 still in the mirror's Builds/quality/focused/anchor-release (as the credits' tests
        /// write theirs); returns how warm the frame is (the mean of red less blue, 0 to 1, over a grid of its pixels).</summary>
        static float Still(string name)
        {
            const int Wide=1920,High=1080;var camera=Camera.main;Assert.IsNotNull(camera,"fixture: no game camera");
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var rt=new RenderTexture(Wide,High,24,RenderTextureFormat.ARGB32);var image=new Texture2D(Wide,High,TextureFormat.RGB24,false);var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;
                foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=camera.nearClipPlane+.1f;}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,Wide,High),0,0);image.Apply();
                string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/focused/anchor-release"));System.IO.Directory.CreateDirectory(folder);
                string file=System.IO.Path.Combine(folder,name+".png");System.IO.File.WriteAllBytes(file,image.EncodeToPNG());TestContext.WriteLine("ANCHOR_STILL "+file);
                float warm=0;int n=0;for(int y=8;y<High;y+=16)for(int x=8;x<Wide;x+=16){var p=image.GetPixel(x,y);warm+=p.r-p.b;n++;}
                return warm/Mathf.Max(1,n);
            }
            finally
            {
                foreach(var c in canvases)if(c!=null){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}
                camera.targetTexture=target;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(image);
            }
        }
        static bool InFrame(Vector3 p){var v=Camera.main.WorldToViewportPoint(p);return v.z>0&&v.x>.02f&&v.x<.98f&&v.y>.02f&&v.y<.98f;}
        /// <summary>The moment the game has walked toward, as the player's camera shows it (the warm turn and the running chain happen only in play, so no editor still had ever shown it):
        /// stills held, at the first clamp, the second, all four thrown back, the chain running, the light turned and the road flowing, in the mirror's
        /// Builds/quality/focused/anchor-release. While it opens the view takes in all four clamps and the chain's head; the frame turns warm; the road's rings move. Each check is first
        /// shown to fail on a case built wrong inside the test.</summary>
        [UnityTest] public IEnumerator TheReleaseIsSeenClampByClampTheChainRunsAndTheChamberTurnsWarm()
        {
            foreach(var f in new[]{"bossdown.MeretGantry","end.wellEntered","end.meretGantry","bossdown.MeretWell","end.meretYielded","end.crewsOrdered","end.sequenceRead"})Flag(f);
            yield return Load("Well");Put(Lead,CollarSeat);
            var partner=PartyController.Current.members.FirstOrDefault(m=>m!=Lead);if(partner!=null)Put(partner,CollarSeat+new Vector3(2,0,-.8f));
            yield return new WaitForSecondsRealtime(1.2f);
            var anchor=AnchorRelease.Current;Assert.IsNotNull(anchor,"there is no anchor in the chamber");
            float held=Still("0-held");
            // The checks can fail: a point behind the lens isn't in frame, and a frame compared with itself hasn't turned.
            Assert.IsFalse(InFrame(Camera.main.transform.position-Camera.main.transform.forward*10),"fixture: a point behind the camera is taken for in frame");
            bool Turned(float from,float to)=>to>from+.04f;Assert.IsFalse(Turned(held,held),"fixture: a frame compared with itself has turned");
            Flag("end.collarTaken");
            yield return Until(()=>anchor.Released>=1,4);yield return new WaitForSecondsRealtime(.35f);Still("1-first-clamp");
            yield return Until(()=>anchor.Released>=2,4);yield return new WaitForSecondsRealtime(.35f);Still("2-second-clamp");
            // Drawn back: every clamp and the chain's head in the frame while they let go.
            var missing=new List<string>();
            foreach(var jaw in anchor.jaws)if(!InFrame(jaw.position))missing.Add(jaw.name+" at "+jaw.position);
            var head=anchor.chain.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            if(!InFrame(new Vector3(A.x,Mathf.Min(head.max.y,A.y+12),A.z)))missing.Add("the chain's head");
            yield return Until(()=>anchor.Released>=4&&anchor.ChainOut>0,6);yield return new WaitForSecondsRealtime(.6f);Still("3-all-four");
            yield return Until(()=>anchor.ChainOut>anchor.chainRun*.3f,8);Still("4-chain-running");
            yield return Until(()=>anchor.Warmth>.97f,8);float warm=Still("5-light-turned");
            var ring=anchor.road.Length>0?anchor.road[0]:null;var was=ring!=null?ring.position:Vector3.zero;
            yield return new WaitForSecondsRealtime(1.6f);Still("6-road-flowing");
            Assert.IsEmpty(missing,"the view drawn back doesn't take in what lets go: "+string.Join(", ",missing));
            Assert.IsTrue(Turned(held,warm),$"the chamber didn't turn warm on screen (red less blue {held:0.000} held, {warm:0.000} let go)");
            Assert.IsNotNull(ring,"the road has no rings");Assert.Greater(Vector3.Distance(ring.position,was),.5f,"the road's rings don't flow once the light has turned");
        }

        // ================================================================== a save after it
        [UnityTest] public IEnumerator ASaveMadeAfterTheCollarLoadsTheAnchorLetGoWithNothingPlayed()
        {
            foreach(var f in new[]{"bossdown.MeretGantry","end.wellEntered","end.meretGantry","bossdown.MeretWell","end.meretYielded","end.crewsOrdered","end.sequenceRead","end.collarTaken","end.anchorReleased","end.custody"})Flag(f);
            var logs=new List<string>();void Log(string message,string _,LogType __)=>logs.Add(message);Application.logMessageReceived+=Log;
            yield return Load("Well");yield return Frames(10);Application.logMessageReceived-=Log;
            var anchor=AnchorRelease.Current;Assert.AreEqual(AnchorRelease.Stage.After,anchor.Now,"the anchor isn't let go on loading");
            Assert.AreEqual(4,anchor.Released);Assert.IsTrue(anchor.Gone,"the chain is still there on loading");Assert.AreEqual(1,anchor.Warmth,.001f,"the light is still held on loading");
            Assert.IsFalse(logs.Any(l=>l.StartsWith("ANCHOR_RELEASE_BEGIN")),"the release played again on loading");
            Assert.IsFalse(GameServices.Current.Input.Blocked,"the party is held on loading after the release");
            Assert.IsFalse(All<LastCollar>().Single().Available,"the last collar is offered again after it opened");
        }
    }
}
