using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// Something in the world that a hero works once, and the place answers: a sluice wheel, a pump or a coolant valve (<see cref="FlowSwitch"/>); an anchor root or a coupling cut
    /// (<see cref="ShiftAnchor"/>). Like a relay lantern, the switch is the answer and not the hand: a <see cref="DiscoveryPoint"/> beside it carries the prompt, the talk, the quest's count and the
    /// save, and sets <see cref="flag"/> (itself, or through its conversation's last line, as a story beat does). Everything that changes listens to that flag (water, doors, the objective), so the
    /// switch never knows what it moved. What it shows plays when the flag goes from unset to set while the zone is open; a save that already has the flag loads with the wheel turned and the root
    /// cut, and nothing played.
    /// </summary>
    public abstract class WorldSwitch:MonoBehaviour
    {
        public string flag;
        /// <summary>Logged as "marker flag" when the place answers, for a test or a replay route to wait on. Empty: the kind's own (SLUICE_OPEN, ANCHOR_CUT).</summary>
        public string marker;
        /// <summary>Where a hero works it, if the switch needs to know (a wheel shows its water to whoever stands there).</summary>
        public DiscoveryPoint point;
        public bool Worked=>!string.IsNullOrEmpty(flag)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(flag);
        protected abstract string Kind{get;}
        /// <summary>The place answers, seen and heard.</summary>
        protected abstract void Work();
        /// <summary>The same end state with nothing played: a loaded save.</summary>
        protected abstract void Settle();
        bool known,worked;
        protected virtual void Update()
        {
            var services=GameServices.Current;if(services==null||string.IsNullOrEmpty(flag))return;
            bool now=services.Flags.GetBool(flag);
            if(!known){known=true;worked=now;if(now)Settle();return;}
            if(!now||worked)return;
            worked=true;Debug.Log((string.IsNullOrEmpty(marker)?Kind:marker)+" "+flag);
            Work();
        }
    }
}
