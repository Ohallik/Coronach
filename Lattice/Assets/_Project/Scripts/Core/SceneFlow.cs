using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Lattice.Core
{
    public sealed class SceneFlow:MonoBehaviour
    {
        public static SceneFlow Current {get;private set;}
        public bool Loading {get;private set;}
        public string Zone {get;private set;}
        public event System.Action<float> FadeChanged;
        void Awake(){Current=this;}
        public void LoadZone(string scene,string spawn="Arrival")
        {
            if(!Loading) StartCoroutine(Load(scene,spawn));
        }
        IEnumerator Load(string scene,string spawn)
        {
            Loading=true;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=true;
            for(float t=0;t<0.3f;t+=Time.unscaledDeltaTime){FadeChanged?.Invoke(t/0.3f);yield return null;}
            FadeChanged?.Invoke(1);
            if(GameServices.Current!=null){GameServices.Current.State.zone=scene;GameServices.Current.State.spawn=spawn;}
            if(!string.IsNullOrEmpty(Zone)&&SceneManager.GetSceneByName(Zone).isLoaded)
                yield return SceneManager.UnloadSceneAsync(Zone);
            if(scene!="Title"&&SceneManager.GetSceneByName("Title").isLoaded)yield return SceneManager.UnloadSceneAsync("Title");
            yield return SceneManager.LoadSceneAsync(scene,LoadSceneMode.Additive);
            Zone=scene; SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
            if(GameServices.Current!=null){GameServices.Current.State.zone=scene;GameServices.Current.State.spawn=spawn;}
            Debug.Log("ZONE_ENTER "+scene);
            for(float t=0;t<0.3f;t+=Time.unscaledDeltaTime){FadeChanged?.Invoke(1-t/0.3f);yield return null;}
            FadeChanged?.Invoke(0); Loading=false;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            if(GameServices.Current!=null&&scene!="Title")BarkService.Play(GameServices.Current.State.party[GameServices.Current.State.activeMember].id,"zone",true);
            if(GameServices.Current!=null&&scene!="Title"&&!scene.StartsWith("Arena_"))GameServices.Current.Saves.Save("autosave",GameServices.Current.State);
        }
    }
}
