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
        /// <summary>The generated canyon floor and strata textures become world materials, cloned from Sorrel's so the
        /// toon shading and tiling match.</summary>
        static void CanyonMaterials()
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            var made=new List<(string texture,string material,string template)>{("canyon-floor","canyon-ground","sorrel-ground"),("canyon-cliff","canyon-cliff","sorrel-rock")};
            foreach(var (texture,_,_) in made)
            {
                string source="art-src/Generated/C9-Lantern/refs/"+texture+".png";
                if(File.Exists(Path.Combine(root,source)))PackStaging.StageFile(source,"Art/Generated/Lantern/"+texture+".png");
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var (texture,material,template) in made)
            {
                string path="Assets/_Project/Art/Generated/Lantern/"+texture+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
                importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;importer.maxTextureSize=1024;importer.SaveAndReimport();
                var source=WorldMaterial(template);if(source==null)continue;
                string target="Assets/_Project/Resources/WorldMaterials/"+material+".mat";var copy=AssetDatabase.LoadAssetAtPath<Material>(target);
                if(copy==null){copy=new Material(source){name=material};AssetDatabase.CreateAsset(copy,target);}else copy.CopyPropertiesFromMaterial(source);
                copy.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));copy.SetTextureScale("_BaseMap",Vector2.one);EditorUtility.SetDirty(copy);
            }
            AssetDatabase.SaveAssets();
        }
        static void Define()
        {
            CanyonMaterials();
            var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");
            var dusk=ArenaBuilder.Asset<Hd2dProfile>("Dusk",p=>{p.tint=new Color(1,.9f,.82f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.5f;});
            var cold=Find<Hd2dProfile>("Cold");var cave=Find<Hd2dProfile>("Cave")??cold;
            var sky=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/tallow-sky.mat");
            ArenaBuilder.Asset<ZoneDef>("WickReach",z=>{z.id=z.scene="WickReach";z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=cold;z.skybox=sky;z.enemyThreat=1.5f;z.flightPlane=1;z.cameraClearsScenery=false;});
            ArenaBuilder.Asset<ZoneDef>("Lanternwick",z=>{z.id=z.scene="Lanternwick";z.kind=ZoneKind.GroundSafe;z.cameraProfile=ground;z.hd2dProfile=dusk;z.skybox=sky;z.enemyThreat=1;z.cameraClearsScenery=true;});
            ArenaBuilder.Asset<ZoneDef>("LanternGorge",z=>{z.id=z.scene="LanternGorge";z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=dusk;z.skybox=sky;z.enemyThreat=1.6f;z.cameraClearsScenery=true;});
            ArenaBuilder.Asset<ZoneDef>("SunkenRelay",z=>{z.id=z.scene="SunkenRelay";z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=cave;z.skybox=null;z.enemyThreat=1.7f;z.cameraClearsScenery=true;});

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
            ArenaBuilder.Asset<EnemyDef>("HessaKorl",e=>{e.id="HessaKorl";e.prefab=Body("Enemies","HessaKorl","Enemies","SentinelHusk");e.integrity=44000;e.breakThreshold=700;e.speed=3.4f;e.attacks=new[]{bite};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.Sentinel;e.xp=420;e.boss=true;e.yieldsOnDefeat=true;e.lootTable=Loot("HessaKorl",new LootEntry{id="ChoirResin",count=3,chance=1},new LootEntry{id="RepairGel",count=2,chance=1});});

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
                q.stepText=new[]{"Search the grazer den under the fallen tower, west of the path before the second relay","Bring the lamp back to Pell"};
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
                =>ArenaBuilder.Asset<CharacterDef>(id,c=>{c.id=c.speakerId=id;c.displayName=display;c.female=female;c.natural=Prefab("Characters",bodyId)??Prefab("Enemies",bodyId)??Prefab(fallbackFolder,fallback);});
            Person("Aumi","Keeper Aumi",true,"AumiBody","Characters","Neve");
            Person("Pell","Pell",true,"PellBody","Characters","SurvivorNatural");
            Person("Ishen","Ishen",false,"IshenBody","Characters","Hal");
            Person("Corra","Corra",true,"CorraBody","Characters","Mira");
            Person("Brekk","Brekk",false,"BrekkBody","Enemies","SentinelHusk");
            Person("Hessa","Hessa Korl",true,"HessaKorl","Enemies","SentinelHusk");
            ArenaBuilder.Asset<ShopInventory>("Lanternwick",s=>
            {
                s.title="ARCH MARKET";
                s.parts=new[]{"EdgesT2","EmittersT2","FramesT2","DrivesT2","ModulesT2"}.Select(Find<TechPartDef>).Where(p=>p!=null).ToArray();
                s.consumables=new[]{"RepairGel","ChargeCell"}.Select(Find<ConsumableDef>).Where(c=>c!=null).ToArray();
                s.materials=new[]{"ScrapAlloy","LatticeFilament","RidgeCrystal"}.Select(Find<MaterialDef>).Where(m=>m!=null).ToArray();
                s.prices=new[]{240,240,260,220,280,22,26,14,16,30};
            });
        }

        // ---------------------------------------------------------------- shared helpers
        static string Kit(string key,string fallback)=>Prefab("Environment",key)!=null?key:fallback;
        /// <summary>A dead relay: its crystal unlit (the generated emission off), so relit relays stand out.</summary>
        static GameObject Dark(GameObject go)
        {
            var block=new MaterialPropertyBlock();block.SetColor("_EmissionColor",Color.black);
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.SetPropertyBlock(block);return go;
        }
        static Material WorldMaterial(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/"+name+".mat");
        /// <summary>The canyon ground and strata (generated textures) or, until they are staged, Sorrel's.</summary>
        static Material Canyon(bool cliff)=>WorldMaterial(cliff?"canyon-cliff":"canyon-ground")??WorldMaterial(cliff?"sorrel-rock":"sorrel-ground");

        static Func<Vector2,float> height=_=>0;
        /// <summary>Ground above this height that is level is "above the cliffs" (strata texture, not floor).</summary>
        static float CliffTop=3.5f;
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
            if(fadeTall&&bounds.size.y>=TallEnoughToBlock)Fadeable(go);
            return go;
        }
        /// <summary>Ground zones: the camera flies over the town. Anything this tall can come between it and the hero.</summary>
        static bool fadeTall;
        const float TallEnoughToBlock=2.5f;
        static void Fadeable(GameObject go)
        {
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
            {
                if(material==null||!material.HasProperty("_OccFade")||material.IsKeywordEnabled("_OCCFADE_ON"))continue;
                material.EnableKeyword("_OCCFADE_ON");material.SetFloat("_OccFadeToggle",1);material.SetFloat("_OccFade",0);EditorUtility.SetDirty(material);
            }
            if(go.GetComponent<SceneryOcclusion>()==null)go.AddComponent<SceneryOcclusion>();
        }
        static Mesh SaveMesh(Mesh mesh,string path)
        {
            var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(prior==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,prior);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(prior);return prior;
        }
        /// <summary>A heightfield landform in three parts: an invisible full-resolution collider (walls stop bodies and
        /// navigation reads it), the walkable floor drawn with the ground texture, and the cliff faces drawn with the
        /// strata texture mapped along each wall so its bands stay horizontal.</summary>
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
            var full=new Mesh{name=label+" collision",indexFormat=IndexFormat.UInt32};full.vertices=vertices;full.triangles=triangles;full.RecalculateNormals();full.RecalculateBounds();
            var floor=new List<int>();var cliffVerts=new List<Vector3>();var cliffUv=new List<Vector2>();var cliffTris=new List<int>();
            for(int t=0;t<triangles.Length;t+=3)
            {
                Vector3 a=vertices[triangles[t]],b=vertices[triangles[t+1]],c=vertices[triangles[t+2]];
                var n=Vector3.Cross(b-a,c-a).normalized;float h=(a.y+b.y+c.y)/3;
                if(Mathf.Abs(n.y)>=.78f&&h<=CliffTop){floor.Add(triangles[t]);floor.Add(triangles[t+1]);floor.Add(triangles[t+2]);continue;}
                bool facesX=Mathf.Abs(n.x)>Mathf.Abs(n.z);
                foreach(var v in new[]{a,b,c})
                {
                    cliffTris.Add(cliffVerts.Count);cliffVerts.Add(v);
                    cliffUv.Add(Mathf.Abs(n.y)>=.78f?new Vector2(v.x,v.z)/8:new Vector2((facesX?v.z:v.x)/7,v.y/5));
                }
            }
            var ground=new Mesh{name=label,indexFormat=IndexFormat.UInt32};ground.vertices=vertices;ground.uv=uv;ground.triangles=floor.ToArray();ground.RecalculateNormals();ground.RecalculateBounds();
            var cliffs=new Mesh{name=label+" cliffs",indexFormat=IndexFormat.UInt32};cliffs.SetVertices(cliffVerts);cliffs.SetUVs(0,cliffUv);cliffs.SetTriangles(cliffTris,0);cliffs.RecalculateNormals();cliffs.RecalculateBounds();
            full=SaveMesh(full,"Assets/_Project/Scenes/"+zone+"-Terrain.asset");
            ground=SaveMesh(ground,"Assets/_Project/Scenes/"+zone+"-Ground.asset");
            cliffs=SaveMesh(cliffs,"Assets/_Project/Scenes/"+zone+"-Cliffs.asset");
            // The collision body keeps an enabled (never drawn) renderer: the ground camera treats only rendered
            // colliders as scenery to look past, as it does Sorrel's basin.
            var terrain=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));terrain.isStatic=true;
            terrain.GetComponent<MeshCollider>().sharedMesh=full;terrain.GetComponent<MeshFilter>().sharedMesh=full;terrain.AddComponent<AudioSurface>();
            var hidden=terrain.GetComponent<MeshRenderer>();hidden.sharedMaterial=material;hidden.forceRenderingOff=true;hidden.shadowCastingMode=ShadowCastingMode.Off;
            var floorView=new GameObject(label+" floor",typeof(MeshFilter),typeof(MeshRenderer));floorView.transform.SetParent(terrain.transform,false);floorView.isStatic=true;
            floorView.GetComponent<MeshFilter>().sharedMesh=ground;floorView.GetComponent<Renderer>().sharedMaterial=material;
            var cliffView=new GameObject(label+" cliffs",typeof(MeshFilter),typeof(MeshRenderer));cliffView.transform.SetParent(terrain.transform,false);cliffView.isStatic=true;
            cliffView.GetComponent<MeshFilter>().sharedMesh=cliffs;cliffView.GetComponent<Renderer>().sharedMaterial=Canyon(true);
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
        static RelayLantern Relay(string name,Vector3 at,string flag,float size,float yaw=0,bool grounded=true,bool solid=true)
        {
            var tower=Prop(Kit("LanternTower","HushwellColumn"),at,size,yaw,solid,grounded);tower.name=name;
            return Lantern(tower,flag,size);
        }
        static RelayLantern Lantern(GameObject body,string flag,float size)
        {
            var lantern=body.AddComponent<RelayLantern>();lantern.flag=flag;lantern.radius=10;
            lantern.crystals=body.GetComponentsInChildren<Renderer>();
            var light=new GameObject("Relay light",typeof(Light));light.transform.SetParent(body.transform,false);
            light.transform.position=body.transform.position+Vector3.up*size*.85f;var l=light.GetComponent<Light>();
            l.type=LightType.Point;l.color=new Color(1,.78f,.45f);l.range=18;l.intensity=2.4f;l.shadows=LightShadows.None;l.enabled=false;lantern.glow=l;
            return lantern;
        }
        /// <summary>Strands of relay light: the Wick's lantern curtain and the door the relay's captain seals, never Choir tissue.</summary>
        static Material CurtainMaterial(string zone)
        {
            var beam=new Material(Resources.Load<Material>("Effects/flare_01"));beam.SetTexture("_BaseMap",Texture2D.whiteTexture);
            AssetDatabase.CreateAsset(beam,"Assets/_Project/Scenes/"+zone+"-Curtain.mat");return beam;
        }
        static void Strand(Transform parent,Vector3 from,Vector3 to,Material beam,float width,Color bottom,Color top)
        {
            var line=new GameObject("Curtain strand",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(parent,true);
            line.useWorldSpace=false;line.positionCount=2;line.SetPosition(0,line.transform.InverseTransformPoint(from));line.SetPosition(1,line.transform.InverseTransformPoint(to));
            line.startWidth=line.endWidth=width;line.sharedMaterial=beam;line.startColor=bottom;line.endColor=top;line.shadowCastingMode=ShadowCastingMode.Off;
        }
        static WarpBeacon Gate(string label,Vector3 at,string scene,string spawn,string flag,string locked,string key="WarpBeacon",float size=3)
        {
            var prop=Prop(Kit(key,"WarpBeacon"),at,size);prop.name=label;
            var warp=prop.AddComponent<WarpBeacon>();warp.prompt=label;warp.range=3.6f;warp.scene=scene;warp.spawn=spawn;warp.requiredFlag=flag??"";warp.lockedPrompt=locked??"";
            WorldBuilder.Label(label,new Vector3(at.x,height(new Vector2(at.x,at.z))+size+.8f,at.z),2.2f).transform.SetParent(prop.transform,true);
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
        /// <summary>Small, non-blocking floor detail scattered where it never meets a route point or prompt.</summary>
        static void Scatter(System.Random rng,int count,Func<Vector2> pick,Func<Vector2,bool> allowed,params (string key,float size,bool solid)[] kinds)
        {
            int placed=0;
            for(int tries=0;placed<count&&tries<count*12;tries++)
            {
                var p=pick();if(!allowed(p))continue;var (key,size,solid)=kinds[rng.Next(kinds.Length)];if(Prefab("Environment",key)==null)continue;
                Prop(key,new Vector3(p.x,0,p.y),size*(.75f+.5f*(float)rng.NextDouble()),(float)rng.NextDouble()*360,solid);placed++;
            }
        }
        /// <summary>Rock outcrops and strata ledges along a cliff foot, so canyon walls read as rock from the fixed camera.</summary>
        static void CliffFoot(IEnumerable<Vector3> points,int seed)
        {
            var rng=new System.Random(seed);string[] kinds={"RidgeRock1","RidgeRock2","RidgeRock3","DressStrataOutcrop","DressStrataLedge","DressBoulders"};
            foreach(var p in points)
            {
                string key=kinds[rng.Next(kinds.Length)];if(Prefab("Environment",key)==null)continue;
                float size=key.StartsWith("Ridge")?3.5f+(float)rng.NextDouble()*3:key=="DressBoulders"?1.6f+(float)rng.NextDouble():2.2f+(float)rng.NextDouble()*1.5f;
                Prop(key,p,size,(float)rng.NextDouble()*360,true);
            }
        }
        static float Smooth(float edge0,float edge1,float x)=>Mathf.SmoothStep(0,1,Mathf.Clamp01((x-edge0)/(edge1-edge0)));
        static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));return Vector2.Distance(p,a+d*t);}

        // ---------------------------------------------------------------- the Wick Reach (space combat)
        const float ReachLength=300;
        static void WickReach()
        {
            height=_=>0;fadeTall=false;
            var scene=WorldBuilder.Begin("WickReach");
            WorldBuilder.Spawn("Arrival",new Vector3(0,1,6));WorldBuilder.Spawn("Lanternwick",new Vector3(0,1,ReachLength-26));WorldBuilder.Spawn("Home",new Vector3(0,1,10));
            WorldArt.Planet("Wick",new Vector3(-150,-90,240),130);
            // A chain of fallen relays, below the flight plane: backdrop, never an obstacle.
            var rng=new System.Random(37);
            for(int i=0;i<20;i++)
            {
                float z=10+i*14,x=(i%2==0?-1:1)*(18+(float)rng.NextDouble()*28);
                var tower=Prop(Kit("LanternTower","HushwellColumn"),new Vector3(x,-14-(float)rng.NextDouble()*8,z),9+(float)rng.NextDouble()*5,(float)rng.NextDouble()*360,false,false);
                tower.transform.rotation=Quaternion.Euler(60+(float)rng.NextDouble()*40,(float)rng.NextDouble()*360,(float)rng.NextDouble()*30);tower.name="Fallen relay "+i;Dark(tower);
            }
            for(int i=0;i<26;i++)
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
            // Backdrop below the flight plane: flying to the objective must not cost a collision with a tower that reads as passable.
            var first=Relay("Wick relay 1",new Vector3(-26,-7,150),"wick.relay1",8,0,false,false);
            Discovery("Relight the first lantern relay",new Vector3(-26,1,150),"Relight the lantern relay","wick.relay1","","SelaWickRelays","Sela",null,0,6.5f);
            Relay("Wick relay 2",new Vector3(26,-7,228),"wick.relay2",8,0,false,false);
            Discovery("Relight the second lantern relay",new Vector3(26,1,228),"Relight the lantern relay","wick.relay2","",null,"Sela",null,0,6.5f);
            var count=new GameObject("Lantern gate count",typeof(FlagAll)).GetComponent<FlagAll>();count.flags=new[]{"wick.relay1","wick.relay2"};count.result="wick.gateOpen";
            var gate=WorldBuilder.Membrane("Lantern gate",new Vector3(0,1,ReachLength-40),110,false,8);
            // Not the Choir's tissue: a curtain of relay light, amber, strung between lantern towers.
            var curtain=new MaterialPropertyBlock();curtain.SetColor("_BaseColor",new Color(1,.62f,.24f,.55f));curtain.SetColor("_EmissionColor",new Color(1.6f,.9f,.3f));
            foreach(var r in gate.GetComponentsInChildren<Renderer>())r.enabled=false;
            var beam=CurtainMaterial("WickReach");
            for(float x=-54;x<=54;x+=4.5f)
                Strand(gate.transform,new Vector3(x,-5,ReachLength-40),new Vector3(x,5,ReachLength-40),beam,.45f,new Color(1.6f,.85f,.3f,.15f),new Color(1.8f,1f,.4f,.85f));
            foreach(float x in new[]{-56f,-20f,20f,56f})Prop(Kit("LanternTower","HushwellColumn"),new Vector3(x,-9,ReachLength-40),10,x,false,false);
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
            float walls=Smooth(33,39,Mathf.Abs(p.x))*(14+6*Mathf.PerlinNoise(p.x*.05f+3,p.y*.05f))+Smooth(44,52,Mathf.Abs(p.x))*8;
            float north=Smooth(106,114,p.y)*(16+5*Mathf.PerlinNoise(p.x*.06f,p.y*.04f));
            float south=Smooth(-34,-42,p.y)*4;
            float texture=.25f*Mathf.PerlinNoise(p.x*.12f+10,p.y*.12f);
            return Mathf.Max(Mathf.Max(east,west),Mathf.Max(walls,Mathf.Max(north,south)))+texture;
        }
        static void Lanternwick()
        {
            height=TownHeight;CliffTop=3.6f;fadeTall=true;
            var scene=WorldBuilder.Begin("Lanternwick");
            Terrain("Lanternwick","Canyon terraces",-60,60,-50,125,1,Canyon(false));
            WorldBuilder.Spawn("Arrival",new Vector3(0,0,-14));WorldBuilder.Spawn("Gorge",new Vector3(-30,1.4f,80));WorldBuilder.Spawn("Archive",new Vector3(0,0,92));
            var dock=Prop("LandingPad",new Vector3(0,0,-22),5,0);dock.name="Courier pad";
            var pad=dock.AddComponent<DockingPad>();pad.prompt="Launch — the Wick Reach";pad.range=4;pad.scene="WickReach";pad.spawn="Lanternwick";
            WorldBuilder.Label("COURIER DOCK",new Vector3(0,.1f,-17));
            // The arch market: the great arch spans the main street; stalls under and around it.
            // Low enough to read as a gateway, and it fades as the camera passes over: at 7 m it filled the screen around Corra's stall.
            Prop(Kit("AncelaneArch","HushwellColumn"),new Vector3(0,0,4),6,0,false).name="Market arch";
            foreach(var (x,z,yaw) in new[]{(-8f,14f,90f),(8f,14f,-90f),(-9f,22f,80f),(9f,23f,-100f)})Prop("DressMarketStall",new Vector3(x,0,z),2.6f,yaw);
            Prop("DressKiosk",new Vector3(-4,0,27),2.2f,10);Prop("DressWaterBarrels",new Vector3(12,0,10),1.2f,40);Prop("DressCargoPallet",new Vector3(-12,0,8),1.1f,20);
            WorldBuilder.Label("ARCH MARKET",new Vector3(0,.1f,12));
            var bench=Prop("LatticeAnvil",new Vector3(13,0,18),1.8f,-90).AddComponent<Bench>();bench.prompt="Fabricate — Lattice Anvil";
            // The Wick House: rooms above the market, a repair cradle and a bed.
            Prop(Kit("CanyonHouse","OutpostHab"),new Vector3(-17,0,31),6,90);
            var repair=Prop("DeckConsole",new Vector3(-12,0,30),1.4f,90).AddComponent<RepairBay>();repair.prompt="Wick House — repair and save";repair.saveSpawn="Arrival";
            WorldBuilder.Label("WICK HOUSE",new Vector3(-13,.1f,34));
            // Homes: carved along the canyon walls and lining the main street up to the archive, doors to the street.
            foreach(var (x,z,yaw) in new[]{(-30f,4f,90f),(-31f,20f,90f),(31f,4f,-90f),(-12f,98f,180f),(14f,99f,180f),(30f,46f,-90f),(30f,74f,-90f),(-30f,32f,90f),
                (-12f,40f,90f),(-12f,56f,90f),(-12f,70f,90f),(-12f,84f,90f),(11f,40f,-90f),(11f,52f,-90f),(11f,80f,-90f),(-22f,-4f,90f),(22f,-6f,-90f),(-20f,92f,140f),(22f,92f,-140f)})
                Prop(Kit("CanyonHouse","OutpostHab"),new Vector3(x,0,z),x*x>400?5.5f:4.8f,yaw);
            // The street itself: lamps on both sides and the clutter of people who live there.
            for(float z=-6;z<92;z+=11)foreach(int side in new[]{-1,1})
            {var at=new Vector3(side*6.5f,0,z+(side>0?5:0));Prop(Kit("LampPost","DressLightBollard"),at,1.6f,0);Lamp(at+Vector3.up*1.6f,8,1.3f);}
            var trng=new System.Random(29);
            Scatter(trng,60,()=>new Vector2((float)trng.NextDouble()*60-30,(float)trng.NextDouble()*120-20),p=>
                {float street=Mathf.Abs(p.x);bool walk=street<5.2f||Mathf.Abs(p.y-26)<3.5f&&p.x>0||Mathf.Abs(p.y-48)<3.5f&&p.x<0||Vector2.Distance(p,new Vector2(0,-20))<9||Vector2.Distance(p,new Vector2(-34,84))<6;
                 return !walk&&TownHeight(p)<3.6f&&street<31;},
                ("DressPlanter",1f,true),("DressWaterBarrels",1.1f,true),("DressFanShrub",1.3f,false),("DressSucculent",1f,false),("DeckCrate",1f,true),("DressCargoPallet",1f,true),("HushwellRubble",.9f,false));
            // The lantern stair: three towers on the east terrace, the keeper below the tallest.
            // Lanternwick never let these go out: "kept the lanterns lit for anyone who came back".
            Relay("Lantern tower south",new Vector3(28,0,40),"",14,0).alwaysLit=true;
            Relay("Lantern tower",new Vector3(31,0,64),"",18,15).alwaysLit=true;
            Relay("Lantern tower north",new Vector3(27,0,88),"",13,-10).alwaysLit=true;
            WorldBuilder.Label("LANTERN STAIR",new Vector3(24,3.2f,33));
            // The archive: a carved facade in the north wall.
            Prop(Kit("AncelaneArch","HushwellColumn"),new Vector3(0,0,104),8,180,false);
            foreach(float x in new[]{-7f,7f})Prop(Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,0,100),5.5f,0);
            // Dark until Ishen has the record back ("the archive's lit again"); the wall of light patterns answers the relays' captain.
            Lantern(Prop(Kit("GreatLantern","CrystalClusterA"),new Vector3(0,0,97),3,0),"ishen.recordRestored",3);
            var wall=ArchiveWall(new Vector3(0,.8f,102.6f));
            var wallLit=new GameObject("The archive wall lights",typeof(FlagGate)).GetComponent<FlagGate>();wallLit.flag="bossdown.HessaKorl";wallLit.targets=new[]{wall};wall.SetActive(false);
            WorldBuilder.Label("ARCHIVE",new Vector3(0,.1f,94));
            // The orchard terrace and the shed someone is hiding in.
            foreach(var (x,z) in new[]{(-25f,50f),(-31f,56f),(-24f,62f),(-30f,70f),(-25f,76f),(-34f,46f)})Prop(Kit("OrchardTree","DressFanShrub"),new Vector3(x,0,z),3.2f,x*z);
            Prop(Kit("CanyonHouse","OutpostHab"),new Vector3(-36,0,64),4.2f,90);
            WorldBuilder.Label("ORCHARD",new Vector3(-26,1.5f,44));
            Gate("Orchard gate — Lantern Gorge",new Vector3(-34,0,84),"LanternGorge","Arrival","met.Aumi","Talk to Keeper Aumi on the lantern stair first","AncelaneColumn",4);
            // Lamps and lamp posts along the walk.
            foreach(var (x,z) in new[]{(-4f,-8f),(4f,-8f),(-5f,30f),(5f,40f),(-6f,55f),(6f,70f),(-5f,85f),(18f,26f)})
            {Prop(Kit("LampPost","DressLightBollard"),new Vector3(x,0,z),1.6f,0);Lamp(new Vector3(x,1.6f,z));}
            // The canyon walls' feet: rock and strata between the homes.
            var townCliffs=new List<Vector3>();
            for(float z=-26;z<104;z+=9)foreach(int side in new[]{-1,1})
            {
                float x=side*(32.5f+1.5f*Mathf.PerlinNoise(z*.1f,side));
                if(Mathf.Abs(z-4)<5||Mathf.Abs(z-20)<5&&side<0||Mathf.Abs(z-46)<5&&side>0||Mathf.Abs(z-74)<5&&side>0||Mathf.Abs(z-32)<5&&side<0||side<0&&z>40&&z<92)continue;
                townCliffs.Add(new Vector3(x,0,z));
            }
            for(float x=-30;x<=30;x+=10)if(Mathf.Abs(x)>9)townCliffs.Add(new Vector3(x,0,106));
            CliffFoot(townCliffs,11);
            // People.
            Person("Aumi",new Vector3(22,0,52),-110,"bossdown.HessaKorl",true,"lanternwick.chartGiven","AumiFinal").postNode="AumiReturn";
            var townPell=Person("Pell",new Vector3(6,0,34),200,"lanternwick.chartGiven");
            var gone=new GameObject("Pell has gone to Tallow",typeof(FlagGate)).GetComponent<FlagGate>();gone.flag="pell.aboard";gone.showWhenSet=false;gone.targets=new[]{townPell.gameObject};
            Person("Ishen",new Vector3(4,0,92),170,"ishen.recordRestored");
            var corra=Person("Corra",new Vector3(-6,0,17),110,"unsworn.rationsDelivered");
            Person("Brekk",new Vector3(-32,0,60),90,"unsworn.rationsDelivered");
            WorldBuilder.Boundary("West town limit",new Vector3(-37,8,40),new Vector3(2,20,170));
            WorldBuilder.Boundary("East town limit",new Vector3(37,8,40),new Vector3(2,20,170));
            WorldBuilder.Boundary("North town limit",new Vector3(0,8,110),new Vector3(90,20,2));
            WorldBuilder.Boundary("South town limit",new Vector3(0,8,-34),new Vector3(90,20,2));
            BakeNavigation("Lanternwick",new Bounds(new Vector3(0,6,40),new Vector3(96,30,160)),new Vector3(0,0,-14),
                new[]{new Vector3(-6,0,17),new Vector3(22,3,50),new Vector3(6,0,34),new Vector3(4,0,90),new Vector3(-30,1.4f,60),new Vector3(-33,1.4f,80),new Vector3(-11,0,28),new Vector3(13,0,15)});
            WorldBuilder.Save(scene,"Lanternwick");Debug.Log("LANTERNWICK_OK");
        }

        /// <summary>The wall of light patterns: passages recorded by lantern, bending side by side, each with the small steady
        /// lights of towns travelling beside it. Lines of relay light on the archive's face; no art to wait for.</summary>
        static GameObject ArchiveWall(Vector3 at)
        {
            var wall=new GameObject("Archive light wall");wall.transform.position=at;
            var beam=CurtainMaterial("Lanternwick");var rng=new System.Random(71);const float Half=6.5f,Tall=6.5f;
            for(int passage=0;passage<8;passage++)
            {
                float rise=(passage+.5f)/8*Tall,sway=.25f+(float)rng.NextDouble()*.45f,phase=(float)rng.NextDouble()*6.28f,bends=.5f+(float)rng.NextDouble()*.5f;
                var line=new GameObject("Passage pattern",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(wall.transform,false);
                const int Points=28;line.useWorldSpace=false;line.positionCount=Points;
                for(int i=0;i<Points;i++){float x=-Half+2*Half*i/(Points-1);line.SetPosition(i,new Vector3(x,rise+Mathf.Sin(x*bends+phase)*sway,0));}
                line.startWidth=line.endWidth=.09f;line.sharedMaterial=beam;line.shadowCastingMode=ShadowCastingMode.Off;
                var tone=passage%3==0?new Color(1.9f,1.3f,.55f,.95f):new Color(1.6f,.95f,.35f,.8f);line.startColor=tone;line.endColor=tone;
                for(int town=0;town<3;town++)
                {
                    float x=-Half+.8f+(float)rng.NextDouble()*(2*Half-1.6f);var center=new Vector3(x,rise+Mathf.Sin(x*bends+phase)*sway+.28f,0);
                    Strand(wall.transform,wall.transform.TransformPoint(center-Vector3.right*.13f),wall.transform.TransformPoint(center+Vector3.right*.13f),beam,.16f,new Color(2,1.7f,1,1),new Color(2,1.7f,1,1));
                }
            }
            var glow=wall.AddComponent<Light>();glow.type=LightType.Point;glow.color=new Color(1,.78f,.45f);glow.range=14;glow.intensity=1.8f;glow.shadows=LightShadows.None;
            return wall;
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
            height=GorgeHeight;CliffTop=2.5f;fadeTall=true;
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
                var fallen=Dark(Prop(Kit("LanternTower","HushwellColumn"),new Vector3(x,0,z),size,yaw,false));fallen.name="Fallen relay tower";
                fallen.transform.rotation=Quaternion.Euler(0,yaw,84);var lying=ModelGeometry.BoundsOf(fallen);
                fallen.transform.position+=new Vector3(x,height(new Vector2(x,z))-.4f,z)-new Vector3(lying.center.x,lying.min.y,lying.center.z);
            }
            foreach(var (x,z) in new[]{(10f,44f),(-10f,88f),(9f,144f),(-9f,166f),(4f,100f)})Prop(Kit("LanternShard","CrystalClusterB"),new Vector3(x,0,z),1.6f,x*13);
            // Cliff feet along both walls, and boulders on the floor's verge (never on the route itself).
            var gorgeCliffs=new List<Vector3>();var verge=new List<Vector3>();
            for(int i=1;i<GorgePath.Length;i++)
            {
                Vector2 a=GorgePath[i-1],d=GorgePath[i]-a;float length=d.magnitude;var normal=new Vector2(-d.y,d.x)/length;
                for(float along=4;along<length;along+=8)
                    foreach(int side in new[]{-1,1})
                    {
                        var at=a+d*(along/length)+normal*side*(14.5f+1.5f*Mathf.PerlinNoise(along,i));
                        if(GorgeDistance(at)<12.5f)continue;
                        gorgeCliffs.Add(new Vector3(at.x,0,at.y));
                        var v=a+d*(along/length)+normal*side*9.5f;if(((int)along/8+i)%3==0&&GorgeDistance(v)>8.5f)verge.Add(new Vector3(v.x,0,v.y));
                    }
            }
            CliffFoot(gorgeCliffs,23);
            foreach(var v in verge)if(Prefab("Environment","DressBoulders")!=null)Prop("DressBoulders",v,1.2f,v.x*31,true);
            var grng=new System.Random(41);
            Scatter(grng,70,()=>new Vector2((float)grng.NextDouble()*60-30,(float)grng.NextDouble()*236-6),p=>{float d=GorgeDistance(p);return d>5.5f&&d<12.5f;},
                ("HushwellRubble",1f,false),("DressSucculent",.9f,false),("DressFanShrub",1.2f,false),("CrystalClusterA",1.1f,false),("LanternShard",1f,false),("DressBoulders",1f,true));
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
            // Inside the first relay's reach: relight it mid-fight, or fall back into its light.
            Fight("Gorge_1",new Vector2(8,60),"Gloomgrazer",4);
            Fight("Gorge_Den",new Vector2(-22,98),"Gloomgrazer",2);
            Fight("Gorge_Camp",new Vector2(-6,114),"MusterScav",3,"MusterSergeant",1);
            Fight("Gorge_3",new Vector2(2,156),"Gloomgrazer",3,"MusterScav",2);
            Fight("Gorge_4",new Vector2(6,204),"MusterScav",3,"MusterSergeant",1);
            // The scavengers' camp around the second relay.
            Prop(Kit("MusterLeanTo","DressCargoPallet"),new Vector3(-14,0,112),3.2f,70);
            Prop(Kit("MusterCutterRig","DressWorkLamp"),new Vector3(-6,0,128),2.2f,-30);
            foreach(var (x,z) in new[]{(-12f,106f),(-11f,108f),(-3f,116f)})Prop(Kit("MusterCrate","DeckCrate"),new Vector3(x,0,z),1.1f,x*7);
            Lamp(new Vector3(-10,1.2f,110),10,1.8f);
            var rations=Discovery("Muster ration crate",new Vector3(-12,0,104),"Take a Muster ration crate","unsworn.rationsFound","MusterRations",null,"Sela","UnswornRations",0);
            rations.visual=Prop(Kit("MusterCrate","DeckCrate"),new Vector3(-12.6f,0,103.4f),.9f,25,false);rations.visual.SetActive(false);
            // The grazer den under the fallen tower holds Pell's lamp.
            var lamp=Discovery("Pell's lamp",Den.x*Vector3.right+Den.y*Vector3.forward,"Pick up the chewed lamp","pell.lampFound","PellsLamp",null,"Sela","PellsLamp",0);
            lamp.visual=Prop(Kit("LampPost","DressLightBollard"),new Vector3(Den.x+.8f,0,Den.y+.5f),1f,80,false);lamp.visual.SetActive(false);
            WorldBuilder.Label("SCAVENGER CAMP",new Vector3(-8,.2f,100));
            // The Sunken Relay's door at the head of the gorge opens once the three relays burn.
            WorldBuilder.Spawn("Relay",new Vector3(0,0,220));
            Prop(Kit("AncelaneArch","HushwellColumn"),new Vector3(0,0,233),8,0,false);
            Gate("Enter the Sunken Relay",new Vector3(0,0,229),"SunkenRelay","Arrival","gorge.relaysLit","The relay door is dark: relight the gorge relays","AncelaneColumn",3.5f);
            // The door is dark until the relays burn: it says so without a word.
            var doorLight=new GameObject("Relay door lamp",typeof(Light));doorLight.transform.position=new Vector3(0,height(new Vector2(0,229))+3.5f,227);
            var doorGlow=doorLight.GetComponent<Light>();doorGlow.type=LightType.Point;doorGlow.color=new Color(1,.78f,.45f);doorGlow.range=14;doorGlow.intensity=3;doorGlow.shadows=LightShadows.None;
            var doorLit=new GameObject("The relay door lights",typeof(FlagGate)).GetComponent<FlagGate>();doorLit.flag="gorge.relaysLit";doorLit.targets=new[]{doorLight};doorLight.SetActive(false);
            var anvil=Prop("LatticeAnvil",new Vector3(-6,0,212),1.8f,40).AddComponent<Bench>();anvil.prompt="Fabricate — Lattice Anvil";
            WorldBuilder.Label("SUNKEN RELAY",new Vector3(0,.2f,224));
            WorldBuilder.Boundary("South gorge limit",new Vector3(0,8,-16),new Vector3(120,20,2));
            WorldBuilder.Boundary("North gorge limit",new Vector3(0,8,238),new Vector3(120,20,2));
            BakeNavigation("LanternGorge",new Bounds(new Vector3(0,8,110),new Vector3(110,40,260)),new Vector3(0,0,0),
                GorgePath.Select(p=>new Vector3(p.x,0,p.y)).Concat(new[]{new Vector3(Den.x+2,0,Den.y-1),new Vector3(-11,0,103)}));
            WorldBuilder.Save(scene,"LanternGorge");Debug.Log("LANTERN_GORGE_OK");
        }

        // ---------------------------------------------------------------- the Sunken Relay (dungeon)
        /// <summary>The relay's halls, carved out of the canyon's rock: (centre, size) of each chamber along +z.</summary>
        static readonly (Vector2 centre,Vector2 size,string label)[] RelayHalls=
        {
            // The entry hall runs well south of the door, so the fixed camera opens on the room and not on a wall behind the hero.
            (new(0,6),new(18,28),"ENTRY HALL"),(new(0,32),new(8,24),"CUT CORRIDOR"),(new(0,64),new(30,40),"FALLEN GALLERY"),
            (new(0,100),new(22,32),"LOWER VAULT"),(new(0,140),new(38,48),"GREAT LANTERN"),
        };
        static float RelayHeight(Vector2 p)
        {
            float inside=float.PositiveInfinity;bool cameraSide=false;
            foreach(var (centre,size,_) in RelayHalls)
            {
                var d=new Vector2(Mathf.Abs(p.x-centre.x)-size.x*.5f,Mathf.Abs(p.y-centre.y)-size.y*.5f);
                // Rounded rectangles: the outside distance, softened at the corners.
                float box=Mathf.Max(d.x,d.y)<0?Mathf.Max(d.x,d.y):new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude;
                if(box<inside){inside=box;cameraSide=p.y<centre.y-size.y*.5f;}
            }
            // Rock between a hall and the camera stays low, or the camera slides in to a close-up of the hero's knees.
            float rock=Smooth(-.2f,1.3f,inside)*(5.2f+1.4f*Mathf.PerlinNoise(p.x*.08f+4,p.y*.08f))*(cameraSide?.45f:1);
            return rock+.06f*Mathf.PerlinNoise(p.x*.3f,p.y*.3f);
        }
        static void Relay()
        {
            height=RelayHeight;CliffTop=2.4f;fadeTall=true;
            var scene=WorldBuilder.Begin("SunkenRelay");
            Terrain("SunkenRelay","Relay halls",-36,36,-14,180,.5f,Canyon(false));
            WorldBuilder.Spawn("Arrival",new Vector3(0,0,3));
            foreach(var (centre,size,label) in RelayHalls)WorldBuilder.Label(label,new Vector3(centre.x,.1f,Mathf.Max(8,centre.y-size.y*.5f+2.5f)),1.6f);
            Gate("Climb out — Lantern Gorge",new Vector3(0,0,1),"LanternGorge","Relay",null,null,"AncelaneColumn",3);
            Discovery("Cut lantern housings",new Vector3(0,0,14),"Look at the cut lantern housings","relay.entered","","SelaSunkenRelay","Taren");
            Prop(Kit("MusterCutterRig","DressWorkLamp"),new Vector3(4.5f,0,15.5f),1.8f,150);
            foreach(var (x,z) in new[]{(-6f,18f),(6f,18f),(-12f,50f),(12f,50f),(-12f,78f),(12f,78f),(-8f,90f),(8f,90f)})
            {Prop(Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,0,z),4.2f,x*z);}
            foreach(var (x,z) in new[]{(-6f,6f),(6f,26f),(-10f,60f),(10f,68f),(0f,96f),(-8f,128f),(8f,128f)})Lamp(new Vector3(x,2.6f,z),11,1.7f);
            var rrng=new System.Random(53);
            Scatter(rrng,46,()=>new Vector2((float)rrng.NextDouble()*36-18,(float)rrng.NextDouble()*160),p=>RelayHeight(p)<.2f&&Mathf.Abs(p.x)>3.2f&&RelayHeight(p+Vector2.right*2.5f)+RelayHeight(p-Vector2.right*2.5f)>.5f,
                ("HushwellRubble",1.1f,false),("LanternShard",1.1f,false),("CrystalClusterB",1f,false),("DressBoulders",1.1f,true),("HushwellStalagmites",1.6f,true));
            foreach(var (x,z,yaw) in new[]{(-11f,60f,80f),(11f,92f,-20f),(-15f,140f,30f)})
            {var fallen=Dark(Prop(Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,0,z),4f,yaw,false));fallen.transform.rotation=Quaternion.Euler(0,yaw,86);var b=ModelGeometry.BoundsOf(fallen);fallen.transform.position+=new Vector3(x,-.2f,z)-new Vector3(b.center.x,b.min.y,b.center.z);}
            // Record plates for Ishen.
            var plates=new[]{new Vector3(-7,0,16),new Vector3(12,0,76),new Vector3(-9,0,108)};
            for(int i=0;i<plates.Length;i++)
            {
                var plate=Discovery("Record plate "+(i+1),plates[i],"Take the record plate","relay.plate"+(i+1),"RecordPlate",null,"Sela","IshensRecord",0);
                plate.visual=RecordPlate(plates[i]+new Vector3(.9f,0,.6f),i*70);plate.visual.SetActive(false);
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
            WorldBuilder.Label("RELAY CRADLE",new Vector3(8,.1f,106.5f),1.6f);
            var anvil=Prop("LatticeAnvil",new Vector3(-8,0,112),1.8f,90).AddComponent<Bench>();anvil.prompt="Fabricate — Lattice Anvil";
            // The Great Lantern chamber: four columns survive every phase as cover against the flash.
            Prop(Kit("GreatLantern","CrystalClusterA"),new Vector3(0,0,158),5,0);
            foreach(var (x,z) in new[]{(-10f,132f),(10f,132f),(-10f,150f),(10f,150f)})Prop(Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,0,z),4.5f,x+z);
            Lamp(new Vector3(0,4,150),18,2.2f);
            // The fixed camera sees about 7 m ahead of the hero: the talk starts just past the seal, with her in front of it, however
            // wide the party comes in. The trigger spans the whole hall; the talk is the checkpoint.
            var confront=Discovery("Confront the scavenger captain",new Vector3(0,0,124),"Confront the scavenger captain","relay.hessaConfronted","","HessaConfront","Hessa",null,0,2);
            confront.saveSpawn="Vault";confront.startsOnCrossing=true;
            var mouth=confront.gameObject.AddComponent<BoxCollider>();mouth.isTrigger=true;mouth.size=new Vector3(40,5,3);mouth.center=Vector3.up*1.5f;
            // The seal shuts the whole mouth, in relay light: strands across a solid, invisible door.
            var entry=WorldBuilder.Membrane("Great Lantern seal",new Vector3(0,1.5f,116.5f),23,true,6);
            var door=entry.GetComponent<BoxCollider>();
            foreach(var filter in entry.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(filter);
            foreach(var view in entry.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(view);
            var sealObstacle=entry.gameObject.AddComponent<NavMeshObstacle>();sealObstacle.shape=NavMeshObstacleShape.Box;sealObstacle.center=door.center;sealObstacle.size=door.size;sealObstacle.carving=true;sealObstacle.carveOnlyStationary=false;sealObstacle.enabled=false;
            var sealBeam=CurtainMaterial("SunkenRelay");
            for(float x=-11;x<=11;x+=1.1f)Strand(entry.transform,new Vector3(x,0,116.5f),new Vector3(x,4.4f,116.5f),sealBeam,.2f,new Color(1.6f,.85f,.3f,.2f),new Color(1.8f,1f,.4f,.9f));
            foreach(var strand in entry.GetComponentsInChildren<LineRenderer>())strand.enabled=false;
            var duel=WorldBuilder.Encounter("Relay_Hessa",new Vector3(0,1,138),new Vector3(38,5,28),"HessaKorl",1,new[]{entry});
            // She is in the room for her own confrontation, at her work, and turns when the fight starts.
            var post=duel.GetComponentInChildren<Spawner>();post.transform.position=new Vector3(3,0,129);post.transform.rotation=Quaternion.Euler(0,180,0);post.faceAsPlaced=true;
            duel.requiredFlag="relay.hessaConfronted";duel.checkpointSpawn="Vault";
            var viewpoint=new GameObject("Hessa preview");viewpoint.transform.position=new Vector3(0,0,124);duel.previewPoint=viewpoint.transform;duel.previewRadius=6;
            confront.previewEncounter=duel;
            // After the duel: the captain under guard, and the lift home.
            var hessa=Person("Hessa",new Vector3(9,0,154),-120,"",false);hessa.firstNode="HessaRepeat";hessa.repeatNode="HessaRepeat";
            var yielded=new GameObject("Hessa under guard",typeof(YieldedCaptain)).GetComponent<YieldedCaptain>();yielded.npc=hessa;hessa.gameObject.SetActive(false);
            Gate("Ride the relay lift — Lanternwick",new Vector3(-6,0,160),"Lanternwick","Archive","relay.hessaYielded","The lift waits: hear the captain out first","AncelaneColumn",3);
            BakeNavigation("SunkenRelay",new Bounds(new Vector3(0,4,82),new Vector3(50,20,186)),new Vector3(0,0,3),
                new[]{new Vector3(0,0,30),new Vector3(10,0,74),new Vector3(-8,0,108),new Vector3(0,0,118),new Vector3(0,0,140)});
            WorldBuilder.Save(scene,"SunkenRelay");Debug.Log("SUNKEN_RELAY_OK");
        }

        /// <summary>A record plate: a round relay mirror on the floor, with its own small light, so it never reads as the rubble.</summary>
        static GameObject RecordPlate(Vector3 at,float yaw)
        {
            var plate=Prop(Kit("RelayMirror","LanternShard"),at,.9f,yaw,false);plate.name="Record plate body";
            var light=new GameObject("Plate light",typeof(Light));light.transform.SetParent(plate.transform,false);light.transform.position=plate.transform.position+Vector3.up*1.2f;
            var l=light.GetComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.8f,.5f);l.range=5;l.intensity=2;l.shadows=LightShadows.None;
            return plate;
        }

        // ---------------------------------------------------------------- Tallow approach: the warp out
        static void StageTallowApproach()
        {
            var approach=EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowApproach.unity");
            foreach(var old in UnityEngine.Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Where(w=>w.scene=="WickReach").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(var old in UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Where(s=>s.id=="Wick").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            ChapterOneStaging.ClearSigns("Warp — The Wick Reach");
            var warp=WorldBuilder.Warp("Warp — The Wick Reach",new Vector3(24,1,-18),"WickReach","lantern.routeKnown","Arrival");
            warp.lockedPrompt="No course plotted. Keeper Abri holds Tallow's old routes.";
            WorldBuilder.Spawn("Wick",new Vector3(17,1,-21));
            ChapterOneStaging.Approach();
            EditorSceneManager.MarkSceneDirty(approach);EditorSceneManager.SaveScene(approach);Debug.Log("TALLOW_APPROACH_WARP_OK");
        }
    }
}
