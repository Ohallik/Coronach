using UnityEngine;

namespace Lattice.Combat
{
    // Authored from the retargeted generated body's visible sole, not from
    // anatomical ankle origins or the donor's unscaled root translation.
    public sealed class GroundStrideProfile : ScriptableObject
    {
        public Vector3 leftHeel, leftToe, rightHeel, rightToe;
        public float walkSpeed=1.1f, runSpeed=6.4f, sprintSpeed=9.6f;
        public float NativeSpeed(string clip)=>clip=="Sprint"?sprintSpeed:clip=="Run"?runSpeed:walkSpeed;
        // Planar motor speed omits the climb. Account for distance along the
        // support surface; walking shares the adjustment with stride length
        // to avoid turning the owned walk into a rapid shuffle.
        public float Cadence(string clip,float speed,float grade=0)=>(clip=="Walk"?
            Mathf.Clamp(speed/walkSpeed,.35f,1.7f):Mathf.Clamp(speed/NativeSpeed(clip),.35f,1.6f))*
            (clip=="Walk"?Mathf.Min(1.15f,TerrainTravel(grade)):TerrainTravel(grade));
        public float Stride(string clip,float speed,float cadence,float grade=0)=>
            Mathf.Clamp(speed*TerrainTravel(grade)/Mathf.Max(.01f,NativeSpeed(clip)*cadence),.55f,1.45f);
        static float TerrainTravel(float grade)=>Mathf.Lerp(1,Mathf.Sqrt(1+grade*grade),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.45f,grade)));
    }
}
