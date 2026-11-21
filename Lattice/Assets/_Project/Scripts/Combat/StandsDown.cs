using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A humanoid body that has stood down (chapter 7: the Last Warden at the fourth clamp, and the Warden the engine hall shows on every later visit). Over <see cref="seconds"/> its
    /// staff arm comes in front of it with the staff's point lowered toward the floor, its other arm hangs at its side, its back and neck bow and its head drops; then it holds there.
    /// No clip is needed and none is bought: the pose is made here from the rig's own bind pose (the avatar's T-pose), so it comes out the same whatever the body was doing when it
    /// stood down, and the staff (part of the mesh, carried by the hand) follows the hand. Only the bones it poses are its (the back, neck, head, shoulders and arms): the hips and
    /// legs are the animation's, so it stands. It runs last, after the body's animation and GeneratedAnimator.
    /// <para><b>The fight's and the room's.</b> The fight puts one on the boss as it stands down (<see cref="fought"/>, eased): the body's GeneratedAnimator is switched off (a body that
    /// yields would otherwise go down on one knee, its yield frame) and it is left standing in its idle, solid again where it stands, turning to <see cref="face"/>. The room puts one,
    /// lowered at once, on the body it shows from then on (OrielBuilder.GlassChoir: "The Warden, stood down", under a FlagGate on bossdown). That body is shown from the moment the
    /// boss is down, so while a fought body of the same <see cref="id"/> stands in the same scene the room's is hidden (its renderers, and its colliders and its holder's): one Warden
    /// stands. On every later load there is no fought body, and the room's stands where the contract puts it (docs/maps/GlassChoir.md, "Boss arena contract").</para>
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class StandsDown:MonoBehaviour
    {
        /// <summary>The boss it is (its EnemyDef id): the room's body and the fight's are paired by it.</summary>
        public string id;
        /// <summary>How long it takes to lower the staff and bow; nought, lowered at once (the room's).</summary>
        public float seconds;
        /// <summary>The fight's own body, standing down where it fought.</summary>
        public bool fought;
        /// <summary>The way it turns to as it stands down, on the floor; none, it keeps its facing.</summary>
        public Vector3 face;
        /// <summary>Whether the staff is in its right hand (the Warden's is).</summary>
        public bool staffInRight=true;

        /// <summary>Its back, neck and head bow forward by these, in degrees: about thirty-five in all from the hips to the eyes.</summary>
        public const float BackBow=3.5f,NeckBow=8,HeadBow=16;
        static readonly List<StandsDown> Standing=new();
        /// <summary>Done lowering: the pose is held from here on.</summary>
        public bool Lowered{get;private set;}
        /// <summary>Hidden for a fought body of the same boss standing in the scene.</summary>
        public bool Hidden{get;private set;}
        Animator animator;bool begun;float began;Quaternion fromFacing,toFacing;
        readonly List<(Transform bone,Quaternion target)> pose=new();
        Renderer[] renderers;Collider[] colliders;

        void OnEnable(){if(!Standing.Contains(this))Standing.Add(this);}
        void OnDisable()=>Standing.Remove(this);
        /// <summary>A body that yields would go down to its yield frame and slump there (GeneratedAnimator): this one stands, in its idle.</summary>
        void Quiet()
        {
            foreach(var rig in GetComponentsInChildren<GeneratedAnimator>())rig.enabled=false;
            var a=GetComponentInChildren<Animator>();
            if(a!=null&&a.runtimeAnimatorController!=null){a.speed=1;if(a.HasState(0,Animator.StringToHash("Idle")))a.CrossFadeInFixedTime("Idle",.35f,0);}
        }
        void Begin()
        {
            begun=true;began=Time.time;if(fought)Quiet();
            var flat=face;flat.y=0;fromFacing=transform.rotation;toFacing=flat.sqrMagnitude>.0001f?Quaternion.LookRotation(flat.normalized,Vector3.up):fromFacing;
            renderers=GetComponentsInChildren<Renderer>(true);
            var own=new List<Collider>(GetComponentsInChildren<Collider>(true));if(!fought&&transform.parent!=null)own.AddRange(transform.parent.GetComponents<Collider>());colliders=own.ToArray();
            animator=GetComponentInChildren<Animator>();
            if(animator==null||animator.avatar==null||!animator.isHuman)return;
            Transform Bone(HumanBodyBones b)=>animator.GetBoneTransform(b);
            var hips=Bone(HumanBodyBones.Hips);
            var back=new[]{Bone(HumanBodyBones.Spine),Bone(HumanBodyBones.Chest),Bone(HumanBodyBones.UpperChest)};
            Transform neck=Bone(HumanBodyBones.Neck),head=Bone(HumanBodyBones.Head);
            HumanBodyBones S(HumanBodyBones right,HumanBodyBones left,bool staff)=>staff==staffInRight?right:left;
            Transform staffShoulder=Bone(S(HumanBodyBones.RightShoulder,HumanBodyBones.LeftShoulder,true)),staffUpper=Bone(S(HumanBodyBones.RightUpperArm,HumanBodyBones.LeftUpperArm,true)),
                staffFore=Bone(S(HumanBodyBones.RightLowerArm,HumanBodyBones.LeftLowerArm,true)),staffHand=Bone(S(HumanBodyBones.RightHand,HumanBodyBones.LeftHand,true));
            Transform freeShoulder=Bone(S(HumanBodyBones.RightShoulder,HumanBodyBones.LeftShoulder,false)),freeUpper=Bone(S(HumanBodyBones.RightUpperArm,HumanBodyBones.LeftUpperArm,false)),
                freeFore=Bone(S(HumanBodyBones.RightLowerArm,HumanBodyBones.LeftLowerArm,false)),freeHand=Bone(S(HumanBodyBones.RightHand,HumanBodyBones.LeftHand,false));
            var posed=new List<Transform>();
            foreach(var t in new[]{back[0],back[1],back[2],neck,head,staffShoulder,staffUpper,staffFore,staffHand,freeShoulder,freeUpper,freeFore,freeHand})if(t!=null&&!posed.Contains(t))posed.Add(t);
            // The pose is made with the body turned the way it will end and its hips as bound (upright and square: whatever the clip had them doing this frame, a lunge's twist, isn't
            // baked into the arms), from the bind pose, and kept as each bone's rotation under its parent; then everything is put back. The hips stay the animation's.
            var now=new Quaternion[posed.Count];for(int i=0;i<posed.Count;i++)now[i]=posed[i].localRotation;
            var hipsNow=hips!=null?hips.localRotation:Quaternion.identity;
            var bind=new Dictionary<string,Quaternion>();
            try{foreach(var bone in animator.avatar.humanDescription.skeleton)if(!string.IsNullOrEmpty(bone.name))bind[bone.name]=bone.rotation;}catch(System.Exception){bind.Clear();}
            transform.rotation=toFacing;
            if(hips!=null&&bind.TryGetValue(hips.name,out var bound))hips.localRotation=bound;
            foreach(var t in posed)if(bind.TryGetValue(t.name,out var q))t.localRotation=q;
            var f=transform.forward;f.y=0;f.Normalize();var u=Vector3.up;var r=Vector3.Cross(u,f);float side=staffInRight?1:-1;
            foreach(var t in back)if(t!=null)t.rotation=Quaternion.AngleAxis(BackBow,r)*t.rotation;
            if(neck!=null)neck.rotation=Quaternion.AngleAxis(NeckBow,r)*neck.rotation;
            if(head!=null)head.rotation=Quaternion.AngleAxis(HeadBow,r)*head.rotation;
            // The staff arm: the upper arm down beside it, the forearm brought forward and down; the wrist as bound, so the staff runs on from the forearm, its point toward the floor
            // ahead of it.
            Aim(staffUpper,staffFore,-u+.25f*f+.12f*side*r);Aim(staffFore,staffHand,-.8f*u+.6f*f+.04f*side*r);
            // The other hangs at its side, the elbow a little bent.
            Aim(freeUpper,freeFore,-u+.06f*f-.14f*side*r);Aim(freeFore,freeHand,-u+.25f*f-.06f*side*r);
            foreach(var t in posed)pose.Add((t,t.localRotation));
            transform.rotation=fromFacing;for(int i=0;i<posed.Count;i++)posed[i].localRotation=now[i];if(hips!=null)hips.localRotation=hipsNow;
        }
        static void Aim(Transform bone,Transform next,Vector3 toward)
        {
            if(bone==null||next==null)return;var along=next.position-bone.position;if(along.sqrMagnitude<1e-8f||toward.sqrMagnitude<1e-8f)return;
            bone.rotation=Quaternion.FromToRotation(along,toward.normalized)*bone.rotation;
        }
        void LateUpdate()
        {
            if(!begun)Begin();
            // The fight's own body is solid again where it stands (its brain let its capsule go when it fell): a Warden stood down is not a thing to walk through.
            if(fought&&TryGetComponent<CharacterController>(out var capsule)&&!capsule.enabled)capsule.enabled=true;
            bool hide=false;
            if(!fought)foreach(var other in Standing)if(other!=null&&other!=this&&other.fought&&other.id==id&&other.isActiveAndEnabled&&other.gameObject.scene==gameObject.scene){hide=true;break;}
            if(hide!=Hidden)
            {
                Hidden=hide;
                foreach(var part in renderers)if(part!=null)part.enabled=!hide;
                foreach(var part in colliders)if(part!=null&&!(part is CharacterController))part.enabled=!hide;
            }
            float k=seconds<=0?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-began)/seconds));
            if(fromFacing!=toFacing)transform.rotation=Quaternion.Slerp(fromFacing,toFacing,k);
            // From whatever the animation gives it this frame toward the pose; at the end, the pose.
            foreach(var (bone,target) in pose)if(bone!=null)bone.localRotation=k>=1?target:Quaternion.Slerp(bone.localRotation,target,k);
            Lowered=k>=1;
        }
    }
}
