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
        }
        public void Activate()
        {
            if(!Held)return;Held=false;waiting.Remove(this);
            for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null)bodies[i].Damageable=damageable[i];
            for(int i=0;i<collision.Length;i++)if(collision[i]!=null)collision[i].enabled=collisionEnabled[i];
            brain.enabled=brainEnabled;if(boss!=null)boss.enabled=bossEnabled;
            Destroy(this);
        }
        public static Health Interest(Vector3 viewer)
        {
            Health chosen=null;float nearest=float.PositiveInfinity;
            foreach(var actor in waiting)
            {
                if(actor==null||!actor.Held||!actor.isActiveAndEnabled)continue;
                float distance=(viewer-actor.viewpoint).sqrMagnitude;
                if(distance<=actor.range*actor.range&&distance<nearest){nearest=distance;chosen=actor.health;}
            }
            return chosen;
        }
        void OnDisable(){waiting.Remove(this);}
        void OnEnable(){if(Held&&!waiting.Contains(this))waiting.Add(this);}
    }
}
