using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/ZoneDef")]
    public sealed class ZoneDef:ScriptableObject
    {
        public string id,scene; public ZoneKind kind; public CameraProfile cameraProfile; public Hd2dProfile hd2dProfile; public AudioClip ambientAudio; public string musicId; public Sprite mapIcon; public float flightPlane=1; public string[] safePockets;
        /// <summary>Outgoing damage of ordinary (non-boss) enemies here: the chapter grows more dangerous as it goes (D194).</summary>
        public float enemyThreat=1;
        /// <summary>Open landforms (D198): where visible scenery (a cliff rim, the drill) stands between the fixed ground
        /// camera and the party, the camera slides in along its own view line instead of hiding them. Stations keep
        /// their authored cutaway framing and leave this off.</summary>
        public bool cameraClearsScenery;
        public Material skybox;
    }
}
