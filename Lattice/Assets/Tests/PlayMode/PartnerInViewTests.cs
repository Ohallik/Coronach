using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The partner where the player can see her (D361). In the whole-game capture of November 22 (nov22-whole) she trailed the travelling lead by seven to nine metres
    /// and never closed, so whenever he went up the screen she was under the control strip, under the skill icons or off the bottom edge: in flight s17/229, s17/282, s17/102,
    /// s17/151, s12/055, s23/159, s32/123; on foot s33/104, s34/116, s34/068, and in Tallow Drift's pressure-lift door at s16/144. Each leg here is the route's own, flown or walked
    /// from where the capture had the pair, through the replay driver's pad with her brain running and the HUD up; on every frame her body is read against the screen and the
    /// HUD's own plates (HudKeepOut). She may be out of the clear frame for no more than <see cref="Share"/> of a leg's frames, and where the lead stops on foot she comes to rest
    /// clear and in the camera's sight.</summary>
    public sealed class PartnerInViewTests
    {
        /// <summary>The share of a leg's frames she may spend out of the clear frame: a lead setting off from rest is a body's length gone before she is.</summary>
        const float Share=.05f;
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
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
            fixture=new GameObject("Partner in view fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();finished=false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(fixture!=null)Object.Destroy(fixture);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the driver, by its private parts (as PartnerFlightRejoinTests drives it)
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        List<string> Failures=>Get<List<string>>("failures");
        IEnumerator Play(QualityRoute route,float most,System.Action each)
        {
            if(Get<Gamepad>("pad")==null)Set("pad",InputSystem.AddDevice<Gamepad>("PartnerInViewPad"));
            Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);var clock=Get<System.Diagnostics.Stopwatch>("clock");if(!clock.IsRunning)clock.Start();
            var run=(IEnumerator)Call("RunSteps");finished=false;Failures.Clear();
            for(float end=Time.unscaledTime+most;Time.unscaledTime<end;)
            {
                if(!run.MoveNext()){finished=true;break;}
                each();Call("InputUpdate");yield return null;
            }
            Set("recording",false);Call("InputUpdate");
        }

        // ------------------------------------------------------------------ routes and fixtures
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
        /// <summary>The route's flags before the step (set) and from it on (cleared), so the zone stands as the route had it there.</summary>
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
            if(!actor.flight&&NavMesh.SamplePosition(at,out var floor,1.5f,NavMesh.AllAreas))at=floor.position;
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(facing);}
            else if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(facing);}
            actor.transform.SetPositionAndRotation(at,Quaternion.LookRotation(Planar(facing).normalized,Vector3.up));body.enabled=true;Physics.SyncTransforms();
        }
        IEnumerator Zone(string loadout,string zone,string route,string step,string partner="Sela")
        {
            DevLoadout.Apply(loadout);FlagsAt(route,step);
            ContinuousReview.ApplyPair(new QualityRoute{name="in view",scene=zone,deployed=new[]{"Taren",partner}});
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

        // ------------------------------------------------------------------ the measure
        /// <summary>Whether the player sees her body clear of the frame's edges and of the HUD's plates, at the camera as it is. In flight: the middle of her hull on the screen by
        /// its own half-width, and no plate within half of that of it. On foot: her feet, waist and head on the screen and under no plate.</summary>
        static bool Clear(CombatActor hero,Camera camera,out string why)
        {
            why="";var at=hero.transform.position;
            if(hero.flight)
            {
                var middle=camera.WorldToScreenPoint(at);if(middle.z<=0){why="behind the camera";return false;}
                float hull=Mathf.Abs(camera.WorldToScreenPoint(at+camera.transform.right*HeroCollision.HullRadius(hero.character)).x-middle.x)*.5f;
                if(middle.x<hull||middle.x>camera.pixelWidth-hull||middle.y<hull||middle.y>camera.pixelHeight-hull){why=$"cut by the edge at ({middle.x:0},{middle.y:0})";return false;}
                if(HudKeepOut.Covers(middle,hull)){why=$"under a plate at ({middle.x:0},{middle.y:0})";return false;}
                return true;
            }
            foreach(float lift in new[]{0f,.9f,1.7f})
            {
                var p=camera.WorldToScreenPoint(at+Vector3.up*lift);
                if(p.z<=0||p.x<0||p.x>camera.pixelWidth||p.y<0||p.y>camera.pixelHeight){why=$"off the frame at ({p.x:0},{p.y:0})";return false;}
                if(HudKeepOut.Covers(p)){why=$"under a plate at ({p.x:0},{p.y:0})";return false;}
            }
            return true;
        }
        /// <summary>Whether the camera sees her waist and her head: nothing drawn and solid on the line from it to either (a door's frame, a prop). An unseen collider hides nothing.</summary>
        static bool InSight(Camera camera,Vector3 at)=>!Behind(camera,at+Vector3.up*.9f)&&!Behind(camera,at+Vector3.up*1.6f);
        /// <summary>Whether both her waist and her head are behind something drawn.</summary>
        static bool Hidden(Camera camera,Vector3 at)=>Behind(camera,at+Vector3.up*.9f)&&Behind(camera,at+Vector3.up*1.6f);
        static bool Behind(Camera camera,Vector3 point)
        {
            var eye=camera.transform.position;var to=point-eye;
            foreach(var hit in Physics.RaycastAll(eye,to.normalized,to.magnitude-.3f,~0,QueryTriggerInteraction.Ignore))
            {
                if(!CombatCover.Opaque(hit.collider))continue;
                var drawn=hit.collider.GetComponent<Renderer>();if(drawn==null)drawn=hit.collider.GetComponentInParent<Renderer>();
                if(drawn!=null&&drawn.enabled&&drawn.gameObject.activeInHierarchy)return true;
            }
            return false;
        }
        sealed class Tally
        {
            public int frames,outOf,restFrames,restOut,hidden;public string first="",rest="";public float farthest;
            public readonly System.Text.StringBuilder trail=new();float note;
            public void Look(float now,bool resting,bool counted=true)
            {
                var camera=Camera.main;var lead=Lead;var her=Partner;
                if(camera==null||her==null||!her.Health.Alive||GameServices.Current.Input.Blocked||!HudKeepOut.Fresh)return;
                bool clear=Clear(her,camera,out string why);float apart=Planar(her.transform.position-lead.transform.position).magnitude;if(counted)farthest=Mathf.Max(farthest,apart);
                if(!counted){}
                else if(resting)
                {
                    restFrames++;bool seen=her.flight||InSight(camera,her.transform.position);
                    if(!clear||!seen){restOut++;if(!seen)hidden++;if(rest=="")rest=$"at rest {(clear?"hidden from the camera":why)}: she at {her.transform.position:F2}, the lead at {lead.transform.position:F2}";}
                }
                else
                {
                    frames++;
                    // On foot, hidden whole from the camera (her waist and her head behind something drawn: a door's frame, a prop) is out of view too.
                    if(clear&&!her.flight&&Hidden(camera,her.transform.position)){clear=false;why="hidden from the camera";}
                    if(!clear){outOf++;if(first=="")first=$"{why}: she at {her.transform.position:F2} {apart:0.0} m from the lead at {lead.transform.position:F2} going {Planar(lead.motor.Velocity).magnitude:0.0} m/s";}
                }
                if(now>=note)
                {
                    note=now+.25f;string drive=her.motor is FlightMotor ship?$" drive={ship.EngineDrive:0.0}":"";
                    var brain=her.GetComponent<PartnerBrain>();object hurry=typeof(PartnerBrain).GetField("hurrying",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(brain);
                    if(typeof(PartnerBrain).GetField("ownCarry",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(brain) is Vector3 carry&&carry.magnitude>.5f)drive+=$" carried={carry.magnitude:0.0}";
                    var guide=typeof(PartnerBrain).GetField("flightObstacles",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(brain);
                    if(guide!=null)drive+=" "+string.Join(",",guide.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic).Where(f=>f.FieldType==typeof(bool)).Select(f=>f.Name+"="+((bool)f.GetValue(guide)?"T":"F")));
                    // What is in front of her along her travel (the solid she brakes for), and which plate she is under, by its rectangle on a screen of this size.
                    if(her.flight){var v=Planar(her.motor.Velocity);if(v.magnitude>.5f&&CombatCover.Sweep(her.transform.position+Vector3.up*.8f,her.transform.position+Vector3.up*.8f+v.normalized*4,HeroCollision.HullRadius(her.character),out var hit))drive+=$" ahead={hit.collider.name}@{hit.distance:0.0}";}
                    if(!clear&&why.StartsWith("under a plate")){var p=camera.WorldToScreenPoint(her.transform.position+Vector3.up*(her.flight?0:.9f));var under=HudKeepOut.Rects.Where(x=>p.x>x.xMin-30&&p.x<x.xMax+30&&p.y>x.yMin-30&&p.y<x.yMax+30).Select(x=>$"({x.x:0},{x.y:0},{x.width:0}x{x.height:0})");drive+=$" plate={string.Join("+",under)} of {camera.pixelWidth}x{camera.pixelHeight}";}
                    trail.Append($" {now:0.00}s lead ({lead.transform.position.x:0.0},{lead.transform.position.z:0.0}) {Planar(lead.motor.Velocity).magnitude:0.0}m/s {her.character} ({her.transform.position.x:0.0},{her.transform.position.z:0.0}) {Planar(her.motor.Velocity).magnitude:0.0}m/s{drive} hurry={hurry} {apart:0.0}m {(clear?"clear":why)};");
                }
            }
            public override string ToString()=>$"out of the clear frame {outOf} of {frames} frames on the move ({(frames>0?100f*outOf/frames:0):0.0}%), first: {first}; at rest {restOut} of {restFrames} ({hidden} hidden){(rest!=""?": "+rest:"")}; farthest from him {farthest:0.0} m";
        }
        /// <summary>A leg of a route as the capture flew or walked it: the zone in the route's state there, the pair where the capture had them, the steps through the pad. On foot the lead
        /// then stands three seconds, and she must come to rest clear and in sight. Where the capture had the lead already flying as the named step began, the step before it is flown
        /// from the capture's staging and not read (<paramref name="readFrom"/>): staged at rest on the named step itself, he set off from a standing start into a turn away from her
        /// side, which the route never asks (route 17 at barge two's rest: the capture had him at 13.6 m/s as step 229 began; the Aurora Shear's short lane: at 14.2 m/s as step 102
        /// began). From rest she fell six to eight metres behind for a second while he gathered speed and was off the bottom edge for it: in eight readings each on November 22,
        /// 2 to 17% of route 17's leg and 9 to 18% of the Aurora Shear's, from one run to the next. <paramref name="share"/> is the leg's own bound where it has one (route 17).</summary>
        IEnumerator Leg(string loadout,string zone,string route,string from,string to,Vector3 lead,Vector3 facing,Vector3 her,string still,string quest=null,string readFrom=null,float share=Share,
            System.Action begin=null,bool herHeld=false,System.Action loaded=null,float readAfter=0,string partner="Sela",System.Action during=null)
        {
            yield return Zone(loadout,zone,route,from,partner);loaded?.Invoke();
            float level=Lead.transform.position.y;lead.y=her.y=Lead.flight?level:lead.y;
            Put(Lead,lead,facing);Put(Partner,her,facing);
            // The view is cut to him where he now stands, as a load does, and let come to rest: the rig would otherwise still be sliding over from the arrival.
            var rig=Object.FindFirstObjectByType<CameraRig>();if(rig!=null)rig.Apply(Lead.transform,ZoneController.Current.CameraProfile);
            // The lead held where the capture had him while it does (an Aurora current carried him forty metres up the short lane in the six seconds it took, and the leg then began
            // somewhere the capture never flew); she, her brain running, comes to her place by him as she would at the end of the step before. Held too (herHeld) where the leg
            // begins with something the capture had moving through where her place would be (barge three adrift).
            void Hold(){Put(Lead,lead,facing);if(herHeld)Put(Partner,her,facing);}
            float held=Time.unscaledTime+.6f;while(Time.unscaledTime<held){Hold();yield return null;}
            var eye=Camera.main.transform.position;float since=Time.unscaledTime,deadline=Time.unscaledTime+6;
            while(Time.unscaledTime-since<.5f&&Time.unscaledTime<deadline){Hold();yield return null;var now=Camera.main.transform.position;if(Vector3.Distance(eye,now)>.002f){eye=now;since=Time.unscaledTime;}}
            Hold();
            // A quest given by the talk the leg follows comes up as its card in the top right corner as the leg begins, as the capture had it (s33/104: "NEW QUEST Cutter Down").
            if(quest!=null){GameServices.Current.State.questSteps.Remove(quest);Lattice.Rpg.RpgServices.Quests.Start(quest);}
            begin?.Invoke();
            var tally=new Tally();float began=Time.unscaledTime;var slice=Slice(route,zone,from,to);
            int read=readFrom==null?0:System.Array.FindIndex(slice.steps,s=>s.name==readFrom);Assert.GreaterOrEqual(read,0,$"fixture: the slice has no step '{readFrom}'");
            yield return Play(slice,90,()=>{tally.Look(Time.unscaledTime-began,false,Get<int>("stepIndex")>=read&&Time.unscaledTime-began>=readAfter);during?.Invoke();});
            bool ground=!Lead.flight;
            if(ground)
            {
                for(float end=Time.unscaledTime+3;Time.unscaledTime<end;){yield return null;if(end-Time.unscaledTime<1)tally.Look(Time.unscaledTime-began,true);}
            }
            string told=$"{still} ({zone}, '{from}' to '{to}', finished={finished}{(Failures.Count>0?", "+string.Join(" | ",Failures):"")}): {tally}";
            TestContext.WriteLine("IN_VIEW "+told+"\n"+tally.trail);
            Assert.IsTrue(finished,"fixture: the leg never finished: "+told);
            Assert.Greater(tally.frames,60,"fixture: too few frames read on the move: "+told);
            Assert.LessOrEqual(tally.outOf,Mathf.FloorToInt(tally.frames*share),$"Sela was out of the clear frame for more than {share:P0} of the leg's frames: "+told);
            if(ground)
            {
                Assert.Greater(tally.restFrames,20,"fixture: too few frames read at rest: "+told);
                Assert.AreEqual(0,tally.restOut,"where the lead stopped, Sela didn't come to rest clear of the HUD and in the camera's sight: "+told);
            }
        }

        // ================================================================== in flight
        [UnityTest,Timeout(300000)] public IEnumerator XylosNearspaceClearOfBargeOnesDrift()=>
            Leg("chapter3","XylosNearspace","chapter-17-xylos-nearspace","clear of barge one's drift","clear of barge one's drift",new Vector3(49.42f,1,247.65f),new Vector3(.677f,0,.736f),new Vector3(47.9f,1,243.29f),"s17/151");
        /// <summary>Its own bound, 25% (the other legs keep <see cref="Share"/>). As step 229 begins the lead turns from south-east to north-east at 13.7 m/s, and she, beside him on the
        /// outside of the turn, meets loose pane "36 -9" on her way round: she brakes for it (5 m/s for a second, her steering reading the pane 1.4 m ahead), falls eighteen metres behind
        /// and boosts back within 0.9 s of the leg, off the left edge until she does. That is flying round a solid, not a fault; on either side of it she keeps four metres off him and
        /// in the frame for the rest of the leg. Read so (from 'back to the lane past the panes'), November 22: 13.0, 16.2 and 15.1% in three runs; the bound is the most of those
        /// and half as much again.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator XylosNearspacePastBargeTwosRest()=>
            Leg("chapter3","XylosNearspace","chapter-17-xylos-nearspace","back to the lane past the panes","past barge two's rest",new Vector3(336.86f,1,870.85f),new Vector3(.973f,0,-.232f),new Vector3(332.89f,1,870.79f),"s17/229",readFrom:"past barge two's rest",share:.25f);
        /// <summary>Barge three, cut free in the fight with its skimmers, drifts across its flank knot's point as step 282 begins (nov22b-whole s17, 323.4 s): the lead is shoved east
        /// along its flank and flies a loop back round to the point, and she keeps beside him on the outside of the loop. As he comes down its last leg past the point at 13.7 m/s and
        /// curls back up to it, she, at a boost twelve metres out, is too fast to turn after her place: she flew a circle round him sixteen metres across, south under the frame's
        /// bottom edge, and came back round his far side as the knots' steps began (s17/282 at the step's end, s17/286 half a second into the knots' steps). Each leg is flown from
        /// 327.5 s, 4.1 s into the step, the pair moving as the capture had them there and the barge as far down its drift; the 4.1 s before it are the barge between them, from
        /// where the skimmers' fight had left her, which no place of hers can put in the frame. s17/282 is read from 1.7 s on (329.2 s in the capture), as he comes down the
        /// loop's last leg past the point and curls up to it, through the knots' steps (they were cleared with the skimmers); s17/286 from the knots' steps on. Before 1.7 s she
        /// rides the outside of his loop ten metres out of him, as the capture had her, which on the test's 640x480 screen puts her under its corner plates for part of it. s17/282 has its own bound, 15%: what it reads out
        /// on the fix is HUD plates of that screen, never an edge (the corner status plate for a tenth of a second as she comes in off the loop eight metres out, and a plate at the left
        /// edge for a fifth of a second at 2.1 s), 9.0 and 9.6% in two runs on November 23; the bound is the most of those and half as much again. On the code before (D361) it read
        /// 60.8%, off the bottom edge, and s17/286 29.9%.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator XylosNearspaceBesideBargeThreesFlankKnot()=>
            Leg("chapter3","XylosNearspace","chapter-17-xylos-nearspace","beside barge three's flank knot","Barge3_Knots cleared",BargeThreeLead,BargeThreeFacing,BargeThreeHer,"s17/282",
                begin:BargeThreeLoop,herHeld:true,loaded:BargeThreeKnotsCleared,readAfter:1.7f,share:.15f);
        [UnityTest,Timeout(300000)] public IEnumerator XylosNearspaceAtBargeThreesKnots()=>
            Leg("chapter3","XylosNearspace","chapter-17-xylos-nearspace","beside barge three's flank knot","barge three comes free",BargeThreeLead,BargeThreeFacing,BargeThreeHer,"s17/286",
                readFrom:"lock Barge3_Knots",begin:BargeThreeLoop,herHeld:true,loaded:BargeThreeKnotsCleared);
        /// <summary>The pair at 327.508 s in the capture's frames.csv: where each was, the lead's nose, and how fast each was going (from the frames either side).</summary>
        static readonly Vector3 BargeThreeLead=new(772.803f,1,1157.654f),BargeThreeFacing=new(-.542f,0,-.84f),BargeThreeHer=new(779.929f,1,1163.661f),
            BargeThreeLeadGoing=new(-11.55f,0,-2.15f),BargeThreeHerGoing=new(-17.06f,0,7.11f);
        /// <summary>How much of its twelve seconds' drift barge three had gone at 327.5 s: 0.27 as the step began (the lead against its starboard flank at (767.99, 1141.59) and
        /// shoved east at five metres a second, Sela against its stern at (771.94, 1134.53) a second later, in the capture's player.log), and 4.1 s more.</summary>
        const float BargeThreeGone=.27f+4.1f/12;
        /// <summary>The knots' fight cleared, as the capture's log has it (cleared with the skimmers'), before the pair stand in it: its knots are gone with the zone's enemies.</summary>
        static void BargeThreeKnotsCleared()
        {
            GameServices.Current.Flags.SetBool("clear.Barge3_Knots",true);
            var knots=Object.FindObjectsByType<Lattice.World.EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(v=>v.encounterId=="Barge3_Knots");
            typeof(Lattice.World.EncounterVolume).GetMethod("FinishSaved",Private).Invoke(knots,null);
        }
        /// <summary>Barge three cut free and as far down its drift as the capture had it, and the pair going as the capture had them, as the leg begins.</summary>
        static void BargeThreeLoop()
        {
            var barge=Object.FindObjectsByType<Lattice.World.SnaggedBarge>(FindObjectsSortMode.None).First(b=>b.freedFlag=="xylos.barge3");
            var type=typeof(Lattice.World.SnaggedBarge);
            // Free without what its release said and reported (the lead's bark, the quest's card): the capture had those as the last knot went, seconds before.
            type.GetField("free",Private).SetValue(barge,true);GameServices.Current.Flags.SetBool(barge.freedFlag,true);type.GetMethod("Show",Private).Invoke(barge,null);
            type.GetField("gone",Private).SetValue(barge,BargeThreeGone);type.GetMethod("Place",Private).Invoke(barge,new object[]{Mathf.SmoothStep(0,1,BargeThreeGone)});
            Physics.SyncTransforms();
            Going(Lead,BargeThreeLeadGoing,BargeThreeFacing);Going(Partner,BargeThreeHerGoing,BargeThreeHerGoing);
        }
        static void Going(CombatActor actor,Vector3 velocity,Vector3 facing)
        {
            var ship=(FlightMotor)actor.motor;typeof(FlightMotor).GetField("velocity",Private).SetValue(ship,velocity);ship.KeepFacing(facing);
        }
        [UnityTest,Timeout(300000)] public IEnumerator WickReachTowardTheFirstRelay()=>
            Leg("chapter2","WickReach","chapter-12-wick-reach","toward the first relay","settle at the first relay",new Vector3(2.9f,1,122.02f),new Vector3(.674f,0,.739f),new Vector3(6.78f,1,121.32f),"s12/055");
        [UnityTest,Timeout(300000)] public IEnumerator SporeVeilPastTheSecondBeacon()=>
            Leg("chapter4","SporeVeil","chapter-20-spore-veil","past the second beacon","past the second beacon",new Vector3(4,1,447.5f),new Vector3(-.143f,0,.99f),new Vector3(5.72f,1,443.74f),"s23/159");
        [UnityTest,Timeout(300000)] public IEnumerator AuroraShearDownTheShortLane()=>
            Leg("chapter6","AuroraShear","chapter-32-aurora-shear","across to the short lane at 345","on down the short lane until Idra calls it",new Vector3(44.71f,1,320.41f),new Vector3(-.231f,0,.973f),new Vector3(48.66f,1,317.08f),"s32/123",readFrom:"across to the short lane at 370");

        // ================================================================== on foot
        [UnityTest,Timeout(300000)] public IEnumerator LastlightOnToHaskAndStopping()=>
            Leg("chapter6","Lastlight","chapter-33-lastlight","on to Hask at the pen gate","on to Hask at the pen gate",new Vector3(-17.36f,.09f,25.84f),new Vector3(-.953f,0,-.303f),new Vector3(-14.99f,.09f,27.37f),"s33/104","CutterDown");
        [UnityTest,Timeout(300000)] public IEnumerator LastlightWestToTheReachGate()=>
            Leg("chapter6","Lastlight","chapter-34-lastlight-errands","west to the Reach gate 6/9","west to the Reach gate 7/9",new Vector3(-7.82f,.09f,41.74f),new Vector3(-.574f,0,.819f),new Vector3(-3,.09f,38.48f),"s34/116");
        [UnityTest,Timeout(300000)] public IEnumerator TallowDriftOutOfThePressureLiftAndStopping()=>
            Leg("chapter2","TallowDrift","chapter-16-lantern-home","through the pressure cabin","through the pressure cabin",new Vector3(0,.08f,-8),Vector3.forward,new Vector3(1.63f,.08f,-10.08f),"s16/144");

        // ================================================================== where a panel comes up (D394)
        /// <summary>Keeps a card's place as the HUD keeps a card that has been up and gone down (HudKeepOut.Reserve), after the HUD has filled its list for the frame (GameHud, order 905).</summary>
        [DefaultExecutionOrder(30000)] sealed class CardKept:MonoBehaviour{public Rect card;void LateUpdate(){if(HudKeepOut.Fresh)HudKeepOut.Reserve(card);}}
        static void AsBefore(bool before)=>typeof(PartnerBrain).GetField("asBefore",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,before);
        /// <summary>Sela stood in Hushwell where the quest's card then came up over her, and was under it a second before she walked out (nov23-whole s08/095, a sliver at its edge at
        /// s08/098). Here Taren stands where the capture had him after the Bellows and she comes to rest by him; the card's place (the capture's 555 by 160 of 1920 by 1080, as a
        /// share of this screen) is put where she rests, kept as the HUD keeps a card that has been up and gone down (HudKeepOut.Reserve), for three seconds, and then the card comes
        /// up there. She must not be under it as it does. Before (PartnerBrain.asBefore) she stays where she rested and is.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator HushwellSheRestsWhereNoCardComesUpOverHer()
        {
            yield return Zone("hushwell","Hushwell","chapter-08-hushwell-nursery","both heroes ready after the Bellows");
            var lead=new Vector3(-6.96f,-7.92f,272.41f);var facing=new Vector3(-.04f,0,.998f);bool under=false;string where="";
            try
            {
                foreach(bool before in new[]{true,false})
                {
                    AsBefore(before);Put(Lead,lead,facing);Put(Partner,lead+new Vector3(2,0,-3),facing);
                    // Until she has come to rest: still for a second (she settles where the frame lets her, which can take a few seconds on this screen).
                    float still=Time.unscaledTime;var was=Partner.transform.position;
                    for(float end=Time.unscaledTime+10;Time.unscaledTime<end&&Time.unscaledTime-still<1;)
                    {Put(Lead,lead,facing);yield return null;if(Planar(Partner.transform.position-was).magnitude>.02f){was=Partner.transform.position;still=Time.unscaledTime;}}
                    var camera=Camera.main;var waist=camera.WorldToScreenPoint(Partner.transform.position+Vector3.up*.9f);
                    var card=new Rect(waist.x-camera.pixelWidth*.145f,waist.y-camera.pixelHeight*.074f,camera.pixelWidth*.29f,camera.pixelHeight*.148f);
                    var keeper=new GameObject("Card kept").AddComponent<CardKept>();keeper.card=card;
                    for(float end=Time.unscaledTime+3;Time.unscaledTime<end;){Put(Lead,lead,facing);yield return null;}
                    Object.Destroy(keeper.gameObject);
                    Assert.IsTrue(HudKeepOut.Fresh,"fixture: the HUD keeps no plates here: the check proves nothing");
                    under=false;where=$"card ({card.x:0},{card.y:0},{card.width:0}x{card.height:0}) of {camera.pixelWidth}x{camera.pixelHeight}";
                    foreach(float lift in new[]{0f,.9f,1.7f}){var p=camera.WorldToScreenPoint(Partner.transform.position+Vector3.up*lift);if(p.z>0&&card.Contains(p)){under=true;where+=$", her at ({p.x:0},{p.y:0})";}}
                    TestContext.WriteLine($"D394_CARD before={before} under={under} {where} she at {Partner.transform.position:F2}");
                    if(before)Assert.IsTrue(under,"fixture: resting as before D394 she was clear of the card's place anyway: the check below proves nothing: "+where);
                }
                Assert.IsFalse(under,"Sela rested where the quest's card comes up, and was under it as it did: "+where);
            }
            finally{AsBefore(false);}
        }

        // ================================================================== a talk in flight (D397)
        /// <summary>The talk's box as it is drawn (DialoguePanel's frame), in screen pixels; empty while it is down.</summary>
        static Rect TalkBox()
        {
            var view=Object.FindFirstObjectByType<DialoguePanel>();if(view==null||!view.IsVisible)return Rect.zero;
            var frame=(GameObject)typeof(DialoguePanel).GetField("panel",Private).GetValue(view);var canvas=frame.GetComponentInParent<Canvas>().rootCanvas;
            var corners=new Vector3[4];((RectTransform)frame.transform).GetWorldCorners(corners);var eye=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var a=RectTransformUtility.WorldToScreenPoint(eye,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(eye,corners[2]);
            return Rect.MinMaxRect(a.x,a.y,b.x,b.y);
        }
        /// <summary>How long after the talk's box comes up she is read: her one move to a clear place takes up to a second.</summary>
        const float TalkSettles=1.15f;
        /// <summary>Teo's radio line (nov23-whole s17/248) opened with Sela beside the lead on his right, which flying east-north-east is down the screen, and the talk froze her
        /// there under its box. The pair are staged as the capture had them three seconds before it (both at 13.8 m/s, she 3.7 m off his right) and fly into the line; from
        /// <see cref="TalkSettles"/> after its box comes up, through two of its lines, her hull must be clear of the box and of the frame's edges.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator XylosNearspaceTeosRadioLineLeavesHerClearOfItsBox()=>TeosRadioLine();
        [UnityTest,Timeout(300000)] public IEnumerator XylosNearspaceTeosRadioLineAsBefore()=>Before(TeosRadioLine());
        /// <summary>D442. The same line with a plate over every place beside him that is clear of its box (the frame's whole upper part, as the quest's card and the objective between
        /// them covered all seven in the whole suite of November 26, where she stayed under the box). The HUD's plates are down while a talk's box is up: as it was
        /// (<c>PartnerBrain.talkPlatesKept</c>) she is found no place; now she is found one clear of the box. Her rule is asked directly, on the hold's first frame, so the plate is
        /// there whatever the HUD had up.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator XylosNearspaceTeosRadioLineWithAPlateOverEveryPlaceClearOfItsBox()
        {
            var facing=new Vector3(.917f,0,.399f);bool probed=false,nowClear=false;string before="unread",now="unread";
            var kept=typeof(PartnerBrain).GetField("talkPlatesKept",BindingFlags.Static|BindingFlags.NonPublic);
            var place=typeof(PartnerBrain).GetMethod("TalkPlace",BindingFlags.Instance|BindingFlags.NonPublic);
            var clear=typeof(PartnerBrain).GetMethod("ClearOfTheBox",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.IsNotNull(kept,"fixture: PartnerBrain.talkPlatesKept");Assert.IsNotNull(place,"fixture: PartnerBrain.TalkPlace");Assert.IsNotNull(clear,"fixture: PartnerBrain.ClearOfTheBox");
            void During()
            {
                var camera=Camera.main;var her=Partner;if(probed||camera==null||her==null||!GameServices.Current.Input.Blocked)return;
                probed=true;var brain=her.GetComponent<PartnerBrain>();var were=HudKeepOut.Rects.ToList();
                HudKeepOut.Begin();HudKeepOut.Add(Rect.MinMaxRect(0,camera.pixelHeight*.42f,camera.pixelWidth,camera.pixelHeight));
                try
                {
                    kept.SetValue(null,true);var was=(Vector3?)place.Invoke(brain,new object[]{camera,Lead});before=was.HasValue?was.Value.ToString("F2"):"none";
                    kept.SetValue(null,false);var found=(Vector3?)place.Invoke(brain,new object[]{camera,Lead});now=found.HasValue?found.Value.ToString("F2"):"none";
                    nowClear=found.HasValue&&(bool)clear.Invoke(brain,new object[]{camera,found.Value,false});
                }
                finally{kept.SetValue(null,false);HudKeepOut.Begin();foreach(var r in were)HudKeepOut.Add(r);}
            }
            yield return Leg("chapter3","XylosNearspace","chapter-17-xylos-nearspace","fly on until Teo's sight of the claim drones","read Teo's sight of the claim drones line 2",
                new Vector3(678.12f,1,1081.47f),facing,new Vector3(678.97f,1,1077.73f),"s17/248",begin:()=>{Going(Lead,facing*13.8f,facing);Going(Partner,facing*13.8f,facing);},herHeld:true,during:During);
            TestContext.WriteLine($"D442_TALK_PLACE with a plate over every place clear of the box: as it was {before}, now {now}");
            Assert.IsTrue(probed,"fixture: the radio line never held the party");
            Assert.AreEqual("none",before,"fixture: as it was she was found a place all the same, so the plate doesn't cover them");
            Assert.IsTrue(nowClear,"with the plates down she was still found no place clear of the talk's box: "+now);
        }
        /// <summary>A D397 case flown as before it (PartnerBrain.asBefore), which must fail on her as the capture did, or the case proves nothing.</summary>
        IEnumerator Before(IEnumerator check)
        {
            AsBefore(true);string why=null;
            try
            {
                while(true)
                {
                    object next;
                    try{if(!check.MoveNext())break;next=check.Current;}
                    catch(AssertionException failed){why=failed.Message;break;}
                    yield return next;
                }
            }
            finally{AsBefore(false);}
            TestContext.WriteLine("D397_BEFORE "+(why??"held"));
            Assert.IsNotNull(why,"fixture: flown as before D397 the case held anyway: it proves nothing");
            Assert.IsFalse(why.Contains("fixture:"),"fixture: flown as before D397 the case failed on its fixture, not on her: "+why);
        }
        IEnumerator TeosRadioLine()
        {
            var facing=new Vector3(.917f,0,.399f);float opened=-1;int read=0,under=0;string first="";
            void During()
            {
                var camera=Camera.main;var her=Partner;if(camera==null||her==null||!GameServices.Current.Input.Blocked)return;
                var box=TalkBox();if(box.width<=0)return;
                if(opened<0)
                {
                    // What her rule had to go on as the box came up: the plates it keeps out from under and the place it chose (none: she stays).
                    opened=Time.unscaledTime;var brain=her.GetComponent<PartnerBrain>();
                    var chose=brain!=null?typeof(PartnerBrain).GetField("talkTo",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.GetValue(brain):null;
                    var plates=string.Join(" ",HudKeepOut.Rects.Select(r=>$"({r.xMin:0},{r.yMin:0})-({r.xMax:0},{r.yMax:0})"));
                    TestContext.WriteLine($"D397_TALK_OPENS she at {her.transform.position:F2} screen {(Vector2)camera.WorldToScreenPoint(her.transform.position)}, the lead at {Lead.transform.position:F2} screen {(Vector2)camera.WorldToScreenPoint(Lead.transform.position)} facing {Lead.motor.Facing:F2}, her place {(chose==null?"none":chose.ToString())}, plates {plates}");
                }
                if(Time.unscaledTime-opened<TalkSettles)return;
                read++;var middle=camera.WorldToScreenPoint(her.transform.position);
                float hull=Mathf.Abs(camera.WorldToScreenPoint(her.transform.position+camera.transform.right*HeroCollision.HullRadius(her.character)).x-middle.x)*.5f;
                bool cut=middle.z<=0||middle.x<hull||middle.x>camera.pixelWidth-hull||middle.y<hull||middle.y>camera.pixelHeight-hull;
                bool covered=middle.x>box.xMin-hull&&middle.x<box.xMax+hull&&middle.y>box.yMin-hull&&middle.y<box.yMax+hull;
                if((cut||covered)&&under++==0)first=$"{(cut?"cut by the frame":"under the talk's box")} at ({middle.x:0},{middle.y:0}), the box ({box.xMin:0},{box.yMin:0})-({box.xMax:0},{box.yMax:0}) of {camera.pixelWidth}x{camera.pixelHeight}, she at {her.transform.position:F2}, the lead at {Lead.transform.position:F2}";
            }
            yield return Leg("chapter3","XylosNearspace","chapter-17-xylos-nearspace","fly on until Teo's sight of the claim drones","read Teo's sight of the claim drones line 2",
                new Vector3(678.12f,1,1081.47f),facing,new Vector3(678.97f,1,1077.73f),"s17/248",begin:()=>{Going(Lead,facing*13.8f,facing);Going(Partner,facing*13.8f,facing);},herHeld:true,during:During);
            TestContext.WriteLine($"D397_TALK read {read} frames from {TalkSettles:0.00} s after the box came up, {under} with her cut or under it{(first!=""?": "+first:"")}");
            Assert.Greater(read,20,"fixture: the radio line never held its box up long enough to read");
            Assert.AreEqual(0,under,"Sela was held where the talk's box covers her, or cut by the frame: "+first);
        }
        // ================================================================== a dungeon walked end to end on foot
        /// <summary>The whole suite of November 22 (nov22b) left Sela 57 m behind on QaPlanWorldTests' walk of the Frozen Host, at (50.45, -3.35, 50.41), where she had come through
        /// every earlier day's: the same walk here (the longest way the floor holds from the arrival to a prompt, inside 140 m, walked by the lead's own motor at full stick, every fight
        /// cleared and every seal open), with where she is against him written every quarter second, and the same measure: within 8 m of him six seconds after he stops, never 18 m off
        /// on the way.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator InTheFrozenHostSheKeepsUpOnTheLongestWalk()=>Walk("FrozenHost");
        IEnumerator Walk(string zone)
        {
            DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.4f);
            foreach(var fight in Object.FindObjectsByType<Lattice.World.EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None))GameServices.Current.Flags.SetBool("clear."+fight.encounterId,true);
            foreach(var pulse in Object.FindObjectsByType<Lattice.World.PressurePulse>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(pulse.vent!=null)pulse.enabled=false;
            foreach(var seal in Object.FindObjectsByType<Lattice.World.Membrane>(FindObjectsInactive.Include,FindObjectsSortMode.None))seal.SetOpen(true);
            foreach(var crossing in Object.FindObjectsByType<Lattice.World.DiscoveryPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.startsOnCrossing&&(string.IsNullOrEmpty(p.requiredFlag)||GameServices.Current.Flags.GetBool(p.requiredFlag))))
                GameServices.Current.Flags.SetBool(crossing.flag,true);
            yield return new WaitForSecondsRealtime(.6f);
            var party=PartyController.Current;var lead=party.Active;var partner=party.members.First(m=>m!=lead);partner.GetComponent<PartnerBrain>().enabled=true;
            // The longest way the floor holds from the arrival to any prompt, inside 140 m (QaPlanWorldTests.LongestWalk).
            var spawn=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).First(s=>s.id=="Arrival");
            Assert.IsTrue(NavMesh.SamplePosition(spawn.transform.position,out var start,3,NavMesh.AllAreas),"fixture: walkable floor at the arrival");
            Vector3[] route=new Vector3[0];float longest=0;var path=new NavMeshPath();
            foreach(var prompt in Object.FindObjectsByType<InteractionPrompt>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!prompt.gameObject.activeInHierarchy||prompt.range<=0)continue;
                if(!NavMesh.SamplePosition(prompt.transform.position,out var near,prompt.range+1.5f,NavMesh.AllAreas))continue;
                if(!NavMesh.CalculatePath(start.position,near.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                float length=0;for(int i=1;i<path.corners.Length;i++)length+=Vector3.Distance(path.corners[i-1],path.corners[i]);
                if(length<=140&&length>longest){longest=length;route=path.corners;}
            }
            Assert.Greater(route.Length,1,"fixture: "+zone+" has a walk to take");
            var trail=new System.Text.StringBuilder();float note=0,began=Time.unscaledTime,widest=0;int corner=1;deadline=Time.unscaledTime+45;
            var brain=partner.GetComponent<PartnerBrain>();var hurried=typeof(PartnerBrain).GetField("hurrying",BindingFlags.Instance|BindingFlags.NonPublic);
            var cornersOf=typeof(PartnerBrain).GetField("corners",BindingFlags.Instance|BindingFlags.NonPublic);
            void Note(string what)
            {
                var c=cornersOf.GetValue(brain) as Vector3[];var p=partner.transform.position;var q=lead.transform.position;
                trail.Append($" {Time.unscaledTime-began:0.00}s {what} lead ({q.x:0.0},{q.y:0.0},{q.z:0.0}) {partner.character} ({p.x:0.0},{p.y:0.0},{p.z:0.0}) {partner.State} {Planar(partner.motor.Velocity).magnitude:0.0}m/s hurry={hurried.GetValue(brain)} path={(c==null?"none":c.Length.ToString())} {Planar(p-q).magnitude:0.0}m;");
            }
            while(corner<route.Length&&Time.unscaledTime<deadline)
            {
                var gap=Planar(route[corner]-lead.transform.position);
                if(gap.magnitude<.7f){corner++;continue;}
                var way=gap.normalized;lead.motor.Move(new Vector2(way.x,way.z),false,false);
                widest=Mathf.Max(widest,Planar(lead.transform.position-partner.transform.position).magnitude);
                if(Time.unscaledTime-began>=note){note=Time.unscaledTime-began+.25f;Note("walk");}
                yield return null;
            }
            Assert.AreEqual(route.Length,corner,$"fixture: the lead never finished the walk, stopped at {lead.transform.position}");
            lead.motor.Move(Vector2.zero,false,false);
            deadline=Time.unscaledTime+6;while(Planar(lead.transform.position-partner.transform.position).magnitude>8&&Time.unscaledTime<deadline){if(Time.unscaledTime-began>=note){note=Time.unscaledTime-began+.25f;Note("still");}yield return null;}
            float left=Planar(lead.transform.position-partner.transform.position).magnitude;
            TestContext.WriteLine($"WALK {zone}: {route.Length} corners {longest:0} m, left {left:0.0} m behind, {widest:0.0} m at the widest\n"+trail);
            Assert.LessOrEqual(left,8,$"{zone}: after a walk of the zone the companion is left {left:0} m behind, at {partner.transform.position}");
            Assert.Less(widest,18,$"{zone}: on the way the companion falls {widest:0} m behind");
        }
    }
}
