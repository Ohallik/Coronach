using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>Stages the music the current slice actually plays. Every other supplied
    /// original stays in Music/ until its map exists, so builds carry no unused tracks.</summary>
    public static class MusicIntake
    {
        public static readonly string[] SliceTracks =
            { "Title Theme", "Hub Town Groove", "Moonbase Market", "Adventure Awaits", "Starfight", "Alien Boss Battle" };
        [Serializable] sealed class Track { public string title, file, sha256, status; public long bytes; }
        [Serializable] sealed class Manifest { public Track[] tracks; }

        public static void Stage() => BatchTools.Run(() =>
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(root, "docs/music/tracks.json")));
            foreach (string title in SliceTracks)
            {
                var track = manifest.tracks.SingleOrDefault(t => t.title == title) ?? throw new InvalidDataException("Track absent from music manifest: " + title);
                if (track.status != "slice") throw new InvalidDataException("Manifest does not assign this track to the slice: " + title);
                string source = Path.Combine(root, track.file);
                using (var sha = SHA256.Create()) using (var stream = File.OpenRead(source))
                    if (BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != track.sha256)
                        throw new InvalidDataException("Nathan's original changed since the manifest: " + track.file);
                PackStaging.StageFile(track.file, "Resources/Audio/Music/" + title + ".ogg");
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string title in SliceTracks)
            {
                // Same import contract as the original presentation intake: streamed Vorbis stereo.
                var importer = (AudioImporter)AssetImporter.GetAtPath("Assets/_Project/Resources/Audio/Music/" + title + ".ogg");
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.Streaming; settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .85f;
                importer.defaultSampleSettings = settings; importer.forceToMono = false; importer.loadInBackground = true; importer.SaveAndReimport();
            }
            Debug.Log("MUSIC_STAGED_OK count=" + SliceTracks.Length);
        });
    }
}
