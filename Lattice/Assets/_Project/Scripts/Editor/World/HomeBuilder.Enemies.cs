using Lattice.Data;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// <para><b>State on November 14.</b> <b>Defined and built</b> (<c>HomeBuilder.Definitions</c>, HOME_DEFS_OK; bodies bound by <c>art-bind.ps1</c>): all eight below, with their attacks and
    /// loot. <b>Tested</b> in <c>HomeCombatTests</c> (each test built wrong under <c>HOME_COMBAT_BROKEN=1</c> as well) and the shared tell, warning-sound and weakness tests. <b>New code</b>:
    /// <c>Dawn</c>/<c>DawnTakes</c>, <c>EnemyAnchored.Tether</c>, <c>EnemyStrafer.Pair</c>, <c>EnemyGuardian.Ground</c>, <c>RiotShield</c>, <c>MachineAtRest</c>, and modes in the tetherer and
    /// the ambusher (guide section 5). <b>Not done</b>: none of it seen in a player. The bosses' XP (1,600 and 1,350) is settled in HomeBuilder.Bosses.cs. <b>Contracts</b>: Nacre's, the
    /// Works' and the Ring's are written in their map docs; the Terminator's answer (register <c>Dawn</c>, not <c>GroundHeat</c>, and a <c>ShadeCover</c>) is with that zone's engineer.</para>
    /// Chapter 9's ordinary enemies and its flight fights (brief, Enemies; manifest section 5 for every id and name; docs/chapters/ch09-wiring.md for who places what). The first combat
    /// engineer's file. Every enemy here was built to help or is somebody doing a job, and has been turned or sent: each is one family of the behaviour library (Lattice.Combat.EnemyBehaviour;
    /// CHAPTER_GUIDE section 5, "Enemy behaviours"), four of them in a mode chapter 9 added, with one tell and one answer unlike its neighbours'. Every number a fight here is tuned by is in this
    /// file. <see cref="Define"/> calls <see cref="Enemies.Define"/> after the zones and before <see cref="Bosses.Define"/> (HomeBuilder.Bosses.cs, the second combat engineer's), so a boss
    /// can name one of these. What each zone gives them is that zone's map doc, under its contract: docs/maps/NacreGrave.md and BrokenRing.md ("Flight fights contract"), Terminator.md
    /// ("Run fights contract"), SeparationWorks.md ("Works fights contract").
    /// <para>The roster, and how each is answered. <b>Nacre Grave</b> (flight): the Tow-drone harpoons a ship down a marked line and hauls it the way it was set facing, toward the berth that
    /// isn't there, reeling it in behind (roll the line, or one shot on the drone); the Bridle Fragment thrashes a marked fan on its free side and can't swing over its own tether (come in along
    /// the tether). <b>The Terminator</b> (on foot, with the dawn line coming on): the Heat-scavenger hunts the warm band just ahead of the line from ringed hides, lunges down a marked lane and
    /// flees shade (keep to shade, or roll the lunge and hit it spent); the Slag Gull hovers off and spits molten slag onto a marked circle that burns (step out toward it, shoot it down); the
    /// dawn takes any of them it reaches, in sight (Lattice.Combat.Dawn). <b>The Separation Works</b>: the Override Rig, clamped at the end of a machine's cable, discharges all round it on a marked
    /// ring (strike between discharges; kill it and its machines are home's again); the Weld Arm lays a weld down a marked strip that burns after (step off it, strike it from beside) and
    /// stands down at rest when its override is cut; the Compact Warden fights behind a shield with a stun-baton, gives ground when its guard breaks and at nought stands aside and walks out
    /// (get round the shield or break it: nobody of Meret's is killed on screen, and none of them speaks). <b>The Broken Ring</b> (flight): Interceptors in pairs, one pinning a ship with a
    /// tractor line while the other runs a marked lane through it (break the line or shoot the one holding it, then cross out of the lane).</para>
    /// <para>Sizing. The party arrives at Sync 25 in Tempered parts (DevLoadout chapter9: the plain Tempered frame, Plating 38) and leaves at 27, where the chapter's stalls, caches and main quest
    /// have made Choirwrought reachable (the plain frame, Plating 52). A hero's bar is 120 + 18 per Sync past the first: 552 at 25, 588 at 27. Taren's and Sela's Plating is 10 + 2 per Sync past
    /// the first + the frame's (96 at 25 in Tempered); Idra's, the guard hero's, 16 + 3 per Sync past the first + the frame's (146 at 27 in Choirwrought), and a hit lands at
    /// 100/(100 + Plating - 10) of itself. So at a zone threat of 1 an authored hit of D costs Taren or Sela at 25 in Tempered D x .5376 / 552 of the bar (the most anyone pays: 13% allows
    /// D = 133) and Idra at 27 in Choirwrought D x .4237 / 588 (the least: 9% needs D = 125). Every ordinary hit here is authored between 126 and 132:</para>
    /// <code>
    ///                                   D    Taren/Sela 25 (T4)  Idra 25 (T4)  Taren/Sela 27 (T5)  Idra 27 (T5)
    ///   Rig discharge, Scavenger lunge  126/129   12.3/12.6%     10.6/10.8%      10.5/10.8%       9.1/9.3%
    ///   Gull spit, Tow harpoon          127/128   12.4/12.5%     10.7/10.7%      10.6/10.7%       9.2/9.2%
    ///   Warden baton, Fragment lash     128/130   12.5/12.7%     10.7/10.9%      10.7/10.8%       9.2/9.4%
    ///   Weld bead, Interceptor run      130/130   12.7%          10.9%           10.8%            9.4%
    ///   Warden shield drive             131       12.8%          11.0%           10.9%            9.4%
    /// </code>
    /// <para>What a spit, a weld or a scavenger leaves burning is a marked danger of 38 a second on top (3.7% of Taren's bar a second at 25), which a step out of it ends. The Interceptor's tractor
    /// line hurts nobody: the pass it holds a ship still for does. Every zone of the chapter stays at threat 1 (HomeBuilder.Define). HomeCombatTests measures every hit on all three heroes.</para>
    /// <para>XP. Sync 25 to 27 costs 3,125 + 3,250 = 6,375 a hero. Quests pay 1,800 (the main quest 1,000 and the four side quests 200 each: HomeBuilder.Quests, the first world engineer's).
    /// The bosses, as HomeBuilder.Bosses.cs defines them (settled November 14; they had been 2,000 and 1,800, which would have ended the chapter at 27.3): the Foundry Saint 1,600, the Debris
    /// Cutter 1,350 (2,950); a different split moves the Syncs below by its difference over 3,125 or 3,250. The encounters the zones place (each zone's map doc): on the main path, Nacre's three tows
    /// (6 Tow-drones at 40: 240), the run's six fights (8 Heat-scavengers at 45 and 4 Slag Gulls at 50: 560), the Works' throat, control room and gallery (3 Override Rigs at 35, 5 Compact
    /// Wardens at 80, a Weld Arm at 80: 585) and the Ring's two pairs on the way to the Cutter (4 Interceptors at 60: 240): 1,625. So the party meets the Saint at about Sync 25.4 (25.7 with the
    /// side quests done on the Decks first), the Cutter at about 26.0 (26.3), and the main path with the main quest ends the chapter at 26.75 (5,575); the side quests take it to 27.0 (6,375
    /// against 6,375), and the optional fights (Nacre's breach fragment 120, the Compact module's two Wardens 160, the courier skiff's arm 80, the Ring's third pair 120) to about 27.14. A run
    /// kept moving past the scavengers pays 360 less and ends the chapter at 26.9.</para>
    /// <para>Drops. The Separation's commons are Halo Rivet (the run's and the Works' bodies, ground and iron) and Nacre Shard (Nacre's and the Ring's craft); its rare, the Ring Keystone, only a
    /// boss drops. Choirwrought parts come out of the bigger bodies at a few percent, beside the Works' caches: every table that can drop one drops a Separation common too, which is what makes it
    /// chapter 9's (ProgressionTests).</para>
    /// </summary>
    public static partial class HomeBuilder
    {
        internal static class Enemies
        {
            /// <summary>Every ordinary EnemyDef, AttackDef and LootTable of the chapter, at a zone threat of 1.</summary>
            internal static void Define()
            {
                GameObject Body(string id,string fallback)=>ZoneKit.Body("Enemies",id,"Enemies",fallback);
                LootTable Loot(string id,params LootEntry[] entries)=>ArenaBuilder.Asset<LootTable>(id,t=>t.entries=entries);
                LootEntry Drop(string id,int count,float chance)=>ProgressionBuilder.Drop(id,count,chance);
                AttackDef Attack(string id,float damage,float range,float telegraph,float cooldown,DamageType type,float radius=1,bool projectile=false)=>
                    ArenaBuilder.Asset<AttackDef>(id,a=>{a.damage=damage;a.range=range;a.telegraph=telegraph;a.cooldown=cooldown;a.type=type;a.radius=radius;a.projectile=projectile;});

                // ---- the attacks, at a zone threat of 1. Range is where the move starts from (a harpoon's hold, a lob's throw, a lunge's ring, a pass's run-in), or for the anchored and the
                // rider how near a hero must be; how far it reaches is the behaviour's own tune.reach.
                var towHarpoon=Attack("TowHarpoon",128,12,.9f,3.4f,DamageType.Kinetic);
                var fragmentLash=Attack("FragmentLash",130,9.5f,1,2.6f,DamageType.Kinetic);
                var scavengerLunge=Attack("ScavengerLunge",129,5,.8f,0,DamageType.Kinetic);
                var gullSpit=Attack("GullSpit",127,12,1.2f,3.2f,DamageType.Plasma,2.2f);
                var rigDischarge=Attack("RigDischarge",126,2.8f,1,3,DamageType.Pulse);
                var weldBead=Attack("WeldBead",130,11,1,3.4f,DamageType.Plasma);
                var wardenBaton=Attack("WardenBaton",128,3,.8f,2.4f,DamageType.Pulse,2.1f);
                var wardenDrive=Attack("WardenShieldDrive",131,7,1,3.6f,DamageType.Kinetic);
                var interceptorRun=Attack("InterceptorRun",130,14,.9f,3.2f,DamageType.Kinetic);
                // The tractor holds a ship still for the pass; it hurts nobody itself.
                var interceptorTractor=Attack("InterceptorTractor",0,15,.9f,4.5f,DamageType.Pulse);

                // ---- Nacre Grave (flight)
                // Tetherer that tows: a rescue drone still hauling toward a berth that isn't there. It holds 12 m off a ship and marks a harpoon line 15 m by 1.2 for .9 s; caught, the ship
                // flies at .45 of its speed and is reeled in at 3.5 m/s while the drone hauls it the way it was set facing (Nacre sets it along the lane inward) at 3 m/s, for at most 3.5 s.
                // Anything that hits the drone cuts the line; a roll snaps it; cover breaks it. Answer: roll the line, or one shot on the drone.
                ArenaBuilder.Asset<EnemyDef>("TowDrone",e=>{e.id="TowDrone";e.displayName="Tow-drone";e.prefab=Body("TowDrone","ClaimDrone");e.integrity=900;e.breakThreshold=200;e.speed=6.5f;e.attacks=new[]{towHarpoon};
                    e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.Tetherer;
                    e.tune=new BehaviourTune{reach=15,pace=.45f,speed=3.5f,linger=3.5f,tow=3};
                    e.xp=40;e.scrip=16;e.scrap=2;e.boss=false;e.motion=EnemyMotion.Hover;e.heavyDeath=true;e.capsuleRadius=.9f;e.capsuleHeight=1.8f;
                    e.lootTable=Loot("TowDrone",Drop("NacreShard",1,.35f),Drop("ScrapAlloy",2,.6f),ProgressionBuilder.AnyPart(5,GearSlot.Drive,.03f));});
                // Anchored on a tether: a piece of the Bridle still made fast to a wreck in the breach. It never turns; every 2.6 s while a ship is within 14 m it thrashes a marked fan of 90
                // degrees out to 9.5 m along its free side (1 s), whoever is where. Its tether side is never struck. Answer: come in along the tether and break it from there (Kinetic).
                ArenaBuilder.Asset<EnemyDef>("BridleFragment",e=>{e.id="BridleFragment";e.displayName="Bridle Fragment";e.prefab=Body("BridleFragment","SnagvineNode");e.integrity=2000;e.breakThreshold=380;e.speed=0;e.attacks=new[]{fragmentLash};
                    e.weakness=DamageType.Kinetic;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.Anchored;e.tune=new BehaviourTune{arc=90,tethered=true};
                    e.xp=120;e.scrip=30;e.scrap=3;e.boss=false;e.heavyDeath=true;e.capsuleRadius=1.6f;e.capsuleHeight=4.4f;
                    e.lootTable=Loot("BridleFragment",Drop("NacreShard",2,.7f),Drop("ScrapAlloy",3,.8f),ProgressionBuilder.AnyPart(5,GearSlot.Frame,.05f));});

                // ---- the Terminator (on foot, the dawn coming on)
                // Ambusher that lunges, of the dawn line: it goes under only in the warm band 1.5 to 14 m ahead of the line and out of shade (Lattice.Combat.Dawn), in a ring 3.6 m round;
                // a hero inside draws it up and down a marked lane 5 m by 1.6 (.8 s); then it is spent in the open 2 s. Spent in shade, or with its hide fallen into shade or behind the line, it
                // runs for the band. Answer: keep to shade, where it won't come; or roll the lunge and hit it spent (Beam). Elsewhere it fights as the Ash Crawler it is graded from.
                ArenaBuilder.Asset<EnemyDef>("HeatScavenger",e=>{e.id="HeatScavenger";e.displayName="Heat-scavenger";e.prefab=Body("HeatScavenger","AshCrawler");e.integrity=1000;e.breakThreshold=220;e.speed=4.6f;e.attacks=new[]{scavengerLunge};
                    e.weakness=DamageType.Beam;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Ambusher;
                    e.tune=new BehaviourTune{wake=3.6f,count=1,speed=6.5f,recover=2,reach=5,width=1.6f};
                    e.xp=45;e.scrip=16;e.scrap=2;e.boss=false;e.capsuleRadius=.7f;e.capsuleHeight=1.4f;
                    e.lootTable=Loot("HeatScavenger",Drop("HaloRivet",1,.35f),Drop("ScrapAlloy",2,.5f),ProgressionBuilder.AnyPart(5,GearSlot.Edge,.02f));});
                // Lobber that hovers: from up to 12 m it spits molten slag 1.2 s through the air onto a marked 2.2 m circle, which burns 2.4 m round for 3.5 s (38 a second); it backs off from
                // a hero who closes, at a walk. Answer: step out of the circle toward it and catch it, or shoot it down (Pulse).
                ArenaBuilder.Asset<EnemyDef>("SlagGull",e=>{e.id="SlagGull";e.displayName="Slag Gull";e.prefab=Body("SlagGull","Sporecaster");e.integrity=1050;e.breakThreshold=220;e.speed=3.4f;e.attacks=new[]{gullSpit};
                    e.weakness=DamageType.Pulse;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.Lobber;
                    e.tune=new BehaviourTune{linger=3.5f,area=2.4f,effect=AreaEffect.Burn,tick=38};
                    e.xp=50;e.scrip=16;e.scrap=2;e.boss=false;e.motion=EnemyMotion.Hover;e.capsuleRadius=.8f;e.capsuleHeight=1.8f;
                    e.lootTable=Loot("SlagGull",Drop("HaloRivet",1,.4f),Drop("ScrapAlloy",2,.5f),Drop("ChargeCell",1,.08f),ProgressionBuilder.AnyPart(5,GearSlot.Emitter,.02f));});

                // ---- the Separation Works
                // Anchored, all round: the Compact's control drone clamped at the end of a machine's cable (the Works draws the cable and frees the machine on clear.<its encounter>). While a
                // hero is within 2.8 m it discharges: a marked ring round it 1 s, then a hit, every 3 s. Answer: strike between discharges, or shoot it from outside the ring (Pulse).
                ArenaBuilder.Asset<EnemyDef>("OverrideRig",e=>{e.id="OverrideRig";e.displayName="Override Rig";e.prefab=Body("OverrideRig","Shellmine");e.integrity=900;e.breakThreshold=200;e.speed=0;e.attacks=new[]{rigDischarge};
                    e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.Anchored;e.tune=new BehaviourTune{arc=360};
                    e.xp=35;e.scrip=18;e.scrap=2;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.6f;e.capsuleHeight=1.4f;
                    e.lootTable=Loot("OverrideRig",Drop("HaloRivet",1,.5f),Drop("ScrapAlloy",2,.6f),ProgressionBuilder.AnyPart(5,GearSlot.Module,.04f));});
                // A rider on its own rail that never moves: the station's welding arm under override. A hero on its strip (11 m along the way it was set facing, 2.4 wide) sees it lit 1 s and
                // the weld runs down it; the seam burns 3 s after (38 a second). At nought the override is cut and the arm stands down at rest where it worked (MachineAtRest). Answer: step off
                // the strip and strike it from beside (Kinetic).
                ArenaBuilder.Asset<EnemyDef>("WeldArm",e=>{e.id="WeldArm";e.displayName="Weld Arm";e.prefab=Body("WeldArm","ClampTender");e.integrity=1400;e.breakThreshold=280;e.speed=0;e.attacks=new[]{weldBead};
                    e.weakness=DamageType.Kinetic;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.Rider;
                    e.tune=new BehaviourTune{reach=11,width=2.4f,wake=3,linger=3,effect=AreaEffect.Burn,tick=38,standsDown=true};
                    e.xp=80;e.scrip=22;e.scrap=3;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.9f;e.capsuleHeight=2.4f;
                    e.lootTable=Loot("WeldArm",Drop("HaloRivet",2,.5f),Drop("ScrapAlloy",3,.7f),ProgressionBuilder.AnyPart(5,GearSlot.Edge,.04f));});
                // Guardian that gives ground: a shield to the front (140 degrees, a fifth through), a stun-baton on a marked circle 2.1 m round (.8 s) and from 3 to 7 m a shield drive down a
                // marked line 7 by 1.8 (1 s), after which the shield is down 1.6 s. Held to 9 m from where it was set. Its guard broken it backs off 3 m and stands open; at nought it stands
                // aside and walks out (the lead's "warden"). Answer: get round the shield, or break its guard and punish it (Plasma).
                ArenaBuilder.Asset<EnemyDef>("CompactWarden",e=>{e.id="CompactWarden";e.displayName="Compact Warden";e.prefab=Body("CompactWarden","MusterPikeguard");e.integrity=1700;e.breakThreshold=300;e.speed=2.6f;e.attacks=new[]{wardenBaton,wardenDrive};
                    e.weakness=DamageType.Plasma;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.Guardian;
                    e.tune=new BehaviourTune{arc=140,guard=.2f,reach=7,width=1.8f,recover=1.6f,wake=9,speed=80,givesGround=true,bark="warden"};
                    e.xp=80;e.scrip=24;e.scrap=3;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.6f;e.capsuleHeight=1.95f;
                    e.lootTable=Loot("CompactWarden",Drop("HaloRivet",2,.5f),Drop("ScrapAlloy",3,.8f),Drop("RepairGel",1,.08f),ProgressionBuilder.AnyPart(5,GearSlot.Frame,.04f));});

                // ---- the Broken Ring (flight)
                // Strafers in a pair: one pins a ship with a tractor line (15 m by 1.2, .9 s; .3 of its speed for up to 3.5 s), the other circles at 14 m and runs a marked lane 22 m by 2.6 at
                // 24 m/s through whoever is pinned. The line breaks on a roll, a hit on the pinner, or cover. Answer: break the line or shoot the one holding it, then cross out of the lane (Beam).
                ArenaBuilder.Asset<EnemyDef>("Interceptor",e=>{e.id="Interceptor";e.displayName="Interceptor";e.prefab=Body("Interceptor","MusterLancer");e.integrity=1050;e.breakThreshold=240;e.speed=12;e.attacks=new[]{interceptorRun,interceptorTractor};
                    e.weakness=DamageType.Beam;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Strafer;
                    e.tune=new BehaviourTune{reach=22,width=2.6f,speed=24,recover=1.4f,pair=true};
                    e.xp=60;e.scrip=18;e.scrap=2;e.boss=false;e.motion=EnemyMotion.Hover;e.heavyDeath=true;e.capsuleRadius=1;e.capsuleHeight=2.2f;
                    e.lootTable=Loot("Interceptor",Drop("NacreShard",1,.4f),Drop("ScrapAlloy",2,.6f),ProgressionBuilder.AnyPart(5,GearSlot.Drive,.03f));});
            }
        }
    }
}
