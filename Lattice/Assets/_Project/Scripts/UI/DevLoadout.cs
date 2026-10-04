using System;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.UI
{
    public static class DevLoadout
    {
        public static void Apply(string name)
        {
            if(string.IsNullOrEmpty(name))return;
            if(name!="starter"&&name!="moon"&&name!="hushwell"&&name!="gullet")throw new ArgumentException("Unknown loadout. Use starter, moon, hushwell or gullet.");
            var state=GameServices.Current.State;int level=name=="gullet"||name=="hushwell"?5:name=="moon"?3:1;
            if(!state.party.Exists(m=>m.id=="Sela"))state.party.Add(new MemberState{id="Sela"});
            foreach(var member in state.party){member.level=level;member.xp=0;member.integrity=Levels.MaxIntegrity(level);member.charge=100;}
            state.consumables["RepairGel"]=8;state.consumables["ChargeCell"]=4;
            foreach(string id in new[]{"ScrapAlloy","LatticeFilament","RidgeCrystal"})state.materials[id]=20;
            if(name!="starter")
            {
                var inv=new Inventory(state);
                foreach(var member in state.party)
                {
                    var part=GameCatalog.Find<TechPartDef>(member.id=="Taren"?"EdgesT2":"EmittersT2");
                    if(part!=null)inv.Equip(member,inv.GivePart(part),part);
                }
            }
            // These fixtures represent distinct points in the chapter. A Hushwell
            // run must still earn the discovery that a Gullet party already owns.
            if(name=="gullet"||name=="hushwell")
            {
                foreach(string flag in new[]{"met.Survivor","bossdown.Burrower","clear.Sorrel_Burrower","warpkey","quest.Sorrel.complete"})state.flags[flag]=true;
                state.questSteps["Sorrel"]=4;
            }
            if(name=="gullet")
            {
                foreach(string flag in new[]{"bossdown.BellowsBelow","clear.Hushwell_0","clear.Hushwell_1","clear.Hushwell_2","clear.Hushwell_3","clear.Hushwell_Bellows","hushwell.nursery","quest.Hushwell.complete"})state.flags[flag]=true;
                state.questSteps["Hushwell"]=2;
            }
            Debug.Log("DEV_LOADOUT_OK "+name);
        }
    }
}
