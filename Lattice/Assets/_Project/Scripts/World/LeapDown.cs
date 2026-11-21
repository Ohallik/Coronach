using Lattice.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lattice.World
{
    /// <summary>
    /// A way down that is jumped, not walked: the gap a thawed rank leaves in the Frozen Host's left gallery, which "goes straight down to the bottom gallery" (SelaDigShortcut). The hand
    /// is a <see cref="DiscoveryPoint"/> beside it (the prompt, the talk, the flag); once <see cref="flag"/> goes from unset to set while the zone is open and the conversation has
    /// closed, the zone is entered again at <see cref="spawn"/>, as any way in is (the load's black is the fall, and arriving autosaves). A save that already has the flag jumps nothing.
    /// Logs <c>LEAP_DOWN spawn</c>.
    /// </summary>
    public sealed class LeapDown:MonoBehaviour
    {
        public string flag,spawn;
        bool known,armed;
        public bool Leapt{get;private set;}
        void Update()
        {
            var services=GameServices.Current;if(services==null||string.IsNullOrEmpty(flag)||SceneFlow.Current==null)return;
            bool set=services.Flags.GetBool(flag);
            if(!known){known=true;armed=!set;return;}
            if(!armed||!set||services.Input.Blocked||SceneFlow.Current.Loading)return;
            armed=false;Leapt=true;Debug.Log("LEAP_DOWN "+spawn);
            SceneFlow.Current.LoadZone(SceneManager.GetActiveScene().name,spawn);
        }
    }
}
