using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Authored first-town blocking for a civilian, not general navigation (D195). Only while <see cref="requireFlag"/>
    /// holds: the civilian waits at the start of <see cref="path"/>, walks it once the party comes near
    /// <see cref="beginNear"/>, and walks <see cref="exitPath"/> once <see cref="exitFlag"/> is set. A scene loaded after
    /// the exit flag leaves the civilian where the scene places it (the exit path's end), so no walk is replayed. The
    /// walk stops for pause, input-blocking UI and anything standing in the way; it never pushes through the player.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class ScriptedWalk:MonoBehaviour
    {
        public Vector3[] path,exitPath;
        public string requireFlag="opening.marketRequested",exitFlag="opening.walkwayRepaired",skipIfJoined;
        public Transform beginNear;public float beginDistance=11;
        public float speed=1.1f,turnSpeed=300;
        readonly RaycastHit[] hits=new RaycastHit[16];
        GameState owner;Vector3[] current;int next;bool exiting;
        public enum Stage{Inactive,Waiting,Walking,Arrived,Exiting,Done}
        public Stage Phase{get;private set;}
        public bool Arrived=>Phase==Stage.Arrived||Phase==Stage.Exiting||Phase==Stage.Done;
        public bool Blocked{get;private set;}

        void Awake()
        {
            var service=GameServices.Current;Phase=Stage.Inactive;
            if(service==null||path==null||path.Length==0)return;
            owner=service.State;
            if(!service.Flags.GetBool(requireFlag)||!string.IsNullOrEmpty(skipIfJoined)&&owner.party.Exists(m=>m.id==skipIfJoined))return;
            if(!string.IsNullOrEmpty(exitFlag)&&service.Flags.GetBool(exitFlag)){Phase=Stage.Done;return;}
            transform.position=path[0];
            if(path.Length>1)Face(path[1]-path[0],true);
            Phase=path.Length>1?Stage.Waiting:Stage.Arrived;
        }
        void Update()
        {
            if(Phase==Stage.Inactive||Phase==Stage.Done)return;
            var service=GameServices.Current;
            if(service==null||!ReferenceEquals(owner,service.State)){Phase=Stage.Inactive;return;}
            if(GameTime.Paused||GameInput.Current!=null&&GameInput.Current.Blocked)return;
            if(Phase==Stage.Waiting)
            {
                var party=PartyController.Current;
                if(beginNear!=null&&(party==null||Vector3.Distance(party.Active.transform.position,beginNear.position)>beginDistance))return;
                Begin(path,false);
            }
            if(Phase==Stage.Arrived&&!exiting&&exitPath!=null&&exitPath.Length>1&&!string.IsNullOrEmpty(exitFlag)&&service.Flags.GetBool(exitFlag))
                Begin(exitPath,true);
            if(Phase==Stage.Walking||Phase==Stage.Exiting)Step();
        }
        void Begin(Vector3[] route,bool exit){current=route;next=1;exiting=exit;Phase=exit?Stage.Exiting:Stage.Walking;}
        void Step()
        {
            var delta=current[next]-transform.position;delta.y=0;
            if(delta.magnitude<.05f)
            {
                next++;
                if(next>=current.Length){Phase=exiting?Stage.Done:Stage.Arrived;Blocked=false;}
                return;
            }
            float distance=Mathf.Min(Mathf.Max(0,speed)*Time.deltaTime,delta.magnitude);if(distance<=0)return;
            var direction=delta.normalized;
            int count=Physics.SphereCastNonAlloc(transform.position+Vector3.up*.95f,.38f,direction,hits,distance+.1f,~0,QueryTriggerInteraction.Ignore);
            Blocked=count==hits.Length;
            for(int i=0;i<count&&!Blocked;i++)
            {
                var other=hits[i].collider.transform;
                if(other!=transform&&!other.IsChildOf(transform))Blocked=true;
            }
            if(Blocked)return;
            Face(direction,false);transform.position+=direction*distance;
        }
        void Face(Vector3 direction,bool snap)
        {
            direction.y=0;if(direction.sqrMagnitude<.0001f)return;var goal=Quaternion.LookRotation(direction);
            transform.rotation=snap?goal:Quaternion.RotateTowards(transform.rotation,goal,Mathf.Max(0,turnSpeed)*Time.deltaTime);
        }
    }
}
