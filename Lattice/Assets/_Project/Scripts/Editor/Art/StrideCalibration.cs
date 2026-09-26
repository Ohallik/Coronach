using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

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
