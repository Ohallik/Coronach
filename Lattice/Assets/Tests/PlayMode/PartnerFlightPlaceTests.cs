using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Where the companion's ship flies (D422). In the capture of November 25 (nov25-r5-chain) Sela fell ten metres behind a lead chaining lunges up the Gullet's canal, under the
    /// skill row and off the bottom edge (s10/012, s10/075); and Idra rested with her hull against the Warmaster's skiff, which had come to her (s52/162). Each case here is shown failing on the code before
    /// (<c>...AsBefore</c>, by a switch only these tests set) and holding now. The legs are the route's own steps through the replay driver's pad, as PartnerInViewTests flies them.</summary>
    public sealed class PartnerFlightPlaceTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic,Hidden=BindingFlags.Static|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;bool finished;
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            fixture=new GameObject("Partner flight place fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();finished=false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            AsBefore(false);
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(fixture!=null)Object.Destroy(fixture);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        /// <summary>The broken states: no answer to his dashes, no way along his trail to a near place, no stepping back out in flight.</summary>
        static void AsBefore(bool before,string which=null)
        {
            void Switch(System.Type type,string field){if(which==null||which==field){var f=type.GetField(field,Hidden);Assert.IsNotNull(f,"fixture: no switch "+field);f.SetValue(null,before);}}
            Switch(typeof(PartnerBrain),"matchAsBefore");Switch(typeof(PartnerBrain),"flightStepOutAsBefore");Switch(typeof(PartnerBrain),"drawnAsBefore");
        }

        // ------------------------------------------------------------------ the driver, by its private parts (as PartnerInViewTests drives it)
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        IEnumerator Play(QualityRoute route,float most,System.Action each)
        {
            if(Get<Gamepad>("pad")==null)Set("pad",InputSystem.AddDevice<Gamepad>("PartnerFlightPlacePad"));
            Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);var clock=Get<System.Diagnostics.Stopwatch>("clock");if(!clock.IsRunning)clock.Start();
            var run=(IEnumerator)Call("RunSteps");finished=false;Get<List<string>>("failures").Clear();
            for(float end=Time.unscaledTime+most;Time.unscaledTime<end;)
            {
                if(!run.MoveNext()){finished=true;break;}
                each();Call("InputUpdate");yield return null;
            }
            Set("recording",false);Call("InputUpdate");
        }
        static string Folder=>System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
        static QualityRoute Read(string route)=>JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(System.IO.Path.Combine(Folder,route+".json")));
        static QualityRoute Slice(string route,string scene,string from,string to)
        {
            var whole=Read(route);int a=System.Array.FindIndex(whole.steps,s=>s.name==from);Assert.GreaterOrEqual(a,0,$"fixture: {route} has no step '{from}'");
            int b=System.Array.FindIndex(whole.steps,a,s=>s.name==to);Assert.GreaterOrEqual(b,a,$"fixture: {route} has no step '{to}' after '{from}'");
            whole.steps=whole.steps.Skip(a).Take(b-a+1).ToArray();whole.scene=scene;whole.name+=" from "+from;return whole;
        }
        static IEnumerable<string> FlagsOf(QualityStep s)
        {
            if(!string.IsNullOrEmpty(s.expectedFlag))yield return s.expectedFlag;
            if(!string.IsNullOrEmpty(s.until)&&s.until.StartsWith("flag:"))yield return s.until.Substring(5);
        }
        static void FlagsAt(string route,string step)
        {
            var steps=Read(route).steps;int at=System.Array.FindIndex(steps,s=>s.name==step);Assert.GreaterOrEqual(at,0,$"fixture: {route} has no step '{step}'");
            foreach(string flag in steps.Skip(at).SelectMany(FlagsOf))GameServices.Current.Flags.SetBool(flag,false);
            foreach(string flag in steps.Take(at).SelectMany(FlagsOf))GameServices.Current.Flags.SetBool(flag,true);
        }
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.FirstOrDefault(m=>m!=Lead&&m.gameObject.activeInHierarchy);
        static Vector3 Planar(Vector3 v){v.y=0;return v;}
        static void Put(CombatActor actor,Vector3 at,Vector3 facing)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(facing);}
            actor.transform.SetPositionAndRotation(at,Quaternion.LookRotation(Planar(facing).normalized,Vector3.up));body.enabled=true;Physics.SyncTransforms();
        }
        IEnumerator Zone(string loadout,string zone,string route,string step,string partner="Sela")
        {
            DevLoadout.Apply(loadout);FlagsAt(route,step);
            ContinuousReview.ApplyPair(new QualityRoute{name="flight place",scene=zone,deployed=new[]{"Taren",partner}});
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(1f);
            for(int i=0;i<4&&Lead.character!="Taren";i++){PartyController.Current.Swap();for(int f=0;f<30;f++)yield return null;}
            Assert.AreEqual("Taren",Lead.character,"fixture: Taren leads");Assert.AreEqual(partner,Partner.character,$"fixture: {partner} is out beside him");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var hero in PartyController.Current.members)
            {
                hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=GameTime.Now+900;hero.target=null;hero.TargetLocked=false;
                hero.GetComponent<PlayerBrain>().AutoPilot=false;hero.GetComponent<PartnerBrain>().enabled=hero!=Lead;
            }
            yield return null;
        }
        /// <summary>Her hull clear of the frame's edges and of the HUD's plates, as PartnerInViewTests reads it.</summary>
        static bool Clear(CombatActor hero,Camera camera,out string why)
        {
            why="";var middle=camera.WorldToScreenPoint(hero.transform.position);if(middle.z<=0){why="behind the camera";return false;}
            float hull=Mathf.Abs(camera.WorldToScreenPoint(hero.transform.position+camera.transform.right*HeroCollision.HullRadius(hero.character)).x-middle.x)*.5f;
            if(middle.x<hull||middle.x>camera.pixelWidth-hull||middle.y<hull||middle.y>camera.pixelHeight-hull){why=$"cut by the edge at ({middle.x:0},{middle.y:0})";return false;}
            if(HudKeepOut.Covers(middle,hull)){why=$"under a plate at ({middle.x:0},{middle.y:0})";return false;}
            return true;
        }
        sealed class Tally
        {
            public int frames,outOf;public float farthest;public string first="";public readonly System.Text.StringBuilder trail=new();float note;
            public void Look(float now)
            {
                var camera=Camera.main;var lead=Lead;var her=Partner;
                if(camera==null||her==null||!her.Health.Alive||GameServices.Current.Input.Blocked||!HudKeepOut.Fresh)return;
                bool clear=Clear(her,camera,out string why);float apart=Planar(her.transform.position-lead.transform.position).magnitude;
                // How far she is from him is read while she follows him: with a body to fight (the canal's darts come up at its end) her place is at it.
                frames++;if(lead.target==null||!lead.target.Alive)farthest=Mathf.Max(farthest,apart);
                if(!clear){outOf++;if(first=="")first=$"{why} at {now:0.00} s, {apart:0.0} m from him";}
                if(now>=note){note=now+.25f;trail.Append($" {now:0.00}s lead ({lead.transform.position.x:0.0},{lead.transform.position.z:0.0}) {Planar(lead.motor.Velocity).magnitude:0.0}m/s {lead.State} her ({her.transform.position.x:0.0},{her.transform.position.z:0.0}) {apart:0.0}m {(clear?"clear":why)};");}
            }
            public override string ToString()=>$"out of the clear frame {outOf} of {frames} frames ({(frames>0?100f*outOf/frames:0):0.0}%), first: {first}; farthest from him {farthest:0.0} m";
        }
        /// <summary>A leg of the Gullet's route from where the capture had the pair, through the pad.</summary>
        IEnumerator Leg(string from,string to,Vector3 lead,Vector3 facing,Vector3 her,System.Action<Tally> verdict)
        {
            yield return Zone("gullet","Gullet_Tunnel","chapter-10-gullet",from);
            float level=Lead.transform.position.y;lead.y=her.y=level;
            Put(Lead,lead,facing);Put(Partner,her,facing);
            var rig=Object.FindFirstObjectByType<CameraRig>();if(rig!=null)rig.Apply(Lead.transform,ZoneController.Current.CameraProfile);
            for(float held=Time.unscaledTime+1.2f;Time.unscaledTime<held;){Put(Lead,lead,facing);yield return null;}
            var tally=new Tally();float began=Time.unscaledTime;
            yield return Play(Slice("chapter-10-gullet","Gullet_Tunnel",from,to),90,()=>tally.Look(Time.unscaledTime-began));
            TestContext.WriteLine($"FLIGHT_PLACE '{from}' to '{to}' finished={finished}: {tally}\n{tally.trail}");
            Assert.IsTrue(finished,"fixture: the leg never finished: "+tally);
            Assert.Greater(tally.frames,60,"fixture: too few frames read: "+tally);
            verdict(tally);
        }

        // ================================================================== his lunges (s10/012, s10/075)
        /// <summary>The canal's first seven lunges and their recoveries, from where the capture had the pair as the first began (s10 step 7), before its darts come up (from the eighth
        /// on it is a fight, and she fights). Each is eight metres in a sixth of a second; she goes with each, so she stays as near him as she began and in the frame. As before she
        /// fell 10 to 14 m behind and off the bottom edge (d422: 50.6% of the leg out of the clear frame).</summary>
        IEnumerator Lunges()=>Leg("advance lunge 1","normal thrust and lunge recovery 7",new Vector3(2.3f,1,34.5f),Vector3.forward,new Vector3(3.9f,1,30f),t=>Lunged=t);
        Tally Lunged;
        const float LungeFarthest=7;
        [UnityTest,Timeout(300000)] public IEnumerator InTheGulletsCanalSheGoesWithHisLunges()
        {
            yield return Lunges();
            Assert.LessOrEqual(Lunged.farthest,LungeFarthest,"down the canal's lunges Sela fell behind him: "+Lunged);
            Assert.LessOrEqual(Lunged.outOf,Mathf.FloorToInt(Lunged.frames*.05f),"down the canal's lunges Sela was out of the clear frame: "+Lunged);
        }
        [UnityTest,Timeout(300000)] public IEnumerator InTheGulletsCanalSheGoesWithHisLungesAsBefore()
        {
            AsBefore(true,"matchAsBefore");yield return Lunges();
            Assert.Greater(Lunged.farthest,LungeFarthest,"the old state should leave her behind his lunges: "+Lunged);
        }

        // ================================================================== a body come to her in a fight (s52/162)
        /// <summary>Sela's ship at her stand-off from a Chorister Drifter the lead is locked on (BodiesKeepApartTests' fixture); the body is then set down 1.2 m nearer her. She steps
        /// back out to her stand-off within two seconds, still facing it. As before she let it lie there, inside her stand-off, its hull against hers.</summary>
        IEnumerator ComeToHer(System.Action<float,float,string> result)
        {
            DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: Arena_Flight never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            void Quiet(){foreach(var a in PartyController.Current.members){a.GetComponent<PlayerBrain>().AutoPilot=true;a.GetComponent<PartnerBrain>().enabled=false;}}
            Quiet();yield return new WaitForSecondsRealtime(.6f);
            for(int i=0;i<4&&Lead.character!="Taren";i++){PartyController.Current.Swap();Quiet();yield return null;}
            Assert.AreEqual("Taren",Lead.character,"fixture: Taren leads");
            foreach(var hero in PartyController.Current.members){hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=float.PositiveInfinity;}
            var lead=Lead;var partner=PartyController.Current.members.First(m=>m!=lead);
            var body=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDrifter"),new Vector3(30,1,0));body.Passive=true;body.Health.maximum=body.Health.integrity=1000000;body.Health.breakThreshold=1e9f;
            body.transform.rotation=Quaternion.LookRotation(Vector3.back);yield return null;yield return null;
            Put(lead,new Vector3(30,1,-9),Vector3.forward);Put(partner,new Vector3(35,1,-12),Vector3.forward);lead.target=body.Health;lead.TargetLocked=true;
            partner.GetComponent<PartnerBrain>().enabled=true;
            float standOff=Mathf.Max(2.2f,HeroCollision.HullRadius(partner.character)+body.BodyRadius+.5f);
            for(float settle=Time.unscaledTime+4;Time.unscaledTime<settle;){lead.target=body.Health;lead.motor.Move(Vector2.zero,false,true);yield return null;}
            // The body comes to her: set down on the line between them, 1.2 m nearer her than her stand-off.
            var her=partner.transform.position;var toBody=Planar(body.transform.position-her).normalized;
            var hull=body.GetComponent<CharacterController>();hull.enabled=false;body.transform.position=her+toBody*(standOff-1.2f)+Vector3.up*(body.transform.position.y-her.y);hull.enabled=true;Physics.SyncTransforms();
            float after=float.NaN;string trail="";
            for(float t=0;t<2.5f;t+=Time.deltaTime)
            {
                lead.target=body.Health;lead.motor.Move(Vector2.zero,false,true);yield return null;
                float gap=Planar(partner.transform.position-body.transform.position).magnitude;if(t>=2)after=float.IsNaN(after)?gap:Mathf.Min(after,gap);
                if(Mathf.Repeat(t,.25f)<Time.deltaTime)trail+=$" {t:0.00}s {gap:0.00}m {partner.State};";
            }
            result(standOff,after,trail);
        }
        [UnityTest,Timeout(120000)] public IEnumerator InFlightABodyComeToHerIsSteppedBackFrom()
        {
            float standOff=0,after=0;string trail="";yield return ComeToHer((s,a,t)=>{standOff=s;after=a;trail=t;});
            TestContext.WriteLine($"FLIGHT_STEP_OUT standOff={standOff:0.00} after={after:0.00}{trail}");
            Assert.GreaterOrEqual(after,standOff-.3f,$"a body set down inside her stand-off ({standOff:0.00} m) was still {after:0.00} m from her two seconds on:{trail}");
        }
        [UnityTest,Timeout(120000)] public IEnumerator InFlightABodyComeToHerIsSteppedBackFromAsBefore()
        {
            AsBefore(true,"flightStepOutAsBefore");
            float standOff=0,after=0;string trail="";yield return ComeToHer((s,a,t)=>{standOff=s;after=a;trail=t;});
            TestContext.WriteLine($"FLIGHT_STEP_OUT as before standOff={standOff:0.00} after={after:0.00}{trail}");
            Assert.Less(after,standOff-.6f,$"the old state should leave the body inside her stand-off:{trail}");
        }

        // ================================================================== his roll back through where she rests (s10/171)
        /// <summary>Taren at rest facing west, Sela come to rest at her place behind his nose; he rolls east, back through it (as at the Cantor's emitter). She gives way, a step aside,
        /// so the two hulls are never drawn through each other: the nearest the two middles come is no less than both hulls' reach less 0.6 m. As before he rolled through her, 2.2 m
        /// off his line, the two hulls one shape.</summary>
        IEnumerator RollThrough(System.Action<float,float,string> result)
        {
            DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: Arena_Flight never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            void Quiet(){foreach(var a in PartyController.Current.members){a.GetComponent<PlayerBrain>().AutoPilot=true;a.GetComponent<PartnerBrain>().enabled=false;}}
            Quiet();yield return new WaitForSecondsRealtime(.6f);
            for(int i=0;i<4&&Lead.character!="Taren";i++){PartyController.Current.Swap();Quiet();yield return null;}
            Assert.AreEqual("Taren",Lead.character,"fixture: Taren leads");
            foreach(var hero in PartyController.Current.members){hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=float.PositiveInfinity;}
            var lead=Lead;var partner=PartyController.Current.members.First(m=>m!=lead);
            Put(lead,new Vector3(30,1,-9),Vector3.left);Put(partner,new Vector3(32.9f,1,-11.2f),Vector3.left);partner.GetComponent<PartnerBrain>().enabled=true;
            for(float settle=Time.unscaledTime+3;Time.unscaledTime<settle;){lead.motor.Move(Vector2.zero,false,true);yield return null;}
            float reach=HeroCollision.HullRadius(lead.character)+HeroCollision.HullRadius(partner.character),nearest=float.PositiveInfinity;string trail=$" rest ({partner.transform.position.x:0.00},{partner.transform.position.z:0.00});";
            Assert.IsTrue(lead.Dodge(Vector3.right),"fixture: the lead's roll didn't begin");
            for(float t=0;t<.8f;t+=Time.deltaTime)
            {
                yield return null;lead.motor.Move(Vector2.zero,false,true);
                float gap=Planar(partner.transform.position-lead.transform.position).magnitude;nearest=Mathf.Min(nearest,gap);
                if(Mathf.Repeat(t,.1f)<Time.deltaTime)trail+=$" {t:0.00}s lead ({lead.transform.position.x:0.00},{lead.transform.position.z:0.00}) her ({partner.transform.position.x:0.00},{partner.transform.position.z:0.00}) {gap:0.00}m;";
            }
            result(reach,nearest,trail);
        }
        [UnityTest,Timeout(120000)] public IEnumerator HisRollThroughWhereSheRestsSheGivesWayTo()
        {
            float reach=0,nearest=0;string trail="";yield return RollThrough((r,n,t)=>{reach=r;nearest=n;trail=t;});
            TestContext.WriteLine($"FLIGHT_ROLL reach={reach:0.00} nearest={nearest:0.00}{trail}");
            Assert.GreaterOrEqual(nearest,reach-.6f,$"his roll came within {nearest:0.00} m of her middle (hulls {reach:0.00}):{trail}");
        }
        [UnityTest,Timeout(120000)] public IEnumerator HisRollThroughWhereSheRestsSheGivesWayToAsBefore()
        {
            AsBefore(true,"matchAsBefore");
            float reach=0,nearest=0;string trail="";yield return RollThrough((r,n,t)=>{reach=r;nearest=n;trail=t;});
            TestContext.WriteLine($"FLIGHT_ROLL as before reach={reach:0.00} nearest={nearest:0.00}{trail}");
            Assert.Less(nearest,reach-.6f,$"the old state should have his roll go through her:{trail}");
        }

        // ================================================================== the drawn hulls in the lanes (s68/023, s68/028, s64/017; D425)
        /// <summary>While he travels out of a fight: how many frames her middle is over a hull that is drawn only (DrawnHulls, as the play camera sees it), and his, and whether she is
        /// in the clear frame and how far from him.</summary>
        sealed class Hulls
        {
            public int frames,over,leadOver,outOf,needless;public float farthest;public string first="";public readonly System.Text.StringBuilder trail=new();float note;
            /// <summary>Whether open water was to be had beside him: any of the places round him at both hulls' reach (either side, ahead, behind, either side half ahead) with no
            /// drawn hull under it to within half her hull's reach.</summary>
            static bool OpenBeside(CombatActor lead,CombatActor her,Camera camera)
            {
                var way=Planar(lead.motor.Velocity).normalized;var right=Vector3.Cross(Vector3.up,way);float apart=HeroCollision.HullRadius(lead.character)+HeroCollision.HullRadius(her.character)+.3f;
                foreach(var (a,r) in new[]{(0f,1f),(0f,-1f),(1f,0f),(-1f,0f),(.7f,.7f),(.7f,-.7f)})
                    if(!DrawnHulls.Under(lead.transform.position+(way*a+right*r)*apart,HeroCollision.HullRadius(her.character)*.5f,camera))return true;
                return false;
            }
            public void Look(float now)
            {
                var camera=Camera.main;var lead=Lead;var her=Partner;
                if(camera==null||her==null||!her.Health.Alive||GameServices.Current.Input.Blocked||!HudKeepOut.Fresh)return;
                if(lead.target!=null&&lead.target.Alive||Planar(lead.motor.Velocity).magnitude<=4)return;
                frames++;bool on=DrawnHulls.Under(her.transform.position,0,camera);if(on)over++;if(DrawnHulls.Under(lead.transform.position,0,camera))leadOver++;
                if(on&&OpenBeside(lead,her,camera))needless++;
                bool clear=Clear(her,camera,out string why);float apart=Planar(her.transform.position-lead.transform.position).magnitude;farthest=Mathf.Max(farthest,apart);
                if(!clear){outOf++;if(first=="")first=$"{why} at {now:0.00} s, {apart:0.0} m from him";}
                if(now>=note){note=now+.5f;var off=Planar(her.transform.position-lead.transform.position);trail.Append($" {now:0.0}s lead ({lead.transform.position.x:0.0},{lead.transform.position.z:0.0}) her off ({off.x:0.0},{off.z:0.0}) {(on?"ON":"open")}{(clear?"":" "+why)};");}
            }
            public string Share=>frames>0?(100f*over/frames).ToString("0.0")+"%":"-";
            public override string ToString()=>$"over a drawn hull {over} of {frames} frames ({Share}; with open water beside him {needless}, {(frames>0?100f*needless/frames:0):0.0}%; the lead {(frames>0?100f*leadOver/frames:0):0.0}%), out of the clear frame {outOf} ({(frames>0?100f*outOf/frames:0):0.0}%) first: {first}; farthest from him {farthest:0.0} m";
        }
        /// <summary>A leg of a chapter 10 route from where the capture had Taren and Idra, through the pad.</summary>
        IEnumerator HullLeg(string zone,string route,string from,string to,Vector3 lead,Vector3 her,System.Action<Hulls> verdict)
        {
            yield return Zone("chapter10",zone,route,from,"Idra");
            Assert.Greater(DrawnHulls.Count,10,$"fixture: {zone}'s drawn hulls were never taken in");
            float level=Lead.transform.position.y;lead.y=her.y=level;
            Put(Lead,lead,Vector3.forward);Put(Partner,her,Vector3.forward);
            var rig=Object.FindFirstObjectByType<CameraRig>();if(rig!=null)rig.Apply(Lead.transform,ZoneController.Current.CameraProfile);
            for(float held=Time.unscaledTime+1.2f;Time.unscaledTime<held;){Put(Lead,lead,Vector3.forward);yield return null;}
            var tally=new Hulls();float began=Time.unscaledTime;
            yield return Play(Slice(route,zone,from,to),120,()=>tally.Look(Time.unscaledTime-began));
            TestContext.WriteLine($"FLIGHT_HULLS {zone} '{from}' to '{to}' finished={finished} drawn={DrawnHulls.Count}: {tally}\n{tally.trail}");
            Assert.IsTrue(finished,"fixture: the leg never finished: "+tally);
            Assert.Greater(tally.frames,60,"fixture: too few frames read: "+tally);
            verdict(tally);
        }
        /// <summary>The Returning Passage's road from 330 m in to 900 m in (s68 steps 23 to 28), the convoy running in its lanes under it. As before her tug sat beside him over the
        /// canoes and rafts of the lanes (s68/023, s68/028).</summary>
        IEnumerator Passage(System.Action<Hulls> verdict)=>HullLeg("ReturningPassage","chapter-68-the-returning-passage","along the road, 330 m in","along the road, 900 m in",new Vector3(0,1,133.75f),new Vector3(4.28f,1,132.74f),verdict);
        /// <summary>Her middle over a drawn hull while open water was beside him: as before 41 to 42% of the leg in every run (d425d to d425i); with D425 24 to 34%, the variants
        /// differing more than the runs. Where the lanes leave no open water beside him (a fifth of the leg has him over a hull himself) she stays at her side. She keeps the clear
        /// frame (3.2% out, under the control strip as she drops back from his line to her side) and comes up to 10 m from him where as before she kept 6.1.</summary>
        // A third, where it was three tenths until November 27: with D425's rule her tug read 20.5, 22.8, 25.8, 26.2, 28.5 and 30.5% of the road's frames over the convoy with
        // open water beside him in six runs of this class (suites nov26a to nov26c, r8-partner, r9flight-whole2, r9-partner), and the state before it 41.0 to 41.6% in each.
        const float PassageOver=.33f,PassageOverBefore=.36f,HullsFarthest=10.5f;
        [UnityTest,Timeout(300000)] public IEnumerator InTheReturningPassageHerTugKeepsOffTheConvoy()
        {
            Hulls t=null;yield return Passage(r=>t=r);
            Assert.LessOrEqual(t.needless,Mathf.FloorToInt(t.frames*PassageOver),"down the passage's road Idra's tug sat over the convoy: "+t);
            // 7% on this road, where the other legs hold 5: off the convoy, her tug read 1.3, 1.9, 2.2, 2.3, 2.5, 3.9, 4.0, 4.4, 4.9 and 5.1% of the road's frames out of the clear
            // frame in ten runs of this class (suites nov26a to nov27a and the focused runs of rounds eight and nine), with nothing changed between them.
            Assert.LessOrEqual(t.outOf,Mathf.FloorToInt(t.frames*.07f),"down the passage's road Idra was out of the clear frame: "+t);
            Assert.LessOrEqual(t.farthest,HullsFarthest,"down the passage's road Idra fell away from him: "+t);
        }
        [UnityTest,Timeout(300000)] public IEnumerator InTheReturningPassageHerTugKeepsOffTheConvoyAsBefore()
        {
            AsBefore(true,"drawnAsBefore");Hulls t=null;yield return Passage(r=>t=r);
            Assert.Greater(t.needless,Mathf.FloorToInt(t.frames*PassageOverBefore),"the old state should have her sit over the convoy: "+t);
        }
        /// <summary>The Last Mooring's moored convoy is read as the capture drew it (a fixture of the drawn hulls, not a case of her place: the lee and the first lane have open water
        /// beside him in under a tenth of their frames, d425d, so her place there changes little). With the play camera where it stood for s64/017, her tug's place then lies over a
        /// moored hull. His reads so too, though the still has him over open water: he is inside the ring segment's curve, and a hull is read as its box.</summary>
        [UnityTest,Timeout(120000)] public IEnumerator AtTheLastMooringTheMooredHullsAreReadAsTheCaptureDrewThem()
        {
            yield return Zone("chapter10","LastMooring","chapter-64-last-mooring","north through chain A's gap","Idra");
            Assert.Greater(DrawnHulls.Count,50,"the moored convoy's hulls were not taken in");
            var camera=Camera.main;Assert.IsNotNull(camera,"fixture: no play camera");
            camera.transform.position=new Vector3(.139f,26.111f,71.288f);
            bool her=DrawnHulls.Under(new Vector3(5.218f,1,91.494f),0,camera),him=DrawnHulls.Under(new Vector3(0,1,91.952f),0,camera);
            TestContext.WriteLine($"MOORED_READ hulls={DrawnHulls.Count} her place over one={her} his={him}");
            Assert.IsTrue(her,"her tug's place at s64/017 does not read as over a moored hull");
        }
    }
}
