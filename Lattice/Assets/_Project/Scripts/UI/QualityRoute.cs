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
        public float seconds = 1, x, y, leftTrigger, rightTrigger;
        public bool navigate;
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
    }
}
