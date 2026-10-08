using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    [DefaultExecutionOrder(1500)]
    public sealed class StridePoseClock : MonoBehaviour
    {
        public Animator rig;
        public int frame;
        public float delta,leftAngle,rightAngle;
        void LateUpdate()
        {
            frame=Time.frameCount;delta=Time.unscaledDeltaTime;
            if(rig==null)return;
            float Angle(HumanBodyBones hip,HumanBodyBones knee,HumanBodyBones foot)
            {var k=rig.GetBoneTransform(knee).position;return Vector3.Angle(rig.GetBoneTransform(hip).position-k,rig.GetBoneTransform(foot).position-k);}
            leftAngle=Angle(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot);
            rightAngle=Angle(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot);
        }
    }
    [DefaultExecutionOrder(1000)]
    public sealed class StrideClockMotion : MonoBehaviour
    {
        void Update(){if(Time.frameCount%2==0)System.Threading.Thread.Sleep(20);}
        void LateUpdate(){transform.position+=Vector3.right*(30*Time.unscaledDeltaTime);}
    }
    public sealed class RuntimeStrideTests
    {
        sealed class Travel : IMotor
        {
            public Vector3 velocity;
            public Vector3 Facing=>Vector3.forward;
            public Vector3 Velocity=>velocity;
            public void Move(Vector2 input,bool boost,bool brake){}
            public void Dash(Vector3 direction,float distance){}
        }
        [UnityTest] public IEnumerator PoseSpeedUsesItsEvaluatedFrameInsteadOfTheNextCoroutineFrame()
        {
            int previousCap=Application.targetFrameRate;Application.targetFrameRate=-1;
            var root=new GameObject("uneven pose-clock control",typeof(StrideClockMotion),typeof(StridePoseClock));
            var clock=root.GetComponent<StridePoseClock>();
            try
            {
                yield return null;yield return null;var previous=root.transform.position;
                float worst=0,wrongClock=0;
                for(int sample=0;sample<30;sample++)
                {
                    yield return null;var current=root.transform.position;
                    Assert.AreEqual(Time.frameCount-1,clock.frame,"pose stamp must identify the completed frame");
                    // Displacement remains measurable on very short frames;
                    // Quaternion.Angle rounds tiny rotations to zero.
                    float angle=Vector3.Distance(previous,current);
                    worst=Mathf.Max(worst,Mathf.Abs(angle/clock.delta-30));
                    wrongClock=Mathf.Max(wrongClock,Mathf.Abs(angle/Time.unscaledDeltaTime-30));
                    previous=current;
                }
                Debug.Log($"POSE_CLOCK_CONTROL evaluatedSpeedError={worst:F6} nextFrameSpeedError={wrongClock:F6}");
                Assert.Less(worst,3,"known 30-metre/s rendered travel has the wrong sampling clock");
                Assert.Greater(wrongClock,10,"control must expose the old mismatched-frame denominator");
            }
            finally{Object.Destroy(root);Application.targetFrameRate=previousCap;}
        }
        [UnityTest] public IEnumerator TarenCollisionRecoveryTransfersOutOfWalkBeforeItsFootSlides()=>CollisionRecovery("Taren");
        [UnityTest] public IEnumerator SelaCollisionRecoveryTransfersOutOfWalkBeforeItsFootSlides()=>CollisionRecovery("Sela");
        static IEnumerator CollisionRecovery(string hero)
        {
            GameTime.Reset();int previousCap=Application.targetFrameRate;Application.targetFrameRate=60;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
            var root=new GameObject("collision recovery stride",typeof(Health),typeof(CombatActor));root.transform.position=new Vector3(100,.08f,100);
            var actor=root.GetComponent<CombatActor>();actor.character=hero;var travel=new Travel{velocity=Vector3.forward*1.46f};actor.motor=travel;
            var body=Object.Instantiate(GameCatalog.Find<CharacterDef>(hero).shaped,root.transform);
            var rig=body.GetComponentInChildren<Animator>();var sole=new SoleProbe(rig,true);Physics.SyncTransforms();
            try
            {
                float started=Time.unscaledTime;
                while(Time.unscaledTime-started<.65f){root.transform.position+=travel.velocity*Time.unscaledDeltaTime;yield return null;}
                // Begin just before the recorded failure's central walking
                // stance, then restore the ordinary 6.7 m/s Shaped run speed.
                rig.Play("Walk",0,.17f);body.GetComponent<GroundFeet>().ResetContacts();yield return null;
                travel.velocity=Vector3.forward*6.7f;started=Time.unscaledTime;
                var planted=new List<Vector3>();float drift=0,transfer=float.PositiveInfinity;int observed=0;
                while(Time.unscaledTime-started<.3f)
                {
                    var info=rig.GetCurrentAnimatorStateInfo(0);
                    if(info.IsName("Run")||rig.IsInTransition(0)&&rig.GetNextAnimatorStateInfo(0).IsName("Run"))transfer=Mathf.Min(transfer,Time.unscaledTime-started);
                    float phase=Mathf.Repeat(info.normalizedTime,1);
                    if(info.IsName("Walk")&&!rig.IsInTransition(0)&&phase>=.2f&&phase<=.4f)
                    {
                        var point=sole.Point();foreach(var prior in planted)drift=Mathf.Max(drift,new Vector2(point.x-prior.x,point.z-prior.z).magnitude);planted.Add(point);
                    }
                    root.transform.position+=travel.velocity*Time.unscaledDeltaTime;observed++;yield return null;
                }
                Debug.Log($"COLLISION_GAIT_RECOVERY {hero} transferSeconds={transfer:F6} drift={drift:F6} plantedSamples={planted.Count} observed={observed}");
                Assert.GreaterOrEqual(observed,12);
                var failures=new List<string>();
                if(transfer>.1f)failures.Add("restored run motion has no gait transfer within 100 ms: "+transfer);
                if(drift>.05f)failures.Add("restored speed drags the planted walking sole "+drift+" m");
                Assert.IsEmpty(failures,string.Join("; ",failures));
            }
            finally{sole.Dispose();Object.Destroy(root);Object.Destroy(floor);Application.targetFrameRate=previousCap;GameTime.Reset();}
        }
        [UnityTest] public IEnumerator UnlockedBodyFacesMeasuredTravelWhenAContactRedirectsIt()
        {
            GameTime.Reset();var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);Physics.SyncTransforms();
            try
            {
                foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})foreach(float angle in new[]{180f,90f})
                {
                    var root=new GameObject("redirected free travel",typeof(Health),typeof(CombatActor));root.transform.position=new Vector3(100,0,100);
                    var actor=root.GetComponent<CombatActor>();actor.character=hero;actor.TargetLocked=false;
                    Vector3 travel=Quaternion.Euler(0,angle,0)*Vector3.forward;float speed=shaped?6.7f:2.6f;
                    actor.motor=new Travel{velocity=travel*speed};
                    var definition=GameCatalog.Find<CharacterDef>(hero);var body=Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
                    var rig=body.GetComponentInChildren<Animator>();float maximum=0;int samples=0;float started=Time.unscaledTime;
                    try
                    {
                        while(Time.unscaledTime-started<1.4f)
                        {
                            if(Time.unscaledTime-started>.45f&&!rig.IsInTransition(0))
                            {
                                var span=rig.GetBoneTransform(HumanBodyBones.RightUpperLeg).position-rig.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
                                var visible=Vector3.Cross(span,Vector3.up);visible.y=0;
                                maximum=Mathf.Max(maximum,Vector3.Angle(visible,travel));samples++;
                            }
                            root.transform.position+=travel*speed*Time.unscaledDeltaTime;yield return null;
                        }
                        Assert.Greater(samples,5);
                        Assert.Less(maximum,10,hero+" "+(shaped?"Shaped":"Natural")+" "+angle+": free body must face real travel, even when it differs from requested root heading");
                    }
                    finally{Object.Destroy(root);}
                    yield return null;
                }
            }
            finally{Object.Destroy(floor);GameTime.Reset();}
        }
        // Independent rendered-mesh probe. It never reads GroundFeet's contact
        // anchor or the profile's marker offsets. BakeMesh also works without
        // retaining every imported mesh's CPU vertex buffer in the player.
        sealed class SoleProbe
        {
            sealed class Part
            {
                public SkinnedMeshRenderer renderer;
                public Mesh baked=new Mesh();
                public readonly List<Vector3> vertices=new();
                public int[] indices;
            }
            readonly List<Part> parts=new();
            public SoleProbe(Animator animator,bool left)
            {
                var foot=animator.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
                var other=animator.GetBoneTransform(left?HumanBodyBones.RightFoot:HumanBodyBones.LeftFoot);
                var candidates=new List<(Part part,int index,Vector3 point)>();
                foreach(var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var part=new Part{renderer=renderer};renderer.BakeMesh(part.baked);parts.Add(part);
                    part.baked.GetVertices(part.vertices);var vertices=part.vertices;
                    for(int i=0;i<vertices.Count;i++)
                    {
                        Vector3 point=renderer.transform.TransformPoint(vertices[i]);
                        if(Vector3.Distance(point,foot.position)<.48f&&Vector3.Dot(point-(foot.position+other.position)*.5f,foot.position-other.position)>0)
                            candidates.Add((part,i,point));
                    }
                }
                Assert.Greater(candidates.Count,0,"visible foot region missing");
                float bottom=candidates.Min(c=>c.point.y);
                var sole=candidates.Where(c=>c.point.y<bottom+.025f).ToArray();
                float back=sole.Min(c=>c.point.z),front=sole.Max(c=>c.point.z);
                Assert.Greater(front-back,.08f,"bind-pose foot must have a visible sole");
                foreach(var part in parts)part.indices=sole.Where(c=>c.part==part&&c.point.z>front-(front-back)*.25f).Select(c=>c.index).ToArray();
            }
            public Vector3 Point()
            {
                Vector3 sum=Vector3.zero;int count=0;
                foreach(var part in parts)
                {
                    if(part.indices.Length==0)continue;
                    part.renderer.BakeMesh(part.baked);part.baked.GetVertices(part.vertices);var vertices=part.vertices;
                    foreach(int i in part.indices){sum+=part.renderer.transform.TransformPoint(vertices[i]);count++;}
                }
                Assert.Greater(count,0);return sum/count;
            }
            public void Dispose(){foreach(var part in parts)Object.Destroy(part.baked);}
        }
        [UnityTest] public IEnumerator MovingEmitterFireKeepsVisibleLegStride()
        {
            GameTime.Reset();var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(200,-.5f,200);floor.transform.localScale=new Vector3(120,1,240);
            Physics.SyncTransforms();
            try
            {
                foreach(float speed in new[]{2.6f,6.7f,10.8f})
                {
                    var root=new GameObject("moving emitter stride",typeof(Health),typeof(CombatActor),typeof(RangedContact));
                    root.transform.position=new Vector3(200,0,200);
                    var actor=root.GetComponent<CombatActor>();actor.character="Sela";actor.Health.friendly=true;
                    actor.motor=new Travel{velocity=Vector3.forward*speed};
                    var body=Object.Instantiate(GameCatalog.Find<CharacterDef>("Sela").shaped,root.transform);
                    var rig=body.GetComponentInChildren<Animator>();var left=new SoleProbe(rig,true);var right=new SoleProbe(rig,false);
                    try
                    {
                        float started=Time.unscaledTime,nextShot=started+1,shotAt=-10;
                        var minimum=new[]{float.PositiveInfinity,float.PositiveInfinity};
                        var maximum=new[]{float.NegativeInfinity,float.NegativeInfinity};int sampled=0,shots=0,contacts=0;
                        var stances=new[]{new List<Vector3>(),new List<Vector3>()};float drift=0;
                        while(Time.unscaledTime-started<4.2f)
                        {
                            if(Time.unscaledTime>=nextShot&&actor.Attack())
                            {shotAt=Time.unscaledTime;nextShot=shotAt+.34f;shots++;}
                            float afterShot=Time.unscaledTime-shotAt;
                            var feet=new[]{left.Point(),right.Point()};
                            if(Time.unscaledTime-started>1&&!rig.IsInTransition(0))
                            {
                                float phase=Mathf.Repeat(rig.GetCurrentAnimatorStateInfo(0).normalizedTime,1);
                                for(int side=0;side<2;side++)
                                {
                                    float p=Mathf.Repeat(phase-side*(speed>7.5f?.55f:.5f),1);
                                    bool planted=speed<3.1f?p>=.2f&&p<=.4f:p>=.08f&&p<=.16f;
                                    var samples=stances[side];
                                    if(planted)samples.Add(feet[side]);
                                    else if(samples.Count>0)
                                    {
                                        foreach(var a in samples)foreach(var b in samples)
                                            drift=Mathf.Max(drift,new Vector2(a.x-b.x,a.z-b.z).magnitude);
                                        if(samples.Count>=2)contacts++;samples.Clear();
                                    }
                                }
                            }
                            // Measure the actual generated soles during the
                            // braced middle of the shot, away from its blend.
                            if(afterShot>=.12f&&afterShot<=.24f)
                            {
                                for(int side=0;side<2;side++)
                                {
                                    float forward=root.transform.InverseTransformPoint(feet[side]).z;
                                    minimum[side]=Mathf.Min(minimum[side],forward);maximum[side]=Mathf.Max(maximum[side],forward);
                                    Assert.GreaterOrEqual(feet[side].y,-.03f,"moving-fire sole penetrates the ground");
                                }
                                sampled++;
                            }
                            root.transform.position+=Vector3.forward*speed*Time.unscaledDeltaTime;yield return null;
                        }
                        Assert.GreaterOrEqual(shots,8);Assert.GreaterOrEqual(sampled,20);
                        Assert.GreaterOrEqual(contacts,4,"moving fire lacks repeated actual stance contacts");
                        Assert.LessOrEqual(drift,.05f,"moving-fire stance slides over the ground");
                        for(int side=0;side<2;side++)
                            Assert.That(maximum[side]-minimum[side],Is.InRange(.25f,2.5f),
                                $"speed {speed} foot {side}: firing pins a planted leg pose to the moving body");
                        TestContext.WriteLine($"MOVING_FIRE speed={speed} leftStride={maximum[0]-minimum[0]:F6} rightStride={maximum[1]-minimum[1]:F6} contacts={contacts} drift={drift:F6}");
                        GameTime.Paused=true;yield return null;
                        var wrist=rig.GetBoneTransform(HumanBodyBones.LeftHand);
                        Vector3 pausedLeft=left.Point(),pausedRight=right.Point(),pausedHand=wrist.position;
                        var phases=new float[rig.layerCount];
                        for(int layer=0;layer<phases.Length;layer++)phases[layer]=rig.GetCurrentAnimatorStateInfo(layer).normalizedTime;
                        yield return new WaitForSecondsRealtime(.2f);
                        Assert.Less(Vector3.Distance(pausedLeft,left.Point()),.003f,"moving-fire left sole changes during pause");
                        Assert.Less(Vector3.Distance(pausedRight,right.Point()),.003f,"moving-fire right sole changes during pause");
                        Assert.Less(Vector3.Distance(pausedHand,wrist.position),.003f,"posed emitter changes during pause");
                        for(int layer=0;layer<phases.Length;layer++)
                            Assert.That(rig.GetCurrentAnimatorStateInfo(layer).normalizedTime,Is.EqualTo(phases[layer]).Within(.001f));
                        GameTime.Paused=false;
                    }
                    finally{left.Dispose();right.Dispose();Object.Destroy(root);}
                    yield return null;
                }
            }
            finally{GameTime.Reset();Object.Destroy(floor);}
        }
        [UnityTest] public IEnumerator VisibleSolesStayPlantedOnUphillAndDownhillGround()=>SlopeContacts(new[]{10f,-10f});
        [UnityTest] public IEnumerator VisibleSolesStayPlantedOnSteepUphillAndDownhillGround()=>SlopeContacts(new[]{39f,-39f});
        [UnityTest] public IEnumerator TarenUphillStartsAndStopsKeepSupportedContinuousLegs()=>UphillStartsAndStops("Taren");
        [UnityTest] public IEnumerator SelaUphillStartsAndStopsKeepSupportedContinuousLegs()=>UphillStartsAndStops("Sela");
        [UnityTest] public IEnumerator TarenDownhillStartsAndStopsKeepSupportedContinuousLegs()=>UphillStartsAndStops("Taren",-39);
        [UnityTest] public IEnumerator SelaDownhillStartsAndStopsKeepSupportedContinuousLegs()=>UphillStartsAndStops("Sela",-39);
        [UnityTest] public IEnumerator TarenDownhillBrakingKeepsContinuousLegsAcrossWalkPhases()=>DownhillBrakePhases("Taren");
        [UnityTest] public IEnumerator SelaDownhillBrakingKeepsContinuousLegsAcrossWalkPhases()=>DownhillBrakePhases("Sela");
        static IEnumerator DownhillBrakePhases(string hero)
        {
            GameTime.Reset();int previousCap=Application.targetFrameRate;Application.targetFrameRate=60;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
            floor.transform.rotation=Quaternion.Euler(39,0,0);Physics.SyncTransforms();
            var normal=floor.transform.up;var surface=floor.transform.position+normal*.5f;
            var failures=new List<string>();
            try
            {
                for(int sample=0;sample<24;sample++)
                {
                    var root=new GameObject("downhill brake phase",typeof(Health),typeof(CombatActor));
                    var actor=root.GetComponent<CombatActor>();actor.character=hero;actor.TargetLocked=false;
                    var travel=new Travel();actor.motor=travel;
                    var position=new Vector3(100,0,100);position.y=Vector3.Dot(normal,surface-position)/normal.y+.22f;root.transform.position=position;
                    var body=Object.Instantiate(GameCatalog.Find<CharacterDef>(hero).shaped,root.transform);
                    var rig=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
                    var clock=root.AddComponent<StridePoseClock>();clock.rig=rig;
                    var knees=new Transform[]{rig.GetBoneTransform(HumanBodyBones.LeftLowerLeg),rig.GetBoneTransform(HumanBodyBones.RightLowerLeg)};
                    var hips=new Transform[]{rig.GetBoneTransform(HumanBodyBones.LeftUpperLeg),rig.GetBoneTransform(HumanBodyBones.RightUpperLeg)};
                    var feet=new Transform[]{rig.GetBoneTransform(HumanBodyBones.LeftFoot),rig.GetBoneTransform(HumanBodyBones.RightFoot)};
                    var soles=new[]{new SoleProbe(rig,true),new SoleProbe(rig,false)};
                    float Angle(int side)=>Vector3.Angle(hips[side].position-knees[side].position,feet[side].position-knees[side].position);
                    try
                    {
                        yield return null;yield return new WaitForSecondsRealtime(.3f);
                        travel.velocity=Vector3.forward*1.46f;
                        float started=Time.unscaledTime,target=sample/24f,phase=0;
                        while(true)
                        {
                            var info=rig.GetCurrentAnimatorStateInfo(0);phase=Mathf.Repeat(info.normalizedTime,1);
                            if(Time.unscaledTime-started>1&&info.IsName("Walk")&&!rig.IsInTransition(0)&&Mathf.Repeat(phase-target,1)<.025f)break;
                            Assert.Less(Time.unscaledTime-started,4,"fixture never reached its requested walking phase");
                            position=root.transform.position+travel.velocity*Time.unscaledDeltaTime;
                            position.y+=Vector3.Dot(normal,surface-position)/normal.y+.22f;root.transform.position=position;
                            yield return null;
                        }
                        var angles=new[]{Angle(0),Angle(1)};var prior=new[]{root.transform.InverseTransformPoint(knees[0].position),root.transform.InverseTransformPoint(knees[1].position)};
                        float angular=0,linear=0,penetration=0;string peak="";int frames=0;
                        travel.velocity=Vector3.zero;started=Time.unscaledTime;
                        while(Time.unscaledTime-started<.65f)
                        {
                            yield return null;float dt=clock.delta;
                            Assert.AreEqual(Time.frameCount-1,clock.frame,"braking pose must retain its evaluated frame");
                            for(int side=0;side<2;side++)
                            {
                                float angle=Angle(side),speed=Mathf.Abs(angle-angles[side])/dt;
                                if(speed>angular){angular=speed;peak=$"side={side} time={Time.unscaledTime-started:F6} angle={angles[side]:F6}->{angle:F6} transition={rig.IsInTransition(0)} poseFrame={clock.frame} readFrame={Time.frameCount} poseDelta={dt:F9} readDelta={Time.unscaledDeltaTime:F9} stampedAngle={(side==0?clock.leftAngle:clock.rightAngle):F6}";}
                                var point=root.transform.InverseTransformPoint(knees[side].position);
                                linear=Mathf.Max(linear,Vector3.Distance(point,prior[side])/dt);angles[side]=angle;prior[side]=point;
                                penetration=Mathf.Max(penetration,-Vector3.Dot(normal,soles[side].Point()-surface));
                            }
                            frames++;
                        }
                        Debug.Log($"DOWNHILL_BRAKE_PHASE {hero} requested={target:F6} actual={phase:F6} angularDps={angular:F6} kneeMps={linear:F6} penetration={penetration:F6} frames={frames} peak={peak}");
                        Assert.GreaterOrEqual(frames,30);Assert.AreEqual("Idle",driver.CurrentAnimation);
                        if(angular>1200||linear>6||penetration>.03f)failures.Add($"{hero} phase={phase:F6} angular={angular} linear={linear} penetration={penetration}");
                    }
                    finally{foreach(var sole in soles)sole.Dispose();Object.Destroy(root);}
                    yield return null;
                }
                Assert.IsEmpty(failures,string.Join("\n",failures));
            }
            finally{Object.Destroy(floor);Application.targetFrameRate=previousCap;GameTime.Reset();}
        }
        static IEnumerator UphillStartsAndStops(string hero,float slope=39)
        {
            int previousCap=Application.targetFrameRate;Application.targetFrameRate=60;
            GameTime.Reset();var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(100,1,100);
            floor.transform.rotation=Quaternion.Euler(-slope,0,0);Physics.SyncTransforms();
            var normal=floor.transform.up;var surface=floor.transform.position+normal*.5f;
            var failures=new List<string>();
            try
            {
                foreach(bool shaped in new[]{false,true})
                {
                    var root=new GameObject("uphill starts and stops",typeof(Health),typeof(CombatActor));
                    var actor=root.GetComponent<CombatActor>();actor.character=hero;actor.TargetLocked=false;
                    var motor=new Travel();actor.motor=motor;
                    var position=new Vector3(100,0,100);position.y=Vector3.Dot(normal,surface-position)/normal.y+.22f;root.transform.position=position;
                    var definition=GameCatalog.Find<CharacterDef>(hero);
                    var body=Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
                    var rig=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
                    var clock=root.AddComponent<StridePoseClock>();clock.rig=rig;
                    var soles=new[]{new SoleProbe(rig,true),new SoleProbe(rig,false)};
                    var knees=new Vector3[2];var angles=new float[2];float kneeSpeed=0,angularSpeed=0,rear=0,penetration=0;
                    int jointSamples=0,heldSamples=0;float elapsed=0;bool prior=false;string peak="",angularPeak="";
                    try
                    {
                        yield return null;yield return null;
                        while(elapsed<6.5f)
                        {
                            float dt=Time.unscaledDeltaTime;
                            if(elapsed>.5f)
                            {
                                Assert.AreEqual(Time.frameCount-1,clock.frame,"start/stop pose must retain its evaluated frame");
                                for(int side=0;side<2;side++)
                                {
                                    var hip=rig.GetBoneTransform(side==0?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg).position;
                                    var knee=rig.GetBoneTransform(side==0?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg).position;
                                    var foot=rig.GetBoneTransform(side==0?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot).position;
                                    float angle=Vector3.Angle(hip-knee,foot-knee);var local=root.transform.InverseTransformPoint(knee);
                                    if(prior)
                                    {
                                        float moving=Vector3.Distance(local,knees[side])/clock.delta;
                                        if(moving>kneeSpeed){kneeSpeed=moving;peak=$"elapsed={elapsed:F6} dt={dt:F6} state={driver.CurrentAnimation} transition={rig.IsInTransition(0)} knee={knees[side]}->{local} root={root.transform.position}";}
                                        float turning=Mathf.Abs(angle-angles[side])/clock.delta;
                                        if(turning>angularSpeed)
                                        {
                                            angularSpeed=turning;
                                            var info=rig.GetCurrentAnimatorStateInfo(0);var next=rig.GetNextAnimatorStateInfo(0);
                                            angularPeak=$"elapsed={elapsed:F6} dt={dt:F6} side={side} angle={angles[side]:F6}->{angle:F6} state={driver.CurrentAnimation} phase={info.normalizedTime:F6} nextPhase={next.normalizedTime:F6} transition={rig.IsInTransition(0)} root={root.transform.position}";
                                        }
                                        jointSamples++;
                                    }
                                    knees[side]=local;angles[side]=angle;
                                    penetration=Mathf.Max(penetration,-Vector3.Dot(normal,soles[side].Point()-surface));
                                }
                                prior=true;
                                if((elapsed>.65f&&elapsed<1)||(elapsed>3.2f&&elapsed<3.5f)||elapsed>5.7f)
                                {
                                    Assert.AreEqual("Idle",driver.CurrentAnimation,"fixture must reach actual idle after braking");
                                    float torso=(rig.GetBoneTransform(HumanBodyBones.Hips).position.z+rig.GetBoneTransform(HumanBodyBones.Chest).position.z)*.5f;
                                    rear=Mathf.Max(rear,Mathf.Min(soles[0].Point().z,soles[1].Point().z)-torso);heldSamples++;
                                }
                            }
                            float speed=elapsed>=1&&elapsed<2.5f||elapsed>=3.5f&&elapsed<5?1.46f:0;
                            motor.velocity=Vector3.forward*speed;
                            position=root.transform.position+motor.velocity*dt;position.y+=Vector3.Dot(normal,surface-position)/normal.y+.22f;root.transform.position=position;
                            elapsed+=dt;yield return null;
                        }
                        string context=hero+"/"+shaped+"/slope="+slope;
                        Debug.Log($"UPHILL_START_STOP {context} kneeMps={kneeSpeed:F6} angularDps={angularSpeed:F6} torsoBehindRearToe={rear:F6} penetration={penetration:F6} joints={jointSamples} idleSamples={heldSamples}");
                        Debug.Log("UPHILL_TRANSITION_PEAK "+peak);
                        Debug.Log("UPHILL_ANGULAR_PEAK "+angularPeak);
                        if(jointSamples<100||heldSamples<20)failures.Add(context+" lacks transition/idle coverage");
                        if(kneeSpeed>6)failures.Add(context+" knee speed "+kneeSpeed);
                        if(angularSpeed>1200)failures.Add(context+" knee angular speed "+angularSpeed);
                        // This forefoot-centroid reference is farther forward
                        // than the rear visible edge used by the EditMode check.
                        if(rear>.35f)failures.Add(context+" idle torso stays behind both forefeet "+rear);
                        if(penetration>.03f)failures.Add(context+" rendered sole penetration "+penetration);
                        GameTime.Paused=true;yield return null;
                        var held=rig.GetBoneTransform(HumanBodyBones.Hips).position;var left=soles[0].Point();var right=soles[1].Point();
                        yield return new WaitForSecondsRealtime(.2f);
                        Assert.Less(Vector3.Distance(held,rig.GetBoneTransform(HumanBodyBones.Hips).position),.001f);
                        Assert.Less(Vector3.Distance(left,soles[0].Point()),.003f);Assert.Less(Vector3.Distance(right,soles[1].Point()),.003f);
                    }
                    finally{GameTime.Reset();foreach(var sole in soles)sole.Dispose();Object.Destroy(root);}
                    yield return null;
                }
                Assert.IsEmpty(failures,string.Join("\n",failures));
            }
            finally{GameTime.Reset();Object.Destroy(floor);Application.targetFrameRate=previousCap;}
        }
        static IEnumerator SlopeContacts(float[] slopes)
        {
            GameTime.Reset();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(60,1,60);
            var failures=new List<string>();
            try
            {
                foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})foreach(float slope in slopes)
                {
                    floor.transform.rotation=Quaternion.Euler(-slope,0,0);
                    Vector3 normal=floor.transform.up,surface=floor.transform.position+normal*.5f;
                    var root=new GameObject("slope stride "+hero,typeof(Health),typeof(CombatActor),typeof(CharacterController),typeof(GroundMotor));
                    var position=new Vector3(100,0,100);position.y=Vector3.Dot(normal,surface-position)/normal.y+.08f;root.transform.position=position;
                    var controller=root.GetComponent<CharacterController>();controller.center=Vector3.up;controller.height=2;controller.radius=.4f;
                    var actor=root.GetComponent<CombatActor>();actor.character=hero;
                    var motor=root.GetComponent<GroundMotor>();actor.motor=motor;
                    var definition=GameCatalog.Find<CharacterDef>(hero);
                    var body=Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
                    var animator=body.GetComponentInChildren<Animator>();
                    var left=new SoleProbe(animator,true);var right=new SoleProbe(animator,false);
                    Physics.SyncTransforms();
                    try
                    {
                        var points=new[]{new List<Vector3>(),new List<Vector3>()};int contacts=0;float drift=0,penetration=0,lift=0;
                        float started=Time.unscaledTime;
                        while(Time.unscaledTime-started<3.4f)
                        {
                            if(Time.unscaledTime-started>.7f&&!animator.IsInTransition(0))
                            {
                                float phase=Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,1);
                                for(int side=0;side<2;side++)
                                {
                                    float p=Mathf.Repeat(phase-side*.5f,1);bool stance=shaped?p>=.08f&&p<=.16f:p>=.2f&&p<=.4f;
                                    var list=points[side];var point=side==0?left.Point():right.Point();
                                    if(stance)
                                    {
                                        list.Add(point);float clearance=Vector3.Dot(normal,point-surface);
                                        penetration=Mathf.Max(penetration,-clearance);lift=Mathf.Max(lift,clearance);
                                    }
                                    else if(list.Count>0)
                                    {
                                        foreach(var a in list)foreach(var b in list)drift=Mathf.Max(drift,Vector3.Distance(a,b));
                                        if(list.Count>=2)contacts++;list.Clear();
                                    }
                                }
                            }
                            motor.Move(Vector2.up*(shaped?1:2.6f/6.7f),false,false);
                            yield return null;
                        }
                        string context=hero+(shaped?" Shaped":" Natural")+" slope "+slope;
                        Debug.Log($"RUNTIME_SLOPE_CASE {context} contacts={contacts} drift={drift:F6} penetration={penetration:F6} lift={lift:F6}");
                        if(contacts<4)failures.Add(context+" fewer than four real contacts");
                        if(drift>.05f)failures.Add(context+" planted sole travel "+drift);
                        if(penetration>.03f)failures.Add(context+" ramp penetration "+penetration);
                        if(lift>.06f)failures.Add(context+" stance lift "+lift);
                        if(Mathf.Abs(root.transform.position.y-position.y)<=.8f)failures.Add(context+" did not change elevation");
                    }
                    finally{left.Dispose();right.Dispose();Object.Destroy(root);}
                    yield return null;
                }
                Assert.IsEmpty(failures,string.Join("\n",failures));
            }
            finally{GameTime.Reset();Object.Destroy(floor);}
        }

        [UnityTest] public IEnumerator VisibleSolesHoldDuringRuntimeLocomotionAndPause()
        {
            GameTime.Reset();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(400,1,400);
            Physics.SyncTransforms();
            try
            {
                foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})
                foreach(float direction in new[]{0f,90,180})
                {
                    var root=new GameObject("runtime stride "+hero,typeof(Health),typeof(CombatActor));root.transform.position=new Vector3(100,0,100);
                    var actor=root.GetComponent<CombatActor>();actor.character=hero;
                    var travel=Quaternion.Euler(0,direction,0)*Vector3.forward;float speed=shaped?6.7f:2.6f;
                    actor.motor=new Travel{velocity=travel*speed};
                    var definition=GameCatalog.Find<CharacterDef>(hero);
                    var body=Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
                    var animator=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
                    var left=new SoleProbe(animator,true);var right=new SoleProbe(animator,false);
                    try
                    {
                        var points=new[]{new List<Vector3>(),new List<Vector3>()};int contacts=0;float drift=0,penetration=0;
                        float started=Time.unscaledTime;
                        while(Time.unscaledTime-started<3.5f)
                        {
                            if(Time.unscaledTime-started>.6f&&!animator.IsInTransition(0))
                            {
                                float phase=Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,1);
                                for(int side=0;side<2;side++)
                                {
                                    float p=Mathf.Repeat(phase-side*.5f,1);bool stance=shaped?p>=.08f&&p<=.16f:p>=.2f&&p<=.4f;
                                    var list=points[side];var point=side==0?left.Point():right.Point();
                                    if(stance){list.Add(point);penetration=Mathf.Max(penetration,-point.y);}
                                    else if(list.Count>0)
                                    {
                                        foreach(var a in list)foreach(var b in list)drift=Mathf.Max(drift,new Vector2(a.x-b.x,a.z-b.z).magnitude);
                                        if(list.Count>=2)contacts++;list.Clear();
                                    }
                                }
                            }
                            root.transform.position+=travel*speed*Time.unscaledDeltaTime;
                            yield return null;
                        }
                        string context=hero+(shaped?" Shaped":" Natural")+" direction "+direction;
                        Assert.GreaterOrEqual(contacts,4,context+" must include repeated actual stance intervals");
                        Assert.LessOrEqual(drift,.05f,context+" rendered planted sole drift");
                        Assert.LessOrEqual(penetration,.03f,context+" rendered sole penetration");
                        Debug.Log($"RUNTIME_STRIDE_CASE {context} contacts={contacts} drift={drift:F6} penetration={penetration:F6}");
                        GameTime.Paused=true;yield return null;
                        Vector3 beforeLeft=left.Point(),beforeRight=right.Point();float beforePhase=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                        yield return new WaitForSecondsRealtime(.2f);
                        Assert.Less(Vector3.Distance(beforeLeft,left.Point()),.003f,context+" left foot moves during pause");
                        Assert.Less(Vector3.Distance(beforeRight,right.Point()),.003f,context+" right foot moves during pause");
                        Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.EqualTo(beforePhase).Within(.001f));
                        GameTime.Paused=false;
                    }
                    finally{GameTime.Reset();left.Dispose();right.Dispose();Object.Destroy(root);}
                    yield return null;
                }
            }
            finally{GameTime.Reset();Object.Destroy(floor);}
        }
    }
}
