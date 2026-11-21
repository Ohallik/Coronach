using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A sensor shadow (chapter 8's Picket of Teeth: staged detection): a rock in a flight lane, as a circle on the flight plane as wide as the rock, that nothing watching from one side of it
    /// can see past. The picket's buoys draw their beams cut by it (Lattice.World.SensorSweep), and a hunter can ask whether it still has a line to the ship it hunts
    /// (<see cref="Blocks"/>). Here in Combat so an enemy's behaviour can read it; the zone puts one on each rock. Every shade is a circle on the plane: the drawn rock tumbles, its shadow doesn't.
    /// </summary>
    public sealed class SensorShade:MonoBehaviour
    {
        /// <summary>The shadow's radius on the plane, about the rock's own.</summary>
        public float radius=5;
        static readonly List<SensorShade> all=new();
        public static IReadOnlyList<SensorShade> All=>all;
        public Vector2 Centre=>new(transform.position.x,transform.position.z);
        void OnEnable(){if(!all.Contains(this))all.Add(this);}
        void OnDisable()=>all.Remove(this);
        /// <summary>Whether a rock stands between two points on the plane: the segment passes inside a shade's circle that holds neither end. Height is ignored.</summary>
        public static bool Blocks(Vector3 from,Vector3 to)=>Blocks(new Vector2(from.x,from.z),new Vector2(to.x,to.z),null);
        /// <summary>The same against a given list of circles (centre, radius), for a test or a builder that has no scene.</summary>
        public static bool Blocks(Vector2 from,Vector2 to,IReadOnlyList<(Vector2 centre,float radius)> shades)
        {
            var d=to-from;float length2=d.sqrMagnitude;
            if(shades!=null){foreach(var (c,r) in shades)if(Cuts(from,d,length2,c,r))return true;return false;}
            foreach(var shade in all)if(shade!=null&&Cuts(from,d,length2,shade.Centre,shade.radius))return true;
            return false;
        }
        static bool Cuts(Vector2 from,Vector2 d,float length2,Vector2 c,float r)
        {
            if((c-from).sqrMagnitude<=r*r)return false;
            float t=Mathf.Clamp01(Vector2.Dot(c-from,d)/Mathf.Max(1e-6f,length2));
            return (from+d*t-c).sqrMagnitude<r*r;
        }
        /// <summary>How far along a ray from <paramref name="from"/> the first shade cuts it (its near edge), or <paramref name="reach"/> if none does before.</summary>
        public static float Reach(Vector2 from,Vector2 direction,float reach)
        {
            float best=reach;
            foreach(var shade in all)if(shade!=null)best=Mathf.Min(best,Hit(from,direction,shade.Centre,shade.radius,reach));
            return best;
        }
        /// <summary>The same against a given list of circles.</summary>
        public static float Reach(Vector2 from,Vector2 direction,float reach,IReadOnlyList<(Vector2 centre,float radius)> shades)
        {
            float best=reach;foreach(var (c,r) in shades)best=Mathf.Min(best,Hit(from,direction,c,r,reach));return best;
        }
        static float Hit(Vector2 from,Vector2 direction,Vector2 c,float r,float reach)
        {
            var f=from-c;if(f.sqrMagnitude<=r*r)return reach;
            float b=Vector2.Dot(f,direction),q=f.sqrMagnitude-r*r,disc=b*b-q;if(disc<0)return reach;
            float t=-b-Mathf.Sqrt(disc);return t>0&&t<reach?t:reach;
        }
    }
}
