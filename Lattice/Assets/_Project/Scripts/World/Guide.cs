using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Someone who goes on ahead and waits: Idra Sorn across the White Reach and down the dig, at each of her talks before the party gets there, until she joins it. Leg i is walked
    /// once <see cref="Leg.flag"/> is set and the conversation that set it has closed (blocking, not navigation, as a <see cref="WalkLoop"/> is: points on the floor, a stop for
    /// anyone in the way); at its end she waits for the next. Held in the way past <see cref="patience"/> she is put at her leg's end, which is out of sight ahead. At load she stands
    /// at the end of the last leg whose flag is set (before any, at the first leg's first point), so nobody sees her walk a road twice; once <see cref="goneFlag"/> is set she is not
    /// here (in the party, or gone). Never someone to talk to: her lines are the crossings'. Her body's own animator sees her move and walks. Logs <c>GUIDE_LEG i</c> and <c>GUIDE_AT i</c>.
    /// </summary>
    public sealed class Guide:MonoBehaviour
    {
        [System.Serializable] public struct Leg{public string flag;public Vector3[] path;}
        public Leg[] legs=System.Array.Empty<Leg>();
        public string goneFlag;
        public float speed=3.4f,turnSpeed=360,patience=3;
        /// <summary>What she looks toward while she waits at the end of each leg.</summary>
        public Vector3[] faces=System.Array.Empty<Vector3>();
        readonly RaycastHit[] hits=new RaycastHit[16];
        public int Stage{get;private set;}=-1;
        int next=-1;float held;bool placed;
        /// <summary>A leg's flag may name more than one, as <c>a|b</c>: any of them sends her on (a route that skips a level skips its talk too). A leg with none is where she starts.</summary>
        static bool Has(string key)
        {
            if(GameServices.Current==null)return false;if(string.IsNullOrEmpty(key))return true;
            foreach(var one in key.Split('|'))if(one.Length>0&&GameServices.Current.Flags.GetBool(one))return true;
            return false;
        }
        int Reached(){int last=-1;for(int i=0;i<legs.Length;i++)if(Has(legs[i].flag))last=i;return last;}
        void Put(int leg)
        {
            Stage=leg;next=-1;held=0;
            var path=leg<0?(legs.Length>0?legs[0].path:null):legs[leg].path;if(path==null||path.Length==0)return;
            transform.position=leg<0?path[0]:path[^1];Face(leg);
        }
        void Face(int leg)
        {
            int k=leg+1;if(k<0||k>=faces.Length)return;var look=faces[k]-transform.position;look.y=0;if(look.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(look);
        }
        void Update()
        {
            var services=GameServices.Current;if(services==null)return;
            if(!string.IsNullOrEmpty(goneFlag)&&Has(goneFlag)){gameObject.SetActive(false);return;}
            if(!placed){placed=true;Put(Reached());return;}
            if(GameTime.Paused)return;
            if(next<0)
            {
                // The next leg whose flag has been set, once nobody is talking: a talk that sends her on ends before she goes.
                if(Stage+1<legs.Length&&Has(legs[Stage+1].flag)&&!services.Input.Blocked){Stage++;next=0;held=0;Debug.Log("GUIDE_LEG "+Stage);}
                return;
            }
            var path=legs[Stage].path;
            if(next>=path.Length){next=-1;Face(Stage);Debug.Log("GUIDE_AT "+Stage);return;}
            var delta=path[next]-transform.position;delta.y=0;
            if(delta.magnitude<.1f){next++;return;}
            float distance=Mathf.Min(speed*Time.deltaTime,delta.magnitude);var direction=delta.normalized;
            int count=Physics.SphereCastNonAlloc(transform.position+Vector3.up*.95f,.34f,direction,hits,distance+.25f,~0,QueryTriggerInteraction.Ignore);
            bool blocked=count==hits.Length;
            for(int i=0;i<count&&!blocked;i++){var other=hits[i].collider;if(other.isTrigger||other.transform==transform||other.transform.IsChildOf(transform))continue;if(other.attachedRigidbody!=null||other is CharacterController)blocked=true;}
            if(blocked){held+=Time.deltaTime;if(held>patience){Put(Stage);Debug.Log("GUIDE_AT "+Stage);}return;}
            held=0;
            transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),turnSpeed*Time.deltaTime);
            var at=transform.position+direction*distance;at.y=Mathf.MoveTowards(at.y,path[next].y,distance*1.2f);transform.position=at;
        }
    }
}
