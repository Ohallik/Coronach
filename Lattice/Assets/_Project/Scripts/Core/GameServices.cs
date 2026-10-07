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
        }
        void Update(){PromptService.Poll();State.playtime+=Time.unscaledDeltaTime;}
        void OnDestroy(){if(Current==this){Input?.Dispose();Current=null;}}
        public void NewGame(){State=new GameState(); Debug.Log("NEW_GAME_OK");}
    }
}
