using UnityEngine;
namespace Lattice.Combat
{
    public sealed class ProceduralMotion:MonoBehaviour
    {
        public enum Motion{Hover,Wriggle,Skitter}
        public Motion motion;
        public Transform[] segments;
        Vector3 origin;
        void Start(){origin=transform.localPosition;}
        void LateUpdate()
        {
            if(motion==Motion.Hover)transform.localPosition=origin+Vector3.up*Mathf.Sin(Time.time*2)*.18f;
            else if(motion==Motion.Skitter)transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.time*18)*6);
            else for(int i=0;i<segments.Length;i++)segments[i].localPosition=new Vector3(Mathf.Sin(Time.time*2-i*.65f)*1.8f,Mathf.Cos(Time.time-i*.3f)*.35f,-i*2.2f);
        }
    }
}
