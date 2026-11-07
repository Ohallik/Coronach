using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Batch 9's fair track, the items that had no red check: what an enemy's strike does at a wall, what a rolled band or a held guard costs, whose shield a resisted hit
    /// raises, what a companion's wait and a Flash Guard's crit are worth, and what Hessa's called crew hit for. Each failure says what a player would see go wrong.</summary>
    public sealed class QaFairTests
    {
        readonly List<string> log=new();
        void Logged(string text,string stack,LogType type){if(text.StartsWith("SKILL_KIND "))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");log.Clear();Application.logMessageReceived+=Logged;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static CombatActor Taren=>PartyController.Current.members.First(m=>m.character=="Taren");
        static CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        static void Quiet(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Quiet();
            yield return new WaitForSecondsRealtime(.7f);
        }
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}
            else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            Physics.SyncTransforms();
        }
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Fair fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        /// <summary>Solid, and not marked static: a built player keeps no such flag.</summary>
        static GameObject Wall(Vector3 center,Vector3 size)
        {var wall=new GameObject("Fair fixture wall",typeof(BoxCollider));wall.transform.position=center;wall.GetComponent<BoxCollider>().size=size;Physics.SyncTransforms();return wall;}
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=passive;return enemy;}
        static EnemyBrain Dummy(string id,Vector3 at){var enemy=Enemy(id,at);var body=enemy.Health;body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;return enemy;}
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
        static CombatActor Other(CombatActor hero)=>PartyController.Current.members.First(m=>m!=hero);
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static void Fresh(CombatActor actor){actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;}
        static DamageType Neutral(Health body)=>System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
        static IEnumerator ToATell(EnemyBrain body,string what)
        {
            for(float end=Time.unscaledTime+3;!body.Telegraphing&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(body.Telegraphing,"fixture: "+what+" told an attack");
        }

        // ------------------------------------------------------------------ item 1: a blade and the wall the hero is pressed against
        /// <summary>The posed wrist is past a thin wall before the swing lands: from the hand on there's nothing in the way.</summary>
        [UnityTest] public IEnumerator ABladeStopsAtAWallTheHeroIsPressedAgainst()
        {
            yield return Zone("Arena_Ground");Arena();var taren=Lead("Taren");Place(Sela,new Vector3(260,0,0));Place(taren,new Vector3(200,0,0));
            var wall=Wall(new Vector3(200,2.5f,.5f),new Vector3(14,7,.1f));
            var body=Dummy("Ridgehound",new Vector3(200,0,1.2f));yield return Frames(3);taren.target=body.Health;
            float landed=0;body.Health.Damaged+=(_,packet,amount)=>{if(packet.source==taren.Health)landed+=amount;};
            for(int swing=0;swing<3;swing++){Assert.IsTrue(taren.Attack(),"fixture: the swing");yield return new WaitForSecondsRealtime(.7f);}
            Assert.AreEqual(0,landed,.01f,"a hero pressed against a thin wall cuts the body behind it: his wrist is already through");
            Object.Destroy(wall);yield return Frames(2);Physics.SyncTransforms();
            for(int swing=0;swing<3&&landed<=0;swing++){Assert.IsTrue(taren.Attack(),"fixture: the swing");yield return new WaitForSecondsRealtime(.7f);}
            Assert.Greater(landed,0,"fixture: with the wall gone the swing reaches, or the line above proves nothing");
        }
        [UnityTest] public IEnumerator TheSwapStrikeStopsAtAWall()
        {
            yield return Zone("Arena_Ground");Arena();var taren=Lead("Taren");Place(taren,new Vector3(203,0,-2));Place(Sela,new Vector3(200,0,0));
            var wall=Wall(new Vector3(200,2.5f,1),new Vector3(14,7,.3f));
            var behind=Dummy("Ridgehound",new Vector3(200,0,2));var near=Dummy("Ridgehound",new Vector3(200,0,-2));yield return Frames(3);
            Assert.IsTrue(PartyController.Current.Swap(),"fixture: the swap");Quiet();
            yield return new WaitForSecondsRealtime(.4f);
            Assert.Less(near.Health.integrity,near.Health.maximum,"fixture: the strike went off");
            Assert.AreEqual(behind.Health.maximum,behind.Health.integrity,.01f,"a swap's strike reaches through a wall");
            Object.Destroy(wall);
        }

        // ------------------------------------------------------------------ item 2: what an enemy strikes with
        /// <summary>The tell starts with a clear view. Then a door shuts, or the hero steps round the corner: the bite, the lunge and the blast all stop at it.</summary>
        [UnityTest] public IEnumerator AnEnemysStrikeStopsAtAWallThatComesBetweenInItsTell()
        {
            yield return Zone("Arena_Ground");Arena();var hero=Taren;Place(Sela,new Vector3(260,0,0));var failures=new List<string>();
            foreach(var (id,wallAt) in new[]{("SentinelHusk",1.05f),("Ridgehound",1.5f)})
            {
                Place(hero,new Vector3(200,0,2.2f));Fresh(hero);
                var body=Enemy(id,new Vector3(200,0,0));yield return new WaitForSecondsRealtime(.7f);body.Passive=false;
                yield return ToATell(body,id);
                var wall=Wall(new Vector3(200,2.5f,wallAt),new Vector3(14,7,.3f));float before=hero.Health.integrity;
                yield return new WaitForSecondsRealtime(1.4f);
                if(hero.Health.integrity<before)failures.Add($"a {id}'s strike reaches a hero behind the wall that came between them: {before-hero.Health.integrity:0} integrity");
                body.Passive=true;Object.Destroy(wall);Object.Destroy(body.gameObject);yield return Frames(3);
            }
            // A Scrapmite's burst: its marked circle reaches him, and a wall stands in it.
            Place(hero,new Vector3(200,0,1.5f));Fresh(hero);var shield=Wall(new Vector3(200,2.5f,.75f),new Vector3(14,7,.3f));
            float whole=hero.Health.integrity;DeathBlast.Arm(new Vector3(200,0,0),new DamagePacket{amount=12,type=DamageType.Pulse,deathAttack=true});
            yield return new WaitForSecondsRealtime(.8f);
            if(hero.Health.integrity<whole)failures.Add("a dying Scrapmite's burst reaches a hero behind a wall");
            Object.Destroy(shield);yield return Frames(2);Physics.SyncTransforms();
            DeathBlast.Arm(new Vector3(200,0,0),new DamagePacket{amount=12,type=DamageType.Pulse,deathAttack=true});
            yield return new WaitForSecondsRealtime(.8f);
            if(hero.Health.integrity>=whole)failures.Add("fixture: with the wall gone the burst reaches him, or the line above proves nothing");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>A corner he can be seen past isn't cover: the look and the strike both go by it.</summary>
        [UnityTest] public IEnumerator AnEnemyStillBitesRoundACornerItCanSeePast()
        {
            yield return Zone("Arena_Ground");Arena();var hero=Taren;Place(Sela,new Vector3(260,0,0));Place(hero,new Vector3(199.5f,0,2));Fresh(hero);
            // Ends at x=200: the line from the Husk to his chest goes by it .75 m to the side.
            var wall=Wall(new Vector3(203,2.5f,1),new Vector3(6,7,.3f));
            var body=Enemy("SentinelHusk",new Vector3(199,0,0));yield return new WaitForSecondsRealtime(.7f);
            float before=hero.Health.integrity;body.Passive=false;
            for(float end=Time.unscaledTime+3;hero.Health.integrity>=before&&Time.unscaledTime<end;)yield return null;
            body.Passive=true;Object.Destroy(wall);
            Assert.Less(hero.Health.integrity,before,"a Husk with a clear view of a hero round the end of a wall doesn't bite him: the wall took a body it isn't between");
        }
        /// <summary>Enemies walk straight at the hero. One that loses him behind a pillar, inside the range it would stop at, has to walk on or it stands there until he moves.</summary>
        [UnityTest] public IEnumerator ABodyWithNoLineWalksOnUntilItHasOne()
        {
            yield return Zone("Arena_Ground");Arena();var hero=Taren;Place(Sela,new Vector3(260,0,0));Place(hero,new Vector3(200,0,4));Fresh(hero);
            var pillar=Wall(new Vector3(200,2.5f,2),new Vector3(1,7,.3f));
            var body=Enemy("Ridgehound",new Vector3(200,0,0));yield return new WaitForSecondsRealtime(.7f);
            body.Passive=false;
            // A hound's sideways weave takes it past a pillar this wide within one swing of it, whichever way it starts.
            for(float end=Time.unscaledTime+7;!body.Telegraphing&&Time.unscaledTime<end;)yield return null;
            bool told=body.Telegraphing;body.Passive=true;Object.Destroy(pillar);
            Assert.IsTrue(told,"a hound 4 m from a hero with a pillar between them stands there for good: it's inside the range it stops at, and it never gets a line");
        }
        [UnityTest] public IEnumerator AMinesBlastStopsAtAWall()
        {
            yield return Zone("Arena_Flight");var hero=Taren;Place(Sela,new Vector3(260,1,0));
            Place(hero,new Vector3(200,1,2.6f));Fresh(hero);
            var mine=Enemy("Shellmine",new Vector3(200,1,0));yield return new WaitForSecondsRealtime(.4f);mine.Passive=false;
            yield return ToATell(mine,"the mine");
            // Still inside the 3 m blast by the hull's edge, with a wall between: and then the same with nothing in the way.
            var wall=Wall(new Vector3(200,4,2.2f),new Vector3(14,20,.2f));Place(hero,new Vector3(200,1,4.3f));Fresh(hero);float before=hero.Health.integrity;
            yield return new WaitForSecondsRealtime(1.4f);
            Assert.AreEqual(before,hero.Health.integrity,.01f,"a mine's blast reaches a hull behind a wall");
            Object.Destroy(wall);yield return Frames(2);Physics.SyncTransforms();
            Place(hero,new Vector3(200,1,2.6f));Fresh(hero);
            var second=Enemy("Shellmine",new Vector3(200,1,0));yield return new WaitForSecondsRealtime(.4f);second.Passive=false;
            yield return ToATell(second,"the second mine");
            Place(hero,new Vector3(200,1,4.3f));Fresh(hero);before=hero.Health.integrity;
            yield return new WaitForSecondsRealtime(1.4f);
            Assert.Less(hero.Health.integrity,before,"fixture: with no wall the blast reaches the hull, or the line above proves nothing");
        }

        // ------------------------------------------------------------------ item 4: what a band he rolled through cost him
        /// <summary>He rolls the first band and stands for the rest. A rolled band used to spend the cast's one hit, so the other two passed through him.</summary>
        [UnityTest] public IEnumerator ARolledBandDoesNotSpendTheCastsOneHit()
        {
            yield return Zone("Arena_Ground");Arena();var hero=Taren;Place(hero,new Vector3(200,.1f,9));Place(Sela,new Vector3(260,.1f,0));Fresh(hero);
            var enemy=Enemy("Burrower",new Vector3(200,.1f,0));yield return Frames(3);var boss=enemy.GetComponent<BossController>();
            int struck=0;hero.Health.Damaged+=(_,__,amount)=>{if(amount>0)struck++;};bool rolled=false;
            Assert.IsTrue(boss.Perform("SeismicRings"),"fixture: the rings");
            for(float end=Time.unscaledTime+15;boss.Busy&&Time.unscaledTime<end;)
            {
                // A band's tell, not the slam's: the slam stops 6 m short of him.
                if(!rolled&&boss.Telegraphing&&boss.TelegraphRemaining<.07f&&Telegraph.Live.Any(t=>t!=null&&t.owner==boss&&t.Shape==Telegraph.Kind.Ring))rolled=hero.Dodge(Vector3.right);
                yield return null;
            }
            Assert.IsTrue(rolled,"fixture: he rolled the first band");
            Assert.AreEqual(1,struck,"he rolled one band and stood for the others, and one of them should have landed, once: a rolled band spends the cast's one hit, or they chain");
        }

        // ------------------------------------------------------------------ item 11: the roll wins, and the guard is still there after it
        [UnityTest] public IEnumerator AGuardStillHeldTakesUpWhenTheRollEnds()
        {
            yield return Zone("Arena_Ground");Arena();var taren=Lead("Taren");Place(Sela,new Vector3(260,0,0));Place(taren,new Vector3(200,0,0));Fresh(taren);yield return Frames(2);
            IEnumerator Hold(float seconds){for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){taren.motor.Move(Vector2.zero,false,false);taren.Guard(true);yield return null;}}
            yield return Hold(.5f);
            Assert.AreEqual(ActorState.Guard,taren.State,"fixture: the guard is up");
            Assert.IsTrue(taren.Dodge(Vector3.right),"fixture: the roll");taren.Guard(true);
            Assert.AreEqual(ActorState.Dodge,taren.State,"X held writes the guard straight over the roll");
            yield return Hold(.5f);
            Assert.AreEqual(ActorState.Guard,taren.State,"a guard still held doesn't take up again when the roll ends: X is down and nothing is blocking");
            float before=taren.Health.integrity;taren.Health.Receive(new DamagePacket{amount=20,type=DamageType.Pulse});float taken=before-taren.Health.integrity;
            Assert.AreEqual(0,taren.FlashGuards,"a roll handed a guard held all along a new Flash Guard window");
            Assert.Greater(taken,0,"fixture: the hit landed");
            Assert.Less(taken,10,"the guard that took up after the roll isn't blocking");
        }

        // ------------------------------------------------------------------ item 9: whose shield it is
        [UnityTest] public IEnumerator AResistedHitRaisesOnlyTheHusksOwnShield()
        {
            yield return Zone("Arena_Ground");Arena();var taren=Taren;Place(taren,new Vector3(260,0,0));Place(Sela,new Vector3(263,0,0));yield return Frames(2);
            var failures=new List<string>();
            foreach(var (id,shielded) in new[]{("SentinelHusk",true),("MusterSergeant",false),("HessaKorl",false)})
            {
                var enemy=Enemy(id,new Vector3(200,.1f,0));yield return Frames(3);
                var body=enemy.Health;body.breakThreshold=1e9f;body.maximum=body.integrity=1000000;
                DamagePacket Hit(DamageType type)=>new DamagePacket{source=taren.Health,amount=100,type=type};
                float clean=body.Receive(Hit(Neutral(body)));
                body.Receive(Hit(body.resistance));
                float after=body.Receive(Hit(Neutral(body)));
                if(shielded&&after>clean*.75f)failures.Add($"a resisted hit no longer raises the Husk's shield: {after:0} where a clean hit deals {clean:0}");
                if(!shielded&&Mathf.Abs(after-clean)>1)failures.Add($"one resisted hit on a {id} halves the whole party's damage on it for two seconds: {after:0} where a clean hit deals {clean:0}");
                Object.Destroy(enemy.gameObject);yield return Frames(2);
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ items 8 and 12: what a re-intake and a rebuild would put back
        [UnityTest] public IEnumerator ARebuildKeepsTheBossesAndAReIntakeKeepsTheMustersFeet()
        {
            yield return null;
            string Source(string path)=>System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"_Project/Scripts/Editor/"+path));
            float Asset(string id)=>GameCatalog.Find<EnemyDef>(id).integrity;
            var arena=System.Text.RegularExpressions.Regex.Match(Source("ArenaBuilder.cs"),@"e\.integrity=e\.boss\?\(n==3\?(\d+):(\d+)\)");
            Assert.IsTrue(arena.Success,"fixture: the arena builder writes the Burrower and the Cantor");
            Assert.AreEqual(Asset("Burrower"),float.Parse(arena.Groups[1].Value),.01f,"a rebuild would put the Burrower's old integrity back");
            Assert.AreEqual(Asset("Cantor"),float.Parse(arena.Groups[2].Value),.01f,"a rebuild would put the Cantor's old integrity back");
            StringAssert.Contains($"e.integrity={Asset("BellowsBelow"):0};",Source("World/HushwellBuilder.cs"),"a rebuild would put the Bellows' old integrity back");
            StringAssert.Contains($"e.integrity={Asset("HessaKorl"):0};",Source("World/LanternRoadBuilder.cs"),"a rebuild would put Hessa's old integrity back");
            StringAssert.Contains("!LifecycleCalibration.HuskRig(row)",Source("Art/GenIntake.cs"),"a re-intake turns Foot IK back on for the Muster and Hessa, whose corpses then sink .3 m through the floor");
            foreach(string model in new[]{"MusterScav","MusterSergeant","HessaKorl"})
                StringAssert.DoesNotContain("m_IKOnFeet: 1",System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,$"_Project/Art/Generated/Models/{model}/{model}.controller")),model+"'s controller has Foot IK on, and the rig is calibrated with it off");
        }

        // ------------------------------------------------------------------ items 7 and 10: the companion's wait, the crew, the crit
        [UnityTest] public IEnumerator ASwapAndBackDoesNotHurryTheCompanionsNextSkill()
        {
            yield return Zone("Arena_Ground");Arena();Sync(12);
            var lead=Lead("Taren");var partner=Other(lead);Place(lead,new Vector3(200,0,0));Place(partner,new Vector3(205,0,0));
            var body=Dummy("Ridgehound",new Vector3(200,0,7));yield return Frames(3);lead.target=body.Health;
            bool Cast()=>log.Any(l=>l.StartsWith("SKILL_KIND "+partner.character));
            log.Clear();partner.GetComponent<PartnerBrain>().enabled=true;
            for(float end=Time.unscaledTime+9;!Cast()&&Time.unscaledTime<end;){partner.charge=100;lead.target=body.Health;yield return null;}
            Assert.IsTrue(Cast(),"fixture: the companion used a skill");
            float cast=Time.unscaledTime;log.Clear();
            yield return new WaitForSecondsRealtime(.5f);
            var party=PartyController.Current;
            Assert.IsTrue(party.Swap(),"fixture: the swap");yield return Frames(2);
            Assert.IsTrue(party.Swap(),"fixture: the swap back");yield return Frames(2);
            log.Clear();
            for(float end=cast+5.8f;Time.unscaledTime<end;){partner.charge=100;lead.target=body.Health;yield return null;}
            Assert.IsFalse(Cast(),"swapping out and back lets the companion cast again inside its six seconds: the player's own skills are meant to lead");
        }
        [UnityTest] public IEnumerator WhatHessaCallsHitsAsHardAsTheCrewInTheRoom()
        {
            float threat=GameCatalog.Find<ZoneDef>("SunkenRelay").enemyThreat;Assert.Greater(threat,1,"fixture: the relay's ordinary enemies hit above scale");
            yield return Zone("SunkenRelay");
            foreach(var volume in Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None))volume.enabled=false;
            var hero=PartyController.Current.Active;Fresh(hero);Fresh(Other(hero));
            var captain=Enemy("HessaKorl",hero.transform.position+Vector3.forward*8);yield return Frames(3);var boss=captain.GetComponent<BossController>();
            // Only what she calls: a crewman a spawner stood in the room has 1.7 already, and would pass this for her.
            var room=new HashSet<EnemyBrain>(Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None));
            Assert.IsTrue(boss.Perform("PhaseShift"),"fixture: the shift");
            EnemyBrain[] Crew()=>Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.definition.id=="MusterScav"&&!room.Contains(e)).ToArray();
            for(float end=Time.unscaledTime+3;Crew().Length==0&&Time.unscaledTime<end;)yield return null;
            var crew=Crew();Assert.IsNotEmpty(crew,"fixture: her shift called the crew");
            foreach(var scav in crew){scav.Passive=true;Assert.AreEqual(threat,scav.DamageScale,.01f,$"what she calls hits at {scav.DamageScale:0.0} where the crew already in the room hit at {threat:0.0}");}
        }
        [UnityTest] public IEnumerator ASwingThatCutsAirKeepsAFlashGuardsCrit()
        {
            yield return Zone("Arena_Ground");Arena();var taren=Lead("Taren");Place(Sela,new Vector3(260,0,0));Place(taren,new Vector3(200,0,0));yield return Frames(2);
            taren.Guard(true);taren.Health.Receive(new DamagePacket{amount=20,type=DamageType.Pulse});taren.Guard(false);
            Assert.AreEqual(1,taren.FlashGuards,"fixture: a perfect guard");
            Assert.IsTrue(taren.Attack(),"fixture: a swing at nothing");
            yield return new WaitForSecondsRealtime(1);
            var body=Dummy("Ridgehound",new Vector3(200,0,1.2f));yield return Frames(3);taren.target=body.Health;
            var hits=new List<bool>();body.Health.Damaged+=(_,packet,__)=>hits.Add(packet.isCrit);
            Assert.IsTrue(taren.Attack(),"fixture: the swing that lands");
            for(float end=Time.unscaledTime+1.2f;hits.Count==0&&Time.unscaledTime<end;)yield return null;
            Assert.IsNotEmpty(hits,"fixture: the swing landed");
            Assert.IsTrue(hits[0],"a swing that cut air spent the crit a Flash Guard earned: the next hit that lands should have it");
            // And it's spent now, not kept for good: three packets in a row can't all be the chance of a crit.
            int crits=0;
            for(int i=0;i<3;i++){yield return new WaitForSecondsRealtime(.35f);if(taren.Packet(1,DamageType.Pulse,0).isCrit)crits++;}
            Assert.Less(crits,3,"the crit a Flash Guard earned is still there after the hit it was for");
        }
    }
}
