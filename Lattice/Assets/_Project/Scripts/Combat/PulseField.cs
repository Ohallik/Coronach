using UnityEngine;
namespace Lattice.Combat
{
    public sealed class PulseField:MonoBehaviour
    {
        public DamagePacket packet;
        public float radius=4,life=4;
        float nextPulse;
        void Start()
        {
            var field=ActorFactory.Visual("NetField",PrimitiveType.Quad,transform,new Vector3(radius*2,radius*2,1),Vector3.up*.1f,"Emission");
            field.transform.localRotation=Quaternion.Euler(90,0,0);
            var renderer=field.GetComponent<Renderer>();renderer.sharedMaterial=Resources.Load<Material>("Effects/circle_02");
            var tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",new Color(.12f,.85f,1,.8f));renderer.SetPropertyBlock(tint);
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        void Update()
        {
            if(Lattice.Core.GameTime.Paused)return;
            life-=Time.deltaTime;if(life<=0){Destroy(gameObject);return;}
            if(Time.time<nextPulse)return;nextPulse=Time.time+1;
            foreach(var health in Health.All.ToArray())
            {
                if(health==null||health.friendly||!health.Alive||(health.transform.position-transform.position).sqrMagnitude>radius*radius)continue;
                health.SlowUntil=Time.time+1.2f;health.Receive(packet);
            }
        }
    }
}
