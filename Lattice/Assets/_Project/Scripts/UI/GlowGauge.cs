using Lattice.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>
    /// The lead ship's glow (<see cref="ShipGlow"/>), under the vitals, only where something has lit it (the Spore Veil, whose rule is that visibility is the resource): a bar that fills as
    /// the ship fires, boosts and uses skills and drains as it flies dark, its words saying what that means ("LIT  ·  SWARMS COME TO IT", "DARK"). It never puts a glow on a ship itself,
    /// so in any other place it stays hidden. Built into the HUD's canvas by GameHud, so it hides with the rest of the HUD.
    /// </summary>
    public sealed class GlowGauge:MonoBehaviour
    {
        const float Width=400;
        /// <summary>Maru's count: "go dark and count two".</summary>
        public const float DarkAfter=2;
        static readonly Color Rising=new(.5f,.86f,.64f),Blazing=new(.86f,1f,.9f),Back=new(.025f,.045f,.065f,.85f);
        GameObject root;Image fill;TMP_Text words;
        /// <summary>What the bar measures and what that means now. 'GLOW   DARK' over an empty bar read as a placeholder (s23 166): it says whose glow it is and what being dark buys.
        /// At the type floor and inside the frame with air to spare: 'SHIP'S GLOW   DARK  ·  SWARMS LOSE YOU' was shrunk under the floor and still ran into the frame's right edge
        /// (s23 170, 177).</summary>
        public static string Words(ShipGlow glow)=>glow.IsLit?"SHIP LIT  ·  SWARMS COME TO IT":Time.time-glow.LastLit>=DarkAfter?"SHIP DARK  ·  SWARMS LOSE YOU":"SHIP FADING  ·  COUNT TWO";
        /// <summary>Whether it is up, and its lower edge in units down from the top of the screen: the pickup feed hangs under it.</summary>
        public bool Showing=>root!=null&&root.activeSelf;
        public float Foot{get{var rect=(RectTransform)root.transform;return -(rect.anchoredPosition.y-rect.sizeDelta.y*.5f);}}
        /// <param name="vitalsFoot">The vitals frame's lower edge, down from the top of the screen (GameHud.VitalsFoot): the gauge hangs six under it.</param>
        public void Build(Transform canvas,float vitalsFoot)
        {
            root=new GameObject("GlowGauge",typeof(RectTransform));root.transform.SetParent(canvas,false);
            UiKit.Rect(root,new Vector2(0,1),new Vector2(0,1),new Vector2(277,-(vitalsFoot+6+41)),new Vector2(524,82));
            var frame=UiKit.DarkFrame(root.transform,"GlowFrame");frame.raycastTarget=false;frame.pixelsPerUnitMultiplier=2;UiKit.Rect(frame.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).sizeDelta=Vector2.zero;
            words=UiKit.Legible(UiKit.Text(root.transform,"GlowLabel","SHIP'S GLOW",24,UiKit.LooseTextColor,TextAlignmentOptions.Left));
            UiKit.Rect(words.gameObject,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(10,14),new Vector2(470,30));
            // One line, whatever it says, at the type floor: the words are written to fit at 24 (a shrink to 21 was a net under words that didn't, and they still ran to the frame).
            words.textWrappingMode=TextWrappingModes.NoWrap;words.enableAutoSizing=false;words.fontSize=24;
            var back=UiKit.Panel(root.transform,"GlowBack",Back);back.raycastTarget=false;UiKit.Rect(back.gameObject,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-15,-17),new Vector2(Width,14));
            fill=UiKit.Panel(back.transform,"Glow",Rising);fill.raycastTarget=false;var rect=UiKit.Rect(fill.gameObject,new Vector2(0,.5f),new Vector2(0,.5f),Vector2.zero,new Vector2(0,14));rect.pivot=new Vector2(0,.5f);
            root.SetActive(false);
        }
        void LateUpdate()
        {
            if(root==null)return;
            var party=PartyController.Current;var lead=party!=null?party.Active:null;
            ShipGlow glow=null;bool show=lead!=null&&lead.flight&&lead.TryGetComponent(out glow)&&glow.isActiveAndEnabled;
            if(root.activeSelf!=show)root.SetActive(show);
            if(!show)return;
            fill.rectTransform.sizeDelta=new Vector2(Width*glow.Level,14);
            fill.color=glow.IsLit?Blazing:Rising;
            string said=Words(glow);if(words.text!=said)words.text=said;
        }
    }
}
