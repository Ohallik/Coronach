using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
namespace Lattice.UI
{
    /// <summary>What the replay driver puts on for itself (QualityRoute.wearBest), as a player who reads the Gear page would. Until November 15 it wore only what fell into
    /// an empty slot: after route 62 Taren still had a Refined Edge and Scrap parts on with Choirwrought Edges, Frames and Drives in the bag and 18,315 scrip unspent, and
    /// every boss the chains timed from chapter 3 on was timed on that party. Decisions only, made from the page's own numbers; ContinuousReview puts each part on with the
    /// page's own call (Inventory.Equip), between fights and talks.
    /// <para>Best is by the page: a part is worth the five numbers its row and its comparison show (Gear.PartTotal: base stats at its upgrade, affix stats at its tier), added,
    /// so a +5 Tempered Edge (98) stays on against a plain Choirwrought one (95), and a Frame with Balanced on it beats the same Frame plain. A part is put on only when it is
    /// worth more than what the hero has in that slot; of two worth the same, the one with more effects (a passive, affixes), then the higher tier. Effects are not otherwise
    /// counted: the page puts no number on them. The slots are the page's: the five, and module slot 2.</para>
    /// <para>Only spares: a part on another hero, out or resting, is never taken. Nothing is upgraded, re-tuned, bought or salvaged.</para></summary>
    public static class ReplayGear
    {
        /// <summary>One part to put on: on whom, in which slot (module slot 2 is <see cref="second"/>), and what it replaces (null for an empty slot).</summary>
        public sealed class Wear
        {
            public MemberState hero;public GearSlot slot;public bool second;
            public PartInstance part,was;public TechPartDef def,wasDef;
            public float gain;
            /// <summary>The slot as the log names it: Emitter, Edge, Frame, Drive, Module, or Module2.</summary>
            public string SlotName=>second?"Module2":slot.ToString();
        }
        /// <summary>The page's slots in its own order, and then module slot 2.</summary>
        static readonly (GearSlot slot,bool second)[] Slots={(GearSlot.Emitter,false),(GearSlot.Edge,false),(GearSlot.Frame,false),(GearSlot.Drive,false),(GearSlot.Module,false),(GearSlot.Module,true)};
        /// <summary>Less than this is the same on the page (it prints whole numbers on a row, and tenths in a comparison).</summary>
        const float Worthwhile=.5f;
        /// <summary>What a part is worth on the Gear page: the five numbers its row and its comparison show, added.</summary>
        public static float Worth(TechPartDef def,PartInstance item)
        {
            if(def==null||item==null)return 0;
            var s=Gear.PartTotal(def,item,GameCatalog.Find<AffixDef>);
            return s.output+s.plating+s.response+s.resonance+s.fortune;
        }
        /// <summary>How many things a part does that its numbers don't say: its passive and its affixes. Only ever a tie-break.</summary>
        static int Effects(TechPartDef def,PartInstance item)=>(PartText.Passive(def)!=""?1:0)+(item.affixes!=null?System.Math.Min(2,item.affixes.Count):0);
        static PartInstance Find(GameState state,string id)=>string.IsNullOrEmpty(id)?null:state.parts.Find(p=>p.instanceId==id);
        /// <summary>What the hero has on in this slot, if anything.</summary>
        public static PartInstance Worn(GameState state,MemberState hero,GearSlot slot,bool second)=>Find(state,second?hero.module2:hero.equipped.TryGetValue(slot,out string id)?id:null);
        /// <summary>The one change worth most, among the heroes named: a weapon for a hero who fights with it first, then the spare that is worth the most more than what one of
        /// them has in a slot; of two as good, the lead (<paramref name="lead"/>, or null). Null when nothing spare beats anything worn. The driver asks again after each, so the
        /// biggest step is always taken first and two heroes never reach for one part. <paramref name="fromAnyone"/> lets a part on another hero count as spare: a case's broken
        /// state only (WearBestTests), never the driver's.</summary>
        public static Wear Next(GameState state,IList<MemberState> heroes,MemberState lead=null,bool fromAnyone=false)
        {
            if(state==null||state.parts==null||heroes==null)return null;
            // What each hero has on in each slot, and what it is worth on the page: asked once, not once for every spare.
            var worn=new (PartInstance part,TechPartDef def,float worth)[heroes.Count,Slots.Length];
            for(int h=0;h<heroes.Count;h++)for(int s=0;s<Slots.Length;s++)
            {
                if(heroes[h]==null)continue;
                var was=Worn(state,heroes[h],Slots[s].slot,Slots[s].second);var wasDef=was!=null?GameCatalog.Find<TechPartDef>(was.definitionId):null;
                worn[h,s]=(was,wasDef,Worth(wasDef,was));
            }
            var inventory=new Inventory(state);Wear best=null;double bestRank=double.NegativeInfinity;
            foreach(var part in state.parts)
            {
                var def=GameCatalog.Find<TechPartDef>(part.definitionId);if(def==null)continue;
                var wearer=inventory.Wearer(part);
                if(wearer!=null&&!fromAnyone)continue;
                float worth=Worth(def,part);if(worth<=0)continue;
                int effects=Effects(def,part);
                for(int h=0;h<heroes.Count;h++)
                {
                    var hero=heroes[h];if(hero==null||hero==wearer)continue;
                    for(int s=0;s<Slots.Length;s++)
                    {
                        if(Slots[s].slot!=def.slot)continue;
                        var (was,wasDef,wasWorth)=worn[h,s];
                        // An equal part is no reason to change; an empty slot beside a spare is what nobody leaves.
                        float gain=worth-wasWorth;
                        if(was!=null&&gain<Worthwhile)continue;
                        // A weapon goes first to a hero who fights with it, as a found one does (Inventory.EquipIfEmpty): Sela shoots with an Emitter, and Taren takes the one she
                        // put down. Module slot 2 comes after every hero's first module: filled first, its empty slot's whole worth beat a first slot's upgrade, and one hero
                        // wore both Prime Modules while the other had a Refined one (the chain's saves replayed, November 15). Then by the gain; then more effects, the
                        // higher tier, the lead. The bag's order settles the rest (the first found stays).
                        double rank=(HeroWeapon.Wields(hero.id,def.slot)?1e7:0)+(Slots[s].second?-1e6:0)+gain*1000.0+effects*10+def.tier+(hero==lead?.25:0);
                        if(best!=null&&rank<=bestRank)continue;
                        best=new Wear{hero=hero,slot=def.slot,second=Slots[s].second,part=part,def=def,was=was,wasDef=wasDef,gain=gain};bestRank=rank;
                    }
                }
            }
            return best;
        }
    }
}
