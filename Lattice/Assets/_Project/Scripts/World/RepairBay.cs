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
        public override void Interact(){if(!string.IsNullOrEmpty(saveSpawn))GameServices.Current.State.spawn=saveSpawn;Repair(free,CompleteSlice);}
        public static bool Repair(bool free=false,bool complete=false)
        {
            var state=GameServices.Current.State;int fee=free?0:state.party[0].level*8;
            if(state.scrip<fee)return false;state.scrip-=fee;
            foreach(var m in state.party){m.integrity=Levels.MaxIntegrity(m.level);m.charge=100;}
            if(PartyController.Current!=null)foreach(var m in PartyController.Current.members){m.Health.ResetFull();m.charge=100;}
            GameServices.Current.Saves.Save("autosave",state);
            if(complete){state.flags["sliceComplete"]=true;GameServices.Current.Saves.Save("autosave",state);Debug.Log("SLICE_COMPLETE");}
            return true;
        }
    }
}
