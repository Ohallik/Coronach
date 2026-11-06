using Lattice.Combat;
using Lattice.Core;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.World
{
    public sealed class RepairBay:InteractionPrompt
    {
        public bool free;
        public bool CompleteSlice;
        public string saveSpawn;
        const string FeeSuffix=" — 8 scrip per Sync level";
        string wording;
        public static int Fee(GameState state)=>state.party[0].level*8;
        /// <summary>No full heal beside a live fight: a free cradle next to a boss made the fight meaningless.</summary>
        public override bool Available=>!BossController.AnyFightUnderway&&!EnemyBrain.AwakeNear(transform.position,EnemyBrain.InteractionCalm);
        void Awake(){wording=prompt.Replace(FeeSuffix,"");}
        // Every paid prompt states its fee, as the number the player will actually pay.
        void Update()
        {
            if(free||GameServices.Current==null)return;
            wording??=prompt.Replace(FeeSuffix,"");
            string text=wording+" — "+Fee(GameServices.Current.State)+" scrip";
            if(prompt!=text)prompt=text;
        }
        public override void Interact()
        {
            string before=GameServices.Current.State.spawn;
            if(!string.IsNullOrEmpty(saveSpawn))GameServices.Current.State.spawn=saveSpawn;
            if(!Repair(free,CompleteSlice))GameServices.Current.State.spawn=before;
        }
        public static bool Repair(bool free=false,bool complete=false)
        {
            var state=GameServices.Current.State;int fee=free?0:Fee(state);
            bool damaged=false;
            foreach(var m in state.party)if(m.integrity<Levels.MaxIntegrity(m.level)-.01f)damaged=true;
            if(!damaged)fee=0; // Saving costs nothing; only a repair does.
            if(state.scrip<fee)
            {
                AudioManager.Play("forceField_000",.05f);
                Notices.Post("NOT ENOUGH SCRIP",$"A repair costs {fee} scrip. You have {state.scrip}.");return false;
            }
            state.scrip-=fee;
            foreach(var m in state.party){m.integrity=Levels.MaxIntegrity(m.level);m.charge=100;}
            if(PartyController.Current!=null)foreach(var m in PartyController.Current.members){m.Health.ResetFull();m.charge=100;}
            GameServices.Current.Saves.Save("autosave",state);
            if(complete){state.flags["sliceComplete"]=true;GameServices.Current.Saves.Save("autosave",state);Debug.Log("SLICE_COMPLETE");}
            AudioManager.Play("forceField_000",.14f);
            Notices.Post(damaged?"REPAIRED AND SAVED":"SAVED",(damaged?"Integrity and Charge restored.":"Your progress is saved.")+(fee>0?$" −{fee} scrip.":""));
            return true;
        }
    }
}
