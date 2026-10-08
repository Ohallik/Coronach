using System;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>A discovery belongs to its place and completed conversation.
    /// Interrupted viewing must not grant its route, quest reward or save.</summary>
    public sealed class DiscoveryPoint:InteractionPrompt
    {
        public static event Action<DiscoveryPoint> DialogueRequested;
        public string flag="hushwell.nursery",situation="discovery",report="HushwellNursery";
        public string dialogueNode="SelaNursery",dialogueSpeaker="Sela";
        public EncounterVolume previewEncounter;
        bool pending;
        GameState requestedState;
        public override bool Available=>!pending&&!GameServices.Current.Flags.GetBool(flag)&&
            (previewEncounter==null||!previewEncounter.Started);
        public override void Interact()
        {
            if(!Available)return;
            if(!string.IsNullOrEmpty(dialogueNode))
            {
                if(DialogueRequested==null)return;
                requestedState=GameServices.Current.State;pending=true;
                DialogueRequested.Invoke(this);
                return;
            }
            Discover();
        }
        public void CompleteDialogue(bool completed)
        {
            if(!pending)return;
            bool sameState=GameServices.Current!=null&&ReferenceEquals(requestedState,GameServices.Current.State);
            pending=false;requestedState=null;
            if(completed&&sameState&&isActiveAndEnabled&&Available)Discover();
        }
        void Discover()
        {
            GameServices.Current.Flags.SetBool(flag,true);Debug.Log("DISCOVERY "+flag);
            if(!string.IsNullOrEmpty(report))Lattice.Rpg.RpgServices.Quests.Report(Lattice.Data.ObjectiveKind.Interact,report);
            if(PartyController.Current!=null)BarkService.Play(PartyController.Current.Active.character,situation,true);
            GameServices.Current.Saves.Save("autosave",GameServices.Current.State);
        }
        protected override void OnDisable(){base.OnDisable();pending=false;requestedState=null;}
    }
}
