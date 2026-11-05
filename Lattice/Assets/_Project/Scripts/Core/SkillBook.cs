using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Core
{
    /// <summary>Which skills each hero has learned, which four are equipped, and how well each is mastered (D201).
    /// Skills are learned with Sync level, as in Ys; every release raises that skill's mastery, which makes it hit
    /// harder and cost less Charge. Loadout and use counts live in the saved <see cref="MemberState"/>.</summary>
    public static class SkillBook
    {
        public const int Slots=4,MaxMastery=5;
        /// <summary>Releases needed to reach mastery 2..5.</summary>
        public static readonly int[] MasteryUses={6,18,36,60};
        public const float PowerPerMastery=.1f,CostPerMastery=2,MinimumCost=8;
        public static event Action<string,SkillDef,int> MasteryRaised;

        static MemberState Member(string character)=>GameServices.Current?.State?.party?.Find(m=>m.id==character);

        public static SkillDef[] All(string character)
        {
            var definition=GameCatalog.Find<CharacterDef>(character);if(definition==null)return Array.Empty<SkillDef>();
            return definition.skills.Concat(definition.learnable??Array.Empty<SkillDef>()).Where(s=>s!=null).ToArray();
        }
        public static bool Knows(MemberState member,SkillDef skill)=>skill!=null&&(member==null?skill.unlockLevel<=1:member.level>=skill.unlockLevel);
        public static SkillDef[] Learned(string character)=>All(character).Where(s=>Knows(Member(character),s)).ToArray();
        /// <summary>Skills a level-up from <paramref name="from"/> to <paramref name="to"/> teaches.</summary>
        public static SkillDef[] LearnedBetween(string character,int from,int to)=>All(character).Where(s=>s.unlockLevel>from&&s.unlockLevel<=to).ToArray();

        public static SkillDef Equipped(string character,int slot)
        {
            if(slot<0||slot>=Slots)return null;
            var definition=GameCatalog.Find<CharacterDef>(character);if(definition==null)return null;
            var member=Member(character);
            if(member?.skillLoadout!=null&&slot<member.skillLoadout.Count&&!string.IsNullOrEmpty(member.skillLoadout[slot]))
            {
                var chosen=All(character).FirstOrDefault(s=>s.id==member.skillLoadout[slot]);
                if(chosen!=null&&Knows(member,chosen))return chosen;
            }
            return definition.skills.Length>slot?definition.skills[slot]:null;
        }
        /// <summary>Puts a learned skill in a slot; if it was already in another slot, the two trade places.</summary>
        public static bool Equip(MemberState member,int slot,SkillDef skill)
        {
            if(member==null||skill==null||slot<0||slot>=Slots||!Knows(member,skill))return false;
            var current=new List<string>();for(int i=0;i<Slots;i++)current.Add(Equipped(member.id,i)?.id);
            int previous=current.IndexOf(skill.id);
            if(previous>=0&&previous!=slot)current[previous]=current[slot];
            current[slot]=skill.id;member.skillLoadout=current;return true;
        }

        public static int Uses(string character,string skill){var member=Member(character);return member?.skillUses!=null&&member.skillUses.TryGetValue(skill,out int n)?n:0;}
        public static int MasteryFor(int uses){int level=1;foreach(int threshold in MasteryUses)if(uses>=threshold)level++;return Mathf.Min(level,MaxMastery);}
        public static int Mastery(string character,string skill)=>MasteryFor(Uses(character,skill));
        public static float Power(int mastery)=>1+PowerPerMastery*(Mathf.Clamp(mastery,1,MaxMastery)-1);
        public static float Cost(SkillDef skill,int mastery)=>skill==null?20:Mathf.Max(MinimumCost,skill.chargeCost-CostPerMastery*(Mathf.Clamp(mastery,1,MaxMastery)-1));
        public static float Cost(string character,SkillDef skill)=>Cost(skill,skill==null?1:Mastery(character,skill.id));
        /// <summary>Uses needed for the next mastery level, or 0 at the cap.</summary>
        public static int NextMasteryAt(int uses){foreach(int threshold in MasteryUses)if(uses<threshold)return threshold;return 0;}

        public static void RecordUse(string character,SkillDef skill)
        {
            var member=Member(character);if(member==null||skill==null)return;
            member.skillUses??=new Dictionary<string,int>();
            member.skillUses.TryGetValue(skill.id,out int uses);int before=MasteryFor(uses);
            member.skillUses[skill.id]=++uses;int after=MasteryFor(uses);
            if(after>before){Debug.Log($"SKILL_MASTERY {character} {skill.id} {after}");MasteryRaised?.Invoke(character,skill,after);}
        }
    }
}
