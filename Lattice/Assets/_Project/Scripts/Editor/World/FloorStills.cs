using System;
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
    /// <summary>D393's stills: the floors and water of round four drawn from the cameras the November 23 capture had (frames.csv of nov23-whole), with the live pipeline and the
    /// licensed water and grass put in as the player puts them in, Taren where the hero stood. The Walking Canopy's basin is drawn drained, as the capture saw it after the
    /// Floodcrown (the scene is built full); the Terminator's dawn line is stood by a first draw, as TerminatorDawnStills does. Nothing is saved. Run:
    /// exec-when-green.ps1 -Method Lattice.EditorTools.FloorStills.Render -Marker FLOOR_STILLS_OK -Graphics -ExtraArgs @('-look-run','name','-still-zones','WalkingCanopy,FrozenHost').</summary>
    public static class FloorStills
    {
        static readonly (string zone,string name,Vector3 hero,Vector3 eye)[] Shots=
        {
            ("WalkingCanopy","s21-280",new Vector3(14.85f,-5.44f,42.48f),new Vector3(8.31f,11.48f,23.82f)),
            ("WalkingCanopy","s21-309",new Vector3(14.85f,-5.44f,42.48f),new Vector3(9.85f,7.12f,28.06f)),
            ("FrozenHost","s35-420",new Vector3(170.6f,-17.42f,218.1f),new Vector3(165.09f,-4.88f,204.08f)),
            ("FrozenHost","s36-076",new Vector3(213.78f,-17.42f,224.91f),new Vector3(207.69f,-3.19f,209.05f)),
            ("Driftreed","s24-112",new Vector3(-15.57f,.09f,46f),new Vector3(-19.86f,12.61f,31.96f)),
            ("Driftreed","s24-176",new Vector3(37.06f,.09f,66.9f),new Vector3(31.3f,12.62f,53.35f)),
            ("Terminator","s55-152",new Vector3(0,.04f,740),new Vector3(-5.11f,12.58f,725.96f)),
            ("Terminator","s56-059",new Vector3(-.86f,.08f,162.38f),new Vector3(-6.39f,12.73f,148.07f)),
        };

        public static void Render()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-look-run")??throw new ArgumentException("-look-run <name> is required");
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/look",run));
            if(System.IO.Directory.Exists(folder))throw new InvalidOperationException("Preserve earlier look run: "+folder);
            System.IO.Directory.CreateDirectory(folder);
            var zones=(DevArgs.Value("-still-zones")??string.Join(",",Shots.Select(s=>s.zone).Distinct())).Split(',',StringSplitOptions.RemoveEmptyEntries);
            var before=GraphicsSettings.defaultRenderPipeline;var beforeQuality=QualitySettings.renderPipeline;
            var pipeline=Resources.Load<RenderPipelineAsset>("LocalBiomes/LocalPipeline")??throw new InvalidOperationException("FLOOR_STILLS the live pipeline is missing");
            var profile=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Ground.asset");
            int count=0;
            try
            {
                GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                foreach(var zone in zones)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
                    foreach(var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
                    int local=0;foreach(var patch in UnityEngine.Object.FindObjectsByType<BiomePatch>(FindObjectsSortMode.None))if(patch.ApplyLocalArt())local++;
                    var def=AssetDatabase.FindAssets("t:ZoneDef").Select(g=>AssetDatabase.LoadAssetAtPath<ZoneDef>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(d=>d.scene==zone||d.id==zone);
                    if(def!=null)RenderSettings.skybox=def.skybox;
                    // The basin as the party walks it after the fight: the water down at its empty level and its deep blocks gone.
                    foreach(var basin in UnityEngine.Object.FindObjectsByType<FloodBasin>(FindObjectsSortMode.None))if(basin.water!=null)Lower(basin.water,basin.emptyLevel);
                    foreach(var shot in Shots.Where(s=>s.zone==zone))
                    {
                        var taren=(GameObject)PrefabUtility.InstantiatePrefab(ZoneKit.Prefab("Characters","TarenNatural"));
                        var rig=taren.GetComponentInChildren<Animator>();var idle=rig!=null&&rig.runtimeAnimatorController!=null?rig.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle")):null;
                        taren.transform.SetPositionAndRotation(shot.hero,Quaternion.Euler(0,200,0));if(idle!=null)idle.SampleAnimation(rig.gameObject,.3f);
                        var camera=new GameObject("Floor still camera",typeof(Camera),typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData)).GetComponent<Camera>();
                        camera.clearFlags=RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.06f);
                        camera.nearClipPlane=.3f;camera.farClipPlane=1500;camera.fieldOfView=profile.fov;
                        var data=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;
                        camera.transform.SetPositionAndRotation(shot.eye,Quaternion.Euler(profile.pitch,profile.yaw,0));
                        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();camera.Render();
                        var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,zone+"-"+shot.name+".png"),image.EncodeToPNG());
                        RenderTexture.active=prior;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
                        UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(taren);count++;
                        Debug.Log($"FLOOR_STILL {zone} {shot.name} local={local}");
                    }
                }
                Debug.Log($"FLOOR_STILLS_OK count={count} folder={folder}");
            }
            finally{GraphicsSettings.defaultRenderPipeline=before;QualitySettings.renderPipeline=beforeQuality;}
        });

        /// <summary>A water stood at another level in memory (XylosBuilder.StillLevel's way): the surface moved, its depth uv rewritten, its deep blocks gone if it is down.</summary>
        static void Lower(WaterVolume water,float level)
        {
            if(water.surface==null)return;
            water.surface.position=new Vector3(water.surface.position.x,level,water.surface.position.z);if(level<water.level)foreach(var block in water.deep)if(block!=null)block.SetActive(false);
            var filter=water.surface.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)return;
            var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);var depth=new List<Vector2>();mesh.GetUVs(1,depth);
            for(int i=0;i<depth.Count;i++)depth[i]=new Vector2(depth[i].x,level-depth[i].x);mesh.SetUVs(1,depth);filter.sharedMesh=mesh;
        }
    }
}
