using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Core
{
    /// <summary>Where the HUD's fixed plates stand on the screen, in screen pixels, for what moves in the world and should keep out from under them: the partner flew
    /// under the skill row, behind the boss card and across the warp prompt, and nothing below the UI knew where the UI was. The HUD fills it once a frame after the
    /// camera has moved; a reader in Update gets the frame before, which is near enough for something that moves at a walk.</summary>
    public static class HudKeepOut
    {
        static readonly List<Rect> rects=new();
        static int filledAt=-99;
        public static IReadOnlyList<Rect> Rects=>rects;
        /// <summary>Whether the list is this frame's or the last one's: a HUD that's gone (a menu, a conversation, a test with none) leaves nothing to keep out of.</summary>
        public static bool Fresh=>Time.frameCount-filledAt<=2;
        public static void Begin(){rects.Clear();reserved.Clear();filledAt=Time.frameCount;}
        public static void Add(Rect screen){if(screen.width>0&&screen.height>0)rects.Add(screen);}
        /// <summary>Where a plate that is down now comes up without anyone asking for it (the quest's card under the objective, a bark): where it stood when it was last up. Sela stood
        /// firing at the Bellows where the quest's card then came up over her, and was under it for a second before she could walk out (nov23-whole s08/095, s08/098). What
        /// comes to rest on foot keeps out from under these too (PartnerBrain.Fits).</summary>
        public static IReadOnlyList<Rect> Reserved=>reserved;
        static readonly List<Rect> reserved=new();
        public static void Reserve(Rect screen){if(screen.width>0&&screen.height>0)reserved.Add(screen);}
        /// <summary>Whether a point on the screen, grown by a margin each way, touches any plate (and, with <paramref name="unbidden"/>, any place a plate comes up unbidden). A point
        /// behind the camera touches nothing.</summary>
        public static bool Covers(Vector3 screen,float margin=0,bool unbidden=false)
        {
            if(!Fresh||screen.z<=0)return false;
            foreach(var r in rects)if(screen.x>r.xMin-margin&&screen.x<r.xMax+margin&&screen.y>r.yMin-margin&&screen.y<r.yMax+margin)return true;
            if(unbidden)foreach(var r in reserved)if(screen.x>r.xMin-margin&&screen.x<r.xMax+margin&&screen.y>r.yMin-margin&&screen.y<r.yMax+margin)return true;
            return false;
        }
        /// <summary>For tests and for a scene with no HUD.</summary>
        public static void Clear(){rects.Clear();reserved.Clear();filledAt=-99;}
    }
}
