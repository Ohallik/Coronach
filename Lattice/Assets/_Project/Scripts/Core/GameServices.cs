using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lattice.Core
{
    public sealed class GameServices:MonoBehaviour
    {
        public static GameServices Current {get;private set;}
        public GameInput Input {get;private set;}
        public GameState State {get;set;}=new();
        public SaveSystem Saves {get;private set;}
        public FlagService Flags=>new(State.flags);
        void Awake()
        {
            if(Current!=null && Current!=this){Destroy(gameObject);return;}
            Current=this;GameTime.Reset();Input=new GameInput();
            gameObject.AddComponent<AudioMix>();
            gameObject.AddComponent<AudioManager>();
            gameObject.AddComponent<MusicDirector>();
            // Retain the original Windows save location across the Coronach product rename.
            var root=DevArgs.Value("-savepath")??Path.Combine(Path.GetDirectoryName(Application.persistentDataPath),"Lattice","Saves");
            Saves=new SaveSystem(root);
            // Synchronize moving scenes to the display. A software 60 fps cap
            // cannot prevent partial-frame scan-out. Headless tests have no
            // display clock and retain their explicit 60 Hz simulation pacing.
            QualitySettings.vSyncCount=1;Application.targetFrameRate=Application.isBatchMode?60:-1;
            // Explicit diagnostic only; ordinary routes retain startup policy.
            if(DevArgs.Has("-quality-route")&&int.TryParse(DevArgs.Value("-quality-vsync"),out int sync)&&sync>=0&&sync<=1)
            {QualitySettings.vSyncCount=sync;Application.targetFrameRate=sync==0?60:-1;}
            if(DevArgs.Has("-quality-route")&&int.TryParse(DevArgs.Value("-quality-queue"),out int queue)&&queue>=0&&queue<=3)
                QualitySettings.maxQueuedFrames=queue;
        }
        void Update(){PromptService.Poll();State.playtime+=Time.unscaledDeltaTime;}
        void OnDestroy(){if(Current==this){Input?.Dispose();Current=null;}}
        public void NewGame(){State=new GameState();GiveStartingGear(State,State.party[0]); Debug.Log("NEW_GAME_OK");}
        /// <summary>A save from before heroes started out armed holds no parts at all, and its party would reach the next fight empty-handed with nothing in Gear to put on. It gets
        /// what a new game gives. Only from the title's picker and a Retry back to that save, not in Repair: fixtures that save and reload a bare party keep their numbers, and a hero with parts left in Gear has only taken them off.</summary>
        public static void ArmAnEmptyParty(GameState state)
        {
            if(state.parts.Count>0)return;
            foreach(var member in state.party)GiveStartingGear(state,member);
        }
        /// <summary>A hero arrives wearing what their definition says, by a new game or by joining. Fixtures don't come through
        /// here (DevLoadout builds its own party), so their numbers stand.</summary>
        public static void GiveStartingGear(GameState state,MemberState member)
        {
            var character=GameCatalog.Find<Lattice.Data.CharacterDef>(member.id);if(character==null||character.startingGear==null)return;
            foreach(var part in character.startingGear)
            {
                if(part==null)continue;
                var item=new Lattice.Data.PartInstance{definitionId=part.id};state.parts.Add(item);member.equipped[part.slot]=item.instanceId;
            }
        }
    }
}
