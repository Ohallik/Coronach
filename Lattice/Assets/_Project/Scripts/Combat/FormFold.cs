using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    // The visible generated meshes and their attached vanes retain full scale.
    // A temporary local pose folds them around their own existing hinges.
    public sealed class FormFold : MonoBehaviour
    {
        sealed class Surface
        {
            public MeshFilter filter;public Mesh original,folded;
            public Vector3[] vertices,normals,result,normalResult;
            public Vector4[] tangents,tangentResult;
            public Matrix4x4 toRoot,fromRoot,normalToRoot,normalFromRoot;
        }
        readonly List<Surface> surfaces=new();
        readonly List<Transform> joints=new();readonly List<Quaternion> rotations=new();readonly List<Vector3> jointPositions=new();
        Animator animator;GeneratedVanes vanes;
        Vector3 position,pivot;Quaternion rotation;
        bool prepared,ship,launch;
        float previousWeight=-1;
        public void Prepare(bool flightTransition)
        {
            if(prepared)return;prepared=true;launch=flightTransition;
            position=transform.localPosition;rotation=transform.localRotation;
            animator=GetComponentInChildren<Animator>();vanes=GetComponent<GeneratedVanes>();
            ship=GetComponent<FlightShipMotion>()!=null;
            if(animator!=null&&animator.isHuman)
            {
                foreach(var joint in animator.GetComponentsInChildren<Transform>())
                {joints.Add(joint);rotations.Add(joint.localRotation);jointPositions.Add(joint.localPosition);}
                pivot=transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Hips).position);
            }
            if(ship&&surfaces.Count==0)
            {
                foreach(var filter in GetComponentsInChildren<MeshFilter>())
                {
                    var mesh=filter.sharedMesh;if(mesh==null)continue;
                    var toRoot=transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                    var surface=new Surface{filter=filter,original=mesh,folded=Instantiate(mesh),vertices=mesh.vertices,normals=mesh.normals,tangents=mesh.tangents,toRoot=toRoot,fromRoot=toRoot.inverse};
                    surface.normalToRoot=toRoot.inverse.transpose;surface.normalFromRoot=toRoot.transpose;
                    surface.folded.name=mesh.name+" transition pose";surface.folded.MarkDynamic();
                    surface.result=new Vector3[surface.vertices.Length];surface.normalResult=new Vector3[surface.normals.Length];surface.tangentResult=new Vector4[surface.tangents.Length];surfaces.Add(surface);
                }
            }
            previousWeight=-1;
        }
        public void Apply(float weight)
        {
            if(!prepared)return;
            weight=Mathf.Clamp01(weight);
            if(ship){FoldHull(weight);return;}
            // The animator is held during this short staged pose. Restore the
            // captured bones first so repeated/reversed updates never accumulate.
            for(int i=0;i<joints.Count;i++){joints[i].localRotation=rotations[i];joints[i].localPosition=jointPositions[i];}
            transform.localPosition=position;transform.localRotation=rotation;
            if(animator!=null&&animator.isHuman)
            {
                for(int sideIndex=0;sideIndex<2;sideIndex++)
                {
                    bool left=sideIndex==0;float side=left?-1:1;
                    var arm=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
                    var elbow=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
                    var hand=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
                    Aim(arm,elbow,new Vector3(side*.14f,-.25f,.33f),weight);
                    Aim(elbow,hand,new Vector3(-side*.08f,.22f,.28f),weight);
                }
            }
            if(vanes!=null&&vanes.vanes!=null)
                for(int i=0;i<vanes.vanes.Length;i++)
                {
                    var vane=vanes.vanes[i];vane.localRotation=vanes.folded[i];
                    var bounds=vane.GetComponent<MeshFilter>().sharedMesh.bounds;
                    Vector3 axis=bounds.size.y>=bounds.size.x&&bounds.size.y>=bounds.size.z?Vector3.up:bounds.size.z>=bounds.size.x?Vector3.forward:Vector3.right;
                    var along=vane.TransformDirection(axis);float side=Mathf.Sign(transform.InverseTransformPoint(vane.position).x);
                    var folded=transform.TransformDirection(new Vector3(side*.08f,.15f,-1)).normalized;
                    if(Vector3.Dot(along,folded)<0)folded=-folded;
                    vane.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(along,folded),weight)*vane.rotation;
                }
            var tilt=Quaternion.Euler((launch?72:14)*weight,0,0);
            transform.localRotation=rotation*tilt;
            transform.localPosition=position+rotation*(pivot-tilt*pivot);
        }
        void Aim(Transform joint,Transform child,Vector3 direction,float weight)
        {
            if(joint==null||child==null)return;
            var turn=Quaternion.FromToRotation(child.position-joint.position,transform.TransformDirection(direction));
            joint.rotation=Quaternion.Slerp(Quaternion.identity,turn,weight)*joint.rotation;
        }
        void FoldHull(float weight)
        {
            if(Mathf.Abs(previousWeight-weight)<.00001f)return;previousWeight=weight;
            foreach(var surface in surfaces)
            {
                if(weight<=0){surface.filter.sharedMesh=surface.original;continue;}
                for(int i=0;i<surface.vertices.Length;i++)
                {
                    var point=surface.toRoot.MultiplyPoint3x4(surface.vertices[i]);
                    float side=Mathf.Sign(point.x),amount=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.38f,.68f,Mathf.Abs(point.x)));
                    // Smooth weights preserve the generated continuous wing root;
                    // the outboard surface folds around its longitudinal hinge.
                    var hinge=new Vector3(side*.38f,.12f,0);var turn=Quaternion.AngleAxis(side*68*weight*amount,Vector3.forward);
                    surface.result[i]=surface.fromRoot.MultiplyPoint3x4(hinge+turn*(point-hinge));
                    if(i<surface.normals.Length)surface.normalResult[i]=surface.normalFromRoot.MultiplyVector(turn*surface.normalToRoot.MultiplyVector(surface.normals[i])).normalized;
                    if(i<surface.tangents.Length)
                    {
                        var t=surface.tangents[i];var tangent=surface.fromRoot.MultiplyVector(turn*surface.toRoot.MultiplyVector(new Vector3(t.x,t.y,t.z))).normalized;
                        surface.tangentResult[i]=new Vector4(tangent.x,tangent.y,tangent.z,t.w);
                    }
                }
                surface.folded.vertices=surface.result;
                if(surface.normals.Length==surface.vertices.Length)surface.folded.normals=surface.normalResult;
                if(surface.tangents.Length==surface.vertices.Length)surface.folded.tangents=surface.tangentResult;
                surface.folded.RecalculateBounds();surface.filter.sharedMesh=surface.folded;
            }
        }
        public void Restore()
        {
            if(!prepared)return;Apply(0);prepared=false;joints.Clear();rotations.Clear();jointPositions.Clear();
            if(vanes!=null)vanes.SetForm(Lattice.Data.BodyForm.Shaped);
        }
        void OnDestroy(){foreach(var surface in surfaces)if(surface.folded!=null){if(Application.isPlaying)Destroy(surface.folded);else DestroyImmediate(surface.folded);}}
    }
}
