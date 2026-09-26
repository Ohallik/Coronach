using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>Bounded two-bone corrections on the generated body. The donor
    /// supplies weight, knee bend, foot roll and swing; actual travel supplies
    /// direction/stride, and a stance contact holds its world-space sole.</summary>
    [DefaultExecutionOrder(500)]
    public sealed class GroundFeet : MonoBehaviour
    {
        sealed class Leg
        {
            public Transform thigh,knee,foot,toe;
            public Vector3 heelOffset,toeOffset,anchor;
            public Vector3 target,pole;
            public Quaternion rotation;
            public float side;
            public int contact=-1;
        }
        Animator animator;
        Transform heading,pelvis;
        Transform[] spine;
        GroundStrideProfile profile;
        Leg left,right;
        Vector3 previousPosition,previousDirection=Vector3.forward;
        Quaternion previousRotation;
        string previousClip;
        bool previousReverse;
        GeneratedAnimator driver;
        CombatActor actor;
        FormController form;
        public float MaximumReachCorrection {get;private set;}
        public float RequestedSupportDrop {get;private set;}
        void OnEnable(){ResetContacts();}

        public void Initialize(Animator rig,Transform root,GroundStrideProfile calibration)
        {
            animator=rig;heading=root;profile=calibration;
            pelvis=animator.GetBoneTransform(HumanBodyBones.Hips);
            var spineBones=new System.Collections.Generic.List<Transform>();
            foreach(var bone in new[]{HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest})
            {var joint=animator.GetBoneTransform(bone);if(joint!=null)spineBones.Add(joint);}
            spine=spineBones.ToArray();
            left=Create(true);right=Create(false);
            previousPosition=heading.position;previousRotation=heading.rotation;
            driver=GetComponent<GeneratedAnimator>();actor=GetComponentInParent<CombatActor>();form=GetComponentInParent<FormController>();
        }
        Leg Create(bool isLeft)=>new()
        {
            thigh=animator.GetBoneTransform(isLeft?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg),
            knee=animator.GetBoneTransform(isLeft?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg),
            foot=animator.GetBoneTransform(isLeft?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot),
            toe=animator.GetBoneTransform(isLeft?HumanBodyBones.LeftToes:HumanBodyBones.RightToes),
            side=isLeft?-1:1,heelOffset=isLeft?profile.leftHeel:profile.rightHeel,
            toeOffset=isLeft?profile.leftToe:profile.rightToe
        };
        void LateUpdate()
        {
            if(profile==null||driver==null||actor==null)return;
            bool locomotion=actor.Health.Alive&&!actor.flight&&(form==null||!form.Shaping)&&driver.IsLocomotion;
            if(!locomotion){ResetContacts();return;}
            var info=animator.GetCurrentAnimatorStateInfo(0);
            Correct(actor.motor.Velocity,driver.CurrentAnimation,info.normalizedTime,driver.StrideScale,
                animator.IsInTransition(0),GameTime.Paused,driver.ReverseLocomotion);
        }
        public void ResetContacts(){if(left!=null)left.contact=right.contact=-1;previousClip=null;previousDirection=Vector3.forward;}
        public void Correct(Vector3 velocity,string clip,float phase,float stride,bool transitioning,bool paused=false,bool reverse=false)
        {
            if(profile==null)return;
            bool reset=previousClip!=clip||previousReverse!=reverse||Vector3.Distance(previousPosition,heading.position)>.75f||Quaternion.Angle(previousRotation,heading.rotation)>40;
            if(reset||transitioning){left.contact=right.contact=-1;}
            previousClip=clip;previousReverse=reverse;previousPosition=heading.position;previousRotation=heading.rotation;
            var local=heading.InverseTransformDirection(velocity);local.y=0;
            if(local.sqrMagnitude>.0025f)previousDirection=local.normalized;
            else if(!paused)previousDirection=Vector3.RotateTowards(previousDirection,Vector3.forward,6*Time.unscaledDeltaTime,0);
            // Paused animation is evaluated again by Unity. Reapply the same
            // correction without creating new anchors or advancing any clock.
            bool moving=paused||velocity.sqrMagnitude>.01f;
            float cycle=Mathf.Repeat(phase,1);
            // Turn the lower body toward its step, distributing the opposing
            // aim twist across the spine. A reverse take steps backward with
            // normal left/right leg ordering; mirroring its foot path crossed
            // the shins despite apparently excellent contact measurements.
            Vector3 gaitDirection=previousDirection*(reverse?-1:1);
            float yaw=Mathf.Clamp(Mathf.Atan2(gaitDirection.x,gaitDirection.z)*Mathf.Rad2Deg,-90,90);
            pelvis.rotation=Quaternion.AngleAxis(yaw,heading.up)*pelvis.rotation;
            foreach(var joint in spine)joint.rotation=Quaternion.AngleAxis(-yaw/spine.Length,heading.up)*joint.rotation;
            Quaternion lowerRotation=heading.rotation*Quaternion.Euler(0,yaw,0);
            Vector3 legDirection=Quaternion.Euler(0,-yaw,0)*gaitDirection;
            // A longer brisk-walk step needs knee room. Lower the animated
            // pelvis by at most eight centimetres instead of stretching a leg
            // past its chain length or accelerating the entire take further.
            if(clip=="Walk")pelvis.position-=heading.up*Mathf.Clamp((stride-1)*.18f,0,.08f);
            MaximumReachCorrection=0;
            float directional=Mathf.Max(Mathf.Abs(yaw)/90,reverse?1:0);
            Apply(left,clip,cycle,stride,moving&&!transitioning,legDirection,lowerRotation,directional);
            Apply(right,clip,Mathf.Repeat(cycle-(clip=="Sprint"?.55f:.5f),1),stride,moving&&!transitioning,legDirection,lowerRotation,directional);
            // Uphill motion raises the root over a rear foot. Keep both final
            // ankle targets inside their real chain length, including landing
            // and toe-off, rather than lifting an anchor or snapping a knee.
            RequestedSupportDrop=Mathf.Max(SupportDrop(left),SupportDrop(right));
            float supportDrop=Mathf.Clamp(RequestedSupportDrop,0,.14f);
            pelvis.position-=Vector3.up*supportDrop;
            Complete(left,directional);Complete(right,directional);
        }
        void Complete(Leg leg,float directional)
        {
            MaximumReachCorrection=Mathf.Max(MaximumReachCorrection,Solve(leg,leg.target,leg.pole,directional));
            leg.foot.rotation=leg.rotation;
        }
        void Apply(Leg leg,string clip,float phase,float stride,bool canPlant,Vector3 direction,Quaternion lowerRotation,float directional)
        {
            Vector3 originalFoot=leg.foot.position;
            Quaternion footRotation=leg.foot.rotation;
            Vector3 local=Quaternion.Inverse(lowerRotation)*(originalFoot-heading.position);
            Vector3 warped=local;
            warped.x=local.x+direction.x*local.z*stride;
            warped.z=direction.z*local.z*stride;
            // A deliberate side/back step needs a base of support. Keep each
            // foot on its own side of the pelvis instead of letting the donor's
            // narrow forward swing cross a foot held in a lateral stance.
            float side=Mathf.Max(warped.x*leg.side,.14f);
            warped.x=Mathf.Lerp(warped.x,side*leg.side,directional);
            Vector3 target=heading.position+lowerRotation*warped;
            Vector3 heel=leg.foot.TransformPoint(leg.heelOffset),toe=leg.toe.TransformPoint(leg.toeOffset);
            int contact=-1;float weight=0;
            if(canPlant)
            {
                if(clip=="Walk")
                {
                    if(phase<.135f){contact=0;weight=Window(phase,0,.135f,.025f);}
                    // Release for the authored toe-off before the opposite
                    // heel lands. Holding through half a cycle overextends
                    // the trailing leg as the root climbs a slope.
                    else if(phase<.45f){contact=1;weight=Window(phase,.135f,.45f,.04f);}
                }
                else if(clip=="Run"||clip=="Sprint")
                {
                    contact=1;weight=Window(phase,clip=="Run"?.035f:.055f,clip=="Run"?.22f:.2f,.025f);
                    if(weight<=0)contact=-1;
                }
            }
            Vector3 marker=contact==0?heel:toe;
            Vector3 proposed=marker+target-originalFoot;
            if(Physics.Raycast(proposed+Vector3.up*.65f,Vector3.down,out var ground,1.4f,~0,QueryTriggerInteraction.Ignore)&&ground.point.y<=heading.position.y+.4f)
            {
                if(contact>=0)
                {
                    if(contact==1&&weight>0)
                    {
                        Vector3 soleAxis=toe-heel;
                        Vector3 flat=Vector3.ProjectOnPlane(soleAxis,ground.normal);
                        if(flat.sqrMagnitude>.001f)
                        {
                            footRotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(soleAxis,flat),weight)*footRotation;
                            leg.foot.rotation=footRotation;
                            heel=leg.foot.TransformPoint(leg.heelOffset);toe=leg.toe.TransformPoint(leg.toeOffset);
                            marker=toe;proposed=marker+target-originalFoot;
                        }
                    }
                    if(leg.contact!=contact)leg.anchor=new Vector3(proposed.x,ground.point.y+.006f,proposed.z);
                    target=Vector3.Lerp(target,leg.anchor-(marker-originalFoot),weight);
                }
                // Do not push the visible heel/forefoot below a flat deck during
                // swing or a transition. Ground normals retain authored foot roll.
                // Compare both soles against the local support plane at their
                // final positions. Reusing the ray's height at the animated
                // swing point pushed a planted foot uphill on every frame.
                Vector3 offset=target-originalFoot;
                float clearance=Mathf.Min(Vector3.Dot(ground.normal,heel+offset-ground.point),
                    Vector3.Dot(ground.normal,toe+offset-ground.point));
                target.y+=Mathf.Max(0,.003f-clearance)/Mathf.Max(.2f,ground.normal.y);
            }
            else contact=-1;
            leg.contact=contact;
            leg.target=target;leg.pole=lowerRotation*new Vector3(leg.side*.3f,0,1);
            leg.rotation=footRotation;
        }
        static float SupportDrop(Leg leg)
        {
            float length=Vector3.Distance(leg.thigh.position,leg.knee.position)+Vector3.Distance(leg.knee.position,leg.foot.position)-.002f;
            Vector3 delta=leg.thigh.position-leg.target;
            float horizontal=delta.x*delta.x+delta.z*delta.z;
            if(horizontal>=length*length)return 0;
            float vertical=Mathf.Sqrt(length*length-horizontal);
            return Mathf.Max(0,delta.y-vertical);
        }
        static float Window(float phase,float start,float end,float edge)=>
            Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Min((phase-start)/edge,(end-phase)/edge)));
        static float Solve(Leg leg,Vector3 target,Vector3 pole,float directional)
        {
            Vector3 origin=leg.thigh.position,knee=leg.knee.position,foot=leg.foot.position;
            float upper=Vector3.Distance(origin,knee),lower=Vector3.Distance(knee,foot);
            Vector3 delta=target-origin;float requested=delta.magnitude;
            if(requested<.001f||upper<.001f||lower<.001f)return 0;
            Vector3 axis=delta/requested;
            float distance=Mathf.Clamp(requested,Mathf.Abs(upper-lower)+.001f,upper+lower-.001f);
            Vector3 bend=Vector3.ProjectOnPlane(knee-origin,axis).normalized;
            if(bend.sqrMagnitude<.1f)bend=Vector3.ProjectOnPlane(leg.thigh.forward,axis).normalized;
            var deliberateBend=Vector3.ProjectOnPlane(pole,axis).normalized;
            bend=Vector3.Slerp(bend,deliberateBend,directional*.75f).normalized;
            float along=(upper*upper+distance*distance-lower*lower)/(2*distance);
            float outward=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            Vector3 desiredKnee=origin+axis*along+bend*outward;
            leg.thigh.rotation=Quaternion.FromToRotation(knee-origin,desiredKnee-origin)*leg.thigh.rotation;
            Vector3 reached=origin+axis*distance;
            leg.knee.rotation=Quaternion.FromToRotation(leg.foot.position-leg.knee.position,reached-leg.knee.position)*leg.knee.rotation;
            return Mathf.Abs(requested-distance);
        }
    }
}
