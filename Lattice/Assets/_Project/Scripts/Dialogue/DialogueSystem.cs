using System;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;
namespace Lattice.Dialogue
{
    public sealed class DialogueSystem:MonoBehaviour
    {
        public static DialogueSystem Current{get;private set;}
        public static event Action<string> UiRequested;
        public static event Action<string> MemberJoined;
        public IDialogueView View;
        public DialogueRunner Runner{get;private set;}
        public bool AutoAdvance;
        public int LinesPresented{get;internal set;}
        public string Speaker{get;private set;}
        public bool Running=>Runner!=null&&Runner.IsDialogueRunning;
        string pendingUi;
        readonly List<string> pendingJoins=new();
        void Awake()
        {
            Current=this;Runner=gameObject.AddComponent<DialogueRunner>();
            var lines=gameObject.AddComponent<LinePresenter>();lines.system=this;
            var options=gameObject.AddComponent<OptionsPresenter>();options.system=this;
            Runner.DialoguePresenters=new List<DialoguePresenterBase>{lines,options};
            Runner.onDialogueStart??=new UnityEvent();Runner.onDialogueComplete??=new UnityEvent();
            Runner.onDialogueComplete.AddListener(()=>
            {
                View?.Hide();GameServices.Current.Input.Blocked=false;
                Debug.Log("DIALOGUE_OK "+Speaker);RpgServices.Quests.Report(ObjectiveKind.TalkTo,Speaker);
                foreach(string id in pendingJoins)
                {
                    var state=GameServices.Current.State;if(state.party.Exists(m=>m.id==id))continue;
                    int level=state.party[0].level;state.party.Add(new MemberState{id=id,level=level,integrity=Levels.MaxIntegrity(level)});MemberJoined?.Invoke(id);Debug.Log("PARTY_JOIN "+id);
                }
                pendingJoins.Clear();
                if(pendingUi!=null){string action=pendingUi;pendingUi=null;UiRequested?.Invoke(action);}
            });
            Runner.AddCommandHandler("setFlag",(Action<string>)(key=>GameServices.Current.Flags.SetBool(key,true)));
            Runner.AddCommandHandler("giveItem",(Action<string,int>)((id,count)=>RpgServices.Inventory.Give(id,count)));
            Runner.AddCommandHandler("startQuest",(Action<string>)(id=>RpgServices.Quests.Start(id)));
            Runner.AddCommandHandler("shop",(Action)(()=>pendingUi="shop"));
            Runner.AddCommandHandler("repair",(Action)(()=>pendingUi="repair"));
            Runner.AddCommandHandler("bench",(Action)(()=>pendingUi="bench"));
            Runner.AddCommandHandler("warpUnlock",(Action)(()=>{GameServices.Current.Flags.SetBool("warpkey",true);Debug.Log("QUEST_STEP warpkey");}));
            Runner.AddCommandHandler("joinParty",(Action<string>)(id=>
            {
                if(!pendingJoins.Contains(id))pendingJoins.Add(id);
            }));
        }
        public void StartNode(string node,string speaker)
        {
            if(Running)return;var project=Resources.Load<YarnProject>("Dialogue/Lattice");
            if(project==null){Debug.LogError("FAILED: Yarn project missing");return;}
            Runner.SetProject(project);Speaker=speaker;GameServices.Current.Input.Blocked=true;
            Runner.StartDialogue(node).Forget();
        }
        void OnDestroy(){if(Current==this)Current=null;}
    }
}
