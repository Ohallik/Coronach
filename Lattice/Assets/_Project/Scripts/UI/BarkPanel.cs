using Lattice.Core;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    public sealed class BarkPanel:MonoBehaviour
    {
        TMP_Text text;float until;
        void Start()
        {
            var canvas=UiKit.CreateCanvas("Barks",8,transform);text=UiKit.Text(canvas.transform,"Bark","",25,UiKit.TextColor);
            UiKit.Rect(text.gameObject,new(.5f,0),new(.5f,0),new(0,285),new(1250,65));text.outlineWidth=.2f;text.outlineColor=new Color32(5,12,22,255);TextPlate.Attach(text,"BarkPlate",34,12);BarkService.Spoken+=Show;
        }
        void Show(string speaker,string line){text.text=DisplayNames.Speaker(speaker)+":  "+line;until=Time.unscaledTime+3.5f;}
        void Update(){if(text!=null&&(Time.unscaledTime>until||GameInput.Current.Blocked))text.text="";}
        void OnDestroy(){BarkService.Spoken-=Show;}
    }
}
