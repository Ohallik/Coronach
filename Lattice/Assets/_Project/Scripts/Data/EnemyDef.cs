using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/EnemyDef")]
    public sealed class EnemyDef:ScriptableObject
    {
        /// <summary>A person, not a monster: at zero she gives up rather than dies, and hands over to the person standing there.</summary>
        public bool yieldsOnDefeat;
        public GameObject collarPlate;
        public string id; public GameObject prefab,bodySegment,tailSegment; public Stats stats; public float integrity=100,breakThreshold=80,speed=3.2f; public AttackDef[] attacks; public DamageType weakness,resistance; public EnemyArchetype archetype; public LootTable lootTable; public int xp=24; public bool boss;
        /// <summary>Scrip and Scrap Alloy every kill pays, beside its loot table. Below zero it pays what every enemy used to: 8 and 2, or a boss's 150 and 12 (LootDrop).</summary>
        public int scrip=-1,scrap=-1;
        /// <summary>What the target card calls it ("Muster Scavenger"). Empty: the id split into words (DisplayNames.Target).</summary>
        public string displayName;
        /// <summary>A boss's track, a title from docs/music/tracks.json. Empty: "Alien Boss Battle". MusicIntake stages whatever is named.</summary>
        public string bossMusicId;
        /// <summary>The body's capsule. Zero: by kind, as every enemy had it (.55 by 1.5; a boss 3 tall, as wide as its registration says).</summary>
        public float capsuleRadius,capsuleHeight;
        /// <summary>A rigid body's idle motion, drawn only.</summary>
        public EnemyMotion motion;
        /// <summary>Only what's drawn: the hit shape stays the capsule (a Dart at 1.5 was too small to find on a dark sky).</summary>
        public float drawnScale=1;
        /// <summary>Dies with the heavy sound, as armour, a mine or a hull does.</summary>
        public bool heavyDeath;
        /// <summary>How it fights, past what its archetype does (Lattice.Combat.EnemyBehaviour): None is by archetype alone, as the first fourteen fight. The archetype still says what kind
        /// of body it is to the party (a Sentinel is too heavy to reel, a Mine holds its ground, a Swarm can be shoved).</summary>
        public EnemyBehaviourKind behaviour;
        /// <summary>That behaviour's own numbers. Which of them a family reads, and what zero means for each, is in CHAPTER_GUIDE section 5, "Enemy behaviours".</summary>
        public BehaviourTune tune;
    }
    /// <summary>The families of enemy behaviour (S/Combat/Enemy*.cs). Stored by number: append only.</summary>
    public enum EnemyBehaviourKind{None,Charger,Lobber,Ambusher,SwarmCloud,Guardian,Anchored,Tetherer,Strafer,Stomper,Burster,Leaper,Rider,Spinner,Tender}
    /// <summary>What a lingering area does to a hero standing in it. Slow and Blind are boundaries on the floor; Burn damages, so it is drawn as a danger (D217, D222). Append only.</summary>
    public enum AreaEffect{Slow,Burn,Blind}
    /// <summary>One behaviour's dials. Each family reads a few and leaves the rest; zero is "the family's own default" everywhere.</summary>
    [System.Serializable] public struct BehaviourTune
    {
        /// <summary>How far the move carries: a charge's or a pass's travel, a root line's length, a tether's reach.</summary>
        public float reach;
        /// <summary>A lane's width, in metres.</summary>
        public float width;
        /// <summary>How fast the move travels (a charge, a pass, a withdrawal), or how fast a tether reels its catch in. Metres a second.</summary>
        public float speed;
        /// <summary>How long it is open afterwards: stuck in a wall, winded after a miss, out of its hide, breaking off a pass. Seconds.</summary>
        public float recover;
        /// <summary>How long what it leaves lasts: a cloud on the floor, a latch, a tether. Seconds; zero leaves nothing where leaving something is optional.</summary>
        public float linger;
        /// <summary>The radius of what it leaves, in metres (zero: its attack's radius).</summary>
        public float area;
        /// <summary>The share of walking pace kept by a hero it has slowed (a cloud, a latch, a tether): .55 is a slog, 1 is no slowing.</summary>
        public float pace;
        /// <summary>What a lingering area does, and for a Burn the damage it does each second.</summary>
        public AreaEffect effect;public float tick;
        /// <summary>How many: an ambusher's strikes, a swarm's motes.</summary>
        public int count;
        /// <summary>An angle in degrees: a guardian's guarded front, an anchored lash's sweep.</summary>
        public float arc;
        /// <summary>The share of a blow a guardian's front lets through (.25: a quarter).</summary>
        public float guard;
        /// <summary>A radius it watches, in metres: an ambusher's hide (step inside and it erupts), a guardian's post (it won't be led further).</summary>
        public float wake;
        /// <summary>Its tell's own sound and pitch (empty: the family's).</summary>
        public string tellSound;public float tellPitch;
        /// <summary>The colour of what it is drawn as beyond its model: a swarm's motes, a lure's lamp. Unset (alpha 0): the family's own.</summary>
        public Color tint;
        /// <summary>An ambusher whose hide is in the open: it stands on show as the thing it mimics (a lamp-post), still and out of reach, instead of going under.</summary>
        public bool disguised;
        /// <summary>A guardian whose long move is a beam swept across a fan of this many degrees (chapter 6's Muster Excavator and its thaw-lance) instead of a line. Zero: the line.</summary>
        public float sweep;
        /// <summary>A guardian whose glass front throws a Beam shot back down a marked line at whoever fired it (chapter 7's Glass Sentinel): attacks[2] is the throw-back.</summary>
        public bool reflects;
        /// <summary>The party's line when this body's moment comes (a charger's plates shed, a rider's fibre lit, a guardian's shot thrown back, a tender at a clamp a hero is
        /// working): a bark situation. Empty: nothing is said.</summary>
        public string bark;
        /// <summary>A guardian that fights in a pair (chapter 8's Pikeguard): two woken together stand apart, face the party as one, one braces and thrusts while the other covers it, and
        /// neither strikes at a hero standing between them.</summary>
        public bool pair;
        /// <summary>A body too crude to shatter (chapter 8's Forge Hound): a leaper caught in the air by a blow is knocked down open instead of breaking apart.</summary>
        public bool heavy;
        /// <summary>A tetherer that hauls what it catches (chapter 9's Tow-drone): while its line holds a ship it flies off the way it was set facing at this many metres a second, the
        /// catch reeled along behind it, as it hauls hulls. Zero: it holds where it is.</summary>
        public float tow;
        /// <summary>An anchored body on a tether (chapter 9's Bridle Fragment): it never turns, and its lash is laid along the way it was set facing, away from what it is made fast to,
        /// whoever is where. Its tether side is never struck.</summary>
        public bool tethered;
        /// <summary>A guardian who is a person doing a job (chapter 9's Compact Warden): its guard broken, it gives ground, and at nought it stands aside and walks out of the fight
        /// instead of falling.</summary>
        public bool givesGround;
        /// <summary>A machine under somebody else's hand (chapter 9's Weld Arm): at nought the override is cut and its drawn body stays where it stood, at rest under its own lamps, while
        /// only the fighter goes.</summary>
        public bool standsDown;
    }
    /// <summary>How a body with no rig moves while it waits (ProceduralMotion). Stored by number: append only.</summary>
    public enum EnemyMotion{None,Hover,Skitter}
}
