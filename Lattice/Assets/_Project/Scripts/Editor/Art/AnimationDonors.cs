using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
namespace Lattice.EditorTools
{
    public static class AnimationDonors
    {
        public const string UalPath="Assets/_Project/Art/Animation/UAL-Skeleton.fbx";
        public static void Stage()=>BatchTools.Run(()=>
        {
            PackStaging.StageFile("art-src/Donors/UAL-Skeleton.fbx","Art/Animation/UAL-Skeleton.fbx");
            PackStaging.StageFile("art-src/Donors/UAL-License.txt","Art/Animation/UAL-License.txt");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(UalPath);
            importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.optimizeGameObjects=false;
            importer.SaveAndReimport();
            var map=new Dictionary<string,string>{{"pelvis","Hips"},{"spine_01","Spine"},{"spine_02","Chest"},{"spine_03","UpperChest"},{"neck_01","Neck"},{"Head","Head"}};
            foreach(var side in new[]{("l","Left"),("r","Right")})
            {
                foreach(var pair in new[]{("clavicle","Shoulder"),("upperarm","UpperArm"),("lowerarm","LowerArm"),("hand","Hand"),("thigh","UpperLeg"),("calf","LowerLeg"),("foot","Foot"),("ball","Toes")})map[pair.Item1+"_"+side.Item1]=side.Item2+pair.Item2;
                foreach(var finger in new[]{("thumb","Thumb"),("index","Index"),("middle","Middle"),("ring","Ring"),("pinky","Little")})
                    for(int i=1;i<=3;i++)map[finger.Item1+"_0"+i+"_"+side.Item1]=side.Item2+" "+finger.Item2+" "+new[]{"Proximal","Intermediate","Distal"}[i-1];
            }
            var description=importer.humanDescription;var names=new HashSet<string>(description.skeleton.Select(b=>b.name));
            description.human=map.Where(m=>names.Contains(m.Key)).Select(m=>new HumanBone{boneName=m.Key,humanName=m.Value,limit=new HumanLimit{useDefaultValues=true}}).ToArray();importer.humanDescription=description;
            var clips=importer.defaultClipAnimations;foreach(var clip in clips){clip.loopTime=clip.name.Contains("Loop");clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;}
            importer.clipAnimations=clips;importer.SaveAndReimport();
            var assets=AssetDatabase.LoadAllAssetsAtPath(UalPath);
            if(!assets.OfType<Avatar>().Any(a=>a.isHuman))throw new InvalidOperationException("UAL donor avatar is not human");
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(UalPath);if(model.GetComponentsInChildren<Renderer>(true).Length!=0)throw new InvalidOperationException("Donor mesh survived stripping");
            var rows=assets.OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).Select(c=>new{path=UalPath,name=c.name,length=c.length,human=c.humanMotion}).ToArray();
            if(rows.Length<3||rows.Any(c=>!c.human))throw new InvalidOperationException("UAL clips are not humanoid");
            var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));File.WriteAllText(Path.Combine(root,"docs/art/animation-clips.json"),JsonConvert.SerializeObject(rows,Formatting.Indented));
            AssetDatabase.SaveAssets();Debug.Log("ANIMATION_DONORS_OK clips="+rows.Length);
        });
    }
}
