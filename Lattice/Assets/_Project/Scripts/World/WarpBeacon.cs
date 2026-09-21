using Lattice.Core;
namespace Lattice.World
{
    public sealed class WarpBeacon:InteractionPrompt
    {
        public string requiredFlag="warpkey",scene="Gullet_Tunnel",spawn="Arrival";
        public override void Interact(){if(string.IsNullOrEmpty(requiredFlag)||GameServices.Current.Flags.GetBool(requiredFlag))SceneFlow.Current.LoadZone(scene,spawn);else prompt="Warp locked — recover the Sorrel key";}
    }
}
