using System.Collections.Generic;
using Lattice.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>A small stacking feed under the vitals: "+2 Scrap Alloy", "+8 scrip", "Saved". Loot, mining and caches used to
    /// add items with no word to the player at all. The same item within a moment adds to its line rather than stacking up.</summary>
    public sealed class PickupFeed:MonoBehaviour
    {
        const float Life=2.6f,Fade=.5f;const int MaxLines=5;
        /// <summary>The band's darkness, and how far it runs past the end of the longest line (its soft edge is in that).</summary>
        public const float BandStrength=.8f,BandPastTheLines=64;
        sealed class Line{public string id,name;public int amount;public float age;public TMP_Text text;}
        readonly List<Line> lines=new();
        Canvas canvas;Image band;GameHud hud;float laidFrom=-1;
        /// <summary>The feed's plate while it's up: a sign keeps out from under it.</summary>
        public RectTransform Band=>band!=null&&band.enabled?band.rectTransform:null;
        /// <summary>How far down from the top of the screen the feed reaches: the foot of what it hangs from (the vitals, or a zone's gauge under them), with nothing up.</summary>
        public float Reach=>band!=null&&band.enabled?From-5+band.rectTransform.rect.height:From;
        /// <summary>Where the feed hangs from, down from the top of the screen: under the vitals, or under the zone's gauge while it's up. At a fixed 330 its 'Saved' printed over
        /// the Picket's panel (s45 140).</summary>
        float From=>hud!=null?hud.ColumnFoot:311;
        void Start()
        {
            canvas=UiKit.CreateCanvas("PickupFeed",6,transform);hud=GetComponent<GameHud>();
            // Gold and a dim blue on Sorrel's orange floor were the weakest words on the screen at the payoff of a fight, outline or no outline. A soft dark band behind the stack, first in the canvas, with a tighter edge than a card's: it stands right under the vitals.
            // At .66 and ending a hand past the longest line, the big painted letters of a roof's TEA ROOM stood bright against '+22 Scrap Alloy', one string with it (s29 046):
            // at .8, the most a band over the world may take (QaHudSecondTests), and running on well past the lines' ends, the world behind them reads as the world.
            band=UiKit.SoftBand(canvas.transform,"PickupBand",new Color(.01f,.02f,.04f,BandStrength));band.pixelsPerUnitMultiplier=2;band.enabled=false;
            Lattice.Rpg.Inventory.Gained+=OnGained;SaveSystem.Written+=OnSaved;Lattice.Combat.PlayerBrain.QuickItemRefused+=OnRefused;Notices.Posted+=OnNotice;
        }
        void OnDestroy(){Lattice.Rpg.Inventory.Gained-=OnGained;SaveSystem.Written-=OnSaved;Lattice.Combat.PlayerBrain.QuickItemRefused-=OnRefused;Notices.Posted-=OnNotice;}
        /// <summary>A card that says it's saved has said it. A repair bay's save was a bare word here and a card at the right, the same news twice. The bay saves first, so the word is already up.</summary>
        void OnNotice(string heading,string body)
        {
            if(canvas==null||!heading.Contains("SAVED"))return;
            var line=lines.Find(l=>l.id=="saved");if(line==null)return;
            Destroy(line.text.gameObject);lines.Remove(line);Layout();
        }
        void OnGained(string id,int amount)
        {
            if(canvas==null)return;
            string name=id=="scrip"?"scrip":id=="xp"?"XP":DisplayNames.Item(id);
            Add(id,name,amount);
        }
        void OnSaved(string slot){if(canvas!=null)Add("saved","Saved",0);}
        void OnRefused(string why){if(canvas!=null)Add("refused",why,0);}
        void Add(string id,string name,int amount)
        {
            var line=lines.Find(l=>l.id==id&&l.age<Life);
            if(line==null)
            {
                if(lines.Count>=MaxLines){Destroy(lines[0].text.gameObject);lines.RemoveAt(0);}
                line=new Line{id=id,name=name};
                // XP comes with every kill: it stays quiet beside what the player actually picked up.
                // "Saved" and a refused item's reason are read once, over whatever's there: only the XP stays quiet.
                line.text=UiKit.Legible(UiKit.Text(canvas.transform,"PickupLine","",24,amount>0&&id!="xp"?UiKit.GoldColor:id=="xp"?UiKit.DimTextColor:id=="refused"?new Color(1,.68f,.55f):UiKit.LooseTextColor,TextAlignmentOptions.MidlineLeft));
                UiKit.Rect(line.text.gameObject,new(0,1),new(0,1),Vector2.zero,new(420,32));
                lines.Add(line);
            }
            line.amount+=amount;line.age=0;
            line.text.text=amount==0&&line.amount==0?name:$"+{line.amount} {name}";
            Layout();
        }
        void Layout()
        {
            float wide=0,from=laidFrom=From;
            for(int i=0;i<lines.Count;i++){lines[i].text.rectTransform.anchoredPosition=new(250,-(from+19)-(lines.Count-1-i)*32);wide=Mathf.Max(wide,lines[i].text.preferredWidth);}
            if(band.enabled!=lines.Count>0)band.enabled=lines.Count>0;
            if(lines.Count==0)return;
            float width=wide+26+BandPastTheLines,height=lines.Count*32+16;
            UiKit.Rect(band.gameObject,new(0,1),new(0,1),new(14+width*.5f,-(from-5)-height*.5f),new(width,height));
        }
        void Update()
        {
            if(canvas==null)return;
            canvas.enabled=GameInput.Current==null||!GameInput.Current.Blocked;
            bool changed=false;float strongest=0;
            for(int i=lines.Count-1;i>=0;i--)
            {
                var line=lines[i];line.age+=Time.unscaledDeltaTime;
                line.text.alpha=Mathf.Clamp01((Life+Fade-line.age)/Fade);strongest=Mathf.Max(strongest,line.text.alpha);
                if(line.age<Life+Fade)continue;
                Destroy(line.text.gameObject);lines.RemoveAt(i);changed=true;
            }
            if(band.enabled)band.canvasRenderer.SetAlpha(strongest);
            // A gauge come up or gone under the vitals moves the column's foot, and the lines with it.
            if(changed||lines.Count>0&&From!=laidFrom)Layout();
        }
    }
}
