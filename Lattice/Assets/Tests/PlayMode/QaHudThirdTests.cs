using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The third eye pass on the HUD. A notched arrow read as a mouse pointer and a Y; a bust stopped on a line; menus went to flat black; four lines of frame stood round one line of words; the boss's name lay on the boss;
    /// a hero down left the objective pointing at someone who wouldn't speak. These measure what makes each of those right. Whether it looks right is a question for the next capture. Every check here fails on the build it was written against.</summary>
    public sealed class QaHudThirdTests
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
        static TMP_Text Text(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static Image Picture(string name)=>Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(i=>i.name==name);
        static Button[] Buttons()=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        static Button Row(string name)=>Buttons().FirstOrDefault(b=>b.name==name);
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
        sealed class ShutPrompt:InteractionPrompt{public override void Interact(){}public override bool Available=>false;}
        static FixturePrompt Prompt(string words,Vector3 at,float range=6)
        {var prompt=new GameObject("Prompt fixture").AddComponent<FixturePrompt>();prompt.prompt=words;prompt.range=range;prompt.transform.position=at;return prompt;}
        static float Plate(string image)=>Picture(image).canvasRenderer.GetAlpha();
        /// <summary>The ground under a screen point, for a body whose middle stands this high above the hero's feet.</summary>
        static Vector3 Under(Vector2 screen,float height)
        {
            var ray=Camera.main.ScreenPointToRay(screen);float y=Hero.transform.position.y+height;return ray.GetPoint((y-ray.origin.y)/ray.direction.y)-Vector3.up*height;
        }
        static Vector3 Behind(RectTransform panel,float height)=>Under(Box(panel).center,height);
        static string Line()=>Text("Objective").text.Split('\n')[0];
        static float Metres(){var found=Regex.Match(Regex.Replace(Text("Objective").text,"<[^>]+>",""),@"(\d+) m\b");Assert.IsTrue(found.Success,"the panel gives no distance: "+Text("Objective").text);return float.Parse(found.Groups[1].Value);}
        static void Kill(CombatActor who){who.Health.InvulnerableUntil=0;who.Health.Receive(new DamagePacket{amount=who.Health.maximum*10});}
        /// <summary>A boss kept where she was put, so that where the camera carries the hero is down to where she stands and not to how fast she runs at him.</summary>
        static void Stay(EnemyBrain boss,Vector3 at)
        {
            var body=boss.GetComponent<CharacterController>();if(body.enabled)body.enabled=false;
            typeof(BossController).GetField("nextSpecial",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(boss.GetComponent<BossController>(),Time.time+900);
            boss.transform.position=at;Physics.SyncTransforms();
        }
        // Where a piece lands on a screen the test machine doesn't have, from its anchors and offsets up to the canvas.
        static Canvas Surface(string name)
        {var found=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(c=>c.name==name);Assert.IsNotNull(found,"the "+name+" surface was never built");return found;}
        static RectTransform Spot(string surface,string name)
        {var found=Surface(surface).GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r=>r.name==name);Assert.IsNotNull(found,surface+" has no "+name);return found;}
        static Vector2 CanvasAt(CanvasScaler scaler,int w,int h)
        {var r=scaler.referenceResolution;float scale=Mathf.Min(w/r.x,h/r.y);return new Vector2(w/scale,h/scale);}
        static Rect Placed(RectTransform rect,RectTransform root,Vector2 size)
        {
            if(rect==root)return new Rect(Vector2.zero,size);
            var outer=Placed((RectTransform)rect.parent,root,size);
            var min=outer.position+Vector2.Scale(outer.size,rect.anchorMin)+rect.offsetMin;var max=outer.position+Vector2.Scale(outer.size,rect.anchorMax)+rect.offsetMax;
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }

        // ------------------------------------------------------------------ item 1: the arrow
        [UnityTest] public IEnumerator TheArrowHasNoNotchAndReadsWithItsOneSharpEnd()
        {
            yield return null;
            var shape=UiSkin.PointerOutline;int tip=0;for(int i=0;i<shape.Length;i++)if(shape[i].y>shape[tip].y)tip=i;
            float turn=0,area=0;
            for(int i=0;i<shape.Length;i++)
            {
                Vector2 a=shape[i],b=shape[(i+1)%shape.Length],c=shape[(i+2)%shape.Length];
                float cross=(b.x-a.x)*(c.y-b.y)-(b.y-a.y)*(c.x-b.x);if(turn==0)turn=cross;
                Assert.Greater(cross*turn,0,$"the outline turns back on itself at {b}: at 20 px a notch forks it into a V or a Y, and Y means Swap on the same screen, and turned up and left it's a mouse pointer");
                area+=a.x*b.y-b.x*a.y;
            }
            for(int i=0;i<shape.Length;i++)if(i!=tip)Assert.GreaterOrEqual(shape[tip].magnitude,shape[i].magnitude*1.25f,$"the point at {shape[i]} reaches about as far as the tip: turned, it reads as a triangle aimed elsewhere");
            float low=shape.Min(p=>p.y),wide=shape.Max(p=>p.x)-shape.Min(p=>p.x);
            Assert.LessOrEqual(wide,.95f,"its wings aren't narrow: it reads as a gem, not an arrow");
            var tail=shape.Where(p=>p.y<low+.01f).ToArray();Assert.AreEqual(2,tail.Length,"the back of it isn't a blunt tail");
            Assert.That(Mathf.Abs(tail[0].x-tail[1].x),Is.InRange(.2f,wide*.7f),"its tail isn't short, blunt and narrower than its wings");
            // Heavy enough at the floor size: the small one on the Also line, in its 30 box.
            Assert.GreaterOrEqual(Mathf.Abs(area)*.5f,.75f,"it's too thin to hold on a line of type at the floor size");
            Assert.AreSame(Picture("ObjectiveArrow").sprite,Picture("BossEdgeMark").sprite,"the boss's mark no longer shares the objective's arrow");
        }

        // ------------------------------------------------------------------ item 2: the bust's right side
        static float Light(float seen){seen/=255;return seen<=.04045f?seen/12.92f:Mathf.Pow((seen+.055f)/1.055f,2.4f);}
        static float Seen(float light)=>255*(light<=.0031308f?12.92f*light:1.055f*Mathf.Pow(light,1/2.4f)-.055f);
        [UnityTest] public IEnumerator TheBustsRightSideReadsAsARampNotAStep()
        {
            yield return null;
            var panel=Object.FindFirstObjectByType<DialoguePanel>();var sets=Resources.LoadAll<PortraitSet>("Portraits");
            panel.Show("Sela",PortraitEmotion.Neutral,sets[0].entries[0].sprite,"Fixture line.");panel.SetVisibleCharacters(int.MaxValue);yield return null;
            var face=Picture("Portrait");var seat=face.GetComponentInParent<RectMask2D>();var strip=Picture("PortraitFade");
            Assert.IsNotNull(strip,"nothing lies over the bust's right side: a shawl stops on a straight line inside the box");
            Assert.IsTrue(strip.isActiveAndEnabled,"the strip isn't up with the box");
            var drawn=face.canvas.GetComponentsInChildren<Graphic>().ToList();
            Assert.Greater(drawn.IndexOf(strip),drawn.IndexOf(face),"the strip is under the bust it should fade");
            Assert.Less(drawn.IndexOf(strip),drawn.IndexOf(Text("Body")),"the strip is over the words");
            Assert.AreEqual((Color)Picture("DialogueBacking").color,strip.color,"the strip isn't in the plate's own colour, so it shows as a stripe of its own");
            float units=face.rectTransform.lossyScale.x;var box=Box(strip.rectTransform);var seatBox=Box(seat.rectTransform);var frame=Box(Picture("DialoguePanel").rectTransform);
            Assert.AreEqual(seatBox.xMax,box.xMax,2*units,"the strip doesn't reach the seat's edge, where the art stops");
            Assert.AreEqual(seatBox.yMin,box.yMin,2*units,"the strip doesn't reach down to where the bust stands");
            Assert.That(box.width/units,Is.InRange(80f,110f),"the fade is too narrow to see, or eats into the face");
            Assert.LessOrEqual(box.yMax,frame.yMax-30*units,"the strip rubs out the box's top bevel");
            // Read across it at the height of a shoulder, as the canvas blends, in light: a bright shawl over the box's dark, through the seat's soft edge and the strip together.
            var texture=strip.sprite.texture;float soft=seat.softness.x,bright=Light(211),dark=Light(18),last=0,most=0,wide=box.width/units;
            for(int unit=0;unit<=(int)wide;unit++)
            {
                float through=Mathf.Min(1,2*unit/soft)*(1-texture.GetPixelBilinear(1-unit/wide,0).a);
                float seen=Seen(dark+(bright-dark)*through);
                if(unit>0)most=Mathf.Max(most,Mathf.Abs(seen-last));last=seen;
                if(unit==6)Assert.Less(seen,Seen(dark)+6,"six units from the cut the shawl still stands out from the box: that's the step the eye reads");
            }
            Assert.Less(most,8,"the shawl falls more than 8 levels in one unit: a step, not a ramp");
            Assert.Greater(last,200,"the shawl is dimmed past the strip's far end, or the strip has a hard start of its own");
            Assert.Less(texture.GetPixelBilinear(1,1).a,.02f,"the strip stops on a hard line across hair and cheeks at its top");
            panel.Hide();
        }

        // ------------------------------------------------------------------ item 3: menus
        static void FramedOverTheWorld(string surface,string name,string doing,List<string> failures)
        {
            var dim=Object.FindObjectsByType<Image>(FindObjectsSortMode.None).First(i=>i.name=="Dim"&&i.canvas.rootCanvas.name==surface);
            if(dim.color.a<.7f||dim.color.a>.9f)failures.Add($"{doing}: the world behind it is dimmed by {dim.color.a:0.00}: flat black from edge to edge read as a debug list, and much less lets signs fight the rows");
            var frame=Picture(name);var backing=Picture(name+"Backing");
            if(frame==null||backing==null){failures.Add($"{doing}: the rows have no framed plate under them");return;}
            if(backing.color.a<.999f)failures.Add($"{doing}: what the rows stand on lets the world through");
            float units=frame.rectTransform.lossyScale.x;var screen=Box((RectTransform)frame.canvas.transform);var box=Box(frame.rectTransform);
            if(box.xMin-screen.xMin<40*units||screen.xMax-box.xMax<40*units||box.yMin-screen.yMin<20*units||screen.yMax-box.yMax<20*units)failures.Add($"{doing}: the panel fills the screen, so its edge doesn't show and the world doesn't either");
            var opening=Box(frame.rectTransform,30*units);var drawn=frame.canvas.GetComponentsInChildren<Graphic>().ToList();
            foreach(var button in Buttons())
            {
                var at=Box((RectTransform)button.transform);
                if(at.xMin<opening.xMin||at.xMax>opening.xMax||at.yMin<opening.yMin||at.yMax>opening.yMax)failures.Add($"{doing}: {button.name} runs out of the panel's opening");
                if(drawn.IndexOf(button.targetGraphic)<drawn.IndexOf(frame))failures.Add($"{doing}: {button.name} is drawn under the panel");
            }
        }
        [UnityTest] public IEnumerator EveryPageOfTheMenuStandsOnAFramedPlateOverTheDimmedWorld()
        {
            yield return null;
            var menu=Object.FindFirstObjectByType<PauseMenu>();var failures=new List<string>();
            for(int page=0;page<menu.PageCount;page++){menu.Open(page);yield return null;FramedOverTheWorld("Menus","MenuPanel","page "+page,failures);menu.Close();yield return null;}
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }
        [UnityTest] public IEnumerator TheShopHasItsSellerOnThePage()
        {
            yield return null;
            foreach(var (zone,node,speaker) in new[]{("Hub_Decks","MiraRepeat","Mira"),("Lanternwick","CorraRepeat","Corra")})
            {
                yield return Zone(zone);
                var shop=Object.FindFirstObjectByType<ShopUi>();var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=true;
                Assert.IsTrue(dialogue.StartNode(node,speaker),"fixture: "+node+" starts");
                float deadline=Time.unscaledTime+20;while(!shop.IsOpen&&Time.unscaledTime<deadline)yield return null;
                Assert.IsTrue(shop.IsOpen,"fixture: "+node+" opens the shop");yield return null;
                var face=Picture("SellerPortrait");Assert.IsNotNull(face,$"{speaker}'s shop is a list with nobody selling it");
                Assert.AreSame(PortraitLookup.Get(speaker,BodyForm.Natural,PortraitEmotion.Happy),face.sprite,$"that isn't {speaker}'s face");Assert.IsTrue(face.isActiveAndEnabled,"her face isn't up");
                Assert.AreEqual(DisplayNames.Speaker(speaker),Text("SellerName").text,$"{speaker}'s name isn't on the page");Assert.AreEqual(UiSkin.Display,Text("SellerName").font,"the name isn't in the dialogue box's face");
                var failures=new List<string>();FramedOverTheWorld("Shop","ShopPanel",speaker+"'s shop",failures);
                var frame=Picture("ShopPanel");float units=frame.rectTransform.lossyScale.x;var seat=Box(face.GetComponentInParent<RectMask2D>().rectTransform);
                Assert.AreEqual(Box(frame.rectTransform).yMin+31*units,seat.yMin,3*units,"the bust doesn't stand on the frame's inner edge, as it does in the dialogue box: its art stops short of its cell");
                Assert.IsTrue(Box(frame.rectTransform,30*units).Contains(new Vector2(seat.xMin+1,seat.yMin+1)),"the bust runs out of the panel");
                Assert.IsNotNull(Picture("SellerPortraitFade"),"her shoulder stops on a line");
                foreach(var button in Buttons())
                {
                    if(Box((RectTransform)button.transform).Overlaps(seat))failures.Add($"{button.name} lies on the seller");
                    var label=button.GetComponentInChildren<TMP_Text>();
                    if(label==null)continue;
                    if(label.fontSize<24)failures.Add($"{button.name} is under the type floor");
                    if(label.GetPreferredValues(label.text,label.rectTransform.rect.width,0).y>label.rectTransform.rect.height+1)failures.Add($"{button.name} wraps off its row");
                }
                Assert.IsEmpty(failures,string.Join("\n",failures));
                // She stays through a turn of the page.
                Row("ShopRow0").onClick.Invoke();yield return null;
                Assert.IsNotNull(Picture("SellerPortrait"),"the seller goes when the page turns");
                shop.Close();yield return null;yield return null;
                Assert.IsNull(Object.FindObjectsByType<Image>(FindObjectsSortMode.None).FirstOrDefault(i=>i.name=="SellerPortrait"),"the seller stays on the screen with the shop gone");
            }
            // With nobody selling it, no empty column.
            var plain=Object.FindFirstObjectByType<ShopUi>();plain.Open(null);yield return null;
            Assert.IsNull(Object.FindObjectsByType<Image>(FindObjectsSortMode.None).FirstOrDefault(i=>i.name=="SellerPortrait"),"a shop with nobody selling it has a face");
            var whole=Box((RectTransform)Picture("ShopPanel").canvas.transform);Assert.AreEqual(whole.center.x,Box((RectTransform)Row("ShopRow0").transform).center.x,whole.width*.01f,"the list stands off the middle with nobody beside it");
            plain.Close();
        }

        // ------------------------------------------------------------------ item 4: the objective's frame
        [UnityTest] public IEnumerator TheObjectivesFrameIsAsTallAsItsWordsAndWhatHangsUnderItFollows()
        {
            yield return Settle();
            var frame=Picture("ObjectiveFrame").rectTransform;var words=Text("Objective");float units=frame.lossyScale.y;
            float Wanted(){words.ForceMeshUpdate();return Mathf.Max(88,words.textInfo.lineCount*27.6f+60);}
            void Check(string doing)
            {
                var whole=Box((RectTransform)frame.GetComponentInParent<Canvas>().transform);var box=Box(frame);
                Assert.AreEqual(Wanted(),box.height/units,1,$"{doing}: the frame isn't as tall as what it says");
                Assert.AreEqual(92.5f,(whole.yMax-box.yMax)/units,1,$"{doing}: the frame's top edge moved, under the zone's name");
                Assert.AreEqual(570,box.width/units,1,$"{doing}: the frame's width moved");Assert.AreEqual(300,(whole.xMax-box.center.x)/units,1,$"{doing}: the frame left its corner");
                words.ForceMeshUpdate();var bounds=words.textBounds;float top=words.transform.TransformPoint(bounds.max).y,bottom=words.transform.TransformPoint(bounds.min).y;
                Assert.GreaterOrEqual((box.yMax-top)/units,25,$"{doing}: the first line crowds the frame's top bevel");Assert.GreaterOrEqual((bottom-box.yMin)/units,25,$"{doing}: the last line crowds its bottom bevel");
            }
            words.ForceMeshUpdate();Assert.AreEqual(1,words.textInfo.lineCount,"fixture: one line: "+words.text);
            Check("one line");Assert.LessOrEqual(frame.rect.height,90,"a line of words stands in a block of frame, over whatever the fight is doing");
            Notices.Post("QUEST UPDATED","A quest's name\nThe next step of it");yield return new WaitForSecondsRealtime(.3f);
            var card=Picture("QuestToastFrame").rectTransform;Assert.IsTrue(card.gameObject.activeInHierarchy,"fixture: the card is up");
            Assert.AreEqual(14.5f,(Box(frame).yMin-Box(card).yMax)/units,1,"the card under the objective doesn't move up with it");
            // More to say, and the same rule. Mira's crystals add the Also line.
            State.questSteps["MiraCrystals"]=0;State.materials["RidgeCrystal"]=0;
            yield return Zone("Sorrel_Ridges");yield return Settle();
            frame=Picture("ObjectiveFrame").rectTransform;words=Text("Objective");units=frame.lossyScale.y;
            foreach(var volume in Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None))volume.enabled=false;
            words.ForceMeshUpdate();int lines=words.textInfo.lineCount;Assert.GreaterOrEqual(lines,3,"fixture: several lines: "+words.text);
            Check(lines+" lines");
            Notices.Post("QUEST UPDATED","A quest's name\nThe next step of it");yield return new WaitForSecondsRealtime(.3f);
            card=Picture("QuestToastFrame").rectTransform;Assert.AreEqual(14.5f,(Box(frame).yMin-Box(card).yMax)/units,1,"the card under the objective doesn't move down with it");
            // Words that change their digits and not their lines don't move anything: the distance runs down all the way across a zone.
            var hero=Hero;float high=frame.rect.height,cardAt=Box(card).yMax;string said=words.text;bool changed=false;
            for(int step=0;step<14;step++)
            {
                Put(hero,hero.transform.position+Vector3.forward);yield return new WaitForSecondsRealtime(.06f);
                changed|=words.text!=said;words.ForceMeshUpdate();
                if(words.textInfo.lineCount!=lines)continue;
                Assert.AreEqual(high,frame.rect.height,.01f,"the frame moved with no line more or less to say");
                if(card.gameObject.activeInHierarchy)Assert.AreEqual(cardAt,Box(card).yMax,.5f,"what hangs under it moved with no line more or less to say");
            }
            Assert.IsTrue(changed,"fixture: the distance ran down");
        }

        // ------------------------------------------------------------------ item 5: the boss's card
        static void CardClear(string doing,List<string> failures)
        {
            var hud=Surface("HUD");var root=(RectTransform)hud.transform;var corner=(RectTransform)Surface("Objective").transform;var scaler=hud.GetComponent<CanvasScaler>();
            string[] others={"BossBand","BossName","BossEpithet","BossIntegrityBack","BossBreakBack","BossState","Skills","Skill0","Skill1","Skill2","Skill3","SkillLabel0","SkillLabel1","SkillLabel2","SkillLabel3","Target","VitalsFrame"};
            foreach(var (w,h,label) in new[]{(1920,1080,"16:9"),(1600,1200,"4:3")})
            {
                var size=CanvasAt(scaler,w,h);var objective=Placed(Spot("Objective","ObjectiveFrame"),corner,size);
                foreach(string piece in new[]{"BossTitleCard","BossTitleRule","BossTitleEpithet"})
                {
                    var at=Placed(Spot("HUD",piece),root,size);
                    if(at.xMin<0||at.xMax>size.x||at.yMin<0||at.yMax>size.y)failures.Add($"{label}, {doing}: {piece} runs off the screen at {at}");
                    foreach(string other in others)if(at.Overlaps(Placed(Spot("HUD",other),root,size)))failures.Add($"{label}, {doing}: {piece} lies on {other}");
                    if(at.Overlaps(objective))failures.Add($"{label}, {doing}: {piece} lies on the objective");
                }
            }
        }
        [UnityTest] public IEnumerator TheBossCardStandsClearOfTheBarTheStripAndTheSkillsOnAnyScreen()
        {
            yield return null;
            var failures=new List<string>();CardClear("just over the bar",failures);
            Assert.AreEqual(new Vector2(.5f,0),Spot("HUD","BossTitleCard").anchorMin,"the card doesn't hang from the foot of the screen, over the bar");
            var hero=Hero;Guard();
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),hero.transform.position+Vector3.forward*14);boss.Passive=false;
            yield return new WaitForSecondsRealtime(.3f);
            CardClear("in the gap",failures);
            boss.Passive=true;Object.Destroy(boss.gameObject);
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }
        [UnityTest] public IEnumerator ABossCloseByHasItsCardOverTheBarClearOfBoth()
        {
            var hero=Hero;Guard();yield return Settle();
            var up=Quaternion.Euler(0,ZoneController.Current.definition.cameraProfile.yaw,0)*Vector3.forward;
            var at=hero.transform.position+up*4;var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),at);boss.Passive=false;
            for(float wait=Time.unscaledTime+.3f;Time.unscaledTime<wait;){Stay(boss,at);yield return null;}
            var card=Text("BossTitleCard");var line=Text("BossTitleEpithet");Assert.Greater(card.alpha,.5f,"fixture: the card is up");
            Assert.AreEqual(new Vector2(.5f,0),card.rectTransform.anchorMin,"with the boss close there's room over the bar, and the card's elsewhere: at Hessa's it lay on her, the numbers across her from the hips up");
            var camera=Camera.main;
            for(float end=Time.unscaledTime+1.2f;Time.unscaledTime<end;)
            {
                Stay(boss,at);float top=Box(card.rectTransform).yMax;
                Assert.Less(top,camera.WorldToScreenPoint(hero.transform.position).y,"the card lies across the hero");
                Assert.IsFalse(Box(card.rectTransform).Contains((Vector2)camera.WorldToScreenPoint(boss.transform.position+Vector3.up))||Box(line.rectTransform).Contains((Vector2)camera.WorldToScreenPoint(boss.transform.position+Vector3.up)),"the card lies across the boss");
                yield return null;
            }
            boss.Passive=true;Object.Destroy(boss.gameObject);
        }
        [UnityTest] public IEnumerator ABossFarUpTheScreenHasItsCardBetweenHerAndTheHero()
        {
            var hero=Hero;Guard();yield return Settle();
            // The camera slides until she fits and carries the hero down, with his feet a quarter of the way up the screen at the Bellows and three tenths at the Burrower. Just over the bar the words would lie on him.
            var up=Quaternion.Euler(0,ZoneController.Current.definition.cameraProfile.yaw,0)*Vector3.forward;
            var at=hero.transform.position+up*14;var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),at);boss.Passive=false;
            for(float wait=Time.unscaledTime+.6f;Time.unscaledTime<wait;){Stay(boss,at);yield return null;}
            var card=Text("BossTitleCard");var line=Text("BossTitleEpithet");Assert.Greater(card.alpha,.5f,"fixture: the card is up");var camera=Camera.main;
            Assert.AreEqual(new Vector2(.5f,.5f),card.rectTransform.anchorMin,"the card doesn't stand in the gap between them");
            for(float end=Time.unscaledTime+1;Time.unscaledTime<end;)
            {
                Stay(boss,at);float head=camera.WorldToScreenPoint(hero.transform.position+Vector3.up*1.9f).y;
                Assert.Greater(Box(line.rectTransform).yMin,head,"the card lies across the hero the camera has carried down");
                Assert.Less(Box(card.rectTransform).yMax,camera.WorldToScreenPoint(boss.transform.position).y,"the card lies across the boss");
                yield return null;
            }
            boss.Passive=true;Object.Destroy(boss.gameObject);
            // A flight frames both from their middle, and its card stays over the bar.
            yield return Zone("Arena_Flight");
            var ship=Hero;ship.GetComponent<FlightMotor>().Halt();ship.Health.InvulnerableUntil=GameTime.Now+60;
            var cantor=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),ship.transform.position+new Vector3(20,0,0));cantor.Passive=false;cantor.enabled=true;
            yield return new WaitForSecondsRealtime(.3f);cantor.Passive=false;yield return null;
            Assert.AreEqual(new Vector2(.5f,0),Text("BossTitleCard").rectTransform.anchorMin,"a flight's card has left its place over the bar");
            cantor.Passive=true;Object.Destroy(cantor.gameObject);
        }
        [UnityTest] public IEnumerator ABossBehindTheLensIsNotTakenForOneUpTheScreen()
        {
            var hero=Hero;Guard();yield return Settle();
            var up=Quaternion.Euler(0,ZoneController.Current.definition.cameraProfile.yaw,0)*Vector3.forward;
            var at=hero.transform.position-up*30;Assert.Less(Camera.main.WorldToViewportPoint(at).z,0,"fixture: she stands behind the lens");
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),at);boss.Passive=false;
            for(float wait=Time.unscaledTime+.3f;Time.unscaledTime<wait;){Stay(boss,at);yield return null;}
            var card=Text("BossTitleCard");Assert.Greater(card.alpha,.5f,"fixture: the card is up");
            // A viewport reading behind the lens comes back mirrored, and she read as far up the screen. The camera will carry the hero up, away from the foot, so the card keeps its place over the bar.
            Assert.AreEqual(new Vector2(.5f,0),card.rectTransform.anchorMin,"a boss far down the screen is taken for one far up it");
            boss.Passive=true;Object.Destroy(boss.gameObject);
        }
        [UnityTest] public IEnumerator ANumberOnTheCardGoesUpOffItNotDownOntoTheBar()
        {
            var hero=Hero;Guard();var camera=Camera.main;
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),hero.transform.position+Vector3.forward*14);boss.Passive=false;
            yield return new WaitForSecondsRealtime(.3f);
            var line=Box(Text("BossTitleEpithet").rectTransform);var card=Box(Text("BossTitleCard").rectTransform);
            var body=new GameObject("Body fixture").AddComponent<Health>();body.id="Fixture";body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;
            body.transform.position=camera.ScreenToWorldPoint(new Vector3(line.center.x,line.center.y,20))-Vector3.up*1.9f;
            body.Receive(new DamagePacket{amount=41,type=DamageType.Pulse});
            for(int i=0;i<4;i++)
            {
                yield return null;
                foreach(var number in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t=>t.name=="Damage"))
                {
                    var at=Box(number.rectTransform,1);
                    Assert.IsFalse(at.Overlaps(line)||at.Overlaps(card),"a number prints across the boss's card");
                    Assert.GreaterOrEqual(at.yMin,card.yMax-camera.pixelHeight*.02f,"a number on the card's epithet was put into its name or down toward the bar");
                }
            }
            boss.Passive=true;Object.Destroy(boss.gameObject);Object.Destroy(body.gameObject);
        }

        // ------------------------------------------------------------------ item 6: signs and fixed things
        static TextMeshPro FixtureSign(string words)
        {
            var view=ZoneController.Current.definition.cameraProfile;
            var go=new GameObject("Sign_Fixture",typeof(TextMeshPro));go.transform.rotation=Quaternion.Euler(72,view.yaw,0);
            var text=go.GetComponent<TextMeshPro>();text.font=UiSkin.Display;text.text=words;text.fontSize=UiKit.WorldFontSize(40);text.alignment=TextAlignmentOptions.Center;text.textWrappingMode=TextWrappingModes.NoWrap;
            go.AddComponent<SignFade>().bossOnly=true;return text;
        }
        static void Lay(TextMeshPro sign,Vector2 screen){sign.transform.position=Under(screen,.05f)+Vector3.up*.05f;}
        static Rect Letters(TextMeshPro sign)
        {
            var camera=Camera.main;var size=sign.GetPreferredValues();
            Vector3 at=sign.transform.position,across=sign.transform.right*(size.x*.5f),up=sign.transform.up*(size.y*.5f);
            Vector3 one=camera.WorldToScreenPoint(at-across-up),two=camera.WorldToScreenPoint(at+across+up);
            return Rect.MinMaxRect(Mathf.Min(one.x,two.x),Mathf.Min(one.y,two.y),Mathf.Max(one.x,two.x),Mathf.Max(one.y,two.y));
        }
        [UnityTest] public IEnumerator ASignRunningUnderTheSkillRowHasGoneByTheTimeItGetsThere()
        {
            yield return Settle();
            var camera=Camera.main;float pixel=camera.pixelHeight/1080f;var sign=FixtureSign("ENTRY HALL");
            var row=Box(Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).First(r=>r.name=="SkillRow"));
            var from=new Vector2(row.center.x,row.yMax+320*pixel);
            Lay(sign,from);yield return new WaitForSecondsRealtime(.4f);
            Assert.Greater(sign.alpha,.9f,"fixture: a sign on open floor shows");
            // At a run the floor goes down the screen at about 460 px a second. It's the frame in s15/024: the hero has just set off, and ENTRY HALL is half gone under the icons.
            float start=Time.unscaledTime,strongest=0;int under=0;
            while(true)
            {
                var at=from-new Vector2(0,(Time.unscaledTime-start)*460*pixel);if(at.y<row.center.y)break;
                Lay(sign,at);yield return null;
                if(!Letters(sign).Overlaps(row))continue;
                under++;strongest=Mathf.Max(strongest,sign.alpha);
            }
            Assert.GreaterOrEqual(under,1,"fixture: the sign went under the row");
            Assert.Less(strongest,.25f,"a sign is still there at that strength under the skill row: 'E RY ALL'");
            Object.Destroy(sign.gameObject);
        }
        [UnityTest] public IEnumerator ASignUnderThePickupFeedGoes()
        {
            yield return Settle();
            var sign=FixtureSign("AB");
            foreach(string id in new[]{"ScrapAlloy","LatticeFilament","RidgeCrystal","HuskCore","ChoirResin"})RpgServices.Inventory.Grant(id,1);
            yield return null;yield return null;
            var band=Box(Picture("PickupBand").rectTransform);Assert.IsTrue(Picture("PickupBand").enabled,"fixture: the feed's up");
            // In its lower part, clear of the vitals over it.
            Lay(sign,new Vector2(band.center.x,band.yMin+band.height*.3f));yield return new WaitForSecondsRealtime(.35f);
            Assert.IsTrue(Letters(sign).Overlaps(band),"fixture: the sign is under the feed");
            Assert.Less(sign.alpha,.05f,"a sign's letters run into the pickup feed's words");
            Object.Destroy(sign.gameObject);
        }

        // ------------------------------------------------------------------ item 7: the hint bar
        [UnityTest] public IEnumerator TheHintBarSaysWhatAPromptWillDoWithTheSameButton()
        {
            var hero=Hero;Guard();yield return Settle();
            PromptService.ForceDevice(PromptDevice.Gamepad);yield return null;yield return null;
            string Strip()=>Text("Skills").text;string attack=PromptService.Tag("Attack")+"  ATTACK",interact=PromptService.Tag("Interact")+"  INTERACT";
            StringAssert.Contains(attack,Strip(),"fixture: with no prompt, A is the attack");
            var prompt=Prompt("Launch",hero.transform.position);yield return null;yield return null;
            Assert.AreSame(prompt,PromptService.Interaction,"fixture: the prompt would answer");
            StringAssert.Contains(interact,Strip(),"the bar and the prompt just over it say different things about one button: A ATTACK under 'A Launch'");
            StringAssert.DoesNotContain("ATTACK",Strip(),"the bar still promises an attack the press won't make");
            // The boss bar hides the prompt and PlayerBrain turns the press into the attack it looks like.
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),hero.transform.position+Vector3.forward*12);boss.Passive=false;yield return null;yield return null;yield return null;
            Assert.IsTrue(string.IsNullOrEmpty(Text("Interaction").text),"fixture: the bar has hidden the prompt");
            StringAssert.Contains(attack,Strip(),"with the boss bar up A is the attack, and the bar has to say so");
            boss.Passive=true;Object.Destroy(boss.gameObject);yield return null;yield return null;
            Assert.AreSame(prompt,PromptService.Interaction,"fixture: the prompt's back");StringAssert.Contains(interact,Strip(),"the bar doesn't follow the prompt back");
            Object.Destroy(prompt.gameObject);yield return null;yield return null;yield return null;
            // A prompt that's shut itself, as a dock does with an enemy on the hero, answers nothing.
            var shut=new GameObject("Shut fixture").AddComponent<ShutPrompt>();shut.prompt="Launch";shut.range=6;shut.transform.position=hero.transform.position;yield return null;yield return null;
            StringAssert.Contains(attack,Strip(),"a prompt that won't answer changes the bar");
            Object.Destroy(shut.gameObject);yield return null;
            // A keyboard's two keys each do what they say.
            prompt=Prompt("Launch",hero.transform.position);PromptService.ForceDevice(PromptDevice.Keyboard);yield return null;yield return null;
            StringAssert.Contains(PromptService.Tag("Attack")+"  ATTACK",Strip(),"on a keyboard J still swings beside E's prompt, and the bar stopped saying so");
            Object.Destroy(prompt.gameObject);
            // In flight it's FIRE.
            yield return Zone("Arena_Flight");PromptService.ForceDevice(PromptDevice.Gamepad);yield return null;yield return null;
            StringAssert.Contains(PromptService.Tag("Fire")+"  FIRE",Strip(),"fixture: with no prompt, A fires");
            prompt=Prompt("Dock",Hero.transform.position);yield return null;yield return null;
            StringAssert.Contains(PromptService.Tag("Interact")+"  INTERACT",Strip(),"A FIRE under 'A Dock'");StringAssert.DoesNotContain("FIRE",Strip(),"the bar still promises a shot the press won't fire");
            Object.Destroy(prompt.gameObject);
        }

        // ------------------------------------------------------------------ item 8: barks
        static void Say(string speaker,string line)=>typeof(BarkPanel).GetMethod("Show",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(Object.FindFirstObjectByType<BarkPanel>(),new object[]{speaker,line});
        /// <summary>A hero as the screen has them, feet to head, about as wide as they stand.</summary>
        static Rect Body(CombatActor who)
        {
            var camera=Camera.main;var at=who.transform.position;Vector2 feet=camera.WorldToScreenPoint(at),head=camera.WorldToScreenPoint(at+Vector3.up*1.9f);
            float half=Mathf.Abs(camera.WorldToScreenPoint(at+camera.transform.right*.5f).x-feet.x);
            return Rect.MinMaxRect(feet.x-half,Mathf.Min(feet.y,head.y),feet.x+half,Mathf.Max(feet.y,head.y));
        }
        [UnityTest] public IEnumerator ABarkSlidesAlongItsColumnOffAHeroAndLandsClear()
        {
            var hero=Hero;var partner=Partner;Guard();yield return Settle();
            Say("Taren","Just a little late.");yield return null;yield return null;
            var plate=Picture("BarkPlate");var words=Text("Bark");Assert.IsTrue(plate.enabled,"fixture: the bark is up");
            Assert.That(plate.color.a,Is.InRange(.8f,.9f),"at .66 a bark's plate goes muddy on a bright floor and the slab shows through it on a dark one");
            Vector2 rest=words.rectTransform.anchoredPosition;
            // She walks under it. It clears her, along its own column.
            Put(partner,Behind(plate.rectTransform,1));yield return new WaitForSecondsRealtime(.4f);
            Assert.IsFalse(Box(plate.rectTransform).Overlaps(Body(partner)),"the bark's words lie across the hero standing under them, with the other hero's name on them");
            Assert.AreEqual(rest.x,words.rectTransform.anchoredPosition.x,.01f,"the bark left its column");
            Assert.IsFalse(Box(plate.rectTransform).Overlaps(Box(Picture("VitalsFrame").rectTransform)),"it slid onto the vitals");
            Assert.IsFalse(Box(plate.rectTransform).Overlaps(Box(Picture("SkillsPlate").rectTransform)),"it slid onto the hint bar");
            Assert.AreEqual(1,words.alpha,.01f,"the words go down with the plate");
            // She leaves, and it goes home.
            Put(partner,hero.transform.position+Vector3.right*2);yield return new WaitForSecondsRealtime(.45f);
            Assert.AreEqual(rest.y,words.rectTransform.anchoredPosition.y,1,"the bark doesn't go back to its place");
            // A new line with her already there comes up clear, with nothing to slide past: read two frames in, before a slide at its speed could have got there.
            words.text="";Put(partner,Behind(plate.rectTransform,1));yield return null;
            Say("Taren","Just a little late.");yield return null;yield return null;
            Assert.IsFalse(Box(plate.rectTransform).Overlaps(Body(partner)),"a line comes up on the hero and only then gets out of the way");
        }
        [UnityTest] public IEnumerator ABarkKeepsOffWhatItIsTalkingAbout()
        {
            var hero=Hero;Guard();yield return Settle();
            Say("Orrin","The tender's free.");yield return null;yield return null;
            var plate=Picture("BarkPlate");var words=Text("Bark");float rest=words.rectTransform.anchoredPosition.y;
            var tender=Prompt("Repair",Under(Box(plate.rectTransform).center,0),.1f);yield return new WaitForSecondsRealtime(.4f);
            Assert.AreNotEqual(rest,words.rectTransform.anchoredPosition.y,"Orrin's box sits on the tender he's saying is free");
            Assert.IsFalse(Box(plate.rectTransform).Contains((Vector2)Camera.main.WorldToScreenPoint(tender.transform.position)),"the box still lies on it");
            Object.Destroy(tender.gameObject);
        }

        // ------------------------------------------------------------------ item 9: the chapter's card
        static TextMeshPro[] Faded()=>Object.FindObjectsByType<SignFade>(FindObjectsSortMode.None).Select(f=>f.GetComponent<TextMeshPro>()).ToArray();
        [UnityTest] public IEnumerator TheChaptersCardClearsTheFloorAndStandsClearOfTheParty()
        {
            Flag("met.Keeper");yield return Zone("TallowDrift");yield return Settle();yield return Settle();
            var hero=Hero;var partner=Partner;Put(partner,hero.transform.position+Vector3.right*2);
            var toast=Object.FindFirstObjectByType<QuestToast>();
            var prompt=Prompt("Talk to Keeper Abri",hero.transform.position);yield return null;yield return null;yield return null;
            StringAssert.Contains("Talk to Keeper Abri",Text("Interaction").text,"fixture: the prompt is up");
            var showing=Faded().Where(s=>s.alpha>.9f).ToArray();Assert.GreaterOrEqual(showing.Length,1,"fixture: a sign is showing");
            Flag("chapter2.homecoming");
            float deadline=Time.unscaledTime+3;while(!toast.Heading.Contains("CHAPTER 2")&&Time.unscaledTime<deadline)yield return null;
            StringAssert.Contains("CHAPTER 2",toast.Heading,"fixture: the chapter ends");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(string.IsNullOrEmpty(Text("Interaction").text),"the prompt crowds in under the chapter's card");
            foreach(var sign in Faded())Assert.Less(sign.alpha,.05f,$"\"{sign.text}\" stays on the floor round the chapter's card: REFUGE / KEEPER ran into its rule");
            var frame=Picture("ChapterCardFrame");Assert.IsNotNull(frame,"the card has no backing of its own");Assert.IsTrue(frame.isActiveAndEnabled,"fixture: the card is up");
            Assert.AreEqual(1,Picture("ChapterCardFrameBacking").color.a,.001f,"the floor shows through the card");
            var box=Box(frame.rectTransform);var drawn=frame.canvas.GetComponentsInChildren<Graphic>().ToList();float units=frame.rectTransform.lossyScale.x;
            foreach(var who in new[]{hero,partner})Assert.Less(box.yMax,Camera.main.WorldToScreenPoint(who.transform.position).y,$"{who.character} stands behind the card's words: the chapter's last picture hides the heroes");
            Assert.GreaterOrEqual(box.yMin,Box(Picture("SkillsPlate").rectTransform).yMax,"the card cuts the hint bar");
            foreach(string name in new[]{"ChapterCardHeading","ChapterCardTitle","ChapterCardBody"})
            {
                var words=Text(name);Assert.Greater(drawn.IndexOf(words),drawn.IndexOf(frame),name+" is under the card's plate");
                words.ForceMeshUpdate();var bounds=words.textBounds;float top=words.transform.TransformPoint(bounds.max).y,bottom=words.transform.TransformPoint(bounds.min).y;
                Assert.LessOrEqual(top,box.yMax-30*units,name+" crosses the plate's top bevel");Assert.GreaterOrEqual(bottom,box.yMin+30*units,name+" crosses the plate's bottom bevel");
            }
            // It leaves them as it found them.
            deadline=Time.unscaledTime+11;while(toast.Showing&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(toast.Showing,"fixture: the card leaves");yield return new WaitForSecondsRealtime(.4f);
            StringAssert.Contains("Talk to Keeper Abri",Text("Interaction").text,"the prompt doesn't come back after the card");
            foreach(var sign in showing)Assert.Greater(sign.alpha,.9f,$"\"{sign.text}\" doesn't come back after the card");
            Object.Destroy(prompt.gameObject);
        }

        // ------------------------------------------------------------------ item 10: a hero down
        [UnityTest] public IEnumerator AHeroDownWithNoFightOnIsTheObjectiveByName()
        {
            var hero=Hero;var partner=Partner;Guard();partner.Health.InvulnerableUntil=0;yield return Settle();
            string calm=Line(),name=DisplayNames.Speaker(partner.character);var arrow=Picture("ObjectiveArrow");var camera=Camera.main;
            Put(partner,hero.transform.position+Vector3.right*9);Kill(partner);
            yield return new WaitForSecondsRealtime(.4f);Assert.IsFalse(partner.Health.Alive,"fixture: the partner is down");
            StringAssert.Contains(name,Line(),"with a hero down and no fight on, the panel doesn't say to get them up, by name");
            Assert.IsTrue(arrow.enabled,"the panel has no arrow on the hero who's down");
            Assert.AreEqual(9,Metres(),1.5f,"the distance isn't the distance to them");
            Vector2 way=(Vector2)camera.WorldToScreenPoint(partner.transform.position)-(Vector2)camera.WorldToScreenPoint(hero.transform.position);Vector2 points=arrow.rectTransform.rotation*Vector3.up;
            Assert.Less(Vector2.Angle(way,points),8,"the arrow doesn't point at them");
            // No 'Here' short of the reach: the revive starts at 2 m, and at 3 m the arrow would stop and nothing would happen.
            Put(hero,partner.transform.position+Vector3.left*2.6f);yield return new WaitForSecondsRealtime(.35f);
            Assert.IsNull(PartyController.Current.ReviveTarget,"fixture: out of the revive's reach");
            Assert.IsTrue(arrow.enabled,"the arrow stops short of the reach, with nothing happening");StringAssert.DoesNotContain("Here",Text("Objective").text,"'Here', short of the reach, with nothing happening");
            // Beside them the revive has started, and the arrow's job is done.
            Put(hero,partner.transform.position+Vector3.left*1.2f);yield return new WaitForSecondsRealtime(.35f);
            Assert.AreSame(partner,PartyController.Current.ReviveTarget,"fixture: the revive has started");Assert.IsFalse(arrow.enabled,"the arrow spins on the spot during the revive");
            float deadline=Time.unscaledTime+4;while(!partner.Health.Alive&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(partner.Health.Alive,"fixture: the partner got up");yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual(calm,Line(),"the panel doesn't go back to what it was saying");
            // In a fight the status panel says it, and the panel's goal stands.
            Kill(partner);var hound=Awake(hero.transform.position+Vector3.forward*8);yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual(calm,Line(),"with a fight on, the panel takes over from what the fight's about");
            hound.Passive=true;Object.Destroy(hound.gameObject);yield return new WaitForSecondsRealtime(.4f);
            StringAssert.Contains(name,Line(),"the fight's over and the panel doesn't ask for the hero who's down");
        }
        [UnityTest] public IEnumerator HessaWaitingOnAHeroDownSendsThePlayerToTheHero()
        {
            Flag("bossdown.HessaKorl");yield return Zone("SunkenRelay");yield return Settle();
            var hero=Hero;var partner=Partner;string name=DisplayNames.Speaker(partner.character);
            StringAssert.Contains("Hear Hessa out",Line(),"fixture: the talk is what's next");
            Put(partner,hero.transform.position+Vector3.right*40);Kill(partner);yield return new WaitForSecondsRealtime(.4f);
            Assert.IsFalse(partner.Health.Alive,"fixture: the partner is down");
            StringAssert.Contains(name,Line(),"the panel says 'Hear Hessa out' with its arrow on her, and she won't speak until everyone's up");
            StringAssert.DoesNotContain("Hessa",Line(),"the panel still sends the player to her");
            var mark=Picture("GoalEdgeMark");Assert.IsNotNull(mark,"nothing on the screen's edge says where");Assert.IsTrue(mark.enabled,"the hero who's down is off screen and nothing at the screen's edge says where: only their boots poked in");
            Vector2 points=mark.rectTransform.rotation*Vector3.up;Vector2 arrow=Picture("ObjectiveArrow").rectTransform.rotation*Vector3.up;Assert.Less(Vector2.Angle(points,arrow),3,"the mark and the panel's arrow disagree");
            partner.Health.Heal(partner.Health.maximum);yield return new WaitForSecondsRealtime(.4f);
            StringAssert.Contains("Hear Hessa out",Line(),"up again, and the panel doesn't go back to her");
            Assert.IsFalse(mark.enabled,"the mark stays with nobody down");
        }

        // ------------------------------------------------------------------ item 11: warp labels in the wide shots
        [UnityTest] public IEnumerator AWarpSignHoldsItsSizeWhenTheFlightCameraDrawsBack()
        {
            yield return Zone("TallowApproach");
            var sign=Object.FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None).First(t=>t.text.StartsWith("Warp")&&t.TryGetComponent<SignFade>(out _));
            var hero=Hero;Put(hero,new Vector3(sign.transform.position.x,hero.transform.position.y,sign.transform.position.z-3));
            yield return new WaitForSecondsRealtime(1.5f);
            float Pixels(){var view=Camera.main;float depth=Vector3.Dot(sign.transform.position-view.transform.position,view.transform.forward);return sign.fontSize*.1f*sign.transform.lossyScale.y/(2*depth*Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad))*1080;}
            float atRest=Pixels();Assert.GreaterOrEqual(atRest,24,"fixture: at rest the sign stands at the type floor");Assert.AreEqual(1,sign.transform.localScale.y,.001f,"at rest the sign isn't the size it was made");
            // The Tallow line's wide view, by the lease that draws it: 60 m back against the 26 the sign was sized for, and 12 px letters on WARP - CINDER HALO.
            var rig=Object.FindFirstObjectByType<CameraRig>();var view2=Camera.main;float restY=view2.transform.position.y;var room=new Bounds(hero.transform.position,Vector3.one*24);
            for(float end=Time.unscaledTime+1.6f;Time.unscaledTime<end;){rig.FrameSceneInterest(rig,room,new Rect(.1f,.1f,.8f,.8f));yield return null;}
            Assert.Greater(view2.transform.position.y-restY,18,"fixture: the camera has drawn well back");
            float wide=Pixels();Assert.GreaterOrEqual(wide,24,$"the sign stands {wide:0} px tall in the wide view, and {atRest:0} at rest: an exit's name is hard to read at the distance it's first seen");
            var plate=sign.transform.Find(SignStyle.PlateName);if(plate!=null)Assert.AreEqual(sign.transform.lossyScale.y,plate.lossyScale.y,.001f,"the sign's plate doesn't grow with its words");
            // The lease over, the camera comes home over its 1.4 s.
            yield return new WaitForSecondsRealtime(2.6f);
            Assert.AreEqual(1,sign.transform.localScale.y,.001f,"the sign stays grown with the camera back at rest");
        }

        // ------------------------------------------------------------------ item 12: the way, at the screen's edge
        [UnityTest] public IEnumerator AMarkAtTheScreensEdgeShowsTheWayWithNoFightOn()
        {
            Flag("met.Orrin");yield return Zone("Hub_CinderHalo");
            var hero=Hero;hero.Health.InvulnerableUntil=GameTime.Now+60;var goal=InteractionPrompt.Active.OfType<WarpBeacon>().First(w=>w.scene=="Sorrel_Ridges").transform.position;
            Put(hero,goal+new Vector3(-55,0,-55));yield return new WaitForSecondsRealtime(1.2f);
            var mark=Picture("GoalEdgeMark");Assert.IsNotNull(mark,"nothing on the screen but specks, and nothing at its edge to say which way");
            Assert.IsTrue(mark.enabled,"the objective's target is off screen and no mark says where");
            var arrow=Picture("ObjectiveArrow");Assert.IsTrue(arrow.enabled,"fixture: the panel points somewhere");
            Assert.AreSame(arrow.sprite,mark.sprite,"the mark isn't the arrow's shape");
            Assert.Greater(mark.color.b,.6f,"the mark is the boss's orange, not the objective's cream");
            Vector2 points=mark.rectTransform.rotation*Vector3.up,shows=arrow.rectTransform.rotation*Vector3.up;Assert.Less(Vector2.Angle(points,shows),3,"the mark and the panel's arrow disagree");
            // It stands on the edge the way lies toward, and on the screen, with no panel over it.
            var camera=Camera.main;Vector2 from=camera.WorldToViewportPoint(hero.transform.position),at=mark.rectTransform.anchorMin;
            Assert.Greater(Vector2.Dot((at-from).normalized,new Vector2(points.x*9/16,points.y).normalized),.8f,"the mark doesn't stand the way it points");
            Assert.That(at.x,Is.InRange(0f,1f));Assert.That(at.y,Is.InRange(0f,1f));
            // By its place, not its corners: it's turned, and a turned rect's corners don't make a box.
            float half=20*mark.rectTransform.lossyScale.x;var box=new Rect(at.x*camera.pixelWidth-half,at.y*camera.pixelHeight-half,half*2,half*2);
            foreach(string plate in new[]{"VitalsFrame","ObjectiveFrame","SkillsPlate","ZonePlate"})Assert.IsFalse(box.Overlaps(Box(Picture(plate).rectTransform)),"the mark lies under "+plate);
            // A menu turns the HUD's canvas off and a graphic has no canvas then. The mark goes on working out its place behind it: asking the graphic threw every frame, and an error logged here fails the test.
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open();yield return null;yield return null;yield return null;
            menu.Close();yield return null;yield return null;
            Assert.IsTrue(mark.enabled,"the mark doesn't come back after a menu");
            // Off during a fight.
            var dart=Spawn("ChoristerDart",hero.transform.position+Vector3.forward*20);dart.Passive=false;yield return null;yield return null;
            Assert.IsFalse(mark.enabled,"the mark stays up in a fight");
            dart.Passive=true;Object.Destroy(dart.gameObject);yield return null;yield return null;
            Assert.IsTrue(mark.enabled,"the mark doesn't come back after the fight");
            // And off with the goal on the screen.
            Put(hero,goal+new Vector3(-4,0,-4));yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsFalse(mark.enabled,"the mark stays up with the goal in plain sight");
            // On foot the walk has its arrow and the world's signs: only a hero down gets a mark.
            yield return Zone("Sorrel_Ridges");yield return Settle();
            Assert.IsFalse(Picture("GoalEdgeMark").enabled,"the mark shows on an ordinary walk");
        }
        [UnityTest] public IEnumerator AMarkWithNowhereClearOnItsEdgeStaysWhereTheWayLeads()
        {
            yield return null;
            var edge=Object.FindFirstObjectByType<BossEdgeMark>();var camera=Camera.main;
            var slide=typeof(BossEdgeMark).GetMethod("OffThePlates",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);Assert.IsNotNull(slide,"a mark in a corner lies under the objective, which draws on top");
            // Along the foot the hint bar and the skill row fill the whole width: sliding on with nowhere to land took the mark for a boss down and to the left to the bottom middle.
            PanelFade.BeginPlates();PanelFade.AddPlate((RectTransform)Surface("HUD").transform);
            var at=new Vector2(.2f,.05f);
            Assert.AreEqual(at,(Vector2)slide.Invoke(edge,new object[]{at,56f,camera,false}),"with nowhere clear on its edge the mark has left where the way leads");
            // With room further along, it goes only as far as that.
            PanelFade.BeginPlates();PanelFade.AddPlate(Picture("VitalsFrame").rectTransform);
            var corner=new Vector2(.1f,.95f);var landed=(Vector2)slide.Invoke(edge,new object[]{corner,56f,camera,false});
            float half=28*Surface("HUD").scaleFactor;var box=new Rect(landed.x*camera.pixelWidth-half,landed.y*camera.pixelHeight-half,half*2,half*2);
            Assert.IsFalse(box.Overlaps(Box(Picture("VitalsFrame").rectTransform)),"with room further along its edge the mark stays under the plate");
            Assert.AreEqual(corner.y,landed.y,.001f,"the mark left its edge");Assert.Less(landed.x,.5f,"the mark went further along its edge than it had to");
        }

        // ------------------------------------------------------------------ item 13: the prompt's plate, and a goal behind a plate
        [UnityTest] public IEnumerator ThePromptsPlateStepsAsideForThePartnerAndForWhatItLiesOn()
        {
            var hero=Hero;var partner=Partner;Guard();yield return Settle();
            var prompt=Prompt("Dock",hero.transform.position);yield return null;yield return null;yield return null;
            var bar=Picture("InteractionPlate");Assert.IsTrue(bar.enabled,"fixture: the prompt is up");Assert.Greater(Plate("InteractionPlate"),.98f,"fixture: nothing is behind it");
            // A calm walk, or a calm flight: it cut Sela's wing.
            Put(partner,Behind(bar.rectTransform,.75f));yield return new WaitForSecondsRealtime(.25f);
            Assert.Less(Plate("InteractionPlate"),.42f,"the prompt's plate stays solid over the partner");
            Assert.AreEqual(1,Text("Interaction").alpha*Text("Interaction").canvasRenderer.GetAlpha(),.01f,"the prompt's words go down with its plate");
            Assert.Greater(Plate("ObjectiveFrame"),.98f,"the other plates go for a partner on a calm walk too");
            Put(partner,hero.transform.position+Vector3.right*2);yield return new WaitForSecondsRealtime(1.4f);
            Assert.Greater(Plate("InteractionPlate"),.98f,"the plate doesn't come back");
            // It lay on a warp, under the warp's own sign.
            var warp=Prompt("Warp",Behind(bar.rectTransform,0),.1f);yield return new WaitForSecondsRealtime(.25f);
            Assert.Less(Plate("InteractionPlate"),.42f,"the prompt's plate stays solid over the warp it lies on");
            Object.Destroy(warp.gameObject);Object.Destroy(prompt.gameObject);
        }
        [UnityTest] public IEnumerator ThePanelStepsAsideForWhereItSendsTheParty()
        {
            yield return Zone("TallowApproach");
            var hero=Hero;var camera=Camera.main;StringAssert.Contains("Dock",Line(),"fixture: the objective names the dock");
            var pad=InteractionPrompt.Active.OfType<DockingPad>().First().transform.position;var frame=Picture("ObjectiveFrame").rectTransform;
            // Stood so the pad lies behind the panel: half of it was hidden by the panel sending the party there.
            for(int look=0;look<3;look++)
            {
                var ray=camera.ScreenPointToRay(Box(frame).center);var spot=ray.GetPoint((pad.y-ray.origin.y)/ray.direction.y);
                var shift=pad-spot;shift.y=0;Put(hero,hero.transform.position+shift);yield return new WaitForSecondsRealtime(1f);
            }
            var seen=camera.WorldToScreenPoint(pad);Assert.IsTrue(Box(frame).Contains((Vector2)seen),"fixture: the pad is behind the panel");
            Assert.Less(Plate("ObjectiveFrame"),.42f,"the panel stays solid over the pad it's sending the party to");
            Assert.AreEqual(1,Text("Objective").alpha,.01f,"its words go down with its plate");
        }
        [UnityTest] public IEnumerator AShipsWingUnderAPanelMovesItsPlate()
        {
            yield return Zone("Arena_Flight");
            var hero=Hero;var partner=Partner;Guard();hero.GetComponent<FlightMotor>().Halt();
            // A fight, which is when the partner counts. Nobody locked, so the camera stays where it rests.
            var dart=Spawn("ChoristerDart",hero.transform.position+Vector3.forward*14);dart.Passive=false;yield return Settle();
            var frame=Picture("ObjectiveFrame").rectTransform;var box=Box(frame);float units=frame.lossyScale.x;
            Assert.Greater(Plate("ObjectiveFrame"),.98f,"fixture: nothing is behind the panel");
            // Her middle stands clear of it by less than her hull: s10/087, wedged between the objective and the card under it with only the wings showing, and no plate stepping aside.
            Put(partner,Under(new Vector2(box.xMin-40*units,box.center.y),0));yield return new WaitForSecondsRealtime(.25f);
            Assert.IsFalse(box.Contains((Vector2)Camera.main.WorldToScreenPoint(partner.transform.position)),"fixture: her middle is beside the panel");
            Assert.Less(Plate("ObjectiveFrame"),.42f,"a ship's wing lies under the panel and its plate stays solid");
            Assert.AreEqual(1,Text("Objective").alpha,.01f,"its words go down with its plate");
            // Well clear of it, it comes back: this is the hull's width, not a wide margin.
            Put(partner,Under(new Vector2(box.xMin-420*units,box.center.y),0));yield return new WaitForSecondsRealtime(1.2f);
            Assert.Greater(Plate("ObjectiveFrame"),.98f,"the panel stays faded for a ship well clear of it");
            dart.Passive=true;Object.Destroy(dart.gameObject);
        }
    }
}
