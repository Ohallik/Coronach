using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/Hd2dProfile")]
    public sealed class Hd2dProfile:ScriptableObject
    {
        public int internalWidth=960,internalHeight=540; public float tiltStart=0.28f,tiltStrength=1.4f,bloom=0.45f,bloomThreshold=1.1f,vignette=0.16f; public Color tint=Color.white;
    }
}
