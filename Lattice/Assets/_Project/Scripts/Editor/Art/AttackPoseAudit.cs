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
    public static class AttackPoseAudit
    {
        public static void Capture()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-attack-run")??"attack-poses-01";
            if(run.IndexOfAny(new[]{'/','\\',':'})>=0)throw new InvalidOperationException("Use one evidence folder name");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C3",run));
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve previous pose evidence");
            Directory.CreateDirectory(folder);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.65f,.72f);
            var light=new GameObject("Attack audit light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;
            light.transform.rotation=Quaternion.Euler(40,-25,0);
            var camera=new GameObject("Attack audit camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;
            camera.orthographicSize=1.8f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.14f,.19f);
            camera.transform.position=new Vector3(4,3,5);camera.transform.LookAt(new Vector3(0,1,0));
            using var report=new StreamWriter(Path.Combine(folder,"poses.csv"));
            report.WriteLine("body,clip,phase,clipLength,handX,handY,handZ,tipX,tipY,tipZ,leftX,leftY,leftZ,rightX,rightY,rightZ,upX,upY,upZ,forwardX,forwardY,forwardZ");
            foreach(string id in new[]{"TarenShaped","SelaShaped"})
            {
                var body=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+id+".prefab"));
                var animator=body.GetComponentInChildren<Animator>();var clips=animator.runtimeAnimatorController.animationClips;
                animator.runtimeAnimatorController=null;
                var edge=new GameObject("Visible wrist edge",typeof(LineRenderer)).GetComponent<LineRenderer>();
                var edgeMaterial=new Material(Resources.Load<Material>("Effects/flare_01"));
                if(DevArgs.Has("-wrist-edge"))edgeMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);
                edge.sharedMaterial=edgeMaterial;edge.positionCount=2;edge.startWidth=.08f;edge.endWidth=.02f;
                edge.startColor=edge.endColor=new Color(1,.65f,.12f);
                foreach(string state in id=="TarenShaped"?new[]{"Attack1","Attack2","Attack3","Cleave"}:new[]{"Shoot"})
                {
                    var clip=clips.Single(c=>c.name==state);var graph=PlayableGraph.Create("Attack pose");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);
                    AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(playable);graph.Play();graph.Evaluate(0);
                    for(int sample=0;sample<=60;sample++)
                    {
                        float phase=sample/60f;playable.SetTime(clip.length*Mathf.Min(.9999f,phase));graph.Evaluate(0);
                        var wrist=animator.GetBoneTransform(HumanBodyBones.RightHand);var hand=wrist.position;
                        var elbow=animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position;
                        var tip=hand+(DevArgs.Has("-wrist-edge")?wrist.up*HeroWeaponVfx.BladeLength:(hand-elbow).normalized*.9f);
                        var left=animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
                        edge.enabled=id=="TarenShaped";edge.SetPosition(0,hand);edge.SetPosition(1,tip);
                        var right=wrist.right;var up=wrist.up;var forward=wrist.forward;
                        report.WriteLine(FormattableString.Invariant($"{id},{state},{phase},{clip.length},{hand.x},{hand.y},{hand.z},{tip.x},{tip.y},{tip.z},{left.x},{left.y},{left.z},{right.x},{right.y},{right.z},{up.x},{up.y},{up.z},{forward.x},{forward.y},{forward.z}"));
                        if(sample%10==0)FacingAudit.Capture(camera,Path.Combine(folder,id+"-"+state+"-"+sample.ToString("D2")+".png"));
                    }
                    graph.Destroy();
                }
                UnityEngine.Object.DestroyImmediate(edge.gameObject);UnityEngine.Object.DestroyImmediate(edgeMaterial);UnityEngine.Object.DestroyImmediate(body);
            }
            Debug.Log("ATTACK_POSES_OK "+folder);
        });
    }
}
