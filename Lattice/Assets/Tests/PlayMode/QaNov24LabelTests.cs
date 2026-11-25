using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
    }
}
