using Lattice.Combat;
using UnityEngine;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Where a boss's flat-out measures stand a hero with an edge: a quarter metre off the body's capsule (his own 0.35 m capsule and .25 past it), as a player walks up to it.
    /// One place for every measure that stands one (QaPlanCombatTests.FlatOut and each chapter's own copy of it). Until November 22 they stood him .9 m off the capsule. From there his
    /// blade reached a body 1.4 m wide (71 cuts in 71) but not a broader one, whose surface is flatter: on the Bellows made 2.4 m wide 55 cuts in 79 reached it and his Arc Cleave 1 in 7,
    /// and the pair read 348 a second against 456; from here, 457 (BellowsDomeTests, D355 continued). The three broad bodies measured with driver shares (the Foundry Saint, the
    /// Rotbloom, the Slag Colossus) read 62%, 24% and 34% higher from here, and their shares were rescaled by old over new reading, so their predicted lengths stay where their replays put them.</summary>
    public static class BossReach
    {
        /// <summary>A hero's capsule (ActorFactory: every hero's is 0.35 m), and the gap left between it and the body's.</summary>
        public const float HeroCapsule=.35f,Gap=.25f;
        /// <summary>How far from a body's middle a hero with an edge stands to strike it.</summary>
        public static float Edge(EnemyBrain body)=>body.GetComponent<CharacterController>().radius+HeroCapsule+Gap;
    }
}
