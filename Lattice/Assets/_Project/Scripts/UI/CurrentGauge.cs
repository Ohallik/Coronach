using Lattice.Combat;
using Lattice.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>
    /// What the aurora's current is doing to the lead ship (<see cref="AuroraCurrent"/>), low on the left over the prompt line, only where there are currents (the Aurora Shear, whose rule is riding
    /// them): a bar from the middle that runs right in the current's teal while the ship rides with it (Thrust filling) and left in amber while it flies against it (Thrust
    /// draining), and its words saying which. Out of every current it says so and stays empty. Anywhere without currents it is hidden. Built into the HUD's canvas by GameHud.
    /// </summary>
    public sealed class CurrentGauge:MonoBehaviour
    {
        const float Width=400;
        static readonly Color With=new(.42f,1f,.86f),Against=new(1f,.7f,.32f),Back=new(.025f,.045f,.065f,.85f),Middle=new(.8f,.86f,.9f,.6f);
        GameObject root;Image fill;TMP_Text words;
        /// <summary>Whether it is up, and the panel itself: a bark come down its column stops over it.</summary>
        public bool Showing=>root!=null&&root.activeSelf;
        public RectTransform Box=>root!=null?(RectTransform)root.transform:null;
        public static string Words(AuroraCurrent.Riding riding)=>riding switch
        {
            AuroraCurrent.Riding.With=>"CURRENT WITH YOU · THRUST FILLING",
            AuroraCurrent.Riding.Against=>"CURRENT AGAINST · THRUST DRAINING",
            AuroraCurrent.Riding.Across=>"CURRENT ACROSS IT · CARRIED",
            _=>"NO CURRENT",
        };
        public void Build(Transform canvas)
        {
            root=new GameObject("CurrentGauge",typeof(RectTransform));root.transform.SetParent(canvas,false);
            // Low on the left, over the prompt line and clear of a boss bar's band. It stood right under the vitals, which is where the pickup feed's lines and "Saved" are
            // written: in the first player run every kill's loot was printed across it (November 14). Held from the bottom, so a taller screen keeps it by the prompt line.
            UiKit.Rect(root,Vector2.zero,Vector2.zero,new Vector2(315,290),new Vector2(600,82));
            var frame=UiKit.DarkFrame(root.transform,"CurrentFrame");frame.raycastTarget=false;frame.pixelsPerUnitMultiplier=2;UiKit.Rect(frame.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).sizeDelta=Vector2.zero;
            words=UiKit.Legible(UiKit.Text(root.transform,"CurrentLabel","CURRENT",24,UiKit.LooseTextColor,TextAlignmentOptions.Left));
            UiKit.Rect(words.gameObject,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,14),new Vector2(550,30));
            // One line, whatever it says: in a 470-wide box the longest of them wrapped onto the bar under it. The words are cut to fit at 24; the shrink is only a net under them.
            words.textWrappingMode=TextWrappingModes.NoWrap;words.enableAutoSizing=true;words.fontSizeMax=24;words.fontSizeMin=22;
            var back=UiKit.Panel(root.transform,"CurrentBack",Back);back.raycastTarget=false;UiKit.Rect(back.gameObject,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-53,-17),new Vector2(Width,14));
            var middle=UiKit.Panel(back.transform,"CurrentMiddle",Middle);middle.raycastTarget=false;UiKit.Rect(middle.gameObject,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(3,20));
            fill=UiKit.Panel(back.transform,"Current",With);fill.raycastTarget=false;var rect=UiKit.Rect(fill.gameObject,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(0,14));rect.pivot=new Vector2(0,.5f);
            root.SetActive(false);
        }
        void LateUpdate()
        {
            if(root==null)return;
            var party=PartyController.Current;var lead=party!=null&&party.members!=null&&party.members.Length>0?party.Active:null;
            bool show=lead!=null&&lead.flight&&AuroraCurrent.All.Count>0;
            if(root.activeSelf!=show)root.SetActive(show);
            if(!show)return;
            var riding=AuroraCurrent.Of(lead,out var current);float strength=0;
            if(current!=null)current.Flow(lead.transform.position,out _,out strength);
            float share=riding==AuroraCurrent.Riding.With?strength:riding==AuroraCurrent.Riding.Against?-strength:0;
            var rect=fill.rectTransform;rect.pivot=new Vector2(share>=0?0:1,.5f);rect.sizeDelta=new Vector2(Width*.5f*Mathf.Abs(share),14);
            fill.color=share>=0?With:Against;
            string said=Words(riding);if(words.text!=said)words.text=said;
        }
    }
}
