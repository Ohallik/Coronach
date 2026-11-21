using Lattice.Data;
using UnityEngine;
namespace Lattice.Core
{
    /// <summary>
    /// What Combat may know of a berth (World's <c>Berth</c>, ch10-wiring 6.1) without seeing World: a part of a zone of the other form, a shelf of ground in a flight zone or
    /// a stretch of space beside a ground one, entered and left only by a prompt. A boss's file finds one by its id (<see cref="ZoneController.FindBerth"/>) and the one the party is
    /// on (<see cref="ZoneController.BerthOn"/>). A berth that moves is moved only through <see cref="Drive"/>, which does nothing while the party stands on it or the controls are
    /// held (D275): what moves round a hero is drawn, never the floor under him.
    /// </summary>
    public interface IBerth
    {
        string Id{get;}
        /// <summary>The berth's form: the zone's Kind while the party is on it.</summary>
        ZoneKind Kind{get;}
        /// <summary>The point is inside the berth's floor (or space).</summary>
        bool Contains(Vector3 point);
        /// <summary>The party stands on it, or is changing onto or off it.</summary>
        bool Occupied{get;}
        /// <summary>Nobody on it, no change under way and the controls free: <see cref="Drive"/> would move it.</summary>
        bool MayMove{get;}
        /// <summary>Seconds it has been free to move: the clock a moving berth's path is read on. It stops while the party is on it and through a conversation.</summary>
        float Travel{get;}
        /// <summary>Seconds the party has stood on it this time, the controls free: the clock an arm draws in on.</summary>
        float DockedSeconds{get;}
        /// <summary>Moves the berth (and everything placed under it) when <see cref="MayMove"/>; false and nothing moved otherwise.</summary>
        bool Drive(Vector3 position,Quaternion rotation);
        /// <summary>Puts the party out to its flight place as if the launch were pressed (an arm drawn in), once the controls are free. Nothing if the party isn't on it.</summary>
        void Launch(string why);
    }
}
