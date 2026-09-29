using System.Collections;
using System.Linq;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Lattice.Tests.PlayMode
{
    public sealed class AudioSettingsUiTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
            Assert.IsNotNull(AudioMix.Current.SettingsFile,"audio UI tests require the runner's isolated settings path");
            foreach(AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))AudioMix.Current.SetLevel(bus,.6f,false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Paused=false;SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Move(Selectable target,MoveDirection direction)
        {
            EventSystem.current.SetSelectedGameObject(target.gameObject);
            ExecuteEvents.Execute(target.gameObject,new AxisEventData(EventSystem.current){moveDir=direction},ExecuteEvents.moveHandler);
        }
        static void CheckChannels(Slider[] sliders)
        {
            Assert.AreEqual(5,sliders.Length);
            foreach(AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))
            {
                var slider=sliders.Single(s=>s.name=="Sound_"+bus);float before=AudioMix.Current.Level(bus);
                Move(slider,MoveDirection.Left);
                Assert.AreSame(slider.gameObject,EventSystem.current.currentSelectedGameObject,"left escaped to a tab instead of reducing the selected gain");
                Assert.Less(AudioMix.Current.Level(bus),before);
                Move(slider,MoveDirection.Right);Assert.AreEqual(before,AudioMix.Current.Level(bus),.0001);
            }
        }
        [UnityTest] public IEnumerator TitleHasFiveLabelledIndependentControlsAndRestoresItsFocus()
        {
            var title=Object.FindFirstObjectByType<TitleScreen>();title.OpenSettings();yield return null;
            var sliders=title.GetComponentsInChildren<Slider>();CheckChannels(sliders);
            foreach(AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))
                Assert.IsTrue(title.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="SoundLabel_"+bus&&t.text.Contains("60%")),"missing readable percent: "+bus);
            Move(sliders.Single(s=>s.name=="Sound_Music"),MoveDirection.Left);
            Assert.AreEqual(.6f,AudioMix.Current.Level(AudioBus.Master),.0001);Assert.AreEqual(.6f,AudioMix.Current.Level(AudioBus.SFX),.0001);
            title.CloseSettings();yield return null;
            Assert.AreEqual("Settings",EventSystem.current.currentSelectedGameObject.name);
            var restored=new AudioPreferences(System.IO.Path.GetDirectoryName(AudioMix.Current.SettingsFile)).Load();
            Assert.Less(restored.music,restored.master);Assert.AreEqual(.6f,restored.master,.0001);
        }
        [UnityTest] public IEnumerator PausedSoundControlsCanDecreaseAndBindingsRemainReachable()
        {
            SceneFlow.Current.LoadZone("Hub_Decks");while(SceneFlow.Current.Loading)yield return null;yield return null;
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open(5);yield return null;
            CheckChannels(menu.GetComponentsInChildren<Slider>());Assert.IsTrue(GameTime.Paused);
            var binding=menu.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<TMP_Text>().text=="Keyboard bindings");EventSystem.current.SetSelectedGameObject(binding.gameObject);binding.onClick.Invoke();yield return null;
            Assert.AreEqual("Back to sound controls",EventSystem.current.currentSelectedGameObject.GetComponentInChildren<TMP_Text>().text,"subpage inherited a different indexed row's focus");
            Assert.AreEqual(0,menu.GetComponentsInChildren<Slider>().Length);
            Assert.AreEqual(6,menu.GetComponentsInChildren<Button>().Count(b=>b.GetComponentInChildren<TMP_Text>().text.StartsWith("Rebind keyboard:")));
            menu.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<TMP_Text>().text=="Back to sound controls").onClick.Invoke();yield return null;
            Assert.AreEqual(5,menu.GetComponentsInChildren<Slider>().Length);menu.Close();Assert.IsFalse(GameTime.Paused);
        }
        [UnityTest] public IEnumerator ShoulderTabFromEmptyQuestsSelectsAnAdjustableSoundControl()
        {
            SceneFlow.Current.LoadZone("Hub_Decks");while(SceneFlow.Current.Loading)yield return null;yield return null;
            GameServices.Current.State.questSteps.Clear();var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open(4);yield return null;
            Assert.AreEqual("Close",EventSystem.current.currentSelectedGameObject.name,"empty-page focus control changed");
            var pad=InputSystem.AddDevice<Gamepad>();
            try
            {
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.RightShoulder));yield return null;yield return null;
                InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
                Assert.AreEqual(5,menu.GetComponentsInChildren<Slider>().Length,"ordinary shoulder input did not change the tab");
                Assert.AreEqual("Sound_Master",EventSystem.current.currentSelectedGameObject.name,"new tab retained the old page's Back control");
                float before=AudioMix.Current.Level(AudioBus.Master);
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadLeft));yield return null;yield return null;
                InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
                Assert.Less(AudioMix.Current.Level(AudioBus.Master),before,"ordinary left did not adjust gain");
            }
            finally{InputSystem.RemoveDevice(pad);menu.Close();}
        }
    }
}
