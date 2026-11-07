using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Dressing pass 3 (D193): Hushwell and the Gullet.
    ///
    /// Hushwell had one dim uniform floor on all three levels: its cave was a single renderer, so
    /// Forward's per-object light limit let only six of its lamps, vents and crystals reach the floor
    /// at all. The cave renderer is now split into lit 16 m chunks (its collider is untouched), and
    /// the floor carries what each level is: on the miners' level, rails from the breach to the two
    /// ore galleries with the carts on them, work lamps along the haul tunnels and the miners' camp
    /// at the breach and in the east pocket; in the pressure gallery, a pale scald crust exactly as
    /// wide as each vent's plume, so the safe ledges read at a glance; lower down, crystal spars on
    /// the far walls that light the descent; in the nursery, a colony of cradles of different sizes
    /// and states instead of five identical cradles in a ring. Floor colour follows level and use.
    ///
    /// The Gullet's floor was the same tiled membrane in every organ. Its bed now shows the anatomy
    /// the brief describes: veins, digestive pools and pores in the feeding chamber, scarred tissue
    /// where the throat folds rub, settled silt under the eddy's wreck, egg clusters thickening toward
    /// the nursery gate, and the coil's floor scoured in arcs by the serpent's body. Everything there
    /// is collider-free and lies far below the flight plane, which FlightAudit enforces.
    /// </summary>
    public static partial class RoomDressing
    {
        const string ChunkRootName="Lit render chunks";
        const float ChunkSize=16;
        static string ChunkedObject=>Cave?"Cave rock and floor":null;
        static readonly Vector2 CaveCameraForward=new(Mathf.Sin(20*Mathf.Deg2Rad),Mathf.Cos(20*Mathf.Deg2Rad));
        static readonly Vector2[] Vents={new(-7,158),new(8,170),new(-12,198),new(5,191),new(-7,230),new(9,222)};
        const float VentPlume=3.2f;

        // ---------- lit chunks ----------
        static void RestoreChunkedRenderer()
        {
            if(ChunkedObject==null)return;
            var go=GameObject.Find(ChunkedObject)??throw new System.InvalidOperationException("Missing "+ChunkedObject);
            go.GetComponent<MeshRenderer>().enabled=true;
        }

        /// <summary>Copies the zone's one big renderer into 16 m chunks (by triangle centroid, materials per submesh kept)
        /// and switches the original renderer off. Its MeshFilter and collider stay, so physics and navigation are unchanged.</summary>
        static void ChunkRenderer()
        {
            if(ChunkedObject==null)return;
            var go=GameObject.Find(ChunkedObject);var renderer=go.GetComponent<MeshRenderer>();var source=go.GetComponent<MeshFilter>().sharedMesh;
            var chunkRoot=new GameObject(ChunkRootName){isStatic=true}.transform;
            var v=source.vertices;var n=source.normals;var uv=source.uv;var colors=source.colors;var world=go.transform.localToWorldMatrix;
            var bins=new Dictionary<(int x,int z),List<int>[]>();
            for(int s=0;s<source.subMeshCount;s++)
            {
                var tris=source.GetTriangles(s);
                for(int t=0;t<tris.Length;t+=3)
                {
                    var c=world.MultiplyPoint3x4((v[tris[t]]+v[tris[t+1]]+v[tris[t+2]])/3);
                    var key=(Mathf.FloorToInt(c.x/ChunkSize),Mathf.FloorToInt(c.z/ChunkSize));
                    if(!bins.TryGetValue(key,out var lists)){lists=Enumerable.Range(0,source.subMeshCount).Select(_=>new List<int>()).ToArray();bins[key]=lists;}
                    lists[s].Add(tris[t]);lists[s].Add(tris[t+1]);lists[s].Add(tris[t+2]);
                }
            }
            foreach(var bin in bins.OrderBy(b=>b.Key.x).ThenBy(b=>b.Key.z))
            {
                var remap=new Dictionary<int,int>();var cv=new List<Vector3>();var cn=new List<Vector3>();var cu=new List<Vector2>();var cc=new List<Color>();
                var subs=new List<int[]>();var materials=new List<Material>();
                for(int s=0;s<bin.Value.Length;s++)
                {
                    var list=bin.Value[s];if(list.Count==0)continue;var local=new int[list.Count];
                    for(int i=0;i<local.Length;i++)
                    {
                        if(!remap.TryGetValue(list[i],out int dst))
                        {
                            dst=cv.Count;remap[list[i]]=dst;cv.Add(v[list[i]]);cn.Add(n[list[i]]);
                            if(uv.Length>0)cu.Add(uv[list[i]]);if(colors.Length>0)cc.Add(colors[list[i]]);
                        }
                        local[i]=dst;
                    }
                    subs.Add(local);materials.Add(renderer.sharedMaterials[s]);
                }
                var mesh=new Mesh{name=$"{zone} lit chunk {bin.Key.x},{bin.Key.z}",indexFormat=cv.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.SetVertices(cv);mesh.SetNormals(cn);if(cu.Count>0)mesh.SetUVs(0,cu);if(cc.Count>0)mesh.SetColors(cc);
                mesh.subMeshCount=subs.Count;for(int s=0;s<subs.Count;s++)mesh.SetTriangles(subs[s],s);
                mesh.RecalculateBounds();decalMeshes.Add(mesh);
                var chunk=new GameObject($"Lit chunk {bin.Key.x},{bin.Key.z}",typeof(MeshFilter),typeof(MeshRenderer)){isStatic=true};
                chunk.transform.SetParent(chunkRoot,false);chunk.transform.SetPositionAndRotation(go.transform.position,go.transform.rotation);chunk.transform.localScale=go.transform.lossyScale;
                chunk.GetComponent<MeshFilter>().sharedMesh=mesh;var r=chunk.GetComponent<MeshRenderer>();r.sharedMaterials=materials.ToArray();
                r.shadowCastingMode=renderer.shadowCastingMode;r.receiveShadows=renderer.receiveShadows;
            }
            renderer.enabled=false;
            Debug.Log($"DRESSING_CHUNKS {zone} chunks={bins.Count} vertices={source.vertexCount}");
        }

        // ---------- the Gullet's flight rule ----------
        /// <summary>Nothing on the Gullet bed may have a collider or rise toward the ships' plane (y=1).</summary>
        static void FlightAudit()
        {
            int renderers=0;
            foreach(var r in root.GetComponentsInChildren<Renderer>())
            {
                renderers++;
                if(r.bounds.max.y>FlightCeiling)problems.Add($"{r.name} rises to y={r.bounds.max.y:0.00}, above the Gullet's flight ceiling {FlightCeiling}");
            }
            foreach(var c in root.GetComponentsInChildren<Collider>())problems.Add($"{c.name} has a collider: nothing on the Gullet bed may touch flight");
            Debug.Log($"DRESSING_FLIGHT {zone} renderers={renderers} problems={problems.Count}");
        }

        // ---------- Hushwell ----------
        static void Hushwell()
        {
            HushwellGround();
            terrainGround=true;
            try
            {
                HushwellMiners();
                HushwellNursery();
                HushwellCrystals();
                HushwellFloor();
            }
            finally{terrainGround=false;}
        }

        /// <summary>A hand-placed cave piece; refuses (as a problem) a spot that is not open cave floor.</summary>
        static GameObject CavePut(string key,float x,float z,float yaw,float scale=1,bool solid=true,float sink=0)
        {
            var go=Put(key,x,z,yaw,scale,solid,false,sink);
            float clear=HushwellLayout.Clear(new Vector2(x,z));
            if(clear>-.4f)problems.Add($"{key} at ({x},{z}) is not on open cave floor (clearance {clear:0.00})");
            return go;
        }

        static void WorkLamp(float x,float z,float yaw,bool scatter=false)
        {
            var go=scatter?Put("DressWorkLamp",x,z,yaw,1,true,true):CavePut("DressWorkLamp",x,z,yaw);
            var holder=new GameObject("Work lamp light"){isStatic=true};holder.transform.SetParent(go.transform,false);holder.transform.localPosition=new Vector3(0,1.25f,-.4f);
            var light=holder.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1f,.82f,.6f);
            light.range=7f;light.intensity=1.25f;light.shadows=LightShadows.None;light.lightmapBakeType=LightmapBakeType.Realtime;
        }

        /// <summary>Point on the main route at a given z, the travel direction there, and its right-hand normal.</summary>
        static (Vector2 at,Vector2 right) RouteAt(float z)
        {
            var main=HushwellLayout.Main;
            for(int i=1;i<main.Length;i++)
                if(z<=main[i].y||i==main.Length-1)
                {
                    var a=main[i-1];var d=main[i]-a;float t=Mathf.Clamp01((z-a.y)/d.y);var dir=d.normalized;
                    return (a+d*t,new Vector2(dir.y,-dir.x));
                }
            return (main[^1],Vector2.right);
        }
        /// <summary>The main route offset to one side (right positive), resampled every 2 m along z.</summary>
        static List<Vector2> RouteOffset(float from,float to,float offset)
        {
            var list=new List<Vector2>();
            for(float z=from;z<=to+.01f;z+=2){var (at,right)=RouteAt(z);list.Add(at+right*offset);}
            return list;
        }
        /// <summary>Walks out from the route to the last spot that is still floor with the given margin from the wall.</summary>
        static Vector2? NearWall(float z,int side,float margin)
        {
            var (at,right)=RouteAt(z);Vector2? best=null;
            for(float o=2.5f;o<=12;o+=.25f){var p=at+right*side*o;if(HushwellLayout.Clear(p)<-margin)best=p;else break;}
            return best;
        }
        static bool InArena(Vector2 p,float pad)
        {
            foreach(var e in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
            {var b=e.GetComponent<Collider>().bounds;if(p.x>b.min.x-pad&&p.x<b.max.x+pad&&p.y>b.min.z-pad&&p.y<b.max.z+pad)return true;}
            return false;
        }

        /// <summary>The miners' level: rails from the breach to both ore galleries with the carts on them,
        /// work lamps along the haul tunnels, the camp at the breach and the cache in the east pocket.</summary>
        static void HushwellMiners()
        {
            // The two loose carts become carts on the line.
            Retire("HushwellOreCart",8,9,1.6f);Retire("HushwellOreCart",-4,30,1.6f);
            // One line up the right side of the haul tunnel from the breach to gallery B's ore, a spur to gallery A's.
            // 3.1 m off the route line: the carts on it stay clear of the replay corridors, the rails clear of the walls.
            var line=RouteOffset(6,84,3.1f);line.AddRange(new[]{new Vector2(13f,87.4f),new Vector2(15f,89f)});
            var spur=new List<Vector2>{RouteAt(33).at+RouteAt(33).right*3.1f,new(-11.4f,39.2f),new(-15.2f,41f),new(-18.8f,41.8f)};
            rails.Clear();rails.Add(Smooth(line.ToArray()));rails.Add(Smooth(spur.ToArray()));
            foreach(var rail in rails)Strip("DecalRails",rail,0,1.25f,.035f);
            // Two carts coupled on the line by the breach, where it runs clear of the tunnel walls and the replay corridors.
            CartOnLine(rails[0],10);CartOnLine(rails[0],14.5f);
            // The line ends at a buffer stop by the breach scaffold, facing the carts that run into it.
            var start=rails[0][0];var dir=(rails[0][1]-rails[0][0]).normalized;
            // Its rails run along local x: turn local x onto the line.
            CavePut("DressRailBuffer",start.x-dir.x*.6f,start.y-dir.y*.6f,Mathf.Atan2(-dir.y,dir.x)*Mathf.Rad2Deg);

            // The miners' camp at the breach: power for the lamps, cable stock, the bench they logged cores on.
            CavePut("DressPowerCell",-12.4f,4.6f,90);CavePut("DressCableDrum",5.5f,-1.8f,-20);
            CavePut("DressWorkbench",-7.4f,14.2f,180);CavePut("DressCoreRack",8.8f,4.8f,-10);
            CavePut("DressWaterBarrels",-12.6f,7.6f,0,.8f);
            WorkLamp(-7f,6.4f,FaceToward(new(-7f,6.4f),new(0,5)));WorkLamp(12.4f,7.6f,FaceToward(new(12.4f,7.6f),new(4,6)));WorkLamp(5.2f,12.4f,FaceToward(new(5.2f,12.4f),new(0,10)));

            // Work lamps along the haul tunnels (the galleries themselves are lit by their crystals).
            foreach(float z in new[]{20f,28f,64f,69f,98f,106f,114f,122f,130f,140f})
            {
                int side=(int)z%2==0?-1:1;
                var p=NearWall(z,side,1.1f);if(p==null||InArena(p.Value,1.6f))continue;
                WorkLamp(p.Value.x,p.Value.y,FaceToward(p.Value,RouteAt(z).at),true);
            }
            // The survey-grid wall: the miners staked the grooves out like a survey.
            foreach(float z in new[]{98.5f,110.5f})foreach(int side in new[]{-1,1})
            {var (at,right)=RouteAt(z);var p=at+right*side*3.6f;Put("DressSurveyBeacon",p.x,p.y,z*7,1,true,true);}
            {var (at,right)=RouteAt(116.5f);var p=at+right*-3.8f;CavePut("DressCoreRack",p.x,p.y,15);}

            // The east pocket: the miners' last cache, with its lamp still running off a power cell.
            CavePut("DressPowerCell",41.4f,160.6f,-30);WorkLamp(45.2f,162f,-100);
        }
        static readonly List<Vector2[]> rails=new();
        /// <summary>Yaw that turns a piece's front (local -z, toward the camera at yaw 0) to face the target.</summary>
        static float FaceToward(Vector2 from,Vector2 target){var d=target-from;return Mathf.Atan2(-d.x,-d.y)*Mathf.Rad2Deg;}
        static void CartOnLine(Vector2[] line,float z)
        {
            int i=System.Array.FindIndex(line,p=>p.y>=z);if(i<1)return;var d=line[i]-line[i-1];
            Put("HushwellOreCart",line[i].x,line[i].y,Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg);
        }
        static float RailDistance(Vector2 p)=>rails.Count==0?float.PositiveInfinity:rails.Min(r=>Distance(p,r));

        /// <summary>The nursery: a colony of cradles of different sizes and states, clear of the three pools.</summary>
        static void HushwellNursery()
        {
            var n=HushwellLayout.Nursery;
            for(int i=0;i<5;i++){float a=(i*72+18)*Mathf.Deg2Rad;Retire("HushwellEggCradle",n.x+Mathf.Cos(a)*6.5f,n.y+Mathf.Sin(a)*5.5f,1.6f);}
            // Clear of the replay paths in (13,322)->(14,330), round the discovery point and out to the lift (25.2,340.2).
            foreach(var (key,x,z,yaw,scale) in new[]{
                ("HushwellEggCradle",7.0f,328.5f,25f,1.05f),("HushwellCradleSingle",2.8f,325.6f,-30f,1f),("HushwellCradleHatched",8.6f,323.4f,160f,1f),
                ("HushwellEggCradle",19.0f,326.0f,-100f,.88f),("HushwellCradleSingle",22.4f,327.6f,40f,.9f),("HushwellCradleHatched",17.6f,340.6f,70f,.95f),
                ("HushwellEggCradle",7.6f,341.2f,140f,.76f)})
            {
                foreach(var pool in BiomeLandscapeUpgrade.NurseryPools)
                {
                    var q=new Vector2((x-pool.x)/(pool.z+1.6f),(z-pool.y)/(pool.w+1.6f));
                    if(q.magnitude<1)problems.Add($"{key} at ({x},{z}) stands in the nursery pool at ({pool.x},{pool.y})");
                }
                CavePut(key,x,z,yaw,scale,true,.08f);
            }
            // The pools' shore: wet stones round each basin, drawn only, so the water has an edge and doesn't read as a flat soft patch.
            foreach(var pool in BiomeLandscapeUpgrade.NurseryPools)for(int i=0;i<9;i++)
            {
                float a=(i*40+pool.x*17)*Mathf.Deg2Rad,reach=1.14f+.1f*Mathf.Sin(i*2.4f);
                Put("HushwellRubble",pool.x+Mathf.Cos(a)*pool.z*reach,pool.y+Mathf.Sin(a)*pool.w*reach,i*67,.3f+.08f*(i%3),false).name="Pool rim";
            }
            // The eggs' own glow on the two largest groups.
            foreach(var p in new[]{new Vector2(7.4f,326.2f),new Vector2(20.6f,328.6f)})
            {
                var light=new GameObject("Egg glow",typeof(Light)){isStatic=true}.GetComponent<Light>();light.transform.SetParent(root,false);
                light.transform.position=new Vector3(p.x,HushwellLayout.Lower+1.4f,p.y);light.type=LightType.Point;light.color=new Color(.45f,.6f,1f);
                light.range=6f;light.intensity=1.4f;light.shadows=LightShadows.None;
            }
        }

        /// <summary>Crystal spars on the far walls (the camera never looks through them) that light the galleries and the descent.</summary>
        static void HushwellCrystals()
        {
            var random=new System.Random(4431);var kept=new List<Vector2>();int count=0;
            var prompts=Object.FindObjectsByType<InteractionPrompt>(FindObjectsSortMode.None).Select(p=>new Vector2(p.transform.position.x,p.transform.position.z)).ToArray();
            for(float z=-10;z<=350;z+=1f)for(float x=-60;x<=60;x+=1f)
            {
                var p=new Vector2(x+(float)random.NextDouble()*.6f,z+(float)random.NextDouble()*.6f);float clear=HushwellLayout.Clear(p);
                if(clear<-1f||clear>-.2f)continue;
                var g=new Vector2(HushwellLayout.Clear(p+Vector2.right*.5f)-HushwellLayout.Clear(p-Vector2.right*.5f),HushwellLayout.Clear(p+Vector2.up*.5f)-HushwellLayout.Clear(p-Vector2.up*.5f));
                if(g.sqrMagnitude<1e-4f)continue;var outward=g.normalized;
                if(Vector2.Dot(outward,CaveCameraForward)<.35f)continue;
                // Where the miners worked and in the breach there is no seam; it starts past the haul line.
                bool seam=z>34&&z<130||z>180&&z<240||z>284&&z<330;
                if(!seam||random.NextDouble()>.45)continue;
                if(kept.Any(k=>Vector2.Distance(k,p)<7f)||prompts.Any(q=>Vector2.Distance(q,p)<3f))continue;
                if(corridors.Any(c=>PointSegment(p,c.a,c.b)<c.half+.9f))continue;
                if(Vents.Any(v=>Vector2.Distance(v,p)<VentPlume+1.5f))continue;
                kept.Add(p);count++;
                float yaw=Mathf.Atan2(-outward.x,-outward.y)*Mathf.Rad2Deg+180+(float)random.NextDouble()*40-20;
                var go=Put("HushwellWallCrystal",p.x,p.y,yaw,.8f+(float)random.NextDouble()*.35f,false,true,.15f);
                var holder=new GameObject("Crystal light"){isStatic=true};holder.transform.SetParent(go.transform,false);
                holder.transform.position=go.transform.position+new Vector3(-outward.x,0,-outward.y)*1.4f+Vector3.up*1.3f;
                var light=holder.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.25f,.88f,1f);
                light.range=7f;light.intensity=1.7f;light.shadows=LightShadows.None;
            }
            // The lower ramp down to the nursery: nobody worked below, so only the seam lights the way.
            // The east wall shades the whole right half of the ramp from the sun, so the seam runs down that side
            // too, its light thrown low across the floor (November 4: the two west crystals alone left it black).
            foreach(var (z,side,scale) in new[]{(299f,-1,.95f),(311f,-1,.95f),(292f,1,1.05f),(303f,1,.9f),(315f,1,1.1f),(325f,1,.85f)})
            {
                var p=NearWall(z,side,.6f);if(p==null){Debug.Log($"DRESSING_RAMP_CRYSTAL none z={z} side={side}");continue;}
                var (at,_)=RouteAt(z);var inward=(at-p.Value).normalized;
                var go=Put("HushwellWallCrystal",p.Value.x,p.Value.y,FaceToward(p.Value,at),scale,false,true,.15f);
                var holder=new GameObject("Crystal light"){isStatic=true};holder.transform.SetParent(go.transform,false);
                bool shaded=side>0;
                // The shaded side's light sits higher and paler so it washes the floor instead of pooling green on it.
                holder.transform.position=go.transform.position+new Vector3(inward.x,0,inward.y)*(shaded?2f:1.4f)+Vector3.up*(shaded?2.4f:1.3f);
                var light=holder.AddComponent<Light>();light.type=LightType.Point;light.color=shaded?new Color(.5f,.8f,1f):new Color(.25f,.88f,1f);
                light.range=shaded?10f:7f;light.intensity=shaded?1.4f:1.7f;light.shadows=LightShadows.None;count++;
            }
            Debug.Log("DRESSING_CRYSTALS placed="+count);
        }

        /// <summary>The cave floor: scald crust exactly as wide as each vent's plume, fissures from the Bellows' breath,
        /// and patches whose kind follows the level (dust above, basalt and fissures below).</summary>
        static void HushwellFloor()
        {
            // The scald crust marks each plume's reach (PressurePulse.hazardRadius 3.2 m); the ledges between stay dark.
            foreach(var v in Vents)Detail("GroundCrust",v.x,v.y,VentPlume*2,VentPlume*2,v.x*13,.03f,true);
            // Fissures radiating from where the Bellows breathes, outside the grooved slab.
            var b=HushwellLayout.Bellows;
            for(int i=0;i<9;i+=2){float a=(i*40+12)*Mathf.Deg2Rad;var p=b+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*12.5f;Detail("GroundFissure",p.x,p.y,4.2f,4.2f,i*40+12+90,.03f);}
            var avoid=new List<(Vector2 at,float radius)>{(b,10f),(HushwellLayout.Nursery,6.4f)};
            foreach(float z in new[]{100f,106f,112f})avoid.Add((new Vector2(RouteAt(z).at.x,z),2.8f));
            foreach(var pool in BiomeLandscapeUpgrade.NurseryPools)avoid.Add((new Vector2(pool.x,pool.y),Mathf.Max(pool.z,pool.w)+.6f));
            var random=new System.Random(6203);int count=0;
            for(float z=-8;z<=350;z+=4.2f)for(float x=-50;x<=50;x+=4.2f)
            {
                var p=new Vector2(x+(float)random.NextDouble()*3.6f-1.8f,z+(float)random.NextDouble()*3.6f-1.8f);
                float size=2.4f+(float)random.NextDouble()*2.2f,yaw=(float)random.NextDouble()*360;double roll=random.NextDouble(),pick=random.NextDouble();
                if(roll>.42||HushwellLayout.Clear(p)>-size*.4f-.2f||avoid.Any(a=>Vector2.Distance(a.at,p)<a.radius+size*.5f))continue;
                if(Vents.Any(v=>Vector2.Distance(v,p)<VentPlume+size*.5f)||RailDistance(p)<size*.5f+.9f)continue;
                float level=HushwellLayout.Level(p.y);
                string kind=level>-1?(pick<.34?"GroundDust":pick<.7?"GroundBasalt":"GroundFissure"):level>-9?(pick<.3?"GroundBasalt":pick<.72?"GroundFissure":"GroundDust"):(pick<.35?"GroundBasalt":pick<.75?"GroundFissure":"GroundDust");
                if(Detail(kind,p.x,p.y,size,size,yaw,.04f))count++;
            }
            Debug.Log("DRESSING_CAVE_PATCHES placed="+count);
        }

        /// <summary>Floor vertex colour by level and use (dusty warm miners' level, cooler basalt in the pressure gallery,
        /// damp blue-violet nursery), darker at the walls and paler where feet wore the route. Vertices are unchanged.</summary>
        static void HushwellGround()
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Scenes/Hushwell-Terrain.asset")??throw new System.InvalidOperationException("Hushwell terrain mesh missing");
            var v=mesh.vertices;var colors=new Color[v.Length];
            for(int i=0;i<v.Length;i++)
            {
                var p=new Vector2(v[i].x,v[i].z);float level=HushwellLayout.Level(p.y),clear=HushwellLayout.Clear(p);
                var tint=level>-1?new Color(1.04f,.98f,.9f):level>-9?Color.Lerp(new Color(1.04f,.98f,.9f),new Color(.86f,.88f,.96f),Mathf.InverseLerp(-1,-8,level)):Color.Lerp(new Color(.86f,.88f,.96f),new Color(.8f,.84f,1f),Mathf.InverseLerp(-8,-16,level));
                var c=new Color(.92f,.92f,.92f)*tint;
                // Walls' feet: the lava-tube floor darkens where it meets the rock.
                c*=Mathf.Lerp(.68f,1f,Mathf.Clamp01(-clear/2.6f));
                // Worn route: feet and carts packed it paler.
                float route=HushwellLayout.Distance(p,HushwellLayout.Main);
                c=Color.Lerp(c,new Color(1.02f,1f,.95f)*tint,(1-Mathf.Clamp01((route-1.4f)/1.8f))*.3f);
                // Broad darker basalt flows across the floor.
                float flow=Mathf.PerlinNoise(p.x*.06f+11.3f,p.y*.06f+4.7f);
                c*=Color.Lerp(Color.white,new Color(.8f,.79f,.84f),Mathf.SmoothStep(0,1,(flow-.56f)/.06f));
                colors[i]=new Color(Mathf.Clamp01(c.r),Mathf.Clamp01(c.g),Mathf.Clamp01(c.b),1);
            }
            mesh.colors=colors;EditorUtility.SetDirty(mesh);
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/hushwell-floor.mat")??throw new System.InvalidOperationException("hushwell-floor material missing");
            material.EnableKeyword("_VCOLOR_ON");material.SetFloat("_VColor",1);
            // Six ramp steps on the floor only: with three, the floor in the walls' shadow quantised to no key light at all
            // (the lower ramp read black) and every lamp or crystal pool was a hard two-ring disc. The rock keeps three.
            material.SetFloat("_RampSteps",6);EditorUtility.SetDirty(material);
            Debug.Log("DRESSING_HUSHWELL_GROUND_OK vertices="+v.Length);
        }

        // ---------- the Gullet ----------
        static Vector2 GulletAt(float z,float across)
        {
            float c=GulletProfile.Center(z);
            return new Vector2(c+(across<0?across*GulletProfile.Left(z):across*GulletProfile.Right(z)),z);
        }
        /// <summary>A patch centre across the bed whose outer edge stays on the bed below the flight ceiling:
        /// the shell rises as 4-8·sqrt(1-a²), so it stays under y=-1 inside about 74% of the half-width.</summary>
        static Vector2 GulletBed(float z,float across,float size)
        {
            float half=across<0?GulletProfile.Left(z):GulletProfile.Right(z);
            float limit=Mathf.Max(0,.74f-size*.5f/half);
            return GulletAt(z,Mathf.Clamp(across,-limit,limit));
        }

        static void Gullet()
        {
            var random=new System.Random(8191);int placed3=0;
            bool Patch(string kind,Vector2 p,float w,float l,float yaw){bool ok=Detail(kind,p.x,p.y,w,l,yaw,.05f);if(ok)placed3++;return ok;}
            float R(float a,float b)=>a+(float)random.NextDouble()*(b-a);
            // The passage's own direction at z, so vessels run with it.
            float Along(float z)=>Mathf.Atan2(GulletProfile.Center(z+4)-GulletProfile.Center(z-4),8)*Mathf.Rad2Deg;

            // Vessels run along both sides of every organ, a little denser and brighter toward the nursery gate.
            // Few enough that the same stamp doesn't show seven or eight times at once when the camera pulls back.
            for(float z=6;z<=880;z+=R(18,30))
                foreach(int side in new[]{-1,1})
                {
                    if(GulletProfile.Valves.Any(v=>Mathf.Abs(v-z)<9)||z>705&&z<868&&random.NextDouble()<.6||random.NextDouble()<.3)continue;
                    float w=R(3,4.5f),l=R(7,11);
                    Patch("GroundVeins",GulletBed(z+R(-3,3),side*R(.22f,.6f),l),w,l,Along(z)+R(-22,22));
                }
            // Feeding chamber: digestive pools in the belly and feeding pores beside them.
            for(float z=124;z<=236;z+=R(14,20)){float size=R(5,8);Patch("GroundPool",GulletBed(z,R(-.45f,.3f),size),size,size,R(0,360));}
            for(float z=40;z<=240;z+=R(16,24)){float size=R(3,4.5f);Patch("GroundPore",GulletBed(z,R(-.55f,.55f),size),size,size,R(0,360));}
            // Slalom throat: scar tissue where each fold rubs the opposite floor, and a few pores.
            foreach(var fold in GulletProfile.Folds)
                for(int i=0;i<3;i++){float size=R(4,6.5f);Patch("GroundScar",GulletBed(fold.z+R(-8,8),-fold.side*R(.15f,.5f),size),size,size,R(0,360));}
            for(float z=280;z<=480;z+=R(22,32)){float size=R(3,4.2f);Patch("GroundPore",GulletBed(z,R(-.5f,.5f),size),size,size,R(0,360));}
            // Salvage eddy: silt settled where the current slows. The pocket's floor rises toward the wreck (which
            // rests on its far slope), so the silt lies on the bed in front of it.
            float eddyX=GulletProfile.RightEdge(GulletProfile.CacheZ)-9;
            Detail("GroundSilt",eddyX-5,GulletProfile.CacheZ+6,11,11,30,.04f,true);placed3++;
            Patch("GroundSilt",new Vector2(eddyX-3,GulletProfile.CacheZ-9),7.5f,7.5f,140);Patch("GroundSilt",new Vector2(eddyX-8,GulletProfile.CacheZ-4),6,6,250);
            // Nursery gate chamber: egg clusters on the floor, thickening toward the gate.
            for(float z=506;z<=692;z+=R(5,8)*(z>630?.7f:1))
                foreach(int side in new[]{-1,1})if(random.NextDouble()<(z>630?.95:.8)){float size=R(3.2f,5.2f);Patch("GroundEggs",GulletBed(z+R(-2,2),side*R(.12f,.58f),size),size,size,R(0,360));}
            // The coil: the serpent's body has scoured the bed in arcs around where it lies, on the flat bottom
            // (r<0.35) and along the crest of the sculpted fold (r~0.7); the fold's inner face is too steep for a band.
            foreach(var (scale,from,to) in new[]{(.3f,10f,150f),(.3f,190f,330f),(.7f,-40f,70f),(.7f,110f,230f),(.7f,260f,300f),(.76f,75f,105f)})
            {
                var arc=new List<Vector2>();
                for(float a=from;a<=to;a+=3){float r=a*Mathf.Deg2Rad;arc.Add(new Vector2(Mathf.Cos(r)*26*scale,GulletProfile.CantorZ+Mathf.Sin(r)*45*scale));}
                Strip("GroundScour",arc.ToArray(),0,4f,.06f);placed3++;
            }
            for(int i=0;i<6;i++){float size=R(4,6);Patch("GroundScar",new Vector2(R(-20,20),R(745,860)),size,size,R(0,360));}
            // The exit valve: vessels converge on the opening.
            foreach(int side in new[]{-1,1})for(int i=0;i<2;i++){float z=872+i*9;Patch("GroundVeins",GulletBed(z,side*.3f,9),3.6f,9,side*-18);}
            // The current out of the animal has worn grooves that converge on the aperture, leading the eye to the way out,
            // and the exit's breathing pores ring the opening (November 4: the exit floor carried almost nothing).
            foreach(var (start,end) in new[]{(-.62f,-.12f),(-.34f,-.05f),(.34f,.05f),(.62f,.12f)})
            {
                var groove=new List<Vector2>();
                for(float z=846;z<=896;z+=2.5f){float t=Mathf.SmoothStep(0,1,(z-846)/50f);groove.Add(GulletAt(z,Mathf.Lerp(start,end,t)));}
                Strip("GroundScour",groove.ToArray(),0,2.6f,.06f);placed3++;
            }
            for(int i=0;i<7;i++){float a=(i/7f*360+R(-12,12))*Mathf.Deg2Rad,size=R(2.6f,3.6f);Patch("GroundPore",GulletBed(884+Mathf.Sin(a)*7,Mathf.Cos(a)*.5f,size),size,size,R(0,360));}
            var exitGlow=new GameObject("Exit aperture glow"){isStatic=true};exitGlow.transform.SetParent(root,false);
            exitGlow.transform.position=new Vector3(GulletProfile.Center(893),FlightCeiling-1.6f,893);
            var glow=exitGlow.AddComponent<Light>();glow.type=LightType.Point;glow.color=new Color(.62f,.86f,1f);glow.range=15f;glow.intensity=1.4f;glow.shadows=LightShadows.None;
            Debug.Log("DRESSING_GULLET_FLOOR placed="+placed3);
        }
    }
}
