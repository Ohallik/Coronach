using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Rpg
{
    public static class Levels
    {
        /// <summary>Sync level cap. Chapter 1 ends near level 7; later chapters keep growing (D201 raised it from 10).</summary>
        public const int Cap=40;
        /// <summary>A member reached a new level: (member, previous level, new level).</summary>
        public static event System.Action<MemberState,int,int> LevelRaised;
        /// <summary>XP from one Sync level to the next. At 125 per level the ledger lands on the plan: chapter 1 ends near Sync 7,
        /// Hessa at 9 (10 with the side content), chapter 2 at 10 (11), with Sync 12 and the last two skills in chapter 3. At 110 it ran a level or two over.</summary>
        public static int RequiredXp(int level)=>Mathf.Max(1,level)*125;
        public static int Grant(MemberState member,int xp)
        {
            if(xp<0)throw new System.ArgumentOutOfRangeException(nameof(xp));
            member.xp+=xp;int gained=0;int before=member.level;
            while(member.level<Cap&&member.xp>=RequiredXp(member.level))
            {member.xp-=RequiredXp(member.level);member.level++;gained++;member.integrity=MaxIntegrity(member.level);}
            if(gained>0){Debug.Log($"LEVEL_UP {member.id} {before}->{member.level}");LevelRaised?.Invoke(member,before,member.level);}
            return gained;
        }
        public static float MaxIntegrity(int level)=>ChapterProgress.MaxIntegrity(level);
        public static Stats Calculate(CharacterDef character,int level)=>character.baseStats+character.growthPerLevel*(Mathf.Clamp(level,1,Cap)-1);
        public static Stats PartStats(TechPartDef part,PartInstance instance)=>part.baseStats*(1+Mathf.Clamp(instance.upgrade,0,5)*.08f);
    }
}
