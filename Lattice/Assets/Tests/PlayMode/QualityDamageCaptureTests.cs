using System.Collections;
using System.IO;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class QualityDamageCaptureTests
    {
        string folder;Health victim,source;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.3f);
            victim=new GameObject("trace victim").AddComponent<Health>();victim.id="trace victim";victim.friendly=true;
            source=new GameObject("trace source").AddComponent<Health>();source.id="trace source";
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C4/damage-test-"+System.Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(folder);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;}
        QualityDamageCapture.Report Read()=>JsonUtility.FromJson<QualityDamageCapture.Report>(File.ReadAllText(Path.Combine(folder,"damage.json")));
        [UnityTest] public IEnumerator DamageKeepsTheSourceStateAndClockAtImpact()
        {
            double seconds=4.125;int step=7;source.integrity=0;
            var packet=new DamagePacket{source=source,amount=14,type=DamageType.Beam,tag="released-shot"};
            using(var trace=new QualityDamageCapture(folder,()=>seconds,()=>step))
            {
                trace.Watch(victim);trace.Watch(victim);victim.transform.position=new Vector3(2,1,3);
                float received=victim.Receive(packet);
                source.Heal(100);victim.transform.position=Vector3.one*50;seconds=5.75;step=8;
                victim.Receive(packet);trace.Finish();
                var report=Read();Assert.IsTrue(report.complete);Assert.AreEqual(0,report.dropped);Assert.AreEqual(2,report.hits.Length,"duplicate subscriptions or lost events");
                var first=report.hits[0];Assert.AreEqual(4.125,first.seconds);Assert.AreEqual(7,first.step);
                Assert.AreEqual("trace source",first.source);Assert.IsTrue(first.sourceAvailable);Assert.IsFalse(first.sourceAlive,"later revival rewrites the impact source state");
                Assert.AreEqual("trace victim",first.victim);Assert.AreEqual("Beam",first.type);Assert.AreEqual("released-shot",first.tag);
                Assert.AreEqual(14,first.requested);Assert.AreEqual(received,first.received);Assert.AreEqual(100-received,first.remaining);
                Assert.AreEqual(new Vector3(2,1,3),first.position);Assert.IsTrue(report.hits[1].sourceAlive);
                Assert.AreEqual(5.75,report.hits[1].seconds);Assert.AreEqual(8,report.hits[1].step);
            }
            yield return null;
        }
        [UnityTest] public IEnumerator OverflowRetainsEvidenceAndRejectsCompleteness()
        {
            using(var trace=new QualityDamageCapture(folder,()=>0,()=>0,1))
            {
                trace.Watch(victim);var packet=new DamagePacket{source=source,amount=1,type=DamageType.Beam};
                victim.Receive(packet);victim.Receive(packet);trace.Finish();
            }
            var report=Read();Assert.IsFalse(report.complete);Assert.AreEqual(1,report.dropped);Assert.AreEqual(1,report.hits.Length);
            yield return null;
        }
        [UnityTest] public IEnumerator InterruptedCaptureIsExplicitlyIncomplete()
        {
            var trace=new QualityDamageCapture(folder,()=>0,()=>0);trace.Watch(victim);
            victim.Receive(new DamagePacket{source=source,amount=1,type=DamageType.Beam});trace.Dispose();
            victim.Receive(new DamagePacket{source=source,amount=1,type=DamageType.Beam});trace.Finish();
            var report=Read();Assert.IsFalse(report.complete);Assert.AreEqual(1,report.hits.Length);Assert.AreEqual(0,report.dropped);
            yield return null;
        }
    }
}
