using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class SerpentSegments:MonoBehaviour
    {
        readonly Transform[] segments=new Transform[7];
        public System.Collections.Generic.IReadOnlyList<Transform> Parts=>segments;
        public const float CenterHeight=1.5f;
        void Start()
        {
            var definition=GetComponent<EnemyBrain>().definition;var health=GetComponent<Health>();
            Initialize(definition,health);
        }
        public static void CenterVisual(GameObject visual,float height)
        {
            var renderers=visual.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            var local=visual.transform.parent.InverseTransformPoint(bounds.center);
            visual.transform.localPosition+=new Vector3(-local.x,height-local.y,-local.z);
        }
        public void Initialize(EnemyDef definition,Health health)
        {
            if(segments[0]!=null)return;
            for(int i=0;i<segments.Length;i++)
            {
                var prefab=i==segments.Length-1?definition.tailSegment:definition.bodySegment;
                var go=new GameObject(i==segments.Length-1?"Tail":"WeakSegment_"+i);go.transform.SetParent(transform,false);
                var visual=prefab!=null?Instantiate(prefab,go.transform):ActorFactory.Visual("ART_PENDING_CantorSegment",PrimitiveType.Sphere,go.transform,new Vector3(1.8f,1.5f,2),Vector3.zero,"Enemy");
                CenterVisual(visual,0);go.transform.position=transform.position-transform.forward*(i+1)*2.2f+Vector3.up*CenterHeight;go.transform.rotation=transform.rotation;
                var collider=go.AddComponent<SphereCollider>();collider.radius=.85f;collider.isTrigger=true;
                var hurtbox=go.AddComponent<Hurtbox>();hurtbox.owner=health;hurtbox.multiplier=i%2==0?2:1;
                segments[i]=go.transform;
            }
        }
        void LateUpdate()
        {
            var previous=transform.position+Vector3.up*CenterHeight;
            for(int i=0;i<segments.Length;i++)
            {
                var segment=segments[i];if(segment==null)continue;
                var delta=previous-segment.position;var direction=delta.sqrMagnitude>.01f?delta.normalized:transform.forward;
                var goal=previous-direction*2.2f;goal.y=transform.position.y+CenterHeight+Mathf.Sin(Time.time*2-i*.5f)*.25f;
                segment.position=Vector3.Lerp(segment.position,goal,1-Mathf.Exp(-8*Time.deltaTime));segment.rotation=Quaternion.LookRotation(direction);previous=segment.position;
            }
        }
    }
}
