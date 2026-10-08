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
        public bool Alive=>integrity>0;
        public bool Targetable=>Alive&&Damageable;
        public bool Broken=>reactionOwner!=null?reactionOwner.Broken:Time.time<BrokenUntil;
        public event Action<Health,DamagePacket,float> Damaged;
        public event Action<Health,DamagePacket> Died;
        public event Action<Health> Revived;
        public static event Action<Health,float,bool> DamageNumber;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);}
        public float Receive(DamagePacket packet,float multiplier=1)
        {
            if(!Alive||!Damageable||packet.source==this||packet.source!=null&&packet.source.friendly==friendly)return 0;
            if(TryGetComponent<CombatActor>(out var actor)&&actor.Deflect(packet))return 0;
            if(GameTime.Now<InvulnerableUntil)return 0;
            var reaction=reactionOwner!=null?reactionOwner:this;
            float damage=friendly?Mathf.Max(0,packet.amount)*multiplier:DamageMath.Resolve(packet.amount,packet.type,weakness,resistance,reaction.Broken,packet.isCrit,multiplier);
            if(!friendly&&Time.time<reaction.ShieldUntil&&!reaction.Broken)damage*=.5f;
            if(actor!=null)damage*=actor.GuardDamageMultiplier*(100/(100+Mathf.Max(0,actor.plating-10)));
            integrity=Mathf.Max(0,integrity-damage);
            if(actor!=null&&integrity>0&&integrity<maximum*.3f)Lattice.Core.BarkService.Play(actor.character,"low");
            if(!friendly&&!reaction.Broken)
            {
                reaction.BreakMeter+=DamageMath.Break(packet.breakPower,packet.type,weakness);
                if(reaction.BreakMeter>=reaction.breakThreshold){reaction.BrokenUntil=Time.time+3;reaction.BreakMeter=0;Debug.Log("BREAK_OK "+reaction.id);}
            }
            // Resisted, shielded, guarded and hull hits sound armored, matching the
            // enemy recoil rule; a clean hit on a body sounds soft.
            bool armored=actor!=null&&(actor.flight||actor.GuardDamageMultiplier<1)||
                !friendly&&(Time.time<reaction.ShieldUntil&&!reaction.Broken||packet.type==resistance);
            if(damage>0)CombatAudio.Impact(this,armored);
            DamageNumber?.Invoke(this,damage,packet.type==weakness&&!friendly);
            Damaged?.Invoke(this,packet,damage);
            if(reaction!=this)
            {
                reaction.SlowUntil=Mathf.Max(reaction.SlowUntil,SlowUntil);
                reaction.Damaged?.Invoke(reaction,packet,damage);
            }
            if(!Alive)Died?.Invoke(this,packet);
            return damage;
        }
        public void Heal(float amount)
        {
            bool wasAlive=Alive;integrity=Mathf.Clamp(integrity+Mathf.Max(0,amount),0,maximum);
            if(!wasAlive&&Alive){BreakMeter=0;BrokenUntil=SlowUntil=ShieldUntil=0;Revived?.Invoke(this);}
        }
        public void ResetFull(){Heal(maximum);BreakMeter=0;BrokenUntil=SlowUntil=ShieldUntil=0;}
    }
}
