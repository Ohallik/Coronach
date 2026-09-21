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
                if(count<Math.Max(1,goal.count))return;
                state.questSteps[d.id]=++step;
                if(step==d.steps.Length)
                {
                    state.flags["quest."+d.id+".complete"]=true;
                    state.scrip+=d.rewardScrip;
                    foreach(var member in state.party)Levels.Grant(member,d.rewardXp);
                    var inv=new Inventory(state);foreach(var reward in d.rewards??Array.Empty<Ingredient>())inv.Give(reward.id,reward.count);
                    foreach(var flag in d.flagsOnComplete??Array.Empty<string>())state.flags[flag]=true;
                }
                Changed?.Invoke(d.id);
        }
    }
}
