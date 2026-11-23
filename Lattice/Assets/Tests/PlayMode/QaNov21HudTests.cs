using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
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
    /// <summary>What the whole-game capture of November 20 found the HUD laying on itself in the player: the prompt a fight asks for drawn across the boss bar's name in
    /// every flight fight with a dock (s63 044, s65 055, s66 149); a bark come down its column onto the boss bar (s63 072); the Picket's panel cutting the resting hero's
    /// line and 'Saved' printed on it (s45 095, 140); every boss fought on foot with no name on its lock card (s15 148, s41 042, s48 216); a card for a Pikeguard nowhere on
    /// the screen (s48 229); a bark showing in pieces round an EXTRA's portrait (s47 080); the title card on the heroes or over the bar that says the same name (s15 135,
    /// s41 030, s63 031); numbers in piles and a '0!!' (s14 169, s41 036). Each measured check was shown finding its fault on the November 20 layout first (run
    /// ui-fight3, behind a switch since removed) and must find none on the layout as it is. Stills of the HUD in those states are written to Builds/quality/focused/ui-fight.</summary>
    public sealed class QaNov21HudTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();PromptService.ForceDevice(PromptDevice.Gamepad);SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            // Three in the party, the third resting: the vitals at their fullest, as the Picket's reviewers saw them.
            DevLoadout.Apply("chapter8");yield return Zone("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            
            var dialogue=DialogueSystem.Current;
            if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            foreach(var marking in Telegraph.Live.ToArray())if(marking!=null)Object.Destroy(marking.gameObject);
            PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            DialogueSystem.Current.AutoAdvance=false;
            yield return new WaitForSecondsRealtime(.4f);
        }
        static CombatActor Hero=>PartyController.Current.Active;
        static CombatActor Partner{get{var party=PartyController.Current;return party.members[1-party.index];}}
        static GameHud Hud=>Object.FindFirstObjectByType<GameHud>();
        static TMP_Text Text(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static Image Picture(string name)=>Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(i=>i.name==name);
        static Rect Box(RectTransform rect,float inset=0)
        {var c=new Vector3[4];rect.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x+inset,c[0].y+inset,c[2].x-inset,c[2].y-inset);}
        static bool Up(Graphic graphic)=>graphic!=null&&graphic.isActiveAndEnabled&&graphic.canvasRenderer.GetAlpha()>.01f&&(graphic.canvas==null||graphic.canvas.enabled);
        static void Guard(){foreach(var m in PartyController.Current.members)m.Health.InvulnerableUntil=GameTime.Now+120;}
        static IEnumerator Settle(float seconds=.4f){yield return new WaitForSecondsRealtime(seconds);}
        sealed class FightPrompt:InteractionPrompt{public override void Interact(){}public override bool InFight=>true;}
        static FightPrompt Prompt(string words,Vector3 at)
        {var prompt=new GameObject("Fight prompt fixture").AddComponent<FightPrompt>();prompt.prompt=words;prompt.range=6;prompt.transform.position=at;return prompt;}
        static void Say(string speaker,string line)=>typeof(BarkPanel).GetMethod("Show",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(Object.FindFirstObjectByType<BarkPanel>(),new object[]{speaker,line});
        static EnemyBrain Boss(string id,Vector3 at){var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);boss.Passive=false;return boss;}
        static Vector3 Ahead(float metres)=>Hero.transform.position+Quaternion.Euler(0,ZoneController.Current.definition.cameraProfile.yaw,0)*Vector3.forward*metres;
        /// <summary>The ground under a screen point.</summary>
        static Vector3 Under(Vector2 screen,float height=0)
        {var ray=Camera.main.ScreenPointToRay(screen);float y=Hero.transform.position.y+height;return ray.GetPoint((y-ray.origin.y)/ray.direction.y)-Vector3.up*height;}
        /// <summary>A hero on the screen, feet to head, as wide as a body.</summary>
        static Rect Body(CombatActor hero)
        {
            var camera=Camera.main;var at=hero.transform.position;
            Vector2 feet=camera.WorldToScreenPoint(at),head=camera.WorldToScreenPoint(at+Vector3.up*(hero.flight?.5f:1.9f));float half=Mathf.Abs(camera.WorldToScreenPoint(at+camera.transform.right*.6f).x-feet.x);
            return Rect.MinMaxRect(feet.x-half,Mathf.Min(feet.y,head.y),feet.x+half,Mathf.Max(feet.y,head.y));
        }
        /// <summary>A check run on the layout as it is: it must find nothing. Each was shown failing first on the November 20 layout (behind a switch set only by run ui-fight3,
        /// since removed): 44 faults for the bark, 8 for the prompt, 3 for the Picket's panel, 1 each for the lock card, the off-screen card and the cut-in, 3 for the numbers.</summary>
        static IEnumerator Now(string fault,System.Func<List<string>,IEnumerator> check)
        {
            var after=new List<string>();yield return check(after);
            Assert.IsFalse(after.Any(f=>f.Contains("fixture:")),string.Join(" | ",after.Where(f=>f.Contains("fixture:")).Distinct()));
            Assert.IsEmpty(after,fault+":\n"+string.Join("\n",after.Distinct()));
        }
        static readonly string[] BarParts={"BossBand","BossName","BossEpithet","BossIntegrityBack","BossBreakBack","BossState"};

        // ------------------------------------------------------------------ blocker: the prompt a fight asks for, and the boss bar
        IEnumerator PromptOverTheBar(string zone,string boss,List<string> faults)
        {
            yield return Zone(zone);Guard();var hero=Hero;
            if(hero.TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            var fight=Boss(boss,hero.flight?hero.transform.position+new Vector3(20,0,0):Ahead(14));if(hero.flight)fight.enabled=true;
            var dock=Prompt("Dock — The west clamp's works",hero.transform.position);
            for(float end=Time.unscaledTime+2.6f;Time.unscaledTime<end;)yield return null;
            var words=Text("Interaction");var plate=Picture("InteractionPlate");
            if(!Text("BossName").isActiveAndEnabled)faults.Add($"{zone}: fixture: no boss bar for {boss}");
            if(!words.text.Contains("west clamp"))faults.Add($"{zone}: fixture: the fight's own prompt isn't up with the bar: '{words.text}'");
            else
            {
                var at=Box(plate.rectTransform);
                foreach(string part in BarParts)
                {
                    var graphic=(Graphic)Text(part)??Picture(part);if(graphic==null||!graphic.isActiveAndEnabled)continue;
                    if(at.Overlaps(Box(graphic.rectTransform)))faults.Add($"{zone}: the prompt's plate {at} lies on the bar's {part} {Box(graphic.rectTransform)}");
                }
                foreach(var member in PartyController.Current.members)if(at.Overlaps(Body(member)))faults.Add($"{zone}: the prompt lifted over the bar lies on {member.character}");
            }
            fight.Passive=true;Object.Destroy(fight.gameObject);Object.Destroy(dock.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator APromptAFightAsksForNeverSharesAPlaceWithTheBossBar()
        {
            yield return Now("the prompt on the boss bar",f=>Both(f));
            IEnumerator Both(List<string> faults){yield return PromptOverTheBar("Arena_Flight","Cantor",faults);yield return PromptOverTheBar("Arena_Ground","HessaKorl",faults);}
        }

        // ------------------------------------------------------------------ blocker: a bark comes down its column onto the bar
        /// <summary>Lanes across the bark's column from the feed's foot down to just over the bar: the only room left in it is the bar's own band and under it.</summary>
        static List<Telegraph> ShutTheColumn(Rect plate,float barTop)
        {
            var lanes=new List<Telegraph>();float scale=Hud.GetComponentInChildren<Canvas>().scaleFactor;
            for(float y=barTop+30*scale;y<Screen.height-330*scale;y+=34*scale)
            {
                Vector3 from=Under(new Vector2(plate.xMin-60,y)),to=Under(new Vector2(plate.xMax+60,y));var way=to-from;way.y=0;
                lanes.Add(Telegraph.Line(from,way.normalized,way.magnitude,1.2f,8,Telegraph.Threat));
            }
            return lanes;
        }
        IEnumerator BarkOntoTheBar(List<string> faults)
        {
            yield return Zone("Arena_Ground");Guard();
            var fight=Boss("HessaKorl",Ahead(14));var dock=Prompt("Launch — Sorrel's Second Sky",Hero.transform.position);
            for(float end=Time.unscaledTime+2.6f;Time.unscaledTime<end;)yield return null;
            Say("Taren","Shorter sweep. The humming's reaching her.");yield return null;yield return null;
            var plate=Picture("BarkPlate");var band=Picture("BossBand");
            if(!Up(plate))faults.Add("fixture: the bark isn't up");
            var lanes=ShutTheColumn(Box(plate.rectTransform),Box(band.rectTransform).yMax);
            for(float end=Time.unscaledTime+1.2f;Time.unscaledTime<end;)
            {
                yield return null;var at=Box(plate.rectTransform);
                foreach(string other in new[]{"BossBand","InteractionPlate","SkillsPlate"})
                {
                    var graphic=Picture(other);if(!Up(graphic))continue;
                    if(at.Overlaps(Box(graphic.rectTransform,2)))faults.Add($"the bark {at} came down onto {other} {Box(graphic.rectTransform)}");
                }
                foreach(string other in BarParts.Skip(1))
                {
                    var graphic=(Graphic)Text(other)??Picture(other);if(!Up(graphic))continue;
                    if(at.Overlaps(Box(graphic.rectTransform)))faults.Add($"the bark {at} lies on the bar's {other}");
                }
            }
            foreach(var lane in lanes)if(lane!=null)Object.Destroy(lane.gameObject);
            fight.Passive=true;Object.Destroy(fight.gameObject);Object.Destroy(dock.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator ABarkNeverComesDownOntoTheBossBarThePromptOrTheHintBar()
        {
            yield return Now("a bark on the boss bar",BarkOntoTheBar);
        }

        // ------------------------------------------------------------------ blocker: the Picket's panel and the feed under the vitals
        IEnumerator UnderTheVitals(List<string> faults)
        {
            yield return Zone("PicketOfTeeth");Guard();yield return Settle(.6f);
            var vitals=Box(Picture("VitalsFrame").rectTransform);var gauge=Picture("PicketFrame");var resting=Text("Resting");
            if(!Up(gauge)){faults.Add("fixture: the Picket's panel isn't up");yield break;}
            if(!resting.isActiveAndEnabled||resting.text.Length==0){faults.Add("fixture: no third hero resting");yield break;}
            var panel=Box(gauge.rectTransform);
            if(panel.Overlaps(vitals))faults.Add($"the Picket's panel {panel} stands up into the vitals {vitals}");
            var restingInk=Ink(resting);
            if(restingInk.Overlaps(panel))faults.Add($"the Picket's panel cuts '{resting.text}' {restingInk}");
            if(!vitals.Contains(restingInk.min)||!vitals.Contains(restingInk.max))faults.Add($"'{resting.text}' {restingInk} isn't inside the vitals {vitals}");
            // The feed's lines, 'Saved' among them, hang under whatever is under the vitals.
            foreach(string id in new[]{"ScrapAlloy","LatticeFilament","RidgeCrystal","HuskCore"})RpgServices.Inventory.Grant(id,1);
            typeof(PickupFeed).GetMethod("OnSaved",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(Object.FindFirstObjectByType<PickupFeed>(),new object[]{"autosave"});
            Say("Idra","Port collar. Outer bolts first.");
            yield return null;yield return null;yield return Settle(.5f);
            var feed=Picture("PickupBand");
            if(!Up(feed))faults.Add("fixture: the feed isn't up");
            else
            {
                var lines=Box(feed.rectTransform,6);
                if(lines.Overlaps(panel))faults.Add($"the feed {lines} prints over the Picket's panel {panel}");
                if(lines.Overlaps(Box(Picture("VitalsFrame").rectTransform,12)))faults.Add("the feed prints over the vitals");
                var bark=Box(Picture("BarkPlate").rectTransform,2);
                foreach(var (name,box) in new[]{("the feed",lines),("the Picket's panel",panel),("the vitals",vitals)})if(bark.Overlaps(box))faults.Add($"the bark {bark} lies on {name} {box}");
            }
        }
        static Rect Ink(TMP_Text text){var local=UiKit.Ink(text);Vector3 lo=text.transform.TransformPoint(new Vector3(local.xMin,local.yMin)),hi=text.transform.TransformPoint(new Vector3(local.xMax,local.yMax));return Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);}
        [UnityTest] public IEnumerator AZonesPanelAndTheFeedHangUnderTheVitalsRealFoot()
        {
            yield return Now("the Picket's panel on the vitals",UnderTheVitals);
            yield return Still("picket-feed-bark");
        }

        // ------------------------------------------------------------------ major: a boss's lock card names her
        IEnumerator BossNamed(string id,List<string> faults)
        {
            Guard();var boss=Boss(id,Ahead(14));var controller=boss.GetComponent<BossController>();
            for(float end=Time.unscaledTime+2.4f;Time.unscaledTime<end;)yield return null;
            Hero.target=boss.Health;Hero.TargetLocked=true;yield return null;yield return null;yield return null;
            var card=Text("Target").text;
            if(!Text("BossName").isActiveAndEnabled)faults.Add($"fixture: {id} has no bar up");
            else if(!card.Contains(controller.DisplayName.ToUpperInvariant())||!Regex.IsMatch(card,@"\d+/\d+"))faults.Add($"{id}'s lock card doesn't name her and her integrity: '{card.Replace("\n"," / ")}'");
            Hero.target=null;Hero.TargetLocked=false;boss.Passive=true;Object.Destroy(boss.gameObject);yield return null;yield return null;
            foreach(var left in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))if(left!=null&&!left.Passive)Object.Destroy(left.gameObject);
            yield return null;
        }
        [UnityTest] public IEnumerator EveryBossFoughtOnFootIsNamedOnItsLockCard()
        {
            yield return Now("a boss's lock card without her name",f=>BossNamed("HessaKorl",f));
            var faults=new List<string>();
            foreach(string id in ContentRegistry.Bosses.Where(b=>!ContentRegistry.Boss(b).flight))yield return BossNamed(id,faults);
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        // ------------------------------------------------------------------ major: no card for a body nowhere on the screen
        IEnumerator OffTheScreen(List<string> faults)
        {
            Guard();var camera=Camera.main;
            var guard=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),Under(new Vector2(camera.pixelWidth*.5f,-camera.pixelHeight*.35f)));guard.Passive=true;
            yield return null;
            var seen=camera.WorldToViewportPoint(guard.transform.position+Vector3.up);
            if(seen.y>=0&&seen.y<=1&&seen.z>0)faults.Add("fixture: the body is on the screen");
            if((guard.transform.position-Hero.transform.position).sqrMagnitude>900)faults.Add("fixture: the body is out of the aim's reach");
            Hero.target=guard.Health;Hero.TargetLocked=false;yield return null;yield return null;yield return null;
            if(Text("Target").text.Length>0)faults.Add($"with the aim on a body nowhere on the screen the card says '{Text("Target").text.Replace("\n"," / ")}'");
            // Locked, it is the player's own choice, and it stays.
            Hero.TargetLocked=true;yield return null;yield return null;
            if(!Text("Target").text.Contains("RIDGEHOUND"))faults.Add("a lock on a body off the screen lost its card");
            Hero.target=null;Hero.TargetLocked=false;Object.Destroy(guard.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator TheCardShowsOnlyATargetThatCanBeSeenOrIsLocked()
        {
            yield return Now("a card for a body nowhere on the screen",OffTheScreen);
        }

        // ------------------------------------------------------------------ major: a bark under an EXTRA's cut-in
        IEnumerator UnderTheCutIn(List<string> faults)
        {
            Say("Taren","Stand clear. This is the loud part.");yield return null;yield return null;
            var cutIn=Object.FindFirstObjectByType<ExtraCutIn>();var barks=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.name=="Barks");
            typeof(ExtraCutIn).GetMethod("Play",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(cutIn,new object[]{Hero,SkillBook.Equipped(Hero.character,0)});
            yield return null;yield return null;
            if(!cutIn.Showing)faults.Add("fixture: the cut-in isn't up");
            if(barks.enabled&&Text("Bark").text.Length>0)faults.Add("the bark shows under the EXTRA's band and portrait");
            for(float end=Time.unscaledTime+1.5f;cutIn.Showing&&Time.unscaledTime<end;)yield return null;
            yield return null;yield return null;
            if(!barks.enabled||Text("Bark").text.Length==0)faults.Add("the bark didn't come back after the cut-in");
        }
        [UnityTest] public IEnumerator ABarkStandsAsideForAnExtrasCutIn()
        {
            yield return Now("a bark under the cut-in",UnderTheCutIn);
        }

        // ------------------------------------------------------------------ major: the title card has one place, clear of the heroes and the bar
        IEnumerator Card(string doing,List<string> faults)
        {
            var card=Text("BossTitleCard");var line=Text("BossTitleEpithet");var rule=Picture("BossTitleRule");
            if(card.alpha<.5f){faults.Add(doing+": fixture: the card isn't up");yield break;}
            foreach(var piece in new Graphic[]{card,line,rule})
            {
                if(piece.rectTransform.anchorMin!=new Vector2(.5f,1))faults.Add($"{doing}: {piece.name} isn't in the card's one place at the top");
                var at=piece is TMP_Text words?Ink(words):Box(piece.rectTransform);
                foreach(var member in PartyController.Current.members)if(at.Overlaps(Body(member)))faults.Add($"{doing}: {piece.name} lies on {member.character}");
                foreach(string part in BarParts){var graphic=(Graphic)Text(part)??Picture(part);if(Up(graphic)&&at.Overlaps(Box(graphic.rectTransform)))faults.Add($"{doing}: {piece.name} lies on the bar's {part}");}
                foreach(string plate in new[]{"VitalsFrame","ObjectiveFrame","ZonePlate"}){var graphic=Picture(plate);if(Up(graphic)&&at.Overlaps(Box(graphic.rectTransform)))faults.Add($"{doing}: {piece.name} lies on {plate}");}
            }
            if(Text("BossName").alpha>.01f)faults.Add($"{doing}: the bar shows the same name under the card");
            if(Text("Target").text.Length>0)faults.Add($"{doing}: the target card stands in the title card's place");
            yield return null;
        }
        [UnityTest] public IEnumerator TheTitleCardHasOnePlaceClearOfTheHeroesAndTheBar()
        {
            var faults=new List<string>();Guard();
            // Close by, far up the screen (the camera carries the party down), and a flight's.
            foreach(var (metres,doing) in new[]{(4f,"a boss close by"),(14f,"a boss far up the screen")})
            {
                // The aim on her, unlocked: a lock ends the card at once (QaHudThirdTests), the plain aim's card waits for it.
                var at=Ahead(metres);var boss=Boss("HessaKorl",at);Hero.target=boss.Health;Hero.TargetLocked=false;
                for(float end=Time.unscaledTime+.7f;Time.unscaledTime<end;){boss.transform.position=at;Physics.SyncTransforms();yield return null;}
                yield return Card(doing,faults);
                if(metres>10)yield return Still("title-card-far");
                for(float end=Time.unscaledTime+2;Time.unscaledTime<end;){boss.transform.position=at;Physics.SyncTransforms();yield return null;}
                if(Text("BossName").alpha<.99f)faults.Add($"{doing}: the bar's name never comes back after the card");
                if(!Text("Target").text.Contains("HESSA KORL"))faults.Add($"{doing}: the target card never comes back after the title card");
                Hero.target=null;Hero.TargetLocked=false;boss.Passive=true;Object.Destroy(boss.gameObject);yield return Settle(.6f);
            }
            yield return Zone("Arena_Flight");Guard();var ship=Hero;ship.GetComponent<FlightMotor>().Halt();
            var cantor=Boss("Cantor",ship.transform.position+new Vector3(20,0,0));cantor.enabled=true;yield return Settle(.5f);
            yield return Card("a flight's",faults);
            cantor.Passive=true;Object.Destroy(cantor.gameObject);
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        // ------------------------------------------------------------------ minors: numbers
        static Health Dummy(Vector3 at,float integrity=1000000)
        {var body=new GameObject("Body fixture").AddComponent<Health>();body.id="Fixture";body.maximum=body.integrity=integrity;body.breakThreshold=1e9f;body.transform.position=at;return body;}
        static TMP_Text[] Numbers()=>Hud.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="Damage"&&t.alpha>.01f).ToArray();
        IEnumerator NumbersRead(List<string> faults)
        {
            var plain=System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=DamageType.Plasma&&t!=DamageType.Kinetic);
            var body=Dummy(Ahead(5));
            // A swing's rhythm on one body is one running number: '48 48 48' (s14 169).
            for(int hit=0;hit<3;hit++){body.Receive(new DamagePacket{amount=48,type=plain});yield return new WaitForSecondsRealtime(.36f);}
            var up=Numbers();if(up.Length!=1)faults.Add($"three swings .36 s apart on one body stand as {up.Length} numbers: "+string.Join(" ",up.Select(n=>n.text)));
            Object.Destroy(body.gameObject);yield return Settle(1.3f);
            // Nothing is no hit to show: '0!!' (s41 036).
            var plate=Dummy(Ahead(5));plate.Receive(new DamagePacket{amount=0.2f,type=plain});yield return null;
            if(Numbers().Any(n=>n.text.StartsWith("0")))faults.Add("a hit worth nothing is shown as '0'");
            Object.Destroy(plate.gameObject);yield return Settle(1.3f);
            // A body that's down keeps its numbers only a moment (s52 177).
            var skiff=Dummy(Ahead(5),100);skiff.Receive(new DamagePacket{amount=60,type=plain});yield return new WaitForSecondsRealtime(.2f);
            skiff.Receive(new DamagePacket{amount=500,type=plain});Assert.IsFalse(skiff.Alive,"fixture: the body is down");
            yield return new WaitForSecondsRealtime(.75f);
            if(Numbers().Length>0)faults.Add("a beaten body's numbers are still up three quarters of a second after it went down");
            Object.Destroy(skiff.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator NumbersAddUpOnOneBodyShowNoZerosAndGoWithTheBody()
        {
            yield return Now("piled, zero or lingering numbers",NumbersRead);
        }

        // ------------------------------------------------------------------ minor: a faded plate's words
        [UnityTest] public IEnumerator AFadedPlatesWordsLetThroughNoMoreThanATwentiethOfTheWorld()
        {
            Guard();yield return Settle();
            var frame=Picture("ObjectiveFrame");var words=Text("Objective");
            var at=Under(Box(frame.rectTransform).center,.75f);var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),at);hound.Passive=false;
            for(float end=Time.unscaledTime+.6f;Time.unscaledTime<end;){hound.transform.position=at;Physics.SyncTransforms();yield return null;}
            float plate=frame.canvasRenderer.GetAlpha();var band=Picture("ObjectiveWordsBand");
            Assert.Less(plate,.42f,"fixture: an awake enemy is behind the objective");
            Assert.IsTrue(band!=null&&band.isActiveAndEnabled,"the words have nothing of their own behind them");
            // The canvas blends in linear light: what reaches the eye behind a letter is what both the plate and the band let through.
            float through=(1-plate*frame.color.a)*(1-band.canvasRenderer.GetAlpha()*band.color.a);
            Assert.LessOrEqual(through,.06f,$"behind the words the plate and their band let {through:P0} of the world through: white type on a white hull, pale ice or a printed board reads grey on grey");
            hound.Passive=true;Object.Destroy(hound.gameObject);
        }

        // ------------------------------------------------------------------ the stills for the lead
        /// <summary>A fight with a boss bar up, the fight's own prompt over it, a bark pushed down its column, the feed full and three heroes in the party, at 1920 by 1080.</summary>
        [UnityTest] public IEnumerator StillsOfTheHudInAFight()
        {
            Guard();var fight=Boss("HessaKorl",Ahead(12));var dock=Prompt("Dock — The west clamp's works",Hero.transform.position);
            for(float end=Time.unscaledTime+.6f;Time.unscaledTime<end;)yield return null;
            yield return Still("fight-title-card");
            for(float end=Time.unscaledTime+2.2f;Time.unscaledTime<end;)yield return null;
            Hero.target=fight.Health;Hero.TargetLocked=true;
            foreach(string id in new[]{"ScrapAlloy","LatticeFilament","RidgeCrystal","HuskCore"})RpgServices.Inventory.Grant(id,2);
            Say("Taren","Shorter sweep. The humming's reaching her.");yield return null;yield return null;
            var lanes=ShutTheColumn(Box(Picture("BarkPlate").rectTransform),Box(Picture("BossBand").rectTransform).yMax);
            var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),Ahead(6)+Vector3.right*3);hound.Passive=true;
            for(int i=0;i<3;i++){hound.Health.Receive(new DamagePacket{amount=48,type=DamageType.Beam});yield return new WaitForSecondsRealtime(.3f);}
            yield return Settle(.3f);
            yield return Still("fight-prompt-bark-feed");
            TestContext.WriteLine($"HUD_STILL screen={Screen.width}x{Screen.height} prompt={Box(Picture("InteractionPlate").rectTransform)} band={Box(Picture("BossBand").rectTransform)} bark={Box(Picture("BarkPlate").rectTransform)} card='{Text("Target").text.Replace("\n"," / ")}'");
            foreach(var lane in lanes)if(lane!=null)Object.Destroy(lane.gameObject);
            fight.Passive=true;Object.Destroy(fight.gameObject);Object.Destroy(dock.gameObject);Object.Destroy(hound.gameObject);
        }
        /// <summary>What the game's camera draws this frame with every canvas over it, as a 1920 by 1080 still in the mirror's Builds/quality/focused/ui-fight.</summary>
        static IEnumerator Still(string name)
        {
            // A frame, not the end of one: in a batch run there is no end-of-frame to wait for and the test never comes back.
            yield return null;
            const int W=1920,H=1080;var camera=Camera.main;Assert.IsNotNull(camera,"fixture: no game camera");
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay&&c.enabled).ToArray();
            var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var image=new Texture2D(W,H,TextureFormat.RGB24,false);var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;
                foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=camera.nearClipPlane+.1f+.01f*(100-c.sortingOrder);}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/focused/ui-fight"));Directory.CreateDirectory(folder);
                string file=Path.Combine(folder,name+".png");File.WriteAllBytes(file,image.EncodeToPNG());TestContext.WriteLine("HUD_STILL "+file+$" (screen {Screen.width}x{Screen.height})");
            }
            finally
            {
                foreach(var c in canvases)if(c!=null){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}
                camera.targetTexture=target;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(image);
            }
        }
    }
}
