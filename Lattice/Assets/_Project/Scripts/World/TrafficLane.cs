using UnityEngine;
using UnityEngine.Splines;
namespace Lattice.World
{
    [RequireComponent(typeof(SplineContainer))]
    public sealed class TrafficLane:MonoBehaviour
    {
        public float speed=5;
        public Vector3 Position(float t)=>GetComponent<SplineContainer>().EvaluatePosition(Mathf.Repeat(t,1));
        public Vector3 Tangent(float t)=>GetComponent<SplineContainer>().EvaluateTangent(Mathf.Repeat(t,1));
        public float Length=>GetComponent<SplineContainer>().CalculateLength();
    }
}
