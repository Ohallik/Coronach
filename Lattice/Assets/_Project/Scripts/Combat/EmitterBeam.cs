using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    // A brief world-space trace, shared by the visible beam and its contact path.
    // Reuse one renderer per emitter; released energy survives owner cancellation.
    public sealed class EmitterBeam : MonoBehaviour
    {
        LineRenderer line;Material material;float until;
        readonly HashSet<Health> victims=new();
        public void Fire(Vector3 origin,Vector3 direction,DamagePacket packet,float length)
        {
            direction.Normalize();victims.Clear();
            if(line==null)
            {
                var go=new GameObject("Emitter beam");go.transform.SetParent(transform,false);
                line=go.AddComponent<LineRenderer>();line.positionCount=2;line.useWorldSpace=true;
                line.startWidth=line.endWidth=.18f;line.numCapVertices=3;
                material=new Material(Resources.Load<Material>("Effects/flare_01"));
                material.SetTexture("_BaseMap",Texture2D.whiteTexture);line.sharedMaterial=material;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var contacts=Physics.SphereCastAll(origin,.09f,direction,length,~0,QueryTriggerInteraction.Collide);
            float distance=length;bool blocked=false;
            // Query order is unspecified. Resolve the nearest opaque surface
            // before considering any victim, including bodies behind that wall.
            foreach(var contact in contacts)
                if(!contact.collider.isTrigger&&contact.collider.gameObject.isStatic&&
                    !contact.collider.TryGetComponent<Hurtbox>(out _)&&contact.distance<=distance)
                {distance=contact.distance;blocked=true;}
            line.SetPosition(0,origin);line.SetPosition(1,origin+direction*distance);
            line.startColor=line.endColor=new Color(.12f,.85f,1,1);line.enabled=true;until=GameTime.Now+.15f;
            foreach(var contact in contacts)
            {
                if(contact.distance>distance||blocked&&contact.distance>=distance)continue;
                if(!contact.collider.TryGetComponent<Hurtbox>(out var box)||box.owner==null||!box.owner.Alive||
                    box.owner==packet.source||packet.source!=null&&box.owner.friendly==packet.source.friendly||!victims.Add(box.owner))continue;
                box.Hit(packet);
            }
        }
        void LateUpdate()
        {
            if(line==null||!line.enabled||GameTime.Paused)return;
            float remaining=until-GameTime.Now;
            if(remaining<=0){line.enabled=false;return;}
            line.startColor=line.endColor=new Color(.12f,.85f,1,Mathf.Clamp01(remaining/.15f));
        }
        void OnDisable(){if(line!=null)line.enabled=false;}
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
