using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lattice.EditorTools
{
    public static class StrideCalibration
    {
        internal static void ConfigureController(AnimatorController controller)
        {
            if(!controller.parameters.Any(p=>p.name=="TravelSign"))
                controller.AddParameter(new AnimatorControllerParameter{name="TravelSign",type=AnimatorControllerParameterType.Float,defaultFloat=1});
            foreach(var child in controller.layers[0].stateMachine.states)
                if(child.state.name=="Walk"||child.state.name=="Run"||child.state.name=="Sprint")
                {child.state.speedParameter="TravelSign";child.state.speedParameterActive=true;EditorUtility.SetDirty(child.state);}
            EditorUtility.SetDirty(controller);
        }
        public static void Install()=>BatchTools.Run(()=>
        {
            foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})
            {
                string key=hero+(shaped?"Shaped":"Natural");
                var definition=GameCatalog.Find<CharacterDef>(hero);
                string path=AssetDatabase.GetAssetPath(shaped?definition.shaped:definition.natural);
                var body=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var profile=Resources.Load<GroundStrideProfile>("Motion/"+key);
                    if(profile==null)throw new System.InvalidOperationException("Missing sole calibration "+key);
                    body.GetComponent<GeneratedAnimator>().strideProfile=profile;
                    ConfigureController((AnimatorController)body.GetComponentInChildren<Animator>().runtimeAnimatorController);
                    PrefabUtility.SaveAsPrefabAsset(body,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(body);}
            }
            AssetDatabase.SaveAssets();Debug.Log("STRIDE_INSTALLED_OK");
        });
        /// <summary>The planted-toe windows the contact checks hold a sole still in (StrideContactAudit.Contact), and the second foot's offset.</summary>
        static (float from,float to,float other) Stance(string clip)=>clip=="Walk"?(.2f,.4f,.5f):clip=="Run"?(.08f,.16f,.5f):(.095f,.16f,.55f);
        /// <summary>A body's own walk, run and sprint speeds, read off its visible sole: how fast each planted toe travels back under the
        /// in-place take through the central stance. A profile at that speed holds the sole still. Taren's and Sela's stored speeds came
        /// from the same trajectories read by hand (D076); <see cref="Measure"/> reads them back as the check on this.</summary>
        internal static (float walk,float run,float sprint) NativeSpeeds(GameObject prefab)
        {
            var body=Object.Instantiate(prefab);
            try
            {
                body.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var driver=body.GetComponent<GeneratedAnimator>();if(driver!=null)Object.DestroyImmediate(driver);
                var animator=body.GetComponentInChildren<Animator>();var clips=animator.runtimeAnimatorController.animationClips;
                if(animator.GetBoneTransform(HumanBodyBones.LeftToes)==null||animator.GetBoneTransform(HumanBodyBones.RightToes)==null)
                    throw new System.InvalidOperationException(prefab.name+" has no toe bones: GroundFeet plants the toe");
                var soles=new[]{new SoleMarkers(animator,true),new SoleMarkers(animator,false)};
                animator.runtimeAnimatorController=null;
                float Speed(string state)
                {
                    var clip=clips.Single(c=>c.name==state);var (from,to,other)=Stance(state);
                    var graph=PlayableGraph.Create("native sole speed");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    try
                    {
                        var play=AnimationClipPlayable.Create(graph,clip);play.SetApplyFootIK(true);
                        AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(play);graph.Play();
                        float Toe(int side,float phase){play.SetTime(Mathf.Repeat(phase+side*other,1)*clip.length);graph.Evaluate(0);return soles[side].Toe.z;}
                        float travelled=0;for(int side=0;side<2;side++)travelled+=Toe(side,from)-Toe(side,to);
                        return travelled*.5f/((to-from)*clip.length);
                    }
                    finally{graph.Destroy();}
                }
                return (Speed("Walk"),Speed("Run"),Speed("Sprint"));
            }
            finally{Object.DestroyImmediate(body);}
        }
        /// <summary>Prints every hero body's sole-measured speeds beside its stored profile.
        /// Run: exec-when-green.ps1 -Method Lattice.EditorTools.StrideCalibration.Measure -Marker STRIDE_MEASURE_OK</summary>
        public static void Measure()=>BatchTools.Run(()=>
        {
            foreach(string hero in new[]{"Taren","Sela","Idra"})foreach(bool shaped in new[]{false,true})
            {
                var definition=GameCatalog.Find<CharacterDef>(hero);var prefab=definition==null?null:shaped?definition.shaped:definition.natural;
                if(prefab==null){Debug.Log("STRIDE_MEASURE "+hero+" "+shaped+" no body");continue;}
                var (walk,run,sprint)=NativeSpeeds(prefab);var stored=prefab.GetComponent<GeneratedAnimator>()?.strideProfile;
                var animator=prefab.GetComponentInChildren<Animator>();
                Debug.Log(System.FormattableString.Invariant($"STRIDE_MEASURE {prefab.name} walk={walk:F3} run={run:F3} sprint={sprint:F3} stored={(stored==null?"none":$"{stored.walkSpeed:F2}/{stored.runSpeed:F2}/{stored.sprintSpeed:F2}")} humanScale={animator.humanScale:F3}"));
            }
            Debug.Log("STRIDE_MEASURE_OK");
        });
        /// <summary>Idra's two ground bodies (IdraNatural, and IdraBraced in her braced form's slot): sole markers from her own generated boots, as
        /// <see cref="Build"/> takes Taren's and Sela's, and walk, run and sprint speeds measured off her sole (<see cref="NativeSpeeds"/>, calibrated
        /// to the convention Taren's and Sela's profiles were stored in), stored at
        /// Motion/IdraNatural and Motion/IdraBraced (where art intake looks for them) and installed on both bodies.
        /// Run: exec-when-green.ps1 -Method Lattice.EditorTools.StrideCalibration.BuildIdra -Marker STRIDE_IDRA_OK</summary>
        public static void BuildIdra()=>BatchTools.Run(()=>
        {
            const string folder="Assets/_Project/Resources/Motion";
            var definition=GameCatalog.Find<CharacterDef>("Idra");
            if(definition==null)throw new System.InvalidOperationException("Idra has no CharacterDef");
            // Calibrate the sole reading to the convention the first two heroes' profiles were stored in: their run and sprint read
            // back within 0.7%, but each stored walk is about 4.7% over its toe's mid-stance speed (the walk's heel lands faster than
            // its toe rolls). Idra's speeds are her own readings scaled by the mean of those four ratios, gait by gait.
            float walkRatio=0,runRatio=0,sprintRatio=0;int references=0;
            foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})
            {
                var reference=GameCatalog.Find<CharacterDef>(hero);var body=shaped?reference.shaped:reference.natural;var stored=body.GetComponent<GeneratedAnimator>().strideProfile;
                var (w,r,s)=NativeSpeeds(body);walkRatio+=stored.walkSpeed/w;runRatio+=stored.runSpeed/r;sprintRatio+=stored.sprintSpeed/s;references++;
            }
            walkRatio/=references;runRatio/=references;sprintRatio/=references;
            Debug.Log(System.FormattableString.Invariant($"STRIDE_IDRA_CONVENTION walk={walkRatio:F4} run={runRatio:F4} sprint={sprintRatio:F4}"));
            foreach(var prefab in new[]{definition.natural,definition.shaped})
            {
                if(prefab==null||!prefab.name.StartsWith("Idra"))throw new System.InvalidOperationException("Idra stands in a borrowed body: "+(prefab==null?"none":prefab.name));
                var (walk,run,sprint)=NativeSpeeds(prefab);walk*=walkRatio;run*=runRatio;sprint*=sprintRatio;
                string path=folder+"/"+prefab.name+".asset";
                var profile=AssetDatabase.LoadAssetAtPath<GroundStrideProfile>(path);
                if(profile==null){profile=ScriptableObject.CreateInstance<GroundStrideProfile>();AssetDatabase.CreateAsset(profile,path);}
                var body=Object.Instantiate(prefab);
                try
                {
                    var animator=body.GetComponentInChildren<Animator>();
                    var left=new SoleMarkers(animator,true);var right=new SoleMarkers(animator,false);
                    profile.leftHeel=animator.GetBoneTransform(HumanBodyBones.LeftFoot).InverseTransformPoint(left.Heel);
                    profile.rightHeel=animator.GetBoneTransform(HumanBodyBones.RightFoot).InverseTransformPoint(right.Heel);
                    profile.leftToe=animator.GetBoneTransform(HumanBodyBones.LeftToes).InverseTransformPoint(left.Toe);
                    profile.rightToe=animator.GetBoneTransform(HumanBodyBones.RightToes).InverseTransformPoint(right.Toe);
                }
                finally{Object.DestroyImmediate(body);}
                profile.walkSpeed=Mathf.Round(walk*100)/100;profile.runSpeed=Mathf.Round(run*100)/100;profile.sprintSpeed=Mathf.Round(sprint*100)/100;
                profile.holdsHandover=true;
                EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
                string prefabPath=AssetDatabase.GetAssetPath(prefab);var contents=PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    contents.GetComponent<GeneratedAnimator>().strideProfile=profile;
                    ConfigureController((AnimatorController)contents.GetComponentInChildren<Animator>().runtimeAnimatorController);
                    PrefabUtility.SaveAsPrefabAsset(contents,prefabPath);
                }
                finally{PrefabUtility.UnloadPrefabContents(contents);}
                Debug.Log(System.FormattableString.Invariant($"STRIDE_IDRA {prefab.name} walk={profile.walkSpeed:F2} run={profile.runSpeed:F2} sprint={profile.sprintSpeed:F2} leftHeel={profile.leftHeel} leftToe={profile.leftToe}"));
            }
            AssetDatabase.SaveAssets();Debug.Log("STRIDE_IDRA_OK");
        });
        public static void Build()=>BatchTools.Run(()=>
        {
            const string folder="Assets/_Project/Resources/Motion";
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})
            {
                var definition=GameCatalog.Find<CharacterDef>(hero);
                var body=Object.Instantiate(shaped?definition.shaped:definition.natural);
                try
                {
                    var animator=body.GetComponentInChildren<Animator>();
                    var left=new SoleMarkers(animator,true);var right=new SoleMarkers(animator,false);
                    string path=folder+"/"+hero+(shaped?"Shaped":"Natural")+".asset";
                    var profile=AssetDatabase.LoadAssetAtPath<GroundStrideProfile>(path);
                    if(profile==null){profile=ScriptableObject.CreateInstance<GroundStrideProfile>();AssetDatabase.CreateAsset(profile,path);}
                    profile.leftHeel=animator.GetBoneTransform(HumanBodyBones.LeftFoot).InverseTransformPoint(left.Heel);
                    profile.rightHeel=animator.GetBoneTransform(HumanBodyBones.RightFoot).InverseTransformPoint(right.Heel);
                    profile.leftToe=animator.GetBoneTransform(HumanBodyBones.LeftToes).InverseTransformPoint(left.Toe);
                    profile.rightToe=animator.GetBoneTransform(HumanBodyBones.RightToes).InverseTransformPoint(right.Toe);
                    // Values measured from independent low-sole trajectories,
                    // excluding heel roll and toe-off from the planted interval.
                    profile.walkSpeed=shaped&&hero=="Taren"?1.12f:1.1f;
                    profile.runSpeed=shaped?(hero=="Taren"?6.48f:6.30f):6.38f;
                    profile.sprintSpeed=shaped?(hero=="Taren"?9.76f:9.45f):9.6f;
                    EditorUtility.SetDirty(profile);
                    Debug.Log($"STRIDE_CALIBRATION {body.name} humanScale={animator.humanScale} leftBottom={animator.leftFeetBottomHeight} rightBottom={animator.rightFeetBottomHeight}");
                }
                finally{Object.DestroyImmediate(body);}
            }
            AssetDatabase.SaveAssets();Debug.Log("STRIDE_CALIBRATION_OK");
        });
    }
}
