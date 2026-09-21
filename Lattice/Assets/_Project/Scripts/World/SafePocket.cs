using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    public sealed class SafePocket:MonoBehaviour
    {
        Collider volume;bool initialized,inside;
        void Awake(){volume=GetComponent<Collider>();}
        void Update()
        {
            if(PartyController.Current==null||ZoneController.Current==null)return;
            bool now=volume.bounds.Contains(PartyController.Current.Active.transform.position+Vector3.up*.5f);
            if(initialized&&now==inside)return;initialized=true;inside=now;ZoneController.Current.SafePocket(inside);
        }
    }
}
