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
        public float maximum=100,integrity=100,breakThreshold=80;
        public DamageType weakness=DamageType.Plasma,resistance=DamageType.Kinetic;
        public float BreakMeter {get;private set;}
        public float BrokenUntil {get;private set;}
        public float InvulnerableUntil;
        public float SlowUntil;
        public float ShieldUntil;
        public bool Alive=>integrity>0;
        public bool Broken=>Time.time<BrokenUntil;
        public event Action<Health,DamagePacket,float> Damaged;
        public event Action<Health,DamagePacket> Died;
        public static event Action<Health,float,bool> DamageNumber;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);}
        public float Receive(DamagePacket packet,float multiplier=1)
        {
            if(!Alive||packet.source==this||packet.source!=null&&packet.source.friendly==friendly)return 0;
            if(TryGetComponent<CombatActor>(out var actor)&&actor.Deflect(packet))return 0;
            if(GameTime.Now<InvulnerableUntil)return 0;
            float damage=friendly?Mathf.Max(0,packet.amount)*multiplier:DamageMath.Resolve(packet.amount,packet.type,weakness,resistance,Broken,packet.isCrit,multiplier);
            if(!friendly&&Time.time<ShieldUntil&&!Broken)damage*=.5f;
            if(actor!=null)damage*=actor.GuardDamageMultiplier*(100/(100+Mathf.Max(0,actor.plating-10)));
            integrity=Mathf.Max(0,integrity-damage);
            if(actor!=null&&integrity>0&&integrity<maximum*.3f)Lattice.Core.BarkService.Play(actor.character,"low");
            if(!friendly&&!Broken)
            {
                BreakMeter+=DamageMath.Break(packet.breakPower,packet.type,weakness);
                if(BreakMeter>=breakThreshold){BrokenUntil=Time.time+3;BreakMeter=0;Debug.Log("BREAK_OK "+id);}
            }
            DamageNumber?.Invoke(this,damage,packet.type==weakness&&!friendly);
            Damaged?.Invoke(this,packet,damage);
            if(!Alive)Died?.Invoke(this,packet);
            return damage;
        }
        public void Heal(float amount){integrity=Mathf.Clamp(integrity+Mathf.Max(0,amount),0,maximum);}
        public void ResetFull(){integrity=maximum;BreakMeter=0;BrokenUntil=0;}
    }
}
