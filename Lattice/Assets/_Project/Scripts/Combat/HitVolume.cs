using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class HitVolume:MonoBehaviour
    {
        public DamagePacket packet;
        public float radius=2,life=.12f;
        /// <summary>False for a released skill: it can't reach through solid scenery.</summary>
        public bool ThroughWalls=true;
        public HashSet<Health> hit=new();
        CombatActor owner;int sequence;bool sourcePresent,released;
        /// <summary>A released effect stands on its own: the caster's next action or fall doesn't erase it.</summary>
        public void Detach(){released=true;owner=null;}
        public void BindOwner()
        {sourcePresent=packet.source!=null;if(sourcePresent&&packet.source.TryGetComponent(out owner))sequence=owner.AttackSequence;}
        void Update()
        {
            if(!released&&(sourcePresent&&(packet.source==null||!packet.deathAttack&&(!packet.source.Alive||packet.source.Broken))||owner!=null&&sequence!=owner.AttackSequence))
            {Destroy(gameObject);return;}
            if(Lattice.Core.GameTime.CombatHeld)return;
            foreach(var col in Physics.OverlapSphere(transform.position,radius,~0,QueryTriggerInteraction.Collide))
            {
                if(!col.TryGetComponent<Hurtbox>(out var box)||box.owner==null||hit.Contains(box.owner))continue;
                if(!ThroughWalls)
                {
                    var nearest=col is MeshCollider mesh&&!mesh.convex?col.bounds.ClosestPoint(transform.position):col.ClosestPoint(transform.position);
                    if(!CombatCover.Clear(transform.position,nearest))continue;
                }
                hit.Add(box.owner);box.Hit(packet);
            }
            life-=Time.deltaTime;if(life<=0)Destroy(gameObject);
        }
    }
}
