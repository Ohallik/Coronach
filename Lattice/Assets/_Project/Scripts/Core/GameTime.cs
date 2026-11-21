using UnityEngine;
namespace Lattice.Core
{
    public static class GameTime
    {
        public static event System.Action<bool> PauseChanged;
        static bool paused;
        static int flashes;
        static float pausedAt,pausedTotal;
        /// <summary>A hit-stop: for a few frames the world's clock stands at 0 and the heroes' own clock, <see cref="Now"/>, holds with it. The heroes run on real time
        /// (their poses, recoveries and i-frames), so a stop that only set the time scale froze the enemy and let the hero swing on through it. A pause outranks it and
        /// ends it, so a menu always closes on a running clock; a Flash's slow motion comes back when it ends. It's counted on the first read of each frame, so nothing has to tick it.</summary>
        static float stopLeft,stoppedTotal;
        static int syncedFrame=-1,heldFrame=-1;
        /// <summary>The longest one stop may hold the game, whoever asks.</summary>
        public const float MostStop=.25f;
        /// <summary>The heroes' clock. It never runs backwards: held by a stop or a pause it is one growing number taken from another, and their rounding could step it
        /// back a float's width (0.00006 s at twelve minutes), so the Cantor's release was once recorded as a negative time old (the replay of November 13).</summary>
        public static float Now
        {
            get
            {
                Sync();float now=Time.unscaledTime-pausedTotal-stoppedTotal-(paused?Time.unscaledTime-pausedAt:0);
                if(now<latest&&latest-now<Backstep)return latest;
                latest=now;return now;
            }
        }
        /// <summary>The most the rounding can take back; anything larger is the clock being set (Reset, or a test moving time), and is believed.</summary>
        const float Backstep=.01f;
        static float latest=float.NegativeInfinity;
        public static bool Paused
        {
            get=>paused;
            set
            {
                if(value==paused)return;
                Sync();
                // What was left of a stop is let go: held instead, it ran on for a few frames after the menu closed, on a clock everything else expects back at once.
                if(value){pausedAt=Time.unscaledTime;stopLeft=0;}else pausedTotal+=Time.unscaledTime-pausedAt;
                paused=value;Apply();PauseChanged?.Invoke(value);
            }
        }
        /// <summary>Time stands still for combat: a menu, or a conversation or load holding the input. Enemies stand frozen
        /// through these, so shots and lingering effects must too.</summary>
        public static bool CombatHeld=>paused||GameServices.Current!=null&&GameServices.Current.Input.Blocked;
        /// <summary>This frame is held by a hit-stop: nothing in the fight moves, the heroes included.</summary>
        public static bool Stopped{get{Sync();return !paused&&(stopLeft>0||heldFrame==Time.frameCount);}}
        /// <summary>What is left of the stop, in real seconds.</summary>
        public static float StopLeft{get{Sync();return stopLeft;}}
        /// <summary>A frame of the heroes' clock: real time, which a pause or a hit-stop holds.</summary>
        public static float Delta{get{Sync();return paused||heldFrame==Time.frameCount?0:Time.unscaledDeltaTime;}}
        public static bool Flashing=>flashes>0;
        public static void BeginFlash(){flashes++;Apply();}
        public static void EndFlash(){flashes=Mathf.Max(0,flashes-1);Apply();}
        /// <summary>Hold the fight for this long from now. A stop already running is kept if it has longer to go: two asks are the longer of them, never their sum.</summary>
        public static void Stop(float seconds)
        {
            Sync();if(seconds<=0||paused)return;
            stopLeft=Mathf.Max(stopLeft,Mathf.Min(seconds,MostStop));Apply();
        }
        public static void Reset(){flashes=0;paused=false;pausedAt=pausedTotal=0;stopLeft=stoppedTotal=0;syncedFrame=heldFrame=-1;latest=float.NegativeInfinity;Apply();}
        /// <summary>The frame that has just gone by was held if a stop was running when it began: the time scale was 0 for it, and its real time comes off the heroes' clock too.
        /// The frame a stop runs out in is still a held one, since its world delta was fixed at 0 before anything ran.</summary>
        static void Sync()
        {
            if(syncedFrame==Time.frameCount)return;syncedFrame=Time.frameCount;
            if(paused||stopLeft<=0)return;
            float dt=Time.unscaledDeltaTime;stoppedTotal+=dt;stopLeft-=dt;heldFrame=Time.frameCount;
            if(stopLeft<=0){stopLeft=0;Apply();}
        }
        static void Apply()=>Time.timeScale=paused||stopLeft>0?0:flashes>0?.25f:1;
    }
}
