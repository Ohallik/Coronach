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
        public static string Words(ShipGlow glow)=>glow.IsLit?"GLOW   LIT  ·  SWARMS COME TO IT":Time.time-glow.LastLit>=DarkAfter?"GLOW   DARK":"GLOW";
        public void Build(Transform canvas)
        {
            root=new GameObject("GlowGauge",typeof(RectTransform));root.transform.SetParent(canvas,false);
            UiKit.Rect(root,new Vector2(0,1),new Vector2(0,1),new Vector2(277,722)-new Vector2(0,1080),new Vector2(524,82));
            var frame=UiKit.DarkFrame(root.transform,"GlowFrame");frame.raycastTarget=false;frame.pixelsPerUnitMultiplier=2;UiKit.Rect(frame.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).sizeDelta=Vector2.zero;
            words=UiKit.Legible(UiKit.Text(root.transform,"GlowLabel","GLOW",24,UiKit.LooseTextColor,TextAlignmentOptions.Left));
            UiKit.Rect(words.gameObject,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(10,14),new Vector2(470,30));
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
