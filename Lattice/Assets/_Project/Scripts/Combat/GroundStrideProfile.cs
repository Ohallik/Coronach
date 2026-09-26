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
        public float Cadence(string clip,float speed)=>clip=="Walk"?
            Mathf.Clamp(speed/walkSpeed,.35f,1.7f):Mathf.Clamp(speed/NativeSpeed(clip),.35f,1.6f);
        public float Stride(string clip,float speed,float cadence)=>
            Mathf.Clamp(speed/Mathf.Max(.01f,NativeSpeed(clip)*cadence),.55f,1.45f);
    }
}
