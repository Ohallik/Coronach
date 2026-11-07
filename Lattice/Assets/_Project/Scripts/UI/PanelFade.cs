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
        /// <summary>Whether something that reaches this far around a point touches the plate: the lock's brackets do, well before its middle is behind it.</summary>
        public bool Touches(Vector3 screen,float reach)=>up&&screen.z>0&&screen.x>box.xMin-reach&&screen.x<box.xMax+reach&&screen.y>box.yMin-reach&&screen.y<box.yMax+reach;
        public void Look(Vector3 screen,float now){if(Contains(screen))coveredAt=now;}
        /// <summary>Through the plate's renderer, not its colour: a colour rebuilds its mesh each step, and a band's own alpha stays what it was built with.</summary>
        public void Step(float now,float dt)
        {
            if(plate==null)return;
            float goal=now-coveredAt<Linger?floor:1;
            if(shown==goal)return;
            shown=Mathf.MoveTowards(shown,goal,dt*(goal<shown?Down:Up));plate.canvasRenderer.SetAlpha(shown);
        }
        // ---------- what a word in the world keeps out from under ----------
        static readonly Rect[] plates=new Rect[12];
        static int plateCount;
        public static void BeginPlates()=>plateCount=0;
        public static void AddPlate(RectTransform rect)=>Add(ScreenBox(rect));
        static void Add(Rect at){if(plateCount<plates.Length)plates[plateCount++]=at;}
        static Rect ScreenBox(RectTransform rect){rect.GetWorldCorners(Corners);return Rect.MinMaxRect(Corners[0].x,Corners[0].y,Corners[2].x,Corners[2].y);}
        /// <summary>Whether something on the screen is cut by a fixed plate or by the screen's own edge. Wholly off the screen cuts nothing.</summary>
        public static bool Cuts(Rect words,float width,float height)
        {
            if(words.xMax<=0||words.xMin>=width||words.yMax<=0||words.yMin>=height)return false;
            if(words.xMin<0||words.yMin<0||words.xMax>width||words.yMax>height)return true;
            for(int i=0;i<plateCount;i++)if(plates[i].Overlaps(words))return true;
            return false;
        }
    }
}
