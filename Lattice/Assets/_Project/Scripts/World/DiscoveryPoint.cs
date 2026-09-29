using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>A place the party understands by standing in it: the space
    /// carries the discovery, the flag only remembers it.</summary>
    public sealed class DiscoveryPoint:InteractionPrompt
    {
        public string flag="hushwell.nursery",situation="discovery";
        public override bool Available=>!GameServices.Current.Flags.GetBool(flag);
        public override void Interact()
        {
            if(!Available)return;GameServices.Current.Flags.SetBool(flag,true);Debug.Log("DISCOVERY "+flag);
            if(PartyController.Current!=null)BarkService.Play(PartyController.Current.Active.character,situation,true);
            GameServices.Current.Saves.Save("autosave",GameServices.Current.State);
        }
    }
}
