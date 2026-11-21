using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The checks that hold chapter content to its definitions: what used to be an arm of a switch somewhere, and failed without a word when someone forgot it.
    /// The first test pins the four bosses' registrations to the switches they replaced, value for value.</summary>
    public sealed class ContentFrameworkTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static GameState State=>GameServices.Current.State;
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.6f);
        }
        static string Objective()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text.Split('\n')[0];

        [UnityTest] public IEnumerator TheFourBossesAreRegisteredAsTheirSwitchesHadThem()
        {
            yield return null;
            CollectionAssert.AreEquivalent(new[]{"Burrower","BellowsBelow","Cantor","HessaKorl"},BossController.RegisteredIds.Intersect(ContentRegistry.FirstBosses).ToArray(),"one of the first four bosses has lost its registration");
            foreach(var (id,thresholds,moves,promised,radius,occludes,ending) in new[]
            {
                ("Burrower",new[]{.6f,.3f},new[]{"DrillCharge","SpoilToss","TailSweep","Burrow","SeismicRings"},new[]{"Burrow","SeismicRings"},1.4f,true,BossController.Ending.ShutDown),
                ("BellowsBelow",new[]{.5f,.3f},new[]{"Breath","RibStar","Bubbles","Crossing"},new[]{"Bubbles","Crossing"},1.4f,true,BossController.Ending.Destroyed),
                ("Cantor",new[]{.75f,.4f},new[]{"Choir","SongSpiral","CoilLash"},new[]{"SongSpiral","CoilLash"},1.4f,false,BossController.Ending.Released),
                ("HessaKorl",new[]{.66f,.33f},new[]{"HookLance","Advance","GrappleChain","Sidearm","SweepingCut","Lampbreaker"},new[]{"SweepingCut","Lampbreaker"},.8f,true,BossController.Ending.Yields),
            })
            {
                CollectionAssert.AreEqual(thresholds,BossController.ThresholdsOf(id),id+"'s phase lines moved");
                CollectionAssert.AreEquivalent(moves,BossController.MovesOf(id),id+"'s moves changed");
                Assert.IsNull(BossController.PromisedMove(id,1),id+" promises a move in its first phase");
                Assert.AreEqual(promised[0],BossController.PromisedMove(id,2),id+"'s second phase promises another move");
                Assert.AreEqual(promised[1],BossController.PromisedMove(id,3),id+"'s third phase promises another move");
                Assert.AreEqual(promised[1],BossController.PromisedMove(id,4),id+"'s last promise doesn't hold past phase 3, as the old switch's did");
                Assert.AreEqual(radius,BossController.CapsuleRadius(id),.0001f,id+"'s body changed width");
                Assert.AreEqual(occludes,BossController.Occludes(id),id+"'s occlusion changed");
                Assert.AreEqual(ending,BossController.EndingOf(id),id+" ends another way");
            }
            // Anything that isn't a registered boss keeps what the old code gave it by default.
            Assert.AreEqual(1.4f,BossController.CapsuleRadius("Ridgehound"),.0001f);Assert.IsFalse(BossController.Occludes("Ridgehound"));Assert.AreEqual(BossController.Ending.Destroyed,BossController.EndingOf("Ridgehound"));
        }
        [UnityTest] public IEnumerator EveryBossDefinedIsRegisteredAndHasItsSheet()
        {
            yield return null;
            foreach(string id in ContentRegistry.Bosses)
            {
                Assert.IsTrue(BossController.Registered(id),id+"'s EnemyDef says boss and no BossController.<Name>.cs registers it: it would have no specials");
                var sheet=ContentRegistry.Boss(id);
                Assert.IsNotNull(GameCatalog.Find<ZoneDef>(sheet.zone),id+"'s sheet puts its door in "+sheet.zone+", which is no zone");
                Assert.Greater(sheet.sync,0,id+"'s sheet gives no Sync");Assert.Greater(sheet.driverShare,0,id+"'s sheet gives no driverShare");
            }
            foreach(string id in ContentRegistry.OrdinaryEnemies.Where(e=>System.Array.IndexOf(ContentRegistry.FirstEnemies,e)<0))ContentRegistry.Tell(id);
        }
#if UNITY_EDITOR
        [UnityTest] public IEnumerator ABossNobodyRegisteredStopsInsteadOfBorrowingAnothersMoves()
        {
            yield return Zone("Arena_Ground");
            var nobody=ScriptableObject.CreateInstance<EnemyDef>();nobody.id="NobodysBoss";nobody.boss=true;nobody.integrity=1000;
            var stopped=Assert.Throws<System.InvalidOperationException>(()=>ActorFactory.Enemy(nobody,new Vector3(200,.1f,0)),"a boss with no registration was built, and used to fight with the Burrower's charge");
            StringAssert.Contains("BOSS_UNREGISTERED NobodysBoss",stopped.Message);
            Assert.IsNull(GameObject.Find("NobodysBoss"),"it stopped with half a body left in the scene");
            Object.Destroy(nobody);
        }
