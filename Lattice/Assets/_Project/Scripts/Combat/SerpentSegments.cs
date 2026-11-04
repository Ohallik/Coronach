using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class SerpentSegments:MonoBehaviour
    {
        readonly Transform[] segments=new Transform[7];
        readonly Vector3[] followedPositions=new Vector3[7];
        bool followingInitialized;
        bool holdingRestoredPose;
        // A dormant animal lies in a loose S that ends in a curled tail, and only breathes until its head moves.
        bool resting,restPending;
        readonly Vector3[] restingPositions=new Vector3[7];
        Vector3 restoredPosition;
        Quaternion restoredRotation;
        public System.Collections.Generic.IReadOnlyList<Transform> Parts=>segments;
        public const float CenterHeight=1.5f;
        void Start()
        {
            var definition=GetComponent<EnemyBrain>().definition;var health=GetComponent<Health>();
            Initialize(definition,health);
            if(restPending)Rest();
        }
        /// <summary>Resting pose, in the carrier's frame (x right, z forward of the head): each joint's heading from the one
        /// before, in degrees from straight back, turning right when positive. The body swings right, back left, and hooks
        /// its tail; every joint stays exactly 2.2 m from the last, as in motion.</summary>
        static readonly float[] RestHeadings={15,40,25,-15,-50,-30,35};
        public static Vector2[] RestingLayout()
        {
            var result=new Vector2[RestHeadings.Length];var at=Vector2.zero;
            for(int i=0;i<result.Length;i++){float h=RestHeadings[i]*Mathf.Deg2Rad;at+=new Vector2(Mathf.Sin(h),-Mathf.Cos(h))*2.2f;result[i]=at;}
            return result;
        }
        /// <summary>Lay the held animal in its resting coil. The carrier itself never moves or turns here.</summary>
        public void Rest()
        {
            if(segments[0]==null){restPending=true;return;}
            restPending=false;var layout=RestingLayout();var previous=transform.position+Vector3.up*CenterHeight;
            for(int i=0;i<segments.Length;i++)
            {
                var at=transform.position+transform.right*layout[i].x+transform.forward*layout[i].y+Vector3.up*CenterHeight;
                segments[i].position=at;segments[i].rotation=Quaternion.LookRotation(previous-at);
                restingPositions[i]=at;followedPositions[i]=at;previous=at;
            }
            followingInitialized=true;restoredPosition=transform.position;restoredRotation=transform.rotation;resting=true;
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
        public void ResumeFromRestoredPose()
        {
            restoredPosition=transform.position;restoredRotation=transform.rotation;
            RememberWorldPositions();
            holdingRestoredPose=true;enabled=true;
        }
        void RememberWorldPositions()
        {for(int i=0;i<segments.Length;i++)if(segments[i]!=null)followedPositions[i]=segments[i].position;followingInitialized=true;}
        void LateUpdate()
        {
            // Even with zero delta, recomputing the preceding frame's tangent
            // rotates attachments. Pause must hold the evaluated chain too.
            if(Lattice.Core.GameTime.Paused)return;
            if(!followingInitialized)RememberWorldPositions();
            // A stationary recovered carrier keeps its restored anatomy. The
            // next actual carrier movement/turn releases the chain, rather
            // than immediately replacing that pose with a later wave phase.
            if(resting)
            {
                if(transform.position==restoredPosition&&transform.rotation==restoredRotation)
                {
                    // Slow breathing travels down the body; the joints keep their places on the bed.
                    for(int i=0;i<segments.Length;i++)if(segments[i]!=null)
                    {segments[i].position=restingPositions[i]+Vector3.up*Mathf.Sin(Time.time*.9f-i*.6f)*.12f;followedPositions[i]=segments[i].position;}
                    return;
                }
                resting=false;
            }
            if(holdingRestoredPose)
            {
                if(transform.position==restoredPosition&&transform.rotation==restoredRotation)return;
                holdingRestoredPose=false;
            }
            var previous=transform.position+Vector3.up*CenterHeight;
            for(int i=0;i<segments.Length;i++)
            {
                var segment=segments[i];if(segment==null)continue;
                // The hierarchy owns lifetime and recovery, not locomotion.
                // Follow the last evaluated world pose; inherited carrier yaw
                // otherwise swings the whole tail through nearby ships.
                var delta=previous-followedPositions[i];var direction=delta.sqrMagnitude>.01f?delta.normalized:transform.forward;
                var goal=previous-direction*2.2f;goal.y=transform.position.y+CenterHeight+Mathf.Sin(Time.time*2-i*.5f)*.25f;
                segment.position=Vector3.Lerp(followedPositions[i],goal,1-Mathf.Exp(-8*Time.deltaTime));followedPositions[i]=segment.position;
                segment.rotation=Quaternion.LookRotation(direction);previous=segment.position;
            }
        }
    }
}
