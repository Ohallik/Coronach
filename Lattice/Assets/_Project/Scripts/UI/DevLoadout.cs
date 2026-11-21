using System;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.UI
{
    public static class DevLoadout
    {
        /// <summary>The first chapter a loadout arrives at, and the last.</summary>
        public const int FirstChapter=2,LastChapter=10;
        /// <summary>Sync on arriving at chapters 2 to 10: where the plan ends the chapter before (SHIPPING_PLAN section 2).</summary>
        static readonly int[] ArrivalSync={7,10,13,15,16,19,22,25,27};
        public static int SyncOnArrival(int chapter)=>ArrivalSync[Mathf.Clamp(chapter,FirstChapter,LastChapter)-FirstChapter];
        /// <summary>The first two chapters' story quests and what their stories leave set. They predate QuestDef.chapter and QuestDef.storyFlags, which say this for every
        /// chapter since: nothing is added here.</summary>
        static readonly (int chapter,string[] quests,string[] flags)[] FirstChapters=
        {
            (1,new[]{"Sorrel","Hushwell"},new[]{"opening.marketRequested","opening.walkwayRepaired","met.Orrin","met.Survivor","bossdown.Burrower","clear.Sorrel_Burrower","warpkey",
                "bossdown.BellowsBelow","clear.Hushwell_0","clear.Hushwell_1","clear.Hushwell_2","clear.Hushwell_3","clear.Hushwell_Bellows","hushwell.nursery",
                "clear.Gullet_Chamber_0","clear.Gullet_Chamber_1","clear.Gullet_Chamber_2","bossdown.Cantor","clear.Gullet_Cantor","met.Keeper","sliceComplete","tallow.lineObserved","tallow.driftDiscussed"}),
            (2,new[]{"LanternRoad"},new[]{"compact.hailHeard","lantern.routeKnown","clear.Wick_0","clear.Wick_1","clear.Wick_2","clear.Wick_3","wick.relay1","wick.relay2","wick.gateOpen","met.Aumi",
                "clear.Gorge_0","clear.Gorge_1","clear.Gorge_3","clear.Gorge_4","gorge.relay1","gorge.relay2","gorge.relay3","gorge.relaysLit","relay.entered","clear.Relay_0","clear.Relay_1","clear.Relay_2",
                "relay.hessaConfronted","bossdown.HessaKorl","clear.Relay_Hessa","relay.hessaYielded","lanternwick.chartGiven","chapter2.homecoming","pell.aboard"}),
        };
        /// <summary>HOOK FOR S2's TIERS. The parts a hero wears on arriving at a chapter, by id, each equipped in its own slot. Today every chapter arrives in the best there
        /// is: the tier 2 weapon the older loadouts give. When tiers 3 and up exist, return them here by chapter (a weapon, and whatever else a player would have by then);
        /// an id with no definition is skipped, so a chapter can be listed before its parts land.</summary>
        public static Func<int,string,string[]> ArrivalGear=(chapter,hero)=>Worn(chapter,hero);
        /// <summary>S2's tiers, by the chapter before: Refined on leaving chapter 2, Prime from chapter 3's stalls (worn into 4, 5 and 6), Tempered from chapter 6's (into 7, 8 and 9),
        /// Choirwrought from chapter 9's (into 10). Chapter 2 arrives as the older loadouts do, with the Refined weapon alone. From chapter 3 on it's the plain part of that tier in every slot
        /// (what a stall sells, with no affix and no passive, so a chapter's numbers are tuned against the floor a player can buy), and the hero's other weapon a tier behind.</summary>
        // The game's own floor (PartyFloor.Parts), which also brings a hero who (re)joins up to it: the fixtures and the story dress a hero from one rule.
        static string[] Worn(int chapter,string hero)=>PartyFloor.Parts(chapter,hero).Select(p=>p.part).ToArray();
        /// <summary>The weapon a hero fights with on foot, which their arrival gear leads with: read off the definition's own starting gear (Taren's is an Edge, Sela's an
        /// Emitter, Idra's the Edge her hammer strikes with), so a new hero is right the day their definition is. NAMED FALLBACK, for a hero with no definition or one that
        /// starts with no weapon: what the loadouts gave before anyone could be asked, an Edge for Taren and an Emitter for anybody else.</summary>
        public static GearSlot Weapon(string hero)=>HeroWeapon.Of(hero);
        static bool Chapter(string name,out int chapter)
        {
            chapter=0;
            return name.StartsWith("chapter",StringComparison.Ordinal)&&int.TryParse(name.Substring(7),out chapter)&&chapter>=FirstChapter&&chapter<=LastChapter;
        }
        /// <summary>A party as it arrives at a chapter: the Sync the plan gives it, every skill that Sync has taught (they come with the level), the gear of
        /// <see cref="ArrivalGear"/>, and every earlier chapter told: its story quests complete with what they set, its <c>chapter&lt;k&gt;.complete</c> flag, and the flags its story leaves
        /// (QuestDef.storyFlags on its main quests). This chapter's own quest is not begun. So a chapter's tests and replay routes can start in the middle of the game.</summary>
        static void Arrive(int chapter,bool joined=false)
        {
            var state=GameServices.Current.State;int level=SyncOnArrival(chapter);
            if(!state.party.Exists(m=>m.id=="Sela"))state.party.Add(new MemberState{id="Sela"});
            // Idra joins during chapter 6, so chapter 7 is the first arrival with her (and JoinedLoadout is chapter 6 once she has). Only once she has a definition: a hero
            // with none can't be given gear, a face or a row on the Party page, so until then the loadout arrives as the two it can make, and says who is missing.
            if((chapter>=ThirdHeroFrom||joined)&&!state.party.Exists(m=>m.id==ThirdHero))
            {
                if(GameCatalog.Find<CharacterDef>(ThirdHero)!=null)state.party.Add(new MemberState{id=ThirdHero});
                else Debug.Log(NoHeroMarker+" "+ThirdHero+": chapter"+chapter+" arrives as two until her CharacterDef exists (IdraBuilder.Build)");
            }
            foreach(var member in state.party){member.level=level;member.xp=0;member.integrity=Levels.MaxIntegrity(level);member.charge=100;}
            // She arrives with the four a join gives her (PartyField.FirstFour), so a fixture's Idra has the slots a player's has.
            var third=state.party.Find(m=>m.id==ThirdHero);if(third!=null)Lattice.Combat.PartyField.FirstFour(third);
            state.consumables["RepairGel"]=8;state.consumables["ChargeCell"]=4;
            foreach(string id in new[]{"ScrapAlloy","LatticeFilament","RidgeCrystal"})state.materials[id]=20;
            var inv=new Inventory(state);
            foreach(var member in state.party)foreach(string id in ArrivalGear(chapter,member.id)??Array.Empty<string>())
            {
                var part=GameCatalog.Find<TechPartDef>(id);if(part!=null)inv.Equip(member,inv.GivePart(part),part);
            }
            // Three arrive, two are out (ArrivalPair). A party of two is asked nothing.
            if(Deployment.Choosing(state)&&!Deployment.Set(state,ArrivalPair))Debug.Log(NoHeroMarker+" the arrival pair isn't two heroes of this party: the first two are out");
            void Told(QuestDef quest)
            {
                if(quest==null||quest.steps==null)return;
                state.questSteps[quest.id]=quest.steps.Length;state.flags["quest."+quest.id+".complete"]=true;
                foreach(string flag in quest.flagsOnComplete??Array.Empty<string>())state.flags[flag]=true;
                foreach(string flag in quest.storyFlags??Array.Empty<string>())state.flags[flag]=true;
                // A chapter told has had its closing conversation: the flag its card waits on is up, so the card isn't still owed to a party that starts past it.
                if(!string.IsNullOrWhiteSpace(quest.cardFlag))state.flags[quest.cardFlag]=true;
                // What each step's doing leaves behind, as far as the step says: the boss down, the person met, the flag itself.
                foreach(var step in quest.steps)
                {
                    if(step.kind==ObjectiveKind.Flag)state.flags[step.target]=true;
                    else if(step.kind==ObjectiveKind.TalkTo)state.flags["met."+step.target]=true;
                    else if(step.kind==ObjectiveKind.Kill){var enemy=GameCatalog.Find<EnemyDef>(step.target);if(enemy!=null&&enemy.boss)state.flags["bossdown."+step.target]=true;}
                }
            }
            for(int told=1;told<chapter;told++)
            {
                state.flags["chapter"+told+".complete"]=true;
                foreach(var first in FirstChapters)if(first.chapter==told){foreach(string id in first.quests)Told(GameCatalog.Find<QuestDef>(id));foreach(string flag in first.flags)state.flags[flag]=true;}
                foreach(var quest in GameCatalog.All<QuestDef>())if(quest!=null&&quest.main&&quest.chapter==told&&Array.IndexOf(StoryQuests.First,quest.id)<0)Told(quest);
            }
            // A party that has finished a chapter has stood in its settlements, so Sela's chart holds them (ChartStop; the first two chapters' are charted by flags set just above).
            foreach(var stop in GameCatalog.All<ChartStop>())if(stop!=null&&stop.chapter>0&&stop.chapter<chapter)state.flags[stop.UnlockFlag]=true;
            // The one thing the join itself leaves that the chapter's brief names (docs/chapters/ch06): what the chapter's own story set before it is the chapter's to add.
            if(joined&&state.party.Exists(m=>m.id==ThirdHero))state.flags[JoinedFlag]=true;
            Debug.Log("DEV_LOADOUT_OK "+(joined?JoinedLoadout:"chapter"+chapter)+" sync="+level);
        }
        public static void Apply(string name)
        {
            if(string.IsNullOrEmpty(name))return;
            if(name==JoinedLoadout){Arrive(ThirdHeroJoins,true);return;}
            if(Chapter(name,out int chapter)){Arrive(chapter);return;}
            if(name!="starter"&&name!="moon"&&name!="hushwell"&&name!="gullet")throw new ArgumentException("Unknown loadout. Use starter, moon, hushwell, gullet, chapter2 to chapter10, or "+JoinedLoadout+".");
            var state=GameServices.Current.State;int level=name=="gullet"||name=="hushwell"?5:name=="moon"?3:1;
            if(!state.party.Exists(m=>m.id=="Sela"))state.party.Add(new MemberState{id="Sela"});
            foreach(var member in state.party){member.level=level;member.xp=0;member.integrity=Levels.MaxIntegrity(level);member.charge=100;}
            state.consumables["RepairGel"]=8;state.consumables["ChargeCell"]=4;
            foreach(string id in new[]{"ScrapAlloy","LatticeFilament","RidgeCrystal"})state.materials[id]=20;
            if(name!="starter")
            {
                var inv=new Inventory(state);
                foreach(var member in state.party)
                {
                    var part=GameCatalog.Find<TechPartDef>(Weapon(member.id)==GearSlot.Edge?"EdgesT2":"EmittersT2");
                    if(part!=null)inv.Equip(member,inv.GivePart(part),part);
                }
            }
            // These fixtures represent distinct points in the chapter. A Hushwell
            // run must still earn the discovery that a Gullet party already owns.
            if(name=="gullet"||name=="hushwell")
            {
                foreach(string flag in new[]{"met.Survivor","bossdown.Burrower","clear.Sorrel_Burrower","warpkey","quest.Sorrel.complete"})state.flags[flag]=true;
                state.questSteps["Sorrel"]=4;
            }
            if(name=="gullet")
            {
                foreach(string flag in new[]{"bossdown.BellowsBelow","clear.Hushwell_0","clear.Hushwell_1","clear.Hushwell_2","clear.Hushwell_3","clear.Hushwell_Bellows","hushwell.nursery","quest.Hushwell.complete"})state.flags[flag]=true;
                state.questSteps["Hushwell"]=2;
            }
            Debug.Log("DEV_LOADOUT_OK "+name);
        }
        /// <summary>The third hero, the first chapter a loadout arrives at with her (she joins during chapter 6), and the pair out on arriving: the lead and her, which is
        /// how the game itself leaves a party she has just joined (PartyField.Join) if nobody touches the Party page. A test or a route that wants Sela out names its
        /// own pair (Deployment.Set, a route's "deployed").</summary>
        public const string ThirdHero="Idra";
        public const int ThirdHeroFrom=7;
        /// <summary>Chapter 6 as it stands once she has joined (in the dig, at the first coupling's gallery): the chapter's arrival, at its Sync, with her in the party and out
        /// beside Taren as the join leaves her, and <see cref="JoinedFlag"/> set. "chapter6" itself is the arrival, before she is met: as two, with her still a voice and a
        /// person in Lastlight (an Npc that hides once its speaker has joined), so it can't hold her.</summary>
        public const string JoinedLoadout="chapter6joined",JoinedFlag="hoar.idraJoined";
        public const int ThirdHeroJoins=6;
        public static readonly string[] ArrivalPair={"Taren",ThirdHero};
        /// <summary>What a loadout logs when it leaves the third hero out because she has no definition yet (IdraBuilder.Build writes it).</summary>
        public const string NoHeroMarker="DEV_LOADOUT_NO_HERO";
    }
}
