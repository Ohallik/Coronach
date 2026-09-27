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
    public static class CreatureRestAudit
    {
        public static void Capture()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-creature-rest-run")??"creature-rest-01";
            if(run.IndexOfAny(new[]{'/','\\',':'})>=0)throw new InvalidOperationException("Use one evidence folder name");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C3",run));
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve prior rest evidence");
            Directory.CreateDirectory(folder);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.65f,.72f);
            var light=new GameObject("Rest audit light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.shadows=LightShadows.Soft;
            light.transform.rotation=Quaternion.Euler(40,-25,0);
            var camera=new GameObject("Rest audit camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.1f,.13f);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.05f,0);floor.transform.localScale=new Vector3(40,.1f,40);
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.22f,.25f,.28f));floor.GetComponent<Renderer>().sharedMaterial=material;
            using var report=new StreamWriter(Path.Combine(folder,"rest.csv"));report.WriteLine("body,pose,pitch,roll,centroidHeight,minimumY");
            foreach(string id in new[]{"Ridgehound","Scrapmite","Burrower"})
            {
                var body=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/"+id+".prefab"));
                var animator=body.GetComponentInChildren<Animator>();PlayableGraph graph=default;
                if(animator!=null&&animator.runtimeAnimatorController!=null)
                {
                    var controller=(AnimatorController)animator.runtimeAnimatorController;
                    var clip=(AnimationClip)controller.layers[0].stateMachine.states.Single(x=>x.state.name=="Idle").state.motion;
                    animator.runtimeAnimatorController=null;graph=PlayableGraph.Create("Creature frozen pose");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var playable=AnimationClipPlayable.Create(graph,clip);AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(playable);graph.Play();graph.Evaluate(0);
                }
                var points=ModelGeometry.Points(body);Vector3 centroid=Vector3.zero;foreach(var p in points)centroid+=p;centroid/=points.Length;
                float Height(Quaternion rotation)
                {float minimum=float.PositiveInfinity;foreach(var p in points)minimum=Mathf.Min(minimum,(rotation*p).y);return (rotation*centroid).y-minimum+.025f;}
                float best=float.PositiveInfinity;Vector2 angles=default;
                // Search only a fallen side/back attitude, not a live standing
                // pose. Choose an actual broad body support over a limb tip.
                for(int pitch=-40;pitch<=40;pitch+=2)for(int roll=70;roll<=170;roll+=2)
                {
                    float height=Height(Quaternion.Euler(pitch,0,roll));
                    if(height<best){best=height;angles=new Vector2(pitch,roll);}
                }
                foreach(bool settled in new[]{false,true})
                {
                    float pitch=settled?angles.x:8,roll=settled?angles.y:id=="Burrower"?42:78;
                    body.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(pitch,0,roll));
                    body.transform.position+=Vector3.up*(.025f-ModelGeometry.BoundsOf(body).min.y);
                    var bounds=ModelGeometry.BoundsOf(body);camera.orthographicSize=Mathf.Max(1.8f,bounds.size.magnitude*.55f);
                    camera.transform.position=bounds.center+new Vector3(3,2.3f,4).normalized*30;camera.transform.LookAt(bounds.center);
                    string pose=settled?"settled":"prior";FacingAudit.Capture(camera,Path.Combine(folder,id+"-"+pose+".png"));
                    camera.transform.position=bounds.center+new Vector3(30,.5f,0);camera.transform.LookAt(bounds.center);FacingAudit.Capture(camera,Path.Combine(folder,id+"-"+pose+"-side.png"));
                    report.WriteLine(FormattableString.Invariant($"{id},{pose},{pitch},{roll},{Height(Quaternion.Euler(pitch,0,roll))},{bounds.min.y}"));
                    Debug.Log($"CREATURE_REST {id} {pose} pitch={pitch} roll={roll} centroid={Height(Quaternion.Euler(pitch,0,roll)):F6}");
                }
                if(graph.IsValid())graph.Destroy();UnityEngine.Object.DestroyImmediate(body);
            }
            Debug.Log("CREATURE_REST_AUDIT_OK");
        });
    }
}
