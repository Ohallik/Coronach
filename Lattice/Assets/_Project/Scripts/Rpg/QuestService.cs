using System;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
namespace Lattice.Rpg
{
    public sealed class QuestService
    {
        readonly GameState state;
        readonly Dictionary<string,QuestDef> definitions=new();
        public event Action<string> Changed;
        public QuestService(GameState state,IEnumerable<QuestDef> definitions)
        {this.state=state;foreach(var d in definitions)this.definitions[d.id]=d;}
        public void Start(string id)
        {
            if(!definitions.TryGetValue(id,out var definition))throw new ArgumentException("Unknown quest "+id);
            if(state.questSteps.ContainsKey(id))return;
            state.questSteps[id]=0;Changed?.Invoke(id);
            // Accepting the gathering job after mining/crafting must remain completable.
            if(definition.steps.Length>0&&definition.steps[0].kind==ObjectiveKind.Collect)
            {
                var goal=definition.steps[0];state.collectedMaterials.TryGetValue(goal.target,out int collected);
                state.materials.TryGetValue(goal.target,out int held);int amount=Math.Max(collected,held);
                if(amount>0)Progress(definition,ObjectiveKind.Collect,goal.target,amount);
            }
        }
        /// <summary>Moves every quest past steps already done: a loaded save can hold a quest stuck on a step that can no longer
        /// happen, like a boss killed before the talk that asked for it.</summary>
        public void Reconcile()
        {
            foreach(var d in definitions.Values)
            {
                if(!state.questSteps.TryGetValue(d.id,out var step)||step<0||step>=d.steps.Length)continue;
                int caughtUp=CatchUp(d,step);if(caughtUp==step)continue;
                state.questSteps[d.id]=caughtUp;if(caughtUp==d.steps.Length)Complete(d);Changed?.Invoke(d.id);
            }
        }
        int CatchUp(QuestDef d,int step){while(step<d.steps.Length&&AlreadyDone(d.steps[step]))step++;return step;}
        /// <summary>Steps the world already satisfies and will never report again. Counted interactions can't be known this way.</summary>
        bool AlreadyDone(Objective goal)=>goal.kind switch
        {
            ObjectiveKind.Kill=>Flag("bossdown."+goal.target),
            ObjectiveKind.TalkTo=>Flag("met."+goal.target),
            ObjectiveKind.Flag=>Flag(goal.target),
            ObjectiveKind.Reach=>state.zone==goal.target,
            ObjectiveKind.Collect=>Math.Max(Held(state.collectedMaterials,goal.target),Held(state.materials,goal.target))>=Math.Max(1,goal.count),
            _=>false
        };
        bool Flag(string key)=>state.flags.TryGetValue(key,out bool value)&&value;
        static int Held(Dictionary<string,int> counts,string key)=>counts.TryGetValue(key,out int value)?value:0;
        public void Report(ObjectiveKind kind,string id,int amount=1)
        {
            if(amount<=0)return;
            foreach(var d in definitions.Values)Progress(d,kind,id,amount);
        }
        void Progress(QuestDef d,ObjectiveKind kind,string id,int amount)
        {
                if(!state.questSteps.TryGetValue(d.id,out var step)||step<0||step>=d.steps.Length)return;
                var goal=d.steps[step];if(goal.kind!=kind||goal.target!=id)return;
                string key=d.id+"/"+step;state.questCounts.TryGetValue(key,out int count);count+=amount;state.questCounts[key]=count;
                if(count<Math.Max(1,goal.count)){Changed?.Invoke(d.id);return;}
                step=CatchUp(d,step+1);state.questSteps[d.id]=step;
                if(step==d.steps.Length)Complete(d);
                Changed?.Invoke(d.id);
        }
        void Complete(QuestDef d)
        {
            state.flags["quest."+d.id+".complete"]=true;
            state.scrip+=d.rewardScrip;
            foreach(var member in state.party)Levels.Grant(member,d.rewardXp);
            // A reward goes where its kind lives (D199); the quest card names it, so the pickup feed stays out of it.
            var inv=new Inventory(state);
            foreach(var reward in d.rewards??Array.Empty<Ingredient>())inv.Grant(reward.id,Math.Max(1,reward.count),false);
            foreach(var flag in d.flagsOnComplete??Array.Empty<string>())state.flags[flag]=true;
        }
    }
}
