using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>A hero who joins the party, or comes back into it after chapters away, is brought up to the floor of the chapter she (re)joins in (Lattice.Rpg.PartyFloor, D329):
    /// every slot empty, or holding a part the Gear page shows worse than the floor's plain part, gets that part, what she wore goes to the bag, a better part stays; once, by flag;
    /// and a save made after such a moment is brought up the same way on load. Idra joining in chapter 6 is its first case (she came with an Edge, a Frame and a Drive, and no stall
    /// stands between her joining and the Host Marshal); Sela's turn back in chapter 10 (D305) its second (she came out in what she had rested in since chapter 6). Each is shown
    /// failing first as the game was (PartyFloor.off).</summary>
    public sealed class JoinKitTests
    {
        readonly List<string> log=new();
        void Logged(string text,string stack,LogType type){if(text.StartsWith("PARTY_JOIN")||text.StartsWith("PARTY_FLOOR")||text.StartsWith("CUT_AWAY"))log.Add(text);}
        static void AsItWas(bool off)=>typeof(PartyFloor).GetField("off",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,off);
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();Time.timeScale=1;SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            log.Clear();Application.logMessageReceived+=Logged;AsItWas(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Application.logMessageReceived-=Logged;AsItWas(false);
            if(DialogueSystem.Current!=null&&DialogueSystem.Current.Running)DialogueSystem.Current.Cancel();
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            TimePlate.Clear();GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static GameState State=>GameServices.Current.State;
        static MemberState Member(string id)=>State.party.Find(m=>m.id==id);
        static string On(string hero,GearSlot slot)=>Member(hero).equipped.TryGetValue(slot,out string id)?State.parts.Find(p=>p.instanceId==id)?.definitionId:null;
        static string Wears(string hero)=>string.Join(", ",Member(hero).equipped.OrderBy(e=>e.Key).Select(e=>e.Key+"="+State.parts.Find(p=>p.instanceId==e.Value)?.definitionId));
        static IEnumerator Zone(string zone,string spawn)
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+40;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            // The heroes stand where they are put and nobody wanders into a crossing.
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            yield return new WaitForSecondsRealtime(.6f);
        }
        /// <summary>Chapter 6's floor as DevLoadout dresses her there: what the rule must bring her up to.</summary>
        static readonly (GearSlot slot,string part)[] IdraFloor={(GearSlot.Edge,"EdgesT3"),(GearSlot.Emitter,"EmittersT2"),(GearSlot.Frame,"FramesT3"),(GearSlot.Drive,"DrivesT3"),(GearSlot.Module,"ModulesT3")};

        // ================================================================== Idra joins (chapter 6)
        IEnumerator Join()
        {
            DevLoadout.Apply("chapter6");
            foreach(string f in new[]{"met.Idra","hoar.planTaken","hoar.reachEntered","hoar.herdMet","hoar.whiteoutMet","hoar.vessaHeard","hoar.ranksSeen","hoar.offerHeard","hoar.slateRead"})GameServices.Current.Flags.SetBool(f,true);
            State.questSteps["LongSleep"]=5;
            Assert.IsFalse(State.party.Exists(m=>m.id=="Idra"),"fixture: chapter 6 arrives without Idra");
            // Where she joins: the dig, a chapter 6 zone (the rule reads the chapter of the zone she joins in).
            yield return Zone("FrozenHost","Arrival");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Assert.AreEqual(6,PartyFloor.Here(),"fixture: the Frozen Host is a chapter 6 zone");
            DialogueSystem.Current.AutoAdvance=true;Assert.IsTrue(DialogueSystem.Current.StartNode("IdraJoins","Idra"),"fixture: the join's conversation starts");
            for(float deadline=Time.unscaledTime+60;Time.unscaledTime<deadline&&(DialogueSystem.Current.Running||!State.party.Exists(m=>m.id=="Idra"));)yield return null;
            Assert.IsTrue(log.Contains("PARTY_JOIN Idra"),"fixture: Idra joined");
        }
        [UnityTest] public IEnumerator IdraJoinsInTheFloorOfTheChapterSheJoinsIn()
        {
            AsItWas(true);yield return Join();
            Assert.IsNull(On("Idra",GearSlot.Module),"fixture: as the game was, Idra joined with a Module: the check below proves nothing ("+Wears("Idra")+")");
            AsItWas(false);yield return Cleanup();yield return Boot();
            yield return Join();
            foreach(var (slot,part) in IdraFloor)Assert.AreEqual(part,On("Idra",slot),$"Idra joined without chapter 6's {part} in her {slot} slot ({Wears("Idra")})");
            Assert.AreEqual(GearSlot.Edge,HeroWeapon.Of("Idra"),"she no longer fights with an Edge");
            Assert.IsTrue(State.flags.TryGetValue(PartyFloor.Flag("Idra",6),out bool done)&&done,"her joining isn't marked, so a load would bring her up again");
        }

        // ================================================================== Sela turns back (chapter 10)
        /// <summary>What the chain's Sela had on when she went to rest in chapter 6 (nov14-recon35 replayed with wearBest): a Tempered Pulse Edge, a Tempered Emitter, a Prime Frame with
        /// Bulwark, a Tempered Drive with Quick and Flashbound, a Prime Module.</summary>
        static Dictionary<string,string> Rested(string hero)
        {
            var inventory=new Inventory(State);var m=Member(hero);var put=new Dictionary<string,string>();
            foreach(var id in m.equipped.Values.ToArray()){var p=State.parts.Find(x=>x.instanceId==id);if(p!=null){inventory.Unequip(p);State.parts.Remove(p);}}
            void Put(string part,params string[] affixes){var def=GameCatalog.Find<TechPartDef>(part);var item=inventory.GivePart(def);item.affixes.AddRange(affixes);Assert.IsTrue(inventory.Equip(m,item,def),"fixture: "+hero+" can't wear "+part);put[part]=item.instanceId;}
            Put("EdgesT4Pulse");Put("EmittersT4");Put("FramesT3","Bulwark");Put("DrivesT4","Quick","Flashbound");Put("ModulesT3");
            return put;
        }
        Dictionary<string,string> selaWore,tarenWore;
        IEnumerator Turn()
        {
            DevLoadout.Apply("chapter10");selaWore=Rested("Sela");tarenWore=Member("Taren").equipped.ToDictionary(e=>e.Key.ToString(),e=>e.Value);
            CollectionAssert.AreEquivalent(new[]{"Taren","Idra"},Deployment.Of(State).Select(m=>m.id),"fixture: chapter 10 arrives with Taren and Idra out, Sela resting");
            foreach(string f in new[]{"end.namesBegun","end.farEnd","end.passageRun"})GameServices.Current.Flags.SetBool(f,true);
            yield return Zone("ReturningPassage","Arrival");
            for(float deadline=Time.unscaledTime+25;!log.Any(l=>l.StartsWith("CUT_AWAY ")&&l.Contains("ReturningPassage/Turn"))&&Time.unscaledTime<deadline;)yield return null;
            Assert.IsTrue(log.Any(l=>l.StartsWith("CUT_AWAY ")),"fixture: the turn made its cut");
            yield return null;for(float deadline=Time.unscaledTime+30;SceneFlow.Current.Loading&&Time.unscaledTime<deadline;)yield return null;yield return null;
            CollectionAssert.AreEquivalent(new[]{"Taren","Sela"},Deployment.Of(State).Select(m=>m.id),"fixture: Sela is out after the turn");
        }
        [UnityTest] public IEnumerator SelaTurningBackIsBroughtUpToChapterTensFloorWhereSheIsUnderIt()
        {
            AsItWas(true);yield return Turn();
            Assert.AreEqual("EmittersT4",On("Sela",GearSlot.Emitter),"fixture: as the game was, Sela turned back in other than her Tempered Emitter: the check below proves nothing ("+Wears("Sela")+")");
            AsItWas(false);yield return Cleanup();yield return Boot();
            yield return Turn();
            string wears=Wears("Sela");
            // Under the floor by the page's numbers: her weapon (60 against the Choirwrought 80), the Edge she doesn't fight with (a Tempered Pulse 64 against the plain 70), the
            // Prime Frame (26 and Bulwark's nothing, against 52), the Prime Module (24 against 40). The Tempered Drive with Quick on it is 32 against the plain Choirwrought 30: kept.
            Assert.AreEqual("EmittersT5",On("Sela",GearSlot.Emitter),"Sela's Tempered Emitter, her weapon, wasn't brought up to chapter 10's Choirwrought at the turn: "+wears);
            Assert.AreEqual("EdgesT4",On("Sela",GearSlot.Edge),"Sela's other weapon wasn't brought up to the floor's plain Tempered Edge: "+wears);
            Assert.AreEqual("FramesT5",On("Sela",GearSlot.Frame),"Sela's Prime Frame wasn't brought up to the Choirwrought one: "+wears);
            Assert.AreEqual("ModulesT5",On("Sela",GearSlot.Module),"Sela's Prime Module wasn't brought up to the Choirwrought one: "+wears);
            Assert.AreEqual("DrivesT4",On("Sela",GearSlot.Drive),"Sela's Tempered Drive with Quick, better than the plain Choirwrought one on the page, was taken off: "+wears);
            // What she wore is in the bag, those very parts, worn by nobody; the Drive is the one she had on.
            foreach(string part in new[]{"EmittersT4","EdgesT4Pulse","FramesT3","ModulesT3"})
            {
                var item=State.parts.Find(p=>p.instanceId==selaWore[part]);
                Assert.IsNotNull(item,$"Sela's {part} was lost at the turn, not put in the bag");Assert.IsNull(new Inventory(State).Wearer(item),$"Sela's {part} is still worn after the turn");
            }
            Assert.AreEqual(selaWore["DrivesT4"],Member("Sela").equipped[GearSlot.Drive],"Sela's own Drive was swapped for another");
            // Taren was out already: he isn't the one rejoining, and nothing of his changes.
            CollectionAssert.AreEquivalent(tarenWore,Member("Taren").equipped.ToDictionary(e=>e.Key.ToString(),e=>e.Value),"the turn changed what Taren wears");
            Assert.IsTrue(State.flags.TryGetValue(PartyFloor.Flag("Sela",10),out bool done)&&done,"her turn isn't marked, so a load would bring her up again");
            Assert.IsTrue(log.Any(l=>l.StartsWith("PARTY_FLOOR Sela chapter 10")),"the turn didn't say what it gave her");
        }

        // ================================================================== a save made after either, loaded
        [UnityTest] public IEnumerator ASaveMadeAfterAJoinOrATurnIsBroughtUpOnLoadOnceInWhatItIsUnder()
        {
            yield return null;
            // Idra, saved after her join as the game was: an Edge, a Frame and a Drive, and a Tempered Module spare in the bag (the rule gives, it doesn't take from the bag).
            DevLoadout.Apply(DevLoadout.JoinedLoadout);var inventory=new Inventory(State);
            foreach(var slot in new[]{GearSlot.Emitter,GearSlot.Module}){var worn=State.parts.First(p=>p.instanceId==Member("Idra").equipped[slot]);inventory.Unequip(worn);State.parts.Remove(worn);}
            inventory.GivePart(GameCatalog.Find<TechPartDef>("ModulesT4"));
            int parts=State.parts.Count;var taren=Member("Taren").equipped.ToDictionary(e=>e.Key,e=>e.Value);
            AsItWas(true);PartyFloor.OnLoad(State);
            Assert.IsNull(On("Idra",GearSlot.Emitter),"fixture: as the game was, a load gave Idra an Emitter: the check below proves nothing");
            AsItWas(false);
            Assert.Greater(PartyFloor.OnLoad(State),0,"a save made after Idra's join, with her bare in two slots, isn't brought up on load");
            Assert.AreEqual("EmittersT2",On("Idra",GearSlot.Emitter),"Idra's empty Emitter slot isn't filled with the floor's on load: "+Wears("Idra"));
            Assert.AreEqual("ModulesT3",On("Idra",GearSlot.Module),"Idra's empty Module slot isn't filled with the floor's on load: "+Wears("Idra"));
            Assert.AreEqual(parts+2,State.parts.Count,"the load gave other than her two empty slots' parts");
            CollectionAssert.AreEquivalent(taren,Member("Taren").equipped,"the load touched what Taren wears");
            // Once: a slot she empties afterwards stays empty through the next load.
            inventory.Unequip(State.parts.First(p=>p.instanceId==Member("Idra").equipped[GearSlot.Emitter]));
            Assert.AreEqual(0,PartyFloor.OnLoad(State),"a second load brought her up again: a free part every load");
            Assert.IsFalse(Member("Idra").equipped.ContainsKey(GearSlot.Emitter),"a slot she emptied after the load was filled again");
            // Sela, saved after the turn in what she had rested in: brought up on load as the turn now brings her up.
            Rested("Sela");GameServices.Current.Flags.SetBool("end.turnedBack",true);
            PartyFloor.OnLoad(State);
            Assert.AreEqual("EmittersT5",On("Sela",GearSlot.Emitter),"a save made after the turn doesn't bring Sela's Emitter up to chapter 10's floor on load: "+Wears("Sela"));
        }
    }
}
