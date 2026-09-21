using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.World;
using UnityEngine;
namespace Lattice.UI
{
    public sealed class WorldUiBridge:MonoBehaviour
    {
        void OnEnable(){Npc.TalkRequested+=Talk;Lattice.World.Bench.Requested+=Bench;Lattice.World.Shop.Requested+=Shop;DialogueSystem.UiRequested+=Request;DialogueSystem.MemberJoined+=Join;}
        void OnDisable(){Npc.TalkRequested-=Talk;Lattice.World.Bench.Requested-=Bench;Lattice.World.Shop.Requested-=Shop;DialogueSystem.UiRequested-=Request;DialogueSystem.MemberJoined-=Join;}
        void Talk(string node,string speaker)=>DialogueSystem.Current.StartNode(node,speaker);
        void Bench()=>GetComponent<PauseMenu>().Open(2,true);
        void Shop(ShopInventory inventory)=>GetComponent<ShopUi>().Open(inventory);
        void Request(string action){if(action=="bench")Bench();else if(action=="shop")Shop(null);else if(action=="repair")RepairBay.Repair();}
        void Join(string id)
        {
            var party=PartyController.Current;var actor=ActorFactory.Hero(id,party.Active.transform.position+Vector3.right*2);actor.transform.SetParent(party.transform);
            var members=new System.Collections.Generic.List<CombatActor>(party.members){actor};party.members=members.ToArray();party.Apply();
        }
    }
}
