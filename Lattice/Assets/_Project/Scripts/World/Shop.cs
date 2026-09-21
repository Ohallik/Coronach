using System;
using Lattice.Core;
using Lattice.Data;
namespace Lattice.World
{
    public sealed class Shop:InteractionPrompt
    {
        public ShopInventory inventory;
        public static event Action<ShopInventory> Requested;
        public override void Interact()=>Requested?.Invoke(inventory);
    }
}
