using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A cut a story makes once a talk has said it (chapter 10, the Returning Passage, docs/maps/ReturningPassage.md). Once <see cref="flag"/> is set and the talk that set it has let
    /// go of the controls, the party is held (<see cref="GameInput.Blocked"/>) for <see cref="hold"/> seconds while the world goes on; then, if it names one, the pair out becomes
    /// <see cref="pair"/> (a fixture's way, <see cref="Deployment.Set"/>; a lead who is one of them keeps the lead, otherwise <see cref="lead"/> leads), <see cref="doneFlag"/> is set,
    /// <see cref="plate"/> goes up on black (<see cref="TimePlate"/>) and <see cref="scene"/> loads at <see cref="spawn"/>.
    /// <para>The turn: Sela turns back for the tug and Taren goes with her, whoever was out (D286), and the zone reloads at the Back section's checkpoint. The epilogue: SEVEN WEEKS ON,
    /// and Neighbourhood Afloat. A zone opened with the flag set and the cut not made (a save between the talk and the load) makes it at once.</para>
    /// </summary>
    public sealed class CutAway:MonoBehaviour
    {
        public string flag,doneFlag="";
        public float hold=2;
        public string[] pair=System.Array.Empty<string>();public string lead="";
        public string plate="";public float plateSeconds=3.5f;
        public string scene,spawn="Arrival";
        float at=-1;bool going;
        void Update()
        {
            var services=GameServices.Current;if(services==null||going||string.IsNullOrEmpty(flag)||string.IsNullOrEmpty(scene))return;
            if(!services.Flags.GetBool(flag)||!string.IsNullOrEmpty(doneFlag)&&services.Flags.GetBool(doneFlag)){at=-1;return;}
            if(SceneFlow.Current!=null&&SceneFlow.Current.Loading)return;
            if(at<0)
            {
                // The talk that set the flag still holds the controls: wait for it to let go, then hold the party ourselves.
                if(services.Input.Blocked)return;
                at=Time.unscaledTime+hold;services.Input.Blocked=true;Debug.Log("CUT_AWAY_HOLD "+name);
                return;
            }
            services.Input.Blocked=true;
            if(Time.unscaledTime<at)return;
            going=true;
            var state=services.State;
            if(pair!=null&&pair.Length>0)
            {
                string was=state.activeMember>=0&&state.activeMember<state.party.Count?state.party[state.activeMember]?.id:null;
                var before=Deployment.Of(state);
                if(Deployment.Set(state,new List<string>(pair)))
                {
                    string leads=System.Array.IndexOf(pair,was)>=0?was:lead;
                    int place=Deployment.PartyIndex(state,leads);if(place>=0)state.activeMember=place;
                    Deployment.Settle(state);
                    // A hero the story brings back out after chapters away comes up to the floor of the chapter she comes back in (PartyFloor, D329): Sela turned back in
                    // the Tempered Emitter she had rested in since chapter 6, under chapter 10's Choirwrought.
                    foreach(var member in Deployment.Of(state))if(!before.Contains(member))Lattice.Rpg.PartyFloor.BringUp(state,member,Lattice.Rpg.PartyFloor.Here());
                }
                Debug.Log("CUT_AWAY_PAIR "+string.Join(",",Deployment.Of(state).ConvertAll(m=>m.id))+" lead="+state.party[state.activeMember].id);
            }
            if(!string.IsNullOrEmpty(doneFlag))services.Flags.SetBool(doneFlag,true);
            if(!string.IsNullOrEmpty(plate))TimePlate.Show(plate,plateSeconds);
            Debug.Log("CUT_AWAY "+name+" -> "+scene+"/"+spawn);
            if(SceneFlow.Current!=null)SceneFlow.Current.LoadZone(scene,spawn);
        }
    }
}
