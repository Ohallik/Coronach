using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class HitVolume:MonoBehaviour
    {
        public DamagePacket packet;
        public float radius=2,life=.12f;
        public HashSet<Health> hit=new();
        CombatActor owner;int sequence;bool sourcePresent;
        public void BindOwner()
        {sourcePresent=packet.source!=null;if(sourcePresent&&packet.source.TryGetComponent(out owner))sequence=owner.AttackSequence;}
        void Update()
        {
            if(sourcePresent&&(packet.source==null||!packet.source.Alive&&!packet.deathAttack)||owner!=null&&sequence!=owner.AttackSequence)
            {Destroy(gameObject);return;}
            if(Lattice.Core.GameTime.Paused)return;
            foreach(var col in Physics.OverlapSphere(transform.position,radius,~0,QueryTriggerInteraction.Collide))
            {
                if(!col.TryGetComponent<Hurtbox>(out var box)||box.owner==null||!hit.Add(box.owner))continue;
                box.Hit(packet);
            }
            life-=Time.deltaTime;if(life<=0)Destroy(gameObject);
        }
    }
}
