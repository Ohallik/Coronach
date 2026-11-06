using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.World
{
    public sealed class KeyPickup:InteractionPrompt
    {
        public string requiredFlag="bossdown.Burrower",grantFlag="warpkey",item="WarpKey";
        public override bool Available=>GameServices.Current.Flags.GetBool(requiredFlag)&&!GameServices.Current.Flags.GetBool(grantFlag);
        void Start(){if(GameServices.Current.Flags.GetBool(grantFlag))gameObject.SetActive(false);}
        public override void Interact()
        {
            if(!Available)return;RpgServices.Inventory.Grant(item,1);
            GameServices.Current.Flags.SetBool(grantFlag,true);Debug.Log("QUEST_STEP "+grantFlag);
            GameServices.Current.Saves.Save("autosave",GameServices.Current.State);gameObject.SetActive(false);
        }
    }
}
