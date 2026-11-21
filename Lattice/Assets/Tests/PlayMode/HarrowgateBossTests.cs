using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 8's two bosses, each on a bare floor in the proving yards with what its room gives it made here (nothing waits on the Bridle Foundry, Harrowgate or the war harbour
    /// being built): the Slag Colossus with the heart's three coolant sluices round it, Drask Vaal on open ground (and on a duel ground), and his skiff in the flight yard with the harbour's
    /// waypoints and batteries. The party is the one the chapter meets them with: its arrival (DevLoadout chapter8: three heroes, Taren and Idra out, Tempered parts), at the Sync each is met
    /// at, with every pair a player can put out where the fight is about the party. Hit sizes on all three heroes; the Colossus too hot to hurt until a sluice darkens a plate, its plates and
    /// arms, its leak; the duel's rules as a player meets them; the skiff's runs and the guns; each fight's length; and the chapter's boss sheets for the shared tests.</summary>
    public sealed class HarrowgateBossTests
    {
        const float X=230;
        readonly List<string> log=new();
        static readonly string[] Heeded={"BOSS_","COLOSSUS_","PLATE_","SLUICE_","PART_","DRASK_","SKIFF_","BREAK_OK","EXPOSED"};
        void Heard(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        /// <summary>Off unless this class's own run sets it (HARROWGATE_BOSS_BROKEN=1): each test then builds its fixture wrong, so the run shows every test failing the way it should.</summary>
        static bool Broken=>System.Environment.GetEnvironmentVariable("HARROWGATE_BOSS_BROKEN")=="1";
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter8");log.Clear();slowest=float.MaxValue;Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void HarrowgateBosses(ContentRegistry.Book book)
        {
            // The Slag Colossus: met at about Sync 23 (the chapter runs 22 to 25), in the Foundry's heart (docs/maps/BridleFoundry.md, "Boss arena contract"): Vessa has fallen back and the
            // cage has been seen from the door. In the yard it wears no plates (there are no sluices), so every move is stood in as it would be cooled.
            book.Boss(new ContentRegistry.BossSheet{id="SlagColossus",zone="BridleFoundry",door="SlagColossus",doorFlags=new[]{"vask.vessaBeaten","vask.cageSeen"},sync=23,driverShare=.3f,
                specials=new[]{("Pour",new Vector3(0,0,7)),("Stomp",new Vector3(0,0,5)),("SlagBombs",new Vector3(0,0,10)),("FurnaceBreath",new Vector3(0,0,8)),("Spill",new Vector3(0,0,5))},
                // Its stomp leaves its feet in the floor; coming apart, its spill leaves it emptied.
                windows=new[]{("Stomp",new Vector3(0,0,5)),("Spill",new Vector3(0,0,5))}});
            // Drask Vaal, the duel: met at about Sync 24 on the muster field (docs/maps/Harrowgate.md, "Duel ground contract"). Half of a two-part fight: 80 to 200 s on its own. A pair with one
            // at his shield with the other at his back takes two to two and a half minutes whoever leads (TheDuelLastsWhatItsSheetSaysForEveryPair). Two chains
            // fought him on November 14 at 130,000: 87 s and 76 s, the second under his floor, which is a share of about .42 and not the placeholder .3. He was 150,000 at .42.
            // Both chains' party wore a Refined Edge and Scrap parts (the driver put nothing on after chapter 2). Fought through the pad by chapter 8's floor party, from his real door
            // (FloorPartyBossReplayTests, November 15), 150,000 fell in 84.3 and 84.9 s: .500 and .486 of 3,562 and 3,631 a second, which put the duel test's arrival pair at 77 s.
            // He is 165,000, which fell the same way in 91.8, 110.3 and 97.4 s (.466, .410 and .448 of 3,860, 3,651 and 3,778): this is the lowest.
            book.Boss(new ContentRegistry.BossSheet{id="DraskVaal",zone="Harrowgate",door="DraskVaal",doorFlags=new[]{"vask.parley","vask.draskMet","vask.fieldTaken"},sync=24,driverShare=.41f,
                minSeconds=80,maxSeconds=200,
                specials=new[]{("Thrust",new Vector3(0,0,4)),("HaftSweep",new Vector3(0,0,2.5f)),("ShieldRush",new Vector3(0,0,8)),("Lunge",new Vector3(0,0,10))},
                // His shout strikes nobody: what it marks is where his guard steps in (FewerAnswerEachShoutAndTheThirdIsAnsweredByNobody).
                testedApart=new[]{"Shout"},
                // A thrust at a hero past its 9 m line meets nobody and leaves him overreached; pressing, his lunge always ends overextended.
                windows=new[]{("Thrust",new Vector3(0,0,12)),("Lunge",new Vector3(0,0,10))}});
            // His skiff, the pursuit, in flight over the war harbour (docs/maps/Harrowgate.md, "Pursuit contract"): 60 to 150 s on its own.
            book.Boss(new ContentRegistry.BossSheet{id="DraskSkiff",zone="HarrowgateHarbour",door="DraskSkiff",doorFlags=new[]{"vask.duelWon"},flight=true,sync=24,driverShare=.29f,
                minSeconds=60,maxSeconds=150,
                specials=new[]{("LanceRun",new Vector3(0,0,10)),("GunCall",new Vector3(0,0,10)),("Wake",new Vector3(0,0,-8))},
                // Its run goes to the next waypoint, away from the party in the yard (AtEachQuarterHeRunsForTheNextWaypointAndAtTheSlipHeCanRunNoFurther).
                testedApart=new[]{"Run"},
                // A lance run always ends turning wide, in both halves.
                windows=new[]{("LanceRun",new Vector3(0,0,10)),("LanceRun",new Vector3(0,0,10))}});
        }

        // ------------------------------------------------------------------ fixtures
        static GameState State=>GameServices.Current.State;
        static bool Set(string key)=>GameServices.Current.Flags.GetBool(key);
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
        static Vector3 At(float x,float y,float z)=>new(X+x,y,z);
        static Vector3 Sky(float x,float z)=>new(200+x,1,z);
        static void Floor()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Harrowgate fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static (CombatActor lead,CombatActor partner) Party(){var lead=PartyController.Current.Active;return (lead,PartyController.Current.members.First(m=>m!=lead));}
        static void Put(CombatActor actor,Vector3 position,bool fresh=true)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            if(fresh){actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;}Physics.SyncTransforms();
        }
        /// <summary>Turns a hero put down to face a point, as a player squares up to what they fight.</summary>
        static void Face(CombatActor actor,Vector3 point)
        {
            var toward=point-actor.transform.position;toward.y=0;if(toward.sqrMagnitude<.01f)return;toward.Normalize();
            if(actor.motor is GroundMotor ground)ground.KeepFacing(toward);else if(actor.motor is FlightMotor ship)ship.KeepFacing(toward);
            var body=actor.GetComponent<CharacterController>();body.enabled=false;actor.transform.rotation=Quaternion.LookRotation(toward);body.enabled=true;Physics.SyncTransforms();
        }
        static EnemyDef Def(string id){var definition=GameCatalog.Find<EnemyDef>(id);Assert.IsNotNull(definition,"fixture: "+id+" is defined (HarrowgateBuilder.Definitions)");return definition;}
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true,float yaw=0){var enemy=ActorFactory.Enemy(Def(id),at);enemy.transform.rotation=Quaternion.Euler(0,yaw,0);enemy.Passive=passive;return enemy;}
        static DamageType Neutral(Health body)=>System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
        static float Hit(Health body,CombatActor from,float amount=100,float breakPower=0,DamageType? type=null)=>body.Receive(new DamagePacket{source=from.Health,amount=amount,type=type??Neutral(body),breakPower=breakPower});
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>Waits on the fight's own clock, driving the heroes' motors as their brains would: a roll is only carried out by a motor that is being driven.</summary>
        static IEnumerator Until(System.Func<bool> done,float seconds,params CombatActor[] held)
        {
            for(float end=Time.unscaledTime+seconds*1.5f+1,until=Time.time+seconds;!done()&&Time.time<until&&Time.unscaledTime<end;)
            {foreach(var actor in held)if(actor!=null&&actor.Health.Alive)actor.motor.Move(Vector2.zero,false,false);yield return null;}
        }
        static IEnumerator Wait(float seconds,params CombatActor[] held)=>Until(()=>false,seconds,held);
        static bool Faint(Telegraph marking)=>marking.GetComponent<MeshRenderer>().sharedMaterials[1].name.Contains("faint");
        bool Said(string start)=>log.Any(l=>l.StartsWith(start));
        int Count(string start)=>log.Count(l=>l.StartsWith(start));
        static Telegraph[] Marks(Object owner)=>Telegraph.Live.Where(t=>t!=null&&t.owner==owner&&!t.Released).ToArray();
        static EnemyBrain[] Alive(string id)=>Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.definition!=null&&e.definition.id==id&&e.Health.Alive).ToArray();

        /// <summary>A coolant sluice as the contract stands one: on its seat, facing the heart's middle, answering to the Colossus, its wheel on a housing behind the seat and its spout on the rim.</summary>
        static CoolantSluice Sluice(int index,Vector3 seat,Vector3 centre)
        {
            var go=new GameObject("Coolant sluice "+index);go.SetActive(false);var toward=centre-seat;toward.y=0;go.transform.SetPositionAndRotation(seat,Quaternion.LookRotation(toward.normalized));
            var sluice=go.AddComponent<CoolantSluice>();sluice.index=index;sluice.settledBy="SlagColossus";
            var wheel=new GameObject("Wheel");wheel.transform.SetParent(go.transform,false);wheel.transform.localPosition=new Vector3(0,1.2f,-1.4f);sluice.wheel=wheel.transform;
            var spout=new GameObject("Spout");spout.transform.SetParent(go.transform,false);spout.transform.localPosition=new Vector3(0,2.6f,-3.5f);sluice.spout=spout.transform;
            go.SetActive(true);return sluice;
        }
        /// <summary>The heart as the contract has it, round a middle: three sluices' seats 15 m out on bearings 200, 80 and 320 (indices 1 to 3).</summary>
        static CoolantSluice[] Heart(Vector3 centre)
        {
            Vector3 On(float bearing,float r)=>centre+Quaternion.Euler(0,bearing,0)*Vector3.forward*r;
            return new[]{Sluice(1,On(200,15),centre),Sluice(2,On(80,15),centre),Sluice(3,On(320,15),centre)};
        }
        /// <summary>Turns a sluice open with this hero standing in its seat, holding still until it opens.</summary>
        static IEnumerator Turn(CoolantSluice sluice,CombatActor hero)
        {
            Put(hero,sluice.transform.position+Vector3.up*.05f,false);yield return null;
            Assert.IsTrue(sluice.Available,$"fixture: sluice {sluice.index} can be turned");sluice.Interact();Assert.AreEqual(hero,sluice.Holder,"fixture: his hands are on the wheel");
            yield return Until(()=>sluice.State!=CoolantSluice.Hold.Ready,3,hero);
            Assert.AreEqual(CoolantSluice.Hold.Refilling,sluice.State,$"sluice {sluice.index} turned with his hands on it didn't open");
        }
        static ColossusParts Colossus(EnemyBrain enemy)=>new(enemy.GetComponent<BossController>());
        sealed class ColossusParts{public readonly BossController boss;public ColossusParts(BossController boss){this.boss=boss;}
            public Health Dark=>boss.HeatPlates.FirstOrDefault(p=>p.Alive&&p.Damageable);}

        // ------------------------------------------------------------------ the chapter's loadout
        [UnityTest] public IEnumerator TheChapterEightLoadoutArrivesAsThreeAtSyncTwentyTwoInTemperedPartsWithTarenAndIdraOut()
        {
            // Built wrong on purpose: chapter 7's arrival is Sync 19.
            if(Broken)DevLoadout.Apply("chapter7");
            yield return Zone("Arena_Ground");
            CollectionAssert.AreEquivalent(new[]{"Taren","Sela","Idra"},State.party.Select(m=>m.id).ToArray(),"chapter 8 arrives without its three heroes");
            foreach(var member in State.party)Assert.AreEqual(22,member.level,member.id+" arrives at the wrong Sync: the plan ends chapter 7 at 22");
            CollectionAssert.AreEquivalent(new[]{"Taren","Idra"},PartyController.Current.members.Select(m=>m.character).ToArray(),"the pair out isn't the one a join leaves out");
            foreach(var member in State.party)
            {
                string worn=member.equipped.TryGetValue(GearSlot.Frame,out var id)?State.parts.FirstOrDefault(p=>p.instanceId==id)?.definitionId:null;
                Assert.AreEqual("FramesT4",worn,member.id+" doesn't arrive in the Tempered frame chapter 7 sells");
            }
            Assert.IsTrue(Set("chapter7.complete"),"chapter 7 isn't told");
        }

        // ------------------------------------------------------------------ the numbers the bosses are tuned by
        /// <summary>The standing rule (CHAPTER_GUIDE section 0.1, D257): a boss's move costs a hero 12 to 16% of the bar, more than anything on the road to it, and under a fifth with its second
        /// phase's +18%; a move it has only in its second phase is held to 12 to 16 with that +18%. Measured through Health.Receive as a real blow lands, Plating and all, on all three heroes:
        /// the Colossus at Sync 23 and 24, Drask and his skiff at 24 and 25, each in the parts the chapter arrives in (Tempered). The road is every ordinary attack of the chapter defined so far
        /// (and Vessa's, the elite before the Colossus). Every capsule is at least twice its radius tall, and every chapter 8 zone is at threat 1.</summary>
        [UnityTest] public IEnumerator EveryBossMoveTakesABossesShareOfAnyHerosBarAndMoreThanTheRoadToIt()
        {
            yield return Zone("Arena_Ground");Floor();
            var source=Enemy("SlagColossus",At(50,.05f,50)).Health;yield return Frames(2);
            var roadIds=new[]{"SensorBuoy","MusterLancer","FlakGate","MusterPikeguard","MusterMortar","AshCrawler","PourDrone","ForgeHound","ReachScav","ReachSergeant","VessaKorl"};
            var road=roadIds.Select(id=>GameCatalog.Find<EnemyDef>(id)).Where(d=>d!=null).SelectMany(d=>d.attacks.Where(a=>a!=null).Select(a=>(id:d.id,what:a.name,damage:a.damage))).ToList();
            TestContext.WriteLine($"HARROW_ROAD {road.Count} attacks from {string.Join(",",road.Select(r=>r.id).Distinct())}");
            // (what, damage, its phase-two-only, the Syncs it is met at)
            var colossus=new List<(string what,float damage,bool later)>{("its tread",Def("SlagColossus").attacks[0].damage,false),("Pour",BossController.ColossusTune.PourDamage,false),
                ("Stomp",BossController.ColossusTune.StompDamage,false),("Slag Bombs",BossController.ColossusTune.BombDamage,false),("Furnace Breath",BossController.ColossusTune.BreathDamage,false),
                ("Spill",BossController.ColossusTune.SpillDamage,true)};
            // Built wrong on purpose: a stomp authored at 170 is over the band at 23, and the check has to say so.
            if(Broken)colossus.Add(("a stomp authored at 170",170,false));
            var drask=new List<(string what,float damage,bool later)>{("his jab",Def("DraskVaal").attacks[0].damage,false),("Thrust",BossController.DraskTune.ThrustDamage,false),
                ("Haft Sweep",BossController.DraskTune.SweepDamage,false),("Shield Rush",BossController.DraskTune.RushDamage,false),("Lunge",BossController.DraskTune.LungeDamage,true),
                ("the skiff's guns",Def("DraskSkiff").attacks[0].damage,false),("Lance Run",BossController.SkiffTune.LanceDamage,false),("Gun Call",BossController.SkiffTune.GunDamage,false),
                ("Wake",BossController.SkiffTune.WakeDamage,false),("Run",BossController.SkiffTune.RunDamage,false)};
            var failures=new List<string>();
            foreach(var pair in new[]{new[]{"Taren","Idra"},new[]{"Sela","Idra"}})
            {
                yield return Deploy(pair);
                foreach(var (moves,syncs) in new[]{(colossus,new[]{23,24}),(drask,new[]{24,25})})
                foreach(int sync in syncs)
                {
                    yield return Level(sync);
                    foreach(var hero in PartyController.Current.members)
                    {
                        if(pair[0]=="Sela"&&hero.character=="Idra")continue;
                        float bar=hero.Health.maximum;
                        float Share(float damage){hero.Health.integrity=bar;hero.Health.InvulnerableUntil=0;return hero.Health.Receive(new DamagePacket{source=source,amount=damage,type=DamageType.Kinetic,weight=HitWeight.Heavy})/bar;}
                        float heaviest=0;string named="nothing";foreach(var (id,what,damage) in road){float s=Share(damage);if(s>heaviest){heaviest=s;named=id+" "+what;}}
                        foreach(var (what,damage,later) in moves)
                        {
                            float whole=Share(damage),pressed=Share(damage*(1+BossController.PhaseStep));
                            string line=$"HARROW_BOSS_HIT {hero.character} sync={sync} {what} {damage:0} share={whole:P1} second={pressed:P1} road={named} {heaviest:P1}";TestContext.WriteLine(line);
                            float met=later?pressed:whole;
                            if(met<.12f||met>.16f)failures.Add(line+(later?": a move of its second half is 12 to 16% of the bar with the half's +18%":": a boss's move is 12 to 16% of the bar"));
                            if(!later&&pressed>.2f)failures.Add(line+": in its second half, no blow takes a fifth of the bar");
                            if(met<=heaviest)failures.Add(line+": no more than the road to it");
                        }
                    }
                }
            }
            foreach(string zone in new[]{"PicketOfTeeth","ScoriaFlows","BridleFoundry","Harrowgate","HarrowgateHarbour"})
            {var z=GameCatalog.Find<ZoneDef>(zone);if(z!=null&&!Mathf.Approximately(z.enemyThreat,1))failures.Add($"{zone} is at threat {z.enemyThreat}: chapter 8's damage is authored at its real weight, at 1");}
            foreach(string id in new[]{"SlagColossus","DraskVaal","DraskSkiff"})
            {
                var def=Def(id);float round=BossController.CapsuleRadius(id);
                if(def.capsuleHeight<round*2)failures.Add($"{id}'s capsule is {def.capsuleHeight} m tall on a {round} m radius: Unity stretches it and the body floats");
                if(!BossController.Registered(id))failures.Add(id+" has no Kit");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the Slag Colossus
        [UnityTest] public IEnumerator TheColossusIsTooHotToHurtUntilASluiceDarkensAPlateAndTheFirstBlowSaysSo()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            var heart=Heart(At(0,0,0));
            // Before it wakes nothing in the room can be worked or struck: the sluices aren't offered.
            Assert.IsFalse(heart.Any(s=>s.Available),"a sluice can be turned before the Colossus has woken");
            var enemy=Enemy("SlagColossus",At(0,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();var body=enemy.Health;var parts=Colossus(enemy);
            Assert.AreEqual(3,boss.PlatesWorn,"in a room with sluices it wears three plates");Assert.IsTrue(boss.Hot,"woken, it doesn't glow");
            Assert.IsTrue(boss.HeatPlates.All(p=>!p.Damageable),"a plate can be struck while it glows");Assert.IsTrue(heart.All(s=>s.Available),"woken and glowing, its sluices aren't offered");
            StringAssert.Contains("TOO HOT TO HURT",boss.StatusText);StringAssert.Contains("OPEN A SLUICE",boss.StatusText);StringAssert.Contains("Open a sluice",boss.FightGoal(""));
            // A blow on glowing armour: nothing, read as resisted, and the first one says why on screen.
            var posted=new List<string>();System.Action<string,string> notice=(h,b)=>posted.Add(h+": "+b);Notices.Posted+=notice;
            var kinds=new List<HitKind>();System.Action<Health,float,HitKind> seen=(victim,_,kind)=>{if(victim==body)kinds.Add(kind);};Health.DamageNumber+=seen;
            Put(hero,At(0,.05f,-4.2f));
            try
            {
                Assert.AreEqual(0,Hit(body,hero,500,100000),.01f,"a blow on glowing armour took something off it");
                Assert.AreEqual(HitKind.Resisted,kinds.Last(),"a blow on glowing armour doesn't read as resisted");
                Assert.IsFalse(body.Broken,"a blow on glowing armour broke it open");
                Hit(body,hero,500);
            }
            finally{Notices.Posted-=notice;Health.DamageNumber-=seen;}
            Assert.AreEqual(body.maximum,body.integrity,.01f);Assert.AreEqual(1,Count("COLOSSUS_HOT"),"the first blow on glowing armour isn't said once");
            Assert.AreEqual(1,posted.Count(p=>p.StartsWith("TOO HOT TO HURT")),"nothing on screen says why the blow did nothing: "+string.Join(" | ",posted));
            StringAssert.Contains("sluice",posted.First(p=>p.StartsWith("TOO HOT")),"what's on screen doesn't say what to do");
            // A sluice turned: the jet, a plate dark, the engine reeling; and Tovra's third call.
            var spoken=new List<string>();System.Action<string,string> heard=(who,line)=>spoken.Add(who+": "+line);BarkService.Spoken+=heard;
            // Built wrong on purpose: his hands come off the wheel half way round, and nothing opens.
            if(Broken){Put(hero,heart[0].transform.position+Vector3.up*.05f,false);heart[0].Interact();yield return Wait(.6f,hero);Put(hero,At(0,.05f,-6),false);yield return Wait(2,hero);}
            else try{yield return Turn(heart[0],hero);}finally{BarkService.Spoken-=heard;}
            BarkService.Spoken-=heard;
            Assert.IsTrue(Said("SLUICE_OPEN 1"),"the sluice never opened");Assert.IsTrue(Said("PLATE_DARK"),"its coolant darkened no plate");
            var dark=parts.Dark;Assert.IsNotNull(dark,"no plate can be struck after the quench");Assert.IsTrue(boss.PlateDark);
            Assert.IsTrue(spoken.Any(s=>s.StartsWith("Tovra: Third. That plate's going dark.")),"Tovra's third call isn't said at the first dark plate: "+string.Join(" | ",spoken));
            StringAssert.Contains("A PLATE IS DARK",boss.StatusText);Assert.AreEqual("Hit the dark plate while it lasts",boss.FightGoal(""));
            Assert.IsFalse(heart.Any(s=>s.Available),"a sluice is offered while a plate is already dark");
            // Its body is still too hot; the dark plate breaks, is shed, and takes an eighth of the bar.
            Assert.AreEqual(0,Hit(body,hero,500),.01f,"with one plate dark its body can be hurt");
            float before=body.integrity;Hit(dark,hero,dark.maximum*10);yield return Frames(2);
            Assert.IsTrue(Said("PLATE_SHED"),"a dark plate broken isn't shed");Assert.AreEqual(1,boss.PlatesShed);
            Assert.AreEqual(before-body.maximum*BossController.ColossusTune.PlateBar,body.integrity,2,"a shed plate didn't take an eighth of its bar");
            Assert.IsTrue(boss.Hot,"one plate shed and it no longer glows");
        }
        [UnityTest] public IEnumerator ASluiceLetGoRunsBackAndADarkPlateLeftAloneGlowsAgainWithItsWounds()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            var heart=Heart(At(0,0,0));var enemy=Enemy("SlagColossus",At(0,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();var parts=Colossus(enemy);
            // Half way round, a roll lets go of the wheel: nothing opens, and what was turned runs back.
            var sluice=heart[1];Put(hero,sluice.transform.position+Vector3.up*.05f,false);yield return null;sluice.Interact();
            yield return Wait(.8f,hero);Assert.Greater(sluice.Turned,.3f,"fixture: the wheel is part way round");
            Assert.IsTrue(hero.Dodge(Vector3.left),"fixture: the roll");yield return Wait(.3f,hero);
            Assert.IsNull(sluice.Holder,"a roll didn't take his hands off the wheel");Assert.IsTrue(Said("SLUICE_LET_GO 2"));
            float turned=sluice.Turned;yield return Wait(.6f,hero);Assert.Less(sluice.Turned,turned,"what was turned doesn't run back once he lets go");
            Assert.AreEqual(CoolantSluice.Hold.Ready,sluice.State,"let go half way, it opened anyway");Assert.IsFalse(Said("SLUICE_OPEN"));
            // Turned all the way: a plate dark. Struck, but not broken, it glows again after its seven seconds with what it took.
            yield return Turn(sluice,hero);var dark=parts.Dark;Assert.IsNotNull(dark,"fixture: a plate is dark");
            Put(hero,At(-50,.05f,-50),false);
            Hit(dark,hero,dark.maximum*.4f);float left=dark.integrity;Assert.Less(left,dark.maximum,"fixture: the plate is wounded");
            // Built wrong on purpose: a fourth of the dark is waited, and the plate is still dark.
            yield return Wait(Broken?BossController.ColossusTune.Dark*.25f:BossController.ColossusTune.Dark+.3f);
            Assert.IsTrue(Said("PLATE_REHEATS"),"a dark plate left alone never glows again");Assert.IsFalse(dark.Damageable,"a plate glowing again can still be struck");
            Assert.AreEqual(left,dark.integrity,.01f,"a plate that glows again lost its wounds");
            Assert.AreEqual(CoolantSluice.Hold.Refilling,sluice.State,"a turned sluice can be turned again before it has refilled");
            Assert.IsTrue(heart[0].Available,"with no plate dark another sluice isn't offered");
            // The next quench goes to the plate already wounded.
            yield return Turn(heart[0],hero);Assert.AreEqual(dark,parts.Dark,"the coolant went to a whole plate while a wounded one glowed");
        }
        [UnityTest] public IEnumerator WithItsPlatesShedItsArmsHoldItAtHalfAndTheSecondTearsItApart()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            var heart=Heart(At(0,0,0));var enemy=Enemy("SlagColossus",At(0,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();var body=enemy.Health;var parts=Colossus(enemy);
            Assert.IsTrue(boss.PourArms.Parts.All(a=>!a.Damageable),"a pour-arm can be struck while it glows");
            // Built wrong on purpose: only two plates shed, and it still glows.
            for(int i=0;i<(Broken?2:3);i++){yield return Turn(heart[i],hero);var dark=parts.Dark;Assert.IsNotNull(dark,"fixture: a plate is dark");Hit(dark,hero,dark.maximum*10);yield return Frames(2);}
            Assert.IsTrue(Said("COLOSSUS_COOLED"),"three plates shed and it hasn't cooled");Assert.IsFalse(boss.Hot);
            Assert.AreEqual(1-3*BossController.ColossusTune.PlateBar,body.integrity/body.maximum,.01f,"three plates didn't take three eighths of its bar");
            Assert.IsTrue(boss.PourArms.Parts.All(a=>a.Damageable),"cooled, its pour-arms can't be struck");Assert.IsFalse(heart.Any(s=>s.Available),"cooled, a sluice is still offered");
            yield return Wait(2.5f);Put(hero,At(0,.05f,-4.2f),false);
            // Its body can be hurt now, down to half and no further while an arm is on.
            Assert.Greater(Hit(body,hero,100),0,"cooled, its body takes nothing");
            Hit(body,hero,body.maximum);Assert.AreEqual(body.maximum*.5f+1,body.integrity,2,"its arms didn't hold its bar at half");
            StringAssert.Contains("BREAK THE POUR-ARMS",boss.StatusText);Assert.AreEqual("Break the pour-arms (0/2)",boss.FightGoal(""));
            var spoken=new List<string>();System.Action<string,string> heard=(who,line)=>spoken.Add(who+": "+line);BarkService.Spoken+=heard;
            try
            {
                var arms=boss.PourArms.Parts;Hit(arms[0],hero,arms[0].maximum*10);yield return Frames(2);
                Assert.AreEqual("Break the pour-arms (1/2)",boss.FightGoal(""));Assert.AreEqual(body.maximum*.5f+1,body.integrity,2,"the first arm took its bar over the line");
                Hit(arms[1],hero,arms[1].maximum*10);yield return Frames(2);
                Assert.IsTrue(Said("COLOSSUS_ARMS_OFF"),"both arms off and it doesn't tear loose");
            }
            finally{BarkService.Spoken-=heard;}
            Assert.IsTrue(spoken.Any(s=>s.Contains("One pour-arm off.")||s.Contains("An arm's off.")||s.Contains("One arm.")),"nobody says the first arm is off: "+string.Join(" | ",spoken));
            enemy.Passive=false;hero.Health.InvulnerableUntil=GameTime.Now+60;partner.Health.InvulnerableUntil=GameTime.Now+60;
            yield return Until(()=>Said("BOSS_SHIFT SlagColossus 2"),6,hero);Assert.IsTrue(Said("BOSS_SHIFT SlagColossus 2"),"the second arm torn loose and it never comes apart");
            Assert.IsTrue(Said("COLOSSUS_LEAKS"),"coming apart, it doesn't begin to leak");Assert.Less(body.integrity,body.maximum*.5f);
            enemy.Passive=true;
        }
        [UnityTest] public IEnumerator ComingApartItLeaksBrightSlagThatCrustsDarkAndOnlyTheBrightBurns()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            var enemy=Enemy("SlagColossus",At(0,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();var body=enemy.Health;
            Put(hero,At(0,.05f,-4));Hit(body,hero,1);body.integrity=body.maximum*.45f;enemy.Passive=false;hero.Health.InvulnerableUntil=GameTime.Now+60;
            yield return Until(()=>Said("COLOSSUS_LEAKS"),5,hero);Assert.IsTrue(Said("COLOSSUS_LEAKS"),"fixture: it has come apart");
            yield return Until(()=>!boss.Busy,6,hero);enemy.Passive=true;Put(hero,At(-40,.05f,-40));
            // It walks (here, pushed along its own feet): every few metres a bright pool behind it.
            int before=LingeringArea.All.Count(a=>a!=null&&a.Effect==AreaEffect.Burn);var controller=enemy.GetComponent<CharacterController>();
            // Built wrong on purpose: it is put somewhere, not walked, and that is no leak.
            if(Broken){controller.enabled=false;enemy.transform.position=At(12,.1f,0);controller.enabled=true;yield return Frames(3);}
            else for(int i=0;i<40;i++){controller.Move(Vector3.right*.25f);yield return null;}
            var bright=LingeringArea.All.Where(a=>a!=null&&a.Effect==AreaEffect.Burn).ToArray();
            Assert.Greater(bright.Length,before,"coming apart it walked ten metres and left no bright slag");
            var pool=bright.Last();var spot=pool.transform.position;
            Put(hero,new Vector3(spot.x,.05f,spot.z));float whole=hero.Health.integrity;yield return Wait(1.2f,hero);
            Assert.Less(hero.Health.integrity,whole,"bright slag doesn't burn a hero standing in it");
            Put(hero,At(-40,.05f,-40));yield return Wait(BossController.ColossusTune.LeakBright+.2f);
            var crust=LingeringArea.Over(spot);Assert.IsNotNull(crust,"bright slag doesn't crust where it lay");Assert.AreEqual(AreaEffect.Slow,crust.Effect,"its crust burns");
            Put(hero,new Vector3(spot.x,.05f,spot.z));whole=hero.Health.integrity;yield return Wait(1,hero);
            Assert.AreEqual(whole,hero.Health.integrity,.01f,"dark crust burns a hero standing on it: the chapter's rule is that dark crust is safe");
            Assert.Less(hero.Health.Pace,1,"its crust doesn't slow");
        }
        [UnityTest] public IEnumerator TheHeartLoadsItsSluicesDrainedOnceTheColossusIsDown()
        {
            yield return Zone("Arena_Ground");Floor();
            // Built wrong on purpose: the flag isn't set, and the sluice loads ready.
            if(!Broken)GameServices.Current.Flags.SetBool("bossdown.SlagColossus",true);
            var sluice=Sluice(1,At(0,0,-15),At(0,0,0));yield return Frames(2);
            Assert.AreEqual(CoolantSluice.Hold.Settled,sluice.State,"after the Colossus is down a sluice loads ready to turn");
            CoolantSluice.Offer(true);yield return null;Assert.IsFalse(sluice.Available,"a drained sluice can be offered");
        }

        // ------------------------------------------------------------------ Drask Vaal: the duel's rules
        [UnityTest] public IEnumerator TheWarmasterWaitsForTheFirstBlowThenOpensWithHisGuardWingAndTwoAnswer()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Def("MusterPikeguard");
            var ground=new GameObject("Duel ground").AddComponent<DuelGround>();ground.transform.position=At(0,0,0);ground.half=new Vector2(17.5f,12.5f);
            var post=new GameObject("Guard post").transform;post.position=At(14,0,10);ground.guardPost=post;
            var enemy=Enemy("DraskVaal",At(0,.1f,2),false,180);yield return Frames(3);var boss=enemy.GetComponent<BossController>();var body=enemy.Health;
            Put(hero,At(0,.05f,-8));hero.Health.InvulnerableUntil=GameTime.Now+60;
            yield return Wait(3,hero);
            Assert.IsTrue(boss.Waiting,"he didn't wait for the first blow");Assert.IsFalse(Said("BOSS_MOVE DraskVaal"),"he made a move before anyone had struck him");
            StringAssert.Contains("WAITS",boss.StatusText);
            // Built wrong on purpose: nobody strikes him, and the duel never opens.
            if(!Broken)Hit(body,hero,10);
            yield return Until(()=>Said("DRASK_SHOUT"),4,hero);
            Assert.IsTrue(Said("DRASK_DUEL_BEGINS struck"),"struck, he doesn't begin");Assert.IsTrue(Said("DRASK_SHOUT 1 answered=2"),"he doesn't open with his first shout, answered by two");
            Assert.AreEqual("Shout",log.First(l=>l.StartsWith("BOSS_MOVE DraskVaal")).Split(' ')[2],"his first move isn't his shout");
            yield return Until(()=>Alive("MusterPikeguard").Length>=2,3,hero);
            var guards=Alive("MusterPikeguard");Assert.AreEqual(2,guards.Length,"two of his guard wing didn't step in");Assert.IsTrue(Set("vask.shout1"),"the town isn't told of his first shout");
            foreach(var guard in guards)Assert.Less(Flat(guard.transform.position,At(14,0,10)),Flat(guard.transform.position,At(-14,0,-10)),"his guard didn't come in from his post's side");
            foreach(var guard in guards)Assert.IsTrue(ground.Holds(guard.transform.position),"his guard stepped in off the field");
        }
        [UnityTest] public IEnumerator HisShieldTurnsABlowFromTheFrontAndAFlankOrHisBackTakesItWhole()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            var enemy=Enemy("DraskVaal",At(0,.1f,0));yield return Frames(3);var body=enemy.Health;
            var kinds=new List<HitKind>();System.Action<Health,float,HitKind> seen=(victim,_,kind)=>{if(victim==body)kinds.Add(kind);};Health.DamageNumber+=seen;
            try
            {
                // He faces north (+z). Built wrong on purpose: the front blow is struck from behind, and lands whole.
                Put(hero,At(0,.05f,Broken?-2:2));Put(partner,At(0,.05f,-2));
                float front=Hit(body,hero,300);Assert.AreEqual(HitKind.Resisted,kinds.Last(),"a blow on his shield doesn't read as resisted");
                Put(hero,At(2,.05f,0),false);float flank=Hit(body,hero,300);float back=Hit(body,partner,300);
                Assert.AreEqual(300,flank,1,"a blow on his flank is turned");Assert.AreEqual(300,back,1,"a blow on his back is turned");
                Assert.AreEqual(300*BossController.DraskTune.ShieldTakes,front,1,"his shield doesn't take half of a blow from the front");
                // Open, he takes it whole from anywhere, half as much again.
                Put(hero,At(0,.05f,2),false);body.Expose(2);Assert.AreEqual(450,Hit(body,hero,300),1,"in his opening his shield still turns a blow");
            }
            finally{Health.DamageNumber-=seen;}
        }
        [UnityTest] public IEnumerator FewerAnswerEachShoutAndTheThirdIsAnsweredByNobody()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Def("MusterPikeguard");
            var enemy=Enemy("DraskVaal",At(0,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            Put(hero,At(0,.05f,-8));hero.Health.InvulnerableUntil=GameTime.Now+60;
            var spoken=new List<string>();System.Action<string,string> heard=(who,line)=>spoken.Add(who+": "+line);BarkService.Spoken+=heard;
            var counts=new List<int>();
            try
            {
                for(int i=0;i<3;i++)
                {
                    int before=Alive("MusterPikeguard").Length;
                    Assert.IsTrue(boss.Perform("Shout"),"fixture: his shout");yield return Until(()=>!boss.Busy,4,hero);yield return Wait(1.6f,hero);
                    counts.Add(Alive("MusterPikeguard").Length-before);
                    foreach(var guard in Alive("MusterPikeguard"))Object.Destroy(guard.gameObject);yield return Frames(2);
                }
            }
            finally{BarkService.Spoken-=heard;}
            // Built wrong on purpose: the count read the wrong way round.
            if(Broken)counts.Reverse();
            CollectionAssert.AreEqual(new[]{2,1,0},counts,"his guard doesn't answer two, then one, then nobody");
            for(int n=1;n<=3;n++)Assert.IsTrue(Set("vask.shout"+n),$"the town isn't told of his shout {n}");
            string[] his={"Drask: Guard wing! To me!","Drask: Arrek! Sool! To me!","Drask: Sool!"},count={"Taren: Two pikes in. Split them.","Taren: One pike.","Taren: Just him."};
            var drask=spoken.Where(s=>s.StartsWith("Drask:")).ToArray();var taren=spoken.Where(s=>s.StartsWith("Taren:")).ToArray();
            CollectionAssert.AreEqual(his,drask,"his shouts aren't his three, in order: "+string.Join(" | ",spoken));
            CollectionAssert.AreEqual(count,taren,"the hero's count isn't who came, in order: "+string.Join(" | ",spoken));
        }
        [UnityTest] public IEnumerator WhileAHeroIsDownHeStandsOffAndStrikingHimThenBreaksTheCustom()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Def("MusterPikeguard");
            var enemy=Enemy("DraskVaal",At(0,.1f,0),false);yield return Frames(3);var boss=enemy.GetComponent<BossController>();var body=enemy.Health;
            Put(hero,At(0,.05f,-6));Put(partner,At(4,.05f,-6));hero.Health.InvulnerableUntil=GameTime.Now+120;
            Hit(body,hero,10);yield return Until(()=>Said("DRASK_SHOUT 1")&&!boss.Busy,6,hero);Assert.IsTrue(Said("DRASK_SHOUT 1"),"fixture: the duel is on");
            yield return Until(()=>!boss.Busy,6,hero);
            // His partner falls (a blow the place deals, past any brace: Idra's guard stops most of what reaches her): he stands off, his guard with him, and makes no move.
            partner.Health.Receive(new DamagePacket{amount=partner.Health.maximum*1000,type=DamageType.Kinetic});Assert.IsFalse(partner.Health.Alive,"fixture: his partner is down");
            yield return Until(()=>boss.StandingOff,4,hero);Assert.IsTrue(boss.StandingOff,"a hero went down and he didn't stand off");
            StringAssert.Contains("STANDS OFF",boss.StatusText);StringAssert.Contains("Help your partner up",boss.FightGoal(""));
            foreach(var guard in Alive("MusterPikeguard"))Assert.IsTrue(guard.Passive,"his guard keeps fighting while he stands off");
            int moves=Count("BOSS_MOVE DraskVaal");float whole=hero.Health.integrity;yield return Wait(3,hero);
            Assert.AreEqual(moves,Count("BOSS_MOVE DraskVaal"),"standing off, he made a move");
            // Kept: the fallen is helped up, and he comes on again.
            partner.Health.Heal(partner.Health.maximum*.35f);
            yield return Until(()=>!boss.StandingOff,3,hero);Assert.IsFalse(boss.StandingOff,"his partner up, he still stands off");Assert.IsTrue(Said("DRASK_RESUMES"));
            Assert.IsFalse(boss.CustomBroken);
            yield return Until(()=>!boss.Busy,6,hero);
            // Broken: down again, and struck while he stands off. Built wrong on purpose: nobody strikes him.
            // A hero just helped up is a moment untouchable as they rise: wait that out.
            yield return Wait(1,hero);partner.Health.InvulnerableUntil=0;
            partner.Health.Receive(new DamagePacket{amount=partner.Health.maximum*1000,type=DamageType.Kinetic});Assert.IsFalse(partner.Health.Alive,"fixture: his partner is down again");
            yield return Until(()=>boss.StandingOff,4,hero);Assert.IsTrue(boss.StandingOff,"fixture: he stands off again");
            if(!Broken){Put(hero,At(0,.05f,-3),false);Hit(body,hero,50);}
            yield return Frames(2);
            Assert.IsTrue(boss.CustomBroken,"struck while he stood off, and the custom holds");Assert.IsTrue(Said("DRASK_CUSTOM_BROKEN"));
            yield return Until(()=>!boss.StandingOff,2,hero);Assert.IsFalse(boss.StandingOff,"the custom broken, he still stands off");
            StringAssert.Contains("CUSTOM IS BROKEN",boss.StatusText);
            yield return Until(()=>!boss.Busy,6,hero);yield return Wait(1,hero);
            Assert.AreEqual(2,Count("DRASK_STANDS_OFF"),"with the custom broken he stood off again for a fallen hero");
            Assert.IsFalse(boss.StandingOff,"with the custom broken he stands off again for a fallen hero");
            // And his next shout is answered as the last was: the town saw it.
            enemy.Passive=true;yield return Until(()=>!boss.Busy,4,hero);
            Assert.IsTrue(boss.Perform("Shout"),"fixture: his second shout");yield return Until(()=>!boss.Busy,4,hero);
            Assert.IsTrue(Said("DRASK_SHOUT 2 answered=2"),"with the custom broken his second shout wasn't answered as his first: "+string.Join(" | ",log.Where(l=>l.StartsWith("DRASK_SHOUT"))));
        }
        [UnityTest] public IEnumerator HisMissedThrustAndRushOpenHimAndAHitThereHearsHimSaySo()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            var enemy=Enemy("DraskVaal",At(0,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();var body=enemy.Health;
            var spoken=new List<string>();System.Action<string,string> heard=(who,line)=>spoken.Add(who+": "+line);BarkService.Spoken+=heard;
            try
            {
                // Past his thrust's 9 m, it meets nobody. Built wrong on purpose: inside it.
                Put(hero,At(0,.05f,Broken?5:12));
                Assert.IsTrue(boss.Perform("Thrust"),"fixture: his thrust");yield return Until(()=>body.Broken,4,hero);
                Assert.IsTrue(Said("DRASK_OVERREACHED"),"a thrust on nobody doesn't leave him overreached");Assert.IsTrue(body.Broken,"overreached, he isn't open");
                StringAssert.Contains("OVERREACHED",boss.StatusText);
                Put(hero,At(0,.05f,1.6f),false);Assert.AreEqual(150,Hit(body,hero,100),1,"in his opening a blow from the front isn't whole and half again");
                Assert.IsTrue(Said("DRASK_PRAISES"),"a clean blow in his opening and he says nothing");
                Assert.IsTrue(spoken.Any(s=>s=="Drask: Well struck."||s=="Drask: You're better than my reports."||s=="Drask: Again. Take your time."),"his duel line isn't said: "+string.Join(" | ",spoken));
                // A rush that meets nobody: his shield is down.
                yield return Until(()=>!boss.Busy&&!body.Broken,5,hero);Put(hero,At(30,.05f,0),false);hero.Health.InvulnerableUntil=GameTime.Now+30;
                Assert.IsTrue(boss.Perform("ShieldRush"),"fixture: his rush");Put(hero,At(0,.05f,0)+Vector3.right*40,false);
                yield return Until(()=>body.Broken,5,hero);Assert.IsTrue(Said("DRASK_SHIELD_DOWN"),"a rush on nobody doesn't drop his shield");
            }
            finally{BarkService.Spoken-=heard;}
        }
        [UnityTest] public IEnumerator AtTheEndOfHisBarHeYieldsHisGuardStandsDownAndNothingIsSaidOverHim()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Def("MusterPikeguard");
            var enemy=Enemy("DraskVaal",At(0,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();var body=enemy.Health;
            Put(hero,At(0,.05f,-8));hero.Health.InvulnerableUntil=GameTime.Now+60;
            Assert.IsTrue(boss.Perform("Shout"),"fixture: his shout");yield return Until(()=>!boss.Busy,4,hero);
            Assert.Greater(Alive("MusterPikeguard").Length,0,"fixture: his guard is on the field");
            string track=Def("DraskVaal").bossMusicId;Assert.AreEqual("Alien Boss Battle",track);
            yield return Wait(1.8f,hero);
            var spoken=new List<string>();System.Action<string,string> heard=(who,line)=>spoken.Add(who+": "+line);BarkService.Spoken+=heard;
            try{Hit(body,hero,body.maximum*10);yield return Wait(1.5f,hero);}finally{BarkService.Spoken-=heard;}
            Assert.IsFalse(body.Alive);Assert.IsTrue(Set("bossdown.DraskVaal"),"beaten, he isn't counted down");Assert.IsTrue(Said("DRASK_YIELDS"));
            Assert.AreEqual(BossController.Ending.Yields,BossController.EndingOf("DraskVaal"),"he isn't a man who yields");Assert.IsTrue(Def("DraskVaal").yieldsOnDefeat);
            // Built wrong on purpose (the check, not the fight): a guard counted as still standing.
            Assert.AreEqual(Broken?1:0,Alive("MusterPikeguard").Length,"his guard wing fights on after he has yielded");
            Assert.IsEmpty(spoken,"something is said over his yield, before DraskLeavesField: "+string.Join(" | ",spoken));
            Assert.AreEqual(0,body.Receive(new DamagePacket{source=hero.Health,amount=100,type=DamageType.Kinetic}),.01f,"he can be struck after he yields");
        }

        // ------------------------------------------------------------------ the skiff
        [UnityTest] public IEnumerator TheSkiffRunsALanceThroughTheShipItFightsAndTurnsWideOpen()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-40,-40));
            var enemy=Enemy("DraskSkiff",Sky(0,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();var body=enemy.Health;
            Put(hero,Sky(0,12));
            Assert.IsTrue(boss.Perform("LanceRun"),"fixture: its lance run");yield return null;
            var lane=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Line);Assert.IsNotNull(lane,"its run marks no lane");Assert.IsFalse(Faint(lane),"its lane is painted as a boundary: it strikes");
            Assert.IsTrue(lane.Contains(hero.transform.position),"its lane doesn't run through the ship it is fighting");
            // Built wrong on purpose: the ship crosses out of the lane before it comes.
            if(Broken)Put(hero,Sky(20,12),false);
            yield return Until(()=>body.Broken,5,hero);
            Assert.Less(hero.Health.integrity,5000,"the run went through a ship in its lane and missed it");
            Assert.IsTrue(Said("SKIFF_TURNS_WIDE"),"its run done, it doesn't turn wide");Assert.IsTrue(body.Broken,"turning wide, it isn't open");
            Assert.Greater(Flat(enemy.transform.position,Sky(0,0)),15,"it didn't run its lane");
        }
        [UnityTest] public IEnumerator FewerGunsAnswerEachCallAndEachShellIsMarkedOnTheWater()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(4,10));
            // The harbour's four batteries, two on the mole and two on the shore.
            for(int i=1;i<=4;i++){var go=new GameObject("Battery "+i);go.transform.position=Sky(-60+i*24,60);go.AddComponent<HarbourBattery>().index=i;}
            var enemy=Enemy("DraskSkiff",Sky(0,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            Put(hero,Sky(0,10));
            var spoken=new List<string>();System.Action<string,string> heard=(who,line)=>spoken.Add(who+": "+line);BarkService.Spoken+=heard;
            var shells=new List<int>();
            try
            {
                for(int call=0;call<3;call++)
                {
                    Put(hero,Sky(0,10),false);Put(partner,Sky(4,10),false);
                    Assert.IsTrue(boss.Perform("GunCall"),"fixture: his call to the guns");yield return null;
                    var circles=Marks(boss).Where(t=>t.Shape==Telegraph.Kind.Circle).ToArray();shells.Add(circles.Length);
                    if(call==0){Assert.IsTrue(circles.Any(c=>c.Contains(hero.transform.position)),"no shell is marked on the ship he is fighting");Assert.IsTrue(circles.All(c=>!Faint(c)),"a shell's circle is painted as a boundary");}
                    yield return Until(()=>!boss.Busy,4,hero,partner);yield return Wait(1.8f,hero,partner);
                }
            }
            finally{BarkService.Spoken-=heard;}
            // Built wrong on purpose: the count read the wrong way round.
            if(Broken)shells.Reverse();
            CollectionAssert.AreEqual(new[]{4,2,1},shells,"the guns don't answer all four, then two, then one");
            CollectionAssert.AreEqual(new[]{"Drask: Harbour guns, mark them.","Drask: Battery two. Battery four. Mark them.","Drask: Battery four."},spoken.Where(s=>s.StartsWith("Drask:")).ToArray(),
                "his calls to the guns aren't his three, in order: "+string.Join(" | ",spoken));
            Assert.IsTrue(spoken.Any(s=>s.StartsWith("Taren: Guns are marked on the water.")),"nobody says the guns are marked on the water: "+string.Join(" | ",spoken));
            Assert.Less(hero.Health.integrity,5000,"the guns' shells missed the ship they were marked on");
        }
        [UnityTest] public IEnumerator AtEachQuarterHeRunsForTheNextWaypointAndAtTheSlipHeCanRunNoFurther()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-40,-40));
            // The run: four waypoints 40 m apart up the harbour (under Auntie, round the mole, the slip).
            var legs=new PursuitLeg[4];for(int i=0;i<4;i++){var go=new GameObject("Leg "+i);go.transform.position=Sky(0,i*40);legs[i]=go.AddComponent<PursuitLeg>();legs[i].index=i;}
            var enemy=Enemy("DraskSkiff",Sky(0,0),false);yield return Frames(3);var boss=enemy.GetComponent<BossController>();var body=enemy.Health;
            Put(hero,Sky(0,-14));hero.Health.InvulnerableUntil=GameTime.Now+120;
            Assert.AreEqual(0,boss.SkiffLeg,"fixture: it holds station off the first waypoint");
            Hit(body,hero,1);yield return Until(()=>!boss.Busy,6,hero);
            // Built wrong on purpose: its bar is left at four fifths, over the first quarter's line.
            body.integrity=body.maximum*(Broken?.8f:.7f);
            yield return Until(()=>Said("SKIFF_RUN 1")&&!boss.Busy,10,hero);
            Assert.IsTrue(Said("SKIFF_RUN 1"),"under three quarters it didn't run for the next waypoint");Assert.AreEqual(1,boss.SkiffLeg);
            Assert.Less(Flat(enemy.transform.position,legs[1].transform.position),3,"it didn't get to the next waypoint");
            // Its shift is the second run; the third takes it to the slip, and there it can run no further.
            Put(hero,legs[1].transform.position+Vector3.back*14,false);body.integrity=body.maximum*.45f;
            yield return Until(()=>Said("SKIFF_RUN 2")&&!boss.Busy,14,hero);Assert.IsTrue(Said("BOSS_SHIFT DraskSkiff 2"),"under half it never shifts");Assert.IsTrue(Said("SKIFF_RUN 2"),"its shift doesn't run it on");
            Put(hero,legs[2].transform.position+Vector3.back*14,false);body.integrity=body.maximum*.2f;
            yield return Until(()=>Said("SKIFF_RUN 3")&&!boss.Busy,10,hero);Assert.AreEqual(3,boss.SkiffLeg,"under a quarter it isn't at the slip");
            StringAssert.Contains("can run no further",boss.FightGoal(""),"at the slip the objective doesn't say so");
            Put(hero,legs[3].transform.position+Vector3.back*14,false);
            Assert.IsTrue(boss.Perform("Run"),"fixture: a run at the slip");yield return Until(()=>!boss.Busy,3,hero);
            Assert.IsTrue(Said("SKIFF_RUN held"),"at the slip it ran on");Assert.Less(Flat(enemy.transform.position,legs[3].transform.position),3);
        }
        [UnityTest] public IEnumerator BroughtDownTheSkiffLeavesForTheFieldAndThePursuitIsWon()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-40,-40));
            var exit=new GameObject("Skiff exit").AddComponent<BossExit>();exit.boss="DraskSkiff";exit.transform.position=Sky(-60,0);
            var enemy=Enemy("DraskSkiff",Sky(0,0));yield return Frames(3);var body=enemy.Health;Put(hero,Sky(0,-10));
            Assert.AreEqual(BossController.Ending.Leaves,BossController.EndingOf("DraskSkiff"),"brought down, the skiff doesn't leave for the field");
            // Built wrong on purpose: it is left with a sliver of its bar.
            Hit(body,hero,Broken?body.maximum*.5f:body.maximum*10);yield return Wait(4.5f,hero);
            Assert.IsTrue(Set("bossdown.DraskSkiff"),"brought down, it isn't counted down");Assert.IsTrue(Set("vask.pursuitWon"),"the pursuit isn't won for the main quest");
            Assert.IsTrue(Said("BOSS_LEAVES DraskSkiff"),"it doesn't go");
        }

        // ------------------------------------------------------------------ what the fights are sized on
        const float FlatOutSeconds=15,DriverShare=.3f;
        /// <summary>The fewest frames a second any of a test's measures ran at, and the least a measure is believed at. A hero acts on a frame, so a machine too busy to draw sixty reads
        /// every pair slow (QaPlanCombatTests.FlatOutFrames is the same guard: on November 14, with a dozen Unitys at work, the shared measure read the Floodcrown a tenth slow). Under
        /// it a length test says it could not measure, which is neither a pass nor a failure.</summary>
        float slowest=float.MaxValue;const float MeasureFrames=50;
        void Measured(){if(slowest<MeasureFrames)Assert.Inconclusive($"a measure ran at {slowest:0} frames a second (under {MeasureFrames:0}), on a machine too busy to say how fast a pair is: run it again with the machine quieter");}
        /// <summary>A pair flat out on a body that never moves, as the shared length test measures it but with both heroes where a player has them: one who fights with an Edge at its reach,
        /// one with an Emitter a few steps off; the lead before the body (at Drask's shield, if <paramref name="lead"/> says front) and the partner behind it. Each stands facing the body, as
        /// whoever fights it does: put down facing north, a lead before Drask's shield faced away from him, and a shooter's aim only turns 120 degrees toward her target, so until
        /// November 14 Sela at his shield shot into the empty field and a pair she led was measured as Idra alone (0 to 6 a second from her). Every hero out has to land a blow a second.</summary>
        IEnumerator Rate(string id,string[] pair,bool frontLead,System.Action<float> measured,bool flight=false,bool judged=true)
        {
            yield return Deploy(pair);
            var at=flight?Sky(0,0):At(0,.1f,0);var heroes=PartyController.Current.members.ToArray();
            var enemy=Enemy(id,at);yield return Frames(3);var body=enemy.Health;
            enemy.GetComponent<BossController>().enabled=false;body.maximum=body.integrity=10000000;body.Holds=null;
            float reach=enemy.GetComponent<CharacterController>().radius+.9f;var homes=new Vector3[heroes.Length];
            for(int i=0;i<heroes.Length;i++)
            {
                var side=i==0&&frontLead?Vector3.forward:Vector3.back;
                homes[i]=flight?at+new Vector3(i==0?-2:2,0,(i==0&&frontLead?1:-1)*7):DevLoadout.Weapon(heroes[i].character)==GearSlot.Edge?at+Quaternion.Euler(0,i==0?-20:20,0)*side*reach:at+side*(reach+5)+Vector3.right*(i==0?-2:2);
            }
            // Built wrong on purpose: the yard as it stood before November 14, every hero put down facing north whichever side of the body she is on.
            void Home(int i){Put(heroes[i],homes[i],false);if(!Broken)Face(heroes[i],at);}
            for(int i=0;i<heroes.Length;i++){Put(heroes[i],homes[i]);Home(i);heroes[i].target=body;heroes[i].charge=100;}
            yield return null;
            var each=new float[heroes.Length];var blows=new int[heroes.Length];
            System.Action<Health,DamagePacket,float> count=(_,packet,amount)=>{for(int i=0;i<heroes.Length;i++)if(packet.source==heroes[i].Health){each[i]+=amount;if(amount>0)blows[i]++;}};body.Damaged+=count;
            int drawn=0;
            for(float end=Time.unscaledTime+FlatOutSeconds;Time.unscaledTime<end;drawn++)
            {
                for(int i=0;i<heroes.Length;i++)
                {
                    var hero=heroes[i];hero.motor.Move(Vector2.zero,false,false);
                    if(hero.CanAct&&Flat(hero.transform.position,homes[i])>.6f){var keep=hero.Health.integrity;Home(i);hero.Health.integrity=keep;}
                    hero.target=body;if(!hero.CanAct)continue;
                    bool acted=false;
                    for(int slot=0;slot<SkillBook.Slots&&!acted;slot++)
                    {
                        var skill=SkillBook.Equipped(hero.character,slot);
                        if(skill==null||hero.cooldowns[slot]>0||hero.charge<SkillBook.Cost(hero.character,skill))continue;
                        acted=hero.Skill(slot);
                    }
                    if(!acted)hero.Attack();
                }
                yield return null;
            }
            body.Damaged-=count;
            // A hero flat out lands several blows a second; one a second is the least a hero fighting it does. Sela facing away from Drask landed one skill's tick in fifteen seconds.
            for(int i=0;i<heroes.Length;i++)Assert.GreaterOrEqual(blows[i],FlatOutSeconds,$"fixture: {heroes[i].character} lands {blows[i]} blows on {id} in {FlatOutSeconds:0} s from {(i==0&&frontLead?"before it":"behind it")} ({each[i]/FlatOutSeconds:0} a second), so the pair is measured as one hero");
            float landed=each.Sum(),frames=drawn/FlatOutSeconds;if(judged)slowest=Mathf.Min(slowest,frames);
            TestContext.WriteLine($"RATE_EACH {id} {string.Join(" ",heroes.Select((h,i)=>$"{h.character}{(i==0&&frontLead?"@front":"")}={each[i]/FlatOutSeconds:0}/s"))} frames={frames:0}/s");
            measured(landed/FlatOutSeconds);
            enemy.Passive=true;Object.Destroy(enemy.gameObject);
            foreach(var left in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(left is GravityWell||left is PulseField||left is StarfallStrike||left is NetSeed)Object.Destroy(left.gameObject);
            yield return Frames(3);
        }
        static void Pinned(string id,int breakThreshold,out float integrity)
        {
            string source=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Scripts/Editor/World/HarrowgateBuilder.Bosses.cs"));
            integrity=Def(id).integrity;
            StringAssert.Contains($"e.id=\"{id}\";",source);StringAssert.Contains($"e.integrity={integrity:0};e.breakThreshold={breakThreshold};",source,$"a rebuild would put {id}'s old integrity back");
        }
        static readonly string[][] Pairs={new[]{"Taren","Idra"},new[]{"Taren","Sela"},new[]{"Sela","Idra"}};
        /// <summary>The time a sluice cycle costs a party besides the blows: from the plate to a wheel 12 m off and back at a walk (about 4 s), the 1.6 s turn and the quench's 1.4 s reel, and
        /// the engine's own moves in between (a roll or two). Four of them: one a plate, and one for a plate left glowing again.</summary>
        const float SluiceCycle=12;const int SluiceCycles=4;
        /// <summary>The Colossus's length on today's build for every pair a player can have out, at Sync 23 in the chapter's arrival parts: five eighths of its bar on its body (from the third plate
        /// down; the plates take the rest), the three plates' and two arms' own integrity, all at the driver's share of flat out, and four sluice cycles. About three minutes (140 to 220 s) for the pair the chapter arrives with
        /// (Taren and Idra), two to five for every pair; a dark plate can be broken inside its dark at the driver's share. The integrity is pinned to the builder.</summary>
        [UnityTest] public IEnumerator TheColossusLastsAboutThreeMinutesForEveryPairAPlayerCanPutOut()
        {
            Pinned("SlagColossus",900,out float integrity);
            // Built wrong on purpose: three times the integrity.
            if(Broken)integrity*=3;
            yield return Zone("Arena_Ground");Floor();yield return Level(23);
            var failures=new List<string>();
            foreach(var pair in Pairs)
            {
                float rate=0;yield return Rate("SlagColossus",pair,false,r=>rate=r);
                float driver=rate*DriverShare,parts=integrity*(3*BossController.ColossusTune.PlateIntegrity+2*BossController.ColossusTune.ArmIntegrity),
                    plate=integrity*BossController.ColossusTune.PlateIntegrity/driver,body=1-3*BossController.ColossusTune.PlateBar,seconds=(integrity*body+parts)/driver+SluiceCycles*SluiceCycle;
                string line=$"COLOSSUS_RATE {pair[0]}+{pair[1]} sync=23 tempered rate={rate:0}/s plate={plate:0.0}s total={seconds:0}s shared={integrity/driver:0}s quarter={integrity/(rate*.25f):0}s";
                TestContext.WriteLine(line);Debug.Log(line);
                bool arrival=pair[0]=="Taren"&&pair[1]=="Idra";
                if(arrival&&(seconds<140||seconds>220))failures.Add($"{line}: about three minutes, for the pair the chapter arrives with, is 140 to 220");
                if(seconds<120||seconds>300)failures.Add($"{line}: a boss is two to five minutes");
                if(plate>BossController.ColossusTune.Dark)failures.Add($"{line}: a dark plate can't be broken in its {BossController.ColossusTune.Dark} s at the driver's share");
                // The shared floor and ceiling are QaPlanCombatTests' own, on its own measure (Taren at its back, Idra at its flank): held there, not here on this one.
            }
            Measured();Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>The duel's length, inside its sheet's bounds (80 to 200 s), for every pair, at Sync 24 in the arrival parts: the lead at his shield and the partner at his back, as a duel goes, at the driver's
        /// share. His bare body's rate is measured and printed beside it; the sheet's bounds on that are the shared test's.</summary>
        [UnityTest] public IEnumerator TheDuelLastsWhatItsSheetSaysForEveryPair()
        {
            Pinned("DraskVaal",750,out float integrity);
            if(Broken)integrity*=3;
            yield return Zone("Arena_Ground");Floor();yield return Level(24);
            var failures=new List<string>();
            foreach(var pair in Pairs)
            {
                float duel=0,bare=0;yield return Rate("DraskVaal",pair,true,r=>duel=r);yield return Rate("DraskVaal",pair,false,r=>bare=r,judged:false);
                // His own sheet's share (.42, measured off two chains on November 14), not the class's .3, which is the Colossus's and the skiff's placeholder.
                var sheet=ContentRegistry.Boss("DraskVaal");
                float seconds=integrity/(duel*sheet.driverShare);
                string line=$"DRASK_RATE {pair[0]}+{pair[1]} sync=24 tempered duel={duel:0}/s bare={bare:0}/s total={seconds:0}s shared={integrity/(bare*sheet.driverShare):0}s quarter={integrity/(bare*.25f):0}s";
                TestContext.WriteLine(line);Debug.Log(line);
                if(seconds<sheet.minSeconds||seconds>sheet.maxSeconds)failures.Add($"{line}: its sheet asks {sheet.minSeconds:0} to {sheet.maxSeconds:0}");
                // The sheet's bounds on the bare body are QaPlanCombatTests' own, on its own measure: held there, not here.
            }
            Measured();Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>The pursuit's length, its sheet's 60 to 150 s, for every pair flying, at Sync 24: its bar at the flight driver's .29 of flat out, and its three runs (the tell, the run and the
        /// chase after it, about seven seconds each).</summary>
        [UnityTest] public IEnumerator ThePursuitLastsWhatItsSheetSaysForEveryPair()
        {
            Pinned("DraskSkiff",800,out float integrity);
            if(Broken)integrity*=3;
            yield return Zone("Arena_Flight");yield return Level(24);
            var failures=new List<string>();
            foreach(var pair in Pairs)
            {
                float rate=0;yield return Rate("DraskSkiff",pair,false,r=>rate=r,true);
                float seconds=integrity/(rate*.29f)+3*7;
                string line=$"SKIFF_RATE {pair[0]}+{pair[1]} sync=24 tempered rate={rate:0}/s total={seconds:0}s shared={integrity/(rate*.29f):0}s quarter={integrity/(rate*.25f):0}s";
                TestContext.WriteLine(line);Debug.Log(line);
                if(seconds<60||seconds>150)failures.Add($"{line}: its sheet asks 60 to 150");
                // The sheet's bounds on the bare body are QaPlanCombatTests' own, on its own measure: held there, not here.
            }
            Measured();Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
