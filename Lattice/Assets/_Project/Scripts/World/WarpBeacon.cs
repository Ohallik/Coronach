using Lattice.Combat;
using Lattice.Core;
namespace Lattice.World
{
    public sealed class WarpBeacon:InteractionPrompt
    {
        public string requiredFlag="warpkey",scene="Gullet_Tunnel",spawn="Arrival",lockedPrompt="Warp locked — recover Sorrel's survey key";
        /// <summary>Like a launch pad, a warp waits for any boss fight under way to end (D198) and for an enemy on the party.</summary>
        public override bool Available=>!BossController.AnyFightUnderway&&!EnemyBrain.AwakeNear(transform.position,EnemyBrain.ExitCalm);
        string openPrompt;
        bool Locked=>!string.IsNullOrEmpty(requiredFlag)&&!GameServices.Current.Flags.GetBool(requiredFlag)||
            scene=="Gullet_Tunnel"&&!ChapterProgress.CanEnterGullet(GameServices.Current.State);
        void Awake()=>openPrompt=prompt;
        /// <summary>A refused press shows why; once the route opens, the prompt names the destination again (the
        /// locked text used to stay until the scene reloaded).</summary>
        void Update(){if(openPrompt!=null&&prompt!=openPrompt&&GameServices.Current!=null&&!Locked)prompt=openPrompt;}
        public override void Interact()
        {
            if(!string.IsNullOrEmpty(requiredFlag)&&!GameServices.Current.Flags.GetBool(requiredFlag)){prompt=lockedPrompt;return;}
            if(scene=="Gullet_Tunnel"&&!ChapterProgress.CanEnterGullet(GameServices.Current.State))
            {prompt="Route incomplete — explore Hushwell beneath Sorrel's drill";return;}
            SceneFlow.Current.LoadZone(scene,spawn);
        }
    }
}
