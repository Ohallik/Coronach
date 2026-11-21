using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// Words on black between two places, for a cut in time (chapter 10's SEVEN WEEKS ON, between the Returning Passage and Neighbourhood Afloat): shown by <see cref="CutAway"/>, drawn by
    /// the screen's fade over everything (Lattice.UI.ScreenFade), which outlives the load. It comes up over <see cref="In"/> seconds, stays at least its own seconds and for as long as a
    /// zone is loading, and goes over <see cref="Out"/>, uncovering the place it cut to.
    /// </summary>
    public static class TimePlate
    {
        public const float In=1.1f,Out=1.4f;
        static string words="";static float shownAt=-100,until=-100,outFrom=-1;
        public static string Words=>words;
        public static void Show(string text,float seconds)
        {
            words=text??"";shownAt=Time.unscaledTime;until=shownAt+Mathf.Max(In,seconds);outFrom=-1;Debug.Log("TIME_PLATE "+words);
        }
        public static void Clear(){words="";outFrom=-1;until=shownAt=-100;}
        /// <summary>How much of it shows now, nought to one: <paramref name="loading"/> holds it up until the load is done.</summary>
        public static float Alpha(bool loading)
        {
            if(string.IsNullOrEmpty(words))return 0;
            float now=Time.unscaledTime;
            if(outFrom<0)
            {
                if(now<until||loading)return Mathf.Clamp01((now-shownAt)/In);
                outFrom=now;
            }
            float a=1-Mathf.Clamp01((now-outFrom)/Out);
            if(a<=0)Clear();
            return a;
        }
    }
}
