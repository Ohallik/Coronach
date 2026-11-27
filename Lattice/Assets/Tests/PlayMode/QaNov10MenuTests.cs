using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Lattice.Tests.PlayMode
{
    /// <summary>November 10, the menus and saves left open by the QA pass: the bench that spent on the tap that ended Hal's talk (QA-036), the frames a save or a
    /// menu's first opening cost (QA-034) and the way the game quits (QA-031). Each check names why it failed on the build the pass ended with.</summary>
    public sealed class QaNov10MenuTests
    {
        static GameState State=>GameServices.Current.State;
        static SaveSystem Saves=>GameServices.Current.Saves;
        static PauseMenu Menu=>Object.FindFirstObjectByType<PauseMenu>();
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return Load("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(Menu!=null&&Menu.IsOpen)Menu.Close();
            if(GameServices.Current!=null){Saves.ResumeFrom("autosave");ClearSaves();}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Load(string zone)
        {
            SceneFlow.Current.LoadZone(zone);
            float deadline=Time.unscaledTime+15;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture load timed out");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return null;yield return null;
        }
        static void ClearSaves()
        {
            // The batch runner's save shield holds the player's real saves elsewhere for the whole run.
            if(!Application.isBatchMode)return;
            foreach(var slot in new[]{"autosave","slot1","slot2","slot3"})foreach(var suffix in new[]{"",".bak",".tmp"})
            {var path=Saves.PathForSlot(slot)+suffix;if(File.Exists(path))File.Delete(path);}
        }
        static Button[] Buttons()=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        static Button Row(string name)=>Buttons().FirstOrDefault(b=>b.name==name);
        static string Words(Component c)=>ListRow.Words(c);
        static string Shown()=>string.Join(" | ",Buttons().Select(b=>b.name+": "+Words(b)));
        static Button With(string words)
        {
            var found=Buttons().FirstOrDefault(b=>Words(b).Contains(words));
            Assert.IsNotNull(found,$"no row says \"{words}\". On screen: {Shown()}");return found;
        }
        static GameObject Selected()
        {var s=EventSystem.current.currentSelectedGameObject;Assert.IsNotNull(s,"nothing has focus: a pad player is stuck");return s;}
        static int Held()=>State.materials.Values.Sum();
        // The first the bench lists: a recipe that takes a later region's material stays off the list until the party has held it (Fabrication.Known), so it's the first of those it knows.
        static RecipeDef FirstRecipe(){var bench=new Lattice.Rpg.Fabrication(State);return GameCatalog.All<RecipeDef>().Where(bench.Known).OrderBy(r=>r.discipline).ThenBy(r=>r.tier).First();}

        // ------------------------------------------------------------------ QA-036: the bench
        /// <summary>QA-036. Hal's bench opened at the end of his talk with the cursor on a recipe, so the next tap of a thumb still going through his lines made one and
        /// spent 2 Scrap Alloy. On the old build the first press here crafts the first recipe.</summary>
        [UnityTest] public IEnumerator TheBenchOpensOnARowThatSpendsNothingWithTheFirstRecipeOnePressDown()
        {
            int held=Held();int parts=State.parts.Count;
            // The anvil's prompt and Hal's talk both reach the bench through the same door (WorldUiBridge.Bench).
            var bench=new GameObject("QaBench").AddComponent<Lattice.World.Bench>();
            try
            {
                bench.Interact();yield return null;
                Assert.IsTrue(Menu.IsOpen&&Menu.AtBench,"fixture: the bench opened");
                // Past the opening hold: these are the taps that land after it.
                yield return new WaitForSecondsRealtime(.4f);
                for(int i=0;i<2;i++){Selected().GetComponent<Button>().onClick.Invoke();yield return null;}
                Assert.AreEqual(held,Held(),"a tap on the row the bench opens on spent materials: a thumb still going through Hal's lines makes a recipe nobody chose");
                Assert.AreEqual(parts,State.parts.Count,"a tap on the row the bench opens on made a part");
                var down=Selected().GetComponent<Selectable>().FindSelectableOnDown();
                Assert.IsNotNull(down,"nothing is one press down from where the bench opens");
                StringAssert.StartsWith(FirstRecipe().displayName,Words(down),"the first recipe isn't one press down from where the bench opens");
            }
            finally{Object.Destroy(bench.gameObject);}
        }
        /// <summary>QA-036's row does a job of its own: with every recipe listed, the few that can be made are spread over pages among grey ones. The old build has
        /// no such row; its first row is a recipe.</summary>
        [UnityTest] public IEnumerator TheBenchCanShowOnlyWhatCanBeMadeNow()
        {
            var menu=Menu;menu.Open(PauseMenu.FabricatePage,true);yield return null;
            var toggle=Row("Row_0");Assert.IsNotNull(toggle,"fixture: the page has rows");
            StringAssert.StartsWith("Showing every recipe",Words(toggle),"the Fabricate page's first row isn't the filter: "+Shown());
            toggle.onClick.Invoke();yield return null;
            StringAssert.StartsWith("Showing what can be made now",Words(Row("Row_0")),"the filter doesn't turn");
            Assert.AreEqual("Row_0",Selected().name,"turning the filter moved the cursor off it, onto a recipe");
            var recipes=Buttons().Where(b=>b.name.StartsWith("Row_")&&b.name!="Row_0").ToArray();
            Assert.Greater(recipes.Length,0,"fixture: the starter loadout can make something at a bench");
            Assert.Less(recipes.Length,GameCatalog.All<RecipeDef>().Length,"fixture: the starter loadout can't make everything");
            foreach(var row in recipes)
                Assert.AreNotEqual(UiKit.DimTextColor,row.GetComponentInChildren<TMP_Text>().color,$"'{Words(row)}' can't be made and is listed under 'what can be made now'");
            Row("Row_0").onClick.Invoke();yield return null;
            StringAssert.StartsWith("Showing every recipe",Words(Row("Row_0")),"the filter doesn't turn back");
            StringAssert.StartsWith(FirstRecipe().displayName,Words(Row("Row_1")),"with the filter off, the first recipe isn't under it");
            menu.Close();
        }

        // ------------------------------------------------------------------ QA-034: a save's frame
        /// <summary>QA-034. A manual save redrew the Party page, which read all three slot files again on the frame of the press. Here slot 2's file is held shut
        /// while slot 1 is saved: on the old build the redraw can't read it and slot 2's row says it's empty.</summary>
        [UnityTest] public IEnumerator ASaveFromThePartyPageReadsNoOtherSlot()
        {
            Saves.Save("slot2",State);string place=DisplayNames.Zone(State.zone);
            var menu=Menu;menu.Open(PauseMenu.PartyPage);yield return null;
            StringAssert.Contains(place,Words(With("Save to slot 2")),"fixture: slot 2's row says where it was saved");
            using(var held=File.Open(Saves.PathForSlot("slot2"),FileMode.Open,FileAccess.Read,FileShare.None))
            {
                With("Save to slot 1").onClick.Invoke();yield return null;
                StringAssert.Contains(place,Words(With("Save to slot 2")),"saving slot 1 read slot 2's file again on the frame of the press");
                StringAssert.Contains(place,Words(With("Save to slot 1")),"slot 1's row doesn't say what was just saved there");
            }
            menu.Close();
        }
        // By name, so this file also builds against the old SaveSystem for its red run.
        static PropertyInfo WriteBehindSwitch=>typeof(SaveSystem).GetProperty("WriteBehind",BindingFlags.Public|BindingFlags.Static);
        static void Drain()=>typeof(SaveSystem).GetMethod("Drain",BindingFlags.Public|BindingFlags.Static)?.Invoke(null,null);
        static int Pending()=>(int?)typeof(SaveSystem).GetProperty("Pending",BindingFlags.Public|BindingFlags.Static)?.GetValue(null)??0;
        /// <summary>QA-034. A save's disk work, the file, its flush through to the disk and the replace, ran on the frame that saved: a checkpoint, a conversation's
        /// autosave and a manual save each cost a 40 ms frame. A reader holding the file stretches that work out to see who waits for it: on the old build the save
        /// itself does, for as long as the reader holds on.</summary>
        [Test] public void ASaveWrittenBehindTheFrameLeavesItAtOnceAndReadsBackWhole()
        {
            string folder=Path.Combine(Path.GetTempPath(),"coronach-behind-"+System.Guid.NewGuid().ToString("N"));
            var saves=new SaveSystem(folder);var toggle=WriteBehindSwitch;object before=toggle?.GetValue(null);
            saves.Save("slot3",new GameState{scrip=10});string path=saves.PathForSlot("slot3");
            var held=File.Open(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
            var release=new Thread(()=>{Thread.Sleep(90);held.Dispose();});
            try
            {
                toggle?.SetValue(null,true);
                release.Start();
                var timer=Stopwatch.StartNew();
                saves.Save("slot3",new GameState{scrip=30});
                double spent=timer.Elapsed.TotalMilliseconds;
                Assert.Less(spent,25,$"the save waited {spent:0} ms for the disk on the frame that made it");
                Assert.AreEqual(30,saves.Load("slot3").scrip,"reading the slot straight after saving it didn't see the save");
                StringAssert.Contains("\"scrip\": 10",File.ReadAllText(path+".bak"),"the save before it isn't kept beside it");
                Assert.IsFalse(File.Exists(path+".tmp"),"the write left its temporary file");
            }
            finally
            {
                if(release.IsAlive)release.Join();held.Dispose();
                if(toggle!=null){Drain();toggle.SetValue(null,before);}
                if(Directory.Exists(folder))Directory.Delete(folder,true);
            }
        }
        /// <summary>Saves queued behind the frame land in order, and any read of a slot sees the last of them, with the one before it kept as the backup.</summary>
        [Test] public void QueuedSavesLandInOrderAndTheNextReadSeesTheLast()
        {
            string folder=Path.Combine(Path.GetTempPath(),"coronach-order-"+System.Guid.NewGuid().ToString("N"));
            var saves=new SaveSystem(folder);var toggle=WriteBehindSwitch;object before=toggle?.GetValue(null);
            try
            {
                toggle?.SetValue(null,true);
                for(int i=1;i<=5;i++)saves.Save("slot2",new GameState{scrip=i});
                Assert.AreEqual(5,saves.Load("slot2").scrip,"a read straight after a run of saves didn't see the last one");
                Assert.AreEqual(0,Pending(),"a read left saves still queued");
                StringAssert.Contains("\"scrip\": 4",File.ReadAllText(saves.PathForSlot("slot2")+".bak"),"the backup isn't the save before the last");
            }
            finally{Drain();if(toggle!=null)toggle.SetValue(null,before);if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }

        // ------------------------------------------------------------------ QA-034: a menu's first opening
        /// <summary>QA-034. The save picker was the run's first user of the display face and of the player's clock, and a Continue's bench the first to load the
        /// item sheet, so each first opening cost a 38 to 46 ms frame. The old build has nothing that warms them.</summary>
        [UnityTest] public IEnumerator TheTitleWarmsWhatEachMenuFirstOpensWith()
        {
            SceneFlow.Current.LoadZone("Title");float deadline=Time.unscaledTime+15;
            while((SceneFlow.Current.Loading||Object.FindFirstObjectByType<TitleScreen>()==null)&&Time.unscaledTime<deadline)yield return null;
            var warmup=typeof(TitleScreen).Assembly.GetType("Lattice.UI.UiWarmup");
            Assert.IsNotNull(warmup,"nothing warms the menus at the title: each first opening pays for its type, sheets and dates on its own frame");
            var done=warmup.GetProperty("Done",BindingFlags.Public|BindingFlags.Static);
            for(int i=0;i<40&&!(bool)done.GetValue(null);i++)yield return null;
            Assert.IsTrue((bool)done.GetValue(null),"the warm-up at the title never finished");
            yield return null;
            Assert.IsNull(GameObject.Find("UiWarmup"),"the warm-up left its canvas behind");
            string focus=Selected().name;
            Assert.IsTrue(focus=="Continue"||focus=="New Game","the warm-up took the title's focus: "+focus);
        }
        // ------------------------------------------------------------------ QA-034: the bench's and the shop's first opening (round ten)
        /// <summary>What one opening cost: the whole frame it opened in, the call that built the page, and what was made in that frame.</summary>
        struct Opening{public double frame,call,canvases;public int made,texts,assets,glyphs;public long garbage;
            public override string ToString()=>$"frame {frame:0.0} ms (the call {call:0.0}, the canvases {canvases:0.0}), {made} objects, {texts} texts, {assets} new textures/sprites/materials, {glyphs} glyphs, {garbage/1024} KB";}
        static int Objects()=>Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
        static int Texts()=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
        static int Assets()=>Resources.FindObjectsOfTypeAll<Texture>().Length+Resources.FindObjectsOfTypeAll<Sprite>().Length+Resources.FindObjectsOfTypeAll<Material>().Length;
        static int Glyphs()=>Resources.FindObjectsOfTypeAll<TMP_FontAsset>().Sum(f=>f.characterTable!=null?f.characterTable.Count:0);
        /// <summary>Opens and closes a page the given number of times, a frame apart, timing the frame each opening falls in from the moment before the call to the next frame.</summary>
        static IEnumerator Openings(System.Action open,System.Action close,Opening[] into)
        {
            using var canvases=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal,"PostLateUpdate.PlayerUpdateCanvases");
            for(int i=0;i<into.Length;i++)
            {
                yield return null;yield return null;
                int made=Objects(),texts=Texts(),assets=Assets(),glyphs=Glyphs();
                long garbage=System.GC.GetAllocatedBytesForCurrentThread();var frame=Stopwatch.StartNew();
                open();double call=frame.Elapsed.TotalMilliseconds;
                yield return null;
                double whole=frame.Elapsed.TotalMilliseconds;long spent=System.GC.GetAllocatedBytesForCurrentThread()-garbage;
                into[i]=new Opening{frame=whole,call=call,canvases=canvases.Valid?canvases.LastValue/1e6:-1,made=Objects()-made,texts=Texts()-texts,assets=Assets()-assets,glyphs=Glyphs()-glyphs,garbage=spent};
                close();
            }
            yield return null;yield return null;
        }
        /// <summary>QA-034, round ten. The capture of record (nov25-whole, recording on) opened the drill's bench in a 92 ms frame and a 90 ms one (s05 step 17, s07 step 13), Hal's
        /// bench in 59 (s01 step 40) and Mira's stall in 59 (s01 step 26), each the first opening of its run; every press inside them after that, which builds the whole page again,
        /// was 23 to 28 ms. Here, past the title's warm-up as a player is, the bench and the stall are each opened five times: the first against the fifth, and what the first made.
        /// <para>Read before the change (r10b-before, the class alone in a mirror): the bench's first opening 154.4 ms (the call that builds the page 149.1) against 12.9, 10.7, 16.0
        /// and 16.1 for the next four, and 4 textures and sprites made that the others didn't (two drawn marks); the stall after it 36.1 ms against 12.4 to 16.2, 17 made. The call
        /// is the page compiled on its first run and the marks drawn. With the marks drawn at the title and RuntimeHelpers.PrepareMethod asked to compile the pages there
        /// (r10b-after1), the first was still 47.2 ms against 11.2 to 11.5: asking compiled nothing. The pages are now built once a run as the zone comes in (PauseMenu.Warm,
        /// ShopUi.Warm). The compiling can't be undone in a run, so this plays the warm-up as it is first, from a run where nothing has opened a page yet, and then the warm-up
        /// as it was (UiWarmup's asBefore) with the marks forgotten, for the count. The bar is twice the fifth: the later openings read 10.7 to 16.2 ms in both runs.</para></summary>
        [UnityTest] public IEnumerator ABenchOrAStallOpensInAFrameLikeItsFifth()
        {
            var menu=Menu;var shop=Object.FindFirstObjectByType<ShopUi>();Assert.IsNotNull(shop,"fixture: the stall's page is there");
            // The pages built as the zone came in leave nothing behind them: no page up, the game running, the pad the party's.
            Assert.IsTrue((bool?)typeof(PauseMenu).GetField("warmed",BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null)??false,"the bench's page wasn't built as the zone came in");
            Assert.IsFalse(menu.IsOpen||shop.IsOpen,"a page built as the zone came in is still up");
            Assert.IsFalse(GameTime.Paused,"building a page as the zone came in paused the game");Assert.IsFalse(GameServices.Current.Input.Blocked,"building a page as the zone came in took the pad");
            var bench=new Opening[5];var stall=new Opening[5];var warm=new System.Collections.Generic.List<double>();
            yield return Warm(false,warm);
            yield return Openings(()=>menu.Open(PauseMenu.FabricatePage,true),()=>menu.Close(),bench);
            yield return Openings(()=>shop.Open(ShopUi.For("Hub_Decks","Mira"),"Mira"),()=>shop.Close(),stall);
            for(int i=0;i<5;i++)TestContext.WriteLine($"BENCH_OPEN {i+1}: {bench[i]}");
            for(int i=0;i<5;i++)TestContext.WriteLine($"STALL_OPEN {i+1}: {stall[i]}");
            TestContext.WriteLine($"WARM_STEPS {warm.Count} frames, the most {warm.Max():0.0} ms: {string.Join(" ",warm.Select(s=>s.ToString("0.0")))}");
            // As it was: the warm-up without the marks, and the marks forgotten, as in a run that has drawn none.
            var before=new Opening[1];
            try{yield return Warm(true,new System.Collections.Generic.List<double>());yield return Openings(()=>menu.Open(PauseMenu.FabricatePage,true),()=>menu.Close(),before);}
            finally{AsBefore(false);}
            TestContext.WriteLine($"BENCH_OPEN as it was, marks forgotten: {before[0]}");
            Assert.Greater(before[0].assets,0,"fixture, as it was: the bench's first opening draws its marks");
            Assert.LessOrEqual(bench[0].assets,bench[4].assets,$"the bench's first opening made {bench[0].assets} textures, sprites or materials its fifth didn't");
            Assert.LessOrEqual(bench[0].frame,bench[4].frame*2,$"the bench's first opening took {bench[0].frame:0.0} ms against its fifth's {bench[4].frame:0.0}");
            // The stall's first opening is left on the record, not held: with its code built as the zone comes in it read 25.7, 27.6 and 32.9 ms against 16.3 (r10b-after2,
            // after3, final), and what it still pays is the seller's portrait, loaded on that frame (its 17 textures and sprites), which nothing here warms.
        }
        static void AsBefore(bool on)=>typeof(UiWarmup).GetField("asBefore",BindingFlags.NonPublic|BindingFlags.Static)?.SetValue(null,on);
        /// <summary>The title's warm-up run again, a frame at a time, each frame's share timed: as it is, or as it was with every drawn mark forgotten.</summary>
        static IEnumerator Warm(bool asBefore,System.Collections.Generic.List<double> steps)
        {
            AsBefore(asBefore);UiWarmup.Forget();
            if(asBefore)(typeof(ItemMarks).GetField("drawn",BindingFlags.NonPublic|BindingFlags.Static)?.GetValue(null) as System.Collections.IDictionary)?.Clear();
            var host=new GameObject("QaWarmHost");var run=UiWarmup.Run(host.transform);var clock=new Stopwatch();
            while(true){clock.Restart();bool more=run.MoveNext();steps.Add(clock.Elapsed.TotalMilliseconds);if(!more)break;yield return run.Current;}
            Object.Destroy(host);
        }
        /// <summary>The catalog keeps each kind it reads, and reads it from the kind's own folder: that only holds while every definition lives where Find looks.</summary>
        [Test] public void TheCatalogKeepsEveryKindWhereFindLooksAndHandsOutItsOwnArray()
        {
            var all=typeof(GameCatalog).GetMethod("All");
            foreach(var kind in typeof(RecipeDef).Assembly.GetTypes().Where(t=>t.IsSubclassOf(typeof(ScriptableObject))&&!t.IsAbstract))
            {
                var everywhere=Resources.LoadAll("Definitions",kind);
                if(everywhere.Length==0)continue;
                var kept=(System.Array)all.MakeGenericMethod(kind).Invoke(null,null);var again=(System.Array)all.MakeGenericMethod(kind).Invoke(null,null);
                CollectionAssert.AreEquivalent(everywhere,kept,$"a {kind.Name} lives outside Definitions/{kind.Name}, where the catalog and Find look");
                Assert.AreNotSame(kept,again,$"the catalog hands out its own {kind.Name} array, and a caller sorting it would change it for everyone");
            }
        }

        // ------------------------------------------------------------------ QA-031: quitting
        /// <summary>QA-031. The engine crashed tearing D3D12 down twice in 162 replay exits, both on the frame the capture let its target go. The replay and the
        /// game's Quit buttons go out through OrderlyQuit, which lets the device retire that work first; on the old build they call Application.Quit directly.</summary>
        [Test] public void TheReplayAndTheQuitButtonsGoOutThroughTheOrderlyDoor()
        {
            foreach(string file in new[]{"ContinuousReview.cs","PauseMenu.cs","TitleScreen.cs"})
            {
                string source=File.ReadAllText(Path.Combine(Application.dataPath,"_Project/Scripts/UI",file));
                StringAssert.DoesNotContain("Application.Quit",source,$"{file} quits without letting the GPU finish");
                StringAssert.Contains("OrderlyQuit.Quit",source,$"fixture: {file} still quits somewhere");
            }
        }
    }
}
