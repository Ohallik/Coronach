using Lattice.Data;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 5's enemies (brief, "The fight: Hulljack"; manifest section 5 for the ids and names): the one ordinary kind, the Claim Hauler that Dessik Varn's tug launches, and the two
    /// parts of the chapter's one fight, Hulljack, the salvage frame on the hull (part one, in flight, ManyHandsHull: BossController.Hulljack.cs) and Dessik Varn in the same frame with its
    /// feet cut, in the freight hall (part two, on foot, ManyHands: BossController.DessikVarn.cs). The combat engineer's file: every number a fight is tuned by is here or in those two
    /// files' own tables, and nowhere else. The rooms they need: docs/maps/ManyHandsHull.md, "Boss arena contract" and "Freight hall contract".
    /// <para>Sizing. The party arrives at Sync 15 in Prime parts (DevLoadout chapter5: the plain Prime frame, Plating 26) and leaves at 16. A hero's bar is 120 + 18 per Sync past the
    /// first: 372 at 15, 390 at 16. Plating is 10 + 2 per Sync past the first + the frame's 26: 64 and 66, and a hit lands at 100/(100 + Plating - 10) of itself: .649 and .641. So at a
    /// zone threat of 1 an authored hit of D costs D x .649 / 372 of the bar at Sync 15 and D x .641 / 390 at 16: 9% of the bar at 16 needs D = 55, 13% at 15 allows D = 74. The hauler's
    /// tether is 58 (10.1% at 15, 9.5% at 16), and both zones of the chapter stay at threat 1. A boss hits harder than the road to it (CHAPTER_GUIDE 0.1: 12 to 16% of the bar, in
    /// every phase a move is used): each part has one shift, at half, which adds 18%, so every blow of the fight is authored 70 to 76 (12.2 to 13.3% at 15) and lands at most 76 x 1.18
    /// = 90 (15.7%) in the second half; the cargo throw, the hall's second half only, is 74 (15.2% there). ManyHandsCombatTests measures every one of these through Health.Receive.</para>
    /// <para>XP. Sync 15 to 16 costs 1,875 a hero. The main quest pays 1,100, so the fight is sized to carry the rest with nothing optional done: the hull's three haulers 114, about four of the
    /// frame's own 152, the frame 320 and Dessik 360, 946 in all, which ends the chapter just past Sync 16. The side quests' 660 carry a thorough player a third of the way to 17.</para>
    /// </summary>
    public static partial class ManyHandsBuilder
    {
        static void DefineEnemies()
        {
            GameObject Body(string id,string fallback)=>ZoneKit.Body("Enemies",id,"Enemies",fallback);
            LootTable Loot(string id,params LootEntry[] entries)=>ArenaBuilder.Asset<LootTable>(id,t=>t.entries=entries);
            LootEntry Drop(string id,int count,float chance)=>ProgressionBuilder.Drop(id,count,chance);
            AttackDef Attack(string id,float damage,float range,float telegraph,float cooldown,DamageType type,float radius=1,bool projectile=false)=>
                ArenaBuilder.Asset<AttackDef>(id,a=>{a.damage=damage;a.range=range;a.telegraph=telegraph;a.cooldown=cooldown;a.type=type;a.radius=radius;a.projectile=projectile;});

            // ---- the attacks, at a zone threat of 1. Range is where the move starts from (a tether's hold, a swipe's reach), not always how far it reaches.
            var haulTether=Attack("HaulTether",58,13,.85f,3.6f,DamageType.Pulse);
            var hullSwipe=Attack("HullSwipe",70,7,.75f,2.6f,DamageType.Kinetic);
            var suitSwipe=Attack("SuitSwipe",70,4.2f,.7f,2.4f,DamageType.Kinetic);

            // ---- the hull (flight)
            // Tetherer, Hulljack's: the claim drone's heavier sister on the same body, launched from the tug two at a time as the frame leaps, and three of them tagging the raft bay's hatch
            // before the fight. It holds off at 13 m, marks a line .85 s, and what it fires holds a ship at .45 of its speed and reels it in to itself at 4 m/s, until the ship rolls, the
            // hauler is hit, a boom comes between them or four seconds pass. Answer: roll the line off, or one shot to cut it; a boom between you is a wall to it.
            ArenaBuilder.Asset<EnemyDef>("ClaimHauler",e=>{e.id="ClaimHauler";e.displayName="Claim Hauler";e.prefab=Body("ClaimHauler","ClaimDrone");e.integrity=640;e.breakThreshold=160;e.speed=6;e.attacks=new[]{haulTether};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.Tetherer;
                e.tune=new BehaviourTune{pace=.45f,speed=4,linger=4,reach=15};
                e.xp=38;e.scrip=14;e.scrap=3;e.boss=false;e.motion=EnemyMotion.Hover;e.drawnScale=1.3f;e.heavyDeath=true;
                e.lootTable=Loot("ClaimHauler",Drop("ScrapAlloy",2,.6f),Drop("ChargeCell",1,.12f),ProgressionBuilder.AnyPart(3,GearSlot.Drive,.05f));});

            // ---- the fight: about three minutes in all for a Sync 15 party in the chapter's arrival parts (DevLoadout chapter5), a minute and a half each part. Measured November 12
            // (QaPlanCombatTests and ManyHandsCombatTests pin these literals to the assets): flat out the party lands 1,142 a second on the frame in flight and 1,679 on the suit on foot.
            // The frame's 30,000 is 91 s at the flight driver's .29 of that and 105 s at a quarter; on the hull it goes a quarter a foot, and each foot (a twentieth of its integrity,
            // struck only while planted) is one plant's opening, so the four feet and three leaps come to about 97 s. The suit's 44,000 is 87 s at the driver's .3 and 105 s at a quarter.
            // Each part's sheet holds it to 60 to 150 s. A foot's share and its quarter, like every other number of the moves, are in the two BossController files' own tables. Both parts
            // drop Prime parts; the frame is salvage, and pays in it.
            ArenaBuilder.Asset<EnemyDef>("Hulljack",e=>{e.id="Hulljack";e.displayName="HULLJACK";e.prefab=Body("Hulljack","SentinelHusk");e.integrity=30000;e.breakThreshold=900;e.speed=0;e.attacks=new[]{hullSwipe};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.None;e.xp=320;e.scrip=240;e.scrap=14;e.boss=true;
                e.bossMusicId="Glitch Boss";e.drawnScale=1.3f;e.heavyDeath=true;
                // A capsule can't stand shorter than it is wide (the Rotbloom hung 1.8 m in the air): its registration makes it 2.4 m round, so 5 m tall.
                e.capsuleHeight=5;
                e.lootTable=Loot("Hulljack",Drop("ScrapAlloy",8,1),Drop("LatticeFilament",3,1),Drop("ChargeCell",2,1),ProgressionBuilder.AnyPart(3,1));});
            ArenaBuilder.Asset<EnemyDef>("DessikVarn",e=>{e.id="DessikVarn";e.displayName="DESSIK VARN";e.prefab=Body("DessikVarn","SentinelHusk");e.integrity=64000;e.breakThreshold=700;e.speed=0;e.attacks=new[]{suitSwipe};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.None;e.xp=360;e.scrip=260;e.scrap=16;e.boss=true;e.yieldsOnDefeat=true;
                e.bossMusicId="Glitch Boss";e.drawnScale=1;e.heavyDeath=true;
                // 1.8 m round by its registration, so 4 m tall.
                e.capsuleHeight=4;
                e.lootTable=Loot("DessikVarn",Drop("ScrapAlloy",8,1),Drop("LatticeFilament",3,1),Drop("RepairSalve",2,1),ProgressionBuilder.AnyPart(3,1));});
        }
    }
}
