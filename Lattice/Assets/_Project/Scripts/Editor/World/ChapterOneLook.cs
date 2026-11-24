using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 1's look fixes after the whole-game capture of November 20 (nov20-whole s02 to s10b). Sorrel's ore and the ridges its road runs between, the first flight's view
    /// (the Cinder Halo), Tallow's approach, the Gullet's salvage eddy, and bodies that have to read on a dark floor. Staging: called from ChapterOneStaging's own entry points,
    /// so every chapter 1 builder and Restage.All keep it. Each piece clears what it made by name first, so it is safe to run twice. Nothing here is solid at the flight plane,
    /// and on the ground only the ridges are, off every road, fight and ore.
    /// </summary>
    public static class ChapterOneLook
    {
        const string WorldMaterials="Assets/_Project/Resources/WorldMaterials/",Models="Assets/_Project/Art/Generated/Models/";
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static void ClearRoot(string name){foreach(var go in All<Transform>().Where(t=>t!=null&&t.parent==null&&t.name==name).Select(t=>t.gameObject).ToArray())if(go!=null)Object.DestroyImmediate(go);}
        static void Wear(GameObject go,Material material)
        {
            foreach(var part in go.GetComponentsInChildren<Renderer>(true))
            {
                var slots=part.sharedMaterials;for(int i=0;i<slots.Length;i++)slots[i]=material;part.sharedMaterials=slots;
                if(PrefabUtility.IsPartOfPrefabInstance(part))PrefabUtility.RecordPrefabInstancePropertyModifications(part);
            }
        }
        static float Smooth(float from,float to,float x)=>Mathf.SmoothStep(0,1,Mathf.Clamp01((x-from)/(to-from)));
        static Texture2D Read(string path){var made=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!File.Exists(path)||!made.LoadImage(File.ReadAllBytes(path)))throw new InvalidOperationException("CH1_LOOK missing texture "+path);return made;}

        // ---------------------------------------------------------------- Sorrel: the ore (s04/055)
        /// <summary>The ridge crystal was the kit's flat teal: its glass is one colour in the texture, so every facet was the same untextured block on a brown lump, with no glow,
        /// where Mira asks for "bright seams". The ore wears its own texture with the glass worked over: deep in each facet a dark sea-green, toward every facet's edge a pale seam,
        /// and the seams alone glowing, through the glow map, at a glow and a material colour low enough that the facets still show under the sun. Made from the kit's own albedo and glow mask, so the rock it sits in is the rock it had. Sorrel's ore only: the relay's
        /// scatter crystals keep the kit's look.</summary>
        public static void SorrelOre()
        {
            var made=new Dictionary<string,Material>();int dressed=0;
            foreach(var node in All<MineNode>())
            {
                string kit=node.name.StartsWith("CrystalClusterB")?"CrystalClusterB":node.name.StartsWith("CrystalClusterA")?"CrystalClusterA":null;
                if(kit==null)continue;
                if(!made.TryGetValue(kit,out var ore))made[kit]=ore=OreLook(kit);
                Wear(node.gameObject,ore);dressed++;
            }
            Debug.Log("CH1_LOOK_ORE dressed="+dressed);
        }
        static Material OreLook(string kit)
        {
            string folder=Models+kit+"/";var albedo=Read(folder+kit+"_base_color.png");var glass=Read(folder+kit+"_emission.png");
            int size=albedo.width;var colours=albedo.GetPixels();int count=colours.Length;
            // What the glow mask lights is glass; distance in pixels from each glass pixel to the nearest rock one (a two-pass chamfer: near enough to Euclid at this scale).
            var inside=new bool[count];for(int y=0;y<size;y++)for(int x=0;x<size;x++)inside[y*size+x]=glass.GetPixelBilinear((x+.5f)/size,(y+.5f)/size).grayscale>.5f;
            var far=new float[count];const float Straight=1,Diagonal=1.4142f;
            for(int i=0;i<count;i++)far[i]=inside[i]?1e6f:0;
            for(int y=1;y<size-1;y++)for(int x=1;x<size-1;x++){int i=y*size+x;if(far[i]==0)continue;far[i]=Mathf.Min(far[i],Mathf.Min(far[i-1]+Straight,Mathf.Min(far[i-size]+Straight,Mathf.Min(far[i-size-1]+Diagonal,far[i-size+1]+Diagonal))));}
            for(int y=size-2;y>0;y--)for(int x=size-2;x>0;x--){int i=y*size+x;if(far[i]==0)continue;far[i]=Mathf.Min(far[i],Mathf.Min(far[i+1]+Straight,Mathf.Min(far[i+size]+Straight,Mathf.Min(far[i+size+1]+Diagonal,far[i+size-1]+Diagonal))));}
            // Painted bright and the material's colour kept low: under Sorrel's sun a bright material colour clips every band of the ramp to one neon cyan (RidgeCrystalsShowTheirFacets,
            // TheRigAndTheCrystalsDoNotClipUnderSorrelsSun), so the facets' light and dark and the pale seams are in the texture, and what glows is the seams alone, at the glow those allow.
            Color deep=new(.06f,.4f,.48f),body=new(.18f,.76f,.84f),seam=new(1,1,1);
            var lookPixels=new Color[count];var glowPixels=new Color[count];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int i=y*size+x;
                if(!inside[i]){lookPixels[i]=colours[i];glowPixels[i]=Color.black;continue;}
                // A facet is lighter on one side than the other (light through the glass), and streaked along its length.
                float streak=Mathf.PerlinNoise(x*.011f+3,y*.045f+7),rim=1-Smooth(1.2f,8,far[i]),core=Smooth(10,46,far[i]);
                var tone=Color.Lerp(body,deep,core*(.6f+.4f*streak));tone=Color.Lerp(tone,seam,rim);
                lookPixels[i]=new Color(tone.r,tone.g,tone.b,1);
                float glow=Mathf.Max(rim,.04f+.08f*streak*(1-core));glowPixels[i]=new Color(glow,glow,glow,1);
            }
            Object.DestroyImmediate(albedo);Object.DestroyImmediate(glass);
            string letter=kit.Substring(kit.Length-1).ToLowerInvariant();
            var look=ZoneKit.Texture("Sorrel/ore-"+letter+"-seams",size,(u,v)=>lookPixels[Mathf.Min(size-1,(int)(v*size))*size+Mathf.Min(size-1,(int)(u*size))]);
            var glowMap=ZoneKit.Texture("Sorrel/ore-"+letter+"-glow",size,(u,v)=>glowPixels[Mathf.Min(size-1,(int)(v*size))*size+Mathf.Min(size-1,(int)(u*size))]);
            return ZoneKit.Toon("sorrel-ore-"+letter,look,m=>
            {
                m.SetColor("_BaseColor",new Color(.6f,.6f,.6f));m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.4f,.52f,.62f));
                m.SetTexture("_EmissionMap",glowMap);m.SetColor("_EmissionColor",new Color(.07f,.28f,.29f));
                m.SetColor("_RimColor",new Color(.42f,.86f,.92f));m.SetFloat("_RimPower",3.2f);
            });
        }

        // ---------------------------------------------------------------- Sorrel: the ridges (s02/019, s04/020)
        /// <summary>"Follow the ridges" and no ridge in frame: the basin's rises between the roads are gentle swells under the ground's own texture, and from the fixed camera, which sees about
        /// nine metres either side of the road and thirteen ahead, they read as flat floor. So the ridges' feet come down to the road: spines of Sorrel's own red slab rock, a steep face
        /// to the road and the crest rising away from it, at the frame's edges where the road leaves the outpost and along the haul road. Each spine is a landform (GroundTerrain over
        /// the basin's own height, its foot under the ground), solid, cut out of the paths where it stands, and held clear of every road, fight, ore and the camera's line to them
        /// (CH1_LOOK_RIDGE refuses one that isn't). Spine, height along it, half width, which way is away from the road, seed, how far its back runs.
        /// <para>Second round (nov22-whole s04/019, 054, D360): those two spines stood at the frame's corners, one of them under the objective panel, and from the haul road between
        /// them the frame was road and scrub. The haul road now runs between ridges from the outpost to the drill: a spine on one side or both wherever the road isn't crossing a fight's
        /// clearing, each piece a dozen metres or so with its own height along it, its face at 7 to 8 m from the road's middle (inside the 9 m either side the play camera sees), swinging
        /// out round the clearings and stopping short of the seam and service roads, whose side of a spine slopes away over a shorter back. Laid out and measured from the play camera
        /// beside the road before it was built (scratchpad d360/ridgeplan.py): in frame at 52 of 66 two-metre steps between the outpost and the drill; the rest are inside fights' clearings.</para>
        /// <para>Third round (nov22b-whole s03/007, s04/020, s04/053, D368): the route stops for its stills at the road's bends and in its fights' clearings (every still between the
        /// outpost and the drill is within ten metres of a fight or at a bend), and those were exactly where the spines swung out or stopped, and where the test skipped. With feet 7 m and
        /// more from the road's middle a spine is at the frame's very edge (the camera sees 9 m either side at the party's depth), and round a clearing it was at a corner, under a panel.
        /// So the side of the road the camera sees past is pulled in, its foot 4.4 to 6 m from the road's middle (held 1.5 m and more off every line the lead and the companion walked in
        /// nov22b s02 to s07, and 1 m off the verge's crates, lamp and rocks), and the clearings have their own: west 3b round the wash's fight (s04/020), west 1 inside the outpost bend
        /// (s03/007), west 6 down past the upper ore to the excavation's fight, and east 7 brought down beside the road to the drill (s04/053). Measured from the capture's own
        /// cameras (ChapterOneTwoLookTests.TheHaulRoadRunsBetweenRidges).</para></summary>
        internal static readonly (string name,Vector2[] spine,float[] tall,float half,Vector2 away,int seed,float back)[] Ridges=
        {
            ("Outpost ridge",new[]{new Vector2(10.5f,24),new Vector2(12.5f,27)},new[]{2.8f,3.4f},2,new Vector2(1,0),11,RidgeBack),
            // West of the road, from the Scrapmites' first fight to the drill: on the left of every frame the road's stills stand in. Inside the outpost bend, its back short of the seam road.
            ("Haul ridge west 1",new[]{new Vector2(-.6f,36.2f),new Vector2(.6f,41.6f),new Vector2(-.6f,45.6f)},new[]{3,3.9f,3.4f},1.7f,new Vector2(-1,0),41,3),
            // Behind the verge's crates at its south end.
            ("Haul ridge west 2",new[]{new Vector2(-1.4f,47),new Vector2(-2.1f,49.6f),new Vector2(-2.2f,52.6f),new Vector2(-3.5f,56.2f)},new[]{3.3f,3.7f,3.1f,2.9f},1.7f,new Vector2(-.97f,-.25f),43,RidgeBack),
            ("Haul ridge west 3",new[]{new Vector2(-4.4f,57.4f),new Vector2(-5.4f,58.8f),new Vector2(-7.1f,61.2f),new Vector2(-8.5f,63.2f)},new[]{2.8f,3.4f,4,3.6f},1.7f,new Vector2(-.82f,-.58f),47,RidgeBack),
            // Round the wash's fight (s04/020), where the road bends north: the left of the frame from the fight's middle.
            ("Haul ridge west 3b",new[]{new Vector2(-12.6f,65.5f),new Vector2(-14.6f,69.5f),new Vector2(-15.9f,73.6f),new Vector2(-15.4f,78.2f),new Vector2(-14.4f,82.2f)},new[]{3.2f,3.8f,4.2f,3.7f,3.2f},1.7f,new Vector2(-1,.1f),49,RidgeBack),
            // Between the Scrapmites' fight and the wash's: the seam road's fight is ten metres west of its back, so it is narrow and its back short; behind the fallen lamp at its south end, and on past the bend to the edge of the basin's fight (s04/031).
            ("Haul ridge west 4",new[]{new Vector2(-14.9f,83.8f),new Vector2(-14.5f,86.3f),new Vector2(-13.3f,88.8f),new Vector2(-12.3f,91.3f),new Vector2(-11.9f,93.9f),new Vector2(-11.8f,96.2f)},new[]{3,3.8f,4.2f,3.6f,3.4f,3},1.4f,new Vector2(-.99f,.16f),53,1.6f),
            // Past the upper ore, toward the excavation: the seam road comes back in behind them, so their backs are short. West 6 runs on past the verge's ledge to the excavation's fight.
            ("Haul ridge west 5",new[]{new Vector2(-6.2f,113.1f),new Vector2(-2.7f,114.1f),new Vector2(-1.1f,115.4f)},new[]{3.1f,3.9f,4.1f},1.7f,new Vector2(-.77f,.64f),59,2.4f),
            ("Haul ridge west 6",new[]{new Vector2(3.4f,115.3f),new Vector2(3.8f,118.4f),new Vector2(4.4f,121.8f),new Vector2(5.8f,125.2f),new Vector2(6.3f,128.5f),new Vector2(6.4f,131.4f)},new[]{3.6f,4.2f,3.8f,4.1f,3.9f,3.3f},1.6f,new Vector2(-.95f,.3f),61,1.6f),
            // East of the road, from the bend below the strata past the Scrapmites' and the wash's fights to the upper ore, and past the excavation's fight to the drill.
            ("Haul ridge east 1",new[]{new Vector2(17,53.5f),new Vector2(14.3f,55.9f),new Vector2(13.3f,59.8f)},new[]{3.6f,4.1f,3.7f},1.7f,new Vector2(.97f,.25f),67,RidgeBack),
            ("Haul ridge east 2",new[]{new Vector2(11.7f,63.4f),new Vector2(9.9f,65.9f),new Vector2(8.2f,68.3f),new Vector2(6.6f,70.6f)},new[]{3.4f,4,4.3f,3.8f},1.7f,new Vector2(.82f,.58f),71,RidgeBack),
            ("Haul ridge east 3",new[]{new Vector2(5.7f,72),new Vector2(4.7f,75),new Vector2(4.4f,78.2f),new Vector2(3,81.4f)},new[]{3.6f,4.2f,3.9f,3.3f},1.7f,new Vector2(.99f,-.16f),73,RidgeBack),
            ("Haul ridge east 4",new[]{new Vector2(2.9f,83.2f),new Vector2(3.3f,86.4f),new Vector2(3.9f,89.3f),new Vector2(5.6f,92)},new[]{3,3.6f,4.1f,3.7f},1.7f,new Vector2(.99f,-.16f),79,RidgeBack),
            // Pulled in to the road where it turns north-east, behind the verge's crates at its south end.
            ("Haul ridge east 5",new[]{new Vector2(5.1f,94.6f),new Vector2(6.7f,97.2f),new Vector2(9.2f,100.5f),new Vector2(11,102.8f),new Vector2(13.6f,105.9f)},new[]{3.2f,4,4.3f,3.6f,3.1f},1.7f,new Vector2(.77f,-.64f),83,RidgeBack),
            ("Haul ridge east 6",new[]{new Vector2(19.2f,109.4f),new Vector2(20.2f,111.8f),new Vector2(20.9f,113.9f)},new[]{3,3.8f,3.2f},1.7f,new Vector2(.93f,-.36f),89,2),
            // Down from the drill's apron to the excavation's fight, beside the road that leaves it (s04/053), its south end short of the service road's fight.
            ("Haul ridge east 7",new[]{new Vector2(22.8f,133.8f),new Vector2(20.6f,136.6f),new Vector2(19,139.6f),new Vector2(17.9f,142.6f),new Vector2(17.3f,145.8f)},new[]{3.2f,3.7f,4.1f,3.7f,3.2f},1.7f,new Vector2(.87f,.49f),97,RidgeBack),
        };
        /// <summary>How far a ridge's foot keeps from a road's middle, a fight's middle and an ore body. A road is graded six metres either side and its gravel runs about three; a
        /// foot past 4.2 m stands on the verge (D368: at 6.6 m the spines were at the frame's edge).</summary>
        internal const float RidgeFromRoad=4.8f,RidgeFromFight=8,RidgeFromOre=3.5f,RidgeBack=4.5f;
        /// <summary>Height a ridge adds over the basin at a point: a steep face on the road side, a crest that keeps rising away from it over <paramref name="back"/> metres, ends rounded,
        /// its edges ragged.</summary>
        internal static float RidgeAt(Vector2 p,Vector2[] spine,float[] tall,float half,Vector2 away,int seed,float back=RidgeBack)
        {
            float best=float.PositiveInfinity,along=0,total=0,run=0;Vector2 nearest=spine[0];
            for(int i=1;i<spine.Length;i++)total+=Vector2.Distance(spine[i-1],spine[i]);
            for(int i=1;i<spine.Length;i++)
            {
                var a=spine[i-1];var d=spine[i]-a;float length=d.magnitude;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/(length*length));var q=a+d*t;
                float gap=Vector2.Distance(p,q);if(gap<best){best=gap;nearest=q;along=(run+t*length)/total;}
                run+=length;
            }
            // Signed across the spine: negative toward the road, positive away from it.
            var direction=(spine[spine.Length-1]-spine[0]).normalized;var normal=new Vector2(-direction.y,direction.x);if(Vector2.Dot(normal,away)<0)normal=-normal;
            float across=Vector2.Dot(p-nearest,normal);float lengthwise=Vector2.Dot(p-nearest,direction);
            float ragged=half*(.85f+.3f*Mathf.PerlinNoise(along*total*.35f+seed,seed*.37f));
            int segment=Mathf.Clamp(Mathf.FloorToInt(along*(tall.Length-1)),0,tall.Length-2);float h=Mathf.Lerp(tall[segment],tall[segment+1],along*(tall.Length-1)-segment);
            // Ends: rounded off past the spine's first and last points.
            float end=along<=.0001f||along>=.9999f?Mathf.Abs(lengthwise):0;
            float face=Smooth(-ragged-.2f,-ragged+.8f,across),rear=1+.3f*Smooth(ragged,ragged+back*.4f,across)-1.3f*Smooth(ragged+back*.35f,ragged+back,across);
            float shape=Mathf.Min(face,Mathf.Max(0,rear))*(1-Smooth(.2f,2.2f,end));
            float crown=.18f*(Mathf.PerlinNoise(p.x*.6f+seed,p.y*.6f)-.5f)*h;
            return Mathf.Max(0,shape*h+crown*shape);
        }
        public static void SorrelRidges()
        {
            const string Root="Sorrel ridge spines";ClearRoot(Root);var root=new GameObject(Root);
            var map=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Textures/sorrel-rock.png");
            var rock=ZoneKit.Toon("sorrel-ridge",map,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.46f,.6f));m.SetColor("_RimColor",new Color(.36f,.26f,.22f));});
            var roads=new[]{SorrelRedesign.Haul,SorrelRedesign.Seam,SorrelRedesign.Service};
            var saved=new HashSet<string>();int stood=0;
            foreach(var (name,spine,tall,half,away,seed,back) in Ridges)
            {
                float reach=half+back+.5f;
                float xMin=spine.Min(p=>p.x)-reach-1,xMax=spine.Max(p=>p.x)+reach+1,zMin=spine.Min(p=>p.y)-reach-1,zMax=spine.Max(p=>p.y)+reach+1;
                // A ridge whose foot comes onto a road, a fight or an ore body (where it rises above the ground by a hand) isn't stood, and says so: ChapterOneTwoLookTests asks for all of them.
                string fault=Fault(name,spine,tall,half,away,seed,back,xMin,xMax,zMin,zMax,roads);
                if(fault!=null){Debug.LogWarning("CH1_LOOK_RIDGE_REFUSED "+fault);continue;}
                var land=GroundTerrain.Build(name,xMin,xMax,zMin,zMax,.4f,p=>SorrelRedesign.Height(p)-.45f+RidgeAt(p,spine,tall,half,away,seed,back),-100,null,new Color(.72f,.62f,.58f),new Color(1,.96f,.92f),_=>-1);
                // Only what stands out of the ground is drawn: the grid's buried skirt would make the ridge's box the whole rectangle round it.
                var drawn=land.cliffs;var at=drawn.vertices;var kept=new List<int>();var tris=drawn.triangles;
                for(int t=0;t<tris.Length;t+=3)
                {
                    float rise=0;for(int k=0;k<3;k++){var v=at[tris[t+k]];rise=Mathf.Max(rise,RidgeAt(new Vector2(v.x,v.z),spine,tall,half,away,seed,back));}
                    if(rise>.3f){kept.Add(tris[t]);kept.Add(tris[t+1]);kept.Add(tris[t+2]);}
                }
                // And only those triangles' corners are kept (D368): a mesh's bounds take every vertex it holds, drawn or not. With the skirt's kept, east 7's box rose to the
                // excavation's cliff tops beside it (11 m), and the scenery fade read it as passing under the lens and faded it whole wherever the camera stood inside its box
                // with the origin (SceneryOcclusion: within 8 m of the lens, over its height less 4), which was most of the haul road: in the editor it stood, in play it was gone.
                var shown=Compact(drawn,kept);Object.DestroyImmediate(drawn);
                // And it stands at its own middle, not at the world's origin: that fade's box starts at the transform's position, so every spine's box ran from the outpost to
                // itself, and east 7's reaches the excavation's rim (it stands on it at its north end) and so still came within 8 m of a lens 12.5 m up on most of the road.
                var middle=shown.bounds.center;middle.y=0;Shift(shown,-middle);Shift(land.collision,-middle);
                string file=name.Replace(' ','-');
                string drawnPath="Assets/_Project/Scenes/Sorrel_Ridges-"+file+".asset",solidPath="Assets/_Project/Scenes/Sorrel_Ridges-"+file+"-collision.asset";saved.Add(drawnPath);saved.Add(solidPath);
                var cliffs=Save(shown,drawnPath);var solid=Save(land.collision,solidPath);
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(root.transform,false);go.transform.position=middle;go.isStatic=true;
                go.GetComponent<MeshFilter>().sharedMesh=cliffs;go.GetComponent<MeshRenderer>().sharedMaterial=rock;go.GetComponent<MeshCollider>().sharedMesh=solid;stood++;
                // The dressing scattered where the ridge now stands would grow out of its rock: a rock or a low ledge deep in it goes (one at its foot stays, fallen from it), and
                // the scrub anywhere in the ridge's drawn box. By its shape alone, plants were left standing in the box at its back and ends, where the outpost's dressing reads a
                // built thing's whole box as built over (QaChapterOneLookTests.PlantsGrowOnOpenGroundNotOutOfPadsAndProps: five plants inside spines in the suite of November 22).
                // Five of the basin's three hundred-odd, one of them on the verge. Cleared here, where the spines are built: a re-dress runs before this does (Restage's order).
                int cleared=ClearPlants(go.GetComponent<MeshRenderer>().bounds);
                foreach(var piece in All<Transform>().Where(t=>t!=null&&Dressing(t)&&!Scrub.Contains(t.name)).ToArray())
                {
                    var p=piece.position;float rise=RidgeAt(new Vector2(p.x,p.z),spine,tall,half,away,seed,back);
                    if(rise>=.6f){Object.DestroyImmediate(piece.gameObject);cleared++;}
                }
                if(cleared>0)Debug.Log($"CH1_LOOK_RIDGE {name} cleared {cleared} pieces of dressing");
                // The paths were baked before it stood: the partner walks round it, not into it. Its whole footprint, from the foot of its face to the end of its back and past its
                // rounded ends: cut round its crest alone, the floor said its back was open, and a walk the floor offered went up the back's steep slope and stopped
                // (QaPlanWorldTests.TheCompanionKeepsUpOnFoot, at the first east spine).
                for(int i=1;i<spine.Length;i++)
                {
                    var a=spine[i-1];var b=spine[i];var mid=(a+b)*.5f;var hole=new GameObject(name+" footprint",typeof(NavMeshObstacle));hole.transform.SetParent(go.transform,false);hole.isStatic=true;
                    hole.transform.SetPositionAndRotation(new Vector3(mid.x,SorrelRedesign.Height(mid)+1,mid.y),Quaternion.LookRotation(new Vector3(b.x-a.x,0,b.y-a.y)));
                    float toBack=Vector3.Dot(hole.transform.right,new Vector3(away.x,0,away.y))>=0?1:-1,face=half*1.15f+.3f,rear=half*1.15f+back;
                    var obstacle=hole.GetComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;
                    // From 2 m under the road to 14 m over it (D368): six metres tall, the box stopped under the basin where it rises behind a spine, and the floor baked there
                    // before the spine stood was left under its back (west 2's at 4.7 m, under rock at 7.2: a body set on it was inside the ridge; TheBodyNeverSticksAtAWallFootOrAProp).
                    obstacle.center=new Vector3(toBack*(rear-face)*.5f,5,0);obstacle.size=new Vector3(face+rear+.4f,16,Vector2.Distance(a,b)+4.4f);obstacle.carving=true;obstacle.carveOnlyStationary=true;
                }
                // Drawn through for a hero behind it, as every tall thing on a walked floor is (D345): its back and its ends can be stood at, and from there the camera's line crosses
                // its crest (QaPlanEarlyTests.TheCameraNeverHidesTheHeroAtTheFootOfAWall).
                ZoneKit.Fadeable(go);
            }
            // A spine the list no longer has leaves its meshes behind under its old name (the first round's "Haul ridge east" and "Haul ridge west"): nothing draws them, so they go.
            foreach(var found in Directory.GetFiles("Assets/_Project/Scenes","Sorrel_Ridges-*-ridge*.asset"))
            {
                string path=found.Replace('\\','/');
                if(!saved.Contains(path)&&AssetDatabase.LoadAssetAtPath<Mesh>(path)!=null){AssetDatabase.DeleteAsset(path);Debug.Log("CH1_LOOK_RIDGE_STALE "+path);}
            }
            // The outpost's rover stood 0.4 m off its hab, and a body pressed into the gap could not walk away (TheBodyNeverSticksAtAWallFootOrAProp, sampled there once these
            // spines changed the floor's edges). RoomDressing puts it against the hab now (no gap, and no floor baked under it), but its step runs only where the dressing is missing, so the built one moves here.
            foreach(var rover in All<Transform>().Where(t=>t!=null&&t.name=="DressRover"&&Vector2.Distance(new Vector2(t.position.x,t.position.z),new Vector2(-15.3f,11.6f))<.3f).ToArray())
                {rover.position+=new Vector3(0,0,.4f);Debug.Log("CH1_LOOK_ROVER moved to "+rover.position);}
            Debug.Log($"CH1_LOOK_RIDGES {stood} of {Ridges.Length}");
        }
        // ---------------------------------------------------------------- Sorrel: the haul road's verge (s04/019, 054)
        /// <summary>The one rock clump the haul road's frames had was the same prop twice (the basin's verge dressing puts the cave's stacked rubble every third piece): at the bend below
        /// the strata and again past the upper ore, at the top of both stills. Below the strata the verge has boulders broken off them now, and past the ore a slab ledge. Each stands
        /// where the rubble stood, solid as it was (the basin's paths were baked round it) and no wider.</summary>
        internal static readonly (Vector2 at,string kit,float size,float yaw)[] Verge={(new Vector2(5,64.6f),"DressBoulders",1.25f,35),(new Vector2(7.5f,119.9f),"DressStrataLedge",1.4f,110)};
        internal const float VergeWidest=2.4f;
        public static void SorrelVerge()
        {
            const string Root="Haul verge rocks";ClearRoot(Root);var root=new GameObject(Root);
            foreach(var (at,kit,size,yaw) in Verge)
            {
                foreach(var old in All<Transform>().Where(t=>t!=null&&t.parent==null&&t.name=="HushwellRubble"&&Vector2.Distance(new Vector2(t.position.x,t.position.z),at)<.8f).ToArray())Object.DestroyImmediate(old.gameObject);
                var piece=WorldBuilder.Piece(kit,Vector3.zero,Vector3.one*size,"Rock",true);piece.transform.rotation=Quaternion.Euler(0,yaw,0);
                var bounds=ModelGeometry.BoundsOf(piece);
                // No wider than the rubble's own spread on the verge: fitted by its height, a ledge ran six metres along it and a boulder pile three.
                float widest=Mathf.Max(bounds.size.x,bounds.size.z);if(widest>VergeWidest){piece.transform.localScale*=VergeWidest/widest;bounds=ModelGeometry.BoundsOf(piece);}
                piece.transform.position+=new Vector3(at.x,SorrelRedesign.Height(at)+.01f,at.y)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                piece.transform.SetParent(root.transform,true);foreach(var t in piece.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;
                var drawn=ModelGeometry.BoundsOf(piece);int cleared=ClearPlants(drawn);
                Debug.Log($"CH1_LOOK_VERGE {kit} at {at} {drawn.size.x:0.0} by {drawn.size.z:0.0}, {drawn.size.y:0.0} tall, cleared {cleared} plants");
            }
        }
        /// <summary>Plants that can't grow out of rock, and the low rocks and spoil a ridge would swallow, wherever they were put down: the room dressing, or the basin's own verge.</summary>
        static readonly HashSet<string> Scrub=new(){"DressSucculent","DressFanShrub"};
        static readonly HashSet<string> Swallowed=new(){"DressBoulders","DressStrataLedge","DressSpoilHeap","HushwellRubble","HushwellLowRidge"};
        /// <summary>The outpost's scrub standing in a built thing's drawn box, with the margin the plant check allows (.3 m) and a little over: it goes.</summary>
        static int ClearPlants(Bounds box)
        {
            int cleared=0;
            foreach(var plant in All<Transform>().Where(t=>t!=null&&Scrub.Contains(t.name)&&t.parent!=null&&t.parent.name=="Room dressing").ToArray())
            {
                var p=plant.position;
                if(p.x>box.min.x-.45f&&p.x<box.max.x+.45f&&p.z>box.min.z-.45f&&p.z<box.max.z+.45f){Object.DestroyImmediate(plant.gameObject);cleared++;}
            }
            return cleared;
        }
        static bool Dressing(Transform t)=>(Scrub.Contains(t.name)||Swallowed.Contains(t.name))&&(t.parent==null||t.parent.name=="Room dressing");
        internal static string Fault(string name,Vector2[] spine,float[] tall,float half,Vector2 away,int seed,float back,float xMin,float xMax,float zMin,float zMax,Vector2[][] roads)
        {
            for(float x=xMin;x<=xMax;x+=.5f)for(float z=zMin;z<=zMax;z+=.5f)
            {
                var p=new Vector2(x,z);if(RidgeAt(p,spine,tall,half,away,seed,back)<.1f)continue;
                foreach(var road in roads)if(Distance(p,road)<RidgeFromRoad-.6f)return $"{name} stands on a road at {p}";
                foreach(var fight in SorrelRedesign.Encounters)if(Vector2.Distance(p,fight)<RidgeFromFight-1.6f)return $"{name} stands in a fight at {p}";
                foreach(var ore in SorrelRedesign.Mines)if(Vector2.Distance(p,ore)<RidgeFromOre)return $"{name} stands on ore at {p}";
            }
            return null;
        }
        internal static float Distance(Vector2 p,Vector2[] path)
        {
            float best=float.PositiveInfinity;
            for(int i=1;i<path.Length;i++){var a=path[i-1];var d=path[i]-a;var q=a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);best=Mathf.Min(best,Vector2.Distance(p,q));}
            return best;
        }
        /// <summary>The mesh of only these triangles: their corners and nothing else, so its bounds are what is drawn.</summary>
        static Mesh Compact(Mesh source,List<int> tris)
        {
            var map=new Dictionary<int,int>();var used=new List<int>();var index=new int[tris.Count];
            for(int i=0;i<tris.Count;i++){if(!map.TryGetValue(tris[i],out int at)){at=used.Count;map[tris[i]]=at;used.Add(tris[i]);}index[i]=at;}
            var mesh=new Mesh{name=source.name};var v=source.vertices;mesh.vertices=used.Select(i=>v[i]).ToArray();
            var n=source.normals;if(n.Length==v.Length)mesh.normals=used.Select(i=>n[i]).ToArray();
            var t=source.tangents;if(t.Length==v.Length)mesh.tangents=used.Select(i=>t[i]).ToArray();
            var c=source.colors;if(c.Length==v.Length)mesh.colors=used.Select(i=>c[i]).ToArray();
            var uv=source.uv;if(uv.Length==v.Length)mesh.uv=used.Select(i=>uv[i]).ToArray();
            var uv2=source.uv2;if(uv2.Length==v.Length)mesh.uv2=used.Select(i=>uv2[i]).ToArray();
            mesh.SetTriangles(index,0);mesh.RecalculateBounds();return mesh;
        }
        static void Shift(Mesh mesh,Vector3 by){var v=mesh.vertices;for(int i=0;i<v.Length;i++)v[i]+=by;mesh.vertices=v;mesh.RecalculateBounds();}
        static Mesh Save(Mesh mesh,string path)
        {
            var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(prior==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,prior);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(prior);return prior;
        }

        // ---------------------------------------------------------------- bodies on a dark floor (s08/021, s14/017, s15/049)
        /// <summary>A body that has to read on a dark floor: a rim that stays on in shade and a little light it never drops below, the same answer the heroes have. The Scrapmites'
        /// dark shells on Hushwell's dark floor, the Gloomgrazers' purple on the relay's and the gorge's dusk. Set on the body's own material, every run.</summary>
        internal static void BodyReads(string enemy,Color rim,float rimInShade,float leastLight,float rimPower=3)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Models+enemy+"/"+enemy+"_Toon.mat");if(material==null){Debug.LogWarning("CH1_LOOK no body material for "+enemy);return;}
            material.SetColor("_RimColor",rim);material.SetFloat("_RimUnlit",rimInShade);material.SetFloat("_LightFloor",leastLight);material.SetFloat("_RimPower",rimPower);EditorUtility.SetDirty(material);
        }
        public static void Bodies()=>BodyReads("Scrapmite",new Color(.95f,.78f,.5f),.55f,.22f);

        // ---------------------------------------------------------------- flight: what lies below the plane
        /// <summary>Where something below the flight plane shows on the flight camera's screen, as a point of the plane: south of where it is, by its depth over the camera's pitch.</summary>
        internal static Vector2 Shows(Vector3 p)=>new(p.x,p.z-(1-p.y)/Mathf.Tan(48*Mathf.Deg2Rad));
        /// <summary>Everything a flight zone has to keep readable: every prompt (warps, docks, tenders, hails), every spawn and every sign. Nothing below the plane shows within this of one.</summary>
        internal static List<Vector2> Readable()
        {
            var keep=new List<Vector2>();
            foreach(var prompt in All<InteractionPrompt>())keep.Add(new Vector2(prompt.transform.position.x,prompt.transform.position.z));
            foreach(var spawn in All<SpawnPoint>())keep.Add(new Vector2(spawn.transform.position.x,spawn.transform.position.z));
            foreach(var sign in All<TextMeshPro>())keep.Add(new Vector2(sign.transform.position.x,sign.transform.position.z));
            return keep;
        }
        /// <summary>A field of drawn-only pieces below the flight plane, deeper ones bigger, each its own silhouette and turn, kept off what the zone must keep readable. The parallax
        /// between depths is what gives a flight frame its depth. Static, so the build batches them.</summary>
        internal static int Field(string rootName,int seed,int count,Func<System.Random,Vector3?> pick,(string key,float small,float large)[] kinds,Material[] looks,Color[] tints,List<Vector2> keep,float clear,Func<Vector3,bool> also=null)
        {
            ClearRoot(rootName);var root=new GameObject(rootName);root.isStatic=true;var rng=new System.Random(seed);int placed=0;
            for(int tries=0;placed<count&&tries<count*20;tries++)
            {
                var at=pick(rng);if(at==null)continue;var p=at.Value;
                var (key,small,large)=kinds[rng.Next(kinds.Length)];float depth=Mathf.InverseLerp(-6,-80,p.y);float size=Mathf.Lerp(small,large,Mathf.Lerp((float)rng.NextDouble(),1,depth*.6f));
                var shows=Shows(p);if(keep.Any(k=>Vector2.Distance(k,shows)<clear+size))continue;if(also!=null&&!also(p))continue;
                if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/"+key+".prefab")==null)continue;
                var piece=WorldBuilder.Piece(key,p,Vector3.one*size,"Rock",false);piece.transform.rotation=Quaternion.Euler((float)rng.NextDouble()*360,(float)rng.NextDouble()*360,(float)rng.NextDouble()*360);
                piece.name=rootName+" piece";piece.transform.SetParent(root.transform,true);foreach(var t in piece.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;
                var look=looks[rng.Next(looks.Length)];if(look!=null)Wear(piece,look);
                var tint=tints[rng.Next(tints.Length)];if(tint!=Color.white)piece.AddComponent<PropTint>().tint=tint;
                foreach(var part in piece.GetComponentsInChildren<Renderer>(true)){part.shadowCastingMode=ShadowCastingMode.Off;part.receiveShadows=false;}
                placed++;
            }
            Debug.Log($"CH1_LOOK_FIELD {rootName} placed={placed}");
            return placed;
        }
        /// <summary>Dark rock split by cracks that glow: cells of a tiling Voronoi, their walls the cracks. Painted once, deterministic.</summary>
        internal static (Texture2D rock,Texture2D glow) Cinder()
        {
            const int Cells=7;var points=new Vector2[Cells*Cells];var rng=new System.Random(907);
            for(int y=0;y<Cells;y++)for(int x=0;x<Cells;x++)points[y*Cells+x]=new Vector2(x+.15f+.7f*(float)rng.NextDouble(),y+.15f+.7f*(float)rng.NextDouble());
            float Crack(float u,float v)
            {
                float px=u*Cells,py=v*Cells,first=float.PositiveInfinity,second=float.PositiveInfinity;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    int cx=Mathf.FloorToInt(px)+dx,cy=Mathf.FloorToInt(py)+dy;var c=points[((cy%Cells+Cells)%Cells)*Cells+(cx%Cells+Cells)%Cells];
                    var at=new Vector2(c.x+Mathf.Floor((float)cx/Cells)*Cells,c.y+Mathf.Floor((float)cy/Cells)*Cells);float d=Vector2.Distance(at,new Vector2(px,py));
                    if(d<first){second=first;first=d;}else if(d<second)second=d;
                }
                return 1-Smooth(.02f,.11f,second-first);
            }
            var rock=ZoneKit.Texture("Halo/cinder-rock",512,(u,v)=>{float c=Crack(u,v),n=ZoneKit.Cloud(u,v,8,3);float g=Mathf.Lerp(.42f+.2f*n,.12f,c);return new Color(g*1.05f,g*.92f,g*.86f);});
            var glow=ZoneKit.Texture("Halo/cinder-glow",512,(u,v)=>{float c=Crack(u,v)*Mathf.Lerp(.55f,1,ZoneKit.Cloud(u,v,4,9));return new Color(c,c,c);});
            return (rock,glow);
        }

        // ---------------------------------------------------------------- the Cinder Halo (s09/009, s02)
        /// <summary>The game's first flight was black space, a few stars and a planet. The Halo is named for what it sits in: a belt of cinders, rock still smouldering from whatever
        /// broke it, drifting under the station in a broad band, near pieces small and quick across the screen, far ones large and slow, their cracks glowing. Below the plane,
        /// drawn only, kept off every warp, dock, hail and sign.</summary>
        /// <summary>Where the flight camera stands for ships north of the station, the Gullet's warp and the tender: what is seen from there.</summary>
        static readonly Vector3[] Eyes={new(-24,20.3f,100),new(0,20.3f,100),new(24,20.3f,100),new(-24,20.3f,125),new(0,20.3f,125),new(24,20.3f,125),new(0,20.3f,150)};
        public static void Halo()
        {
            var (rock,glow)=Cinder();
            var cinder=ZoneKit.Toon("halo-cinder",rock,m=>{m.SetColor("_BaseColor",new Color(.62f,.52f,.5f));m.SetFloat("_RampSteps",3);m.SetColor("_ShadowTint",new Color(.32f,.3f,.42f));
                m.SetTexture("_EmissionMap",glow);m.SetColor("_EmissionColor",new Color(3.4f,1f,.28f));m.SetColor("_RimColor",new Color(.55f,.3f,.2f));m.SetFloat("_RimPower",4);});
            var ash=ZoneKit.Toon("halo-cinder-cold",rock,m=>{m.SetColor("_BaseColor",new Color(.7f,.64f,.62f));m.SetFloat("_RampSteps",3);m.SetColor("_ShadowTint",new Color(.3f,.3f,.42f));
                m.SetTexture("_EmissionMap",glow);m.SetColor("_EmissionColor",new Color(.5f,.14f,.05f));m.SetColor("_RimColor",new Color(.4f,.34f,.36f));m.SetFloat("_RimPower",4);});
            var keep=Readable();var planet=GameObject.Find("Vorun")?.transform;
            // The belt runs north-west to south-east across the whole zone, well past the station on both sides; the station's own footprint is left to the station.
            var dir=new Vector2(1,-.62f).normalized;var across=new Vector2(-dir.y,dir.x);var centre=new Vector2(0,150);
            Vector3? Pick(System.Random r)
            {
                float along=((float)r.NextDouble()-.5f)*380,width=((float)r.NextDouble()+(float)r.NextDouble()-1)*52;var p=centre+dir*along+across*width;
                float depth=-10-Mathf.Pow((float)r.NextDouble(),1.6f)*70;
                if(Mathf.Abs(p.x)<58&&p.y>-70&&p.y<40)return null;
                var at=new Vector3(p.x,depth,p.y);
                // Nothing across the face of the planet below, from anywhere a ship comes out north of the station.
                if(planet!=null)foreach(var eye in Eyes){var toPlanet=planet.position-eye;float radius=Mathf.Asin(Mathf.Clamp01(planet.lossyScale.x*.5f/toPlanet.magnitude))*Mathf.Rad2Deg;if(Vector3.Angle(at-eye,toPlanet)<radius+3)return null;}
                return at;
            }
            Field("Cinder Halo belt",1903,170,Pick,new[]{("RidgeRock1",.8f,3.4f),("RidgeRock2",.9f,3.8f),("RidgeRock3",.7f,3f),("DressBoulders",1f,5.5f),("HushwellRubble",1f,5f)},
                new[]{cinder,cinder,ash},new[]{Color.white},keep,9);
        }

        // ---------------------------------------------------------------- Tallow's approach (s27/111, s10b/185)
        /// <summary>Tallow's approach was the hull at the top of the frame and black under it. A station that has been drifting for years trails what it has lost: a slow cloud of
        /// its own salvage and ice below and around it, panels, tanks, crates, pallets, solar wings, lit here and there by a running light still on. Below the plane, drawn only,
        /// kept off every warp, dock and sign.</summary>
        public static void Approach()
        {
            var keep=Readable();var hull=GameObject.Find("TallowStationHull");var middle=hull!=null?new Vector2(hull.transform.position.x,hull.transform.position.z):new Vector2(0,30);
            var steel=ZoneKit.Toon("tallow-drift-steel",null,m=>{m.SetColor("_BaseColor",new Color(.56f,.58f,.64f));m.SetColor("_ShadowTint",new Color(.32f,.34f,.48f));m.SetColor("_RimColor",new Color(.5f,.6f,.78f));m.SetFloat("_RimPower",3.5f);});
            var ice=ZoneKit.Toon("tallow-drift-ice",null,m=>{m.SetColor("_BaseColor",new Color(.5f,.6f,.7f));m.SetColor("_ShadowTint",new Color(.38f,.46f,.62f));m.SetColor("_EmissionColor",new Color(.03f,.07f,.1f));m.SetColor("_RimColor",new Color(.7f,.9f,1));m.SetFloat("_RimPower",2.6f);});
            Vector3? Pick(System.Random r)
            {
                float a=(float)r.NextDouble()*Mathf.PI*2,reach=24+Mathf.Pow((float)r.NextDouble(),1.2f)*80;var p=middle+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*reach;
                return new Vector3(p.x,-8-Mathf.Pow((float)r.NextDouble(),1.4f)*60,p.y);
            }
            // The salvage keeps its own paint (null look); a share of it wears the drift's cold steel, and the ice its own.
            Field("Tallow drift salvage",2207,120,Pick,new[]{("DressCargoPallet",1.2f,3.6f),("DressFuelTank",1.4f,4.5f),("HushwellCrates",1.2f,3.4f),("DressSolarArray",2f,6f),("DressPowerCell",.9f,2.4f),("DressCableDrum",1f,2.6f)},
                new Material[]{null,null,steel},new[]{Color.white,new Color(.82f,.8f,.84f),new Color(.7f,.72f,.78f)},keep,8);
            Field("Tallow drift ice",2311,30,Pick,new[]{("HushwellRubble",1.2f,4.5f),("DressBoulders",1,4f)},new[]{ice},new[]{Color.white},keep,8);
        }

        // ---------------------------------------------------------------- the Gullet's salvage eddy (s10b/049)
        /// <summary>The eddy's loose cargo was three of the deck's crates in a weathered grey: from the drawn-back camera of its fight, a flat grey box among the mines. What the current
        /// left here is a ship's spilled hold, each piece its own shape and tumbled as it settled: a crate stack, a fuel tank, a pallet, with the cache's warm lift in its paint.
        /// Each keeps a solid box the size of what is drawn, where the crate's stood.</summary>
        public static void Gullet()
        {
            float eddyX=GulletProfile.RightEdge(GulletProfile.CacheZ)-9;
            var loose=All<Transform>().Where(t=>t!=null&&t.parent==null&&(t.name=="DeckCrate"||t.name=="Eddy salvage")&&Mathf.Abs(t.position.z-GulletProfile.CacheZ-10)<12&&Mathf.Abs(t.position.x-eddyX-5)<8).OrderBy(t=>t.position.z).ToArray();
            string[] kinds={"HushwellCrates","DressFuelTank","DressCargoPallet"};float[] sizes={1.5f,1.6f,1.3f};
            var paint=new Dictionary<string,Material>();
            for(int i=0;i<loose.Length;i++)
            {
                var wrapper=loose[i];string kind=kinds[i%kinds.Length];
                for(int c=wrapper.childCount-1;c>=0;c--)Object.DestroyImmediate(wrapper.GetChild(c).gameObject);
                foreach(var old in wrapper.GetComponents<Collider>())Object.DestroyImmediate(old);
                wrapper.rotation=Quaternion.identity;var at=wrapper.position;wrapper.name="Eddy salvage";
                var piece=WorldBuilder.Piece(kind,at,Vector3.one*sizes[i%sizes.Length],"Rock",false);
                // Tumbled as it settled, not set square on a shelf.
                piece.transform.rotation=Quaternion.Euler(18+i*11,i*97+40,-14+i*19);
                // No bigger across than the crate it replaces was: the eddy's flight lines were flown past those.
                var across=new Bounds(piece.transform.position,Vector3.zero);foreach(var r in piece.GetComponentsInChildren<Renderer>())across.Encapsulate(r.bounds);
                float widest=Mathf.Max(across.size.x,across.size.z);if(widest>1.9f)piece.transform.localScale*=1.9f/widest;
                if(!paint.TryGetValue(kind,out var look))
                {
                    var source=AssetDatabase.LoadAssetAtPath<Material>(Models+kind+"/"+kind+"_Toon.mat");
                    look=paint[kind]=source==null?null:ZoneKit.Toon("gullet-salvage-"+kind.ToLowerInvariant(),source.GetTexture("_BaseMap") as Texture2D,m=>{m.SetColor("_BaseColor",new Color(.72f,.66f,.6f));m.SetTexture("_EmissionMap",source.GetTexture("_EmissionMap"));m.SetColor("_EmissionColor",new Color(.04f,.16f,.18f));m.SetColor("_RimColor",new Color(.4f,.55f,.7f));});
                }
                if(look!=null)Wear(piece,look);
                var drawn=new Bounds(piece.transform.position,Vector3.zero);foreach(var r in piece.GetComponentsInChildren<Renderer>())drawn.Encapsulate(r.bounds);
                var body=piece.transform.GetChild(0);body.SetParent(wrapper,true);Object.DestroyImmediate(piece);
                var box=wrapper.gameObject.AddComponent<BoxCollider>();box.center=wrapper.InverseTransformPoint(drawn.center);
                var scale=wrapper.lossyScale;box.size=new Vector3(drawn.size.x/Mathf.Abs(scale.x),drawn.size.y/Mathf.Abs(scale.y),drawn.size.z/Mathf.Abs(scale.z));
            }
            Debug.Log("CH1_LOOK_EDDY salvage="+loose.Length);
        }

        // ---------------------------------------------------------------- the Cantor's coil (nov22-whole s10/159 to 176)
        /// <summary>After the Cantor the camera comes back in to the flight's own distance, and the coil's bed, which its fight had drawn far back over, was twenty seconds of the same
        /// blue cells with two ships on them: the coil had four moorings and its fold rings and nothing else. What the current carries round a coil settles where it slows, on the bed
        /// at the outer fold: eddies of what the Cantor caught, a ship's hold spilled and tumbled (the eddy's salvage, in the same cold paint), here and there a skiff's hull on its side. (No
        /// Choir pods: small, pale and glowing cyan on the bed, from above they read as the darts that fight in here.) All of it lies on the bed, five metres and more under the flight plane, drawn only, with nothing a ship can touch, kept off the moorings'
        /// sockets and the middle where the Cantor rises, and a little off where every prompt and sign shows. Its own root, so it can be hidden to see the capture's frame again.</summary>
        internal const string CoilBed="The coil's bed";
        /// <summary>Each eddy's middle on the bed, and how many pieces it holds. Three lie in the frame the replay waits in after the fight (12 to 20 m east, 764 north), round the
        /// pocket where the collar is studied, which is kept clear.</summary>
        internal static readonly (Vector2 at,int pieces,bool hull)[] CoilEddies=
        {
            (new Vector2(5,775),6,false),(new Vector2(12,762),5,false),(new Vector2(27,770),5,false),(new Vector2(32,757),4,false),(new Vector2(-21,760),6,false),(new Vector2(-6,737),5,false),
            (new Vector2(16,731),5,false),(new Vector2(29,803),5,false),(new Vector2(-29,804),6,true),(new Vector2(-4,843),5,false),(new Vector2(17,848),5,false),(new Vector2(-19,846),4,false),
        };
        public static void GulletCoil()
        {
            ClearRoot(CoilBed);var root=new GameObject(CoilBed);root.isStatic=true;var rng=new System.Random(805);
            // What has to stay readable is what is drawn or asked for: every prompt and sign. A spawn is a point nobody sees.
            var keep=All<InteractionPrompt>().Select(p=>new Vector2(p.transform.position.x,p.transform.position.z)).Concat(All<TextMeshPro>().Select(t=>new Vector2(t.transform.position.x,t.transform.position.z))).ToList();
            (string key,float small,float large)[] hold={("HushwellCrates",1.2f,1.9f),("DressFuelTank",1.3f,2.4f),("DressCargoPallet",1.1f,1.9f),("DressCableDrum",.9f,1.5f),("DressPowerCell",.8f,1.3f),("DressSolarArray",1.6f,2.8f)};
            var looks=new Dictionary<string,Material>();
            Material Look(string kind)
            {
                if(looks.TryGetValue(kind,out var look))return look;
                var source=AssetDatabase.LoadAssetAtPath<Material>(Models+kind+"/"+kind+"_Toon.mat");
                // The eddy's cold paint: the deep coil's light is a dim violet, and a hold in its own bright paint was a hole of colour on the bed.
                look=looks[kind]=source==null?null:ZoneKit.Toon("gullet-coil-"+kind.ToLowerInvariant(),source.GetTexture("_BaseMap") as Texture2D,m=>{m.SetColor("_BaseColor",new Color(.62f,.6f,.68f));
                    if(source.HasProperty("_EmissionMap"))m.SetTexture("_EmissionMap",source.GetTexture("_EmissionMap"));m.SetColor("_EmissionColor",new Color(.03f,.12f,.16f));m.SetColor("_RimColor",new Color(.38f,.5f,.72f));m.SetFloat("_RimPower",3.2f);});
                return look;
            }
            bool Clear(Vector3 p,float size)
            {
                // Three metres and a little off where a prompt or sign shows: seven metres down, a piece under one is the floor, not over its words.
                var shows=Shows(p);if(keep.Any(k=>Vector2.Distance(k,shows)<3+size*.5f))return false;
                // Off the moorings' pressed sockets, and off the middle where the Cantor comes up out of the bed.
                foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(side*18,805+end*20))<11)return false;
                return Vector2.Distance(new Vector2(p.x,p.z),new Vector2(0,805))>12;
            }
            GameObject Lay(string key,Vector3 at,Vector3 envelope,Quaternion turn,Material look,Color tint)
            {
                var piece=WorldBuilder.Piece(key,Vector3.zero,envelope,"Rock",false);piece.transform.rotation=turn;
                var bounds=ModelGeometry.BoundsOf(piece);
                // Bedded a fifth of its height into the tissue, as a thing that has lain there does.
                piece.transform.position+=at-new Vector3(bounds.center.x,bounds.min.y+bounds.size.y*.2f,bounds.center.z);
                var top=ModelGeometry.BoundsOf(piece).max.y;if(top>-1.5f)piece.transform.position+=Vector3.down*(top+1.5f);
                piece.name=CoilBed+" "+key;piece.transform.SetParent(root.transform,true);
                foreach(var t in piece.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;
                foreach(var c in piece.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
                if(look!=null)Wear(piece,look);if(tint!=Color.white)piece.AddComponent<PropTint>().tint=tint;
                return piece;
            }
            Color[] tints={Color.white,new(.84f,.82f,.92f),new(.72f,.72f,.84f)};int laid=0,hulls=0;
            foreach(var (middle,count,hull) in CoilEddies)
            {
                if(hull)
                {
                    var at=new Vector3(middle.x,GulletCoilArt.Height(middle.x,middle.y),middle.y);
                    // On its side and in the coil's cold paint, its running lights dead: lying level with its glow on, from above it was one more ship.
                    if(Clear(at,3)){Lay("Skiff",at,new Vector3(3.6f,1.3f,5.4f),Quaternion.Euler(20+(float)rng.NextDouble()*20,(float)rng.NextDouble()*360,(rng.Next(2)==0?-1:1)*(55+(float)rng.NextDouble()*20)),Look("Skiff"),new Color(.56f,.54f,.64f));hulls++;}
                }
                for(int i=0;i<count;i++)
                {
                    var (key,small,large)=hold[rng.Next(hold.Length)];float size=small+(float)rng.NextDouble()*(large-small);
                    float a=(float)rng.NextDouble()*Mathf.PI*2,r=(hull?3.4f:1.2f)+(float)rng.NextDouble()*3.4f;var p=middle+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
                    var at=new Vector3(p.x,GulletCoilArt.Height(p.x,p.y),p.y);if(!Clear(at,size))continue;
                    // Tumbled as it settled: tilted, never stood square.
                    var turn=Quaternion.Euler(-28+(float)rng.NextDouble()*56,(float)rng.NextDouble()*360,-28+(float)rng.NextDouble()*56);
                    Lay(key,at,Vector3.one*size,turn,Look(key),tints[rng.Next(tints.Length)]);laid++;
                }
            }
            Debug.Log($"CH1_LOOK_COIL eddies={CoilEddies.Length} salvage={laid} hulls={hulls}");
        }
    }
}
