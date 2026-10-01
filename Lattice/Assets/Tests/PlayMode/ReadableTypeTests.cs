using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>C6/C7: no visible text on any surface renders under 16 px at the
    /// smallest supported screen, 1280×720, through its canvas scaler.</summary>
    public sealed class ReadableTypeTests
    {
        const float Floor=16;
        [UnitySetUp] public IEnumerator Boot()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);}
        [UnityTearDown] public IEnumerator Cleanup()
        {PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}

        static float Scale(CanvasScaler scaler,int w,int h)
        {var r=scaler.referenceResolution;return Mathf.Pow(2,Mathf.Lerp(Mathf.Log(w/r.x,2),Mathf.Log(h/r.y,2),scaler.matchWidthOrHeight));}
        static void Audit(string surface,List<string> failures)
        {
            foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                // InputProbe is the development build's input diagnostic, not player-facing.
                if(!text.isActiveAndEnabled||string.IsNullOrWhiteSpace(text.text)||text is TextMeshPro||text.name=="InputProbe")continue;
                var canvas=text.canvas!=null?text.canvas.rootCanvas:null;var scaler=canvas!=null?canvas.GetComponent<CanvasScaler>():null;
                if(scaler==null||scaler.uiScaleMode!=CanvasScaler.ScaleMode.ScaleWithScreenSize)continue;
                float size=text.enableAutoSizing?text.fontSizeMin:text.fontSize;
                float px=size*Scale(scaler,1280,720);
                string sample=text.text.Replace("\n"," ").Substring(0,Mathf.Min(40,text.text.Length));
                if(px<Floor-.01f)failures.Add($"{surface}: {canvas.name}/{text.name} renders at {px:0.0} px (\"{sample}\")");
                // Larger type must still fit its box (damage numbers float free of any box).
                var rect=text.rectTransform.rect;
                if(text.name!="Damage"&&rect.width>1)
                {
                    var need=text.GetPreferredValues(text.text,rect.width,0);
                    if(need.x>rect.width+1||need.y>rect.height+1)failures.Add($"{surface}: {canvas.name}/{text.name} overflows its {rect.width:0}x{rect.height:0} box, needing {need.x:0}x{need.y:0} (\"{sample}\")");
                }
            }
        }

        // The dark panel's artwork, measured from ui-panel.png at its 200 px/unit slice,
        // reaches 34 units in at the sides and 30 at the top, and its 9-slice band is
        // 42.5 deep at the bottom. Content keeps about 10 units clear of each.
        internal static class FrameClear{public const float Side=44,Top=40,Bottom=50;}
        static Rect Box(RectTransform rt){var c=new Vector3[4];rt.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y);}
        static void Inside(string frameName,string[] texts,string[] bars,List<string> failures)
        {
            var frame=Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).FirstOrDefault(r=>r.name==frameName);Assert.IsNotNull(frame,"missing "+frameName);
            // Look only inside the frame's own canvas: other surfaces reuse names like "Status".
            var all=frame.GetComponentInParent<Canvas>().rootCanvas.GetComponentsInChildren<RectTransform>(true);
            var outer=Box(frame);var scale=frame.lossyScale.x;
            void Check(string name,Vector3 lo,Vector3 hi)
            {
                float left=(lo.x-outer.xMin)/scale,right=(outer.xMax-hi.x)/scale,bottom=(lo.y-outer.yMin)/scale,top=(outer.yMax-hi.y)/scale;
                if(left<FrameClear.Side||right<FrameClear.Side||bottom<FrameClear.Bottom||top<FrameClear.Top)failures.Add($"{frameName}/{name} crowds the border ({left:0} left, {right:0} right, {bottom:0} bottom, {top:0} top)");
            }
            foreach(string name in texts)
            {
                var text=all.Select(r=>r.GetComponent<TMP_Text>()).FirstOrDefault(t=>t!=null&&t.name==name);Assert.IsNotNull(text,$"{frameName}: no text named {name}");
                text.ForceMeshUpdate();var g=text.textBounds;
                Assert.Greater(g.size.x,0,name+" has no glyph bounds; the border check cannot fail");
                Check(name,text.transform.TransformPoint(g.min),text.transform.TransformPoint(g.max));
            }
            foreach(string name in bars){var bar=all.FirstOrDefault(r=>r.name==name);Assert.IsNotNull(bar,$"{frameName}: no bar named {name}");var b=Box(bar);Check(name,new Vector3(b.xMin,b.yMin),new Vector3(b.xMax,b.yMax));}
        }

        [UnityTest] public IEnumerator EverySurfaceReadsAt720p()
        {
            var failures=new List<string>();
            // The audit must be able to fail: a 10-point line on a scaled canvas is caught.
            var probe=UiKit.CreateCanvas("ReadabilityProbe",99);UiKit.Text(probe.transform,"Probe","tiny print",10,Color.white);
            var crammed=UiKit.Text(probe.transform,"Crammed","far too many words for one small box",30,Color.white);UiKit.Rect(crammed.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(120,40));yield return null;
            var caught=new List<string>();Audit("probe",caught);Object.Destroy(probe.gameObject);
            Assert.IsTrue(caught.Any(c=>c.Contains("ReadabilityProbe/Probe")),"the audit cannot see a 10-point line");
            Assert.IsTrue(caught.Any(c=>c.Contains("ReadabilityProbe/Crammed")&&c.Contains("overflows")),"the audit cannot see an overflowing line");
            yield return null;Audit("title",failures);
            var title=Object.FindFirstObjectByType<TitleScreen>();title.OpenSettings();yield return null;Audit("title settings",failures);
            DevLoadout.Apply("starter");
            foreach(var device in new[]{PromptDevice.Keyboard,PromptDevice.Gamepad})
            {
                // The combat HUD adds skill icons and labels to the town HUD.
                PromptService.ForceDevice(device);SceneFlow.Current.LoadZone("Sorrel_Ridges");float wait=Time.unscaledTime+10;
                while(SceneFlow.Current.Loading&&Time.unscaledTime<wait)yield return null;
                Assert.IsFalse(SceneFlow.Current.Loading);yield return new WaitForSecondsRealtime(.5f);
                Audit("combat hud "+device,failures);
            }
            PromptService.ForceDevice(PromptDevice.Keyboard);
            SceneFlow.Current.LoadZone("Hub_Decks");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);yield return new WaitForSecondsRealtime(.5f);
            Audit("hud",failures);
            // Framed HUD content clears its frame's artwork (the dialogue panel's rule).
            Inside("VitalsFrame",new[]{"Status","IntegrityLabel","ChargeLabel","ThrustLabel","Partner"},new[]{"IntegrityBack","ChargeBack","ThrustBack"},failures);
            Inside("ObjectiveFrame",new[]{"Objective"},new string[0],failures);
            // The partner line is longest when the partner is down and waiting for help.
            var party=Lattice.Combat.PartyController.Current;var partner=party.members[1-party.index];
            partner.Health.Receive(new Lattice.Combat.DamagePacket{amount=partner.Health.maximum*10});yield return new WaitForSecondsRealtime(.3f);
            Assert.IsFalse(partner.Health.Alive,"the partner did not go down");
            Audit("hud, partner down",failures);
            Inside("VitalsFrame",new[]{"Partner"},new string[0],failures);
            partner.Health.Heal(partner.Health.maximum);yield return null;
            var panel=Object.FindFirstObjectByType<DialoguePanel>();panel.Show("Orrin",PortraitEmotion.Neutral,null,"The dock office is this way.");panel.SetVisibleCharacters(int.MaxValue);
            panel.Options(new List<string>{"Ask about Sorrel.","Leave."},_=>{});yield return null;Audit("dialogue",failures);panel.ClearOptions();panel.Hide();
            var shop=Object.FindFirstObjectByType<ShopUi>();shop.Open(null);yield return null;Audit("shop",failures);shop.Close();yield return null;
            var pause=Object.FindFirstObjectByType<PauseMenu>();
            for(int page=0;page<6;page++){pause.Open(page);yield return null;Audit("pause page "+page,failures);pause.Close();yield return null;}
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }
    }
}
