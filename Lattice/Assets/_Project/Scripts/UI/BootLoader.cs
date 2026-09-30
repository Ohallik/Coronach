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
            QualityRoute quality = null;
            if (DevArgs.Has("-quality-route"))
            {
                quality = ContinuousReview.ReadRoute();
                if (quality.starterParty) DevLoadout.Apply("starter");
                if (!string.IsNullOrEmpty(quality.loadout)) DevLoadout.Apply(quality.loadout);
                gameObject.AddComponent<ContinuousReview>();
            }
#if LATTICE_DEV || UNITY_EDITOR
            if(DevArgs.Has("-review"))gameObject.AddComponent<ReviewInput>();
            DevLoadout.Apply(DevArgs.Value("-loadout"));
            if(DevArgs.Value("-route")=="slice")gameObject.AddComponent<SliceSmoke>();
            if(DevArgs.Has("-perf"))gameObject.AddComponent<PerformanceProbe>();
            if(DevArgs.Has("-audio-mix-probe"))gameObject.AddComponent<AudioMixProbe>();
            if(DevArgs.Has("-uismoke"))gameObject.AddComponent<UiSmoke>();
            if(DevArgs.Has("-portraitsmoke"))gameObject.AddComponent<PortraitSmoke>();
            if(DevArgs.Has("-balance"))gameObject.AddComponent<BalanceProbe>();
            if(DevArgs.Has("-look"))gameObject.AddComponent<ZoneLookProbe>();
            if(DevArgs.Has("-creature-defeat"))gameObject.AddComponent<CreatureDefeatProbe>();
#endif
            var zone=quality != null ? quality.scene : DevArgs.Value("-scene");
            if(string.IsNullOrEmpty(zone)||zone=="Title") yield return SceneManager.LoadSceneAsync("Title",LoadSceneMode.Additive);
            else SceneFlow.Current.LoadZone(zone);
        }
    }
}
