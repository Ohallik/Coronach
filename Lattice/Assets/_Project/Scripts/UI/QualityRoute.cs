using System;
using UnityEngine;

namespace Lattice.UI
{
    // Inputs are replayed through the ordinary Input System. Points are navigation
    // goals, never actor teleports. Keep fixtures alongside their captured evidence.
    [Serializable] public sealed class QualityRoute
    {
        public string name, scene, sourceRevision;
        public float settleSeconds = 5;
        public float screenshotInterval;
        public int frameCap = 60;
        public bool starterParty = true;
        // Optional DevLoadout (moon, gullet) so a route starts at the level its
        // place is actually reached; applied after the starter party.
        public string loadout;
        public QualityStep[] steps;
        public QualitySegment[] segments;
        // Optional: "Gentle", "Standard" or "Demanding" (Lattice.Core.Difficulty). Unset is
        // Standard. The driver holds the game at it for the whole route, whatever the save
        // it continues was left at, so a chain never inherits a setting from a save.
        public string difficulty;
        /// <summary>The driver looks after the party's Integrity as a player does, with the pad: under half in a fight it uses the item that best fits what is missing
        /// (never inside the game's wait between items), tops up between fights with nothing wasted, and raises a fallen partner with a Relink Spike or by walking to
        /// them. Off unless a route says so: a route about a defeat, and every older fixture, keeps its input as written.</summary>
        public bool autoHeal;
        /// <summary>The driver releases the lead's EXTRA move (RB+LT) when the gauge is full and a boss, or three or more enemies, are in its reach. Off unless a
        /// route says so; the routes of chapter 3 onward say so.</summary>
        public bool useExtra;
        /// <summary>The two heroes this route is played with, by id, once the party is three ("deployed": ["Taren","Idra"]). Starting from a loadout, the driver puts
        /// them out with it. Continuing a save, it checks at the first step with a party that these are the two out, and fails the run there, naming both pairs,
        /// if the save has another pair: not twenty steps later behind the wrong partner. Unset, as in every route of a party of two, nothing is asked.</summary>
        public string[] deployed;
        /// <summary>The driver wears the best it holds, as a player does (ContinuousReview.Gear.cs, ReplayGear): when the route begins and whenever the party is out of any
        /// fight and any talk, each hero out is put into the best spare part for each slot by the Gear page's own numbers, through the page's own equip call, never taking a
        /// part off another hero. Off unless a route says so; on for every chapter route after chapter-05-drill-equipment, which drives the Gear page itself.</summary>
        public bool wearBest;
    }
    [Serializable] public sealed class QualitySegment
    {
        public string name;
        public int firstStep, stepCount;
    }
    [Serializable] public sealed class QualityStep
    {
        public string name, expectedScene, expectedCharacter, expectedForm, expectedUi, expectedFlag;
        // Permits a simulation pause only when this exact menu is observed.
        // Offline clean timing and runtime headroom always reject paused frames.
        public string pauseUi;
        public string until;
        // Ends the step early once true, without requiring it: a fight round
        // that its outcome may or may not cut short.
        public string stopWhen;
        /// <summary>Ordinary input: press Swap (spaced half a second) until this hero leads. A harder fight can leave the
        /// partner in the lead after the other went down and was revived; later routes expect a particular lead.</summary>
        public string ensureCharacter;
        /// <summary>Ordinary input: d-pad down to the row named here, by its object's name or the words it starts with, and only then the step's buttons. A list's order can change with
        /// which save is newest or what the party holds, and a count of presses can't see that. "until":"row:name" ends a step on arrival.</summary>
        public string pickRow;
        public float seconds = 1, x, y, leftTrigger, rightTrigger;
        public bool navigate;
        /// <summary>A walk from wherever a fight left the party: the stick follows the baked floor's way to the point, round what stands between, as the walk to a fallen hero
        /// does. A straight push from the far side of a drained hall met a ledge and stood against it (the Drowned Waystation, November 13). On foot only.</summary>
        public bool findWay;
        public bool approachPartner;
        // Chase the active hero's current target as a player holds the stick at a
        // retreating enemy: re-close whenever it is beyond tolerance, and hold the
        // step's buttons only once in reach.
        public bool approachTarget;
        // A ranged hero's reach in a chase (Sela shoots; she does not close to melee).
        public float rangedTolerance;
        // Opt-in agent reaction to the chased enemy's visible attack wind-up.
        // It queues a lateral stick + dodge press through the ordinary input path.
        public bool evadeTelegraphs;
        // Explicit replay policy only: react to an incoming visible shot with
        // normal stick/roll input. Default routes retain their original input.
        public bool evadeProjectiles;
        public Vector3 point;
        public float tolerance = .65f, magnitude = 1, pulseSeconds;
        public string[] buttons;
        /// <summary>The route's own say over the pouch on this step. A consumable's id: the driver cycles the quick item to it and presses it once, hurt or not, for a
        /// step that teaches one (the game still answers as it would a person). "none": the driver keeps its hands off the pouch for the step, whatever autoHeal wants.</summary>
        public string useItem;
        /// <summary>With pickRow on a consumable's row at a stall or a bench: press the step's buttons until the party carries this many, "cap" for all it may carry.
        /// The presses stop there, or at the first one that buys nothing (no scrip, no materials), and "until":"stocked" ends the step. Counted presses bought twelve
        /// gels where nine fit, and the last three were refusals.</summary>
        public string stock;
        /// <summary>A boss's id, on the first step of the approach to its fight: where a retry from its door picks the route up. If the party falls before
        /// bossdown.&lt;id&gt; is set, the driver takes the defeat screen's Retry as a player would and plays on from this step, up to twice; every attempt is written
        /// to the run's evidence. The step must be good from the door's checkpoint spawn as well as from where the route arrives.</summary>
        public string bossDoor;
    }
}
