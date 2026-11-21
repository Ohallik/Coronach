using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A waypoint of a pursuit (chapter 8: the Warmaster's skiff running for the carrier across the war harbour, "under Auntie, round the flak mole, up the slip"): an empty object at the flight
    /// plane, numbered in the order the runner takes them. It holds station off one and fights there, and at each quarter of its bar breaks off for the next (<see cref="After"/>). With none in
    /// the room (the flight yard) it runs away from the party instead. The harbour: docs/maps/Harrowgate.md, "Pursuit contract".
    /// </summary>
    public sealed class PursuitLeg:MonoBehaviour
    {
        public static readonly List<PursuitLeg> All=new();
        /// <summary>Its place in the run: 0 where the runner lifts, and on from there.</summary>
        public int index;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        /// <summary>The waypoint after this index in the run, or none past the last.</summary>
        public static PursuitLeg After(int index)
        {
            PursuitLeg next=null;
            foreach(var leg in All)if(leg!=null&&leg.index>index&&(next==null||leg.index<next.index))next=leg;
            return next;
        }
        /// <summary>The waypoint with this index, or none.</summary>
        public static PursuitLeg At(int index){foreach(var leg in All)if(leg!=null&&leg.index==index)return leg;return null;}
        /// <summary>The waypoint nearest a point on the plane, or none.</summary>
        public static PursuitLeg Nearest(Vector3 point)
        {
            PursuitLeg best=null;float least=float.PositiveInfinity;
            foreach(var leg in All){if(leg==null)continue;var o=leg.transform.position-point;o.y=0;if(o.sqrMagnitude<least){least=o.sqrMagnitude;best=leg;}}
            return best;
        }
    }
}
