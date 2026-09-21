using UnityEngine;
namespace Lattice.Combat
{
    public sealed class PulseField:MonoBehaviour
    {
        public DamagePacket packet;
        public float radius=4,life=4;
        float nextPulse;
        void Start()=>ActorFactory.Visual("NetField",PrimitiveType.Cylinder,transform,new Vector3(radius*2,.02f,radius*2),Vector3.up*.1f,"Emission");
        void Update()
        {
            life-=Time.deltaTime;if(life<=0){Destroy(gameObject);return;}
            if(Time.time<nextPulse)return;nextPulse=Time.time+1;
            foreach(var health in Health.All.ToArray())
            {
                if(health==null||health.friendly||!health.Alive||(health.transform.position-transform.position).sqrMagnitude>radius*radius)continue;
                health.SlowUntil=Time.time+1.2f;health.Receive(packet);
            }
        }
    }
}
