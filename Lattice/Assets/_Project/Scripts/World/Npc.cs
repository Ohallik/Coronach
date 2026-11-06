using System;
using Lattice.Core;
namespace Lattice.World
{
    public sealed class Npc:InteractionPrompt
    {
        public static event Action<string,string,Action<bool>> TalkRequested;
        /// <summary>A conversation nobody is standing there for: a recorded message left in a crate.</summary>
        public static bool Say(string node,string speaker,Action<bool> done=null)
        {
            if(TalkRequested==null)return false;
            TalkRequested.Invoke(node,speaker,done??(_=>{}));return true;
        }
        public string speaker,firstNode,repeatNode,postNode,postFlag="warpkey";
        public string finalNode,finalFlag;
        // Optional completion-owned acknowledgement after the final tier.
        // Existing met/post/final behavior remains for NPCs without these fields.
        public string finalCompletionFlag,finalRepeatNode;
        public string hideWhenJoined;
        /// <summary>A scene that is still playing out (the first morning, D197) can lend a speaker a short holding line. While
        /// set, talking plays only this node: the speaker is not marked met and no completion flag is touched, so the
        /// conversation the scene is protecting still happens later. A speaker who simply could not be talked to read as
        /// a broken talk button.</summary>
        [System.NonSerialized] public string holdNode;
        bool pending;int request;
        public override bool Available=>!pending&&isActiveAndEnabled;
        void Update(){if(!string.IsNullOrEmpty(hideWhenJoined)&&GameServices.Current.State.party.Exists(m=>m.id==hideWhenJoined))gameObject.SetActive(false);}
        public override void Interact()
        {
            if(!Available)return;
            if(!string.IsNullOrEmpty(holdNode))
            {
                if(TalkRequested==null)return;
                pending=true;int held=++request;
                TalkRequested.Invoke(holdNode,speaker,_=>{if(this!=null&&held==request)pending=false;});
                return;
            }
            var service=GameServices.Current;var flags=service.Flags;var state=service.State;
            // A side story at its point for this speaker (D199) plays instead of the ordinary tier.
            string story=SideStories.NodeFor(speaker,state);
            if(story!=null)
            {
                if(TalkRequested==null)return;
                pending=true;int asked=++request;
                TalkRequested.Invoke(story,speaker,_=>{if(this!=null&&asked==request)pending=false;});
                return;
            }
            // A first meeting comes first, whatever the story has moved on to: a later flag used to skip the introduction,
            // and with it what the introduction gives (Hal's lesson, Neve's cache, Ilo explaining the Compact).
            bool firstMeeting=!flags.GetBool("met."+speaker)&&!string.IsNullOrEmpty(firstNode);
            bool final=!firstMeeting&&!string.IsNullOrEmpty(finalFlag)&&!string.IsNullOrEmpty(finalNode)&&flags.GetBool(finalFlag);
            string node=firstMeeting?firstNode:final?finalNode:flags.GetBool(postFlag)&&!string.IsNullOrEmpty(postNode)?postNode:
                !string.IsNullOrEmpty(repeatNode)?repeatNode:firstNode;
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
