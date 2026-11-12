using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The plates the last QA pass left lying on things (E3-gullet-03, E4-lanternwick-04, E4-relay-02, E2-tallow-12, E4-tallow-01), the words a faded plate left
    /// painted on the floor (E4-gullet-04), the skill row's words with nothing behind them (E4-tallow-02), the boss state against the Cleave icon (E2-hushwell-11), and where the
    /// HUD tells the partner its plates stand. Every check here fails on the build it was written against (e37c9b4): there the target card and the prompt never moved, the bark
    /// moved only for heroes and prompt things, nothing backed a faded plate's words or the skill labels, the state ran its full 900 units, and nothing filled HudKeepOut.</summary>
    public sealed class QaNov10HudTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();PromptService.ForceDevice(null);HudKeepOut.Clear();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return Zone("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var dialogue=DialogueSystem.Current;
            if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            foreach(var marking in Telegraph.Live.ToArray())if(marking!=null)Object.Destroy(marking.gameObject);
            PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            DialogueSystem.Current.AutoAdvance=false;
            yield return new WaitForSecondsRealtime(.35f);
        }
        static CombatActor Hero=>PartyController.Current.Active;
        static CombatActor Partner{get{var party=PartyController.Current;return party.members[1-party.index];}}
        static IEnumerator Settle(){yield return new WaitForSecondsRealtime(.4f);}
        static TMP_Text Text(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static Image Picture(string name)=>Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(i=>i.name==name);
        static Rect Box(RectTransform rect,float inset=0)
        {var c=new Vector3[4];rect.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x+inset,c[0].y+inset,c[2].x-inset,c[2].y-inset);}
        static void Put(CombatActor hero,Vector3 at)
        {
            var body=hero.GetComponent<CharacterController>();body.enabled=false;
            if(hero.TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            hero.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static EnemyBrain Awake(Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),at);enemy.Passive=false;return enemy;}
        static void Guard(){Hero.Health.InvulnerableUntil=Partner.Health.InvulnerableUntil=GameTime.Now+90;}
        sealed class FixturePrompt:InteractionPrompt{public override void Interact(){}}
        static FixturePrompt Prompt(string words,Vector3 at,float range=6)
        {var prompt=new GameObject("Prompt fixture").AddComponent<FixturePrompt>();prompt.prompt=words;prompt.range=range;prompt.transform.position=at;return prompt;}
        static float Shown(Graphic graphic){float alpha=graphic.canvasRenderer.GetAlpha();foreach(var group in graphic.GetComponentsInParent<CanvasGroup>(true))alpha*=group.alpha;return alpha;}
        /// <summary>The ground under a screen point, for a body whose middle stands this high above the hero's feet.</summary>
        static Vector3 Under(Vector2 screen,float height)
        {
            var ray=Camera.main.ScreenPointToRay(screen);float y=Hero.transform.position.y+height;return ray.GetPoint((y-ray.origin.y)/ray.direction.y)-Vector3.up*height;
        }
        static IEnumerator Hold(EnemyBrain enemy,Vector3 at,float seconds)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){enemy.transform.position=at;Physics.SyncTransforms();yield return null;}}
        /// <summary>What the fade has always looked at on an enemy: its middle and its top.</summary>
        static Vector2[] Points(Component body){var at=body.transform.position;var camera=Camera.main;return new Vector2[]{camera.WorldToScreenPoint(at+Vector3.up*.75f),camera.WorldToScreenPoint(at+Vector3.up*1.6f)};}
        static void Clear(Rect plate,Component body,string what,List<string> failures)
        {foreach(var point in Points(body))if(plate.Contains(point))failures.Add($"{what}: the plate at {plate} still lies on the body at {point}");}
        static void Say(string speaker,string line)=>typeof(BarkPanel).GetMethod("Show",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(Object.FindFirstObjectByType<BarkPanel>(),new object[]{speaker,line});
        static Rect Ink(TMP_Text text)
        {
            text.ForceMeshUpdate();var info=text.textInfo;float low=float.MaxValue,high=float.MinValue,left=float.MaxValue,right=float.MinValue;
            for(int i=0;i<info.characterCount;i++)
            {
                var c=info.characterInfo[i];if(!c.isVisible)continue;
                var metrics=c.textElement.glyph.metrics;float top=c.baseLine+metrics.horizontalBearingY*c.scale;
                high=Mathf.Max(high,top);low=Mathf.Min(low,top-metrics.height*c.scale);left=Mathf.Min(left,c.origin);right=Mathf.Max(right,c.xAdvance);
            }
            Vector3 lo=text.transform.TransformPoint(new Vector3(left,low)),hi=text.transform.TransformPoint(new Vector3(right,high));
            return Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);
        }
        static IEnumerator Lock(EnemyBrain enemy){Hero.target=enemy.Health;Hero.TargetLocked=true;yield return null;yield return null;yield return null;}

        // ------------------------------------------------------------------ the target card
        [UnityTest] public IEnumerator TheTargetCardSlidesAlongTheTopOffWhatStandsUnderIt()
        {
            Guard();yield return Settle();
            var hound=Awake(Hero.transform.position+Vector3.right*30);yield return Lock(hound);
            var plate=Picture("TargetPlate");var words=Text("Target");Assert.IsTrue(plate.enabled,"fixture: the card is up");
            var home=Box(plate.rectTransform);var vitals=Box(Picture("VitalsFrame").rectTransform);var objective=Box(Picture("ObjectiveFrame").rectTransform);
            // Under its right quarter, at its own height: the scavengers' heads and chests behind a Gloomgrazer's card.
            var at=Under(new Vector2(home.xMin+home.width*.8f,home.center.y),.75f);yield return Hold(hound,at,.7f);
            var failures=new List<string>();var box=Box(plate.rectTransform);
            Clear(box,hound,"one enemy under the card",failures);
            if(box.yMax>Screen.height+1||box.yMin<home.yMin-2)failures.Add($"the card left the top band for {box}: D222 brought it back up after stepping it into the playfield");
            if(box.Overlaps(vitals))failures.Add("the card slid onto the vitals");if(box.Overlaps(objective))failures.Add("the card slid onto the objective");
            if(words.alpha*Shown(words)<.99f)failures.Add("the card's words go down instead of the card moving");
            StringAssert.Contains(DisplayNames.Target(hound.Health.id).ToUpperInvariant(),words.text,"the card lost the target's name");
            // It goes home once the band is clear, whole.
            hound.transform.position=Hero.transform.position+Vector3.right*30;Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(1f);
            box=Box(plate.rectTransform);
            if(Mathf.Abs(box.center.x-home.center.x)>1.5f)failures.Add($"the card doesn't go home: its middle is at {box.center.x:0}, home {home.center.x:0}");
            StringAssert.Contains("\n",words.text,"the card stays cut to one line with nothing under it");
            Assert.IsEmpty(failures,string.Join("\n",failures));
            Object.Destroy(hound.gameObject);
        }
        [UnityTest] public IEnumerator WhereTheTopBandIsFullTheCardKeepsItsNameAndStandsHigher()
        {
            Guard();yield return Settle();
            var first=Awake(Hero.transform.position+Vector3.right*30);var second=Awake(Hero.transform.position+Vector3.right*32);yield return Lock(first);
            var plate=Picture("TargetPlate");var words=Text("Target");Assert.IsTrue(plate.enabled,"fixture: the card is up");
            var home=Box(plate.rectTransform);
            // One under each end: no slide clears the whole card, as with Hessa's crew standing under hers.
            Vector3 left=Under(new Vector2(home.xMin+20,home.center.y-10),.75f),right=Under(new Vector2(home.xMax-20,home.center.y-10),.75f);
            for(float end=Time.unscaledTime+.7f;Time.unscaledTime<end;){first.transform.position=left;second.transform.position=right;Physics.SyncTransforms();yield return null;}
            var failures=new List<string>();var box=Box(plate.rectTransform);
            Clear(box,first,"the enemy under the card's left end",failures);Clear(box,second,"the enemy under the card's right end",failures);
            if(box.yMax>Screen.height+1)failures.Add($"the card runs off the top of the screen: {box}");
            if(box.yMin<home.yMin-2)failures.Add($"the card stepped down into the playfield: {box}");
            StringAssert.Contains(DisplayNames.Target(first.Health.id).ToUpperInvariant(),words.text,"the card lost the target's name");
            Assert.IsEmpty(failures,string.Join("\n",failures));
            Object.Destroy(first.gameObject);Object.Destroy(second.gameObject);
        }

        // ------------------------------------------------------------------ the prompt bar
        [UnityTest] public IEnumerator ThePromptBarSlidesAlongTheFootOffThePartnerAndWhatItNames()
        {
            var hero=Hero;var partner=Partner;Guard();yield return Settle();
            var fixture=Prompt("Dock — Lanternwick",hero.transform.position);yield return null;yield return null;yield return null;
            var plate=Picture("InteractionPlate");var words=Text("Interaction");Assert.IsTrue(plate.enabled,"fixture: the prompt is up");
            var home=Box(plate.rectTransform);var failures=new List<string>();
            // The partner walks under it, chest at its middle.
            var chest=Under(new Vector2(home.xMin+home.width*.7f,home.center.y),1);Put(partner,chest);
            yield return new WaitForSecondsRealtime(.6f);
            var box=Box(plate.rectTransform);var camera=Camera.main;
            foreach(float up in new[]{.3f,1f,1.7f})if(box.Contains((Vector2)camera.WorldToScreenPoint(partner.transform.position+Vector3.up*up)))failures.Add($"the prompt lies on the partner at {up} m: 'A Dock — Lanternwick' cut Sela's wing");
            if(Mathf.Abs(box.center.y-home.center.y)>1)failures.Add("the prompt left its band at the foot of the screen");
            if(words.alpha*Shown(words)<.99f)failures.Add("the prompt's words go down instead of the bar moving");
            // Then the thing it's about stands under it: the warp beacon under its own sign.
            Put(partner,hero.transform.position+Vector3.left*2);Object.Destroy(fixture.gameObject);yield return new WaitForSecondsRealtime(1.2f);
            var beacon=Prompt("Inspect the port docking line",Under(new Vector2(home.xMin+home.width*.75f,home.center.y),0),60);
            yield return new WaitForSecondsRealtime(.6f);
            StringAssert.Contains("Inspect the port docking line",words.text,"fixture: the beacon is the prompt");
            box=Box(plate.rectTransform);
            if(box.Contains((Vector2)camera.WorldToScreenPoint(beacon.transform.position)))failures.Add("the prompt lies on the very thing it names");
            Assert.IsEmpty(failures,string.Join("\n",failures));
            Object.Destroy(beacon.gameObject);
        }

        // ------------------------------------------------------------------ the bark column
        [UnityTest] public IEnumerator ABarkSlidesOffAnAwakeEnemyAndTheLaneItMarks()
        {
            Guard();yield return Settle();
            Say("Sela","Curtain's coming down, Vosk. That door only opens for people who relight things.");yield return null;yield return null;
            var plate=Picture("BarkPlate");var words=Text("Bark");Assert.IsTrue(plate.enabled,"fixture: the bark is up");
            float column=words.rectTransform.anchoredPosition.x;var rest=Box(plate.rectTransform);var failures=new List<string>();
            var hound=Awake(Under(rest.center,.75f));yield return Hold(hound,Under(rest.center,.75f),.5f);
            Clear(Box(plate.rectTransform),hound,"an awake raider under the bark",failures);
            if(Mathf.Abs(words.rectTransform.anchoredPosition.x-column)>.01f)failures.Add("the bark left its column");
            hound.Passive=true;hound.transform.position=Hero.transform.position+Vector3.right*40;Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.45f);
            // The lane it fires down, crossing under the column at the bark's own height.
            Vector3 from=Under(new Vector2(rest.xMin-40,rest.center.y),0),to=Under(new Vector2(rest.xMax+40,rest.center.y),0);var way=to-from;way.y=0;
            var lane=Telegraph.Line(from,way.normalized,way.magnitude,2,5,Telegraph.Threat);yield return new WaitForSecondsRealtime(.5f);
            var middle=(Vector2)Camera.main.WorldToScreenPoint(from+way*.5f);
            if(Box(plate.rectTransform).Contains(middle))failures.Add("the bark lies on the lane a raider is firing down");
            Assert.IsEmpty(failures,string.Join("\n",failures));
            Object.Destroy(lane.gameObject);Object.Destroy(hound.gameObject);
        }

        // ------------------------------------------------------------------ a faded plate's words
        [UnityTest] public IEnumerator AFadedPlateKeepsABackingBehindItsWords()
        {
            Guard();yield return Settle();
            var frame=Picture("ObjectiveFrame");var words=Text("Objective");
            var at=Under(Box(frame.rectTransform).center,.75f);var hound=Awake(at);yield return Hold(hound,at,.45f);
            Assert.Less(frame.canvasRenderer.GetAlpha(),.42f,"fixture: an awake enemy is behind the objective");
            var band=Picture("ObjectiveWordsBand");
            Assert.IsNotNull(band,"the objective's words have nothing behind them with the plate aside: they lay straight on the launch pad they named");
            Assert.IsTrue(band.isActiveAndEnabled,"the words' backing isn't up with the plate aside");
            Assert.That(band.canvasRenderer.GetAlpha(),Is.InRange(.45f,.8f),"the backing either doesn't darken what's behind the words or blacks it out again");
            var drawn=frame.canvas.GetComponentsInChildren<Graphic>().ToList();
            Assert.Less(drawn.IndexOf(frame),drawn.IndexOf(band),"the backing is under the plate");Assert.Less(drawn.IndexOf(band),drawn.IndexOf(words),"the backing is drawn over the words");
            var ink=Ink(words);var under=Box(band.rectTransform);
            Assert.IsTrue(under.xMin<=ink.xMin&&under.xMax>=ink.xMax&&under.yMin<=ink.yMin&&under.yMax>=ink.yMax,$"the backing {under} doesn't reach round the words {ink}");
            Assert.Less(under.width*under.height,Box(frame.rectTransform).width*Box(frame.rectTransform).height,"the backing is as big as the plate it stands in for");
            // With nobody behind, the plate comes back and the backing goes.
            hound.Passive=true;hound.transform.position=Hero.transform.position+Vector3.right*40;Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.IsTrue(!band.isActiveAndEnabled||band.canvasRenderer.GetAlpha()<.05f,"the backing stays with the plate back");
            Object.Destroy(hound.gameObject);
        }
        [UnityTest] public IEnumerator TheSkillRowsWordsHaveABackingOfTheirOwn()
        {
            yield return Settle();
            Assert.IsTrue(ZoneController.Current.Combat,"fixture: a fight zone, with the skill row up");
            var failures=new List<string>();
            for(int i=0;i<4;i++)
            {
                var label=Text("SkillLabel"+i);if(label==null||!label.isActiveAndEnabled)continue;
                var band=Picture("SkillLabelBand"+i);
                if(band==null||!band.isActiveAndEnabled){failures.Add($"SkillLabel{i} '{label.text}' has nothing behind it: a lit relay's crown stood straight behind 'RB+A · 18C'");continue;}
                if(band.color.a*band.canvasRenderer.GetAlpha()<.4f||band.color.a>.8f)failures.Add($"SkillLabelBand{i} is at {band.color.a:0.00}: it either doesn't back the words or is a black box");
                var drawn=band.canvas.GetComponentsInChildren<Graphic>().ToList();if(drawn.IndexOf(band)>drawn.IndexOf(label))failures.Add($"SkillLabelBand{i} is drawn over its words");
                var ink=Ink(label);var under=Box(band.rectTransform);
                if(!(under.xMin<=ink.xMin&&under.xMax>=ink.xMax&&under.yMin<=ink.yMin&&under.yMax>=ink.yMax))failures.Add($"SkillLabelBand{i} {under} doesn't reach round '{label.text}' {ink}");
                if(under.Overlaps(Box(Picture("Skill"+i).rectTransform,2)))failures.Add($"SkillLabelBand{i} runs up over its icon");
            }
            Assert.IsNotNull(Text("SkillLabel0"),"fixture: no skill labels");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the boss state
        [UnityTest] public IEnumerator TheLongestBossStateStandsClearOfTheSkillRow()
        {
            var hero=Hero;Guard();
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),hero.transform.position+Vector3.forward*14);boss.Passive=false;
            yield return new WaitForSecondsRealtime(.4f);
            var state=Text("BossState");var name=Text("BossName");Assert.IsTrue(state.isActiveAndEnabled,"fixture: the bar is up");
            var icon=Box(Picture("Skill0").rectTransform);float units=state.rectTransform.lossyScale.x;var bar=Box(Picture("BossIntegrityBack").rectTransform);
            var failures=new List<string>();
            // Read in the same frame they're set: the bar puts its own state back on its next refresh.
            foreach(string words in new[]{"BRACED ON ITS ORGANS  ·  BREAK THEM","THE COLLAR HOLDS  ·  CUT THE LINKS","OPENING  ·  STRIKE NOW"})
            {
                state.text=words;var ink=Ink(state);
                if(icon.xMin-ink.xMax<20*units)failures.Add($"'{words}' ends {(icon.xMin-ink.xMax)/units:0} units short of the Cleave icon");
                if(Mathf.Abs(ink.center.x-bar.center.x)>4*units)failures.Add($"'{words}' isn't centred under the bar");
                if(state.fontSize<=name.fontSize)failures.Add($"'{words}' shrank to {state.fontSize}, no bigger than the boss's name");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
            boss.Passive=true;Object.Destroy(boss.gameObject);
        }

        // ------------------------------------------------------------------ what the partner keeps out of
        [UnityTest] public IEnumerator TheHudSaysWhereItsPlatesStand()
        {
            yield return Settle();
            Assert.IsTrue(HudKeepOut.Fresh,"nothing tells the partner where the HUD's plates are: she flew under the skill row and behind the boss card");
            var rects=HudKeepOut.Rects.ToList();
            bool Has(Rect want)=>rects.Any(r=>Mathf.Abs(r.xMin-want.xMin)<1.5f&&Mathf.Abs(r.xMax-want.xMax)<1.5f&&Mathf.Abs(r.yMin-want.yMin)<1.5f&&Mathf.Abs(r.yMax-want.yMax)<1.5f);
            Assert.IsTrue(Has(Box(Picture("VitalsFrame").rectTransform)),"the vitals aren't in the list");
            Assert.IsTrue(Has(Box(Picture("ObjectiveFrame").rectTransform)),"the objective isn't in the list");
            if(ZoneController.Current.Combat){var row=Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).First(r=>r.name=="SkillRow");Assert.IsTrue(Has(Box(row)),"the skill row isn't in the list");}
            var foot=Box(Picture("SkillsPlate").rectTransform);
            Assert.IsTrue(HudKeepOut.Covers(new Vector3(foot.center.x,foot.center.y,1)),"a point on the hint bar isn't covered");
            // A menu or a conversation hides the HUD, and then nothing is kept out of.
            GameServices.Current.Input.Blocked=true;for(int i=0;i<4;i++)yield return null;
            Assert.IsFalse(HudKeepOut.Fresh,"with the HUD hidden its plates are still listed");
            GameServices.Current.Input.Blocked=false;for(int i=0;i<3;i++)yield return null;
            Assert.IsTrue(HudKeepOut.Fresh,"the list doesn't come back with the HUD");
        }
    }
}
