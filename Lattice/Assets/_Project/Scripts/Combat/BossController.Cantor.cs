using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Cantor (D198): a collared route-singer fought in flight. Its choir rhythm stays as built (an outer sweep, then
    /// a song whose stagger the chorus link controls; the collar links still change both). Once its integrity falls it
    /// weaves bullet song between choirs, in the manner of a top-down shooter: rotating spiral arms with gaps to fly
    /// through, and, in the last phase, a lash along a marked lane with its whole coil. These new moves follow integrity
    /// only, so cutting the collar never unlocks them early. Until they arrive, the end of its song is the opening: the head
    /// can't be hurt yet, but an exposed head makes its links take half as much again, and nothing fires while it lasts.
    /// </summary>
    public sealed partial class BossController
    {
        int cantorRhythm;

        (string,IEnumerator) ChooseCantor(CombatActor hero)
        {
            // Every other pick once it is hurt: at every third, the last phase saw one of them before it was over.
            if(HealthPhase>=2&&cantorRhythm%2==1)
            {
                cantorRhythm++;
                return HealthPhase>=3&&Random.value<.5f?("CoilLash",CoilLash()):("SongSpiral",SongSpiral());
            }
            cantorRhythm++;return("Choir",ChoirAttack());
        }

        IEnumerator ChoirAttack()
        {
            busy=true;bool song=Phase>=2&&(cantorSpecials++%2)==1;
            bool sweep=collar==null||collar.SweepAttached,chorus=collar==null||collar.ChorusAttached;
            float radius=song?(chorus?14:10):(sweep?8:5),tell=song?1.1f:.85f;strikeAt=Time.time+tell;
            Debug.Log($"CANTOR_TELL song={song} radius={radius} stagger={song&&chorus}");
            var marking=Mark(Telegraph.Circle(transform.position,radius,tell,song?Telegraph.Stagger:Telegraph.Threat));
            yield return new WaitForSeconds(tell);
            bool sung=health.Alive&&!health.Broken;
            if(sung)
            {
                marking.Release();
                foreach(var actor in PartyController.Current.members)
                    if(marking.Contains(actor.transform.position))
                    {
                        float damage=actor.Health.Receive(Packet(song?(chorus?15:10):30,song?DamageType.Pulse:DamageType.Kinetic));
                        if(song&&chorus&&damage>0)actor.Stagger(.65f);
                    }
            }
            if(sung&&HealthPhase<2)SongSpent();
            Finish(Phase==3?4:6);
        }
        /// <summary>The opening of the fight's first stages: the first 1.2 s of its rest, so the rhythm is unchanged and punishing it costs nothing.</summary>
        void SongSpent()
        {
            OpenFor(1.2f);
            if(collar==null||collar.Remaining==0)return;
            collar.Stall();
            if(!stallSaid){stallSaid=true;BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);}
        }

        IEnumerator SongSpiral()
        {
            // A short, marked inhale, then rotating arms of song: the gaps between the arms are the way through.
            float tell=.7f;var marking=Mark(Telegraph.Circle(transform.position,2.6f,tell,Telegraph.Stagger));strikeAt=Time.time+tell;
            yield return new WaitForSeconds(tell);
            if(Interrupted)yield break;
            marking.Release();strikeAt=0;
            int arms=HealthPhase>=3?4:3;float duration=1.8f,interval=.12f,spin=HealthPhase>=3?190:150;
            float heading=transform.eulerAngles.y,next=0;
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up,.2f,AudioBus.SFX,70);
            for(float t=0;t<duration;)
            {
                if(GameTime.Paused){yield return null;continue;}
                if(Interrupted)yield break;
                t+=Time.deltaTime;
                if(t>=next)
                {
                    next+=interval;
                    for(int a=0;a<arms;a++)
                    {
                        var direction=Quaternion.Euler(0,heading+spin*t+a*360f/arms,0)*Vector3.forward;
                        var packet=ProjectileProvenance.Volley(Packet(8,DamageType.Pulse));
                        Projectile.Fire(transform.position+Vector3.up,direction,packet,10);
                    }
                    if(Mathf.FloorToInt(t/interval)%3==0)CombatAudio.HostileShot(transform,transform.position+Vector3.up);
                }
                yield return null;
            }
            yield return Opening(1.2f,4);
        }

        IEnumerator CoilLash()
        {
            // The whole coil lashes along a marked lane toward the active hero.
            var controller=GetComponent<CharacterController>();
            var direction=Toward(PartyController.Current.Active.transform.position);float tell=.8f,length=22;
            var lane=Mark(Telegraph.Line(transform.position,direction,length,4.5f,tell,Telegraph.Heavy));strikeAt=Time.time+tell;
            yield return new WaitForSeconds(tell);
            if(Interrupted)yield break;
            lane.Release();strikeAt=0;
            var hit=new HashSet<Health>();float travelled=0;Collide(false);
            while(travelled<length&&!Interrupted)
            {
                if(GameTime.Paused){yield return null;continue;}
                float step=24*Time.deltaTime;var before=transform.position;
                if(controller!=null&&controller.enabled)controller.Move(direction*step);else{transform.position+=direction*step;}
                travelled+=Mathf.Max(step*.25f,Flat(transform.position-before).magnitude);
                transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),720*Time.deltaTime);
                Contact(hit,transform.position,3f,28,.35f,lane);
                yield return null;
            }
            Collide(true);yield return Opening(1.5f,4);
        }
    }
}
