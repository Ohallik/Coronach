using System.Collections;
using System.Collections.Generic;
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

        // ---------------------------------------------------------------- the fourth look round (D392, nov23-whole s55, s63, s66, s67)
        /// <summary>Every triangle of a renderer's submesh in world space, with its normal.</summary>
        static IEnumerable<(Vector3 a,Vector3 b,Vector3 c,Vector3 n)> Triangles(Renderer view,int submesh)
        {
            var mesh=view.GetComponent<MeshFilter>().sharedMesh;Assert.IsTrue(mesh.isReadable,"fixture: "+view.name+"'s mesh can be read");
            var v=mesh.vertices;var t=mesh.GetTriangles(submesh);var m=view.transform.localToWorldMatrix;
            for(int i=0;i<t.Length;i+=3){var a=m.MultiplyPoint3x4(v[t[i]]);var b=m.MultiplyPoint3x4(v[t[i+1]]);var c=m.MultiplyPoint3x4(v[t[i+2]]);yield return (a,b,c,Vector3.Cross(b-a,c-a).normalized);}
        }
        static Renderer Named(string name){var view=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(r=>r.name==name);Assert.IsNotNull(view,"fixture: "+name+" is in the scene");return view;}
        static GameObject Body(string name){var body=Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g=>g.scene.IsValid()&&g.name==name);Assert.IsNotNull(body,"fixture: "+name+" is in the scene");return body;}

        /// <summary>s63/150, s63/164: the bores in Mother of Doors' crater floor were bright filled cyan ellipses: flat dark discs on the floor with a glow 4.4 mouths wide laid over
        /// them (D382 had shrunk it from 9 to 15 m). A mouth reads as a hole when something stands round it deeper than its foot and no glow lies across it: each bore's collar stands
        /// at least half a metre over its foot, its throat goes down inside the collar, and every glow at a mouth lies inside its throat (half of it under three quarters of the
        /// collar's inner radius).</summary>
        [UnityTest] public IEnumerator TheCratersBoresAreMouthsWithTheirLightInsideThem()
        {
            bool Mouth(float lip,float glowHalf,float inner)=>lip>=.45f&&inner>0&&glowHalf<=inner*.75f;
            // The capture's: no collar, the glow 4.4 mouths across over a mouth's disc.
            Assert.IsFalse(Mouth(0,2.2f*3,3),"fixture: the capture's bore reads as a mouth");
            yield return Load("SorrelSecondSky");yield return Frames(4);
            var collars=Named("The bores' collars");var throats=Named("The bores' throats");
            var rim=collars.GetComponent<MeshFilter>().sharedMesh.vertices.Select(p=>collars.transform.TransformPoint(p)).ToArray();
            var wall=throats.GetComponent<MeshFilter>().sharedMesh.vertices.Select(p=>throats.transform.TransformPoint(p)).ToArray();
            var cores=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.name=="The young's light down the bore").ToArray();
            var glows=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.sharedMaterial!=null&&(r.sharedMaterial.name.EndsWith("Young glow")||r.sharedMaterial.name.EndsWith("Young spill"))&&r.GetComponent<MeshFilter>()!=null&&r.GetComponent<MeshFilter>().sharedMesh!=null&&r.GetComponent<MeshFilter>().sharedMesh.name=="Quad").ToArray();
            Assert.GreaterOrEqual(cores.Length,11,"fixture: the crater's eleven bores");
            float Flat(Vector3 a,Vector3 b)=>Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));
            foreach(var core in cores)
            {
                // Its mouth's radius from its glow's size (LastMooringBuilder.Sorrel: the glow is 1.15 of the bore's foot, the foot .62 of the mouth).
                var c=core.transform.position;float foot=c.y-.06f,wide=core.transform.lossyScale.x/(.62f*1.15f);
                var mine=rim.Where(p=>Flat(p,c)<wide*1.7f).ToArray();Assert.IsNotEmpty(mine,$"the bore at {c} has no collar");
                float lip=mine.Max(p=>p.y)-foot,inner=mine.Where(p=>p.y>foot+lip*.85f).Min(p=>Flat(p,c));
                Assert.IsTrue(wall.Any(p=>Flat(p,c)<inner&&p.y<foot+lip*.5f),$"the bore at {c} has no throat going down inside its collar");
                float glowHalf=glows.Where(g=>Flat(g.bounds.center,c)<inner).Select(g=>Mathf.Max(g.bounds.extents.x,g.bounds.extents.z)).DefaultIfEmpty(0).Max();
                TestContext.WriteLine($"SORREL_BORE at ({c.x:0.0},{c.z:0.0}) lip={lip:0.00} inner={inner:0.00} glow half={glowHalf:0.00}");
                Assert.IsTrue(Mouth(lip,glowHalf,inner),$"the bore at {c} reads as a disc of light: its collar {lip:0.00} m over its foot, a glow {glowHalf:0.00} m in half-width inside {inner:0.00} m");
            }
        }

        /// <summary>s66/149, s66/153: among the Continuance's finished plate a flat teal strip ran the width of her back (the bands, in the flat teal of a blockout) and the decks'
        /// skylights were bright cyan squares (the lamps' teal). Neither may stay: no up-facing piece of the back's flat teal is a strip ten metres long and a metre wide, and no
        /// skylight pane is a lamp.</summary>
        [UnityTest] public IEnumerator TheContinuancesBackHasNoFlatTealStripsOrCyanPanes()
        {
            bool Strip(float longer,float shorter)=>longer>=10&&shorter>=1;bool Pane(float longer,float shorter)=>longer>=1&&shorter>=1;
            Assert.IsTrue(Strip(76,2.4f),"fixture: the capture's band is a strip");Assert.IsTrue(Pane(1.6f,1.2f),"fixture: the capture's skylight is a pane");
            yield return Load("Continuance");yield return Frames(4);
            (float longer,float shorter) Extent((Vector3 a,Vector3 b,Vector3 c,Vector3 n) t){var b=new Bounds(t.a,Vector3.zero);b.Encapsulate(t.b);b.Encapsulate(t.c);return (Mathf.Max(b.size.x,b.size.z),Mathf.Min(b.size.x,b.size.z));}
            var banded=Named("The hull's back, banded");int tealAt=System.Array.FindIndex(banded.sharedMaterials,m=>m!=null&&m.name.Contains("teal"));Assert.GreaterOrEqual(tealAt,0,"fixture: the back's teal");
            var strips=Triangles(banded,tealAt).Where(t=>t.n.y>.9f).Select(Extent).Where(e=>Strip(e.longer,e.shorter)).ToList();
            var lit=Named("The hull's lit windows");int lampAt=System.Array.FindIndex(lit.sharedMaterials,m=>m!=null&&m.name.Contains("lamp-teal"));
            var panes=lampAt<0?new List<(float,float)>():Triangles(lit,lampAt).Where(t=>t.n.y>.9f).Select(Extent).Where(e=>Pane(e.longer,e.shorter)).ToList();
            TestContext.WriteLine($"CONTINUANCE_FLAT teal strips={strips.Count} lamp panes={panes.Count} (teal material {banded.sharedMaterials[tealAt].name}, lamp {(lampAt<0?"none":lit.sharedMaterials[lampAt].name)})");
            Assert.IsEmpty(strips,$"{strips.Count} flat teal strips across the Continuance's back (the first {(strips.Count>0?strips[0].longer+" by "+strips[0].shorter:"")} m)");
            Assert.IsEmpty(panes,$"{panes.Count} skylight panes on the Continuance's back are the lamps' flat teal");
        }

        static float Lightness(Texture map)
        {
            var rt=RenderTexture.GetTemporary(64,64,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Graphics.Blit(map,rt);
            var prior=RenderTexture.active;RenderTexture.active=rt;var read=new Texture2D(64,64,TextureFormat.RGBA32,false);read.ReadPixels(new Rect(0,0,64,64),0,0);read.Apply();RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);
            float mean=read.GetPixels().Average(c=>(c.r+c.g+c.b)/3);Object.Destroy(read);return mean;
        }
        /// <summary>s67/193, 203, 204: at the west clamp a smith stood as a near-black shape with no form. It was Tovra: her generated body's base map is the darkest of any (about a
        /// fifth of white where Brekk's is over a third), and the chamber's cold low light left nothing of it. Each smith at the clamps is drawn as light as the others: their base map's
        /// mean, times what their tint makes of it, at least four fifths of the four's middle.</summary>
        [UnityTest] public IEnumerator NobodyAtTheClampsIsABlackShape()
        {
            yield return Load("StillwaterChamber");yield return Frames(4);
            string[] names={"Tovra, at the first clamp","Brekk, at the second clamp","A smith, at the third clamp","A smith, at the fourth clamp"};
            var drawn=new List<(string name,float plain,float now)>();
            foreach(var name in names)
            {
                var body=Body(name);var skin=body.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault();Assert.IsNotNull(skin,"fixture: "+name+" has a body");
                var material=skin.sharedMaterial;float plain=Lightness(material.GetTexture("_BaseMap"))*material.GetColor("_BaseColor").grayscale;
                var tint=body.GetComponentInChildren<PropTint>(true);float now=plain*(tint!=null?(tint.tint.r+tint.tint.g+tint.tint.b)/3:1);
                drawn.Add((name,plain,now));TestContext.WriteLine($"CHAMBER_SMITH {name} body={skin.sharedMesh.name} plain={plain:0.000} drawn={now:0.000}");
            }
            float Middle(IEnumerable<float> v){var s=v.OrderBy(x=>x).ToArray();return (s[1]+s[2])*.5f;}
            Assert.Less(drawn.Min(d=>d.plain),.8f*Middle(drawn.Select(d=>d.plain)),"fixture: untinted, one smith is darker than the others");
            var middle=Middle(drawn.Select(d=>d.now));
            foreach(var d in drawn)Assert.GreaterOrEqual(d.now,.8f*middle,$"{d.name} is drawn at {d.now:0.000} against the smiths' {middle:0.000}: a black shape under the chamber's light");
        }

        /// <summary>s67/112, s67/117: at play size Meret was still a small pale figure, though her ring and strut set her apart. In her frame she is drawn larger than a hero, and the
        /// frame throws its own light on her face from in front of her brow: neither was so in the capture (drawn at one, a hero's size; no light in the frame).</summary>
        [UnityTest] public IEnumerator MeretStandsOverTheHeroesAndHerFrameLightsHer()
        {
            Floor();
            var definition=GameCatalog.Find<EnemyDef>("MeretWell");var meret=ActorFactory.Enemy(definition,new Vector3(230,.05f,0));meret.Passive=true;meret.transform.rotation=Quaternion.identity;
            Place(Taren,new Vector3(236,.1f,0));yield return Frames(6);
            var visual=meret.GetComponent<DefeatPresentation>().visual;var body=Skinned(visual);float hero=Skinned(Taren.transform).size.y;float drawn=definition.drawnScale>0?definition.drawnScale:1;
            TestContext.WriteLine($"MERET_PRESENCE hero={hero:0.00} meret={body.size.y:0.00} ({body.size.y/hero:0.00}x, drawn at {drawn:0.00}; at one {body.size.y/drawn/hero:0.00}x)");
            Assert.Less(body.size.y/drawn/hero,1.05f,"fixture: drawn at one, she is no taller than a hero");
            Assert.GreaterOrEqual(body.size.y/hero,1.05f,$"Meret is drawn {body.size.y/hero:0.00} of a hero's height in her frame");
            var gear=visual.GetComponentInChildren<HessaRegalia>(true);Assert.IsNotNull(gear,"fixture: her frame");
            var lamps=gear.GetComponentsInChildren<Light>(true).Where(l=>l.enabled&&l.type==LightType.Point).ToArray();
            Assert.IsNotEmpty(lamps,"her frame throws no light on her");
            var head=new Vector3(body.center.x,body.max.y,body.center.z);var lamp=lamps.OrderBy(l=>Vector3.Distance(l.transform.position,head)).First();
            float apart=Vector3.Distance(lamp.transform.position,head);
            TestContext.WriteLine($"MERET_FRAME_LIGHT at {apart:0.00} m from her crown, range {lamp.range:0.00}, {lamp.intensity:0.00} {lamp.color}");
            Assert.Less(apart,1.2f,"her frame's light is not at her head");Assert.Greater(lamp.range,apart+.6f,"her frame's light doesn't reach her face and coat");
        }

        /// <summary>s67/112, s67/117: the control well's cover anchors read as simple banded pillars. Each is a control column now: screens lit in at least three of its faces (the
        /// capture's capstan had one lit thing, its crown, facing up).</summary>
        [UnityTest] public IEnumerator TheWellsCoverAnchorsAreMachinesWithScreens()
        {
            int Faces(IEnumerable<Vector3> normals)=>normals.Where(n=>Mathf.Abs(n.y)<.8f).Select(n=>Mathf.RoundToInt(Mathf.Atan2(n.x,n.z)*Mathf.Rad2Deg/90f)&3).Distinct().Count();
            Assert.Less(Faces(new[]{Vector3.up,Vector3.up}),3,"fixture: the capture's crown lights no face");
            yield return Load("StillwaterChamber");yield return Frames(4);
            for(int i=1;i<=3;i++)
            {
                var root=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Well cover anchor "+i)).ToArray();Assert.IsNotEmpty(root,"fixture: well cover anchor "+i);
                var lit=root.Where(r=>r.sharedMaterials.Any(m=>m!=null&&m.HasProperty("_EmissionColor")&&m.GetColor("_EmissionColor").maxColorComponent>.5f)).ToArray();
                var normals=lit.SelectMany(r=>Enumerable.Range(0,r.GetComponent<MeshFilter>().sharedMesh.subMeshCount).Where(k=>r.sharedMaterials[Mathf.Min(k,r.sharedMaterials.Length-1)].GetColor("_EmissionColor").maxColorComponent>.5f).SelectMany(k=>Triangles(r,k)).Select(t=>t.n)).ToList();
                TestContext.WriteLine($"WELL_COVER {i} lit renderers={lit.Length} lit faces={Faces(normals)}");
                Assert.GreaterOrEqual(Faces(normals),3,$"well cover anchor {i} has lit screens on {Faces(normals)} faces: a banded pillar, not a machine");
            }
        }

        /// <summary>s55/035: translucent orange compass-star rays drew across the chart table's front and legs: its star was painted on the floor under the table, its points out to
        /// 4.6 m. The star is in the chart on the table's top now; nothing of the floor's paint lies within the table's reach (2.6 m of its middle).</summary>
        [UnityTest] public IEnumerator TheChartTablesStarIsOnItsTopNotTheFloor()
        {
            var table=new Vector2(0,47.2f);   // ManyHandsBuilder.City.ChartTable
            bool UnderIt(IEnumerable<Vector2> paint)=>paint.Any(p=>Vector2.Distance(p,table)<2.6f);
            Assert.IsTrue(UnderIt(new[]{table,table+Vector2.up*4.6f}),"fixture: the capture's star lies under the table");
            yield return Load("ManyHands");yield return Frames(4);
            var floor=Named("Floor detail the compass rose and rugs");
            var paint=floor.GetComponent<MeshFilter>().sharedMesh.vertices.Select(p=>floor.transform.TransformPoint(p)).Select(p=>new Vector2(p.x,p.z)).ToArray();
            TestContext.WriteLine($"HANDS_FLOOR_ROSE nearest to the table {paint.Min(p=>Vector2.Distance(p,table)):0.00} m");
            Assert.IsFalse(UnderIt(paint),"the floor's compass paint lies under the chart table and draws across its side");
        }
    }
}
