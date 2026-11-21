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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Two bars that were sized on the replay's under-geared party (D312 and the Host Marshal's sheet, November 14), fought again through the driver's own pad with the party the
    /// chapter really arrives with (DevLoadout: the plain part of the chapter's tier in every slot, the floor a player can buy; D257), from each real door in the zone as built, with the
    /// route's own steps read from the files the generators write (chapter-36 and chapter-52). Until November 15 the driver never put on what it earned after chapter 2, and every
    /// time the chains measured from chapter 3 on was a party in Scrap and Refined parts. Each fight's length and its share of flat out are printed (FLOOR_PLAY), the share against
    /// the shared length test's own measure (QaPlanCombatTests.FlatOut: the chapter's loadout, Taren and the hero beside him, 60 s on the boss standing still), which is what a
    /// sheet's driverShare is a share of; the fight must fall inside its sheet's bounds, at no less than nine tenths of the share its sheet carries, with nobody down.</summary>
    public sealed class FloorPartyBossReplayTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;readonly List<string> log=new();readonly List<string> errors=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Heeded={"QUALITY_BOSS","QUALITY_EXTRA","QUALITY_ITEM","QUALITY_CHASE","QUALITY_LOCK","QUALITY_TOWARD","QUALITY_WEARS","COUPLING_","ENCOUNTER_","REVIVE","DEFEAT","SWAP_OK"};
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
            log.Clear();errors.Clear();began=Time.unscaledTime;Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;
            // The muster field is a FightPocket until the duel is won, and the case ends before his yield: let it go while the party stands, or its OnDisable at the unload
            // refreshes heroes the unload has already destroyed (FightPocket.cs OnDisable, PartyController.RefreshStats: a MissingReferenceException in this class's first run).
            foreach(var pocket in Object.FindObjectsByType<FightPocket>(FindObjectsSortMode.None))if(pocket!=null)pocket.enabled=false;
            yield return null;
            if(replay!=null){var pad=Get<Gamepad>("pad");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);Set("pad",null);}
            if(fixture!=null)Object.Destroy(fixture);fixture=null;replay=null;
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the driver, by its private parts (as MooringBossReplayTests drives it)
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        List<string> Failures=>Get<List<string>>("failures");
        string Ui()=>(string)Call("ObserveUi");
        bool finished;
        IEnumerator Play(QualityRoute route,float most,System.Action each=null,System.Func<bool> enough=null)
        {
            fixture=new GameObject("Floor party replay fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
            Set("pad",InputSystem.AddDevice<Gamepad>("FloorPartyPad"));
            Call("InputUpdate");yield return null;yield return null;
            Set("route",route);Set("recording",true);Get<System.Diagnostics.Stopwatch>("clock").Start();
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
        /// <summary>Every flag the chapter's routes (numbered first to last) have asserted or waited on before this step of this route: the save the route resumes from has them.</summary>
        static string[] FlagsBefore(int first,string route,string step)
        {
            var flags=new HashSet<string>();
            foreach(string name in System.IO.Directory.GetFiles(Folder,"chapter-*.json").Select(System.IO.Path.GetFileNameWithoutExtension)
                .Where(n=>int.TryParse(n.Split('-')[1],out int k)&&k>=first&&string.CompareOrdinal(n,route)<=0).OrderBy(n=>n,System.StringComparer.Ordinal))
            {
                foreach(var s in Read(name).steps)
                {
                    if(name==route&&s.name==step)return flags.ToArray();
                    if(!string.IsNullOrEmpty(s.expectedFlag))flags.Add(s.expectedFlag);
                    if(!string.IsNullOrEmpty(s.until)&&s.until.StartsWith("flag:"))flags.Add(s.until.Substring(5));
                }
            }
            Assert.Fail($"fixture: {route} has no step '{step}'");return null;
        }
        /// <summary>The route as written, from the named step to the one named last, with the route's own hands (autoHeal, useExtra, wearBest, its pair).</summary>
        static QualityRoute Slice(string route,string from,string to)
        {
            var whole=Read(route);int a=System.Array.FindIndex(whole.steps,s=>s.name==from);Assert.GreaterOrEqual(a,0,$"fixture: {route} has no step '{from}'");
            int b=System.Array.FindIndex(whole.steps,a,s=>s.name==to);Assert.GreaterOrEqual(b,a,$"fixture: {route} has no step '{to}' after '{from}'");
            return new QualityRoute{name=whole.name+" from "+from,scene=whole.steps[a].expectedScene,autoHeal=whole.autoHeal,useExtra=whole.useExtra,wearBest=whole.wearBest,deployed=whole.deployed,
                steps=whole.steps.Skip(a).Take(b-a+1).ToArray()};
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static CombatActor Lead=>PartyController.Current.Active;
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static bool Flag(string key)=>GameServices.Current.Flags.GetBool(key);
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            Physics.SyncTransforms();
        }
        /// <summary>What every hero wears, by part, for the record: the fight is the floor party's, and says so.</summary>
        static string Wearing()=>string.Join("; ",State.party.Select(m=>m.id+": "+string.Join(",",m.equipped.Select(e=>State.parts.Find(p=>p.instanceId==e.Value)?.definitionId))+(string.IsNullOrEmpty(m.module2)?"":",+"+State.parts.Find(p=>p.instanceId==m.module2)?.definitionId)));

        // ------------------------------------------------------------------ flat out: the shared length test's own measure (QaPlanCombatTests.FlatOut), what a sheet's share is a share of
        const float FlatOutSeconds=60;
        /// <summary>QaPlanCombatTests.MeasuredIn: a boss since chapter 2 is measured against its chapter's loadout.</summary>
        static string MeasuredIn(string id)=>"chapter"+Mathf.Clamp(ContentRegistry.Zone(ContentRegistry.Boss(id).zone).chapter,DevLoadout.FirstChapter,DevLoadout.LastChapter);
        IEnumerator FlatOut(string id,System.Action<float> measured)
        {
            var sheet=ContentRegistry.Boss(id);var at=new Vector3(200,.1f,0);
            DevLoadout.Apply(MeasuredIn(id));
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            foreach(var left in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(left.gameObject);
            foreach(var hero in PartyController.Current.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Floor party fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            foreach(var member in State.party){member.level=sheet.sync;member.skillUses?.Clear();}
            PartyController.Current.RefreshStats();
            if(Lead.character!="Taren")Assert.IsTrue(PartyController.Current.Swap(),"fixture: the swap");
            var taren=Lead;var other=PartyController.Current.members.First(m=>m!=taren);
            foreach(var hero in new[]{taren,other}){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;}
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;yield return Frames(3);var body=enemy.Health;
            body.maximum=body.integrity=10000000;
            float reach=enemy.GetComponent<CharacterController>().radius+.9f;
            bool blade=DevLoadout.Weapon(other.character)==GearSlot.Edge;
            var tarenAt=at+Vector3.back*reach;var otherAt=blade?at+Quaternion.Euler(0,-70,0)*Vector3.back*reach:at+Vector3.back*8+Vector3.right*2;
            Place(taren,tarenAt);Place(other,otherAt);taren.target=other.target=body;taren.charge=other.charge=100;yield return null;
            float landed=0;System.Action<Health,DamagePacket,float> count=(_,packet,amount)=>{if(packet.source==taren.Health||packet.source==other.Health)landed+=amount;};body.Damaged+=count;
            int drawn=0;
            for(float end=Time.unscaledTime+FlatOutSeconds;Time.unscaledTime<end;drawn++)
            {
                foreach(var hero in new[]{taren,other})
                {
                    hero.motor.Move(Vector2.zero,false,false);
                    var home=hero==taren?tarenAt:otherAt;
                    if(hero.CanAct&&Flat(hero.transform.position,home)>.6f)Place(hero,home);
                    hero.target=body;if(!hero.CanAct)continue;bool acted=false;
                    for(int slot=0;slot<SkillBook.Slots&&!acted;slot++){var skill=SkillBook.Equipped(hero.character,slot);if(skill==null||hero.cooldowns[slot]>0||hero.charge<SkillBook.Cost(hero.character,skill))continue;acted=hero.Skill(slot);}
                    if(!acted)hero.Attack();
                }
                yield return null;
            }
            body.Damaged-=count;Assert.Greater(landed,0,"fixture: the pair is hitting "+id);
            float frames=drawn/FlatOutSeconds;
            TestContext.WriteLine($"FLOOR_FLAT_OUT {id} loadout={MeasuredIn(id)} pair={taren.character}+{other.character} sync={sheet.sync} rate={landed/FlatOutSeconds:0}/s frames={frames:0}/s wearing {Wearing()}");
            if(frames<50)Assert.Inconclusive($"{id}: the flat-out measure ran at {frames:0} frames a second, on a machine too busy to say how fast a party is");
            measured(landed/FlatOutSeconds);
            enemy.Passive=true;Object.Destroy(enemy.gameObject);yield return Frames(3);
        }

        // ------------------------------------------------------------------ the fight from its door
        sealed class Account{public string boss;public float woke=-1,fell=-1,lastSample;public BossController her;public int leadFalls,partnerFalls,defeats;public readonly Dictionary<CombatActor,bool> alive=new();public readonly List<string> lines=new();}
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
                if(member==null)continue;bool up=member.Health.Alive;
                if(a.alive.TryGetValue(member,out bool was)&&was&&!up&&a.woke>=0&&a.fell<0){if(member==party.Active)a.leadFalls++;else a.partnerFalls++;a.lines.Add($"{now-a.woke:0.0} {member.character} falls");}
                a.alive[member]=up;
            }
            if(a.woke<0||a.fell>=0||now-a.lastSample<10)return;
            a.lastSample=now;var lead=party.Active;var step=Get<QualityStep>("step");
            a.lines.Add($"{now-a.woke:0} s bar={(body!=null?body.integrity/body.maximum:-1):P0} '{(a.her!=null?a.her.StatusText:"")}' lead={lead.character} {lead.Health.integrity/lead.Health.maximum:P0} {lead.State} step='{(step!=null?step.name:"")}'");
        }
        /// <summary>The chapter's party (its loadout, the pair the route plays with, at the sheet's Sync, whole) at the door's checkpoint, with every flag the routes set before the door step.</summary>
        IEnumerator AtTheDoor(string id,string loadout,string zone,string spawn,string[] flags,string[] pair)
        {
            DevLoadout.Apply(loadout);var sheet=ContentRegistry.Boss(id);
            foreach(string flag in flags)GameServices.Current.Flags.SetBool(flag,true);
            foreach(string flag in new[]{"bossdown."+id,"clear."+sheet.door})GameServices.Current.Flags.SetBool(flag,false);
            foreach(var member in State.party){member.level=sheet.sync;member.integrity=Levels.MaxIntegrity(sheet.sync);member.charge=100;}
            Assert.IsTrue(!Deployment.Choosing(State)||Deployment.Set(State,pair),"fixture: "+string.Join(" and ",pair)+" can be put out");
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(1f);
            PartyController.Current.RefreshStats();
            if(Lead.character!=pair[0])PartyController.Current.Swap();yield return Frames(3);
            Assert.AreEqual(pair[0],Lead.character,"fixture: "+pair[0]+" leads");
            CollectionAssert.AreEquivalent(pair,PartyController.Current.members.Select(m=>m.character).ToArray(),"fixture: the pair out");
            foreach(var hero in PartyController.Current.members){hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=0;}
            yield return new WaitForSecondsRealtime(.5f);
        }
        IEnumerator Fight(string id,string loadout,string zone,string spawn,string[] flags,string[] pair,QualityRoute route,float flat)
        {
            yield return AtTheDoor(id,loadout,zone,spawn,flags,pair);
            string worn=Wearing();var a=new Account{boss=id};began=Time.unscaledTime;
            yield return Play(route,900,()=>Watch(a),()=>Ui()=="defeat"||a.fell>0&&Time.unscaledTime-a.fell>2);
            a.defeats=Ui()=="defeat"?1:0;
            var sheet=ContentRegistry.Boss(id);float integrity=GameCatalog.Find<EnemyDef>(id).integrity;
            float seconds=a.fell>0?a.fell-a.woke:Time.unscaledTime-a.woke;var body=Body(id);float left=a.fell>0?0:body!=null?body.integrity/body.maximum:-1;
            float share=a.fell>0?integrity/(flat*seconds):integrity*(1-Mathf.Max(0,left))/(flat*Mathf.Max(1,seconds));
            float least=sheet.minSeconds,most=sheet.maxSeconds;
            TestContext.WriteLine($"FLOOR_PLAY {id} fell={a.fell>0} seconds={seconds:0.0} left={left:P0} integrity={integrity:0} flatOut={flat:0}/s share={share:0.000} sheetShare={sheet.driverShare:0.00} bounds={least:0}-{most:0}s " +
                $"atSheetShare={integrity/(flat*sheet.driverShare):0}s leadFalls={a.leadFalls} partnerFalls={a.partnerFalls} defeats={a.defeats}");
            TestContext.WriteLine($"FLOOR_PLAY_PARTY {id} {worn}");
            // The integrity that puts this fight, at the share just measured, at the middle of its sheet's bounds: what to set if it falls outside them.
            TestContext.WriteLine($"FLOOR_PLAY_SIZE {id} for {(least+most)/2:0}s at share {share:0.000}: integrity {flat*share*(least+most)/2:0}; for {least:0}s: {flat*share*least:0}; for {most:0}s: {flat*share*most:0}");
            foreach(var line in a.lines)TestContext.WriteLine("FLOOR_TIMELINE "+line);
            foreach(var line in log)TestContext.WriteLine("LOG "+line);
            foreach(var line in errors.Distinct().Take(20))TestContext.WriteLine("ERROR "+line);
            foreach(var f in Failures)TestContext.WriteLine("STEP_FAILED "+f);
            Assert.Greater(a.woke,0,$"{id}'s fight never began from its door");
            Assert.AreEqual(0,a.defeats,$"the party fell to {id}");
            Assert.Greater(a.fell,0,$"{id} never fell through the pad ({left:P0} of its bar left after {seconds:0} s)");
            Assert.That(seconds,Is.InRange(least,most),$"{id} lasted {seconds:0} s through the pad to the chapter's floor party: its sheet's bounds are {least:0} to {most:0}");
            Assert.GreaterOrEqual(share,sheet.driverShare*.9f,$"the driver got {share:0.00} of flat out against {id}, under the sheet's {sheet.driverShare:0.00}");
            Assert.AreEqual(0,a.leadFalls+a.partnerFalls,$"a hero fell {a.leadFalls+a.partnerFalls} times to {id}");
        }

        // ================================================================== the Host Marshal (route 36)
        /// <summary>Route 36 from the Marshal's door (the walk into the bottom gallery, which a retry starts from) to its fall: Taren and Idra, as the join leaves them, in chapter 6's
        /// Prime parts at Sync 17. Its sheet's .45 came off reconnaissance s36 on November 14 (139 s at 115,000), a party in a Refined Edge and Scrap parts with no Emitter or Module on Idra.</summary>
        [UnityTest,Timeout(1500000)] public IEnumerator TheHostMarshalFallsToTheChaptersFloorPartyThroughThePadInsideItsSheet()
        {
            const string id="HostMarshal";float flat=0;yield return FlatOut(id,r=>flat=r);
            yield return Cleanup();yield return Boot();
            const string route="chapter-36-host-marshal",door="into the bottom gallery";
            yield return Fight(id,DevLoadout.JoinedLoadout,"FrozenHost","Door",FlagsBefore(32,route,door),new[]{"Taren","Idra"},Slice(route,door,"HostMarshal is down"),flat);
        }

        // ================================================================== Drask Vaal, the duel (route 52)
        /// <summary>Route 52 from the muster field (the walk out onto it, which a retry starts from) to the Warmaster's fall: Taren and Idra in chapter 8's Tempered parts at Sync 24. His
        /// sheet's .42 and his 150,000 came off two chains on November 14 (87 s and 76 s at 130,000), the same Refined-Edge party.</summary>
        [UnityTest,Timeout(1500000)] public IEnumerator DraskVaalsDuelFallsToTheChaptersFloorPartyThroughThePadInsideItsSheet()
        {
            const string id="DraskVaal";float flat=0;yield return FlatOut(id,r=>flat=r);
            yield return Cleanup();yield return Boot();
            const string route="chapter-52-the-warmaster",door="out onto the muster field";
            yield return Fight(id,"chapter8","Harrowgate","Field",FlagsBefore(44,route,door),new[]{"Taren","Idra"},Slice(route,door,"DraskVaal is down"),flat);
        }
    }
}
