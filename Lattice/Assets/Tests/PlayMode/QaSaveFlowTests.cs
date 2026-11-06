using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The November QA pass: a save must survive a stray press, a defeat must return somewhere playable, and the
    /// game must say what it just did. Every check here failed on the build the pass started from.</summary>
    public sealed class QaSaveFlowTests
    {
        static SaveSystem Saves=>GameServices.Current.Saves;
        static GameState State=>GameServices.Current.State;
        static readonly string[] Slots={"autosave","slot1","slot2","slot3"};
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            Clear();
            DevLoadout.Apply("starter");yield return Load("Arena_Ground");
            Quiet();
        }
        static void Clear()
        {
            // The batch runner's save shield holds the player's real saves elsewhere for the whole run.
            if(!Application.isBatchMode)return;
            foreach(var slot in Slots)foreach(var suffix in new[]{"",".bak",".tmp"})
            {var path=Saves.PathForSlot(slot)+suffix;if(File.Exists(path))File.Delete(path);}
            Saves.ResumeFrom("autosave");
        }
        static IEnumerator Load(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);
            float deadline=Time.unscaledTime+15;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture load timed out");
            yield return null;
        }
        static void Quiet()
        {
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            if(PartyController.Current==null)return;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Saves.ResumeFrom("autosave");Clear();GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static Button Find(string name)=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name);
        static IEnumerator ToTitle()
        {
            SceneFlow.Current.LoadZone("Title");
            float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            yield return null;yield return null;
        }
        static void Hurt(CombatActor hero,float amount)=>hero.Health.Receive(new DamagePacket{amount=amount,type=DamageType.Pulse});

        // ------------------------------------------------------------------ title
        /// <summary>A habitual press at the title must not lose a campaign.</summary>
        [UnityTest] public IEnumerator TheTitleFocusesContinueWhenASaveExists()
        {
            Saves.Save("autosave",State);yield return ToTitle();
            Assert.AreEqual("Continue",EventSystem.current.currentSelectedGameObject.name,"focus sits on New Game with a save on disk");
        }
        [UnityTest] public IEnumerator NewGameAsksBeforeReplacingAnAutosave()
        {
            State.flags["qa.marker"]=true;Saves.Save("autosave",State);
            var before=File.ReadAllBytes(Saves.PathForSlot("autosave"));
            yield return ToTitle();
            Find("New Game").onClick.Invoke();yield return null;yield return null;
            var yes=Find("ConfirmNewGame");Assert.IsNotNull(yes,"New Game started with no question");
            Assert.IsNotNull(Find("KeepSave"));Assert.AreEqual("KeepSave",EventSystem.current.currentSelectedGameObject.name,"the question must default to keeping the save");
            CollectionAssert.AreEqual(before,File.ReadAllBytes(Saves.PathForSlot("autosave")),"asking must not touch the save");
            Assert.IsFalse(SceneFlow.Current.Loading);
            Find("KeepSave").onClick.Invoke();yield return null;yield return null;
            Assert.IsNull(Find("ConfirmNewGame"));Assert.IsNotNull(Find("New Game"),"Back returns to the menu");
            CollectionAssert.AreEqual(before,File.ReadAllBytes(Saves.PathForSlot("autosave")));
        }
        /// <summary>The title is live during its fade-in, when a load is refused: the old code hid the menu anyway.</summary>
        [UnityTest] public IEnumerator ConfirmingDuringTheFadeInDoesNotBlackOutTheTitle()
        {
            SceneFlow.Current.LoadZone("Title");
            float deadline=Time.unscaledTime+15;
            while(Object.FindFirstObjectByType<TitleScreen>()==null&&Time.unscaledTime<deadline)yield return null;
            yield return null;
            Assert.IsTrue(SceneFlow.Current.Loading,"fixture: still in the fade-in");
            var title=Object.FindFirstObjectByType<TitleScreen>();var state=State;
            title.StartGame(false);
            Assert.IsTrue(title.GetComponentInChildren<Canvas>(false)!=null,"the menu vanished and nothing is loading");
            Assert.AreSame(state,State,"a refused start must not replace the game");
            while(SceneFlow.Current.Loading)yield return null;
            Assert.IsTrue(SceneManager.GetSceneByName("Title").isLoaded);
        }
        [UnityTest] public IEnumerator LoadingASlotLeavesTheAutosaveAloneAndRetryReturnsThere()
        {
            var main=JsonCopy(State);main.flags["qa.autosave"]=true;Saves.Save("autosave",main);
            var slot=JsonCopy(State);slot.flags["qa.slot"]=true;slot.zone="TallowDrift";slot.spawn="Arrival";Saves.Save("slot1",slot);
            yield return ToTitle();
            Find("Continue").onClick.Invoke();yield return null;yield return null;
            Find("slot1").onClick.Invoke();
            float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||SceneFlow.Current.Zone!="TallowDrift")&&Time.unscaledTime<deadline)yield return null;
            yield return null;yield return null;
            var kept=Saves.Load("autosave");
            Assert.IsTrue(kept.flags.ContainsKey("qa.autosave"),"loading an older slot replaced the newest autosave");
            Assert.AreEqual("slot1",Saves.RetrySlot,"a defeat must return to the slot in play, not another campaign");
            Saves.Save("autosave",State);Assert.AreEqual("autosave",Saves.RetrySlot,"the next real autosave takes over");
        }
        static GameState JsonCopy(GameState state){var copier=new SaveSystem(TempDirectory());copier.Save("slot3",state);return copier.Load("slot3");}

        // ------------------------------------------------------------------ the save file
        static string TempDirectory()=>Path.Combine(Application.temporaryCachePath,"qa-saves-"+System.Guid.NewGuid().ToString("N"));
        static GameState Sample(int scrip){var state=new GameState{scrip=scrip};state.party.Clear();state.party.Add(new MemberState{id="Taren",level=3,integrity=100});return state;}
        [Test] public void ADamagedSaveFallsBackToItsBackup()
        {
            var saves=new SaveSystem(TempDirectory());
            saves.Save("autosave",Sample(11));saves.Save("autosave",Sample(22));
            File.WriteAllText(saves.PathForSlot("autosave"),"");
            Assert.IsNotNull(saves.Load("autosave"),"a good backup sits beside a damaged save");
            Assert.AreEqual(11,saves.Load("autosave").scrip);
            File.Delete(saves.PathForSlot("autosave"));
            Assert.AreEqual(11,saves.Load("autosave")?.scrip,"a missing save with a backup beside it is not empty");
        }
        [Test] public void AnAutosaveOfADeadPartyIsRefused()
        {
            var saves=new SaveSystem(TempDirectory());saves.Save("autosave",Sample(5));
            var dead=Sample(9);dead.party[0].integrity=0;saves.Save("autosave",dead);
            Assert.AreEqual(5,saves.Load("autosave").scrip,"a save with nobody standing is an endless defeat loop");
        }
        [Test] public void ASaveWithEveryoneDownLoadsOnItsFeet()
        {
            var saves=new SaveSystem(TempDirectory());var dead=Sample(9);dead.party[0].integrity=0;
            saves.Save("slot1",dead);
            Assert.Greater(saves.Load("slot1").party[0].integrity,0,"an old save with nobody standing must not spawn a dead party");
        }
        [Test] public void LootedConsumablesInOldSavesMoveToTheBag()
        {
            var saves=new SaveSystem(TempDirectory());var old=Sample(1);old.materials["RepairGel"]=2;old.materials["ScrapAlloy"]=4;old.consumables["RepairGel"]=1;
            saves.Save("slot1",old);var loaded=saves.Load("slot1");
            Assert.AreEqual(3,loaded.consumables["RepairGel"],"gels stranded in materials from older builds");
            Assert.IsFalse(loaded.materials.ContainsKey("RepairGel"));Assert.AreEqual(4,loaded.materials["ScrapAlloy"]);
        }

        // ------------------------------------------------------------------ defeat
        [UnityTest] public IEnumerator RetryRestoresThePartyToFull()
        {
            var party=PartyController.Current.members;
            var saved=JsonCopy(State);saved.scrip=777;saved.zone="Arena_Ground";saved.spawn="Arrival";
            saved.party[0].integrity=12;saved.party[1].integrity=0;Saves.Save("autosave",saved);
            foreach(var hero in party)Hurt(hero,100000);
            var panel=Object.FindFirstObjectByType<DefeatPanel>();
            float deadline=Time.unscaledTime+6;while(!panel.IsOpen&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(panel.IsOpen,"no defeat panel");
            Find("Retry").onClick.Invoke();
            deadline=Time.unscaledTime+15;
            yield return null;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            yield return null;yield return null;
            Assert.AreEqual(777,State.scrip,"fixture: the autosave came back");
            foreach(var hero in PartyController.Current.members)
                Assert.AreEqual(hero.Health.maximum,hero.Health.integrity,1,hero.character+" came back wounded: retrying at 12 integrity beside the same fight is a loop");
        }
        [UnityTest] public IEnumerator DefeatEndsARunningConversation()
        {
            var party=PartyController.Current.members;
            Assert.IsTrue(DialogueSystem.Current.StartNode("PellRepeat","Pell"),"fixture: conversation started");yield return null;
            foreach(var hero in party)hero.Health.integrity=0;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsFalse(DialogueSystem.Current.Running,"the conversation keeps running under LINK LOST, and can finish into an autosave");
        }
        [UnityTest] public IEnumerator NoOneIsHurtDuringAConversation()
        {
            var hero=PartyController.Current.Active;float before=hero.Health.integrity;
            GameServices.Current.Input.Blocked=true;yield return null;
            Assert.AreEqual(0,hero.Health.Receive(new DamagePacket{amount=50,type=DamageType.Pulse}),"a shot already in the air struck during a conversation");
            Assert.AreEqual(before,hero.Health.integrity);
            GameServices.Current.Input.Blocked=false;
        }
        [UnityTest] public IEnumerator ShotsWaitOutAConversation()
        {
            var source=new GameObject("Shooter").AddComponent<Health>();source.friendly=false;
            GameServices.Current.Input.Blocked=true;
            Projectile.Fire(new Vector3(40,1,0),Vector3.right,new DamagePacket{source=source,amount=1,type=DamageType.Beam},10);
            var shot=Projectile.Live.Last();var from=shot.transform.position;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(from,shot.transform.position,"shots fly on while enemies stand frozen");
            GameServices.Current.Input.Blocked=false;Object.Destroy(source.gameObject);
        }

        // ------------------------------------------------------------------ encounters
        [UnityTest] public IEnumerator ClearingAnEncounterAutosavesItsRewards()
        {
            var root=new GameObject("QA encounter");root.SetActive(false);
            var volume=root.AddComponent<EncounterVolume>();volume.encounterId="QaClear";volume.membranes=System.Array.Empty<Membrane>();
            var spawner=new GameObject("Spawn").AddComponent<Spawner>();spawner.transform.position=new Vector3(30,0,30);spawner.definition=GameCatalog.Find<EnemyDef>("Scrapmite");spawner.count=1;spawner.radius=0;
            volume.spawners=new[]{spawner};root.SetActive(true);yield return null;
            int xp=State.party[0].xp+State.party[0].level*100000;
            volume.Begin();yield return null;yield return null;
            var enemy=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).First(e=>e.definition==spawner.definition);enemy.Passive=true;
            enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=100000,type=DamageType.Pulse});
            yield return null;yield return null;yield return null;
            var saved=Saves.Load("autosave");Assert.IsNotNull(saved,"a cleared fight wrote no save: a defeat replays it");
            Assert.IsTrue(saved.flags.ContainsKey("clear.QaClear"),"the clear itself was not saved");
            Assert.Greater(saved.party[0].xp+saved.party[0].level*100000,xp,"the save missed the kill's rewards");
        }
        [UnityTest] public IEnumerator ASavedClearOpensItsSealsOnLoad()
        {
            State.flags["bossdown.Burrower"]=true;State.flags["clear.Sorrel_Burrower"]=true;State.flags["warpkey"]=true;
            State.party[1].integrity=0;
            yield return Load("Sorrel_Ridges","Hushwell");
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return null;yield return null;
            var seal=Object.FindObjectsByType<Membrane>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(m=>m.name=="Drill cradle seal");
            Assert.IsNotNull(seal,"fixture: the seal exists");
            Assert.IsTrue(seal.Open,"a cleared arena's exit stayed shut across the way the HUD points");
        }

        // ------------------------------------------------------------------ what you get
        [UnityTest] public IEnumerator EverythingGoesWhereItsKindLives()
        {
            var state=State;state.consumables.TryGetValue("RepairGel",out int gels);int parts=state.parts.Count;
            RpgServices.Inventory.Grant("RepairGel",2);RpgServices.Inventory.Grant("EdgesT2",1);RpgServices.Inventory.Grant("ScrapAlloy",3);
            Assert.AreEqual(gels+2,state.consumables["RepairGel"],"a gel landed where it can't be used");
            Assert.IsFalse(state.materials.ContainsKey("RepairGel"));Assert.AreEqual(parts+1,state.parts.Count,"a part landed in materials");
            Assert.IsFalse(state.materials.ContainsKey("EdgesT2"));Assert.GreaterOrEqual(state.materials["ScrapAlloy"],3);
            yield return null;
        }
        [UnityTest] public IEnumerator LootedGelsReachTheBag()
        {
            var table=ScriptableObject.CreateInstance<LootTable>();table.entries=new[]{new LootEntry{id="RepairGel",count=2,chance=1}};
            var def=Object.Instantiate(GameCatalog.Find<EnemyDef>("Scrapmite"));def.id="Scrapmite";def.lootTable=table;
            var enemy=ActorFactory.Enemy(def,new Vector3(30,0,30));enemy.Passive=true;yield return null;yield return null;
            State.consumables.TryGetValue("RepairGel",out int gels);
            enemy.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=100000,type=DamageType.Pulse});yield return null;
            Assert.AreEqual(gels+2,State.consumables["RepairGel"],"Hessa's guaranteed gels never reach the bag");
            Assert.IsFalse(State.materials.ContainsKey("RepairGel"));
        }
        [UnityTest] public IEnumerator ALateTalkCatchesUpWithDeedsAlreadyDone()
        {
            var state=State;state.questSteps.Clear();state.questCounts.Clear();int scrip=state.scrip;
            RpgServices.Quests.Start("Sorrel");RpgServices.Quests.Report(ObjectiveKind.Reach,"Sorrel_Ridges");
            // Walk past Dace: the boss and the key come first.
            state.flags["bossdown.Burrower"]=true;RpgServices.Quests.Report(ObjectiveKind.Kill,"Burrower");
            RpgServices.Inventory.Give("WarpKey",1);RpgServices.Quests.Report(ObjectiveKind.Collect,"WarpKey");
            RpgServices.Quests.Report(ObjectiveKind.TalkTo,"Survivor");
            var quest=GameCatalog.Find<QuestDef>("Sorrel");
            Assert.AreEqual(quest.steps.Length,state.questSteps["Sorrel"],"stuck forever on Stop the Burrower: it never respawns");
            Assert.AreEqual(scrip+quest.rewardScrip,state.scrip,"reward paid once");
            RpgServices.Quests.Report(ObjectiveKind.TalkTo,"Survivor");Assert.AreEqual(scrip+quest.rewardScrip,state.scrip,"paid twice");
            yield return null;
        }
        [UnityTest] public IEnumerator ASaveAlreadyStuckRepairsItself()
        {
            var state=new GameState();state.party.Add(new MemberState{id="Taren"});state.questSteps["Sorrel"]=2;state.flags["bossdown.Burrower"]=true;state.collectedMaterials["WarpKey"]=1;
            GameServices.Current.State=state;
            RpgServices.Quests.Reconcile();
            Assert.AreEqual(GameCatalog.Find<QuestDef>("Sorrel").steps.Length,state.questSteps["Sorrel"]);
            yield return null;
        }
        [UnityTest] public IEnumerator EveryCrystalCountsOnce()
        {
            var state=State;state.questSteps.Clear();state.questCounts.Clear();state.collectedMaterials.Clear();state.materials.Remove("RidgeCrystal");
            RpgServices.Quests.Start("MiraCrystals");
            RpgServices.Inventory.Give("RidgeCrystal",2);
            Assert.AreEqual(2,state.questCounts["MiraCrystals/0"],"the Bellows' crystals didn't count toward Mira's six");
            var node=new GameObject("Node").AddComponent<MineNode>();node.nodeId="qa";node.Interact();
            Assert.AreEqual(3,state.questCounts["MiraCrystals/0"],"a mined crystal must count exactly once");
            yield return null;
        }
        [UnityTest] public IEnumerator AFirstMeetingAlwaysPlaysFirst()
        {
            var flags=GameServices.Current.Flags;flags.SetBool("qa.post",true);flags.SetBool("met.Pell",false);
            var go=new GameObject("Pell fixture");var npc=go.AddComponent<Npc>();npc.speaker="Pell";npc.firstNode="PellFirst";npc.repeatNode="PellRepeat";npc.postNode="PellPost";npc.postFlag="qa.post";
            string heard=null;void Heard(string node,string speaker,System.Action<bool> done){heard=node;}
            Npc.TalkRequested+=Heard;
            try{npc.Interact();}finally{Npc.TalkRequested-=Heard;}
            Assert.AreEqual("PellFirst",heard,"a later flag skipped the introduction, and with it Hal's lesson and Neve's cache");
            yield return null;
        }

        // ------------------------------------------------------------------ what the game says
        [UnityTest] public IEnumerator ARepairSaysWhatItDidAndWhatItCost()
        {
            var cards=new List<string>();void Card(string heading,string body){cards.Add(heading+"|"+body);}
            Notices.Posted+=Card;
            {
                var state=State;var bay=new GameObject("Bay").AddComponent<RepairBay>();bay.prompt="Bay";bay.saveSpawn="Wherever";
                int fee=state.party[0].level*8;
                state.scrip=fee-1;string spawn=state.spawn;Hurt(PartyController.Current.Active,10);
                yield return null;yield return null; // The party's wounds reach the saved state on its next update.
                bay.Interact();
                Assert.AreEqual(1,cards.Count,"a refused repair said nothing");StringAssert.Contains(fee.ToString(),cards[0]);
                Assert.AreEqual(spawn,state.spawn,"a refused repair moved the checkpoint");
                state.scrip=fee+5;cards.Clear();bay.Interact();
                Assert.AreEqual(5,state.scrip,"fee");Assert.AreEqual(1,cards.Count,"a repair said nothing");StringAssert.Contains(fee.ToString(),cards[0]);
                Assert.AreEqual("Wherever",state.spawn);
                cards.Clear();bay.Interact();
                Assert.AreEqual(5,state.scrip,"nothing to repair, and it charged anyway");Assert.AreEqual(1,cards.Count,"a save said nothing");
            }
            Notices.Posted-=Card;
            yield return null;
        }
        [UnityTest] public IEnumerator NothingRepairsWithAnEnemyAtHand()
        {
            var bay=new GameObject("Bay").AddComponent<RepairBay>();bay.free=true;
            Assert.IsTrue(bay.Available,"fixture: a quiet bay is available");
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Scrapmite"),PartyController.Current.Active.transform.position+Vector3.right*8);enemy.Passive=false;
            yield return null;yield return null;
            Assert.IsFalse(bay.Available,"a free full heal beside a live fight");
            enemy.Passive=true;Object.Destroy(enemy.gameObject);
        }
        [UnityTest] public IEnumerator MinedNodesStayGoneAfterAReload()
        {
            GameServices.Current.Flags.SetBool("mined.qa2",true);
            var go=new GameObject("Node");var node=go.AddComponent<MineNode>();node.nodeId="qa2";yield return null;yield return null;
            Assert.IsFalse(go.activeSelf,"a mined crystal stands whole with no prompt");
        }
        [UnityTest] public IEnumerator CountedStepsAnnounceEachPickup()
        {
            var state=State;var toast=Object.FindFirstObjectByType<QuestToast>();
            state.questSteps.Clear();state.questCounts.Clear();
            yield return new WaitForSecondsRealtime(.3f);
            RpgServices.Quests.Start("IshensRecord");
            float deadline=Time.unscaledTime+7;while(!toast.Body.Contains("(0/3)")&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(4.7f);
            RpgServices.Quests.Report(ObjectiveKind.Interact,"RecordPlate");
            deadline=Time.unscaledTime+7;while(!toast.Body.Contains("(1/3)")&&Time.unscaledTime<deadline)yield return null;
            StringAssert.Contains("(1/3)",toast.Body,"taking a record plate gave no feedback at all");
        }
        [UnityTest] public IEnumerator AConversationThatSetsItsOwnFlagStillAutosaves()
        {
            yield return new WaitUntil(()=>DialogueSystem.Current!=null&&DialogueSystem.Current.Prepared);
            DialogueSystem.Current.AutoAdvance=true;
            var point=new GameObject("Discovery").AddComponent<DiscoveryPoint>();point.flag="relay.hessaConfronted";point.report="";point.situation="";point.dialogueNode="HessaConfront";point.dialogueSpeaker="Hessa";point.previewEncounter=null;point.requiredQuest="";
            yield return null;
            point.Interact();
            float deadline=Time.unscaledTime+40;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            yield return null;
            var saved=Saves.Load("autosave");Assert.IsNotNull(saved,"the confrontation wrote no save, so a defeat replays the whole dungeon");
            Assert.IsTrue(saved.flags.ContainsKey("relay.hessaConfronted"));
        }
        [UnityTest] public IEnumerator PickupsAppearInTheFeed()
        {
            RpgServices.Inventory.Grant("ScrapAlloy",2);yield return null;yield return null;
            var line=Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="PickupLine"&&t.text.Contains("Scrap Alloy"));
            Assert.IsNotNull(line,"nothing tells the player what they picked up");StringAssert.Contains("2",line.text);
        }
    }
}
