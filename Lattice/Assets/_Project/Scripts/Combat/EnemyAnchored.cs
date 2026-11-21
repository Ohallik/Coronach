using System;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The anchored body (the Snagvine Knot; later the Flak Gate, the Rot-pod's root): fixed where it was set. Nothing drags, reels or shoves it, and it never steps. It strikes
    /// whoever comes inside its reach, on its own rhythm: a lash across a marked fan (or all round it, with an arc of 360), or, if its attack says <c>projectile</c>, a shot down a
    /// marked line. A blade lunge that lands on it in the middle of its tell takes the tell from it, as a lunge has always been the safe way through a Shellmine. And what it holds
    /// is freed when it dies: <see cref="Freed"/> says which body let go and where, for a barge, a door or a curtain to answer.
    /// Reads: attacks[0] (telegraph, range is its reach, damage, cooldown is its rhythm, projectile), tune.arc (the lash's fan, 120 degrees).
    /// Give it the Mine archetype: that is what makes the rest of the game treat it as a thing that holds its ground.
    /// <para><b>A curtain</b> (<c>tune.count</c> over nought: chapter 8's Flak Gate) is <c>EnemyAnchored.Curtain.cs</c>.</para>
    /// <para><b>On a tether</b> (<c>tune.tethered</c>: chapter 9's Bridle Fragment) is <c>EnemyAnchored.Tether.cs</c>: it never turns, and thrashes its fan along the way it was set facing,
    /// away from what it is made fast to, whoever is where.</para>
    /// </summary>
    public sealed partial class EnemyAnchored:EnemyBehaviour
    {
        /// <summary>An anchored body has died: the brain it was, and where it held.</summary>
        public static event Action<EnemyBrain,Vector3> Freed;
        /// <summary>How long a lunge that lands mid-tell keeps it from starting another.</summary>
        public const float LungeStuns=1.4f;
        float Arc=>Or(tune.arc,120);
        public override bool Movable=>false;
        public override bool Roams=>false;
        public override void Tick(CombatActor victim)
        {
            if(Curtained){Curtain(victim);return;}
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;
            if(brain.Telegraphing)
            {
                if(!brain.TellRunOut)return;
                var mark=brain.EndTell();brain.Rest(Cooldown);if(mark==null)return;
                var packet=brain.Blow(attack,HitWeight.Medium);
                if(attack!=null&&attack.projectile)
                {
                    packet=ProjectileProvenance.Volley(packet);CombatAudio.HostileShot(body,Chest);
                    mark.Carry(new[]{Projectile.Fire(Chest,brain.Aim,packet,11,Mathf.Max(13,Range))});
                }
                else{StrikeIn(mark,packet,Chest);mark.Release();Sound("swing_0",.2f,.8f);}
                return;
            }
            if(Tethered){Thrash(victim);return;}
            if(distance>=Range||!brain.Rested||!brain.Sees(victim))return;
            var aim=distance>.01f?to/distance:body.forward;body.rotation=Quaternion.LookRotation(aim);
            var marking=attack!=null&&attack.projectile?Telegraph.Line(body.position,aim,Mathf.Max(13,Range),1.1f,Tell,Telegraph.Threat,true):
                Arc>=359?Telegraph.Circle(body.position,Range,Tell,Telegraph.Threat,true):Telegraph.Cone(body.position,aim,Range,Arc,Tell,Telegraph.Threat,true);
            Mark(marking,Tell,aim,victim,"root_groan",1.5f,attack!=null&&attack.projectile);
        }
        public override void Struck(DamagePacket packet,float amount)
        {
            if(packet.tag!="lunge"||!brain.Telegraphing)return;
            brain.Interrupt();brain.Rest(LungeStuns);Debug.Log("ANCHOR_LUNGED "+def.id);
        }
        public override void Fell(DamagePacket packet){Unmark();Debug.Log("ANCHOR_FREED "+def.id);Freed?.Invoke(brain,body.position);}
        public override void Cancel()=>Unmark();
        public override void Dispose()=>Unmark();
    }
}
