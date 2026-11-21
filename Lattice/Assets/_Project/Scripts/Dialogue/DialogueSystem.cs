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
        /// <summary>A conversation ran to its end and changed what a quit would lose: a quest finished in it, or a chapter's flag went up. Whoever can see the fight writes the save.</summary>
        public static event Action Settled;
        public IDialogueView View;
        public bool Preparing{get;private set;}
        public bool Prepared{get;private set;}
        readonly IDialogueView preparationView=new PreparationView();
        internal IDialogueView PresenterView=>leaving||View is UnityEngine.Object viewObject&&viewObject==null?null:Preparing?preparationView:View;
        public DialogueRunner Runner{get;private set;}
        public bool AutoAdvance;
        internal bool Interrupted;
        public int LinesPresented{get;internal set;}
        public string Speaker{get;private set;}
        /// <summary>The node the running conversation started at, and <see cref="LinesPresented"/> as it started: what a scene staged line by line reads (LineCues: the meeting's turning heads, Hal's bench).</summary>
        public string Node{get;private set;}
        public int NodeStartLines{get;private set;}
        public bool Running=>dialogueActive||following||Runner!=null&&Runner.IsDialogueRunning;
        /// <summary>A conversation asked for straight after the one running (<see cref="Follow"/>) is waiting for it to end, or to let go of the runner: the box stays up and the party held.</summary>
        public bool FollowsOn=>next.Count>0&&!Interrupted;
        readonly Queue<(string node,string speaker,Action<bool> completed)> next=new();
        bool following;
        string pendingUi;
        Action<bool> pendingCompletion;
        bool leaving,preparationComplete,dialogueActive,chapterFlag;
        GameState startedOn;int questsAtStart;
        static int QuestsDone(GameState state)
        {
            int done=0;
            foreach(var flag in state.flags)if(flag.Value&&flag.Key.StartsWith("quest.",StringComparison.Ordinal)&&flag.Key.EndsWith(".complete",StringComparison.Ordinal))done++;
            return done;
        }
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
                bool completed=false;
                try
                {
                if(leaving||GameServices.Current==null)return;
                if(Preparing){preparationComplete=true;return;}
                // Followed (the Coronach's extras after a door's line): the box stays up and the party held until the next one's first line takes it over (Update).
                if(FollowsOn)following=true;else{View?.Hide();GameServices.Current.Input.Blocked=false;}
                if(Interrupted){pendingJoins.Clear();pendingUi=null;DropFollowing();Debug.Log("DIALOGUE_CANCELLED "+Speaker);return;}
                Debug.Log("DIALOGUE_OK "+Speaker);RpgServices.Quests.Report(ObjectiveKind.TalkTo,Speaker);
                foreach(string id in pendingJoins)
                {
                    var state=GameServices.Current.State;if(state.party.Exists(m=>m.id==id))continue;
                    int level=state.party[0].level;var joined=new MemberState{id=id,level=level,integrity=Levels.MaxIntegrity(level)};state.party.Add(joined);GameServices.GiveStartingGear(state,joined);
                    // She joins in the floor of the chapter she joins in (PartyFloor, D329): Idra came with nothing in her Emitter's or Module's slot before the Host Marshal.
                    PartyFloor.BringUp(state,joined,PartyFloor.Here());
                    MemberJoined?.Invoke(id);Debug.Log("PARTY_JOIN "+id);
                }
                pendingJoins.Clear();
                if(pendingUi!=null){string action=pendingUi;pendingUi=null;UiRequested?.Invoke(action);}
                completed=true;
                }
                finally {try{FinishPending(completed);}finally{dialogueActive=false;}}
                // Last, with everything it set off done. Only a conversation that ran to its end comes this far.
                if(ReferenceEquals(startedOn,GameServices.Current.State)&&(chapterFlag||QuestsDone(startedOn)>questsAtStart))Settled?.Invoke();
            });
            Runner.AddCommandHandler("setFlag",(Action<string>)(key=>{GameServices.Current.Flags.SetBool(key,true);RpgServices.Quests.Report(Lattice.Data.ObjectiveKind.Flag,key);if(key.StartsWith("chapter",StringComparison.Ordinal))chapterFlag=true;}));
            Runner.AddCommandHandler("giveItem",(Action<string,int>)((id,count)=>RpgServices.Inventory.Grant(id,count)));
            Runner.AddCommandHandler("startQuest",(Action<string>)(id=>RpgServices.Quests.Start(id)));
            Runner.AddCommandHandler("shop",(Action)(()=>pendingUi="shop"));
            Runner.AddCommandHandler("repair",(Action)(()=>pendingUi="repair"));
            Runner.AddCommandHandler("bench",(Action)(()=>pendingUi="bench"));
            // The station shifts a few centimetres under the opening scene: a short deck tremor and a hull creak (D195).
            Runner.AddCommandHandler("stationShift",(Action)(()=>
            {
                var rig=UnityEngine.Object.FindFirstObjectByType<CameraRig>();if(rig!=null)rig.Tremor(.9f,.07f);
                AudioManager.Play("impactMetal_000",.16f);Debug.Log("STATION_SHIFT");
            }));
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
        public bool StartNode(string node,string speaker,Action<bool> completed=null)
        {
            if(Preparing||Running||!Prepared)return false;
            return Begin(node,speaker,completed);
        }
        /// <summary>Said straight after the conversation running now, in the same box, which stays up between them with the party still held: one stop, not two (chapter 9's Coronach, where
        /// a door's own line and the side stories' after it closed the box for three frames between). Each is asked in turn once the one before has ended to its last line; one cut
        /// short takes everything after it with it. With nothing running, refused: a follow-on never starts a conversation of its own.</summary>
        public bool Follow(string node,string speaker,Action<bool> completed=null)
        {
            if(Preparing||!Prepared||!Running||Interrupted||!Known(node))return false;
            next.Enqueue((node,speaker,completed));return true;
        }
        bool Known(string node)=>!string.IsNullOrEmpty(node)&&Runner!=null&&Runner.YarnProject!=null&&Runner.YarnProject.Program.Nodes.ContainsKey(node);
        void Update()
        {
            // The one before has let go of the runner: the next takes the box over without it closing.
            if(!following||dialogueActive||Runner!=null&&Runner.IsDialogueRunning)return;
            following=false;
            while(next.Count>0){var (node,speaker,completed)=next.Dequeue();if(Begin(node,speaker,completed))return;completed?.Invoke(false);}
            View?.Hide();if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
        }
        void DropFollowing()
        {
            following=false;
            while(next.Count>0){var (_,_,completed)=next.Dequeue();completed?.Invoke(false);}
        }
        bool Begin(string node,string speaker,Action<bool> completed)
        {
            // Reject bad content before taking input or completion ownership.
            if(!Known(node))return false;
            // Yarn's VM stops before its presenters and completion commands drain.
            // Keep the conversation owned until those commands finish, so callers
            // cannot start a second node against the first node's cancellation state.
            Speaker=speaker;Node=node;NodeStartLines=LinesPresented;Interrupted=false;pendingCompletion=completed;dialogueActive=true;GameServices.Current.Input.Blocked=true;
            startedOn=GameServices.Current.State;questsAtStart=QuestsDone(startedOn);chapterFlag=false;
            using(StartNodeMarker.Auto())Runner.StartDialogue(node).Forget();
            return true;
        }
        /// <summary>Ends a running conversation without its completion taking effect.</summary>
        public void Cancel()
        {
            if(!Running||Preparing)return;
            // Between a conversation and the one it is followed by, nothing is running but the hold: let it go.
            if(following&&!dialogueActive&&(Runner==null||!Runner.IsDialogueRunning)){DropFollowing();View?.Hide();GameServices.Current.Input.Blocked=false;return;}
            Interrupted=true;if(Runner!=null&&Runner.IsDialogueRunning)Runner.Stop().Forget();
        }
        void FinishPending(bool completed)
        {var callback=pendingCompletion;pendingCompletion=null;callback?.Invoke(completed);}
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
        void OnDisable(){leaving=true;FinishPending(false);DropFollowing();}
        void OnDestroy(){if(Current==this)Current=null;}
    }
}
