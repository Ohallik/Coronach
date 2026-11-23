using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The partner's ship that stopped for good (the whole-game capture of November 20, nov20-whole): in Xylos' nearspace Sela sat beside barge two's loose pane from the second
    /// round of its skimmers to the end of the zone (s17, about 6,700 frames, her hull never touched), and in Nacre Grave Idra sat by the inner tows from the end of their fight to the end of
    /// the zone (s54, about 3,100 frames). Each is played here through the replay driver's own pad, the partner's brain running as it does in play, and the partner has to keep with the lead:
    /// never further than <see cref="Far"/> from him for longer than <see cref="FarMost"/>, and beside him a few seconds after he stops. Every second of every play is written
    /// (PARTNER_SAMPLE), and the first time she sits still away from him, what her steering reads from where she sits (PARTNER_STALL).</summary>
    public sealed class PartnerFlightRejoinTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;bool finished;readonly List<string> log=new();readonly List<string> errors=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Heeded={"QUALITY_FLIES","QUALITY_COVER","QUALITY_TOWARD","ENCOUNTER_","WALL_CONTACT","BARGE_","DEFEAT","SWAP_OK","TETHER_","WRECK_STRUCK"};
        float began;
        void Logged(string text,string stack,LogType type)
        {
            if(type==LogType.Exception||type==LogType.Error){errors.Add(text+" "+(stack??"").Split('\n').FirstOrDefault());return;}
            foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add($"{Time.unscaledTime-began:0.0} {text}");return;}
        }
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            fixture=new GameObject("Partner rejoin fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
            log.Clear();errors.Clear();finished=false;began=Time.unscaledTime;Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(fixture!=null)Object.Destroy(fixture);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the driver, by its private parts (as BargeSkimmerReplayTests drives it)
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        List<string> Failures=>Get<List<string>>("failures");
        IEnumerator Play(QualityRoute route,float most,System.Action each=null,System.Func<bool> enough=null)
        {
            if(Get<Gamepad>("pad")==null)Set("pad",InputSystem.AddDevice<Gamepad>("PartnerRejoinPad"));
            Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);var clock=Get<System.Diagnostics.Stopwatch>("clock");if(!clock.IsRunning)clock.Start();
            var run=(IEnumerator)Call("RunSteps");finished=false;Failures.Clear();
            for(float end=Time.unscaledTime+most;Time.unscaledTime<end;)
            {
                if(enough!=null&&enough())break;
                if(!run.MoveNext()){finished=true;break;}
                each?.Invoke();Call("InputUpdate");yield return null;
            }
            Set("recording",false);Call("InputUpdate");
        }

        // ------------------------------------------------------------------ routes, read
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
        static void FlagsAt(string route,string step,params string[] alsoSet)
        {
            var steps=Read(route).steps;int at=System.Array.FindIndex(steps,s=>s.name==step);Assert.GreaterOrEqual(at,0,$"fixture: {route} has no step '{step}'");
            foreach(string flag in steps.Skip(at).SelectMany(FlagsOf))GameServices.Current.Flags.SetBool(flag,false);
            foreach(string flag in steps.Take(at).SelectMany(FlagsOf).Concat(alsoSet))GameServices.Current.Flags.SetBool(flag,true);
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.FirstOrDefault(m=>m!=Lead);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static void Put(CombatActor actor,Vector3 at,Vector3 facing)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(facing);}
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static bool Closable(Collider c)=>c is BoxCollider||c is SphereCollider||c is CapsuleCollider||c is MeshCollider mesh&&mesh.convex;
        static Vector3 Slot(Vector3 lead,Vector3 facing){facing.y=0;facing.Normalize();return lead-facing*3+Vector3.Cross(Vector3.up,facing)*2;}
        IEnumerator Zone(string loadout,string zone,string leader,string partner)
        {
            DevLoadout.Apply(loadout);
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(1f);
            for(int i=0;i<4&&Lead.character!=leader;i++){PartyController.Current.Swap();yield return Frames(30);}
            Assert.AreEqual(leader,Lead.character,"fixture: "+leader+" leads");Assert.AreEqual(partner,Partner.character,"fixture: "+partner+" is out beside him");
            foreach(var hero in PartyController.Current.members){hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=0;hero.target=null;hero.TargetLocked=false;hero.GetComponent<PlayerBrain>().AutoPilot=false;hero.GetComponent<PartnerBrain>().enabled=hero!=Lead;}
        }

        // ------------------------------------------------------------------ what she reads, and the watch
        static readonly FieldInfo Obstacles=typeof(PartnerBrain).GetField("flightObstacles",Private);
        static string Steering(CombatActor partner)
        {
            var guide=Obstacles?.GetValue(partner.GetComponent<PartnerBrain>());if(guide==null)return "steering=none";
            var fields=guide.GetType().GetFields(Private).Where(f=>f.FieldType==typeof(bool)||f.FieldType==typeof(Vector3)||f.FieldType==typeof(float)||f.FieldType.IsEnum).Select(f=>
            {
                var v=f.GetValue(guide);return f.Name+"="+(v is Vector3 p?$"({p.x:0.00},{p.z:0.00})":v is float n?n.ToString("0.00"):v.ToString());
            });
            return "steering "+string.Join(" ",fields);
        }
        /// <summary>From where she sits: what overlaps the sphere her steering sweeps (her hull and the 0.3 m it keeps), what overlaps her hull, and how far that sphere can go each way.</summary>
        static string Reads(CombatActor partner)
        {
            var cc=partner.GetComponent<CharacterController>();var centre=partner.transform.position+cc.center;float hull=cc.radius;
            string Over(float r)=>string.Join(",",Physics.OverlapSphere(centre,r,~0,QueryTriggerInteraction.Ignore).Where(CombatCover.Opaque).Select(c=>c.name).Distinct());
            var ways=new List<string>();int nought=0;
            for(int i=0;i<16;i++)
            {
                var d=Quaternion.Euler(0,i*22.5f,0)*Vector3.forward;float free=8;string what="";
                foreach(var hit in Physics.SphereCastAll(centre,hull+.3f,d,8,~0,QueryTriggerInteraction.Ignore))if(CombatCover.Opaque(hit.collider)&&hit.distance<free){free=hit.distance;what=hit.collider.name;}
                if(free<=.001f)nought++;ways.Add($"{i*22.5f:0}:{free:0.00}{(what!=""?"["+what+"]":"")}");
            }
            return $"hull={hull:0.00} overSweep=[{Over(hull+.3f)}] overHull=[{Over(hull-.01f)}] waysAtNought={nought}/16 ways={string.Join(" ",ways)}";
        }
        /// <summary>How far from the lead she may be, and for how long: the fixed camera's frame is about 46 m across in flight, the lock reaches 21 m, and a fight is fought within 18.</summary>
        const float Far=24,FarMost=6;
        sealed class Watch
        {
            public float farSince=-1,longestFar,lastSample=-1,stillSince=-1;public Vector3 still;public string farFrom="",stall="",pressed="";public int stalls;
            public float every=1;
            public readonly List<string> lines=new();
        }
        /// <summary>Everything with a body (no triggers) within her hull and this much: what a hull that keeps its speed and goes nowhere is pressed against.</summary>
        static string Touching(CombatActor partner,float more)
        {
            var cc=partner.GetComponent<CharacterController>();var centre=partner.transform.position+cc.center;
            return string.Join(",",Physics.OverlapSphere(centre,cc.radius+more,~0,QueryTriggerInteraction.Ignore).Where(c=>c.transform.root!=partner.transform.root).Select(c=>$"{c.name}<{c.GetType().Name}{(c.GetComponentInParent<Health>()!=null?" health":"")}{(CombatCover.Opaque(c)?" opaque":"")}>"));
        }
        void Sample(Watch w)
        {
            var party=PartyController.Current;if(party==null||party.Active==null)return;var lead=party.Active;var partner=party.members.FirstOrDefault(m=>m!=lead);if(partner==null)return;
            float now=Time.unscaledTime,gap=Flat(lead.transform.position,partner.transform.position);
            bool counts=partner.Health.Alive&&!GameServices.Current.Input.Blocked;
            if(counts&&gap>Far)
            {
                if(w.farSince<0){w.farSince=now;w.farFrom=$"from step {Get<int>("stepIndex")} with her at ({partner.transform.position.x:0.0},{partner.transform.position.z:0.0}) and the lead at ({lead.transform.position.x:0.0},{lead.transform.position.z:0.0})";}
                w.longestFar=Mathf.Max(w.longestFar,now-w.farSince);
            }
            else w.farSince=-1;
            if(w.stillSince<0||(partner.transform.position-w.still).magnitude>.15f){w.still=partner.transform.position;w.stillSince=now;}
            else if(counts&&now-w.stillSince>2&&gap>8&&w.stalls++==0)
                w.stall=$"{now-began:0.0} s step={Get<int>("stepIndex")} she={partner.character} ({partner.transform.position.x:0.00},{partner.transform.position.z:0.00}) {partner.State} gap={gap:0.0} lead ({lead.transform.position.x:0.0},{lead.transform.position.z:0.0}) target={(partner.target!=null?partner.target.id:"none")} {Reads(partner)} {Steering(partner)}";
            if(w.pressed==""&&counts&&partner.motor.Velocity.magnitude>.3f&&now-w.stillSince>1.5f)
                w.pressed=$"{now-began:0.0} s step={Get<int>("stepIndex")} ({partner.transform.position.x:0.00},{partner.transform.position.z:0.00}) v={partner.motor.Velocity.magnitude:0.00} facing=({partner.motor.Facing.x:0.00},{partner.motor.Facing.z:0.00}) flags={partner.GetComponent<CharacterController>().collisionFlags} touching=[{Touching(partner,.8f)}] target={(partner.target!=null?partner.target.id+"@"+partner.target.transform.position.ToString("F1"):"none")} {Steering(partner)}";
            if(now-w.lastSample<w.every)return;w.lastSample=now;
            w.lines.Add($"{now-began:0.00} s step={Get<int>("stepIndex")} lead ({lead.transform.position.x:0.0},{lead.transform.position.z:0.0}) {lead.State} | {partner.character} ({partner.transform.position.x:0.0},{partner.transform.position.z:0.0}) {partner.State} v={partner.motor.Velocity.magnitude:0.0} {partner.Health.integrity:0} gap={gap:0.0} target={(partner.target!=null?partner.target.id:"none")} {Steering(partner)}");
        }
        void Dump(string label,Watch w)
        {
            TestContext.WriteLine($"==== {label}: longest far={w.longestFar:0.0} s ({w.farFrom}) stalls={w.stalls}");
            if(w.stall!="")TestContext.WriteLine("PARTNER_STALL "+w.stall);
            if(w.pressed!="")TestContext.WriteLine("PARTNER_PRESSED "+w.pressed);
            foreach(var line in w.lines)TestContext.WriteLine("PARTNER_SAMPLE "+line);
            foreach(var line in log)TestContext.WriteLine("LOG "+line);
            foreach(var line in errors.Distinct().Take(30))TestContext.WriteLine("ERROR "+line);
            foreach(var f in Failures)TestContext.WriteLine("STEP_FAILED "+f);
            log.Clear();errors.Clear();
        }
        /// <summary>The lead let be once the route is flown (the stick at rest), until she is beside him or six seconds have gone: how far off she is then.</summary>
        IEnumerator Settle(System.Action<float> gap)
        {
            float end=Time.unscaledTime+6;
            while(Time.unscaledTime<end&&Partner!=null&&Flat(Lead.transform.position,Partner.transform.position)>12)yield return null;
            gap(Partner!=null?Flat(Lead.transform.position,Partner.transform.position):float.PositiveInfinity);
        }
        /// <summary>One flown stretch: what it asks of the partner, added to the faults.</summary>
        void Judge(string label,Watch w,float settled,List<string> faults)
        {
            Dump(label,w);
            if(w.longestFar>FarMost)faults.Add($"{label}: the partner was left more than {Far} m from the lead for {w.longestFar:0} s ({w.farFrom}); first stall: {w.stall}");
            if(settled>12)faults.Add($"{label}: six seconds after the lead stopped the partner was {settled:0} m from him; first stall: {w.stall}");
            if(Ui()=="defeat")faults.Add($"{label}: the party fell (the stretch is not a fair test of her)");
        }
        string Ui()=>(string)Call("ObserveUi");

        // ================================================================== Xylos' nearspace, barge two (route 17)
        const string Route17="chapter-17-xylos-nearspace";
        IEnumerator Nearspace(string step,params string[] alsoSet)
        {
            FlagsAt(Route17,step,alsoSet.Concat(new[]{"xylos.charted"}).ToArray());State.questSteps["SeedRoad"]=1;
            yield return Zone("chapter3","XylosNearspace","Taren","Sela");
        }
        /// <summary>Where the capture had her from the barge's second round on (stills 205 to 214 of s17, read through the flight camera at the frames' camera): south-east of the loose pane
        /// "-20 -16", at its low corner. Her hull there is a few centimetres off the pane and the sphere her steering sweeps (her hull and 0.3 m) is in it. Where the lead sat for the round.</summary>
        static readonly Vector3 SeenAtThePane=new(327.3f,1,856.5f),LeadInTheRound=new(319.54f,1,858.82f),LeadHeading=new(.799f,0,.601f);
        /// <summary>Her place beside the pane: from where the stills put her, along the plane, to where her hull is <paramref name="gap"/> off it.</summary>
        static Vector3 BesideThePane(CombatActor partner,Collider pane,float gap)
        {
            var cc=partner.GetComponent<CharacterController>();float hull=cc.radius;
            float Off(Vector3 at){var centre=at+cc.center;return Vector3.Distance(centre,Physics.ClosestPoint(centre,pane,pane.transform.position,pane.transform.rotation))-hull;}
            var near=Physics.ClosestPoint(SeenAtThePane+cc.center,pane,pane.transform.position,pane.transform.rotation);var away=SeenAtThePane-near;away.y=0;away.Normalize();
            float lo=-3,hi=3;for(int i=0;i<40;i++){float mid=(lo+hi)*.5f;if(Off(SeenAtThePane+away*mid)<gap)lo=mid;else hi=mid;}
            return SeenAtThePane+away*hi;
        }
        /// <summary>The capture's state at barge two, staged: the skimmers' fight over, the lead where he sat for it, Sela where the stills had her for the rest of the zone, against the pane. Then
        /// route 17 on from there through the pad: the flank knot, the knots' fight, the barge coming free, the lane past the panes and on to the third swarm. As the capture had it she sits there
        /// (her steering sweeps nothing clear from inside the pane's margin); she has to come with him.</summary>
        [UnityTest,Timeout(900000)] public IEnumerator AtBargeTwoSelaLeftAgainstALoosePaneComesOnWithTheLead()
        {
            yield return Nearspace("beside barge two's flank knot","clear.Barge2_Skimmers");
            var pane=GameObject.Find("Loose pane -20 -16")?.GetComponent<BoxCollider>();Assert.IsNotNull(pane,"fixture: barge two has its loose pane -20 -16");
            var partner=Partner;var brain=partner.GetComponent<PartnerBrain>();brain.enabled=false;Put(Lead,LeadInTheRound,LeadHeading);
            var at=BesideThePane(partner,pane,.06f);Put(partner,at,LeadHeading);yield return Frames(2);
            string reads=Reads(partner);TestContext.WriteLine($"STAGED Sela at ({at.x:0.00},{at.z:0.00}), {Flat(at,SeenAtThePane):0.00} m from where the stills put her: {reads}");
            Assert.Less(Flat(at,SeenAtThePane),1.2f,"fixture: the place beside the pane is not where the stills had her");
            StringAssert.Contains("overHull=[]",reads,"fixture: her hull is inside something");StringAssert.Contains("overSweep=[Loose pane -20 -16]",reads,"fixture: the pane alone is not inside the margin her steering keeps");
            brain.enabled=true;
            var w=new Watch();began=Time.unscaledTime;log.Clear();
            yield return Play(Slice(Route17,"XylosNearspace","beside barge two's flank knot","into the third swarm"),150,()=>Sample(w),()=>Ui()=="defeat");
            float settled=0;yield return Settle(g=>settled=g);
            var faults=new List<string>();Judge("barge two, staged at the pane",w,settled,faults);
            Assert.IsTrue(finished||Ui()=="defeat",$"fixture: the route never finished (step {Get<int>("stepIndex")}): "+string.Join(" | ",Failures));
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
        /// <summary>Route 17 through the pad from the weather mirror, into barge two's skimmers and both its fights, past the panes to the third swarm, twice, as the capture flew it: she has
        /// to keep with him all the way. (Not staged: whether she ends against a pane is the fight's.)</summary>
        [UnityTest,Timeout(1200000)] public IEnumerator BargeTwoIsFlownThroughThePadWithThePartnerKeepingUp()
        {
            var faults=new List<string>();
            for(int n=1;n<=2;n++)
            {
                yield return Nearspace("over the weather mirror");
                var from=new Vector3(224.45f,1,679.68f);var heading=new Vector3(.702f,0,-.712f);Put(Lead,from,heading);Put(Partner,Slot(from,heading),heading);yield return new WaitForSecondsRealtime(.5f);
                var w=new Watch();began=Time.unscaledTime;log.Clear();
                yield return Play(Slice(Route17,"XylosNearspace","over the weather mirror","into the third swarm"),260,()=>Sample(w),()=>Ui()=="defeat");
                float settled=0;yield return Settle(g=>settled=g);
                Judge($"barge two, play {n} (finished={finished}, step {Get<int>("stepIndex")})",w,settled,faults);
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        // ================================================================== Nacre Grave, the inner tows (route 54)
        const string Route54="chapter-54-nacre-grave";
        IEnumerator Grave(string step)
        {
            FlagsAt(Route54,step,"home.tugFound");
            yield return Zone("chapter9","NacreGrave","Taren","Idra");
            State.questSteps["HouseLeaves"]=4;
        }
        /// <summary>Where the capture had the lead at the end of the inner tows' fight (s54, step 36 to 38) and where it had Idra: east of him beside a small wreck (stills 026 and 037).</summary>
        static readonly Vector3 LeadAtTheTows=new(-130.49f,1,-27.17f),LeadAtTheTowsHeading=new(.721f,0,.693f),IdraSeen=new(-118.5f,1,-28.5f);
        /// <summary>Idra held where the capture had her when the tows' fight ended, by the hand of the test (put back where she was every frame, her brain running), while route 54 flies the lead
        /// on round the inner pass through the pad to turn 350, the way the capture flew it: about 150 m round the spiral, with the field's walls between them. Then let go, she has to be beside
        /// him within <see cref="BackWithin"/>, as far as the pad has flown him. Straight at him and round one corner were all her steering had: from beyond the bend both are shut by the walls
        /// and she stopped at the first, for the rest of the zone. (The capture's hold itself, from 53 s at the tows to 60 s, is not reproduced here: <see cref="TheInnerTowsAreFlownThroughThePadWithThePartnerKeepingUp"/>.)</summary>
        [UnityTest,Timeout(900000)] public IEnumerator LeftBehindAtTheInnerTowsIdraComesBackRoundThePassOnHerOwn()
        {
            {
                yield return Grave("round the inner pass, turn 290");
                var partner=Partner;Put(Lead,LeadAtTheTows,LeadAtTheTowsHeading);Put(partner,IdraSeen,new Vector3(-.9f,0,-.4f));yield return Frames(2);
                bool held=true;var w=new Watch();began=Time.unscaledTime;log.Clear();
                yield return Play(Slice(Route54,"NacreGrave","round the inner pass, turn 290","round the inner pass, turn 350"),80,()=>{if(held)Put(partner,IdraSeen,new Vector3(-.9f,0,-.4f));},()=>Ui()=="defeat");
                float left=Flat(Lead.transform.position,partner.transform.position);held=false;
                Assert.Greater(left,60,"fixture: the lead never got far round the pass from her");
                float from=Time.unscaledTime,back=float.PositiveInfinity;
                while(Time.unscaledTime-from<BackWithin+6)
                {
                    Sample(w);
                    if(Flat(Lead.transform.position,partner.transform.position)<=12){back=Time.unscaledTime-from;break;}
                    yield return null;
                }
                string line=$"LEFT_BEHIND left={left:0} m back={(float.IsPositiveInfinity(back)?"never":back.ToString("0.0")+" s")} she ends at ({partner.transform.position.x:0.0},{partner.transform.position.z:0.0}) {partner.State} lead ({Lead.transform.position.x:0.0},{Lead.transform.position.z:0.0}) stall={w.stall}";
                Dump(line,w);
                Assert.LessOrEqual(back,BackWithin,"left behind at the inner tows, Idra never came back round the pass to the lead\n"+line);
            }
        }
        /// <summary>How soon she is beside him from a pass's length behind: at a boost's 22.8 m/s a hundred and fifty metres is under seven seconds.</summary>
        const float BackWithin=12;

        // ================================================================== a regroup: the lead flies up to his partner (route 10)
        /// <summary>Route 10's "regroup beside the measured hulls" (an approachPartner step: the lead flies to her, brake held, to within 3.8 m in 8 s), through the pad, from where the whole-game
        /// capture of November 21 had the pair (nov21-whole s10 to s10c) and from 18 m, his nose toward her so that her place behind it is beyond him. On the evening's steering she went
        /// round him and kept ten metres off as he turned to her, three plays in three (in this fixture, from 18 m in open flight: 6.45 m at the nearest). She holds her ground while he comes.</summary>
        IEnumerator Regroup(string loadout,string zone,Vector3 lead,Vector3 her)
        {
            yield return Zone(loadout,zone,"Taren","Sela");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            var toward=her-lead;toward.y=0;Put(Lead,lead,toward.normalized);Put(Partner,her,toward.normalized);yield return new WaitForSecondsRealtime(.6f);
            var step=new QualityStep{name="regroup beside the measured hulls",seconds=8,approachPartner=true,navigate=true,leftTrigger=1,tolerance=3.8f,expectedScene=zone};
            float nearest=float.PositiveInfinity;began=Time.unscaledTime;
            yield return Play(new QualityRoute{name="regroup",scene=zone,steps=new[]{step}},12,()=>nearest=Mathf.Min(nearest,Flat(Lead.transform.position,Partner.transform.position)));
            TestContext.WriteLine($"REGROUP {zone} from {Flat(lead,her):0} m: finished={finished} nearest={nearest:0.00} she ends at ({Partner.transform.position.x:0.0},{Partner.transform.position.z:0.0}) lead ({Lead.transform.position.x:0.0},{Lead.transform.position.z:0.0}) failures={string.Join(" | ",Failures)}");
            Assert.IsTrue(finished,$"the regroup in {zone} never ended");
            CollectionAssert.IsEmpty(Failures,$"flown at his partner from {Flat(lead,her):0} m in {zone} the lead never came within 3.8 m of her (nearest {nearest:0.00})");
        }
        /// <summary>The regroup as the capture of November 22 had it (nov22-recon10 s10, step 29): Sela held 17 to 18 m off by the last fight for the step's first three seconds (here by the
        /// test's hand), then free, while the lead flies at her at the step's braked pace (2.1 m/s). She comes back to her place by him as a partner does when a fight ends (in the capture, holding her
        /// ground she sat 18 m off and he closed to 7.4 m in the eight seconds; here the old hold broke when his way swung off her, so this fixture does not show it).</summary>
        IEnumerator RegroupAfterAFight(System.Action<bool,float> done)
        {
            yield return Zone("gullet","Gullet_Tunnel","Taren","Sela");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            var lead=new Vector3(3.59f,1,175.79f);var her=new Vector3(-3.13f,1,184.67f);
            Put(Lead,lead,Vector3.forward);Put(Partner,her,Vector3.forward);
            yield return new WaitForSecondsRealtime(.4f);
            var step=new QualityStep{name="regroup beside the measured hulls",seconds=8,approachPartner=true,navigate=true,leftTrigger=1,tolerance=3.8f,expectedScene="Gullet_Tunnel"};
            float nearest=float.PositiveInfinity,freed=-1,off=0;began=Time.unscaledTime;
            yield return Play(new QualityRoute{name="regroup after a fight",scene="Gullet_Tunnel",steps=new[]{step}},12,()=>
            {
                // Held 17.5 m ahead of him for the step's first three seconds, as the capture's last fight held her (by the test's hand, her brain running), then let go.
                if(Time.unscaledTime-began<3){var ahead=Partner.transform.position-Lead.transform.position;ahead.y=0;Put(Partner,Lead.transform.position+ahead.normalized*17.5f,Vector3.forward);}
                else if(freed<0){freed=Time.unscaledTime-began;off=Flat(Lead.transform.position,Partner.transform.position);}
                nearest=Mathf.Min(nearest,Flat(Lead.transform.position,Partner.transform.position));
            });
            TestContext.WriteLine($"REGROUP_AFTER_A_FIGHT freed {off:0.0} m off at {freed:0.0} s: finished={finished} nearest={nearest:0.00} she ends at ({Partner.transform.position.x:0.0},{Partner.transform.position.z:0.0}) lead ({Lead.transform.position.x:0.0},{Lead.transform.position.z:0.0}) failures={string.Join(" | ",Failures)}");
            done(Failures.Count==0&&finished,off);
        }
        [UnityTest,Timeout(300000)] public IEnumerator AfterAFightInTheGulletSheComesBackWhileTheLeadFliesUpToHer()
        {
            bool made=false;float off=0;
            yield return RegroupAfterAFight((m,o)=>{made=m;off=o;});
            Assert.Greater(off,16,"fixture: she was not let go 17 to 18 m off as the capture had her");
            Assert.IsTrue(made,"after a fight that held her 17.5 m off, the lead flying up to her at the regroup's pace never came within 3.8 m of her in eight seconds");
        }
        [UnityTest,Timeout(300000)] public IEnumerator InTheGulletTheLeadFliesUpToHisPartnerFromElevenMetres()=>Regroup("gullet","Gullet_Tunnel",new Vector3(3.59f,1,175.79f),new Vector3(-3.13f,1,184.67f));
        [UnityTest,Timeout(300000)] public IEnumerator InTheGulletTheLeadFliesUpToHisPartnerFromEighteenMetres()=>Regroup("gullet","Gullet_Tunnel",new Vector3(3.59f,1,175.79f),new Vector3(3.59f,1,193.79f));
        [UnityTest,Timeout(300000)] public IEnumerator InOpenFlightTheLeadFliesUpToHisPartnerFromElevenMetres()=>Regroup("starter","Arena_Flight",new Vector3(30,1,-17),new Vector3(23.3f,1,-8.2f));
        [UnityTest,Timeout(300000)] public IEnumerator InOpenFlightTheLeadFliesUpToHisPartnerFromEighteenMetres()=>Regroup("starter","Arena_Flight",new Vector3(30,1,-17),new Vector3(30,1,1));

        // ================================================================== the Picket of Teeth, pocket 9 to pocket 10 (route 45)
        const string Route45="chapter-45-picket-of-teeth";
        /// <summary>How far ahead of a lead in flight his way is read.</summary>
        const float ShipLookAhead=14;
        /// <summary>Where both of the scout's plays of November 21 had the pair as leg 9 began (nov21-recon1 s45, s45b): the lead in pocket 8's shadow, Idra four metres to his right.</summary>
        static readonly Vector3 LegNineLead=new(-19.72f,1,423.6f),LegNineHeading=new(-.18f,0,.98f),LegNineIdra=new(-15.96f,1,420.66f);
        /// <summary>Route 45 through the pad from leg 9 to pocket 10, the Picket's buoys and beams as they are, as both of the scout's plays of November 21 flew it. Coming into pocket 9 from
        /// twenty metres back, Idra ran on past the lead ten metres to the north; her place behind his nose, turned down leg 10, was then south of him, and she crept back to it down leg 10's
        /// own line and sat on it as he set off: he pressed against her hull for a second, the driver went round, and leg 10 timed out (twice). The partner never stands on the lead's way: while
        /// he flies a leg she is never ahead of him within both hulls of his line, and every step of the route is made.</summary>
        [UnityTest,Timeout(600000)] public IEnumerator AtThePicketsPocketNineIdraComesInBehindAndIsNeverOnLegTen()
        {
            GameServices.Current.Flags.SetBool("vask.brekkWord",true);GameServices.Current.Flags.SetBool("vask.lockAdmitted",true);GameServices.Current.Flags.SetBool("vask.picketRead",true);
            FlagsAt(Route45,"leg 9 round the rock 1");State.questSteps["Harrowgate"]=2;
            ContinuousReview.ApplyPair(new QualityRoute{name="the Picket",scene="PicketOfTeeth",deployed=new[]{"Taren","Idra"}});
            yield return Zone("chapter8","PicketOfTeeth","Taren","Idra");
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+900;
            var partner=Partner;Put(Lead,LegNineLead,LegNineHeading);Put(partner,LegNineIdra,LegNineHeading);yield return new WaitForSecondsRealtime(.5f);
            var route=Slice(Route45,"PicketOfTeeth","leg 9 round the rock 1","settle into pocket 10");
            float apart=HeroCollision.HullRadius("Taren")+HeroCollision.HullRadius("Idra"),worst=float.PositiveInfinity;string where="";int onHisWay=0;
            var w=new Watch{every=.1f};began=Time.unscaledTime;log.Clear();
            yield return Play(route,90,()=>
            {
                Sample(w);
                var step=Get<QualityStep>("step");if(step==null||!step.name.StartsWith("leg "))return;
                // On his way: ahead of him along his travel, within both hulls of his line (her place behind him, 3.6 m off his middle, is not).
                var travel=Lead.motor.Velocity;travel.y=0;if(travel.sqrMagnitude<2.25f)return;travel.Normalize();
                var off=partner.transform.position-Lead.transform.position;off.y=0;float ahead=Vector3.Dot(off,travel),across=Mathf.Abs(Vector3.Dot(off,Vector3.Cross(Vector3.up,travel)));
                if(ahead<=0||ahead>ShipLookAhead)return;
                if(across<worst){worst=across;where=$"'{step.name}' at {Time.unscaledTime-began:0.0} s: lead ({Lead.transform.position.x:0.00},{Lead.transform.position.z:0.00}) Idra ({partner.transform.position.x:0.00},{partner.transform.position.z:0.00}), {ahead:0.0} m ahead";}
                if(across<apart)onHisWay++;
            },()=>Ui()=="defeat");
            Dump($"the Picket, leg 9 to pocket 10 (finished={finished}, step {Get<int>("stepIndex")}) nearest his line ahead of him {worst:0.00} m ({where}), frames on his way {onHisWay}",w);
            Assert.IsTrue(finished,$"the route from leg 9 never finished (step {Get<int>("stepIndex")})");
            CollectionAssert.IsEmpty(Failures,"a step from leg 9 to pocket 10 failed: "+string.Join(" | ",Failures));
            Assert.AreEqual(0,onHisWay,$"while the lead flew a leg Idra was on his way {onHisWay} frames: ahead of him {worst:0.00} m off his line, inside both hulls ({apart:0.00}) ({where})");
            Assert.IsFalse(log.Any(l=>l.Contains("QUALITY_FLIES_ROUND")&&l.Contains("leg 10")),"the driver went round on leg 10: "+string.Join(" | ",log));
        }
        /// <summary>Route 54 through the pad from the outer pass's quiet side into the inner tows, their fight, and round the inner pass, twice, as the capture flew it.</summary>
        [UnityTest,Timeout(1200000)] public IEnumerator TheInnerTowsAreFlownThroughThePadWithThePartnerKeepingUp()
        {
            var faults=new List<string>();
            for(int n=1;n<=2;n++)
            {
                yield return Grave("along the outer pass's quiet side, turn 230");
                var from=new Vector3(-46.55f,1,-127.9f);var heading=new Vector3(-.761f,0,.649f);Put(Lead,from,heading);Put(Partner,Slot(from,heading),heading);yield return new WaitForSecondsRealtime(.5f);
                if(n==1)
                {
                    // What stands round where the capture had her sit, for the reading of the stills.
                    var eye=IdraSeen+Vector3.up*.8f;
                    foreach(var c in Physics.OverlapSphere(eye,7,~0,QueryTriggerInteraction.Ignore).Where(CombatCover.Opaque))
                    {
                        var p=Closable(c)?Physics.ClosestPoint(eye,c,c.transform.position,c.transform.rotation):c.bounds.ClosestPoint(eye);
                        TestContext.WriteLine($"NEAR_IDRA {c.name} {c.GetType().Name}{(Closable(c)?"":" (bounds)")} at ({c.transform.position.x:0.0},{c.transform.position.z:0.0}) closest ({p.x:0.00},{p.y:0.00},{p.z:0.00}) {Vector3.Distance(p,eye):0.00} m rigidbody={(c.attachedRigidbody!=null)}");
                    }
                }
                var w=new Watch();began=Time.unscaledTime;log.Clear();
                yield return Play(Slice(Route54,"NacreGrave","along the outer pass's quiet side, turn 230","round the inner pass, turn 380"),200,()=>Sample(w),()=>Ui()=="defeat");
                float settled=0;yield return Settle(g=>settled=g);
                Judge($"inner tows, play {n} (finished={finished}, step {Get<int>("stepIndex")})",w,settled,faults);
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
    }
}
