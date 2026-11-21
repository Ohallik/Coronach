using System;
using UnityEngine;
namespace Lattice.Data
{
    public enum ZoneKind{GroundSafe,GroundCombat,SpaceSafe,SpaceCombat}
    public enum BodyForm{Natural,Shaped,CivilFlight,Flight}
    public enum DamageType{Beam,Plasma,Kinetic,Pulse}
    public enum GearSlot{Emitter,Edge,Frame,Drive,Module}
    public enum Discipline{Emitters,Edges,Frames,Drives,Modules}
    public enum EnemyArchetype{PackHunter,Swarm,Sentinel,Spitter,Mine,Serpent}
    public enum ObjectiveKind{TalkTo,Reach,Kill,Collect,Interact,Flag}
    public enum ActorState{Idle,Move,Attack,Dodge,Guard,Skill,Stagger,Down,Dead}
    [Serializable] public struct Stats
    {
        public float output,plating,response,resonance,fortune;
        public static Stats operator +(Stats a,Stats b)=>new(){output=a.output+b.output,plating=a.plating+b.plating,response=a.response+b.response,resonance=a.resonance+b.resonance,fortune=a.fortune+b.fortune};
        public static Stats operator *(Stats a,float n)=>new(){output=a.output*n,plating=a.plating*n,response=a.response*n,resonance=a.resonance*n,fortune=a.fortune*n};
    }
    [Serializable] public struct Ingredient{public string id; public int count;}
    [Serializable] public struct Objective{public ObjectiveKind kind;public string target;public int count;}
    /// <summary>What a drop or a cache holds. An <see cref="id"/> names a material, a consumable or a part. With no id and a <see cref="partTier"/> it is any part of that tier
    /// that drops (TechPartDef.drops), in <see cref="partSlot"/> or, with <see cref="anySlot"/>, in any slot. A part from the world rolls its affixes (Loot.RollAffixes).</summary>
    [Serializable] public struct LootEntry{public string id;public int count;[Range(0,1)] public float chance;public int partTier;public GearSlot partSlot;public bool anySlot;}
    /// <summary>One thing a shop sells, at its own price: a part, a consumable or a material by id. A material sells <see cref="lot"/> at a time (3 when unset).
    /// With <see cref="requires"/>, it is on the counter only once that flag is set.</summary>
    [Serializable] public struct ShopOffer{public string id;public int price,lot;public string requires;}
    /// <summary>What gear does beyond the five stats: an affix, a module's passive or a draught adds to these, a hero's parts sum them (Gear.Modifiers) and the fight reads the sum
    /// (CombatActor.mods). Shares are fractions (.05 is five points or five percent); every field adds.</summary>
    [Serializable] public struct Mods
    {
        /// <summary>Crit chance, in points on top of Response's; how much harder a crit hits; Break power.</summary>
        public float critChance,critDamage,breakPower;
        /// <summary>Damage of the wearing part's own type: Gear turns it into one of the four below when it sums a part.</summary>
        public float ownType;
        public float beam,plasma,kinetic,pulse;
        /// <summary>Charge from hits and kills; Integrity a kill gives back, as a share of the bar; Charge off a skill's cost (negative is cheaper); a skill's cooldown (-.1 is a tenth shorter).</summary>
        public float chargeGain,lifeOnKill,skillCost,cooldown;
        /// <summary>What a guard pays in Thrust, what a roll out of rhythm pays, and how fast Thrust returns.</summary>
        public float guardCost,mashCost,thrustRegen;
        /// <summary>Seconds on the Flash Move and Flash Guard windows; Charge a Flash pays on top of its own.</summary>
        public float flashWindow,flashCharge;
        /// <summary>Damage to a Broken enemy, and to one under 30% Integrity.</summary>
        public float brokenDamage,executeDamage;
        /// <summary>Drop chance added past Fortune's cap, and how far that cap is raised.</summary>
        public float dropLuck,luckCap;
        public float damageDealt,damageTaken;
        /// <summary>Added to the partner's 60% share of damage.</summary>
        public float partnerDamage;
        /// <summary>Above zero, Charge doesn't fade; out of a fight it refills to the floor.</summary>
        public float chargeHold,chargeFloor;
        /// <summary>Seconds added to a Break this hero causes; damage for five seconds after a Flash; how much of a healing item also reaches the partner.</summary>
        public float brokenTime,flashDamage,sharedHeal;
        /// <summary>Mastery levels a skill acts above its own; above zero, once a fight a killing blow leaves 1 Integrity; Integrity restored each second, as a share of the bar.</summary>
        public float mastery,secondWind,regen;
        public static Mods operator +(Mods a,Mods b)=>new()
        {
            critChance=a.critChance+b.critChance,critDamage=a.critDamage+b.critDamage,breakPower=a.breakPower+b.breakPower,ownType=a.ownType+b.ownType,
            beam=a.beam+b.beam,plasma=a.plasma+b.plasma,kinetic=a.kinetic+b.kinetic,pulse=a.pulse+b.pulse,
            chargeGain=a.chargeGain+b.chargeGain,lifeOnKill=a.lifeOnKill+b.lifeOnKill,skillCost=a.skillCost+b.skillCost,cooldown=a.cooldown+b.cooldown,
            guardCost=a.guardCost+b.guardCost,mashCost=a.mashCost+b.mashCost,thrustRegen=a.thrustRegen+b.thrustRegen,flashWindow=a.flashWindow+b.flashWindow,flashCharge=a.flashCharge+b.flashCharge,
            brokenDamage=a.brokenDamage+b.brokenDamage,executeDamage=a.executeDamage+b.executeDamage,dropLuck=a.dropLuck+b.dropLuck,luckCap=a.luckCap+b.luckCap,
            damageDealt=a.damageDealt+b.damageDealt,damageTaken=a.damageTaken+b.damageTaken,partnerDamage=a.partnerDamage+b.partnerDamage,
            chargeHold=a.chargeHold+b.chargeHold,chargeFloor=a.chargeFloor+b.chargeFloor,brokenTime=a.brokenTime+b.brokenTime,flashDamage=a.flashDamage+b.flashDamage,
            sharedHeal=a.sharedHeal+b.sharedHeal,mastery=a.mastery+b.mastery,secondWind=a.secondWind+b.secondWind,regen=a.regen+b.regen,
        };
        /// <summary>The bonus to one damage type.</summary>
        public readonly float Type(DamageType type)=>type switch{DamageType.Beam=>beam,DamageType.Plasma=>plasma,DamageType.Kinetic=>kinetic,_=>pulse};
        /// <summary>These as a part of that type wears them: <see cref="ownType"/> becomes that type's bonus.</summary>
        public readonly Mods On(DamageType type)
        {
            var worn=this;if(ownType==0)return worn;
            if(type==DamageType.Beam)worn.beam+=ownType;else if(type==DamageType.Plasma)worn.plasma+=ownType;else if(type==DamageType.Kinetic)worn.kinetic+=ownType;else worn.pulse+=ownType;
            worn.ownType=0;return worn;
        }
        /// <summary>Whether these change anything at all.</summary>
        public readonly bool Any=>critChance!=0||critDamage!=0||breakPower!=0||ownType!=0||beam!=0||plasma!=0||kinetic!=0||pulse!=0||chargeGain!=0||lifeOnKill!=0||skillCost!=0||cooldown!=0||
            guardCost!=0||mashCost!=0||thrustRegen!=0||flashWindow!=0||flashCharge!=0||brokenDamage!=0||executeDamage!=0||dropLuck!=0||luckCap!=0||damageDealt!=0||damageTaken!=0||
            partnerDamage!=0||chargeHold!=0||chargeFloor!=0||brokenTime!=0||flashDamage!=0||sharedHeal!=0||mastery!=0||secondWind!=0||regen!=0;
    }
}
