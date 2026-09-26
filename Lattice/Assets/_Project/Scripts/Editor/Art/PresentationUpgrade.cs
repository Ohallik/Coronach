using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Data;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    public static class PresentationUpgrade
    {
        const string AnimRoot="Assets/_Project/Art/Animation/";
        static readonly string Root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        public static void Apply()=>BatchTools.Run(()=>
        {
            foreach(string name in new[]{"Title Theme","Hub Town Groove","Moonbase Market","Adventure Awaits"})
                PackStaging.StageFile("Music/"+name+".ogg","Resources/Audio/Music/"+name+".ogg");
            foreach(string name in new[]{"HeroLocomotion","HeroCombat"})PackStaging.StageFile("art-src/Donors/"+name+".fbx","Art/Animation/"+name+".fbx");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/_Project/Resources/Audio/Music"}))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.85f;
                importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.loadInBackground=true;importer.SaveAndReimport();
            }
            ConfigureDonor("HeroLocomotion",new[]{
                Spec("Idle","Idle_Loop",true),Spec("Walk","Walk_Loop",true,rotation:7),Spec("Run","Jog_Fwd_Loop",true,rotation:28),Spec("Sprint","Sprint_Loop",true,rotation:20),
                Spec("CombatIdle","Sword_Idle",true),Spec("RangedIdle","Pistol_Idle_Loop",true,mirror:true),Spec("Shoot","Pistol_Shoot",false,mirror:true),
                Spec("Dodge","Roll",false,0,30),Spec("Stagger","Hit_Chest",false),Spec("Buff","Spell_Simple_Shoot",false)});
            ConfigureDonor("HeroCombat",new[]{
                Spec("Attack1","Sword_Regular_A",false),Spec("Attack2","Sword_Regular_B",false),Spec("Attack3","Sword_Regular_C",false,0,32),
                Spec("Guard","Sword_Block",true,15,18),Spec("Cleave","Sword_Heavy_Combo",false,40,75),
                Spec("Dash","Sword_Dash",false,6,28),Spec("Pulse","OverhandThrow",false)});
            string intake=Path.Combine(Root,"docs/art/intake.json");
            var rows=JsonConvert.DeserializeObject<GenIntake.Row[]>(File.ReadAllText(intake));
            foreach(var row in rows.Where(r=>r.character=="Taren"||r.character=="Sela"))
            {
                if(row.kind=="biped")
                {
                    var clips=new List<GenIntake.ClipRow>();
                    foreach(string state in new[]{"Idle","Walk","Run","Sprint","Shoot","Dodge","Stagger","Buff"})
                        clips.Add(new GenIntake.ClipRow{state=state,path=AnimRoot+"HeroLocomotion.fbx",name=state=="Idle"&&row.form=="Shaped"?(row.character=="Taren"?"CombatIdle":"RangedIdle"):state});
                    foreach(string state in new[]{"Attack1","Attack2","Attack3","Guard","Cleave","Dash","Pulse"})
                        clips.Add(new GenIntake.ClipRow{state=state,path=AnimRoot+"HeroCombat.fbx",name=state});
                    row.clips=clips.ToArray();
                }
                GenIntake.Build(row);
            }
            File.WriteAllText(intake,JsonConvert.SerializeObject(rows,Formatting.Indented)+"\n");
            foreach(string guid in AssetDatabase.FindAssets("t:Hd2dProfile"))
            {
                var profile=AssetDatabase.LoadAssetAtPath<Hd2dProfile>(AssetDatabase.GUIDToAssetPath(guid));
                profile.nativeResolution=true;profile.internalWidth=1920;profile.internalHeight=1080;profile.tiltStrength=0;EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();Debug.Log("PRESENTATION_INTAKE_OK");
        });
        public static void AlignLocomotion()=>BatchTools.Run(()=>
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(AnimRoot+"HeroLocomotion.fbx");
            var clips=importer.clipAnimations;
            foreach(var clip in clips)
                if(clip.name=="Walk"||clip.name=="Run"||clip.name=="Sprint")
                {
                    // Calibrated against visible generated rigs and independent hip/
                    // shoulder spans. Keep actor, idle, aiming and attack axes intact.
                    clip.keepOriginalOrientation=false;
                    clip.rotationOffset=clip.name=="Walk"?7:clip.name=="Run"?28:20;
                }
            importer.clipAnimations=clips;importer.SaveAndReimport();
            Debug.Log("LOCOMOTION_ALIGNMENT_OK");
        });
        struct ClipSpec { public string name,take;public bool loop,mirror;public float start,end,rotation; }
        static ClipSpec Spec(string name,string take,bool loop,float start=0,float end=-1,bool mirror=false,float rotation=0)=>new(){name=name,take=take,loop=loop,start=start,end=end,mirror=mirror,rotation=rotation};
        static void ConfigureDonor(string name,ClipSpec[] specs)
        {
            string path=AnimRoot+name+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.optimizeGameObjects=false;importer.SaveAndReimport();
            var map=new Dictionary<string,string>{{"pelvis","Hips"},{"spine_01","Spine"},{"spine_02","Chest"},{"spine_03","UpperChest"},{"neck_01","Neck"},{"Head","Head"}};
            foreach(var side in new[]{("l","Left"),("r","Right")})
            {
                foreach(var pair in new[]{("clavicle","Shoulder"),("upperarm","UpperArm"),("lowerarm","LowerArm"),("hand","Hand"),("thigh","UpperLeg"),("calf","LowerLeg"),("foot","Foot"),("ball","Toes")})map[pair.Item1+"_"+side.Item1]=side.Item2+pair.Item2;
                foreach(var finger in new[]{("thumb","Thumb"),("index","Index"),("middle","Middle"),("ring","Ring"),("pinky","Little")})
                    for(int i=1;i<=3;i++)map[finger.Item1+"_0"+i+"_"+side.Item1]=side.Item2+" "+finger.Item2+" "+new[]{"Proximal","Intermediate","Distal"}[i-1];
            }
            var desc=importer.humanDescription;var names=new HashSet<string>(desc.skeleton.Select(b=>b.name));
            desc.human=map.Where(m=>names.Contains(m.Key)).Select(m=>new HumanBone{boneName=m.Key,humanName=m.Value,limit=new HumanLimit{useDefaultValues=true}}).ToArray();importer.humanDescription=desc;
            var available=importer.defaultClipAnimations;
            importer.clipAnimations=specs.Select(spec=>
            {
                var source=available.Single(c=>c.name.Split('|').Last()==spec.take);
                return new ModelImporterClipAnimation{name=spec.name,takeName=source.takeName,firstFrame=source.firstFrame+spec.start,lastFrame=spec.end<0?source.lastFrame:Mathf.Min(source.lastFrame,source.firstFrame+spec.end),loopTime=spec.loop,loopPose=spec.loop,mirror=spec.mirror,lockRootRotation=true,lockRootHeightY=true,lockRootPositionXZ=true,keepOriginalOrientation=false,rotationOffset=spec.rotation,keepOriginalPositionXZ=true,heightFromFeet=true};
            }).ToArray();
            importer.SaveAndReimport();
            var assets=AssetDatabase.LoadAllAssetsAtPath(path);
            if(!assets.OfType<Avatar>().Any(a=>a.isHuman)||assets.OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).Any(c=>!c.humanMotion))throw new Exception("Invalid humanoid donor "+name);
            if(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>(true).Length!=0)throw new Exception("Donor mesh survived "+name);
            Debug.Log("HERO_DONOR_OK "+name+" clips="+specs.Length);
        }
    }
}
