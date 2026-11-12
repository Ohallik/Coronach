using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
namespace Lattice.Core
{
    /// <summary>Quits once the GPU has nothing of the game's in flight. Twice in 162 replay exits the player crashed inside D3D12Core while Unity tore the device down
    /// (QA-031): both dumps fault in the same engine release path on the main thread, both were captured runs, and both quit about 4 s after the relay lift's load,
    /// on the frame the capture released its render target. So the last work is done first: queued saves reach the disk, readbacks finish, and a few frames pass
    /// so the device retires what was released before Unity starts releasing everything itself. The cameras keep drawing: switched off, Camera.main goes null
    /// under scripts that don't expect it. It makes the engine's fault less likely; it can't remove it, and if it recurs the next Unity 6000.4 patch is the cure to try.</summary>
    public sealed class OrderlyQuit:MonoBehaviour
    {
        /// <summary>Enough frames for D3D12's frames in flight to retire, plus margin, at any frame rate the game runs.</summary>
        public const int SettleFrames=8;
        public static bool Quitting{get;private set;}
        public static void Quit(int exitCode=0)
        {
            // The editor ignores a quit; its suites press Quit buttons and carry on.
            if(Application.isEditor){Application.Quit(exitCode);return;}
            if(Quitting)return;Quitting=true;
            var runner=new GameObject("OrderlyQuit");DontDestroyOnLoad(runner);runner.AddComponent<OrderlyQuit>().StartCoroutine(Run(exitCode));
        }
        static IEnumerator Run(int exitCode)
        {
            SaveSystem.Drain();
            AsyncGPUReadback.WaitAllRequests();
            for(int i=0;i<SettleFrames;i++)yield return null;
            SaveSystem.Drain();
            Application.Quit(exitCode);
        }
    }
}
