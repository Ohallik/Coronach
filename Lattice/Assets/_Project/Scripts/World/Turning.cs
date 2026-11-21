using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>A part of a machine that keeps turning on its own axle at its own rate: the Orrery's gear wheels (chapter 7), on the machine's clock (<see cref="GameTime"/>, so
    /// they stop with a pause and a hit-stop). Drawn only; nothing reads it.</summary>
    public sealed class Turning:MonoBehaviour
    {
        /// <summary>The axle, in the object's own space, and degrees a second about it (negative turns the other way).</summary>
        public Vector3 axis=Vector3.up;public float degrees=6;
        Quaternion rest;bool placed;
        void Update()
        {
            if(!placed){rest=transform.localRotation;placed=true;}
            transform.localRotation=rest*Quaternion.AngleAxis(GameTime.Now*degrees,axis);
        }
    }
}
