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
            if(name!="starter"&&name!="moon"&&name!="gullet")throw new ArgumentException("Unknown loadout. Use starter, moon or gullet.");
            var state=GameServices.Current.State;int level=name=="gullet"?5:name=="moon"?3:1;
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
            // The warp key comes only from the anvil the Burrower guards, so a gullet
            // party has met the survivor and already won that arena.
            if(name=="gullet")foreach(string flag in new[]{"met.Survivor","bossdown.Burrower","clear.Sorrel_Burrower","warpkey"})state.flags[flag]=true;
            Debug.Log("DEV_LOADOUT_OK "+name);
        }
    }
}
