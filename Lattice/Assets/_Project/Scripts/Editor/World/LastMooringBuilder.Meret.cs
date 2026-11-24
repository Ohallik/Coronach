using Lattice.Combat;
using Lattice.Data;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Meret Venn and the Continuance (brief, "The fights"): three phases, a checkpoint between each, no fourth bar, each phase its own boss with its own bar and door. Ship to ship in flight round the Continuance (MeretShip: her craft's shield returns a shot fired at the wrong moment, as a Flash Guard does; three shield projectors), on the frame gantry on foot (MeretGantry: the Saint's press, the Works' welding line, the Foundry's sluices, run by someone who understands them; two arm couplings), and in the control well (MeretWell: a duel, fast, precise and courteous; a counter stance that punishes mashing, clamp-traps that punish standing still, every attack marked and none new). At nought in the well she yields standing: she is not killed and does not fall off anything.
    /// <para>Owner: the Meret engineer (stream H). Their moves and every number: <c>S/Combat/BossController.MeretShip.cs</c> (<see cref="BossController.ShipTune"/>, the hull's clamps
    /// <see cref="MeretClamp"/>), <c>.MeretGantry.cs</c> (<see cref="BossController.GantryTune"/>, her couplings <see cref="MeretCoupling"/>, her arms <see cref="MeretFrame"/>) and <c>.MeretWell.cs</c>
    /// (<see cref="BossController.WellTune"/>); her three sheets and her tests in <c>T/PlayMode/LastMooringBossTests.cs</c> (its Meret region). What each fight needs of its room:
    /// docs/maps/Continuance.md ("Boss arena contract", phase one) and docs/maps/StillwaterChamber.md (two and three). Section 6.5 of docs/chapters/ch10-wiring.md says what each checkpoint
    /// between them is.</para>
    /// <para>Sizing (D296's ledger: the party meets her ship at Sync 28.8, the gantry at 29.1, the well at 29.5, so each sheet says 29). At Sync 29 a hero's bar is 624, at 30 642; in the
    /// Choirwrought parts the chapter is fought in Taren's Plating is 118 (120 at 30) and Idra's 152 (155), and a hit lands at 100/(100 + Plating - 10) of itself. So an authored blow D costs
    /// Taren at 29 D x .4808 / 624 (16% allows D = 207) and Idra at 30 D x .4082 / 642 (12% needs D = 189): every blow of hers in all three phases is authored at 192 to 198, and her phases add
    /// nothing to it (LastMooringBossTests measures every one on all three heroes through Health.Receive). The ordinary road to her hits at 142 to 150.</para>
    /// <para>Length. Each sheet's bounds are 80 to 150 s (wiring 6.5: the three together about five minutes); each integrity is sized from the shared length test's flat out against her at
    /// Sync 29 (QaPlanCombatTests.ABossLastsTwoToFiveMinutesAtItsExpectedSync, BOSS_RATE) to last about 100 s at the assumed .3 of it and under 150 s at a quarter. The gantry's bar is also held at
    /// each third until a coupling opens, which its own test measures (LastMooringBossTests).</para>
    /// <para>XP (D296's ledger): 1,200, 1,300 and 1,400. The rare (the Stillwater Core) is the gantry's.</para>
    /// </summary>
    public static partial class LastMooringBuilder
    {
        internal static class Meret
        {
            /// <summary>Her three definitions, each with its Kit registered in the same change (an unregistered boss throws <c>BOSS_UNREGISTERED</c>). LastMooringBossTests pins each literal
            /// <c>e.integrity=</c> below to its asset.</summary>
            internal static void Define()
            {
                // Her name on the boss card and bar is the manifest's "DIRECTOR MERET VENN" in her craft and her frame, and "MERET VENN" in the well: the turn from the office to
                // the woman. It is 796 units wide at the card's 58 on a card of 740, so the card sets it a little smaller (GameHud.CardSizeFor); a name is not cut to suit a card.
                GameObject Body(string id,string fallbackFolder,string fallback)=>ZoneKit.Body("Enemies",id,fallbackFolder,fallback);
                LootTable Loot(string id,params LootEntry[] entries)=>ArenaBuilder.Asset<LootTable>(id,t=>t.entries=entries);
                LootEntry Drop(string id,int count,float chance)=>ProgressionBuilder.Drop(id,count,chance);
                AttackDef Attack(string id,float damage,float range,float telegraph,float cooldown,DamageType type,float radius=1)=>
                    ArenaBuilder.Asset<AttackDef>(id,a=>{a.damage=damage;a.range=range;a.telegraph=telegraph;a.cooldown=cooldown;a.type=type;a.radius=radius;a.projectile=false;});
                // ---- her craft never fights by its brain's own attack: every blow is a marked special (one at her weight that reaches nobody, so its brain holds still). The frame's
                // brain shoves at whoever is in front of it between moves, and the well's cuts, each marked by the brain as any blow is.
                var shipStill=Attack("MeretShipStill",194,0,1,99,DamageType.Beam);
                var frameShove=Attack("MeretFrameShove",192,2.4f,.8f,3.2f,DamageType.Kinetic,2);
                var wellCut=Attack("MeretWellCut",192,2.2f,.6f,2.4f,DamageType.Kinetic,1.6f);

                // ---- phase one: her craft (9 m), flown by its own file from her shelter on the Continuance's back. A Compact hull turns a kinetic round and a beam goes through it, as the
                // Cutter's did. Its anchor clamps are drawn from the AnchorClamp model (bodySegment). 3 m round in flight, 6 m tall.
                ArenaBuilder.Asset<EnemyDef>("MeretShip",e=>{e.id="MeretShip";e.displayName="DIRECTOR MERET VENN";e.prefab=Body("MeretShip","Enemies","DebrisCutter");
                    e.bodySegment=ZoneKit.Prefab("Environment","AnchorClamp");
                    // Sized on what the replay driver's pad gets of flat out (MeretReplayTests, November 15): her dome stands most of the fight, and the pair fired through its windows got .12
                    // of the 1,907 a second they deal flat out; 58,000 lasted 255 s, the dome up three quarters of it. With her shield's relays to shoot while it stands (ShipTune.Relays) the
                    // lead acts nineteen twentieths of the fight. Each window of her dome down gives up a twelfth of her bar at most (ShipTune.WindowShare), so her fight is about as many windows
                    // as the player makes: 40,000 is about two minutes through the pad (a window every seven seconds or so, most of them worth their twelfth), and 66 to 87 s at a quarter of flat
                    // out (1,817 to 2,419 a second on nine runs).
                    e.integrity=40000;e.breakThreshold=900;e.speed=0;e.attacks=new[]{shipStill};
                    e.weakness=DamageType.Beam;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.None;e.xp=1200;e.scrip=420;e.scrap=20;e.boss=true;
                    e.bossMusicId="Final Stand";e.yieldsOnDefeat=false;e.heavyDeath=true;e.drawnScale=1;e.motion=EnemyMotion.Hover;
                    e.capsuleRadius=0;e.capsuleHeight=6;
                    e.lootTable=Loot("MeretShip",Drop("AnchorThread",4,1),Drop("ScrapAlloy",8,1),Drop("ChargeCell",2,1),ProgressionBuilder.AnyPart(5,GearSlot.Drive,1));});

                // ---- phase two: her heavy frame (3.2 m, rigged) with the ship's maintenance arms on it. A machine's controls take a shock, and it is built for heat. It walks, slowly.
                ArenaBuilder.Asset<EnemyDef>("MeretGantry",e=>{e.id="MeretGantry";e.displayName="DIRECTOR MERET VENN";e.prefab=Body("MeretGantry","Enemies","CompactWarden");
                    // Sized on what the replay driver's pad gets of flat out (MeretReplayTests, November 15; her sheet's share), every third of her bar paid in blows (a coupling only lets a held
                    // line through, D281). Hard to break (900 broke her nine times in 92 s, a third of the fight standing stunned): she is the frame that out-builds you, and fights.
                    // Through the pad, a third in blows on the plated frame, a held line, a collar, and again: 110,000 lasted 118 to 148 s on five runs (.20 to .27 of flat out). 104,000 is 112 to
                    // 140 s on those, and 125 s at a quarter of the lowest flat out read for her (3,338 to 3,786 a second).
                    e.integrity=122000;e.breakThreshold=2000;e.speed=2.2f;e.attacks=new[]{frameShove};
                    e.weakness=DamageType.Pulse;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.None;e.xp=1300;e.scrip=480;e.scrap=24;e.boss=true;
                    e.bossMusicId="Final Stand";e.yieldsOnDefeat=false;e.heavyDeath=true;e.drawnScale=1;
                    e.capsuleRadius=0;e.capsuleHeight=3.2f;
                    e.lootTable=Loot("MeretGantry",Drop("StillwaterCore",1,1),Drop("AnchorThread",4,1),Drop("RepairSalve",2,1),ProgressionBuilder.AnyPart(5,1));});

                // ---- phase three: her emergency frame, a coat on her own body (1.85 m, rigged). A person: she yields standing. Her coat grounds a shock and a torch cuts it.
                // At play size she was a pale figure a hero's height with no face or fronds to make out (nov22b-whole s67/117): her boss body wears her emergency frame (MeretFrame,
                // D373), and her body takes a lilac rim of her own that the chamber's light isn't. The woman who yields afterwards wears her plain body: the frame is what she puts down.
                ChapterOneLook.BodyReads("MeretWell",MeretFrame.Rim,MeretFrame.RimUnlit,MeretFrame.LightFloor,MeretFrame.RimPower);
                ArenaBuilder.Asset<EnemyDef>("MeretWell",e=>{e.id="MeretWell";e.displayName="MERET VENN";e.prefab=MeretFrame.Director(ZoneKit.Prefab("Enemies","MeretWell"))??Body("MeretWell","Characters","MeretBody");
                    // Sized on what the replay driver's pad gets of flat out (MeretReplayTests, November 15; her sheet's share), under the shared ceiling (a quarter of flat out inside 150 s).
                    // Hard to break (750 broke her six times in a 70 s duel, a quarter of it standing stunned): the woman who will not stop doesn't stand about. 150,000 lasted 104 and 92 s
                    // through the pad (.33 and .42 of 4,328 and 3,887 a second): 140,000 stays under the ceiling down to 3,733 a second flat out.
                    e.integrity=190000;e.breakThreshold=2200;e.speed=3.8f;e.attacks=new[]{wellCut};
                    e.weakness=DamageType.Plasma;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.None;e.xp=1400;e.scrip=0;e.scrap=0;e.boss=true;
                    e.bossMusicId="Final Stand";e.yieldsOnDefeat=true;e.heavyDeath=false;e.drawnScale=1;
                    e.capsuleRadius=0;e.capsuleHeight=2;
                    e.lootTable=null;});
            }
        }
    }
}
