using System.Collections;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    [DefaultExecutionOrder(100)]
    public sealed class FlightThrustDriver:MonoBehaviour
    {
        public Vector2 input;public bool boost,brake;
        void Update(){GetComponent<FlightMotor>().Move(input,boost,brake);}
    }
    public sealed class FlightThrustTests
    {
        CombatActor[] heroes;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            heroes=PartyController.Current.members;
            for(int i=0;i<heroes.Length;i++)
            {
                var actor=heroes[i];actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;
                var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.GetComponent<FlightMotor>().Halt();
                actor.transform.position=new Vector3(200+i*20,1,0);cc.enabled=true;actor.gameObject.AddComponent<FlightThrustDriver>();
            }
            yield return new WaitForSecondsRealtime(.7f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static LineRenderer[] Flames(CombatActor actor)=>actor.GetComponent<FormController>().flight.GetComponentsInChildren<LineRenderer>(true).Where(l=>l.name=="Flight exhaust").ToArray();
        static Vector3 WorldPoint(LineRenderer line,int index)=>line.useWorldSpace?line.GetPosition(index):line.transform.TransformPoint(line.GetPosition(index));
        static float Length(LineRenderer line)=>Vector3.Distance(WorldPoint(line,0),WorldPoint(line,1));
        static void AssertAttached(CombatActor actor)
        {
            var sockets=actor.GetComponent<FormController>().flight.GetComponent<FlightSockets>();var flames=Flames(actor);
            Assert.AreEqual(actor.character=="Taren"?4:3,flames.Length,"every actual rear nozzle needs its own exhaust");
            foreach(var socket in sockets.engines)
            {
                var flame=flames.SingleOrDefault(l=>Vector3.Distance(WorldPoint(l,0),socket.position)<.02f);
                Assert.IsNotNull(flame,"exhaust origin detached from the posed nozzle");Assert.IsTrue(flame.enabled,"thrust has no visible exhaust");
                Assert.Greater(Length(flame),.25f);Assert.Greater(Vector3.Dot((WorldPoint(flame,1)-WorldPoint(flame,0)).normalized,socket.forward),.99f);
            }
        }
        [UnityTest] public IEnumerator BothHullsEmitAtEveryPosedNozzleAndBoostExtendsThePlume()
        {
            foreach(var actor in heroes)actor.GetComponent<FlightThrustDriver>().input=Vector2.up;
            yield return new WaitForSecondsRealtime(.3f);
            foreach(var actor in heroes)
            {
                AssertAttached(actor);float normal=Flames(actor).Average(Length);
                var hull=actor.GetComponent<FormController>().flight;hull.GetComponent<FlightShipMotion>().enabled=false;
                hull.transform.localRotation=Quaternion.Euler(-5,0,70);actor.GetComponent<FlightThrustDriver>().boost=true;
                yield return new WaitForSecondsRealtime(.3f);AssertAttached(actor);
                Assert.Greater(Flames(actor).Average(Length),normal*1.5f,"boost must read differently from ordinary thrust");
            }
        }
        [UnityTest] public IEnumerator CoastingBrakingAndBoostWithoutInputDoNotBurnMainEngines()
        {
            foreach(var actor in heroes){var driver=actor.GetComponent<FlightThrustDriver>();driver.input=Vector2.up;driver.boost=true;}
            yield return new WaitForSecondsRealtime(.4f);
            foreach(var actor in heroes){AssertAttached(actor);actor.GetComponent<FlightThrustDriver>().input=Vector2.zero;}
            yield return new WaitForSecondsRealtime(.25f);
            foreach(var actor in heroes)
            {
                Assert.Greater(actor.motor.Velocity.magnitude,1,"fixture must still be coasting");
                Assert.IsTrue(Flames(actor).All(l=>!l.enabled),"inertia alone must not burn engines");actor.GetComponent<FlightThrustDriver>().brake=true;
            }
            yield return new WaitForSecondsRealtime(.25f);
            foreach(var actor in heroes)Assert.IsTrue(Flames(actor).All(l=>!l.enabled),"braking has retained stale main thrust");
        }
        [UnityTest] public IEnumerator PauseHoldsThePlumeAndDisabledCraftClearItWithoutGrowingRenderers()
        {
            var actor=heroes[0];var driver=actor.GetComponent<FlightThrustDriver>();driver.input=Vector2.up;
            yield return new WaitForSecondsRealtime(.3f);AssertAttached(actor);
            var flames=Flames(actor);var lengths=flames.Select(Length).ToArray();GameTime.Paused=true;driver.input=Vector2.zero;
            yield return new WaitForSecondsRealtime(.25f);
            for(int i=0;i<flames.Length;i++){Assert.IsTrue(flames[i].enabled);Assert.AreEqual(lengths[i],Length(flames[i]),.0001f,"paused plume advanced");}
            GameTime.Paused=false;yield return new WaitForSecondsRealtime(.25f);Assert.IsTrue(flames.All(l=>!l.enabled));
            for(int i=0;i<4;i++)
            {driver.input=Vector2.up;yield return new WaitForSecondsRealtime(.15f);driver.input=Vector2.zero;yield return new WaitForSecondsRealtime(.2f);}
            Assert.AreEqual(flames.Length,Flames(actor).Length,"thrust cycles leaked renderers");
            driver.input=Vector2.up;yield return new WaitForSecondsRealtime(.2f);AssertAttached(actor);
            actor.Health.Receive(new DamagePacket{amount=100000,type=Lattice.Data.DamageType.Kinetic});yield return null;yield return null;
            Assert.IsFalse(actor.Health.Alive);Assert.IsTrue(flames.All(l=>!l.enabled),"disabled craft still burns engines");
        }
    }
}
