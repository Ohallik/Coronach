using Lattice.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Lattice.UI
{
    // Reuses the existing text/glyph skin; the selected channel is readable
    // without trying to distinguish tiny colour changes on a slider handle.
    public sealed class AudioChannelFocus:MonoBehaviour,ISelectHandler,IDeselectHandler
    {
        TMP_Text label;AudioBus bus;float value;bool selected;
        public void Bind(TMP_Text text,AudioBus channel,float initial)
        {label=text;bus=channel;SetValue(initial);}
        public void SetValue(float gain){value=gain;Refresh();}
        public void OnSelect(BaseEventData data){selected=true;Refresh();}
        public void OnDeselect(BaseEventData data){selected=false;Refresh();}
        void Refresh()
        {
            if(label==null)return;
            label.text=$"{(selected?"> ":"  ")}{bus}   {Mathf.RoundToInt(value*100)}%";
            label.color=selected?UiKit.GoldColor:UiKit.TextColor;
        }
    }
}
