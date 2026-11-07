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
        bool counted,hasFaction,friendly,marked;
        public static int ActiveCount {get;private set;}
        /// <summary>Shots in the air, so AI can read incoming fire as a player does.</summary>
        public static readonly HashSet<Projectile> Live=new();
        public Vector3 Velocity=>velocity;
        static readonly List<Projectile> fizzling=new();
        /// <summary>Whoever fired these is out of the fight: their shots burn out where they are.</summary>
        public static void Fizzle(Health source)
        {
            fizzling.Clear();
            foreach(var shot in Live)if(shot.packet.source==source)fizzling.Add(shot);
            foreach(var shot in fizzling){CombatVfx.Burst(shot.transform.position,new Color(1,.45f,.15f),"spark");shot.Recycle();}
            fizzling.Clear();
        }
        /// <summary>Would this shot harm a body on the given side?</summary>
        public bool Threatens(bool friendlySide)=>hasFaction&&friendly!=friendlySide;
        Renderer view;TrailRenderer trail;
        static Material friendlyShot,hostileShot,partyShot;
        static Material Shots(bool hostile)
        {
            friendlyShot??=Resources.Load<Material>("Blockout/Emission");
            if(hostileShot==null&&friendlyShot!=null)
            {
                hostileShot=new Material(friendlyShot){name="hostile shot (runtime)"};
                var hot=new Color(1,.34f,.12f);
                if(hostileShot.HasProperty("_BaseColor"))hostileShot.SetColor("_BaseColor",hot);
                if(hostileShot.HasProperty("_Color"))hostileShot.SetColor("_Color",hot);
                if(hostileShot.HasProperty("_EmissionColor"))hostileShot.SetColor("_EmissionColor",hot*2.4f);
            }
            // The shared Emission burns out to plain white under the grade: a white ball in the air, nobody's in particular.
            if(partyShot==null&&friendlyShot!=null)
            {
                partyShot=new Material(friendlyShot){name="party shot (runtime)"};
                var cool=new Color(.2f,.85f,1);
                if(partyShot.HasProperty("_BaseColor"))partyShot.SetColor("_BaseColor",cool);
                if(partyShot.HasProperty("_Color"))partyShot.SetColor("_Color",cool);
                if(partyShot.HasProperty("_EmissionColor"))partyShot.SetColor("_EmissionColor",cool*1.6f);
            }
            return hostile?hostileShot:partyShot;
        }
        /// <param name="reach">How far a marked shot flies: it ends where its marking ends. Unmarked shots fly their three seconds.</param>
        public static void Fire(Vector3 position,Vector3 direction,DamagePacket damage,float speed=24,float reach=0)
        {
            Projectile p=null;while(pool.Count>0&&p==null)p=pool.Pop();
            if(p==null)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Projectile";go.transform.position=position;Destroy(go.GetComponent<Collider>());
                p=go.AddComponent<Projectile>();p.view=go.GetComponent<Renderer>();p.view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                p.trail=go.AddComponent<TrailRenderer>();p.trail.time=.12f;p.trail.startWidth=.26f;p.trail.endWidth=0;p.trail.minVertexDistance=.2f;
                p.trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;p.trail.sharedMaterial=Shots(true);
            }
            // Their shots read as theirs: bigger, hot and trailing, against the party's small cyan needles.
            bool hostile=damage.source!=null&&!damage.source.friendly;
            p.transform.localScale=Vector3.one*(hostile?.36f:.18f);p.view.sharedMaterial=Shots(hostile);
            // Theirs a hot trail, the party's a thin cyan streak: a shot says whose it is.
            p.trail.sharedMaterial=p.view.sharedMaterial;p.trail.startWidth=hostile?.26f:.12f;
            p.trail.enabled=true;
            p.transform.position=position;p.velocity=direction.normalized*speed;p.life=reach>0?reach/speed:3;p.marked=reach>0;
            damage.projectileOrigin=ProjectileProvenance.StampLaunch(damage,position,p.velocity,p.GetInstanceID());p.packet=damage;
            p.hasFaction=damage.source!=null;p.friendly=p.hasFaction&&damage.source.friendly;
            p.gameObject.SetActive(true);p.counted=true;ActiveCount++;Live.Add(p);
            // Only now, where it is and awake. Cleared before it moved, the trail kept the last spot it stood on, the world's origin for a new shot or where the last one died, and for its .12 s a
            // long bright line ran from there to the muzzle: the brightest thing on screen, through the shooter and off the edge of it, kinked where the real trail began.
            p.trail.Clear();
        }
        void Update()
        {
            if(Lattice.Core.GameTime.CombatHeld)return;
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
            transform.position+=delta;life-=Time.deltaTime;
            // A marked shot ends at its marking's tip, on screen, and long after the paint has faded: it burns out rather than vanishing in a frame.
            if(life<=0){if(marked)CombatVfx.Burst(transform.position,new Color(1,.45f,.15f),"spark");Recycle();}
        }
        void OnDisable(){Live.Remove(this);if(counted){counted=false;ActiveCount=Mathf.Max(0,ActiveCount-1);}}
        void Recycle(){gameObject.SetActive(false);pool.Push(this);}
    }
}
