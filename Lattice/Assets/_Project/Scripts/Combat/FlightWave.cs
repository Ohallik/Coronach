using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    public sealed class FlightWave:MonoBehaviour
    {
        readonly HashSet<Health> victims=new();
        DamagePacket packet;FlightEnergyArc edge;bool cleave,hasFaction,friendly;float age,previousRadius;
        public static void Release(Vector3 origin,Vector3 direction,DamagePacket damage,bool forwardArc)
        {
            var go=new GameObject(forwardArc?"Flight cleave":"Flight pulse",typeof(FlightWave));
            go.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(direction,Vector3.up));
            var wave=go.GetComponent<FlightWave>();wave.packet=damage;wave.cleave=forwardArc;
            wave.hasFaction=damage.source!=null;wave.friendly=wave.hasFaction&&damage.source.friendly;
            wave.edge=FlightEnergyArc.Create("Flight skill edge");wave.edge.transform.SetParent(go.transform,false);
            wave.edge.Draw(0,forwardArc?65:180,Color.clear,true);
        }
        void Update()
        {
            if(GameTime.Paused)return;
            age+=Time.deltaTime;float radius=(cleave?5.4f:5)*Mathf.Clamp01(age/(cleave?.24f:.28f));
            edge.Draw(radius,cleave?65:180,new Color(1,.65f,.2f,Mathf.Clamp01((.4f-age)/.12f)),true);
            foreach(var health in Health.All.ToArray())
            {
                if(health==null||!health.Alive||health==packet.source||hasFaction&&health.friendly==friendly||victims.Contains(health))continue;
                var body=health.GetComponent<CapsuleCollider>();
                var point=body!=null?body.ClosestPoint(transform.position):health.transform.position+Vector3.up*.75f;
                var offset=point-transform.position;if(Mathf.Abs(offset.y)>.6f)continue;offset.y=0;
                // A released wave cannot leave a filled invisible damage disc
                // behind its travelling edge during the fade-out.
                var center=body!=null?body.bounds.center:point;var radial=center-transform.position;radial.y=0;
                float bodyRadius=body!=null?body.radius*Mathf.Max(body.transform.lossyScale.x,body.transform.lossyScale.z):0;
                if(radial.magnitude+bodyRadius<previousRadius)continue;
                if(offset.sqrMagnitude>radius*radius||cleave&&Vector3.Angle(transform.forward,offset)>65||!CombatCover.Clear(transform.position,point))continue;
                victims.Add(health);health.Receive(packet);
            }
            previousRadius=radius;
            if(age>=.4f)Destroy(gameObject);
        }
    }
}
