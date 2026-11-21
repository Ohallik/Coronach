using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/MaterialDef")]
    public sealed class MaterialDef:ScriptableObject
    {
        public string id,displayName; public Sprite icon; public int stack=999;
        /// <summary>Scrip a shop pays for one. Nothing at zero: a shop doesn't take it.</summary>
        public int value;
        /// <summary>A boss's or an elite's: sold one at a time and asked twice, and what the last upgrade and a re-tune are paid in.</summary>
        public bool rare;
        /// <summary>The chapter whose region it comes from. A recipe that takes one from chapter 3 on stays off the bench's list until the party has held it (Fabrication.Known).</summary>
        public int chapter;
    }
}
