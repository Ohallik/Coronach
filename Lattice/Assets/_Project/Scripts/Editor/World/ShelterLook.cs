using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Data;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    /// <summary>D415: Idra's Shelter drawn where the capture of November 25 found it (nov24-whole s46/128, the Scoria battery), through the game's ground camera from where that still's camera
    /// stood, at 1920x1080: the dome as D387 left it, then the ring and low rim at its reach, rising, standing and sinking. Read-only: never saves a scene or an asset. Marker SHELTER_LOOK_OK.</summary>
    public static class ShelterLook
    {
        /// <summary>From s46/128's frame: the lead, Idra and the camera; the Shelter stands between the two heroes (IdraSkills.Between).</summary>
        static readonly Vector3 Lead=new(-.12f,0,188.4f),Partner=new(2.39f,0,183.5f),Eye=new(-6.73f,18.54f,169.85f);
        public static void Render()=>BatchTools.Run(()=>
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/look/d415"));
            if(Directory.Exists(folder)&&Directory.GetFiles(folder).Length>0)throw new System.InvalidOperationException("Preserve earlier renders: "+folder);
            Directory.CreateDirectory(folder);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/ScoriaFlows.unity");
            foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
            foreach(var animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))Idle(animator);
            var def=AssetDatabase.FindAssets("t:ZoneDef").Select(g=>AssetDatabase.LoadAssetAtPath<ZoneDef>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(d=>d.scene=="ScoriaFlows"||d.id=="ScoriaFlows");
            if(def!=null)RenderSettings.skybox=def.skybox;
            Physics.SyncTransforms();
            var lead=Hero("TarenNatural",Lead,Partner);var partner=Hero("IdraNatural",Partner,Lead);
            var at=Ground((lead.transform.position+partner.transform.position)*.5f);
            var profile=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Ground.asset");
            var camera=new GameObject("Look camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.clearFlags=RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.06f);
            camera.nearClipPlane=.3f;camera.farClipPlane=1500;camera.fieldOfView=profile.fov;
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.None;
            camera.transform.SetPositionAndRotation(Eye,Quaternion.Euler(profile.pitch,profile.yaw,0));
            // Before: D387's dome, its sphere sized from the mesh's bounds to the reach in its own glass, over the ring a skill ring drew at a second old.
            var before=new GameObject("Shelter (as D387)");before.transform.position=at;
            var sphere=Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            var shell=new GameObject("Shell",typeof(MeshFilter),typeof(MeshRenderer));shell.transform.SetParent(before.transform,false);shell.transform.localScale=Vector3.one*ShelterDome.Radius/sphere.bounds.extents.x;
            shell.GetComponent<MeshFilter>().sharedMesh=sphere;
            var glass=new Material(ActorFactory.Energy(new Color(1f,.84f,.55f,.55f))){name="Shelter glass (as D387)"};glass.SetFloat("_Core",.03f);glass.SetFloat("_Sharpness",3.5f);glass.SetFloat("_Intensity",.6f);
            var view=shell.GetComponent<MeshRenderer>();view.sharedMaterial=glass;view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view.receiveShadows=false;
            var edge=new GameObject("Edge");edge.transform.SetParent(before.transform,false);var ring=edge.AddComponent<GroundRing>();ring.Initialize(ShelterDome.Radius+.5f);ring.Draw(ShelterDome.Radius,GroundRing.Friendly);
            Shot(camera,Path.Combine(folder,"before-s46-128.png"));
            before.SetActive(false);
            // After: the ring and the rim, rising, standing and sinking.
            var dome=ShelterDome.Stand(at,false);
            foreach(var (name,since) in new[]{("after-rising",.1f),("after-standing",1f),("after-sinking",4.8f)}){dome.Pose(since);Shot(camera,Path.Combine(folder,name+"-s46-128.png"));}
            Debug.Log($"SHELTER_LOOK_OK folder={folder} at={at} rim={dome.DrawnRadius:0.00} stands={ShelterDome.RimHeight}");
        });
        static void Idle(Animator animator)
        {
            if(animator.GetComponentInParent<Lattice.World.Posed>()!=null)return;
            var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];
            var idle=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle"));if(idle!=null)idle.SampleAnimation(animator.gameObject,.3f);
        }
        static Vector3 Ground(Vector3 at)=>Physics.Raycast(new Vector3(at.x,at.y+30,at.z),Vector3.down,out var hit,80,~0,QueryTriggerInteraction.Ignore)?hit.point:at;
        static GameObject Hero(string prefab,Vector3 at,Vector3 facing)
        {
            var body=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+prefab+".prefab"));
            var foot=Ground(at);var way=facing-at;way.y=0;body.transform.SetPositionAndRotation(foot,Quaternion.LookRotation(way.sqrMagnitude>.01f?way:Vector3.forward));
            foreach(var b in body.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
            foreach(var animator in body.GetComponentsInChildren<Animator>())Idle(animator);
            return body;
        }
        static void Shot(Camera camera,string path)
        {
            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;
            camera.Render();camera.Render();
            var prior=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());
            RenderTexture.active=prior;camera.targetTexture=null;
            Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
        }
    }
}
