using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Batch 11, what the final audit's skeptics still found in the fights: a blink that dashes at what it is escaping, a strike that lands off its paint, a Bellows that shows nothing as it
    /// shifts, and a shift a Flash could cut short. Each failure says what a player would see go wrong.</summary>
    public sealed class QaFightLastTests
    {
        System.Action<BossController,string> moved;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(moved!=null){BossController.MoveStarted-=moved;moved=null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static CombatActor Taren=>PartyController.Current.members.First(m=>m.character=="Taren");
        static CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        static CombatActor Other(CombatActor hero)=>PartyController.Current.members.First(m=>m!=hero);
        static void Quiet(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        static CombatActor Lead(string hero)
        {
            var party=PartyController.Current;if(party.Active.character!=hero)Assert.IsTrue(party.Swap(),"fixture: the swap");
            Quiet();return party.Active;
        }
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
        /// <summary>Open floor far from the arena's walls.</summary>
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Fight fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        /// <summary>Solid, and not marked static: a built player keeps no such flag.</summary>
        static GameObject Wall(Vector3 center,Vector3 size,float yaw=0)
        {
            var wall=new GameObject("Fight fixture wall",typeof(BoxCollider));wall.transform.SetPositionAndRotation(center,Quaternion.Euler(0,yaw,0));
            wall.GetComponent<BoxCollider>().size=size;Physics.SyncTransforms();return wall;
        }
        static EnemyBrain Enemy(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        /// <summary>A body that never dies or breaks, so what's asked of it is only where it goes and what it does.</summary>
        static EnemyBrain Dummy(string id,Vector3 at){var enemy=Enemy(id,at);var body=enemy.Health;body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;return enemy;}
        static void Sync(int level)
        {
            foreach(var member in GameServices.Current.State.party){member.level=level;member.skillUses?.Clear();}
            PartyController.Current.RefreshStats();
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static void Fresh(CombatActor actor){actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;}
        static DamageType Neutral(Health body)=>System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().First(t=>t!=body.weakness&&t!=body.resistance);
        /// <summary>The motor only carries out a dash while it is being driven, as the player's brain does every frame.</summary>
        static IEnumerator Drive(CombatActor actor,float seconds)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){actor.motor.Move(Vector2.zero,false,false);yield return null;}}
        static IEnumerator ToATell(EnemyBrain body,string what)
        {
            for(float end=Time.unscaledTime+3;!body.Telegraphing&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(body.Telegraphing,"fixture: "+what+" told an attack");
        }
        /// <summary>A body that has told, with the hero it told at, the Fold's owner off to one side and a settled start.</summary>
        static IEnumerator Told(string id,Vector3 at,Vector3 heroAt,System.Action<EnemyBrain,CombatActor> found)
        {
            yield return Zone("Arena_Ground");Arena();var hero=Lead("Taren");Place(Other(hero),new Vector3(260,0,0));Place(hero,heroAt);Fresh(hero);
            var body=Dummy(id,at);yield return new WaitForSecondsRealtime(.7f);
            body.Passive=false;yield return ToATell(body,id);
            found(body,hero);
        }
        /// <summary>A real Fold, thrown from just short of where it opens: it lands in a third of a second, inside a tell.</summary>
        static void Fold(Vector3 at)=>GravityWell.Throw(Sela,at+Vector3.up,at,false,1);

        // ------------------------------------------------------------------ item 3: a blink goes away
        const float BlinkGap=3;
        Vector3 blinked;float blinkLanded;
        static void EquipBlink(CombatActor sela)
        {
            var member=GameServices.Current.State.party.Find(m=>m.id==sela.character);
            Assert.IsTrue(SkillBook.Equip(member,0,SkillBook.All(sela.character).First(s=>s.kind==SkillKind.WaypointBlink)),"fixture: Waypoint Blink on its slot");
        }
        /// <summary>One blink from a stand with the stick held as given, and with a target a body 3 m ahead: how far it carried her, and what the charge she left behind did to the body.</summary>
        IEnumerator BlinkOnce(CombatActor sela,Vector3 at,Vector3 stick,bool targeted,bool flight)
        {
            Place(sela,at);EnemyBrain body=null;float landed=0;
            if(targeted)
            {
                body=Dummy(flight?"ChoristerDart":"Ridgehound",at+Vector3.forward*BlinkGap);yield return Frames(2);sela.target=body.Health;
                body.Health.Damaged+=(_,packet,amount)=>{if(packet.source==sela.Health)landed+=amount;};
            }
            // What PlayerBrain writes from the stick every frame.
            sela.MoveIntent=stick;sela.charge=100;sela.cooldowns[0]=0;
            Assert.IsTrue(sela.Skill(0),"fixture: the blink");
            yield return Drive(sela,.35f);sela.MoveIntent=Vector3.zero;
            blinked=sela.transform.position-at;blinked.y=0;
            yield return Drive(sela,.6f);// The charge's fuse is .6 s from the cast.
            blinkLanded=landed;
            if(body!=null){Object.Destroy(body.gameObject);yield return Frames(2);}
            sela.target=null;
        }
        /// <summary>Thrust and stride are the stick, so flying or running at something always holds that way, on a digital pad most of all. The row says "Blink away" on the ground and in flight.</summary>
        [UnityTest] public IEnumerator ABlinkWithTheStickAtItsTargetGoesAwayFromIt([Values(true,false)] bool flight)
        {
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Arena();
            float y=flight?1:0;var at=new Vector3(200,y,0);var sela=Sela;Place(Taren,new Vector3(260,y,0));Sync(12);EquipBlink(sela);var failures=new List<string>();
            foreach(float off in new[]{0f,45f,-55f})
            {
                yield return BlinkOnce(sela,at,Quaternion.Euler(0,off,0)*Vector3.forward,true,flight);
                if(Vector3.Angle(blinked,Vector3.back)>15)failures.Add($"with the stick {off:0} degrees off the enemy, the blink went {Vector3.Angle(blinked,Vector3.back):0} degrees off straight away from it: the row says 'Blink away' and she dashes at the threat");
                if(blinked.magnitude<(flight?6:4))failures.Add($"with the stick {off:0} degrees off the enemy, the blink carried her only {blinked.magnitude:0.0} m");
                if(blinkLanded<=0)failures.Add($"with the stick {off:0} degrees off the enemy, the charge she left behind reached nothing 3 m away");
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>The stick's say stays for every other way, and for a blink with nothing targeted.</summary>
        [UnityTest] public IEnumerator ABlinkWithTheStickAnyOtherWayFollowsTheStick([Values(true,false)] bool flight)
        {
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Arena();
            float y=flight?1:0;var at=new Vector3(200,y,0);var sela=Sela;Place(Taren,new Vector3(260,y,0));Sync(12);EquipBlink(sela);var failures=new List<string>();
            foreach(float off in new[]{75f,-100f,180f})
            {
                var stick=Quaternion.Euler(0,off,0)*Vector3.forward;
                yield return BlinkOnce(sela,at,stick,true,flight);
                if(Vector3.Angle(blinked,stick)>15)failures.Add($"with the stick {off:0} degrees off the enemy the blink went {Vector3.Angle(blinked,stick):0} degrees off the stick: the player steered it clear and it ignored him");
                if(blinked.magnitude<(flight?6:4))failures.Add($"with the stick {off:0} degrees off the enemy, the blink carried her only {blinked.magnitude:0.0} m");
            }
            yield return BlinkOnce(sela,at,Vector3.forward,false,flight);
            if(Vector3.Angle(blinked,Vector3.forward)>15||blinked.magnitude<(flight?6:4))failures.Add($"with nothing targeted the stick is all there is, and the blink went {blinked} on a stick straight ahead");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ------------------------------------------------------------------ item 4: a strike lands on its paint
        /// <summary>The skeptic's path: a bite told, the hero steps clear of its paint on the far side, a Fold opens beyond him and drags the body after him. It was bitten up to 2.6 m off the circle.</summary>
        [UnityTest] public IEnumerator ABiteDraggedOffItsPaintLosesItsTellAndBitesNobody()
        {
            EnemyBrain body=null;CombatActor hero=null;var at=new Vector3(200,0,0);
            yield return Told("SentinelHusk",at,new Vector3(200,0,1.8f),(b,h)=>{body=b;hero=h;});
            float told=Time.unscaledTime;var from=body.transform.position;var mark=body.AttackMark;
            Assert.AreEqual(Telegraph.Kind.Circle,mark.Shape,"fixture: a bite paints a circle");
            Place(hero,mark.Origin+mark.Forward*(mark.Radius+.7f));float before=hero.Health.integrity;
            Fold(at+mark.Forward*5.5f);
            bool heldOn=false;
            for(float end=told+.9f;Time.unscaledTime<end;){heldOn|=body.Telegraphing&&Flat(body.transform.position,from)>.62f;yield return null;}
            float dragged=Flat(body.transform.position,from);body.Passive=true;
            Assert.Greater(dragged,.62f,"fixture: the Fold dragged the body inside what was left of its tell");
            Assert.IsFalse(heldOn,"a body dragged off what it marked still holds its tell: its strike will land off its paint");
            Assert.AreEqual(before,hero.Health.integrity,.01f,"standing .7 m clear of the painted circle doesn't make him safe: the Fold dragged the bite onto him");
        }
        /// <summary>Under the limit it keeps its tell, and the bite still falls where the paint says, wherever the nudge left the body.</summary>
        [UnityTest] public IEnumerator ABiteNudgedUnderTheLimitStillLandsOnItsPaint()
        {
            EnemyBrain body=null;CombatActor hero=null;
            yield return Told("SentinelHusk",new Vector3(200,0,0),new Vector3(200,0,1.8f),(b,h)=>{body=b;hero=h;});
            var mark=body.AttackMark;Place(hero,mark.Origin+mark.Forward*(mark.Radius+.7f));float before=hero.Health.integrity;
            body.Displace(mark.Forward*.45f);yield return Frames(2);
            Assert.IsTrue(body.Telegraphing,"a nudge of .45 m cost the body its tell: only a real drag should");
            for(float end=Time.unscaledTime+1.5f;body.Telegraphing&&Time.unscaledTime<end;)yield return null;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(before,hero.Health.integrity,.01f,"a bite nudged .45 m landed off its paint, on a hero standing .7 m clear of the circle");
            // And it still reaches what the paint covers, off to the side of the nudge.
            for(float end=Time.unscaledTime+4;!body.Telegraphing&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(body.Telegraphing,"fixture: it bites again");
            mark=body.AttackMark;var side=Vector3.Cross(Vector3.up,mark.Forward);
            Place(hero,mark.Origin+side*(mark.Radius-.2f));before=hero.Health.integrity;
            body.Displace(-side*.45f);
            for(float end=Time.unscaledTime+1.5f;hero.Health.integrity>=before&&Time.unscaledTime<end;)yield return null;
            body.Passive=true;
            Assert.Less(hero.Health.integrity,before,"a bite misses a hero standing inside its painted circle");
        }
        /// <summary>The same for a lane: from where it landed, a hound lunged along the old aim with its strike outside what it painted, onto a hero who had stepped clear of it.</summary>
        [UnityTest] public IEnumerator AHoundDraggedOffItsLaneDoesNotLungeBesideIt()
        {
            EnemyBrain hound=null;CombatActor hero=null;var at=new Vector3(200,0,0);
            yield return Told("Ridgehound",at,new Vector3(200,0,3),(b,h)=>{hound=b;hero=h;});
            float told=Time.unscaledTime;var lane=hound.AttackMark;
            Assert.AreEqual(Telegraph.Kind.Line,lane.Shape,"fixture: a hound paints a lane");
            var side=Vector3.Cross(Vector3.up,lane.Forward);
            Place(hero,lane.Origin+lane.Forward*3+side*1.6f);float before=hero.Health.integrity;
            Assert.IsFalse(lane.Contains(hero.transform.position),"fixture: he has stepped clear of the lane");
            Fold(at+side*5.5f);
            for(float end=told+1;Time.unscaledTime<end;)yield return null;
            hound.Passive=true;
            Assert.AreEqual(before,hero.Health.integrity,.01f,"standing clear of the painted lane doesn't make him safe: the Fold dragged the hound, and it lunged parallel to its lane");
        }
        /// <summary>Held by a wall it hasn't moved: the strike is still where it was marked.</summary>
        [UnityTest] public IEnumerator ABodyAWallHoldsAgainstAFoldKeepsItsTell()
        {
            var at=new Vector3(200,0,0);
            yield return Zone("Arena_Ground");Arena();var hero=Lead("Taren");Place(Other(hero),new Vector3(260,0,0));Place(hero,new Vector3(200,0,1.8f));Fresh(hero);
            Wall(at+new Vector3(.75f,2.5f,0),new Vector3(.3f,7,6));
            var body=Dummy("SentinelHusk",at);yield return new WaitForSecondsRealtime(.7f);body.Passive=false;yield return ToATell(body,"the Husk");
            for(float end=Time.unscaledTime+.25f;Time.unscaledTime<end;){body.Displace(Vector3.right*.2f);yield return null;}
            Assert.Less(Flat(body.transform.position,at),.5f,"fixture: the wall holds it");
            Assert.IsTrue(body.Telegraphing,"a body that hasn't moved lost its tell for being pulled at");
            body.Passive=true;
        }
        /// <summary>One started on the way would be lost in an eighth of a second: a flicker of paint on the floor, a wind-up sound and a lesson about a dodge there's no need for.</summary>
        [UnityTest] public IEnumerator ABodyBeingDraggedStartsNoTellItWouldOnlyLose()
        {
            yield return Zone("Arena_Ground");Arena();var hero=Lead("Taren");Place(Other(hero),new Vector3(260,0,0));Place(hero,new Vector3(202.5f,0,2.2f));Fresh(hero);
            var at=new Vector3(200,0,0);var hound=Dummy("Ridgehound",at);yield return new WaitForSecondsRealtime(.7f);
            var centre=at+Vector3.right*5.2f;Fold(centre);
            yield return new WaitForSecondsRealtime(.42f);// Open now, and dragging.
            hound.Passive=false;bool told=false;
            for(float end=Time.unscaledTime+2;Flat(hound.transform.position,centre)>1.2f&&Time.unscaledTime<end;){told|=hound.Telegraphing;yield return null;}
            Assert.IsFalse(told,"a hound on its way to the well starts a tell it can only lose");
            Assert.Less(Flat(hound.transform.position,centre),1.3f,"fixture: the Fold gathered it");
            // Gathered, it isn't dragged any more, and it's a hound again.
            for(float end=Time.unscaledTime+2;!hound.Telegraphing&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(hound.Telegraphing,"a body at the well's centre never gets to attack again");
            hound.Passive=true;
        }
        /// <summary>The well is what stops a body being pulled anywhere: one at its centre, walking at a hero and taken back a step at a time, is standing there, and what it can reach it can still bite.</summary>
        [UnityTest] public IEnumerator ABodyGatheredAtTheWellStillAttacksWhatItCanReach()
        {
            yield return Zone("Arena_Ground");Arena();var hero=Lead("Taren");Place(Other(hero),new Vector3(260,0,0));var centre=new Vector3(200,0,0);Place(hero,centre+Vector3.forward*4);Fresh(hero);
            var body=Dummy("SentinelHusk",centre+Vector3.forward*.8f);yield return new WaitForSecondsRealtime(.7f);
            Fold(centre);yield return new WaitForSecondsRealtime(.5f);// Open, and it has gathered.
            body.Passive=false;yield return new WaitForSecondsRealtime(.4f);
            Assert.IsFalse(body.Telegraphing,"fixture: he's out of its reach, and it walks at him against the pull");
            Place(hero,centre+Vector3.forward*3);bool told=false;
            for(float end=Time.unscaledTime+.5f;!told&&Time.unscaledTime<end;){told=body.Telegraphing;yield return null;}
            body.Passive=true;
            Assert.IsTrue(told,"a body held at the well's centre with a hero in its reach never tells: a Fold has stopped it attacking, not only moved it");
        }
        /// <summary>A Fold that lands in the lunge drags it the same way, its strike with it.</summary>
        [UnityTest] public IEnumerator ADragThatLandsMidLungeEndsIt()
        {
            EnemyBrain hound=null;CombatActor hero=null;
            yield return Told("Ridgehound",new Vector3(200,0,0),new Vector3(200,0,3),(b,h)=>{hound=b;hero=h;});
            var from=hound.transform.position;var lane=hound.AttackMark;var side=Vector3.Cross(Vector3.up,lane.Forward);
            Place(hero,from+side*5);
            for(float end=Time.unscaledTime+2;hound.Telegraphing&&Time.unscaledTime<end;)yield return null;
            Assert.IsFalse(hound.Telegraphing,"fixture: the tell ends");
            hound.Displace(side*.6f);hound.Passive=true;yield return new WaitForSecondsRealtime(.5f);
            Assert.Less(Vector3.Dot(hound.transform.position-from,lane.Forward),3,"a hound dragged .6 m off its lane in mid-lunge runs on beside it with its strike");
        }
        /// <summary>A controller slides along a wall it meets at an angle. The wall comes in the tell, so the hound has its line and its lane.</summary>
        [UnityTest] public IEnumerator ALungeThatClipsAnAngledWallStopsThere()
        {
            EnemyBrain hound=null;CombatActor hero=null;
            yield return Told("Ridgehound",new Vector3(200,0,0),new Vector3(200,0,4.4f),(b,h)=>{hound=b;hero=h;});
            var from=hound.transform.position;var lane=hound.AttackMark;var across=Vector3.Cross(Vector3.up,lane.Forward);
            Wall(from+lane.Forward*2.6f+Vector3.up*2.5f,new Vector3(14,7,.3f),45);
            for(float end=Time.unscaledTime+2;hound.Telegraphing&&Time.unscaledTime<end;)yield return null;
            hound.Passive=true;yield return new WaitForSecondsRealtime(.5f);
            var off=hound.transform.position-from;float slid=Mathf.Abs(Vector3.Dot(off,across)),along=Vector3.Dot(off,lane.Forward);
            TestContext.WriteLine($"LUNGE_WALL along={along:0.00} slid={slid:0.00}");
            Assert.Greater(along,1,"fixture: it lunged");
            Assert.Less(slid,.9f,$"a lunge that clipped a wall slid {slid:0.00} m along it, off the lane it painted, with its strike going too");
        }
        /// <summary>The guard: with nothing in the way the lane is painted to the end of its travel, and it gets there.</summary>
        [UnityTest] public IEnumerator ALungeOnOpenFloorStillRunsItsWholeLane()
        {
            EnemyBrain hound=null;CombatActor hero=null;
            yield return Told("Ridgehound",new Vector3(200,0,0),new Vector3(200,0,3),(b,h)=>{hound=b;hero=h;});
            var from=hound.transform.position;var lane=hound.AttackMark;var across=Vector3.Cross(Vector3.up,lane.Forward);
            Place(hero,from+across*5);
            for(float end=Time.unscaledTime+2;hound.Telegraphing&&Time.unscaledTime<end;)yield return null;
            hound.Passive=true;yield return new WaitForSecondsRealtime(.5f);
            var off=hound.transform.position-from;
            Assert.Greater(Vector3.Dot(off,lane.Forward),4.2f,"a lunge across open floor stopped short of its painted lane");
            Assert.Less(Mathf.Abs(Vector3.Dot(off,across)),.1f,"a lunge across open floor left its lane");
        }

        // ------------------------------------------------------------------ item 5: a shift the Bellows can show and a Flash can't cut short
        /// <summary>The plan's bar is a readable brace. Pose returns at once for a body with no rig, so through the whole shift the dome only went on breathing.</summary>
        [UnityTest] public IEnumerator TheBellowsShowsItsBraceThroughAShift()
        {
            yield return Zone("Arena_Ground");Arena();var hero=Lead("Taren");Place(Other(hero),new Vector3(260,.1f,0));Place(hero,new Vector3(200,.1f,-12));
            hero.Health.InvulnerableUntil=GameTime.Now+30;
            var enemy=Enemy("BellowsBelow",new Vector3(200,.1f,0));var dome=enemy.GetComponent<DefeatPresentation>().visual;var rest=dome.localScale;yield return Frames(3);
            var boss=enemy.GetComponent<BossController>();
            Assert.IsTrue(boss.Perform("PhaseShift"),"fixture: the shift");
            float lowest=1,widest=1;
            for(float end=Time.unscaledTime+4;boss.Shifting&&Time.unscaledTime<end;)
            {lowest=Mathf.Min(lowest,dome.localScale.y/rest.y);widest=Mathf.Max(widest,dome.localScale.x/rest.x);yield return null;}
            Assert.IsFalse(boss.Shifting,"fixture: the shift ends");
            TestContext.WriteLine($"BELLOWS_BRACE lowest={lowest:0.00} widest={widest:0.00}");
            Assert.Less(lowest,.85f,"the dome doesn't draw down through a phase shift: it goes on breathing as if nothing were happening");
            Assert.Greater(widest,1.08f,"the dome doesn't spread as it draws down: its brace doesn't read as bearing down on the floor");
            yield return Frames(2);
            Assert.AreEqual(1,dome.localScale.y/rest.y,.05f,"the dome stays squashed after its shift");
            Assert.AreEqual(1,dome.localScale.x/rest.x,.05f,"the dome stays spread after its shift");
        }
        /// <summary>The Flash slows the shift and not the 1.4 s of real time its brace was timed on. From there a hit landed with the bar still saying BRACE, and a break stopped the routine with
        /// the signature move and the summons still to come.</summary>
        [UnityTest] public IEnumerator AFlashThroughAShiftCostsItNeitherItsMoveNorItsSummons([Values("Burrower","HessaKorl")] string id)
        {
            string adds=id=="Burrower"?"Scrapmite":"MusterScav",promised=id=="Burrower"?"Burrow":"SweepingCut";var at=new Vector3(200,.1f,0);
            yield return Zone("Arena_Ground");Arena();var lead=Lead("Taren");var partner=Other(lead);Place(partner,new Vector3(260,.1f,0));Place(lead,at+Vector3.back*9);
            lead.Health.InvulnerableUntil=partner.Health.InvulnerableUntil=GameTime.Now+60;
            var enemy=Enemy(id,at);yield return Frames(3);var boss=enemy.GetComponent<BossController>();var body=enemy.Health;
            body.Receive(new DamagePacket{source=lead.Health,amount=1,type=Neutral(body)});
            Assert.IsTrue(boss.Engaged,"fixture: the fight is on");
            var started=new List<string>();moved=(who,move)=>{if(who==boss)started.Add(move);};BossController.MoveStarted+=moved;
            enemy.Passive=false;GameTime.BeginFlash();// Earned a moment ago, and running through the whole of what follows.
            body.integrity=body.maximum*(boss.Thresholds[0]-.02f);
            for(float end=Time.unscaledTime+4;!boss.Shifting&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(boss.Shifting,"fixture: crossing the threshold shifts it");
            var failures=new List<string>();
            for(float end=Time.unscaledTime+1.45f;Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(boss.Shifting,"fixture: a Flash has it still in its shift, past the old limit in real time");
            float breakAt=body.breakThreshold;body.breakThreshold=1;
            float dealt=body.Receive(new DamagePacket{source=lead.Health,amount=500,type=Neutral(body),breakPower=1000});
            body.breakThreshold=breakAt;
            if(dealt>0)failures.Add($"with the bar saying BRACE, through a Flash, a hit lands for {dealt:0}");
            if(body.Broken)failures.Add("a hit breaks the boss in the middle of its shift");
            GameTime.EndFlash();
            for(float end=Time.unscaledTime+3;boss.Shifting&&Time.unscaledTime<end;)yield return null;
            Assert.IsFalse(boss.Shifting,"its shift never ends");
            // Not a moment after it.
            if(failures.Count==0&&body.Receive(new DamagePacket{source=lead.Health,amount=1,type=Neutral(body)})<=0)failures.Add("the boss stays untouchable after its bar has stopped saying so");
            int called=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Count(e=>e!=enemy&&e.definition.id==adds&&e.Health.Alive);
            if(called!=2)failures.Add($"the shift called {called} of its 2 {adds}: a break cut it short before they came");
            for(float end=Time.unscaledTime+6;started.Count<2&&Time.unscaledTime<end;)yield return null;
            enemy.Passive=true;
            if(started.Count==0||started[0]!="PhaseShift")failures.Add("fixture: the shift is logged as a move");
            if(started.Count<2||started[1]!=promised)failures.Add($"after {id}'s shift the next move is {(started.Count>1?started[1]:"none")}, not the {promised} it promised");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
