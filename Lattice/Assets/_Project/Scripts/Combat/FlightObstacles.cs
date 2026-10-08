using UnityEngine;

namespace Lattice.Combat
{
    // Local guidance for the companion's ordinary thrust. Query the actual
    // solid scene at hull height; a formation point is not always flyable.
    sealed class FlightObstacles
    {
        readonly RaycastHit[] hits=new RaycastHit[64];
        readonly float radius;
        readonly Vector3 center;
        Vector3 waypoint,lastGoal;
        float nextPlan;
        bool redirected,corner;
        public FlightObstacles(CharacterController body)
        {radius=body.radius+.3f;center=body.center;}
        float FreeDistance(Vector3 from,Vector3 to)
        {
            var delta=to-from;float distance=delta.magnitude;
            if(distance<.001f)return distance;
            int count=Physics.SphereCastNonAlloc(from+center,radius,delta/distance,hits,distance,~0,QueryTriggerInteraction.Ignore);
            if(count==hits.Length)return 0;
            for(int i=0;i<count;i++)if(CombatCover.Opaque(hits[i].collider))distance=Mathf.Min(distance,hits[i].distance);
            return distance;
        }
        bool Clear(Vector3 from,Vector3 to)=>FreeDistance(from,to)>=(to-from).magnitude-.001f;
        public Vector3 Guide(Vector3 position,Vector3 goal,Vector3 velocity,out bool adjusted,out bool detour,out bool brake)
        {
            goal.y=position.y;velocity.y=0;
            if(Time.unscaledTime>=nextPlan||(lastGoal-goal).sqrMagnitude>1||(waypoint-position).sqrMagnitude<.25f)
            {
                nextPlan=Time.unscaledTime+.15f;lastGoal=goal;Plan(position,goal);
            }
            adjusted=redirected;detour=corner;
            // Braking covers current momentum as well as the requested path.
            // The flight motor's brake damps at 9/s; retain a clearance margin.
            brake=redirected||!Clear(position,position+velocity/9+velocity.normalized*.6f);
            return redirected?waypoint:goal;
        }
        void Plan(Vector3 start,Vector3 goal)
        {
            float distance=(goal-start).magnitude,free=FreeDistance(start,goal);
            redirected=free<distance-.001f;corner=false;waypoint=goal;
            if(!redirected)return;
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
            // A requested formation point may be inside tissue. Stop on this
            // side with the full hull margin instead of thrusting into it.
            waypoint=start+(goal-start).normalized*Mathf.Max(0,free-.15f);
        }
    }
}
