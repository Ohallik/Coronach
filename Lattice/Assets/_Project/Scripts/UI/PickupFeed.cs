using System.Collections.Generic;
using Lattice.Core;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>A small stacking feed under the vitals: "+2 Scrap Alloy", "+8 scrip", "Saved". Loot, mining and caches used to
    /// add items with no word to the player at all. The same item within a moment adds to its line rather than stacking up.</summary>
    public sealed class PickupFeed:MonoBehaviour
    {
        const float Life=2.6f,Fade=.5f;const int MaxLines=5;
        sealed class Line{public string id,name;public int amount;public float age;public TMP_Text text;}
        readonly List<Line> lines=new();
        Canvas canvas;
        void Start()
        {
            canvas=UiKit.CreateCanvas("PickupFeed",6,transform);
            Lattice.Rpg.Inventory.Gained+=OnGained;SaveSystem.Written+=OnSaved;
        }
        void OnDestroy(){Lattice.Rpg.Inventory.Gained-=OnGained;SaveSystem.Written-=OnSaved;}
        void OnGained(string id,int amount)
        {
            if(canvas==null)return;
            string name=id=="scrip"?"scrip":DisplayNames.Item(id);
            Add(id,name,amount);
        }
        void OnSaved(string slot){if(canvas!=null)Add("saved","Saved",0);}
        void Add(string id,string name,int amount)
        {
            var line=lines.Find(l=>l.id==id&&l.age<Life);
            if(line==null)
            {
                if(lines.Count>=MaxLines){Destroy(lines[0].text.gameObject);lines.RemoveAt(0);}
                line=new Line{id=id,name=name};
                line.text=UiKit.Text(canvas.transform,"PickupLine","",24,amount>0?UiKit.GoldColor:UiKit.DimTextColor,TextAlignmentOptions.MidlineLeft);
                line.text.outlineWidth=.2f;line.text.outlineColor=new Color32(5,12,22,255);
                UiKit.Rect(line.text.gameObject,new(0,1),new(0,1),Vector2.zero,new(420,32));
                lines.Add(line);
            }
            line.amount+=amount;line.age=0;
            line.text.text=amount==0&&line.amount==0?name:$"+{line.amount} {name}";
            Layout();
        }
        void Layout(){for(int i=0;i<lines.Count;i++)lines[i].text.rectTransform.anchoredPosition=new(250,-318-(lines.Count-1-i)*32);}
        void Update()
        {
            if(canvas==null)return;
            canvas.enabled=GameInput.Current==null||!GameInput.Current.Blocked;
            bool changed=false;
            for(int i=lines.Count-1;i>=0;i--)
            {
                var line=lines[i];line.age+=Time.unscaledDeltaTime;
                line.text.alpha=Mathf.Clamp01((Life+Fade-line.age)/Fade);
                if(line.age<Life+Fade)continue;
                Destroy(line.text.gameObject);lines.RemoveAt(i);changed=true;
            }
            if(changed)Layout();
        }
    }
}
