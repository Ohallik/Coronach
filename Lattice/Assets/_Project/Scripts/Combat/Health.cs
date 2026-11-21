using System;
using System.Collections.Generic;
using Lattice.Data;
using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class Health:MonoBehaviour
    {
        public static readonly List<Health> All=new();
        public string id;
        public bool friendly;
        public bool Damageable=true;
        // Separately targetable equipment can share its carrier's break/tell
        // response without sharing its integrity or resolution event.
        public Health reactionOwner;
        public float maximum=100,integrity=100,breakThreshold=80;
        public DamageType weakness=DamageType.Plasma,resistance=DamageType.Kinetic;
        float breakMeter;
        public float BreakMeter {get=>reactionOwner!=null?reactionOwner.BreakMeter:breakMeter;private set=>breakMeter=value;}
        public float BrokenUntil {get;private set;}
        public float InvulnerableUntil;
        public float SlowUntil;
        public float ShieldUntil;
        /// <summary>Armour that depends on the blow: the share of it a body lets through (a guardian's front takes a quarter, its back all of it). What it stops reads as a resisted hit,
        /// and a break opens it as it opens everything. Unset on every body that has none.</summary>
        public Func<DamagePacket,float> Plate;
        /// <summary>The least a blow may bring this body's integrity to (chapter 7: the Last Warden's hold on the road stops at a line until a clamp takes it over). Unset, or nought: none.</summary>
        public Func<float> Holds;
        /// <summary>A blow tagged so goes through a plate as if it weren't there (S6a: Idra's Cutting Torch, a rescue cutter's).</summary>
        public const string CutsPlate="cuts-plate";
        /// <summary>A packet's tag for a move meant to reach heroes of either form (a boss's sweep painted onto a berth's shelf): every other hit from a body of the other form passes a hero by.</summary>
        public const string AnyForm="any-form";
        /// <summary>Who takes a hero's hits for them, if anyone (S6a: Idra's Take the Weight). Asked of an enemy's hit that would have landed, after the hero's own roll and
        /// Flash have had their chance; the hit goes to whoever it names, whole, to guard or to take. Null, or naming nobody, when nobody is.</summary>
        public static Func<Health,Health> Covering;
        /// <summary>Until when a Cinder Brand holds (S3): a branded body takes <see cref="BrandTaken"/> times the damage, from everyone. Like a break it belongs to the
        /// carrier, so a brand on a collar link is a brand on the animal.</summary>
        public float BrandUntil;
        public const float BrandTaken=1.2f;
        public bool Branded=>reactionOwner!=null?reactionOwner.Branded:Time.time<BrandUntil;
        public void Brand(float seconds){var reaction=reactionOwner!=null?reactionOwner:this;reaction.BrandUntil=Mathf.Max(reaction.BrandUntil,Time.time+Mathf.Max(0,seconds));}
        /// <summary>Footing that slows a body for as long as it stands in it (shallows, a spore cloud, a puddle): a share of its walking pace, claimed for a moment and renewed each frame by whatever lays it, so it
        /// lapses by itself a step after the body is out. The slowest live claim wins. A hero's motor and an enemy's walk both read <see cref="Pace"/>; a dodge, a dash and a lunge keep their own speed, so there's always a way out.</summary>
        float hinderPace=1,hinderUntil;
        public void Hinder(float pace,float seconds=.15f)
        {
            pace=Mathf.Clamp(pace,.1f,1);
            if(Time.time>=hinderUntil||pace<=hinderPace){hinderPace=pace;hinderUntil=Time.time+Mathf.Max(0,seconds);}
        }
        public float Pace=>Time.time<hinderUntil?hinderPace:1;
        public bool Alive=>integrity>0;
        public bool Targetable=>Alive&&Damageable;
        public bool Broken=>reactionOwner!=null?reactionOwner.Broken:Time.time<BrokenUntil;
        public event Action<Health,DamagePacket,float> Damaged;
        public event Action<Health,DamagePacket> Died;
        public event Action<Health> Revived;
        public static event Action<Health,float,HitKind> DamageNumber;
        /// <summary>A hit that did something, once per hit, with the packet that did it: (victim, packet, damage, kind, it broke the body, it killed it). Raised before
        /// <see cref="Died"/>, so what answers a killing blow sees the body as it stood. <see cref="Damaged"/> is no use for this: a collar link raises it twice.</summary>
        public static event Action<Health,DamagePacket,float,HitKind,bool,bool> Impacted;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);}
        public float Receive(DamagePacket packet,float multiplier=1)
        {
            if(!Alive||!Damageable||packet.source==this||packet.source!=null&&packet.source.friendly==friendly)return 0;
            // Enemies stand frozen through a conversation or menu; nothing already in the air may land on a hero meanwhile.
            if(friendly&&GameServices.Current!=null&&GameServices.Current.Input.Blocked)return 0;
            // Chapter 10's berth: what a body of the other form dealt (a flier's shot over a hero on a shelf, a strike on the shelf under a ship) passes the hero by, unless the move was
            // marked to reach him (AnyForm, a boss's move painted onto the shelf). Only in a zone with a berth.
            if(friendly&&packet.tag!=AnyForm&&EnemyBrain.OtherForm(this,packet.source))return 0;
            if(TryGetComponent<CombatActor>(out var actor)&&actor.Deflect(packet))return 0;
            if(GameTime.Now<InvulnerableUntil)return 0;
            if(friendly&&Covering!=null&&packet.source!=null&&!packet.source.friendly){var cover=Covering(this);if(cover!=null&&cover!=this&&cover.Alive)return cover.Receive(packet,multiplier);}
            var reaction=reactionOwner!=null?reactionOwner:this;
            float damage=friendly?Mathf.Max(0,packet.amount)*multiplier:DamageMath.Resolve(packet.amount,packet.type,weakness,resistance,reaction.Broken,packet.isCrit,multiplier);
            // The difficulty's share of an enemy's hit (Difficulty), here where every one of them lands: a bite, a shot fired before the setting changed, a boss's special, a summon's,
            // a blast whose maker is gone by the time it goes off (so asked by reference: a destroyed source still says whose it was). What nobody dealt (the cave's pulse, rising
            // water, a wall flown into) names no source and is the place's, not an enemy's. Standard hands the number back as it came.
            if(friendly&&!ReferenceEquals(packet.source,null)&&!packet.source.friendly)damage=Difficulty.EnemyDamage(damage);
            if(!friendly&&Time.time<reaction.ShieldUntil&&!reaction.Broken)damage*=.5f;
            bool plated=false;if(!friendly&&reaction.Plate!=null&&!reaction.Broken&&packet.tag!=CutsPlate){float through=Mathf.Clamp01(reaction.Plate(packet));if(through<1){damage*=through;plated=true;}}
            if(!friendly&&reaction.Branded)damage*=BrandTaken;
            // What the striker's gear makes of it (an affix, a module's passive, a draught): here, where every hit lands, since a drone's bolt and a well's tick never pass through Packet.
            CombatActor striker=null;if(!friendly&&packet.source!=null)packet.source.TryGetComponent(out striker);
            if(striker!=null)damage*=GearEffects.Dealt(striker,packet,this,reaction);
            // Plating first, so a guard pays for what got past the frame: a heavier frame makes a guard cheaper as well as a hit lighter.
            if(actor!=null)damage=actor.Guarded(damage*GearEffects.Taken(actor)*(100/(100+Mathf.Max(0,actor.Plating-10))));
            // A spread covers lanes; its overlapping pellets do not multiply
            // damage on one hero hull. Invulnerable/deflected hits above do not
            // spend this budget. Other victims and later volleys remain live.
            if(actor!=null&&damage>0&&packet.hullVolleyHits!=null&&!packet.hullVolleyHits.Add(this))return 0;
            if(actor!=null)damage=GearEffects.Survive(actor,this,damage);
            // A bar held at a line (Holds): what lands is only what it gives up, so a blow on a held line is no kill and its number says what it took.
            if(!friendly&&Holds!=null)damage=Mathf.Min(damage,Mathf.Max(0,integrity-Mathf.Max(0,Holds())));
            integrity=Mathf.Max(0,integrity-damage);
            if(actor!=null&&integrity>0&&integrity<maximum*.3f)Lattice.Core.BarkService.Play(actor.character,"low");
            bool broke=false;
            if(!friendly&&!reaction.Broken)
            {
                reaction.BreakMeter+=DamageMath.Break(packet.breakPower,packet.type,weakness)*GearEffects.Break(striker);
                if(reaction.BreakMeter>=reaction.breakThreshold){reaction.BrokenUntil=Time.time+3+GearEffects.BrokenTime(striker);reaction.BreakMeter=0;broke=true;Debug.Log("BREAK_OK "+reaction.id);}
            }
            // Resisted, shielded, guarded and hull hits sound armored, matching the
            // enemy recoil rule; a clean hit on a body sounds soft.
            bool armored=actor!=null&&(actor.flight||actor.GuardDamageMultiplier<1)||
                !friendly&&(Time.time<reaction.ShieldUntil&&!reaction.Broken||packet.type==resistance||plated);
            if(damage>0)CombatAudio.Impact(this,armored);
            var kind=plated?HitKind.Resisted:KindOf(packet,reaction,multiplier);
            DamageNumber?.Invoke(this,damage,kind);
            if(damage>0||broke)Impacted?.Invoke(this,packet,damage,kind,broke,!Alive);
            Damaged?.Invoke(this,packet,damage);
            if(reaction!=this)
            {
                reaction.SlowUntil=Mathf.Max(reaction.SlowUntil,SlowUntil);
                reaction.Damaged?.Invoke(reaction,packet,damage);
            }
            if(!Alive)Died?.Invoke(this,packet);
            return damage;
        }
        /// <summary>Resisted hits used to look like any other, so nobody learned why a blade hit for half; a shielded body counts,
        /// which is the braced Bellows. A weak spot doubled the damage with no sign at all; a resisted type on one nets plain damage, so it stays grey.</summary>
        HitKind KindOf(DamagePacket packet,Health reaction,float multiplier)
        {
            if(friendly)return HitKind.Normal;
            if(Time.time<reaction.ShieldUntil&&!reaction.Broken)return HitKind.Resisted;
            return packet.type==resistance?HitKind.Resisted:multiplier>1?HitKind.WeakSpot:packet.type==weakness?HitKind.Weak:HitKind.Normal;
        }
        public void Heal(float amount)
        {
            bool wasAlive=Alive;integrity=Mathf.Clamp(integrity+Mathf.Max(0,amount),0,maximum);
            if(!wasAlive&&Alive){BreakMeter=0;BrokenUntil=SlowUntil=ShieldUntil=0;Revived?.Invoke(this);}
        }
        /// <summary>A deliberate opening (D198): a boss wedged in rock or surfacing from a dive is open to punishment as if
        /// broken, for a set time, without spending or filling its break meter.</summary>
        public void Expose(float seconds)
        {
            var reaction=reactionOwner!=null?reactionOwner:this;
            reaction.BrokenUntil=Mathf.Max(reaction.BrokenUntil,Time.time+Mathf.Max(0,seconds));
            Debug.Log("EXPOSED "+reaction.id);
        }
        /// <summary>Its break meter back to nought, as if nothing had been put into it: for a body that can't be broken for a while (the Host Marshal while a thaw coupling keeps it
        /// warm), so its meter neither fills on screen nor pays out the moment it can break again. Nothing else calls it.</summary>
        public void ClearBreak()=>BreakMeter=0;
        public void ResetFull(){Heal(maximum);BreakMeter=0;BrokenUntil=SlowUntil=ShieldUntil=BrandUntil=0;}
    }
}
