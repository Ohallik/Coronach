using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class MotionClockTests
    {
        [UnityTest] public IEnumerator FlashUsesActualTravelAndPauseFreezesBothClocks()
        {
            GameTime.Reset();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(30,1,30);
            var roots=new GameObject[2];var actors=new CombatActor[2];var motors=new GroundMotor[2];
            var rigs=new Animator[2];var drivers=new GeneratedAnimator[2];
            try
            {
                for(int i=0;i<2;i++)
                {
                    roots[i]=new GameObject("Flash clock "+i,typeof(Health),typeof(CombatActor),typeof(CharacterController),typeof(GroundMotor));
                    roots[i].transform.position=new Vector3(100+i*4,0,100);
                    var controller=roots[i].GetComponent<CharacterController>();controller.center=Vector3.up;controller.height=2;controller.radius=.4f;
                    actors[i]=roots[i].GetComponent<CombatActor>();actors[i].character=i==0?"Taren":"Sela";
                    motors[i]=roots[i].GetComponent<GroundMotor>();actors[i].motor=motors[i];
                    var body=Object.Instantiate(GameCatalog.Find<CharacterDef>(actors[i].character).shaped,roots[i].transform);
                    rigs[i]=body.GetComponentInChildren<Animator>();drivers[i]=body.GetComponent<GeneratedAnimator>();
                }
                yield return null;yield return null;
                actors[0].Health.InvulnerableUntil=GameTime.Now+3;
                GameTime.BeginFlash();
                float until=Time.unscaledTime+.75f;
                var integrated=new float[2];var start=new[]{roots[0].transform.position,roots[1].transform.position};
                while(Time.unscaledTime<until)
                {
                    for(int i=0;i<2;i++){motors[i].Move(Vector2.up,false,false);integrated[i]+=motors[i].Velocity.z*Time.unscaledDeltaTime;}
                    yield return null;
                }
                for(int i=0;i<2;i++)
                    Assert.That(integrated[i],Is.EqualTo(roots[i].transform.position.z-start[i].z).Within(.03f),"reported unscaled stride distance must equal real displacement through Flash");
                float fast=roots[0].transform.position.z-start[0].z,slow=roots[1].transform.position.z-start[1].z;
                Assert.Greater(fast,3,"protected hero must retain responsive real-time travel");
                Assert.That(slow,Is.InRange(.5f,1.4f),"ordinary world movement must respect quarter-speed Flash");
                Assert.AreEqual("Run",drivers[0].CurrentAnimation,"fast hero must keep a running stride");
                Assert.AreEqual("Walk",drivers[1].CurrentAnimation,"slowed real travel needs a walking stride, not commanded-speed running");
                GameTime.Paused=true;yield return null;
                var positions=new[]{roots[0].transform.position,roots[1].transform.position};
                var phases=new[]{rigs[0].GetCurrentAnimatorStateInfo(0).normalizedTime,rigs[1].GetCurrentAnimatorStateInfo(0).normalizedTime};
                until=Time.unscaledTime+.15f;
                while(Time.unscaledTime<until)
                {
                    for(int i=0;i<2;i++)motors[i].Move(Vector2.right,false,false);
                    yield return null;
                }
                for(int i=0;i<2;i++)
                {
                    Assert.That(Vector3.Distance(positions[i],roots[i].transform.position),Is.LessThan(.001f));
                    Assert.That(rigs[i].GetCurrentAnimatorStateInfo(0).normalizedTime,Is.EqualTo(phases[i]).Within(.001f));
                    Assert.That(motors[i].Velocity.magnitude,Is.LessThan(.001f));
                }
                GameTime.Paused=false;GameTime.EndFlash();
                until=Time.unscaledTime+.2f;
                while(Time.unscaledTime<until){for(int i=0;i<2;i++)motors[i].Move(Vector2.up,false,false);yield return null;}
                Assert.Greater(roots[1].transform.position.z-positions[1].z,.7f,"resume must restore normal travel without a stale Flash clock");
            }
            finally{GameTime.Reset();foreach(var root in roots)if(root!=null)Object.Destroy(root);Object.Destroy(floor);}
        }
    }
}
