using Lattice.Core;
namespace Lattice.World
{
    public sealed class WarpBeacon:InteractionPrompt
    {
        public string requiredFlag="warpkey",scene="Gullet_Tunnel",spawn="Arrival",lockedPrompt="Warp locked — recover the Sorrel key";
        public override void Interact()
        {
            if(!string.IsNullOrEmpty(requiredFlag)&&!GameServices.Current.Flags.GetBool(requiredFlag)){prompt=lockedPrompt;return;}
            if(scene=="Gullet_Tunnel"&&!ChapterProgress.CanEnterGullet(GameServices.Current.State))
            {prompt="Route incomplete — explore Hushwell beneath Sorrel's drill";return;}
            SceneFlow.Current.LoadZone(scene,spawn);
        }
    }
}
