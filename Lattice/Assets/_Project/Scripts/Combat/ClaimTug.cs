using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Where a salvage frame's tug stands off (chapter 5: Dessik Varn's, off Many Hands' stern): an empty object a builder stands at the flight plane by the tug's drone rack. The frame's
    /// tow drones come in from its side of the fight, so a pilot learns where to look for them. With none in the scene they come from the south, the camera's side, where they are seen
    /// arriving. It marks a direction only: the drones are launched a short flight from the frame, never from wherever the tug is moored.
    /// </summary>
    public sealed class ClaimTug:MonoBehaviour
    {
        public static readonly List<ClaimTug> All=new();
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        /// <summary>The way from a point to the nearest tug on the plane, or south with none.</summary>
        public static Vector3 From(Vector3 point)
        {
            ClaimTug best=null;float least=float.PositiveInfinity;
            foreach(var tug in All)
            {
                if(tug==null)continue;var offset=tug.transform.position-point;offset.y=0;
                if(offset.sqrMagnitude<least){least=offset.sqrMagnitude;best=tug;}
            }
            if(best==null)return Vector3.back;
            var way=best.transform.position-point;way.y=0;return way.sqrMagnitude>.01f?way.normalized:Vector3.back;
        }
    }
}
