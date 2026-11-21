using System;
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
using UnityEngine;

namespace Lattice.Tests.EditMode
{
    /// <summary>The loot and progression layer's sums (S2, D223): what gear adds up to, what an upgrade and a re-tune cost, how a part from the world is rolled, what the pouch
    /// holds, and what the definitions ProgressionBuilder writes must keep true of each other. Each check fails on the build before it: Gear had no Mods, an affix was a flat
    /// bonus on any tier, an upgrade was 3 Scrap Alloy a level from anywhere, an overclock was always the pool's first affix, and nothing was capped.</summary>
    public sealed class ProgressionTests
    {
        static T Find<T>(string id)where T:ScriptableObject
        {var def=GameCatalog.Find<T>(id);Assert.IsNotNull(def,$"fixture: no {typeof(T).Name} named {id}. Run Lattice.EditorTools.ProgressionBuilder.Build.");return def;}
        static (GameState state,MemberState taren,Inventory bag) Fresh(){var state=new GameState();return (state,state.party[0],new Inventory(state));}
        static PartInstance Wear(GameState state,MemberState member,string id,params string[] affixes)
        {
            var def=Find<TechPartDef>(id);var bag=new Inventory(state);var item=bag.GivePart(def);item.affixes.AddRange(affixes);
            bool second=def.slot==GearSlot.Module&&member.equipped.ContainsKey(GearSlot.Module);
            Assert.IsTrue(bag.Equip(member,item,def,second),"fixture: "+id+" goes on");return item;
        }
        static Stats Sum(GameState state,MemberState member,GearSlot weapon)=>Gear.Calculate(state,member,Find<CharacterDef>(member.id),weapon,GameCatalog.Find<TechPartDef>,GameCatalog.Find<AffixDef>);
        static Mods Effects(GameState state,MemberState member)=>Gear.Modifiers(state,member,GameCatalog.Find<TechPartDef>,GameCatalog.Find<AffixDef>);
        static int Count(Ingredient[] list,string id)=>list.Where(i=>i.id==id).Sum(i=>i.count);
        static readonly FieldInfo[] ModFields=typeof(Mods).GetFields(BindingFlags.Public|BindingFlags.Instance);
        static Mods With(FieldInfo field,float value){object boxed=new Mods();field.SetValue(boxed,value);return (Mods)boxed;}

        // ------------------------------------------------------------------ item 1: the weapon not in hand
        /// <summary>The fight reads every stat but Output off the Edge's sum (PartyController.RefreshStats), and that sum dropped an Emitter's base stats whole with its Output:
        /// the Choir Emitter's Resonance +4 stood on its row and reached nothing. QaNov10GuardEconomyTests compared the stat lines, which is why it couldn't see it.</summary>
        [Test] public void AWeaponNotInHandKeepsOnlyItsOutputToItself()
        {
            var (state,taren,_)=Fresh();var bare=Sum(state,taren,GearSlot.Edge);var choir=Find<TechPartDef>("EmittersChoir");
            Assert.Greater(choir.baseStats.resonance,0,"fixture: the Choir Emitter carries Resonance of its own");
            Wear(state,taren,"EmittersChoir");
            var swing=Sum(state,taren,GearSlot.Edge);var shot=Sum(state,taren,GearSlot.Emitter);
            Assert.AreEqual(bare.resonance+choir.baseStats.resonance,swing.resonance,.001f,"the Choir Emitter's own Resonance is on its row and missing from the sum the fight reads");
            Assert.AreEqual(bare.output,swing.output,.001f,"an Emitter's Output leaks into the swing");
            Assert.AreEqual(bare.output+choir.baseStats.output,shot.output,.001f,"fixture: the Emitter's Output reaches its own attack");
            Assert.AreEqual(swing.resonance,shot.resonance,.001f,"the two sums disagree about a stat that isn't Output");
            // The other way round, for the hero who shoots: an Edge's Plating counts while the Emitter is in hand.
            var (other,sela,_)=Fresh();float plain=Sum(other,sela,GearSlot.Emitter).plating;var heartwood=Find<TechPartDef>("EdgesHeartwood");
            Wear(other,sela,"EdgesHeartwood");
            Assert.AreEqual(plain+heartwood.baseStats.plating,Sum(other,sela,GearSlot.Emitter).plating,.001f,"an Edge's own Plating is lost while the Emitter is in hand");
        }

