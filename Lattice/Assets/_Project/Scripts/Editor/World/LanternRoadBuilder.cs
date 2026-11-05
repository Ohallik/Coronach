using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 2, The Lantern Road (D202). Isolated builder for its definitions and four zones:
    /// <list type="bullet">
    /// <item>The Wick Reach (space combat): Muster raiders stripping a chain of fallen Ancelane relays; relighting two
    /// relays opens the lantern gate to the dock.</item>
    /// <item>Lanternwick (town): a canyon town of carved terraces and lantern towers.</item>
    /// <item>Lantern Gorge (open ground combat): a dark canyon where light-shy grazers hunt and scavengers camp; three
    /// relays to relight.</item>
    /// <item>The Sunken Relay (dungeon): a buried relay station and the scavenger captain's duel.</item>
    /// </list>
    /// Kit pieces generated for the chapter are used when staged; until then each falls back to an existing kit piece,
    /// so the zones are playable before their art lands. Runs: <c>Definitions</c>, <c>Build</c> (all four plus the
    /// Tallow approach warp), or <c>BuildZones</c> with <c>-lantern-zones WickReach,Lanternwick,...</c>.
    /// </summary>
    public static class LanternRoadBuilder
    {
        public static readonly string[] Zones={"WickReach","Lanternwick","LanternGorge","SunkenRelay"};
        public static void Definitions()=>BatchTools.Run(()=>{Define();AssetDatabase.SaveAssets();Debug.Log("LANTERN_DEFS_OK");});
        public static void Build()=>BatchTools.Run(()=>
        {
            Define();RefreshDialogue();foreach(var zone in Zones)BuildZone(zone);StageTallowApproach();
            BatchTools.RegisterScenes();AssetDatabase.SaveAssets();Debug.Log("LANTERN_ROAD_OK");
        });
        public static void BuildZones()=>BatchTools.Run(()=>
        {
            string arg=DevArgs.Value("-lantern-zones");var zones=string.IsNullOrEmpty(arg)?Zones:arg.Split(',',StringSplitOptions.RemoveEmptyEntries);
            Define();foreach(var zone in zones)BuildZone(zone.Trim());
            BatchTools.RegisterScenes();AssetDatabase.SaveAssets();Debug.Log("LANTERN_ZONES_OK "+string.Join(",",zones));
        });
        public static void StageApproach()=>BatchTools.Run(()=>{StageTallowApproach();AssetDatabase.SaveAssets();Debug.Log("LANTERN_APPROACH_OK");});
        /// <summary>Copies docs/writing/*.yarn into Resources and recompiles the Yarn project: a new .yarn file is not in
        /// the compiled program until the project reimports, and a missing node fails silently at runtime.</summary>
        public static void Dialogue()=>BatchTools.Run(()=>{RefreshDialogue();Debug.Log("LANTERN_DIALOGUE_OK");});
        static void RefreshDialogue()
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            foreach(var file in Directory.GetFiles(Path.Combine(root,"docs/writing"),"*.yarn"))File.Copy(file,"Assets/_Project/Resources/Dialogue/"+Path.GetFileName(file),true);
            File.Copy(Path.Combine(root,"docs/writing/barks.json"),"Assets/_Project/Resources/Audio/barks.json",true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset("Assets/_Project/Resources/Dialogue/Lattice.yarnproject",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            var yarn=AssetDatabase.LoadAssetAtPath<Yarn.Unity.YarnProject>("Assets/_Project/Resources/Dialogue/Lattice.yarnproject");
            foreach(var node in new[]{"OrrinFirst","AbriLanternTale","AumiFirst","AumiReturn","SelaRelayLit","SelaWickRelays","HessaConfront","HessaYields","BrekkRations","IshenRecordReturn","PellLampOffer","CorraRationsOffer"})
                if(yarn==null||!yarn.Program.Nodes.ContainsKey(node))throw new InvalidOperationException("Yarn project did not compile "+node);
            Debug.Log("YARN_NODES "+yarn.Program.Nodes.Count);
        }
        static void BuildZone(string zone)
        {
            switch(zone)
            {
                case "WickReach":WickReach();break;
                case "Lanternwick":Lanternwick();break;
                case "LanternGorge":Gorge();break;
                case "SunkenRelay":Relay();break;
                default:throw new InvalidOperationException("Unknown Lantern Road zone "+zone);
            }
        }

        // ---------------------------------------------------------------- definitions
        static T Find<T>(string id)where T:ScriptableObject=>AssetDatabase.LoadAssetAtPath<T>("Assets/_Project/Resources/Definitions/"+typeof(T).Name+"/"+id+".asset");
        static GameObject Prefab(string folder,string id)=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/"+folder+"/"+id+".prefab");
        static GameObject Body(string folder,string id,string fallbackFolder,string fallback)=>Prefab(folder,id)??Prefab(fallbackFolder,fallback);
        static Objective Step(ObjectiveKind kind,string target,int count=1)=>new(){kind=kind,target=target,count=count};
        static void Define()
        {
            var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");
            var dusk=ArenaBuilder.Asset<Hd2dProfile>("Dusk",p=>{p.tint=new Color(1,.9f,.82f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.5f;});
            var cold=Find<Hd2dProfile>("Cold");var cave=Find<Hd2dProfile>("Cave")??cold;
            var sky=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/tallow-sky.mat");
            ArenaBuilder.Asset<ZoneDef>("WickReach",z=>{z.id=z.scene="WickReach";z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=cold;z.skybox=sky;z.enemyThreat=1.25f;z.flightPlane=1;z.cameraClearsScenery=false;});
            ArenaBuilder.Asset<ZoneDef>("Lanternwick",z=>{z.id=z.scene="Lanternwick";z.kind=ZoneKind.GroundSafe;z.cameraProfile=ground;z.hd2dProfile=dusk;z.skybox=sky;z.enemyThreat=1;z.cameraClearsScenery=true;});
            ArenaBuilder.Asset<ZoneDef>("LanternGorge",z=>{z.id=z.scene="LanternGorge";z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=dusk;z.skybox=sky;z.enemyThreat=1.3f;z.cameraClearsScenery=true;});
            ArenaBuilder.Asset<ZoneDef>("SunkenRelay",z=>{z.id=z.scene="SunkenRelay";z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=cave;z.skybox=null;z.enemyThreat=1.35f;z.cameraClearsScenery=true;});

            var bite=Find<AttackDef>("Bite");var volley=Find<AttackDef>("Volley");
            LootTable Loot(string id,params LootEntry[] entries)=>ArenaBuilder.Asset<LootTable>(id,t=>t.entries=entries);
            ArenaBuilder.Asset<EnemyDef>("Gloomgrazer",e=>{e.id="Gloomgrazer";e.prefab=Body("Enemies","Gloomgrazer","Enemies","Ridgehound");e.integrity=170;e.breakThreshold=90;e.speed=3.7f;e.attacks=new[]{bite};
                e.weakness=DamageType.Beam;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.PackHunter;e.xp=40;e.boss=false;e.lootTable=Loot("Gloomgrazer",new LootEntry{id="LatticeFilament",count=1,chance=.3f});});
            ArenaBuilder.Asset<EnemyDef>("MusterScav",e=>{e.id="MusterScav";e.prefab=Body("Enemies","MusterScav","Enemies","SentinelHusk");e.integrity=210;e.breakThreshold=90;e.speed=2.7f;e.attacks=new[]{volley};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Spitter;e.xp=45;e.boss=false;e.lootTable=Loot("MusterScav",new LootEntry{id="ScrapAlloy",count=2,chance=.5f},new LootEntry{id="ChargeCell",count=1,chance=.08f});});
            ArenaBuilder.Asset<EnemyDef>("MusterSergeant",e=>{e.id="MusterSergeant";e.prefab=Body("Enemies","MusterSergeant","Enemies","SentinelHusk");e.integrity=560;e.breakThreshold=170;e.speed=2.2f;e.attacks=new[]{bite};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Sentinel;e.xp=95;e.boss=false;e.lootTable=Loot("MusterSergeant",new LootEntry{id="ScrapAlloy",count=4,chance=1},new LootEntry{id="RepairGel",count=1,chance=.35f});});
            ArenaBuilder.Asset<EnemyDef>("MusterSkiff",e=>{e.id="MusterSkiff";e.prefab=Body("Enemies","MusterSkiff","Enemies","ChoristerDrifter");e.integrity=170;e.breakThreshold=80;e.speed=4.2f;e.attacks=new[]{volley};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Spitter;e.xp=40;e.boss=false;e.lootTable=Loot("MusterSkiff",new LootEntry{id="ScrapAlloy",count=2,chance=.45f});});
            ArenaBuilder.Asset<EnemyDef>("HessaKorl",e=>{e.id="HessaKorl";e.prefab=Body("Enemies","HessaKorl","Enemies","SentinelHusk");e.integrity=20000;e.breakThreshold=700;e.speed=3.4f;e.attacks=new[]{bite};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.Sentinel;e.xp=420;e.boss=true;e.lootTable=Loot("HessaKorl",new LootEntry{id="ChoirResin",count=3,chance=1},new LootEntry{id="RepairGel",count=2,chance=1});});

            ArenaBuilder.Asset<QuestDef>("LanternRoad",q=>{q.id="LanternRoad";q.title="The Lantern Road";
                q.description="Venn wants a route that waits for a moving town. Lanternwick's lanterns once guided stations like Tallow.";
                q.steps=new[]{Step(ObjectiveKind.Reach,"WickReach"),Step(ObjectiveKind.Reach,"Lanternwick"),Step(ObjectiveKind.TalkTo,"Aumi"),
                    Step(ObjectiveKind.Interact,"GorgeRelay",3),Step(ObjectiveKind.Kill,"HessaKorl"),Step(ObjectiveKind.Flag,"lanternwick.chartGiven")};
                q.stepText=new[]{"Take Tallow's old lantern route through the Wick Reach","Dock at Lanternwick's courier pad","Find Lanternwick's lantern-keeper",
                    "Relight the three relays in Lantern Gorge","Stop the scavengers stripping the Sunken Relay","Bring word back to Keeper Aumi"};
                q.rewardXp=700;q.rewardScrip=400;q.rewards=new[]{new Ingredient{id="ModulesT2",count=1},new Ingredient{id="RepairGel",count=3}};q.flagsOnComplete=new[]{"chapter2.complete"};});
            ArenaBuilder.Asset<QuestDef>("PellsLamp",q=>{q.id="PellsLamp";q.title="Light for a Lamp-Keeper";
                q.description="Pell lost her mother's lamp to a grazer in Lantern Gorge and hasn't told Aumi.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"PellsLamp"),Step(ObjectiveKind.Flag,"pell.lampReturned")};
                q.stepText=new[]{"Search the grazer den under the fallen tower in Lantern Gorge","Bring the lamp back to Pell"};
                q.rewardXp=180;q.rewardScrip=120;q.rewards=new[]{new Ingredient{id="ChargeCell",count=2}};q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("IshensRecord",q=>{q.id="IshensRecord";q.title="Ishen's Missing Record";
                q.description="Three record plates went missing in the Sunken Relay below Lantern Gorge.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"RecordPlate",3),Step(ObjectiveKind.Flag,"ishen.recordRestored")};
                q.stepText=new[]{"Recover the record plates in the Sunken Relay","Bring the plates to Ishen at the archive"};
                q.rewardXp=240;q.rewardScrip=150;q.rewards=new[]{new Ingredient{id="EmittersT2",count=1}};q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("UnswornRations",q=>{q.id="UnswornRations";q.title="Rations for the Unsworn";
                q.description="Corra is feeding someone in the orchard shed who can't eat Lanternwick's food.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"MusterRations"),Step(ObjectiveKind.Flag,"unsworn.rationsDelivered")};
                q.stepText=new[]{"Take a ration crate from the scavengers' camp in Lantern Gorge","Bring the rations to the orchard shed"};
                q.rewardXp=200;q.rewardScrip=100;q.rewards=new[]{new Ingredient{id="EdgesT2",count=1}};q.flagsOnComplete=Array.Empty<string>();});

            void Person(string id,string display,bool female,string bodyId,string fallbackFolder,string fallback)
                =>ArenaBuilder.Asset<CharacterDef>(id,c=>{c.id=c.speakerId=id;c.displayName=display;c.female=female;c.natural=Body("Characters",bodyId,fallbackFolder,fallback)??Prefab("Enemies",bodyId);});
            Person("Aumi","Keeper Aumi",true,"AumiBody","Characters","Neve");
            Person("Pell","Pell",true,"PellBody","Characters","SurvivorNatural");
            Person("Ishen","Ishen",false,"IshenBody","Characters","Hal");
            Person("Corra","Corra",true,"CorraBody","Characters","Mira");
            Person("Brekk","Brekk",false,"BrekkBody","Enemies","SentinelHusk");
            Person("Hessa","Hessa Korl",true,"HessaKorl","Enemies","SentinelHusk");
            ArenaBuilder.Asset<ShopInventory>("Lanternwick",s=>
            {
                s.parts=new[]{"EdgesT2","EmittersT2","FramesT2","DrivesT2","ModulesT2"}.Select(Find<TechPartDef>).Where(p=>p!=null).ToArray();
                s.consumables=new[]{"RepairGel","ChargeCell"}.Select(Find<ConsumableDef>).Where(c=>c!=null).ToArray();
                s.materials=new[]{"ScrapAlloy","LatticeFilament","RidgeCrystal"}.Select(Find<MaterialDef>).Where(m=>m!=null).ToArray();
                s.prices=new[]{240,240,260,220,280,22,26,14,16,30};
            });
        }

        // ---------------------------------------------------------------- shared helpers
        static string Kit(string key,string fallback)=>Prefab("Environment",key)!=null?key:fallback;
        static Material WorldMaterial(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/"+name+".mat");
        /// <summary>The canyon ground and strata (generated textures) or, until they are staged, Sorrel's.</summary>
        static Material Canyon(bool cliff)=>WorldMaterial(cliff?"canyon-cliff":"canyon-ground")??WorldMaterial(cliff?"sorrel-rock":"sorrel-ground");

        static Func<Vector2,float> height=_=>0;
        /// <summary>Stand-ins whose proportions differ from the generated piece they replace are scaled to its footprint.</summary>
        static readonly Dictionary<string,float> StandIn=new(){{"OutpostHab",.55f},{"HushwellColumn",.8f}};
        static GameObject Prop(string key,Vector3 at,float size,float yaw=0,bool solid=true,bool grounded=true)
        {
            if(StandIn.TryGetValue(key,out float fit))size*=fit;
            var go=WorldBuilder.Piece(key,Vector3.zero,Vector3.one*size,"Rock",solid);
            go.transform.rotation=Quaternion.Euler(0,yaw,0);var bounds=ModelGeometry.BoundsOf(go);
            float y=grounded?height(new Vector2(at.x,at.z))+at.y:at.y;
            go.transform.position+=new Vector3(at.x,y+.01f,at.z)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            if(!solid)foreach(var c in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            go.isStatic=solid;foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.isStatic=solid;
            return go;
        }
        static GameObject Terrain(string zone,string label,float xMin,float xMax,float zMin,float zMax,float cell,Material material)
        {
            int nx=Mathf.CeilToInt((xMax-xMin)/cell)+1,nz=Mathf.CeilToInt((zMax-zMin)/cell)+1;
            var vertices=new Vector3[nx*nz];var uv=new Vector2[vertices.Length];var triangles=new int[(nx-1)*(nz-1)*6];int ti=0;
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                int i=z*nx+x;var p=new Vector2(xMin+x*cell,zMin+z*cell);vertices[i]=new Vector3(p.x,height(p),p.y);uv[i]=p/8;
                if(x==nx-1||z==nz-1)continue;
                triangles[ti++]=i;triangles[ti++]=i+nx;triangles[ti++]=i+1;triangles[ti++]=i+1;triangles[ti++]=i+nx;triangles[ti++]=i+nx+1;
            }
            var mesh=new Mesh{name=label,indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path="Assets/_Project/Scenes/"+zone+"-Terrain.asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(prior==null){AssetDatabase.CreateAsset(mesh,path);prior=mesh;}else{EditorUtility.CopySerialized(mesh,prior);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(prior);}
            var terrain=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));terrain.isStatic=true;
            terrain.GetComponent<MeshFilter>().sharedMesh=prior;terrain.GetComponent<MeshCollider>().sharedMesh=prior;terrain.GetComponent<Renderer>().sharedMaterial=material;
            terrain.AddComponent<AudioSurface>();
            return terrain;
        }
        static Npc Person(string id,Vector3 at,float yaw,string postFlag,bool post=true,string finalFlag=null,string finalNode=null)
        {
            var npc=WorldBuilder.Npc(id,new Vector3(at.x,height(new Vector2(at.x,at.z))+at.y,at.z));npc.transform.rotation=Quaternion.Euler(0,yaw,0);
            npc.postFlag=postFlag;if(!post)npc.postNode="";npc.finalFlag=finalFlag;npc.finalNode=finalNode;return npc;
        }
        static DiscoveryPoint Discovery(string name,Vector3 at,string prompt,string flag,string report,string node=null,string speaker="Sela",string quest=null,int step=0,float range=3.2f)
        {
            var go=new GameObject(name,typeof(DiscoveryPoint));go.transform.position=new Vector3(at.x,height(new Vector2(at.x,at.z))+at.y,at.z);
            var point=go.GetComponent<DiscoveryPoint>();point.prompt=prompt;point.flag=flag;point.report=report??"";point.situation="";
            point.dialogueNode=node??"";point.dialogueSpeaker=speaker;point.requiredQuest=quest??"";point.requiredStep=step;point.range=range;
            return point;
        }
        static RelayLantern Relay(string name,Vector3 at,string flag,float size,float yaw=0,bool grounded=true)
        {
            var tower=Prop(Kit("LanternTower","HushwellColumn"),at,size,yaw,true,grounded);tower.name=name;
            var lantern=tower.AddComponent<RelayLantern>();lantern.flag=flag;lantern.radius=10;
            lantern.crystals=tower.GetComponentsInChildren<Renderer>();
            var light=new GameObject("Relay light",typeof(Light));light.transform.SetParent(tower.transform,false);
            light.transform.position=tower.transform.position+Vector3.up*size*.85f;var l=light.GetComponent<Light>();
            l.type=LightType.Point;l.color=new Color(1,.78f,.45f);l.range=18;l.intensity=2.4f;l.shadows=LightShadows.None;l.enabled=false;lantern.glow=l;
            return lantern;
        }
        static WarpBeacon Gate(string label,Vector3 at,string scene,string spawn,string flag,string locked,string key="WarpBeacon",float size=3)
        {
            var prop=Prop(Kit(key,"WarpBeacon"),at,size);prop.name=label;
            var warp=prop.AddComponent<WarpBeacon>();warp.prompt=label;warp.range=3.6f;warp.scene=scene;warp.spawn=spawn;warp.requiredFlag=flag??"";warp.lockedPrompt=locked??"";
            return warp;
        }
        static void Lamp(Vector3 at,float range=9,float intensity=1.6f)
        {
            var go=new GameObject("Lamp light",typeof(Light));go.transform.position=new Vector3(at.x,height(new Vector2(at.x,at.z))+at.y,at.z);
            var l=go.GetComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.76f,.45f);l.range=range;l.intensity=intensity;l.shadows=LightShadows.None;
        }
        static void BakeNavigation(string zone,Bounds bounds,Vector3 origin,IEnumerable<Vector3> mustReach)
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
                Debug.Log($"LANTERN_ROUTE_BAKE_OK {zone} points={mustReach.Count()}");
            }
            finally{instance.Remove();}
            string file="Assets/_Project/Scenes/"+zone+"-Navigation.asset";var previous=AssetDatabase.LoadAssetAtPath<NavMeshData>(file);
            if(previous==null){AssetDatabase.CreateAsset(built,file);previous=built;}
            else{EditorUtility.CopySerialized(built,previous);UnityEngine.Object.DestroyImmediate(built);EditorUtility.SetDirty(previous);}
            new GameObject("Walkable paths",typeof(GroundNavigation)).GetComponent<GroundNavigation>().data=previous;
        }
        static float Smooth(float edge0,float edge1,float x)=>Mathf.SmoothStep(0,1,Mathf.Clamp01((x-edge0)/(edge1-edge0)));
        static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));return Vector2.Distance(p,a+d*t);}

        // ---------------------------------------------------------------- the Wick Reach (space combat)
        const float ReachLength=300;
        static void WickReach()
        {
            height=_=>0;
            var scene=WorldBuilder.Begin("WickReach");
            WorldBuilder.Spawn("Arrival",new Vector3(0,1,6));WorldBuilder.Spawn("Lanternwick",new Vector3(0,1,ReachLength-26));WorldBuilder.Spawn("Home",new Vector3(0,1,10));
            WorldArt.Planet("Wick",new Vector3(-150,-90,240),130);
            // A chain of fallen relays, below the flight plane: backdrop, never an obstacle.
            var rng=new System.Random(37);
            for(int i=0;i<14;i++)
            {
                float z=12+i*21,x=(i%2==0?-1:1)*(26+(float)rng.NextDouble()*24);
                var tower=Prop(Kit("LanternTower","HushwellColumn"),new Vector3(x,-14-(float)rng.NextDouble()*8,z),9+(float)rng.NextDouble()*5,(float)rng.NextDouble()*360,false,false);
                tower.transform.rotation=Quaternion.Euler(60+(float)rng.NextDouble()*40,(float)rng.NextDouble()*360,(float)rng.NextDouble()*30);tower.name="Fallen relay "+i;
            }
            for(int i=0;i<10;i++)
                Prop(Kit("LanternShard","CrystalClusterB"),new Vector3((float)rng.NextDouble()*90-45,-6-(float)rng.NextDouble()*6,(float)rng.NextDouble()*ReachLength),2+(float)rng.NextDouble()*2,(float)rng.NextDouble()*360,false,false);
            // Encounters along the lane: raiders first, then the wild Choir swarm the raiders stirred up.
            void Fight(string id,float x,float z,(string enemy,int count)[] waves)
            {
                var encounter=WorldBuilder.Encounter(id,new Vector3(x,1,z),new Vector3(70,6,24),waves[0].enemy,waves[0].count);
                var spawners=new List<Spawner>(encounter.spawners);
                for(int i=1;i<waves.Length;i++)
                {
                    var extra=new GameObject("Spawn_"+waves[i].enemy,typeof(Spawner));extra.transform.SetParent(encounter.transform,false);extra.transform.localPosition=new Vector3(i%2==0?-9:9,0,7);
                    var spawner=extra.GetComponent<Spawner>();spawner.definition=GameCatalog.Find<EnemyDef>(waves[i].enemy);spawner.count=waves[i].count;spawner.radius=3;spawners.Add(spawner);
                }
                encounter.spawners=spawners.ToArray();foreach(var s in spawners)s.transform.position=new Vector3(s.transform.position.x,1,s.transform.position.z);
            }
            Fight("Wick_0",0,60,new[]{("MusterSkiff",3)});
            Fight("Wick_1",0,125,new[]{("ChoristerDart",5),("Shellmine",3)});
            Fight("Wick_2",0,190,new[]{("MusterSkiff",3),("ChoristerDrifter",2)});
            Fight("Wick_3",0,245,new[]{("MusterSkiff",4),("Shellmine",2)});
            // Two lantern relays light the gate; the first introduces the chain.
            var first=Relay("Wick relay 1",new Vector3(-26,-7,150),"wick.relay1",8,0,false);
            Discovery("Relight the first lantern relay",new Vector3(-26,1,150),"Relight the lantern relay","wick.relay1","","SelaWickRelays","Sela",null,0,6.5f);
            Relay("Wick relay 2",new Vector3(26,-7,228),"wick.relay2",8,0,false);
            Discovery("Relight the second lantern relay",new Vector3(26,1,228),"Relight the lantern relay","wick.relay2","",null,"Sela",null,0,6.5f);
            var count=new GameObject("Lantern gate count",typeof(FlagAll)).GetComponent<FlagAll>();count.flags=new[]{"wick.relay1","wick.relay2"};count.result="wick.gateOpen";
            var gate=WorldBuilder.Membrane("Lantern gate",new Vector3(0,1,ReachLength-40),110,false,8);
            var gateKeeper=new GameObject("Lantern gate light",typeof(FlagGate)).GetComponent<FlagGate>();gateKeeper.flag="wick.gateOpen";gateKeeper.showWhenSet=false;gateKeeper.targets=new[]{gate.gameObject};
            WorldBuilder.Label("LANTERN GATE",new Vector3(0,4,ReachLength-42),2.2f);
            WorldBuilder.Dock("Dock — Lanternwick",new Vector3(0,1,ReachLength-14),"Lanternwick","Arrival",WorldBuilder.FlightDockRange);
            WorldBuilder.Label("LANTERNWICK",new Vector3(0,5,ReachLength-10),2.6f);
            WorldBuilder.Warp("Warp — Tallow Drift",new Vector3(-16,1,-2),"TallowApproach","lantern.routeKnown","Wick");
            // Walls at the flight plane; the far reaches stay visible.
            WorldBuilder.Boundary("West reach limit",new Vector3(-58,1,ReachLength*.5f),new Vector3(2,12,ReachLength+60));
            WorldBuilder.Boundary("East reach limit",new Vector3(58,1,ReachLength*.5f),new Vector3(2,12,ReachLength+60));
            WorldBuilder.Boundary("South reach limit",new Vector3(0,1,-16),new Vector3(120,12,2));
            WorldBuilder.Boundary("North reach limit",new Vector3(0,1,ReachLength+4),new Vector3(120,12,2));
            WorldBuilder.Save(scene,"WickReach");Debug.Log("WICK_REACH_OK");
        }

        // ---------------------------------------------------------------- Lanternwick (town)
        static float TownHeight(Vector2 p)
        {
            // A canyon floor (0 m) with a raised east lantern terrace (+3 m, ramp at its south end), a low west orchard
            // terrace (+1.4 m) and carved walls; the south mouth stays low so the fixed camera sees the courier dock.
            float east=Smooth(15,19,p.x)*Smooth(18,34,p.y)*(1-Smooth(96,100,p.y))*3f;
            float west=Smooth(-17,-21,p.x)*Smooth(42,48,p.y)*(1-Smooth(84,88,p.y))*1.4f;
            float walls=Smooth(40,46,Mathf.Abs(p.x))*(14+6*Mathf.PerlinNoise(p.x*.05f+3,p.y*.05f));
            float north=Smooth(106,114,p.y)*(16+5*Mathf.PerlinNoise(p.x*.06f,p.y*.04f));
            float south=Smooth(-34,-42,p.y)*4;
            float texture=.25f*Mathf.PerlinNoise(p.x*.12f+10,p.y*.12f);
            return Mathf.Max(Mathf.Max(east,west),Mathf.Max(walls,Mathf.Max(north,south)))+texture;
        }
        static void Lanternwick()
        {
            height=TownHeight;
            var scene=WorldBuilder.Begin("Lanternwick");
            Terrain("Lanternwick","Canyon terraces",-60,60,-50,125,1,Canyon(false));
            WorldBuilder.Spawn("Arrival",new Vector3(0,0,-14));WorldBuilder.Spawn("Gorge",new Vector3(-30,1.4f,80));WorldBuilder.Spawn("Archive",new Vector3(0,0,92));
            var dock=Prop("LandingPad",new Vector3(0,0,-22),5,0);dock.name="Courier pad";
            var pad=dock.AddComponent<DockingPad>();pad.prompt="Launch — the Wick Reach";pad.range=4;pad.scene="WickReach";pad.spawn="Lanternwick";
            WorldBuilder.Label("COURIER DOCK",new Vector3(0,.1f,-17));
            // The arch market: the great arch spans the main street; stalls under and around it.
            Prop(Kit("AncelaneArch","HushwellColumn"),new Vector3(0,0,6),9,0,false);
            foreach(var (x,z,yaw) in new[]{(-8f,14f,90f),(8f,14f,-90f),(-9f,22f,80f),(9f,23f,-100f)})Prop("DressMarketStall",new Vector3(x,0,z),2.6f,yaw);
            Prop("DressKiosk",new Vector3(-4,0,27),2.2f,10);Prop("DressWaterBarrels",new Vector3(12,0,10),1.2f,40);Prop("DressCargoPallet",new Vector3(-12,0,8),1.1f,20);
            WorldBuilder.Label("ARCH MARKET",new Vector3(0,.1f,12));
            var bench=Prop("LatticeAnvil",new Vector3(13,0,18),1.8f,-90).AddComponent<Bench>();bench.prompt="Link to the market's Lattice Anvil";
            // The Wick House: rooms above the market, a repair cradle and a bed.
            Prop(Kit("CanyonHouse","OutpostHab"),new Vector3(-17,0,31),6,90);
            var repair=Prop("DeckConsole",new Vector3(-12,0,30),1.4f,90).AddComponent<RepairBay>();repair.prompt="Wick House — repair and save";repair.saveSpawn="Arrival";
            WorldBuilder.Label("WICK HOUSE",new Vector3(-13,.1f,34));
            // Homes carved along the canyon walls.
            foreach(var (x,z,yaw) in new[]{(-30f,4f,90f),(-31f,20f,90f),(31f,4f,-90f),(-12f,98f,180f),(14f,99f,180f),(34f,40f,-90f),(35f,70f,-90f),(-32f,30f,90f)})
                Prop(Kit("CanyonHouse","OutpostHab"),new Vector3(x,0,z),5.5f,yaw);
            // The lantern stair: three towers on the east terrace, the keeper below the tallest.
            Relay("Lantern tower south",new Vector3(28,0,40),"lanternwick.lit",14,0);
            Relay("Lantern tower",new Vector3(31,0,64),"lanternwick.lit",18,15);
            Relay("Lantern tower north",new Vector3(27,0,88),"lanternwick.lit",13,-10);
            var always=new GameObject("Town lanterns burn",typeof(FlagAll)).GetComponent<FlagAll>();always.flags=new[]{"met.Aumi"};always.result="lanternwick.lit";
            WorldBuilder.Label("LANTERN STAIR",new Vector3(24,3.2f,33));
            // The archive: a carved facade in the north wall.
            Prop(Kit("AncelaneArch","HushwellColumn"),new Vector3(0,0,104),8,180,false);
            foreach(float x in new[]{-7f,7f})Prop(Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,0,100),5.5f,0);
            Prop(Kit("GreatLantern","CrystalClusterA"),new Vector3(0,0,97),3,0);
            WorldBuilder.Label("ARCHIVE",new Vector3(0,.1f,94));
            // The orchard terrace and the shed someone is hiding in.
            foreach(var (x,z) in new[]{(-25f,50f),(-31f,56f),(-24f,62f),(-30f,70f),(-25f,76f),(-34f,46f)})Prop(Kit("OrchardTree","DressFanShrub"),new Vector3(x,0,z),3.2f,x*z);
            Prop(Kit("CanyonHouse","OutpostHab"),new Vector3(-36,0,64),4.2f,90);
            WorldBuilder.Label("ORCHARD",new Vector3(-26,1.5f,44));
            Gate("Orchard gate — Lantern Gorge",new Vector3(-34,0,84),"LanternGorge","Arrival","met.Aumi","Talk to the lantern-keeper first","AncelaneColumn",4);
            // Lamps and lamp posts along the walk.
            foreach(var (x,z) in new[]{(-4f,-8f),(4f,-8f),(-5f,30f),(5f,40f),(-6f,55f),(6f,70f),(-5f,85f),(18f,26f)})
            {Prop(Kit("LampPost","DressLightBollard"),new Vector3(x,0,z),1.6f,0);Lamp(new Vector3(x,1.6f,z));}
            // People.
            Person("Aumi",new Vector3(22,0,52),-110,"bossdown.HessaKorl",true,"lanternwick.chartGiven","AumiFinal").postNode="AumiReturn";
            Person("Pell",new Vector3(6,0,34),200,"lanternwick.chartGiven");
            Person("Ishen",new Vector3(4,0,92),170,"ishen.recordRestored");
            var corra=Person("Corra",new Vector3(-6,0,17),110,"unsworn.rationsDelivered");
            Person("Brekk",new Vector3(-32,0,60),90,"unsworn.rationsDelivered");
            WorldBuilder.Boundary("West town limit",new Vector3(-44,8,40),new Vector3(2,20,170));
            WorldBuilder.Boundary("East town limit",new Vector3(44,8,40),new Vector3(2,20,170));
            WorldBuilder.Boundary("North town limit",new Vector3(0,8,110),new Vector3(90,20,2));
            WorldBuilder.Boundary("South town limit",new Vector3(0,8,-34),new Vector3(90,20,2));
            BakeNavigation("Lanternwick",new Bounds(new Vector3(0,6,40),new Vector3(96,30,160)),new Vector3(0,0,-14),
                new[]{new Vector3(-6,0,17),new Vector3(22,3,50),new Vector3(6,0,34),new Vector3(4,0,90),new Vector3(-30,1.4f,60),new Vector3(-33,1.4f,80),new Vector3(-11,0,28),new Vector3(13,0,15)});
            WorldBuilder.Save(scene,"Lanternwick");Debug.Log("LANTERNWICK_OK");
        }

        // ---------------------------------------------------------------- Lantern Gorge (open ground combat)
        static readonly Vector2[] GorgePath={new(0,-6),new(2,30),new(8,62),new(2,95),new(-8,120),new(0,150),new(8,180),new(4,205),new(1,221)};
        static readonly Vector2 DenEntry=new(-8,92),Den=new(-26,100);
        static float GorgeDistance(Vector2 p){float d=float.PositiveInfinity;for(int i=1;i<GorgePath.Length;i++)d=Mathf.Min(d,Segment(p,GorgePath[i-1],GorgePath[i]));return Mathf.Min(d,Segment(p,DenEntry,Den)+3);}
        static float GorgeHeight(Vector2 p)
        {
            // A winding canyon floor about 26 m wide; walls rise steeply beyond. Low lip toward the camera side.
            float d=GorgeDistance(p);
            float floor=.35f*Mathf.PerlinNoise(p.x*.1f,p.y*.1f);
            float wall=Smooth(13,19,d)*(9+7*Mathf.PerlinNoise(p.x*.04f+7,p.y*.03f))+Smooth(26,34,d)*8;
            float camera=Mathf.Clamp01(Mathf.InverseLerp(10,-30,p.x))*.45f;   // the west wall is the fixed camera's foreground
            return floor+wall*(1-camera)+Smooth(-12,-18,p.y)*10+Smooth(234,240,p.y)*12;
        }
        static void Gorge()
        {
            height=GorgeHeight;
            var scene=WorldBuilder.Begin("LanternGorge");
            Terrain("LanternGorge","Lantern Gorge",-60,60,-24,250,1,Canyon(false));
            WorldBuilder.Spawn("Arrival",new Vector3(0,0,0));WorldBuilder.Spawn("Relay",new Vector3(0,0,220));
            Gate("Climb to Lanternwick",new Vector3(-4,0,-8),"Lanternwick","Gorge",null,null,"AncelaneColumn",4);
            var rest=Prop("DeckConsole",new Vector3(6,0,8),1.4f,-60).AddComponent<RepairBay>();rest.free=true;rest.saveSpawn="Arrival";rest.prompt="Waystone — repair and save";
            Lamp(new Vector3(6,2,8),8,1.4f);
            // Fallen relay towers lie across the canyon: landmarks and cover.
            foreach(var (x,z,yaw,size) in new[]{(14f,40f,30f,14f),(-14f,84f,-20f,16f),(13f,140f,60f,13f),(-12f,170f,-40f,15f)})
            {
                // Lying along the canyon wall: scenery and a landmark, never a wall across the route.
                var fallen=Prop(Kit("LanternTower","HushwellColumn"),new Vector3(x,0,z),size,yaw,false);fallen.name="Fallen relay tower";
                fallen.transform.rotation=Quaternion.Euler(0,yaw,84);var lying=ModelGeometry.BoundsOf(fallen);
                fallen.transform.position+=new Vector3(x,height(new Vector2(x,z))-.4f,z)-new Vector3(lying.center.x,lying.min.y,lying.center.z);
            }
            foreach(var (x,z) in new[]{(10f,44f),(-10f,88f),(9f,144f),(-9f,166f),(4f,100f)})Prop(Kit("LanternShard","CrystalClusterB"),new Vector3(x,0,z),1.6f,x*13);
            // Relays to relight (the main quest's step), each in an open clearing.
            var gorgeRelays=new[]{(new Vector3(9,0,52),"gorge.relay1","SelaRelayLit"),(new Vector3(-9,0,124),"gorge.relay2",(string)null),(new Vector3(10,0,186),"gorge.relay3",(string)null)};
            int index=0;
            foreach(var (at,flag,node) in gorgeRelays)
            {
                index++;Relay("Gorge relay "+index,at+new Vector3(Mathf.Sign(at.x)*3,0,0),flag,11);
                Discovery("Relight gorge relay "+index,at,"Relight the relay",flag,"GorgeRelay",node,"Sela","LanternRoad",3,3.6f);
            }
            var lit=new GameObject("Gorge relays lit",typeof(FlagAll)).GetComponent<FlagAll>();lit.flags=gorgeRelays.Select(r=>r.Item2).ToArray();lit.result="gorge.relaysLit";
            // Grazers hunt the dark stretches; the scavengers hold a camp by the second relay.
            void Fight(string id,Vector2 at,string enemy,int count,string second=null,int secondCount=0)
            {
                var encounter=WorldBuilder.Encounter(id,new Vector3(at.x,1,at.y),new Vector3(22,5,16),enemy,count);
                var spawner=encounter.GetComponentInChildren<Spawner>();spawner.transform.position=new Vector3(at.x,height(at),at.y+3);
                if(second==null)return;
                var extra=new GameObject("Spawn_"+second,typeof(Spawner));extra.transform.SetParent(encounter.transform,false);
                extra.transform.position=new Vector3(at.x+5,height(at),at.y+6);var s=extra.GetComponent<Spawner>();s.definition=GameCatalog.Find<EnemyDef>(second);s.count=secondCount;s.radius=secondCount>1?2.5f:0;
                encounter.spawners=new[]{spawner,s};
            }
            Fight("Gorge_0",new Vector2(2,26),"Gloomgrazer",3);
            Fight("Gorge_1",new Vector2(6,74),"Gloomgrazer",4);
            Fight("Gorge_Den",new Vector2(-22,98),"Gloomgrazer",2);
            Fight("Gorge_Camp",new Vector2(-6,114),"MusterScav",3,"MusterSergeant",1);
            Fight("Gorge_3",new Vector2(2,156),"Gloomgrazer",3,"MusterScav",2);
            Fight("Gorge_4",new Vector2(6,204),"MusterScav",3,"MusterSergeant",1);
            // The scavengers' camp around the second relay.
            Prop(Kit("MusterLeanTo","DressCargoPallet"),new Vector3(-14,0,112),3.2f,70);
            Prop(Kit("MusterCutterRig","DressWorkLamp"),new Vector3(-6,0,128),2.2f,-30);
            foreach(var (x,z) in new[]{(-12f,106f),(-11f,108f),(-3f,116f)})Prop(Kit("MusterCrate","DeckCrate"),new Vector3(x,0,z),1.1f,x*7);
            Lamp(new Vector3(-10,1.2f,110),10,1.8f);
            Discovery("Muster ration crate",new Vector3(-12,0,104),"Take a Muster ration crate","unsworn.rationsFound","MusterRations",null,"Sela","UnswornRations",0);
            // The grazer den under the fallen tower holds Pell's lamp.
            Discovery("Pell's lamp",Den.x*Vector3.right+Den.y*Vector3.forward,"Pick up the chewed lamp","pell.lampFound","PellsLamp",null,"Sela","PellsLamp",0);
            Prop(Kit("LampPost","DressLightBollard"),new Vector3(Den.x+.8f,0,Den.y+.5f),1f,80,false);
            WorldBuilder.Label("SCAVENGER CAMP",new Vector3(-8,.2f,100));
            // The Sunken Relay's door at the head of the gorge opens once the three relays burn.
            WorldBuilder.Spawn("Relay",new Vector3(0,0,220));
            Prop(Kit("AncelaneArch","HushwellColumn"),new Vector3(0,0,233),8,0,false);
            Gate("Enter the Sunken Relay",new Vector3(0,0,229),"SunkenRelay","Arrival","gorge.relaysLit","The relay door is dark: relight the gorge relays","AncelaneColumn",3.5f);
            var anvil=Prop("LatticeAnvil",new Vector3(-6,0,212),1.8f,40).AddComponent<Bench>();anvil.prompt="Link to the Lattice Anvil";
            WorldBuilder.Label("SUNKEN RELAY",new Vector3(0,.2f,224));
            WorldBuilder.Boundary("South gorge limit",new Vector3(0,8,-16),new Vector3(120,20,2));
            WorldBuilder.Boundary("North gorge limit",new Vector3(0,8,238),new Vector3(120,20,2));
            BakeNavigation("LanternGorge",new Bounds(new Vector3(0,8,110),new Vector3(110,40,260)),new Vector3(0,0,0),
                GorgePath.Select(p=>new Vector3(p.x,0,p.y)).Concat(new[]{new Vector3(Den.x+2,0,Den.y-1),new Vector3(-11,0,103)}));
            WorldBuilder.Save(scene,"LanternGorge");Debug.Log("LANTERN_GORGE_OK");
        }

        // ---------------------------------------------------------------- the Sunken Relay (dungeon)
        static void Room(Vector3 centre,Vector3 size,string label,bool northOpen=true,bool southOpen=true,float doorway=6)
        {
            var floor=WorldBuilder.Piece("DeckFloor",new Vector3(centre.x,-.08f,centre.z),new Vector3(size.x,.16f,size.z),"Ground");floor.name=label+" floor";
            foreach(var r in floor.GetComponentsInChildren<Renderer>())r.sharedMaterial=Canyon(false);
            float wall=4.5f;var material=Canyon(true);
            void Wall(Vector3 at,Vector3 extent){var w=WorldBuilder.Piece("DeckWall",at,extent,"Rock");foreach(var r in w.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;w.name=label+" wall";foreach(var t in w.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;}
            Wall(new Vector3(centre.x-size.x*.5f,wall*.5f,centre.z),new Vector3(.8f,wall,size.z));
            Wall(new Vector3(centre.x+size.x*.5f,wall*.5f,centre.z),new Vector3(.8f,wall,size.z));
            foreach(var (open,z) in new[]{(northOpen,centre.z+size.z*.5f),(southOpen,centre.z-size.z*.5f)})
            {
                if(!open){Wall(new Vector3(centre.x,wall*.5f,z),new Vector3(size.x,wall,.8f));continue;}
                float side=(size.x-doorway)*.5f;
                Wall(new Vector3(centre.x-doorway*.5f-side*.5f,wall*.5f,z),new Vector3(side,wall,.8f));
                Wall(new Vector3(centre.x+doorway*.5f+side*.5f,wall*.5f,z),new Vector3(side,wall,.8f));
            }
            WorldBuilder.Label(label,new Vector3(centre.x,.1f,centre.z-size.z*.5f+2.5f),1.6f);
        }
        static void Relay()
        {
            height=_=>0;
            var scene=WorldBuilder.Begin("SunkenRelay");
            WorldBuilder.Spawn("Arrival",new Vector3(0,0,3));
            Room(new Vector3(0,0,10),new Vector3(18,0,20),"ENTRY HALL",true,false);
            Room(new Vector3(0,0,32),new Vector3(8,0,24),"CUT CORRIDOR",true,true,5);
            Room(new Vector3(0,0,64),new Vector3(30,0,40),"FLOODED GALLERY",true,true,6);
            Room(new Vector3(0,0,100),new Vector3(22,0,32),"LOWER VAULT",true,true,6);
            Room(new Vector3(0,0,140),new Vector3(38,0,48),"GREAT LANTERN",false,true,6);
            Gate("Climb out — Lantern Gorge",new Vector3(0,0,1),"LanternGorge","Relay",null,null,"AncelaneColumn",3);
            Discovery("Cut lantern housings",new Vector3(0,0,14),"Look at the cut lantern housings","relay.entered","","SelaSunkenRelay","Taren");
            foreach(var (x,z) in new[]{(-6f,18f),(6f,18f),(-12f,50f),(12f,50f),(-12f,78f),(12f,78f),(-8f,90f),(8f,90f)})
            {Prop(Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,0,z),4.2f,x*z);}
            foreach(var (x,z) in new[]{(-6f,6f),(6f,26f),(-10f,60f),(10f,68f),(0f,96f),(-8f,128f),(8f,128f)})Lamp(new Vector3(x,2.6f,z),11,1.7f);
            // Record plates for Ishen.
            var plates=new[]{new Vector3(-7,0,16),new Vector3(12,0,76),new Vector3(-9,0,108)};
            for(int i=0;i<plates.Length;i++)
            {
                Prop(Kit("LanternShard","CrystalClusterA"),plates[i]+new Vector3(.9f,0,.6f),.9f,i*70,false);
                Discovery("Record plate "+(i+1),plates[i],"Take the record plate","relay.plate"+(i+1),"RecordPlate",null,"Sela","IshensRecord",0);
            }
            // The scavengers' work crews.
            void Fight(string id,Vector3 at,string enemy,int count,string second=null,int secondCount=0)
            {
                var encounter=WorldBuilder.Encounter(id,new Vector3(at.x,1,at.z),new Vector3(14,5,10),enemy,count);
                var spawner=encounter.GetComponentInChildren<Spawner>();spawner.transform.position=new Vector3(at.x,0,at.z+4);
                if(second==null)return;
                var extra=new GameObject("Spawn_"+second,typeof(Spawner));extra.transform.SetParent(encounter.transform,false);
                extra.transform.position=new Vector3(at.x+4,0,at.z+6);var s=extra.GetComponent<Spawner>();s.definition=GameCatalog.Find<EnemyDef>(second);s.count=secondCount;s.radius=secondCount>1?2.5f:0;
                encounter.spawners=new[]{spawner,s};
            }
            Fight("Relay_0",new Vector3(0,0,30),"MusterScav",2);
            Fight("Relay_1",new Vector3(0,0,58),"Gloomgrazer",4,"MusterScav",2);
            Fight("Relay_2",new Vector3(0,0,96),"MusterSergeant",1,"MusterScav",2);
            foreach(var (x,z) in new[]{(-9f,40f),(9f,82f),(-7f,92f)})Prop(Kit("MusterCrate","DeckCrate"),new Vector3(x,0,z),1.1f,z);
            Prop(Kit("MusterCutterRig","DressWorkLamp"),new Vector3(7,0,104),2.2f,200);
            // The checkpoint before the captain: under a minute from her floor.
            var cradle=Prop("DeckConsole",new Vector3(8,0,110),1.4f,-90).AddComponent<RepairBay>();cradle.free=true;cradle.saveSpawn="Vault";cradle.prompt="Relay cradle — repair and save";
            WorldBuilder.Spawn("Vault",new Vector3(4,0,108));
            var anvil=Prop("LatticeAnvil",new Vector3(-8,0,112),1.8f,90).AddComponent<Bench>();anvil.prompt="Link to the Lattice Anvil";
            // The Great Lantern chamber: four columns survive every phase as cover against the flash.
            Prop(Kit("GreatLantern","CrystalClusterA"),new Vector3(0,0,158),5,0);
            foreach(var (x,z) in new[]{(-10f,132f),(10f,132f),(-10f,150f),(10f,150f)})Prop(Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,0,z),4.5f,x+z);
            Lamp(new Vector3(0,4,150),18,2.2f);
            Discovery("Confront the scavenger captain",new Vector3(0,0,118),"Confront the scavenger captain","relay.hessaConfronted","","HessaConfront","Hessa",null,0,4);
            var entry=WorldBuilder.Membrane("Great Lantern seal",new Vector3(0,.3f,117),7,true,4.5f);
            var duel=WorldBuilder.Encounter("Relay_Hessa",new Vector3(0,1,138),new Vector3(30,5,28),"HessaKorl",1,new[]{entry});
            duel.GetComponentInChildren<Spawner>().transform.position=new Vector3(0,0,148);
            var arm=new GameObject("Duel waits for the confrontation",typeof(FlagGate)).GetComponent<FlagGate>();arm.flag="relay.hessaConfronted";arm.showWhenSet=true;arm.targets=new[]{duel.gameObject};
            // After the duel: the captain under guard, and the lift home.
            var hessa=Person("Hessa",new Vector3(9,0,154),-120,"",false);hessa.firstNode="HessaYields";hessa.repeatNode="HessaRepeat";
            var yielded=new GameObject("Hessa under guard",typeof(FlagGate)).GetComponent<FlagGate>();yielded.flag="bossdown.HessaKorl";yielded.showWhenSet=true;yielded.targets=new[]{hessa.gameObject};
            Gate("Ride the relay lift — Lanternwick",new Vector3(-6,0,160),"Lanternwick","Archive","bossdown.HessaKorl","The lift won't run while the scavengers hold the chamber","AncelaneColumn",3);
            BakeNavigation("SunkenRelay",new Bounds(new Vector3(0,4,85),new Vector3(50,20,180)),new Vector3(0,0,3),
                new[]{new Vector3(0,0,30),new Vector3(10,0,74),new Vector3(-8,0,108),new Vector3(0,0,118),new Vector3(0,0,140)});
            WorldBuilder.Save(scene,"SunkenRelay");Debug.Log("SUNKEN_RELAY_OK");
        }

        // ---------------------------------------------------------------- Tallow approach: the warp out
        static void StageTallowApproach()
        {
            var approach=EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowApproach.unity");
            foreach(var old in UnityEngine.Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Where(w=>w.scene=="WickReach").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(var old in UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Where(s=>s.id=="Wick").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            var warp=WorldBuilder.Warp("Warp — The Wick Reach",new Vector3(24,1,-18),"WickReach","lantern.routeKnown","Arrival");
            warp.lockedPrompt="No course plotted beyond the approach";
            WorldBuilder.Spawn("Wick",new Vector3(17,1,-21));
            EditorSceneManager.MarkSceneDirty(approach);EditorSceneManager.SaveScene(approach);Debug.Log("TALLOW_APPROACH_WARP_OK");
        }
    }
}
