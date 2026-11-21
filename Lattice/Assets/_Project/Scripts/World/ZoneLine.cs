using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A line a zone's own mechanism puts on the objective panel while it means it, and where its arrow points: chapter 9's Terminator says "Keep ahead of the dawn", "Re-mate the
    /// coupling (1/3)" and "Ride the tread-hall out" (manifest section 6), which no quest step can, because they follow the line and the party from moment to moment. One owner at a time;
    /// the panel (UI's ObjectiveHud, in its default branch, after the story's step and before a boss's or a downed partner's line) reads it every quarter second. Words left empty keep the
    /// story's own and only move the arrow. An owner that is destroyed lets go by itself.
    /// </summary>
    public static class ZoneLine
    {
        static Object owner;static string words;static Vector3? goal;
        public static void Say(Object by,string line,Vector3? at){if(by==null)return;owner=by;words=line;goal=at;}
        public static void Clear(Object by){if(owner!=by)return;owner=null;words=null;goal=null;}
        /// <summary>Whether a line is held, what it says (empty: the story's own) and where its arrow points (none: the story's own).</summary>
        public static bool Read(out string line,out Vector3? at)
        {
            line=null;at=null;if(owner==null)return false;
            line=words;at=goal;return true;
        }
    }
}
