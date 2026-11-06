using Lattice.Core;
using Lattice.Rpg;
namespace Lattice.World
{
    public sealed class MineNode:InteractionPrompt
    {
        public string nodeId,material="RidgeCrystal";
        public int amount=1;
        public override bool Available=>!GameServices.Current.Flags.GetBool("mined."+nodeId)&&!Lattice.Combat.EnemyBrain.AwakeNear(transform.position,Lattice.Combat.EnemyBrain.InteractionCalm);
        // A mined crystal stays mined after a reload, not a whole one with no prompt.
        void Start(){if(GameServices.Current.Flags.GetBool("mined."+nodeId))gameObject.SetActive(false);}
        public override void Interact()
        {
            if(!Available)return;RpgServices.Inventory.Grant(material,amount);GameServices.Current.Flags.SetBool("mined."+nodeId,true);
            AudioManager.Play("impactMetal_000",.12f);gameObject.SetActive(false);
        }
    }
}
