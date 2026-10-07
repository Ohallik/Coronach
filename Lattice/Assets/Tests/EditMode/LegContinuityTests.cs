using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lattice.Tests.EditMode
{
    public sealed class LegContinuityTests
    {
        [TestCase("Taren",false,2.6f)][TestCase("Sela",false,2.6f)]
        [TestCase("Taren",false,5.4f)][TestCase("Sela",false,5.4f)]
        [TestCase("Taren",true,6.7f)][TestCase("Sela",true,6.7f)]
        [TestCase("Taren",true,10.385f)][TestCase("Sela",true,10.385f)]
        public void WholeStepKeepsKneesContinuousWithoutFreezingTheSwing(string hero,bool shaped,float speed)
            =>Measure(hero,shaped,speed,0);

        [TestCase("Taren",false,2.6f,39f)][TestCase("Sela",false,2.6f,39f)]
        [TestCase("Taren",false,5.4f,39f)][TestCase("Sela",false,5.4f,39f)]
        [TestCase("Taren",true,6.7f,39f)][TestCase("Sela",true,6.7f,39f)]
        [TestCase("Taren",true,10.385f,39f)][TestCase("Sela",true,10.385f,39f)]
        [TestCase("Taren",false,2.6f,-39f)][TestCase("Sela",false,2.6f,-39f)]
        [TestCase("Taren",false,5.4f,-39f)][TestCase("Sela",false,5.4f,-39f)]
        [TestCase("Taren",true,6.7f,-39f)][TestCase("Sela",true,6.7f,-39f)]
        [TestCase("Taren",true,10.385f,-39f)][TestCase("Sela",true,10.385f,-39f)]
        public void SteepStepsKeepKneesContinuousWithoutFreezingTheSwing(string hero,bool shaped,float speed,float slope)
        {
            Measure(hero,shaped,speed,slope);
            Measure(hero,shaped,speed,slope,.22f);
        }

        static void Measure(string hero,bool shaped,float speed,float slope,float clearance=.08f)
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(500,1,500);
            floor.transform.rotation=Quaternion.Euler(-slope,0,0);
            var normal=floor.transform.up;var surface=floor.transform.position+normal*.5f;Physics.SyncTransforms();
            var root=new GameObject("full cycle leg regression");
            var definition=GameCatalog.Find<CharacterDef>(hero);
            var body=Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
            var rig=body.GetComponentInChildren<Animator>();
            string state=speed>7.5f?"Sprint":speed>3.1f?"Run":"Walk";
            var clip=rig.runtimeAnimatorController.animationClips.Single(c=>c.name==state);
            var driver=body.GetComponent<GeneratedAnimator>();var profile=driver.strideProfile;
            Object.DestroyImmediate(driver);
            var feet=body.AddComponent<GroundFeet>();feet.Initialize(rig,root.transform,profile);
            rig.runtimeAnimatorController=null;
            var graph=PlayableGraph.Create("whole step regression");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var play=AnimationClipPlayable.Create(graph,clip);play.SetApplyFootIK(true);
            AnimationPlayableOutput.Create(graph,"pose",rig).SetSourcePlayable(play);graph.Play();
            float grade=Mathf.Abs(Mathf.Tan(slope*Mathf.Deg2Rad));
            float cadence=profile.Cadence(state,speed,grade),stride=profile.Stride(state,speed,cadence,grade);
            const float dt=1f/240;
            var previousAngles=new float[2];var previousKnees=new Vector3[2];
            var minimum=new[]{float.PositiveInfinity,float.PositiveInfinity};var maximum=new[]{float.NegativeInfinity,float.NegativeInfinity};
            float angularSpeed=0,kneeSpeed=0,angleAt=0,kneeAt=0;int samples=0;
            try
            {
                for(int frame=0;frame<Mathf.CeilToInt(clip.length/cadence*3/dt);frame++)
                {
                    float time=frame*dt,phase=time*cadence/clip.length;
                    var position=new Vector3(0,0,time*speed);position.y=Vector3.Dot(normal,surface-position)/normal.y+clearance;
                    root.transform.position=position;
                    play.SetTime(Mathf.Repeat(phase,1)*clip.length);graph.Evaluate(0);
                    feet.Correct(Vector3.forward*speed,state,phase,stride,false,targetFacing:false,deltaTime:dt);
                    for(int side=0;side<2;side++)
                    {
                        var hip=rig.GetBoneTransform(side==0?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg).position;
                        var knee=rig.GetBoneTransform(side==0?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg).position;
                        var foot=rig.GetBoneTransform(side==0?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot).position;
                        float angle=Vector3.Angle(hip-knee,foot-knee);knee-=root.transform.position;
                        if(phase>=1)
                        {
                            float angular=Mathf.Abs(angle-previousAngles[side])/dt,speedNow=Vector3.Distance(knee,previousKnees[side])/dt;
                            if(angular>angularSpeed){angularSpeed=angular;angleAt=phase;}
                            if(speedNow>kneeSpeed){kneeSpeed=speedNow;kneeAt=phase;}
                            minimum[side]=Mathf.Min(minimum[side],angle);maximum[side]=Mathf.Max(maximum[side],angle);samples++;
                        }
                        previousAngles[side]=angle;previousKnees[side]=knee;
                    }
                }
                Debug.Log($"LEG_CONTINUITY {hero}/{shaped}/{state} slope={slope} clearance={clearance} angularDegPerSec={angularSpeed:F2} kneeMps={kneeSpeed:F3} angleAt={angleAt:F6} kneeAt={kneeAt:F6}");
                Assert.Greater(samples,100,"must cover complete repeated steps");
                var failures=new System.Collections.Generic.List<string>();
                if(!(angularSpeed<=(state=="Walk"?1200:3000)))failures.Add("knee extension snaps during landing/toe-off: "+angularSpeed+" deg/s");
                if(!(kneeSpeed<=(state=="Walk"?6:state=="Run"?15:18)))failures.Add("knee bend plane flips: "+kneeSpeed+" m/s");
                for(int side=0;side<2;side++)if(!(maximum[side]-minimum[side]>25))failures.Add("a frozen knee is not a repair: side "+side);
                Assert.IsEmpty(failures,string.Join("; ",failures));
            }
            finally{graph.Destroy();Object.DestroyImmediate(root);Object.DestroyImmediate(floor);}
        }
    }
}
