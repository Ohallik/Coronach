using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    public sealed class DockingPad:InteractionPrompt
    {
        public string scene="Hub_Decks",spawn="Arrival";
        public override void Interact(){AudioManager.Play("doorOpen_000");Debug.Log("DOCK_OK");SceneFlow.Current.LoadZone(scene,spawn);}
    }
}
