using Lattice.Core;
using Lattice.World;

namespace Lattice.UI
{
    // The driver's reading of chapter 10's berth (ch10-wiring 6.1.1): a ship docks at a shelf by flying to its pad and pressing A, and launches by walking to the shelf's
    // launch and pressing A, as a player does. Nothing here moves the party: the route's ordinary steps fly, walk and press; these say when the change is over.
    public sealed partial class ContinuousReview
    {
        /// <summary>"until":"docked:&lt;id&gt;": the party stands on that berth, in its form, and the change's fade is over (the controls are the player's again).</summary>
        static bool DockedAt(string id) => Berth.Current != null && Berth.Current.id == id && !Berth.Changing && ZoneController.Current != null && ZoneController.Current.BerthId == id;

        /// <summary>"until":"launched:&lt;id&gt;": the party is back in the zone's own form, out of that berth (pressed or put out by it), and the change is over.</summary>
        static bool LaunchedFrom(string id)
        {
            var zone = ZoneController.Current;
            return Berth.Current == null && !Berth.Changing && Berth.LastLaunched == id && zone != null && !zone.Berthed && zone.Kind == zone.definition.kind;
        }
    }
}
