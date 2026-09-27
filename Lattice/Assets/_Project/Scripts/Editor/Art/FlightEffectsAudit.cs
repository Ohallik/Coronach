using System;
using System.IO;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Lattice.EditorTools
{
    public static class FlightEffectsAudit
    {
        // Controlled stills of the actual runtime renderers. This does not
        // substitute for an ordinary player's motion or state evidence.
        public static void Capture()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-flight-effects-run")??"hull-effects-01";
            if(run.IndexOfAny(new[]{'/','\\',':'})>=0)throw new InvalidOperationException("Use one evidence folder name");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C4",run));
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve prior effects evidence");
            Directory.CreateDirectory(folder);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.65f,.72f);
            var light=new GameObject("Effects audit light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(40,-25,0);
            var camera=new GameObject("Effects audit camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=2.8f;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.1f,.13f);
            foreach(string id in new[]{"Taren","Sela"})
            {
                var root=new GameObject(id,typeof(CharacterController),typeof(Health));root.AddComponent<CapsuleCollider>();
                var actor=root.AddComponent<CombatActor>();actor.SendMessage("Awake");actor.character=id;actor.flight=true;
                var body=root.AddComponent<HeroCollision>();body.SendMessage("Awake");body.SetFlight(true);
                var motor=root.AddComponent<FlightMotor>();motor.SendMessage("Awake");actor.motor=motor;
                var form=root.AddComponent<FormController>();form.flight=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+id+"Flight.prefab"),root.transform);
                form.flight.transform.localPosition=Vector3.up*.8f;form.flight.transform.localRotation=Quaternion.Euler(-5,0,15);
                var thrusters=root.AddComponent<FlightThrusters>();thrusters.SendMessage("Awake");thrusters.SendMessage("Prepare");
                var edge=root.AddComponent<FlightDashContact>();edge.SendMessage("Awake");
                foreach(string state in new[]{"thrust","boost","lunge"})
                {
                    motor.Move(Vector2.up,state!="thrust",false);
                    typeof(FlightThrusters).GetField("drive",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(thrusters,state=="thrust"?1f:2f);
                    thrusters.SendMessage("LateUpdate");if(state=="lunge")edge.Begin(default);
                    foreach(string view in new[]{"back","side","top"})
                    {
                        Vector3 center=root.transform.position+Vector3.up*.95f;
                        Vector3 direction=view=="back"?new Vector3(.2f,.25f,-1):view=="side"?new Vector3(1,.2f,0):new Vector3(0,1,.001f);
                        camera.transform.position=center+direction.normalized*20;camera.transform.LookAt(center);
                        FacingAudit.Capture(camera,Path.Combine(folder,id+"-"+state+"-"+view+".png"));
                    }
                }
                UnityEngine.Object.DestroyImmediate(root);
            }
            Debug.Log("FLIGHT_EFFECTS_AUDIT_OK");
        });
    }
}
