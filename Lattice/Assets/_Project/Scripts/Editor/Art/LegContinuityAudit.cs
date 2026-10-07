using System;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lattice.EditorTools
{
    // Diagnostic only: compare the owned take with the final corrected joints
    // at every phase, including landing/toe-off omitted by central-stance checks.
    public static class LegContinuityAudit
    {
        public static void Capture()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-leg-run")??"baseline";
            if(run.IndexOfAny(new[]{'/','\\',':'})>=0)throw new ArgumentException("Use one run name");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/2026-10-06-legs",run));
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve earlier evidence");
            Directory.CreateDirectory(folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Camera camera=null;
            if(DevArgs.Has("-leg-render"))
            {
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.65f,.72f);
                var key=new GameObject("key",typeof(Light)).GetComponent<Light>();key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(45,-25,0);
                camera=new GameObject("pose camera",typeof(Camera)).GetComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1.3f;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.14f,.19f);
            }
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(500,1,500);Physics.SyncTransforms();
            using var csv=new StreamWriter(Path.Combine(folder,"joints.csv"));
            csv.WriteLine("hero,form,clip,corrected,seconds,phase,side,hipY,kneeX,kneeY,kneeZ,footX,footY,footZ,kneeAngle");
            foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})
            foreach(float speed in shaped?new[]{6.7f,10.385f}:new[]{2.6f,5.4f})foreach(bool corrected in new[]{false,true})
            {
                var root=new GameObject("leg continuity root");root.transform.position=Vector3.up*.08f;
                var definition=GameCatalog.Find<CharacterDef>(hero);
                var body=UnityEngine.Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
                var rig=body.GetComponentInChildren<Animator>();
                string state=speed>7.5f?"Sprint":speed>3.1f?"Run":"Walk";
                var clip=rig.runtimeAnimatorController.animationClips.Single(c=>c.name==state);
                var profile=body.GetComponent<GeneratedAnimator>().strideProfile;
                UnityEngine.Object.DestroyImmediate(body.GetComponent<GeneratedAnimator>());
                var feet=body.AddComponent<GroundFeet>();feet.Initialize(rig,root.transform,profile);
                rig.runtimeAnimatorController=null;
                var graph=PlayableGraph.Create("leg continuity");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var play=AnimationClipPlayable.Create(graph,clip);play.SetApplyFootIK(!DevArgs.Has("-leg-no-ik"));
                AnimationPlayableOutput.Create(graph,"pose",rig).SetSourcePlayable(play);graph.Play();
                float cadence=profile.Cadence(state,speed),stride=profile.Stride(state,speed,cadence);
                const float dt=1f/240;
                int shot=0;
                try
                {
                    for(int sample=0;sample<Mathf.CeilToInt(clip.length/cadence*3/dt);sample++)
                    {
                        float time=sample*dt,phase=time*cadence/clip.length;
                        root.transform.position=new Vector3(0,.08f,time*speed);
                        play.SetTime(Mathf.Repeat(phase,1)*clip.length);graph.Evaluate(0);
                        if(corrected)feet.Correct(Vector3.forward*speed,state,phase,stride,false,targetFacing:false,deltaTime:dt);
                        if(camera!=null&&corrected&&phase>=1+shot/8f&&shot<8)
                        {
                            camera.transform.rotation=Quaternion.Euler(15,125,0);
                            camera.transform.position=root.transform.position+Vector3.up*1.05f-camera.transform.forward*6;
                            FacingAudit.Capture(camera,Path.Combine(folder,$"{hero}-{(shaped?"Shaped":"Natural")}-{state}-{shot++:00}.png"));
                        }
                        foreach(bool left in new[]{true,false})
                        {
                            var hip=rig.GetBoneTransform(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg).position;
                            var knee=rig.GetBoneTransform(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg).position;
                            var foot=rig.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot).position;
                            float angle=Vector3.Angle(hip-knee,foot-knee);
                            hip-=root.transform.position;knee-=root.transform.position;foot-=root.transform.position;
                            csv.WriteLine(FormattableString.Invariant($"{hero},{(shaped?"Shaped":"Natural")},{state},{corrected},{time:F6},{phase:F6},{(left?"left":"right")},{hip.y:F6},{knee.x:F6},{knee.y:F6},{knee.z:F6},{foot.x:F6},{foot.y:F6},{foot.z:F6},{angle:F6}"));
                        }
                    }
                }
                finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(root);}
            }
            UnityEngine.Object.DestroyImmediate(floor);Debug.Log("LEG_CONTINUITY_AUDIT_OK "+folder);
        });
    }
}
