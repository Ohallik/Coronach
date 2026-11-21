using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>Where a hero's <see cref="Lattice.Data.Mods"/> meet a hit: what an affix, a module's passive or a draught does to damage dealt and taken, to a Break and to a killing blow.
    /// They're applied where the hit lands (Health.Receive), not where it's made, so a drone's bolt and a well's tick count as a swing does: three skills build their packets by hand.
    /// The bonuses add to each other and then multiply what weakness, Break and a crit already made of the hit.</summary>
    public static class GearEffects
    {
        /// <summary>Executioner's line: under this share of its Integrity a body takes the bonus.</summary>
        public const float ExecuteBelow=.3f;
        /// <summary>How long a Flash Coil's bonus runs after a Flash Move or a Flash Guard.</summary>
        public const float FlashCoilSeconds=5;
        /// <summary>Charge a second a Reservoir refills, out of a fight, up to its floor.</summary>
        public const float FloorRefill=10;
        /// <summary>What the striker's gear makes of a hit on this body: damage dealt, its type's bonus, and what it has against a Broken or a nearly finished enemy.</summary>
        public static float Dealt(CombatActor striker,DamagePacket packet,Health victim,Health reaction)
        {
            if(striker==null)return 1;
            var m=striker.mods;
            float bonus=m.damageDealt+m.Type(packet.type);
            if(reaction!=null&&reaction.Broken)bonus+=m.brokenDamage;
            if(victim!=null&&victim.maximum>0&&victim.integrity<victim.maximum*ExecuteBelow)bonus+=m.executeDamage;
            if(GameTime.Now<striker.flashDamageUntil)bonus+=m.flashDamage;
            return Mathf.Max(0,1+bonus);
        }
        public static float Break(CombatActor striker)=>striker==null?1:Mathf.Max(0,1+striker.mods.breakPower);
        public static float BrokenTime(CombatActor striker)=>striker==null?0:Mathf.Max(0,striker.mods.brokenTime);
        /// <summary>What the hero's own gear makes of a hit on them, before the frame and the guard.</summary>
        public static float Taken(CombatActor hero)=>hero==null?1:Mathf.Max(.1f,1+hero.mods.damageTaken);
        /// <summary>A Second Wind: the blow that would have finished the hero leaves 1 Integrity, once a fight. It comes back when nothing near is awake.</summary>
        public static float Survive(CombatActor hero,Health body,float damage)
        {
            if(hero==null||hero.mods.secondWind<=0||hero.secondWindSpent||body.integrity<=1||damage<body.integrity)return damage;
            hero.secondWindSpent=true;hero.nextCalmCheck=GameTime.Now+2;
            CombatVfx.Burst(hero.transform.position+Vector3.up*1.2f,new Color(.5f,1.5f,1.3f),"shape");
            Notices.Post("SECOND WIND",$"{DisplayNames.Speaker(hero.character)} holds at 1 Integrity.");
            Debug.Log("SECOND_WIND "+hero.character);
            return body.integrity-1;
        }
        /// <summary>A Flash Move or a Flash Guard just happened: what gear adds to it.</summary>
        public static void Flashed(CombatActor hero)
        {
            var m=hero.mods;
            if(m.flashCharge>0)hero.charge=Mathf.Min(100,hero.charge+m.flashCharge);
            if(m.flashDamage>0)hero.flashDamageUntil=GameTime.Now+FlashCoilSeconds;
        }
        public static bool InFight(Vector3 at)=>BossController.AnyFightUnderway||EnemyBrain.AwakeNear(at,EnemyBrain.InteractionCalm);
        /// <summary>Each frame of a hero's own clock: a meal's slow mend, a Reservoir's refill and the Second Wind's return. Whether a fight is on is asked twice a second, and only by gear that cares.</summary>
        public static void Tick(CombatActor hero)
        {
            var m=hero.mods;
            if(m.regen>0&&hero.Health.integrity<hero.Health.maximum)hero.Health.Heal(hero.Health.maximum*m.regen*Time.deltaTime);
            if(m.chargeFloor<=0&&!hero.secondWindSpent)return;
            if(GameTime.Now>=hero.nextCalmCheck)
            {
                hero.nextCalmCheck=GameTime.Now+.5f;hero.calm=!InFight(hero.transform.position);
                if(hero.calm)hero.secondWindSpent=false;
            }
            if(hero.calm&&hero.charge<m.chargeFloor)hero.charge=Mathf.Min(m.chargeFloor,hero.charge+FloorRefill*Time.deltaTime);
        }
    }
}
