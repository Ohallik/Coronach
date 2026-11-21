using System;
namespace Lattice.Data
{
    /// <summary>What a hero's plain attack is. Default is the first two heroes' own (Taren's edge, Sela's emitter), which the code still tells apart as it always did;
    /// Hammer is the guard hero's slow three-hit chain on foot and a short breaching cutter in flight.</summary>
    public enum AttackStyle{Default,Hammer}

    /// <summary>How one hero's body fights where it differs from another's (S6a): the numbers of the guard, the dodge and the plain attack, as data on the hero's
    /// <see cref="CharacterDef"/> rather than a name checked in the code. Every default is what Taren and Sela have always had, so a definition that says nothing
    /// fights exactly as before.</summary>
    [Serializable] public sealed class HeroStyle
    {
        public AttackStyle attack;
        /// <summary>What a held guard lets through, and what it pays in Thrust for a whole Integrity bar stopped (CombatActor.GuardTakes, GuardCost).</summary>
        public float guardTakes=.35f,guardCost=250;
        /// <summary>Whether the guard works in flight, as a prow braced against what comes from ahead, and how wide an arc ahead it covers, in degrees.</summary>
        public bool flightGuard;public float prowArc=120;
        /// <summary>A Flash Guard's answer: a shockwave round the hero, in Output, with its Break and reach. Zero is none.</summary>
        public float counter,counterBreak,counterReach;
        /// <summary>The dodge on foot: how far it carries, how long it takes, and for how long of that nothing can touch the hero. The others' roll is untouchable throughout.</summary>
        public float hop=3.3f,hopSeconds=.25f,hopSafe=99;
        /// <summary>The same for a ship's roll.</summary>
        public float roll=4,rollSeconds=.3f,rollSafe=99;
        /// <summary>How long after a swing the next one still counts as the chain's next stage.</summary>
        public float chainWindow=.9f;
        /// <summary>As a companion, this hero meets a hit that's about to land with the guard where the others roll.</summary>
        public bool partnerGuards;
        /// <summary>The hero's own effects (a ship's thrusters, a dash's edge, the flash of a form change) wear the warm palette, as Taren's do, where the default is Sela's cool one.</summary>
        public bool warm;
        /// <summary>A hero standing in another's bodies until their own are made: whose they are, and the wash that tells the two apart. A form is washed only while it
        /// is still the lender's, so the day the hero's own body lands it's shown as made. White is none.</summary>
        public string bodiesFrom;
        public UnityEngine.Color bodyTint=UnityEngine.Color.white;
    }
}
