using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// A resident on an errand: an authored loop walked for as long as the zone is open, with a wait at each stop. Blocking, not navigation, as <see cref="ScriptedWalk"/> is (D195),
    /// and for people with no name: a child after a seed pod, a crew heaving on a rope, a queue shuffling up. It stands still for a pause and for anyone in its way, and it never
    /// pushes the party. It goes on under a conversation, because a town that freezes when someone speaks reads as a photograph. The body's own animator sees it move and walks.
    /// <see cref="ZoneKit"/>'s Walker makes one.
    /// </summary>
    public sealed class WalkLoop:MonoBehaviour
    {
        public Vector3[] path;
        public float speed=1.1f,turnSpeed=300;
        /// <summary>Seconds waited at each point of the path.</summary>
        public float pause=2;
        /// <summary>Walks to the end and back along the same line instead of round to the start.</summary>
        public bool thereAndBack;
        /// <summary>Kept facing this whichever way the feet go: a crew on a rope faces what it hauls and steps backward.</summary>
        public Transform faces;
        /// <summary>Once this flag is set the walk ends at its next stop (the job is done).</summary>
        public string stopFlag;
        readonly RaycastHit[] hits=new RaycastHit[16];
        int next=1,step=1;float waitUntil;
        public bool Blocked{get;private set;}
        void Start()
        {
            if(path==null||path.Length<2){enabled=false;return;}
            transform.position=path[0];Face(faces!=null?faces.position-path[0]:path[1]-path[0],true);
            // People on the same errand don't keep step: each starts a little into its first wait.
            waitUntil=Time.time+Mathf.Repeat(path[0].x*7.3f+path[0].z*3.1f,Mathf.Max(.1f,pause));
        }
        void Update()
        {
            if(GameTime.Paused||Time.time<waitUntil)return;
            var delta=path[next]-transform.position;delta.y=0;
            if(delta.magnitude<.05f)
            {
                if(!string.IsNullOrEmpty(stopFlag)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(stopFlag)){Blocked=false;enabled=false;return;}
                waitUntil=Time.time+pause;
                if(thereAndBack){if(next+step<0||next+step>=path.Length)step=-step;next+=step;}
                else next=(next+1)%path.Length;
                return;
            }
            float distance=Mathf.Min(Mathf.Max(0,speed)*Time.deltaTime,delta.magnitude);if(distance<=0)return;
            var direction=delta.normalized;
            int count=Physics.SphereCastNonAlloc(transform.position+Vector3.up*.95f,.38f,direction,hits,distance+.25f,~0,QueryTriggerInteraction.Ignore);
            Blocked=count==hits.Length;
            for(int i=0;i<count&&!Blocked;i++)
            {
                var other=hits[i].collider.transform;
                if(other!=transform&&!other.IsChildOf(transform))Blocked=true;
            }
            if(Blocked)return;
            Face(faces!=null?faces.position-transform.position:direction,false);
            var at=transform.position+direction*distance;at.y=Mathf.MoveTowards(at.y,path[next].y,distance);transform.position=at;
        }
        void Face(Vector3 direction,bool snap)
        {
            direction.y=0;if(direction.sqrMagnitude<.0001f)return;var goal=Quaternion.LookRotation(direction);
            transform.rotation=snap?goal:Quaternion.RotateTowards(transform.rotation,goal,Mathf.Max(0,turnSpeed)*Time.deltaTime);
        }
    }
}
