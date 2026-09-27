using UnityEngine;
namespace Lattice.Tests.PlayMode
{
    public sealed class FlightCollisionWitness:MonoBehaviour
    {
        public Collider observed;
        public int contacts;
        void OnControllerColliderHit(ControllerColliderHit hit){if(hit.collider==observed)contacts++;}
    }
}
