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
    /// <summary>Chapter 9's ordinary fights, each on a bare floor or open sky in the proving yards with what it needs made here (nothing waits on Nacre Grave, the Terminator, the Separation
    /// Works or the Broken Ring being built; their contracts are in each zone's map doc). The party is the one the chapter is met by: chapter 9's arrival (DevLoadout chapter9: three heroes,
    /// Taren and Idra out, Tempered parts), with every pair a player can put out where the fight is about the party. The hit sizes on all three heroes at the chapter's Syncs and parts; one test
    /// for each mode the chapter added, saying what a player sees: the tow-drone hauling its catch the way it tows, the fragment thrashing its free side and never its tether, the scavenger
    /// keeping to the dawn's warm band and fleeing shade, and the dawn taking what it reaches, the weld arm's burning seam and its standing down at rest, the warden giving ground and standing
    /// aside, the interceptors pinning and striking and the two answers that free a pinned ship. The chapter's expectations for the shared enemy tests are here too (the bosses' are the second
    /// combat engineer's, in their class).</summary>
    public sealed class HomeCombatTests
    {
        const float X=230;
        readonly List<string> log=new();
        static readonly string[] Heeded={"TETHER_","FRAGMENT_","ANCHOR_","AMBUSH_","DAWN_","LOBBER_","RIDER_","MACHINE_","WARDEN_","GUARD_","BOSS_LEAVES","PAIR_","PIN_","STRAFER_","COMBAT_KILL"};
        void Heard(string text,string stack,LogType type){foreach(string prefix in Heeded)if(text.StartsWith(prefix)){log.Add(text);return;}}
        /// <summary>Off unless this class's own run sets it (HOME_COMBAT_BROKEN=1): each test then builds its fixture wrong, so the run shows every test failing the way it should.</summary>
        static bool Broken=>System.Environment.GetEnvironmentVariable("HOME_COMBAT_BROKEN")=="1";
        Object dawn;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter9");log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Heard;
            if(dawn!=null){Dawn.Unregister(dawn);Object.Destroy(dawn);dawn=null;}
            LingeringArea.DismissAll();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ what a chapter writes by hand for the shared tests
        [ContentRegistry.Expectations] static void HomeEnemies(ContentRegistry.Book book)
        {
            // Where a hero stands for each ordinary enemy's tell: inside the scavenger's ring, the gull's throw, the rig's ring, on the weld arm's strip ahead of it, inside the warden's
            // baton; flying inside the drone's harpoon, the fragment's fan (laid ahead of it, the way it is set facing) and the interceptor's run-in. Alone, an interceptor strikes: a pair
            // is a pair only with its partner.
            book.Tell("HeatScavenger",2.4f);book.Tell("SlagGull",7);book.Tell("OverrideRig",2);book.Tell("WeldArm",5);book.Tell("CompactWarden",2);
            book.Tell("TowDrone",8,flight:true);book.Tell("BridleFragment",5,flight:true);book.Tell("Interceptor",8,flight:true);
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
        /// <summary>Every hero wears this frame (Tempered on arriving, Choirwrought by the chapter's end).</summary>
        static IEnumerator Frame(string id)
        {
            var part=GameCatalog.Find<TechPartDef>(id);Assert.IsNotNull(part,"fixture: "+id+" is defined (ProgressionBuilder)");var inv=new Inventory(State);
            foreach(var member in State.party)Assert.IsTrue(inv.Equip(member,inv.GivePart(part),part),"fixture: "+member.id+" wears "+id);
            PartyController.Current.RefreshStats();yield return null;
        }
        static Vector3 At(float x,float y,float z)=>new(X+x,y,z);
        static Vector3 Sky(float x,float z)=>new(200+x,1,z);
        static void Floor()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Home fixture floor";floor.transform.position=new Vector3(X,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static GameObject Block(Vector3 centre,Vector3 size)
        {var wall=new GameObject("Home fixture block",typeof(BoxCollider));wall.transform.position=centre;wall.GetComponent<BoxCollider>().size=size;wall.isStatic=true;Physics.SyncTransforms();return wall;}
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
        /// <summary>A stand-in for the Terminator's dawn (docs/maps/Terminator.md, "Run fights contract"): a straight line across the floor at <paramref name="line"/>(), coming on toward +z,
        /// and shade where <paramref name="shade"/> says.</summary>
        void Dawning(System.Func<float> line,System.Func<Vector3,bool> shade)
        {
            if(dawn!=null)Dawn.Unregister(dawn);
            dawn=new GameObject("Fixture dawn");Dawn.Register(dawn,p=>p.z-line(),shade);
        }

        // ------------------------------------------------------------------ the chapter's loadout and its numbers
        static readonly string[] Ordinary={"TowDrone","BridleFragment","HeatScavenger","SlagGull","OverrideRig","WeldArm","CompactWarden","Interceptor"};
        [UnityTest] public IEnumerator TheChapterNineLoadoutArrivesAsThreeAtSyncTwentyFiveInTemperedPartsWithTarenAndIdraOut()
        {
            // Built wrong on purpose: chapter 8's arrival is the three at 22.
            if(Broken)DevLoadout.Apply("chapter8");
            yield return Zone("Arena_Ground");
            CollectionAssert.AreEquivalent(new[]{"Taren","Sela","Idra"},State.party.Select(m=>m.id).ToArray(),"chapter 9 arrives without its three heroes");
            foreach(var member in State.party)Assert.AreEqual(25,member.level,member.id+" arrives at the wrong Sync: the plan ends chapter 8 at 25");
            CollectionAssert.AreEquivalent(new[]{"Taren","Idra"},PartyController.Current.members.Select(m=>m.character).ToArray(),"the pair out isn't the one the join leaves out");
            foreach(var member in State.party)
            {
                string worn=member.equipped.TryGetValue(GearSlot.Frame,out var id)?State.parts.FirstOrDefault(p=>p.instanceId==id)?.definitionId:null;
                Assert.AreEqual("FramesT4",worn,member.id+" doesn't arrive in the Tempered frame the chapter's numbers are sized on");
            }
            Assert.IsTrue(GameServices.Current.Flags.GetBool("chapter8.complete"),"chapter 8 isn't told");Assert.IsFalse(GameServices.Current.Flags.GetBool("chapter9.complete"),"chapter 9 arrives already told");
        }
        /// <summary>The standing rule (CHAPTER_GUIDE section 0.1): an ordinary hit costs a hero 9 to 13% of the bar. Measured through Health.Receive as a real blow lands, Plating and all, on all
        /// three heroes (Idra, the guard hero, is built with more Plating, so she is the floor and Taren and Sela the ceiling): every ordinary attack of the chapter at Sync 25 and 27 in the Tempered
        /// parts it arrives in, and at 27 in the Choirwrought frame the chapter makes reachable. A hit that hurts nobody (the Interceptor's tractor line: the pass it holds a ship for does) is
        /// left out by being authored at nought, and must be. Every chapter 9 zone is at threat 1, every capsule is at least twice its radius tall, no guardian's arc reaches 180 degrees, and
        /// every one of the eight is drawn in its own generated body.</summary>
        [UnityTest] public IEnumerator EveryOrdinaryHitTakesANinthToAnEighthOfAnyHerosBar()
        {
            yield return Zone("Arena_Ground");Floor();
            var source=Enemy("HeatScavenger",At(40,.05f,40)).Health;yield return Frames(2);
            var attacks=Ordinary.SelectMany(id=>Def(id).attacks.Where(a=>a!=null&&a.damage>0).Select(a=>(id,what:a.name,damage:a.damage))).ToList();
            // Built wrong on purpose: a hit authored at 135 is over the band at 25, and the check has to say so.
            if(Broken)attacks.Add(("fixture","a hit authored at 135",135));
            var failures=new List<string>();
            foreach(var (frame,syncs) in new[]{("FramesT4",new[]{25,27}),("FramesT5",new[]{27})})
            {
                yield return Frame(frame);
                foreach(var pair in new[]{new[]{"Taren","Idra"},new[]{"Sela","Idra"}})
                {
                    yield return Deploy(pair);
                    foreach(int sync in syncs)
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
                                string line=$"HOME_HIT {hero.character} sync={sync} {frame} plating={hero.plating:0} {id} {what} {damage:0} share={share:P1}";TestContext.WriteLine(line);
                                if(share<.09f||share>.13f)failures.Add(line+": the plan asks 9 to 13%");
                            }
                        }
                    }
                }
            }
            foreach(string id in Ordinary)foreach(var attack in Def(id).attacks)if(attack!=null&&attack.damage<=0&&!(id=="Interceptor"&&attack.name=="InterceptorTractor"))failures.Add($"{id}'s {attack.name} hurts nobody: only the tractor line is meant to");
            foreach(string zone in new[]{"NacreGrave","MeridianCrawl","Terminator","DecksLeaving","SeparationWorks","BrokenRing"})
            {var z=GameCatalog.Find<ZoneDef>(zone);if(z!=null&&!Mathf.Approximately(z.enemyThreat,1))failures.Add($"{zone} is at threat {z.enemyThreat}: chapter 9's damage is authored at its real weight, at 1");}
            foreach(string id in Ordinary)
            {
                var def=Def(id);float round=def.capsuleRadius>0?def.capsuleRadius:.55f;float tall=def.capsuleHeight>0?def.capsuleHeight:1.5f;
                if(tall<round*2)failures.Add($"{id}'s capsule is {tall} m tall on a {round} m radius: Unity stretches it and the body floats");
                if(def.behaviour==EnemyBehaviourKind.Guardian&&def.tune.arc>=180)failures.Add($"{id}'s guarded arc is {def.tune.arc}: a flank blow must land whole");
                // Built wrong on purpose: as if the Warden still wore the Pikeguard it stood in for.
                string body=def.prefab!=null?def.prefab.name:"nothing";if(Broken&&id=="CompactWarden")body="MusterPikeguard";
                if(body!=id)failures.Add($"{id} is drawn as {body}, not in its own generated body");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ Nacre Grave
        [UnityTest] public IEnumerator ATowDroneHarpoonsAShipAndHaulsItTheWayItTowsUntilTheLineIsCut()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-60,-60));Put(hero,Sky(0,9));
            // Built wrong on purpose: chapter 3's Claim Drone reels a ship in to itself and hauls nothing.
            var drone=Enemy(Broken?"ClaimDrone":"TowDrone",Sky(0,0),true,90);yield return Frames(3);var tether=drone.Behaviour as EnemyTetherer;Assert.IsNotNull(tether,"a Tow-drone isn't a tetherer");
            drone.Passive=false;yield return Until(()=>drone.Telegraphing,5,hero);Assert.IsTrue(drone.Telegraphing,"it never harpooned at a ship 9 m off");
            var line=drone.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,line.Shape,"its harpoon isn't marked as a line");Assert.IsTrue(line.Contains(hero.transform.position),"its line isn't laid to the ship");
            Assert.GreaterOrEqual(drone.TellDuration,.85f,"its harpoon is too quick to read");
            yield return Until(()=>Said("TETHER_ON"),2,hero);Assert.IsTrue(Said("TETHER_ON"),"its harpoon missed the ship it was laid to");Assert.Less(hero.Health.integrity,5000,"the catch is a marked hit, and it did nothing");
            // The hold is renewed each tick from the one after the catch: read it there.
            yield return Frames(2);Assert.Less(hero.Health.Pace,.5f,"a caught ship isn't held to under half its speed");
            // Set facing east: it hauls its catch east, along the way it tows, as it hauls hulls.
            var from=hero.transform.position;var drawnFrom=drone.transform.position;
            // Reeled at 3.5 m/s toward a drone hauling at 3 east: about 3.9 m east in 2.4 s (the drone 7.2), where a drone that only reels moves him not at all.
            yield return Wait(2.4f,hero);
            float hauled=hero.transform.position.x-from.x;TestContext.WriteLine($"TOW_HAULED ship={hauled:0.00} drone={drone.transform.position.x-drawnFrom.x:0.00}");
            Assert.Greater(hauled,2.5f,"a caught ship isn't hauled the way the drone tows: it should be carried along with it, not only reeled in");
            Assert.Greater(drone.transform.position.x-drawnFrom.x,4.5f,"holding a catch, the drone doesn't haul it anywhere");
            Assert.Less(Flat(drone.transform.position,hero.transform.position),15*EnemyTetherer.SnapsAt,"fixture: the line still holds");
            // One shot on the drone cuts the line.
            Hit(drone.Health,hero,20);yield return Frames(2);
            Assert.IsTrue(Said("TETHER_OFF TowDrone cut"),"a hit on the drone didn't cut its line");Assert.IsNull(tether.Held,"cut, it still holds the ship");
        }
        [UnityTest] public IEnumerator ABridleFragmentThrashesItsFreeSideAndNeverOverItsTether()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-60,-60));
            // Set facing east, away from its tether on the west. Built wrong on purpose: chapter 3's Snagvine Knot lashes whoever it turns to.
            var piece=Enemy(Broken?"SnagvineNode":"BridleFragment",Sky(0,0),true,90);yield return Frames(3);Assert.IsInstanceOf<EnemyAnchored>(piece.Behaviour,"a Bridle Fragment isn't anchored");
            var from=piece.transform.position;
            // In its lee, on the tether's side: it thrashes (it sees him) but never over its tether, and he isn't touched.
            Put(hero,Sky(-5,0));piece.Passive=false;
            bool laidOnHim=false;
            for(float end=Time.time+6;Time.time<end;){hero.motor.Move(Vector2.zero,false,false);if(piece.Telegraphing&&piece.AttackMark!=null&&piece.AttackMark.Contains(hero.transform.position))laidOnHim=true;yield return null;}
            Assert.IsTrue(Said("FRAGMENT_THRASHES BridleFragment"),"with a ship 5 m off its tether side it never thrashed: it thrashes at what it sees");
            Assert.IsFalse(laidOnHim,"it laid a lash over its own tether, onto a ship in its lee");Assert.AreEqual(5000,hero.Health.integrity,.01f,"a ship in its lee was struck");
            // On its free side: the fan is laid on him and the lash lands.
            Put(hero,Sky(5,0),false);
            yield return Until(()=>piece.Telegraphing&&piece.AttackMark!=null&&piece.AttackMark.Contains(hero.transform.position),5,hero);
            Assert.IsTrue(piece.Telegraphing,"it never thrashed at a ship on its free side");var fan=piece.AttackMark;Assert.AreEqual(Telegraph.Kind.Cone,fan.Shape,"its thrash isn't a marked fan");
            Assert.Less(Vector3.Angle(fan.Forward,Vector3.right),3,"its fan isn't laid the way it was set facing");Assert.GreaterOrEqual(piece.TellDuration,.95f,"its thrash is too quick to read");
            yield return Until(()=>!piece.Telegraphing,2,hero);yield return Frames(2);Assert.Less(hero.Health.integrity,5000,"its lash missed the ship in its fan");
            Assert.Less(Flat(piece.transform.position,from),.05f,"an anchored fragment moved");Assert.Less(Vector3.Angle(piece.transform.forward,Vector3.right),1,"a tethered fragment turned");
        }

        // ------------------------------------------------------------------ the Terminator
        [UnityTest] public IEnumerator AHeatScavengerHuntsTheDawnsWarmBandFleesShadeAndTheDawnTakesWhatItReaches()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,10));
            // The line across the floor at z -2, still for now: the warm band is z -0.5 to 12. Shade first nowhere. Built wrong on purpose: no dawn at all, as anywhere but the Terminator.
            float lineZ=-2;var shaded=new List<Bounds>();
            if(!Broken)Dawning(()=>lineZ,p=>shaded.Any(b=>b.Contains(new Vector3(p.x,0,p.z))));
            var scav=Enemy("HeatScavenger",At(6,.05f,4),false);yield return Frames(3);var ambusher=scav.Behaviour as EnemyAmbusher;Assert.IsNotNull(ambusher,"a Heat-scavenger isn't an ambusher");
            yield return Until(()=>ambusher.Hidden,8,hero);Assert.IsTrue(ambusher.Hidden,"fixture: it goes under");
            var hide=ambusher.Spot.transform.position;TestContext.WriteLine($"SCAVENGER_HIDE ahead={Dawn.Ahead(hide):0.0} x={hide.x-X:0.0}");
            Assert.IsTrue(Dawn.Warm(hide),$"it went under {Dawn.Ahead(hide):0.0} m ahead of the dawn line: it hunts the warm band, {Dawn.BandFrom} to {Dawn.BandTo} m ahead, out of shade");
            Assert.Less(Mathf.Abs(hide.x-hero.transform.position.x),9,"it hides nowhere near the hero it hunts: across from him in the band");
            // Its hide's ground falls into shade (a rock's long shadow comes over it): up and away to warm ground.
            shaded.Add(new Bounds(new Vector3(hide.x,0,hide.z),new Vector3(5,2,5)));
            yield return Until(()=>Said("AMBUSH_FLEES"),2,hero);Assert.IsTrue(Said("AMBUSH_FLEES HeatScavenger"),"its hide fell into shade and it stayed under");
            yield return Until(()=>ambusher.Hidden,6,hero);Assert.IsTrue(ambusher.Hidden,"it never went under again");
            Assert.IsTrue(Dawn.Warm(ambusher.Spot.transform.position),"it went under again in shade or out of the band");
            // A hero in shade inside its ring isn't hunted: it won't come up into shade after him.
            var spot=ambusher.Spot.transform.position;shaded.Add(new Bounds(new Vector3(spot.x+4,0,spot.z),new Vector3(4.4f,2,6)));
            float whole=hero.Health.integrity;Put(hero,new Vector3(spot.x+3,.05f,spot.z),false);
            Assert.IsTrue(ambusher.Spot.Within(hero.transform.position)&&Dawn.Shade(hero.transform.position),"fixture: he stands in its ring, in shade");
            for(float end=Time.time+2.5f;Time.time<end;){hero.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.IsTrue(ambusher.Hidden,"it came up into shade after a hero standing there");Assert.AreEqual(whole,hero.Health.integrity,.01f,"a hero in shade was hurt by it");
            // Out of the shade, in the same ring: up it comes.
            Put(hero,new Vector3(spot.x,.05f,spot.z+2.2f),false);
            yield return Until(()=>scav.Telegraphing,3,hero);Assert.IsTrue(scav.Telegraphing,"fixture: out of shade in its ring, it comes up at him");
            yield return Until(()=>Said("AMBUSH_SPENT"),3,hero);Assert.IsTrue(Said("AMBUSH_LUNGE HeatScavenger"),"it came up and never lunged");
            // The dawn comes on past it: it flares where it stands and falls, in sight, and pays nothing.
            Put(hero,At(0,.05f,40),false);int xp=State.party[0].xp;int level=State.party[0].level;
            lineZ=scav.transform.position.z+4;
            yield return Until(()=>!scav.Health.Alive,3,hero);
            Assert.IsTrue(Said("DAWN_TAKES HeatScavenger"),"the dawn passed it by 4 m and didn't take it");Assert.IsFalse(scav.Health.Alive,"the dawn passed it and it lives");
            Assert.AreEqual(level,State.party[0].level,"the dawn's kill raised a Sync");Assert.AreEqual(xp,State.party[0].xp,"the dawn's kill paid XP: the party didn't win it");
        }
        [UnityTest] public IEnumerator ASlagGullSpitsOntoAMarkedCircleThatBurnsAfter()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,8));
            // Built wrong on purpose: chapter 3's Sporecaster leaves a cloud that only slows.
            var gull=Enemy(Broken?"Sporecaster":"SlagGull",At(0,.05f,0),false);
            yield return Until(()=>gull.Telegraphing,5,hero);Assert.IsTrue(gull.Telegraphing,"it never spat at a hero 8 m off");
            var landing=gull.AttackMark;Assert.AreEqual(Telegraph.Kind.Circle,landing.Shape);Assert.IsTrue(landing.Contains(hero.transform.position),"the spit isn't marked where it will land");
            Assert.GreaterOrEqual(gull.TellDuration,1.15f,"the spit is in the air too short a time to step out of");
            yield return Until(()=>!gull.Telegraphing,3,hero);yield return null;Assert.Less(hero.Health.integrity,5000,"the spit landed on a hero in its circle and did nothing");
            gull.Passive=true;
            var slag=LingeringArea.Over(hero.transform.position);Assert.IsNotNull(slag,"the spit left nothing where it landed");Assert.AreEqual(AreaEffect.Burn,slag.Effect,"what the spit leaves doesn't burn");
            var ring=Telegraph.Live.FirstOrDefault(t=>t!=null&&ReferenceEquals(t.owner,slag));Assert.IsNotNull(ring);Assert.IsFalse(Faint(ring),"burning slag is drawn as a boundary: it hurts, so it is a danger (D217)");
            float before=hero.Health.integrity;yield return Wait(1.2f,hero);Assert.Less(hero.Health.integrity,before,"a hero standing in the burning slag isn't burnt");
            // Nothing it leaves outlives it.
            Object.Destroy(gull.gameObject);yield return Frames(3);Assert.IsNull(LingeringArea.Over(hero.transform.position),"its slag outlived the gull into whatever comes next");
        }

        // ------------------------------------------------------------------ the Separation Works
        [UnityTest] public IEnumerator AnOverrideRigDischargesAllRoundItsClampAndOnlyThere()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            // Built wrong on purpose: chapter 3's Snagvine lashes a fan at the one it aims at, not all round it.
            var rig=Enemy(Broken?"SnagvineNode":"OverrideRig",At(0,.05f,0));yield return Frames(3);Assert.IsInstanceOf<EnemyAnchored>(rig.Behaviour,"an Override Rig isn't anchored");
            Put(hero,At(0,.05f,4.5f));Put(partner,At(-40,.05f,-40));rig.Passive=false;
            yield return Wait(2,hero,partner);Assert.IsFalse(rig.Telegraphing,"it discharged at a hero 4.5 m off: its ring is 2.8");Assert.AreEqual(5000,hero.Health.integrity,.01f);
            Put(hero,At(0,.05f,2),false);Put(partner,At(-.5f,.05f,-2),false);
            yield return Until(()=>rig.Telegraphing,3,hero,partner);Assert.IsTrue(rig.Telegraphing,"it never discharged at a hero inside its ring");
            var ring=rig.AttackMark;Assert.AreEqual(Telegraph.Kind.Circle,ring.Shape,"its discharge isn't marked all round it");
            Assert.IsTrue(ring.Contains(hero.transform.position)&&ring.Contains(partner.transform.position),"its discharge doesn't reach both heroes at its clamp, before and behind");
            yield return Until(()=>!rig.Telegraphing,2,hero,partner);yield return Frames(2);
            Assert.Less(hero.Health.integrity,5000,"its discharge missed the hero before it");Assert.Less(partner.Health.integrity,5000,"its discharge missed the hero behind it");
        }
        [UnityTest] public IEnumerator AWeldArmLaysItsWeldDownItsStripLeavesTheSeamBurningAndStandsDownAtRest()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();
            // Built wrong on purpose: chapter 8's Pour Drone pours down its rail too, but it is broken, not given back.
            var arm=Enemy(Broken?"PourDrone":"WeldArm",At(0,.05f,0));yield return Frames(3);Assert.IsInstanceOf<EnemyRider>(arm.Behaviour,"a Weld Arm isn't a rider");
            var from=arm.transform.position;
            Put(hero,At(0,.05f,5));Put(partner,At(3.6f,.05f,5));arm.Passive=false;
            yield return Until(()=>arm.Telegraphing,4,hero,partner);Assert.IsTrue(arm.Telegraphing,"it never welded at a hero on its strip");
            var strip=arm.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,strip.Shape);Assert.Less(Vector3.Angle(strip.Forward,Vector3.forward),3,"its weld isn't laid the way it was set facing");
            Assert.IsTrue(strip.Contains(hero.transform.position),"fixture: he is on its strip");Assert.IsFalse(strip.Contains(partner.transform.position),"fixture: his partner stands beside it");
            yield return Until(()=>!arm.Telegraphing,3,hero,partner);yield return Wait(.3f,hero,partner);arm.Passive=true;
            Assert.Less(hero.Health.integrity,5000,"the weld missed the hero on its strip");Assert.AreEqual(5000,partner.Health.integrity,.01f,"the weld reached a hero beside its strip");
            var seam=LingeringArea.Over(hero.transform.position);Assert.IsNotNull(seam,"the weld left nothing on its strip");Assert.AreEqual(AreaEffect.Burn,seam.Effect,"the fresh seam doesn't burn");
            Assert.IsNull(LingeringArea.Over(partner.transform.position),"the seam lies beside the strip");
            Assert.Less(Flat(arm.transform.position,from),.05f,"a weld arm moved off its mount");
            // Its override cut: the arm stays where it worked, at rest, and only the fighter goes. Its seam goes with it.
            arm.Health.Receive(new DamagePacket{source=hero.Health,amount=arm.Health.maximum*3,type=DamageType.Kinetic});
            yield return Wait(2.5f,hero,partner);
            Assert.IsTrue(Said("MACHINE_AT_REST"),"at nought it was broken, not given back");
            var rest=Object.FindObjectsByType<MachineAtRest>(FindObjectsSortMode.None);Assert.AreEqual(1,rest.Length,"nothing of the arm stands where it worked");
            Assert.IsTrue(rest[0].Settled,"the arm never settled to rest");Assert.Less(Flat(rest[0].transform.position,from),1.2f,"what stands at rest isn't where the arm worked");
            Assert.IsTrue(rest[0].GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"the arm at rest is drawn as nothing");
            Assert.IsEmpty(rest[0].GetComponentsInChildren<Collider>(),"the arm at rest kept a fighter's collider");
            Assert.IsNull(LingeringArea.Over(hero.transform.position),"its seam outlived the arm");
        }
        [UnityTest] public IEnumerator ACompactWardenGivesGroundWhenItsGuardBreaksAndAtNoughtStandsAsideAndWalksOut()
        {
            yield return Zone("Arena_Ground");Floor();var (hero,partner)=Party();Put(partner,At(-50,.05f,-50));Put(hero,At(0,.05f,2.2f));
            // Built wrong on purpose: chapter 8's Pikeguard fights to the last and falls.
            var warden=Enemy(Broken?"MusterPikeguard":"CompactWarden",At(0,.05f,0));yield return Frames(3);var guard=warden.Behaviour as EnemyGuardian;Assert.IsNotNull(guard,"a Compact Warden isn't a guardian");
            // Its shield: drawn, raised, and its front plated; a blow from behind lands whole.
            var shield=warden.GetComponent<RiotShield>();Assert.IsNotNull(shield,"its riot shield isn't drawn");yield return Frames(2);Assert.IsTrue(shield.Raised,"its shield isn't raised while its guard is up");
            float front=Hit(warden.Health,hero,100);Put(partner,At(0,.05f,-2.2f),false);float back=Hit(warden.Health,partner,100);Put(partner,At(-50,.05f,-50),false);
            Assert.Less(front,30,"a blow from in front of its shield landed whole");Assert.AreEqual(100,back,1,"a blow from behind it didn't land whole");
            // Its guard broken: it backs off from whoever broke it, shield down, and stands open.
            var before=warden.transform.position;
            warden.Health.Receive(new DamagePacket{source=hero.Health,amount=1,type=DamageType.Plasma,breakPower=5000});
            Assert.IsTrue(warden.Health.Broken,"fixture: its guard is broken");
            yield return Wait(.8f,hero);
            Assert.IsTrue(Said("WARDEN_GIVES_GROUND CompactWarden"),"its guard broken, it stood its ground");
            float backed=Flat(warden.transform.position,hero.transform.position)-Flat(before,hero.transform.position);TestContext.WriteLine($"WARDEN_BACKED {backed:0.00}");
            Assert.Greater(backed,2.2f,"giving ground, it didn't back off from the hero who broke it");Assert.IsTrue(warden.Health.Broken,"giving ground, it isn't open");Assert.IsFalse(shield.Raised,"its guard broken, its shield is still raised");
            // At nought it stands aside: no fall, no kill; it walks out, away from the party, and is gone.
            var at=warden.transform.position;
            warden.Health.Receive(new DamagePacket{source=hero.Health,amount=warden.Health.maximum*3,type=DamageType.Plasma});
            Assert.IsTrue(Said("WARDEN_STANDS_ASIDE CompactWarden")&&Said("BOSS_LEAVES CompactWarden"),"at nought it fell where it stood");
            yield return Wait(2.4f,hero);
            Assert.IsTrue(warden!=null&&warden.gameObject!=null,"it vanished at once instead of walking out");
            float walked=Flat(warden.transform.position,at);float away=Flat(warden.transform.position,hero.transform.position)-Flat(at,hero.transform.position);
            TestContext.WriteLine($"WARDEN_WALKED {walked:0.00} away={away:0.00}");
            Assert.Greater(walked,3,"it didn't walk out of the fight");Assert.Greater(away,3,"it walked out toward the party, not away from it");
            yield return Wait(2,hero);Assert.IsTrue(warden==null,"it never left: it stood there after its leaving");
        }

        // ------------------------------------------------------------------ the Broken Ring
        [UnityTest] public IEnumerator AnInterceptorPairPinsAShipWhileTheOtherRunsItsLaneThroughHimAndEitherAnswerFreesHim()
        {
            yield return Zone("Arena_Flight");var (hero,partner)=Party();Put(partner,Sky(-80,-80));Put(hero,Sky(0,13));
            // Built wrong on purpose: two of chapter 8's Lancers, strafers who don't pair.
            string id=Broken?"MusterLancer":"Interceptor";
            var a=Enemy(id,Sky(-3,0));var b=Enemy(id,Sky(3,0));yield return Frames(3);var sa=a.Behaviour as EnemyStrafer;var sb=b.Behaviour as EnemyStrafer;Assert.IsNotNull(sa,"an Interceptor isn't a strafer");
            var hurtBy=new List<Health>();System.Action<Health,DamagePacket,float> hurt=(_,packet,amount)=>{if(amount>0)hurtBy.Add(packet.source);};hero.Health.Damaged+=hurt;
            a.Passive=b.Passive=false;
            yield return Until(()=>Said("PIN_ON"),7,hero);
            Assert.IsTrue(Said("PAIR_FORMED Interceptor"),"two woken together didn't pair");Assert.IsTrue(Said("PIN_ON Interceptor"),"neither pinned the ship");
            var pinner=sa.Pins?sa:sb;var striker=pinner==sa?sb:sa;var pinnerBody=pinner==sa?a:b;var strikerBody=pinner==sa?b:a;
            Assert.IsFalse(striker.Pins,"both pin: one pins, the other strikes");Assert.AreEqual(hero,pinner.Pinned,"it pinned someone else");
            Assert.IsFalse(hurtBy.Contains(pinnerBody.Health),"the tractor line itself hurt the ship: the pass it holds him for does");
            // The pin is renewed each tick from the one after the catch: read it there.
            yield return Frames(2);Assert.LessOrEqual(hero.Health.Pace,EnemyStrafer.PinPace+.01f,"a pinned ship isn't held to its pinned pace");
            Assert.IsNotNull(Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).FirstOrDefault(l=>l.name=="Tractor line"),"the tractor line isn't drawn");
            // While he is pinned the striker lines up on him.
            yield return Until(()=>strikerBody.Telegraphing,PinWindow,hero);
            Assert.IsTrue(strikerBody.Telegraphing,"its partner pinned a ship and the striker never ran at him");Assert.IsTrue(strikerBody.AttackMark.Contains(hero.transform.position),"the striker's lane isn't laid through the pinned ship");
            // The first answer: shoot the one holding the line.
            Hit(pinnerBody.Health,hero,20);yield return Frames(2);Assert.IsTrue(Said("PIN_OFF Interceptor cut"),"a hit on the pinner didn't cut its line");Assert.IsNull(pinner.Pinned,"cut, it still pins him");
            // The second: cover between them breaks it.
            log.RemoveAll(l=>l.StartsWith("PIN_"));
            yield return Until(()=>Said("PIN_ON"),9,hero);Assert.IsTrue(Said("PIN_ON"),"fixture: it pins him again");
            var between=(pinnerBody.transform.position+hero.transform.position)*.5f;Block(between,new Vector3(4,6,4));
            yield return Until(()=>Said("PIN_OFF"),2,hero);Assert.IsTrue(Said("PIN_OFF Interceptor cover"),"a wall between them didn't break the line");
            hero.Health.Damaged-=hurt;
        }
        const float PinWindow=EnemyStrafer.PinHolds;
    }
}
