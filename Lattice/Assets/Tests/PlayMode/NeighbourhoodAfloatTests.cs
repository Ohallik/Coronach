using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Neighbourhood Afloat, the epilogue town (LastMooringBuilder.Neighbourhood; docs/maps/NeighbourhoodAfloat.md): the cut from the passage arriving on the moorings, everyone the
    /// epilogue names standing where the script has them and reached from the arrival, each saying their own line first, again and after the last scene, the nine counted visits with their
    /// extras in the same box, Orrin's offer, the handrail, the line and the last scene in that order with nothing said after it, the board, the terminal and the moorings, the services the
    /// chart lists, and each district standing as the place it came from. What every zone gets from the shared checks is theirs. Coordinates are the builder's own constants.</summary>
    public sealed class NeighbourhoodAfloatTests
    {
        const string Zone="NeighbourhoodAfloat";
        /// <summary>The builder's own plan (LastMooringBuilder.Neighbourhood, an editor class the tests can't see), as docs/maps/NeighbourhoodAfloat.md publishes it.</summary>
        static class Plan
        {
            internal static readonly Vector2 Arrival=new(0,-4),RepaintAt=new(-14.3f,23.4f);
            internal const float GangwayHalf=1.5f;
            internal static readonly (string name,Vector2 a,Vector2 b)[] Gangways=
            {
                ("Gangway to the market",new(0,8),new(0,14)),("Gangway to the arcade",new(18,8),new(18,14)),("Gangway from the market to the square",new(-6,42),new(-6,50)),
                ("Gangway from the arcade to the square",new(14,42),new(14,50)),("Gangway to the low street",new(-18,62),new(-24,62)),("Gangway to the east terrace",new(18,66),new(24,66)),
                ("Gangway to the lantern gallery",new(-10,76),new(-10,84)),("Gangway from the low street to the gallery",new(-40,78),new(-40,84)),("Gangway to the sky-garden",new(12,76),new(12,82)),
                ("Gangway from the terrace to the sky-garden",new(40,74),new(40,82)),
            };
            internal static readonly (string id,string node,Vector2 at,float yaw,bool visit,(string node,string quest)[] extras)[] People=
            {
                ("Keeper","AbriHomecoming",new(35.8f,67.4f),180,true,new[]{("AbriHomeGlass","RelayGlass")}),("Teo","TeoHomecoming",new(40.6f,25.2f),250,true,new[]{("TeoHomeTags","ClaimTags")}),
                ("Maru","MaruHomecoming",new(-39.4f,64),130,true,new[]{("MaruHomeHarbourLamp","BeaconKeeper")}),("Hask","HaskHomecoming",new(-1.6f,63.4f),200,true,new[]{("HaskHomeStrays","StrayBeasts")}),
                ("Pell","PellHomecoming",new(-26,91.2f),170,true,new[]{("PellHomeMothersLamp","PellsLamp"),("PellHomeOriel","LumesLantern")}),
                ("Tovra","TovraHomecoming",new(-16.8f,33.2f),230,true,new[]{("TovraHomePikes","QuietHands")}),("Oda","OdaHomecoming",new(48.4f,58),250,true,new[]{("OdaHomeStamp","OdaBerth")}),
                ("Dorrow","DorrowHomecoming",new(24,91),200,true,new (string,string)[0]),
                ("Mira","MiraHomecoming",new(-18.8f,18.4f),110,true,new[]{("MiraHomeCrystals","MiraCrystals"),("MiraHomeTable","SecondInventory")}),
                ("Ruva","RuvaPassageRules",new(-9.4f,57),120,false,new[]{("RuvaHomePost","MooringPost"),("RuvaHomeShoot","EmptyGardens")}),
                ("Meret","MeretAccount",new(15.2f,58.4f),225,false,new (string,string)[0]),("Drask","DraskWatches",new(5,74.5f),180,false,new (string,string)[0]),
                ("Orrin","OrrinDirectorship",new(-12.4f,70.4f),170,false,new (string,string)[0]),
                ("Jask","JaskNewSky",new(-36,92),160,false,new[]{("JaskHomeThree","Questions"),("JaskHomeFare","StowawayFare")}),("Pip","PipRota",new(-13,91.6f),200,false,new[]{("PipHomeFile","PipsOath")}),
                ("Tam","TamSkyGarden",new(36,94.4f),190,false,new (string,string)[0]),
                ("Iru",null,new(3,61),160,false,new[]{("IruHomeLamps","ShrineLamps")}),("Pim",null,new(11.6f,55.2f),230,false,new[]{("PimHomeKey","FourthPump")}),
                ("Neve",null,new(31,55.4f),150,false,new[]{("NeveHomeParcels","NeveParcel"),("NeveHomeSocks","ReturnAddress")}),("Survivor",null,new(46,51.6f),20,false,new[]{("SurvivorHomeLamps","SorrelBeacons")}),
                ("Ishen",null,new(47.6f,66.6f),250,false,new[]{("IshenHomeShelves","IshensRecord"),("IshenHomeMoons","MoonNames")}),
                ("Brekk",null,new(-33.4f,70.4f),140,false,new[]{("BrekkHomeAwning","UnswornRations"),("BrekkHomeKettle","OdrunsKitchen")}),("Corra",null,new(-30,73),180,false,new[]{("CorraHomeKettles","KettleTrade")}),
                ("Dov",null,new(-31.9f,56),90,false,new[]{("DovHomeSmokehouse","SmokehouseOrder"),("DovHomeBell","SchoolBell")}),("Binna",null,new(26.6f,37.6f),180,false,new[]{("BinnaHomeBaskets","ThreeBaskets")}),
                ("Ilo",null,new(39.6f,37.6f),190,false,new[]{("IloHomeBook","ClaimsLedger")}),("Hal",null,new(33.8f,35.4f),200,false,new[]{("HalHomeBenches","PortableBenches")}),
            };
            internal static Vector2 Stand(Vector2 at,float yaw){float a=yaw*Mathf.Deg2Rad;return at+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*2.4f;}
        }
        readonly List<string> talks=new(),follows=new();
        void Talked(string node,string speaker,System.Action<bool> done)=>talks.Add(node);
        void Followed(string node,string speaker,System.Action<bool> done)=>follows.Add(node);
        void Looked(DiscoveryPoint point){if(!string.IsNullOrEmpty(point.dialogueNode))talks.Add(point.dialogueNode);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter10");talks.Clear();follows.Clear();
            Npc.TalkRequested+=Talked;Npc.FollowRequested+=Followed;DiscoveryPoint.DialogueRequested+=Looked;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Talked;Npc.FollowRequested-=Followed;DiscoveryPoint.DialogueRequested-=Looked;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what this class writes by hand for the shared tests
        [ContentRegistry.Expectations] static void Afloat(ContentRegistry.Book book)
        {
            // The districts are hulls moored across open sky: their sides are hull plate going down to nothing, not a canyon's strata, and nothing below is a floor.
            book.Exempt("CliffBandsRunLevelAndTheCanyonIsNotStretched",Zone,"the districts' sides are hull plate going down into open sky (LastMooringBuilder.Neighbourhood.Trim), not strata over a floor");
            book.Exempt("EachNaturalPlaceHasItsOwnAir",Zone,"a town of moored hulls, not a natural place: it hums as Many Hands does");
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static T[] Live<T>()where T:Component=>All<T>().Where(c=>c.gameObject.activeInHierarchy).ToArray();
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Seconds(float s){float until=Time.unscaledTime+s;while(Time.unscaledTime<until)yield return null;}
        static float Flat(Vector3 a,Vector2 b)=>Vector2.Distance(new Vector2(a.x,a.z),b);
        static readonly string[] Speakers=Plan.People.Select(p=>p.id).Concat(new[]{"Idra","Sela","Taren","Bryn","Jun"}).Distinct().ToArray();
        /// <summary>The story told to the rendezvous: every earlier chapter (DevLoadout), the operation's flags, TurnBack at its step 13, everyone met.</summary>
        static void Told()
        {
            foreach(var f in new[]{"end.planHeard","end.planSaid","end.motherFreed","end.sectionHeld","end.gateOpen","end.boarded","end.meretShip","end.meretGantry","end.meretYielded","end.crewsOrdered",
                "end.anchorReleased","end.custody","end.passageRun","end.tugFetched","end.rendezvous","visited.SorrelSecondSky","visited.LastMooring"})Flag(f);
            foreach(var s in Speakers)Flag("met."+s);
            State.questSteps["TurnBack"]=13;
        }
        static IEnumerator Load(string spawn="Arrival")
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(Zone,spawn);deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,Zone+" never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.6f);
        }
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;actor.target=null;actor.TargetLocked=false;Physics.SyncTransforms();
        }
        static Npc Person(string speaker)=>Live<Npc>().FirstOrDefault(n=>n.speaker==speaker);
        static Homecoming Home(string speaker)=>All<Homecoming>().FirstOrDefault(h=>h.speaker==speaker&&h.npc!=null);
        static DiscoveryPoint Point(string name)=>All<DiscoveryPoint>().FirstOrDefault(p=>p.name==name);
        /// <summary>Waits out a conversation and everything asked to follow it: done once nothing has been said for a moment.</summary>
        static IEnumerator Settle(float most=40)
        {
            float deadline=Time.unscaledTime+most;
            for(float quiet=0;quiet<.6f&&Time.unscaledTime<deadline;){yield return null;quiet=DialogueSystem.Current.Running||GameInput.Current.Blocked?0:quiet+Time.unscaledDeltaTime;}
            Assert.IsFalse(DialogueSystem.Current.Running,"a conversation never ended");
        }
        IEnumerator Talk(Npc npc)
        {
            Assert.IsNotNull(npc,"fixture: nobody to talk to");Assert.IsTrue(npc.isActiveAndEnabled,npc.speaker+" offers nothing to talk to");
            DialogueSystem.Current.AutoAdvance=true;npc.Interact();yield return null;yield return Settle();yield return Seconds(.2f);
        }
        IEnumerator Take(DiscoveryPoint point)
        {
            Assert.IsNotNull(point,"fixture: no such point");Assert.IsTrue(point.Available,point.name+" isn't offered");
            DialogueSystem.Current.AutoAdvance=true;point.Interact();yield return null;yield return Settle();yield return Seconds(.2f);
        }
        static bool Reached(Vector3 from,Vector3 to)
        {
            if(!NavMesh.SamplePosition(from,out var a,2,NavMesh.AllAreas)||!NavMesh.SamplePosition(to,out var b,1.6f,NavMesh.AllAreas))return false;
            var path=new NavMeshPath();return NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
        static Vector3 OnFloor(Vector2 p){var top=new Vector3(p.x,12,p.y);return Physics.Raycast(top,Vector3.down,out var hit,40,~0,QueryTriggerInteraction.Ignore)?hit.point:new Vector3(p.x,-30,p.y);}

        // ------------------------------------------------------------------ the arrival
        /// <summary>The passage's cut lands at Arrival (wiring section 1): the party stands on the moorings, in a safe town with its paths, and the epilogue's flag is set by arriving.</summary>
        [UnityTest] public IEnumerator TheCutArrivesOnTheMooringsAndTheEpilogueBeginsThere()
        {
            Told();
            Assert.IsFalse(Has("end.epilogue"),"fixture: the epilogue has begun before anyone arrived");
            yield return Load();
            Assert.AreEqual(ZoneKind.GroundSafe,ZoneController.Current.definition.kind,"the neighbourhood isn't a safe town");
            Assert.IsNotNull(Object.FindFirstObjectByType<GroundNavigation>(),"the neighbourhood has no paths");
            Assert.Less(Flat(Lead.transform.position,Plan.Arrival),2.5f,"the party didn't arrive on the moorings");
            float deadline=Time.unscaledTime+3;while(!Has("end.epilogue")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(Has("end.epilogue"),"arriving didn't begin the epilogue (end.epilogue)");
            Assert.IsNotNull(Person("Orrin"),"step 13's arrow has nobody to find: Orrin isn't at his board");
        }

        // ------------------------------------------------------------------ people
        /// <summary>Everyone the epilogue names stands at their place on the plan, on a floor, and the place a player talks to them from is reached from the arrival and in their reach.</summary>
        [UnityTest] public IEnumerator EveryoneStandsWhereTheScriptPutsThemOnFloorReachedFromTheArrival()
        {
            Told();foreach(var p in Plan.People)foreach(var e in p.extras)Flag("quest."+e.quest+".complete");
            yield return Load();yield return Frames(3);
            var arrival=Lead.transform.position;
            // The check refuses a place a body can't reach: open sky between the market and the square.
            Assert.IsFalse(Reached(arrival,OnFloor(new Vector2(-14,46))),"the reach check takes open sky between two districts for floor");
            var faults=new List<string>();
            foreach(var (id,_,at,yaw,_,_) in Plan.People)
            {
                var npc=All<Npc>().FirstOrDefault(n=>n.speaker==id);
                if(npc==null||!npc.gameObject.activeInHierarchy){faults.Add(id+" isn't there");continue;}
                if(Flat(npc.transform.position,at)>.6f)faults.Add($"{id} stands at {npc.transform.position}, not at {at}");
                var stand=OnFloor(Plan.Stand(at,yaw));
                if(!Reached(arrival,stand))faults.Add($"nobody can walk to where {id} is spoken to ({stand})");
                if(Flat(npc.transform.position,new Vector2(stand.x,stand.z))>npc.range)faults.Add($"{id} is out of reach from where they're spoken to");
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
        /// <summary>Each person opens with their own epilogue line, says it again when asked again, and still does after the last scene (Orrin excepted: his offer, then nothing).
        /// The old beats are still there for each of them elsewhere: without the neighbourhood's own, an old offer or an aboard line would play seven weeks on.</summary>
        [UnityTest] public IEnumerator EachPersonOpensWithTheirOwnLineEveryTimeAndAfterTheLastScene()
        {
            // Each person's first finished story only where it is all they have (so they stand there), Tam back for a week, every visit already made (Ruva gives out Homecomings
            // in the middle of this, and an unmade visit would then be the person's prompt).
            Told();foreach(var p in Plan.People.Where(p=>p.node==null))Flag("quest."+p.extras[0].quest+".complete");Flag("quest.StillGround.complete");
            foreach(var p in Plan.People.Where(p=>p.visit))Flag("end.visit"+(p.id=="Keeper"?"Abri":p.id));
            // Away from the neighbourhood the same state plays older lines: what this check is against.
            var away=new GameState();foreach(var kv in State.flags)away.flags[kv.Key]=kv.Value;foreach(var kv in State.questSteps)away.questSteps[kv.Key]=kv.Value;away.zone="ManyHands";
            Assert.AreNotEqual("AbriHomecoming",SideStories.NodeFor("Keeper",away),"fixture: Abri's old line aboard should be another");
            yield return Load();yield return Frames(3);
            var expected=Plan.People.ToDictionary(p=>p.id,p=>p.node??p.extras[0].node);expected["Tam"]="TamHomeFrame";
            var faults=new List<string>();
            foreach(var round in new[]{"first","again"})
                foreach(var (id,node) in expected)
                {
                    if(id=="Orrin"&&round=="again")continue;
                    var npc=Person(id);if(npc==null){faults.Add(id+" isn't there");continue;}
                    // A counted visit is the person's prompt while it's wanted: Homecomings hasn't begun, so their own Npc answers.
                    if(!npc.enabled){faults.Add($"{id} offers nothing to talk to");continue;}
                    talks.Clear();yield return Talk(npc);
                    if(talks.FirstOrDefault()!=node)faults.Add($"{id}, {round}: said {talks.FirstOrDefault()??"nothing"}, not {node}");
                }
            // After the last scene everyone still says theirs; Orrin is at his board and offers nothing.
            Flag("game.complete");Flag("end.handrail");yield return Frames(3);
            Assert.IsFalse(Person("Orrin").enabled,"Orrin still offers a talk after the handrail is his");
            foreach(var (id,node) in expected.Where(e=>e.Key!="Orrin"))
            {
                var npc=Person(id);talks.Clear();yield return Talk(npc);
                if(talks.FirstOrDefault()!=node)faults.Add($"{id}, after the last scene: said {talks.FirstOrDefault()??"nothing"}, not {node}");
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
        /// <summary>A visit is the person's only prompt while it's wanted (two prompts on one body left the nearer one to answer, and a visit heard through the Npc was never counted);
        /// heard, it is counted once, the person's finished stories follow it in the same box, and their Npc says it again afterwards without counting it again.</summary>
        [UnityTest] public IEnumerator AVisitIsThePersonsOnlyPromptUntilHeardThenTheirExtrasFollowInTheSameBox()
        {
            Told();Flag("quest.MiraCrystals.complete");Flag("quest.SecondInventory.complete");
            yield return Load();RpgServices.Quests.Start("Homecomings");yield return Frames(3);
            var mira=Home("Mira");Assert.IsNotNull(mira,"fixture: Mira has no place in the town");
            // Without the Homecoming, her Npc and her visit are both offered at once: the fault.
            mira.enabled=false;mira.npc.enabled=true;yield return Frames(2);
            Assert.IsTrue(mira.npc.Available&&mira.point.Available,"fixture: with nothing to choose, both of Mira's prompts should be offered at once");
            mira.enabled=true;yield return Frames(2);
            Assert.IsFalse(mira.npc.enabled,"Mira's Npc still answers while her visit is wanted");Assert.IsTrue(mira.point.Available,"Mira's visit isn't offered");
            Put(Lead,OnFloor(Plan.Stand(mira.npc.transform.position.x*Vector2.right+mira.npc.transform.position.z*Vector2.up,110)));
            yield return Take(mira.point);
            CollectionAssert.AreEqual(new[]{"MiraHomecoming"},talks,"Mira's visit isn't her homecoming");
            CollectionAssert.AreEqual(new[]{"MiraHomeCrystals","MiraHomeTable"},follows,"Mira's finished stories don't follow her visit in the same box, in order");
            Assert.IsTrue(Has("end.visitMira"),"the visit isn't marked heard");
            State.questCounts.TryGetValue("Homecomings/0",out int counted);Assert.AreEqual(1,counted,"one visit isn't counted once");
            yield return Frames(3);Assert.IsTrue(mira.npc.enabled,"once her visit is heard Mira can't be talked to");
            talks.Clear();follows.Clear();yield return Talk(mira.npc);
            CollectionAssert.AreEqual(new[]{"MiraHomecoming"},talks,"asked again, Mira doesn't say her visit again");
            CollectionAssert.AreEqual(new[]{"MiraHomeCrystals","MiraHomeTable"},follows,"asked again, her stories don't follow");
            State.questCounts.TryGetValue("Homecomings/0",out counted);Assert.AreEqual(1,counted,"asking again counted the visit twice");
        }
        /// <summary>Homecomings is given by Ruva and finished by the nine visits, once each, in any order; the cache is paid once. Nothing else in the town counts toward it.</summary>
        [UnityTest] public IEnumerator RuvaGivesHomecomingsAndTheNineVisitsFinishIt()
        {
            Told();yield return Load();yield return Frames(3);
            var points=All<DiscoveryPoint>().Where(p=>p.report=="Homecoming").ToArray();
            Assert.AreEqual(9,points.Length,"the town doesn't have nine visits to make");
            Assert.IsFalse(points.Any(p=>p.Wanted),"a visit is offered before Ruva has given out Homecomings");
            yield return Talk(Person("Ruva"));
            Assert.AreEqual("RuvaPassageRules",talks.FirstOrDefault(),"Ruva isn't chairing motion nine");
            Assert.IsTrue(State.questSteps.TryGetValue("Homecomings",out int step)&&step==0,"Ruva's talk doesn't give out Homecomings");
            Assert.IsTrue(Has("end.rulesHeard"),"Ruva's motion nine isn't marked heard (her mark would never clear)");
            int scrip=State.scrip;
            foreach(var point in points.OrderBy(p=>p.name)){Put(Lead,OnFloor(new Vector2(point.transform.position.x,point.transform.position.z-2)));yield return Take(point);}
            Assert.IsTrue(Has("quest.Homecomings.complete"),"nine visits don't finish Homecomings");
            Assert.AreEqual(scrip+600,State.scrip,"Homecomings' cache wasn't paid once");
            Assert.IsFalse(points.Any(p=>p.Wanted),"a visit is still offered once Homecomings is done");
        }
        /// <summary>Someone whose only lines are extras stands in the town only if one of their stories was finished, and what stands with a story (Dace's lamps, Binna's baskets) only with it.</summary>
        [UnityTest] public IEnumerator SomeoneWhoseStoriesWereLeftUndoneIsNotThereAndNeitherAreTheirThings()
        {
            Told();Flag("quest.SorrelBeacons.complete",false);Flag("quest.ThreeBaskets.complete");
            yield return Load();yield return Frames(3);
            var dace=All<Homecoming>().First(h=>h.speaker=="Survivor");var binna=All<Homecoming>().First(h=>h.speaker=="Binna");
            Assert.IsTrue(dace.Nobody&&!dace.npc.gameObject.activeInHierarchy,"Dace stands on the terrace with nothing to say: his lamps were never lit");
            Assert.IsTrue(dace.extras[0].shows.All(s=>!s.activeInHierarchy),"Dace's four lamps stand on the rail though the beacons were never done");
            Assert.IsFalse(binna.Nobody,"Binna isn't at her window though her baskets came home");Assert.IsTrue(binna.npc.gameObject.activeInHierarchy,"Binna isn't there");
            Assert.IsTrue(binna.extras[0].shows.All(s=>s.activeInHierarchy),"Binna's three baskets aren't in her window");
            talks.Clear();yield return Talk(binna.npc);Assert.AreEqual("BinnaHomeBaskets",talks.FirstOrDefault(),"Binna doesn't open with her baskets");
        }

        // ------------------------------------------------------------------ the last steps
        /// <summary>Orrin's offer (step 13), then the handrail, then the line, in that order and no other; the line painted shows the wet new line and the pears one crate past it; the last
        /// scene plays at the stall as Taren's, sets game.complete, and from the painting nothing barks again.</summary>
        [UnityTest] public IEnumerator OrrinsOfferThenTheHandrailThenTheLineThenTheLastSceneAndNothingAfterIt()
        {
            Told();yield return Load();yield return Frames(3);
            var rail=Point("The garden stair: the handrail");var line=Point("The fruit stall: the line");
            // Out of turn: neither the rail nor the line is offered before Orrin's offer, and the line not before the rail.
            Assert.IsFalse(rail.Wanted||line.Wanted,"the handrail or the line is offered before Orrin has spoken");
            line.Interact();yield return Frames(2);Assert.IsFalse(Has("end.linePainted"),"the line was painted before the handrail was mended");
            Assert.AreEqual("OrrinDirectorship",SideStories.NodeFor("Orrin",State),"Orrin's offer isn't waiting");
            yield return Talk(Person("Orrin"));
            Assert.AreEqual("OrrinDirectorship",talks.FirstOrDefault(),"Orrin didn't make his offer");
            Assert.IsTrue(Has("end.handrail"),"the offer doesn't give Taren the handrail");Assert.AreEqual(14,State.questSteps["TurnBack"],"the main quest didn't move on to the handrail and the line");
            yield return Frames(3);Assert.IsFalse(Person("Orrin").enabled,"Orrin still offers a talk once the handrail is Taren's");
            Assert.IsTrue(rail.Wanted&&!line.Wanted,"after the offer it's the handrail first");
            var loose=All<Transform>().First(t=>t.name.StartsWith("The garden stair's south handrail, its third bracket loose")).gameObject;
            var mended=All<Transform>().First(t=>t.name=="The garden stair's south handrail, mended").gameObject;
            Assert.IsTrue(loose.activeInHierarchy&&!mended.activeInHierarchy,"the handrail isn't loose before it's mended");
            yield return Take(rail);yield return Frames(2);
            Assert.IsTrue(Has("end.railMended"),"mending the handrail isn't done");Assert.IsTrue(mended.activeInHierarchy&&!loose.activeInHierarchy,"the mended rail doesn't show");
            Assert.IsTrue(line.Wanted,"the line isn't offered once the rail is mended");
            var pearsAfter=All<Transform>().First(t=>t.name=="Jun's crate of pears, one crate past the line").gameObject;var newLine=All<Transform>().First(t=>t.name=="Floor detail: The new yellow line, wet").gameObject;
            Assert.IsFalse(pearsAfter.activeInHierarchy||newLine.activeInHierarchy,"the pears are past a line that isn't painted yet");
            Put(Lead,OnFloor(Plan.RepaintAt));talks.Clear();
            line.Interact();yield return Frames(2);
            Assert.IsTrue(Has("end.linePainted"),"painting the line isn't done");Assert.IsTrue(pearsAfter.activeInHierarchy&&newLine.activeInHierarchy,"the new line and the pears past it don't show");
            yield return Frames(2);Assert.IsTrue(BarkService.Hushed,"barks aren't hushed from the painting on");
            DialogueSystem.Current.AutoAdvance=true;
            float deadline=Time.unscaledTime+8;while(!talks.Contains("JunLastLine")&&Time.unscaledTime<deadline)yield return null;
            Assert.Contains("JunLastLine",talks,"the last scene doesn't play at the stall");
            yield return Settle();
            Assert.IsTrue(Has("game.complete"),"the last scene doesn't hand over to the credits (game.complete)");
            Assert.IsTrue(BarkService.Hushed,"something can bark after Taren's last word");
        }
        /// <summary>The message board's three, the gallery lantern's, the terminal's return leg and the moorings each play their line once; Idra's crew follow her at the cleat if her crew came home.</summary>
        [UnityTest] public IEnumerator TheBoardTheLanternTheTerminalAndTheMooringsEachSayTheirLine()
        {
            Told();Flag("quest.CutterDown.complete");yield return Load();yield return Frames(3);
            var cases=new[]{("The message board: a knotted cord","IruTernsMessage","end.msgTern"),("The message board: a seed tag","RuvaSennsMessage","end.msgSenn"),
                ("The message board: a page of valve drawings","PimQuillsMessage","end.msgQuill"),("The gallery lantern: Aumi's message","PellAumisMessage","end.msgAumi"),
                ("The travel terminal: the return leg","SelaReturnLeg","end.routeFiled"),("The moorings: one cleat","IdraTiedUp","end.cleatSeen")};
            foreach(var (name,node,flag) in cases)
            {
                var point=Point(name);talks.Clear();follows.Clear();yield return Take(point);
                Assert.AreEqual(node,talks.FirstOrDefault(),name+" doesn't play "+node);Assert.IsTrue(Has(flag),name+" doesn't set "+flag);Assert.IsFalse(point.Wanted,name+" is offered again once heard");
                if(node=="IdraTiedUp")CollectionAssert.AreEqual(new[]{"IdraHomeCrew"},follows,"Idra's crew don't follow her at the cleat");
            }
            // The survey: offered here as at Many Hands' table (one flag), filed once the road is flown.
            var offer=Point("The travel terminal: the nursery on the chart");var filed=Point("The travel terminal: the road filed");
            Assert.IsTrue(offer.Wanted,"the nursery isn't on the terminal's chart");Assert.IsFalse(filed.Wanted,"the road can be filed before it's flown");
            talks.Clear();yield return Take(offer);Assert.AreEqual("SelaLastSurveyOffer",talks.FirstOrDefault(),"the terminal doesn't offer the survey");
            Assert.IsTrue(State.questSteps.ContainsKey("LastSurvey"),"the offer doesn't give LastSurvey");
            State.questSteps["LastSurvey"]=1;Flag("end.surveyFlown");yield return Frames(2);
            Assert.IsTrue(filed.Wanted,"the flown road can't be put on the chart");talks.Clear();yield return Take(filed);
            Assert.AreEqual("SelaSurveyHome",talks.FirstOrDefault(),"filing the road isn't Sela's");Assert.IsTrue(Has("quest.LastSurvey.complete"),"filing the road doesn't finish the survey");
        }

        // ------------------------------------------------------------------ services and ways
        /// <summary>What Sela's chart lists for the neighbourhood stands in it (a shop, a bench, a repair), and the nursery's road opens from the moorings with the survey's offer.</summary>
        [UnityTest] public IEnumerator TheServicesTheChartListsStandAndTheNurserysRoadOpensWithItsOffer()
        {
            Told();yield return Load();yield return Frames(3);
            var stop=GameCatalog.Find<ChartStop>(Zone);Assert.IsTrue(stop!=null&&stop.shop&&stop.bench&&stop.repair,"the chart doesn't list the town's shop, bench and repair");
            Assert.IsNotEmpty(Live<Bench>(),"no bench");Assert.IsNotEmpty(Live<RepairBay>(),"no repair");
            var shop=Live<Shop>().FirstOrDefault();Assert.IsNotNull(shop,"no shop");Assert.IsTrue(shop.inventory!=null&&shop.inventory.offers.Length>0,"pitch eleven has nothing on its counter");
            var pad=All<DockingPad>().First(d=>d.scene=="SorrelSecondSky");
            Assert.IsFalse(pad.gameObject.activeInHierarchy,"the nursery's road is offered before the survey has been");
            Flag("sela.surveyOffered");yield return Frames(2);
            Assert.IsTrue(pad.gameObject.activeInHierarchy,"the nursery's road doesn't open with the survey's offer");Assert.AreEqual("Road",pad.spawn,"the road's pad doesn't land at the course's start");
            StringAssert.Contains("Sorrel's Second Sky",pad.prompt,"the road's pad doesn't say where it goes");
        }
        /// <summary>Each district is the place it came from, by what stands there (the stall and pear tree, the arcade's fronts, the rib, the rafts, the cabins, the lanterns, the
        /// sky-garden's trees), and every gap between them is crossed by a gangway with a handrail each side.</summary>
        [UnityTest] public IEnumerator EachDistrictStandsAsThePlaceItCameFromJoinedByGangways()
        {
            Told();yield return Load();yield return Frames(3);
            var names=new HashSet<string>(All<Transform>().Select(t=>t.name));
            foreach(var piece in new[]{"Jun's pear tree","Jun's fruit stall","The arcade's shopfronts","The rib from Lastlight, over the square","A reed raft on the basin","Tallow's cabins","A lantern tower",
                "The gallery lantern","The sky-garden","Orrin's new board","The message board","Meret's plain room","Drask's cabin over the square","Tovra's forge","The travel terminal","Forty cleats"})
                Assert.IsTrue(names.Contains(piece),"the town has no "+piece);
            // Each gangway by its own name (its corner rails are drawn under it as "<name>, its corners", and would count twice by a prefix).
            var drawn=new HashSet<string>(All<MeshRenderer>().Select(r=>r.name));
            foreach(var (name,_,_) in Plan.Gangways)Assert.IsTrue(drawn.Contains(name),"a gap has no gangway drawn across it: "+name);
            foreach(var (name,a,b) in Plan.Gangways)
            {
                var mid=(a+b)*.5f;Assert.IsTrue(Physics.Raycast(new Vector3(mid.x,6,mid.y),Vector3.down,out var hit,10,~0,QueryTriggerInteraction.Ignore)&&Mathf.Abs(hit.point.y)<.2f,name+" has nothing to walk on");
                var across=new Vector2(b.y-a.y,a.x-b.x).normalized*(Plan.GangwayHalf+.4f);
                foreach(var side in new[]{mid+across,mid-across})Assert.IsTrue(Physics.CheckSphere(new Vector3(side.x,.7f,side.y),.5f,~0,QueryTriggerInteraction.Ignore),name+" has no handrail on one side");
            }
        }
    }
}
