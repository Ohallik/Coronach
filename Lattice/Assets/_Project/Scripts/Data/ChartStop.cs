using UnityEngine;
namespace Lattice.Data
{
    /// <summary>One settlement on Sela's chart (RouteChart, the pause menu's Chart page): a place with a shop, a bench or people to come back to, that the party can
    /// travel to from anywhere once it has stood there. A chapter adds one for each settlement it builds, from its own builder; nothing else is edited.</summary>
    [CreateAssetMenu(menuName="Lattice/ChartStop")]
    public sealed class ChartStop:ScriptableObject
    {
        /// <summary>The asset's file name. What a player reads: the place ("Sorrel Outpost"), where it is ("Vorun's moon"), and one line on why anyone goes back.</summary>
        public string id,displayName,region,blurb;
        /// <summary>Where travelling here arrives: a ZoneDef's id and a SpawnPoint in its scene.</summary>
        public string zone,spawn="Arrival";
        /// <summary>The flag that says the party has stood here. Empty: <c>visited.&lt;zone&gt;</c>, which the game sets itself the moment a zone of chapter 3 or later is entered.
        /// Chapters 1 and 2's four name the story flag only someone standing there could have set, so a save from before the chart existed has them.</summary>
        public string unlockFlag;
        /// <summary>A story beat that closes the place: while this flag is set nobody travels here, and the chart says <see cref="lockedReason"/>.</summary>
        public string lockedFlag,lockedReason;
        /// <summary>Where its lantern stands on the chart, each 0 to 1: x from the west edge to the east, y from the south edge up. The story runs west to east.</summary>
        public Vector2 position;
        /// <summary>The stops a route line joins this one to (their ids): usually the one the party first came here from.</summary>
        public string[] routes;
        /// <summary>What stands there to come back for.</summary>
        public bool shop,bench,repair;
        /// <summary>The chapter that builds it (1 to 10): the chart lists stops in this order.</summary>
        public int chapter;
        /// <summary>The zones reached from here and nowhere else (its approach, its wilds, its dungeon): while the party or the story's next step is in one of them, this is the stop marked.</summary>
        public string[] reaches;
        public const string VisitedPrefix="visited.";
        /// <summary>The flag the chart reads: <see cref="unlockFlag"/>, or the zone's own visited flag.</summary>
        public string UnlockFlag=>string.IsNullOrEmpty(unlockFlag)?VisitedPrefix+zone:unlockFlag;
    }
}
