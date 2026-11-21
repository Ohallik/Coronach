using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A place on a hull where a salvage frame can clamp on (chapter 5, Hulljack's first part): an empty object a builder stands at the flight plane over one of the hull's joins,
    /// numbered in the order the frame takes them. The frame is set down on the first; each foot cut from it, it lets go and leaps to the next. With none in the scene (the proving yard)
    /// it leaps a fixed way instead. What the room round a join must hold is in docs/maps/ManyHandsHull.md, "Boss arena contract".
    /// </summary>
    public sealed class HullJoin:MonoBehaviour
    {
        public static readonly List<HullJoin> All=new();
        /// <summary>Its place in the frame's order: the frame goes from each to the next higher one.</summary>
        public int index;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        /// <summary>The join nearest a point on the plane, or none.</summary>
        public static HullJoin Nearest(Vector3 point)
        {
            HullJoin best=null;float least=float.PositiveInfinity;
            foreach(var join in All)
            {
                if(join==null)continue;var offset=join.transform.position-point;offset.y=0;
                if(offset.sqrMagnitude<least){least=offset.sqrMagnitude;best=join;}
            }
            return best;
        }
        /// <summary>The next join in the frame's order after this number, or none: the last one has no next.</summary>
        public static HullJoin After(int index)
        {
            HullJoin best=null;
            foreach(var join in All)if(join!=null&&join.index>index&&(best==null||join.index<best.index))best=join;
            return best;
        }
    }
}
