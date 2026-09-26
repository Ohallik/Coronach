using System;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lattice.EditorTools
{
    public static class LifecyclePoseAudit
    {
        public static void Capture()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-lifecycle-run")??"poses-01";
            if(run.IndexOfAny(new[]{'/','\\',':'})>=0)throw new InvalidOperationException("Use one evidence folder name");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C3",run));
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve previous pose evidence");
            Directory.CreateDirectory(folder);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.65f,.72f);
            var light=new GameObject("Lifecycle audit light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;
            light.transform.rotation=Quaternion.Euler(40,-25,0);
            var camera=new GameObject("Lifecycle audit camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;
            camera.orthographicSize=1.6f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.14f,.19f);
            camera.transform.position=new Vector3(3,2.3f,4);camera.transform.LookAt(new Vector3(0,.7f,0));
            using var report=new StreamWriter(Path.Combine(folder,"poses.csv"));report.WriteLine("body,clip,phase,hipY,headY,leftFootY,rightFootY,hipX,hipZ,boundsX,boundsY,boundsZ,minimumY,supportY");
            foreach(string id in new[]{"TarenNatural","TarenShaped","SelaNatural","SelaShaped","SentinelHusk"})
            {
                string category=id=="SentinelHusk"?"Enemies":"Characters";
                var body=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/"+category+"/"+id+".prefab"));
                var animator=body.GetComponentInChildren<Animator>();var clips=animator.runtimeAnimatorController.animationClips;
                animator.runtimeAnimatorController=null;
                foreach(string state in id=="SentinelHusk"?new[]{"Down"}:new[]{"Down","Revive"})
                {
                    var clip=clips.Single(c=>c.name==state);var graph=PlayableGraph.Create("Lifecycle pose");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);
                    AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(playable);graph.Play();graph.Evaluate(0);
                    foreach(float phase in new[]{0f,.5f,.9999f})
                    {
                        body.transform.position=Vector3.zero;
                        playable.SetTime(clip.length*phase);graph.Evaluate(0);
                        if(DevArgs.Has("-lifecycle-support"))
                        {
                            var support=body.GetComponent<GroundLifecycleSupport>();
                            var vanes=body.GetComponent<GeneratedVanes>();if(vanes!=null)vanes.SetDown(GroundLifecycleSupport.VaneWeight(state,phase),Vector3.up);
                            var curve=state=="Down"?support.downSurface:support.reviveSurface;
                            body.transform.position=Vector3.up*Mathf.Max(0,.012f-curve.Evaluate(phase));
                        }
                        var bounds=ModelGeometry.BoundsOf(body);var hip=animator.GetBoneTransform(HumanBodyBones.Hips).position;
                        if(id=="SentinelHusk")foreach(var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            var mesh=new Mesh();skin.BakeMesh(mesh);
                            var points=mesh.vertices.Select(v=>skin.transform.TransformPoint(v)).ToArray();
                            var bakedBounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bakedBounds.Encapsulate(p);
                            Debug.Log($"LIFECYCLE_SKIN {state} {phase} active={skin.enabled} manual={bounds} baked={bakedBounds} scale={skin.transform.lossyScale}");
                            UnityEngine.Object.DestroyImmediate(mesh);
                        }
                        camera.orthographicSize=Mathf.Max(1.3f,bounds.size.magnitude*.58f);
                        camera.transform.position=bounds.center+new Vector3(3,2.3f,4).normalized*6;camera.transform.LookAt(bounds.center);
                        report.WriteLine(FormattableString.Invariant($"{id},{state},{phase},{hip.y},{animator.GetBoneTransform(HumanBodyBones.Head).position.y},{animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y},{animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y},{hip.x},{hip.z},{bounds.size.x},{bounds.size.y},{bounds.size.z},{bounds.min.y},{body.transform.position.y}"));
                        if((DevArgs.Has("-lifecycle-support")||DevArgs.Has("-lifecycle-ground-check"))&&bounds.min.y<-.02f)throw new InvalidOperationException(id+" "+state+" surface penetration "+bounds.min.y);
                        FacingAudit.Capture(camera,Path.Combine(folder,id+"-"+state+"-"+(phase<.01f?"start":phase<.9f?"middle":"end")+".png"));
                    }
                    graph.Destroy();
                }
                UnityEngine.Object.DestroyImmediate(body);
            }
            Debug.Log("LIFECYCLE_POSES_OK "+folder);
        });
    }
}
