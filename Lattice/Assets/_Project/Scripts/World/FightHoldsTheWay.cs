using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>A boss fight that holds its zone's way home shut while it is under way, wherever in the zone the party has gone. A flight boss is fought far from every way out
    /// (Mother of Doors 330 m up Sorrel's valley from the warp home, the Tollkeeper at Last Mooring's gate), and a launch pad or a warp refuses only within a boss fight's reach (D198):
    /// mid-fight a ship could fly back down the valley and warp home, with nothing to say it was leaving a fight. From the moment the door's fight begins until it is won, the way
    /// waits on the boss's fall (its <see cref="WarpBeacon.requiredFlag"/> is <see cref="until"/>, its refusal <see cref="saying"/>, as the Debris Cutter's and the Warmaster's
    /// skiff's ways home wait on theirs); before the fight and after it (won, or lost and the zone loaded again) it is the way it was built, open. Lives on an always-active object.</summary>
    public sealed class FightHoldsTheWay:MonoBehaviour
    {
        public EncounterVolume fight;public WarpBeacon way;
        /// <summary>The boss's fall (<c>bossdown.&lt;id&gt;</c>), and what a refused press says while the fight holds the way: it names the boss.</summary>
        public string until,saying;
        string ownFlag,ownLocked;bool known,holding;
        void Start()=>Apply();
        void Update()=>Apply();
        void Apply()
        {
            if(way==null||GameServices.Current==null)return;
            if(!known){known=true;ownFlag=way.requiredFlag;ownLocked=way.lockedPrompt;}
            bool held=fight!=null&&fight.Started&&!fight.Cleared&&!string.IsNullOrEmpty(until)&&!GameServices.Current.Flags.GetBool(until);
            if(held==holding)return;holding=held;
            way.requiredFlag=held?until:ownFlag;way.lockedPrompt=held?saying:ownLocked;
            Debug.Log($"WAY_HELD {(held?"by":"let go by")} {fight.encounterId}: {way.name}");
        }
    }
}
