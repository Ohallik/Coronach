using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Uphill step-height audit (November 4, docs/validation/C2-uphill-fold.md). Walks each hero and form up 30 and 39
    /// degree floors at the recorded 1.47 m/s through the real GroundFeet and reports the stepping knee, its height under
    /// the hip, the hip height against flat ground and the hip bob, against bounds that reject the recorded fold (Sela,
    /// 60.88 degrees with the knee at hip height, hips at 60%, 19 cm bob). OPEN: the committed walk fails for both heroes;
    /// five bounded tuning variants broke planted-sole or knee-continuity checks. Diagnostic only, not a test.
    /// Run: exec.ps1 -Method Lattice.EditorTools.UphillStepAudit.Run -Marker UPHILL_STEP_AUDIT
    /// </summary>
    public static class UphillStepAudit
    {
        public static void Run()=>BatchTools.Run(()=>
        {
            int open=0;
            foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})foreach(float slope in new[]{30f,39f})
                Measure(hero,shaped,slope,ref open);
            Debug.Log(open==0?"UPHILL_STEP_AUDIT PASS":"UPHILL_STEP_AUDIT OPEN cases="+open);
        });
        static void Measure(string hero,bool shaped,float slope,ref int open)
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(100,1,100);
            var root=new GameObject("uphill step height regression");
            var definition=GameCatalog.Find<CharacterDef>(hero);
            var body=Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
            var rig=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
            var profile=driver.strideProfile;Object.DestroyImmediate(driver);
            var clip=rig.runtimeAnimatorController.animationClips.Single(c=>c.name=="Walk");rig.runtimeAnimatorController=null;
            var feet=body.AddComponent<GroundFeet>();feet.Initialize(rig,root.transform,profile);
            var graph=PlayableGraph.Create("uphill step height");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var pose=AnimationClipPlayable.Create(graph,clip);pose.SetApplyFootIK(true);
            AnimationPlayableOutput.Create(graph,"pose",rig).SetSourcePlayable(pose);graph.Play();
            const float dt=1f/120,speed=1.47f;
            // The root rides where the hero's capsule does: 8 cm over flat ground, and higher over a slope by the
            // capsule's radius effect (r/cos - r), which GroundFeet removes from the rendered body again.
            float RootClearance(float degrees)=>.08f+.35f*(1/Mathf.Cos(degrees*Mathf.Deg2Rad)-1);
            var hips=rig.GetBoneTransform(HumanBodyBones.Hips);
            try
            {
                // Flat-ground hip height is the reference for how far the slope may crouch the body.
                float FlatHipHeight()
                {
                    floor.transform.rotation=Quaternion.identity;Physics.SyncTransforms();feet.ResetContacts();
                    float rate=profile.Cadence("Walk",speed,0),stride=profile.Stride("Walk",speed,rate,0),sum=0;int n=0;
                    for(int frame=0;frame<Mathf.CeilToInt(clip.length/rate*2/dt);frame++)
                    {
                        float time=frame*dt,phase=time*rate/clip.length;root.transform.position=new Vector3(0,RootClearance(0),speed*time);
                        pose.SetTime(Mathf.Repeat(phase,1)*clip.length);graph.Evaluate(0);
                        feet.Correct(Vector3.forward*speed,"Walk",phase,stride,false,targetFacing:false,deltaTime:dt);
                        if(phase>=1){sum+=hips.position.y-root.transform.position.y;n++;}
                    }
                    return sum/n;
                }
                float flat=FlatHipHeight();
                floor.transform.rotation=Quaternion.Euler(-slope,0,0);Physics.SyncTransforms();feet.ResetContacts();
                var normal=floor.transform.up;var surface=floor.transform.position+normal*.5f;
                float grade=Mathf.Abs(normal.z)/normal.y,cadence=profile.Cadence("Walk",speed,grade),strideScale=profile.Stride("Walk",speed,cadence,grade);
                float minimumKnee=180,minimumKneeDrop=float.PositiveInfinity,stanceKnee=180,lowestHip=float.PositiveInfinity,highestHip=float.NegativeInfinity;int samples=0;string worstFrame="";
                for(int frame=0;frame<Mathf.CeilToInt(clip.length/cadence*3/dt);frame++)
                {
                    float time=frame*dt,phase=time*cadence/clip.length;
                    var position=Vector3.forward*speed*time;position.y=Vector3.Dot(normal,surface-position)/normal.y+RootClearance(slope);root.transform.position=position;
                    pose.SetTime(Mathf.Repeat(phase,1)*clip.length);graph.Evaluate(0);
                    feet.Correct(Vector3.forward*speed,"Walk",phase,strideScale,false,targetFacing:false,deltaTime:dt);
                    if(phase<1)continue;samples++;
                    // GroundFeet plants the left foot over the first half of its cycle (heel, then toe) and the right foot
                    // half a cycle later; the rest is each leg's swing, the stepping leg the recording showed folded.
                    foreach(var (hip,knee,ankle,offset) in new[]{(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,0f),(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,.5f)})
                    {
                        Vector3 h=rig.GetBoneTransform(hip).position,k=rig.GetBoneTransform(knee).position,a=rig.GetBoneTransform(ankle).position;
                        float legPhase=Mathf.Repeat(phase-offset,1),angle=Vector3.Angle(h-k,a-k);
                        if(legPhase<.5f){stanceKnee=Mathf.Min(stanceKnee,angle);continue;}
                        if(angle<minimumKnee)worstFrame=$"legPhase={legPhase:F2} drop={feet.RequestedSupportDrop:F3} hip={hips.position.y-position.y:F3} footOverHip={a.y-h.y:F3} footAhead={Vector3.Dot(a-h,Vector3.forward):F3}";
                        minimumKnee=Mathf.Min(minimumKnee,angle);minimumKneeDrop=Mathf.Min(minimumKneeDrop,h.y-k.y);
                    }
                    float height=hips.position.y-position.y+RootClearance(slope)-RootClearance(0);lowestHip=Mathf.Min(lowestHip,height);highestHip=Mathf.Max(highestHip,height);
                }
                Debug.Log($"UPHILL_STEP {hero}/{shaped} slope={slope} samples={samples} flatHip={flat:F3} lowestHip={lowestHip:F3} bob={highestHip-lowestHip:F3} swingKnee={minimumKnee:F2} swingKneeBelowHip={minimumKneeDrop:F3} stanceKnee={stanceKnee:F2} worst[{worstFrame}]");
                if(samples<=80)throw new System.InvalidOperationException("must cover repeated steps");
                // The recorded fold: knee interior 60.88 degrees with the knee level with its hip, hips at 60% of their
                // flat height and a 19 cm bob. A steep climb may bend the swing knee to about a right angle and crouch a
                // little; it may not fold the thigh up to the hip or squat the body.
                bool ok=minimumKnee>=80f&&minimumKneeDrop>=.12f&&lowestHip>=flat*.75f&&highestHip-lowestHip<=.12f;
                if(!ok)open++;
            }
            finally{graph.Destroy();Object.DestroyImmediate(root);Object.DestroyImmediate(floor);}
        }
    }
}
