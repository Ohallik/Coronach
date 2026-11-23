using Lattice.Data;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 8's bosses (brief, Enemies; manifest section 5 for every id and name): the Slag Colossus in the Bridle Foundry's heart, and Warmaster Drask Vaal, fought on Harrowgate's
    /// muster field and then in his skiff over the war harbour. Their moves are <c>BossController.SlagColossus.cs</c>, <c>BossController.DraskVaal.cs</c> and
    /// <c>BossController.DraskSkiff.cs</c>, each with its own table of numbers; the rooms they need are docs/maps/BridleFoundry.md ("Boss arena contract") and docs/maps/Harrowgate.md
    /// ("Duel ground contract", "Pursuit contract"). Called last by <c>DefineEnemies</c> (<c>HarrowgateBuilder.Enemies.cs</c>), after the ordinary enemies, so a boss can name one
    /// (the guard wing Drask calls is the <c>MusterPikeguard</c>).
    /// <para>Sizing. The party arrives at Sync 22 in Tempered parts (DevLoadout chapter8: the plain Tempered frame, Plating 38) and leaves at 25. It meets the Colossus at about 23 and
    /// Drask at about 24, so the Colossus is measured at 23 and 24 and both of Drask's parts at 24 and 25. A hero's bar is 120 + 18 per Sync past the first: 516 at 23, 534 at 24, 552
    /// at 25. Taren's and Sela's Plating is 10 + 2 per Sync past the first + the frame's (92 at 23, 96 at 25), Idra's 16 + 3 per Sync past the first + the frame's (120 at 23, 126 at 25),
    /// and a hit lands at 100/(100 + Plating - 10) of itself. So at threat 1 an authored hit of D costs Taren at 23 D x .5495 / 516 (the most anyone pays at the Colossus: 16% allows
    /// D = 150) and Idra at 24 D x .4695 / 534 (the least: 12% needs D = 137); for Drask, Taren at 24 D x .5435 / 534 (16%: D = 157) and Idra at 25 D x .4630 / 552 (12%: D = 143).
    /// Every move of the Colossus that it has in both halves is authored between 138 and 148, and its one move of the second half (the Spill, with the half's +18%) at 122; Drask's
    /// between 144 and 152, his Lunge (second half only) at 126; the skiff's between 144 and 152, its gun call (fired in both halves) at 146. HarrowgateBossTests measures every one
    /// of them on all three heroes through Health.Receive.</para>
    /// <para>Length. Each integrity is the party's measured rate times the time the fight should take (HarrowgateBossTests pins the literals to the assets and holds each fight to its
    /// time for every pair a player can put out), flat out on a body that never moves. Re-measured November 14, after the shared length test (QaPlanCombatTests) was corrected to
    /// stand a blade-carrying second hero at the body: until then it stood Idra 8 m off, where her hammer reaches nothing, and every boss since chapter 7 was sized on about half of
    /// what the pair deals. Read only at 50 frames a second or more (a busy machine reads a party slow); at sixty, one reading to the next still moves a fifth. The Colossus there:
    /// 2,155 to 2,713 a second at Sync 23 (it was 1,153 to 1,474), so 82,000 lasted 121 s at the driver's .3, on the 120 s floor. The rest of its three minutes is the room: its
    /// plates and arms are integrity of their own (5% and 15% of its bar each, ColossusTune), its body is fought from five eighths down, and four sluice cycles (one a plate, one for a
    /// plate left glowing) of about twelve seconds. With both heroes behind it a pair lands 1,919 to 2,794 a second (Idra at its back finds no weak spot, where the shared measure's
    /// Idra at its flank does). It is 100,000, held three ways: 123 to 155 s on the shared measure (the floor 120), a dark plate broken in 8.7 s at the slowest pair's .3 (its dark is
    /// 9), and the arrival pair's whole fight 186 to 208 s (its own bound 220). That window is narrow for readings that move a fifth: a longer dark or lighter arms would widen it,
    /// and a share measured off a chain (its own model puts the driver near .2 of the bare body's rate, a third of the fight being the room) would settle it. Drask at Sync 24: 3,655
    /// to 4,348 a second on the corrected shared measure (it was 2,451 to 3,201), so 86,000 lasted 71 to 77 s against his sheet's 80. With the lead at his shield and the partner at
    /// his back, as the duel goes (HarrowgateBossTests), Taren and Idra land 3,708 to 3,779 a second (Taren before him 2,171 to 2,316, Idra behind 1,462 to 1,537), Taren and Sela
    /// 2,921 to 3,434 and Sela and Idra 2,567 to 3,139 (her shots into his shield 950 to 1,455; until November 14 the yard had her facing away from him and measured that pair as Idra
    /// alone, 1,478 to 1,945, which made it look twice as slow as the rest). 130,000 is 100 to 119 s on the shared measure (142 at a quarter, against 200) and 115 to 117, 126 to 148
    /// and 138 to 169 s for those three pairs at the driver's .3: the slowest at least 15% under his sheet's 200, the fastest two fifths over its 80. The skiff flying at 24: 2,268 to
    /// 2,533 a second on the shared measure (flight was never affected), 2,174 to 3,092 for the pairs; 70,000 is 95 to 106 s on the shared measure and 99 to 132 s with its three runs.</para>
    /// <para>XP and drops. The Colossus pays 1,800 and drops the region's rare, the Foundry Seal (only it drops one); the duel 1,200 and the pursuit 1,000. The first combat engineer's
    /// ledger (HarrowgateBuilder.Enemies.cs) counts these against Sync 22 to 25.</para>
    /// </summary>
    public static partial class HarrowgateBuilder
    {
        static void BossDefinitions()
        {
            GameObject Body(string id,string fallback)=>ZoneKit.Body("Enemies",id,"Enemies",fallback);
            LootTable Loot(string id,params LootEntry[] entries)=>ArenaBuilder.Asset<LootTable>(id,t=>t.entries=entries);
            LootEntry Drop(string id,int count,float chance)=>ProgressionBuilder.Drop(id,count,chance);
            AttackDef Attack(string id,float damage,float range,float telegraph,float cooldown,DamageType type,float radius=1,bool projectile=false)=>
                ArenaBuilder.Asset<AttackDef>(id,a=>{a.damage=damage;a.range=range;a.telegraph=telegraph;a.cooldown=cooldown;a.type=type;a.radius=radius;a.projectile=projectile;});

            // ---- the attacks the brain throws between specials, at boss weight (a boss stays at threat 1): the Colossus's foot, the Warmaster's lance jab, the skiff's bow guns.
            var colossusTread=Attack("ColossusTread",140,5.6f,.9f,2.8f,DamageType.Kinetic,3);
            var warmasterJab=Attack("WarmasterJab",146,3.2f,.7f,2.4f,DamageType.Kinetic,1.8f);
            var skiffGuns=Attack("SkiffGuns",144,22,.8f,3.2f,DamageType.Plasma,1,true);

            // ---- the Slag Colossus. A walking crucible: three heat plates make it too hot to hurt until the heart's sluices darken and the party breaks them, then two pour-arms hold
            // its bar at half, then it comes apart on its own slag. A furnace shrugs heat and cracks under shock. A 3 m round body by its registration, so 7 m tall, its own height.
            ArenaBuilder.Asset<EnemyDef>("SlagColossus",e=>{e.id="SlagColossus";e.displayName="THE SLAG COLOSSUS";e.prefab=Body("SlagColossus","Rotbloom");e.integrity=135000;e.breakThreshold=900;e.speed=1.4f;e.attacks=new[]{colossusTread};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.None;e.xp=1800;e.scrip=480;e.scrap=24;e.boss=true;
                e.bossMusicId="Glitch Boss";e.yieldsOnDefeat=false;e.heavyDeath=true;e.drawnScale=1;
                e.capsuleHeight=7;
                e.lootTable=Loot("SlagColossus",Drop("FoundrySeal",1,1),Drop("BridleWire",6,1),Drop("ScoriaGlass",6,1),Drop("RepairSalve",2,1),ProgressionBuilder.AnyPart(4,1));});

            // ---- Warmaster Drask Vaal, part one: the duel on the muster field. A person in a war-frame, lance and shield (.9 m round, 2.5 m tall: his body's height). He yields; he isn't
            // killed. His frame grounds a shock and a torch cuts it. 165,000 (November 15): fought through the pad by chapter 8's floor party (Tempered parts) he fell at 150,000 in 84 and 85 s,
            // a share of .49 to .50 and not the .42 an under-geared replay party had measured; at that share the duel test's arrival pair stood at 77 s, under his 80. At 165,000 the
            // same fight took 91.8 and 110.3 s, a share of .41 to .47 (FloorPartyBossReplayTests).
            // His body is his own (the town engineer's, November 21: HarrowgateBuilder.TownLook's Warmaster): his war-frame under a crimson mantle, drawn a head over his guard wing. Until
            // then he was the guard wing's own armoured shape, told apart only by the lock box (s52/085). What he hits and is hit by is his capsule, below, and didn't change.
            ArenaBuilder.Asset<EnemyDef>("DraskVaal",e=>{e.id="DraskVaal";e.displayName="WARMASTER DRASK VAAL";e.prefab=Body("DraskWarmaster","DraskVaal");e.integrity=225000;e.breakThreshold=750;e.speed=3.4f;e.attacks=new[]{warmasterJab};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.None;e.xp=1200;e.scrip=300;e.scrap=12;e.boss=true;
                e.bossMusicId="Alien Boss Battle";e.yieldsOnDefeat=true;e.heavyDeath=true;e.drawnScale=e.prefab!=null&&e.prefab.name=="DraskWarmaster"?Town.WarmasterScale:1;
                e.capsuleHeight=2.5f;
                e.lootTable=Loot("DraskVaal",Drop("BridleWire",4,1),Drop("ScoriaGlass",4,1),Drop("RepairSalve",1,1),ProgressionBuilder.AnyPart(4,GearSlot.Edge,1));});

            // ---- Drask Vaal, part two: his war-skiff over the war harbour, running for the carrier. The Lancer's hull in his colours, half again its size (2.4 m round in flight, 5 m tall).
            // Brought down, it limps back to the muster field.
            ArenaBuilder.Asset<EnemyDef>("DraskSkiff",e=>{e.id="DraskSkiff";e.displayName="THE WARMASTER'S SKIFF";e.prefab=Body("DraskSkiff","MusterSkiff");e.integrity=70000;e.breakThreshold=800;e.speed=0;e.attacks=new[]{skiffGuns};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.None;e.xp=1000;e.scrip=260;e.scrap=14;e.boss=true;
                e.bossMusicId="Alien Boss Battle";e.yieldsOnDefeat=false;e.heavyDeath=true;e.drawnScale=1;e.motion=EnemyMotion.Hover;
                e.capsuleHeight=5;
                e.lootTable=Loot("DraskSkiff",Drop("ScrapAlloy",8,1),Drop("BridleWire",3,1),Drop("ChargeCell",2,1),ProgressionBuilder.AnyPart(4,GearSlot.Drive,1));});
        }
    }
}
