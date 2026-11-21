using UnityEngine;
using UnityEngine.InputSystem;
namespace Lattice.Core
{
    /// <summary>A short pulse of the pad's motors for a hit. Only a pad that has motors and says so gets one: an XInput pad. The bridged DirectInput pad (PadBridge) and a
    /// replay's or a test's virtual pad are plain Gamepads with nothing behind them, and are left alone. It stops for a pause and at quit, so a motor is never left running.</summary>
    public static class PadRumble
    {
        static Gamepad driven;static float until;static bool hooked;
        public static float Low{get;private set;}
        public static float High{get;private set;}
        public static bool Active=>driven!=null;
        public static bool Supports(Gamepad pad)=>pad!=null&&pad.added&&pad.description.interfaceName=="XInput";
        /// <param name="low">The heavy motor, 0 to 1.</param><param name="high">The light motor, 0 to 1.</param>
        public static void Pulse(float low,float high,float seconds)
        {
            if(seconds<=0||low<=0&&high<=0||GameTime.Paused||!ImpactSettings.Enabled||!ImpactSettings.Rumble)return;
            var pad=Gamepad.current;if(!Supports(pad))return;
            if(!hooked){hooked=true;GameTime.PauseChanged+=OnPause;Application.quitting+=Stop;}
            low=Mathf.Clamp01(low);high=Mathf.Clamp01(high);
            // A stronger pulse still running is the one felt: a tick landing on top of a slam doesn't cut it short.
            if(driven==pad&&Time.unscaledTime<until&&low<=Low&&high<=High)return;
            if(driven!=null&&driven!=pad)Stop();
            driven=pad;Low=low;High=high;until=Time.unscaledTime+seconds;pad.SetMotorSpeeds(low,high);
        }
        /// <summary>Once a frame, by whoever asks for pulses: a pulse ends on real time, through a hit-stop.</summary>
        public static void Tick(){if(driven!=null&&Time.unscaledTime>=until)Stop();}
        public static void Stop(){if(driven!=null&&driven.added)driven.SetMotorSpeeds(0,0);driven=null;Low=High=0;}
        static void OnPause(bool paused){if(paused)Stop();}
    }
}
