using System.Collections.Generic;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>What a hit's number and spark say about it.</summary>
    public enum HitKind{Normal,Weak,Resisted,WeakSpot}
    /// <summary>How hard a hit lands, for what it does to the clock, the camera and the body it strikes (<see cref="Impact"/>). None is a hit nobody swung, and it's the
    /// default on purpose: a packet built by hand, by a fixture or a wall, changes nothing but integrity. Tick is one of many: a needle, a pellet, a turn of a spin.</summary>
    public enum HitWeight{None,Tick,Light,Medium,Heavy,Finisher}
    public struct DamagePacket
    {
        public HitWeight weight;
        public float amount,breakPower;
        public DamageType type;
        public Health source;
        public Vector3 knockback;
        public bool isCrit;
        /// <summary>This hit carries a crit a hero earned, which it spends by landing: see <see cref="CombatActor.Packet"/>.</summary>
        public bool earnedCrit;
        public bool deathAttack;
        public string tag;
        /// <summary>How much of a landed hit's Charge this packet gives up: a skill landing many small hits can't repay itself on one target.</summary>
        public float chargeCut;
        public ProjectileOrigin projectileOrigin;
        // Shared only by one hostile spread. Released shots retain this
        // bounded hit budget independently of capture or source lifetime.
        public HashSet<Health> hullVolleyHits;
    }
    public static class DamageMath
    {
        public static float Resolve(float amount,DamageType type,DamageType weakness,DamageType resistance,bool broken,bool critical,float multiplier=1)
        {return Mathf.Max(0,amount)*(type==weakness?1.5f:type==resistance?.5f:1)*(broken?1.5f:1)*(critical?1.5f:1)*multiplier;}
        public static float Break(float power,DamageType type,DamageType weakness)=>Mathf.Max(0,power)*(type==weakness?2:1);
    }
}
