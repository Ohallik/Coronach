using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
namespace Lattice.World
{
    public sealed class MineNode:InteractionPrompt
    {
        public string nodeId,material="RidgeCrystal";
        public int amount=1;
        public override bool Available=>!GameServices.Current.Flags.GetBool("mined."+nodeId);
        public override void Interact()
        {
            if(!Available)return;RpgServices.Inventory.Give(material,amount);GameServices.Current.Flags.SetBool("mined."+nodeId,true);
            RpgServices.Quests.Report(ObjectiveKind.Collect,material,amount);gameObject.SetActive(false);
        }
    }
}
