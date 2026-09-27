using System;
using System.IO;
using Lattice.Combat;
using Lattice.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Lattice.EditorTools
{
    public static class FlightDisableAudit
    {
        public static void Capture()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-flight-disable-run")??"flight-disable-poses01";
            if(run.IndexOfAny(new[]{'/','\\',':'})>=0)throw new InvalidOperationException("Use one evidence folder name");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C4",run));
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve prior disable evidence");
            Directory.CreateDirectory(folder);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.65f,.72f);
            var light=new GameObject("Disable audit light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(40,-25,0);
            var camera=new GameObject("Disable audit camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=2.5f;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.1f,.13f);
            foreach(string hero in new[]{"Taren","Sela"})
            {
                var root=new GameObject(hero,typeof(CombatActor));var actor=root.GetComponent<CombatActor>();actor.SendMessage("Awake");actor.character=hero;actor.flight=true;
                var body=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+hero+"Flight.prefab"),root.transform);body.transform.localPosition=Vector3.up*.8f;
                var pose=root.AddComponent<FlightFailurePresentation>();pose.Begin(actor,body.transform,default);
                void Capture(string stage)
                {
                    foreach(string view in new[]{"front","side","top"})
                    {
                        var center=Vector3.up*.95f;var direction=view=="front"?new Vector3(.15f,.15f,1):view=="side"?new Vector3(1,.1f,0):new Vector3(.01f,1,.001f);
                        camera.transform.position=center+direction.normalized*20;camera.transform.LookAt(center);
                        FacingAudit.Capture(camera,Path.Combine(folder,hero+"-"+stage+"-"+view+".png"));
                    }
                }
                foreach(float elapsed in new[]{0,.15f,.55f,1.05f}){pose.Advance(elapsed,false);Capture("down-"+Mathf.RoundToInt(elapsed*100));}
                pose.BeginRecovery();pose.Advance(.55f,true);Capture("recover-55");pose.Finish();Capture("restored");
                UnityEngine.Object.DestroyImmediate(root);
            }
            Debug.Log("FLIGHT_DISABLE_AUDIT_OK");
        });
    }
}
