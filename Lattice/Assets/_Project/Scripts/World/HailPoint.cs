using Lattice.Core;
namespace Lattice.World
{
    public sealed class HailPoint:InteractionPrompt
    {
        public Npc contact;
        public override void Interact(){if(contact!=null)contact.Interact();}
    }
}
