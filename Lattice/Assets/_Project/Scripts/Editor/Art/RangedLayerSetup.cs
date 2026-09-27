using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Lattice.EditorTools
{
    public static class RangedLayerSetup
    {
        internal static void Configure(AnimatorController controller)
        {
            const string maskPath="Assets/_Project/Art/Animation/SelaEmitter.mask";
            var mask=AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
            if(mask==null){mask=new AvatarMask{name="Sela emitter upper body"};AssetDatabase.CreateAsset(mask,maskPath);}
            for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
            foreach(var part in new[]{AvatarMaskBodyPart.Body,AvatarMaskBodyPart.Head,AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers})
                mask.SetHumanoidBodyPartActive(part,true);
            EditorUtility.SetDirty(mask);
            if(!controller.parameters.Any(p=>p.name=="EmitterRate"))
                controller.AddParameter(new AnimatorControllerParameter{name="EmitterRate",type=AnimatorControllerParameterType.Float,defaultFloat=1});
            if(!controller.layers.Any(l=>l.name=="Emitter"))controller.AddLayer("Emitter");
            var layers=controller.layers;var layer=layers.Single(l=>l.name=="Emitter");
            layer.avatarMask=mask;layer.defaultWeight=0;layer.blendingMode=AnimatorLayerBlendingMode.Override;layer.iKPass=false;
            var state=layer.stateMachine.states.Select(s=>s.state).SingleOrDefault(s=>s.name=="Shoot")??layer.stateMachine.AddState("Shoot");
            state.motion=layers[0].stateMachine.states.Single(s=>s.state.name=="Shoot").state.motion;
            state.speed=1;state.speedParameter="EmitterRate";state.speedParameterActive=true;state.iKOnFeet=false;state.writeDefaultValues=false;
            layer.stateMachine.defaultState=state;controller.layers=layers;
            EditorUtility.SetDirty(state);EditorUtility.SetDirty(layer.stateMachine);EditorUtility.SetDirty(controller);
        }
        public static void Install()=>BatchTools.Run(()=>
        {
            foreach(string body in new[]{"SelaNatural","SelaShaped"})
            {
                string path="Assets/_Project/Art/Generated/Models/"+body+"/"+body+".controller";
                string guid=AssetDatabase.AssetPathToGUID(path);
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if(controller==null)throw new InvalidOperationException("Missing Sela controller "+path);
                Configure(controller);
                if(guid!=AssetDatabase.AssetPathToGUID(path))throw new InvalidOperationException("Controller GUID changed "+path);
            }
            AssetDatabase.SaveAssets();Debug.Log("RANGED_LAYER_OK");
        });
    }
}
