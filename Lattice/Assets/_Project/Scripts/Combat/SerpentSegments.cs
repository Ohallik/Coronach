using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class SerpentSegments:MonoBehaviour
    {
        readonly Transform[] segments=new Transform[7];
        void Start()
        {
            var definition=GetComponent<EnemyBrain>().definition;var health=GetComponent<Health>();
            for(int i=0;i<segments.Length;i++)
            {
                var prefab=i==segments.Length-1?definition.tailSegment:definition.bodySegment;
                var go=prefab!=null?Instantiate(prefab,transform):ActorFactory.Visual("ART_PENDING_CantorSegment",PrimitiveType.Sphere,transform,new Vector3(1.8f,1.5f,2),Vector3.zero,"Enemy");
                go.name=i==segments.Length-1?"Tail":"WeakSegment_"+i;go.transform.position=transform.position-Vector3.forward*(i+1)*2.2f+Vector3.up;
                var collider=go.AddComponent<SphereCollider>();collider.radius=.65f;collider.isTrigger=true;
                var hurtbox=go.AddComponent<Hurtbox>();hurtbox.owner=health;hurtbox.multiplier=i%2==0?2:1;
                segments[i]=go.transform;
            }
        }
        void LateUpdate()
        {
            var previous=transform.position+Vector3.up;
            for(int i=0;i<segments.Length;i++)
            {
                var segment=segments[i];if(segment==null)continue;
                var delta=previous-segment.position;var direction=delta.sqrMagnitude>.01f?delta.normalized:transform.forward;
                var goal=previous-direction*2.2f;goal.y=transform.position.y+1+Mathf.Sin(Time.time*2-i*.5f)*.25f;
                segment.position=Vector3.Lerp(segment.position,goal,1-Mathf.Exp(-8*Time.deltaTime));segment.rotation=Quaternion.LookRotation(direction);previous=segment.position;
            }
        }
    }
}
