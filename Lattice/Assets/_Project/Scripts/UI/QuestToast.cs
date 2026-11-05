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
    /// </summary>
    public sealed class QuestToast:MonoBehaviour
    {
        Canvas canvas;Image frame;TMP_Text heading,body;
        GameState watched;readonly Dictionary<string,int> seen=new();readonly Queue<(string heading,string body)> pending=new();
        float showing,next;
        public string Heading=>heading!=null?heading.text:"";
        public string Body=>body!=null?body.text:"";
        public bool Showing=>frame!=null&&frame.gameObject.activeSelf;
        void Start()
        {
            canvas=UiKit.CreateCanvas("QuestToast",7,transform);
            frame=UiKit.DarkFrame(canvas.transform,"QuestToastFrame");frame.raycastTarget=false;
            UiKit.Rect(frame.gameObject,new(1,1),new(1,1),new(-300,-330),new(570,118));
            heading=UiKit.Text(frame.transform,"QuestToastHeading","",21,new Color(1,.78f,.36f),TextAlignmentOptions.MidlineLeft);
            UiKit.Rect(heading.gameObject,new(.5f,1),new(.5f,1),new(0,-24),new(510,30));
            body=UiKit.Text(frame.transform,"QuestToastBody","",22,UiKit.TextColor,TextAlignmentOptions.TopLeft);
            UiKit.Rect(body.gameObject,new(.5f,1),new(.5f,1),new(0,-70),new(510,64));
            frame.gameObject.SetActive(false);
            Lattice.Rpg.Levels.LevelRaised+=OnLevel;SkillBook.MasteryRaised+=OnMastery;
        }
        void OnDestroy(){Lattice.Rpg.Levels.LevelRaised-=OnLevel;SkillBook.MasteryRaised-=OnMastery;}
        /// <summary>Level-ups and newly learned skills share the card (D201).</summary>
        void OnLevel(MemberState member,int from,int to)
        {
            string who=DisplayNames.Speaker(member.id);var learned=SkillBook.LearnedBetween(member.id,from,to);
            string text=learned.Length==0?$"{who} reached Sync {to}.":$"{who} learned {string.Join(" and ",System.Array.ConvertAll(learned,s=>s.displayName))}. Equip it from Skills.";
            pending.Enqueue((learned.Length==0?$"SYNC {to}":$"SYNC {to}  ·  NEW SKILL",text));
        }
        void OnMastery(string character,SkillDef skill,int level)=>
            pending.Enqueue(($"MASTERY {level}",$"{DisplayNames.Speaker(character)}'s {skill.displayName} hits harder and costs less Charge."));
        void Update()
        {
            var service=GameServices.Current;if(service==null||canvas==null)return;
            var state=service.State;
            if(!ReferenceEquals(state,watched)){watched=state;seen.Clear();pending.Clear();showing=0;foreach(var pair in state.questSteps)seen[pair.Key]=pair.Value;frame.gameObject.SetActive(false);return;}
            if(Time.unscaledTime>=next)
            {
                next=Time.unscaledTime+.2f;
                foreach(var pair in state.questSteps)
                {
                    bool known=seen.TryGetValue(pair.Key,out int was);
                    if(known&&was==pair.Value)continue;
                    seen[pair.Key]=pair.Value;Announce(pair.Key,known?was:-1,pair.Value);
                }
            }
            bool blocked=GameInput.Current!=null&&GameInput.Current.Blocked;
            if(showing<=0&&pending.Count>0&&!blocked)
            {
                var (h,b)=pending.Dequeue();heading.text=h;body.text=b;showing=4.5f;
                AudioManager.Play("forceField_000",.08f);
            }
            if(showing>0&&!blocked)showing-=Time.unscaledDeltaTime;
            bool visible=showing>0&&!blocked;
            if(frame.gameObject.activeSelf!=visible)frame.gameObject.SetActive(visible);
        }
        void Announce(string id,int was,int now)
        {
            var quest=GameCatalog.Find<QuestDef>(id);if(quest==null)return;
            bool done=now>=quest.steps.Length;
            string h=done?"QUEST COMPLETE":was<0?"NEW QUEST":"QUEST UPDATED";
            string b=done?$"{quest.title}\n+{quest.rewardXp} XP   +{quest.rewardScrip} scrip"+Rewards(quest):$"{quest.title}\n{QuestText.Step(quest,now,GameServices.Current.State)}";
            pending.Enqueue((h,b));
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
            int goal=quest.steps[step].count;
            if(goal>1)
            {
                int count=0;state?.questCounts.TryGetValue(quest.id+"/"+step,out count);
                text+=$" ({Mathf.Min(count,goal)}/{goal})";
            }
            return text;
        }
        public static string Readable(string value)=>System.Text.RegularExpressions.Regex.Replace(value??"","(?<=[a-z0-9])(?=[A-Z])"," ");
    }
}
