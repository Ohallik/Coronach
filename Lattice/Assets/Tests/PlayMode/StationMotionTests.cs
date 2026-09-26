using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class StationMotionTests
    {
        [UnityTest] public IEnumerator SteadyWalkingDoesNotRepeatedlyStopTheCamera()
        {
            SceneManager.LoadScene("_Boot"); yield return null; yield return new WaitForSecondsRealtime(.5f);
            SceneFlow.Current.LoadZone("Hub_Decks");
            while (SceneFlow.Current.Loading) yield return null;
            var rig = Object.FindFirstObjectByType<CameraRig>();
            var marker = new GameObject("constant-speed camera target");
            marker.transform.position = new Vector3(-20,0,-5);
            rig.Apply(marker.transform, ZoneController.Current.definition.cameraProfile);
            int stopped = 0, count = 0;
            Vector3 previous = Camera.main.transform.position;
            for (int i = 0; i < 150; i++)
            {
                marker.transform.position += Vector3.right * (2.6f * Time.unscaledDeltaTime);
                yield return null;
                if (i > 40)
                {
                    count++;
                    if (Vector3.Distance(previous,Camera.main.transform.position) < .0001f) stopped++;
                }
                previous = Camera.main.transform.position;
            }
            Object.Destroy(marker);
            Assert.Less((float)stopped/count,.05f,"a constant-speed walk must not repeatedly freeze the camera between threshold crossings");
        }

        [UnityTest] public IEnumerator WallContactReportsActualPlanarTravel()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(100,-.5f,100); floor.transform.localScale = new Vector3(20,1,20);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(100,1,102); wall.transform.localScale = new Vector3(10,3,.5f);
            var body = new GameObject("blocked motor", typeof(CharacterController), typeof(GroundMotor));
            body.transform.position = new Vector3(100,0,100);
            var controller = body.GetComponent<CharacterController>(); controller.center=Vector3.up; controller.height=2; controller.radius=.4f;
            var motor = body.GetComponent<GroundMotor>();
            for(int i=0;i<60;i++) { motor.Move(Vector2.up,false,false); yield return null; }
            Vector3 before=body.transform.position;
            motor.Move(Vector2.up,false,false);
            float actual=Vector3.Distance(before,body.transform.position)/Time.deltaTime;
            float reported=motor.Velocity.magnitude;
            Object.Destroy(body);Object.Destroy(floor);Object.Destroy(wall);
            Assert.Less(actual,.1f,"fixture must actually be blocked by the wall");
            Assert.Less(reported,.1f,"stride input must stop when actual movement is blocked");
        }
    }
}
