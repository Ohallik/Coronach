using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The November QA pass, the fight itself: what is marked is what is hit, a press isn't lost, a Flash is earned, Charge
    /// limits something, and every boss has a punish and its own hit. Every check here failed on the build it was written against.</summary>
    public sealed class QaFightTests
    {
        Gamepad pad;
        CombatActor Taren=>PartyController.Current.members.First(m=>m.character=="Taren");
        CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        static MemberState Member(string id)=>GameServices.Current.State.party.Find(m=>m.id==id);
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return Zone("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);pad=null;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+15;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.7f);
        }
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            if(actor.motor is GroundMotor ground)ground.KeepFacing(Vector3.forward);
            Physics.SyncTransforms();
        }
        /// <summary>Open floor far from the arena's walls.</summary>
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Fight fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static GameObject Wall(Vector3 center,Vector3 size)
        {var wall=new GameObject("Fight fixture wall",typeof(BoxCollider));wall.transform.position=center;wall.GetComponent<BoxCollider>().size=size;Physics.SyncTransforms();return wall;}
        static EnemyBrain Enemy(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        /// <summary>A body that never dies or breaks, and takes every skill at face value (Charge is the question, not type).</summary>
        static EnemyBrain Dummy(Vector3 at,DamageType neutralTo=DamageType.Plasma)
        {
            var enemy=Enemy("Ridgehound",at);var body=enemy.Health;body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;
            body.weakness=body.resistance=neutralTo==DamageType.Kinetic?DamageType.Pulse:DamageType.Kinetic;return enemy;
        }
        static BossController Boss(string id,Vector3 at){var enemy=Enemy(id,at);return enemy.GetComponent<BossController>();}
        /// <summary>A boss's specials wait: the fixture is the filler attack between them. After Start, which sets the clock.</summary>
        static void NoSpecials(EnemyBrain enemy)
        {if(enemy.TryGetComponent<BossController>(out var boss))typeof(BossController).GetField("nextSpecial",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(boss,Time.time+60);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static IEnumerator UntilFree(BossController boss,float seconds)
        {for(float end=Time.unscaledTime+seconds;boss.Busy&&Time.unscaledTime<end;)yield return null;}
        static IEnumerator Tell(EnemyBrain enemy,float seconds=3)
        {
            for(float end=Time.unscaledTime+seconds;!enemy.Telegraphing&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(enemy.Telegraphing,"fixture: "+enemy.definition.id+" never marked an attack");
        }
        /// <summary>The Sync level the fixture plays at, with every skill back at mastery 1.</summary>
        static void Sync(int level)
        {
            foreach(var member in GameServices.Current.State.party){member.level=level;member.skillUses?.Clear();}
            PartyController.Current.RefreshStats();
        }
        static CombatActor Lead(string hero)
        {
            var party=PartyController.Current;if(party.Active.character!=hero)Assert.IsTrue(party.Swap(),"fixture: the swap");
            foreach(var member in party.members){member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;}
            return party.Active;
        }
        static SkillDef Equip(CombatActor hero,SkillKind kind,int slot=0)
        {
            var skill=SkillBook.All(hero.character).First(s=>s.kind==kind);
            Assert.IsTrue(SkillBook.Equip(Member(hero.character),slot,skill),"fixture: "+kind+" can't be equipped");
            hero.charge=100;hero.cooldowns[slot]=0;return skill;
        }
        static readonly MethodInfo WorthwhileMethod=typeof(PartnerBrain).GetMethod("Worthwhile",BindingFlags.NonPublic|BindingFlags.Instance);
        static bool Worthwhile(CombatActor hero,SkillKind kind)=>(bool)WorthwhileMethod.Invoke(hero.GetComponent<PartnerBrain>(),new object[]{kind});
        IEnumerator Pad(params GamepadButton[] down)
        {
            var state=new GamepadState();foreach(var button in down)state=state.WithButton(button);
            InputSystem.QueueStateEvent(pad,state);yield return null;yield return null;
        }

        // ------------------------------------------------------------------ the Flash's clock
        [UnityTest] public IEnumerator ASkillCastWithoutIFramesInAFlashStillReleasesOnItsPose()
        {
            Arena();Sync(12);var sela=Lead("Sela");Place(sela,new Vector3(200,0,0));Place(Taren,new Vector3(260,0,0));yield return Frames(2);
            Equip(sela,SkillKind.Starfall);
            // Someone else's Flash: the world crawls and this hero has no i-frames of their own.
            GameTime.BeginFlash();
            Assert.IsTrue(sela.Skill(0),"fixture: Starfall cast");
            for(float end=Time.unscaledTime+1;!sela.CanAct&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(sela.Attack(),"fixture: she is free again and attacks as the pose ends");
            yield return new WaitForSecondsRealtime(.1f);
            Assert.IsNotNull(Object.FindFirstObjectByType<StarfallStrike>(),"a hero swapped in during a Flash pays for Starfall, the pose ends, the next attack cancels it, and nothing ever falls");
        }
        [UnityTest] public IEnumerator AForgeSpiralInAFlashLandsAllItsHitsBeforeThePoseEnds()
        {
            Arena();Sync(12);var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var victim=Dummy(new Vector3(200,0,2));yield return Frames(2);taren.target=victim.Health;
            int hits=0;victim.Health.Damaged+=(_,packet,__)=>{if(packet.source==taren.Health)hits++;};
            Equip(taren,SkillKind.ForgeSpiral);
            GameTime.BeginFlash();
            Assert.IsTrue(taren.Skill(0),"fixture: Forge Spiral cast");
            for(float end=Time.unscaledTime+1.5f;!taren.CanAct&&Time.unscaledTime<end;)yield return null;
            yield return Frames(2);
            Assert.GreaterOrEqual(hits,5,"in a partner's Flash the spin runs four times slower than its pose, and the next attack cuts it off with its Charge spent");
        }

        // ------------------------------------------------------------------ a marking reaches as far as its hit
        [UnityTest] public IEnumerator AHoundBitesNoFurtherThanItsPaintedLane()
        {
            Arena();var taren=Taren;Place(Sela,new Vector3(260,0,0));
            var hound=Enemy("Ridgehound",new Vector3(200,0,0));Place(taren,new Vector3(200,0,5.5f));
            yield return new WaitForSecondsRealtime(.7f);
            hound.Passive=false;yield return Tell(hound);
            var lane=hound.AttackMark;Assert.AreEqual(Telegraph.Kind.Line,lane.Shape,"fixture: a hound's lane");
            // Backing off along the lane: 4.5 m of travel, a sphere of 1.15 set .5 ahead, and his own body still reach him here.
            Place(taren,lane.Origin+lane.Forward*6.3f);
            Assert.IsTrue(lane.Contains(taren.transform.position),$"a hero 6.3 m down a {lane.Length:0.00} m lane looks clear of it, and the lunge still bites him");
            float before=taren.Health.integrity;
            for(float end=Time.unscaledTime+1.2f;taren.Health.integrity>=before&&Time.unscaledTime<end;)yield return null;
            Assert.Less(taren.Health.integrity,before,"fixture: the bite really reaches that far");
            hound.Passive=true;
        }
        [UnityTest] public IEnumerator AShotIsMarkedAsFarAsItFliesAndFliesNoFurther()
        {
            Arena();var taren=Taren;Place(Sela,new Vector3(260,0,0));
            foreach(string id in new[]{"Burrower","ChoristerDrifter"})
            {
                var shooter=Enemy(id,new Vector3(200,.1f,0));Place(taren,new Vector3(200,.1f,14.5f));
                yield return new WaitForSecondsRealtime(.7f);
                NoSpecials(shooter);
                shooter.Passive=false;yield return Tell(shooter);
                var mark=shooter.AttackMark;var origin=mark.Origin;
                Assert.IsTrue(mark.Contains(taren.transform.position),$"{id} opens fire from 14.5 m with a marking that stops short of the hero it is aimed at");
                // Step past the tip during the tell: standing clear of the marking has to be safe.
                Place(taren,origin+Vector3.forward*16.6f);float before=taren.Health.integrity;
                Assert.IsFalse(mark.Contains(taren.transform.position),"fixture: past the marking's tip");
                for(float end=Time.unscaledTime+2;shooter.Telegraphing&&Time.unscaledTime<end;)yield return null;
                shooter.Passive=true;
                // Watch the shots out over that empty ground: each ends where it burns out, in plain sight of the player.
                var flying=new Dictionary<Projectile,Vector3>();int burnedOut=0,vanished=0;
                for(float end=Time.unscaledTime+2.2f;Time.unscaledTime<end;)
                {
                    foreach(var shot in Projectile.Live)if(shot.Threatens(true))flying[shot]=shot.transform.position;
                    foreach(var ended in flying.Keys.Where(shot=>!Projectile.Live.Contains(shot)).ToList())
                    {
                        var at=flying[ended];flying.Remove(ended);
                        if(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(p=>p.name=="Lattice particles"&&p.IsAlive()&&Vector3.Distance(p.transform.position,at)<1))burnedOut++;else vanished++;
                    }
                    yield return null;
                }
                Assert.Greater(burnedOut+vanished,0,$"fixture: {id} fired");
                Assert.AreEqual(0,vanished,$"{id}'s shot ends at the tip of its marking by vanishing in a single frame, with the paint long gone: it looks like a bug");
                Assert.AreEqual(before,taren.Health.integrity,.01f,$"{id}'s shot flew on past the end of its marking and hit a hero standing clear of it");
                // And it does reach everyone the marking covers.
                Place(taren,origin+Vector3.forward*14.5f);before=taren.Health.integrity;
                shooter.Passive=false;yield return Tell(shooter);
                for(float end=Time.unscaledTime+2;shooter.Telegraphing&&Time.unscaledTime<end;)yield return null;
                shooter.Passive=true;
                for(float end=Time.unscaledTime+2;taren.Health.integrity>=before&&Time.unscaledTime<end;)yield return null;
                Assert.Less(taren.Health.integrity,before,$"{id}'s shot dies short of a hero inside its marking");
                Object.Destroy(shooter.gameObject);yield return Frames(2);
            }
        }
        [UnityTest] public IEnumerator HessasSidearmStopsAtTheEndOfItsLine()
        {
            Arena();var taren=Taren;Place(Sela,new Vector3(260,0,0));
            var boss=Boss("HessaKorl",new Vector3(200,.1f,0));Place(taren,new Vector3(200,.1f,12));yield return Frames(3);
            Assert.IsTrue(boss.Perform("Sidearm"),"fixture: the sidearm");
            // She backsteps first: the line starts where she stands to fire, and each shot aims afresh along its own line.
            Telegraph line=null;
            for(float end=Time.unscaledTime+1.5f;line==null&&Time.unscaledTime<end;){line=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==boss&&t.Shape==Telegraph.Kind.Line);yield return null;}
            Assert.IsNotNull(line,"fixture: the sidearm's line");
            Place(taren,line.Origin+line.Forward*(line.Length+2));float before=taren.Health.integrity;
            var seen=new HashSet<Projectile>();
            for(float end=Time.unscaledTime+2.4f;Time.unscaledTime<end;){foreach(var shot in Projectile.Live)if(shot.Threatens(true))seen.Add(shot);yield return null;}
            Assert.Greater(seen.Count,0,"fixture: she fired");
            Assert.AreEqual(before,taren.Health.integrity,.01f,$"her shots fly on past the end of their {line.Length:0} m line and hit a hero standing clear of it");
        }

        // ------------------------------------------------------------------ bodies wind up
        [UnityTest] public IEnumerator ARiggedBodyWindsUpThroughItsTell()
        {
            Arena();var taren=Taren;Place(Sela,new Vector3(260,0,0));
            // One body on each skeleton: neither clip is named for its Attack state, and they differ in length.
            foreach(var (id,distance) in new[]{("Ridgehound",3f),("MusterScav",8f)})
            {
                var body=Enemy(id,new Vector3(200,0,0));Place(taren,new Vector3(200,0,distance));
                var rig=body.GetComponentInChildren<GeneratedAnimator>();Assert.IsNotNull(rig,$"fixture: the {id} is rigged");
                yield return new WaitForSecondsRealtime(.7f);
                body.Passive=false;yield return Tell(body);
                for(float end=Time.unscaledTime+1;body.Telegraphing&&body.TelegraphRemaining>.1f&&Time.unscaledTime<end;)yield return null;
                Assert.IsTrue(body.Telegraphing,"fixture: still telling");
                Assert.AreEqual("Attack",rig.CurrentAnimation,$"a rigged {id} idles through its whole tell, and the Attack clip starts only at the strike: nothing in the body says it is coming");
                Assert.AreEqual(body.definition.attacks[0].telegraph,body.TellDuration,.001f,$"the {id}'s wind-up follows some other length than its own tell");
                var animator=body.GetComponentInChildren<Animator>();
                var clip=animator.runtimeAnimatorController.animationClips.First(c=>c.name.EndsWith("Attack"));
                Assert.AreEqual(clip.length*EnemyBrain.TellShare/body.TellDuration,animator.speed,.02f,$"the {id}'s wind-up isn't stretched over its tell: the clip plays at its own speed and runs on past the wind-up");
                // Both clips swing from about a fifth of the way in.
                Assert.Less(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,.22f,$"the {id} has already swung with its hit still to land: the blow comes after the body has finished striking");
                body.Passive=true;body.Interrupt();Object.Destroy(body.gameObject);yield return Frames(2);
            }
        }
        [UnityTest] public IEnumerator ARigidBodyLeansIntoItsTellAndComesBack()
        {
            Arena();var taren=Taren;Place(Sela,new Vector3(260,0,0));
            var drifter=Enemy("ChoristerDrifter",new Vector3(200,.1f,0));Place(taren,new Vector3(200,.1f,8));
            yield return new WaitForSecondsRealtime(.7f);
            var body=drifter.GetComponent<DefeatPresentation>().visual;var rest=body.localRotation;
            drifter.Passive=false;yield return Tell(drifter);
            float peak=0;
            for(float end=Time.unscaledTime+2;drifter.Telegraphing&&Time.unscaledTime<end;){peak=Mathf.Max(peak,Quaternion.Angle(rest,body.localRotation));yield return null;}
            drifter.Passive=true;
            Assert.GreaterOrEqual(peak,6,"a rigid body floats through its tell without moving: only the disc under it says anything");
            yield return new WaitForSecondsRealtime(.6f);
            Assert.Less(Quaternion.Angle(rest,body.localRotation),1,"the body stays leaned after its strike");
        }
        [UnityTest] public IEnumerator AMineSwellsThroughItsTellAndBurstsFromThatSize()
        {
            Arena();var taren=Taren;Place(Sela,new Vector3(260,0,0));
            var mine=Enemy("Shellmine",new Vector3(200,.1f,0));Place(taren,new Vector3(200,.1f,2));
            yield return new WaitForSecondsRealtime(.7f);
            var body=mine.GetComponent<DefeatPresentation>().visual;float rest=body.localScale.x,peak=rest;
            mine.Passive=false;yield return Tell(mine);
            for(float end=Time.unscaledTime+2;mine!=null&&mine.Telegraphing&&Time.unscaledTime<end;){peak=Mathf.Max(peak,body.localScale.x);yield return null;}
            Assert.Greater(peak,rest*1.1f,"a mine about to blow looks exactly like one that isn't");
            Assert.IsFalse(mine.Health.Alive,"fixture: it went off");
            float lowest=peak;
            for(float end=Time.unscaledTime+.1f;Time.unscaledTime<end;){lowest=Mathf.Min(lowest,body.localScale.x);yield return null;}
            Assert.GreaterOrEqual(lowest,peak*.98f,$"a mine swollen to {peak/rest:0.00} drops to {lowest/rest:0.00} in the frame it blows: it visibly shrinks at the moment of the explosion");
        }
        [UnityTest] public IEnumerator TheBellowsSwellsBeforeItSpits()
        {
            Arena();var taren=Taren;Place(Sela,new Vector3(260,0,0));
            var bellows=Enemy("BellowsBelow",new Vector3(200,.1f,0));Place(taren,new Vector3(200,.1f,10));yield return Frames(3);
            var body=bellows.GetComponent<DefeatPresentation>().visual;float breathing=0;
            for(float end=Time.unscaledTime+3.2f;Time.unscaledTime<end;){breathing=Mathf.Max(breathing,body.localScale.x);yield return null;}
            NoSpecials(bellows);bellows.Passive=false;yield return Tell(bellows);float peak=0;
            for(float end=Time.unscaledTime+2;bellows.Telegraphing&&Time.unscaledTime<end;){peak=Mathf.Max(peak,body.localScale.x);yield return null;}
            bellows.Passive=true;
            Assert.Greater(peak,breathing*1.03f,"the dome just breathes on through a spit's tell");
        }

        // ------------------------------------------------------------------ a moving body stays in its lane
        [UnityTest] public IEnumerator AChargeStaysInItsLaneAfterThePaintHasFaded()
        {
            Arena();var taren=Taren;var sela=Sela;
            var boss=Boss("Burrower",new Vector3(200,.1f,0));Place(taren,new Vector3(200,.1f,16));
            // Beside the lane, inside the rig's 2.3 m contact, and .6 s of charging away: the painted lane has faded and destroyed itself by then.
            Place(sela,new Vector3(202.1f,.1f,10));yield return Frames(3);
            float beside=sela.Health.integrity,inside=taren.Health.integrity;
            Assert.IsTrue(boss.Perform("DrillCharge"),"fixture: the charge");yield return null;
            var lane=Telegraph.Live.First(t=>t!=null&&t.owner==boss&&t.Shape==Telegraph.Kind.Line);
            Assert.IsFalse(lane.Contains(sela.transform.position),"fixture: she stands outside the paint");
            Assert.IsTrue(lane.Contains(taren.transform.position),"fixture: he stands in the lane");
            yield return UntilFree(boss,5);
            Assert.Greater(boss.transform.position.z,12,"fixture: the rig charged past her");
            Assert.AreEqual(beside,sela.Health.integrity,.01f,"the lane is only tested for its first .32 s: a hero outside the paint is struck by the rest of the charge");
            Assert.Less(taren.Health.integrity,inside,"the charge no longer strikes the hero standing in its lane");
        }

        // ------------------------------------------------------------------ the Cantor
        [UnityTest] public IEnumerator TheCollarStallsAfterItsSongAndThePunishIsReal()
        {
            yield return Zone("Arena_Flight");
            var taren=Taren;Place(taren,new Vector3(200,1,-20));Place(Sela,new Vector3(240,1,-20));
            var boss=Boss("Cantor",new Vector3(200,1,0));yield return Frames(3);
            var cantor=boss.GetComponent<EnemyBrain>();var collar=boss.GetComponent<CantorCollar>();var link=collar.Links[0];
            DamagePacket Hit(float breakPower)=>new DamagePacket{source=taren.Health,amount=100,breakPower=breakPower,type=DamageType.Beam};
            link.Receive(Hit(100));float gauge=cantor.Health.BreakMeter;Assert.Greater(gauge,0,"fixture: some break built up");
            var plate=collar.Presentation(0).GetComponentsInChildren<Renderer>()[0].transform;float snug=plate.localPosition.magnitude;
            var said=new List<string>();void Heard(string who,string line){said.Add(line);}BarkService.Spoken+=Heard;
            Assert.IsTrue(boss.Perform("Choir"),"fixture: the song");yield return UntilFree(boss,3);
            BarkService.Spoken-=Heard;
            Assert.IsTrue(cantor.Health.Broken,"after its song the Cantor rests at full strength: the whole collar stage has no opening to punish");
            Assert.AreEqual(gauge,cantor.Health.BreakMeter,.01f,"the opening emptied the break gauge the player had built up");
            Assert.IsTrue(said.Any(l=>l.Contains("collar")),"nobody says the collar has given way: "+string.Join(" / ",said));
            float before=link.integrity;link.Receive(Hit(0));float open=before-link.integrity;
            cantor.Passive=false;yield return new WaitForSecondsRealtime(.3f);
            var state=Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<TMP_Text>(true).First(t=>t.name=="BossState");
            StringAssert.Contains("OPENING",state.text,"the bar doesn't say the window is open");
            cantor.Passive=true;
            Assert.Greater(plate.localPosition.magnitude,snug*1.08f,"the collar looks no different while it is open");
            for(float end=Time.unscaledTime+2;cantor.Health.Broken&&Time.unscaledTime<end;)yield return null;
            Assert.IsFalse(cantor.Health.Broken,"fixture: the window closes");
            before=link.integrity;link.Receive(Hit(0));float shut=before-link.integrity;
            Assert.AreEqual(1.5f,open/shut,.01f,"hitting the links during the opening does no more than hitting them after it: the head can't be hurt yet, so the links have to take the punish");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(snug,plate.localPosition.magnitude,.01f,"the collar stays slack after its opening has closed");
        }
        [UnityTest] public IEnumerator TheCantorsNewMovesGetRealTimeAtTheRateThePartyDealsDamage()
        {
            yield return Zone("Arena_Flight");
            var taren=Taren;Place(taren,new Vector3(200,1,-14));Place(Sela,new Vector3(214,1,-14));
            foreach(var hero in PartyController.Current.members)hero.Health.InvulnerableUntil=GameTime.Now+90;
            var boss=Boss("Cantor",new Vector3(200,1,0));yield return Frames(3);var cantor=boss.GetComponent<EnemyBrain>();var collar=boss.GetComponent<CantorCollar>();
            const float rate=285; // Integrity per second the replay party takes off this boss, on the x2 hurtboxes of its links and head alike.
            float total=collar.Links.Sum(l=>l.maximum)+cantor.Health.maximum;
            Assert.AreEqual(cantor.definition.integrity,total,1,"the shares stopped adding up to the boss's total");
            float last=cantor.Health.maximum*boss.Thresholds[boss.Thresholds.Length-1]/rate;
            Assert.GreaterOrEqual(last,20,$"its last phase lasts {last:0.0} s of a {total/rate:0} s fight: the lash and the spiral play about once each and it is over");
            foreach(var link in collar.Links)link.Receive(new DamagePacket{source=taren.Health,amount=link.maximum*100,type=DamageType.Beam});
            yield return null;Assert.IsTrue(cantor.Health.Damageable,"fixture: the collar is gone");
            cantor.Health.Receive(new DamagePacket{source=taren.Health,amount=1,type=DamageType.Beam});
            cantor.Passive=false;cantor.Health.integrity=cantor.Health.maximum*boss.Thresholds[boss.Thresholds.Length-1]*.999f;
            var moves=new List<string>();void Moved(BossController who,string move){if(who==boss)moves.Add(move);}BossController.MoveStarted+=Moved;
            for(float end=Time.unscaledTime+45;cantor.Health.integrity>1&&Time.unscaledTime<end;){cantor.Health.integrity=Mathf.Max(1,cantor.Health.integrity-rate*Time.deltaTime);yield return null;}
            BossController.MoveStarted-=Moved;cantor.Passive=true;
            Assert.GreaterOrEqual(moves.Count(m=>m=="CoilLash"||m=="SongSpiral"),2,"the last phase is over before the second lash or spiral: "+string.Join(",",moves));
        }
        [UnityTest] public IEnumerator TheCantorHitsAsHardAsTheBossesAroundIt()
        {
            yield return Zone("Arena_Flight");Sync(6);
            var taren=Taren;Place(taren,new Vector3(200,1,5));Place(Sela,new Vector3(240,1,0));
            var boss=Boss("Cantor",new Vector3(200,1,0));yield return Frames(3);
            float biggest=0;taren.Health.Damaged+=(_,__,amount)=>biggest=Mathf.Max(biggest,amount);
            Assert.IsTrue(boss.Perform("Choir"),"fixture: the sweep");yield return UntilFree(boss,3);
            float share=biggest/taren.Health.maximum;
            Assert.Greater(share,0,"fixture: the sweep landed");
            Assert.GreaterOrEqual(share,.11f,$"the Cantor's sweep takes {share:P1} of a Sync 6 hero: the softest boss in the game, against 11.4% from the first one");
            Assert.LessOrEqual(share,.16f,$"the Cantor's sweep takes {share:P1} of a Sync 6 hero in its first phase: well over what any other boss opens with");
        }

        // ------------------------------------------------------------------ a press isn't lost
        [UnityTest] public IEnumerator APressMadeEarlyInALongRecoveryWaitsForItAndADodgeForgetsIt()
        {
            pad=InputSystem.AddDevice<Gamepad>("QaFightPad");Arena();Sync(12);yield return null;
            var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            taren.GetComponent<PlayerBrain>().AutoPilot=false;yield return Frames(3);
            // A skill pressed at the start of Forge Spiral's .9 s recovery.
            Equip(taren,SkillKind.ForgeSpiral);Assert.IsTrue(taren.Skill(0),"fixture: the spiral");float started=Time.unscaledTime;
            var second=SkillBook.Equipped("Taren",1);taren.charge=100;taren.cooldowns[1]=0;
            yield return Pad(GamepadButton.RightShoulder);yield return Pad(GamepadButton.RightShoulder,GamepadButton.East);yield return Pad();
            Assert.IsFalse(taren.CanAct,"fixture: he is still in the recovery");
            Assert.AreEqual(0,taren.cooldowns[1],.001f,"the press fired through the recovery");
            for(float end=started+1.3f;taren.cooldowns[1]<=0&&Time.unscaledTime<end;)yield return null;
            Assert.Greater(taren.cooldowns[1],0,$"{second.displayName} pressed .8 s before the recovery ended was lost with no cue");
            for(float end=Time.unscaledTime+2;!taren.CanAct&&Time.unscaledTime<end;)yield return null;
            // An attack pressed in the same place. A swing is the only thing that changes the pose to an AttackN.
            taren.cooldowns[0]=0;taren.charge=100;Assert.IsTrue(taren.Skill(0),"fixture: the spiral again");started=Time.unscaledTime;
            yield return Pad(GamepadButton.South);yield return Pad();
            for(float end=started+1.3f;!taren.VisualAction.StartsWith("Attack")&&Time.unscaledTime<end;)yield return null;
            StringAssert.StartsWith("Attack",taren.VisualAction,"an attack pressed .8 s before the recovery ended was lost with no cue");
            for(float end=Time.unscaledTime+2;!taren.CanAct&&Time.unscaledTime<end;)yield return null;
            // A dodge in between: the player has moved on, and nothing old fires when it ends.
            taren.cooldowns[0]=taren.cooldowns[1]=0;taren.charge=100;Assert.IsTrue(taren.Skill(0),"fixture: the spiral once more");started=Time.unscaledTime;
            taren.cooldowns[1]=0;
            yield return Pad(GamepadButton.RightShoulder);yield return Pad(GamepadButton.RightShoulder,GamepadButton.East);yield return Pad();
            yield return Pad(GamepadButton.East);yield return Pad();
            for(float end=started+1.5f;Time.unscaledTime<end;)yield return null;
            Assert.AreEqual(0,taren.cooldowns[1],.001f,"a skill pressed before a dodge fired after it: the player had already moved on");
            // And a guard, the same.
            for(float end=Time.unscaledTime+2;!taren.CanAct&&Time.unscaledTime<end;)yield return null;
            taren.cooldowns[0]=taren.cooldowns[1]=0;taren.charge=100;Assert.IsTrue(taren.Skill(0),"fixture: the spiral a last time");started=Time.unscaledTime;
            taren.cooldowns[1]=0;
            yield return Pad(GamepadButton.RightShoulder);yield return Pad(GamepadButton.RightShoulder,GamepadButton.East);yield return Pad();
            yield return Pad(GamepadButton.West);
            for(float end=started+1.2f;Time.unscaledTime<end;)yield return null;
            yield return Pad();
            Assert.AreEqual(0,taren.cooldowns[1],.001f,"a skill pressed before a guard fired through it: the player had already moved on");
        }
        [UnityTest] public IEnumerator AFlightLungeWaitsOutARecoveryAndARollForgetsIt()
        {
            pad=InputSystem.AddDevice<Gamepad>("QaFightPad");yield return Zone("Arena_Flight");Sync(12);
            var taren=Lead("Taren");Place(taren,new Vector3(200,1,0));Place(Sela,new Vector3(260,1,0));
            var mark=Dummy(new Vector3(200,1,20));yield return Frames(2);taren.target=mark.Health;
            taren.GetComponent<PlayerBrain>().AutoPilot=false;yield return Frames(3);
            // A lunge pressed at the start of a flight Forge Spiral's .5 s recovery.
            Equip(taren,SkillKind.ForgeSpiral);Assert.IsTrue(taren.Skill(0),"fixture: the spiral");float started=Time.unscaledTime;
            yield return Pad(GamepadButton.West);yield return Pad();
            Assert.IsFalse(taren.CanAct,"fixture: he is still in the recovery");
            Assert.AreNotEqual("Dash",taren.VisualAction,"the lunge fired through the recovery");
            for(float end=started+1;taren.VisualAction!="Dash"&&Time.unscaledTime<end;)yield return null;
            Assert.AreEqual("Dash",taren.VisualAction,"a lunge pressed .4 s before the recovery ended was lost with no cue");
            yield return new WaitForSecondsRealtime(.8f);
            // The same press with a roll straight after it. The roll has to go: this is the player moving on.
            Place(taren,new Vector3(200,1,0));taren.target=mark.Health;
            taren.cooldowns[0]=0;taren.charge=100;Assert.IsTrue(taren.Skill(0),"fixture: the spiral again");started=Time.unscaledTime;
            yield return Pad(GamepadButton.West);yield return Pad();
            yield return Pad(GamepadButton.East);yield return Pad();
            Assert.AreEqual("Dodge",taren.VisualAction,"fixture: the roll");
            for(float end=started+1.2f;Time.unscaledTime<end;)yield return null;
            Assert.AreNotEqual("Dash",taren.VisualAction,"a lunge pressed before a roll fired when the roll ended: the ship rolled clear, then dashed 8 m back at its target with no i-frames");
        }
        [UnityTest] public IEnumerator ADodgeThatIsRefusedLosesNoPress()
        {
            pad=InputSystem.AddDevice<Gamepad>("QaFightPad");Arena();yield return null;
            var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            taren.GetComponent<PlayerBrain>().AutoPilot=false;yield return Frames(3);
            // An attack pressed during a roll, then B again before another roll can start: that B does nothing, so it can't cost the attack.
            yield return Pad(GamepadButton.East);yield return Pad();
            Assert.AreEqual("Dodge",taren.VisualAction,"fixture: the roll");
            yield return Pad(GamepadButton.South);yield return Pad(GamepadButton.South,GamepadButton.East);yield return Pad();
            for(float end=Time.unscaledTime+1;!taren.VisualAction.StartsWith("Attack")&&Time.unscaledTime<end;)yield return null;
            StringAssert.StartsWith("Attack",taren.VisualAction,"B tapped during a roll does nothing, and it threw away the attack pressed before it: no second roll, no attack and no cue");
            // And a skill waiting its turn, RB let go as B goes down.
            for(float end=Time.unscaledTime+2;!taren.CanAct&&Time.unscaledTime<end;)yield return null;
            taren.charge=100;taren.cooldowns[0]=0;
            yield return Pad(GamepadButton.East);yield return Pad(GamepadButton.RightShoulder);yield return Pad(GamepadButton.RightShoulder,GamepadButton.South);
            yield return Pad(GamepadButton.East);yield return Pad();
            for(float end=Time.unscaledTime+1;taren.cooldowns[0]<=0&&Time.unscaledTime<end;)yield return null;
            Assert.Greater(taren.cooldowns[0],0,"B tapped during a roll does nothing, and it threw away the skill pressed before it: no second roll, no skill and no cue");
        }

        // ------------------------------------------------------------------ skills and walls
        [UnityTest] public IEnumerator StarfallDropsNoBeamPastAWall()
        {
            Arena();Sync(12);var sela=Sela;Place(sela,new Vector3(175,0,0));Place(Taren,new Vector3(260,0,0));
            Wall(new Vector3(200.9f,2,5),new Vector3(.3f,4,12));
            var near=Dummy(new Vector3(199,0,5));var far=Dummy(new Vector3(202.4f,0,5));yield return Frames(2);
            float nearBefore=near.Health.integrity,farBefore=far.Health.integrity;
            // Several at once: each beam's place is a random roll, and any one past the wall is the bug.
            for(int i=0;i<4;i++)StarfallStrike.Call(sela,new Vector3(200,0,5),false,1);
            yield return new WaitForSecondsRealtime(1.8f);
            Assert.AreEqual(farBefore,far.Health.integrity,.01f,"the circle's ring ends at the wall and its beams land past it, on an enemy behind cover");
            Assert.Less(near.Health.integrity,nearBefore,"fixture: the beams still hit on her side of the wall");
        }
        [UnityTest] public IEnumerator AGroundProbeInsideASolidFindsTheFloorNotTheWorldsOrigin()
        {
            Arena();Wall(new Vector3(200,2,.95f),new Vector3(8,4,1));yield return null;
            var ground=CombatCover.Ground(new Vector3(200,1,.95f));
            Assert.Less(Flat(ground,new Vector3(200,0,.95f)),.05f,$"a probe that starts inside a wall answers {ground}: whatever asked now opens at the world origin");
            Assert.AreEqual(0,ground.y,.05f,"the probe missed the floor under the wall");
        }
        [UnityTest] public IEnumerator AFoldCastAtAWallOpensBesideHer()
        {
            Arena();Sync(12);var sela=Sela;Place(sela,new Vector3(200,0,0));Place(Taren,new Vector3(260,0,0));
            // She stands right against it, so the seed's usual start .6 m ahead is inside the wall.
            Wall(new Vector3(200,2,.95f),new Vector3(8,4,1));yield return Frames(2);
            Equip(sela,SkillKind.GravityFold);Assert.IsTrue(sela.Skill(0),"fixture: the Fold");
            yield return new WaitForSecondsRealtime(.8f);
            var well=Object.FindFirstObjectByType<GravityWell>();Assert.IsNotNull(well,"fixture: the well opened");
            Assert.Less(Flat(well.transform.position,sela.transform.position),1.6f,$"a Fold thrown at a wall she stands against opens at {well.transform.position}, nowhere near her");
            Assert.Less(well.transform.position.z,.45f,"the well opened inside or behind the wall");
            Assert.AreEqual(0,well.transform.position.y,.3f,"the well isn't on the floor");
        }
        [UnityTest] public IEnumerator StarfallReachesAnUphillTarget()
        {
            var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.transform.SetPositionAndRotation(new Vector3(200,0,10),Quaternion.Euler(-10,0,0));ramp.transform.localScale=new Vector3(20,1,60);Physics.SyncTransforms();
            Assert.IsTrue(Physics.Raycast(new Vector3(200,30,0),Vector3.down,out var low,60),"fixture: the ramp");
            Assert.IsTrue(Physics.Raycast(new Vector3(200,30,8),Vector3.down,out var high,60),"fixture: the ramp");
            Sync(12);var sela=Sela;Place(sela,low.point);Place(Taren,new Vector3(260,low.point.y,0));
            var target=Dummy(high.point+Vector3.up*.05f);yield return Frames(3);sela.target=target.Health;
            Equip(sela,SkillKind.Starfall);Assert.IsTrue(sela.Skill(0),"fixture: Starfall");
            yield return new WaitForSecondsRealtime(.55f);
            var strike=Object.FindFirstObjectByType<StarfallStrike>();Assert.IsNotNull(strike,"fixture: the circle");
            Assert.Less(Flat(strike.transform.position,target.transform.position),1,$"the sweep runs flat at her height and stops at the slope: the circle lands {Flat(strike.transform.position,target.transform.position):0.0} m short of a target 8 m up a gentle rise");
            Assert.AreEqual(high.point.y,strike.transform.position.y,.3f,"the circle isn't on the ground under the target");
        }

        // ------------------------------------------------------------------ Charge limits something
        [UnityTest] public IEnumerator AMultiHitSkillCannotPayForItselfOnOneTarget()
        {
            Arena();Sync(12);yield return Frames(2);
            var failures=new List<string>();
            foreach(var (name,kind,seconds,distance) in new[]{("Taren",SkillKind.ForgeSpiral,1.3f,2f),("Sela",SkillKind.SurveyDrone,9.6f,4f),("Sela",SkillKind.Starfall,2.3f,4f),("Sela",SkillKind.GravityFold,3.1f,4f),("Sela",SkillKind.StaticNet,5f,4f),("Sela",SkillKind.ScatterBloom,1f,1.4f)})
            {
                var hero=Lead(name);var other=PartyController.Current.members.First(m=>m!=hero);
                Place(hero,new Vector3(200,0,0));Place(other,new Vector3(260,0,0));
                var victim=Dummy(new Vector3(200,0,distance));yield return Frames(2);hero.target=victim.Health;
                var skill=Equip(hero,kind);float cost=SkillBook.Cost(hero.character,skill),gained=0;
                Assert.AreEqual(1,SkillBook.Mastery(hero.character,skill.id),"fixture: mastery 1");
                // Room to earn it all back: the bar's cap mustn't hide what the skill repays.
                hero.charge=cost+5;
                Assert.IsTrue(hero.Skill(0),"fixture: "+kind);
                float last=hero.charge;
                for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);if(hero.charge>last)gained+=hero.charge-last;last=hero.charge;yield return null;}
                float dealt=victim.Health.maximum-victim.Health.integrity;
                if(dealt<=0)failures.Add($"fixture: {kind} never hit its target");
                if(gained>=cost)failures.Add($"{kind} costs {cost:0} Charge and pays {gained:0} back from one target");
                foreach(var drone in Object.FindObjectsByType<SurveyDrone>(FindObjectsSortMode.None))Object.Destroy(drone.gameObject);
                Object.Destroy(victim.gameObject);yield return new WaitForSecondsRealtime(.2f);
                for(float end=Time.unscaledTime+1;!hero.CanAct&&Time.unscaledTime<end;)yield return null;
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator SwingsAloneCannotKeepAFullLoadoutOnCooldown()
        {
            Arena();yield return null;var victim=Dummy(new Vector3(200,0,2),DamageType.Kinetic);yield return Frames(2);
            var swing=3/(GroundMove.Cut(0).duration+GroundMove.Cut(1).duration+GroundMove.Cut(2).duration);var needle=1/GroundMove.Needle.duration;
            foreach(int level in new[]{3,8,11})
            {
                Sync(level);
                foreach(var hero in PartyController.Current.members)
                {
                    float spend=0;for(int slot=0;slot<4;slot++){var skill=SkillBook.Equipped(hero.character,slot);spend+=SkillBook.Cost(hero.character,skill)/skill.cooldown;}
                    bool taren=hero.character=="Taren";
                    hero.charge=0;hero.damageScale=1;
                    // Exactly what Attack() sends, with the type each weapon really has, onto a body that neither resists nor is weak to it.
                    victim.Health.Receive(taren?hero.Packet(hero.damage,DamageType.Kinetic,0):hero.Packet(hero.rangedDamage*.7f,DamageType.Beam,0));
                    float perHit=hero.charge,income=perHit*(taren?swing:needle);
                    Assert.Greater(perHit,0,"fixture: a hit pays Charge");
                    Assert.Less(income,spend,$"{hero.character}'s plain attacks earn {income:0.0} Charge/s at Sync {level} against {spend:0.0}/s for every skill on cooldown: Charge never limits a skill");
                }
            }
        }
        [UnityTest] public IEnumerator AWeakSpotDealsMoreButPaysNoMoreCharge()
        {
            Arena();Sync(8);yield return null;var taren=Lead("Taren");taren.damageScale=1;
            var victim=Dummy(new Vector3(200,0,2),DamageType.Kinetic);yield return Frames(2);
            taren.charge=0;victim.Health.Receive(new DamagePacket{source=taren.Health,amount=taren.damage*.5f,type=DamageType.Kinetic});float plain=taren.charge;
            taren.charge=0;victim.Health.Receive(new DamagePacket{source=taren.Health,amount=taren.damage*.5f,type=DamageType.Kinetic},2);float spot=taren.charge;
            Assert.Greater(plain,0,"fixture: a hit pays Charge");
            Assert.AreEqual(plain,spot,.01f,"a x2 hurtbox pays double Charge: on a collar link plain shots out-earn a full loadout");
        }
        [UnityTest] public IEnumerator AKillPaysCharge()
        {
            Arena();Sync(5);yield return null;var taren=Lead("Taren");taren.damageScale=1;
            var live=Dummy(new Vector3(200,0,2),DamageType.Kinetic);var dying=Enemy("Ridgehound",new Vector3(204,0,2));dying.Health.weakness=dying.Health.resistance=DamageType.Pulse;
            yield return Frames(2);dying.Health.maximum=dying.Health.integrity=1;
            taren.charge=0;live.Health.Receive(new DamagePacket{source=taren.Health,amount=taren.damage,type=DamageType.Kinetic});float hit=taren.charge;
            taren.charge=0;dying.Health.Receive(new DamagePacket{source=taren.Health,amount=taren.damage,type=DamageType.Kinetic});float kill=taren.charge;
            Assert.Greater(kill,hit*1.5f,"a kill pays no more than any other hit: with the hit award down to where a boss can't be spammed, an ordinary fight would run short of Charge");
        }

        // ------------------------------------------------------------------ the companion
        [UnityTest] public IEnumerator TheCompanionDoesNotHookItsHullIntoAWall()
        {
            yield return Zone("Arena_Flight");
            var taren=Taren;Place(taren,new Vector3(200,1,0));Place(Sela,new Vector3(260,1,0));yield return Frames(2);
            var nose=taren.motor.Facing;var side=Vector3.Cross(Vector3.up,nose).normalized;
            var cantor=Enemy("Cantor",taren.transform.position+nose*9);yield return Frames(2);taren.target=cantor.Health;
            var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.transform.position=taren.transform.position+nose*4+side*1.2f+Vector3.up;post.transform.localScale=new Vector3(.4f,6,.4f);Physics.SyncTransforms();
            Assert.IsNotNull(WorthwhileMethod,"fixture: the companion's skill check");
            Assert.IsFalse(Worthwhile(taren,SkillKind.TetherHook),"a hook on the Cantor pulls the hull about 7 m and nothing checks the path: the companion rams the post for 12 integrity");
            var dart=Enemy("ChoristerDart",taren.transform.position+nose*9+side*.5f);yield return Frames(2);taren.target=dart.Health;
            Assert.IsTrue(Worthwhile(taren,SkillKind.TetherHook),"fixture: a dart is reeled in, so the hull goes nowhere");
            taren.target=cantor.Health;Object.Destroy(post);yield return null;Physics.SyncTransforms();
            Assert.IsTrue(Worthwhile(taren,SkillKind.TetherHook),"fixture: with the post gone the hook is fine");
        }
        [UnityTest] public IEnumerator TheCompanionKeepsItsChargeWhileMovingAwayFromItsTarget()
        {
            Arena();Sync(12);var taren=Taren;Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var behind=Dummy(new Vector3(200,0,-5));yield return Frames(2);taren.target=behind.Health;
            Assert.IsFalse(Worthwhile(taren,SkillKind.Faultline),"its target is in the 120 degree arc behind it, so Aim goes along its heading: a directional skill spent into empty space");
            Assert.IsFalse(Worthwhile(taren,SkillKind.AnchorSlam),"it leaps away from its target with the Charge paid");
            Assert.IsTrue(Worthwhile(taren,SkillKind.PulseBurst),"fixture: a burst goes all round, so it doesn't care");
            // A Fold or a Starfall lands on the target itself, wherever she faces: retreating is when a placed skill is most use.
            var sela=Sela;Place(sela,new Vector3(206,0,0));sela.target=behind.Health;yield return null;
            Assert.IsFalse(Worthwhile(sela,SkillKind.ThreadLance),"fixture: her lance leaves along her heading, with the target behind her");
            Assert.IsTrue(Worthwhile(sela,SkillKind.GravityFold),"a companion moving away from its target refuses a Fold, which lands on the target whichever way she faces");
            Assert.IsTrue(Worthwhile(sela,SkillKind.Starfall),"a companion moving away from its target refuses a Starfall, which lands on the target whichever way she faces");
            behind.transform.position=new Vector3(200,0,5);Physics.SyncTransforms();yield return null;
            Assert.IsTrue(Worthwhile(taren,SkillKind.Faultline),"fixture: with the target in front it is fine");
        }
        [UnityTest] public IEnumerator ACompanionShortOfChargeLeavesTheSkillIconsAlone()
        {
            Arena();Sync(12);var taren=Lead("Taren");var sela=Sela;Place(taren,new Vector3(200,0,0));Place(sela,new Vector3(200,0,-4));
            var victim=Dummy(new Vector3(200,0,4));yield return Frames(2);taren.target=victim.Health;
            System.Array.Clear(sela.SkillRefusedAt,0,4);sela.GetComponent<PartnerBrain>().enabled=true;
            // Its first skill waits three seconds: this runs past that, with nothing to spend.
            for(float end=Time.unscaledTime+3.5f;Time.unscaledTime<end;){sela.charge=0;yield return null;}
            Assert.AreEqual(0,sela.SkillRefusedAt.Max(),.001f,"the companion, short of Charge, retries a skill every frame and each refusal stamps its slot: swap to it and the icon flashes red with the error sound, no press made");
            // The same brain with Charge to spend: it was trying.
            sela.charge=100;
            for(float end=Time.unscaledTime+1;sela.cooldowns.All(c=>c<=0)&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(sela.cooldowns.Any(c=>c>0),"fixture: the companion casts once it has the Charge");
        }

        // ------------------------------------------------------------------ openings and conversations
        [UnityTest] public IEnumerator AnOpeningLeavesTheBreakGaugeAlone()
        {
            Arena();var taren=Taren;var hound=Enemy("Ridgehound",new Vector3(200,0,2));yield return Frames(2);
            var body=hound.Health;body.maximum=body.integrity=100000;
            body.Receive(new DamagePacket{source=taren.Health,amount=1,breakPower=30,type=DamageType.Beam});float gauge=body.BreakMeter;
            Assert.Greater(gauge,0,"fixture: some break built up");Assert.IsFalse(body.Broken,"fixture: not broken yet");
            body.Expose(.2f);
            Assert.AreEqual(gauge,body.BreakMeter,.01f,"a hook, a relit relay or a boss's opening empties the gauge the player built up, against Expose's own promise");
            Assert.IsTrue(body.Broken,"fixture: exposed");
            yield return new WaitForSecondsRealtime(.3f);
            body.Receive(new DamagePacket{source=taren.Health,amount=1,breakPower=body.breakThreshold,type=DamageType.Beam});
            Assert.IsTrue(body.Broken,"fixture: a real break");Assert.AreEqual(0,body.BreakMeter,.01f,"a real break still spends the gauge");
        }
        [UnityTest] public IEnumerator StaticNetAndCooldownsWaitOutAConversation()
        {
            Arena();var sela=Sela;Place(sela,new Vector3(200,0,0));Place(Taren,new Vector3(260,0,0));
            var victim=Dummy(new Vector3(200,0,2));yield return Frames(2);float before=victim.Health.integrity;
            sela.cooldowns[0]=3;
            GameServices.Current.Input.Blocked=true;
            NetSeed.Land(new Vector3(200,0,2),sela.Packet(10,DamageType.Pulse,0));
            yield return new WaitForSecondsRealtime(2.1f);
            float frozen=victim.Health.integrity,cooldown=sela.cooldowns[0];
            GameServices.Current.Input.Blocked=false;
            Assert.AreEqual(before,frozen,.01f,"a Static Net ticks damage on an enemy frozen by a conversation");
            Assert.AreEqual(3,cooldown,.01f,"skill cooldowns run out during the lines");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.Less(victim.Health.integrity,before,"fixture: the net works once the conversation ends");
            Assert.Less(sela.cooldowns[0],3,"fixture: the cooldown runs again");
        }

        // ------------------------------------------------------------------ the body turns
        [UnityTest] public IEnumerator EveryAimedGroundSkillTurnsTheBody()
        {
            Arena();var sela=Sela;Place(Taren,new Vector3(260,0,0));
            var victim=Dummy(new Vector3(206,0,0));yield return Frames(2);
            for(int slot=0;slot<3;slot++)
            {
                Place(sela,new Vector3(200,0,0));sela.target=victim.Health;sela.charge=100;sela.cooldowns[slot]=0;yield return null;
                var skill=SkillBook.Equipped("Sela",slot);
                Assert.IsTrue(sela.Skill(slot),"fixture: "+skill.displayName);
                yield return new WaitForSecondsRealtime(.15f);
                Assert.Less(Vector3.Angle(sela.transform.forward,Vector3.right),15,$"{skill.displayName} leaves with her body still turned away from a target at her side");
                for(float end=Time.unscaledTime+2;!sela.CanAct&&Time.unscaledTime<end;)yield return null;
            }
        }

        // ------------------------------------------------------------------ a Flash is earned
        [UnityTest] public IEnumerator MashingDodgeEvadesButEarnsNoFlashAndIsPaidFor()
        {
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));yield return Frames(2);
            float before=taren.Health.integrity;DamagePacket Incoming()=>new DamagePacket{amount=20,type=DamageType.Pulse};
            var bar=Object.FindFirstObjectByType<GameHud>().GetComponentsInChildren<UnityEngine.UI.Image>(true).First(i=>i.name=="Thrust");
            taren.thrust=100;
            Assert.IsTrue(taren.Dodge(Vector3.right),"fixture: the first roll");float first=Time.unscaledTime;
            bool again=false;
            for(float end=first+.44f;!again&&Time.unscaledTime<end;){again=taren.Dodge(Vector3.left);if(!again)yield return null;}
            Assert.IsTrue(again,"fixture: the second roll of a mash, inside .45 s of the first");
            float second=Time.unscaledTime;
            taren.Health.Receive(Incoming());
            Assert.AreEqual(0,taren.FlashMoves,"a hit in a mashed roll still pays a 1.5 s invulnerable Flash: mashing beats timing");
            Assert.AreEqual(1f,Time.timeScale,.001f,"a mashed roll slowed the world");
            Assert.AreEqual(before,taren.Health.integrity,.01f,"the roll itself must still evade");
            Assert.Less(taren.thrust,100,"a mashed roll costs nothing: i-frames 71% of the time, free");
            // The bar empty, a mash stops for as long as it goes on, and the bar says why. Well past .45 s from the last roll: if a refused press
            // left the clock alone, an empty bar would only space the mash out into free rolls that earn their Flash, on a third of all hits.
            bool flashed=false;
            for(float end=second+.8f;Time.unscaledTime<end;)
            {
                taren.thrust=0;
                Assert.IsFalse(taren.Dodge(Vector3.right),$"{Time.unscaledTime-second:0.00} s into a mash on an empty bar the roll goes, free: mashing still pays");
                yield return null;
                flashed|=bar.color.g<.5f;
            }
            Assert.IsTrue(flashed,"a roll refused for Thrust says nothing: the hero just stands there with B pressed");
            // A roll in its own time still pays, and it is free on an empty bar.
            yield return new WaitForSecondsRealtime(.7f);
            Assert.Greater(bar.color.g,.5f,"the Thrust bar stays red after the refusal has passed");
            taren.thrust=0;
            Assert.IsTrue(taren.Dodge(Vector3.right),"a roll in its own time is refused on an empty bar: dodging is meant to stay free");
            Assert.AreEqual(0,taren.thrust,.001f,"a roll in its own time cost Thrust: dodging is meant to stay free");
            taren.Health.Receive(Incoming());
            Assert.AreEqual(1,taren.FlashMoves,"a roll well after the last one didn't earn its Flash");
            Assert.Less(Time.timeScale,1f,"a timed Flash no longer slows the world");
        }
        [UnityTest] public IEnumerator MashingGuardStillBlocksButEarnsNoFlashGuard()
        {
            Arena();var taren=Lead("Taren");Place(taren,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));yield return Frames(2);
            DamagePacket Incoming()=>new DamagePacket{amount=20,type=DamageType.Pulse};
            taren.Guard(true);taren.Health.Receive(Incoming());taren.Guard(false);
            Assert.AreEqual(1,taren.FlashGuards,"fixture: the first press of a guard is a perfect one");
            yield return new WaitForSecondsRealtime(.1f);
            // X again inside .45 s of the last press: each press re-arms the .2 s window, with no lockout.
            taren.charge=0;float before=taren.Health.integrity;
            taren.Guard(true);taren.Health.Receive(Incoming());taren.Guard(false);
            Assert.AreEqual(1,taren.FlashGuards,"X tapped over and over makes every hit a perfect guard: no damage, a crit and 25 Charge for mashing");
            Assert.AreEqual(0,taren.charge,.5f,"a mashed guard paid the Flash Guard's Charge");
            float taken=before-taren.Health.integrity;
            Assert.Greater(taken,0,"a mashed guard stopped the whole hit");
            Assert.Less(taken,10,"a mashed guard stopped blocking: it has to keep the guard's 65%");
            // A press in its own time is still perfect.
            yield return new WaitForSecondsRealtime(.7f);
            taren.Guard(true);taren.Health.Receive(Incoming());taren.Guard(false);
            Assert.AreEqual(2,taren.FlashGuards,"a guard pressed in its own time is no longer perfect");
        }

        // ------------------------------------------------------------------ chapter 2 calls for chapter 2's gear
        static float Output(string hero,int sync,string part)=>Levels.Calculate(GameCatalog.Find<CharacterDef>(hero),sync).output+GameCatalog.Find<TechPartDef>(part).baseStats.output;
        static int EdgeHits(EnemyDef enemy,float output)
        {
            float left=enemy.integrity;int hits=0;
            while(left>.001f&&hits<99){left-=DamageMath.Resolve(output*(hits%3==2?1.5f:1),DamageType.Kinetic,enemy.weakness,enemy.resistance,false,false);hits++;}
            return hits;
        }
        static int NeedleHits(EnemyDef enemy,float output)
        {
            float left=enemy.integrity;int hits=0;
            while(left>.001f&&hits<99){left-=DamageMath.Resolve(output*.7f,DamageType.Beam,enemy.weakness,enemy.resistance,false,false);hits++;}
            return hits;
        }
        [UnityTest] public IEnumerator ChapterTwosEnemiesCallForRefinedGearWithoutGettingLong()
        {
            yield return null;
            foreach(int sync in new[]{8,9})
            {
                var grazer=GameCatalog.Find<EnemyDef>("Gloomgrazer");
                int edgeOld=EdgeHits(grazer,Output("Taren",sync,"EdgesT1")),edgeNew=EdgeHits(grazer,Output("Taren",sync,"EdgesT2"));
                int needleOld=NeedleHits(grazer,Output("Sela",sync,"EmittersT1")),needleNew=NeedleHits(grazer,Output("Sela",sync,"EmittersT2"));
                Assert.Greater(edgeOld,edgeNew,$"a Grazer at Sync {sync} takes {edgeOld} hits with a Scrap Edge and {edgeNew} with a Refined one: chapter 2 doesn't call for T2 gear");
                Assert.Greater(needleOld,needleNew,$"a Grazer at Sync {sync} takes {needleOld} needles with a Scrap Emitter and {needleNew} with a Refined one");
                Assert.LessOrEqual(edgeOld,4,$"a pack enemy takes {edgeOld} Scrap Edge hits at Sync {sync}: ordinary fights in chapter 2 get long");
                Assert.LessOrEqual(edgeNew,3,$"a pack enemy takes {edgeNew} Refined Edge hits at Sync {sync}: ordinary fights in chapter 2 get long");
                foreach(string id in new[]{"MusterScav","MusterSergeant"})
                {
                    var enemy=GameCatalog.Find<EnemyDef>(id);
                    Assert.Greater(EdgeHits(enemy,Output("Taren",sync,"EdgesT1")),EdgeHits(enemy,Output("Taren",sync,"EdgesT2")),$"{id} at Sync {sync} takes the same hits with T1 and T2");
                }
            }
            string builder=File.ReadAllText(Path.Combine(Application.dataPath,"_Project/Scripts/Editor/World/LanternRoadBuilder.cs"));
            var written=Regex.Match(builder,"e\\.id=\"Gloomgrazer\".*?e\\.integrity=(\\d+)");
            Assert.IsTrue(written.Success,"fixture: the builder writes the Grazer");
            Assert.AreEqual(GameCatalog.Find<EnemyDef>("Gloomgrazer").integrity,float.Parse(written.Groups[1].Value),.01f,"a rebuild would put the Grazer's old integrity back");
        }
    }
}
