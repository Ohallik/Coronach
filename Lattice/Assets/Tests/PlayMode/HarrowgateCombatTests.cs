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
    /// <summary>Chapter 8's ordinary fights and its elite, each on a bare floor or open sky in the proving yards with what it needs made here (nothing waits on the Picket of Teeth, the Scoria
    /// Flows or the Bridle Foundry being built). The party is the one the chapter is met by: chapter 8's arrival (DevLoadout chapter8: three heroes, Taren and Idra out, Tempered parts), with
    /// every pair a player can put out where the fight is about the party. The hit sizes on all three heroes; one test for each family the chapter added a mode to, saying what a player sees:
    /// the buoy's flare all round, the Lancer's lane and its slow turn, the Lancer losing a ship behind a rock, the flak curtain's rows on their clock, the pikeguards' pair and the hero between
    /// them, the mortar's burning ground, the crawler's lunge and its flight from hot ground, the pour drone's burning rail, the forge hound knocked out of the air. Then Vessa Korl: her sweep
    /// and the cover that takes it, her pikes called with her, her falling back at half to her way out and the floor she lays, her leaving at nought with her squad fighting on and the music
    /// given back, and her length for every pair. The chapter's expectations for the shared enemy and boss tests are here too (the bosses' are the second combat engineer's, in their class).</summary>
    public sealed class HarrowgateCombatTests
    {
        const float X=230;
        readonly List<string> log=new();
        static readonly string[] Heeded={"STRAFER_","CURTAIN_","ANCHOR_","PAIR_","GUARD_","LOBBER_","AMBUSH_","RIDER_","LEAPER_","VESSA_","BOSS_","COMBAT_KILL"};
        void Heard(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        /// <summary>Off unless this class's own run sets it (HARROWGATE_COMBAT_BROKEN=1): each test then builds its fixture wrong, so the run shows every test failing the way it should.</summary>
        static bool Broken=>System.Environment.GetEnvironmentVariable("HARROWGATE_COMBAT_BROKEN")=="1";
        object heat;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter8");log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;Time.captureDeltaTime=0;
            if(heat!=null){GroundHeat.Unregister((Object)heat);Object.Destroy((Object)heat);heat=null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void HarrowgateEnemies(ContentRegistry.Book book)
        {
            // Where a hero stands for each ordinary enemy's tell: inside the reach its attack starts from (the pair's lone one before its bash, inside the crawler's ring, on the drone's own
            // rail ahead of it, inside the hound's spring), and flying inside the buoy's flare, the Lancer's run-in and the curtain's first row, which lies right before the gate.
            book.Tell("MusterPikeguard",2);book.Tell("MusterMortar",7);book.Tell("AshCrawler",2.4f);book.Tell("PourDrone",4);book.Tell("ForgeHound",3);
            book.Tell("ReachScav",8);book.Tell("ReachSergeant",2);
            book.Tell("SensorBuoy",4,flight:true);book.Tell("MusterLancer",8,flight:true);book.Tell("FlakGate",4,flight:true);book.Tell("ReachSkiff",8,flight:true);
            // Vessa Korl, the casting gallery's elite: met at Sync 22 to 23 (HarrowgateBuilder.Enemies' ledger), measured at 22. Her blows, the same in both halves (her second half adds
            // nothing: VessaTune.HotBonus), as shares of a bar measured through Health.Receive (EveryOrdinaryHitTakes... below; the table is VessaTune's):
            //                  Taren/Sela 22  Idra 22  Taren/Sela 23  Idra 23
            //   her jab   132      14.7%       12.8%      14.1%         12.2%
            //   Pour      134      15.0%       13.0%      14.3%         12.4%   (second half)
            //   Beam      136      15.2%       13.2%      14.5%         12.6%
            //   Sweep     136      15.2%       13.2%      14.5%         12.6%
            //   Pike Call 140      15.6%       13.6%      14.9%         12.9%   (first half)
            //   Dead Wt.  142      15.8%       13.8%      15.1%         13.1%   (second half)
            //   the road  116      12.9%       11.3%      12.4%         10.7%   (a Pikeguard's thrust, the heaviest before her) Her door is the gallery's (docs/maps/BridleFoundry.md,
            // "Casting gallery contract"): the VessaGallery crossing sets vask.vessaMet. An elite says her own bounds: a minute to a minute and a half for the replay party, and the floor at
            // the driver's share under that (50 s); under two and a half minutes for someone who spends most of it dodging (the ceiling, at a quarter: 140 s, the shared measure of her
            // being the lowest, with its second hero standing off).
            book.Boss(new ContentRegistry.BossSheet{id="VessaKorl",zone="BridleFoundry",door="VessaKorl",doorFlags=new[]{"vask.vessaMet"},sync=22,driverShare=.3f,minSeconds=50,maxSeconds=140,
                // Lines, a fan, a thrust, strips across the way to her: a hero a few metres before her is in each. Falling back, she goes the other way and pours across the way she came.
                specials=new[]{("ThawBeam",new Vector3(0,0,6)),("HeatSweep",new Vector3(0,0,5)),("PikeCall",new Vector3(0,0,6)),("FallBack",new Vector3(0,0,6)),("PourOrder",new Vector3(0,0,8)),
                    ("DeadWeight",new Vector3(0,0,4))},
                // The sweep vents her lance; the dead weight wedges it in the floor.
                windows=new[]{("HeatSweep",new Vector3(0,0,5)),("DeadWeight",new Vector3(0,0,4))}});
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
        static GameObject Block(Vector3 centre,Vector3 size)
        {var wall=new GameObject("Harrowgate fixture block",typeof(BoxCollider));wall.transform.position=centre;wall.GetComponent<BoxCollider>().size=size;Physics.SyncTransforms();return wall;}
        static (CombatActor lead,CombatActor partner) Party(){var lead=PartyController.Current.Active;return (lead,PartyController.Current.members.First(m=>m!=lead));}
        static void Put(CombatActor actor,Vector3 position,bool fresh=true)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            if(fresh){actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;}Physics.SyncTransforms();
        }
        static void Stand(EnemyBrain enemy,Vector3 position,float yaw=0)
        {var body=enemy.GetComponent<CharacterController>();bool was=body.enabled;body.enabled=false;enemy.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));body.enabled=was;Physics.SyncTransforms();}
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
        static Telegraph[] Marks(Object owner)=>Telegraph.Live.Where(t=>t!=null&&t.owner==owner&&!t.Released).ToArray();
        static ForceLine Rail(Vector3 from,Vector3 to)
        {var go=new GameObject("Fixture rail");go.SetActive(false);go.transform.position=from;var line=go.AddComponent<ForceLine>();line.end=to;go.SetActive(true);return line;}

        // ------------------------------------------------------------------ the chapter's loadout
        [UnityTest] public IEnumerator TheChapterEightLoadoutArrivesAsThreeAtSyncTwentyTwoInTemperedPartsWithTarenAndIdraOut()
        {
            // Built wrong on purpose: chapter 7's arrival is the three at 19.
            if(Broken)DevLoadout.Apply("chapter7");
            yield return Zone("Arena_Ground");
            CollectionAssert.AreEquivalent(new[]{"Taren","Sela","Idra"},State.party.Select(m=>m.id).ToArray(),"chapter 8 arrives without its three heroes");
            foreach(var member in State.party)Assert.AreEqual(22,member.level,member.id+" arrives at the wrong Sync: the plan ends chapter 7 at 22");
            CollectionAssert.AreEquivalent(new[]{"Taren","Idra"},PartyController.Current.members.Select(m=>m.character).ToArray(),"the pair out isn't the one the join leaves out");
            foreach(var member in State.party)
            {
                string worn=member.equipped.TryGetValue(GearSlot.Frame,out var id)?State.parts.FirstOrDefault(p=>p.instanceId==id)?.definitionId:null;
                Assert.AreEqual("FramesT4",worn,member.id+" doesn't arrive in the Tempered frame the chapter's numbers are sized on");
            }
            Assert.IsTrue(Set("chapter7.complete"),"chapter 7 isn't told");Assert.IsFalse(Set("chapter8.complete"),"chapter 8 arrives already told");
        }

        // ------------------------------------------------------------------ the numbers the chapter is tuned by
        static readonly string[] Ordinary={"SensorBuoy","MusterLancer","FlakGate","ReachSkiff","MusterPikeguard","MusterMortar","AshCrawler","PourDrone","ForgeHound","ReachScav","ReachSergeant"};
        /// <summary>The standing rule (CHAPTER_GUIDE section 0.1): an ordinary hit costs a hero 9 to 13% of the bar, an elite's or a boss's move 12 to 16 and more than anything on the road to
        /// it. Measured through Health.Receive as a real blow lands, Plating and all, on all three heroes (Idra, the guard hero, is built with more Plating, so she is the floor and Taren and Sela
        /// the ceiling): every ordinary attack of the chapter at Sync 22 and 25 in the parts it arrives in (Tempered), and Vessa's at 22 and 23, where she is met, in both her halves (her
        /// second half's weight is her own, VessaTune.HotBonus, and stays inside the band). Every chapter 8 zone is at threat 1, every capsule is at least twice its radius tall, no guardian's arc reaches 180 degrees, and chapter 2's three Muster, tuned for
        /// Sync 9, have chapter 8's own in their stead.</summary>
        [UnityTest] public IEnumerator EveryOrdinaryHitTakesANinthToAnEighthOfAnyHerosBarAndVessasAnElitesShare()
        {
            yield return Zone("Arena_Ground");Floor();
            var source=Enemy("ReachScav",At(40,.05f,40)).Health;yield return Frames(2);
            var attacks=Ordinary.SelectMany(id=>Def(id).attacks.Select(a=>(id,what:a.name,damage:a.damage))).ToList();
            // Built wrong on purpose: a hit authored at 118 is over the band at 22, and the check has to say so.
            if(Broken)attacks.Add(("fixture","a hit authored at 118",118));
            var vessa=new (string what,float damage)[]{("her jab",Def("VessaKorl").attacks[0].damage),("Thaw Beam",BossController.VessaTune.BeamDamage),("Heat Sweep",BossController.VessaTune.SweepDamage),
                ("Pike Call",BossController.VessaTune.PikeDamage),("Pour Order",BossController.VessaTune.PourDamage),("Dead Weight",BossController.VessaTune.WeightDamage)};
            var failures=new List<string>();
            foreach(var pair in new[]{new[]{"Taren","Idra"},new[]{"Sela","Idra"}})
            {
                yield return Deploy(pair);
                foreach(int sync in new[]{22,23,25})
                {
                    yield return Level(sync);
                    foreach(var hero in PartyController.Current.members)
                    {
                        if(pair[0]=="Sela"&&hero.character=="Idra")continue;
                        float bar=hero.Health.maximum;
                        float Share(float damage){hero.Health.integrity=bar;hero.Health.InvulnerableUntil=0;return hero.Health.Receive(new DamagePacket{source=source,amount=damage,type=DamageType.Kinetic,weight=HitWeight.Medium})/bar;}
                        float road=0;string heaviest="";
                        foreach(var (id,what,damage) in attacks)
                        {
                            float share=Share(damage);if(share>road&&id!="fixture"){road=share;heaviest=what;}
                            string line=$"HARROWGATE_HIT {hero.character} sync={sync} plating={hero.plating:0} {id} {what} {damage:0} share={share:P1}";
                            if(sync==22||sync==25){TestContext.WriteLine(line);if(share<.09f||share>.13f)failures.Add(line+": the plan asks 9 to 13%");}
                        }
                        if(sync==22||sync==23)foreach(var (what,damage) in vessa)
                        {
                            float share=Share(damage),hot=Share(damage*(1+BossController.VessaTune.HotBonus));
                            string line=$"HARROWGATE_VESSA_HIT {hero.character} sync={sync} {what} {damage:0} share={share:P1} hot={hot:P1} road={heaviest} {road:P1}";TestContext.WriteLine(line);
                            if(share<.12f||share>.16f)failures.Add(line+": an elite's move is 12 to 16% of the bar");
                            if(hot<.12f||hot>.16f)failures.Add(line+": in her second half too, an elite's move is 12 to 16% of the bar: a phase's weight is inside the band, not on top of it");
                            if(share<=road)failures.Add(line+": no more than the road to her");
                        }
                    }
                }
            }
            foreach(string zone in new[]{"PicketOfTeeth","ScoriaFlows","BridleFoundry","Harrowgate"})
            {var z=GameCatalog.Find<ZoneDef>(zone);if(z!=null&&!Mathf.Approximately(z.enemyThreat,1))failures.Add($"{zone} is at threat {z.enemyThreat}: chapter 8's damage is authored at its real weight, at 1");}
            foreach(string id in Ordinary.Append("VessaKorl"))
            {
                var def=Def(id);float round=def.boss?BossController.CapsuleRadius(id):def.capsuleRadius>0?def.capsuleRadius:.55f;float tall=def.capsuleHeight>0?def.capsuleHeight:def.boss?3:1.5f;
                if(tall<round*2)failures.Add($"{id}'s capsule is {tall} m tall on a {round} m radius: Unity stretches it and the body floats");
                if(def.behaviour==EnemyBehaviourKind.Guardian&&def.tune.arc>=180)failures.Add($"{id}'s guarded arc is {def.tune.arc}: a flank blow must land whole");
            }
            // The returning three fight as chapter 2's did, in chapter 2's bodies, at this chapter's weight.
            foreach(var (reach,first) in new[]{("ReachScav","MusterScav"),("ReachSergeant","MusterSergeant"),("ReachSkiff","MusterSkiff")})
            {
                var now=Def(reach);var then=Def(first);
                if(now.archetype!=then.archetype||now.behaviour!=then.behaviour||now.weakness!=then.weakness||now.resistance!=then.resistance)failures.Add($"{reach} doesn't fight as chapter 2's {first} did");
                if(now.displayName!=DisplayNames.Target(first))failures.Add($"{reach} is called \"{now.displayName}\", where chapter 2's is \"{DisplayNames.Target(first)}\"");
                if(now.attacks[0].damage<then.attacks[0].damage*4)failures.Add($"{reach} hits for {now.attacks[0].damage}: no heavier than chapter 2's {first}");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the Picket's bodies
        [UnityTest] public IEnumerator TheLancerIsDrawnLongAlongItsRunWithItsLanceLeading()
        {
            // The intake turns it -90 degrees so its lance leads: drawn, it is longer nose to tail than across, and its weight (the hull and engines) sits behind the middle of its length, the
            // thin lance ahead. Built wrong on purpose: turned about.
            var drawn=Object.Instantiate(Def("MusterLancer").prefab);drawn.transform.rotation=Quaternion.Euler(0,Broken?180:0,0);yield return null;
            var renderers=drawn.GetComponentsInChildren<Renderer>();Assert.IsNotEmpty(renderers,"fixture: the Lancer has a body");
            var box=renderers[0].bounds;foreach(var part in renderers)box.Encapsulate(part.bounds);
            var weight=Vector3.zero;int count=0;
            foreach(var filter in drawn.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null)foreach(var v in filter.sharedMesh.vertices){weight+=filter.transform.TransformPoint(v);count++;}
            foreach(var skin in drawn.GetComponentsInChildren<SkinnedMeshRenderer>())if(skin.sharedMesh!=null)foreach(var v in skin.sharedMesh.vertices){weight+=skin.transform.TransformPoint(v);count++;}
            Object.Destroy(drawn);Assert.Greater(count,0,"fixture: the Lancer's mesh has vertices");weight/=count;
            TestContext.WriteLine($"LANCER_DRAWN x={box.size.x:0.00} z={box.size.z:0.00} centre={box.center.z:0.00} weight={weight.z:0.00}");
            Assert.Greater(box.size.z,box.size.x,"the Lancer is drawn across its run, not along it");
            Assert.Less(weight.z,box.center.z,"the Lancer's weight is ahead of its middle: it flies tail first, its lance trailing");
        }
        [UnityTest] public IEnumerator ASensorBuoyFlaresAllRoundAtAShipThatComesCloseAndNotAtOneThatKeepsOff()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();
            // Built wrong on purpose: chapter 3's Snagvine lashes a fan at the one it aims at, not all round it.
            var buoy=Enemy(Broken?"SnagvineNode":"SensorBuoy",Sky(0,0));yield return Frames(3);Assert.IsInstanceOf<EnemyAnchored>(buoy.Behaviour,"a Sensor Buoy isn't an anchored body");
            Put(hero,Sky(0,12));Put(partner,Sky(-40,-40));buoy.Passive=false;
            yield return Wait(2.5f,hero,partner);Assert.IsFalse(buoy.Telegraphing,"it flared at a ship 12 m off: its ring is 8");Assert.AreEqual(5000,hero.Health.integrity,.01f);
            Vector3 from=buoy.transform.position;
            Put(hero,Sky(0,5),false);Put(partner,Sky(-1,-5),false);
            yield return Until(()=>buoy.Telegraphing,3,hero,partner);Assert.IsTrue(buoy.Telegraphing,"it never flared at the ship that came within its ring");
            var ring=buoy.AttackMark;Assert.AreEqual(Telegraph.Kind.Circle,ring.Shape,"its flare isn't marked all round it");
            Assert.IsTrue(ring.Contains(hero.transform.position)&&ring.Contains(partner.transform.position),"its flare doesn't reach both ships close to it, ahead and behind");
            yield return Until(()=>!buoy.Telegraphing,2,hero,partner);yield return Frames(2);
            Assert.Less(hero.Health.integrity,5000,"its flare missed the ship it was aimed at");Assert.Less(partner.Health.integrity,5000,"its flare missed the ship behind it");
            Assert.Less(Flat(buoy.transform.position,from),.05f,"an anchored buoy moved");
        }
        [UnityTest] public IEnumerator ALancerRunsItsMarkedLaneThenComesAboutSlowAndOpen()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-40,-40));Put(hero,Sky(0,8));
            // Built wrong on purpose: chapter 3's Podskimmer breaks away fast, and is no opening.
            var lancer=Enemy(Broken?"Podskimmer":"MusterLancer",Sky(0,0));yield return Frames(3);var strafer=lancer.Behaviour as EnemyStrafer;Assert.IsNotNull(strafer,"a Lancer isn't a strafer");
            lancer.Passive=false;yield return Until(()=>lancer.Telegraphing,6,hero);Assert.IsTrue(lancer.Telegraphing,"it never lined up on a ship 8 m off");
            var lane=lancer.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,lane.Shape,"its run isn't marked as a lane");Assert.IsTrue(lane.Contains(hero.transform.position),"its lane isn't laid through the ship");
            Assert.GreaterOrEqual(lane.Length,25,"its lance run is no longer than a skiff's pass");Assert.GreaterOrEqual(lancer.TellDuration,.99f,"its lining up is too quick to read");
            yield return Until(()=>Said("STRAFER_COMES_ABOUT"),4,hero);
            Assert.IsTrue(Said("STRAFER_COMES_ABOUT MusterLancer"),"at the end of its run it didn't come about");Assert.Less(hero.Health.integrity,5000,"its run missed the ship it was laid through");
            Assert.IsTrue(strafer.ComingAbout&&lancer.Health.Broken,"coming about, it isn't open");Assert.That(lancer.Health.BrokenUntil-Time.time,Is.InRange(1.3f,1.85f),"open through its 1.8 s turn");
            Assert.AreEqual(150,Hit(lancer.Health,hero),1,"a blow on it as it comes about isn't an opening's half as much again");
            var at=lancer.transform.position;yield return Wait(.6f,hero);
            Assert.Less(Flat(lancer.transform.position,at),Def("MusterLancer").tune.speed*.6f*.5f,"it comes about at its run's speed: a slow turn should be slow");
        }
        [UnityTest] public IEnumerator ALancerLosesAShipARockHidesAndCirclesWhereItIs()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-60,-60));Put(hero,Sky(0,22));
            // A rock's shadow between them (the Picket's SensorShade). Built wrong on purpose: no rock.
            var rock=Broken?null:new GameObject("Fixture rock");if(rock!=null){rock.transform.position=Sky(0,11);rock.AddComponent<SensorShade>().radius=5;}
            var lancer=Enemy("MusterLancer",Sky(0,0));yield return Frames(3);var strafer=(EnemyStrafer)lancer.Behaviour;
            lancer.Passive=false;
            // Held where it is while it looks: unseen for its two seconds, it gives him up.
            for(float end=Time.time+3.5f;Time.time<end&&!Said("STRAFER_LOST");){Stand(lancer,Sky(0,0));hero.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.IsTrue(Said("STRAFER_LOST MusterLancer"),"behind a rock for 3.5 s, the ship it hunted wasn't lost");Assert.IsTrue(strafer.LostItsQuarry);
            yield return Wait(2,hero);
            Assert.Less(Flat(lancer.transform.position,Sky(0,0)),EnemyStrafer.LostCircle+3,"having lost him, it went after him instead of circling where it lost him");
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"it struck a ship it had lost");
            Object.Destroy(rock);yield return Until(()=>Said("STRAFER_FOUND"),2,hero);Assert.IsTrue(Said("STRAFER_FOUND MusterLancer"),"in sight again, it didn't take him up");
        }
        [UnityTest] public IEnumerator AFlakGateBurstsAlternateRowsOnItsOwnClockAndTwoGatesStrikeOnceAVolley()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();
            // Facing north along its lane: its rows lie across the lane before it, 8 m deep each. Built wrong on purpose: chapter 3's Snagvine lashes whoever it aims at.
            var gate=Enemy(Broken?"SnagvineNode":"FlakGate",Sky(0,0));yield return Frames(3);var anchored=gate.Behaviour as EnemyAnchored;Assert.IsNotNull(anchored,"a Flak Gate isn't anchored");
            Put(hero,Sky(0,4));Put(partner,Sky(6,12));gate.Passive=false;
            yield return Until(()=>gate.Telegraphing,3,hero,partner);Assert.IsTrue(gate.Telegraphing,"it never fired with ships before it");
            var rows=Marks(gate);Assert.AreEqual(2,rows.Length,"its first beat isn't its first and third rows");
            Assert.IsTrue(rows.Any(r=>r.Contains(hero.transform.position)),"the first row isn't marked");Assert.IsFalse(rows.Any(r=>r.Contains(partner.transform.position)),"the second row is marked on the first beat");
            foreach(var row in rows)Assert.AreEqual(Telegraph.Kind.Line,row.Shape);
            yield return Until(()=>Said("CURTAIN_BURST"),2,hero,partner);yield return Frames(2);
            float struck=5000-hero.Health.integrity;Assert.Greater(struck,0,"the flak missed the ship in its marked row");Assert.AreEqual(5000,partner.Health.integrity,.01f,"the flak struck a ship in a quiet row");
            // Next beat, the other rows: the ship that stayed in the first row is in a quiet one now.
            Put(hero,Sky(0,4));yield return Until(()=>gate.Telegraphing,4,hero,partner);
            Assert.IsTrue(Marks(gate).Any(r=>r.Contains(partner.transform.position)),"its second beat isn't its second row");Assert.IsFalse(Marks(gate).Any(r=>r.Contains(hero.transform.position)),"its second beat marks the first row again: it follows the ship");
            yield return Until(()=>!gate.Telegraphing,2,hero,partner);yield return Frames(2);
            Assert.AreEqual(5000,hero.Health.integrity,.01f,"the flak struck a ship in a quiet row");Assert.Less(partner.Health.integrity,5000,"the flak missed the ship in the second row");
            // Two gates whose curtains overlap, on the same beat: a ship in both strikes once.
            gate.Passive=true;Object.Destroy(gate.gameObject);yield return Frames(3);
            var west=Enemy("FlakGate",Sky(-20,40));var east=Enemy("FlakGate",Sky(20,40));yield return Frames(3);Put(hero,Sky(0,44));Put(partner,Sky(-60,-60));
            west.Passive=east.Passive=false;yield return Until(()=>Said("CURTAIN_BURST FlakGate beat=1")&&log.Count(l=>l.StartsWith("CURTAIN_BURST FlakGate beat=1"))>=2,4,hero);yield return Frames(2);
            Assert.AreEqual(struck,5000-hero.Health.integrity,1,"two curtains bursting together struck a ship in both twice");
        }

        // ------------------------------------------------------------------ the Flows' and the Foundry's bodies
        [UnityTest] public IEnumerator APikeguardPairTakesTurnsToBraceAndNeitherStrikesAHeroBetweenThem()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,7));
            // Built wrong on purpose: two of chapter 4's sentries, guardians who don't pair.
            string id=Broken?"DrownedSentry":"MusterPikeguard";
            var left=Enemy(id,At(-2.1f,.05f,0));var right=Enemy(id,At(2.1f,.05f,0));yield return Frames(3);
            var a=left.Behaviour as EnemyGuardian;var b=right.Behaviour as EnemyGuardian;Assert.IsNotNull(a,"a Pikeguard isn't a guardian");
            left.Passive=right.Passive=false;
            yield return Until(()=>left.Telegraphing||right.Telegraphing,5,hero);
            Assert.IsTrue(Said("PAIR_FORMED"),"two woken together didn't pair");Assert.IsNotNull(a.PairedWith,"they aren't a pair");
            var bracer=left.Telegraphing?left:right;var cover=bracer==left?right:left;
            Assert.IsTrue(bracer.Telegraphing&&!cover.Telegraphing,"both thrust at once: one braces, the other covers");
            Assert.AreEqual(Telegraph.Kind.Line,bracer.AttackMark.Shape,"the brace isn't a line");Assert.IsTrue(bracer.AttackMark.Contains(hero.transform.position),"the brace isn't aimed at him");
            yield return Until(()=>!bracer.Telegraphing,2,hero);Assert.Less(hero.Health.integrity,5000,"the braced thrust missed a hero in its line");
            // Between them: neither strikes, and both plates face away from him.
            yield return Wait(.3f,hero);
            var middle=(left.transform.position+right.transform.position)*.5f;middle.y=.05f;Put(hero,middle);
            for(float end=Time.time+1.2f;Time.time<end;)
            {
                hero.motor.Move(Vector2.zero,false,false);
                Assert.IsFalse((left.Telegraphing&&left.AttackMark!=null&&left.AttackMark.Contains(hero.transform.position))||(right.Telegraphing&&right.AttackMark!=null&&right.AttackMark.Contains(hero.transform.position)),"a pikeguard struck at a hero between the pair");
                yield return null;
            }
            Assert.IsTrue(a.HeroBetween,"a hero between them isn't seen as between them");Assert.IsTrue(Said("PAIR_SPLIT"));
            Assert.AreEqual(100,Hit(left.Health,hero),1,"a blow from between them didn't land whole on the left one's flank");Assert.AreEqual(100,Hit(right.Health,hero),1,"a blow from between them didn't land whole on the right one's flank");
            // Then they step apart round him and turn in: time to be out.
            yield return Until(()=>a.Gap>EnemyGuardian.PairGap+EnemyGuardian.Spread*.9f,3,hero);Assert.Greater(a.Gap,EnemyGuardian.PairGap+1,"they never stepped apart round a hero between them");
            yield return Until(()=>left.Telegraphing||right.Telegraphing,4,hero);Assert.IsTrue(left.Telegraphing||right.Telegraphing,"stepped apart, neither turned in on him");
        }
        [UnityTest] public IEnumerator AMortarsShellBurnsWhereItLands()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,7));
            // Built wrong on purpose: chapter 3's Sporecaster leaves a cloud that only slows.
            var mortar=Enemy(Broken?"Sporecaster":"MusterMortar",At(0,.05f,0),false);
            yield return Until(()=>mortar.Telegraphing,5,hero);Assert.IsTrue(mortar.Telegraphing,"it never lobbed at a hero 7 m off");
            var landing=mortar.AttackMark;Assert.AreEqual(Telegraph.Kind.Circle,landing.Shape);Assert.IsTrue(landing.Contains(hero.transform.position),"the shell isn't marked where it will land");
            Assert.GreaterOrEqual(mortar.TellDuration,1.2f,"the shell is in the air too short a time to step out of");
            yield return Until(()=>!mortar.Telegraphing,3,hero);yield return null;Assert.Less(hero.Health.integrity,5000,"the shell landed on a hero in its circle and did nothing");
            mortar.Passive=true;
            var slag=LingeringArea.Over(hero.transform.position);Assert.IsNotNull(slag,"the shell left nothing where it landed");Assert.AreEqual(AreaEffect.Burn,slag.Effect,"what the shell leaves doesn't burn");
            var ring=Telegraph.Live.FirstOrDefault(t=>t!=null&&ReferenceEquals(t.owner,slag));Assert.IsNotNull(ring);Assert.IsFalse(Faint(ring),"burning ground is drawn as a boundary: it hurts, so it is a danger (D217)");
            float before=hero.Health.integrity;yield return Wait(1.2f,hero);Assert.Less(hero.Health.integrity,before,"a hero standing on the burning slag isn't burnt");
        }
        [UnityTest] public IEnumerator AnAshCrawlerLungesDownAMarkedLaneAndLeavesGroundThatTurnsHot()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-22));
            // Built wrong on purpose: chapter 6's Rime-stalker bites twice on circles, and goes on hiding where it is whatever the ground does.
            var crawler=Enemy(Broken?"RimeStalker":"AshCrawler",At(0,.05f,0),false);yield return Frames(3);var ambusher=crawler.Behaviour as EnemyAmbusher;Assert.IsNotNull(ambusher);
            yield return Until(()=>ambusher.Hidden,4,hero);Assert.IsTrue(ambusher.Hidden,"fixture: it goes under");
            var from=crawler.transform.position;Put(hero,At(0,.05f,2.4f));
            yield return Until(()=>crawler.Telegraphing,3,hero);Assert.IsTrue(crawler.Telegraphing,"a hero stepped into its ring and it never came up at him");
            var lane=crawler.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,lane.Shape,"it comes up with no lunge marked");Assert.IsTrue(lane.Contains(hero.transform.position),"its lunge isn't marked at him");
            Assert.GreaterOrEqual(lane.Length,4.5f,"its lunge's lane is shorter than its lunge");
            yield return Until(()=>Said("AMBUSH_SPENT"),3,hero);
            Assert.IsTrue(Said("AMBUSH_LUNGE AshCrawler"),"it didn't lunge");Assert.Less(hero.Health.integrity,5000,"its lunge missed the hero it was marked at");
            Assert.Greater(Flat(crawler.transform.position,from),2.5f,"it didn't go the length of its lunge");Assert.AreEqual(1,log.Count(l=>l.StartsWith("AMBUSH_LUNGE")),"it lunged more than once before it was spent");
            // Its next hide, and the ground under it turning hot (a seam lighting under the Flows' crust): up and away to a cold one.
            Put(hero,At(0,.05f,-30));yield return Until(()=>ambusher.Hidden,8,hero);Assert.IsTrue(ambusher.Hidden,"fixture: it goes under again");
            var hide=ambusher.Spot.transform.position;
            var source=new GameObject("Fixture crust");heat=source;GroundHeat.Register(source,p=>Flat(p,hide)<4?.5f:0);
            yield return Until(()=>Said("AMBUSH_FLEES"),2,hero);Assert.IsTrue(Said("AMBUSH_FLEES AshCrawler"),"its hide's ground turned hot and it stayed under");
            Assert.IsFalse(GroundHeat.Hot(ambusher.Spot.transform.position),"it fled to another hide on hot ground");
            yield return Until(()=>ambusher.Hidden,6,hero);Assert.IsTrue(ambusher.Hidden,"it never went under again on cold ground");Assert.IsFalse(GroundHeat.Hot(crawler.transform.position),"it went under on hot ground");
        }
        /// <summary>An ambusher walking to a hide goes under at it however long a frame is. Its walk ended only once it was within .6 m, and a long frame's step (its pace times the
        /// frame: 2 m at a third of a second, a hitch or a machine starved by another Unity) carried a crawler from .84 m short to 1.16 m past and back again for ever, pacing round a cold
        /// hide and never going under. The walk back from a hide gone hot is its own hides' spacing, its ring times 1.9 (6.84 m), the walk that failed in harrow-look-10 (November 14),
        /// in a run whose fights went at a quarter of their usual pace.</summary>
        [UnityTest] public IEnumerator AnAmbusherGoesUnderAtTheHideItWalksToHoweverLongAFrameIs()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-30));
            float apart=Def("AshCrawler").tune.wake*1.9f;var failures=new List<string>();
            try
            {
                foreach(float frame in new[]{1/60f,1/4f,1/3f})
                {
                    var hide=new GameObject("Fixture hide").AddComponent<AmbushSpot>();hide.transform.position=At(apart,0,0);
                    // Built wrong on purpose: the hide's ground is hot, so it never goes under there.
                    if(Broken){var source=new GameObject("Fixture crust");heat=source;var at=hide.transform.position;GroundHeat.Register(source,p=>Flat(p,at)<2?.5f:0);}
                    var crawler=Enemy("AshCrawler",At(0,.05f,0));yield return Frames(2);var ambusher=crawler.Behaviour as EnemyAmbusher;Assert.IsNotNull(ambusher);
                    Time.captureDeltaTime=frame;crawler.Passive=false;
                    yield return Until(()=>ambusher.Hidden,6,hero);
                    Time.captureDeltaTime=0;
                    if(!ambusher.Hidden||ambusher.Spot!=hide)failures.Add($"at a frame of {frame:0.000} s it walked to a cold hide {apart:0.00} m off and never went under there: it stands {Flat(crawler.transform.position,hide.transform.position):0.00} m from it");
                    crawler.Passive=true;Object.Destroy(crawler.gameObject);Object.Destroy(hide.gameObject);
                    if(heat!=null){GroundHeat.Unregister((Object)heat);Object.Destroy((Object)heat);heat=null;}
                    yield return Frames(3);
                }
            }
            finally{Time.captureDeltaTime=0;}
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator APourDronesPourBurnsOnItsRailAndNotBesideIt()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            var rail=Rail(At(0,.05f,-4),At(0,.05f,16));
            // Built wrong on purpose: chapter 7's Rail Warden fires a bolt down its rail and leaves nothing.
            var drone=Enemy(Broken?"RailWarden":"PourDrone",At(0,.05f,-2));yield return Frames(3);Assert.IsInstanceOf<EnemyRider>(drone.Behaviour,"a Pour Drone isn't a rider");
            Put(hero,At(0,.05f,5));Put(partner,At(3.6f,.05f,5));drone.Passive=false;
            yield return Until(()=>drone.Telegraphing,4,hero,partner);Assert.IsTrue(drone.Telegraphing,"it never poured at a hero under its rail");
            var strip=drone.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,strip.Shape);Assert.Less(Vector3.Angle(strip.Forward,Vector3.forward),3,"its pour isn't down its rail");
            Assert.IsTrue(strip.Contains(hero.transform.position),"fixture: he is under the rail");Assert.IsFalse(strip.Contains(partner.transform.position),"fixture: his partner stands beside it");
            yield return Until(()=>!drone.Telegraphing,3,hero,partner);yield return Wait(.3f,hero,partner);drone.Passive=true;
            Assert.Less(hero.Health.integrity,5000,"the pour missed the hero under it");Assert.AreEqual(5000,partner.Health.integrity,.01f,"the pour reached a hero beside the rail");
            var poured=LingeringArea.Over(hero.transform.position);Assert.IsNotNull(poured,"the pour left nothing on the rail");Assert.AreEqual(AreaEffect.Burn,poured.Effect,"what it poured doesn't burn");
            Assert.IsNull(LingeringArea.Over(partner.transform.position),"what it poured lies beside the rail");
            float before=hero.Health.integrity;yield return Wait(1.2f,hero,partner);Assert.Less(hero.Health.integrity,before,"a hero standing in the pour isn't burnt");
        }
        [UnityTest] public IEnumerator AForgeHoundCaughtInTheAirIsKnockedDownNotBrokenAndScorchesWhereItComesDown()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,5));
            // Built wrong on purpose: chapter 6's Bridle-hound shatters at the first blow in the air.
            var hound=Enemy(Broken?"BridleHound":"ForgeHound",At(0,.05f,0),false);yield return Frames(3);var leaper=hound.Behaviour as EnemyLeaper;Assert.IsNotNull(leaper,"a Forge Hound isn't a leaper");
            yield return Until(()=>leaper.Airborne,6,hero);Assert.IsTrue(leaper.Airborne,"fixture: it leaps at a hero 5 m off");
            Hit(hound.Health,hero,50);yield return Frames(3);
            Assert.IsTrue(hound.Health.Alive,"caught in the air, it broke apart as the Ancelane one does: the Muster's copy is too crude to");
            Assert.IsTrue(leaper.KnockedDown&&Said("LEAPER_KNOCKED_DOWN ForgeHound"),"caught in the air, it wasn't knocked down");
            Assert.IsTrue(hound.Health.Broken,"knocked down, it isn't open");Assert.That(hound.Health.BrokenUntil-Time.time,Is.InRange(1.2f,1.7f),"down for as long as one thrown off a guard");
            var scorch=LingeringArea.Over(hound.transform.position);Assert.IsNotNull(scorch,"where it came down isn't scorched");Assert.AreEqual(AreaEffect.Burn,scorch.Effect);
            // And a leap that lands scorches too.
            yield return Until(()=>!hound.Health.Broken,3,hero);foreach(var area in LingeringArea.All.ToArray())area.Dismiss();yield return null;
            Put(hero,hound.transform.position+Vector3.forward*5);
            yield return Until(()=>Said("LEAPER_PIN")||Said("LEAPER_MISSED"),6,hero);Assert.IsTrue(Said("LEAPER_PIN")||Said("LEAPER_MISSED"),"it never leapt again");
            Assert.IsNotNull(LingeringArea.Over(hound.transform.position),"where its leap came down isn't scorched");
        }

        // ------------------------------------------------------------------ Vessa Korl
        [UnityTest] public IEnumerator VessasSweepCrossesAMarkedFanCoverTakesItAndHerLanceVents()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            // A rack between her and one hero. Built wrong on purpose: no rack, and the beam reaches him.
            if(!Broken)Block(At(2.5f,1.5f,2.5f),new Vector3(1.2f,3,1.2f));
            var vessa=Enemy("VessaKorl",At(0,.1f,0));yield return Frames(3);var boss=vessa.GetComponent<BossController>();
            Put(hero,At(0,.05f,5));Put(partner,At(5,.05f,5));
            Assert.IsTrue(boss.Perform("HeatSweep"),"fixture: her sweep");yield return null;
            var fan=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Cone);Assert.IsNotNull(fan,"her sweep marks no fan");
            Assert.IsTrue(fan.Contains(hero.transform.position)&&fan.Contains(partner.transform.position),"fixture: both stand in her fan");Assert.AreEqual(BossController.VessaTune.SweepTell,fan.Duration,.01f);
            yield return Until(()=>Said("VESSA_VENTS"),4,hero,partner);
            Assert.Less(hero.Health.integrity,5000,"the beam crossed a hero in the open and missed him");Assert.AreEqual(5000,partner.Health.integrity,.01f,"the beam went through the rack to the hero behind it");
            Assert.IsTrue(vessa.Health.Broken,"her lance vented and she isn't open");Assert.That(vessa.Health.BrokenUntil-Time.time,Is.InRange(1.4f,1.85f),"open for her vent, 1.8 s");
            Assert.AreEqual(150,Hit(vessa.Health,hero),1,"a blow while she vents isn't an opening's half as much again");
        }
        [UnityTest] public IEnumerator VessasPikeCallBringsHerPairsThrustsWithHerOwn()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,9));
            var vessa=Enemy("VessaKorl",At(0,.1f,-2));yield return Frames(3);var boss=vessa.GetComponent<BossController>();
            // Her pair before her. Built wrong on purpose: no squad, and she holds the pike alone.
            var squad=Broken?new EnemyBrain[0]:new[]{Enemy("MusterPikeguard",At(-2.1f,.05f,2)),Enemy("MusterPikeguard",At(2.1f,.05f,2))};yield return Frames(3);
            foreach(var pike in squad)pike.Passive=false;
            Assert.IsTrue(boss.Perform("PikeCall"),"fixture: her call");yield return null;
            Assert.IsTrue(Said("VESSA_PIKES 2"),"her call wasn't answered by both her pikes: "+string.Join(", ",log.Where(l=>l.StartsWith("VESSA_PIKES"))));
            foreach(var pike in squad){Assert.IsTrue(pike.Telegraphing,"a pike of hers didn't brace at her call");Assert.IsTrue(pike.AttackMark.Contains(hero.transform.position),"a pike braced at somebody else");}
            var hers=Marks(boss).FirstOrDefault(t=>t.Shape==Telegraph.Kind.Line);Assert.IsNotNull(hers,"her own thrust isn't marked");Assert.IsTrue(hers.Contains(hero.transform.position));
            float before=hero.Health.integrity;yield return Until(()=>!boss.Busy,4,hero);Assert.Less(hero.Health.integrity,before-BossController.VessaTune.PikeDamage*.3f,"her call landed nothing on the hero it was aimed at");
        }
        [UnityTest] public IEnumerator AtHalfVessaFallsBackToHerWayOutSetsHerTalksFlagAndLaysTheFloorBehindHer()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-6));
            // The gantry door to the heart, 20 m behind her. Built wrong on purpose: none in the room, and she goes straight away from the party.
            if(!Broken){var exit=new GameObject("Fixture gantry door").AddComponent<BossExit>();exit.boss="VessaKorl";exit.transform.position=At(12,0,20);}
            var vessa=Enemy("VessaKorl",At(0,.1f,0));yield return Frames(3);var boss=vessa.GetComponent<BossController>();var body=vessa.Health;
            hero.Health.InvulnerableUntil=GameTime.Now+60;
            body.Receive(new DamagePacket{source=hero.Health,amount=1,type=Neutral(body)});vessa.Passive=false;
            var started=new List<string>();System.Action<BossController,string> moved=(who,move)=>{if(who==boss)started.Add(move);};BossController.MoveStarted+=moved;
            try
            {
                for(float end=Time.unscaledTime+8;(boss.Busy||body.Broken)&&Time.unscaledTime<end;)yield return null;
                body.integrity=body.maximum*.48f;
                yield return Until(()=>Said("BOSS_SHIFT VessaKorl 2"),5,hero);Assert.IsTrue(Said("BOSS_SHIFT VessaKorl 2"),"at half she never shifted");
                Assert.IsTrue(Set(BossController.FallingBackFlag),"her shift didn't set the flag her talk plays on");
                Assert.AreEqual(1+BossController.VessaTune.HotBonus,vessa.DamageScale,.001f,"in her second half her blows take the shared phase step, not her own (VessaTune.HotBonus): over an elite's 16% of a bar");
                yield return Until(()=>started.Contains("FallBack"),4,hero);Assert.IsTrue(started.Contains("FallBack"),"her shift didn't bring her falling back: "+string.Join(", ",started));
                var post=At(12,0,20)+(Flat(At(0,0,0),At(12,0,20))>0?(At(0,0,0)-At(12,0,20)).normalized:Vector3.back)*BossController.VessaTune.PostBeforeExit;
                yield return Until(()=>Said("VESSA_POUR"),8,hero);Assert.IsTrue(Said("VESSA_POUR"),"falling back, she never laid the floor");
                Assert.Less(Flat(vessa.transform.position,post),1.5f,"she doesn't hold before her way out to the heart");
                var burning=LingeringArea.All.Where(a=>a!=null&&a.Effect==AreaEffect.Burn).ToArray();Assert.GreaterOrEqual(burning.Length,BossController.VessaTune.Pours,"her pour didn't leave its strips burning");
                foreach(var strip in burning){var ring=Telegraph.Live.FirstOrDefault(t=>t!=null&&ReferenceEquals(t.owner,strip));Assert.IsNotNull(ring);Assert.IsFalse(Faint(ring),"her burning floor is drawn as a boundary");}
                StringAssert.Contains("Drive Vessa Korl out",boss.FightGoal(""),"the objective doesn't say what her second half is");
            }
            finally{BossController.MoveStarted-=moved;}
            vessa.Passive=true;
        }
        [UnityTest] public IEnumerator AtNoughtVessaLeavesByHerWayOutHerSquadFightsOnAndTheZonesMusicComesBack()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,-8));
            var exit=new GameObject("Fixture gantry door").AddComponent<BossExit>();exit.boss="VessaKorl";exit.transform.position=At(0,0,14);
            string zoneTrack=MusicDirector.Current.Track;
            // Built wrong on purpose: her sister, who yields where she stands.
            var vessa=Enemy(Broken?"HessaKorl":"VessaKorl",At(0,.1f,0));var squad=new[]{Enemy("MusterPikeguard",At(-2.1f,.05f,-3)),Enemy("MusterPikeguard",At(2.1f,.05f,-3))};yield return Frames(3);
            string track=MusicDirector.BossTrack;Assert.AreEqual(track,MusicDirector.Current.Track,"fixture: her fight plays the boss track (her definition names none: her sister's)");
            Assert.IsTrue(string.IsNullOrEmpty(Def("VessaKorl").bossMusicId),"her definition names a track of its own: stage it (MusicIntake.Stage) and say so here");
            int xp=State.party[0].xp+State.party[0].level*100000;
            vessa.Health.Receive(new DamagePacket{source=hero.Health,amount=vessa.Health.maximum*20,type=Neutral(vessa.Health)});yield return Frames(3);
            Assert.IsFalse(vessa.Health.Alive);Assert.IsTrue(Set("bossdown.VessaKorl")||Broken&&Set("bossdown.HessaKorl"),"at nought she isn't counted as beaten");
            Assert.AreEqual(BossController.Ending.Leaves,BossController.EndingOf(Broken?"HessaKorl":"VessaKorl"),"at nought she doesn't leave");
            Assert.Greater(State.party[0].xp+State.party[0].level*100000,xp,"falling back paid the party nothing");
            Assert.AreNotEqual(track,MusicDirector.Current.Track,"she has gone and the boss track plays on: the zone's own music should come back");
            foreach(var pike in squad){Assert.IsTrue(pike.Health.Alive,"her squad went with her");}
            yield return Wait(4.5f,hero);
            Assert.IsTrue(vessa==null||!vessa.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.gameObject.activeInHierarchy),"gone through her way out, her body still stands in the gallery");
            foreach(var pike in squad)pike.Passive=false;yield return Until(()=>squad.Any(p=>p.Telegraphing),5,hero);Assert.IsTrue(squad.Any(p=>p.Telegraphing),"with her gone her squad stopped fighting");
            Assert.AreEqual(zoneTrack,MusicDirector.Current.Track,"the zone's own track isn't the one that came back");
        }

        // ------------------------------------------------------------------ what Vessa's length is sized on
        const float DriverShare=.3f,FlatOutSeconds=15;
        /// <summary>The fewest frames a second any of the test's measures ran at, and the least a measure is believed at: a hero acts on a frame, so a machine too busy to draw sixty reads
        /// every pair slow (QaPlanCombatTests.FlatOutFrames is the same guard, and HarrowgateBossTests'). Under it the length test says it could not measure.</summary>
        float slowest=float.MaxValue;const float MeasureFrames=50;
        /// <summary>A pair flat out on a body that never moves, as QaPlanCombatTests measures it: both heroes attacking, every skill as its cooldown and Charge allow; one who fights with an
        /// Edge stands at its reach, one with an Emitter a few steps off.</summary>
        IEnumerator Rate(string[] pair,System.Action<float> measured)
        {
            yield return Deploy(pair);
            var at=At(0,.1f,0);var heroes=PartyController.Current.members.ToArray();
            var enemy=Enemy("VessaKorl",at);yield return Frames(3);var body=enemy.Health;
            enemy.GetComponent<BossController>().enabled=false;body.maximum=body.integrity=10000000;
            float reach=enemy.GetComponent<CharacterController>().radius+.9f;var homes=new Vector3[heroes.Length];
            for(int i=0;i<heroes.Length;i++)homes[i]=DevLoadout.Weapon(heroes[i].character)==GearSlot.Edge?at+Quaternion.Euler(0,i==0?-20:20,0)*Vector3.back*reach:at+Vector3.back*(reach+5)+Vector3.right*(i==0?-2:2);
            for(int i=0;i<heroes.Length;i++){Put(heroes[i],homes[i]);heroes[i].target=body;heroes[i].charge=100;}
            yield return null;
            float landed=0;System.Action<Health,DamagePacket,float> count=(_,packet,amount)=>{if(heroes.Any(h=>packet.source==h.Health))landed+=amount;};body.Damaged+=count;
            int drawn=0;
            for(float end=Time.unscaledTime+FlatOutSeconds;Time.unscaledTime<end;drawn++)
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
            body.Damaged-=count;Assert.Greater(landed,0,"fixture: "+string.Join(" and ",pair)+" are hitting her");measured(landed/FlatOutSeconds);
            slowest=Mathf.Min(slowest,drawn/FlatOutSeconds);TestContext.WriteLine($"VESSA_FRAMES {string.Join("+",pair)} {drawn/FlatOutSeconds:0}/s");
            enemy.Passive=true;Object.Destroy(enemy.gameObject);
            foreach(var left in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(left is GravityWell||left is PulseField||left is StarfallStrike||left is NetSeed)Object.Destroy(left.gameObject);
            yield return Frames(3);
        }
        /// <summary>Her sheet's own bounds on today's build, with nothing assumed but the driver's share: the floor at the driver's share and the ceiling at a quarter of flat out, for every pair a player can have out by then, at Sync 22 in the chapter's arrival parts
        /// (Tempered). Her whole bar is fought (the second half from the gantry door). And the integrity is pinned to the builder: a rebuild writes whatever its source says.</summary>
        [UnityTest] public IEnumerator VessaLastsHerOwnBoundsForEveryPairAPlayerCanPutOut()
        {
            string source=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Scripts/Editor/World/HarrowgateBuilder.Enemies.cs"));
            float integrity=Def("VessaKorl").integrity;
            StringAssert.Contains("e.id=\"VessaKorl\";",source);StringAssert.Contains($"e.integrity={integrity:0};e.breakThreshold=650;",source,"a rebuild would put Vessa's old integrity back");
            var sheet=ContentRegistry.Boss("VessaKorl");
            // Built wrong on purpose: three times the integrity, which no pair gets through in two minutes.
            if(Broken)integrity*=3;
            yield return Zone("Arena_Ground");Floor();yield return Level(sheet.sync);
            var failures=new List<string>();slowest=float.MaxValue;
            foreach(var pair in new[]{new[]{"Taren","Idra"},new[]{"Taren","Sela"},new[]{"Sela","Idra"}})
            {
                float rate=0;yield return Rate(pair,r=>rate=r);
                float driven=integrity/(rate*DriverShare),quarter=integrity/(rate*.25f);
                string line=$"VESSA_RATE {pair[0]}+{pair[1]} sync={sheet.sync} tempered rate={rate:0}/s driven={driven:0}s quarter={quarter:0}s bounds={sheet.minSeconds:0}-{sheet.maxSeconds:0}";
                TestContext.WriteLine(line);Debug.Log(line);
                if(driven<sheet.minSeconds)failures.Add($"{line}: the floor, {sheet.minSeconds:0} s at the driver's share");
                if(quarter>sheet.maxSeconds)failures.Add($"{line}: the ceiling, {sheet.maxSeconds:0} s at a quarter of flat out");
            }
            if(slowest<MeasureFrames)Assert.Inconclusive($"a measure ran at {slowest:0} frames a second (under {MeasureFrames:0}), on a machine too busy to say how fast a pair is: run it again with the machine quieter");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
