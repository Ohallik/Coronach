using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class Spawner:MonoBehaviour
    {
        public EnemyDef definition;
        public int count=3;
        public float radius=3;
        /// <summary>Stand as this spawner faces, for a body on show before its fight (Hessa at her work, turning to the door).</summary>
        public bool faceAsPlaced;
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
            float threat=definition.boss?1:ZoneThreat();
            for(int i=0;i<count;i++){float angle=i*2*Mathf.PI/count;spawned[i]=ActorFactory.Enemy(definition,transform.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius);spawned[i].DamageScale=threat;if(faceAsPlaced)spawned[i].transform.rotation=transform.rotation;}
            return spawned;
        }
        /// <summary>The current zone's ordinary-enemy threat (ZoneDef.enemyThreat), 1 when unknown.</summary>
        float ZoneThreat()
        {
            string scene=gameObject.scene.name;
            foreach(var zone in Lattice.Core.GameCatalog.All<ZoneDef>())if(zone!=null&&(zone.scene==scene||zone.id==scene))return Mathf.Max(.1f,zone.enemyThreat);
            return 1;
        }
        void OnDestroy()=>CancelPreview();
    }
}