        // ------------------------------------------------------------------ item 2: mods and tiers
        [Test] public void AnAffixsStatsGrowWithItsPartsTierAndItsEffectDoesNot()
        {
            var resonant=Find<AffixDef>("Resonant");
            for(int tier=1;tier<=5;tier++)
            {
                var module=Find<TechPartDef>("ModulesT"+tier);Assert.AreEqual(tier,module.tier,"fixture: ModulesT"+tier+"'s tier");
                Assert.AreEqual(resonant.bonus.resonance*tier,Gear.AffixStats(resonant,module).resonance,.001f,$"Resonant on a tier {tier} part: a flat bonus that mattered on Scrap is noise on Choirwrought");
            }
            Assert.AreEqual(4,Gear.AffixStats(resonant,Find<TechPartDef>("ModulesT2")).resonance,.001f,"a Resonant Refined part was +4 before affixes scaled, and a save holding one must find it unchanged");
            var (state,taren,_)=Fresh();var bare=Sum(state,taren,GearSlot.Edge);var edge=Find<TechPartDef>("EdgesT3");
            Wear(state,taren,"EdgesT3","Honed","Keen");
            Assert.AreEqual(bare.output+edge.baseStats.output+Find<AffixDef>("Honed").bonus.output*edge.tier,Sum(state,taren,GearSlot.Edge).output,.001f,"Honed on a Prime Edge isn't three times Honed on a Scrap one in the sum the fight reads");
            Assert.AreEqual(Find<AffixDef>("Keen").mods.critChance,Effects(state,taren).critChance,.0001f,"an affix's effect scaled with its part's tier: five points of crit are five on any part");
        }
        [Test] public void EffectsAddAcrossPartsAndAnAttunedWeaponFavoursItsOwnType()
        {
            var (state,taren,_)=Fresh();
            Assert.IsFalse(Effects(state,taren).Any,"fixture: a hero with no parts has no effects");
            Wear(state,taren,"EdgesT3Plasma","Attuned");Wear(state,taren,"ModulesGuardCapacitor");Wear(state,taren,"ModulesPrismPlasma");
            Assert.AreEqual(Find<TechPartDef>("ModulesPrismPlasma").instanceId(state),taren.module2,"fixture: the second module is in the second slot");
            var worn=Effects(state,taren);float attuned=Find<AffixDef>("Attuned").mods.ownType,prism=Find<TechPartDef>("ModulesPrismPlasma").mods.plasma;
            Assert.Greater(attuned,0,"fixture: Attuned");Assert.Greater(prism,0,"fixture: the prism");
            Assert.AreEqual(attuned+prism,worn.plasma,.0001f,"Attuned on a Plasma Edge and a Plasma Prism don't add up to one Plasma bonus");
            Assert.AreEqual(0,worn.ownType,.0001f,"Attuned was left as a bonus to no type at all");Assert.AreEqual(0,worn.kinetic,.0001f,"Attuned on a Plasma Edge favours Kinetic");
            var capacitor=Find<TechPartDef>("ModulesGuardCapacitor");
            Assert.AreEqual(capacitor.mods.guardCost,worn.guardCost,.0001f,"a module's passive doesn't reach the hero");Assert.Less(worn.guardCost,0,"fixture: the capacitor makes a guard cheaper");
            // A third affix on a part isn't read, as a third stat line isn't.
            var (two,sela,_)=Fresh();Wear(two,sela,"EdgesT3","Keen","Vicious","Breaker");
            Assert.AreEqual(0,Effects(two,sela).breakPower,.0001f,"a part reads more than two affixes");
        }
        /// <summary>A field added to Mods and left out of the sum, or of the words, would be an effect that silently does nothing or that no row can explain.</summary>
        [Test] public void EveryModifierIsSummedAndCanBeSaidInWords()
        {
            Assert.Greater(ModFields.Length,25,"fixture: the modifier fields");
            foreach(var field in ModFields)
            {
                Assert.AreEqual(typeof(float),field.FieldType,field.Name+" isn't a number: Mods adds field by field");
                var sum=With(field,.25f)+With(field,.5f);
                Assert.AreEqual(.75f,(float)field.GetValue(sum),.0001f,field.Name+" is left out of Mods' sum: two parts with it count as none");
                Assert.IsTrue(With(field,.25f).Any,field.Name+" alone doesn't count as an effect");
                var words=PartText.Phrases(With(field,.25f)).ToArray();
                Assert.AreEqual(1,words.Length,$"{field.Name} has {words.Length} phrases: a modifier the fight applies needs exactly one way of saying it");
                Assert.IsFalse(Regex.IsMatch(words[0],@"[a-z][A-Z]|_"),$"{field.Name} reads as a raw name: \"{words[0]}\"");
                Assert.IsFalse(Regex.IsMatch(words[0],@"\bcharge\b|\b[Hh]ealth\b|\bHP\b"),$"\"{words[0]}\": the resource is Charge and the stat is Integrity");
            }
            Assert.IsFalse(new Mods().Any,"no effects at all counts as an effect");Assert.IsEmpty(PartText.Says(new Mods()),"no effects at all has words");
        }
        [Test] public void EveryAffixModuleAndConsumableSaysWhatItDoes()
        {
            var edge=Find<TechPartDef>("EdgesT3");var affixes=GameCatalog.All<AffixDef>();
            Assert.GreaterOrEqual(affixes.Length,20,"the affixes ProgressionBuilder writes aren't all there");
            foreach(var affix in affixes)
            {
                string says=PartText.AffixSays(affix,edge);StringAssert.StartsWith(affix.displayName+": ",says,"an affix's words don't lead with its name");
                Assert.Greater(says.Length,affix.displayName.Length+4,$"{affix.id} does nothing a row can say: \"{says}\"");
                Assert.IsFalse(Regex.IsMatch(says,@"[a-z][A-Z]|_"),$"{affix.id} reads as a raw name: \"{says}\"");
            }
            var passives=GameCatalog.All<TechPartDef>().Where(p=>p.mods.Any).ToArray();
            Assert.GreaterOrEqual(passives.Length,14,"the modules with a passive aren't all there");
            foreach(var part in passives)Assert.IsNotEmpty(PartText.Passive(part),part.id+" has a passive and no words for it");
            foreach(var item in GameCatalog.All<ConsumableDef>())
            {
                string says=PartText.Restores(item);Assert.Greater(says.Length,12,$"{item.id} doesn't say what it does: \"{says}\"");
                StringAssert.DoesNotContain("restores  ",says,item.id+" restores nothing");StringAssert.DoesNotContain("restores to",says,item.id+" restores nothing");
                if(item.seconds>0)Assert.IsTrue(item.buff.Any,item.id+" runs for a time and lends nothing");
            }
            StringAssert.Contains("25% Integrity (min 60)",PartText.Restores(Find<ConsumableDef>("RepairGel")),"a gel doesn't say it's a share of the bar with a floor");
        }

        // ------------------------------------------------------------------ item 2: where the mods meet a hit
        [Test] public void GearBonusesAddToEachOtherAndReadTheBodyTheHitLandsOn()
        {
            var hero=new GameObject("ProgressionTests hero",typeof(Health),typeof(CombatActor)).GetComponent<CombatActor>();var body=new GameObject("ProgressionTests body").AddComponent<Health>();
            var skill=ScriptableObject.CreateInstance<SkillDef>();
            try
            {
                body.maximum=body.integrity=100;
                var kinetic=new DamagePacket{type=DamageType.Kinetic};var plasma=new DamagePacket{type=DamageType.Plasma};
                Assert.AreEqual(1,GearEffects.Dealt(hero,plasma,body,body),.0001f,"a hero with no effects hits for more or less than the hit");
                hero.mods=new Mods{damageDealt=.25f,plasma=.15f,brokenDamage=.15f,executeDamage=.2f};
                Assert.AreEqual(1.25f,GearEffects.Dealt(hero,kinetic,body,body),.0001f,"damage dealt +25%");
                Assert.AreEqual(1.4f,GearEffects.Dealt(hero,plasma,body,body),.0001f,"a type's bonus counts only for that type, on top of damage dealt");
                body.integrity=GearEffects.ExecuteBelow*body.maximum-1;
                Assert.AreEqual(1.6f,GearEffects.Dealt(hero,plasma,body,body),.0001f,"Executioner doesn't see a body under its line");
                body.Expose(5);
                Assert.AreEqual(1.75f,GearEffects.Dealt(hero,plasma,body,body),.0001f,"Hunter doesn't see a Broken body");
                Assert.AreEqual(1,GearEffects.Dealt(null,plasma,body,body),.0001f,"a hit nobody's gear is behind is changed");
                hero.mods=new Mods{damageTaken=-.2f};Assert.AreEqual(.8f,GearEffects.Taken(hero),.0001f,"Plating Wax");
                hero.mods=new Mods{damageTaken=-9};Assert.AreEqual(.1f,GearEffects.Taken(hero),.0001f,"stacked far enough, damage taken goes to nothing or below it");
                hero.mods=new Mods{breakPower=.4f,brokenTime=1};Assert.AreEqual(1.4f,GearEffects.Break(hero),.0001f);Assert.AreEqual(1,GearEffects.BrokenTime(hero),.0001f);
                Assert.AreEqual(1,GearEffects.Break(null),.0001f);Assert.AreEqual(0,GearEffects.BrokenTime(null),.0001f);
                // What a skill costs and how long it rests, as the release, the HUD and the row all read it.
                skill.id="ProgressionTestsSkill";skill.chargeCost=20;skill.cooldown=10;
                hero.mods=new Mods{skillCost=-2,cooldown=-.1f,mashCost=-.5f,mastery=1};
                Assert.AreEqual(18,hero.CostOf(skill,1),.0001f,"Overcharged");Assert.AreEqual(9,hero.CooldownOf(skill),.0001f,"Quickcycle");Assert.AreEqual(CombatActor.MashCost*.5f,hero.MashPrice,.0001f,"Fleet");
                Assert.AreEqual(2,hero.MasteryOf(skill),"a Mastery Lens doesn't lift a skill used for the first time to mastery 2");
                skill.chargeCost=9;Assert.AreEqual(SkillBook.MinimumCost,hero.CostOf(skill,1),.0001f,"a cheaper skill goes under the floor every skill has");
                hero.mods=new Mods{mastery=99};Assert.AreEqual(SkillBook.MaxMastery,hero.MasteryOf(skill),"a lens lifts a skill past the top mastery");
                // Fortune's drop luck: capped, the cap raised by a Fortune Compass, and Scavenger added past it.
                hero.fortune=100;hero.mods=new Mods();Assert.AreEqual(.15f,LootDrop.Luck(hero),.0001f,"fixture: Fortune's drop luck stops at 15%");
                hero.mods=new Mods{luckCap=.1f,dropLuck=.05f};Assert.AreEqual(.3f,LootDrop.Luck(hero),.0001f,"a Fortune Compass and Scavenger");
                hero.fortune=10;Assert.AreEqual(.1f,LootDrop.Luck(hero),.0001f,"a raised cap gives luck the hero's Fortune hasn't earned");
            }
            finally{UnityEngine.Object.DestroyImmediate(hero.gameObject);UnityEngine.Object.DestroyImmediate(body.gameObject);UnityEngine.Object.DestroyImmediate(skill);}
        }

