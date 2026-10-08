using System.Collections;
using System.Globalization;
using System.IO;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class GroundPoseCaptureTests
    {
        string folder;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.6f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Sorrel_Ridges");
            float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            yield return new WaitForSecondsRealtime(.5f);
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C2/ground-pose-test-"+System.Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(folder);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.3f);
        }
        [UnityTest] public IEnumerator RecordsEvaluatedBonesForBothHeroesBeforeLaterPoseChanges()
        {
            var active=PartyController.Current.Active;var partner=PartyController.Current.members[1-PartyController.Current.index];
            var activeRig=active.GetComponentInChildren<Animator>();var partnerRig=partner.GetComponentInChildren<Animator>();
            var activeKnee=activeRig.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            var partnerKnee=partnerRig.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            activeKnee.position+=new Vector3(.123f,.078f,.321f);
            partnerKnee.position+=new Vector3(-.213f,.187f,-.132f);
            var expected=new[]{activeKnee.position,partnerKnee.position};
            using(var capture=new GroundPoseCapture(folder))
            {
                capture.Record(0,Time.frameCount,.25,7,active,partner);
                activeKnee.position+=Vector3.one;partnerKnee.position-=Vector3.one;
                capture.Finish();Assert.IsNull(capture.Failure);
            }
            var lines=File.ReadAllLines(Path.Combine(folder,"ground-pose.csv"));Assert.AreEqual(3,lines.Length);
            var headers=lines[0].Split(',');
            for(int i=0;i<2;i++)
            {
                var values=lines[i+1].Split(',');Assert.AreEqual(headers.Length,values.Length);
                string Value(string key)=>values[System.Array.IndexOf(headers,key)];
                Assert.AreEqual(i==0?"active":"partner",Value("role"));
                Assert.AreEqual(i==0?active.character:partner.character,Value("hero"));
                Assert.AreEqual("True",Value("available"));
                foreach(char axis in "XYZ")
                {
                    float actual=float.Parse(Value("leftKnee"+axis),CultureInfo.InvariantCulture);
                    Assert.AreEqual(expected[i]["XYZ".IndexOf(axis)],actual,.00001f,"captured knee must be the evaluated world-space bone, not a later pose or other joint");
                }
            }
            yield return null;
        }
        [UnityTest] public IEnumerator MissingPartnerRetainsUnavailableRowAndRejectsCompletion()
        {
            using(var capture=new GroundPoseCapture(folder))
            {
                capture.Record(0,Time.frameCount,.25,7,PartyController.Current.Active,null);
                capture.Finish();StringAssert.Contains("unavailable for partner",capture.Failure);
            }
            var lines=File.ReadAllLines(Path.Combine(folder,"ground-pose.csv"));Assert.AreEqual(3,lines.Length);
            StringAssert.Contains(",partner,",lines[2]);StringAssert.Contains("NaN",lines[2]);
            StringAssert.Contains("\"complete\": false",File.ReadAllText(Path.Combine(folder,"ground-pose.json")));
            yield return null;
        }
    }
}
