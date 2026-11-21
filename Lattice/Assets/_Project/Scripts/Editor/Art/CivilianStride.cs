using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Stride for people who walk and never fight. GeneratedAnimator plays such a body's walk at its speed over the profile's walk speed, and without a profile at its
    /// speed over 2.1 m/s held to no slower than .7 (D195): Mira's .9 m/s walk home on the first morning played at .7, so each planted sole slid forward under her.
    /// Her profile is read off her own visible sole as the heroes' are (StrideCalibration), in the convention their stored walks keep, and stored where art intake
    /// looks for it (Resources/Motion/Mira), so a fresh intake of her body binds it again. Only the walk is measured: her body has no run or sprint.
    /// </summary>
    public static class CivilianStride
    {
        /// <summary>The walk's planted-toe window and the second foot's offset, as StrideCalibration and StrideContactAudit take them.</summary>
        const float StanceFrom=.2f,StanceTo=.4f,OtherFoot=.5f;
        const string Folder="Assets/_Project/Resources/Motion";
        /// <summary>How fast a body's planted toe travels back through the walk's central stance, its walk taken in place: the speed that holds that sole still.</summary>
        internal static float WalkSpeed(GameObject prefab)
        {
            var body=Object.Instantiate(prefab);
            try
            {
                body.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var driver=body.GetComponent<GeneratedAnimator>();if(driver!=null)Object.DestroyImmediate(driver);
                // The clip the Walk state plays: a hero's is named Walk, a townsperson's is the shared UAL take (Walk_Loop), retargeted to each body's own size.
                var animator=body.GetComponentInChildren<Animator>();var controller=animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                var state=controller!=null?controller.layers[0].stateMachine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Walk"):null;
                var walk=state?.motion as AnimationClip??animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name=="Walk");
                // Played as its own Walk state plays it. The heroes' states place the feet by IK and Mira's doesn't: read with IK on, her walk came out 4% faster than she
                // walks it, and her planted sole slid forward 8 cm a second under her.
                bool footIK=state==null||state.iKOnFeet;
                if(walk==null)throw new System.InvalidOperationException(prefab.name+"'s Walk state plays no clip");
                if(animator.GetBoneTransform(HumanBodyBones.LeftToes)==null||animator.GetBoneTransform(HumanBodyBones.RightToes)==null)throw new System.InvalidOperationException(prefab.name+" has no toe bones");
                var soles=new[]{new SoleMarkers(animator,true),new SoleMarkers(animator,false)};
                animator.runtimeAnimatorController=null;
                var graph=PlayableGraph.Create("civilian sole speed");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                try
                {
                    var play=AnimationClipPlayable.Create(graph,walk);play.SetApplyFootIK(footIK);
                    AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(play);graph.Play();
                    float Toe(int side,float phase){play.SetTime(Mathf.Repeat(phase+side*OtherFoot,1)*walk.length);graph.Evaluate(0);return soles[side].Toe.z;}
                    float travelled=0;for(int side=0;side<2;side++)travelled+=Toe(side,StanceFrom)-Toe(side,StanceTo);
                    return travelled*.5f/((StanceTo-StanceFrom)*walk.length);
                }
                finally{graph.Destroy();}
            }
            finally{Object.DestroyImmediate(body);}
        }
        /// <summary>How the heroes' stored walks stand to the same reading of their own soles (their walk's heel lands a little faster than its toe rolls): the mean over
        /// Taren's and Sela's four ground bodies. A civilian's walk is stored in the same convention, so one rule plants every sole.</summary>
        internal static float Convention()
        {
            float sum=0;int count=0;
            foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})
            {
                var definition=GameCatalog.Find<CharacterDef>(hero);var body=shaped?definition.shaped:definition.natural;
                var stored=body.GetComponent<GeneratedAnimator>().strideProfile;if(stored==null)throw new System.InvalidOperationException(body.name+" has no stride profile");
                sum+=stored.walkSpeed/WalkSpeed(body);count++;
            }
            return sum/count;
        }
        /// <summary>Prints each named body's walk speed in the heroes' convention: the pace at which an unprofiled walker's feet hold (GeneratedAnimator plays it at .7 below 1.47 m/s).
        /// Run: exec-when-green.ps1 -Method Lattice.EditorTools.CivilianStride.Measure -Marker CIVILIAN_STRIDE_MEASURE_OK -ExtraArgs @('-stride-bodies','Neve,OdaBody')</summary>
        public static void Measure()=>BatchTools.Run(()=>
        {
            float convention=Convention();
            foreach(string id in (DevArgs.Value("-stride-bodies")??"Mira").Split(','))
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+id.Trim()+".prefab");
                if(prefab==null){Debug.Log("CIVILIAN_STRIDE "+id+" no body");continue;}
                float walk=WalkSpeed(prefab)*convention;
                Debug.Log(System.FormattableString.Invariant($"CIVILIAN_STRIDE {prefab.name} walk={walk:F3} heldAt70={walk*.7f:F3} stored={(prefab.GetComponent<GeneratedAnimator>()?.strideProfile?.walkSpeed.ToString("F2")??"none")}"));
            }
            Debug.Log(System.FormattableString.Invariant($"CIVILIAN_STRIDE_MEASURE_OK convention={convention:F4}"));
        });
        /// <summary>Mira walked as the deck walks her: her own controller in its Walk state, the body carried at .9 m/s and the Animator stepped at the rate GeneratedAnimator
        /// sets (her pace over her profile's walk; without it, the floor rate .7). Prints how fast each planted toe slides forward through its stance, the reading
        /// TownLifeTests.MiraPlantsHerFeetOnHerWalkHome takes on the deck.
        /// Run: exec-when-green.ps1 -Method Lattice.EditorTools.CivilianStride.Slip -Marker CIVILIAN_STRIDE_SLIP_OK</summary>
        public static void Slip()=>BatchTools.Run(()=>
        {
            var prefab=GameCatalog.Find<CharacterDef>("Mira").natural;var profile=prefab.GetComponent<GeneratedAnimator>().strideProfile;
            if(profile==null)throw new System.InvalidOperationException("Mira has no stride profile to walk on");
            foreach(float rate in new[]{.9f/profile.walkSpeed,.7f})
            {
                var body=Object.Instantiate(prefab);
                try
                {
                    Object.DestroyImmediate(body.GetComponent<GeneratedAnimator>());body.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                    var animator=body.GetComponentInChildren<Animator>();var toes=new[]{animator.GetBoneTransform(HumanBodyBones.LeftToes),animator.GetBoneTransform(HumanBodyBones.RightToes)};var marks=new[]{profile.leftToe,profile.rightToe};
                    animator.Play("Walk",0,0);animator.speed=rate;animator.Update(0);
                    const float dt=1f/60;var slide=new List<float>();Vector3[] last=null;
                    for(int f=0;f<600;f++)
                    {
                        body.transform.position+=Vector3.forward*.9f*dt;animator.Update(dt);float phase=Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,1);
                        var now=new[]{toes[0].TransformPoint(marks[0]),toes[1].TransformPoint(marks[1])};
                        if(last!=null)for(int side=0;side<2;side++){float p=Mathf.Repeat(phase-side*OtherFoot,1);if(p>StanceFrom&&p<StanceTo)slide.Add((now[side].z-last[side].z)/dt);}
                        last=now;
                    }
                    Debug.Log(System.FormattableString.Invariant($"CIVILIAN_STRIDE_SLIP rate={rate:F3} slide={slide.Average():F3} m/s over {slide.Count} stance frames"));
                    // As a test runner samples it: the body moved this frame, the bones posed last frame, frame times swinging between 8 and 40 ms. A mean of each frame's speed
                    // and the distance over the time, summed.
                    var rng=new System.Random(5);var perFrame=new List<float>();float distance=0,time=0;Vector3[] before=null;float phaseBefore=0;
                    for(int f=0;f<900;f++)
                    {
                        float step=rng.Next(2)==0?.008f:.04f;
                        var posed=new[]{toes[0].TransformPoint(marks[0]),toes[1].TransformPoint(marks[1])};float phase=Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,1);
                        body.transform.position+=Vector3.forward*.9f*step;Physics.SyncTransforms();
                        var seen=new[]{toes[0].TransformPoint(marks[0]),toes[1].TransformPoint(marks[1])};
                        if(before!=null)for(int side=0;side<2;side++){float p=Mathf.Repeat(phase-side*OtherFoot,1);if(p>StanceFrom&&p<StanceTo){float moved=seen[side].z-before[side].z;perFrame.Add(moved/step);distance+=moved;time+=step;}}
                        before=seen;phaseBefore=phase;animator.Update(step);
                    }
                    Debug.Log(System.FormattableString.Invariant($"CIVILIAN_STRIDE_SLIP_LAGGED rate={rate:F3} perFrameMean={perFrame.Average():F3} summed={distance/time:F3} ({perFrame.Count})"));
                }
                finally{Object.DestroyImmediate(body);}
            }
            Debug.Log("CIVILIAN_STRIDE_SLIP_OK");
        });
        /// <summary>Mira's walk: her sole's markers and her walk speed, stored at Motion/Mira and set on her body. Her animator controller is left as it is: the reversing
        /// parameter the heroes' profiles bring is read only for a fighter.
        /// Run: exec-when-green.ps1 -Method Lattice.EditorTools.CivilianStride.BuildMira -Marker CIVILIAN_STRIDE_MIRA_OK</summary>
        public static void BuildMira()=>BatchTools.Run(()=>
        {
            var definition=GameCatalog.Find<CharacterDef>("Mira");
            var prefab=definition!=null?definition.natural:null;
            if(prefab==null||prefab.name!="Mira")throw new System.InvalidOperationException("Mira stands in a borrowed body: "+(prefab==null?"none":prefab.name));
            float convention=Convention(),walk=WalkSpeed(prefab)*convention;
            string path=Folder+"/Mira.asset";
            var profile=AssetDatabase.LoadAssetAtPath<GroundStrideProfile>(path);
            if(profile==null){profile=ScriptableObject.CreateInstance<GroundStrideProfile>();AssetDatabase.CreateAsset(profile,path);}
            var body=Object.Instantiate(prefab);
            try
            {
                var animator=body.GetComponentInChildren<Animator>();var left=new SoleMarkers(animator,true);var right=new SoleMarkers(animator,false);
                profile.leftHeel=animator.GetBoneTransform(HumanBodyBones.LeftFoot).InverseTransformPoint(left.Heel);profile.rightHeel=animator.GetBoneTransform(HumanBodyBones.RightFoot).InverseTransformPoint(right.Heel);
                profile.leftToe=animator.GetBoneTransform(HumanBodyBones.LeftToes).InverseTransformPoint(left.Toe);profile.rightToe=animator.GetBoneTransform(HumanBodyBones.RightToes).InverseTransformPoint(right.Toe);
            }
            finally{Object.DestroyImmediate(body);}
            profile.walkSpeed=Mathf.Round(walk*100)/100;
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
            string prefabPath=AssetDatabase.GetAssetPath(prefab);var contents=PrefabUtility.LoadPrefabContents(prefabPath);
            try{contents.GetComponent<GeneratedAnimator>().strideProfile=profile;PrefabUtility.SaveAsPrefabAsset(contents,prefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            AssetDatabase.SaveAssets();
            Debug.Log(System.FormattableString.Invariant($"CIVILIAN_STRIDE_MIRA_OK walk={profile.walkSpeed:F2} convention={convention:F4} leftToe={profile.leftToe}"));
        });
    }
}
