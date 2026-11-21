using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>A chapter's two cards, from its main quest's definition (ChapterCards, drawn by QuestToast): its number and name as the quest begins, and CHAPTER n COMPLETE when its closing
    /// flag goes up or, with none named, when the quest is done. Chapters past the second are built here in memory and handed to ChapterCards.Follow, so a card with its own flag and its own
    /// words is held to the rules before any chapter ships one; the chapters that have shipped are measured on their own plates. The last test pins chapter 2's card to what it was while it
    /// was written out in QuestToast: its words, where each piece stands, its nine seconds and its flag.</summary>
    public sealed class ChapterCardTests
    {
        readonly List<(QuestDef quest,System.IDisposable handle)> made=new();
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach(var (quest,handle) in made){handle.Dispose();Object.Destroy(quest);}
            made.Clear();
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static GameState State=>GameServices.Current.State;
        static QuestToast Toast=>Object.FindFirstObjectByType<QuestToast>();
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.6f);
        }
        /// <summary>A party arriving at chapter 3 with chapters 1 and 2 told, not yet in a zone: what a test sets now is in the save before any card looks at it.</summary>
        static void ArriveAtChapterThree(){GameServices.Current.NewGame();DevLoadout.Apply("chapter3");}
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static IEnumerator Until(System.Func<bool> met,float seconds){float deadline=Time.unscaledTime+seconds;while(!met()&&Time.unscaledTime<deadline)yield return null;}
        static TMP_Text Text(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static Image Picture(string name)=>Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(i=>i.name==name);
        static bool Up(string image){var picture=Picture(image);return picture!=null&&picture.gameObject.activeInHierarchy;}
        /// <summary>A main quest for a chapter nobody has built, in memory: two steps, and the card data the test is about. ChapterCards follows it until Cleanup.</summary>
        QuestDef Chapter(int chapter,string id,string title,string flag=null,string words=null)
        {
            var quest=ScriptableObject.CreateInstance<QuestDef>();quest.name=id;quest.id=id;quest.title=title;quest.description="A chapter built in memory for ChapterCardTests.";
            quest.main=true;quest.chapter=chapter;quest.cardFlag=flag;quest.cardBody=words;
            quest.steps=new[]{new Objective{kind=ObjectiveKind.Flag,target=id+".first",count=1},new Objective{kind=ObjectiveKind.Flag,target=id+".last",count=1}};
            quest.stepText=new[]{"Do the first thing","Do the last thing"};
            made.Add((quest,ChapterCards.Follow(quest)));
            return quest;
        }
        static readonly FieldInfo Timer=typeof(QuestToast).GetField("showing",BindingFlags.NonPublic|BindingFlags.Instance);
        /// <summary>Reads each card's heading off the real panel in the order they come up, sending each on after a frame: a chapter's card stays up for seconds.</summary>
        static IEnumerator Read(List<string> seen,float seconds)
        {
            Assert.IsNotNull(Timer,"fixture: the card's own timer has moved");
            float until=Time.unscaledTime+seconds;
            while(Time.unscaledTime<until)
            {
                yield return null;var toast=Toast;if(toast==null)continue;
                if(toast.Showing&&(seen.Count==0||seen[seen.Count-1]!=toast.Heading))seen.Add(toast.Heading);
                Timer.SetValue(toast,0f);
            }
        }
        static IEnumerator NoChapterCard(string complaint,float seconds=1.2f)
        {
            float until=Time.unscaledTime+seconds;
            while(Time.unscaledTime<until)
            {
                var now=Toast;
                if(now!=null&&now.Showing)StringAssert.DoesNotContain("CHAPTER",now.Heading,complaint);
                Assert.IsFalse(QuestToast.CardUp,complaint);
                yield return null;
            }
        }
        static void Fits(TMP_Text text)
        {
            Assert.IsNotNull(text,"fixture: the card's pieces");Assert.IsTrue(text.isActiveAndEnabled,text.name+" isn't up with its card");
            var box=text.rectTransform.rect;var need=text.GetPreferredValues(text.text,box.width,0);
            Assert.LessOrEqual(need.y,box.height+1,$"{text.name} overflows its box: \"{text.text}\"");Assert.GreaterOrEqual(text.fontSize,24,$"{text.name} is under the type floor");
        }
        static void Stands(RectTransform rect,Vector2 at,Vector2 size,string what)
        {
            Assert.AreEqual(at.x,rect.anchoredPosition.x,.01f,what+" moved");Assert.AreEqual(at.y,rect.anchoredPosition.y,.01f,what+" moved");
            Assert.AreEqual(size.x,rect.sizeDelta.x,.01f,what+" changed size");Assert.AreEqual(size.y,rect.sizeDelta.y,.01f,what+" changed size");
        }
        const string BorrowedWords="The lamps are lit as far as the last berth, and the people aboard have somewhere to sleep. The lane past it is still being drawn, so keep exploring.";

        [UnityTest] public IEnumerator AChapterOpensOnItsNumberAndNameAheadOfItsQuestCardOnceTheTalkCloses()
        {
            ArriveAtChapterThree();
            var quest=ChapterCards.Quests.FirstOrDefault(q=>GameCatalog.Find<QuestDef>(q.id)==q&&!ChapterCards.Begun(q,State));
            Assert.IsNotNull(quest,"fixture: a main quest past chapter 2 is defined, and a party arriving at chapter 3 hasn't begun it");
            yield return Zone("Arena_Ground");var toast=Toast;
            yield return NoChapterCard("arriving at a chapter that hasn't begun puts a chapter's card up",.6f);
            // The conversation that gives the quest is still on screen: nothing comes up over it.
            GameServices.Current.Input.Blocked=true;RpgServices.Quests.Start(quest.id);
            yield return new WaitForSecondsRealtime(.8f);
            Assert.IsFalse(toast.Showing,"the chapter's name comes up over the conversation that begins it");Assert.IsFalse(QuestToast.CardUp,"the prompt and the signs clear for a card that isn't up");
            GameServices.Current.Input.Blocked=false;
            yield return Until(()=>toast.Showing,3);
            Assert.AreEqual("CHAPTER "+quest.chapter,toast.Heading,"a chapter's main quest begins and the first card isn't the chapter's own: NEW QUEST went ahead of it, or nothing says which chapter this is");
            var heading=Text("ChapterTitleCardHeading");var title=Text("ChapterTitleCardTitle");
            Fits(heading);Fits(title);
            Assert.AreEqual("CHAPTER "+quest.chapter,heading.text);Assert.AreEqual(quest.title.ToUpperInvariant(),title.text,"the chapter's card isn't named for its main quest");
            Assert.AreEqual("Cinzel SDF",heading.font.name,"the opening card isn't in the display face the closing card is");Assert.AreEqual(Text("ChapterCardTitle").color,title.color,"the name isn't in the closing card's gold");
            Assert.IsTrue(QuestToast.CardUp,"the prompt and the floor signs stay up under the chapter's name");
            Assert.IsFalse(Up("QuestToastFrame"),"the corner card shows beside the chapter's name");Assert.IsFalse(Up("ChapterCardFrame"),"the closing card's plate is up with the opening's");
            // It stands on the closing card's own ground, which QaHudThirdTests holds clear of the hint bar and the party: the same middle, as wide, and less tall.
            var plate=Picture("ChapterTitleCardFrame").rectTransform;var closing=Picture("ChapterCardFrame").rectTransform;
            Assert.AreEqual(((RectTransform)closing.parent).anchoredPosition,((RectTransform)plate.parent).anchoredPosition,"the opening card stands somewhere the closing card doesn't");
            Assert.AreEqual(closing.anchoredPosition,plate.anchoredPosition);Assert.AreEqual(closing.sizeDelta.x,plate.sizeDelta.x,.01f);Assert.Less(plate.sizeDelta.y,closing.sizeDelta.y,"a card of two lines stands on the plate made for five");
            Assert.AreEqual(1,Picture("ChapterTitleCardFrameBacking").color.a,.001f,"the floor shows through the opening card");
            var seen=new List<string>();yield return Read(seen,1);
            CollectionAssert.AreEqual(new[]{"CHAPTER "+quest.chapter,"NEW QUEST"},seen.Take(2).ToArray(),"the chapter's name and its quest's own card don't come one after the other, name first");
            StringAssert.StartsWith(quest.title+"\n",toast.Body,"fixture: the NEW QUEST card is this quest's");
            Assert.AreEqual(1,seen.Count(h=>h.StartsWith("CHAPTER")),"the chapter's name comes up twice");
            // Once: not on the next zone, and not on a load.
            yield return Zone("Arena_Ground");yield return NoChapterCard("the chapter's name plays again on the next zone");
            var saves=new SaveSystem(Path.Combine(Application.temporaryCachePath,"ChapterCard-"+System.Guid.NewGuid().ToString("N")));
            saves.Save("slot1",State);GameServices.Current.State=saves.Load("slot1");
            yield return Zone("Arena_Ground");yield return NoChapterCard("loading a save from inside a chapter plays its opening card");
        }
        [UnityTest] public IEnumerator AChapterWithAClosingFlagEndsOnItsOwnWordsWhenTheFlagGoesUpAndNeverOnALoad()
        {
            ArriveAtChapterThree();
            var quest=Chapter(9,"TestBorrowedRoad","The Borrowed Road","test.borrowedRoad.home",BorrowedWords);
            State.questSteps[quest.id]=1;
            yield return Zone("Arena_Ground");var toast=Toast;
            yield return NoChapterCard("a chapter already under way when the save is picked up opens again");
            // The quest is paid in the chapter's last fight; the card is the closing conversation's, which comes after.
            State.questSteps[quest.id]=quest.steps.Length;
            yield return NoChapterCard("the card came up when the quest was done, ahead of the flag its definition names");
            Flag(quest.cardFlag);
            yield return Until(()=>toast.Showing&&toast.Heading=="CHAPTER 9 COMPLETE",3);
            Assert.IsTrue(toast.Showing&&toast.Heading=="CHAPTER 9 COMPLETE","the chapter's closing flag went up and nothing says the chapter is complete");yield return null;
            var heading=Text("ChapterCardHeading");var title=Text("ChapterCardTitle");var words=Text("ChapterCardBody");
            Fits(heading);Fits(title);Fits(words);
            Assert.AreEqual("CHAPTER 9 COMPLETE",heading.text);Assert.AreEqual("THE BORROWED ROAD",title.text,"the card still carries another chapter's name");
            Assert.AreEqual(BorrowedWords,words.text,"the card doesn't say the words its quest gives it");Assert.AreEqual(words.text,toast.Body,"fixture: the toast still reports what it shows");
            Assert.IsTrue(QuestToast.CardUp);Assert.IsFalse(Up("QuestToastFrame"),"the corner card shows the same words beside it");Assert.IsFalse(Up("ChapterTitleCardFrame"),"the short plate is up with the full one");
            yield return new WaitForSecondsRealtime(5.5f);
            Assert.IsTrue(toast.Showing,"three lines at the end of a chapter are gone before they can be read");
            yield return Until(()=>!toast.Showing,6);
            Assert.IsFalse(toast.Showing,"fixture: the card leaves");
            yield return Zone("Arena_Ground");yield return NoChapterCard("the chapter's card plays again on the next zone");
            var saves=new SaveSystem(Path.Combine(Application.temporaryCachePath,"ChapterCard-"+System.Guid.NewGuid().ToString("N")));
            saves.Save("slot1",State);GameServices.Current.State=saves.Load("slot1");
            yield return Zone("Arena_Ground");yield return NoChapterCard("loading a save from after a chapter's end plays its card");
        }
        [UnityTest] public IEnumerator WithNoFlagAndNoWordsAChapterEndsWhenItsQuestDoesOnTheShortPlate()
        {
            ArriveAtChapterThree();
            var quest=Chapter(8,"TestQuietHarbour","A Quiet Harbour");quest.closingLine="Chapter 8 complete. The next lane isn't charted yet.";
            State.questSteps[quest.id]=1;
            yield return Zone("Arena_Ground");var toast=Toast;
            State.questSteps[quest.id]=quest.steps.Length;
            yield return Until(()=>toast.Showing,3);
            Assert.AreEqual("CHAPTER 8 COMPLETE",toast.Heading,"a main quest with no closing flag is done and nothing says its chapter is");
            float up=Time.unscaledTime;yield return null;
            var heading=Text("ChapterTitleCardHeading");var title=Text("ChapterTitleCardTitle");
            Fits(heading);Fits(title);
            Assert.AreEqual("CHAPTER 8 COMPLETE",heading.text);Assert.AreEqual("A QUIET HARBOUR",title.text);
            Assert.IsTrue(string.IsNullOrEmpty(toast.Body),"the objective panel's closing line is repeated on the card: it is on screen already, a hand's width away");
            Assert.IsTrue(QuestToast.CardUp);Assert.IsFalse(Up("ChapterCardFrame"),"a card with no words under its rule stands on the tall plate, a third of it empty");Assert.IsFalse(Up("QuestToastFrame"));
            yield return Until(()=>!toast.Showing,8);
            float stood=Time.unscaledTime-up;
            Assert.IsFalse(toast.Showing,"fixture: the card leaves");
            Assert.That(stood,Is.InRange(3.5f,5.5f),"a number and a name hold the foot of the screen, and the prompt under it, about as long as an ordinary card: not the nine seconds three lines need");
        }
        [UnityTest] public IEnumerator SeveralChaptersInOneSaveEachSayItOnceAndInStoryOrder()
        {
            ArriveAtChapterThree();
            var eighth=Chapter(8,"TestQuietHarbour","A Quiet Harbour");var ninth=Chapter(9,"TestBorrowedRoad","The Borrowed Road","test.borrowedRoad.home",BorrowedWords);var tenth=Chapter(10,"TestLastBerth","The Last Berth");
            State.questSteps[eighth.id]=0;
            yield return Zone("Arena_Ground");
            // One conversation ends chapter 8 and begins chapter 9: the ending is said first, whichever the game noticed first.
            GameServices.Current.Input.Blocked=true;
            State.questSteps[ninth.id]=0;yield return new WaitForSecondsRealtime(.5f);
            State.questSteps[eighth.id]=eighth.steps.Length;yield return new WaitForSecondsRealtime(.5f);
            Assert.IsFalse(Toast.Showing,"a chapter's card comes up over the conversation that ends it");
            GameServices.Current.Input.Blocked=false;
            var seen=new List<string>();yield return Read(seen,1);
            CollectionAssert.AreEqual(new[]{"CHAPTER 8 COMPLETE","CHAPTER 9"},seen.Where(h=>h.StartsWith("CHAPTER")).ToArray(),"a chapter ending as the next begins doesn't say so once each, the ending first");
            // A load in the middle of chapter 9 says neither again, and chapter 9's own ending is still to come.
            var saves=new SaveSystem(Path.Combine(Application.temporaryCachePath,"ChapterCard-"+System.Guid.NewGuid().ToString("N")));
            saves.Save("slot1",State);GameServices.Current.State=saves.Load("slot1");
            yield return Zone("Arena_Ground");yield return NoChapterCard("a save with one chapter complete and the next begun plays their cards on loading");
            State.questSteps[ninth.id]=ninth.steps.Length;Flag(ninth.cardFlag);
            // And a chapter first seen already finished (a fixture's jump, a save patched forward) has no beginning to announce.
            State.questSteps[tenth.id]=tenth.steps.Length;
            seen.Clear();yield return Read(seen,1);
            CollectionAssert.AreEqual(new[]{"CHAPTER 9 COMPLETE","CHAPTER 10 COMPLETE"},seen.Where(h=>h.StartsWith("CHAPTER")).ToArray(),"with chapter 8 long complete in this save, ending chapters 9 and 10 doesn't say exactly those two, in order");
            seen.Clear();yield return Zone("Arena_Ground");yield return Read(seen,.8f);
            Assert.IsFalse(seen.Any(h=>h.StartsWith("CHAPTER")),"a chapter's card plays again on the next zone: "+string.Join(", ",seen));
        }
        [UnityTest] public IEnumerator EveryChapterDefinedSinceTheSecondHasOneMainQuestWhoseCardsFitTheirPlates()
        {
            // One main quest a chapter: each brings its chapter's two cards, so a second would open and close the chapter twice.
            var later=GameCatalog.All<QuestDef>().Where(q=>q.main&&System.Array.IndexOf(StoryQuests.First,q.id)<0).ToArray();
            foreach(var quest in later)Assert.Greater(quest.chapter,0,quest.id+" is a main quest with no chapter (QuestDef.chapter): its chapter gets no cards");
            foreach(var chapter in later.GroupBy(q=>q.chapter))Assert.AreEqual(1,chapter.Count(),$"chapter {chapter.Key} has {chapter.Count()} main quests ({string.Join(", ",chapter.Select(q=>q.id))}): its cards would come up once for each");
            CollectionAssert.AreEquivalent(later.Select(q=>q.id).ToArray(),ChapterCards.Quests.Select(q=>q.id).ToArray(),"the chapters with cards aren't the main quests defined since the first three");
            ArriveAtChapterThree();
            foreach(var quest in later){State.questSteps.Remove(quest.id);if(!string.IsNullOrWhiteSpace(quest.cardFlag))State.flags.Remove(quest.cardFlag);}
            yield return Zone("Arena_Ground");var toast=Toast;
            Assert.IsNotNull(Timer,"fixture: the card's own timer has moved");
            IEnumerator Card(string wanted)
            {
                float deadline=Time.unscaledTime+3;
                while(Time.unscaledTime<deadline&&!(toast.Showing&&toast.Heading==wanted)){Timer.SetValue(toast,0f);yield return null;}
                Assert.IsTrue(toast.Showing&&toast.Heading==wanted,wanted+" never comes up");
            }
            foreach(var quest in later)
            {
                State.questSteps[quest.id]=0;yield return Card(ChapterCards.Opening(quest));
                Fits(Text("ChapterTitleCardHeading"));Fits(Text("ChapterTitleCardTitle"));
                Assert.AreEqual(quest.title.ToUpperInvariant(),Text("ChapterTitleCardTitle").text,quest.id+" opens under another name");
                State.questSteps[quest.id]=quest.steps.Length;if(!string.IsNullOrWhiteSpace(quest.cardFlag))Flag(quest.cardFlag);
                // The last chapter ends on the credits, not on a card (D288): CreditsTests holds that none comes up for it, and that every earlier chapter's still does.
                if(ChapterCards.EndsOnCredits(quest))continue;
                yield return Card(ChapterCards.Closing(quest));
                // Two lines of the plate, about 190 characters: a third runs into the rule above it and off the plate's foot.
                if(ChapterCards.Body(quest).Length>0){Fits(Text("ChapterCardHeading"));Fits(Text("ChapterCardTitle"));Fits(Text("ChapterCardBody"));Assert.AreEqual(quest.cardBody.Trim(),Text("ChapterCardBody").text);}
                else{Fits(Text("ChapterTitleCardHeading"));Fits(Text("ChapterTitleCardTitle"));}
            }
        }
        [UnityTest] public IEnumerator ChapterTwoStillEndsOnTheCardItShippedWithWhateverItsAssetSays()
        {
            yield return Zone("Arena_Ground");var toast=Toast;
            // Its card is QuestToast's own, on the homecoming's flag. Given one from its data as well, the chapter would end twice.
            Assert.IsFalse(ChapterCards.Quests.Any(q=>System.Array.IndexOf(StoryQuests.First,q.id)>=0),"one of the first three quests has cards from its definition: chapters 1 and 2 show what they always showed");
            var road=GameCatalog.Find<QuestDef>("LanternRoad");Assert.IsNotNull(road,"fixture: The Lantern Road");
            // Beginning it and finishing it say what they always said: the task's own cards, and no plate across the foot of the screen.
            var seen=new List<string>();RpgServices.Quests.Start("LanternRoad");yield return Read(seen,.8f);
            CollectionAssert.Contains(seen,"NEW QUEST","fixture: The Lantern Road is announced");
            State.questSteps["LanternRoad"]=road.steps.Length;yield return Read(seen,.8f);
            Assert.IsFalse(seen.Any(h=>h.Contains("CHAPTER")),"chapter 2 now opens on a card, or ends when its quest does and not at the homecoming: "+string.Join(", ",seen));
            Flag("chapter2.homecoming");
            yield return Until(()=>toast.Showing&&toast.Heading=="CHAPTER 2 COMPLETE",3);
            Assert.IsTrue(toast.Showing&&toast.Heading=="CHAPTER 2 COMPLETE","the homecoming is heard and nothing says the chapter is complete");
            float up=Time.unscaledTime;yield return null;
            var heading=Text("ChapterCardHeading");var title=Text("ChapterCardTitle");var words=Text("ChapterCardBody");
            Assert.AreEqual("CHAPTER 2 COMPLETE",heading.text);Assert.AreEqual("THE LANTERN ROAD",title.text);
            Assert.AreEqual("Tallow has its route that waits, and a lantern to keep it. The road runs on to Xylos's river towns, uncharted so far. Keeper Abri has a reason to try: the cabins are asking about dinner.",words.text);
            Assert.AreEqual(words.text,toast.Body);
            Assert.IsTrue(QuestToast.CardUp);Assert.IsFalse(Up("ChapterTitleCardFrame"),"chapter 2's card has moved to the short plate");Assert.IsFalse(Up("QuestToastFrame"));
            // Where each piece stood on the canvas (1920 by 1080) while the card was written out in QuestToast.
            var frame=Picture("ChapterCardFrame").rectTransform;var holder=(RectTransform)frame.parent;
            Assert.AreEqual(new Vector2(.5f,0),holder.anchorMin);Assert.AreEqual(new Vector2(.5f,0),holder.anchorMax);Assert.AreEqual(new Vector2(0,252),holder.anchoredPosition,"the card stands somewhere else on the screen");
            Stands(frame,Vector2.zero,new Vector2(1560,300),"the plate");Stands(Picture("ChapterCardFrameBacking").rectTransform,Vector2.zero,new Vector2(1512,252),"the plate's backing");
            Stands(heading.rectTransform,new Vector2(0,76),new Vector2(1300,78),"the heading");Stands(title.rectTransform,new Vector2(0,14),new Vector2(1300,44),"the title");
            Stands(Picture("ChapterCardRule").rectTransform,new Vector2(0,-14),new Vector2(620,3),"the rule");Stands(words.rectTransform,new Vector2(0,-66),new Vector2(1440,80),"the words");
            Assert.AreEqual(64,heading.fontSize,.01f);Assert.AreEqual(32,title.fontSize,.01f);Assert.AreEqual(30,words.fontSize,.01f);
            Assert.AreEqual(new Color(1,.93f,.8f),heading.color);Assert.AreEqual(UiKit.GoldColor,title.color);Assert.AreEqual(UiKit.LooseTextColor,words.color);Assert.AreEqual(UiKit.GoldColor,Picture("ChapterCardRule").color);
            Assert.AreEqual("Cinzel SDF",heading.font.name);Assert.AreEqual("Cinzel SDF",title.font.name);
            var pieces=holder.Cast<Transform>().Select(t=>t.name).ToArray();
            CollectionAssert.AreEqual(new[]{"ChapterCardFrameBacking","ChapterCardFrame","ChapterCardHeading","ChapterCardTitle","ChapterCardRule","ChapterCardBody"},pieces,"the card is made of other pieces, or they draw in another order");
            yield return Until(()=>!toast.Showing,11);
            float stood=Time.unscaledTime-up;
            Assert.IsFalse(toast.Showing,"fixture: the card leaves");
            Assert.That(stood,Is.InRange(8.3f,9.9f),"chapter 2's card no longer holds for its nine seconds");
        }
    }
}
