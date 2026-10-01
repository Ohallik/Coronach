using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>C6: a hint names the buttons of the device the player last touched,
    /// and none of the other device's, on every surface that shows one, including
    /// when the device changes while the surface is open.</summary>
    public sealed class DeviceHintTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);}
        [UnityTearDown] public IEnumerator Cleanup()
        {PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}

        static PromptDevice Other(PromptDevice d)=>d==PromptDevice.Gamepad?PromptDevice.Keyboard:PromptDevice.Gamepad;
        // A glyph is a sprite from the prompt sheet or a word of its plain label.
        static HashSet<string> Glyphs(string text)
        {
            var set=new HashSet<string>();
            foreach(Match m in Regex.Matches(text,"<sprite name=\"([^\"]+)\">"))set.Add("sprite:"+m.Groups[1].Value);
            foreach(var w in Regex.Replace(text,"<[^>]+>"," ").Split(new[]{' ','/','·','.','+','\n'},StringSplitOptions.RemoveEmptyEntries))set.Add(w.ToUpperInvariant());
            return set;
        }
        // The prompt service's own rendering of a hint source under a given device.
        static string Under(PromptDevice device,Func<string> source)
        {var current=PromptService.Device;PromptService.ForceDevice(device);string s=source();PromptService.ForceDevice(current);return s;}
        static void Speaks(string surface,string text,PromptDevice device,params Func<string>[] sources)
        {
            var own=new HashSet<string>();var other=new HashSet<string>();
            foreach(var source in sources){own.UnionWith(Glyphs(Under(device,source)));other.UnionWith(Glyphs(Under(Other(device),source)));}
            var shared=own.Intersect(other).ToList();own.ExceptWith(shared);other.ExceptWith(shared);
            Assert.IsNotEmpty(own,$"{surface}: the two devices render identically, so the check cannot tell them apart");
            var glyphs=Glyphs(text);
            Assert.IsTrue(own.IsSubsetOf(glyphs),$"{surface} on {device} lacks {string.Join(", ",own.Except(glyphs))}: \"{text}\"");
            Assert.IsEmpty(other.Intersect(glyphs).ToList(),$"{surface} on {device} also names the other device: \"{text}\"");
        }
        static TMP_Text Named(Component root,string name)
        {var t=root.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(c=>c.name==name);Assert.IsNotNull(t,"missing "+name);return t;}
        // The runtime object carries every UI; look inside one surface's own canvas or row.
        static Transform Within(Component root,string name)
        {var t=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(c=>c.name==name);Assert.IsNotNull(t,"missing "+name);return t;}

        [UnityTest] public IEnumerator TheTitleHintSpeaksTheActiveDevice()
        {
            var title=UnityEngine.Object.FindFirstObjectByType<TitleScreen>();Assert.IsNotNull(title);
            foreach(var device in new[]{PromptDevice.Gamepad,PromptDevice.Keyboard})
            {
                PromptService.ForceDevice(device);yield return null;
                Speaks("Title",Named(title,"Hint").text,device,PromptService.SubmitTag,
                    ()=>PromptService.Device==PromptDevice.Gamepad?PromptService.TagMoveVertical():PromptService.Label("Move","up")+" "+PromptService.Label("Move","down"));
            }
        }

        [UnityTest] public IEnumerator DialogueShopAndPauseHintsSpeakTheActiveDevice()
        {
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Hub_Decks");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);yield return new WaitForSecondsRealtime(.3f);
            var panel=UnityEngine.Object.FindFirstObjectByType<DialoguePanel>();var shop=UnityEngine.Object.FindFirstObjectByType<ShopUi>();var pause=UnityEngine.Object.FindFirstObjectByType<PauseMenu>();
            Func<string> interact=()=>PromptService.Tag("Interact"),prev=()=>PromptService.Tag("MenuPrev"),next=()=>PromptService.Tag("MenuNext");
            foreach(var device in new[]{PromptDevice.Gamepad,PromptDevice.Keyboard})
            {
                PromptService.ForceDevice(device);yield return null;
                panel.Show("Orrin",PortraitEmotion.Neutral,null,"The dock office is this way.");yield return null;
                var hint=Named(Within(panel,"Dialogue"),"Continue");
                if(device==PromptDevice.Gamepad)Speaks("Dialogue",hint.text,device,PromptService.SubmitTag);
                else Speaks("Dialogue",hint.text,device,PromptService.SubmitTag,interact);
                // Touch the other device with the line still open: the hint follows.
                PromptService.ForceDevice(Other(device));yield return null;
                Speaks("Dialogue after a device change",hint.text,Other(device),PromptService.SubmitTag);
                PromptService.ForceDevice(device);yield return null;panel.Hide();

                shop.Open(null);yield return null;
                Speaks("Shop",Named(Within(Within(shop,"Shop"),"ShopRow0"),"Label").text,device,prev,next);
                shop.Close();yield return null;

                pause.Open(5);yield return null;
                Speaks("Pause sound settings",Named(Within(pause,"Menus"),"Note").text,device,prev,next);
                pause.Close();yield return null;
            }
        }
    }
}
