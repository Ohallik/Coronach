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
        void Awake()=>gameObject.AddComponent<ConversationFraming>();
        void OnEnable(){Npc.TalkRequested+=Talk;DiscoveryPoint.DialogueRequested+=Discover;Lattice.World.Bench.Requested+=Bench;Lattice.World.Shop.Requested+=Shop;DialogueSystem.UiRequested+=Request;DialogueSystem.MemberJoined+=Join;DialogueSystem.Settled+=Keep;}
        void OnDisable(){Npc.TalkRequested-=Talk;DiscoveryPoint.DialogueRequested-=Discover;Lattice.World.Bench.Requested-=Bench;Lattice.World.Shop.Requested-=Shop;DialogueSystem.UiRequested-=Request;DialogueSystem.MemberJoined-=Join;DialogueSystem.Settled-=Keep;}
        GameState keep;
        void Keep()=>keep=GameServices.Current.State;
        /// <summary>QUEST COMPLETE or a chapter's card on screen has to survive a quit: nothing else writes the save until the next dock. It's written in a safe place only, as a dock
        /// or a repair would be: it waits out a fight and anyone down, and another conversation, whose half-given rewards would pay twice on a Continue. The proving grounds never
        /// save (SceneFlow), or Continue would land there. A frame after the conversation closed, so the write doesn't land on the frame that pays the quest.</summary>
        void Update()
        {
            if(keep==null)return;
            var services=GameServices.Current;var flow=SceneFlow.Current;var party=PartyController.Current;
            if(services==null||!ReferenceEquals(keep,services.State)||flow==null||string.IsNullOrEmpty(flow.Zone)||flow.Zone.StartsWith("Arena_")){keep=null;return;}
            if(flow.Loading||party==null||party.Active==null||DialogueSystem.Current!=null&&DialogueSystem.Current.Running)return;
            foreach(var member in party.members)if(!member.Health.Alive||member.Recovering)return;
            if(BossController.AnyFightUnderway||EnemyBrain.AwakeNear(party.Active.transform.position,EnemyBrain.InteractionCalm))return;
            var state=keep;keep=null;services.Saves.Save("autosave",state);
        }
        void Talk(string node,string speaker,System.Action<bool> completed)
        {
            var dialogue=DialogueSystem.Current;
            if(dialogue==null||!dialogue.StartNode(node,speaker,completed))completed?.Invoke(false);
        }
        void Discover(DiscoveryPoint point)
        {
            var dialogue=DialogueSystem.Current;
            if(dialogue==null||!dialogue.StartNode(point.dialogueNode,point.dialogueSpeaker,completed=>{if(point!=null)point.CompleteDialogue(completed);}))
                point.CompleteDialogue(false);
        }
        void Bench()=>GetComponent<PauseMenu>().Open(PauseMenu.FabricatePage,true);
        void Shop(ShopInventory inventory)=>GetComponent<ShopUi>().Open(inventory);
        void Request(string action){if(action=="bench")Bench();else if(action=="shop")GetComponent<ShopUi>().Open(ZoneController.Current!=null?GameCatalog.Find<ShopInventory>(ZoneController.Current.definition.id):null,DialogueSystem.Current!=null?DialogueSystem.Current.Speaker:null);else if(action=="repair")RepairBay.Repair();}
        void Join(string id)
        {
            var party=PartyController.Current;float spacing=party.Active.flight?HeroCollision.ArrivalSpacing:2;
            var actor=ActorFactory.Hero(id,party.Active.transform.position+Vector3.right*spacing);actor.transform.SetParent(party.transform);
            var members=new System.Collections.Generic.List<CombatActor>(party.members){actor};party.members=members.ToArray();party.Apply();
        }
    }
}
