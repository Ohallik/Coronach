using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>
    /// A short card under the objective panel when a task begins, moves on or completes (D199), with the next step's
    /// text or the reward. It watches the saved quest steps rather than service events, so loading a save or starting a
    /// new game announces nothing, and a quest begun inside a conversation is announced once that conversation closes.
    /// What it has seen and has yet to show outlives the scene, so a step reached by arriving is announced, and a zone
    /// change doesn't throw away a card waiting behind a level-up.
    /// </summary>
    public sealed class QuestToast:MonoBehaviour
    {
        Canvas canvas;Image frame;TMP_Text heading,body,readHint;bool readable;
        static GameState watched;
        static readonly Dictionary<string,int> seen=new(),seenCounts=new();
        static readonly List<(string heading,string body)> pending=new();
        float showing,next;
        const string ChapterEnd="chapter2.homecoming",ChapterEndHeading="CHAPTER 2 COMPLETE",ChapterEndBody="The story stops here in this build, for today. Xylos comes next. Nothing closes behind you, so keep exploring.";
        const float ChapterEndSeconds=9;
        /// <summary>The chapter's last conversation has been heard. Read from the state on adoption, so a load says nothing.</summary>
        static bool chapterEnded;
        const float TextWidth=490;
        public string Heading=>heading!=null?heading.text:"";
        public string Body=>body!=null?body.text:"";
        public bool Showing=>frame!=null&&frame.gameObject.activeSelf;
        void Start()
        {
            canvas=UiKit.CreateCanvas("QuestToast",7,transform);
            frame=UiKit.DarkFrame(canvas.transform,"QuestToastFrame");frame.raycastTarget=false;frame.pixelsPerUnitMultiplier=2;
            var box=UiKit.Rect(frame.gameObject,new(1,1),new(1,1),new(-300,-272),new(570,118));box.pivot=new(.5f,1);box.anchoredPosition=new(-300,-272);
            heading=UiKit.Text(frame.transform,"QuestToastHeading","",24,new Color(1,.78f,.36f),TextAlignmentOptions.MidlineLeft);
            UiKit.Rect(heading.gameObject,new(.5f,1),new(.5f,1),new(0,-40),new(TextWidth,30));
            body=UiKit.Text(frame.transform,"QuestToastBody","",24,UiKit.TextColor,TextAlignmentOptions.TopLeft);
            var words=UiKit.Rect(body.gameObject,new(.5f,1),new(.5f,1),new(0,-58),new(TextWidth,30));words.pivot=new(.5f,1);words.anchoredPosition=new(0,-58);
            frame.gameObject.SetActive(false);
            readHint=UiKit.Text(frame.transform,"QuestToastHint","",24,UiKit.DimTextColor,TextAlignmentOptions.MidlineRight);
            UiKit.Rect(readHint.gameObject,new(.5f,1),new(.5f,1),new(0,-40),new(TextWidth,30));
            Lattice.Rpg.Levels.LevelRaised+=OnLevel;SkillBook.MasteryRaised+=OnMastery;Notices.Posted+=OnNotice;PromptService.Changed+=RefreshHint;
        }
        void OnDestroy(){Lattice.Rpg.Levels.LevelRaised-=OnLevel;SkillBook.MasteryRaised-=OnMastery;Notices.Posted-=OnNotice;PromptService.Changed-=RefreshHint;}
        /// <summary>A card about a quest says which button opens the log, on the device in the player's hand.</summary>
        void RefreshHint(){if(readHint!=null)readHint.text=readable?PromptService.Tag("Log")+"  TO READ":"";}
        /// <summary>Level-ups and newly learned skills share the card (D201). Heroes rise together, so one level is one card.</summary>
        void OnLevel(MemberState member,int from,int to)
        {
            string who=DisplayNames.Speaker(member.id);var learned=SkillBook.LearnedBetween(member.id,from,to);
            string text=learned.Length==0?$"{who} reached Sync {to}.":$"{who} learned {string.Join(" and ",System.Array.ConvertAll(learned,s=>s.displayName))}. Equip it from Skills.";
            string title=learned.Length==0?$"SYNC {to}":$"SYNC {to}  ·  NEW SKILL";
            for(int i=pending.Count-1;i>=0;i--)
            {
                if(!pending[i].heading.StartsWith($"SYNC {to}"))continue;
                pending[i]=(learned.Length>0?title:pending[i].heading,pending[i].body+"\n"+text);return;
            }
            pending.Add((title,text));
        }
        void OnMastery(string character,SkillDef skill,int level)=>
            pending.Add(($"MASTERY {level}",$"{DisplayNames.Speaker(character)}'s {skill.displayName} hits harder and costs less Charge."));
        /// <summary>The result of something the player just pressed: shown next, not behind a queue of level cards.</summary>
        void OnNotice(string h,string b){pending.Insert(0,(h,b));showing=Mathf.Min(showing,.4f);}
        void Update()
        {
            var service=GameServices.Current;if(service==null||canvas==null)return;
            var state=service.State;
            if(!ReferenceEquals(state,watched))
            {
                watched=state;seen.Clear();seenCounts.Clear();pending.Clear();showing=0;chapterEnded=state.flags.TryGetValue(ChapterEnd,out bool ended)&&ended;
                foreach(var pair in state.questSteps)seen[pair.Key]=pair.Value;foreach(var pair in state.questCounts)seenCounts[pair.Key]=pair.Value;
                frame.gameObject.SetActive(false);return;
            }
            if(Time.unscaledTime>=next)Scan(state);
            bool blocked=GameInput.Current!=null&&GameInput.Current.Blocked;
            if(showing<=0&&pending.Count>0&&!blocked)
            {
                // A quest's level-up queues the moment it pays and the quest itself only shows up in the scan: look first, so QUEST COMPLETE goes ahead of its own SYNC card.
                Scan(state);
                var (h,b)=pending[0];pending.RemoveAt(0);heading.text=h;body.text=b;showing=h==ChapterEndHeading?ChapterEndSeconds:pending.Count>0?3.2f:4.5f;readable=h=="NEW QUEST"||h=="QUEST UPDATED";RefreshHint();
                // The frame grows with its words, so a long step or a double skill never crosses its edge (QA-005).
                float high=body.GetPreferredValues(b,TextWidth,0).y;
                body.rectTransform.sizeDelta=new(TextWidth,high);frame.rectTransform.sizeDelta=new(570,58+high+34);
                AudioManager.Play("forceField_000",.08f);
            }
            if(showing>0&&!blocked)showing-=Time.unscaledDeltaTime;
            bool visible=showing>0&&!blocked;
            if(frame.gameObject.activeSelf!=visible)frame.gameObject.SetActive(visible);
        }
        void Scan(GameState state)
        {
            next=Time.unscaledTime+.2f;
            foreach(var pair in state.questSteps)
            {
                bool known=seen.TryGetValue(pair.Key,out int was);
                string counted=pair.Key+"/"+pair.Value;state.questCounts.TryGetValue(counted,out int count);seenCounts.TryGetValue(counted,out int counts);
                if(known&&was==pair.Value)
                {
                    // A counted step moved without finishing: one plate of three.
                    if(count>counts){seenCounts[counted]=count;Announce(pair.Key,pair.Value,pair.Value);}
                    continue;
                }
                seen[pair.Key]=pair.Value;seenCounts[counted]=count;Announce(pair.Key,known?was:-1,pair.Value);
            }
            // The chapter's last word: it follows whatever its conversation paid, and the conversation holds it until it closes.
            if(!chapterEnded&&state.flags.TryGetValue(ChapterEnd,out bool ended)&&ended){chapterEnded=true;pending.Add((ChapterEndHeading,ChapterEndBody));}
        }
        void Announce(string id,int was,int now)
        {
            var quest=GameCatalog.Find<QuestDef>(id);if(quest==null)return;
            bool done=now>=quest.steps.Length;
            string h=done?"QUEST COMPLETE":was<0?"NEW QUEST":"QUEST UPDATED";
            string b=done?$"{quest.title}\n+{quest.rewardXp} XP   +{quest.rewardScrip} scrip"+Rewards(quest):$"{quest.title}\n{QuestText.Step(quest,now,GameServices.Current.State)}";
            // Ahead of level cards the same moment queued: the quest is what the player just did.
            int at=0;while(at<pending.Count&&!pending[at].heading.StartsWith("SYNC")&&!pending[at].heading.StartsWith("MASTERY"))at++;
            pending.Insert(at,(h,b));
        }
        static string Rewards(QuestDef quest)
        {
            if(quest.rewards==null||quest.rewards.Length==0)return "";
            var parts=new List<string>();foreach(var reward in quest.rewards)parts.Add(reward.count>1?$"{DisplayNames.Item(reward.id)} ×{reward.count}":DisplayNames.Item(reward.id));
            return "   "+string.Join(", ",parts);
        }
    }
    /// <summary>Player-facing quest text shared by the toast and the quest log.</summary>
    public static class QuestText
    {
        public static string Step(QuestDef quest,int step,GameState state=null)
        {
            if(step>=quest.steps.Length)return "Complete";
            string text=quest.stepText!=null&&step<quest.stepText.Length&&!string.IsNullOrEmpty(quest.stepText[step])?quest.stepText[step]:$"Step {step+1} of {quest.steps.Length}";
            return text+Count(quest,step,state);
        }
        static string Count(QuestDef quest,int step,GameState state)
        {
            int goal=quest.steps[step].count;if(goal<=1)return "";
            int count=0;state?.questCounts.TryGetValue(quest.id+"/"+step,out count);
            return $" ({Mathf.Min(count,goal)}/{goal})";
        }
        /// <summary>A side quest's step in the few words the objective panel has a line for, and who it waits on when that's a person. The
        /// log keeps the directions: a step runs to 87 characters and the panel's line holds about 40.</summary>
        static readonly Dictionary<string,(string words,string speaker)> Briefs=new()
        {
            {"PellsLamp/0",("Search the grazer den",null)},{"PellsLamp/1",("Bring the lamp to Pell","Pell")},
            {"IshensRecord/0",("Recover the plates",null)},{"IshensRecord/1",("Bring the plates to Ishen","Ishen")},
            {"UnswornRations/0",("Take a ration crate",null)},{"UnswornRations/1",("Bring rations to the shed","Brekk")},
            {"SorrelBeacons/0",("Silence the beacons",null)},{"SorrelBeacons/1",("Tell Dace it's quiet","Survivor")},
            {"MiraCrystals/0",("Mine Ridge Crystals",null)},
            {"NeveParcel/0",("Take the parcel to Dace","Survivor")},{"NeveParcel/1",("Bring the reply to Neve","Neve")},
            {"OdaBerth/0",("Ask Orrin for the token","Orrin")},{"OdaBerth/1",("Bring the token to Oda","Oda")},
        };
        public static string Brief(QuestDef quest,int step,GameState state=null)=>Briefs.TryGetValue(quest.id+"/"+step,out var brief)?brief.words+Count(quest,step,state):Step(quest,step,state);
        public static string WaitsOn(QuestDef quest,int step)=>Briefs.TryGetValue(quest.id+"/"+step,out var brief)?brief.speaker:null;
        public static string Readable(string value)=>System.Text.RegularExpressions.Regex.Replace(value??"","(?<=[a-z0-9])(?=[A-Z])"," ");
    }
}
