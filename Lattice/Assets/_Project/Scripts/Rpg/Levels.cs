using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Rpg
{
    public static class Levels
    {
        public static int RequiredXp(int level)=>Mathf.Max(1,level)*110;
        public static int Grant(MemberState member,int xp)
        {
            if(xp<0)throw new System.ArgumentOutOfRangeException(nameof(xp));
            member.xp+=xp;int gained=0;
            while(member.level<10&&member.xp>=RequiredXp(member.level))
            {member.xp-=RequiredXp(member.level);member.level++;gained++;member.integrity=MaxIntegrity(member.level);}
            return gained;
        }
        public static float MaxIntegrity(int level)=>120+18*(Mathf.Clamp(level,1,10)-1);
        public static Stats Calculate(CharacterDef character,int level)=>character.baseStats+character.growthPerLevel*(Mathf.Clamp(level,1,10)-1);
        public static Stats PartStats(TechPartDef part,PartInstance instance)=>part.baseStats*(1+Mathf.Clamp(instance.upgrade,0,5)*.08f);
    }
}
