using Lattice.Core;
using Lattice.Combat;
using Lattice.Data;
using UnityEngine;
namespace Lattice.World
{
    public sealed class DockingPad:InteractionPrompt
    {
        public string scene="Hub_Decks",spawn="Arrival";
        public override void Interact()
        {
            var flow=SceneFlow.Current;if(flow==null||flow.Loading)return;
            var party=PartyController.Current;var members=party!=null?party.members:System.Array.Empty<CombatActor>();
            var zone=GameCatalog.Find<ZoneDef>(scene);bool toFlight=zone!=null&&(zone.kind==ZoneKind.SpaceCombat||zone.kind==ZoneKind.SpaceSafe);
            foreach(var member in members)
            {
                if(member==null)continue;
                member.GetComponent<GroundMotor>().Halt();member.GetComponent<FlightMotor>().Halt();
                member.GetComponent<FormController>().BeginDeparture(toFlight);
            }
            AudioManager.Play("doorOpen_000");Debug.Log("DOCK_OK");
            flow.LoadDockedZone(scene,spawn,()=>
            {
                foreach(var member in members)if(member!=null&&member.Health.Alive&&!member.GetComponent<FormController>().DepartureReady)return false;
                return true;
            });
        }
    }
}
