using Lattice.Data;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 9's bosses (brief, "Enemies"; manifest section 5 for every id and name): the Foundry Saint in the Separation Works' press hall and the Debris Cutter in the Broken Ring, ids exactly
    /// <c>FoundrySaint</c> and <c>DebrisCutter</c> (their barks are found by id). The second combat engineer's file. Their moves are <c>BossController.FoundrySaint.cs</c> and
    /// <c>BossController.DebrisCutter.cs</c>, each with its own table of numbers; the rooms they need are docs/maps/SeparationWorks.md ("Boss arena contract") and docs/maps/BrokenRing.md
    /// ("Cutter arena contract"). Called by <see cref="HomeBuilder"/>.Define after <see cref="Enemies"/>.Define, so a boss can name an ordinary enemy (neither does).
    /// <para>Sizing. The party arrives at Sync 25 in Tempered parts (DevLoadout chapter9: the plain Tempered frame, Plating 38) and leaves at 27; the chapter's own stalls (the Crawl's, the Decks')
    /// sell Choirwrought (its frame's Plating 52), which a player may be wearing by either fight. It meets the Saint at about 26 (the Works come after Nacre, the Crawl and the meeting) and the
    /// Cutter at about 26 to 27 (the chapter's last fight), so the Saint is measured at 25 and 26 and the Cutter at 26 and 27, each in both. A hero's bar is 120 + 18 per Sync past the first: 552
    /// at 25, 570 at 26, 588 at 27. Taren's and Sela's Plating is 10 + 2 per Sync past the first + the frame's, Idra's 16 + 3 per Sync past the first + the frame's, and a hit lands at
    /// 100/(100 + Plating - 10) of itself. So at threat 1 an authored hit of D costs Taren at 25 in Tempered D x .5376 / 552 (the most anyone pays at the Saint: 16% allows D = 164) and Idra at 26
    /// in Choirwrought D x .4292 / 570 (the least: 12% needs D = 160); for the Cutter, Taren at 26 in Tempered D x .5319 / 570 (16%: D = 171) and Idra at 27 in Choirwrought D x .4237 / 588
    /// (12%: D = 167). Every move of the Saint that it has in both phases is authored at 161 to 163, and its one move of the second (the Sweep, with the phase's +18%) at 137; every move of the
    /// Cutter at 168 to 170. The heaviest ordinary hit of the chapter is the Compact Warden's 131 (HomeBuilder.Enemies.cs). HomeBossTests measures every one of them on all three heroes in both
    /// parts through Health.Receive.</para>
    /// <para>Length (November 14). Neither bar is handed over in pieces: an arm or a pod decides which goes and when, never how much, and both bars are paid in blows (D272: a boss whose parts
    /// take bar with them gave the replay party far more than the .3 its sheet assumed). The Saint's press takes nothing while its raised arms bear it, only its unlocked collars and its open press
    /// (the head's lift, a break) and, bare, the rig from up on the arms; so it is held by its openings, and HomeBossTests sizes it from them on the pair's own rate at a planted boss's share (.45,
    /// D272's .36 to .57). The Cutter is an honest flying bar; its sheet carried the .29 the Cantor and the Hulljack measured until the driver was given the hands this fight asks for (it
    /// keeps 18 m off and changes lane whenever the lead comes nearer: flown after brake off and fired on wherever it is in reach, ContinuousReview.Home.cs). Fought that way by the arrival
    /// pair in the Broken Ring (HomeReplayHandsTests) it gave .35 to .41 of flat out (123,000 fell in 144 s), so its sheet says .35 and it is 160,500 (155,000 until November 20, 159,000 for a day: read each from a game just started, two minute-long readings and their mean, twice over on November 21, the pairs stand at 3,800 and 3,895 a second for Taren and Sela, 3,593 and 3,595 for Sela and Idra and 2,339 and 2,359 for Taren and Idra, and a third run read Taren and Sela at 3,932 and Taren and Idra at 2,264 while the shared length test read the arrival pair at 2,213, so the room between the fastest pair's two-minute floor and the slowest reading's five minutes at a quarter of flat out is 155,500 to 165,800 and 160,500 is its middle, three per cent inside each; the earlier numbers were read one pair after another in one game, where a pair's rate climbs with the minutes already fought): about three minutes for the arrival
    /// pair (DECISIONS, November 14, the Cutter). HomeBossTests measures both on the chapter's loadout for every pair, and the integrities sit inside the shared length test's floor and ceiling (QaPlanCombatTests: at least 120 s
    /// at the sheet's share of flat out, at most 300 s at a quarter of it). The arithmetic is in each length test's summary and in its log lines.</para>
    /// <para>XP and drops (settled November 14 with the roster's ledger, HomeBuilder.Enemies.cs, which counts them against Sync 25 to 27): the Saint pays 1,600 and drops the region's rare, the
    /// Ring Keystone (only it, and Tam's quest, give one); the Cutter 1,350. With the main path's ordinary fights and the chapter's quests that is the 6,375 Sync 25 to 27 costs.</para>
    /// </summary>
    public static partial class HomeBuilder
    {
        internal static class Bosses
        {
            internal static void Define()
            {
                GameObject Body(string id,string fallback)=>ZoneKit.Body("Enemies",id,"Enemies",fallback);
                LootTable Loot(string id,params LootEntry[] entries)=>ArenaBuilder.Asset<LootTable>(id,t=>t.entries=entries);
                LootEntry Drop(string id,int count,float chance)=>ProgressionBuilder.Drop(id,count,chance);
                AttackDef Attack(string id,float damage,float range,float telegraph,float cooldown,DamageType type,float radius=1)=>
                    ArenaBuilder.Asset<AttackDef>(id,a=>{a.damage=damage;a.range=range;a.telegraph=telegraph;a.cooldown=cooldown;a.type=type;a.radius=radius;a.projectile=false;});

                // ---- neither fights by its brain's own attack: every blow either lands is a marked special. Each has one at boss weight that reaches nobody (range 0), so its brain stands still.
                var pressStill=Attack("SaintStill",162,0,1,99,DamageType.Kinetic);
                var cutterStill=Attack("CutterStill",170,0,1,99,DamageType.Beam);

                // ---- the Foundry Saint. The Halo's own separation press under a Compact override: six arms released at their collars, then a climb to the rig on its back. An old machine's
                // controls take a shock, and its frame is built to take heat. Bolted to its hall: a Mine to its brain (it never walks or turns). Its capsule is 2.6 m round and 12 m tall, the
                // press's own height. Its arms are drawn from the SaintArm model (bodySegment) and the rig on its back from the Override Rig's (tailSegment).
                ArenaBuilder.Asset<EnemyDef>("FoundrySaint",e=>{e.id="FoundrySaint";e.displayName="THE FOUNDRY SAINT";e.prefab=Body("FoundrySaint","SlagColossus");
                    e.bodySegment=ZoneKit.Prefab("Environment","SaintArm");e.tailSegment=ZoneKit.Prefab("Enemies","OverrideRig");
                    e.integrity=140000;e.breakThreshold=1400;e.speed=0;e.attacks=new[]{pressStill};
                    e.weakness=DamageType.Pulse;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.None;e.xp=1600;e.scrip=520;e.scrap=26;e.boss=true;
                    e.bossMusicId="Glitch Boss";e.yieldsOnDefeat=false;e.heavyDeath=true;e.drawnScale=1;e.motion=EnemyMotion.None;
                    e.capsuleRadius=0;e.capsuleHeight=12;
                    e.lootTable=Loot("FoundrySaint",Drop("RingKeystone",1,1),Drop("HaloRivet",6,1),Drop("NacreShard",4,1),Drop("RepairSalve",2,1),ProgressionBuilder.AnyPart(5,1));});

                // ---- the Debris Cutter. A Compact interceptor, the Interceptor's hull at twice its size, with three weapon pods; it ends dead in space and whole. A Compact hull is shielded
                // against a shock and its plate turns a kinetic round; a beam goes through it. Flown by its own file: a Mine to its brain. 2.6 m round in flight, 5.6 m tall.
                ArenaBuilder.Asset<EnemyDef>("DebrisCutter",e=>{e.id="DebrisCutter";e.displayName="THE DEBRIS CUTTER";e.prefab=Body("DebrisCutter","DraskSkiff");
                    e.integrity=210000;e.breakThreshold=800;e.speed=0;e.attacks=new[]{cutterStill};
                    e.weakness=DamageType.Beam;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.None;e.xp=1350;e.scrip=440;e.scrap=20;e.boss=true;
                    e.bossMusicId="Alien Boss Battle";e.yieldsOnDefeat=false;e.heavyDeath=true;e.drawnScale=1;e.motion=EnemyMotion.Hover;
                    e.capsuleRadius=0;e.capsuleHeight=5.6f;
                    e.lootTable=Loot("DebrisCutter",Drop("HaloRivet",4,1),Drop("ScrapAlloy",8,1),Drop("ChargeCell",2,1),ProgressionBuilder.AnyPart(5,GearSlot.Drive,1));});
            }
        }
    }
}
