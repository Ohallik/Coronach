using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The Decks on separation day (zone DecksLeaving, HomeBuilder.Decks; docs/maps/DecksLeaving.md): chapter 1's Hub_Decks left exactly as it was, the day's Decks
    /// taking its place only between going home and the launch, nothing of chapter 1's story left to fire, each of home's people saying the day's words whatever chapter 1
    /// left undone, the meeting as eight talks in turn in one room that turns to whoever has the floor, and Hal taking his bench down in nine pieces with Taren holding the
    /// lamp. What every zone gets from the shared checks is theirs and not repeated here. Coordinates are the builder's own constants (HomeBuilder.Decks).</summary>
    public sealed class DecksLeavingTests
    {
        readonly List<string> heard=new();
        void Talked(string node,string speaker,System.Action<bool> done)=>heard.Add(node);
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter9");heard.Clear();Npc.TalkRequested+=Talked;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Talked;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what this class writes by hand for the shared tests
        [ContentRegistry.Expectations] static void DecksLeaving(ContentRegistry.Book book)
        {
            // The way down under the promenade: "The town meets first: Orrin has called it in the dock office".
            book.Lock("home.meetingHeld","dock office");
            // The day is a copy of chapter 1's Hub_Decks, which none of these checks reads: a station's deck plates and bulkheads (StationRedesign), with no landform floor or cliff
            // wall to meet in shade or band, and the station's own hum for its air.
            const string Station="the Decks on separation day is chapter 1's Hub_Decks copied whole (HomeBuilder.Decks): station deck plates and bulkheads, not a landform";
            book.Exempt("FloorAndWallMeetInShade",Zone,Station+", so no floor meets a cliff wall");
            book.Exempt("CliffBandsRunLevelAndTheCanyonIsNotStretched",Zone,Station+", so there are no cliff faces");
            book.Exempt("EachNaturalPlaceHasItsOwnAir",Zone,"a station's deck, the Decks itself: it hums as Hub_Decks does, which isn't one of the natural places either");
            // Hal's three choices from his crates are HomeWorldTests' entry (one chapter may say a node's branches once: the registry throws on a second). Mira's three talks
            // (MiraInventoryOffer, MiraCratedRepeat, MiraInventoryReturn) end at her crated stall with no choice: the script gives each a <<shop>> and no option, so they are not branched,
            // and DialogueInputTests reads where they end from their own lines (EndingOf). Branched, that test waits for a choice that never comes. Manifest 8.2 asks for a Branch on each;
            // a Branch is a choice, and these have none.
        }

        // ------------------------------------------------------------------ the day's places (HomeBuilder.Decks' constants)
        static readonly Vector2 BenchAt=new(32,6),FrontBenchWest=new(-31.9f,5.6f),FrontBenchEast=new(-23.2f,5.6f);
        const string Zone="DecksLeaving";
        static readonly string[] Story={"home.meetingOpened","home.offerMade","home.halAsked","home.manifestShown","home.namesRead","home.odaStood","home.orrinSaid","home.meetingHeld"};

        // ------------------------------------------------------------------ fixtures
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static T[] Live<T>()where T:Component=>All<T>().Where(c=>c.gameObject.activeInHierarchy).ToArray();
        static float Flat(Vector3 a,Vector2 b)=>Vector2.Distance(new Vector2(a.x,a.z),b);
        static IEnumerator Load(string zone,string spawn)
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone,spawn);deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.6f);
        }
        static void Home(params string[] more){foreach(var f in new[]{"home.treadRecovered","home.returned"}.Concat(more))Flag(f);}
        static Npc Person(string speaker)=>Live<Npc>().Where(n=>n.speaker==speaker).OrderBy(n=>n.name).FirstOrDefault();
        static IEnumerator Talk(Npc npc,System.Action<int> eachLine=null)
        {
            Assert.IsNotNull(npc,"fixture: nobody to talk to");
            var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=true;npc.enabled=true;npc.Interact();yield return null;
            float deadline=Time.unscaledTime+40;
            while((dialogue.Running||GameInput.Current.Blocked)&&Time.unscaledTime<deadline){if(dialogue.Running)eachLine?.Invoke(dialogue.LinesPresented-dialogue.NodeStartLines);yield return null;}
            Assert.IsFalse(dialogue.Running,npc.speaker+"'s conversation never ended");
            yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ chapter 1's Decks, untouched
        /// <summary>The build read Hub_Decks before it began and again after it had saved the day (objects, a hash of the whole hierarchy, the file's bytes: HomeBuilder.Decks.Fingerprint),
        /// wrote both down and refused on a difference. Here: what it wrote says the same before and after, and the Hub_Decks in the tree now is byte for byte the file it read.</summary>
        [Test] public void HubDecksIsTheSameSceneBeforeAndAfterTheDaysBuild()
        {
            string record=Path.Combine(Application.dataPath,"_Project/Scenes/DecksLeaving-Source.txt"),scene=Path.Combine(Application.dataPath,"_Project/Scenes/Hub_Decks.unity");
            Assume.That(File.Exists(record),"fixture: DecksLeaving hasn't been built here (HomeBuilder.BuildZones -home-zones DecksLeaving writes DecksLeaving-Source.txt)");
            var lines=File.ReadAllLines(record);
            var before=Read(lines,"before");var after=Read(lines,"after");
            // The comparison catches a change: the same record with one object more after the build, or another hash, is refused.
            Assert.IsFalse(Same(before,(after.objects+1,after.hash,after.file)),"a Hub_Decks with one object more after the build should read as changed");
            Assert.IsFalse(Same(before,(after.objects,after.hash+"0",after.file)),"a Hub_Decks whose hierarchy hash moved should read as changed");
            Assert.IsTrue(Same(before,after),$"Hub_Decks changed while the day was built: objects {before.objects}>{after.objects}, hash {before.hash}>{after.hash}, file {before.file}>{after.file}");
            Assert.Greater(after.objects,500,"fixture: the record names too few objects for the Decks: "+after.objects);
            using var sha=SHA256.Create();string now=string.Concat(sha.ComputeHash(File.ReadAllBytes(scene)).Take(8).Select(b=>b.ToString("x2")));
            Assert.AreEqual(after.file,now,"Hub_Decks.unity in the tree isn't the file the day's build left untouched: chapter 1's scene has been saved since (rebuild DecksLeaving after any Hub_Decks staging)");
        }
        static (int objects,string hash,string file) Read(string[] lines,string which)
        {
            var line=lines.FirstOrDefault(l=>l.StartsWith("Hub_Decks "+which+" "));Assert.IsNotNull(line,"the record has no "+which+" line");
            var m=Regex.Match(line,@"objects=(\d+) hash=(\w+) file=(\w+)");Assert.IsTrue(m.Success,"unreadable record line: "+line);
            return (int.Parse(m.Groups[1].Value),m.Groups[2].Value,m.Groups[3].Value);
        }
        static bool Same((int objects,string hash,string file) a,(int objects,string hash,string file) b)=>a.objects==b.objects&&a.hash==b.hash&&a.file==b.file;

        // ------------------------------------------------------------------ the window
        [UnityTest] public IEnumerator EveryWayToTheDecksLeadsToTheDayOnlyFromGoingHomeUntilTheLaunch()
        {
            // Before the story sends the party home: chapter 1's Decks, from the Halo's dock as ever.
            yield return Load("Hub_Decks","Office");
            Assert.AreEqual("Hub_Decks",SceneManager.GetActiveScene().name,"before home.treadRecovered the Halo's dock should land in chapter 1's Decks");
            Flag("home.treadRecovered");
            yield return Load("Hub_Decks","Office");
            Assert.AreEqual(Zone,SceneManager.GetActiveScene().name,"once the story says go home, a way to Hub_Decks should land on the Decks on separation day");
            Assert.AreEqual(Zone,GameServices.Current.State.zone,"the save should name the day's Decks");
            Assert.Less(Flat(PartyController.Current.Active.transform.position,new Vector2(-28,-11)),3,"the dock's spawn on the day is chapter 1's Office spawn");
            Flag("home.launched");
            yield return Load("Hub_Decks","Office");
            Assert.AreEqual("Hub_Decks",SceneManager.GetActiveScene().name,"after the launch nothing stands in for the Decks any more");
            var day=GameCatalog.Find<ChartStop>(Zone);var first=GameCatalog.Find<ChartStop>("Decks");
            Assert.IsNotNull(day,"the day's chart stop isn't defined");Assert.AreEqual("home.treadRecovered",day.unlockFlag,"the day's stop is charted when the story sends the party home");
            Assert.AreEqual("home.launched",day.lockedFlag,"the day's stop closes at the launch");
            if(first!=null){Assert.AreEqual("home.launched",first.lockedFlag,"chapter 1's Decks stop closes at the launch");StringAssert.Contains("came apart",first.lockedReason);}
        }

        // ------------------------------------------------------------------ nothing of chapter 1's story
        [UnityTest] public IEnumerator NothingOfChapterOnesStoryCanTalkOrFireOnTheDay()
        {
            Home();yield return Load(Zone,"Arrival");
            Assert.IsEmpty(All<OpeningMarketSequence>(),"the first morning's sequence is still on the day");
            Assert.IsEmpty(All<ScriptedWalk>(),"a chapter 1 scripted walk is still on the day");
            Assert.IsEmpty(All<QuestStarter>(),"a quest starter is on the day");
            Assert.IsFalse(All<Npc>().Any(n=>n.speaker=="Sela"),"Sela's waiting spot from chapter 1 is still an Npc on the day");
            var first=FirstChapterNodes();Assume.That(first.Count,Is.GreaterThan(10),"fixture: chapter 1's Decks script wasn't found");
            foreach(var npc in All<Npc>())foreach(var node in new[]{npc.firstNode,npc.repeatNode,npc.postNode,npc.finalNode,npc.finalRepeatNode})
                Assert.IsFalse(!string.IsNullOrEmpty(node)&&first.Contains(node),$"{npc.name} ({npc.speaker}) on the day would still say chapter 1's {node}");
            foreach(var point in All<DiscoveryPoint>())Assert.IsFalse(point.flag.StartsWith("opening.")||first.Contains(point.dialogueNode??""),point.name+" is chapter 1's first morning");
            Assert.IsFalse(All<FlagGate>().Any(g=>g.flag=="warpkey"||g.flag.StartsWith("opening.")),"one of chapter 1's flag gates is still on the day");
        }
        static HashSet<string> FirstChapterNodes()
        {
            var found=new HashSet<string>();string file=Path.GetFullPath(Path.Combine(Application.dataPath,"../../docs/writing/Decks.yarn"));
            if(File.Exists(file))foreach(var line in File.ReadAllLines(file))if(line.StartsWith("title: "))found.Add(line.Substring(7).Trim());
            return found;
        }

        // ------------------------------------------------------------------ home's people say the day's words
        /// <summary>A save that left chapter 1 undone (Oda's token unasked, Neve's parcel undelivered, Dace's beacons unreported, Mira already thanked) keeps those beats where they were
        /// (chapter 1's Decks still offers them), and on the day each of these people says the day's words instead (manifest 8.3).</summary>
        [Test] public void OnTheDayHomesPeopleSayTheDaysWordsWhateverChapterOneLeftUndone()
        {
            GameState State(string zone)
            {
                var s=new GameState{zone=zone};
                foreach(var f in new[]{"met.Orrin","met.Mira","met.Hal","met.Neve","met.Oda","met.Ilo","met.Survivor","met.Dorrow","met.Tam","mira.thanked","bossdown.Burrower","home.treadRecovered","home.returned"})s.flags[f]=true;
                s.questSteps["OdaBerth"]=0;s.questSteps["NeveParcel"]=0;s.questSteps["SorrelBeacons"]=1;return s;
            }
            var chapterOne=State("Hub_Decks");
            Assert.AreEqual("OrrinBerth",SideStories.NodeFor("Orrin",chapterOne),"fixture: chapter 1's Decks should still ask Orrin for Oda's token");
            Assert.AreEqual("NeveParcelOffer",SideStories.NodeFor("Neve",chapterOne)??"NeveParcelOffer");
            var day=State(Zone);
            var expected=new Dictionary<string,string>{["Oda"]="OdaDecks",["Ilo"]="IloDecks",["Neve"]="NeveFlyingAStreet",["Survivor"]="SurvivorDecks",["Mira"]="MiraInventoryOffer",["Hal"]="HalBenchesOffer",
                ["Tam"]="TamStillOffer",["Dorrow"]="DorrowListens"};
            foreach(var (speaker,node) in expected)Assert.AreEqual(node,SideStories.NodeFor(speaker,day),speaker+" on the day");
            day.flags["home.returned"]=false;Assert.AreEqual("OrrinHomecoming",SideStories.NodeFor("Orrin",day),"Orrin's first words on the day are his homecoming");
            day.flags["home.returned"]=true;day.questSteps["SecondInventory"]=0;Assert.AreEqual("MiraCratedRepeat",SideStories.NodeFor("Mira",day),"Mira, her quest under way, sells from her crates (not chapter 1's thanks)");
        }

        // ------------------------------------------------------------------ chapter 1's quests once the Decks has gone
        /// <summary>Oda's berth token (asked of Orrin at the Decks, taken to Oda at the Halo) and Neve's parcel (from Neve at the Halo, to Dace, who is on the Decks by chapter 9) point at
        /// places the launch ends: left undone, each leaves the log at <c>home.launched</c> with no reward and no word. Mira's crystals point at Sorrel, which stays, and stay; a quest done stays done.</summary>
        [Test] public void ChapterOnesQuestsThatPointAtTheDecksLeaveTheLogWhenItGoes()
        {
            var state=GameServices.Current.State;var quests=RpgServices.Quests;int scrip=state.scrip;
            foreach(var id in new[]{"OdaBerth","NeveParcel"})Assert.AreEqual("home.launched",GameCatalog.Find<QuestDef>(id).letGoAt,id+" should leave the log when the Decks goes");
            state.questSteps.Remove("OdaBerth");state.questSteps.Remove("NeveParcel");state.questSteps.Remove("MiraCrystals");state.flags.Remove("quest.MiraCrystals.complete");
            // Under way, not done: chapter 9's loadout carries Ridge Crystals, and with six in the bag Mira's job finished as it started and paid its 100 inside this test's measure.
            state.materials.Remove("RidgeCrystal");state.collectedMaterials.Remove("RidgeCrystal");
            quests.Start("OdaBerth");quests.Start("NeveParcel");quests.Start("MiraCrystals");
            Assert.IsTrue(state.questSteps.ContainsKey("OdaBerth")&&state.questSteps.ContainsKey("NeveParcel"),"fixture: the two quests are under way");
            Flag("home.returned");quests.Report(ObjectiveKind.Flag,"home.returned");
            Assert.IsTrue(state.questSteps.ContainsKey("OdaBerth"),"Oda's token goes only once the Decks does, not on the day");
            Flag("home.launched");quests.Report(ObjectiveKind.Flag,"home.launched");
            Assert.IsFalse(state.questSteps.ContainsKey("OdaBerth"),"Oda's berth token is still in the log pointing at a dock office that has flown");
            Assert.IsFalse(state.questSteps.ContainsKey("NeveParcel"),"Neve's parcel is still in the log pointing at the Halo");
            Assert.IsTrue(Has("quest.OdaBerth.letGo")&&!Has("quest.OdaBerth.complete"),"let go, not done");
            Assert.AreEqual(scrip,state.scrip,"a quest let go pays nothing");
            Assert.IsTrue(state.questSteps.ContainsKey("MiraCrystals"),"Mira's crystals point at Sorrel, which is still there");
        }

        // ------------------------------------------------------------------ the town meeting
        [UnityTest] public IEnumerator TheMeetingIsEightTalksEachInItsTurnInOneRoom()
        {
            Home("home.meetingGathered");yield return Load(Zone,"Meeting");
            var room=All<Transform>().FirstOrDefault(t=>t.name=="The town meeting"&&t.parent==null);
            Assert.IsNotNull(room,"no meeting room in the scene");Assert.IsTrue(room.gameObject.activeInHierarchy,"the town isn't in the dock office once it has gathered");
            Assert.IsFalse(Live<Npc>().Any(n=>n.speaker=="Mira"&&Flat(n.transform.position,new Vector2(-7.7f,-.9f))<1),"Mira is at her stall and in the meeting at once");
            Npc In(string speaker)=>Live<Npc>().Where(n=>n.speaker==speaker&&n.transform.IsChildOf(room)).FirstOrDefault()??(speaker=="Orrin"?Person("Orrin"):null);
            var order=new[]{"Orrin","Meret","Hal","Mira","Ilo","Oda","Orrin","Orrin"};
            var nodes=new[]{"OrrinMeetingOpens","MeretBerthOffer","HalApprentices","MiraManifest","IloReadsNames","OdaStands","OrrinPostponed","MeretWithdraws"};
            var cast=new[]{"Orrin","Meret","Hal","Mira","Ilo","Oda","Jun","Survivor"}.Select(In).ToArray();
            for(int i=0;i<cast.Length;i++)Assert.IsNotNull(cast[i],"nobody in the meeting speaks as "+new[]{"Orrin","Meret","Hal","Mira","Ilo","Oda","Jun","Survivor"}[i]);
            for(int step=0;step<8;step++)
            {
                yield return null;yield return null;
                var open=cast.Where(n=>n.GetComponent<TurnToSpeak>()==null||n.GetComponent<TurnToSpeak>().Open).Select(n=>n.speaker).Distinct().ToArray();
                CollectionAssert.AreEqual(new[]{order[step]},open,$"talk {step+1} ({nodes[step]}): only {order[step]} should have something to say, not {string.Join(", ",open)}");
                Assert.AreEqual(nodes[step],SideStories.NodeFor(order[step],GameServices.Current.State),$"talk {step+1} plays {nodes[step]}");
                Flag(Story[step]);
            }
            // Meret stands: no pose holds her sitting. Nobody sits on the front bench, which has a crate on it.
            Assert.IsNull(cast[1].GetComponent<Posed>(),"Meret should be standing at the front");
            var crate=All<Transform>().FirstOrDefault(t=>t.name=="The crate on the front bench");Assert.IsNotNull(crate,"no crate on the front bench");
            Assert.Less(Flat(crate.position,FrontBenchWest),2.2f,"the crate isn't on the front bench");Assert.Greater(crate.position.y,.3f,"the crate isn't up on the bench");
            foreach(var person in All<Transform>().Where(t=>t.name.StartsWith("Townsperson")&&t.gameObject.activeInHierarchy).Concat(Live<Npc>().Select(n=>n.transform)))
                foreach(var bench in new[]{FrontBenchWest,FrontBenchEast})
                    Assert.IsFalse(Mathf.Abs(person.position.x-bench.x)<2.1f&&Mathf.Abs(person.position.z-bench.y)<.5f,person.name+" sits on the front bench");
        }
        [UnityTest] public IEnumerator OdaStandsOnHerFirstWordAndFourHandsGoUpForTheBerth()
        {
            Home("home.meetingGathered","home.meetingOpened","home.offerMade","home.halAsked","home.manifestShown","home.namesRead");yield return Load(Zone,"Meeting");
            var oda=Live<Npc>().First(n=>n.speaker=="Oda"&&n.GetComponent<Posed>()!=null);var jun=Live<Npc>().First(n=>n.speaker=="Jun"&&n.GetComponent<Posed>()!=null);
            Assert.AreEqual("base",oda.GetComponent<Posed>().Showing,"Oda should be sitting in the third row before she speaks");
            string odaAtFirst=null;
            yield return Talk(oda,line=>{if(line==1&&odaAtFirst==null)odaAtFirst=oda.GetComponent<Posed>().Showing;});
            Assert.AreEqual("stand",odaAtFirst,"Oda stands on her first word (\"Here.\")");
            Assert.AreEqual("stand",jun.GetComponent<Posed>().Showing,"Jun stands when Mira says she's up");
            Assert.IsTrue(Has("home.odaStood"));
            // The vote: Orrin's MeretWithdraws.
            Flag("home.orrinSaid");
            var room=All<Transform>().First(t=>t.name=="The town meeting"&&t.parent==null);
            int Hands()=>room.GetComponentsInChildren<Posed>().Count(p=>p.Showing=="hand");
            int forIt=-1,against=-1,after=-1;
            yield return Talk(Person("Orrin"),line=>{if(line==1)forIt=Hands();else if(line==2)against=Mathf.Max(against,Hands());else if(line==3)after=Hands();});
            Assert.AreEqual(4,forIt,"four hands go up for the berth");
            Assert.Greater(against,20,"against it, the room's hands go up (Orrin can't see the back wall)");
            Assert.AreEqual(0,after,"hands come down once Orrin has counted");
            Assert.IsTrue(Has("home.meetingHeld")&&Has("home.berthRefused"),"the meeting ends refusing the berth");
        }

        /// <summary>The November 20 capture's town meeting (s57 055, 136, 155): the seated town sank into its benches or crouched through them, turned bodily on the bench toward
        /// whoever had the floor, and wore four copies of one woman and four of one man in one screen. Here the room is held as Ilo reads the names (the room turns to Ilo, to Jun, to
        /// Dace at the back and to Oda in her row, all four far off to one side of most of the benches): every seated body turns no more than a seat lets it (Posed.SeatTurn),
        /// and no body is worn twice in the room. (How the seated sit is not held here: see the chapter 8 report of November 21.)</summary>
        [UnityTest] public IEnumerator TheTownSitsOnItsBenchesAndTurnsOnThemToWhoeverHasTheFloor()
        {
            Home("home.meetingGathered","home.meetingOpened","home.offerMade","home.halAsked","home.manifestShown");yield return Load(Zone,"Meeting");
            var room=All<Transform>().First(t=>t.name=="The town meeting"&&t.parent==null);
            var seated=room.GetComponentsInChildren<Transform>().Where(t=>t.name=="Townsperson: Sitting at the meeting").ToArray();
            Assert.GreaterOrEqual(seated.Length,7,"fixture: the seated town isn't in the room");
            // Where each would face if turned bodily to each speaker of the talk: most of the room is more than a seat's turn from at least one (the check below means something).
            var speakers=new[]{"Ilo","Jun","Survivor","Oda"}.Select(s=>Live<Npc>().First(n=>n.speaker==s&&n.transform.IsChildOf(room)).transform.position).ToArray();
            float Away(Transform t,Vector3 to){var look=to-t.position;look.y=0;return Mathf.Abs(Mathf.DeltaAngle(0,Quaternion.LookRotation(look).eulerAngles.y));}
            Assert.GreaterOrEqual(seated.Count(t=>speakers.Any(s=>Away(t,s)>Posed.SeatTurn+10)),6,"fixture: the talk should call most of the seated round further than a seat turns");
            // One of them held as the room was built before (no seat on its pose, so it turns bodily): it has to turn past what a seat allows, or the check below proves nothing.
            var loose=seated.OrderByDescending(t=>speakers.Max(s=>Away(t,s))).First();var looseSeat=loose.GetComponent<Posed>();float keep=looseSeat.seat;looseSeat.seat=0;
            float worst=0,looseWorst=0;
            yield return Talk(Person("Ilo"),_=>{foreach(var t in seated){float a=Mathf.Abs(Mathf.DeltaAngle(0,t.eulerAngles.y));if(t==loose)looseWorst=Mathf.Max(looseWorst,a);else worst=Mathf.Max(worst,a);}});
            looseSeat.seat=keep;
            Assert.IsTrue(Has("home.namesRead"),"fixture: Ilo's talk didn't finish");
            Assert.Greater(looseWorst,Posed.SeatTurn+5,$"fixture: a body with no seat should have turned bodily past a seat's {Posed.SeatTurn} degrees (it turned {looseWorst:0})");
            Assert.LessOrEqual(worst,Posed.SeatTurn+.5f,$"a seated body turned {worst:0} degrees on its bench while Ilo read the names: a seat turns {Posed.SeatTurn}");
            // Nobody's double in the room: no body worn twice.
            var bodies=room.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Townsperson")).Select(t=>(body:BodyOf(t),west:t.position.x<-28)).ToArray();
            Assert.GreaterOrEqual(bodies.Length,30,"fixture: the town isn't at the meeting");
            foreach(var g in bodies.GroupBy(b=>b.body))
            {
                Assert.AreEqual(1,g.Count(),$"{g.Key} is worn {g.Count()} times at the meeting ({g.Count(b=>b.west)} west of the aisle)");
            }
        }
        /// <summary>The November 22 capture's town meeting (s57 139, 142, 156): every body posed off a seat was drawn with its pelvis and chest turned about 164 degrees from the way it
        /// was placed (the generated bodies' idle, sampled outside play, turns them round), so the seated sat with their backs to the board and their knees bent back under them, and
        /// Oda and Jun stood up facing the back of the room; and Oda left an empty cushion on the bench beside her. Here, as the meeting loads after Ilo's talk: everyone posed off a
        /// seat is drawn facing the way their body faces, the seated with their knees that way too; and once Oda's talk is over nobody standing has a seat left out.</summary>
        [UnityTest] public IEnumerator TheSeatedFaceTheWayTheySitAndWhoeverStandsLeavesNoEmptySeat()
        {
            Home("home.meetingGathered","home.meetingOpened","home.offerMade","home.halAsked","home.manifestShown","home.namesRead");yield return Load(Zone,"Meeting");
            yield return new WaitForSecondsRealtime(1.2f);
            var room=All<Transform>().First(t=>t.name=="The town meeting"&&t.parent==null);
            var posed=room.GetComponentsInChildren<Posed>().Where(p=>p.seat>0).ToArray();
            Assert.GreaterOrEqual(posed.Length,10,"fixture: the seated town, Oda, Jun and Oda's mother aren't in the room");
            // The pelvis's facing (across the hip joints) and the knees' (hip to knee), against the way the body faces.
            static Transform Bone(Posed p,HumanBodyBones b)=>p.GetComponentInChildren<Animator>(true).GetBoneTransform(b);
            static float Twist(Posed p){var drawn=Vector3.Cross(Vector3.up,Bone(p,HumanBodyBones.LeftUpperLeg).position-Bone(p,HumanBodyBones.RightUpperLeg).position);drawn.y=0;var f=p.transform.forward;f.y=0;return Vector3.Angle(drawn,f);}
            static float Knees(Posed p){var k=Bone(p,HumanBodyBones.LeftLowerLeg).position-Bone(p,HumanBodyBones.LeftUpperLeg).position+Bone(p,HumanBodyBones.RightLowerLeg).position-Bone(p,HumanBodyBones.RightUpperLeg).position;k.y=0;var f=p.transform.forward;f.y=0;return Vector3.Angle(k,f);}
            // The check sees a body turned round: one sitter's pelvis turned as the capture drew it, measured, and put back (one frame; nothing runs between).
            var probe=posed.First(p=>p.Seated);var hips=Bone(probe,HumanBodyBones.Hips);var kept=hips.rotation;hips.rotation=Quaternion.AngleAxis(164,Vector3.up)*kept;
            float turned=Twist(probe);hips.rotation=kept;
            Assert.Greater(turned,120,"fixture: a pelvis turned round should read as turned round");
            foreach(var p in posed)
            {
                Assert.Less(Twist(p),35,$"{p.name} at ({p.transform.position.x:0.0},{p.transform.position.z:0.0}) is drawn {Twist(p):0} degrees from the way it faces ({p.Showing})");
                if(p.Seated)Assert.Less(Knees(p),50,$"{p.name} at ({p.transform.position.x:0.0},{p.transform.position.z:0.0}) sits with its knees {Knees(p):0} degrees off the way it faces");
            }
            // Oda's talk: she stands, then Jun, then her mother. After it nobody who has stood up off a seat has a seat of their own left out on the bench (the capture's green
            // cushion was Oda's: sitting turned round, her pelvis hung over the slats and the seating rule gave her one).
            yield return Talk(Person("Oda"));
            Assert.IsTrue(Has("home.odaStood"),"fixture: Oda's talk didn't finish");
            var stood=room.GetComponentsInChildren<Posed>().Where(p=>p.seat>0&&!p.Seated).ToArray();
            Assert.GreaterOrEqual(stood.Length,3,"fixture: Oda, Jun and Oda's mother should be on their feet");
            foreach(var p in stood){var fit=p.GetComponent<SeatFit>();bool left=fit!=null&&fit.seat!=null&&fit.seat.activeInHierarchy;Assert.IsFalse(left,$"{p.name} has stood up and left {(left?fit.seat.name:"")} out on the bench");}
        }
        /// <summary>Which body a townsperson wears (its rig's avatar, one to each generated body).</summary>
        static string BodyOf(Transform person){var rig=person.GetComponentInChildren<Animator>(true);return rig!=null&&rig.avatar!=null?rig.avatar.name:person.GetComponentInChildren<SkinnedMeshRenderer>(true)?.sharedMesh?.name??"?";}

        // ------------------------------------------------------------------ Hal's bench
        [UnityTest] public IEnumerator HalTakesHisBenchDownInNinePiecesWhileTarenHoldsTheLamp()
        {
            Home("home.meetingGathered","home.meetingHeld","home.saintReleased","bossdown.FoundrySaint");yield return Load(Zone,"Lift");
            var pieces=All<Transform>().Where(t=>t.name.StartsWith("Bench piece ")).OrderBy(t=>t.name).ToArray();
            Assert.AreEqual(9,pieces.Length,"Hal's bench should be nine pieces");
            Assert.IsTrue(pieces.All(p=>Flat(p.position,BenchAt)<1.6f),"the bench should stand whole before Hal starts");
            var taren=PartyController.Current.members.FirstOrDefault(m=>m!=null&&m.character=="Taren");Assume.That(taren,Is.Not.Null,"fixture: Taren isn't out");
            var hand=taren.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
            var lamp=All<Transform>().First(t=>t.name=="The hand lamp");
            int heldFrom=-1;var hal=Live<Npc>().First(n=>n.speaker=="Hal");var halStart=hal.transform.position;
            yield return Talk(hal,line=>{if(heldFrom<0&&lamp.gameObject.activeInHierarchy&&lamp.IsChildOf(hand))heldFrom=line;});
            CollectionAssert.Contains(heard,"HalLastMachine","Hal's last machine should be what he says");
            Assert.AreEqual(4,heldFrom,"Taren takes the lamp on the fourth line (\"No. Hold the lamp. Higher. There.\"), not before");
            Assert.IsFalse(lamp.IsChildOf(taren.transform),"the lamp is put down once the talk is over");
            yield return new WaitForSecondsRealtime(3);
            Assert.IsTrue(Has("home.halChose"));
            foreach(var p in pieces)Assert.Greater(Flat(p.position,BenchAt),1.8f,p.name+" was never taken off the bench");
            Assert.IsFalse(All<Transform>().First(t=>t.name=="Hal's bench, standing").gameObject.activeInHierarchy,"the bench's solid box stands where there's no bench");
            Assert.Greater(Flat(hal.transform.position,new Vector2(halStart.x,halStart.z)),.5f,"Hal does the work: he should have moved round the bench");
            // From the next load the pieces are packed in a crate and the bolt holes show where it stood.
            yield return Load(Zone,"Lift");
            Assert.IsFalse(All<Transform>().First(t=>t.name=="Hal's bench in nine pieces").gameObject.activeInHierarchy,"the loose pieces are still out after a reload");
            var packed=All<Transform>().First(t=>t.name=="Hal's bench, packed");Assert.IsTrue(packed.gameObject.activeInHierarchy,"the crate of nine pieces isn't there after the bench came down");
            Assert.IsNotNull(packed.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Four bolt holes"),"no bolt holes where the bench stood");
        }

        // ------------------------------------------------------------------ the way down
        [UnityTest] public IEnumerator TheWayDownWaitsForTheMeetingAndSaysSo()
        {
            Home();yield return Load(Zone,"Arrival");
            var down=All<WarpBeacon>().Where(w=>w.scene=="SeparationWorks").ToArray();
            Assert.AreEqual(1,down.Length,"one way down to the Separation Works");
            Assert.AreEqual("home.meetingHeld",down[0].requiredFlag,"the way down opens once the meeting is held");
            StringAssert.Contains("dock office",down[0].lockedPrompt,"the shut way down should say what opens it");
            Assert.Less(Flat(down[0].transform.position,new Vector2(6,-10.15f)),1,"the way down is the gate in the market vestibule");
            foreach(var spawn in new[]{"Works","Lift","Meeting","Arrival","Office","Shop","Repair"})Assert.IsTrue(All<SpawnPoint>().Any(s=>s.id==spawn),"no spawn "+spawn+" on the day");
            Assert.IsTrue(All<DockingPad>().Count(d=>d.scene=="Hub_CinderHalo")==3,"the Halo's three docks are the Decks' ways out on the day");
        }
    }
}
