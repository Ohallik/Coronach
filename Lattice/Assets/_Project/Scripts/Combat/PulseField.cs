using UnityEngine;
namespace Lattice.Combat
{
    public sealed class PulseField:MonoBehaviour
    {
        public DamagePacket packet;
        public float radius=4,life=4;
        public bool flight;
        bool factionCaptured,hasFaction,friendly;
        float nextPulse;
        internal void RememberFaction(bool known,bool ally)
        {factionCaptured=true;hasFaction=known;friendly=ally;}
        void Start()
        {
            if(!factionCaptured)RememberFaction(packet.source!=null,packet.source!=null&&packet.source.friendly);
            var ring=gameObject.AddComponent<GroundRing>();ring.Initialize(radius,flight?0:.12f,flight?0:.75f);
            ring.Draw(radius,new Color(.12f,.85f,1,.8f));
        }
        void Update()
        {
            if(Lattice.Core.GameTime.Paused)return;
            life-=Time.deltaTime;if(life<=0){Destroy(gameObject);return;}
            if(Time.time<nextPulse)return;nextPulse=Time.time+1;
            foreach(var health in Health.All.ToArray())
            {
                if(health==null||!health.Alive||health==packet.source||hasFaction&&health.friendly==friendly)continue;
                var body=flight?health.GetComponent<CapsuleCollider>():null;
                var target=body!=null?body.ClosestPoint(transform.position):health.transform.position;
                if((target-transform.position).sqrMagnitude>radius*radius||
                    !CombatCover.Clear(transform.position+Vector3.up*(flight?0:.75f),target+Vector3.up*(flight?0:.75f)))continue;
                health.SlowUntil=Time.time+1.2f;health.Receive(packet);
            }
        }
    }
}
