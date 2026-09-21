using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Lattice.EditorTools
{
    /// <summary>Ported staging boundary: archives stay outside Assets; only curated ship files enter.</summary>
    public static class PackStaging
    {
        public static void StageFile(string sourceRelative,string destinationRelative)
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            string src=Path.GetFullPath(Path.Combine(root,sourceRelative));
            string dst=Path.GetFullPath(Path.Combine(Application.dataPath,"_Project",destinationRelative));
            if(!src.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||
                !dst.StartsWith(Path.GetFullPath(Application.dataPath)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Staging paths must remain inside the project");
            if(src.Contains("_Meshy.fbx")||src.Contains("_walking_fbx")||src.Contains("_running_fbx"))
                throw new ArgumentException("Intermediate model cannot ship");
            Directory.CreateDirectory(Path.GetDirectoryName(dst));File.Copy(src,dst,true);
        }
    }
}
