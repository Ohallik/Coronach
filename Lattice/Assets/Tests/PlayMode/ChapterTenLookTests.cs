using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>What of chapter 10's third look round (D373, nov22b-whole s66 and s67) can be measured: Meret read as a pale figure a hero's height with no face or fronds at play size
    /// (s67/117). The Continuance's finish is judged on stills (Builds/quality/look/d373-after beside nov22b-whole s66/021 and s66/153). Each case requires the capture's state to fail
    /// first, so a check that can't fail can't pass.</summary>
    public sealed class ChapterTenLookTests
    {
        CombatActor Taren=>PartyController.Current.members.First(m=>m.character=="Taren");
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
            yield return Load("Arena_Ground");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);
        }
        [UnityTearDown] public IEnumerator Cleanup(){GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Load(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+20;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static void Floor(){var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Look fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();if(body!=null)body.enabled=false;actor.transform.position=position;if(body!=null)body.enabled=true;actor.target=null;actor.TargetLocked=false;Physics.SyncTransforms();
        }
        static Bounds Skinned(Transform root){var parts=root.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).ToArray();Assert.IsNotEmpty(parts,"fixture: "+root.name+" has a skinned body");var all=parts[0].bounds;foreach(var p in parts)all.Encapsulate(p.bounds);return all;}

        /// <summary>s67/117: in her own duel Meret was a white figure a hero's height and a hero's outline. Her boss body wears her emergency frame now: the Compact's ring behind her
        /// head and a spine over it, a strut past each shoulder, a dark mantle down her back. With the frame hidden (the capture's state) nothing stood over her head or past her
        /// shoulders. Drawn only: her capsule and her body's own mesh, found first, are as they were.</summary>
        [UnityTest] public IEnumerator MeretsOutlineInHerWellIsNotAHeros()
        {
            Floor();
            var definition=GameCatalog.Find<EnemyDef>("MeretWell");var meret=ActorFactory.Enemy(definition,new Vector3(230,.05f,0));meret.Passive=true;meret.transform.rotation=Quaternion.identity;
            Place(Taren,new Vector3(236,.1f,0));yield return Frames(6);
            var visual=meret.GetComponent<DefeatPresentation>().visual;var body=Skinned(visual);float hero=Skinned(Taren.transform).size.y;
            var gear=visual.GetComponentInChildren<HessaRegalia>(true);Assert.IsNotNull(gear,"Meret's boss body in the well carries no emergency frame");
            (float over,float wider) Outline()
            {
                var drawn=visual.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(r is MeshRenderer or SkinnedMeshRenderer)).ToArray();
                var all=drawn[0].bounds;foreach(var r in drawn)all.Encapsulate(r.bounds);
                return (all.max.y-body.max.y,Mathf.Max(all.size.x,all.size.z)-Mathf.Max(body.size.x,body.size.z));
            }
            gear.gameObject.SetActive(false);yield return null;var then=Outline();gear.gameObject.SetActive(true);yield return null;var now=Outline();
            TestContext.WriteLine($"MERET_OUTLINE hero={hero:0.00} meret={body.size.y:0.00} ({body.size.y/hero:0.00}x) over her head then={then.over:0.00} now={now.over:0.00} wider than her body then={then.wider:0.00} now={now.wider:0.00}");
            Assert.Less(then.over,.1f,"fixture: with her frame hidden, nothing stands over her head");
            Assert.GreaterOrEqual(now.over,.3f,$"her frame stands {now.over:0.00} m over her head: from the play camera her outline is a hero's");
            // Past her shoulders: a skinned body's box is its bind pose's (arms out), so the frame's own span is read against her shoulders' joints.
            var arms=visual.GetComponentInChildren<Animator>();float shoulders=Vector3.Distance(arms.GetBoneTransform(HumanBodyBones.LeftUpperArm).position,arms.GetBoneTransform(HumanBodyBones.RightUpperArm).position);
            var frameParts=gear.GetComponentsInChildren<Renderer>();var frame=frameParts[0].bounds;foreach(var r in frameParts)frame.Encapsulate(r.bounds);
            TestContext.WriteLine($"MERET_FRAME span={Mathf.Max(frame.size.x,frame.size.z):0.00} shoulders={shoulders:0.00}");
            Assert.GreaterOrEqual(Mathf.Max(frame.size.x,frame.size.z),shoulders*2.5f,"her frame doesn't reach past her shoulders");
            var lit=gear.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.GetColor("_EmissionColor").maxColorComponent>.5f).ToArray();
            Assert.IsNotEmpty(lit,"her frame has no light of its own: her ring is a dark hoop at play size");
            var cc=meret.GetComponent<CharacterController>();
            TestContext.WriteLine($"MERET_CAPSULE height={cc.height:0.00} radius={cc.radius:0.00}");
            Assert.IsEmpty(gear.GetComponentsInChildren<Collider>(true),"her frame has a collider: it is drawn only");
            Assert.AreSame(visual.GetComponentsInChildren<Renderer>().First(r=>r is MeshRenderer or SkinnedMeshRenderer).GetType(),typeof(SkinnedMeshRenderer),"her frame is found before her body's own mesh");
        }

        /// <summary>s67/117: her body's rim was the intake's dark grey (.25, .25, .3) at none in shade, so on the chamber's cold whites her edge didn't show and her white coat ran into the
        /// floor. Her rim is a bright colour of her own now, kept in shade, and its hue is well away from every coloured light within thirty metres of her post.</summary>
        [UnityTest] public IEnumerator MeretsRimIsAColourHerChamberIsNot()
        {
            yield return Load("StillwaterChamber");yield return Frames(4);
            var post=new Vector3(0,-16,63);   // the well's middle (LastMooringBuilder.Chamber.W)
            var colours=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.enabled&&l.gameObject.activeInHierarchy&&(l.type==LightType.Directional||Vector3.Distance(l.transform.position,post)<30))
                .Select(l=>l.color).Where(c=>{Color.RGBToHSV(c,out _,out float s,out _);return s>=.2f;}).ToList();
            Assert.Greater(colours.Count,2,"fixture: the chamber's coloured lights");
            float Hue(Color c){Color.RGBToHSV(c,out float h,out _,out _);return h*360;}
            float Apart(Color rim)=>colours.Min(c=>{float d=Mathf.Abs(Hue(c)-Hue(rim))%360;return Mathf.Min(d,360-d);});
            bool Reads(Color rim,float inShade){Color.RGBToHSV(rim,out _,out float s,out float v);return v>=.8f&&s>=.3f&&inShade>=.4f&&Apart(rim)>=40;}
            var meret=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MeretWell"),new Vector3(0,-200,0));meret.Passive=true;yield return null;
            var material=meret.GetComponentsInChildren<Renderer>().First(r=>r is SkinnedMeshRenderer).sharedMaterial;
            var rim=material.GetColor("_RimColor");float shade=material.GetFloat("_RimUnlit");
            TestContext.WriteLine($"MERET_RIM rim={rim} hue={Hue(rim):0} in shade={shade:0.00} apart={Apart(rim):0} lights={string.Join(" ",colours.Select(c=>Hue(c).ToString("0")))}");
            Assert.IsFalse(Reads(new Color(.25f,.25f,.3f),0),"fixture: the capture's rim reads");
            Assert.IsTrue(Reads(rim,shade),$"Meret's rim {rim} (in shade {shade:0.00}, {Apart(rim):0} degrees from the nearest light) is not a bright colour of her own in her chamber");
            Object.Destroy(meret.gameObject);
        }
    }
}
