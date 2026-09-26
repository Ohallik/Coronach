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
        [UnityTest] public IEnumerator StartAndStopKeepAResponsiveBoundedStep()
        {
            GameTime.Reset();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(30,1,30);
            var body=new GameObject("start stop motor",typeof(CharacterController),typeof(GroundMotor));
            body.transform.position=new Vector3(100,0,100);
            var controller=body.GetComponent<CharacterController>();controller.center=Vector3.up;controller.height=2;controller.radius=.4f;
            var motor=body.GetComponent<GroundMotor>();
            try
            {
                yield return null;yield return null;
                Assert.Less(Time.unscaledDeltaTime,.05f,"fixture must resolve an ordinary start step");
                motor.Move(Vector2.up,false,false);
                // Headless frames can be shorter than a position float can
                // resolve. Response still has to be visible within 60 ms.
                float responseDeadline=Time.unscaledTime+.06f;
                while(motor.Velocity.z<=.01f&&Time.unscaledTime<responseDeadline)
                {yield return null;motor.Move(Vector2.up,false,false);}
                Assert.Greater(motor.Velocity.z,.01f,"movement must respond within 60 ms");
                Assert.Less(motor.Velocity.z,6,"the first step must not jump instantly to full running speed");
                float until=Time.unscaledTime+.25f;
                while(Time.unscaledTime<until){motor.Move(Vector2.up,false,false);yield return null;}
                Assert.Greater(motor.Velocity.z,6,"starting must promptly reach ordinary pace");
                Vector3 before=body.transform.position;
                motor.Move(Vector2.zero,false,false);
                Assert.Greater(motor.Velocity.z,.1f,"release should complete a short braking step");
                until=Time.unscaledTime+.2f;
                while(Time.unscaledTime<until){motor.Move(Vector2.zero,false,false);yield return null;}
                Assert.Less(motor.Velocity.magnitude,.05f,"stop must settle without drift");
                Assert.Less(Vector3.Distance(before,body.transform.position),.5f,"braking must not slide half a metre");
            }
            finally{Object.Destroy(body);Object.Destroy(floor);}
        }

        [UnityTest] public IEnumerator FreeReversalTakesAStepAndSettlesOnTravelHeading()
        {
            GameTime.Reset();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(30,1,30);
            var body=new GameObject("reversal motor",typeof(CharacterController),typeof(GroundMotor));
            body.transform.position=new Vector3(100,0,100);
            var controller=body.GetComponent<CharacterController>();controller.center=Vector3.up;controller.height=2;controller.radius=.4f;
            var motor=body.GetComponent<GroundMotor>();
            try
            {
                yield return null;
                for(int i=0;i<10;i++){motor.Move(Vector2.up,false,false);yield return null;}
                Assert.Less(Time.unscaledDeltaTime,.05f,"fixture must run faster than 20 fps to distinguish a step from an instantaneous reversal");
                Vector3 before=body.transform.forward;
                motor.Move(Vector2.down,false,false);
                Assert.Less(Vector3.Angle(before,body.transform.forward),90,"a half-turn cannot occur in one ordinary frame");
                float until=Time.unscaledTime+.35f;
                while(Time.unscaledTime<until){motor.Move(Vector2.down,false,false);yield return null;}
                Assert.Less(Vector3.Angle(body.transform.forward,Vector3.back),10,"turn smoothing must settle promptly on requested free-travel heading");
                Assert.Greater(Vector3.Dot(motor.Velocity,Vector3.back),4,"the turn must preserve responsive travel");
            }
            finally{Object.Destroy(body);Object.Destroy(floor);}
        }

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