#endif
        [UnityTest] public IEnumerator EveryZoneDefinedIsInTheBuildUnderItsOwnNameWithItsMusicStaged()
        {
            yield return null;
            foreach(var zone in ContentRegistry.ZoneDefs)
            {
                Assert.IsTrue(zone.id==zone.name&&zone.scene==zone.name,$"ZoneDef {zone.name}: its id ('{zone.id}'), its scene ('{zone.scene}') and its file name must be the same string");
                Assert.IsTrue(Application.CanStreamedLevelBeLoaded(zone.name),zone.name+" has a ZoneDef and isn't in the build: it can't be loaded (build the zone; the build list is read from the ZoneDefs)");
                Assert.IsNotNull(zone.cameraProfile,zone.name+" has no camera profile");Assert.IsNotNull(zone.hd2dProfile,zone.name+" has no HD-2D profile");
                if(!ContentRegistry.IsFirst(zone.name))Assert.IsFalse(string.IsNullOrWhiteSpace(zone.musicId),zone.name+" names no music (ZoneDef.musicId): it plays in silence");
                foreach(string track in new[]{zone.musicId,zone.fightMusicId,zone.pocketMusicId,zone.shopMusicId})
                    if(!string.IsNullOrWhiteSpace(track))Assert.IsNotNull(Resources.Load<AudioClip>("Audio/Music/"+track),$"{zone.name} names the track '{track}', which isn't staged: run MusicIntake.Stage");
            }
            foreach(var boss in ContentRegistry.EnemyDefs.Where(e=>e.boss&&!string.IsNullOrWhiteSpace(e.bossMusicId)))
                Assert.IsNotNull(Resources.Load<AudioClip>("Audio/Music/"+boss.bossMusicId),$"{boss.name} names the track '{boss.bossMusicId}', which isn't staged: run MusicIntake.Stage");
        }
        [UnityTest] public IEnumerator AZoneWithNoLinesOfItsOwnSaysTheStorysStepOrItsNameAndNeverThePracticeLine()
        {
            yield return Zone("Hub_Decks","Office");
            string decks=Objective();Assert.IsFalse(string.IsNullOrEmpty(decks),"fixture: the Decks have an objective");
            // A zone the panel has no case for, as every zone since chapter 2 is: the place's name with no story open.
            State.zone="Nowhere_Yet";yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual("Nowhere Yet",Objective(),"a zone with no lines of its own and no story quest open doesn't show its name");
            // And the open story quest's step once there is one, in its own words.
            RpgServices.Quests.Start("LanternRoad");yield return new WaitForSecondsRealtime(.6f);
            var road=GameCatalog.Find<QuestDef>("LanternRoad");
            StringAssert.StartsWith(road.stepText[0],Objective(),"a zone with no lines of its own doesn't show the open story quest's step");
            StringAssert.DoesNotContain("Practice",Objective(),"the yard's practice line shows in a real zone");
            // In a zone that has its lines, they're untouched by any of it.
            State.questSteps.Remove("LanternRoad");State.zone="Hub_Decks";yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(decks,Objective(),"the Decks' own line changed");
            yield return Zone("Arena_Ground");
            StringAssert.Contains("Practice",Objective(),"the yard lost its practice line");
        }
        [UnityTest] public IEnumerator AChapterLoadoutArrivesAtThePlansSyncWithTheEarlierChaptersTold()
        {
            yield return null;
            // Asked of the state as it stands: a new game is a new set of flags.
            bool Has(string flag)=>GameServices.Current.Flags.GetBool(flag);
            Assert.Throws<System.ArgumentException>(()=>DevLoadout.Apply("chapter11"),"a chapter past the last is taken for a loadout");
            DevLoadout.Apply("chapter2");
            Assert.IsTrue(State.party.Count==2&&State.party.All(m=>m.level==7),"chapter 2 is reached at Sync 7");
            Assert.IsTrue(Has("chapter1.complete")&&Has("bossdown.Cantor")&&Has("quest.Hushwell.complete")&&Has("warpkey"),"chapter 1 isn't told");
            Assert.IsFalse(State.questSteps.ContainsKey("LanternRoad"),"chapter 2's own quest is already begun on arriving");Assert.IsFalse(Has("chapter2.complete"));
            GameServices.Current.NewGame();DevLoadout.Apply("chapter3");
            Assert.IsTrue(State.party.All(m=>m.level==10),"chapter 3 is reached at Sync 10");
            var road=GameCatalog.Find<QuestDef>("LanternRoad");
            Assert.AreEqual(road.steps.Length,State.questSteps["LanternRoad"],"The Lantern Road isn't complete on arriving at chapter 3");
            foreach(string told in new[]{"chapter1.complete","chapter2.complete","quest.LanternRoad.complete","bossdown.HessaKorl","met.Aumi","lanternwick.chartGiven","chapter2.homecoming"})Assert.IsTrue(Has(told),"arriving at chapter 3 without "+told);
            Assert.IsTrue(State.party.All(m=>m.equipped.Count>0),"a chapter loadout arrives with nothing equipped");
            for(int chapter=DevLoadout.FirstChapter;chapter<DevLoadout.LastChapter;chapter++)Assert.LessOrEqual(DevLoadout.SyncOnArrival(chapter),DevLoadout.SyncOnArrival(chapter+1),"the plan's Sync falls between chapters");
            Assert.AreEqual(27,DevLoadout.SyncOnArrival(10));
        }
        [UnityTest] public IEnumerator AWayOutCarriedOnATallBodyIsOfferedFromTheFloorBesideIt()
        {
            // Rootmarket's river gate: an arch six metres high, placed by its middle, with a reach of 4.6 m and a place to stand 3.4 m off on the floor. Measured to
            // the arch's middle by the straight line that was 4.67 m, and the gate offered nothing (first player run of chapter 3, November 13).
            yield return Zone("TallowDrift");
            var hero=Lattice.Combat.PartyController.Current.Active.transform.position;
            var tall=new GameObject("Tall way").AddComponent<Lattice.World.WarpBeacon>();tall.prompt="A way on a tall arch";tall.range=4.6f;tall.scene="TallowApproach";tall.requiredFlag="";
            var above=new GameObject("Upper floor way").AddComponent<Lattice.World.WarpBeacon>();above.prompt="A way on the floor above";above.range=3;above.scene="TallowApproach";above.requiredFlag="";
            try
            {
                foreach(var other in InteractionPrompt.Active.ToArray())if(other!=tall&&other!=above)other.enabled=false;
                tall.transform.position=hero+new Vector3(3.4f,3.2f,0);above.transform.position=hero+new Vector3(60,0,0);
                PromptService.FindInteraction(hero);
                Assert.AreSame(tall,PromptService.Interaction,"a way out on a tall body isn't offered from the floor 3.4 m off: its reach is measured to its middle, overhead");
                // A floor above is still a floor above: flat over the hero and further up than its own reach.
                tall.transform.position=hero+new Vector3(60,0,0);above.transform.position=hero+new Vector3(.5f,3.6f,0);
                PromptService.FindInteraction(hero);
                Assert.IsNull(PromptService.Interaction,"a prompt on the floor above is offered through the floor");
                // And out of reach on the plan is out of reach.
                above.transform.position=hero+new Vector3(3.2f,0,0);
                PromptService.FindInteraction(hero);
                Assert.IsNull(PromptService.Interaction,"a prompt 3.2 m off with a reach of 3 is offered");
            }
            finally{Object.Destroy(tall.gameObject);Object.Destroy(above.gameObject);}
        }
        [UnityTest] public IEnumerator AHurtPartyThatCannotPayForTheRepairStillSavesAtACradle()
        {
            // "Saving costs nothing; only a repair does" was true only of a party that wasn't hurt: hurt and short of the fee, it was turned away with nothing saved.
            yield return null;
            State.scrip=0;State.party[0].integrity=1;int before=SaveSystem.Asked;
            Assert.IsTrue(Lattice.World.RepairBay.Repair(),"a hurt party with an empty purse is turned away from the cradle");
            Assert.AreEqual(before+1,SaveSystem.Asked,"the cradle took no save");
            Assert.AreEqual(1,State.party[0].integrity,.01f,"the repair was given away");Assert.AreEqual(0,State.scrip);
            // One that can pay is repaired, charged the fee and saved, as ever.
            State.scrip=1000;int fee=Lattice.World.RepairBay.Fee(State);
            Assert.IsTrue(Lattice.World.RepairBay.Repair());
            Assert.AreEqual(1000-fee,State.scrip,"the repair's fee");Assert.Greater(State.party[0].integrity,1,"paid for and not repaired");Assert.AreEqual(before+2,SaveSystem.Asked);
        }
        [UnityTest] public IEnumerator BetweenChaptersTwoAndThreeThePanelPointsAtAbriNotBackToTheReach()
        {
            // Chapter 2 told and the Seed Road not begun: Abri is the one person with something to say, twice (her last word on the lanterns, then her offer).
            // The panel said 'Launch for the Wick Reach warp' and pointed at the pad (first player run, November 13).
            yield return null;
            GameServices.Current.NewGame();DevLoadout.Apply("chapter3");
            Assert.IsFalse(State.questSteps.ContainsKey("SeedRoad"),"fixture: chapter 3's quest is already open on arriving");
            GameServices.Current.Flags.SetBool(Lattice.World.SideStories.AfterwordHeard,false);
            yield return Zone("TallowDrift");
            var hud=Object.FindFirstObjectByType<ObjectiveHud>();
            var abri=InteractionPrompt.Active.OfType<Lattice.World.Npc>().First(p=>p.speaker=="Keeper").transform.position;
            Assert.AreEqual("Hear what Keeper Abri says next",Objective().Trim(),"with the homecoming told, the panel doesn't send the party to Abri");
            Assert.IsTrue(hud.Goal.HasValue&&Vector3.Distance(hud.Goal.Value,abri)<3,$"the arrow points at {hud.Goal}, and Abri stands at {abri}");
            GameServices.Current.Flags.SetBool(Lattice.World.SideStories.AfterwordHeard,true);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual("Ask Keeper Abri about the cabins' dinner",Objective().Trim(),"with her last word heard, the panel doesn't say she has an errand");
            Assert.IsTrue(hud.Goal.HasValue&&Vector3.Distance(hud.Goal.Value,abri)<3,$"the arrow points at {hud.Goal}, and Abri stands at {abri}");
        }
        /// <summary>"The chapter arrived at, the zone stood in, what the arrow must find there, the chart's diamond": a way out to a scene (to:Scene), the talk that begins the
        /// chapter by its node (node:Name), or someone to speak to. A new chapter adds the lines for the hand-off into it.</summary>
        static readonly string[] HandOffs=
        {
            "4 Rootmarket to:XylosNearspace TallowDrift","4 TallowDrift Keeper TallowDrift",
            "5 TallowDrift to:TallowApproach TallowDrift","5 TallowApproach to:ManyHands TallowDrift",
            "6 ManyHands node:SelaHoarfellChart ManyHands","7 ManyHands node:IshenArrives ManyHands",
            "8 OrielSkyfields to:Orrery ManyHands","8 ManyHands node:BrekkScoriaWord ManyHands",
            "10 ManyHands node:IdraOnePlan ManyHands","10 ManyHandsHull to:ManyHands ManyHands",
        };
        [UnityTest] public IEnumerator BetweenChaptersThePanelSaysTheToldChaptersLastWordAndItsArrowLeadsToTheNext([ValueSource(nameof(HandOffs))] string hand)
        {
            // The first player run finished chapter 4 on a bridge in Driftreed reading "Many Hands is waiting", with no arrow and nothing on the chart: a chart ride, a lift
            // and a launch from where chapter 5 starts (November 14). The told chapter now says where the next begins (QuestDef.next).
            var parts=hand.Split(' ');int chapter=int.Parse(parts[0]);string zone=parts[1],find=parts[2],stop=parts[3];
            yield return null;
            GameServices.Current.NewGame();DevLoadout.Apply("chapter"+chapter);
            Assert.IsNull(StoryQuests.Open(State,out _),"fixture: a story quest is already open on arriving at chapter "+chapter);
            var told=StoryQuests.Told(State);Assert.IsNotNull(told,"fixture: no chapter is told");Assert.AreEqual(chapter-1,told.chapter,"fixture: the chapter told");
            yield return Zone(zone);
            Assert.IsNull(StoryQuests.Open(State,out _),$"arriving in {zone} began chapter {chapter} by itself: this case has to stand where it doesn't");
            var hud=Object.FindFirstObjectByType<ObjectiveHud>();
            Assert.AreEqual(told.closingLine,Objective().Trim(),"between chapters the panel doesn't say the told chapter's last word");
            var wanted=InteractionPrompt.Active.Where(p=>find.StartsWith("to:")?p is Lattice.World.DockingPad pad&&pad.scene==find.Substring(3)||p is Lattice.World.WarpBeacon warp&&warp.scene==find.Substring(3)
                :find.StartsWith("node:")?p is Lattice.World.DiscoveryPoint point&&point.dialogueNode==find.Substring(5)
                :p is Lattice.World.Npc npc&&npc.speaker==find).Select(p=>p.transform.position).ToArray();
            Assert.IsNotEmpty(wanted,$"{zone} offers no {find} with chapter {chapter-1} told: chapter {chapter} can't begin from here");
            Assert.IsTrue(hud.Goal.HasValue,$"'{told.closingLine}' and no arrow, in {zone}");
            float Plan(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
            Assert.IsTrue(wanted.Any(at=>Plan(at,hud.Goal.Value)<3),$"the arrow points at {hud.Goal}, and {find} is at {string.Join(", ",wanted)}");
            var diamond=RouteChart.Objective(State);
            Assert.AreEqual(stop,diamond!=null?diamond.id:"(none)","the chart's diamond between chapters");
        }
    }
}
