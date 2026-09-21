using System;
using System.IO;
using Lattice.Core;
using Lattice.Combat;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using NUnit.Framework;
using UnityEngine;
namespace Lattice.Tests.EditMode
{
    public class CoreSystemTests
    {
        [Test]public void FinishingFlashCannotUnpauseAMenu()
        {
            try{GameTime.BeginFlash();GameTime.Paused=true;GameTime.EndFlash();Assert.AreEqual(0,Time.timeScale);GameTime.Paused=false;Assert.AreEqual(1,Time.timeScale);}
            finally{GameTime.Reset();}
        }
        [Test]public void GatheringBeforeAcceptingQuestStillCountsAfterCrafting()
        {
            var q=ScriptableObject.CreateInstance<QuestDef>();
            try
            {
                q.id="LateCrystals";q.steps=new[]{new Objective{kind=ObjectiveKind.Collect,target="RidgeCrystal",count=6}};
                var state=new GameState();var inv=new Inventory(state);inv.Give("RidgeCrystal",6);inv.Spend(new[]{new Ingredient{id="RidgeCrystal",count=4}});
                var service=new QuestService(state,new[]{q});service.Start(q.id);Assert.AreEqual(1,state.questSteps[q.id]);Assert.AreEqual(2,state.materials["RidgeCrystal"]);
            }
            finally{UnityEngine.Object.DestroyImmediate(q);}
        }
        [TestCase(DamageType.Beam,150)] [TestCase(DamageType.Pulse,50)] [TestCase(DamageType.Kinetic,100)]
        public void DamageHonorsWeaknessAndResistance(DamageType type,float expected)
        {Assert.AreEqual(expected,DamageMath.Resolve(100,type,DamageType.Beam,DamageType.Pulse,false,false));}
        [Test]public void BrokenCriticalAndWeakPointMultiply()
        {Assert.AreEqual(675,DamageMath.Resolve(100,DamageType.Beam,DamageType.Beam,DamageType.Pulse,true,true,2));}
        [Test]public void WeaknessDoublesBreakNotResistance()
        {Assert.AreEqual(40,DamageMath.Break(20,DamageType.Pulse,DamageType.Pulse));Assert.AreEqual(20,DamageMath.Break(20,DamageType.Beam,DamageType.Pulse));}
        [Test]public void SaveRoundTripAllSlotsAndCorruption()
        {
            string dir=Path.Combine(Path.GetTempPath(),"lattice-test-"+Guid.NewGuid());
            try
            {
                var saves=new SaveSystem(dir);var state=new GameState{zone="Gullet_Tunnel",activeMember=1};
                state.party.Add(new MemberState{id="Sela",level=4});state.flags["warpkey"]=true;state.materials["RidgeCrystal"]=6;
                state.parts.Add(new PartInstance{definitionId="EdgesT2",upgrade=3,affixes=new(){"Resonant"}});state.craftingXp["Edges"]=45;
                foreach(string slot in new[]{"slot1","slot2","slot3","autosave"})
                {saves.Save(slot,state);var load=saves.Load(slot);Assert.AreEqual(2,load.party.Count);Assert.AreEqual("Sela",load.party[1].id);Assert.AreEqual("Gullet_Tunnel",load.zone);Assert.IsTrue(load.flags["warpkey"]);Assert.AreEqual(4,load.party[1].level);Assert.AreEqual(3,load.parts[0].upgrade);Assert.AreEqual("Resonant",load.parts[0].affixes[0]);}
                File.WriteAllText(saves.PathForSlot("slot2"),"broken{");Assert.IsNull(saves.Load("slot2"));Assert.Throws<ArgumentException>(()=>saves.PathForSlot("../escape"));
            }finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
        [Test]public void InventoryPaymentIsAtomicAndAggregatesDuplicateIngredients()
        {
            var state=new GameState();var inv=new Inventory(state);inv.Give("ScrapAlloy",5);
            Assert.IsFalse(inv.Spend(new[]{new Ingredient{id="ScrapAlloy",count=3},new Ingredient{id="ScrapAlloy",count=3}}));Assert.AreEqual(5,inv.Count("ScrapAlloy"));
            Assert.IsTrue(inv.Spend(new[]{new Ingredient{id="ScrapAlloy",count=4}}));Assert.AreEqual(1,inv.Count("ScrapAlloy"));
        }
        [Test]public void FabricationRequiresBenchAndConsumesOnceThenAwardsDisciplineXp()
        {
            var part=ScriptableObject.CreateInstance<TechPartDef>();var recipe=ScriptableObject.CreateInstance<RecipeDef>();var affix=ScriptableObject.CreateInstance<AffixDef>();
            try{
                part.id="Edge";affix.id="Resonant";part.affixPool=new[]{affix};recipe.output=part;recipe.discipline=Discipline.Edges;recipe.inputs=new[]{new Ingredient{id="ScrapAlloy",count=3}};
                var state=new GameState();state.materials["ScrapAlloy"]=3;var f=new Fabrication(state);
                Assert.IsFalse(f.Craft(recipe,false,0,out _));Assert.AreEqual(3,state.materials["ScrapAlloy"]);
                Assert.IsTrue(f.Craft(recipe,true,0,out var result));Assert.AreEqual("Resonant",result.affixes[0]);Assert.AreEqual(10,f.Xp(Discipline.Edges));Assert.AreEqual(0,f.Xp(Discipline.Frames));
                Assert.IsFalse(f.Craft(recipe,true,0,out _));Assert.AreEqual(1,state.parts.Count);
            }finally{UnityEngine.Object.DestroyImmediate(part);UnityEngine.Object.DestroyImmediate(recipe);UnityEngine.Object.DestroyImmediate(affix);}
        }
        [Test]public void QuestSequenceAndRewardsAreExactlyOnce()
        {
            var q=ScriptableObject.CreateInstance<QuestDef>();
            try{q.id="Moon";q.steps=new[]{new Objective{kind=ObjectiveKind.Collect,target="Crystal",count=2}};q.rewardScrip=50;q.flagsOnComplete=new[]{"warpkey"};
                var state=new GameState();var service=new QuestService(state,new[]{q});service.Start(q.id);service.Report(ObjectiveKind.Kill,"Crystal",9);Assert.AreEqual(0,state.questSteps[q.id]);
                service.Report(ObjectiveKind.Collect,"Crystal");Assert.IsFalse(state.flags.ContainsKey("warpkey"));service.Report(ObjectiveKind.Collect,"Crystal");service.Report(ObjectiveKind.Collect,"Crystal",9);Assert.AreEqual(170,state.scrip);Assert.IsTrue(state.flags["warpkey"]);
            }finally{UnityEngine.Object.DestroyImmediate(q);}
        }
        [Test]public void LevelsCapAtTenAndRetainRemainder()
        {var m=new MemberState{level=1};Assert.AreEqual(2,Levels.Grant(m,340));Assert.AreEqual(3,m.level);Assert.AreEqual(10,m.xp);Levels.Grant(m,99999);Assert.AreEqual(10,m.level);}
        [Test]public void FlightCoastsAndBrakeReducesVelocity()
        {var coast=FlightMotor.Integrate(Vector3.forward*10,Vector3.zero,.1f,19,.75f,12,false);var brake=FlightMotor.Integrate(Vector3.forward*10,Vector3.zero,.1f,19,.75f,12,true);Assert.Greater(coast.z,9);Assert.Less(brake.z,5);Assert.AreEqual(0,coast.y);}
        [Test]public void SyncedPortraitTagPreservesSlotSeven()
        {Assert.AreEqual(7,(int)PortraitEmotion.Synced);Assert.AreEqual(PortraitEmotion.Synced,EmotionTags.Parse(new[]{"#emotion:Synced"}));Assert.AreEqual(PortraitEmotion.Neutral,EmotionTags.Parse(null));}
        [Test]public void OnlyOneGameplayMapEnabled()
        {using(var input=new GameInput()){Assert.IsTrue(input.Ground.enabled);Assert.IsFalse(input.Flight.enabled);input.SetFlight(true);Assert.IsFalse(input.Ground.enabled);Assert.IsTrue(input.Flight.enabled);}}
        [Test]public void EquippedWeaponUpgradeAndAffixAffectOnlyItsAttack()
        {
            var character=ScriptableObject.CreateInstance<CharacterDef>();var edge=ScriptableObject.CreateInstance<TechPartDef>();var affix=ScriptableObject.CreateInstance<AffixDef>();
            try
            {
                character.baseStats=new Stats{output=50};edge.id="edge";edge.slot=GearSlot.Edge;edge.baseStats=new Stats{output=25};affix.bonus=new Stats{output=3};
                var state=new GameState();var inv=new Inventory(state);var item=inv.GivePart(edge);item.upgrade=2;item.affixes.Add("boost");inv.Equip(state.party[0],item,edge);
                var melee=Gear.Calculate(state,state.party[0],character,GearSlot.Edge,_=>edge,_=>affix);
                var ranged=Gear.Calculate(state,state.party[0],character,GearSlot.Emitter,_=>edge,_=>affix);
                Assert.AreEqual(82,melee.output,.001);Assert.AreEqual(50,ranged.output);
                Assert.IsFalse(inv.Salvage(item,edge));Assert.AreEqual(1,state.parts.Count);
            }
            finally{UnityEngine.Object.DestroyImmediate(character);UnityEngine.Object.DestroyImmediate(edge);UnityEngine.Object.DestroyImmediate(affix);}
        }
    }
}
