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
        public static GroundMove Needle=>new("Shoot",.32f,.46f,.46f,.272f);
        public static GroundMove Scatter=>new("Shoot",.4f,.46f,.46f,.34f);
        public static GroundMove Lance=>new("Shoot",.5f,.46f,.46f,.425f);
        public static GroundMove EmberDash=>new("Dash",.55f,.25f,.78f,.495f);
        public static GroundMove Pulse=>new("Buff",.55f,.46f,.46f,.49f);
        public static GroundMove StaticNet=>new("Pulse",.65f,.255f,.255f,.56f);
    }
}
