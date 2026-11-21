using System;
using System.Runtime.CompilerServices;

namespace Lattice.Core
{
    /// <summary>
    /// Says, next to the code that does it, "this code sets this flag". A shared test reads every declaration to know which flags the game can set from code
    /// (the script's own setFlag commands and the scenes' components it already sees). It also records the source file it was written in, and a second test checks
    /// that file still holds the flag as a string literal, so a declaration cannot outlive the code that sets the flag.
    /// With <see cref="Prefix"/> true, <see cref="Flag"/> is the start of a family built at run time ("visited." + zone).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class SetsFlagAttribute : Attribute
    {
        public readonly string Flag;
        public readonly string File;
        public bool Prefix { get; set; }
        public SetsFlagAttribute(string flag, [CallerFilePath] string file = "") { Flag = flag; File = file; }
    }
}
