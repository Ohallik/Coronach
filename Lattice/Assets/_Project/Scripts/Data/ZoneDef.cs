using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/ZoneDef")]
    public sealed class ZoneDef:ScriptableObject
    {
        public string id,scene; public ZoneKind kind; public CameraProfile cameraProfile; public Hd2dProfile hd2dProfile; public AudioClip ambientAudio; public string musicId; public Sprite mapIcon; public float flightPlane=1; public string[] safePockets;
        /// <summary>Outgoing damage of ordinary (non-boss) enemies here: the chapter grows more dangerous as it goes (D194).</summary>
        public float enemyThreat=1;
        public Material skybox;
    }
}
