using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>S6a: Idra Sorn's own way of fighting, before her skills. Her definition (the sturdy hero, on the same Sync curve, with a style that is data and leaves the
    /// first two heroes exactly as they were), the bodies she borrows, the hammer's three slow heavy hits and what the third does to a light body, Brace (85% for less
    /// Thrust, and a Flash Guard's shockwave), the short hop with its short untouchable moment, and in flight the prow guard, the cutter, and X held for the prow.
    /// She fights here in a party of two, in Taren's place: choosing which two of three are out is another stream's.</summary>
    public sealed class IdraKitTests
    {
        public const int Level=17;
        Gamepad pad;
        InputSettings.BackgroundBehavior priorBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode priorEditor;
#endif
        [UnitySetUp] public IEnumerator Boot()
        {
            priorBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            // Headless has no focused Game view. Route virtual devices into play.
            priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);pad=null;
            InputSystem.settings.backgroundBehavior=priorBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
#endif
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static CombatActor Idra=>PartyController.Current.members.First(m=>m.character=="Idra");
        static CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        static void Quiet(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        /// <summary>The arena with Idra in Taren's place beside Sela, both at the Sync she joins at, on open floor far from the arena's walls.</summary>
        static IEnumerator Zone(bool flight)
        {
            Assert.IsNotNull(GameCatalog.Find<CharacterDef>("Idra"),"Idra has no definition: run Lattice.EditorTools.IdraBuilder.Build");
            var state=GameServices.Current.State;state.party.RemoveAll(m=>m.id=="Taren"||m.id=="Idra");
            state.party.Insert(0,new MemberState{id="Idra",level=Level,charge=100});state.activeMember=0;
            SceneFlow.Current.LoadZone(flight?"Arena_Flight":"Arena_Ground");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"the arena never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Quiet();
            yield return new WaitForSecondsRealtime(.7f);
            if(!flight){var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Idra fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
            foreach(var member in state.party){member.level=Level;member.skillUses?.Clear();}
            PartyController.Current.RefreshStats();
            Assert.AreEqual(2,PartyController.Current.members.Length,"fixture: a party of two, Idra and Sela");
            foreach(var hero in PartyController.Current.members){hero.Health.integrity=hero.Health.maximum;hero.thrust=100;}
        }
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}
            else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            Physics.SyncTransforms();
        }
        static EnemyBrain Enemy(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        /// <summary>A body that never dies or breaks, and that no damage type she uses here is strong or weak against.</summary>
        static EnemyBrain Dummy(bool flight,Vector3 at)
        {
            var enemy=Enemy(flight?"ChoristerDart":"Ridgehound",at);var body=enemy.Health;
            body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;body.weakness=body.resistance=DamageType.Pulse;return enemy;
        }
        static CombatActor Lead(string hero)
        {
            var party=PartyController.Current;if(party.Active.character!=hero)Assert.IsTrue(party.Swap(),"fixture: the swap");
            Quiet();return party.Active;
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static IEnumerator Drive(CombatActor actor,float seconds,Vector2 stick=default)
        {for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;){actor.motor.Move(stick,false,false);yield return null;}}
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        sealed class Hits{public int count;public float plain;public readonly List<float> each=new();public readonly List<HitWeight> weights=new();public readonly List<float> at=new();}
        /// <summary>The hero's hits on a body, with a crit's bonus taken back out so a number can be held to.</summary>
        static Hits Watch(EnemyBrain body,CombatActor hero)
        {
            var hits=new Hits();
            body.Health.Damaged+=(_,packet,amount)=>
            {
                if(packet.source!=hero.Health||amount<=0)return;
                float plain=packet.isCrit?amount/(1.5f*(1+Mathf.Max(0,hero.fortune)*.01f+hero.mods.critDamage)):amount;
                hits.count++;hits.plain+=plain;hits.each.Add(plain);hits.weights.Add(packet.weight);hits.at.Add(Time.unscaledTime);
            };
            return hits;
        }
        /// <summary>What a hit from this body comes to on her after her frame, before any guard.</summary>
        static float Plated(CombatActor hero,float amount)=>amount*(100/(100+Mathf.Max(0,hero.Plating-10)));
        static float Take(CombatActor hero,Health from,float amount)
        {float before=hero.Health.integrity;hero.Health.Receive(new DamagePacket{source=from,amount=amount,type=DamageType.Kinetic});return before-hero.Health.integrity;}
        /// <summary>Swings the chain's next hit and drives her until it has landed or its time is up.</summary>
        static IEnumerator Swing(CombatActor hero,Hits hits,int expected,System.Action eachFrame=null)
        {
            for(float end=Time.unscaledTime+1.5f;!hero.Attack()&&Time.unscaledTime<end;){hero.motor.Move(Vector2.zero,false,false);yield return null;}
            for(float end=Time.unscaledTime+1.2f;hits.count<expected&&Time.unscaledTime<end;){eachFrame?.Invoke();hero.motor.Move(Vector2.zero,false,false);yield return null;}
        }

        // ------------------------------------------------------------------ who she is
        [UnityTest] public IEnumerator IdraIsTheSturdyHeroOnTheSameCurveAndTheFirstTwoAreAsTheyWere()
        {
            yield return null;
            var idra=GameCatalog.Find<CharacterDef>("Idra");var taren=GameCatalog.Find<CharacterDef>("Taren");var sela=GameCatalog.Find<CharacterDef>("Sela");
            Assert.IsNotNull(idra,"Idra has no definition: run Lattice.EditorTools.IdraBuilder.Build");
            Assert.AreEqual("Idra Sorn",DisplayNames.Speaker("Idra"),"she isn't named in full");Assert.IsTrue(idra.female);Assert.AreEqual("Idra",idra.speakerId);
            Assert.AreEqual(taren.baseStats.output,idra.baseStats.output,.001f,"her Output starts off the others' curve");Assert.AreEqual(taren.growthPerLevel.output,idra.growthPerLevel.output,.001f,"her Output grows off the others' curve");
            Assert.Greater(idra.baseStats.plating,taren.baseStats.plating,"she starts no sturdier than Taren");Assert.Greater(idra.growthPerLevel.plating,taren.growthPerLevel.plating,"her Plating grows no faster than Taren's");
            Assert.Less(idra.growthPerLevel.response,taren.growthPerLevel.response,"her Response grows as fast as Taren's: she's meant to be the slow one");
            Assert.Greater(Lattice.Rpg.Levels.Calculate(idra,Level).plating,Lattice.Rpg.Levels.Calculate(taren,Level).plating*1.3f,"at the Sync she joins at her frame is hardly thicker than his");
            Assert.AreNotEqual(taren.syncHue,idra.syncHue,"she wears Taren's amber");Assert.Greater(idra.syncHue.r,idra.syncHue.g*2,"her hue isn't an ember's");
            Assert.IsNotNull(idra.natural,"she has no body out of combat");Assert.IsNotNull(idra.shaped,"she has no body to fight in");Assert.IsNotNull(idra.flight,"she has no ship");
            // The style: hers says what differs, and a definition that says nothing is the first two heroes' numbers to the digit.
            var style=idra.style;
            Assert.AreEqual(AttackStyle.Hammer,style.attack);
            Assert.AreEqual(.15f,style.guardTakes,.001f,"Brace doesn't stop 85%");Assert.Less(style.guardCost,CombatActor.GuardCost,"Brace pays as much Thrust as a guard");
            Assert.Less(style.guardCost*(1-style.guardTakes),CombatActor.GuardCost*(1-CombatActor.GuardTakes),"for the same hit, Brace pays more Thrust than a guard does");
            Assert.IsTrue(style.flightGuard,"her ship has no guard");Assert.Greater(style.counter,0,"her Flash Guard answers with nothing");
            Assert.Less(style.hopSafe,style.hopSeconds,"her hop is untouchable throughout, as a roll is");Assert.Less(style.rollSafe,style.rollSeconds,"her ship's roll is untouchable throughout");
            Assert.IsTrue(style.partnerGuards,"as a companion she'd roll, not guard");
            foreach(var first in new[]{taren,sela})
            {
                var plain=first.style;string who=first.id;
                Assert.AreEqual(AttackStyle.Default,plain.attack,who);Assert.AreEqual(CombatActor.GuardTakes,plain.guardTakes,.0001f,who+"'s guard changed");Assert.AreEqual(CombatActor.GuardCost,plain.guardCost,.0001f,who+"'s guard changed its price");
                Assert.IsFalse(plain.flightGuard,who+" can guard in flight");Assert.AreEqual(0,plain.counter,who+"'s Flash Guard answers with a shockwave");
                Assert.AreEqual(3.3f,plain.hop,.0001f,who);Assert.AreEqual(.25f,plain.hopSeconds,.0001f,who);Assert.GreaterOrEqual(plain.hopSafe,plain.hopSeconds,who+"'s roll can be hit");
                Assert.AreEqual(4,plain.roll,.0001f,who);Assert.AreEqual(.3f,plain.rollSeconds,.0001f,who);Assert.GreaterOrEqual(plain.rollSafe,plain.rollSeconds,who+"'s ship can be hit mid-roll");
                Assert.AreEqual(.9f,plain.chainWindow,.0001f,who);Assert.IsFalse(plain.partnerGuards,who);
            }
            // Her skills: four from the start, seven more with Sync, and an EXTRA; each with a picture of its own. This pinned four more, at 3, 5, 8 and 12, until her
            // late three (Take the Weight, Cutting Torch, Bulkhead) put her on the whole ladder the first two climb, to Sync 24 (November 12; IdraLateTests).
            CollectionAssert.AreEqual(new[]{SkillKind.Breach,SkillKind.TowLine,SkillKind.Shelter,SkillKind.SlagHammer},idra.skills.Select(s=>s.kind).ToArray(),"the four she joins with");
            CollectionAssert.AreEqual(new[]{SkillKind.RescueFlare,SkillKind.Counterweight,SkillKind.Avalanche,SkillKind.HoldTheLine,SkillKind.TakeTheWeight,SkillKind.CuttingTorch,SkillKind.Bulkhead},
                idra.learnable.Select(s=>s.kind).ToArray(),"the seven she learns");
            CollectionAssert.AreEqual(new[]{1,1,1,1},idra.skills.Select(s=>s.unlockLevel).ToArray());CollectionAssert.AreEqual(new[]{3,5,8,12,16,20,24},idra.learnable.Select(s=>s.unlockLevel).ToArray(),"she learns them at the Sync the others learn theirs");
            Assert.IsNotNull(idra.extra,"she has no EXTRA move");Assert.AreEqual(SkillKind.LastOneOut,idra.extra.kind);
            var cells=new HashSet<string>();
            foreach(var skill in idra.skills.Concat(idra.learnable).Append(idra.extra))
            {
                Assert.IsTrue(skill.ground&&skill.flight,skill.displayName+" isn't for both forms");Assert.IsFalse(string.IsNullOrWhiteSpace(skill.description),skill.displayName+" says nothing");
                Assert.IsNotNull(UiSkin.Skill(skill),skill.displayName+" has no icon: run tools/meshy/s6_idra_icons.py --compose 1 and the builder");
                Assert.IsTrue(cells.Add(skill.iconSheet+"#"+skill.iconCell),skill.displayName+" shares its icon with another of hers");
                Assert.IsFalse(SkillBook.All("Taren").Concat(SkillBook.All("Sela")).Any(s=>s.iconSheet==skill.iconSheet),skill.displayName+" is drawn from one of the first two heroes' sheets");
            }
            Assert.AreEqual(12,SkillBook.All("Idra").Length+1,"eleven skills and an EXTRA");
            Assert.IsNotNull(BarkService.Line("Idra",ExtraMove.Shout),"she has no line to shout for Last One Out: the barks weren't copied (IdraBuilder.Build)");
        }
        [UnityTest] public IEnumerator SheStandsInBorrowedBodiesWashedInHerOwnColourAndCarriesTheGuardKit()
        {
            yield return Zone(false);var idra=Idra;var def=GameCatalog.Find<CharacterDef>("Idra");var lender=GameCatalog.Find<CharacterDef>(def.style.bodiesFrom??"");
            Assert.AreEqual(AttackStyle.Hammer,idra.Style.attack,"the hero built from her definition doesn't fight by her style");
            Assert.AreEqual(AttackStyle.Default,Sela.Style.attack,"Sela's style isn't the plain one");
            var kit=idra.GetComponent<BraceKit>();Assert.IsNotNull(kit,"she's built without her guard's kit");
            bool borrowed=lender!=null&&(def.natural==lender.natural||def.shaped==lender.shaped||def.flight==lender.flight);
            Assert.AreEqual(borrowed,kit.Washed,borrowed?"she wears another hero's body unwashed: two of one model in a party":"a body of her own is washed as if it were borrowed");
            Assert.IsFalse(Sela.GetComponent<BraceKit>().Washed,"Sela's own body is washed");
            Assert.AreEqual(Lattice.Rpg.Levels.Calculate(def,Level).plating,idra.plating,.01f,"her Plating in a fight isn't her definition's");
            Assert.Greater(idra.plating,Sela.plating,"she's no sturdier than Sela in the same gear");
        }

        // ------------------------------------------------------------------ the hammer
        [UnityTest] public IEnumerator TheHammerIsThreeSlowHitsEachHeavierThanTarensInDamageAndBreak()
        {
            yield return Zone(false);var idra=Lead("Idra");Place(idra,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var body=Dummy(false,new Vector3(200,0,1.7f));yield return Frames(3);idra.target=body.Health;var hits=Watch(body,idra);
            var weights=new[]{HitWeight.Medium,HitWeight.Heavy,HitWeight.Finisher};float[] his={1,1,1.5f},hisBreak={20,20,30};
            for(int stage=0;stage<3;stage++)
            {
                float pressed=Time.unscaledTime;yield return Swing(idra,hits,stage+1);
                Assert.AreEqual(stage+1,hits.count,$"hit {stage+1} of the chain didn't land on a body a stride ahead of her");
                Assert.AreEqual(idra.damage*HeroSkills.HammerHit[stage],hits.each[stage],idra.damage*.05f,$"hit {stage+1} isn't {HeroSkills.HammerHit[stage]} of her Output");
                Assert.AreEqual(weights[stage],hits.weights[stage],$"hit {stage+1} doesn't land with the weight of the heaviest swing in the party");
                Assert.Greater(HeroSkills.HammerHit[stage],his[stage],$"hit {stage+1} is no heavier than Taren's");Assert.Greater(HeroSkills.HammerBreak[stage],hisBreak[stage],$"hit {stage+1} breaks no more than Taren's");
                Assert.Greater(HeroSkills.HammerSeconds[stage],GroundMove.Cut(stage).duration,$"swing {stage+1} is as quick as Taren's");
                // It lands part-way through the swing, not as the button goes down.
                Assert.Greater(hits.at[stage]-pressed,HeroSkills.HammerSeconds[stage]*.3f,$"hit {stage+1} lands as the swing starts");
                // And the next can't begin until this swing is done.
                Assert.IsFalse(idra.Attack(),$"swing {stage+2} began inside swing {stage+1}");
            }
            // Break adds up as it says: one chain of hers opens a hound (30+36+60 against its 80), where Taren's whole chain (20+20+30) leaves it standing.
            Assert.Greater(HeroSkills.HammerBreak.Sum(),80);Assert.Less(hisBreak.Sum(),80,"fixture: Taren's chain alone doesn't break a hound");
            Object.Destroy(body.gameObject);yield return new WaitForSecondsRealtime(1.3f);
            var hound=Enemy("Ridgehound",new Vector3(200,0,1.7f));hound.Health.maximum=hound.Health.integrity=1000000;hound.Health.weakness=hound.Health.resistance=DamageType.Pulse;
            Assert.AreEqual(80,hound.Health.breakThreshold,.01f,"fixture: a hound's Break");
            yield return Frames(3);idra.target=hound.Health;var second=Watch(hound,idra);
            yield return Swing(idra,second,1);yield return Swing(idra,second,2);Assert.IsFalse(hound.Health.Broken,"fixture: two swings don't break a hound");
            yield return Swing(idra,second,3);Assert.AreEqual(3,second.count,"fixture: the second chain");
            Assert.IsTrue(hound.Health.Broken,"one chain of the hammer doesn't break a hound");
        }
        [UnityTest] public IEnumerator TheThirdHitPutsALightBodyDownAndNeverTakesBackATell()
        {
            yield return Zone(false);var idra=Lead("Idra");Place(idra,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            idra.Health.InvulnerableUntil=GameTime.Now+60;
            EnemyBrain Hound(){var hound=Enemy("Ridgehound",new Vector3(200,0,1.7f));hound.Health.maximum=hound.Health.integrity=1000000;hound.Health.breakThreshold=1e9f;return hound;}
            // Asleep (a body held for a scene): struck, and left where it stands.
            var body=Hound();yield return Frames(3);idra.target=body.Health;var hits=Watch(body,idra);
            yield return Swing(idra,hits,1);yield return Swing(idra,hits,2);yield return Swing(idra,hits,3);
            Assert.AreEqual(3,hits.count,"fixture: the chain");Assert.LessOrEqual(body.Health.SlowUntil,Time.time,"a body held asleep was knocked down");
            Object.Destroy(body.gameObject);yield return new WaitForSecondsRealtime(1.3f);
            // Awake and between attacks: down. It's slowed for a while, and what it does next waits.
            body=Hound();yield return Frames(3);idra.target=body.Health;hits=Watch(body,idra);
            yield return Swing(idra,hits,1);yield return Swing(idra,hits,2);
            var awake=body;yield return Swing(idra,hits,3,()=>{awake.Passive=false;awake.Interrupt();});
            Assert.AreEqual(3,hits.count,"fixture: the chain on a waking hound");
            Assert.Greater(body.Health.SlowUntil,Time.time+HeroSkills.HammerDown*.6f,"the third hit didn't put a light body down");
            Object.Destroy(body.gameObject);yield return new WaitForSecondsRealtime(1.3f);
            // Awake and telling its bite: the hit lands, and the tell stands. A marked attack is never taken back by a shove.
            body=Hound();yield return Frames(3);idra.target=body.Health;hits=Watch(body,idra);
            yield return Swing(idra,hits,1);yield return Swing(idra,hits,2);
            // It wakes as the swing begins, so its two-thirds of a second of tell is running when the hammer lands four tenths in.
            bool told=false;var telling=body;yield return Swing(idra,hits,3,()=>{telling.Passive=false;told=telling.Telegraphing;});
            Assert.AreEqual(3,hits.count,"fixture: the third hit on a telling hound");
            Assert.IsTrue(told,"fixture: the hound was telling in the frame before the hit");
            Assert.LessOrEqual(body.Health.SlowUntil,Time.time,"the third hit knocked down a body in the middle of its tell");
            Assert.IsTrue(body.Telegraphing||body.Attacking,"the third hit took a marked attack back");
            // And a heavy body is never put down.
            Object.Destroy(body.gameObject);yield return new WaitForSecondsRealtime(1.3f);
            var husk=Enemy("SentinelHusk",new Vector3(200,0,2.2f));husk.Health.maximum=husk.Health.integrity=1000000;husk.Health.breakThreshold=1e9f;
            yield return Frames(3);idra.target=husk.Health;hits=Watch(husk,idra);
            yield return Swing(idra,hits,1);yield return Swing(idra,hits,2);var heavy=husk;yield return Swing(idra,hits,3,()=>{heavy.Passive=false;heavy.Interrupt();});
            Assert.AreEqual(3,hits.count,"fixture: the chain on a husk");Assert.LessOrEqual(husk.Health.SlowUntil,Time.time,"the third hit knocked a heavy body down");
        }

        // ------------------------------------------------------------------ Brace
        [UnityTest] public IEnumerator BraceStopsEightyFivePercentAndPaysLessThrustThanAGuard()
        {
            yield return Zone(false);var idra=Lead("Idra");Place(idra,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var body=Dummy(false,new Vector3(200,0,3));yield return Frames(3);float hit=idra.Health.maximum*.2f;
            float bare=Take(idra,body.Health,hit);Assert.AreEqual(Plated(idra,hit),bare,.5f,"fixture: an open hit");
            yield return new WaitForSecondsRealtime(.7f);idra.Health.integrity=idra.Health.maximum;
            idra.Guard(true);yield return new WaitForSecondsRealtime(.4f);
            idra.thrust=100;float taken=Take(idra,body.Health,hit);
            Assert.AreEqual(0,idra.FlashGuards,"fixture: a held guard isn't a perfect one");
            Assert.AreEqual(bare*.15f,taken,.5f,"Brace doesn't stop 85% of a hit");
            Assert.Less(taken,bare*CombatActor.GuardTakes*.6f,"Brace lets through what a guard does");
            float spent=100-idra.thrust,stopped=bare*.85f;
            Assert.AreEqual(idra.Style.guardCost*stopped/idra.Health.maximum,spent,1,"Brace's price isn't in proportion to what it stopped");
            Assert.Greater(spent,10,"Brace is free: holding X is total cover for nothing");
            // The same hit on a guard: it stops less, and pays more for it.
            float guard=CombatActor.GuardCost*bare*(1-CombatActor.GuardTakes)/idra.Health.maximum;
            Assert.Less(spent,guard-3,$"Brace stopped more of the hit than a guard would and paid {spent:0.0} Thrust where a guard pays {guard:0.0}: it's meant to cost less for each hit stopped");
            idra.Guard(false);
            // Sela, in the same party, guards as she always did.
            Place(idra,new Vector3(260,0,8));var sela=Lead("Sela");Place(sela,new Vector3(200,0,0));yield return Frames(2);sela.Health.integrity=sela.Health.maximum;
            float hers=sela.Health.maximum*.2f;sela.Guard(true);yield return new WaitForSecondsRealtime(.4f);sela.thrust=100;
            float through=Take(sela,body.Health,hers);
            Assert.AreEqual(Plated(sela,hers)*CombatActor.GuardTakes,through,.5f,"Sela's guard no longer stops its 65%");
            Assert.AreEqual(CombatActor.GuardCost*Plated(sela,hers)*(1-CombatActor.GuardTakes)/sela.Health.maximum,100-sela.thrust,1,"Sela's guard no longer costs what it did");
            sela.Guard(false);
        }
        [UnityTest] public IEnumerator AFlashGuardAnswersWithAShockwaveRoundHerMarkedAsARing()
        {
            yield return Zone(false);var idra=Lead("Idra");Place(idra,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            // One beside her, one behind her, one out of the wave's reach.
            var beside=Dummy(false,new Vector3(202.5f,0,0));var behind=Dummy(false,new Vector3(200,0,-3));var beyond=Dummy(false,new Vector3(200,0,7));
            yield return Frames(3);var first=Watch(beside,idra);var second=Watch(behind,idra);var third=Watch(beyond,idra);
            var kit=idra.GetComponent<BraceKit>();int rings=Object.FindObjectsByType<SkillRing>(FindObjectsSortMode.None).Length;
            yield return new WaitForSecondsRealtime(.6f);
            idra.thrust=100;idra.Guard(true);float taken=Take(idra,beside.Health,idra.Health.maximum*.2f);
            Assert.AreEqual(1,idra.FlashGuards,"fixture: a perfect guard");Assert.AreEqual(0,taken,.01f,"fixture: a perfect guard takes nothing");
            Assert.AreEqual(0,first.count,"the shockwave left from inside the hit that earned it");
            yield return Frames(2);
            Assert.AreEqual(1,kit.Shockwaves,"a Flash Guard released no shockwave, or more than one");
            Assert.AreEqual(1,first.count,"the shockwave missed a body beside her");Assert.AreEqual(1,second.count,"the shockwave isn't all round her: a body behind her was missed");
            Assert.AreEqual(0,third.count,$"the shockwave reached a body 7 m off: it's {idra.Style.counterReach} m");
            Assert.AreEqual(idra.damage*idra.Style.counter,first.each[0],idra.damage*.06f,"the shockwave isn't her style's counter in Output");
            Assert.AreEqual(HitWeight.Heavy,first.weights[0],"the shockwave doesn't land as a heavy hit");
            Assert.Greater(Object.FindObjectsByType<SkillRing>(FindObjectsSortMode.None).Length,rings,"nothing marks the shockwave: it has to read as hers");
            idra.Guard(false);
            // A guard merely held answers nothing; and neither does Sela's perfect one.
            yield return new WaitForSecondsRealtime(.7f);idra.Guard(true);yield return new WaitForSecondsRealtime(.4f);idra.thrust=100;
            Take(idra,beside.Health,idra.Health.maximum*.1f);yield return Frames(2);
            Assert.AreEqual(1,kit.Shockwaves,"a held guard answered with a shockwave with no Counterweight armed");
            idra.Guard(false);
            Place(idra,new Vector3(260,0,8));var sela=Lead("Sela");Place(sela,new Vector3(200,0,0));yield return new WaitForSecondsRealtime(.6f);
            var hers=Watch(beside,sela);sela.Guard(true);Take(sela,beside.Health,sela.Health.maximum*.2f);yield return Frames(2);
            Assert.AreEqual(1,sela.FlashGuards,"fixture: Sela's perfect guard");
            Assert.AreEqual(0,sela.GetComponent<BraceKit>().Shockwaves,"Sela's Flash Guard releases a shockwave");Assert.AreEqual(0,hers.count,"Sela's Flash Guard hurt the body beside her");
            sela.Guard(false);
        }

        // ------------------------------------------------------------------ the hop
        [UnityTest] public IEnumerator HerHopIsShortAndOnlyItsFirstMomentIsUntouchable()
        {
            yield return Zone(false);var idra=Lead("Idra");Place(idra,new Vector3(200,0,0));Place(Sela,new Vector3(260,0,0));
            var body=Dummy(false,new Vector3(200,0,12));yield return Frames(3);yield return new WaitForSecondsRealtime(.6f);
            var style=idra.Style;var from=idra.transform.position;
            // Its first instant: untouchable, and on the beat a Flash Move like anyone's.
            Assert.IsTrue(idra.Dodge(Vector3.right),"fixture: the hop");
            Assert.AreEqual(0,Take(idra,body.Health,40),.001f,"she can be hit in the first instant of her hop");
            Assert.AreEqual(1,idra.FlashMoves,"a hit in the hop's first instant isn't a Flash Move");
            yield return Drive(idra,.5f);
            Assert.AreEqual(style.hop,Flat(idra.transform.position,from),.5f,"her hop doesn't carry her its 2.2 m");
            Assert.Less(Flat(idra.transform.position,from),3,"her hop is a roll's length");
            // Again, once that Flash and the second and a half nothing can touch her for it have run out. Past the hop's untouchable moment and still in the air: a hit lands.
            yield return new WaitForSecondsRealtime(2.4f);idra.Health.InvulnerableUntil=0;idra.thrust=100;
            Assert.IsTrue(idra.Dodge(Vector3.left),"fixture: the second hop");float began=GameTime.Now;
            while(GameTime.Now-began<style.hopSafe+.02f){idra.motor.Move(Vector2.zero,false,false);yield return null;}
            bool hopping=idra.State==ActorState.Dodge&&GameTime.Now-began<style.hopSeconds;
            Assert.IsTrue(hopping,"fixture: a frame ran longer than the hop; the late hit can't be told from one after it");
            Assert.Greater(Take(idra,body.Health,40),0,$"her hop is untouchable {GameTime.Now-began:0.00} s in: only its first {style.hopSafe} s is meant to be");
            Assert.AreEqual(1,idra.FlashMoves,"a hit past the hop's untouchable moment earned a Flash Move");
            yield return Drive(idra,.5f);
            // Sela's roll, the same test: untouchable all the way.
            Place(idra,new Vector3(260,0,8));var sela=Lead("Sela");Place(sela,new Vector3(200,0,0));yield return new WaitForSecondsRealtime(.6f);
            Assert.IsTrue(sela.Dodge(Vector3.right),"fixture: Sela's roll");began=GameTime.Now;
            while(GameTime.Now-began<style.hopSafe+.02f){sela.motor.Move(Vector2.zero,false,false);yield return null;}
            if(sela.State==ActorState.Dodge&&GameTime.Now-began<.25f)Assert.AreEqual(0,Take(sela,body.Health,40),.001f,"Sela can be hit in the middle of her roll");
            yield return Drive(sela,.5f);
            Assert.AreEqual(3.3f,Flat(sela.transform.position,new Vector3(200,0,0)),.5f,"Sela's roll no longer carries her 3.3 m");
        }

        // ------------------------------------------------------------------ in flight
        [UnityTest] public IEnumerator InFlightHerGuardIsAProwThatCoversOnlyWhatComesFromAhead()
        {
            yield return Zone(true);var idra=Lead("Idra");Place(idra,new Vector3(200,1,0));Place(Sela,new Vector3(260,1,0));
            var ahead=Dummy(true,new Vector3(200,1,9));var behind=Dummy(true,new Vector3(200,1,-9));
            // Fifty degrees off the nose is under the prow's 120; seventy isn't.
            var inside=Dummy(true,new Vector3(200,1,0)+Quaternion.Euler(0,50,0)*Vector3.forward*9);var outside=Dummy(true,new Vector3(200,1,0)+Quaternion.Euler(0,-70,0)*Vector3.forward*9);
            yield return Frames(3);float hit=idra.Health.maximum*.1f;
            float bare=Take(idra,ahead.Health,hit);Assert.Greater(bare,0,"fixture: an open hit on her hull");
            yield return new WaitForSecondsRealtime(.7f);
            idra.Guard(true);Assert.IsTrue(idra.Guarding,"her ship can't raise its guard");
            for(float end=Time.unscaledTime+.4f;Time.unscaledTime<end;){idra.Guard(true);idra.motor.Move(Vector2.zero,false,false);yield return null;}
            Assert.AreEqual(120,idra.Style.prowArc,.01f,"the prow doesn't cover 120 degrees");
            var arc=idra.GetComponentsInChildren<FlightEnergyArc>(true).FirstOrDefault(a=>a.name=="Prow guard");Assert.IsNotNull(arc,"nothing shows the prow guard is up");
            idra.thrust=100;float front=Take(idra,ahead.Health,hit);float spent=100-idra.thrust;
            Assert.AreEqual(bare*idra.Style.guardTakes,front,bare*.04f,"the prow doesn't stop 85% of what comes from dead ahead");Assert.Greater(spent,1,"the prow stops a hit for no Thrust");
            idra.thrust=100;Assert.AreEqual(bare*idra.Style.guardTakes,Take(idra,inside.Health,hit),bare*.04f,"the prow doesn't cover a hit from 50 degrees off the nose");
            idra.thrust=100;Assert.AreEqual(bare,Take(idra,outside.Health,hit),bare*.04f,"the prow stopped a hit from 70 degrees off the nose: it covers 60 each way");
            Assert.AreEqual(100,idra.thrust,.5f,"a hit the prow didn't cover was charged for");
            Assert.AreEqual(bare,Take(idra,behind.Health,hit),bare*.04f,"the prow stopped a hit from behind");
            Assert.AreEqual(0,idra.FlashGuards,"fixture: none of these was a perfect guard");
            idra.Guard(false);yield return Frames(2);Assert.IsFalse(idra.Guarding);
            // On the beat it's a Flash Guard like any other, shockwave and all; and from behind there's no such thing.
            var hits=Watch(ahead,idra);yield return new WaitForSecondsRealtime(.6f);
            idra.Guard(true);Assert.AreEqual(bare,Take(idra,behind.Health,hit),bare*.04f,"a prow raised on the beat turned away a hit from behind");Assert.AreEqual(0,idra.FlashGuards,"a hit from behind earned a Flash Guard");
            idra.Guard(false);yield return new WaitForSecondsRealtime(.6f);
            idra.Guard(true);Assert.AreEqual(0,Take(idra,ahead.Health,hit),.01f,"a prow raised on the beat didn't turn the hit away whole");Assert.AreEqual(1,idra.FlashGuards,"a prow raised on the beat isn't a Flash Guard");
            yield return Frames(2);Assert.AreEqual(1,idra.GetComponent<BraceKit>().Shockwaves,"a Flash Guard on the prow released no shockwave");
            idra.Guard(false);
            // Nobody else's ship has a guard.
            Place(idra,new Vector3(260,1,8));var sela=Lead("Sela");Place(sela,new Vector3(200,1,0));yield return Frames(2);sela.Guard(true);
            Assert.IsFalse(sela.Guarding,"Sela's ship can guard: the prow is meant to be the only guard in flight");
            float open=sela.Health.maximum*.1f;float before=sela.Health.integrity;sela.Health.Receive(new DamagePacket{source=ahead.Health,amount=open,type=DamageType.Kinetic});
            Assert.AreEqual(Plated(sela,open),before-sela.Health.integrity,open*.04f,"a hit on Sela's ship was guarded");
        }
        [UnityTest] public IEnumerator InFlightHerHammerIsAShortBreachingCutterOffTheProw()
        {
            yield return Zone(true);var idra=Lead("Idra");Place(idra,new Vector3(200,1,0));Place(Sela,new Vector3(260,1,0));
            var near=Dummy(true,new Vector3(200,1,4));var far=Dummy(true,new Vector3(200,1,9));var side=Dummy(true,new Vector3(204,1,-1));
            yield return Frames(3);idra.target=near.Health;var first=Watch(near,idra);var second=Watch(far,idra);var third=Watch(side,idra);
            int shots=Projectile.Live.Count;
            // Half a beat (D400: a ship's .17 s since November 24, where .15 s was most of the hammer's .4): the cut lands as it is swung.
            Assert.IsTrue(idra.Attack(),"fixture: the cutter");yield return Drive(idra,HeroSkills.CutterSeconds*.5f);
            Assert.AreEqual(1,first.count,"the cutter missed a body 4 m off her prow");
            Assert.AreEqual(idra.damage*HeroSkills.CutterHit,first.each[0],idra.damage*.05f,"the cutter isn't its share of her Output");
            Assert.AreEqual(HitWeight.Medium,first.weights[0],"the cutter lands like an emitter's tick");
            Assert.AreEqual(0,second.count,$"the cutter reached a body 9 m off: it's {HeroSkills.CutterReach} m");Assert.AreEqual(0,third.count,"the cutter struck a body off to her side and behind");
            Assert.AreEqual(shots,Projectile.Live.Count,"her plain attack in flight fired a shot: it's a cutter, not an emitter");
            Assert.IsFalse(idra.Attack(),"the cutter has no beat between cuts");
            yield return Drive(idra,HeroSkills.CutterSeconds);
            Assert.IsTrue(idra.Attack(),"the cutter never comes round again");yield return Drive(idra,.15f);Assert.AreEqual(2,first.count,"the second cut missed");
        }
        IEnumerator Hold(bool x,bool a)
        {
            var state=new GamepadState();if(x)state=state.WithButton(GamepadButton.West);if(a)state=state.WithButton(GamepadButton.South);
            InputSystem.QueueStateEvent(pad,state);yield return null;yield return null;
        }
        [UnityTest] public IEnumerator HoldingXInFlightBracesTheProwWhereTheOthersLunge()
        {
            pad=InputSystem.AddDevice<Gamepad>("IdraPad");yield return null;
            yield return Zone(true);var idra=Lead("Idra");Place(idra,new Vector3(200,1,0));Place(Sela,new Vector3(260,1,0));
            var body=Dummy(true,new Vector3(200,1,4));yield return Frames(3);idra.target=body.Health;var hits=Watch(body,idra);
            idra.GetComponent<PlayerBrain>().AutoPilot=false;yield return Hold(false,false);
            var from=idra.transform.position;
            yield return Hold(true,false);yield return Frames(3);
            Assert.IsTrue(idra.Guarding,"X held in flight doesn't raise her prow");
            yield return Hold(true,false);yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(idra.Guarding,"the prow drops while X is still held");
            Assert.Less(Flat(idra.transform.position,from),1.5f,"X lunged her ship: on hers it's the prow");
            // The cutter waits while the prow is up, and comes back with it down.
            int before=hits.count;yield return Hold(true,true);yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(before,hits.count,"she cut with the prow raised: a guard isn't an attack");Assert.IsTrue(idra.Guarding,"firing dropped the prow");
            yield return Hold(false,true);yield return new WaitForSecondsRealtime(.5f);
            Assert.IsFalse(idra.Guarding,"the prow stays up with X released");Assert.Greater(hits.count,before,"with the prow down, the fire button doesn't cut");
            yield return Hold(false,false);
            // Sela's X is still the lunge.
            var sela=Lead("Sela");Place(sela,new Vector3(200,1,-20));yield return Frames(2);sela.GetComponent<PlayerBrain>().AutoPilot=false;sela.thrust=100;
            var start=sela.transform.position;yield return Hold(true,false);yield return new WaitForSecondsRealtime(.4f);yield return Hold(false,false);
            Assert.IsFalse(sela.Guarding,"X raised a guard on Sela's ship");Assert.Greater(Flat(sela.transform.position,start),1.5f,"X no longer lunges Sela's ship");
        }
    }
}
