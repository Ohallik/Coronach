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
        /// <summary>Offered only while this side quest is at this step (D199); empty for every other discovery.</summary>
        public string requiredQuest;public int requiredStep;
        /// <summary>The thing there is to take: shown only while it can be taken, gone once it has been (QA: a lamp "picked up"
        /// that stayed on the floor).</summary>
        public GameObject visual;
        /// <summary>A checkpoint: reaching this point moves the respawn here.</summary>
        public string saveSpawn;
        bool QuestReady=>string.IsNullOrEmpty(requiredQuest)||GameServices.Current.State.questSteps.TryGetValue(requiredQuest,out var step)&&step==requiredStep;
        bool pending;
        GameState requestedState;
        public override bool Available=>!pending&&isActiveAndEnabled&&!GameServices.Current.Flags.GetBool(flag)&&QuestReady&&
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
            if(!completed||!sameState||!isActiveAndEnabled)return;
            // A conversation that sets this point's own flag has still happened: it must save like any other discovery,
            // or a defeat after it replays everything since the last save.
            if(GameServices.Current.Flags.GetBool(flag)){Save();return;}
            if(Available)Discover();
        }
        void Save()
        {
            if(!string.IsNullOrEmpty(saveSpawn))GameServices.Current.State.spawn=saveSpawn;
            GameServices.Current.Saves.Save("autosave",GameServices.Current.State);
        }
        void Update()
        {
            if(visual==null||GameServices.Current==null)return;
            bool offered=QuestReady&&!GameServices.Current.Flags.GetBool(flag);
            if(visual.activeSelf!=offered)visual.SetActive(offered);
        }
        void Discover()
        {
            GameServices.Current.Flags.SetBool(flag,true);Debug.Log("DISCOVERY "+flag);
            if(!string.IsNullOrEmpty(report))Lattice.Rpg.RpgServices.Quests.Report(Lattice.Data.ObjectiveKind.Interact,report);
            if(PartyController.Current!=null)BarkService.Play(PartyController.Current.Active.character,situation,true);
            if(string.IsNullOrEmpty(dialogueNode))AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up,.16f,AudioBus.SFX,40);
            Save();
        }
        protected override void OnDisable(){base.OnDisable();pending=false;requestedState=null;}
    }
}
