using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>TEST_PLAN 4.4 read the way a tester reads it: each skill at Sync 12 from each slot on both forms, no wall in the way of
    /// anything, each of the 14 enemy types on its own, and the boss table for all four bosses. Exploratory: these state what the plan
    /// says should be true from what a player would see, and a red one is a finding.</summary>
    public sealed class QaPlanCombatTests
    {
        readonly List<(float at,string line)> log=new();
        void Logged(string text,string stack,LogType type){if(text.StartsWith("SKILL_KIND ")||text.StartsWith("HESSA_OPEN")||text.StartsWith("BURROWER_STUCK"))log.Add((Time.unscaledTime,text));}
        System.Action<BossController,string> moved;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");log.Clear();Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;
            if(moved!=null){BossController.MoveStarted-=moved;moved=null;}
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static CombatActor Taren=>PartyController.Current.members.First(m=>m.character=="Taren");
        static CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        static CombatActor Other(CombatActor hero)=>PartyController.Current.members.First(m=>m!=hero);
        static MemberState Member(string id)=>GameServices.Current.State.party.Find(m=>m.id==id);
        static GameState State=>GameServices.Current.State;
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static T[] All<T>() where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static void Quiet(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        static IEnumerator Zone(string zone,bool clearEnemies=true)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)){if(clearEnemies)Object.Destroy(enemy.gameObject);else enemy.Passive=true;}
            Quiet();
            yield return new WaitForSecondsRealtime(.7f);
        }
        /// <summary>Stands a hero with the fixtures' +z ahead, whatever the last skill did to the body or the ship's nose.</summary>
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}
            else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            Physics.SyncTransforms();
        }
        static void PlaceBody(EnemyBrain enemy,Vector3 position)
        {var body=enemy.GetComponent<CharacterController>();body.enabled=false;enemy.transform.position=position;body.enabled=true;Physics.SyncTransforms();}
        /// <summary>Open floor far from the arena's walls.</summary>
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Plan fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        /// <summary>A wall as a door, a seal or a crate is one: solid, and not marked static.</summary>
        static GameObject Wall(Vector3 center,Vector3 size)
        {var wall=new GameObject("Plan fixture wall",typeof(BoxCollider));wall.transform.position=center;wall.GetComponent<BoxCollider>().size=size;Physics.SyncTransforms();return wall;}
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=passive;return enemy;}
        /// <summary>A body that never dies or breaks.</summary>
        static EnemyBrain Dummy(string id,Vector3 at){var enemy=Enemy(id,at);var body=enemy.Health;body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;return enemy;}
        static string Small(bool flight)=>flight?"ChoristerDart":"Ridgehound";
        static void Sync(int level)
        {
            foreach(var member in GameServices.Current.State.party){member.level=level;member.skillUses?.Clear();}
            PartyController.Current.RefreshStats();
        }
        static CombatActor Lead(string hero)
        {
            var party=PartyController.Current;if(party.Active.character!=hero)Assert.IsTrue(party.Swap(),"fixture: the swap");
            Quiet();return party.Active;
        }
        static SkillDef Equip(CombatActor hero,SkillKind kind,int slot=0)
        {
            var skill=SkillBook.All(hero.character).First(s=>s.kind==kind);
            Assert.IsTrue(SkillBook.Equip(Member(hero.character),slot,skill),"fixture: "+kind+" can't be equipped");
            hero.charge=100;hero.cooldowns[slot]=0;return skill;
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>A motor only carries out a dash while it is being driven, as the brain does every frame.</summary>
        static IEnumerator Drive(CombatActor actor,float seconds)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){actor.motor.Move(Vector2.zero,false,false);yield return null;}}
        static IEnumerator UntilFree(CombatActor hero)
        {for(float end=Time.unscaledTime+2;!hero.CanAct&&Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);yield return null;}}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static void Remove<T>() where T:Component{foreach(var left in Object.FindObjectsByType<T>(FindObjectsSortMode.None))Object.Destroy(left.gameObject);}
        static void ClearEffects(){Remove<SurveyDrone>();Remove<WaypointCharge>();Remove<GravityWell>();Remove<StarfallStrike>();Remove<PulseField>();Remove<NetSeed>();Remove<FaultlineWave>();Remove<TetherLine>();}
        static void Fresh(CombatActor actor){actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;}
        static DamageType Neutral(Health body)=>System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
        static TMP_Text HudText(string name)=>Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name==name);

        EnemyBrain victim;int hits;float dealt,firstHit;
        /// <summary>Stands a hero at the origin with one body straight ahead, releases the skill and drives the motor.</summary>
        IEnumerator Try(CombatActor hero,SkillKind kind,float distance,float seconds,string body)
        {
            float y=hero.flight?1:0;
            yield return UntilFree(hero);
            ClearEffects();if(victim!=null)Object.Destroy(victim.gameObject);yield return Frames(2);
            Place(hero,new Vector3(200,y,0));Place(Other(hero),new Vector3(260,y,0));
            var struck=Dummy(body,new Vector3(200,y,distance));victim=struck;yield return Frames(3);hero.target=struck.Health;
            hits=0;dealt=0;firstHit=-1;float cast=Time.unscaledTime;
            struck.Health.Damaged+=(_,packet,amount)=>{if(packet.source!=hero.Health)return;hits++;dealt+=amount;if(firstHit<0)firstHit=Time.unscaledTime-cast;};
            Equip(hero,kind);Assert.IsTrue(hero.Skill(0),"fixture: "+kind+" would not start");
            yield return Drive(hero,seconds);
        }

        // ------------------------------------------------------------------ all sixteen, each in its turn
        /// <summary>Where to stand for a skill to reach: (on foot, aloft).</summary>
        static (float ground,float flight) Stand(SkillKind kind)=>kind switch
        {
            SkillKind.ArcCleave=>(1.4f,3f),SkillKind.EmberStep=>(3f,5f),SkillKind.PulseBurst=>(3f,3f),
            SkillKind.ThreadLance=>(6f,6f),SkillKind.ScatterBloom=>(2.5f,4f),SkillKind.StaticNet=>(4f,4f),
            SkillKind.AnchorSlam=>(5f,7f),SkillKind.TetherHook=>(8f,8f),SkillKind.ForgeSpiral=>(2f,3f),SkillKind.Faultline=>(8f,8f),
            SkillKind.SurveyDrone=>(6f,6f),SkillKind.WaypointBlink=>(2.5f,2.6f),SkillKind.GravityFold=>(5f,5f),SkillKind.Starfall=>(4f,4f),
            _=>(4f,4f),
        };
        [UnityTest] public IEnumerator AtSync12EverySkillLandsFromWhicheverSlotItIsInOnTheGround(){yield return Zone("Arena_Ground");Arena();yield return EverySkillLands(false);}
        [UnityTest] public IEnumerator AtSync12EverySkillLandsFromWhicheverSlotItIsInInFlight(){yield return Zone("Arena_Flight");yield return EverySkillLands(true);}
        IEnumerator EverySkillLands(bool flight)
        {
            Sync(12);float y=flight?1:0;var failures=new List<string>();int turn=0;string form=flight?"in flight":"on the ground";
            foreach(var hero in PartyController.Current.members)
            {
                foreach(var skill in SkillBook.All(hero.character))
                {
                    int slot=turn++%SkillBook.Slots;
                    yield return UntilFree(hero);ClearEffects();yield return Frames(2);
                    Place(hero,new Vector3(200,y,0));Place(Other(hero),new Vector3(260,y,0));
                    var (onFoot,aloft)=Stand(skill.kind);float distance=flight?aloft:onFoot;
                    var body=Dummy(Small(flight),new Vector3(200,y,distance));yield return Frames(3);hero.target=body.Health;
                    float landed=0;body.Health.Damaged+=(_,packet,amount)=>{if(packet.source==hero.Health)landed+=amount;};
                    if(!SkillBook.Equip(Member(hero.character),slot,skill)){failures.Add($"{skill.displayName} can't be equipped in slot {slot+1} at Sync 12");Object.Destroy(body.gameObject);continue;}
                    hero.charge=100;hero.cooldowns[slot]=0;
                    if(!hero.Skill(slot)){failures.Add($"{hero.character}'s {skill.displayName} in slot {slot+1} won't start {form} with full Charge");Object.Destroy(body.gameObject);continue;}
                    bool armed=false;
                    for(float end=Time.unscaledTime+3.5f;landed<=0&&!armed&&Time.unscaledTime<end;)
                    {
                        hero.motor.Move(Vector2.zero,false,false);yield return null;
                        armed=skill.kind==SkillKind.Overdrive&&hero.Overdriving||skill.kind==SkillKind.Refract&&hero.Refracting;
                    }
                    if(landed<=0&&!armed)failures.Add($"{hero.character}'s {skill.displayName} from slot {slot+1} {form}, with its target {distance} m away, spends its Charge and does nothing");
                    Object.Destroy(body.gameObject);
                }
            }
            ClearEffects();
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ each skill's own job, where nothing has checked it
        [UnityTest] public IEnumerator TheHookPullsTarenToABigBodyOnTheGround()
        {
            yield return Zone("Arena_Ground");Arena();Sync(12);var taren=Taren;var failures=new List<string>();
            foreach(string id in new[]{"SentinelHusk","MusterSergeant","Burrower"})
            {
                yield return Try(taren,SkillKind.TetherHook,9,1.2f,id);
                float gap=Flat(taren.transform.position,victim.transform.position);
                if(hits==0)failures.Add($"the hook never caught a {id} 9 m ahead");
                else if(gap>3.6f)failures.Add($"a hook on a {id} leaves Taren {gap:0.0} m away: the row promises it pulls him to big ones");
                if(Flat(victim.transform.position,new Vector3(200,0,9))>.5f)failures.Add($"the hook moved a {id}, which is too big to reel in");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator TarensLearnedSkillsDoTheirOwnJobsInFlight()
        {
            yield return Zone("Arena_Flight");Sync(12);var taren=Taren;var failures=new List<string>();
            yield return Try(taren,SkillKind.AnchorSlam,9,.7f,"ChoristerDart");
            if(Flat(taren.transform.position,victim.transform.position)>4.5f)failures.Add($"Anchor Slam in flight leaves the ship {Flat(taren.transform.position,victim.transform.position):0.0} m from a target it started 9 m from: it closes nothing");
            if(hits==0)failures.Add("Anchor Slam in flight arrives and doesn't strike");
            yield return Try(taren,SkillKind.TetherHook,9,1f,"ChoristerDart");
            if(Flat(taren.transform.position,victim.transform.position)>4)failures.Add($"the hook leaves a small enemy {Flat(taren.transform.position,victim.transform.position):0.0} m out in flight: it reeled nothing in");
            if(Flat(taren.transform.position,new Vector3(200,1,0))>1.5f)failures.Add("the hook flew the ship to a small enemy instead of reeling it in");
            if(!victim.Health.Broken)failures.Add("a dart reeled in in flight isn't left open");
            yield return Try(taren,SkillKind.ForgeSpiral,3,1.1f,"ChoristerDart");
            if(hits<3)failures.Add($"Forge Spiral in flight landed {hits} hits on a target 3 m ahead: the row promises again and again");
            yield return Try(taren,SkillKind.Faultline,10,1f,"ChoristerDart");
            if(hits==0)failures.Add("Faultline in flight doesn't reach ten metres");
            // A big body in flight is a collar link: the hull goes to it.
            yield return UntilFree(taren);ClearEffects();Object.Destroy(victim.gameObject);victim=null;yield return Frames(2);
            var cantor=Enemy("Cantor",new Vector3(200,1,30));yield return Frames(3);
            var link=cantor.GetComponent<CantorCollar>().Links[2];var linkAt=link.transform.position;
            Place(taren,new Vector3(linkAt.x,1,linkAt.z-9));Place(Sela,new Vector3(260,1,0));yield return Frames(2);
            taren.target=link;float whole=link.integrity;Equip(taren,SkillKind.TetherHook);Assert.IsTrue(taren.Skill(0),"fixture: the hook");
            // How near it brought him, not where he drifted: like every dash in flight it hands on its speed, and left alone the hull coasts on past the link.
            float nearest=Flat(taren.transform.position,link.transform.position);
            for(float end=Time.unscaledTime+1;Time.unscaledTime<end;){taren.motor.Move(Vector2.zero,false,false);yield return null;nearest=Mathf.Min(nearest,Flat(taren.transform.position,link.transform.position));}
            if(link.integrity>=whole)failures.Add("the hook never caught a collar link 9 m ahead");
            else if(nearest>3.6f)failures.Add($"a hook on a collar link brings the ship no nearer than {nearest:0.0} m: it doesn't pull the hull in");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator SelasLearnedSkillsDoTheirOwnJobsInFlight()
        {
            yield return Zone("Arena_Flight");Sync(12);var sela=Sela;var failures=new List<string>();
            // Waypoint Blink: away, untouchable while it goes, and the charge bursts where she was.
            yield return Try(sela,SkillKind.WaypointBlink,2.6f,.25f,"ChoristerDart");
            if(Flat(sela.transform.position,new Vector3(200,1,0))<5)failures.Add($"Waypoint Blink in flight has carried her {Flat(sela.transform.position,new Vector3(200,1,0)):0.0} m by the time the blink is over: it escapes nothing");
            float blinked=sela.Health.integrity;sela.Health.Receive(new DamagePacket{source=victim.Health,amount=20,type=DamageType.Pulse});
            if(sela.Health.integrity<blinked)failures.Add("a hit lands on her in the middle of Waypoint Blink");
            if(Object.FindFirstObjectByType<WaypointCharge>()==null)failures.Add("Waypoint Blink in flight leaves no charge behind");
            if(hits>0)failures.Add("the waypoint charge went off with no fuse");
            yield return Drive(sela,.8f);
            if(hits==0)failures.Add("the waypoint charge never burst on the enemy beside it in flight");
            if(Object.FindFirstObjectByType<WaypointCharge>()!=null)failures.Add("the waypoint charge is still there after its fuse");
            // Starfall: the circle first, and nothing falls until its warning has run.
            yield return Try(sela,SkillKind.Starfall,4,.55f,"ChoristerDart");
            if(Object.FindFirstObjectByType<SkillRing>()==null)failures.Add("Starfall in flight marks no circle");
            if(hits>0)failures.Add("Starfall in flight struck before its warning ran out");
            yield return Drive(sela,1.2f);
            if(hits==0)failures.Add("Starfall in flight never struck its marked circle");
            // Gravity Fold: two darts come together; a mine, anchored, holds its ground.
            yield return UntilFree(sela);ClearEffects();Object.Destroy(victim.gameObject);victim=null;yield return Frames(2);
            Place(sela,new Vector3(200,1,0));Place(Taren,new Vector3(260,1,0));
            var first=Dummy("ChoristerDart",new Vector3(200,1,6));var second=Dummy("ChoristerDart",new Vector3(204.5f,1,6));var mine=Dummy("Shellmine",new Vector3(196.5f,1,6));
            yield return Frames(3);sela.target=first.Health;float apart=Flat(first.transform.position,second.transform.position);var anchored=mine.transform.position;
            Equip(sela,SkillKind.GravityFold);Assert.IsTrue(sela.Skill(0),"fixture: the Fold");yield return Drive(sela,2.9f);
            if(Flat(first.transform.position,second.transform.position)>apart-2)failures.Add($"Gravity Fold in flight leaves two darts {Flat(first.transform.position,second.transform.position):0.0} m apart, from {apart:0.0}: it gathers nothing");
            if(second.Health.integrity>=second.Health.maximum)failures.Add("Gravity Fold in flight did no damage to what it gathered");
            if(Flat(mine.transform.position,anchored)>.2f)failures.Add("Gravity Fold dragged an anchored mine");
            ClearEffects();
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator FaultlineReachesTenMetresAndStopsAtAWall([Values(false,true)] bool flight)
        {
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Arena();Sync(12);float y=flight?1:0;var taren=Taren;
            yield return Try(taren,SkillKind.Faultline,10,1.1f,Small(flight));
            Assert.Greater(hits,0,"Faultline doesn't reach an enemy ten metres ahead on clear ground");
            yield return UntilFree(taren);ClearEffects();Object.Destroy(victim.gameObject);victim=null;yield return Frames(2);
            Place(taren,new Vector3(200,y,0));Place(Sela,new Vector3(260,y,0));
            Wall(new Vector3(200,4,5),new Vector3(14,20,.3f));
            var near=Dummy(Small(flight),new Vector3(200,y,3));var far=Dummy(Small(flight),new Vector3(200,y,7.5f));yield return Frames(3);taren.target=near.Health;
            Equip(taren,SkillKind.Faultline);Assert.IsTrue(taren.Skill(0),"fixture: Faultline");
            float furthest=0;
            for(float end=Time.unscaledTime+1.1f;Time.unscaledTime<end;)
            {
                taren.motor.Move(Vector2.zero,false,false);
                foreach(var ring in Object.FindObjectsByType<SkillRing>(FindObjectsSortMode.None))furthest=Mathf.Max(furthest,ring.transform.position.z);
                yield return null;
            }
            Assert.Less(near.Health.integrity,near.Health.maximum,"with a wall 5 m ahead Faultline doesn't even hit what stands on this side of it");
            Assert.AreEqual(far.Health.maximum,far.Health.integrity,.01f,"Faultline ruptures through a wall and hits the enemy behind it");
            Assert.Less(furthest,5,"a burst of Faultline shows past the wall it should have stopped at");
        }
        [UnityTest] public IEnumerator SurveyDroneKeepsFiringWhileSelaWalksAndRollsAway([Values(false,true)] bool flight)
        {
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Arena();Sync(12);float y=flight?1:0;var sela=Sela;
            Place(sela,new Vector3(200,y,-2));Place(Taren,new Vector3(260,y,0));
            var target=Dummy(Small(flight),new Vector3(200,y,5));yield return Frames(3);sela.target=target.Health;
            Equip(sela,SkillKind.SurveyDrone);Assert.IsTrue(sela.Skill(0),"fixture: the drone");float cast=Time.unscaledTime;
            yield return Drive(sela,.6f);
            var drone=SurveyDrone.Current;Assert.IsNotNull(drone,"no drone came out");
            int shots=drone.Shots;float integrity=target.Health.integrity;var from=sela.transform.position;
            // She goes about her fight: off to one side, a roll, and back across.
            for(float end=Time.unscaledTime+1.1f;Time.unscaledTime<end;){sela.motor.Move(new Vector2(1,0),false,false);yield return null;}
            float away=Flat(sela.transform.position,from);
            sela.Dodge(Vector3.left);yield return Drive(sela,.35f);
            for(float end=Time.unscaledTime+1.1f;Time.unscaledTime<end;){sela.motor.Move(new Vector2(-1,0),false,false);yield return null;}
            Assert.Greater(away,4,"fixture: she really walked off");
            Assert.IsTrue(drone!=null,"the drone goes when she moves");
            Assert.GreaterOrEqual(drone.Shots-shots,3,$"the drone fired {drone.Shots-shots} shots while she moved: it should keep firing about twice a second");
            Assert.Less(target.Health.integrity,integrity,"the drone's shots while she moved did nothing");
            // It lasts its nine seconds and then it is gone: both ends of "fires for nine seconds". She comes back into its reach for this: a ship left
            // coasting after that walk is 29 m off by now, past the drone's 14 m, and a drone with nothing in reach is right to hold its fire.
            Place(sela,new Vector3(200,y,-2));
            yield return Drive(sela,Mathf.Max(0,8.2f-(Time.unscaledTime-cast)));
            Assert.IsTrue(drone!=null,"the drone was gone before its nine seconds");
            shots=drone.Shots;yield return Drive(sela,.6f);
            Assert.Greater(drone!=null?drone.Shots-shots:0,0,"the drone stopped firing before its nine seconds were up");
            yield return Drive(sela,Mathf.Max(0,9.7f-(Time.unscaledTime-cast)));
            Assert.IsTrue(drone==null,"the drone is still there past its nine seconds");
        }

        // ------------------------------------------------------------------ no skill through a wall
        /// <summary>A wall in reach of the skill and a body just behind it, and how long the skill could still land.</summary>
        static (float wall,float body,float wait) Cover(SkillKind kind,bool flight)
        {
            if(flight)return kind switch
            {
                SkillKind.ArcCleave=>(2.5f,3.5f,.7f),SkillKind.EmberStep=>(2.2f,2.9f,.9f),SkillKind.PulseBurst=>(2.5f,3.5f,.8f),
                SkillKind.ThreadLance=>(2.5f,5f,.8f),SkillKind.ScatterBloom=>(2.5f,3.6f,.8f),SkillKind.StaticNet=>(2.5f,4f,1.6f),SkillKind.Refract=>(2.5f,5f,1.2f),
                SkillKind.AnchorSlam=>(2.6f,3.8f,1.1f),SkillKind.TetherHook=>(3f,6f,1f),SkillKind.ForgeSpiral=>(2.5f,3.5f,1.2f),SkillKind.Faultline=>(3f,3.9f,1f),
                SkillKind.SurveyDrone=>(3f,5f,2f),SkillKind.WaypointBlink=>(2.2f,2.9f,1.1f),SkillKind.GravityFold=>(2.6f,4f,2.9f),_=>(2.6f,4f,1.9f),
            };
            return kind switch
            {
                SkillKind.ArcCleave=>(.95f,1.7f,.7f),SkillKind.EmberStep=>(1.5f,2.6f,.9f),SkillKind.PulseBurst=>(1.5f,3f,.8f),
                SkillKind.ThreadLance=>(2f,5f,.8f),SkillKind.ScatterBloom=>(1.2f,2.4f,.8f),SkillKind.StaticNet=>(1.5f,3f,1.6f),SkillKind.Refract=>(1.5f,3f,1.4f),
                SkillKind.AnchorSlam=>(2.5f,4f,1.1f),SkillKind.TetherHook=>(3f,6f,1f),SkillKind.ForgeSpiral=>(1.2f,2.2f,1.2f),SkillKind.Faultline=>(3f,3.9f,1f),
                SkillKind.SurveyDrone=>(2.5f,5f,2f),SkillKind.WaypointBlink=>(1.2f,2.4f,1.1f),SkillKind.GravityFold=>(2.5f,4.5f,2.9f),_=>(3f,4.5f,1.9f),
            };
        }
        float landedByCast;bool castStarted;
        IEnumerator Cast(CombatActor hero,SkillDef skill,Health body,float wait,bool untilHit,Vector3 from)
        {
            yield return UntilFree(hero);
            Place(hero,from);hero.target=body;hero.charge=100;
            Assert.IsTrue(SkillBook.Equip(Member(hero.character),0,skill),"fixture: "+skill.displayName+" can't be equipped");hero.cooldowns[0]=0;
            landedByCast=0;
            System.Action<Health,DamagePacket,float> watch=(_,packet,amount)=>{if(packet.source==hero.Health)landedByCast+=amount;};body.Damaged+=watch;
            castStarted=hero.Skill(0);
            bool struck=false;
            for(float end=Time.unscaledTime+wait;castStarted&&Time.unscaledTime<end&&!(untilHit&&landedByCast>0);)
            {
                hero.motor.Move(Vector2.zero,false,false);
                // Refract's beam needs something to throw back: the body behind the wall takes a shot at her.
                if(skill.kind==SkillKind.Refract&&!struck&&hero.Refracting){struck=true;hero.Health.Receive(new DamagePacket{source=body,amount=1,type=DamageType.Pulse});}
                yield return null;
            }
            body.Damaged-=watch;
        }
        [UnityTest] public IEnumerator NoSkillReachesABodyBehindAWall([Values("Taren","Sela")] string who,[Values(false,true)] bool flight)
        {
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Arena();Sync(12);
            float y=flight?1:0;string form=flight?"in flight":"on the ground";var hero=PartyController.Current.members.First(m=>m.character==who);
            Place(Other(hero),new Vector3(260,y,0));var failures=new List<string>();
            foreach(var skill in SkillBook.All(who).Where(s=>s.kind!=SkillKind.Overdrive))
            {
                var (wallAt,bodyAt,wait)=Cover(skill.kind,flight);var from=new Vector3(200,y,0);var spot=new Vector3(200,y,bodyAt);
                yield return UntilFree(hero);Place(hero,from);
                var wall=flight?Wall(new Vector3(200,4,wallAt),new Vector3(14,20,.2f)):Wall(new Vector3(200,2.5f,wallAt),new Vector3(14,7,.2f));
                var body=Dummy(Small(flight),spot);yield return Frames(3);
                yield return Cast(hero,skill,body.Health,wait,false,from);
                if(!castStarted)failures.Add($"fixture: {skill.displayName} would not start {form}");
                if(landedByCast>0)failures.Add($"{who}'s {skill.displayName} {form} did {landedByCast:0} damage to a body behind a solid wall {wallAt} m away: it reaches through");
                if(Flat(body.transform.position,spot)>.3f)failures.Add($"{who}'s {skill.displayName} {form} dragged a body {Flat(body.transform.position,spot):0.0} m behind a solid wall");
                ClearEffects();Object.Destroy(wall);yield return Frames(2);Physics.SyncTransforms();PlaceBody(body,spot);
                // The same cast with the wall gone has to hit, or the line above proves nothing.
                yield return Cast(hero,skill,body.Health,wait+2.5f,true,from);
                if(landedByCast<=0)failures.Add($"fixture: with the wall gone {who}'s {skill.displayName} {form} still misses a body {bodyAt} m ahead, so its wall check says nothing");
                // That cast stopped at its first hit, and what it still has coming lands now, not on the next skill's body: Anchor Slam's wave leaves
                // the hull .2 s in and runs .4 s, from past where the next wall is about to stand, and read as a Tether Hook through that wall.
                ClearEffects();Object.Destroy(body.gameObject);yield return Frames(2);yield return UntilFree(hero);yield return Drive(hero,.7f);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ Charge against a boss
        /// <summary>TEST_PLAN 4.4: "no skill can be spammed to trivialise a boss (check Charge cost against Charge gain)". Up to Sync 11, the most this build can reach, Charge is the limit:
        /// QaFightTests.SwingsAloneCannotKeepAFullLoadoutOnCooldown. At Sync 12 plain attacks can pay for the cheapest four skills (the CHARGE line prints it, and GAME_DESIGN 5 sells Resonance
        /// as Charge gain), so what stands between a skill and a spam is its cooldown. So this hands the Charge over free at Sync 12, on the last boss of each form, and asks the two things
        /// that would make it spam: does any skill go off more often than its cooldown, and does the boss still last a minute, half the plan's floor, at the share of flat out the replay driver gets.</summary>
        [UnityTest] public IEnumerator NoSkillCanBeSpammedToTrivialiseABoss([Values(false,true)] bool flight)
        {
            string id=flight?"Cantor":"HessaKorl";
            yield return FlatOut(id,12,true);
            float y=flight?1:0;string form=flight?"in flight":"on the ground";var failures=new List<string>();
            foreach(var hero in PartyController.Current.members)foreach(var skill in SkillBook.All(hero.character))
            {
                int casts=log.Count(l=>l.line.StartsWith($"SKILL_KIND {hero.character} {skill.id} "));
                if(casts>FlatOutSeconds/skill.cooldown+1)failures.Add($"with Charge free, {hero.character}'s {skill.displayName} went off {casts} times in {FlatOutSeconds:0} s on a {skill.cooldown:0} s cooldown: it can be spammed");
            }
            float integrity=GameCatalog.Find<EnemyDef>(id).integrity,share=DriverShare(id),seconds=integrity/(flatOutRate*share);
            TestContext.WriteLine($"SPAM_RATE {id} sync=12 rate={flatOutRate:0}/s atDriverShare={seconds:0}s");
            if(seconds<60)failures.Add($"at Sync 12 with Charge free the party takes {flatOutRate:0} integrity a second off {id}, and {seconds:0} s of fight at the replay driver's {share:P0} of that: skills on cooldown trivialise it");
            // Charge against cost, as the plan asks, for the record: this is what the cooldowns are standing in for.
            foreach(var hero in PartyController.Current.members)
            {
                Place(Other(hero),new Vector3(260,y,0));Place(hero,new Vector3(200,y,0));hero.damageScale=1;
                bool melee=hero.character=="Taren"&&!flight;
                var body=Dummy(Small(flight),new Vector3(200,y,melee?1.4f:6));body.Health.weakness=body.Health.resistance=DamageType.Pulse;
                yield return Frames(3);hero.target=body.Health;hero.charge=0;
                float gained=0,last=0;const float Seconds=6;
                for(float end=Time.unscaledTime+Seconds;Time.unscaledTime<end;)
                {
                    hero.motor.Move(Vector2.zero,false,false);hero.Attack();
                    if(hero.charge>last)gained+=hero.charge-last;
                    if(hero.charge>90)hero.charge=0;
                    last=hero.charge;yield return null;
                }
                float income=gained/Seconds;
                Assert.Greater(income,0,"fixture: plain attacks land and pay Charge");
                float Spend(int mastery)=>SkillBook.All(hero.character).Select(s=>SkillBook.Cost(s,mastery)/s.cooldown).OrderBy(r=>r).Take(SkillBook.Slots).Sum();
                TestContext.WriteLine($"CHARGE {hero.character} {form} income={income:0.0} cheapest={Spend(1):0.0} mastered={Spend(SkillBook.MaxMastery):0.0}");
                Object.Destroy(body.gameObject);yield return Frames(2);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator AMasteredMultiHitSkillStillCannotPayForItself([Values(false,true)] bool flight)
        {
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Arena();Sync(12);
            float y=flight?1:0;string form=flight?"in flight":"on the ground";var failures=new List<string>();
            foreach(var (who,kind,seconds,onFoot,aloft) in new[]{("Taren",SkillKind.ForgeSpiral,1.3f,2f,3f),("Sela",SkillKind.SurveyDrone,9.6f,4f,4f),("Sela",SkillKind.Starfall,2.3f,4f,4f),("Sela",SkillKind.GravityFold,3.1f,4f,4f),("Sela",SkillKind.StaticNet,5f,4f,4f),("Sela",SkillKind.ScatterBloom,1f,1.4f,4f)})
            {
                var hero=Lead(who);var other=Other(hero);Place(hero,new Vector3(200,y,0));Place(other,new Vector3(260,y,0));
                var body=Dummy(Small(flight),new Vector3(200,y,flight?aloft:onFoot));yield return Frames(3);hero.target=body.Health;
                var skill=SkillBook.All(who).First(s=>s.kind==kind);var member=Member(who);
                member.skillUses??=new Dictionary<string,int>();member.skillUses[skill.id]=SkillBook.MasteryUses[SkillBook.MasteryUses.Length-1];
                Equip(hero,kind);float cost=SkillBook.Cost(who,skill),gained=0;
                Assert.AreEqual(SkillBook.MaxMastery,SkillBook.Mastery(who,skill.id),"fixture: full mastery");
                hero.charge=cost+5;Assert.IsTrue(hero.Skill(0),"fixture: "+kind);float last=hero.charge;
                for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);if(hero.charge>last)gained+=hero.charge-last;last=hero.charge;yield return null;}
                if(body.Health.integrity>=body.Health.maximum)failures.Add($"fixture: {kind} never hit its target {form}");
                if(gained>=cost)failures.Add($"mastered, {kind} {form} costs {cost:0} Charge and pays {gained:0} back from one target: it can be spammed for free");
                ClearEffects();Object.Destroy(body.gameObject);yield return new WaitForSecondsRealtime(.2f);yield return UntilFree(hero);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        [UnityTest] public IEnumerator BreakingAPressureOrganPaysChargeLikeAnyOtherHit()
        {
            yield return Zone("Hushwell");
            foreach(var volume in All<EncounterVolume>())volume.enabled=false;
            var organ=All<PressureOrgan>().First();var hero=Taren;hero.charge=0;
            organ.Health.Receive(new DamagePacket{source=hero.Health,amount=hero.damage,type=DamageType.Kinetic});
            Assert.Less(organ.Health.integrity,organ.Health.maximum,"fixture: the organ took the hit");
            Assert.Greater(hero.charge,0,"the fight's own objective pays nothing: while the bar says BREAK THEM, a hero swinging at the organs earns no Charge, and hits are what Charge is for");
        }

        // ------------------------------------------------------------------ the companion
        [UnityTest] public IEnumerator TheCompanionSpacesItsSkillsSixToNineSecondsApart([Values("Taren","Sela")] string companion,[Values(false,true)] bool flight)
        {
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Arena();Sync(12);float y=flight?1:0;
            var lead=Lead(companion=="Taren"?"Sela":"Taren");var partner=Other(lead);
            Place(lead,new Vector3(200,y,0));Place(partner,new Vector3(205,y,0));
            var body=Dummy(Small(flight),new Vector3(200,y,7));yield return Frames(3);lead.target=body.Health;
            log.Clear();partner.GetComponent<PartnerBrain>().enabled=true;
            for(float end=Time.unscaledTime+30;Time.unscaledTime<end;){partner.charge=100;lead.target=body.Health;yield return null;}
            partner.GetComponent<PartnerBrain>().enabled=false;
            var casts=log.Where(l=>l.line.StartsWith("SKILL_KIND "+companion)).Select(l=>l.at).ToArray();
            Assert.GreaterOrEqual(casts.Length,3,$"in 30 s with full Charge and a target in reach the companion used {casts.Length} skills: it should use one every six to nine seconds");
            for(int i=1;i<casts.Length;i++)
            {
                float gap=casts[i]-casts[i-1];
                Assert.GreaterOrEqual(gap,5.9f,$"the companion used two skills {gap:0.0} s apart: the player's own skills are meant to lead");
                Assert.LessOrEqual(gap,12,$"with full Charge and a target in reach the companion went {gap:0.0} s between skills");
            }
        }
        [UnityTest] public IEnumerator TheCompanionKeepsItsSkillsWithNothingInReach([Values("Taren","Sela")] string companion)
        {
            yield return Zone("Arena_Ground");Arena();Sync(12);
            var lead=Lead(companion=="Taren"?"Sela":"Taren");var partner=Other(lead);
            Place(lead,new Vector3(200,0,-30));Place(partner,new Vector3(203,0,-30));
            var body=Dummy("Ridgehound",new Vector3(200,0,30));yield return Frames(3);lead.target=body.Health;
            log.Clear();partner.GetComponent<PartnerBrain>().enabled=true;
            for(float end=Time.unscaledTime+10;Time.unscaledTime<end;){partner.charge=100;lead.target=body.Health;yield return null;}
            Assert.IsFalse(log.Any(l=>l.line.StartsWith("SKILL_KIND "+companion)),"the companion spent Charge on a skill with its target 40 m away: "+string.Join(",",log.Select(l=>l.line)));
            // The same brain with the target in reach: it was only holding back.
            PlaceBody(body,partner.transform.position+partner.motor.Facing*4);
            for(float end=Time.unscaledTime+9.5f;Time.unscaledTime<end&&!log.Any(l=>l.line.StartsWith("SKILL_KIND "+companion));){partner.charge=100;lead.target=body.Health;yield return null;}
            partner.GetComponent<PartnerBrain>().enabled=false;
            Assert.IsTrue(log.Any(l=>l.line.StartsWith("SKILL_KIND "+companion)),"fixture: with the target in reach the companion casts");
        }
        [UnityTest] public IEnumerator TheCompanionDoesNotSwingACleaveFromWhereItCannotReach()
        {
            yield return Zone("Arena_Ground");Arena();Sync(8);
            var lead=Lead("Sela");var taren=Other(lead);Place(lead,new Vector3(200,0,-14));
            var body=Dummy("Ridgehound",new Vector3(200,0,0));yield return Frames(3);lead.target=body.Health;
            float landed=0;body.Health.Damaged+=(_,packet,amount)=>{if(packet.source==taren.Health)landed+=amount;};
            // Everything but the Cleave is on cooldown, and its first try waits three seconds: held well out till then, then let run in.
            taren.GetComponent<PartnerBrain>().enabled=true;
            void OnlyCleave(){taren.charge=100;for(int slot=1;slot<4;slot++)taren.cooldowns[slot]=99;lead.target=body.Health;}
            for(float end=Time.unscaledTime+3.2f;Time.unscaledTime<end;){OnlyCleave();Place(taren,new Vector3(203,0,-14));yield return null;}
            log.Clear();float from=-1;
            for(float end=Time.unscaledTime+4;from<0&&Time.unscaledTime<end;)
            {
                OnlyCleave();yield return null;
                if(log.Any(l=>l.line.Contains("ArcCleave")))from=Flat(taren.transform.position,body.transform.position);
            }
            Assert.GreaterOrEqual(from,0,"fixture: the companion swung its Cleave");
            for(float end=Time.unscaledTime+.4f;Time.unscaledTime<end;){OnlyCleave();yield return null;}
            taren.GetComponent<PartnerBrain>().enabled=false;
            Assert.Greater(landed,0,$"the companion swung Arc Cleave from {from:0.0} m and hit nothing: 20 Charge into the air");
        }

        // ------------------------------------------------------------------ every enemy's tell
        /// <summary>The eye's test: an attack starts, its marking is on the ground under the hero, the body winds up, and only then is anyone hurt.</summary>
        IEnumerator EveryBodyTells((string id,float distance)[] table,float y)
        {
            var failures=new List<string>();var hero=Taren;Place(Sela,new Vector3(260,y,0));
            foreach(var (id,distance) in table)
            {
                Place(hero,new Vector3(200,y,distance));hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=0;
                var body=Enemy(id,new Vector3(200,y,0));yield return Frames(2);
                var show=body.GetComponent<DefeatPresentation>().visual;var rig=body.GetComponentInChildren<GeneratedAnimator>();
                var rest=show.localRotation;float restScale=show.localScale.x;
                yield return new WaitForSecondsRealtime(.7f);
                float bitten=-1;System.Action<Health,DamagePacket,float> hurt=(_,__,amount)=>{if(bitten<0&&amount>0)bitten=Time.time;};hero.Health.Damaged+=hurt;
                body.Passive=false;
                for(float end=Time.unscaledTime+3;!body.Telegraphing&&bitten<0&&Time.unscaledTime<end;)yield return null;
                if(!body.Telegraphing)failures.Add(bitten>=0?$"a {id} hurt the hero with no tell at all":$"a {id} stood {distance} m from the hero for 3 s and never told an attack");
                else
                {
                    float toldAt=Time.time,told=body.TellDuration;var mark=body.AttackMark;
                    if(told<.5f)failures.Add($"a {id}'s tell lasts {told:0.00} s: too short to read and dodge");
                    if(mark==null)failures.Add($"a {id}'s attack marks nothing on the ground");
                    else
                    {
                        if(!mark.Contains(hero.transform.position))failures.Add($"a {id}'s marking doesn't reach the hero it is aimed at, {distance} m away");
                        var view=mark.GetComponent<MeshRenderer>();var mesh=mark.GetComponent<MeshFilter>();
                        if(view==null||!view.enabled||mesh==null||mesh.sharedMesh==null||mesh.sharedMesh.vertexCount==0)failures.Add($"a {id}'s marking draws nothing");
                    }
                    bool windingUp=false;float lean=0,swell=restScale;
                    for(float end=Time.unscaledTime+2.5f;body!=null&&body.Telegraphing&&Time.unscaledTime<end;)
                    {
                        windingUp|=rig!=null&&rig.CurrentAnimation=="Attack";
                        lean=Mathf.Max(lean,Vector3.Angle(rest*Vector3.forward,show.localRotation*Vector3.forward));swell=Mathf.Max(swell,show.localScale.x);
                        yield return null;
                    }
                    if(rig!=null?!windingUp:lean<6&&swell<restScale*1.1f)failures.Add($"a {id}'s body doesn't move through its tell (lean {lean:0} degrees, swell {swell/restScale:0.00}): only the paint says anything");
                    for(float end=Time.unscaledTime+2;bitten<0&&Time.unscaledTime<end;)yield return null;
                    if(bitten<0)failures.Add($"a {id} told an attack at a hero standing in its marking and never hit him");
                    else if(bitten-toldAt<told-.06f)failures.Add($"a {id} hit the hero {bitten-toldAt:0.00} s into a {told:0.00} s tell");
                }
                hero.Health.Damaged-=hurt;
                if(body!=null){body.Passive=true;Object.Destroy(body.gameObject);}
                yield return Frames(3);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator EveryGroundEnemyTellsBeforeItLands()
        {
            yield return Zone("Arena_Ground");Arena();
            yield return EveryBodyTells(new[]{("Ridgehound",3f),("Scrapmite",1.8f),("SentinelHusk",2f),("Gloomgrazer",3f),("MusterScav",8f),("MusterSergeant",2f)},0);
        }
        [UnityTest] public IEnumerator EveryFlyingEnemyTellsBeforeItLands()
        {
            yield return Zone("Arena_Flight");
            yield return EveryBodyTells(new[]{("ChoristerDart",3.5f),("ChoristerDrifter",8f),("MusterSkiff",8f),("Shellmine",2.5f)},1);
        }
        [UnityTest] public IEnumerator ABiteStopsAtTheEdgeOfItsPaint()
        {
            yield return Zone("Arena_Ground");Arena();var hero=Taren;Place(Sela,new Vector3(260,0,0));var failures=new List<string>();
            foreach(string id in new[]{"SentinelHusk","MusterSergeant","Scrapmite"})
            {
                var body=Enemy(id,new Vector3(200,0,0));Place(hero,new Vector3(200,0,1.8f));hero.Health.integrity=hero.Health.maximum;
                yield return new WaitForSecondsRealtime(.7f);body.Passive=false;
                for(float end=Time.unscaledTime+3;!body.Telegraphing&&Time.unscaledTime<end;)yield return null;
                var mark=body.AttackMark;
                if(mark==null||mark.Shape!=Telegraph.Kind.Circle){failures.Add($"fixture: a {id}'s bite paints a circle");body.Passive=true;Object.Destroy(body.gameObject);yield return Frames(2);continue;}
                // Clear of the ring by a clear margin, in time: this is what the paint told the player to do.
                var clear=mark.Origin+mark.Forward*(mark.Radius+.7f);clear.y=0;Place(hero,clear);float before=hero.Health.integrity;
                yield return new WaitForSecondsRealtime(1.2f);
                if(hero.Health.integrity<before)failures.Add($"a {id}'s bite reached a hero standing {.7f} m clear of its painted ring");
                // And it reaches the ring's inside.
                Place(hero,new Vector3(body.transform.position.x,0,body.transform.position.z+1.8f));before=hero.Health.integrity;
                for(float end=Time.unscaledTime+4;!body.Telegraphing&&Time.unscaledTime<end;)yield return null;
                mark=body.AttackMark;
                if(mark==null){failures.Add($"fixture: a {id} bites again");}
                else
                {
                    var inside=mark.Origin+mark.Forward*(mark.Radius-.2f);inside.y=0;Place(hero,inside);
                    for(float end=Time.unscaledTime+1.5f;hero.Health.integrity>=before&&Time.unscaledTime<end;)yield return null;
                    if(hero.Health.integrity>=before)failures.Add($"a {id}'s bite misses a hero standing inside the edge of its ring");
                }
                body.Passive=true;Object.Destroy(body.gameObject);yield return Frames(2);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        /// <summary>Enemies walk straight at the hero, so one chasing him round a corner ends up pressed against the wall between them.</summary>
        [UnityTest] public IEnumerator AnEnemyDoesNotBiteThroughTheWallItIsPressedAgainst()
        {
            yield return Zone("Arena_Ground");Arena();var hero=Taren;Place(Sela,new Vector3(260,0,0));var failures=new List<string>();
            foreach(string id in new[]{"SentinelHusk","Ridgehound"})
            {
                Place(hero,new Vector3(200,0,1.8f));hero.Health.integrity=hero.Health.maximum;
                var wall=Wall(new Vector3(200,2.5f,1.05f),new Vector3(14,7,.3f));
                var body=Enemy(id,new Vector3(200,0,0));yield return new WaitForSecondsRealtime(.7f);
                float before=hero.Health.integrity;body.Passive=false;
                yield return new WaitForSecondsRealtime(2.5f);
                if(hero.Health.integrity<before)failures.Add($"a {id} bit a hero standing behind a solid wall: {before-hero.Health.integrity:0} integrity");
                body.Passive=true;Object.Destroy(wall);yield return Frames(2);Physics.SyncTransforms();
                PlaceBody(body,new Vector3(200,0,0));before=hero.Health.integrity;body.Passive=false;
                for(float end=Time.unscaledTime+4;hero.Health.integrity>=before&&Time.unscaledTime<end;)yield return null;
                if(hero.Health.integrity>=before)failures.Add($"fixture: with the wall gone a {id} still doesn't bite");
                body.Passive=true;Object.Destroy(body.gameObject);yield return Frames(2);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ every enemy's weakness and resistance
        static readonly string[] AllFourteen={"Ridgehound","Scrapmite","SentinelHusk","Burrower","ChoristerDart","ChoristerDrifter","Shellmine","Cantor","BellowsBelow","Gloomgrazer","MusterScav","MusterSergeant","MusterSkiff","HessaKorl"};
        static (Color color,float size,string text) NewestNumber()
        {
            var text=Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<TMP_Text>(true).Last(t=>t.name=="Damage");
            return (text.color,text.fontSize,text.text);
        }
        static bool Grey(Color c)=>Mathf.Abs(c.r-c.g)<.1f&&Mathf.Abs(c.g-c.b)<.1f&&c.r<.8f;
        [UnityTest] public IEnumerator EveryEnemyShowsItsWeaknessAndResistanceOnHit()
        {
            yield return Zone("Arena_Ground");Arena();var taren=Taren;Place(taren,new Vector3(260,0,0));Place(Sela,new Vector3(263,0,0));yield return Frames(2);
            // GAME_DESIGN 8 states these eight; chapter 2's come from the same rule with no table to check against.
            var design=new Dictionary<string,(DamageType weak,DamageType resist)>
            {
                ["Ridgehound"]=(DamageType.Plasma,DamageType.Kinetic),["Scrapmite"]=(DamageType.Pulse,DamageType.Beam),["SentinelHusk"]=(DamageType.Kinetic,DamageType.Plasma),
                ["Burrower"]=(DamageType.Beam,DamageType.Pulse),["ChoristerDart"]=(DamageType.Beam,DamageType.Pulse),["ChoristerDrifter"]=(DamageType.Pulse,DamageType.Beam),
                ["Shellmine"]=(DamageType.Kinetic,DamageType.Plasma),["Cantor"]=(DamageType.Plasma,DamageType.Kinetic),
            };
            var failures=new List<string>();
            foreach(string id in AllFourteen)
            {
                var enemy=Enemy(id,new Vector3(200,.1f,0));yield return Frames(3);
                var carrier=enemy.Health;var collar=enemy.GetComponent<CantorCollar>();var body=collar!=null?collar.Links[0]:carrier;
                carrier.breakThreshold=body.breakThreshold=1e9f;body.maximum=body.integrity=1000000;
                if(design.TryGetValue(id,out var row)&&(body.weakness!=row.weak||body.resistance!=row.resist))failures.Add($"a {id} is weak to {body.weakness} and resists {body.resistance}: the design says {row.weak} and {row.resist}");
                DamagePacket Hit(DamageType type)=>new DamagePacket{source=taren.Health,amount=100,type=type};
                float clean=body.Receive(Hit(Neutral(body)));var cleanNumber=NewestNumber();
                float weak=body.Receive(Hit(body.weakness));var weakNumber=NewestNumber();
                if(Mathf.Abs(weak-clean*1.5f)>1)failures.Add($"a {id}'s weakness deals {weak:0} where a clean hit deals {clean:0}: the rule is half as much again");
                if(!weakNumber.text.Contains("!")||weakNumber.color==cleanNumber.color)failures.Add($"a hit on a {id}'s weakness looks like any other: \"{weakNumber.text}\"");
                if(carrier.GetComponent<BossController>()!=null)
                {
                    var spot=enemy.GetComponentsInChildren<Hurtbox>().FirstOrDefault(h=>h.multiplier>1&&h.owner==body);
                    if(spot==null)failures.Add($"a {id} has no weak spot to find");
                    else
                    {
                        spot.Hit(Hit(Neutral(body)));var spotNumber=NewestNumber();
                        if(!spotNumber.text.Contains("!!")||spotNumber.color==weakNumber.color)failures.Add($"a hit on a {id}'s weak spot says nothing of it: \"{spotNumber.text}\"");
                    }
                }
                // Resistance last: a Sentinel's shield goes up on a resisted hit, and everything after it reads as resisted.
                float resisted=body.Receive(Hit(body.resistance));var resistedNumber=NewestNumber();
                if(Mathf.Abs(resisted-clean*.5f)>1)failures.Add($"a {id}'s resistance deals {resisted:0} where a clean hit deals {clean:0}: the rule is half");
                if(!Grey(resistedNumber.color)||resistedNumber.size>cleanNumber.size)failures.Add($"a resisted hit on a {id} doesn't read as resisted: {resistedNumber.color} at {resistedNumber.size}");
                Object.Destroy(enemy.gameObject);yield return Frames(2);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        [UnityTest] public IEnumerator AShotThatPassesBesideHessaMisses()
        {
            yield return Zone("Arena_Ground");Arena();var sela=Sela;Place(sela,new Vector3(260,0,0));Place(Taren,new Vector3(263,0,0));
            var enemy=Enemy("HessaKorl",new Vector3(200,.1f,0));enemy.transform.rotation=Quaternion.identity;yield return Frames(3);
            float before=enemy.Health.integrity;
            // Shoulder high, sixty centimetres clear of a body that is .8 m wide each side at its widest.
            foreach(float side in new[]{-1.6f,1.6f})Projectile.Fire(new Vector3(200+side,1.5f,-6),Vector3.forward,new DamagePacket{source=sela.Health,amount=10,type=DamageType.Beam},24);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(before,enemy.Health.integrity,.01f,"a needle that visibly flies past her shoulder hits for double, with the !! of a weak spot, on nothing but the air beside her");
            Projectile.Fire(new Vector3(200,1.5f,-6),Vector3.forward,new DamagePacket{source=sela.Health,amount=10,type=DamageType.Beam},24);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.Less(enemy.Health.integrity,before,"fixture: a shot straight at her lands");
        }

        // ------------------------------------------------------------------ every enemy's death
        static float Minimum(Transform visual,Vector3 plane,Vector3 normal,out float centroidHeight)
        {
            float value=float.PositiveInfinity;double sum=0;int count=0;
            foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh();skin.BakeMesh(mesh);
                var matrix=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
                foreach(var v in mesh.vertices){float height=Vector3.Dot(matrix.MultiplyPoint3x4(v)-plane,normal);value=Mathf.Min(value,height);sum+=height;count++;}
                Object.Destroy(mesh);
            }
            foreach(var mesh in visual.GetComponentsInChildren<MeshFilter>())
                if(mesh.sharedMesh!=null)foreach(var v in mesh.sharedMesh.vertices){float height=Vector3.Dot(mesh.transform.TransformPoint(v)-plane,normal);value=Mathf.Min(value,height);sum+=height;count++;}
            Assert.Greater(count,0,"fixture: the body has geometry");centroidHeight=(float)(sum/count);
            return value;
        }
        static GameObject Slope(float degrees,out Vector3 plane,out Vector3 normal)
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Plan corpse floor";
            floor.transform.SetPositionAndRotation(new Vector3(150,0,0),Quaternion.Euler(0,0,degrees));floor.transform.localScale=new Vector3(30,1,30);
            plane=floor.transform.TransformPoint(Vector3.up*.5f);normal=floor.transform.up;Physics.SyncTransforms();return floor;
        }
        static void Kill(Health body)=>body.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=body.maximum*100,type=DamageType.Pulse});
        [UnityTest] public IEnumerator AKilledGrazerLiesDownOnTheFloorItDiedOn()
        {
            yield return Zone("Arena_Flight");var failures=new List<string>();
            foreach(float slope in new[]{0f,10f,-10f})
            {
                var floor=Slope(slope,out var plane,out var normal);
                var enemy=Enemy("Gloomgrazer",plane);enemy.transform.rotation=Quaternion.Euler(0,slope==0?0:135,0);yield return new WaitForSecondsRealtime(.25f);
                var visual=enemy.GetComponent<DefeatPresentation>().visual;var standing=visual.rotation;
                Minimum(visual,plane,normal,out float upright);
                using var cues=new CueLog();
                Kill(enemy.Health);float start=GameTime.Now,worst=float.PositiveInfinity,terminal=0,centroid=0;
                while(GameTime.Now-start<1.05f){yield return null;float low=Minimum(visual,plane,normal,out centroid);worst=Mathf.Min(worst,low);terminal=low;}
                if(worst<-.03f)failures.Add($"a dying grazer on a {slope} degree slope sinks {-worst:0.00} m through the floor");
                if(terminal<-.03f||terminal>.05f)failures.Add($"a dead grazer on a {slope} degree slope lies {terminal:0.00} m off the floor");
                if(centroid>upright*.85f)failures.Add($"a dead grazer stays propped up on a {slope} degree slope: its body sits {centroid:0.00} m up, from {upright:0.00} standing");
                if(Quaternion.Angle(standing,visual.rotation)<20)failures.Add("a killed grazer doesn't go down: it barely changes its attitude");
                if(cues.Count("death_creature",enemy.transform)==0)failures.Add("a grazer dies without a creature's fall: "+cues.All);
                Object.Destroy(enemy.gameObject);Object.Destroy(floor);yield return Frames(2);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator TheMusterFallLikePeopleOnTheFloorTheyDiedOn()
        {
            yield return Zone("Arena_Flight");var failures=new List<string>();
            foreach(string id in new[]{"MusterScav","MusterSergeant"})foreach(float slope in new[]{0f,10f})
            {
                var floor=Slope(slope,out var plane,out var normal);
                var enemy=Enemy(id,plane);enemy.transform.rotation=Quaternion.Euler(0,slope==0?0:135,0);yield return new WaitForSecondsRealtime(.25f);
                var rig=enemy.GetComponentInChildren<Animator>();Assert.IsNotNull(rig,"fixture: "+id+" is rigged");Assert.IsTrue(rig.isHuman,"fixture: "+id+" stands on a human rig");
                var visual=enemy.GetComponent<DefeatPresentation>().visual;var root=enemy.transform.position;
                float Hips()=>Vector3.Dot(rig.GetBoneTransform(HumanBodyBones.Hips).position-plane,normal);
                float standing=Hips();
                using var cues=new CueLog();
                Kill(enemy.Health);yield return new WaitForSecondsRealtime(1.45f);
                if(enemy==null){failures.Add($"a {id} is gone before its corpse has settled");Object.Destroy(floor);continue;}
                float hips=Hips(),low=Minimum(visual,plane,normal,out _);
                if(hips>standing-.3f||hips>.6f)failures.Add($"a killed {id} doesn't fall: its hips are {hips:0.00} m up, from {standing:0.00}");
                if(low<-.04f||low>.06f)failures.Add($"a dead {id} on a {slope} degree slope lies {low:0.00} m off the floor");
                if(Vector3.Distance(root,enemy.transform.position)>.001f)failures.Add($"a dead {id} moved its gameplay root");
                // They are people. A machine's crunch belongs to the Husks and the mines.
                if(cues.Count("death_mech",enemy.transform)>0)failures.Add($"a {id} is a person, and dies with a machine's crunch: "+cues.All);
                if(cues.Count("death_creature",enemy.transform)==0)failures.Add($"a {id} falls in silence: "+cues.All);
                Object.Destroy(enemy.gameObject);Object.Destroy(floor);yield return Frames(2);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator TheBodiesNobodyHasWatchedLeaveThroughACleanupNotAPop()
        {
            yield return Zone("Arena_Flight");var failures=new List<string>();
            foreach(string id in new[]{"Gloomgrazer","MusterScav","MusterSergeant","MusterSkiff","BellowsBelow"})
            {
                var enemy=Enemy(id,new Vector3(200,1,0));yield return Frames(2);
                var root=enemy.transform;float full=root.localScale.x,lastScale=1;bool partial=false;
                Kill(enemy.Health);yield return new WaitForSecondsRealtime(.35f);
                if(enemy==null){failures.Add($"a killed {id} is gone in under .35 s: no death to see");continue;}
                if(!enemy.GetComponentsInChildren<Renderer>().Any(r=>r.enabled))failures.Add($"a killed {id}'s body disappears at once");
                if(enemy.GetComponent<CharacterController>().enabled)failures.Add($"a killed {id} still blocks the way");
                for(float end=Time.unscaledTime+6;enemy!=null&&Time.unscaledTime<end;)
                {float scale=root.localScale.x/full;if(scale>.15f&&scale<.85f)partial=true;lastScale=scale;yield return null;}
                if(enemy!=null){failures.Add($"a {id}'s corpse is never cleaned up");Object.Destroy(enemy.gameObject);}
                else if(!partial||lastScale>.3f)failures.Add($"a {id}'s corpse pops out of existence from {lastScale:0.00} of its size");
                yield return Frames(2);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ grazers and a real relay
        [UnityTest] public IEnumerator ARelitGorgeRelayDrawsItsRingOnTheGroundAndTurnsAGrazerBack()
        {
            yield return Zone("LanternGorge");
            foreach(var volume in All<EncounterVolume>())volume.enabled=false;
            var lantern=All<RelayLantern>().First(r=>r.flag=="gorge.relay1");var hub=lantern.transform.position;
            Flag(lantern.flag);yield return Frames(3);
            Assert.IsTrue(lantern.Lit,"fixture: the relay burns");
            var ring=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==lantern);
            Assert.IsNotNull(ring,"a relit relay in the gorge shows nothing on the ground: nobody can see where its ward ends");
            Assert.AreEqual(Telegraph.Kind.Ring,ring.Shape,"the ward's marking isn't a ring");
            Assert.AreEqual(lantern.radius,ring.Radius,.01f,"the ring on the ground isn't the ward's real reach");
            var verts=ring.GetComponent<MeshFilter>().sharedMesh.vertices;int onGround=0;
            foreach(var v in verts)if(Mathf.Abs(CombatCover.Ground(new Vector3(v.x,ring.Origin.y+3,v.z)).y-v.y)<=.4f)onGround++;
            Assert.GreaterOrEqual(onGround,verts.Length*.8f,$"only {onGround} of the ward ring's {verts.Length} points lie on the gorge floor: the ring floats or is buried");
            // A grazer inside the light with the hero, in the open toward the middle of the gorge.
            var inward=(new Vector3(0,hub.y,hub.z)-hub).normalized;
            var hero=Taren;var heroAt=CombatCover.Ground(hub+inward*3.5f+Vector3.up*6);Place(hero,heroAt+Vector3.up*.05f);
            Place(Sela,CombatCover.Ground(hub+inward*3.5f+Vector3.forward*2+Vector3.up*6)+Vector3.up*.05f);
            var grazer=Enemy("Gloomgrazer",CombatCover.Ground(hub+inward*7+Vector3.up*6)+Vector3.up*.05f,false);
            yield return Frames(2);
            float start=Flat(grazer.transform.position,hub),before=hero.Health.integrity;bool told=false;
            yield return new WaitForSecondsRealtime(1.2f);
            float early=Flat(grazer.transform.position,hub);
            Assert.Greater(early,start+.6f,$"a grazer in a relay's light doesn't back out: {start:0.0} m from the relay, then {early:0.0}");
            Assert.LessOrEqual((early-start)/1.2f,2,$"a grazer backs out of the light at {(early-start)/1.2f:0.0} m/s: its full stride is 3.7, and the light should slow it");
            for(float end=Time.unscaledTime+3;Time.unscaledTime<end;){told|=grazer.Telegraphing&&grazer.AttackMark!=null&&grazer.AttackMark.Contains(hero.transform.position);yield return null;}
            Assert.AreEqual(before,hero.Health.integrity,.01f,"a grazer bit a hero standing well inside a lit relay's ring");
            Assert.IsFalse(told,"a grazer winds up at a hero standing well inside a lit relay's ring");
            grazer.Passive=true;
        }

        // ------------------------------------------------------------------ the boss table: title card and bar
        IEnumerator BossCard(string id,float y)
        {
            var lead=PartyController.Current.Active;Place(lead,new Vector3(200,y,0));Place(Other(lead),new Vector3(260,y,0));
            lead.Health.InvulnerableUntil=GameTime.Now+30;
            var enemy=Enemy(id,new Vector3(200,y+.1f,14));yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            enemy.Passive=false;yield return new WaitForSecondsRealtime(.6f);
            var card=HudText("BossTitleCard");var cardLine=HudText("BossTitleEpithet");var name=HudText("BossName");var epithet=HudText("BossEpithet");
            Assert.IsNotNull(card,"the HUD has no title card");
            Assert.AreEqual(boss.DisplayName,card.text,id+"'s fight opens with no title card");Assert.Greater(card.alpha,.5f,id+"'s title card isn't showing as its fight begins");
            Assert.IsFalse(string.IsNullOrWhiteSpace(boss.Epithet),id+" has no epithet to show");
            Assert.AreEqual(boss.Epithet,cardLine.text,id+"'s title card has no epithet under its name");
            Assert.IsTrue(name.isActiveAndEnabled,id+" has no boss bar");Assert.AreEqual(boss.DisplayName,name.text,id+"'s bar doesn't name it");
            Assert.IsTrue(string.IsNullOrEmpty(epithet.text),id+"'s epithet shows twice, on its title card and on the bar under it");
            var images=Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<UnityEngine.UI.Image>(true);
            var back=images.First(i=>i.name=="BossIntegrityBack");float width=back.rectTransform.rect.width;
            var notches=images.Where(i=>i.name.StartsWith("BossNotch")&&i.gameObject.activeInHierarchy).ToArray();var phases=boss.Thresholds;
            // The Cantor's bar is its head and its links together: a notch where each link goes, the last where the head opens, and the head's phases inside its share.
            var collar=enemy.GetComponent<CantorCollar>();
            if(collar!=null)
            {
                float whole=enemy.Health.maximum+collar.Links.Sum(l=>l.maximum),share=enemy.Health.maximum/whole,left=whole;
                phases=collar.Links.Select(l=>{left-=l.maximum;return left/whole;}).ToArray().Concat(phases.Select(p=>p*share)).ToArray();
            }
            Assert.AreEqual(phases.Length,notches.Length,id+"'s bar doesn't notch each of its phases");
            for(int i=0;i<phases.Length;i++)Assert.AreEqual(phases[i],notches[i].rectTransform.anchoredPosition.x/width,.02f,id+"'s notch doesn't sit where its phase really turns");
            // The card clears the fight once it has been read.
            yield return new WaitForSecondsRealtime(2.9f);
            Assert.Less(card.alpha,.1f,id+"'s title card is still across the screen more than three seconds into the fight");
            // The bar takes the epithet up when the card has gone.
            Assert.AreEqual(boss.Epithet,epithet.text,id+"'s bar has no epithet");
            Assert.LessOrEqual(name.GetPreferredValues(name.text).x+epithet.GetPreferredValues(epithet.text).x,width,id+"'s name and epithet run into each other on the bar");
            enemy.Passive=true;Object.Destroy(enemy.gameObject);yield return Frames(3);
            foreach(var left in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(left.gameObject);
            foreach(var shot in Telegraph.Live.ToArray())if(shot!=null)Object.Destroy(shot.gameObject);
            yield return Frames(2);
        }
        [UnityTest] public IEnumerator EveryBossOpensOnItsTitleCardAndABarWithItsNameEpithetAndNotches()
        {
            yield return Zone("Arena_Ground");Arena();
            foreach(string id in new[]{"Burrower","BellowsBelow","HessaKorl"})yield return BossCard(id,0);
            yield return Zone("Arena_Flight");
            yield return BossCard("Cantor",1);
        }

        // ------------------------------------------------------------------ the boss table: every special is marked and dodgeable
        static readonly Dictionary<string,(string move,Vector3 stand)[]> Specials=new()
        {
            ["Burrower"]=new[]{("DrillCharge",new Vector3(0,0,8)),("SpoilToss",new Vector3(0,0,9)),("TailSweep",new Vector3(0,0,-3.6f)),("Burrow",new Vector3(4,0,-6)),("SeismicRings",new Vector3(0,0,9))},
            ["BellowsBelow"]=new[]{("Breath",new Vector3(0,0,7)),("RibStar",new Vector3(0,0,7)),("Bubbles",new Vector3(6,0,-3))},
            ["Cantor"]=new[]{("Choir",new Vector3(0,0,5)),("CoilLash",new Vector3(0,0,10))},
            ["HessaKorl"]=new[]{("HookLance",new Vector3(0,0,3)),("Advance",new Vector3(0,0,12)),("GrappleChain",new Vector3(0,0,9)),("Sidearm",new Vector3(0,0,10)),("SweepingCut",new Vector3(0,0,3)),("Lampbreaker",new Vector3(0,0,6))},
        };
        static Vector3 JustOutside(Telegraph mark,float y)
        {
            var across=Vector3.Cross(Vector3.up,mark.Forward);
            var at=mark.Shape==Telegraph.Kind.Line?mark.Origin+mark.Forward*(mark.Length*.5f)+across*(mark.Width*.5f+.8f):mark.Origin+mark.Forward*(mark.Radius+.8f);
            at.y=y;return at;
        }
        static Vector3 Inside(Telegraph mark,float y)
        {
            var at=mark.Shape==Telegraph.Kind.Line?mark.Origin+mark.Forward*Mathf.Min(mark.Length*.5f,6):mark.Origin+mark.Forward*(mark.Radius*.5f);
            at.y=y;return at;
        }
        /// <summary>Each special, with the lead in its marking and the partner just outside it. Unrolled: what lands was marked at least .3 s
        /// and lands inside the marking. Rolled: a roll timed to the tell takes nothing. A released marking destroys itself and still knows its shape.</summary>
        IEnumerator RunSpecials(string id,bool roll)
        {
            bool flight=id=="Cantor";float y=flight?1:.1f;var at=new Vector3(200,y,0);
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Arena();
            var lead=PartyController.Current.Active;var partner=Other(lead);var failures=new List<string>();
            var enemy=Enemy(id,at);yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            foreach(var (move,stand) in Specials[id])
            {
                for(float end=Time.unscaledTime+7;(boss.Busy||boss.Health.Broken||Time.timeScale<1)&&Time.unscaledTime<end;)yield return null;
                PlaceBody(enemy,at);enemy.transform.rotation=Quaternion.identity;
                Place(lead,at+stand);Place(partner,new Vector3(260,y,0));Fresh(lead);Fresh(partner);lead.thrust=100;yield return Frames(2);
                float before=lead.Health.integrity;int struck=0;var seen=new List<Telegraph>();
                // What the last move is still fading out isn't this one's: Perform doesn't wait out a rest as the fight does, and a released marking lingers .32 s.
                var earlier=new HashSet<Telegraph>(Telegraph.Live);
                void Collect(){foreach(var mark in Telegraph.Live)if(mark!=null&&mark.owner==boss&&!earlier.Contains(mark)&&!seen.Contains(mark))seen.Add(mark);}
                System.Action<Health,DamagePacket,float> witness=(body,packet,amount)=>
                {
                    if(packet.source!=boss.Health||amount<=0)return;
                    if(body==lead.Health)struck++;
                    Collect();var where=body.transform.position;
                    if(!seen.Any(mark=>mark.Contains(where)))failures.Add($"{id}'s {move} struck a hero standing outside every marking it laid");
                    else if(!seen.Any(mark=>mark.Contains(where)&&mark.Elapsed>=.3f))failures.Add($"{id}'s {move} struck inside a marking less than .3 s old: no time to read it");
                };
                lead.Health.Damaged+=witness;partner.Health.Damaged+=witness;
                Assert.IsTrue(boss.Perform(move),"fixture: "+move+" would not start");
                bool placed=false;
                // The Sidearm's move ends as its last shot leaves, before its first has reached a hero 14 m off: the witness stays until what the move fired has landed.
                for(float end=Time.unscaledTime+12;(boss.Busy||Projectile.Live.Any(s=>s!=null&&s.Threatens(true)))&&Time.unscaledTime<end;)
                {
                    Collect();
                    if(!placed&&seen.Count>0)
                    {
                        placed=true;var outside=JustOutside(seen[0],y);
                        if(!seen.Any(mark=>mark.Contains(outside)))Place(partner,outside);
                        // Spokes and bubbles land at random or come to him: he steps into one. The other moves are aimed at where he stands.
                        if((move=="RibStar"||move=="Bubbles")&&!seen.Any(mark=>mark.Contains(lead.transform.position)))Place(lead,Inside(seen[0],y));
                    }
                    lead.motor.Move(Vector2.zero,false,false);
                    // Left: the partner stands on the +x side of a lane, and a body in the way would stop the roll inside it.
                    // Rings are the exception: a band keeps coming at 8 m/s, so a roll that carries him away only puts it off until the roll ends, and the
                    // move's own note says dodge through. He waits for a band, not the slam that stops 6 m short of him, and rolls in at it.
                    if(roll&&boss.Telegraphing&&boss.TelegraphRemaining<.1f*Time.timeScale)
                    {
                        if(move!="SeismicRings")lead.Dodge(Vector3.left);
                        else if(seen.Count>1)lead.Dodge(enemy.transform.position-lead.transform.position);
                    }
                    yield return null;
                }
                lead.Health.Damaged-=witness;partner.Health.Damaged-=witness;
                if(seen.Count==0)failures.Add($"{id}'s {move} marks nothing on the ground");
                if(!roll&&struck==0)failures.Add($"{id}'s {move} never struck a hero standing in its marking");
                if(roll&&lead.Health.integrity<before)failures.Add($"{id}'s {move} hit a hero who rolled as its tell ran out: {before-lead.Health.integrity:0} integrity");
            }
            // The song's shots are bullets with gaps to fly through (D198), so they get its marked inhale instead of paint of their own.
            if(id=="Cantor"&&!roll)
            {
                for(float end=Time.unscaledTime+7;(boss.Busy||boss.Health.Broken)&&Time.unscaledTime<end;)yield return null;
                PlaceBody(enemy,at);Place(lead,at+new Vector3(0,0,8));Fresh(lead);yield return Frames(2);
                float markedAt=-1,firedAt=-1;Assert.IsTrue(boss.Perform("SongSpiral"),"fixture: the song");
                for(float end=Time.unscaledTime+5;boss.Busy&&Time.unscaledTime<end;)
                {
                    if(markedAt<0&&Telegraph.Live.Any(t=>t!=null&&t.owner==boss))markedAt=Time.time;
                    if(firedAt<0&&Projectile.Live.Any(s=>s!=null&&s.Threatens(true)))firedAt=Time.time;
                    yield return null;
                }
                if(markedAt<0)failures.Add("the Cantor's song spiral has no marked inhale");
                else if(firedAt<0)failures.Add("the Cantor's song spiral fired nothing");
                else if(firedAt-markedAt<.5f)failures.Add($"the Cantor's song spiral fires {firedAt-markedAt:0.00} s after its marking: too short to read");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator WhatABossLandsWasMarkedAndLandsInsideIt([Values("Burrower","BellowsBelow","Cantor","HessaKorl")] string id)=>RunSpecials(id,false);
        [UnityTest] public IEnumerator ARollTimedToTheTellClearsEverySpecial([Values("Burrower","BellowsBelow","Cantor","HessaKorl")] string id)=>RunSpecials(id,true);
        [UnityTest] public IEnumerator TheBellowsChamberBlastsOnlyTheHalfItMarksAndItsCrossingStaysInItsLane()
        {
            yield return Zone("Hushwell");
            foreach(var volume in All<EncounterVolume>())volume.enabled=false;
            var lead=PartyController.Current.Active;var partner=Other(lead);Fresh(lead);Fresh(partner);
            float floor=HushwellLayout.OnFloor(HushwellLayout.Bellows).y;
            var enemy=Enemy("BellowsBelow",HushwellLayout.OnFloor(HushwellLayout.Bellows));yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            Assert.IsTrue(PressureOrgan.AnyPumping,"fixture: the organs pump");
            // Fourteen metres either side of the dome, outside its ring: one hero in the half its organ marks, one in the other.
            var mid=PressureOrgan.Middle;
            Assert.IsTrue(boss.Perform("Breath"),"fixture: the breath");yield return null;
            var half=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==boss&&t.Shape==Telegraph.Kind.Cone);
            Assert.IsNotNull(half,"an organ's blast marks nothing of the half it strikes");
            var marked=mid+half.Forward*12;marked.y=floor;var other=mid-half.Forward*12;other.y=floor;
            Place(lead,marked);Place(partner,other);
            float inHalf=lead.Health.integrity,elsewhere=partner.Health.integrity;
            for(float end=Time.unscaledTime+6;boss.Busy&&Time.unscaledTime<end;)yield return null;
            Assert.Less(lead.Health.integrity,inHalf,"the marked half of the chamber wasn't blasted");
            Assert.AreEqual(elsewhere,partner.Health.integrity,.01f,"the blast reached the half it didn't mark");
            // The last phase's crossing: nothing lands off its lane, and it lands on it.
            enemy.Health.integrity=enemy.Health.maximum*.25f;yield return Frames(2);Assert.AreEqual(3,boss.HealthPhase,"fixture: the last phase");
            Fresh(lead);Fresh(partner);
            Assert.IsTrue(boss.Perform("Crossing"),"fixture: the crossing");yield return null;
            var lane=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==boss&&!t.Released&&t.Shape==Telegraph.Kind.Line);
            Assert.IsNotNull(lane,"the Bellows' crossing marks no lane");
            Place(lead,Inside(lane,floor));Place(partner,JustOutside(lane,floor));yield return null;
            Assert.IsTrue(lane.Contains(lead.transform.position),"fixture: he stands in the lane");
            float onLane=lead.Health.integrity,offLane=partner.Health.integrity;
            // Until the breath that follows it marks anything, its ring or with an organ pumping its half: that one is allowed to reach him.
            for(float end=Time.unscaledTime+3.2f;Time.unscaledTime<end&&!Telegraph.Live.Any(t=>t!=null&&t.owner==boss&&!t.Released&&t.Shape!=Telegraph.Kind.Line);)yield return null;
            Assert.Less(lead.Health.integrity,onLane,"the crossing passed through a hero standing in its lane");
            Assert.AreEqual(offLane,partner.Health.integrity,.01f,"the crossing struck a hero just outside its lane");
        }
        [UnityTest] public IEnumerator ABubbleVolleyDoesNotChainAStandingHeroToAQuarterOfHisIntegrity()
        {
            yield return Zone("Arena_Ground");Arena();Sync(5);
            var lead=PartyController.Current.Active;Place(lead,new Vector3(200,.1f,7));Place(Other(lead),new Vector3(260,.1f,0));
            var enemy=Enemy("BellowsBelow",new Vector3(200,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            enemy.Health.integrity=enemy.Health.maximum*.45f;yield return Frames(2);Assert.AreEqual(2,boss.HealthPhase,"fixture: phase 2, where the bubbles begin");
            lead.Health.integrity=lead.Health.maximum;float before=lead.Health.integrity;int struck=0;lead.Health.Damaged+=(_,__,___)=>struck++;
            Assert.IsTrue(boss.Perform("Bubbles"),"fixture: the bubbles");
            for(float end=Time.unscaledTime+8;boss.Busy&&Time.unscaledTime<end;)yield return null;
            Assert.Greater(struck,0,"fixture: the bubbles reached him");
            float share=(before-lead.Health.integrity)/lead.Health.maximum;
            Assert.LessOrEqual(share,.25f,$"one volley took {struck} hits and {share:P0} of a Sync 5 hero: bubbles burst about .12 s apart with a .25 s stagger, so the first one holds him for the rest, with no way to roll");
        }

        // ------------------------------------------------------------------ the boss table: openings
        [UnityTest] public IEnumerator EveryPhaseOfEveryBossLeavesAWindowToPunish([Values("Burrower","BellowsBelow","Cantor","HessaKorl")] string id)
        {
            bool flight=id=="Cantor";float y=flight?1:.1f;var at=new Vector3(200,y,0);
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Arena();
            var lead=PartyController.Current.Active;Place(Other(lead),new Vector3(260,y,0));var failures=new List<string>();
            var enemy=Enemy(id,at);yield return Frames(3);var boss=enemy.GetComponent<BossController>();var body=enemy.Health;var collar=enemy.GetComponent<CantorCollar>();
            var moves=id switch
            {
                "Burrower"=>new[]{("DrillCharge",new Vector3(0,0,9)),("Burrow",new Vector3(4,0,-6)),("Burrow",new Vector3(4,0,-6))},
                "BellowsBelow"=>new[]{("Breath",new Vector3(0,0,7)),("Breath",new Vector3(0,0,7)),("Crossing",new Vector3(0,0,7))},
                "Cantor"=>new[]{("Choir",new Vector3(0,0,5)),("SongSpiral",new Vector3(0,0,8)),("CoilLash",new Vector3(0,0,10))},
                _=>new[]{("GrappleChain",new Vector3(0,0,9)),("GrappleChain",new Vector3(0,0,9)),("GrappleChain",new Vector3(0,0,9))},
            };
            // A wall to wedge the first charge in: the player's bait, and every other move doesn't care.
            if(id=="Burrower")Wall(new Vector3(200,2,14),new Vector3(30,6,1));
            var turns=boss.Thresholds;
            for(int phase=1;phase<=3;phase++)
            {
                var (move,stand)=moves[phase-1];
                for(float end=Time.unscaledTime+7;(boss.Busy||body.Broken)&&Time.unscaledTime<end;)yield return null;
                // Its head can't be hurt until the collar is off: past phase 1 the real fight has cut it.
                if(phase==2&&collar!=null)foreach(var link in collar.Links)link.Receive(new DamagePacket{source=lead.Health,amount=link.maximum*100,type=DamageType.Beam});
                PlaceBody(enemy,at);enemy.transform.rotation=Quaternion.identity;Place(lead,at+stand);Fresh(lead);
                body.integrity=body.maximum*(phase==1?1:phase==2?(turns[0]+turns[1])*.5f:turns[1]*.6f);yield return Frames(2);
                if(boss.HealthPhase!=phase){failures.Add($"fixture: {id} isn't in phase {phase}");continue;}
                if(!boss.Perform(move)){failures.Add($"fixture: {move} would not start in phase {phase}");continue;}
                // Her chain is the dodged one: he steps out of its lane while it is told.
                if(id=="HessaKorl"){yield return null;Place(lead,at+new Vector3(6,0,9));}
                for(float end=Time.unscaledTime+8;!body.Broken&&boss.Busy&&Time.unscaledTime<end;){lead.motor.Move(Vector2.zero,false,false);yield return null;}
                if(!body.Broken){failures.Add($"{id}'s {move} in phase {phase} ends with no opening: nothing to punish");continue;}
                float window=body.BrokenUntil-Time.time;
                if(window<.9f)failures.Add($"{id}'s opening after {move} in phase {phase} lasts {window:0.0} s: too short to get to it and swing");
                var struck=collar!=null&&collar.Remaining>0?collar.Links[0]:body;
                float landed=struck.Receive(new DamagePacket{source=lead.Health,amount=100,type=Neutral(struck)});
                if(landed<149)failures.Add($"a hit in {id}'s opening after {move} in phase {phase} deals {landed:0} of 100: an opening is half as much again");
                if(Telegraph.Live.Any(t=>t!=null&&t.owner==boss&&!t.Released&&!t.Charged))failures.Add($"{id} is marking its next attack during the opening after {move} in phase {phase}");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator HessaOffersHerOpeningInEveryPhaseEvenAtLanceRange()
        {
            yield return Zone("Arena_Ground");Arena();
            var lead=PartyController.Current.Active;Place(lead,new Vector3(200,.1f,3));Place(Other(lead),new Vector3(260,.1f,0));
            var enemy=Enemy("HessaKorl",new Vector3(200,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            const System.Reflection.BindingFlags Private=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var type=typeof(BossController);var choose=type.GetMethod("ChooseHessa",Private);var count=type.GetField("moveCount",Private);var last=type.GetField("lastMove",Private);var usedField=type.GetField("lastUsed",Private);
            Assert.IsNotNull(choose,"fixture: how she picks");Assert.IsNotNull(count,"fixture: how she picks");Assert.IsNotNull(last,"fixture: how she picks");Assert.IsNotNull(usedField,"fixture: how she picks");
            var used=(Dictionary<string,int>)usedField.GetValue(boss);var failures=new List<string>();
            // Six to eight moves a phase in the recorded fights (Builds/quality/QA/v2c/s15): eight is generous.
            const int Fights=300,Picks=8;
            foreach(var (phase,ratio) in new[]{(1,.9f),(2,.5f),(3,.25f)})
            {
                enemy.Health.integrity=enemy.Health.maximum*ratio;yield return Frames(2);Assert.AreEqual(phase,boss.Phase,"fixture: the phase");
                int open=0;
                for(int fight=0;fight<Fights;fight++)
                {
                    used.Clear();count.SetValue(boss,0);last.SetValue(boss,null);bool chain=false;
                    for(int n=1;n<=Picks;n++)
                    {
                        var pick=(System.ValueTuple<string,IEnumerator>)choose.Invoke(boss,new object[]{lead});
                        chain|=pick.Item1=="GrappleChain";
                        count.SetValue(boss,n);last.SetValue(boss,pick.Item1);used[pick.Item1]=n;
                    }
                    if(chain)open++;
                }
                float share=open/(float)Fights;
                TestContext.WriteLine($"HESSA_CHAIN phase={phase} share={share:P0}");
                if(share<.9f)failures.Add($"to a hero at lance range she throws the chain in only {share:P0} of phase {phase}s, and a dodged chain is her one deliberate opening: most of the time the phase offers no punish");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ the boss table: phase shifts
        [UnityTest] public IEnumerator EveryThresholdBracesShovesAndBringsTheMoveItPromised([Values("Burrower","BellowsBelow","Cantor","HessaKorl")] string id)
        {
            bool flight=id=="Cantor";float y=flight?1:.1f;var at=new Vector3(200,y,0);
            var promised=id switch{"Burrower"=>new[]{"Burrow","SeismicRings"},"BellowsBelow"=>new[]{"Bubbles","Crossing"},"Cantor"=>new[]{"SongSpiral","CoilLash"},_=>new[]{"SweepingCut","Lampbreaker"}};
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Arena();
            var lead=PartyController.Current.Active;var partner=Other(lead);Place(partner,new Vector3(260,y,0));Place(lead,at+Vector3.back*(flight?6:4));
            lead.Health.InvulnerableUntil=GameTime.Now+60;partner.Health.InvulnerableUntil=GameTime.Now+60;
            var enemy=Enemy(id,at);yield return Frames(3);var boss=enemy.GetComponent<BossController>();var body=enemy.Health;
            var collar=enemy.GetComponent<CantorCollar>();
            if(collar!=null)foreach(var link in collar.Links)link.Receive(new DamagePacket{source=lead.Health,amount=link.maximum*100,type=DamageType.Beam});
            body.Receive(new DamagePacket{source=lead.Health,amount=1,type=Neutral(body)});
            Assert.IsTrue(boss.Engaged,"fixture: the fight is on");
            var started=new List<string>();moved=(who,move)=>{if(who==boss)started.Add(move);};BossController.MoveStarted+=moved;
            enemy.Passive=false;var turns=boss.Thresholds;
            for(int i=0;i<turns.Length;i++)
            {
                for(float end=Time.unscaledTime+9;(boss.Busy||body.Broken)&&Time.unscaledTime<end;)yield return null;
                Place(lead,enemy.transform.position+Vector3.back*(flight?6:4));yield return null;
                var from=lead.transform.position;started.Clear();
                body.integrity=body.maximum*(turns[i]-.02f);
                for(float end=Time.unscaledTime+4;!boss.Shifting&&Time.unscaledTime<end;)yield return null;
                Assert.IsTrue(boss.Shifting,$"{id} crosses threshold {i+1} and nothing shows it changing");
                yield return Frames(2);// The HUD reads the boss once a frame, in whichever order.
                float dealt=body.Receive(new DamagePacket{source=lead.Health,amount=500,type=Neutral(body)});
                Assert.AreEqual(0,dealt,.01f,$"{id} can be hurt through its brace");
                StringAssert.Contains("BRACE",HudText("BossState").text,$"{id}'s bar doesn't say to brace as it shifts");
                Assert.IsTrue(Telegraph.Live.Any(t=>t!=null&&t.owner==boss&&!t.Released&&t.Shape==Telegraph.Kind.Circle),$"{id}'s shockwave comes with no ring on the ground");
                float gap=Flat(from,enemy.transform.position);
                for(float end=Time.unscaledTime+3;boss.Shifting&&Time.unscaledTime<end;)yield return null;
                Assert.IsFalse(boss.Shifting,$"{id}'s shift never ends");
                Assert.Greater(Flat(lead.transform.position,enemy.transform.position),gap+1.5f,$"{id}'s shift didn't push a hero standing in its ring back");
                for(float end=Time.unscaledTime+4;started.Count<2&&Time.unscaledTime<end;)yield return null;
                Assert.AreEqual("PhaseShift",started.Count>0?started[0]:"none","fixture: the shift is logged as a move");
                Assert.AreEqual(promised[i],started.Count>1?started[1]:"none",$"after {id}'s shift the next move isn't the new one its phase brings");
            }
            enemy.Passive=true;
        }

        // ------------------------------------------------------------------ the boss table: duration
        const float FlatOutSeconds=20;
        float flatOutRate;
        /// <summary>Flat out, the boss never moving, both heroes attacking and every skill as soon as it's ready: the upper bound on how fast the party can go. With free Charge only the cooldowns hold them back.</summary>
        IEnumerator FlatOut(string id,int sync,bool freeCharge)
        {
            bool flight=id=="Cantor";float y=flight?1:.1f;var at=new Vector3(200,y,0);
            DevLoadout.Apply("moon");// Refined parts, the best the road has by then.
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Arena();Sync(sync);
            var taren=Lead("Taren");var sela=Other(taren);
            var enemy=Enemy(id,at);yield return Frames(3);var body=enemy.Health;
            var collar=enemy.GetComponent<CantorCollar>();
            if(collar!=null)foreach(var link in collar.Links)link.Receive(new DamagePacket{source=taren.Health,amount=link.maximum*100,type=DamageType.Beam});
            body.maximum=body.integrity=10000000;
            float reach=enemy.GetComponent<CharacterController>().radius+.9f;
            var tarenAt=flight?at+new Vector3(-2,0,-6):at+Vector3.back*reach;var selaAt=flight?at+new Vector3(2,0,-6):at+Vector3.back*8+Vector3.right*2;
            Place(taren,tarenAt);Place(sela,selaAt);taren.target=sela.target=body;taren.charge=sela.charge=100;yield return null;
            float landed=0;body.Damaged+=(_,packet,amount)=>{if(packet.source==taren.Health||packet.source==sela.Health)landed+=amount;};
            bool Act(CombatActor hero)
            {
                if(!hero.CanAct)return false;
                for(int slot=0;slot<SkillBook.Slots;slot++)
                {
                    var skill=SkillBook.Equipped(hero.character,slot);
                    if(skill==null||hero.cooldowns[slot]>0||hero.charge<SkillBook.Cost(hero.character,skill))continue;
                    if(hero.Skill(slot))return true;
                }
                return hero.Attack();
            }
            log.Clear();
            for(float end=Time.unscaledTime+FlatOutSeconds;Time.unscaledTime<end;)
            {
                foreach(var hero in new[]{taren,sela})
                {
                    hero.motor.Move(Vector2.zero,false,false);
                    var home=hero==taren?tarenAt:selaAt;
                    if(hero.CanAct&&Flat(hero.transform.position,home)>.6f)Place(hero,home);
                    hero.target=body;if(freeCharge)hero.charge=100;Act(hero);
                }
                yield return null;
            }
            Assert.Greater(landed,0,"fixture: the party is hitting it");
            flatOutRate=landed/FlatOutSeconds;
            ClearEffects();enemy.Passive=true;Object.Destroy(enemy.gameObject);yield return Frames(3);
        }
        /// <summary>What the replay driver really got of flat out, BOSS_DURATION against BOSS_RATE on the v2 chains (300 of 720, 200 to 237 of 533, 382 of 1334, 310 to 465 of 1037), the lower where it ranged.</summary>
        static float DriverShare(string id)=>id switch{"Burrower"=>.42f,"BellowsBelow"=>.40f,"Cantor"=>.29f,_=>.30f};
        /// <summary>The plan's bar is 2 to 5 minutes, and tools/boss_durations.py reads it off a chain's real fights. This is the guard between chains, with nothing assumed:
        /// the flat-out rate on today's build, at the share of it the driver was measured to get, has to land inside the bar. Nathan's own time is the real test (TEST_PLAN section 6).</summary>
        [UnityTest] public IEnumerator ABossLastsTwoToFiveMinutesAtItsExpectedSync([Values("Burrower","BellowsBelow","Cantor","HessaKorl")] string id)
        {
            int sync=id=="Burrower"?4:id=="BellowsBelow"?5:id=="Cantor"?7:9;
            yield return FlatOut(id,sync,false);
            float integrity=GameCatalog.Find<EnemyDef>(id).integrity,rate=flatOutRate,share=DriverShare(id),seconds=integrity/(rate*share);
            TestContext.WriteLine($"BOSS_RATE {id} sync={sync} rate={rate:0}/s flatOut={integrity/rate:0}s atDriverShare={seconds:0}s");
            Assert.GreaterOrEqual(integrity,rate*share*120,$"{id} at Sync {sync} takes {rate:0} integrity a second flat out and the replay party gets {share:P0} of that: {integrity:0} integrity lasts {seconds:0} s, and the bar is two minutes");
            // The old ceiling stays: a person who spends most of the fight dodging is who the five minutes are for, and the driver's share is no measure of them.
            Assert.LessOrEqual(integrity/(rate*.25f),300,$"{id} at Sync {sync} takes {rate:0} integrity a second flat out, so {integrity/(rate*.25f):0} s at a quarter of that: the bar is five minutes");
        }

        // ------------------------------------------------------------------ the boss table: retry
        [UnityTest] public IEnumerator ALostBossFightComesBackAtItsDoorWholeAndShorter([Values("Burrower","BellowsBelow","Cantor","HessaKorl")] string id)
        {
            var (zone,doorId)=id switch{"Burrower"=>("Sorrel_Ridges","Sorrel_Burrower"),"BellowsBelow"=>("Hushwell","Hushwell_Bellows"),"Cantor"=>("Gullet_Tunnel","Gullet_Cantor"),_=>("SunkenRelay","Relay_Hessa")};
            if(id=="HessaKorl")Flag("relay.hessaConfronted");
            yield return Zone(zone,false);
            EncounterVolume Door()=>All<EncounterVolume>().Single(e=>e.encounterId==doorId);
            var door=Door();string checkpoint=door.checkpointSpawn;Assert.IsFalse(string.IsNullOrEmpty(checkpoint),"fixture: "+doorId+" is a checkpoint");
            door.Begin();yield return null;
            foreach(var brain in All<EnemyBrain>())brain.Passive=true;
            yield return Frames(2);
            var first=All<EnemyBrain>().FirstOrDefault(e=>e.definition.id==id);Assert.IsNotNull(first,"fixture: "+id+" came out");
            first.Health.integrity=first.Health.maximum*.5f;
            Assert.AreEqual(checkpoint,GameServices.Current.Saves.Load("autosave").spawn,"walking into the arena didn't save at its door");
            foreach(var hero in PartyController.Current.members)hero.Health.Receive(new DamagePacket{amount=hero.Health.maximum*100,type=DamageType.Pulse});
            var panel=Object.FindFirstObjectByType<DefeatPanel>();Assert.IsNotNull(panel,"fixture: there is a defeat panel");
            for(float end=Time.unscaledTime+6;!panel.IsOpen&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(panel.IsOpen,"with both heroes down no defeat panel opens");
            Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b=>b.name=="Retry").onClick.Invoke();
            yield return null;
            for(float end=Time.unscaledTime+25;SceneFlow.Current.Loading&&Time.unscaledTime<end;)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"the retry never finished loading");
            yield return Frames(3);Quiet();foreach(var brain in All<EnemyBrain>())brain.Passive=true;
            Assert.AreEqual(zone,State.zone,"a retry goes back to another zone");
            Assert.AreEqual(checkpoint,State.spawn,"a retry doesn't come back at the door");
            var spawn=All<SpawnPoint>().SingleOrDefault(s=>s.id==checkpoint);Assert.IsNotNull(spawn,zone+" has no spawn called "+checkpoint);
            var active=PartyController.Current.Active;
            Assert.LessOrEqual(Flat(active.transform.position,spawn.transform.position),6,"a retry puts the hero somewhere other than the door's spawn");
            foreach(var hero in PartyController.Current.members)Assert.AreEqual(hero.Health.maximum,hero.Health.integrity,1,hero.character+" comes back wounded");
            door=Door();Assert.IsFalse(door.Started,"the arena comes back already started, with its seals shut and nobody in it");
            // Shorter: less walk than the zone's arrival, and nothing to sit through again.
            var arrival=All<SpawnPoint>().FirstOrDefault(s=>s.id=="Arrival");
            if(arrival!=null)Assert.Less(Flat(spawn.transform.position,door.transform.position),Flat(arrival.transform.position,door.transform.position),"the retry's walk to the door is no shorter than the walk from the zone's start");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsFalse(DialogueSystem.Current.Running,"a retry opens on a conversation the party has already had");
            door.Begin();yield return Frames(2);
            foreach(var brain in All<EnemyBrain>())brain.Passive=true;
            Assert.IsFalse(DialogueSystem.Current.Running,"the second attempt's door opens on a conversation the party has already had");
            Assert.IsFalse(GameServices.Current.Flags.GetBool("bossdown."+id),"a lost fight counts the boss as down");
            var second=All<EnemyBrain>().FirstOrDefault(e=>e.definition.id==id);Assert.IsNotNull(second,id+" isn't there for the second attempt");
            Assert.AreEqual(second.Health.maximum,second.Health.integrity,1,id+" keeps its wounds from the lost attempt");
        }

        // ------------------------------------------------------------------ Hessa, specifically
        [UnityTest] public IEnumerator HerLampbreakerCanBeOutrunOrRolledThrough()
        {
            yield return Zone("Arena_Ground");Arena();Sync(9);var taren=Lead("Taren");Place(Sela,new Vector3(260,.1f,0));
            var enemy=Enemy("HessaKorl",new Vector3(200,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            // A jog from three metres, a sprint from point blank.
            foreach(var (start,sprint) in new[]{(3f,false),(1.2f,true)})
            {
                for(float end=Time.unscaledTime+6;boss.Busy&&Time.unscaledTime<end;)yield return null;
                PlaceBody(enemy,new Vector3(200,.1f,0));Place(taren,new Vector3(200,.1f,start));taren.Health.integrity=taren.Health.maximum;yield return Frames(2);
                float before=taren.Health.integrity;
                Assert.IsTrue(boss.Perform("Lampbreaker"),"fixture: the flash");
                for(float end=Time.unscaledTime+4;boss.Busy&&Time.unscaledTime<end;){taren.motor.Move(Vector2.up,sprint,false);yield return null;}
                Assert.Greater(Flat(taren.transform.position,enemy.transform.position),11.25f,$"fixture: he got clear of the ring from {start} m");
                Assert.AreEqual(before,taren.Health.integrity,.01f,$"a hero who ran from {start} m was still caught by the flash");
            }
            // A roll as it goes off, in the open, well inside the ring.
            for(float end=Time.unscaledTime+6;boss.Busy&&Time.unscaledTime<end;)yield return null;
            Place(taren,new Vector3(200,.1f,5));taren.Health.integrity=taren.Health.maximum;yield return Frames(2);
            float standing=taren.Health.integrity;bool rolled=false;
            Assert.IsTrue(boss.Perform("Lampbreaker"),"fixture: the flash");
            for(float end=Time.unscaledTime+6;boss.Busy&&Time.unscaledTime<end;)
            {
                taren.motor.Move(Vector2.zero,false,false);
                if(!rolled&&boss.Telegraphing&&boss.TelegraphRemaining<.1f*Time.timeScale)rolled=taren.Dodge(Vector3.right);
                yield return null;
            }
            Assert.IsTrue(rolled,"fixture: the flash told him when to roll");
            Assert.Less(Flat(taren.transform.position,enemy.transform.position),11,"fixture: he rolled through it, not out of its ring");
            Assert.AreEqual(standing,taren.Health.integrity,.01f,"a roll as the flash goes off doesn't get through it");
        }
        [UnityTest] public IEnumerator ARealRollOutOfHerChainLeavesHerOpen()
        {
            yield return Zone("Arena_Ground");Arena();var taren=Lead("Taren");Place(Sela,new Vector3(260,.1f,0));Place(taren,new Vector3(200,.1f,9));
            var enemy=Enemy("HessaKorl",new Vector3(200,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            float before=taren.Health.integrity;bool rolled=false;log.Clear();
            Assert.IsTrue(boss.Perform("GrappleChain"),"fixture: the chain");
            for(float end=Time.unscaledTime+4;boss.Busy&&Time.unscaledTime<end;)
            {
                taren.motor.Move(Vector2.zero,false,false);
                if(!rolled&&boss.Telegraphing&&boss.TelegraphRemaining<.08f)rolled=taren.Dodge(Vector3.right);
                yield return null;
            }
            Assert.IsTrue(rolled,"fixture: he rolled as the chain flew");
            Assert.AreEqual(before,taren.Health.integrity,.01f,"the chain caught a hero who rolled as it flew");
            Assert.Greater(Flat(taren.transform.position,enemy.transform.position),6,"a hero who rolled was dragged in anyway");
            Assert.IsTrue(log.Any(l=>l.line.StartsWith("HESSA_OPEN")),"a chain dodged with a real roll leaves her no opening");
            Assert.IsTrue(enemy.Health.Broken,"a chain dodged with a real roll leaves her no opening");
            Assert.Greater(enemy.Health.BrokenUntil-Time.time,.5f,"the opening after her dodged chain is already nearly over");
        }
        [UnityTest] public IEnumerator InHerLastPhaseTheFourthThrustIsTheOneHeldABeat()
        {
            yield return Zone("Arena_Ground");Arena();var lead=PartyController.Current.Active;Place(Other(lead),new Vector3(260,.1f,0));Place(lead,new Vector3(200,.1f,2.5f));Fresh(lead);
            var enemy=Enemy("HessaKorl",new Vector3(200,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            enemy.Health.integrity=enemy.Health.maximum*.25f;yield return Frames(2);Assert.AreEqual(3,boss.Phase,"fixture: her last phase");
            var tells=new List<float>();var seen=new HashSet<Telegraph>();
            Assert.IsTrue(boss.Perform("HookLance"),"fixture: the lance");
            for(float end=Time.unscaledTime+7;boss.Busy&&Time.unscaledTime<end;)
            {
                foreach(var mark in Telegraph.Live)if(mark!=null&&mark.owner==boss&&mark.Shape==Telegraph.Kind.Cone&&seen.Add(mark))tells.Add(mark.Duration);
                yield return null;
            }
            Assert.AreEqual(4,tells.Count,$"desperate, her combo should be four thrusts: it marked {tells.Count}");
            Assert.Greater(tells[3],tells[2]+.3f,"her last thrust keeps the rhythm of the ones before it: dodging on the beat would be safe");
        }
    }
}
