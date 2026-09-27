using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>Visual finish and recoverable bodies outlive lethal gameplay resolution.</summary>
    [DefaultExecutionOrder(600)]
    public sealed class DefeatPresentation : MonoBehaviour
    {
        public Transform visual;
        Health health;CombatActor actor;EnemyBrain enemy;
        Collider[] colliders;bool[] collisionEnabled;
        bool down,recovering,rigid;
        float started,duration,hold;
        Vector3 origin,terminal;
        Quaternion originRotation,terminalRotation;
        Vector3 supportPoint,supportNormal;Vector3[] supportVertices;
        Transform[] sections;Vector3[] sectionOrigins;Quaternion[] sectionRotations;
        FlightFailurePresentation flightFailure;
        void Awake()
        {
            health=GetComponent<Health>();actor=GetComponent<CombatActor>();enemy=GetComponent<EnemyBrain>();
            health.Died+=BeginDown;health.Revived+=BeginRecovery;
        }
        void Start(){if(!health.Alive)BeginDown(health,default);}
        void OnDestroy(){if(health!=null){health.Died-=BeginDown;health.Revived-=BeginRecovery;}}
        void BeginDown(Health _,DamagePacket packet)
        {
            if(down)return;
            down=true;recovering=false;started=GameTime.Now;
            duration=actor!=null?CombatActor.DownDuration:enemy.definition.boss?1.5f:
                enemy.definition.id=="SentinelHusk"?1.15f:enemy.definition.id=="Ridgehound"?.9f:.75f;
            hold=enemy!=null&&enemy.definition.boss?1.65f:1.15f;
            colliders=GetComponentsInChildren<Collider>(true);collisionEnabled=new bool[colliders.Length];
            for(int i=0;i<colliders.Length;i++)
            {
                collisionEnabled[i]=colliders[i].enabled;
                // A recoverable ship still occupies space. Its damage trigger
                // shuts down, while its motionless hull stops a rescuer passing
                // through it. Ground corpses and enemies retain their cleanup.
                colliders[i].enabled=actor!=null&&actor.flight&&colliders[i] is CharacterController&&collisionEnabled[i];
            }
            foreach(var motion in GetComponentsInChildren<ProceduralMotion>())motion.enabled=false;
            if(TryGetComponent<SerpentSegments>(out var segments))
            {
                segments.enabled=false;sections=new Transform[segments.Parts.Count];
                sectionOrigins=new Vector3[sections.Length];sectionRotations=new Quaternion[sections.Length];
                for(int i=0;i<sections.Length;i++)
                {sections[i]=segments.Parts[i];if(sections[i]!=null){sectionOrigins[i]=sections[i].localPosition;sectionRotations[i]=sections[i].localRotation;}}
            }
            var animator=GetComponentInChildren<Animator>();
            rigid=animator==null||!animator.isHuman||actor!=null&&actor.flight;
            if(animator!=null)
            {
                animator.updateMode=AnimatorUpdateMode.UnscaledTime;
                if(rigid)animator.enabled=false;
            }
            if(actor!=null&&actor.flight)
            {
                var form=GetComponent<FormController>();form.FinishForDefeat();visual=form.flight.transform;
                flightFailure=GetComponent<FlightFailurePresentation>()??gameObject.AddComponent<FlightFailurePresentation>();
                flightFailure.Begin(actor,visual,packet);return;
            }
            if(!rigid||visual==null)return;
            origin=visual.localPosition;originRotation=visual.localRotation;
            bool airborne=actor!=null&&actor.flight||enemy!=null&&enemy.definition.id.StartsWith("Chorister");
            terminalRotation=originRotation*Quaternion.Euler(airborne?12:8,0,airborne?58:enemy!=null&&enemy.definition.boss?42:78);
            terminal=origin+Vector3.down*(airborne?.22f:.12f);
            bool groundCreature=enemy!=null&&(enemy.definition.id=="Ridgehound"||enemy.definition.id=="Scrapmite"||enemy.definition.id=="Burrower");
            if(groundCreature)
            {
                // Inspected against each generated body: hound on its flank,
                // mite on its upper shell, burrower along its lower side.
                // A shared tilt left them balancing on a paw, leg or jaw.
                Vector3 rest=enemy.definition.id=="Ridgehound"?new Vector3(0,0,98):
                    enemy.definition.id=="Scrapmite"?new Vector3(0,0,170):new Vector3(-10,0,128);
                terminalRotation=originRotation*Quaternion.Euler(rest);
            }
            if(groundCreature&&CaptureSupport())
            {
                terminalRotation=Quaternion.Inverse(visual.parent.rotation)*Quaternion.FromToRotation(transform.up,supportNormal)*visual.parent.rotation*terminalRotation;
                visual.localRotation=terminalRotation;visual.localPosition=terminal;SupportRigidBody();terminal=visual.localPosition;
                visual.localPosition=origin;visual.localRotation=originRotation;
            }
            else if(!airborne)
            {
                // The remaining flight archetypes retain their existing branch
                // until their separate failure-motion review.
                // Calibrate the collapsed generated geometry once, rather than
                // trusting the inflated bounds of the grafted animal skeleton.
                float surface=transform.position.y;
                if(Physics.Raycast(transform.position+Vector3.up*2,Vector3.down,out var ground,6,~0,QueryTriggerInteraction.Ignore))surface=ground.point.y;
                visual.localRotation=terminalRotation;visual.localPosition=terminal;
                float bottom=MinimumY(visual);
                if(!float.IsInfinity(bottom))terminal+=visual.parent.InverseTransformVector(Vector3.up*(surface+.025f-bottom));
                visual.localPosition=origin;visual.localRotation=originRotation;
            }
        }
        bool CaptureSupport()
        {
            float nearest=float.PositiveInfinity;supportVertices=null;
            foreach(var hit in Physics.RaycastAll(transform.position+Vector3.up*2,Vector3.down,6,~0,QueryTriggerInteraction.Ignore))
            {
                if(!CombatCover.Opaque(hit.collider)||hit.normal.y<.5f||hit.distance>=nearest)continue;
                nearest=hit.distance;supportPoint=hit.point;supportNormal=hit.normal;
            }
            if(float.IsInfinity(nearest))return false;
            // The nonhumanoid Animator has stopped on the actual lethal pose.
            // Cache that geometry once; intermediate tilt needs support too.
            var vertices=new List<Vector3>();
            foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                foreach(var p in GeneratedGeometry.WorldSkinPoints(skin))vertices.Add(visual.InverseTransformPoint(p));
            foreach(var filter in visual.GetComponentsInChildren<MeshFilter>())
                if(filter.sharedMesh!=null)foreach(var p in filter.sharedMesh.vertices)
                    vertices.Add(visual.InverseTransformPoint(filter.transform.TransformPoint(p)));
            supportVertices=vertices.ToArray();return supportVertices.Length>0;
        }
        void SupportRigidBody()
        {
            if(supportVertices==null||supportVertices.Length==0)return;
            // Dot in local space avoids transforming every vertex each frame.
            // Transpose retains any parent scale; a direction alone would not.
            var localNormal=visual.localToWorldMatrix.transpose.MultiplyVector(supportNormal);
            float minimum=float.PositiveInfinity;
            foreach(var p in supportVertices)minimum=Mathf.Min(minimum,Vector3.Dot(p,localNormal));
            minimum+=Vector3.Dot(visual.position-supportPoint,supportNormal);
            visual.position+=supportNormal*(.025f-minimum);
        }
        static float MinimumY(Transform root)
        {
            float result=float.PositiveInfinity;var points=new List<Vector3>();
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach(var p in GeneratedGeometry.WorldSkinPoints(skin))result=Mathf.Min(result,p.y);
            }
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh==null)continue;filter.sharedMesh.GetVertices(points);
                foreach(var p in points)result=Mathf.Min(result,filter.transform.TransformPoint(p).y);
            }
            return result;
        }
        void BeginRecovery(Health _)
        {
            if(!down)return;recovering=true;started=GameTime.Now;
            if(flightFailure!=null)flightFailure.BeginRecovery();
        }
        void LateUpdate()
        {
            if(!down)return;
            float elapsed=GameTime.Now-started;
            if(flightFailure!=null)
            {
                if(!health.Alive)
                    for(int i=0;i<colliders.Length;i++)if(colliders[i] is CharacterController)colliders[i].enabled=actor.flight&&collisionEnabled[i];
                flightFailure.Advance(elapsed,recovering);
            }
            if(recovering)
            {
                if(flightFailure==null&&rigid&&visual!=null)
                {
                    float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/CombatActor.ReviveDuration));
                    visual.localPosition=Vector3.Lerp(terminal,origin,t);visual.localRotation=Quaternion.Slerp(terminalRotation,originRotation,t);
                }
                if(elapsed>=CombatActor.ReviveDuration)
                {
                    if(flightFailure!=null)flightFailure.Finish();
                    for(int i=0;i<colliders.Length;i++)if(colliders[i]!=null)colliders[i].enabled=collisionEnabled[i];
                    down=recovering=false;
                }
                return;
            }
            if(flightFailure==null&&rigid&&visual!=null)
            {
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/duration));
                visual.localPosition=Vector3.Lerp(origin,terminal,t);visual.localRotation=Quaternion.Slerp(originRotation,terminalRotation,t);
                SupportRigidBody();
            }
            if(sections!=null)
                for(int i=0;i<sections.Length;i++)
                {
                    if(sections[i]==null)continue;
                    float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-i*.09f)/duration));
                    sections[i].localPosition=sectionOrigins[i]+Vector3.down*(.65f*t);
                    sections[i].localRotation=sectionRotations[i]*Quaternion.Euler(8*t,0,(38+i*5)*t);
                }
            if(enemy!=null&&elapsed>=duration+hold)Destroy(gameObject);
        }
    }
}
