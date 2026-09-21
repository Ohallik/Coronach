using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class Spawner:MonoBehaviour
    {
        public EnemyDef definition;
        public int count=3;
        public float radius=3;
        public EnemyBrain[] Spawn()
        {
            var spawned=new EnemyBrain[count];
            for(int i=0;i<count;i++){float angle=i*2*Mathf.PI/count;spawned[i]=ActorFactory.Enemy(definition,transform.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius);}
            return spawned;
        }
    }
}
