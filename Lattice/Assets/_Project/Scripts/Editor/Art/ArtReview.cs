using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Lattice.EditorTools
{
    /// <summary>Actual Unity prefab evidence: evaluated poses, both framings, two renders.</summary>
    public static class ArtReview
    {
        static readonly string Root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        static readonly string Out=Path.Combine(Root,"Builds/logs/renders");
        static Camera camera;
        static readonly Dictionary<Animator,PlayableGraph> graphs=new();
        static IEnumerator routine;
        static double nextTick;
        static GenIntake.Row[] Rows()
        {
            var rows=JsonConvert.DeserializeObject<GenIntake.Row[]>(File.ReadAllText(Path.Combine(Root,"docs/art/intake.json")));
            string prefix=Lattice.Core.DevArgs.Value("-intake-prefix");
            return string.IsNullOrEmpty(prefix)?rows:rows.Where(r=>r.id.StartsWith(prefix,StringComparison.Ordinal)).ToArray();
        }
        public static void Render(){routine=ReviewRoutine();nextTick=0;EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<nextTick)return;nextTick=EditorApplication.timeSinceStartup+.06;
            try{EditorApplication.QueuePlayerLoopUpdate();if(routine.MoveNext())return;EditorApplication.update-=Tick;Debug.Log("ART_RENDERS_OK");}
            catch(Exception e){EditorApplication.update-=Tick;Debug.LogError("FAILED: ART_RENDERS "+e);EditorApplication.Exit(1);}
        }
        static IEnumerator ReviewRoutine()
        {
            var rows=Rows();if(rows==null||rows.Length==0)throw new InvalidOperationException("No models to render");
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("Graphics must be enabled");
            Directory.CreateDirectory(Out);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.62f,.65f,.72f);RenderSettings.fog=false;
            var light=new GameObject("ReviewLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;light.transform.rotation=Quaternion.Euler(35,-30,0);light.shadows=LightShadows.None;
            camera=new GameObject("ReviewCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.21f,.27f);camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=500;
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
            var reports=new List<object>();var lineup=new List<GameObject>();float cursor=0;
            foreach(var row in rows)
            {
                string path="Assets/_Project/Prefabs/"+row.folder+"/"+row.id+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab==null)throw new InvalidOperationException("Missing prefab "+path);
                var actor=UnityEngine.Object.Instantiate(prefab);actor.name=row.id;Pose(actor,row,"Idle",.2f);
                var bounds=BoundsOf(actor);if(bounds.size.sqrMagnitude<.01f)throw new InvalidOperationException("Empty rendered bounds "+row.id);
                var animator=actor.GetComponentInChildren<Animator>();
                object hands=null;if(animator!=null&&animator.isHuman)
                    hands=new{left=Point(animator.GetBoneTransform(HumanBodyBones.LeftHand).position),right=Point(animator.GetBoneTransform(HumanBodyBones.RightHand).position),hips=Point(animator.GetBoneTransform(HumanBodyBones.Hips).position)};
                reports.Add(new{id=row.id,renderers=actor.GetComponentsInChildren<Renderer>().Length,size=new[]{bounds.size.x,bounds.size.y,bounds.size.z},humanoid=animator!=null&&animator.isHuman,hands});
                yield return null;
                Frame(bounds,new Vector3(0,.1f,1),960,1080);Capture(row.id+"-idle-front",960,1080);
                Frame(bounds,new Vector3(1,.15f,0),960,1080);Capture(row.id+"-idle-side",960,1080);
                Frame(bounds,new Vector3(0,.12f,-1),960,1080);Capture(row.id+"-idle-back",960,1080);
                Frame(bounds,new Vector3(.7f,.55f,1),960,1080);Capture(row.id+"-idle-3quarter",960,1080);
                if(row.clips!=null&&row.clips.Any(c=>c.state=="Walk"))
                {
                    foreach(float phase in new[]{.15f,.65f}){Pose(actor,row,"Walk",phase);yield return null;Frame(BoundsOf(actor),new Vector3(.65f,.25f,1),960,1080);Capture(row.id+"-walk-"+Mathf.RoundToInt(phase*100),960,1080);}
                    Pose(actor,row,"Attack",.45f);yield return null;Frame(BoundsOf(actor),new Vector3(.65f,.25f,1),960,1080);Capture(row.id+"-attack",960,1080);
                }
                if(row.form=="Shaped")
                {
                    Pose(actor,row,"Idle",.2f);actor.transform.rotation=Quaternion.Euler(90,0,0);actor.GetComponent<Lattice.Combat.GeneratedVanes>().SetForm(Lattice.Data.BodyForm.Flight);
                    yield return null;Frame(BoundsOf(actor),new Vector3(.7f,1,.5f),1280,960);Capture(row.character+"-Flight",1280,960);
                    Frame(BoundsOf(actor),new Vector3(.01f,1,.01f),1280,960);Capture(row.character+"-Flight-top",1280,960);
                    actor.transform.rotation=Quaternion.identity;actor.GetComponent<Lattice.Combat.GeneratedVanes>().SetForm(Lattice.Data.BodyForm.Shaped);
                }
                Pose(actor,row,"Idle",.2f);bounds=BoundsOf(actor);actor.transform.position+=new Vector3(cursor-bounds.min.x,-bounds.min.y,0);cursor+=bounds.size.x+.6f;actor.SetActive(false);lineup.Add(actor);
            }
            foreach(var actor in lineup)actor.SetActive(true);
            yield return null;
            var rowBounds=BoundsOf(lineup[0]);foreach(var actor in lineup.Skip(1))rowBounds.Encapsulate(BoundsOf(actor));
            Frame(rowBounds,new Vector3(0,.08f,1),Mathf.Max(1920,rows.Length*180),1080);Capture("true-scale-row",Mathf.Max(1920,rows.Length*180),1080);
            foreach(var actor in lineup)actor.SetActive(false);
            var cantor=Lattice.Core.GameCatalog.Find<Lattice.Data.EnemyDef>("Cantor");
            if(string.IsNullOrEmpty(Lattice.Core.DevArgs.Value("-intake-prefix"))&&cantor!=null&&cantor.prefab!=null&&cantor.bodySegment!=null&&cantor.tailSegment!=null)
            {
                var root=new GameObject("Cantor chain review");var head=UnityEngine.Object.Instantiate(cantor.prefab,root.transform);Lattice.Combat.SerpentSegments.CenterVisual(head,Lattice.Combat.SerpentSegments.CenterHeight);
                root.AddComponent<Lattice.Combat.SerpentSegments>().Initialize(cantor,null);yield return null;
                Frame(BoundsOf(root),new Vector3(1,.65f,.35f),2048,1024);Capture("Cantor-chain",2048,1024);UnityEngine.Object.DestroyImmediate(root);
            }
            File.WriteAllText(Path.Combine(Out,"model-review.json"),JsonConvert.SerializeObject(reports,Formatting.Indented,new JsonSerializerSettings{ReferenceLoopHandling=ReferenceLoopHandling.Ignore}));
            foreach(var graph in graphs.Values)if(graph.IsValid())graph.Destroy();graphs.Clear();
            Debug.Log("ART_RENDERS_COUNT "+rows.Length);
        }
        static void Pose(GameObject actor,GenIntake.Row row,string state,float phase)
        {
            var spec=row.clips?.FirstOrDefault(c=>c.state==state);var animator=actor.GetComponentInChildren<Animator>();if(spec==null||animator==null)return;
            var clip=AssetDatabase.LoadAllAssetsAtPath(spec.path).OfType<AnimationClip>().Single(c=>c.name==spec.name);
            if(graphs.TryGetValue(animator,out var previous)&&previous.IsValid())previous.Destroy();
            animator.runtimeAnimatorController=null;
            foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
            var graph=PlayableGraph.Create("Review pose");graphs[animator]=graph;
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var play=AnimationClipPlayable.Create(graph,clip);AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(play);graph.Play();graph.Evaluate(0);play.SetTime(clip.length*phase);graph.Evaluate(0);
        }
        static float[] Point(Vector3 p)=>new[]{p.x,p.y,p.z};
        static Bounds BoundsOf(GameObject actor)
        {
            return ModelGeometry.BoundsOf(actor);
        }
        static void Frame(Bounds bounds,Vector3 direction,int width,int height)
        {
            float distance=bounds.size.magnitude*2+5;camera.farClipPlane=Mathf.Max(500,distance+bounds.size.magnitude*2+10);
            camera.transform.position=bounds.center+direction.normalized*distance;camera.transform.LookAt(bounds.center);
            float x=0,y=0;for(int i=0;i<8;i++)
            {var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var v=camera.transform.InverseTransformPoint(p);x=Mathf.Max(x,Mathf.Abs(v.x));y=Mathf.Max(y,Mathf.Abs(v.y));}
            camera.aspect=(float)width/height;camera.orthographicSize=Mathf.Max(y,x/camera.aspect)*1.13f;
        }
        static void Capture(string name,int width,int height)
        {
            var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
            var png=new Texture2D(width,height,TextureFormat.RGB24,false);try
            {
                camera.Render();camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();RenderTexture.active=previous;
                var pixels=png.GetRawTextureData<byte>();int lowR=255,lowG=255,highR=0,highG=0;
                for(int y=0;y<height;y+=Mathf.Max(1,height/128))for(int x=0;x<width;x+=Mathf.Max(1,width/512))
                {int at=(y*width+x)*3;lowR=Mathf.Min(lowR,pixels[at]);highR=Mathf.Max(highR,pixels[at]);lowG=Mathf.Min(lowG,pixels[at+1]);highG=Mathf.Max(highG,pixels[at+1]);}
                if(Mathf.Max(highR-lowR,highG-lowG)<18)throw new InvalidOperationException("Uniform/empty rendered image "+name);
                var bytes=png.EncodeToPNG();if(bytes.Length<30000)throw new InvalidOperationException("Blank/suspicious capture "+name);File.WriteAllBytes(Path.Combine(Out,name+".png"),bytes);Debug.Log("ART_RENDER "+name+" bytes="+bytes.Length);
            }finally{camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(png);}
        }
    }
}
