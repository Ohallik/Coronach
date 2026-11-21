using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Chapter 5's fight, Hulljack on the hull and Dessik Varn in the freight hall, each in the proving yards with stand-ins for what its room gives it (joins, a boom, the tug, a roof
    /// door; stacks and loose crates), so nothing waits on Many Hands being built. The hit sizes against the party the chapter is met by. The frame: feet shut until one is planted, a cut
    /// foot a quarter of the bar and a leap to the next join with the tug's haulers, the grip giving way when it is battered over a line, the hull's booms as cover and a grappled ship
    /// rolling free, and its fourth foot taking it through the roof door with nothing said over its last lines. The suit: no feet and no walking, a dodged claw hauling it to what it bit,
    /// the pulse dragging crates down marked lanes, burning lines that stop at cover, cargo thrown as cover, the kneel and its cockpit, and the alarm at half and his yield in quiet.
    /// Then the length of the whole fight at the party's measured rates. The chapter's expectations for the shared enemy and boss tests are here too.</summary>
    public sealed class ManyHandsCombatTests
    {
        const float X=230;
        readonly List<string> log=new();readonly List<(string key,Transform emitter)> cues=new();
        static readonly string[] Heeded={"HULLJACK_","DESSIK_","BREACH_","BOSS_","PART_","TETHER_","COMBAT_KILL","BREAK_OK"};
        void Heard(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        void Cue(string key,Transform emitter)=>cues.Add((key,emitter));
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter5");log.Clear();cues.Clear();Application.logMessageReceived+=Heard;AudioManager.CueStarted+=Cue;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;AudioManager.CueStarted-=Cue;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void ManyHands(ContentRegistry.Book book)
        {
            // Where a ship stands for the hauler's tell: inside the 13 m it holds at.
            book.Tell("ClaimHauler",9,flight:true);
            // One fight in two parts, each its own boss with its own bar, about a minute and a half each for the real party (DevLoadout chapter5): so each part is held to a minute to two
            // and a half on its own (the shared test's floor at the driver's share, its ceiling at a quarter of flat out; measured, both sit near 90 and 105 s), and
            // TheWholeFightLastsWhatThePlanAsksAtThePartysMeasuredRates holds the two together to the plan's two to five minutes (about three). Met at the plan's Sync 15 (the chapter runs
            // 15 to 16). The doors are the hull's and the hall's (docs/maps/ManyHandsHull.md, "Boss arena contract" and "Freight hall contract"). The driver's share in flight is the
            // Cantor's, the one flight boss measured off a chain.
            book.Boss(new ContentRegistry.BossSheet{id="Hulljack",zone="ManyHandsHull",door="Hulljack",doorFlags=new[]{"hands.claimRead"},flight=true,sync=15,driverShare=.29f,minSeconds=60,maxSeconds=150,
                specials=new[]{("TorchSweep",new Vector3(0,0,8)),("GrappleShot",new Vector3(0,0,10)),("DebrisFan",new Vector3(0,0,10)),("Leap",new Vector3(0,0,14)),("Plant",new Vector3(0,0,4))},
                // A planted foot opens the frame in both halves.
                windows=new[]{("Plant",new Vector3(0,0,5)),("Plant",new Vector3(0,0,5))}});
            // The cargo throw lands a crate where its hero stood, so it goes after the moves stood for at that spot; the torch's burning lines outlast it, so it goes last.
            book.Boss(new ContentRegistry.BossSheet{id="DessikVarn",zone="ManyHands",door="DessikVarn",doorFlags=new[]{"hands.claimRead","bossdown.Hulljack","hands.hallEntered"},sync=15,driverShare=.3f,
                minSeconds=60,maxSeconds=150,
                specials=new[]{("Claw",new Vector3(0,0,8)),("MagnetPulse",new Vector3(0,0,4)),("CargoThrow",new Vector3(0,0,10)),("TorchLines",new Vector3(0,0,6))},
                // A claw thrown at a hero past its reach bites nothing (or the wall behind him) and leaves the frame open, in both halves.
                windows=new[]{("Claw",new Vector3(0,0,19)),("Claw",new Vector3(0,0,19))}});
        }

        // ------------------------------------------------------------------ fixtures
        static bool Set(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
        }
        static Vector3 At(float x,float y,float z)=>new(X+x,y,z);
        /// <summary>The flight yard's open space, on its plane.</summary>
        static Vector3 Sky(float x,float z)=>new(200+x,1,z);
        static void Floor()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Many Hands fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static GameObject Block(string name,Vector3 centre,Vector3 size)
        {var wall=new GameObject(name,typeof(BoxCollider));wall.transform.position=centre;wall.GetComponent<BoxCollider>().size=size;Physics.SyncTransforms();return wall;}
        static (CombatActor lead,CombatActor partner) Party(){var lead=PartyController.Current.Active;return (lead,PartyController.Current.members.First(m=>m!=lead));}
        /// <summary>Stood there facing +z, whole, with a bar no fixture blow will empty.</summary>
        static void Put(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;Physics.SyncTransforms();
        }
        static void Stand(EnemyBrain enemy,Vector3 position)
        {var body=enemy.GetComponent<CharacterController>();bool was=body.enabled;body.enabled=false;enemy.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=was;Physics.SyncTransforms();}
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true)
        {var definition=GameCatalog.Find<EnemyDef>(id);Assert.IsNotNull(definition,"fixture: "+id+" is defined (ManyHandsBuilder.Definitions)");var enemy=ActorFactory.Enemy(definition,at);enemy.Passive=passive;return enemy;}
        static DamageType Neutral(Health body)=>System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
        static float Hit(Health body,CombatActor from,float amount=100,string tag=null,float breakPower=0)=>body.Receive(new DamagePacket{source=from.Health,amount=amount,type=Neutral(body),tag=tag,breakPower=breakPower});
        static float Kill(Health body,CombatActor from)=>body.Receive(new DamagePacket{source=from.Health,amount=body.maximum*20,type=Neutral(body)});
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
        static Telegraph[] Marks(BossController boss)=>Telegraph.Live.Where(t=>t!=null&&t.owner==boss&&!t.Released).ToArray();

        /// <summary>The hull as the contract gives it, in the flight yard: four joins in the frame's order, the tug standing off to the south, and the roof door it leaves by.</summary>
        static HullJoin[] Joins()
        {
            var at=new[]{Sky(0,0),Sky(-30,28),Sky(30,28),Sky(30,-6)};var joins=new HullJoin[at.Length];
            for(int i=0;i<at.Length;i++){var go=new GameObject("Fixture join "+(i+1));go.transform.position=at[i];joins[i]=go.AddComponent<HullJoin>();joins[i].index=i+1;}
            return joins;
        }
        static ClaimTug Tug(Vector3 at){var go=new GameObject("Fixture tug");go.transform.position=at;return go.AddComponent<ClaimTug>();}
        /// <summary>A cargo boom standing up through the flight plane, solid at the ships' height.</summary>
        static GameObject Boom(Vector3 at,Vector3 size)=>Block("Fixture boom",new Vector3(at.x,1,at.z),new Vector3(size.x,4,size.z));
        /// <summary>A freight crate standing loose on the floor.</summary>
        static LooseCargo Crate(Vector3 floor)=>LooseCargo.Make(floor);

        // ------------------------------------------------------------------ the numbers the chapter is tuned by
        /// <summary>The standing rule (CHAPTER_GUIDE 0.1): an ordinary hit costs a hero 9 to 13% of the bar. Measured through Health.Receive as a real blow lands, Plating and all, on both
        /// heroes as the chapter meets them: the chapter 5 loadout (Prime parts) at Sync 15, and the same party at 16. Both zones are at threat 1, so the authored damage is the damage.</summary>
        [UnityTest] public IEnumerator EveryOrdinaryHitTakesANinthToAnEighthOfAHerosBarAcrossTheChapter()
        {
            yield return Zone("Arena_Ground");Floor();var source=Enemy("ClaimHauler",At(30,1,30)).Health;yield return Frames(2);
            var failures=new List<string>();
            foreach(int sync in new[]{15,16})
            {
                foreach(var member in GameServices.Current.State.party)member.level=sync;PartyController.Current.RefreshStats();yield return null;
                foreach(var hero in PartyController.Current.members)
                {
                    float bar=hero.Health.maximum;Assert.AreEqual(Levels.MaxIntegrity(sync),bar,.5f,"fixture: the hero's bar at Sync "+sync);
                    foreach(var attack in GameCatalog.Find<EnemyDef>("ClaimHauler").attacks)
                    {
                        hero.Health.integrity=bar;hero.Health.InvulnerableUntil=0;float share=hero.Health.Receive(new DamagePacket{source=source,amount=attack.damage,type=attack.type,weight=HitWeight.Medium})/bar;
                        string line=$"MANYHANDS_HIT sync={sync} {hero.character} bar={bar:0} plating={hero.plating:0} {attack.name} {attack.damage:0} share={share:P1}";TestContext.WriteLine(line);Debug.Log(line);
                        if(share<.09f||share>.13f)failures.Add(line+": the plan asks 9 to 13%");
                    }
                }
            }
            foreach(string zone in new[]{"ManyHands","ManyHandsHull"})
            {var z=GameCatalog.Find<ZoneDef>(zone);if(z!=null&&!Mathf.Approximately(z.enemyThreat,1))failures.Add($"{zone} is at threat {z.enemyThreat}: chapter 5's damage is authored at its real weight, at 1");}
            // A capsule shorter than twice its radius is stretched by Unity round its middle, and the body floats.
            foreach(string id in new[]{"Hulljack","DessikVarn"})
            {var def=GameCatalog.Find<EnemyDef>(id);float round=BossController.CapsuleRadius(id);if(def.capsuleHeight<round*2)failures.Add($"{id}'s capsule is {def.capsuleHeight} m tall on a {round} m radius");}
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>A boss hits harder than the road to it (CHAPTER_GUIDE 0.1): every blow of both parts, the plain swipes among them, takes 12 to 16% of a Sync 15 bar in the chapter's
        /// Prime parts in each phase it is used (the second adds <see cref="BossController.PhaseStep"/>), and more than the claim hauler's tether, the heaviest ordinary hit on the hull.
        /// Written against the first numbers, which broke it (the grapple's bite at 5%, the moves at 9 to 11%, and with three shifts the frame's last quarter at +54%).</summary>
        [UnityTest] public IEnumerator EveryBlowOfTheFightHitsHarderThanTheRoadToIt()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));
            foreach(var member in GameServices.Current.State.party)member.level=15;PartyController.Current.RefreshStats();yield return null;
            var source=Enemy("DessikVarn",At(40,.1f,40)).Health;yield return Frames(2);
            float Share(float amount){hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=0;return hero.Health.Receive(new DamagePacket{source=source,amount=amount,type=DamageType.Kinetic,weight=HitWeight.Heavy})/hero.Health.maximum;}
            Assert.Greater(Share(64*(1+3*BossController.PhaseStep)),.16f,"fixture: the first crush, 64, three shifts in, should overshoot");Assert.Less(Share(30),.12f,"fixture: the first grapple bite, 30, should fall short");
            var road=GameCatalog.Find<AttackDef>("HaulTether");float heaviest=Share(road.damage);
            int[] both={1,2},second={2};
            var blows=new (string blow,float damage,int[] phases)[]
            {
                ("the frame's swipe",GameCatalog.Find<AttackDef>("HullSwipe").damage,both),("torch sweep",BossController.HulljackTune.TorchDamage,both),("grapple's bite",BossController.HulljackTune.GrappleCatch,both),
                ("grapple's crush",BossController.HulljackTune.CrushDamage,both),("plant",BossController.HulljackTune.PlantDamage,both),("debris fan",BossController.HulljackTune.DebrisDamage,both),
                ("leap's pass",BossController.HulljackTune.LeapDamage,both),("leap's landing",BossController.HulljackTune.LandingDamage,both),
                ("the suit's swipe",GameCatalog.Find<AttackDef>("SuitSwipe").damage,both),("claw's bite",BossController.DessikTune.ClawCatch,both),("claw's slam",BossController.DessikTune.SlamDamage,both),
                ("claw's haul",BossController.DessikTune.HaulDamage,both),("torch lines",BossController.DessikTune.TorchDamage,both),("magnet pulse",BossController.DessikTune.PulseDamage,both),
                ("dragged crate",BossController.DessikTune.CrateDamage,both),("cargo throw",BossController.DessikTune.ThrowDamage,second),
            };
            Assert.AreEqual(1,BossController.ThresholdsOf("Hulljack").Length,"fixture: the frame has one shift");Assert.AreEqual(1,BossController.ThresholdsOf("DessikVarn").Length,"fixture: the suit has one shift");
            var failures=new List<string>();var shares=new List<string>{$"road {road.name} {heaviest:P1}"};
            foreach(var (blow,damage,phases) in blows)foreach(int phase in phases)
            {
                float share=Share(damage*(1+(phase-1)*BossController.PhaseStep));shares.Add($"{blow} p{phase} {share:P1}");
                if(share<.12f||share>.16f)failures.Add($"the {blow} in phase {phase} takes {share:P1} of a Sync 15 bar: a boss's blow takes 12 to 16%");
                if(share<=heaviest)failures.Add($"the {blow} in phase {phase} takes {share:P1}, no more than {road.name} on the road to it ({heaviest:P1})");
            }
            TestContext.WriteLine("MANYHANDS_BOSS_SHARES "+string.Join("; ",shares));Debug.Log("MANYHANDS_BOSS_SHARES "+string.Join("; ",shares));
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ Hulljack, on the hull
        [UnityTest] public IEnumerator TheFramesFeetAreShutUntilItPlantsOneAndOnlyThePlantedFootCanBeCut()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-90,-90));Put(hero,Sky(0,9));Joins();
            var frame=Enemy("Hulljack",Sky(0,0));yield return Frames(3);var boss=frame.GetComponent<BossController>();var body=frame.Health;
            var feet=frame.GetComponent<BossParts>();Assert.IsNotNull(feet,"the frame has no feet to cut");Assert.AreEqual(BossController.HulljackTune.Feet,feet.Parts.Count);
            foreach(var foot in feet.Parts){Assert.IsFalse(foot.Targetable,"a foot can be targeted before it is planted");Assert.AreEqual(0,Kill(foot,hero),.01f,"a foot can be cut before it is planted");}
            Assert.AreEqual(1,boss.AtJoin,"set down on the first join, it doesn't know it");Assert.IsTrue(boss.Clamped,"on a join with its feet on, it isn't clamped");
            var kinds=new List<HitKind>();System.Action<Health,float,HitKind> seen=(victim,_,kind)=>{if(victim==body)kinds.Add(kind);};Health.DamageNumber+=seen;
            try{Assert.AreEqual(100*BossController.HulljackTune.Clamp,Hit(body,hero),1,"clamped, a blow on the frame isn't cut to its share");Assert.AreEqual(HitKind.Resisted,kinds.Last(),"a clamped frame's blow doesn't read as resisted");}
            finally{Health.DamageNumber-=seen;}
            StringAssert.Contains("WAIT FOR A FOOT",boss.StatusText,"its bar doesn't say what to wait for");
            StringAssert.Contains("(0/4)",boss.FightGoal(""),"the objective doesn't count the feet");
            var spoken=new List<string>();System.Action<string,string> heard=(who,line)=>spoken.Add(who+": "+line);BarkService.Spoken+=heard;
            try
            {
                Assert.IsTrue(boss.Perform("Plant"),"fixture: the plant");yield return null;
                var stamp=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Circle);Assert.IsNotNull(stamp,"the plant marks nothing where the foot comes down");
                yield return Until(()=>boss.PlantedFoot>=0,3,hero);
            }
            finally{BarkService.Spoken-=heard;}
            Assert.GreaterOrEqual(boss.PlantedFoot,0,"the plant planted nothing");Assert.IsTrue(Said("HULLJACK_PLANT "+boss.PlantedFoot));
            var planted=feet.Parts[boss.PlantedFoot];
            Assert.IsTrue(planted.Targetable,"the planted foot can't be targeted");Assert.AreEqual(1,feet.Parts.Count(p=>p.Targetable),"more than the planted foot can be cut");
            float nearest=feet.Parts.Min(p=>Flat(p.transform.position,hero.transform.position));Assert.AreEqual(nearest,Flat(planted.transform.position,hero.transform.position),.01f,"it planted a foot other than the one nearest the ship");
            Assert.IsTrue(body.Broken,"a foot planted, the frame isn't open");Assert.That(body.BrokenUntil-Time.time,Is.InRange(4.4f,5.05f),"a planted foot stands open five seconds");
            Assert.AreEqual(150,Hit(body,hero),1,"open, a blow on the frame isn't whole and half as much again");
            StringAssert.Contains("CUT IT",boss.StatusText,"its bar doesn't say a foot is planted");
            Assert.IsTrue(spoken.Any(s=>s.Contains(BarkService.Line(hero.character,"bossopening.Hulljack")??"(none)")),"nobody says a foot has been planted: "+string.Join(", ",spoken));
            yield return Until(()=>boss.PlantedFoot<0,6,hero);
            Assert.AreEqual(-1,boss.PlantedFoot,"the planted foot never lifts");Assert.IsFalse(planted.Targetable,"a lifted foot can still be targeted");Assert.AreEqual(0,Kill(planted,hero),.01f,"a lifted foot can be cut");
        }
        [UnityTest] public IEnumerator ACutFootTakesAQuarterReelsTheFrameAndItLeapsToTheNextJoinAsItsTugLaunchesHaulers()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-90,-90));Put(hero,Sky(0,9));var joins=Joins();Tug(Sky(0,-120));
            var frame=Enemy("Hulljack",Sky(0,0));yield return Frames(3);var boss=frame.GetComponent<BossController>();var body=frame.Health;var feet=frame.GetComponent<BossParts>();
            float whole=body.maximum;Assert.AreEqual(whole*BossController.HulljackTune.FootIntegrity,feet.Parts[0].maximum,1,"a foot's integrity isn't the fight's to say");
            Assert.IsTrue(boss.Perform("Plant"),"fixture: the plant");yield return Until(()=>boss.PlantedFoot>=0,3,hero);Assert.GreaterOrEqual(boss.PlantedFoot,0,"fixture: a foot planted");
            log.Clear();Kill(feet.Parts[boss.PlantedFoot],hero);yield return null;
            Assert.IsTrue(Said("HULLJACK_FOOT_CUT 1"),"a foot was cut and nothing heard it");Assert.IsTrue(Set("hands.feet1"),"the first foot cut isn't kept as the brief's flag");
            Assert.AreEqual(3,feet.Standing);Assert.That(body.integrity,Is.InRange(whole*.74f,whole*.75f),"a foot should take a quarter of its bar, over the first line");
            Assert.IsTrue(body.Broken,"a foot cut, the frame isn't reeling open");Assert.GreaterOrEqual(body.BrokenUntil-Time.time,BossController.HulljackTune.Reels-.2f,"it reels for less than three and a half seconds");
            StringAssert.Contains("(1/4)",boss.FightGoal(""));
            // Awake: the reel, then it lets go and leaps to the next join, and the tug's haulers come in from the tug's side. The first quarter is no phase: the shift is at half.
            frame.Passive=false;Put(hero,Sky(-14,-14));hero.Health.InvulnerableUntil=GameTime.Now+60;
            yield return Until(()=>Said("BOSS_SUMMON Hulljack"),14,hero);Assert.IsTrue(Said("BOSS_SUMMON Hulljack"),"a foot cut and its tug launched nothing: "+string.Join(", ",log));
            Assert.IsFalse(Said("BOSS_SHIFT"),"the first foot shifted it: its one shift is at half");
            var haulers=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.definition!=null&&e.definition.id=="ClaimHauler").ToArray();
            Assert.AreEqual(BossController.HulljackTune.Haulers,haulers.Length,"its shift launched no claim haulers");
            foreach(var hauler in haulers)Assert.Less(hauler.transform.position.z,frame.transform.position.z-20,"a hauler didn't come in from the tug's side (south)");
            yield return Until(()=>Said("HULLJACK_LEAP"),6,hero);Assert.IsTrue(Said("HULLJACK_LEAP 2"),"after the shift it didn't leap to the second join: "+string.Join(", ",log));
            yield return Until(()=>!boss.Busy&&boss.AtJoin==2,8,hero);
            Assert.AreEqual(2,boss.AtJoin,"it didn't come down on the second join");Assert.Less(Flat(frame.transform.position,joins[1].transform.position),1,"it came down away from the second join");
            Assert.IsTrue(boss.Clamped,"on its new join it isn't clamped");
            // The second foot carries it over half: its one shift, which promises the same leap, to the third join.
            // A boss acts only with the lead within 30 m of it: the ship follows the frame to its new join.
            frame.Passive=true;yield return Until(()=>!boss.Busy&&!body.Broken,6,hero);Put(hero,joins[1].transform.position+new Vector3(10,0,-12));hero.Health.InvulnerableUntil=GameTime.Now+60;log.Clear();
            Assert.IsTrue(boss.Perform("Plant"),"fixture: the second plant");yield return Until(()=>boss.PlantedFoot>=0,3,hero);Assert.GreaterOrEqual(boss.PlantedFoot,0,"fixture: a second foot planted");
            // Awake as the foot goes: a boss held for a scene that crosses a line doesn't shift.
            frame.Passive=false;Kill(feet.Parts[boss.PlantedFoot],hero);yield return null;
            Assert.That(body.integrity,Is.InRange(whole*.49f,whole*.5f),"the second foot should carry it over half");Assert.IsTrue(Set("hands.feet2"));
            yield return Until(()=>Said("HULLJACK_LEAP"),14,hero);
            Assert.IsTrue(Said("BOSS_SHIFT Hulljack 2"),"over half it never shifted");Assert.IsTrue(Said("HULLJACK_LEAP 3"),"after its shift it didn't leap to the third join: "+string.Join(", ",log));
            Assert.AreEqual(1,log.Count(l=>l.StartsWith("HULLJACK_LEAP")),"it leapt twice for one foot");
        }
        [UnityTest] public IEnumerator BatteredOverALineWithItsFootStillOnTheGripGivesAndTheMostCutFootTearsOff()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-90,-90));Put(hero,Sky(0,9));Joins();
            var frame=Enemy("Hulljack",Sky(0,0));yield return Frames(3);var boss=frame.GetComponent<BossController>();var body=frame.Health;var feet=frame.GetComponent<BossParts>();
            Assert.IsTrue(boss.Perform("Plant"),"fixture: the plant");yield return Until(()=>boss.PlantedFoot>=0,3,hero);int cut=boss.PlantedFoot;
            Hit(feet.Parts[cut],hero,feet.Parts[cut].maximum*.2f);Assert.Less(feet.Parts[cut].integrity,feet.Parts[cut].maximum,"fixture: the planted foot is cut into");
            yield return Until(()=>!body.Broken&&!boss.Busy,8,hero);
            // Pounded over the first quarter's line on the frame itself, through its clamp.
            log.Clear();body.integrity=body.maximum*.755f;Hit(body,hero,body.maximum*.02f/BossController.HulljackTune.Clamp);
            Assert.Less(body.integrity,body.maximum*.75f,"fixture: battered over the line");
            yield return Frames(2);
            Assert.AreEqual(3,feet.Standing,"over its first quarter it still stands on four feet: the bar and the feet disagree");
            Assert.IsFalse(feet.Parts[cut].Alive,"the grip gave, and not at the foot that had been cut into");Assert.IsTrue(Said("HULLJACK_FOOT_TORN 1"));Assert.IsTrue(Set("hands.feet1"),"a torn foot isn't counted");
            frame.Passive=false;hero.Health.InvulnerableUntil=GameTime.Now+60;
            yield return Until(()=>Said("HULLJACK_LEAP"),10,hero);Assert.IsTrue(Said("HULLJACK_LEAP 2"),"its grip gave and it didn't let go for the next join: "+string.Join(", ",log));
            Assert.IsTrue(Said("BOSS_SUMMON Hulljack"),"a torn foot doesn't call the tug as a cut one does");
        }
        [UnityTest] public IEnumerator ItsTorchAndScrapStopAtABoomAndAShipInAGapBetweenTheLanesIsUntouched()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(hero,Sky(4,9));Put(partner,Sky(-4,11));Joins();
            Boom(Sky(-2.6f,6.5f),new Vector3(3,0,1.4f));
            var frame=Enemy("Hulljack",Sky(0,0));yield return Frames(3);var boss=frame.GetComponent<BossController>();
            Assert.IsTrue(boss.Perform("TorchSweep"),"fixture: the torch");yield return null;
            var fan=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Cone);Assert.IsNotNull(fan,"the torch marks no fan");
            Assert.IsTrue(fan.Contains(hero.transform.position)&&fan.Contains(partner.transform.position),"fixture: both ships are in the fan");
            yield return Until(()=>!boss.Busy,4,hero,partner);
            Assert.Less(hero.Health.integrity,5000,"the torch missed a ship in its fan in the open");Assert.AreEqual(5000,partner.Health.integrity,.01f,"the torch went through a cargo boom");
            // The scrap: five lanes with gaps between; one ship on the middle lane, one in the gap beside it, one behind a boom on another lane.
            Stand(frame,Sky(0,0));Put(hero,Sky(0,12));Put(partner,Sky(18*Mathf.Sin(10*Mathf.Deg2Rad),18*Mathf.Cos(10*Mathf.Deg2Rad)));yield return Wait(.3f,hero,partner);
            Assert.IsTrue(boss.Perform("DebrisFan"),"fixture: the scrap");yield return null;
            var lanes=Marks(boss).Where(t=>t.Shape==Telegraph.Kind.Line).ToArray();Assert.AreEqual(BossController.HulljackTune.DebrisLanes,lanes.Length,"the scrap isn't flung down five marked lanes");
            Assert.IsTrue(lanes.Any(l=>l.Contains(hero.transform.position)),"fixture: the lead on a lane");Assert.IsFalse(lanes.Any(l=>l.Contains(partner.transform.position)),"fixture: the partner in a gap");
            yield return Until(()=>!boss.Busy,4,hero,partner);
            Assert.Less(hero.Health.integrity,5000,"the scrap missed a ship on its lane");Assert.AreEqual(5000,partner.Health.integrity,.01f,"the scrap struck a ship in the gap between its lanes");
        }
        [UnityTest] public IEnumerator AGrappledShipIsReeledInAndRollsFreeBeforeTheCrushAndAHookOnABoomLeavesTheFrameOpen()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-90,-90));Put(hero,Sky(0,12));Joins();
            var frame=Enemy("Hulljack",Sky(0,0));yield return Frames(3);var boss=frame.GetComponent<BossController>();var body=frame.Health;
            Assert.IsTrue(boss.Perform("GrappleShot"),"fixture: the grapple");yield return null;
            var line=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Line);Assert.IsNotNull(line,"the grapple marks no line");Assert.IsTrue(line.Contains(hero.transform.position));
            yield return Until(()=>Said("HULLJACK_GRAPPLE"),3,hero);Assert.IsTrue(Said("HULLJACK_GRAPPLE caught"),"a ship in the grapple's line wasn't hooked: "+string.Join(", ",log));
            Assert.Less(hero.Health.integrity,5000,"the hook bit nothing");float bitten=hero.Health.integrity;float gap=Flat(hero.transform.position,frame.transform.position);
            yield return Frames(6);Assert.AreEqual(BossController.HulljackTune.ReelPace,hero.Health.Pace,.01f,"a hooked ship keeps its speed");
            Assert.Less(Flat(hero.transform.position,frame.transform.position),gap-.2f,"a hooked ship isn't reeled in");
            Assert.IsTrue(hero.Dodge(Vector3.right),"fixture: the roll");yield return Until(()=>!boss.Busy,3,hero);
            Assert.IsTrue(Said("HULLJACK_GRAPPLE rolled"),"a roll didn't free a hooked ship");Assert.AreEqual(bitten,hero.Health.integrity,.01f,"a ship that rolled free was crushed all the same");
            Assert.IsFalse(Telegraph.Live.Any(t=>t!=null&&t.owner==boss&&!t.Released),"after a roll the crush is still marked");
            // Behind a boom: the hook bites the boom, and the frame is thrown off its balance.
            yield return Wait(.5f,hero);Stand(frame,Sky(0,0));Put(hero,Sky(0,12));Boom(Sky(0,6),new Vector3(4,0,1.5f));yield return Wait(.3f,hero);log.Clear();
            Assert.IsTrue(boss.Perform("GrappleShot"),"fixture: the grapple again");
            yield return Until(()=>Said("HULLJACK_GRAPPLE"),3,hero);Assert.IsTrue(Said("HULLJACK_GRAPPLE missed"),"a ship behind a boom was hooked through it: "+string.Join(", ",log));
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"a ship behind a boom took the hook");
            yield return Until(()=>body.Broken,2,hero);Assert.IsTrue(body.Broken,"a hook on nothing leaves the frame as it was");
        }
        [UnityTest] public IEnumerator WithItsFourthFootCutItGoesDownThroughTheRoofDoorAndNothingSpeaksOverItsLastLines()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-90,-90));Put(hero,Sky(0,9));Joins();
            var door=new GameObject("Fixture roof door").AddComponent<BossExit>();door.boss="Hulljack";door.sinks=true;door.transform.position=Sky(32,-10);
            var frame=Enemy("Hulljack",Sky(0,0));yield return Frames(3);var boss=frame.GetComponent<BossController>();var body=frame.Health;var feet=frame.GetComponent<BossParts>();
            Assert.AreEqual(BossController.Ending.Leaves,BossController.EndingOf("Hulljack"),"the frame is beaten, not destroyed: it goes into the hall");
            for(int i=0;i<3;i++)feet.Settle(i);body.integrity=body.maximum*.2f;yield return Frames(2);Assert.AreEqual(1,feet.Standing,"fixture: one foot left");
            Assert.IsTrue(boss.Perform("Plant"),"fixture: the last plant");yield return Until(()=>boss.PlantedFoot>=0,3,hero);Assert.AreEqual(3,boss.PlantedFoot);
            var spoken=new List<(string who,string line,float at)>();System.Action<string,string> heard=(who,line)=>spoken.Add((who,line,Time.time));BarkService.Spoken+=heard;
            int xp=GameServices.Current.State.party[0].xp;var body0=frame.transform;float x=body0.position.x;
            try{Kill(feet.Parts[3],hero);yield return Wait(3,hero);}finally{BarkService.Spoken-=heard;}
            Assert.AreEqual(0,body.integrity,"its last foot cut, it still holds on");Assert.IsTrue(Set("bossdown.Hulljack"),"gone through the door, it isn't counted as beaten");
            Assert.IsTrue(Set("hands.feet4"),"the fourth foot isn't kept as the brief's flag");Assert.IsTrue(Said("BOSS_LEAVES Hulljack"),"at nothing it stayed where it was");
            Assert.Greater(GameServices.Current.State.party[0].xp,xp,"the cut that beat it paid nobody");
            Assert.IsFalse(spoken.Any(s=>s.who!="Dessik"&&s.line==BarkService.Line(s.who,"boss")),"the shared down bark says the fight is over: "+string.Join(", ",spoken.Select(s=>s.who+": "+s.line)));
            Assert.IsTrue(spoken.Any(s=>s.who==hero.character),"nobody says the last foot is off");Assert.IsTrue(spoken.Any(s=>s.who=="Dessik"),"Dessik says nothing as his frame goes");
            var party=spoken.First(s=>s.who==hero.character);var his=spoken.First(s=>s.who=="Dessik");
            Assert.GreaterOrEqual(his.at-party.at,1.5f,"Dessik's line comes over the party's before it can be read");
            Assert.IsTrue(frame==null||Flat(frame.transform.position,door.transform.position)<Flat(Sky(0,0),door.transform.position)-4,"it didn't make for the roof door");
        }

        // ------------------------------------------------------------------ Dessik Varn, in the freight hall
        [UnityTest] public IEnumerator WithNoFeetHeNeverWalksAndADodgedClawHaulsTheFrameToWhatItBit()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,9));
            var suit=Enemy("DessikVarn",At(0,.1f,0),false);yield return Frames(3);var boss=suit.GetComponent<BossController>();var body=suit.Health;
            var from=suit.transform.position;hero.Health.InvulnerableUntil=GameTime.Now+60;yield return Wait(3,hero);
            Assert.Less(Flat(from,suit.transform.position),.5f,"with its feet cut it walked toward a hero standing off");
            suit.Passive=true;yield return Until(()=>!boss.Busy&&!body.Broken,8,hero);Stand(suit,At(0,.1f,0));Put(hero,At(0,.05f,8));
            var stack=Block("Fixture stack",At(0,1.5f,15),new Vector3(4,3,2));yield return Frames(2);log.Clear();
            // Dodged: the claw flies on, bites the stack behind him, and the frame hauls itself there and arrives winded.
            Assert.IsTrue(boss.Perform("Claw"),"fixture: the claw");
            yield return Until(()=>boss.TelegraphRemaining>0&&boss.TelegraphRemaining<.1f,2,hero);Assert.IsTrue(hero.Dodge(Vector3.right),"fixture: the roll");
            yield return Until(()=>Said("DESSIK_CLAW"),2,hero);Assert.IsTrue(Said("DESSIK_CLAW hauled"),"a dodged claw didn't bite the stack behind him: "+string.Join(", ",log));
            yield return Until(()=>body.Broken,3,hero);
            Assert.Greater(suit.transform.position.z,At(0,0,7).z,"the frame didn't haul itself toward what its claw bit");Assert.Less(suit.transform.position.z,At(0,0,14).z-1,"the frame hauled itself into the stack");
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"a hero who rolled out of the claw's line was struck");
            Assert.IsTrue(body.Broken,"arrived winded, it isn't open");Assert.That(body.BrokenUntil-Time.time,Is.InRange(1,BossController.DessikTune.Winded+.05f),"winded, it is open for 1.6 s");
            // Caught: hauled in to its front and slammed.
            yield return Until(()=>!boss.Busy&&!body.Broken,5,hero);Object.Destroy(stack);Stand(suit,At(0,.1f,0));Put(hero,At(0,.05f,8));yield return Frames(2);log.Clear();
            Assert.IsTrue(boss.Perform("Claw"),"fixture: the claw again");yield return Until(()=>!boss.Busy,4,hero);
            Assert.IsTrue(Said("DESSIK_CLAW caught"),"a hero standing in the claw's line wasn't caught: "+string.Join(", ",log));
            Assert.Less(Flat(hero.transform.position,suit.transform.position),BossController.CapsuleRadius("DessikVarn")+BossController.DessikTune.ClawStops+BossController.DessikTune.SlamRadius,"a caught hero wasn't hauled in");
            Assert.Less(hero.Health.integrity,5000-30,"a caught hero was hauled in and not slammed");
            // Guarded: the claw clamps on the guard and holds nothing; the frame is yanked open.
            yield return Until(()=>!boss.Busy&&!body.Broken,5,hero);Stand(suit,At(0,.1f,0));Put(hero,At(0,.05f,8));yield return Frames(2);log.Clear();
            Assert.IsTrue(boss.Perform("Claw"),"fixture: the claw a third time");
            for(float end=Time.time+2;!Said("DESSIK_CLAW")&&Time.time<end;){hero.Guard(true);hero.motor.Move(Vector2.zero,false,false);yield return null;}
            hero.Guard(false);
            Assert.IsTrue(Said("DESSIK_CLAW guarded"),"a guarded claw wasn't clamped on the guard: "+string.Join(", ",log));
            Assert.Greater(Flat(hero.transform.position,suit.transform.position),6,"a guarding hero was hauled in");
            yield return Until(()=>body.Broken,1,hero);Assert.IsTrue(body.Broken,"a claw clamped on a guard leaves the frame as it was");
        }
        [UnityTest] public IEnumerator HisMagnetPulseDragsEveryLooseCrateDownAMarkedLaneAndCargoHomeJoltsHimOpen()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,8));
            var crates=new[]{Crate(At(0,0,13)),Crate(At(-11,0,4)),Crate(At(9,0,-9))};var far=Crate(At(40,0,0));yield return Frames(2);
            var suit=Enemy("DessikVarn",At(0,.1f,0));yield return Frames(3);var boss=suit.GetComponent<BossController>();var body=suit.Health;
            Assert.IsTrue(boss.Perform("MagnetPulse"),"fixture: the pulse");yield return null;
            var marks=Marks(boss);var disc=marks.FirstOrDefault(t=>t.Shape==Telegraph.Kind.Circle);var lanes=marks.Where(t=>t.Shape==Telegraph.Kind.Line).ToArray();
            Assert.IsNotNull(disc,"the pulse marks no disc round the frame");Assert.AreEqual(3,lanes.Length,"the pulse doesn't mark a lane for each crate in reach (and none for the one 40 m off)");
            foreach(var crate in crates)Assert.IsTrue(lanes.Any(l=>l.Contains(crate.Middle)),"a crate in reach has no lane marked from it");
            Assert.IsTrue(lanes.Any(l=>l.Contains(hero.transform.position)),"fixture: the hero stands in a crate's lane");Assert.IsFalse(disc.Contains(hero.transform.position),"fixture: outside the disc");
            Assert.IsTrue(lanes.All(l=>!Faint(l)),"a crate's lane is painted as no danger");
            // Its opening is the end of the move itself: looked at the moment the crates are home.
            yield return Until(()=>Said("DESSIK_JOLTED")||Said("DESSIK_PULSE")&&!boss.Busy,6,hero);
            Assert.IsTrue(Said("DESSIK_PULSE 3"),"the pulse didn't take the three crates in reach");Assert.Less(hero.Health.integrity,5000,"a crate came down its lane through a hero and touched nothing");
            float girth=BossController.CapsuleRadius("DessikVarn");
            foreach(var crate in crates)
            {
                Assert.Less(Flat(crate.transform.position,suit.transform.position),girth+crate.Half+1,"a crate wasn't dragged home to the frame");
                Assert.IsFalse(crate.Moving,"a crate is still on the move");Assert.IsTrue(crate.GetComponent<BoxCollider>().enabled,"a crate set down isn't solid: it's no cover");
            }
            Assert.Less(Flat(far.transform.position,At(40,0,0)),.01f,"a crate out of reach was dragged");
            Assert.IsTrue(Said("DESSIK_JOLTED 3"),"its own cargo slammed home and it wasn't jolted");Assert.IsTrue(body.Broken,"jolted, it isn't open");
            Assert.That(body.BrokenUntil-Time.time,Is.InRange(.9f,BossController.DessikTune.Jolted+.05f),"jolted, it is open for 1.5 s");
        }
        [UnityTest] public IEnumerator HisTorchLeavesBurningLinesThatAreADangerBurnWhoStandsInThemAndStopAtCover()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,5));
            var suit=Enemy("DessikVarn",At(0,.1f,0));yield return Frames(3);var boss=suit.GetComponent<BossController>();float girth=BossController.CapsuleRadius("DessikVarn");
            // A crate on the left line, four metres along it.
            var left=Quaternion.Euler(0,-BossController.DessikTune.BurnSpread,0)*Vector3.forward;var crate=Crate(At(0,0,0)+left*(girth+4));yield return Frames(2);
            Assert.IsTrue(boss.Perform("TorchLines"),"fixture: the torch");yield return null;
            var fan=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Cone);Assert.IsNotNull(fan,"the torch marks no fan");Assert.IsFalse(Faint(fan),"the torch's fan is painted as no danger");
            yield return Until(()=>!boss.Busy,4,hero);
            Assert.Less(hero.Health.integrity,5000,"the torch missed a hero in its fan");
            var burns=LingeringArea.All.Where(a=>a!=null&&a.Effect==AreaEffect.Burn).ToArray();Assert.AreEqual(BossController.DessikTune.BurnLines,burns.Length,"the torch left no burning lines");
            foreach(var burn in burns){var ring=Telegraph.Live.FirstOrDefault(t=>t!=null&&ReferenceEquals(t.owner,burn));Assert.IsNotNull(ring);Assert.AreEqual(Telegraph.Kind.Line,ring.Shape,"a burn is laid as a circle, not a line");Assert.IsFalse(Faint(ring),"a burning line is painted as no danger");}
            var straight=At(0,0,0)+Vector3.forward*(girth+6.5f);Assert.IsTrue(burns.Any(b=>b.Contains(straight)),"the middle line doesn't run its length");
            Assert.IsFalse(burns.Any(b=>b.Contains(At(0,0,0)+left*(girth+6))),"the left line burns on past the crate standing in it");
            // On a line, it burns; between two, nothing.
            var on=At(0,.05f,0)+Vector3.forward*(girth+5);var between=At(0,.05f,0)+Quaternion.Euler(0,BossController.DessikTune.BurnSpread*.5f,0)*Vector3.forward*(girth+5);
            Put(hero,on);Put(partner,between);yield return Wait(1.2f,hero,partner);
            Assert.Less(hero.Health.integrity,5000,"a hero standing on a burning line wasn't burned");Assert.AreEqual(5000,partner.Health.integrity,.01f,"a hero between the lines was burned");
            yield return Until(()=>LingeringArea.All.All(a=>a==null),BossController.DessikTune.BurnSeconds+1,hero,partner);Assert.IsTrue(LingeringArea.All.All(a=>a==null),"the lines never go out");
        }
        [UnityTest] public IEnumerator FromHalfHeThrowsCargoOntoMarkedCirclesAndEachCrateLandsAsCover()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(hero,At(0,.05f,9));Put(partner,At(7,.05f,7));
            var near=Crate(At(0,0,-4));yield return Frames(2);
            var suit=Enemy("DessikVarn",At(0,.1f,0));yield return Frames(3);var boss=suit.GetComponent<BossController>();
            Assert.AreEqual("CargoThrow",BossController.PromisedMove("DessikVarn",2),"his shift at half doesn't bring the cargo");
            Assert.IsTrue(boss.Perform("CargoThrow"),"fixture: the throw");yield return null;
            var circles=Marks(boss).Where(t=>t.Shape==Telegraph.Kind.Circle).ToArray();Assert.AreEqual(2,circles.Length,"a crate for each hero in reach isn't marked where it lands");
            Assert.IsTrue(circles.Any(c=>c.Contains(hero.transform.position))&&circles.Any(c=>c.Contains(partner.transform.position)),"the landings aren't marked where the heroes stand");
            var landed=circles.Select(c=>c.Origin).ToArray();
            yield return Until(()=>!boss.Busy,4,hero,partner);
            Assert.IsTrue(Said("DESSIK_THROW 2"));Assert.Less(hero.Health.integrity,5000,"a crate came down on a hero and did nothing");
            var all=LooseCargo.All.Where(c=>c!=null).ToArray();Assert.AreEqual(2,all.Length,"fixture: the crate by the frame, and one torn off its rack for the second throw");
            foreach(var at in landed)
            {
                var crate=all.OrderBy(c=>Flat(c.transform.position,at)).First();
                Assert.Less(Flat(crate.transform.position,at),.5f,"a crate didn't land on its circle");Assert.IsFalse(crate.Moving);Assert.IsTrue(crate.GetComponent<BoxCollider>().enabled,"a landed crate isn't solid");
                foreach(var member in new[]{hero,partner})Assert.Greater(Flat(member.transform.position,crate.Middle),crate.Half+.3f,"a hero was left standing inside a landed crate");
                var behind=crate.Middle+(crate.Middle-suit.transform.position).normalized*(crate.Half+1);behind.y=1;
                Assert.IsFalse(CombatCover.Clear(suit.transform.position+Vector3.up,behind),"a landed crate is no cover");
            }
            Assert.Less(Flat(near.transform.position,landed[0]),.5f,"it threw a crate other than the one beside it first");
        }
        [UnityTest] public IEnumerator BrokenTheSuitKneelsAndOnlyThenIsItsCockpitATarget()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,4));
            var suit=Enemy("DessikVarn",At(0,.1f,0));yield return Frames(3);var boss=suit.GetComponent<BossController>();var body=suit.Health;
            var cockpit=suit.transform.Find("Cockpit");Assert.IsNotNull(cockpit,"the suit has no cockpit to strike");
            Assert.IsFalse(cockpit.gameObject.activeInHierarchy,"standing, its cockpit is a target");
            Assert.IsFalse(suit.GetComponentsInChildren<Hurtbox>(false).Any(h=>h.multiplier>1),"standing, the suit has a weak spot to hit");
            var spoken=new List<string>();System.Action<string,string> heard=(who,line)=>spoken.Add(line);BarkService.Spoken+=heard;
            try{Hit(body,hero,10,null,body.breakThreshold*10);yield return Frames(2);}finally{BarkService.Spoken-=heard;}
            Assert.IsTrue(Said("BREAK_OK DessikVarn"),"fixture: the break");Assert.IsTrue(Said("DESSIK_KNEELS"),"broken, the suit didn't kneel");
            Assert.IsTrue(boss.Kneeling);StringAssert.Contains("COCKPIT",boss.StatusText,"its bar doesn't say to strike the cockpit");
            Assert.IsTrue(spoken.Contains(BarkService.Line(hero.character,"bossopening.DessikVarn")),"nobody says the suit is kneeling");
            Assert.IsTrue(cockpit.gameObject.activeInHierarchy,"kneeling, its cockpit isn't a target");
            float struck=cockpit.GetComponent<Hurtbox>().Hit(new DamagePacket{source=hero.Health,amount=100,type=Neutral(body)});
            Assert.AreEqual(100*BossController.DessikTune.CockpitTakes*1.5f,struck,1,"a blow on the cockpit of a kneeling suit isn't twice an opening's");
            // Kneeling, the cockpit is out past the front of its capsule at a blade's height, within a cut's reach of a hero standing against its body (QaPlanCombatTests' measure: a
            // blade reaches 1.3 m from a chest a metre up, the hero .35 m round). Standing, flush with the capsule, nothing would reach it.
            var shape=cockpit.GetComponent<SphereCollider>();var centre=shape.transform.TransformPoint(shape.center);float size=shape.radius*shape.transform.lossyScale.x,girth=suit.GetComponent<CharacterController>().radius;
            var off=centre-suit.transform.position;var flat=new Vector3(off.x,0,off.z);
            Assert.Greater(flat.magnitude+size,girth+.05f,"the kneeling cockpit is inside the suit's own capsule: a blade hits the body first");
            Assert.That(off.y,Is.InRange(.4f,2.4f),"the kneeling cockpit is out of a cut's height");
            var stand=suit.transform.position+flat.normalized*(girth+.35f)+Vector3.up;Assert.LessOrEqual(Vector3.Distance(stand,centre)-size,1.3f,"a hero at the edge of the suit can't reach the kneeling cockpit");
            yield return Until(()=>!body.Broken,6,hero);yield return Frames(2);
            Assert.IsFalse(boss.Kneeling);Assert.IsFalse(cockpit.gameObject.activeInHierarchy,"risen, its cockpit is still a target");Assert.AreEqual("",boss.StatusText);
            // The moment brought about by hand, for the shared tests' look at its weak spot, is the same kneel, and ends when told.
            Assert.IsTrue(BossController.WeakSpotGated("DessikVarn"),"the suit's weak spot isn't declared as coming and going");
            Assert.IsTrue(boss.ShowWeakSpot(true));Assert.IsTrue(boss.Kneeling);Assert.IsTrue(cockpit.gameObject.activeInHierarchy,"brought about by hand, the kneel has no cockpit");
            Assert.IsTrue(boss.ShowWeakSpot(false));Assert.IsFalse(boss.Kneeling);Assert.IsFalse(cockpit.gameObject.activeInHierarchy,"ended by hand, the kneel leaves its cockpit out");
            Assert.IsFalse(BossController.WeakSpotGated("Hulljack"),"the frame's spots are said to come and go");
        }
        [UnityTest] public IEnumerator AtHalfTheCabinAlarmSoundsTheHailsComeInTurnAndNothingSpeaksOverHisYield()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,12));
            var suit=Enemy("DessikVarn",At(0,.1f,0),false);yield return Frames(3);var boss=suit.GetComponent<BossController>();var body=suit.Health;
            hero.Health.InvulnerableUntil=GameTime.Now+120;Hit(body,hero,1);
            StringAssert.Contains("cockpit",boss.FightGoal(""),"before the alarm the objective isn't the fight's");
            var spoken=new List<(string who,string line,float at)>();System.Action<string,string> heard=(who,line)=>spoken.Add((who,line,Time.time));BarkService.Spoken+=heard;
            try
            {
                yield return Until(()=>!boss.Busy&&!body.Broken,8,hero);body.integrity=body.maximum*.48f;
                yield return Until(()=>Said("BREACH_ALARM"),12,hero);
                Assert.IsTrue(Said("BOSS_SHIFT DessikVarn 2"),"under half he never shifted");Assert.IsTrue(Said("BREACH_ALARM"),"the cabin alarm never sounded");
                Assert.IsTrue(boss.AlarmSounded);StringAssert.Contains("cabins",boss.FightGoal(""),"after the alarm the objective doesn't say the cabins can't be reached");
                yield return Until(()=>spoken.Any(s=>s.who=="Patrol"),BossController.DessikTune.LatchedAfter+2,hero);
            }
            finally{BarkService.Spoken-=heard;}
            var keeper=spoken.FirstOrDefault(s=>s.who=="Keeper");var party=spoken.FirstOrDefault(s=>s.who==hero.character&&s.line==BarkService.Line(hero.character,"breach"));var patrol=spoken.FirstOrDefault(s=>s.who=="Patrol");
            Assert.IsNotNull(keeper.line,"Keeper Abri never calls the breach");Assert.IsNotNull(party.line,"the party never hears the cabins");Assert.IsNotNull(patrol.line,"the Steady Hand never latches on");
            var shift=spoken.FirstOrDefault(s=>s.who==hero.character&&s.line==BarkService.Line(hero.character,"bossphase.DessikVarn"));
            if(shift.line!=null)Assert.GreaterOrEqual(keeper.at-shift.at,3.5f,"the alarm's first word comes over the shift's before it can be read");
            Assert.GreaterOrEqual(party.at-keeper.at,3.5f,"the party's line comes over Abri's before it can be read");Assert.GreaterOrEqual(patrol.at-party.at,3.5f,"the patrol's line comes over the party's before it can be read");
            Assert.IsTrue(cues.Any(c=>c.key=="ui_error"),"the alarm makes no sound");
            // He yields: no bark talks over what he says, and the alarm stops.
            spoken.Clear();BarkService.Spoken+=heard;
            try{Kill(body,hero);cues.Clear();yield return Wait(3,hero);}finally{BarkService.Spoken-=heard;}
            Assert.IsTrue(Set("bossdown.DessikVarn"),"beaten, he isn't counted as down");Assert.AreEqual(BossController.Ending.Yields,BossController.EndingOf("DessikVarn"),"a person yields");
            Assert.IsEmpty(spoken,"a bark talks over his yield: "+string.Join(", ",spoken.Select(s=>s.who+": "+s.line)));
            Assert.IsFalse(cues.Any(c=>c.key=="ui_error"),"the alarm goes on after the fight");Assert.IsFalse(boss.AlarmSounded);
        }

        // ------------------------------------------------------------------ what the fight is sized on
        /// <summary>The flight driver's share is the Cantor's, the ground driver's the plan's .3 (ContentRegistry). How long the frame waits between plants, and a leap's cost from cut to clamp.</summary>
        const float FlightShare=.29f,GroundShare=.3f,FlatOutSeconds=15,PlantGap=11,LeapCost=11;
        /// <summary>The party flat out on a body that never moves, as QaPlanCombatTests measures it: both heroes attacking, every skill as its cooldown and Charge allow. The body alone and open:
        /// its feet and its clamp are counted apart.</summary>
        IEnumerator Rate(string id,bool flight,System.Action<float> measured)
        {
            var at=flight?Sky(0,0):At(0,.1f,0);var party=PartyController.Current;if(party.Active.character!="Taren")Assert.IsTrue(party.Swap(),"fixture: the swap");
            var taren=party.Active;var sela=party.members.First(m=>m!=taren);
            var enemy=Enemy(id,at);yield return Frames(3);var body=enemy.Health;
            enemy.GetComponent<BossController>().enabled=false;body.Plate=null;var parts=enemy.GetComponent<BossParts>();if(parts!=null)parts.Expose(false);
            body.maximum=body.integrity=10000000;
            float reach=enemy.GetComponent<CharacterController>().radius+.9f;
            var tarenAt=flight?at+new Vector3(-2,0,-6):at+Vector3.back*reach;var selaAt=flight?at+new Vector3(2,0,-6):at+Vector3.back*8+Vector3.right*2;
            Put(taren,tarenAt);Put(sela,selaAt);taren.target=sela.target=body;taren.charge=sela.charge=100;yield return null;
            float landed=0;body.Damaged+=(_,packet,amount)=>{if(packet.source==taren.Health||packet.source==sela.Health)landed+=amount;};
            for(float end=Time.unscaledTime+FlatOutSeconds;Time.unscaledTime<end;)
            {
                foreach(var hero in new[]{taren,sela})
                {
                    hero.motor.Move(Vector2.zero,false,false);var home=hero==taren?tarenAt:selaAt;
                    if(hero.CanAct&&Flat(hero.transform.position,home)>.6f){var keep=hero.Health.integrity;Put(hero,home);hero.Health.integrity=keep;}
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
            Assert.Greater(landed,0,"fixture: the party is hitting it");measured(landed/FlatOutSeconds);
            enemy.Passive=true;Object.Destroy(enemy.gameObject);
            foreach(var left in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(left is GravityWell||left is PulseField||left is StarfallStrike||left is NetSeed)Object.Destroy(left.gameObject);
            yield return Frames(3);
        }
        /// <summary>The plan's length, on today's build, with what is assumed written down: two to five minutes for the whole fight, part one and part two, for a Sync 15 party in the chapter's
        /// arrival parts (DevLoadout chapter5). On the hull the frame's bar goes a quarter a foot, and a foot takes blows only while it is planted (the frame open, so half as much again):
        /// each foot is its integrity at the driver's share of flat out, in five-second openings that come every <see cref="PlantGap"/> seconds; the frame's clamped body is left out
        /// (at a share of .3 it moves the bar a little, so this is the long side), and each of the three leaps costs <see cref="LeapCost"/> seconds from cut to clamp. In the hall the bar is
        /// the body's at the driver's share, its openings left out. Each part is also held inside its own sheet's bar as the shared test holds it. The integrities are pinned to the builder.</summary>
        [UnityTest] public IEnumerator TheWholeFightLastsWhatThePlanAsksAtThePartysMeasuredRates()
        {
            string source=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Scripts/Editor/World/ManyHandsBuilder.Enemies.cs"));
            float frame=GameCatalog.Find<EnemyDef>("Hulljack").integrity,suit=GameCatalog.Find<EnemyDef>("DessikVarn").integrity;
            StringAssert.Contains($"e.integrity={frame:0};e.breakThreshold=900;",source,"a rebuild would put the frame's old integrity back");
            StringAssert.Contains($"e.integrity={suit:0};e.breakThreshold=700;",source,"a rebuild would put the suit's old integrity back");
            yield return Zone("Arena_Flight");float flying=0;yield return Rate("Hulljack",true,r=>flying=r);
            yield return Zone("Arena_Ground");Floor();float walking=0;yield return Rate("DessikVarn",false,r=>walking=r);
            float foot=frame*BossController.HulljackTune.FootIntegrity,open=BossController.HulljackTune.PlantOpen;
            float fire=foot/(flying*1.5f*FlightShare),openings=Mathf.Max(1,fire/open),eachFoot=openings*(PlantGap+open);
            float partOne=BossController.HulljackTune.Feet*eachFoot+(BossController.HulljackTune.Feet-1)*LeapCost,partTwo=suit/(walking*GroundShare),whole=partOne+partTwo;
            string line=$"MANYHANDS_RATE flight={flying:0}/s ground={walking:0}/s frame={frame:0} foot={foot:0} fire={fire:0.0}s openings={openings:0.0} eachFoot={eachFoot:0}s partOne={partOne:0}s suit={suit:0} partTwo={partTwo:0}s whole={whole:0}s";
            TestContext.WriteLine(line);Debug.Log(line);
            var failures=new List<string>();
            if(whole<120||whole>300)failures.Add($"the whole fight lasts {whole:0} s ({partOne:0} on the hull, {partTwo:0} in the hall): the plan asks 120 to 300");
            // Each part as the shared test holds it on its sheet: at least its floor at the driver's share, and no more than its ceiling at a quarter of flat out.
            foreach(var (id,integrity,rate,share) in new[]{("Hulljack",frame,flying,FlightShare),("DessikVarn",suit,walking,GroundShare)})
            {
                var sheet=ContentRegistry.Boss(id);
                if(integrity<rate*share*sheet.minSeconds)failures.Add($"{id}'s {integrity:0} at {rate:0} a second lasts {integrity/(rate*share):0} s at the driver's share: its sheet's floor is {sheet.minSeconds:0}");
                if(integrity/(rate*.25f)>sheet.maxSeconds)failures.Add($"{id}'s {integrity:0} at a quarter of {rate:0} a second lasts {integrity/(rate*.25f):0} s: its sheet's ceiling is {sheet.maxSeconds:0}");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
