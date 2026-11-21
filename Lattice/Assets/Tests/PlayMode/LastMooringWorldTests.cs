using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 10's first engineer's work as a player meets it: the chapter's definitions (its six zones, three quests and the neighbourhood's chart stop, every story step moved
    /// on by a line of the script), its fixture (<c>DevLoadout chapter10</c>: chapter 9 told, the operation not begun), chapter 9's hand-off to it at Many Hands' chart table, the plan and
    /// Orrin's roll that begin it, the lanes off the hull's stern, and the chapter's beats aboard. The places and the fights are their own engineers' and tested in their own classes
    /// (docs/chapters/ch10-wiring.md, section 9).</summary>
    public sealed class LastMooringWorldTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter10");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void LastMooring(ContentRegistry.Book book)
        {
            // Nothing yet. The lanes off the hull's stern are drawn, and open, once the story has a reason for them (LastMooringBuilder.Approach.Lane): no lock to name. The script has
            // no node with choices and none past ninety seconds (manifest 8.2). Each place's and each fight's engineer declares their own in their own class.
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static int Step(string quest)=>State.questSteps.TryGetValue(quest,out var step)?step:-1;
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Prepared,"fixture: "+zone+"'s conversations never finished preparing");
            yield return new WaitForSecondsRealtime(.7f);
        }
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship)ship.Halt();else if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static IEnumerator Settle(float most=60)
        {
            float deadline=Time.unscaledTime+most;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(DialogueSystem.Current.Running,"a conversation never ended");
            yield return new WaitForSecondsRealtime(.3f);
        }
        static string Objective()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text.Split('\n')[0].Trim();
        static Vector3? Goal()=>Object.FindFirstObjectByType<ObjectiveHud>().Goal;
        static DiscoveryPoint Point(string flag)=>All<DiscoveryPoint>().Single(p=>p.flag==flag);
        static string ScriptText()=>string.Concat(Directory.GetFiles(Path.Combine(Application.dataPath,"_Project/Resources/Dialogue"),"*.yarn").OrderBy(f=>f).Select(File.ReadAllText)).Replace("\r\n","\n");

        const string Sorrel="SorrelSecondSky",Mooring="LastMooring",Continuance="Continuance",Chamber="StillwaterChamber",Passage="ReturningPassage",Afloat="NeighbourhoodAfloat";
        static readonly string[] Quests={"TurnBack","LastSurvey","Homecomings"};

        /// <summary>A quest's Flag steps that no line of the script sets: such a step can never move on (a flag set by a point or by code moves a Flag step only at the next load, guide 8).</summary>
        static List<string> Unmoved(QuestDef quest,string script)
        {
            var settable=new HashSet<string>(Regex.Matches(script,@"<<setFlag\s+([^\s>]+)\s*>>").Cast<Match>().Select(m=>m.Groups[1].Value));
            var wrong=new List<string>();
            for(int i=0;i<quest.steps.Length;i++)if(quest.steps[i].kind==ObjectiveKind.Flag&&!settable.Contains(quest.steps[i].target))wrong.Add($"{quest.id} step {i+1} waits on {quest.steps[i].target}, which no line of the script sets");
            return wrong;
        }

        // ================================================================== the chapter's definitions
        [UnityTest] public IEnumerator TheChapterIsDefinedAsItsScriptAsks()
        {
            yield return null;
            var zones=new Dictionary<string,(ZoneKind kind,string name,string music)>
            {
                [Sorrel]=(ZoneKind.SpaceCombat,"Sorrel's Second Sky","Adventure Awaits"),[Mooring]=(ZoneKind.SpaceCombat,"Last Mooring","Starfight"),
                [Continuance]=(ZoneKind.GroundCombat,"The Continuance","Regal Alien Town"),[Chamber]=(ZoneKind.GroundCombat,"The Anchor Chamber","Regal Alien Town"),
                [Passage]=(ZoneKind.SpaceCombat,"The Returning Passage","Endless Flight"),[Afloat]=(ZoneKind.GroundSafe,"Neighbourhood Afloat","Hub Town Groove"),
            };
            var built=Enumerable.Range(0,SceneManager.sceneCountInBuildSettings).Select(i=>Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i))).ToArray();
            foreach(var (id,(kind,name,music)) in zones)
            {
                var zone=GameCatalog.Find<ZoneDef>(id);Assert.IsNotNull(zone,id+" has no ZoneDef");Assert.AreEqual(id,zone.id);Assert.AreEqual(id,zone.scene);Assert.AreEqual(10,zone.chapter,id);
                Assert.AreEqual(kind,zone.kind,id);Assert.AreEqual(name,zone.displayName,id+": the manifest's name (section 5)");Assert.AreEqual(music,zone.musicId,id);
                Assert.AreEqual(1,zone.enemyThreat,id+"'s threat isn't 1: chapter 10's hits are authored at their weight");Assert.IsNotNull(zone.hd2dProfile,id+" has no profile");
                CollectionAssert.Contains(built,id,id+" isn't in the build list: its scene (or its placeholder) was never built");
            }
            // Every speaker of the script has a definition and a face; nobody new speaks (manifest 5). Meret's two new expressions are on her sheet.
            string script=ScriptText();StringAssert.Contains("title: JunLastLine\n",script,"fixture: LastMooring.yarn isn't in Resources/Dialogue (LastMooringBuilder.Dialogue)");
            string own=File.ReadAllText(Path.Combine(Application.dataPath,"_Project/Resources/Dialogue/LastMooring.yarn")).Replace("\r\n","\n");
            var lines=Regex.Matches(own,@"^([A-Z][A-Za-z]+): ",RegexOptions.Multiline).Cast<Match>().Select(m=>m.Groups[1].Value).Distinct().ToArray();
            Assert.GreaterOrEqual(lines.Length,30,"fixture: the chapter's speakers weren't read from its script");
            foreach(string speaker in lines){var person=GameCatalog.Find<CharacterDef>(speaker);Assert.IsNotNull(person,speaker+" speaks in chapter 10 and has no definition");Assert.IsNotNull(person.portraitNatural,speaker+" has no portrait set");}
            var meret=GameCatalog.Find<CharacterDef>("Meret").portraitNatural;
            foreach(var feeling in new[]{PortraitEmotion.Determined,PortraitEmotion.Shocked})Assert.IsTrue(meret.entries.Any(e=>e.emotion==feeling&&e.sprite!=null),"Meret's sheet has no "+feeling+" of its own");
            // The three quests, as the manifest has them (section 4).
            var main=GameCatalog.Find<QuestDef>("TurnBack");Assert.IsNotNull(main,"the main quest isn't defined");Assert.IsTrue(main.main);Assert.AreEqual(10,main.chapter);
            Assert.AreEqual(15,main.steps.Length);Assert.AreEqual("game.complete",main.steps[14].target,"the story ends on the game's last line");
            foreach(var id in Quests)
            {
                var quest=GameCatalog.Find<QuestDef>(id);Assert.IsNotNull(quest,id+" isn't defined");Assert.AreEqual(10,quest.chapter,id);
                Assert.AreEqual(quest.steps.Length,quest.stepText.Length,id);Assert.AreEqual(quest.steps.Length,quest.stepTargets.Length,id+": every step says where it happens");
                foreach(string text in quest.stepText){Assert.IsFalse(string.IsNullOrWhiteSpace(text),id+" has a blank step");Assert.LessOrEqual(text.Length,80,$"{id}: \"{text}\" runs past the panel's two lines");}
                Assert.AreEqual(1,Regex.Matches(script,@"<<startQuest\s+"+id+@"\s*>>").Count,id+" isn't started by exactly one conversation");
            }
            // Every story step that waits on a flag is moved on by a line of the script. The check itself first, on a step the script never sets, so it can fail.
            var broken=ScriptableObject.CreateInstance<QuestDef>();broken.id="Broken";broken.steps=new[]{new Objective{kind=ObjectiveKind.Flag,target="end.nobodySaysThis",count=1}};
            Assert.IsNotEmpty(Unmoved(broken,script),"the check finds nothing wrong with a step on a flag nobody sets");Object.Destroy(broken);
            var unmoved=Quests.SelectMany(id=>Unmoved(GameCatalog.Find<QuestDef>(id),script)).ToList();
            Assert.IsEmpty(unmoved,string.Join("\n",unmoved));
            // The neighbourhood's stop, charted by arriving, in the last column's top slot.
            var stop=GameCatalog.Find<ChartStop>(Afloat);Assert.IsNotNull(stop,"the neighbourhood has no stop on Sela's chart");Assert.AreEqual(Afloat,stop.zone);Assert.AreEqual("Arrival",stop.spawn);
            Assert.AreEqual("visited."+Afloat,stop.UnlockFlag);Assert.AreEqual(10,stop.chapter);
        }

        // ================================================================== the fixture: chapter 9 told
        /// <summary>What a party arriving at chapter 10 must have (the plan's Sync, the third hero out with the lead, chapter 9 told with its home gone and its chart read, the Halo's stop
        /// closed and the Crawl's charted) and must not (any of chapter 10 begun).</summary>
        static List<string> NotArrivedAtTen()
        {
            var wrong=new List<string>();
            if(State.party.Count!=3)wrong.Add($"the party is {State.party.Count}, not three");
            foreach(var member in State.party)if(member.level!=DevLoadout.SyncOnArrival(10))wrong.Add($"{member.id} is at Sync {member.level}, not {DevLoadout.SyncOnArrival(10)}");
            if(State.deployed==null||!State.deployed.Contains("Taren")||!State.deployed.Contains("Idra"))wrong.Add("Taren and Idra aren't the pair out");
            foreach(string flag in new[]{"chapter9.complete","quest.HouseLeaves.complete","home.launched","bossdown.DebrisCutter","home.chartRead"})if(!Has(flag))wrong.Add(flag+" isn't set: chapter 9 isn't told");
            var told=StoryQuests.Told(State);if(told==null||told.id!="HouseLeaves")wrong.Add("the chapter told is "+(told!=null?told.id:"none")+", not The House That Can Leave");
            if(StoryQuests.Open(State,out _)!=null)wrong.Add("a story quest is open on arriving");
            foreach(string id in Quests)if(Step(id)>=0||Has("quest."+id+".complete"))wrong.Add(id+" is begun on arriving");
            foreach(string flag in new[]{"end.planHeard","end.planSaid"})if(Has(flag))wrong.Add(flag+" is set on arriving");
            var halo=GameCatalog.Find<ChartStop>("DecksLeaving");if(halo==null||RouteChart.Of(halo,State)!=RouteChart.Standing.Closed)wrong.Add("Cinder Halo's stop isn't closed: its districts have flown");
            var crawl=GameCatalog.Find<ChartStop>("MeridianCrawl");if(crawl==null||!Has(crawl.UnlockFlag))wrong.Add("the Crawl isn't charted");
            var afloat=GameCatalog.Find<ChartStop>(Afloat);if(afloat==null||Has(afloat.UnlockFlag))wrong.Add("the neighbourhood is charted before anyone has stood there");
            return wrong;
        }
        [UnityTest] public IEnumerator AChapterTenArrivalHasChapterNineToldAndTheOperationNotBegun()
        {
            yield return null;State.zone="ManyHands";
            // A chapter 9 arrival is wrong for chapter 10 in every way the check names, so the check can fail.
            GameServices.Current.NewGame();DevLoadout.Apply("chapter9");State.zone="ManyHands";
            var early=NotArrivedAtTen();TestContext.WriteLine("CHAPTER_NINE_ARRIVAL "+string.Join(" | ",early));
            Assert.IsTrue(early.Any(w=>w.Contains("chapter 9 isn't told")),"a chapter 9 arrival passes for chapter 10: "+string.Join(" | ",early));
            GameServices.Current.NewGame();DevLoadout.Apply("chapter10");State.zone="ManyHands";
            var wrong=NotArrivedAtTen();Assert.IsEmpty(wrong,"DevLoadout chapter10:\n"+string.Join("\n",wrong));
        }

        // ================================================================== Many Hands: chapter 9 hands on, the plan, the roll
        [UnityTest] public IEnumerator ChapterNineHandsOnAtTheChartTableAndThePlanBeginsTheOperation()
        {
            yield return Zone("ManyHands");
            var told=GameCatalog.Find<QuestDef>("HouseLeaves");
            StringAssert.EndsWith(": hear Idra's plan at the chart table.",told.closingLine,"chapter 9's last word doesn't end with what to do next (D273)");
            Assert.AreEqual(told.closingLine,Objective(),"between chapters the panel doesn't say chapter 9's last word");
            var plan=Point("end.planHeard");Assert.AreEqual("IdraOnePlan",plan.dialogueNode);Assert.AreEqual("OnePlan",plan.report);Assert.AreEqual("chapter9.complete",plan.requiredFlag);
            Assert.IsTrue(plan.Available,"Idra's plan isn't offered at the chart table with chapter 9 told");
            var goal=Goal();Assert.IsTrue(goal.HasValue,"chapter 9's last word and no arrow");Assert.Less(Flat(goal.Value,plan.transform.position),3,$"the arrow points at {goal}, and the plan is at {plan.transform.position}");
            // Someone can stand there: the point is on the floor plan within its reach of walkable floor.
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(plan.transform.position,out var stand,plan.range,UnityEngine.AI.NavMesh.AllAreas),"no floor anyone can stand on within reach of Idra's plan");
            var roll=All<AfterTalk>().Single(t=>t.node=="OrrinRoll");Assert.AreEqual("end.planHeard",roll.flag);Assert.AreEqual("end.planSaid",roll.heardFlag);Assert.AreEqual("Orrin",roll.speaker);
            var survey=Point("sela.surveyOffered");Assert.AreEqual("SelaLastSurveyOffer",survey.dialogueNode);Assert.IsFalse(survey.Available,"Sela's survey is offered before the plan is said");
            // Orrin waits for the plan: nothing plays by itself.
            yield return new WaitForSecondsRealtime(2);
            Assert.IsFalse(DialogueSystem.Current.Running,"something talks with nothing pressed before the plan");Assert.IsFalse(Has("end.planSaid"),"Orrin's roll played before Idra's plan");
            // The plan, then the roll straight after, which starts the operation and says its first part.
            Put(Lead,stand.position);yield return Frames(3);
            DialogueSystem.Current.AutoAdvance=true;plan.Interact();yield return null;yield return Settle();
            Assert.IsTrue(Has("end.planHeard"),"the plan's point didn't keep its flag");
            float deadline=Time.unscaledTime+12;while(!DialogueSystem.Current.Running&&!Has("end.planSaid")&&Time.unscaledTime<deadline)yield return null;
            yield return Settle(90);
            Assert.IsTrue(Has("end.planSaid"),"Orrin's roll didn't follow the plan");
            Assert.AreEqual(1,Step("TurnBack"),"the roll didn't begin the operation and say its first part");
            yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual("Go back to the first moon",Objective(),"the panel doesn't say the operation's first step");
            // Its arrow is the way to the hull, where the lane to Sorrel is.
            var toHull=InteractionPrompt.Active.Where(p=>p is DockingPad pad&&pad.scene=="ManyHandsHull"||p is WarpBeacon warp&&warp.scene=="ManyHandsHull").Select(p=>p.transform.position).ToArray();
            Assert.IsNotEmpty(toHull,"fixture: Many Hands has no way to its hull");
            goal=Goal();Assert.IsTrue(goal.HasValue&&toHull.Any(at=>Flat(at,goal.Value)<3),$"the arrow points at {goal}, and the way to the hull is at {string.Join(", ",toHull)}");
            Assert.IsTrue(survey.Available||Flat(Lead.transform.position,survey.transform.position)>survey.range,"Sela's survey isn't offered once the plan is said");
        }

        // ================================================================== the hull: the lanes off the stern
        [UnityTest] public IEnumerator TheLanesOffTheSternAreDrawnOnlyOnceTheStoryHasAReason()
        {
            yield return Zone("ManyHandsHull");
            var sorrel=All<WarpBeacon>().Single(w=>w.scene==Sorrel);var mooring=All<WarpBeacon>().Single(w=>w.scene==Mooring);
            Assert.IsFalse(sorrel.gameObject.activeInHierarchy,"the lane to Sorrel is drawn before the plan is said");Assert.IsFalse(mooring.gameObject.activeInHierarchy,"the lane to Last Mooring is drawn before she is free");
            Flag("end.planSaid");yield return Frames(3);
            Assert.IsTrue(sorrel.gameObject.activeInHierarchy&&InteractionPrompt.Active.Contains(sorrel),"once the plan is said the lane to Sorrel isn't drawn and offered");
            Assert.AreEqual("Warp — Sorrel's Second Sky",sorrel.prompt);Assert.IsTrue(string.IsNullOrEmpty(sorrel.requiredFlag),"the lane to Sorrel is drawn and shut");Assert.AreEqual("Arrival",sorrel.spawn);
            Assert.IsFalse(mooring.gameObject.activeInHierarchy,"the lane to Last Mooring is drawn before she is free");
            Put(Lead,sorrel.transform.position+Vector3.back*2);yield return Frames(3);
            Assert.IsTrue(sorrel.Available,"the lane to Sorrel isn't offered from beside it: something is awake by it");
            Flag("end.motherFreed");yield return Frames(3);
            Assert.IsTrue(mooring.gameObject.activeInHierarchy&&InteractionPrompt.Active.Contains(mooring),"once she is free the lane to Last Mooring isn't drawn and offered");
            Assert.AreEqual("Warp — Last Mooring",mooring.prompt);Assert.IsTrue(string.IsNullOrEmpty(mooring.requiredFlag));
            // One sign each, and the spawns the ships come home to.
            foreach(string words in new[]{"Warp — Sorrel's Second Sky","Warp — Last Mooring"})Assert.AreEqual(2,All<TextMeshPro>().Count(t=>t.text==words),words+": a second staging piled up its signs (the warp's own and the staged one)");
            foreach(string id in new[]{"Sorrel","Mooring"})Assert.AreEqual(1,All<SpawnPoint>().Count(s=>s.id==id),"the hull has no single spawn "+id+" for a ship coming home");
        }

        // ================================================================== the beats aboard
        [UnityTest] public IEnumerator ChapterTensBeatsWaitForChapterTen()
        {
            yield return null;State.zone="ManyHands";Flag("met.Drask");
            Assert.AreEqual("AbriNamesOnTheWall",SideStories.NodeFor("Keeper",State),"chapter 9's last aboard line for Abri was lost before the plan");
            Assert.AreNotEqual("DraskBeforeTheField",SideStories.NodeFor("Drask",State),"Drask's line before the field plays before the plan");
            Flag("end.planSaid");
            Assert.AreEqual("AbriFortiethDay",SideStories.NodeFor("Keeper",State));Assert.AreEqual("DraskBeforeTheField",SideStories.NodeFor("Drask",State));
            Flag("visited."+Sorrel);Assert.AreNotEqual("AbriFortiethDay",SideStories.NodeFor("Keeper",State),"Abri still notes the fortieth day after Sorrel");
            State.zone=Afloat;Assert.AreNotEqual("DraskBeforeTheField",SideStories.NodeFor("Drask",State),"Drask's line aboard plays off the ship");
            // And none of it wins in chapter 9.
            GameServices.Current.NewGame();DevLoadout.Apply("chapter9");State.zone="ManyHands";Flag("met.Drask");Flag("home.noticeHeard");
            Assert.AreEqual("AbriNamesOnTheWall",SideStories.NodeFor("Keeper",State),"chapter 10's line wins in chapter 9");
        }
    }
}
