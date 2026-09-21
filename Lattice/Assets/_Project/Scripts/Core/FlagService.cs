using System;
using System.Collections.Generic;
namespace Lattice.Core
{
    public sealed class FlagService
    {
        readonly Dictionary<string,bool> values;
        public event Action<string> Changed;
        public FlagService(Dictionary<string,bool> values) { this.values=values; }
        public bool GetBool(string key,bool fallback=false)=>values.TryGetValue(key,out var v)?v:fallback;
        public void SetBool(string key,bool value)
        {
            if(string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Flag key required");
            if(values.TryGetValue(key,out var old)&&old==value)return;
            values[key]=value; Changed?.Invoke(key);
        }
    }
}
