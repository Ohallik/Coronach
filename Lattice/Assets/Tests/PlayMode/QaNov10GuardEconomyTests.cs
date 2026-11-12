using System.Collections;
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
    /// <summary>November 10, the design calls the QA pass left open. boss269: a held guard took 65% off any hit for nothing. prog158: Husk Core, Choir Resin and
    /// Cantor Pearl sat in the bag as "keep this: no use yet". Each check here failed on the build before (e37c9b4): the guard spent no Thrust and never broke,
    /// Thrust refilled at the same pace under a guard, and no recipe took a trophy.</summary>
    public sealed class QaNov10GuardEconomyTests
    {
        static GameState State=>GameServices.Current.State;
        CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return Zone("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
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
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            if(actor.motor is GroundMotor ground)ground.KeepFacing(Vector3.forward);
            Physics.SyncTransforms();
        }
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Guard fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static CombatActor Lead(string hero)
        {
            var party=PartyController.Current;if(party.Active.character!=hero)Assert.IsTrue(party.Swap(),"fixture: the swap");
            foreach(var member in party.members){member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;}
            return party.Active;
        }
        IEnumerator Taren(System.Action<CombatActor> got)
        {
            Arena();var taren=Lead("Taren");Place(Sela,new Vector3(260,0,0));Place(taren,new Vector3(200,0,0));yield return Frames(2);
            taren.Health.integrity=taren.Health.maximum;got(taren);
        }
        /// <summary>What a hit of this size comes to after Taren's frame, before any guard: the guard is the question, not the plating.</summary>
        static float Plated(CombatActor hero,float amount)=>amount*(100/(100+Mathf.Max(0,hero.plating-10)));
        static float Take(CombatActor hero,float amount){float before=hero.Health.integrity;hero.Health.Receive(new DamagePacket{amount=amount,type=DamageType.Pulse});return before-hero.Health.integrity;}

        // ------------------------------------------------------------------ boss269: what a guard costs
        [UnityTest] public IEnumerator AHeldGuardPaysThrustForWhatItStopsAndAFlashGuardDoesNot()
        {
            CombatActor taren=null;yield return Taren(t=>taren=t);float hit=taren.Health.maximum*.2f;
            // The first press is on the beat: turned away whole, and free even short of the price.
            taren.thrust=10;taren.Guard(true);float taken=Take(taren,hit);taren.Guard(false);
            Assert.AreEqual(1,taren.FlashGuards,"fixture: a perfect guard");Assert.AreEqual(0,taken,.01f,"fixture: a perfect guard takes nothing");
            Assert.GreaterOrEqual(taren.thrust,10,"a Flash Guard was charged for the hit it turned away: timing a guard is meant to stay free");
            // A guard held well past its press stops 65% of the hit, and that much costs Thrust.
            yield return new WaitForSecondsRealtime(.7f);taren.Guard(true);yield return new WaitForSecondsRealtime(.4f);
            taren.thrust=100;taken=Take(taren,hit);
            Assert.AreEqual(1,taren.FlashGuards,"fixture: a held guard isn't a perfect one");
            Assert.AreEqual(Plated(taren,hit)*CombatActor.GuardTakes,taken,.5f,"a guard with Thrust to spare doesn't stop its 65%");
            float spent=100-taren.thrust,stopped=Plated(taren,hit)*(1-CombatActor.GuardTakes);
            Assert.Greater(spent,15,$"a guard stopped {stopped:0} Integrity, a fifth of the bar, and spent {spent:0.0} Thrust: holding X is total cover for nothing (boss269)");
            Assert.AreEqual(CombatActor.GuardCost*stopped/taren.Health.maximum,spent,1,"the guard's price isn't in proportion to what it stopped");
            taren.Guard(false);
        }
        [UnityTest] public IEnumerator AGuardOutOfThrustBreaksTheHitLandsWholeAndXMustComeUp()
        {
            CombatActor taren=null;yield return Taren(t=>taren=t);float hit=taren.Health.maximum*.2f;
            taren.Guard(true);yield return new WaitForSecondsRealtime(.4f);
            taren.thrust=2;float taken=Take(taren,hit);
            Assert.AreEqual(Plated(taren,hit),taken,.5f,"a guard with no Thrust behind it still stopped the hit: an empty bar has to mean an open hero");
            Assert.AreEqual(ActorState.Stagger,taren.State,"a broken guard leaves the hero standing ready: nothing says it broke");
            Assert.IsTrue(taren.GuardBroken,"fixture: the guard broke");
            Assert.Greater(taren.RollRefusedAt,0,"the Thrust bar doesn't flash for a guard that broke on it");
            // X still held, as a thumb would be: the guard waits for a fresh press, so the end of the stagger hands back no free guard and no Flash window.
            for(float end=Time.unscaledTime+CombatActor.GuardBreak+.4f;Time.unscaledTime<end;){taren.Guard(true);yield return null;}
            Assert.AreNotEqual(ActorState.Guard,taren.State,"X held through the break raised the guard again by itself");
            Assert.AreEqual(1,taren.GuardDamageMultiplier,"X held through the break is blocking again without a press");
            taren.Guard(false);yield return null;taren.thrust=100;taren.Guard(true);yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual(ActorState.Guard,taren.State,"a fresh press after a break doesn't guard");
            Assert.Less(Take(taren,hit),Plated(taren,hit)*.5f,"a fresh guard after a break doesn't block");
            taren.Guard(false);
        }
        [UnityTest] public IEnumerator ThrustRefillsSlowerUnderAGuard()
        {
            CombatActor taren=null;yield return Taren(t=>taren=t);
            taren.Guard(true);yield return new WaitForSecondsRealtime(.3f);
            taren.thrust=20;float start=Time.time;yield return new WaitForSecondsRealtime(1);
            float guarded=(taren.thrust-20)/(Time.time-start);taren.Guard(false);
            Assert.AreEqual(CombatActor.GuardedRest,guarded,2,$"Thrust came back at {guarded:0.0} a second under a guard: one held through everything pays for itself");
            taren.thrust=20;start=Time.time;yield return new WaitForSecondsRealtime(1);
            float open=(taren.thrust-20)/(Time.time-start);
            Assert.AreEqual(CombatActor.ThrustRest,open,2,"fixture: Thrust refills at its old pace with no guard up");
        }

        // ------------------------------------------------------------------ prog158: what the trophies are for
        static readonly (string trophy,string part,string refined)[] Trophies={("HuskCore","FramesHusk","FramesT2"),("ChoirResin","EmittersChoir","EmittersT2"),("CantorPearl","ModulesPearl","ModulesT2")};
        static float Sum(Stats s)=>s.output+s.plating+s.response+s.resonance+s.fortune;
        [UnityTest] public IEnumerator EachTrophyIsTheKeyToAPartWorthMakingTheDayItDrops()
        {
            yield return null;
            foreach(var (trophy,partId,refinedId) in Trophies)
            {
                var recipe=GameCatalog.All<RecipeDef>().FirstOrDefault(r=>r.inputs!=null&&r.inputs.Any(i=>i.id==trophy));
                Assert.IsNotNull(recipe,$"no recipe takes {trophy}: a boss's trophy sits in the bag with no use (prog158)");
                Assert.IsNotNull(recipe.output,$"{trophy}'s recipe makes a consumable, which Mira's stall sells for 20 scrip with no trophy at all");
                Assert.AreEqual(partId,recipe.output.id,$"{trophy} makes {recipe.output.id}");
                var refined=GameCatalog.Find<TechPartDef>(refinedId);
                Assert.AreEqual(refined.slot,recipe.output.slot,"fixture: the part stands against the Refined part in its own slot");
                Assert.Greater(Sum(recipe.output.baseStats),Sum(refined.baseStats),$"{recipe.output.displayName} is no better than the {refined.displayName} anyone can buy at Corra's: why make it");
                Assert.Greater(recipe.output.tier,1,$"{recipe.output.displayName} is a tier 1 part, so Mira's stall sells it without the trophy");
                Assert.AreEqual(1,recipe.unlockLevel,$"{recipe.displayName} needs a rank: the day it drops, a player who never made a {recipe.discipline} part can't use it");
                Assert.AreEqual(1,recipe.tier,$"{recipe.displayName} needs {recipe.discipline} {recipe.tier}");
                Assert.IsFalse(GameCatalog.All<ShopInventory>().Any(s=>s.parts!=null&&s.parts.Contains(recipe.output)),$"{recipe.output.displayName} is on sale: the trophy is the only way to it");
                // Made at a bench on a fresh game with just what the recipe asks for.
                var state=new GameState();foreach(var input in recipe.inputs)state.materials[input.id]=input.count;
                var bench=new Fabrication(state);
                Assert.IsTrue(bench.Craft(recipe,true,1,out var made),$"{recipe.displayName} can't be made at a bench with its materials in hand on a fresh game");
                Assert.AreEqual(partId,made.definitionId);Assert.AreEqual(0,state.materials[trophy],$"{recipe.displayName} didn't spend the {trophy}");
                Assert.IsFalse(bench.Craft(recipe,true,1,out _),"fixture: one trophy, one part");
            }
        }
        [UnityTest] public IEnumerator NoMaterialSaysItHasNoUse()
        {
            yield return null;
            foreach(var (trophy,_,_) in Trophies)State.materials[trophy]=1;
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open(PauseMenu.ItemsPage);yield return null;
            string shown=string.Join(" | ",Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Where(t=>t.isActiveAndEnabled).Select(t=>t.text));
            foreach(var (trophy,_,_) in Trophies)StringAssert.Contains(DisplayNames.Item(trophy)+" ×1  ·  used to fabricate",shown,$"{DisplayNames.Item(trophy)} doesn't say it's for fabricating");
            StringAssert.DoesNotContain("no use yet",shown,"a material still says it has no use");
            menu.Close();
        }
    }
}
