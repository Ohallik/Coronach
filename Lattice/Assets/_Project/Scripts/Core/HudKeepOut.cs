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
        public static void Begin(){rects.Clear();filledAt=Time.frameCount;}
        public static void Add(Rect screen){if(screen.width>0&&screen.height>0)rects.Add(screen);}
        /// <summary>Whether a point on the screen, grown by a margin each way, touches any plate. A point behind the camera touches nothing.</summary>
        public static bool Covers(Vector3 screen,float margin=0)
        {
            if(!Fresh||screen.z<=0)return false;
            foreach(var r in rects)if(screen.x>r.xMin-margin&&screen.x<r.xMax+margin&&screen.y>r.yMin-margin&&screen.y<r.yMax+margin)return true;
            return false;
        }
        /// <summary>For tests and for a scene with no HUD.</summary>
        public static void Clear(){rects.Clear();filledAt=-99;}
    }
}
