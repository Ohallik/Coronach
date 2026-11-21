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
    /// <summary>Chapter 7's wild zone, the Skyfields (docs/maps/OrielSkyfields.md): the rule shown at the camp before it is said (the book falls to the wall), the lifts that carry the
    /// party up a taut fibre, the junctions that cross either way and keep or lose what rides on them, a junction's state kept in the save, the route walked island to island, the
    /// gardener and Ishen walking with the party, the long fibre home, the flags the talks and points set for the script, and no wall between the fixed camera and any fight. Flags are
    /// set directly where a conversation would set them: the script's nodes are tested by the dialogue tests.</summary>
    public sealed class SkyfieldsTests
    {
        const string Zone="OrielSkyfields";
        // The builder's numbers (OrielBuilder.Skyfields), copied: the tests can't see the editor assembly.
        const float Camp=0,Terrace=6,Upper=12,High=18;
        static readonly Vector2 Arrival=new(-36,13),Crate=new(-6.4f,17.6f),LiftLow=new(26,17.5f),LiftHigh=new(26,39.5f),LongFrom=new(-12,159.5f),LongTo=new(20,10),
            GardenerHome=new(-2,19.2f),GardenerWait=new(-10.6f,106.4f),GardenerStep=new(-2,166.6f),IshenPost=new(-9,15.6f),Door=new(4,169);
        const float WallFace=19.84f;
        /// <summary>The junctions: (number, gap's x, near rim, far rim, level).</summary>
        static readonly (int n,float x,float rimA,float rimB,float level)[] Junctions={(1,-30,51,63,Terrace),(2,-38,109,121,Upper),(3,20,137,149,Upper)};
        readonly List<string> log=new();
        void Heard(string text,string stack,LogType type){if(text.StartsWith("FIBRE_LIFT")||text.StartsWith("JUNCTION_")||text.StartsWith("FALLS_ALONG")||text.StartsWith("KEEPS_PACE")||text.StartsWith("DISCOVERY"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter7");log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what this zone writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Skyfields(ContentRegistry.Book book)
        {
            book.Exempt("FloorAndWallMeetInShade","OrielSkyfields","the islands' faces are not drawn (the landform's walls renderer is off): each island's keel is, so there is no edge where floor meets wall to see");
        }

        // ------------------------------------------------------------------ fixtures
        static void Set(params string[] keys){foreach(var key in keys)GameServices.Current.Flags.SetBool(key,true);}
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Load(string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(Zone,spawn);float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Zone+" never finished loading");Assert.AreEqual(Zone,SceneManager.GetActiveScene().name);
            Hold();yield return new WaitForSecondsRealtime(.6f);
        }
        /// <summary>The heroes stand where they are put and nobody wanders into a crossing.</summary>
        static void Hold(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}}
        static Vector3 Ground(Vector2 p)
        {
            var hits=Physics.RaycastAll(new Vector3(p.x,60,p.y),Vector3.down,140,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<CharacterController>()==null).OrderBy(h=>h.distance).ToArray();
            Assert.IsNotEmpty(hits,"fixture: there is ground at "+p);return hits[0].point;
        }
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=Lead);
        static void Put(CombatActor actor,Vector3 at){var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=at;cc.enabled=true;}
        static bool Reach(Vector3 from,Vector3 to)
        {
            var path=new NavMeshPath();
            return NavMesh.SamplePosition(from,out var start,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(to,out var end,1.5f,NavMesh.AllAreas)&&
                NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
        static Vector3 At(Vector2 p,float level)=>new(p.x,level,p.y);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static DiscoveryPoint Point(System.Func<DiscoveryPoint,bool> which,string what)
        {var found=All<DiscoveryPoint>().Where(which).ToArray();Assert.AreEqual(1,found.Length,what+" is not in the Skyfields exactly once");return found[0];}
        static FibreJunction Junction(int n)=>All<FibreJunction>().Single(j=>j.crossedFlag=="oriel.junction"+n);
        /// <summary>Walks the lead toward a point with its own motor for so long, as a stick held that way does.</summary>
        static IEnumerator Drive(Vector2 to,float seconds)
        {
            var lead=Lead;float until=Time.unscaledTime+seconds;
            while(Time.unscaledTime<until){var d=new Vector3(to.x,0,to.y)-lead.transform.position;d.y=0;if(d.magnitude<.4f)break;var way=d.normalized;lead.motor.Move(new Vector2(way.x,way.z),false,false);yield return null;}
            lead.motor.Move(Vector2.zero,false,false);
        }

        // ------------------------------------------------------------------ the rule, shown before it is said
        [UnityTest] public IEnumerator TheBookFallsSidewaysOntoTheWallAsIshenSpeaksAndAFallenBookStaysThere()
        {
            yield return Load();
            var book=All<FallsAlong>().Single();var landing=Point(p=>p.dialogueNode=="IshenLanding","Ishen's landing talk");
            Assert.AreSame(landing,book.startsWith,"the book doesn't fall with Ishen's talk");Assert.AreEqual("Landing",landing.report,"the landing talk isn't what the main quest's arrow finds");
            Assert.IsTrue(landing.startsOnCrossing,"the landing talk isn't a crossing");
            // At rest it lies on Ishen's top crate, flat.
            Assert.Greater(book.transform.position.y,Ground(Crate).y-.2f,"the book isn't on the crate");Assert.Greater(Vector3.Dot(book.transform.up,Vector3.up),.9f,"the book doesn't lie flat on the crate");
            // What the camp shows before a word: things lying on the wall's face, not at its foot.
            var taken=All<MeshRenderer>().Single(r=>r.name=="What the fibre took").bounds;
            Assert.Less(Mathf.Abs(taken.center.z-WallFace),.3f,"nothing lies on the old wall's face");Assert.Greater(taken.center.y,Ground(new Vector2(-6,19)).y+.4f,"what the fibre took lies at the wall's foot, not on its face");
            // The talk opens: the book slides off the crate and falls to the wall, and lies on the wall's face.
            landing.Interact();float until=Time.unscaledTime+12;while(!log.Any(l=>l.StartsWith("FALLS_ALONG"))&&Time.unscaledTime<until)yield return null;
            Assert.IsTrue(log.Any(l=>l.StartsWith("FALLS_ALONG")),"the book never falls when Ishen's talk opens");
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.Less(Mathf.Abs(book.transform.position.z-WallFace),.4f,"the book fell somewhere other than the wall: "+book.transform.position);
            Assert.Greater(book.transform.position.y,Ground(new Vector2(book.transform.position.x,WallFace-1)).y+.5f,"the book fell to the ground, not sideways to the wall");
            Assert.Greater(Mathf.Abs(Vector3.Dot(book.transform.up,Vector3.forward)),.9f,"the book doesn't lie on the wall's face");
        }
        [UnityTest] public IEnumerator ASaveWithTheRuleSeenHasTheBookOnTheWallWithNothingPlayed()
        {
            Set("oriel.ruleSeen");log.Clear();yield return Load();var book=All<FallsAlong>().Single();
            Assert.Less(Mathf.Abs(book.transform.position.z-WallFace),.4f,"a loaded camp has the book back on the crate");Assert.IsFalse(log.Any(l=>l.StartsWith("FALLS_ALONG")),"a loaded camp plays the fall again");
        }

        // ------------------------------------------------------------------ lifts
        [UnityTest] public IEnumerator ALiftNodeCarriesBothHeroesUpItsFibreAndSetsThemDownTogether()
        {
            yield return Load();
            var lift=All<FibreLift>().Single(l=>Vector2.Distance(new Vector2(l.transform.position.x,l.transform.position.z),LiftLow)<1);
            Assert.AreEqual("Ride the lift node",lift.prompt);Assert.IsTrue(lift.Available,"the first lift can't be ridden with nothing awake");
            // Its road is solid: the terrace above can be walked to from the landing.
            Assert.IsTrue(Reach(Ground(Arrival),Ground(LiftHigh)),"the first lift's road isn't a way up: the terrace can't be walked to");
            Put(Lead,Ground(LiftLow+new Vector2(-1.5f,-1.5f))+Vector3.up*.05f);Put(Partner,Ground(LiftLow+new Vector2(1.5f,-2))+Vector3.up*.05f);yield return null;
            lift.Interact();Assert.IsTrue(GameServices.Current.Input.Blocked,"riding a lift leaves the party's hands free");
            float until=Time.unscaledTime+8;while(!log.Any(l=>l.StartsWith("FIBRE_LIFT_DOWN"))&&Time.unscaledTime<until)yield return null;
            Assert.IsTrue(log.Any(l=>l.StartsWith("FIBRE_LIFT_DOWN")),"the lift never sets the party down");
            yield return null;
            var high=Ground(LiftHigh);
            Assert.Less(Vector3.Distance(Lead.transform.position,high),1.2f,"the lead isn't set down at the far node: "+Lead.transform.position);
            Assert.Less(Vector3.Distance(Partner.transform.position,high),3f,"the companion is left behind by a lift: "+Partner.transform.position);
            Assert.IsFalse(GameServices.Current.Input.Blocked,"the party's hands aren't given back after a lift");
            Assert.IsTrue(Lead.GetComponent<CharacterController>().enabled&&Partner.GetComponent<CharacterController>().enabled,"a hero's feet aren't given back after a lift");
            // Standing on the terrace, they stay there: the floor holds them.
            yield return new WaitForSecondsRealtime(.6f);Assert.AreEqual(Terrace,Lead.transform.position.y,.4f,"the lead doesn't stand on the terrace after the ride");
        }

        [UnityTest] public IEnumerator TheLongFibreTakesThePartyHomeAndTheCallWaitsThereOnceTheRecordIsRead()
        {
            Set("oriel.ruleSeen","oriel.junction1","oriel.junction1.seat","oriel.junction2","oriel.junction2.seat","oriel.junction3","oriel.junction3.seat","oriel.doorReached","oriel.coronachHeard");
            yield return Load("ChoirDoor");
            var lift=All<FibreLift>().Single(l=>Vector2.Distance(new Vector2(l.transform.position.x,l.transform.position.z),LongFrom)<1.5f);
            Put(Lead,Ground(LongFrom+new Vector2(1.5f,-1.5f))+Vector3.up*.05f);yield return null;
            var call=Point(p=>p.dialogueNode=="MeretOrielCall","Meret's call");Assert.AreEqual(("oriel.coronachHeard","CampReturn"),(call.requiredFlag,call.report),"the call waits on the wrong flag or reports the wrong thing");
            bool asked=false;void Asked(DiscoveryPoint p){if(p==call)asked=true;}DiscoveryPoint.DialogueRequested+=Asked;
            try
            {
                lift.Interact();float until=Time.unscaledTime+14;while(!log.Any(l=>l.StartsWith("FIBRE_LIFT_DOWN"))&&Time.unscaledTime<until)yield return null;
                Assert.IsTrue(log.Any(l=>l.StartsWith("FIBRE_LIFT_DOWN")),"the long fibre never sets the party down");
                Assert.Less(Vector2.Distance(new Vector2(Lead.transform.position.x,Lead.transform.position.z),LongTo),1.5f,"the long fibre doesn't bring the party to the camp");
                Assert.AreEqual(Camp,Lead.transform.position.y,.4f,"the long fibre sets the party down off the camp's floor");
                until=Time.unscaledTime+3;while(!asked&&Time.unscaledTime<until)yield return null;
                Assert.IsTrue(asked,"coming home on the long fibre once the record is read doesn't bring Meret's call");
            }
            finally{DiscoveryPoint.DialogueRequested-=Asked;}
        }

        // ------------------------------------------------------------------ junctions
        [UnityTest] public IEnumerator AJunctionSeatedOrCutCrossesTheSameGapAndKeepsOrLosesWhatRidesOnIt()
        {
            Set("oriel.ruleSeen");yield return Load();
            foreach(var (n,x,rimA,rimB,level) in Junctions)
            {
                var j=Junction(n);Assert.AreEqual(FibreJunction.State.Sagging,j.Now,"junction "+n+" doesn't load sagging");
                Assert.IsFalse(j.deck.activeSelf,"junction "+n+"'s deck is across before anyone has worked it");
                Assert.IsFalse(Reach(Ground(new Vector2(x,rimA-2)),Ground(new Vector2(x,rimB+2))),"junction "+n+" can be walked across while its island sags");
                Assert.AreEqual(2,j.hands.Count(h=>h.activeInHierarchy),"junction "+n+" doesn't offer both a seat and a cut");
            }
            // The check has to be able to see a junction that doesn't answer: with its component off, a seat flag leaves the gap open.
            var third=Junction(3);third.enabled=false;Set("oriel.junction3.seat");yield return new WaitForSecondsRealtime(4.5f);
            Assert.IsFalse(Reach(Ground(new Vector2(20,135)),Ground(new Vector2(20,151))),"fixture: the check can't see a junction that stays sagging");
            GameServices.Current.Flags.SetBool("oriel.junction3.seat",false);third.enabled=true;yield return null;
            // Seated: the island comes up flush, the chest on it with it, and the gap is walked.
            var chest=All<LootCache>().Single(c=>c.cacheId=="oriel.sky.junction");
            var seat=third.hands.Select(h=>h.GetComponent<DiscoveryPoint>()).Single(p=>p.prompt=="Seat the junction");seat.Interact();
            Assert.IsTrue(Flag("oriel.junction3.seat"),"seating the third junction sets nothing");
            float until=Time.unscaledTime+8;while(third.Now!=FibreJunction.State.Seated&&Time.unscaledTime<until)yield return null;yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(FibreJunction.State.Seated,third.Now,"the third junction never seats");Assert.IsTrue(Flag("oriel.junction3"),"a seated junction doesn't say it is crossed");
            Assert.IsTrue(log.Any(l=>l.StartsWith("JUNCTION_SEATED oriel.junction3.seat")));
            Assert.IsTrue(Reach(Ground(new Vector2(20,135)),Ground(new Vector2(20,151))),"a seated junction can't be walked across");
            Assert.IsTrue(chest.gameObject.activeInHierarchy,"seating the junction lost the chest on it");Assert.AreEqual(Upper,chest.transform.position.y,.3f,"the chest didn't come up with the island");
            Assert.IsFalse(third.hands.Any(h=>h.activeInHierarchy),"a seated junction still offers to be cut");
            // Walked across: a body crosses the seated island on its own feet.
            Put(Lead,Ground(new Vector2(18.5f,134))+Vector3.up*.05f);yield return null;yield return Drive(new Vector2(18.5f,152),6);
            Assert.Greater(Lead.transform.position.z,149.5f,"a body can't walk across a seated junction: it stopped at "+Lead.transform.position);
            // Cut: the island drops with its garden, the freed fibre is a road across the same gap, and the garden's keepers are gone (they have turned).
            var second=Junction(2);var keepers=All<FlagGate>().Single(g=>g.flag=="oriel.junction2.cut");
            Assert.IsTrue(keepers.targets.All(t=>t.activeInHierarchy),"the second junction's garden has no keepers before it is cut");
            var cut=second.hands.Select(h=>h.GetComponent<DiscoveryPoint>()).Single(p=>p.prompt=="Cut the junction");cut.Interact();
            until=Time.unscaledTime+8;while(second.Now!=FibreJunction.State.Cut&&Time.unscaledTime<until)yield return null;yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(FibreJunction.State.Cut,second.Now,"the second junction never cuts");
            Assert.IsFalse(second.island.gameObject.activeInHierarchy,"a cut junction's island is still there");Assert.IsTrue(second.road.activeInHierarchy,"a cut junction leaves no road across");
            Assert.IsTrue(Reach(Ground(new Vector2(-38,107)),Ground(new Vector2(-38,123))),"a cut junction can't be walked across");
            Assert.IsTrue(keepers.targets.All(t=>!t.activeInHierarchy),"the garden's keepers still tend a garden that is gone");
            var pruners=All<EncounterVolume>().Single(e=>e.encounterId=="Sky_Pruners");Assert.AreEqual("oriel.junction2.cut",pruners.requiredFlag,"cutting the garden's junction doesn't turn its keepers");
            // The first, the lesson, carries nothing: either crosses.
            Assert.AreEqual(0,Junction(1).lost.Length,"the first junction, the lesson, carries something to lose");
        }

        [UnityTest] public IEnumerator ARailStopsAWalkerAtASaggingJunctionAndAJunctionsStateLoadsWithNothingPlayed()
        {
            Set("oriel.ruleSeen");yield return Load();
            Put(Lead,Ground(new Vector2(-30,48))+Vector3.up*.05f);yield return null;yield return Drive(new Vector2(-30,66),3);
            Assert.Less(Lead.transform.position.z,51.5f,"nothing stops a walker going off the rim where the first junction's island sags: "+Lead.transform.position);
            Assert.Greater(Lead.transform.position.y,Terrace-.5f,"a walker fell where the first junction sags");
            // A save that cut the first and seated the second loads them so, with nothing played.
            // The zone loaded now answers the flags as they are set (it is the live world): let it, then load it again and listen only to the new one.
            Set("oriel.junction1.cut","oriel.junction1","oriel.junction2.seat","oriel.junction2");yield return new WaitForSecondsRealtime(.5f);log.Clear();yield return Load();
            Assert.AreEqual(FibreJunction.State.Cut,Junction(1).Now);Assert.AreEqual(FibreJunction.State.Seated,Junction(2).Now);Assert.AreEqual(FibreJunction.State.Sagging,Junction(3).Now);
            Assert.IsFalse(Junction(1).island.gameObject.activeInHierarchy,"a cut junction's island loads back in its place");
            Assert.IsFalse(log.Any(l=>l.StartsWith("JUNCTION_")),"a loaded junction plays its seat or its cut again: "+string.Join(", ",log));
            Assert.IsTrue(Reach(Ground(new Vector2(-30,49)),Ground(new Vector2(-30,65))),"a loaded cut junction can't be walked across");
        }

        // ------------------------------------------------------------------ the route
        [UnityTest] public IEnumerator TheRouteIsWalkedIslandToIslandOnceTheJunctionsAreWorkedAndNotBefore()
        {
            yield return Load();var start=Ground(Arrival);
            foreach(var (what,at) in new[]{("the cradle",new Vector2(-23.4f,9.6f)),("the bench",new Vector2(-20.4f,16.4f)),("Lume",new Vector2(9,14)),("the first lift's top",LiftHigh),("the walled garden",new Vector2(-14,43.8f)),
                ("the glass forest",new Vector2(-48,43)),("the first junction's near rim",new Vector2(-30,49))})Assert.IsTrue(Reach(start,Ground(at)),"from the landing nobody can walk to "+what);
            foreach(var (what,at) in new[]{("the fibre fields",new Vector2(0,71)),("the sagging gardens",new Vector2(6,101)),("the forecourt",new Vector2(4,155))})
                Assert.IsFalse(Reach(start,Ground(at)),what+" can be walked to before the first junction is worked");
            Set("oriel.junction1.seat");yield return new WaitForSecondsRealtime(5);
            foreach(var (what,at) in new[]{("the fibre fields",new Vector2(0,71)),("the swept stair",new Vector2(18,73.4f)),("the sagging gardens",new Vector2(6,101)),("the second junction's near rim",new Vector2(-38,107))})
                Assert.IsTrue(Reach(start,Ground(at)),"once the first junction is seated nobody can walk to "+what);
            Assert.IsFalse(Reach(start,Ground(new Vector2(-30,129))),"the stair gardens can be walked to before the second junction is worked");
            Set("oriel.junction2.cut");yield return new WaitForSecondsRealtime(4);Set("oriel.junction3.seat");yield return new WaitForSecondsRealtime(5);
            foreach(var (what,at) in new[]{("the stair gardens",new Vector2(-30,129)),("the sealed garden's door",new Vector2(-43,128)),("the highest island",new Vector2(-58,155)),("the forecourt",new Vector2(4,155)),
                ("the Choir's door",new Vector2(4,167.4f)),("the long fibre's node",LongFrom)})Assert.IsTrue(Reach(start,Ground(at)),"once the junctions are worked nobody can walk to "+what);
        }

        // ------------------------------------------------------------------ the two who walk with the party
        [UnityTest] public IEnumerator TheGardenerKeepsTwelvePacesBehindGoesAheadToTheFrayedGardenAndSitsAtTheDoor()
        {
            yield return Load();
            var gardener=All<KeepsPace>().Single(k=>k.GetComponent<Npc>()==null);
            Assert.Less(Vector2.Distance(new Vector2(gardener.transform.position.x,gardener.transform.position.z),GardenerHome),1,"the gardener isn't by the camp's wall before the rule is said");
            Set("oriel.ruleSeen");yield return new WaitForSecondsRealtime(.3f);
            // The party walks on: it follows, and when they stop it stops twelve paces back.
            yield return Drive(new Vector2(20,14),10);yield return new WaitForSecondsRealtime(4);
            Assert.That(gardener.PathToLead,Is.InRange(gardener.gap-1.5f,gardener.gap+2.5f),"the gardener doesn't keep twelve paces behind once the party stops");
            var at=gardener.transform.position;yield return new WaitForSecondsRealtime(1.5f);
            Assert.Less(Vector3.Distance(at,gardener.transform.position),.2f,"the gardener doesn't stop when the party stops");
            // Seen following, it goes ahead to the first frayed garden, its light low, and waits.
            Set("oriel.junction1","oriel.junction1.seat","oriel.followerSeen");yield return Load("Arrival");
            gardener=All<KeepsPace>().Single(k=>k.GetComponent<Npc>()==null);
            Assert.Less(Vector2.Distance(new Vector2(gardener.transform.position.x,gardener.transform.position.z),GardenerWait),1.2f,"the gardener isn't waiting at the first frayed garden once seen following");
            Assert.Less(gardener.lampLight.intensity,1,"the gardener's light isn't low while it waits to be asked");
            // At the Choir's door it stops, and stays.
            Set("oriel.gardenerAsked","oriel.junction2","oriel.junction2.seat","oriel.junction3","oriel.junction3.seat","oriel.doorReached");yield return Load("ChoirDoor");
            gardener=All<KeepsPace>().Single(k=>k.GetComponent<Npc>()==null);
            Assert.Less(Vector2.Distance(new Vector2(gardener.transform.position.x,gardener.transform.position.z),GardenerStep),1.2f,"the gardener isn't on the Choir's step once the door is reached");
            // Unless the three gardens are seated and it hasn't yet led the party to the sealed garden: then it leads.
            Assert.AreEqual(4,gardener.Current,"fixture: the gardener is at the door's stage");
            Set("oriel.garden1","oriel.garden2","oriel.garden3","oriel.gardensSeated");yield return null;yield return null;
            Assert.AreEqual(5,gardener.Current,"three seated gardens don't send the gardener to lead the party to the sealed garden");
            Assert.IsTrue(gardener.stages[5].leads,"the gardener follows to the sealed garden instead of leading");
        }

        [UnityTest] public IEnumerator IshenWalksWithThePartyFromTheLandingAndHoldsBackFromAFight()
        {
            yield return Load();
            var ishen=All<Npc>().Single(n=>n.speaker=="Ishen");var keeps=ishen.GetComponent<KeepsPace>();
            Assert.AreEqual(("IshenCamp","IshenCamp","IshenOrielPost","oriel.coronachHeard"),(ishen.firstNode,ishen.repeatNode,ishen.postNode,ishen.postFlag),"Ishen at the camp plays the wrong lines");
            Assert.Less(Vector2.Distance(new Vector2(ishen.transform.position.x,ishen.transform.position.z),IshenPost),1,"Ishen isn't at his crates before the rule is said");
            Set("oriel.ruleSeen");yield return Drive(new Vector2(20,14),6);yield return new WaitForSecondsRealtime(3);
            Assert.Less(keeps.PathToLead,keeps.gap+2.5f,"Ishen doesn't walk with the party once the rule is said");
            // Out on the terrace, out of the camp's pocket, something awake beside the party: he holds where he is.
            Put(Lead,Ground(new Vector2(10,43))+Vector3.up*.05f);yield return new WaitForSecondsRealtime(3);
            Assert.Less(keeps.PathToLead,keeps.gap+2.5f,"Ishen isn't with the party on the terrace");
            var foe=ActorFactory.Enemy(GameCatalog.Find<Lattice.Data.EnemyDef>("Thornback"),Lead.transform.position+Vector3.right*4);yield return null;yield return null;
            Assert.IsTrue(EnemyBrain.AwakeNear(Lead.transform.position,8),"fixture: the foe beside the party is awake");
            var at=ishen.transform.position;yield return Drive(new Vector2(-12,43),2);yield return new WaitForSecondsRealtime(.5f);
            Assert.IsTrue(keeps.Holding,"Ishen walks on into a fight");Assert.Less(Vector3.Distance(at,ishen.transform.position),1,"Ishen walks on into a fight");
            Object.Destroy(foe.gameObject);
            // Once he has read the record he is at his crates again.
            Set("oriel.coronachHeard");yield return Load();
            ishen=All<Npc>().Single(n=>n.speaker=="Ishen");
            Assert.Less(Vector2.Distance(new Vector2(ishen.transform.position.x,ishen.transform.position.z),IshenPost),1,"Ishen isn't back at his crates once he has read the record");
        }

        // ------------------------------------------------------------------ what the script expects of the world
        [UnityTest] public IEnumerator TheTalksAndPointsSetTheFlagsTheScriptExpects()
        {
            yield return Load();
            // (node, flag, needs, report, a crossing): the manifest's section 2 and the main quest's steps.
            foreach(var (node,flag,needs,report,crossing) in new[]{("IshenLanding","oriel.ruleSeen","","Landing",true),("IdraEmptyGarden","oriel.gardenSeen","","",true),("IshenJunction","oriel.junctionRead","","",true),
                ("IdraGardenerFollows","oriel.followerSeen","","",true),("IdraChoirDoor","oriel.doorReached","","",true),("MeretOrielCall","oriel.meretHeard","oriel.coronachHeard","CampReturn",true),
                ("IdraGlassForest","oriel.forestHeard","","",false),("IdraSweptStair","oriel.stairSeen","oriel.gardenSeen","",false),("IdraGardenerWaits","oriel.gardenerAsked","oriel.followerSeen","",false)})
            {
                var point=Point(p=>p.dialogueNode==node,node);
                Assert.AreEqual(flag,point.flag,node+" sets the wrong flag");Assert.AreEqual(needs,point.requiredFlag??"",node+" waits on the wrong flag");Assert.AreEqual(report,point.report??"",node+" reports the wrong thing");
                Assert.AreEqual(crossing,point.startsOnCrossing,node+(crossing?" isn't a crossing":" starts by walking past it"));
            }
            foreach(var (node,quest,step,report) in new[]{("IdraSealedGarden","EmptyGardens",1,"SealedGarden"),("IdraFarLantern","LumesLantern",0,"LanternSeat")})
            {var point=Point(p=>p.dialogueNode==node,node);Assert.AreEqual((quest,step,report),(point.requiredQuest,point.requiredStep,point.report),node+" is offered at the wrong step or reports the wrong thing");}
            var gardens=All<DiscoveryPoint>().Where(p=>p.report=="GardenJunction").ToArray();Assert.AreEqual(3,gardens.Length,"the Empty Gardens doesn't have three garden junctions");
            Assert.IsTrue(gardens.All(p=>p.requiredQuest=="EmptyGardens"&&p.requiredStep==0&&p.prompt=="Seat the junction"&&p.situation=="mend"),"a garden junction is offered outside the quest's first step, or can be cut");
            Assert.AreEqual(3,gardens.Select(p=>p.flag).Distinct().Count(),"two garden junctions share a flag");
            var seated=All<FlagAll>().Single(f=>f.result=="oriel.gardensSeated");CollectionAssert.AreEquivalent(gardens.Select(p=>p.flag),seated.flags,"the sealed garden doesn't wait on the three gardens");
            // People and ways.
            var lume=All<Npc>().Single(n=>n.speaker=="Lume");Assert.AreEqual(("LumeFirst","LumeRepeat","LumePost","oriel.coronachHeard"),(lume.firstNode,lume.repeatNode,lume.postNode,lume.postFlag),"Lume plays the wrong lines");
            var launch=All<DockingPad>().Single();Assert.AreEqual(("Orrery","OrielSkyfields"),(launch.scene,launch.spawn),"the launch doesn't go back up to the Orrery's inner end");
            var door=All<WarpBeacon>().Single(w=>w.scene=="GlassChoir");Assert.AreEqual(("Arrival",""),(door.spawn,door.requiredFlag),"the Choir's door isn't open onto its arrival");
            foreach(var id in new[]{"Arrival","Camp","ChoirDoor"})Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id==id),"the Skyfields has no spawn "+id);
            foreach(var id in new[]{"oriel.sky.grove","oriel.sky.junction","oriel.sky.high"})Assert.IsTrue(All<LootCache>().Any(c=>c.cacheId==id),"the cache "+id+" is missing");
            Assert.AreEqual("oriel.landed",All<FlagAll>().Single(f=>f.flags.Contains("visited."+Zone)).result,"arriving doesn't say the party has landed");
            // Every junction's two hands set its two flags, and say so.
            foreach(var (n,_,_,_,_) in Junctions)
            {
                var j=Junction(n);var hands=j.hands.Select(h=>h.GetComponent<DiscoveryPoint>()).ToArray();
                CollectionAssert.AreEquivalent(new[]{"oriel.junction"+n+".seat","oriel.junction"+n+".cut"},hands.Select(h=>h.flag),"junction "+n+"'s hands set the wrong flags");
                CollectionAssert.AreEquivalent(new[]{"junction.seat",n==2?"garden":"junction.cut"},hands.Select(h=>h.situation),"junction "+n+"'s hands don't bark, or the garden's cut doesn't say the garden is gone");
            }
        }

        // ------------------------------------------------------------------ the camera
        [UnityTest] public IEnumerator NoWallOrTallPieceStandsBetweenTheCameraAndAFight()
        {
            Set("oriel.junction1","oriel.junction1.seat","oriel.junction2","oriel.junction2.cut","oriel.junction3","oriel.junction3.seat");
            yield return Load();yield return new WaitForSecondsRealtime(.5f);
            var toCamera=-(Quaternion.Euler(SightlineAudit.Pitch,SightlineAudit.Yaw,0)*Vector3.forward);var hidden=new List<string>();int samples=0;
            foreach(var fight in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
            {
                var box=fight.GetComponent<BoxCollider>().bounds;
                for(float x=box.min.x+1;x<box.max.x;x+=2)for(float z=box.min.z+1;z<box.max.z;z+=2)
                {
                    if(!Physics.Raycast(new Vector3(x,box.max.y+8,z),Vector3.down,out var floor,40,~0,QueryTriggerInteraction.Ignore)||!NavMesh.SamplePosition(floor.point,out var on,.4f,NavMesh.AllAreas))continue;
                    if(Mathf.Abs(on.position.y-(box.center.y-1.5f))>1.6f)continue;
                    samples++;var chest=on.position+Vector3.up*SightlineAudit.Chest;
                    foreach(var hit in Physics.RaycastAll(chest,toCamera,SightlineAudit.Distance-SightlineAudit.NearIgnore,~0,QueryTriggerInteraction.Ignore))
                    {
                        if(hit.collider.GetComponentInParent<Health>()!=null||hit.collider.GetComponentInParent<CharacterController>()!=null)continue;
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
