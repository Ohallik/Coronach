using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Isolated room and outpost dressing for the Decks, Tallow and Sorrel. Opens each
    /// saved scene, rebuilds only its "Room dressing" root (restoring any stand-ins it
    /// retired last time), rebakes that zone's navigation and saves. Nothing else in the
    /// scene is regenerated. A layout is refused, unsaved, if any piece blocks a replay
    /// route, a door, a speaker or a spawn, if a prompt becomes unreachable, or if any
    /// dressing rises into the fixed camera's sightline to reachable floor (D119).
    /// Sorrel's slope scatter is a generator: scatter pieces that would hide a hero are
    /// pruned and logged instead of failing.
    /// Run again after any rebuild of these scenes (StationRedesign/TallowRedesign/SorrelRedesign).
    /// </summary>
    public static partial class RoomDressing
    {
        public const string RootName="Room dressing";
        const string RetiredSuffix=" (retired by room dressing)";
        public static readonly string[] Zones={"Hub_Decks","TallowDrift","Sorrel_Ridges"};

        /// <summary>Width (x), height, depth (z) of each piece as placed at scale 1; front faces -z.</summary>
        static readonly Dictionary<string,Vector3> Nominal=new()
        {
            ["DressMarketStall"]=new(2.4f,2.1f,1.4f),["DressDeckLamp"]=new(.45f,1.3f,.45f),["DressShelfRack"]=new(2.2f,1.9f,.7f),
            ["DressGalleyTable"]=new(2.6f,.85f,1.8f),["DressWorkbench"]=new(2.2f,1f,1f),["DressOfficeDesk"]=new(2.2f,1.3f,1.4f),
            ["DressPlanter"]=new(2.4f,.9f,.8f),["DressCargoPallet"]=new(1.6f,1.2f,1.4f),["DressCabinBunk"]=new(2.4f,1.2f,1.1f),
            ["DressSpoilHeap"]=new(4f,1.2f,3f),["DressStrataOutcrop"]=new(4f,2.6f,3.5f),["DressStrataLedge"]=new(5f,1.1f,1.6f),
            ["DressFuelTank"]=new(3f,1.35f,1.6f),["DressCommsMast"]=new(1.2f,5.5f,1.2f),["DressSolarArray"]=new(3.6f,1.2f,1.6f),
            ["DressWaterBarrels"]=new(1.8f,1.2f,1.8f),["DressRover"]=new(3.4f,1.9f,1.9f),["DressOreHopper"]=new(2.2f,2.8f,2.4f),
            // Reused generated kit, sized as their original builders size them.
            ["DeckBench"]=new(2.2f,1f,.9f),["DeckCrate"]=new(1f,1f,1f),["HushwellCrates"]=new(1.6f,1.4f,1.4f),["HushwellRubble"]=new(2f,1f,2f),
            ["HushwellOreCart"]=new(1.4f,1.3f,2f),["HushwellLowRidge"]=new(3f,1.1f,1.2f),["RidgeRock1"]=new(3f,3f,3f),["RidgeRock2"]=new(3f,3f,3f),["RidgeRock3"]=new(3f,3f,3f),
        };
        /// <summary>Pieces fitted by their largest horizontal extent rather than height.</summary>
        static readonly HashSet<string> Footprint=new(){"DressSpoilHeap","DressStrataLedge","DressStrataOutcrop","HushwellRubble","HushwellLowRidge"};

        sealed class Placed{public GameObject go;public string key;public bool scatter;public bool solid;public bool structural;}
        static readonly List<Placed> placed=new();
        static readonly List<string> problems=new();
        static readonly SortedSet<string> pending=new();
        static readonly HashSet<string> logged=new();
        static Transform root;static string zone;static bool final;
        static List<(Vector2 a,Vector2 b,float half,string route)> corridors;
        static HashSet<InteractionPrompt> baseline;

        public static void Apply()=>BatchTools.Run(()=>
        {
            final=DevArgs.Has("-dress-final");
            bool allow=DevArgs.Has("-dress-allow-problems");
            string only=DevArgs.Value("-dress-zones");
            StageDecals();
            var refused=new List<string>();
            foreach(var z in Zones)
            {
                if(!string.IsNullOrEmpty(only)&&!only.Split(',').Contains(z))continue;
                if(!Dress(z,allow))refused.Add(z);
            }
            if(DevArgs.Has("-dress-floors"))FloorWear();
            if(refused.Count>0)throw new InvalidOperationException("Dressing refused, scenes left unsaved: "+string.Join(", ",refused));
            AssetDatabase.SaveAssets();
            Debug.Log("ROOM_DRESSING_OK pending="+string.Join(",",pending));
        });

        static bool Dress(string name,bool allow)
        {
            zone=name;placed.Clear();problems.Clear();
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.EndsWith(RetiredSuffix)).ToArray())
            {t.name=t.name.Substring(0,t.name.Length-RetiredSuffix.Length);t.gameObject.SetActive(true);}
            foreach(var old in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.parent==null&&t.name==RootName).ToArray())
                Object.DestroyImmediate(old.gameObject);
            root=new GameObject(RootName){isStatic=true}.transform;
            Physics.SyncTransforms();
            corridors=Corridors();
            // Dressing may not take away an approach that existed before it (prompts behind
            // closed boss seals were never reachable from the arrival and are not required).
            var arrival=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).First(s=>s.id=="Arrival").transform.position;
            baseline=Bake()?WithNavigation(()=>ReachablePrompts(arrival)):new HashSet<InteractionPrompt>();
            decalMeshes.Clear();
            if(zone=="Hub_Decks")Decks();else if(zone=="TallowDrift")Tallow();else Sorrel();
            SaveDecals();
            Physics.SyncTransforms();
            Clearances();
            Audit();
            foreach(var p in problems)Debug.LogWarning("DRESSING_PROBLEM "+zone+": "+p);
            Debug.Log($"DRESSING_ZONE {zone} pieces={placed.Count} problems={problems.Count}");
            if(problems.Count>0&&!allow)return false;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return true;
        }

        // ---------- placement ----------
        static GameObject Put(string key,float x,float z,float yaw,float scale=1,bool solid=true,bool scatter=false,float sink=0)
        {
            if(!Nominal.TryGetValue(key,out var nominal))throw new InvalidOperationException("Unknown dressing piece "+key);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/"+key+".prefab");
            var go=new GameObject(key);go.transform.SetParent(root,false);
            if(prefab!=null)
            {
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.transform.SetParent(go.transform,false);
                var b=ModelGeometry.BoundsOf(body);
                float factor=Footprint.Contains(key)?nominal.x*scale/Mathf.Max(b.size.x,b.size.z):nominal.y*scale/b.size.y;
                body.transform.localScale*=factor;b=ModelGeometry.BoundsOf(body);
                body.transform.localPosition-=new Vector3(b.center.x,b.min.y,b.center.z);
                foreach(var c in body.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
            }
            else
            {
                if(final)throw new InvalidOperationException("Final dressing requires generated prefab "+key);
                pending.Add(key);
                var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.name="ART_PENDING_"+key;Object.DestroyImmediate(block.GetComponent<Collider>());
                block.transform.SetParent(go.transform,false);var size=nominal*scale;block.transform.localScale=size;block.transform.localPosition=Vector3.up*size.y*.5f;
                block.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Blockout/"+(size.y>1.39f?"Enemy":"Taren"));
            }
            var local=ModelGeometry.BoundsOf(go);
            if(logged.Add(key+scale))Debug.Log($"DRESSING_PIECE {key} scale={scale:0.00} size=({local.size.x:0.00},{local.size.y:0.00},{local.size.z:0.00})");
            if(solid){var box=go.AddComponent<BoxCollider>();box.center=local.center;box.size=local.size;}
            go.transform.rotation=Quaternion.Euler(0,yaw,0);
            go.transform.position=new Vector3(x,Ground(x,z,local,yaw)-sink,z);
            foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;
            placed.Add(new Placed{go=go,key=key,scatter=scatter,solid=solid});
            return go;
        }
        /// <summary>Lowest supporting surface under the footprint, ignoring dressing, so nothing floats on a slope.</summary>
        static float Ground(float x,float z,Bounds local,float yaw)
        {
            var rotation=Quaternion.Euler(0,yaw,0);float lowest=float.PositiveInfinity;
            foreach(var corner in new[]{Vector3.zero,new Vector3(local.min.x,0,local.min.z)*.9f,new Vector3(local.max.x,0,local.min.z)*.9f,new Vector3(local.min.x,0,local.max.z)*.9f,new Vector3(local.max.x,0,local.max.z)*.9f})
            {
                var at=new Vector3(x,40,z)+rotation*new Vector3(corner.x,0,corner.z);
                foreach(var hit in Physics.RaycastAll(at,Vector3.down,80,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                {
                    if(hit.collider.transform.IsChildOf(root)||hit.normal.y<.5f)continue;
                    // Station floors are at deck level; anything standing proud of them is not floor.
                    if(zone!="Sorrel_Ridges"&&hit.point.y>.3f)continue;
                    lowest=Mathf.Min(lowest,hit.point.y);break;
                }
            }
            return float.IsInfinity(lowest)?0:lowest;
        }
        /// <summary>A stand-in replaced by generated furniture is switched off, never deleted.</summary>
        static void Retire(string key,float x,float z,float radius=.8f)
        {
            var hit=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name==key&&(t.parent==null||t.parent.name!=key)&&!t.IsChildOf(root)&&Vector2.Distance(new Vector2(t.position.x,t.position.z),new Vector2(x,z))<radius).ToArray();
            if(hit.Length!=1){problems.Add($"expected one {key} to retire at ({x},{z}), found {hit.Length}");return;}
            if(hit[0].GetComponentInChildren<InteractionPrompt>(true)!=null){problems.Add($"{key} at ({x},{z}) is interactive and cannot be retired");return;}
            hit[0].name+=RetiredSuffix;hit[0].gameObject.SetActive(false);
        }
        /// <summary>Structural fix with the station's own generated low-wall module.</summary>
        static void Wall(string label,float x,float z,float width,float depth)
        {
            var wall=WorldBuilder.Piece("DeckWall",new Vector3(x,.55f,z),new Vector3(width,1.1f,depth));wall.name=label;wall.transform.SetParent(root,true);
            foreach(var t in wall.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;
            placed.Add(new Placed{go=wall,key="DeckWall",solid=true,structural=true});
        }

        // ---------- floor decals: walkway runners and rugs (generated textures on flat quads) ----------
        const string DecalFolder="Assets/_Project/Art/Generated/Dressing/";
        static readonly List<Mesh> decalMeshes=new();
        static void StageDecals()
        {
            foreach(var name in new[]{"DressRunner","DressRug"})
                PackStaging.StageFile("art-src/Generated/C8-RoomDressing/"+name+"_base_color.png","Art/Generated/Dressing/"+name+"_base_color.png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        static Material DecalMaterial(string kind)
        {
            string path="Assets/_Project/Resources/WorldMaterials/dress-"+kind+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Lattice/Toon"));AssetDatabase.CreateAsset(m,path);}
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(DecalFolder+(kind=="runner"?"DressRunner":"DressRug")+"_base_color.png");
            if(tex==null)throw new InvalidOperationException("Missing staged decal texture for "+kind);
            m.SetTexture("_BaseMap",tex);m.SetColor("_BaseColor",kind=="runner"?new Color(.92f,.94f,.96f):Color.white);EditorUtility.SetDirty(m);
            return m;
        }
        /// <summary>A flat quad lying on the deck: width across local x, length along local z. Runners tile along their length.</summary>
        static void Decal(string kind,float x,float z,float width,float length,float yaw,float lift=.012f)
        {
            float hw=width*.5f,hl=length*.5f,v=kind=="runner"?length/width:1;
            var mesh=new Mesh{name=$"{zone} {kind} {decalMeshes.Count}"};
            mesh.vertices=new[]{new Vector3(-hw,0,-hl),new Vector3(hw,0,-hl),new Vector3(-hw,0,hl),new Vector3(hw,0,hl)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,v),new Vector2(1,v)};
            mesh.triangles=new[]{0,2,1,2,3,1};mesh.RecalculateNormals();mesh.RecalculateBounds();decalMeshes.Add(mesh);
            var go=new GameObject(kind=="runner"?"Walkway runner":"Rug",typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(root,false);go.transform.SetPositionAndRotation(new Vector3(x,Ground(x,z,new Bounds(Vector3.zero,Vector3.zero),0)+lift,z),Quaternion.Euler(0,yaw,0));
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=DecalMaterial(kind);
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;go.isStatic=true;
        }
        static void SaveDecals()
        {
            string path=DecalFolder+"FloorDecals-"+zone+".asset";
            if(AssetDatabase.LoadMainAssetAtPath(path)!=null)AssetDatabase.DeleteAsset(path);
            if(decalMeshes.Count==0)return;
            AssetDatabase.CreateAsset(decalMeshes[0],path);
            foreach(var mesh in decalMeshes.Skip(1))AssetDatabase.AddObjectToAsset(mesh,path);
            AssetDatabase.ImportAsset(path);
        }

        // ---------- checks ----------
        static List<(Vector2,Vector2,float,string)> Corridors()
        {
            var list=new List<(Vector2,Vector2,float,string)>();
            string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../../docs/quality/routes"));
            var spawns=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Select(s=>new Vector2(s.transform.position.x,s.transform.position.z)).ToArray();
            foreach(var file in Directory.GetFiles(dir,"*.json").OrderBy(f=>f))
            {
                QualityRoute route;try{route=JsonUtility.FromJson<QualityRoute>(File.ReadAllText(file));}catch{continue;}
                if(route?.steps==null)continue;
                string current=route.scene,name=Path.GetFileNameWithoutExtension(file);Vector2? last=null;
                var segments=new List<(Vector2,Vector2,float,string)>();
                foreach(var step in route.steps)
                {
                    string scene=string.IsNullOrEmpty(step.expectedScene)?current:step.expectedScene;
                    if(scene!=current){current=scene;last=null;}
                    if(scene!=zone){last=null;continue;}
                    if(!step.navigate||step.approachTarget||step.approachPartner)continue;
                    var p=new Vector2(step.point.x,step.point.z);
                    var from=last??(spawns.Length>0?spawns.OrderBy(s=>Vector2.Distance(s,p)).First():p);
                    segments.Add((from,p,Mathf.Min(1.6f,SightlineAudit.HeroRadius+step.tolerance+.3f),name));last=p;
                }
                if(segments.Count==0)continue;
                // A route whose own path already runs through a wall predates the current layout.
                var blocked=segments.FirstOrDefault(s=>Vector2.Distance(s.Item1,s.Item2)>.1f&&Physics.Linecast(new Vector3(s.Item1.x,1,s.Item1.y),new Vector3(s.Item2.x,1,s.Item2.y),~0,QueryTriggerInteraction.Ignore));
                if(blocked.Item4!=null){Debug.Log($"DRESSING_ROUTE_OBSOLETE {zone} {name} {blocked.Item1}->{blocked.Item2}");continue;}
                list.AddRange(segments);
            }
            Debug.Log($"DRESSING_CORRIDORS {zone} segments={list.Count}");
            return list;
        }
        static Rect Footprint2D(GameObject go)
        {
            var b=go.GetComponentsInChildren<Collider>().Select(c=>c.bounds).Aggregate((a,c)=>{a.Encapsulate(c);return a;});
            return Rect.MinMaxRect(b.min.x+.05f,b.min.z+.05f,b.max.x-.05f,b.max.z-.05f);
        }
        static void Clearances()
        {
            var doors=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Pressure frame collision"&&!t.IsChildOf(root)).Select(t=>new Vector2(t.position.x,t.position.z)).ToArray();
            var prompts=Object.FindObjectsByType<InteractionPrompt>(FindObjectsSortMode.None).Where(p=>!p.transform.IsChildOf(root)).ToArray();
            var spawns=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            var encounters=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Select(e=>e.GetComponent<Collider>().bounds).ToArray();
            var existing=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.enabled&&!c.isTrigger&&!c.transform.IsChildOf(root)&&c.bounds.size.x<60&&c.bounds.max.y-c.bounds.min.y>.35f&&c.GetComponentInParent<InteractionPrompt>()==null).ToArray();
            var reject=new List<Placed>();
            foreach(var item in placed.Where(p=>p.solid&&!p.structural))
            {
                var rect=Footprint2D(item.go);string why=null;
                foreach(var c in corridors)if(SegmentRect(c.a,c.b,rect)<c.half){why=$"blocks route {c.route} {c.a}->{c.b}";break;}
                if(why==null)foreach(var d in doors)if(PointRect(d,rect)<1.6f){why="crowds the doorway at "+d;break;}
                if(why==null)foreach(var p in prompts){var at=new Vector2(p.transform.position.x,p.transform.position.z);if(PointRect(at,rect)<.8f){why="crowds "+p.name;break;}}
                if(why==null)foreach(var s in spawns)if(PointRect(new Vector2(s.transform.position.x,s.transform.position.z),rect)<1.5f){why="crowds spawn "+s.id;break;}
                if(why==null)foreach(var e in encounters)if(rect.Overlaps(Rect.MinMaxRect(e.min.x-1,e.min.z-1,e.max.x+1,e.max.z+1))){why="intrudes on an encounter arena";break;}
                if(why==null)foreach(var c in existing)
                {
                    var b=c.bounds;
                    if(rect.Overlaps(Rect.MinMaxRect(b.min.x,b.min.z,b.max.x,b.max.z))&&b.max.y>item.go.transform.position.y+.3f){why="intersects "+c.name;break;}
                }
                if(why==null)foreach(var other in placed)
                {
                    if(other==item||!other.solid||placed.IndexOf(other)>placed.IndexOf(item))continue;
                    if(rect.Overlaps(Footprint2D(other.go))){why="intersects dressing "+other.key;break;}
                }
                if(why==null)continue;
                if(item.scatter)reject.Add(item);else problems.Add($"{item.key} at {item.go.transform.position} {why}");
            }
            foreach(var r in reject){placed.Remove(r);Object.DestroyImmediate(r.go);}
            if(reject.Count>0)Debug.Log($"DRESSING_SCATTER_CLEARANCE_PRUNED {zone} {reject.Count}");
            Physics.SyncTransforms();
        }
        static void Audit()
        {
            var start=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).First(s=>s.id=="Arrival").transform.position;
            for(int pass=0;;pass++)
            {
                if(!Bake())return;
                var data=Object.FindFirstObjectByType<GroundNavigation>().data;var instance=NavMesh.AddNavMeshData(data);
                try
                {
                    var report=SightlineAudit.Run(root,start);
                    Debug.Log($"DRESSING_SIGHTLINE {zone} pass={pass} {report}");
                    var scatter=placed.Where(p=>p.scatter&&report.offenders.Contains(p.go.name)).ToList();
                    var hand=report.offenders.Where(o=>!placed.Any(p=>p.scatter&&p.go.name==o)).ToList();
                    if(hand.Count>0){problems.Add("hides the hero: "+string.Join(", ",hand)+"\n"+string.Join("\n",report.hidden.Where(h=>hand.Any(o=>h.Contains(o))).Take(12)));}
                    if(scatter.Count>0&&pass<4)
                    {
                        // Scatter pieces share names; prune those whose bounds contain a reported hit.
                        var prune=placed.Where(p=>p.scatter&&report.offenders.Contains(p.go.name)&&HitInside(p.go,report)).ToList();
                        foreach(var p in prune){placed.Remove(p);Object.DestroyImmediate(p.go);}
                        Debug.Log($"DRESSING_SCATTER_SIGHTLINE_PRUNED {zone} {prune.Count}");
                        if(prune.Count>0){Physics.SyncTransforms();continue;}
                    }
                    if(scatter.Count>0)problems.Add("scatter still hides the hero after pruning: "+string.Join(", ",scatter.Select(s=>s.go.name).Distinct()));
                    Reachability(start);
                    return;
                }
                finally{instance.Remove();}
            }
        }
        static bool HitInside(GameObject go,SightlineAudit.Report report)
        {
            var b=go.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,c)=>{a.Encapsulate(c);return a;});b.Expand(.05f);
            return report.hits.Any(h=>h.who==go.name&&b.Contains(h.point));
        }
        static bool Bake()
        {
            foreach(var nav in Object.FindObjectsByType<GroundNavigation>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(nav.gameObject);
            try{if(zone=="Sorrel_Ridges")SorrelRedesign.BakeNavigation();else StationNavigationBake.Bake(zone);return true;}
            catch(Exception e){problems.Add("navigation: "+e.Message);return false;}
        }
        static T WithNavigation<T>(Func<T> body)
        {
            var instance=NavMesh.AddNavMeshData(Object.FindFirstObjectByType<GroundNavigation>().data);
            try{return body();}finally{instance.Remove();}
        }
        static void Reachability(Vector3 start)
        {
            var now=ReachablePrompts(start);
            foreach(var prompt in baseline)
                if(prompt!=null&&!now.Contains(prompt))problems.Add($"{prompt.GetType().Name} '{prompt.name}' at {prompt.transform.position} lost its reachable approach within {prompt.range} m");
            Debug.Log($"DRESSING_REACHABLE {zone} before={baseline.Count} after={now.Count}");
        }
        static HashSet<InteractionPrompt> ReachablePrompts(Vector3 start)
        {
            var result=new HashSet<InteractionPrompt>();
            if(!NavMesh.SamplePosition(start,out var from,2,NavMesh.AllAreas))return result;
            foreach(var prompt in Object.FindObjectsByType<InteractionPrompt>(FindObjectsSortMode.None))
            {
                if(prompt is MineNode)continue; // Sorrel's bake already proves each ore approach.
                var at=prompt.transform.position;bool ok=false;
                for(float r=.6f;r<=prompt.range&&!ok;r+=.4f)
                for(int i=0;i<16&&!ok;i++)
                {
                    float a=i*Mathf.PI/8;var probe=at+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
                    // Prompts sit on their fixture (a console's centre is 0.8 m up); test the floor below.
                    if(!NavMesh.SamplePosition(probe,out var hit,at.y+1f,NavMesh.AllAreas))continue;
                    var flat=hit.position-at;flat.y=0;var drift=hit.position-probe;drift.y=0;
                    if(flat.magnitude>prompt.range||drift.magnitude>.4f)continue;
                    var path=new NavMeshPath();ok=NavMesh.CalculatePath(from.position,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                }
                if(ok)result.Add(prompt);
            }
            return result;
        }
        static float PointRect(Vector2 p,Rect r){float dx=Mathf.Max(r.xMin-p.x,0,p.x-r.xMax),dz=Mathf.Max(r.yMin-p.y,0,p.y-r.yMax);return Mathf.Sqrt(dx*dx+dz*dz);}
        static float SegmentRect(Vector2 a,Vector2 b,Rect r)
        {
            if(r.Contains(a)||r.Contains(b))return 0;
            var c=new[]{new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax),new Vector2(r.xMin,r.yMax)};
            for(int i=0;i<4;i++)if(Intersects(a,b,c[i],c[(i+1)%4]))return 0;
            float d=Mathf.Min(PointRect(a,r),PointRect(b,r));
            foreach(var p in c)d=Mathf.Min(d,PointSegment(p,a,b));
            return d;
        }
        static float PointSegment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=d.sqrMagnitude<1e-6f?0:Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);return Vector2.Distance(p,a+d*t);}
        static bool Intersects(Vector2 p1,Vector2 p2,Vector2 q1,Vector2 q2)
        {
            float Cross(Vector2 o,Vector2 a,Vector2 b)=>(a.x-o.x)*(b.y-o.y)-(a.y-o.y)*(b.x-o.x);
            float d1=Cross(q1,q2,p1),d2=Cross(q1,q2,p2),d3=Cross(p1,p2,q1),d4=Cross(p1,p2,q2);
            return (d1>0)!=(d2>0)&&(d3>0)!=(d4>0);
        }

        /// <summary>World-space grime and wear patches on the station deck finishes (toon detail keyword), breaking up the repeated plate.</summary>
        static void FloorWear()
        {
            foreach(string area in new[]{"public","arrival","quiet","work","service","rooms"})
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/station-floor-"+area+".mat");
                if(m==null)throw new InvalidOperationException("Missing station finish "+area);
                m.EnableKeyword("_DETAIL_ON");m.SetFloat("_Detail",1);
                m.SetVector("_DetailParams",new Vector4(.11f,.9f,.42f,.07f));
                m.SetColor("_DetailTintA",new Color(.8f,.79f,.77f));m.SetColor("_DetailTintB",new Color(1.05f,1.04f,1.02f));
                EditorUtility.SetDirty(m);
            }
            Debug.Log("DRESSING_FLOOR_WEAR_OK");
        }
    }
}
