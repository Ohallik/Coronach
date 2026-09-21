using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace Lattice.EditorTools
{
    public static class GenIntake
    {
        [Serializable]public sealed class ClipRow{public string state,path,name;}
        [Serializable]public sealed class Row
        {
            public string id,model,albedo,emission,emissionColor="#22F5FF",kind="static",fit="height",folder="Environment",character,form,enemy;
            public float size=1.9f,yaw,pitch,roll;public ClipRow[] clips=Array.Empty<ClipRow>();
        }
        static readonly (string bone,string human)[] MeshyMap={
            ("Hips","Hips"),("Spine02","Spine"),("Spine01","Chest"),("Spine","UpperChest"),("neck","Neck"),("Head","Head"),
            ("LeftShoulder","LeftShoulder"),("LeftArm","LeftUpperArm"),("LeftForeArm","LeftLowerArm"),("LeftHand","LeftHand"),
            ("RightShoulder","RightShoulder"),("RightArm","RightUpperArm"),("RightForeArm","RightLowerArm"),("RightHand","RightHand"),
            ("LeftUpLeg","LeftUpperLeg"),("LeftLeg","LeftLowerLeg"),("LeftFoot","LeftFoot"),("LeftToeBase","LeftToes"),
            ("RightUpLeg","RightUpperLeg"),("RightLeg","RightLowerLeg"),("RightFoot","RightFoot"),("RightToeBase","RightToes")};
        static Row[] Rows()
        {
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../docs/art/intake.json"));
            var rows=JsonConvert.DeserializeObject<Row[]>(File.ReadAllText(path));
            if(rows==null||rows.Length==0)throw new InvalidOperationException("No approved model intake rows. An empty batch cannot pass.");
            if(rows.Select(r=>r.id).Distinct().Count()!=rows.Length)throw new InvalidOperationException("Duplicate intake id");
            return rows;
        }
        static string Dir(Row row)=>"Assets/_Project/Art/Generated/Models/"+row.id+"/";
        static string Model(Row row)=>Dir(row)+row.id+"_clean.fbx";
        static string Prefab(Row row)=>"Assets/_Project/Prefabs/"+row.folder+"/"+row.id+".prefab";
        public static void Build()=>BatchTools.Run(()=>
        {
            foreach(var row in Rows())Build(row);
            AssetDatabase.SaveAssets();Debug.Log("GEN_INTAKE_OK");
        });
        static void Build(Row row)
        {
            if(!System.Text.RegularExpressions.Regex.IsMatch(row.id,"^[A-Za-z0-9-]+$")||row.size<=0)throw new InvalidOperationException("Invalid id/size "+row.id);
            if(row.folder!="Characters"&&row.folder!="Enemies"&&row.folder!="Ships"&&row.folder!="Environment")throw new InvalidOperationException("Unknown prefab category");
            if(!row.model.EndsWith("_clean.fbx",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Strip embedded textures before staging: "+row.model);
            string destination="Art/Generated/Models/"+row.id+"/";
            PackStaging.StageFile(row.albedo,destination+row.id+"_base_color.png");
            if(!string.IsNullOrEmpty(row.emission))PackStaging.StageFile(row.emission,destination+row.id+"_emission.png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string materialPath=Dir(row)+row.id+"_Toon.mat";
            var stagedMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(stagedMaterial==null){stagedMaterial=new Material(Shader.Find("Lattice/Toon"));AssetDatabase.CreateAsset(stagedMaterial,materialPath);}
            stagedMaterial.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir(row)+row.id+"_base_color.png"));EditorUtility.SetDirty(stagedMaterial);AssetDatabase.SaveAssets();
            PackStaging.StageFile(row.model,destination+row.id+"_clean.fbx");AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(Model(row));
            // A replacement FBX must not retain the old rig's serialized rest pose.
            if(row.kind=="biped")
            {
                importer.animationType=ModelImporterAnimationType.Generic;
                var reset=importer.humanDescription;reset.human=Array.Empty<HumanBone>();reset.skeleton=Array.Empty<SkeletonBone>();importer.humanDescription=reset;importer.SaveAndReimport();
            }
            importer.animationType=row.kind=="biped"?ModelImporterAnimationType.Human:ModelImporterAnimationType.Generic;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.optimizeGameObjects=false;
            importer.SaveAndReimport();
            if(row.kind=="biped")Map(importer,false);importer.SaveAndReimport();
            if(row.kind=="biped"&&!Human(Model(row))){Map(importer,true);importer.SaveAndReimport();}
            if(row.kind=="biped"&&!Human(Model(row)))throw new InvalidOperationException("Avatar is not human: "+row.id);
            if(AssetDatabase.LoadAllAssetsAtPath(Model(row)).OfType<Texture2D>().Any())throw new InvalidOperationException("Embedded texture survived strip: "+row.id);
            var albedo=AssetDatabase.LoadAssetAtPath<Texture2D>(Dir(row)+row.id+"_base_color.png");
            if(albedo==null||Mathf.Max(albedo.width,albedo.height)>1024)throw new InvalidOperationException("Albedo intake failed: "+row.id);
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Dir(row)+row.id+"_Toon.mat");
            if(mat==null||mat.GetTexture("_BaseMap")!=albedo)throw new InvalidOperationException("Importer did not bind the unique sibling albedo: "+row.id);
            if(!string.IsNullOrEmpty(row.emission)){mat.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir(row)+row.id+"_emission.png"));ColorUtility.TryParseHtmlString(row.emissionColor,out var tint);mat.SetColor("_EmissionColor",tint*1.5f);EditorUtility.SetDirty(mat);}
            var root=new GameObject(row.id);var body=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model(row)),root.transform);body.name="Body";
            try
            {
                foreach(var renderer in body.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=Enumerable.Repeat(mat,Mathf.Max(1,renderer.sharedMaterials.Length)).ToArray();
                var animator=body.GetComponentInChildren<Animator>();
                if(row.kind=="biped")
                {
                    if(animator==null||!animator.isHuman)throw new InvalidOperationException("Humanoid animator missing: "+row.id);
                    var bounds=ModelGeometry.BoundsOf(body);
                    if(bounds.size.y<.001f)throw new InvalidOperationException("Skinned vertices have no vertical span: "+row.id);
                    body.transform.localScale*=row.size/bounds.size.y;
                    bounds=ModelGeometry.BoundsOf(body);body.transform.position-=Vector3.up*bounds.min.y;
                    if(Mathf.Abs(bounds.size.y-row.size)>.005f)throw new InvalidOperationException("Height normalization failed: "+row.id);
                    Debug.Log($"MODEL_HEIGHT {row.id} measured={bounds.size.y:0.000} target={row.size:0.000}");
                }
                else
                {
                    var renderers=body.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new InvalidOperationException("No model renderers");
                    body.transform.localRotation=Quaternion.Euler(row.pitch,row.yaw,row.roll);
                    var bounds=ModelGeometry.BoundsOf(body);
                    float measure=row.fit=="depth"?bounds.size.z:row.fit=="length"?Mathf.Max(bounds.size.x,bounds.size.z):bounds.size.y;
                    if(measure<.001f)throw new InvalidOperationException("Zero model bounds");body.transform.localScale*=row.size/measure;
                    bounds=ModelGeometry.BoundsOf(body);body.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                }
                if(row.kind=="biped"||row.kind=="generic")
                {
                    if(animator==null)throw new InvalidOperationException("Rigged model has no animator");
                    ConfigureClips(row,animator);
                    var probe=UnityEngine.Object.Instantiate(root);try{VerifyMotion(row,probe.GetComponentInChildren<Animator>());}finally{UnityEngine.Object.DestroyImmediate(probe);}
                    root.AddComponent<Lattice.Combat.GeneratedAnimator>();
                }
                if(row.form=="Shaped"&&row.kind=="biped")
                {
                    var vanes=body.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Vane_")).OrderBy(t=>t.name).ToArray();
                    if(vanes.Length!=4||vanes.Any(v=>v.GetComponent<MeshFilter>()==null))throw new InvalidOperationException("Shaped hero needs four separate generated vane meshes: "+row.id);
                    foreach(var vane in vanes)vane.SetParent(animator.GetBoneTransform(vane.name.Contains("Upper")?HumanBodyBones.Chest:HumanBodyBones.Hips),true);
                    var driver=root.AddComponent<Lattice.Combat.GeneratedVanes>();driver.vanes=vanes;driver.folded=vanes.Select(v=>v.localRotation).ToArray();
                    Debug.Log("VANE_PARTS_OK "+row.id+" count=4");
                }
                Directory.CreateDirectory(Path.GetDirectoryName(Prefab(row)));var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefab(row));
                if(!string.IsNullOrEmpty(row.character))
                {
                    var definition=GameCatalog.Find<CharacterDef>(row.character);if(definition==null)throw new InvalidOperationException("Unknown character "+row.character);
                    if(row.form=="Natural")definition.natural=prefab;else if(row.form=="Shaped"){definition.shaped=prefab;definition.flight=prefab;}else throw new InvalidOperationException("Unknown form");EditorUtility.SetDirty(definition);
                }
                if(!string.IsNullOrEmpty(row.enemy)){var enemy=GameCatalog.Find<EnemyDef>(row.enemy);if(enemy==null)throw new InvalidOperationException("Unknown enemy");if(row.form=="Body")enemy.bodySegment=prefab;else if(row.form=="Tail")enemy.tailSegment=prefab;else enemy.prefab=prefab;EditorUtility.SetDirty(enemy);}
                Debug.Log("MODEL_INTAKE_OK "+row.id);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static bool Human(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().Any(a=>a.isHuman);
        static void Map(ModelImporter importer,bool minimal)
        {
            var description=importer.humanDescription;var names=new HashSet<string>(description.skeleton.Select(b=>b.name));
            var map=MeshyMap.Where(m=>!minimal||m.human!="UpperChest"&&m.human!="LeftToes"&&m.human!="RightToes").ToArray();
            var missing=map.Where(m=>!names.Contains(m.bone)).Select(m=>m.bone).ToArray();if(missing.Length>0)throw new InvalidOperationException("Meshy skeleton changed: "+string.Join(",",missing));
            description.human=map.Select(m=>new HumanBone{boneName=m.bone,humanName=m.human,limit=new HumanLimit{useDefaultValues=true}}).ToArray();importer.humanDescription=description;
        }
        static AnimationClip Clip(ClipRow row)
        {
            var clips=AssetDatabase.LoadAllAssetsAtPath(row.path).OfType<AnimationClip>().Where(c=>c.name==row.name).ToArray();
            if(clips.Length!=1)throw new InvalidOperationException("Missing/ambiguous clip "+row.path+" / "+row.name);return clips[0];
        }
        static void ConfigureClips(Row row,Animator animator)
        {
            if(!row.clips.Any(c=>c.state=="Idle")||!row.clips.Any(c=>c.state=="Walk"))throw new InvalidOperationException("Idle and Walk clips are required");
            string path=Dir(row)+row.id+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);
            foreach(var clip in row.clips){var state=machine.AddState(clip.state);state.motion=Clip(clip);if(clip.state=="Idle")machine.defaultState=state;}
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;EditorUtility.SetDirty(controller);
        }
        static void VerifyMotion(Row row,Animator animator)
        {
            var clip=Clip(row.clips.Single(c=>c.state=="Walk"));var bones=animator.GetComponentsInChildren<Transform>();
            var graph=PlayableGraph.Create("Lattice motion gate");
            try
            {
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,clip);AnimationPlayableOutput.Create(graph,"Motion",animator).SetSourcePlayable(playable);graph.Play();
                graph.Evaluate(0);playable.SetTime(clip.length*.15);graph.Evaluate(0);var before=bones.Select(b=>b.position).ToArray();var rotations=bones.Select(b=>b.localRotation).ToArray();var skinBefore=SkinPoints(animator);
                playable.SetTime(clip.length*.65);graph.Evaluate(0);float travel=bones.Select((b,i)=>Vector3.Distance(before[i],b.position)).Sum()/row.size;
                if(travel<=.05f)throw new InvalidOperationException("Walk is frozen on "+row.id+" relative travel="+travel);
                float localTurn=bones.Select((b,i)=>Quaternion.Angle(rotations[i],b.localRotation)).Sum();
                if(localTurn<20)throw new InvalidOperationException("Root travels but limb rotations are frozen: "+row.id);
                var skinAfter=SkinPoints(animator);if(skinAfter.Length!=skinBefore.Length||skinAfter.Length==0)throw new InvalidOperationException("No measurable skinned geometry: "+row.id);
                float skinTravel=skinAfter.Select((p,i)=>Vector3.Distance(p,skinBefore[i])).Average()/row.size;
                if(skinTravel<=.0035f)throw new InvalidOperationException("Bones move but visible skin is frozen: "+row.id);
                Debug.Log($"ANIMATION_MOVES {row.id} relativeTravel={travel:0.000} skinTravel={skinTravel:0.000} localTurn={localTurn:0.0}");
            }
            finally{graph.Destroy();}
        }
        static Vector3[] SkinPoints(Animator animator)
        {
            return ModelGeometry.Points(animator.gameObject);
        }
    }
}
