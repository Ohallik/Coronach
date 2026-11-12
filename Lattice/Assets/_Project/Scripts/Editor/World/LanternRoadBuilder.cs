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
            foreach(var node in new[]{"OrrinFirst","AbriLanternTale","AumiFirst","AumiReturn","SelaRelayLit","SelaWickRelays","HessaConfront","HessaYields","HessaYieldsLate","HessaGuarded","SurvivorLate","SurvivorFirstParcel","SurvivorLateParcel","BrekkRations","IshenRecordReturn","PellLampOffer","CorraRationsOffer"})
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
                copy.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));copy.SetTextureScale("_BaseMap",Vector2.one);
                // A wall takes vertex colour too, so the buried relay's wall tops can stay dark. With no colours on a mesh it changes nothing.
                if(material=="canyon-cliff"){copy.EnableKeyword("_VCOLOR_ON");copy.SetFloat("_VColor",1);}
                // Sorrel's ground projects from the side now (ChapterOneStaging). These floors have their walls in another mesh, so that's two more samples a pixel for nothing.
                copy.DisableKeyword("_TRIPLANAR_ON");copy.SetFloat("_Triplanar",0);
                EditorUtility.SetDirty(copy);
            }
            AssetDatabase.SaveAssets();
        }
        /// <summary>The Wick Reach isn't Tallow's space: the same panorama turned away and warmed, so the warp arrives somewhere new.</summary>
        static Material WickSky(Material source)
        {
            if(source==null)return null;
            const string path="Assets/_Project/Resources/WorldMaterials/wick-sky.mat";
            var sky=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(sky==null){sky=new Material(source){name="wick-sky"};AssetDatabase.CreateAsset(sky,path);}else sky.CopyPropertiesFromMaterial(source);
            // The same panorama on a shader that can take a turn. From a camera that never rotates a sky is painted on the screen: the same mote in the same place 100 m apart.
            var drifting=Shader.Find("Lattice/DriftingSky");if(drifting!=null)sky.shader=drifting;
            sky.SetColor("_Tint",new Color(.62f,.47f,.36f,.5f));sky.SetFloat("_Rotation",137);sky.SetFloat("_Exposure",.85f);EditorUtility.SetDirty(sky);return sky;
        }
        static void Define()
        {
            CanyonMaterials();
            var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");
            var dusk=ArenaBuilder.Asset<Hd2dProfile>("Dusk",p=>{p.tint=new Color(1,.9f,.82f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.5f;});
            var cold=Find<Hd2dProfile>("Cold");var cave=Find<Hd2dProfile>("Cave")??cold;
            var sky=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/tallow-sky.mat");var wickSky=WickSky(sky);
            ArenaBuilder.Asset<ZoneDef>("WickReach",z=>{z.id=z.scene="WickReach";z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=cold;z.skybox=wickSky;z.enemyThreat=1.5f;z.flightPlane=1;z.cameraClearsScenery=false;});
            ArenaBuilder.Asset<ZoneDef>("Lanternwick",z=>{z.id=z.scene="Lanternwick";z.kind=ZoneKind.GroundSafe;z.cameraProfile=ground;z.hd2dProfile=dusk;z.skybox=sky;z.enemyThreat=1;z.cameraClearsScenery=true;});
            ArenaBuilder.Asset<ZoneDef>("LanternGorge",z=>{z.id=z.scene="LanternGorge";z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=dusk;z.skybox=sky;z.enemyThreat=1.6f;z.cameraClearsScenery=true;});
            ArenaBuilder.Asset<ZoneDef>("SunkenRelay",z=>{z.id=z.scene="SunkenRelay";z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=cave;z.skybox=null;z.enemyThreat=1.7f;z.cameraClearsScenery=true;});

            var bite=Find<AttackDef>("Bite");var volley=Find<AttackDef>("Volley");
            LootTable Loot(string id,params LootEntry[] entries)=>ArenaBuilder.Asset<LootTable>(id,t=>t.entries=entries);
            ArenaBuilder.Asset<EnemyDef>("Gloomgrazer",e=>{e.id="Gloomgrazer";e.prefab=Body("Enemies","Gloomgrazer","Enemies","Ridgehound");e.integrity=220;e.breakThreshold=90;e.speed=3.7f;e.attacks=new[]{bite};
                e.weakness=DamageType.Beam;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.PackHunter;e.xp=40;e.boss=false;e.lootTable=Loot("Gloomgrazer",new LootEntry{id="LatticeFilament",count=1,chance=.3f});});
            ArenaBuilder.Asset<EnemyDef>("MusterScav",e=>{e.id="MusterScav";e.prefab=Body("Enemies","MusterScav","Enemies","SentinelHusk");e.integrity=210;e.breakThreshold=90;e.speed=2.7f;e.attacks=new[]{volley};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Spitter;e.xp=45;e.boss=false;e.lootTable=Loot("MusterScav",new LootEntry{id="ScrapAlloy",count=2,chance=.5f},new LootEntry{id="ChargeCell",count=1,chance=.08f});});
            ArenaBuilder.Asset<EnemyDef>("MusterSergeant",e=>{e.id="MusterSergeant";e.prefab=Body("Enemies","MusterSergeant","Enemies","SentinelHusk");e.integrity=560;e.breakThreshold=170;e.speed=2.2f;e.attacks=new[]{bite};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Sentinel;e.xp=95;e.boss=false;e.lootTable=Loot("MusterSergeant",new LootEntry{id="ScrapAlloy",count=4,chance=1},new LootEntry{id="RepairGel",count=1,chance=.35f});});
            ArenaBuilder.Asset<EnemyDef>("MusterSkiff",e=>{e.id="MusterSkiff";e.prefab=Body("Enemies","MusterSkiff","Enemies","ChoristerDrifter");e.integrity=170;e.breakThreshold=80;e.speed=4.2f;e.attacks=new[]{volley};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Spitter;e.xp=40;e.boss=false;e.lootTable=Loot("MusterSkiff",new LootEntry{id="ScrapAlloy",count=2,chance=.45f});});
            ArenaBuilder.Asset<EnemyDef>("HessaKorl",e=>{e.id="HessaKorl";e.prefab=Body("Enemies","HessaKorl","Enemies","SentinelHusk");e.integrity=48000;e.breakThreshold=700;e.speed=3.4f;e.attacks=new[]{bite};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.Sentinel;e.xp=420;e.boss=true;e.yieldsOnDefeat=true;e.lootTable=Loot("HessaKorl",new LootEntry{id="ChoirResin",count=3,chance=1},new LootEntry{id="RepairGel",count=2,chance=1});});

            ArenaBuilder.Asset<QuestDef>("LanternRoad",q=>{q.id="LanternRoad";q.title="The Lantern Road";
                q.description="Venn wants a route that waits for a moving town. Lanternwick's lanterns once guided stations like Tallow.";
                q.steps=new[]{Step(ObjectiveKind.Reach,"WickReach"),Step(ObjectiveKind.Reach,"Lanternwick"),Step(ObjectiveKind.TalkTo,"Aumi"),
                    Step(ObjectiveKind.Interact,"GorgeRelay",3),Step(ObjectiveKind.Kill,"HessaKorl"),Step(ObjectiveKind.Flag,"lanternwick.chartGiven")};
                q.stepText=new[]{"Take Tallow's old lantern route through the Wick Reach","Land at Lanternwick's courier dock","Find Lanternwick's lantern-keeper",
                    "Relight the three relays in Lantern Gorge","Stop the scavengers stripping the Sunken Relay","Bring word back to Keeper Aumi"};
                q.rewardXp=700;q.rewardScrip=400;q.rewards=new[]{new Ingredient{id="ModulesT2",count=1},new Ingredient{id="RepairGel",count=3}};q.flagsOnComplete=new[]{"chapter2.complete"};});
            ArenaBuilder.Asset<QuestDef>("PellsLamp",q=>{q.id="PellsLamp";q.title="Light for a Lamp-Keeper";
                q.description="Pell lost her mother's lamp to a grazer in Lantern Gorge and hasn't told Aumi.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"PellsLamp"),Step(ObjectiveKind.Flag,"pell.lampReturned")};
                q.stepText=new[]{"Search the grazer den under the fallen tower, west of the path before the second relay","Bring the lamp back to Pell"};
                q.rewardXp=180;q.rewardScrip=120;q.rewards=new[]{new Ingredient{id="ChargeCell",count=2}};q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("IshensRecord",q=>{q.id="IshensRecord";q.title="Ishen's Missing Record";
                q.description="Three record plates never came up from the Sunken Relay below Lantern Gorge.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"RecordPlate",3),Step(ObjectiveKind.Flag,"ishen.recordRestored")};
                q.stepText=new[]{"Recover the record plates from the lit mirror stands in the Sunken Relay","Bring the plates to Ishen at the archive"};
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
            Person("Brekk","Brekk",false,"BrekkBody","Characters","Orrin");
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
        /// <summary>A dead relay: its crystal unlit, so relit relays stand out. The component does it as the scene loads: a property block set here isn't saved with the scene.</summary>
        static GameObject Dark(GameObject go,string kit=null){var dark=go.AddComponent<StaysDark>();if(kit!=null)dark.glass=DeadGlass(kit);return go;}
        /// <summary>Unlit, a tower's glass still wore its painted amber in full sun, so a dead relay looked softly lit and the one you could relight didn't stand out. This is the
        /// model's own texture with whatever its emission mask lights turned to the unlit smoky crystal of the kit's design board. Made once from the staged art; a lit relay goes back to the original.</summary>
        internal static Texture2D DeadGlass(string kit)
        {
            string folder="Assets/_Project/Art/Generated/Models/"+kit+"/",target="Assets/_Project/Art/Generated/Lantern/"+kit+"_dead.png";
            var made=AssetDatabase.LoadAssetAtPath<Texture2D>(target);if(made!=null)return made;
            string albedoFile=folder+kit+"_base_color.png",maskFile=folder+kit+"_emission.png";
            if(!File.Exists(albedoFile)||!File.Exists(maskFile))return null;
            var albedo=new Texture2D(2,2,TextureFormat.RGB24,false);albedo.LoadImage(File.ReadAllBytes(albedoFile));
            var mask=new Texture2D(2,2,TextureFormat.RGB24,false);mask.LoadImage(File.ReadAllBytes(maskFile));
            var pixels=albedo.GetPixels();int width=albedo.width,tall=albedo.height;
            for(int y=0;y<tall;y++)for(int x=0;x<width;x++)
            {
                float lit=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.6f,mask.GetPixelBilinear((x+.5f)/width,(y+.5f)/tall).grayscale));if(lit<=0)continue;
                var own=pixels[y*width+x];pixels[y*width+x]=Color.Lerp(own,new Color(.31f,.37f,.48f)*Mathf.Lerp(.7f,1.2f,own.grayscale),lit);
            }
            albedo.SetPixels(pixels);File.WriteAllBytes(target,albedo.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(albedo);UnityEngine.Object.DestroyImmediate(mask);
            AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(target);
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
            if(go.GetComponent<SceneryOcclusion>()==null)MeasureShape(go.AddComponent<SceneryOcclusion>(),go);
        }
        static Mesh SaveMesh(Mesh mesh,string path)
        {
            var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(prior==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,prior);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(prior);return prior;
        }
        /// <summary>The toon ramp bands the sun, and a floor lit near a step of the ramp changes band on the slightest bump: Lanternwick's low sun meets level ground at .53 against a
        /// step at .5, so a rise two degrees off the sun dropped a whole band and left a hard, stair-stepped dark shape on the floor with nothing casting it. Ground this gentle is lit as level.</summary>
        const float LevelFloorY=.97f;
        static void LevelFloor(Mesh floor){var normals=floor.normals;for(int i=0;i<normals.Length;i++)if(normals[i].y>=LevelFloorY)normals[i]=Vector3.up;floor.normals=normals;}
        const float FootDark=.62f,FootReach=2.6f;
        /// <summary>Floor and strata meet along a line of single triangles, each chosen by how steep it is, and at full contrast that saw edge was the hardest thing on the screen. Both darken
        /// toward it, as ground does where a wall rises out of it, so the line sits in shade. Worked out per point of the grid, so the two meshes agree exactly where they meet.</summary>
        static float[] FootShading(Vector3[] grid,int nx,int nz,float cell)
        {
            var shade=new float[grid.Length];int reach=Mathf.Max(1,Mathf.RoundToInt(FootReach/cell));
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                float y=grid[z*nx+x].y,low=y,high=y;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                {
                    float other=grid[Mathf.Clamp(z+dz*reach,0,nz-1)*nx+Mathf.Clamp(x+dx*reach,0,nx-1)].y;
                    low=Mathf.Min(low,other);high=Mathf.Max(high,other);
                }
                shade[z*nx+x]=Mathf.Lerp(1,FootDark,Mathf.InverseLerp(.5f,2.4f,high-y)*(1-Mathf.InverseLerp(.2f,2.4f,y-low)));
            }
            return shade;
        }
        static Color Shaded(Color tone,float by)=>new(tone.r*by,tone.g*by,tone.b*by,tone.a);
        /// <summary>A heightfield landform in three parts: an invisible full-resolution collider (walls stop bodies and
        /// navigation reads it), the walkable floor drawn with the ground texture, and the cliff faces drawn with the
        /// strata texture mapped along each wall so its bands stay horizontal. A tone colours the floor's vertices, and a wall top darkens the walls toward their tops from a wall foot, white unless it's given.</summary>
        static GameObject Terrain(string zone,string label,float xMin,float xMax,float zMin,float zMax,float cell,Material material,Func<Vector2,Color> floorTone=null,Color? wallTop=null,Color? wallFoot=null)
        {
            int nx=Mathf.CeilToInt((xMax-xMin)/cell)+1,nz=Mathf.CeilToInt((zMax-zMin)/cell)+1;
            var vertices=new Vector3[nx*nz];var uv=new Vector2[vertices.Length];var triangles=new int[(nx-1)*(nz-1)*6];int ti=0;
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                int i=z*nx+x;var p=new Vector2(xMin+x*cell,zMin+z*cell);vertices[i]=new Vector3(p.x,height(p),p.y);uv[i]=p/8;
            }
            // Each cell's diagonal runs between its two nearest heights, along the ground's contour. With one fixed diagonal, a wall running across it broke at its foot into single triangles, each floor or wall by how steep it came out: hard teeth along the floor's edge.
            for(int z=0;z<nz-1;z++)for(int x=0;x<nx-1;x++)
            {
                int i=z*nx+x;
                if(Mathf.Abs(vertices[i].y-vertices[i+nx+1].y)<Mathf.Abs(vertices[i+1].y-vertices[i+nx].y)){triangles[ti++]=i;triangles[ti++]=i+nx;triangles[ti++]=i+nx+1;triangles[ti++]=i;triangles[ti++]=i+nx+1;triangles[ti++]=i+1;}
                else{triangles[ti++]=i;triangles[ti++]=i+nx;triangles[ti++]=i+1;triangles[ti++]=i+1;triangles[ti++]=i+nx;triangles[ti++]=i+nx+1;}
            }
            var full=new Mesh{name=label+" collision",indexFormat=IndexFormat.UInt32};full.vertices=vertices;full.triangles=triangles;full.RecalculateNormals();full.RecalculateBounds();
            var floor=new List<int>();var cliffVerts=new List<Vector3>();var cliffUv=new List<Vector2>();var cliffTris=new List<int>();var cliffTone=new List<Color>();
            var foot=FootShading(vertices,nx,nz,cell);
            for(int t=0;t<triangles.Length;t+=3)
            {
                Vector3 a=vertices[triangles[t]],b=vertices[triangles[t+1]],c=vertices[triangles[t+2]];
                var n=Vector3.Cross(b-a,c-a).normalized;float h=(a.y+b.y+c.y)/3;
                if(Mathf.Abs(n.y)>=.78f&&h<=CliffTop){floor.Add(triangles[t]);floor.Add(triangles[t+1]);floor.Add(triangles[t+2]);continue;}
                bool facesX=Mathf.Abs(n.x)>Mathf.Abs(n.z);
                for(int k=0;k<3;k++)
                {
                    var v=vertices[triangles[t+k]];
                    cliffTris.Add(cliffVerts.Count);cliffVerts.Add(v);
                    cliffUv.Add(Mathf.Abs(n.y)>=.78f?new Vector2(v.x,v.z)/8:new Vector2((facesX?v.z:v.x)/7,v.y/5));
                    var wall=wallTop is Color top?Color.Lerp(wallFoot??Color.white,top,Mathf.Abs(n.y)>=.78f?1:Mathf.InverseLerp(.5f,5,v.y)):Color.white;
                    cliffTone.Add(Shaded(wall,foot[triangles[t+k]]));
                }
            }
            var ground=new Mesh{name=label,indexFormat=IndexFormat.UInt32};ground.vertices=vertices;ground.uv=uv;ground.triangles=floor.ToArray();ground.RecalculateNormals();ground.RecalculateBounds();
            LevelFloor(ground);
            var tones=new Color[vertices.Length];for(int v=0;v<tones.Length;v++)tones[v]=Shaded(floorTone!=null?floorTone(new Vector2(vertices[v].x,vertices[v].z)):Color.white,foot[v]);ground.colors=tones;
            var cliffs=new Mesh{name=label+" cliffs",indexFormat=IndexFormat.UInt32};cliffs.SetVertices(cliffVerts);cliffs.SetUVs(0,cliffUv);cliffs.SetTriangles(cliffTris,0);cliffs.SetColors(cliffTone);cliffs.RecalculateNormals();cliffs.RecalculateBounds();
            full=SaveMesh(full,"Assets/_Project/Scenes/"+zone+"-Terrain.asset");
            ground=SaveMesh(ground,"Assets/_Project/Scenes/"+zone+"-Ground.asset");
            cliffs=SaveMesh(cliffs,"Assets/_Project/Scenes/"+zone+"-Cliffs.asset");
            // The collision body keeps an enabled renderer with nothing to draw: the ground camera treats only rendered colliders as scenery to look past, as it does
            // Sorrel's basin. It used to carry the full mesh with forceRenderingOff, but that flag isn't saved with a scene, so the whole landform drew twice at one depth,
            // flat wherever its missing uvs won the draw: the floor's stone came and went with the order of a zone load.
            var terrain=new GameObject(label,typeof(MeshRenderer),typeof(MeshCollider));terrain.isStatic=true;
            terrain.GetComponent<MeshCollider>().sharedMesh=full;terrain.AddComponent<AudioSurface>();
            // No mesh, so its material never draws: it is only here so nothing reads this renderer as missing one.
            var hidden=terrain.GetComponent<MeshRenderer>();hidden.sharedMaterial=material;hidden.shadowCastingMode=ShadowCastingMode.Off;
            // One big renderer each, so there's nothing to batch. Marked static, they went into the same combined mesh as whatever else shared their material.
            var floorView=new GameObject(label+" floor",typeof(MeshFilter),typeof(MeshRenderer));floorView.transform.SetParent(terrain.transform,false);
            floorView.GetComponent<MeshFilter>().sharedMesh=ground;floorView.GetComponent<Renderer>().sharedMaterial=material;
            var cliffView=new GameObject(label+" cliffs",typeof(MeshFilter),typeof(MeshRenderer));cliffView.transform.SetParent(terrain.transform,false);
            cliffView.GetComponent<MeshFilter>().sharedMesh=cliffs;cliffView.GetComponent<Renderer>().sharedMaterial=Canyon(true);
            // From their lit side only, the default. Two-sided, a wall with no lit side on the mesh (the gorge's south end, whose lit side is past z -24) laid a shadow over the whole arrival, and every wall triangle having its own vertices, the shadow pass's normal bias opened that shadow into straight rows of pale dashes.
            // So the landform's shadow comes from one welded caster, the collision heightfield itself, whose shared vertices give the bias nothing to open (E3-lanternwick-08); the drawn floor and walls only receive.
            floorView.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;cliffView.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            var caster=new GameObject(label+" shadow",typeof(MeshFilter),typeof(MeshRenderer));caster.transform.SetParent(terrain.transform,false);
            caster.GetComponent<MeshFilter>().sharedMesh=full;var casts=caster.GetComponent<MeshRenderer>();casts.sharedMaterial=Canyon(true);casts.shadowCastingMode=ShadowCastingMode.ShadowsOnly;casts.receiveShadows=false;
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
            return Lantern(tower,flag,size,"LanternTower");
        }
        static RelayLantern Lantern(GameObject body,string flag,float size,string kit)
        {
            var lantern=body.AddComponent<RelayLantern>();lantern.flag=flag;lantern.radius=10;lantern.deadGlass=DeadGlass(kit);
            lantern.crystals=body.GetComponentsInChildren<Renderer>();
            var light=new GameObject("Relay light",typeof(Light));light.transform.SetParent(body.transform,false);
            light.transform.position=body.transform.position+Vector3.up*size*.85f;var l=light.GetComponent<Light>();
            l.type=LightType.Point;l.color=new Color(1,.78f,.45f);l.range=18;l.intensity=2.4f;l.shadows=LightShadows.None;l.enabled=false;lantern.glow=l;
            return lantern;
        }
        /// <summary>Strands of relay light: the Wick's lantern curtain and the door the relay's captain seals, never Choir tissue. They keep the flare's own soft texture and add to what's
        /// behind them, so a strand is a glow with a bright core and no edge (a white texture on flat alpha made a bar chart of solid yellow rectangles). fromFloor draws curtain_soft: a bell across
        /// each strand that comes up from nothing over its first eighth, so it has no straight end on the floor, and the strand's colour fades it to nothing at the top. That rise is in the texture
        /// and not in a colour key because a line only takes colour at its points: a middle key on a two-point strand changes nothing. The archive's patterns keep the whole flare.</summary>
        static Material CurtainMaterial(string zone,bool fromFloor=true,string asset="Curtain")
        {
            var beam=new Material(Resources.Load<Material>("Effects/flare_01"));
            beam.SetFloat("_Blend",2);beam.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);beam.SetFloat("_DstBlend",(float)BlendMode.One);beam.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);beam.SetFloat("_DstBlendAlpha",(float)BlendMode.One);
            beam.SetColor("_BaseColor",fromFloor?new Color(1.3f,1.05f,.85f,1):new Color(1.8f,1.35f,.9f,1));
            // A soft bell across each strand and nothing along it, and the strand's colour fades it upward. On the flare, a thin streak, they drew a row of sparks: the particle shader ignores tiling, so no window on it could do this.
            if(fromFloor)beam.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Particles/curtain_soft.png"));
            AssetDatabase.CreateAsset(beam,"Assets/_Project/Scenes/"+zone+"-"+asset+".mat");return beam;
        }
        static void Strand(Transform parent,Vector3 from,Vector3 to,Material beam,float width,Color bottom,Color top)
        {
            var line=new GameObject("Curtain strand",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(parent,true);
            line.useWorldSpace=false;line.positionCount=2;line.SetPosition(0,line.transform.InverseTransformPoint(from));line.SetPosition(1,line.transform.InverseTransformPoint(to));
            line.startWidth=line.endWidth=width;line.sharedMaterial=beam;line.startColor=bottom;line.endColor=top;line.shadowCastingMode=ShadowCastingMode.Off;
        }
        /// <summary>A way out, staged as what its words say. Its sign stands low in front of it: raised over a tall prop it ran off the top of the screen or under the status panel.
        /// A prompt measures to the middle of the body, so a tall one needs a longer reach from the floor.</summary>
        static WarpBeacon Gate(string label,Vector3 at,string scene,string spawn,string flag,string locked,string key="WarpBeacon",float size=3,float range=3.6f,Vector2? signAt=null)
        {
            var prop=Prop(Kit(key,"WarpBeacon"),at,size);prop.name=label;
            var warp=prop.AddComponent<WarpBeacon>();warp.prompt=label;warp.range=range;warp.scene=scene;warp.spawn=spawn;warp.requiredFlag=flag??"";warp.lockedPrompt=locked??"";
            var sign=signAt??new Vector2(at.x,at.z-size*.5f-1.6f);
            WorldBuilder.Label(label,new Vector3(sign.x,height(sign)+.3f,sign.y),2.2f);
            return warp;
        }
        /// <summary>The great arch on its feet: solid legs and an open way through. The fade is told where the opening is, because a hero seen through it isn't behind anything.</summary>
        static GameObject Arch(string name,Vector3 at,float size,float yaw)
        {
            var arch=Prop(Kit("AncelaneArch","HushwellColumn"),at,size,yaw,false);arch.name=name;
            var fade=arch.GetComponent<SceneryOcclusion>();
            if(fade!=null)fade.opening=new Bounds(new Vector3(0,-size*.15f,0),new Vector3(size*.4f,size*.7f,size*.26f));
            foreach(int side in new[]{-1,1})
            {
                var leg=new GameObject("Arch leg",typeof(BoxCollider));leg.transform.SetParent(arch.transform,false);leg.isStatic=true;
                leg.transform.localPosition=new Vector3(side*size*.33f,1.25f-size*.5f,0);leg.GetComponent<BoxCollider>().size=new Vector3(size*.33f,2.5f,size*.37f);
            }
            return arch;
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
        /// <summary>Small, non-blocking floor detail scattered where it never meets a route point or prompt. What keptOff refuses is dropped, not moved: the draws stay in step,
        /// so everything else stands exactly where every route was walked.</summary>
        static void Scatter(System.Random rng,int count,Func<Vector2> pick,Func<Vector2,bool> allowed,Func<Vector2,bool> keptOff,params (string key,float size,bool solid)[] kinds)
        {
            int placed=0;
            for(int tries=0;placed<count&&tries<count*12;tries++)
            {
                var p=pick();if(!allowed(p))continue;var (key,size,solid)=kinds[rng.Next(kinds.Length)];if(Prefab("Environment",key)==null)continue;
                float scale=size*(.75f+.5f*(float)rng.NextDouble()),yaw=(float)rng.NextDouble()*360;placed++;
                if(keptOff==null||!keptOff(p))Prop(key,new Vector3(p.x,key=="DressFanShrub"||key=="DressSucculent"?-PlantSkirt*scale:0,p.y),scale,yaw,solid);
            }
        }
        /// <summary>The kit's plants stand on a jagged disc of their own, up to 15% of their height: sunk this far it's under the ground and the plant starts at its leaves.</summary>
        const float PlantSkirt=.16f;
        /// <summary>On a floor sign's ground as the sign style lays it, or close enough on the camera's side to stand across its words. Planters and pallets are drawn after the signs are placed, so each zone lists its own.</summary>
        static bool OnASign(Vector2 p,(Vector2 at,float half)[] signs)
        {
            foreach(var (at,half) in signs)if(Mathf.Abs(p.x-at.x)<half+2&&p.y>at.y-3.5f&&p.y<at.y+2)return true;
            return false;
        }
        /// <summary>Close to someone, or on the floor between them and the fixed camera: from there a waist-high barrel hides their legs.</summary>
        static bool InSightOf(Vector2 p,Vector2 person,float reach,float length,float half)
        {
            var offset=p-person;if(offset.magnitude<reach)return true;
            var toCamera=new Vector2(-Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),-Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));
            float along=Vector2.Dot(offset,toCamera),across=Mathf.Abs(offset.x*toCamera.y-offset.y*toCamera.x);
            return along>0&&along<length&&across<half;
        }
        /// <summary>Rock outcrops and strata ledges along a cliff foot, so canyon walls read as rock from the fixed camera. What's left out or doesn't stay is dropped after its draws, so the others stand where they stood.</summary>
        static void CliffFoot(IEnumerable<Vector3> points,int seed,Func<GameObject,bool> stays=null,Func<Vector2,bool> leftOut=null)
        {
            var rng=new System.Random(seed);string[] kinds={"RidgeRock1","RidgeRock2","RidgeRock3","DressStrataOutcrop","DressStrataLedge","DressBoulders"};
            foreach(var p in points)
            {
                string key=kinds[rng.Next(kinds.Length)];if(Prefab("Environment",key)==null)continue;
                float size=key.StartsWith("Ridge")?3.5f+(float)rng.NextDouble()*3:key=="DressBoulders"?1.6f+(float)rng.NextDouble():2.2f+(float)rng.NextDouble()*1.5f;
                float yaw=(float)rng.NextDouble()*360;
                if(leftOut!=null&&leftOut(new Vector2(p.x,p.z)))continue;
                var rock=Prop(key,p,size,yaw,true);
                if(stays!=null&&!stays(rock))UnityEngine.Object.DestroyImmediate(rock);
            }
        }
        /// <summary>Kit rocks are fitted by height, and the wide ones are three to six times as wide as they stand: a ledge 3.3 m tall came out twenty metres across. Stood on the ground under
        /// its middle, half way up a wall, it hung out over the canyon floor four metres up, solid, between the camera and the fight: Gorge_3's stack hid the whole brawl behind one dotted hole.
        /// Now no rock is wider than this, and one is walked back from the floor until its whole footprint clears it, then rests on the lowest ground under it, its far side in the wall.</summary>
        const float WidestCliffRock=8,CliffRockClear=11.5f,MostSeatWalk=9;
        static readonly (float x,float z)[] Footprint={(-1,-1),(0,-1),(1,-1),(-1,0),(0,0),(1,0),(-1,1),(0,1),(1,1)};
        /// <summary>By its drawn box, the one the camera's fade and the checks go by.</summary>
        static Bounds Drawn(GameObject go){var parts=go.GetComponentsInChildren<Renderer>();var all=parts[0].bounds;foreach(var part in parts)all.Encapsulate(part.bounds);return all;}
        /// <summary>False when it can't clear the floor, or would be half under the ground: the caller drops it.</summary>
        static bool SeatInWall(GameObject rock,Func<Vector2,float> fromFloor,float clear,bool capped=true)
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
        /// <summary>The rule, and QaWorldSecondTests' check of it: no piece standing more than 3 m over the floor crosses the fixed camera's line to a chest on the floor of a fight or the walked path.</summary>
        const float TallOverAFight=3;
        static bool HidesFloor(Bounds box,List<Vector3> floor)
        {
            foreach(var at in floor)
            {
                if(box.max.y-at.y<=TallOverAFight)continue;
                var chest=at+Vector3.up*SightlineAudit.Chest;
                if(box.IntersectRay(new Ray(chest+SightlineAudit.ToCamera*SightlineAudit.Distance,-SightlineAudit.ToCamera),out float along)&&along<SightlineAudit.Distance-SightlineAudit.NearIgnore)return true;
            }
            return false;
        }
        static float Smooth(float edge0,float edge1,float x)=>Mathf.SmoothStep(0,1,Mathf.Clamp01((x-edge0)/(edge1-edge0)));
        static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));return Vector2.Distance(p,a+d*t);}

        // ---------------------------------------------------------------- the Wick Reach (space combat)
        const float ReachLength=300;
        /// <summary>What must stay readable on the reach: the warp home, the dock and the two relays. The flight camera looks down at 48 degrees, so anything below the flight plane
        /// shows south of where it is: a wreck 14 m under the lane sat on the warp's claws from 13 m away. So debris is kept clear by where it shows, not by where it is.</summary>
        static readonly (Vector2 at,float clear)[] ReachKeep={(new(-16,-2),11),(new(0,ReachLength-14),11),(new(-26,150),9),(new(26,228),9)};
        static bool ClearOnScreen(Vector3 p,float reach)
        {
            var shows=new Vector2(p.x,p.z-(1-p.y)/Mathf.Tan(48*Mathf.Deg2Rad));
            foreach(var (at,clear) in ReachKeep)if(Vector2.Distance(shows,at)<clear+reach)return false;
            return true;
        }
        /// <summary>A thin layer of dust through the reach, so an empty stretch of lane still moves.</summary>
        static void DriftingDust(Vector3 center,Vector3 size,Material glow)
        {
            var go=new GameObject("Drifting dust",typeof(ParticleSystem));go.transform.position=center;
            var dust=go.GetComponent<ParticleSystem>();dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=dust.main;main.loop=true;main.prewarm=true;main.duration=16;main.startLifetime=16;main.startSpeed=.3f;main.startSize=new ParticleSystem.MinMaxCurve(.14f,.5f);
            main.startColor=new Color(1,.8f,.55f,.28f);main.maxParticles=700;main.simulationSpace=ParticleSystemSimulationSpace.World;main.playOnAwake=true;
            var emission=dust.emission;emission.rateOverTime=700/16f;
            var shape=dust.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=size;
            var fade=dust.colorOverLifetime;fade.enabled=true;var ramp=new Gradient();
            ramp.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.25f),new GradientAlphaKey(1,.75f),new GradientAlphaKey(0,1)});fade.color=ramp;
            var view=go.GetComponent<ParticleSystemRenderer>();view.sharedMaterial=glow;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
        }
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
                tower.transform.rotation=Quaternion.Euler(60+(float)rng.NextDouble()*40,(float)rng.NextDouble()*360,(float)rng.NextDouble()*30);tower.name="Fallen relay "+i;Dark(tower,"LanternTower");
                if(!ClearOnScreen(tower.transform.position,7))UnityEngine.Object.DestroyImmediate(tower);
            }
            // What a fallen chain leaves: cages, column drums, mirror stands and the raiders' cuttings, no two the same size or colour. The one cage model, turned, was nearly all there was.
            // No raiders' crates: black with red corners, dark on a dark sky and in the raiders' own red, they read as a hazard or a target. Column drums in their slot, so the draws stay in step.
            (string key,float small,float large)[] wreckage={("LanternShard",1.3f,4.4f),("LanternShard",2f,3.2f),("AncelaneColumn",1.2f,2.8f),("RelayMirror",1.1f,2.2f),("HushwellRubble",.9f,2f),("AncelaneColumn",.8f,1.5f),("DressCableDrum",.8f,1.5f),("DressPowerCell",.7f,1.2f)};
            Color[] weathering={new(1,1,1),new(.62f,.6f,.66f),new(.78f,.7f,.64f),new(.5f,.54f,.66f),new(.9f,.74f,.6f)};
            for(int i=0;i<36;i++)
            {
                var (key,small,large)=wreckage[rng.Next(wreckage.Length)];
                var at=new Vector3((float)rng.NextDouble()*90-45,-6-(float)rng.NextDouble()*6,(float)rng.NextDouble()*ReachLength);float size=small+(float)rng.NextDouble()*(large-small);
                var turned=Quaternion.Euler((float)rng.NextDouble()*360,(float)rng.NextDouble()*360,(float)rng.NextDouble()*360);var tone=weathering[rng.Next(weathering.Length)];
                if(!ClearOnScreen(at,size))continue;
                var piece=Prop(Kit(key,"CrystalClusterB"),at,size,0,false,false);piece.transform.rotation=turned;piece.name="Wreckage "+key;piece.AddComponent<PropTint>().tint=tone;
            }
            var glow=CurtainMaterial("WickReach",false,"Glow");
            DriftingDust(new Vector3(0,-2,ReachLength*.5f),new Vector3(110,14,ReachLength+20),glow);
            new GameObject("Sky drift",typeof(SkyDrift));
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
            foreach(var r in gate.GetComponentsInChildren<Renderer>())r.enabled=false;
            var beam=CurtainMaterial("WickReach");
            // The door goes at once, so nothing waits on a fade to fly through; the strands are their own object and part with a moment.
            var strands=new GameObject("Lantern gate strands",typeof(LightCurtain));strands.transform.position=gate.transform.position;
            var wave=new System.Random(61);
            for(float x=-54;x<=54;x+=3)
            {
                // Nothing lies under it here to meet its foot: the feet are scattered so no line runs through them, and it thins out and shortens toward both ends instead of stopping in a row of teeth.
                float taper=1-Smooth(.66f,1,Mathf.Abs(x)/54);
                float lean=((float)wave.NextDouble()-.5f)*1.2f,rise=(8+(float)wave.NextDouble()*3.5f)*Mathf.Lerp(.55f,1,taper),width=(4.4f+(float)wave.NextDouble()*1.2f)*Mathf.Lerp(.72f,1,taper);
                var foot=new Vector3(x+((float)wave.NextDouble()-.5f)*1.6f,-6-(float)wave.NextDouble()*1.6f,ReachLength-40+((float)wave.NextDouble()-.5f)*1.8f);
                Strand(strands.transform,foot,foot+new Vector3(lean,rise,0),beam,width,new Color(1,.6f,.22f,.62f*Mathf.Lerp(.15f,1,taper)),new Color(1,.6f,.22f,0));
            }
            // The relay that opens it can be 30 m away: a beam runs from the relay to the curtain as it comes down.
            var sweep=new GameObject("Curtain sweep",typeof(LineRenderer)).GetComponent<LineRenderer>();sweep.useWorldSpace=true;sweep.positionCount=2;sweep.startWidth=sweep.endWidth=2.4f;
            sweep.sharedMaterial=glow;sweep.startColor=sweep.endColor=new Color(1,.78f,.42f,.9f);sweep.shadowCastingMode=ShadowCastingMode.Off;sweep.enabled=false;
            strands.GetComponent<LightCurtain>().sweep=sweep;
            foreach(float x in new[]{-56f,-20f,20f,56f})Prop(Kit("LanternTower","HushwellColumn"),new Vector3(x,-9,ReachLength-40),10,x,false,false);
            var gateKeeper=new GameObject("Lantern gate light",typeof(FlagGate)).GetComponent<FlagGate>();gateKeeper.flag="wick.gateOpen";gateKeeper.showWhenSet=false;gateKeeper.targets=new[]{gate.gameObject};
            // In front of the curtain, and gone with it: once it has come down there is no gate here to name. Larger than the style's floor sign: it's mostly read with the view pulled far back to show the curtain.
            WorldBuilder.Label("LANTERN GATE",new Vector3(0,-1,ReachLength-41.9f),7).transform.SetParent(strands.transform,true);
            WorldBuilder.Dock("Dock — Lanternwick",new Vector3(0,1,ReachLength-14),"Lanternwick","Arrival",WorldBuilder.FlightDockRange);
            WorldBuilder.Label("LANTERNWICK",new Vector3(0,5,ReachLength-10),2.6f);
            // The dock was a bare disc alone in the sky, and the next frame a desert town. The canyon's lip and two of its lanterns stand below and beyond it. Under the flight plane and drawn only, as all the Reach's backdrop is.
            foreach(var (key,x,y,z,size,yaw) in new[]{("RidgeRock2",-17f,-14f,ReachLength-4,11f,30f),("RidgeRock2",18f,-15f,ReachLength+2,12f,200f),("RidgeRock3",-2f,-17f,ReachLength+12,12f,110f),("LanternTower",-12f,-10f,ReachLength+3,9f,0f),("LanternTower",13f,-11f,ReachLength+7,10f,40f)})
                Prop(Kit(key,"CrystalClusterB"),new Vector3(x,y,z),size,yaw,false,false).name="Lanternwick below the dock";
            // No lock: nobody stands in the Reach without the route. It lands above Tallow with the dock still ahead, and is called that, as the Halo's and the Gullet's are.
            WorldBuilder.Warp("Warp — Tallow Approach",new Vector3(-16,1,-2),"TallowApproach","","Wick").lockedPrompt="";
            // Walls at the flight plane; the far reaches stay visible.
            WorldBuilder.Boundary("West reach limit",new Vector3(-58,1,ReachLength*.5f),new Vector3(2,12,ReachLength+60));
            WorldBuilder.Boundary("East reach limit",new Vector3(58,1,ReachLength*.5f),new Vector3(2,12,ReachLength+60));
            WorldBuilder.Boundary("South reach limit",new Vector3(0,1,-16),new Vector3(120,12,2));
            WorldBuilder.Boundary("North reach limit",new Vector3(0,1,ReachLength+4),new Vector3(120,12,2));
            WorldBuilder.Save(scene,"WickReach");Debug.Log("WICK_REACH_OK");
        }

        // ---------------------------------------------------------------- Lanternwick (town)
        /// <summary>The town's named people. Nothing the scatter or the street lamps put down may stand on them or in the camera's line to them.</summary>
        static readonly Vector2[] TownPeople={new(22,52),new(6,34),new(4,92),new(-6,17),new(-32,60)};
        /// <summary>The ground, with the bluff the canyon wall throws forward at the archive: the camera sees only about 9 m ahead of the hero, so a wall has to come this close to be
        /// behind Ishen when you talk to him.</summary>
        static float TownHeight(Vector2 p)=>Mathf.Max(TownGround(p),Smooth(98.6f,101,p.y)*(1-Smooth(9.5f,13,Mathf.Abs(p.x)))*12);
        static float TownGround(Vector2 p)
        {
            // A canyon floor (0 m) with a raised east lantern terrace (+3 m, ramp at its south end), a low west orchard
            // terrace (+1.4 m) and carved walls; the south mouth stays low so the fixed camera sees the courier dock.
            float east=Smooth(15,19,p.x)*Smooth(18,34,p.y)*(1-Smooth(96,100,p.y))*3f;
            float west=Smooth(-17,-21,p.x)*Smooth(42,48,p.y)*(1-Smooth(84,88,p.y))*1.4f;
            float walls=Smooth(33,39,Mathf.Abs(p.x))*(14+6*Mathf.PerlinNoise(p.x*.05f+3,p.y*.05f))+Smooth(44,52,Mathf.Abs(p.x))*8;
            // The fixed camera sits south-west of the hero, so the west wall is its foreground. At full height its top came up to the lens: a third of the screen was
            // one long smear of strata on the way to the orchard gate. Kept low, as the gorge keeps its own.
            if(p.x<0)walls*=.42f;
            float north=Smooth(106,114,p.y)*(16+5*Mathf.PerlinNoise(p.x*.06f,p.y*.04f));
            // What stops the party at the mouth has to be seen doing it: the limit stands at -33, and a bank that only began there ran on at a walkable slope with nothing marking the stop.
            // It rises from the limit's face now, steep enough to be cliff. 4 m by -38 clears a sphere cast from the hero's chest at the camera's pitch by a metre: by -36 it would drag the camera in.
            float south=Smooth(-33,-38,p.y)*4;
            float texture=.25f*Mathf.PerlinNoise(p.x*.12f+10,p.y*.12f);
            return Mathf.Max(Mathf.Max(east,west),Mathf.Max(walls,Mathf.Max(north,south)))+texture;
        }
        /// <summary>The way through town is laid stone, paler than the ground either side of it, and opens out at the courier square, the market and the archive. It's the floor's own
        /// vertex colour: nothing to draw, and no edge to read as a sticker.</summary>
        static Color TownTone(Vector2 p)
        {
            float street=p.y<-22||p.y>96?0:1-Smooth(4.6f,6.6f,Mathf.Abs(p.x));
            float square=1-Smooth(8,12.5f,Vector2.Distance(p,new Vector2(0,-12))),market=1-Smooth(8,12,Vector2.Distance(p,new Vector2(0,18))),forecourt=1-Smooth(7,11,Vector2.Distance(p,new Vector2(1,91)));
            float laid=Mathf.Max(street,Mathf.Max(square,Mathf.Max(market,forecourt))),wear=.93f+.07f*Mathf.PerlinNoise(p.x*.17f+3,p.y*.17f+7);
            var tone=Color.Lerp(new Color(.8f,.72f,.66f),new Color(1,.97f,.93f),laid);
            return new Color(tone.r*wear,tone.g*wear,tone.b*wear,1);
        }
        /// <summary>The market goes on down to the square, facing the street as the others do: the model's counter and shelf are on its forward side, and its back is a plain dark panel. Lamps climb the east terrace, off the walk up it.</summary>
        static readonly (float x,float z,float yaw)[] NewStalls={(8.6f,4.6f,-90),(-8.8f,3.6f,85)};
        static readonly (float x,float z)[] StairLamps={(22.6f,21.5f),(22.6f,26.5f),(22.6f,31),(22.6f,39.5f),(22.6f,46)};
        /// <summary>People who live here, with no names and nothing to say: behind the stalls, on the square, on a doorstep, up on the terrace. Off every route, none between the camera and anyone with
        /// a name, and each tinted so nobody takes one for the person whose body it borrows. They stand in a body-sized box, so the navigation walks round them.</summary>
        static readonly (string body,float x,float z,float yaw,Color tint)[] Townsfolk=
        {
            ("Neve",10.2f,14.2f,-90,new(.84f,.74f,.66f)),("SurvivorNatural",11,23.2f,-100,new(.7f,.78f,.72f)),("Hal",-11,22,80,new(.78f,.7f,.74f)),
            ("Mira",10.6f,4.6f,-90,new(.72f,.74f,.84f)),("Orrin",-10.8f,3.6f,85,new(.86f,.76f,.62f)),
            ("Neve",-9.4f,-7.2f,120,new(.66f,.74f,.8f)),("SurvivorNatural",7.5f,-8.4f,-100,new(.82f,.7f,.6f)),
            ("Hal",-7.6f,62.8f,100,new(.74f,.8f,.7f)),("Mira",25,46,-120,new(.86f,.72f,.72f)),("Orrin",-7.6f,88.4f,60,new(.68f,.72f,.8f)),
        };
        static void Townsperson(string body,Vector3 at,float yaw,Color tint)
        {
            var person=new GameObject("Townsperson");person.transform.SetPositionAndRotation(new Vector3(at.x,height(new Vector2(at.x,at.z)),at.z),Quaternion.Euler(0,yaw,0));person.isStatic=true;
            var model=(GameObject)PrefabUtility.InstantiatePrefab(Body("Characters",body,"Characters","Neve"));model.transform.SetParent(person.transform,false);model.name="Generated civilian";
            // Static for the bake, which only reads static boxes. Off screen, a body's animation stops: this camera can hold four of them at most, on the square, and the town's frame budget is close.
            var solid=person.AddComponent<BoxCollider>();solid.size=new Vector3(.7f,1.9f,.7f);solid.center=Vector3.up*.95f;
            person.AddComponent<PropTint>().tint=tint;
            foreach(var rig in model.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullCompletely;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
        }
        /// <summary>The town's floor signs, for the scatter to keep off: their spots and half their widths. Two spots signs have left stay listed, so nothing new lands where every route was walked.</summary>
        static readonly (Vector2 at,float half)[] TownSigns={(new(7.7f,-13.4f),3.2f),(new(3.85f,-12.5f),1.9f),(new(0,8),1.8f),(new(-8.5f,33.6f),1.6f),(new(21.5f,35),1.9f),(new(24,48.5f),1.9f),(new(24.6f,51.8f),1.9f),(new(-4.2f,91.4f),1.2f),(new(-23.5f,46.5f),1.3f),(new(-27,82),3.2f)};
        /// <summary>What the town's scatter stays off: its people, its signs, the archive's forecourt, and the homes that close in on the courier square.</summary>
        static bool ScatterKeepsOff(Vector2 p)
        {
            foreach(var person in TownPeople)if(InSightOf(p,person,2.6f,4.5f,1.6f))return true;
            if(OnASign(p,TownSigns))return true;
            foreach(var (_,x,z,_,_) in Townsfolk)if(Vector2.Distance(p,new Vector2(x,z))<2.4f)return true;
            foreach(var (x,z,_) in NewStalls)if(Vector2.Distance(p,new Vector2(x,z))<3)return true;
            foreach(var (x,z) in StairLamps)if(Vector2.Distance(p,new Vector2(x,z))<2)return true;
            if(OnTheStair(p))return true;
            // Where two homes now stand. What the scatter would put inside them is dropped, not moved.
            if(p.x>-18&&p.x<-5.2f&&p.y>58&&p.y<68||p.x>7&&p.x<15&&p.y>64&&p.y<72)return true;
            if(p.x>-11&&p.x<12.5f&&p.y>86&&p.y<99.5f)return true;
            return p.x>-18&&p.x<-10&&p.y>-9&&p.y<-1||p.x>11&&p.x<19&&p.y>-11&&p.y<-3;
        }
        /// <summary>The lantern stair: it stands behind the keeper and climbs toward the tallest tower, its risers to the camera and one flank in view, so it reads as steps going up from where she stands. Solid and off the bake's walkable floor, so nobody walks through it: nothing goes up it, and no route does.</summary>
        static readonly Vector2 StairFoot=new(22.4f,54.4f);
        const float StairYaw=35,StairWidth=3.6f,StairRun=.85f,StairRise=.34f,StairLanding=1.2f;const int StairSteps=10;
        static Vector3 StairPoint(float along,float across,float up)=>new Vector3(StairFoot.x,height(StairFoot),StairFoot.y)+Quaternion.Euler(0,StairYaw,0)*new Vector3(across,up,along);
        static bool OnTheStair(Vector2 p)
        {
            var local=Quaternion.Euler(0,-StairYaw,0)*new Vector3(p.x-StairFoot.x,0,p.y-StairFoot.y);
            return local.z>-1.5f&&local.z<StairSteps*StairRun+StairLanding+1.5f&&Mathf.Abs(local.x)<StairWidth*.5f+1.5f;
        }
        static void LanternStair()
        {
            const float Skirt=-.5f;float half=StairWidth*.5f,end=StairSteps*StairRun+StairLanding,top=StairSteps*StairRise;
            var points=new List<Vector3>();var uv=new List<Vector2>();var tones=new List<Color>();var treads=new List<int>();var faces=new List<int>();
            void Quad(List<int> into,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outward,Color tone,Func<Vector3,Vector2> map)
            {
                int at=points.Count;foreach(var p in new[]{a,b,c,d}){points.Add(p);uv.Add(map(p));tones.Add(tone);}
                into.AddRange(Vector3.Dot(Vector3.Cross(b-a,c-a),outward)<0?new[]{at,at+2,at+1,at,at+3,at+2}:new[]{at,at+1,at+2,at,at+2,at+3});
            }
            // Pale treads and dark risers, the two things that say steps from up here.
            Color tread=new(1,.95f,.88f),riser=new(.46f,.4f,.42f),flank=new(.62f,.55f,.54f);
            Vector2 Plan(Vector3 p)=>new(p.x/8,p.z/8);Vector2 Across(Vector3 p)=>new(p.x/7,p.y/5);Vector2 Along(Vector3 p)=>new(p.z/7,p.y/5);
            for(int k=0;k<StairSteps;k++)
            {
                float near=k*StairRun,far=k==StairSteps-1?end:(k+1)*StairRun,y=(k+1)*StairRise,under=k==0?Skirt:k*StairRise;
                Quad(treads,new(-half,y,near),new(-half,y,far),new(half,y,far),new(half,y,near),Vector3.up,tread,Plan);
                Quad(faces,new(-half,under,near),new(-half,y,near),new(half,y,near),new(half,under,near),Vector3.back,riser,Across);
                foreach(int side in new[]{-1,1})Quad(faces,new(side*half,Skirt,near),new(side*half,y,near),new(side*half,y,far),new(side*half,Skirt,far),Vector3.right*side,flank,Along);
            }
            Quad(faces,new(-half,Skirt,end),new(-half,top,end),new(half,top,end),new(half,Skirt,end),Vector3.forward,flank,Across);
            var mesh=new Mesh{name="Lantern stair",subMeshCount=2};mesh.SetVertices(points);mesh.SetUVs(0,uv);mesh.SetColors(tones);mesh.SetTriangles(treads,0);mesh.SetTriangles(faces,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var stair=new GameObject("Lantern stair",typeof(MeshFilter),typeof(MeshRenderer));stair.transform.SetPositionAndRotation(StairPoint(0,0,0),Quaternion.Euler(0,StairYaw,0));
            stair.GetComponent<MeshFilter>().sharedMesh=mesh;stair.GetComponent<MeshRenderer>().sharedMaterials=new[]{Canyon(false),Canyon(true)};
            var block=new GameObject("Stair block",typeof(BoxCollider));block.isStatic=true;block.transform.SetParent(stair.transform,false);block.transform.localPosition=new Vector3(0,(top+Skirt)*.5f,end*.5f);block.GetComponent<BoxCollider>().size=new Vector3(StairWidth,top-Skirt,end);
            // Lanterns up both sides and a pair at the top. Fixtures with no lights of their own: this terrace is one floor renderer, already past Forward's six lights a renderer.
            foreach(var (along,up) in new[]{(3.5f*StairRun,4*StairRise),(6.5f*StairRun,7*StairRise),(9.5f*StairRun,top),(end-.5f,top)})foreach(int side in new[]{-1,1})
                Prop(Kit("LampPost","DressLightBollard"),StairPoint(along,side*(half-.24f),up),1.25f,0,false,false).transform.SetParent(stair.transform,true);
        }
        static void Lanternwick()
        {
            height=TownHeight;CliffTop=3.6f;fadeTall=true;
            var scene=WorldBuilder.Begin("Lanternwick");
            // Half-metre cells: floor and wall meet along single triangles, and at a metre each the foot of every wall was a row of hard teeth (E1-lanternwick-13).
            Terrain("Lanternwick","Canyon terraces",-60,60,-50,125,.5f,Canyon(false),TownTone);
            WorldBuilder.Spawn("Arrival",new Vector3(0,0,-14));WorldBuilder.Spawn("Gorge",new Vector3(-30,1.4f,80));WorldBuilder.Spawn("Archive",new Vector3(0,0,92));
            var dock=Prop("LandingPad",new Vector3(0,0,-22),5,0);dock.name="Courier pad";
            var pad=dock.AddComponent<DockingPad>();pad.prompt="Launch — The Wick Reach";pad.range=4;pad.scene="WickReach";pad.spawn="Lanternwick";
            // Against the ringed mat's east side, on the first screen. Further out it hung over bare floor with nothing under it, the first sign in town naming nothing.
            WorldBuilder.Label("COURIER DOCK",new Vector3(3.85f,.1f,-12.5f));
            // The great arch stands at the head of the courier square, the way into the town from the pad. Between the camera and the arriving party it filled the first screen: the party small between its piers, and no town.
            // From here the first look is the square with the party on it and the arch's feet ahead, and the walk up the street goes through it. It stays well south of the market: anything tall over the street
            // passes under the lens at the stalls, and an arch fading out at Corra's stall read as a rendering fault.
            Arch("Market arch",new Vector3(0,0,-8.2f),8,0);
            // The square itself: a carved pad in the stone, a notice post and a tree, and homes closing in either side.
            // Only its carved top is wanted, so the pad is pressed flat into the floor rather than most of a plinth buried under it. Dark: cyan arcs lit in a town of amber read as a stray machine.
            var inlay=Dark(Prop("HushwellMarker",new Vector3(0,-.215f,-11.8f),.47f,0,false));inlay.transform.localScale=new Vector3(1,.3f,1);inlay.AddComponent<PropTint>().tint=new Color(.92f,.72f,.55f);
            Prop(Kit("OrchardTree","DressFanShrub"),new Vector3(-7.6f,0,-10),3.2f,40);
            Prop("DressKiosk",new Vector3(5.6f,0,-9.5f),1.7f,-20);
            // The arch market: stalls either side of the main street.
            foreach(var (x,z,yaw) in new[]{(-8f,14f,90f),(8f,14f,-90f),(-9f,22f,80f),(9f,23f,-100f)})Prop("DressMarketStall",new Vector3(x,0,z),2.6f,yaw);
            foreach(var (x,z,yaw) in NewStalls)Prop("DressMarketStall",new Vector3(x,0,z),2.6f,yaw);
            Prop("DressKiosk",new Vector3(-4,0,27),2.2f,10);Prop("DressWaterBarrels",new Vector3(12,0,10),1.2f,40);Prop("DressCargoPallet",new Vector3(-12,0,8),1.1f,20);
            // Up the street from the arch, between the first stalls. Any nearer and the arch's crown stands across its words from the camera.
            WorldBuilder.Label("ARCH MARKET",new Vector3(0,.1f,8));
            var bench=Prop("LatticeAnvil",new Vector3(13,0,18),1.8f,-90).AddComponent<Bench>();bench.prompt="Fabricate — Lattice Anvil";
            // The Wick House: rooms above the market, a repair cradle and a bed.
            Prop(Kit("CanyonHouse","OutpostHab"),new Vector3(-17,0,31),6,90);
            var repair=Prop("DeckConsole",new Vector3(-12,0,30),1.4f,90).AddComponent<RepairBay>();repair.prompt="Wick House — repair and save";repair.saveSpawn="Arrival";
            WorldBuilder.Label("WICK HOUSE",new Vector3(-8.5f,.1f,33.6f));
            // Homes: carved along the canyon walls and lining the main street up to the archive, doors to the street.
            foreach(var (x,z,yaw) in new[]{(-30f,4f,90f),(-31f,20f,90f),(31f,4f,-90f),(-12f,98f,180f),(14f,99f,180f),(30f,46f,-90f),(30f,74f,-90f),(-30f,32f,90f),
                (-12f,40f,90f),(-12f,56f,90f),(-12f,63f,90f),(-12f,84f,90f),(11f,40f,-90f),(11f,68f,-90f),(11f,80f,-90f),(-14f,-5f,90f),(15f,-7f,-90f),(-20f,92f,140f),(22f,92f,-140f)})
                Prop(Kit("CanyonHouse","OutpostHab"),new Vector3(x,0,z),x*x>400?5.5f:4.8f,yaw);
            // The street itself: lamps on both sides and the clutter of people who live there.
            for(float z=-6;z<92;z+=11)foreach(int side in new[]{-1,1})
            {
                var at=new Vector3(side*6.5f,0,z+(side>0?5:0));
                // Not in front of anyone: a post stood across Corra's legs for her whole conversation.
                while(TownPeople.Any(p=>InSightOf(new Vector2(at.x,at.z),p,1.4f,3.5f,1)))at.z+=1.7f;
                Prop(Kit("LampPost","DressLightBollard"),at,1.6f,0);Lamp(at+Vector3.up*1.6f,8,1.3f);
            }
            var trng=new System.Random(29);
            Scatter(trng,60,()=>new Vector2((float)trng.NextDouble()*60-30,(float)trng.NextDouble()*120-20),p=>
                {float street=Mathf.Abs(p.x);bool walk=street<5.2f||Mathf.Abs(p.y-26)<3.5f&&p.x>0||Mathf.Abs(p.y-48)<3.5f&&p.x<0||Vector2.Distance(p,new Vector2(0,-20))<9||Vector2.Distance(p,new Vector2(-34,84))<6;
                 return !walk&&TownGround(p)<3.6f&&street<31;},ScatterKeepsOff,
                ("DressPlanter",1f,true),("DressWaterBarrels",1.1f,true),("DressFanShrub",1.3f,false),("DressSucculent",1f,false),("DeckCrate",1f,true),("DressCargoPallet",1f,true),("HushwellRubble",.9f,false));
            // The lantern stair: three towers on the east terrace, the keeper below the tallest.
            // Lanternwick never let these go out: "kept the lanterns lit for anyone who came back".
            LanternStair();
            Relay("Lantern tower south",new Vector3(28,0,40),"",14,0).alwaysLit=true;
            Relay("Lantern tower",new Vector3(31,0,64),"",18,15).alwaysLit=true;
            Relay("Lantern tower north",new Vector3(27,0,88),"",13,-10).alwaysLit=true;
            // On the floor at the stair's foot, under its first step's east end and beside the keeper. 3 m further south it named bare floor by a lamp post. Any further east the home at (30,46) stands across its last letters.
            WorldBuilder.Label("LANTERN STAIR",new Vector3(24.6f,3.2f,51.8f));
            // Lit lamps climb the stair. Fixtures and painted pools, with no lights of their own: the terrace is part of one floor renderer, already past Forward's six lights a renderer, and one more
            // real lamp would put out another pool somewhere else in view.
            var stairPools=new GameObject("Stair lamp pools");var stairPool=ChapterOneStaging.Soft("lamp-pool",new Color(1,.7f,.36f,.2f));
            foreach(var (x,z) in StairLamps)
            {
                // Laid on the ground itself. Following whatever a ray meets first, it ran up the planter and the ledge beside the first lamp as a slanted orange sheet.
                var pool=ChapterOneStaging.SoftPatch("Lamp pool",stairPools.transform,new Vector3(x,height(new Vector2(x,z)),z),2.6f,2.6f,0,stairPool,false).GetComponent<MeshFilter>().sharedMesh;
                var lying=pool.vertices;for(int i=0;i<lying.Length;i++)lying[i].y=height(new Vector2(lying[i].x,lying[i].z))+.04f;pool.vertices=lying;pool.RecalculateBounds();
                Prop(Kit("LampPost","DressLightBollard"),new Vector3(x,0,z),1.6f,0);
            }
            // The archive: a door carved into the bluff, broken columns either side of it, and the racks and mirror stands of the people who read the plates.
            Arch("Archive door",new Vector3(0,0,98.7f),8,0);
            foreach(float x in new[]{-7f,7f})Prop(Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,0,98.8f),4.5f,0);
            foreach(var (x,z,yaw) in new[]{(5.6f,95.5f,0f),(8.9f,95.1f,-12f)})Prop("DressShelfRack",new Vector3(x,0,z),1.9f,yaw);
            foreach(var (x,z,yaw) in new[]{(9.8f,91.8f,-30f),(-6f,92.4f,30f)})Prop(Kit("RelayMirror","LanternShard"),new Vector3(x,0,z),1.5f,yaw);
            // Dark until Ishen has the record back ("the archive's lit again"); the wall of light patterns answers the relays' captain.
            Lantern(Prop(Kit("GreatLantern","CrystalClusterA"),new Vector3(0,0,95.2f),3,0),"ishen.recordRestored",3,"GreatLantern");
            var wall=ArchiveWall(new Vector3(0,.7f,96.7f));
            var wallLit=new GameObject("The archive wall lights",typeof(FlagGate)).GetComponent<FlagGate>();wallLit.flag="bossdown.HessaKorl";wallLit.targets=new[]{wall};wall.SetActive(false);
            WorldBuilder.Label("ARCHIVE",new Vector3(-4.2f,.1f,91.4f));
            // The orchard terrace and the shed someone is hiding in.
            foreach(var (x,z) in new[]{(-25f,50f),(-31f,56f),(-24f,62f),(-30f,70f),(-22f,75f),(-34f,46f)})Prop(Kit("OrchardTree","DressFanShrub"),new Vector3(x,0,z),3.2f,x*z);
            // Up on the cliff at x -36 the shed was never in frame. A deserter's lean-to against the wall behind him, in view while he talks. On its own it read as a black tarp on poles,
            // and the quest says shed: so a small one of the town's own stone stands behind it, let into the wall, its door to the terrace. Well back from the line the route walks past him: nearer, on the way on to the gate, its back filled the foot of the screen.
            Prop(Kit("CanyonHouse","OutpostHab"),new Vector3(-36.2f,1.4f,64),3,90,true,false);
            Prop(Kit("MusterLeanTo","DressCargoPallet"),new Vector3(-33.8f,0,63.6f),2.6f,110,false);
            WorldBuilder.Label("ORCHARD",new Vector3(-23.5f,1.5f,46.5f));
            // Clear of the wall's foot, or its left leg goes three metres into the rock. Its sign lies south and east of it, on open terrace off to the side of where the route stops for the prompt.
            Gate("Orchard gate — Lantern Gorge",new Vector3(-32.2f,0,86),"LanternGorge","Arrival","met.Aumi","Talk to Keeper Aumi on the lantern stair first","AncelaneArch",5.5f,5,new Vector2(-27,82));
            // Lamps and lamp posts along the walk.
            foreach(var (x,z) in new[]{(-4.4f,-12.2f),(4.2f,-10.8f),(-5f,30f),(5f,40f),(-6f,55f),(6f,70f),(-5f,85f),(18f,26f)})
            {Prop(Kit("LampPost","DressLightBollard"),new Vector3(x,0,z),1.6f,0);Lamp(new Vector3(x,1.6f,z));}
            // The canyon walls' feet: rock and strata between the homes.
            var townCliffs=new List<Vector3>();
            for(float z=-26;z<104;z+=9)foreach(int side in new[]{-1,1})
            {
                float x=side*(32.5f+1.5f*Mathf.PerlinNoise(z*.1f,side));
                if(Mathf.Abs(z-4)<5||Mathf.Abs(z-20)<5&&side<0||Mathf.Abs(z-46)<5&&side>0||Mathf.Abs(z-74)<5&&side>0||Mathf.Abs(z-32)<5&&side<0||side<0&&z>40&&z<92)continue;
                townCliffs.Add(new Vector3(x,0,z));
            }
            for(float x=-30;x<=30;x+=10)if(Mathf.Abs(x)>13)townCliffs.Add(new Vector3(x,0,106));
            // Nothing wide stands straight behind the orchard gate: a way out with a cliff of rock across it isn't one.
            CliffFoot(townCliffs,11,null,p=>p.x<-20&&p.y>88&&p.y<104);
            // People.
            foreach(var (body,x,z,yaw,tint) in Townsfolk)Townsperson(body,new Vector3(x,0,z),yaw,tint);
            Person("Aumi",new Vector3(22,0,52),-110,"bossdown.HessaKorl",true,"lanternwick.chartGiven","AumiFinal").postNode="AumiReturn";
            var townPell=Person("Pell",new Vector3(6,0,34),200,"lanternwick.chartGiven");
            var gone=new GameObject("Pell has gone to Tallow",typeof(FlagGate)).GetComponent<FlagGate>();gone.flag="pell.aboard";gone.showWhenSet=false;gone.targets=new[]{townPell.gameObject};
            Person("Ishen",new Vector3(4,0,92),170,"ishen.recordRestored");
            var corra=Person("Corra",new Vector3(-6,0,17),110,"unsworn.rationsDelivered");
            Person("Brekk",new Vector3(-32,0,60),90,"unsworn.rationsDelivered");
            // "It's on the stair, where she kept it": lit once she has it back, and gone with her to Tallow, where the chewed lamp stands beside her. Two gates, one for each.
            var pellStand=new GameObject("Pell's lamp stand");
            var pellLamp=Prop(Kit("LampPost","DressLightBollard"),StairPoint(1.5f*StairRun,StairWidth*.5f-.45f,2*StairRise),1.5f,0,false,false);pellLamp.name="Pell's lamp";pellLamp.transform.SetParent(pellStand.transform,true);
            var pellLight=new GameObject("Lamp light",typeof(Light));pellLight.transform.SetParent(pellLamp.transform,false);pellLight.transform.position=pellLamp.transform.position+Vector3.up*1.4f;
            var pellGlow=pellLight.GetComponent<Light>();pellGlow.type=LightType.Point;pellGlow.color=new Color(1,.78f,.45f);pellGlow.range=6;pellGlow.intensity=1.6f;pellGlow.shadows=LightShadows.None;
            var pellBurns=new GameObject("Pell's lamp burns",typeof(FlagGate)).GetComponent<FlagGate>();pellBurns.flag="pell.lampReturned";pellBurns.targets=new[]{pellLamp};pellLamp.SetActive(false);
            var pellTakes=new GameObject("Pell takes her lamp",typeof(FlagGate)).GetComponent<FlagGate>();pellTakes.flag="pell.aboard";pellTakes.showWhenSet=false;pellTakes.targets=new[]{pellStand};
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
            var beam=CurtainMaterial("Lanternwick",false);var rng=new System.Random(71);const float Half=7.2f,Tall=3.4f;
            for(int passage=0;passage<6;passage++)
            {
                float rise=(passage+.5f)/6*Tall,sway=.14f+(float)rng.NextDouble()*.2f,phase=(float)rng.NextDouble()*6.28f,bends=.5f+(float)rng.NextDouble()*.5f;
                var line=new GameObject("Passage pattern",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(wall.transform,false);
                const int Points=28;line.useWorldSpace=false;line.positionCount=Points;
                for(int i=0;i<Points;i++){float x=-Half+2*Half*i/(Points-1);line.SetPosition(i,new Vector3(x,rise+Mathf.Sin(x*bends+phase)*sway,0));}
                // The soft flare beads along the line, a pulse of light every couple of metres.
                line.startWidth=line.endWidth=.55f;line.sharedMaterial=beam;line.shadowCastingMode=ShadowCastingMode.Off;line.textureMode=LineTextureMode.Tile;line.textureScale=new Vector2(.45f,1);
                var tone=passage%3==0?new Color(1,.86f,.55f,.95f):new Color(1,.7f,.35f,.8f);line.startColor=tone;line.endColor=tone;
                for(int town=0;town<3;town++)
                {
                    float x=-Half+.8f+(float)rng.NextDouble()*(2*Half-1.6f);var center=new Vector3(x,rise+Mathf.Sin(x*bends+phase)*sway+.28f,0);
                    Strand(wall.transform,wall.transform.TransformPoint(center-Vector3.right*.3f),wall.transform.TransformPoint(center+Vector3.right*.3f),beam,.7f,new Color(1,.95f,.7f,1),new Color(1,.95f,.7f,1));
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
        /// <summary>The middle of each of the gorge's fights.</summary>
        static readonly Vector2[] GorgeFights={new(2,26),new(8,60),new(-22,98),new(-6,114),new(2,156),new(6,204)};
        /// <summary>The floor a fight or the walk uses: four metres either side of the path, and round each fight's middle as far as a fight really drifts.</summary>
        static List<Vector3> GorgeFloor()
        {
            var floor=new List<Vector3>();
            void Add(Vector2 p){if(GorgeDistance(p)<12)floor.Add(new Vector3(p.x,height(p),p.y));}
            for(int i=1;i<GorgePath.Length;i++)
            {
                Vector2 a=GorgePath[i-1],d=GorgePath[i]-a;float length=d.magnitude;var across=new Vector2(-d.y,d.x)/length;
                for(float along=0;along<=length;along+=2)foreach(float off in new[]{-4f,-2f,0f,2f,4f})Add(a+d*(along/length)+across*off);
            }
            for(float along=0;along<=1;along+=.1f)Add(Vector2.Lerp(DenEntry,Den,along));
            foreach(var fight in GorgeFights)for(float x=-10;x<=10;x+=2)for(float z=-7;z<=7;z+=2)Add(fight+new Vector2(x,z));
            return floor;
        }
        /// <summary>What the gorge's scatter stays off: the spots you relight a relay from (a dark cage lying beside the prompt looked like the thing to light), the things to take, and its floor signs.</summary>
        static readonly (Vector2 at,float half)[] GorgeSigns={(new(-8,100),2.2f),(new(.4f,-9),2.6f),(new(4.6f,227.2f),2.8f)};
        /// <summary>The scavengers' tent, and how far a rock keeps from it: a boulder on the verge stood through its corner, with a tent peg poking out of the stone.</summary>
        static readonly Vector2 CampTent=new(-14,112);const float TentClear=4.6f;
        /// <summary>The den's fallen tower, near enough to be over the den on screen. Its east end stops short of the camp's floor, or it stands across the line to that fight, and its south edge short of the den's.</summary>
        static readonly Vector2 DenTower=new(-28.2f,108.9f);
        /// <summary>The road's dead lanterns, at its two ends where the floor had nothing on it. Below the size that would stand across a fight, off the walked line and drawn only.</summary>
        static readonly (float x,float z,float yaw)[] RoadLanterns={(-7,3.5f,30),(5,-3,200),(6.5f,216,80),(-6.8f,219.2f,140),(7.2f,223,300)};
        static bool GorgeKeepsOff(Vector2 p)
        {
            if(OnASign(p,GorgeSigns)||Vector2.Distance(p,CampTent)<TentClear)return true;
            foreach(var clear in new[]{new Vector2(9,52),new Vector2(-9,124),new Vector2(10,186)})if(Vector2.Distance(p,clear)<5.5f)return true;
            return Vector2.Distance(p,new Vector2(-12.6f,103.4f))<3||Vector2.Distance(p,new Vector2(Den.x+.8f,Den.y+.5f))<3;
        }
        /// <summary>The way through is worn paler than the ground either side of it, in the floor's own vertex colour, so long stretches of one flat mauve have something to lead the eye. The floor under the relay door runs on into the dark it opens on.</summary>
        static Color GorgeTone(Vector2 p)
        {
            float worn=1-Smooth(2.5f,8,GorgeDistance(p)),wear=.9f+.1f*Mathf.PerlinNoise(p.x*.17f+3,p.y*.17f+7);
            var tone=Color.Lerp(new Color(.78f,.72f,.74f),new Color(1,.97f,.94f),worn);
            return Color.Lerp(new Color(tone.r*wear,tone.g*wear,tone.b*wear,1),new Color(.16f,.15f,.17f),(1-Smooth(1.6f,3.6f,Mathf.Abs(p.x)))*Smooth(228.4f,231.2f,p.y));
        }
        /// <summary>The den was open floor with an agave on it. Dark trodden ground, and what they've dragged in out of the light along its back and its west side: dead lamps, a broken cage, a heap of spoil. Drawn only, and off the walk to Pell's lamp.</summary>
        static void GrazerDen()
        {
            var den=new GameObject("Grazer den");
            var dirt=ChapterOneStaging.SoftPatch("Grazer den dirt",den.transform,new Vector3(-27.5f,height(Den),100.5f),9.5f,7,10,ChapterOneStaging.Soft("den-dirt",new Color(.05f,.035f,.04f,.5f)),false).GetComponent<MeshFilter>().sharedMesh;
            // Its triangles run six metres: a hand clear of the floor, or the floor's own swells come through it in hard-edged blotches.
            var lying=dirt.vertices;for(int i=0;i<lying.Length;i++)lying[i].y=height(new Vector2(lying[i].x,lying[i].z))+.1f;dirt.vertices=lying;dirt.RecalculateBounds();
            foreach(var (key,x,z,size,yaw) in new[]{("HushwellFallenLamp",-30.6f,101.4f,.8f,35f),("HushwellFallenLamp",-26.2f,104.4f,.7f,160f),("LanternShard",-32.6f,98.2f,1.1f,200f),("HushwellRubble",-31.8f,103f,.7f,60f),("HushwellRubble",-27.6f,94.6f,.6f,130f),("HushwellShells",-33.4f,101.6f,.9f,10f),("DressSpoilHeap",-34.6f,96.8f,.7f,80f)})
                Prop(Kit(key,"HushwellRubble"),new Vector3(x,0,z),size,yaw,false).transform.SetParent(den.transform,true);
        }
        static void Gorge()
        {
            height=GorgeHeight;CliffTop=2.5f;fadeTall=true;
            var scene=WorldBuilder.Begin("LanternGorge");
            Terrain("LanternGorge","Lantern Gorge",-60,60,-24,250,.5f,Canyon(false),GorgeTone);
            WorldBuilder.Spawn("Arrival",new Vector3(0,0,0));WorldBuilder.Spawn("Relay",new Vector3(0,0,220));
            // Its sign is south of the arrival's view: any nearer, standing at the arrival it runs along the bottom of the screen under the skill bar.
            Gate("Climb to Lanternwick",new Vector3(-4,0,-8),"Lanternwick","Gorge",null,null,"HushwellScaffold",3,3.6f,new Vector2(.4f,-9));
            var rest=Prop("DeckConsole",new Vector3(6,0,8),1.4f,-60).AddComponent<RepairBay>();rest.free=true;rest.saveSpawn="Arrival";rest.prompt="Waystone — repair and save";
            Lamp(new Vector3(6,2,8),8,1.4f);
            // Fallen relay towers lie across the canyon: landmarks and cover.
            // The den's lies at the back of the den, where you see it from inside. On the camera's side of the way in it filled the lower half of the screen as you walked past it, and further back it was off the top of the screen from the den's own floor.
            foreach(var (x,z,yaw,size) in new[]{(14f,40f,30f,14f),(DenTower.x,DenTower.y,-6f,16f),(13f,140f,60f,13f),(-12f,170f,-40f,15f)})
            {
                // Lying along the canyon wall: scenery and a landmark, never a wall across the route.
                var fallen=Dark(Prop(Kit("LanternTower","HushwellColumn"),new Vector3(x,0,z),size,yaw,false),"LanternTower");fallen.name="Fallen relay tower";
                fallen.transform.rotation=Quaternion.Euler(0,yaw,84);var lying=ModelGeometry.BoundsOf(fallen);
                fallen.transform.position+=new Vector3(x,height(new Vector2(x,z))-.4f,z)-new Vector3(lying.center.x,lying.min.y,lying.center.z);
                if(x!=DenTower.x)SeatInWall(fallen,GorgeDistance,CliffRockClear,false);
                fallen.transform.position+=Vector3.down*.4f;
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
            var fightFloor=GorgeFloor();
            CliffFoot(gorgeCliffs,23,rock=>SeatInWall(rock,GorgeDistance,CliffRockClear)&&!HidesFloor(Drawn(rock),fightFloor));
            foreach(var v in verge)if(Prefab("Environment","DressBoulders")!=null&&Vector2.Distance(new Vector2(v.x,v.z),CampTent)>=TentClear)Prop("DressBoulders",v,1.2f,v.x*31,true);
            foreach(var (x,z,yaw) in RoadLanterns)
            {
                Dark(Prop(Kit("LanternTower","HushwellColumn"),new Vector3(x,0,z),2.6f,yaw,false),"LanternTower").name="Road lantern";
                Prop("HushwellRubble",new Vector3(x+.8f,0,z-.9f),.55f,yaw*3,false);
            }
            var grng=new System.Random(41);
            // Lantern shards in the crystals' slot, so the draws stay in step. That kit's crystals are flat fills of teal on its chart: the nearest prop at the arrival read as two untextured slabs on a mound.
            Scatter(grng,70,()=>new Vector2((float)grng.NextDouble()*60-30,(float)grng.NextDouble()*236-6),p=>{float d=GorgeDistance(p);return d>5.5f&&d<12.5f;},GorgeKeepsOff,
                ("HushwellRubble",1f,false),("DressSucculent",.9f,false),("DressFanShrub",1.2f,false),("LanternShard",1.1f,false),("LanternShard",1f,false),("DressBoulders",1f,true));
            // Relays to relight (the main quest's step), each in an open clearing.
            var gorgeRelays=new[]{(new Vector3(9,0,52),"gorge.relay1","SelaRelayLit"),(new Vector3(-9,0,124),"gorge.relay2",(string)null),(new Vector3(10,0,186),"gorge.relay3",(string)null)};
            int index=0;
            foreach(var (at,flag,node) in gorgeRelays)
            {
                index++;var relay=Relay("Gorge relay "+index,at+new Vector3(Mathf.Sign(at.x)*3,0,0),flag,11);
                Discovery("Relight gorge relay "+index,at,"Relight the relay",flag,"GorgeRelay",node,"Sela","LanternRoad",3,3.6f);
                // The tower's crown is off the top of the screen from where you relight it, and its light was too high to reach the floor: pressing A changed nothing you could see.
                // A small lantern at its foot wears the same glass and lights with it, and the light comes down to where it lays a pool.
                var foot=Prop(Kit("LanternTower","HushwellColumn"),at+new Vector3(Mathf.Sign(at.x)*2.2f,0,-2.4f),2.6f,0,false);foot.name="Relay foot lantern";
                foot.transform.SetParent(relay.transform,true);relay.crystals=relay.GetComponentsInChildren<Renderer>();
                relay.glow.transform.position=at+new Vector3(Mathf.Sign(at.x)*.8f,height(new Vector2(at.x,at.z))+3.2f,-1);relay.glow.intensity=6.5f;
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
            // A lamp with no fixture threw a pool beside the crates with nothing to throw it.
            Prop(Kit("DressWorkLamp","DressLightBollard"),new Vector3(-10,0,110),1.35f,20,false);Lamp(new Vector3(-10,2.6f,110),10,4);
            var rations=Discovery("Muster ration crate",new Vector3(-12,0,104),"Take a Muster ration crate","unsworn.rationsFound","MusterRations",null,"Sela","UnswornRations",0);
            // The one crate the quest means looked like every other black crate of the camp's: this one is a pale deck crate someone took from a station, with a light on it.
            rations.visual=Prop(Kit("DeckCrate","MusterCrate"),new Vector3(-12.6f,0,103.4f),.9f,25,false);rations.visual.AddComponent<PropTint>().tint=new Color(.92f,.84f,.72f);
            var rationsLight=new GameObject("Ration crate light",typeof(Light));rationsLight.transform.SetParent(rations.visual.transform,false);rationsLight.transform.localPosition=Vector3.up*1.6f;
            var rationsGlow=rationsLight.GetComponent<Light>();rationsGlow.type=LightType.Point;rationsGlow.color=new Color(1,.8f,.5f);rationsGlow.range=5;rationsGlow.intensity=2.5f;rationsGlow.shadows=LightShadows.None;
            rations.visual.SetActive(false);
            // The grazer den under the fallen tower holds Pell's lamp.
            var lamp=Discovery("Pell's lamp",Den.x*Vector3.right+Den.y*Vector3.forward,"Pick up the chewed lamp","pell.lampFound","PellsLamp",null,"Sela","PellsLamp",0);
            // Chewed and lost: on its side and unlit, not a clean street lamp standing in a den.
            lamp.visual=Dark(Prop(Kit("LampPost","DressLightBollard"),new Vector3(Den.x+.8f,0,Den.y+.5f),1.2f,80,false));
            lamp.visual.transform.rotation=Quaternion.Euler(10,80,82);var lampLies=ModelGeometry.BoundsOf(lamp.visual);
            lamp.visual.transform.position+=new Vector3(Den.x+.8f,height(new Vector2(Den.x+.8f,Den.y+.5f))+.02f,Den.y+.5f)-new Vector3(lampLies.center.x,lampLies.min.y,lampLies.center.z);
            lamp.visual.SetActive(false);
            GrazerDen();
            WorldBuilder.Label("SCAVENGER CAMP",new Vector3(-8,.2f,100));
            // The Sunken Relay's door at the head of the gorge opens once the three relays burn.
            WorldBuilder.Spawn("Relay",new Vector3(0,0,220));
            // The door is the arch itself now, standing against the head of the canyon. A prompt measures to the middle of its 8 m, so its reach is long.
            Gate("Enter the Sunken Relay",new Vector3(0,0,231.6f),"SunkenRelay","Arrival","gorge.relaysLit","The relay door is dark: relight the gorge relays","AncelaneArch",8,7.2f,new Vector2(4.6f,227.2f));
            // What it opens on: the way down, unlit. A wall panel with its colour and its lights taken out fills the arch from behind, and the floor runs into the same dark (GorgeTone).
            var doorway=WorldBuilder.Piece("DeckWall",new Vector3(0,height(new Vector2(0,233.3f))+3,233.3f),new Vector3(3.6f,6,.4f),"Rock",false);doorway.name="Relay doorway";
            Dark(doorway).AddComponent<PropTint>().tint=new Color(.035f,.035f,.05f);
            // The door is dark until the relays burn: it says so without a word.
            var doorLight=new GameObject("Relay door lamp",typeof(Light));doorLight.transform.position=new Vector3(0,height(new Vector2(0,229))+3.5f,227);
            var doorGlow=doorLight.GetComponent<Light>();doorGlow.type=LightType.Point;doorGlow.color=new Color(1,.78f,.45f);doorGlow.range=14;doorGlow.intensity=3;doorGlow.shadows=LightShadows.None;
            var doorLit=new GameObject("The relay door lights",typeof(FlagGate)).GetComponent<FlagGate>();doorLit.flag="gorge.relaysLit";doorLit.targets=new[]{doorLight};doorLight.SetActive(false);
            var anvil=Prop("LatticeAnvil",new Vector3(-6,0,212),1.8f,40).AddComponent<Bench>();anvil.prompt="Fabricate — Lattice Anvil";
            // The door is named by its arch's sign and its prompt. A floor sign saying it a third time, a few steps off, read as something left behind.
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
        /// <summary>The rock as the scatter has always read it. Its draws have to stay in step, so it keeps the old walls: a low west wall would refuse spots along it and move every piece after them.</summary>
        static float RelayHeight(Vector2 p)=>RelayRock(p,false);
        /// <summary>The camera stands south and west of the hero, so a wall on either side stands between them. lowWest brings the west side down with the south: at full height, a hero hugging a hall's
        /// west wall dragged the camera in to a close-up, Sela huge at the screen's edge and Hessa off it.</summary>
        static float RelayRock(Vector2 p,bool lowWest)
        {
            float inside=float.PositiveInfinity;bool cameraSide=false;
            foreach(var (centre,size,_) in RelayHalls)
            {
                var d=new Vector2(Mathf.Abs(p.x-centre.x)-size.x*.5f,Mathf.Abs(p.y-centre.y)-size.y*.5f);
                // Rounded rectangles: the outside distance, softened at the corners.
                float box=Mathf.Max(d.x,d.y)<0?Mathf.Max(d.x,d.y):new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude;
                if(box<inside){inside=box;cameraSide=p.y<centre.y-size.y*.5f||lowWest&&p.x<centre.x-size.x*.5f;}
            }
            // Just south of any hall's wall counts too: a corridor's flank past its door belongs to the corridor by distance, and stood full height between the camera and a hero hugging that wall. So does just west.
            foreach(var (centre,size,_) in RelayHalls)
            {
                float south=centre.y-size.y*.5f,west=centre.x-size.x*.5f;
                if(p.y<south&&p.y>south-3.5f&&Mathf.Abs(p.x-centre.x)<size.x*.5f+1){cameraSide=true;break;}
                if(lowWest&&p.x<west&&p.x>west-3.5f&&Mathf.Abs(p.y-centre.y)<size.y*.5f+1){cameraSide=true;break;}
            }
            // Rock between a hall and the camera stays low, or the camera slides in to a close-up of the hero's knees.
            float rock=Smooth(-.2f,1.3f,inside)*(5.2f+1.4f*Mathf.PerlinNoise(p.x*.08f+4,p.y*.08f))*(cameraSide?.36f:1);
            return rock+.06f*Mathf.PerlinNoise(p.x*.3f,p.y*.3f);
        }
        /// <summary>The relay's floor signs, for the scatter to keep off.</summary>
        static readonly (Vector2 at,float half)[] RelaySigns={(new(0,8),1.5f),(new(0,22.5f),1.9f),(new(0,46.5f),2),(new(0,86.5f),1.7f),(new(0,118.5f),2),(new(8,106.5f),1.8f),(new(-3.4f,-.2f),3.7f),(new(-2.2f,154.2f),3.4f)};
        /// <summary>Each hall its own colour of dark, one running into the next at its door.</summary>
        static Color RelayTone(Vector2 p)
        {
            Color[] halls={new(.9f,.9f,.94f),new(.7f,.7f,.76f),new(.78f,.86f,.94f),new(.84f,.76f,.7f),new(1,.92f,.8f)};float[] doors={20,44,84,116};
            var tone=halls[0];for(int i=0;i<doors.Length;i++)tone=Color.Lerp(tone,halls[i+1],Smooth(doors[i]-2,doors[i]+2,p.y));
            float wear=.9f+.1f*Mathf.PerlinNoise(p.x*.21f+5,p.y*.21f+2);
            return new Color(tone.r*wear,tone.g*wear,tone.b*wear,1);
        }
        /// <summary>The floor as it's built. Two halls only touch at their door, so the rock rose .3 m along the one row of the grid between them: a ridge across every doorway, which
        /// caught the light as a pale straight seam across the floor. RelayHeight stays as it was for the scatter, whose draws have to stay in step, as it does for the low west walls.</summary>
        static float RelayFloor(Vector2 p)
        {
            for(int i=1;i<RelayHalls.Length;i++)
            {
                var (near,nearSize,_)=RelayHalls[i-1];var (far,farSize,_)=RelayHalls[i];
                if(Mathf.Abs(p.y-(near.y+nearSize.y*.5f))<.3f&&Mathf.Abs(p.x)<Mathf.Min(nearSize.x,farSize.x)*.5f)return .06f*Mathf.PerlinNoise(p.x*.3f,p.y*.3f);
            }
            return RelayRock(p,true);
        }
        /// <summary>A lamp beside a wall lit that wall from a metre away, three and four times what the floor under it got: broad hot stripes up the strata, the brightest thing on the screen in a hall with no sky.
        /// Its light hangs out over the room, as far from its fixture as still reads as the fixture's. A corridor's walls are too close to get far from, so that's as far as it goes.</summary>
        static float OffTheWall(float x,float z)
        {
            foreach(var (centre,size,_) in RelayHalls)
            {
                if(Mathf.Abs(z-centre.y)>size.y*.5f)continue;
                float room=Mathf.Max(0,size.x*.5f-2.6f);
                return x+Mathf.Clamp(Mathf.Clamp(x,-room,room)-x,-1.3f,1.3f);
            }
            return x;
        }
        static void Relay()
        {
            // The low walls' tops are rock too, dark as the others. Under 2.4 m they counted as floor: a second, lit floor up on top of every low wall.
            height=RelayFloor;CliffTop=1.2f;fadeTall=true;
            var scene=WorldBuilder.Begin("SunkenRelay");
            // The strata's orange, muted at the foot too: it's a buried wall, however well lit.
            Terrain("SunkenRelay","Relay halls",-36,36,-14,180,.5f,Canyon(false),RelayTone,new Color(.4f,.4f,.5f),new Color(.76f,.7f,.72f));
            WorldBuilder.Spawn("Arrival",new Vector3(0,0,3));
            foreach(var (centre,size,label) in RelayHalls)WorldBuilder.Label(label,new Vector3(centre.x,.1f,Mathf.Max(8,centre.y-size.y*.5f+2.5f)),1.6f);
            // The way up is a scaffold against the wall, as at Hushwell's bore, beside the way in rather than between the camera and the party as it arrives.
            Gate("Climb out — Lantern Gorge",new Vector3(-6.6f,0,3),"LanternGorge","Relay",null,null,"HushwellScaffold",3,3.6f,new Vector2(-3.4f,-.2f));
            Discovery("Cut lantern housings",new Vector3(0,0,14),"Look at the cut lantern housings","relay.entered","","SelaSunkenRelay","Taren");
            Prop(Kit("MusterCutterRig","DressWorkLamp"),new Vector3(4.5f,0,15.5f),1.8f,150);
            // The columns that stood on the first two plates now stand back against the walls. The door's pair is set into the wall, on the floor: placed by the ground under
            // its middle, a column there stands on top of the rock. Square on, because a renderer's box grows as it turns, and that box is what crowded the plate.
            foreach(int side in new[]{-1,1})Prop(Kit("AncelaneColumn","HushwellColumn"),new Vector3(side*6.5f,0,20.4f),4.2f,0,true,false);
            foreach(var (x,z) in new[]{(-12f,50f),(12f,50f),(-12.5f,81.5f),(12.5f,81.5f),(-8f,90f),(8f,90f)})
            {Prop(Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,0,z),4.2f,x*z);}
            // Underground the light comes from what the scavengers brought in and what the station has left, so every pool has its lamp on the floor. A lamp's light falls off fast:
            // a source this high and this strong lays a wide pool with no edge, where a low weak one was a coin at its foot.
            var lamps=new[]{("DressWorkLamp",5.4f,.5f,1.35f),("DressWorkLamp",-7.4f,9.5f,1.35f),("DressWorkLamp",3.3f,24.5f,1.35f),("DressWorkLamp",-3.3f,37f,1.35f),("LampPost",-14.3f,52f,2f),("LampPost",14.3f,68f,2f),
                ("DressWorkLamp",9.8f,94.5f,1.35f),("DressWorkLamp",-9.8f,102f,1.35f),("DressWorkLamp",10f,107.6f,1.35f),("LampPost",-8f,128f,2f),("LampPost",8f,128f,2f),("LampPost",-15f,141f,2f),("LampPost",15f,143f,2f)};
            foreach(var (key,x,z,size) in lamps){Prop(Kit(key,"DressLightBollard"),new Vector3(x,0,z),size,0,false);Lamp(new Vector3(OffTheWall(x,z),3,z),14,6.5f);}
            // Each hall has a trade of its own. Nothing solid: all of it stands against the walls, off every route.
            foreach(var (key,x,z,size,yaw) in new[]{
                // Entry hall: where the cutting starts.
                ("LanternShard",7f,12.6f,1.5f,40f),("LanternShard",7.6f,17f,1.2f,200f),("MusterCrate",-7.6f,12.4f,1.1f,15f),("MusterLeanTo",5.8f,-4.5f,2.8f,-25f),
                // The cut corridor: cable and pipe stripped from the walls.
                ("DressCableDrum",-3.1f,25.5f,1f,90f),("DressPipeRun",3.4f,33f,.7f,90f),("DressPowerCell",-3.2f,41f,.8f,0f),
                // The fallen gallery: a row of dead mirrors.
                ("RelayMirror",13f,47.5f,1.6f,-60f),("RelayMirror",13.2f,55f,1.6f,-90f),("RelayMirror",13f,62.5f,1.6f,-120f),
                // The lower vault: emptied racks.
                ("DressShelfRack",-9.8f,96f,1.9f,90f),("DressShelfRack",9.8f,89.5f,1.9f,-90f),
                // The Great Lantern's floor: the mirrors that once answered it.
                ("RelayMirror",-16f,150f,1.8f,100f),("RelayMirror",16f,150f,1.8f,-100f),("RelayMirror",-13f,160f,1.8f,135f),("RelayMirror",13f,161f,1.8f,-135f)})
                Prop(Kit(key,"DeckCrate"),new Vector3(x,0,z),size,yaw,false);
            // And in the gallery, one of the towers themselves, felled.
            var felled=Dark(Prop(Kit("LanternTower","HushwellColumn"),new Vector3(-9.5f,0,70),10,20,false),"LanternTower");felled.name="Fallen relay tower";
            felled.transform.rotation=Quaternion.Euler(0,20,84);var felledAt=ModelGeometry.BoundsOf(felled);felled.transform.position+=new Vector3(-9.5f,-.3f,70)-new Vector3(felledAt.center.x,felledAt.min.y,felledAt.center.z);
            // Record plates for Ishen. Nothing the scatter puts down crowds them, or the ways out.
            var plates=new[]{new Vector3(-7,0,16),new Vector3(12,0,76),new Vector3(-9,0,108)};
            bool Crowded(Vector2 p)
            {
                foreach(var plate in plates)if(Vector2.Distance(p,new Vector2(plate.x,plate.z))<6.5f)return true;
                foreach(var (_,x,z,_) in lamps)if(Vector2.Distance(p,new Vector2(x,z))<2.5f)return true;
                if(OnASign(p,RelaySigns))return true;
                // The cradle's sign keeps the floor south of it to itself: a dark crate and a rusty cage lay just under the words on screen, and that's what a reader took for the cradle, 8 m short of the real one.
                return Vector2.Distance(p,new Vector2(-6.6f,3))<4.5f||Vector2.Distance(p,new Vector2(-6,160))<5||Vector2.Distance(p,new Vector2(8,105))<6.5f;
            }
            var rrng=new System.Random(53);
            Scatter(rrng,46,()=>new Vector2((float)rrng.NextDouble()*36-18,(float)rrng.NextDouble()*160),p=>RelayHeight(p)<.2f&&Mathf.Abs(p.x)>3.2f&&RelayHeight(p+Vector2.right*2.5f)+RelayHeight(p-Vector2.right*2.5f)>.5f,Crowded,
                ("HushwellRubble",1.1f,false),("LanternShard",1.1f,false),("CrystalClusterB",1f,false),("DressBoulders",1.1f,true),("HushwellStalagmites",1.6f,true));
            foreach(var (x,z,yaw) in new[]{(-11f,60f,80f),(11f,92f,-20f),(-15f,140f,30f)})
            {var fallen=Dark(Prop(Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,0,z),4f,yaw,false));fallen.transform.rotation=Quaternion.Euler(0,yaw,86);var b=ModelGeometry.BoundsOf(fallen);fallen.transform.position+=new Vector3(x,-.2f,z)-new Vector3(b.center.x,b.min.y,b.center.z);}
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
            // Well south of the cradle: it stood across the cradle's sign from the fixed camera. Drawn only, like the rest of this hall's trade: solid, with the column south of it and the wall east of it, it
            // made a pocket in the middle of a fight, and a scavenger backed into that pocket couldn't be reached by a blade or a shot.
            Prop(Kit("MusterCutterRig","DressWorkLamp"),new Vector3(9,0,97.5f),2.2f,200,false);
            // The checkpoint before the captain: under a minute from her floor.
            var cradle=Prop("DeckConsole",new Vector3(8,0,110),1.4f,-90).AddComponent<RepairBay>();cradle.free=true;cradle.saveSpawn="Vault";cradle.prompt="Relay cradle — repair and save";
            // The same bay as the drill's, with no light of its own: this floor is one renderer, well past its six. Its west post keeps off the spot the party stops at, and its pad stands clear of the floor's lumps.
            ChapterOneStaging.CradleBay("Relay cradle",cradle.gameObject,new Vector3(8,0,110),.07f,false,true);
            WorldBuilder.Spawn("Vault",new Vector3(4,0,108));
            WorldBuilder.Label("RELAY CRADLE",new Vector3(8,.1f,106.5f),1.6f);
            var anvil=Prop("LatticeAnvil",new Vector3(-8,0,112),1.8f,90).AddComponent<Bench>();anvil.prompt="Fabricate — Lattice Anvil";
            // The Great Lantern chamber: four columns survive every phase as cover against the flash.
            Prop(Kit("GreatLantern","CrystalClusterA"),new Vector3(0,0,158),5,0);
            foreach(var (x,z) in new[]{(-10f,132f),(10f,132f),(-10f,150f),(10f,150f)})Prop(Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,0,z),4.5f,x+z);
            Lamp(new Vector3(0,3.6f,155.2f),22,6.5f);
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
            // The lamps' colour, and well under a curtain's strength: this is what came on behind the party as Hessa's fight started, a smoky cloud across the floor in her own markings' orange, the brightest thing on the screen with no lamp in view.
            var sealBeam=CurtainMaterial("SunkenRelay");sealBeam.SetColor("_BaseColor",new Color(.72f,.72f,.72f,1));EditorUtility.SetDirty(sealBeam);var wave=new System.Random(83);
            for(float x=-11;x<=11;x+=1.1f)
            {
                float rise=3.1f+(float)wave.NextDouble()*.9f,lean=((float)wave.NextDouble()-.5f)*.35f,width=1.55f+(float)wave.NextDouble()*.5f;
                Strand(entry.transform,new Vector3(x,0,116.5f),new Vector3(x+lean,rise,116.5f),sealBeam,width,new Color(1,.76f,.45f,.62f),new Color(1,.76f,.45f,0));
            }
            foreach(var strand in entry.GetComponentsInChildren<LineRenderer>())strand.enabled=false;
            var duel=WorldBuilder.Encounter("Relay_Hessa",new Vector3(0,1,138),new Vector3(38,5,28),"HessaKorl",1,new[]{entry});
            // She is in the room for her own confrontation, at her work, and turns when the fight starts.
            var post=duel.GetComponentInChildren<Spawner>();post.transform.position=new Vector3(3,0,129);post.transform.rotation=Quaternion.Euler(0,180,0);post.faceAsPlaced=true;
            duel.requiredFlag="relay.hessaConfronted";duel.checkpointSpawn="Vault";
            var viewpoint=new GameObject("Hessa preview");viewpoint.transform.position=new Vector3(0,0,124);duel.previewPoint=viewpoint.transform;duel.previewRadius=6;
            confront.previewEncounter=duel;
            // After the duel: the captain, and the lift home. Once Aumi has been told, a later visit finds her under guard (YieldedCaptain decides as the scene loads).
            // Clear of the column at (10,150), which hid her from the fixed camera on every later visit.
            var hessa=Person("Hessa",new Vector3(5,0,153.5f),-120,"",false);hessa.firstNode="HessaRepeat";hessa.repeatNode="HessaRepeat";
            var yielded=new GameObject("Hessa under guard",typeof(YieldedCaptain)).GetComponent<YieldedCaptain>();yielded.npc=hessa;hessa.gameObject.SetActive(false);
            yielded.guards=new[]{Guard(hessa.transform,1.7f,"AumiBody","Neve"),Guard(hessa.transform,-1.7f,"PellBody","SurvivorNatural")};
            // A real lift, its lamp dark until it will run. A prompt measures to the middle of the cage, so its reach is long enough for the floor in front of it.
            // East of the column at (-10,150), which stood across its words from the camera.
            Gate("Ride the relay lift — Lanternwick",new Vector3(-6,0,160),"Lanternwick","Archive","relay.hessaYielded","The lift waits: hear the captain out first","HushwellLift",4,4.4f,new Vector2(-2.2f,154.2f));
            var liftLamp=new GameObject("Relay lift lamp",typeof(Light));liftLamp.transform.position=new Vector3(-6,height(new Vector2(-6,158))+3.2f,158);
            var liftGlow=liftLamp.GetComponent<Light>();liftGlow.type=LightType.Point;liftGlow.color=new Color(1,.78f,.45f);liftGlow.range=12;liftGlow.intensity=5;liftGlow.shadows=LightShadows.None;
            var liftLit=new GameObject("The relay lift lights",typeof(FlagGate)).GetComponent<FlagGate>();liftLit.flag="relay.hessaYielded";liftLit.targets=new[]{liftLamp};liftLamp.SetActive(false);
            BakeNavigation("SunkenRelay",new Bounds(new Vector3(0,4,82),new Vector3(50,20,186)),new Vector3(0,0,3),
                new[]{new Vector3(0,0,30),new Vector3(10,0,74),new Vector3(-8,0,108),new Vector3(0,0,118),new Vector3(0,0,140)});
            WorldBuilder.Save(scene,"SunkenRelay");Debug.Log("SUNKEN_RELAY_OK");
        }

        /// <summary>A Lanternwick keeper standing over a prisoner, turned toward her: a body with no name and nothing to say. Hidden until the scene decides, and tinted then.</summary>
        static GameObject Guard(Transform prisoner,float side,string body,string fallback)
        {
            var at=prisoner.position+prisoner.right*side-prisoner.forward*.35f;at.y=height(new Vector2(at.x,at.z));
            var guard=new GameObject("Keeper on guard");var toward=prisoner.position-at;toward.y=0;guard.transform.SetPositionAndRotation(at,Quaternion.LookRotation(toward));
            var model=(GameObject)PrefabUtility.InstantiatePrefab(Body("Characters",body,"Characters",fallback));model.transform.SetParent(guard.transform,false);model.name="Generated civilian";
            var solid=guard.AddComponent<CapsuleCollider>();solid.height=1.9f;solid.radius=.35f;solid.center=Vector3.up*.95f;
            guard.SetActive(false);return guard;
        }
        /// <summary>An old relay mirror on its stand, there on every visit, and resting against it the round plate Ishen wants, under a small light of
        /// its own so it never reads as the rubble. Only the plate and its light belong to the quest: taking it leaves the stand. It leans on the
        /// camera's side, tipped toward the camera: lying flat on top, a disc this size is a sliver from up there.</summary>
        static GameObject RecordPlate(Vector3 at,float yaw)
        {
            var stand=Prop(Kit("RelayMirror","LanternShard"),at,1.25f,yaw);stand.name="Record plate stand";var foot=ModelGeometry.BoundsOf(stand);
            var plate=Prop("LandingPad",Vector3.zero,.56f,0,false,false);plate.name="Record plate body";plate.transform.rotation=Quaternion.Euler(-48,0,0);var lying=ModelGeometry.BoundsOf(plate);
            plate.transform.position+=new Vector3(foot.center.x,foot.min.y+.01f,foot.min.z-.12f)-new Vector3(lying.center.x,lying.min.y,lying.center.z);
            var light=new GameObject("Plate light",typeof(Light));light.transform.SetParent(plate.transform,false);light.transform.position=new Vector3(foot.center.x,foot.min.y+1.7f,foot.center.z-.3f);
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
