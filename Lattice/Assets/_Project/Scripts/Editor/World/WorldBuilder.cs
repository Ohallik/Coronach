using System;
using System.IO;
using Lattice.Core;
using Lattice.Data;
using Lattice.Combat;
using Lattice.UI;
using Lattice.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace Lattice.EditorTools
{
    public static class WorldBuilder
    {
        static bool requireGenerated;
        public static void BuildAll()=>BatchTools.Run(()=>Build(false));
        public static void BuildFinal()=>BatchTools.Run(()=>Build(true));
        static void Build(bool final)
        {
            requireGenerated=final;
            Prepare();CinderHaloBuilder.BuildZone();DecksBuilder.BuildZone();SorrelBuilder.BuildZone();GulletBuilder.BuildZone();TallowDriftBuilder.BuildZone();
            if(final)ArenaBuilder.BuildGeneratedArenas();
            BatchTools.RegisterScenes();AssetDatabase.SaveAssets();Debug.Log(final?"WORLD_GENERATED_OK":"WORLD_BLOCKOUT_OK");
        }
        public static void Prepare()
        {
            PipelineConverter.ConvertTo3DInternal();ArenaBuilder.Definitions();ArenaBuilder.Materials();
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            foreach(var file in Directory.GetFiles(Path.Combine(root,"docs/writing"),"*.yarn"))File.Copy(file,"Assets/_Project/Resources/Dialogue/"+Path.GetFileName(file),true);
            Directory.CreateDirectory("Assets/_Project/Resources/Audio");File.Copy(Path.Combine(root,"docs/writing/barks.json"),"Assets/_Project/Resources/Audio/barks.json",true);
            foreach(string clip in new[]{"laserSmall_000","impactMetal_000","forceField_000","explosionCrunch_000","thrusterFire_000","doorOpen_000","spaceEngineLow_000","computerNoise_000"})
                PackStaging.StageFile("art-src/Kenney_SciFiSounds/Audio/"+clip+".ogg","Resources/Audio/SFX/"+clip+".ogg");
            PackStaging.StageFile("art-src/Kenney_SciFiSounds/License.txt","Audio/Licenses/Kenney-SciFiSounds.txt");
            foreach(var name in new[]{"Orrin","Mira","Hal","Neve","Survivor","Keeper"})ArenaBuilder.Asset<CharacterDef>(name,c=>{c.id=c.speakerId=c.displayName=name;});
            foreach(var pair in new[]{("Survivor","Orrin"),("Keeper","Hal")})
            {
                var source=GameCatalog.Find<CharacterDef>(pair.Item2);
                ArenaBuilder.Asset<CharacterDef>(pair.Item1,c=>{c.natural=source.natural;c.portraitNatural=source.portraitNatural;});
            }
            ArenaBuilder.Asset<ConsumableDef>("ChargeCell",d=>{d.id="ChargeCell";d.displayName="Charge cell";d.heal=0;d.charge=50;});
            ArenaBuilder.Asset<RecipeDef>("ChargeCell",r=>{r.id=r.displayName="ChargeCell";r.consumableOutput="ChargeCell";r.inputs=new[]{new Ingredient{id="LatticeFilament",count=2}};});
            AssetDatabase.Refresh();
            ParticleIntake.Prepare();
            WorldArt.Prepare();
            AssetDatabase.ImportAsset("Assets/_Project/Resources/Dialogue/Lattice.yarnproject",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            var yarn=Resources.Load<Yarn.Unity.YarnProject>("Dialogue/Lattice");
            if(yarn==null||!yarn.Program.Nodes.ContainsKey("OrrinFirst"))throw new Exception("Yarn project did not compile the route nodes");
        }
        public static Scene Begin(string name)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("ZoneRoot",typeof(ZoneController),typeof(ArenaRuntime));
            root.GetComponent<ZoneController>().definition=GameCatalog.Find<ZoneDef>(name);root.GetComponent<ArenaRuntime>().spawnArenaEnemies=false;
            var light=new GameObject("KeyLight",typeof(Light));light.transform.rotation=Quaternion.Euler(48,-35,0);light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=name=="Gullet_Tunnel"?.8f:1.3f;light.GetComponent<Light>().shadows=LightShadows.Soft;
            RenderSettings.ambientLight=name=="Gullet_Tunnel"?new Color(.12f,.22f,.32f):new Color(.3f,.34f,.45f);
            RenderSettings.ambientMode=AmbientMode.Flat;
            if(name=="Hub_Decks"||name=="TallowDrift")light.GetComponent<Light>().intensity=1.15f;
            var volume=new GameObject("ZoneLook",typeof(Volume)).GetComponent<Volume>();volume.isGlobal=true;
            string vp="Assets/_Project/Settings/"+name+"Volume.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(vp);
            if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,vp);}
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}
            if(!profile.TryGet<Vignette>(out var vignette)){vignette=profile.Add<Vignette>();AssetDatabase.AddObjectToAsset(vignette,profile);}
            var look=root.GetComponent<ZoneController>().definition.hd2dProfile;bloom.intensity.Override(name=="Gullet_Tunnel"?.9f:look.bloom);bloom.threshold.Override(1.2f);vignette.intensity.Override(.17f);
            EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(vignette);EditorUtility.SetDirty(profile);volume.sharedProfile=profile;
            return scene;
        }
        public static void Save(Scene scene)=>EditorSceneManager.SaveScene(scene,"Assets/_Project/Scenes/"+scene.name+".unity");
        public static void Save(Scene scene,string name)=>EditorSceneManager.SaveScene(scene,"Assets/_Project/Scenes/"+name+".unity");
        public static GameObject Piece(string key,Vector3 pos,Vector3 size,string material="Rock",bool solid=true)
        {
            if(key=="DeckFloor"&&(size.x>9||size.z>9))return FloorTiles(pos,size,solid);
            if(key=="DeckWall"&&Mathf.Max(size.x,size.z)>9)return WallTiles(pos,size,solid);
            string asset=key switch{"OutpostConsole" or "RepairConsole"=>"DeckConsole","WarpKeyCradle"=>"LatticeAnvil","NeveCache"=>"DeckCrate","DockingPodShop"=>"DockingPodOutfitter",_=>key};
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/"+asset+".prefab")??AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Ships/"+asset+".prefab");
            GameObject go;
            if(prefab!=null)
            {
                // Wrapper uses the builder's centre-position contract. Intake prefabs are feet-origin.
                go=new GameObject(key);var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.transform.SetParent(go.transform,false);
                if(key.StartsWith("GulletWall"))body.transform.localRotation=Quaternion.Euler(0,90,0);
                var bounds=ModelGeometry.BoundsOf(body);Vector3 scale;
                if(key=="DeckFloor"||key=="DeckWall")
                {
                    if(key=="DeckWall"&&size.z>size.x){body.transform.localRotation=Quaternion.Euler(0,90,0);bounds=ModelGeometry.BoundsOf(body);}
                    scale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);
                    if(key=="DeckWall"&&size.z>size.x)scale=new Vector3(scale.z,scale.y,scale.x);
                }
                else
                {
                    float factor=key=="Hauler"||key=="Skiff"||key=="PatrolCutter"?size.z/bounds.size.z:
                        key.StartsWith("RingSegment")||key=="LandingPad"||key=="TallowStationHull"||key.StartsWith("MoonGround")?Mathf.Min(size.x/bounds.size.x,size.z/bounds.size.z):size.y/bounds.size.y;
                    scale=Vector3.one*factor;
                }
                body.transform.localScale=Vector3.Scale(body.transform.localScale,scale);bounds=ModelGeometry.BoundsOf(body);
                body.transform.localPosition-=bounds.center;go.transform.position=pos;
                if(key=="DeckFloor")
                {
                    // Preserve generated panel detail at human scale with a quiet steel floor value.
                    foreach(var renderer in body.GetComponentsInChildren<Renderer>())
                    {
                        var source=renderer.sharedMaterial;if(source==null)continue;
                        string path="Assets/_Project/Resources/WorldMaterials/deck-floor-muted.mat";
                        var muted=AssetDatabase.LoadAssetAtPath<Material>(path);
                        if(muted==null){muted=new Material(source);AssetDatabase.CreateAsset(muted,path);}
                        muted.SetColor("_BaseColor",new Color(.38f,.45f,.52f));muted.SetColor("_EmissionColor",new Color(.04f,.15f,.17f));
                        EditorUtility.SetDirty(muted);renderer.sharedMaterial=muted;
                    }
                }
                if(solid)
                {
                    if(key=="DeckDoorway")foreach(var filter in body.GetComponentsInChildren<MeshFilter>())filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
                    else go.AddComponent<BoxCollider>().size=bounds.size;
                }
            }
            else
            {
                if(requireGenerated&&key!="GulletMembrane")throw new InvalidOperationException("Final world requires generated prefab "+key+" ("+asset+")");
                go=ArenaBuilder.Block("ART_PENDING_"+key,pos,size,material);WorldArt.Dress(go,key,size);
            }
            go.name=key;go.isStatic=solid;if(!solid)foreach(var collider in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
            return go;
        }
        static GameObject FloorTiles(Vector3 pos,Vector3 size,bool solid)
        {
            var root=new GameObject("Deck floor modules");int nx=Mathf.CeilToInt(size.x/4),nz=Mathf.CeilToInt(size.z/4);float x=size.x/nx,z=size.z/nz;
            for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)Piece("DeckFloor",pos+new Vector3((i+.5f)*x-size.x*.5f,0,(j+.5f)*z-size.z*.5f),new Vector3(x,size.y,z),"Ground",solid).transform.SetParent(root.transform,true);
            return root;
        }
        static GameObject WallTiles(Vector3 pos,Vector3 size,bool solid)
        {
            var root=new GameObject("Deck wall modules");bool alongX=size.x>size.z;float length=alongX?size.x:size.z;int count=Mathf.CeilToInt(length/5);float span=length/count;
            for(int i=0;i<count;i++){var extent=alongX?new Vector3(span,size.y,size.z):new Vector3(size.x,size.y,span);Piece("DeckWall",pos+(alongX?Vector3.right:Vector3.forward)*((i+.5f)*span-length*.5f),extent,"Rock",solid).transform.SetParent(root.transform,true);}
            return root;
        }
        public static SpawnPoint Spawn(string id,Vector3 position)
        {var go=new GameObject("Spawn_"+id,typeof(SpawnPoint));go.transform.position=position;go.GetComponent<SpawnPoint>().id=id;return go.GetComponent<SpawnPoint>();}
        public static void Boundary(string name,Vector3 position,Vector3 size)
        {var go=new GameObject(name,typeof(BoxCollider));go.transform.position=position;go.GetComponent<BoxCollider>().size=size;go.isStatic=true;}
        public static GameObject Label(string text,Vector3 position,float size=2)
        {
            var go=new GameObject("Sign_"+text,typeof(TextMeshPro));go.transform.position=position;go.transform.rotation=Quaternion.Euler(60,0,0);
            var label=go.GetComponent<TextMeshPro>();label.text=text;label.font=TMP_Settings.defaultFontAsset;label.fontSize=size;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.75f,.92f,1);label.rectTransform.sizeDelta=new Vector2(22,4);
            return go;
        }
        public static Npc Npc(string id,Vector3 position,string first=null)
        {
            var go=new GameObject(id);go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,180,0);
            var definition=GameCatalog.Find<CharacterDef>(id);
            if(definition!=null&&definition.natural!=null){var body=(GameObject)PrefabUtility.InstantiatePrefab(definition.natural);body.transform.SetParent(go.transform,false);body.name="Generated civilian";}
            else {if(requireGenerated)throw new InvalidOperationException("Final world requires generated civilian "+id);var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="ART_PENDING_Civilian";body.transform.SetParent(go.transform,false);body.transform.localPosition=Vector3.up*.95f;body.transform.localScale=new Vector3(.75f,.95f,.75f);}
            var collider=go.AddComponent<CapsuleCollider>();collider.height=1.9f;collider.radius=.35f;collider.center=Vector3.up*.95f;
            var npc=go.AddComponent<Npc>();npc.speaker=id;npc.firstNode=first??id+"First";npc.repeatNode=id+"Repeat";npc.postNode=id+"Post";npc.prompt="Talk to "+id;npc.range=3.2f;
            Label(id,position+new Vector3(0,2.8f,0),1.5f).transform.SetParent(go.transform,true);return npc;
        }
        public static DockingPad Dock(string name,Vector3 position,string scene,string spawn)
        {
            var go=Piece("LandingPad",position-Vector3.up*.3f,new Vector3(5,.3f,5),"Sela");go.name=name;
            var dock=go.AddComponent<DockingPad>();dock.prompt=name;dock.range=4;dock.scene=scene;dock.spawn=spawn;return dock;
        }
        public static WarpBeacon Warp(string name,Vector3 position,string scene,string flag="warpkey",string spawn="Arrival")
        {
            var go=Piece("WarpBeacon",position,new Vector3(2,3,2),"Emission");go.name=name;
            var warp=go.AddComponent<WarpBeacon>();warp.prompt=name;warp.range=5;warp.scene=scene;warp.requiredFlag=flag;warp.spawn=spawn;Label(name,position+Vector3.up*4,1.5f);return warp;
        }
        public static EncounterVolume Encounter(string id,Vector3 center,Vector3 triggerSize,string enemy,int count,Membrane[] membranes=null)
        {
            var go=new GameObject(id,typeof(BoxCollider),typeof(EncounterVolume));go.transform.position=center;
            go.GetComponent<BoxCollider>().isTrigger=true;go.GetComponent<BoxCollider>().size=triggerSize;
            var spawn=new GameObject("Spawn_"+enemy,typeof(Spawner));spawn.transform.SetParent(go.transform,false);spawn.transform.localPosition=Vector3.forward*5;
            if(enemy=="Ridgehound"||enemy=="Scrapmite"||enemy=="SentinelHusk"||enemy=="Burrower")spawn.transform.position=new Vector3(spawn.transform.position.x,0,spawn.transform.position.z);
            var spawner=spawn.GetComponent<Spawner>();spawner.definition=GameCatalog.Find<EnemyDef>(enemy);spawner.count=count;spawner.radius=count>1?3:0;
            var e=go.GetComponent<EncounterVolume>();e.encounterId=id;e.spawners=new[]{spawner};e.membranes=membranes??Array.Empty<Membrane>();return e;
        }
        public static Membrane Membrane(string id,Vector3 position,float width,bool open)
        {
            var go=Piece("GulletMembrane",position,new Vector3(width,6,.6f),"Emission");go.name=id;var membrane=go.AddComponent<Membrane>();membrane.SetOpen(open);return membrane;
        }
    }
}
