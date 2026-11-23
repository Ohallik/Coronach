using Lattice.Data;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 6's enemies (brief, Enemies; manifest section 5 for every id and name): seven ordinary kinds, each one family of the behaviour library (Lattice.Combat.EnemyBehaviour;
    /// CHAPTER_GUIDE section 5, "Enemy behaviours"), each with one tell and one answer unlike its neighbours'; and the Host Marshal, whose moves are BossController.HostMarshal.cs.
    /// Every number a fight is tuned by is here, and only here. <c>HoarfellBuilder.Define</c> calls <c>DefineEnemies</c> after the zones and before the quests.
    /// <para>Sizing. The party arrives at Sync 16 in Prime parts (DevLoadout chapter6: the plain Prime frame, Plating 26), Hask sells Tempered ones in Lastlight (frame 38), and it leaves at
    /// 19. A hero's bar is 120 + 18 per Sync past the first: 390 at 16, 426 at 18, 444 at 19. Taren's and Sela's Plating is 10 + 2 per Sync past the first + the frame's (66 at 16, 72 at 19 in
    /// Prime); Idra's, the guard hero's, 16 + 3 per Sync (87 and 96), and a hit lands at 100/(100 + Plating - 10) of itself. So at a zone threat of 1 an authored hit of D costs Taren or
    /// Sela D x .641 / 390 of the bar at 16 (the most anyone pays) and Idra D x .538 / 444 at 19 in Prime (the least): 13% of the first allows D = 79, 9% of the second needs D = 74.
    /// Every ordinary attack here is authored between 75 and 79 (Taren at 16: 12.3 to 13.0%; Idra at 19: 9.1 to 9.6%), and every zone of the chapter stays at threat 1. In Hask's Tempered
    /// frame Idra at 19 takes 8.5 to 9.0%: the guard hero in the best frame the chapter sells is the one case under the band, and by a hair. The Host Marshal is met at Sync 17 to 18: its
    /// blows are 100 to 102, 12.0 to 15.8% of any of the three there in Prime or Tempered (Taren at 17 in Prime x .633 / 408 the most, Idra at 18 in Tempered x .513 / 426 the least), before
    /// its cold phase's +18% (18.7% at most).</para>
    /// <para>XP. Sync 16 to 19 costs 2,000 + 2,125 + 2,250 = 6,375 a hero. The main quest pays 1,600 and the three side quests 880 (HoarfellBuilder.Quests). The kills are sized to the
    /// encounters as planned (docs/maps/FrozenHost.md, and about four tenders and eight skiffs in the Shear): on the main path the Shear 800 (4 x 100, 8 x 50), the White Reach 510 (4
    /// Rime-stalkers x 120, a mote swarm 30), the dig 1,080 (four mote swarms 120, two ranks of Thawed Guards 780, two Bridle-hounds 180) and the Marshal 1,090 (700, and about three of
    /// the soldiers it calls out, 390); the optional pockets 590 (a third rank 390, the Excavator 200). So a party meets the Marshal at Sync 17.2, or 17.9 having done everything
    /// optional; the main path with the main quest ends the chapter at 18.4, the side quests take it to 18.8, and everything to 19.1. A soldier put back to sleep by its coupling pays
    /// nothing, and that is the fight working.</para>
    /// <para>Drops. Hoarfell's commons are Rime Glass (the ice ring's and the Reach's) and Host Alloy (the Ancelane host's, from the dig); its rare, the Marshal's Seal, only the Marshal drops.
    /// The Shear drops Prime parts, as the Veil did; the dig is where Tempered ones come out of the ice, at a few percent a body, beside Hask's counter.</para>
    /// </summary>
    public static partial class HoarfellBuilder
    {
        static void DefineEnemies()
        {
            GameObject Body(string id,string fallback)=>ZoneKit.Body("Enemies",id,"Enemies",fallback);
            LootTable Loot(string id,params LootEntry[] entries)=>ArenaBuilder.Asset<LootTable>(id,t=>t.entries=entries);
            LootEntry Drop(string id,int count,float chance)=>ProgressionBuilder.Drop(id,count,chance);
            AttackDef Attack(string id,float damage,float range,float telegraph,float cooldown,DamageType type,float radius=1,bool projectile=false)=>
                ArenaBuilder.Asset<AttackDef>(id,a=>{a.damage=damage;a.range=range;a.telegraph=telegraph;a.cooldown=cooldown;a.type=type;a.radius=radius;a.projectile=projectile;});

            // ---- the attacks, at a zone threat of 1. Range is where the move starts from (a lob's throw, a pass's run-in, a leap's spring), not always how far it reaches: that is the
            // behaviour's own tune.reach.
            var thawCharge=Attack("ThawCharge",78,14,1.3f,3.6f,DamageType.Plasma,2.6f);
            var skiffPass=Attack("SkiffPass",76,13,.8f,3,DamageType.Kinetic);
            var stalkerBite=Attack("StalkerBite",77,2.8f,.65f,0,DamageType.Kinetic);
            var moteDart=Attack("MoteDart",75,3.2f,.6f,2.8f,DamageType.Pulse);
            var moteBurst=Attack("MoteBurst",75,2.5f,.45f,0,DamageType.Pulse,2.5f);
            var guardSlam=Attack("GuardSlam",78,3.2f,.8f,2.4f,DamageType.Kinetic,2.2f);
            var guardThrust=Attack("GuardThrust",79,9,.95f,3.4f,DamageType.Kinetic);
            var excavatorSlam=Attack("ExcavatorSlam",78,3.4f,.85f,2.6f,DamageType.Kinetic,2.4f);
            var thawLance=Attack("ThawLance",79,11,1.1f,4.2f,DamageType.Plasma);
            var houndLeap=Attack("HoundLeap",77,7,.8f,2.6f,DamageType.Kinetic);
            var marshalButt=Attack("MarshalSpearButt",100,4,.8f,2.4f,DamageType.Kinetic,2.4f);

            // ---- the Aurora Shear (flight). Every fight there stands in a current, and a current carries the wing, its markings, the melt it leaves and the shots as it carries the
            // ships (AuroraCurrent.Float): a running fight down the lane, in which each answer below is what it is in still space. Only the ships riding, every tell landed where a
            // ship had been and two player runs took no blow from any of them (November 14).
            // Lobber on the wing: a heavy hauler, slow (2.2 m/s), that heaves a thaw-charge onto a marked 2.6 m circle where the ship is, 1.3 s in the air; where it bursts the ice-melt burns
            // for 4 s across 3.2 m (22 a second, a danger drawn as one). Answer: fly out of the circle toward it, round the burning wake, and break it close: it can't outrun a ship.
            // It has no body of its own yet (the chapter's art order had none): it flies as the hauler hull.
            ArenaBuilder.Asset<EnemyDef>("MusterTender",e=>{e.id="MusterTender";e.displayName="Muster Tender";e.prefab=ZoneKit.Body("Enemies","MusterTender","Ships","Hauler");e.integrity=1100;e.breakThreshold=260;e.speed=2.2f;e.attacks=new[]{thawCharge};
                e.weakness=DamageType.Kinetic;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.Lobber;
                e.tune=new BehaviourTune{linger=4,area=3.2f,effect=AreaEffect.Burn,tick=22};
                e.xp=100;e.scrip=22;e.scrap=3;e.boss=false;e.motion=EnemyMotion.Hover;e.heavyDeath=true;e.capsuleRadius=1.6f;e.capsuleHeight=3.2f;
                e.lootTable=Loot("MusterTender",Drop("ScrapAlloy",3,.7f),Drop("RimeGlass",2,.5f),Drop("ChargeCell",1,.1f),ProgressionBuilder.AnyPart(3,GearSlot.Drive,.05f));});
            // Strafer: the Muster's raider, here riding the currents. It circles off a ship and, inside 13 m, marks an 18 m lane 2.6 m wide for .8 s and runs it at 22 m/s, then breaks
            // away for 1.3 s; when one dies the rest of its wing scatters. Answer: cross out of its lane as the tell ends, and hit it as it breaks off. (Chapter 2's Muster Skiff is a
            // shooter tuned for Sync 9; this one is the same craft at chapter 6's weight.)
            ArenaBuilder.Asset<EnemyDef>("CurrentSkiff",e=>{e.id="CurrentSkiff";e.displayName="Muster Skiff";e.prefab=Body("MusterSkiff","Podskimmer");e.integrity=380;e.breakThreshold=110;e.speed=9;e.attacks=new[]{skiffPass};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Strafer;
                e.tune=new BehaviourTune{reach=18,width=2.6f,speed=22,recover=1.3f};
                e.xp=50;e.scrip=10;e.scrap=1;e.boss=false;e.motion=EnemyMotion.Hover;e.capsuleRadius=.9f;e.capsuleHeight=1.8f;
                e.lootTable=Loot("CurrentSkiff",Drop("ScrapAlloy",2,.5f),Drop("RimeGlass",1,.3f));});

            // ---- the White Reach
            // Ambusher: it lies under a drift (a ringed hide, 3.6 m), comes up at whoever steps inside the ring, bites twice (marked, .65 s and .5 s) and stands spent in the open for
            // 1.6 s before it goes for another drift. Answer: step in to draw it, roll the bites, and hit it while it is spent.
            ArenaBuilder.Asset<EnemyDef>("RimeStalker",e=>{e.id="RimeStalker";e.displayName="Rime-stalker";e.prefab=Body("RimeStalker","Reedstalker");e.integrity=650;e.breakThreshold=170;e.speed=3.8f;e.attacks=new[]{stalkerBite};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Ambusher;
                e.tune=new BehaviourTune{wake=3.6f,count=2,speed=6,recover=1.6f};
                e.xp=120;e.scrip=20;e.scrap=2;e.boss=false;e.capsuleRadius=.9f;e.capsuleHeight=1.8f;
                e.lootTable=Loot("RimeStalker",Drop("RimeGlass",2,.6f),Drop("RepairGel",1,.06f),ProgressionBuilder.AnyPart(4,GearSlot.Edge,.03f));});
            // Swarm cloud, the cold itself: twenty pale motes with no model. It darts on a marked circle and, landed, chills: the hero wades at half his stride for 2.4 s until he rolls or it
            // is hit. Broken (50: one Pulse hit) or killed it bursts on a marked .45 s fuse. Answer: break it from a step back.
            ArenaBuilder.Asset<EnemyDef>("FrostMotes",e=>{e.id="FrostMotes";e.displayName="Frost Motes";e.prefab=null;e.integrity=240;e.breakThreshold=50;e.speed=5.5f;e.attacks=new[]{moteDart,moteBurst};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Swarm;e.behaviour=EnemyBehaviourKind.SwarmCloud;
                e.tune=new BehaviourTune{linger=2.4f,pace=.5f,recover=1.3f,count=20,tint=new Color(.78f,.94f,1.3f,1)};
                e.xp=30;e.scrip=4;e.scrap=0;e.boss=false;e.capsuleRadius=.7f;e.capsuleHeight=1.4f;
                e.lootTable=Loot("FrostMotes",Drop("RimeGlass",1,.25f));});

            // ---- the dig
            // Guardian in a rank: three woken together stand shoulder to shoulder, face the party as one, advance together and thrust their spears together, three parallel 9 m lines
            // marked .95 s (then each spear is in the floor and its shield down 1.4 s). Each shield lets a fifth of a blow through over its front 120 degrees. Three abreast, their shields are
            // locked through the middle one: break it (its Break meter) and every shield in the rank is down while it reels; kill it and the rank comes apart, the other two open 1.6 s.
            // Answer: break the middle, or go round the end of the rank.
            ArenaBuilder.Asset<EnemyDef>("ThawedGuard",e=>{e.id="ThawedGuard";e.displayName="Thawed Guard";e.prefab=Body("ThawedGuard","AncelaneGuard");e.integrity=1100;e.breakThreshold=240;e.speed=2.2f;e.attacks=new[]{guardSlam,guardThrust};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.Guardian;
                e.tune=new BehaviourTune{arc=120,guard=.2f,reach=9,width=1.4f,recover=1.4f,wake=12,speed=70,count=3};
                e.xp=130;e.scrip=24;e.scrap=3;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.7f;e.capsuleHeight=2.4f;
                e.lootTable=Loot("ThawedGuard",Drop("HostAlloy",1,.5f),Drop("ScrapAlloy",3,.8f),ProgressionBuilder.AnyPart(4,.05f));});
            // Guardian with a swept beam: armoured to the front 130 degrees (a fifth through), slow to turn (70 a second). Close, a slam on a marked 2.4 m circle; from further, its thaw-lance:
            // a 100-degree fan out to 11 m marked 1.1 s, then a Plasma beam crosses it edge to edge in .6 s and strikes whoever it passes; then the lance vents, shield down and no turning
            // for 1.8 s. Answer: get behind it or under its slam's reach before the fan fills, or roll through the beam as it comes, then punish the vent.
            ArenaBuilder.Asset<EnemyDef>("MusterExcavator",e=>{e.id="MusterExcavator";e.displayName="Muster Excavator";e.prefab=Body("MusterExcavator","MusterSergeant");e.integrity=1500;e.breakThreshold=300;e.speed=1.8f;e.attacks=new[]{excavatorSlam,thawLance};
                e.weakness=DamageType.Kinetic;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.Guardian;
                e.tune=new BehaviourTune{arc=130,guard=.2f,reach=11,recover=1.8f,wake=10,speed=70,sweep=100};
                e.xp=200;e.scrip=34;e.scrap=4;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.85f;e.capsuleHeight=2.6f;
                e.lootTable=Loot("MusterExcavator",Drop("HostAlloy",2,.6f),Drop("ScrapAlloy",3,.8f),Drop("ChargeCell",1,.1f),ProgressionBuilder.AnyPart(4,.08f));});
            // Leaper: it circles at the edge of its 7 m spring, marks a lane .8 s and leaps it at 16 m/s; whoever it lands on is struck and pinned at .15 of his stride for 1.4 s until he
            // rolls or it is hit. Caught in the air by any blow it shatters; landed on a hero who is guarding, it is thrown off and open 1.65 s. Answer: roll the lane, strike it as it comes,
            // or brace for it.
            ArenaBuilder.Asset<EnemyDef>("BridleHound",e=>{e.id="BridleHound";e.displayName="Bridle-hound";e.prefab=Body("BridleHound","Ridgehound");e.integrity=380;e.breakThreshold=100;e.speed=6;e.attacks=new[]{houndLeap};
                e.weakness=DamageType.Beam;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Leaper;
                e.tune=new BehaviourTune{reach=7,width=1.6f,speed=16,linger=1.4f,pace=.15f,recover=1.1f};
                e.xp=90;e.scrip=14;e.scrap=1;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.6f;e.capsuleHeight=1.3f;
                e.lootTable=Loot("BridleHound",Drop("HostAlloy",1,.35f),Drop("ScrapAlloy",2,.5f),ProgressionBuilder.AnyPart(4,GearSlot.Drive,.03f));});

            // ---- the Host Marshal. Integrity is sized on the party's measured rate (HoarfellCombatTests pins this literal to the asset and holds the fight to its time for every pair a player
            // can put out): two to five minutes for a Sync 17 party in the chapter's arrival parts (Prime), three couplings and then half the bar. Measured November 12 flat out: Taren and
            // Idra (the pair the join leaves out) 2,002 a second, Sela and Idra 1,735, Taren and Sela 1,715. At the driver's share, the couplings struck half the time: 165 s for the
            // first pair, 190 for the others; the shared test's ceiling (a quarter of flat out) 268 s for the slowest. QaPlanCombatTests measures it in chapter 6's arrival parts too.
            // A coupling takes a sixth of its bar with it; like every other number of its moves that is in BossController.HostMarshal.cs's own table. It drops the region's rare,
            // the Marshal's Seal. 125,000 (November 15): fought through the pad by chapter 6's floor party (Prime parts) it fell at 115,000 in 107 and 122 s, a share of .53 to .58 and not
            // the .45 an under-geared replay party had measured: under its two minutes. Its slowest pair's quarter of flat out (265 s at 115,000) caps it near 130,000 (FloorPartyBossReplayTests).
            ArenaBuilder.Asset<EnemyDef>("HostMarshal",e=>{e.id="HostMarshal";e.displayName="THE HOST MARSHAL";e.prefab=Body("HostMarshal","AncelaneGuard");e.integrity=155000;e.breakThreshold=800;e.speed=1.6f;e.attacks=new[]{marshalButt};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.None;e.xp=700;e.scrip=420;e.scrap=20;e.boss=true;
                e.bossMusicId="Boss of the Ages";
                // A capsule can't stand shorter than it is wide: 4.8 m on its 1.5 m radius, the body's own height.
                e.capsuleHeight=4.8f;
                e.lootTable=Loot("HostMarshal",Drop("MarshalsSeal",1,1),Drop("HostAlloy",6,1),Drop("RimeGlass",6,1),Drop("RepairSalve",2,1),ProgressionBuilder.AnyPart(4,1));});
        }
    }
}