        // ------------------------------------------------------------------ item 8: upgrades, salvage, re-tune
        [Test] public void AnUpgradeCostsScrapAndScripByLevelAndTierAndARegionsMaterialFromPrime()
        {
            var refined=Find<TechPartDef>("EdgesT2");var prime=Find<TechPartDef>("EdgesT3");
            int scrip=0,scrap=0;
            for(int level=1;level<=Gear.MaxUpgrade;level++)
            {
                var cost=Gear.UpgradeCost(refined,level);
                Assert.AreEqual(1,cost.materials.Length,"a Refined part's upgrade takes something beside Scrap Alloy and scrip");Assert.AreEqual("ScrapAlloy",cost.materials[0].id);
                Assert.AreEqual(3*level*2,cost.materials[0].count,"Scrap Alloy for level "+level);Assert.AreEqual(25*level*2,cost.scrip,"scrip for level "+level);
                scrip+=cost.scrip;scrap+=cost.materials[0].count;
            }
            Assert.AreEqual(750,scrip,"a Refined part to +5: the scrip sink chapter 2 didn't have (prog160)");Assert.AreEqual(90,scrap);
            Assert.IsNotEmpty(prime.upgradeMaterial,"fixture: a Prime part names its region's common");Assert.IsNotEmpty(prime.upgradeRare,"fixture: a Prime part names its region's rare");
            var fourth=Gear.UpgradeCost(prime,4);var last=Gear.UpgradeCost(prime,5);
            Assert.AreEqual(4,Count(fourth.materials,prime.upgradeMaterial),"a Prime part's fourth level doesn't take four of its region's common");Assert.AreEqual(0,Count(fourth.materials,prime.upgradeRare),"the rare is asked before the last level");
            Assert.AreEqual(45,Count(last.materials,"ScrapAlloy"));Assert.AreEqual(5,Count(last.materials,prime.upgradeMaterial));Assert.AreEqual(1,Count(last.materials,prime.upgradeRare),"the last level doesn't take the region's rare");Assert.AreEqual(375,last.scrip);
            // Paying: at a bench, with all of it in hand, and exactly that.
            var (state,_,bag)=Fresh();var part=bag.GivePart(refined);state.materials["ScrapAlloy"]=6;state.scrip=49;
            Assert.IsFalse(Gear.Upgrade(state,part,refined,true),"an upgrade went through a scrip short");Assert.AreEqual(6,bag.Count("ScrapAlloy"),"a refused upgrade kept the Scrap Alloy");
            state.scrip=50;
            Assert.IsFalse(Gear.Upgrade(state,part,refined,false),"an upgrade went through away from a bench");Assert.AreEqual(0,part.upgrade);Assert.AreEqual(50,state.scrip);
            Assert.IsTrue(Gear.Upgrade(state,part,refined,true),"an upgrade with its price in hand at a bench is refused");
            Assert.AreEqual(1,part.upgrade);Assert.AreEqual(0,state.scrip,"the upgrade didn't take its scrip");Assert.AreEqual(0,bag.Count("ScrapAlloy"),"the upgrade didn't take its Scrap Alloy");
            part.upgrade=Gear.MaxUpgrade;state.scrip=9999;state.materials["ScrapAlloy"]=999;
            Assert.IsFalse(Gear.Upgrade(state,part,refined,true),"a part went past +5");Assert.AreEqual(9999,state.scrip);
        }
        [Test] public void SalvageGivesBackHalfOfWhatAPartsUpgradesTook()
        {
            var (state,_,bag)=Fresh();var prime=Find<TechPartDef>("EdgesT3");var part=bag.GivePart(prime);part.upgrade=Gear.MaxUpgrade;
            int scrapSpent=0,commonSpent=0;for(int level=1;level<=Gear.MaxUpgrade;level++){var cost=Gear.UpgradeCost(prime,level);scrapSpent+=Count(cost.materials,"ScrapAlloy");commonSpent+=Count(cost.materials,prime.upgradeMaterial);}
            int ownScrap=Count(prime.salvage,"ScrapAlloy"),ownCommon=Count(prime.salvage,prime.upgradeMaterial);
            var yield=Inventory.SalvageYield(prime,part);
            Assert.AreEqual(ownScrap+scrapSpent/2,Count(yield,"ScrapAlloy"),"salvage doesn't give back half the Scrap Alloy the upgrades took");
            Assert.AreEqual(ownCommon+commonSpent/2,Count(yield,prime.upgradeMaterial),"salvage doesn't give back half the region's material");
            Assert.AreEqual(0,Count(yield,prime.upgradeRare),"half of one rare is a whole rare back");
            Assert.AreEqual(yield.Length,yield.Select(i=>i.id).Distinct().Count(),"the yield names a material twice");
            Assert.IsTrue(bag.Salvage(part,prime),"fixture: a spare part salvages");
            Assert.AreEqual(ownScrap+scrapSpent/2,bag.Count("ScrapAlloy"));Assert.AreEqual(ownCommon+commonSpent/2,bag.Count(prime.upgradeMaterial));
            // A part that was never upgraded gives its own yield and no more.
            var plain=bag.GivePart(prime);Assert.AreEqual(ownScrap,Count(Inventory.SalvageYield(prime,plain),"ScrapAlloy"),"a plain part's salvage changed");
        }
        [Test] public void ARetuneRollsOneAffixAgainForItsPriceAndNeverRepeatsOne()
        {
            var (state,_,bag)=Fresh();var refined=Find<TechPartDef>("EdgesT2");var part=bag.GivePart(refined);part.affixes.Add("Keen");part.affixes.Add("Honed");
            Assert.Greater(refined.affixPool.Length,2,"fixture: a Refined Edge has other affixes to become");
            int price=Gear.RetuneCost(refined).scrip;Assert.AreEqual(75*refined.tier,price);Assert.IsEmpty(Gear.RetuneCost(refined).materials,"a Refined part's re-tune takes a rare its tier has none of");
            state.scrip=price*40;var rng=new System.Random(3);
            for(int i=0;i<40;i++)
            {
                string was=part.affixes[0],other=part.affixes[1];
                Assert.IsTrue(Gear.Retune(state,part,refined,0,rng,true),"a re-tune with its price in hand at a bench is refused");
                Assert.AreNotEqual(was,part.affixes[0],"a re-tune rolled the affix it had: the scrip bought nothing");Assert.AreNotEqual(other,part.affixes[0],"a re-tune rolled the part's other affix: it carries one twice");
                Assert.AreEqual(other,part.affixes[1],"a re-tune changed the affix nobody asked about");Assert.IsTrue(refined.affixPool.Any(a=>a.id==part.affixes[0]),"a re-tune rolled an affix from outside the part's pool");
            }
            Assert.AreEqual(0,state.scrip,"forty re-tunes didn't cost forty prices");
            state.scrip=price;string kept=part.affixes[0];
            Assert.IsFalse(Gear.Retune(state,part,refined,0,rng,false),"a re-tune went through away from a bench");
            Assert.IsFalse(Gear.Retune(state,part,refined,2,rng,true),"a part has a third affix to re-tune");
            Assert.AreEqual(kept,part.affixes[0]);Assert.AreEqual(price,state.scrip,"a refused re-tune spent scrip");
            // From Prime it takes the region's rare as well, and is refused without one.
            var prime=Find<TechPartDef>("EdgesT3");var better=bag.GivePart(prime);better.affixes.Add("Keen");state.scrip=9999;
            Assert.AreEqual(1,Count(Gear.RetuneCost(prime).materials,prime.upgradeRare),"a Prime part's re-tune doesn't take its region's rare");
            Assert.IsFalse(Gear.Retune(state,better,prime,0,rng,true),"a Prime re-tune went through with no rare");Assert.AreEqual("Keen",better.affixes[0]);
            state.materials[prime.upgradeRare]=1;
            Assert.IsTrue(Gear.Retune(state,better,prime,0,rng,true));Assert.AreEqual(0,bag.Count(prime.upgradeRare),"the re-tune didn't take the rare");Assert.AreNotEqual("Keen",better.affixes[0]);
        }

