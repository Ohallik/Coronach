using Lattice.Data;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 8's ordinary enemies, the Picket's flight fights and the elite Vessa Korl (brief, Enemies; manifest section 5 for every id and name). Each ordinary kind is one family of the
    /// behaviour library (Lattice.Combat.EnemyBehaviour; CHAPTER_GUIDE section 5, "Enemy behaviours"), with one tell and one answer unlike its neighbours'; Vessa's moves are
    /// BossController.VessaKorl.cs. Every number a fight here is tuned by is in this file or in her VessaTune, and only there. <c>HarrowgateBuilder.Define</c> calls <c>DefineEnemies</c> after the
    /// zones and before the quests; it ends by calling <c>BossDefinitions</c> (HarrowgateBuilder.Bosses.cs, the second combat engineer's), so a boss can name an enemy defined here.
    /// <para>The roster, and how each is answered. <b>The Picket</b> (flight): the Sensor Buoy flares all round it at a ship that comes close (keep off it, or shoot it from outside its
    /// ring); the Muster Lancer runs a long marked lane and then comes about, slow and open (cross out of the lane, hit it on its turn; it loses a ship a rock hides from it); the Flak Gate
    /// bursts flak in alternate rows of a curtain across the lane on a clock (cross in the quiet rows); the Muster Skiff shoots down a marked line as it did in chapter 2. <b>The Flows and the
    /// Foundry</b> (on foot): the Muster Pikeguard fights in a pair, one bracing a long thrust while the other covers it (get between them: neither strikes across the other); the Muster
    /// Mortar lobs onto a marked circle that burns after (step out toward it); the Ash Crawler lunges out of a ringed hide under the crust and flees bright flow (step in to draw it, roll the
    /// lunge, hit it spent); the Pour Drone rides its rail and pours down it, and what it poured burns (step out from under the rail); the Forge Hound leaps a marked lane, is knocked down
    /// rather than broken when caught in the air, and scorches where it lands (roll the lane, don't roll back); chapter 2's Scavenger and Sergeant return as they fought, at this chapter's
    /// weight. Chapter 2's own MusterScav, MusterSergeant and MusterSkiff are tuned for Sync 9 and are not placed in this chapter.</para>
    /// <para>Sizing. The party arrives at Sync 22 in Tempered parts (DevLoadout chapter8: the plain Tempered frame, Plating 38) and leaves at 25. A hero's bar is 120 + 18 per Sync past the
    /// first: 498 at 22, 552 at 25. Taren's and Sela's Plating is 10 + 2 per Sync past the first + the frame's (90 at 22, 96 at 25); Idra's, the guard hero's, 16 + 3 per Sync past the first
    /// + the frame's (117 at 22, 126 at 25), and a hit lands at 100/(100 + Plating - 10) of itself. So at a zone threat of 1 an authored hit of D costs Taren or Sela at 22 D x .5556 / 498 of
    /// the bar (the most anyone pays: 13% allows D = 116) and Idra at 25 D x .4630 / 552 (the least: 9% needs D = 108). Every ordinary attack here is authored between 108 and 116 (Taren at
    /// 22: 12.0 to 12.9%; Idra at 25: 9.1 to 9.7%), and every zone of the chapter stays at threat 1. What a mortar's shell, a drone's pour or a hound's landing leaves burning is a
    /// marked danger of 30 to 36 a second on top (about 3.5% of a bar a second), which a step out of it ends. Vessa is met at 22 to 23: VessaTune holds her at 12 to 16%.</para>
    /// <para>XP. Sync 22 to 25 costs 2,750 + 2,875 + 3,000 = 8,625 a hero. The second combat engineer's bosses pay 4,000 (the Colossus 1,800, the duel 1,200, the pursuit 1,000:
    /// HarrowgateBuilder.Bosses.cs), and the guard wing Drask calls in about 220 (two of his shouts answered, Pikeguards at 110). This ledger assumes the main quest pays 1,000 and the four side
    /// quests 800 between them (HarrowgateBuilder.Quests, the first world engineer's), and the encounters the zones' briefs plan: on the main path the Picket 140 (the hunter wing the lock
    /// wakes, 2 Lancers; a buoy), the Flows 1,040 (the causeway's pair and a Scav, the delta's 3 Crawlers, the battery's 2 Mortars and a Scav, the gate's pair and a Sergeant), the Foundry 770
    /// before the gallery (the yard's 2 Hounds, the charging floor's pair and a Hound, the press bay's 2 Hounds, gallery south's 2 Drones and a Pikeguard) and Vessa 420 with her pair 220:
    /// 2,590 of the ordinary and elite. So the party meets Vessa at about Sync 22.7 (22.9 with a side quest and the optional pockets on the way), the Colossus at about 23, Drask at about
    /// 23.8, and the main path with the main quest ends the chapter at 24.7 (7,810); the side quests take it to 25.0 (8,610), and the optional fights (the skip store's pair 220, the cinder
    /// hollow's 2 Crawlers 140, a loud Picket's second and third wings 300, the two flak gates 160, the other buoys 60) to about 25.3. A Picket crossed quietly pays almost nothing, and that
    /// is its rule working. Main path, main quest and side quests: 8,610 against 8,625.</para>
    /// <para>Drops. The Reach's commons are Scoria Glass (the Flows' and the Foundry's ground soldiers and beasts) and Bridle Wire (the Picket's craft and the Foundry's machines); its rare,
    /// the Foundry Seal, only the Colossus drops. Tempered parts come out of the bigger bodies at a few percent, beside the camp's and the back table's counters.</para>
    /// </summary>
    public static partial class HarrowgateBuilder
    {
        static void DefineEnemies()
        {
            GameObject Body(string id,string fallback)=>ZoneKit.Body("Enemies",id,"Enemies",fallback);
            LootTable Loot(string id,params LootEntry[] entries)=>ArenaBuilder.Asset<LootTable>(id,t=>t.entries=entries);
            LootEntry Drop(string id,int count,float chance)=>ProgressionBuilder.Drop(id,count,chance);
            AttackDef Attack(string id,float damage,float range,float telegraph,float cooldown,DamageType type,float radius=1,bool projectile=false)=>
                ArenaBuilder.Asset<AttackDef>(id,a=>{a.damage=damage;a.range=range;a.telegraph=telegraph;a.cooldown=cooldown;a.type=type;a.radius=radius;a.projectile=projectile;});

            // ---- the attacks, at a zone threat of 1. Range is where the move starts from (a pass's run-in, a lob's throw, a lunge's ring), or for the anchored two how near a ship must be;
            // how far it reaches is the behaviour's own tune.reach.
            var buoyFlare=Attack("BuoyFlare",108,8,1,3.2f,DamageType.Pulse);
            var lancerRun=Attack("LancerRun",114,16,1,3.6f,DamageType.Kinetic);
            var flakBurst=Attack("FlakBurst",110,40,1,2.2f,DamageType.Kinetic);
            var skiffShot=Attack("ReachSkiffShot",108,13,.75f,2.4f,DamageType.Pulse,1,true);
            var pikeBash=Attack("PikeguardBash",110,3.2f,.8f,2.4f,DamageType.Kinetic,2.2f);
            var pikeThrust=Attack("PikeguardThrust",116,9,1,3.6f,DamageType.Kinetic);
            var mortarShell=Attack("MortarShell",112,13,1.3f,3.4f,DamageType.Plasma,2.4f);
            var crawlerLunge=Attack("CrawlerLunge",114,4.5f,.75f,0,DamageType.Kinetic);
            var dronePour=Attack("DronePour",112,10,1,3,DamageType.Plasma);
            var houndLeap=Attack("ForgeHoundLeap",112,7,.8f,2.6f,DamageType.Kinetic);
            var scavShot=Attack("ReachScavShot",108,11,.7f,2.2f,DamageType.Pulse,1,true);
            var sergeantMaul=Attack("ReachSergeantMaul",116,3,.8f,2,DamageType.Kinetic);
            var vessaJab=Attack("VessaLanceJab",132,3.4f,.7f,2.4f,DamageType.Kinetic);

            // ---- the Picket of Teeth (flight)
            // Anchored, all round: the beacon the picket's sweep is drawn from (the sweep and the alarm are the zone's: docs/maps/PicketOfTeeth.md). Close to it, inside 8 m, it flares: a
            // marked ring all round it for 1 s, then a Pulse burst. Answer: keep out of its ring, or shoot it from outside it (Pulse is its weakness). Every enemy resists something (the
            // shared weakness test): a sensor built to take light, it resists Beam.
            ArenaBuilder.Asset<EnemyDef>("SensorBuoy",e=>{e.id="SensorBuoy";e.displayName="Sensor Buoy";e.prefab=Body("SensorBuoy","Shellmine");e.integrity=700;e.breakThreshold=160;e.speed=0;e.attacks=new[]{buoyFlare};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.Anchored;e.tune=new BehaviourTune{arc=360};
                e.xp=20;e.scrip=6;e.scrap=1;e.boss=false;e.motion=EnemyMotion.Hover;e.heavyDeath=true;e.capsuleRadius=1;e.capsuleHeight=2.4f;
                e.lootTable=Loot("SensorBuoy",Drop("BridleWire",1,.25f),Drop("ScrapAlloy",2,.5f));});
            // Strafer that comes about, and hunts: it circles a ship at its 16 m run-in, marks a lane 26 m long and 2.6 m wide for 1 s and runs it at 26 m/s, striking what it touches in the
            // lane; then it comes about through a wide half turn at a third of that speed for 1.8 s, open. A ship it hasn't been able to see for 2 s (a rock's SensorShade between them, or
            // past 40 m) is lost: it circles where it is. When one dies its wing scatters. Answer: cross out of the lane as it fills, then hit it on its turn; or lose it behind a rock.
            ArenaBuilder.Asset<EnemyDef>("MusterLancer",e=>{e.id="MusterLancer";e.displayName="Muster Lancer";e.prefab=Body("MusterLancer","Podskimmer");e.integrity=950;e.breakThreshold=230;e.speed=11;e.attacks=new[]{lancerRun};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Strafer;
                e.tune=new BehaviourTune{reach=26,width=2.6f,speed=26,recover=1.8f,pace=.35f,linger=2};
                e.xp=60;e.scrip=14;e.scrap=2;e.boss=false;e.motion=EnemyMotion.Hover;e.heavyDeath=true;e.capsuleRadius=1.1f;e.capsuleHeight=2.4f;
                e.lootTable=Loot("MusterLancer",Drop("BridleWire",1,.35f),Drop("ScrapAlloy",2,.5f),ProgressionBuilder.AnyPart(4,GearSlot.Drive,.03f));});
            // Anchored with a curtain: set in the lane facing along it. While a ship is within 40 m it fills three rows across the lane before it (24 m deep together, 52 m across), the
            // first and third, then the second, each marked 1 s and then bursting, 3.2 s a beat; a row never follows a ship. Answer: cross in a quiet row, work in front of it on the
            // off-beat, or break it (Kinetic).
            ArenaBuilder.Asset<EnemyDef>("FlakGate",e=>{e.id="FlakGate";e.displayName="Flak Gate";e.prefab=Body("FlakGate","SnagvineNode");e.integrity=2400;e.breakThreshold=420;e.speed=0;e.attacks=new[]{flakBurst};
                e.weakness=DamageType.Kinetic;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.Anchored;e.tune=new BehaviourTune{count=3,reach=24,width=52};
                e.xp=80;e.scrip=24;e.scrap=3;e.boss=false;e.heavyDeath=true;e.capsuleRadius=2;e.capsuleHeight=4.4f;
                e.lootTable=Loot("FlakGate",Drop("ScrapAlloy",3,.8f),Drop("BridleWire",2,.5f),ProgressionBuilder.AnyPart(4,GearSlot.Frame,.05f));});
            // Chapter 2's skiff as it always fought, at chapter 8's weight: it keeps off and shoots down a marked line (.75 s). Answer: slip off the line, close on it.
            ArenaBuilder.Asset<EnemyDef>("ReachSkiff",e=>{e.id="ReachSkiff";e.displayName="Muster Skiff";e.prefab=Body("MusterSkiff","ChoristerDrifter");e.integrity=560;e.breakThreshold=140;e.speed=5;e.attacks=new[]{skiffShot};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.None;
                e.xp=35;e.scrip=9;e.scrap=1;e.boss=false;e.motion=EnemyMotion.Hover;e.heavyDeath=true;e.capsuleRadius=.9f;e.capsuleHeight=1.8f;
                e.lootTable=Loot("ReachSkiff",Drop("ScrapAlloy",2,.45f),Drop("BridleWire",1,.2f));});

            // ---- the Scoria Flows and the Bridle Foundry
            // Guardian in a pair: two woken within 7 m stand 4.2 m apart and face the party as one. One braces (turned on the hero, a pike thrust down a marked line 9 m long, 1 s; then its pike
            // is in the floor and its plate down 1.6 s) while the other covers it, plate to the hero and its bash for whoever comes in reach; they take turns. Neither strikes across the
            // other, so a hero between them is struck by neither and both their flanks are his, until they step apart round him (1.6 s). Each plate lets a fifth through over its 140 degrees.
            // Answer: get between them (Brekk's), or break one while the other's pike is down.
            ArenaBuilder.Asset<EnemyDef>("MusterPikeguard",e=>{e.id="MusterPikeguard";e.displayName="Muster Pikeguard";e.prefab=Body("MusterPikeguard","ThawedGuard");e.integrity=1500;e.breakThreshold=300;e.speed=2.4f;e.attacks=new[]{pikeBash,pikeThrust};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.Guardian;
                e.tune=new BehaviourTune{arc=140,guard=.2f,reach=9,width=1.5f,recover=1.6f,wake=10,speed=70,pair=true};
                e.xp=110;e.scrip=28;e.scrap=3;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.75f;e.capsuleHeight=2.3f;
                e.lootTable=Loot("MusterPikeguard",Drop("ScoriaGlass",1,.4f),Drop("ScrapAlloy",3,.8f),ProgressionBuilder.AnyPart(4,.05f));});
            // Lobber: a slag mortar on legs. From up to 13 m it lobs a shell 1.3 s in the air onto a marked 2.4 m circle, which burns 2.8 m across for 4 s (36 a second); it walks back from a hero
            // who closes, at a walk. Answer: step out of the circle toward it, and catch it.
            ArenaBuilder.Asset<EnemyDef>("MusterMortar",e=>{e.id="MusterMortar";e.displayName="Muster Mortar";e.prefab=Body("MusterMortar","Sporecaster");e.integrity=1300;e.breakThreshold=260;e.speed=1.6f;e.attacks=new[]{mortarShell};
                e.weakness=DamageType.Beam;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.Lobber;
                e.tune=new BehaviourTune{linger=4,area=2.8f,effect=AreaEffect.Burn,tick=36};
                e.xp=100;e.scrip=26;e.scrap=3;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.9f;e.capsuleHeight=2;
                e.lootTable=Loot("MusterMortar",Drop("ScoriaGlass",2,.5f),Drop("ChargeCell",1,.08f),ProgressionBuilder.AnyPart(4,GearSlot.Emitter,.04f));});
            // Ambusher that lunges: under the crust in a ringed hide (3.6 m), never where the ground is hot (GroundHeat: a seam about to go, or open melt), and out and away from a hide that
            // turns hot. A hero inside the ring draws it: it comes up and lunges down a marked lane 4.5 m long (.75 s), once, then stands spent in the open 2 s. Answer: step in to draw it,
            // roll the lunge, hit it spent; or let the crust move it.
            ArenaBuilder.Asset<EnemyDef>("AshCrawler",e=>{e.id="AshCrawler";e.displayName="Ash Crawler";e.prefab=Body("AshCrawler","RimeStalker");e.integrity=900;e.breakThreshold=200;e.speed=4.2f;e.attacks=new[]{crawlerLunge};
                e.weakness=DamageType.Beam;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Ambusher;
                e.tune=new BehaviourTune{wake=3.6f,count=1,speed=6,recover=2,reach=4.5f,width=1.6f};
                e.xp=70;e.scrip=16;e.scrap=1;e.boss=false;e.capsuleRadius=.7f;e.capsuleHeight=1.4f;
                e.lootTable=Loot("AshCrawler",Drop("ScoriaGlass",2,.5f),Drop("RepairGel",1,.06f),ProgressionBuilder.AnyPart(4,GearSlot.Edge,.03f));});
            // Rider on a rail, whose pour stays: a ladle on an overhead rail (the Foundry lays a ForceLine on the floor under each; with none it makes its own ahead of where it was set
            // facing). A hero under its rail within 10 m sees the strip lit 2.2 m wide for 1 s and the pour comes down it; the strip burns 3.5 s (36 a second). Answer: step out from under the
            // rail, and not back onto what it poured; hit it from beside the rail.
            ArenaBuilder.Asset<EnemyDef>("PourDrone",e=>{e.id="PourDrone";e.displayName="Pour Drone";e.prefab=Body("PourDrone","ClampTender");e.integrity=780;e.breakThreshold=180;e.speed=3.2f;e.attacks=new[]{dronePour};
                e.weakness=DamageType.Kinetic;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.Rider;
                e.tune=new BehaviourTune{reach=10,width=2.2f,linger=3.5f,effect=AreaEffect.Burn,tick=36,wake=16};
                e.xp=70;e.scrip=16;e.scrap=2;e.boss=false;e.motion=EnemyMotion.Hover;e.heavyDeath=true;e.capsuleRadius=.7f;e.capsuleHeight=2.4f;
                e.lootTable=Loot("PourDrone",Drop("BridleWire",1,.4f),Drop("ScrapAlloy",2,.6f),ProgressionBuilder.AnyPart(4,GearSlot.Module,.03f));});
            // Leaper, heavy and hot: the Muster's crude copy of the Bridle-hound. It circles at the edge of its 7 m spring, marks a lane .8 s and leaps it; whoever it lands on is struck and
            // pinned 1.2 s until he rolls or it is hit. Caught in the air it doesn't shatter, as the Ancelane one does: it is knocked down, open 1.65 s. Wherever it comes down it scorches the
            // floor 1.6 m round for 3.5 s (30 a second). Answer: roll the lane and don't roll back onto the scorch; or strike it in the air and punish it down; or brace.
            ArenaBuilder.Asset<EnemyDef>("ForgeHound",e=>{e.id="ForgeHound";e.displayName="Forge Hound";e.prefab=Body("ForgeHound","BridleHound");e.integrity=760;e.breakThreshold=180;e.speed=6.2f;e.attacks=new[]{houndLeap};
                e.weakness=DamageType.Beam;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Leaper;
                e.tune=new BehaviourTune{reach=7,width=1.7f,speed=16,linger=1.2f,pace=.15f,recover=1.1f,heavy=true,area=1.6f,effect=AreaEffect.Burn,tick=30};
                e.xp=60;e.scrip=16;e.scrap=2;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.65f;e.capsuleHeight=1.4f;
                e.lootTable=Loot("ForgeHound",Drop("ScoriaGlass",1,.35f),Drop("ScrapAlloy",2,.5f),ProgressionBuilder.AnyPart(4,GearSlot.Drive,.03f));});
            // Chapter 2's scavenger as it always fought, at chapter 8's weight: it keeps 5 to 9 m off and shoots down a marked line (.7 s). Answer: step off the line, and run it down.
            ArenaBuilder.Asset<EnemyDef>("ReachScav",e=>{e.id="ReachScav";e.displayName="Muster Scavenger";e.prefab=Body("MusterScav","SentinelHusk");e.integrity=640;e.breakThreshold=150;e.speed=2.8f;e.attacks=new[]{scavShot};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.None;
                e.xp=45;e.scrip=12;e.scrap=2;e.boss=false;
                e.lootTable=Loot("ReachScav",Drop("ScrapAlloy",2,.5f),Drop("ChargeCell",1,.06f));});
            // Chapter 2's sergeant as it always fought, at chapter 8's weight: heavy and close, a maul on a marked circle before it (.8 s). Answer: step back out of the circle and come in after.
            ArenaBuilder.Asset<EnemyDef>("ReachSergeant",e=>{e.id="ReachSergeant";e.displayName="Muster Sergeant";e.prefab=Body("MusterSergeant","SentinelHusk");e.integrity=1400;e.breakThreshold=280;e.speed=2.3f;e.attacks=new[]{sergeantMaul};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.None;
                e.xp=100;e.scrip=24;e.scrap=3;e.boss=false;e.heavyDeath=true;
                e.lootTable=Loot("ReachSergeant",Drop("ScrapAlloy",4,1),Drop("RepairGel",1,.2f),Drop("ScoriaGlass",1,.3f));});

            // ---- Vessa Korl, the casting gallery's elite. Integrity is sized on the party's measured rate (HarrowgateCombatTests pins this literal to the asset and holds the fight to its own
            // bounds for every pair a player can put out): about a minute to a minute and a half for a Sync 22 party in the chapter's arrival parts (Tempered), her whole bar, the first half in
            // the middle of the gallery and the second held at the gantry door she leaves by. Measured November 12, flat out: Taren and Idra 3,243 a second, Taren and Sela 3,145, Sela and
            // Idra 2,609; QaPlanCombatTests' own measure (its second hero stands off, so Idra's hammer rarely lands) 1,964 to 2,290 from run to run. At 60,000: 62 to 77 s for the replay
            // party (the floor, at the driver's share) and 92 to 122 s at a quarter of flat out (the ceiling), inside her sheet's own 50 to 140 s by about a sixth at each end.
            // She is chapter 6's Vessa as an enemy (no lance on the body: BossController.VessaKorl.cs draws it). A dig-master's frame grounds a shock and is built against heat: weak to
            // Pulse, resists Plasma (her sister, the other way round). She leaves, not falls: nothing of the rare (that is the Colossus's).
            ArenaBuilder.Asset<EnemyDef>("VessaKorl",e=>{e.id="VessaKorl";e.displayName="VESSA KORL";e.prefab=Body("VessaKorl","HessaKorl");e.integrity=82000;e.breakThreshold=650;e.speed=3;e.attacks=new[]{vessaJab};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.None;e.xp=420;e.scrip=240;e.scrap=12;e.boss=true;
                e.yieldsOnDefeat=false;e.heavyDeath=true;
                // A capsule can't stand shorter than it is wide: 2.6 m on her .8 m radius, her body's own height.
                e.capsuleHeight=2.6f;
                e.lootTable=Loot("VessaKorl",Drop("BridleWire",4,1),Drop("ScoriaGlass",4,1),Drop("RepairSalve",1,1),ProgressionBuilder.AnyPart(4,1));});

            BossDefinitions();
        }
    }
}
