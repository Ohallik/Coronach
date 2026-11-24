using System.Collections.Generic;
using Lattice.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Meret Venn as the boss of her own duel (D373; nov22b-whole s67/117): at play size she was a pale white figure, a hero's height, with no face or fronds you could make out, on a
    /// floor of the same cold whites. As Hessa was made to read (D360): a shape no hero has, her own colours, and a rim the room isn't. Her boss body in the well is her generated body
    /// with the emergency frame on it, all drawn only: a slate spine up her back to over her head, the Compact's ring behind her head (gold, lit teal inside it: the director's mark, and
    /// a dark disc her pale head stands against), a strut past each shoulder with a lit tip, and a mantle of the Compact's deep teal down her back with a gold hem, so a dark mass stands
    /// behind the white coat. Her rim is a lilac of her own skin's (her portrait's lavender), well away from the chamber's cold blues and its warm floods. The gear rides her upper chest as
    /// Hessa's does (<see cref="HessaRegalia"/>), and comes off her as she yields: the frame is what she puts down. Her capsule, her moves and what she is hit by are as they were.
    /// </summary>
    public static partial class LastMooringBuilder
    {
        internal static class MeretFrame
        {
            internal const string DirectorPath="Assets/_Project/Prefabs/Enemies/MeretWellDirector.prefab";
            const string Folder="Assets/_Project/Art/Generated/Continuance/";
            /// <summary>Her rim: lilac, the hue of her own face, which no light in the chamber is (its floods are warm at 30 to 40 degrees, its lamps cold at 190 to 215).</summary>
            internal static readonly Color Rim=new(.74f,.52f,1);
            internal const float RimUnlit=.55f,LightFloor=.34f,RimPower=3.6f;

            /// <summary>Her boss body in the well: her generated body, dressed. Made again every run from the body as it is now.</summary>
            internal static GameObject Director(GameObject body)
            {
                if(body==null)return null;
                var made=(GameObject)PrefabUtility.InstantiatePrefab(body);
                try
                {
                    made.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                    var animator=made.GetComponentInChildren<Animator>();
                    Transform Bone(HumanBodyBones b)=>animator!=null&&animator.isHuman?animator.GetBoneTransform(b):null;
                    var chest=Bone(HumanBodyBones.UpperChest)??Bone(HumanBodyBones.Chest)??Bone(HumanBodyBones.Spine);
                    var left=Bone(HumanBodyBones.LeftUpperArm);var right=Bone(HumanBodyBones.RightUpperArm);var hips=Bone(HumanBodyBones.Hips);
                    if(chest==null||left==null||right==null||hips==null)throw new System.InvalidOperationException("MERET_FRAME her body has no humanoid chest, shoulders and hips to dress");
                    var toes=Bone(HumanBodyBones.LeftToes);var heel=Bone(HumanBodyBones.LeftFoot);
                    float facing=toes!=null&&heel!=null&&Mathf.Abs(toes.position.z-heel.position.z)>.01f?Mathf.Sign(toes.position.z-heel.position.z):1;
                    var o=chest.position;var drawn=ModelGeometry.BoundsOf(made);
                    float top=drawn.max.y,back=facing>0?drawn.min.z:2*o.z-drawn.max.z,shoulder=(left.position.y+right.position.y)*.5f,half=Mathf.Abs(left.position.x-right.position.x)*.5f,hip=hips.position.y;
                    float k=Mathf.Max(1,shoulder/1.45f);
                    Debug.Log($"MERET_FRAME body {drawn.size.y:0.00} m tall, facing {(facing>0?"+z":"-z")}, shoulders {shoulder:0.00} m up and {half*2:0.00} apart, hips {hip:0.00}, back {back-o.z:0.00} from her chest");
                    var root=new GameObject("Meret's emergency frame");root.transform.SetParent(made.transform,false);root.transform.position=o;

                    var frame=new ZoneKit.Shape();var lit=new ZoneKit.Shape();
                    var slate=new Color(.2f,.24f,.28f);var slateTop=new Color(.42f,.47f,.52f);var gold=new Color(.9f,.72f,.36f);
                    // The spine: from the small of her back to over her head, leaning back a little; its lit line up its back.
                    var spineFoot=new Vector3(0,hip+.1f*k,back-.06f*k);var spineHead=new Vector3(0,top+.42f*k,back-.14f*k);
                    frame.Slot(0).Beam(spineFoot,spineHead,.13f*k,.09f*k,slate,slateTop);
                    lit.Slot(0).Beam(spineFoot+new Vector3(0,.15f*k,-.05f*k),spineHead+new Vector3(0,-.08f*k,-.05f*k),.035f*k,.02f*k,Color.white);
                    // The ring behind her head: a dark disc, a gold ring round it, a lit teal ring inside the gold, facing the way she does.
                    var ring=new Vector3(0,top-.12f*k,back-.18f*k);float rr=.36f*k;
                    frame.Slot(0).Disc(ring+Vector3.back*.01f,Vector3.forward,rr*.92f,32,new Color(.12f,.15f,.18f),0,null);
                    frame.Slot(0).Disc(ring+Vector3.back*.012f,Vector3.back,rr*.92f,32,new Color(.12f,.15f,.18f),0,null);
                    var hoop=new List<Vector3>();var hoopR=new List<float>();var inner=new List<Vector3>();var innerR=new List<float>();
                    for(int n=0;n<=32;n++){float a=n/32f*Mathf.PI*2;hoop.Add(ring+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*rr);hoopR.Add(.032f*k);inner.Add(ring+new Vector3(Mathf.Cos(a),Mathf.Sin(a),.015f)*rr*.82f);innerR.Add(.018f*k);}
                    frame.Slot(1).Tube(hoop,hoopR,8,gold,gold,8,.5f,false);
                    lit.Slot(0).Tube(inner,innerR,6,Color.white,Color.white,8,.5f,false);
                    // A strut past each shoulder, rising, its tip lit.
                    foreach(int side in new[]{-1,1})
                    {
                        var path=new List<Vector3>{new(0,shoulder+.02f*k,back-.08f*k),new(side*(half+.12f*k),shoulder+.1f*k,back-.1f*k),new(side*(half+.32f*k),shoulder+.42f*k,back-.14f*k),new(side*(half+.36f*k),shoulder+.64f*k,back-.16f*k)};
                        frame.Slot(0).Tube(path,new[]{.05f*k,.048f*k,.042f*k,.036f*k},8,slate,slateTop,1,.5f);
                        frame.Slot(1).Box(new Vector3(side*(half+.1f*k),shoulder+.06f*k,back-.06f*k),new Vector3(.2f*k,.12f*k,.16f*k),Quaternion.Euler(0,0,side*-14),gold,gold);
                        lit.Slot(0).Tube(new List<Vector3>{path[3],path[3]+new Vector3(side*.02f,.16f,-.01f)*k},new[]{.05f*k,.03f*k},8,Color.white,Color.white,1,.5f);
                    }
                    var frameMesh=Save(frame.Mesh("Meret frame"),o,"meret-frame");var litMesh=Save(lit.Mesh("Meret frame lights"),o,"meret-frame-lights");
                    Part(root,"Frame",frameMesh,new[]{FrameMat(),FrameMat()},ShadowCastingMode.On);
                    Part(root,"Frame lights",litMesh,new[]{LitMat()},ShadowCastingMode.Off);
                    // The mantle: the Compact's deep teal from her shoulders down her back to her knees, flaring a little, a gold hem.
                    float mantleTop=shoulder+.06f*k,mantleFoot=hip-.42f*k;
                    var mantle=Sheet(14,12,(u,v)=>
                    {
                        float y=Mathf.Lerp(mantleTop,mantleFoot,v),wide=Mathf.Lerp(half+.06f*k,half+.2f*k,v),x=Mathf.Lerp(-wide,wide,u),edge=2*u-1;
                        float bulge=(.1f*(1-edge*edge)+.06f*v)*k;float wrap=Mathf.SmoothStep(0,1,1-v*4)*Mathf.Abs(edge)*.18f*k;
                        return new Vector3(x,y,back-.03f*k-bulge+wrap);
                    });
                    Part(root,"Mantle",Save(mantle,o,"meret-mantle"),new[]{MantleMat()},ShadowCastingMode.TwoSided);
                    if(facing<0)root.transform.rotation=Quaternion.Euler(0,180,0);
                    var follow=root.AddComponent<HessaRegalia>();follow.anchor=chest;follow.basis=made.transform;follow.offset=chest.InverseTransformPoint(root.transform.position);follow.turn=Quaternion.Inverse(made.transform.rotation)*root.transform.rotation;
                    root.transform.SetAsLastSibling();
                    var all=ModelGeometry.BoundsOf(made);
                    Debug.Log($"MERET_FRAME dressed {all.size.y:0.00} m tall and {all.size.x:0.00} wide with the frame (body {drawn.size.y:0.00} by {drawn.size.x:0.00})");
                    return PrefabUtility.SaveAsPrefabAsset(made,DirectorPath);
                }
                finally{Object.DestroyImmediate(made);}
            }

            static Mesh Save(Mesh mesh,Vector3 origin,string file)
            {
                var at=mesh.vertices;for(int i=0;i<at.Length;i++)at[i]-=origin;mesh.vertices=at;mesh.RecalculateBounds();
                return ZoneKit.SaveMesh(mesh,Folder+file+".asset");
            }
            static void Part(GameObject root,string name,Mesh mesh,Material[] materials,ShadowCastingMode shadows)
            {
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var view=go.GetComponent<MeshRenderer>();var mats=new Material[Mathf.Max(1,mesh.subMeshCount)];
                for(int i=0;i<mats.Length;i++)mats[i]=materials[Mathf.Min(i,materials.Length-1)];view.sharedMaterials=mats;view.shadowCastingMode=shadows;
            }
            /// <summary>Lit as her body is: the same rim, rim in shade and least light, so the frame and the woman in it read as one figure.</summary>
            static void Body(Material m){m.SetFloat("_RampSteps",3);m.SetColor("_RimColor",Rim);m.SetFloat("_RimUnlit",RimUnlit);m.SetFloat("_LightFloor",LightFloor);m.SetFloat("_RimPower",RimPower);m.SetColor("_ShadowTint",new Color(.44f,.44f,.58f));}
            static Material FrameMat()=>ZoneKit.Toon("meret-frame",null,m=>{ZoneKit.Tinted(m);Body(m);});
            static Material LitMat()=>ZoneKit.Toon("meret-frame-lit",null,m=>{m.SetColor("_BaseColor",new Color(.62f,.96f,1));m.SetColor("_EmissionColor",new Color(.3f,1.35f,1.45f));m.SetFloat("_LightFloor",.6f);});
            static Material MantleMat()
            {
                // The Compact's deep teal, a fine twill, a gold band a hand above the hem and the hem itself a darker teal.
                var map=ZoneKit.Texture("Continuance/meret-mantle",256,(u,v)=>
                {
                    float twill=.5f+.5f*Mathf.Sin((u+v)*220*Mathf.PI),n=ZoneKit.Cloud(u,v,6,19);
                    var cloth=new Color(.08f,.26f,.3f)*(.85f+.22f*n+.06f*twill);
                    // The sheet's foot is the texture's foot (v 0): the band a hand over it.
                    float band=ZoneKit.Smooth(.085f,.1f,v)*(1-ZoneKit.Smooth(.145f,.16f,v));cloth=Color.Lerp(cloth,new Color(.86f,.68f,.34f)*(.9f+.2f*n),band);
                    return Color.Lerp(cloth,cloth*.6f,1-ZoneKit.Smooth(0,.05f,v));
                },false);
                return ZoneKit.TwoSided(ZoneKit.Toon("meret-mantle",map,Body));
            }
            /// <summary>A grid laid by a function of (u, v), each 0 to 1, v down: a cloth.</summary>
            static Mesh Sheet(int nu,int nv,System.Func<float,float,Vector3> at)
            {
                var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
                for(int j=0;j<=nv;j++)for(int i=0;i<=nu;i++){float u=i/(float)nu,v=j/(float)nv;vertices.Add(at(u,v));uv.Add(new Vector2(u,1-v));}
                for(int j=0;j<nv;j++)for(int i=0;i<nu;i++){int a=j*(nu+1)+i,b=a+nu+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
                var mesh=new Mesh{name="Meret mantle"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
            }
        }
    }
}
