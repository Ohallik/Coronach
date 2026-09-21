using Lattice.Core;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.World
{
    public sealed class SalvageField:InteractionPrompt
    {
        public string cacheId="Neve",requiredFlag="neve.cache";
        public override bool Available=>GameServices.Current.Flags.GetBool(requiredFlag)&&!GameServices.Current.Flags.GetBool("cache."+cacheId);
        public override void Interact()
        {
            if(!Available)return;RpgServices.Inventory.Give("ChoirResin",4);RpgServices.Inventory.Give("ScrapAlloy",12);
            var state=GameServices.Current.State;state.consumables.TryGetValue("RepairGel",out int count);state.consumables["RepairGel"]=count+3;
            GameServices.Current.Flags.SetBool("cache."+cacheId,true);Debug.Log("SALVAGE_OK "+cacheId);gameObject.SetActive(false);
        }
        void OnTriggerEnter(Collider other){if(other.TryGetComponent<Lattice.Combat.Health>(out var h)&&h.friendly)Interact();}
    }
}
