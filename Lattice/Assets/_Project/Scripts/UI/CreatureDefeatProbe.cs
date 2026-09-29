#if LATTICE_DEV || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Unity.Cinemachine;
using UnityEngine;

namespace Lattice.UI
{
    // Isolated graphics diagnostic: creates specimens and delivers lethal
    // packets directly. This is never ordinary-input combat/route evidence.
    public sealed class CreatureDefeatProbe:MonoBehaviour
    {
        [Serializable] sealed class Snapshot
        {public string body,image;public float slope,seconds,minimum;public bool focus;public int liveColliders;}
        [Serializable] sealed class Report
        {public string method="Direct lethal diagnostic, not ordinary-input play";public string captureFailure;public Snapshot[] snapshots;}
        readonly List<Snapshot> snapshots=new();
        Camera camera;string folder;QualityCapture capture;
        static List<Vector3> Points(Transform root)
        {
            var points=new List<Vector3>();
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())points.AddRange(GeneratedGeometry.WorldSkinPoints(skin));
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                if(filter.sharedMesh!=null)foreach(var v in filter.sharedMesh.vertices)points.Add(filter.transform.TransformPoint(v));
            return points;
        }
        void Capture(EnemyBrain enemy,string name,float slope,float elapsed,Vector3 plane,Vector3 normal)
        {
            float minimum=float.PositiveInfinity;foreach(var p in Points(enemy.transform))minimum=Mathf.Min(minimum,Vector3.Dot(p-plane,normal));
            int live=0;foreach(var c in enemy.GetComponentsInChildren<Collider>())if(c.enabled)live++;
            string file=name+".png";ScreenCapture.CaptureScreenshot(Path.Combine(folder,file));
            snapshots.Add(new Snapshot{body=enemy.definition.id,image=file,slope=slope,seconds=elapsed,minimum=minimum,focus=Application.isFocused,liveColliders=live});
        }
        IEnumerator Start()
        {
            if(string.IsNullOrWhiteSpace(DevArgs.Value("-savepath")))throw new InvalidOperationException("Creature audit requires isolated saves");
            folder=Path.GetFullPath(DevArgs.Value("-creature-defeat"));
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"../../quality"))+Path.DirectorySeparatorChar;
            if(!folder.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Creature audit output must be inside Builds/quality");
            // Packaged player data lives beneath Builds/WindowsDev. The launcher
            // supplies the exact workspace evidence directory, never a save slot.
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve earlier creature audit output");
            Directory.CreateDirectory(folder);
            while(PartyController.Current==null||SceneFlow.Current.Loading)yield return null;
            yield return new WaitForSecondsRealtime(1);
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            foreach(var enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var rig in FindObjectsByType<CameraRig>(FindObjectsSortMode.None))rig.enabled=false;
            camera=Camera.main;var brain=camera.GetComponent<CinemachineBrain>();if(brain!=null)brain.enabled=false;
            camera.orthographic=true;camera.cullingMask=1<<31;camera.farClipPlane=70;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.1f,.13f);
            GameServices.Current.Input.Blocked=true;
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
            Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
            if(DevArgs.Has("-quality-ffmpeg")){capture=camera.gameObject.AddComponent<QualityCapture>();capture.Begin(folder,DevArgs.Value("-quality-ffmpeg"));}
            foreach(string id in new[]{"Ridgehound","Scrapmite","Burrower","ChoristerDart","ChoristerDrifter","Shellmine","Cantor"})
            {
                bool ground=id=="Ridgehound"||id=="Scrapmite"||id=="Burrower";
                foreach(float slope in ground?new[]{0f,10f,-10f}:new[]{0f})
                {
                    var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Diagnostic support plane";floor.layer=31;
                    floor.transform.SetPositionAndRotation(new Vector3(150,ground?-.5f:-6.5f,0),Quaternion.Euler(0,0,slope));floor.transform.localScale=new Vector3(40,1,40);
                    var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.22f,.25f,.28f));floor.GetComponent<Renderer>().sharedMaterial=material;
                    var plane=floor.transform.TransformPoint(Vector3.up*.5f);var normal=floor.transform.up;Physics.SyncTransforms();
                    var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),ground?plane:new Vector3(150,1,0));enemy.Passive=true;
                    enemy.transform.rotation=Quaternion.Euler(0,slope==0?0:135,0);
                    yield return new WaitForSecondsRealtime(.25f);enemy.enabled=false;
                    foreach(var renderer in enemy.GetComponentsInChildren<Renderer>(true))renderer.gameObject.layer=31;
                    var points=Points(enemy.transform);var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
                    camera.orthographicSize=Mathf.Max(1.8f,bounds.size.magnitude*.55f);
                    var centre=bounds.center;camera.transform.position=centre+new Vector3(3,2.3f,4).normalized*30;camera.transform.LookAt(centre);
                    string label=id+"-"+slope.ToString("F0",System.Globalization.CultureInfo.InvariantCulture);
                    yield return new WaitForSecondsRealtime(.2f);Capture(enemy,label+"-standing",slope,-1,plane,normal);
                    yield return new WaitForEndOfFrame();yield return null;
                    enemy.Health.Receive(new DamagePacket{amount=enemy.Health.maximum*100,type=DamageType.Pulse});float started=GameTime.Now;
                    bool flier=id.StartsWith("Chorister");
                    float duration=enemy.definition.boss?1.5f:id=="Ridgehound"?.9f:flier?1.1f:.75f;
                    float hold=enemy.definition.boss?1.65f:flier?.6f:1.15f;
                    foreach(float phase in new[]{.25f,.5f,.75f,1f})
                    {
                        while(GameTime.Now-started<duration*phase)yield return null;
                        yield return null;Capture(enemy,label+"-"+Mathf.RoundToInt(phase*100),slope,GameTime.Now-started,plane,normal);
                        yield return new WaitForEndOfFrame();yield return null;
                    }
                    GameTime.Paused=true;camera.transform.position=centre+Quaternion.FromToRotation(Vector3.up,normal)*new Vector3(30,.5f,0);camera.transform.LookAt(centre);
                    yield return new WaitForSecondsRealtime(.3f);Capture(enemy,label+"-side-held",slope,GameTime.Now-started,plane,normal);
                    yield return new WaitForSecondsRealtime(.2f);GameTime.Paused=false;
                    // Deliberate cleanup, viewed from the same three-quarter camera.
                    camera.transform.position=centre+new Vector3(3,2.3f,4).normalized*30;camera.transform.LookAt(centre);
                    foreach(float part in new[]{.35f,.75f})
                    {
                        while(enemy!=null&&GameTime.Now-started<duration+hold+DefeatPresentation.Cleanup*part)yield return null;
                        if(enemy==null)break;
                        yield return null;Capture(enemy,label+"-cleanup"+Mathf.RoundToInt(part*100),slope,GameTime.Now-started,plane,normal);
                        yield return new WaitForEndOfFrame();yield return null;
                    }
                    if(enemy!=null)Destroy(enemy.gameObject);Destroy(floor);Destroy(material);yield return null;
                }
            }
            yield return new WaitForSecondsRealtime(.25f);
            if(capture!=null)yield return capture.Finish();
            var report=new Report{captureFailure=capture!=null?capture.Failure:null,snapshots=snapshots.ToArray()};
            File.WriteAllText(Path.Combine(folder,"report.json"),JsonUtility.ToJson(report,true));
            bool valid=string.IsNullOrEmpty(report.captureFailure);Debug.Log(valid?"CREATURE_DEFEAT_CAPTURE_OK":"CREATURE_DEFEAT_CAPTURE_REJECTED "+report.captureFailure);Application.Quit(valid?0:1);
        }
    }
}

#endif
