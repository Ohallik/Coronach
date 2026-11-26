using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Core
{
    /// <summary>Hulls that are drawn only, under the flight plane, with nothing solid to a sweep: the Returning Passage's running convoy (ConvoyFlow), the Last Mooring's moored one
    /// (MooredHulls) and the traffic going by (TrafficShip). Each is kept as the box its drawing fills in its own frame, and goes with it as it moves. What flies over one at the
    /// plane is drawn lying on it from the camera's height: Idra's tug lay in a cargo canoe and on a ring hull beside the lead (nov25-r5-chain s68/023, s68/028, s64/017), because
    /// the companion's place in flight read only what is solid, and these live where Combat can't see them (D425). The world fills it; a place is read against it.</summary>
    public static class DrawnHulls
    {
        sealed class Hull{public Transform at;public Vector3 middle,half;public float reach;public object owner;}
        static readonly List<Hull> hulls=new();
        /// <summary>How deep under a place a hull is read as lying under it: the moored convoy's tops are 4 to 18 m under the plane in its arenas, the passage's lanes 8 to 12; the
        /// deep convoy (24 m and more) is far enough below to read as below.</summary>
        public const float Deepest=20;
        public static int Count=>hulls.Count;
        /// <summary>Takes in a hull by what it draws (its renderers, not its particles or lines), boxed in its own frame. Nothing drawn, nothing taken. <paramref name="owner"/> is
        /// what lets it go (<see cref="Remove"/>); one destroyed goes by itself.</summary>
        public static void Add(Transform hull,object owner)
        {
            if(hull==null)return;
            var toLocal=hull.worldToLocalMatrix;bool any=false;var box=new Bounds();
            foreach(var r in hull.GetComponentsInChildren<Renderer>())
            {
                if(r is ParticleSystemRenderer||r is TrailRenderer||r is LineRenderer)continue;
                var local=r.localBounds;var m=toLocal*r.transform.localToWorldMatrix;
                for(int i=0;i<8;i++)
                {
                    var corner=local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var p=m.MultiplyPoint3x4(corner);
                    if(!any){box=new Bounds(p,Vector3.zero);any=true;}else box.Encapsulate(p);
                }
            }
            if(!any)return;
            var scale=hull.lossyScale;float most=Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            hulls.Add(new Hull{at=hull,middle=box.center,half=box.extents,reach=(box.center.magnitude+box.extents.magnitude)*most,owner=owner});
        }
        public static void Remove(object owner)
        {
            for(int i=hulls.Count-1;i>=0;i--)if(hulls[i].at==null||Equals(hulls[i].owner,owner))hulls.RemoveAt(i);
        }
        /// <summary>Whether a drawn hull lies under a place at the plane as <paramref name="view"/> sees it (along its ray through the place, on down; straight down with no camera),
        /// to within <paramref name="room"/> of its box and no more than <paramref name="deepest"/> under the place.</summary>
        public static bool Under(Vector3 place,float room,Camera view,float deepest=Deepest)
        {
            if(hulls.Count==0)return false;
            var way=view!=null?place-view.transform.position:Vector3.down;
            if(way.y>-.01f)return false;
            way/=-way.y;
            // The ray from the place on down to the deepest read, in metres down: place + way*t, t from nought to deepest.
            float sideways=new Vector2(way.x,way.z).magnitude*deepest;
            for(int i=hulls.Count-1;i>=0;i--)
            {
                var h=hulls[i];if(h.at==null){hulls.RemoveAt(i);continue;}
                if(!h.at.gameObject.activeInHierarchy)continue;
                var pivot=h.at.position;float dx=pivot.x-place.x,dz=pivot.z-place.z,near=h.reach+room+sideways;
                if(dx*dx+dz*dz>near*near||pivot.y-h.reach>place.y||pivot.y+h.reach<place.y-deepest)continue;
                var m=h.at.worldToLocalMatrix;var o=m.MultiplyPoint3x4(place);var d=m.MultiplyVector(way);
                var scale=h.at.lossyScale;var grow=new Vector3(room/Mathf.Max(.001f,Mathf.Abs(scale.x)),0,room/Mathf.Max(.001f,Mathf.Abs(scale.z)));
                if(Hits(o,d,h.middle-h.half-grow,h.middle+h.half+grow,deepest))return true;
            }
            return false;
        }
        /// <summary>Whether the segment o + d*t, t in [0, most], passes through the box.</summary>
        static bool Hits(Vector3 o,Vector3 d,Vector3 lo,Vector3 hi,float most)
        {
            float near=0,far=most;
            for(int axis=0;axis<3;axis++)
            {
                float start=o[axis],step=d[axis];
                if(Mathf.Abs(step)<1e-6f){if(start<lo[axis]||start>hi[axis])return false;continue;}
                float a=(lo[axis]-start)/step,b=(hi[axis]-start)/step;if(a>b)(a,b)=(b,a);
                near=Mathf.Max(near,a);far=Mathf.Min(far,b);if(near>far)return false;
            }
            return true;
        }
    }
}
