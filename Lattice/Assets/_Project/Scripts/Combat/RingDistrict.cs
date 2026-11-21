using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// One of Cinder Halo's districts flying out through the Broken Ring (chapter 9): on the object that carries the district's cover at the flight plane (its solid collider, or the root over them).
    /// It is cover by the ordinary rule (<see cref="CombatCover"/>): the Debris Cutter's beam and its tractor's line stop at it, and it never lays a mine inside one. This says which district it is,
    /// so the fight can name where its beam struck (Mira's line when it is the market) and which one it turned wide round. Never a bar, never a score: the districts are landmarks and cover.
    /// The Ring's contract: docs/maps/BrokenRing.md, "Cutter arena contract".
    /// </summary>
    public sealed class RingDistrict:MonoBehaviour
    {
        public static readonly List<RingDistrict> All=new();
        /// <summary><c>market</c>, <c>repair</c>, <c>crescent</c> or <c>dock</c>; and how a player would say it ("the market").</summary>
        public string id,displayName;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        /// <summary>The district a collider belongs to, if it is one's.</summary>
        public static RingDistrict Of(Collider collider)=>collider!=null?collider.GetComponentInParent<RingDistrict>():null;
        /// <summary>The district whose cover is nearest a point on the plane, within a reach, or none.</summary>
        public static RingDistrict Nearest(Vector3 point,float within)
        {
            RingDistrict best=null;float least=within;
            foreach(var district in All)
            {
                if(district==null)continue;
                float gap=float.PositiveInfinity;
                foreach(var cover in district.GetComponentsInChildren<Collider>()){if(cover.isTrigger||!cover.enabled)continue;var near=cover.ClosestPoint(point);near.y=point.y;gap=Mathf.Min(gap,Vector3.Distance(near,point));}
                if(gap<least){least=gap;best=district;}
            }
            return best;
        }
        /// <summary>Whether a point at the plane is inside a district's cover: nothing is laid there.</summary>
        public static bool Inside(Vector3 point,float radius)
        {
            foreach(var hit in Physics.OverlapSphere(point,radius,~0,QueryTriggerInteraction.Ignore))if(Of(hit)!=null)return true;
            return false;
        }
    }
}
