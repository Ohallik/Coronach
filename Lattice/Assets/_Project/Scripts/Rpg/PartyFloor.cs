using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Rpg
{
    /// <summary>The floor a chapter is sized on, and the rule that brings a hero up to it (D329). Every boss and every hit from chapter 3 on is measured on a party in the plain part
    /// of its chapter's tier in every slot (DevLoadout dresses its fixtures from <see cref="Parts"/>): Refined into chapter 3, Prime into 4 to 6, Tempered into 7 to 9, Choirwrought
    /// into 10, the hero's other weapon a tier behind.
    /// <para>A hero who joins the party, or comes back into it after chapters away, is brought up to the floor of the chapter she (re)joins in: every slot of hers that is empty, or
    /// holds a part the Gear page's numbers show worse than the floor's plain part, gets that part; what she wore goes to the bag, and a better part she wears is left alone. Once
    /// for each (re)joining, by <see cref="Flag"/>. Idra joining in chapter 6 is its first case (she came with an Edge, a Frame and a Drive and nothing in her Emitter's or Module's
    /// slot, and no stall stands between her joining and the Host Marshal); Sela's turn back in chapter 10 (D305) is its second (she came out in what she had rested in since
    /// chapter 6, a Tempered Emitter under a Choirwrought floor).</para></summary>
    public static class PartyFloor
    {
        /// <summary>For a case's broken state only (JoinKitTests), off otherwise: the game as it was, with no floor brought to anyone.</summary>
        static bool off;
        /// <summary>The floor's tier on arriving at a chapter.</summary>
        public static int Tier(int chapter)=>chapter>=10?5:chapter>=7?4:chapter>=4?3:2;
        /// <summary>The floor's plain part for each slot, for a hero arriving at a chapter: chapter 2 the Refined weapon alone, as the older loadouts give; from chapter 3 the tier's
        /// part in every slot and the other weapon a tier behind. Nothing before chapter 2.</summary>
        public static IEnumerable<(GearSlot slot,string part)> Parts(int chapter,string hero)
        {
            if(chapter<2)yield break;
            bool edge=HeroWeapon.Of(hero)==GearSlot.Edge;
            if(chapter<=2){yield return edge?(GearSlot.Edge,"EdgesT2"):(GearSlot.Emitter,"EmittersT2");yield break;}
            int tier=Tier(chapter);
            yield return edge?(GearSlot.Edge,"EdgesT"+tier):(GearSlot.Emitter,"EmittersT"+tier);
            yield return edge?(GearSlot.Emitter,"EmittersT"+(tier-1)):(GearSlot.Edge,"EdgesT"+(tier-1));
            yield return (GearSlot.Frame,"FramesT"+tier);yield return (GearSlot.Drive,"DrivesT"+tier);yield return (GearSlot.Module,"ModulesT"+tier);
        }
        /// <summary>What a part is worth on the Gear page: the five numbers its row and its comparison show (Gear.PartTotal), added.</summary>
        public static float Worth(TechPartDef def,PartInstance item)
        {
            if(def==null||item==null)return 0;
            var s=Gear.PartTotal(def,item,GameCatalog.Find<AffixDef>);return s.output+s.plating+s.response+s.resonance+s.fortune;
        }
        /// <summary>Set once a hero has been brought up to a chapter's floor.</summary>
        public static string Flag(string hero,int chapter)=>"floor."+hero+"."+chapter;
        /// <summary>Brings a hero up to the floor of the chapter she (re)joins in, once. Returns what she was given, by part id.</summary>
        public static List<string> BringUp(GameState state,MemberState member,int chapter)
        {
            var given=new List<string>();
            if(off||state==null||member==null||chapter<2)return given;
            string flag=Flag(member.id,chapter);if(state.flags.TryGetValue(flag,out bool done)&&done)return given;
            var inventory=new Inventory(state);
            foreach(var (slot,id) in Parts(chapter,member.id))
            {
                var def=GameCatalog.Find<TechPartDef>(id);if(def==null)continue;
                var worn=member.equipped.TryGetValue(slot,out string on)?state.parts.Find(p=>p.instanceId==on):null;
                var wornDef=worn!=null?GameCatalog.Find<TechPartDef>(worn.definitionId):null;
                if(worn!=null&&wornDef!=null&&Worth(wornDef,worn)+.5f>=Worth(def,new PartInstance{definitionId=id}))continue;
                // The plain part goes on; what was on stays in the bag, spare.
                if(inventory.Equip(member,inventory.GivePart(def),def))given.Add(id);
            }
            state.flags[flag]=true;
            if(given.Count>0)Debug.Log("PARTY_FLOOR "+member.id+" chapter "+chapter+": "+string.Join(", ",given));
            return given;
        }
        /// <summary>The story's own (re)joinings, each with the flag that marks it and the chapter it happens in, for a save made after one and before this rule: such a save is
        /// brought up on load as the moment itself now brings her up (the moments call <see cref="BringUp"/> with their zone's chapter). Idra joins in the dig; Sela turns back.</summary>
        public static readonly (string hero,string flag,int chapter)[] Rejoinings={("Idra","hoar.idraJoined",6),("Sela","end.turnedBack",10)};
        /// <summary>On a save's load (the title's picker, a Retry): every (re)joining the save is past, for a hero it holds, brought up once.</summary>
        public static int OnLoad(GameState state)
        {
            if(state==null||state.party==null||state.flags==null)return 0;
            int given=0;
            foreach(var (hero,flag,chapter) in Rejoinings)
            {
                if(!(state.flags.TryGetValue(flag,out bool past)&&past))continue;
                var member=state.party.Find(m=>m!=null&&m.id==hero);if(member!=null)given+=BringUp(state,member,chapter).Count;
            }
            return given;
        }
        /// <summary>The chapter of the zone the party stands in now (0 where it can't be told).</summary>
        public static int Here()=>ZoneController.Current!=null&&ZoneController.Current.definition!=null?ZoneController.Current.definition.chapter:0;
    }
}
