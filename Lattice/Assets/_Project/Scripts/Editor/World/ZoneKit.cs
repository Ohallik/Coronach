using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// What every region builder builds a zone with, lifted out of chapter 2's <see cref="LanternRoadBuilder"/> so chapters 3 to 10 share one copy. A kit is one zone's context: its name,
    /// its ground (<see cref="height"/>), where level ground stops being floor (<see cref="cliffTop"/>) and whether tall props fade for the ground camera (<see cref="fadeTall"/>).
    /// A builder makes one before <c>WorldBuilder.Begin</c> and calls everything through it, so nothing is left over from the zone built before:
    /// <code>
    /// var kit=ZoneKit.Ground("Rootmarket",RootHeight,3.2f);var scene=WorldBuilder.Begin(kit.zone);
    /// kit.Terrain("Root platforms",-75,75,-20,100,.5f,floorMaterial,wallMaterial,RootTone);
    /// kit.Prop(ZoneKit.Kit("SeedHouse","CanyonHouse"),new Vector3(0,0,72),9,180);
    /// </code>
    /// Positions are x,z on the plan with y as an offset above the ground, as LRB's were. What needs no zone (a kit key, a sightline, a mesh on disk) is static.
    /// Files: this one (context, props, people, ways out, navigation), <c>ZoneKit.Flight.cs</c> (space zones), <c>ZoneKit.Shape.cs</c> (meshes built in code) and
    /// <c>ZoneKit.Paint.cs</c> (textures and materials made in code). The landform itself is <see cref="GroundTerrain"/>'s.
    /// </summary>
    public sealed partial class ZoneKit
    {
        // ---------------------------------------------------------------- the zone's context
        public readonly string zone;
        /// <summary>The ground under a point of the plan. Flight zones leave it at nothing.</summary>
        public Func<Vector2,float> height=_=>0;
        /// <summary>Ground above this height that is level is "above the cliffs" (wall material, not floor).</summary>
        public float cliffTop=3.5f;
        /// <summary>Ground zones: the camera flies over the town, and anything <see cref="TallEnoughToBlock"/> can come between it and the hero.</summary>
        public bool fadeTall;
        /// <summary>Stand-ins whose proportions differ from the generated piece they replace are scaled to its footprint.</summary>
        public readonly Dictionary<string,float> standIn=new(){{"OutpostHab",.55f},{"HushwellColumn",.8f}};
        ZoneKit(string zone){this.zone=zone;}
        public static ZoneKit Ground(string zone,Func<Vector2,float> height,float cliffTop)=>new(zone){height=height,cliffTop=cliffTop,fadeTall=true};
        public static ZoneKit Flight(string zone)=>new(zone);
        public float Y(float x,float z)=>height(new Vector2(x,z));
        /// <summary>A plan position (y an offset above the ground) as a point of the world.</summary>
        public Vector3 On(Vector3 at)=>new(at.x,Y(at.x,at.z)+at.y,at.z);
        public Vector3 On(float x,float z,float up=0)=>new(x,Y(x,z)+up,z);

        // ---------------------------------------------------------------- what needs no zone
        public const float TallEnoughToBlock=2.5f;
        /// <summary>The rule, and QaWorldSecondTests' check of it: no piece standing more than 3 m over the floor crosses the fixed camera's line to a chest on the floor of a fight or the walked path.</summary>
        public const float TallOverAFight=3;
        /// <summary>Kit rocks are fitted by height, and the wide ones are three to six times as wide as they stand. No rock seated in a wall is wider than this, and one is walked back until its whole footprint clears the floor by <see cref="CliffRockClear"/>.</summary>
        public const float WidestCliffRock=8,CliffRockClear=11.5f,MostSeatWalk=9;
        /// <summary>The kit's plants stand on a jagged disc of their own, up to 15% of their height: sunk this far it's under the ground and the plant starts at its leaves.</summary>
        public const float PlantSkirt=.16f;
        public static readonly HashSet<string> Skirted=new(){"DressFanShrub","DressSucculent"};
        public static T Find<T>(string id)where T:ScriptableObject=>AssetDatabase.LoadAssetAtPath<T>("Assets/_Project/Resources/Definitions/"+typeof(T).Name+"/"+id+".asset");
        public static GameObject Prefab(string folder,string id)=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/"+folder+"/"+id+".prefab");
        public static GameObject Body(string folder,string id,string fallbackFolder,string fallback)=>Prefab(folder,id)??Prefab(fallbackFolder,fallback);
        /// <summary>The key if its prefab has been taken in (a prop or a ship), else the fallback: a zone is walkable before its art lands.</summary>
        public static string Kit(string key,string fallback)=>Prefab("Environment",key)!=null||Prefab("Ships",key)!=null?key:fallback;
        public static bool Landed(string key)=>Prefab("Environment",key)!=null||Prefab("Ships",key)!=null;
        public static Material WorldMaterial(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/"+name+".mat");
        public static float Smooth(float edge0,float edge1,float x)=>Mathf.SmoothStep(0,1,Mathf.Clamp01((x-edge0)/(edge1-edge0)));
        public static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));return Vector2.Distance(p,a+d*t);}
        /// <summary>The nearest a point comes to a path of straight legs.</summary>
        public static float FromPath(Vector2 p,IReadOnlyList<Vector2> path){float d=float.PositiveInfinity;for(int i=1;i<path.Count;i++)d=Mathf.Min(d,Segment(p,path[i-1],path[i]));return d;}
        /// <summary>By its drawn box, the one the camera's fade and the checks go by.</summary>
        public static Bounds Drawn(GameObject go){var parts=go.GetComponentsInChildren<Renderer>();var all=parts[0].bounds;foreach(var part in parts)all.Encapsulate(part.bounds);return all;}
        public static Mesh SaveMesh(Mesh mesh,string path)
        {
            var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(prior==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,prior);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(prior);return prior;
        }
        /// <summary>On a floor sign's ground as the sign style lays it, or close enough on the camera's side to stand across its words. Each zone lists its own signs.</summary>
        public static bool OnASign(Vector2 p,(Vector2 at,float half)[] signs)
        {
            foreach(var (at,half) in signs)if(Mathf.Abs(p.x-at.x)<half+2&&p.y>at.y-3.5f&&p.y<at.y+2)return true;
            return false;
        }
        /// <summary>Close to someone, or on the floor between them and the fixed camera: from there a waist-high barrel hides their legs.</summary>
        public static bool InSightOf(Vector2 p,Vector2 person,float reach,float length,float half)
        {
            var offset=p-person;if(offset.magnitude<reach)return true;
            var toCamera=new Vector2(-Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),-Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));
            float along=Vector2.Dot(offset,toCamera),across=Mathf.Abs(offset.x*toCamera.y-offset.y*toCamera.x);
            return along>0&&along<length&&across<half;
        }
        /// <summary>Whether a box this tall stands on the fixed camera's line to a chest at any of these floor points.</summary>
        public static bool HidesFloor(Bounds box,List<Vector3> floor)
        {
            foreach(var at in floor)
            {
                if(box.max.y-at.y<=TallOverAFight)continue;
                var chest=at+Vector3.up*SightlineAudit.Chest;
                if(box.IntersectRay(new Ray(chest+SightlineAudit.ToCamera*SightlineAudit.Distance,-SightlineAudit.ToCamera),out float along)&&along<SightlineAudit.Distance-SightlineAudit.NearIgnore)return true;
            }
            return false;
        }
        /// <summary>How far north of a thing this tall the fixed camera loses a hero's chest behind it: where a walk may pass behind something without it fading.</summary>
        public static float Shadowed(float tall)=>Mathf.Max(0,tall-SightlineAudit.Chest)/Mathf.Tan(SightlineAudit.Pitch*Mathf.Deg2Rad);
        /// <summary>What's really there, as cells a hand or two across in the piece's own space, for SceneryOcclusion to ask in play. In play a static piece's mesh is its whole merged batch, so this has to be taken here. A fruit tree's box has nothing in its corners.</summary>
        static void MeasureShape(SceneryOcclusion fade,GameObject go)
        {
            var toLocal=go.transform.worldToLocalMatrix;var corners=new List<Vector3>();var low=Vector3.one*float.MaxValue;var high=-low;
            foreach(var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;if(mesh==null)continue;
                var to=toLocal*filter.transform.localToWorldMatrix;var at=mesh.vertices;
                foreach(int i in mesh.triangles){var p=to.MultiplyPoint3x4(at[i]);corners.Add(p);low=Vector3.Min(low,p);high=Vector3.Max(high,p);}
            }
            if(corners.Count==0)return;
            var size=high-low;float cell=Mathf.Max(.3f,Mathf.Max(size.x,Mathf.Max(size.y,size.z))/16);
            int nx=Mathf.Max(1,Mathf.CeilToInt(size.x/cell)),ny=Mathf.Max(1,Mathf.CeilToInt(size.y/cell)),nz=Mathf.Max(1,Mathf.CeilToInt(size.z/cell));
            var filled=new byte[(nx*ny*nz+7)/8];
            for(int t=0;t<corners.Count;t+=3)
            {
                Vector3 a=corners[t],b=corners[t+1]-a,c=corners[t+2]-a;
                int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(b.magnitude,Mathf.Max(c.magnitude,(c-b).magnitude))/(cell*.5f)));
                for(int i=0;i<=steps;i++)for(int j=0;j<=steps-i;j++)
                {
                    var p=a+b*(i/(float)steps)+c*(j/(float)steps)-low;
                    int bit=(Mathf.Min(nz-1,(int)(p.z/cell))*ny+Mathf.Min(ny-1,(int)(p.y/cell)))*nx+Mathf.Min(nx-1,(int)(p.x/cell));
                    filled[bit>>3]|=(byte)(1<<(bit&7));
                }
            }
            fade.shape=filled;fade.shapeLow=low;fade.shapeCell=cell;fade.shapeX=nx;fade.shapeY=ny;fade.shapeZ=nz;
        }
        /// <summary>Lets the ground camera open a hero-sized hole in this piece, or fade it whole under the lens. Turns the fade on in its materials (the variant has to be on a material in the build) and measures its real shape.</summary>
        public static void Fadeable(GameObject go)
        {
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
            {
                if(material==null||!material.HasProperty("_OccFade")||material.IsKeywordEnabled("_OCCFADE_ON"))continue;
                material.EnableKeyword("_OCCFADE_ON");material.SetFloat("_OccFadeToggle",1);material.SetFloat("_OccFade",0);EditorUtility.SetDirty(material);
            }
            if(go.GetComponent<SceneryOcclusion>()==null)MeasureShape(go.AddComponent<SceneryOcclusion>(),go);
        }
        /// <summary>A line of light or of rope between two points, in its parent's space.</summary>
        public static LineRenderer Strand(Transform parent,Vector3 from,Vector3 to,Material beam,float width,Color bottom,Color top)
        {
            var line=new GameObject("Curtain strand",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(parent,true);
            line.useWorldSpace=false;line.positionCount=2;line.SetPosition(0,line.transform.InverseTransformPoint(from));line.SetPosition(1,line.transform.InverseTransformPoint(to));
            line.startWidth=line.endWidth=width;line.sharedMaterial=beam;line.startColor=bottom;line.endColor=top;line.shadowCastingMode=ShadowCastingMode.Off;return line;
        }

        // ---------------------------------------------------------------- the landform
        public sealed class Land
        {
            public GameObject root;public MeshRenderer floor,walls;public Mesh floorMesh,wallMesh,collision;
            /// <summary>The drawn floor, which is what <c>GroundKit.Open</c> takes: its layers, flat details, cover and moving shade go on after everything else stands in the scene.</summary>
            public MeshFilter FloorFilter=>floor.GetComponent<MeshFilter>();
        }
        /// <summary>A heightfield landform in three parts: an invisible full-resolution collider (walls stop bodies and navigation reads it), the walkable floor drawn with
        /// <paramref name="floorMaterial"/>, and what rises out of it drawn with <paramref name="wallMaterial"/>, its texture laid along each wall so its bands stay level. A tone colours the
        /// floor's vertices; a wall top darkens the walls toward their tops from a wall foot, white unless it's given. Use half-metre cells where a wall foot is in view.
        /// The meshes are <see cref="GroundTerrain"/>'s; this stands them in the scene as LRB's Terrain did, and GroundKit works on the floor it returns.
        /// <paramref name="drawn"/> is the ground as it is seen where that is plainer than the ground as it is stood on: a kerb a heightfield can only draw as a saw of triangles is
        /// left out of the picture and built as a shape of its own on the same line, and bodies and the bake still meet it in the collider.
        /// <paramref name="floorWithin"/> is positive on the zone's floors where it knows them: everything outside is drawn as rock (see <see cref="GroundTerrain.Build"/>).</summary>
        public Land Terrain(string label,float xMin,float xMax,float zMin,float zMax,float cell,Material floorMaterial,Material wallMaterial,Func<Vector2,Color> floorTone=null,Color? wallTop=null,Color? wallFoot=null,Func<Vector2,float> drawn=null,Func<Vector2,float> floorWithin=null)
        {
            var land=GroundTerrain.Build(label,xMin,xMax,zMin,zMax,cell,height,cliffTop,floorTone,wallTop,wallFoot,floorWithin);
            var seen=drawn==null?land:GroundTerrain.Build(label,xMin,xMax,zMin,zMax,cell,drawn,cliffTop,floorTone,wallTop,wallFoot,floorWithin);
            var full=SaveMesh(land.collision,"Assets/_Project/Scenes/"+zone+"-Terrain.asset");
            var ground=SaveMesh(seen.floor,"Assets/_Project/Scenes/"+zone+"-Ground.asset");
            var cliffs=SaveMesh(seen.cliffs,"Assets/_Project/Scenes/"+zone+"-Cliffs.asset");
            // The shadow is the drawn ground's: a kerb left out of the picture would otherwise shade the floor from nowhere.
            var shade=drawn==null?full:SaveMesh(seen.collision,"Assets/_Project/Scenes/"+zone+"-Terrain-Drawn.asset");
            if(drawn!=null){UnityEngine.Object.DestroyImmediate(land.floor);UnityEngine.Object.DestroyImmediate(land.cliffs);}
            // The collision body keeps an enabled renderer with nothing to draw: the ground camera treats only rendered colliders as scenery to look past. A mesh there with
            // forceRenderingOff drew twice, because that flag isn't saved with a scene.
            var terrain=new GameObject(label,typeof(MeshRenderer),typeof(MeshCollider));terrain.isStatic=true;
            terrain.GetComponent<MeshCollider>().sharedMesh=full;terrain.AddComponent<AudioSurface>();
            var hidden=terrain.GetComponent<MeshRenderer>();hidden.sharedMaterial=floorMaterial;hidden.shadowCastingMode=ShadowCastingMode.Off;
            // One big renderer each, so there's nothing to batch: not static, or they join the combined mesh of whatever shares their material.
            var floorView=new GameObject(label+" floor",typeof(MeshFilter),typeof(MeshRenderer));floorView.transform.SetParent(terrain.transform,false);
            floorView.GetComponent<MeshFilter>().sharedMesh=ground;floorView.GetComponent<Renderer>().sharedMaterial=floorMaterial;
            var cliffView=new GameObject(label+" cliffs",typeof(MeshFilter),typeof(MeshRenderer));cliffView.transform.SetParent(terrain.transform,false);
            cliffView.GetComponent<MeshFilter>().sharedMesh=cliffs;cliffView.GetComponent<Renderer>().sharedMaterial=wallMaterial;
            // The landform's shadow comes from one welded caster, the collision heightfield itself, whose shared vertices give the shadow bias nothing to open (E3-lanternwick-08); the drawn floor and walls only receive.
            floorView.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;cliffView.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            var caster=new GameObject(label+" shadow",typeof(MeshFilter),typeof(MeshRenderer));caster.transform.SetParent(terrain.transform,false);
            caster.GetComponent<MeshFilter>().sharedMesh=shade;var casts=caster.GetComponent<MeshRenderer>();casts.sharedMaterial=wallMaterial;casts.shadowCastingMode=ShadowCastingMode.ShadowsOnly;casts.receiveShadows=false;
            return new Land{root=terrain,floor=floorView.GetComponent<MeshRenderer>(),walls=cliffView.GetComponent<MeshRenderer>(),floorMesh=ground,wallMesh=cliffs,collision=full};
        }

        // ---------------------------------------------------------------- props
        /// <summary>A kit piece standing on the ground: <paramref name="size"/> is its height (a few keys fit by footprint or length, as WorldBuilder.Piece decides). Not grounded, y is the world's.</summary>
        public GameObject Prop(string key,Vector3 at,float size,float yaw=0,bool solid=true,bool grounded=true)
        {
            if(standIn.TryGetValue(key,out float fit))size*=fit;
            var go=WorldBuilder.Piece(key,Vector3.zero,Vector3.one*size,"Rock",solid);
            go.transform.rotation=Quaternion.Euler(0,yaw,0);var bounds=ModelGeometry.BoundsOf(go);
            float y=grounded?Y(at.x,at.z)+at.y:at.y;
            go.transform.position+=new Vector3(at.x,y+.01f,at.z)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            if(!solid)foreach(var c in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            go.isStatic=solid;foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.isStatic=solid;
            if(fadeTall&&bounds.size.y>=TallEnoughToBlock)Fadeable(go);
            return go;
        }
        /// <summary>A piece drawn only and turned any way up, tinted apart from its fellows: wreckage under a flight lane, a thing lying on its side.</summary>
        public GameObject Loose(string key,Vector3 at,float size,Quaternion turned,Color? tint=null,bool grounded=false)
        {
            var piece=Prop(key,at,size,0,false,grounded);var centre=Drawn(piece).center;
            piece.transform.rotation=turned;piece.transform.position+=centre-Drawn(piece).center;
            if(tint is Color tone)piece.AddComponent<PropTint>().tint=tone;
            return piece;
        }
        /// <summary>Small floor detail scattered where it never meets a route point or prompt. What keptOff refuses is dropped, not moved: the draws stay in step,
        /// so everything else stands exactly where every route was walked (D213). Replace a key in its own slot for the same reason.</summary>
        public void Scatter(System.Random rng,int count,Func<Vector2> pick,Func<Vector2,bool> allowed,Func<Vector2,bool> keptOff,params (string key,float size,bool solid)[] kinds)
        {
            int placed=0;
            for(int tries=0;placed<count&&tries<count*12;tries++)
            {
                var p=pick();if(!allowed(p))continue;var (key,size,solid)=kinds[rng.Next(kinds.Length)];if(!Landed(key))continue;
                float scale=size*(.75f+.5f*(float)rng.NextDouble()),yaw=(float)rng.NextDouble()*360;placed++;
                if(keptOff==null||!keptOff(p))Prop(key,new Vector3(p.x,Skirted.Contains(key)?-PlantSkirt*scale:0,p.y),scale,yaw,solid);
            }
        }
        public static readonly (string key,float small,float large)[] CanyonRocks={("RidgeRock1",3.5f,6.5f),("RidgeRock2",3.5f,6.5f),("RidgeRock3",3.5f,6.5f),("DressStrataOutcrop",2.2f,3.7f),("DressStrataLedge",2.2f,3.7f),("DressBoulders",1.6f,2.6f)};
        /// <summary>Pieces along a wall's foot, so a wall reads as what it's made of from the fixed camera. What's left out or doesn't stay is dropped after its draws, so the others stand where they stood.</summary>
        public void WallFoot(IEnumerable<Vector3> points,int seed,(string key,float small,float large)[] kinds=null,Func<GameObject,bool> stays=null,Func<Vector2,bool> leftOut=null)
        {
            var rng=new System.Random(seed);kinds??=CanyonRocks;
            foreach(var p in points)
            {
                var (key,small,large)=kinds[rng.Next(kinds.Length)];if(!Landed(key))continue;
                float size=small+(float)rng.NextDouble()*(large-small),yaw=(float)rng.NextDouble()*360;
                if(leftOut!=null&&leftOut(new Vector2(p.x,p.z)))continue;
                var rock=Prop(key,p,size,yaw,true);
                if(stays!=null&&!stays(rock))UnityEngine.Object.DestroyImmediate(rock);
            }
        }
        static readonly (float x,float z)[] Footprint={(-1,-1),(0,-1),(1,-1),(-1,0),(0,0),(1,0),(-1,1),(0,1),(1,1)};
        /// <summary>Walks a wide piece back from the floor until its whole footprint clears it, then rests it on the lowest ground under it, its far side in the wall.
        /// False when it can't clear the floor, or would be half under the ground: the caller drops it. <paramref name="fromFloor"/> is the distance from the walked floor at a point.</summary>
        public bool SeatInWall(GameObject rock,Func<Vector2,float> fromFloor,float clear,bool capped=true)
        {
            var box=Drawn(rock);
            float wide=Mathf.Max(box.size.x,box.size.z);
            if(capped&&wide>WidestCliffRock)
            {
                var middle=box.center;rock.transform.localScale*=WidestCliffRock/wide;box=Drawn(rock);
                rock.transform.position+=new Vector3(middle.x-box.center.x,0,middle.z-box.center.z);box=Drawn(rock);
            }
            var at=new Vector2(box.center.x,box.center.z);
            var away=new Vector2(fromFloor(at+Vector2.right)-fromFloor(at-Vector2.right),fromFloor(at+Vector2.up)-fromFloor(at-Vector2.up));
            if(away.sqrMagnitude>0)away.Normalize();
            float Nearest(Vector2 c){float near=float.PositiveInfinity;foreach(var (x,z) in Footprint)near=Mathf.Min(near,fromFloor(c+new Vector2(x*box.extents.x,z*box.extents.z)));return near;}
            for(float walked=0;walked<MostSeatWalk&&Nearest(at)<clear;walked+=.75f)at+=away*.75f;
            bool fits=Nearest(at)>=clear;
            float low=float.PositiveInfinity;foreach(var (x,z) in Footprint)low=Mathf.Min(low,height(at+new Vector2(x*box.extents.x,z*box.extents.z)));
            rock.transform.position+=new Vector3(at.x-box.center.x,low+.01f-ModelGeometry.BoundsOf(rock).min.y,at.y-box.center.z);
            // Where the plan's check looks for ground under a prop: if even the lowest of those is half way up it, the rock reads as sunk in.
            float showing=height(at);foreach(var (x,z) in new[]{(.6f,.6f),(-.6f,.6f),(.6f,-.6f),(-.6f,-.6f)})showing=Mathf.Min(showing,height(at+new Vector2(x*box.extents.x,z*box.extents.z)));
            return fits&&showing-low<box.size.y*.45f;
        }
        /// <summary>A point light with no shadows. Underground and past six lights a renderer, a lit thing is a fixture and a painted pool instead (D217).</summary>
        public Light Lamp(Vector3 at,float range=9,float intensity=1.6f,Color? colour=null)
        {
            var go=new GameObject("Lamp light",typeof(Light));go.transform.position=On(at);
            var l=go.GetComponent<Light>();l.type=LightType.Point;l.color=colour??new Color(1,.76f,.45f);l.range=range;l.intensity=intensity;l.shadows=LightShadows.None;return l;
        }
        /// <summary>A floor sign beside what it names, a hand above the ground (SignStyle lays it at load). Upper case.</summary>
        public GameObject Sign(string text,float x,float z,float size=2)=>WorldBuilder.Label(text,On(x,z,.1f),size);
        /// <summary>A gateway on its feet: solid legs and an open way through. The fade is told where the opening is, because a hero seen through it isn't behind anything.</summary>
        public GameObject Arch(string name,Vector3 at,float size,float yaw,string key="AncelaneArch",string fallback="HushwellColumn")
        {
            var arch=Prop(Kit(key,fallback),at,size,yaw,false);arch.name=name;
            var fade=arch.GetComponent<SceneryOcclusion>();
            if(fade!=null)fade.opening=new Bounds(new Vector3(0,-size*.15f,0),new Vector3(size*.4f,size*.7f,size*.26f));
            foreach(int side in new[]{-1,1})
            {
                var leg=new GameObject("Arch leg",typeof(BoxCollider));leg.transform.SetParent(arch.transform,false);leg.isStatic=true;
                leg.transform.localPosition=new Vector3(side*size*.33f,1.25f-size*.5f,0);leg.GetComponent<BoxCollider>().size=new Vector3(size*.33f,2.5f,size*.37f);
            }
            return arch;
        }

        // ---------------------------------------------------------------- ways out, people, things to find
        /// <summary>A way out, staged as what its words say ("Verb — Place"). Its sign stands low in front of it: raised over a tall prop it ran off the top of the screen.
        /// A prompt measures to the middle of the body, so a tall one needs a longer reach from the floor. A null flag is an open way; a locked prompt names what opens it.</summary>
        public WarpBeacon Gate(string label,Vector3 at,string scene,string spawn,string flag,string locked,string key="WarpBeacon",float size=3,float range=3.6f,Vector2? signAt=null,float yaw=0)
        {
            var prop=Prop(Kit(key,"WarpBeacon"),at,size,yaw);prop.name=label;
            return Way(prop,label,scene,spawn,flag,locked,range,signAt??new Vector2(at.x,at.z-size*.5f-1.6f));
        }
        /// <summary>The same on a body the builder made itself.</summary>
        public WarpBeacon Way(GameObject body,string label,string scene,string spawn,string flag,string locked,float range,Vector2? signAt)
        {
            var warp=body.AddComponent<WarpBeacon>();warp.prompt=label;warp.range=range;warp.scene=scene;warp.spawn=spawn;warp.requiredFlag=flag??"";warp.lockedPrompt=locked??"";
            if(signAt is Vector2 sign)WorldBuilder.Label(label,On(sign.x,sign.y,.3f),2.2f);
            return warp;
        }
        /// <summary>A named person: nodes <c>IdFirst</c>, <c>IdRepeat</c>, <c>IdPost</c> unless told otherwise. Npc's own post flag is chapter 1's, so it is always set here.</summary>
        public Npc Person(string id,Vector3 at,float yaw,string postFlag,bool post=true,string finalFlag=null,string finalNode=null,string first=null)
        {
            var npc=WorldBuilder.Npc(id,On(at),first);npc.transform.rotation=Quaternion.Euler(0,yaw,0);
            npc.postFlag=postFlag??"";if(!post)npc.postNode="";npc.finalFlag=finalFlag;npc.finalNode=finalNode;return npc;
        }
        /// <summary>A thing to look at, take or cross: every field set, because the component's defaults are chapter 1's. With a quest and step it is offered only while that quest is at that step;
        /// give it a <c>visual</c> and switch the visual off. A node plays as <paramref name="speaker"/>.</summary>
        public DiscoveryPoint Discovery(string name,Vector3 at,string prompt,string flag,string report,string node=null,string speaker="Sela",string quest=null,int step=0,float range=3.2f)
        {
            var go=new GameObject(name,typeof(DiscoveryPoint));go.transform.position=On(at);
            var point=go.GetComponent<DiscoveryPoint>();point.prompt=prompt;point.flag=flag;point.report=report??"";point.situation="";
            point.dialogueNode=node??"";point.dialogueSpeaker=speaker;point.requiredQuest=quest??"";point.requiredStep=step;point.requiredFlag="";point.range=range;
            return point;
        }
        /// <summary>A conversation the party can't walk round: it starts as the active hero crosses this box (D195, and a boss's door).</summary>
        public DiscoveryPoint Crossing(string name,Vector3 at,Vector3 size,string flag,string node,string speaker,string requiredFlag=null)
        {
            var point=Discovery(name,at,name,flag,"",node,speaker,null,0,2);point.startsOnCrossing=true;point.requiredFlag=requiredFlag??"";
            var mouth=point.gameObject.AddComponent<BoxCollider>();mouth.isTrigger=true;mouth.size=size;mouth.center=Vector3.up*size.y*.5f;
            return point;
        }
        /// <summary>Someone who lives here, with no name and nothing to say. Tinted so nobody takes one for the person whose body it borrows, and standing in a body-sized static box, which is
        /// all the bake reads. Off screen its animation stops: the frame holds about four moving bodies.</summary>
        public GameObject Townsperson(string body,Vector3 at,float yaw,Color tint,string fallback="Neve")
        {
            var person=new GameObject("Townsperson");person.transform.SetPositionAndRotation(On(at),Quaternion.Euler(0,yaw,0));person.isStatic=true;
            var model=(GameObject)PrefabUtility.InstantiatePrefab(Body("Characters",body,"Characters",fallback));model.transform.SetParent(person.transform,false);model.name="Generated civilian";
            var solid=person.AddComponent<BoxCollider>();solid.size=new Vector3(.7f,1.9f,.7f);solid.center=Vector3.up*.95f;
            person.AddComponent<PropTint>().tint=tint;
            foreach(var rig in model.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullCompletely;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
            return person;
        }
        public void Townsfolk(IEnumerable<(string body,float x,float z,float yaw,Color tint)> people){foreach(var (body,x,z,yaw,tint) in people)Townsperson(body,new Vector3(x,0,z),yaw,tint);}
        /// <summary>A resident on an errand: walks an authored loop for as long as the zone is open, waits a moment at each stop, and stands still for anyone in the way (<see cref="WalkLoop"/>).
        /// Not static and not in the bake, so keep the loop off every route. Points are on the plan.</summary>
        public WalkLoop Walker(string body,IReadOnlyList<Vector2> loop,float speed,float pause,Color tint,string fallback="Neve")
        {
            var person=new GameObject("Townsperson on an errand");person.transform.position=On(loop[0].x,loop[0].y);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(Body("Characters",body,"Characters",fallback));model.transform.SetParent(person.transform,false);model.name="Generated civilian";
            var solid=person.AddComponent<CapsuleCollider>();solid.height=1.9f;solid.radius=.35f;solid.center=Vector3.up*.95f;
            person.AddComponent<PropTint>().tint=tint;
            foreach(var rig in model.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullCompletely;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
            var walk=person.AddComponent<WalkLoop>();walk.path=loop.Select(p=>On(p.x,p.y)).ToArray();walk.speed=speed;walk.pause=pause;
            return walk;
        }

        // ---------------------------------------------------------------- navigation
        /// <summary>Bakes the walkable floor from the scene's static box and mesh colliders (nothing else is read; a named person's own collider is marked not walkable), then refuses to go on unless
        /// the arrival has floor and every must-reach point has a complete path from it: list every place a player must stand. Saves <c>Zone-Navigation.asset</c>.</summary>
        public void BakeNavigation(Bounds bounds,Vector3 origin,IEnumerable<Vector3> mustReach)
        {
            Physics.SyncTransforms();var sources=new List<NavMeshBuildSource>();
            foreach(var collider in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if(!collider.enabled||collider.isTrigger||!collider.gameObject.isStatic||collider.GetComponentInParent<Membrane>()!=null)continue;
                if(collider is BoxCollider box)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=box.transform.localToWorldMatrix*Matrix4x4.Translate(box.center),size=box.size,area=0});
                else if(collider is MeshCollider mesh&&mesh.sharedMesh!=null)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=mesh.sharedMesh,transform=mesh.transform.localToWorldMatrix,area=0});
            }
            foreach(var npc in UnityEngine.Object.FindObjectsByType<Npc>(FindObjectsSortMode.None))
            {
                var c=npc.GetComponent<Collider>();if(c==null)continue;
                sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(c.bounds.center,Quaternion.identity,Vector3.one),size=c.bounds.size,area=1});
            }
            var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.55f;settings.agentHeight=2;settings.agentClimb=.3f;settings.agentSlope=35;
            settings.overrideVoxelSize=true;settings.voxelSize=.15f;
            var built=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
            if(built==null)throw new InvalidOperationException(zone+" navigation bake failed");
            var instance=NavMesh.AddNavMeshData(built);
            try
            {
                if(!NavMesh.SamplePosition(origin,out var start,2,NavMesh.AllAreas))throw new InvalidOperationException(zone+" has no walkable floor at its arrival "+origin);
                foreach(var goal in mustReach)
                {
                    var path=new NavMeshPath();
                    if(!NavMesh.SamplePosition(goal,out var sampled,2.5f,NavMesh.AllAreas)||!NavMesh.CalculatePath(start.position,sampled.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
                        throw new InvalidOperationException(zone+" route blocked at "+goal+" (sampled "+NavMesh.SamplePosition(goal,out var near,6,NavMesh.AllAreas)+" "+near.position+")");
                }
                Debug.Log($"ZONE_ROUTE_BAKE_OK {zone} points={mustReach.Count()}");
            }
            finally{instance.Remove();}
            string file="Assets/_Project/Scenes/"+zone+"-Navigation.asset";var previous=AssetDatabase.LoadAssetAtPath<NavMeshData>(file);
            if(previous==null){AssetDatabase.CreateAsset(built,file);previous=built;}
            else{EditorUtility.CopySerialized(built,previous);UnityEngine.Object.DestroyImmediate(built);EditorUtility.SetDirty(previous);}
            new GameObject("Walkable paths",typeof(GroundNavigation)).GetComponent<GroundNavigation>().data=previous;
        }
        /// <summary>The same with plan points: each is lifted to the ground under it.</summary>
        public void BakeNavigation(Bounds bounds,Vector2 origin,IEnumerable<Vector2> mustReach)=>BakeNavigation(bounds,On(origin.x,origin.y),mustReach.Select(p=>On(p.x,p.y)).ToArray());
    }
}
