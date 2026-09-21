using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/CameraProfile")]
    public sealed class CameraProfile:ScriptableObject
    {
        public float pitch=40,yaw=0,fov=30,distance=23,deadZone=0.7f,lookAhead=0.18f,speedZoom=0.12f;
    }
}
