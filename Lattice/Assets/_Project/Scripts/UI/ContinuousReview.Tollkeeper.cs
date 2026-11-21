using Lattice.Combat;
using Lattice.Core;
using Lattice.World;
using UnityEngine;

namespace Lattice.UI
{
    // The driver's hands for the Tollkeeper in its room (route 65, tools/mooring_routes.py; MooringBossReplayTests). Ordinary pad presses and what a player reads off the screen, as
    // everywhere in the driver: the game still does to the party whatever it would do to a person who misjudged it.
    public sealed partial class ContinuousReview
    {
        /// <summary>The driver as it was before these hands, for a test's broken state (MooringBossReplayTests sets it by reflection and puts it back): no route sets it.</summary>
        static bool tollHandsOff;

        /// <summary>How near a body of the fight the ship flies before the lock can take it (the lock reaches 30 m; what is behind it, less).</summary>
        const float FlightLockWithin=24;
        Health flightFightFar,flightFightSaid;double flightFightAt=-1;
        /// <summary>In flight, a fight step with nothing in the lock, the ship inside a fight's box that is begun and not cleared, and a body of that fight awake further off than the lock
        /// reaches: the stick toward it, the brake off, until the lock can take it. At chain C's gap the west gun broke and the east one stood 33 m off, firing its rows on the beat: the
        /// lock took nothing that far, and the ship sat braked where the first had been for the rest of the rounds (MooringBossReplayTests). A person flies at the gun still firing.
        /// Null otherwise: on foot the chase has its own (TowardFight), and a boss its own (LostBossBeyond).</summary>
        Vector2? FlightTowardFight(CombatActor lead)
        {
            if(tollHandsOff||lead==null||!lead.flight||lead.target!=null&&lead.target.Alive)return null;
            // Every flight fight, not chapter 10's alone as it was for its first night: in Xylos' nearspace a skimmer of barge three's stood beyond the lock with the companion
            // down, and the ship sat braked through three rounds (first whole run, November 15). It only ever acts where the ship would otherwise wait with nothing locked.
            if(BossController.Focus(lead.transform.position,LostBossWithin)!=null)return null;
            double now=clock.Elapsed.TotalSeconds;var at=lead.transform.position;
            if(now-flightFightAt>.5)
            {
                flightFightAt=now;flightFightFar=null;float least=float.PositiveInfinity;
                foreach(var body in Health.All)
                {
                    if(body==null||body.friendly||!body.Alive||!body.Targetable||EnemyBrain.OtherForm(lead,body)||!InTheSameFight(at,body.transform.position))continue;
                    var owner=body.reactionOwner!=null?body.reactionOwner:body;
                    if(!owner.TryGetComponent<EnemyBrain>(out var brain)||!brain.enabled||brain.Passive)continue;
                    var gap=body.transform.position-at;gap.y=0;if(gap.sqrMagnitude<least){least=gap.sqrMagnitude;flightFightFar=body;}
                }
                if(flightFightFar!=null&&least<=FlightLockWithin*FlightLockWithin)flightFightFar=null;
                if(flightFightFar!=null&&flightFightFar!=flightFightSaid){flightFightSaid=flightFightFar;Debug.Log("QUALITY_TOWARD_FIGHT "+flightFightFar.name+" (in flight)");}
            }
            if(flightFightFar==null||!flightFightFar.Alive)return null;
            var heading=flightFightFar.transform.position-at;heading.y=0;if(heading.sqrMagnitude<.0001f)return null;
            var local=Quaternion.Euler(0,-ZoneController.Current.CameraProfile.yaw,0)*heading.normalized;
            return new Vector2(local.x,local.z);
        }

        /// <summary>Whether a marking of the Tollkeeper's lands on the lead now, and the way out (ContinuousReview.Mother.cs's UnstruckLands asks it after her wave): null for any marking
        /// read as every boss's is (its tell nearly out, and over the lead), which is every one of its today.</summary>
        bool? TollLands(Telegraph marking,BossController boss,CombatActor lead,float soon,out Vector3 way)
        {
            way=default;
            if(tollHandsOff||boss==null||!boss.TollRoom)return null;
            return null;
        }
    }
}
