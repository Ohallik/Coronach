using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Last Warden struck open (its guard broken by blows, or one of its own openings: its staff fouled in a fibre, its staff ringing in the floor) staggers where it stands, on its
    /// feet. It bows from the hips over its staff, its head drops, its knees give and its hips sink a little, the staff's point comes down to the floor before it to hold it up and
    /// its other arm goes out for its balance; its legs stay under it and both feet stay where they stood. November 20 (s41/036, 037): through "OPENING · STRIKE NOW" it hung rigid at
    /// about forty degrees, a falling plank with Idra standing inside its legs, and in a yard it stood in the shared Stagger frame with its head not a hand lower than at rest, so
    /// nothing in its body said it had been struck.
    /// <para>The pose is made each frame over whatever its animation gives it (GeneratedAnimator holds the shared Stagger frame while it is broken): the drawn body's root is held
    /// upright on its feet, the back and the staff arm are turned from the animated pose, and each leg is a two-bone reach from its hip to where its foot was, the knee bending
    /// forward. Eased in over a fifth of a second and out over a third. Added by the boss's own start (BossController.LastWarden); it does nothing to a body with no human rig.</para>
    /// </summary>
    [DefaultExecutionOrder(31000)]
    public sealed class WardenStagger:MonoBehaviour
    {
        /// <summary>The bow, in degrees: the back from the hips, the neck and the head; and how far the hips sink, in metres.</summary>
        public const float BackBow=24,NeckBow=10,HeadBow=16,Sink=.26f;
        /// <summary>How far into the stagger it is: nought standing, one staggered.</summary>
        public float Weight{get;private set;}
        Health health;Animator animator;Transform visual;Quaternion visualRest;
        Transform hips,neck,head,staffUpper,staffFore,staffHand,freeUpper,freeFore,freeHand;Transform[] back=new Transform[0];
        (Transform upper,Transform lower,Transform foot)[] legs=new (Transform,Transform,Transform)[0];
        void Start()
        {
            health=GetComponent<Health>();var presentation=GetComponent<DefeatPresentation>();visual=presentation!=null?presentation.visual:null;
            animator=GetComponentInChildren<Animator>();if(health==null||visual==null||animator==null||!animator.isHuman){enabled=false;return;}
            visualRest=visual.localRotation;
            Transform B(HumanBodyBones bone)=>animator.GetBoneTransform(bone);
            hips=B(HumanBodyBones.Hips);neck=B(HumanBodyBones.Neck);head=B(HumanBodyBones.Head);
            back=new[]{B(HumanBodyBones.Spine),B(HumanBodyBones.Chest),B(HumanBodyBones.UpperChest)};
            staffUpper=B(HumanBodyBones.RightUpperArm);staffFore=B(HumanBodyBones.RightLowerArm);staffHand=B(HumanBodyBones.RightHand);
            freeUpper=B(HumanBodyBones.LeftUpperArm);freeFore=B(HumanBodyBones.LeftLowerArm);freeHand=B(HumanBodyBones.LeftHand);
            legs=new[]{(B(HumanBodyBones.LeftUpperLeg),B(HumanBodyBones.LeftLowerLeg),B(HumanBodyBones.LeftFoot)),(B(HumanBodyBones.RightUpperLeg),B(HumanBodyBones.RightLowerLeg),B(HumanBodyBones.RightFoot))};
            if(hips==null)enabled=false;
        }
        void LateUpdate()
        {
            if(GameTime.Paused)return;
            bool open=health.Alive&&health.Broken;
            Weight=Mathf.MoveTowards(Weight,open?1:0,Time.deltaTime/(open?.2f:.35f));
            if(Weight<=0)return;
            float k=Mathf.SmoothStep(0,1,Weight);
            // Upright on its feet: whatever leaned the drawn body as a whole lets it go while it staggers.
            visual.localRotation=Quaternion.Slerp(visual.localRotation,visualRest,k);
            var f=transform.forward;f.y=0;f=f.sqrMagnitude>1e-4f?f.normalized:Vector3.forward;var u=Vector3.up;var r=Vector3.Cross(u,f);
            // Where its feet are now, before the hips sink: they stay there.
            var feet=new Vector3[legs.Length];var footTurn=new Quaternion[legs.Length];
            for(int i=0;i<legs.Length;i++)if(legs[i].foot!=null){feet[i]=legs[i].foot.position;footTurn[i]=legs[i].foot.rotation;}
            hips.position+=Vector3.down*(Sink*k);
            for(int i=0;i<legs.Length;i++)Reach(legs[i].upper,legs[i].lower,legs[i].foot,feet[i],f);
            for(int i=0;i<legs.Length;i++)if(legs[i].foot!=null)legs[i].foot.rotation=footTurn[i];
            // The bow, from the hips up.
            float[] share={.4f,.35f,.25f};
            for(int i=0;i<back.Length;i++)if(back[i]!=null)back[i].rotation=Quaternion.AngleAxis(BackBow*share[i]*k,r)*back[i].rotation;
            if(neck!=null)neck.rotation=Quaternion.AngleAxis(NeckBow*k,r)*neck.rotation;
            if(head!=null)head.rotation=Quaternion.AngleAxis(HeadBow*k,r)*head.rotation;
            // The staff arm brings its point down to the floor before it, to hold it up; the other arm goes out for its balance.
            Aim(staffUpper,staffFore,-u+.5f*f+.18f*r,k);Aim(staffFore,staffHand,-.9f*u+.55f*f+.05f*r,k);
            Aim(freeUpper,freeFore,-u-.55f*r+.15f*f,k);Aim(freeFore,freeHand,-u-.3f*r+.35f*f,k);
        }
        /// <summary>A leg as a two-bone reach from its hip to where its foot is to stay, the knee bending toward <paramref name="forward"/>.</summary>
        static void Reach(Transform upper,Transform lower,Transform foot,Vector3 target,Vector3 forward)
        {
            if(upper==null||lower==null||foot==null)return;
            var hip=upper.position;float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,foot.position);
            var to=target-hip;float d=Mathf.Clamp(to.magnitude,.01f,a+b-.001f);var dir=to.normalized;
            float x=(a*a-b*b+d*d)/(2*d),h=Mathf.Sqrt(Mathf.Max(0,a*a-x*x));
            var bend=forward-dir*Vector3.Dot(forward,dir);bend=bend.sqrMagnitude>1e-6f?bend.normalized:forward;
            var knee=hip+dir*x+bend*h;
            upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,knee-hip)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(foot.position-lower.position,target-lower.position)*lower.rotation;
        }
        static void Aim(Transform bone,Transform next,Vector3 toward,float k)
        {
            if(bone==null||next==null)return;var along=next.position-bone.position;if(along.sqrMagnitude<1e-8f)return;
            var to=Quaternion.FromToRotation(along,toward.normalized)*bone.rotation;bone.rotation=Quaternion.Slerp(bone.rotation,to,k);
        }
    }
}
