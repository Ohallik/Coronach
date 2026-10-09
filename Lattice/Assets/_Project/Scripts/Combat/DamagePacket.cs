using System.Collections.Generic;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    public struct DamagePacket
    {
        public float amount,breakPower;
        public DamageType type;
        public Health source;
        public Vector3 knockback;
        public bool isCrit;
        public bool deathAttack;
        public string tag;
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
