using System.Collections.Generic;
using Lattice.Core;
using Lattice.Rpg;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>The draught and the meal the party is under, each a word and its seconds, at the right of the vitals' own rows: the draught beside Integrity, the meal beside Charge.
    /// A buff that can't be seen running is one nobody knows has run out. They're a canvas of their own over the vitals, as the pickup feed is under them, so the HUD's panel isn't rebuilt
    /// around them; they hide with it whenever input is held.</summary>
    public sealed class BuffMarks:MonoBehaviour
    {
        Canvas canvas;readonly TMP_Text[] marks=new TMP_Text[2];
        /// <summary>What's showing, for a check: "SURGE 42s".</summary>
        public IEnumerable<string> Shown{get{foreach(var mark in marks)if(mark!=null&&mark.text!="")yield return mark.text;}}
        void Start()
        {
            canvas=UiKit.CreateCanvas("BuffMarks",6,transform);
            // The same rows and the same right edge as the vitals' labels (GameHud: 470 wide at 297, on 976 and 933 of 1080), read from the other end.
            for(int i=0;i<marks.Length;i++)
            {
                marks[i]=UiKit.Legible(UiKit.Text(canvas.transform,i==0?"Draught":"Meal","",24,UiKit.GoldColor,TextAlignmentOptions.Right));
                marks[i].raycastTarget=false;
                UiKit.Rect(marks[i].gameObject,new(0,1),new(0,1),new(297,(i==0?976:933)-1080),new(470,30));
            }
        }
        void Update()
        {
            if(canvas==null)return;
            var services=GameServices.Current;bool up=services!=null&&services.State!=null&&(GameInput.Current==null||!GameInput.Current.Blocked);
            canvas.enabled=up;if(!up)return;
            var state=services.State;
            Show(0,state.draughtLeft>0?state.draught:null,state.draughtLeft);Show(1,state.mealLeft>0?state.meal:null,state.mealLeft);
        }
        readonly string[] shownId=new string[2];readonly int[] shownSeconds={-2,-2};
        /// <summary>A mark is written again only when its second turns over or its item changes: with nothing running, a frame here costs two comparisons and no words.</summary>
        void Show(int slot,string id,float left)
        {
            if(string.IsNullOrEmpty(id))id=null;
            int seconds=id==null?-1:Mathf.CeilToInt(left);
            if(shownId[slot]==id&&shownSeconds[slot]==seconds)return;
            shownId[slot]=id;shownSeconds[slot]=seconds;
            var def=id==null?null:GameCatalog.Find<Lattice.Data.ConsumableDef>(id);
            marks[slot].text=def==null?"":(string.IsNullOrEmpty(def.mark)?def.displayName:def.mark).ToUpperInvariant()+" "+seconds+"s";
        }
    }
}
