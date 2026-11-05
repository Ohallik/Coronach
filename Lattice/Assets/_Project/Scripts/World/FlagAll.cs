using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>Sets <see cref="result"/> once every listed flag is set: the gorge counts its three relays this way.</summary>
    public sealed class FlagAll:MonoBehaviour
    {
        public string[] flags=System.Array.Empty<string>();public string result;
        void Update()
        {
            var service=GameServices.Current;if(service==null||string.IsNullOrEmpty(result)||service.Flags.GetBool(result))return;
            foreach(var flag in flags)if(!service.Flags.GetBool(flag))return;
            service.Flags.SetBool(result,true);Debug.Log("FLAG_ALL "+result);
        }
    }
}
