using Lattice.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>A plate steps aside for what it hides: with someone who matters genuinely behind it, the plate alone drops to about a third quickly and comes back gently once
    /// they've gone. Its words and bars stay as they are: the whole group fading took the words with it, and pale text on an enemy's legs was no gain on a solid box.</summary>
    public sealed class PanelFade
    {
        public const float Floor=.35f;
        const float Down=6,Up=1.4f,Linger=.3f;
        static readonly Vector3[] Corners=new Vector3[4];
        readonly RectTransform rect;readonly Graphic plate;readonly float floor;
        Rect box;bool up;float coveredAt=-99,shown=1;
        public PanelFade(RectTransform rect,Graphic plate,float floor=Floor){this.rect=rect;this.plate=plate;this.floor=floor;}
        /// <summary>Where it stands on the screen this frame, edge to edge: a margin faded it for anything merely near it. A panel that isn't showing covers nothing.</summary>
        public void Measure()
        {
            up=rect!=null&&plate!=null&&plate.isActiveAndEnabled;if(!up)return;
            box=ScreenBox(rect);Add(box);
        }
        public bool Contains(Vector3 screen)=>up&&screen.z>0&&box.Contains(screen);
        public bool Showing=>up;
        /// <summary>Where it stood on the screen when last measured, in pixels: what the partner keeps out from under (HudKeepOut).</summary>
        public Rect Box=>box;
        /// <summary>Whether something that reaches this far around a point touches the plate: the lock's brackets do, well before its middle is behind it.</summary>
        public bool Touches(Vector3 screen,float reach)=>up&&screen.z>0&&screen.x>box.xMin-reach&&screen.x<box.xMax+reach&&screen.y>box.yMin-reach&&screen.y<box.yMax+reach;
        public void Look(Vector3 screen,float now){if(Contains(screen))coveredAt=now;}
        /// <summary>For a thing known by one point that's wider than a point: a pad, a ship's wing.</summary>
        public void Near(Vector3 screen,float reach,float now){if(Touches(screen,reach))coveredAt=now;}
        /// <summary>Through the plate's renderer, not its colour: a colour rebuilds its mesh each step, and a band's own alpha stays what it was built with.</summary>
        public void Step(float now,float dt)
        {
            if(plate==null)return;
            float goal=now-coveredAt<Linger?floor:1;
            if(shown!=goal){shown=Mathf.MoveTowards(shown,goal,dt*(goal<shown?Down:Up));plate.canvasRenderer.SetAlpha(shown);}
            if(bands!=null)StepBands();
        }

        // ---------- what the words keep when the plate has gone ----------
        /// <summary>How dark the words' own backing gets with the plate fully aside, and how far it reaches past their letters. With the plate at a third and nothing of their own,
        /// a panel's words lay straight on the world: 'Also: Mine Ridge Crystals' and the 9 m arrow read as painted on the launch pad they named. A soft band hugging the
        /// letters keeps them type on a screen while the pad, the enemy or the partner still shows round and between the lines.</summary>
        const float BandMost=.62f;
        static readonly Vector2 BandAir=new(16,8);
        TMP_Text[] words;Image[] bands;string[] measured;Rect[] inks;bool bandsUp;
        /// <summary>Gives these labels a band each, drawn between the plate and the words, that comes up as the plate goes down. A label that moves (a sliding card) takes its band with it.</summary>
        public PanelFade Backs(params TMP_Text[] labels)
        {
            words=labels;bands=new Image[labels.Length];measured=new string[labels.Length];inks=new Rect[labels.Length];
            for(int i=0;i<labels.Length;i++)
            {
                var label=labels[i];if(label==null)continue;
                var band=UiKit.SoftBand(label.transform.parent,label.name+"WordsBand",new Color(.01f,.02f,.04f,1));var at=band.rectTransform;var from=label.rectTransform;
                at.anchorMin=from.anchorMin;at.anchorMax=from.anchorMax;at.pivot=new Vector2(.5f,.5f);
                band.transform.SetSiblingIndex(label.transform.GetSiblingIndex());band.canvasRenderer.SetAlpha(0);band.enabled=false;bands[i]=band;
            }
            return this;
        }
        void StepBands()
        {
            float backing=floor<1?Mathf.Clamp01((1-shown)/(1-floor))*BandMost:0;
            if(backing<=.01f&&!bandsUp)return;
            bandsUp=false;
            for(int i=0;i<bands.Length;i++)
            {
                var band=bands[i];var label=words[i];if(band==null)continue;
                bool on=backing>.01f&&label!=null&&label.isActiveAndEnabled&&label.alpha>.01f&&!string.IsNullOrEmpty(label.text);
                if(band.enabled!=on)band.enabled=on;
                if(!on)continue;
                bandsUp=true;
                // Measured as its words change, not every frame: the objective's distance changes four times a second, the plate's alpha every frame.
                if(label.text!=measured[i]){measured[i]=label.text;inks[i]=UiKit.Ink(label);}
                var at=band.rectTransform;at.anchoredPosition=label.rectTransform.anchoredPosition+inks[i].center;at.sizeDelta=inks[i].size+BandAir*2;
                band.canvasRenderer.SetAlpha(backing);
            }
        }

        // ---------- what a word in the world keeps out from under ----------
        static readonly Rect[] plates=new Rect[13];
        static int plateCount;
        public static void BeginPlates()=>plateCount=0;
        public static void AddPlate(RectTransform rect)=>Add(ScreenBox(rect));
        /// <summary>A plate that also claims the room over it: a sign standing there reads as its heading.</summary>
        public static void AddHeadroom(RectTransform rect,float above){var at=ScreenBox(rect);at.yMax+=above;at.xMin-=above*.6f;at.xMax+=above*.6f;Add(at);}
        static void Add(Rect at){if(plateCount<plates.Length)plates[plateCount++]=at;}
        public static Rect ScreenBox(RectTransform rect){rect.GetWorldCorners(Corners);return Rect.MinMaxRect(Corners[0].x,Corners[0].y,Corners[2].x,Corners[2].y);}
        /// <summary>Whether something on the screen is cut by a fixed plate or by the screen's own edge. Wholly off the screen cuts nothing.</summary>
        public static bool Cuts(Rect words,float width,float height)
        {
            if(words.xMax<=0||words.xMin>=width||words.yMax<=0||words.yMin>=height)return false;
            if(words.xMin<0||words.yMin<0||words.xMax>width||words.yMax>height)return true;
            for(int i=0;i<plateCount;i++)if(plates[i].Overlaps(words))return true;
            return false;
        }

        // ---------- what a sliding plate keeps off ----------
        static readonly Vector3[] BoundsCorners=new Vector3[8];
        /// <summary>How tall a body with no shape of its own is taken to stand, and how wide, for a plate to keep off it: what the fade has always looked at, a middle at .75 and a top at 1.6.</summary>
        const float BodyTop=1.6f,BodyHalf=.5f;
        /// <summary>A body as the screen has it, in pixels: its solid shape where it has one (a skiff's hull is wider than a hound), else a standing figure's. Behind the lens it's nothing.</summary>
        public static bool ScreenBody(Camera camera,Health body,out Rect screen)
        {
            screen=default;if(camera==null||body==null)return false;
            Bounds shape;
            if(body.TryGetComponent<Collider>(out var solid)&&solid.enabled)shape=solid.bounds;
            else{var feet=body.transform.position;shape=new Bounds(feet+Vector3.up*BodyTop*.5f,new Vector3(BodyHalf*2,BodyTop,BodyHalf*2));}
            return ScreenBounds(camera,shape,out screen);
        }
        public static bool ScreenBounds(Camera camera,Bounds shape,out Rect screen)
        {
            screen=default;Vector3 min=shape.min,max=shape.max;
            BoundsCorners[0]=new Vector3(min.x,min.y,min.z);BoundsCorners[1]=new Vector3(max.x,min.y,min.z);BoundsCorners[2]=new Vector3(min.x,max.y,min.z);BoundsCorners[3]=new Vector3(max.x,max.y,min.z);
            BoundsCorners[4]=new Vector3(min.x,min.y,max.z);BoundsCorners[5]=new Vector3(max.x,min.y,max.z);BoundsCorners[6]=new Vector3(min.x,max.y,max.z);BoundsCorners[7]=new Vector3(max.x,max.y,max.z);
            float left=float.MaxValue,right=float.MinValue,low=float.MaxValue,high=float.MinValue;
            foreach(var corner in BoundsCorners)
            {
                var at=camera.WorldToScreenPoint(corner);if(at.z<=0)return false;
                left=Mathf.Min(left,at.x);right=Mathf.Max(right,at.x);low=Mathf.Min(low,at.y);high=Mathf.Max(high,at.y);
            }
            screen=Rect.MinMaxRect(left,low,right,high);return true;
        }
    }

    /// <summary>A plate that keeps its edge of the screen and slides along it, off what it would otherwise lie on: the stretches of the edge it can't stand in
    /// this frame, as offsets of its middle from home, and the nearest place clear of them. It's the bark column's rule (D221), laid along a row.</summary>
    public sealed class EdgeRoom
    {
        readonly float[] shut=new float[96];int count;
        public int Count=>count/2;
        public void Begin()=>count=0;
        public void Shut(float from,float to){if(count+2<=shut.Length&&to>from){shut[count++]=from;shut[count++]=to;}}
        public bool Clear(float at,float air)
        {
            for(int i=0;i<count;i+=2)if(at>shut[i]-air&&at<shut[i+1]+air)return false;
            return true;
        }
        /// <summary>Home, with room to spare; else where it has got to while that still works, so nobody walking along an edge makes it twitch; else the clear spot nearest
        /// home inside its reach. Null when nowhere is clear.</summary>
        public float? Seat(float now,float least,float most,float air)
        {
            if(Clear(0,air*2))return 0;
            if(now>=least&&now<=most&&Clear(now,air))return now;
            float? best=null;float bestFar=float.MaxValue;
            for(int i=0;i<count;i++)
            {
                float spot=shut[i]+(i%2==0?-air-1:air+1);
                if(spot<least||spot>most||!Clear(spot,air))continue;
                float far=Mathf.Abs(spot);if(far<bestFar){bestFar=far;best=spot;}
            }
            return best;
        }
    }
}
