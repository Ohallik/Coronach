using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Saves older builds wrote, read the way the game reads them: a file on disk, through SaveSystem. Until November 6 a later flag could play a speaker's later words as the first
    /// talk and still mark them met, so a save can hold Neve met with her cache never opened, or Hal met with his starting materials never given, and neither first meeting plays again. A save
    /// from before pell.aboard existed can hold the homecoming with Pell still packing in Lanternwick. The checks with no "Guard" in their comment fail on the build they were written against.
    /// The guards pass on it on purpose: they say what the repair must not do, which is pay twice, pay for what's still to come, or send her away early.</summary>
    public sealed class QaOldSaveTests
    {
        string heard;
        readonly List<string> folders=new();
        void Listen(string node,string speaker,System.Action<bool> done)=>heard=node;
        static readonly string[] Slots={"autosave","slot1","slot2","slot3"};

        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            ClearSaves();DevLoadout.Apply("starter");heard=null;Npc.TalkRequested+=Listen;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Npc.TalkRequested-=Listen;
            var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            if(GameServices.Current!=null){Saves.ResumeFrom("autosave");ClearSaves();}
            foreach(string folder in folders)if(Directory.Exists(folder))Directory.Delete(folder,true);
            folders.Clear();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static void ClearSaves()
        {
            // The batch runner's save shield holds the player's real saves elsewhere for the whole run.
            if(!Application.isBatchMode)return;
            foreach(var slot in Slots)foreach(var suffix in new[]{"",".bak",".tmp"})
            {var path=Saves.PathForSlot(slot)+suffix;if(File.Exists(path))File.Delete(path);}
            Saves.ResumeFrom("autosave");
        }

        // ------------------------------------------------------------------ the saves
        const string PastTheKey="'met.Orrin':true,'met.Survivor':true,'bossdown.Burrower':true,'clear.Sorrel_Burrower':true,'warpkey':true,'quest.Sorrel.complete':true";
        const string ChartGiven=PastTheKey+",'hushwell.nursery':true,'bossdown.BellowsBelow':true,'bossdown.Cantor':true,'met.Keeper':true,'sliceComplete':true,'tallow.lineObserved':true,'tallow.driftDiscussed':true,'compact.hailHeard':true,"+
            "'lantern.routeKnown':true,'met.Aumi':true,'met.Pell':true,'gorge.relaysLit':true,'relay.hessaConfronted':true,'bossdown.HessaKorl':true,'clear.Relay_Hessa':true,'relay.hessaYielded':true,'lanternwick.chartGiven':true,'chapter2.complete':true,'quest.LanternRoad.complete':true";
        const string Road="'Sorrel':4,'LanternRoad':6";
        /// <summary>A save as the first week of November wrote it: no skill loadouts, no mastery, no parts in Gear. Its quotes are swapped so it reads here. It holds 40 Scrap Alloy and one Lattice Filament.</summary>
        static string OldSave(string zone,string flags,string materials="{'ScrapAlloy':40,'LatticeFilament':1,'WarpKey':1}",string quests="'Sorrel':4")=>
            ("{'version':2,'savedAtUtc':'2026-11-04T22:41:09.5120000Z','zone':'"+zone+"','spawn':'Arrival','activeMember':0,'quickItem':'RepairGel',"+
            "'party':[{'id':'Taren','level':6,'xp':0,'integrity':210.0,'charge':0.0,'gear':[],'equipped':{},'module2':null},{'id':'Sela','level':6,'xp':0,'integrity':210.0,'charge':0.0,'gear':[],'equipped':{},'module2':null}],"+
            "'flags':{"+flags+"},'materials':"+materials+",'collectedMaterials':"+materials+",'consumables':{'RepairGel':2},'craftingXp':{},'questSteps':{"+quests+"},'questCounts':{},'parts':[],'scrip':640,'playtime':3120.5}").Replace('\'','"');
        /// <summary>A save system of its own in a folder of its own, with this save in slot 1. The game's own slots aren't involved.</summary>
        SaveSystem Holding(string json)
        {
            string folder=Path.Combine(Application.temporaryCachePath,"qa-old-saves-"+System.Guid.NewGuid().ToString("N"));folders.Add(folder);
            var saves=new SaveSystem(folder);Directory.CreateDirectory(folder);File.WriteAllText(saves.PathForSlot("slot1"),json);return saves;
        }
        /// <summary>What the title's picker does with the save it's told to continue.</summary>
        static void Play(GameState saved){GameServices.Current.State=saved;GameServices.ArmAnEmptyParty(saved);}
        static bool On(GameState state,string flag)=>state.flags.TryGetValue(flag,out bool value)&&value;
        static int Held(GameState state,string id)=>state.materials.TryGetValue(id,out int count)?count:0;
        static int Gathered(GameState state,string id)=>state.collectedMaterials.TryGetValue(id,out int count)?count:0;

        // ------------------------------------------------------------------ the world
        static GameState State=>GameServices.Current.State;
        static SaveSystem Saves=>GameServices.Current.Saves;
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static Npc Person(string speaker)=>All<Npc>().Single(n=>n.speaker==speaker);
        static SalvageField NevesCache()=>All<SalvageField>().Single(c=>c.cacheId=="Neve");
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Until(System.Func<bool> done,float seconds){float end=Time.unscaledTime+seconds;while(!done()&&Time.unscaledTime<end)yield return null;}
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone,spawn);deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            DialogueSystem.Current.AutoAdvance=false;
            yield return new WaitForSecondsRealtime(.35f);
        }
        static IEnumerator StopTalking()
        {
            DialogueSystem.Current.Cancel();
            yield return Until(()=>!DialogueSystem.Current.Running,3);
            yield return null;
        }
        static Button Row(string name)=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name);
        static IEnumerator ToTitle()
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone("Title");deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"never reached the title");
            yield return null;yield return null;
        }
        static IEnumerator Pick(string slot,string zone)
        {
            var open=Row("Continue");Assert.IsNotNull(open,"the title has no Continue");Assert.IsTrue(open.interactable,"Continue is greyed out with a save on disk");
            open.onClick.Invoke();yield return null;yield return null;
            var row=Row(slot);Assert.IsNotNull(row,"the picker doesn't list "+slot);Assert.IsTrue(row.interactable,slot+" can't be chosen");
            row.onClick.Invoke();float deadline=Time.unscaledTime+25;
            while((SceneFlow.Current.Loading||SceneFlow.Current.Zone!=zone)&&Time.unscaledTime<deadline)yield return null;
            Assert.AreEqual(zone,SceneFlow.Current.Zone,"Continue landed in the wrong zone");Assert.IsFalse(SceneFlow.Current.Loading);
            yield return new WaitForSecondsRealtime(.35f);
        }
        // The script as source, as QaPlanWorldTests reads it.
        static string ScriptText()=>string.Concat(Directory.GetFiles(Path.Combine(Application.dataPath,"_Project/Resources/Dialogue"),"*.yarn").OrderBy(f=>f).Select(File.ReadAllText)).Replace("\r\n","\n");
        static string Body(string node)
        {
            string all=ScriptText();int at=all.IndexOf("title: "+node+"\n",System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(at,0,"the script has no "+node);
            return all.Substring(at,all.IndexOf("\n===",at,System.StringComparison.Ordinal)-at);
        }
        static int Writers(string flag)=>Regex.Matches(ScriptText(),@"<<setFlag\s+"+Regex.Escape(flag)+@"\s*>>").Count;

        // ------------------------------------------------------------------ scri41: Neve's cache
        /// <summary>The finding's path: she was first hailed after the key, played a later line and was marked met. NeveFirst never plays again, and the crate in the Gullet stayed shut for good: no prompt, and contact gave nothing.</summary>
        [UnityTest] public IEnumerator ASaveThatMetNeveOnALaterLineHasHerCacheOpen()
        {
            var saves=Holding(OldSave("Gullet_Tunnel",PastTheKey+",'met.Neve':true"));
            var saved=saves.Load("slot1");
            Assert.IsTrue(On(saved,"neve.cache"),"Neve was met on a later line, her first meeting never plays again, and the Gullet's cache stays shut for good");
            Play(saved);yield return Zone("Gullet_Tunnel");
            var cache=NevesCache();
            Assert.IsTrue(cache.Available,"the crate has no prompt on a save that's owed it");
            int alloy=Held(State,"ScrapAlloy"),resin=Held(State,"ChoirResin"),gels=State.consumables["RepairGel"];
            cache.Interact();
            Assert.IsTrue(Has("cache.Neve"),"the crate gave nothing");
            Assert.AreEqual(alloy+12,Held(State,"ScrapAlloy"),"the crate didn't pay its Scrap Alloy");Assert.AreEqual(resin+4,Held(State,"ChoirResin"),"the crate didn't pay its Choir Resin");Assert.AreEqual(gels+3,State.consumables["RepairGel"],"the crate didn't pay its gels");
            // Taken is taken, however often it's reloaded.
            saves.Save("slot1",State);var back=saves.Load("slot1");
            Assert.IsTrue(On(back,"cache.Neve"),"the taken crate came back");Assert.AreEqual(alloy+12,Held(back,"ScrapAlloy"),"the repair paid something of its own for Neve");
        }
        /// <summary>Guard. A party that has never spoken to her still has her first meeting to come, and that's what opens it.</summary>
        [UnityTest] public IEnumerator HerCacheWaitsForAPartyThatNeverMetHer()
        {
            var saved=Holding(OldSave("Gullet_Tunnel",PastTheKey)).Load("slot1");
            Assert.IsFalse(On(saved,"neve.cache"),"the cache opened for a party she has never spoken to");
            Play(saved);yield return Zone("Gullet_Tunnel");
            Assert.IsFalse(NevesCache().Available,"the crate offers itself before she has said it's there");
        }
        /// <summary>Guard. A save that took the cache keeps nothing to take.</summary>
        [UnityTest] public IEnumerator ACacheAlreadyTakenIsNotOfferedAgain()
        {
            var saved=Holding(OldSave("Gullet_Tunnel",PastTheKey+",'met.Neve':true,'neve.cache':true,'cache.Neve':true")).Load("slot1");
            Assert.AreEqual(40,Held(saved,"ScrapAlloy"),"loading paid something on a save that had everything");
            Play(saved);yield return Zone("Gullet_Tunnel");
            var cache=NevesCache();
            Assert.IsFalse(cache.gameObject.activeInHierarchy,"a taken crate is standing in the chamber again");Assert.IsFalse(cache.Available,"a taken crate offers itself again");
        }

        // ------------------------------------------------------------------ scri41: Hal's starting materials
        /// <summary>The finding's path: Hal's later words played, no ScrapAlloy 12 and no LatticeFilament 3. hal.lesson is the proof: see TheRepairRestsOnWhatTheScriptSays.</summary>
        [UnityTest] public IEnumerator ASaveThatMetHalOnALaterLineGetsHisMaterialsOnce()
        {
            var saves=Holding(OldSave("Hub_Decks",PastTheKey+",'met.Hal':true"));
            string file=saves.PathForSlot("slot1");var bytes=File.ReadAllBytes(file);
            var saved=saves.Load("slot1");
            Assert.AreEqual(52,Held(saved,"ScrapAlloy"),"Hal was met on a later line, his first meeting never plays again, and his Scrap Alloy is lost for good");
            Assert.AreEqual(4,Held(saved,"LatticeFilament"),"his Lattice Filament is lost for good");
            Assert.AreEqual(52,Gathered(saved,"ScrapAlloy"),"a given item counts as gathered, as it would from him");Assert.AreEqual(4,Gathered(saved,"LatticeFilament"),"a given item counts as gathered, as it would from him");
            Assert.IsTrue(On(saved,"hal.lesson"),"nothing records that he's paid, so the next save would pay again");
            // The title reads every slot, and more than once. Every read starts from the file and none of them writes to it.
            Assert.IsTrue(saves.Exists("slot1"),"fixture: the save reads");Assert.IsFalse(saves.Damaged("slot1"),"fixture: the save reads");
            Assert.AreEqual(52,Held(saves.Load("slot1"),"ScrapAlloy"),"reading the slot twice paid twice");
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(file),"reading a save wrote to it");
            // Written back, it's paid: Continue after Continue, and what's been spent stays spent.
            saves.Save("slot1",saved);var again=saves.Load("slot1");
            Assert.AreEqual(52,Held(again,"ScrapAlloy"),"every Continue pays Hal's gift again");Assert.AreEqual(4,Held(again,"LatticeFilament"),"every Continue pays Hal's gift again");
            again.materials["ScrapAlloy"]=2;saves.Save("slot1",again);
            Assert.AreEqual(2,Held(saves.Load("slot1"),"ScrapAlloy"),"spent Scrap Alloy came back on a reload");
            // Talking to him doesn't pay it either: his later words have no gift in them, and the first meeting stays spent.
            Play(saves.Load("slot1"));yield return Zone("Hub_Decks");
            Person("Hal").Interact();
            Assert.AreEqual("HalPost",heard,"the repair played his first meeting over again, with its gift");
            yield return StopTalking();
            Assert.AreEqual(2,Held(State,"ScrapAlloy"),"talking to him paid again");
        }
        /// <summary>The oldest saves come through Migrate first, and get the same.</summary>
        [UnityTest] public IEnumerator AVersionOneSaveThatMetHalLateGetsTheSame()
        {
            yield return null;
            var saved=Holding(OldSave("Hub_Decks",PastTheKey+",'met.Hal':true").Replace("\"version\":2","\"version\":1")).Load("slot1");
            Assert.AreEqual(ChapterProgress.SaveVersion,saved.version,"fixture: the oldest format is brought up to date");
            Assert.AreEqual(52,Held(saved,"ScrapAlloy"),"the oldest saves lose Hal's Scrap Alloy for good");Assert.AreEqual(4,Held(saved,"LatticeFilament"),"the oldest saves lose Hal's Lattice Filament for good");
        }
        /// <summary>A damaged file falls back to the copy beside it, and after the first write back that copy is the old save, from before the repair. It pays from its own numbers, once: what the newer file held doesn't come into it.</summary>
        [UnityTest] public IEnumerator ABackupFromBeforeTheRepairPaysFromItsOwnNumbers()
        {
            yield return null;
            var saves=Holding(OldSave("Hub_Decks",PastTheKey+",'met.Hal':true"));
            var saved=saves.Load("slot1");saved.materials["ScrapAlloy"]=5;saves.Save("slot1",saved);
            Assert.AreEqual(5,Held(saves.Load("slot1"),"ScrapAlloy"),"fixture: the newer file has spent it");
            File.WriteAllText(saves.PathForSlot("slot1"),"{ not a save");
            var back=saves.Load("slot1");Assert.IsNotNull(back,"fixture: the copy beside it reads");
            Assert.AreEqual(52,Held(back,"ScrapAlloy"),"a backup from before the repair doesn't pay, or pays on top of the newer file");
            Assert.IsTrue(On(back,"hal.lesson"),"nothing records that the backup has been paid");
        }
        /// <summary>The same state with no old build in it: marked met when the talk starts, nothing given, and his first meeting never plays again. The next load pays it, once, and his later words have no gift in them.</summary>
        [UnityTest] public IEnumerator ATalkCutOffBeforeHisGiftIsPaidOnTheNextLoad()
        {
            var saves=Holding(OldSave("Hub_Decks",PastTheKey));
            Play(saves.Load("slot1"));yield return Zone("Hub_Decks");
            Person("Hal").Interact();Assert.AreEqual("HalFirst",heard,"fixture: a first meeting comes first");
            yield return Frames(3);yield return StopTalking();
            Assert.IsTrue(Has("met.Hal"),"fixture: he's been met");Assert.IsFalse(Has("hal.lesson"),"fixture: cut off before the gift");Assert.AreEqual(40,Held(State,"ScrapAlloy"),"fixture: cut off before the gift");
            saves.Save("slot1",State);var back=saves.Load("slot1");
            Assert.AreEqual(52,Held(back,"ScrapAlloy"),"a talk cut off before his gift loses it for good");Assert.AreEqual(4,Held(back,"LatticeFilament"),"a talk cut off before his gift loses it for good");
            Play(back);yield return Zone("Hub_Decks");
            Person("Hal").Interact();Assert.AreEqual("HalPost",heard,"his first meeting played over again, with its gift");
            yield return StopTalking();
            Assert.AreEqual(52,Held(State,"ScrapAlloy"),"talking to him paid again");
        }
        /// <summary>Guard. The ordinary save, which heard him out and has since spent it all: nothing comes back, and nothing appears that wasn't there.</summary>
        [UnityTest] public IEnumerator ASaveThatHeardHalIsNeverPaidAgain()
        {
            yield return null;
            var saved=Holding(OldSave("Hub_Decks",PastTheKey+",'met.Hal':true,'hal.lesson':true",materials:"{'ScrapAlloy':3,'WarpKey':1}")).Load("slot1");
            Assert.AreEqual(3,Held(saved,"ScrapAlloy"),"a save that had Hal's gift was paid it again");
            Assert.IsFalse(saved.materials.ContainsKey("LatticeFilament"),"a save that had Hal's gift was paid it again");
            Assert.AreEqual(3,Gathered(saved,"ScrapAlloy"),"a save that had Hal's gift counts it as gathered twice");
        }
        /// <summary>Guard. A party that has never spoken to him gets it from him, in his own words, once: the load pays nothing and the talk pays all of it.</summary>
        [UnityTest] public IEnumerator APartyThatNeverMetHalGetsItFromHimOnce()
        {
            var saves=Holding(OldSave("Hub_Decks",PastTheKey));
            var saved=saves.Load("slot1");
            Assert.AreEqual(40,Held(saved,"ScrapAlloy"),"the load paid for a meeting still to come");Assert.IsFalse(On(saved,"hal.lesson"),"the load spent a meeting still to come");
            Play(saved);yield return Zone("Hub_Decks");
            DialogueSystem.Current.AutoAdvance=true;Person("Hal").Interact();
            Assert.AreEqual("HalFirst",heard,"with the key in hand a first meeting doesn't come first");
            yield return Until(()=>Has("hal.lesson"),15);
            DialogueSystem.Current.AutoAdvance=false;
            Assert.IsTrue(Has("hal.lesson"),"fixture: his first meeting reaches the gift");
            Assert.AreEqual(52,Held(State,"ScrapAlloy"),"fixture: he gives it");Assert.AreEqual(4,Held(State,"LatticeFilament"),"fixture: he gives it");
            yield return StopTalking();
            saves.Save("slot1",State);var back=saves.Load("slot1");
            Assert.AreEqual(52,Held(back,"ScrapAlloy"),"a save written after he paid is paid again on load");Assert.AreEqual(4,Held(back,"LatticeFilament"),"a save written after he paid is paid again on load");
        }
        /// <summary>The repair names these items and amounts itself, since Core can't read the script, and its proof is a fact about the script. If either changes in the script, this says so.</summary>
        [UnityTest] public IEnumerator TheRepairRestsOnWhatTheScriptSays()
        {
            yield return null;
            string hal=Body("HalFirst");
            var gift=Regex.Matches(hal,@"<<giveItem\s+(\w+)\s+(\d+)\s*>>").Cast<Match>().ToDictionary(m=>m.Groups[1].Value,m=>int.Parse(m.Groups[2].Value));
            Assert.IsNotEmpty(gift,"fixture: Hal's first meeting gives something");
            var saved=Holding(OldSave("Hub_Decks","'met.Hal':true",materials:"{}")).Load("slot1");
            CollectionAssert.AreEquivalent(gift,saved.materials,"the repair and Hal's first meeting give different things");
            CollectionAssert.AreEquivalent(gift,saved.collectedMaterials,"the repair and Hal's first meeting give different things");
            // Met without this flag proves he never gave it only while nothing can stand between the gift and the flag, and nothing else sets the flag.
            Assert.IsTrue(Regex.IsMatch(hal,@"(<<giveItem[^\n]*>>\n)+<<setFlag hal\.lesson>>"),"something now stands between Hal's gift and hal.lesson: a save could hold one without the other, and the repair would pay it twice");
            Assert.IsFalse(Regex.IsMatch(hal.Substring(0,hal.IndexOf("<<giveItem",System.StringComparison.Ordinal)),@"(?m)^\s*->"),"a choice comes before Hal's gift: a talk could pass him by without it, and nothing here would know");
            Assert.AreEqual(1,Writers("hal.lesson"),"hal.lesson has another writer: it no longer proves his gift was given");
            StringAssert.Contains("<<setFlag neve.cache>>",Body("NeveFirst"),"her first meeting no longer opens the cache");
            Assert.AreEqual(1,Writers("neve.cache"),"neve.cache has another writer: check the repair still means what it says");
        }

        // ------------------------------------------------------------------ worl61: Pell
        /// <summary>The finding's path: a save written before pell.aboard existed, made after AbriLanternHome, then Lanternwick without docking at Tallow first.</summary>
        [UnityTest] public IEnumerator PellHasLeftLanternwickOnASaveFromAfterTheHomecoming()
        {
            var saved=Holding(OldSave("TallowApproach",ChartGiven+",'chapter2.homecoming':true,'pell.lampFound':true",quests:Road+",'PellsLamp':1")).Load("slot1");
            Assert.IsTrue(On(saved,"pell.aboard"),"Abri has said Pell's over at Tallow, and the save still has her in Lanternwick");
            Play(saved);yield return Zone("Lanternwick");
            Assert.IsFalse(Person("Pell").gameObject.activeInHierarchy,"she's on the square saying she's packed four times, after the homecoming");
            Assert.IsFalse(All<Transform>().Single(t=>t.name=="Pell's lamp stand").gameObject.activeInHierarchy,"her lamp stand stays on the stair after she has taken it to Tallow");
            yield return Zone("TallowDrift");
            var pell=Person("Pell");
            Assert.IsTrue(pell.gameObject.activeInHierarchy,"she's on Tallow when Abri says so, and isn't");
            pell.Interact();Assert.AreEqual("PellLampReturn",heard,"her lamp can't be handed back to her on Tallow");
        }
        /// <summary>Guard. The chart alone isn't the homecoming: she's still packing until the party reaches Tallow, and reaching it brings her aboard as it always has.</summary>
        [UnityTest] public IEnumerator AChartInHandWithNoHomecomingLeavesHerPackingUntilTallow()
        {
            var saved=Holding(OldSave("Lanternwick",ChartGiven+",'pell.lampReturned':true,'quest.PellsLamp.complete':true",quests:Road+",'PellsLamp':2")).Load("slot1");
            Assert.IsFalse(On(saved,"pell.aboard"),"she has left before the party has");
            Play(saved);yield return Zone("Lanternwick");
            var pell=Person("Pell");Assert.IsTrue(pell.gameObject.activeInHierarchy,"she's gone from the square before the party has reached Tallow");
            pell.Interact();Assert.AreEqual("PellPost",heard,"fixture: she's packing");yield return StopTalking();
            yield return Zone("TallowDrift");yield return Frames(2);
            Assert.IsTrue(Has("pell.aboard"),"docking at Tallow with the chart doesn't bring her aboard");
            yield return Zone("Lanternwick");
            Assert.IsFalse(Person("Pell").gameObject.activeInHierarchy,"she's in two places");
        }

        // ------------------------------------------------------------------ both, the way a player meets them
        /// <summary>All of it at once, through the title: the rows read the file, Continue loads it, and a real save writes it back.</summary>
        [UnityTest] public IEnumerator AnOldSaveContinuedFromTheTitleIsPutRightOnce()
        {
            if(!Application.isBatchMode)Assert.Ignore("This writes to the game's own slots, so it runs only under the batch runner's save shield.");
            yield return Zone("Arena_Ground");
            string file=Saves.PathForSlot("slot1");Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file,OldSave("TallowApproach",ChartGiven+",'chapter2.homecoming':true,'met.Neve':true,'met.Hal':true",quests:Road));
            var bytes=File.ReadAllBytes(file);
            yield return ToTitle();
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(file),"the title's own reading wrote to the save");
            yield return Pick("slot1","TallowApproach");
            Assert.IsTrue(Has("neve.cache"),"Continue doesn't open Neve's cache");Assert.IsTrue(Has("pell.aboard"),"Continue leaves Pell in Lanternwick");
            Assert.AreEqual(52,Held(State,"ScrapAlloy"),"Continue doesn't give Hal's Scrap Alloy");Assert.AreEqual(4,Held(State,"LatticeFilament"),"Continue doesn't give Hal's Lattice Filament");
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(file),"continuing wrote to the slot before the player did anything");
            Saves.Save("slot1",State);
            yield return ToTitle();yield return Pick("slot1","TallowApproach");
            Assert.AreEqual(52,Held(State,"ScrapAlloy"),"a second Continue pays Hal's gift again");Assert.AreEqual(4,Held(State,"LatticeFilament"),"a second Continue pays Hal's gift again");
        }
    }
}
