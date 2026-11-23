using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>The one list the shop, Gear, Party, Fabricate and the quest log share: rows laid out in columns that line up, a kind mark at the left, headings over the columns,
    /// and the controls that page and filter it standing at the panel's edge as controls. Every row was one centred string on an identical bar ("Scrap Emitter · Output +8 — 60 scrip
    /// · slot empty"), with "Next page (1/2)" and "Back" among the goods, and a reviewer of the first capture called the first shop a debug list (November 20).</summary>
    public static class MenuList
    {
        /// <summary>A column: where it stands in the row (from the row's left edge), how its words sit, and the heading over it. Line 0 is the row's whole height; a two-line row
        /// (the quest log) puts a column on line 1, its upper half, or line 2, its lower.</summary>
        public readonly struct Column
        {
            public readonly string name,heading;public readonly float left,width,size;public readonly TextAlignmentOptions align;public readonly Color color;public readonly int line;
            public Column(string name,float left,float width,string heading="",TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft,float size=24,Color? color=null,int line=0)
            {this.name=name;this.left=left;this.width=width;this.heading=heading;this.align=align;this.size=size;this.color=color??UiKit.TextColor;this.line=line;}
            public float Right=>left+width;
        }
        /// <summary>Where a row's kind mark stands: clear of the selection bar, which is 16 to 22 in (a selected blade and the bar merged into an L).</summary>
        public const float MarkAt=52,MarkSize=44,FirstColumn=88;
        /// <summary>Words that say a thing is better than what's worn, and worse.</summary>
        public const string Better="#9BE8A6",Worse="#FFB08A";
        public static string[] Say(params string[] wordings)=>wordings;

        /// <summary>A row of the list. <paramref name="cells"/> holds each column's words: the first that fits its column is drawn, so a cell gives way to a leaner wording
        /// and the strip under the list says the rest. <paramref name="key"/> is what the row is found by (ListRow).</summary>
        /// <param name="spread">How far a two-line row's lines stand from its middle: 12 for the quest log's, 15 for the bench's, whose recipe name sat on the line under it.</param>
        public static Button Row(Transform parent,string name,Vector2 centre,Vector2 size,IList<Column> columns,IList<string[]> cells,Action onClick,string key,ItemMarks.Kind mark=ItemMarks.Kind.None,float spread=12)
        {
            var row=UiKit.Row(parent,name,"",onClick,columns.Count>0?columns[0].size:24);UiKit.Rect(row.gameObject,new(.5f,.5f),new(.5f,.5f),centre,size);
            // The row's own label is its first column, so the selection's gold falls on the name.
            var first=row.GetComponentInChildren<TMP_Text>();var texts=new TMP_Text[columns.Count];
            for(int i=0;i<columns.Count;i++)
            {
                var c=columns[i];var text=i==0?first:UiKit.Text(row.transform,c.name,"",c.size,c.color,c.align);
                text.name=c.name;text.fontSize=c.size;text.color=c.color;text.alignment=c.align;text.margin=Vector4.zero;text.richText=true;
                text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Ellipsis;
                var rect=text.rectTransform;rect.anchorMin=new(0,c.line==2?0:c.line==1?.5f:0);rect.anchorMax=new(0,c.line==1?1:c.line==2?.5f:1);rect.pivot=new(0,.5f);
                // A two-line row's lines stand together about its middle, 12 units each side, inside the bevel's inner line, which takes ten units of the row's top and bottom.
                rect.anchoredPosition=new(c.left,c.line==1?spread-size.y*.25f:c.line==2?size.y*.25f-spread:0);rect.sizeDelta=new(c.width,0);
                var wordings=i<cells.Count?cells[i]:null;text.text=wordings!=null&&wordings.Length>0?wordings[0]??"":"";
                // The first wording that fits its column; past the leanest, a column set over the type floor comes down to it before anything is cut.
                bool Fits(){if(wordings==null)return true;foreach(string words in wordings){if(string.IsNullOrEmpty(words))continue;text.text=words;if(text.GetPreferredValues(words).x<=c.width)return true;}return false;}
                if(!Fits()&&c.size>24){text.fontSize=24;Fits();}
                texts[i]=text;
            }
            if(mark!=ItemMarks.Kind.None)Mark(row.transform,mark,new Vector2(MarkAt,0),MarkSize);
            var list=row.gameObject.AddComponent<ListRow>();list.key=key;list.columns=columns.Select(c=>c.name).ToArray();list.cells=texts;
            return row;
        }
        /// <summary>A kind's mark, named "Item" as a row's picture always was: a drawn line stands on a plate of its own, as the sheet's pictures stand on theirs.</summary>
        public static Image Mark(Transform parent,ItemMarks.Kind kind,Vector2 at,float size,bool left=true)
        {
            var (sprite,tint)=ItemMarks.For(kind);
            if(ItemMarks.Drawn(kind))
            {
                var plate=UiKit.Panel(parent,"ItemPlate",new Color32(10,18,30,255));plate.raycastTarget=false;
                UiKit.Rect(plate.gameObject,left?new(0,.5f):new(.5f,.5f),left?new(0,.5f):new(.5f,.5f),at,Vector2.one*size);
            }
            var image=UiKit.Panel(parent,"Item",sprite!=null?tint:Color.clear);image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
            UiKit.Rect(image.gameObject,left?new(0,.5f):new(.5f,.5f),left?new(0,.5f):new(.5f,.5f),at,Vector2.one*(ItemMarks.Drawn(kind)?size*.86f:size));
            return image;
        }
        /// <summary>A picture of the row's own at this place from its left edge (a skill's icon, as the HUD draws it), named "Item" as a row's picture always is.</summary>
        public static Image Picture(Transform row,Sprite sprite,float at,float size)
        {
            var image=UiKit.Panel(row,"Item",sprite!=null?Color.white:Color.clear);image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
            UiKit.Rect(image.gameObject,new(0,.5f),new(0,.5f),new(at+0f,0),Vector2.one*size);return image;
        }
        /// <summary>How far along something is (a skill's mastery to its next level, the EXTRA's gauge): a dark track with a gold fill, this wide, from this place in the row.</summary>
        public static Image Bar(Transform row,float left,float width,float share)
        {
            var track=UiKit.Panel(row,"BarTrack",new Color32(4,10,20,255));track.raycastTarget=false;var rect=track.rectTransform;
            rect.anchorMin=rect.anchorMax=new(0,.5f);rect.pivot=new(0,.5f);rect.anchoredPosition=new(left,0);rect.sizeDelta=new(width,10);
            var fill=UiKit.Panel(track.transform,"BarFill",UiKit.GoldColor);fill.raycastTarget=false;var inner=fill.rectTransform;
            inner.anchorMin=Vector2.zero;inner.anchorMax=new(Mathf.Clamp01(share),1);inner.offsetMin=new(2,2);inner.offsetMax=new(share>=.999f?-2:0,-2);
            fill.enabled=share*width>4;
            return fill;
        }
        /// <summary>The columns' headings, in the list's quiet blue, over a row of this width centred here.</summary>
        public static void Headings(Transform parent,Vector2 centre,float width,IList<Column> columns)
        {
            foreach(var c in columns)
            {
                if(string.IsNullOrEmpty(c.heading))continue;
                var t=UiKit.Text(parent,"ColumnHead",c.heading,24,UiKit.DimTextColor,c.align==TextAlignmentOptions.MidlineRight?TextAlignmentOptions.BottomRight:TextAlignmentOptions.BottomLeft);
                t.characterSpacing=4;t.textWrappingMode=TextWrappingModes.NoWrap;
                UiKit.Rect(t.gameObject,new(.5f,.5f),new(.5f,.5f),centre+new Vector2(c.left+c.width*.5f-width*.5f,0),new(Mathf.Max(c.width,t.GetPreferredValues(c.heading).x),32));
            }
            var rule=UiKit.Divider(parent,"ColumnRule");UiKit.Rect(rule.gameObject,new(.5f,.5f),new(.5f,.5f),centre+new Vector2(0,-20),new(width,4));
        }
        /// <summary>One of the panel's own controls, a pager or Back: a button of its own size at the panel's edge, not a row among the goods.</summary>
        public static Button Control(Transform parent,string name,string label,Action onClick,Vector2 centre,Vector2 size,float font=24)
        {
            var b=UiKit.Button(parent,name,label,onClick);var text=b.GetComponentInChildren<TMP_Text>();text.fontSize=font;text.textWrappingMode=TextWrappingModes.NoWrap;
            UiKit.Rect(b.gameObject,new(.5f,.5f),new(.5f,.5f),centre,size);return b;
        }
        /// <summary>A row a press can only refuse: every cell and the mark go grey, and it keeps its focus, so the refusal can still say why.</summary>
        public static void Dim(Button row)
        {
            foreach(var text in row.GetComponentsInChildren<TMP_Text>())text.color=UiKit.DimTextColor;
            var state=row.colors;state.normalColor=new Color(.46f,.52f,.58f);row.colors=state;
            var icon=row.transform.Find("Item");if(icon!=null&&icon.TryGetComponent<Image>(out var image))image.color=new Color(.55f,.55f,.55f,image.color.a);
        }
        /// <summary>Words in a colour, for a cell or a strip.</summary>
        public static string Ink(string words,string colour)=>string.IsNullOrEmpty(words)?words:$"<color={colour}>{words}</color>";
        /// <summary>The page's controls named in the device's own words, for the line at the panel's foot.</summary>
        public static string Hints(params (string tag,string does)[] hints)=>string.Join("  ·  ",hints.Where(h=>!string.IsNullOrEmpty(h.tag)).Select(h=>h.tag+"  "+h.does));
    }
}
