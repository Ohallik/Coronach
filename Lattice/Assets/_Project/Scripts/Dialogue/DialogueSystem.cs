using System;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;
using Unity.Profiling;
namespace Lattice.Dialogue
{
    public sealed class DialogueSystem:MonoBehaviour
    {
        public static DialogueSystem Current{get;private set;}
        public static event Action<string> UiRequested;
        public static event Action<string> MemberJoined;
        public IDialogueView View;
        public bool Preparing{get;private set;}
        public bool Prepared{get;private set;}
        readonly IDialogueView preparationView=new PreparationView();
        internal IDialogueView PresenterView=>Preparing?preparationView:View;
        public DialogueRunner Runner{get;private set;}
        public bool AutoAdvance;
        public int LinesPresented{get;internal set;}
        public string Speaker{get;private set;}
        public bool Running=>Runner!=null&&Runner.IsDialogueRunning;
        string pendingUi;
        bool leaving,preparationComplete;
        readonly List<string> pendingJoins=new();
        static readonly ProfilerMarker LoadProjectMarker=new("Coronach.Dialogue.LoadProject");
        static readonly ProfilerMarker SetProjectMarker=new("Coronach.Dialogue.SetProject");
        static readonly ProfilerMarker StartNodeMarker=new("Coronach.Dialogue.StartNode");
        void Awake()
        {
            Current=this;Runner=gameObject.AddComponent<DialogueRunner>();
            var lines=gameObject.AddComponent<LinePresenter>();lines.system=this;
            var options=gameObject.AddComponent<OptionsPresenter>();options.system=this;
            Runner.DialoguePresenters=new List<DialoguePresenterBase>{lines,options};
            Runner.onDialogueStart??=new UnityEvent();Runner.onDialogueComplete??=new UnityEvent();
            Runner.onDialogueComplete.AddListener(()=>
            {
                if(leaving||GameServices.Current==null)return;
                if(Preparing){preparationComplete=true;return;}
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
            Prepare().Forget();
        }
        async YarnTask Prepare()
        {
            Preparing=true;
            var cancellation=destroyCancellationToken;
            using var hold=SceneFlow.Current?.HoldPreparation();
            try
            {
                YarnProject project;
                using(LoadProjectMarker.Auto())project=Resources.Load<YarnProject>("Dialogue/Lattice");
                if(project==null)throw new InvalidOperationException("Yarn project missing");
                using(SetProjectMarker.Auto())Runner.SetProject(project);
                // This dedicated node has no commands, rewards or narrative state.
                // Exercise Yarn's lazy VM/async line/options setup under the load fade.
                await Runner.StartDialogue("__RuntimeReady");
                while(!preparationComplete&&!cancellation.IsCancellationRequested)await YarnTask.Yield();
                if(cancellation.IsCancellationRequested)return;
                // This is a new runner, before any story node. Discard only its
                // preparation visit history; saved game flags live in GameState.
                Runner.VariableStorage.Clear();
                Prepared=true;
                Debug.Log("DIALOGUE_PREPARED");
            }
            finally {Preparing=false;}
        }
        public void StartNode(string node,string speaker)
        {
            if(Preparing||Running||!Prepared)return;
            Speaker=speaker;GameServices.Current.Input.Blocked=true;
            using(StartNodeMarker.Auto())Runner.StartDialogue(node).Forget();
        }
        sealed class PreparationView:IDialogueView
        {
            public bool AdvanceRequested=>true;
            public void Show(string speaker,PortraitEmotion emotion,Sprite portrait,string body){}
            public void SetVisibleCharacters(int count){}
            public void Hide(){}
            public void Options(IReadOnlyList<string> labels,Action<int> selected)=>selected(0);
            public void ClearOptions(){}
        }
        void OnEnable(){leaving=false;}
        void OnDisable(){leaving=true;}
        void OnDestroy(){if(Current==this)Current=null;}
    }
}
