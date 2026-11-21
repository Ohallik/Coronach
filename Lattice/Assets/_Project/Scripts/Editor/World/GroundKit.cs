using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The ground kit: what makes a zone's floor look authored from a fixed three-quarter camera, where most of every frame is ground. One kit works on the open scene's drawn floor
    /// (any mesh: a Lantern Road floor, Sorrel's basin) and adds, under one <c>Ground kit</c> root that a rerun replaces:
    /// <list type="bullet">
    /// <item>material variety on the one terrain: up to three more tiling layers, painted as per-vertex weights (along paths, as noise-shaped patches, round points, by slope or height,
    /// at cliff feet) into the floor mesh's third uv set for LatticeToon's splat blend, plus painted shade (ruts, hollows, wear);</item>
    /// <item>a flat detail layer: cracks, loose slabs, pebbles, drifts, litter, lichen, scorch, stains, tracks, ruts, survey lines and lamp pools from one atlas, laid on the ground itself,
    /// batched per 12 m cell so each batch takes its own nearby lamps (D192), drawn before danger markings so nothing ever lies over one;</item>
    /// <item>low cover under 0.6 m with no colliders: plant and bone cards turned to the fixed camera, small rocks, each with a contact shadow; grass tufts are BiomePatches, so the
    /// licensed local grass can replace them and the public scene still carries ours (D131);</item>
    /// <item>puddles, and moving shade over the whole zone (GroundShade).</item>
    /// </list>
    /// Everything keeps off floor signs and prompts, off what stands on the ground and out of the lanes a builder declares, and is placed by a hash of where it is, so one kind's
    /// count never moves another's pieces and no grid shows. <see cref="Finish"/> checks the result and throws a named GROUND_PROBLEM rather than leave a fault in a saved scene.
    /// </summary>
    public sealed class GroundKit
    {
        public const string RootName="Ground kit",Art="Assets/_Project/Art/Generated/Ground/",WorldMaterials="Assets/_Project/Resources/WorldMaterials/";
        /// <summary>D131: nothing that grows or lies on open ground stands taller than this.</summary>
        public const float CoverLimit=.6f;
        /// <summary>Cover this tall or taller stays out of the middle of a fight (D131's combat-lane rule); lower than this it's ground texture a marking shows through.</summary>
        public const float TallCover=.32f;
        const float Cell=12,Lift=.025f,EdgeCell=.5f;
        public static readonly string[] DecalKinds={"CracksA","CracksB","PebblesA","PebblesB","Slabs","Flagstones","SandDrift","Litter","Scorch","Stain","Lichen","Tracks","Ruts","SurveyLine","Shadow","LampPool"};
        public static readonly string[] CoverKinds={"TuftA","TuftB","TuftC","Fern","Flowers","Reeds","Succulent","Twigs","FungusA","FungusB","BonesA","BonesB","Sprouts","Shards"};
        /// <summary>How much of its square card each cover sprite stands up in, so a card is sized by how tall the plant on it is.</summary>
        static readonly float[] CoverFills={.55f,.88f,.4f,.45f,.62f,.93f,.47f,.68f,.4f,.56f,.5f,.3f,.3f,.5f};
        static readonly HashSet<string> Grass=new(){"TuftA","TuftB","TuftC","Sprouts"};

        public struct Layer
        {
            public string texture;public Color tint;public float metres;
            /// <summary>A tiling texture from Art/Generated/Ground (its name without "ground-"), its tint (alpha: how solid it lies) and how many metres one repeat covers.</summary>
            public Layer(string texture,Color tint,float metres){this.texture=texture;this.tint=tint;this.metres=metres;}
        }
        sealed class Item{public Vector3[] v;public Vector2[] uv,uv2;public Color32[] c;public int[] t;public Vector3[] n;public Vector2 at;public Vector4 sprout;}

        readonly string zone;readonly MeshFilter floor;readonly Mesh mesh;readonly Transform root;readonly MeshCollider probe;readonly int seed;readonly float maxFloor,top,bottom;
        readonly Vector3[] points;readonly Vector4[] paint;readonly float[] open;
        readonly List<Rect> signs=new(),solids=new(),arenas=new();readonly List<Vector2> prompts=new(),lamps=new();readonly List<Vector3> people=new();
        readonly List<(Vector2[] path,float half)> lanes=new();
        readonly Dictionary<(string kind,int x,int z),List<Item>> batches=new();
        readonly List<(Rect rect,string kind,bool strong)> laid=new();
        readonly List<(Vector3 at,float height,string kind)> standing=new();
        readonly List<string> problems=new();
        Layer[] layers=Array.Empty<Layer>();Vector4 splatEdge=new(.04f,.3f,.35f,.3f);float edgeNoiseMetres=1.1f;
        string palette="Canyon";Texture2D rockTexture;
        float[] edge;int edgeX,edgeZ;Vector2 edgeOrigin;
        readonly Vector3 right,away;

        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);

        // ---------------------------------------------------------------- opening a zone's floor
        /// <summary>The drawn floor of a Lantern Road style landform (the one mesh named "... floor"), or null.</summary>
        public static MeshFilter FindFloor()=>All<MeshFilter>().FirstOrDefault(f=>f.name.EndsWith(" floor")&&f.sharedMesh!=null);

        /// <summary>Opens the kit on the open scene. <paramref name="floor"/> is the mesh the ground is drawn with; floor above <paramref name="maxFloor"/> is left alone (a rim, a cliff top).
        /// <paramref name="wholeLandform"/>: the mesh is a whole landform with its slopes and rims in it (Sorrel's basin), not a floor cut out on its own (GroundTerrain's).
        /// <paramref name="isFloor"/>: for a landform whose floor isn't simply its low level ground (a cave open to the camera, whose rock tops are level too), what counts as floor at a point of its surface.</summary>
        public static GroundKit Open(string zone,MeshFilter floor,int seed,float maxFloor=float.PositiveInfinity,bool wholeLandform=false,Func<Vector3,bool> isFloor=null)=>new GroundKit(zone,floor,seed,maxFloor,wholeLandform,isFloor);
        readonly Func<Vector3,bool> isFloor;
        GroundKit(string zone,MeshFilter floor,int seed,float maxFloor,bool wholeLandform,Func<Vector3,bool> isFloor)
        {
            this.isFloor=isFloor;
            this.zone=zone;this.floor=floor??throw new InvalidOperationException("GROUND_PROBLEM "+zone+": no floor mesh to work on");mesh=floor.sharedMesh;this.seed=seed;this.maxFloor=maxFloor;
            foreach(var old in All<Transform>().Where(t=>t!=null&&t.parent==null&&t.name==RootName).Select(t=>t.gameObject).ToArray())Object.DestroyImmediate(old);
            root=new GameObject(RootName).transform;
            var to=floor.transform.localToWorldMatrix;points=mesh.vertices;for(int i=0;i<points.Length;i++)points[i]=to.MultiplyPoint3x4(points[i]);
            paint=new Vector4[points.Length];
            // A whole landform's mesh (Sorrel's basin) has slopes and rims in it: layers are painted on its floor only, fading out where it steepens.
            var normals=mesh.normals;open=new float[points.Length];
            for(int i=0;i<points.Length;i++)open[i]=!wholeLandform?1:points[i].y>maxFloor||isFloor!=null&&!isFloor(points[i])?0:Smooth(.8f,.92f,to.MultiplyVector(normals[i]).normalized.y);
            var bounds=floor.GetComponent<Renderer>().bounds;top=bounds.max.y+5;bottom=bounds.min.y-5;
            var holder=new GameObject("Ground probe"){hideFlags=HideFlags.HideAndDontSave};holder.transform.SetPositionAndRotation(floor.transform.position,floor.transform.rotation);holder.transform.localScale=floor.transform.lossyScale;
            probe=holder.AddComponent<MeshCollider>();probe.sharedMesh=mesh;Physics.SyncTransforms();
            // Cards are turned once to the fixed ground camera, which never turns: one quad each, always seen whole.
            float yaw=SightlineAudit.Yaw*Mathf.Deg2Rad;right=new Vector3(Mathf.Cos(yaw),0,-Mathf.Sin(yaw));away=new Vector3(Mathf.Sin(yaw),0,Mathf.Cos(yaw));
            Survey();Edges();
        }

        void Survey()
        {
            foreach(var sign in All<TextMeshPro>())
            {
                if(!sign.name.StartsWith("Sign_")||sign.GetComponentInParent<Npc>(true)!=null)continue;
                var at=sign.transform.position;float half=sign.text.Length*.2f+.5f;
                var plate=sign.transform.Find("Sign plate");if(plate!=null&&plate.TryGetComponent<Renderer>(out var drawn))half=Mathf.Max(half,drawn.bounds.extents.x);
                // What a sign covers once it's sized for the camera, and the ground on the camera's side of it, where anything standing would stand across its words.
                signs.Add(Rect.MinMaxRect(at.x-half-1,at.z-2.6f,at.x+half+1,at.z+1.6f));
            }
            foreach(var prompt in All<InteractionPrompt>())prompts.Add(new Vector2(prompt.transform.position.x,prompt.transform.position.z));
            foreach(var spawn in All<SpawnPoint>())prompts.Add(new Vector2(spawn.transform.position.x,spawn.transform.position.z));
            foreach(var npc in All<Npc>())people.Add(npc.transform.position);
            foreach(var body in All<SkinnedMeshRenderer>()){var at=body.transform.root.position;if(!people.Any(q=>Vector3.Distance(q,at)<.5f))people.Add(at);}
            foreach(var fight in All<EncounterVolume>()){var box=fight.GetComponent<Collider>().bounds;arenas.Add(Rect.MinMaxRect(box.min.x,box.min.z,box.max.x,box.max.z));}
            foreach(var light in All<Light>())if(light.type==LightType.Point&&light.name=="Lamp light"&&light.gameObject.activeInHierarchy)lamps.Add(new Vector2(light.transform.position.x,light.transform.position.z));
            foreach(var part in All<Renderer>())
            {
                if(!(part is MeshRenderer||part is SkinnedMeshRenderer)||!part.enabled||!part.gameObject.activeInHierarchy||part.transform==floor.transform||part.transform.IsChildOf(root))continue;
                if(part.GetComponentInParent<TMP_Text>()!=null||part.GetComponent<BiomePatch>()!=null||part.name.StartsWith("Floor detail")||part.name.StartsWith("Lit chunk")||part.name.EndsWith(" floor")||part.name.EndsWith(" cliffs")||part.name.EndsWith(" shadow"))continue;
                var box=part.bounds;if(Mathf.Max(box.size.x,box.size.z)>40||box.size.y<.12f)continue;
                solids.Add(Rect.MinMaxRect(box.min.x-.12f,box.min.z-.12f,box.max.x+.12f,box.max.z+.12f));
            }
            Debug.Log($"GROUND_SURVEY {zone} floor={points.Length}v signs={signs.Count} prompts={prompts.Count} people={people.Count} solids={solids.Count} fights={arenas.Count} lamps={lamps.Count}");
        }

        /// <summary>How far every half metre of floor is from where the floor ends (a cliff foot, a ledge, the rim).</summary>
        void Edges()
        {
            var b=floor.GetComponent<Renderer>().bounds;edgeOrigin=new Vector2(b.min.x-EdgeCell,b.min.z-EdgeCell);
            edgeX=Mathf.CeilToInt(b.size.x/EdgeCell)+3;edgeZ=Mathf.CeilToInt(b.size.z/EdgeCell)+3;edge=new float[edgeX*edgeZ];
            const float Far=1e4f;
            for(int z=0;z<edgeZ;z++)for(int x=0;x<edgeX;x++)edge[z*edgeX+x]=Floor(edgeOrigin+new Vector2(x,z)*EdgeCell,out _)?Far:0;
            float straight=EdgeCell,diagonal=EdgeCell*1.4142f;
            void Relax(int i,int j,float d){if(edge[j]+d<edge[i])edge[i]=edge[j]+d;}
            for(int z=1;z<edgeZ-1;z++)for(int x=1;x<edgeX-1;x++){int i=z*edgeX+x;Relax(i,i-1,straight);Relax(i,i-edgeX,straight);Relax(i,i-edgeX-1,diagonal);Relax(i,i-edgeX+1,diagonal);}
            for(int z=edgeZ-2;z>0;z--)for(int x=edgeX-2;x>0;x--){int i=z*edgeX+x;Relax(i,i+1,straight);Relax(i,i+edgeX,straight);Relax(i,i+edgeX+1,diagonal);Relax(i,i+edgeX-1,diagonal);}
        }

        // ---------------------------------------------------------------- what a recipe can ask about a spot
        /// <summary>The floor's height under a point, if there is floor there to lie on.</summary>
        public bool Floor(Vector2 p,out float y)
        {
            if(probe.Raycast(new Ray(new Vector3(p.x,top,p.y),Vector3.down),out var hit,top-bottom)&&hit.normal.y>=.7f&&hit.point.y<=maxFloor&&(isFloor==null||isFloor(hit.point))){y=hit.point.y;return true;}
            y=0;return false;
        }
        /// <summary>Metres to where the floor ends: a cliff foot, a ledge, the rim. 0 off the floor.</summary>
        public float Edge(Vector2 p)
        {
            var q=(p-edgeOrigin)/EdgeCell;int x=Mathf.FloorToInt(q.x),z=Mathf.FloorToInt(q.y);if(x<0||z<0||x>=edgeX-1||z>=edgeZ-1)return 0;
            float fx=q.x-x,fz=q.y-z;int i=z*edgeX+x;
            return Mathf.Lerp(Mathf.Lerp(edge[i],edge[i+1],fx),Mathf.Lerp(edge[i+edgeX],edge[i+edgeX+1],fx),fz);
        }
        /// <summary>Two octaves of noise about <paramref name="metres"/> across, 0 to 1, different for every salt and zone seed.</summary>
        public float Noise(Vector2 p,float metres,int salt=0)
        {
            float ox=(seed*37+salt*101)%977*.731f,oz=(seed*53+salt*211)%991*.577f;
            return Mathf.Clamp01(Mathf.PerlinNoise(p.x/metres+ox,p.y/metres+oz)*.65f+Mathf.PerlinNoise(p.x/metres*2.3f+oz,p.y/metres*2.3f+ox)*.35f);
        }
        public static float Smooth(float from,float to,float x)=>Mathf.SmoothStep(0,1,Mathf.Clamp01((x-from)/(to-from)));
        public static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));return Vector2.Distance(p,a+d*t);}
        public static float Distance(Vector2 p,Vector2[] path){float d=float.PositiveInfinity;for(int i=1;i<path.Length;i++)d=Mathf.Min(d,Segment(p,path[i-1],path[i]));return d;}
        /// <summary>Metres outside the nearest declared lane (negative inside one).</summary>
        public float Lane(Vector2 p){float d=float.PositiveInfinity;foreach(var (path,half) in lanes)d=Mathf.Min(d,Distance(p,path)-half);return d;}
        /// <summary>Metres to the nearest of the zone's standing things (props, homes, rocks), by their drawn boxes.</summary>
        public float Solid(Vector2 p){float d=float.PositiveInfinity;foreach(var r in solids)d=Mathf.Min(d,Mathf.Max(Mathf.Max(r.xMin-p.x,p.x-r.xMax),Mathf.Max(r.yMin-p.y,p.y-r.yMax)));return d;}
        public bool InAFight(Vector2 p,float inset=0)=>arenas.Any(a=>p.x>a.xMin+inset&&p.x<a.xMax-inset&&p.y>a.yMin+inset&&p.y<a.yMax-inset);
        public IReadOnlyList<Vector2> Lamps=>lamps;
        /// <summary>Where the zone's own things stand, by the start of their name (root objects that are switched on): its stalls, its homes, its fallen towers.</summary>
        public static List<Vector2> Named(params string[] names)=>All<Transform>().Where(t=>t.parent==null&&t.gameObject.activeInHierarchy&&names.Any(n=>t.name.StartsWith(n)))
            .Select(t=>{var parts=t.GetComponentsInChildren<Renderer>();var at=parts.Length>0?parts[0].bounds.center:t.position;return new Vector2(at.x,at.z);}).ToList();
        public static float Nearest(Vector2 p,List<Vector2> of){float d=float.PositiveInfinity;foreach(var q in of)d=Mathf.Min(d,Vector2.Distance(p,q));return d;}
        public IReadOnlyList<Vector3> People=>people;
        static int Stable(string text){int h=17;foreach(char c in text)h=unchecked(h*31+c);return Mathf.Abs(h%1000);}
        static float Hash(int x,int z,int salt){unchecked{uint h=(uint)x*374761393u+(uint)z*668265263u+(uint)salt*2246822519u;h=(h^(h>>13))*1274126177u;h^=h>>16;return (h&0xFFFFFF)/16777216f;}}

        List<MeshFilter> pieces;Material piecesPlain;
        /// <summary>For a landform that isn't drawn by the mesh the kit paints but by pieces cut from it (Hushwell's lit chunks, each lit by its own lamps): every piece takes the
        /// weights of the points it was cut from, and the zone's layered material wherever it wore <paramref name="plain"/>.</summary>
        public GroundKit DrawnAs(IEnumerable<MeshFilter> cut,Material plain){pieces=cut.Where(c=>c!=null&&c.sharedMesh!=null).ToList();piecesPlain=plain;return this;}
        static Vector3Int Spot(Vector3 p)=>new(Mathf.RoundToInt(p.x*50),Mathf.RoundToInt(p.y*50),Mathf.RoundToInt(p.z*50));
        /// <summary>A way that must stay legible and clear: nothing stands in it, and only quiet marks lie on it.</summary>
        public GroundKit ClearLane(Vector2[] path,float half){lanes.Add((path,half));return this;}

        // ---------------------------------------------------------------- 1. material variety: layers and their weights
        /// <summary>Up to three layers over the floor's own texture, in the order they lie (the last is on top). <paramref name="edge"/>: half width of the cut, noise amount,
        /// how far a layer's own height moves its edge, and the dark line under it (LatticeToon's _SplatEdge).</summary>
        public GroundKit Layers(Vector4? edge,float edgeNoiseMetres,params Layer[] over)
        {
            if(over.Length>3)throw new InvalidOperationException("GROUND_PROBLEM "+zone+": a floor takes three layers over its own, not "+over.Length);
            layers=over;if(edge is Vector4 e)splatEdge=e;this.edgeNoiseMetres=edgeNoiseMetres;return this;
        }
        /// <summary>Paints a layer where <paramref name="weight"/> says, 0 to 1 at each floor point; the strongest of what's painted stays. The shader cuts each layer at half weight,
        /// so a weight that falls from 1 to 0 over a metre gives an edge half way, ragged by about a third of that metre.</summary>
        public GroundKit Paint(int layer,Func<Vector2,float> weight)
        {
            for(int i=0;i<points.Length;i++){if(open[i]<=0)continue;float w=Mathf.Clamp01(weight(new Vector2(points[i].x,points[i].z)))*open[i];if(w>paint[i][layer])paint[i][layer]=w;}
            return this;
        }
        /// <summary>Takes a layer away where <paramref name="amount"/> says (under a building, across a doorway).</summary>
        public GroundKit Erase(int layer,Func<Vector2,float> amount)
        {
            for(int i=0;i<points.Length;i++)paint[i][layer]*=1-Mathf.Clamp01(amount(new Vector2(points[i].x,points[i].z)));
            return this;
        }
        /// <summary>A path: the layer lies <paramref name="half"/> metres either side of the line. <paramref name="wear"/> under 1 thins it; <paramref name="broken"/> lets the ground
        /// under it show through in noise-shaped gaps (0 none, 1 mostly gaps), so an old road is whole in places and gone in others.</summary>
        public GroundKit PaintPath(int layer,Vector2[] path,float half,float wear=1,float broken=0,float feather=.6f,int salt=0)=>
            Paint(layer,p=>{float along=1-Smooth(half-feather,half+feather,Distance(p,path));return along<=0?0:along*wear*(1-broken*Smooth(.42f,.62f,Noise(p,5.5f,salt+3)));});
        /// <summary>Noise-shaped patches about <paramref name="metres"/> across over <paramref name="cover"/> of the ground (0 to 1), where <paramref name="where"/> (0 to 1) allows.</summary>
        public GroundKit PaintPatches(int layer,float metres,float cover,Func<Vector2,float> where=null,int salt=0)=>
            Paint(layer,p=>{float allowed=where==null?1:where(p);if(allowed<=0)return 0;float cut=Mathf.Lerp(.78f,.22f,cover);return Smooth(cut-.07f,cut+.07f,Noise(p,metres,salt+11))*allowed;});
        /// <summary>Wear round a point: a door, a stall, a camp fire. Ragged, never a disc.</summary>
        public GroundKit PaintAround(int layer,Vector2 at,float radius,float strength=1,int salt=0)=>
            Paint(layer,p=>{float d=Vector2.Distance(p,at);return d>radius*1.5f?0:strength*(1-Smooth(radius*.55f,radius*1.25f,d+(Noise(p,2.2f,salt+17)-.5f)*radius*.7f));});
        /// <summary>A layer along the foot of the cliffs, out to <paramref name="reach"/> metres from where the floor ends: drifted sand, fallen scree.</summary>
        public GroundKit PaintFoot(int layer,float reach,float broken=.5f,int salt=0)=>
            Paint(layer,p=>{float d=Edge(p);return d>reach*1.6f?0:(1-Smooth(reach*.5f,reach*1.4f,d+(Noise(p,4,salt+23)-.5f)*reach*1.2f))*(1-broken*Smooth(.5f,.68f,Noise(p,9,salt+29)));});
        /// <summary>A layer along another's edge, where the shader cuts <paramref name="of"/> (its weight near half), in runs broken by noise: a worn patch that ends in grit and
        /// rubble, not on a line. Paint <paramref name="of"/> first.</summary>
        public GroundKit Fringe(int layer,int of,float width=.25f,float broken=.45f,Func<Vector2,float> where=null,int salt=0)
        {
            for(int i=0;i<points.Length;i++)
            {
                if(open[i]<=0)continue;float near=1-Smooth(width*.5f,width,Mathf.Abs(paint[i][of]-.5f));if(near<=0)continue;
                var p=new Vector2(points[i].x,points[i].z);float w=near*open[i]*(where==null?1:Mathf.Clamp01(where(p)))*(1-broken*Smooth(.45f,.6f,Noise(p,2.4f,salt+41)));
                if(w>paint[i][layer])paint[i][layer]=w;
            }
            return this;
        }
        /// <summary>A layer between two heights of ground (a terrace, a wash's bed).</summary>
        public GroundKit PaintByHeight(int layer,float from,float to,float feather=.3f)
        {
            for(int i=0;i<points.Length;i++){float w=Smooth(from-feather,from+feather,points[i].y)*(1-Smooth(to-feather,to+feather,points[i].y))*open[i];if(w>paint[i][layer])paint[i][layer]=w;}
            return this;
        }
        /// <summary>A layer where the ground is this steep (0 level, 1 a wall): scree on a bank.</summary>
        public GroundKit PaintBySlope(int layer,float from,float to)
        {
            var normals=mesh.normals;var to3=floor.transform.localToWorldMatrix;
            for(int i=0;i<points.Length;i++){float w=Smooth(from,to,1-to3.MultiplyVector(normals[i]).normalized.y);if(w>paint[i][layer])paint[i][layer]=w;}
            return this;
        }
        /// <summary>Painted shade under every layer: positive darkens (a rut, a hollow, trodden ground), negative lifts (a crown, a scuffed pale patch). Adds to what's there.</summary>
        public GroundKit Shade(Func<Vector2,float> dark)
        {
            for(int i=0;i<points.Length;i++)if(open[i]>0)paint[i].w=Mathf.Clamp(paint[i].w+dark(new Vector2(points[i].x,points[i].z))*open[i],-.35f,.6f);
            return this;
        }
        /// <summary>What a relief function did to the ground, painted so it reads under a toon ramp that lights gentle ground as level: hollows darker, crowns paler.</summary>
        public GroundKit ShadeRelief(Func<Vector2,float> relief,float perMetre=2.2f)=>Shade(p=>Mathf.Clamp(-relief(p)*perMetre,-.12f,.3f));

        // ---------------------------------------------------------------- 2. the flat detail layer
        static Rect Aabb(Vector2 at,float w,float l,float yaw)
        {
            float c=Mathf.Abs(Mathf.Cos(yaw*Mathf.Deg2Rad)),s=Mathf.Abs(Mathf.Sin(yaw*Mathf.Deg2Rad));float hx=(w*c+l*s)*.5f,hz=(w*s+l*c)*.5f;
            return Rect.MinMaxRect(at.x-hx,at.y-hz,at.x+hx,at.y+hz);
        }
        static Rect Shrunk(Rect r,float by){float dx=r.width*by*.5f,dz=r.height*by*.5f;return Rect.MinMaxRect(r.xMin+dx,r.yMin+dz,r.xMax-dx,r.yMax-dz);}
        static Vector2 AtlasUv(int cell,float u,float v)
        {
            const float Inset=.006f;int column=cell%4,row=cell/4;
            return new Vector2((column+Mathf.Lerp(Inset,1-Inset,u))/4,(3-row+Mathf.Lerp(Inset,1-Inset,v))/4);
        }
        bool ClearFor(Rect rect,bool strong)
        {
            var inner=Shrunk(rect,.3f);
            if(signs.Any(s=>s.Overlaps(rect))||solids.Any(s=>s.Overlaps(inner)))return false;
            if(!strong)return true;
            var middle=rect.center;if(prompts.Any(p=>Vector2.Distance(p,middle)<1.2f+rect.width*.25f))return false;
            return !laid.Any(l=>l.strong&&l.rect.Overlaps(inner));
        }
        void Add(string kind,Item item){var key=(kind,Mathf.FloorToInt(item.at.x/Cell),Mathf.FloorToInt(item.at.y/Cell));if(!batches.TryGetValue(key,out var list))batches[key]=list=new List<Item>();list.Add(item);}
        /// <summary>A flat piece laid on the ground itself, point by point, or nothing if any of it would hang off the floor or lie across a step.</summary>
        Item Lay(int cell,Vector2 at,float w,float l,float yaw,Color colour,float lift,float uFrom=0,float uTo=1)
        {
            Color32 tint=Baked(colour);
            int nx=Mathf.Max(1,Mathf.CeilToInt(w/.7f)),nz=Mathf.Max(1,Mathf.CeilToInt(l/.7f));var turn=Quaternion.Euler(0,yaw,0);
            var v=new Vector3[(nx+1)*(nz+1)];var uv=new Vector2[v.Length];var c=new Color32[v.Length];float low=float.MaxValue,high=float.MinValue;
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++)
            {
                var local=turn*new Vector3(-w*.5f+w*i/nx,0,-l*.5f+l*j/nz);var p=new Vector2(at.x+local.x,at.y+local.z);
                if(!Floor(p,out float y))return null;
                int k=j*(nx+1)+i;v[k]=new Vector3(p.x,y+lift,p.y);uv[k]=AtlasUv(cell,Mathf.Lerp(uFrom,uTo,i/(float)nx),j/(float)nz);c[k]=tint;low=Mathf.Min(low,y);high=Mathf.Max(high,y);
            }
            if(high-low>Mathf.Max(w,l)*.35f)return null;
            var t=new int[nx*nz*6];int ti=0;
            for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){int a=j*(nx+1)+i,b=a+1,d=a+nx+1,e=d+1;t[ti++]=a;t[ti++]=d;t[ti++]=b;t[ti++]=d;t[ti++]=e;t[ti++]=b;}
            return new Item{v=v,uv=uv,c=c,t=t,at=at};
        }
        /// <summary>A tint as a vertex carries it. Vertex colour reaches the shader as it's stored, with none of the conversion a material's colour gets, so a tint written
        /// as it's meant to look has to be made linear here or every piece comes out pale and chalky.</summary>
        static Color32 Baked(Color tint){var linear=tint.linear;return new Color(Mathf.Clamp01(linear.r),Mathf.Clamp01(linear.g),Mathf.Clamp01(linear.b),tint.a);}
        static Color Vary(Color tint,float by,float roll){float k=1+(roll-.5f)*2*by;return new Color(tint.r*k,tint.g*k,tint.b*k,tint.a);}

        /// <summary>One flat detail from the atlas, <paramref name="size"/> metres across. A strong one (a crack, a slab, pebbles) keeps off prompts and off other strong ones; a quiet one
        /// (a stain, a drift) only keeps off signs and what stands. False if it couldn't lie there.</summary>
        public bool Decal(string kind,Vector2 at,float size,float yaw,Color tint,bool strong=true,float aspect=1)
        {
            int cell=Array.IndexOf(DecalKinds,kind);if(cell<0)throw new InvalidOperationException("GROUND_PROBLEM "+zone+": no flat detail called "+kind);
            var rect=Aabb(at,size,size*aspect,yaw);if(!ClearFor(rect,strong))return false;
            var item=Lay(cell,at,size,size*aspect,yaw,tint,Lift+(strong?.004f:0));if(item==null)return false;
            Add("decal",item);laid.Add((rect,kind,strong));return true;
        }
        /// <summary>Calls <paramref name="each"/> at seeded, jittered spots, about <paramref name="per100"/> to every 100 square metres where <paramref name="where"/> is 1, fewer where
        /// it's less. A spot depends only on where it is and the salt, so changing one recipe line moves nothing else.</summary>
        public void Sow(float per100,Func<Vector2,float> where,int salt,Action<Vector2,Vector4> each)
        {
            float step=Mathf.Sqrt(100/Mathf.Max(.01f,per100));var b=floor.GetComponent<Renderer>().bounds;int mix=seed*31+salt*7;
            for(int z=Mathf.FloorToInt(b.min.z/step);z<=Mathf.FloorToInt(b.max.z/step);z++)for(int x=Mathf.FloorToInt(b.min.x/step);x<=Mathf.FloorToInt(b.max.x/step);x++)
            {
                var p=new Vector2((x+Hash(x,z,mix+1))*step,(z+Hash(x,z,mix+2))*step);
                float chance=where==null?1:where(p);if(chance<=0||Hash(x,z,mix+3)>=chance)continue;
                each(p,new Vector4(Hash(x,z,mix+4),Hash(x,z,mix+5),Hash(x,z,mix+6),Hash(x,z,mix+7)));
            }
        }
        /// <summary>Scatters flat details of the given kinds. Returns how many lay down.</summary>
        public int Decals(string[] kinds,float per100,Func<Vector2,float> where,float smallest,float largest,Color tint,bool strong=true,float vary=.08f,int salt=0)
        {
            int placed=0;
            Sow(per100,where,salt+Stable(kinds[0]),(p,roll)=>{if(Decal(kinds[Mathf.Min(kinds.Length-1,(int)(roll.x*kinds.Length))],p,Mathf.Lerp(smallest,largest,roll.y),roll.w*360,Vary(tint,vary,roll.z),strong))placed++;});
            Debug.Log($"GROUND_DECALS {zone} {string.Join("/",kinds)} placed={placed}");return placed;
        }
        /// <summary>A tiling strip from the atlas along a path (Ruts, SurveyLine, Tracks), <paramref name="offset"/> metres to its left, fading out at each end of every run and
        /// stopping for signs and whatever stands across it.</summary>
        public void Strip(string kind,Vector2[] path,float offset,float width,Color tint,float from=0,float to=float.PositiveInfinity,float repeat=0)
        {
            int cell=Array.IndexOf(DecalKinds,kind);if(cell<0)throw new InvalidOperationException("GROUND_PROBLEM "+zone+": no strip called "+kind);
            if(repeat<=0)repeat=width;int perRepeat=Mathf.Max(2,Mathf.RoundToInt(repeat/.55f));float step=repeat/perRepeat,along=0;
            var rows=new List<(Vector3 l,Vector3 m,Vector3 r,float along,int index)>();int index=0;
            for(int i=1;i<path.Length;i++)
            {
                Vector2 a=path[i-1],d=path[i]-a;float length=d.magnitude;var n=new Vector2(-d.y,d.x)/length;
                for(float s=0;s<length;s+=step,along+=step,index++)
                {
                    if(along<from||along>to)continue;
                    var c=a+d*(s/length)+n*offset;Vector2 l=c+n*width*.5f,r=c-n*width*.5f;
                    if(signs.Any(q=>q.Contains(c))||solids.Any(q=>q.Contains(c))||!Floor(l,out float yl)||!Floor(c,out float ym)||!Floor(r,out float yr)||Mathf.Max(yl,Mathf.Max(ym,yr))-Mathf.Min(yl,Mathf.Min(ym,yr))>.4f)continue;
                    rows.Add((new Vector3(l.x,yl+Lift,l.y),new Vector3(c.x,ym+Lift,c.y),new Vector3(r.x,yr+Lift,r.y),along,index));
                }
            }
            for(int s=0;s<rows.Count;)
            {
                int e=s;while(e+1<rows.Count&&rows[e+1].index==rows[e].index+1)e++;
                float start=rows[s].along,end=rows[e].along,fade=Mathf.Min(2.5f,(end-start)*.3f);
                byte Alpha(int k)=>(byte)Mathf.RoundToInt(255*tint.a*(fade<=0?1:Smooth(0,fade,rows[k].along-start)*Smooth(0,fade,end-rows[k].along)));
                for(int k=s;k<e;k++)
                {
                    var (l0,m0,r0,_,i0)=rows[k];var (l1,m1,r1,_,_)=rows[k+1];float v0=(i0%perRepeat)/(float)perRepeat,v1=v0+1f/perRepeat;
                    Color32 c0=Baked(tint),c1=c0;c0.a=Alpha(k);c1.a=Alpha(k+1);
                    Add("decal",new Item{v=new[]{l0,m0,r0,l1,m1,r1},c=new[]{c0,c0,c0,c1,c1,c1},t=new[]{0,3,1,3,4,1,1,4,2,4,5,2},at=new Vector2(m0.x,m0.z),
                        uv=new[]{AtlasUv(cell,0,v0),AtlasUv(cell,.5f,v0),AtlasUv(cell,1,v0),AtlasUv(cell,0,v1),AtlasUv(cell,.5f,v1),AtlasUv(cell,1,v1)}});
                }
                s=e+1;
            }
        }
        /// <summary>A pool of light on the ground under every lamp the zone has (lights named "Lamp light") that doesn't already have a painted one. A floor is one renderer and takes
        /// six lights at most, so most lamps along a long street never reach it: the pool is what says the lamp is lit.</summary>
        public int LampPools(float radius,Color light)
        {
            var have=All<Transform>().Where(t=>t.name=="Lamp pool").Select(t=>t.GetComponent<Renderer>()).Where(r=>r!=null).Select(r=>new Vector2(r.bounds.center.x,r.bounds.center.z)).ToArray();
            int placed=0;
            foreach(var lamp in lamps)
            {
                if(have.Any(h=>Vector2.Distance(h,lamp)<1.5f))continue;
                var item=Lay(Array.IndexOf(DecalKinds,"LampPool"),lamp,radius*2,radius*2,Hash((int)(lamp.x*10),(int)(lamp.y*10),seed)*360,light,Lift+.01f);
                if(item==null)continue;Add("glow",item);placed++;
            }
            Debug.Log($"GROUND_LAMP_POOLS {zone} placed={placed} of {lamps.Count}");return placed;
        }
        /// <summary>Small lights lying on the ground itself: glow-fungus specks, crystal dust, embers. Added to what's under them, so they show in the dark. Returns how many lie.</summary>
        public int Glows(float per100,Func<Vector2,float> where,float smallest,float largest,Color light,int salt=0)
        {
            int placed=0,cell=Array.IndexOf(DecalKinds,"LampPool");
            Sow(per100,where,salt+700,(p,roll)=>
            {
                if(signs.Any(s=>s.Contains(p))||prompts.Any(q=>Vector2.Distance(q,p)<1))return;
                float size=Mathf.Lerp(smallest,largest,roll.y);var item=Lay(cell,p,size,size,roll.w*360,Vary(light,.2f,roll.z),Lift+.008f);
                if(item!=null){Add("glow",item);placed++;}
            });
            Debug.Log($"GROUND_GLOWS {zone} placed={placed}");return placed;
        }
        /// <summary>A puddle: level water in a ragged outline, lying on ground that's level enough to hold it. False if the ground isn't, or something's in the way.</summary>
        public bool Puddle(Vector2 at,float rx,float rz,float yaw=0)
        {
            var rect=Aabb(at,rx*2,rz*2,yaw);if(!ClearFor(rect,true))return false;
            const int Around=28,Rings=3;var turn=Quaternion.Euler(0,yaw,0);float low=float.MaxValue,high=float.MinValue,narrow=Mathf.Min(rx,rz);
            var v=new List<Vector3>{Vector3.zero};var uv=new List<Vector2>{Vector2.one*.5f};var uv2=new List<Vector2>{new Vector2(0,narrow)};var t=new List<int>();
            if(!Floor(at,out float middle))return false;
            for(int ring=1;ring<=Rings;ring++)for(int i=0;i<Around;i++)
            {
                float a=i*Mathf.PI*2/Around,reach=(float)ring/Rings*(.86f+.1f*Mathf.Sin(a*3+at.x)+.06f*Mathf.Sin(a*7+at.y));
                var local=turn*new Vector3(Mathf.Cos(a)*rx*reach,0,Mathf.Sin(a)*rz*reach);var p=new Vector2(at.x+local.x,at.y+local.z);
                if(!Floor(p,out float y))return false;
                low=Mathf.Min(low,y);high=Mathf.Max(high,y);
                v.Add(new Vector3(p.x,0,p.y));uv.Add(new Vector2(.5f+Mathf.Cos(a)*.48f*reach,.5f+Mathf.Sin(a)*.48f*reach));uv2.Add(new Vector2(0,Mathf.Max(0,1-(float)ring/Rings)*narrow));
                int current=1+(ring-1)*Around+i,next=1+(ring-1)*Around+(i+1)%Around;
                if(ring==1)t.AddRange(new[]{0,next,current});else{int inner=current-Around,innerNext=next-Around;t.AddRange(new[]{inner,innerNext,current,current,innerNext,next});}
            }
            if(high-Mathf.Min(low,middle)>.07f)return false;
            float level=Mathf.Max(high,middle)+.012f;v[0]=new Vector3(at.x,level,at.y);for(int i=1;i<v.Count;i++)v[i]=new Vector3(v[i].x,level,v[i].z);
            Add("puddle",new Item{v=v.ToArray(),uv=uv.ToArray(),uv2=uv2.ToArray(),t=t.ToArray(),c=Enumerable.Repeat(new Color32(255,255,255,255),v.Count).ToArray(),at=at});
            laid.Add((rect,"Puddle",true));return true;
        }

        // ---------------------------------------------------------------- 3. low cover
        /// <summary>Close to someone, or on the floor between them and the fixed camera: from there even a low plant stands across their feet.</summary>
        bool InSightOf(Vector2 p,float reach,float length,float half)
        {
            var toCamera=new Vector2(-away.x,-away.z);
            foreach(var body in people)
            {
                var offset=p-new Vector2(body.x,body.z);if(offset.magnitude<reach)return true;
                float along=Vector2.Dot(offset,toCamera),across=Mathf.Abs(offset.x*toCamera.y-offset.y*toCamera.x);
                if(along>0&&along<length&&across<half)return true;
            }
            return false;
        }
        bool CoverClear(Vector2 p,float height,float girth)
        {
            if(Lane(p)<girth*.5f||signs.Any(s=>s.Contains(p))||prompts.Any(q=>Vector2.Distance(q,p)<1.6f)||InSightOf(p,1.6f,3.2f,1.1f))return false;
            if(height>=TallCover&&InAFight(p,3))return false;
            float margin=girth*.5f;
            foreach(var s in solids)if(p.x>s.xMin-margin&&p.x<s.xMax+margin&&p.y>s.yMin-margin&&p.y<s.yMax+margin)return false;
            return true;
        }
        /// <summary>One card of cover standing <paramref name="height"/> metres, turned to the fixed camera, with its contact shadow. False if it may not stand there.</summary>
        public bool Stand(string kind,Vector2 p,float height,Color tint,bool flip=false)
        {
            int cell=Array.IndexOf(CoverKinds,kind);if(cell<0)throw new InvalidOperationException("GROUND_PROBLEM "+zone+": no cover called "+kind);
            if(height>CoverLimit){problems.Add($"{kind} asked to stand {height:0.00} m at ({p.x:0.0},{p.y:0.0}): cover stays under {CoverLimit} m (D131)");return false;}
            float side=height/CoverFills[cell];
            if(!CoverClear(p,height,side*.6f)||!Floor(p,out float y))return false;
            // Leaning back a little from the camera, as a drawn card does: upright it loses a quarter of its height to the camera's pitch.
            var foot=new Vector3(p.x,y-side*.035f,p.y);var up=(Vector3.up+away*.3f).normalized*side;
            Color32 low=Baked(new Color(tint.r*.74f,tint.g*.74f,tint.b*.78f,1)),high=Baked(new Color(tint.r,tint.g,tint.b,1));float u0=flip?1:0,u1=flip?0:1;
            Add(Grass.Contains(kind)?"grass":"cover",new Item{v=new[]{foot-right*side*.5f,foot+right*side*.5f,foot-right*side*.5f+up,foot+right*side*.5f+up},
                uv=new[]{AtlasUv(cell,u0,0),AtlasUv(cell,u1,0),AtlasUv(cell,u0,1),AtlasUv(cell,u1,1)},c=new[]{low,low,high,high},t=new[]{0,2,1,1,2,3},at=p,sprout=new Vector4(p.x,y,p.y,height)});
            standing.Add((new Vector3(p.x,y,p.y),height,kind));
            var shadow=Lay(Array.IndexOf(DecalKinds,"Shadow"),p+new Vector2(away.x,away.z)*side*.06f,side*.8f,side*.42f,SightlineAudit.Yaw,new Color(.2f,.17f,.24f,.75f),Lift-.006f);
            if(shadow!=null)Add("decal",shadow);
            return true;
        }
        /// <summary>Scatters cover cards in clumps of up to <paramref name="clump"/>, between two heights. Returns how many stand.</summary>
        public int Cover(string[] kinds,float per100,Func<Vector2,float> where,float lowest,float tallest,Color tint,int clump=1,float vary=.12f,int salt=0)
        {
            int placed=0;
            Sow(per100,where,salt+Stable(kinds[0])+500,(p,roll)=>
            {
                string kind=kinds[Mathf.Min(kinds.Length-1,(int)(roll.x*kinds.Length))];int count=1+(int)(roll.y*clump*.999f);
                for(int k=0;k<count;k++)
                {
                    float a=Hash(k,(int)(p.x*8),(int)(p.y*8))*6.283f,d=k==0?0:.18f+.5f*Hash(k+7,(int)(p.x*8),(int)(p.y*8));
                    var at=p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*d;float size=Mathf.Lerp(lowest,tallest,Hash(k+13,(int)(p.x*8),(int)(p.y*8)));
                    if(Stand(kind,at,size,Vary(tint,vary,Hash(k+19,(int)(p.x*8),(int)(p.y*8))),Hash(k+23,(int)(p.x*8),(int)(p.y*8))>.5f))placed++;
                }
            });
            Debug.Log($"GROUND_COVER {zone} {string.Join("/",kinds)} placed={placed}");return placed;
        }
        static readonly Vector3[] RockShape=Icosahedron();
        static readonly int[] RockFaces={0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
        static Vector3[] Icosahedron()
        {
            float t=(1+Mathf.Sqrt(5))/2;
            return new[]{new Vector3(-1,t,0),new Vector3(1,t,0),new Vector3(-1,-t,0),new Vector3(1,-t,0),new Vector3(0,-1,t),new Vector3(0,1,t),new Vector3(0,-1,-t),new Vector3(0,1,-t),new Vector3(t,0,-1),new Vector3(t,0,1),new Vector3(-t,0,-1),new Vector3(-t,0,1)}.Select(v=>v.normalized).ToArray();
        }
        /// <summary>One small rock, half sunk, a different lump each time. It casts a shadow of its own.</summary>
        public bool Rock(Vector2 p,float height,float girth,float yaw,Color tint,int lump=0)
        {
            if(height>CoverLimit){problems.Add($"a rock asked to stand {height:0.00} m at ({p.x:0.0},{p.y:0.0}): cover stays under {CoverLimit} m (D131)");return false;}
            if(!CoverClear(p,height,girth)||!Floor(p,out float y))return false;
            var turn=Quaternion.Euler(0,yaw,0);var v=new Vector3[12];var n=new Vector3[12];var c=new Color32[12];var uv=new Vector2[12];
            for(int i=0;i<12;i++)
            {
                var s=RockShape[i];float bulge=.72f+.5f*Hash(i,lump,seed+41);
                var local=turn*new Vector3(s.x*girth*.5f*bulge,Mathf.Max(-.25f,s.y)*height*.82f*bulge+height*.18f,s.z*girth*.5f*(.7f+.45f*Hash(i,lump,seed+43)));
                v[i]=new Vector3(p.x,y,p.y)+local;n[i]=(turn*new Vector3(s.x,Mathf.Max(.1f,s.y+.35f),s.z)).normalized;
                float lit=.7f+.3f*Mathf.Clamp01(s.y+.4f);c[i]=Baked(new Color(tint.r*lit,tint.g*lit,tint.b*lit,1));uv[i]=new Vector2(v[i].x+v[i].z,v[i].y*2)*.45f;
            }
            Add("rock",new Item{v=v,n=n,c=c,uv=uv,t=RockFaces,at=p});standing.Add((new Vector3(p.x,y,p.y),height,"Rock"));return true;
        }
        /// <summary>Scatters small rocks in clumps. Returns how many lie.</summary>
        public int Rocks(float per100,Func<Vector2,float> where,float lowest,float tallest,Color tint,int clump=3,float vary=.12f,int salt=0)
        {
            int placed=0;
            Sow(per100,where,salt+900,(p,roll)=>
            {
                int count=1+(int)(roll.y*clump*.999f);
                for(int k=0;k<count;k++)
                {
                    int px=(int)(p.x*8),pz=(int)(p.y*8);float a=Hash(k,px,pz)*6.283f,d=k==0?0:.15f+.55f*Hash(k+7,px,pz),h=Mathf.Lerp(lowest,tallest,Hash(k+13,px,pz)*(k==0?1:.6f));
                    if(Rock(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*d,h,h*(1.5f+1.3f*Hash(k+17,px,pz)),Hash(k+29,px,pz)*360,Vary(tint,vary,Hash(k+19,px,pz)),px*31+pz+k))placed++;
                }
            });
            Debug.Log($"GROUND_ROCKS {zone} placed={placed}");return placed;
        }
        /// <summary>The palette the licensed local grass takes for this zone's tufts (LocalBiomeBake), and the texture its small rocks wear.</summary>
        public GroundKit Look(string grassPalette,Texture2D rocks){palette=grassPalette;rockTexture=rocks;return this;}

        // ---------------------------------------------------------------- 4. moving light
        /// <summary>Cloud shadow (or, small and swaying, canopy dapple) across the zone's sun. One per zone; underground, none.</summary>
        public GroundShade MovingShade(string map,float size,Vector2 drift,float strength,float threshold=.5f,float edgeWidth=.02f,Vector2 sway=default,Vector2 phase=default)
        {
            var shade=new GameObject("Ground shade",typeof(GroundShade)).GetComponent<GroundShade>();shade.transform.SetParent(root,false);
            shade.map=Texture("ground-shade-"+map,true,false,true);
            shade.size=size;shade.drift=drift;shade.strength=strength;shade.threshold=threshold;shade.edge=edgeWidth;shade.sway=sway;shade.phase=phase;return shade;
        }

        // ---------------------------------------------------------------- materials
        /// <summary>Each texture is looked at once a run, and only the settings a material depends on are compared: the project's own texture rules set the size, and comparing
        /// that sent every call back round an import (seven hundred in one staging run, with a floor's material losing its layers somewhere among them).</summary>
        static readonly Dictionary<string,Texture2D> textures=new();
        static Texture2D Texture(string file,bool tiling,bool cutout=false,bool data=false)
        {
            if(textures.TryGetValue(file,out var known)&&known!=null)return known;
            string path=Art+file+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter??throw new InvalidOperationException("GROUND_PROBLEM missing texture "+path+": run python tools/ground_textures.py");
            var wrap=tiling?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            if(importer.wrapMode!=wrap||!importer.mipmapEnabled||importer.alphaIsTransparency!=cutout||importer.mipMapsPreserveCoverage!=cutout||importer.sRGBTexture==data)
            {
                importer.wrapMode=wrap;importer.mipmapEnabled=true;importer.alphaIsTransparency=cutout;importer.mipMapsPreserveCoverage=cutout;importer.alphaTestReferenceValue=.45f;importer.sRGBTexture=!data;
                importer.anisoLevel=4;importer.SaveAndReimport();
            }
            return textures[file]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        /// <summary>A world material by name, tuned and written at once. The kit's shared ones are made once a run.</summary>
        static readonly Dictionary<string,Material> shared=new();
        static Material Made(string name,Action<Material> tune,Material from=null)
        {
            if(from==null&&shared.TryGetValue(name,out var known)&&known!=null)return known;
            string path=WorldMaterials+name+".mat";var made=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(made==null){made=from!=null?new Material(from):new Material(Shader.Find("Lattice/Toon"));AssetDatabase.CreateAsset(made,path);}
            else if(from!=null)made.CopyPropertiesFromMaterial(from);
            tune(made);EditorUtility.SetDirty(made);AssetDatabase.SaveAssetIfDirty(made);
            if(from==null)shared[name]=made;
            return made;
        }
        static void Keyword(Material m,string keyword,string toggle,bool on){if(on)m.EnableKeyword(keyword);else m.DisableKeyword(keyword);m.SetFloat(toggle,on?1:0);}
        static void Blended(Material m,BlendMode to,int queue)
        {
            m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)to);m.SetFloat("_ZWrite",0);m.renderQueue=queue;m.SetOverrideTag("RenderType","Transparent");
            Keyword(m,"_VCOLOR_ON","_VColor",true);Keyword(m,"_ALPHATEST_ON","_AlphaTest",false);
        }
        /// <summary>Flat details blend onto the ground and are drawn just before danger markings (Transparent-10), so a marking always lies over them.</summary>
        public const int DetailQueue=(int)RenderQueue.Transparent-20;
        /// <summary>Where a danger marking starts to draw (its fill, a step before its outline). Nothing that lies on a floor may draw at or after it.</summary>
        public static int MarkingQueue{get{var fill=Shader.Find("Lattice/TelegraphFill");return fill!=null?fill.renderQueue:(int)RenderQueue.Transparent-11;}}
        static Material DecalMaterial=>Made("ground-decal",m=>{m.SetTexture("_BaseMap",Texture("ground-decals",false,true));m.SetColor("_BaseColor",Color.white);Blended(m,BlendMode.OneMinusSrcAlpha,DetailQueue);});
        static Material GlowMaterial=>Made("ground-lamp-pool",m=>
        {
            m.SetTexture("_BaseMap",Texture("ground-decals",false,true));m.SetColor("_BaseColor",Color.white);Blended(m,BlendMode.One,DetailQueue+1);
            Keyword(m,"_GLOW_ON","_Glow",true);m.SetVector("_GlowParams",new Vector4(1,.5f,0,0));m.SetColor("_EmissionColor",Color.black);
        });
        static Material CoverMaterial=>Made("ground-cover",m=>
        {
            m.SetTexture("_BaseMap",Texture("ground-cover",false,true));m.SetColor("_BaseColor",Color.white);m.SetFloat("_Cull",0);
            Keyword(m,"_ALPHATEST_ON","_AlphaTest",true);m.SetFloat("_Cutoff",.45f);Keyword(m,"_VCOLOR_ON","_VColor",true);m.renderQueue=(int)RenderQueue.AlphaTest;
        });
        Material RockMaterial=>Made("ground-rock-"+zone,m=>{m.SetTexture("_BaseMap",rockTexture);m.SetFloat("_BaseMapStrength",.7f);m.SetColor("_BaseColor",Color.white);Keyword(m,"_VCOLOR_ON","_VColor",true);});
        static Material PuddleMaterial=>Made("ground-puddle",m=>
        {
            // The sky lying in a hollow: the toon water, shallow, with a soft ragged rim and a slow glint.
            m.SetColor("_BaseColor",new Color(.4f,.42f,.56f,.55f));m.SetColor("_DeepColor",new Color(.22f,.24f,.38f,.86f));m.SetColor("_FoamColor",new Color(.7f,.72f,.86f,.5f));
            Keyword(m,"_WATER_ON","_Water",true);Keyword(m,"_EDGEFADE_ON","_EdgeFade",true);
            m.SetVector("_WaterParams",new Vector4(1.4f,.1f,.35f,.5f));m.SetVector("_EdgeParams",new Vector4(1,.12f,.03f,.5f));m.SetVector("_FoamParams",new Vector4(.03f,.6f,.02f,.05f));
            m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.renderQueue=DetailQueue+2;m.SetOverrideTag("RenderType","Transparent");
        });
        /// <summary>The floor's own material, copied for this zone alone, with the layers switched on: the original is shared (a stair's treads, another zone's floor) by meshes
        /// that carry no weights.</summary>
        Material SplatMaterial(Material source)
        {
            // Made afresh from the floor's own material and written over the zone's copy whole, as the landform's meshes are: nothing of an earlier run is left in it.
            var fresh=new Material(source){name="ground-"+zone};
            Keyword(fresh,"_SPLAT_ON","_Splat",layers.Length>0||paint.Any(p=>p.w!=0));
            var scales=new Vector4(.25f,.25f,.25f,1/Mathf.Max(.05f,edgeNoiseMetres));
            for(int i=0;i<3;i++)
            {
                bool used=i<layers.Length;
                fresh.SetTexture("_SplatMap"+(i+1),used?Texture("ground-"+layers[i].texture,true):null);fresh.SetColor("_SplatTint"+(i+1),used?layers[i].tint:new Color(1,1,1,0));
                if(used)scales[i]=1/Mathf.Max(.1f,layers[i].metres);
            }
            fresh.SetVector("_SplatScale",scales);fresh.SetVector("_SplatEdge",splatEdge);fresh.SetOverrideTag("GroundSource",source.name);
            string path=WorldMaterials+"ground-"+zone+".mat";var kept=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(kept==null){AssetDatabase.CreateAsset(fresh,path);kept=fresh;}
            else{EditorUtility.CopySerialized(fresh,kept);Object.DestroyImmediate(fresh);EditorUtility.SetDirty(kept);}
            AssetDatabase.SaveAssetIfDirty(kept);
            Debug.Log($"GROUND_FLOOR_MATERIAL {zone} from={source.name} layers={layers.Length} splat={kept.IsKeywordEnabled("_SPLAT_ON")} first={(kept.GetTexture("_SplatMap1")!=null?kept.GetTexture("_SplatMap1").name:"none")}");
            return kept;
        }

        // ---------------------------------------------------------------- 6. the self-check, and writing it all down
        static bool Hides(Bounds box,Vector3 target)
        {
            var eye=target+SightlineAudit.ToCamera*SightlineAudit.Distance;
            return box.IntersectRay(new Ray(eye,target-eye),out float along)&&along<SightlineAudit.Distance-SightlineAudit.NearIgnore;
        }
        bool HidesSomeone(Bounds box)
        {
            foreach(var person in people)foreach(float height in new[]{.4f,.9f,1.5f})if(Hides(box,person+Vector3.up*height))return true;
            return false;
        }
        /// <summary>A standing batch whose drawn box takes in where someone stands, as the suite reads a prop's box (its inner three quarters and a body's width): every tuft kept
        /// 1.6 m from Teo, the Braided River's cover still made a box round him, and he read as standing inside a prop.</summary>
        bool HoldsSomeone(Bounds box)
        {
            if(box.size.y<.25f)return false;
            foreach(var p in people)if(Mathf.Abs(p.x-box.center.x)<box.extents.x*.75f+.3f&&Mathf.Abs(p.z-box.center.z)<box.extents.z*.75f+.3f&&box.max.y>p.y+.3f&&box.min.y<p.y+1.8f)return true;
            return false;
        }
        static Bounds BoundsOf(List<Item> items){var b=new Bounds(items[0].v[0],Vector3.zero);foreach(var item in items)foreach(var v in item.v)b.Encapsulate(v);return b;}
        /// <summary>A batch whose drawn box would stand between the camera and someone, or take in where someone stands, is split until no part's does: a box spanning a terrace
        /// edge claims the air over the lower floor.</summary>
        void Split(List<Item> items,List<List<Item>> into)
        {
            var box=BoundsOf(items);
            if(!HidesSomeone(box)&&!HoldsSomeone(box)){into.Add(items);return;}
            if(items.Count==1){Debug.Log($"GROUND_DROPPED {zone} a piece at ({items[0].at.x:0.0},{items[0].at.y:0.0}) would stand in front of someone");return;}
            bool alongX=box.size.x>=box.size.z;var sorted=items.OrderBy(i=>alongX?i.at.x:i.at.y).ToList();
            Split(sorted.GetRange(0,sorted.Count/2),into);Split(sorted.GetRange(sorted.Count/2,sorted.Count-sorted.Count/2),into);
        }
        void Check()
        {
            foreach(var (rect,kind,_) in laid)if(signs.Any(s=>s.Overlaps(rect)))problems.Add($"{kind} at ({rect.center.x:0.0},{rect.center.y:0.0}) lies under a floor sign");
            foreach(var (at,height,kind) in standing)
            {
                var p=new Vector2(at.x,at.z);
                if(height>CoverLimit)problems.Add($"{kind} at ({p.x:0.0},{p.y:0.0}) stands {height:0.00} m: cover stays under {CoverLimit} m (D131)");
                if(Lane(p)<0)problems.Add($"{kind} at ({p.x:0.0},{p.y:0.0}) stands in a lane that has to stay clear");
                if(signs.Any(s=>s.Contains(p)))problems.Add($"{kind} at ({p.x:0.0},{p.y:0.0}) stands on a floor sign");
                if(prompts.Any(q=>Vector2.Distance(q,p)<1.2f))problems.Add($"{kind} at ({p.x:0.0},{p.y:0.0}) stands on a prompt");
            }
            if(root.GetComponentsInChildren<Collider>(true).Length>0)problems.Add("something in the ground kit is solid: navigation was baked without it");
            for(int i=0;i<paint.Length;i++)if(float.IsNaN(paint[i].x+paint[i].y+paint[i].z+paint[i].w)){problems.Add("a painted weight isn't a number");break;}
        }

        /// <summary>Writes the weights into the floor mesh, builds the batches under the root, and checks the lot. Throws GROUND_PROBLEM (and so leaves the scene unsaved) on a fault.</summary>
        public void Finish()
        {
            try
            {
                Check();
                if(problems.Count>0)
                {
                    foreach(var problem in problems.Take(40))Debug.LogError("GROUND_PROBLEM "+zone+": "+problem);
                    throw new InvalidOperationException($"GROUND_PROBLEM {zone}: {problems.Count} faults, first: {problems[0]}");
                }
                // Every texture is settled before a material is touched: an import in the middle of a material's edit is how a floor lost its layers.
                foreach(var layer in layers)Texture("ground-"+layer.texture,true);
                Texture("ground-decals",false,true);Texture("ground-cover",false,true);
                // A zone that paints no layer and no shade keeps its floor's mesh and material as they are: details, cover and light only.
                bool painted=layers.Length>0||paint.Any(q=>q.w!=0);
                if(painted&&pieces!=null)
                {
                    var layered=SplatMaterial(piecesPlain);var from=new Dictionary<Vector3Int,int>();for(int i=0;i<points.Length;i++)from[Spot(points[i])]=i;int worn=0;
                    foreach(var piece in pieces)
                    {
                        var cut=piece.sharedMesh;var at=cut.vertices;var world=piece.transform.localToWorldMatrix;var weights=new List<Vector4>(at.Length);
                        foreach(var v in at)weights.Add(from.TryGetValue(Spot(world.MultiplyPoint3x4(v)),out int k)?paint[k]:Vector4.zero);
                        cut.SetUVs(2,weights);EditorUtility.SetDirty(cut);
                        var drawn=piece.GetComponent<Renderer>();var wears=drawn.sharedMaterials;bool changed=false;
                        for(int j=0;j<wears.Length;j++)if(wears[j]==piecesPlain||wears[j]!=null&&wears[j].name=="ground-"+zone){wears[j]=layered;changed=true;}
                        if(changed){drawn.sharedMaterials=wears;worn++;}
                    }
                    Debug.Log($"GROUND_PIECES {zone} pieces={pieces.Count} layered={worn}");
                }
                else if(painted){mesh.SetUVs(2,new List<Vector4>(paint));EditorUtility.SetDirty(mesh);}
                var view=floor.GetComponent<Renderer>();var source=view.sharedMaterial;if(painted&&pieces==null){
                // A rerun finds the kit's own material on the floor: the one it was copied from is named in it.
                if(source.name=="ground-"+zone)source=AssetDatabase.LoadAssetAtPath<Material>(WorldMaterials+source.GetTag("GroundSource",false,"")+".mat")??throw new InvalidOperationException("GROUND_PROBLEM "+zone+": the floor's own material is lost; rebuild the zone");
                var splat=SplatMaterial(source);view.sharedMaterial=splat;
                if(layers.Length>0&&(!splat.IsKeywordEnabled("_SPLAT_ON")||splat.GetTexture("_SplatMap1")==null))throw new InvalidOperationException("GROUND_PROBLEM "+zone+": the floor's material didn't take its layers");
                // One big mesh with a uv set static batching has no use for: left out of it, as the Lantern Road's floors are.
                var flags=GameObjectUtility.GetStaticEditorFlags(floor.gameObject);if((flags&StaticEditorFlags.BatchingStatic)!=0)GameObjectUtility.SetStaticEditorFlags(floor.gameObject,flags&~StaticEditorFlags.BatchingStatic);
                }

                var meshes=new List<Mesh>();int parts=0,triangles=0;
                foreach(var group in batches.OrderBy(b=>b.Key.kind).ThenBy(b=>b.Key.x).ThenBy(b=>b.Key.z))
                {
                    var (kind,cx,cz)=group.Key;var pieces=new List<List<Item>>();Split(group.Value,pieces);int n=0;
                    foreach(var items in pieces)
                    {
                        // Tufts hang from their patch's own place, as every BiomePatch's sprouts do; the rest are drawn where they lie.
                        var origin=kind=="grass"?new Vector3((cx+.5f)*Cell,BoundsOf(items).min.y,(cz+.5f)*Cell):Vector3.zero;
                        var v=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var uv2=new List<Vector2>();var c=new List<Color32>();var t=new List<int>();
                        foreach(var item in items)
                        {
                            int first=v.Count;foreach(var p in item.v)v.Add(p-origin);
                            if(item.n!=null)normals.AddRange(item.n);else for(int i=0;i<item.v.Length;i++)normals.Add(Vector3.up);
                            uv.AddRange(item.uv);c.AddRange(item.c);if(item.uv2!=null)uv2.AddRange(item.uv2);foreach(int i in item.t)t.Add(first+i);
                        }
                        string name=kind switch{"decal"=>"Floor detail ground","glow"=>"Floor detail lamp pool","puddle"=>"Floor detail puddle","rock"=>"Floor detail cover rocks","grass"=>"Floor detail cover grass",_=>"Floor detail cover"};
                        var made=new Mesh{name=$"{zone} {kind} {cx},{cz}"+(pieces.Count>1?" "+n:""),indexFormat=v.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};n++;
                        made.SetVertices(v);made.SetNormals(normals);made.SetUVs(0,uv);if(uv2.Count==v.Count)made.SetUVs(1,uv2);made.SetColors(c);made.SetTriangles(t,0);made.RecalculateBounds();meshes.Add(made);
                        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.transform.position=origin;
                        go.GetComponent<MeshFilter>().sharedMesh=made;var drawn=go.GetComponent<MeshRenderer>();
                        drawn.sharedMaterial=kind switch{"decal"=>DecalMaterial,"glow"=>GlowMaterial,"puddle"=>PuddleMaterial,"rock"=>RockMaterial,_=>CoverMaterial};
                        drawn.shadowCastingMode=kind=="rock"?ShadowCastingMode.On:ShadowCastingMode.Off;drawn.receiveShadows=kind!="glow";
                        if(kind=="grass")
                        {
                            var patch=go.AddComponent<BiomePatch>();patch.resourceKey=$"{zone}-ground-{cx}_{cz}"+(pieces.Count>1?"-"+(n-1):"");patch.palette=palette;patch.water=false;
                            patch.sprouts=items.Select(i=>new Vector4(i.sprout.x-origin.x,i.sprout.y-origin.y,i.sprout.z-origin.z,i.sprout.w)).ToArray();
                        }
                        parts++;triangles+=t.Count/3;
                    }
                }
                string path=Art+"GroundMeshes-"+zone+".asset";
                if(AssetDatabase.LoadMainAssetAtPath(path)!=null)AssetDatabase.DeleteAsset(path);
                if(meshes.Count>0){AssetDatabase.CreateAsset(meshes[0],path);foreach(var made in meshes.Skip(1))AssetDatabase.AddObjectToAsset(made,path);AssetDatabase.ImportAsset(path);}
                Debug.Log($"GROUND_KIT {zone} layers={layers.Length} details={laid.Count} standing={standing.Count} batches={parts} triangles={triangles}");
            }
            finally{Close();}
        }
        /// <summary>Lets go of the probe without writing anything.</summary>
        public void Close(){if(probe!=null)Object.DestroyImmediate(probe.gameObject);Physics.SyncTransforms();}
    }

    /// <summary>
    /// Gentle relief for a zone's height function: centimetres to a decimetre or two, added before the landform is built, so collision, navigation, feet and props all stand on it.
    /// Each piece is a term to add to a builder's height. <see cref="Check"/> refuses relief a body would trip on or a toon band would break across.
    /// </summary>
    public static class GroundRelief
    {
        /// <summary>The most a relief may change the ground over one step of floor (a tenth of the navigation's climb), and the steepest it may tilt it and still be lit as level.</summary>
        public const float MostStep=.12f,MostSlope=.2f;
        /// <summary>A pressed channel <paramref name="width"/> metres wide along a path: a rut, a runnel, a way worn hollow. Negative height.</summary>
        public static float Rut(Vector2 p,Vector2[] path,float offset,float width,float depth)
        {
            float best=float.PositiveInfinity;
            for(int i=1;i<path.Length;i++)
            {
                Vector2 a=path[i-1],d=path[i]-a;float length=d.magnitude;var n=new Vector2(-d.y,d.x)/length;
                best=Mathf.Min(best,GroundKit.Segment(p,a+n*offset,path[i]+n*offset));
            }
            return -depth*(1-GroundKit.Smooth(width*.15f,width*.5f,best));
        }
        /// <summary>Low mounds about <paramref name="metres"/> across over <paramref name="cover"/> of the ground: drifted banks, heaped spoil. Positive height.</summary>
        public static float Mounds(Vector2 p,float metres,float height,float cover,int salt=0)
        {
            float n=Mathf.PerlinNoise(p.x/metres+salt*17.3f+5,p.y/metres+salt*9.1f+3)*.7f+Mathf.PerlinNoise(p.x/metres*2.1f+salt*3.7f,p.y/metres*2.1f+salt*5.3f)*.3f;
            float cut=Mathf.Lerp(.75f,.3f,cover);return height*GroundKit.Smooth(cut,cut+.22f,n);
        }
        /// <summary>A laid surface standing <paramref name="height"/> proud of the ground round it: a slab edge, a kerb, rising over <paramref name="run"/> metres where
        /// <paramref name="inside"/> (metres inside the surface's edge, negative outside) crosses nought.</summary>
        public static float Kerb(float inside,float height,float run=.5f)=>height*GroundKit.Smooth(-run*.5f,run*.5f,inside);
        /// <summary>Null if the relief is gentle everywhere on the rectangle; else what's wrong and where, for a GROUND_PROBLEM.</summary>
        public static string Check(Func<Vector2,float> relief,float xMin,float xMax,float zMin,float zMax,float cell=.5f)
        {
            for(float z=zMin;z<zMax;z+=cell)for(float x=xMin;x<xMax;x+=cell)
            {
                var p=new Vector2(x,z);float here=relief(p),stepX=Mathf.Abs(relief(p+Vector2.right*cell)-here),stepZ=Mathf.Abs(relief(p+Vector2.up*cell)-here),step=Mathf.Max(stepX,stepZ);
                if(step>MostStep)return $"relief steps {step:0.00} m in {cell} m at ({x:0.0},{z:0.0}): more than {MostStep} m trips feet and navigation";
                if(step/cell>MostSlope)return $"relief tilts the ground {step/cell:0.00} at ({x:0.0},{z:0.0}): past {MostSlope} it falls out of the level band of the toon ramp";
            }
            return null;
        }
    }
}
