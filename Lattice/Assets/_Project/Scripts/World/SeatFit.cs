using UnityEngine;

namespace Lattice.World
{
    /// <summary>What the editor's seating rule (Seating) did to a seated body, kept so a scene saved again is fitted from the builder's placing, never from the last fitting:
    /// where the builder left the body's model, and the seat it was given. Nothing reads it while the game runs.</summary>
    public sealed class SeatFit:MonoBehaviour
    {
        /// <summary>The model's place under the person as the builder left it.</summary>
        public Vector3 builtLocal;
        /// <summary>The seat this rule put under them (none when what the builder gave fits).</summary>
        public GameObject seat;
        /// <summary>What it did, in words: how far it lifted the body and what it sits on.</summary>
        public string fit="";
    }
}
