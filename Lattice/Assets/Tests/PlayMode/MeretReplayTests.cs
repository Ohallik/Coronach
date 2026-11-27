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
    /// <summary>The game's last boss, Director Meret Venn, played through the replay driver's own pad with her routes' own steps (docs/quality/routes chapter-66 and chapter-67, read from the
    /// files tools/mooring_routes.py writes), in the zones as built, from each door to her fall, her yield or the release: her craft over the Continuance's back from the bay's pad to the dock at
    /// the frame gantry; the gantry from its seal to her going down in the lift, and the ride; the well from its door to her yield, the crews' sequence, the last collar held through a whole
    /// ease, the anchor's release, her line, Ilo's custody and the launch into the Returning Passage. The party is the chapter's (DevLoadout chapter10, Taren and Idra out) at each sheet's
    /// Sync, with the save's flags as the routes before set them. Each fight's length through the pad is held to its sheet's bounds, its share of the same pair's flat out (measured in the same
    /// run, as QaPlanCombatTests measures BOSS_RATE) to the sheet's driverShare, and the party must not fall. Every line it plays is written out (MERET_PLAY) for the fight's own account.</summary>
    public sealed class MeretReplayTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject fixture;ContinuousReview replay;bool finished;readonly List<string> log=new();readonly List<string> errors=new();
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        static readonly string[] Heeded={"MERET_","COUPLING_","QUALITY_BOSS","QUALITY_EXTRA","QUALITY_ITEM","QUALITY_PROMPT","QUALITY_BERTH_GOAL","QUALITY_CHASE","QUALITY_TOWARD","QUALITY_READS_TALK",
            "QUALITY_UNSTRUCK","QUALITY_MERET","BOSS_","CHAMBER_","COLLAR_","ANCHOR_","BERTH_","WALL_CONTACT","CREDITS_","HERO_DOWN","DEFEAT"};
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
            fixture=new GameObject("Meret replay fixture");fixture.SetActive(false);replay=fixture.AddComponent<ContinuousReview>();
            log.Clear();errors.Clear();finished=false;began=Time.unscaledTime;Application.logMessageReceived+=Logged;
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
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ the driver, by its private parts (as HomeReplayHandsTests drives it)
        void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
        T Get<T>(string name)=>(T)typeof(ContinuousReview).GetField(name,Private).GetValue(replay);
        object Call(string name,params object[] arguments)=>typeof(ContinuousReview).GetMethod(name,Private).Invoke(replay,arguments);
        List<string> Failures=>Get<List<string>>("failures");
        string Ui()=>(string)Call("ObserveUi");
        GamepadState Held()=>Get<GamepadState>("held");
        IEnumerator Play(QualityRoute route,float most,System.Action each=null,System.Func<bool> enough=null)
        {
            if(Get<Gamepad>("pad")==null)Set("pad",InputSystem.AddDevice<Gamepad>("MeretReplayPad"));
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
        const string ShipRoute="chapter-66-the-continuance",ChamberRoute="chapter-67-the-anchor-chamber";
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
        /// <summary>A hero as the save a route resumes from holds them: Sync, EXTRA, each skill's uses (its mastery), each part by its definition and what it rolled, the second module.</summary>
        sealed class Arriving{public string hero;public int level;public float extra;public (string skill,int uses)[] used;public (string part,string[] rolled)[] worn;public (string part,string[] rolled) second;}
        /// <summary>Taren and Idra at the well's door as the player's chain brings them: the save route 67 resumed from on November 25 (Builds/quality/nov25-whole s66 final-state.json)
        /// with what the gantry left on the way (s67 player.log: Idra up a Sync, 28 to 29, and into a Choirwrought Emitter). Taren's Arc Cleave and Pulse Burst and Idra's Breach and Slag
        /// Hammer at mastery 5, Idra's EXTRA full and Taren's as the save holds it (he spends and fills it again in the gantry), a second module each, the chapter's tier with what it
        /// rolled, and the save's stock. nov23-whole and nov24-whole s66 bring the same mastery and EXTRA and the same tier but for a part or an affix. The loadout (DevLoadout
        /// chapter10) brings every skill at its first mastery, no EXTRA and no second module, and the well ran 133 and 147 s of her 150 (d429c, d429f) where the player's ran 118 to 131.</summary>
        static readonly Arriving[] AtTheWell=
        {
            new Arriving{hero="Taren",level=29,extra=12,used=new[]{("Taren_1",98),("Taren_3",131)},
                worn=new[]{("EdgesT5",new string[0]),("EmittersT4",new[]{"Attuned","Executioner"}),("FramesT5",new string[0]),("ModulesT4",new[]{"Lucky"}),("DrivesT5Scout",new[]{"Flashbound"})},
                second=("ModulesT5",new string[0])},
            new Arriving{hero="Idra",level=29,extra=100,used=new[]{("Idra_1",111),("Idra_4",67),("Idra_3",3)},
                worn=new[]{("EdgesT5",new string[0]),("FramesT5",new string[0]),("DrivesT5Scout",new string[0]),("EmittersT5",new string[0]),("ModulesT4",new[]{"Lucky"})},
                second=("ModulesT5",new string[0])},
        };
        static readonly (string item,int count)[] WellStock={("RepairGel",8),("ChargeCell",5),("RepairSalve",5)};
        const string WellQuick="RepairGel";
        /// <summary>The party put as the save holds it, over the loadout: each hero's Sync, EXTRA, skill uses and parts, and the stock.</summary>
        static void Arrive(Arriving[] party,(string item,int count)[] stock,string quick)
        {
            var inv=new Inventory(State);
            foreach(var a in party)
            {
                var member=State.party.Find(m=>m.id==a.hero);Assert.IsNotNull(member,"fixture: "+a.hero+" is in the party");
                member.level=a.level;member.integrity=Levels.MaxIntegrity(a.level);member.extra=a.extra;
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
        static string Arrived()=>string.Join("; ",State.party.Select(m=>$"{m.id} sync={m.level} extra={m.extra:0} uses={(m.skillUses==null?"":string.Join(",",m.skillUses.Select(p=>p.Key+":"+p.Value+"=m"+SkillBook.Mastery(m.id,p.Key))))} wears "+
            string.Join(",",m.equipped.Select(e=>State.parts.Find(p=>p.instanceId==e.Value)?.definitionId))+(string.IsNullOrEmpty(m.module2)?"":",+"+State.parts.Find(p=>p.instanceId==m.module2)?.definitionId)))+
            $" stock={string.Join(",",State.consumables.Select(p=>p.Key+":"+p.Value))} quick={State.quickItem}";
        /// <summary>The pair out (Taren leading, Idra beside him) at a Sync, whole, every skill's uses as a party that has used them; given an arrival, as the player's chain brings them.</summary>
        static IEnumerator Pair(int sync,Arriving[] arrival=null)
        {
            foreach(var member in State.party){member.level=sync;member.integrity=Levels.MaxIntegrity(sync);member.charge=100;}
            if(arrival!=null)Arrive(arrival,WellStock,WellQuick);
            Assert.IsTrue(Deployment.Set(State,new[]{"Taren","Idra"}),"fixture: Taren and Idra can be put out");
            PartyField.Reform(PartyController.Current);yield return Frames(3);PartyController.Current.RefreshStats();yield return null;
            if(Lead.character!="Taren")PartyController.Current.Swap();yield return Frames(3);
            foreach(var hero in PartyController.Current.members){hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=0;}
        }
        /// <summary>Every flag up now, and putting back exactly those: a broken sample's play sets flags (her talk heard, her fight begun) the play after it must find unset.</summary>
        static HashSet<string> Up()=>new(State.flags.Where(f=>f.Value).Select(f=>f.Key));
        static void Back(HashSet<string> up){foreach(var key in State.flags.Keys.ToList())if(State.flags[key]&&!up.Contains(key))GameServices.Current.Flags.SetBool(key,false);}
        /// <summary>A zone as the route's save has it: every flag the routes set before the step, loaded at the spawn the save names.</summary>
        IEnumerator Zone(string zone,string spawn,string[] flags,int sync,Arriving[] arrival=null)
        {
            foreach(string flag in flags)GameServices.Current.Flags.SetBool(flag,true);
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(1f);yield return Pair(sync,arrival);yield return new WaitForSecondsRealtime(.5f);
        }

        // ------------------------------------------------------------------ flat out (QaPlanCombatTests.FlatOut's measure, the pair the route plays with)
        /// <summary>The pair flat out on her standing still in the yard of her form, as the shared length test measures BOSS_RATE: Taren at her body, Idra (a blade) round her side from him, or
        /// both ships six metres off; every skill as it comes, else a blow; for 30 s. What the driver's share is a share of.</summary>
        static IEnumerator FlatOut(string id,System.Action<float> measured)
        {
            var sheet=ContentRegistry.Boss(id);bool flight=sheet.flight;
            SceneFlow.Current.LoadZone(flight?"Arena_Flight":"Arena_Ground");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            foreach(var left in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(left.gameObject);
            yield return new WaitForSecondsRealtime(.6f);
            if(!flight){var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Meret flat-out floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
            yield return Pair(sheet.sync);
            var taren=Lead;var idra=PartyController.Current.members.First(m=>m!=taren);
            foreach(var hero in new[]{taren,idra}){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;}
            var at=new Vector3(200,flight?1:.1f,0);var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;yield return Frames(3);
            var body=enemy.Health;body.maximum=body.integrity=10000000;
            float reach=BossReach.Edge(enemy);bool blade=!flight&&DevLoadout.Weapon(idra.character)==GearSlot.Edge;
            var homes=new[]{flight?at+new Vector3(-2,0,-6):at+Vector3.back*reach,flight?at+new Vector3(2,0,-6):blade?at+Quaternion.Euler(0,-70,0)*Vector3.back*reach:at+Vector3.back*8+Vector3.right*2};
            var heroes=new[]{taren,idra};
            void Home(int i){Place(heroes[i],homes[i]);var face=at-homes[i];face.y=0;if(heroes[i].motor is FlightMotor ship)ship.KeepFacing(face);else if(heroes[i].motor is GroundMotor feet)feet.KeepFacing(face);}
            for(int i=0;i<2;i++){Home(i);heroes[i].target=body;heroes[i].charge=100;}
            yield return null;
            float landed=0;System.Action<Health,DamagePacket,float> count=(_,packet,amount)=>{if(packet.source==taren.Health||packet.source==idra.Health)landed+=amount;};body.Damaged+=count;
            const float Seconds=30;int drawn=0;
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
            body.Damaged-=count;Assert.Greater(landed,0,"fixture: the pair is hitting her");
            string said=$"MERET_FLAT_OUT {id} sync={sheet.sync} rate={landed/Seconds:0}/s frames={drawn/Seconds:0}/s";TestContext.WriteLine(said);Debug.Log(said);
            if(drawn/Seconds<40)Assert.Inconclusive($"{id}: the flat-out measure ran at {drawn/Seconds:0} frames a second, on a machine too busy to say how fast a party is");
            measured(landed/Seconds);
            foreach(var hero in heroes){hero.GetComponent<PlayerBrain>().AutoPilot=false;hero.GetComponent<PartnerBrain>().enabled=true;}
            enemy.Passive=true;Object.Destroy(enemy.gameObject);yield return Frames(3);
        }

        // ------------------------------------------------------------------ what a fight is like, frame by frame
        /// <summary>The record of one fight played through the pad: when she woke and fell, what of hers struck whom, who fell, how the lead's thumb and her guard stood.</summary>
        sealed class Account
        {
            public string boss;public float woke=-1,fell=-1,lastSample;public BossController her;public string move="(none)";
            public readonly Dictionary<string,int> hits=new();public readonly Dictionary<string,float> taken=new();public int leadFalls,partnerFalls,defeats;
            public float shieldDown,shieldDownFiring,near,firing,frames,landed,lastBlow=-1,longestDry,acting;public int blows;public readonly HashSet<Health> hooked=new();public readonly Dictionary<CombatActor,bool> alive=new();
            public readonly List<string> lines=new();
        }
        static Health Body(string id)=>Health.All.FirstOrDefault(h=>h!=null&&!h.friendly&&h.id==id);
        void Watch(Account a)
        {
            float now=Time.unscaledTime;var party=PartyController.Current;if(party==null||party.members==null)return;
            var body=Body(a.boss);
            if(body!=null&&a.her==null){a.her=body.GetComponent<BossController>();body.Damaged+=(_,packet,amount)=>{if(amount<=0)return;a.landed+=amount;a.blows++;a.lastBlow=Time.unscaledTime;};}
            // A blow on a relay of hers lands too: it is what the lead is for while her dome stands.
            if(a.her!=null&&a.boss=="MeretShip")foreach(var relay in a.her.ShipRelays)if(relay!=null&&relay.health!=null&&a.hooked.Add(relay.health))relay.health.Damaged+=(_,packet,amount)=>{if(amount<=0)return;a.blows++;a.lastBlow=Time.unscaledTime;};
            if(a.her!=null&&a.woke<0&&a.her.Engaged)a.woke=now;
            if(a.woke>=0&&a.fell<0&&Flag("bossdown."+a.boss))a.fell=now;
            foreach(var member in party.members)
            {
                if(member==null)continue;
                if(a.hooked.Add(member.Health)){var who=member.character;member.Health.Damaged+=(h,packet,amount)=>{if(amount<=0||a.fell>=0||a.woke<0)return;bool hers=packet.source!=null&&packet.source.id==a.boss;
                    string key=who+" "+(hers?a.move:(packet.source!=null?packet.source.id:"?"))+(string.IsNullOrEmpty(packet.tag)?"":"/"+packet.tag);a.hits[key]=(a.hits.TryGetValue(key,out int n)?n:0)+1;a.taken[who]=(a.taken.TryGetValue(who,out float t)?t:0)+amount/Mathf.Max(1,h.maximum);};}
                bool up=member.Health.Alive;if(a.alive.TryGetValue(member,out bool was)&&was&&!up&&a.woke>=0&&a.fell<0){if(member==party.Active)a.leadFalls++;else a.partnerFalls++;a.lines.Add($"{now-a.woke:0.0} {member.character} falls ({a.move})");}
                a.alive[member]=up;
            }
            if(a.woke<0||a.fell>=0)return;
            a.frames+=Time.unscaledDeltaTime;a.longestDry=Mathf.Max(a.longestDry,now-Mathf.Max(a.woke,a.lastBlow));var lead=party.Active;var pad=Held();bool fire=(pad.buttons&(1u<<(int)GamepadButton.South))!=0;
            if(fire)a.firing+=Time.unscaledDeltaTime;
            if(body!=null&&lead!=null&&Flat(lead.transform.position,body.transform.position)<(lead.flight?28:3.5f))a.near+=Time.unscaledDeltaTime;
            if(a.her!=null&&a.boss=="MeretShip"&&!a.her.ShipShieldUp){a.shieldDown+=Time.unscaledDeltaTime;if(fire)a.shieldDownFiring+=Time.unscaledDeltaTime;}
            // Able to act and acting: rolling out of something, or firing at what takes it (her and her projectors with her dome down; a lit relay with it up). Fire held at her dome is not.
            if(a.her!=null&&a.boss=="MeretShip"&&lead!=null)
            {
                bool atRelay=lead.target!=null&&lead.target.TryGetComponent<MeretRelay>(out var aimed)&&aimed.Lit;
                if(lead.State==ActorState.Dodge||fire&&(!a.her.ShipShieldUp||atRelay))a.acting+=Time.unscaledDeltaTime;
            }
            if(now-a.lastSample>=5&&body!=null&&lead!=null)
            {
                a.lastSample=now;var partner=party.members.FirstOrDefault(m=>m!=lead);int si=Get<int>("stepIndex");var step=Get<QualityStep>("step");
                string line=$"{now-a.woke:0} s bar={body.integrity/body.maximum:P0} ({body.integrity:0}; {a.blows} blows {a.landed:0} landed; target {(lead.target!=null?lead.target.id:"none")}; inv {body.InvulnerableUntil-GameTime.Now:0.0} dmg {body.Damageable} broken {body.Broken} plate {(body.Plate!=null?body.Plate(new DamagePacket()):1):0.0} shots {Projectile.Live.Count(pr=>pr!=null&&pr.isActiveAndEnabled&&pr.Threatens(false))}) '{(a.her!=null?a.her.StatusText:"")}' lead={lead.character} {lead.Health.integrity/lead.Health.maximum:P0} partner={(partner!=null?partner.Health.integrity/partner.Health.maximum:0):P0} at={lead.transform.position:F1} gap={Flat(lead.transform.position,body.transform.position):0.0} step={si} '{(step!=null?step.name:"")}'";
                a.lines.Add(line);Debug.Log("MERET_PLAY_AT "+a.boss+" "+line);
            }
        }
        void Report(Account a,float flat,float integrity)
        {
            float seconds=a.fell>0?a.fell-a.woke:Time.unscaledTime-a.woke;
            var body=Body(a.boss);float left=a.fell>0?0:body!=null?body.integrity/body.maximum:-1;
            float rate=integrity*(1-Mathf.Max(0,left))/Mathf.Max(1,seconds);
            string summary=$"MERET_PLAY {a.boss} fell={a.fell>0} seconds={seconds:0.0} left={left:P0} rate={rate:0}/s flatOut={flat:0}/s share={(flat>0?rate/flat:0):0.000} leadFalls={a.leadFalls} partnerFalls={a.partnerFalls} " +
                $"near={a.near/Mathf.Max(1,a.frames):P0} firing={a.firing/Mathf.Max(1,a.frames):P0}"+(a.boss=="MeretShip"?$" shieldDown={a.shieldDown/Mathf.Max(1,a.frames):P0} firedWhileDown={a.shieldDownFiring/Mathf.Max(.01f,a.shieldDown):P0} acting={a.acting/Mathf.Max(1,a.frames):P0}":"");
            TestContext.WriteLine(summary);Debug.Log(summary);
            TestContext.WriteLine("MERET_TAKEN "+string.Join(", ",a.taken.Select(p=>$"{p.Key} {p.Value:P0}")));
            TestContext.WriteLine("MERET_HITS "+string.Join(", ",a.hits.OrderByDescending(p=>p.Value).Select(p=>$"{p.Key} x{p.Value}")));
            foreach(var line in a.lines)TestContext.WriteLine("MERET_TIMELINE "+line);
        }
        void Dump()
        {
            foreach(var line in log)TestContext.WriteLine("LOG "+line);
            foreach(var line in errors.Distinct().Take(30))TestContext.WriteLine("ERROR "+line);
            foreach(var f in Failures)TestContext.WriteLine("STEP_FAILED "+f);
        }
        System.Action Moves(Account a)
        {
            System.Action<BossController,string> moved=(who,move)=>{if(who!=null&&who.Health!=null&&who.Health.id==a.boss)a.move=move;};
            BossController.MoveStarted+=moved;return ()=>BossController.MoveStarted-=moved;
        }

        // ================================================================== phase one: her craft
        /// <summary>The driver without its hands for her fights, for a case's broken state: put back by every case and by the tear-down.</summary>
        static void HandsOff(bool off)=>typeof(ContinuousReview).GetField("meretHandsOff",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,off);
        /// <summary>Her fight alone, from her door to her fall, as route 66 plays it: what it was like.</summary>
        IEnumerator ShipFight(string[] flags,int sync,System.Action<Account> done,float most=900)
        {
            yield return Zone("Continuance","LaunchBay",flags,sync);
            var a=new Account{boss="MeretShip"};var unhook=Moves(a);
            yield return Play(Slice(ShipRoute,"to the bay's pad","MeretShip is down"),most,()=>Watch(a),()=>Ui()=="defeat"||a.fell>0);
            unhook();a.defeats=Ui()=="defeat"?1:0;done(a);
        }
        /// <summary>Route 66 from her door (the walk to the bay's pad, which a retry starts from too) to its end: the launch round the hull, into her lane, the lock, the pursuit rounds,
        /// her going to the gantry, her line at the rail, the flight to the gantry's dock and the dock into the anchor chamber; and her fight twice more, for how much it varies. Built
        /// first twice: as it was (no relays, the driver without its hand: within a minute and a half it drifts in behind a shelter wing and lands nothing for half a minute at a time;
        /// it fell short at 62% after fourteen rounds), and with the hand but without her relays (her dome stood about seven tenths of the fight and a player held fire through it).
        /// Now the lead fires at a relay while her dome stands and at her while it is down, acting most of the fight, and three fights of hers differ by no more than a quarter.</summary>
        [UnityTest,Timeout(2400000)] public IEnumerator HerCraftIsFoughtThroughThePadFromTheBayToTheDockAtTheGantry()
        {
            const string id="MeretShip";float flat=0;yield return FlatOut(id,r=>flat=r);
            var sheet=ContentRegistry.Boss(id);var flags=FlagsBefore(ShipRoute,"to the bay's pad");float integrity=GameCatalog.Find<EnemyDef>(id).integrity;
            var before=Up();Account off=null,bare=null;
            try
            {
                // As it was: no relays, and the driver without its hand.
                BossController.ShipRelaysOff=true;HandsOff(true);
                yield return ShipFight(flags,sheet.sync,r=>off=r,110);
                HandsOff(false);Report(off,flat,integrity);Back(before);
                TestContext.WriteLine($"MERET_DRY without the hand: {off.longestDry:0} s with nothing landing");
                Assert.Greater(off.longestDry,25,$"fixture: without the hand the driver never went 25 s without landing a blow on her (the longest {off.longestDry:0} s): the check below proves nothing");
                // The hand, without her relays: the dome stands most of the fight and nothing can be done under it but wait.
                log.Clear();
                yield return ShipFight(flags,sheet.sync,r=>bare=r,110);
                Report(bare,flat,integrity);Back(before);
            }
            finally{BossController.ShipRelaysOff=false;HandsOff(false);}
            float bareActing=bare.acting/Mathf.Max(1,bare.frames);
            TestContext.WriteLine($"MERET_ACTING without her relays: {bareActing:P0}");
            Assert.Less(bareActing,.5f,$"fixture: without her relays the lead was acting {bareActing:P0} of the fight: the check below proves nothing");
            // Now: route 66 whole, then her fight twice more.
            log.Clear();
            yield return Zone("Continuance","LaunchBay",flags,sheet.sync);
            var a=new Account{boss=id};var unhook=Moves(a);
            yield return Play(Slice(ShipRoute,"to the bay's pad"),900,()=>Watch(a),()=>Ui()=="defeat");
            unhook();a.defeats=Ui()=="defeat"?1:0;
            Report(a,flat,integrity);Dump();
            Judge(a,flat,id);
            float acting=a.acting/Mathf.Max(1,a.frames);
            TestContext.WriteLine($"MERET_ACTING with her relays: {acting:P0}; MERET_DRY {a.longestDry:0} s with nothing landing");
            Assert.GreaterOrEqual(acting,.6f,$"the lead was acting {acting:P0} of her fight: firing at what takes it or rolling out of what's marked should be most of it");
            Assert.Less(a.longestDry,20,$"with the hand the lead went {a.longestDry:0} s without landing a blow on her or a relay");
            Assert.AreEqual(0,log.Count(l=>l.Contains("WALL_CONTACT Taren")),"the lead scraped the berth's solids during her fight: "+string.Join(" | ",log.Where(l=>l.Contains("WALL_CONTACT")).Take(5)));
            Assert.IsTrue(finished,"route 66 never reached its end from her door");
            CollectionAssert.IsEmpty(Failures,"route 66 failed a step from her door on: "+string.Join(" | ",Failures));
            Assert.AreEqual("StillwaterChamber",SceneFlow.Current.Zone,"route 66 never docked at the frame gantry");
            var lengths=new List<float>{a.fell-a.woke};
            for(int i=0;i<2;i++)
            {
                Back(before);log.Clear();Account again=null;
                yield return ShipFight(flags,sheet.sync,r=>again=r);
                Report(again,flat,integrity);
                Assert.AreEqual(0,again.defeats,$"the party fell to her craft in fight {i+2}");Assert.Greater(again.fell,0,$"her craft never fell in fight {i+2}");
                lengths.Add(again.fell-again.woke);
            }
            float mean=lengths.Average(),spread=(lengths.Max()-lengths.Min())/mean;
            TestContext.WriteLine($"MERET_SPREAD {string.Join(", ",lengths.Select(l=>l.ToString("0")))} s: mean {mean:0} s, spread {spread:P0}");
            foreach(float l in lengths)Assert.That(l,Is.InRange(sheet.minSeconds,sheet.maxSeconds),$"one of three fights of hers lasted {l:0} s: her sheet's bounds are {sheet.minSeconds:0} to {sheet.maxSeconds:0}");
            Assert.LessOrEqual(spread,.25f,$"three fights of hers lasted {string.Join(", ",lengths.Select(l=>l.ToString("0")))} s: {spread:P0} apart, where a quarter is the most luck should move it");
        }

        // ================================================================== phase two: the gantry, and the lift
        /// <summary>Route 67 from the gantry's seal (her door) through her fall, the wait for the cage and the ride down to the lift's foot. Her bar held at a line never stands long with
        /// nothing offered to open.</summary>
        [UnityTest,Timeout(1800000)] public IEnumerator HerFrameIsFoughtThroughThePadFromTheSealToTheLiftsFoot()
        {
            const string id="MeretGantry";float flat=0;yield return FlatOut(id,r=>flat=r);
            const string door="north through the gantry's seal";
            var route=Slice(ChamberRoute,door,"ride down to the control well");
            // The pair as the player's chain brings it (AtTheWell: the gantry is the same segment's first fight), since November 26. With every skill at its first mastery and no
            // EXTRA the replay read .29 to .33 of flat out (119 and 125 s, d429f and gantry1) against a share measured off the chain's party.
            yield return Zone("StillwaterChamber","Gantry",FlagsBefore(ChamberRoute,door),ContentRegistry.Boss(id).sync,AtTheWell);
            var a=new Account{boss=id};var unhook=Moves(a);float held=0,heldSince=-1,longestHeld=0;
            yield return Play(route,900,()=>{Watch(a);bool now=a.her!=null&&a.fell<0&&a.her.StatusText.StartsWith("HER ARMS HOLD");
                if(now&&heldSince<0)heldSince=Time.unscaledTime;if(!now&&heldSince>=0){longestHeld=Mathf.Max(longestHeld,Time.unscaledTime-heldSince);heldSince=-1;}if(now)held+=Time.unscaledDeltaTime;},()=>Ui()=="defeat");
            unhook();a.defeats=Ui()=="defeat"?1:0;
            Report(a,flat,GameCatalog.Find<EnemyDef>(id).integrity);Dump();TestContext.WriteLine($"MERET_HELD {held:0.0} s in all, the longest {longestHeld:0.0} s");
            Judge(a,flat,id);
            // Held, she reaches with an arm still coupled: a rest, a move dodged, a walk to the seat. It stood 25 s with the lead at her feet before she did.
            Assert.Less(longestHeld,20,$"her bar stood held at a line for {longestHeld:0} s with no arm overreached to open");
            Assert.IsTrue(finished,"route 67 never reached the lift's foot from the gantry's seal");
            CollectionAssert.IsEmpty(Failures,"route 67 failed a step from the gantry's seal to the lift's foot: "+string.Join(" | ",Failures));
            Assert.Less(Flat(Lead.transform.position,new Vector3(0,-16,46.4f)),3,$"the ride down left the lead at {Lead.transform.position:F1}, not at the lift's foot");
        }

        // ================================================================== phase three: the well, her yield, the release
        /// <summary>How often this hero was clamped by a trap that closed under him for standing still (a clamp between .7 and 1.5 s after one was marked under him).</summary>
        int ClampedStanding(string hero)
        {
            float At(string line)=>float.Parse(line.Substring(0,line.IndexOf(' ')),System.Globalization.CultureInfo.CurrentCulture);
            var marked=log.Where(l=>l.Contains("MERET_TRAP_STILL "+hero)).Select(At).ToList();
            return log.Where(l=>l.Contains("MERET_CLAMPED "+hero)).Select(At).Count(t=>marked.Any(m=>t-m>.7f&&t-m<1.5f));
        }
        /// <summary>Route 67 from the well's door to its end: her talk, the duel to her yield, her words standing, the crews' sequence, the last collar held through a whole ease, the anchor's
        /// release and her line over it, Ilo's custody, the launch into the Returning Passage. The lead is never clamped for standing still (the driver's hand, shown failing first by
        /// <see cref="AClampOfHersFromNoMoveIsRolledOutOfAsItLands"/>; played as it was the first time, he was clamped six times).</summary>
        [UnityTest,Timeout(1800000)] public IEnumerator SheIsFoughtThroughThePadInTheWellToHerYieldAndTheReleaseAndTheLaunch()
        {
            const string id="MeretWell";float flat=0;yield return FlatOut(id,r=>flat=r);
            const string door="into the well's door until Meret speaks";var flags=FlagsBefore(ChamberRoute,door);var sheet=ContentRegistry.Boss(id);
            const string lead="Taren";
            // The pair as the player's chain brings it to the well (AtTheWell); flat out above is the shared length test's yardstick, every skill at its first mastery.
            yield return Zone("StillwaterChamber","Well",flags,sheet.sync,AtTheWell);
            TestContext.WriteLine("MERET_ARRIVAL "+id+" as the player's chain brings it: "+Arrived());
            var a=new Account{boss=id};var unhook=Moves(a);float bedAtRelease=-1,bedLow=-1;
            yield return Play(Slice(ChamberRoute,door),900,()=>{Watch(a);if(AnchorRelease.Current!=null&&AnchorRelease.Current.Now!=AnchorRelease.Stage.Holding){if(bedAtRelease<0)bedAtRelease=AudioManager.BedVolume;bedLow=AudioManager.BedVolume;}},()=>Ui()=="defeat");
            unhook();a.defeats=Ui()=="defeat"?1:0;
            Report(a,flat,GameCatalog.Find<EnemyDef>(id).integrity);Dump();
            TestContext.WriteLine($"MERET_BED at the release {bedAtRelease:0.000}, after {bedLow:0.000}");
            Judge(a,flat,id);
            Assert.AreEqual(0,ClampedStanding(lead),"with the hand the lead was still clamped for standing still");
            Assert.IsTrue(finished,"route 67 never reached its end from the well's door");
            CollectionAssert.IsEmpty(Failures,"route 67 failed a step from the well's door on: "+string.Join(" | ",Failures));
            foreach(string flag in new[]{"end.collarTaken","end.anchorReleased","end.custody"})Assert.IsTrue(Flag(flag),flag+" never set");
            Assert.Less(bedLow,bedAtRelease*.5f,"the steel's bed didn't come down at the release");
            Assert.AreEqual("ReturningPassage",SceneFlow.Current.Zone,"route 67 never launched into the Returning Passage");
        }

        /// <summary>Her markings that come from no move of hers (her shield's answer, the clamp that closes under a hero who stood still) aren't in the tell her moves count down, which is all
        /// the older rule reads. One clamp laid under a lead held still in a fight step, its tell a second: as the driver was he sat in it as it closed; with the hand he rolls out.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator AClampOfHersFromNoMoveIsRolledOutOfAsItLands()
        {
            bool inside=false;int rolls=0;
            IEnumerator Once(bool broken)
            {
                HandsOff(broken);rolls=0;yield return Yard(false);
                var at=new Vector3(200,.1f,0);var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MeretWell"),at);enemy.Passive=true;yield return Frames(3);
                var boss=enemy.GetComponent<BossController>();var lead=Lead;lead.GetComponent<PlayerBrain>().AutoPilot=false;
                Place(lead,at+Vector3.forward*6);Place(PartyController.Current.members.First(m=>m!=lead),at+Vector3.right*30);yield return Frames(3);
                var mark=Telegraph.Circle(lead.transform.position,BossController.WellTune.TrapRadius,1f,Telegraph.Heavy);mark.owner=boss;bool landed=false;inside=true;
                var hold=new QualityStep{name="hold where the clamp is marked",seconds=1.8f,evadeTelegraphs=true};
                yield return Play(new QualityRoute{name="her clamp",scene="Arena_Ground",steps=new[]{hold}},3,()=>{if(Lead.State==ActorState.Dodge)rolls++;if(!landed&&mark!=null&&mark.Charged){landed=true;inside=mark.Contains(Lead.transform.position);}});
                if(mark!=null)Object.Destroy(mark.gameObject);enemy.Passive=true;Object.Destroy(enemy.gameObject);yield return Frames(2);HandsOff(false);
            }
            yield return Once(true);
            Assert.IsTrue(inside,"fixture: without the hand the lead wasn't in her clamp as it closed: the check below proves nothing");
            yield return Once(false);
            Assert.Greater(rolls,0,"the driver never rolled as her clamp's tell ran out");
            Assert.IsFalse(inside,"the driver rolled and was still in her clamp as it closed");
        }

        /// <summary>The route's skill presses between rounds read no tells: a clamp she marked under the lead for standing still as a fight step ended closed on him through the two presses
        /// and their releases (red1 twice, and the chain's own captures: nov25-whole s67 "release skill MeretWell 2a" and "3b", nov24-whole s67 "2b" and "4a", November 26). Her clamp laid
        /// under him as route 67's four steps between rounds one and two begin, its tell a second: as the driver was (<c>stillRollOff</c>) he presses both skills and is in it as it closes; now
        /// he rolls out first and both skills still go off.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator ASkillStepRollsOutOfHerClampForStandingStillAndStillPressesItsSkill()
        {
            bool inside=false;int rolls=0,skills=0;var field=typeof(ContinuousReview).GetField("stillRollOff",BindingFlags.Static|BindingFlags.NonPublic);
            var steps=Slice(ChamberRoute,"skill MeretWell 2a","release skill MeretWell 2b").steps;
            IEnumerator Once(bool broken)
            {
                field.SetValue(null,broken);rolls=skills=0;inside=false;
                try
                {
                    yield return Yard(false);
                    var at=new Vector3(200,.1f,0);var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MeretWell"),at);enemy.Passive=true;yield return Frames(3);
                    var boss=enemy.GetComponent<BossController>();var lead=Lead;lead.GetComponent<PlayerBrain>().AutoPilot=false;lead.charge=100;
                    // At her blade's reach, where the fight steps leave him.
                    Place(lead,at+Vector3.forward*BossReach.Edge(enemy));Place(PartyController.Current.members.First(m=>m!=lead),at+Vector3.right*30);yield return Frames(3);
                    lead.target=enemy.Health;
                    // Her own clamp for standing still, laid as she lays it (BossController.WellStillTrap), under him where he stands.
                    var trap=(IEnumerator)typeof(BossController).GetMethod("WellStillTrap",Private).Invoke(boss,new object[]{lead,lead.transform.position});boss.StartCoroutine(trap);
                    var mark=Telegraph.Live.LastOrDefault(t=>t!=null&&t.owner==boss);Assert.IsNotNull(mark,"fixture: her clamp was never marked");Assert.IsTrue(boss.WellStillMark(mark),"fixture: the clamp isn't one she reads as laid for standing still");
                    bool landed=false;Application.LogCallback said=(text,_,__)=>{if(text.StartsWith("SKILL_OK "+lead.character))skills++;};Application.logMessageReceived+=said;
                    try
                    {
                        yield return Play(new QualityRoute{name="her clamp on a skill step",scene="Arena_Ground",steps=steps},4,()=>{if(Lead.State==ActorState.Dodge)rolls++;if(!landed&&mark!=null&&mark.Charged){landed=true;inside=mark.Contains(Lead.transform.position);}});
                        for(float end=Time.unscaledTime+1.5f;Time.unscaledTime<end;)yield return Still(0);
                    }
                    finally{Application.logMessageReceived-=said;}
                    TestContext.WriteLine($"MERET_STILL_SKILL_STEP {(broken?"as it was":"now")}: rolls={rolls} frames, inside as it closed={inside}, skills={skills}");
                    Assert.IsTrue(landed,"fixture: her clamp never closed");
                    if(mark!=null)Object.Destroy(mark.gameObject);enemy.Passive=true;Object.Destroy(enemy.gameObject);yield return Frames(2);
                }
                finally{field.SetValue(null,false);}
            }
            yield return Once(true);
            int skillsAsItWas=skills;
            Assert.IsTrue(inside,"fixture: as it was the lead wasn't in her clamp as it closed: the check below proves nothing");
            Assert.AreEqual(2,skillsAsItWas,"fixture: as it was the two skill steps didn't put out two skills");
            yield return Once(false);
            Assert.Greater(rolls,0,"the driver never rolled out of her clamp on the skill steps");
            Assert.IsFalse(inside,"the driver was in her clamp as it closed on the skill steps");
            Assert.AreEqual(skillsAsItWas,skills,"the roll cost a skill the steps press");
        }

        /// <summary>His own EXTRA roots him where it goes off, and nothing can be pressed through it: as it was its seconds counted toward her clamp for standing still, and played through the
        /// pad one was laid under Taren inside his EXTRA and closed on him as it ended, the driver's roll pressed and refused (red1, November 26: EXTRA_BEGIN, MERET_TRAP_STILL, QUALITY_MERET_ROLL,
        /// MERET_CLAMPED, EXTRA_OK). Her third phase (1.3 s still) with no move of hers due, the lead stood a moment and then into his EXTRA: as it was (<c>WellExtraCountsStill</c>) a clamp is
        /// laid under him inside it; now none is.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator HisOwnExtraIsNotStandingStillToHerClamps()
        {
            int laid=0;bool fired=false;
            IEnumerator Once(bool broken)
            {
                BossController.WellExtraCountsStill=broken;laid=0;fired=false;
                try
                {
                    yield return Yard(false);
                    var at=new Vector3(200,.1f,0);var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MeretWell"),at);enemy.Passive=true;yield return Frames(3);
                    var boss=enemy.GetComponent<BossController>();var lead=Lead;
                    enemy.Health.integrity=enemy.Health.maximum*.2f;typeof(BossController).GetField("nextSpecial",Private).SetValue(boss,float.MaxValue);
                    Place(lead,at+Vector3.forward*BossReach.Edge(enemy));Place(PartyController.Current.members.First(m=>m!=lead),at+Vector3.right*30);lead.target=enemy.Health;yield return Frames(3);
                    Assert.AreEqual(3,boss.Phase,"fixture: she isn't in her third phase");
                    var move=lead.GetComponent<ExtraMove>();State.party.Find(m=>m.id==lead.character).extra=ExtraGauge.Full;
                    // Her count of who stands still runs only while she fights.
                    enemy.Passive=false;yield return Still(.4f);
                    Application.LogCallback said=(text,_,__)=>{if(text.StartsWith("MERET_TRAP_STILL "+lead.character)&&move.Performing)laid++;};Application.logMessageReceived+=said;
                    try
                    {
                        fired=move.TryFire();
                        for(float end=Time.unscaledTime+6;fired&&move.Performing&&Time.unscaledTime<end;)yield return Still(0);
                    }
                    finally{Application.logMessageReceived-=said;}
                    TestContext.WriteLine($"MERET_EXTRA_STILL {(broken?"as it was":"now")}: fired={fired}, clamps laid under him inside it={laid}");
                    enemy.Passive=true;Object.Destroy(enemy.gameObject);yield return Frames(2);
                }
                finally{BossController.WellExtraCountsStill=false;}
            }
            yield return Once(true);
            Assert.IsTrue(fired,"fixture: his EXTRA never went off");
            Assert.Greater(laid,0,"fixture: as it was no clamp was laid under him inside his EXTRA: the check below proves nothing");
            yield return Once(false);
            Assert.IsTrue(fired,"fixture: his EXTRA never went off");
            Assert.AreEqual(0,laid,"a clamp was laid under him for standing still inside his own EXTRA, where nothing can be pressed");
        }

        /// <summary>Her frame turns half of a blow while her arms brace it, and a first-time player has to be told so in words, as the Slag Colossus's glowing armour tells him: the first turned
        /// blow puts up why on screen (what opens her), the lead says it, and the bar's line says it while it holds. Built first as it was (<c>GantryPlateSilent</c>: grey numbers alone).</summary>
        [UnityTest,Timeout(300000)] public IEnumerator HerFrameSaysWhyABlowGlancesAndWhatOpensHer()
        {
            string posted=null,status=null,said=null,openStatus=null;
            IEnumerator Once(bool broken)
            {
                BossController.GantryPlateSilent=broken;posted=status=said=openStatus=null;
                try
                {
                    yield return Yard(false);
                    var at=new Vector3(200,.1f,0);var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MeretGantry"),at);enemy.Passive=false;yield return Frames(3);
                    var boss=enemy.GetComponent<BossController>();var lead=Lead;Place(lead,at+Vector3.forward*8);Place(PartyController.Current.members.First(m=>m!=lead),at+Vector3.right*30);yield return Frames(2);
                    System.Action<string,string> post=(title,body)=>{posted??=title;};Notices.Posted+=post;
                    System.Action<string,string> spoken=(who,line)=>{if(who==lead.character)said??=line;};BarkService.Spoken+=spoken;
                    try
                    {
                        float before=enemy.Health.integrity;float landed=enemy.Health.Receive(new DamagePacket{source=lead.Health,amount=400,type=DamageType.Kinetic});
                        Assert.Less(landed,399,"fixture: her braced frame turned nothing of the blow");
                        status=boss.StatusText;
                        for(float end=Time.unscaledTime+1.5f;Time.unscaledTime<end;)yield return Still(0);
                        // And open (her frame run out: an opening), the line says strike.
                        enemy.Health.Expose(2);yield return Frames(2);openStatus=boss.StatusText;
                    }
                    finally{Notices.Posted-=post;BarkService.Spoken-=spoken;}
                    enemy.Passive=true;Object.Destroy(enemy.gameObject);yield return Frames(2);
                }
                finally{BossController.GantryPlateSilent=false;}
            }
            yield return Once(true);
            Assert.AreNotEqual("HER FRAME TURNS A BLOW",posted,"fixture: built as it was, the turned blow was told on screen all the same: the check below proves nothing");
            Assert.IsTrue(string.IsNullOrEmpty(status),$"fixture: built as it was, the bar said '{status}' of her braced frame");
            yield return Once(false);
            Assert.AreEqual("HER FRAME TURNS A BLOW",posted,"the first blow her frame turned put nothing on screen to say why");
            Assert.AreEqual("HER ARMS BRACE THE FRAME  ·  STRIKE WHEN IT OPENS",status,"while her arms brace the frame the bar doesn't say so, or what to do");
            Assert.AreEqual("THE FRAME IS OPEN  ·  STRIKE",openStatus,"with her frame open the bar doesn't say to strike");
            Assert.IsNotNull(said,"the lead said nothing as the first blow glanced off her frame");
            Assert.AreEqual(BarkService.Line(Lead.character,"bossbraced.MeretGantry"),said,"the lead's line on the turned blow isn't the frame's own");
        }

        /// <summary>From her crews being told (<c>end.crewsOrdered</c>) to the sequence being read, the crews walk in to the clamps and the braces and Tovra's smiths fit the gear: route 67's own
        /// steps, played through the pad, with the longest stretch in which nothing is said, heard or offered to the lead measured. No more than about four seconds. Built first without
        /// Tovra's radio (the three lines that carry it), which must leave the thirteen and a half seconds to the sequence empty.</summary>
        [UnityTest,Timeout(600000)] public IEnumerator BeforeTheReleaseTheCrewsArrivalNeverLeavesMoreThanFourSecondsWithNothingInIt()
        {
            float quiet=0;string story="";
            yield return Crews(true,(l,h)=>{quiet=l;story=h;});
            Assert.Greater(quiet,4.5f,$"fixture: without Tovra's radio the crews' arrival still had nothing longer than {quiet:0.0} s empty: the check below proves nothing ({story})");
            yield return Crews(false,(l,h)=>{quiet=l;story=h;});
            Assert.LessOrEqual(quiet,4.5f,$"before the release {quiet:0.0} s went by with nothing said, heard or offered ({story})");
        }
        IEnumerator Crews(bool silent,System.Action<float,string> measured)
        {
            const string from="Meret yields begins",to="finish Tovra reads the sequence";
            var before=Up();
            yield return Zone("StillwaterChamber","Well",FlagsBefore(ChamberRoute,from),29);
            if(silent)foreach(var hail in Object.FindObjectsByType<RadioHail>(FindObjectsSortMode.None))if(hail.speaker=="Tovra")hail.enabled=false;
            float ordered=-1,talkAt=-1,busyUntil=-1,longest=0;var happened=new List<string>();
            System.Action<string,string> spoken=(who,line)=>{if(ordered>=0&&talkAt<0){busyUntil=Mathf.Max(busyUntil,Time.unscaledTime+3.5f);happened.Add($"{Time.unscaledTime-ordered:0.0} {who}: {line}");}};
            BarkService.Spoken+=spoken;
            try
            {
                yield return Play(Slice(ChamberRoute,from,to),120,()=>
                {
                    float now=Time.unscaledTime;bool talking=DialogueSystem.Current!=null&&DialogueSystem.Current.Running;
                    if(ordered<0&&Flag("end.crewsOrdered")&&!talking){ordered=now;busyUntil=now;}
                    if(ordered<0||talkAt>=0)return;
                    if(talking){talkAt=now;happened.Add($"{now-ordered:0.0} the sequence");return;}
                    PromptService.FindInteraction(Lead.transform.position);var offered=PromptService.Interaction;if(offered!=null&&offered.Available)busyUntil=Mathf.Max(busyUntil,now);
                    longest=Mathf.Max(longest,now-busyUntil);
                });
            }
            finally{BarkService.Spoken-=spoken;}
            TestContext.WriteLine($"MERET_CREWS {(silent?"without Tovra's radio":"as built")}: ordered->sequence {talkAt-ordered:0.0} s, longest with nothing {longest:0.0} s: "+string.Join(" | ",happened));Dump();
            Assert.Greater(ordered,0,"fixture: her crews were never told");
            Assert.Greater(talkAt,0,"the sequence was never read");
            CollectionAssert.IsEmpty(Failures,"route 67 failed a step from her yield to the sequence: "+string.Join(" | ",Failures));
            Back(before);measured(longest,string.Join(" | ",happened));
        }

        // ================================================================== the end of the game
        /// <summary>Route 69 from Orrin's board (seven weeks on, after the release and the passage home) through the handrail mended, the old line repainted and Jun's last line to the moment
        /// the credits begin, by themselves, over the town: what the release is for, played through the pad.</summary>
        [UnityTest,Timeout(600000)] public IEnumerator SevenWeeksOnTheTownsLastWordsArePlayedThroughThePadToTheCreditsBeginning()
        {
            const string route="chapter-69-neighbourhood-afloat",from="back down to the garden stair 1/4";
            GameServices.Current.Flags.SetBool("game.complete",false);GameServices.Current.Flags.SetBool("credits.rolled",false);
            yield return Zone("NeighbourhoodAfloat","Arrival",FlagsBefore(route,from),30);
            // Where the route stands before the slice: the end of its last walk (Orrin's board).
            var steps=Read(route).steps;int first=System.Array.FindIndex(steps,st=>st.name==from);var stood=steps.Take(first).Last(st=>st.navigate).point;
            foreach(var hero in PartyController.Current.members)Place(hero,stood+Vector3.up*.05f+(hero==Lead?Vector3.zero:Vector3.right*1.4f));yield return Frames(3);
            bool rolled=false;
            yield return Play(Slice(route,from,"let the last word go"),180,()=>{if(Credits.Current!=null&&Credits.Current.Running)rolled=true;});
            for(float end=Time.unscaledTime+6;!rolled&&Time.unscaledTime<end;){if(Credits.Current!=null&&Credits.Current.Running)rolled=true;yield return null;}
            Dump();
            Assert.IsTrue(finished,"route 69 never reached Jun's last word");
            CollectionAssert.IsEmpty(Failures,"route 69 failed a step to the last word: "+string.Join(" | ",Failures));
            Assert.IsTrue(Flag("game.complete"),"Jun's last line ended without the game complete");
            Assert.IsTrue(rolled,"the last word was said and the credits never began");
        }

        // ================================================================== what playing them found in the game
        static IEnumerator Yard(bool flight)
        {
            SceneFlow.Current.LoadZone(flight?"Arena_Flight":"Arena_Ground");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            foreach(var left in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(left.gameObject);
            yield return new WaitForSecondsRealtime(.6f);
            if(!flight){var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Meret fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
            yield return Pair(29);
            foreach(var hero in PartyController.Current.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.Health.InvulnerableUntil=GameTime.Now+900;}
        }
        /// <summary>The heroes' motors driven still for so long, a frame at least.</summary>
        static IEnumerator Still(float seconds){float end=Time.unscaledTime+seconds;do{foreach(var hero in PartyController.Current.members)if(hero.Health.Alive)hero.motor.Move(Vector2.zero,false,false);yield return null;}while(Time.unscaledTime<end);}
        /// <summary>Her shield is down for a few seconds after her lance, and a person fires all through the gap: a shot fired then, still in the air as the dome rises, only burns out on it
        /// (played through the pad, every shot she gave back was one of those). What is fired at the raised dome still comes back. Built first as it was (<c>shipReturnsTheGap</c>), which must
        /// give one back.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator AShotFiredThroughTheGapOnlyBurnsOutOnHerShieldAsItRises()
        {
            int returned=0,answered=0;
            IEnumerator Once(bool broken)
            {
                yield return Yard(true);
                var her=new Vector3(-300,1,300);var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MeretShip"),her);enemy.transform.rotation=Quaternion.Euler(0,180,0);enemy.Passive=false;
                var boss=enemy.GetComponent<BossController>();boss.shipReturnsTheGap=broken;var lead=Lead;var partner=PartyController.Current.members.First(m=>m!=lead);
                Place(lead,her+Vector3.back*20);Place(partner,her+Vector3.back*50+Vector3.left*30);yield return Frames(3);
                for(float end=Time.unscaledTime+8;(!boss.ShipShieldUp||boss.Busy)&&Time.unscaledTime<end;)yield return Still(0);
                Assert.IsTrue(boss.ShipShieldUp,"fixture: her shield is up");
                for(float end=Time.unscaledTime+6;!boss.Perform("Lance")&&Time.unscaledTime<end;)yield return null;
                for(float end=Time.unscaledTime+4;boss.ShipShieldUp&&Time.unscaledTime<end;)yield return Still(0);
                Assert.IsFalse(boss.ShipShieldUp,"fixture: her lance dropped her shield");
                int before=log.Count(l=>l.Contains("MERET_RETURN"));
                // Fired all through the gap, a tenth of a second apart, and for a tenth after it closes (no thumb comes off a trigger faster).
                float lastShot=0,closed=-1;
                for(float end=Time.unscaledTime+6;Time.unscaledTime<end;)
                {
                    if(boss.ShipShieldUp&&closed<0)closed=Time.unscaledTime;
                    if(closed>0&&Time.unscaledTime>closed+.1f)break;
                    if(Time.unscaledTime-lastShot>=.1f){lastShot=Time.unscaledTime;var aim=(enemy.transform.position-lead.transform.position).normalized;
                        Projectile.Fire(lead.transform.position+Vector3.up*.9f+aim*2,aim,new DamagePacket{source=lead.Health,amount=60,type=DamageType.Beam},24);}
                    yield return Still(0);
                }
                Assert.Greater(closed,0,"fixture: her shield came back up");
                yield return Still(1.6f);returned=log.Count(l=>l.Contains("MERET_RETURN"))-before;
                // One fired at the raised dome, well after it rose: that comes back.
                if(!broken&&boss.ShipShieldUp)
                {
                    var aim=(enemy.transform.position-lead.transform.position).normalized;
                    Projectile.Fire(lead.transform.position+Vector3.up*.9f+aim*2,aim,new DamagePacket{source=lead.Health,amount=60,type=DamageType.Beam},24);
                    yield return Still(1.6f);answered=log.Count(l=>l.Contains("MERET_RETURN"))-before-returned;
                }
                enemy.Passive=true;Object.Destroy(enemy.gameObject);yield return Frames(2);
            }
            yield return Once(true);
            Assert.Greater(returned,0,"fixture: built as it was, nothing fired through the gap came back: the check below proves nothing");
            yield return Once(false);
            Assert.AreEqual(0,returned,"a shot fired while her shield was down came back as it rose");
            Assert.Greater(answered,0,"a shot fired at her raised shield didn't come back: the dome's answer is gone");
        }
        /// <summary>A coupling releases a held third: its collar is offered at an overreach only while her bar stands held at its line, and opened it lets the bar through that line and takes
        /// nothing more (D281). Played through the pad both couplings were offered and opened at her first two overreaches, at 89% and 81% of her bar, each taking the bar to its line (61 s
        /// against a floor of 80), and once the bar paid in blows the frame stood bare for the rest. Built first as it was (<c>gantryCouplingTakes</c>): offered at 90% and taking the bar to
        /// two thirds.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator ACouplingIsOfferedOnlyWhileHerBarIsHeldAndTakesNothingOffIt()
        {
            float after=0,max=1;bool offeredOver=false,offeredHeld=false;
            IEnumerator Weld(BossController boss,EnemyBrain enemy,CombatActor lead,float bar)
            {
                enemy.Passive=true;for(float end=Time.unscaledTime+6;(boss.Busy||enemy.Health.Broken)&&Time.unscaledTime<end;)yield return Still(0);
                enemy.Health.integrity=bar;Place(lead,enemy.transform.position+Vector3.forward*8);yield return Frames(2);
                Assert.IsTrue(boss.Perform("Weld"),"fixture: her weld");
                // Run out to its end, the welding arm at full stretch: her frame is open (an overreach always opens it).
                for(float end=Time.unscaledTime+6;!enemy.Health.Broken&&Time.unscaledTime<end;)yield return Still(0);
                Assert.IsTrue(enemy.Health.Broken,"fixture: her weld ran out and left the welding arm overreached");
            }
            IEnumerator Once(bool broken)
            {
                yield return Yard(false);
                var at=new Vector3(200,.1f,0);var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MeretGantry"),at);enemy.Passive=true;yield return Frames(3);
                var boss=enemy.GetComponent<BossController>();boss.gantryCouplingTakes=broken;var body=enemy.Health;max=body.maximum;
                var lead=Lead;var partner=PartyController.Current.members.First(m=>m!=lead);Place(partner,at+Vector3.right*30);var seat=boss.GantryWelder;
                // Over her line (90%): the weld overreaches, and is the collar offered?
                yield return Weld(boss,enemy,lead,max*.9f);yield return Still(.4f);offeredOver=seat.Offered;
                if(offeredOver)
                {
                    enemy.Passive=false;Place(lead,seat.transform.position);yield return Frames(2);seat.Interact();
                    for(float end=Time.unscaledTime+4;!seat.IsOpen&&Time.unscaledTime<end;)yield return Still(0);
                    after=body.integrity;
                }
                else
                {
                    // Held at its first line: offered, and opened it lets the bar through.
                    yield return Weld(boss,enemy,lead,max*2/3f);yield return Still(.4f);offeredHeld=seat.Offered;
                    if(offeredHeld){enemy.Passive=false;Place(lead,seat.transform.position);yield return Frames(2);seat.Interact();
                        for(float end=Time.unscaledTime+4;!seat.IsOpen&&Time.unscaledTime<end;)yield return Still(0);}
                    after=body.integrity;
                }
                enemy.Passive=true;Object.Destroy(enemy.gameObject);yield return Frames(2);
            }
            yield return Once(true);
            Assert.IsTrue(offeredOver,"fixture: built as it was, the collar wasn't offered over her line: the check below proves nothing");
            Assert.Less(after,max*2/3f,"fixture: built as it was, the coupling took nothing: the check below proves nothing");
            yield return Once(false);
            Assert.IsFalse(offeredOver,"the collar was offered at an overreach with her bar at 90%, over its line: two thirds of her bar went that way, with no blow");
            Assert.IsTrue(offeredHeld,"with her bar held at its line her weld's overreach offered no collar");
            Assert.Less(after,max*2/3f,"the coupling opened at her held line and the bar didn't go through it");
            Assert.Greater(after,max*2/3f-max*.02f,$"the coupling opened at her held line and took the bar to {after/max:P0}: it lets the line through and takes nothing more");
        }

        /// <summary>Her frame beaten, the lift's cage goes down the shaft with her and comes back up empty fourteen seconds later. A party at the lift's head is told so (the prompt says the cage
        /// is on its way back, and a press is refused), and offered the ride once it is back. Built first as it was (<c>silentWhileAway</c>: nothing offered while it was away).</summary>
        [UnityTest,Timeout(300000)] public IEnumerator WhileTheCageIsAwayWithHerTheLiftsHeadSaysSoAndOffersTheRideOnceItIsBack()
        {
            string said=null,saw="";bool offeredRide=false;
            IEnumerator Once(bool broken)
            {
                GameServices.Current.Flags.SetBool("bossdown.MeretGantry",false);
                yield return Zone("StillwaterChamber","Gantry",FlagsBefore(ChamberRoute,"north through the gantry's seal"),29);
                // Her frame gone already (the flag below is her fall): nothing of her fight stands to hold the lift.
                foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
                foreach(var volume in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))volume.enabled=false;yield return Frames(3);
                var lift=Object.FindObjectsByType<ChamberLift>(FindObjectsSortMode.None).Single();lift.silentWhileAway=broken;
                foreach(var hero in PartyController.Current.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;}
                Place(Lead,lift.transform.position+Vector3.back*1.6f);yield return Frames(3);
                GameServices.Current.Flags.SetBool("bossdown.MeretGantry",true);said=null;offeredRide=false;bool wasAway=false;
                for(float end=Time.unscaledTime+18;Time.unscaledTime<end&&!offeredRide;)
                {
                    // The heroes are held on autopilot here: ask the prompt service what it offers where the lead stands, as the player's brain does each frame.
                    PromptService.FindInteraction(Lead.transform.position);var offered=PromptService.Interaction;
                    if(lift.Away)wasAway=true;
                    if(lift.Away&&offered==lift)said=offered.prompt;
                    saw=$"away {lift.Away} available {lift.Available} travel {lift.Travel:0.00} prompt '{lift.prompt}' offered '{(offered!=null?offered.name:"nothing")}' fight {BossController.AnyFightUnderway} lead {Lead.transform.position:F1} lift {lift.transform.position:F1}";
                    // Back up from taking her: the ride is offered again (before her going is seen, it is the ride as it stood).
                    if(wasAway&&!lift.Away&&offered==lift&&lift.prompt.StartsWith("Ride down"))offeredRide=true;
                    yield return Still(0);
                }
                lift.silentWhileAway=false;
            }
            yield return Once(true);
            Assert.IsNull(said,"fixture: built as it was, the lift's head offered something while the cage was away: the check below proves nothing");
            yield return Once(false);
            Assert.AreEqual("The cage is on its way back up",said,"while the cage was away with her the lift's head said nothing (last seen: "+saw+")");
            Assert.IsTrue(offeredRide,"the cage came back and the ride down wasn't offered");
        }

        // ================================================================== the chamber's bed after the release
        static AudioSource BedSource=>GameServices.Current.GetComponents<AudioSource>().Single(s=>s.loop&&s.outputAudioMixerGroup!=null&&s.outputAudioMixerGroup.name=="Ambience");
        /// <summary>The chamber's bed is its steel under the anchor's load. Held, it plays at the zone's own level; let go, it comes down with the warm turn to a low level over a few seconds,
        /// the same clip playing on (a swap cuts it and starts it again); a load after the release starts at that low level. Built first as it was (nothing at the release changed the bed:
        /// <c>bedAfter</c> 1), which must leave it at the zone's level.</summary>
        [UnityTest,Timeout(300000)] public IEnumerator AtTheReleaseTheSteelsBedComesDownWithTheWarmLightAndALoadAfterItStartsThere()
        {
            var flags=FlagsBefore(ChamberRoute,"to the last collar");
            float level=0,lowest=0;bool restarted=false;
            IEnumerator Release(bool broken)
            {
                if(DialogueSystem.Current!=null&&DialogueSystem.Current.Running)DialogueSystem.Current.Cancel();GameServices.Current.Input.Blocked=false;
                foreach(string flag in new[]{"end.collarTaken","end.anchorReleased","end.custody"})GameServices.Current.Flags.SetBool(flag,false);
                yield return Zone("StillwaterChamber","Well",flags,29);
                var anchor=AnchorRelease.Current;Assert.IsNotNull(anchor,"fixture: the chamber has its anchor");Assert.AreEqual(AnchorRelease.Stage.Holding,anchor.Now,"fixture: the anchor is held");
                var bed=BedSource;level=ZoneController.Current.definition.ambientVolume;
                Assert.IsNotNull(bed.clip,"fixture: the chamber has its bed");Assert.AreEqual(level,bed.volume,.001f,"held, the bed isn't at the zone's own level");
                if(broken)anchor.bedAfter=1;
                var clip=bed.clip;float last=bed.time;restarted=false;
                GameServices.Current.Flags.SetBool("end.collarTaken",true);
                lowest=level;
                for(float end=Time.unscaledTime+12;Time.unscaledTime<end;)
                {
                    if(bed.clip!=clip||!bed.isPlaying||bed.time+.05f<last&&bed.time>.5f)restarted=true;last=bed.time;lowest=Mathf.Min(lowest,bed.volume);
                    yield return null;
                }
            }
            yield return Release(true);
            Assert.Greater(lowest,level*.9f,"fixture: built as it was, the bed came down at the release all the same: the check below proves nothing");
            yield return Release(false);
            Assert.IsFalse(restarted,"the bed's clip was swapped or restarted at the release");
            Assert.Less(lowest,level*.5f,$"the bed stayed at {lowest:0.000} through the release (the zone's level {level:0.000}): the steel still groans under a load that's gone");
            Assert.Greater(lowest,0,"the bed went silent: it eases down, it doesn't stop");
            // A load after it: at the low level from the first.
            GameServices.Current.Flags.SetBool("end.anchorReleased",true);
            SceneFlow.Current.LoadZone("StillwaterChamber","Well");float deadline=Time.unscaledTime+40;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(lowest,BedSource.volume,.01f,"loaded after the release, the bed isn't at the low level the release left it at");
        }

        /// <summary>Each fight against its sheet: she falls (or yields), inside the sheet's bounds through the pad, at no less than nine tenths of the share the sheet carries, and the party
        /// standing at the end of it.</summary>
        static void Judge(Account a,float flat,string id)
        {
            var sheet=ContentRegistry.Boss(id);float integrity=GameCatalog.Find<EnemyDef>(id).integrity;
            Assert.Greater(a.woke,0,$"{id}'s fight never began");
            Assert.AreEqual(0,a.defeats,$"the party fell to {id}");
            Assert.Greater(a.fell,0,$"{id} never fell through the pad");
            float seconds=a.fell-a.woke,share=integrity/seconds/flat;
            Assert.That(seconds,Is.InRange(sheet.minSeconds,sheet.maxSeconds),$"{id} lasted {seconds:0} s through the pad: her sheet's bounds are {sheet.minSeconds:0} to {sheet.maxSeconds:0}");
            Assert.GreaterOrEqual(share,sheet.driverShare*.9f,$"the driver got {share:0.00} of flat out against {id}, under the sheet's {sheet.driverShare:0.00}");
            Assert.AreEqual(0,a.leadFalls,$"the lead fell {a.leadFalls} times to {id}");
        }
    }
}
