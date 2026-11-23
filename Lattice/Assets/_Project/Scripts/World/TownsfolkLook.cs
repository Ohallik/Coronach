using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// What an ordinary townsperson looks like to a player, and when two of them read as one figure twice (November 21, after the whole-game capture: four of one
    /// red-haired woman at the decks' meeting, five aproned smiths in a row in the foundry, one antennaed body in colour swaps across Driftreed). The bodies are a dozen
    /// species meshes, each repainted per named person on one UV layout, so "the same body" is the same mesh (its template), and what tells two of one template apart is:
    /// <list type="bullet">
    /// <item>their clothes: the painted texture over the body's clothes (everything the spine, shoulders, arms, hips and legs carry), compared texel by texel on the one
    /// layout once a tint over the whole body (read off the hands) is taken out, and different where a quarter of the clothes has changed colour;</item>
    /// <item>their head: the same over the head's texels (hair, fronds, a hood painted on), or something worn on the head ("Worn …");</item>
    /// <item>what they carry ("Carried …");</item>
    /// <item>their build: seven percent in height or ten in breadth.</item>
    /// </list>
    /// Two of one template are copies unless at least three of the four differ: a colour swap of one figure (clothes alone) is a copy, and so is the same figure with a
    /// crate. A tint (<see cref="PropTint"/>) is not a look. A body in a force's colours (<see cref="Uniforms"/>), or a squad at drill, is in uniform, and two in uniform
    /// are not compared (whoever stands beside one in the same body still is).
    /// Shared by the editor's townsfolk pass, which dresses every scene's people so that no two who can share a screen are copies, and the test that reads every zone's
    /// frames (TownsfolkLookTests), as SightlineAudit is by its builder and its test.
    /// </summary>
    public static class TownsfolkLook
    {
        public const string Model="Generated civilian";
        /// <summary>Bodies painted in a force's colours: the Muster's guard wing and its scavs, the Compact's wardens and its crews.</summary>
        public static readonly string[] Uniforms={"MusterPikeguard_base_color","MusterSergeant_base_color","MusterScav_base_color","CompactWarden_base_color","PatrolBody_base_color"};
        public const float ClothesChanged=.25f,HeadChanged=.25f,Taller=.07f,Broader=.1f;public const int Apart=3;
        const int Grid=64;const float TexelChanged=18;

        /// <summary>A squad at drill (Harrowgate's cadets marching in files in the yard and on the field) is in its kit: one squad, compared with nobody of the squad.</summary>
        public static bool Drilling(GameObject who)=>who.name.Contains(" at drill");
        /// <summary>Someone with no name who lives here: tinted, wearing a body, with no Npc on them and not a fighter.</summary>
        public static bool Resident(GameObject who)=>who!=null&&who.GetComponent<PropTint>()!=null&&who.transform.Find(Model)!=null&&who.GetComponent<Npc>()==null&&who.GetComponentInParent<CombatActor>(true)==null;

        public sealed class Look
        {
            public GameObject who;public SkinnedMeshRenderer body;public string template,dye,carried,worn;public float tall,broad;public bool uniform;
            public Texture texture;
            public override string ToString()=>$"{who.name} ({dye}{(carried.Length>0?", "+carried:"")}{(worn.Length>0?", "+worn:"")}, {tall:0.00}x{broad:0.00}) at ({who.transform.position.x:0.0}, {who.transform.position.z:0.0})";
        }
        static bool Piece(Transform t,Transform model){for(var at=t;at!=null&&at!=model;at=at.parent)if(at.name.StartsWith("Carried ")||at.name.StartsWith("Worn "))return true;return false;}
        /// <summary>The body a resident wears: the biggest skinned renderer under its model that isn't something carried or worn.</summary>
        public static SkinnedMeshRenderer Body(GameObject who)
        {
            var model=who.transform.Find(Model);if(model==null)return null;
            return model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.sharedMesh!=null&&!Piece(r.transform,model))
                .OrderByDescending(r=>Vector3.Scale(r.sharedMesh.bounds.size,r.transform.lossyScale).sqrMagnitude).FirstOrDefault();
        }
        /// <summary>A template is a mesh's shape: its vertex count and size (each body's own import is a mesh of its own, the same for every person repainted on it).</summary>
        public static string Template(Mesh mesh){var s=mesh.bounds.size;return $"{mesh.vertexCount}v {s.x:0.00}x{s.y:0.00}x{s.z:0.00}";}
        public static Texture TextureOf(Material material)=>material==null?null:material.HasProperty("_BaseMap")?material.GetTexture("_BaseMap"):material.mainTexture;
        static string Named(Transform root,string prefix)=>string.Join("+",root.GetComponentsInChildren<Transform>(true).Where(t=>t.gameObject.activeSelf&&t.name.StartsWith(prefix)).Select(t=>t.name.Substring(prefix.Length).Split(' ')[0]).OrderBy(n=>n));
        public static Look Read(GameObject who)
        {
            var body=Body(who);if(body==null)return null;var model=who.transform.Find(Model);var texture=TextureOf(body.sharedMaterial);
            return new Look{who=who,body=body,template=Template(body.sharedMesh),texture=texture,dye=texture!=null?texture.name:body.sharedMaterial!=null?body.sharedMaterial.name:"?",
                carried=Named(who.transform,"Carried "),worn=Named(who.transform,"Worn "),tall=model.lossyScale.y,broad=model.lossyScale.x,uniform=texture!=null&&System.Array.IndexOf(Uniforms,texture.name)>=0||Drilling(who)};
        }

        // ---------------------------------------------------------------- the painted colours, on the template's own layout
        /// <summary>Each texel of a template's layout: 0 nobody's, 1 head, 2 hands, 3 feet, 4 clothes, by the bone that carries most of the triangle it is in.</summary>
        static readonly Dictionary<Mesh,byte[]> regions=new();
        static readonly Dictionary<Texture,Vector3[]> colours=new();
        static readonly Dictionary<(Texture,Texture,Mesh),(float clothes,float head)> compared=new();
        static byte Part(string bone)
        {
            string b=bone.ToLowerInvariant();
            if(b.Contains("head")||b.Contains("neck"))return 1;
            if(b.EndsWith("hand"))return 2;
            if(b.Contains("foot")||b.Contains("toe"))return 3;
            return 4;
        }
        /// <summary>Where each template's map is kept for a run that can't read its mesh (a body's mesh isn't readable in play, even in the editor): written by the editor
        /// (<see cref="SaveRegions"/>), read by TownsfolkLookTests.</summary>
        public static string RegionFile(string template)=>System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../docs/art/townsfolk-regions",template.Replace(' ','_')+".bin"));
        static byte[] Regions(SkinnedMeshRenderer body)
        {
            var mesh=body.sharedMesh;if(regions.TryGetValue(mesh,out var known))return known;
            if(!mesh.isReadable&&Application.isPlaying)
            {
                string file=RegionFile(Template(mesh));var read=System.IO.File.Exists(file)?System.IO.File.ReadAllBytes(file):null;
                if(read==null||read.Length!=Grid*Grid){Debug.LogWarning("TOWNSFOLK_LOOK no region map for "+Template(mesh)+" ("+file+"): its clothes and head can't be compared");read=new byte[Grid*Grid];}
                regions[mesh]=read;return read;
            }
            var map=new byte[Grid*Grid];var uv=mesh.uv;var tris=mesh.triangles;var weights=mesh.boneWeights;var bones=body.bones;
            if(uv.Length==0||weights.Length==0||bones==null||bones.Length==0){regions[mesh]=map;return map;}
            byte Of(int v){var w=weights[v];int i=w.boneIndex0;return i>=0&&i<bones.Length&&bones[i]!=null?Part(bones[i].name):(byte)4;}
            for(int t=0;t+2<tris.Length;t+=3)
            {
                int a=tris[t],b=tris[t+1],c=tris[t+2];byte pa=Of(a),pb=Of(b),pc=Of(c);byte part=pa==pb||pa==pc?pa:pb==pc?pb:pa;
                Vector2 A=uv[a]*Grid,B=uv[b]*Grid,C=uv[c]*Grid;
                int x0=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(A.x,Mathf.Min(B.x,C.x)))),x1=Mathf.Min(Grid-1,Mathf.CeilToInt(Mathf.Max(A.x,Mathf.Max(B.x,C.x))));
                int y0=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(A.y,Mathf.Min(B.y,C.y)))),y1=Mathf.Min(Grid-1,Mathf.CeilToInt(Mathf.Max(A.y,Mathf.Max(B.y,C.y))));
                float den=(B.y-C.y)*(A.x-C.x)+(C.x-B.x)*(A.y-C.y);if(Mathf.Abs(den)<1e-8f)continue;
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                {
                    float px=x+.5f,py=y+.5f,w0=((B.y-C.y)*(px-C.x)+(C.x-B.x)*(py-C.y))/den,w1=((C.y-A.y)*(px-C.x)+(A.x-C.x)*(py-C.y))/den;
                    if(w0>=-.02f&&w1>=-.02f&&1-w0-w1>=-.02f)map[y*Grid+x]=part;
                }
            }
            regions[mesh]=map;return map;
        }
        /// <summary>Writes a template's map where a run in play reads it (editor only: the mesh is read here).</summary>
        public static void SaveRegions(SkinnedMeshRenderer body)
        {
            var map=Regions(body);string file=RegionFile(Template(body.sharedMesh));System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));System.IO.File.WriteAllBytes(file,map);
        }
        /// <summary>A texture's colours at the layout's grid, in Lab: in the editor each cell the mean of its block of the source file (a builder may run with no graphics
        /// device, and a compressed import has no readable copy), elsewhere read back through the GPU.</summary>
        static Vector3[] Colours(Texture texture)
        {
            if(texture==null)return null;if(colours.TryGetValue(texture,out var known))return known;
#if UNITY_EDITOR
            string path=UnityEditor.AssetDatabase.GetAssetPath(texture);
            if(!string.IsNullOrEmpty(path)&&System.IO.File.Exists(path))
            {
                var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
                if(source.LoadImage(System.IO.File.ReadAllBytes(path)))
                {
                    var all=source.GetPixels32();int w=source.width,h=source.height;var mean=new Vector3[Grid*Grid];
                    for(int gy=0;gy<Grid;gy++)for(int gx=0;gx<Grid;gx++)
                    {
                        int xa=gx*w/Grid,xb=(gx+1)*w/Grid,ya=gy*h/Grid,yb=(gy+1)*h/Grid;float r=0,g=0,b=0;int n=0;
                        for(int y=ya;y<yb;y+=2)for(int x=xa;x<xb;x+=2){var p=all[y*w+x];r+=p.r;g+=p.g;b+=p.b;n++;}
                        mean[gy*Grid+gx]=Lab(new Color32((byte)(r/n),(byte)(g/n),(byte)(b/n),255));
                    }
                    Object.DestroyImmediate(source);colours[texture]=mean;return mean;
                }
                Object.DestroyImmediate(source);
            }
#endif
            var target=RenderTexture.GetTemporary(Grid,Grid,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;
            Graphics.Blit(texture,target);RenderTexture.active=target;
            var read=new Texture2D(Grid,Grid,TextureFormat.RGBA32,false,false);read.ReadPixels(new Rect(0,0,Grid,Grid),0,0);read.Apply();
            RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
            var pixels=read.GetPixels32();Object.DestroyImmediate(read);
            var lab=new Vector3[pixels.Length];for(int i=0;i<pixels.Length;i++)lab[i]=Lab(pixels[i]);
            colours[texture]=lab;return lab;
        }
        static float Linear(float c)=>c<=.04045f?c/12.92f:Mathf.Pow((c+.055f)/1.055f,2.4f);
        static Vector3 Lab(Color32 c)
        {
            float r=Linear(c.r/255f),g=Linear(c.g/255f),b=Linear(c.b/255f);
            float x=(r*.4124f+g*.3576f+b*.1805f)/.95047f,y=r*.2126f+g*.7152f+b*.0722f,z=(r*.0193f+g*.1192f+b*.9505f)/1.08883f;
            float F(float t)=>t>.008856f?Mathf.Pow(t,1/3f):7.787f*t+16/116f;
            return new Vector3(116*F(y)-16,500*(F(x)-F(y)),200*(F(y)-F(z)));
        }
        /// <summary>The share of the clothes' texels and of the head's that are painted another colour in <paramref name="b"/> than in <paramref name="a"/>, on one template,
        /// once a tint over the whole body is taken out (the hands' median difference: a hand is the same skin or the same glove under every dye).</summary>
        public static (float clothes,float head) Changed(SkinnedMeshRenderer body,Texture a,Texture b)
        {
            if(a==b)return (0,0);if(a==null||b==null)return (1,1);
            var key=(a,b,body.sharedMesh);if(compared.TryGetValue(key,out var known))return known;
            var map=Regions(body);var ca=Colours(a);var cb=Colours(b);
            var hands=new List<Vector3>();for(int i=0;i<map.Length;i++)if(map[i]==2)hands.Add(cb[i]-ca[i]);
            Vector3 tint=hands.Count<8?Vector3.zero:new Vector3(Median(hands.Select(h=>h.x)),Median(hands.Select(h=>h.y)),Median(hands.Select(h=>h.z)));
            int clothes=0,clothesChanged=0,head=0,headChanged=0;
            for(int i=0;i<map.Length;i++)
            {
                if(map[i]!=1&&map[i]!=4)continue;bool changed=(cb[i]-ca[i]-tint).magnitude>TexelChanged;
                if(map[i]==4){clothes++;if(changed)clothesChanged++;}else{head++;if(changed)headChanged++;}
            }
            var result=(clothes==0?0:clothesChanged/(float)clothes,head==0?0:headChanged/(float)head);
            compared[key]=result;compared[(b,a,body.sharedMesh)]=result;return result;
        }
        static float Median(IEnumerable<float> values){var s=values.OrderBy(v=>v).ToArray();return s.Length==0?0:s[s.Length/2];}
        /// <summary>Forget what was read: a texture made or remade since (the townsfolk pass's dyes) is read afresh.</summary>
        public static void Forget(){regions.Clear();colours.Clear();compared.Clear();}

        // ---------------------------------------------------------------- two of them
        /// <summary>How many of the four (clothes, head, carried, build) differ between two of one template, and which.</summary>
        public static int Differences(Look a,Look b,out string which)
        {
            var (clothes,head)=Changed(a.body,a.texture,b.texture);var parts=new List<string>();
            if(clothes>=ClothesChanged)parts.Add("clothes");
            if(head>=HeadChanged||a.worn!=b.worn)parts.Add("head");
            if(a.carried!=b.carried)parts.Add("carried");
            // A hair under the line still counts: a scale of 1.07 read back through a transform is 1.0699999.
            if(Mathf.Abs(a.tall/b.tall-1)>=Taller-.002f||Mathf.Abs(a.broad/b.broad-1)>=Broader-.002f)parts.Add("build");
            which=parts.Count==0?"nothing":string.Join(", ",parts);return parts.Count;
        }
        /// <summary>One figure twice: one template, and fewer than <see cref="Apart"/> of the four differ. Uniforms are never copies of each other.</summary>
        public static bool Copies(Look a,Look b,out string which)
        {
            which="another template";if(a.template!=b.template)return false;
            if(a.uniform&&b.uniform){which="uniforms";return false;}
            return Differences(a,b,out which)<Apart;
        }
    }
}
