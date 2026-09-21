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
        public static void ProbeFox()=>BatchTools.Run(()=>
        {
            var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Animation/Fox-Skeleton.fbx").OfType<AnimationClip>().First(c=>c.name.EndsWith("Walk"));
            foreach(var path in new[]{"Assets/_Project/Art/Animation/Fox-Skeleton.fbx","Assets/_Project/Art/Generated/Models/Ridgehound/Ridgehound_clean.fbx"})
            {
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var animator=model.GetComponentInChildren<Animator>();
                var basis=animator!=null?animator.transform:model.transform;
                Debug.Log("FOX_ROOT "+path+" animator="+(animator!=null?animator.name:"none")+" paths="+string.Join(",",model.GetComponentsInChildren<Transform>().Select(t=>AnimationUtility.CalculateTransformPath(t,basis))));
            }
            Debug.Log("FOX_BINDINGS "+string.Join(",",AnimationUtility.GetCurveBindings(clip).Select(b=>b.path+":"+b.propertyName).Distinct().Take(24)));
            Debug.Log("FOX_PROBE_OK");
        });
        public static void StageFox()=>BatchTools.Run(()=>
        {
            const string path="Assets/_Project/Art/Animation/Fox-Skeleton.fbx";
            PackStaging.StageFile("art-src/Donors/Fox-Skeleton.fbx","Art/Animation/Fox-Skeleton.fbx");
            PackStaging.StageFile("art-src/Donors/Fox-License.txt","Art/Animation/Fox-License.txt");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Generic;importer.optimizeGameObjects=false;
            importer.SaveAndReimport();var clips=importer.defaultClipAnimations;
            foreach(var clip in clips){clip.loopTime=!clip.name.EndsWith("Attack");clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;}
            importer.clipAnimations=clips;importer.SaveAndReimport();
            if(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>(true).Length!=0)throw new InvalidOperationException("Fox donor mesh survived");
            var sourceClips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            var rows=sourceClips.Select(source=>
            {
                string destination="Assets/_Project/Art/Animation/Fox-"+source.name.Split('|').Last()+".anim";
                var copy=AssetDatabase.LoadAssetAtPath<AnimationClip>(destination);
                if(copy==null){copy=new AnimationClip();AssetDatabase.CreateAsset(copy,destination);}
                EditorUtility.CopySerialized(source,copy);copy.name=source.name;
                foreach(var binding in AnimationUtility.GetCurveBindings(copy).Where(b=>string.IsNullOrEmpty(b.path)||b.path=="AnimalArmature"))AnimationUtility.SetEditorCurve(copy,binding,null);
                EditorUtility.SetDirty(copy);return new{path=destination,name=copy.name,length=copy.length,human=copy.humanMotion};
            }).ToArray();AssetDatabase.SaveAssets();
            if(rows.Length!=3)throw new InvalidOperationException("Fox clips missing");
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../docs/art/fox-clips.json")),JsonConvert.SerializeObject(rows,Formatting.Indented));
            Debug.Log("FOX_DONOR_OK clips="+rows.Length);
        });
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
