using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>What a chapter's two cards say and when, read from its main quest's definition; QuestToast draws them. One as the quest begins ("CHAPTER 3" over the quest's title) and one as
    /// the chapter ends ("CHAPTER 3 COMPLETE", the title, and the quest's <see cref="QuestDef.cardBody"/>). A chapter gets both by defining its one main quest: nothing is written here or in
    /// QuestToast. Chapters 1 and 2 are not here: neither opened on a card, and chapter 2's closing card is QuestToast's own constants, as it shipped, whatever its asset says.</summary>
    public static class ChapterCards
    {
        static QuestDef[] quests;
        static readonly List<QuestDef> handed=new();
        /// <summary>Every main quest past the first three with a chapter to its name, in story order, then any handed in by <see cref="Follow"/>. In play it's read once, as StoryQuests' list is.</summary>
        public static IReadOnlyList<QuestDef> Quests
        {
            get
            {
                if(quests!=null&&Application.isPlaying)return quests;
                var found=new List<QuestDef>();var ids=StoryQuests.Ordered;
                for(int i=StoryQuests.First.Length;i<ids.Length;i++){var quest=GameCatalog.Find<QuestDef>(ids[i]);if(quest!=null&&quest.chapter>0)found.Add(quest);}
                found.AddRange(handed);
                return quests=found.ToArray();
            }
        }
        /// <summary>A main quest the catalog doesn't hold gets its chapter's cards like one it does, until the handle is let go. ChapterCardTests builds a chapter in memory this way, so a card
        /// with its own flag and its own words is proven before any chapter ships one.</summary>
        public static System.IDisposable Follow(QuestDef quest){handed.Add(quest);quests=null;return new Handed(quest);}
        sealed class Handed:System.IDisposable
        {
            QuestDef quest;
            public Handed(QuestDef quest){this.quest=quest;}
            public void Dispose(){if(ReferenceEquals(quest,null))return;handed.Remove(quest);quests=null;quest=null;}
        }
        public static string Opening(QuestDef quest)=>"CHAPTER "+quest.chapter;
        public static string Closing(QuestDef quest)=>"CHAPTER "+quest.chapter+" COMPLETE";
        public static string Title(QuestDef quest)=>(quest.title??"").ToUpperInvariant();
        /// <summary>The closing card's words. None is a card of its heading and title alone.</summary>
        public static string Body(QuestDef quest)=>string.IsNullOrWhiteSpace(quest.cardBody)?"":quest.cardBody.Trim();
        public static bool Begun(QuestDef quest,GameState state)=>state.questSteps.ContainsKey(quest.id);
        public static bool Done(QuestDef quest,GameState state)=>quest.steps!=null&&state.questSteps.TryGetValue(quest.id,out int at)&&at>=quest.steps.Length;
        /// <summary>The chapter is over: the flag its quest names is up (<see cref="QuestDef.cardFlag"/>, the closing conversation's), or with none named the quest is done. The game's last
        /// chapter never is, as far as a card goes (<see cref="EndsOnCredits"/>).</summary>
        public static bool Ended(QuestDef quest,GameState state)=>!EndsOnCredits(quest)&&(string.IsNullOrWhiteSpace(quest.cardFlag)?Done(quest,state):state.flags.TryGetValue(quest.cardFlag,out bool up)&&up);
        /// <summary>The last chapter ends on the credits, not on a card (D288): the one whose closing flag is the game's own end (<see cref="Credits.Flag"/>, which the credits roll on). It
        /// opens on its card like any other; nothing says CHAPTER 10 COMPLETE over the roll or after it.</summary>
        public static bool EndsOnCredits(QuestDef quest)=>quest!=null&&quest.cardFlag==Credits.Flag;
        /// <summary>Play mode reloads the domain here, but an editor that skips the reload would carry the last session's list in.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Forget(){quests=null;handed.Clear();}
    }
}
