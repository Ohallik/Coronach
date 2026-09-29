using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    [DefaultExecutionOrder(700)]
    public sealed class FlightEmitter:MonoBehaviour
    {
        public enum Pattern{Needle,Fan,Beam,Counter}
        CombatActor actor;FlightSockets sockets;EmitterBeam beam;Health target;
        DamagePacket packet;DamagePacket[] fan;Vector3 aim;Pattern pattern;int sequence;bool pending;
        void Awake(){actor=GetComponent<CombatActor>();}
        public void Queue(Vector3 direction,DamagePacket damage,Pattern shot=Pattern.Needle,DamagePacket[] pellets=null)
        {
            aim=direction;packet=damage;fan=pellets;pattern=shot;sequence=actor.AttackSequence;pending=true;target=actor.target;
            if(target!=null)
            {
                var offset=target.transform.position-transform.position;offset.y=0;
                if(!target.Alive||Vector3.Angle(aim,offset)>1)target=null;
            }
        }
        void LateUpdate()
        {
            if(!pending)return;
            if(!actor.flight||!actor.Health.Alive||actor.Recovering||sequence!=actor.AttackSequence){pending=false;return;}
            if(GameTime.Paused)return;
            if(sockets==null)sockets=GetComponent<FormController>().flight.GetComponent<FlightSockets>();
            if(sockets==null||sockets.muzzle==null)throw new MissingReferenceException("Flight hull lacks its calibrated muzzle: "+actor.character);
            // Sample after FlightShipMotion has posed the actual visible hull.
            pending=false;Vector3 origin=sockets.muzzle.position;
            CombatAudio.Shot(actor,origin);
            if(CombatCover.Sweep(transform.position+Vector3.up*.9f,origin,.09f,out var wall))
            {CombatVfx.Burst(wall.point,Color.cyan,"hit");return;}
            Vector3 direction=aim;
            if(target!=null&&target.Alive)
            {
                var planar=target.transform.position-transform.position;planar.y=0;
                if(Vector3.Angle(aim,planar)<30)
                {
                    var body=target.GetComponent<CapsuleCollider>();
                    var center=body!=null?body.bounds.center:target.transform.position+Vector3.up*.75f;
                    direction=(center-origin).normalized;
                }
            }
            CombatVfx.Burst(origin,actor.character=="Taren"?new Color(1,.65f,.2f):Color.cyan,"shot");
            if(pattern==Pattern.Beam||pattern==Pattern.Counter)
            {
                if(beam==null)beam=gameObject.AddComponent<EmitterBeam>();
                beam.Fire(origin,direction,packet,pattern==Pattern.Counter?16:18);
            }
            else if(pattern==Pattern.Fan)
                for(int ray=-2;ray<=2;ray++)Projectile.Fire(origin,Quaternion.AngleAxis(ray*12,Vector3.up)*direction,fan[ray+2]);
            else Projectile.Fire(origin,direction,packet);
        }
        void OnDisable(){pending=false;}
    }
}
