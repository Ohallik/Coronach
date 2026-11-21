using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Someone who arrives where the party can see, a moment after a flag: the other half of <see cref="LeavesAfter"/>. Meret gets up from the dinner table at the end of the Sorrel talk
    /// and walks to the galley door (LeavesAfter on her seat); her place at the door is shown by this once she would have got there, so there are never two of her in one frame and she
    /// doesn't blink in ahead of herself. A zone loaded with the flag already set shows its targets at once. Once shown it lets them be: a LeavesAfter under them can take them away
    /// again. Lives on an always-active object, as a FlagGate does.
    /// </summary>
    public sealed class TurnsUp:MonoBehaviour
    {
        public string flag;
        /// <summary>Seconds from the flag to the arrival, in play.</summary>
        public float delay=4;
        public GameObject[] targets=System.Array.Empty<GameObject>();
        float at=-1;bool done;
        void Start()
        {
            if(GameServices.Current==null)return;
            done=GameServices.Current.Flags.GetBool(flag);Show(done);
        }
        void Update()
        {
            if(done||GameServices.Current==null||!GameServices.Current.Flags.GetBool(flag))return;
            if(at<0)at=Time.time+delay;
            if(Time.time<at)return;
            done=true;Show(true);Debug.Log("TURNS_UP "+name);
        }
        void Show(bool on){foreach(var target in targets)if(target!=null&&target.activeSelf!=on)target.SetActive(on);}
    }
}
