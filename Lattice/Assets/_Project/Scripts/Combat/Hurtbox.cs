using UnityEngine;
namespace Lattice.Combat
{
    public sealed class Hurtbox:MonoBehaviour
    {
        public Health owner;
        public float multiplier=1;
        void Awake(){if(owner==null)owner=GetComponentInParent<Health>();}
        public float Hit(DamagePacket packet)=>owner!=null?owner.Receive(packet,multiplier):0;
    }
}
