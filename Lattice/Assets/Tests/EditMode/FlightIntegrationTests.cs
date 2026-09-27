using Lattice.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Lattice.Tests.EditMode
{
    public sealed class FlightIntegrationTests
    {
        static (float distance,float velocity) Brake(int fps)
        {
            var velocity=Vector3.forward*12;float distance=0;
            for(int i=0;i<fps*2;i++)distance+=FlightMotor.Advance(ref velocity,Vector3.zero,1f/fps,19,.75f,12,true).z;
            return(distance,velocity.z);
        }
        [Test] public void BrakingDistanceAgreesAtThirtySixtyAndOneTwentyFrames()
        {
            float exact=12f/9*(1-Mathf.Exp(-18));
            float low=float.PositiveInfinity,high=0;
            foreach(int fps in new[]{30,60,120})
            {
                var result=Brake(fps);TestContext.WriteLine($"brake fps={fps} distance={result.distance:F6} velocity={result.velocity:F6}");
                low=Mathf.Min(low,result.distance);high=Mathf.Max(high,result.distance);
                Assert.That(result.velocity,Is.LessThan(.001f),"brake must actually stop the craft");
            }
            Assert.That((high-low)/exact,Is.LessThan(.05f),"same two-second brake input exceeds C4's five-percent travel spread");
            Assert.That((exact-low)/exact,Is.LessThan(.05f),"a matching but shortened travel curve is not sufficient");
        }
        [Test] public void FreeThrustTravelAndReversalAgreeAcrossFrameRates()
        {
            Vector3? reference=null;float referenceTravel=0;
            foreach(int fps in new[]{120,60,30})
            {
                Vector3 velocity=Vector3.zero,position=Vector3.zero;float travel=0;
                for(int i=0;i<fps*6;i++)
                {
                    float t=i/(float)fps;Vector3 input=t<1?Vector3.forward:t<2?Vector3.right:t<3?Vector3.back:t<4?Vector3.left:Vector3.zero;
                    bool boost=t>=2&&t<4;var delta=FlightMotor.Advance(ref velocity,input,1f/fps,19*(boost?2.1f:1),.75f,12*(boost?1.9f:1),t>=5);
                    position+=delta;travel+=delta.magnitude;
                }
                TestContext.WriteLine($"turn fps={fps} position={position:F6} velocity={velocity:F6} travel={travel:F6}");
                if(reference==null){reference=position;referenceTravel=travel;}
                else
                {
                    Assert.That(Vector3.Distance(position,reference.Value),Is.LessThan(.3f),"matched six-second thrust sequence changes its end point by more than 0.3 m");
                    Assert.That(Mathf.Abs(travel-referenceTravel)/referenceTravel,Is.LessThan(.05f),"matched thrust/reversal travel exceeds five percent");
                }
            }
        }
        [Test] public void UnpoweredCoastAgreesWithItsContinuousDragCurve()
        {
            float exact=12f/.75f*(1-Mathf.Exp(-1.5f));
            foreach(int fps in new[]{30,60,120})
            {
                var velocity=Vector3.forward*12;float distance=0;
                for(int i=0;i<fps*2;i++)distance+=FlightMotor.Advance(ref velocity,Vector3.zero,1f/fps,19,.75f,12,false).z;
                TestContext.WriteLine($"coast fps={fps} distance={distance:F6} velocity={velocity.z:F6}");
                Assert.That(Mathf.Abs(distance-exact)/exact,Is.LessThan(.05f));
                Assert.That(velocity.z,Is.EqualTo(12*Mathf.Exp(-1.5f)).Within(.001f));
            }
        }
        [TestCase(6f)] [TestCase(22f)] public void ZeroTimeDoesNotChangePositionOrMomentum(float forwardSpeed)
        {
            var velocity=new Vector3(4,0,forwardSpeed);var before=velocity;
            Assert.AreEqual(Vector3.zero,FlightMotor.Advance(ref velocity,Vector3.left,0,19,.75f,12,true));
            Assert.AreEqual(before,velocity);
        }
    }
}
