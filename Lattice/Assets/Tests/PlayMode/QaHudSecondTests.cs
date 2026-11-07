using System.Collections;
using System.Collections.Generic;
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
    /// <summary>A blow landing where real ones land: MeleeContact swings in LateUpdate, at 700.</summary>
    [DefaultExecutionOrder(700)]
    public sealed class HudSecondLateBlow:MonoBehaviour
    {
        public Health body,from;
        void LateUpdate(){if(body==null)return;body.Receive(QaHudSecondTests.Blow(from));body=null;}
    }
    /// <summary>What the frame's own picture shows: after the blow and the HUD, before it's drawn.</summary>
    [DefaultExecutionOrder(3000)]
    public sealed class HudSecondFrameReader:MonoBehaviour
    {
        public string plate;public bool bar,read;
        void LateUpdate(){if(read)return;read=true;plate=QaHudSecondTests.Text("Target").text;bar=QaHudSecondTests.Text("BossState").isActiveAndEnabled;}
    }
    /// <summary>The second eye pass on the HUD. A panel that stepped aside took its words with it, for reasons that weren't reasons; signs lay through and under the plates; the arrow read
    /// as a triangle aimed elsewhere. These measure what makes each of those right. Whether it looks right is a question for the next capture. Every check here fails on the build it was written against.</summary>
    public sealed class QaHudSecondTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();PromptService.ForceDevice(null);SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return Zone("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var dialogue=DialogueSystem.Current;
            if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();
            var shop=Object.FindFirstObjectByType<ShopUi>();if(shop!=null&&shop.IsOpen)shop.Close();
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
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
        static GameState State=>GameServices.Current.State;
        static CombatActor Hero=>PartyController.Current.Active;
        static CombatActor Partner{get{var party=PartyController.Current;return party.members[1-party.index];}}
        static void Flag(string key,bool on=true)=>GameServices.Current.Flags.SetBool(key,on);
        static IEnumerator Settle(){yield return new WaitForSecondsRealtime(.4f);}
        internal static TMP_Text Text(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static Image Picture(string name)=>Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(i=>i.name==name);
        static TMP_Text[] Live(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t=>t.name==name).ToArray();
        static Rect Box(RectTransform rect,float inset=0)
        {var c=new Vector3[4];rect.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x+inset,c[0].y+inset,c[2].x-inset,c[2].y-inset);}
        static void Put(CombatActor hero,Vector3 at)
        {
            var body=hero.GetComponent<CharacterController>();body.enabled=false;
            if(hero.TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            hero.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static EnemyBrain Spawn(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        static EnemyBrain Awake(Vector3 at){var enemy=Spawn("Ridgehound",at);enemy.Passive=false;return enemy;}
        static void Guard(){Hero.Health.InvulnerableUntil=Partner.Health.InvulnerableUntil=GameTime.Now+90;}
        sealed class FixturePrompt:InteractionPrompt{public override void Interact(){}}
        /// <summary>What a graphic is really drawn at: its renderer's own alpha and every group over it.</summary>
        static float Shown(Graphic graphic)
        {
            float alpha=graphic.canvasRenderer.GetAlpha();
            foreach(var group in graphic.GetComponentsInParent<CanvasGroup>(true))alpha*=group.alpha;
            return alpha;
        }
        static float Plate(string image)=>Shown(Picture(image));
        static void Solid(string name,string doing,List<string> failures)
        {
            var words=Text(name);if(words==null){failures.Add("fixture: no "+name);return;}
            if(words.alpha*Shown(words)<.99f)failures.Add($"{name} goes down to {words.alpha*Shown(words):0.00} {doing}: the plate should step aside, not the words");
        }
        /// <summary>The ground under a screen point, for a body whose middle stands this high above the hero's feet.</summary>
        static Vector3 Under(Vector2 screen,float height)
        {
            var ray=Camera.main.ScreenPointToRay(screen);float y=Hero.transform.position.y+height;return ray.GetPoint((y-ray.origin.y)/ray.direction.y)-Vector3.up*height;
        }
        static Vector3 Behind(RectTransform panel,float height)=>Under(Box(panel).center,height);
        static IEnumerator Hold(EnemyBrain enemy,Vector3 at,float seconds)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){enemy.transform.position=at;Physics.SyncTransforms();yield return null;}}
        static IEnumerator HoldBehind(CombatActor hero,RectTransform panel,float seconds)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){Put(hero,Behind(panel,1));yield return null;}}

        // ------------------------------------------------------------------ item 1: the arrow
        [UnityTest] public IEnumerator TheObjectiveArrowHasOneSharpEndAndItsTipReachesFurthest()
        {
            yield return null;
            var shape=UiSkin.PointerOutline;int tip=0;for(int i=0;i<shape.Length;i++)if(shape[i].y>shape[tip].y)tip=i;
            Assert.AreEqual(0,shape[tip].x,.001f,"the arrow's tip isn't on its axis: it points off from where it's turned");
            for(int i=0;i<shape.Length;i++)if(i!=tip)Assert.GreaterOrEqual(shape[tip].magnitude,shape[i].magnitude*1.25f,$"a wing at {shape[i]} reaches about as far from the middle as the tip: turned, it reads as a triangle aimed elsewhere");
            float area=0;for(int i=0;i<shape.Length;i++){var a=shape[i];var b=shape[(i+1)%shape.Length];area+=a.x*b.y-b.x*a.y;}
            for(int i=0;i<shape.Length;i++)
            {
                Vector2 before=shape[(i+shape.Length-1)%shape.Length]-shape[i],after=shape[(i+1)%shape.Length]-shape[i];
                bool outward=(after.x*before.y-after.y*before.x)*area>0;if(!outward)continue;
                float angle=Vector2.Angle(before,after);
                if(i==tip)Assert.Less(angle,50,"the tip isn't sharp");else Assert.Greater(angle,70,$"the point at {shape[i]} is as sharp as the tip: the arrow has more than one sharp end");
            }
            float low=shape.Min(p=>p.y),high=shape.Max(p=>p.y),wide=shape.Max(p=>p.x)-shape.Min(p=>p.x),length=high-low;
            Assert.GreaterOrEqual(length/wide,1.5f,"the arrow isn't long and slim: it's a triangle");
            var notch=shape.Where((p,i)=>i!=tip&&Mathf.Abs(p.x)<.001f).ToArray();Assert.IsEmpty(notch,"the arrow has a notch on its axis: at this size it forks into a V or a Y, and turned up and left it's a mouse pointer");
            // Still drawn at the type floor, the small one too: it sits on a line of type and has to read beside it.
            foreach(string name in new[]{"ObjectiveArrow","ObjectiveAlsoArrow"})
            {
                var arrow=Picture(name);Assert.IsNotNull(arrow,"fixture: "+name);
                Assert.GreaterOrEqual(arrow.rectTransform.rect.height*length*.5f,24,name+" is drawn smaller than the type floor");
            }
            Assert.Greater(Picture("ObjectiveArrow").rectTransform.rect.height,Picture("ObjectiveAlsoArrow").rectTransform.rect.height,"the main arrow is no larger than the dim line's");
        }

        // ------------------------------------------------------------------ item 2: the plate steps aside, not the words
        [UnityTest] public IEnumerator APanelsWordsAndBarsStaySolidWhileItsPlateStepsAside()
        {
            var hero=Hero;var partner=Partner;Guard();
            yield return Settle();
            var failures=new List<string>();
            // In a fight, the partner behind the vitals.
            var fight=Awake(hero.transform.position+Vector3.left*6+Vector3.back*3);
            yield return HoldBehind(partner,Picture("VitalsFrame").rectTransform,.3f);
            Assert.Less(Plate("VitalsFrame"),.42f,"fixture: the partner is behind the vitals in a fight and the plate stays solid");
            foreach(string name in new[]{"Status","IntegrityLabel","ChargeLabel","ThrustLabel","Partner"})Solid(name,"while the vitals' plate steps aside",failures);
            foreach(string name in new[]{"IntegrityBack","Integrity","ChargeBack","Charge","ThrustBack","Thrust"})if(Plate(name)<.99f)failures.Add($"{name} goes down to {Plate(name):0.00} with the plate, so health dims just as it matters");
            Put(partner,hero.transform.position+Vector3.right*2);Object.Destroy(fight.gameObject);yield return null;
            // An awake enemy behind the objective, and the target behind its plate.
            var at=Behind(Picture("ObjectiveFrame").rectTransform,.75f);var hound=Awake(at);
            yield return Hold(hound,at,.3f);
            Assert.Less(Plate("ObjectiveFrame"),.42f,"fixture: an awake enemy is behind the objective");
            Solid("Objective","while the objective's plate steps aside",failures);
            Assert.AreEqual(1,Shown(Picture("ObjectiveArrow")),.01f,"the arrow goes down with the plate");
            hound.Passive=true;hero.target=hound.Health;hero.TargetLocked=true;yield return null;yield return null;
            at=Behind(Picture("TargetPlate").rectTransform,.75f);yield return Hold(hound,at,.3f);
            Assert.Less(Plate("TargetPlate"),.42f,"fixture: the locked target is behind its plate");
            Solid("Target","while the target's plate steps aside",failures);
            Assert.IsEmpty(failures,string.Join("\n",failures));
            Object.Destroy(hound.gameObject);
        }
        [UnityTest] public IEnumerator WhatAPanelSaysHoldsWithoutThePanel()
        {
            Notices.Post("QUEST UPDATED","A quest's name\nThe next step of it");
            yield return new WaitForSecondsRealtime(.3f);
            var failures=new List<string>();
            foreach(string name in new[]{"Status","IntegrityLabel","ChargeLabel","ThrustLabel","Partner","Objective","QuestToastHeading","QuestToastBody","QuestToastHint"})
            {
                var words=Text(name);if(words==null){failures.Add("fixture: no "+name);continue;}
                var material=words.fontSharedMaterial;
                if(material.GetFloat("_OutlineWidth")<.3f||material.GetFloat("_FaceDilate")<.3f||!material.IsKeywordEnabled("UNDERLAY_ON"))failures.Add($"{name} has no heavy style: with its plate stepped aside it lies on the floor in thin type");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator APartnerBehindAPanelOnACalmWalkLeavesItAlone()
        {
            var partner=Partner;Guard();yield return Settle();
            foreach(string image in new[]{"ObjectiveFrame","VitalsFrame"})
            {
                yield return HoldBehind(partner,Picture(image).rectTransform,.3f);
                Assert.Greater(Plate(image),.98f,image+" fades for the partner on a calm walk, with nobody to fight");
            }
            BarkService.Play("Taren","swap",true);yield return null;yield return null;yield return null;
            Assert.IsTrue(Picture("BarkPlate").enabled,"fixture: the bark is up");
            yield return HoldBehind(partner,Picture("BarkPlate").rectTransform,.3f);
            Assert.Greater(Plate("BarkPlate"),.98f,"the bark's plate fades for the partner on a calm walk");
        }
        [UnityTest] public IEnumerator ABodyBesideAPlateDoesNotFadeIt()
        {
            Guard();yield return Settle();
            var frame=Picture("ObjectiveFrame").rectTransform;var box=Box(frame);float units=frame.lossyScale.x;
            // Thirty units off its left edge, level with its middle.
            var beside=Under(new Vector2(box.xMin-30*units,box.center.y),.75f);var hound=Awake(beside);
            yield return Hold(hound,beside,.4f);
            Assert.Greater(Plate("ObjectiveFrame"),.98f,"the objective fades for a body merely near it");
            // And behind it, it goes: the rule hasn't simply been switched off.
            var behind=Behind(frame,.75f);yield return Hold(hound,behind,.3f);
            Assert.Less(Plate("ObjectiveFrame"),.42f,"an awake enemy is really behind the objective and the plate stays solid");
            // A body's top half counts as well as its middle: one with its upper half behind the vitals left them solid.
            var vitals=Box(Picture("VitalsFrame").rectTransform);var feet=Under(new Vector2(vitals.center.x,vitals.yMin-4*units),.75f);
            yield return Hold(hound,feet,.3f);
            Assert.Less(Plate("VitalsFrame"),.42f,"an enemy stands with its top half behind the vitals and the plate stays solid");
            Object.Destroy(hound.gameObject);
        }
        [UnityTest] public IEnumerator TheBossBarKeepsItsColourAndItsOpeningWhileItsBandSteps()
        {
            var hero=Hero;var partner=Partner;Guard();
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),hero.transform.position+Vector3.forward*14);boss.Passive=false;
            yield return new WaitForSecondsRealtime(.3f);
            boss.Health.Expose(3);yield return null;yield return null;
            var fill=Picture("BossIntegrity");var state=Text("BossState");Color red=fill.color;Color gold=state.color;
            StringAssert.Contains("OPENING",state.text,"fixture: an opening");
            yield return HoldBehind(partner,Picture("BossBand").rectTransform,.3f);
            Assert.Less(Plate("BossBand"),.7f,"fixture: the partner is behind the band in a fight");
            Assert.AreEqual(1,Shown(fill),.01f,"the bar's fill fades with the band and turns salmon, which reads as another state");
            Assert.AreEqual(red,fill.color,"the bar's fill changes colour");
            Assert.AreEqual(1,Shown(state)*state.alpha,.01f,"OPENING goes dull just as it says to strike");
            Assert.AreEqual(gold,state.color,"OPENING changes colour");
            boss.Passive=true;Object.Destroy(boss.gameObject);
        }
        [UnityTest] public IEnumerator TheBarkAndThePromptFollowTheSameRule()
        {
            var hero=Hero;Guard();yield return Settle();
            var fixture=new GameObject("Prompt fixture").AddComponent<FixturePrompt>();fixture.prompt="Finish the walkway repair";fixture.range=6;fixture.transform.position=hero.transform.position;
            BarkService.Play("Taren","swap",true);yield return null;yield return null;yield return null;
            Assert.IsTrue(Picture("InteractionPlate").enabled&&Picture("BarkPlate").enabled,"fixture: the prompt and the bark are up");
            var bark=Picture("BarkPlate");
            Assert.That(bark.color.a,Is.InRange(.8f,.9f),"a bark's plate either goes muddy on a bright floor, with the slab showing through it on a dark one, or is a solid box lying on the very tender Orrin is saying is free");
            var at=Behind(Picture("InteractionPlate").rectTransform,.75f);var hound=Awake(at);
            yield return Hold(hound,at,.3f);
            Assert.Less(Plate("InteractionPlate"),.42f,"an awake enemy is behind the prompt and its plate stays solid");
            var failures=new List<string>();Solid("Interaction","while the prompt's plate steps aside",failures);
            at=Behind(bark.rectTransform,.75f);yield return Hold(hound,at,.3f);
            Assert.Less(Plate("BarkPlate"),.42f,"an awake enemy is behind the bark and its plate stays solid");
            Solid("Bark","while the bark's plate steps aside",failures);
            Assert.IsEmpty(failures,string.Join("\n",failures));
            hound.Passive=true;Object.Destroy(hound.gameObject);Object.Destroy(fixture.gameObject);
        }

        // ------------------------------------------------------------------ item 3: signs and panels
        [UnityTest] public IEnumerator TheDialogueBoxAndTheMenusAreFullyOpaque()
        {
            yield return null;
            var panel=Object.FindFirstObjectByType<DialoguePanel>();panel.Show("Hal",PortraitEmotion.Neutral,null,"Repair and save, or just visiting?");panel.SetVisibleCharacters(int.MaxValue);yield return null;
            var frame=Picture("DialoguePanel");var backing=Picture("DialogueBacking");
            Assert.IsNotNull(backing,"the box's art is 254 of 255 opaque, and the canvas blends in linear: on so dark a plate a bright sign reads through as a ghost");
            Assert.IsTrue(backing.isActiveAndEnabled,"the backing isn't up with the box");
            Assert.AreEqual(1,backing.color.a,.001f,"the backing lets the world through");
            var drawn=frame.canvas.GetComponentsInChildren<Graphic>().ToList();
            Assert.Less(drawn.IndexOf(backing),drawn.IndexOf(frame),"the backing is drawn over the box");
            float units=frame.rectTransform.lossyScale.x;var opening=Box(frame.rectTransform,31*units);var under=Box(backing.rectTransform);
            Assert.IsTrue(under.xMin<=opening.xMin&&under.xMax>=opening.xMax&&under.yMin<=opening.yMin&&under.yMax>=opening.yMax,"the backing doesn't reach the edges of the box's opening");
            // The frame's corners are cut: first solid 20 to 22 units in along the diagonal, so a backing over that would show past them.
            var outer=Box(frame.rectTransform,22*units);
            Assert.IsTrue(under.xMin>=outer.xMin&&under.xMax<=outer.xMax&&under.yMin>=outer.yMin&&under.yMax<=outer.yMax,"the backing shows past the frame's cut corners");
            panel.Hide();yield return null;
            Assert.IsFalse(backing.isActiveAndEnabled,"the backing stays on the screen with the box gone");
            // The world shows round them now, dimmed, which QaHudThirdTests pins. What their rows stand on lets nothing through.
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open();yield return null;
            Assert.AreEqual(1,Picture("MenuPanelBacking").color.a,.001f,"the pause menu's plate lets the world through: a bright sign read as a footer under every page");
            menu.Close();yield return null;yield return null;
            var shop=Object.FindFirstObjectByType<ShopUi>();shop.Open(GameCatalog.Find<ShopInventory>("Lanternwick"));yield return null;
            Assert.AreEqual(1,Picture("ShopPanelBacking").color.a,.001f,"the shop's plate lets the world through: a sign read as ghost letters between its rows");
            shop.Close();
        }
        static TextMeshPro[] Faded()=>Object.FindObjectsByType<SignFade>(FindObjectsSortMode.None).Select(f=>f.GetComponent<TextMeshPro>()).ToArray();
        static void AllGone(string doing,TextMeshPro[] signs)
        {foreach(var sign in signs)Assert.Less(sign.alpha,.05f,$"\"{sign.text}\" stays in the world {doing}");}
        static void AllBack(string doing,TextMeshPro[] signs)
        {foreach(var sign in signs)Assert.Greater(sign.alpha,.9f,$"\"{sign.text}\" doesn't come back {doing}");}
        [UnityTest] public IEnumerator SignsAndNamesLeaveForAConversationOrAMenuAndComeBack()
        {
            Flag("met.Keeper");yield return Zone("TallowDrift");yield return Settle();yield return Settle();
            var names=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Where(n=>n.isActiveAndEnabled).SelectMany(n=>n.GetComponentsInChildren<TextMeshPro>()).Where(t=>t.GetComponent<Renderer>().enabled).ToArray();
            Assert.GreaterOrEqual(names.Length,1,"fixture: Tallow's people wear their names");
            foreach(var name in names)Assert.IsTrue(name.TryGetComponent<SignFade>(out _),$"{name.text}'s label doesn't step aside: a name prints over a conversation or gets cut by a plate");
            var all=Faded();Assert.GreaterOrEqual(all.Length,4,"fixture: signs and names");
            var showing=all.Where(s=>s.alpha>.9f).ToArray();Assert.GreaterOrEqual(showing.Length,2,"fixture: some are showing");
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open();yield return new WaitForSecondsRealtime(.35f);
            AllGone("behind the pause menu",all);
            menu.Close();yield return new WaitForSecondsRealtime(.5f);
            AllBack("after the pause menu",showing);
            var dialogue=DialogueSystem.Current;Assert.IsTrue(dialogue.StartNode("OrrinRepeat","Orrin"),"fixture: the conversation starts");
            yield return new WaitForSecondsRealtime(.35f);
            AllGone("through a conversation",all);
            dialogue.Cancel();yield return new WaitForSecondsRealtime(1f);
            AllBack("after a conversation",showing);
            var shop=Object.FindFirstObjectByType<ShopUi>();shop.Open(GameCatalog.Find<ShopInventory>("Lanternwick"));yield return new WaitForSecondsRealtime(.35f);
            AllGone("behind the shop",all);
            shop.Close();yield return new WaitForSecondsRealtime(.5f);
            AllBack("after the shop",showing);
        }
        /// <summary>A sign laid on the floor at a screen point. The real ones only differ in where the builders put them, and these can be walked under any plate with the camera at rest.</summary>
        static TextMeshPro FixtureSign()
        {
            var view=ZoneController.Current.definition.cameraProfile;
            var go=new GameObject("Sign_Fixture",typeof(TextMeshPro));go.transform.rotation=Quaternion.Euler(72,view.yaw,0);
            var text=go.GetComponent<TextMeshPro>();text.font=UiSkin.Display;text.text="FIXTURE SIGN";text.fontSize=UiKit.WorldFontSize(40);text.alignment=TextAlignmentOptions.Center;text.textWrappingMode=TextWrappingModes.NoWrap;
            go.AddComponent<SignFade>().bossOnly=true;return text;
        }
        static void Lay(TextMeshPro sign,Vector2 screen){sign.transform.position=Under(screen,.05f)+Vector3.up*.05f;}
        [UnityTest] public IEnumerator ASignCrossingAFixedPlateFadesWhileItCrosses()
        {
            var hero=Hero;var hound=Spawn("Ridgehound",hero.transform.position+Vector3.forward*8);yield return null;hero.target=hound.Health;hero.TargetLocked=true;
            yield return Settle();
            var camera=Camera.main;var sign=FixtureSign();var clear=new Vector2(camera.pixelWidth*.5f,camera.pixelHeight*.72f);
            Lay(sign,clear);yield return new WaitForSecondsRealtime(.4f);
            Assert.Greater(sign.alpha,.9f,"fixture: a sign on open floor shows");
            foreach(string plate in new[]{"SkillsPlate","Skill1","VitalsFrame","ObjectiveFrame","TargetPlate"})
            {
                var image=Picture(plate);Assert.IsTrue(image!=null&&image.isActiveAndEnabled,"fixture: "+plate+" is up");
                Lay(sign,Box(image.rectTransform).center);yield return new WaitForSecondsRealtime(.4f);
                Assert.Less(sign.alpha,.05f,$"a sign under {plate} stays: its words are cut by the plate's edge and run into what the plate says");
                Lay(sign,clear);yield return new WaitForSecondsRealtime(.4f);
                Assert.Greater(sign.alpha,.9f,$"the sign doesn't come back out from under {plate}");
            }
            // Half off the screen is the same fault, a word chopped to 'SORRE'.
            Lay(sign,new Vector2(0,camera.pixelHeight*.5f));yield return new WaitForSecondsRealtime(.4f);
            Assert.Less(sign.alpha,.05f,"a sign half off the screen's edge stays, cut to half a word");
            Object.Destroy(sign.gameObject);Object.Destroy(hound.gameObject);
        }
        [UnityTest] public IEnumerator ASignBesideTheHeroKeepsItsStrengthAndGoesOnlyUnderHim()
        {
            yield return Zone("Hushwell");
            var sign=Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.text=="BELLOWS CHAMBER");var lead=Hero;
            // Where the readers stood: 2 to 2.3 m from DRILL SITE and SUNKEN RELAY, with the word at a quarter of its strength.
            Put(lead,sign.transform.position+new Vector3(2.2f,.1f,0));yield return new WaitForSecondsRealtime(1f);
            Assert.Greater(sign.alpha,.9f,"a sign is a washed-out ghost with the hero merely beside it");
            var plate=sign.transform.Find("Sign plate");Assert.IsNotNull(plate,"fixture: the sign's plate");
            var block=new MaterialPropertyBlock();plate.GetComponent<Renderer>().GetPropertyBlock(block);
            if(block.HasFloat("_Fade"))Assert.Greater(block.GetFloat("_Fade"),.9f,"the sign's plate is washed out with the hero merely beside it");
            Put(lead,sign.transform.position+new Vector3(1,.1f,0));yield return new WaitForSecondsRealtime(.2f);
            Assert.Greater(sign.alpha,.6f,"a sign goes to a ghost a metre from the hero");
        }

        // ------------------------------------------------------------------ item 4: portraits
        [UnityTest] public IEnumerator TheBustFadesAtItsSidesAndNoStrayRowShowsOverIt()
        {
            yield return null;
            var panel=Object.FindFirstObjectByType<DialoguePanel>();var sets=Resources.LoadAll<PortraitSet>("Portraits");
            panel.Show("Sela",PortraitEmotion.Neutral,sets[0].entries[0].sprite,"Fixture line.");panel.SetVisibleCharacters(int.MaxValue);yield return null;
            var face=Picture("Portrait");var seat=face.GetComponentInParent<RectMask2D>();Assert.IsNotNull(seat,"fixture: the seat");
            // UI/Default spreads softness over half its value each side of the clip: about 50 units is what a shoulder needs to dissolve rather than stop.
            Assert.GreaterOrEqual(seat.softness.x,80,"the bust's right side stops in a straight cut inside the box");
            Assert.AreEqual(0,seat.softness.y,"softness on the bottom edge would lift the bust off the frame it sits on");
            float units=face.rectTransform.lossyScale.y;float clipped=(Box(face.rectTransform).yMax-Box(seat.rectTransform).yMax)/units;
            Assert.GreaterOrEqual(clipped,3,"the top row of a cell shows: the sheet's next row up bleeds over it when scaled, a broken line floating over the head");
            Assert.LessOrEqual(clipped,8,"the seat takes the top off the heads that reach their cell's top");
            panel.Hide();
        }

        // ------------------------------------------------------------------ item 5: numbers, and the feed
        static Health Dummy(Vector3 at)
        {var body=new GameObject("Body fixture").AddComponent<Health>();body.id="Fixture";body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;body.transform.position=at;return body;}
        static Rect Glyphs(TMP_Text text)
        {
            text.ForceMeshUpdate();var b=text.textBounds;
            Vector3 lo=text.transform.TransformPoint(b.min),hi=text.transform.TransformPoint(b.max);return Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);
        }
        [UnityTest] public IEnumerator HitsComingThickAndFastReadAsOneRunningTotal()
        {
            var hero=Hero;var body=Dummy(hero.transform.position+Vector3.forward*5);
            var plain=System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
            float each=DamageMath.Resolve(20,plain,body.weakness,body.resistance,false,false,1);
            for(int hit=0;hit<6;hit++){body.Receive(new DamagePacket{amount=20,type=plain});yield return new WaitForSecondsRealtime(.15f);}
            var numbers=Live("Damage");
            Assert.LessOrEqual(numbers.Length,3,"six hits on one body in under a second are six numbers: a fan of the same digits over what the player is aiming at: "+string.Join(" | ",numbers.Select(n=>n.text)));
            Assert.AreEqual(each*6,numbers.Sum(n=>float.Parse(n.text)),numbers.Length+.01f,"the running totals don't add up what landed");
            // The newest is held from its last hit, not its first.
            var newest=numbers.OrderByDescending(n=>n.transform.GetSiblingIndex()).First();
            yield return new WaitForSecondsRealtime(.35f);
            Assert.Greater(newest.alpha,.98f,"a running total fades while it's still being read");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsEmpty(Live("Damage"),"the numbers never leave");
            Object.Destroy(body.gameObject);
        }
        [UnityTest] public IEnumerator AStreamOfHitsLeavesTheLockReticlesMiddleClear()
        {
            var hero=Hero;var body=Dummy(hero.transform.position+Vector3.forward*1.5f);
            // Its middle stands exactly where a number first lands: a head, as the Cantor's does. In the open: at 5 m it lay under the target plate, whose own keep-out pushes numbers off it with or without any of this.
            var shape=body.gameObject.AddComponent<SphereCollider>();shape.isTrigger=true;shape.center=Vector3.up*1.9f;shape.radius=.4f;Physics.SyncTransforms();
            hero.target=body;hero.TargetLocked=true;yield return null;yield return null;yield return null;
            var reticle=Picture("TargetReticle");Assert.IsTrue(reticle.enabled,"fixture: the reticle is up");
            Assert.IsFalse(Box(Picture("TargetPlate").rectTransform).Contains(reticle.rectTransform.position),"fixture: the lock lies under the target plate, which keeps numbers off it for its own reason");
            var camera=Camera.main;float pixel=camera.pixelHeight/1080f;int most=0;
            var types=System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().ToArray();
            for(int hit=0;hit<14;hit++)
            {
                body.Receive(new DamagePacket{amount=100+hit*37,type=types[hit%types.Length]},hit%2==0?2:1);
                for(float until=Time.unscaledTime+.13f;Time.unscaledTime<until;)
                {
                    yield return null;
                    var middle=(Vector2)reticle.rectTransform.position;var heart=Rect.MinMaxRect(middle.x-10*pixel,middle.y-10*pixel,middle.x+10*pixel,middle.y+10*pixel);
                    var numbers=Live("Damage");most=Mathf.Max(most,numbers.Length);
                    foreach(var number in numbers)Assert.IsFalse(Glyphs(number).Overlaps(heart),$"\"{number.text}\" lies on the lock reticle's middle, the one place the player is aiming");
                }
            }
            // Enough to fill the slots around it, which is when a number has nowhere clear left to land.
            Assert.GreaterOrEqual(most,5,"fixture: a crowd of numbers was up at once");
            Object.Destroy(body.gameObject);
        }
        [UnityTest] public IEnumerator NumbersAtTheScreensEdgeKeepTheirFan()
        {
            yield return null;
            var camera=Camera.main;var body=Dummy(camera.ScreenToWorldPoint(new Vector3(-camera.pixelWidth*.2f,camera.pixelHeight*.5f,20))-Vector3.up*1.9f);
            var types=System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().ToArray();
            for(int hit=0;hit<6;hit++){body.Receive(new DamagePacket{amount=100+hit*37,type=types[hit%types.Length]});yield return new WaitForSecondsRealtime(.08f);}
            var numbers=Live("Damage");Assert.GreaterOrEqual(numbers.Length,3,"fixture: several numbers are up");
            float units=numbers[0].rectTransform.lossyScale.x;var xs=numbers.Select(n=>Box(n.rectTransform).center.x/units).ToArray();
            Assert.Greater(xs.Max()-xs.Min(),40,"the numbers of a body off the screen pile up in a dead-straight column against its edge");
            Object.Destroy(body.gameObject);
        }
        [UnityTest] public IEnumerator ThePickupFeedHasAPlate()
        {
            yield return null;
            RpgServices.Inventory.Grant("ScrapAlloy",2);RpgServices.Inventory.Grant("RidgeCrystal",1);yield return null;yield return null;
            var band=Picture("PickupBand");Assert.IsNotNull(band,"reward words lie on a bright floor with nothing behind them: gold on orange dirt");
            Assert.IsTrue(band.isActiveAndEnabled,"the feed's plate isn't up with its lines");Assert.That(band.color.a,Is.InRange(.4f,.8f),"the feed's plate either doesn't darken the floor or blacks it out");
            var lines=Live("PickupLine");Assert.GreaterOrEqual(lines.Length,2,"fixture: two lines");var box=Box(band.rectTransform);var drawn=band.canvas.GetComponentsInChildren<Graphic>().ToList();
            foreach(var line in lines)
            {
                var words=Glyphs(line);
                Assert.IsTrue(box.Contains(words.min)&&box.Contains(words.max),$"\"{line.text}\" runs out of the feed's plate");
                Assert.Less(drawn.IndexOf(band),drawn.IndexOf(line),"the plate is drawn over the words");
            }
            Assert.IsFalse(box.Overlaps(Box(Picture("VitalsFrame").rectTransform,12)),"the feed's plate dims the vitals over it");
            yield return new WaitForSecondsRealtime(3.5f);
            Assert.IsEmpty(Live("PickupLine"),"fixture: the lines have gone");Assert.IsFalse(band.isActiveAndEnabled,"the plate stays with no words on it");
        }
        [UnityTest] public IEnumerator ASaveTheCardAnnouncesIsNotAnnouncedTwice()
        {
            yield return Settle();
            var toast=Object.FindFirstObjectByType<QuestToast>();
            RepairBay.Repair(true);yield return null;yield return null;
            float deadline=Time.unscaledTime+2;while(!toast.Heading.Contains("SAVED")&&Time.unscaledTime<deadline)yield return null;
            StringAssert.Contains("SAVED",toast.Heading,"fixture: the card says it");
            Assert.IsFalse(Live("PickupLine").Any(t=>t.text=="Saved"),"one save is announced twice: a bare word under the vitals and a card at the right");
            // A save nothing else mentions still gets its word.
            GameServices.Current.Saves.Save("autosave",State);yield return null;
            Assert.IsTrue(Live("PickupLine").Any(t=>t.text=="Saved"),"an autosave is no longer announced at all");
        }

        // ------------------------------------------------------------------ item 6: the boss card and the bar
        [UnityTest] public IEnumerator TheBossCardIsBriefAndHasNoBandOverTheBoss()
        {
            var hero=Hero;Guard();
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),hero.transform.position+Vector3.forward*12);boss.Passive=false;
            yield return new WaitForSecondsRealtime(1.3f);
            var card=Text("BossTitleCard");Assert.Greater(card.alpha,.9f,"the card has gone before it can be read");
            Assert.IsNull(Picture("BossTitleBand"),"the card's dark band hides a boss standing under it from the hips up");
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.Less(card.alpha,.02f,"the card is still over the boss more than two seconds into the fight");
            Assert.IsFalse(Picture("BossTitleRule").isActiveAndEnabled,"the card's rule stays over the fight after its words have gone");
            StringAssert.Contains("scavenger",Text("BossEpithet").text,"the epithet never reaches the bar");
            boss.Passive=true;Object.Destroy(boss.gameObject);yield return null;yield return null;
            // Its clock starts when the bar can be seen: a conversation hides the HUD, and the card spent itself behind it.
            GameServices.Current.Input.Blocked=true;
            var next=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Burrower"),hero.transform.position+Vector3.forward*14);next.Passive=false;
            yield return new WaitForSecondsRealtime(2.4f);
            GameServices.Current.Input.Blocked=false;
            yield return new WaitForSecondsRealtime(.5f);
            StringAssert.Contains("BURROWER",card.text,"fixture: the second boss's card");
            Assert.Greater(card.alpha,.9f,"the card ran out behind the conversation that opened the fight");
            next.Passive=true;Object.Destroy(next.gameObject);
        }
        [UnityTest] public IEnumerator ANumberKeepsOffTheBossCard()
        {
            var hero=Hero;Guard();var camera=Camera.main;
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),hero.transform.position+Vector3.forward*12);boss.Passive=false;
            yield return new WaitForSecondsRealtime(.3f);
            var card=Text("BossTitleCard");var at=Box(card.rectTransform).center;
            var body=Dummy(camera.ScreenToWorldPoint(new Vector3(at.x,at.y,20))-Vector3.up*1.9f);
            body.Receive(new DamagePacket{amount=41,type=DamageType.Pulse});
            for(int i=0;i<4;i++)
            {
                yield return null;
                foreach(var number in Live("Damage"))Assert.IsFalse(Box(number.rectTransform,1).Overlaps(Box(card.rectTransform)),"a number prints across the boss's name");
            }
            boss.Passive=true;Object.Destroy(boss.gameObject);Object.Destroy(body.gameObject);
        }
        [UnityTest] public IEnumerator ANumberKeepsOffTheSkillRow()
        {
            yield return null;
            var camera=Camera.main;var skill=Picture("Skill1");Assert.IsTrue(skill.isActiveAndEnabled,"fixture: the skill row is up");var at=Box(skill.rectTransform).center;
            var body=Dummy(camera.ScreenToWorldPoint(new Vector3(at.x,at.y,20))-Vector3.up*1.9f);
            body.Receive(new DamagePacket{amount=41,type=DamageType.Pulse});
            for(int i=0;i<4;i++)
            {
                yield return null;
                foreach(var number in Live("Damage"))Assert.IsFalse(Box(number.rectTransform,1).Overlaps(Box(skill.rectTransform)),"a number prints across a skill's icon and its cooldown");
            }
            Object.Destroy(body.gameObject);
        }
        internal static DamagePacket Blow(Health from)=>new DamagePacket{source=from,amount=1e8f,type=DamageType.Pulse};
        static IEnumerator Cantor(System.Action<EnemyBrain> found)
        {
            yield return Zone("Arena_Flight");
            var hero=Hero;hero.GetComponent<FlightMotor>().Halt();hero.Health.InvulnerableUntil=GameTime.Now+60;
            var cantor=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),hero.transform.position+new Vector3(20,0,0));cantor.Passive=false;cantor.enabled=true;
            yield return null;yield return null;yield return null;cantor.Passive=false;yield return null;yield return null;
            Assert.IsTrue(Picture("BossIntegrityBack").isActiveAndEnabled,"fixture: the bar is up");
            found(cantor);
        }
        [UnityTest] public IEnumerator EveryLinkCutOnTheCantorsBarLandsOnAMark()
        {
            EnemyBrain cantor=null;yield return Cantor(c=>cantor=c);var hero=Hero;
            var collar=cantor.GetComponent<CantorCollar>();var head=cantor.Health;var fill=Picture("BossIntegrity");var back=Picture("BossIntegrityBack");
            float width=back.rectTransform.rect.width;float Bar()=>fill.rectTransform.rect.width/width;
            var notches=fill.canvas.GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("BossNotch")).Select(i=>i.rectTransform.anchoredPosition.x/width).OrderByDescending(x=>x).ToArray();
            var phases=cantor.GetComponent<BossController>().Thresholds;
            Assert.AreEqual(collar.Links.Count+phases.Length,notches.Length,"the bar doesn't mark each link as well as the head's phases: three cuts land on nothing");
            float whole=head.maximum+collar.Links.Sum(l=>l.maximum),share=head.maximum/whole,left=whole;
            for(int i=0;i<collar.Links.Count;i++)
            {
                left-=collar.Links[i].maximum;
                Assert.AreEqual(left/whole,notches[i],.005f,$"no notch where link {i+1} goes");
                collar.Links[i].Receive(Blow(hero.Health));yield return null;yield return null;
                Assert.AreEqual(notches[i],Bar(),.01f,$"the cut of link {i+1} doesn't land on its mark");
            }
            Assert.AreEqual(share,notches[collar.Links.Count-1],.005f,"the last link's mark isn't where the head opens");
            for(int i=0;i<phases.Length;i++)Assert.AreEqual(share*phases[i],notches[collar.Links.Count+i],.005f,"the head's phase isn't marked where it falls on the whole fight's bar");
            cantor.Passive=true;Object.Destroy(cantor.gameObject);
        }
        [UnityTest] public IEnumerator TheFrameOfTheKillingBlowDoesNotArgueWithItself()
        {
            EnemyBrain cantor=null;yield return Cantor(c=>cantor=c);var hero=Hero;
            var collar=cantor.GetComponent<CantorCollar>();var head=cantor.Health;
            foreach(var link in collar.Links)link.Receive(Blow(hero.Health));yield return null;yield return null;
            hero.target=head;hero.TargetLocked=true;head.integrity=68;yield return null;yield return null;yield return null;
            StringAssert.Contains("COLLAR LOCK",Text("Target").text,"fixture: the plate names the lock");
            head.InvulnerableUntil=0;// Setting the head that low crossed its phases at once, and the shift's window would turn the blow away.
            var blow=new GameObject("Blow fixture").AddComponent<HudSecondLateBlow>();blow.from=hero.Health;blow.body=head;
            var seen=new GameObject("Frame fixture").AddComponent<HudSecondFrameReader>();
            yield return null;yield return null;
            Assert.IsFalse(head.Alive,"fixture: the blow landed");Assert.IsTrue(seen.read,"fixture: the frame was read");
            Assert.IsTrue(string.IsNullOrEmpty(seen.plate),$"the frame that pays the rewards still says \"{seen.plate}\": the HUD read the fight before the blow landed");
            Assert.IsFalse(seen.bar,"OPENING is lit over an empty bar in the frame the boss went down");
        }

        // ------------------------------------------------------------------ item 7: Fabricate
        static Button[] Buttons()=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        static string Words(Component c){var t=c.GetComponentInChildren<TMP_Text>();return t!=null?t.text:"";}
        static Button Row(string name)=>Buttons().FirstOrDefault(b=>b.name==name);
        static IEnumerator FindRow(string words,System.Action<Button> found)
        {
            for(int i=0;i<4;i++)
            {
                var row=Buttons().FirstOrDefault(b=>Words(b).Contains(words));if(row!=null){found(row);yield break;}
                Row("Pager").onClick.Invoke();yield return null;
            }
            Assert.Fail("fixture: no row says "+words);
        }
        [UnityTest] public IEnumerator FabricateSaysWhyARowIsGreyWithEveryMaterialMet()
        {
            State.materials["ScrapAlloy"]=60;State.materials["RidgeCrystal"]=9;State.craftingXp["Emitters"]=0;State.craftingXp["Modules"]=0;
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open(PauseMenu.FabricatePage,true);yield return null;
            Button can=null,locked=null;
            yield return FindRow("Scrap Emitter",b=>can=b);Color canColor=can.GetComponentInChildren<TMP_Text>().color;StringAssert.DoesNotContain("Needs",Words(can),"a row that can be made talks about what it needs");
            yield return FindRow("Refined Emitter",b=>locked=b);var words=locked.GetComponentInChildren<TMP_Text>();
            EventSystem.current.SetSelectedGameObject(Row("Close").gameObject);yield return null;
            Assert.Greater(Mathf.Abs(canColor.r-words.color.r)+Mathf.Abs(canColor.g-words.color.g)+Mathf.Abs(canColor.b-words.color.b),.3f,"fixture: the row can't be made, so it's grey");
            StringAssert.DoesNotContain("<color",System.Text.RegularExpressions.Regex.Replace(words.text,"<color=[^>]*>Needs[^<]*</color>",""),"fixture: every material is met");
            StringAssert.Contains("Needs Emitters 2",words.text,"a grey row with every material met doesn't say why: the Emitters rank is only in the footer");
            // The row holds that on its one line, at the longest it can run.
            yield return FindRow("Refined Module",b=>locked=b);words=locked.GetComponentInChildren<TMP_Text>();
            StringAssert.Contains("Needs Modules 2",words.text,"fixture: the longest locked row");
            // TMP's measure doesn't take the label's margins off the width it's given.
            Assert.LessOrEqual(words.GetPreferredValues(words.text,words.rectTransform.rect.width-words.margin.x-words.margin.z,0).y,locked.GetComponent<RectTransform>().rect.height,"the reason wraps the row off its line");
            menu.Close();yield return null;
            // Trained, the same row says what it makes and asks for nothing.
            State.craftingXp["Emitters"]=30;menu.Open(PauseMenu.FabricatePage,true);yield return null;
            yield return FindRow("Refined Emitter",b=>locked=b);
            StringAssert.DoesNotContain("Needs",Words(locked),"the row still says it needs a rank it has");StringAssert.Contains("Output +",Words(locked),"a row that can be made doesn't say what it makes");
            menu.Close();
        }

        // ------------------------------------------------------------------ item 8: the chapter card
        [UnityTest] public IEnumerator TheChapterCardSaysTheRoadRunsOnInTheGamesVoice()
        {
            var toast=Object.FindFirstObjectByType<QuestToast>();yield return Settle();
            Flag("chapter2.homecoming");
            float deadline=Time.unscaledTime+3;while(!toast.Heading.Contains("CHAPTER 2")&&Time.unscaledTime<deadline)yield return null;
            StringAssert.Contains("CHAPTER 2",toast.Heading,"fixture: the chapter ends");yield return null;
            string words=Text("ChapterCardBody").text;
            StringAssert.DoesNotContain("build",words,"the card steps out of the game at the end of the chapter");
            StringAssert.Contains("road runs on",words,"the card no longer says the road goes on");
            StringAssert.Contains("Xylos",words,"the card no longer says where the road goes");
            StringAssert.Contains("still being drawn",words,"'isn't charted yet' reads as a task: the card has to say nobody's waiting on the player for it");
            StringAssert.Contains("keep exploring",words,"the card no longer says what's left to do");
            var body=Text("ChapterCardBody");var box=body.rectTransform.rect;
            Assert.LessOrEqual(body.GetPreferredValues(words,box.width,0).y,box.height+1,"the card's words overflow it");
        }
    }
}
