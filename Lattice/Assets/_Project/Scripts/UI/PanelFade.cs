using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>A panel steps aside for what it hides: with someone who matters behind it, its whole group drops to about a third quickly and comes
    /// back gently once they've gone. The top corners are where enemies arrive and a partner trails, and the panels were solid boxes.</summary>
    public sealed class PanelFade
    {
        public const float Floor=.35f;
        /// <summary>Who moves it. The two plates that say what to do next wait for the party; the corners and the target plate give way to anyone in the fight.</summary>
        public const int Hero=0,Party=1,Everyone=2;
        const float Down=6,Up=1.4f,Linger=.3f;
        static readonly Vector3[] Corners=new Vector3[4];
        readonly RectTransform rect;readonly Graphic shown;readonly CanvasGroup group;readonly int reach;readonly float floor;
        Rect box;bool up;float coveredAt=-99;
        public PanelFade(RectTransform rect,Graphic shown,CanvasGroup group,int reach=Everyone,float floor=Floor)
        {this.rect=rect;this.shown=shown;this.group=group;this.reach=reach;this.floor=floor;}
        /// <summary>Where it stands on the screen this frame, with a margin for bodies only partly behind it. A panel that isn't showing covers nothing.</summary>
        public void Measure(float margin)
        {
            up=rect!=null&&shown!=null&&shown.isActiveAndEnabled;if(!up)return;
            rect.GetWorldCorners(Corners);box=Rect.MinMaxRect(Corners[0].x-margin,Corners[0].y-margin,Corners[2].x+margin,Corners[2].y+margin);
        }
        public bool Contains(Vector3 screen)=>up&&screen.z>0&&box.Contains(screen);
        public void Look(Vector3 screen,int who,float now){if(who<=reach&&Contains(screen))coveredAt=now;}
        public void Step(float now,float dt)
        {
            if(group==null)return;
            float goal=now-coveredAt<Linger?floor:1;
            if(group.alpha!=goal)group.alpha=Mathf.MoveTowards(group.alpha,goal,dt*(goal<group.alpha?Down:Up));
        }
    }
}
