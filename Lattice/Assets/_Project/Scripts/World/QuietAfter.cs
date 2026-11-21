using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// Nothing barks for a while after a moment: once <see cref="flag"/> is set in play (not at a load that finds it set), every bark is hushed for <see cref="seconds"/>. Chapter 10's
    /// Sorrel: Dace's one line as Mother of Doors goes ("Go on, then. You've had the quiet. Have the rest.") sets <c>end.motherFreed</c>, and nothing is said after it for eight seconds
    /// (ch10-wiring section 2, <c>SurvivorMotherGoes</c>).
    /// </summary>
    public sealed class QuietAfter:MonoBehaviour
    {
        public string flag;public float seconds=8;
        bool unsetSeen,hushed;float until=-1;
        void Update()
        {
            var services=GameServices.Current;if(services==null||string.IsNullOrEmpty(flag))return;
            if(!services.Flags.GetBool(flag)){unsetSeen=true;return;}
            if(!unsetSeen){enabled=false;return;}
            if(until<0){until=Time.time+seconds;BarkService.Hush(this,true);hushed=true;Debug.Log("QUIET_AFTER "+flag+" "+seconds);}
            if(Time.time>=until){BarkService.Hush(this,false);hushed=false;enabled=false;}
        }
        void OnDisable(){if(hushed){BarkService.Hush(this,false);hushed=false;}}
    }
}
