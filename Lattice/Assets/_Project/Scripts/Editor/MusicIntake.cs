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
        /// <summary>The tracks the code itself names: the title, the shop, the boss cue and the first two chapters' zones (MusicDirector). Nothing is added here:
        /// a zone or a boss since names its music on its own definition, and <see cref="Named"/> finds it there.</summary>
        public static readonly string[] SliceTracks =
            { "Title Theme", "Hub Town Groove", "Moonbase Market", "Adventure Awaits", "Starfight", "Alien Boss Battle", "Moon Caverns", "Old Village" };
        [Serializable] sealed class Track { public string title, file, sha256, status; public long bytes; }
        [Serializable] sealed class Manifest { public Track[] tracks; }
        const string ByCode = "MusicDirector";

        /// <summary>Every track the game can ask for and who asks for it first: the code's own, then each ZoneDef's (its own music, its fights', its safe
        /// pocket's, its shops') and each boss EnemyDef's, in a fixed order.</summary>
        public static System.Collections.Generic.List<(string title, string by)> Named()
        {
            var named = SliceTracks.Select(title => (title, ByCode)).ToList();
            void Add(string title, string by) { if (!string.IsNullOrWhiteSpace(title) && !named.Exists(n => n.title == title)) named.Add((title, by)); }
            foreach (var zone in Lattice.Core.GameCatalog.All<Lattice.Data.ZoneDef>().Where(z => z != null).OrderBy(z => z.name, StringComparer.Ordinal))
            {
                Add(zone.musicId, "ZoneDef " + zone.name + ".musicId"); Add(zone.fightMusicId, "ZoneDef " + zone.name + ".fightMusicId");
                Add(zone.pocketMusicId, "ZoneDef " + zone.name + ".pocketMusicId"); Add(zone.shopMusicId, "ZoneDef " + zone.name + ".shopMusicId");
                if (zone.cueMusicIds != null) foreach (var cue in zone.cueMusicIds) Add(cue, "ZoneDef " + zone.name + ".cueMusicIds");
            }
            foreach (var enemy in Lattice.Core.GameCatalog.All<Lattice.Data.EnemyDef>().Where(e => e != null && e.boss).OrderBy(e => e.name, StringComparer.Ordinal))
                Add(enemy.bossMusicId, "EnemyDef " + enemy.name + ".bossMusicId");
            return named;
        }

        /// <summary>Every named track has a row in docs/music/tracks.json and a source file on disk, or this throws with all that don't and who names them:
        /// a title misspelt on a definition was a zone that played nothing, found by ear.</summary>
        public static void Validate()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(root, "docs/music/tracks.json")));
            var faults = new System.Collections.Generic.List<string>();
            foreach (var (title, by) in Named())
            {
                var track = manifest.tracks.SingleOrDefault(t => t.title == title);
                if (track == null) faults.Add(by + " names '" + title + "', which docs/music/tracks.json doesn't list");
                else if (!File.Exists(Path.Combine(root, track.file))) faults.Add(by + " names '" + title + "', whose source file is missing: " + track.file);
            }
            if (faults.Count > 0) throw new InvalidDataException("MUSIC_UNSOURCED\n" + string.Join("\n", faults));
        }

        public static void Stage() => BatchTools.Run(() =>
        {
            Validate();
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(root, "docs/music/tracks.json")));
            var named = Named();
            foreach (var (title, by) in named)
            {
                var track = manifest.tracks.SingleOrDefault(t => t.title == title) ?? throw new InvalidDataException("Track absent from music manifest: " + title);
                // The code's own tracks are the ones the manifest assigns to the slice. A definition that names a track is itself the assignment.
                if (by == ByCode && track.status != "slice") throw new InvalidDataException("Manifest does not assign this track to the slice: " + title);
                if (by != ByCode) Debug.Log("MUSIC_NAMED " + title + " by " + by + " (manifest status '" + track.status + "')");
                string source = Path.Combine(root, track.file);
                using (var sha = SHA256.Create()) using (var stream = File.OpenRead(source))
                    if (BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != track.sha256)
                        throw new InvalidDataException("Nathan's original changed since the manifest: " + track.file);
                PackStaging.StageFile(track.file, "Resources/Audio/Music/" + title + ".ogg");
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var (title, _) in named)
            {
                // Same import contract as the original presentation intake: streamed Vorbis stereo.
                var importer = (AudioImporter)AssetImporter.GetAtPath("Assets/_Project/Resources/Audio/Music/" + title + ".ogg");
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.Streaming; settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .85f;
                importer.defaultSampleSettings = settings; importer.forceToMono = false; importer.loadInBackground = true; importer.SaveAndReimport();
            }
            Debug.Log("MUSIC_STAGED_OK count=" + named.Count);
        });
    }
}
