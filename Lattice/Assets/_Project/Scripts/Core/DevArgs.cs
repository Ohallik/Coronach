using System;
namespace Lattice.Core
{
    public static class DevArgs
    {
        public static bool Has(string name)=>Array.IndexOf(Environment.GetCommandLineArgs(),name)>=0;
        public static string Value(string name)
        {
            var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,name);
            return i>=0 && i+1<a.Length?a[i+1]:null;
        }
    }
}
