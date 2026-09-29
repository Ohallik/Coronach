using System;
using System.IO;
using System.Security.Cryptography;
using Lattice.Core;
using UnityEditor;
using UnityEngine;
namespace Lattice.EditorTools
{
    public static class AudioPaletteSetup
    {
        [Serializable] sealed class FileRecord {public string path,sha256;public long bytes;}
        [Serializable] sealed class ClipRecord {public string key;public FileRecord output;}
        [Serializable] sealed class Manifest {public ClipRecord[] clips;public FileRecord[] sources,licenses;}
        static string Hash(string path)
        {using var sha=SHA256.Create();using var stream=File.OpenRead(path);return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
        static void Verify(FileRecord item)
        {if(!File.Exists(item.path)||new FileInfo(item.path).Length!=item.bytes||Hash(item.path)!=item.sha256)throw new InvalidDataException("Audio provenance mismatch: "+item.path);}
        public static void Stage()=>BatchTools.Run(()=>
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            string source=Path.GetFullPath(DevArgs.Value("-audio-palette")??throw new ArgumentException("Supply the prepared palette manifest"));
            string allowed=Path.Combine(root,"Builds","quality")+Path.DirectorySeparatorChar;
            if(!source.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Use a prepared quality palette");
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(source));
            if(manifest?.clips==null||manifest.clips.Length!=32||manifest.sources==null||manifest.licenses==null)throw new InvalidDataException("Incomplete prepared palette");
            foreach(var item in manifest.sources)Verify(item);
            foreach(var item in manifest.licenses)
            {
                Verify(item);string name=new DirectoryInfo(Path.GetDirectoryName(item.path)).Name.Replace('_','-');
                if(!System.Text.RegularExpressions.Regex.IsMatch(name,"^[A-Za-z0-9-]+$"))throw new InvalidDataException("Invalid licence pack name");
                string target=Path.Combine(Application.dataPath,"_Project/Audio/Licenses",name+".txt");
                if(File.Exists(target)&&Hash(target)!=item.sha256)throw new InvalidOperationException("Preserve existing licence notice: "+target);
            }
            // Validate the entire batch before writing an imported file.
            foreach(var clip in manifest.clips)
            {
                if(!System.Text.RegularExpressions.Regex.IsMatch(clip.key,"^[a-z0-9_]+$"))throw new InvalidDataException("Invalid sound key");
                if(!Path.GetFullPath(clip.output.path).StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Candidate escaped quality directory");
                Verify(clip.output);
                string target=Path.Combine(Application.dataPath,"_Project/Resources/Audio/Palette",clip.key+".wav");
                if(File.Exists(target)&&Hash(target)!=clip.output.sha256)throw new InvalidOperationException("Preserve changed imported audio: "+target);
            }
            foreach(var clip in manifest.clips)
            {
                string path="Assets/_Project/Resources/Audio/Palette/"+clip.key+".wav";
                PackStaging.StageFile(Path.GetRelativePath(root,clip.output.path),"Resources/Audio/Palette/"+clip.key+".wav");
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
                settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.quality=1;settings.preloadAudioData=true;
                importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.loadInBackground=false;importer.SaveAndReimport();
                if(AssetDatabase.LoadAssetAtPath<AudioClip>(path)==null)throw new InvalidOperationException("Sound failed import: "+path);
            }
            // Keep source packs read-only. Verified notice copies enter the same
            // workspace staging boundary as the prepared short audio files.
            string notices=Path.Combine(Path.GetDirectoryName(source),"licences");Directory.CreateDirectory(notices);
            foreach(var item in manifest.licenses)
            {
                string name=new DirectoryInfo(Path.GetDirectoryName(item.path)).Name.Replace('_','-')+".txt";
                string copy=Path.Combine(notices,name);
                if(File.Exists(copy)){if(Hash(copy)!=item.sha256)throw new InvalidOperationException("Preserve changed notice copy: "+copy);}
                else File.Copy(item.path,copy);
                PackStaging.StageFile(Path.GetRelativePath(root,copy),"Audio/Licenses/"+name);
            }
            const string provenance="Audio/Palettes/movement-combat-ui-candidate.json";
            string imported=Path.Combine(Application.dataPath,"_Project",provenance);
            if(File.Exists(imported)&&Hash(imported)!=Hash(source))throw new InvalidOperationException("Preserve existing palette provenance");
            PackStaging.StageFile(Path.GetRelativePath(root,source),provenance);AssetDatabase.Refresh();
            Debug.Log("AUDIO_PALETTE_STAGED_OK count="+manifest.clips.Length+" audition=UNVERIFIED");
        });
    }
}
