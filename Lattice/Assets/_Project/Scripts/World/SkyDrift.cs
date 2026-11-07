using UnityEngine;
namespace Lattice.World
{
    /// <summary>The Wick Reach's sky takes a little of the view's travel, and turns very slowly on its own. A flight camera never rotates, so a skybox is fixed to the screen: readers found the same
    /// amber mote at the same pixel in frames 100 m apart, painted on the glass. A vector is all this costs: Lattice/DriftingSky reads it, and nothing else does.</summary>
    public sealed class SkyDrift:MonoBehaviour
    {
        /// <summary>Degrees of sky per metre the view travels. At a 30 degree view that's about 2 px a metre against the ships' 40: far off, but going past.</summary>
        const float PerMetre=.06f,PerSecond=.04f,FlightPlane=1;
        static readonly int Turn=Shader.PropertyToID("_SkyTurn");
        Camera view;
        void LateUpdate()
        {
            if(view==null){view=Camera.main;if(view==null)return;}
            // Where the view looks on the flight plane, not where the lens stands: a lens pulling back along its own axis sees the same sky, and measured from the lens every zoom slid it 100 px.
            var lens=view.transform;Vector3 at=lens.position,look=lens.forward;if(look.y<-.01f)at+=look*((at.y-FlightPlane)/-look.y);
            // Its own slow turn goes round the world's up. A tilt that grew with the clock would bring the panorama's pole, a pinched point, into view within the hour.
            Shader.SetGlobalVector(Turn,new Vector4(at.z*PerMetre,at.x*PerMetre+Time.time*PerSecond,0,0));
        }
        void OnDisable()=>Shader.SetGlobalVector(Turn,Vector4.zero);
    }
}
