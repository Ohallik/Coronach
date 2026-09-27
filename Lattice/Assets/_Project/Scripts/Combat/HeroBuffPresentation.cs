using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    // Persistent cues use the actual timed state and posed wrists, not a second
    // visual timer. The bounded particle sets freeze with the pose during pause.
    [DefaultExecutionOrder(800)]
    public sealed class HeroBuffPresentation:MonoBehaviour
    {
        CombatActor actor;FormController form;Animator rig;
        ParticleSystem charge,ward;ParticleSystemRenderer chargeRenderer,wardRenderer;
        readonly ParticleSystem.Particle[] particles=new ParticleSystem.Particle[2];
        void Start()
        {
            actor=GetComponent<CombatActor>();form=GetComponent<FormController>();
            rig=form.shaped.GetComponentInChildren<Animator>(true);
            charge=Create("Overdrive charge",out chargeRenderer);ward=Create("Refract ward",out wardRenderer);
        }
        ParticleSystem Create(string label,out ParticleSystemRenderer renderer)
        {
            var go=new GameObject(label,typeof(ParticleSystem));go.transform.SetParent(transform,false);
            var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.Local;
            main.maxParticles=2;main.simulationSpeed=0;main.startSpeed=0;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Resources.Load<Material>("Effects/circle_02");
            renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;renderer.enabled=false;return ps;
        }
        void LateUpdate()
        {
            if(rig==null||!rig.isHuman)return;
            bool shown=actor.Health.Alive&&form.Current==BodyForm.Shaped;
            Show(charge,chargeRenderer,shown&&actor.Overdriving,true);
            Show(ward,wardRenderer,shown&&actor.Refracting,false);
        }
        void Show(ParticleSystem effect,ParticleSystemRenderer renderer,bool active,bool overdrive)
        {
            renderer.enabled=active;if(!active){if(effect.particleCount>0)effect.Clear();return;}
            int count=overdrive?2:1;
            for(int i=0;i<count;i++)
            {
                var hand=rig.GetBoneTransform(i==0?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
                particles[i].position=transform.InverseTransformPoint(hand.position);
                particles[i].startSize=overdrive?.65f:1.1f;
                particles[i].startColor=overdrive?new Color(1,.65f,.15f,.7f):new Color(.15f,.85f,1,.75f);
                particles[i].startLifetime=particles[i].remainingLifetime=1000;
            }
            effect.SetParticles(particles,count);
        }
        public void Intercept()
        {
            if(rig!=null&&rig.isHuman)CombatVfx.Burst(rig.GetBoneTransform(HumanBodyBones.LeftHand).position,Color.cyan,"hit");
        }
    }
}
