using System.Collections.Generic;
using Lattice.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Hessa Korl as the boss of her room (nov22-whole s15/135, 149; D360). Drawn 1.3 times over she was still a third hero in orange, a little taller than the party, and her warm rim
    /// was lost on a floor lit orange by the lamps. Her boss body is her own generated body with a captain's gear on it, all drawn only: a yoke across her shoulders with a stolen relay
    /// lantern hung past each one (the Lampbreaker's crystal is the relay's own glass), the Muster's standard on a pole a head and more over her own, and a mantle over her shoulders and
    /// down her back. A silhouette nobody in the party has, with two lights at her shoulders and a flag over her, from the play camera's height. The gear is a prefab variant of her body
    /// (<see cref="Captain"/>), so the captain under guard after the fight, who wears her plain body, has been stripped of it. Painted and shaped here; the glass is the kit's.
    /// </summary>
    public static class HessaRegaliaBuilder
    {
        public const string CaptainPath="Assets/_Project/Prefabs/Enemies/HessaKorlCaptain.prefab";
        const string Folder="Assets/_Project/Art/Generated/Lantern/";
        /// <summary>Her rim, in a colour the relay's floor never is: its stone is a dark sea-green, its lamps and the Great Lantern light it orange and gold, so a warm orange edge
        /// (D347's) vanished on it. A hostile crimson with rose in it reads on both, and is still warmer than it is cold (EveryBodyInTheDarkKeepsARimInShade).</summary>
        public static readonly Color Rim=new(1,.14f,.4f);
        /// <summary>An edge, not a coat: at D347's broad 2.4 her rim painted her whole body its colour (the capture's orange figure, and in this colour a pink one).</summary>
        public const float RimUnlit=.62f,LightFloor=.34f,RimPower=3.6f;

        /// <summary>Her boss body: her generated body, dressed. Made again every run from the body as it is now.</summary>
        public static GameObject Captain(GameObject body)
        {
            if(body==null)return null;
            used.Clear();
            var made=(GameObject)PrefabUtility.InstantiatePrefab(body);
            try
            {
                made.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var animator=made.GetComponentInChildren<Animator>();
                Transform Bone(HumanBodyBones b)=>animator!=null&&animator.isHuman?animator.GetBoneTransform(b):null;
                var chest=Bone(HumanBodyBones.UpperChest)??Bone(HumanBodyBones.Chest)??Bone(HumanBodyBones.Spine);
                var left=Bone(HumanBodyBones.LeftUpperArm);var right=Bone(HumanBodyBones.RightUpperArm);var hips=Bone(HumanBodyBones.Hips);
                if(chest==null||left==null||right==null||hips==null)throw new System.InvalidOperationException("HESSA_REGALIA her body has no humanoid chest, shoulders and hips to dress");
                // Which way she faces, by where her toes point from her feet: everything below is laid out as for a body facing +z, and turned round if hers faces the other way.
                var toes=Bone(HumanBodyBones.LeftToes);var heel=Bone(HumanBodyBones.LeftFoot);
                float facing=toes!=null&&heel!=null&&Mathf.Abs(toes.position.z-heel.position.z)>.01f?Mathf.Sign(toes.position.z-heel.position.z):1;
                var o=chest.position;
                // Her own body's box: its back is where the gear sits, its top where her head is.
                var drawn=ModelGeometry.BoundsOf(made);
                float top=drawn.max.y,back=facing>0?drawn.min.z:2*o.z-drawn.max.z,shoulder=(left.position.y+right.position.y)*.5f,half=Mathf.Abs(left.position.x-right.position.x)*.5f,hip=hips.position.y;
                Debug.Log($"HESSA_REGALIA body {drawn.size.y:0.00} m tall, facing {(facing>0?"+z":"-z")}, shoulders {shoulder:0.00} m up and {half*2:0.00} apart, hips {hip:0.00}, back {back-o.z:0.00} from her chest");
                var root=new GameObject("Hessa's regalia");root.transform.SetParent(made.transform,false);root.transform.position=chest.position;
                // Sized against her own body: what is written here in metres is for a body with its shoulders 1.45 m up, and hers are higher. The standard grows less than she does.
                float k=Mathf.Max(1,shoulder/1.45f),f=Mathf.Sqrt(k);

                var iron=Iron();var cloth=Cloth();var flag=Flag();var plate=Plate();var lit=Glass();
                // The yoke: an arched bar over her upper back, past each shoulder, hooked down at its ends.
                float reach=half+.36f*k,yokeY=shoulder+.07f*k,yokeZ=back-.05f*k;
                var yoke=new List<Vector3>();for(int i=0;i<=16;i++){float t=i/16f;float x=Mathf.Lerp(-reach,reach,t);yoke.Add(new Vector3(x,yokeY+.05f*k*(1-Mathf.Pow(2*t-1,2)),yokeZ-.04f*k*(1-Mathf.Pow(2*t-1,2))));}
                Part(root,"Yoke",Tube(yoke,.032f*k,8),iron,o);
                foreach(int side in new[]{-1,1})
                {
                    var end=new Vector3(side*reach,yokeY,yokeZ);
                    Part(root,"Yoke hook",Tube(new List<Vector3>{end,end+new Vector3(side*.03f,-.1f,0)*k,end+new Vector3(side*.02f,-.16f,0)*k},.018f*k,6),iron,o);
                    // A cage round the stolen glass, and the glass in it.
                    var cageTop=end+new Vector3(side*.02f,-.17f,0)*k;float cageTall=.36f*k,cageWide=.12f*k;
                    var cage=new List<Mesh>();
                    for(int n=0;n<4;n++){float a=n*Mathf.PI*.5f+Mathf.PI*.25f;var off=new Vector3(Mathf.Cos(a)*cageWide,0,Mathf.Sin(a)*cageWide);cage.Add(Tube(new List<Vector3>{cageTop+off*.6f,cageTop+off+Vector3.down*.06f*k,cageTop+off+Vector3.down*(cageTall-.06f*k),cageTop+off*.6f+Vector3.down*cageTall},.011f*k,5));}
                    foreach(float y in new[]{.06f*k,cageTall-.06f*k}){var ring=new List<Vector3>();for(int n=0;n<=12;n++){float a=n/12f*Mathf.PI*2;ring.Add(cageTop+new Vector3(Mathf.Cos(a)*cageWide,-y,Mathf.Sin(a)*cageWide));}cage.Add(Tube(ring,.012f*k,5));}
                    Part(root,"Lantern cage",Combine(cage),iron,o);
                    var glass=WorldBuilder.Piece(ZoneKit.Kit("LanternShard","CrystalClusterB"),Vector3.zero,Vector3.one*(cageTall*.78f),"Emission",false);glass.name="Stolen relay glass";
                    var box=ModelGeometry.BoundsOf(glass);glass.transform.position+=cageTop+Vector3.down*(cageTall*.5f)-box.center;glass.transform.SetParent(root.transform,true);
                    foreach(var c in glass.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
                    foreach(var r in glass.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=ShadowCastingMode.Off;r.sharedMaterial=lit;}
                }
                // The standard: a pole from the small of her back to well over her head, leaning a little back; a crossbar at its head and the banner hung from it, swallow-tailed.
                var foot=new Vector3(0,hip+.12f*k,back-.1f*k);var head=new Vector3(0,top+1.3f*f,back-.34f*f);
                Part(root,"Standard pole",Tube(new List<Vector3>{foot,head},.03f*k,8),iron,o);
                Part(root,"Standard finial",Tube(new List<Vector3>{head,head+new Vector3(0,.1f,-.01f)*f,head+new Vector3(0,.22f,-.02f)*f},.045f*k,6,.1f),iron,o);
                var bar=head+new Vector3(0,-.14f,-.01f)*f;
                Part(root,"Standard crossbar",Tube(new List<Vector3>{bar+Vector3.left*.4f*f,bar+Vector3.right*.4f*f},.02f*k,6),iron,o);
                var banner=Sheet(14,18,(u,v)=>
                {
                    // u across, v down; a swallowtail cut into its foot, and a slow ripple through it.
                    float x=Mathf.Lerp(-.37f,.37f,u)*f,notch=.22f*f*(1-Mathf.Abs(2*u-1)),drop=Mathf.Lerp(.02f,1.12f*f-notch,v);
                    return bar+new Vector3(x,-drop,(-.02f-.05f*Mathf.Sin(u*Mathf.PI*2.2f+v*1.4f)*v-.03f*v)*f);
                });
                Part(root,"Standard banner",banner,flag,o,true);
                // The mantle: over the shoulders and down the back to the hips, flaring as it falls, its hem ragged.
                float mantleTop=shoulder+.1f*k,mantleFoot=hip-.04f*k;
                var mantle=Sheet(16,12,(u,v)=>
                {
                    float y=Mathf.Lerp(mantleTop,mantleFoot,v),wide=Mathf.Lerp(half+.14f*k,half+.26f*k,v),x=Mathf.Lerp(-wide,wide,u),edge=2*u-1;
                    // Ragged: the hem's foot rises and falls across it.
                    if(v>.95f)y+=.07f*k*Mathf.Abs(Mathf.Sin(u*Mathf.PI*7.3f));
                    float bulge=(.12f*(1-edge*edge)+.05f*v)*k;
                    // At its top it wraps forward over the shoulders.
                    float wrap=Mathf.SmoothStep(0,1,1-v*4)*Mathf.Abs(edge)*.22f*k;
                    return new Vector3(x,y,back-.04f*k-bulge+wrap);
                });
                Part(root,"Mantle",mantle,cloth,o,true);
                foreach(int side in new[]{-1,1})
                {
                    // A plate over each shoulder, as broad as the yoke is long: it is what widens her from the front.
                    var shoulderAt=new Vector3(side*(half+.02f*k),shoulder+.06f*k,(back+chest.position.z)*.5f+.03f*k);
                    var cap=Sheet(10,8,(u,v)=>{float a=u*Mathf.PI*2,t=v*Mathf.PI*.5f;return shoulderAt+new Vector3(Mathf.Cos(a)*Mathf.Sin(t)*.2f,Mathf.Cos(t)*.13f,Mathf.Sin(a)*Mathf.Sin(t)*.19f)*k;});
                    Part(root,"Shoulder plate",cap,plate,o,true);
                }
                if(facing<0)root.transform.rotation=Quaternion.Euler(0,180,0);
                // It rides on her chest's place and turns with her whole body, not with the chest bone: a bone's own axes are the rig's business (turned with the chest bone, the first still had the gear on her front).
                var follow=root.AddComponent<HessaRegalia>();follow.anchor=chest;follow.basis=made.transform;follow.offset=chest.InverseTransformPoint(root.transform.position);follow.turn=Quaternion.Inverse(made.transform.rotation)*root.transform.rotation;
                // Last among the body's children: whatever looks for her body's own mesh finds it before any of this.
                root.transform.SetAsLastSibling();
                var all=ModelGeometry.BoundsOf(made);
                Debug.Log($"HESSA_REGALIA dressed {all.size.y:0.00} m tall with the standard (sized {k:0.00} times over)");
                var saved=PrefabUtility.SaveAsPrefabAsset(made,CaptainPath);
                return saved;
            }
            finally{Object.DestroyImmediate(made);}
        }

        // ---------------------------------------------------------------- what it is made of
        static Material Iron()
        {
            var map=ZoneKit.Texture("Lantern/hessa-iron",128,(u,v)=>{float g=.2f+.1f*ZoneKit.Cloud(u,v,4,3)+.05f*Mathf.Sin(v*120);return new Color(g*1.02f,g*.95f,g*.9f);});
            return ZoneKit.Toon("hessa-iron",map,Body);
        }
        static Material Cloth()
        {
            // Muster wool: a dark brown-grey weave, a red border band a hand above the hem, and the hem itself frayed dark.
            var map=ZoneKit.Texture("Lantern/hessa-mantle",256,(u,v)=>
            {
                float weave=.5f+.5f*Mathf.Sin(u*256*Mathf.PI)*Mathf.Sin(v*256*Mathf.PI),n=ZoneKit.Cloud(u,v,6,11);
                var wool=new Color(.24f,.2f,.19f)*(.8f+.25f*n+.08f*weave);
                float band=ZoneKit.Smooth(.8f,.82f,v)*(1-ZoneKit.Smooth(.88f,.9f,v));wool=Color.Lerp(wool,new Color(.46f,.08f,.08f)*(.85f+.3f*n),band);
                return Color.Lerp(wool,wool*.45f,ZoneKit.Smooth(.93f,1,v));
            },false);
            return ZoneKit.TwoSided(ZoneKit.Toon("hessa-mantle",map,Body));
        }
        static Material Flag()
        {
            // The Muster's standard: blood red, a black band at its head, and a pale ring with a hook through it, the mark on their crates.
            var map=ZoneKit.Texture("Lantern/hessa-standard",256,(u,v)=>
            {
                float n=ZoneKit.Cloud(u,v,5,23);var red=new Color(.6f,.07f,.08f)*(.8f+.3f*n);
                if(v<.14f)red=new Color(.06f,.05f,.05f);
                float cx=u-.5f,cy=v-.46f,r=Mathf.Sqrt(cx*cx+cy*cy);
                float ring=1-ZoneKit.Smooth(.012f,.03f,Mathf.Abs(r-.2f));
                float shank=(1-ZoneKit.Smooth(.02f,.04f,Mathf.Abs(cx)))*ZoneKit.Smooth(.18f,.2f,v)*(1-ZoneKit.Smooth(.66f,.68f,v));
                float hookR=Mathf.Sqrt((cx+.07f)*(cx+.07f)+(v-.66f)*(v-.66f)),hook=(1-ZoneKit.Smooth(.012f,.03f,Mathf.Abs(hookR-.07f)))*(v>.66f?1:0);
                float mark=Mathf.Max(ring,Mathf.Max(shank,hook));
                var pale=new Color(.86f,.8f,.68f);red=Color.Lerp(red,pale,mark*.9f);
                // Weathered toward the tails.
                return Color.Lerp(red,red*.55f,ZoneKit.Smooth(.75f,1,v)*.7f);
            },false);
            return ZoneKit.TwoSided(ZoneKit.Toon("hessa-standard",map,Body));
        }
        /// <summary>The relay's own glass, still burning: the kit shard's paint, lit through. The shard's glow map is dark, so in the kit's own material it was a dark blue stone in a cage.</summary>
        static Material Glass()
        {
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Generated/Models/LanternShard/LanternShard_Toon.mat");
            var glow=ZoneKit.Texture("Lantern/hessa-glass-glow",32,(u,v)=>{float n=ZoneKit.Cloud(u,v,2,5);return new Color(.7f+.3f*n,.7f+.3f*n,.7f+.3f*n);});
            return ZoneKit.Toon("hessa-stolen-glass",source!=null?source.GetTexture("_BaseMap") as Texture2D:null,m=>{m.SetColor("_BaseColor",new Color(1,.9f,.75f));m.SetTexture("_EmissionMap",glow);
                m.SetColor("_EmissionColor",new Color(1.5f,.82f,.3f));m.SetColor("_RimColor",new Color(1,.8f,.5f));m.SetFloat("_RimPower",3);m.SetFloat("_LightFloor",.5f);});
        }
        static Material Plate()
        {
            var map=ZoneKit.Texture("Lantern/hessa-plate",128,(u,v)=>
            {
                float g=.3f+.12f*ZoneKit.Cloud(u,v,4,31);var c=new Color(g*1.05f,g*.92f,g*.82f);
                float rivet=Mathf.Repeat(u*8,1),row=Mathf.Abs(v-.3f);if(row<.04f&&Mathf.Abs(rivet-.5f)<.12f)c*=1.5f;
                return c;
            });
            return ZoneKit.TwoSided(ZoneKit.Toon("hessa-plate",map,Body));
        }
        /// <summary>Lit as her body is: the same rim in shade and least light, so the gear and the woman under it read as one figure.</summary>
        static void Body(Material m){m.SetFloat("_RampSteps",3);m.SetColor("_RimColor",Rim);m.SetFloat("_RimUnlit",RimUnlit);m.SetFloat("_LightFloor",LightFloor);m.SetFloat("_RimPower",RimPower);m.SetColor("_ShadowTint",new Color(.42f,.38f,.5f));}

        // ---------------------------------------------------------------- shapes
        static void Part(GameObject root,string name,Mesh mesh,Material material,Vector3 origin,bool twoSided=false)
        {
            var at=mesh.vertices;for(int i=0;i<at.Length;i++)at[i]-=origin;mesh.vertices=at;mesh.RecalculateBounds();mesh.RecalculateNormals();
            mesh=ZoneKit.SaveMesh(mesh,Folder+"hessa-"+name.ToLowerInvariant().Replace(' ','-')+(Count(name)>0?"-"+Count(name):"")+".asset");used[name]=Count(name)+1;
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=material;view.shadowCastingMode=twoSided?ShadowCastingMode.TwoSided:ShadowCastingMode.On;
        }
        static readonly Dictionary<string,int> used=new();
        static int Count(string name)=>used.TryGetValue(name,out int n)?n:0;
        /// <summary>A tube along a path: its sides round the path, u around, v along. <paramref name="tip"/> narrows it to that share of its width at its end.</summary>
        static Mesh Tube(List<Vector3> path,float radius,int sides,float tip=1)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();float run=0;
            for(int i=0;i<path.Count;i++)
            {
                var along=(i<path.Count-1?path[i+1]-path[i]:path[i]-path[i-1]).normalized;if(i>0&&i<path.Count-1)along=(path[i+1]-path[i-1]).normalized;
                var side=Vector3.Cross(along,Mathf.Abs(along.y)<.9f?Vector3.up:Vector3.right).normalized;var up=Vector3.Cross(side,along).normalized;
                if(i>0)run+=Vector3.Distance(path[i],path[i-1]);float r=radius*Mathf.Lerp(1,tip,i/(float)(path.Count-1));
                for(int k=0;k<=sides;k++){float a=k/(float)sides*Mathf.PI*2;vertices.Add(path[i]+(side*Mathf.Cos(a)+up*Mathf.Sin(a))*r);uv.Add(new Vector2(k/(float)sides,run*2));}
                if(i==0)continue;
                int a0=(i-1)*(sides+1),b0=i*(sides+1);
                for(int k=0;k<sides;k++){triangles.AddRange(new[]{a0+k,b0+k,a0+k+1,a0+k+1,b0+k,b0+k+1});}
            }
            var mesh=new Mesh{name="Hessa tube"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }
        /// <summary>A grid laid by a function of (u, v), each 0 to 1: a cloth, a cap. Drawn from both sides.</summary>
        static Mesh Sheet(int nu,int nv,System.Func<float,float,Vector3> at)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int j=0;j<=nv;j++)for(int i=0;i<=nu;i++){float u=i/(float)nu,v=j/(float)nv;vertices.Add(at(u,v));uv.Add(new Vector2(u,1-v));}
            for(int j=0;j<nv;j++)for(int i=0;i<nu;i++){int a=j*(nu+1)+i,b=a+nu+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            var mesh=new Mesh{name="Hessa sheet"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }
        static Mesh Combine(List<Mesh> parts)
        {
            var combine=new CombineInstance[parts.Count];for(int i=0;i<parts.Count;i++)combine[i]=new CombineInstance{mesh=parts[i],transform=Matrix4x4.identity};
            var mesh=new Mesh{name="Hessa cage"};mesh.CombineMeshes(combine,true,true);foreach(var p in parts)Object.DestroyImmediate(p);return mesh;
        }
    }
}
