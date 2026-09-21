using System;
using Lattice.Core;
namespace Lattice.World
{
    public sealed class Bench:InteractionPrompt
    {
        public static event Action Requested;
        public override void Interact()=>Requested?.Invoke();
    }
}
