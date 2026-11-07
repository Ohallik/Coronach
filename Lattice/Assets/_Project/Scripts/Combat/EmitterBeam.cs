using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    // A brief world-space trace, shared by the visible beam and its contact path.
    // Reuse one renderer per emitter; released energy survives owner cancellation.
    public sealed class EmitterBeam : MonoBehaviour
    {
        LineRenderer line,core;Material material;float until;
        readonly HashSet<Health> victims=new();
        public void Fire(Vector3 origin,Vector3 direction,DamagePacket packet,float length)
        {
            direction.Normalize();victims.Clear();
            if(line==null)
            {
                var go=new GameObject("Emitter beam");go.transform.SetParent(transform,false);
                line=go.AddComponent<LineRenderer>();line.positionCount=2;line.useWorldSpace=true;
                // A flat cyan band with a bright core, not a stretched flare with a white thread down it: nobody could tell that from an attack on the hero behind the target.
                line.startWidth=line.endWidth=.15f;line.numCapVertices=3;
                line.sharedMaterial=Resources.Load<Material>("Effects/flare_01");
                var center=new GameObject("Emitter core");center.transform.SetParent(transform,false);
                core=center.AddComponent<LineRenderer>();core.positionCount=2;core.useWorldSpace=true;
                core.startWidth=core.endWidth=.055f;core.numCapVertices=3;core.sortingOrder=1;
                material=new Material(line.sharedMaterial);material.SetTexture("_BaseMap",Texture2D.whiteTexture);core.sharedMaterial=material;line.sharedMaterial=material;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                core.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var contacts=Physics.SphereCastAll(origin,.09f,direction,length,~0,QueryTriggerInteraction.Collide);
            float distance=length;bool blocked=false;
            // Query order is unspecified. Resolve the nearest opaque surface
            // before considering any victim, including bodies behind that wall.
            foreach(var contact in contacts)
                if(CombatCover.Opaque(contact.collider)&&contact.distance<=distance)
                {distance=contact.distance;blocked=true;}
            line.SetPosition(0,origin);line.SetPosition(1,origin+direction*distance);
            core.SetPosition(0,origin);core.SetPosition(1,origin+direction*distance);
            Tint(1);line.enabled=core.enabled=true;until=GameTime.Now+.15f;
            // A body struck point blank is at no distance at all: nothing struck is what ends the beam at its full length.
            float farthest=-1;
            foreach(var contact in contacts)
            {
                if(contact.distance>distance||blocked&&contact.distance>=distance)continue;
                if(!contact.collider.TryGetComponent<Hurtbox>(out var box)||box.owner==null||!box.owner.Alive||
                    box.owner==packet.source||packet.source!=null&&box.owner.friendly==packet.source.friendly||!victims.Add(box.owner))continue;
                farthest=Mathf.Max(farthest,contact.distance);
                box.Hit(packet);
            }
            // It still strikes everything in its reach, but it looks finished at the last body it struck: run on past the boss, it pointed at the hero standing behind her.
            if(farthest>=0&&farthest+.8f<distance){var end=origin+direction*(farthest+.8f);line.SetPosition(1,end);core.SetPosition(1,end);}
        }
        void LateUpdate()
        {
            if(line==null||!line.enabled||GameTime.Paused)return;
            float remaining=until-GameTime.Now;
            if(remaining<=0){line.enabled=core.enabled=false;return;}
            Tint(Mathf.Clamp01(remaining/.15f));
        }
        void Tint(float alpha)
        {
            line.startColor=line.endColor=new Color(.1f,.8f,1.1f,.55f*alpha);
            core.startColor=core.endColor=new Color(.7f,1.7f,2.2f,alpha);
        }
        void OnDisable(){if(line!=null)line.enabled=false;if(core!=null)core.enabled=false;}
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
