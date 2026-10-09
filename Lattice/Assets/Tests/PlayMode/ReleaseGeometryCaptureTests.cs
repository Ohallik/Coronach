using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
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
    [DefaultExecutionOrder(2001)] public sealed class ReleaseGeometryProbe:MonoBehaviour
    {
        public ReleaseGeometryCapture capture;
        public int samples;
        void LateUpdate()
        {
            if(capture==null)return;
            var party=PartyController.Current;
            capture.Record(samples++,Time.frameCount,Time.realtimeSinceStartupAsDouble,7,party.Active,party.members[1-party.index],Camera.main);
        }
    }
    public sealed class ReleaseGeometryCaptureTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        string folder;
        EnemyBrain enemy;
        CombatActor active,partner;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var brain in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))brain.Passive=true;
            var party=PartyController.Current;active=party.Active;partner=party.members[1-party.index];
            foreach(var hero in party.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;hero.Health.Damageable=false;hero.GetComponent<FlightMotor>().Halt();}
            enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),new Vector3(200,1,0));enemy.Passive=true;
            yield return new WaitForSecondsRealtime(.5f);
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C4/release-geometry/test-"+System.Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(folder);Debug.Log("RELEASE_GEOMETRY_TEST "+folder);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        void Release()
        {
            var hit=new DamagePacket{source=active.Health,amount=100000,type=DamageType.Pulse};
            foreach(var link in enemy.GetComponent<CantorCollar>().Links.ToArray())link.Receive(hit);
            enemy.Health.Receive(hit);Assert.IsTrue(enemy.GetComponent<CantorRelease>().Departing);
        }
        void Record(ReleaseGeometryCapture capture,int sample=0,CombatActor companion=null)
            =>capture.Record(sample,Time.frameCount+sample,.25+sample,7,active,companion!=null?companion:partner,Camera.main);
        ReleaseGeometryCapture.Report Report()=>JsonUtility.FromJson<ReleaseGeometryCapture.Report>(File.ReadAllText(Path.Combine(folder,"release-geometry.json")));
        ReleaseGeometryCapture.Frame[] Frames()=>File.ReadAllLines(Path.Combine(folder,"release-geometry.jsonl")).Select(JsonUtility.FromJson<ReleaseGeometryCapture.Frame>).ToArray();
        void Close(ReleaseGeometryCapture capture,int sample=1)
        {enemy.Health.Heal(enemy.Health.maximum);Record(capture,sample);capture.Finish();}

        [UnityTest] public IEnumerator CapturesEveryRealSectionAndBothEvaluatedHulls()
        {
            Release();var release=enemy.GetComponent<CantorRelease>();release.Advance(.85f);
            var roots=new[]{enemy.GetComponent<DefeatPresentation>().visual}.Concat(enemy.GetComponent<SerpentSegments>().Parts).ToArray();
            var positions=roots.Select(r=>r.position).ToArray();
            var expected=roots.Select(r=>r.GetComponentsInChildren<MeshRenderer>()[0]).ToArray();
            using(var capture=new ReleaseGeometryCapture(folder))
            {
                Record(capture);Close(capture);Assert.IsNull(capture.Failure);
            }
            var frames=Frames();Assert.AreEqual(1,frames.Length);var row=frames[0];
            Assert.AreEqual(8,row.anatomy.Length);Assert.AreEqual(2,row.heroes.Length);
            Assert.AreEqual(.85f,row.releaseAge,.00001f);Assert.AreEqual(.25,row.elapsed);Assert.AreEqual(7,row.step);
            for(int i=0;i<8;i++)
            {
                var shape=row.anatomy[i];Assert.IsTrue(shape.available);Assert.AreEqual(positions[i],shape.position);
                Assert.AreEqual(i==0?"head":"part"+i,shape.role);Assert.Greater(shape.meshes,0);Assert.AreEqual(shape.meshes*8,shape.corners.Length);
                Assert.Greater(Vector3.Distance(shape.minimum,shape.maximum),.5f);
                // The saved bounds must enclose the evaluated local mesh corners,
                // independently reprojected through the actual camera.
                foreach(var corner in shape.corners)
                {
                    Assert.That(corner.x,Is.InRange(shape.minimum.x-.001f,shape.maximum.x+.001f));
                    Assert.That(corner.z,Is.InRange(shape.minimum.z-.001f,shape.maximum.z+.001f));
                    var viewport=Camera.main.WorldToViewportPoint(corner);
                    Assert.That(viewport.x,Is.InRange(shape.viewportMinimum.x-.00001f,shape.viewportMaximum.x+.00001f));
                    Assert.That(viewport.y,Is.InRange(shape.viewportMinimum.y-.00001f,shape.viewportMaximum.y+.00001f));
                }
            }
            Assert.AreEqual(active.character,row.heroes[0].hero);Assert.AreEqual(partner.character,row.heroes[1].hero);
            Assert.AreEqual(active.transform.position,row.heroes[0].position);Assert.AreEqual(partner.transform.position,row.heroes[1].position);
            Assert.AreEqual(HeroCollision.HullRadius(partner.character),row.heroes[1].hullRadius);
            foreach(var shape in row.heroes){Assert.IsTrue(shape.available);Assert.Greater(shape.corners.Length,0);Assert.Greater(Vector3.Distance(shape.minimum,shape.maximum),1);}
            Assert.IsTrue(Report().complete);Assert.AreEqual("recovered",Report().spans[0].end);yield return null;
        }
        [UnityTest] public IEnumerator CompleteLateDepartureRetainsPausedFramesAndStopsAtRemoval()
        {
            var order=typeof(ContinuousReview).GetCustomAttribute<DefaultExecutionOrder>();Assert.GreaterOrEqual(order.order,2000);
            var probe=active.gameObject.AddComponent<ReleaseGeometryProbe>();
            using(var capture=new ReleaseGeometryCapture(folder))
            {
                probe.capture=capture;Release();yield return new WaitForSecondsRealtime(.6f);
                GameTime.Paused=true;yield return new WaitForSecondsRealtime(.35f);GameTime.Paused=false;
                yield return new WaitForSecondsRealtime(3.6f);
                Assert.IsTrue(enemy==null);probe.capture=null;capture.Finish();Assert.IsNull(capture.Failure);
            }
            var rows=Frames();Assert.Greater(rows.Length,140);var paused=rows.Where(r=>r.paused).ToArray();Assert.Greater(paused.Length,10);
            foreach(var row in paused)
            {Assert.AreEqual(paused[0].releaseAge,row.releaseAge);Assert.AreEqual(paused[0].sourcePosition,row.sourcePosition);Assert.AreEqual(paused[0].anatomy[7].position,row.anatomy[7].position);}
            for(int i=1;i<rows.Length;i++){Assert.AreEqual(rows[i-1].sample+1,rows[i].sample);Assert.AreEqual(rows[i-1].frame+1,rows[i].frame);Assert.Greater(rows[i].elapsed,rows[i-1].elapsed);}
            Assert.Greater(rows.Last().releaseAge,3.5f);Assert.AreEqual("removed",Report().spans[0].end);Assert.IsTrue(Report().complete);
        }
        [UnityTest] public IEnumerator ReplayLateUpdateActuallyFeedsTheGeometryClock()
        {
            Release();var fixture=new GameObject("inactive review fixture");fixture.SetActive(false);var replay=fixture.AddComponent<ContinuousReview>();
            void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
            using(var capture=new ReleaseGeometryCapture(folder))
            {
                try
                {
                    Set("releaseGeometry",capture);Set("recording",true);Set("stepIndex",7);
                    var clock=(System.Diagnostics.Stopwatch)typeof(ContinuousReview).GetField("clock",Private).GetValue(replay);clock.Start();
                    typeof(ContinuousReview).GetMethod("LateUpdate",Private).Invoke(replay,null);
                    enemy.Health.Heal(enemy.Health.maximum);yield return null;
                    typeof(ContinuousReview).GetMethod("LateUpdate",Private).Invoke(replay,null);capture.Finish();
                    Assert.IsNull(capture.Failure);Assert.AreEqual(1,Frames().Length);Assert.AreEqual(2,Report().samples);Assert.AreEqual(7,Frames()[0].step);
                }
                finally{Object.Destroy(fixture);}
            }
        }
        [UnityTest] public IEnumerator ReplayCsvPreservesTheRecordedWorldCoordinatePrecision()
        {
            var fixture=new GameObject("inactive precision fixture");fixture.SetActive(false);var replay=fixture.AddComponent<ContinuousReview>();
            void Set(string name,object value)=>typeof(ContinuousReview).GetField(name,Private).SetValue(replay,value);
            var cc=active.GetComponent<CharacterController>();cc.enabled=false;
            active.transform.position=new Vector3(-1.8226261138916016f,1,808.8939208984375f);cc.enabled=true;
            Camera.main.transform.position=new Vector3(-.7798277735710144f,33.80458068847656f,791.0359497070312f);
            var expectedActor=active.transform.position;var expectedCamera=Camera.main.transform.position;
            try
            {
                Set("recording",true);Set("folder",folder);Set("stepIndex",0);
                Set("route",new QualityRoute{name="coordinate precision",scene="Arena_Flight",steps=new[]{new QualityStep{name="hold",seconds=1}}});
                var clock=(System.Diagnostics.Stopwatch)typeof(ContinuousReview).GetField("clock",Private).GetValue(replay);clock.Start();
                typeof(ContinuousReview).GetMethod("LateUpdate",Private).Invoke(replay,null);yield return null;
                typeof(ContinuousReview).GetMethod("LateUpdate",Private).Invoke(replay,null);
                typeof(ContinuousReview).GetMethod("WriteEvidence",Private).Invoke(replay,null);
                var lines=File.ReadAllLines(Path.Combine(folder,"frames.csv"));var headers=lines[0].Split(',');var values=lines[1].Split(',');
                double Value(string key)=>double.Parse(values[System.Array.IndexOf(headers,key)],System.Globalization.CultureInfo.InvariantCulture);
                for(int axis=0;axis<3;axis++)
                {
                    Assert.That(System.Math.Abs(Value("xyz"[axis].ToString())-expectedActor[axis]),Is.LessThanOrEqualTo(.00002),"active coordinate lost the strict geometry-matching precision");
                    Assert.That(System.Math.Abs(Value("camera"+"XYZ"[axis])-expectedCamera[axis]),Is.LessThanOrEqualTo(.00002),"camera coordinate lost the strict geometry-matching precision");
                }
            }
            finally{Object.Destroy(fixture);}
        }

        [UnityTest] public IEnumerator MissingBodyMeshRetainsItsRowAndRejectsCompleteness()
        {
            Release();foreach(var mesh in enemy.GetComponent<SerpentSegments>().Parts[3].GetComponentsInChildren<Renderer>())mesh.enabled=false;
            using(var capture=new ReleaseGeometryCapture(folder)){Record(capture);Close(capture);StringAssert.Contains("unavailable for part4",capture.Failure);}
            Assert.IsFalse(Report().complete);Assert.IsFalse(Frames()[0].anatomy[4].available);yield return null;
        }
        [UnityTest] public IEnumerator MissingPartnerRetainsTheMissingHullAndRejectsCompleteness()
        {
            Release();using(var capture=new ReleaseGeometryCapture(folder))
            {capture.Record(0,Time.frameCount,.25,7,active,null,Camera.main);Close(capture);Assert.IsNotNull(capture.Failure);}
            Assert.IsFalse(Report().complete);Assert.IsFalse(Frames()[0].heroes[1].available);yield return null;
        }
        [UnityTest] public IEnumerator OverflowKeepsItsFirstFrameAndRejectsDroppedEvidence()
        {
            Release();using(var capture=new ReleaseGeometryCapture(folder,1)){Record(capture);Record(capture,1);Close(capture,2);StringAssert.Contains("frame limit",capture.Failure);}
            Assert.IsFalse(Report().complete);Assert.AreEqual(1,Report().dropped);Assert.AreEqual(1,Frames().Length);yield return null;
        }
        [UnityTest] public IEnumerator InterruptedOrMidReleaseCaptureCannotClaimComplete()
        {
            Release();using(var capture=new ReleaseGeometryCapture(folder)){Record(capture);}
            Assert.IsFalse(Report().complete);StringAssert.Contains("interrupted",Report().failure);Assert.AreEqual("capture-ended-mid-release",Report().spans[0].end);yield return null;
        }
        [UnityTest] public IEnumerator MissingSampleCannotClaimComplete()
        {
            using(var capture=new ReleaseGeometryCapture(folder)){Record(capture);Record(capture,2);capture.Finish();StringAssert.Contains("sequence",capture.Failure);}
            Assert.IsFalse(Report().complete);yield return null;
        }
        [UnityTest] public IEnumerator NoDepartureIsAnExplicitEmptyTrace()
        {
            using(var capture=new ReleaseGeometryCapture(folder)){Record(capture);capture.Finish();Assert.IsNull(capture.Failure);}
            Assert.IsTrue(Report().complete);Assert.AreEqual(0,Frames().Length);Assert.AreEqual(0,Report().spans.Length);yield return null;
        }
    }
}
