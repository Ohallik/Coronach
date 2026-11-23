using System.Collections.Generic;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>
    /// Keeps the party's ships out of a big flying body where it is drawn. A flier's solid is its capsule, a vertical circle round its middle; the Debris Cutter's hull is drawn about twice
    /// as long as that, so Idra's tug flew into its underside mid-fight and stayed there (whole-game capture, November 20, s61/065). This holds the outline on the plan of what is drawn of
    /// the body about the ships' height (convex, in the body's own frame, measured once from its meshes as it wakes) and, after everything has moved each frame, puts any ship whose hull
    /// has come inside it back to its edge. It is not a collider, so nothing about what is struck changes: shots and weak spots are the capsule's as before, and a lunge still goes in to
    /// strike (a ship mid-lunge is let be until it has finished), only to be put out of the hull after. Switched off while the body means to pass through the party (the Cutter's run).
    /// </summary>
    public sealed class HullKeepsOff:MonoBehaviour
    {
        /// <summary>The outline, counter-clockwise on the plan in the body's root frame (x, z), and the band of height about the root it stands in.</summary>
        public Vector2[] outline=System.Array.Empty<Vector2>();
        public float low=-1,high=2.6f;
        /// <summary>The furthest the outline reaches from the root, for the log and a quick skip.</summary>
        public float Reach{get;private set;}
        readonly Dictionary<FlightMotor,(int sequence,float until)> lunges=new();        const float LungeHolds=.3f;

        /// <summary>Measures what is drawn under <paramref name="drawn"/> between <paramref name="low"/> and <paramref name="high"/> about <paramref name="body"/>'s root and keeps the ships
        /// off it. Meshes that can't be read at run time are taken by their boxes.</summary>
        public static HullKeepsOff Fit(GameObject body,Transform drawn,float low,float high)
        {
            var keeps=body.GetComponent<HullKeepsOff>()??body.AddComponent<HullKeepsOff>();keeps.low=low;keeps.high=high;
            keeps.outline=Outline(body,drawn,low,high);keeps.Reach=Furthest(keeps.outline);
            return keeps;
        }
        /// <summary>The furthest an outline reaches from its root.</summary>
        public static float Furthest(Vector2[] outline){float most=0;foreach(var p in outline)most=Mathf.Max(most,p.magnitude);return most;}
        /// <summary>The convex outline on the plan, in the root's frame, of what is drawn under <paramref name="drawn"/> between two heights about the root.</summary>
        public static Vector2[] Outline(GameObject body,Transform drawn,float low,float high)
        {
            var points=new List<Vector2>();var toRoot=body.transform.worldToLocalMatrix;
            if(drawn!=null)foreach(var filter in drawn.GetComponentsInChildren<MeshFilter>())
            {
                var view=filter.GetComponent<MeshRenderer>();var mesh=filter.sharedMesh;if(mesh==null||view==null||!view.enabled)continue;
                var m=toRoot*filter.transform.localToWorldMatrix;
                if(!mesh.isReadable)
                {
                    var b=mesh.bounds;
                    for(int i=0;i<8;i++){var p=m.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(p.y>=low&&p.y<=high)points.Add(new Vector2(p.x,p.z));}
                    continue;
                }
                var v=mesh.vertices;var t=mesh.triangles;var at=new Vector3[v.Length];for(int i=0;i<v.Length;i++)at[i]=m.MultiplyPoint3x4(v[i]);
                // Every triangle cut to the band, not only its corners inside it: a hull's long faces cross the band with their corners above and below it.
                var poly=new List<Vector3>(6);var kept=new List<Vector3>(6);
                for(int i=0;i+2<t.Length;i+=3)
                {
                    poly.Clear();poly.Add(at[t[i]]);poly.Add(at[t[i+1]]);poly.Add(at[t[i+2]]);
                    Slab(poly,low,true,kept);Slab(kept,high,false,poly);
                    foreach(var p in poly)points.Add(new Vector2(p.x,p.z));
                }
            }
            return Hull(points);
        }
        /// <summary>A polygon cut to one side of a level: what of it lies over (<paramref name="keepAbove"/>) or under <paramref name="y"/>, into <paramref name="into"/>.</summary>
        static void Slab(List<Vector3> poly,float y,bool keepAbove,List<Vector3> into)
        {
            var source=new List<Vector3>(poly);into.Clear();
            for(int i=0;i<source.Count;i++)
            {
                var a=source[i];var b=source[(i+1)%source.Count];bool inA=keepAbove?a.y>=y:a.y<=y,inB=keepAbove?b.y>=y:b.y<=y;
                if(inA)into.Add(a);if(inA!=inB)into.Add(Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y)));
            }
        }
        /// <summary>The outline as the furthest point each way round, 40 ways, in order: convex and counter-clockwise.</summary>
        static Vector2[] Hull(List<Vector2> points)
        {
            if(points.Count<3)return System.Array.Empty<Vector2>();
            const int Ways=40;var ring=new List<Vector2>();
            for(int k=0;k<Ways;k++)
            {
                float a=k*Mathf.PI*2/Ways;var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var far=points[0];float most=float.NegativeInfinity;
                foreach(var p in points){float d=Vector2.Dot(p,u);if(d>most){most=d;far=p;}}
                if(ring.Count==0||(ring[ring.Count-1]-far).sqrMagnitude>1e-5f)ring.Add(far);
            }
            if(ring.Count>1&&(ring[0]-ring[ring.Count-1]).sqrMagnitude<1e-5f)ring.RemoveAt(ring.Count-1);
            return ring.Count>=3?ring.ToArray():System.Array.Empty<Vector2>();
        }
        /// <summary>Signed distance from a point of the plan in the body's frame to the outline (negative inside), and the way out of it from there.</summary>
        public float Distance(Vector2 p,out Vector2 outward)
        {
            outward=Vector2.zero;int n=outline.Length;if(n<3)return float.PositiveInfinity;
            float deepest=float.NegativeInfinity;Vector2 deepestOut=Vector2.zero;bool inside=true;float nearest=float.PositiveInfinity;Vector2 nearestPoint=p;
            for(int i=0;i<n;i++)
            {
                var a=outline[i];var b=outline[(i+1)%n];var e=b-a;if(e.sqrMagnitude<1e-8f)continue;var normal=new Vector2(e.y,-e.x).normalized;
                float side=Vector2.Dot(p-a,normal);if(side>0)inside=false;if(side>deepest){deepest=side;deepestOut=normal;}
                float t=Mathf.Clamp01(Vector2.Dot(p-a,e)/e.sqrMagnitude);var q=a+e*t;float d=(p-q).magnitude;if(d<nearest){nearest=d;nearestPoint=q;}
            }
            if(inside){outward=deepestOut;return deepest;}
            outward=nearest>1e-5f?(p-nearestPoint)/nearest:deepestOut;return nearest;
        }
        void LateUpdate()
        {
            var party=PartyController.Current;if(party==null||outline.Length<3)return;
            foreach(var ship in party.members)
            {
                if(ship==null||!ship.flight||!ship.gameObject.activeInHierarchy||ship.Health==null||!ship.Health.Alive)continue;
                if(ship.motor is FlightMotor motor)
                {
                    int sequence=motor.DashSequence;
                    if(!lunges.TryGetValue(motor,out var seen)){lunges[motor]=(sequence,0);}
                    else if(seen.sequence!=sequence)lunges[motor]=(sequence,Time.time+LungeHolds);
                    if(Time.time<lunges[motor].until)continue;
                }
                float radius=HeroCollision.HullRadius(ship.character);
                var local=transform.InverseTransformPoint(ship.transform.position);float middle=local.y+.8f;
                if(middle<low-radius||middle>high+radius)continue;
                var plan=new Vector2(local.x,local.z);if(plan.magnitude>Reach+radius)continue;
                float d=Distance(plan,out var outward);if(d>=radius)continue;
                var push=transform.TransformDirection(new Vector3(outward.x,0,outward.y))*(radius-d);push.y=0;
                var body=ship.GetComponent<CharacterController>();
                if(body!=null&&body.enabled)body.Move(push);else ship.transform.position+=push;
            }
        }
    }
}
