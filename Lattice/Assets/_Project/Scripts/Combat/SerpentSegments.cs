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
        /// <summary>One link of the swimming chain. The drums are 2.6 m long: on a 2 m link they stay shut on the outside of a bend up to MostBend, where 2.2 m opened at 19 degrees.
        /// The resting coil and its bed were laid out at RestLink, and the chain draws in from there as it wakes.</summary>
        public const float Link=2,RestLink=2.2f;
        /// <summary>The body bends no further than this at a joint once it has caught up. A head doubling back can outrun that for a moment;
        /// the bend eases back at this rate instead of whipping fourteen metres of tail round in a frame.</summary>
        const float MostBend=26,BendRate=540;
        /// <summary>Nothing but a joint's own neighbours comes closer than this (the drums are 2 m across): the body coils round itself, not through itself.</summary>
        const float KeepClear=2.6f;
        /// <summary>Faster than anything the animal does (its lash is 24 m/s), so the carrier was put there: the body takes that jump up over the next moment, not in one frame.</summary>
        const float JumpSpeed=40,JumpRecovery=3;
        Vector3 lastCarrier,slack;float link=RestLink;
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
            followingInitialized=true;restoredPosition=transform.position;restoredRotation=transform.rotation;resting=true;lastCarrier=transform.position;slack=Vector3.zero;link=RestLink;
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
        {for(int i=0;i<segments.Length;i++)if(segments[i]!=null)followedPositions[i]=segments[i].position;followingInitialized=true;lastCarrier=transform.position;slack=Vector3.zero;}
        /// <summary>Where a joint hangs from the one ahead of it: exactly one link away across the plane. The wave is height only, so speed can't stretch the body.</summary>
        static Vector3 Hang(Vector3 ahead,Vector3 at,Vector3 aheadHeading,bool bends,float length,float dt,out Vector3 heading)
        {
            var delta=ahead-at;delta.y=0;
            heading=delta.sqrMagnitude>.0001f?delta.normalized:aheadHeading;
            if(bends)
            {
                float bend=Vector3.SignedAngle(aheadHeading,heading,Vector3.up),size=Mathf.Abs(bend);
                if(size>MostBend)heading=Quaternion.AngleAxis(Mathf.Sign(bend)*Mathf.Max(MostBend,size-BendRate*dt),Vector3.up)*aheadHeading;
            }
            return ahead-heading*length;
        }
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
            float dt=Time.deltaTime,height=transform.position.y+CenterHeight;
            var carrier=transform.position;var moved=carrier-lastCarrier;moved.y=0;lastCarrier=carrier;
            if(moved.magnitude>JumpSpeed*dt)slack+=moved;
            slack=Vector3.MoveTowards(slack,Vector3.zero,JumpRecovery*dt);
            var previous=carrier-slack;previous.y=height;
            var ahead=transform.forward;ahead.y=0;ahead=ahead.sqrMagnitude>.0001f?ahead.normalized:Vector3.forward;
            link=Mathf.MoveTowards(link,Link,.8f*dt);
            for(int i=0;i<segments.Length;i++)
            {
                var segment=segments[i];if(segment==null)continue;
                // The hierarchy owns lifetime and recovery, not locomotion.
                // Follow the last evaluated world pose; inherited carrier yaw
                // otherwise swings the whole tail through nearby ships.
                var at=Hang(previous,followedPositions[i],ahead,i>0,link,dt,out var heading);
                for(int other=-1;other<i-1;other++)
                {
                    var near=other<0?carrier:followedPositions[other];
                    var apart=at-near;apart.y=0;float gap=apart.magnitude;
                    if(gap>=KeepClear)continue;
                    at=Hang(previous,near+(gap>.001f?apart/gap:transform.right)*KeepClear,ahead,i>0,link,dt,out heading);
                }
                at.y=height+Mathf.Sin(Time.time*2-i*.5f)*.25f;
                segment.position=at;followedPositions[i]=at;
                segment.rotation=Quaternion.LookRotation(previous-at);previous=at;ahead=heading;
            }
        }
    }
}
