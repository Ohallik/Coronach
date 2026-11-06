using Lattice.Core;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.World
{
    public sealed class SalvageField:InteractionPrompt
    {
        public string cacheId="Neve",requiredFlag="neve.cache";
        public override bool Available=>GameServices.Current.Flags.GetBool(requiredFlag)&&!GameServices.Current.Flags.GetBool("cache."+cacheId);
        void Start(){if(GameServices.Current.Flags.GetBool("cache."+cacheId))gameObject.SetActive(false);}
        public override void Interact()
        {
            if(!Available)return;var inventory=RpgServices.Inventory;
            inventory.Grant("ChoirResin",4);inventory.Grant("ScrapAlloy",12);inventory.Grant("RepairGel",3);
            Notices.Post("CACHE RECOVERED","Neve's cache: Choir Resin ×4, Scrap Alloy ×12, Repair Gel ×3");AudioManager.Play("doorOpen_000",.2f);
            GameServices.Current.Flags.SetBool("cache."+cacheId,true);Debug.Log("SALVAGE_OK "+cacheId);gameObject.SetActive(false);
        }
        void OnTriggerEnter(Collider other){if(other.TryGetComponent<Lattice.Combat.Health>(out var h)&&h.friendly)Interact();}
    }
}
