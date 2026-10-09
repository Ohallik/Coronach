using System;
using Lattice.Core;
namespace Lattice.World
{
    public sealed class Npc:InteractionPrompt
    {
        public static event Action<string,string,Action<bool>> TalkRequested;
        public string speaker,firstNode,repeatNode,postNode,postFlag="warpkey";
        public string finalNode,finalFlag;
        // Optional completion-owned acknowledgement after the final tier.
        // Existing met/post/final behavior remains for NPCs without these fields.
        public string finalCompletionFlag,finalRepeatNode;
        public string hideWhenJoined;
        bool pending;int request;
        public override bool Available=>!pending;
        void Update(){if(!string.IsNullOrEmpty(hideWhenJoined)&&GameServices.Current.State.party.Exists(m=>m.id==hideWhenJoined))gameObject.SetActive(false);}
        public override void Interact()
        {
            if(!Available)return;
            var service=GameServices.Current;var flags=service.Flags;var state=service.State;
            bool final=!string.IsNullOrEmpty(finalFlag)&&!string.IsNullOrEmpty(finalNode)&&flags.GetBool(finalFlag);
            string node=final?finalNode:flags.GetBool(postFlag)&&!string.IsNullOrEmpty(postNode)?postNode:
                flags.GetBool("met."+speaker)&&!string.IsNullOrEmpty(repeatNode)?repeatNode:firstNode;
            string completionFlag=final?finalCompletionFlag:null;
            if(final&&!string.IsNullOrEmpty(finalCompletionFlag)&&flags.GetBool(finalCompletionFlag)&&!string.IsNullOrEmpty(finalRepeatNode))
            {node=finalRepeatNode;completionFlag=null;}
            flags.SetBool("met."+speaker,true);if(TalkRequested==null)return;
            pending=true;int current=++request;
            TalkRequested.Invoke(node,speaker,completed=>
            {
                if(this==null||!pending||current!=request)return;
                pending=false;
                if(!completed||!isActiveAndEnabled||GameServices.Current==null||!ReferenceEquals(state,GameServices.Current.State)||string.IsNullOrEmpty(completionFlag))return;
                if(GameServices.Current.Flags.GetBool(completionFlag))return;
                GameServices.Current.Flags.SetBool(completionFlag,true);
                GameServices.Current.Saves.Save("autosave",state);
            });
        }
        protected override void OnDisable(){base.OnDisable();pending=false;request++;}
    }
}
