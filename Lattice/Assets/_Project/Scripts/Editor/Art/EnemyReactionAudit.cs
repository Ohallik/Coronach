using System;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lattice.EditorTools
{
    public static class EnemyReactionAudit
    {
        public static void Capture()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-reaction-run")??"sentinel-reaction-poses-01";
            if(run.IndexOfAny(new[]{'/','\\',':'})>=0)throw new InvalidOperationException("Use one evidence folder name");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C3",run));
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve previous pose evidence");
            Directory.CreateDirectory(folder);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.65f,.72f);
            var light=new GameObject("Reaction audit light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;
            light.transform.rotation=Quaternion.Euler(40,-25,0);
            var camera=new GameObject("Reaction audit camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.14f,.19f);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Diagnostic support plane";
            floor.transform.position=new Vector3(0,-.02f,0);floor.transform.localScale=new Vector3(5,.04f,5);
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.22f,.25f,.28f));
            floor.GetComponent<Renderer>().sharedMaterial=material;
            var body=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/SentinelHusk.prefab"));
            var animator=body.GetComponentInChildren<Animator>();
            var controller=(AnimatorController)animator.runtimeAnimatorController;
            var idle=(AnimationClip)controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Idle").state.motion;
            var stagger=AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Animation/HeroLocomotion.fbx").OfType<AnimationClip>().Single(c=>c.name=="Stagger");
            animator.runtimeAnimatorController=null;
            using var report=new StreamWriter(Path.Combine(folder,"poses.csv"));report.WriteLine("clip,phase,headX,headY,headZ,leftFootY,rightFootY,minimumY,bakedMinimumY");
            foreach(string label in DevArgs.Has("-reaction-recoil")?new[]{"Recoil"}:new[]{"Idle","Stagger","Walk","Attack"})
            {
                var clip=label=="Recoil"?AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Animation/SentinelRecoil.anim"):
                    label=="Stagger"?stagger:(AnimationClip)controller.layers[0].stateMachine.states.Single(s=>s.state.name==label).state.motion;
                var graph=PlayableGraph.Create("Enemy reaction pose");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);
                var output=AnimationPlayableOutput.Create(graph,"Pose",animator);
                if(label=="Recoil")
                {
                    var basePose=AnimationClipPlayable.Create(graph,idle);basePose.SetApplyFootIK(false);basePose.SetSpeed(0);
                    var mixer=AnimationLayerMixerPlayable.Create(graph,2);
                    graph.Connect(basePose,0,mixer,0);mixer.SetInputWeight(0,1);graph.Connect(playable,0,mixer,1);
                    mixer.SetInputWeight(1,DevArgs.Has("-reaction-armored")?.45f:1);
                    mixer.SetLayerMaskFromAvatarMask(1,AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/_Project/Art/Animation/EnemyRecoil.mask"));
                    output.SetSourcePlayable(mixer);
                }
                else output.SetSourcePlayable(playable);
                graph.Play();graph.Evaluate(0);
                for(int sample=0;sample<=120;sample++)
                {
                    float phase=Mathf.Min(sample/120f,.9999f);
                    body.transform.position=Vector3.zero;
                    playable.SetTime(clip.length*phase);graph.Evaluate(0);
                    if(DevArgs.Has("-reaction-support"))
                    {
                        var support=body.GetComponent<GroundLifecycleSupport>();
                        var curve=label=="Recoil"||label=="Idle"?support.idleSurface:label=="Stagger"?support.staggerSurface:
                            label=="Walk"?support.walkSurface:support.attackSurface;
                        body.transform.position=Vector3.up*Mathf.Max(0,.012f-curve.Evaluate(label=="Recoil"?0:phase));
                    }
                    var bounds=ModelGeometry.BoundsOf(body);var head=animator.GetBoneTransform(HumanBodyBones.Head).position;
                    float bakedMinimum=float.PositiveInfinity;
                    foreach(var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var mesh=new Mesh();skin.BakeMesh(mesh);
                        var world=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
                        foreach(var vertex in mesh.vertices)bakedMinimum=Mathf.Min(bakedMinimum,world.MultiplyPoint3x4(vertex).y);
                        UnityEngine.Object.DestroyImmediate(mesh);
                    }
                    if(Mathf.Abs(bakedMinimum-bounds.min.y)>.001f)throw new InvalidOperationException("Independent skin probes disagree "+bakedMinimum+" / "+bounds.min.y);
                    report.WriteLine(FormattableString.Invariant($"{clip.name},{phase},{head.x},{head.y},{head.z},{animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y},{animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y},{bounds.min.y},{bakedMinimum}"));
                    camera.orthographicSize=1.65f;camera.transform.position=new Vector3(3,2.3f,4);camera.transform.LookAt(new Vector3(0,1,0));
                    if(sample==0||sample==18||sample==30||sample==36||sample==54||sample==72||sample==96||sample==120)
                        FacingAudit.Capture(camera,Path.Combine(folder,label+"-"+phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png"));
                    if(DevArgs.Has("-reaction-ground-check")&&bounds.min.y<-.03f)throw new InvalidOperationException(label+" foot penetration "+bounds.min.y);
                }
                graph.Destroy();
            }
            Debug.Log("ENEMY_REACTION_POSES_OK "+folder);
        });
    }
}
