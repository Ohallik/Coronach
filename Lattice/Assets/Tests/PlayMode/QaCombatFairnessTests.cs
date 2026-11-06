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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The November QA pass on combat: any hit that lands was marked and dodgeable, an opening rewards the punish, and
    /// nothing free can be spammed. Every check here failed on the build the pass started from.</summary>
    public sealed class QaCombatFairnessTests
    {
        Gamepad pad;
        CombatActor Taren=>PartyController.Current.members.First(m=>m.character=="Taren");
        CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Ground");
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture load timed out");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.3f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);pad=null;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static void Place(CombatActor actor,Vector3 position)
        {var body=actor.GetComponent<CharacterController>();body.enabled=false;actor.transform.position=position;body.enabled=true;Physics.SyncTransforms();}
        static EnemyBrain Enemy(string id,Vector3 at,bool passive=true){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=passive;return enemy;}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>The motor only carries out a dash while it is being driven, as the player's brain does every frame.</summary>
        static IEnumerator Drive(CombatActor actor,float seconds)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){actor.motor.Move(Vector2.zero,false,false);yield return null;}}
        static List<Telegraph> Marks(Object owner)=>Telegraph.Live.Where(t=>t!=null&&t.owner==owner).ToList();

        // ------------------------------------------------------------------ free damage
        [UnityTest] public IEnumerator TheSwapStrikeCannotBeMashed()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,30));Place(Sela,new Vector3(30.5f,0,30));
            var enemy=Enemy("SentinelHusk",new Vector3(31,0,30));yield return Frames(2);
            float before=enemy.Health.integrity;int hits=0;enemy.Health.Damaged+=(_,__,___)=>hits++;
            for(int i=0;i<10;i++){PartyController.Current.Swap();yield return Frames(2);}
            yield return new WaitForSecondsRealtime(.2f);
            Assert.LessOrEqual(hits,1,"ten swaps in a moment each struck for free");
            Object.Destroy(enemy.gameObject);
        }
        [UnityTest] public IEnumerator SwappingInAMenuQueuesNoStrikes()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,30));Place(Sela,new Vector3(30.5f,0,30));
            var enemy=Enemy("SentinelHusk",new Vector3(31,0,30));yield return Frames(2);
            float before=enemy.Health.integrity;
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open(PauseMenu.SkillsPage);yield return null;
            for(int i=0;i<6;i++){PartyController.Current.Swap();yield return null;}
            menu.Close();yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(before,enemy.Health.integrity,.01f,"strikes bought while paused all landed on resume");
            Object.Destroy(enemy.gameObject);
        }
        // ------------------------------------------------------------------ input
        IEnumerator Pad(GamepadButton[] down)
        {
            var state=new GamepadState();foreach(var b in down)state=state.WithButton(b);
            InputSystem.QueueStateEvent(pad,state);yield return null;yield return null;
        }
        [UnityTest] public IEnumerator LettingGoOfRbFirstDoesNotCancelTheSkill()
        {
            pad=InputSystem.AddDevice<Gamepad>("QaPad");yield return null;
            var taren=Taren;taren.GetComponent<PlayerBrain>().AutoPilot=false;taren.charge=100;Place(taren,new Vector3(30,0,30));
            var enemy=Enemy("SentinelHusk",new Vector3(31.5f,0,30));yield return Frames(3);
            float before=enemy.Health.integrity;
            yield return Pad(new[]{GamepadButton.RightShoulder});
            yield return Pad(new[]{GamepadButton.RightShoulder,GamepadButton.West});
            Assert.AreEqual(ActorState.Skill,taren.State,"fixture: RB+X released the slot's skill");int sequence=taren.AttackSequence;
            yield return Pad(new[]{GamepadButton.West}); // RB up first, X still down: fingers release in either order.
            Assert.AreEqual(sequence,taren.AttackSequence,"RB released first turned the skill into a Guard, and its Charge was already spent");
            Assert.AreNotEqual(ActorState.Guard,taren.State);
            yield return Pad(new GamepadButton[0]);
            Object.Destroy(enemy.gameObject);
        }
        [UnityTest] public IEnumerator ASkillPressedDuringASwingStillFires()
        {
            pad=InputSystem.AddDevice<Gamepad>("QaPad");yield return null;
            var taren=Taren;taren.GetComponent<PlayerBrain>().AutoPilot=false;taren.charge=100;Place(taren,new Vector3(30,0,30));yield return Frames(3);
            Assert.IsTrue(taren.Attack(),"fixture: a swing");yield return null;Assert.IsFalse(taren.CanAct,"fixture: mid swing");
            float charge=taren.charge;
            yield return Pad(new[]{GamepadButton.RightShoulder});yield return Pad(new[]{GamepadButton.RightShoulder,GamepadButton.South});
            yield return Pad(new GamepadButton[0]);
            float deadline=Time.unscaledTime+.6f;while(taren.charge>=charge-.5f&&Time.unscaledTime<deadline)yield return null;
            Assert.Less(taren.charge,charge-.5f,"a skill press during a swing was eaten: no buffer, no cue");
        }
        [UnityTest] public IEnumerator ARefusedSkillSaysSo()
        {
            pad=InputSystem.AddDevice<Gamepad>("QaPad");yield return null;
            var taren=Taren;taren.GetComponent<PlayerBrain>().AutoPilot=false;taren.charge=0;yield return Frames(3);
            yield return Pad(new[]{GamepadButton.RightShoulder});yield return Pad(new[]{GamepadButton.RightShoulder,GamepadButton.South});
            yield return Pad(new GamepadButton[0]);yield return Frames(2);
            Assert.Greater(taren.SkillRefusedAt[0],0,"a press with no Charge did nothing and said nothing");
        }
        // ------------------------------------------------------------------ flash
        [UnityTest] public IEnumerator ThePartnersDodgeDoesNotSlowTheWorld()
        {
            var party=PartyController.Current;var partner=party.members[1-party.index];
            Assert.IsTrue(partner.Dodge(Vector3.right),"fixture: the partner dodged");
            partner.Health.Receive(new DamagePacket{amount=20,type=DamageType.Pulse,source=Enemy("Scrapmite",new Vector3(60,0,60)).Health});
            yield return null;
            Assert.AreEqual(1f,Time.timeScale,.001f,"the AI partner's dodge put the player's hero in slow motion");
            Assert.AreEqual(1,partner.FlashMoves,"it is still a perfect evade for the partner");
        }
        [UnityTest] public IEnumerator FlashGuardShowsItself()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,30));yield return Frames(2);
            int cues=0;void Heard(string who,string line){cues++;}BarkService.Spoken+=Heard;
            taren.Guard(true);yield return null;
            taren.Health.Receive(new DamagePacket{amount=20,type=DamageType.Pulse,source=Enemy("Scrapmite",new Vector3(60,0,60)).Health});
            yield return null;BarkService.Spoken-=Heard;taren.Guard(false);
            Assert.AreEqual(1,taren.FlashGuards,"fixture: a perfect guard");
            Assert.Greater(taren.FlashGuardShownAt,0,"a perfect guard is silent and invisible");
        }
        [UnityTest] public IEnumerator LearnedSkillsReleaseOnTheHerosClockDuringFlash()
        {
            var sela=Sela;PartyController.Current.Swap();yield return Frames(2);
            sela.charge=100;sela.Health.InvulnerableUntil=GameTime.Now+1.5f;GameTime.BeginFlash();
            float start=Time.unscaledTime;
            GameServices.Current.State.party.Find(m=>m.id=="Sela").level=12;
            SkillBook.Equip(GameServices.Current.State.party.Find(m=>m.id=="Sela"),0,SkillBook.All("Sela").First(s=>s.kind==SkillKind.Starfall));
            Assert.IsTrue(sela.Skill(0),"fixture: Starfall cast");
            yield return new WaitForSecondsRealtime(.6f);
            Assert.IsNotNull(Object.FindFirstObjectByType<StarfallStrike>(),"during Flash a learned skill releases four times late, after the pose has ended");
            GameTime.EndFlash();
        }
        // ------------------------------------------------------------------ skills
        [UnityTest] public IEnumerator SkillStrikesStopAtWalls()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,30));
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(32,1.5f,30);wall.transform.localScale=new Vector3(.4f,3,8);Physics.SyncTransforms();
            var enemy=Enemy("SentinelHusk",new Vector3(33.2f,0,30));yield return Frames(2);float before=enemy.Health.integrity;
            var strike=CombatActor.Strike(new Vector3(31,.9f,30),3.2f,taren.Packet(100,DamageType.Kinetic,10));strike.ThroughWalls=false;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(before,enemy.Health.integrity,.01f,"a skill's strike reached through a solid wall");
            Object.Destroy(enemy.gameObject);Object.Destroy(wall);
        }
        [UnityTest] public IEnumerator ALethalHookDoesNotYankTarenToTheCorpse()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,30));yield return Frames(2);
            var enemy=Enemy("Scrapmite",new Vector3(38,0,30));yield return Frames(2);
            var packet=taren.Packet(100000,DamageType.Pulse,10);
            TetherLine.Cast(taren,taren.transform.position+Vector3.up*1.1f,Vector3.right,12,packet);
            yield return Drive(taren,.45f);
            Assert.IsFalse(enemy.Health.Alive,"fixture: the hook killed it");
            Assert.Less(Vector3.Distance(taren.transform.position,new Vector3(30,0,30)),.5f,"a kill threw Taren across the arena to where the enemy died");
        }
        [UnityTest] public IEnumerator TheHookDoesNotPullTarenOntoAMine()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,30));yield return Frames(2);
            var enemy=Enemy("Shellmine",new Vector3(38,0,30));yield return Frames(2);
            TetherLine.Cast(taren,taren.transform.position+Vector3.up*1.1f,Vector3.right,12,taren.Packet(1,DamageType.Pulse,1));
            yield return Drive(taren,.45f);
            Assert.Less(Vector3.Distance(taren.transform.position,new Vector3(30,0,30)),.5f,"the hook pulled Taren into a mine's blast");
            Object.Destroy(enemy.gameObject);
        }
        [UnityTest] public IEnumerator AReeledEnemyLosesItsPendingAttack()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,30));yield return Frames(2);
            var enemy=Enemy("Ridgehound",new Vector3(38,0,30),false);
            float deadline=Time.unscaledTime+6;enemy.transform.position=new Vector3(34,0,30);while(!enemy.Telegraphing&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(enemy.Telegraphing,"fixture: mid tell");
            enemy.Health.Expose(.9f);yield return Frames(2);
            Assert.IsFalse(enemy.Telegraphing,"a stunned enemy holds its tell and strikes the moment it recovers");
            enemy.Passive=true;Object.Destroy(enemy.gameObject);
        }
        [UnityTest] public IEnumerator WaypointBlinkKeepsAimOnTheEnemy()
        {
            var sela=Sela;PartyController.Current.Swap();yield return Frames(2);Place(sela,new Vector3(30,0,30));
            var enemy=Enemy("SentinelHusk",new Vector3(35,0,30));yield return Frames(2);sela.target=enemy.Health;
            GameServices.Current.State.party.Find(m=>m.id=="Sela").level=12;
            SkillBook.Equip(GameServices.Current.State.party.Find(m=>m.id=="Sela"),0,SkillBook.All("Sela").First(s=>s.kind==SkillKind.WaypointBlink));
            sela.charge=100;Assert.IsTrue(sela.Skill(0));yield return new WaitForSecondsRealtime(.35f);
            var toEnemy=enemy.transform.position-sela.transform.position;toEnemy.y=0;
            Assert.Less(Vector3.Angle(sela.motor.Facing,toEnemy),120,"after a blink the next shot fires away from the enemy");
            Object.Destroy(enemy.gameObject);
        }
        // ------------------------------------------------------------------ enemies
        [UnityTest] public IEnumerator ShellminesHurtAndAreMarked()
        {
            var taren=Taren;var mine=Enemy("Shellmine",new Vector3(30,0,30));Place(taren,new Vector3(31.5f,0,30));Place(Sela,new Vector3(60,0,60));yield return Frames(2);
            float before=taren.Health.integrity;mine.Passive=false;
            float deadline=Time.unscaledTime+3;while(!mine.Telegraphing&&Time.unscaledTime<deadline)yield return null;
            Assert.IsNotNull(mine.AttackMark,"a mine's blast has no marking of its true size");Assert.AreEqual(3,mine.AttackMark.Radius,.01f);
            deadline=Time.unscaledTime+3;while(mine.Health.Alive&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.Less(taren.Health.integrity,before,"a mine's own death cancels its blast: mines can't hurt anyone");
        }
        [UnityTest] public IEnumerator ALungeMarksItsLane()
        {
            var taren=Taren;var hound=Enemy("Ridgehound",new Vector3(30,0,30),false);Place(taren,new Vector3(33,0,30));yield return null;
            float deadline=Time.unscaledTime+4;while(!hound.Telegraphing&&Time.unscaledTime<deadline)yield return null;
            Assert.IsNotNull(hound.AttackMark,"a hound's lunge marks nothing of its reach");
            Assert.AreEqual(Telegraph.Kind.Line,hound.AttackMark.Shape);Assert.Greater(hound.AttackMark.Length,4f);
            hound.Passive=true;Object.Destroy(hound.gameObject);
        }
        [UnityTest] public IEnumerator AScrapmiteBlastIsMarkedAndDodgeable()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,30));Place(Sela,new Vector3(60,0,60));
            var mite=Enemy("Scrapmite",new Vector3(31,0,30));yield return Frames(2);float before=taren.Health.integrity;
            mite.Health.Receive(new DamagePacket{source=Sela.Health,amount=100000,type=DamageType.Pulse});
            yield return Frames(2);
            Assert.IsTrue(Telegraph.Live.Any(t=>t!=null&&!t.Released&&t.Shape==Telegraph.Kind.Circle),"a dying mite's blast has no marking");
            Assert.AreEqual(before,taren.Health.integrity,.01f,"the blast struck with no time to react");
            Place(taren,new Vector3(40,0,30)); // Walk clear of the marking.
            yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(before,taren.Health.integrity,.01f,"the blast reached outside its marking");
        }
        [UnityTest] public IEnumerator HostileShotsLookHostile()
        {
            var source=Enemy("Scrapmite",new Vector3(60,0,60));yield return Frames(2);
            Projectile.Fire(new Vector3(40,1,40),Vector3.right,new DamagePacket{source=source.Health,amount=1,type=DamageType.Beam},1);
            var hostile=Projectile.Live.Last();
            Projectile.Fire(new Vector3(40,1,42),Vector3.right,new DamagePacket{source=Taren.Health,amount=1,type=DamageType.Beam},1);
            var friendly=Projectile.Live.Last();
            Assert.Greater(hostile.transform.localScale.x,friendly.transform.localScale.x*1.5f,"your own shots and theirs are the same size");
            Assert.AreNotEqual(friendly.GetComponent<Renderer>().sharedMaterial,hostile.GetComponent<Renderer>().sharedMaterial,"your own shots and theirs are the same colour");
        }
        [UnityTest] public IEnumerator EnemiesDoNotHuntIntoASafePocket()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,30));Place(Sela,new Vector3(31,0,30));
            var hound=Enemy("Ridgehound",new Vector3(32,0,30),false);yield return null;
            ZoneController.Current.SafePocket(true);yield return Frames(2);
            float before=taren.Health.integrity;
            yield return new WaitForSecondsRealtime(3f);
            ZoneController.Current.SafePocket(false);
            Assert.AreEqual(before,taren.Health.integrity,.01f,"a hound bit a hero who can't fight back inside a safe pocket");
            hound.Passive=true;Object.Destroy(hound.gameObject);
        }
        // ------------------------------------------------------------------ bosses
        BossController Boss(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=false;return enemy.GetComponent<BossController>();}
        [UnityTest] public IEnumerator OneCastStrikesAHeroOnce()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,30));Place(Sela,new Vector3(31,0,30));
            var boss=Boss("Burrower",new Vector3(30,0,42));yield return Frames(3);boss.GetComponent<EnemyBrain>().Passive=true;yield return null;
            int hits=0;taren.Health.Damaged+=(_,__,___)=>hits++;
            Assert.IsTrue(boss.Perform("SpoilToss"));
            float deadline=Time.unscaledTime+3;while(boss.Busy&&Time.unscaledTime<deadline)yield return null;
            Assert.LessOrEqual(hits,1,"overlapping circles of one toss each landed in the same frame");
            Object.Destroy(boss.gameObject);
        }
        [UnityTest] public IEnumerator SeismicBandsAreOneBudget()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,36));Place(Sela,new Vector3(70,0,70));
            var boss=Boss("Burrower",new Vector3(30,0,30));yield return Frames(3);boss.GetComponent<EnemyBrain>().Passive=true;yield return null;
            int hits=0;taren.Health.Damaged+=(_,__,___)=>hits++;
            Assert.IsTrue(boss.Perform("SeismicRings"));
            float deadline=Time.unscaledTime+6;while(boss.Busy&&Time.unscaledTime<deadline)yield return null;
            Assert.LessOrEqual(hits,1,"bands .55 s apart with a .3 s stagger chain a hero from 60 to 0");
            Object.Destroy(boss.gameObject);
        }
        [UnityTest] public IEnumerator HessasSidearmIsMarked()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,30));var boss=Boss("HessaKorl",new Vector3(30,0,40));yield return Frames(3);boss.GetComponent<EnemyBrain>().Passive=true;yield return null;
            Assert.IsTrue(boss.Perform("Sidearm"));
            float deadline=Time.unscaledTime+.8f;bool marked=false;
            while(!marked&&Time.unscaledTime<deadline){marked=Marks(boss).Any(m=>m.Shape==Telegraph.Kind.Line&&!m.Released);yield return null;}
            Assert.IsTrue(marked,"Sidearm is the one Hessa special with no marking");
            Object.Destroy(boss.gameObject);
        }
        [UnityTest] public IEnumerator HookLanceStrikesOnlyInsideItsPaint()
        {
            var taren=Taren;Place(Sela,new Vector3(70,0,70));
            var boss=Boss("HessaKorl",new Vector3(30,0,30));boss.transform.rotation=Quaternion.LookRotation(Vector3.forward);
            Place(taren,new Vector3(30,0,35.5f));yield return Frames(3);boss.GetComponent<EnemyBrain>().Passive=true;yield return null;
            var mark=default(Telegraph);
            Assert.IsTrue(boss.Perform("HookLance"));yield return null;yield return null;
            mark=Marks(boss).First(m=>m.Shape==Telegraph.Kind.Cone);
            // Stand exactly at the painted edge's outside, as a player backing away from it does.
            var origin=mark.Origin;var edge=origin+mark.Forward*(mark.Radius+.6f);Place(taren,edge);
            float before=taren.Health.integrity,deadline=Time.unscaledTime+.75f;
            while(!mark.Released&&Time.unscaledTime<deadline)yield return null;yield return null;
            Assert.AreEqual(origin,mark.Origin,"the marking jumped forward at the moment of the strike");
            Assert.AreEqual(before,taren.Health.integrity,.01f,"struck having never stood in the marking");
            Object.Destroy(boss.gameObject);
        }
        [UnityTest] public IEnumerator TheBellowsOffersAnOpening()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,50));Place(Sela,new Vector3(70,0,70));
            var boss=Boss("BellowsBelow",new Vector3(30,0,30));yield return Frames(3);boss.GetComponent<EnemyBrain>().Passive=true;yield return null;
            Assert.IsTrue(boss.Perform("Breath"));
            float deadline=Time.unscaledTime+3;while(!boss.Health.Broken&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(boss.Health.Broken,"no move in the Bellows' set leaves a punish window");
            Object.Destroy(boss.gameObject);
        }
        [UnityTest] public IEnumerator PunishingAnOpeningDoesNotHastenTheNextMove()
        {
            var taren=Taren;Place(taren,new Vector3(30,0,30));Place(Sela,new Vector3(70,0,70));
            var boss=Boss("Burrower",new Vector3(30,0,40));yield return Frames(3);boss.GetComponent<EnemyBrain>().Passive=true;yield return null;
            Assert.IsTrue(boss.Perform("Burrow"));
            float deadline=Time.unscaledTime+6;while(!boss.Health.Broken&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(boss.Health.Broken,"fixture: the eruption's opening");
            taren.Health.InvulnerableUntil=GameTime.Now+9;
            boss.Health.Receive(new DamagePacket{source=taren.Health,amount=10,type=DamageType.Kinetic});yield return null;
            deadline=Time.unscaledTime+4;while(boss.Busy&&Time.unscaledTime<deadline)yield return null;
            Assert.Greater(boss.NextSpecialAt-boss.Health.BrokenUntil,1.5f,"hitting the opening brought the rig back 3 s sooner than leaving it alone");
            Object.Destroy(boss.gameObject);
        }
        [UnityTest] public IEnumerator TradingSlotsKeepsACooldown()
        {
            var taren=Taren;var member=GameServices.Current.State.party.Find(m=>m.id=="Taren");
            var cleave=SkillBook.Equipped("Taren",0);var other=SkillBook.Equipped("Taren",1);
            taren.charge=100;Assert.IsTrue(taren.Skill(0));float left=taren.cooldowns[0];Assert.Greater(left,0);
            SkillBook.Equip(member,1,cleave);yield return null;
            Assert.Greater(taren.cooldowns[1],left*.5f,"a trip to the menu reset the skill's cooldown");
            Assert.Less(taren.cooldowns[0],.01f,"the other skill inherited a cooldown it never earned");
            SkillBook.Equip(member,0,cleave);
        }
        [UnityTest] public IEnumerator APhaseShiftBringsItsSignatureMoveNext()
        {
            // Eight metres off, an ordinary pick can't be the Sweeping Cut: it is only offered to someone crowding her.
            var taren=Taren;Place(taren,new Vector3(30,0,30));Place(Sela,new Vector3(70,0,70));
            var boss=Boss("HessaKorl",new Vector3(30,0,38));yield return Frames(3);
            var moves=new List<string>();void Moved(BossController who,string move){if(who==boss)moves.Add(move);}BossController.MoveStarted+=Moved;
            boss.Health.Receive(new DamagePacket{source=taren.Health,amount=1,type=DamageType.Kinetic});
            boss.Health.integrity=boss.Health.maximum*.6f; // into phase 2
            // Hold the distance: she walks in during her rest, and up close an ordinary pick could land on it by luck.
            float deadline=Time.unscaledTime+6;
            while(moves.Count<2&&Time.unscaledTime<deadline){Place(taren,boss.transform.position+Vector3.back*9);yield return null;}
            BossController.MoveStarted-=Moved;
            Assert.AreEqual("PhaseShift",moves[0]);Assert.AreEqual("SweepingCut",moves.Count>1?moves[1]:"none","a phase shift promises change, and the next move was an ordinary pick");
            boss.GetComponent<EnemyBrain>().Passive=true;Object.Destroy(boss.gameObject);
        }
        [UnityTest] public IEnumerator EveryMoveOnOfferComesRoundWithinAFewPicks()
        {
            var taren=Taren;Place(Sela,new Vector3(70,0,70));
            var boss=Boss("Burrower",new Vector3(30,0,30));Place(taren,new Vector3(30,0,39));yield return Frames(1);
            boss.GetComponent<EnemyBrain>().Passive=true;
            const System.Reflection.BindingFlags Private=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var type=typeof(BossController);var pick=type.GetMethod("Pick",Private);var count=type.GetField("moveCount",Private);var last=type.GetField("lastMove",Private);
            var used=(Dictionary<string,int>)type.GetField("lastUsed",Private).GetValue(boss);
            var offer=new (string move,float weight)[]{("DrillCharge",2.2f),("SpoilToss",1.8f),("TailSweep",0f),("Burrow",1.5f),("SeismicRings",1.6f)};
            for(int fight=0;fight<300;fight++)
            {
                used.Clear();count.SetValue(boss,0);last.SetValue(boss,null);var seen=new HashSet<string>();
                for(int n=1;n<=6;n++)
                {
                    string move=(string)pick.Invoke(boss,new object[]{offer});
                    seen.Add(move);count.SetValue(boss,n);last.SetValue(boss,move);used[move]=n;
                }
                Assert.IsFalse(seen.Contains("TailSweep"),"a move with nothing to offer right now was played");
                Assert.AreEqual(4,seen.Count,"six picks went by and the player never saw part of the boss's set: "+string.Join(",",seen));
            }
            // A move the boss is steered away from up close stays rare: the rule must not make her throw a chain at someone she could stab.
            var close=new (string move,float weight)[]{("HookLance",2.2f),("GrappleChain",.4f)};int chains=0;
            used.Clear();count.SetValue(boss,0);last.SetValue(boss,null);
            for(int n=1;n<=400;n++)
            {
                string move=(string)pick.Invoke(boss,new object[]{close});
                if(move=="GrappleChain")chains++;
                count.SetValue(boss,n);last.SetValue(boss,move);used[move]=n;
            }
            Assert.Less(chains,160,"the lance's reach is where she fights, and the chain took over anyway");
        }
        [UnityTest] public IEnumerator ABossDoesNotBackAwayFromMelee()
        {
            var taren=Taren;Place(Sela,new Vector3(70,0,70));
            var boss=Boss("Burrower",new Vector3(30,0,30));Place(taren,new Vector3(30,0,33));yield return Frames(3);
            var brain=boss.GetComponent<EnemyBrain>();brain.DamageScale=0;
            boss.GetType().GetField("nextSpecial",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(boss,Time.time+9);
            var from=boss.transform.position;yield return new WaitForSecondsRealtime(1f);
            var moved=boss.transform.position-from;moved.y=0;var away=from-taren.transform.position;away.y=0;
            Assert.LessOrEqual(Vector3.Dot(moved,away.normalized),.3f,"a heavy brawler back-pedals through the rest that should be the time to close");
            brain.Passive=true;Object.Destroy(boss.gameObject);
        }
        // ------------------------------------------------------------------ companion
        [UnityTest] public IEnumerator TheCompanionWalksOutOfAMarking()
        {
            var party=PartyController.Current;var partner=party.members[1-party.index];var lead=party.Active;
            partner.GetComponent<PartnerBrain>().enabled=true;
            Place(lead,new Vector3(40,0,40));Place(partner,new Vector3(30,0,30));
            var boss=Boss("Burrower",new Vector3(30,0,22.4f));yield return Frames(3);boss.GetComponent<EnemyBrain>().Passive=true;
            lead.target=boss.Health;yield return null;
            var mark=Telegraph.Circle(new Vector3(30,0,30),3,2.5f,Telegraph.Threat);mark.owner=boss;
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.IsFalse(mark.Contains(partner.transform.position),"the companion stands in a marking while the player walks clear");
            mark.Cancel();Object.Destroy(boss.gameObject);
        }
    }
}
