using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// Hushwell's one rule: the nursery breathes. Every vent shares one pressure
    /// cycle. On the exhale a vent membrane opens, and a floor vent scalds
    /// whoever stands in its plume, hounds and heroes alike. On the inhale
    /// membranes seal and plumes stop. The throat's glow swells before each
    /// exhale, so the next one is always readable.
    /// </summary>
    public sealed class PressurePulse:MonoBehaviour
    {
        public const float Period=6,OpenFraction=.5f;
        public Membrane vent;
        public Renderer throat;
        public float phase,hazardRadius,hazardDamage=16;
        public bool Open{get;private set;}
        bool initialized;
        /// <summary>Open during the first half of every cycle, measured on the
        /// shared game clock so every vent in the cave breathes together.</summary>
        public static bool OpenAt(float time,float phase)=>Mathf.Repeat(time/Period+phase,1)<OpenFraction;
        void Update()
        {
            bool open=OpenAt(GameTime.Now,phase);
            if(throat!=null)
            {
                float cycle=Mathf.Repeat(GameTime.Now/Period+phase,1);
                float glow=open?1:Mathf.SmoothStep(.15f,.8f,(cycle-OpenFraction)/(1-OpenFraction));
                var block=new MaterialPropertyBlock();throat.GetPropertyBlock(block);block.SetColor("_EmissionColor",new Color(.13f,.96f,1)*glow*2.2f);throat.SetPropertyBlock(block);
            }
            if(initialized&&open==Open)return;bool exhale=initialized&&open;initialized=true;Open=open;
            if(vent!=null)vent.SetOpen(open);
            if(!exhale||!Application.isPlaying)return;
            CombatVfx.Burst(transform.position+Vector3.up*1.5f,new Color(.84f,.74f,.6f),"shape");
            if(hazardRadius<=0)return;
            foreach(var health in Health.All.ToArray())
            {
                if(!health.Alive)continue;var offset=health.transform.position-transform.position;offset.y=0;
                if(offset.sqrMagnitude<=hazardRadius*hazardRadius&&Mathf.Abs(health.transform.position.y-transform.position.y)<3)
                    health.Receive(new DamagePacket{amount=hazardDamage,type=DamageType.Plasma});
            }
        }
    }
}
