using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/Hd2dProfile")]
    public sealed class Hd2dProfile:ScriptableObject
    {
        public bool nativeResolution=true;
        public int internalWidth=1920,internalHeight=1080; public float tiltStart=0.28f,tiltStrength=0,bloom=0.45f,bloomThreshold=1.1f,vignette=0.16f; public Color tint=Color.white;
    }
}
