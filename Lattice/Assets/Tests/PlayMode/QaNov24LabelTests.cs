using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
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
    /// <summary>D388, from the November 23 night capture (nov23-whole): a warp's name drawn a second time under the skill row (s53 023), the Cutter's last number read as eight digits
    /// (s61 071), the partner's numbers in the resisted grey beside the lead's bright ones (s30 034, s29 030), and names whose tags were far larger than the rest (PELL s13 079, MIRA
    /// s69 038). Each check is shown to see the old state first, inside the case.</summary>
    [Timeout(600000)]
    public sealed class QaNov24LabelTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members[1-PartyController.Current.index];
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone,spawn);deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var fight in All<EncounterVolume>())foreach(var box in fight.GetComponents<Collider>())box.enabled=false;
            if(PartyController.Current!=null)foreach(var actor in PartyController.Current.members)
            {
                actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;
                actor.Health.maximum=actor.Health.integrity=90000;
            }
            yield return new WaitForSecondsRealtime(.6f);
        }
        static void Put(CombatActor hero,Vector3 at)
        {
            var cc=hero.GetComponent<CharacterController>();if(cc!=null)cc.enabled=false;hero.transform.position=at;if(cc!=null)cc.enabled=true;Physics.SyncTransforms();
            if(hero.TryGetComponent<FlightMotor>(out var motor))motor.Halt();
        }
        static string Words(TextMeshPro t)=>System.Text.RegularExpressions.Regex.Replace(t.text,"<[^>]*>","");
        static bool InView(Camera camera,Component thing){var v=camera.WorldToViewportPoint(thing.transform.position);return v.z>0&&v.x>0&&v.x<1&&v.y>0&&v.y<1;}

        // ---------------------------------------------------------------- 1. a warp's name, once, and as a sign
        /// <summary>Every world word that repeats a sign's words, on a warp: whatever draws it is a sign SignStyle dressed (a SignFade, so a panel, the skill row or the screen's
        /// edge takes it away). Counted whether its object is shown yet or not: a FlagGate shows it later, and the twin under the skill row was one.</summary>
        static List<string> Undressed(string zone)
        {
            var faults=new List<string>();
            var signs=new HashSet<string>(All<TextMeshPro>().Where(t=>t.name.StartsWith("Sign_")&&!string.IsNullOrEmpty(t.text)).Select(t=>t.text));
            foreach(var text in All<TextMeshPro>())
            {
                if(string.IsNullOrEmpty(text.text)||!signs.Contains(text.text)||!text.text.StartsWith("Warp")||text.GetComponentInParent<Canvas>(true)!=null)continue;
                var view=text.GetComponent<Renderer>();bool drawn=text.enabled&&view!=null&&view.enabled;
                if(drawn&&!text.TryGetComponent<SignFade>(out _))
                    faults.Add($"{zone}: \"{text.text}\" is drawn undressed by '{text.name}' (font {text.font?.name}, colour {text.color}): nothing takes it from under the skill row or the panels");
            }
            return faults;
        }
        /// <summary>s53/023: at the Picket's warp a small pale-blue "Warp — Nacre Grave" lay under the skill row, letters mixed into Cleave and Ember, while the same name stood on its
        /// plate over the gate. It is the second label five builders hang on a warp, 7 m short of it, renamed "…warp sign" so SignStyle never dressed it. The Hull carries six.</summary>
        [UnityTest] public IEnumerator AWarpsNameIsDrawnOnlyAsItsSign()
        {
            var faults=new List<string>();var seen=new List<string>();
            foreach(var zone in new[]{"PicketOfTeeth","ManyHandsHull"})
            {
                yield return Cleanup();yield return Boot();
                GameServices.Current.Flags.SetBool("home.turnedForNacre",true);
                yield return Zone(zone);
                var twins=All<TextMeshPro>().Where(t=>!t.name.StartsWith("Sign_")&&t.name.EndsWith("warp sign")).ToList();
                seen.Add($"{zone}: {twins.Count} second labels ({string.Join(", ",twins.Select(t=>$"'{t.name}' \"{t.text}\" active={t.gameObject.activeInHierarchy}"))})");
                if(twins.Count==0){faults.Add($"fixture: {zone} holds no builder's second warp label: the case shows nothing");continue;}
                // The check on the twins as the builders left them: drawn, undressed.
                var kept=twins.Select(t=>(text:t,on:t.enabled,drawn:t.GetComponent<Renderer>().enabled)).ToList();
                foreach(var t in twins){t.enabled=true;t.GetComponent<Renderer>().enabled=true;}
                var old=Undressed(zone);
                foreach(var k in kept){k.text.enabled=k.on;k.text.GetComponent<Renderer>().enabled=k.drawn;}
                if(old.Count<twins.Count)faults.Add($"fixture: {zone}: the check finds {old.Count} of the {twins.Count} twins drawn as they were");
                faults.AddRange(Undressed(zone));
                yield return new WaitForSecondsRealtime(.3f);
                // Shown by its gate now the lane is open, and still drawing nothing.
                foreach(var t in twins.Where(t=>t.text.Contains("Nacre")))if(!t.gameObject.activeInHierarchy)faults.Add($"fixture: {zone}: the Nacre warp's twin isn't shown with the lane open");
                faults.AddRange(Undressed(zone));
            }
            foreach(var line in seen){TestContext.WriteLine("WARP_TWIN "+line);Debug.Log("WARP_TWIN "+line);}
            Assert.IsEmpty(faults,string.Join("\n",faults.Distinct()));
        }

        // ---------------------------------------------------------------- 2. numbers
        static TMP_Text Newest()=>Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<TMP_Text>(true).Last(t=>t.name=="Damage");
        static float Value(TMP_Text t)=>float.Parse(new string(t.text.Where(char.IsDigit).ToArray()),System.Globalization.CultureInfo.InvariantCulture);
        /// <summary>What reads as a number half faded: a grey (the resisted grey is .62, .66, .7), or anything short of full strength.</summary>
        static bool Faded(Color c)=>c.a<.99f||Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))<.12f&&Mathf.Max(c.r,Mathf.Max(c.g,c.b))<.8f;
        static Health Body(Vector3 at,float bar)
        {
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MusterScav"),at);enemy.Passive=true;
            var health=enemy.GetComponent<Health>();health.maximum=health.integrity=bar;health.breakThreshold=1e9f;return health;
        }
        /// <summary>s61/071: the Cutter's last pod is struck for a hundred times its bar (BossController.DebrisCutter), and the number read the blow whole, '56714930!', one number
        /// of eight digits on the dying hull. The number is what the bar lost.</summary>
        [UnityTest] public IEnumerator AKillingBlowShowsWhatTheBarHad()
        {
            yield return Zone("Arena_Ground");
            var body=Body(Lead.transform.position+Vector3.forward*6,1000);yield return null;yield return null;
            body.integrity=500;float reported=-1;
            System.Action<Health,DamagePacket,float,HitKind,bool,bool> heard=(h,p,d,k,b,killed)=>{if(h==body)reported=d;};
            Health.Impacted+=heard;
            try{body.Receive(new DamagePacket{source=Lead.Health,amount=body.maximum*100,type=body.weakness,weight=HitWeight.Heavy});}
            finally{Health.Impacted-=heard;}
            var number=Newest();string line=$"KILL_NUMBER reported={reported:0} shown=\"{number.text}\" bar=500";TestContext.WriteLine(line);Debug.Log(line);
            Assert.IsFalse(body.Alive,"fixture: the blow beats the body");
            Assert.Greater(reported,5000,"fixture: the blow is reported whole, as the Cutter's was: the old number");
            Assert.LessOrEqual(Value(number),500.5f,line+": the killing blow's number says more than the body had");
            Assert.GreaterOrEqual(Value(number),499.5f,line+": the killing blow's number says less than it took");
        }
        /// <summary>s30/034, s29/030, s41/049: the partner's hits on armour came out in the resisted grey ('3697' beside '25111'), a number half faded beside the lead's. The partner's
        /// number has a colour of its own, at full strength with the same dark edge, whatever its hit; the lead's keeps the kinds' colours.</summary>
        [UnityTest] public IEnumerator ThePartnersNumberIsItsOwnColourAtFullStrength()
        {
            yield return Zone("Arena_Ground");
            var at=Lead.transform.position;
            var first=Body(at+Vector3.forward*6,1e6f);var second=Body(at+Vector3.forward*6+Vector3.right*5,1e6f);yield return null;yield return null;
            var plain=System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=first.weakness&&t!=first.resistance);
            first.Receive(new DamagePacket{source=Lead.Health,amount=100,type=plain});var leads=Newest();var leadsColour=leads.color;var edge=leads.fontSharedMaterial;
            first.Receive(new DamagePacket{source=Partner.Health,amount=100,type=first.resistance});var armour=Newest();var armourColour=armour.color;
            second.Receive(new DamagePacket{source=Partner.Health,amount=100,type=plain});var clean=Newest();
            second.Receive(new DamagePacket{source=Lead.Health,amount=100,type=second.resistance});var leadsArmour=Newest();
            string line=$"PARTNER_NUMBER lead {leadsColour} partner on armour {armourColour} \"{armour.text}\" partner clean {clean.color} lead on armour {leadsArmour.color}";
            TestContext.WriteLine(line);Debug.Log(line);
            Assert.AreNotSame(leads,armour,"fixture: the partner's hit has a number of its own beside the lead's");
            Assert.IsTrue(Faded(new Color(.62f,.66f,.7f)),"fixture: the check doesn't see the resisted grey the partner's numbers were drawn in as faded");
            Assert.IsTrue(Faded(leadsArmour.color),"fixture: the lead's hit on armour keeps the resisted grey");
            Assert.IsFalse(Faded(armourColour),line+": the partner's hit on armour is drawn grey, a number half faded beside the lead's");
            Assert.IsFalse(Faded(clean.color),line+": the partner's clean hit is drawn faded");
            Assert.AreEqual(armourColour,clean.color,line+": the partner's numbers change colour with the hit, so they aren't the partner's own");
            Assert.AreNotEqual(leadsColour,armourColour,line+": the partner's number is the lead's colour");
            Assert.AreEqual(edge,armour.fontSharedMaterial,"the partner's number hasn't the lead's dark edge");
        }

        // ---------------------------------------------------------------- 3. a name's tag
        const int W=1920,H=1080;
        static string shot="";
        static int FreeLayer(Camera camera){for(int l=31;l>=8;l--)if(string.IsNullOrEmpty(LayerMask.LayerToName(l))&&(camera.cullingMask&(1<<l))==0)return l;return 31;}
        /// <summary>How tall the name's tag stands on a 1080p frame: the rows the name changes, drawn with it and without it, the camera's post-processing off so its bloom doesn't
        /// spread the change.</summary>
        static float Tag(Camera camera,TextMeshPro name,int layer)
        {
            var target=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);target.Create();var image=new Texture2D(W,H,TextureFormat.RGB24,false);
            var was=camera.targetTexture;var mask=camera.cullingMask;var clear=camera.clearFlags;var back=camera.backgroundColor;var before=RenderTexture.active;int own=name.gameObject.layer;
            var data=System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            var urp=data!=null?camera.GetComponent(data):null;var post=data?.GetProperty("renderPostProcessing");var posted=urp!=null&&post!=null?post.GetValue(urp):null;
            // Drawn in the frame as it stands, with the name and without it: alone on a layer of its own the letters were drawn and the tag's own pass wasn't (22 px, the letters' halo).
            var view=name.GetComponent<Renderer>();
            try
            {
                if(posted!=null)post.SetValue(urp,false);
                name.ForceMeshUpdate();camera.targetTexture=target;
                Color32[] Shoot(){camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();return image.GetPixels32();}
                var with=Shoot();view.enabled=false;var without=Shoot();view.enabled=true;
                int low=H,high=-1,changed=0;
                for(int y=0;y<H;y++)for(int x=0;x<W;x+=2)
                {
                    var p=with[y*W+x];var q=without[y*W+x];
                    if(Mathf.Max(Mathf.Abs(p.r-q.r),Mathf.Max(Mathf.Abs(p.g-q.g),Mathf.Abs(p.b-q.b)))>12){changed++;low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
                }
                shot=$"{changed} pixels changed, post {(posted!=null?"off":"unknown")}";
                return high<low?0:high-low+1;
            }
            finally
            {
                if(posted!=null)post.SetValue(urp,posted);view.enabled=true;
                name.gameObject.layer=own;camera.cullingMask=mask;camera.clearFlags=clear;camera.backgroundColor=back;camera.targetTexture=was;RenderTexture.active=before;
                target.Release();Object.Destroy(target);Object.Destroy(image);
            }
        }
        /// <summary>Where a ray through this height of the screen meets the level a name hangs at.</summary>
        static Vector3 OnScreenAt(Camera camera,float viewportY,float level)
        {
            var ray=camera.ViewportPointToRay(new Vector3(.5f,viewportY,0));return ray.GetPoint((level-ray.origin.y)/ray.direction.y);
        }
        const string OldMark="<size=150%><b>!</b></size>  ";
        /// <summary>s69/038, 075: MIRA's tag at the foot of the screen was a dark block of 180 by 110 pixels, drawn as perspective had it, about 1.3 times a name in the middle; and
        /// s13/079: PELL's, marked, was 190 by 125, grown to 32 pixels for its mark with the mark half as tall again. Every tag is the small tag: the name's made size, marked or not.</summary>
        [UnityTest] public IEnumerator ANamesTagIsTheSmallTagAtTheFootOfTheScreenAndMarked()
        {
            yield return Zone("Hub_Decks");
            Put(Lead,new Vector3(-.03f,.08f,1.607f));Put(Partner,new Vector3(1.846f,.08f,-.491f));yield return new WaitForSecondsRealtime(2f);
            var camera=Camera.main;var view=ZoneController.Current.definition.cameraProfile;
            var name=All<TextMeshPro>().Where(t=>t.isActiveAndEnabled&&t.text.Contains("<mark")&&InView(camera,t))
                .OrderByDescending(t=>Words(t).IndexOf("Mira",System.StringComparison.OrdinalIgnoreCase)>=0).FirstOrDefault();
            Assert.IsNotNull(name,"fixture: a person's backed name on the screen");
            var fade=name.GetComponent<SignFade>();bool fading=fade!=null&&fade.enabled;if(fade!=null)fade.enabled=false;
            var home=name.transform.position;string shows=name.text,bare=shows.Substring(shows.LastIndexOf("em>")+3);float size=name.fontSize;var face=name.fontSharedMaterial;float alpha=name.alpha;
            int layer=FreeLayer(camera);float made,foot,oldFoot,marked,oldMarked;
            try
            {
                name.alpha=1;
                // Made: on the camera's middle line, at the camera's own distance.
                name.transform.position=camera.transform.position+camera.transform.forward*view.distance;made=Tag(camera,name,layer);
                // At the foot of the screen, at the height it hangs, as MIRA stood: drawn as perspective has it (the old band), then held.
                name.transform.position=OnScreenAt(camera,.1f,home.y);
                face.SetFloat("_Band",0);oldFoot=Tag(camera,name,layer);face.SetFloat("_Band",SignStyle.NameBand);foot=Tag(camera,name,layer);
                // Marked, back in the middle: the mark as it is, then as it was (32 pixels, the mark half as tall again).
                name.transform.position=camera.transform.position+camera.transform.forward*view.distance;
                name.text=PeopleMarks.Marked(name,bare,shows,true);marked=Tag(camera,name,layer);
                name.fontSize=size*32f/26f;name.text=SignStyle.Backing(name,OldMark+bare)+OldMark+shows;oldMarked=Tag(camera,name,layer);
            }
            finally
            {
                face.SetFloat("_Band",SignStyle.NameBand);name.fontSize=size;name.text=shows;name.alpha=alpha;name.transform.position=home;name.ForceMeshUpdate();if(fade!=null)fade.enabled=fading;
            }
            string line=$"NAME_TAG \"{bare}\" made={made:0}px foot={foot:0}px (old {oldFoot:0}) marked={marked:0}px (old {oldMarked:0}); last frame {shot}";TestContext.WriteLine(line);Debug.Log(line);
            Assert.Greater(made,40,line+": fixture: the tag isn't measured");
            Assert.Greater(oldFoot,made*1.12f,line+": fixture: drawn as perspective had it, the name at the foot of the screen isn't larger, as MIRA's was");
            Assert.Greater(oldMarked,made*1.15f,line+": fixture: the old mark doesn't make the tag larger, as PELL's was");
            Assert.LessOrEqual(foot,made*1.04f,line+": a name at the foot of the screen stands larger than its made size, and its tag with it (MIRA, s69 038)");
            Assert.LessOrEqual(marked,made*1.04f,line+": a marked name's tag stands taller than any other's (PELL, s13 079)");
        }

        // ================================================================ D416, from the November 24 night capture (nov24-whole)
        static Image Picture(string name)=>All<Image>().FirstOrDefault(i=>i.name==name&&i.isActiveAndEnabled);
        static Rect Screen(RectTransform rect)=>PanelFade.ScreenBox(rect);
        /// <summary>sRGB levels a white letter behind a plate of the HUD's colour lifts its red, when this share of the world's light comes through (the canvas blends in linear light).</summary>
        static float Lift(float through)
        {
            float plate=Mathf.GammaToLinearSpace(UiKit.PlateColor.r/255f);
            return (Mathf.LinearToGammaSpace(plate*(1-through)+through)-Mathf.LinearToGammaSpace(plate))*255;
        }
        /// <summary>s57/155, 156: with nothing behind the vitals, the Decks notice board's chalk ('TOWN MEETS FIRST', 'MARKET FIRST OUT', 'CRESCENT, TUG DOCK') read through them
        /// as ghost letters, the plate's red lifted from 6 to 22 over each letter: the frame sheet's opening is 254 of 255 solid. Each of the HUD's plates of words has a solid
        /// backing under its opening, and when the plate steps aside for a body the backing goes with it.</summary>
        [UnityTest] public IEnumerator AHudPlateLetsNothingThroughAtRestAndItsBackingStepsAsideWithIt()
        {
            yield return Zone("Hub_Decks");
            Notices.Post("QUEST UPDATED","Harrowgate\nStand on the field while Harrowgate answers");
            float deadline=Time.unscaledTime+3;while(Picture("QuestToastFrame")==null&&Time.unscaledTime<deadline)yield return null;
            // At rest: a plate that stepped aside for something at the load comes back over half a second.
            bool Resting(string name){var p=Picture(name);return p==null||p.canvasRenderer.GetAlpha()>.999f;}
            deadline=Time.unscaledTime+2;while(!(Resting("VitalsFrame")&&Resting("ObjectiveFrame")&&Resting("QuestToastFrame"))&&Time.unscaledTime<deadline)yield return null;
            float sheet=1-254f/255f;
            Assert.Greater(Lift(sheet),8,"fixture: the sheet's opening alone doesn't lift a white letter behind it as the still's did (6 to 22)");
            var faults=new List<string>();
            foreach(string name in new[]{"VitalsFrame","ObjectiveFrame","QuestToastFrame"})
            {
                var frame=Picture(name);if(frame==null){faults.Add($"fixture: {name} isn't up");continue;}
                var under=frame.GetComponentsInChildren<Image>().FirstOrDefault(i=>i!=frame&&i.name.EndsWith("SolidUnder")&&i.isActiveAndEnabled);
                float through=sheet*(under!=null?1-under.color.a*under.canvasRenderer.GetAlpha():1);
                if(Lift(through)>1){faults.Add($"{name}: a white word behind it lifts its plate {Lift(through):0} levels at rest (no solid backing under its opening)");continue;}
                Rect outer=frame.rectTransform.rect,inner=under.rectTransform.rect;
                if(inner.width<outer.width-80||inner.height<outer.height-80)faults.Add($"{name}: its backing is {inner.width:0} by {inner.height:0} under a plate of {outer.width:0} by {outer.height:0}");
                if(inner.width>outer.width||inner.height>outer.height)faults.Add($"{name}: its backing runs past its frame");
            }
            // Something behind the objective for half a second: the plate steps aside and its backing with it.
            var objective=Object.FindFirstObjectByType<ObjectiveHud>();var plate=Picture("ObjectiveFrame");
            for(float end=Time.unscaledTime+.6f;Time.unscaledTime<end;){objective.Fade.Measure();var middle=Screen(objective.Frame).center;objective.Fade.Look(new Vector3(middle.x,middle.y,1),Time.unscaledTime);yield return null;}
            var backing=objective.Fade.Under;
            if(backing==null)faults.Add("the objective has no backing to step aside");
            else if(Mathf.Abs(backing.canvasRenderer.GetAlpha()-plate.canvasRenderer.GetAlpha())>.02f||plate.canvasRenderer.GetAlpha()>.6f)
                faults.Add($"stepping aside: plate {plate.canvasRenderer.GetAlpha():0.00}, its backing {backing.canvasRenderer.GetAlpha():0.00}: the backing stays solid over the body the plate steps aside for");
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
        /// <summary>s29/043: the roof's lit TEA ROOM read through '+3 Lattice Filament' and '+22 Scrap Alloy' under the feed's band at .8 (a fifth of the world's light). Each line has a
        /// band of its own over the feed's, so at most a twentieth of the world lies behind its words, as behind a panel's.</summary>
        [UnityTest] public IEnumerator APickupLineHasNoMoreThanATwentiethOfTheWorldBehindIt()
        {
            yield return Zone("Hub_Decks");
            foreach(string id in new[]{"ScrapAlloy","LatticeFilament","RidgeCrystal"})RpgServices.Inventory.Grant(id,22);
            yield return null;yield return null;
            var band=Picture("PickupBand");Assert.IsNotNull(band,"fixture: the feed's up");
            float feed=band.color.a*band.canvasRenderer.GetAlpha();
            Assert.Greater(1-feed,PanelFade.MostThrough,"fixture: the feed's band alone keeps the world from behind its words, as it didn't at s29 043");
            var lines=All<TMP_Text>().Where(t=>t.name=="PickupLine"&&t.isActiveAndEnabled).ToArray();Assert.GreaterOrEqual(lines.Length,3,"fixture: three lines");
            var bands=All<Image>().Where(i=>i.name=="PickupLineBand"&&i.isActiveAndEnabled).ToArray();
            var faults=new List<string>();
            foreach(var line in lines)
            {
                line.ForceMeshUpdate();var ink=UiKit.Ink(line);var middle=Screen(line.rectTransform).center;float s=line.canvas.scaleFactor;
                var words=new Rect(middle.x+ink.xMin*s,middle.y+ink.yMin*s,ink.width*s,ink.height*s);
                var own=bands.FirstOrDefault(b=>{var at=Screen(b.rectTransform);return at.Contains(words.min)&&at.Contains(words.max);});
                float through=(1-feed)*(own!=null?1-own.color.a*own.canvasRenderer.GetAlpha():1);
                if(through>PanelFade.MostThrough+.001f)faults.Add($"'{line.text}': {through:0.00} of the world lies behind its words");
                else if(own.transform.GetSiblingIndex()>line.transform.GetSiblingIndex())faults.Add($"'{line.text}': its band is drawn over its words");
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
        /// <summary>Reads, after every HUD script's LateUpdate this frame, what a check asks: where things stand as the frame is drawn.</summary>
        [DefaultExecutionOrder(5000)]
        sealed class AsDrawn:MonoBehaviour{public System.Func<float> read;public float? seen;void LateUpdate(){if(read!=null&&!seen.HasValue)seen=read();}}
        /// <summary>s52/171: the frame the objective grew from one line to three, the quest card still stood under the one line and cut '172 m' in half. The card is seated after the
        /// objective has written its words (it was seated in Update, a frame behind them), so as the frame is drawn it hangs under the objective's new foot.</summary>
        [UnityTest] public IEnumerator TheQuestCardStandsUnderTheObjectiveTheFrameItGrows()
        {
            yield return Zone("Hub_Decks");
            var objective=Object.FindFirstObjectByType<ObjectiveHud>();
            Notices.Post("QUEST UPDATED","Harrowgate\nStand on the field while Harrowgate answers");
            float deadline=Time.unscaledTime+3;while(Picture("QuestToastFrame")==null&&Time.unscaledTime<deadline)yield return null;
            var card=Picture("QuestToastFrame");Assert.IsNotNull(card,"fixture: the card's up");
            var probe=new GameObject("AsDrawn").AddComponent<AsDrawn>();yield return null;yield return null;
            Assert.GreaterOrEqual(Screen(objective.Frame).yMin-Screen(card.rectTransform).yMax,0f,"fixture: the card isn't under the objective to begin with");
            // Grown after every Update and before the LateUpdates, as the objective's words are written.
            var grown=objective.Frame;float was=grown.sizeDelta.y;
            probe.read=()=>Screen(grown).yMin-Screen(card.rectTransform).yMax;
            grown.sizeDelta=new Vector2(grown.sizeDelta.x,was+56);
            yield return null;
            grown.sizeDelta=new Vector2(grown.sizeDelta.x,was);float gap=probe.seen??float.NaN;Object.Destroy(probe.gameObject);
            Assert.IsFalse(float.IsNaN(gap),"fixture: the probe read nothing");
            Assert.GreaterOrEqual(gap,0f,$"the frame the objective grew, the card stood {-gap:0} px up into it as the frame was drawn (s52 171)");
        }
        /// <summary>s38/110: 'LAND — THE SKYFIELDS' on its plate at the top and 'A  Land — The Skyfields' in the prompt in one frame. While the prompt says a sign's words the sign
        /// is away, and back when it doesn't.</summary>
        [UnityTest] public IEnumerator ASignIsAwayWhileThePromptSaysItsWords()
        {
            yield return Zone("Orrery");
            var signs=All<TextMeshPro>().Where(t=>t.name.StartsWith("Sign_")&&t.TryGetComponent<SignFade>(out _)).ToArray();
            WarpBeacon warp=null;TextMeshPro sign=null;
            foreach(var beacon in All<WarpBeacon>()){sign=signs.FirstOrDefault(s=>s.text.Length>=4&&beacon.prompt.StartsWith(s.text,System.StringComparison.OrdinalIgnoreCase));if(sign!=null){warp=beacon;break;}}
            Assert.IsNotNull(warp,"fixture: a warp with its sign in the Orrery");
            Put(Lead,warp.transform.position);
            float deadline=Time.unscaledTime+4;while(PromptService.Interaction!=warp&&Time.unscaledTime<deadline){Put(Lead,warp.transform.position);yield return null;}
            Assert.AreEqual(warp,PromptService.Interaction,"fixture: the warp's prompt isn't offered at the warp");
            yield return null;yield return null;
            var fade=sign.GetComponent<SignFade>();
            Assert.AreEqual("said",fade.GoneFor,$"'{sign.text}' is drawn while the prompt says '{warp.prompt}' (s38 110)");
            Assert.Less(sign.alpha,.01f,"the sign is said and still drawn");
            Put(Lead,warp.transform.position+new Vector3(0,0,-60));
            deadline=Time.unscaledTime+3;while(PromptService.Interaction==warp&&Time.unscaledTime<deadline){Put(Lead,warp.transform.position+new Vector3(0,0,-60));yield return null;}
            yield return null;yield return null;
            Assert.AreNotEqual("said",fade.GoneFor,"the sign stays away with the prompt gone");
        }
    }
}
