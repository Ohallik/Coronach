using System;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    public static partial class HomeBuilder
    {
        /// <summary>The two places of D381 drawn from the cameras the November 22 capture had (frames.csv of nov22b-whole), with the live pipeline, Taren where the hero stood: the
        /// Meridian Crawl's helm (s55/138: the dawn mirror and the wheelhouse's lit windows) and the town meeting (s57/139 while Ilo reads Oda's name, s57/156 once Oda has stood,
        /// and the room turned to Orrin at the board). The meeting is posed as its talk leaves it (LineCues' own turn, a seat's turn at most), and every body in it is measured:
        /// which way its root faces, which way its hips and shoulders are drawn facing, and where its knees point (lines D381_FACE). Run:
        /// exec-when-green.ps1 -Method Lattice.EditorTools.HomeBuilder.PlayStills -Marker HOME_PLAY_STILLS_OK -Graphics -ExtraArgs @('-look-run','name').</summary>
        public static void PlayStills()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-look-run")??throw new ArgumentException("-look-run <name> is required");
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/look",run));
            if(System.IO.Directory.Exists(folder))throw new InvalidOperationException("Preserve earlier look run: "+folder);
            System.IO.Directory.CreateDirectory(folder);
            var before=GraphicsSettings.defaultRenderPipeline;var beforeQuality=QualitySettings.renderPipeline;
            var pipeline=Resources.Load<RenderPipelineAsset>("LocalBiomes/LocalPipeline")??throw new InvalidOperationException("HOME_PLAY_STILLS the live pipeline is missing");
            try
            {
                GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                var profile=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Ground.asset");
                // The Crawl's helm, s55/138.
                Open(CrawlZone);
                Draw(folder,CrawlZone+"-s55-138",profile,new Vector3(-76.74f,2.48f,.18f),new Vector3(-81.45f,15.0f,-13.88f),250);
                // The meeting.
                Open(DecksZone);
                var all=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                var room=all.First(t=>t.name==Decks.MeetingRoot);room.gameObject.SetActive(true);
                foreach(var w in UnityEngine.Object.FindObjectsByType<Whereabouts>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    if(w.awayFrom==Decks.Gathered)foreach(var t in w.targets)if(t!=null)t.SetActive(false);
                    if(w.name=="Orrin at his board (when)")foreach(var t in w.targets)if(t!=null)t.SetActive(true);
                }
                Transform Named(string speaker)=>room.GetComponentsInChildren<Npc>(true).Concat(UnityEngine.Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None)).First(n=>n.speaker==speaker).transform;
                var oda=Named("Oda");var orrin=Named("Orrin");var jun=Named("Jun");var mother=room.Find("Townsperson: Oda's mother");
                var groups=new[]{"The town at the meeting","Hal's apprentices at the meeting","Dace's crew at the meeting"}.Select(n=>room.Find(n)).Where(g=>g!=null).ToArray();
                var placed=groups.SelectMany(g=>g.Cast<Transform>()).ToDictionary(t=>t,t=>t.rotation);
                void Face(Transform to){foreach(var body in placed.Keys){body.rotation=placed[body];var look=to.position-body.position;look.y=0;if(look.sqrMagnitude<.01f)continue;var want=Quaternion.LookRotation(look);body.rotation=body.TryGetComponent<Posed>(out var p)?p.WithinSeat(want):want;}}
                void Stand(Transform who,string pose){var p=who!=null?who.GetComponent<Posed>():null;if(p!=null&&p.Has(pose)){p.current=pose;p.Hold();}}
                Measure(room,"as built",Decks.Board.stand);
                Face(oda);Measure(room,"names read: to Oda",new Vector2(oda.position.x,oda.position.z));
                Draw(folder,DecksZone+"-s57-139",profile,new Vector3(-28.59f,.08f,2.9f),new Vector3(-33.69f,12.61f,-10.33f),0);
                Stand(oda,"stand");Stand(jun,"stand");Stand(mother,"stand");
                Draw(folder,DecksZone+"-s57-156",profile,new Vector3(-28.6f,.08f,5.7f),new Vector3(-33.71f,12.61f,-9.15f),0);
                Face(orrin);Measure(room,"to Orrin",new Vector2(orrin.position.x,orrin.position.z));
                Draw(folder,DecksZone+"-s57-board",profile,new Vector3(-28.6f,.08f,5.7f),new Vector3(-33.71f,12.61f,-9.15f),0);
                Debug.Log($"HOME_PLAY_STILLS_OK folder={folder}");
            }
            finally{GraphicsSettings.defaultRenderPipeline=before;QualitySettings.renderPipeline=beforeQuality;}
        });
        /// <summary>Which way a body is drawn facing as the meeting's posing goes, step by step, for a body placed at several turns: lines D381_POSE.
        /// exec-when-green.ps1 -Method Lattice.EditorTools.HomeBuilder.PoseProbe -Marker HOME_POSE_PROBE_OK -Graphics.</summary>
        public static void PoseProbe()=>BatchTools.Run(()=>
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            static float Yaw(Vector3 v)=>Mathf.Atan2(v.x,v.z)*Mathf.Rad2Deg;
            static string Facing(Animator rig){Transform B(HumanBodyBones b)=>rig.GetBoneTransform(b);var k=(B(HumanBodyBones.LeftLowerLeg).position-B(HumanBodyBones.LeftUpperLeg).position);k.y=0;
                return $"hips={Yaw(Vector3.Cross(Vector3.up,B(HumanBodyBones.LeftUpperLeg).position-B(HumanBodyBones.RightUpperLeg).position)):0} knees={(k.sqrMagnitude>1e-4f?Yaw(k).ToString("0"):"-")} hipsRot={B(HumanBodyBones.Hips).rotation.eulerAngles} rootRot={rig.transform.rotation.eulerAngles}";}
            foreach(var body in new[]{"OdaBody","JunBody"})foreach(float yaw in new[]{0f,10f,90f})foreach(float seat in new[]{.45f,0f})
            {
                var person=new GameObject("Probe");person.transform.SetPositionAndRotation(new Vector3(yaw,0,seat*10),Quaternion.Euler(0,yaw,0));
                var model=(GameObject)PrefabUtility.InstantiatePrefab(ZoneKit.Prefab("Characters",body));model.transform.SetParent(person.transform,false);
                var rig=person.GetComponentInChildren<Animator>(true);string tag=$"{body} yaw={yaw} seat={seat}";
                Debug.Log($"D381_POSE {tag} placed {Facing(rig)}");
                var idle=rig.runtimeAnimatorController!=null?rig.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle")):null;
                if(idle!=null){idle.SampleAnimation(rig.gameObject,.3f);Debug.Log($"D381_POSE {tag} idle({idle.name},humanoid={idle.humanMotion}) {Facing(rig)}");}
                Decks.Pose(person,seat,"base","stand");Debug.Log($"D381_POSE {tag} posed {Facing(rig)}");
            }
            Debug.Log("HOME_POSE_PROBE_OK");
        });
        static void Open(string zone)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
            foreach(var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
            var def=Find<ZoneDef>(zone);if(def!=null)RenderSettings.skybox=def.skybox;
        }
        static void Draw(string folder,string name,CameraProfile profile,Vector3 hero,Vector3 eye,float heroYaw)
        {
            var camera=new GameObject("Still camera",typeof(Camera),typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.06f);camera.nearClipPlane=.3f;camera.farClipPlane=600;camera.fieldOfView=profile.fov;
            var data=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;
            var taren=(GameObject)PrefabUtility.InstantiatePrefab(ZoneKit.Prefab("Characters","TarenNatural"));
            var rig=taren.GetComponentInChildren<Animator>();var idle=rig!=null&&rig.runtimeAnimatorController!=null?rig.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle")):null;
            taren.transform.SetPositionAndRotation(hero,Quaternion.Euler(0,heroYaw,0));if(idle!=null)idle.SampleAnimation(rig.gameObject,.3f);
            camera.transform.SetPositionAndRotation(eye,Quaternion.Euler(profile.pitch,profile.yaw,0));
            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();
            var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name+".png"),image.EncodeToPNG());
            RenderTexture.active=prior;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(taren);
        }
        /// <summary>Every body in the meeting: its root's yaw, the yaw its hips and shoulders are drawn facing, where its knees point, the yaw to what it should face, its seat.</summary>
        static void Measure(Transform room,string moment,Vector2 toward)
        {
            static float Yaw(Vector3 v)=>Mathf.Atan2(v.x,v.z)*Mathf.Rad2Deg;
            foreach(var rig in room.GetComponentsInChildren<Animator>(true))
            {
                if(!rig.isHuman)continue;var body=rig.transform;while(body.parent!=null&&body.parent!=room&&body.parent.parent!=room)body=body.parent;
                Transform B(HumanBodyBones b)=>rig.GetBoneTransform(b);
                var hipsAcross=B(HumanBodyBones.LeftUpperLeg).position-B(HumanBodyBones.RightUpperLeg).position;var armsAcross=B(HumanBodyBones.LeftUpperArm).position-B(HumanBodyBones.RightUpperArm).position;
                var knees=(B(HumanBodyBones.LeftLowerLeg).position-B(HumanBodyBones.LeftUpperLeg).position)+(B(HumanBodyBones.RightLowerLeg).position-B(HumanBodyBones.RightUpperLeg).position);knees.y=0;
                var to=new Vector3(toward.x,0,toward.y)-body.position;var seat=body.GetComponentInChildren<SeatFit>(true);var posed=body.GetComponent<Posed>();
                Debug.Log($"D381_FACE [{moment}] {body.name} | {rig.transform.parent?.name}/{rig.name} at ({body.position.x:0.0},{body.position.z:0.0}) root={Mathf.DeltaAngle(0,body.eulerAngles.y):0} hips={Yaw(Vector3.Cross(Vector3.up,hipsAcross)):0} "
                    +$"chest={Yaw(Vector3.Cross(Vector3.up,armsAcross)):0} knees={(knees.sqrMagnitude>.0004f?Yaw(knees).ToString("0"):"-")} want={Yaw(to):0} pose={(posed!=null?posed.Showing:"-")} seat={(seat!=null&&seat.seat!=null?seat.seat.name:"-")}");
            }
        }
    }
}
