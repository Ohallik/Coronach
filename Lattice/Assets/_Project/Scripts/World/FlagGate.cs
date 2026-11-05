using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>Shows its targets only while a flag is set (or only while it is not). Lives on an always-active object
    /// so a hidden target can come back (D202: Hessa under guard, the archive wall lit, a lantern gate opening).</summary>
    public sealed class FlagGate:MonoBehaviour
    {
        public string flag;public bool showWhenSet=true;public GameObject[] targets=System.Array.Empty<GameObject>();
        void Start()=>Apply();
        void Update()=>Apply();
        void Apply()
        {
            if(GameServices.Current==null||string.IsNullOrEmpty(flag))return;
            bool set=GameServices.Current.Flags.GetBool(flag),show=showWhenSet?set:!set;
            foreach(var target in targets)if(target!=null&&target.activeSelf!=show)target.SetActive(show);
        }
    }
}
