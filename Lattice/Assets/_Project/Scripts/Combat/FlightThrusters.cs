using Lattice.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.Combat
{
    // Reusable local-space exhaust follows the banked generated hull. No
    // world-space particles are spawned for ordinary thrust or inertial drift.
    [DefaultExecutionOrder(800)]
    public sealed class FlightThrusters:MonoBehaviour
    {
        CombatActor actor;FlightMotor motor;FlightSockets sockets;
        LineRenderer[] flames;float drive;
        void Awake(){actor=GetComponent<CombatActor>();motor=GetComponent<FlightMotor>();}
        void Prepare()
        {
            sockets=GetComponent<FormController>().flight.GetComponent<FlightSockets>();
            if(sockets==null||sockets.engines==null||sockets.engines.Length==0)
                throw new MissingReferenceException("Flight hull lacks calibrated engines: "+actor.character);
            var material=Resources.Load<Material>("Effects/flare_01");
            flames=new LineRenderer[sockets.engines.Length];
            for(int i=0;i<flames.Length;i++)
            {
                var go=new GameObject("Flight exhaust",typeof(LineRenderer));go.transform.SetParent(sockets.engines[i],false);
                var line=flames[i]=go.GetComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=2;
                line.sharedMaterial=material;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
                line.textureMode=LineTextureMode.Stretch;line.numCapVertices=3;line.SetPosition(0,Vector3.zero);line.enabled=false;
            }
        }
        void LateUpdate()
        {
            if(!actor.flight||!actor.Health.Alive||actor.Recovering){Clear();return;}
            if(GameTime.Paused)return;
            if(flames==null)Prepare();
            drive=Mathf.MoveTowards(drive,motor.EngineDrive,Time.deltaTime*14);
            Color color=actor.character=="Taren"?new Color(1,.48f,.12f,1):new Color(.12f,.8f,1,1);
            float shimmer=1+Mathf.Sin(GameTime.Now*39)*.035f;
            for(int i=0;i<flames.Length;i++)
            {
                var line=flames[i];line.enabled=drive>.015f;
                line.SetPosition(1,Vector3.forward*((.25f+drive*.65f)*shimmer));
                line.startWidth=.12f+drive*.035f;line.endWidth=.012f;
                line.startColor=Color.Lerp(Color.white,color,.35f);line.endColor=new Color(color.r,color.g,color.b,0);
            }
        }
        void Clear(){drive=0;if(flames!=null)foreach(var line in flames)if(line!=null)line.enabled=false;}
        void OnDisable(){Clear();}
    }
}
