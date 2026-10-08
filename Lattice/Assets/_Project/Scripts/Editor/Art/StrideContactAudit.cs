using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    public static class StrideContactAudit
    {
        sealed class Motion : IMotor
        {
            public Vector3 velocity;
            public Vector3 Facing=>Vector3.forward;
            public Vector3 Velocity=>velocity;
            public void Move(Vector2 input,bool boost,bool brake){}
            public void Dash(Vector3 direction,float distance){}
        }
        public static void Capture()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-stride-run")??"stride-contact";
            if(run.IndexOfAny(new[]{'/','\\',':'})>=0)throw new InvalidOperationException("Use one evidence folder name");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C2",run));
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve previous contact evidence");
            Directory.CreateDirectory(folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            float slope=float.Parse(DevArgs.Value("-stride-slope")??"0",System.Globalization.CultureInfo.InvariantCulture);
            float rootClearance=float.Parse(DevArgs.Value("-stride-root-clearance")??"0",System.Globalization.CultureInfo.InvariantCulture);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(300,1,300);
            floor.transform.rotation=Quaternion.Euler(-slope,0,0);
            Vector3 normal=floor.transform.up,surface=floor.transform.position+normal*.5f;
            if(DevArgs.Has("-stride-shots"))
            {
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.6f,.65f);
                var light=new GameObject("Contact audit light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(40,-30,0);
                var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.16f,.19f,.23f);floor.GetComponent<Renderer>().sharedMaterial=material;
                var grid=new Texture2D(2,2){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
                grid.SetPixels(new[]{Color.white,new Color(.65f,.65f,.65f),new Color(.65f,.65f,.65f),Color.white});grid.Apply();
                material.mainTexture=grid;material.mainTextureScale=new Vector2(150,150);
            }
            Physics.SyncTransforms();GameTime.Reset();
            var failures=new List<string>();
            using(var summary=new StreamWriter(Path.Combine(folder,"contacts.csv")))
            {
                summary.WriteLine("hero,form,speed,direction,clip,cadence,stride,leftContacts,rightContacts,worstDrift,worstPenetration,contactLift,reachCorrection,shinSeparation,slope,rootClearance,torsoBehindRearSole");
                foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})
                foreach(float speed in shaped?new[]{1.2f,6.7f,10.385f}:new[]{1.2f,2.6f,5.4f})
                {
                    foreach(float direction in DevArgs.Has("-stride-directions")?new[]{0f,45,90,135,180,225,270,315}:new[]{0f})
                    {
                        string key=hero+(shaped?"Shaped":"Natural")+"-"+speed.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"-"+direction;
                        string filter=DevArgs.Value("-stride-case");
                        if(filter!=null&&!filter.Split(',').Contains(key))continue;
                        var result=Measure(hero,shaped,speed,direction,Path.Combine(folder,key+".csv"),normal,surface,rootClearance);
                        summary.WriteLine(FormattableString.Invariant($"{hero},{(shaped?"Shaped":"Natural")},{speed},{direction},{result.clip},{result.cadence},{result.stride},{result.left},{result.right},{result.drift},{result.penetration},{result.lift},{result.reach},{result.shin},{slope},{rootClearance},{result.torsoBehind}"));
                        if(result.left<2||result.right<2||result.drift>.05f||result.penetration>.03f||result.lift>.04f||result.reach>.08f||result.shin<.07f)
                            failures.Add($"{key}: contacts={result.left}/{result.right} drift={result.drift:F4} penetration={result.penetration:F4} lift={result.lift:F4} reach={result.reach:F4} shin={result.shin:F4}");
                    }
                }
            }
            File.WriteAllLines(Path.Combine(folder,"failures.txt"),failures);
            Object.DestroyImmediate(floor);
            if(failures.Count>0)throw new InvalidOperationException("Stride contact rejected: "+string.Join("; ",failures));
            Debug.Log("STRIDE_CONTACT_OK "+folder);
        });
        struct Result { public string clip;public float cadence,stride,drift,penetration,lift,reach,shin,torsoBehind;public int left,right; }
        static Result Measure(string hero,bool shaped,float speed,float direction,string path,Vector3 normal,Vector3 surface,float rootClearance)
        {
            var actorObject=new GameObject("independent contact travel",typeof(Health),typeof(CombatActor));
            var actor=actorObject.GetComponent<CombatActor>();actor.character=hero;
            Vector3 travel=Quaternion.Euler(0,direction,0)*Vector3.forward;
            bool reverse=DevArgs.Has("-stride-reverse")&&Vector3.Dot(travel,Vector3.forward)<-.15f;
            actor.motor=new Motion{velocity=travel*speed};
            var definition=GameCatalog.Find<CharacterDef>(hero);
            var body=Object.Instantiate(shaped?definition.shaped:definition.natural,actorObject.transform);
            var animator=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
            var left=new SoleMarkers(animator,true);var right=new SoleMarkers(animator,false);
            // SendMessage refuses ordinary behaviours in edit mode. Invoke the
            // real driver directly; the manual playable below supplies its pose.
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            if(actor.Health==null)typeof(CombatActor).GetMethod("Awake",flags).Invoke(actor,null);
            typeof(GeneratedAnimator).GetMethod("Start",flags).Invoke(driver,null);
            typeof(GeneratedAnimator).GetMethod("Update",flags).Invoke(driver,null);
            string state=driver.CurrentAnimation;float rate=animator.speed;
            var clip=animator.runtimeAnimatorController.animationClips.Single(c=>c.name==state);
            var feet=body.GetComponent<GroundFeet>();
            float stride=driver.StrideScale;
            if(DevArgs.Has("-stride-correction"))
            {
                var calibration=Resources.Load<GroundStrideProfile>("Motion/"+hero+(shaped?"Shaped":"Natural"));
                if(calibration==null)throw new InvalidOperationException("Create the visible-sole calibration before the correction experiment");
                float grade=Mathf.Abs(Vector3.Dot(normal,travel))/Mathf.Max(.2f,normal.y);
                rate=calibration.Cadence(state,speed,grade);stride=calibration.Stride(state,speed,rate,grade);
                feet=feet??body.AddComponent<GroundFeet>();feet.Initialize(animator,actorObject.transform,calibration);
            }
            var graph=PlayableGraph.Create("independent visible sole contact");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(true);
            AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(playable);graph.Play();
            var result=new Result{clip=state,cadence=rate,stride=stride,shin=float.PositiveInfinity};
            var contacts=new[]{new List<Vector3>(),new List<Vector3>()};
            bool leftShot=false,rightShot=false;
            const float dt=1f/120;
            float duration=clip.length/rate*4;
            using var writer=new StreamWriter(path);writer.WriteLine("seconds,phase,leftX,leftY,leftZ,rightX,rightY,rightZ,leftContact,rightContact,shinSeparation,reachCorrection,supportDrop,hipAlong,chestAlong,rearSoleAlong,torsoBehindRearSole");
            try
            {
                for(int frame=0;frame<Mathf.CeilToInt(duration/dt);frame++)
                {
                    float seconds=frame*dt,normalized=seconds*rate/clip.length,poseNormalized=reverse?-normalized:normalized,phase=Mathf.Repeat(poseNormalized,1);
                    actorObject.transform.position=travel*speed*seconds;
                    var position=actorObject.transform.position;position.y=Vector3.Dot(normal,surface-position)/normal.y+rootClearance;actorObject.transform.position=position;
                    playable.SetTime(phase*clip.length);graph.Evaluate(0);
                    if(feet!=null)feet.Correct(travel*speed,state,poseNormalized,stride,false,reverse:reverse,deltaTime:dt);
                    if(DevArgs.Has("-stride-shots")&&normalized>=1&&normalized<2)
                    {
                        float leftPhase=state=="Walk"?.28f:.12f,rightPhase=leftPhase+(state=="Sprint"?.55f:.5f);
                        if(!leftShot&&Mathf.Repeat(phase-leftPhase,1)<.035f){Shot(actorObject.transform.position,path.Replace(".csv","-left.png"));leftShot=true;}
                        if(!rightShot&&Mathf.Repeat(phase-rightPhase,1)<.035f){Shot(actorObject.transform.position,path.Replace(".csv","-right.png"));rightShot=true;}
                    }
                    Vector3 l=left.Toe,r=right.Toe;
                    bool lc=Contact(state,phase),rc=Contact(state,Mathf.Repeat(phase-(state=="Sprint"?.55f:.5f),1));
                    // Posture proxy only, not physical COM or visual acceptance.
                    // Project vertically along planar travel, not the slope normal.
                    float hipAlong=Vector3.Dot(animator.GetBoneTransform(HumanBodyBones.Hips).position,travel);
                    float chestAlong=Vector3.Dot(animator.GetBoneTransform(HumanBodyBones.Chest).position,travel);
                    float rearSole=Mathf.Min(Vector3.Dot(l,travel),Vector3.Dot(r,travel),
                        Vector3.Dot(left.Heel,travel),
                        Vector3.Dot(right.Heel,travel));
                    float behind=rearSole-(hipAlong+chestAlong)*.5f;
                    if(normalized>=1&&(lc||rc))result.torsoBehind=Mathf.Max(result.torsoBehind,behind);
                    float shin=Segments(
                        animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position,animator.GetBoneTransform(HumanBodyBones.LeftFoot).position,
                        animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position,animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
                    writer.WriteLine(FormattableString.Invariant($"{seconds:F6},{phase:F6},{l.x:F6},{l.y:F6},{l.z:F6},{r.x:F6},{r.y:F6},{r.z:F6},{lc},{rc},{shin:F6},{feet?.MaximumReachCorrection??0:F6},{feet?.RequestedSupportDrop??0:F6},{hipAlong:F6},{chestAlong:F6},{rearSole:F6},{behind:F6}"));
                    for(int side=0;side<2;side++)
                    {
                        bool planted=side==0?lc:rc;var point=side==0?l:r;var points=contacts[side];
                        if(planted&&normalized>=1){points.Add(point);float clearance=Vector3.Dot(normal,point-surface);result.penetration=Mathf.Max(result.penetration,-clearance);result.lift=Mathf.Max(result.lift,clearance);}
                        else if(points.Count>0)
                        {
                            foreach(var a in points)foreach(var b in points)
                                result.drift=Mathf.Max(result.drift,Vector3.Distance(a,b));
                            if(side==0)result.left++;else result.right++;points.Clear();
                        }
                    }
                    if(feet!=null)result.reach=Mathf.Max(result.reach,feet.MaximumReachCorrection);
                    if(normalized>=1)result.shin=Mathf.Min(result.shin,shin);
                }
                return result;
            }
            finally{graph.Destroy();Object.DestroyImmediate(actorObject);}
        }
        // Central, visually flat forefoot intervals from the preserved sole
        // trajectory audit. Landing and rolling toe-off are not planted feet.
        static bool Contact(string state,float phase)=>state=="Walk"?phase>=.2f&&phase<=.4f:
            state=="Run"?phase>=.08f&&phase<=.16f:phase>=.095f&&phase<=.16f;
        static void Shot(Vector3 position,string path)
        {
            var camera=new GameObject("Contact audit camera",typeof(Camera)).GetComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=1.25f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.11f,.16f);
            float slope=float.Parse(DevArgs.Value("-stride-slope")??"0",System.Globalization.CultureInfo.InvariantCulture);
            camera.transform.position=position+(DevArgs.Has("-stride-side")?new Vector3(4,1.4f,0):new Vector3(2.8f,1.6f+Mathf.Max(0,Mathf.Tan(slope*Mathf.Deg2Rad)*4.5f),4.5f));camera.transform.LookAt(position+Vector3.up*.92f);
            FacingAudit.Capture(camera,path);Object.DestroyImmediate(camera.gameObject);
        }
        static float Segments(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            Vector3 u=b-a,v=d-c,r=a-c;float uu=Vector3.Dot(u,u),vv=Vector3.Dot(v,v),uv=Vector3.Dot(u,v),ur=Vector3.Dot(u,r),vr=Vector3.Dot(v,r);
            float denominator=uu*vv-uv*uv;
            float s=denominator>.000001f?Mathf.Clamp01((uv*vr-ur*vv)/denominator):0;
            float t=(uv*s+vr)/vv;
            if(t<0){t=0;s=Mathf.Clamp01(-ur/uu);}else if(t>1){t=1;s=Mathf.Clamp01((uv-ur)/uu);}
            return Vector3.Distance(a+u*s,c+v*t);
        }
    }
}
