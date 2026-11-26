using System.Collections.Generic;
using UnityEngine;

namespace Lattice.Combat
{
    // The real encounter actor is visible before activation. Preserve each
    // damage/collision state, including the collar's protected head lock.
    public sealed class DormantEnemy:MonoBehaviour
    {
        static readonly List<DormantEnemy> waiting=new();
        Health health;
        Health[] bodies;
        bool[] damageable,collisionEnabled;
        Collider[] collision;
        EnemyBrain brain;
        BossController boss;
        bool brainEnabled,bossEnabled;
        Vector3 viewpoint;
        float range;
        public bool Held{get;private set;}
        public void Hold(Vector3 point,float radius)
        {
            if(Held)return;
            health=GetComponent<Health>();brain=GetComponent<EnemyBrain>();boss=GetComponent<BossController>();
            brainEnabled=brain.enabled;brain.enabled=false;
            if(boss!=null){bossEnabled=boss.enabled;boss.enabled=false;}
            bodies=GetComponentsInChildren<Health>(true);damageable=new bool[bodies.Length];
            for(int i=0;i<bodies.Length;i++){damageable[i]=bodies[i].Damageable;bodies[i].Damageable=false;}
            collision=GetComponentsInChildren<Collider>(true);collisionEnabled=new bool[collision.Length];
            for(int i=0;i<collision.Length;i++){collisionEnabled[i]=collision[i].enabled;collision[i].enabled=false;}
            viewpoint=point;range=Mathf.Max(0,radius);Held=true;waiting.Add(this);
            // A held serpent lies in its resting coil rather than the straight line it was assembled in.
            if(TryGetComponent<SerpentSegments>(out var serpent))serpent.Rest();
        }
        public void Activate()
        {
            if(!Held)return;Held=false;waiting.Remove(this);
            for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null)bodies[i].Damageable=damageable[i];
            for(int i=0;i<collision.Length;i++)if(collision[i]!=null)collision[i].enabled=collisionEnabled[i];
            brain.enabled=brainEnabled;if(boss!=null)boss.enabled=bossEnabled;
            Destroy(this);
        }
        /// <summary>How much farther from the animal than its viewpoint is a viewer may be and still be shown it.</summary>
        public const float ShownBehind=8;
        /// <summary>For a test's before: a show holds through the whole of its pocket, behind its viewpoint too, as it did until D428.</summary>
        public static bool ShownFromBehind;
        public static Health Interest(Vector3 viewer)
        {
            Health chosen=null;float nearest=float.PositiveInfinity;
            foreach(var actor in waiting)
            {
                if(actor==null||!actor.Held||!actor.isActiveAndEnabled)continue;
                float distance=(viewer-actor.viewpoint).sqrMagnitude;
                if(distance>actor.range*actor.range||distance>=nearest)continue;
                // D428: a show is seen from its viewpoint on, not from behind it. A big animal's pocket reaches from its viewpoint to the animal (the Tollkeeper's is 126 m
                // round its checkpoint), and that sphere reaches as far back the other way: over the Last Mooring's guns' gap and its second lane, where the view went back
                // 270 m to the whole carrier on the way into each fight and again once it was won, the ships 12 px long (nov24-recon-r6b-64 s64/072 to 075, 089). Small
                // pockets (the Cantor's 3.5 m) lie wholly inside the margin and are as they were.
                if(!ShownFromBehind)
                {
                    var at=actor.transform.position;float most=(actor.viewpoint-at).magnitude+ShownBehind;
                    if((viewer-at).sqrMagnitude>most*most)continue;
                }
                nearest=distance;chosen=actor.health;
            }
            return chosen;
        }
        void OnDisable(){waiting.Remove(this);}
        void OnEnable(){if(Held&&!waiting.Contains(this))waiting.Add(this);}
    }
}
