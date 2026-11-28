using Lattice.Data;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 3's enemies: eight ordinary kinds, each one family of the behaviour library (Lattice.Combat.EnemyBehaviour; CHAPTER_GUIDE section 5, "Enemy behaviours"), and the
    /// two fights' definitions (their moves are BossController.Seedthane.cs and BossController.Floodcrown.cs). Every number a fight is tuned by is here, and only here.
    /// <para>Sizing (November 12, measured through Health.Receive by XylosCombatTests). A hero's bar is 120 + 18 per Sync past the first; Plating is 10 + 2 per Sync past the first + the
    /// frame's, and a hit lands at 100/(100 + Plating - 10) of itself. The party arrives in Refined parts (DevLoadout chapter3: the Refined frame, 16) and leaves in Prime ones (chapter4:
    /// the Prime frame, 26). So a hit of D at the zone's threat t costs D x t x .746 / 282 of the bar at Sync 10, x .735 / 300 at 11, x .725 / 318 at 12 in Refined parts and x .667 / 336
    /// at 13 in Prime. The plan asks 9 to 13% where each is met: the nearspace (threat 1) at 10, so D 34 to 49; the Braided River (1) at 10 and 11, so 37 to 49; the Walking Canopy (1.1)
    /// at 12 in Refined parts and 13 in Prime, so 41 to 52, and what is met in both the river and the Canopy (the swarm, the caster) 41 to 49. Every ordinary attack is authored in its
    /// band below: 9.2 to 12.7% wherever it is met. The first numbers, 26 to 45, reasoned for Plating taking a sixth off, came to 5 to 9%.</para>
    /// <para>XP. Three Syncs from 10 cost 4,125. The story quest pays 900, so the kills on the main path are sized to about 3,100 as the encounters stand (26 skimmers, 9 knots,
    /// 1 drone, 6 Thornbacks, 9 Reedstalkers, 12 Sporecasters, 25 swarms, 4 Rootwardens and the two fights), and the optional pockets to about 450 beside the side quests' 920.</para>
    /// </summary>
    public static partial class XylosBuilder
    {
        static void DefineEnemies()
        {
            GameObject Body(string id,string fallback)=>ZoneKit.Body("Enemies",id,"Enemies",fallback);
            LootTable Loot(string id,params LootEntry[] entries)=>ArenaBuilder.Asset<LootTable>(id,t=>t.entries=entries);
            LootEntry Drop(string id,int count,float chance)=>ProgressionBuilder.Drop(id,count,chance);
            AttackDef Attack(string id,float damage,float range,float telegraph,float cooldown,DamageType type,float radius=1,bool projectile=false)=>
                ArenaBuilder.Asset<AttackDef>(id,a=>{a.damage=damage;a.range=range;a.telegraph=telegraph;a.cooldown=cooldown;a.type=type;a.radius=radius;a.projectile=projectile;});

            // ---- the attacks, at a zone threat of 1. Range is where the move starts from (a charge's run-in, a lob's throw, a tether's hold), not always how far it reaches:
            // that is the behaviour's own tune.reach.
            var skimPass=Attack("SkimPass",40,11,.7f,2.6f,DamageType.Kinetic);
            var vineLash=Attack("VineLash",44,5.5f,.85f,2.6f,DamageType.Kinetic);
            var claimTether=Attack("ClaimTether",38,12,.8f,4,DamageType.Pulse);
            var thornCharge=Attack("ThornCharge",48,9,.9f,3.2f,DamageType.Kinetic);
            var reedBite=Attack("ReedBite",40,2.8f,.6f,1.6f,DamageType.Kinetic);
            var sporePod=Attack("SporePod",44,11,1.1f,3.4f,DamageType.Pulse,2.4f);
            var mireDart=Attack("MireDart",42,2.6f,.6f,2.6f,DamageType.Kinetic);
            var mireBurst=Attack("MireBurst",42,2.5f,.45f,0,DamageType.Pulse,2.5f);
            var rootSlam=Attack("RootSlam",46,3.4f,.8f,2.4f,DamageType.Kinetic,2.4f);
            var rootLine=Attack("RootLine",50,11,1,3.2f,DamageType.Kinetic);
            var thaneStab=Attack("ThaneStab",50,3.6f,.7f,2.2f,DamageType.Kinetic);
            var crownGore=Attack("CrownGore",56,4.2f,.7f,2.4f,DamageType.Kinetic);

            // ---- nearspace
            // Strafer: a wing of them circles, each marks a lane through a ship, runs it and breaks away; a kill scatters the rest. Weak alone: two or three shots.
            ArenaBuilder.Asset<EnemyDef>("Podskimmer",e=>{e.id="Podskimmer";e.displayName="Podskimmer";e.prefab=Body("Podskimmer","ChoristerDart");e.integrity=150;e.breakThreshold=70;e.speed=6.5f;e.attacks=new[]{skimPass};
                e.weakness=DamageType.Beam;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Strafer;e.tune=new BehaviourTune{reach=16,width=2.2f,speed=20,recover=1.3f};
                e.xp=18;e.scrip=6;e.scrap=1;e.boss=false;e.motion=EnemyMotion.Hover;e.drawnScale=1.4f;e.heavyDeath=false;
                e.lootTable=Loot("Podskimmer",Drop("AmberSap",1,.25f),Drop("Rootfibre",1,.15f));});
            // Anchored: it never moves and nothing moves it; a lash across a marked fan at whoever comes inside 5.5 m; a blade lunge landing mid-tell takes the lash from it; its
            // death frees what it holds (the barge reads its encounter's clearing; EnemyAnchored.Freed says so too). Two lunges, or a few seconds' fire from outside its reach.
            ArenaBuilder.Asset<EnemyDef>("SnagvineNode",e=>{e.id="SnagvineNode";e.displayName="Snagvine Knot";e.prefab=Body("SnagvineNode","Shellmine");e.integrity=260;e.breakThreshold=110;e.speed=0;e.attacks=new[]{vineLash};
                e.weakness=DamageType.Kinetic;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.Anchored;e.tune=new BehaviourTune{arc=130};
                e.xp=16;e.scrip=4;e.scrap=0;e.boss=false;e.drawnScale=1.6f;e.heavyDeath=true;e.capsuleRadius=1;e.capsuleHeight=2;
                e.lootTable=Loot("SnagvineNode",Drop("Rootfibre",2,.7f));});
            // Tetherer: holds off at 12 m, marks a line, and what it fires holds a ship to half speed and reels it in at 3.5 m/s until the ship rolls, the drone is hit, cover
            // comes between them or five seconds pass.
            ArenaBuilder.Asset<EnemyDef>("ClaimDrone",e=>{e.id="ClaimDrone";e.displayName="Claim Drone";e.prefab=Body("ClaimDrone","MusterSkiff");e.integrity=300;e.breakThreshold=110;e.speed=5.5f;e.attacks=new[]{claimTether};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.Tetherer;e.tune=new BehaviourTune{pace=.5f,speed=3.5f,linger=5};
                e.xp=46;e.scrip=22;e.scrap=3;e.boss=false;e.motion=EnemyMotion.Hover;e.heavyDeath=true;
                e.lootTable=Loot("ClaimDrone",Drop("ScrapAlloy",2,.6f),Drop("ChargeCell",1,.12f),ProgressionBuilder.AnyPart(3,GearSlot.Drive,.06f));});

            // ---- the river and the Canopy
            // Charger: squares up inside 9 m, marks an 11 m lane and runs it at 15 m/s. In a root wall it is stuck and open for 2.4 s; a clean miss winds it for 1.1 s.
            // Its plates turn Taren's Kinetic edge: Plasma, or the opening.
            ArenaBuilder.Asset<EnemyDef>("Thornback",e=>{e.id="Thornback";e.displayName="Thornback";e.prefab=Body("Thornback","Gloomgrazer");e.integrity=640;e.breakThreshold=190;e.speed=3;e.attacks=new[]{thornCharge};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.Charger;e.tune=new BehaviourTune{reach=11,width=2.6f,speed=15,recover=2.4f};
                e.xp=70;e.scrip=18;e.scrap=2;e.boss=false;e.capsuleRadius=.8f;e.capsuleHeight=1.6f;
                e.lootTable=Loot("Thornback",Drop("Rootfibre",2,.8f),Drop("AmberSap",1,.3f),ProgressionBuilder.AnyPart(3,GearSlot.Frame,.05f));});
            // Ambusher: under a ringed hide until a hero steps inside 3.2 m; two bites, .6 s and .5 s; then spent in the open for 1.5 s before it makes for another hide.
            ArenaBuilder.Asset<EnemyDef>("Reedstalker",e=>{e.id="Reedstalker";e.displayName="Reedstalker";e.prefab=Body("Reedstalker","Ridgehound");e.integrity=300;e.breakThreshold=100;e.speed=4.2f;e.attacks=new[]{reedBite};
                e.weakness=DamageType.Kinetic;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Ambusher;e.tune=new BehaviourTune{wake=3.2f,count=2,speed=5.5f,recover=1.5f};
                e.xp=40;e.scrip=10;e.scrap=1;e.boss=false;
                e.lootTable=Loot("Reedstalker",Drop("Rootfibre",1,.5f),Drop("AmberSap",1,.25f),ProgressionBuilder.AnyPart(3,GearSlot.Edge,.03f));});
            // Lobber: holds 11 m off and backs away at a walk; a pod 1.1 s in the air onto a marked 2.4 m circle, which stays 5 s as a cloud that slows to .55 of pace.
            ArenaBuilder.Asset<EnemyDef>("Sporecaster",e=>{e.id="Sporecaster";e.displayName="Sporecaster";e.prefab=Body("Sporecaster","MusterScav");e.integrity=250;e.breakThreshold=90;e.speed=2.4f;e.attacks=new[]{sporePod};
                e.weakness=DamageType.Beam;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.Lobber;e.tune=new BehaviourTune{linger=5,area=2.6f,effect=AreaEffect.Slow,pace=.55f};
                e.xp=38;e.scrip=10;e.scrap=1;e.boss=false;
                e.lootTable=Loot("Sporecaster",Drop("AmberSap",2,.6f),Drop("RepairGel",1,.08f),ProgressionBuilder.AnyPart(3,GearSlot.Emitter,.03f));});
            // Swarm cloud: ONE body is a whole cloud of eighteen motes, with no model (a pocket wants two or three of these, not six). It darts onto a marked circle and, landing,
            // rides a hero for 2.5 s at .7 of his pace until he rolls or it is hit; broken (40 Break: one Pulse hit) or killed, it bursts on a .45 s fuse.
            ArenaBuilder.Asset<EnemyDef>("MireflySwarm",e=>{e.id="MireflySwarm";e.displayName="Mirefly Swarm";e.prefab=null;e.integrity=170;e.breakThreshold=40;e.speed=4.6f;e.attacks=new[]{mireDart,mireBurst};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Swarm;e.behaviour=EnemyBehaviourKind.SwarmCloud;e.tune=new BehaviourTune{linger=2.5f,pace=.7f,recover=1.2f,count=18};
                e.xp=10;e.scrip=3;e.scrap=0;e.boss=false;e.motion=EnemyMotion.Hover;e.capsuleRadius=.6f;e.capsuleHeight=1.4f;
                e.lootTable=Loot("MireflySwarm",Drop("AmberSap",1,.2f));});
            // Guardian: its front 150 degrees lets a quarter of a blow through; it turns at 75 degrees a second; a slam inside 3.4 m, and from further a root line 11 m long,
            // after which its arms are in the ground and its plate down for 1.6 s. It won't be led more than 10 m from its post.
            ArenaBuilder.Asset<EnemyDef>("Rootwarden",e=>{e.id="Rootwarden";e.displayName="Rootwarden";e.prefab=Body("Rootwarden","SentinelHusk");e.integrity=820;e.breakThreshold=220;e.speed=2.2f;e.attacks=new[]{rootSlam,rootLine};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.Guardian;e.tune=new BehaviourTune{arc=150,guard=.25f,reach=11,width=1.9f,recover=1.6f,wake=10,speed=75};
                e.xp=85;e.scrip=24;e.scrap=3;e.boss=false;e.heavyDeath=true;
                e.lootTable=Loot("Rootwarden",Drop("Rootfibre",3,1),Drop("AmberSap",2,.6f),ProgressionBuilder.AnyPart(3,.08f));});

            // ---- the two fights. Integrity is sized on the party's measured rate (XylosCombatTests pins these literals to the assets and holds each to its time), for the party the
            // chapter's loadout arrives with (Refined parts in every slot; QaPlanCombatTests measures every boss since chapter 2 the same way): the Seedthane for about 75 s of a
            // Sync 11 party (1,139 a second flat out on its body alone, 1,343 with its legs; measured November 12), the Floodcrown for about three minutes of a Sync 12 one. A leg or a gate broken takes a share of the bar with it, and
            // those shares, like every other number of the moves, are in the two BossController files' own tables.
            // 39,000 since November 14: the shared measure now samples a minute and reads its sustained rate (1,213 a second, where twenty seconds of opening skills read 1,498), and
            // at 44,000 a quarter of that was 145.04 s against its 145 s ceiling; the next run read 1,148, and 42,000 was 146 s. 39,000 holds the ceiling down to 1,076 a second.
            // In the player 44,000 fell in 82 to 89 s, so this is about 75: a minute to a minute and a half, as its sheet asks.
            ArenaBuilder.Asset<EnemyDef>("Seedthane",e=>{e.id="Seedthane";e.displayName="THE SEEDTHANE";e.prefab=Body("Seedthane","SentinelHusk");e.integrity=50000;e.breakThreshold=650;e.speed=3.4f;e.attacks=new[]{thaneStab};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.None;e.xp=240;e.scrip=180;e.scrap=10;e.boss=true;e.bossMusicId="Creature Boss";
                // A capsule can't stand shorter than it is wide: on the boss's default 3 m its 1.6 m radius (its Kit's) held it a tenth of a metre off the floor.
                e.capsuleHeight=3.2f;
                e.lootTable=Loot("Seedthane",Drop("Rootfibre",6,1),Drop("AmberSap",4,1),ProgressionBuilder.AnyPart(3,1));});
            ArenaBuilder.Asset<EnemyDef>("Floodcrown",e=>{e.id="Floodcrown";e.displayName="THE FLOODCROWN";e.prefab=Body("Floodcrown","Burrower");e.integrity=112000;e.breakThreshold=850;e.speed=3.2f;e.attacks=new[]{crownGore};
                e.weakness=DamageType.Beam;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.None;e.xp=380;e.scrip=320;e.scrap=16;e.boss=true;e.bossMusicId="Creature Boss";
                // On the boss's default 3 m its 2 m radius made Unity stretch the capsule round its middle, and it waded half a metre over the basin's floor.
                e.capsuleHeight=4;
                e.lootTable=Loot("Floodcrown",Drop("HeartwoodNode",1,1),Drop("AmberSap",6,1),Drop("Rootfibre",6,1),Drop("RepairGel",2,1),ProgressionBuilder.AnyPart(3,1));});
        }
    }
}
