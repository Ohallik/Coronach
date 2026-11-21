using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Someone who is in more than one place over a chapter, in this one of them: here from one flag, away between two others, gone for good at a last one. Teo stands by
    /// Rootmarket's landing pool once the terms are heard, is at Seedfall from the Seedthane's fall until the vote, and is back after it; a second object in the other scene has
    /// the complement, so there is only ever one of him. Decided as the scene loads by default, so nobody blinks in or out in front of the party: a flag set while the party is
    /// standing here (the terms) takes effect at once only where <see cref="live"/> says the place is out of sight when it happens. It lives on an always-active object, as a FlagGate does.
    /// </summary>
    public sealed class Whereabouts:MonoBehaviour
    {
        /// <summary>Not here until this is set (empty: from the start).</summary>
        public string from;
        /// <summary>Away while <see cref="awayFrom"/> is set and <see cref="backAt"/> isn't.</summary>
        public string awayFrom,backAt;
        /// <summary>Never here again once this is set.</summary>
        public string goneAt;
        public GameObject[] targets=System.Array.Empty<GameObject>();
        /// <summary>Follows the flags as they change, not only at load.</summary>
        public bool live;
        void Start()=>Apply();
        void Update(){if(live)Apply();}
        public bool Here
        {
            get
            {
                var flags=GameServices.Current.Flags;
                if(!string.IsNullOrEmpty(from)&&!flags.GetBool(from))return false;
                if(!string.IsNullOrEmpty(goneAt)&&flags.GetBool(goneAt))return false;
                return string.IsNullOrEmpty(awayFrom)||!flags.GetBool(awayFrom)||!string.IsNullOrEmpty(backAt)&&flags.GetBool(backAt);
            }
        }
        void Apply()
        {
            if(GameServices.Current==null)return;bool here=Here;
            foreach(var target in targets)if(target!=null&&target.activeSelf!=here)target.SetActive(here);
        }
    }
}
