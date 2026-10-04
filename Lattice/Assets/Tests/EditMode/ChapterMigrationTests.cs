using System;
using System.IO;
using Lattice.Core;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Lattice.Tests.EditMode
{
    public sealed class ChapterMigrationTests
    {
        string directory;
        SaveSystem saves;
        [SetUp] public void Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"Coronach-chapter-migration-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);saves=new SaveSystem(directory);
        }
        [TearDown] public void Cleanup(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
        void Write(GameState state)=>File.WriteAllText(saves.PathForSlot("slot1"),JsonConvert.SerializeObject(state,Formatting.Indented));

        [Test] public void FreshProfilesUseTheChapterFormatWithoutLegacyAccess()
        {
            var state=new GameState();
            Assert.AreEqual(2,state.version,"fresh profiles still use the optional-nursery format");
            Assert.IsFalse(state.flags.ContainsKey("legacy.gulletAccess"));
        }

        [TestCase("Hub_CinderHalo")][TestCase("Sorrel_Ridges")][TestCase("Hushwell")]
        [TestCase("Gullet_Tunnel")][TestCase("TallowApproach")][TestCase("TallowDrift")]
        public void OldKeyOwnersKeepEarnedAccessWithoutInventingANurseryVisit(string zone)
        {
            var old=new GameState{version=1,zone=zone,scrip=377,savedAtUtc="2026-09-01T12:34:56Z"};
            old.flags["warpkey"]=true;old.flags["bossdown.Burrower"]=true;
            old.materials["WarpKey"]=1;old.questSteps["Sorrel"]=4;old.questCounts["Sorrel/3"]=1;
            old.party[0].level=4;old.party[0].xp=172;Write(old);
            var bytes=File.ReadAllBytes(saves.PathForSlot("slot1"));
            Assert.IsTrue(saves.Exists("slot1"));var loaded=saves.Load("slot1");
            Assert.IsNotNull(loaded);Assert.AreEqual(2,loaded.version);
            Assert.IsTrue(loaded.flags.TryGetValue("legacy.gulletAccess",out bool access)&&access);
            Assert.IsFalse(loaded.flags.TryGetValue("hushwell.nursery",out bool nursery)&&nursery);
            Assert.AreEqual(zone,loaded.zone);Assert.AreEqual(377,loaded.scrip);
            Assert.AreEqual(1,loaded.materials["WarpKey"]);Assert.AreEqual(4,loaded.questSteps["Sorrel"]);
            Assert.AreEqual(1,loaded.questCounts["Sorrel/3"]);Assert.AreEqual(4,loaded.party[0].level);
            Assert.AreEqual(172,loaded.party[0].xp);Assert.AreEqual(old.savedAtUtc,loaded.savedAtUtc);
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(saves.PathForSlot("slot1")),"read-only load rewrote the source save");
            Assert.IsFalse(File.Exists(saves.PathForSlot("slot1")+".bak"));
            saves.Save("slot2",loaded);var repeated=saves.Load("slot2");
            Assert.AreEqual(2,repeated.version);Assert.IsTrue(repeated.flags["legacy.gulletAccess"]);
            Assert.AreEqual(377,repeated.scrip,"migration replayed a reward");
            CollectionAssert.AreEquivalent(loaded.questSteps,repeated.questSteps);
        }

        [Test] public void OldPreKeyProfilesDoNotAcquireLegacyAccess()
        {
            var old=new GameState{version=1};old.flags["bossdown.Burrower"]=true;Write(old);
            var loaded=saves.Load("slot1");Assert.IsNotNull(loaded);Assert.AreEqual(2,loaded.version);
            Assert.IsFalse(loaded.flags.TryGetValue("legacy.gulletAccess",out bool access)&&access);
        }

        [Test] public void ExistingDiscoveryAndItsRewardsSurviveMigrationExactlyOnce()
        {
            var old=new GameState{version=1,scrip=523};old.flags["hushwell.nursery"]=true;
            old.flags["quest.Hushwell.complete"]=true;old.questSteps["Hushwell"]=2;
            old.party[0].xp=799;Write(old);
            var loaded=saves.Load("slot1");Assert.IsNotNull(loaded);Assert.AreEqual(2,loaded.version);
            Assert.IsTrue(loaded.flags["hushwell.nursery"]);Assert.AreEqual(2,loaded.questSteps["Hushwell"]);
            Assert.AreEqual(523,loaded.scrip);Assert.AreEqual(799,loaded.party[0].xp);
        }

        [TestCase(0)][TestCase(-1)][TestCase(3)][TestCase(999)]
        public void UnsupportedVersionsAreRejectedWithoutWriting(int version)
        {
            Write(new GameState{version=version});var before=File.ReadAllBytes(saves.PathForSlot("slot1"));
            Assert.IsNull(saves.Load("slot1"));Assert.IsFalse(saves.Exists("slot1"));
            CollectionAssert.AreEqual(before,File.ReadAllBytes(saves.PathForSlot("slot1")));
        }

        [TestCase("{\"party\":[{\"id\":\"Taren\"}]}")]
        [TestCase("{\"version\":\"1\",\"party\":[{\"id\":\"Taren\"}]}")]
        public void AnAbsentOrTextVersionCannotBeGuessedFromFieldDefaults(string json)
        {
            File.WriteAllText(saves.PathForSlot("slot1"),json);
            var before=File.ReadAllBytes(saves.PathForSlot("slot1"));
            Assert.IsNull(saves.Load("slot1"));Assert.IsFalse(saves.Exists("slot1"));
            CollectionAssert.AreEqual(before,File.ReadAllBytes(saves.PathForSlot("slot1")));
        }
    }
}