        // ------------------------------------------------------------------ item 3: rolling
        [Test] public void APartFromTheWorldRollsNoneOneOrTwoAffixesAndAShopsComesClean()
        {
            var prime=Find<TechPartDef>("EdgesT3");var counts=new int[3];var rng=new System.Random(20261111);const int rolls=20000;
            for(int i=0;i<rolls;i++)
            {
                var item=new PartInstance{definitionId=prime.id};Loot.RollAffixes(item,prime,rng);
                Assert.Less(item.affixes.Count,3,"a part rolled a third affix");counts[item.affixes.Count]++;
                Assert.AreEqual(item.affixes.Count,item.affixes.Distinct().Count(),"a part rolled the same affix twice");
                Assert.IsTrue(item.affixes.All(id=>prime.affixPool.Any(a=>a.id==id)),"a part rolled an affix from outside its pool");
            }
            Assert.AreEqual(.55,counts[0]/(double)rolls,.02,"about 55% of parts from the world come with no affix");
            Assert.AreEqual(.35,counts[1]/(double)rolls,.02,"about 35% come with one");Assert.AreEqual(.10,counts[2]/(double)rolls,.02,"about 10% come with two");
            Assert.AreEqual(0,Loot.AffixCount(.999));Assert.AreEqual(1,Loot.AffixCount(Loot.TwoAffixes));Assert.AreEqual(2,Loot.AffixCount(0));
            // What's bought, or handed over by a plain quest, never rolls.
            var (state,_,bag)=Fresh();Loot.Seed(1);
            for(int i=0;i<60;i++)Assert.IsEmpty(bag.GivePart(prime).affixes,"a shop's part rolled an affix");
            bag.Grant("EdgesT3",20,false);Assert.IsTrue(state.parts.All(p=>p.affixes.Count==0),"a part granted plain (a quest's, a talk's) rolled an affix");
            bag.Grant("EdgesT3",60,false,true);Assert.IsTrue(state.parts.Any(p=>p.affixes.Count>0),"sixty parts granted as rolled (a flagged quest reward) rolled nothing");
        }
        [Test] public void ASeedRepeatsEveryDropExactly()
        {
            string Run(int seed)
            {
                Loot.Seed(seed);var (state,_,bag)=Fresh();var said=new List<string>();
                for(int i=0;i<40;i++)said.Add(bag.GrantLoot(new LootEntry{partTier=3,anySlot=true,count=1},false));
                return string.Join(" | ",said)+" || "+string.Join(",",state.parts.Select(p=>p.definitionId+":"+string.Join("+",p.affixes)));
            }
            string first=Run(77);
            Assert.AreEqual(first,Run(77),"the same seed gave different drops: a replay or a test can't repeat a run");
            Assert.AreNotEqual(first,Run(78),"fixture: another seed gives other drops");
            StringAssert.Contains("(",first,"fixture: forty drops with no affix among them");
            Assert.Greater(first.Split(new[]{" || "},StringSplitOptions.None)[1].Split(',').Select(p=>p.Split(':')[0]).Distinct().Count(),3,"fixture: 'any Prime part' kept giving the same part");
        }
        [Test] public void AnOverclockIsAnyAffixOfThePartsPoolAndRollsTwiceFromLadderSix()
        {
            var part=ScriptableObject.CreateInstance<TechPartDef>();var recipe=ScriptableObject.CreateInstance<RecipeDef>();
            var pool=new[]{"First","Second","Third","Fourth"}.Select(id=>{var a=ScriptableObject.CreateInstance<AffixDef>();a.id=a.displayName=id;return a;}).ToArray();
            try
            {
                part.id="ProgressionTestsEdge";part.slot=GearSlot.Edge;part.affixPool=pool;recipe.output=part;recipe.discipline=Discipline.Edges;recipe.inputs=new[]{new Ingredient{id="ScrapAlloy",count=1}};
                var state=new GameState();state.materials["ScrapAlloy"]=100000;var bench=new Fabrication(state);var rng=new System.Random(11);var seen=new HashSet<string>();
                // Below ladder 6: one affix on an overclock, any of the pool, never two.
                for(int i=0;i<200;i++)
                {
                    state.craftingXp["Edges"]=0;Assert.IsTrue(bench.Craft(recipe,true,0,out var made,false,rng),"fixture: the craft");
                    Assert.AreEqual(1,made.affixes.Count,"an overclock below ladder 6 gave no affix, or two");seen.Add(made.affixes[0]);
                    state.craftingXp["Edges"]=0;Assert.IsTrue(bench.Craft(recipe,true,1,out var plain,false,rng));Assert.IsEmpty(plain.affixes,"a craft that didn't overclock came with an affix");
                }
                Assert.AreEqual(pool.Length,seen.Count,"an overclock is always the same affix: it was the pool's first, every time, so a part could never be made with any other");
                // From ladder 6 the overclock rolls a second time, on its own chance.
                int level=Fabrication.SecondOverclockLevel;int two=0,one=0;const int crafts=3000;
                for(int i=0;i<crafts;i++)
                {
                    state.craftingXp["Edges"]=(level-1)*30;Assert.AreEqual(level,bench.Level(Discipline.Edges),"fixture: the ladder");
                    Assert.IsTrue(bench.Craft(recipe,true,0,out var made,false,rng));if(made.affixes.Count==2)two++;
                    Assert.AreEqual(made.affixes.Count,made.affixes.Distinct().Count(),"a double overclock gave the same affix twice");
                    state.craftingXp["Edges"]=(level-1)*30;Assert.IsTrue(bench.Craft(recipe,true,1,out var late,false,rng));if(late.affixes.Count==1)one++;Assert.Less(late.affixes.Count,2);
                }
                state.craftingXp["Edges"]=(level-1)*30;float chance=bench.OverclockChance(Discipline.Edges);
                Assert.AreEqual(chance,two/(float)crafts,.03f,"from ladder 6 an overclocked part doesn't take a second affix at the overclock's own chance");
                Assert.AreEqual(chance,one/(float)crafts,.03f,"from ladder 6 the second roll doesn't stand on its own when the first misses");
            }
            finally{UnityEngine.Object.DestroyImmediate(part);UnityEngine.Object.DestroyImmediate(recipe);foreach(var a in pool)UnityEngine.Object.DestroyImmediate(a);}
        }

