using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 9's dungeon, the Separation Works (docs/maps/SeparationWorks.md): walked north through the hulls the station was built from, a collar holding each until its joint
    /// is opened by hand; its rule (a Compact rig is clamped to whatever has been turned on the party, its cable shows what it drives, and cutting it gives the machine back) as the throat
    /// teaches it, joint 2's press needs it, and the gallery's fight uses it; a press's blow at an ordinary hit's weight, inside its marking only and never the last of it, and on a crew
    /// under it too; the marking read over a bay from the fixed camera; a joint kept in the save; the script's talks, the table, the caches and the ways home; the press hall built to
    /// its contract; and no wall between the camera and a fight. Flags are set directly where a conversation or a fight would set them: the script's nodes are the dialogue tests'.</summary>
    public sealed class SeparationWorksTests
    {
        const string Works="SeparationWorks";
        // The builder's numbers (HomeBuilder.Works), copied: the tests can't see the editor assembly.
        const float Tug=0,Hauler=-1.5f,Liner=-3,Barge=-6;
        static readonly Vector2 P=new(0,196),ThroatRig=new(9.8f,57.4f),ControlRig=new(25,90.2f),GalleryRig=new(14.2f,121.2f),LiftAt=new(20.8f,209.8f);
        /// <summary>Each joint's hand (HomeBuilder.Works.HandOf): a critical joint's 4.4 m to the side of its collar, an optional one's 3.6, 2.3 m out from the wall.</summary>
        static readonly Dictionary<int,Vector2> Hands=new(){[1]=new(4.4f,34),[2]=new(4.4f,95),[3]=new(4.4f,144),[4]=new(8.8f,16.4f),[5]=new(8.8f,46.4f),[6]=new(13.8f,130.4f)};
        static readonly (int index,Vector2 at,float bearing)[] Arms={(1,new(-4.3f,193.2f),225),(2,new(-2.6f,193.2f),205),(3,new(-.9f,193.2f),188),(4,new(.9f,193.2f),172),(5,new(2.6f,193.2f),155),(6,new(4.3f,193.2f),135)};
        static readonly Vector2[] Cover={new(-8,184.5f),new(8,184.5f)};
        readonly List<string> log=new();
        void Heard(string text,string stack,LogType type)
        {
            if(text.StartsWith("JOINT_")||text.StartsWith("PRESS_")||text.StartsWith("FIRST_SIGHT")||text.StartsWith("DISCOVERY"))log.Add(text);
        }
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter9");log.Clear();Application.logMessageReceived+=Heard;
            // The town has met: the way under the promenade is open, and Dorrow has said his piece (a hero put past his crossing would start it).
            Set("home.meetingHeld","home.worksRead");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void WorksExpectations(ContentRegistry.Book book)
        {
            // The lift home after the Saint: "The lift answers the press hall's board: release the Foundry Saint first". The shared check's own guess at a boss's words is its
            // title in capitals (THE FOUNDRY SAINT), which a sentence never prints.
            book.Lock("bossdown.FoundrySaint","Foundry Saint");
        }

        // ------------------------------------------------------------------ fixtures
        static void Set(params string[] keys){foreach(var key in keys)GameServices.Current.Flags.SetBool(key,true);}
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Zone(string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(Works,spawn);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Works+" never finished loading");Assert.AreEqual(Works,SceneManager.GetActiveScene().name);
            Hold();yield return new WaitForSecondsRealtime(.6f);
        }
        /// <summary>The heroes stand where they are put and nobody wanders into a crossing.</summary>
        static void Hold(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}}
        /// <summary>The ground under a plan point: the first solid thing a ray from above meets that isn't a seal's blocker (a held joint's seam, a pinned press's: a Membrane's).</summary>
        static Vector3 Ground(Vector2 p,float from=30)
        {
            var hits=Physics.RaycastAll(new Vector3(p.x,from,p.y),Vector3.down,80,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<Membrane>()==null&&h.collider.GetComponentInParent<Health>()==null).OrderBy(h=>h.distance).ToArray();
            Assert.IsNotEmpty(hits,"fixture: there is ground at "+p);
            return hits[0].point;
        }
        static Vector3 Hero=>PartyController.Current.Active.transform.position;
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.Partner;
        static void Put(CombatActor actor,Vector2 p){var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=Ground(p)+Vector3.up*.05f;cc.enabled=true;Physics.SyncTransforms();}
        static bool Reach(Vector3 from,Vector2 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(from,out var start,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(Ground(to),out var end,1.2f,NavMesh.AllAreas)&&
                NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
        static IEnumerator Until(System.Func<bool> done,float seconds,string what){float deadline=Time.unscaledTime+seconds;while(!done()&&Time.unscaledTime<deadline)yield return null;Assert.IsTrue(done(),what);}
        static DiscoveryPoint Point(System.Func<DiscoveryPoint,bool> which,string what)
        {var found=Object.FindObjectsByType<DiscoveryPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(which).ToArray();Assert.AreEqual(1,found.Length,what+" is not in the Works exactly once");return found[0];}
        static DiscoveryPoint Hand(int n)=>Point(p=>p.flag=="home.joint"+n,"joint "+n+"'s hand");
        static SeparationJoint Joint(int n){var found=Object.FindObjectsByType<SeparationJoint>(FindObjectsSortMode.None).Where(j=>j.flag=="home.joint"+n).ToArray();Assert.AreEqual(1,found.Length,"joint "+n+" is not in the Works exactly once");return found[0];}
        static DrivenPress[] Presses(string line)=>Object.FindObjectsByType<DrivenPress>(FindObjectsSortMode.None).Where(p=>p.name.StartsWith("Press "+line+" ")).OrderBy(p=>p.name).ToArray();
        static float Flat(Vector3 v){v.y=0;return v.magnitude;}
        /// <summary>The cable's own colour: the least red of nine points along it, so the pulse running down a driven cable is never what is read.</summary>
        static Color CableColour(DrivenPress press)=>Enumerable.Range(1,9).Select(i=>press.cable.colorGradient.Evaluate(i/10f)).OrderBy(c=>c.r).First();

        // ------------------------------------------------------------------ the way through, and what stops it
        [UnityTest] public IEnumerator TheWorksAreWalkedThroughTheHullsACollarHoldingEachUntilItsJointIsOpened()
        {
            yield return Zone();var start=Hero;
            foreach(var (what,at) in new[]{("the tug's hold",new Vector2(0,14)),("joint 1's jack",Hands[1]),("joint 4's jack",Hands[4]),("the hatch's cradle",new Vector2(-3,-1.4f))})
                Assert.IsTrue(Reach(start,at),"from the hatch nobody can walk to "+what+" at "+at);
            // A held collar is the way's wall: the check has to see an open one as open, so with joint 1's seam open the hauler is reachable.
            var seam=Joint(1).blocks;seam.SetActive(false);yield return null;yield return null;Assert.IsTrue(Reach(start,new Vector2(0,48)),"fixture: the check can't see through an open collar");
            seam.SetActive(true);yield return null;yield return null;
            foreach(var (what,at) in new[]{("the hauler",new Vector2(0,48)),("the Compact module",new Vector2(20,20)),("the liner",new Vector2(0,110)),("the landing",new Vector2(0,165))})
                Assert.IsFalse(Reach(start,at),what+" can be walked to before its collar is opened");
            // Joint 1 by its hand: the hauler, the throat, the control room open; joint 2 stands under its press, and the liner beyond it.
            Hand(1).Interact();Assert.IsTrue(Flag("home.joint1"),"working joint 1's jack sets nothing");
            yield return Until(()=>!Joint(1).blocks.activeSelf,6,"joint 1 doesn't give");yield return new WaitForSecondsRealtime(.4f);
            foreach(var (what,at) in new[]{("the hauler",new Vector2(0,48)),("the throat's far side",new Vector2(0,75)),("the hauler's north end",new Vector2(-6,90)),("the control room",new Vector2(24,85)),("joint 5's jack",Hands[5])})
                Assert.IsTrue(Reach(start,at),"after joint 1 nobody can walk to "+what);
            foreach(var (what,at) in new[]{("joint 2's jack, under its press",Hands[2]),("the liner",new Vector2(0,110)),("the baggage bay",new Vector2(20,50))})
                Assert.IsFalse(Reach(start,at),what+" can be walked to after joint 1 alone");
            // The control room's rig cut: joint 2's press lifts; joint 2 opened: the liner; joint 3: the stair, the landing and the press hall.
            Set("clear.Works_ControlRig");yield return Until(()=>Presses("on joint 2").All(p=>p.AtRest),6,"joint 2's press never lifts when its rig is cut");yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(Reach(start,Hands[2]),"joint 2's jack can't be walked to once its press has lifted");
            Hand(2).Interact();yield return Until(()=>!Joint(2).blocks.activeSelf,6,"joint 2 doesn't give");yield return new WaitForSecondsRealtime(.4f);
            foreach(var (what,at) in new[]{("the liner",new Vector2(0,110)),("the press gallery's north end",new Vector2(0,138)),("joint 3's jack",Hands[3]),("joint 6's jack",Hands[6])})Assert.IsTrue(Reach(start,at),"after joint 2 nobody can walk to "+what);
            Assert.IsFalse(Reach(start,new Vector2(0,165)),"the landing can be walked to before joint 3");
            Hand(3).Interact();yield return Until(()=>!Joint(3).blocks.activeSelf,6,"joint 3 doesn't give");yield return new WaitForSecondsRealtime(.4f);
            foreach(var (what,at) in new[]{("the stair down",new Vector2(0,157)),("the landing",new Vector2(0,165)),("the press hall",new Vector2(0,180)),("the lift",LiftAt+new Vector2(-2.4f,-1))})Assert.IsTrue(Reach(start,at),"after joint 3 nobody can walk to "+what);
        }

        // ------------------------------------------------------------------ the joints
        [UnityTest] public IEnumerator OnlyTheThreeCriticalJointsCountAndEveryHandIsTheManifests()
        {
            yield return Zone();
            for(int n=1;n<=6;n++)
            {
                var hand=Hand(n);
                Assert.AreEqual("Take the joint's weight",hand.prompt,"joint "+n+"'s prompt isn't the manifest's");Assert.AreEqual("joint",hand.situation,"joint "+n+" says nothing as it opens");
                Assert.AreEqual(n<=3?"Joint":"",hand.report??"","joint "+n+(n<=3?" doesn't count toward the quest's three":" counts toward the quest's three, which are the critical path's"));
                Assert.AreEqual("",hand.dialogueNode??"","joint "+n+" plays a talk");Assert.AreEqual(Hands[n].x,hand.transform.position.x,.2f);Assert.AreEqual(Hands[n].y,hand.transform.position.z,.2f,"joint "+n+"'s hand isn't at its jack");
                Assert.AreSame(hand,Joint(n).point,"joint "+n+"'s hand isn't the joint's");
            }
            Assert.AreEqual("clear.Works_ControlRig",Hand(2).requiredFlag,"joint 2's hand isn't held by the press its rig drives");
            foreach(int n in new[]{1,3,4,5,6})Assert.IsEmpty(Hand(n).requiredFlag??"","joint "+n+" waits on something");
            // The pockets' fights wait for their joints, so nobody behind a held collar wakes.
            var fights=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None);
            Assert.AreEqual("home.joint4",fights.Single(f=>f.encounterId=="Works_Module").requiredFlag);Assert.AreEqual("home.joint6",fights.Single(f=>f.encounterId=="Works_Skiff").requiredFlag);
        }
        [UnityTest] public IEnumerator AJointTakesItsWeightThenGivesAndALoadFindsItOpenWithNothingPlayed()
        {
            yield return Zone();
            var joint=Joint(1);var dogs=joint.dogs.Select(d=>d.position).ToArray();var leaves=joint.leaves.Select(l=>l.position).ToArray();
            Assert.GreaterOrEqual(dogs.Length,6,"joint 1's collar has too few dogs to read as held");Assert.AreEqual(2,leaves.Length,"joint 1 hasn't two hull leaves");
            Assert.IsTrue(joint.shutLamps.activeSelf&&!joint.openLamps.activeSelf,"joint 1's lamp is lit before it opens");
            Put(Lead,Hands[1]);yield return new WaitForSecondsRealtime(.3f);
            Hand(1).Interact();
            yield return Until(()=>log.Any(l=>l=="JOINT_OPEN home.joint1"),1,"joint 1 doesn't say it is opening");
            // It takes the weight first: the dogs draw back while the leaves stay put.
            yield return new WaitForSecondsRealtime(joint.take*.9f);
            Assert.IsTrue(joint.dogs.Select((d,i)=>Vector3.Distance(d.position,dogs[i])).Count(m=>m>joint.dogTravel*.5f)>=dogs.Length-1,"the dogs don't draw back as the weight is taken");
            Assert.Less(joint.Opened,.05f,"the leaves part before the dogs are drawn");Assert.IsTrue(joint.blocks.activeSelf,"the way opens before the joint gives");
            // Then it gives: the leaves part into the walls, the lamp comes on, the way opens.
            yield return Until(()=>log.Any(l=>l=="JOINT_GIVEN home.joint1"),joint.give+2,"joint 1 never comes to rest open");
            Assert.AreEqual(1,joint.Opened,.01f);Assert.IsTrue(joint.leaves.Select((l,i)=>Vector3.Distance(l.position,leaves[i])).All(m=>m>2),"a leaf doesn't part");
            Assert.IsFalse(joint.blocks.activeSelf,"the seam still holds once the joint has given");Assert.IsTrue(joint.openLamps.activeSelf&&!joint.shutLamps.activeSelf,"joint 1's lamp doesn't come on");
            // The save: arriving with the flag finds it open with nothing played; the check can see a save that lost it.
            yield return Zone();
            var saved=GameServices.Current.Saves.Load("autosave");Assert.AreEqual(Works,saved.zone,"fixture: arriving autosaves");Assert.IsTrue(saved.flags["home.joint1"],"fixture: the save has joint 1");
            var stripped=GameServices.Current.Saves.Load("autosave");stripped.flags.Remove("home.joint1");GameServices.Current.Saves.Save("autosave",stripped);
            yield return Continue();yield return new WaitForSecondsRealtime(.3f);Assert.IsTrue(Joint(1).blocks.activeSelf,"fixture: the check can't see a save that lost its joint");
            var kept=GameServices.Current.Saves.Load("autosave");kept.flags["home.joint1"]=true;kept.zone=Works;kept.spawn="Arrival";GameServices.Current.Saves.Save("autosave",kept);
            log.Clear();yield return Continue();yield return new WaitForSecondsRealtime(.4f);
            joint=Joint(1);Assert.AreEqual(1,joint.Opened,.01f,"an opened joint loads shut");Assert.IsFalse(joint.blocks.activeSelf,"an opened joint loads holding the way");
            Assert.IsFalse(log.Any(l=>l.StartsWith("JOINT_OPEN")),"a loaded joint plays its opening again");
        }
        /// <summary>Title, then Continue from the autosave, as a player does.</summary>
        IEnumerator Continue()
        {
            SceneFlow.Current.LoadZone("Title");while(SceneFlow.Current.Loading)yield return null;yield return null;
            Object.FindFirstObjectByType<TitleScreen>().StartGame(true);
            float until=Time.realtimeSinceStartup+30;while(SceneFlow.Current.Loading&&Time.realtimeSinceStartup<until)yield return null;
            Assert.AreEqual(Works,SceneFlow.Current.Zone,"the save doesn't resume in the Works");Hold();yield return null;yield return null;
        }

        // ------------------------------------------------------------------ the rule: a rig's cable shows what it drives, and cutting the rig gives it back
        [UnityTest] public IEnumerator ARigsCablesShowWhatItDrivesAndCuttingTheRigGivesEveryPressOnThemBack()
        {
            Set("home.joint1");
            yield return Zone();
            var throat=Presses("throat");Assert.AreEqual(3,throat.Length,"the throat hasn't three presses across it");
            var post=new Vector3(ThroatRig.x,Ground(ThroatRig).y,ThroatRig.y);
            foreach(var press in throat)
            {
                Assert.AreEqual("clear.Works_Throat",press.rigFlag,press.name+" isn't driven by the throat's rig");Assert.IsFalse(press.Freed,press.name+" is home's before its rig is cut");
                Assert.IsTrue(press.drivenLamps.activeSelf&&!press.localLamps.activeSelf,press.name+" doesn't show the rig's cyan");
                Assert.Greater(press.cable.positionCount,8,press.name+" has no cable");
                Assert.Less(Flat(press.cable.GetPosition(press.cable.positionCount-1)-post),2,press.name+"'s cable doesn't end at the rig's post");
                var c=CableColour(press);Assert.IsTrue(c.b>.5f&&c.g>.4f&&c.r<.35f,press.name+"'s cable isn't the Compact's cyan ("+c+")");
            }
            // The throat's rig and nothing else drives these: the other machines keep their own rigs.
            Assert.IsTrue(Presses("gallery south").Concat(Presses("gallery north")).All(p=>p.rigFlag=="clear.Works_GalleryRig"),"the gallery's presses aren't on the gallery's rig");
            Assert.IsTrue(Presses("on joint 2").All(p=>p.rigFlag=="clear.Works_ControlRig"&&p.pins),"joint 2's press isn't pinned by the control room's rig");
            GameObject Named(string name)=>Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene==SceneManager.GetActiveScene()&&t.name==name).Select(t=>t.gameObject).Single();
            var lit=Named("Throat rig clamp lit");var dead=Named("Throat rig clamp dead");Assert.IsTrue(lit.activeInHierarchy&&!dead.activeInHierarchy,"the throat rig's clamp isn't lit while it holds");
            // The rig cut (its encounter cleared): every press on its cables lets go, climbs to rest and goes warm; the cables drop slack and dark; nothing else changes.
            var taut=throat.Select(p=>p.cable.GetPosition(p.cable.positionCount/2).y).ToArray();
            Set("clear.Works_Throat");
            yield return Until(()=>throat.All(p=>p.Freed),1,"the throat's presses aren't freed when their rig is cut");
            Assert.AreEqual(3,log.Count(l=>l.StartsWith("PRESS_FREED Press throat")),"a throat press doesn't say it is freed");
            yield return Until(()=>throat.All(p=>p.AtRest),4,"the throat's presses never come to rest");yield return new WaitForSecondsRealtime(1.4f);
            for(int i=0;i<throat.Length;i++)
            {
                var press=throat[i];Assert.IsTrue(press.localLamps.activeSelf&&!press.drivenLamps.activeSelf,press.name+"'s lamps don't go to the plant's warm light");
                Assert.Less(Vector3.Distance(press.ram.position,press.ramUp),.05f,press.name+"'s ram isn't at rest");
                var c=CableColour(press);Assert.IsTrue(c.maxColorComponent<.3f,press.name+"'s cable is still lit once its rig is cut ("+c+")");
                Assert.Less(press.cable.GetPosition(press.cable.positionCount/2).y,taut[i]-.3f,press.name+"'s cable doesn't drop slack");
            }
            Assert.IsTrue(!lit.activeInHierarchy&&dead.activeInHierarchy,"the throat rig's clamp stays lit once it is cut");
            Assert.IsFalse(Presses("gallery south").Concat(Presses("on joint 2")).Any(p=>p.Freed),"cutting the throat's rig freed another rig's machines");
        }
        [UnityTest] public IEnumerator ADrivenPressStrikesOnlyInsideItsMarkingAndNeverTakesTheLast()
        {
            Set("home.joint1");
            yield return Zone();
            var press=Presses("throat")[1];var inside=new Vector2(0,64.9f);var beside=new Vector2(0,69.6f);
            Assert.IsTrue(press.Contains(Ground(inside)),"fixture: the bay's middle is in the press's bay");Assert.IsFalse(press.Contains(Ground(beside)),"fixture: the point north of the bay is out of it");
            Put(Partner,beside);yield return new WaitForSecondsRealtime(1.5f);Assert.AreEqual(0,press.Slams,"a press dropped with nobody in its bay");
            // The lead steps in on his own brain with the pad untouched, as a player does: a held hero's motor isn't driven, and a throw is a dash the motor makes.
            float besideBefore=Partner.Health.integrity;Put(Lead,inside);Lead.GetComponent<PlayerBrain>().AutoPilot=false;float before=Lead.Health.integrity;
            yield return Until(()=>press.Marked,1.5f,"the press doesn't mark a bay a hero has stepped into");float marked=Time.time;
            yield return Until(()=>press.Slams>0,press.warn+1,"the marked press never drops");Assert.GreaterOrEqual(Time.time-marked,.5f,"the press drops under half a second after its marking");
            Assert.IsTrue(log.Any(x=>x.StartsWith("PRESS_STRUCK "+Lead.character)),"the press doesn't strike a hero under it");
            // Measured as every hit is (guide 0.1): through Health.Receive, in the parts a player has here. An ordinary hit's band, 9 to 13 percent.
            float took=(before-Lead.Health.integrity)/Lead.Health.maximum;Assert.That(took,Is.InRange(.09f,.13f),"the press doesn't take an ordinary hit's share of the bar ("+took.ToString("0.000")+")");
            yield return new WaitForSecondsRealtime(.5f);Assert.IsFalse(press.Contains(Lead.transform.position),"the press leaves its hero under the ram");
            Assert.AreEqual(besideBefore,Partner.Health.integrity,.01f,"the press struck a hero standing outside its bay");
            // Never the last of it.
            Lead.GetComponent<PlayerBrain>().AutoPilot=true;yield return new WaitForSecondsRealtime(press.rise+press.rest+.3f);
            Lead.Health.integrity=1;Put(Lead,inside);int slams=press.Slams;
            yield return Until(()=>press.Slams>slams,press.warn+2,"the press never drops again");yield return new WaitForSecondsRealtime(.6f);
            Assert.IsTrue(Lead.Health.Alive,"a press killed a hero");Assert.GreaterOrEqual(Lead.Health.integrity,1,"a press took the last of a hero's Integrity");
        }
        /// <summary>Iron doesn't know whose side it is on: a hunting press aimed at a hero falls on a Compact crew standing in the same bay too. The rule used in a fight.</summary>
        [UnityTest] public IEnumerator AHuntingPressFallsOnACompactCrewUnderItToo()
        {
            Set("home.joint1","home.joint2","clear.Works_Gallery","clear.Works_GalleryRig");
            // The gallery's rig is "cut" in this save only so its presses would be home's: the check has to see a freed press strike nobody, then a driven one strike the crew.
            yield return Zone();
            var press=Presses("gallery south")[1];Assert.IsTrue(press.Freed,"fixture: a cut rig's press is freed");
            var def=GameCatalog.Find<EnemyDef>("CompactWarden")??GameCatalog.Find<EnemyDef>("MusterPikeguard");Assert.IsNotNull(def,"fixture: a crew to stand under it");
            var bay=new Vector2(0,114.5f);
            EnemyBrain Crew(){var crew=ActorFactory.Enemy(def,Ground(bay+new Vector2(1.2f,0))+Vector3.up*.05f);crew.enabled=false;return crew;}
            var first=Crew();Put(Lead,bay+new Vector2(-1.2f,0));yield return new WaitForSecondsRealtime(2.4f);
            Assert.AreEqual(0,press.Slams,"a freed press dropped");Assert.AreEqual(first.Health.maximum,first.Health.integrity,.01f,"a freed press struck the crew under it");
            Object.Destroy(first.gameObject);
            // The same bay driven (a save before the rig is cut).
            GameServices.Current.Flags.SetBool("clear.Works_GalleryRig",false);yield return Zone();
            press=Presses("gallery south")[1];Assert.IsFalse(press.Freed,"fixture: the gallery's presses are driven");
            var crew=Crew();Put(Lead,bay+new Vector2(-1.2f,0));
            yield return Until(()=>press.Slams>0,press.warn+2,"the driven press never drops on the hero in its bay");yield return null;
            Assert.IsTrue(log.Any(l=>l.StartsWith("PRESS_CRUSHED")),"the press doesn't strike the crew under it");Assert.AreEqual(1,press.Crushed);
            Assert.Less(crew.Health.integrity,crew.Health.maximum*.9f,"the crew under the press lost under a tenth of its integrity ("+(1-crew.Health.integrity/crew.Health.maximum).ToString("0.00")+")");
        }
        /// <summary>The rule used to get somewhere: joint 2's jack has a press standing on it, its cable runs to the control room, and only that rig cut lets the joint be worked.</summary>
        [UnityTest] public IEnumerator JointTwosPressHoldsItUntilTheControlRoomsRigIsCut()
        {
            Set("home.joint1");
            yield return Zone();
            var press=Presses("on joint 2").Single();var hand=Hand(2);
            Assert.IsTrue(press.Pinning&&press.pinned.activeSelf,"joint 2's press doesn't stand on its jack");Assert.IsTrue(press.Contains(hand.transform.position),"joint 2's hand isn't under its press");
            Assert.Less(Vector3.Distance(press.ram.position,press.ramDown),.05f,"joint 2's press's ram isn't down on the jack");
            Assert.IsFalse(hand.Wanted,"joint 2's jack can be worked under its press");
            // Its cable runs to the control room: its rig's end is in the room, at the board.
            var end=press.cable.GetPosition(press.cable.positionCount-1);Assert.IsTrue(end.x>17.4f&&end.x<32&&end.z>74&&end.z<94,"joint 2's press's cable doesn't run to the control room ("+end+")");
            Assert.Less(Flat(end-new Vector3(ControlRig.x,0,ControlRig.y)),2.6f,"joint 2's press's cable doesn't end at the control room's rig");
            Put(Lead,new Vector2(-2,90));yield return new WaitForSecondsRealtime(1.5f);Assert.AreEqual(0,press.Slams,"a pinning press hunts");
            Set("clear.Works_ControlRig");
            yield return Until(()=>press.AtRest,4,"joint 2's press doesn't lift when the control room's rig is cut");
            Assert.IsFalse(press.pinned.activeSelf,"joint 2's jack is still blocked once its press has lifted");Assert.IsTrue(hand.Wanted,"joint 2's jack isn't offered once its press has lifted");
            Put(Lead,new Vector2(4.4f,93.6f));yield return new WaitForSecondsRealtime(.3f);
            hand.Interact();Assert.IsTrue(Flag("home.joint2"),"joint 2's jack sets nothing");yield return Until(()=>log.Any(l=>l=="JOINT_GIVEN home.joint2"),6,"joint 2 doesn't give");
        }

        // ------------------------------------------------------------------ the marking from the fixed camera
        /// <summary>The look review's fault, measured: a marking lost against the floor it lies on. A press's marking over its bay, from the game's own camera, as the lead waits at the
        /// bay's edge: the danger's orange at its rim, brighter than the dark anvil plate beside it by what a pour's must be. Frames in Builds/quality/focused/works-press for the eye.</summary>
        [UnityTest] public IEnumerator APressesMarkingReadsOverItsBayFromTheFixedCamera()
        {
            Assert.AreNotEqual(UnityEngine.Rendering.GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"fixture: the runner renders");
            Set("home.joint1","home.joint2","clear.Works_Gallery");
            yield return Zone();
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/focused/works-press"));Directory.CreateDirectory(folder);var weak=new List<string>();int read=0;
            // The partner steps into the bay (the press hunts any hero) while the lead waits a stride south of it, where a player stands to watch.
            foreach(var (name,line,index,wait) in new[]{("throat","throat",1,new Vector2(-2,59.6f)),("gallery","gallery south",0,new Vector2(-10,108.8f))})
            {
                var press=Presses(line)[index];var o=press.transform.position;var axis=press.along.normalized;var across=Vector3.Cross(Vector3.up,axis);
                Put(Lead,wait);Put(Partner,new Vector2(o.x,o.z)+new Vector2(axis.x,axis.z)*(press.length*.5f));yield return new WaitForSecondsRealtime(.2f);
                yield return Until(()=>press.Marked,2,"the "+name+" press never marks its bay");yield return new WaitForSecondsRealtime(press.warn*.6f);
                Vector3 OnGround(Vector3 p)=>Ground(new Vector2(p.x,p.z))+Vector3.up*.05f;
                var rim=o+across*.6f;var floor=o-axis*.35f+across*.6f;var margin=o+axis*.3f+across*.6f;
                var shot=Grab(folder,name,OnGround(rim),OnGround(floor),OnGround(margin));read++;
                if(shot.rimOrange<=0||shot.rimLuma-shot.floorLuma<=.12f)weak.Add($"{name}: rim {shot.rimLuma:0.00} (orange {shot.rimOrange:0.00}) against the plate's {shot.floorLuma:0.00}");
                yield return Until(()=>!press.Marked,3,"the "+name+" press's marking never ends");yield return new WaitForSecondsRealtime(press.rise+press.rest+.5f);
            }
            Assert.AreEqual(2,read,"fixture: both bays were photographed");
            Assert.IsEmpty(weak,"a press's marking doesn't read over its bay: "+string.Join("; ",weak));
        }
        struct Shot{public float rimOrange,rimLuma,floorLuma;}
        static Shot Grab(string folder,string name,Vector3 rim,Vector3 floor,Vector3 margin)
        {
            const int W=1280,H=720;var camera=Camera.main;
            var image=new Texture2D(W,H,TextureFormat.RGB24,false);var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
                Color At(Vector3 world,int spread,System.Func<Color,float> best)
                {
                    var s=camera.WorldToScreenPoint(world);int cx=Mathf.RoundToInt(s.x),cy=Mathf.RoundToInt(s.y);Color pick=Color.black;float score=float.NegativeInfinity;
                    for(int y=-spread;y<=spread;y++)for(int x=-spread;x<=spread;x++){int px=Mathf.Clamp(cx+x,0,W-1),py=Mathf.Clamp(cy+y,0,H-1);var c=image.GetPixel(px,py);float v=best(c);if(v>score){score=v;pick=c;}}
                    return pick;
                }
                static float Orange(Color c)=>Mathf.Min(c.r-c.g*1.15f,c.r-c.b*1.8f);
                var r=At(rim,3,Orange);var f=At(floor,1,c=>-c.grayscale);
                Debug.Log($"WORKS_PRESS_SHOT {name} rim=({r.r:0.00},{r.g:0.00},{r.b:0.00}) floor={f.grayscale:0.00}");
                return new Shot{rimOrange=Orange(r),rimLuma=r.grayscale,floorLuma=f.grayscale};
            }
            finally{camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
        }

        // ------------------------------------------------------------------ what the script and the wiring table expect of the world
        [UnityTest] public IEnumerator TheTalksTheTableTheCachesAndTheWaysAreTheScriptsAndTheWiringTables()
        {
            yield return Zone();
            var dorrow=Point(p=>p.dialogueNode=="DorrowWorks","DorrowWorks");
            Assert.AreEqual(("home.worksRead","",true),(dorrow.flag,dorrow.requiredFlag??"",dorrow.startsOnCrossing),"Dorrow's talk isn't a crossing that sets home.worksRead");
            Assert.That(dorrow.transform.position.z,Is.InRange(26f,33f),"Dorrow's talk isn't at the first joint");Assert.IsTrue(dorrow.TryGetComponent<BoxCollider>(out var mouth)&&mouth.isTrigger&&mouth.size.x>=20,"Dorrow's talk can be walked round");
            var table=Point(p=>p.dialogueNode=="SelaWeddingTable","SelaWeddingTable");
            Assert.AreEqual(("Look at the table","mira.lotTable","Consignment","SecondInventory",0,"Sela"),(table.prompt,table.flag,table.report,table.requiredQuest,table.requiredStep,table.dialogueSpeaker),"the wedding table isn't the wiring table's");
            Assert.IsTrue(GameObject.Find("The Arrens' wedding table")!=null,"the wedding table isn't built");
            var talk=Object.FindObjectsByType<AfterTalk>(FindObjectsSortMode.None).SingleOrDefault(t=>t.node=="OrrinSaintSpeaks");Assert.IsNotNull(talk,"nothing plays OrrinSaintSpeaks");
            Assert.AreEqual(("bossdown.FoundrySaint","home.saintReleased","Orrin"),(talk.flag,talk.heardFlag,talk.speaker),"Orrin's voice from the press isn't the contract's");
            Assert.Less(Flat(talk.transform.position-new Vector3(0,0,193)),1,"Orrin's voice isn't the press's speaker");
            foreach(var id in new[]{"home.works.module","home.works.baggage","home.works.skiff"})Assert.IsTrue(Object.FindObjectsByType<LootCache>(FindObjectsSortMode.None).Any(c=>c.cacheId==id),"the cache "+id+" is missing");
            var sights=Object.FindObjectsByType<FirstSight>(FindObjectsSortMode.None);
            Assert.IsTrue(sights.Any(s=>s.situation=="override"),"nobody says anything at the first rig");Assert.IsTrue(sights.Where(s=>s.situation=="weldarm").All(s=>s.onceFlag=="home.works.sawArm")&&sights.Any(s=>s.situation=="weldarm"),"the first Weld Arm isn't said once");
            var ways=Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Where(w=>w.scene=="DecksLeaving").ToArray();Assert.AreEqual(2,ways.Length,"the Works hasn't two ways up to the Decks");
            var stair=ways.Single(w=>w.spawn=="Works");Assert.IsEmpty(stair.requiredFlag??"","the hatch stair is locked");Assert.Less(Flat(stair.transform.position-Hero),12,"the hatch stair isn't at the arrival");
            var lift=ways.Single(w=>w.spawn=="Lift");Assert.AreEqual("bossdown.FoundrySaint",lift.requiredFlag,"the lift home isn't held until the Saint settles");
            StringAssert.Contains("Foundry Saint",lift.lockedPrompt,"the lift's lock doesn't say what opens it");Assert.Less(Flat(lift.transform.position-new Vector3(LiftAt.x,0,LiftAt.y)),3,"the lift isn't at the press hall's north-east corner");
            foreach(var way in ways)StringAssert.Contains("The Decks",way.prompt,"a way up doesn't say where it goes");
        }

        // ------------------------------------------------------------------ the press hall
        [UnityTest] public IEnumerator ThePressHallIsBuiltToItsContract()
        {
            Set("home.joint1","home.joint2","home.joint3","clear.Works_ControlRig");
            yield return Zone("PressHall");
            var duel=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="FoundrySaint");
            var post=duel.spawners.Single();Assert.AreEqual(P.x,post.transform.position.x,.1f);Assert.AreEqual(P.y,post.transform.position.z,.1f,"the Saint's spawner isn't at P");
            Assert.AreEqual(Barge,post.transform.position.y,.1f,"the Saint doesn't stand on the hall's floor");
            Assert.IsTrue(post.faceAsPlaced&&Vector3.Angle(post.transform.forward,Vector3.forward)<3&&post.radius==0,"the Saint doesn't face north, its back to the door");
            Assert.AreEqual(("home.joint3","PressHall",1),(duel.requiredFlag,duel.checkpointSpawn,duel.membranes.Length),"the door isn't the contract's");
            Assert.AreEqual(172,duel.membranes[0].transform.position.z,.4f,"the seal isn't across the door at z 172");Assert.IsTrue(duel.membranes[0].Open,"the seal is shut before the fight");
            // The trigger lies over the floor the fight is fought on: the hall's width, from its south wall to behind P, its middle on open floor in front of the press (outside the
            // Saint's capsule, 2.6 m round P); it begins once every hero is through the door (EncounterVolume.TryEnter).
            var box=duel.GetComponent<BoxCollider>();var trigger=box.bounds;Assert.That(box.size.x,Is.InRange(44f,48f));
            Assert.That(trigger.min.z,Is.InRange(173.5f,175.5f),"the Saint's trigger doesn't start at the hall's south wall");Assert.Greater(trigger.max.z,P.y+2.6f,"the Saint's trigger doesn't take in the press");
            Assert.Greater(Vector2.Distance(new Vector2(trigger.center.x,trigger.center.z),P),2.6f+1,"the Saint's trigger's middle is inside the Saint");
            Assert.Greater(trigger.min.z-duel.membranes[0].transform.position.z,1.5f,"the Saint's trigger starts at the seal: a hero in the doorway would be in it");
            Assert.AreEqual(168,duel.previewPoint.position.z,.4f,"the Saint isn't previewed from the landing");Assert.GreaterOrEqual(duel.previewRadius,Vector3.Distance(duel.previewPoint.position,post.transform.position)-.5f,"its preview doesn't take in P");
            // The six arms: an empty at each collar, two metres up, its forward along its bearing, numbered west to east.
            var arms=GameObject.Find("The Saint's arms");Assert.IsNotNull(arms,"the arms' collars aren't stood");Assert.AreEqual(6,arms.transform.childCount,"the Saint hasn't six arms");
            foreach(var (index,at,bearing) in Arms)
            {
                var collar=arms.transform.Find("Saint arm "+index);Assert.IsNotNull(collar,"arm "+index+" isn't stood");
                Assert.Less(Vector2.Distance(new Vector2(collar.position.x,collar.position.z),at),.05f,"arm "+index+"'s collar isn't the contract's");
                Assert.AreEqual(Barge+2,collar.position.y,.08f,"arm "+index+"'s collar isn't two metres over the floor");
                Assert.Less(Vector3.Angle(collar.forward,Quaternion.Euler(0,bearing,0)*Vector3.forward),1,"arm "+index+" doesn't lie along its bearing");
            }
            // The floor: level within r 14 on the south half and r 10 on the north, and nothing solid on it there but the two bolt stacks; each strip and 1.5 m either side open.
            float floor=Ground(P).y;
            foreach(var at in Cover){Assert.IsTrue(Physics.Raycast(new Vector3(at.x,floor+10,at.y),Vector3.down,out var hit,20,~0,QueryTriggerInteraction.Ignore));Assert.AreEqual(2.4f,hit.point.y-floor,.15f,"the stack at "+at+" isn't 2.4 m of cover ("+hit.collider.name+")");}
            for(float x=-14;x<=14;x+=1)for(float z=-14;z<=14;z+=1)
            {
                var p=P+new Vector2(x,z);float r=new Vector2(x,z).magnitude;if(r>(z<0?14:10)||Cover.Any(c=>Mathf.Abs(c.x-p.x)<2&&Mathf.Abs(c.y-p.y)<2))continue;
                Assert.IsTrue(Physics.Raycast(new Vector3(p.x,floor+16,p.y),Vector3.down,out var hit,30,~0,QueryTriggerInteraction.Ignore));
                if(hit.collider.GetComponentInParent<Health>()!=null)continue;
                Assert.AreEqual(floor,hit.point.y,.3f,"the press hall's floor isn't level and open at "+p+": "+hit.collider.name);
            }
            // Headroom over every raised arm (9 m clear within 8 m of each collar on its bearing).
            foreach(var (index,at,bearing) in Arms)for(float d=0;d<=8;d+=1)
            {
                var p=at+new Vector2(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad))*d;
                Assert.IsFalse(Physics.Raycast(new Vector3(p.x,floor+.3f,p.y),Vector3.up,out var over,9,~0,QueryTriggerInteraction.Ignore)&&over.collider.GetComponentInParent<Health>()==null,"something hangs over arm "+index+" at "+p+": "+over.collider?.name);
            }
            // The press as scenery is there only once it has settled.
            var settled=Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene==SceneManager.GetActiveScene()&&t.name=="The Foundry Saint, settled").Select(t=>t.gameObject).Single();
            Assert.IsFalse(settled.activeInHierarchy,"the settled press stands at P before the fight");
            Assert.IsNull(BossExit.For("FoundrySaint"),"the Saint has a way out: it settles where it stands");
            var spawn=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Single(s=>s.id=="PressHall");
            Assert.Less(spawn.transform.position.z,172-1,"the checkpoint is inside the seal");Assert.IsTrue(NavMesh.SamplePosition(spawn.transform.position,out _,1.5f,NavMesh.AllAreas),"the checkpoint isn't on the paths");
            var bay=Object.FindObjectsByType<RepairBay>(FindObjectsSortMode.None).OrderBy(r=>Vector3.Distance(r.transform.position,spawn.transform.position)).First();
            Assert.IsTrue(bay.free&&bay.saveSpawn=="PressHall"&&Vector3.Distance(bay.transform.position,spawn.transform.position)<20,"there is no free cradle at the press hall's door");
            Set("bossdown.FoundrySaint","clear.FoundrySaint");yield return Zone("PressHall");
            settled=Resources.FindObjectsOfTypeAll<Transform>().Where(t=>t.gameObject.scene==SceneManager.GetActiveScene()&&t.name=="The Foundry Saint, settled").Select(t=>t.gameObject).Single();
            Assert.IsTrue(settled.activeInHierarchy,"the press isn't standing at P once it has settled");
        }

        /// <summary>A boss's fight has to be able to begin (D272): the door's trigger takes every hero who comes through the door, and only once both are through it. With the partner
        /// still in the doorway nothing begins; both three metres into the hall, the Saint's fight has begun and the seal is shut behind them.</summary>
        [UnityTest] public IEnumerator TheSaintsFightBeginsOnceBothHeroesAreThroughTheDoor()
        {
            Set("home.joint1","home.joint2","home.joint3","clear.Works_ControlRig");
            yield return Zone("PressHall");
            var duel=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="FoundrySaint");
            Put(Lead,new Vector2(-1,177));Put(Partner,new Vector2(1.4f,172.4f));yield return new WaitForSecondsRealtime(.8f);
            Assert.IsFalse(duel.Started,"the Saint's fight began with the partner still in the doorway: the seal would shut on her");
            Put(Partner,new Vector2(1.4f,176));yield return Until(()=>duel.Started,2,"both heroes are three metres into the hall and the Saint's fight hasn't begun");
            Assert.IsFalse(duel.membranes[0].Open,"the press hall's seal is open with the fight under way");
        }

        // ------------------------------------------------------------------ the camera
        /// <summary>D272, measured on the camera's real line: it hangs 12.1 m over the hero and 15 m back (14.97 on the plan, 20.7 degrees off north), so its line to a hero's feet rises
        /// .81 a metre. Where a floor ends on the camera's side, the keel as it is drawn (the landform's floor and walls, and the hull plate stood on them) stays four tenths and more
        /// under that line from a metre and a half back, no higher than the deck's kerb before that, and nothing drawn comes within five metres under the lens. The Walking Canopy's
        /// near walls stood just over the line and hid a boss's whole aftermath; built by the same rule, the Works' did the same at the liner's and the press hall's south edges.</summary>
        [UnityTest] public IEnumerator WhatIsDrawnOnTheCamerasSideOfAFloorStaysUnderItsRealLineOfSight()
        {
            Set("home.joint1","home.joint2","home.joint3","home.joint4","home.joint5","home.joint6","clear.Works_ControlRig");
            yield return Zone();
            const int Floor=30,Probe=31;const float Up=12.1f,Rise=12.1f/14.97f,Margin=.3f,Kerb=.65f,LensClear=5;
            var back=new Vector3(-5.3f,0,-14).normalized;
            var probes=new List<GameObject>();
            GameObject Lay(Transform on,Mesh mesh,int layer){var probe=new GameObject("Sightline probe"){layer=layer};probe.transform.SetParent(on,false);probe.AddComponent<MeshCollider>().sharedMesh=mesh;probes.Add(probe);return probe;}
            try
            {
                var drawn=new List<GameObject>();GameObject floorProbe=null,bodies=null;
                foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                {
                    if(filter.sharedMesh==null)continue;
                    if(filter.name=="Works floors floor")floorProbe=Lay(filter.transform,filter.sharedMesh,Floor);
                    if(filter.name=="Works floors floor"||filter.name=="Works floors cliffs"||filter.name.StartsWith("Hull plate "))drawn.Add(Lay(filter.transform,filter.sharedMesh,Probe));
                }
                // The landform bodies meet, the first build's drawn keel (Works.Height): the measure has to see it stand over the line.
                var land=Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).FirstOrDefault(c=>c.name=="Works floors");
                Assert.IsNotNull(floorProbe,"fixture: the drawn floor");Assert.IsNotNull(land,"fixture: the landform bodies meet");Assert.GreaterOrEqual(drawn.Count,3,"fixture: the drawn keel and its plate");
                Physics.SyncTransforms();
                // The floors a party stands on: the drawn floor under the navigation (the bake also lays islands on the keel's flat tops, which nobody reaches).
                var standing=new List<Vector3>();
                for(float x=-30;x<=34;x+=1)for(float z=-12;z<=214;z+=1)
                {
                    if(!Physics.Raycast(new Vector3(x,30,z),Vector3.down,out var floor,60,1<<Floor,QueryTriggerInteraction.Collide))continue;
                    if(NavMesh.SamplePosition(floor.point,out var on,.3f,NavMesh.AllAreas)&&Mathf.Abs(on.position.y-floor.point.y)<.3f)standing.Add(on.position);
                }
                Assert.Greater(standing.Count,2000,"fixture: the Works' floors");
                List<string> Measure(System.Func<Vector3,float> top,out int edges,out int lenses)
                {
                    var faults=new List<string>();edges=0;lenses=0;
                    foreach(var feet in standing)
                    {
                        // Nothing within five metres under the lens of anyone standing here.
                        if((Mathf.RoundToInt(feet.x)+Mathf.RoundToInt(feet.z))%2==0){lenses++;var lens=feet+back*14.97f+Vector3.up*Up;float under=top(lens);if(lens.y-under<LensClear)faults.Add($"({feet.x:0},{feet.z:0}): drawn {lens.y-under:0.0} m under the lens");}
                        // The camera's side of this floor ends within a stride: the keel there.
                        if(!NavMesh.Raycast(feet,feet+back*2.5f,out var edge,NavMesh.AllAreas)||edge.distance>1.5f)continue;
                        edges++;
                        for(float d=.6f;d<=16;d+=.5f)
                        {
                            var at=feet+back*d;float h=top(at);if(float.IsNegativeInfinity(h))continue;
                            float most=d<1.5f?feet.y+Kerb:feet.y+Rise*d-Margin;
                            if(h>most){faults.Add($"({feet.x:0.0},{feet.z:0.0}) at {feet.y:0.0}: drawn {h-feet.y:0.00} m over the floor {d:0.0} m toward the camera, where the line to the feet is {Rise*d:0.00}");break;}
                        }
                    }
                    return faults;
                }
                float Drawn(Vector3 at)=>Physics.Raycast(new Vector3(at.x,at.y+40,at.z),Vector3.down,out var hit,80,1<<Probe,QueryTriggerInteraction.Collide)?hit.point.y:float.NegativeInfinity;
                float Body(Vector3 at)=>land.Raycast(new Ray(new Vector3(at.x,at.y+40,at.z),Vector3.down),out var hit,80)?hit.point.y:float.NegativeInfinity;
                var old=Measure(Body,out _,out _);
                Assert.Greater(old.Count,30,"fixture: the measure can't see the keel as the first build drew it (the landform bodies meet) stand over the camera's line");
                var faults=Measure(Drawn,out int edges,out int lenses);
                Assert.Greater(edges,150,"fixture: the floors' edges on the camera's side were read");Assert.Greater(lenses,1000,"fixture: the lens was read from the floors");
                Assert.IsEmpty(faults,faults.Count+" places the keel is drawn into the camera's line or its lens: "+string.Join("; ",faults.Take(16)));
            }
            finally{foreach(var p in probes)Object.Destroy(p);}
        }
        [UnityTest] public IEnumerator NoWallOrTallPieceStandsBetweenTheCameraAndAFight()
        {
            Set("home.joint1","home.joint2","home.joint3","home.joint4","home.joint6","clear.Works_ControlRig");
            yield return Zone();
            var toCamera=-(Quaternion.Euler(SightlineAudit.Pitch,SightlineAudit.Yaw,0)*Vector3.forward);var hidden=new List<string>();int samples=0;
            foreach(var fight in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
            {
                var box=fight.GetComponent<BoxCollider>().bounds;
                for(float x=box.min.x+1;x<box.max.x;x+=2)for(float z=box.min.z+1;z<box.max.z;z+=2)
                {
                    if(!Physics.Raycast(new Vector3(x,box.max.y+8,z),Vector3.down,out var floor,40,~0,QueryTriggerInteraction.Ignore)||!NavMesh.SamplePosition(floor.point,out var on,.4f,NavMesh.AllAreas))continue;
                    if(Mathf.Abs(on.position.y-(box.center.y-1.5f))>1.7f)continue;
                    samples++;var chest=on.position+Vector3.up*SightlineAudit.Chest;
                    foreach(var hit in Physics.RaycastAll(chest,toCamera,SightlineAudit.Distance-SightlineAudit.NearIgnore,~0,QueryTriggerInteraction.Ignore))
                    {
                        if(hit.collider.GetComponentInParent<Health>()!=null||hit.collider.GetComponentInParent<CharacterController>()!=null||hit.collider.GetComponentInParent<Membrane>()!=null)continue;
                        var size=hit.collider.bounds.size;if(hit.collider.bounds.max.y-on.position.y<3||size.x<1.6f&&size.z<1.6f)continue;
                        hidden.Add(fight.encounterId+" at ("+x.ToString("0")+", "+z.ToString("0")+") behind "+hit.collider.name);break;
                    }
                }
            }
            Assert.Greater(samples,400,"fixture: the fights' floors were sampled");
            Assert.IsEmpty(hidden,hidden.Count+" of "+samples+" fight floor points are hidden from the camera: "+string.Join("; ",hidden.Take(12)));
        }
    }
}
