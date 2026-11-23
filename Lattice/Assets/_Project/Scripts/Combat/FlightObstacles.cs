using System.Collections.Generic;
using UnityEngine;

namespace Lattice.Combat
{
    // Local guidance for the companion's ordinary thrust. Query the actual
    // solid scene at hull height; a formation point is not always flyable.
    sealed class FlightObstacles
    {
        readonly RaycastHit[] hits=new RaycastHit[64];
        readonly Collider[] touching=new Collider[24];
        readonly CharacterController body;
        /// <summary>The room her sweeps keep past her hull, and what they come down to where she already sits closer than that to something solid. A sweep that starts overlapping
        /// a solid meets it at nought (PhysX reports such a sweep at distance 0 whichever way it goes), so from inside that room every way read shut, the plan was to stay where she
        /// was, and nothing ever moved her again: Sela's hull came to rest six centimetres off barge two's loose pane in its skimmers' second round and she sat there to the end of
        /// Xylos' nearspace (the whole-game capture of November 20, s17; PartnerFlightRejoinTests). Each plan now sweeps with the widest of these that is clear of everything where
        /// she is, and from inside a solid (one that came through her) with the core of her hull.</summary>
        public const float Skin=.3f;
        /// <summary>The most a ship flies while its way is turned round something solid.</summary>
        const float CornerPace=8;
        static readonly float[] Skins={Skin,.12f,.03f};
        const float Core=-.4f;
        /// <summary>A goal further than this (past a fight's 18 m and the frame's reach of her) is first sought along the lead's own way, unbraked, before one corner, which is braked.
        /// A nearer one never is.</summary>
        const float FarGoal=20;
        float skin=Skin;
        Vector3 waypoint,lastGoal;
        float nextPlan;
        bool redirected,corner,trailing,braked;
        public FlightObstacles(CharacterController body){this.body=body;}
        float Radius=>body.radius+skin;
        /// <summary>The body she is fighting and the part of it she aims at: closed on to her stand-off (PartnerBrain.ShipStandOff), never steered round. Set before each Guide.</summary>
        public Health Fighting,Part;
        /// <summary>Living bodies near her, each as its middle and how far what it draws reaches round it (EnemyBrain.BodyRadius): kept off by her hull and the room her sweeps keep,
        /// as a solid is. A body has no solid to a sweep (it has Health), so her steering flew straight through any in her way, and a Chorister Drifter's capsule is half what it draws
        /// (nov20-whole s10b/089; BodiesKeepApartTests). The animal on a collar is kept off by its own guide (FlightSeparation); a part of a body counts as its body.</summary>
        readonly List<(Vector3 at,float radius)> bodies=new();
        void Gather(Vector3 near)
        {
            bodies.Clear();
            foreach(var living in Health.All)
            {
                if(living==null||living.friendly||!living.Alive||living==Fighting||living==Part||living.reactionOwner!=null)continue;
                if(!living.TryGetComponent<EnemyBrain>(out var brain)||living.TryGetComponent<SerpentSegments>(out _))continue;
                var at=living.transform.position;var off=at-near;
                if(Mathf.Abs(off.y)>3||off.x*off.x+off.z*off.z>60*60)continue;
                bodies.Add((at,brain.BodyRadius));
            }
        }
        float FreeDistance(Vector3 from,Vector3 to)
        {
            var delta=to-from;float distance=delta.magnitude;
            if(distance<.001f)return distance;
            int count=Physics.SphereCastNonAlloc(from+body.center,Radius,delta/distance,hits,distance,~0,QueryTriggerInteraction.Ignore);
            if(count==hits.Length)return 0;
            for(int i=0;i<count;i++)if(CombatCover.Opaque(hits[i].collider))distance=Mathf.Min(distance,hits[i].distance);
            // A body she already stands within the room of is no wall to leaving it: only one ahead of her on the way counts.
            var way=new Vector3(delta.x,0,delta.z);float run=way.magnitude;
            if(run>.001f)
            {
                way/=run;
                foreach(var (at,radius) in bodies)
                {
                    var rel=at-from;rel.y=0;float reach=Radius+radius;
                    if(rel.sqrMagnitude<=reach*reach)continue;
                    float along=Vector3.Dot(rel,way);if(along<=0)continue;
                    float aside=rel.sqrMagnitude-along*along;if(aside>=reach*reach)continue;
                    distance=Mathf.Min(distance,Mathf.Max(0,(along-Mathf.Sqrt(reach*reach-aside))*distance/run));
                }
            }
            return distance;
        }
        bool Clear(Vector3 from,Vector3 to)=>FreeDistance(from,to)>=(to-from).magnitude-.001f;
        bool Touches(Vector3 position,float room)
        {
            int count=Physics.OverlapSphereNonAlloc(position+body.center,body.radius+room,touching,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(CombatCover.Opaque(touching[i]))return true;
            return false;
        }
        /// <summary>The widest room a sweep from here can keep and still start clear.</summary>
        float SkinAt(Vector3 position)
        {
            foreach(float room in Skins)if(!Touches(position,room))return room;
            return body.radius*Core;
        }
        /// <summary>Where to steer for the goal: straight at it; else, near, round one corner; far, along the lead's own way first (<paramref name="trail"/>, oldest first: where his
        /// hull went, hers goes), to the newest point of it she can reach straight from where she is, and then round one corner. Straight and one corner were all there was: with the lead
        /// round a bend she stopped at the first solid on the line to him and waited there for as long as he stayed beyond it (left behind at Nacre Grave's inner tows, Idra stopped at
        /// the first wall of the inner pass for good; PartnerFlightRejoinTests).</summary>
        public Vector3 Guide(Vector3 position,Vector3 goal,Vector3 velocity,IReadOnlyList<Vector3> trail,out bool adjusted,out bool detour,out bool brake)
        {
            goal.y=position.y;velocity.y=0;skin=SkinAt(position);Gather(position);
            if(Time.unscaledTime>=nextPlan||(lastGoal-goal).sqrMagnitude>1||(waypoint-position).sqrMagnitude<.25f)
            {
                nextPlan=Time.unscaledTime+.15f;lastGoal=goal;Plan(position,goal,trail);
            }
            adjusted=redirected;detour=corner;
            // Braking covers current momentum as well as the requested path.
            // The flight motor's brake damps at 9/s; retain a clearance margin.
            // Along the lead's way she flies at his pace: braked all the way (about 2 m/s) a way back of a hundred metres took most of a minute. Round a corner she is held to
            // CornerPace, not to the brake's crawl: braked to 2 m/s round each crystal of the Aurora Shear's short lane she was left twenty metres behind a lead flying it and off the
            // frame for a second at a time (PartnerInViewTests). What is close ahead of her brakes her whatever she is doing.
            brake=redirected&&!trailing&&velocity.magnitude>CornerPace||!Clear(position,position+velocity/9+velocity.normalized*.6f);braked=brake;
            return redirected?waypoint:goal;
        }
        void Plan(Vector3 start,Vector3 goal,IReadOnlyList<Vector3> trail)
        {
            float distance=(goal-start).magnitude,free=FreeDistance(start,goal);
            redirected=free<distance-.001f;corner=trailing=false;waypoint=goal;
            if(!redirected)return;
            bool far=distance>FarGoal;
            if(far&&Along(start,trail))return;
            // First try a two-leg path around the obstruction. Bounded rings
            // handle nearby folds without assuming a map's wall coordinates.
            float shortest=float.PositiveInfinity;
            for(int ring=1;ring<=4;ring++)for(int spoke=0;spoke<16;spoke++)
            {
                float angle=spoke*Mathf.PI/8;
                var point=start+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(ring*4);
                float length=ring*4+Vector3.Distance(point,goal);
                if(length>=shortest||!Clear(start,point)||!Clear(point,goal))continue;
                shortest=length;waypoint=point;corner=true;
            }
            if(corner)return;
            // Near, his way is not hers: a fight's target behind a fold sent her to his last point and back, flat out, and she never fired (CompanionWallTests' change of target).
            // A requested formation point may be inside tissue. Stop on this
            // side with the full hull margin instead of thrusting into it.
            waypoint=start+(goal-start).normalized*Mathf.Max(0,free-.15f);
        }
        /// <summary>The lead's way: the newest point of it she can fly to straight, with a few centimetres past her hull where he went with a few past his. A point under a metre off
        /// is where she already is: past it his way is shut to her hull (he went through a gap she can't), and there is nothing of it to follow.</summary>
        bool Along(Vector3 start,IReadOnlyList<Vector3> trail)
        {
            if(trail==null||trail.Count==0)return false;
            float kept=skin;skin=Mathf.Min(skin,Skins[Skins.Length-1]);
            try
            {
                for(int i=trail.Count-1;i>=0;i--)
                {
                    var point=trail[i];point.y=start.y;
                    if((point-start).sqrMagnitude<1)return false;
                    if(!Clear(start,point))continue;
                    waypoint=point;corner=trailing=true;return true;
                }
                return false;
            }
            finally{skin=kept;}
        }
    }
}