        // ------------------------------------------------------------------ item 7: the pouch
        [Test] public void ThePouchHoldsOnlySoManyAndWhatIsFoundOverThatIsPaidInScrip()
        {
            var (state,_,bag)=Fresh();var gel=Find<ConsumableDef>("RepairGel");int cap=Inventory.Cap(gel);
            Assert.AreEqual(9,cap,"the Repair Gel's carry cap (prog159)");Assert.AreEqual(5,Inventory.Cap(Find<ConsumableDef>("ChargeCell")),"the Charge Cell's carry cap");
            foreach(var item in GameCatalog.All<ConsumableDef>()){Assert.GreaterOrEqual(Inventory.Cap(item),1,item.id+" can't be carried at all");Assert.LessOrEqual(Inventory.Cap(item),9,item.id+" has no carry cap: prog159 is open again for it");}
            state.consumables["RepairGel"]=cap-1;int scrip=state.scrip;
            bag.Grant("RepairGel",3,false);
            Assert.AreEqual(cap,bag.Held("RepairGel"),"loot went over the cap");Assert.AreEqual(scrip+2*gel.value,state.scrip,"what didn't fit was lost, not paid for");Assert.IsTrue(bag.Full(gel));
            // The bench: not past the cap, and not in a fight; a refusal spends nothing.
            var recipe=Find<RecipeDef>("RepairGel");var bench=new Fabrication(state);state.materials["ScrapAlloy"]=10;
            Assert.AreEqual(Fabrication.Block.Full,bench.Blocked(recipe,false),"the bench makes a gel the pouch has no room for");
            Assert.IsFalse(bench.Craft(recipe,false,1,out _));Assert.AreEqual(10,bag.Count("ScrapAlloy"),"a refused craft spent its materials");Assert.AreEqual(cap,bag.Held("RepairGel"));
            state.consumables["RepairGel"]=0;
            Assert.AreEqual(Fabrication.Block.Hostile,bench.Blocked(recipe,false,true),"a gel is fabricated from the pause menu with something awake and hostile nearby (prog159)");
            Assert.IsFalse(bench.Craft(recipe,false,1,out _,true));Assert.AreEqual(10,bag.Count("ScrapAlloy"));
            Assert.IsTrue(bench.Craft(recipe,false,1,out _),"with room and no fight a gel can't be made");Assert.AreEqual(1,bag.Held("RepairGel"));
            // A part is bench work whatever is awake: the fight doesn't stop the anvil, which already refuses at its own prompt.
            var edge=Find<RecipeDef>("EdgesT1");Assert.AreEqual(Fabrication.Block.None,bench.Blocked(edge,true,true));Assert.AreEqual(Fabrication.Block.Bench,bench.Blocked(edge,false));
        }
        [Test] public void ADraughtAndAMealRunSideBySideAndASecondOfAKindTakesTheFirstsPlace()
        {
            var state=new GameState();var surge=Find<ConsumableDef>("SurgeDraught");var wax=Find<ConsumableDef>("PlatingWax");var stew=Find<ConsumableDef>("PodStew");
            Assert.IsFalse(surge.meal);Assert.IsTrue(stew.meal,"fixture: Pod Stew is a meal");Assert.Greater(stew.seconds,surge.seconds,"a meal doesn't outlast a draught");
            Assert.IsFalse(PartyBuffs.Current(state).Any,"fixture: nothing runs on a new game");
            PartyBuffs.Begin(state,surge);PartyBuffs.Begin(state,stew);
            var both=PartyBuffs.Current(state);
            Assert.AreEqual(surge.buff.damageDealt,both.damageDealt,.0001f,"the draught doesn't run");Assert.AreEqual(stew.buff.regen,both.regen,.00001f,"the meal took the draught's place, or doesn't run");
            PartyBuffs.Begin(state,wax);var second=PartyBuffs.Current(state);
            Assert.AreEqual(0,second.damageDealt,.0001f,"two draughts run at once");Assert.AreEqual(wax.buff.damageTaken,second.damageTaken,.0001f);Assert.AreEqual(stew.buff.regen,second.regen,.00001f,"a second draught ended the meal");
            PartyBuffs.Tick(state,wax.seconds+1);
            Assert.AreEqual(0,PartyBuffs.Current(state).damageTaken,.0001f,"a draught outlives its minute");Assert.IsNull(state.draught);Assert.AreEqual(stew.buff.regen,PartyBuffs.Current(state).regen,.00001f,"the meal ran out with the draught");
            Assert.AreEqual(1,PartyBuffs.Running(state).Count());
            PartyBuffs.Tick(state,stew.seconds);Assert.IsFalse(PartyBuffs.Current(state).Any,"a meal never runs out");Assert.IsNull(state.meal);
        }

