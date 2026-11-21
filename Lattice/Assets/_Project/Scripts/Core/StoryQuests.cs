using System.Collections.Generic;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Core
{
    /// <summary>Which quests are the story's, read from the definitions (QuestDef.main) instead of a list kept by hand in the pause menu: the log leads
    /// with them, the objective panel's line follows the open one, and the side-quest rules leave them out. A chapter adds its main quest by defining it.</summary>
    public static class StoryQuests
    {
        /// <summary>Chapters 1 and 2. They are main whatever their assets say, in this order, and their objective lines are the ones written out by hand
        /// in ObjectiveHud. Nothing is added here.</summary>
        public static readonly string[] First={"Sorrel","Hushwell","LanternRoad"};
        static string[] ordered;
        /// <summary>Every main quest's id in story order: the first three, then by chapter, then by id. In play it's read once; outside play, where builders
        /// write new definitions, every time.</summary>
        public static string[] Ordered
        {
            get
            {
                if(ordered!=null&&Application.isPlaying)return ordered;
                var later=new List<QuestDef>();
                foreach(var quest in GameCatalog.All<QuestDef>())if(quest!=null&&quest.main&&System.Array.IndexOf(First,quest.id)<0)later.Add(quest);
                later.Sort((a,b)=>a.chapter!=b.chapter?a.chapter.CompareTo(b.chapter):string.CompareOrdinal(a.id,b.id));
                var ids=new string[First.Length+later.Count];First.CopyTo(ids,0);
                for(int i=0;i<later.Count;i++)ids[First.Length+i]=later[i].id;
                return ordered=ids;
            }
        }
        public static bool IsMain(string id)=>System.Array.IndexOf(Ordered,id)>=0;
        public static bool IsMain(QuestDef quest)=>quest!=null&&(quest.main||System.Array.IndexOf(First,quest.id)>=0);
        /// <summary>The story quest that is open, and the step it's at: the latest chapter's if two are. With <paramref name="pastTheFirst"/>, only one from
        /// chapter 3 on, whose line comes from its data wherever the party stands.</summary>
        public static QuestDef Open(GameState state,out int step,bool pastTheFirst=false)
        {
            step=-1;var ids=Ordered;
            for(int i=ids.Length-1;i>=(pastTheFirst?First.Length:0);i--)
            {
                if(!state.questSteps.TryGetValue(ids[i],out int at)||at<0)continue;
                var quest=GameCatalog.Find<QuestDef>(ids[i]);if(quest==null||quest.steps==null||at>=quest.steps.Length)continue;
                step=at;return quest;
            }
            return null;
        }
        /// <summary>The story has gone past the first two chapters: a main quest from chapter 3 on has been started, open or done. From then on the
        /// objective panel's hand-written lines have had their say and the quests' own data speaks everywhere.</summary>
        public static bool BegunPastTheFirst(GameState state)
        {
            var ids=Ordered;
            for(int i=First.Length;i<ids.Length;i++)if(state.questSteps.ContainsKey(ids[i]))return true;
            return false;
        }
        /// <summary>The last word of the latest story quest that is done (QuestDef.closingLine), for when nothing is open; null if it left none.</summary>
        public static string Closing(GameState state)
        {
            var quest=Told(state);
            return quest==null||string.IsNullOrWhiteSpace(quest.closingLine)?null:quest.closingLine;
        }
        /// <summary>The latest story quest that is done: the chapter just told, whose closing line and <see cref="QuestDef.next"/> stand while nothing is open.</summary>
        public static QuestDef Told(GameState state)
        {
            var ids=Ordered;
            for(int i=ids.Length-1;i>=0;i--)
            {
                if(!state.questSteps.TryGetValue(ids[i],out int at))continue;
                var quest=GameCatalog.Find<QuestDef>(ids[i]);if(quest==null||quest.steps==null||at<quest.steps.Length)continue;
                return quest;
            }
            return null;
        }
        /// <summary>Where the story sends the party between chapters: the told chapter's <see cref="QuestDef.next"/>, when no story quest is open and it names anything.</summary>
        public static bool Between(GameState state,out QuestTarget next)
        {
            next=default;if(Open(state,out _)!=null)return false;
            var quest=Told(state);if(quest==null)return false;
            next=quest.next;
            return !string.IsNullOrEmpty(next.scene)||!string.IsNullOrEmpty(next.speaker)||!string.IsNullOrEmpty(next.report)||next.hasPoint;
        }
        /// <summary>Play mode reloads the domain here, but an editor that skips the reload would carry the last session's list in.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Forget()=>ordered=null;
    }
}
