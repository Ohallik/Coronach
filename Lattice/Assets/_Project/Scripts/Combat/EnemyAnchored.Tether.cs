using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// An anchored body <b>on a tether</b> (<c>tune.tethered</c>: chapter 9's Bridle Fragment, a broken piece of the cage still made fast to a wreck in Nacre's breach). It never turns: its
    /// fan is laid along the way it was set facing, which is away from what it is made fast to, and it thrashes on its own rhythm whenever a hero is anywhere near, whoever is where.
    /// It can't swing over its own tether, so the side the tether runs to is never struck: that is its lee, and the answer is to come in along the tether and break it from there,
    /// or to cross its free side between thrashes. Logs <c>FRAGMENT_THRASHES &lt;id&gt;</c>.
    /// Reads: attacks[0] (telegraph, range is the fan's reach, damage, cooldown is the rhythm), tune.arc (the fan, 120 degrees).
    /// </summary>
    public sealed partial class EnemyAnchored
    {
        bool Tethered=>tune.tethered;
        /// <summary>How far past its reach a hero sets it thrashing: it thrashes at what it can see coming, not only at what it can reach.</summary>
        public const float ThrashesWithin=4.5f;
        Vector3 free;bool freeKnown;
        /// <summary>The way its fan is laid, on the floor: the way it was set facing as it first thrashed, and never turned from.</summary>
        public Vector3 FreeSide{get{if(!freeKnown){var f=Flat(body.forward);free=f.sqrMagnitude>.01f?f.normalized:Vector3.forward;freeKnown=true;}return free;}}
        void Thrash(CombatActor victim)
        {
            if(!brain.Rested||PartyController.Current==null)return;
            bool near=false;
            foreach(var hero in PartyController.Current.members)if(hero.Health.Alive&&Flat(hero.transform.position-body.position).magnitude<Range+ThrashesWithin){near=true;break;}
            if(!near)return;
            var side=FreeSide;body.rotation=Quaternion.LookRotation(side);
            Mark(Telegraph.Cone(body.position,side,Range,Arc,Tell,Telegraph.Threat,true),Tell,side,victim,"root_groan",1.3f);
            Debug.Log("FRAGMENT_THRASHES "+def.id);
        }
    }
}
