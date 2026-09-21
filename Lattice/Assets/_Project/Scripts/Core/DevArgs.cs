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
            return name!="-scene"&&name!="-spawn"&&name!="-loadout"&&name!="-route"&&name!="-perf"&&name!="-uismoke"&&name!="-portraitsmoke"&&name!="-balance"&&name!="-look";
#endif
        }
        public static bool Has(string name)=>Allowed(name)&&Array.IndexOf(Environment.GetCommandLineArgs(),name)>=0;
        public static string Value(string name)
        {
            if(!Allowed(name))return null;
            var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,name);
            return i>=0 && i+1<a.Length?a[i+1]:null;
        }
    }
}
