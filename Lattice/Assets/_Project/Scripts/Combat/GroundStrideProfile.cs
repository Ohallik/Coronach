using UnityEngine;

namespace Lattice.Combat
{
    // Authored from the retargeted generated body's visible sole, not from
    // anatomical ankle origins or the donor's unscaled root translation.
    public sealed class GroundStrideProfile : ScriptableObject
    {
        public Vector3 leftHeel, leftToe, rightHeel, rightToe;
        public float walkSpeed=1.1f, runSpeed=6.38f, sprintSpeed=9.6f;
        public float NativeSpeed(string clip)=>clip=="Sprint"?sprintSpeed:clip=="Run"?runSpeed:walkSpeed;
        // Planar motor speed omits the climb. Account for distance along the
        // support surface; walking shares the adjustment with stride length
        // to avoid turning the owned walk into a rapid shuffle.
        public float Cadence(string clip,float speed,float grade=0)=>(clip=="Walk"?
            Mathf.Clamp(speed/walkSpeed,.35f,1.7f):RunCadence(speed/NativeSpeed(clip)))*
            (clip=="Walk"?Mathf.Min(1.15f,TerrainTravel(grade)):TerrainTravel(grade));
        public float Stride(string clip,float speed,float cadence,float grade=0)=>
            Mathf.Clamp(speed*TerrainTravel(grade)/Mathf.Max(.01f,NativeSpeed(clip)*cadence),.55f,1.45f);
        /// <summary>D197. The owned jog is a long, slow-cadence bound: slowing its clip to fit a lower speed read as slow
        /// motion ("running through a swamp"), so below its own pace the jog keeps a slightly quicker cadence and
        /// shortens the step instead, down to the shortest stride the contact correction carries cleanly. Above its own
        /// pace a run speeds up as before. Walking keeps its cadence-first rule: at the 1.6 m/s civil walk that is a
        /// brisk 2.2 steps a second at the owned stride, with no stretched step.</summary>
        public static float RunCadence(float ratio)
        {
            ratio=Mathf.Max(0,ratio);
            if(ratio>=1)return Mathf.Min(1.6f,ratio);
            float stride=Mathf.Lerp(ShortestRunStride,1,Mathf.InverseLerp(.65f,1,ratio));
            return Mathf.Clamp(ratio/stride,.35f,1.6f);
        }
        public const float ShortestRunStride=.58f;
        static float TerrainTravel(float grade)=>Mathf.Lerp(1,Mathf.Sqrt(1+grade*grade),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.45f,grade)));
    }
}
