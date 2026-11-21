using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/ShopInventory")]
    public sealed class ShopInventory:ScriptableObject
    {
        /// <summary>The shop's heading, when it isn't the outfitter's.</summary>
        public string title;
        /// <summary>What's on the counter, each with its own price, in the order it's shown. The prices used to be one list read by position after the parts were sorted by slot,
        /// which only worked while the first two cost the same; and a zone with no inventory sold every consumable in the catalogue at 20.</summary>
        public ShopOffer[] offers;
        /// <summary>Whether the Sell page takes materials as well as parts.</summary>
        public bool buysMaterials=true;
        /// <summary>A seller's own price, once something has earned it (Corra's, after the kettles): while <see cref="discountFlag"/> is set every buying price here is
        /// multiplied by <see cref="discount"/>, and the stall says why in <see cref="discountNote"/>. Empty: no discount.</summary>
        public string discountFlag,discountNote;public float discount=1;
        /// <summary>A line on the stall's page while <see cref="noteFlag"/> is set: what the story has brought to the counter (Mira's best crates opened for the road, the convoy's
        /// stock at Corra's), said where the seller's price note stands, so stock that waits on a flag doesn't arrive from nowhere. Empty: nothing said.</summary>
        public string noteFlag,note;
    }
}
