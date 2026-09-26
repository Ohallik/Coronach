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
        Transform[] sections;Vector3[] sectionOrigins;Quaternion[] sectionRotations;
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
            for(int i=0;i<colliders.Length;i++){collisionEnabled[i]=colliders[i].enabled;colliders[i].enabled=false;}
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
            if(actor!=null&&actor.flight)visual=GetComponent<FormController>().flight.transform;
            if(!rigid||visual==null)return;
            origin=visual.localPosition;originRotation=visual.localRotation;
            bool airborne=actor!=null&&actor.flight||enemy!=null&&enemy.definition.id.StartsWith("Chorister");
            terminalRotation=originRotation*Quaternion.Euler(airborne?12:8,0,airborne?58:enemy!=null&&enemy.definition.boss?42:78);
            terminal=origin+Vector3.down*(airborne?.22f:.12f);
            if(!airborne)
            {
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
        {if(!down)return;recovering=true;started=GameTime.Now;}
        void LateUpdate()
        {
            if(!down)return;
            float elapsed=GameTime.Now-started;
            if(recovering)
            {
                if(rigid&&visual!=null)
                {
                    float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/CombatActor.ReviveDuration));
                    visual.localPosition=Vector3.Lerp(terminal,origin,t);visual.localRotation=Quaternion.Slerp(terminalRotation,originRotation,t);
                }
                if(elapsed>=CombatActor.ReviveDuration)
                {
                    for(int i=0;i<colliders.Length;i++)if(colliders[i]!=null)colliders[i].enabled=collisionEnabled[i];
                    down=recovering=false;
                }
                return;
            }
            if(rigid&&visual!=null)
            {
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/duration));
                visual.localPosition=Vector3.Lerp(origin,terminal,t);visual.localRotation=Quaternion.Slerp(originRotation,terminalRotation,t);
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
