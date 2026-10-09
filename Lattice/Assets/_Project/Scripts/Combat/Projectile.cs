using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class Projectile:MonoBehaviour
    {
        static readonly Stack<Projectile> pool=new();
        DamagePacket packet;
        Vector3 velocity;
        float life;
        bool counted,hasFaction,friendly;
        public static int ActiveCount {get;private set;}
        /// <summary>Shots in the air, so AI can read incoming fire as a player does.</summary>
        public static readonly HashSet<Projectile> Live=new();
        public Vector3 Velocity=>velocity;
        /// <summary>Would this shot harm a body on the given side?</summary>
        public bool Threatens(bool friendlySide)=>hasFaction&&friendly!=friendlySide;
        public static void Fire(Vector3 position,Vector3 direction,DamagePacket damage,float speed=24)
        {
            Projectile p=null;while(pool.Count>0&&p==null)p=pool.Pop();
            if(p==null)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Projectile";Destroy(go.GetComponent<Collider>());
                go.transform.localScale=Vector3.one*.18f;p=go.AddComponent<Projectile>();
                go.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Blockout/Emission");
            }
            p.transform.position=position;p.velocity=direction.normalized*speed;p.life=3;
            damage.projectileOrigin=ProjectileProvenance.StampLaunch(damage,position,p.velocity,p.GetInstanceID());p.packet=damage;
            p.hasFaction=damage.source!=null;p.friendly=p.hasFaction&&damage.source.friendly;
            p.gameObject.SetActive(true);p.counted=true;ActiveCount++;Live.Add(p);
        }
        void Update()
        {
            if(Lattice.Core.GameTime.Paused)return;
            var delta=velocity*Time.deltaTime;
            Hurtbox victim=null;float victimDistance=float.PositiveInfinity,coverDistance=float.PositiveInfinity;
            foreach(var hit in Physics.SphereCastAll(transform.position,.15f,velocity.normalized,delta.magnitude,~0,QueryTriggerInteraction.Collide))
            {
                if(CombatCover.Opaque(hit.collider)){coverDistance=Mathf.Min(coverDistance,hit.distance);continue;}
                if(!hit.collider.TryGetComponent<Hurtbox>(out var box)||box.owner==null||!box.owner.Alive||
                    box.owner==packet.source||hasFaction&&box.owner.friendly==friendly)continue;
                if(hit.distance<victimDistance){victimDistance=hit.distance;victim=box;}
            }
            // Sweep results are unordered. A farther victim may never win over
            // an earlier wall/body just because physics returned it first.
            if(!float.IsPositiveInfinity(coverDistance)&&coverDistance<=victimDistance){Recycle();return;}
            if(victim!=null){victim.Hit(packet);Recycle();return;}
            transform.position+=delta;life-=Time.deltaTime;if(life<=0)Recycle();
        }
        void OnDisable(){Live.Remove(this);if(counted){counted=false;ActiveCount=Mathf.Max(0,ActiveCount-1);}}
        void Recycle(){gameObject.SetActive(false);pool.Push(this);}
    }
}
