using System.Collections.Generic;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>The drawn bodies of the large things round a ship in flight, read on the plan, for the companion's way out of a marking (PartnerBrain.WayOutInFlight). Since D323
    /// she leaves every enemy's marking in flight, and a way out read off the marking alone took her across the Cantor's body: inside its volley cone, with the lead in her
    /// way out, the roll's fallback for shots (the other way) bent toward the head and left her in front of it as it swam on into her (CompanionTravelTests, November 20).
    /// What is kept off a body is what FlightSeparation keeps her travel off the Cantor's by: the hull and <see cref="Margin"/> past what is drawn.</summary>
    sealed class FlightBodies
    {
        /// <summary>A body is large when what is drawn of it reaches further than this from its middle on the plan: more than any hero's hull (Idra's is 2.17).</summary>
        public const float Large=2.5f;
        /// <summary>Past the hull, the room kept from what is drawn: FlightSeparation's margin for her travel round the Cantor.</summary>
        public const float Margin=.8f;
        /// <summary>A body in motion has the next this-many seconds of its travel kept clear as well (the head that swam into her came on at 2 m/s); and nothing past this reach is read.</summary>
        const float Ahead=.6f,Reach=30,Step=.5f;
        struct Box{public Vector2 min,max;}
        readonly List<Box> boxes=new();
        readonly Dictionary<Health,Renderer[]> drawn=new();
        readonly List<Renderer> scratch=new();
        int gathered=-1;
        public bool Any=>boxes.Count>0;
        static Vector2 Plan(Vector3 p)=>new(p.x,p.z);
        /// <summary>Read once a frame: every drawn part of each large enemy body within reach, alive or on its way out, each part's box swept along its body's motion.</summary>
        public void Gather(Vector3 near)
        {
            if(gathered==Time.frameCount)return;gathered=Time.frameCount;boxes.Clear();
            if(drawn.Count>64)drawn.Clear();
            foreach(var body in Health.All)
            {
                if(body==null||body.friendly||!body.gameObject.activeInHierarchy||!body.TryGetComponent<EnemyBrain>(out _))continue;
                if(Plan(body.transform.position-near).sqrMagnitude>Reach*Reach)continue;
                if(!drawn.TryGetValue(body,out var parts)||parts==null)
                {
                    scratch.Clear();
                    foreach(var r in body.GetComponentsInChildren<Renderer>(true))if(r is MeshRenderer||r is SkinnedMeshRenderer)scratch.Add(r);
                    drawn[body]=parts=scratch.ToArray();
                }
                bool any=false;var whole=new Bounds();
                foreach(var part in parts)
                {
                    if(part==null||!part.enabled||!part.gameObject.activeInHierarchy)continue;
                    if(!any){whole=part.bounds;any=true;}else whole.Encapsulate(part.bounds);
                }
                if(!any||Mathf.Max(whole.extents.x,whole.extents.z)<Large)continue;
                var motion=body.TryGetComponent<CharacterController>(out var controller)&&controller.enabled?controller.velocity:Vector3.zero;
                var lead=Plan(motion)*Ahead;if(lead.sqrMagnitude<.09f)lead=Vector2.zero;
                foreach(var part in parts)
                {
                    if(part==null||!part.enabled||!part.gameObject.activeInHierarchy)continue;
                    var b=part.bounds;var min=Plan(b.min);var max=Plan(b.max);
                    boxes.Add(new Box{min=Vector2.Min(min,min+lead),max=Vector2.Max(max,max+lead)});
                }
            }
        }
        /// <summary>From a point, how far past the hull and the margin it is from a box (negative inside that room).</summary>
        static float Off(Box box,Vector2 p,float hull)
        {
            var nearest=Vector2.Max(box.min,Vector2.Min(box.max,p));
            return (p-nearest).magnitude-hull-Margin;
        }
        /// <summary>The least room a hull keeps from the large bodies on a straight way from one point to another: negative where the way comes within the hull and the margin of what is
        /// drawn. As her travel round the Cantor does (FlightSeparation.Clear), a way that starts inside that room and leaves it outward is held to where it ends, not where it starts.</summary>
        public float Room(Vector3 from,Vector3 to,float hull)
        {
            var a=Plan(from);var b=Plan(to);var d=b-a;float length=d.magnitude;int steps=Mathf.Max(1,Mathf.CeilToInt(length/Step));
            float least=float.PositiveInfinity;
            foreach(var box in boxes)
            {
                float start=Off(box,a,hull),room;
                if(start<0&&Vector2.Dot(d,a-(box.min+box.max)*.5f)>=0)room=Off(box,b,hull);
                else
                {
                    room=start;
                    for(int i=1;i<=steps;i++)room=Mathf.Min(room,Off(box,a+d*(i/(float)steps),hull));
                }
                least=Mathf.Min(least,room);
            }
            return least;
        }
    }
}
