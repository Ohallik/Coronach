using UnityEngine;

namespace Lattice.Combat
{
    public static class CombatCover
    {
        public static bool Opaque(Collider collider)=>!collider.isTrigger&&collider.GetComponentInParent<Health>()==null;
        public static bool Sweep(Vector3 from,Vector3 to,float radius,out RaycastHit nearest)
        {
            var delta=to-from;nearest=default;float distance=delta.magnitude;bool found=false;
            if(distance<.00001f)return false;
            foreach(var hit in Physics.SphereCastAll(from,radius,delta/distance,distance,~0,QueryTriggerInteraction.Ignore))
                if(Opaque(hit.collider)&&hit.distance<=distance){nearest=hit;distance=hit.distance;found=true;}
            return found;
        }
        public static bool Clear(Vector3 from,Vector3 to)=>!Sweep(from,to,.01f,out _);
        public static Vector3 Ground(Vector3 position)
        {
            // A probe that starts inside a solid gets that solid back at distance 0 with a zero point, which read as the world's origin:
            // skip it, and what's left is the ground under it.
            var from=position+Vector3.up*.05f;float nearest=float.PositiveInfinity;var ground=position;
            foreach(var hit in Physics.SphereCastAll(from,.01f,Vector3.down,8.05f,~0,QueryTriggerInteraction.Ignore))
            {
                if(!Opaque(hit.collider)||hit.distance<=0&&hit.point==Vector3.zero)continue;
                if(hit.distance<nearest){nearest=hit.distance;ground=hit.point;}
            }
            return ground;
        }
    }
}
