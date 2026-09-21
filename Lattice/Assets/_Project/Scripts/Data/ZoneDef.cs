using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/ZoneDef")]
    public sealed class ZoneDef:ScriptableObject
    {
        public string id,scene; public ZoneKind kind; public CameraProfile cameraProfile; public Hd2dProfile hd2dProfile; public AudioClip ambientAudio; public string musicId; public Sprite mapIcon; public float flightPlane=1; public string[] safePockets;
        public Material skybox;
    }
}
