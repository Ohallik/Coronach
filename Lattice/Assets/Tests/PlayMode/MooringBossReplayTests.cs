using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 10's first two bosses played through the replay driver's own pad with their routes' own steps (docs/quality/routes chapter-63 and chapter-65, read from the files
    /// tools/mooring_routes.py writes), in the zones as built, from each door to its fall: Mother of Doors from the east aperture (her checkpoint) through her four clamps, and the Tollkeeper
    /// from chain D's gap (its checkpoint) through its three braces, docks and stages. The party is the chapter's (DevLoadout chapter10, Taren and Idra out) at each sheet's Sync, whole, with
    /// the save's flags as the routes before set them. Each fight is first played the way the driver played it before its hands for that boss (the broken state, which must fail the way the
    /// player's run did), then with them: it must fall inside its sheet's bounds through the pad, at no less than nine tenths of the share its sheet carries (of the same pair's flat out on
    /// the boss in its yard, measured in the same run), and the party must not fall. Every line of the fight is written out (MOORING_PLAY) for its own account.</summary>
    public sealed class MooringBossReplayTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;bool finished;readonly List<string> log=new();readonly List<string> errors=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Heeded={"MOTHER_","TOLL_","CLAMP_","CLAMPS_","TENDED","QUALITY_BOSS","QUALITY_EXTRA","QUALITY_ITEM","QUALITY_BERTH_GOAL","QUALITY_CHASE","QUALITY_TOWARD",
            "QUALITY_UNSTRUCK","QUALITY_MOTHER","QUALITY_TOLL","QUALITY_LOCK","BERTH_","ENCOUNTER_","COMBAT_KILL","BREAK_OK","WALL_CONTACT","CURTAIN_BURST","REVIVE","DEFEAT","SWAP_OK"};
        void Logged(string text,string stack,LogType type)
        {
            if(type==LogType.Exception||type==LogType.Error){errors.Add(text+" "+(stack??"").Split('\n').FirstOrDefault());return;}
            foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add($"{Time.unscaledTime-began:0.0} {text}");return;}
        }
        float began;
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("chapter10");
            fixture=new GameObject("Mooring boss replay fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
            log.Clear();errors.Clear();finished=false;began=Time.unscaledTime;Application.logMessageReceived+=Logged;HandsOff(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;HandsOff(false);
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(fixture!=null)Object.Destroy(fixture);
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            BridleClamp.Slack=false;MotherStrain.Straining=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the driver, by its private parts (as HomeReplayHandsTests drives it)
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        List<string> Failures=>Get<List<string>>("failures");
        string Ui()=>(string)Call("ObserveUi");
        GamepadState Held()=>Get<GamepadState>("held");
        /// <summary>The driver without its hands for these two bosses (ContinuousReview.Mother.cs, ContinuousReview.Tollkeeper.cs), for a case's broken state: put back by every case and the tear-down.</summary>
        static void HandsOff(bool off){foreach(string name in new[]{"motherHandsOff","tollHandsOff"})typeof(ContinuousReview).GetField(name,BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,off);}
        IEnumerator Play(QualityRoute route,float most,System.Action each=null,System.Func<bool> enough=null)
        {
            if(Get<Gamepad>("pad")==null)Set("pad",InputSystem.AddDevice<Gamepad>("MooringBossPad"));
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

        // ------------------------------------------------------------------ the routes, read
        static string Folder=>System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
        static QualityRoute Read(string name)=>JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(System.IO.Path.Combine(Folder,name+".json")));
        const string MotherRoute="chapter-63-mother-of-doors",TollRoute="chapter-65-the-tollkeeper";
        const string MotherDoor="up the east aperture to her door",TollDoor="through chain D's gap into its lane";
        /// <summary>Every flag chapter 10's routes have asserted or waited on before this step of this route: the save the route resumes from has them.</summary>
        static string[] FlagsBefore(string route,string step)
        {
            var flags=new HashSet<string>();
            foreach(string name in System.IO.Directory.GetFiles(Folder,"chapter-*.json").Select(System.IO.Path.GetFileNameWithoutExtension).Where(n=>int.TryParse(n.Split('-')[1],out int k)&&k>=62&&k<=69).OrderBy(n=>n,System.StringComparer.Ordinal))
            {
                foreach(var s in Read(name).steps)
                {
                    if(name==route&&s.name==step)return flags.ToArray();
                    if(!string.IsNullOrEmpty(s.expectedFlag))flags.Add(s.expectedFlag);
                    if(!string.IsNullOrEmpty(s.until)&&s.until.StartsWith("flag:"))flags.Add(s.until.Substring(5));
                }
                if(name==route)break;
            }
            Assert.Fail($"fixture: {route} has no step '{step}'");return null;
        }
        /// <summary>The route as written, from the named step to the one named last (or its end), with the route's own hands (autoHeal, useExtra, its pair).</summary>
        static QualityRoute Slice(string route,string from,string to=null)
        {
            var whole=Read(route);int a=System.Array.FindIndex(whole.steps,s=>s.name==from);Assert.GreaterOrEqual(a,0,$"fixture: {route} has no step '{from}'");
            int b=to==null?whole.steps.Length-1:System.Array.FindIndex(whole.steps,a,s=>s.name==to);Assert.GreaterOrEqual(b,a,$"fixture: {route} has no step '{to}' after '{from}'");
            return new QualityRoute{name=whole.name+" from "+from,scene=whole.steps[a].expectedScene,autoHeal=whole.autoHeal,useExtra=whole.useExtra,deployed=whole.deployed,
                steps=whole.steps.Skip(a).Take(b-a+1).ToArray()};
        }

        // ------------------------------------------------------------------ fixtures
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static GameState State=>GameServices.Current.State;
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static void Place(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();bool was=body.enabled;body.enabled=false;
            if(actor.motor is FlightMotor ship)ship.Halt();else if(actor.motor is GroundMotor ground)ground.Halt();
            actor.transform.position=at;body.enabled=was;Physics.SyncTransforms();
        }
        /// <summary>The pair out (Taren leading, Idra beside him) at a Sync, whole, every skill at its last mastery, as the chain's own party arrives here (the save route 63 played from has
        /// its main skills at mastery 5). Left to the fixture, mastery climbed through a run's cases, and the same pair measured 1,067 flat out on the Tollkeeper first and 1,662 fourth.</summary>
        static IEnumerator Pair(int sync)
        {
            foreach(var member in State.party)
            {
                member.level=sync;member.integrity=Levels.MaxIntegrity(sync);member.charge=100;member.skillUses??=new Dictionary<string,int>();
                foreach(var skill in GameCatalog.All<SkillDef>())if(skill!=null&&skill.id.StartsWith(member.id+"_"))member.skillUses[skill.id]=SkillBook.MasteryUses[SkillBook.MasteryUses.Length-1];
            }
            Assert.IsTrue(Deployment.Set(State,new[]{"Taren","Idra"}),"fixture: Taren and Idra can be put out");
            PartyField.Reform(PartyController.Current);yield return Frames(3);PartyController.Current.RefreshStats();yield return null;
            if(Lead.character!="Taren")PartyController.Current.Swap();yield return Frames(3);
            foreach(var hero in PartyController.Current.members){hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=0;}
        }
        /// <summary>A zone as the route's save has it: every flag the routes set before the step, loaded at the spawn the save names, and the fight's own flags (what a case before set) cleared.</summary>
        IEnumerator Zone(string zone,string spawn,string[] flags,string[] cleared,int sync)
        {
            foreach(string flag in cleared)GameServices.Current.Flags.SetBool(flag,false);
            foreach(string flag in flags)GameServices.Current.Flags.SetBool(flag,true);
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(1f);yield return Pair(sync);yield return new WaitForSecondsRealtime(.5f);
        }
        static readonly string[] MotherFlags={"bossdown.MotherOfDoors","end.motherFreed","end.clamp1","end.clamp2","end.clamp3","end.clamp4","clear.Sorrel_Clamp1","clear.Sorrel_Clamp2","clear.Sorrel_Clamp3","clear.Sorrel_Clamp4","clear.MotherOfDoors"};
        static readonly string[] TollFlags={"bossdown.Tollkeeper","end.gateOpen","end.motorStopped","end.motor1","end.motor2","end.brace1","end.brace2","end.brace3","clear.Tollkeeper",
            "end.toll.brace1","end.toll.brace2","end.toll.brace3","end.toll.brace4","end.toll.brace5","end.toll.brace6"};

        // ------------------------------------------------------------------ flat out (QaPlanCombatTests.FlatOut's measure, the pair the route plays with)
        /// <summary>The pair flat out on the boss standing still in the flight yard (its room has no clamps or cage there, so it fights on integrity as the shared length test measures it): both
        /// ships six metres off, every skill as it comes, else a shot, for 60 s. What the driver's share is a share of.</summary>
        static IEnumerator FlatOut(string id,System.Action<float> measured)
        {
            var sheet=ContentRegistry.Boss(id);
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            foreach(var left in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(left.gameObject);
            yield return new WaitForSecondsRealtime(.6f);
            yield return Pair(sheet.sync);
            var taren=Lead;var idra=PartyController.Current.members.First(m=>m!=taren);
            foreach(var hero in new[]{taren,idra}){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;}
            var at=new Vector3(200,1,0);var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;yield return Frames(3);
            var body=enemy.Health;body.maximum=body.integrity=10000000;body.Holds=null;
            var homes=new[]{at+new Vector3(-2,0,-6),at+new Vector3(2,0,-6)};var heroes=new[]{taren,idra};
            void Home(int i){Place(heroes[i],homes[i]);var face=at-homes[i];face.y=0;if(heroes[i].motor is FlightMotor ship)ship.KeepFacing(face);}
            for(int i=0;i<2;i++){Home(i);heroes[i].target=body;heroes[i].charge=100;}
            yield return null;
            float landed=0;System.Action<Health,DamagePacket,float> count=(_,packet,amount)=>{if(packet.source==taren.Health||packet.source==idra.Health)landed+=amount;};body.Damaged+=count;
            // Sixty seconds, as the shared length test samples (D312): twenty read the same pair anywhere between 1,540 and 1,964 a second on her.
            const float Seconds=60;int drawn=0;
            for(float end=Time.unscaledTime+Seconds;Time.unscaledTime<end;drawn++)
            {
                for(int i=0;i<2;i++)
                {
                    var hero=heroes[i];hero.motor.Move(Vector2.zero,false,false);
                    if(hero.CanAct&&Flat(hero.transform.position,homes[i])>.6f)Home(i);
                    hero.target=body;if(!hero.CanAct)continue;bool acted=false;
                    for(int slot=0;slot<SkillBook.Slots&&!acted;slot++){var skill=SkillBook.Equipped(hero.character,slot);if(skill==null||hero.cooldowns[slot]>0||hero.charge<SkillBook.Cost(hero.character,skill))continue;acted=hero.Skill(slot);}
                    if(!acted)hero.Attack();
                }
                yield return null;
            }
            body.Damaged-=count;Assert.Greater(landed,0,"fixture: the pair is hitting "+id);
            TestContext.WriteLine($"MOORING_FLAT_OUT {id} sync={sheet.sync} rate={landed/Seconds:0}/s frames={drawn/Seconds:0}/s");
            if(drawn/Seconds<40)Assert.Inconclusive($"{id}: the flat-out measure ran at {drawn/Seconds:0} frames a second, on a machine too busy to say how fast a party is");
            measured(landed/Seconds);
            foreach(var hero in heroes){hero.GetComponent<PlayerBrain>().AutoPilot=false;hero.GetComponent<PartnerBrain>().enabled=true;}
            enemy.Passive=true;Object.Destroy(enemy.gameObject);yield return Frames(3);
        }

        // ------------------------------------------------------------------ what a fight is like, frame by frame
        /// <summary>The record of one fight played through the pad: when it woke and fell, what of the boss's struck whom, who fell, and how the lead spent the fight.</summary>
        sealed class Account
        {
            public string boss;public float woke=-1,fell=-1,lastSample;public BossController her;public string move="(none)";
            public readonly Dictionary<string,int> hits=new();public readonly Dictionary<string,float> taken=new();public int leadFalls,partnerFalls,defeats;
            public float frames,flying,onFoot,attacking,dodging,holding;public readonly HashSet<Health> hooked=new();public readonly Dictionary<CombatActor,bool> alive=new();
            public readonly List<string> lines=new();
        }
        static Health Body(string id)=>Health.All.FirstOrDefault(h=>h!=null&&!h.friendly&&h.id==id);
        void Watch(Account a)
        {
            float now=Time.unscaledTime;var party=PartyController.Current;if(party==null||party.members==null)return;
            var body=Body(a.boss);
            if(body!=null&&a.her==null)a.her=body.GetComponent<BossController>();
            if(a.her!=null&&a.woke<0&&a.her.Engaged)a.woke=now;
            if(a.woke>=0&&a.fell<0&&Flag("bossdown."+a.boss))a.fell=now;
            foreach(var member in party.members)
            {
                if(member==null)continue;
                if(a.hooked.Add(member.Health)){var who=member.character;member.Health.Damaged+=(h,packet,amount)=>{if(amount<=0||a.fell>=0||a.woke<0)return;bool boss=packet.source!=null&&packet.source.id==a.boss;
                    string key=who+" "+(boss?a.move:(packet.source!=null?packet.source.id:"the place"))+(string.IsNullOrEmpty(packet.tag)?"":"/"+packet.tag);a.hits[key]=(a.hits.TryGetValue(key,out int n)?n:0)+1;
                    a.taken[who]=(a.taken.TryGetValue(who,out float t)?t:0)+amount/Mathf.Max(1,h.maximum);};}
                bool up=member.Health.Alive;if(a.alive.TryGetValue(member,out bool was)&&was&&!up&&a.woke>=0&&a.fell<0){if(member==party.Active)a.leadFalls++;else a.partnerFalls++;a.lines.Add($"{now-a.woke:0.0} {member.character} falls ({a.move})");}
                a.alive[member]=up;
            }
            if(a.woke<0||a.fell>=0)return;
            float dt=Time.unscaledDeltaTime;a.frames+=dt;var lead=party.Active;if(lead==null)return;
            if(lead.flight)a.flying+=dt;else a.onFoot+=dt;
            if(lead.State==ActorState.Attack||lead.State==ActorState.Skill)a.attacking+=dt;if(lead.State==ActorState.Dodge)a.dodging+=dt;
            foreach(var clamp in BridleClamp.All)if(clamp!=null&&clamp.Holder==lead){a.holding+=dt;break;}
            if(now-a.lastSample>=5)
            {
                a.lastSample=now;var partner=party.members.FirstOrDefault(m=>m!=lead);int si=Get<int>("stepIndex");var step=Get<QualityStep>("step");
                string line=$"{now-a.woke:0} s bar={(body!=null?body.integrity/body.maximum:-1):P0} '{(a.her!=null?a.her.StatusText:"")}' lead={lead.character} {lead.Health.integrity/lead.Health.maximum:P0} {lead.State} partner={(partner!=null?partner.Health.integrity/partner.Health.maximum:0):P0} at={lead.transform.position:F1} step={si} '{(step!=null?step.name:"")}'";
                a.lines.Add(line);
            }
        }
        void Report(Account a,float flat,float integrity)
        {
            float seconds=a.fell>0?a.fell-a.woke:Time.unscaledTime-a.woke;
            var body=Body(a.boss);float left=a.fell>0?0:body!=null?body.integrity/body.maximum:-1;
            float rate=integrity*(1-Mathf.Max(0,left))/Mathf.Max(1,seconds);float frames=Mathf.Max(1,a.frames);
            TestContext.WriteLine($"MOORING_PLAY {a.boss} fell={a.fell>0} seconds={seconds:0.0} left={left:P0} rate={rate:0}/s flatOut={flat:0}/s share={(flat>0?rate/flat:0):0.000} leadFalls={a.leadFalls} partnerFalls={a.partnerFalls} " +
                $"flying={a.flying/frames:P0} onFoot={a.onFoot/frames:P0} attacking={a.attacking/frames:P0} dodging={a.dodging/frames:P0} holding={a.holding/frames:P0}");
            TestContext.WriteLine("MOORING_TAKEN "+string.Join(", ",a.taken.Select(p=>$"{p.Key} {p.Value:P0}")));
            TestContext.WriteLine("MOORING_HITS "+string.Join(", ",a.hits.OrderByDescending(p=>p.Value).Select(p=>$"{p.Key} x{p.Value}")));
            foreach(var line in a.lines)TestContext.WriteLine("MOORING_TIMELINE "+line);
        }
        void Dump(string label)
        {
            TestContext.WriteLine("==== "+label);
            foreach(var line in log)TestContext.WriteLine("LOG "+line);
            foreach(var line in errors.Distinct().Take(30))TestContext.WriteLine("ERROR "+line);
            foreach(var f in Failures)TestContext.WriteLine("STEP_FAILED "+f);
            log.Clear();errors.Clear();
        }
        System.Action Moves(Account a)
        {
            System.Action<BossController,string> moved=(who,move)=>{if(who!=null&&who.Health!=null&&who.Health.id==a.boss)a.move=move;};
            BossController.MoveStarted+=moved;return ()=>BossController.MoveStarted-=moved;
        }
        /// <summary>A fight played from its door through the pad, with its account.</summary>
        IEnumerator Fight(string id,QualityRoute route,string zone,string spawn,string[] flags,string[] cleared,float most,System.Func<bool> enough,System.Action<Account> done,System.Action ready=null)
        {
            yield return Zone(zone,spawn,flags,cleared,ContentRegistry.Boss(id).sync);ready?.Invoke();
            var a=new Account{boss=id};var unhook=Moves(a);began=Time.unscaledTime;
            yield return Play(route,most,()=>Watch(a),()=>Ui()=="defeat"||enough!=null&&enough());
            unhook();a.defeats=Ui()=="defeat"?1:0;done(a);
        }
        /// <summary>A fight against its sheet: it falls inside the sheet's bounds through the pad, at no less than nine tenths of the share the sheet carries, and the party stands throughout.</summary>
        static void Judge(Account a,float flat,string id)
        {
            var sheet=ContentRegistry.Boss(id);float integrity=GameCatalog.Find<EnemyDef>(id).integrity;
            Assert.Greater(a.woke,0,$"{id}'s fight never began");
            Assert.AreEqual(0,a.defeats,$"the party fell to {id}");
            Assert.Greater(a.fell,0,$"{id} never fell through the pad");
            float seconds=a.fell-a.woke,share=integrity/seconds/flat;
            Assert.That(seconds,Is.InRange(sheet.minSeconds,sheet.maxSeconds),$"{id} lasted {seconds:0} s through the pad: its sheet's bounds are {sheet.minSeconds:0} to {sheet.maxSeconds:0}");
            Assert.GreaterOrEqual(share,sheet.driverShare*.9f,$"the driver got {share:0.00} of flat out against {id}, under the sheet's {sheet.driverShare:0.00}");
            Assert.AreEqual(0,a.leadFalls,$"the lead fell {a.leadFalls} times to {id}");
            Assert.AreEqual(0,a.partnerFalls,$"the partner fell {a.partnerFalls} times to {id}");
        }

        // ================================================================== Mother of Doors (route 63)
        /// <summary>Route 63 from her door (the flight up the east aperture from her checkpoint, which a retry starts from too) to her release: four times the clamp's guns flown down, the
        /// dock, the Clamp Tender stopped and the clamp opened on foot, the launch (the fourth shelf on her strain). Played first as the driver was before her hands (the player's run, nov14-recon32
        /// s63: docked at the west clamp's works, it stood against the works for five rounds with the clamp's prompt offered and never took its weight), which must leave the first clamp shut.</summary>
        [UnityTest,Timeout(2700000)] public IEnumerator MotherOfDoorsIsFreedThroughThePadFromHerDoorToHerRelease()
        {
            const string id="MotherOfDoors";float flat=0;yield return FlatOut(id,r=>flat=r);
            var flags=FlagsBefore(MotherRoute,MotherDoor);
            // The driver as it was: from her door to the first clamp's last round.
            HandsOff(true);Account broken=null;
            yield return Fight(id,Slice(MotherRoute,MotherDoor,"clamp 1 is open"),"SorrelSecondSky","Clamps",flags,MotherFlags,420,()=>Flag("end.clamp1"),r=>broken=r);
            HandsOff(false);Report(broken,flat,GameCatalog.Find<EnemyDef>(id).integrity);Dump("without the hands");
            Assert.IsTrue(Flag("clear.Sorrel_Clamp1")||broken.defeats>0,"fixture: without the hands the first clamp's guns were never flown down: the check below says nothing of the clamp");
            Assert.IsFalse(Flag("end.clamp1"),"fixture: without the hands the driver opened the first clamp all the same: the check below proves nothing");
            // With them: from her door to her release.
            Account a=null;
            yield return Fight(id,Slice(MotherRoute,MotherDoor,"she lets go"),"SorrelSecondSky","Clamps",flags,MotherFlags,900,null,r=>a=r);
            Report(a,flat,GameCatalog.Find<EnemyDef>(id).integrity);Dump("with the hands");
            Judge(a,flat,id);
            Assert.IsTrue(finished,"route 63 never reached her release from her door");
            CollectionAssert.IsEmpty(Failures,"route 63 failed a step from her door on: "+string.Join(" | ",Failures));
        }

        /// <summary>Her wave is a band filling at her coil's edge that then runs out at 12 m/s across every lane. The companion read only markings already over her, which a running band
        /// is only the frame it strikes: in the player's run she took every wave of the fight (nov14-recon32 s63, a seventh of her bar each). Built first as the band was marked before
        /// (nothing saying it runs: <see cref="Telegraph.Spreads"/> 0), which must strike her; then as it is, which she reads coming and rolls through toward her.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator HerCompanionReadsHerWaveAsItRunsAndRollsThroughIt()
        {
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+25;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            foreach(var left in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(left.gameObject);
            yield return new WaitForSecondsRealtime(.6f);yield return Pair(27);
            var at=new Vector3(300,1,300);var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MotherOfDoors"),at);enemy.transform.rotation=Quaternion.Euler(0,180,0);enemy.Passive=true;yield return Frames(3);
            var boss=enemy.GetComponent<BossController>();var lead=Lead;var partner=PartyController.Current.members.First(m=>m!=lead);
            foreach(var hero in PartyController.Current.members){hero.Health.maximum=hero.Health.integrity=9000;hero.Health.InvulnerableUntil=0;}
            int struck=0;partner.Health.Damaged+=(h,packet,amount)=>{if(amount>0&&packet.source==enemy.Health)struck++;};
            IEnumerator Wave(bool asItWas)
            {
                struck=0;for(float end=Time.unscaledTime+8;boss.Busy&&Time.unscaledTime<end;)yield return null;
                Place(lead,at+Vector3.back*30);Place(partner,at+new Vector3(3,0,-31));yield return new WaitForSecondsRealtime(1.2f);
                var before=new HashSet<Telegraph>(Telegraph.Live);Assert.IsTrue(boss.Perform("Wave"),"fixture: her wave");
                for(float end=Time.unscaledTime+8;Time.unscaledTime<end&&(boss.Busy||Telegraph.Live.Any(t=>t!=null&&t.owner==boss&&!t.Released&&!before.Contains(t)));)
                {
                    if(asItWas)foreach(var t in Telegraph.Live)if(t!=null&&t.owner==boss&&!before.Contains(t))t.Spreads=0;
                    lead.motor.Move(Vector2.zero,false,false);yield return null;
                }
                TestContext.WriteLine($"MOORING_WAVE_COMPANION asItWas={asItWas} struck={struck} at={partner.transform.position:F1} r={Flat(partner.transform.position,at):0.0}");
            }
            yield return Wave(true);
            Assert.Greater(struck,0,"fixture: her band marked as it was (nothing saying it runs) never struck the companion: the check below proves nothing");
            yield return Wave(false);
            Assert.AreEqual(0,struck,"her companion took her running wave: she reads a band only once it is over her");
        }

        /// <summary>From her third clamp open the fourth shelf swings on her strain, and stands still whenever the party is on it (wiring 6.1 rule 5). Sorrel's builder declares the
        /// shelf's berth inside its frame, under the root her strain swings, and the strain looked for a berth only on the root: in this class's first run the deck slid ten metres in
        /// eight seconds under the lead's feet and out of his hands' reach (the fourth clamp let go four times). Built first as it was (the strain with no berth to ask), which must move.</summary>
        [UnityTest,Timeout(600000)] public IEnumerator TheFourthShelfStandsStillWhileThePartyStandsOnIt()
        {
            var flags=FlagsBefore(MotherRoute,MotherDoor).Concat(new[]{"end.clamp1","end.clamp2","end.clamp3","clear.Sorrel_Clamp1","clear.Sorrel_Clamp2","clear.Sorrel_Clamp3","clear.Sorrel_Clamp4"}).ToArray();
            float moved=0;
            IEnumerator Docked(bool asItWas)
            {
                yield return Zone("SorrelSecondSky","Clamps",flags,new[]{"bossdown.MotherOfDoors","end.clamp4","end.motherFreed"},27);
                foreach(var hero in PartyController.Current.members){hero.Health.maximum=hero.Health.integrity=9000;}
                var door=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="MotherOfDoors");door.Begin();
                var berth=Berth.Find("Clamp4");Assert.IsNotNull(berth,"fixture: Sorrel has the fourth shelf's berth");
                var strain=berth.GetComponentInParent<MotherStrain>();Assert.IsNotNull(strain,"fixture: the fourth shelf swings on her strain");
                if(asItWas)typeof(MotherStrain).GetField("under",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(strain,null);
                // Swinging: then docked by its pad, where it is.
                for(float end=Time.unscaledTime+10;strain.Clock<2&&Time.unscaledTime<end;)yield return null;
                Assert.Greater(strain.Clock,1,"fixture: three clamps open and her fight begun, the fourth shelf never swung");
                Place(Lead,berth.pad.transform.position);Place(PartyController.Current.members.First(m=>m!=Lead),berth.pad.transform.position+Vector3.right*4.2f);
                yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);
                for(float end=Time.unscaledTime+5;!berth.pad.Available&&Time.unscaledTime<end;){Place(Lead,berth.pad.transform.position);yield return null;}
                Assert.IsTrue(berth.pad.Available,"fixture: the fourth shelf's pad isn't offered");berth.pad.Interact();
                for(float end=Time.unscaledTime+4;(Berth.Current!=berth||Berth.Changing)&&Time.unscaledTime<end;)yield return null;
                Assert.IsTrue(Berth.Current==berth&&!Lead.flight,"fixture: the party is on the fourth shelf");
                var from=berth.transform.position;moved=0;
                for(float end=Time.unscaledTime+8;Time.unscaledTime<end;){moved=Mathf.Max(moved,Flat(berth.transform.position,from));yield return null;}
                TestContext.WriteLine($"MOORING_FOURTH_SHELF asItWas={asItWas} moved={moved:0.00} m in 8 s docked");
            }
            yield return Docked(true);
            Assert.Greater(moved,1,"fixture: built as it was, the fourth shelf stood still with the party on it all the same: the check below proves nothing");
            yield return Docked(false);
            Assert.Less(moved,.05f,$"the fourth shelf moved {moved:0.00} m with the party standing on it");
        }

        /// <summary>On foot the interact button is the attack button. On the Tollkeeper's arm the launch stands 5.1 m from the gate motor and answers from 3 m, and the motor's surge shoves a
        /// hero striking it toward the launch: in this class's final run the lead, staggered to 2.6 m from the launch, pressed his next blow into it and put the party out with the third
        /// stage running, which the route never got back. On a fight's shelf the launch now waits until the lead's hands have been off the fight for a moment (Berth.FightLaunchCalm).
        /// Built first as it was (the berth's calm clock held at never), which must offer the launch to a hero who has just struck.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator OnTheArmABlowAtTheMotorIsNeverALaunch()
        {
            var flags=FlagsBefore(TollRoute,TollDoor).Concat(new[]{"end.toll.brace3","end.brace1"}).ToArray();
            yield return Zone("LastMooring","Gate",flags,TollFlags.Where(f=>f!="end.toll.brace3"&&f!="end.brace1").ToArray(),28);
            foreach(var hero in PartyController.Current.members){hero.Health.maximum=hero.Health.integrity=9000;}
            var door=Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="Tollkeeper");door.Begin();
            BossController boss=null;
            for(float end=Time.unscaledTime+6;(boss==null||!boss.TollArmOut)&&Time.unscaledTime<end;){boss??=Object.FindObjectsByType<BossController>(FindObjectsSortMode.None).FirstOrDefault(b=>b.Health!=null&&b.Health.id=="Tollkeeper");yield return null;}
            Assert.IsTrue(boss!=null&&boss.TollArmOut,"fixture: a brace kept broken, the arm is out for its dock");
            var berth=Berth.Find("Arm");Place(Lead,berth.pad.transform.position);yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);
            Assert.IsTrue(berth.pad.Available,"fixture: the arm's pad isn't offered");berth.pad.Interact();
            for(float end=Time.unscaledTime+4;(Berth.Current!=berth||Berth.Changing)&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(Berth.Current==berth&&!Lead.flight,"fixture: the party is on the arm");
            boss.GetComponent<EnemyBrain>().Passive=true;
            var launch=berth.launch.transform.position;var motor=TollkeeperCage.Current.seat.position;var shoved=launch+Flat3(motor-launch).normalized*2.6f;shoved.y=Lead.transform.position.y;
            Assert.Less(Flat(shoved,launch),berth.launch.range,"fixture: 2.6 m from the launch is inside its reach");
            var calm=typeof(Berth).GetField("fightingAt",BindingFlags.Instance|BindingFlags.NonPublic);
            // A blow at the motor, then the next press 2.6 m from the launch: is the launch what A would answer?
            IEnumerator Struck(bool asItWas,System.Action<bool> offered)
            {
                Place(Lead,shoved);yield return new WaitForSecondsRealtime(1.2f);
                Lead.target=TollkeeperCage.Current.Motor;Lead.Attack();
                for(float end=Time.unscaledTime+.25f;Time.unscaledTime<end;){if(asItWas)calm.SetValue(berth,-99f);yield return null;}
                if(asItWas)calm.SetValue(berth,-99f);
                PromptService.FindInteraction(Lead.transform.position);offered(PromptService.Interaction==berth.launch);
            }
            bool took=false;
            yield return Struck(true,o=>took=o);
            Assert.IsTrue(took,"fixture: built as it was, a hero who has just struck the motor 2.6 m from the launch isn't offered it: the check below proves nothing");
            yield return Struck(false,o=>took=o);
            Assert.IsFalse(took,"a hero who has just struck the gate motor is offered the launch 2.6 m off: his next blow puts the party out");
            // Hands off the fight a moment, at the console: the launch is offered.
            Place(Lead,launch);yield return new WaitForSecondsRealtime(Berth.FightLaunchCalm+.3f);PromptService.FindInteraction(Lead.transform.position);
            Assert.AreEqual(berth.launch,PromptService.Interaction,"standing still at its console, the launch isn't offered");
        }
        static Vector3 Flat3(Vector3 v){v.y=0;return v;}

        /// <summary>Her clamps are opened in order, 1 to 4: each clamp's guns wait on the one before (D292). A later shelf's pad was offered all the same, from the start of her fight (its
        /// guns asleep, nothing awake near it), and an opened shelf's pad after its clamp had gone: docked at either, a party stood on a shelf with nothing to do and no word why (the
        /// driver did, in this class's fifth run). A pad out of turn is offered with words that say which shelf comes first (or next), as a locked warp says what opens it, and takes
        /// nobody; the shelf whose turn it is says "Dock" and docks. Built first as it was (her clamps not held to their order), which must take the party out of turn.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator APadOutOfHerTurnSaysWhichShelfIsNextAndTakesNobody()
        {
            var flags=FlagsBefore(MotherRoute,MotherDoor).Concat(new[]{"clear.Sorrel_Clamp1","clear.Sorrel_Clamp2","clear.Sorrel_Clamp3","clear.Sorrel_Clamp4"}).ToArray();
            string[] cleared={"bossdown.MotherOfDoors","end.clamp1","end.clamp2","end.clamp3","end.clamp4","end.motherFreed"};
            IEnumerator Room(params string[] open)
            {
                yield return Zone("SorrelSecondSky","Clamps",flags.Concat(open).ToArray(),cleared.Except(open).ToArray(),27);
                foreach(var hero in PartyController.Current.members){hero.Health.maximum=hero.Health.integrity=9000;}
                Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Single(e=>e.encounterId=="MotherOfDoors").Begin();
                for(float end=Time.unscaledTime+5;Time.unscaledTime<end&&!Object.FindObjectsByType<BossController>(FindObjectsSortMode.None).Any(b=>b.MotherRoom);)yield return null;
                var her=Object.FindObjectsByType<BossController>(FindObjectsSortMode.None).FirstOrDefault(b=>b.MotherRoom);Assert.IsNotNull(her,"fixture: her fight began at her door");
                her.GetComponent<EnemyBrain>().Passive=true;yield return Frames(3);
            }
            // What the pad of this shelf says to a ship over it, and whether pressing it docks.
            IEnumerator AtPad(string id,System.Action<string,bool> said)
            {
                var berth=Berth.Find(id);Assert.IsNotNull(berth,"fixture: "+id);
                Place(Lead,berth.pad.transform.position);yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.3f);Place(Lead,berth.pad.transform.position);
                PromptService.FindInteraction(Lead.transform.position);var offered=PromptService.Interaction;string words=offered==berth.pad?offered.prompt:"(not offered)";
                if(offered==berth.pad)PromptService.TryInteract();
                for(float end=Time.unscaledTime+1.5f;Berth.Current!=berth&&Time.unscaledTime<end;)yield return null;
                bool docked=Berth.Current==berth;
                for(float end=Time.unscaledTime+2;Berth.Changing&&Time.unscaledTime<end;)yield return null;
                TestContext.WriteLine($"MOORING_TURN {id} says '{words}' docked={docked}");said(words,docked);
            }
            string words=null;bool docked=false;
            // As it was: her clamps not held to their order.
            yield return Room();
            foreach(var clamp in BridleClamp.All)typeof(BridleClamp).GetField("inOrder")?.SetValue(clamp,false);
            yield return AtPad("Clamp2",(w,d)=>{words=w;docked=d;});
            Assert.IsTrue(docked,"fixture: built as it was, the second shelf's pad didn't take the party before the first clamp: the check below proves nothing");
            // As it is: before the first clamp, the second shelf says the first comes first and takes nobody; the first's docks.
            yield return Room();
            yield return AtPad("Clamp2",(w,d)=>{words=w;docked=d;});
            Assert.IsFalse(docked,"the second shelf's pad took the party before the first clamp was open");
            Assert.AreEqual("The west clamp's works first",words,"out of turn, the second shelf's pad doesn't say which shelf comes first");
            yield return AtPad("Clamp1",(w,d)=>{words=w;docked=d;});
            Assert.IsTrue(docked&&words=="Dock — The west clamp's works",$"the first shelf's pad, in its turn, says '{words}' and docked={docked}");
            // With the first clamp open: its shelf says the next, and the second's docks.
            yield return Room("end.clamp1");
            yield return AtPad("Clamp1",(w,d)=>{words=w;docked=d;});
            Assert.IsFalse(docked,"the first shelf's pad took the party with its clamp already open: nothing to do there");
            Assert.AreEqual("The south-west clamp's works next",words,"with its clamp open, the first shelf's pad doesn't say which shelf is next");
            yield return AtPad("Clamp2",(w,d)=>{words=w;docked=d;});
            Assert.IsTrue(docked,"with the first clamp open, the second shelf's pad didn't take the party");
        }

        /// <summary>In flight the companion read incoming shots, and marked areas only a boss had laid: an ordinary enemy's marking (a clamp gun's flak rows, a lunge's lane, a mine's
        /// ring) she never left, and the clamp guns' rows struck Idra four times in every Mother of Doors fight through the pad. A clamp gun in the flight yard, the lead idle inside its
        /// curtain and the companion at her place behind him in its second row. Built first with the gun's rows owned by nothing (no enemy's marking), which must strike her; then as the
        /// game lays them, which she leaves before they burst.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator InFlightTheCompanionLeavesAnOrdinaryEnemysMarkedRowBeforeItBursts()
        {
            SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+25;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            foreach(var left in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(left.gameObject);
            yield return new WaitForSecondsRealtime(.6f);yield return Pair(27);
            var lead=Lead;var partner=PartyController.Current.members.First(m=>m!=lead);lead.GetComponent<PlayerBrain>().AutoPilot=true;
            foreach(var hero in PartyController.Current.members){hero.Health.maximum=hero.Health.integrity=9000;hero.Health.InvulnerableUntil=0;}
            var at=new Vector3(300,1,300);var gun=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ClampGun"),at);gun.transform.rotation=Quaternion.Euler(0,180,0);yield return Frames(3);
            gun.Health.maximum=gun.Health.integrity=10000000;var brain=gun.GetComponent<EnemyBrain>();
            int struck=0;partner.Health.Damaged+=(h,packet,amount)=>{if(amount>0&&packet.source==gun.Health)struck++;};
            IEnumerator Beats(bool asItWas)
            {
                struck=0;brain.Passive=true;Place(lead,at+Vector3.back*6);Place(partner,at+new Vector3(2,0,-9));yield return new WaitForSecondsRealtime(1f);brain.Passive=false;
                for(float end=Time.unscaledTime+14;Time.unscaledTime<end;)
                {
                    if(asItWas)foreach(var t in Telegraph.Live)if(t!=null&&ReferenceEquals(t.owner,brain))t.owner=null;
                    lead.motor.Move(Vector2.zero,false,false);Place(lead,at+Vector3.back*6);yield return null;
                }
                brain.Passive=true;TestContext.WriteLine($"MOORING_CURTAIN_COMPANION asItWas={asItWas} struck={struck} at={partner.transform.position:F1}");
            }
            yield return Beats(true);
            Assert.Greater(struck,0,"fixture: with nobody's marking over her she was never struck by the gun's rows: the check below proves nothing");
            yield return Beats(false);
            Assert.AreEqual(0,struck,"in flight the companion stayed in a clamp gun's marked row as it burst: she reads only a boss's markings");
        }

        // ================================================================== Last Mooring's lane (route 64), and route 65 from where it leaves off
        const string LaneRoute="chapter-64-last-mooring";
        static readonly string[] LaneFlags={"clear.Mooring_Lane1","clear.Mooring_Gap","clear.Mooring_Lane2","clear.Mooring_Island","end.sectionStruck","end.sectionHeld","end.matsHeard"};
        /// <summary>The first step a route plays after its title and its load: where the chain's player takes it up.</summary>
        static string FirstPlayed(string route){var steps=Read(route).steps;return steps[System.Array.FindIndex(steps,s=>s.pickRow=="autosave")+1].name;}
        /// <summary>Chain C's gap is held by two clamp guns on the chain either side of it. As built until November 15 each stood on the chain's own line, its 2 m body wholly inside the
        /// chain's 5 m solid, which stops every shot (the chain is cover): in the player's runs (nov15-recon36 s64, s64b) the lead fired at the west gun for two minutes from 7 m, pressed on
        /// the chain, and nothing reached it; nothing could. Route 64 played through the pad from its first step (Sorrel's crater, the lane east, the warp, chain A, the first pair, the
        /// section struck and Maru's weavers, the west island's cradle, the gap's guns, the second pair) to its end, and route 65 from the save it leaves to the Tollkeeper's door and on into
        /// its fight. Built first with the gap's guns where they stood (on the chain's line), which must leave the gap unbroken.</summary>
        [UnityTest,Timeout(2700000)] public IEnumerator Route64sLaneIsFlownThroughThePadAndRoute65GoesOnFromItsSaveIntoTheTollkeepersFight()
        {
            string start=FirstPlayed(LaneRoute);var flags=FlagsBefore(LaneRoute,start);var cleared=LaneFlags.Concat(TollFlags).ToArray();
            bool asItWas=false;int falls=0;var alive=new Dictionary<CombatActor,bool>();
            void Moved(Scene scene,LoadSceneMode mode)
            {
                if(!asItWas||scene.name!="LastMooring")return;
                foreach(var gap in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))if(gap.encounterId=="Mooring_Gap")foreach(var s in gap.spawners){var p=s.transform.position;p.z=270;s.transform.position=p;}
            }
            void Falls(){var party=PartyController.Current;if(party==null||party.members==null)return;foreach(var m in party.members){if(m==null)continue;bool up=m.Health.Alive;if(alive.TryGetValue(m,out bool was)&&was&&!up)falls++;alive[m]=up;}}
            SceneManager.sceneLoaded+=Moved;
            try
            {
                // As it was: the gap's guns on chain C's line.
                asItWas=true;
                yield return Zone("SorrelSecondSky","Shelf4",flags,cleared,27);
                yield return Play(Slice(LaneRoute,start,"Mooring_Gap cleared"),900,Falls,()=>Ui()=="defeat"||Flag("clear.Mooring_Gap"));
                var guns=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.definition!=null&&e.definition.id=="ClampGun").ToArray();
                TestContext.WriteLine($"MOORING_GAP asItWas=True cleared={Flag("clear.Mooring_Gap")} guns={guns.Length} at {string.Join(" ",guns.Select(g=>g.transform.position.ToString("F1")))} bars {string.Join(" ",guns.Select(g=>(g.Health.integrity/g.Health.maximum).ToString("P0")))}");
                Dump("the gap's guns on the chain's line");
                Assert.Greater(guns.Length,0,"fixture: built as it was, the gap's guns never stood up: the check below says nothing of reaching them");
                Assert.IsFalse(Flag("clear.Mooring_Gap"),"fixture: with the gap's guns inside chain C's solid, the gap was broken all the same: the check below proves nothing");
                asItWas=false;alive.Clear();falls=0;
                // As it is: the whole of route 64.
                yield return Zone("SorrelSecondSky","Shelf4",flags,cleared,27);
                yield return Play(Slice(LaneRoute,start),900,Falls,()=>Ui()=="defeat");
                var lane=Failures.ToList();Dump("route 64");
                Assert.AreNotEqual("defeat",Ui(),"the party fell on route 64's lane");
                Assert.IsTrue(finished,"route 64 never reached its end");
                CollectionAssert.IsEmpty(lane,"route 64 failed a step: "+string.Join(" | ",lane));
                Assert.IsTrue(Flag("clear.Mooring_Gap")&&Flag("clear.Mooring_Lane2"),"route 64 ended without the gap's guns or the second pair down");
                // Route 65 from the save route 64 leaves: loaded where that save stands, with what it holds, played from its first step through the Tollkeeper's door into its fight.
                var save=GameServices.Current.Saves.Load("autosave");Assert.AreEqual("LastMooring",save.zone,"route 64's last save isn't in Last Mooring");
                TestContext.WriteLine($"MOORING_LANE_SAVE zone={save.zone} spawn={save.spawn} falls={falls}");
                SceneFlow.Current.LoadZone(save.zone,save.spawn);float deadline=Time.unscaledTime+40;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
                yield return new WaitForSecondsRealtime(1.5f);
                string tollStart=FirstPlayed(TollRoute);
                yield return Play(Slice(TollRoute,tollStart,"up the middle lane past the dividing chains"),300,Falls,()=>Ui()=="defeat");
                var gate=Failures.ToList();Dump("route 65 from route 64's save");
                Assert.IsTrue(finished,"route 65 never reached the Tollkeeper's lane from route 64's save");
                CollectionAssert.IsEmpty(gate,"route 65 failed a step from route 64's save to its fight: "+string.Join(" | ",gate));
                var toll=Body("Tollkeeper");Assert.IsTrue(toll!=null&&toll.GetComponent<BossController>().Engaged,"through chain D's gap from route 64's save, the Tollkeeper's fight never began");
                Assert.AreEqual(0,falls,$"a hero fell {falls} times on route 64's lane or on the way to the Tollkeeper");
            }
            finally{SceneManager.sceneLoaded-=Moved;}
        }

        // ================================================================== the Tollkeeper (route 65)
        /// <summary>Route 65 from its door (through chain D's gap from its checkpoint, which a retry starts from too) to the third stage stopped: three times a brace broken from flight, the dock on
        /// the arm, the gate motor's stage stopped on foot (the Clamp Tenders stopped first from the second), the launch. Played first with its braces and its motor at the sizes it had until
        /// it was first played this way (9,500 and 11,000: the fight fell in 101 s, under its sheet's floor), which must be under its bounds.</summary>
        [UnityTest,Timeout(2700000)] public IEnumerator TheTollkeepersGateIsStoppedThroughThePadFromItsDoorToTheThirdStage()
        {
            const string id="Tollkeeper";float flat=0;yield return FlatOut(id,r=>flat=r);
            var flags=FlagsBefore(TollRoute,TollDoor);var sheet=ContentRegistry.Boss(id);
            // At the old sizes: a brace and the motor's stages as they were built before.
            Account small=null;
            void OldSizes(){var cage=TollkeeperCage.Current;Assert.IsNotNull(cage,"fixture: Last Mooring stands the Tollkeeper's cage");foreach(var b in cage.braces){b.health.maximum=b.health.integrity=9500;}cage.Motor.maximum=cage.Motor.integrity=11000;}
            yield return Fight(id,Slice(TollRoute,TollDoor,"stage 3 is stopped"),"LastMooring","Gate",flags,TollFlags,900,null,r=>small=r,OldSizes);
            Report(small,flat,GameCatalog.Find<EnemyDef>(id).integrity);Dump("at the old sizes");
            Assert.Greater(small.fell,0,"fixture: at the old sizes the Tollkeeper never fell through the pad: the check below says nothing of its length");
            Assert.Less(small.fell-small.woke,sheet.minSeconds,$"fixture: at the old sizes the fight lasted {small.fell-small.woke:0} s, inside its sheet's bounds: the check below proves nothing");
            Account a=null;
            yield return Fight(id,Slice(TollRoute,TollDoor,"stage 3 is stopped"),"LastMooring","Gate",flags,TollFlags,900,null,r=>a=r);
            Report(a,flat,GameCatalog.Find<EnemyDef>(id).integrity);Dump("with the hands");
            Judge(a,flat,id);
            Assert.IsTrue(finished,"route 65 never reached its third stage from its door");
            CollectionAssert.IsEmpty(Failures,"route 65 failed a step from its door on: "+string.Join(" | ",Failures));
        }
    }
}
