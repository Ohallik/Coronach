using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The lobber (the Sporecaster; later the Rot-pod, the Muster Mortar, the Slag Gull): it keeps its distance and throws. Each throw marks the circle it will land on for as long
    /// as it is in the air, strikes who is still in it, and leaves the circle behind as a <see cref="LingeringArea"/>: a cloud that slows, slag that burns, a haze. It backs away
    /// from a hero who closes and it is slow, so the answer is to step out of the circle toward it. A lob goes over what a shot stops at, and its landing stops at cover like any
    /// other hit. Reads: attacks[0] (telegraph is the time in the air, range, radius of the landing, damage, cooldown), tune.linger (seconds the area stays, none by default),
    /// area (its radius, the landing's by default), effect, pace (a Slow's share of pace, .6), tick (a Burn's damage a second). With <c>speed</c> 0 on the definition it is a turret.
    /// <para><b>A shell</b> (<c>tune.guard</c> over zero: chapter 4's Rot-pod): struck, it shuts, drops what it was throwing and lets only <c>guard</c> of each blow through (read as
    /// resisted) for <c>tune.recover</c> seconds (2.2); as it opens again it is soft for <see cref="Reopened"/> seconds, an opening, and a blow then doesn't shut it. So a hit is a
    /// question and the reopening is the answer: strike, wait, punish.</para>
    /// </summary>
    public sealed class EnemyLobber:EnemyBehaviour
    {
        Transform pod;Vector3 from;
        static readonly Color PodTone=new(.7f,1.1f,.5f,.9f);
        float Landing=>attack!=null&&attack.radius>0?attack.radius:2.2f;
        /// <summary>How long a shell is soft as it opens again.</summary>
        public const float Reopened=1.2f;
        float shutUntil,softUntil;bool shut;
        bool Shelled=>tune.guard>0;
        /// <summary>Shut against a blow: what a check reads.</summary>
        public bool Shut=>shut;
        protected override void Begin(){if(Shelled)health.Plate=_=>shut?Mathf.Clamp01(tune.guard):1;}
        public override void Struck(DamagePacket packet,float amount)
        {
            // Only awake: a body on show before its fight, or held for a scene, takes a blow as it stands.
            if(!Shelled||shut||amount<=0||Time.time<softUntil||!health.Alive||brain.Passive)return;
            shut=true;shutUntil=Time.time+Or(tune.recover,2.2f);
            if(brain.Telegraphing)brain.Interrupt();else Drop();
            Posing=true;Swell=-.2f;Pitch=0;Sound("doorOpen_000",.18f,.6f);Debug.Log("LOBBER_SHUT "+def.id);
        }
        public override void Tick(CombatActor victim)
        {
            if(shut)
            {
                if(Time.time<shutUntil){Posing=true;Swell=-.2f;return;}
                // Opening again: soft for a beat. Open stands it still (the brain doesn't tick a body that is open), so it throws nothing meanwhile.
                shut=false;softUntil=Time.time+Reopened;Posing=false;Swell=0;Open(Reopened);Sound("doorOpen_000",.18f,1.2f);Debug.Log("LOBBER_OPENS "+def.id);
                return;
            }
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;
            if(brain.Telegraphing)
            {
                // It fills as it gathers the throw and the pod is in the air for the rest: what's marked is where it comes down.
                var mark=brain.AttackMark;float k=brain.TellProgress;Posing=true;Pitch=-10*k;Swell=.16f*Mathf.Sin(Mathf.Clamp01(k*2.5f)*Mathf.PI*.5f);
                if(pod!=null&&mark!=null)pod.position=Vector3.Lerp(from,mark.Origin,k)+Vector3.up*(Mathf.Sin(k*Mathf.PI)*(Flying?0:4.5f));
                if(!brain.TellRunOut)return;
                mark=brain.EndTell();Drop();
                if(mark==null)return;
                var at=mark.Origin;StrikeIn(mark,brain.Blow(attack,HitWeight.Medium),at+Vector3.up*.6f);mark.Release();
                CombatVfx.Burst(at+Vector3.up*.4f,PodTone,"shape");AudioManager.PlayAt("water_splash",null,at,.22f,AudioBus.SFX,110,.08f,.8f);
                if(tune.linger>0)Leave(LingeringArea.Lay(at,Or(tune.area,Landing),tune.linger,tune.effect,Or(tune.pace,.6f),tune.tick,health,attack!=null?attack.type:DamageType.Kinetic));
                brain.Rest(Cooldown);Posing=false;Swell=0;
                return;
            }
            Posing=false;Swell=0;
            if(distance<Range&&brain.Rested&&brain.Sees(victim))
            {
                var at=Flying?victim.transform.position:CombatCover.Ground(victim.transform.position+Vector3.up);
                if(distance>.01f)body.rotation=Quaternion.LookRotation(to);
                Mark(Telegraph.Circle(at,Landing,Tell,Telegraph.Threat,true),Tell,to,victim,"forceField_000",.62f,true);
                from=body.position+Vector3.up*(controller!=null?controller.height:1.5f);
                pod=GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;pod.name="Lobbed pod";Object.Destroy(pod.GetComponent<Collider>());
                pod.localScale=Vector3.one*.55f;pod.position=from;pod.GetComponent<Renderer>().sharedMaterial=ActorFactory.Energy(PodTone);
                return;
            }
            brain.Spread(to,distance,Range);
            if(distance>.01f)body.rotation=Quaternion.LookRotation(to);
            // It holds the far end of its reach, and gives ground at walking pace to a hero who comes for it: slow enough to be caught.
            if(distance>Range*.85f)brain.Walk(to,def.speed);else if(distance<Range*.45f)brain.Walk(-to,def.speed*.8f);
        }
        void Drop(){if(pod!=null)Object.Destroy(pod.gameObject);pod=null;}
        /// <summary>Carried by a current, the throw starts from where the body has been carried to: the pod in the air leaves its hand, not the place it stood.</summary>
        public override void Carried(Vector3 delta)=>from+=delta;
        public override void Cancel(){Drop();if(!shut){Posing=false;Swell=0;}}
        /// <summary>Its body gone from the scene, what it threw goes with it (a cloud, burning slag).</summary>
        public override void Dispose(){Drop();TakeUp();}
    }
}
