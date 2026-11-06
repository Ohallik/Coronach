using Lattice.Combat;
using Lattice.Core;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.World
{
    public sealed class SalvageField:InteractionPrompt
    {
        /// <summary>A chasing fight is still a fight, wherever the crate is: this far from the party counts.</summary>
        const float ChaseRange=35;
        public string cacheId="Neve",requiredFlag="neve.cache";
        /// <summary>Whose it is, for the card; and what it says, once any fight around it is over.</summary>
        public string owner="Neve's cache",dialogueNode,dialogueSpeaker="Neve";
        bool speechWaiting;
        bool Takeable=>GameServices.Current.Flags.GetBool(requiredFlag)&&!GameServices.Current.Flags.GetBool("cache."+cacheId);
        /// <summary>With an enemy on the hero, A is an attack. Flying or walking into the crate still takes it, whatever is awake.</summary>
        public override bool Available=>Takeable&&!EnemyBrain.AwakeNear(transform.position,EnemyBrain.ExitCalm);
        void Start(){if(GameServices.Current.Flags.GetBool("cache."+cacheId))gameObject.SetActive(false);}
        public override void Interact()
        {
            if(!Takeable)return;var inventory=RpgServices.Inventory;
            inventory.Grant("ChoirResin",4);inventory.Grant("ScrapAlloy",12);inventory.Grant("RepairGel",3);
            Notices.Post("CACHE RECOVERED",owner+": Choir Resin ×4, Scrap Alloy ×12, Repair Gel ×3");AudioManager.Play("doorOpen_000",.2f);
            GameServices.Current.Flags.SetBool("cache."+cacheId,true);Debug.Log("SALVAGE_OK "+cacheId);
            if(string.IsNullOrEmpty(dialogueNode)){gameObject.SetActive(false);return;}
            // The crate is gone at once; a recorded message waits until nothing is shooting at the party.
            foreach(var body in GetComponentsInChildren<Renderer>())body.enabled=false;
            foreach(var solid in GetComponentsInChildren<Collider>())solid.enabled=false;
            speechWaiting=true;
        }
        void Update()
        {
            if(!speechWaiting||GameServices.Current.Input.Blocked||BossController.AnyFightUnderway)return;
            if(EnemyBrain.AwakeNear(transform.position,EnemyBrain.InteractionCalm))return;
            var party=PartyController.Current;
            if(party!=null&&party.Active!=null&&EnemyBrain.AwakeNear(party.Active.transform.position,ChaseRange))return;
            speechWaiting=false;Npc.Say(dialogueNode,dialogueSpeaker);gameObject.SetActive(false);
        }
        void OnTriggerEnter(Collider other){if(other.TryGetComponent<Health>(out var h)&&h.friendly)Interact();}
    }
}
