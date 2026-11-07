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
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The eye pass on the HUD: readers looked at a whole playthrough and reported what the words and plates did on screen. These
    /// checks measure the same things in rendered glyphs and screen rects. Whether the type now holds on a bright floor is still a question
    /// for eyes: they pin what makes it hold, and the next capture has to judge it. Every check here fails on the build it was written against.</summary>
    public sealed class QaHudLookTests
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
        static float ColorGap(Color a,Color b)=>Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b);
        sealed class FixturePrompt:InteractionPrompt{public override void Interact(){}}

        // ------------------------------------------------------------------ measuring type
        /// <summary>Where the letters really stand, in the label's own space. TMP's text bounds report the font's ascent and descent, not the glyphs'.</summary>
        static Rect Ink(TMP_Text text)
        {
            text.ForceMeshUpdate();var info=text.textInfo;float low=float.MaxValue,high=float.MinValue,left=float.MaxValue,right=float.MinValue;
            for(int i=0;i<info.characterCount;i++)
            {
                var c=info.characterInfo[i];if(!c.isVisible)continue;
                var metrics=c.textElement.glyph.metrics;float top=c.baseLine+metrics.horizontalBearingY*c.scale;
                high=Mathf.Max(high,top);low=Mathf.Min(low,top-metrics.height*c.scale);left=Mathf.Min(left,c.origin);right=Mathf.Max(right,c.xAdvance);
            }
            Assert.Greater(high,low,"fixture: "+text.name+" has glyphs to measure");
            return Rect.MinMaxRect(left,low,right,high);
        }
        /// <summary>Where the dark opening of ui-panel.png starts, in texture rows from the sheet's bottom: measured on the art, at the end of the bevel's cyan line.</summary>
        const float OpeningTopRow=902.5f,OpeningBottomRow=137.5f;
        /// <summary>How far in from its rect a frame's opening starts, in canvas units, through the frame's nine-slice at this frame's size and weight.</summary>
        static float Inset(Image frame,float rows)
        {
            var sprite=frame.sprite;float perRow=1/(frame.pixelsPerUnit*frame.pixelsPerUnitMultiplier),border=sprite.border.y;
            if(rows<=border)return rows*perRow;
            float middle=(frame.rectTransform.rect.height-2*border*perRow)/(sprite.rect.height-2*border);
            return border*perRow+(rows-border)*middle;
        }
        static float TopBevel(Image frame)=>Inset(frame,frame.sprite.rect.yMax-OpeningTopRow);
        static float BottomBevel(Image frame)=>Inset(frame,OpeningBottomRow-frame.sprite.rect.yMin);
        /// <summary>The air above and below the letters inside a frame's opening, in canvas units.</summary>
        static (float above,float below) Air(Image frame,TMP_Text text)
        {
            var ink=Ink(text);var c=new Vector3[4];frame.rectTransform.GetWorldCorners(c);float scale=frame.rectTransform.lossyScale.y;
            float top=text.transform.TransformPoint(new Vector3(0,ink.yMax,0)).y,bottom=text.transform.TransformPoint(new Vector3(0,ink.yMin,0)).y;
            return ((c[1].y-top)/scale-TopBevel(frame),(bottom-c[0].y)/scale-BottomBevel(frame));
        }
        static void Sits(string plate,TMP_Text text,List<string> failures)
        {
            Assert.IsFalse(string.IsNullOrEmpty(text.text),"fixture: "+text.name+" has words");
            var frame=Picture(plate);Assert.IsNotNull(frame,"fixture: "+plate);Assert.IsTrue(frame.enabled,"fixture: "+plate+" is showing");
            var (above,below)=Air(frame,text);
            if(above<3)failures.Add($"{plate}: \"{text.text}\" has {above:0.0} of air under the frame's top bevel: its letters touch it");
            if(below<3)failures.Add($"{plate}: \"{text.text}\" has {below:0.0} of air over the frame's bottom bevel: its letters sit on it, or their tails cross it");
            if(Mathf.Abs(above-below)>2.5f)failures.Add($"{plate}: \"{text.text}\" has {above:0.0} of air above and {below:0.0} below: it sits off centre in its plate");
        }

        // ------------------------------------------------------------------ item 1: words sit in the middle of their plates
        [UnityTest] public IEnumerator EveryThinPlateCentresItsLettersInItsOpening()
        {
            var hero=Hero;var failures=new List<string>();
            var fixture=new GameObject("Prompt fixture").AddComponent<FixturePrompt>();fixture.prompt="Finish the walkway repair";fixture.range=6;fixture.transform.position=hero.transform.position;
            var hound=Spawn("Ridgehound",hero.transform.position+Vector3.forward*8);yield return null;hero.target=hound.Health;hero.TargetLocked=true;
            BarkService.Play("Taren","swap",true);
            yield return null;yield return null;yield return null;
            Sits("InteractionPlate",Text("Interaction"),failures);
            Sits("SkillsPlate",Text("Skills"),failures);
            Sits("TargetPlate",Text("Target"),failures);
            Sits("BarkPlate",Text("Bark"),failures);
            // A plate measures again when its words change: the prompt's next line has its own letters.
            fixture.prompt="Hold";yield return null;yield return null;yield return null;
            Sits("InteractionPlate",Text("Interaction"),failures);
            // The zone's name had no backing at all: a strut or a neon strip ran straight through its letters.
            if(Picture("ZonePlate")==null)failures.Add("the zone's name has nothing behind it, so scenery runs straight through its letters");else Sits("ZonePlate",Text("Zone"),failures);
            Assert.IsEmpty(failures,string.Join("\n",failures));
            Object.Destroy(fixture.gameObject);
        }
        [UnityTest] public IEnumerator EveryFramesOpeningStandsEvenInItsRect()
        {
            yield return null;var failures=new List<string>();
            foreach(string name in new[]{"VitalsFrame","ObjectiveFrame","QuestToastFrame","DialoguePanel"})
            {
                var frame=Picture(name);Assert.IsNotNull(frame,"fixture: "+name);
                float top=TopBevel(frame),bottom=BottomBevel(frame);
                if(Mathf.Abs(top-bottom)>2)failures.Add($"{name}: its opening starts {top:0.0} under the rect's top and {bottom:0.0} over its bottom, so anything centred in the frame looks low");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ item 2: loose words hold on a bright floor
        static void Holds(TMP_Text text,Dictionary<TMP_FontAsset,Material> shared,List<string> failures)
        {
            if(text==null){failures.Add("fixture: a label is missing");return;}
            var material=text.fontSharedMaterial;
            if(material.GetFloat("_OutlineWidth")<.3f)failures.Add($"{text.name}: outline {material.GetFloat("_OutlineWidth"):0.00} is too thin to hold Nunito's strokes on a bright floor");
            if(material.GetFloat("_FaceDilate")<.3f)failures.Add($"{text.name}: the outline eats a face dilated only {material.GetFloat("_FaceDilate"):0.00}, and thin strokes go");
            if(!material.IsKeywordEnabled("UNDERLAY_ON"))failures.Add($"{text.name}: no soft underlay behind the letters");
            if(material==text.font.material)failures.Add($"{text.name}: drawn with the font's plain material");
            if(shared.TryGetValue(text.font,out var first)){if(first!=material)failures.Add($"{text.name} draws with a material of its own: anything spawned often would make one per label");}
            else shared[text.font]=material;
        }
        [UnityTest] public IEnumerator LooseWordsShareOneHeavyStylePerFont()
        {
            var hero=Hero;hero.Health.InvulnerableUntil=GameTime.Now+30;
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),hero.transform.position+Vector3.forward*12);boss.Passive=false;
            RpgServices.Inventory.Grant("ScrapAlloy",2);RpgServices.Inventory.Grant("RidgeCrystal",1);
            BarkService.Play("Taren","swap",true);
            var body=new GameObject("Body fixture").AddComponent<Health>();body.id="Fixture";body.maximum=body.integrity=500;body.transform.position=hero.transform.position+Vector3.right*5;
            body.Receive(new DamagePacket{amount=5,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.2f);
            body.Receive(new DamagePacket{amount=5,type=DamageType.Pulse});yield return null;
            var failures=new List<string>();var shared=new Dictionary<TMP_FontAsset,Material>();
            foreach(string name in new[]{"Zone","BossName","BossEpithet","BossState","BossTitleCard","BossTitleEpithet","SkillLabel0","Bark","Skills","Interaction","Target"})Holds(Text(name),shared,failures);
            var lines=Live("PickupLine");Assert.GreaterOrEqual(lines.Length,2,"fixture: two pickup lines");foreach(var line in lines)Holds(line,shared,failures);
            var numbers=Live("Damage");Assert.GreaterOrEqual(numbers.Length,1,"fixture: a number");foreach(var number in numbers)Holds(number,shared,failures);
            Assert.IsEmpty(failures,string.Join("\n",failures));
            foreach(var font in shared.Keys)Assert.AreEqual(0,font.material.GetFloat("_OutlineWidth"),.001f,font.name+": the style was painted onto the font's own material, so every plain label in the game got it too");
            // A build keeps only the shader variants some material asset uses: without this one the underlay is dropped from every player, silently.
            var keeper=Resources.Load<Material>("Fonts & Materials/Legible Type Variants");
            Assert.IsNotNull(keeper,"no material asset keeps the underlay's shader variant: the soft edge shows in the editor and vanishes from the build");
            Assert.AreEqual(TMP_Settings.defaultFontAsset.material.shader,keeper.shader,"the variant kept isn't the one the words use");
            Assert.IsTrue(keeper.IsKeywordEnabled("UNDERLAY_ON"),"the material kept for the build doesn't turn the underlay on");
            // The bar's words lie over the fullest markings, so they get a dark band behind them.
            var band=Picture("BossBand");Assert.IsNotNull(band,"the boss bar's words have nothing behind them on a bright floor");
            Assert.IsTrue(band.isActiveAndEnabled,"the band isn't up with the bar");Assert.That(band.color.a,Is.InRange(.4f,.8f),"the band either doesn't darken the floor or blacks out the fight under it");
            var state=Text("BossState");var name2=Text("BossName");var drawn=band.canvas.GetComponentsInChildren<Graphic>().ToList();
            Assert.Less(drawn.IndexOf(band),drawn.IndexOf(state),"the band is drawn over the words");
            Assert.Less(drawn.IndexOf(band),drawn.IndexOf(Picture("Skill0")),"the band's lower edge dims the skill icons in the middle of a boss fight");
            Assert.IsTrue(Box(band.rectTransform).Contains(Box(state.rectTransform).center)&&Box(band.rectTransform).Contains(Box(name2.rectTransform).center),"the band doesn't lie behind the bar's words");
            // The state line is what the player acts on.
            Assert.Greater(state.fontSize,name2.fontSize,"the fight's instruction is smaller than the boss's name");Assert.Greater(state.fontSize,Text("BossEpithet").fontSize,"the fight's instruction is no bigger than its flavour text");
            Assert.AreEqual(FontStyles.Bold,state.fontStyle&FontStyles.Bold,"the fight's instruction is no heavier than anything around it");
            boss.Health.Expose(2);yield return null;yield return null;
            StringAssert.Contains("OPENING",state.text,"fixture: an opening");
            Assert.Greater(state.color.r,.9f,"OPENING isn't warm: it looks like every other line");Assert.Less(state.color.b,.6f,"OPENING isn't warm: it looks like every other line");
            boss.Passive=true;Object.Destroy(boss.gameObject);Object.Destroy(body.gameObject);
        }

        // ------------------------------------------------------------------ item 3: damage numbers
        static Rect Glyphs(TMP_Text text)
        {
            text.ForceMeshUpdate();var b=text.textBounds;
            Vector3 lo=text.transform.TransformPoint(b.min),hi=text.transform.TransformPoint(b.max);return Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);
        }
        static Health Dummy(Vector3 at)
        {var body=new GameObject("Body fixture").AddComponent<Health>();body.id="Fixture";body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;body.transform.position=at;return body;}
        [UnityTest] public IEnumerator ABurstOnOneBodyLandsAsSeparateNumbersBesideIt()
        {
            var hero=Hero;var body=Dummy(hero.transform.position+Vector3.forward*5);var camera=Camera.main;
            float pixel=camera.pixelHeight/1080f;int most=0;
            var types=System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().ToArray();
            for(int hit=0;hit<14;hit++)
            {
                body.Receive(new DamagePacket{amount=100+hit*37,type=types[hit%types.Length]});
                for(float until=Time.unscaledTime+.13f;Time.unscaledTime<until;)
                {
                    yield return null;
                    var numbers=Live("Damage");most=Mathf.Max(most,numbers.Length);
                    var boxes=numbers.Select(Glyphs).ToArray();
                    for(int i=0;i<boxes.Length;i++)
                    {
                        for(int j=i+1;j<boxes.Length;j++)
                        {
                            var a=boxes[i];a.xMin+=2*pixel;a.xMax-=2*pixel;a.yMin+=2*pixel;a.yMax-=2*pixel;
                            Assert.IsFalse(a.Overlaps(boxes[j]),$"\"{numbers[i].text}\" lands on top of \"{numbers[j].text}\" on one body: the numbers smear together");
                        }
                        var spot=(Vector2)camera.WorldToScreenPoint(body.transform.position+Vector3.up*1.9f);
                        Assert.Less((boxes[i].center-spot).magnitude,340*pixel,$"\"{numbers[i].text}\" flies off the body it belongs to");
                    }
                }
            }
            Assert.GreaterOrEqual(most,4,"fixture: several numbers were up at once");
            Object.Destroy(body.gameObject);
        }
        [UnityTest] public IEnumerator ANumberHoldsMostOfItsLifeAndHitsTogetherReadAsOne()
        {
            var hero=Hero;var body=Dummy(hero.transform.position+Vector3.forward*5);
            var plain=System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
            for(int i=0;i<3;i++)body.Receive(new DamagePacket{amount=20,type=plain});
            yield return null;
            var numbers=Live("Damage");
            Assert.AreEqual(1,numbers.Length,"a volley landing together prints a stack of the same digits: "+string.Join(" | ",numbers.Select(t=>t.text)));
            float expected=DamageMath.Resolve(20,plain,body.weakness,body.resistance,false,false,1)*3;
            Assert.AreEqual(expected.ToString("0"),numbers[0].text,"the one number doesn't add up what landed");
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsTrue(numbers[0]!=null,"the number is gone before it can be read");
            Assert.Greater(numbers[0].alpha,.98f,"the number is already fading at half a second: it's only readable for a moment");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(numbers[0]==null,"the number never leaves");
            Object.Destroy(body.gameObject);
        }
        [UnityTest] public IEnumerator ANumberStaysOnTheScreenAndOffThePanels()
        {
            yield return null;
            var camera=Camera.main;var vitals=Picture("VitalsFrame");var screen=Box((RectTransform)vitals.canvas.transform);var c=new Vector3[4];vitals.rectTransform.GetWorldCorners(c);
            // A body just off the screen's left edge, level with the vitals: pressed back on screen, its number mustn't land across them.
            var body=Dummy(camera.ScreenToWorldPoint(new Vector3(-camera.pixelWidth*.2f,(c[0].y+c[2].y)*.5f,20))-Vector3.up*1.9f);
            body.Receive(new DamagePacket{amount=43,type=DamageType.Pulse});
            for(int i=0;i<4;i++)
            {
                yield return null;
                var box=Box(Live("Damage").Single().rectTransform,1);
                Assert.IsTrue(box.xMin>=screen.xMin&&box.xMax<=screen.xMax&&box.yMin>=screen.yMin&&box.yMax<=screen.yMax,"a number is cut off by the edge of the screen, as '43!' was over a boss at the top");
                Assert.IsFalse(box.Overlaps(Box(vitals.rectTransform)),"a number pressed back on screen lands across the vitals, over what the player is reading mid-fight");
            }
            Object.Destroy(body.gameObject);
        }

        // ------------------------------------------------------------------ item 4: the reticle
        static Vector3 DrawnCentre(Component root)
        {
            var bounds=new Bounds();bool found=false;
            foreach(var view in root.GetComponentsInChildren<Renderer>())
            {
                if(view is not (MeshRenderer or SkinnedMeshRenderer))continue;
                if(found)bounds.Encapsulate(view.bounds);else{bounds=view.bounds;found=true;}
            }
            Assert.IsTrue(found,"fixture: the body is drawn");return bounds.center;
        }
        [UnityTest] public IEnumerator TheReticleMarksTheBodyItIsLockedTo()
        {
            yield return Zone("Arena_Flight");
            var hero=Hero;hero.Health.InvulnerableUntil=GameTime.Now+30;hero.GetComponent<FlightMotor>().Halt();
            var dart=Spawn("ChoristerDart",hero.transform.position+Vector3.forward*10+Vector3.right*3);yield return null;yield return null;
            hero.target=dart.Health;hero.TargetLocked=true;yield return null;yield return null;yield return null;
            var camera=Camera.main;var reticle=Picture("TargetReticle");Assert.IsTrue(reticle.enabled,"fixture: the reticle is up");
            var centre=(Vector2)camera.WorldToScreenPoint(DrawnCentre(dart));
            float off=Vector2.Distance((Vector2)reticle.rectTransform.position,centre)/(camera.pixelHeight/1080f);
            Assert.Less(off,12,$"the reticle sits {off:0} px from the dart it's locked to: it brackets empty sky");
            // It lies under the plates, and dims where it would cross their words.
            var drawn=reticle.canvas.GetComponentsInChildren<Graphic>().ToList();
            foreach(string plate in new[]{"TargetPlate","VitalsFrame"})
                Assert.Less(drawn.IndexOf(reticle),drawn.IndexOf(Picture(plate)),$"the reticle is drawn over {plate}: its brackets cut through the words ('LO..KED')");
            var words=Text("Target");var frame=Picture("TargetPlate");var c=new Vector3[4];frame.rectTransform.GetWorldCorners(c);
            // A locked target moved up the screen makes the flight camera reframe, so it's held under the plate until the camera settles.
            for(int settling=0;settling<90;settling++)
            {
                var ray=camera.ScreenPointToRay((c[0]+c[2])*.5f);dart.transform.position=ray.GetPoint((dart.transform.position.y+.3f-ray.origin.y)/ray.direction.y);
                Physics.SyncTransforms();yield return null;
            }
            yield return null;Assert.IsTrue(reticle.enabled,"fixture: the lock held");
            StringAssert.Contains("LOCK",words.text,"fixture: the plate names the lock");
            Assert.Less(reticle.color.a,.5f,"a bright reticle shows through the plate's words when its target is behind them");
        }

        // ------------------------------------------------------------------ item 5: plates step aside
        /// <summary>The ground under a screen point, for a body whose middle stands this high above the hero's feet.</summary>
        static Vector3 Behind(RectTransform panel,float height)
        {
            var c=new Vector3[4];panel.GetWorldCorners(c);var ray=Camera.main.ScreenPointToRay((c[0]+c[2])*.5f);
            float y=Hero.transform.position.y+height;return ray.GetPoint((y-ray.origin.y)/ray.direction.y)-Vector3.up*height;
        }
        static IEnumerator Hold(EnemyBrain enemy,Vector3 at,float seconds)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){enemy.transform.position=at;Physics.SyncTransforms();yield return null;}}
        /// <summary>How far a plate has stepped aside. It's the plate alone that goes: QaHudSecondTests pins the words staying.</summary>
        static float Plate(string image)=>Picture(image).canvasRenderer.GetAlpha();
        [UnityTest] public IEnumerator APanelStepsAsideForWhatItHidesAndComesBackGently()
        {
            var hero=Hero;var partner=Partner;hero.Health.InvulnerableUntil=partner.Health.InvulnerableUntil=GameTime.Now+60;
            yield return Settle();
            Assert.Greater(Plate("VitalsFrame"),.98f,"the vitals are faded with nobody behind them");Assert.Greater(Plate("ObjectiveFrame"),.98f,"the objective is faded with nobody behind it");
            // In a fight the partner walks behind the vitals, down to about a third, quickly. On a calm walk she's no reason to: QaHudSecondTests pins that.
            var fight=Spawn("Ridgehound",hero.transform.position+Vector3.left*6+Vector3.back*3);fight.Passive=false;
            Put(partner,Behind(Picture("VitalsFrame").rectTransform,1));
            yield return new WaitForSecondsRealtime(.3f);
            Assert.Less(Plate("VitalsFrame"),.42f,"the partner is behind the vitals and the panel stays solid");
            Assert.Greater(Plate("VitalsFrame"),.3f,"the panel fades so far its numbers are gone");
            Assert.Greater(Plate("ObjectiveFrame"),.98f,"the objective fades for someone behind the other corner");
            // Then she's gone, and it comes back gently: no pop.
            Put(partner,hero.transform.position+Vector3.right*2);Object.Destroy(fight.gameObject);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.Less(Plate("VitalsFrame"),.8f,"the panel snaps back the moment nobody's behind it, and would flicker on a body at its edge");
            yield return new WaitForSecondsRealtime(1f);
            Assert.Greater(Plate("VitalsFrame"),.98f,"the panel never comes back");
            // A body at rest isn't the fight; an awake enemy is.
            var at=Behind(Picture("ObjectiveFrame").rectTransform,.75f);var hound=Spawn("Ridgehound",at);
            yield return Hold(hound,at,.3f);
            Assert.Greater(Plate("ObjectiveFrame"),.98f,"the objective fades for a sleeping enemy, which isn't what the player needs to see");
            hound.Passive=false;yield return Hold(hound,at,.3f);
            Assert.Less(Plate("ObjectiveFrame"),.42f,"an awake enemy is behind the objective and the panel stays solid");
            hound.Passive=true;hound.transform.position=hero.transform.position+Vector3.right*30;Physics.SyncTransforms();
            // What the player is locked onto counts wherever it is, asleep or not.
            yield return null;hero.target=hound.Health;hero.TargetLocked=true;yield return null;yield return null;
            at=Behind(Picture("TargetPlate").rectTransform,.75f);
            yield return Hold(hound,at,.3f);
            Assert.Less(Plate("TargetPlate"),.42f,"the locked target is behind the target plate and the plate stays solid");
            hound.transform.position=hero.transform.position+Vector3.right*30;Physics.SyncTransforms();
            // A prompt the hero is reading gives way only to the hero: the partner walking behind it mustn't take it away.
            var fixture=new GameObject("Prompt fixture").AddComponent<FixturePrompt>();fixture.prompt="Finish the walkway repair";fixture.range=6;fixture.transform.position=hero.transform.position;
            yield return null;yield return null;
            Put(partner,Behind(Picture("InteractionPlate").rectTransform,1));
            yield return new WaitForSecondsRealtime(.25f);
            Assert.Greater(Plate("InteractionPlate"),.98f,"the prompt the hero is standing at fades because the partner walked behind it");
            Object.Destroy(fixture.gameObject);
        }

        // ------------------------------------------------------------------ item 6: barks
        [UnityTest] public IEnumerator BarksKeepToASideColumnClearOfThePlayAndTheFeed()
        {
            yield return null;
            string longest="Got the key? Good. Tallow's on the far side, and that warp doesn't run back until you're through.";
            typeof(BarkPanel).GetMethod("Show",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(Object.FindFirstObjectByType<BarkPanel>(),new object[]{"Orrin",longest});
            foreach(string id in new[]{"ScrapAlloy","LatticeFilament","RidgeCrystal","HuskCore","ChoirResin"})RpgServices.Inventory.Grant(id,1);
            yield return null;yield return null;yield return null;
            var bark=Text("Bark");var plate=Picture("BarkPlate");Assert.IsTrue(plate.enabled,"fixture: the bark is up");
            var box=Box(plate.rectTransform);var screen=Box((RectTransform)bark.canvas.transform);
            float middleFrom=screen.xMin+screen.width*.3f,middleTo=screen.xMin+screen.width*.7f;
            Assert.IsTrue(box.xMax<=middleFrom||box.xMin>=middleTo,$"a bark runs from {box.xMin:0} to {box.xMax:0} across the middle of the screen ({middleFrom:0} to {middleTo:0}), right where the ship flies");
            // Long lines wrap in their column instead of running across the play.
            bark.ForceMeshUpdate();Assert.GreaterOrEqual(bark.textInfo.lineCount,3,"the column doesn't wrap a long line");
            var need=bark.GetPreferredValues(bark.text,bark.rectTransform.rect.width,0);Assert.LessOrEqual(need.y,bark.rectTransform.rect.height+1,"the longest bark overflows its column");
            StringAssert.Contains("<color=",bark.text,"the speaker's name runs into the line, with nothing to set it apart");
            StringAssert.Contains(DisplayNames.Speaker("Orrin"),bark.text.Split('\n')[0],"the speaker's name isn't at the head of the column");
            StringAssert.Contains(longest,bark.text.Replace("\n"," "),"fixture: the whole line shows");
            Assert.IsFalse(box.Overlaps(Box(Picture("VitalsFrame").rectTransform)),"the bark lands on the vitals");
            var lines=Live("PickupLine");Assert.GreaterOrEqual(lines.Length,5,"fixture: a full feed");
            foreach(var line in lines)Assert.IsFalse(box.Overlaps(Box(line.rectTransform)),"the bark lands on the pickup feed's \""+line.text+"\"");
            // And the column gives way like the other panels, if the party does stray behind it.
            Assert.Greater(Plate("BarkPlate"),.98f,"the bark is faded with nobody behind it");
            var partner=Partner;Hero.Health.InvulnerableUntil=partner.Health.InvulnerableUntil=GameTime.Now+10;
            // A fight: on a calm walk the partner doesn't move it, which QaHudSecondTests pins.
            Spawn("Ridgehound",Hero.transform.position+Vector3.right*6+Vector3.back*3).Passive=false;Put(partner,Behind(plate.rectTransform,1));
            yield return new WaitForSecondsRealtime(.3f);
            Assert.Less(Plate("BarkPlate"),.42f,"the partner is behind a bark and the plate stays solid");
        }

        // ------------------------------------------------------------------ items 7 and 13: what the objective says and points at
        static string Objective()=>Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective").text;
        static string Plain(string words)=>Regex.Replace(words,"<[^>]+>","");
        static readonly Regex Metres=new(@"\d+ m\b");
        [UnityTest] public IEnumerator ABossFightDropsTheWalkingDirections()
        {
            Flag("met.Survivor");yield return Zone("Sorrel_Ridges");yield return Settle();
            var hero=Hero;hero.Health.InvulnerableUntil=GameTime.Now+60;
            StringAssert.Contains("stop the Burrower",Objective(),"fixture: the walk to the drill");
            Assert.IsTrue(Metres.IsMatch(Plain(Objective())),"fixture: the walk gives a distance: "+Objective());
            var rig=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Burrower"),hero.transform.position+Vector3.forward*10);rig.Passive=false;
            yield return Settle();
            string fight=Plain(Objective());
            Assert.IsFalse(Metres.IsMatch(fight),"with the boss bar up the panel still gives a distance to a fixed spot: "+fight);
            StringAssert.DoesNotContain("Follow the ridges",fight,"the panel tells the player to follow the ridges with the Burrower on top of them");
            StringAssert.Contains("Burrower",fight,"the panel doesn't name the fight's goal");
            foreach(var word in new[]{"behind","ahead"})StringAssert.DoesNotContain(word,fight,"the panel gives a direction mid-fight");
            var arrow=Picture("ObjectiveArrow");Assert.IsTrue(arrow!=null&&!arrow.enabled,"the panel still points at the fixed spot with the boss on top of the hero");
            rig.Passive=true;yield return Settle();
            Assert.IsTrue(arrow.enabled,"the arrow doesn't come back with the walk");
            StringAssert.Contains("stop the Burrower",Objective(),"the walk doesn't come back when the bar goes down");
            Object.Destroy(rig.gameObject);
        }
        /// <summary>The launch at Tallow Drift: no encounter to wake, and the spot the readers stood on when the panel said '0 m · behind'.</summary>
        static IEnumerator AtTheLaunch()
        {
            Flag("met.Keeper");Flag("sliceComplete");yield return Zone("TallowDrift");yield return Settle();
            StringAssert.Contains("Launch to inspect",Objective(),"fixture: the launch is the goal");
        }
        static DockingPad Launch=>InteractionPrompt.Active.OfType<DockingPad>().First();
        [UnityTest] public IEnumerator StandingOnTheGoalSaysHere()
        {
            yield return AtTheLaunch();
            var hero=Hero;var at=Launch.transform.position;Put(hero,new Vector3(at.x+1,hero.transform.position.y,at.z-1));yield return Settle();
            var lines=Plain(Objective()).Split('\n').Select(l=>l.Trim()).ToArray();
            CollectionAssert.Contains(lines,"Here","a body standing on the launch is told it's a metre or two off in some direction: "+Objective());
            Assert.IsFalse(Metres.IsMatch(lines[1]),"the panel gives a distance from on top of the spot: "+Objective());
            foreach(var word in new[]{"behind","ahead"})StringAssert.DoesNotContain(word,Objective(),"a direction means nothing from on top of the spot");
            var arrow=Picture("ObjectiveArrow");Assert.IsNotNull(arrow,"the panel has no arrow");Assert.IsFalse(arrow.enabled,"an arrow spins on the spot when there's nowhere to point");
        }
        [UnityTest] public IEnumerator TheBellowsFightSaysWhatToBreak()
        {
            yield return Zone("Hushwell");yield return Settle();
            var hero=Hero;hero.Health.InvulnerableUntil=GameTime.Now+60;
            StringAssert.Contains("Follow the breathing",Objective(),"fixture: the walk down");
            var bellows=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("BellowsBelow"),hero.transform.position+Vector3.forward*12);bellows.Passive=false;
            yield return Settle();
            string fight=Plain(Objective());
            StringAssert.DoesNotContain("Follow the breathing",fight,"the panel sends the player down to the Bellows while it fills the screen");
            StringAssert.Contains("Bellows",fight,"the panel doesn't name the fight");
            Assert.IsFalse(Metres.IsMatch(fight),"the panel gives a distance to a fixed spot in the boss's own chamber: "+fight);
            bellows.Passive=true;Object.Destroy(bellows.gameObject);
        }
        [UnityTest] public IEnumerator TheObjectivePointsAnArrowWhereTheGoalIsOnScreen()
        {
            yield return AtTheLaunch();
            var hero=Hero;var goal=Launch.transform.position;var arrow=Picture("ObjectiveArrow");
            Assert.IsNotNull(arrow,"the objective's direction is still a word, and 'behind' is wrong as often as it's right");
            int read=0;
            foreach(var offset in new[]{new Vector3(0,0,-11),new Vector3(9,0,-7),new Vector3(-9,0,-7),new Vector3(11,0,3),new Vector3(-11,0,3)})
            {
                Put(hero,new Vector3(goal.x+offset.x,hero.transform.position.y,goal.z+offset.z));yield return new WaitForSecondsRealtime(.35f);
                string words=Plain(Objective());
                foreach(var word in new[]{"behind","ahead"," left"," right"})StringAssert.DoesNotContain(word,words,"the panel still gives a direction in words");
                Assert.IsTrue(Metres.IsMatch(words),"the panel lost the distance: "+words);
                Assert.IsTrue(arrow.enabled,"the arrow is off with the goal "+offset.magnitude.ToString("0")+" m away");
                var camera=Camera.main;var at=hero.transform.position;var flat=new Vector3(goal.x,at.y,goal.z);
                var seen=camera.WorldToScreenPoint(flat);if(seen.z<1)continue;
                Vector2 way=(Vector2)seen-(Vector2)camera.WorldToScreenPoint(at);
                Vector2 points=arrow.rectTransform.rotation*Vector3.up;
                Assert.Less(Vector2.Angle(way,points),8,$"from {offset} the goal lies toward {way.normalized} on screen and the arrow points {points.normalized}");
                read++;
            }
            Assert.GreaterOrEqual(read,4,"fixture: the goal was on the camera's side often enough to read");
            // The arrow sits by the distance, in the room left for it, not on the words.
            var text=Object.FindFirstObjectByType<ObjectiveHud>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Objective");
            text.ForceMeshUpdate();var info=text.textInfo;
            var digit=info.characterInfo.Take(info.characterCount).First(c=>c.isVisible&&char.IsDigit(c.character));
            var drawn=Box(arrow.rectTransform);float first=text.transform.TransformPoint(new Vector3(digit.origin,0,0)).x;
            Assert.LessOrEqual(drawn.xMax,first+1,"the arrow lies on the distance's digits");
            float line=text.transform.TransformPoint(new Vector3(0,digit.baseLine,0)).y,em=text.fontSize*text.transform.lossyScale.y;
            Assert.That(drawn.center.y,Is.InRange(line-.1f*em,line+.8f*em),"the arrow doesn't sit on the distance's line");
            // The dim line gets a smaller one, for the same reason.
            State.questSteps["MiraCrystals"]=0;State.materials["RidgeCrystal"]=0;
            yield return Zone("Sorrel_Ridges");yield return Settle();
            hero=Hero;
            StringAssert.Contains("Also:",Objective(),"fixture: Mira's crystals are in this zone");
            var also=Picture("ObjectiveAlsoArrow");Assert.IsNotNull(also,"the Also line gives metres and no way to tell where");Assert.IsTrue(also.enabled,"the Also line's arrow is off");
            var nearest=InteractionPrompt.Active.OfType<MineNode>().Where(n=>n.material=="RidgeCrystal").OrderBy(n=>(n.transform.position-hero.transform.position).sqrMagnitude).First();
            var view=Camera.main;var gap=nearest.transform.position-hero.transform.position;gap.y=0;var step=hero.transform.position+gap.normalized*2;
            Vector2 toward=(Vector2)view.WorldToScreenPoint(step)-(Vector2)view.WorldToScreenPoint(hero.transform.position);Vector2 shows=also.rectTransform.rotation*Vector3.up;
            Assert.Less(Vector2.Angle(toward,shows),10,"the Also line's arrow doesn't point at the crystal it names");
        }

        // ------------------------------------------------------------------ item 8: the Cantor's bar and plate
        [UnityTest] public IEnumerator TheCantorsBarShowsTheWholeFightAndItsPlateNamesTheLock()
        {
            yield return Zone("Arena_Flight");
            var hero=Hero;hero.GetComponent<FlightMotor>().Halt();hero.Health.InvulnerableUntil=GameTime.Now+60;
            var cantor=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),hero.transform.position+new Vector3(20,0,0));cantor.Passive=false;cantor.enabled=true;
            yield return null;yield return null;yield return null;cantor.Passive=false;yield return null;yield return null;
            var fill=Picture("BossIntegrity");var back=Picture("BossIntegrityBack");Assert.IsTrue(back.isActiveAndEnabled,"fixture: the bar is up");
            var collar=cantor.GetComponent<CantorCollar>();var head=cantor.Health;float Bar()=>fill.rectTransform.rect.width/back.rectTransform.rect.width;
            float Whole()=>head.maximum+collar.Links.Sum(l=>l.maximum);float Left()=>head.integrity+collar.Links.Sum(l=>l.integrity);
            Assert.AreEqual(1,Bar(),.01f,"the fight starts with a part of its bar already gone");
            // A link nearly cut: the plate's 66/5100 and the bar have to agree.
            collar.Links[0].integrity=66;yield return null;yield return null;
            Assert.AreEqual(Left()/Whole(),Bar(),.015f,"a link is nearly gone and the bar hasn't moved: 66/5100 on the plate reads as nearly dead over a full bar");
            // Notches where the fight changes: each link's cut, the last where the head opens, then the head's phases.
            var notches=fill.canvas.GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("BossNotch")).Select(i=>i.rectTransform.anchoredPosition.x/back.rectTransform.rect.width).OrderByDescending(x=>x).ToArray();
            float share=head.maximum/Whole();var thresholds=cantor.GetComponent<BossController>().Thresholds;
            Assert.AreEqual(5,notches.Length,"the bar doesn't mark each link coming off as well as the head's two phases");
            Assert.AreEqual(1-collar.Links[0].maximum/Whole(),notches[0],.01f,"no notch where the first link goes");
            Assert.AreEqual(1-(collar.Links[0].maximum+collar.Links[1].maximum)/Whole(),notches[1],.01f,"no notch where the second link goes");
            Assert.AreEqual(share,notches[2],.01f,"no notch where the last link goes and the head opens");
            Assert.AreEqual(share*thresholds[0],notches[3],.01f,"the head's first phase isn't marked where it falls on the whole fight's bar");
            Assert.AreEqual(share*thresholds[1],notches[4],.01f,"the head's second phase isn't marked where it falls on the whole fight's bar");
            // The links gone, the bar stands at the head's share.
            foreach(var link in collar.Links)link.Receive(new DamagePacket{source=hero.Health,amount=1e8f,type=DamageType.Pulse});
            yield return null;yield return null;
            Assert.IsFalse(collar.Links.Any(l=>l.Alive),"fixture: every link cut");
            Assert.AreEqual(share,Bar(),.015f,"with the collar off the bar doesn't show the head's share of the fight");
            // The objective says to release the lock, and the plate has to say the same.
            hero.target=head;hero.TargetLocked=true;head.integrity=head.maximum*.5f;yield return null;yield return null;
            string plate=Text("Target").text;
            StringAssert.Contains("COLLAR LOCK",plate,"the objective says to release the lock and the plate only says LB LOCK");
            StringAssert.Contains($"{head.integrity:0}/{head.maximum:0}",plate,"the plate gives no progress on the lock");
            Assert.AreEqual(share*.5f,Bar(),.015f,"the bar doesn't follow the head");
            cantor.Passive=true;Object.Destroy(cantor.gameObject);yield return null;
            // Any other boss keeps its short plate and its own bar: the bar has the name and the gauge.
            var rig=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),hero.transform.position+new Vector3(14,0,0));rig.Passive=false;
            yield return null;yield return null;yield return null;
            hero.target=rig.Health;hero.TargetLocked=true;rig.Health.integrity=rig.Health.maximum*.5f;yield return null;yield return null;
            StringAssert.DoesNotContain("COLLAR",Text("Target").text,"another boss's plate talks about a collar");
            Assert.AreEqual(.5f,Bar(),.02f,"another boss's bar doesn't show its integrity");
            rig.Passive=true;Object.Destroy(rig.gameObject);
        }

        // ------------------------------------------------------------------ item 9: portraits
        /// <summary>The most any cell of the sheets stops short of its own bottom, measured on the art (Taren_Natural). A run with graphics reads each cell itself.</summary>
        const int WorstGapInTheSheets=57;
        sealed class Sheet{public Texture texture;public Color32[] pixels;}
        static int BottomGap(Sprite sprite,Sheet sheet)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return WorstGapInTheSheets;
            var texture=sprite.texture;
            if(sheet.texture!=texture)
            {
                var target=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);var prior=RenderTexture.active;
                Graphics.Blit(texture,target);RenderTexture.active=target;
                var read=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);read.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);read.Apply(false);
                RenderTexture.active=prior;RenderTexture.ReleaseTemporary(target);sheet.pixels=read.GetPixels32();Object.Destroy(read);sheet.texture=texture;
            }
            var r=sprite.rect;int width=texture.width;
            for(int row=0;row<(int)r.height;row++)for(int column=0;column<(int)r.width;column++)
                if(sheet.pixels[((int)r.y+row)*width+(int)r.x+column].a>20)return row;
            return (int)r.height;
        }
        [UnityTest] public IEnumerator EveryBustIsSeatedOnTheFramesInnerEdge()
        {
            yield return null;
            var panel=Object.FindFirstObjectByType<DialoguePanel>();var sets=Resources.LoadAll<PortraitSet>("Portraits");
            Assert.GreaterOrEqual(sets.Length,15,"fixture: every speaker's portraits");
            panel.Show("Sela",PortraitEmotion.Neutral,sets[0].entries[0].sprite,"Fixture line.");panel.SetVisibleCharacters(int.MaxValue);yield return null;
            var face=Picture("Portrait");var frame=Picture("DialoguePanel");var seat=face.GetComponentInParent<RectMask2D>();
            Assert.IsNotNull(seat,"nothing clips the bust: its straight cut floats in the panel");
            var c=new Vector3[4];frame.rectTransform.GetWorldCorners(c);float scale=frame.rectTransform.lossyScale.y,inner=c[0].y+BottomBevel(frame)*scale;
            var seatBox=Box(seat.rectTransform);
            Assert.AreEqual(inner,seatBox.yMin,3*scale,"the bust is clipped away from the frame's inner edge: a line floats above or below the bevel");
            Assert.LessOrEqual(seatBox.xMax,Box(Text("Body").rectTransform).xMin+1,"the bust runs under the words");
            var box=Box(face.rectTransform);
            var sheet=new Sheet();var failures=new List<string>();int read=0;
            foreach(var set in sets)foreach(var entry in set.entries)
            {
                if(entry.sprite==null)continue;read++;
                // Square cells drawn with their aspect kept: the cell's bottom is the box's, or the box's middle less half its width.
                float size=Mathf.Min(box.width,box.height),bottom=box.center.y-size*.5f,cut=bottom+BottomGap(entry.sprite,sheet)*size/entry.sprite.rect.height;
                if(cut>seatBox.yMin+.5f*scale)failures.Add($"{set.name} {entry.emotion}: the picture stops {(cut-seatBox.yMin)/scale:0} above the frame's inner edge");
            }
            Assert.Greater(read,200,"fixture: all the expressions");
            Assert.IsEmpty(failures,string.Join("\n",failures.Take(8)));
            panel.Hide();
        }
        // ------------------------------------------------------------------ the lanternwick report: choices stand aside
        [UnityTest] public IEnumerator ChoicesStandToTheSideOfThePeopleTalking()
        {
            yield return null;
            var panel=Object.FindFirstObjectByType<DialoguePanel>();panel.Show("Hal",PortraitEmotion.Neutral,null,"Repair and save, or just visiting?");panel.SetVisibleCharacters(int.MaxValue);yield return null;
            panel.Options(new List<string>{"Repair and save (8 scrip per Sync level).","Just visiting."},_=>{});yield return null;
            var screen=Box((RectTransform)Picture("DialoguePanel").canvas.transform);var rows=Buttons().Where(b=>b.name.StartsWith("Option_")).ToArray();
            Assert.AreEqual(2,rows.Length,"fixture: two choices are up");
            foreach(var row in rows)
            {
                var box=Box((RectTransform)row.transform);
                Assert.Greater(box.xMin,screen.center.x+screen.width*.08f,$"the choice \"{Words(row)}\" lies across the middle of the screen, over the people talking");
                Assert.LessOrEqual(box.xMax,screen.xMax,$"the choice \"{Words(row)}\" runs off the screen");
                var words=row.GetComponentInChildren<TMP_Text>();Assert.LessOrEqual(words.GetPreferredValues(words.text,float.PositiveInfinity,float.PositiveInfinity).x,words.rectTransform.rect.width+1,$"the choice \"{Words(row)}\" no longer fits its bar");
            }
            panel.ClearOptions();panel.Hide();
        }

        // ------------------------------------------------------------------ item 10: the name marks
        [UnityTest] public IEnumerator NameLabelsFaceTheCameraReadFromPlayDistanceAndSitOverTheHead()
        {
            State.questSteps["LanternRoad"]=3;yield return Zone("Lanternwick");yield return Settle();
            var people=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None);var labels=new List<(Npc npc,TextMeshPro label)>();
            foreach(var npc in people)
            {
                var label=npc.GetComponentsInChildren<TextMeshPro>().FirstOrDefault(t=>t.GetComponent<Renderer>().enabled);if(label==null)continue;
                labels.Add((npc,label));npc.transform.rotation=Quaternion.Euler(0,73,0);
            }
            Assert.GreaterOrEqual(labels.Count,3,"fixture: the town has labelled people");
            yield return null;yield return null;
            var camera=Camera.main;var view=ZoneController.Current.definition.cameraProfile;float seen=2*view.distance*Mathf.Tan(view.fov*.5f*Mathf.Deg2Rad),pixel=camera.pixelHeight/1080f;
            var failures=new List<string>();
            foreach(var (npc,label) in labels)
            {
                if(Vector3.Dot(label.transform.right,camera.transform.right)<.995f||Vector3.Dot(label.transform.up,camera.transform.up)<.995f)failures.Add($"{label.text} turns with the person who wears it, and reads on a slant");
                float tall=label.fontSize*.1f*label.transform.lossyScale.y/seen*1080;
                if(tall<24)failures.Add($"{label.text} stands {tall:0} px tall on a 1080p screen, under the type floor: it can't be read at play distance");
                float top=float.MinValue;
                foreach(var body in npc.GetComponentsInChildren<Renderer>())if(!body.transform.IsChildOf(label.transform)&&body is MeshRenderer or SkinnedMeshRenderer)top=Mathf.Max(top,body.bounds.max.y);
                if(top==float.MinValue)continue;
                var ink=Ink(label);var head=camera.WorldToScreenPoint(new Vector3(npc.transform.position.x,top,npc.transform.position.z));
                var foot=camera.WorldToScreenPoint(label.transform.TransformPoint(new Vector3(0,ink.yMin,0)));
                float gap=(foot.y-head.y)/pixel;
                if(gap>48)failures.Add($"{label.text} floats {gap:0} px over the head: it reads as a sign on the wall behind them");
                if(gap<-12)failures.Add($"{label.text} sits {-gap:0} px down into the body");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
            // Someone with news: a heavy mark and not a tick before the name.
            var pell=labels.First(p=>p.npc.speaker=="Pell").label;Flag("met.Pell");yield return Settle();
            pell.ForceMeshUpdate();var info=pell.textInfo;var mark=info.characterInfo.Take(info.characterCount).First(c=>c.isVisible);
            Assert.AreEqual('!',mark.character,"Pell has news and no mark before her name: "+pell.text);
            Assert.AreEqual(FontStyles.Bold,mark.style&FontStyles.Bold,"the mark is thin Cinzel, and reads as a stray tick");
            Assert.GreaterOrEqual(mark.pointSize,pell.fontSize*1.4f,"the mark is no bigger than the letters beside it");
        }

        // ------------------------------------------------------------------ item 11: Fabricate
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
        [UnityTest] public IEnumerator TheSelectionBarStandsClearOfARowsIcon()
        {
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open(PauseMenu.FabricatePage,true);yield return null;yield return null;
            var row=EventSystem.current.currentSelectedGameObject.transform;var bar=row.Find("SelectionBar");var icon=row.Find("Item");
            Assert.IsNotNull(bar,"fixture: the selected row shows its bar");Assert.IsNotNull(icon,"fixture: the selected row has an icon");
            Assert.IsFalse(Box((RectTransform)bar).Overlaps(Box((RectTransform)icon)),"the selection bar is drawn on the icon's left side: a selected blade and the bar merge into an L");
            menu.Close();
        }
        [UnityTest] public IEnumerator FabricateDimsWhatCantBeMadeAndThePagerSaysWhatItDoes()
        {
            State.materials["ScrapAlloy"]=20;State.materials["RidgeCrystal"]=0;
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open(PauseMenu.FabricatePage,true);yield return null;
            Button can=null,short1=null;
            yield return FindRow("Scrap Edge",b=>can=b);Color canColor=can.GetComponentInChildren<TMP_Text>().color;
            yield return FindRow("Refined Edge",b=>short1=b);var shortText=short1.GetComponentInChildren<TMP_Text>();
            EventSystem.current.SetSelectedGameObject(Row("Close").gameObject);yield return null;
            Assert.Greater(ColorGap(canColor,shortText.color),.3f,$"\"{Plain(shortText.text)}\" can't be made and looks the same as a row that can: the player has to read every count");
            StringAssert.Contains("Ridge Crystal 0/",shortText.text,"the row no longer says what it's short of");
            StringAssert.Contains("<color=",shortText.text,"nothing marks which material is short");
            // Without a bench no part can be made, whatever's in the bag; a consumable still can.
            menu.Close();yield return null;State.materials["LatticeFilament"]=20;
            menu.Open(PauseMenu.FabricatePage);yield return null;
            Button part=null,gel=null;yield return FindRow("Scrap Edge",b=>part=b);Color partColor=part.GetComponentInChildren<TMP_Text>().color;
            yield return FindRow("Repair Gel",b=>gel=b);EventSystem.current.SetSelectedGameObject(Row("Close").gameObject);yield return null;
            Assert.Greater(ColorGap(partColor,gel.GetComponentInChildren<TMP_Text>().color),.3f,"away from a bench a part looks as makeable as a Repair Gel");
            // The pager: in line with the rows above it, and honest on its last page.
            var pager=Row("Pager");Assert.IsNotNull(pager,"fixture: the page turns");var seen=new List<string>();
            for(int i=0;i<4;i++)
            {
                pager=Row("Pager");string words=Words(pager);if(seen.Contains(words))break;seen.Add(words);
                var above=Buttons().First(b=>b.name.StartsWith("Row_"));
                Assert.AreEqual(above.GetComponentInChildren<TMP_Text>().margin.x,pager.GetComponentInChildren<TMP_Text>().margin.x,.01f,"'"+words+"' starts hard against the row's left chevron, out of line with the rows above it");
                pager.onClick.Invoke();yield return null;
            }
            Assert.GreaterOrEqual(seen.Count,2,"fixture: more than one page");
            Assert.IsTrue(seen.Take(seen.Count-1).All(w=>w.StartsWith("Next page")),"fixture: the pages before the last say Next page: "+string.Join(" | ",seen));
            StringAssert.DoesNotContain("Next page",seen.Last(),"the last page still says Next page, and pressing it goes back to the first");
            StringAssert.Contains("first page",seen.Last(),"the last page's row doesn't say where it goes");
            menu.Close();yield return null;
            // The shop's own pager says the same.
            var shop=Object.FindFirstObjectByType<ShopUi>();shop.Open(GameCatalog.Find<ShopInventory>("Lanternwick"));yield return null;
            var shopPager=Row("Pager");Assert.IsNotNull(shopPager,"fixture: Corra's stock pages");
            string firstWords=Words(shopPager);StringAssert.StartsWith("Next page",firstWords,"fixture: the first page");
            for(int i=0;i<4&&Words(Row("Pager")).StartsWith("Next page");i++){Row("Pager").onClick.Invoke();yield return null;}
            StringAssert.Contains("first page",Words(Row("Pager")),"the shop's last page still says Next page");
            shop.Close();
        }

        // ------------------------------------------------------------------ item 14: the boss's card
        [UnityTest] public IEnumerator TheBossCardEntersAboveTheHeroAndClearOfThePlates()
        {
            var hero=Hero;hero.Health.InvulnerableUntil=GameTime.Now+30;
            var boss=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("HessaKorl"),hero.transform.position+Vector3.forward*12);boss.Passive=false;
            yield return new WaitForSecondsRealtime(.55f);
            var card=Text("BossTitleCard");var line=Text("BossTitleEpithet");Assert.IsTrue(card.isActiveAndEnabled&&line.isActiveAndEnabled&&card.alpha>.5f,"fixture: the card is up");
            StringAssert.Contains("HESSA KORL",card.text,"fixture: her name");
            Assert.AreEqual("Cinzel SDF",card.font.name,"the card isn't in the game's display face: it reads as a label, not an entrance");
            var head=Camera.main.WorldToScreenPoint(hero.transform.position+Vector3.up*2.1f);
            Assert.Greater(Box(line.rectTransform).yMin,head.y,"the card lies across the hero, over whatever is marked under them");
            foreach(string plate in new[]{"VitalsFrame","ObjectiveFrame"})
            {
                Assert.IsFalse(Box(card.rectTransform).Overlaps(Box(Picture(plate).rectTransform)),"the card stacks on "+plate);
                Assert.IsFalse(Box(line.rectTransform).Overlaps(Box(Picture(plate).rectTransform)),"the card's epithet stacks on "+plate);
            }
            Assert.Less(Box(card.rectTransform).yMax,Box(Text("Target").rectTransform).yMin,"the card stacks on the target plate's place");
            Assert.IsTrue(string.IsNullOrEmpty(Text("BossEpithet").text),"the same epithet shows twice, on the card and on the bar under it");
            // The words fit it, for every boss.
            foreach(string id in new[]{"Burrower","BellowsBelow","Cantor","HessaKorl"})
            {
                var other=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),hero.transform.position+Vector3.forward*200);other.Passive=true;yield return null;yield return null;
                var controller=other.GetComponent<BossController>();
                var name=card.GetPreferredValues(controller.DisplayName,card.rectTransform.rect.width,0);var says=line.GetPreferredValues(controller.Epithet,line.rectTransform.rect.width,0);
                Assert.LessOrEqual(name.x,card.rectTransform.rect.width+1,id+"'s name runs off its card");Assert.LessOrEqual(says.y,line.rectTransform.rect.height+1,id+"'s epithet doesn't fit one line of its card");
                Object.Destroy(other.gameObject);
            }
            // Its moment over, the bar carries the epithet.
            yield return new WaitForSecondsRealtime(3.2f);
            Assert.Less(card.alpha,.01f,"the card never leaves");
            Assert.IsNull(Picture("BossTitleBand"),"the card has a dark band over the fight, and it hid a boss standing under it");
            Assert.IsFalse(Picture("BossTitleRule").isActiveAndEnabled,"the card's rule stays over the fight after its words have gone");
            StringAssert.Contains("scavenger",Text("BossEpithet").text,"the epithet never reaches the bar");
            boss.Passive=true;Object.Destroy(boss.gameObject);
        }

        // ------------------------------------------------------------------ item 15: the end of chapter 2
        [UnityTest] public IEnumerator TheChapterEndsOnACentredCardInTheGamesVoice()
        {
            var toast=Object.FindFirstObjectByType<QuestToast>();yield return Settle();
            Flag("chapter2.homecoming");
            float deadline=Time.unscaledTime+3;while(!toast.Heading.Contains("CHAPTER 2")&&Time.unscaledTime<deadline)yield return null;
            StringAssert.Contains("CHAPTER 2",toast.Heading,"fixture: the chapter ends");yield return null;
            var card=Text("ChapterCardHeading");Assert.IsNotNull(card,"the chapter ends on a small card in the corner under the objective");
            Assert.IsTrue(card.isActiveAndEnabled,"the chapter's card isn't up");
            Assert.AreEqual("Cinzel SDF",card.font.name,"the card isn't in the boss card's display face");
            var words=Text("ChapterCardBody");var screen=Box((RectTransform)card.canvas.transform);
            Assert.AreEqual(screen.center.x,Box(card.rectTransform).center.x,screen.width*.02f,"the card isn't centred");
            Assert.AreEqual(screen.center.y,(Box(card.rectTransform).center.y+Box(words.rectTransform).center.y)*.5f,screen.height*.12f,"the card isn't in the middle of the screen");
            Assert.IsFalse(Picture("QuestToastFrame").gameObject.activeInHierarchy,"the corner card shows the same words beside it");
            StringAssert.Contains("road runs on to Xylos",words.text,"nothing says plainly that the road goes on");
            foreach(var developer in new[]{"for today","stops here","build"})StringAssert.DoesNotContain(developer,words.text,"the card sounds like a developer's note");
            foreach(var text in new[]{card,words,Text("ChapterCardTitle")})
            {
                Assert.IsNotNull(text,"fixture: the card's pieces");var box=text.rectTransform.rect;var need=text.GetPreferredValues(text.text,box.width,0);
                Assert.LessOrEqual(need.y,box.height+1,$"{text.name} overflows its box");Assert.GreaterOrEqual(text.fontSize,24,$"{text.name} is under the type floor");
            }
            Assert.AreEqual(toast.Body,words.text,"fixture: the toast still reports what it shows");
        }
    }
}
