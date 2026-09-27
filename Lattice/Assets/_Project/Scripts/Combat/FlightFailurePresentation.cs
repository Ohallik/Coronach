using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    // A disabled hull remains on its flight plane with power shut down. The
    // settling attitude and rescue signal are independent of its live motor.
    public sealed class FlightFailurePresentation:MonoBehaviour
    {
        CombatActor actor;Transform hull;FormFold panels;FlightEnergyArc signal;
        Vector3 fromPosition;Quaternion fromRotation;float fromFold,fold,side;
        public void Begin(CombatActor owner,Transform body,DamagePacket packet)
        {
            actor=owner;hull=body;fromPosition=hull.localPosition;fromRotation=hull.localRotation;
            side=packet.source!=null?Mathf.Sign(Vector3.Dot(transform.right,transform.position-packet.source.transform.position)):actor.character=="Taren"?1:-1;
            if(side==0)side=1;
            panels=hull.GetComponent<FormFold>()??hull.gameObject.AddComponent<FormFold>();panels.Prepare(true);fold=0;
            if(signal==null)
            {
                signal=FlightEnergyArc.Create("Flight rescue signal");signal.transform.SetParent(transform,false);
                signal.transform.localPosition=Vector3.up*.8f;
            }
            signal.GetComponent<LineRenderer>().enabled=true;
            var sockets=hull.GetComponent<FlightSockets>();
            if(sockets!=null)foreach(var engine in sockets.engines)CombatVfx.Burst(engine.position,new Color(1,.6f,.18f),"failure");
        }
        public void BeginRecovery()
        {fromPosition=hull.localPosition;fromRotation=hull.localRotation;fromFold=fold;}
        public void Advance(float elapsed,bool recovering)
        {
            if(!actor.flight)
            {panels.Restore();signal.GetComponent<LineRenderer>().enabled=false;return;}
            if(!signal.GetComponent<LineRenderer>().enabled)
            {panels.Prepare(true);signal.GetComponent<LineRenderer>().enabled=true;}
            if(GameTime.Paused)return;
            if(recovering)
            {
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/CombatActor.ReviveDuration));
                hull.localPosition=Vector3.Lerp(fromPosition,Vector3.up*.8f,t);hull.localRotation=Quaternion.Slerp(fromRotation,Quaternion.identity,t);
                fold=fromFold*(1-t);panels.Apply(fold);
                signal.Draw(HeroCollision.HullRadius(actor.character)+.12f+.2f*t,180,new Color(.2f,1,.85f,.8f*(1-t)),false);
                return;
            }
            float phase=Mathf.Clamp01(elapsed/CombatActor.DownDuration);
            // Brief opposite torque, then a weighted roll that settles without
            // spinning the craft or letting its root fall out of the map.
            float settle=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.82f,phase));
            float kick=Mathf.Sin(Mathf.Clamp01(phase/.28f)*Mathf.PI)*9;
            float recoil=Mathf.Sin(Mathf.Clamp01(phase/.78f)*Mathf.PI)*6;
            float bank=actor.character=="Taren"?21:24;
            hull.localRotation=Quaternion.Slerp(fromRotation,Quaternion.Euler(8,0,side*bank),settle)*Quaternion.Euler(recoil,0,-side*kick);
            hull.localPosition=Vector3.Lerp(fromPosition,Vector3.up*.72f,settle);fold=.22f*settle;panels.Apply(fold);
            bool assisting=PartyController.Current!=null&&PartyController.Current.ReviveTarget==actor;
            float pulse=.5f+.2f*Mathf.Sin(GameTime.Now*Mathf.PI*2);
            var color=assisting?new Color(.2f,1,.85f,pulse):new Color(1,.65f,.2f,pulse);
            signal.Draw(HeroCollision.HullRadius(actor.character)+.12f,180,color,false);
        }
        public void Finish()
        {
            panels.Restore();fold=0;hull.localPosition=Vector3.up*.8f;hull.localRotation=Quaternion.identity;
            signal.GetComponent<LineRenderer>().enabled=false;
        }
    }
}
