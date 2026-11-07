using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.World
{
    public sealed class KeyPickup:InteractionPrompt
    {
        public string requiredFlag="bossdown.Burrower",grantFlag="warpkey",item="WarpKey";
        /// <summary>The key itself, sitting in the drill's cradle: this is what goes when it's taken. The machine stays where it is.</summary>
        public GameObject visual;
        public override bool Available=>GameServices.Current.Flags.GetBool(requiredFlag)&&!GameServices.Current.Flags.GetBool(grantFlag)&&!EnemyBrain.AwakeNear(transform.position,EnemyBrain.ExitCalm);
        void Start(){if(GameServices.Current.Flags.GetBool(grantFlag))Taken();}
        void Taken(){if(visual!=null)visual.SetActive(false);else gameObject.SetActive(false);}
        public override void Interact()
        {
            if(!Available)return;RpgServices.Inventory.Grant(item,1);
            GameServices.Current.Flags.SetBool(grantFlag,true);Debug.Log("QUEST_STEP "+grantFlag);
            GameServices.Current.Saves.Save("autosave",GameServices.Current.State);Taken();
        }
    }
}
