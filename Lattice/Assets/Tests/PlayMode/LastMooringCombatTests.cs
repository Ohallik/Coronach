using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 10's ordinary fights (LastMooringBuilder.Enemies.cs), each on a bare floor or open sky in the proving yards with what it needs made here: nothing waits on Sorrel, Last
    /// Mooring, the Continuance or the passage being built (their contracts are in each zone's map doc and the wiring's section 5). The party is the one the chapter is met by: chapter 10's
    /// arrival (DevLoadout chapter10: three heroes, Taren and Idra out, Choirwrought parts). The hit sizes on all three heroes from Sync 27 to 30; the XP ledger walked with the definitions as
    /// they stand; one test for each enemy, saying what a player sees: the wardens' pair, bracing and covering and never striking across each other, each standing aside at nought; the weld
    /// arm's burning seam and its rest; the rig's ring; the interceptors pinning and striking, freed by a shot or a chain; the tow-drone hauling its catch away; two clamp guns whose curtains
    /// cross, so neither has a lee; the tender closing what the lead holds; and the guns going quiet over a hero docked on their clamp's shelf, where the shelf's tender is his fight. Each test
    /// builds its fixture wrong under MOORING_COMBAT_BROKEN=1, so a run with it set shows every one failing the way it should. The bosses' tests are their engineers', in their classes.</summary>
    public sealed class LastMooringCombatTests
    {
        const float X=230;
        const string Warden="StillwaterWarden",WeldArm="StillwaterWeldArm",Rig="StillwaterRig",Interceptor="StillwaterInterceptor",TowDrone="StillwaterTowDrone",Gun="ClampGun",Tender="StillwaterTender";
        static readonly string[] Ordinary={Warden,WeldArm,Rig,Interceptor,TowDrone,Gun,Tender};
        readonly List<string> log=new();
        static readonly string[] Heeded={"TETHER_","ANCHOR_","CURTAIN_","RIDER_","MACHINE_","WARDEN_","GUARD_","BOSS_LEAVES","PAIR_","PIN_","STRAFER_","TENDER_","COMBAT_KILL","BERTH_"};
        void Heard(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        /// <summary>Off unless this class's own run sets it (MOORING_COMBAT_BROKEN=1): each test then builds its fixture wrong, so the run shows every test failing the way it should.</summary>
        static bool Broken=>System.Environment.GetEnvironmentVariable("MOORING_COMBAT_BROKEN")=="1";
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter10");log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;SceneManager.sceneLoaded-=ShelfYard;BridleClamp.Slack=false;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            LingeringArea.DismissAll();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void MooringEnemies(ContentRegistry.Book book)
        {
            // Where a hero stands for each ordinary enemy's tell: inside the warden's baton (alone, a warden of a pair fights as one), on the weld arm's strip ahead of it, inside the rig's
            // ring and the tender's torch; flying inside the interceptor's run-in, the drone's harpoon, and the clamp gun's first row (it marks the first and third on its first beat).
            book.Tell(Warden,2);book.Tell(WeldArm,5);book.Tell(Rig,2);book.Tell(Tender,2);
            book.Tell(Interceptor,8,flight:true);book.Tell(TowDrone,8,flight:true);book.Tell(Gun,4,flight:true);
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Hold();yield return new WaitForSecondsRealtime(.5f);
        }
        /// <summary>The heroes out are the test's to move: no pad, no partner's brain.</summary>
        static void Hold(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        static IEnumerator Deploy(params string[] pair)
        {
            Assert.IsTrue(Deployment.Set(State,pair),"fixture: the pair "+string.Join(" and ",pair)+" can be put out");
            PartyField.Reform(PartyController.Current);yield return Frames(3);PartyController.Current.RefreshStats();yield return null;Hold();
            foreach(string hero in pair)Assert.IsTrue(PartyController.Current.members.Any(m=>m.character==hero),"fixture: "+hero+" is out");
        }
        static IEnumerator Level(int sync)
        {
            foreach(var member in State.party)member.level=sync;PartyController.Current.RefreshStats();yield return null;
            foreach(var hero in PartyController.Current.members)Assert.AreEqual(Levels.MaxIntegrity(sync),hero.Health.maximum,.5f,"fixture: "+hero.character+"'s bar at Sync "+sync);
        }
        static IEnumerator Frame(string id)
        {
            var part=GameCatalog.Find<TechPartDef>(id);Assert.IsNotNull(part,"fixture: "+id+" is defined (ProgressionBuilder)");var inv=new Inventory(State);
            foreach(var member in State.party)Assert.IsTrue(inv.Equip(member,inv.GivePart(part),part),"fixture: "+member.id+" wears "+id);
            PartyController.Current.RefreshStats();yield return null;
        }
        static Vector3 At(float x,float y,float z)=>new(X+x,y,z);
        static Vector3 Sky(float x,float z)=>new(200+x,1,z);
        static void Floor()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Mooring fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static GameObject Block(Vector3 centre,Vector3 size)
        {var wall=new GameObject("Mooring fixture block",typeof(BoxCollider));wall.transform.position=centre;wall.GetComponent<BoxCollider>().size=size;wall.isStatic=true;Physics.SyncTransforms();return wall;}
        static (CombatActor lead,CombatActor partner) Party(){var lead=PartyController.Current.Active;return (lead,PartyController.Current.members.First(m=>m!=lead));}
        static void Put(CombatActor actor,Vector3 position,bool fresh=true)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            if(fresh){actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;}Physics.SyncTransforms();
        }
        static EnemyDef Def(string id){var definition=GameCatalog.Find<EnemyDef>(id);Assert.IsNotNull(definition,"fixture: "+id+" is defined (LastMooringBuilder.Definitions)");return definition;}
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true,float yaw=0){var enemy=ActorFactory.Enemy(Def(id),at);enemy.transform.rotation=Quaternion.Euler(0,yaw,0);enemy.Passive=passive;return enemy;}
        static float Hit(Health body,CombatActor from,float amount=100,float breakPower=0,DamageType? type=null)=>body.Receive(new DamagePacket{source=from.Health,amount=amount,type=type??Neutral(body),breakPower=breakPower});
        static DamageType Neutral(Health body)=>System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>Waits on the fight's own clock, driving the heroes' motors as their brains would.</summary>
        static IEnumerator Until(System.Func<bool> done,float seconds,params CombatActor[] held)
        {
            for(float end=Time.unscaledTime+seconds*1.5f+1,until=Time.time+seconds;!done()&&Time.time<until&&Time.unscaledTime<end;)
            {foreach(var actor in held)if(actor!=null&&actor.Health.Alive)actor.motor.Move(Vector2.zero,false,false);yield return null;}
        }
        static IEnumerator Wait(float seconds,params CombatActor[] held)=>Until(()=>false,seconds,held);
        static bool Faint(Telegraph marking)=>marking.GetComponent<MeshRenderer>().sharedMaterials[1].name.Contains("faint");
        bool Said(string start)=>log.Any(l=>l.StartsWith(start));
        int Count(string start)=>log.Count(l=>l.StartsWith(start));

        // ------------------------------------------------------------------ the chapter's loadout and its numbers
        [UnityTest] public IEnumerator TheChapterTenLoadoutArrivesAsThreeAtSyncTwentySevenInChoirwroughtPartsWithTarenAndIdraOut()
        {
            // Built wrong on purpose: chapter 9's arrival is the three at 25 in Tempered parts.
            if(Broken)DevLoadout.Apply("chapter9");
            yield return Zone("Arena_Ground");
            CollectionAssert.AreEquivalent(new[]{"Taren","Sela","Idra"},State.party.Select(m=>m.id).ToArray(),"chapter 10 arrives without its three heroes");
            foreach(var member in State.party)Assert.AreEqual(27,member.level,member.id+" arrives at the wrong Sync: the plan ends chapter 9 at 27");
            CollectionAssert.AreEquivalent(new[]{"Taren","Idra"},PartyController.Current.members.Select(m=>m.character).ToArray(),"the pair out isn't the one the join leaves out");
            foreach(var member in State.party)
            {
                string worn=member.equipped.TryGetValue(GearSlot.Frame,out var id)?State.parts.FirstOrDefault(p=>p.instanceId==id)?.definitionId:null;
                Assert.AreEqual("FramesT5",worn,member.id+" doesn't arrive in the Choirwrought frame the chapter's numbers are sized on");
            }
            Assert.IsTrue(GameServices.Current.Flags.GetBool("chapter9.complete"),"chapter 9 isn't told");Assert.IsFalse(GameServices.Current.Flags.GetBool("chapter10.complete"),"chapter 10 arrives already told");
        }
        /// <summary>The standing rule (CHAPTER_GUIDE section 0.1, ch10-wiring section 5): an ordinary hit costs a hero 9 to 13% of the bar. Measured through Health.Receive as a real blow lands,
        /// Plating and all, on all three heroes (Idra, the guard hero, is built with more Plating, so she is the floor and Taren and Sela the ceiling), in the Choirwrought parts the chapter
        /// arrives in, at Sync 27 (its arrival), 29 (Meret) and 30 (its end). A hit that hurts nobody (the Interceptor's tractor line: the pass it holds a ship for does) is left out by being
        /// authored at nought, and must be. Every chapter 10 zone is at threat 1, every capsule is at least twice its radius tall, no guardian's arc reaches 180 degrees, and every one of the
        /// seven is drawn in the body it was given (a body taken in for chapters 7 to 9; nothing is generated): never a grey stand-in.</summary>
        [UnityTest] public IEnumerator EveryOrdinaryHitTakesANinthToAnEighthOfAnyHerosBarFromSyncTwentySevenToThirty()
        {
            yield return Zone("Arena_Ground");Floor();
            var source=Enemy(Rig,At(40,.05f,40)).Health;yield return Frames(2);
            var attacks=Ordinary.SelectMany(id=>Def(id).attacks.Where(a=>a!=null&&a.damage>0).Select(a=>(id,what:a.name,damage:a.damage))).ToList();
            // Built wrong on purpose: chapter 9's Warden baton, 128, is under the band for Idra from Sync 28, and the check has to say so.
            if(Broken)attacks.Add(("fixture","chapter 9's baton",128));
            var failures=new List<string>();
            yield return Frame("FramesT5");
            foreach(var pair in new[]{new[]{"Taren","Idra"},new[]{"Sela","Idra"}})
            {
                yield return Deploy(pair);
                foreach(int sync in new[]{27,29,30})
                {
                    yield return Level(sync);
                    foreach(var hero in PartyController.Current.members)
                    {
                        if(pair[0]=="Sela"&&hero.character=="Idra")continue;
                        float bar=hero.Health.maximum;
                        foreach(var (id,what,damage) in attacks)
                        {
                            hero.Health.integrity=bar;hero.Health.InvulnerableUntil=0;
                            float share=hero.Health.Receive(new DamagePacket{source=source,amount=damage,type=DamageType.Kinetic,weight=HitWeight.Medium})/bar;
                            string line=$"MOORING_HIT {hero.character} sync={sync} plating={hero.plating:0} {id} {what} {damage:0} share={share:P1}";TestContext.WriteLine(line);
                            if(share<.09f||share>.13f)failures.Add(line+": the plan asks 9 to 13%");
                        }
                    }
                }
            }
            foreach(string id in Ordinary)foreach(var attack in Def(id).attacks)if(attack!=null&&attack.damage<=0&&!(id==Interceptor&&attack.name=="StillwaterTractor"))failures.Add($"{id}'s {attack.name} hurts nobody: only the tractor line is meant to");
            foreach(string zone in new[]{"SorrelSecondSky","LastMooring","Continuance","StillwaterChamber","ReturningPassage","NeighbourhoodAfloat"})
            {var z=GameCatalog.Find<ZoneDef>(zone);if(z!=null&&!Mathf.Approximately(z.enemyThreat,1))failures.Add($"{zone} is at threat {z.enemyThreat}: chapter 10's damage is authored at its real weight, at 1");}
            var bodies=new Dictionary<string,string>{[Warden]="CompactWarden",[WeldArm]="WeldArm",[Rig]="OverrideRig",[Interceptor]="Interceptor",[TowDrone]="TowDrone",[Gun]="ClampGun",[Tender]="StillwaterTender"};
            foreach(string id in Ordinary)
            {
                var def=Def(id);float round=def.capsuleRadius>0?def.capsuleRadius:.55f;float tall=def.capsuleHeight>0?def.capsuleHeight:1.5f;
                if(tall<round*2)failures.Add($"{id}'s capsule is {tall} m tall on a {round} m radius: Unity stretches it and the body floats");
                if(def.behaviour==EnemyBehaviourKind.Guardian&&def.tune.arc>=180)failures.Add($"{id}'s guarded arc is {def.tune.arc}: a flank blow must land whole");
                if(def.weakness==def.resistance)failures.Add($"{id} is weak to what it resists");
                if(string.IsNullOrWhiteSpace(def.displayName)||DisplayNames.Target(id)!=def.displayName)failures.Add($"{id} has no name of its own on screen: '{DisplayNames.Target(id)}'");
                // Built wrong on purpose: as if the Warden were still drawn as the Pikeguard chapter 9's stood in for.
                string body=def.prefab!=null?def.prefab.name:"nothing";if(Broken&&id==Warden)body="MusterPikeguard";
                if(body!=bodies[id])failures.Add($"{id} is drawn as {body}, not as {bodies[id]}");
                var made=Enemy(id,At(-30,.05f,-30));yield return Frames(2);
                if(made.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("ART_PENDING"))||!made.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&!(r is LineRenderer)))
                    failures.Add($"{id} stands as a grey stand-in, not its body");
                Object.Destroy(made.gameObject);yield return Frames(2);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the ledger
        /// <summary>The chapter's road, as the wiring's section 5 and LastMooringBuilder.Enemies.cs plan it: what is fought on the main path in order, each boss's door where it falls, then the two
        /// side quests. A boss not yet defined counts at the XP the ledger assumes for it (and says so); everything else is read off its definition.</summary>
        static readonly (string id,int count)[] Road=
        {
            (Interceptor,4),                                    // Sorrel: the valley's two lane pairs
            ("@MotherOfDoors",0),(Gun,8),(Tender,6),("MotherOfDoors",1),
            (Interceptor,4),(Gun,2),                            // Last Mooring: two lane pairs, the gap's crossed guns
            ("@Tollkeeper",0),("Tollkeeper",1),
            (Warden,2),(Warden,2),(WeldArm,1),(Rig,1),          // the Continuance: the mess, the works
            ("@MeretShip",0),("MeretShip",1),
            ("@MeretGantry",0),("MeretGantry",1),
            ("@MeretWell",0),("MeretWell",1),
            (TowDrone,3),                                       // the passage: the tug
            ("quest:TurnBack",1),("@main path",0),
            ("quest:LastSurvey",1),("quest:Homecomings",1),("@side quests",0),
        };
        static readonly Dictionary<string,int> AssumedBossXp=new(){["MotherOfDoors"]=1500,["Tollkeeper"]=1600,["MeretShip"]=1200,["MeretGantry"]=1300,["MeretWell"]=1400};
        /// <summary>Where the ledger puts the party at each point (LastMooringBuilder.Enemies.cs).</summary>
        static readonly Dictionary<string,float> Planned=new(){["MotherOfDoors"]=27.11f,["Tollkeeper"]=28.09f,["MeretShip"]=28.78f,["MeretGantry"]=29.10f,["MeretWell"]=29.46f,["main path"]=29.91f,["side quests"]=30.02f};
        [UnityTest] public IEnumerator TheLedgerBringsThePartyToEachBossAtTheSyncItsSheetAssumes()
        {
            yield return null;
            // Built wrong on purpose: the road fought with chapter 9's crews and machines at chapter 9's pay.
            var swap=Broken?new Dictionary<string,string>{[Warden]="CompactWarden",[WeldArm]="WeldArm",[Rig]="OverrideRig",[Interceptor]="Interceptor",[TowDrone]="TowDrone",[Gun]="FlakGate",[Tender]="ClampTender"}:new Dictionary<string,string>();
            int level=27,xp=0;var failures=new List<string>();
            float Sync()=>level+xp/(float)Levels.RequiredXp(level);
            void Pay(int amount){xp+=amount;while(xp>=Levels.RequiredXp(level)){xp-=Levels.RequiredXp(level);level++;}}
            foreach(var (step,count) in Road)
            {
                if(step.StartsWith("@"))
                {
                    string at=step.Substring(1);float sync=Sync();
                    string line=$"MOORING_LEDGER {at} sync={sync:0.00} planned={Planned[at]:0.00}";
                    if(ContentRegistry.Expected.bosses.TryGetValue(at,out var sheet)){line+=$" sheet={sheet.sync}";if(Mathf.Abs(sync-sheet.sync)>.6f)failures.Add($"the party meets {at} at Sync {sync:0.00} and its sheet assumes {sheet.sync}");}
                    TestContext.WriteLine(line);
                    if(Mathf.Abs(sync-Planned[at])>.15f)failures.Add($"the party reaches {at} at Sync {sync:0.00}, where the ledger plans {Planned[at]:0.00}");
                    continue;
                }
                if(step.StartsWith("quest:"))
                {
                    var quest=GameCatalog.Find<QuestDef>(step.Substring(6));if(quest==null){failures.Add($"fixture: {step} is defined");continue;}
                    if(quest.id=="TurnBack"&&quest.rewardXp!=0)failures.Add($"the main quest pays {quest.rewardXp}: the ledger has it pay nothing (the climb is the fights')");
                    Pay(quest.rewardXp*count);continue;
                }
                string id=swap.TryGetValue(step,out var other)?other:step;var def=GameCatalog.Find<EnemyDef>(id);
                if(def!=null)Pay(def.xp*count);
                else if(AssumedBossXp.TryGetValue(id,out int assumed)){Pay(assumed*count);TestContext.WriteLine($"MOORING_LEDGER {id} isn't defined yet: counted at the {assumed} the ledger assumes");}
                else failures.Add($"{id} is on the road and isn't defined");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the Continuance's crews
        [UnityTest] public IEnumerator AWardenPairBracesAndCoversNeverStrikesAcrossItselfAndEachStandsAsideAtNought()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,5));
            // Built wrong on purpose: chapter 9's Warden, who fights alone.
            string id=Broken?"CompactWarden":Warden;
            var a=Enemy(id,At(-2.1f,.05f,0));var b=Enemy(id,At(2.1f,.05f,0));yield return Frames(3);
            var ga=a.Behaviour as EnemyGuardian;var gb=b.Behaviour as EnemyGuardian;Assert.IsNotNull(ga,"a Compact Warden isn't a guardian");
            a.Passive=b.Passive=false;
            // Before them: one braces its shield drive down a marked line at him, and the other covers it, plate to him, striking nothing.
            yield return Until(()=>Said("PAIR_BRACE"),5,hero);
            Assert.IsTrue(Said("PAIR_FORMED "+Warden),"two wardens woken together didn't stand as a pair");Assert.IsTrue(Said("PAIR_BRACE "+Warden),"neither braced its drive at a hero before them");
            var bracer=a.Telegraphing?a:b;var cover=bracer==a?b:a;var coverGuard=cover==a?ga:gb;
            Assert.IsTrue(bracer.Telegraphing&&bracer.AttackMark.Shape==Telegraph.Kind.Line,"the brace isn't a marked line");Assert.IsTrue(bracer.AttackMark.Contains(hero.transform.position),"the brace isn't laid at him");
            Assert.GreaterOrEqual(bracer.TellDuration,.95f,"the drive comes too quick to read");Assert.IsFalse(cover.Telegraphing,"both struck at once: one braces, the other covers");
            Assert.IsTrue(coverGuard.Guarding&&Vector3.Angle(cover.transform.forward,Vector3.ProjectOnPlane(hero.transform.position-cover.transform.position,Vector3.up))<70,"the one covering doesn't keep its plate to him");
            yield return Until(()=>!bracer.Telegraphing,2,hero);yield return Frames(2);Assert.Less(hero.Health.integrity,5000,"the drive missed the hero it was laid at");
            // Between them: neither strikes across the other, and from there both their flanks are his.
            float whole=hero.Health.integrity;Put(hero,(a.transform.position+b.transform.position)*.5f+Vector3.up*.05f,false);
            yield return Until(()=>Said("PAIR_SPLIT"),1.5f,hero);Assert.IsTrue(Said("PAIR_SPLIT "+Warden),"a hero between them isn't seen as between them");
            bool struckAt=false;for(float end=Time.time+1.3f;Time.time<end;){hero.motor.Move(Vector2.zero,false,false);struckAt|=(a.Telegraphing&&a.AttackMark.Contains(hero.transform.position))||(b.Telegraphing&&b.AttackMark.Contains(hero.transform.position));yield return null;}
            Assert.IsFalse(struckAt,"a warden struck across its partner at the hero between them");Assert.AreEqual(whole,hero.Health.integrity,.01f,"a hero between them was hurt");
            Assert.AreEqual(100,Hit(a.Health,hero,100),1,"from between them a blow on one's flank was taken on its plate");Assert.AreEqual(100,Hit(b.Health,hero,100),1,"from between them a blow on the other's flank was taken on its plate");
            yield return Until(()=>Said("PAIR_REFORM"),2,hero);Assert.IsTrue(Said("PAIR_REFORM "+Warden),"they never stepped apart round the hero between them");
            // Its guard broken, one backs off and stands open; at nought it stands aside and walks out, and its partner fights alone.
            var before=a.transform.position;a.Health.Receive(new DamagePacket{source=hero.Health,amount=1,type=DamageType.Plasma,breakPower=5000});
            yield return Wait(.8f,hero);Assert.IsTrue(Said("WARDEN_GIVES_GROUND "+Warden),"its guard broken, it stood its ground");
            Assert.Greater(Flat(a.transform.position,hero.transform.position)-Flat(before,hero.transform.position),2,"giving ground, it didn't back off from the hero who broke it");
            var at=a.transform.position;a.Health.Receive(new DamagePacket{source=hero.Health,amount=a.Health.maximum*3,type=DamageType.Plasma});
            Assert.IsTrue(Said("WARDEN_STANDS_ASIDE "+Warden)&&Said("BOSS_LEAVES "+Warden),"at nought it fell where it stood");
            yield return Wait(2.4f,hero);Assert.IsTrue(a!=null&&a.gameObject!=null,"it vanished at once instead of walking out");Assert.Greater(Flat(a.transform.position,at),3,"it didn't walk out of the fight");
            Assert.IsNull(gb.PairedWith,"with its partner gone the other still fights as a pair");
            yield return Wait(2,hero);Assert.IsTrue(a==null,"it never left: it stood there after its leaving");
        }
        [UnityTest] public IEnumerator AWeldArmLaysItsWeldDownItsStripLeavesTheSeamBurningAndStandsDownAtRest()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            // Built wrong on purpose: chapter 8's Pour Drone pours down its rail too, but it is broken, not given back.
            var arm=Enemy(Broken?"PourDrone":WeldArm,At(0,.05f,0));yield return Frames(3);Assert.IsInstanceOf<EnemyRider>(arm.Behaviour,"a Weld Arm isn't a rider");
            var from=arm.transform.position;
            Put(hero,At(0,.05f,5));Put(partner,At(3.6f,.05f,5));arm.Passive=false;
            yield return Until(()=>arm.Telegraphing,4,hero,partner);Assert.IsTrue(arm.Telegraphing,"it never welded at a hero on its strip");
            var strip=arm.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,strip.Shape);Assert.Less(Vector3.Angle(strip.Forward,Vector3.forward),3,"its weld isn't laid the way it was set facing");
            Assert.IsTrue(strip.Contains(hero.transform.position),"fixture: he is on its strip");Assert.IsFalse(strip.Contains(partner.transform.position),"fixture: his partner stands beside it");
            Assert.GreaterOrEqual(arm.TellDuration,.95f,"its weld comes too quick to read");
            yield return Until(()=>!arm.Telegraphing,3,hero,partner);yield return Wait(.3f,hero,partner);arm.Passive=true;
            Assert.Less(hero.Health.integrity,5000,"the weld missed the hero on its strip");Assert.AreEqual(5000,partner.Health.integrity,.01f,"the weld reached a hero beside its strip");
            var seam=LingeringArea.Over(hero.transform.position);Assert.IsNotNull(seam,"the weld left nothing on its strip");Assert.AreEqual(AreaEffect.Burn,seam.Effect,"the fresh seam doesn't burn");
            var ring=Telegraph.Live.FirstOrDefault(t=>t!=null&&ReferenceEquals(t.owner,seam));Assert.IsNotNull(ring,"the seam isn't drawn");Assert.IsFalse(Faint(ring),"a burning seam is drawn as a boundary: it hurts, so it is a danger (D217)");
            Assert.IsNull(LingeringArea.Over(partner.transform.position),"the seam lies beside the strip");Assert.Less(Flat(arm.transform.position,from),.05f,"a weld arm moved off its mount");
            // Its drive cut: the arm stays where it worked, at rest, and only the fighter goes. Its seam goes with it.
            arm.Health.Receive(new DamagePacket{source=hero.Health,amount=arm.Health.maximum*3,type=DamageType.Kinetic});
            yield return Wait(2.5f,hero,partner);
            Assert.IsTrue(Said("MACHINE_AT_REST"),"at nought it was broken, not given back");
            var rest=Object.FindObjectsByType<MachineAtRest>(FindObjectsSortMode.None);Assert.AreEqual(1,rest.Length,"nothing of the arm stands where it worked");
            Assert.IsTrue(rest[0].Settled,"the arm never settled to rest");Assert.Less(Flat(rest[0].transform.position,from),1.2f,"what stands at rest isn't where the arm worked");
            Assert.IsEmpty(rest[0].GetComponentsInChildren<Collider>(),"the arm at rest kept a fighter's collider");
            Assert.IsNull(LingeringArea.Over(hero.transform.position),"its seam outlived the arm");
        }
        [UnityTest] public IEnumerator AnOverrideRigDischargesAllRoundItsClampAndOnlyThere()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            // Built wrong on purpose: chapter 3's Snagvine lashes a fan at the one it aims at, not all round it.
            var rig=Enemy(Broken?"SnagvineNode":Rig,At(0,.05f,0));yield return Frames(3);Assert.IsInstanceOf<EnemyAnchored>(rig.Behaviour,"an Override Rig isn't anchored");
            Put(hero,At(0,.05f,4.5f));Put(partner,At(-40,.05f,-40));rig.Passive=false;
            yield return Wait(2,hero,partner);Assert.IsFalse(rig.Telegraphing,"it discharged at a hero 4.5 m off: its ring is 2.8");Assert.AreEqual(5000,hero.Health.integrity,.01f);
            Put(hero,At(0,.05f,2),false);Put(partner,At(-.5f,.05f,-2),false);
            yield return Until(()=>rig.Telegraphing,3,hero,partner);Assert.IsTrue(rig.Telegraphing,"it never discharged at a hero inside its ring");
            var ring=rig.AttackMark;Assert.AreEqual(Telegraph.Kind.Circle,ring.Shape,"its discharge isn't marked all round it");Assert.GreaterOrEqual(rig.TellDuration,.95f,"its discharge comes too quick to read");
            Assert.IsTrue(ring.Contains(hero.transform.position)&&ring.Contains(partner.transform.position),"its discharge doesn't reach both heroes at its clamp, before and behind");
            yield return Until(()=>!rig.Telegraphing,2,hero,partner);yield return Frames(2);
            Assert.Less(hero.Health.integrity,5000,"its discharge missed the hero before it");Assert.Less(partner.Health.integrity,5000,"its discharge missed the hero behind it");
        }

        // ------------------------------------------------------------------ in flight: Sorrel's valley and Last Mooring's lanes
        [UnityTest] public IEnumerator AnInterceptorPairPinsAShipWhileTheOtherRunsItsLaneThroughHimAndAShotOrAChainFreesHim()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-80,-80));Put(hero,Sky(0,13));
            // Built wrong on purpose: two of chapter 8's Lancers, strafers who don't pair.
            string id=Broken?"MusterLancer":Interceptor;
            var a=Enemy(id,Sky(-3,0));var b=Enemy(id,Sky(3,0));yield return Frames(3);var sa=a.Behaviour as EnemyStrafer;var sb=b.Behaviour as EnemyStrafer;Assert.IsNotNull(sa,"an Interceptor isn't a strafer");
            var hurtBy=new List<Health>();System.Action<Health,DamagePacket,float> hurt=(_,packet,amount)=>{if(amount>0)hurtBy.Add(packet.source);};hero.Health.Damaged+=hurt;
            try
            {
                a.Passive=b.Passive=false;
                yield return Until(()=>Said("PIN_ON"),7,hero);
                Assert.IsTrue(Said("PAIR_FORMED "+Interceptor),"two woken together didn't pair");Assert.IsTrue(Said("PIN_ON "+Interceptor),"neither pinned the ship");
                var pinner=sa.Pins?sa:sb;var striker=pinner==sa?sb:sa;var pinnerBody=pinner==sa?a:b;var strikerBody=pinner==sa?b:a;
                Assert.IsFalse(striker.Pins,"both pin: one pins, the other strikes");Assert.AreEqual(hero,pinner.Pinned,"it pinned someone else");
                Assert.IsFalse(hurtBy.Contains(pinnerBody.Health),"the tractor line itself hurt the ship: the pass it holds him for does");
                yield return Frames(2);Assert.LessOrEqual(hero.Health.Pace,EnemyStrafer.PinPace+.01f,"a pinned ship isn't held to its pinned pace");
                Assert.IsNotNull(Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).FirstOrDefault(l=>l.name=="Tractor line"),"the tractor line isn't drawn");
                // While he is pinned the striker lines up on him, marked.
                yield return Until(()=>strikerBody.Telegraphing,EnemyStrafer.PinHolds,hero);
                Assert.IsTrue(strikerBody.Telegraphing,"its partner pinned a ship and the striker never ran at him");Assert.IsTrue(strikerBody.AttackMark.Contains(hero.transform.position),"the striker's lane isn't laid through the pinned ship");
                Assert.GreaterOrEqual(strikerBody.TellDuration,.85f,"the run comes too quick to read");
                // The first answer: shoot the one holding the line.
                Hit(pinnerBody.Health,hero,20);yield return Frames(2);Assert.IsTrue(Said("PIN_OFF "+Interceptor+" cut"),"a hit on the pinner didn't cut its line");Assert.IsNull(pinner.Pinned,"cut, it still pins him");
                // The second: something solid between them (Last Mooring's chains) breaks it.
                log.RemoveAll(l=>l.StartsWith("PIN_"));
                yield return Until(()=>Said("PIN_ON"),9,hero);Assert.IsTrue(Said("PIN_ON"),"fixture: it pins him again");
                var between=(pinnerBody.transform.position+hero.transform.position)*.5f;Block(between,new Vector3(4,6,4));
                yield return Until(()=>Said("PIN_OFF"),2,hero);Assert.IsTrue(Said("PIN_OFF "+Interceptor+" cover"),"a chain between them didn't break the line");
            }
            finally{hero.Health.Damaged-=hurt;}
        }

        // ------------------------------------------------------------------ the passage: the tug
        [UnityTest] public IEnumerator ATowDroneHarpoonsAShipAndHaulsItAwayTheWayItTowsUntilTheLineIsCut()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-60,-60));Put(hero,Sky(0,9));
            // Built wrong on purpose: chapter 3's Claim Drone reels a ship in to itself and hauls nothing.
            var drone=Enemy(Broken?"ClaimDrone":TowDrone,Sky(0,0),true,90);yield return Frames(3);var tether=drone.Behaviour as EnemyTetherer;Assert.IsNotNull(tether,"a Tow-drone isn't a tetherer");
            drone.Passive=false;yield return Until(()=>drone.Telegraphing,5,hero);Assert.IsTrue(drone.Telegraphing,"it never harpooned at a ship 9 m off");
            var line=drone.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,line.Shape,"its harpoon isn't marked as a line");Assert.IsTrue(line.Contains(hero.transform.position),"its line isn't laid to the ship");
            Assert.GreaterOrEqual(drone.TellDuration,.85f,"its harpoon is too quick to read");
            yield return Until(()=>Said("TETHER_ON"),2,hero);Assert.IsTrue(Said("TETHER_ON"),"its harpoon missed the ship it was laid to");Assert.Less(hero.Health.integrity,5000,"the catch is a marked hit, and it did nothing");
            yield return Frames(2);Assert.Less(hero.Health.Pace,.5f,"a caught ship isn't held to under half its speed");
            // Set facing east, away from the tug: it hauls its catch east.
            var from=hero.transform.position;var drawnFrom=drone.transform.position;
            yield return Wait(2.4f,hero);
            float hauled=hero.transform.position.x-from.x;TestContext.WriteLine($"TOW_HAULED ship={hauled:0.00} drone={drone.transform.position.x-drawnFrom.x:0.00}");
            Assert.Greater(hauled,2.5f,"a caught ship isn't hauled the way the drone tows: it should be carried off with it, not only reeled in");
            Assert.Greater(drone.transform.position.x-drawnFrom.x,4.5f,"holding a catch, the drone doesn't haul it anywhere");
            Hit(drone.Health,hero,20);yield return Frames(2);
            Assert.IsTrue(Said("TETHER_OFF "+TowDrone+" cut"),"a hit on the drone didn't cut its line");Assert.IsNull(tether.Held,"cut, it still holds the ship");
        }

        // ------------------------------------------------------------------ the clamps' guns: curtains that cross
        /// <summary>Where a point lies in a gun's curtain: its row (0 nearest), or -1 outside every row. Read from its definition (tune.count rows out to tune.reach, tune.width across).</summary>
        static int RowOf(EnemyBrain gun,Vector3 point)
        {
            var tune=gun.definition.tune;var ahead=gun.transform.forward;ahead.y=0;ahead.Normalize();var across=Vector3.Cross(Vector3.up,ahead);
            var off=point-gun.transform.position;off.y=0;float along=Vector3.Dot(off,ahead),side=Vector3.Dot(off,across);
            if(along<0||along>=tune.reach||Mathf.Abs(side)>tune.width*.5f)return -1;
            return Mathf.FloorToInt(along/(tune.reach/Mathf.Max(1,tune.count)));
        }
        [UnityTest] public IEnumerator TwoClampGunsCrossTheirCurtainsSoNeitherHasALeeAndTheirQuietRowsMeetInACell()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-80,-80));
            // A clamp's two guns 9 m apart, each turned 35 degrees toward the other, so their curtains cross over the clamp's pad. Built wrong on purpose: both facing straight out, as two
            // guns side by side are: their curtains lie one over the other and behind both is a lee.
            float turn=Broken?0:35;
            var left=Enemy(Gun,Sky(-4.5f,0),true,turn);var right=Enemy(Gun,Sky(4.5f,0),true,-turn);yield return Frames(3);
            Assert.IsInstanceOf<EnemyAnchored>(left.Behaviour,"a Clamp Gun isn't anchored");
            // The cell: on the line between them, 9 m ahead of each (the middle of each one's second row), 7.84 m out.
            var cell=Sky(0,7.84f);
            Assert.AreEqual(1,RowOf(left,cell),"fixture: the cell is in the left gun's second row");Assert.AreEqual(1,RowOf(right,cell),"fixture: the cell is in the right gun's second row");
            // Behind the left gun, 5 m back along its own facing: no lee, because it is in front of the right one.
            var lee=left.transform.position-left.transform.forward*5;lee.y=1;
            Assert.AreEqual(-1,RowOf(left,lee),"fixture: behind the left gun is out of its own curtain");
            Assert.GreaterOrEqual(RowOf(right,lee),0,"behind one gun of a crossed pair is out of the other's curtain too: a lee to sit in, and guns that don't cover each other");
            Put(hero,cell);int hits=0;System.Action<Health,DamagePacket,float> hurt=(_,__,amount)=>{if(amount>0)hits++;};hero.Health.Damaged+=hurt;
            try
            {
                left.Passive=right.Passive=false;
                // The double beat: the first and third rows of both, marked a second; the cell where their second rows cross is quiet.
                yield return Until(()=>Count("CURTAIN_MARKED "+Gun+" beat=0")>=2,3,hero);Assert.AreEqual(2,Count("CURTAIN_MARKED "+Gun+" beat=0"),"the two guns didn't mark their first beat together");
                Assert.IsFalse(Telegraph.Live.Any(t=>t!=null&&(t.owner==left||t.owner==right)&&t.Contains(hero.transform.position)),"on the double beat the cell where their quiet rows cross is marked");
                Assert.GreaterOrEqual(left.TellDuration,.95f,"a row bursts too soon after it is marked");
                yield return Until(()=>Count("CURTAIN_BURST "+Gun)>=2,3,hero);Assert.AreEqual(0,hits,"a ship in the quiet cell was struck on the double beat");
                // The single beat: both mark their second rows, which cross over him; he is struck once, not twice.
                yield return Until(()=>Count("CURTAIN_MARKED "+Gun+" beat=1")>=2,4,hero);
                Assert.IsTrue(Telegraph.Live.Any(t=>t!=null&&t.owner==left&&t.Contains(hero.transform.position))&&Telegraph.Live.Any(t=>t!=null&&t.owner==right&&t.Contains(hero.transform.position)),"on the single beat their second rows don't both cross the cell");
                yield return Until(()=>Count("CURTAIN_BURST "+Gun)>=4,3,hero);yield return Frames(2);Assert.AreEqual(1,hits,"two crossed curtains bursting over one ship struck it other than once");
                // Behind one gun is in front of the other: the lee is struck.
                hits=0;Put(hero,lee,false);
                yield return Wait(7,hero);Assert.Greater(hits,0,"a ship behind one gun of the crossed pair sat out two beats unhurt");
            }
            finally{hero.Health.Damaged-=hurt;}
        }

        // ------------------------------------------------------------------ Sorrel's shelves: the tender
        static BridleClamp Clamp(int index,Vector3 seat,Vector3 facing)
        {
            var go=new GameObject("Stillwater clamp "+index);go.SetActive(false);go.transform.SetPositionAndRotation(seat,Quaternion.LookRotation(facing));
            var clamp=go.AddComponent<BridleClamp>();clamp.index=index;clamp.settledBy="MotherOfDoors";clamp.openedFlag="test.clamp"+index;
            clamp.whole=new GameObject("whole");clamp.whole.transform.SetParent(go.transform,false);
            clamp.onCatch=new GameObject("on its catch");clamp.onCatch.transform.SetParent(go.transform,false);
            clamp.released=new GameObject("let go");clamp.released.transform.SetParent(go.transform,false);
            go.SetActive(true);return clamp;
        }
        [UnityTest] public IEnumerator AClampTenderGoesToTheClampTheLeadIsWorkingAndClosesWhatHeTookUntilItIsStopped()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            var clamp=Clamp(1,At(0,.05f,0),Vector3.back);yield return Frames(2);BridleClamp.Offer(true);
            // Built wrong on purpose: the clamp it should keep is out of its sight.
            var tender=Enemy(Tender,At(Broken?40:-9,.05f,6));yield return Frames(3);Assert.IsInstanceOf<EnemyTender>(tender.Behaviour,"a Clamp Tender isn't a tender");
            Put(hero,clamp.transform.position+Vector3.up*.05f);yield return null;clamp.Interact();Assert.AreEqual(hero,clamp.Holder,"fixture: his hands are on the clamp");
            tender.Passive=false;
            yield return Until(()=>clamp.Tended.Tending,8,hero);
            Assert.IsTrue(clamp.Tended.Tending,"the tender never came to the clamp the lead was working");Assert.IsTrue(Said("TENDER_CLOSING "+Tender),"it closed a clamp in a hero's hands with no word from him");
            BridleClamp.Slack=true;yield return Wait(2.5f,hero);
            Assert.AreEqual(BridleClamp.Hold.Shut,clamp.State,"the clamp opened with a tender at work on it");Assert.Less(clamp.Weight,.1f,"what he took of its weight held with a tender closing it");
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"at work, the tender burned the hero");
            // Stopped (it is fragile: the partner's to stop), and the clamp opens in his hands.
            Assert.LessOrEqual(Def(Tender).integrity,Def(Warden).integrity/3,"the tender isn't fragile beside the crews");
            Hit(tender.Health,hero,tender.Health.maximum*3);yield return Until(()=>clamp.State!=BridleClamp.Hold.Shut,4,hero);
            Assert.AreEqual(BridleClamp.Hold.Open,clamp.State,"with the tender stopped, the clamp held through the slack didn't open");
            // With nothing to keep, it torches a hero before it, marked.
            var second=Enemy(Tender,At(20,.05f,0));yield return Frames(3);Put(hero,At(20,.05f,2));second.Passive=false;
            yield return Until(()=>second.Telegraphing,4,hero);Assert.IsTrue(second.Telegraphing,"with nothing to keep, it never torched a hero before it");
            Assert.AreEqual(Telegraph.Kind.Cone,second.AttackMark.Shape,"its torch isn't marked as a fan");Assert.IsTrue(second.AttackMark.Contains(hero.transform.position),"its torch isn't laid on him");
        }

        // ------------------------------------------------------------------ a clamp's shelf (ch10-wiring 6.1.1, D291): the guns go quiet over a hero on it
        const float Plane=1,ShelfTop=-.2f;
        static readonly Vector3 ShelfPad=new(150,Plane,140),ShelfLaunch=new(145,ShelfTop,152),ShelfFoot=new(150,ShelfTop,147),ShelfFlight=new(150,Plane,131),ShelfReturn=new(142,Plane,133);
        const string ShelfId="MooringTestShelf";
        /// <summary>The flight yard with a clamp's shelf on it (BerthTests' yard, in this class's own name), built into the scene as it loads.</summary>
        static void ShelfYard(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!="Arena_Flight")return;
            var yard=new GameObject("Mooring shelf yard");SceneManager.MoveGameObjectToScene(yard,scene);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Shelf floor";floor.transform.SetParent(yard.transform,false);
            floor.transform.position=new Vector3(150,ShelfTop-.5f,150);floor.transform.localScale=new Vector3(14,1,10);
            var door=new GameObject("MooringTestShelfDoor",typeof(SpawnPoint));door.transform.SetParent(yard.transform,false);door.transform.position=new Vector3(150,Plane,108);door.GetComponent<SpawnPoint>().id="MooringTestShelfDoor";
            Berth.Declare(ShelfId,"The test clamp's works",ZoneKind.GroundCombat,new Bounds(new Vector3(150,ShelfTop+.5f,150),new Vector3(14,1,10)),ShelfPad,ShelfLaunch,ShelfFoot,ShelfFlight,"MooringTestShelfPad",ShelfReturn,true,true,yard.transform);
        }
        static IEnumerator Press(InteractionPrompt expected,string what)
        {
            yield return new WaitForSecondsRealtime(GameInput.HandBackHold+.1f);
            PromptService.FindInteraction(PartyController.Current.Active.transform.position);
            Assert.AreSame(expected,PromptService.Interaction,$"{what}: the lead isn't offered \"{expected.prompt}\" (offered \"{PromptService.Interaction?.prompt??"nothing"}\")");
            Assert.IsTrue(PromptService.TryInteract(),what+": A didn't take the prompt");
            float end=Time.unscaledTime+3;while(Berth.Changing&&Time.unscaledTime<end)yield return null;
            Assert.IsFalse(Berth.Changing,what+": the change never finished");Hold();
        }
        [UnityTest] public IEnumerator AClampsGunsGoQuietOverAHeroDockedOnItsShelfWhereTheShelfsTenderIsHisFight()
        {
            SceneManager.sceneLoaded+=ShelfYard;
            yield return Zone("Arena_Flight");var berth=Berth.Find(ShelfId);Assert.IsNotNull(berth,"fixture: the yard has no shelf");var (hero,partner)=Party();
            // Two guns at the plane beyond the shelf, turned back over it (20 m from the pad: outside the 15 m a pad is held by), awake as the ship comes in; the shelf's tender asleep.
            var guns=new[]{Enemy(Gun,new Vector3(146,Plane,160),true,180+20),Enemy(Gun,new Vector3(154,Plane,160),true,180-20)};
            var tender=Enemy(Tender,new Vector3(152,ShelfTop+.1f,150));yield return Frames(3);
            Assert.IsTrue(guns.All(g=>g.Flier),"fixture: a gun at the plane reads as a body on foot");Assert.IsFalse(tender.Flier,"fixture: the tender on the shelf reads as a flier");
            Put(hero,ShelfPad+Vector3.back*3);Put(partner,ShelfPad+Vector3.back*3+Vector3.right*4.2f);hero.Health.InvulnerableUntil=partner.Health.InvulnerableUntil=GameTime.Now+900;
            foreach(var gun in guns)gun.Passive=false;
            yield return Until(()=>Said("CURTAIN_MARKED "+Gun),4,hero,partner);Assert.IsTrue(Said("CURTAIN_MARKED "+Gun),"fixture: the guns fire at a ship coming in to the pad");
            yield return Press(berth.pad,"docking at the clamp's works with its guns awake");
            Assert.IsTrue(Berth.Docked,"fixture: the party is on the shelf");
            hero.Health.InvulnerableUntil=partner.Health.InvulnerableUntil=0;float lead=hero.Health.integrity,other=partner.Health.integrity;
            log.RemoveAll(l=>l.StartsWith("CURTAIN_"));
            // Built wrong on purpose: the world without its berth, where the guns' form is the zone's and nothing keeps them off a hero on foot.
            var berths=ZoneController.Berths.ToArray();if(Broken)ZoneController.Berths.Clear();
            try
            {
                for(float end=Time.time+4;Time.time<end;){foreach(var h in new[]{hero,partner})h.motor.Move(Vector2.zero,false,false);yield return null;}
                Assert.IsFalse(Said("CURTAIN_MARKED")||Said("CURTAIN_BURST"),"a clamp's gun marked or burst its curtain over the heroes standing on its shelf");
                Assert.AreEqual(lead,hero.Health.integrity,.01f,"the lead on the shelf was hurt by the guns over it");Assert.AreEqual(other,partner.Health.integrity,.01f,"the partner on the shelf was hurt by the guns over it");
                foreach(var gun in guns)Assert.AreEqual(Plane,gun.transform.position.y,.3f,"a gun left the plane while the party stood on the shelf");
            }
            finally{if(Broken){ZoneController.Berths.Clear();ZoneController.Berths.AddRange(berths);}}
            // On the shelf the tender is the fight, marked on foot.
            Put(hero,tender.transform.position+tender.transform.forward*2+Vector3.up*.05f,false);tender.Passive=false;
            yield return Until(()=>tender.Telegraphing,4,hero,partner);Assert.IsTrue(tender.Telegraphing&&tender.AttackMark!=null,"on the shelf the tender never torched the hero on foot");
            tender.Passive=true;Object.Destroy(tender.gameObject);yield return Frames(2);
            // Launched, the guns fight the ships again.
            Put(hero,ShelfLaunch+Vector3.right*1.2f,false);yield return Press(berth.launch,"launching");
            // Set down at the berth's flight place, 29 m off: back toward the pad, inside the guns' 24 m.
            Put(hero,ShelfPad+Vector3.back*3,false);log.RemoveAll(l=>l.StartsWith("CURTAIN_"));
            yield return Until(()=>Said("CURTAIN_MARKED "+Gun),5,hero,partner);Assert.IsTrue(Said("CURTAIN_MARKED "+Gun),"after the launch the guns never fired at the ships again");
        }
    }
}
