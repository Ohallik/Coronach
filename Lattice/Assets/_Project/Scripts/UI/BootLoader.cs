using System.Collections;
using Lattice.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lattice.UI
{
    public sealed class BootLoader:MonoBehaviour
    {
        IEnumerator Start()
        {
#if LATTICE_DEV || UNITY_EDITOR
            DevLoadout.Apply(DevArgs.Value("-loadout"));
            if(DevArgs.Value("-route")=="slice")gameObject.AddComponent<SliceSmoke>();
            if(DevArgs.Has("-perf"))gameObject.AddComponent<PerformanceProbe>();
            if(DevArgs.Has("-uismoke"))gameObject.AddComponent<UiSmoke>();
            if(DevArgs.Has("-portraitsmoke"))gameObject.AddComponent<PortraitSmoke>();
#endif
            var zone=DevArgs.Value("-scene");
            if(string.IsNullOrEmpty(zone)||zone=="Title") yield return SceneManager.LoadSceneAsync("Title",LoadSceneMode.Additive);
            else SceneFlow.Current.LoadZone(zone);
        }
    }
}
