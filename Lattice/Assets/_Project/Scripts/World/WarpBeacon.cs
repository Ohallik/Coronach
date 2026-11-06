using Lattice.Combat;
using Lattice.Core;
namespace Lattice.World
{
    public sealed class WarpBeacon:InteractionPrompt
    {
        public string requiredFlag="warpkey",scene="Gullet_Tunnel",spawn="Arrival",lockedPrompt="Warp locked — recover Sorrel's survey key";
        /// <summary>The far end has no way back until this flag is set, and until then the prompt carries this note: the only exit from
        /// there may need a boss beaten, and the autosave is written on arrival.</summary>
        public string wayBackFlag,oneWayNote;
        /// <summary>What a locked warp calls itself when nobody has named where it goes yet.</summary>
        public string unnamedPrompt;
        /// <summary>Like a launch pad, a warp waits for any boss fight under way to end (D198) and for an enemy on the party.</summary>
        public override bool Available=>!BossController.AnyFightUnderway&&!EnemyBrain.AwakeNear(transform.position,EnemyBrain.ExitCalm);
        string openPrompt;
        bool Locked=>!string.IsNullOrEmpty(requiredFlag)&&!GameServices.Current.Flags.GetBool(requiredFlag)||
            scene=="Gullet_Tunnel"&&!ChapterProgress.CanEnterGullet(GameServices.Current.State);
        void Awake()=>openPrompt=prompt;
        /// <summary>A refused press shows why; once the route opens, the prompt names the destination again (the
        /// locked text used to stay until the scene reloaded).</summary>
        void Update()
        {
            if(openPrompt==null||GameServices.Current==null)return;
            if(Locked){if(prompt==openPrompt&&!string.IsNullOrEmpty(unnamedPrompt))prompt=unnamedPrompt;return;}
            string open=!string.IsNullOrEmpty(wayBackFlag)&&!GameServices.Current.Flags.GetBool(wayBackFlag)?openPrompt+oneWayNote:openPrompt;
            if(prompt!=open)prompt=open;
        }
        public override void Interact()
        {
            if(!string.IsNullOrEmpty(requiredFlag)&&!GameServices.Current.Flags.GetBool(requiredFlag)){prompt=lockedPrompt;return;}
            if(scene=="Gullet_Tunnel"&&!ChapterProgress.CanEnterGullet(GameServices.Current.State))
            {prompt="Route incomplete — explore Hushwell beneath Sorrel's drill";return;}
            SceneFlow.Current.LoadZone(scene,spawn);
        }
    }
}
