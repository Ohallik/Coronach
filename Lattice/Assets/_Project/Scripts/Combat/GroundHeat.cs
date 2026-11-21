using System;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// How hot the ground is at a point, for a body that has to read it: chapter 8's Ash Crawler flees bright flow and won't hide under crust about to go. The crust itself is the
    /// world's (Lattice.World.CrustField, the Scoria Flows); combat can't see that assembly, so the field registers its answer here while it is enabled. Anywhere without one the
    /// ground is cold. 0 is ground that never cycles or dark crust, .5 a lit seam (about to go), 1 open melt (it burns).
    /// </summary>
    public static class GroundHeat
    {
        static Func<Vector3,float> source;static UnityEngine.Object owner;
        public static void Register(UnityEngine.Object by,Func<Vector3,float> heat){owner=by;source=heat;}
        public static void Unregister(UnityEngine.Object by){if(owner==by){owner=null;source=null;}}
        public static float Of(Vector3 point)=>source!=null&&owner!=null?source(point):0;
        /// <summary>Open melt: a body standing here is burnt.</summary>
        public static bool Bright(Vector3 point)=>Of(point)>=.99f;
        /// <summary>A lit seam or open melt: nowhere to stand for long.</summary>
        public static bool Hot(Vector3 point)=>Of(point)>=.49f;
    }
}