        // ------------------------------------------------------------------ item 14: old saves
        static string OldSave(string consumables,string parts,string equipped)=>
            ("{'version':2,'savedAtUtc':'2026-11-08T20:11:09.0000000Z','zone':'Lanternwick','spawn':'Arrival','activeMember':0,'quickItem':'RepairGel',"+
            "'party':[{'id':'Taren','level':10,'xp':0,'integrity':282.0,'charge':0.0,'gear':[],'equipped':"+equipped+",'module2':null},{'id':'Sela','level':10,'xp':0,'integrity':282.0,'charge':0.0,'gear':[],'equipped':{},'module2':null}],"+
            "'flags':{'chapter2.complete':true},'materials':{'ScrapAlloy':40},'collectedMaterials':{'ScrapAlloy':90},'consumables':"+consumables+",'craftingXp':{'Edges':45},'questSteps':{},'questCounts':{},"+
            "'parts':"+parts+",'scrip':100,'playtime':9000.5}").Replace('\'','"');
        /// <summary>A chapter 2 save written before any of this: more gels than the pouch now holds, parts with no affix list at all, and none of the new fields.</summary>
        [Test] public void AChapterTwoSaveLoadsWithItsPouchPaidDownAndItsPartsWhole()
        {
            string dir=Path.Combine(Path.GetTempPath(),"lattice-progression-"+Guid.NewGuid());
            try
            {
                var saves=new SaveSystem(dir);Directory.CreateDirectory(dir);
                File.WriteAllText(saves.PathForSlot("slot1"),OldSave("{'RepairGel':14,'ChargeCell':7}",
                    "[{'instanceId':'a','definitionId':'EdgesT2','upgrade':3,'affixes':null},{'instanceId':'b','definitionId':'FramesT1','upgrade':0},{'instanceId':'c','definitionId':'EmittersT2','upgrade':5,'affixes':['Resonant']}]","{'Edge':'a','Emitter':'c'}"));
                var loaded=saves.Load("slot1");Assert.IsNotNull(loaded,"a chapter 2 save reads as damaged");
                var gel=Find<ConsumableDef>("RepairGel");var cell=Find<ConsumableDef>("ChargeCell");
                Assert.AreEqual(Inventory.Cap(gel),loaded.consumables["RepairGel"],"an old save keeps more gels than a party carries: the cap doesn't hold until they're drunk");
                Assert.AreEqual(Inventory.Cap(cell),loaded.consumables["ChargeCell"]);
                Assert.AreEqual(100+(14-Inventory.Cap(gel))*gel.value+(7-Inventory.Cap(cell))*cell.value,loaded.scrip,"what was over the cap was taken, not paid for");
                Assert.IsTrue(loaded.parts.All(p=>p.affixes!=null),"a part saved without an affix list loads with none: every sum over it throws");
                Assert.AreEqual(3,loaded.parts.Count);Assert.AreEqual(3,loaded.parts[0].upgrade,"an upgrade paid for under the old price was lost");Assert.AreEqual("Resonant",loaded.parts[2].affixes[0]);
                Assert.IsNull(loaded.draught);Assert.IsNull(loaded.meal);Assert.AreEqual(0,loaded.draughtLeft);
                // It plays: the sums the fight and the Gear page read, and what the new systems make of its parts.
                var taren=loaded.party[0];var edge=Find<TechPartDef>("EdgesT2");var level=Levels.Calculate(Find<CharacterDef>("Taren"),10);
                Assert.AreEqual(level.output+edge.baseStats.output*(1+3*.08f),Sum(loaded,taren,GearSlot.Edge).output,.01f,"the old save's Refined Edge +3 isn't worth what it was");
                Assert.AreEqual(level.resonance+4,Sum(loaded,taren,GearSlot.Edge).resonance,.01f,"the old save's Resonant Emitter isn't worth its +4 to the swing");
                Assert.IsFalse(Effects(loaded,taren).Any,"parts from before affixes had effects came back with some");
                Assert.AreEqual(4+(6+12+18)/2,Count(Inventory.SalvageYield(edge,loaded.parts[0]),"ScrapAlloy"),"an old +3 part salvages for something other than its yield and half its upgrades at today's price");
                // Paid once: what's saved from here loads the same.
                saves.Save("slot2",loaded);var again=saves.Load("slot2");
                Assert.AreEqual(loaded.scrip,again.scrip,"the pouch is paid down again on every load");Assert.AreEqual(Inventory.Cap(gel),again.consumables["RepairGel"]);
                // A pouch inside the cap is left exactly as it was.
                File.WriteAllText(saves.PathForSlot("slot3"),OldSave("{'RepairGel':2}","[]","{}"));var small=saves.Load("slot3");
                Assert.AreEqual(2,small.consumables["RepairGel"]);Assert.AreEqual(100,small.scrip,"a save inside the cap was paid for nothing");
            }
            finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }

