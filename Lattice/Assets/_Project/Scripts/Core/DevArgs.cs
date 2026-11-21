using System;
namespace Lattice.Core
{
    public static class DevArgs
    {
        static bool Allowed(string name)
        {
#if LATTICE_DEV || UNITY_EDITOR
            return true;
#else
            return name!="-scene"&&name!="-spawn"&&name!="-loadout"&&name!="-route"&&name!="-perf"&&name!="-uismoke"&&name!="-portraitsmoke"&&name!="-balance"&&name!="-look"&&name!="-review";
#endif
        }
        /// <summary>Arguments an editor runner hands the entry points it calls inside one Unity (Restage.All): read before the command line while set, a null
        /// value meaning absent. Only an editor tool sets it, and it clears it after each call.</summary>
        public static System.Collections.Generic.Dictionary<string,string> Given;
        public static bool Has(string name)=>Allowed(name)&&(Given!=null&&Given.TryGetValue(name,out var given)?given!=null:Array.IndexOf(Environment.GetCommandLineArgs(),name)>=0);
        public static string Value(string name)
        {
            if(!Allowed(name))return null;
            if(Given!=null&&Given.TryGetValue(name,out var given))return given;
            var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,name);
            return i>=0 && i+1<a.Length?a[i+1]:null;
        }
    }
}
