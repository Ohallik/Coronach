using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>A quiet attached ward persists for the actual protective state.</summary>
    [DefaultExecutionOrder(800)]
    public sealed class EnemyArmorPresentation : MonoBehaviour
    {
        Health health;Transform chest;
        ParticleSystem ward;ParticleSystemRenderer wardRenderer;
        readonly ParticleSystem.Particle[] particle=new ParticleSystem.Particle[1];
        void Start()
        {
            health=GetComponent<Health>();var rig=GetComponentInChildren<Animator>();
            if(rig!=null&&rig.isHuman)chest=rig.GetBoneTransform(HumanBodyBones.Chest);
            var go=new GameObject("Armor ward",typeof(ParticleSystem));go.transform.SetParent(transform,false);
            ward=go.GetComponent<ParticleSystem>();ward.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ward.main;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.Local;
            main.maxParticles=1;main.simulationSpeed=0;main.startSpeed=0;
            var emission=ward.emission;emission.enabled=false;var shape=ward.shape;shape.enabled=false;
            wardRenderer=ward.GetComponent<ParticleSystemRenderer>();wardRenderer.sharedMaterial=Resources.Load<Material>("Effects/circle_02");
            wardRenderer.renderMode=ParticleSystemRenderMode.Billboard;wardRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            wardRenderer.receiveShadows=false;wardRenderer.enabled=false;
        }
        void LateUpdate()
        {
            if(ward==null)return;
            bool active=health.Alive&&!health.Broken&&Time.time<health.ShieldUntil;
            if(active!=wardRenderer.enabled)Debug.Log((active?"ARMOR_UP ":"ARMOR_DOWN ")+health.id);
            wardRenderer.enabled=active;
            if(!active){if(ward.particleCount>0)ward.Clear();return;}
            particle[0].position=chest!=null?transform.InverseTransformPoint(chest.position):Vector3.up;
            particle[0].startSize=2.4f;particle[0].startColor=new Color(.25f,.85f,1,.34f);
            particle[0].startLifetime=particle[0].remainingLifetime=1000;
            ward.SetParticles(particle,1);
        }
    }
}
