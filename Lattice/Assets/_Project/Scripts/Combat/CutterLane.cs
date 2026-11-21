using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A point of the Debris Cutter's weave (chapter 9, the Broken Ring; <c>BossController.DebrisCutter.cs</c>): an empty object at the flight plane, numbered clockwise round the fight. Between moves
    /// the Cutter flies to whichever point stands at its distance from the party (closer once it has lost its beam, further once it has lost its mines), so its pursuit has places to be among the
    /// districts. With none in the room (the flight yard) it circles the lead instead. The Ring's contract: docs/maps/BrokenRing.md, "Cutter arena contract".
    /// </summary>
    public sealed class CutterLane:MonoBehaviour
    {
        public static readonly List<CutterLane> All=new();
        /// <summary>Its place round the ring, clockwise from the first.</summary>
        public int index;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        /// <summary>The point whose distance from <paramref name="from"/> is nearest <paramref name="distance"/>, or none in a room with none.</summary>
        public static CutterLane AtDistance(Vector3 from,float distance)
        {
            CutterLane best=null;float least=float.PositiveInfinity;
            foreach(var lane in All)
            {
                if(lane==null)continue;var o=lane.transform.position-from;o.y=0;float off=Mathf.Abs(o.magnitude-distance);
                if(off<least){least=off;best=lane;}
            }
            return best;
        }
    }
}
