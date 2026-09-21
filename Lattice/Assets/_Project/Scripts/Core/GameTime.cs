using UnityEngine;
namespace Lattice.Core
{
    public static class GameTime
    {
        static bool paused;
        static int flashes;
        static float pausedAt,pausedTotal;
        public static float Now=>Time.unscaledTime-pausedTotal-(paused?Time.unscaledTime-pausedAt:0);
        public static bool Paused
        {
            get=>paused;
            set
            {
                if(value==paused)return;
                if(value)pausedAt=Time.unscaledTime;else pausedTotal+=Time.unscaledTime-pausedAt;
                paused=value;Apply();
            }
        }
        public static void BeginFlash(){flashes++;Apply();}
        public static void EndFlash(){flashes=Mathf.Max(0,flashes-1);Apply();}
        public static void Reset(){flashes=0;paused=false;pausedAt=pausedTotal=0;Apply();}
        static void Apply()=>Time.timeScale=paused?0:flashes>0?.25f:1;
    }
}
