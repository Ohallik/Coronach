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
            float reach=BossReach.Edge(enemy);
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
        // ------------------------------------------------------------------ the party as the player's chain brings it to a door
        /// <summary>A hero as the save a route resumes from holds them: Sync, EXTRA, each skill's uses (its mastery), each part by its definition and what it rolled, the second module.</summary>
        sealed class Arriving{public string hero;public int level;public float extra;public (string skill,int uses)[] used;public (string part,string[] rolled)[] worn;public (string part,string[] rolled) second;}
        /// <summary>Taren and Idra at the muster field as the player's chain brings them (Builds/quality/nov25-whole s51 final-state.json, the save route 52 resumed from on November 25):
        /// Taren's Arc Cleave and Pulse Burst at mastery 5, Idra's Breach at 4 and Slag Hammer at 3, both EXTRAs full, Idra a Sync behind, a second module each, and the chapter's tier
        /// with what it rolled. nov23-whole and nov24-whole s51 bring the same skills (her hammer one mastery higher) and EXTRA, and the same tier but for a part or an affix. The
        /// loadout (DevLoadout chapter8, what the shared length test measures) brings every skill at its first mastery, no EXTRA and no second module, and the fight ran 145 s where
        /// the player's ran 99 to 134 (d429c, d429f).</summary>
        static readonly Arriving[] AtTheMusterField=
        {
            new Arriving{hero="Taren",level=24,extra=100,used=new[]{("Taren_1",80),("Taren_3",97)},
                worn=new[]{("EdgesT4",new string[0]),("EmittersT4",new[]{"Attuned","Executioner"}),("FramesT3Light",new[]{"Balanced"}),("ModulesT4",new[]{"Lucky"}),("DrivesT4",new string[0])},
                second=("ModulesT4",new[]{"Overcharged"})},
            new Arriving{hero="Idra",level=23,extra=100,used=new[]{("Idra_1",47),("Idra_4",33),("Idra_3",1)},
                worn=new[]{("EdgesT4",new string[0]),("FramesT4",new string[0]),("DrivesT4",new[]{"Fleet"}),("EmittersT4Plasma",new string[0]),("ModulesT4",new string[0])},
                second=("ModulesT3",new string[0])},
        };
        /// <summary>The same save's stock and quick item.</summary>
        static readonly (string item,int count)[] MusterFieldStock={("RepairGel",9),("ChargeCell",5),("RepairSalve",6)};
        const string MusterFieldQuick="RepairSalve";
        /// <summary>The party put as the save holds it, over the loadout: each hero's Sync, charge, EXTRA, skill uses and parts, and the stock.</summary>
        static void Arrive(Arriving[] party,(string item,int count)[] stock,string quick)
        {
            var inv=new Inventory(State);
            foreach(var a in party)
            {
                var member=State.party.Find(m=>m.id==a.hero);Assert.IsNotNull(member,"fixture: "+a.hero+" is in the party");
                member.level=a.level;member.integrity=Levels.MaxIntegrity(a.level);member.charge=100;member.extra=a.extra;
                member.skillUses??=new Dictionary<string,int>();member.skillUses.Clear();foreach(var (skill,uses) in a.used)member.skillUses[skill]=uses;
                member.equipped.Clear();member.module2=null;
                foreach(var (part,rolled) in a.worn)
                {
                    var def=GameCatalog.Find<TechPartDef>(part);Assert.IsNotNull(def,"fixture: there is a "+part);
                    var item=inv.GivePart(def);item.affixes.AddRange(rolled);Assert.IsTrue(inv.Equip(member,item,def),"fixture: "+a.hero+" wears "+part);
                }
                var second=GameCatalog.Find<TechPartDef>(a.second.part);Assert.IsNotNull(second,"fixture: there is a "+a.second.part);
                var held=inv.GivePart(second);held.affixes.AddRange(a.second.rolled);Assert.IsTrue(inv.Equip(member,held,second,true),"fixture: "+a.hero+"'s second module");
            }
            State.consumables.Clear();foreach(var (item,count) in stock)State.consumables[item]=count;State.quickItem=quick;
        }
        static string Arrived()=>string.Join("; ",State.party.Select(m=>$"{m.id} sync={m.level} extra={m.extra:0} uses={(m.skillUses==null?"":string.Join(",",m.skillUses.Select(p=>p.Key+":"+p.Value+"=m"+SkillBook.Mastery(m.id,p.Key))))}"))+
            $" stock={string.Join(",",State.consumables.Select(p=>p.Key+":"+p.Value))} quick={State.quickItem}";

        /// <summary>The chapter's party (its loadout, the pair the route plays with, at the sheet's Sync, whole) at the door's checkpoint, with every flag the routes set before the door step;
        /// or, given an arrival, the party as the player's chain brings it there.</summary>
        IEnumerator AtTheDoor(string id,string loadout,string zone,string spawn,string[] flags,string[] pair,Arriving[] arrival=null)
        {
            DevLoadout.Apply(loadout);var sheet=ContentRegistry.Boss(id);
            foreach(string flag in flags)GameServices.Current.Flags.SetBool(flag,true);
            foreach(string flag in new[]{"bossdown."+id,"clear."+sheet.door})GameServices.Current.Flags.SetBool(flag,false);
            foreach(var member in State.party){member.level=sheet.sync;member.integrity=Levels.MaxIntegrity(sheet.sync);member.charge=100;}
            if(arrival!=null)Arrive(arrival,MusterFieldStock,MusterFieldQuick);
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
        IEnumerator Fight(string id,string loadout,string zone,string spawn,string[] flags,string[] pair,QualityRoute route,float flat,Arriving[] arrival=null)
        {
            yield return AtTheDoor(id,loadout,zone,spawn,flags,pair,arrival);
            string worn=Wearing(),arrived=Arrived();var a=new Account{boss=id};began=Time.unscaledTime;
            yield return Play(route,900,()=>Watch(a),()=>Ui()=="defeat"||a.fell>0&&Time.unscaledTime-a.fell>2);
            a.defeats=Ui()=="defeat"?1:0;
            var sheet=ContentRegistry.Boss(id);float integrity=GameCatalog.Find<EnemyDef>(id).integrity;
            float seconds=a.fell>0?a.fell-a.woke:Time.unscaledTime-a.woke;var body=Body(id);float left=a.fell>0?0:body!=null?body.integrity/body.maximum:-1;
            float share=a.fell>0?integrity/(flat*seconds):integrity*(1-Mathf.Max(0,left))/(flat*Mathf.Max(1,seconds));
            float least=sheet.minSeconds,most=sheet.maxSeconds;
            TestContext.WriteLine($"FLOOR_PLAY {id} fell={a.fell>0} seconds={seconds:0.0} left={left:P0} integrity={integrity:0} flatOut={flat:0}/s share={share:0.000} sheetShare={sheet.driverShare:0.00} bounds={least:0}-{most:0}s " +
                $"atSheetShare={integrity/(flat*sheet.driverShare):0}s leadFalls={a.leadFalls} partnerFalls={a.partnerFalls} defeats={a.defeats}");
            TestContext.WriteLine($"FLOOR_PLAY_PARTY {id} {worn}");
            TestContext.WriteLine($"FLOOR_PLAY_ARRIVAL {id} {(arrival!=null?"as the player's chain brings it":"the loadout's")}: {arrived}");
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
        /// <summary>Route 52 from the muster field (the walk out onto it, which a retry starts from) to the Warmaster's fall: Taren and Idra as the player's chain brings them there
        /// (<see cref="AtTheMusterField"/>: their mastery, EXTRA, Sync, parts and stock), against flat out measured as the shared length test measures it (the loadout's Tempered
        /// parts at Sync 24, every skill at its first mastery). His sheet's share is the player's own of that yardstick at his 225,000, over the four fights of November 23 to 25, less a
        /// tenth for room (HarrowgateBossTests' sheet says which); the loadout's floor party played him in 145 s, at .40 and .45 of it (d429c, d429f), where the player took 99 to 134 s;
        /// as the chain brings it, 112 to 126 s (red1, red2, red4, red5).</summary>
        [UnityTest,Timeout(1500000)] public IEnumerator DraskVaalsDuelFallsToTheChaptersFloorPartyThroughThePadInsideItsSheet()
        {
            const string id="DraskVaal";float flat=0;yield return FlatOut(id,r=>flat=r);
            yield return Cleanup();yield return Boot();
            const string route="chapter-52-the-warmaster",door="out onto the muster field";
            yield return Fight(id,"chapter8","Harrowgate","Field",FlagsBefore(44,route,door),new[]{"Taren","Idra"},Slice(route,door,"DraskVaal is down"),flat,AtTheMusterField);
        }
    }
}
