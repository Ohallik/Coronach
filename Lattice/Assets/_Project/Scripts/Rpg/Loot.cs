using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
namespace Lattice.Rpg
{
    /// <summary>The dice a part from the world is made with: how many affixes it carries, which ones, and which part "any Prime edge" turns out to be.
    /// A shop's part never comes through here, so what's found or made can be better than what's bought.</summary>
    public static class Loot
    {
        /// <summary>One stream for every roll. Seeded, a run of drops, caches and overclocks repeats exactly: a test's, or a replay's.</summary>
        public static Random Rng{get;private set;}=new();
        public static void Seed(int seed)=>Rng=new Random(seed);
        /// <summary>A part from the world carries one affix about a third of the time and two a tenth: 55 / 35 / 10.</summary>
        public const double OneAffix=.35,TwoAffixes=.10;
        /// <summary>How many affixes a roll in [0,1) gives.</summary>
        public static int AffixCount(double roll)=>roll<TwoAffixes?2:roll<TwoAffixes+OneAffix?1:0;
        /// <summary>Gives a new part its affixes, out of its own pool and never the same one twice. It always takes one number for the count and one for each affix, so a seed says the same thing every time.</summary>
        public static void RollAffixes(PartInstance item,TechPartDef def,Random rng=null)
        {
            rng??=Rng;int count=AffixCount(rng.NextDouble());
            for(int i=0;i<count;i++){string id=PickAffix(def,item.affixes,rng);if(id==null)break;item.affixes.Add(id);}
        }
        /// <summary>One affix from the part's pool that isn't in <paramref name="taken"/>, or null when the pool has nothing else.</summary>
        public static string PickAffix(TechPartDef def,ICollection<string> taken,Random rng=null)
        {
            rng??=Rng;
            var open=(def.affixPool??Array.Empty<AffixDef>()).Where(a=>a!=null&&!string.IsNullOrEmpty(a.id)&&(taken==null||!taken.Contains(a.id))).Select(a=>a.id).Distinct().ToList();
            return open.Count==0?null:open[rng.Next(open.Count)];
        }
        /// <summary>Any part of a tier that drops, in one slot or in any: null when nothing of that tier does, which is how a tier stays out of the world until its chapter places it.
        /// The pool is read in id order, so the same seed picks the same part whatever order the assets load in.</summary>
        public static TechPartDef PickPart(int tier,GearSlot slot,bool anySlot,Random rng=null)
        {
            rng??=Rng;
            var pool=GameCatalog.All<TechPartDef>().Where(p=>p!=null&&p.drops&&p.tier==tier&&(anySlot||p.slot==slot)).OrderBy(p=>p.id,StringComparer.Ordinal).ToList();
            return pool.Count==0?null:pool[rng.Next(pool.Count)];
        }
    }
}
