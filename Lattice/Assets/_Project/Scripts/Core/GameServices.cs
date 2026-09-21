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
            gameObject.AddComponent<AudioManager>();
            var root=DevArgs.Value("-savepath")??Path.Combine(Application.persistentDataPath,"Saves");
            Saves=new SaveSystem(root);
            Application.targetFrameRate=60;
            AudioListener.volume=PlayerPrefs.GetFloat("volume",1);
        }
        void Update(){PromptService.Poll();State.playtime+=Time.unscaledDeltaTime;}
        void OnDestroy(){if(Current==this){Input?.Dispose();Current=null;}}
        public void NewGame(){State=new GameState(); Debug.Log("NEW_GAME_OK");}
    }
}
