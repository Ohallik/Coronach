using UnityEngine;
using UnityEngine.EventSystems;
namespace Lattice.UI
{
    /// <summary>A screen that comes up under a thumb still tapping holds its first press for a moment: what's under the cursor can spend something, and nobody has read it yet.
    /// One rule for the shop, the bench and a conversation's choice. A mouse click goes by the pointer and isn't held: nobody clicks by accident.</summary>
    public struct FirstPress
    {
        public const float Hold=.3f;
        float until;
        public bool Held=>until>0;
        public void Begin(){until=Time.unscaledTime+Hold;Navigation(false);}
        /// <summary>Once a frame while the screen is up. Something else may want the navigation off for its own reasons, a rebind's listen: it says so, and gives it back itself.</summary>
        public void Step(bool giveBack=true){if(until>0&&Time.unscaledTime>=until){until=0;if(giveBack)Navigation(true);}}
        public void End(){if(until<=0)return;until=0;Navigation(true);}
        static void Navigation(bool on){var system=EventSystem.current;if(system!=null)system.sendNavigationEvents=on;}
    }
}
