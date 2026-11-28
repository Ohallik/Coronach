using UnityEngine;

namespace Lattice.Core
{
    /// <summary>The last thing between the game's mix and the ears: a peak limiter on the listener's own object (D460). The first captures
    /// with round ten's levels (November 28) read the summed mix over full scale in every boss fight: 17 to 23 times in a fight of two or
    /// three minutes, +2.7 dBFS at the worst, half a millisecond a time, wherever two or three blows landed inside one block over the music.
    /// Each voice is at its level (<see cref="CueLevels"/>); it is their sum that has no ceiling, and what leaves the engine over full scale
    /// is clipped. So nothing is turned down: the sum is held under <see cref="CeilingDb"/> for as long as a stack lasts, and let go after it.
    /// It looks <see cref="LookMs"/> ahead (the mix is that much late, which nobody hears), so the cut is in place before the peak arrives
    /// and the wave keeps its shape; a mix under the ceiling passes sample for sample.</summary>
    public sealed class MasterLimiter:MonoBehaviour
    {
        public const float CeilingDb=-1f,LookMs=1.5f,ReleaseMs=80f;
        public static readonly float Ceiling=Mathf.Pow(10,CeilingDb/20);
        /// <summary>Tests: the mix as round eleven left it, passed through untouched.</summary>
        public static bool AsRoundEleven;
        /// <summary>How many blocks were cut at all, and the deepest cut in dB (0 when nothing was over), since the player started.</summary>
        public static int BlocksCut{get;private set;}
        public static float DeepestCutDb{get;private set;}

        /// <summary>The limiter itself, apart from the engine: interleaved frames in, the same frames <see cref="Delay"/> late and never
        /// over the ceiling out.</summary>
        public sealed class Line
        {
            readonly int look;readonly float attack,release;
            float[] held;int at,hold;float gain=1,low=1;
            public int Delay=>look;
            public float Gain=>gain;
            public Line(int rate,int channels=2)
            {
                look=Mathf.Max(8,Mathf.RoundToInt(rate*LookMs/1000));
                // The cut closes 99% of the way in the time the mix is held back, and opens over ReleaseMs.
                attack=1-Mathf.Exp(-5f/look);release=1-Mathf.Exp(-1000f/(rate*ReleaseMs));
                held=new float[look*Mathf.Max(1,channels)];
            }
            /// <summary>Returns the lowest gain it applied in this block (1 when it cut nothing).</summary>
            public float Run(float[] data,int channels)
            {
                if(channels<1)return 1;
                if(held.Length!=look*channels){held=new float[look*channels];at=0;}
                float lowest=1;
                for(int i=0;i+channels<=data.Length;i+=channels)
                {
                    float peak=0;
                    for(int c=0;c<channels;c++){float x=data[i+c];if(x<0)x=-x;if(x>peak)peak=x;}
                    // The deepest cut any frame still held back needs, kept until the last of them has left.
                    if(peak>Ceiling){float want=Ceiling/peak;if(want<low)low=want;hold=look+1;}
                    else if(hold>0&&--hold==0)low=1;
                    gain+=(low-gain)*(low<gain?attack:release);
                    // Within a hundredth of a dB of open is open: a float's steps near 1 are too coarse for the release to finish by itself.
                    if(low>=1&&gain>.999f)gain=1;
                    if(gain<lowest)lowest=gain;
                    for(int c=0;c<channels;c++)
                    {
                        float past=held[at+c];held[at+c]=data[i+c];
                        if(gain<1)past*=gain;
                        // What the attack's last hundredth leaves over: a hair, taken off here so the ceiling is a ceiling.
                        data[i+c]=past>Ceiling?Ceiling:past<-Ceiling?-Ceiling:past;
                    }
                    at+=channels;if(at>=held.Length)at=0;
                }
                return lowest;
            }
        }

        Line line;
        void OnEnable(){line=new Line(AudioSettings.outputSampleRate);}
        void OnAudioFilterRead(float[] data,int channels)
        {
            var mine=line;if(mine==null||AsRoundEleven)return;
            float lowest=mine.Run(data,channels);
            if(lowest<1){BlocksCut++;float db=20*Mathf.Log10(lowest);if(db<DeepestCutDb)DeepestCutDb=db;}
        }
        /// <summary>One on the object that carries the listener, ahead of anything put there afterwards to record the mix (filters run in
        /// the order their components were added).</summary>
        public static MasterLimiter On(GameObject ears)=>ears==null?null:ears.TryGetComponent<MasterLimiter>(out var have)?have:ears.AddComponent<MasterLimiter>();
    }
}
