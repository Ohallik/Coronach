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
            light.shadows=LightShadows.Soft;
            light.transform.rotation=Quaternion.Euler(40,-25,0);
            var camera=new GameObject("Lifecycle audit camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;
            camera.orthographicSize=1.6f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.14f,.19f);
            camera.transform.position=new Vector3(3,2.3f,4);camera.transform.LookAt(new Vector3(0,.7f,0));
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Diagnostic floor";
            floor.transform.position=new Vector3(0,-.02f,0);floor.transform.localScale=new Vector3(6,.04f,6);
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.22f,.25f,.28f));
            floor.GetComponent<Renderer>().sharedMaterial=material;
            using var report=new StreamWriter(Path.Combine(folder,"poses.csv"));report.WriteLine("body,clip,phase,hipY,headY,leftFootY,rightFootY,hipX,hipZ,boundsX,boundsY,boundsZ,minimumY,supportY");
            using var bones=new StreamWriter(Path.Combine(folder,"bones.csv"));bones.WriteLine("body,clip,phase,bone,x,y,z");
            foreach(string id in DevArgs.Has("-lifecycle-sentinel")?new[]{"SentinelHusk"}:new[]{"TarenNatural","TarenShaped","SelaNatural","SelaShaped","SentinelHusk"})
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
                    Quaternion settle=Quaternion.identity;Vector3 pivot=Vector3.zero;
                    if(DevArgs.Has("-lifecycle-settle")&&state=="Down")
                    {
                        playable.SetTime(clip.length*.9999f);graph.Evaluate(0);
                        pivot=animator.GetBoneTransform(HumanBodyBones.Hips).position;
                        var points=body.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(GeneratedGeometry.WorldSkinPoints).ToArray();
                        float best=float.PositiveInfinity;Vector2 angles=Vector2.zero;
                        for(int pitch=-40;pitch<=40;pitch+=2)for(int roll=-40;roll<=40;roll+=2)
                        {
                            var q=Quaternion.Euler(pitch,0,roll);float minimum=float.PositiveInfinity;
                            foreach(var p in points)minimum=Mathf.Min(minimum,(q*(p-pivot)).y);
                            if(-minimum<best){best=-minimum;settle=q;angles=new Vector2(pitch,roll);}
                        }
                        Debug.Log($"LIFECYCLE_SETTLE {id} pivot={pivot:F5} angles={angles} hipHeight={best}");
                    }
                    foreach(float phase in new[]{0f,.25f,.5f,.65f,.8f,.9f,.9999f})
                    {
                        body.transform.position=Vector3.zero;body.transform.rotation=Quaternion.identity;
                        playable.SetTime(clip.length*phase);graph.Evaluate(0);
                        if(DevArgs.Has("-lifecycle-settle")&&state=="Down")
                        {
                            var q=Quaternion.Slerp(Quaternion.identity,settle,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.9f,phase)));
                            body.transform.rotation=q;body.transform.position=pivot-q*pivot;
                            body.transform.position+=Vector3.up*(.012f-ModelGeometry.BoundsOf(body).min.y);
                        }
                        else if(DevArgs.Has("-lifecycle-support"))
                        {
                            var support=body.GetComponent<GroundLifecycleSupport>();
                            support.RestPose(GroundLifecycleSupport.RestWeight(state,phase),out var tilt,out var offset);
                            body.transform.SetPositionAndRotation(offset,tilt);
                            var vanes=body.GetComponent<GeneratedVanes>();if(vanes!=null)vanes.SetDown(GroundLifecycleSupport.VaneWeight(state,phase),Vector3.up);
                            var curve=state=="Down"?support.downSurface:support.reviveSurface;
                            body.transform.position+=Vector3.up*Mathf.Max(0,.012f-curve.Evaluate(phase)-body.transform.position.y);
                        }
                        var bounds=ModelGeometry.BoundsOf(body);var hip=animator.GetBoneTransform(HumanBodyBones.Hips).position;
                        if(phase>.9f)
                        {
                            float trunkMin=float.PositiveInfinity,legMin=float.PositiveInfinity;
                            foreach(var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>())
                            {
                                var points=GeneratedGeometry.WorldSkinPoints(skin);var weights=skin.sharedMesh.boneWeights;
                                for(int i=0;i<points.Length;i++)
                                {
                                    var w=weights[i];int index=w.boneIndex0;float weight=w.weight0;
                                    if(w.weight1>weight){index=w.boneIndex1;weight=w.weight1;}if(w.weight2>weight){index=w.boneIndex2;weight=w.weight2;}if(w.weight3>weight)index=w.boneIndex3;
                                    var bone=skin.bones[index];
                                    bool leg=bone.IsChildOf(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg))||bone.IsChildOf(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg));
                                    bool trunk=bone==animator.GetBoneTransform(HumanBodyBones.Hips)||bone==animator.GetBoneTransform(HumanBodyBones.Spine)||bone==animator.GetBoneTransform(HumanBodyBones.Chest)||bone==animator.GetBoneTransform(HumanBodyBones.UpperChest);
                                    if(leg)legMin=Mathf.Min(legMin,points[i].y);if(trunk)trunkMin=Mathf.Min(trunkMin,points[i].y);
                                }
                            }
                            Debug.Log($"LIFECYCLE_REST_GEOMETRY {id} trunk={trunkMin:F5} legs={legMin:F5}");
                        }
                        if(DevArgs.Has("-lifecycle-rest-check")&&phase>.9f)
                        {
                            report.Flush();bones.Flush();
                            if(hip.y>.5f||Mathf.Min(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y)>.45f)
                                throw new InvalidOperationException(id+" terminal body remains suspended: hips="+hip.y);
                        }
                        if(id=="SentinelHusk")foreach(var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            var mesh=new Mesh();skin.BakeMesh(mesh);
                            var world=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
                            var points=mesh.vertices.Select(v=>world.MultiplyPoint3x4(v)).ToArray();
                            var bakedBounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bakedBounds.Encapsulate(p);
                            Debug.Log($"LIFECYCLE_SKIN {state} {phase} active={skin.enabled} manual={bounds} baked={bakedBounds} scale={skin.transform.lossyScale}");
                            UnityEngine.Object.DestroyImmediate(mesh);
                        }
                        camera.orthographicSize=Mathf.Max(1.3f,bounds.size.magnitude*.58f);
                        camera.transform.position=bounds.center+new Vector3(3,2.3f,4).normalized*6;camera.transform.LookAt(bounds.center);
                        report.WriteLine(FormattableString.Invariant($"{id},{state},{phase},{hip.y},{animator.GetBoneTransform(HumanBodyBones.Head).position.y},{animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y},{animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y},{hip.x},{hip.z},{bounds.size.x},{bounds.size.y},{bounds.size.z},{bounds.min.y},{body.transform.position.y}"));
                        foreach(HumanBodyBones bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Chest,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot})
                        {var p=animator.GetBoneTransform(bone).position;bones.WriteLine(FormattableString.Invariant($"{id},{state},{phase},{bone},{p.x},{p.y},{p.z}"));}
                        if((DevArgs.Has("-lifecycle-support")||DevArgs.Has("-lifecycle-ground-check"))&&bounds.min.y<-.02f)throw new InvalidOperationException(id+" "+state+" surface penetration "+bounds.min.y);
                        string label=id+"-"+state+"-"+phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture);
                        FacingAudit.Capture(camera,Path.Combine(folder,label+".png"));
                        camera.transform.position=bounds.center+new Vector3(6,.4f,0);camera.transform.LookAt(bounds.center);
                        FacingAudit.Capture(camera,Path.Combine(folder,label+"-side.png"));
                    }
                    graph.Destroy();
                }
                UnityEngine.Object.DestroyImmediate(body);
            }
            Debug.Log("LIFECYCLE_POSES_OK "+folder);
        });
    }
}