        // ------------------------------------------------------------------ item 6: what the bench lists, and what stays out of the world
        [Test] public void TheBenchListsARegionsRecipesOnceItsMaterialHasBeenHeldAndOpensATierWithItsLadder()
        {
            var (state,_,bag)=Fresh();var bench=new Fabrication(state);
            foreach(string id in new[]{"EdgesT1","EdgesT2","RepairGel","FramesHusk","ModulesPearl"})Assert.IsTrue(bench.Known(Find<RecipeDef>(id)),id+" is hidden from a new game's bench: the first two chapters' recipes were always listed");
            var prime=Find<RecipeDef>("EdgesT3");
            Assert.IsFalse(bench.Known(prime),"a new game's bench lists a recipe made from a plant nobody has seen");
            int hidden=GameCatalog.All<RecipeDef>().Count(r=>!bench.Known(r));Assert.GreaterOrEqual(hidden,20,"fixture: the later regions' recipes");
            bag.Give("Rootfibre",1);Assert.IsTrue(bench.Known(prime),"the Prime Edge stays off the list with Rootfibre in the bag");
            state.materials["Rootfibre"]=0;Assert.IsTrue(bench.Known(prime),"a recipe leaves the list when its material is spent");
            // Tier by ladder, to the fifth: the page's own copy of this gate stopped at three.
            Assert.AreEqual(5,Fabrication.TopTier);
            foreach(var input in prime.inputs)state.materials[input.id]=input.count;
            Assert.AreEqual(Fabrication.Block.Rank,bench.Blocked(prime,true),"a Prime part is made on a ladder that hasn't reached 3");
            state.craftingXp["Edges"]=60;Assert.AreEqual(3,bench.Level(Discipline.Edges),"fixture: the ladder");
            Assert.AreEqual(Fabrication.Block.None,bench.Blocked(prime,true));Assert.IsTrue(bench.Craft(prime,true,1,out var made));Assert.AreEqual("EdgesT3",made.definitionId);
            Assert.AreEqual(60+30,bench.Xp(Discipline.Edges),"a Prime part doesn't train its ladder by its tier");
        }
        [Test] public void NothingOfTheLastTwoTiersCanBeReachedUntilItsChapterPlacesIt()
        {
            var late=GameCatalog.All<TechPartDef>().Where(p=>p.tier>=4).ToDictionary(p=>p.id,p=>p);
            Assert.GreaterOrEqual(late.Count,22,"fixture: the Tempered and Choirwrought parts are written");
            // Chapters 6 to 8 place Tempered and 9 to 10 Choirwrought (ProgressionBuilder: "nothing reaches them until their chapters place them"). This used to hold no late part
            // anywhere at all, which stopped being the plan when chapter 6's stall and quests handed out Tempered parts. A late part's chapter is its region's, the one whose common
            // it is upgraded with (ProgressionBuilder.RegionOf): Tempered is Hoarfell's, 6, Choirwrought the Separation's, 9, and a module with a passive its own region's (the
            // Fortune Compass is Vaskar's, 8). A stall's chapter is its zone's, a quest's its own; a recipe's is the latest of its materials' (the bench lists it only once they have
            // been held); a loot table's is the latest of the region materials it drops (every enemy's table drops its region's). Anything without one is chapter 0, before them all.
            int Of(string material)=>string.IsNullOrEmpty(material)?0:GameCatalog.Find<MaterialDef>(material) is MaterialDef m?m.chapter:0;
            int PartChapter(TechPartDef p){int c=Of(p.upgradeMaterial);Assert.Greater(c,0,$"{p.id} is upgraded with '{p.upgradeMaterial}', which is no region's material: its chapter can't be told");return c;}
            Assert.AreEqual(6,PartChapter(late["EdgesT4"]),"fixture: Tempered is chapter 6's, Hoarfell's");Assert.AreEqual(9,PartChapter(late["EdgesT5"]),"fixture: Choirwrought is chapter 9's, the Separation's");
            // "Any part of a tier" can come up as any of the tier's parts that drop: its chapter is the latest of theirs, and a tier with none that drop is never reached.
            int TierChapter(int tier)=>late.Values.Where(p=>p.tier==tier&&p.drops).Select(PartChapter).DefaultIfEmpty(int.MaxValue).Max();
            int ZoneChapter(string stall){var zone=GameCatalog.Find<ZoneDef>(stall);int cut=stall.LastIndexOf('_');if(zone==null&&cut>0)zone=GameCatalog.Find<ZoneDef>(stall.Substring(0,cut));return zone!=null?zone.chapter:0;}
            var early=new List<string>();
            void Check(string where,int chapter,string id,string what){if(late.TryGetValue(id??"",out var part)&&chapter<PartChapter(part))early.Add($"{where} {what} {id} in chapter {chapter}, before its chapter {PartChapter(part)}");}
            foreach(var recipe in GameCatalog.All<RecipeDef>())if(recipe.output!=null)Check(recipe.id,(recipe.inputs??new Ingredient[0]).Select(i=>Of(i.id)).DefaultIfEmpty(0).Max(),recipe.output.id,"makes");
            // An offer that waits on a flag is on the counter from that flag's chapter, if it is later than the stall's: Corra's convoy stock at Many Hands (a chapter 5 zone) waits
            // on chapter9.complete (D326). A chapter's close is that chapter; a quest's completion, its quest's.
            int Gate(string flag)
            {
                if(string.IsNullOrEmpty(flag))return 0;
                var close=System.Text.RegularExpressions.Regex.Match(flag,@"^chapter(\d+)\.complete$");if(close.Success)return int.Parse(close.Groups[1].Value);
                var done=System.Text.RegularExpressions.Regex.Match(flag,@"^quest\.(\w+)\.complete$");if(done.Success&&GameCatalog.Find<QuestDef>(done.Groups[1].Value) is QuestDef q)return q.chapter;
                return 0;
            }
            foreach(var stall in GameCatalog.All<ShopInventory>())foreach(var offer in stall.offers??new ShopOffer[0])Check(stall.name,Mathf.Max(ZoneChapter(stall.name),Gate(offer.requires)),offer.id,"sells");
            foreach(var table in GameCatalog.All<LootTable>())
            {
                var entries=table.entries??new LootEntry[0];int chapter=entries.Select(e=>Of(e.id)).DefaultIfEmpty(0).Max();
                foreach(var entry in entries)
                {
                    Check(table.name,chapter,entry.id,"drops");
                    if(entry.partTier>=4&&chapter<TierChapter(entry.partTier))early.Add($"{table.name} drops any part of tier {entry.partTier} in chapter {chapter}, before its chapter");
                }
            }
            foreach(var quest in GameCatalog.All<QuestDef>())foreach(var reward in quest.rewards??new Ingredient[0])Check(quest.id,quest.chapter,reward.id,"hands over");
            Assert.IsEmpty(early,"a part of the last two tiers is reached before its chapter places it:\n"+string.Join("\n",early));
            // "Any part of a tier" gives only what's flagged to drop, and nothing at all from a tier with nothing flagged.
            var rng=new System.Random(5);
            for(int i=0;i<200;i++)
            {
                var any=Loot.PickPart(3,GearSlot.Edge,true,rng);Assert.IsTrue(any.drops&&any.tier==3,$"'any Prime part' gave {any.id}");
                Assert.AreEqual(GearSlot.Drive,Loot.PickPart(3,GearSlot.Drive,false,rng).slot,"'any Prime drive' gave another slot");
            }
            Assert.IsNull(Loot.PickPart(9,GearSlot.Edge,true,rng),"a tier nothing is written for gives a part");
            foreach(var part in GameCatalog.All<TechPartDef>().Where(p=>p.mods.Any||p.id.EndsWith("Husk")||p.id.EndsWith("Choir")||p.id.EndsWith("Pearl")||p.id=="EdgesHeartwood"||p.id=="DrivesLure"))
                Assert.IsFalse(part.drops,part.id+" can be found at random: a trophy part or a module with a passive is made or bought");
        }

