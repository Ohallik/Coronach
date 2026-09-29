using System;
using System.IO;
using Lattice.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Lattice.Tests.EditMode
{
    public sealed class AudioPreferencesTests
    {
        string directory,path;AudioPreferences preferences;
        [SetUp] public void Setup()
        {
            directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C5/preferences-tests",Guid.NewGuid().ToString("N")));
            path=Path.Combine(directory,"audio-settings.json");preferences=new AudioPreferences(directory);
        }
        [Test] public void MissingIsolatedSettingsNeverReadOrWriteTheHumanProfile()
        {
            var levels=preferences.Load();foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))Assert.AreEqual(1,levels.Get(bus));
            Assert.IsFalse(Directory.Exists(directory),"loading defaults created settings");
        }
        [Test] public void SeparateLevelsIncludingExactMuteSurviveRepeatedAtomicSaves()
        {
            float legacy=PlayerPrefs.GetFloat("volume",1);var levels=preferences.Load();float value=0;
            foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus))){levels.Set(bus,value);value+=.2f;}
            preferences.Save(levels);var restored=new AudioPreferences(directory).Load();
            foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))Assert.AreEqual(levels.Get(bus),restored.Get(bus),.0001);
            Assert.AreEqual(0,restored.master);Assert.IsFalse(File.Exists(path+".tmp"));
            restored.master=.73f;preferences.Save(restored);Assert.AreEqual(.73f,new AudioPreferences(directory).Load().master,.0001);
            Assert.AreEqual(0,JsonUtility.FromJson<AudioLevels>(File.ReadAllText(path+".bak")).master,"previous mute preference was not retained");
            restored.master=.42f;preferences.Save(restored);Assert.AreEqual(.42f,new AudioPreferences(directory).Load().master,.0001);
            Assert.AreEqual(.73f,JsonUtility.FromJson<AudioLevels>(File.ReadAllText(path+".bak")).master,.0001);
            Assert.AreEqual(legacy,PlayerPrefs.GetFloat("volume",1),"isolated save changed the human preference");
        }
        [Test] public void PartialSettingsKeepOtherDefaultsAndClampOutOfRangeValues()
        {
            Directory.CreateDirectory(directory);File.WriteAllText(path,"{\"version\":1,\"music\":0.35,\"sfx\":9,\"ambience\":-4}");
            var levels=preferences.Load();Assert.AreEqual(.35f,levels.music,.0001);Assert.AreEqual(1,levels.master);Assert.AreEqual(1,levels.ui);Assert.AreEqual(1,levels.sfx);Assert.AreEqual(0,levels.ambience);
        }
        [TestCase("{broken",true)] [TestCase("{\"version\":20,\"master\":0}",false)]
        public void InvalidAndFutureSettingsFallBackWithoutOverwriting(string text,bool warning)
        {
            Directory.CreateDirectory(directory);File.WriteAllText(path,text);
            if(warning)LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("Audio settings were invalid:"));
            var levels=preferences.Load();foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))Assert.AreEqual(1,levels.Get(bus));
            Assert.AreEqual(text,File.ReadAllText(path));
        }
        [Test] public void NonFiniteRequestedGainsCannotContaminateTheMixer()
        {
            var levels=new AudioLevels();levels.Set(AudioBus.Music,float.NaN);levels.Set(AudioBus.SFX,float.PositiveInfinity);
            Assert.AreEqual(1,levels.music);Assert.AreEqual(1,levels.sfx);
        }
    }
}
