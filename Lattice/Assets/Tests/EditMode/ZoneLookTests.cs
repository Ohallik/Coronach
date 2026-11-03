using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lattice.Tests.EditMode
{
    /// <summary>D060/D189: zone grades may colour the image but never blur or warp it.</summary>
    public sealed class ZoneLookTests
    {
        static string[] Offences(VolumeProfile profile)=>profile.components.Where(c=>c.active&&(
            c is DepthOfField dof&&dof.mode.overrideState&&dof.mode.value!=DepthOfFieldMode.Off||
            c is MotionBlur blur&&blur.intensity.overrideState&&blur.intensity.value>0||
            c is LensDistortion lens&&lens.intensity.overrideState&&lens.intensity.value!=0||
            c is PaniniProjection panini&&panini.distance.overrideState&&panini.distance.value>0||
            c is ChromaticAberration chroma&&chroma.intensity.overrideState&&chroma.intensity.value>0)).Select(c=>c.GetType().Name).ToArray();

        [Test] public void TheCheckerRecognisesABlurringProfile()
        {
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();
            try
            {
                Assert.IsEmpty(Offences(profile));
                profile.Add<DepthOfField>().mode.Override(DepthOfFieldMode.Gaussian);
                profile.Add<MotionBlur>().intensity.Override(.3f);
                CollectionAssert.AreEquivalent(new[]{"DepthOfField","MotionBlur"},Offences(profile));
            }
            finally{Object.DestroyImmediate(profile);}
        }

        [Test] public void EveryZoneVolumeIsSharpAndGraded()
        {
            var paths=AssetDatabase.FindAssets("t:VolumeProfile",new[]{"Assets/_Project/Settings"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith("Volume.asset")).ToArray();
            Assert.GreaterOrEqual(paths.Length,8,"zone volumes missing");
            foreach(var path in paths)
            {
                var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                Assert.IsEmpty(Offences(profile),path+" blurs or warps the image");
                Assert.IsTrue(profile.TryGet<ColorAdjustments>(out var adjust)&&adjust.active&&adjust.contrast.overrideState,path+" has no authored grade");
            }
        }
        [Test] public void PracticalLightsLeaveWithTheirSpeaker()
        {
            foreach(var zone in new[]{"Hub_Decks","TallowDrift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity",OpenSceneMode.Single);
                var lights=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(l=>l.type==LightType.Point&&l.name.StartsWith("Practical")).ToArray();
                Assert.IsNotEmpty(lights,zone+" has no practical lights");
                foreach(var light in lights)
                {
                    var owner=light.GetComponentInParent<Lattice.Core.InteractionPrompt>(true);
                    Assert.IsNotNull(owner,zone+": "+light.name+" is not owned by a speaker or service, so it would stay lit after they leave");
                    Assert.AreEqual(LightShadows.None,light.shadows,zone+" practical casts shadows");
                    Assert.IsEmpty(light.GetComponents<Collider>(),zone+" practical has a collider");
                }
            }
        }
    }
}
