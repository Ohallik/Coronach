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
        public static int ActiveCount {get;private set;}
        public static void Fire(Vector3 position,Vector3 direction,DamagePacket damage,float speed=24)
        {
            Projectile p=null;while(pool.Count>0&&p==null)p=pool.Pop();
            if(p==null)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Projectile";Destroy(go.GetComponent<Collider>());
                go.transform.localScale=Vector3.one*.18f;p=go.AddComponent<Projectile>();
                go.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Blockout/Emission");
            }
            p.transform.position=position;p.packet=damage;p.velocity=direction.normalized*speed;p.life=3;
            p.gameObject.SetActive(true);ActiveCount++;
        }
        void Update()
        {
            if(Lattice.Core.GameTime.Paused)return;
            var delta=velocity*Time.deltaTime;
            foreach(var hit in Physics.SphereCastAll(transform.position,.15f,velocity.normalized,delta.magnitude,~0,QueryTriggerInteraction.Collide))
            {
                if(hit.collider.TryGetComponent<Hurtbox>(out var box))
                {
                    if(box.owner==null||box.owner==packet.source||packet.source!=null&&box.owner.friendly==packet.source.friendly)continue;
                    box.Hit(packet);Recycle();return;
                }
                if(!hit.collider.isTrigger&&hit.collider.gameObject.isStatic){Recycle();return;}
            }
            transform.position+=delta;life-=Time.deltaTime;if(life<=0)Recycle();
        }
        void Recycle(){ActiveCount=Mathf.Max(0,ActiveCount-1);gameObject.SetActive(false);pool.Push(this);}
    }
}
