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
            /// <summary>Where the toe was drawn last frame: a climbing heel hands its foot to the toe hold there (UprightClimb).</summary>
            public Vector3 lastToe;
            /// <summary>This step holds its heel to the handover and its toe from it, decided once at its landing: a climbing step
            /// (UprightClimb), or any step of a body whose profile asks for it (GroundStrideProfile.holdsHandover).</summary>
            public bool climbing;
            /// <summary>The phase the current hold began: the heel lift eases in from it.</summary>
            public float holdStart;
            /// <summary>Climbing, a hold dropped by a stop or a gait blend lets go of where the foot stood over 0.2 s (1 to 0), not at once.</summary>
            public float letGo;
            public Vector3 letGoFrom,lastAnkle;
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
        float idleBalanceLift;
        Vector3 balanceUp=Vector3.up;
        Vector3 previousVelocity;float previousYaw,leanPitch,leanRoll;
        /// <summary>UprightClimb: how far an uphill walk stands in the gravity frame (0 on the flat, downhill and in every other gait),
        /// the part of the support tilt the feet carry on their own, and the ground's rise per metre along x and z.</summary>
        float uprightWeight,stepDelta,walkKneeRoom;Quaternion footTilt=Quaternion.identity;Vector2 slopeGrade;
        public float UprightWeight=>uprightWeight;
        /// <summary>How far a climbing trailing heel may peel up off the slope (HeelLift), degrees.</summary>
        public const float ClimbHeelLift=60;
        /// <summary>The fastest a climbing run's legs lift the upright hips, metres a second (UprightClimb): at 2.5 the stance knee of a
        /// 6.7 m/s run up 39 degrees straightens at about 2,300 deg/s and the hips hold 88 to 95% of the flat run's height.</summary>
        public const float ClimbLift=2.5f;
        public float LeanPitch=>leanPitch;
        public float LeanRoll=>leanRoll;
        /// <summary>Diagnostics for the step audits: which part of a sole is holding (-1 none, 0 heel, 1 toe) and how firmly.</summary>
        public int ContactOf(bool isLeft)=>(isLeft?left:right)?.contact??-1;
        public float ContactWeightOf(bool isLeft)=>(isLeft?left:right)?.weight??0;
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
                animator.IsInTransition(0),GameTime.Paused||GameTime.Stopped,driver.ReverseLocomotion,actor.TargetLocked);
        }
        public void ResetContacts(){if(left!=null){left.contact=right.contact=-1;left.soundReady=right.soundReady=false;}previousClip=null;previousDirection=Vector3.forward;}
        public void Correct(Vector3 velocity,string clip,float phase,float stride,bool transitioning,bool paused=false,bool reverse=false,bool targetFacing=true,float deltaTime=-1)
        {
            if(profile==null)return;
            bool reset=previousClip==null||previousReverse!=reverse||previousTargetFacing!=targetFacing||Vector3.Distance(previousPosition,heading.position)>.75f||Quaternion.Angle(previousRotation,heading.rotation)>40;
            float correctionDelta=Mathf.Max(0,deltaTime<0?Time.unscaledDeltaTime:deltaTime);
            if(reset)turnRecovery=0;
            else if(!paused)turnRecovery=Quaternion.Angle(previousRotation,heading.rotation)>.5f?.2f:Mathf.Max(0,turnRecovery-correctionDelta);
            // A hold picked up again after a gait blend or a reset is not a landing: it plants where the foot is, with the loose
            // handover, and its heel lift eases in from there (a stale climbing latch once planted it at full weight in one frame).
            if(reset||previousClip!=clip||transitioning)
            {
                // Upright, the take's path is not where a climbing foot was planted (the tilted frame's was): dropped at once by a
                // stop, a planted foot jumped to it, 9 m/s at the knee. It lets go of where it stood instead (Apply).
                if(!reset&&!paused&&uprightWeight>0)foreach(var leg in new[]{left,right})if(leg.contact>=0&&leg.weight>.5f){leg.letGo=1;leg.letGoFrom=leg.lastAnkle;}
                left.contact=right.contact=-1;left.climbing=right.climbing=false;
            }
            if(reset)left.letGo=right.letGo=0;
            stepDelta=paused?0:correctionDelta;
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
            float climb=-Vector3.Dot(terrainNormal,heading.TransformDirection(previousDirection))/Mathf.Max(.2f,terrainNormal.y);
            // Walking, running and sprinting up a slope are posed in the gravity frame (UprightClimb). The tilted frame folded a
            // climbing run harder than a walk: at 39 degrees the combat run's knees came up to its chest and its hips sank to 62%.
            float climbing=clip=="Walk"||clip=="Run"||clip=="Sprint"?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.45f,climb)):0;
            // Upright, a planted leg has to lift the hips as fast as the climb rises under them, and a stance leg straightens from bent
            // to full reach in the time that takes. A run lifts them only as fast as ClimbLift; faster than that (6.7 m/s up 39 degrees
            // rises 5.4 m/s, and the knee straightened at 3,200 deg/s) the stride leans back toward the slope's frame. A walk never
            // rises that fast. Backpedalling up a slope keeps the slope's frame.
            if(clip!="Walk"&&climbing>0)
            {
                var planar=velocity;planar.y=0;float rising=planar.magnitude*Mathf.Max(0,climb);
                climbing=reverse?0:climbing*Mathf.Clamp01(ClimbLift/Mathf.Max(.01f,rising));
            }
            if(reset)uprightWeight=climbing;
            else if(!paused)uprightWeight=Mathf.Lerp(uprightWeight,climbing,1-Mathf.Exp(-correctionDelta/.2f));
            if(uprightWeight<.0005f&&climbing==0)uprightWeight=0;
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
            // UprightClimb: the support frame tilts a climbing body back off the slope with its hips behind both feet, and every
            // repair of that (the balance shift, the support drop) folded the legs. A climb stands the hips and torso up in the
            // gravity frame instead; only the feet keep the support frame (footTilt), and their targets follow the ground (Apply).
            footTilt=Quaternion.identity;slopeGrade=Vector2.zero;
            if(uprightWeight>0)
            {
                var standing=Quaternion.Slerp(terrainRotation,Quaternion.identity,uprightWeight);
                footTilt=terrainRotation*Quaternion.Inverse(standing);terrainRotation=standing;supportUp=terrainRotation*Vector3.up;
                slopeGrade=new Vector2(-terrainNormal.x,-terrainNormal.z)/Mathf.Max(.2f,terrainNormal.y);
            }
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
            // A walking knee never locks straight at heel strike: keep a small
            // bend reserve even at the owned stride (D197). At stride 1 the
            // landing leg otherwise reached full extension and unlocked at
            // ~1230 deg/s on flat ground, a visible snap in the civil walk.
            // Steep support already keeps four extra centimetres of bend in the
            // solver, so the reserve fades out with the terrain blend.
            float bendReserve=WalkBendReserve*(1-terrainBlend);
            // Climbing, that knee room eases out over 0.06 s when the walk ends: switched off in one frame at a stop, the hips sprang
            // up to 5 cm (31 m/s at the knee in a 0.7 ms frame). On the flat and downhill it switches as it always did.
            float kneeRoom=clip=="Walk"?Mathf.Clamp(bendReserve+(stride-1)*.18f,bendReserve,.08f):0;
            if(reset||uprightWeight<=0)walkKneeRoom=kneeRoom;
            else if(!paused)walkKneeRoom=kneeRoom>walkKneeRoom?kneeRoom:Mathf.Lerp(walkKneeRoom,kneeRoom,1-Mathf.Exp(-correctionDelta/.06f));
            pelvis.position-=supportUp*walkKneeRoom;
            // The profile's stride covers the distance along the slope. Upright, the feet travel the level distance the
            // body does, so the step is shorter by that same factor: the climb shortens the step, as a climber's does.
            float footStride=stride*Mathf.Lerp(1,1/GroundStrideProfile.TerrainTravel(TerrainGrade),uprightWeight);
            MaximumReachCorrection=0;
            float directional=targetFacing?Mathf.Max(Mathf.Abs(yaw)/90,reverse?1:0):0;
            // A slow uphill step has room to support the hips vertically.
            // Full terrain rotation otherwise leaves the torso behind both
            // feet. Preserve the established foot targets, and fade this
            // correction as an extended walk exhausts the donor's reach.
            // Idle has no travelling anchor to exhaust; retain gravity-based
            // support there so a stopped companion does not sit downhill.
            float balance=clip=="Idle"?.85f*terrainBlend:clip=="Walk"?.65f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.45f,climb))*
                (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.12f,1.3f,stride))):0;
            if(reset)balanceWeight=balance;
            else if(!paused)balanceWeight=Mathf.Lerp(balanceWeight,balance,1-Mathf.Exp(-correctionDelta/.2f));
            Apply(left,clip,cycle,footStride,moving&&!transitioning,legDirection,terrainRotation*lowerRotation,directional);
            Apply(right,clip,Mathf.Repeat(cycle-(clip=="Sprint"?.55f:.5f),1),footStride,moving&&!transitioning,legDirection,terrainRotation*lowerRotation,directional);
            pelvis.position+=Vector3.ProjectOnPlane(gravityPelvis-terrainPelvis,Vector3.up)*balanceWeight;
            // Horizontal idle support still left the hips lowered by the
            // terrain rotation, even when neither leg needed a reach drop.
            // Restore that vertical loss after preserving the foot targets.
            // Fade through starts/stops so this cannot pop the knees; the
            // existing reach descent below remains authoritative.
            float idleLift=clip=="Idle"?Mathf.Clamp(gravityPelvis.y-terrainPelvis.y,0,.25f)*balanceWeight:0;
            if(reset)idleBalanceLift=idleLift;
            else if(!paused)idleBalanceLift=Mathf.Lerp(idleBalanceLift,idleLift,1-Mathf.Exp(-correctionDelta/.3f));
            pelvis.position+=Vector3.up*idleBalanceLift;
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
            Lean(velocity,clip,reset,paused,targetFacing,correctionDelta);
            if(!paused)
            {
                bool canSound=actor!=null&&actor.Health.Alive&&!actor.flight&&!actor.ChangingForm&&
                    controller!=null&&controller.isGrounded&&velocity.sqrMagnitude>.01f&&!reset;
                Footfall(left,canSound,transitioning);Footfall(right,canSound,transitioning);
            }
        }
        /// <summary>D197. A free-running body leans into its pace and its acceleration and banks into turns, applied at the
        /// lower spine after both legs are solved, so contacts and knees are untouched. Walking, target-facing travel
        /// and paused frames keep the donor torso. Pause reapplies the held lean without advancing it.</summary>
        void Lean(Vector3 velocity,string clip,bool reset,bool paused,bool targetFacing,float delta)
        {
            if(spine.Length==0)return;
            float yaw=heading.eulerAngles.y;
            if(!paused)
            {
                float pitch=0,roll=0;
                bool running=(clip=="Run"||clip=="Sprint")&&!targetFacing;
                if(running&&!reset&&delta>0)
                {
                    var forward=heading.forward;forward.y=0;forward.Normalize();
                    var planar=velocity;planar.y=0;float speed=planar.magnitude;
                    float acceleration=Vector3.Dot(planar-previousVelocity,forward)/delta;
                    float turnRate=Mathf.DeltaAngle(previousYaw,yaw)/delta;
                    pitch=Mathf.Clamp(speed*MaxLeanPerMetre+acceleration*.1f,-4,MaxLean);
                    roll=Mathf.Clamp(-turnRate*speed*.0018f,-MaxBank,MaxBank);
                }
                float follow=reset?1:1-Mathf.Exp(-delta/.12f);
                leanPitch=Mathf.Lerp(leanPitch,pitch,follow);leanRoll=Mathf.Lerp(leanRoll,roll,follow);
                var flat=velocity;flat.y=0;previousVelocity=flat;previousYaw=yaw;
            }
            if(Mathf.Abs(leanPitch)<.01f&&Mathf.Abs(leanRoll)<.01f)return;
            var right=heading.right;right.y=0;right.Normalize();var ahead=heading.forward;ahead.y=0;ahead.Normalize();
            spine[0].rotation=Quaternion.AngleAxis(leanRoll,ahead)*Quaternion.AngleAxis(leanPitch,right)*spine[0].rotation;
        }
        public const float MaxLean=8,MaxBank=8,MaxLeanPerMetre=.6f,WalkBendReserve=.02f;
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
            leg.lastToe=leg.toe.TransformPoint(leg.toeOffset);leg.lastAnkle=leg.foot.position;
        }
        void Apply(Leg leg,string clip,float phase,float stride,bool canPlant,Vector3 direction,Quaternion lowerRotation,float directional)
        {
            Vector3 originalFoot=leg.foot.position;
            // Climbing upright, the foot alone keeps the support frame: its roll from heel to toe stays the take's, laid on the slope.
            if(uprightWeight>0)leg.foot.rotation=footTilt*leg.foot.rotation;
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
            // Climbing upright, the take's foot path rides on the ground it is over. Behind the hips the slope falls away, and
            // a foot lifting into its swing keeps the level path, which clears that ground and is still within the leg's reach.
            // (Following even half that fall behind brought the recovering foot from 30 to 20 cm over a 39 degree slope, but the
            // hips had to come down to reach it: the walk's bob went to 19 cm and its swing knee to 77 degrees.)
            if(uprightWeight>0)
            {
                float rise=slopeGrade.x*(target.x-bodyOrigin.x)+slopeGrade.y*(target.z-bodyOrigin.z);
                // Rounded over 4 cm either side of the hips: a sharp corner there jerked the swinging knee as the foot passed under them.
                const float round=.04f;
                target.y+=uprightWeight*(rise>=round?rise:rise<=-round?0:(rise+round)*(rise+round)/(4*round));
            }
            if(!canPlant&&leg.contact>=0&&leg.weight>.5f&&uprightWeight>0&&stepDelta>0){leg.letGo=1;leg.letGoFrom=leg.lastAnkle;}
            if(leg.letGo>0){target=Vector3.Lerp(target,leg.letGoFrom,Mathf.SmoothStep(0,1,leg.letGo));leg.letGo=Mathf.Max(0,leg.letGo-stepDelta/.2f);}
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
                    // UprightClimb holds the heel to the handover and the toe from it (hold): between two loosened holds the foot drifted
                    // back toward the take's path, and the climbing knee straightened in a burst as the toe hold took it again.
                    if(phase<.135f&&leg.contact<0)leg.climbing=uprightWeight>0||profile.holdsHandover;
                    float hold=leg.climbing?1:0;
                    // A held heel lands over the whole landing ramp: at a long climbing stride the balance weight fades and the 0.025
                    // ramp dropped Idra's landing heel onto its hold at 6.6 m/s at the knee.
                    if(phase<.135f){contact=0;weight=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Min(phase/(.025f+.065f*Mathf.Max(balanceWeight,hold)),Mathf.Lerp((.135f-phase)/.06f,1,hold))));}
                    // Release for the authored toe-off before the opposite
                    // heel lands. Holding through half a cycle overextends
                    // the trailing leg as the root climbs a slope.
                    else if(phase<.48f){contact=1;weight=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Min(Mathf.Lerp((phase-.135f)/(.04f+.025f*balanceWeight),1,hold),(.48f-phase)/.07f)));}
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
                    // A climbing heel lets its foot roll down flat onto the slope about it before the toe takes over (UprightClimb).
                    float roll=contact==0&&leg.climbing?Mathf.SmoothStep(0,1,phase/.135f):0;
                    if(roll>0)
                    {
                        Vector3 soleAxis=toe-heel;Vector3 flat=Vector3.ProjectOnPlane(soleAxis,ground.normal);
                        if(flat.sqrMagnitude>.001f)
                        {
                            footRotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(soleAxis,flat),roll)*footRotation;leg.foot.rotation=footRotation;
                            heel=leg.foot.TransformPoint(leg.heelOffset);toe=leg.toe.TransformPoint(leg.toeOffset);marker=heel;proposed=marker+target-originalFoot;
                        }
                    }
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
                    if(leg.contact!=contact)
                    {
                        leg.anchor=new Vector3(proposed.x,ground.point.y+.006f,proposed.z);leg.holdStart=phase;
                        // Handed over from a climbing heel, the toe is held where that rolled-down foot had put it, so the ankle doesn't move.
                        if(leg.contact==0&&contact==1&&leg.climbing&&TrySupport(leg.lastToe,out var under))leg.anchor=new Vector3(leg.lastToe.x,under.point.y+.006f,leg.lastToe.z);
                    }
                    Vector3 planted=leg.anchor-(marker-originalFoot);
                    if(contact==1&&weight>0&&uprightWeight>0)planted=HeelLift(leg,planted,leg.anchor,toe-heel,ground.normal,weight,phase-leg.holdStart,ref footRotation);
                    target=Vector3.Lerp(target,planted,weight);
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
        /// <summary>UprightClimb's push-off. The hips climb away from a planted trailing foot faster than its leg can follow, so its
        /// heel peels up about the planted toe, which stays exactly where it was set down, by as much as keeps the ankle within the
        /// leg's reach (up to <see cref="ClimbHeelLift"/> degrees off the slope). Whatever the heel can't give, the support drop still
        /// takes from the hips. It comes in over the toe hold's first 0.065 of the cycle (held, the 0.135 to 0.2 before the planted window the contact
        /// checks hold still): at a gentle climb the handed-over leg can already be at full reach, and lifting the heel in one frame
        /// snapped the knee. The whole boot turns about the toe, not the ball: a boot whose forefoot is skinned to the foot (Idra's)
        /// otherwise dragged its toe through the slope. Returns the planted ankle target.</summary>
        Vector3 HeelLift(Leg leg,Vector3 planted,Vector3 pivot,Vector3 soleAxis,Vector3 normal,float weight,float held,ref Quaternion footRotation)
        {
            var forward=Vector3.ProjectOnPlane(soleAxis,normal);if(forward.sqrMagnitude<1e-6f)return planted;
            var axis=Vector3.Cross(normal,forward.normalized).normalized;Vector3 hip=leg.thigh.position;
            float reach=Vector3.Distance(leg.thigh.position,leg.knee.position)+Vector3.Distance(leg.knee.position,leg.foot.position)-Reserve();
            float Gap(float degrees)=>Vector3.Distance(pivot+Quaternion.AngleAxis(degrees,axis)*(planted-pivot),hip)-reach;
            float most=ClimbHeelLift*uprightWeight*Mathf.SmoothStep(0,1,held/.065f),lift=0;
            if(most>0&&Gap(0)>0)
            {
                // The least lift that reaches; if none does, the lift that comes nearest (a heel turned past the hip's line only
                // carries the ankle away again). Both are continuous as the hips climb, so the heel rises smoothly.
                float low=0,high=most;
                for(int i=0;i<24;i++){float a=low+(high-low)/3,b=high-(high-low)/3;if(Gap(a)<Gap(b))high=b;else low=a;}
                float nearest=(low+high)*.5f;
                if(Gap(nearest)>0)lift=nearest;
                else{low=0;high=nearest;for(int i=0;i<16;i++){float mid=(low+high)*.5f;if(Gap(mid)>0)low=mid;else high=mid;}lift=high;}
            }
            if(lift<=0)return planted;
            var turn=Quaternion.AngleAxis(lift,axis);
            footRotation=Quaternion.Slerp(Quaternion.identity,turn,weight)*footRotation;
            return pivot+turn*(planted-pivot);
        }
        /// <summary>How far short of straight a solved leg stops: a walking knee never locks, and a steep step in the support frame keeps
        /// four more centimetres. An upright climb (UprightClimb) doesn't tilt the leg chain, so it keeps the flat walk's reserve.</summary>
        float Reserve()=>.025f+.04f*terrainWeight*(1-uprightWeight);
        float SupportDrop(Leg leg,float margin)
        {
            float length=Vector3.Distance(leg.thigh.position,leg.knee.position)+Vector3.Distance(leg.knee.position,leg.foot.position)-Reserve()-margin;
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
            float distance=Mathf.Clamp(requested,Mathf.Abs(upper-lower)+.001f,upper+lower-Reserve());
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
