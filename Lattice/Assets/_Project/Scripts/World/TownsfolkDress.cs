using UnityEngine;

namespace Lattice.World
{
    /// <summary>What a townsperson's builder gave them, kept by the editor's townsfolk pass (TownsfolkLooks) beside what it chose: a scene saved again is dressed from the
    /// builder's choice, never from the last dressing, so dressing twice gives the same people. Nothing reads it while the game runs.</summary>
    public sealed class TownsfolkDress:MonoBehaviour
    {
        /// <summary>The body's material as the builder left it, and the model's scale.</summary>
        public Material built;public Vector3 builtScale=Vector3.one;
        /// <summary>An errand's pace as the builder set it (below nought: not on an errand): a taller or shorter walker walks at its height's pace.</summary>
        public float builtSpeed=-1;
        /// <summary>What the pass put on them (worn and carried pieces), taken off before they are dressed again.</summary>
        public GameObject[] added=System.Array.Empty<GameObject>();
        /// <summary>The look the pass chose, in words: painting, head, carried, build.</summary>
        public string look="";
    }
}
