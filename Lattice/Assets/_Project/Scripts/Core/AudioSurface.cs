using UnityEngine;
namespace Lattice.Core
{
    public enum FootstepSurface { Metal,Rock,Soil }
    // Put an override on the physical collider or its parent when a map mixes
    // materials. Existing station decks and natural ground retain a sensible default.
    public sealed class AudioSurface:MonoBehaviour
    {
        public FootstepSurface material;
        public static string Family(Collider ground,string scene)
        {
            var surface=ground!=null?ground.GetComponentInParent<AudioSurface>():null;
            var kind=surface!=null?surface.material:
                scene=="Hub_Decks"||scene=="TallowDrift"?FootstepSurface.Metal:FootstepSurface.Rock;
            return kind switch {FootstepSurface.Metal=>"step_metal",FootstepSurface.Soil=>"step_soil",_=>"step_rock"};
        }
    }
}
