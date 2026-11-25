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
    /// <summary>Chapter 9's two bosses, each in a proving yard with what its room gives it made here (nothing waits on the Separation Works or the Broken Ring being built): the Foundry Saint on
    /// a bare floor with the six arms its room's contract stands (it makes them itself where a room has none), and the Debris Cutter in the flight yard with lanes, districts and Bryn's tug where a
    /// test needs them. The party is the chapter's arrival (DevLoadout chapter9: three heroes, Taren and Idra out, Tempered parts) at the Sync each is met at, with every pair a player can put out
    /// where the fight is about the party. Hit sizes on all three heroes; the Saint's collars, arms, roads, climb and sweeps, and its settling; the Cutter's pods, its hunts by which pod went
    /// first, its beam, mines and tractor, and the tug; each fight's length; and the chapter's boss sheets for the shared tests.</summary>
    public sealed class HomeBossTests
    {
        const float X=230;
        readonly List<string> log=new();
        static readonly string[] Heeded={"BOSS_","SAINT_","CUTTER_","TOW_","BREAK_OK","EXPOSED"};
        void Heard(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        /// <summary>Off unless this class's own run sets it (HOME_BOSS_BROKEN=1): each test then builds its fixture wrong, so the run shows every test failing the way it should.</summary>
        static bool Broken=>System.Environment.GetEnvironmentVariable("HOME_BOSS_BROKEN")=="1";
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter9");log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void HomeBosses(ContentRegistry.Book book)
        {
            // The Foundry Saint: met at about Sync 26 (the chapter runs 25 to 27), in the Works' press hall (docs/maps/SeparationWorks.md, "Boss arena contract") once the three critical joints
            // are open. In the yard it makes its own six arms where the contract stands them: arm 4's strip runs south-south-east from its collar, and the slam takes the two nearest the lead.
            // Its share is what TheSaintLastsAboutThreeMinutesForEveryPairAPlayerCanPutOut expects the chain to measure against the shared test's flat out (BOSS_RATE 1,813 a second at Sync 26 on
            // November 14, both blades at the body): 88,000 over the model's about 178 s is .28. Planted, but it takes blows only in its openings (about a third of its first phase), so the
            // driver's .45 of what lands while it is open is about .28 of flat out overall. Not measured in a player yet.
            book.Boss(new ContentRegistry.BossSheet{id="FoundrySaint",zone="SeparationWorks",door="FoundrySaint",doorFlags=new[]{"home.joint1","home.joint2","home.joint3"},sync=26,driverShare=SaintExpectedShare,
                specials=new[]{("ArmSlam",new Vector3(1.46f,0,-6.76f)),("WeldLine",new Vector3(0,0,-9)),("HeadDrop",new Vector3(0,0,-5))},
                // The sweep crosses fallen arms, which the shared table's fresh press has none of: TheLastThreeArmsSweepTheRoadsTheyCrossAndThrowAClimberOff, in this class.
                testedApart=new[]{"Sweep"},
                // Its head drops and lifts slowly, open, in both phases.
                windows=new[]{("HeadDrop",new Vector3(0,0,-5)),("HeadDrop",new Vector3(0,0,-5))}});
            // The Debris Cutter: met at about Sync 26 in the Broken Ring (docs/maps/BrokenRing.md, "Cutter arena contract"), the zone reached once the districts have launched. An honest flying
            // bar (its pods choose, they don't take). Its share is measured, not a flight boss's .29: chapter-61's first play (nov14-recon31) got .05 of flat out with the braked chase
            // (60% of 123,000 left after 471 s, a fault in the driver's hands, not a share), and with the hands this fight asks for (ContinuousReview.Home.cs, CutterHeading) the arrival pair
            // fought by the driver's pad in the Ring (HomeReplayHandsTests) took 854 to 905 a second: 123,000 fell in 144.1 s, .35 of that run's own flat out (2,431) and .39 of the shared
            // test's (BOSS_RATE 2,214); 62 s samples .41 and .44. The lower is the sheet's. Taren with Sela out took 1,663 a second (.49) and Sela with Idra 1,075 (.29): at 155,000 (160,500 since November 21) about
            // 95 s and 145 s, the arrival pair about three minutes.
            book.Boss(new ContentRegistry.BossSheet{id="DebrisCutter",zone="BrokenRing",door="DebrisCutter",doorFlags=new[]{"home.launched"},flight=true,sync=26,driverShare=CutterShare,
                specials=new[]{("Beam",new Vector3(0,0,12)),("Mines",new Vector3(0,0,12)),("Tractor",new Vector3(0,0,12)),("Run",new Vector3(0,0,12))},
                // A run always ends turning wide, open, in both halves.
                windows=new[]{("Run",new Vector3(0,0,12)),("Run",new Vector3(0,0,12))}});
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
        /// <summary>Every hero in this frame (the arrival's Tempered, or the Choirwrought chapter 9's stalls sell), the rest of their parts as they were.</summary>
        static IEnumerator Wear(string frame)
        {
            var part=GameCatalog.Find<TechPartDef>(frame);Assert.IsNotNull(part,"fixture: "+frame+" is defined");
            var inv=new Inventory(State);foreach(var member in State.party)inv.Equip(member,inv.GivePart(part),part);
            PartyController.Current.RefreshStats();yield return null;
        }
        static IEnumerator Level(int sync)
        {
            foreach(var member in State.party)member.level=sync;PartyController.Current.RefreshStats();yield return null;
            foreach(var hero in PartyController.Current.members)Assert.AreEqual(Levels.MaxIntegrity(sync),hero.Health.maximum,.5f,"fixture: "+hero.character+"'s bar at Sync "+sync);
        }
        static Vector3 At(float x,float y,float z)=>new(X+x,y,z);
        static Vector3 Sky(float x,float z)=>new(200+x,1,z);
        static void Floor()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Home fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static (CombatActor lead,CombatActor partner) Party(){var lead=PartyController.Current.Active;return (lead,PartyController.Current.members.First(m=>m!=lead));}
        static void Put(CombatActor actor,Vector3 position,bool fresh=true)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            if(fresh){actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;}Physics.SyncTransforms();
        }
        static EnemyDef Def(string id){var definition=GameCatalog.Find<EnemyDef>(id);Assert.IsNotNull(definition,"fixture: "+id+" is defined (HomeBuilder.Definitions)");return definition;}
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
        bool Said(string start)=>log.Any(l=>l.StartsWith(start));
        int Count(string start)=>log.Count(l=>l.StartsWith(start));
        static Telegraph[] Marks(Object owner)=>Telegraph.Live.Where(t=>t!=null&&t.owner==owner&&!t.Released).ToArray();
        static bool Faint(Telegraph marking)=>marking.GetComponent<MeshRenderer>().sharedMaterials[1].name.Contains("faint");
        static List<string> Spoken(out System.Action<string,string> heard){var said=new List<string>();heard=(who,line)=>said.Add(who+": "+line);BarkService.Spoken+=heard;return said;}
        static List<string> Posted(out System.Action<string,string> posted){var said=new List<string>();posted=(h,b)=>said.Add(h+": "+b);Notices.Posted+=posted;return said;}

        // ------------------------------------------------------------------ the Saint's fixtures
        /// <summary>The press, woken and passive, facing north with its arms (its own, as the room's contract stands them) on its back to the south.</summary>
        static IEnumerator Saint(System.Action<EnemyBrain,BossController> got,Vector3? at=null)
        {
            var enemy=Enemy("FoundrySaint",at??At(0,.02f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            Assert.AreEqual(6,boss.SaintArms.Count,"fixture: the Saint stands its six arms");got(enemy,boss);
        }
        static SaintArm ArmNo(BossController boss,int index)=>boss.SaintArms.First(a=>a.index==index);
        /// <summary>Every arm's collar unlocked as the head's lift unlocks them, and these struck through: released as a player releases them.</summary>
        static IEnumerator Release(BossController boss,CombatActor hero,params int[] arms)
        {
            foreach(int index in arms)
            {
                var arm=ArmNo(boss,index);if(arm.IsReleased)continue;arm.Unlock(3);
                Assert.Greater(Hit(arm.Collar,hero,arm.Collar.maximum*20,0,DamageType.Pulse),0,$"fixture: arm {index}'s unlocked collar takes the blow");
                yield return null;
            }
            yield return Until(()=>arms.All(i=>ArmNo(boss,i).Resting),3);
            foreach(int index in arms)Assert.IsTrue(ArmNo(boss,index).Resting,$"fixture: arm {index} has come down");
        }
        static Vector3 OnRoad(SaintArm arm,float t)=>arm.RoadPoint(t)+Vector3.up*.08f;

        // ------------------------------------------------------------------ the chapter's loadout
        [UnityTest] public IEnumerator TheChapterNineLoadoutArrivesAsThreeAtSyncTwentyFiveInTemperedPartsWithTarenAndIdraOut()
        {
            // Built wrong on purpose: chapter 8's arrival is Sync 22.
            if(Broken)DevLoadout.Apply("chapter8");
            yield return Zone("Arena_Ground");
            CollectionAssert.AreEquivalent(new[]{"Taren","Sela","Idra"},State.party.Select(m=>m.id).ToArray(),"chapter 9 arrives without its three heroes");
            foreach(var member in State.party)Assert.AreEqual(25,member.level,member.id+" arrives at the wrong Sync: the plan ends chapter 8 at 25");
            CollectionAssert.AreEquivalent(new[]{"Taren","Idra"},PartyController.Current.members.Select(m=>m.character).ToArray(),"the pair out isn't the one a join leaves out");
            foreach(var member in State.party)
            {
                string worn=member.equipped.TryGetValue(GearSlot.Frame,out var id)?State.parts.FirstOrDefault(p=>p.instanceId==id)?.definitionId:null;
                Assert.AreEqual("FramesT4",worn,member.id+" doesn't arrive in the Tempered frame chapters 6 to 8 sell");
            }
            Assert.IsTrue(Set("chapter8.complete"),"chapter 8 isn't told");Assert.IsTrue(Set("vask.pursuitWon"),"chapter 8's story isn't told (its storyFlags)");
            Assert.IsFalse(Set("chapter9.complete"),"chapter 9 arrives already finished");
        }

        // ------------------------------------------------------------------ the numbers the bosses are tuned by
        /// <summary>The standing rule (CHAPTER_GUIDE section 0.1, D257): a boss's move costs a hero 12 to 16% of the bar, more than anything on the road to it, and under a fifth with its second
        /// phase's +18%; a move it has only in its second phase is held to 12 to 16 with that +18%. Measured through Health.Receive as a real blow lands, Plating and all, on all three heroes: the
        /// Saint at Sync 25 and 26, the Cutter at 26 and 27, each in the parts the chapter arrives in (Tempered) and in the ones its own stalls sell (Choirwrought), since a player may wear
        /// either by then. The road is every ordinary attack of the chapter defined so far. Every capsule is at least twice its radius tall, both are registered, and every chapter 9 zone is at
        /// threat 1.</summary>
        [UnityTest] public IEnumerator EveryBossMoveTakesABossesShareOfAnyHerosBarAndMoreThanTheRoadToIt()
        {
            yield return Zone("Arena_Ground");Floor();
            var source=Enemy("FoundrySaint",At(50,.05f,50)).Health;yield return Frames(2);
            var roadIds=new[]{"TowDrone","BridleFragment","HeatScavenger","SlagGull","OverrideRig","WeldArm","CompactWarden","Interceptor"};
            var road=roadIds.Select(id=>GameCatalog.Find<EnemyDef>(id)).Where(d=>d!=null&&!d.boss).SelectMany(d=>d.attacks.Where(a=>a!=null).Select(a=>(id:d.id,what:a.name,damage:a.damage))).ToList();
            TestContext.WriteLine($"HOME_ROAD {road.Count} attacks from {string.Join(",",road.Select(r=>r.id).Distinct())}");
            var saint=new List<(string what,float damage,bool later)>{("Arm Slam",BossController.SaintTune.SlamDamage,false),("Weld Line",BossController.SaintTune.WeldDamage,false),
                ("Head Drop",BossController.SaintTune.HeadDamage,false),("Sweep",BossController.SaintTune.SweepDamage,true)};
            // Built wrong on purpose: a slam authored at 175 is over the band at 25, and the check has to say so.
            if(Broken)saint.Add(("a slam authored at 175",175,false));
            var cutter=new List<(string what,float damage,bool later)>{("Beam",BossController.CutterTune.BeamDamage,false),("Mines",BossController.CutterTune.MineDamage,false),
                ("Tractor",BossController.CutterTune.TractorDamage,false),("Crush",BossController.CutterTune.CrushDamage,false),("Run",BossController.CutterTune.RunDamage,false)};
            var failures=new List<string>();
            foreach(var pair in new[]{new[]{"Taren","Idra"},new[]{"Sela","Idra"}})
            foreach(string frame in new[]{"FramesT4","FramesT5"})
            {
                yield return Deploy(pair);yield return Wear(frame);
                foreach(var (moves,syncs) in new[]{(saint,new[]{25,26}),(cutter,new[]{26,27})})
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
                            string line=$"HOME_BOSS_HIT {hero.character} sync={sync} {frame} plating={hero.Plating:0} {what} {damage:0} share={whole:P1} second={pressed:P1} road={named} {heaviest:P1}";TestContext.WriteLine(line);
                            float met=later?pressed:whole;
                            if(met<.12f||met>.16f)failures.Add(line+(later?": a move of its second phase is 12 to 16% of the bar with the phase's +18%":": a boss's move is 12 to 16% of the bar"));
                            if(!later&&pressed>.2f)failures.Add(line+": in its second phase, no blow takes a fifth of the bar");
                            if(met<=heaviest)failures.Add(line+": no more than the road to it");
                        }
                    }
                }
            }
            foreach(string zone in new[]{"NacreGrave","MeridianCrawl","Terminator","DecksLeaving","SeparationWorks","BrokenRing"})
            {var z=GameCatalog.Find<ZoneDef>(zone);if(z!=null&&!Mathf.Approximately(z.enemyThreat,1))failures.Add($"{zone} is at threat {z.enemyThreat}: chapter 9's damage is authored at its real weight, at 1");}
            foreach(string id in new[]{"FoundrySaint","DebrisCutter"})
            {
                var def=Def(id);float round=BossController.CapsuleRadius(id);
                if(def.capsuleHeight<round*2)failures.Add($"{id}'s capsule is {def.capsuleHeight} m tall on a {round} m radius: Unity stretches it and the body floats");
                if(!BossController.Registered(id))failures.Add(id+" has no Kit");
                if(def.attacks.Any(a=>a!=null&&a.range>0))failures.Add($"{id}'s brain has an attack that reaches someone: every blow it lands is a marked special");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the Foundry Saint
        /// <summary>Its arms stand in the room from the first look, raised, and nothing of them can be struck or targeted until it wakes (a press held for its preview, as the door holds it).</summary>
        [UnityTest] public IEnumerator ItsArmsStandRaisedBeforeItWakesAndNoCollarCanBeStruckUntilItDoes()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            // The room's six, as the contract stands them, before the press is there to claim them.
            var arms=new List<SaintArm>();for(int i=0;i<6;i++)arms.Add(SaintArm.Make("FoundrySaint",i+1,At(BossController.SaintTune.ArmX[i],2,-BossController.SaintTune.ArmBack),BossController.SaintTune.ArmBearing[i]));
            yield return Frames(2);
            Assert.IsTrue(arms.All(a=>!a.IsReleased&&a.Pitch<-40),"an arm doesn't stand raised before the fight");
            Assert.IsTrue(arms.All(a=>!a.Collar.Targetable),"a collar can be targeted before the press wakes");
            var enemy=Enemy("FoundrySaint",At(0,.02f,0));enemy.gameObject.AddComponent<DormantEnemy>().Hold(At(0,0,-28),30);
            yield return Frames(3);
            Assert.IsTrue(arms.All(a=>!a.Collar.Targetable),"a collar can be targeted while the press is held at its door");
            Put(hero,arms[3].transform.position+arms[3].Outward*.9f+Vector3.down*2);
            Assert.AreEqual(0,Hit(arms[3].Collar,hero,500),.01f,"a collar takes a blow before the fight");
            // Built wrong on purpose: the press is never let go.
            if(!Broken)enemy.GetComponent<DormantEnemy>().Activate();
            yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            Assert.IsTrue(Said("SAINT_WAKES arms=6"),"woken, the press doesn't take the room's six arms");Assert.AreEqual(6,boss.SaintArms.Count);
            Assert.IsTrue(arms.All(a=>a.Claimed&&a.Collar.Targetable),"woken, its collars can't be targeted");
        }
        /// <summary>The fault the brief warned of: hitting a collar and wondering why nothing happens. A shut collar takes nothing, reads as resisted, and the first blow on one says why on screen.</summary>
        [UnityTest] public IEnumerator AShutCollarTakesNothingAndTheFirstBlowOnOneSaysWhyOnScreen()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            EnemyBrain enemy=null;BossController boss=null;yield return Saint((e,b)=>{enemy=e;boss=b;});
            var arm=ArmNo(boss,4);Put(hero,arm.CollarFloor+arm.Outward*.9f);
            Assert.IsFalse(arm.Unlocked,"fixture: the collar is shut");
            var posted=Posted(out var post);var kinds=new List<HitKind>();System.Action<Health,float,HitKind> seen=(victim,_,kind)=>{if(victim==arm.Collar)kinds.Add(kind);};Health.DamageNumber+=seen;
            try
            {
                // Built wrong on purpose: the collar is unlocked first, so the blow lands.
                if(Broken)arm.Unlock(3);
                Assert.AreEqual(0,Hit(arm.Collar,hero,300),.01f,"a blow on a shut collar took something off it");
                Assert.AreEqual(HitKind.Resisted,kinds.Last(),"a blow on a shut collar doesn't read as resisted");
                Hit(arm.Collar,hero,300);
            }
            finally{Notices.Posted-=post;Health.DamageNumber-=seen;}
            Assert.AreEqual(1,Count("SAINT_COLLAR_SHUT"),"the first blow on a shut collar isn't said, once");
            Assert.AreEqual(1,posted.Count(p=>p.StartsWith("THE COLLAR IS SHUT")),"nothing on screen says why the collar took nothing: "+string.Join(" | ",posted));
            var said=posted.First(p=>p.StartsWith("THE COLLAR IS SHUT"));StringAssert.Contains("slams an arm",said,"what's on screen doesn't say when it opens");StringAssert.Contains("drops its head",said);
            Assert.IsFalse(arm.IsReleased);
        }
        /// <summary>A slam: the two raised arms nearest the lead come down on marked strips, and lie planted with their collars unlocked (and only theirs). A collar struck through there releases its
        /// arm: a fifth of the bar goes, the arm comes down and lies still, its road turns solid along the strip, and the party says so.</summary>
        [UnityTest] public IEnumerator ASlammedArmLiesPlantedWithItsCollarOpenAndAStruckCollarReleasesItAsARoad()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            EnemyBrain enemy=null;BossController boss=null;yield return Saint((e,b)=>{enemy=e;boss=b;});var body=enemy.Health;
            var four=ArmNo(boss,4);Put(hero,four.CollarFloor+four.Outward*4);
            Assert.IsTrue(boss.Perform("ArmSlam"),"fixture: the slam");yield return null;
            var strips=Marks(boss).Where(t=>t.Shape==Telegraph.Kind.Line).ToArray();
            Assert.AreEqual(2,strips.Length,"a slam doesn't mark two strips");Assert.IsTrue(strips.Any(s=>s.Contains(hero.transform.position)),"a slam doesn't come down on the lead's side");
            Assert.IsTrue(strips.All(s=>!Faint(s)),"a slam's strip is painted as a boundary: it strikes");
            yield return Until(()=>four.Planted,3,hero);
            var planted=boss.SaintArms.Where(a=>a.Unlocked).Select(a=>a.index).OrderBy(i=>i).ToArray();
            CollectionAssert.AreEqual(new[]{4,5},planted,"the slammed arms aren't the ones unlocked (and only they)");
            Assert.Less(hero.Health.integrity,5000,"the slam missed the hero in its strip");
            StringAssert.Contains("RELEASE A COLLAR",boss.StatusText,"the bar doesn't say the collars are open");
            var spoken=Spoken(out var heard);float before=body.integrity;
            try
            {
                // Built wrong on purpose: the collar struck after its window has shut.
                if(Broken)yield return Wait(BossController.SaintTune.SlamUnlock+.3f);
                Hit(four.Collar,hero,four.Collar.maximum*20,0,DamageType.Pulse);yield return null;
            }
            finally{BarkService.Spoken-=heard;}
            Assert.IsTrue(four.IsReleased,"a collar struck through while unlocked didn't release its arm");Assert.IsTrue(Said("SAINT_ARM_RELEASED 4 released=1"));
            Assert.AreEqual(before-body.maximum*BossController.SaintTune.ArmShare,body.integrity,2,"a collar struck through didn't take the fifth it carries off the bar");
            Assert.AreEqual("Release an arm at its shoulder collar (1/3)",boss.FightGoal(""));
            Assert.IsTrue(spoken.Any(s=>s.StartsWith("Taren: Collar's released.")),"nobody says the arm is released: "+string.Join(" | ",spoken));
            // It comes down and lies still, and the road is there: a ray down onto the middle of its strip meets it at about half its collar height.
            Put(hero,At(-30,.05f,-30));yield return Until(()=>four.Resting,3);
            Assert.IsTrue(four.Resting,"a released arm never comes to rest");
            var middle=four.RoadPoint(.5f);Assert.IsTrue(Physics.Raycast(middle+Vector3.up*3,Vector3.down,out var top,6,~0,QueryTriggerInteraction.Ignore),"no road under the fallen arm");
            Assert.AreEqual(middle.y,top.point.y,.15f,"the fallen arm's road isn't where its top should be");Assert.IsNull(top.collider.GetComponentInParent<Health>(),"the road is under something with a bar (a marking won't lie on it)");
            Assert.IsTrue(CombatCover.Opaque(top.collider),"the road isn't solid");
            // The other planted arm, not struck, rises again by itself.
            yield return Until(()=>!ArmNo(boss,5).Planted&&ArmNo(boss,5).Pitch<-40,BossController.SaintTune.SlamUnlock+2);Assert.Less(ArmNo(boss,5).Pitch,-40,"an arm left planted never rises again");
        }
        /// <summary>The head drops on a marked circle round the press and lifts slowly: the press is open, and every collar unlocks while the head is up. The party's line says so.</summary>
        [UnityTest] public IEnumerator TheHeadsLiftOpensThePressAndUnlocksEveryCollar()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            EnemyBrain enemy=null;BossController boss=null;yield return Saint((e,b)=>{enemy=e;boss=b;});var body=enemy.Health;
            // Built wrong on purpose: the lead stands outside its circle.
            Put(hero,At(0,.05f,Broken?-12:-5));
            var spoken=Spoken(out var heard);
            try{Assert.IsTrue(boss.Perform("HeadDrop"),"fixture: the head");yield return null;
                var circle=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Circle);Assert.IsNotNull(circle,"the head drop marks no circle");Assert.IsTrue(circle.Contains(hero.transform.position),"its circle doesn't take in a hero at its back");
                yield return Until(()=>Said("SAINT_HEAD_UP"),3,hero);}
            finally{BarkService.Spoken-=heard;}
            Assert.Less(hero.Health.integrity,5000,"the head missed a hero in its circle");
            Assert.IsTrue(body.Broken,"as the head lifts the press isn't open");Assert.GreaterOrEqual(body.BrokenUntil-Time.time,BossController.SaintTune.HeadUp-.6f,"the head's opening is short");
            Assert.IsTrue(boss.SaintArms.All(a=>a.Unlocked),"under the head's lift, not every collar is unlocked");
            Assert.IsTrue(spoken.Any(s=>s.StartsWith("Taren: Head's up.")),"nobody says the head is up: "+string.Join(" | ",spoken));
        }
        /// <summary>The bar is paid in blows (D272: parts that take bar with them give the fight away). While it fights, its raised arms take every blow on the press (resisted, and said once),
        /// and its head's lift opens it; a blow on an unlocked collar comes off the bar; each fifth the bar passes releases the arm whose collar was struck most, and nothing more comes off the bar
        /// for it than the blows took; three released, and the rig is bare.</summary>
        [UnityTest] public IEnumerator ItsArmsTakeEveryBlowOnThePressAndEachFifthItsBarPassesReleasesTheArmStruckMost()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            EnemyBrain enemy=null;BossController boss=null;yield return Saint((e,b)=>{enemy=e;boss=b;});var body=enemy.Health;
            // Awake, as it fights, with the party out past the 30 m it chooses a move inside: the test strikes it from there.
            Put(hero,At(0,.05f,-40));enemy.Passive=false;yield return Frames(2);
            float fifth=body.maximum*BossController.SaintTune.ArmShare;
            var posted=Posted(out var post);
            try{Assert.AreEqual(0,Hit(body,hero,500),.01f,"a blow on the press landed while its raised arms bear it");}finally{Notices.Posted-=post;}
            Assert.IsTrue(posted.Any(p=>p.StartsWith("THE ARMS HOLD IT")),"nothing on screen says the arms hold it: "+string.Join(" | ",posted));
            StringAssert.Contains("THE ARMS HOLD IT",boss.StatusText);StringAssert.Contains("RELEASE AN ARM AT THE SHOULDER",boss.StatusText);
            Assert.AreEqual("Release an arm at its shoulder collar (0/3)",boss.FightGoal(""));
            // Under the head's lift the press is open.
            body.Expose(.5f);float open=Hit(body,hero,fifth*.02f);Assert.Greater(open,0,"the head's lift doesn't open the press");
            yield return Wait(.7f);Assert.IsFalse(body.Broken,"fixture: the lift is over");
            // Two collars struck while unlocked: each blow comes off the bar, and no arm goes before the bar passes its line.
            var three=ArmNo(boss,3);var four=ArmNo(boss,4);three.Unlock(6);four.Unlock(6);
            float before=body.integrity,onFour=Hit(four.Collar,hero,fifth*.55f),onThree=Hit(three.Collar,hero,fifth*.3f);
            Assert.AreEqual(before-onFour-onThree,body.integrity,2,"a blow on an unlocked collar didn't come off the bar");
            yield return Frames(3);Assert.AreEqual(0,boss.SaintReleased,"an arm went before the bar passed its line");
            // Over the line, from arm 3's collar: arm 4's was struck most, and it is the one that goes. Built wrong on purpose: arm 3's is struck past it first.
            before=body.integrity;float over=Hit(three.Collar,hero,Broken?fifth*.4f:fifth*.2f);
            yield return Frames(3);
            Assert.IsTrue(four.IsReleased,"the bar passed its first line and the arm whose collar was struck most still holds");Assert.IsFalse(three.IsReleased,"the bar's first line let two arms go");
            Assert.AreEqual(before-over,body.integrity,2,"the arm let go took more off the bar than the blows did");
            Assert.IsTrue(Said("SAINT_ARM_RELEASED 4 released=1"));Assert.AreEqual("Release an arm at its shoulder collar (1/3)",boss.FightGoal(""));
            // Two collars struck through: each carries the fifth its arm holds, so each goes at its line; three down, the rig is bare.
            yield return Release(boss,hero,2,5);
            Assert.IsTrue(boss.SaintBare,"three arms released and the rig isn't bare");Assert.IsTrue(Said("SAINT_RIG_BARE"));
            Assert.Less(body.integrity,body.maximum*BossController.SaintTune.Bare,"three arms down and its bar isn't past its shift");
            Assert.AreEqual("Climb the fallen arms to the rig on its back",boss.FightGoal(""));
        }
        /// <summary>The bar and the arms never disagree: a bar set past arms' lines (as the shared tests set it) lets that many arms go.</summary>
        [UnityTest] public IEnumerator ABarPastItsLinesLetsThatManyArmsGo()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(-40,.05f,-30));
            EnemyBrain enemy=null;BossController boss=null;yield return Saint((e,b)=>{enemy=e;boss=b;});var body=enemy.Health;
            body.integrity=body.maximum*(Broken?.7f:.5f);yield return Frames(3);
            Assert.AreEqual(2,boss.SaintReleased,"half its bar gone and two arms aren't down");
            body.integrity=body.maximum*.3f;yield return Frames(3);
            Assert.AreEqual(3,boss.SaintReleased,"its bar under its shift and three arms aren't down");Assert.IsTrue(boss.SaintBare);
        }
        /// <summary>An opening lets one arm go at most, and the bar no further than that arm's line (D364). An EXTRA under the head's lift took 32 and 41% of its bar in the player, two arms
        /// and most of the rig's share (Builds/quality/nov22b-whole s59, nov22-recon-k-59: 76 and 61 s, where one that met a held press ran 145).</summary>
        [UnityTest] public IEnumerator AnOpeningLetsOneArmGoAndTheBarNoFurtherThanItsLine()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            EnemyBrain enemy=null;BossController boss=null;yield return Saint((e,b)=>{enemy=e;boss=b;});var body=enemy.Health;
            Put(hero,At(0,.05f,-40));enemy.Passive=false;yield return Frames(2);
            // Built wrong on purpose: its bar held nowhere, as it was.
            if(Broken)body.Holds=null;
            float share=BossController.SaintTune.ArmShare;
            // The head's lift: the press open and every collar unlocked, and a blow of 45% of its bar on it, as the EXTRA was.
            body.Expose(3);foreach(var arm in boss.SaintArms)arm.Unlock(3);
            Hit(body,hero,body.maximum*.45f);yield return Frames(3);
            Assert.AreEqual(1,boss.SaintReleased,"one opening let more than one arm go");
            Assert.GreaterOrEqual(body.integrity,body.maximum*(1-share)-2,"one opening took its bar past the arm's line");
            Assert.IsTrue(Said("SAINT_HOLDS"),"the arm went and nothing holds its bar");
            // The rest of that lift takes nothing more, on the press or on a collar.
            float held=body.integrity;var other=boss.SaintArms.First(a=>!a.IsReleased);
            Hit(body,hero,body.maximum*.1f);Hit(other.Collar,hero,other.Collar.maximum);yield return Frames(2);
            Assert.AreEqual(held,body.integrity,1,"the lift an arm went in took more off its bar");Assert.AreEqual(1,boss.SaintReleased,"the lift an arm went in let another go");
            // The next opening lets the next arm go, at its line.
            yield return Until(()=>!body.Broken,4);Assert.IsFalse(body.Broken,"fixture: the lift is over");yield return Frames(2);
            body.Expose(3);foreach(var arm in boss.SaintArms)arm.Unlock(3);
            Hit(body,hero,body.maximum*.45f);yield return Frames(3);
            Assert.AreEqual(2,boss.SaintReleased,"the next opening didn't let the next arm go");
            Assert.GreaterOrEqual(body.integrity,body.maximum*(1-2*share)-2,"the next opening took its bar past the next arm's line");
            enemy.Passive=true;
        }
        /// <summary>The rig bare: a blow from the floor is resisted (the first says so on screen); a blow from up on a fallen arm lands; and from the top of every fallen road a blade reaches the rig,
        /// which takes a blow twice over, while from the floor nothing does.</summary>
        [UnityTest] public IEnumerator WithTheRigBareOnlyABlowFromUpOnTheArmsLandsAndEveryRoadReachesTheRig()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            EnemyBrain enemy=null;BossController boss=null;yield return Saint((e,b)=>{enemy=e;boss=b;});var body=enemy.Health;
            Put(hero,At(-30,.05f,-30));yield return Release(boss,hero,1,3,6);Assert.IsTrue(boss.SaintBare,"fixture: the rig is bare");
            body.integrity=body.maximum*.35f;
            Put(hero,At(0,.05f,-3.4f),false);var posted=Posted(out var post);
            try{Assert.AreEqual(0,Hit(body,hero,500),.01f,"with the rig bare a blow from the floor landed");}finally{Notices.Posted-=post;}
            Assert.IsTrue(posted.Any(p=>p.StartsWith("THE RIG IS ON ITS BACK")),"nothing on screen says to climb: "+string.Join(" | ",posted));
            StringAssert.Contains("CLIMB",boss.StatusText);
            var rig=enemy.GetComponentsInChildren<Hurtbox>(true).FirstOrDefault(h=>h.name=="Override rig");Assert.IsNotNull(rig,"no rig target on its back");Assert.IsTrue(rig.gameObject.activeInHierarchy,"the rig target isn't up");
            Assert.AreEqual(2,rig.multiplier,"the rig doesn't take a blow twice over");var box=rig.GetComponent<BoxCollider>();
            const float Reach=1.3f;
            foreach(var road in boss.SaintArms.Where(a=>a.IsReleased))
            {
                // Built wrong on purpose: measured from the road's foot.
                var feet=road.RoadPoint(Broken?.1f:.92f);var chest=feet+Vector3.up;
                float gap=Vector3.Distance(box.ClosestPoint(chest),chest);
                TestContext.WriteLine($"SAINT_RIG_REACH road={road.index} feet={feet.y-road.Floor:0.00} gap={gap:0.00}");
                Assert.LessOrEqual(gap,Reach,$"from the top of fallen arm {road.index} a blade doesn't reach the rig ({gap:0.00} m)");
                Put(hero,feet+Vector3.up*.08f,false);yield return Frames(2);
                Assert.GreaterOrEqual(hero.transform.position.y-road.Floor,BossController.SaintTune.ClimbHeight,$"fixture: the hero stands up on fallen arm {road.index}");
                Assert.Greater(Hit(body,hero,100),0,$"a blow from up on fallen arm {road.index} took nothing");
            }
            var floorChest=enemy.transform.position+Vector3.back*(BossController.SaintTune.Radius+.35f)+Vector3.up;
            Assert.Greater(Vector3.Distance(box.ClosestPoint(floorChest),floorChest),Reach,"a blade reaches the rig from the floor: nobody needs to climb");
        }
        /// <summary>The climb, as a player makes it: a fallen arm is a road a hero walks up from its foot to its collar under the fixed camera, gentler than a nav slope, and from the game's camera
        /// nothing stands between the camera and a hero on it.</summary>
        [UnityTest] public IEnumerator AFallenArmIsARoadAHeroWalksUpInFullViewOfTheCamera()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            EnemyBrain enemy=null;BossController boss=null;yield return Saint((e,b)=>{enemy=e;boss=b;});
            Put(hero,At(-30,.05f,-30));yield return Release(boss,hero,4);var road=ArmNo(boss,4);
            float slope=Mathf.Atan2(SaintArm.Tune.RoadTopHeight,SaintArm.Tune.RoadFootOut-SaintArm.Tune.RoadTopOut)*Mathf.Rad2Deg;
            Assert.Less(slope,30,"the road is steeper than a walk");Assert.GreaterOrEqual(SaintArm.Tune.RoadWidth,1.6f,"the road is narrower than a hero and a margin");
            // Built wrong on purpose: he walks the other way.
            Put(hero,road.RoadPoint(0)+road.Outward*1.2f+Vector3.up*.05f,false);var up=-road.Outward*(Broken?-1:1);
            // Up the road until he is near its top, as a player stops there to strike the rig. Walked on for a fixed 3 s he went over the top into the press and slid off its side.
            for(float end=Time.time+4;Time.time<end&&!(road.OnRoad(hero.transform.position)&&road.RoadAt(hero.transform.position)>=.85f);){hero.motor.Move(new Vector2(up.x,up.z),false,false);yield return null;}
            yield return Wait(.3f,hero);
            float height=hero.transform.position.y-road.Floor;TestContext.WriteLine($"SAINT_CLIMB height={height:0.00} along={road.RoadAt(hero.transform.position):0.00}");
            Assert.GreaterOrEqual(height,1.6f,"a hero walking up the fallen arm didn't climb it");Assert.IsTrue(road.OnRoad(hero.transform.position),"walking up the fallen arm he fell off it");
            var view=Camera.main;Assert.IsNotNull(view,"fixture: the game's camera");
            var chest=hero.transform.position+Vector3.up*1.2f;var from=view.transform.position;
            var blockers=Physics.RaycastAll(from,(chest-from).normalized,Vector3.Distance(from,chest)-.5f,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponentInParent<CombatActor>()==null).ToArray();
            Assert.IsEmpty(blockers.Select(h=>h.collider.name).ToArray(),"from the fixed camera something stands between it and a hero on the road");
        }
        /// <summary>With the rig bare its last three arms sweep the roads: the top half of each road a sweep will cross is marked on the road itself; a climber caught there is struck and thrown off
        /// its side to the floor; the lower half of a road, and the floor, are safe.</summary>
        [UnityTest] public IEnumerator TheLastThreeArmsSweepTheRoadsTheyCrossAndThrowAClimberOff()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            EnemyBrain enemy=null;BossController boss=null;yield return Saint((e,b)=>{enemy=e;boss=b;});
            Put(hero,At(-30,.05f,-30));Put(partner,At(-40,.05f,-30));yield return Release(boss,hero,2,3,4);Assert.IsTrue(boss.SaintBare,"fixture: the rig is bare");
            var road=ArmNo(boss,3);
            Put(hero,OnRoad(road,.88f));Put(partner,OnRoad(ArmNo(boss,2),Broken?.88f:.15f));
            // A hero on the floor beside the sweep: never struck by it.
            var floorHero=hero;yield return Frames(2);
            Assert.IsTrue(boss.Perform("Sweep"),"fixture: the sweep");yield return null;
            var marks=Marks(boss).Where(t=>t.Shape==Telegraph.Kind.Line).ToArray();
            Assert.Greater(marks.Length,0,"the sweep marks nothing");Assert.IsTrue(marks.Any(m=>m.Contains(hero.transform.position)),"the sweep doesn't mark the road the lead stands on");
            Assert.IsTrue(marks.All(m=>!Faint(m)),"a sweep's marking is painted as a boundary: it strikes");
            // On the road itself: the marking's paint rises with it, well off the floor.
            foreach(var mark in marks){var bounds=mark.GetComponent<MeshRenderer>().bounds;TestContext.WriteLine($"SAINT_SWEEP_PAINT top={bounds.max.y-road.Floor:0.00} bottom={bounds.min.y-road.Floor:0.00}");
                Assert.Greater(bounds.max.y-road.Floor,1.6f,"a sweep's marking lies on the floor under the road, where a climber can't see it");}
            Assert.IsFalse(marks.Any(m=>m.Contains(partner.transform.position)),"the lower half of a road is marked: there is nowhere safe on it");
            yield return Until(()=>!boss.Busy,4,hero,partner);
            Assert.Less(hero.Health.integrity,5000,"the sweep missed a climber at the top of a marked road");Assert.IsTrue(Said("SAINT_THROWN "+hero.character),"a climber caught by the sweep isn't thrown off");
            yield return Wait(.8f,hero,partner);
            Assert.Less(hero.transform.position.y-road.Floor,.7f,"thrown off the road, the climber didn't come down to the floor");
            Assert.AreEqual(5000,partner.Health.integrity,.01f,"the sweep struck a hero on the lower half of a road");
        }
        /// <summary>The rig broken: the press settles where it stands (not destroyed, not shrunk away, not toppled), its last three arms come down and lie still, nothing barks over Orrin; and a
        /// room loaded after it has every arm down with its road solid.</summary>
        [UnityTest] public IEnumerator WithTheRigBrokenItSettlesWhereItStandsAndEveryArmLiesStill()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            EnemyBrain enemy=null;BossController boss=null;yield return Saint((e,b)=>{enemy=e;boss=b;});var body=enemy.Health;
            Put(hero,At(-30,.05f,-30));yield return Release(boss,hero,3,4,5);
            var top=ArmNo(boss,4);Put(hero,OnRoad(top,.9f),false);var visualRest=enemy.GetComponent<DefeatPresentation>().visual.localRotation;
            var spoken=Spoken(out var heard);
            try
            {
                body.integrity=10;Assert.Greater(Hit(body,hero,1000),0,"fixture: the last blow from the arms");
                // Built wrong on purpose: the body's own presentation is left to run.
                if(Broken)enemy.GetComponent<DefeatPresentation>().enabled=true;
                yield return Wait(4.5f);
            }
            finally{BarkService.Spoken-=heard;}
            Assert.IsTrue(Set("bossdown.FoundrySaint"),"down, it isn't counted down");Assert.IsTrue(Said("SAINT_SETTLES"),"it doesn't settle");
            Assert.AreEqual(BossController.Ending.ShutDown,BossController.EndingOf("FoundrySaint"));
            Assert.IsTrue(enemy!=null&&enemy.gameObject.activeInHierarchy,"the press is gone four seconds after it settled: it should stand where it is");
            Assert.AreEqual(Vector3.one,enemy.transform.localScale,"the press shrank away");
            Assert.Less(Quaternion.Angle(visualRest,enemy.GetComponent<DefeatPresentation>().visual.localRotation),3,"the press toppled as it settled");
            Assert.IsTrue(boss.SaintArms.All(a=>a.IsReleased),"its last arms don't come down as it settles");
            Assert.IsEmpty(spoken,"something is said over its settling, before OrrinSaintSpeaks: "+string.Join(" | ",spoken));
            var later=SaintArm.Make("FoundrySaint",7,At(20,2,20),180);yield return Frames(2);
            Assert.IsTrue(later.IsReleased&&later.Resting,"a room loaded after the Saint is down doesn't have its arms lying still");
        }

        // ------------------------------------------------------------------ the Debris Cutter
        static IEnumerator Cutter(System.Action<EnemyBrain,BossController> got,Vector3? at=null)
        {
            var enemy=Enemy("DebrisCutter",at??Sky(0,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();got(enemy,boss);
        }
        /// <summary>Disable a pod as a player does: open it as its move opens it (here, Perform), and strike it through while it vents.</summary>
        static IEnumerator Disable(BossController boss,CombatActor hero,int pod)
        {
            string move=pod==BossController.CutterTune.Beam?"Beam":pod==BossController.CutterTune.Mines?"Mines":"Tractor";
            yield return Until(()=>!boss.Busy&&!boss.Health.Broken,8,hero);
            Assert.IsTrue(boss.Perform(move),"fixture: its "+move);yield return Until(()=>boss.CutterVenting(pod),4,hero);
            Assert.IsTrue(boss.CutterVenting(pod),$"fixture: pod {pod} vents after its move");
            var part=boss.CutterPodHealth(pod);Hit(part,hero,part.maximum*20,0,DamageType.Beam);yield return null;
            Assert.IsFalse(boss.CutterHas(pod),$"fixture: pod {pod} is disabled");
        }
        /// <summary>Its pods are shut until they fire: no blow reaches one, and none can be targeted. Each vents after its own move (its target open, amber), the first vent says what the pods are
        /// on screen, and it shuts again after.</summary>
        [UnityTest] public IEnumerator EachPodIsShutUntilItFiresAndVentsAfterItsOwnMove()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-40,-40));
            EnemyBrain enemy=null;BossController boss=null;yield return Cutter((e,b)=>{enemy=e;boss=b;});
            Put(hero,Sky(0,14));
            for(int pod=0;pod<3;pod++)Assert.IsFalse(boss.CutterPodHealth(pod).Targetable,$"pod {pod} can be targeted before it has fired");
            var posted=Posted(out var post);
            try
            {
                foreach(var (move,pod) in new[]{("Beam",BossController.CutterTune.Beam),("Mines",BossController.CutterTune.Mines),("Tractor",BossController.CutterTune.Tractor)})
                {
                    yield return Until(()=>!boss.Busy,8,hero);Put(hero,Sky(0,14));
                    Assert.IsTrue(boss.Perform(move),"fixture: its "+move);
                    // Built wrong on purpose: the wrong pod is watched.
                    int watched=Broken?(pod+1)%3:pod;
                    yield return Until(()=>boss.CutterVenting(watched),4,hero);
                    Assert.IsTrue(boss.CutterVenting(watched),$"after its {move} pod {pod} doesn't vent");Assert.IsTrue(boss.CutterPodHealth(pod).Targetable,$"venting, pod {pod} can't be struck");
                    StringAssert.Contains("VENTING",boss.StatusText,"the bar doesn't say a pod is venting");
                    yield return Until(()=>!boss.CutterVenting(pod),BossController.CutterTune.Vent+4,hero);
                    Assert.IsFalse(boss.CutterPodHealth(pod).Targetable,$"pod {pod} stays open after its vent");
                }
            }
            finally{Notices.Posted-=post;}
            Assert.AreEqual(1,posted.Count(p=>p.StartsWith("A POD IS OPEN")),"the first vent isn't said on screen, once: "+string.Join(" | ",posted));
        }
        /// <summary>The player's choice, played three ways: a pod disabled takes a third of its bar and is its shift, and its pursuit changes by which went first. With no beam it closes in and
        /// crushes what its tractor reels; with no mines its beam cuts twice from further off; with no tractor its mines come in two rows, boxing a ship in.</summary>
        [UnityTest] public IEnumerator WhicheverPodGoesFirstChangesHowItHunts()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-40,-40));
            var expected=new[]{(pod:BossController.CutterTune.Beam,mode:"close"),(pod:BossController.CutterTune.Mines,mode:"standoff"),(pod:BossController.CutterTune.Tractor,mode:"boxing")};
            var failures=new List<string>();
            foreach(var (pod,mode) in expected)
            {
                EnemyBrain enemy=null;BossController boss=null;yield return Cutter((e,b)=>{enemy=e;boss=b;});var body=enemy.Health;
                Put(hero,Sky(0,14));hero.Health.InvulnerableUntil=GameTime.Now+60;
                yield return Disable(boss,hero,pod);
                // Built wrong on purpose: read before the pod has gone.
                string mode_=Broken?"whole":boss.CutterMode;
                if(mode_!=mode)failures.Add($"with pod {pod} first it hunts '{mode_}', not '{mode}'");
                if(Mathf.Abs(body.integrity/body.maximum-(1-BossController.CutterTune.PodBar))>.01f)failures.Add($"pod {pod} took {1-body.integrity/body.maximum:P0} of its bar, not a third");
                if(!Said("CUTTER_HUNTS "+mode))failures.Add($"pod {pod} gone doesn't log its hunt '{mode}'");
                log.Clear();
                // What the hunt looks like.
                yield return Until(()=>!boss.Busy&&!body.Broken,6,hero);Put(hero,Sky(0,14));
                if(mode=="standoff"){Assert.IsTrue(boss.Perform("Beam"));yield return Until(()=>!boss.Busy,6,hero);if(Count("CUTTER_BEAM")!=2)failures.Add($"standing off, its beam cut {Count("CUTTER_BEAM")} times, not twice");}
                if(mode=="boxing"){Assert.IsTrue(boss.Perform("Mines"));yield return null;int laid=Marks(boss).Count(t=>t.Shape==Telegraph.Kind.Circle);if(laid<BossController.CutterTune.MineCount*2-1)failures.Add($"boxing, it laid {laid} mines, not two rows");yield return Until(()=>!boss.Busy,6,hero);}
                if(mode=="close")
                {
                    Put(hero,Sky(0,16));Assert.IsTrue(boss.Perform("Tractor"));yield return Until(()=>Said("CUTTER_TRACTOR_CAUGHT")||Said("CUTTER_TRACTOR_MISSED"),3,hero);
                    yield return Until(()=>Marks(boss).Any(t=>t.Shape==Telegraph.Kind.Circle),4,hero);
                    if(!Marks(boss).Any(t=>t.Shape==Telegraph.Kind.Circle))failures.Add("closing in, what its tractor reels it doesn't crush");
                    yield return Until(()=>!boss.Busy,4,hero);
                }
                enemy.Passive=true;Object.Destroy(enemy.gameObject);yield return Frames(3);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>Its bar is paid in blows (D272: parts that take bar with them give the fight away): a blow on a venting pod comes off the bar as one on the hull does; the bar passing its first
        /// third takes the pod struck most, not the first in order, and nothing more comes off the bar for it than the blows took.</summary>
        [UnityTest] public IEnumerator ABlowOnAVentingPodComesOffItsBarAndEachThirdItPassesTakesThePodStruckMost()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-40,-40));
            EnemyBrain enemy=null;BossController boss=null;yield return Cutter((e,b)=>{enemy=e;boss=b;});var body=enemy.Health;
            Put(hero,Sky(0,14));hero.Health.InvulnerableUntil=GameTime.Now+60;
            float third=body.maximum*BossController.CutterTune.PodBar;
            Assert.IsTrue(boss.Perform("Mines"),"fixture: the mines");yield return Until(()=>boss.CutterVenting(BossController.CutterTune.Mines),4,hero);
            Assert.IsTrue(boss.CutterVenting(BossController.CutterTune.Mines),"fixture: the mine-layer vents after it lays");
            // Built wrong on purpose: the blow goes on a pod that is shut.
            var struck=boss.CutterPodHealth(Broken?BossController.CutterTune.Tractor:BossController.CutterTune.Mines);float before=body.integrity;
            float dealt=Hit(struck,hero,third*.4f);
            Assert.Greater(dealt,0,"a blow on the venting mine-layer took nothing");Assert.AreEqual(before-dealt,body.integrity,1,"a blow on a venting pod didn't come off the bar");
            Assert.AreEqual(3,boss.CutterPodsLeft,"a pod went before its bar passed a third");
            // The hull, for the rest of the third and a little over: the mine-layer, struck most, goes; the beam pod, first in order, is still working.
            before=body.integrity;float hull=Hit(body,hero,body.integrity-body.maximum*(1-BossController.CutterTune.PodBar)+third*.05f);yield return Frames(3);
            Assert.IsFalse(boss.CutterHas(BossController.CutterTune.Mines),"the bar passed its first third and the pod struck most is still working");
            Assert.IsTrue(boss.CutterHas(BossController.CutterTune.Beam)&&boss.CutterHas(BossController.CutterTune.Tractor),"a pod nobody struck went with it");
            Assert.AreEqual("standoff",boss.CutterMode,"with the mine-layer gone first it doesn't stand off");
            Assert.AreEqual(before-hull,body.integrity,1,"the pod gone took more off the bar than the blows did");
        }
        /// <summary>While a pod vents the Cutter keeps it turned on the ship it fired at, so whichever a player chooses faces him while it can be struck: the mine-layer's stern, the tractor's
        /// flank, the beam pod's nose. (Facing the lead with its nose between moves, its stern never came round to him.)</summary>
        [UnityTest] public IEnumerator WhileAPodVentsTheCutterKeepsItTurnedOnTheShipItFiredAt()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-40,-40));
            EnemyBrain enemy=null;BossController boss=null;yield return Cutter((e,b)=>{enemy=e;boss=b;});
            Put(hero,Sky(0,14));hero.Health.InvulnerableUntil=GameTime.Now+60;enemy.Passive=false;var failures=new List<string>();
            foreach(var (move,pod,face) in new[]{("Mines",BossController.CutterTune.Mines,Vector3.back),("Tractor",BossController.CutterTune.Tractor,Vector3.right),("Beam",BossController.CutterTune.Beam,Vector3.forward)})
            {
                yield return Until(()=>!boss.Busy&&!enemy.Health.Broken,8,hero);
                Assert.IsTrue(boss.Perform(move),"fixture: its "+move);
                // Its move done, its rest begun: the weave turns it while the pod still vents.
                yield return Until(()=>!boss.Busy,6,hero);yield return Wait(1.4f,hero);
                if(!boss.CutterVenting(pod)){failures.Add($"fixture: after its {move} pod {pod} has stopped venting");continue;}
                // Built wrong on purpose: its nose is watched, whichever pod vents.
                var faced=enemy.transform.TransformDirection(Broken?Vector3.forward:face);faced.y=0;var toShip=hero.transform.position-enemy.transform.position;toShip.y=0;
                float angle=Vector3.Angle(faced,toShip);TestContext.WriteLine($"CUTTER_FACES {move} {angle:0}");
                if(angle>25)failures.Add($"venting after its {move}, the pod is {angle:0} degrees off the ship it fired at");
            }
            enemy.Passive=true;
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>The districts are cover and landmarks, never a score: a beam is stopped where it meets one (its lane ends there and nobody behind it is struck), and when it is the market,
        /// Mira says so on its radio.</summary>
        [UnityTest] public IEnumerator ABeamStopsAtADistrictAndTheMarketSaysSo()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-40,-40));
            EnemyBrain enemy=null;BossController boss=null;yield return Cutter((e,b)=>{enemy=e;boss=b;});
            // The market district's cover across the line, 10 m off the Cutter's nose; the lead 20 m off, behind it.
            var market=new GameObject("Market district",typeof(BoxCollider));market.transform.position=Sky(0,10);market.GetComponent<BoxCollider>().size=new Vector3(16,6,4);
            var district=market.AddComponent<RingDistrict>();district.id="market";district.displayName="the market";
            // Built wrong on purpose: the district is moved out of the line.
            if(Broken)market.transform.position=Sky(40,10);
            Physics.SyncTransforms();Put(hero,Sky(0,20));
            var spoken=Spoken(out var heard);
            try
            {
                Assert.IsTrue(boss.Perform("Beam"),"fixture: the beam");yield return null;
                var lane=Marks(boss).First(t=>t.Shape==Telegraph.Kind.Line);
                Assert.Less(lane.Length,10,"the beam's lane runs on through the district");Assert.IsFalse(lane.Contains(hero.transform.position),"the beam's lane takes in a ship behind a district");
                yield return Until(()=>!boss.Busy,4,hero);yield return Wait(BossController.CutterTune.RadioAfter+.3f,hero);
            }
            finally{BarkService.Spoken-=heard;}
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"the beam struck a ship behind a district");
            Assert.IsTrue(Said("CUTTER_BEAM_STOPPED market"),"the beam stopped at the market doesn't say so");
            Assert.IsTrue(spoken.Any(s=>s.StartsWith("Mira: Market to the board. Something's hit my awning.")),"Mira doesn't say her awning was hit: "+string.Join(" | ",spoken));
        }
        /// <summary>The tractor: the first ship in its marked line is struck and reeled toward it, off its line; a roll breaks the line, and so does cover coming between.</summary>
        [UnityTest] public IEnumerator TheTractorReelsAShipInUntilItRollsOrCoverComesBetween()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-40,-40));
            EnemyBrain enemy=null;BossController boss=null;yield return Cutter((e,b)=>{enemy=e;boss=b;});
            Put(hero,Sky(0,20));
            Assert.IsTrue(boss.Perform("Tractor"),"fixture: the tractor");yield return null;
            var line=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Line);Assert.IsNotNull(line,"the tractor marks no line");Assert.IsTrue(line.Contains(hero.transform.position),"its line doesn't run to the ship");
            yield return Until(()=>Said("CUTTER_TRACTOR_CAUGHT"),2,hero);Assert.IsTrue(Said("CUTTER_TRACTOR_CAUGHT "+hero.character),"the ship in its line isn't caught");
            float caughtAt=Flat(hero.transform.position,enemy.transform.position);yield return Wait(.6f,hero);
            Assert.Less(Flat(hero.transform.position,enemy.transform.position),caughtAt-1.5f,"a caught ship isn't reeled in");
            Assert.IsTrue(boss.CutterVenting(BossController.CutterTune.Tractor),"pulling, the tractor pod isn't open");
            // Built wrong on purpose: no roll.
            if(!Broken)Assert.IsTrue(hero.Dodge(Vector3.left),"fixture: the roll");
            yield return Until(()=>Said("CUTTER_TRACTOR_BROKEN"),1.5f,hero);Assert.IsTrue(Said("CUTTER_TRACTOR_BROKEN rolled"),"a roll doesn't break the line");
            yield return Until(()=>!boss.Busy,4,hero);log.Clear();
            // Cover: a district coming between the pod and the ship breaks the line.
            Put(hero,Sky(0,20));Assert.IsTrue(boss.Perform("Tractor"));yield return Until(()=>Said("CUTTER_TRACTOR_CAUGHT"),2,hero);
            var cover=new GameObject("Crescent district",typeof(BoxCollider));var district=cover.AddComponent<RingDistrict>();district.id="crescent";district.displayName="the residential crescent";
            var between=(hero.transform.position+enemy.transform.position)*.5f;cover.transform.position=between;cover.GetComponent<BoxCollider>().size=new Vector3(8,6,1.5f);Physics.SyncTransforms();
            yield return Until(()=>Said("CUTTER_TRACTOR_BROKEN"),1.5f,hero);Assert.IsTrue(Said("CUTTER_TRACTOR_BROKEN cover"),"a district coming between doesn't break the line");
        }
        /// <summary>Its mines come in a row across the lane, a hull's width apart: between each pair there is room for the broadest hull a player flies (Idra's), and a ship there is never struck.</summary>
        [UnityTest] public IEnumerator ItsMinesLeaveAHullsWidthBetweenEachPair()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();
            EnemyBrain enemy=null;BossController boss=null;yield return Cutter((e,b)=>{enemy=e;boss=b;});
            Put(hero,Sky(0,14));Put(partner,Sky(-40,-40));
            Assert.IsTrue(boss.Perform("Mines"),"fixture: the mines");yield return null;
            var circles=Marks(boss).Where(t=>t.Shape==Telegraph.Kind.Circle).OrderBy(t=>t.Origin.x).ToArray();
            Assert.AreEqual(BossController.CutterTune.MineCount,circles.Length,"it doesn't lay its row");
            Assert.IsTrue(circles.Any(c=>c.Contains(hero.transform.position)),"its row doesn't lie across the lead");
            float widest=HeroCollision.HullRadius("Idra")*2;
            for(int i=1;i<circles.Length;i++)
            {
                float gap=Vector3.Distance(circles[i].Origin,circles[i-1].Origin)-circles[i].Radius-circles[i-1].Radius-2*Telegraph.BodyAllowance;
                // Built wrong on purpose: the gap held to a hull and a half.
                if(gap<(Broken?widest*1.5f:widest-2*Telegraph.BodyAllowance))Assert.Fail($"between mines {i-1} and {i} there is {gap:0.00} m: a {widest:0.00} m hull can't pass");
            }
            // The partner in the first gap is never struck.
            var gapAt=(circles[0].Origin+circles[1].Origin)*.5f;Put(partner,gapAt);
            yield return Until(()=>!boss.Busy,5,hero,partner);
            Assert.AreEqual(5000,partner.Health.integrity,.01f,"a ship between two mines was struck");Assert.Less(hero.Health.integrity,5000,"the mine laid on the lead missed him");
        }
        /// <summary>Its third pod gone it is dead in space and whole: not destroyed, not shrunk away, its crew aboard; nothing barks over Bryn; and the tug comes in, takes it on a line and tows it off.</summary>
        [UnityTest] public IEnumerator WithItsLastPodGoneItIsDeadInSpaceAndTheTugTowsItOff()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-40,-40));
            var tugBody=GameObject.CreatePrimitive(PrimitiveType.Cube);tugBody.name="Welcome Back";Object.Destroy(tugBody.GetComponent<Collider>());tugBody.transform.position=Sky(-40,20);
            var away=new GameObject("Tug away").transform;away.position=Sky(-110,20);var tug=tugBody.AddComponent<TowedOff>();tug.boss="DebrisCutter";tug.away=away;tug.speed=20;
            EnemyBrain enemy=null;BossController boss=null;yield return Cutter((e,b)=>{enemy=e;boss=b;});
            Put(hero,Sky(0,14));hero.Health.InvulnerableUntil=GameTime.Now+120;
            var spoken=Spoken(out var heard);
            try
            {
                yield return Disable(boss,hero,BossController.CutterTune.Tractor);yield return Disable(boss,hero,BossController.CutterTune.Mines);
                // Built wrong on purpose: the last pod is left.
                if(!Broken)yield return Disable(boss,hero,BossController.CutterTune.Beam);
                yield return Wait(4.5f,hero);
            }
            finally{BarkService.Spoken-=heard;}
            Assert.IsTrue(Set("bossdown.DebrisCutter"),"its pods gone, it isn't counted down");Assert.IsTrue(Said("CUTTER_DEAD_IN_SPACE"));
            Assert.AreEqual(BossController.Ending.ShutDown,BossController.EndingOf("DebrisCutter"));
            Assert.IsTrue(spoken.Any(s=>s.StartsWith("Taren: That's all three.")),"nobody says the third pod is down: "+string.Join(" | ",spoken));
            Assert.IsFalse(spoken.Any(s=>s.StartsWith("Taren: We can stop shouting")),"the shared down bark is said over Bryn's tow");
            Assert.IsTrue(Said("TOW_COMES DebrisCutter"),"the tug doesn't come for it");
            // The tug destroys the body the frame it logs TOW_GONE, and a destroyed object reads as gone only at that frame's end: read a frame on.
            yield return Until(()=>Said("TOW_GONE"),25);yield return Frames(2);
            Assert.IsTrue(Said("TOW_TAKES DebrisCutter"),"the tug never puts a line on it");Assert.IsTrue(Said("TOW_GONE DebrisCutter"),"the tug never tows it off");
            Assert.IsTrue(enemy==null,"towed off, the Cutter is still there");
        }

        // ------------------------------------------------------------------ what the fights are sized on
        const float FlatOutSeconds=15;
        /// <summary>A pair flat out on a body that never moves, as the shared length test measures it but with both heroes where a player has them: one who fights with an Edge at its reach, one
        /// with an Emitter a few steps off; or, <paramref name="climber"/> given, that hero alone standing at the top of a fallen arm (the partner on the floor, whose blows the bare press shrugs
        /// off). Flying, both ships 6 m off its stern, where the shared length test flies them (at 8 m Taren and Idra dealt the Cutter 1,523 a second to the shared test's 2,374: Idra's
        /// ship reaches at 6 m and not at 8). Also how often its break meter fills (breaks a second).</summary>
        IEnumerator Rate(string id,string[] pair,System.Action<float,float> measured,bool flight=false,string climber=null,float seconds=FlatOutSeconds)
        {
            yield return Deploy(pair);
            var at=flight?Sky(0,0):At(0,.02f,0);var heroes=PartyController.Current.members.ToArray();
            var enemy=Enemy(id,at);yield return Frames(3);var body=enemy.Health;var boss=enemy.GetComponent<BossController>();
            var homes=new Vector3[heroes.Length];
            if(climber!=null)
            {
                // Its rig bare, as three arms down leave it, the middle road for the climber.
                Put(heroes[0],At(-30,.05f,-30));Put(heroes[1],At(-36,.05f,-30));body.integrity=body.maximum*.3f;yield return Until(()=>boss.SaintBare&&boss.SaintArms.Count(a=>a.Resting)>=3,3);
                var road=boss.SaintArms.Where(a=>a.Resting).OrderBy(a=>Mathf.Abs(Mathf.DeltaAngle(a.Bearing,180))).First();
                for(int i=0;i<heroes.Length;i++)homes[i]=heroes[i].character==climber?road.RoadPoint(.9f)+Vector3.up*.08f:at+Vector3.back*(BossController.SaintTune.Radius+.9f)+Vector3.right*4;
            }
            float reach=BossReach.Edge(enemy);
            boss.enabled=false;body.maximum=body.integrity=10000000;body.Holds=null;
            for(int i=0;i<heroes.Length;i++)
            {
                if(climber!=null)continue;
                homes[i]=flight?at+new Vector3(i==0?-2:2,0,-6):DevLoadout.Weapon(heroes[i].character)==GearSlot.Edge?at+Quaternion.Euler(0,i==0?-20:20,0)*Vector3.back*reach:at+Vector3.back*(reach+5)+Vector3.right*(i==0?-2:2);
            }
            for(int i=0;i<heroes.Length;i++){Put(heroes[i],homes[i]);heroes[i].target=body;heroes[i].charge=100;}
            yield return null;
            float landed=0;var parts=new SortedDictionary<string,float>();
            System.Action<Health,DamagePacket,float> count=(_,packet,amount)=>
            {
                var by=heroes.FirstOrDefault(h=>packet.source==h.Health);if(by==null)return;landed+=amount;
                string key=$"{by.character}.{packet.type}.{packet.weight}{(packet.tag!=null?"/"+packet.tag:"")}";parts[key]=(parts.TryGetValue(key,out float sum)?sum:0)+amount;
            };
            body.Damaged+=count;
            int broke=Count("BREAK_OK"),shown=0;
            for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;shown++)
            {
                for(int i=0;i<heroes.Length;i++)
                {
                    var hero=heroes[i];hero.motor.Move(Vector2.zero,false,false);
                    if(hero.CanAct&&Flat(hero.transform.position,homes[i])>.6f){var keep=hero.Health.integrity;Put(hero,homes[i]);hero.Health.integrity=keep;}
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
            body.Damaged-=count;Assert.Greater(landed,0,"fixture: "+string.Join(" and ",pair)+" are hitting it");measured(landed/seconds,(Count("BREAK_OK")-broke)/seconds);
            RateFrames=shown/seconds;
            // Who dealt what, by kind of blow, a second (the lead at full scale, the partner at PartyController.PartnerShare): where a pair's rate comes from (D400).
            RateParts=string.Join(" ",heroes.Select(h=>$"{h.character}(x{h.damageScale:0.0})={parts.Where(p=>p.Key.StartsWith(h.character+".")).Sum(p=>p.Value)/seconds:0}"))+" ["+
                string.Join(" ",parts.Select(p=>$"{p.Key}={p.Value/seconds:0}"))+"]";
            enemy.Passive=true;Object.Destroy(enemy.gameObject);
            foreach(var left in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(left is GravityWell||left is PulseField||left is StarfallStrike||left is NetSeed)Object.Destroy(left.gameObject);
            yield return Frames(3);
        }
        /// <summary>How many frames a second the last <see cref="Rate"/> ran at. A measure taken on a machine too busy to draw fifty is not a rate to judge a bar by (the shared length test's
        /// rule, QaPlanCombatTests.FlatOutFrames): with a dozen engineers' Unitys at work on November 21 one pair's two readings in one run stood six per cent apart (3,924 and 4,177)
        /// where a quiet machine had read them a tenth of a per cent apart the day before (3,900 and 3,897), and the Cutter failed a floor it stands inside, a different pair each run.</summary>
        float RateFrames;
        /// <summary>The last <see cref="Rate"/> by hero and kind of blow, a second (D400).</summary>
        string RateParts="";
        const float RateFramesFloor=50;
        static void Pinned(string id,int breakThreshold,out float integrity)
        {
            string source=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Scripts/Editor/World/HomeBuilder.Bosses.cs"));
            integrity=Def(id).integrity;
            StringAssert.Contains($"e.id=\"{id}\";",source);StringAssert.Contains($"e.integrity={integrity:0};e.breakThreshold={breakThreshold};",source,$"a rebuild would put {id}'s old integrity back");
        }
        static readonly string[][] Pairs={new[]{"Taren","Idra"},new[]{"Taren","Sela"},new[]{"Sela","Idra"}};
        /// <summary>What the replay driver gets of a pair's own flat out: at a planted boss, D272's .36 to .57 (the Seedthane, the Floodcrown, the Rotbloom), so .45 (the Cutter's is its own, <see cref="CutterShare"/>). A quarter is the person who spends most of the fight dodging, whom the five minutes are for.</summary>
        /// .59 since D362 (November 22), the Saint's own: today's scouts felled 88,000 in 60.6 s (Builds/quality/nov22-recon-g s59, the 12:35 build) and 123,000 in 144.5 s
        /// (nov22-recon-h-59, the 14:52 build), the pair out Taren and Idra, for whom <see cref="SaintLength"/> gave 144 s at .45 for 88,000 on this class's rates that morning
        /// (d355-saint: 3,008 a second, the climb 2,970). It solves to .967 and .595, the lower. The length is not smooth in integrity: each head lift unlocks every collar, and at
        /// 88,000 one lift let two arms go, at 123,000 a collar took two or three lifts.
        const float PlantedShare=.59f,Dodging=.25f;
        /// <summary>The Cutter's share, measured by the driver's own hands for it (the sheet's comment, HomeBosses): <see cref="TheCutterLastsAboutThreeMinutesForEveryPairAPlayerCanPutOut"/> reads it off the sheet.</summary>
        /// .70 since D362 (November 22): today's scout of the 12:35 build felled 160,500 in 93.6 s (Builds/quality/nov22-recon-g s61) against 2,492 and 2,382 a second (d355-dome3,
        /// d355-lengths), which is .704; against this class's arrival pair (2,385, nov22a-part7) with its first five seconds and its shift it is .78. It is 210,000 since D362
        /// (continued): the shared slow pair's 400 s on its slowest reading (2,171) holds it there, about 113 s on the fastest scout (86.3 s at 160,500, nov22-recon-h-61).
        /// .61 since D384 (November 24): the round-three build felled 210,000 in 126.8 s and 184.7 s (Builds/quality/nov23-whole s61, nov23-recon-C s61, November 23), median 155.8,
        /// against the shared test's 2,225 a second (nov24a): .606. At that share no integrity passes this class's length test: the pairs with Sela read 3,600 to 3,850 a second
        /// flying, 1.6 times Taren and Idra's 2,395, so 120 s for them at .61 needs 264,000 and Taren and Idra's dodging pair holds it under 235,000 (D384).
        /// Still .61 since D400 (November 24): the flight pairs were narrowed (Sela's ship at CombatActor.SelaShipShare with her fan spread, Idra's cutter at a ship's beat), which moved
        /// the yardstick and the player's pace together, so the share is the same. This class's pairs then read Taren and Idra 2,647 and 2,557 a second, Taren and Sela 3,354 and
        /// 3,244, Sela and Idra 2,970 and 3,094 (d400-b, d400-v1): 1.27 apart. The room they leave is 231,000 (Taren and Sela's 120 s on 3,354) to 251,000 (the slow pair's 400 s
        /// on 2,557); the Cutter is 240,000, its middle (1.143 on 210,000, under D400's 1.164 between the flat outs, 2,236 to 2,602 a second on the shared test), Taren and Idra about
        /// 160 s, Taren and Sela 124 to 128, the slow pair 382 to 370. The in-house fight is no longer held to a share of it: HomeReplayHandsTests.CutterRuns, as played (D403).
        const float CutterShare=.61f;
        /// <summary>How long the Cutter's pairs are measured for, and how far apart two such readings have stood (a reading inside that of a bound is taken twice).</summary>
        const float CutterSeconds=60,CutterScatter=1.06f;
        /// <summary>The least a planted boss has given the driver (the Seedthane, .36): the Saint at that share of what lands while it is open is the slow fight the five minutes are for.
        /// (A quarter of what lands while it is open would be about a tenth of flat out overall, not the quarter the shared ceiling means.)</summary>
        const float PlantedLeast=.36f;
        /// <summary>The Saint's sheet share: what <see cref="TheSaintLastsAboutThreeMinutesForEveryPairAPlayerCanPutOut"/> expects the chain to measure against the shared test's flat out.</summary>
        /// .17, not .28 (D355, continued): the shared flat-out measure stood an edge .9 m off the Saint's capsule, where his blade didn't reach its broad body, and read 1,800 a second; from a
        /// quarter metre off it (BossReach.Edge) it reads 2,922, so the chain's own share of flat out is .28 x 1,800 / 2,922. The fight and its predicted length are unchanged.
        /// .29 since D362 (November 22), measured, no longer expected: today's scouts felled 88,000 in 60.6 s (Builds/quality/nov22-recon-g s59) and 123,000 in 144.5 s
        /// (nov22-recon-h-59) against 2,922 and 2,889 a second (d355-dome3, d355-lengths): .50 and .293, the lower. It is 115,000 now: at 123,000 Sela and Idra read 2,429 a second
        /// and their dodging fight (at <see cref="PlantedLeast"/>) stood at 302 s of 300 (d362-ch8to10); at 115,000 that is about 283. 140,000 since D364: an opening lets one arm go
        /// at most, so an EXTRA no longer hands over two arms and the rig's share, and the fight is paid for in openings again. 150,000 ran 161 to 194 s in the player
        /// (nov23-recon-a-59, -b-59) and its slow pair read 416 s of 400 on a slow climb reading (1,467 a second where 3,000 is usual); 140,000 leaves that reading inside.
        const float SaintExpectedShare=.29f;
        /// <summary>How much of the bare phase a climber is up on a road and striking: a sweep about every seven seconds sends him down the road or off it.</summary>
        const float ClimbUptime=.65f;
        /// <summary>The Saint's move cycle with its arms up, from its own numbers and the chooser's weights for a hero at its back (slam 2, weld 1.3, head 1.4): how long a move takes with its
        /// rest, and the share of that time something is open to a blow (a slam's two unlocked collars, every collar and the press under the head's lift), a second of each spent getting to it.</summary>
        static (float seconds,float open) SaintCycle()
        {
            float slam=BossController.SaintTune.SlamTell+BossController.SaintTune.SlamUnlock+BossController.SaintTune.RestHeld,
                weld=BossController.SaintTune.WeldTell+BossController.SaintTune.WeldLength*.5f/BossController.SaintTune.WeldSpeed+BossController.SaintTune.RestHeld,
                head=BossController.SaintTune.HeadTell+BossController.SaintTune.HeadUp+BossController.SaintTune.RestHeld;
            float ws=2,ww=1.3f,wh=1.4f,all=ws*slam+ww*weld+wh*head;
            return (all/(ws+ww+wh),(ws*(BossController.SaintTune.SlamUnlock-1)+wh*(BossController.SaintTune.HeadUp-1))/all);
        }
        /// <summary>Bare, the share of the time its head is up (the floor's only opening): sweep 2.2 (a climber up), weld 1.4 (the partner on the floor), head 1.2.</summary>
        static float SaintBareHeadOpen()
        {
            float sweep=BossController.SaintTune.SweepTell+.5f+BossController.SaintTune.RestBare,
                weld=BossController.SaintTune.WeldTellLater+BossController.SaintTune.WeldLength*.5f/BossController.SaintTune.WeldSpeed+BossController.SaintTune.RestBare,
                head=BossController.SaintTune.HeadTellLater+BossController.SaintTune.HeadUp+BossController.SaintTune.RestBare;
            return 1.2f*(BossController.SaintTune.HeadUp-1)/(2.2f*sweep+1.4f*weld+1.2f*head);
        }
        /// <summary>The Saint's length on today's build for a pair at a share of its own flat out. Its arms: three fifths of the bar, struck only in its openings (the cycle's, and a break's 3 s
        /// each time its meter fills, which it does at that share of how often it fills flat out). Its rig: two fifths, by the climber from the top of a road (twice over on the rig: the climb rate
        /// has that in it) up there <see cref="ClimbUptime"/> of the time, and the partner from the floor under the head's lift. Its first five seconds and its shift.</summary>
        static (float seconds,float arms,float rig) SaintLength(float integrity,float rate,float climb,float breaks,float share)
        {
            var (_,open)=SaintCycle();float broken=Mathf.Min(.4f,breaks*share*3);
            float arms=integrity*(1-BossController.SaintTune.Bare)/(rate*share*Mathf.Min(1,open+broken));
            float rig=integrity*BossController.SaintTune.Bare/(climb*share*ClimbUptime+rate*.5f*share*SaintBareHeadOpen());
            return (5+arms+2+rig,arms,rig);
        }
        /// <summary>The Saint's length on today's build for every pair a player can have out, at Sync 26 in the chapter's arrival parts, at a planted boss's share of the pair's flat out (the bar
        /// is paid in blows, struck only in its openings: <see cref="SaintLength"/>). About three minutes (140 to 220 s) for the pair the chapter arrives with, two to five for every pair, and no
        /// more than five for any pair at the least share a planted boss has given the driver (<see cref="PlantedLeast"/>). The share its sheet carries (<see cref="SaintExpectedShare"/>) is this length's for the arrival pair against the shared
        /// test's own flat out (its BOSS_RATE), worked by hand in the sheet's comment: this fixture's placement is not the shared test's. The integrity is pinned to the builder.</summary>
        [UnityTest] public IEnumerator TheSaintLastsAboutThreeMinutesForEveryPairAPlayerCanPutOut()
        {
            Pinned("FoundrySaint",1400,out float integrity);
            // Built wrong on purpose: three times the integrity.
            if(Broken)integrity*=3;
            yield return Zone("Arena_Ground");Floor();yield return Level(26);
            var (cycle,open)=SaintCycle();var failures=new List<string>();
            foreach(var pair in Pairs)
            {
                float rate=0,breaks=0,climb=0;yield return Rate("FoundrySaint",pair,(r,b)=>{rate=r;breaks=b;});yield return Rate("FoundrySaint",pair,(r,_)=>climb=r,false,pair[0]);
                var (seconds,arms,rig)=SaintLength(integrity,rate,climb,breaks,PlantedShare);float dodging=SaintLength(integrity,rate,climb,breaks,PlantedLeast).seconds;
                string line=$"SAINT_RATE {pair[0]}+{pair[1]} sync=26 tempered pair={rate:0}/s climb={climb:0}/s breaks={breaks:0.00}/s cycle={cycle:0.0}s open={open:0.00} arms={arms:0}s rig={rig:0}s total={seconds:0}s dodging={dodging:0}s";
                bool arrival=pair[0]=="Taren"&&pair[1]=="Idra";
                if(arrival&&(seconds<140||seconds>220))failures.Add($"{line}: about three minutes, for the pair the chapter arrives with, is 140 to 220");
                TestContext.WriteLine(line);Debug.Log(line);
                if(seconds<120||seconds>300)failures.Add($"{line}: a boss is two to five minutes");
                if(dodging>ContentRegistry.SlowPairCeiling(300))failures.Add($"{line}: at the least share a planted boss has given the driver it takes over {ContentRegistry.SlowPairCeiling(300):0} s (D362 continued)");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>The Cutter's length on today's build for every pair flying, at Sync 26. Its bar is honest (a blow on a venting pod comes off it as one on the hull does; its pods choose, they
        /// don't take), so it lasts its integrity at its sheet's share of the pair's flat out (measured by the driver's hands for this fight, <see cref="CutterShare"/>), with its first five seconds and its shift. About three minutes for the arrival pair, two to five for
        /// every pair, and no more than five at a quarter of flat out.</summary>
        [UnityTest,Timeout(900000)] public IEnumerator TheCutterLastsAboutThreeMinutesForEveryPairAPlayerCanPutOut()
        {
            Pinned("DebrisCutter",800,out float integrity);
            if(Broken)integrity*=3;
            var failures=new List<string>();float slowest=float.MaxValue;
            foreach(var pair in Pairs)
            {
                // Each pair from a game just started, two minute-long readings and their mean, always. Read one after another in one game the readings climbed with the
                // minutes already fought (Sela and Idra: 3,910 as the fourth reading of a run, 4,171 as the seventh, 4,553 as the ninth, November 21), so a pair's number
                // depended on how many readings stood before it, and a third reading taken to steady a mean pushed it over the floor it was steadying. A minute, as the
                // shared length test takes (QaPlanCombatTests.FlatOutSeconds), not this class's fifteen seconds, which read Taren and Sela anywhere from 3,602 to 4,144.
                Application.logMessageReceived-=Heard;yield return Boot();yield return Zone("Arena_Flight");yield return Level(26);
                float rate=0;yield return Rate("DebrisCutter",pair,(r,_)=>rate=r,true,null,CutterSeconds);slowest=Mathf.Min(slowest,RateFrames);
                TestContext.WriteLine($"CUTTER_PARTS {pair[0]}+{pair[1]} {RateParts}");Debug.Log($"CUTTER_PARTS {pair[0]}+{pair[1]} {RateParts}");
                float first=rate;yield return Rate("DebrisCutter",pair,(r,_)=>rate=r,true,null,CutterSeconds);slowest=Mathf.Min(slowest,RateFrames);
                TestContext.WriteLine($"CUTTER_PARTS {pair[0]}+{pair[1]} {RateParts}");Debug.Log($"CUTTER_PARTS {pair[0]}+{pair[1]} {RateParts}");
                string readings=$"{first:0} and {rate:0}";rate=(first+rate)*.5f;
                // Its own sheet's share (measured, HomeBossTests.HomeBosses), not the class's flight placeholder: D312's rule for the Warmaster.
                float share=ContentRegistry.Boss("DebrisCutter").driverShare;
                bool arrival=pair[0]=="Taren"&&pair[1]=="Idra";
                float seconds=5+integrity/(rate*share)+2,dodging=5+integrity/(rate*Dodging)+2;
                string line=$"CUTTER_RATE {pair[0]}+{pair[1]} sync=26 tempered rate={rate:0}/s share={share:0.00} total={seconds:0}s dodging={dodging:0}s read={readings} frames={RateFrames:0}/s";
                TestContext.WriteLine(line);Debug.Log(line);
                if(arrival&&(seconds<140||seconds>220))failures.Add($"{line}: about three minutes, for the pair the chapter arrives with, is 140 to 220");
                if(seconds<120||seconds>300)failures.Add($"{line}: a boss is two to five minutes");
                if(dodging>ContentRegistry.SlowPairCeiling(300))failures.Add($"{line}: a pair at a quarter of flat out takes over {ContentRegistry.SlowPairCeiling(300):0} s (D362 continued)");
            }
            if(slowest<RateFramesFloor)Assert.Inconclusive($"the Cutter's pairs were measured at as few as {slowest:0} frames a second (under {RateFramesFloor:0}), on a machine too busy to say how fast a pair is. Run it again with the machine quieter."+(failures.Count>0?"\nRead meanwhile:\n"+string.Join("\n",failures):""));
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
