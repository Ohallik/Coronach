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
        public static void BuildAll()=>BatchTools.Run(()=>
        {
            Prepare();CinderHaloBuilder.BuildZone();DecksBuilder.BuildZone();SorrelBuilder.BuildZone();GulletBuilder.BuildZone();TallowDriftBuilder.BuildZone();
            BatchTools.RegisterScenes();AssetDatabase.SaveAssets();Debug.Log("WORLD_BLOCKOUT_OK");
        });
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
            var volume=new GameObject("ZoneLook",typeof(Volume)).GetComponent<Volume>();volume.isGlobal=true;
            string vp="Assets/_Project/Settings/"+name+"Volume.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(vp);
            if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,vp);}
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}
            if(!profile.TryGet<Vignette>(out var vignette)){vignette=profile.Add<Vignette>();AssetDatabase.AddObjectToAsset(vignette,profile);}
            bloom.intensity.Override(name=="Gullet_Tunnel"?.9f:.4f);bloom.threshold.Override(1.2f);vignette.intensity.Override(.2f);
            EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(vignette);EditorUtility.SetDirty(profile);volume.sharedProfile=profile;
            return scene;
        }
        public static void Save(Scene scene)=>EditorSceneManager.SaveScene(scene,"Assets/_Project/Scenes/"+scene.name+".unity");
        public static void Save(Scene scene,string name)=>EditorSceneManager.SaveScene(scene,"Assets/_Project/Scenes/"+name+".unity");
        public static GameObject Piece(string key,Vector3 pos,Vector3 size,string material="Rock",bool solid=true)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/"+key+".prefab");
            GameObject go;
            if(prefab!=null)
            {
                go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.position=pos;
                var renderers=go.GetComponentsInChildren<Renderer>();var bounds=renderers.Length>0?renderers[0].bounds:new Bounds(pos,Vector3.one);foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(size.x/Mathf.Max(.01f,bounds.size.x),size.y/Mathf.Max(.01f,bounds.size.y),size.z/Mathf.Max(.01f,bounds.size.z)));
            }
            else {go=ArenaBuilder.Block("ART_PENDING_"+key,pos,size,material);WorldArt.Dress(go,key,size);}
            go.name=key;if(!solid)foreach(var collider in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
            return go;
        }
        public static SpawnPoint Spawn(string id,Vector3 position)
        {var go=new GameObject("Spawn_"+id,typeof(SpawnPoint));go.transform.position=position;go.GetComponent<SpawnPoint>().id=id;return go.GetComponent<SpawnPoint>();}
        public static void Label(string text,Vector3 position,float size=2)
        {
            var go=new GameObject("Sign_"+text,typeof(TextMeshPro));go.transform.position=position;go.transform.rotation=Quaternion.Euler(60,0,0);
            var label=go.GetComponent<TextMeshPro>();label.text=text;label.font=TMP_Settings.defaultFontAsset;label.fontSize=size;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.75f,.92f,1);label.rectTransform.sizeDelta=new Vector2(22,4);
        }
        public static Npc Npc(string id,Vector3 position,string first=null)
        {
            var go=new GameObject(id);go.transform.position=position;
            var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="ART_PENDING_Civilian";body.transform.SetParent(go.transform,false);body.transform.localPosition=Vector3.up*.95f;body.transform.localScale=new Vector3(.75f,.95f,.75f);body.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Blockout/"+(id=="Mira"||id=="Neve"||id=="Sela"?"Sela":"Taren"));
            var npc=go.AddComponent<Npc>();npc.speaker=id;npc.firstNode=first??id+"First";npc.repeatNode=id+"Repeat";npc.postNode=id+"Post";npc.prompt="Talk to "+id;npc.range=3.2f;
            go.AddComponent<NpcVisual>().character=id;Label(id,position+new Vector3(0,2.8f,0),1.5f);return npc;
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
            var spawner=spawn.GetComponent<Spawner>();spawner.definition=GameCatalog.Find<EnemyDef>(enemy);spawner.count=count;spawner.radius=count>1?3:0;
            var e=go.GetComponent<EncounterVolume>();e.encounterId=id;e.spawners=new[]{spawner};e.membranes=membranes??Array.Empty<Membrane>();return e;
        }
        public static Membrane Membrane(string id,Vector3 position,float width,bool open)
        {
            var go=Piece("GulletMembrane",position,new Vector3(width,6,.6f),"Emission");go.name=id;var membrane=go.AddComponent<Membrane>();membrane.SetOpen(open);return membrane;
        }
    }
}
