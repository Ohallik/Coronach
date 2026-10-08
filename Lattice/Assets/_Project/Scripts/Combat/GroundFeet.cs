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
            public bool soundReady;
            public float weight;
            public Collider ground;
            public Vector3 surfacePoint,surfaceNormal;
        }
        Animator animator;
        Transform heading,pelvis;
        Transform[] spine;
        GroundStrideProfile profile;
        Leg left,right;
        Vector3 previousPosition,previousDirection=Vector3.forward;
        Quaternion previousRotation;
        string previousClip;
        bool previousReverse,previousTargetFacing;
        float smoothedSupportDrop;
        GeneratedAnimator driver;
        CombatActor actor;
        FormController form;
        CharacterController controller;
        public float MaximumReachCorrection {get;private set;}
        public float RequestedSupportDrop {get;private set;}
        public float TerrainGrade {get;private set;}
        Vector3 supportUp=Vector3.up;
        Vector3 bodyOrigin;
        float terrainWeight;
        Vector3 smoothedTerrainNormal=Vector3.up;
        float smoothedClearance;
        float smoothedStride=1;
        Vector3 freeDirection=Vector3.forward;
        float turnRecovery;
        float balanceWeight;
        Vector3 balanceUp=Vector3.up;
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
            driver=GetComponent<GeneratedAnimator>();actor=GetComponentInParent<CombatActor>();form=GetComponentInParent<FormController>();controller=actor!=null?actor.GetComponent<CharacterController>():null;
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
            Correct(actor.motor.Velocity,driver.LocomotionAnimation,info.normalizedTime,driver.StrideScale,
                animator.IsInTransition(0),GameTime.Paused,driver.ReverseLocomotion,actor.TargetLocked);
        }
        public void ResetContacts(){if(left!=null){left.contact=right.contact=-1;left.soundReady=right.soundReady=false;}previousClip=null;previousDirection=Vector3.forward;}
        public void Correct(Vector3 velocity,string clip,float phase,float stride,bool transitioning,bool paused=false,bool reverse=false,bool targetFacing=true,float deltaTime=-1)
        {
            if(profile==null)return;
            bool reset=previousClip==null||previousReverse!=reverse||previousTargetFacing!=targetFacing||Vector3.Distance(previousPosition,heading.position)>.75f||Quaternion.Angle(previousRotation,heading.rotation)>40;
            float correctionDelta=Mathf.Max(0,deltaTime<0?Time.unscaledDeltaTime:deltaTime);
            if(reset)turnRecovery=0;
            else if(!paused)turnRecovery=Quaternion.Angle(previousRotation,heading.rotation)>.5f?.2f:Mathf.Max(0,turnRecovery-correctionDelta);
            if(reset||previousClip!=clip||transitioning)left.contact=right.contact=-1;
            if(reset)left.soundReady=right.soundReady=false;
            if(reset)smoothedStride=stride;
            else if(!paused)smoothedStride=Mathf.Lerp(smoothedStride,stride,1-Mathf.Exp(-Mathf.Max(0,deltaTime<0?Time.unscaledDeltaTime:deltaTime)/.04f));
            stride=smoothedStride;
            previousClip=clip;previousReverse=reverse;previousTargetFacing=targetFacing;previousPosition=heading.position;previousRotation=heading.rotation;
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
            // A motor-heading turn carries the body through braking; smoothing
            // its residual reversal prevents an extra 180-degree pelvis flip.
            // Once that turn settles, follow contact-redirected travel directly.
            // Globally damping direction left a steady moving body sideways
            // when a pad or wall deflected it partway through an existing step.
            if(reset)freeDirection=previousDirection;
            else if(!paused)freeDirection=turnRecovery>0?Vector3.RotateTowards(freeDirection,previousDirection,6*correctionDelta,0):previousDirection;
            Vector3 gaitDirection=(targetFacing?previousDirection:freeDirection)*(reverse?-1:1);
            float yaw=Mathf.Atan2(gaitDirection.x,gaitDirection.z)*Mathf.Rad2Deg;
            if(targetFacing)yaw=Mathf.Clamp(yaw,-90,90);
            pelvis.rotation=Quaternion.AngleAxis(yaw,heading.up)*pelvis.rotation;
            if(targetFacing)foreach(var joint in spine)joint.rotation=Quaternion.AngleAxis(-yaw/spine.Length,heading.up)*joint.rotation;
            Quaternion lowerRotation=heading.rotation*Quaternion.Euler(0,yaw,0);
            Vector3 legDirection=Quaternion.Euler(0,-yaw,0)*gaitDirection;
            var terrainNormal=TrySupport(heading.position,out var rootSupport)?rootSupport.normal:Vector3.up;
            float extraClearance=Mathf.Clamp(heading.position.y-rootSupport.point.y-.08f,0,.25f);
            if(reset){smoothedTerrainNormal=terrainNormal;smoothedClearance=extraClearance;}
            else if(!paused)
            {
                float follow=1-Mathf.Exp(-Mathf.Max(0,deltaTime<0?Time.unscaledDeltaTime:deltaTime)/.08f);
                smoothedTerrainNormal=Vector3.Slerp(smoothedTerrainNormal,terrainNormal,follow);
                smoothedClearance=Mathf.Lerp(smoothedClearance,extraClearance,follow);
            }
            terrainNormal=smoothedTerrainNormal;
            float slope=new Vector2(terrainNormal.x,terrainNormal.z).magnitude/Mathf.Max(.2f,terrainNormal.y);
            TerrainGrade=Mathf.Abs(Vector3.Dot(terrainNormal,heading.TransformDirection(previousDirection)))/Mathf.Max(.2f,terrainNormal.y);
            float terrainBlend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.45f,slope));
            terrainWeight=terrainBlend;
            // Warp the whole leg chain into the support frame. Moving only
            // the ankle uphill leaves the hips inside the other leg's swing.
            // Counter-rotate the spine so the torso remains upright.
            var terrainRotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(Vector3.up,terrainNormal),terrainBlend);
            supportUp=terrainRotation*Vector3.up;
            // Tilting a horizontal heading toward the normal also changed its
            // visible yaw on diagonal slopes. Keep the hip-span's horizontal
            // direction, then lift that right axis onto the support plane.
            var terrainRight=lowerRotation*Vector3.right;
            terrainRight.y=-Vector3.Dot(supportUp,terrainRight)/Mathf.Max(.2f,supportUp.y);
            terrainRight.Normalize();
            terrainRotation=Quaternion.LookRotation(Vector3.Cross(terrainRight,supportUp),supportUp)*Quaternion.Inverse(lowerRotation);
            // The new terrain basis must still carry the foot path along the
            // actual slope trajectory, including diagonal reverse steps.
            var slopeTravel=heading.TransformDirection(gaitDirection);
            slopeTravel.y=-Vector3.Dot(supportUp,slopeTravel)/Mathf.Max(.2f,supportUp.y);
            legDirection=Quaternion.Inverse(terrainRotation*lowerRotation)*slopeTravel.normalized;
            // A capsule stands higher over a ramp than over a flat floor.
            // Remove that extra clearance from the rendered body, preserving
            // the calibration's eight-centimetre root clearance and collision.
            bodyOrigin=heading.position-Vector3.up*smoothedClearance*terrainBlend;
            var gravityPelvis=pelvis.position-heading.position;
            var terrainPelvis=terrainRotation*gravityPelvis;
            pelvis.position=bodyOrigin+terrainPelvis;
            pelvis.rotation=terrainRotation*pelvis.rotation;
            // Put the full balance correction at the lower spine. Dividing
            // this pitch among joints left most of the weighted torso leaning
            // downhill even after the upper chest had returned upright.
            if(spine.Length>0)spine[0].rotation=Quaternion.SlerpUnclamped(Quaternion.identity,Quaternion.Inverse(terrainRotation),clip=="Walk"?1.9f:1.35f)*spine[0].rotation;
            // A longer brisk-walk step needs knee room. Lower the animated
            // pelvis by at most eight centimetres instead of stretching a leg
            // past its chain length or accelerating the entire take further.
            if(clip=="Walk")pelvis.position-=supportUp*Mathf.Clamp((stride-1)*.18f,0,.08f);
            MaximumReachCorrection=0;
            float directional=targetFacing?Mathf.Max(Mathf.Abs(yaw)/90,reverse?1:0):0;
            // A slow uphill step has room to support the hips vertically.
            // Full terrain rotation otherwise leaves the torso behind both
            // feet. Preserve the established foot targets, and fade this
            // correction as an extended walk exhausts the donor's reach.
            // Idle has no travelling anchor to exhaust; retain gravity-based
            // support there so a stopped companion does not sit downhill.
            float climb=-Vector3.Dot(terrainNormal,heading.TransformDirection(previousDirection))/Mathf.Max(.2f,terrainNormal.y);
            float balance=clip=="Idle"?.85f*terrainBlend:clip=="Walk"?.65f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.45f,climb))*
                (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.12f,1.3f,stride))):0;
            if(reset)balanceWeight=balance;
            else if(!paused)balanceWeight=Mathf.Lerp(balanceWeight,balance,1-Mathf.Exp(-correctionDelta/.2f));
            Apply(left,clip,cycle,stride,moving&&!transitioning,legDirection,terrainRotation*lowerRotation,directional);
            Apply(right,clip,Mathf.Repeat(cycle-(clip=="Sprint"?.55f:.5f),1),stride,moving&&!transitioning,legDirection,terrainRotation*lowerRotation,directional);
            pelvis.position+=Vector3.ProjectOnPlane(gravityPelvis-terrainPelvis,Vector3.up)*balanceWeight;
            balanceUp=Vector3.Slerp(supportUp,Vector3.up,balanceWeight);
            // Uphill motion raises the root over a rear foot. Keep both final
            // ankle targets inside their real chain length, including landing
            // and toe-off, rather than lifting an anchor or snapping a knee.
            // Start the support descent before a steep running stance reaches
            // its bound. This leaves time for the existing smooth pelvis
            // response instead of speeding it up and snapping a reverse knee.
            float supportMargin=clip=="Run"?.025f*terrainBlend:0;
            RequestedSupportDrop=Mathf.Max(SupportDrop(left,supportMargin),SupportDrop(right,supportMargin));
            float supportDrop=Mathf.Clamp(RequestedSupportDrop,0,.16f+terrainBlend*.2f);
            if(reset)smoothedSupportDrop=supportDrop;
            else if(!paused)smoothedSupportDrop=Mathf.Lerp(smoothedSupportDrop,supportDrop,
                1-Mathf.Exp(-Mathf.Max(0,deltaTime<0?Time.unscaledDeltaTime:deltaTime)/(clip=="Sprint"?.01f:.025f-.005f*balanceWeight)));
            pelvis.position-=balanceUp*smoothedSupportDrop;
            Complete(left);Complete(right);
            if(!paused)
            {
                bool canSound=actor!=null&&actor.Health.Alive&&!actor.flight&&!actor.ChangingForm&&
                    controller!=null&&controller.isGrounded&&velocity.sqrMagnitude>.01f&&!reset;
                Footfall(left,canSound,transitioning);Footfall(right,canSound,transitioning);
            }
        }
        void Footfall(Leg leg,bool eligible,bool transitioning)
        {
            if(!eligible){leg.soundReady=false;return;}
            // A gait blend is neither a new landing nor a released foot.
            if(transitioning)return;
            // Heel-to-toe handover is one stance, not a second footfall. A
            // transition/reset must first observe a released foot before rearming.
            if(leg.contact<0){leg.soundReady=true;return;}
            if(!leg.soundReady||leg.weight<.3f||leg.ground==null||leg.surfaceNormal.y<.5f)return;
            Vector3 sole=leg.contact==0?leg.foot.TransformPoint(leg.heelOffset):leg.toe.TransformPoint(leg.toeOffset);
            if(Mathf.Abs(Vector3.Dot(sole-leg.surfacePoint,leg.surfaceNormal))>.06f)return;
            leg.soundReady=false;
            float gain=Mathf.Lerp(.14f,.23f,Mathf.InverseLerp(1,10,actor.motor.Velocity.magnitude));
            AudioManager.Family(AudioSurface.Family(leg.ground,SceneFlow.Current!=null?SceneFlow.Current.Zone:actor.gameObject.scene.name),
                4,actor.transform,sole,gain,170,.08f);
        }
        void Complete(Leg leg)
        {
            MaximumReachCorrection=Mathf.Max(MaximumReachCorrection,Solve(leg,leg.target,leg.pole));
            leg.foot.rotation=leg.rotation;
            // Reach clamping can move a toe across a raised platform edge.
            // Check the final rotated footprint too, after the bounded solve.
            float lift=Mathf.Max(FootprintLift(leg.foot.TransformPoint(leg.heelOffset)),FootprintLift(leg.toe.TransformPoint(leg.toeOffset)));
            if(lift>0)
            {
                MaximumReachCorrection=Mathf.Max(MaximumReachCorrection,Solve(leg,leg.foot.position+Vector3.up*lift,leg.pole));
                leg.foot.rotation=leg.rotation;
            }
        }
        void Apply(Leg leg,string clip,float phase,float stride,bool canPlant,Vector3 direction,Quaternion lowerRotation,float directional)
        {
            Vector3 originalFoot=leg.foot.position;
            Quaternion footRotation=leg.foot.rotation;
            Vector3 local=Quaternion.Inverse(lowerRotation)*(originalFoot-bodyOrigin);
            Vector3 warped=local;
            warped.x=local.x+direction.x*local.z*stride;
            warped.z=direction.z*local.z*stride;
            // A deliberate side/back step needs a base of support. Keep each
            // foot on its own side of the pelvis instead of letting the donor's
            // narrow forward swing cross a foot held in a lateral stance.
            float side=Mathf.Max(warped.x*leg.side,.14f);
            warped.x=Mathf.Lerp(warped.x,side*leg.side,directional);
            Vector3 target=bodyOrigin+lowerRotation*warped;
            Vector3 heel=leg.foot.TransformPoint(leg.heelOffset),toe=leg.toe.TransformPoint(leg.toeOffset);
            int contact=-1;float weight=0;leg.ground=null;
            if(canPlant)
            {
                if(clip=="Walk")
                {
                    // A turning heel can be far from the next swing pose.
                    // Release it gradually before handing support to the toe.
                    // The gravity-balanced hip needs a longer heel landing;
                    // retain the existing release and fully planted toe phase.
                    if(phase<.135f){contact=0;weight=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Min(phase/(.025f+.065f*balanceWeight),(.135f-phase)/.06f)));}
                    // Release for the authored toe-off before the opposite
                    // heel lands. Holding through half a cycle overextends
                    // the trailing leg as the root climbs a slope.
                    else if(phase<.48f){contact=1;weight=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Min((phase-.135f)/(.04f+.025f*balanceWeight),(.48f-phase)/.07f)));}
                }
                else if(clip=="Run"||clip=="Sprint")
                {
                    // Blend into landing and release over the authored swing,
                    // retaining full support through the measured stance.
                    // The old 0.025-cycle release yanked the ankle in ~10 ms.
                    float start=clip=="Run"?.02f:.03f,attack=clip=="Run"?.055f:.065f;
                    float end=clip=="Run"?.26f:.21f,release=clip=="Run"?.1f:.05f;
                    // Reverse playback exits through the forward landing ramp.
                    // Give a steep backstep its full release interval while
                    // keeping the existing central stance fully planted.
                    float reverseRelease=clip=="Run"&&previousReverse?.02f*terrainWeight:0;
                    start-=reverseRelease;attack+=reverseRelease;
                    float extension=.06f*terrainWeight;end+=extension;release+=extension;
                    contact=1;weight=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Min((phase-start)/attack,(end-phase)/release)));
                    if(weight<=0)contact=-1;
                }
            }
            Vector3 marker=contact==0?heel:toe;
            Vector3 proposed=marker+target-originalFoot;
            if(TrySupport(proposed,out var ground))
            {
                leg.ground=ground.collider;leg.surfacePoint=ground.point;leg.surfaceNormal=ground.normal;
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
                // Each sole needs its own support query: a toe and heel may
                // straddle the raised pad, where one extrapolated plane fails.
                Vector3 offset=target-originalFoot;
                target.y+=Mathf.Max(FootprintLift(heel+offset),FootprintLift(toe+offset));
            }
            else contact=-1;
            leg.contact=contact;leg.weight=weight;
            leg.target=target;leg.pole=lowerRotation*new Vector3(leg.side*.3f,0,1);
            leg.rotation=footRotation;
        }
        float SupportDrop(Leg leg,float margin)
        {
            float length=Vector3.Distance(leg.thigh.position,leg.knee.position)+Vector3.Distance(leg.knee.position,leg.foot.position)-(.025f+.04f*terrainWeight)-margin;
            Vector3 delta=leg.thigh.position-leg.target;
            float altitude=Vector3.Dot(delta,balanceUp);
            float horizontal=Vector3.ProjectOnPlane(delta,balanceUp).sqrMagnitude;
            // Keep the requested drop continuous across the reach boundary;
            // an unreachable horizontal target still needs the bounded drop.
            float vertical=Mathf.Sqrt(Mathf.Max(0,length*length-horizontal));
            return Mathf.Max(0,altitude-vertical);
        }
        float FootprintLift(Vector3 sole)=>TrySupport(sole,out var hit)?Mathf.Max(0,hit.point.y+.003f-sole.y):0;
        bool TrySupport(Vector3 point,out RaycastHit support)
        {
            var origin=new Vector3(point.x,heading.position.y+1.2f,point.z);
            return Physics.Raycast(origin,Vector3.down,out support,2.4f,~0,QueryTriggerInteraction.Ignore)&&support.point.y<=heading.position.y+.8f;
        }
        float Solve(Leg leg,Vector3 target,Vector3 pole)
        {
            Vector3 origin=leg.thigh.position,knee=leg.knee.position,foot=leg.foot.position;
            float upper=Vector3.Distance(origin,knee),lower=Vector3.Distance(knee,foot);
            Vector3 delta=target-origin;float requested=delta.magnitude;
            if(requested<.001f||upper<.001f||lower<.001f)return 0;
            Vector3 axis=delta/requested;
            float distance=Mathf.Clamp(requested,Mathf.Abs(upper-lower)+.001f,upper+lower-(.025f+.04f*terrainWeight));
            // The ankle path retains the take's bend/extension and foot roll.
            // A stable anatomical pole keeps the knee facing with the lower
            // body even when Mecanim's almost-straight leg changes bend plane.
            // Steep steps keep four additional centimetres of bend reserve.
            Vector3 bend=Vector3.Cross(axis,Vector3.Cross(pole,-supportUp)).normalized;
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
