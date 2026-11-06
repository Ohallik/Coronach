using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

namespace Lattice.Tests.PlayMode
{
    /// <summary>What the November QA pass saw on screen and pinned down: text that has to read on any floor, a card whose
    /// words stay inside its frame, status lines that tell the truth, and a way out of a running game.</summary>
    public sealed class QaHudTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return Load("Arena_Ground");
        }
        static IEnumerator Load(string zone)
        {
            SceneFlow.Current.LoadZone(zone);
            float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture load timed out");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return null;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static TMP_Text Label(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static Image Plate(string name)=>Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(i=>i.name==name);
        sealed class FixturePrompt:InteractionPrompt{public override void Interact(){}}
        static void AssertBacks(string plate,TMP_Text text)
        {
            var image=Plate(plate);Assert.IsNotNull(image,plate+" is missing: floating text has nothing behind it");
            Assert.IsTrue(image.enabled&&image.gameObject.activeInHierarchy,plate+" hides while its text shows");
            Assert.Greater(image.color.a,.4f,plate+" is too faint to separate text from a bright floor");
            var need=text.GetPreferredValues(text.text,text.rectTransform.rect.width,0);
            Assert.GreaterOrEqual(image.rectTransform.rect.width,need.x,plate+" is narrower than its words");
            Assert.GreaterOrEqual(image.rectTransform.rect.height,need.y,plate+" is shorter than its words");
        }

        /// <summary>QA-002. White text with a thin outline washes out on the Decks' and Sorrel's bright floors.</summary>
        [UnityTest] public IEnumerator PromptsHintsAndBarksSitOnPlates()
        {
            var hero=PartyController.Current.Active;
            var fixture=new GameObject("Prompt fixture").AddComponent<FixturePrompt>();
            fixture.prompt="Finish the walkway repair";fixture.range=6;fixture.transform.position=hero.transform.position;
            yield return null;yield return null;yield return null;
            var prompt=Label("Interaction");Assert.IsFalse(string.IsNullOrEmpty(prompt.text),"fixture prompt never showed");
            AssertBacks("InteractionPlate",prompt);
            var hints=Label("Skills");Assert.IsFalse(string.IsNullOrEmpty(hints.text));AssertBacks("SkillsPlate",hints);
            BarkService.Play("Taren","swap",true);yield return null;yield return null;
            var bark=Label("Bark");Assert.IsFalse(string.IsNullOrEmpty(bark.text));AssertBacks("BarkPlate",bark);
            Object.Destroy(fixture.gameObject);yield return null;yield return null;yield return null;
            Assert.IsFalse(Plate("InteractionPlate").enabled,"the plate hides with its prompt");
        }
        /// <summary>QA-005. The card's words crossed its frame: heading over the top edge, body past the left.</summary>
        [UnityTest] public IEnumerator TheLevelCardKeepsItsWordsInsideItsFrame()
        {
            var state=GameServices.Current.State;var member=state.party[0];
            Lattice.Rpg.Levels.Grant(member,Lattice.Rpg.Levels.RequiredXp(member.level)*2);
            var toast=Object.FindFirstObjectByType<QuestToast>();
            float deadline=Time.unscaledTime+2;while(!toast.Showing&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(toast.Showing,"no card for a level-up");yield return null;
            var frame=Plate("QuestToastFrame");var heading=Label("QuestToastHeading");var body=Label("QuestToastBody");
            var corners=new Vector3[4];frame.rectTransform.GetWorldCorners(corners);
            float scale=frame.canvas.scaleFactor;
            // The frame art's border is decoration: words must sit inside it (a quarter of the slice is transparent margin).
            float edge=frame.sprite.border.x/frame.sprite.pixelsPerUnit*frame.canvas.referencePixelsPerUnit/frame.pixelsPerUnitMultiplier*.75f*scale;
            float top=corners[1].y-edge,bottom=corners[0].y+edge,left=corners[0].x+edge*.75f,right=corners[2].x-edge*.75f;
            foreach(var text in new[]{heading,body})
            {
                text.ForceMeshUpdate();var bounds=text.textBounds;
                var min=text.rectTransform.TransformPoint(bounds.min);var max=text.rectTransform.TransformPoint(bounds.max);
                Assert.LessOrEqual(max.y,top+1,text.name+" crosses the frame's top edge");
                Assert.GreaterOrEqual(min.y,bottom-1,text.name+" crosses the frame's bottom edge");
                Assert.GreaterOrEqual(min.x,left-1,text.name+" crosses the frame's left edge");
                Assert.LessOrEqual(max.x,right+1,text.name+" crosses the frame's right edge");
            }
        }
        /// <summary>QA-006. An organ that can't be broken read "BREAK 124 / 100000" over the fight.</summary>
        [UnityTest] public IEnumerator AnUnbreakableTargetShowsNoBreakGauge()
        {
            var organ=new GameObject("Organ fixture").AddComponent<Health>();organ.id="PressureOrgan";organ.maximum=organ.integrity=1500;organ.breakThreshold=100000;
            var hero=PartyController.Current.Active;hero.target=organ;yield return null;yield return null;
            var target=Label("Target");StringAssert.Contains("1500",target.text,"fixture target never showed");
            StringAssert.DoesNotContain("100000",target.text);StringAssert.DoesNotContain("BREAK",target.text);
            Object.Destroy(organ.gameObject);
        }
        /// <summary>QA-003. The Cantor flies through a warp tunnel; while its collar holds the bar said BELOW GROUND.</summary>
        [UnityTest] public IEnumerator TheBossBarNamesWhatIsProtectingTheBoss()
        {
            yield return Load("Arena_Flight");
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(20,1,0));enemy.Passive=false;enemy.enabled=true;
            var hero=PartyController.Current.Active;hero.GetComponent<FlightMotor>().Halt();
            yield return null;yield return null;yield return null;enemy.Passive=false;yield return null;yield return null;
            var state=Label("BossState");Assert.IsNotNull(state,"no boss bar");
            Assert.AreEqual(false,enemy.Health.Damageable,"fixture: the collar holds");
            StringAssert.DoesNotContain("GROUND",state.text);StringAssert.Contains("COLLAR",state.text);
            Object.Destroy(enemy.gameObject);yield return null;
        }
        /// <summary>A break on a collar link interrupts the Cantor's move; that must not lift the collar's protection.</summary>
        [UnityTest] public IEnumerator ABreakOnALinkLeavesTheCollarProtecting()
        {
            yield return Load("Arena_Flight");
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(30,1,0));enemy.Passive=true;
            yield return null;yield return null;
            var collar=enemy.GetComponent<CantorCollar>();var link=collar.Links[0];
            var source=PartyController.Current.Active.Health;
            link.Receive(new DamagePacket{source=source,amount=1,type=DamageType.Pulse,breakPower=enemy.Health.breakThreshold*3});
            yield return null;
            Assert.IsTrue(enemy.Health.Broken,"fixture: the link's hit broke the animal");
            Assert.IsFalse(enemy.Health.Damageable,"a break must not open the body while links remain");
            Object.Destroy(enemy.gameObject);yield return null;
        }
        /// <summary>QA-004. A running game needs a way out that isn't Alt+F4.</summary>
        [UnityTest] public IEnumerator SettingsOffersAConfirmedReturnToTitle()
        {
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open(PauseMenu.SettingsPage);yield return null;
            Button Find(string words)=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.GetComponentInChildren<TMP_Text>()!=null&&b.GetComponentInChildren<TMP_Text>().text.Contains(words));
            var leave=Find("Leave the game");Assert.IsNotNull(leave,"no way out from the pause menu");
            leave.onClick.Invoke();yield return null;yield return null;
            Assert.IsTrue(menu.IsOpen,"one press never leaves: it asks first");
            var title=Find("Return to title");Assert.IsNotNull(title,"no confirm row for the title");Assert.IsNotNull(Find("Quit to desktop"));Assert.IsNotNull(Find("Stay"));
            title.onClick.Invoke();
            float deadline=Time.unscaledTime+10;
            while(!SceneManager.GetSceneByName("Title").isLoaded&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(SceneManager.GetSceneByName("Title").isLoaded,"never reached the title");
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsFalse(GameTime.Paused,"the pause came along to the title");
            Assert.IsNotNull(EventSystem.current.currentSelectedGameObject,"the pad has no focus on the title");
        }
        /// <summary>QA-010. On a pad the selected row must be unmistakable.</summary>
        [UnityTest] public IEnumerator TheSelectedRowStandsApart()
        {
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open();yield return null;yield return null;
            var selected=EventSystem.current.currentSelectedGameObject;Assert.IsNotNull(selected);
            var row=selected.GetComponent<Button>();
            var other=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b!=row&&b.name.StartsWith("Row_"));
            var a=row.GetComponentInChildren<TMP_Text>().color;var b2=other.GetComponentInChildren<TMP_Text>().color;
            Assert.Greater(Mathf.Abs(a.r-b2.r)+Mathf.Abs(a.g-b2.g)+Mathf.Abs(a.b-b2.b),.25f,"the selected row's words look like every other row's");
            yield return null;menu.Close();
        }
        /// <summary>QA-007. The same line on every load wears out in minutes.</summary>
        [UnityTest] public IEnumerator EnteringZonesDoesNotRepeatTheSameLine()
        {
            var lines=new List<string>();void Heard(string who,string line){if(who!=null)lines.Add(line);}
            BarkService.Spoken+=Heard;
            try
            {
                for(int i=0;i<4;i++){yield return Load(i%2==0?"Arena_Flight":"Arena_Ground");yield return new WaitForSecondsRealtime(.1f);}
            }
            finally{BarkService.Spoken-=Heard;}
            Assert.AreEqual(lines.Count,lines.Distinct().Count(),"repeated: "+string.Join(" | ",lines));
        }
        /// <summary>QA-008. The title's picker must not show the menu through itself, and Back must leave the pad a focus.</summary>
        [UnityTest] public IEnumerator TheSavePickerCoversTheMenuAndReturnsFocus()
        {
            GameServices.Current.Saves.Save("slot1",GameServices.Current.State);
            SceneFlow.Current.LoadZone("Title");
            float deadline=Time.unscaledTime+10;
            while((SceneFlow.Current.Loading||!SceneManager.GetSceneByName("Title").isLoaded)&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.3f);
            Button Find(string name)=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name);
            Find("Continue").onClick.Invoke();yield return null;yield return null;
            var title=Object.FindFirstObjectByType<TitleScreen>();Assert.IsTrue(title.SavesOpen);
            Assert.IsNull(Find("New Game"),"the main menu shows through the picker");
            Find("Back").onClick.Invoke();yield return null;yield return null;
            Assert.IsFalse(title.SavesOpen);
            var focus=EventSystem.current.currentSelectedGameObject;Assert.IsNotNull(focus,"Back left the pad with no focus");
            Assert.AreEqual("Continue",focus.name,"focus returns to what opened the picker");
        }
    }
}
