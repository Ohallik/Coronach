using UnityEngine;
namespace Lattice.World
{
    /// <summary>A drawn rock turning slowly about its own centre (the Picket's asteroids): its look only. Whatever it stands for in play (a solid circle, a sensor shadow) is on another object
    /// and doesn't move.</summary>
    public sealed class Tumble:MonoBehaviour
    {
        /// <summary>The axis it turns about, in the world, and degrees a second.</summary>
        public Vector3 axis=Vector3.up;public float rate=6;
        void Update(){if(!Lattice.Core.GameTime.Paused)transform.Rotate(axis,rate*Time.deltaTime,Space.World);}
    }
}
