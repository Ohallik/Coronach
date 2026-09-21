using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    public sealed class SafePocket:MonoBehaviour
    {
        void OnTriggerEnter(Collider other){if(other.gameObject==PartyController.Current.Active.gameObject)ZoneController.Current.SafePocket(true);}
        void OnTriggerExit(Collider other){if(PartyController.Current!=null&&other.gameObject==PartyController.Current.Active.gameObject)ZoneController.Current.SafePocket(false);}
    }
}
