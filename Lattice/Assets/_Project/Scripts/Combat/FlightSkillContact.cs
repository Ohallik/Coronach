using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    // Posed hull releases run after ship banking and before final effects.
    [DefaultExecutionOrder(720)]
    public sealed class FlightSkillContact:MonoBehaviour
    {
        public enum Release { Cleave, Pulse, Net, Overdrive, Refract }
        CombatActor actor;FlightSockets sockets;FlightEnergyArc cue;
        Release kind;DamagePacket packet;Vector3 direction,destination;
        int sequence;bool pending;float age;
        void Awake(){actor=GetComponent<CombatActor>();}
        public void Begin(Release release,Vector3 aim,DamagePacket damage)
        {
            kind=release;direction=aim.normalized;packet=damage;sequence=actor.AttackSequence;age=0;pending=true;
            float distance=actor.target!=null&&actor.target.Alive?Mathf.Min(6,Vector3.Distance(transform.position,actor.target.transform.position)):4;
            destination=transform.position+direction*distance+Vector3.up*.8f;
        }
        void OnDisable(){pending=false;if(cue!=null)cue.gameObject.SetActive(false);}
        void LateUpdate()
        {
            if(!actor.flight||!actor.Health.Alive||actor.Recovering){pending=false;Hide();return;}
            if(pending&&sequence!=actor.AttackSequence)pending=false;
            if(GameTime.Paused)return;
            if(pending)
            {
                age+=Time.deltaTime;float anticipation=kind==Release.Pulse?.18f:kind==Release.Net?.15f:.12f;
                var color=actor.character=="Taren"?new Color(1,.65f,.2f):new Color(.1f,.8f,1);
                color.a=.25f+.6f*Mathf.Clamp01(age/anticipation);
                Show(direction,kind==Release.Cleave?65:180,color);
                if(age<anticipation)return;
                pending=false;
                if(kind==Release.Cleave||kind==Release.Pulse)
                    FlightWave.Release(transform.position+Vector3.up*.8f,direction,packet,kind==Release.Cleave);
                else if(kind==Release.Overdrive)actor.ArmOverdrive();
                else if(kind==Release.Refract)actor.ArmRefract();
                else
                {
                    if(sockets==null)sockets=GetComponent<FormController>().flight.GetComponent<FlightSockets>();
                    var center=transform.position+Vector3.up*.8f;var origin=sockets.muzzle.position;
                    if(CombatCover.Sweep(center,origin,.12f,out var wall))
                    {
                        var safe=center+(origin-center).normalized*Mathf.Max(0,wall.distance-.025f);
                        NetSeed.Land(safe,packet,true);CombatVfx.Burst(wall.point,Color.cyan,"hit");
                    }
                    else NetSeed.Throw(origin,destination,packet,true);
                }
                // Sela's own launch voice: the bare laser is now hostile fire.
                if(kind==Release.Net)CombatAudio.Shot(actor,sockets!=null&&sockets.muzzle!=null?sockets.muzzle.position:transform.position);
                else AudioManager.Play("forceField_000",.18f);
            }
            if(actor.Overdriving||actor.Refracting)
                Show(actor.motor.Facing,180,actor.Refracting?new Color(.1f,.8f,1,.5f):new Color(1,.65f,.2f,.3f));
            else Hide();
        }
        void Show(Vector3 aim,float halfAngle,Color color)
        {
            if(cue==null)
            {
                cue=FlightEnergyArc.Create("Flight skill charge");cue.transform.SetParent(transform,false);
                cue.transform.localPosition=Vector3.up*.8f;
            }
            cue.gameObject.SetActive(true);cue.transform.rotation=Quaternion.LookRotation(aim,Vector3.up);
            cue.Draw(GetComponent<CharacterController>().radius,halfAngle,color,false);
        }
        void Hide(){if(cue!=null)cue.gameObject.SetActive(false);}
    }
}
