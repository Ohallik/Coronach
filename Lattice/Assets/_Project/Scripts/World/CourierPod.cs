using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A small ship flying in the party's wake that is nobody's to fly: Lume's courier pod with Ishen in it, behind Sela down the Orrery (chapter 7: "Courier pod to the Shapers. Lume,
    /// flying."). Drawn only, never in anyone's way and never struck: it keeps <see cref="behind"/> metres astern and <see cref="aside"/> to port of whichever of the party's ships is
    /// further back, a little under the plane, eases after it, noses the way it goes and bobs. Gone once <see cref="goneFlag"/> is set.
    /// </summary>
    public sealed class CourierPod:MonoBehaviour
    {
        public float behind=8,aside=3.5f,below=.8f,ease=.7f,bob=.25f;public string goneFlag="";
        Vector3 velocity;bool placed;
        void LateUpdate()
        {
            var services=GameServices.Current;var party=PartyController.Current;
            if(services!=null&&!string.IsNullOrEmpty(goneFlag)&&services.Flags.GetBool(goneFlag)){gameObject.SetActive(false);return;}
            if(party==null||party.Active==null||!party.Active.flight)return;
            Transform rear=party.Active.transform;float leadZ=Vector3.Dot(rear.position,rear.forward);
            foreach(var hero in party.members)if(hero!=null&&hero.flight&&hero.gameObject.activeInHierarchy&&Vector3.Dot(hero.transform.position,rear.forward)<leadZ){rear=hero.transform;leadZ=Vector3.Dot(rear.position,rear.forward);}
            var way=rear.forward;way.y=0;if(way.sqrMagnitude<.01f)way=Vector3.forward;way.Normalize();
            var port=new Vector3(-way.z,0,way.x);
            var goal=rear.position-way*behind+port*aside;goal.y=rear.position.y-below+Mathf.Sin(Time.time*1.3f)*bob;
            if(!placed){transform.position=goal;placed=true;}
            var before=transform.position;
            transform.position=Vector3.SmoothDamp(transform.position,goal,ref velocity,ease,40,Time.deltaTime);
            var moved=transform.position-before;moved.y=0;
            if(moved.sqrMagnitude>.0004f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(moved.normalized)*Quaternion.Euler(0,0,Mathf.Clamp(-Vector3.SignedAngle(transform.forward,moved,Vector3.up)*2,-20,20)),1-Mathf.Exp(-4*Time.deltaTime));
        }
    }
}
