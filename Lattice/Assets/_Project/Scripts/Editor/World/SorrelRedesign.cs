using System;
using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>Workshop outpost and connected extraction basin. Isolated rebuild;
    /// no definition regeneration, station edits, save migration or paid intake.</summary>
    public static class SorrelRedesign
    {
        static bool blockout;
        static readonly List<Rect> platforms=new();
        static readonly Vector2[] Haul={new(0,-18),new(0,8),new(0,24),new(9,39),new(4,58),new(-8,75),new(-5,94),new(9,111),new(16,129),new(7,145),new(0,159),new(-5,177),new(0,199)};
        static readonly Vector2[] Seam={new(-5,27),new(-24,35),new(-36,49),new(-39,68),new(-28,88),new(-24,107),new(-13,124),new(7,145)};
        static readonly Vector2[] Service={new(9,39),new(29,46),new(39,64),new(31,83),new(35,103),new(31,123),new(16,129)};
        static readonly Vector2[] Encounters={new(-33,46),new(-39,69),new(-25,90),new(-21,112),new(9,45),new(-7,75),new(-4,101),new(15,129),new(29,49),new(36,73),new(34,104),new(29,126)};
        static readonly Vector2[] Mines={new(-38,36),new(-31,106),new(14,34),new(3,109),new(39,39),new(43,99)};
        public static void Blockout()=>BatchTools.Run(()=>{BuildZone(true);AssetDatabase.SaveAssets();Debug.Log("SORREL_BLOCKOUT_OK");});
        public static void Final()=>BatchTools.Run(()=>{BuildZone(false);AssetDatabase.SaveAssets();Debug.Log("SORREL_REDESIGN_OK");});
        public static void BuildZone(bool grey=false)
        {
            blockout=grey;platforms.Clear();
            if(!grey)foreach(string id in new[]{"OutpostHab","DeckConsole","DeckCrate","DeckFloor","DeckWall","RidgeRock1","RidgeRock2","RidgeRock3","OutpostDrill","LatticeAnvil","CrystalClusterA","CrystalClusterB","LandingPad","HushwellScaffold","HushwellRubble","HushwellLowRidge","HushwellFallenLamp","HushwellDrillBit","HushwellCrates"})
                if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/"+id+".prefab")==null)throw new InvalidOperationException("Missing generated Sorrel piece "+id);
            var scene=WorldBuilder.Begin("Sorrel_Ridges");
            Terrain();WorldArt.Planet("Vorun",new Vector3(160,35,350),150);
            WorldBuilder.Spawn("Arrival",new Vector3(0,0,-16));WorldBuilder.Spawn("Outpost",new Vector3(0,0,14));
            Dock("Launch — Cinder Halo",0,-18,"Moon");
            Prop("Skiff",-9,-18,2,90,false);
            // Receiving is directly beside the landing; stores serve the haul
            // road. The pedestrian approach crosses neither repair nor freight.
            Platform("Receiving apron",7,-11,6,7,"arrival");
            for(int i=0;i<3;i++)Prop("DeckCrate",5+i*1.5f,-11,1.1f);
            Platform("Parts and stores",14,-3,7,8,"service");
            for(int i=0;i<4;i++)Prop("DeckCrate",12+(i%2)*2,-5+(i/2)*3,1.2f);
            Platform("Repair work pad",-7,6,8,8,"work");
            var repair=Prop("DeckConsole",-10,7,1.4f).AddComponent<RepairBay>();repair.free=true;repair.saveSpawn="Outpost";repair.prompt="Outpost cradle — repair & save";
            Prop("DeckCrate",-10,3,.8f);Prop("DeckBench",-9,9,1);
            foreach(int side in new[]{-1,1})
            {
                Platform("Hab footing",side*15,17,9,10,"quiet");
                // The generated hab's door is on +Z, measured in the four-view
                // prop audit. Rotate it inward and align its entry with the path.
                Prop("OutpostHab",side*15,blockout?18:16,4,side>0?-90:90);
                Platform("Hab approach",side*8,16,7,3,"quiet");
                if(blockout)ArenaBuilder.Block("Hab entrance blockout",new Vector3(side*10.95f,1.15f,16),new Vector3(.12f,2.3f,1.8f),"Rock");
            }
            var safe=new GameObject("Sheltered outpost",typeof(BoxCollider),typeof(SafePocket));safe.transform.position=new Vector3(0,1,10);safe.GetComponent<BoxCollider>().size=new Vector3(42,5,35);safe.GetComponent<BoxCollider>().isTrigger=true;
            var survivor=WorldBuilder.Npc("Survivor",new Vector3(1,0,18));survivor.finalFlag="hushwell.nursery";survivor.finalNode="SurvivorNursery";
            WorldBuilder.Label("SORREL OUTPOST",new Vector3(0,.1f,2));
            // Rock strata follow route bends. The terrain supplies continuity;
            // these exposed pieces are landmarks and low cover, not fence posts.
            Ridge(-17,50,3.5f,25);Ridge(-20,66,4,-15);Ridge(-22,82,3.2f,20);
            Ridge(18,63,3.8f,35);Ridge(16,83,3.3f,-25);Ridge(20,106,3.6f,25);
            Ridge(-14,104,3.5f,0);Ridge(0,128,3.1f,30);
            Ridge(-49,51,6,10);Ridge(-51,83,7,-5);Ridge(-43,116,6,25);
            Ridge(50,51,7,-20);Ridge(50,88,6,15);Ridge(49,118,7,25);
            for(int i=0;i<Encounters.Length;i++)
            {
                var p=Encounters[i];string enemy=i<6?"Ridgehound":i<10?"Scrapmite":"SentinelHusk";int count=i<6?3:i<10?6:1;
                // Local arena extents follow the open wash, with stable IDs.
                var encounter=WorldBuilder.Encounter("Sorrel_"+i,new Vector3(p.x,1,p.y),new Vector3(16,5,14),enemy,count);
                var spawner=encounter.GetComponentInChildren<Spawner>();spawner.transform.position=new Vector3(p.x,0,p.y+3);
            }
            for(int i=0;i<Mines.Length;i++)
            {
                var p=Mines[i];var node=Prop(i%2==0?"CrystalClusterA":"CrystalClusterB",p.x,p.y,2.5f).AddComponent<MineNode>();
                node.nodeId="ridge_"+(i/2)+"_"+(i%2);node.prompt="Mine Ridge Crystal";node.amount=1;
            }
            Dress();
            // Service equipment gives Scrapmites an actual salvage/repair context.
            Prop("DeckCrate",40,58,1,20);Prop("DeckConsole",43,79,1.5f,-30);Prop("DeckCrate",30,117,1.1f,10);
            Platform("Drill maintenance bench",7,141,7,6,"work");
            var bench=Prop("LatticeAnvil",8,142,2).AddComponent<Bench>();bench.prompt="Link to the Lattice Anvil";
            var entry=WorldBuilder.Membrane("Drill entry seal",new Vector3(0,2,153),26,true);
            var exit=WorldBuilder.Membrane("Drill cradle seal",new Vector3(0,2,191),26,false);
            SealObstacle(entry,true);SealObstacle(exit,false);
            WorldBuilder.Encounter("Sorrel_Burrower",new Vector3(0,1,164),new Vector3(24,5,12),"Burrower",1,new[]{entry,exit});
            Platform("Drill service footing",-8,180,9,12,"work");Prop("OutpostDrill",-8,180,8);
            WorldBuilder.Label("DRILL SITE",new Vector3(0,.1f,147));
            // The drill bored into Hushwell. With the Burrower down, the miners'
            // scaffold over the bore is the way into the cave and back.
            WorldBuilder.Spawn("Hushwell",new Vector3(8,0,172));
            var bore=Prop("HushwellScaffold",8,177,2.6f).AddComponent<WarpBeacon>();
            bore.prompt="Descend the drill bore — Hushwell";bore.range=3.5f;bore.scene="Hushwell";bore.spawn="Arrival";
            bore.requiredFlag="bossdown.Burrower";bore.lockedPrompt="The Burrower still guards the bore";
            var key=Prop("LatticeAnvil",0,199,2).AddComponent<KeyPickup>();key.prompt="Recover the warp key";
            Dock("Launch — outer Halo beacon",8,201,"Outer");
            // Hard limits sit within the steep, visible enclosing basin rim.
            WorldBuilder.Boundary("West distant terrain limit",new Vector3(-106,15,90),new Vector3(2,40,340));
            WorldBuilder.Boundary("East distant terrain limit",new Vector3(106,15,90),new Vector3(2,40,340));
            WorldBuilder.Boundary("South distant terrain limit",new Vector3(0,15,-61),new Vector3(212,40,2));
            WorldBuilder.Boundary("North distant terrain limit",new Vector3(0,15,258),new Vector3(212,40,2));
            BakeNavigation();WorldBuilder.Save(scene,"Sorrel_Ridges");
        }
        static GameObject Prop(string key,float x,float z,float height,float yaw=0,bool solid=true)
        {
            Vector3 envelope=key=="OutpostHab"?new Vector3(8,height,8):key=="Skiff"?new Vector3(4,height,7):Vector3.one*height;
            string greyMaterial=key=="OutpostHab"?"Sela":key.StartsWith("Crystal")?"Emission":"Rock";
            GameObject go=blockout?ArenaBuilder.Block(key+" blockout",Vector3.zero,envelope,greyMaterial):
                WorldBuilder.Piece(key,Vector3.zero,key=="Skiff"?new Vector3(4,height,7):Vector3.one*height,"Rock",solid);
            go.transform.rotation=Quaternion.Euler(0,yaw,0);var bounds=ModelGeometry.BoundsOf(go);
            float surface=Height(new Vector2(x,z));foreach(var pad in platforms)if(pad.Contains(new Vector2(x,z)))surface=Mathf.Max(surface,0);
            go.transform.position+=new Vector3(x,surface+.01f,z)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            if(!solid)foreach(var c in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            return go;
        }
        static void Dock(string label,float x,float z,string spawn)
        {
            var go=blockout?ArenaBuilder.Block(label,Vector3.zero,new Vector3(5,.16f,5),"Sela"):
                WorldBuilder.Piece("LandingPad",Vector3.zero,new Vector3(5,.3f,5),"Sela");
            // The old fixed -.3 m centre buried this shallow generated pad in
            // the terrain. Ground its actual bottom; keep the top step usable.
            var bounds=ModelGeometry.BoundsOf(go);
            if(bounds.size.y>.3f)throw new InvalidOperationException("Landing pad requires a measured ramp: "+bounds.size);
            go.transform.position+=new Vector3(x,Height(new Vector2(x,z))+.01f,z)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            go.name=label;var dock=go.AddComponent<DockingPad>();dock.prompt=label;dock.range=4;dock.scene="Hub_CinderHalo";dock.spawn=spawn;
        }
        static void Platform(string label,float x,float z,float width,float depth,string finish)
        {
            platforms.Add(new Rect(x-width*.5f,z-depth*.5f,width,depth));
            var position=new Vector3(x,-.08f,z);var size=new Vector3(width,.16f,depth);
            var go=blockout?ArenaBuilder.Block(label,position,size,"Taren"):WorldBuilder.Piece("DeckFloor",position,size,"Ground");
            go.name=label;if(!blockout)StationSurfaces.FloorFinish(go,finish);
        }
        /// <summary>Route-edge dressing: the natural seam gathers rubble and low
        /// strata; the equipment branch collects what the workers discarded. Every
        /// piece keeps clear of encounters, ore, junctions and the excavation.</summary>
        static void Dress()
        {
            int placed=0;
            foreach(var (path,half,kinds) in new[]{(Seam,5f,new[]{"HushwellRubble","HushwellLowRidge","HushwellRubble"}),(Service,5f,new[]{"HushwellFallenLamp","HushwellCrates","HushwellDrillBit"}),
                (Haul,6f,new[]{"HushwellCrates","HushwellRubble","HushwellFallenLamp"})})
            {
                float along=6;int n=0;
                for(int i=1;i<path.Length;i++)
                {
                    Vector2 a=path[i-1],d=path[i]-a;float length=d.magnitude;var normal=new Vector2(-d.y,d.x)/length;
                    for(;along<length;along+=13)
                    {
                        var p=a+d*(along/length)+normal*((n%2==0?1:-1)*(half-1.4f));
                        bool clear=true;
                        foreach(var e in Encounters)if(Vector2.Distance(p,e)<8)clear=false;
                        foreach(var m in Mines)if(Vector2.Distance(p,m)<6)clear=false;
                        foreach(var other in new[]{Haul,Seam,Service})if(other!=path&&Distance(p,other)<7)clear=false;
                        if(p.y>140||p.y<30||Height(p)>.25f)clear=false;
                        if(!clear)continue;
                        string key=kinds[n%kinds.Length];n++;placed++;
                        if(key=="HushwellCrates")Prop(key,p.x,p.y,1.4f,n*47);
                        else if(key=="HushwellLowRidge")Prop(key,p.x,p.y,1.1f,Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg+90);
                        else Prop(key,p.x,p.y,key=="HushwellRubble"?1.2f:key=="HushwellDrillBit"?1.1f:.8f,n*63);
                        // A little scattered spoil around each piece, toward the verge.
                        var spoil=p+normal*((n%2==1?1:-1)*1.9f)+d/length*1.2f;
                        if(Height(spoil)<=.25f&&Distance(spoil,path)<half)Prop("HushwellRubble",spoil.x,spoil.y,.6f,n*29);
                    }
                    along-=length;
                }
            }
            Debug.Log("SORREL_DRESSING count="+placed);
        }
        static void Ridge(float x,float z,float height,float yaw)=>Prop("RidgeRock"+(Mathf.RoundToInt(Mathf.Abs(x+z))%3+1),x,z,height,yaw);
        static float Distance(Vector2 p,Vector2[] path)
        {
            float distance=float.PositiveInfinity;
            for(int i=1;i<path.Length;i++)
            {Vector2 a=path[i-1],d=path[i]-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);distance=Mathf.Min(distance,Vector2.Distance(p,a+d*t));}
            return distance;
        }
        static float Height(Vector2 p)
        {
            // Flat graded corridors and encounter clearings are cut from a
            // continuous basin rather than laid as three rectangular strips.
            float clear=Mathf.Min(Distance(p,Haul)-6,Mathf.Min(Distance(p,Seam)-5,Distance(p,Service)-5));
            foreach(var e in Encounters)clear=Mathf.Min(clear,Vector2.Distance(p,e)-10);
            foreach(var m in Mines)clear=Mathf.Min(clear,Vector2.Distance(p,m)-3);
            clear=Mathf.Min(clear,(new Vector2(p.x/30,(p.y-4)/35).magnitude-1)*28);
            clear=Mathf.Min(clear,(new Vector2(p.x/17,(p.y-174)/32).magnitude-1)*20);
            // Grade the whole northern pad apron, including its corners; a
            // level pad cannot be placed through the rising side of the hollow.
            clear=Mathf.Min(clear,Vector2.Distance(p,new Vector2(8,201))-4.5f);
            float texture=.7f+.3f*Mathf.PerlinNoise((p.x+170)*.045f,(p.y+60)*.035f);
            float strata=2.2f+2.4f*Mathf.PerlinNoise((p.x+300)*.032f,(p.y+140)*.025f)+1.2f*Mathf.Sin(p.x*.10f+p.y*.045f);
            float interior=Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/7))*strata;
            float width=62+6*Mathf.Sin(p.y*.023f)+3*Mathf.Sin(p.y*.065f);
            float endCurve=12*Mathf.Pow(p.x/65,2);
            float rim=Mathf.Max(Mathf.Abs(p.x)-width,Mathf.Max(-29+endCurve-p.y,p.y-(212-endCurve)));
            // Keep the foreground below the camera sightline. Add the rim to
            // the local strata so an intersecting hill cannot erase its face
            // and create a long bypass around the excavation's locked seal.
            float background=Mathf.Min(Mathf.InverseLerp(-50,10,p.x),Mathf.InverseLerp(0,140,p.y));
            float outer=Mathf.SmoothStep(0,1,Mathf.Clamp01(rim/.8f))*(2.5f+background*(6+4*Mathf.PerlinNoise((p.x+180)*.035f,(p.y+90)*.028f)));
            float southCut=147+2*Mathf.Sin(p.x*.11f)-Mathf.Max(0,Mathf.Abs(p.x)-20)*.7f,northCut=198+2*Mathf.Sin(p.x*.13f)+Mathf.Max(0,Mathf.Abs(p.x)-20)*.7f;
            float excavationBand=Mathf.Min(Mathf.Clamp01((p.y-southCut)/1.2f),Mathf.Clamp01((northCut-p.y)/1.2f));
            float excavationWidth=12.7f+4*Mathf.Max(0,Mathf.Sin((p.y-153)/38*Mathf.PI));
            float excavation=Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Abs(p.x)-excavationWidth)/1.2f))*excavationBand;
            float cliffs=excavation*(6+4*texture+1.5f*Mathf.Sin(p.x*.16f+p.y*.07f));
            return Mathf.Max(interior,cliffs)+outer-.1f;
        }
        static void Terrain()
        {
            const int nx=221,nz=333;var vertices=new Vector3[nx*nz];var uv=new Vector2[vertices.Length];var triangles=new int[(nx-1)*(nz-1)*6];int ti=0;
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                int i=z*nx+x;var p=new Vector2(x-110,z-65);vertices[i]=new Vector3(p.x,Height(p),p.y);uv[i]=p/8;
                if(x==nx-1||z==nz-1)continue;
                triangles[ti++]=i;triangles[ti++]=i+nx;triangles[ti++]=i+1;
                triangles[ti++]=i+1;triangles[ti++]=i+nx;triangles[ti++]=i+nx+1;
            }
            var mesh=new Mesh{name="Sorrel connected basin",indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path="Assets/_Project/Scenes/Sorrel_Ridges-Terrain.asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(prior==null){AssetDatabase.CreateAsset(mesh,path);prior=mesh;}else{EditorUtility.CopySerialized(mesh,prior);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(prior);}
            var terrain=new GameObject("Continuous eroded basin",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));terrain.isStatic=true;
            terrain.GetComponent<MeshFilter>().sharedMesh=prior;terrain.GetComponent<MeshCollider>().sharedMesh=prior;
            terrain.GetComponent<Renderer>().sharedMaterial=blockout?Resources.Load<Material>("Blockout/Ground"):AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/sorrel-ground.mat");
        }
        static void SealObstacle(Membrane seal,bool open)
        {
            var box=seal.GetComponent<BoxCollider>();if(box==null)throw new InvalidOperationException("Drill seal needs measured box collision");
            var obstacle=seal.gameObject.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=box.center;obstacle.size=box.size;
            obstacle.carving=true;obstacle.carveOnlyStationary=false;obstacle.enabled=!open;
        }
        internal static void BakeNavigation()
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
            var built=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(0,8,90),new Vector3(180,50,270)),Vector3.zero,Quaternion.identity);
            if(built==null)throw new InvalidOperationException("Sorrel mesh navigation bake failed");
            var instance=NavMesh.AddNavMeshData(built);
            try
            {
                // The boss's closed exit is intentionally excluded. All three
                // exploration branches must connect to the outpost repair.
                var points=new List<Vector2>(Haul);points.RemoveAll(p=>p.y>150);points.AddRange(Seam);points.AddRange(Service);
                foreach(var p in points)
                {
                    var goal=new Vector3(p.x,0,p.y);var path=new NavMeshPath();
                    if(!NavMesh.SamplePosition(goal,out var sampled,2,NavMesh.AllAreas)||
                       !NavMesh.CalculatePath(new Vector3(0,0,14),sampled.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
                        throw new InvalidOperationException("Sorrel route blocked at "+goal);
                }
                // Ore centres contain solid crystals. Verify a reachable place
                // to stand within the actual interaction range, not inside one.
                foreach(var node in UnityEngine.Object.FindObjectsByType<MineNode>(FindObjectsSortMode.None))
                {
                    var p=new Vector2(node.transform.position.x,node.transform.position.z);Vector2 nearest=p;float distance=float.PositiveInfinity;
                    foreach(var route in new[]{Haul,Seam,Service})for(int i=1;i<route.Length;i++)
                    {
                        var a=route[i-1];var d=route[i]-a;var q=a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);
                        if((q-p).sqrMagnitude<distance){distance=(q-p).sqrMagnitude;nearest=q;}
                    }
                    var approach=Vector2.MoveTowards(p,nearest,2.45f);var goal=new Vector3(approach.x,0,approach.y);var path=new NavMeshPath();
                    if(!NavMesh.SamplePosition(goal,out var sampled,.75f,NavMesh.AllAreas)||
                       Vector3.Distance(sampled.position,node.transform.position)>node.range||
                       !NavMesh.CalculatePath(new Vector3(0,0,14),sampled.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
                        throw new InvalidOperationException("Sorrel ore interaction has no reachable approach: "+node.nodeId+" at "+goal);
                }
                Debug.Log("SORREL_ROUTE_BAKE_OK points="+points.Count);
            }
            finally{instance.Remove();}
            string file="Assets/_Project/Scenes/Sorrel_Ridges-Navigation.asset";var previous=AssetDatabase.LoadAssetAtPath<NavMeshData>(file);
            if(previous==null){AssetDatabase.CreateAsset(built,file);previous=built;}
            else {EditorUtility.CopySerialized(built,previous);UnityEngine.Object.DestroyImmediate(built);EditorUtility.SetDirty(previous);}
            new GameObject("Basin paths",typeof(GroundNavigation)).GetComponent<GroundNavigation>().data=previous;
        }
    }
}
