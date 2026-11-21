using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// One of the two shield relays that feed Meret Venn's dome in the Continuance's flight berth (chapter 10, phase one: <c>BossController.MeretShip.cs</c>): a mast on the hull's back
    /// in front of each shelter wing, its head at the plane, a cyan beam from it to her dome. While her dome is up a lit relay can be locked and shot (a shot at it is never given back:
    /// it stands well outside the dome); shot through, it goes dark, her dome falls for a window and the relay recharges. So the fight under her dome is the read and the shot at a
    /// relay, not holding fire. Made by her Kit at her station, as her clamps are when the room stands none; nothing of it is solid. Logs <c>MERET_RELAY_DOWN</c>, <c>MERET_RELAY_LIT</c>.
    /// </summary>
    public sealed class MeretRelay:MonoBehaviour
    {
        public static readonly List<MeretRelay> All=new();
        /// <summary>Whose dome it feeds, which it is (0 the west, 1 the east), and its own small bar (not hers: shooting it takes nothing off her bar).</summary>
        public BossController boss;public int index;public Health health;
        /// <summary>It is lit, takes blows and can be locked: her dome stands, and it isn't recharging.</summary>
        public bool Lit{get;private set;}
        public float RechargedAt{get;set;}=-1;
        internal SphereCollider shape;internal Renderer head;internal LineRenderer beam;
        static readonly Color LitTone=new(.45f,1.45f,1.65f,.95f),DarkTone=new(.18f,.22f,.26f,.9f);
        public void Light(bool lit,Vector3 dome)
        {
            if(health!=null){health.Damageable=lit;}
            if(shape!=null)shape.enabled=lit;
            if(head!=null){head.sharedMaterial=ActorFactory.Energy(lit?LitTone:DarkTone);head.transform.localScale=Vector3.one*(lit?1.25f+.12f*Mathf.Sin(Time.time*6+index):1.05f);}
            if(beam!=null){beam.enabled=lit;if(lit){beam.SetPosition(0,transform.position);beam.SetPosition(1,dome);}}
            if(lit!=Lit){Lit=lit;Debug.Log((lit?"MERET_RELAY_LIT ":"MERET_RELAY_DARK ")+index);}
        }
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);}
    }
}
