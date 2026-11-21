using System;
using System.Collections.Generic;
using Lattice.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>The four choices about how a hit lands (<see cref="ImpactSettings"/>), as rows that step through their values when pressed. The title's settings and the
    /// pause menu's both draw from here, so the words and the order are the same in both.</summary>
    public static class ImpactSettingsUi
    {
        static string Shake()=>ImpactSettings.Shake switch{ImpactSettings.ShakeLevel.Full=>"Full",ImpactSettings.ShakeLevel.Reduced=>"Reduced",_=>"Off"};
        /// <summary>Each row's name, what it says now, and what a press does. A press always lands on the next value, and the last comes round to the first.</summary>
        public static readonly (string name,Func<string> says,Action next)[] Rows=
        {
            ("Feel_Shake",()=>"Screen shake  ·  "+Shake(),()=>ImpactSettings.Shake=ImpactSettings.Shake switch
                {ImpactSettings.ShakeLevel.Full=>ImpactSettings.ShakeLevel.Reduced,ImpactSettings.ShakeLevel.Reduced=>ImpactSettings.ShakeLevel.Off,_=>ImpactSettings.ShakeLevel.Full}),
            ("Feel_HitPause",()=>"Hit pause  ·  "+(ImpactSettings.HitStop?"On":"Off"),()=>ImpactSettings.HitStop=!ImpactSettings.HitStop),
            ("Feel_Flashes",()=>"Flashes  ·  "+(ImpactSettings.FullFlashes?"Full":"Reduced"),()=>ImpactSettings.FullFlashes=!ImpactSettings.FullFlashes),
            ("Feel_Rumble",()=>"Controller rumble  ·  "+(ImpactSettings.Rumble?"On":"Off"),()=>ImpactSettings.Rumble=!ImpactSettings.Rumble),
        };
        /// <summary>What the four are for, in a line.</summary>
        public const string Note="How a hit lands: the camera's kick, the beat the game holds on a heavy blow, how brightly bodies and the screen's edge flash, and the pad's motors.";
        /// <summary>The rows as a column of buttons that restate themselves in place, for a page that isn't rebuilt on a press.</summary>
        public static void Add(Transform parent,Vector2 first,float width,float spacing,List<Selectable> controls)
        {
            for(int i=0;i<Rows.Length;i++)
            {
                var row=Rows[i];Button button=null;
                button=UiKit.Row(parent,row.name,row.says(),()=>{row.next();button.GetComponentInChildren<TMP_Text>().text=row.says();},26);
                UiKit.Rect(button.gameObject,new(.5f,.5f),new(.5f,.5f),first+Vector2.down*(i*spacing),new(width,spacing-8));
                controls.Add(button);
            }
        }
    }
}
