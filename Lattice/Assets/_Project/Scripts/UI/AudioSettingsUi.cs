using System;
using System.Collections.Generic;
using Lattice.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    public static class AudioSettingsUi
    {
        public static void Add(Transform parent,Vector2 first,float width,float spacing,List<Selectable> controls)
        {
            int row=0;
            foreach(AudioBus channel in Enum.GetValues(typeof(AudioBus)))
            {
                var bus=channel;var center=first+Vector2.down*(row++*spacing);
                var label=UiKit.Text(parent,"SoundLabel_"+bus,"",24,UiKit.TextColor,TextAlignmentOptions.Left);
                UiKit.Rect(label.gameObject,new(.5f,.5f),new(.5f,.5f),center+new Vector2(-width*.32f,0),new(width*.34f,50));
                float initial=AudioMix.Current.Level(bus);AudioChannelFocus focus=null;
                var slider=UiKit.Slider(parent,"Sound_"+bus,initial,value=>{AudioMix.Current.SetLevel(bus,value);focus.SetValue(value);});
                focus=slider.gameObject.AddComponent<AudioChannelFocus>();focus.Bind(label,bus,initial);
                UiKit.Rect(slider.gameObject,new(.5f,.5f),new(.5f,.5f),center+new Vector2(width*.18f,0),new(width*.62f,42));
                controls.Add(slider);
            }
        }
    }
}
