using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class Spawner:MonoBehaviour
    {
        public EnemyDef definition;
        public int count=3;
        public float radius=3;
        EnemyBrain[] prepared;
        public void Prepare(Vector3 viewpoint,float viewRadius)
        {
            if(prepared!=null)return;prepared=Create();
            foreach(var enemy in prepared)enemy.gameObject.AddComponent<DormantEnemy>().Hold(viewpoint,viewRadius);
        }
        public void CancelPreview()
        {
            if(prepared==null)return;
            foreach(var enemy in prepared)if(enemy!=null)Destroy(enemy.gameObject);
            prepared=null;
        }
        public EnemyBrain[] Spawn()
        {
            if(prepared==null)return Create();
            var spawned=prepared;prepared=null;
            foreach(var enemy in spawned)enemy.GetComponent<DormantEnemy>().Activate();
            return spawned;
        }
        EnemyBrain[] Create()
        {
            var spawned=new EnemyBrain[count];
            for(int i=0;i<count;i++){float angle=i*2*Mathf.PI/count;spawned[i]=ActorFactory.Enemy(definition,transform.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius);}
            return spawned;
        }
        void OnDestroy()=>CancelPreview();
    }
}
