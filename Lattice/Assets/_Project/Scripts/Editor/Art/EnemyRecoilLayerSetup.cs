using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Lattice.EditorTools
{
    // Preserve lower-body travel while a non-breaking impact recoils the torso.
    public static class EnemyRecoilLayerSetup
    {
        internal static void Configure(AnimatorController controller)
        {
            const string path="Assets/_Project/Art/Animation/EnemyRecoil.mask";
            var mask=AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            if(mask==null){mask=new AvatarMask{name="Enemy upper-body recoil"};AssetDatabase.CreateAsset(mask,path);}
            for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
            foreach(var part in new[]{AvatarMaskBodyPart.Body,AvatarMaskBodyPart.Head,AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers})
                mask.SetHumanoidBodyPartActive(part,true);
            EditorUtility.SetDirty(mask);
            if(!controller.parameters.Any(p=>p.name=="RecoilRate"))
                controller.AddParameter(new AnimatorControllerParameter{name="RecoilRate",type=AnimatorControllerParameterType.Float,defaultFloat=1});
            if(!controller.layers.Any(l=>l.name=="Recoil"))controller.AddLayer("Recoil");
            var layers=controller.layers;var layer=layers.Single(l=>l.name=="Recoil");
            layer.avatarMask=mask;layer.defaultWeight=0;layer.blendingMode=AnimatorLayerBlendingMode.Override;layer.iKPass=false;
            var state=layer.stateMachine.states.Select(s=>s.state).SingleOrDefault(s=>s.name=="Recoil")??layer.stateMachine.AddState("Recoil");
            const string clipPath="Assets/_Project/Art/Animation/SentinelRecoil.anim";
            var donor=(AnimationClip)layers[0].stateMachine.states.Single(s=>s.state.name=="Stagger").state.motion;
            var recoil=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if(recoil==null){recoil=new AnimationClip();AssetDatabase.CreateAsset(recoil,clipPath);}
            EditorUtility.CopySerialized(donor,recoil);recoil.name="SentinelRecoil";
            // The donor's lower-body shift disappears under the travel mask.
            // Author a bounded spine bend so the torso itself receives impact.
            var bend=new AnimationCurve(new Keyframe(0,0),new Keyframe(donor.length*.65f,-.65f),new Keyframe(donor.length,-.8f));
            AnimationUtility.SetEditorCurve(recoil,EditorCurveBinding.FloatCurve("",typeof(Animator),"Spine Front-Back"),bend);
            EditorUtility.SetDirty(recoil);state.motion=recoil;
            state.speed=1;state.speedParameter="RecoilRate";state.speedParameterActive=true;state.iKOnFeet=false;state.writeDefaultValues=false;
            layer.stateMachine.defaultState=state;controller.layers=layers;
            EditorUtility.SetDirty(state);EditorUtility.SetDirty(layer.stateMachine);EditorUtility.SetDirty(controller);
        }
        public static void Install()=>BatchTools.Run(()=>
        {
            const string path="Assets/_Project/Art/Generated/Models/SentinelHusk/SentinelHusk.controller";
            string guid=AssetDatabase.AssetPathToGUID(path);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller==null)throw new InvalidOperationException("Missing Sentinel controller");
            Configure(controller);
            if(guid!=AssetDatabase.AssetPathToGUID(path))throw new InvalidOperationException("Sentinel controller GUID changed");
            AssetDatabase.SaveAssets();Debug.Log("ENEMY_RECOIL_LAYER_OK");
        });
        public static void Inspect()=>BatchTools.Run(()=>
        {
            var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Animation/HeroLocomotion.fbx").OfType<AnimationClip>().Single(c=>c.name=="Stagger");
            foreach(var binding in AnimationUtility.GetCurveBindings(clip).Where(b=>b.propertyName.Contains("Spine")||b.propertyName.Contains("Chest")))
            {
                var curve=AnimationUtility.GetEditorCurve(clip,binding);
                Debug.Log($"RECOIL_CHANNEL {binding.path} {binding.type} {binding.propertyName} start={curve.Evaluate(0)} peak={curve.Evaluate(clip.length*.25f)} end={curve.Evaluate(clip.length)} length={clip.length}");
            }
            Debug.Log("RECOIL_INSPECT_OK");
        });
    }
}
