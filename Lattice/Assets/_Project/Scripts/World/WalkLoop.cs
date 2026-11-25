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
            if(!Blocked)Blocked=HeroBeside(direction,distance+.25f);
            if(Blocked)return;
            Face(faces!=null?faces.position-transform.position:direction,false);
            var at=transform.position+direction*distance;at.y=Mathf.MoveTowards(at.y,path[next].y,distance);transform.position=at;
        }
        /// <summary>A hero its body would come against on this step: one ahead of it nearer its way than its own solid's width and his capsule's. The look ahead is a 0.38 m ball at
        /// the chest, and since D391 a walker's solid is as wide as what it draws (the cook with his pot, 1 m and more): a hero standing or walking beside its way, outside the ball
        /// and inside its solid, was walked through, its body sliding into hers (Idra in the cook, nov24-recon-h-52 s52/232, D408). Only the party: a wall beside its way is the
        /// builder's to keep clear, and a person standing there would leave it stuck.</summary>
        bool HeroBeside(Vector3 direction,float ahead)
        {
            var party=Lattice.Combat.PartyController.Current;if(lookedAsBefore||party==null||party.members==null)return false;
            if(own<0){own=TryGetComponent<CapsuleCollider>(out var solid)&&solid.enabled&&!solid.isTrigger?solid.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.z)):.38f;}
            foreach(var hero in party.members)
            {
                if(hero==null||!hero.gameObject.activeInHierarchy||!hero.TryGetComponent<CharacterController>(out var his)||!his.enabled)continue;
                var off=hero.transform.position-transform.position;if(Mathf.Abs(off.y)>2)continue;off.y=0;
                float along=Vector3.Dot(off,direction);if(along<=0)continue;
                float across=(off-direction*Mathf.Min(along,ahead)).magnitude;
                if(across<own+his.radius)return true;
            }
            return false;
        }
        float own=-1;
        /// <summary>For a test's broken state only (BodiesKeepApartTests): it looks ahead with the ball alone, as before D408.</summary>
        static bool lookedAsBefore;
        void Face(Vector3 direction,bool snap)
        {
            direction.y=0;if(direction.sqrMagnitude<.0001f)return;var goal=Quaternion.LookRotation(direction);
            transform.rotation=snap?goal:Quaternion.RotateTowards(transform.rotation,goal,Mathf.Max(0,turnSpeed)*Time.deltaTime);
        }
    }
}
