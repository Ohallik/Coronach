using Lattice.Core;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    public sealed class BarkPanel:MonoBehaviour
    {
        TMP_Text text;float until;
        static readonly string NameColor=ColorUtility.ToHtmlStringRGB(UiKit.GoldColor);
        /// <summary>A line isn't worth hiding the party for.</summary>
        public PanelFade Fade{get;private set;}
        void Start()
        {
            var canvas=UiKit.CreateCanvas("Barks",8,transform);text=UiKit.Legible(UiKit.Text(canvas.transform,"Bark","",25,UiKit.LooseTextColor,TextAlignmentOptions.TopLeft));
            // Under the vitals and the pickup feed's five lines, in a column of its own. Across the middle at y 285 it cut the ship in half, in the very band a fight flies in.
            UiKit.Rect(text.gameObject,new(0,1),new(0,1),new(292,-575),new(460,170));var plate=TextPlate.Attach(text,"BarkPlate",34,16);BarkService.Spoken+=Show;
            var group=canvas.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;Fade=new PanelFade(plate.Frame.rectTransform,plate.Frame,group,PanelFade.Party);
        }
        void Show(string speaker,string line){text.text=$"<color=#{NameColor}>{DisplayNames.Speaker(speaker)}</color>\n{line}";until=Time.unscaledTime+3.5f;}
        void Update(){if(text!=null&&(Time.unscaledTime>until||GameInput.Current.Blocked))text.text="";}
        void OnDestroy(){BarkService.Spoken-=Show;}
    }
}
