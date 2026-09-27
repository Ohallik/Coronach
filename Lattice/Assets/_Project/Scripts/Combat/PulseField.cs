using UnityEngine;
namespace Lattice.Combat
{
    public sealed class PulseField:MonoBehaviour
    {
        public DamagePacket packet;
        public float radius=4,life=4;
        float nextPulse;
        void Start()
        {
            var ring=gameObject.AddComponent<GroundRing>();ring.Initialize(radius);
            ring.Draw(radius,new Color(.12f,.85f,1,.8f));
        }
        void Update()
        {
            if(Lattice.Core.GameTime.Paused)return;
            life-=Time.deltaTime;if(life<=0){Destroy(gameObject);return;}
            if(Time.time<nextPulse)return;nextPulse=Time.time+1;
            foreach(var health in Health.All.ToArray())
            {
                if(health==null||!health.Alive||health==packet.source||packet.source!=null&&health.friendly==packet.source.friendly||
                    (health.transform.position-transform.position).sqrMagnitude>radius*radius||
                    !CombatCover.Clear(transform.position+Vector3.up*.75f,health.transform.position+Vector3.up*.75f))continue;
                health.SlowUntil=Time.time+1.2f;health.Receive(packet);
            }
        }
    }
}
