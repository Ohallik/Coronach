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
        public int frameCap = 60;
        public bool starterParty = true;
        public QualityStep[] steps;
    }
    [Serializable] public sealed class QualityStep
    {
        public string name, expectedScene, expectedCharacter, expectedUi;
        public string until;
        public float seconds = 1, x, y, leftTrigger, rightTrigger;
        public bool navigate;
        public bool approachPartner;
        public Vector3 point;
        public float tolerance = .65f, magnitude = 1, pulseSeconds;
        public string[] buttons;
    }
}
