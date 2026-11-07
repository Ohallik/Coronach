using Lattice.Core;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    public sealed class BarkPanel:MonoBehaviour
    {
        TMP_Text text;float until;Canvas canvas;
        static readonly string NameColor=ColorUtility.ToHtmlStringRGB(UiKit.GoldColor);
        /// <summary>A line isn't worth hiding the party for.</summary>
        public PanelFade Fade{get;private set;}
        void Start()
        {
            canvas=UiKit.CreateCanvas("Barks",8,transform);text=UiKit.Legible(UiKit.Text(canvas.transform,"Bark","",25,UiKit.LooseTextColor,TextAlignmentOptions.TopLeft));
            // Under the vitals and the pickup feed's five lines, in a column of its own. Across the middle at y 285 it cut the ship in half, in the very band a fight flies in.
            UiKit.Rect(text.gameObject,new(0,1),new(0,1),new(292,-575),new(460,170));var plate=TextPlate.Attach(text,"BarkPlate",34,16);BarkService.Spoken+=Show;
            var group=canvas.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;Fade=new PanelFade(plate.Frame.rectTransform,plate.Frame,group,PanelFade.Party);
        }
        void Show(string speaker,string line){text.text=$"<color=#{NameColor}>{DisplayNames.Speaker(speaker)}</color>\n{line}";until=Time.unscaledTime+3.5f;}
        /// <summary>A menu or a conversation hides the line and holds its time. Wiped, it was gone for good, and a hail counts as heard from its first line: Orrin's warning before a one-way warp could be lost to a pause.</summary>
        void Update()
        {
            if(text==null)return;
            bool blocked=GameInput.Current.Blocked;if(canvas.enabled==blocked)canvas.enabled=!blocked;
            if(blocked){until+=Time.unscaledDeltaTime;return;}
            if(Time.unscaledTime>until&&text.text.Length>0)text.text="";
        }
        void OnDestroy(){BarkService.Spoken-=Show;}
    }
}
