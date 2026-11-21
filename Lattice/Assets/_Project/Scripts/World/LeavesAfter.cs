using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Someone who goes once a flag is set, there and then: Pim leaving the Drowned Waystation's dry pocket at the end of the talk that finds them. The moment the flag is set they stop
    /// being someone to talk to (every <see cref="Npc"/> on them is switched off, so nothing offers a line meant for somewhere else); once the conversation has closed they walk
    /// <see cref="path"/> (rising and falling to each point's height, as a <see cref="WalkLoop"/> does: the body's animator sees it move and walks), and at its end they are gone.
    /// Anyone in the way holds them up; held for longer than <see cref="patience"/>, they are gone where they stand. A zone loaded with the flag set never shows them. It lives on the
    /// person, and needs no always-active holder: it hides only itself.
    /// </summary>
    public sealed class LeavesAfter:MonoBehaviour
    {
        public string flag;
        public Vector3[] path=System.Array.Empty<Vector3>();
        public float speed=2.2f,turnSpeed=360,patience=2.5f;
        readonly RaycastHit[] hits=new RaycastHit[16];
        int next=-1;float held;bool told;
        public bool Leaving=>next>=0;
        bool Set=>!string.IsNullOrEmpty(flag)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(flag);
        void Start(){if(Set)gameObject.SetActive(false);}
        void Update()
        {
            if(!Set||GameTime.Paused)return;
            if(!told){told=true;foreach(var npc in GetComponentsInChildren<Npc>())npc.enabled=false;}
            if(next<0)
            {
                if(GameServices.Current.Input.Blocked)return;
                next=0;Debug.Log("LEAVES "+name);
            }
            if(next>=path.Length){Gone();return;}
            var delta=path[next]-transform.position;delta.y=0;
            if(delta.magnitude<.08f){next++;return;}
            float distance=Mathf.Min(speed*Time.deltaTime,delta.magnitude);var direction=delta.normalized;
            int count=Physics.SphereCastNonAlloc(transform.position+Vector3.up*.95f,.34f,direction,hits,distance+.25f,~0,QueryTriggerInteraction.Ignore);
            bool blocked=count==hits.Length;
            for(int i=0;i<count&&!blocked;i++){var other=hits[i].collider;if(other.isTrigger||other.transform==transform||other.transform.IsChildOf(transform))continue;if(other.attachedRigidbody!=null||other is CharacterController)blocked=true;}
            if(blocked){held+=Time.deltaTime;if(held>patience)Gone();return;}
            held=0;
            var goal=Quaternion.LookRotation(direction);transform.rotation=Quaternion.RotateTowards(transform.rotation,goal,turnSpeed*Time.deltaTime);
            var at=transform.position+direction*distance;at.y=Mathf.MoveTowards(at.y,path[next].y,distance*1.2f);transform.position=at;
        }
        void Gone(){Debug.Log("GONE "+name);gameObject.SetActive(false);}
    }
}
