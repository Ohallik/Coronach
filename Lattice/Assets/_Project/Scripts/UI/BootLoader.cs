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
            var zone=DevArgs.Value("-scene");
            if(string.IsNullOrEmpty(zone)||zone=="Title") yield return SceneManager.LoadSceneAsync("Title",LoadSceneMode.Additive);
            else SceneFlow.Current.LoadZone(zone);
        }
    }
}
