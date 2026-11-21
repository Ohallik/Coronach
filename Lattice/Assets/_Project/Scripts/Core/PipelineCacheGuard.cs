using System;
using System.IO;
using UnityEngine;
namespace Lattice.Core
{
    /// <summary>Keeps the engine from carrying D3D12's pipeline library from one session to the next (QA-031). While quitting, the graphics device serialises that
    /// library, and when the library had been loaded from an earlier session's file D3D12Core copied a cached pipeline past the end of mapped memory: an access
    /// violation in GfxDeviceD3D12's destructor, after everything was saved. Six dumps show the same frame (November 6, 7 and 13). On November 13 one cache file, grown
    /// over twelve replay segments, crashed four exits of the same route in a row and was never rewritten; the same route with no file exited cleanly, and so did
    /// every start from an empty folder. So a folder stands where the file would be: the engine starts a new library each session, serialises it, and can't write it
    /// (it logs "Error when writing PSO binary data", which is this). Nothing measurable is lost: the graphics driver keeps its own shader cache, and the Lanternwick
    /// route's frame times were the same with the file and without it. The engine's other cache (-force-d3d12-single-pso-disk-cache, a command-line switch only) is
    /// untouched, and so is a driver without pipeline libraries, which never uses this file.</summary>
    public static class PipelineCacheGuard
    {
        public const string LibraryFile="dx12_pso_cache_lib.bin";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnStart()
        {
            if(Application.isEditor||Application.platform!=RuntimePlatform.WindowsPlayer)return;
            Block(Application.temporaryCachePath);
        }
        /// <summary>Puts a folder where the library file would be written in <paramref name="cacheFolder"/>, removing a file already there. True when the folder stands.
        /// A file left by an earlier build was loaded before any script ran, so that one session can still fault on quitting; every later one starts clean.</summary>
        public static bool Block(string cacheFolder)
        {
            try
            {
                if(string.IsNullOrEmpty(cacheFolder))return false;
                string path=Path.Combine(cacheFolder,LibraryFile);
                if(Directory.Exists(path))return true;
                if(File.Exists(path)){File.SetAttributes(path,FileAttributes.Normal);File.Delete(path);}
                Directory.CreateDirectory(path);
                return Directory.Exists(path);
            }
            // A cache folder that can't be written is no reason to stop the game: the engine then can't write its file there either.
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException||e is NotSupportedException||e is ArgumentException){return false;}
        }
    }
}
