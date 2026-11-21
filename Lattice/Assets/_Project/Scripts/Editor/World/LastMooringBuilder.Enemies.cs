using Lattice.Data;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 10's ordinary enemies and the flight fights' rosters (brief: "Compact crews hold it: some fight, some stand aside, a few help"; "nothing new is introduced"). The first combat
    /// engineer's file. Every enemy here is one the player has met, in the Compact's white and teal, at the last chapter's weight, and most of them now work in twos: wardens that cover each
    /// other, clamp guns whose curtains cross, interceptors that pin for each other. Each is one family of the behaviour library (Lattice.Combat.EnemyBehaviour; CHAPTER_GUIDE section 5),
    /// tuned by data; no family is new and no body is new. Every number a fight here is tuned by is in this file. <c>LastMooringBuilder.Define</c> calls <see cref="Enemies.Define"/> first,
    /// so a boss can name one. A zone names one through <see cref="Foe"/>; what each zone gives them is that zone's map doc, under its contract (docs/maps/SorrelSecondSky.md "Flight fights
    /// contract", docs/maps/Continuance.md "Crews contract"); the places not yet started have their rosters in docs/chapters/ch10-wiring.md section 5.
    /// <para><b>Why new ids.</b> Chapter 9's crews and machines are authored at 126 to 131, sized for Sync 25 to 27 in Tempered parts. At this chapter's Sync 28 and 29 in Choirwrought they
    /// fall under 9% of Idra's bar (the Warden's baton, 128, is 8.8% at 28 and 8.5% at 29), so each comes back under a Stillwater id at its new weight, drawn in the same body and named
    /// the same on screen (a new id is a new weight, guide 5). Chapter 9's own definitions are untouched: chapter 9 still fights them.</para>
    /// <para>The roster, where each fights and how it is answered.
    /// <b>Compact Warden</b> (<c>StillwaterWarden</c>; the Continuance's mess and works): chapter 9's guardian that gives ground, now in a <b>pair</b> (chapter 8's mode): one braces its shield
    /// drive down a marked line while the other covers it, plate to the hero, and neither strikes across the other; guard broken, one backs off and stands open; at nought it stands aside
    /// and walks out. Answer: get between them, or round the one covering, and break it while its partner's drive is spent.
    /// <b>Weld Arm</b> (<c>StillwaterWeldArm</c>; the anchor works): lays a weld down its marked strip, the seam burns after; at nought it stands down at rest. Step off the strip, strike from beside.
    /// <b>Override Rig</b> (<c>StillwaterRig</c>; the anchor works): discharges all round its clamp on a marked ring. Strike between discharges, or shoot it from outside.
    /// <b>Interceptor</b> (<c>StillwaterInterceptor</c>; Sorrel's valley, Last Mooring's lanes between the chains): a pair, one pinning a ship with a tractor line while the other runs a marked
    /// lane through it. Break the line (a roll, a shot on the pinner, a chain between you) and cross out of the lane.
    /// <b>Tow-drone</b> (<c>StillwaterTowDrone</c>; the tug in the passage, three): harpoons a ship down a marked line and hauls it the way it was set facing, away from the tug. Roll the line, or
    /// one shot on the drone.
    /// <b>Clamp Gun</b> (<c>ClampGun</c>; two at each of Sorrel's four clamps, two across Last Mooring's gap, the Tollkeeper's outriggers if its engineer wants them): a curtain gun (chapter 8's
    /// Flak Gate's family, in its body), set in pairs whose curtains <b>cross</b>: each bursts flak in alternate rows of its curtain on a clock, so behind one gun is in front of the other and
    /// there is no lee; where their quiet rows cross is the cell to sit in on the double beat, and on the single beat everything but the middle rows is quiet. Read the clock, or break them
    /// (Kinetic).
    /// <b>Clamp Tender</b> (<c>StillwaterTender</c>; on Sorrel's shelves, stepped out by Mother of Doors' fight as the party docks): works the clamp shut while the lead holds it, heeding
    /// nobody; with nothing to keep, a torch on a marked fan. Fragile: the partner's to stop.</para>
    /// <para>Sizing. The party arrives at Sync 27 in Choirwrought parts (DevLoadout chapter10: the plain Choirwrought frame, Plating 52) and leaves at 30. A hero's bar is 120 + 18 per Sync past
    /// the first: 588 at 27, 624 at 29. Taren's and Sela's Plating is 10 + 2 per Sync past the first + the frame's (114 at 27, 118 at 29); Idra's, the guard hero's, 16 + 3 per Sync past the
    /// first + the frame's (146 at 27, 152 at 29), and a hit lands at 100/(100 + Plating - 10) of itself. So at a zone threat of 1 an authored hit of D costs Taren or Sela at 27 D x .4902 / 588
    /// of the bar (the most anyone pays: 13% allows D = 155) and Idra at 29 D x .4132 / 624 (the least: 9% needs D = 136; at 30, 142). Every ordinary hit here is authored between 142 and
    /// 150, inside the band from Sync 27 to 30:</para>
    /// <code>
    ///                                   D    Taren/Sela 27   Idra 27   Taren/Sela 29   Idra 29   Idra 30
    ///   Tender torch                   142      11.8%        10.2%       10.9%         9.4%      9.0%
    ///   Rig discharge, Clamp Gun burst 144      12.0%        10.4%       11.1%         9.5%      9.2%
    ///   Warden baton, Tow harpoon      146      12.2%        10.5%       11.2%         9.7%      9.3%
    ///   Weld bead                      148      12.3%        10.7%       11.4%         9.8%      9.4%
    ///   Warden drive, Interceptor run  150      12.5%        10.8%       11.6%         9.9%      9.5%
    /// </code>
    /// <para>What a weld leaves burning is a marked danger of 44 a second on top (3.7% of Taren's bar at 27 a second), which a step off the seam ends. The Interceptor's tractor line hurts
    /// nobody: the pass it holds a ship for does. Every zone of the chapter is at threat 1 (LastMooringBuilder.Define). LastMooringCombatTests measures every hit on all three heroes.
    /// Integrity is chapter 9's a sixth to a seventh up, for a party a tier and two Syncs stronger: a Warden 1,900, a Weld Arm 1,550, a gun 3,300 (Mother of Doors' sheet allows each clamp's
    /// two guns about 24 s. At 2,200 the arrival pair flew a clamp's two down through the replay driver's pad in about 16 s, and with four clamps her whole fight from her door lasted 117 to
    /// 132 s against her 120 to 300: MooringBossReplayTests).</para>
    /// <para>XP. Sync 27 to 30 costs 3,375 + 3,500 + 3,625 = 10,500 a hero. The main quest pays nothing and the two side quests 400 (LastMooringBuilder.Quests). The bosses, as this ledger
    /// assumes them (each boss engineer's to set on its definition; a different number moves the Syncs below by its difference over about 3,500): Mother of Doors 1,500 (as defined),
    /// the Tollkeeper 1,600, Meret's ship 1,200, her gantry 1,300, her well 1,400 (7,000). The encounters on the main path, as the zones' contracts plan them:</para>
    /// <code>
    ///   Sorrel        two lane pairs, 4 Interceptors at 90                                   360
    ///                 (her fight) the four clamps' guns, 8 Clamp Guns at 110; 6 Tenders at 60  1,240   then Mother of Doors 1,500
    ///   Last Mooring  two lane pairs, 4 Interceptors at 90; the gap's two Clamp Guns at 110       580   then the Tollkeeper 1,600
    ///   Continuance   the mess, a Warden pair at 140; the works, a pair, a Weld Arm at 140 and
    ///                 an Override Rig at 70                                                      770   then Meret's ship 1,200
    ///   Chamber       nothing ordinary                                                              then her gantry 1,300 and her well 1,400
    ///   Passage       the tug, 3 Tow-drones at 80                                                240
    /// </code>
    /// <para>So the party meets Mother of Doors at Sync 27.1 (her sheet: 27), the Tollkeeper at 28.1 (28), Meret's ship at 28.8 (29), the gantry at 29.1 (29) and the well at 29.5 (29), and the main
    /// path ends the chapter at 29.9 (10,190); the two side quests take it to 30.0 (10,590 against 10,500). The mess can be walked past (280 less, 29.8); an optional pair on a Last Mooring
    /// island pays 280 more. LastMooringCombatTests.TheLedgerBringsThePartyToEachBossAtTheSyncItsSheetAssumes walks this road with the definitions as they stand.</para>
    /// <para>Drops. The region's common is Anchor Thread (every table here drops it); its rare, the Stillwater Core, only a boss drops. Choirwrought parts come out of the bigger bodies at a few
    /// percent.</para>
    /// </summary>
    public static partial class LastMooringBuilder
    {
        internal static class Enemies
        {
            /// <summary>The roster's ids, for the zones (through <see cref="Foe"/>) and the bosses that call one in.</summary>
            internal const string Warden="StillwaterWarden",WeldArm="StillwaterWeldArm",Rig="StillwaterRig",Interceptor="StillwaterInterceptor",TowDrone="StillwaterTowDrone",
                Gun="ClampGun",Tender="StillwaterTender";
            /// <summary>Every ordinary EnemyDef, AttackDef and LootTable of the chapter, at a zone threat of 1.</summary>
            internal static void Define()
            {
                // Each is drawn in a body that exists (art/intake: nothing is generated for this chapter). A missing one stops the build here instead of standing a grey capsule.
                // The gun and the tender wear their own grades of chapter 8's and chapter 7's bodies in the Compact's white and teal (ClampGun, StillwaterTender: tools/c17_intake.py).
                GameObject Body(string id){var body=ZoneKit.Prefab("Enemies",id);if(body==null)throw new System.InvalidOperationException("MOORING_ENEMY_BODY_MISSING "+id+": chapter 10's enemies are drawn in bodies taken in for chapters 7 to 9");return body;}
                LootTable Loot(string id,params LootEntry[] entries)=>ArenaBuilder.Asset<LootTable>(id,t=>t.entries=entries);
                LootEntry Drop(string id,int count,float chance)=>ProgressionBuilder.Drop(id,count,chance);
                AttackDef Attack(string id,float damage,float range,float telegraph,float cooldown,DamageType type,float radius=1,bool projectile=false)=>
                    ArenaBuilder.Asset<AttackDef>(id,a=>{a.damage=damage;a.range=range;a.telegraph=telegraph;a.cooldown=cooldown;a.type=type;a.radius=radius;a.projectile=projectile;});
                const string Thread="AnchorThread";

                // ---- the attacks, at a zone threat of 1. Range is where the move starts from (a harpoon's hold, a pass's run-in, a lunge's ring), or for the anchored how near a hero must
                // be; how far it reaches is the behaviour's own tune.reach.
                var wardenBaton=Attack("StillwaterBaton",146,3,.8f,2.4f,DamageType.Pulse,2.1f);
                var wardenDrive=Attack("StillwaterShieldDrive",150,7,1,3.6f,DamageType.Kinetic);
                var weldBead=Attack("StillwaterWeldBead",148,11,1,3.4f,DamageType.Plasma);
                var rigDischarge=Attack("StillwaterDischarge",144,2.8f,1,3,DamageType.Pulse);
                var interceptorRun=Attack("StillwaterInterceptorRun",150,14,.9f,3.2f,DamageType.Kinetic);
                // The tractor holds a ship still for the pass; it hurts nobody itself.
                var interceptorTractor=Attack("StillwaterTractor",0,15,.9f,4.5f,DamageType.Pulse);
                var towHarpoon=Attack("StillwaterHarpoon",146,12,.9f,3.4f,DamageType.Kinetic);
                // A curtain's range is how near a ship must come before it fires at all: the clamp's box and a little more.
                var gunBurst=Attack("ClampGunBurst",144,24,1,2.2f,DamageType.Kinetic);
                var tenderTorch=Attack("StillwaterTorch",142,3,.7f,2.6f,DamageType.Plasma);

                // ---- the Continuance's crews
                // Guardian that gives ground, in a pair: a shield to the front (140 degrees, a fifth through), a stun-baton on a marked circle 2.1 m round (.8 s) and from 2.4 to 7 m a shield
                // drive down a marked line 7 by 1.8 (1 s), after which its shield is down 1.6 s. Two woken within 7 m stand 4.2 m apart: one braces its drive while the other covers it, and
                // neither strikes across the other, so a hero between them is struck by neither until they step apart round him. Held to 9 m from where it was set. Its guard broken it backs
                // off 3 m and stands open; at nought it stands aside and walks out (the lead's "warden"). Answer: between them, or round the one covering while the other's drive is spent (Plasma).
                ArenaBuilder.Asset<EnemyDef>(Warden,e=>{e.id=Warden;e.displayName="Compact Warden";e.prefab=Body("CompactWarden");e.integrity=1900;e.breakThreshold=340;e.speed=2.6f;e.attacks=new[]{wardenBaton,wardenDrive};
                    e.weakness=DamageType.Plasma;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.Guardian;
                    e.tune=new BehaviourTune{arc=140,guard=.2f,reach=7,width=1.8f,recover=1.6f,wake=9,speed=80,givesGround=true,pair=true,bark="warden"};
                    e.xp=140;e.scrip=28;e.scrap=3;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.6f;e.capsuleHeight=1.95f;
                    e.lootTable=Loot(Warden,Drop(Thread,2,.5f),Drop("ScrapAlloy",3,.8f),Drop("RepairGel",1,.08f),ProgressionBuilder.AnyPart(5,GearSlot.Frame,.04f));});
                // A rider on its own rail that never moves: the works' welding arm, built properly. A hero on its strip (11 m along the way it was set facing, 2.4 wide) sees it lit 1 s and the
                // weld runs down it; the seam burns 3 s after (44 a second). At nought its drive is cut and it stands down at rest where it worked. Answer: off the strip, strike from beside (Kinetic).
                ArenaBuilder.Asset<EnemyDef>(WeldArm,e=>{e.id=WeldArm;e.displayName="Weld Arm";e.prefab=Body("WeldArm");e.integrity=1550;e.breakThreshold=310;e.speed=0;e.attacks=new[]{weldBead};
                    e.weakness=DamageType.Kinetic;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.Rider;
                    e.tune=new BehaviourTune{reach=11,width=2.4f,wake=3,linger=3,effect=AreaEffect.Burn,tick=44,standsDown=true};
                    e.xp=140;e.scrip=26;e.scrap=3;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.9f;e.capsuleHeight=2.4f;
                    e.lootTable=Loot(WeldArm,Drop(Thread,2,.5f),Drop("ScrapAlloy",3,.7f),ProgressionBuilder.AnyPart(5,GearSlot.Edge,.04f));});
                // Anchored, all round: the Compact's control drone clamped on a machine's cable. While a hero is within 2.8 m it discharges: a marked ring round it 1 s, then a hit, every 3 s.
                // Answer: strike between discharges, or shoot it from outside the ring (Pulse).
                ArenaBuilder.Asset<EnemyDef>(Rig,e=>{e.id=Rig;e.displayName="Override Rig";e.prefab=Body("OverrideRig");e.integrity=1000;e.breakThreshold=220;e.speed=0;e.attacks=new[]{rigDischarge};
                    e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.Anchored;e.tune=new BehaviourTune{arc=360};
                    e.xp=70;e.scrip=20;e.scrap=2;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.6f;e.capsuleHeight=1.4f;
                    e.lootTable=Loot(Rig,Drop(Thread,1,.5f),Drop("ScrapAlloy",2,.6f),ProgressionBuilder.AnyPart(5,GearSlot.Module,.04f));});

                // ---- in flight: Sorrel's valley, Last Mooring's lanes
                // Strafers in a pair: one pins a ship with a tractor line (15 m by 1.2, .9 s; .3 of its speed for up to 3.5 s), the other circles at 14 m and runs a marked lane 22 m by 2.6 at
                // 24 m/s through whoever is pinned. The line breaks on a roll, a hit on the pinner, or cover (Last Mooring's chains). Answer: break the line, then cross out of the lane (Beam).
                ArenaBuilder.Asset<EnemyDef>(Interceptor,e=>{e.id=Interceptor;e.displayName="Interceptor";e.prefab=Body("Interceptor");e.integrity=1180;e.breakThreshold=270;e.speed=12;e.attacks=new[]{interceptorRun,interceptorTractor};
                    e.weakness=DamageType.Beam;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Strafer;
                    e.tune=new BehaviourTune{reach=22,width=2.6f,speed=24,recover=1.4f,pair=true};
                    e.xp=90;e.scrip=22;e.scrap=2;e.boss=false;e.motion=EnemyMotion.Hover;e.heavyDeath=true;e.capsuleRadius=1;e.capsuleHeight=2.2f;
                    e.lootTable=Loot(Interceptor,Drop(Thread,1,.4f),Drop("ScrapAlloy",2,.6f),ProgressionBuilder.AnyPart(5,GearSlot.Drive,.03f));});
                // Anchored with a curtain, set in pairs that cross: three rows across the way it faces, 6 m deep each out to 18 m and 30 m across, the first and third then the second, each
                // marked 1 s and bursting, 3.2 s a beat, whoever is where, while a ship is within 24 m. Two crossed (each turned toward the other) leave no lee behind either, and their quiet
                // rows cross in a cell on the double beat. Two that overlap strike a ship once a volley. Answer: read the clock, sit in the quiet cell, or break them (Kinetic).
                ArenaBuilder.Asset<EnemyDef>(Gun,e=>{e.id=Gun;e.displayName="Clamp Gun";e.prefab=Body("ClampGun");e.integrity=3300;e.breakThreshold=420;e.speed=0;e.attacks=new[]{gunBurst};
                    e.weakness=DamageType.Kinetic;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.Anchored;e.tune=new BehaviourTune{count=3,reach=18,width=30};
                    e.xp=110;e.scrip=30;e.scrap=3;e.boss=false;e.heavyDeath=true;e.capsuleRadius=2;e.capsuleHeight=4.4f;
                    e.lootTable=Loot(Gun,Drop(Thread,2,.6f),Drop("ScrapAlloy",3,.8f),ProgressionBuilder.AnyPart(5,GearSlot.Frame,.05f));});

                // ---- the passage
                // Tetherer that tows: a drone on the tug Welcome Back. It holds 12 m off a ship and marks a harpoon line 15 m by 1.2 for .9 s; caught, the ship flies at .45 of its speed and
                // is reeled in at 3.5 m/s while the drone hauls it the way it was set facing (the passage sets it away from the tug) at 3 m/s, for at most 3.5 s. Anything that hits the drone
                // cuts the line; a roll snaps it; cover breaks it. Answer: roll the line, or one shot on the drone (Pulse).
                ArenaBuilder.Asset<EnemyDef>(TowDrone,e=>{e.id=TowDrone;e.displayName="Tow-drone";e.prefab=Body("TowDrone");e.integrity=1000;e.breakThreshold=220;e.speed=6.5f;e.attacks=new[]{towHarpoon};
                    e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.Tetherer;
                    e.tune=new BehaviourTune{reach=15,pace=.45f,speed=3.5f,linger=3.5f,tow=3};
                    e.xp=80;e.scrip=20;e.scrap=2;e.boss=false;e.motion=EnemyMotion.Hover;e.heavyDeath=true;e.capsuleRadius=.9f;e.capsuleHeight=1.8f;
                    e.lootTable=Loot(TowDrone,Drop(Thread,1,.35f),Drop("ScrapAlloy",2,.6f),ProgressionBuilder.AnyPart(5,GearSlot.Drive,.03f));});

                // ---- Sorrel's shelves (stepped out by Mother of Doors' fight as the party docks: its file names it)
                // Tender: the Compact's clamp hand. It walks to whatever it keeps (a clamp a hero can work: Lattice.Combat.Tended) within 24 m and works it, heeding nobody: a clamp it is at
                // loses what a hero has taken of its weight and never eases. With nothing to keep, a torch on a marked 70-degree fan inside 3 m (.7 s), and it keeps a step off. Fragile (600: a
                // few of the partner's blows). Answer: the partner stops it while the lead holds the clamp.
                ArenaBuilder.Asset<EnemyDef>(Tender,e=>{e.id=Tender;e.displayName="Clamp Tender";e.prefab=Body("StillwaterTender");e.integrity=600;e.breakThreshold=120;e.speed=3.2f;e.attacks=new[]{tenderTorch};
                    e.weakness=DamageType.Plasma;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Tender;
                    e.tune=new BehaviourTune{arc=70,wake=24,bark="tender"};
                    e.xp=60;e.scrip=16;e.scrap=2;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.5f;e.capsuleHeight=1.1f;
                    e.lootTable=Loot(Tender,Drop(Thread,1,.3f),Drop("ScrapAlloy",2,.6f),Drop("ChargeCell",1,.08f));});
            }
        }
    }
}
