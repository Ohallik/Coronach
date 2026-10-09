using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    // The flight map uses the refuge as its reference frame. The detached marker
    // falls behind in one direction; it never oscillates back to imply an anchor.
    public sealed class TallowMarkerDrift:MonoBehaviour
    {
        public Transform marker,observation;
        public float speed=.3f,maxTravel=60,watchDistance=22;
        Vector3 origin;float travel;bool started;
        public float Travel=>travel;
        void Awake(){if(marker!=null)origin=marker.localPosition;}
        void Update()
        {
            if(marker==null||observation==null||GameTime.Paused)return;
            var party=PartyController.Current;
            if(!started&&party!=null&&Vector3.Distance(party.Active.transform.position,observation.position)<watchDistance)started=true;
            if(!started)return;
            travel=Mathf.MoveTowards(travel,Mathf.Max(0,maxTravel),Mathf.Max(0,speed)*Time.unscaledDeltaTime);
            marker.localPosition=origin+Vector3.back*travel;
        }
    }
}
