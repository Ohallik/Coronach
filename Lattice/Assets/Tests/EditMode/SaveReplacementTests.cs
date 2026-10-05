using System;
using System.IO;
using System.Threading;
using Lattice.Core;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Lattice.Tests.EditMode
{
    public sealed class SaveReplacementTests
    {
        string directory;
        SaveSystem saves;
        string path;

        [SetUp] public void Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"coronach-replace-"+Guid.NewGuid().ToString("N"));
            saves=new SaveSystem(directory);path=saves.PathForSlot("slot3");
            saves.Save("slot3",new GameState{scrip=10});
            saves.Save("slot3",new GameState{scrip=20});
        }

        [TearDown] public void Cleanup()
        {
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }

        static int ReadScrip(string file)=>JsonConvert.DeserializeObject<GameState>(File.ReadAllText(file)).scrip;

        [TestCase("")] [TestCase(".bak")]
        public void BriefReaderCannotLoseTheNextSaveOrItsPreviousVersion(string heldSuffix)
        {
            // Real OS sharing denial, including 1175 when the backup is held.
            var held=File.Open(path+heldSuffix,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
            var release=new Thread(()=>{Thread.Sleep(50);held.Dispose();});
            release.Start();
            try{saves.Save("slot3",new GameState{scrip=30});}
            finally{release.Join();held.Dispose();}
            Assert.AreEqual(30,saves.Load("slot3").scrip);
            Assert.AreEqual(20,ReadScrip(path+".bak"));
            Assert.IsFalse(File.Exists(path+".tmp"));
        }

        [TestCase("")] [TestCase(".bak")]
        public void PersistentReaderReportsFailureAndPreservesAllThreeVersions(string heldSuffix)
        {
            using(var held=File.Open(path+heldSuffix,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
            {
                var timer=System.Diagnostics.Stopwatch.StartNew();
                Assert.Throws<IOException>(()=>saves.Save("slot3",new GameState{scrip=30}));
                Assert.Less(timer.Elapsed.TotalSeconds,1,"persistent failure must not hang saving");
                Assert.AreEqual(20,ReadScrip(path));
                Assert.AreEqual(10,ReadScrip(path+".bak"));
                Assert.AreEqual(30,ReadScrip(path+".tmp"));
            }
            // The caller can retry the same slot normally after the reader closes.
            saves.Save("slot3",new GameState{scrip=30});
            Assert.AreEqual(30,saves.Load("slot3").scrip);
            Assert.AreEqual(20,ReadScrip(path+".bak"));
        }
    }
}
