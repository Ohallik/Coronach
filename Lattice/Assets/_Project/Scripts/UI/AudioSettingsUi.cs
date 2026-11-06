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
        /// <summary>What a slider plays as it moves, so a level is heard rather than guessed from a number. It answers the slider's change only, so opening a page is silent.</summary>
        static string Cue(AudioBus bus)=>bus switch{AudioBus.Music=>"hero_revive",AudioBus.SFX=>"impactMetal_000",AudioBus.Ambience=>"forceField_000",_=>"ui_confirm"};
        public static void Add(Transform parent,Vector2 first,float width,float spacing,List<Selectable> controls)
        {
            int row=0;
            foreach(AudioBus channel in Enum.GetValues(typeof(AudioBus)))
            {
                var bus=channel;var center=first+Vector2.down*(row++*spacing);
                var label=UiKit.Text(parent,"SoundLabel_"+bus,"",24,UiKit.TextColor,TextAlignmentOptions.Left);
                UiKit.Rect(label.gameObject,new(.5f,.5f),new(.5f,.5f),center+new Vector2(-width*.32f,0),new(width*.34f,50));
                float initial=AudioMix.Current.Level(bus);AudioChannelFocus focus=null;
                var slider=UiKit.Slider(parent,"Sound_"+bus,initial,value=>{AudioMix.Current.SetLevel(bus,value);focus.SetValue(value);AudioManager.Sample(Cue(bus),bus);});
                focus=slider.gameObject.AddComponent<AudioChannelFocus>();focus.Bind(label,bus,initial);
                UiKit.Rect(slider.gameObject,new(.5f,.5f),new(.5f,.5f),center+new Vector2(width*.18f,0),new(width*.62f,42));
                controls.Add(slider);
            }
        }
    }
}
