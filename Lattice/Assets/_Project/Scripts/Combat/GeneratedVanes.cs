using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class GeneratedVanes:MonoBehaviour
    {
        public Transform[] vanes;
        public Quaternion[] folded;
        public void SetDown(float weight,Vector3 normal)
        {
            if(vanes==null||folded==null||vanes.Length!=folded.Length)return;
            for(int i=0;i<vanes.Length;i++)
            {
                var vane=vanes[i];if(vane==null)continue;vane.localRotation=folded[i];
                if(weight<=0)continue;
                var size=vane.GetComponent<MeshFilter>().sharedMesh.bounds.size;
                Vector3 axis=size.y>=size.x&&size.y>=size.z?Vector3.up:size.z>=size.x?Vector3.forward:Vector3.right;
                float side=Mathf.Sign(transform.InverseTransformPoint(vane.position).x);
                Vector3 outward=Vector3.ProjectOnPlane(transform.right*side+transform.forward*.2f,normal).normalized;
                var flat=Quaternion.FromToRotation(vane.TransformDirection(axis),outward)*vane.rotation;
                vane.rotation=Quaternion.Slerp(vane.rotation,flat,weight);
            }
        }
        public void SetForm(BodyForm form)
        {
            if(vanes==null||folded==null||vanes.Length!=folded.Length)return;
            float angle=form==BodyForm.Flight?65:form==BodyForm.CivilFlight?35:0;
            for(int i=0;i<vanes.Length;i++)
            {
                var vane=vanes[i];if(vane==null)continue;
                var swivelAxis=vane.parent.InverseTransformDirection(transform.up);
                var spreadAxis=vane.parent.InverseTransformDirection(transform.forward);
                float side=Mathf.Sign(transform.InverseTransformPoint(vane.position).x);
                var center=Vector3.Scale(vane.GetComponent<MeshFilter>().sharedMesh.bounds.center,vane.localScale);
                Quaternion best=folded[i];float outward=float.NegativeInfinity;
                foreach(float swivel in new[]{-angle,angle})foreach(float spread in new[]{-angle*.85f,angle*.85f})
                {
                    var candidate=Quaternion.AngleAxis(spread,spreadAxis)*Quaternion.AngleAxis(swivel,swivelAxis)*folded[i];
                    var world=vane.parent.TransformPoint(vane.localPosition+candidate*center);
                    float width=transform.InverseTransformPoint(world).x*side;
                    if(width>outward){outward=width;best=candidate;}
                }
                vane.localRotation=best;
            }
        }
    }
}
