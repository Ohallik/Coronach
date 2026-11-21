using System;
using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Break targets a boss carries on its own body: the Seedthane's planted legs, and later a hull's feet, an arm's collar, a pour-arm. Each part is something to hit with
    /// integrity of its own, set round the body where the model has it; it shares the boss's break and its tells (<see cref="Health.reactionOwner"/>) but not its integrity or
    /// its death, as a collar link does. What breaking one does is the boss's to say: it hears <see cref="Broke"/>. A part shows as a knot of light where its hurtbox is, so the
    /// target is the thing the player can see; while <see cref="Expose"/> has them shut, they can't be hit and the knots are dark. Built from the boss's Kit.start.
    /// </summary>
    public sealed class BossParts:MonoBehaviour
    {
        /// <summary>A part has just broken: which set, and its number in it.</summary>
        public event Action<BossParts,int> Broke;
        readonly List<Health> parts=new();readonly List<Renderer> knots=new();
        public IReadOnlyList<Health> Parts=>parts;
        public bool Exposed{get;private set;}=true;
        public int Standing{get{int count=0;foreach(var part in parts)if(part!=null&&part.Alive)count++;return count;}}
        /// <param name="label">What the target card calls one ("Seedthane - leg").</param>
        /// <param name="ring">How far from the body's middle they stand, and <paramref name="turn"/> where the first is, in degrees from its forward.</param>
        public void Build(Health owner,string label,int count,float ring,float height,float radius,float integrity,float turn,Color tone)
        {
            for(int i=0;i<count;i++)
            {
                var node=new GameObject(label+" "+(i+1));node.transform.SetParent(transform,false);
                node.transform.localPosition=Quaternion.Euler(0,turn+i*360f/count,0)*Vector3.forward*ring+Vector3.up*height;
                var part=node.AddComponent<Health>();part.id=label;part.maximum=part.integrity=integrity;part.weakness=owner.weakness;part.resistance=owner.resistance;
                part.breakThreshold=owner.breakThreshold;part.reactionOwner=owner;
                var shape=node.AddComponent<SphereCollider>();shape.isTrigger=true;shape.radius=radius;
                node.AddComponent<Hurtbox>().owner=part;
                var knot=GameObject.CreatePrimitive(PrimitiveType.Sphere);knot.name="Knot";Destroy(knot.GetComponent<Collider>());knot.transform.SetParent(node.transform,false);
                knot.transform.localScale=Vector3.one*(radius*.9f);var view=knot.GetComponent<Renderer>();view.sharedMaterial=ActorFactory.Energy(tone);
                view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view.receiveShadows=false;
                int index=parts.Count;parts.Add(part);knots.Add(view);
                part.Died+=(_,__)=>
                {
                    shape.enabled=false;view.enabled=false;
                    CombatVfx.Burst(node.transform.position,tone,"crack");
                    Debug.Log($"PART_BROKEN {label} standing={Standing}");
                    Broke?.Invoke(this,index);
                };
            }
        }
        /// <summary>Whether blows land on the parts still standing: a boss opens them for the phase they belong to.</summary>
        public void Expose(bool open)
        {
            Exposed=open;
            for(int i=0;i<parts.Count;i++)
            {
                if(parts[i]==null||!parts[i].Alive)continue;
                parts[i].Damageable=open;if(knots[i]!=null)knots[i].enabled=open;
                if(parts[i].TryGetComponent<Collider>(out var shape))shape.enabled=open;
            }
        }
        /// <summary>One part opened or shut and the others left as they are: a frame that plants one foot at a time (chapter 5's Hulljack) opens only the foot it planted.</summary>
        public void Expose(int index,bool open)
        {
            if(index<0||index>=parts.Count||parts[index]==null||!parts[index].Alive)return;
            parts[index].Damageable=open;if(knots[index]!=null)knots[index].enabled=open;
            if(parts[index].TryGetComponent<Collider>(out var shape))shape.enabled=open;
            Exposed=false;foreach(var part in parts)if(part!=null&&part.Alive&&part.Damageable)Exposed=true;
        }
        /// <summary>A part gone without a blow and without <see cref="Broke"/>: the boss has lost it some other way (its bar crossed the line that part held), so nothing pays for it twice.</summary>
        public void Settle(int index)
        {
            if(index<0||index>=parts.Count||parts[index]==null||!parts[index].Alive)return;
            parts[index].integrity=0;parts[index].Damageable=false;if(knots[index]!=null)knots[index].enabled=false;
            if(parts[index].TryGetComponent<Collider>(out var shape))shape.enabled=false;
            Debug.Log($"PART_SETTLED {parts[index].id} standing={Standing}");
        }
    }
}
