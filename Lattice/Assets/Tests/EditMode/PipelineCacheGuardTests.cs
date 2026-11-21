using System.IO;
using Lattice.Core;
using NUnit.Framework;
namespace Lattice.Tests.EditMode
{
    /// <summary>The guard that keeps D3D12's pipeline library from being carried between sessions (QA-031): whatever it finds, a folder stands where the file would be.</summary>
    public sealed class PipelineCacheGuardTests
    {
        string folder;
        [SetUp]public void MakeFolder(){folder=Path.Combine(Path.GetTempPath(),"coronach-pipeline-guard-"+System.Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);}
        [TearDown]public void RemoveFolder(){if(Directory.Exists(folder))Directory.Delete(folder,true);}
        string Library=>Path.Combine(folder,PipelineCacheGuard.LibraryFile);
        [Test]
        public void AFolderStandsWhereTheLibraryWouldBeWritten()
        {
            Assert.IsTrue(PipelineCacheGuard.Block(folder));
            Assert.IsTrue(Directory.Exists(Library),"nothing stands in the library file's place, so the engine would write it and load it next session");
            Assert.Throws<System.UnauthorizedAccessException>(()=>File.WriteAllBytes(Library,new byte[]{1}),"a file could still be written over the folder");
        }
        [Test]
        public void ALibraryLeftByAnEarlierBuildIsRemovedEvenWhenReadOnly()
        {
            File.WriteAllBytes(Library,new byte[4096]);File.SetAttributes(Library,FileAttributes.ReadOnly);
            Assert.IsTrue(PipelineCacheGuard.Block(folder));
            Assert.IsFalse(File.Exists(Library),"the earlier build's library file is still there to be loaded");
            Assert.IsTrue(Directory.Exists(Library));
        }
        [Test]
        public void RunningItAgainChangesNothing()
        {
            Assert.IsTrue(PipelineCacheGuard.Block(folder));
            File.WriteAllText(Path.Combine(folder,"other.bin"),"kept");
            Assert.IsTrue(PipelineCacheGuard.Block(folder));
            Assert.IsTrue(Directory.Exists(Library));
            Assert.AreEqual("kept",File.ReadAllText(Path.Combine(folder,"other.bin")),"the guard touched a file that isn't the library");
        }
        [Test]
        public void AFolderThatCannotBeUsedIsReportedAndNothingIsThrown()
        {
            Assert.IsFalse(PipelineCacheGuard.Block(null));
            Assert.IsFalse(PipelineCacheGuard.Block(""));
            // A file where the cache folder should be: nothing can be made inside it.
            string notAFolder=Path.Combine(folder,"file");File.WriteAllText(notAFolder,"x");
            Assert.IsFalse(PipelineCacheGuard.Block(notAFolder));
        }
    }
}
