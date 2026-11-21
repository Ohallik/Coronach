using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/QuestDef")]
    public sealed class QuestDef:ScriptableObject
    {
        public string id,title,description; public Objective[] steps; public int rewardXp,rewardScrip; public Ingredient[] rewards; public string[] flagsOnComplete;
        /// <summary>Player-facing text for each step (D199): the quest log and task cards show it.</summary>
        public string[] stepText;
        /// <summary>A part among the rewards rolls affixes as one found in the world does. Off, it comes clean, as a shop's would.</summary>
        public bool rewardsRolled;
        /// <summary>The story's own quest: it leads the log, its current step is the objective panel's line in any zone with no hand-written one, and the
        /// side-quest checks leave it out. The first three (Sorrel, Hushwell, LanternRoad) are main whatever this says (MainQuests).</summary>
        public bool main;
        /// <summary>The chapter it belongs to (0 on the first ten until ContentScaffold stamps them): main quests lead the log in this order.</summary>
        public int chapter;
        /// <summary>Where each step sends the party, one per step, optional. With none (or an empty one) the panel works it out from the step itself:
        /// the way out to a Reach's scene, a TalkTo's speaker, the nearest open discovery with an Interact's report, a Kill's boss or its encounter.</summary>
        public QuestTarget[] stepTargets;
        /// <summary>A main quest's last word: what the objective panel says once it is done and no later story quest is open (the chapter is over, the next
        /// isn't begun). Empty: the place's name.</summary>
        public string closingLine;
        /// <summary>A main quest's: where <see cref="closingLine"/> sends the party, which is where the next chapter begins (the keeper with the next errand, the dock the next
        /// visitor steps onto, the chart table). The objective panel's arrow and the route chart's diamond follow it while no story quest is open, as they follow a step's
        /// target while one is. Empty: no arrow, and the closing line had better say everything (the first player run stood on a bridge reading "Many Hands is waiting").</summary>
        public QuestTarget next;
        /// <summary>A main quest's: every flag its story has set by the time it is done that its steps and <see cref="flagsOnComplete"/> don't already say (relays lit,
        /// doors opened, conversations heard). Nothing in play reads it: DevLoadout's chapter loadouts set them, so a later chapter can be started from a fixture.</summary>
        public string[] storyFlags;
        /// <summary>A main quest's, from chapter 3 on (ChapterCards): the flag that brings up "CHAPTER n COMPLETE" when it turns true in play, for a chapter whose last word is a
        /// conversation after the quest is paid (chapter 2's was the homecoming's flag, not the quest's). Empty: the quest's own completion.</summary>
        public string cardFlag;
        /// <summary>The words on that card under the quest's title: two lines of the plate, about 190 characters (a test measures them). Empty: the card is the heading and the title
        /// alone, on the shorter plate the chapter opened on, and <see cref="closingLine"/> on the objective panel is the last word.</summary>
        public string cardBody;
        /// <summary>A side quest whose giver or goal is a place the story ends (chapter 1's Decks and the Halo, gone once the districts fly): once this flag is set, the quest, if it isn't
        /// done, leaves the log without a word and without its reward (QuestService), so nothing is left pointing at a place that no longer exists. Empty: it never goes.</summary>
        public string letGoAt;
    }
    /// <summary>Where one step of a quest sends the party: the objective panel measures to it and points at it (ObjectiveHud). Every field is optional.
    /// Outside <see cref="scene"/> the panel points at the way out that leads there, or failing that to the first scene of <see cref="via"/> this zone has a way to
    /// (the scenes between, nearest the goal first). In it, or with none named, it points at the first of these it finds: the speaker, the nearest open discovery
    /// with this report, the encounter (its boss once the bar is up), the point.</summary>
    [System.Serializable] public struct QuestTarget
    {
        public string scene;public string[] via;
        public string speaker,report,encounter;
        public bool hasPoint;public Vector3 point;
        /// <summary>The step in the few words the panel's second line holds, about 40 characters, for a side quest. Empty: the step's own text.</summary>
        public string brief;
    }
}
