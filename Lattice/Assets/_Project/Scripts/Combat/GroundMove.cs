namespace Lattice.Combat
{
    // Contact phases come from the retargeted visible blade audit. Each take has
    // its own anticipation/strike/recovery; these are not a shared timer fraction.
    public readonly struct GroundMove
    {
        public readonly string clip;
        public readonly float duration,contactStart,contactEnd,recoveryEnd;
        public GroundMove(string clip,float duration,float start,float end,float recoveryEnd)
        {this.clip=clip;this.duration=duration;contactStart=start;contactEnd=end;this.recoveryEnd=recoveryEnd;}
        public static GroundMove Cut(int stage)=>stage==2?new("Attack3",.48f,.57f,.66f,.408f):
            stage==1?new("Attack2",.36f,.54f,.70f,.306f):new("Attack1",.32f,.48f,.62f,.272f);
        public static GroundMove Cleave=>new("Cleave",.5f,.34f,.48f,.425f);
    }
}
