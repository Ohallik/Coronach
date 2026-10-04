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
        void OnEnable(){Npc.TalkRequested+=Talk;DiscoveryPoint.DialogueRequested+=Discover;Lattice.World.Bench.Requested+=Bench;Lattice.World.Shop.Requested+=Shop;DialogueSystem.UiRequested+=Request;DialogueSystem.MemberJoined+=Join;}
        void OnDisable(){Npc.TalkRequested-=Talk;DiscoveryPoint.DialogueRequested-=Discover;Lattice.World.Bench.Requested-=Bench;Lattice.World.Shop.Requested-=Shop;DialogueSystem.UiRequested-=Request;DialogueSystem.MemberJoined-=Join;}
        void Talk(string node,string speaker)=>DialogueSystem.Current.StartNode(node,speaker);
        void Discover(DiscoveryPoint point)
        {
            var dialogue=DialogueSystem.Current;
            if(dialogue==null||!dialogue.StartNode(point.dialogueNode,point.dialogueSpeaker,completed=>{if(point!=null)point.CompleteDialogue(completed);}))
                point.CompleteDialogue(false);
        }
        void Bench()=>GetComponent<PauseMenu>().Open(2,true);
        void Shop(ShopInventory inventory)=>GetComponent<ShopUi>().Open(inventory);
        void Request(string action){if(action=="bench")Bench();else if(action=="shop")Shop(null);else if(action=="repair")RepairBay.Repair();}
        void Join(string id)
        {
            var party=PartyController.Current;float spacing=party.Active.flight?HeroCollision.ArrivalSpacing:2;
            var actor=ActorFactory.Hero(id,party.Active.transform.position+Vector3.right*spacing);actor.transform.SetParent(party.transform);
            var members=new System.Collections.Generic.List<CombatActor>(party.members){actor};party.members=members.ToArray();party.Apply();
        }
    }
}
