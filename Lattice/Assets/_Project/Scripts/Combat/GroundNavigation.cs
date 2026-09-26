using UnityEngine;
using UnityEngine.AI;

namespace Lattice.Combat
{
    /// <summary>Baked walkable rooms support companion steering; the existing
    /// CharacterController and GroundMotor still own all displacement.</summary>
    public sealed class GroundNavigation:MonoBehaviour
    {
        public NavMeshData data;
        NavMeshDataInstance instance;
        public static GroundNavigation Current{get;private set;}
        void OnEnable(){if(data!=null){instance=NavMesh.AddNavMeshData(data);Current=this;}}
        void OnDisable(){if(instance.valid)instance.Remove();if(Current==this)Current=null;}
        public bool FindPath(Vector3 from,Vector3 goal,Vector3 leader,NavMeshPath path)
        {
            if(!NavMesh.SamplePosition(from,out var start,1.2f,NavMesh.AllAreas))return false;
            // A formation offset can lie inside a wall. Fall back to the leader's
            // reachable floor rather than manufacturing a point beyond the hull.
            if(!NavMesh.SamplePosition(goal,out var end,1.5f,NavMesh.AllAreas)&&
               !NavMesh.SamplePosition(leader,out end,1.2f,NavMesh.AllAreas))return false;
            if(NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)return true;
            return NavMesh.SamplePosition(leader,out end,1.2f,NavMesh.AllAreas)&&
                NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
        }
    }
}
