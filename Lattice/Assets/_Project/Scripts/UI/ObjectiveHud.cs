using System.Linq;
using Lattice.Core;
using Lattice.Combat;
using Lattice.World;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    public sealed class ObjectiveHud:MonoBehaviour
    {
        TMP_Text text;float next;Canvas canvas;
        void Start()
        {
            canvas=UiKit.CreateCanvas("Objective",6,transform);var frame=UiKit.DarkFrame(canvas.transform,"ObjectiveFrame");frame.raycastTarget=false;
            UiKit.Rect(frame.gameObject,new(1,1),new(1,1),new(-300,-175),new(570,165));
            text=UiKit.Text(frame.transform,"Objective","",24,UiKit.TextColor,TextAlignmentOptions.MidlineLeft);
            UiKit.Rect(text.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(480,95));
        }
        void Update()
        {
            canvas.enabled=!GameInput.Current.Blocked;
            if(Time.unscaledTime<next||PartyController.Current==null)return;next=Time.unscaledTime+.25f;
            var state=GameServices.Current.State;var flags=GameServices.Current.Flags;Vector3? goal=null;string label="";
            var prompts=InteractionPrompt.Active;
            InteractionPrompt target=null;
            switch(state.zone)
            {
                case "Hub_CinderHalo":
                    if(!flags.GetBool("met.Orrin")){label="Dock at Orrin's office";target=prompts.OfType<DockingPad>().FirstOrDefault(p=>p.spawn=="Office");}
                    else if(ChapterProgress.CanEnterGullet(state)){label="Enter the Gullet";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="Gullet_Tunnel");}
                    else{label=flags.GetBool("warpkey")?"Explore Hushwell beneath Sorrel":"Land on Sorrel";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="Sorrel_Ridges");}
                    break;
                case "Hub_Decks":
                    if(!flags.GetBool("met.Orrin")){label="Talk to Orrin";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Orrin");}
                    else{label="Return to the Halo";target=prompts.OfType<DockingPad>().OrderBy(p=>(p.transform.position-PartyController.Current.Active.transform.position).sqrMagnitude).FirstOrDefault();}
                    break;
                case "Sorrel_Ridges":
                    if(!flags.GetBool("met.Survivor")){label="Reach the outpost survivor";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Survivor");}
                    else if(!flags.GetBool("bossdown.Burrower")){label="Follow the ridges to the drill";goal=new Vector3(0,0,164);}
                    else if(!flags.GetBool("warpkey")){label="Recover the warp key";target=prompts.OfType<KeyPickup>().FirstOrDefault();}
                    else if(ChapterProgress.NeedsNursery(state)){label="Descend the bore into Hushwell";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.scene=="Hushwell");}
                    else{label="Return to the Halo";target=prompts.OfType<DockingPad>().FirstOrDefault(p=>p.spawn=="Outer");}
                    break;
                case "Hushwell":
                    if(!flags.GetBool("bossdown.BellowsBelow")){label="Follow the breathing down to the Bellows";goal=new Vector3(HushwellLayout.Bellows.x,HushwellLayout.Middle,HushwellLayout.Bellows.y);}
                    else if(!flags.GetBool("hushwell.nursery")){label="Find what the Bellows was guarding";target=prompts.OfType<DiscoveryPoint>().FirstOrDefault();}
                    else{label="Ride the drill-shaft lift back to Sorrel";target=prompts.OfType<WarpBeacon>().FirstOrDefault(p=>p.requiredFlag=="hushwell.nursery");}
                    break;
                case "Gullet_Tunnel":
                    if(!flags.GetBool("bossdown.Cantor"))
                    {
                        label="Find the collared Cantor";goal=new Vector3(Lattice.Data.GulletProfile.Center(Lattice.Data.GulletProfile.CantorZ),1,Lattice.Data.GulletProfile.CantorZ);
                        foreach(var collar in CantorCollar.All)if(!collar.Resolved)
                        {label=collar.Remaining>0?$"Cut the collar links ({3-collar.Remaining}/3)":"Release the exposed collar lock";goal=collar.transform.position;break;}
                    }
                    else{label="Warp to Tallow Drift";target=prompts.OfType<WarpBeacon>().FirstOrDefault();}
                    break;
                case "TallowApproach":label="Dock at Tallow Drift";target=prompts.OfType<DockingPad>().FirstOrDefault();break;
                case "TallowDrift":
                    if(flags.GetBool("sliceComplete"))label="LINK SECURE · SLICE COMPLETE";
                    else if(!flags.GetBool("met.Keeper")){label="Talk to the dock keeper";target=prompts.OfType<Npc>().FirstOrDefault(p=>p.speaker=="Keeper");}
                    else{label="Repair and save at Tallow Drift";target=prompts.OfType<RepairBay>().FirstOrDefault();}
                    break;
                default:label="Practice attacks, dodges and swaps";break;
            }
            if(target!=null)goal=target.transform.position;
            string direction="";
            if(goal.HasValue)
            {
                var delta=goal.Value-PartyController.Current.Active.transform.position;delta.y=0;
                var local=Quaternion.Euler(0,-ZoneController.Current.definition.cameraProfile.yaw,0)*delta;
                direction=$"\n{delta.magnitude:0} m · "+(Mathf.Abs(local.x)>Mathf.Abs(local.z)?local.x>0?"right":"left":local.z>0?"ahead":"behind");
            }
            text.text=label+direction;
        }
    }
}
