using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class FlightHullTests
    {
        CombatActor a,b;GameObject fixture;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            a=PartyController.Current.members[0];b=PartyController.Current.members[1];
            foreach(var actor in new[]{a,b}){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            fixture=new GameObject("Hull collision fixture");yield return new WaitForSecondsRealtime(.7f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();Object.Destroy(fixture);SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor actor,Vector3 position,float yaw=0)
        {
            var motor=actor.GetComponent<FlightMotor>();var cc=actor.GetComponent<CharacterController>();cc.enabled=false;motor.Halt();
            var heading=Quaternion.Euler(0,yaw,0)*Vector3.forward;motor.Move(new Vector2(heading.x,heading.z),false,false);motor.Halt();
            actor.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));cc.enabled=true;
            var hull=actor.GetComponent<FormController>().flight;hull.GetComponent<FlightShipMotion>().enabled=false;hull.transform.localRotation=Quaternion.identity;
            Physics.SyncTransforms();
        }
        static Vector3 Nose(CombatActor actor)=>actor.GetComponent<FormController>().flight.transform.TransformPoint(new Vector3(0,actor.character=="Taren"?.205f:.114f,1.6f));
        [UnityTest] public IEnumerator LandingRestoresTheGroundBodyAndSwapKeepsTheFlightFootprint()
        {
            var cc=a.GetComponent<CharacterController>();float radius=cc.radius;Assert.Greater(radius,1.5f);
            PartyController.Current.Swap();Assert.AreEqual(radius,cc.radius);
            foreach(var hero in new[]{a,b})hero.GetComponent<PartnerBrain>().enabled=false;
            ZoneController.Current.SafePocket(true);yield return new WaitForSecondsRealtime(.7f);
            foreach(var hero in new[]{a,b})
            {
                var movement=hero.GetComponent<CharacterController>();var damage=hero.GetComponent<CapsuleCollider>();
                Assert.AreEqual(.35f,movement.radius,.0001f);Assert.AreEqual(1.9f,movement.height,.0001f);
                Assert.AreEqual(Vector3.up*.95f,movement.center);Assert.AreEqual(.08f,movement.skinWidth,.0001f);
                Assert.AreEqual(movement.radius,damage.radius);Assert.AreEqual(movement.height,damage.height);Assert.AreEqual(movement.center,damage.center);
            }
        }
        [UnityTest] public IEnumerator FlightReviveWorksBesideTheHullWithoutRequiringOverlap()
        {
            Place(a,new Vector3(200,1,0));Place(b,new Vector3(200,1,5),180);
            b.Health.Receive(new DamagePacket{amount=100000,type=Lattice.Data.DamageType.Kinetic});
            yield return new WaitForSecondsRealtime(2.2f);Assert.IsFalse(b.Health.Alive,"distant craft revived outside assistance reach");
            var cc=a.GetComponent<CharacterController>();cc.enabled=false;a.transform.position=new Vector3(200,1,1.3f);cc.enabled=true;
            Assert.Greater(Vector3.Dot(Nose(b)-Nose(a),Vector3.forward),.2f,"fixture must leave visible space between noses");
            yield return new WaitForSecondsRealtime(1);Assert.IsFalse(b.Health.Alive,"revive skipped its two-second hold");
            yield return new WaitForSecondsRealtime(1.3f);Assert.IsTrue(b.Health.Alive,"adjacent non-overlapping craft could not assist");
            yield return new WaitForSecondsRealtime(1.3f);Assert.IsTrue(b.CanAct);Assert.IsTrue(b.GetComponent<CharacterController>().enabled);
        }
        [UnityTest] public IEnumerator FlightArrivalHasRoomForBothGeneratedWingSpans()
        {
            // These extents are independently measured generated mesh bounds.
            float spacing=Mathf.Abs(a.transform.position.x-b.transform.position.x);
            Assert.GreaterOrEqual(spacing,1.4875f+1.325f+.1f,"arrival spawns the two visible hulls through each other");
            yield return null;
        }
        [UnityTest] public IEnumerator HeadOnAndCrossHeadingCraftContactBeforeTheirNosesIntersect()
        {
            foreach(float yaw in new[]{0f,90f})
            {
                Vector3 forward=Quaternion.Euler(0,yaw,0)*Vector3.forward,start=new Vector3(200,1,0);
                Place(a,start,yaw);Place(b,start+forward*6,yaw+180);
                float deadline=Time.unscaledTime+1.1f,minGap=float.PositiveInfinity;
                while(Time.unscaledTime<deadline)
                {
                    a.motor.Move(new Vector2(forward.x,forward.z),false,false);yield return null;
                    minGap=Mathf.Min(minGap,Vector3.Dot(Nose(b)-Nose(a),forward));
                }
                Assert.GreaterOrEqual(minGap,-.03f,"movement collision admits visibly interpenetrating noses");
                Assert.Less(minGap,1,"fixture did not reach the other craft");
            }
        }
        [UnityTest] public IEnumerator OpaqueWallStopsTheActualNoseAtBothShipHeadings()
        {
            Place(b,new Vector3(220,1,0));
            foreach(var actor in new[]{a,b})foreach(float yaw in new[]{0f,90f})
            {
                Place(a,new Vector3(220,1,0));Place(b,new Vector3(230,1,0));
                Vector3 forward=Quaternion.Euler(0,yaw,0)*Vector3.forward,start=new Vector3(200,1,0);Place(actor,start,yaw);
                var wall=new GameObject("Hull wall",typeof(BoxCollider));wall.transform.SetParent(fixture.transform);
                wall.transform.SetPositionAndRotation(start+forward*5+Vector3.up,Quaternion.Euler(0,yaw,0));wall.GetComponent<BoxCollider>().size=new Vector3(12,8,.2f);
                Physics.SyncTransforms();float deadline=Time.unscaledTime+.9f,minGap=float.PositiveInfinity;
                while(Time.unscaledTime<deadline)
                {
                    actor.motor.Move(new Vector2(forward.x,forward.z),false,false);yield return null;
                    minGap=Mathf.Min(minGap,Vector3.Dot(wall.transform.position-forward*.1f-Nose(actor),forward));
                }
                Assert.GreaterOrEqual(minGap,-.03f,actor.character+" nose penetrates solid wall");Assert.Less(minGap,1,"fixture failed to approach the wall");
                Object.Destroy(wall);yield return null;
            }
        }
    }
}
