using System;
using Lattice.Core;
namespace Lattice.World
{
    public sealed class Npc:InteractionPrompt
    {
        public static event Action<string,string> TalkRequested;
        public string speaker,firstNode,repeatNode,postNode,postFlag="warpkey";
        public string hideWhenJoined;
        void Update(){if(!string.IsNullOrEmpty(hideWhenJoined)&&GameServices.Current.State.party.Exists(m=>m.id==hideWhenJoined))gameObject.SetActive(false);}
        public override void Interact()
        {
            var flags=GameServices.Current.Flags;string node=flags.GetBool(postFlag)&&!string.IsNullOrEmpty(postNode)?postNode:flags.GetBool("met."+speaker)&&!string.IsNullOrEmpty(repeatNode)?repeatNode:firstNode;
            flags.SetBool("met."+speaker,true);TalkRequested?.Invoke(node,speaker);
        }
    }
}
