using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Uphill step-height audit (November 4, docs/validation/C2-uphill-fold.md). Walks each hero and form up 30 and 39
    /// degree floors at the recorded 1.47 m/s through the real GroundFeet and reports the stepping knee, its height under
    /// the hip, the hip height against flat ground and the hip bob, against bounds that reject the recorded fold (Sela,
    /// 60.88 degrees with the knee at hip height, hips at 60%, 19 cm bob). Diagnostic only, not a test.
    /// Run: exec.ps1 -Method Lattice.EditorTools.UphillStepAudit.Run -Marker UPHILL_STEP_AUDIT
    /// Options (November 12): -uphill-slopes 12,20,30,39 walks the slopes the game has (GroundSlopeSurvey measures them);
    /// -uphill-heroes Taren,Sela,Idra (the default is every hero whose ground bodies carry a stride profile);
    /// -uphill-trace name writes each case's evaluated joints frame by frame to Builds/quality/C2/step-height/name;
    /// -uphill-stills name (with -Graphics) draws eight side views of each step there, ground line included;
    /// -uphill-speed and -uphill-rate trace another walking pace at another sample rate (the bounds stay the recorded fold's);
    /// -uphill-run puts the run and sprint through the same slopes instead (UPHILL_RUN lines, each beside its own flat run).
    /// Passes since UprightClimb (D259, November 12): 36 of 36 at 15 to 39 degrees for all three heroes.
    /// </summary>
    public static class UphillStepAudit
    {
        public static void Run()=>BatchTools.Run(()=>
        {
            int open=0;
            float[] slopes=(DevArgs.Value("-uphill-slopes")??"30,39").Split(',').Select(s=>float.Parse(s,CultureInfo.InvariantCulture)).ToArray();
            string trace=DevArgs.Value("-uphill-trace");
            if(trace!=null&&trace.IndexOfAny(new[]{'/','\\',':'})>=0)throw new System.InvalidOperationException("Use one trace folder name");
            string folder=trace==null?null:Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C2/step-height",trace));
            if(folder!=null)Directory.CreateDirectory(folder);
            string stills=DevArgs.Value("-uphill-stills");
            if(stills!=null&&stills.IndexOfAny(new[]{'/','\\',':'})>=0)throw new System.InvalidOperationException("Use one stills folder name");
            stillsFolder=stills==null?null:Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C2/step-height",stills));
            if(stillsFolder!=null){Directory.CreateDirectory(stillsFolder);Stage();}
            // -uphill-run: the gaits a player runs in most, at the paces the gait checks use: the civil jog and civil sprint on the natural
            // body (both play the Run take), the combat run and sprint on the shaped one. Reported beside the same body's flat run.
            bool running=DevArgs.Has("-uphill-run");
            float walk=float.Parse(DevArgs.Value("-uphill-speed")??"1.47",CultureInfo.InvariantCulture);
            foreach(string hero in Heroes())foreach(bool shaped in new[]{false,true})
            foreach(float speed in running?(shaped?new[]{6.7f,10.385f}:new[]{GroundMotor.NaturalJog,GroundMotor.NaturalSprint}):new[]{walk})
            foreach(float slope in slopes)
                Measure(hero,shaped,slope,speed,ref open,folder);
            if(running)Debug.Log("UPHILL_RUN_AUDIT_DONE");
            Debug.Log(open==0?"UPHILL_STEP_AUDIT PASS":"UPHILL_STEP_AUDIT OPEN cases="+open);
        });
        /// <summary>The heroes named, or each hero whose ground bodies both carry a measured stride profile.</summary>
        internal static string[] Heroes()
        {
            string named=DevArgs.Value("-uphill-heroes");
            if(!string.IsNullOrEmpty(named))return named.Split(',');
            return new[]{"Taren","Sela","Idra"}.Where(h=>
            {
                var d=GameCatalog.Find<CharacterDef>(h);
                return d!=null&&d.natural!=null&&d.shaped!=null&&d.natural.GetComponent<GeneratedAnimator>()?.strideProfile!=null&&d.shaped.GetComponent<GeneratedAnimator>()?.strideProfile!=null;
            }).ToArray();
        }
        /// <summary>One body, gait and pace, on the flat and then up the slope. A walk is held to the fold's bounds; a run (whose swing knee folds
        /// deep even on the flat) is reported beside its own flat run: the lowest hip, the bob, the swing knee and how far it rises toward
        /// the hip, the torso's forward pitch (a dive into the slope), and how high the recovering foot rides over the ground early in its swing.</summary>
        static void Measure(string hero,bool shaped,float slope,float speed,ref int open,string folder)
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(100,1,100);
            if(stillsFolder!=null)
            {
                // Seen from the side the floor is edge-on: a narrow drawn strip of it, with no collider, shows the ground line under the feet.
                floor.GetComponent<Renderer>().enabled=false;
                var strip=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(strip.GetComponent<Collider>());
                strip.transform.SetParent(floor.transform,false);strip.transform.localScale=new Vector3(.012f,1,1);strip.GetComponent<Renderer>().sharedMaterial=stillFloor;
            }
            var root=new GameObject("uphill step height regression");
            var definition=GameCatalog.Find<CharacterDef>(hero);
            var body=Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
            var rig=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
            var profile=driver.strideProfile;Object.DestroyImmediate(driver);
            string state=GeneratedAnimator.Gait(speed);bool walking=state=="Walk";
            var clip=rig.runtimeAnimatorController.animationClips.Single(c=>c.name==state);rig.runtimeAnimatorController=null;
            var feet=body.AddComponent<GroundFeet>();feet.Initialize(rig,root.transform,profile);
            var graph=PlayableGraph.Create("uphill step height");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var pose=AnimationClipPlayable.Create(graph,clip);pose.SetApplyFootIK(true);
            AnimationPlayableOutput.Create(graph,"pose",rig).SetSourcePlayable(pose);graph.Play();
            // -uphill-rate samples at another rate (the leg-continuity checks sample at 240 Hz).
            float dt=1f/float.Parse(DevArgs.Value("-uphill-rate")??"120",CultureInfo.InvariantCulture);
            // Each leg plants from 0 in its own cycle (the right half a stride later, a sprint's .55); a walk's stance is the first half,
            // a run's the first third (GroundFeet's run hold ends by .32 on the steepest ground).
            float other=state=="Sprint"?.55f:.5f,stanceEnd=walking?.5f:.35f;
            // The root rides where the hero's capsule does: 8 cm over flat ground, and higher over a slope by the
            // capsule's radius effect (r/cos - r), which GroundFeet removes from the rendered body again.
            float RootClearance(float degrees)=>.08f+.35f*(1/Mathf.Cos(degrees*Mathf.Deg2Rad)-1);
            var hips=rig.GetBoneTransform(HumanBodyBones.Hips);var chest=rig.GetBoneTransform(HumanBodyBones.Chest);
            string pace=walking?"":"-"+state+speed.ToString("0.#",CultureInfo.InvariantCulture);
            string body_=$"{hero}{(shaped?"Shaped":"Natural")}{pace}";
            string key=$"{body_}-{slope.ToString("0.#",CultureInfo.InvariantCulture)}";
            var writer=folder==null?null:new StreamWriter(Path.Combine(folder,key+".csv"));int shot=-1;
            writer?.WriteLine("pass,phase,"+string.Join(",",Traced.Select(b=>b+"X,"+b+"Y,"+b+"Z"))+",leftHeelX,leftHeelY,leftHeelZ,leftToeX,leftToeY,leftToeZ,rightHeelX,rightHeelY,rightHeelZ,rightToeX,rightToeY,rightToeZ,leftContact,leftWeight,rightContact,rightWeight,drop,reach,cadence,stride");
            void Trace(string pass,float phase,float cadence,float stride)
            {
                if(writer==null)return;
                var o=root.transform.position;var cells=new List<string>{pass,F(phase)};
                foreach(var b in Traced){var p=rig.GetBoneTransform(b).position-o;cells.Add(F(p.x));cells.Add(F(p.y));cells.Add(F(p.z));}
                foreach(bool isLeft in new[]{true,false})
                {
                    var heel=rig.GetBoneTransform(isLeft?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot).TransformPoint(isLeft?profile.leftHeel:profile.rightHeel)-o;
                    var toe=rig.GetBoneTransform(isLeft?HumanBodyBones.LeftToes:HumanBodyBones.RightToes).TransformPoint(isLeft?profile.leftToe:profile.rightToe)-o;
                    cells.AddRange(new[]{F(heel.x),F(heel.y),F(heel.z),F(toe.x),F(toe.y),F(toe.z)});
                }
                cells.AddRange(new[]{feet.ContactOf(true).ToString(),F(feet.ContactWeightOf(true)),feet.ContactOf(false).ToString(),F(feet.ContactWeightOf(false)),F(feet.RequestedSupportDrop),F(feet.MaximumReachCorrection),F(cadence),F(stride)});
                writer.WriteLine(string.Join(",",cells));
            }
            // What one pass measured, after its first cycle.
            float minimumKnee=0,minimumKneeDrop=0,stanceKnee=0,lowestHip=0,highestHip=0,pitch=0,recover=0;int samples=0;string worstFrame="";
            void Walk(Vector3 normal,Vector3 surface,float clearance,string pass,string name)
            {
                minimumKnee=180;minimumKneeDrop=float.PositiveInfinity;stanceKnee=180;lowestHip=float.PositiveInfinity;highestHip=float.NegativeInfinity;pitch=-90;recover=0;samples=0;worstFrame="";shot=-1;
                feet.ResetContacts();
                float grade=Mathf.Abs(normal.z)/normal.y,cadence=profile.Cadence(state,speed,grade),strideScale=profile.Stride(state,speed,cadence,grade);
                float Ground(Vector3 p)=>Vector3.Dot(normal,surface-new Vector3(p.x,0,p.z))/normal.y;
                for(int frame=0;frame<Mathf.CeilToInt(clip.length/cadence*3/dt);frame++)
                {
                    float time=frame*dt,phase=time*cadence/clip.length;
                    var position=Vector3.forward*speed*time;position.y=Ground(position)+clearance;root.transform.position=position;
                    pose.SetTime(Mathf.Repeat(phase,1)*clip.length);graph.Evaluate(0);
                    feet.Correct(Vector3.forward*speed,state,phase,strideScale,false,targetFacing:false,deltaTime:dt);
                    Shoot(phase,name);
                    if(phase<1)continue;samples++;Trace(pass,phase,cadence,strideScale);
                    foreach(var (hip,knee,ankle,toes,offset,isLeft) in new[]{(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.LeftToes,0f,true),(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,HumanBodyBones.RightToes,other,false)})
                    {
                        Vector3 h=rig.GetBoneTransform(hip).position,k=rig.GetBoneTransform(knee).position,a=rig.GetBoneTransform(ankle).position;
                        float legPhase=Mathf.Repeat(phase-offset,1),angle=Vector3.Angle(h-k,a-k);
                        if(legPhase<stanceEnd){stanceKnee=Mathf.Min(stanceKnee,angle);continue;}
                        if(angle<minimumKnee)worstFrame=$"legPhase={legPhase:F2} drop={feet.RequestedSupportDrop:F3} hip={hips.position.y-position.y:F3} footOverHip={a.y-h.y:F3} footAhead={Vector3.Dot(a-h,Vector3.forward):F3}";
                        minimumKnee=Mathf.Min(minimumKnee,angle);minimumKneeDrop=Mathf.Min(minimumKneeDrop,h.y-k.y);
                        // Early in the swing, how high the lower of heel and toe rides over the ground straight under it.
                        if(legPhase<stanceEnd+.25f)
                        {
                            var heel=rig.GetBoneTransform(ankle).TransformPoint(isLeft?profile.leftHeel:profile.rightHeel);
                            var toe=rig.GetBoneTransform(toes).TransformPoint(isLeft?profile.leftToe:profile.rightToe);
                            recover=Mathf.Max(recover,Mathf.Min(heel.y-Ground(heel),toe.y-Ground(toe)));
                        }
                    }
                    // The torso's lean off vertical along the travel, forward positive: a body diving into the slope pitches far forward.
                    var spine=chest.position-hips.position;pitch=Mathf.Max(pitch,Mathf.Atan2(spine.z,spine.y)*Mathf.Rad2Deg);
                    float height=hips.position.y-position.y+clearance-RootClearance(0);lowestHip=Mathf.Min(lowestHip,height);highestHip=Mathf.Max(highestHip,height);
                }
            }
            // Eight frames of the second cycle, one every eighth of a stride (the left foot lands at 0, the right at 4).
            void Shoot(float phase,string name)
            {
                if(stillsFolder==null||phase<1||phase>=2)return;
                int k=Mathf.FloorToInt((phase-1)*8);if(k==shot)return;shot=k;Still(root.transform,$"{name}-p{k}");
            }
            try
            {
                if(writer!=null)
                {
                    // The take as authored, nothing corrected: where the clip itself puts each joint over flat ground.
                    for(int frame=0;frame<120;frame++){float phase=frame/120f;pose.SetTime(phase*clip.length);root.transform.position=Vector3.up*RootClearance(0);graph.Evaluate(0);Trace("raw",phase,1,1);}
                }
                // Flat ground is the reference for how far the slope may crouch, bob, fold or tip the body.
                floor.transform.rotation=Quaternion.identity;Physics.SyncTransforms();
                Walk(Vector3.up,Vector3.zero,RootClearance(0),"flat",body_+"-flat");
                float flatLow=lowestHip,flatBob=highestHip-lowestHip,flatKnee=minimumKnee,flatKneeDrop=minimumKneeDrop,flatPitch=pitch,flatRecover=recover;
                float flat=0;
                // The walk's reference is the mean hip height over its flat cycle, as it always was.
                if(walking)flat=FlatMean();
                floor.transform.rotation=Quaternion.Euler(-slope,0,0);Physics.SyncTransforms();
                var normal=floor.transform.up;var surface=floor.transform.position+normal*.5f;
                Walk(normal,surface,RootClearance(slope),"slope",key);
                if(samples<=80)throw new System.InvalidOperationException("must cover repeated steps");
                if(walking)
                {
                    Debug.Log($"UPHILL_STEP {hero}/{shaped} slope={slope} samples={samples} flatHip={flat:F3} lowestHip={lowestHip:F3} bob={highestHip-lowestHip:F3} swingKnee={minimumKnee:F2} swingKneeBelowHip={minimumKneeDrop:F3} stanceKnee={stanceKnee:F2} torsoPitch={pitch:F1} recoverClear={recover:F3} worst[{worstFrame}]");
                    // The recorded fold: knee interior 60.88 degrees with the knee level with its hip, hips at 60% of their
                    // flat height and a 19 cm bob. A steep climb may bend the swing knee to about a right angle and crouch a
                    // little; it may not fold the thigh up to the hip or squat the body.
                    bool ok=minimumKnee>=80f&&minimumKneeDrop>=.12f&&lowestHip>=flat*.75f&&highestHip-lowestHip<=.12f;
                    if(!ok)open++;
                }
                else Debug.Log(System.FormattableString.Invariant($"UPHILL_RUN {hero}/{(shaped?"Shaped":"Natural")} {state} speed={speed} slope={slope} samples={samples} lowestHip={lowestHip:F3}({lowestHip/flatLow:P0} of flat {flatLow:F3}) bob={highestHip-lowestHip:F3}(flat {flatBob:F3}) swingKnee={minimumKnee:F1}(flat {flatKnee:F1}) swingKneeBelowHip={minimumKneeDrop:F3}(flat {flatKneeDrop:F3}) torsoPitch={pitch:F1}(flat {flatPitch:F1}) recoverClear={recover:F3}(flat {flatRecover:F3}) stanceKnee={stanceKnee:F1} upright={feet.UprightWeight:F2}"));
            }
            finally{writer?.Dispose();graph.Destroy();Object.DestroyImmediate(root);Object.DestroyImmediate(floor);}
            // The walk's flat reference: the mean hip height over its second flat cycle (unchanged from the November 4 audit).
            float FlatMean()
            {
                floor.transform.rotation=Quaternion.identity;Physics.SyncTransforms();feet.ResetContacts();
                float rate=profile.Cadence("Walk",speed,0),stride=profile.Stride("Walk",speed,rate,0),sum=0;int n=0;
                for(int frame=0;frame<Mathf.CeilToInt(clip.length/rate*2/dt);frame++)
                {
                    float time=frame*dt,phase=time*rate/clip.length;root.transform.position=new Vector3(0,RootClearance(0),speed*time);
                    pose.SetTime(Mathf.Repeat(phase,1)*clip.length);graph.Evaluate(0);
                    feet.Correct(Vector3.forward*speed,"Walk",phase,stride,false,targetFacing:false,deltaTime:dt);
                    if(phase>=1){sum+=hips.position.y-root.transform.position.y;n++;}
                }
                return sum/n;
            }
        }
        static string stillsFolder;
        static Material stillFloor;
        /// <summary>-uphill-stills (needs -Graphics): a lit, gridded stage so each walk can be drawn from the side.</summary>
        static void Stage()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.6f,.65f);
            var light=new GameObject("Step stills light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-60,0);
            stillFloor=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.3f,.34f,.4f)};
            var grid=new Texture2D(2,2){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
            grid.SetPixels(new[]{Color.white,new Color(.7f,.7f,.7f),new Color(.7f,.7f,.7f),Color.white});grid.Apply();
            stillFloor.mainTexture=grid;stillFloor.mainTextureScale=new Vector2(400,400);
        }
        /// <summary>The body from its right side, square to the slope's rise, with the ground line through the frame: torso, knees and feet read at once.</summary>
        static void Still(Transform root,string name)
        {
            var camera=new GameObject("Step stills camera",typeof(Camera)).GetComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=1.05f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.11f,.16f);
            camera.transform.position=root.position+new Vector3(4,.85f,0);camera.transform.LookAt(root.position+new Vector3(0,.85f,0));
            FacingAudit.Capture(camera,Path.Combine(stillsFolder,name+".png"));Object.DestroyImmediate(camera.gameObject);
        }
        static readonly HumanBodyBones[] Traced={HumanBodyBones.Hips,HumanBodyBones.Chest,HumanBodyBones.Head,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.LeftToes,
            HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,HumanBodyBones.RightToes};
        static string F(float v)=>v.ToString("F4",CultureInfo.InvariantCulture);
    }
}
