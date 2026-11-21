using Lattice.Data;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 4's enemies (brief, Enemies; manifest section 5 for every id and name): eight ordinary kinds, each one family of the behaviour library (Lattice.Combat.EnemyBehaviour;
    /// CHAPTER_GUIDE section 5, "Enemy behaviours"), each with one tell and one answer unlike its neighbours'; and the Rotbloom, whose moves are BossController.Rotbloom.cs. Every number a
    /// fight is tuned by is here, and only here.
    /// <para>Sizing. The party arrives at Sync 13 in Prime parts (DevLoadout chapter4: the plain Prime frame, Plating 26) and leaves at 15. A hero's bar is 120 + 18 per Sync past the
    /// first: 336, 354, 372. Plating is 10 + 2 per Sync past the first + the frame's 26: 60, 62, 64, and a hit lands at 100/(100 + Plating - 10) of itself: .667, .658, .649. So at a zone
    /// threat of 1 an authored hit of D costs D x .667 / 336 of the bar at Sync 13 and D x .649 / 372 at 15: 9% of the bar at 15 needs D = 52, 13% at 13 allows D = 65. Every ordinary attack
    /// here is authored between 52 (a dart, a bite: 10.3% at 13, 9.1% at 15) and 64 (a spear line, a ray's pass: 12.7% and 11.2%), and every zone of the chapter stays at threat 1. The
    /// Rotbloom's moves are 60 to 72, each 12.3 to 15.8% of a Sync 14 bar in whichever phase it is used (the second adds 18%): a boss's weight, and more than anything on the road
    /// to it (the sentry's spear, 11.9%).</para>
    /// <para>XP. Sync 13 to 15 costs 1,625 + 1,750 = 3,375 a hero. The main quest pays 1,000, so the kills on the main path are sized to about 2,300 as the encounters are planned
    /// (docs/maps/DrownedWaystation.md: 3 Bogstriders 300, 3 Lurelights 240, 4 leech knots 96, 5 sentries 650, 2 rot-pods 140; the Veil about 10 pods shot 100, 8 swarms 144 and the ray 180;
    /// the Rotbloom 520 and the two rot-pods it seeds), which ends the fight at about Sync 15 with nothing optional done. The side quests' 920 and the off-lane pockets carry a thorough player
    /// a third of the way to 16. A swarm the party flew dark past pays nothing, and that is the Veil's rule working.</para>
    /// </summary>
    public static partial class WendmireBuilder
    {
        static void DefineEnemies()
        {
            GameObject Body(string id,string fallback)=>ZoneKit.Body("Enemies",id,"Enemies",fallback);
            LootTable Loot(string id,params LootEntry[] entries)=>ArenaBuilder.Asset<LootTable>(id,t=>t.entries=entries);
            LootEntry Drop(string id,int count,float chance)=>ProgressionBuilder.Drop(id,count,chance);
            AttackDef Attack(string id,float damage,float range,float telegraph,float cooldown,DamageType type,float radius=1,bool projectile=false)=>
                ArenaBuilder.Asset<AttackDef>(id,a=>{a.damage=damage;a.range=range;a.telegraph=telegraph;a.cooldown=cooldown;a.type=type;a.radius=radius;a.projectile=projectile;});

            // ---- the attacks, at a zone threat of 1. Range is where the move starts from (a dart's leap, a lob's throw, a pass's run-in, the ring a pod is set off inside), not always
            // how far it reaches: that is the behaviour's own tune.reach.
            var sporeDart=Attack("SporeDart",52,9,.65f,2.8f,DamageType.Kinetic);
            var sporeBurst=Attack("SporeBurst",52,2.5f,.45f,0,DamageType.Pulse,2.5f);
            var podBurst=Attack("PodBurst",54,4.5f,.8f,0,DamageType.Pulse,3.2f);
            var rayPass=Attack("RayPass",62,15,1.2f,4.5f,DamageType.Kinetic);
            var rayTail=Attack("RayTail",56,7,.7f,3,DamageType.Kinetic);
            var lureSnap=Attack("LureSnap",52,2.8f,.6f,1.6f,DamageType.Kinetic);
            var bogStomp=Attack("BogStomp",62,4.2f,.85f,2.4f,DamageType.Kinetic,2.4f);
            var bogKick=Attack("BogKick",56,4.5f,.55f,2,DamageType.Kinetic);
            var leechDart=Attack("LeechDart",52,3,.6f,2.6f,DamageType.Kinetic);
            var leechBurst=Attack("LeechBurst",52,2.5f,.45f,0,DamageType.Pulse,2.5f);
            var rotLob=Attack("RotLob",56,12,1.1f,3.2f,DamageType.Pulse,2.4f);
            var sentryBash=Attack("SentryBash",60,3.2f,.8f,2.2f,DamageType.Kinetic,2.2f);
            var sentrySpear=Attack("SentrySpear",64,10,.9f,3,DamageType.Kinetic);
            var rotSnap=Attack("RotSnap",66,7,.8f,2.6f,DamageType.Kinetic);

            // ---- the Spore Veil (flight)
            // Swarm cloud, drawn to light: twenty-two green motes with no model. Awake in a place with the glow rule (ShipGlow) it leaves a ship flown dark alone unless it comes
            // within 6 m; it darts on a marked circle and rides the ship for 3 s at .6 of its speed, draining 40 Thrust a second (23 more than a hull makes back), until it rolls, the swarm
            // is hit, or it has flown dark two seconds. Broken (45: one Pulse hit) or killed, it bursts on a marked .45 s fuse. Answer: go dark, or roll it off, and break it from a step back.
            ArenaBuilder.Asset<EnemyDef>("SporeSwarm",e=>{e.id="SporeSwarm";e.displayName="Spore Swarm";e.prefab=null;e.integrity=200;e.breakThreshold=45;e.speed=6;e.attacks=new[]{sporeDart,sporeBurst};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Swarm;e.behaviour=EnemyBehaviourKind.SwarmCloud;
                e.tune=new BehaviourTune{linger=3,pace=.6f,tick=40,recover=1.2f,count=22,wake=6,tint=new Color(.7f,1.25f,.62f,1)};
                e.xp=18;e.scrip=4;e.scrap=0;e.boss=false;e.motion=EnemyMotion.Hover;e.capsuleRadius=.8f;e.capsuleHeight=1.4f;
                e.lootTable=Loot("SporeSwarm",Drop("Glowspore",1,.2f));});
            // Burster: a pod drifting on a 1.5 m loop, its own 2.2 m body (origin at its stalk's tip: EnemyBurster draws it round its capsule). A ship inside 4.5 m, or a shot that doesn't finish it, sets it off: it swells on a marked 3.2 m circle for .8 s and bursts, and its
            // spores stand there 3.5 s as a Blind cloud that hides every marking laid inside it. Shot dead from range it bursts at once into its cloud and strikes nobody. Answer: clear the
            // lane from range and fly round the cloud. A pod that went off by itself pays nothing.
            ArenaBuilder.Asset<EnemyDef>("SporePod",e=>{e.id="SporePod";e.displayName="Spore Pod";e.prefab=Body("SporePod","RotPod");e.integrity=60;e.breakThreshold=100000;e.speed=.6f;e.attacks=new[]{podBurst};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.Burster;
                e.tune=new BehaviourTune{wake=4.5f,linger=3.5f,area=4.5f,effect=AreaEffect.Blind,reach=1.5f,tint=new Color(.85f,1,.72f,.85f)};
                e.xp=10;e.scrip=2;e.scrap=0;e.boss=false;e.motion=EnemyMotion.Hover;e.drawnScale=1;e.capsuleRadius=1.1f;e.capsuleHeight=2.2f;e.heavyDeath=false;
                e.lootTable=Loot("SporePod",Drop("Glowspore",1,.35f));});
            // Strafer on a circuit: it glides a 22 m loop round where it was set, and a ship that comes within 15 m of it gets a 24 m pass, 4 m wide, marked 1.2 s, at 18 m/s, after which
            // it breaks away for 1.6 s and goes back to its loop. A ship close behind it, inside 7 m and its tail's 140 degrees, is swept on a marked fan. Answer: cross its nose at the
            // end of the tell, never tail it; or time the dead beacon to its far pass.
            ArenaBuilder.Asset<EnemyDef>("VeilRay",e=>{e.id="VeilRay";e.displayName="Veil Ray";e.prefab=Body("VeilRay","ChoristerDrifter");e.integrity=1800;e.breakThreshold=300;e.speed=9;e.attacks=new[]{rayPass,rayTail};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Strafer;
                e.tune=new BehaviourTune{reach=24,width=4,speed=18,recover=1.6f,wake=22,arc=140};
                e.xp=180;e.scrip=40;e.scrap=4;e.boss=false;e.motion=EnemyMotion.Hover;e.capsuleRadius=2.2f;e.capsuleHeight=1.4f;
                e.lootTable=Loot("VeilRay",Drop("ReedWeave",2,.6f),Drop("Glowspore",2,.6f),Drop("ChargeCell",1,.2f),ProgressionBuilder.AnyPart(3,GearSlot.Drive,.12f));});

            // ---- the mire
            // Ambusher, disguised: it stands as a lamp-post, out of reach, its lamp flickering in shorts that never hold a long (Driftreed's lamps keep two short and one long). A hero inside
            // its 3.4 m ring is snapped at twice, .6 s and .5 s; then it stands spent in the open for 1.6 s before it hops to another post. Answer: read the flicker, step in to draw it, roll
            // the snaps, and hit it while it is spent.
            ArenaBuilder.Asset<EnemyDef>("Lurelight",e=>{e.id="Lurelight";e.displayName="Lurelight";e.prefab=Body("Lurelight","Rootwarden");e.integrity=420;e.breakThreshold=110;e.speed=4.4f;e.attacks=new[]{lureSnap};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Ambusher;
                e.tune=new BehaviourTune{wake=3.4f,count=2,speed=6,recover=1.6f,disguised=true,tint=new Color(.9f,1.15f,.3f,1)};
                e.xp=80;e.scrip=14;e.scrap=1;e.boss=false;e.capsuleRadius=.5f;e.capsuleHeight=3;
                e.lootTable=Loot("Lurelight",Drop("Glowspore",2,.6f),ProgressionBuilder.AnyPart(3,GearSlot.Emitter,.03f));});
            // Stomper: slow to turn (55 degrees a second); a stomp on a marked 2.4 m circle before it inside 4.2 m, a kick across a marked 120-degree fan behind it inside 4.5 m, nothing at
            // its flanks. A stomp that strikes nobody sticks its foot in the mire, open for 1.8 s. Answer: keep to its flank; draw the stomp and step aside; never stand behind it.
            ArenaBuilder.Asset<EnemyDef>("Bogstrider",e=>{e.id="Bogstrider";e.displayName="Bogstrider";e.prefab=Body("Bogstrider","Gloomgrazer");e.integrity=900;e.breakThreshold=240;e.speed=2.6f;e.attacks=new[]{bogStomp,bogKick};
                e.weakness=DamageType.Kinetic;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.Stomper;
                e.tune=new BehaviourTune{speed=55,arc=120,recover=1.8f};
                e.xp=100;e.scrip=22;e.scrap=2;e.boss=false;e.capsuleRadius=.9f;e.capsuleHeight=2.2f;
                e.lootTable=Loot("Bogstrider",Drop("ReedWeave",2,.7f),Drop("Glowspore",1,.3f),ProgressionBuilder.AnyPart(3,GearSlot.Frame,.05f));});
            // Swarm cloud in the water: a knot of fourteen leeches with no model. It darts on a marked circle and latches for 2.4 s at .55 of a hero's stride until he rolls or it is hit;
            // broken (45: one Pulse hit) or killed it bursts on a marked .45 s fuse. Answer: break it from a step back, or take the dry way round.
            ArenaBuilder.Asset<EnemyDef>("MireLeeches",e=>{e.id="MireLeeches";e.displayName="Mire Leeches";e.prefab=null;e.integrity=220;e.breakThreshold=45;e.speed=3.6f;e.attacks=new[]{leechDart,leechBurst};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Swarm;e.behaviour=EnemyBehaviourKind.SwarmCloud;
                e.tune=new BehaviourTune{linger=2.4f,pace=.55f,recover=1.4f,count=14,tint=new Color(1,.52f,.42f,1)};
                e.xp=24;e.scrip=4;e.scrap=0;e.boss=false;e.capsuleRadius=.6f;e.capsuleHeight=1.2f;
                e.lootTable=Loot("MireLeeches",Drop("Glowspore",1,.25f));});

            // ---- the station
            // Lobber, rooted, with a shell: it lobs a pod 1.1 s in the air onto a marked 2.4 m circle from 12 m, which stands 5 s as a cloud slowing to .55. Struck, it shuts for 2.2 s
            // (a blow lands at .15, read as resisted) and throws nothing; as it opens again it is soft for 1.2 s. Answer: step out of the circle toward it, strike, and punish the reopening.
            ArenaBuilder.Asset<EnemyDef>("RotPod",e=>{e.id="RotPod";e.displayName="Rot-pod";e.prefab=Body("RotPod","Sporecaster");e.integrity=520;e.breakThreshold=120;e.speed=0;e.attacks=new[]{rotLob};
                e.weakness=DamageType.Beam;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.Lobber;
                e.tune=new BehaviourTune{linger=5,area=2.8f,effect=AreaEffect.Slow,pace=.55f,guard=.15f,recover=2.2f};
                e.xp=70;e.scrip=12;e.scrap=1;e.boss=false;e.capsuleRadius=.8f;e.capsuleHeight=1.8f;
                e.lootTable=Loot("RotPod",Drop("Glowspore",2,.5f),Drop("RepairGel",1,.06f),ProgressionBuilder.AnyPart(3,GearSlot.Module,.03f));});
            // Guardian: a shield over its front 120 degrees lets a fifth of a blow through; it turns at 85 degrees a second; a bash on a marked circle inside 3.2 m, and from further a spear
            // thrust down a marked line 10 m long and 1.4 m wide, after which its spear is in the floor and its shield down for 1.4 s. It holds a post 9 m round. Answer: its flank or back;
            // or step off the spear's line and go in while it is down.
            ArenaBuilder.Asset<EnemyDef>("DrownedSentry",e=>{e.id="DrownedSentry";e.displayName="Drowned Sentry";e.prefab=Body("DrownedSentry","SentinelHusk");e.integrity=1050;e.breakThreshold=260;e.speed=2;e.attacks=new[]{sentryBash,sentrySpear};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.Guardian;
                e.tune=new BehaviourTune{arc=120,guard=.2f,reach=10,width=1.4f,recover=1.4f,wake=9,speed=85};
                e.xp=130;e.scrip=28;e.scrap=3;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.7f;e.capsuleHeight=2.2f;
                e.lootTable=Loot("DrownedSentry",Drop("ScrapAlloy",3,.8f),Drop("LatticeFilament",2,.4f),Drop("ReedWeave",1,.4f),ProgressionBuilder.AnyPart(3,.08f));});

            // ---- the Rotbloom. Integrity is sized on the party's measured rate (WendmireCombatTests pins this literal to the asset and holds the fight to its time): two to five minutes for a
            // Sync 14 party in the Prime parts its chapter arrives in (DevLoadout chapter4: 934 to 1,177 a second flat out on November 14; it drinks light, so Sela's beam lands at half),
            // two stalks and then the bar. QaPlanCombatTests measures it in the same parts. A stalk is a quarter of its integrity and takes a quarter of the bar with it, and like every
            // other number of its moves that is in BossController.Rotbloom.cs's own table. 74,000 lasted 161.6 s for the replay driver fighting it as a person does (RotbloomDriverTests,
            // November 14: a share of .465), and was over the shared ceiling (a quarter of flat out inside five minutes) at every rate under 987; 68,000 held that ceiling down to 907.
            // The whole suite then read 830 a second with the measure at full frame rate (November 14, nov14b): its flat-out rate has ranged from 830 to 1,047 on one day, and the
            // sheet's floor (.48 of flat out for two minutes) and the ceiling (a quarter of it inside five) leave room for all of that range only between 60,307 and 62,250.
            // 61,500 is inside it; in the player 68,000 fell in 108 s of fighting for the driver, so this is about 98 s.
            // It drops the region's rare, the Lure Bulb, which is the key to the Lurelight Drive.
            ArenaBuilder.Asset<EnemyDef>("Rotbloom",e=>{e.id="Rotbloom";e.displayName="THE ROTBLOOM";e.prefab=Body("Rotbloom","Floodcrown");e.integrity=63500;e.breakThreshold=900;e.speed=1.4f;e.attacks=new[]{rotSnap};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.None;e.xp=520;e.scrip=380;e.scrap=18;e.boss=true;
                // A capsule can't stand shorter than it is wide: at 3.2 m tall on its 3.4 m radius Unity stretched it and the bloom hung 1.8 m over the floor.
                e.capsuleHeight=6.8f;
                e.lootTable=Loot("Rotbloom",Drop("LureBulb",1,1),Drop("Glowspore",6,1),Drop("ReedWeave",6,1),Drop("RepairSalve",2,1),ProgressionBuilder.AnyPart(3,1));});
        }
    }
}
