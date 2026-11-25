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
        GroundRing ring;float dismissed=-1;
        const float DismissSeconds=.25f;
        /// <summary>The fight it was laid in is over. It goes quickly, and through a conversation too: its clock stops for one, which left the ring on the floor for the whole talk.
        /// D385: at once, on the frame the fight ends. Faded over a quarter second, it was round the dead Bellows at full strength in the still taken as the fight's step ended
        /// (nov23-whole s08/092), and round the Hulljack (s29/034).</summary>
        public void Dismiss(){if(dismissed>=0)return;dismissed=DismissSeconds;if(ring!=null)ring.Draw(radius,Color.clear);Destroy(gameObject);}
        internal void RememberFaction(bool known,bool ally)
        {factionCaptured=true;hasFaction=known;friendly=ally;}
        void Start()
        {
            if(!factionCaptured)RememberFaction(packet.source!=null,packet.source!=null&&packet.source.friendly);
            ring=gameObject.AddComponent<GroundRing>();ring.Initialize(radius,flight?0:.12f,flight?0:.75f);
            ring.Draw(radius,new Color(.12f,.85f,1,.8f));
            // D363: laid after its fight was won it goes as one cleared at the win. Sela's net, begun as the Bellows died, landed after the win had cleared the field and lay
            // round the dead dome for four seconds, a bright cyan ring that read as the boss's (nov22b-whole s08/098 to 104).
            if(hasFaction&&!AnyFoeNear())Dismiss();
        }
        /// <summary>A living body of the other side that can still be hurt, anywhere a fight round here could be (a beaten boss's parts are shut with it: EnemyBrain.ShutItsParts).</summary>
        bool AnyFoeNear()
        {
            const float reach=40;
            foreach(var health in Health.All)
                if(health!=null&&health.Alive&&health.Damageable&&health.friendly!=friendly&&(health.transform.position-transform.position).sqrMagnitude<reach*reach)return true;
            return false;
        }
        void Update()
        {
            if(dismissed>=0)
            {
                dismissed+=Time.unscaledDeltaTime;
                if(ring!=null)ring.Draw(radius,new Color(.12f,.85f,1,.8f*Mathf.Clamp01(1-dismissed/DismissSeconds)));
                if(dismissed>=DismissSeconds)Destroy(gameObject);
                return;
            }
            if(Lattice.Core.GameTime.CombatHeld)return;
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