        // ------------------------------------------------------------------ items 6 and 9: the data holds together
        [Test] public void EveryStallPricesWhatItSellsAboveWhatAShopPaysForIt()
        {
            var stalls=GameCatalog.All<ShopInventory>();
            foreach(string id in new[]{"Hub_Decks","Lanternwick","Rootmarket_Binna","Rootmarket_Hadda"})Assert.IsTrue(stalls.Any(s=>s.name==id),"no stall named "+id+": a stall with no inventory sells nothing");
            foreach(var stall in stalls)
            {
                Assert.IsNotNull(stall.offers,stall.name+" has no offers");Assert.Greater(stall.offers.Length,0,stall.name+" has nothing on the counter");
                Assert.AreEqual(stall.offers.Length,stall.offers.Select(o=>o.id).Distinct().Count(),stall.name+" offers the same thing twice");
                foreach(var offer in stall.offers)
                {
                    var part=GameCatalog.Find<TechPartDef>(offer.id);var item=GameCatalog.Find<ConsumableDef>(offer.id);var material=GameCatalog.Find<MaterialDef>(offer.id);
                    Assert.AreEqual(1,(part!=null?1:0)+(item!=null?1:0)+(material!=null?1:0),$"{stall.name}: '{offer.id}' isn't one part, consumable or material");
                    Assert.Greater(offer.price,0,$"{stall.name} gives {offer.id} away");
                    // Nothing can be bought and sold round in a circle: a shop's part sells for 20 a tier, a consumable over the cap for its value, a material for its own.
                    if(part!=null)Assert.Greater(offer.price,20+(part.tier-1)*20+Gear.MaxUpgrade*5,$"{stall.name}: {offer.id} sells back for more than it costs");
                    if(item!=null)Assert.Greater(offer.price,item.value,$"{stall.name}: {offer.id} costs {offer.price} and a spare one pays {item.value}");
                    if(material!=null){int lot=offer.lot>0?offer.lot:3;Assert.Greater(offer.price,lot*material.value,$"{stall.name}: {lot} {offer.id} cost {offer.price} and sell for {lot*material.value}");}
                }
            }
            // Mira's and Corra's prices are what they were when a list of numbers stood for them.
            var mira=stalls.First(s=>s.name=="Hub_Decks");var corra=stalls.First(s=>s.name=="Lanternwick");
            int Price(ShopInventory s,string id)=>s.offers.First(o=>o.id==id).price;
            Assert.AreEqual(60,Price(mira,"EdgesT1"));Assert.AreEqual(20,Price(mira,"RepairGel"));Assert.AreEqual(12,Price(mira,"ScrapAlloy"));
            Assert.AreEqual(240,Price(corra,"EdgesT2"));Assert.AreEqual(240,Price(corra,"EmittersT2"));Assert.AreEqual(260,Price(corra,"FramesT2"));Assert.AreEqual(220,Price(corra,"DrivesT2"));Assert.AreEqual(280,Price(corra,"ModulesT2"));
            Assert.AreEqual(22,Price(corra,"RepairGel"));Assert.AreEqual(26,Price(corra,"ChargeCell"));Assert.AreEqual(30,Price(corra,"RidgeCrystal"));
            Assert.IsFalse(mira.offers.Any(o=>GameCatalog.Find<ConsumableDef>(o.id) is ConsumableDef c&&c.id!="RepairGel"&&c.id!="ChargeCell"),"Mira sells a consumable from a later chapter: her stall used to list the whole catalogue at 20");
            // A cook's dishes wait for her baskets.
            var binna=stalls.First(s=>s.name=="Rootmarket_Binna");var state=new GameState();
            int before=ShopUi.Stock(binna,state).Count();state.flags["quest.ThreeBaskets.complete"]=true;
            Assert.AreEqual(before+2,ShopUi.Stock(binna,state).Count(),"Binna's two later dishes aren't waiting on her baskets");
            Assert.IsTrue(binna.offers.Where(o=>GameCatalog.Find<ConsumableDef>(o.id)?.meal==true).Count()>=3,"Binna doesn't sell three cooked dishes");
            // Rootmarket has no stall of its own, only its sellers': anyone else's talk there opens a bare counter, not the outfitter's stock and not the catalogue.
            var nobody=ShopUi.For("Rootmarket","Nobody");Assert.IsNotNull(nobody);Assert.IsEmpty(ShopUi.Stock(nobody,state).ToArray(),"a seller with no stall sells something");
            Assert.AreSame(binna,ShopUi.For("Rootmarket","Binna"),"Binna's talk doesn't open Binna's stall");Assert.AreSame(corra,ShopUi.For("Lanternwick","Corra"),"a zone with one stall doesn't open it for its seller");
            Assert.AreSame(mira,ShopUi.Outfitter,"a shop opened with no inventory named isn't the outfitter's own stock");
        }
        [Test] public void EveryPartRecipeAndMaterialNamesThingsThatExist()
        {
            var parts=GameCatalog.All<TechPartDef>();Assert.GreaterOrEqual(parts.Length,60,"the parts ProgressionBuilder writes aren't all there");
            foreach(var part in parts)
            {
                Assert.AreEqual(part.name,part.id,part.name+"'s id isn't its file's name: the catalogue finds it by one and the save names it by the other");
                Assert.IsTrue(part.affixPool!=null&&part.affixPool.Length>=3&&part.affixPool.All(a=>a!=null),part.id+" has too small a pool, or a hole in it: a re-tune has nowhere to go");
                Assert.AreEqual(part.affixPool.Length,part.affixPool.Select(a=>a.id).Distinct().Count(),part.id+"'s pool names an affix twice");
                if(part.tier<3)continue;
                Assert.IsNotNull(GameCatalog.Find<MaterialDef>(part.upgradeMaterial),$"{part.id} is upgraded with '{part.upgradeMaterial}', which isn't a material");
                var rare=GameCatalog.Find<MaterialDef>(part.upgradeRare);Assert.IsTrue(rare!=null&&rare.rare,$"{part.id}'s last upgrade takes '{part.upgradeRare}', which isn't a rare material");
                foreach(var piece in part.salvage??new Ingredient[0])Assert.IsNotNull(GameCatalog.Find<MaterialDef>(piece.id),$"{part.id} salvages into '{piece.id}', which isn't a material");
            }
            // A slot and a tier each have a plain part, and from Prime a choice.
            for(int tier=3;tier<=5;tier++)foreach(GearSlot slot in Enum.GetValues(typeof(GearSlot)))
                Assert.GreaterOrEqual(parts.Count(p=>p.tier==tier&&p.slot==slot&&p.drops),slot==GearSlot.Module?1:2,$"tier {tier} has too few {slot} parts that drop");
            foreach(DamageType type in new[]{DamageType.Kinetic,DamageType.Plasma,DamageType.Pulse})Assert.IsTrue(parts.Any(p=>p.tier==3&&p.slot==GearSlot.Edge&&p.damageType==type),"no Prime Edge of type "+type);
            foreach(DamageType type in new[]{DamageType.Beam,DamageType.Plasma,DamageType.Pulse})Assert.IsTrue(parts.Any(p=>p.tier==3&&p.slot==GearSlot.Emitter&&p.damageType==type),"no Prime Emitter of type "+type);
            // Each tier's plain weapon outdoes the one before, and stands at a third to two thirds of the hero's own Output at the Sync it's found.
            var taren=Find<CharacterDef>("Taren");
            foreach(var (tier,sync) in new[]{(3,12),(4,18),(5,27)})
            {
                float edge=Find<TechPartDef>("EdgesT"+tier).baseStats.output,below=Find<TechPartDef>("EdgesT"+(tier-1)).baseStats.output,own=Levels.Calculate(taren,sync).output;
                Assert.Greater(edge,below*1.25f,$"a tier {tier} Edge is barely better than a tier {tier-1} one");
                Assert.Greater(edge/own,.33f,$"at Sync {sync} a tier {tier} Edge is {edge/own:P0} of the hero's own Output: gear stops mattering");Assert.Less(edge/own,.66f,$"at Sync {sync} a tier {tier} Edge is {edge/own:P0} of the hero's own Output: levels stop mattering");
            }
            foreach(var recipe in GameCatalog.All<RecipeDef>())
            {
                Assert.AreEqual(recipe.name,recipe.id);Assert.IsTrue(recipe.inputs!=null&&recipe.inputs.Length>0,recipe.id+" is made from nothing");
                foreach(var input in recipe.inputs){Assert.IsNotNull(GameCatalog.Find<MaterialDef>(input.id),$"{recipe.id} takes '{input.id}', which isn't a material");Assert.Greater(input.count,0);}
                if(recipe.output==null){var made=GameCatalog.Find<ConsumableDef>(recipe.consumableOutput);Assert.IsNotNull(made,recipe.id+" makes no part and no consumable");Assert.IsFalse(made.meal,recipe.id+": a meal is cooked and bought, never fabricated");}
                else Assert.LessOrEqual(recipe.tier,Fabrication.TopTier,recipe.id+" asks for a ladder past its top");
            }
            // Every region of the ten chapters has its two commons and its rare (the last has one common), and a shop will take each.
            var materials=GameCatalog.All<MaterialDef>();
            foreach(int chapter in new[]{3,4,6,7,8,9,10})
            {
                var region=materials.Where(m=>m.chapter==chapter).ToArray();
                Assert.AreEqual(chapter==10?1:2,region.Count(m=>!m.rare),$"chapter {chapter}'s region has the wrong number of common materials");Assert.AreEqual(1,region.Count(m=>m.rare),$"chapter {chapter}'s region has no rare, or more than one");
            }
            // A crafting material (one with a chapter) always has a buyer. Something a quest gathers and nothing is made from may have no value, and then no shop takes it.
            foreach(var material in materials.Where(m=>m.chapter>0)){Assert.Greater(material.value,0,material.id+" can't be sold: it has no use but its recipes, and a spare one has none");Assert.AreEqual(material.name,material.id);}
        }
    }
    static class ProgressionTestsParts
    {
        /// <summary>The instance of this part a state holds, for a check on where it was put.</summary>
        public static string instanceId(this TechPartDef def,GameState state)=>state.parts.FindLast(p=>p.definitionId==def.id)?.instanceId;
    }
}
