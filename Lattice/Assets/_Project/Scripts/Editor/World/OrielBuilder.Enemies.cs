using Lattice.Data;
using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 7's enemies (brief, Enemies; manifest section 5 for every id and name): seven ordinary kinds, each one family of the behaviour library (Lattice.Combat.EnemyBehaviour;
    /// CHAPTER_GUIDE section 5, "Enemy behaviours"), each with one tell and one answer unlike its neighbours'; and the Last Warden, whose moves are BossController.LastWarden.cs. Every
    /// number a fight is tuned by is here, and only here. <c>OrielBuilder.Define</c> calls <c>DefineEnemies</c> after the zones and before the quests.
    /// <para>The chapter's rule is that force follows lines, so most of its fights are about a line: the Rail Warden fires down its rail and the Fibre Wisp down the fibre it rides (step off
    /// a lit line), the Moonshard runs a marked track ahead of its moon (keep out of the track), the Glassback charges a lane and leaves its plates where it stops (sidestep, and don't step
    /// back), and the Last Warden plucks, strums and draws the engine hall's fibres. Its neighbours answer differently: the Pruner's spinning cut is a ring with a hole in its middle (in under
    /// the blades, or out), the Glass Sentinel throws a shot that lands on its glass front back down a marked line (get round it), and the Clamp Tender closes what a hero is opening (stop it
    /// first: it is fragile).</para>
    /// <para>Sizing. The party arrives at Sync 19 in Tempered parts (DevLoadout chapter7: the plain Tempered frame, Plating 38) and leaves at 22. A hero's bar is 120 + 18 per Sync past the
    /// first: 444 at 19, 462 at 20, 480 at 21, 498 at 22. Taren's and Sela's Plating is 10 + 2 per Sync past the first + the frame's (84 at 19, 90 at 22); Idra's, the guard hero's, 16 + 3 per
    /// Sync past the first + the frame's (108 at 19, 117 at 22), and a hit lands at 100/(100 + Plating - 10) of itself. So at a zone threat of 1 an authored hit of D costs Taren or Sela at 19
    /// D x .5747 / 444 of the bar (the most anyone pays: 13% allows D = 100) and Idra at 22 D x .4831 / 498 (the least: 9% needs D = 93). Every ordinary attack here is authored between 96
    /// and 100 (Taren at 19: 12.4 to 12.9%; Idra at 22: 9.3 to 9.7%), and every zone of the chapter stays at threat 1. In the Prime frame chapter 6 sold, Taren at 19 takes 13.4 to 13.9%:
    /// the band is held in the parts the chapter arrives in. The Last Warden is met at Sync 20 to 21: its moves are 120 to 128, 12.2% (Idra at 21, 120) to 15.7% (Taren at 20, 128) of the
    /// bar, all more than anything on the road (100), in both halves: its second half adds nothing to a blow (BossController.WardenTune.SlackBonus; the shared +18% took the snap to 18.6%).</para>
    /// <para>XP. Sync 19 to 22 costs 2,375 + 2,500 + 2,625 = 7,500 a hero. The main quest pays 1,800 and the three side quests 920 (OrielBuilder.Quests). The kills are sized to the
    /// encounters as the zones' briefs plan them (docs/maps/Orrery.md, OrielSkyfields.md, GlassChoir.md). On the main path: the Skyfields 1,385 (2 Glassbacks 500, 5 wisps 425, 2 Pruners
    /// at the door 460), the Glass Choir 1,350 (3 Glass Sentinels 960, 2 wisps 170, 2 tenders 220) and the Warden 1,720 (1,500 and the two tenders its shift calls out, 220): 4,455. The
    /// Orrery pays almost nothing to a party that keeps the machine's time, which is its rule working (a shard shot down 20, a forced rail's wardens 90 each). So a party meets the Warden
    /// at Sync 20.1, or 20.5 with the side quests done; the main path with the main quest ends the chapter at 21.5, the side quests take it to 21.9, and the optional pockets (the grove's
    /// Glassback, the wrecked garden's two Pruners, three shards, a forced rail) to 22.2.</para>
    /// <para>Drops. Oriel's commons are Fibre Thread and Singing Glass; its rare, the Warden Key, only the Warden drops. Tempered parts come out of the Choir and the Skyfields at a few
    /// percent a body, beside Lume's counter.</para>
    /// </summary>
    public static partial class OrielBuilder
    {
        static void DefineEnemies()
        {
            GameObject Body(string id,string fallback)=>ZoneKit.Body("Enemies",id,"Enemies",fallback);
            LootTable Loot(string id,params LootEntry[] entries)=>ArenaBuilder.Asset<LootTable>(id,t=>t.entries=entries);
            LootEntry Drop(string id,int count,float chance)=>ProgressionBuilder.Drop(id,count,chance);
            AttackDef Attack(string id,float damage,float range,float telegraph,float cooldown,DamageType type,float radius=1,bool projectile=false)=>
                ArenaBuilder.Asset<AttackDef>(id,a=>{a.damage=damage;a.range=range;a.telegraph=telegraph;a.cooldown=cooldown;a.type=type;a.radius=radius;a.projectile=projectile;});

            // ---- the attacks, at a zone threat of 1. Range is where the move starts from (a charge's run-in, a spinner's reach to open its shears), not always how far it reaches:
            // that is the behaviour's own tune.reach.
            var railBolt=Attack("RailBolt",96,16,.9f,3,DamageType.Beam,1,true);
            var shardRun=Attack("ShardRun",98,30,1.1f,1,DamageType.Kinetic);
            var shearsSpin=Attack("ShearsSpin",98,3.6f,.85f,2.6f,DamageType.Kinetic,4.4f);
            var wispShock=Attack("WispShock",96,10,.75f,2.4f,DamageType.Pulse);
            var glassCharge=Attack("GlassCharge",100,9,.9f,3.2f,DamageType.Kinetic);
            var sentinelBash=Attack("SentinelBash",98,3.2f,.8f,2.4f,DamageType.Kinetic,2.2f);
            var halberdSweep=Attack("HalberdSweep",100,11,1.1f,4,DamageType.Kinetic);
            var glassThrow=Attack("GlassThrow",96,13,.55f,1.6f,DamageType.Beam);
            var tenderTorch=Attack("TenderTorch",96,3,.7f,2.6f,DamageType.Plasma);
            var wardenStaff=Attack("WardenStaff",120,3.6f,.8f,2.4f,DamageType.Kinetic,2.4f);

            // ---- the Orrery (flight)
            // Rider on a rail: it glides its rail (laid by the Orrery, or with none its own, ahead of it along the way it was set facing) and fires only down it: a ship on the rail ahead of it,
            // within 16 m, sees the rail marked 3 m wide for .9 s and a bolt come down it. Asleep on show until the lead forces one of its rail's gates. Answer: get off the rail, cross it, don't
            // fly along it; or hit it from the side, where nothing of it reaches.
            ArenaBuilder.Asset<EnemyDef>("RailWarden",e=>{e.id="RailWarden";e.displayName="Rail Warden";e.prefab=Body("RailWarden","ChoristerDrifter");e.integrity=950;e.breakThreshold=230;e.speed=5;e.attacks=new[]{railBolt};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Spitter;e.behaviour=EnemyBehaviourKind.Rider;
                e.tune=new BehaviourTune{reach=16,width=3,wake=20};
                e.xp=90;e.scrip=16;e.scrap=2;e.boss=false;e.motion=EnemyMotion.Hover;e.heavyDeath=true;e.capsuleRadius=1.2f;e.capsuleHeight=2.4f;
                e.lootTable=Loot("RailWarden",Drop("FibreThread",1,.4f),Drop("ScrapAlloy",2,.6f),Drop("ChargeCell",1,.1f),ProgressionBuilder.AnyPart(4,GearSlot.Drive,.04f));});
            // Charger on a track: calved off the Clerk of Roads' leading face, it runs where it was set facing (the moon's way along its rail) once, down a lane marked 30 m long and 3.4 m wide
            // for 1.1 s, at 20 m/s, and is spent at the end of it: it breaks up and pays nothing. Shot down first (180: a few shots) it is a kill. Answer: keep out of the marked track ahead
            // of a moon, and its lee is calm.
            ArenaBuilder.Asset<EnemyDef>("Moonshard",e=>{e.id="Moonshard";e.displayName="Moonshard";e.prefab=Body("Moonshard","Shellmine");e.integrity=180;e.breakThreshold=100000;e.speed=1;e.attacks=new[]{shardRun};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.Charger;
                e.tune=new BehaviourTune{reach=30,width=3.4f,speed=20,recover=.6f,count=1};
                e.xp=20;e.scrip=3;e.scrap=0;e.boss=false;e.motion=EnemyMotion.Hover;e.heavyDeath=true;e.capsuleRadius=1.2f;e.capsuleHeight=2.4f;
                e.lootTable=Loot("Moonshard",Drop("SingingGlass",1,.25f));});

            // ---- the Skyfields
            // Spinner: a gardener set to prune. Inside 3.6 m it opens its shears and turns once: a ring from 1.7 m to 4.4 m round it, marked .85 s; then it is giddy, open 1.6 s. It keeps to
            // its garden, 12 m round where it was set. Hostile only near a wrecked garden or on the Warden's order (the Skyfields' encounters). Answer: in under the blades against its body,
            // or out past them, and strike it while it reels.
            ArenaBuilder.Asset<EnemyDef>("Pruner",e=>{e.id="Pruner";e.displayName="Pruner";e.prefab=Body("Pruner","ThawedGuard");e.integrity=1150;e.breakThreshold=260;e.speed=2.8f;e.attacks=new[]{shearsSpin};
                e.weakness=DamageType.Kinetic;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Spinner;
                e.tune=new BehaviourTune{width=1.7f,recover=1.6f,wake=12};
                e.xp=230;e.scrip=26;e.scrap=2;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.55f;e.capsuleHeight=2.2f;
                e.lootTable=Loot("Pruner",Drop("FibreThread",2,.6f),Drop("SingingGlass",1,.3f),ProgressionBuilder.AnyPart(4,GearSlot.Edge,.04f));});
            // Rider that hops: a mote of the old road (its body is WispBody, below). It rides the fibre nearest the hero (the zone's, laid on the floor under each fibre, or with none its own
            // through him) and when he stands on it within 10 m the fibre lights 1.6 m wide for .75 s and the shock runs down it. Then it hops to the fibre he is on, open for 1.1 s in the air;
            // with him off its fibre a while it hops too. Answer: off the lit fibre, and hit it as it hops.
            ArenaBuilder.Asset<EnemyDef>("FibreWisp",e=>{e.id="FibreWisp";e.displayName="Fibre Wisp";e.prefab=WispBody();e.integrity=320;e.breakThreshold=70;e.speed=3.5f;e.attacks=new[]{wispShock};
                e.weakness=DamageType.Pulse;e.resistance=DamageType.Plasma;e.archetype=EnemyArchetype.Swarm;e.behaviour=EnemyBehaviourKind.Rider;
                e.tune=new BehaviourTune{reach=10,width=1.6f,recover=1.1f,count=10,wake=16,tint=new Color(.95f,1.15f,1.4f,1),bark="wisp"};
                e.xp=85;e.scrip=8;e.scrap=0;e.boss=false;e.motion=EnemyMotion.Hover;e.capsuleRadius=.55f;e.capsuleHeight=1.2f;
                e.lootTable=Loot("FibreWisp",Drop("FibreThread",1,.35f));});
            // Charger: a grazer grown glass plates. Inside 9 m it squares up, marks a 12 m lane 2.8 m wide for .9 s and runs it at 15 m/s; in a wall it is stuck and open 2.2 s, a clean miss winds
            // it for 1 s. Where it stops it sheds its plates: caltrops in a 2.6 m ring for 5 s, a danger that cuts 30 a second. Answer: sidestep with a rim behind you, and don't step back into
            // the glass.
            ArenaBuilder.Asset<EnemyDef>("Glassback",e=>{e.id="Glassback";e.displayName="Glassback";e.prefab=Body("Glassback","Thornback");e.integrity=1600;e.breakThreshold=320;e.speed=3;e.attacks=new[]{glassCharge};
                e.weakness=DamageType.Kinetic;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.Charger;
                e.tune=new BehaviourTune{reach=12,width=2.8f,speed=15,recover=2.2f,linger=5,area=2.6f,effect=AreaEffect.Burn,tick=30,bark="glassback"};
                e.xp=250;e.scrip=30;e.scrap=2;e.boss=false;e.capsuleRadius=.9f;e.capsuleHeight=2;
                e.lootTable=Loot("Glassback",Drop("SingingGlass",2,.7f),Drop("FibreThread",1,.3f),Drop("RepairGel",1,.06f),ProgressionBuilder.AnyPart(4,GearSlot.Frame,.05f));});

            // ---- the Glass Choir
            // Guardian with a glass front and a swept halberd: its front 130 degrees lets a fifth of a blow through, and a Beam shot that lands there is gathered and thrown back at whoever
            // fired it down a marked line (.55 s, then a bolt). Close, a bash on a marked 2.2 m circle; further, its halberd: a 110-degree fan out to 10 m marked 1.1 s, a blade of light crossing
            // it in .6 s; then it is down 1.6 s. It holds a post 10 m round and turns at 70 a second. Answer: get round it, a shot or a blade; or behind it as the fan fills, or roll the blade.
            ArenaBuilder.Asset<EnemyDef>("GlassSentinel",e=>{e.id="GlassSentinel";e.displayName="Glass Sentinel";e.prefab=Body("GlassSentinel","ThawedGuard");e.integrity=1650;e.breakThreshold=330;e.speed=2;e.attacks=new[]{sentinelBash,halberdSweep,glassThrow};
                e.weakness=DamageType.Kinetic;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.Guardian;
                e.tune=new BehaviourTune{arc=130,guard=.2f,reach=10,recover=1.6f,wake=10,speed=70,sweep=110,reflects=true,tint=new Color(.8f,1.1f,1.5f,1),bark="sentinel"};
                e.xp=320;e.scrip=36;e.scrap=3;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.75f;e.capsuleHeight=2.4f;
                e.lootTable=Loot("GlassSentinel",Drop("SingingGlass",2,.6f),Drop("ScrapAlloy",3,.8f),ProgressionBuilder.AnyPart(4,.07f));});
            // Tender: the court's maintenance construct, half a person tall. It walks to whatever it keeps (a clamp a hero can work, a stay gone slack: Lattice.Combat.Tended) within 24 m
            // and works it, heeding nobody: a clamp it is at loses what a hero has taken of its weight and never eases. With nothing to keep, a welding torch on a marked 70-degree fan
            // inside 3 m (.7 s), and it keeps a step off. Fragile (420). Answer: stop it first.
            ArenaBuilder.Asset<EnemyDef>("ClampTender",e=>{e.id="ClampTender";e.displayName="Clamp Tender";e.prefab=Body("ClampTender","Scrapmite");e.integrity=420;e.breakThreshold=90;e.speed=3.2f;e.attacks=new[]{tenderTorch};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.PackHunter;e.behaviour=EnemyBehaviourKind.Tender;
                e.tune=new BehaviourTune{arc=70,wake=24,bark="tender"};
                e.xp=110;e.scrip=14;e.scrap=2;e.boss=false;e.heavyDeath=true;e.capsuleRadius=.5f;e.capsuleHeight=1.1f;
                e.lootTable=Loot("ClampTender",Drop("ScrapAlloy",2,.6f),Drop("FibreThread",1,.3f),Drop("ChargeCell",1,.08f));});

            // ---- the Last Warden. Its bar is its hold on the road, and it is sized on the party's measured rate (OrielCombatTests pins this literal to the asset and holds the fight to its
            // time for every pair a player can put out): two to five minutes for a Sync 20 party in the chapter's arrival parts (Tempered), four quarters of the bar fought down, each held at its
            // line until a clamp is opened over it. Two measures, both November 12, Sync 20, Tempered. QaPlanCombatTests' BOSS_RATE (its FlatOut stands the second hero 8 m off, as a
            // gunner stands, so a melee Idra hardly lands) ran Taren and Idra 1,665 a second: 116,000 is 232 s of the bar at the driver's share and 279 s at a quarter of flat out, under
            // its 300 s ceiling. OrielCombatTests' own measure, each hero at their reach, ran Taren and Idra 3,017, Taren and Sela 2,422, Sela and Idra 2,859 a second: 128, 160 and 135 s
            // of the bar at the driver's share, each over the shared 120 s floor, and about 40 s more for the four clamps, 168, 200 and 175 s, about three minutes for every pair.
            // The first figure, 150,000, ran 352 s at a quarter by the shared measure; the second, 78,000, came under the floor by the reach measure (86 s for Taren and Idra).
            // QaPlanCombatTests measures it in the same parts. Its numbers past this are BossController.LastWarden.cs's own table. It stands down at the
            // fourth clamp (it yields: its body stays, staff lowered), and drops the region's rare, the Warden Key.
            // 245,000 since D384 (November 24), from 200,000: the player fought 200,000 down in 130 to 171 s, its bar alone about 105 s of that, under the shared 120 s floor.
            ArenaBuilder.Asset<EnemyDef>("LastWarden",e=>{e.id="LastWarden";e.displayName="THE LAST WARDEN";e.prefab=Body("LastWarden","HostMarshal");e.integrity=260000;e.breakThreshold=800;e.speed=1.6f;e.attacks=new[]{wardenStaff};
                e.weakness=DamageType.Plasma;e.resistance=DamageType.Pulse;e.archetype=EnemyArchetype.Sentinel;e.behaviour=EnemyBehaviourKind.None;e.xp=1500;e.scrip=450;e.scrap=22;e.boss=true;
                e.bossMusicId="Boss of the Ages";e.yieldsOnDefeat=true;
                // A capsule can't stand shorter than it is wide: 3.6 m on its 1.5 m radius, the body's own height.
                e.capsuleHeight=3.6f;
                e.lootTable=Loot("LastWarden",Drop("WardenKey",1,1),Drop("FibreThread",6,1),Drop("SingingGlass",6,1),Drop("RepairSalve",2,1),ProgressionBuilder.AnyPart(4,1));});
        }

        /// <summary>The Fibre Wisp's body: a mote of the old road, a knot of its light held in a cage of dark glass. It was ten motes of the shared flare, each a fifth of a metre whose bright
        /// core is a few centimetres, so it drew as two or three pixels apiece in the editor and the player alike, and on the Skyfields' pale stone a player saw the lock's card over
        /// empty floor (November 20, s39/153). Now, at chest height and the size of a hero's head and shoulders: a white-blue core that blooms, three rings of near-black violet glass
        /// turning round it on three axes (dark against pale stone, their rims lit against a dark deck), four threads of its light trailing toward the fibre it rides, and a cold
        /// light of its own that pools on the floor under it. Its tell is the Rider's: the fibre lights, and the whole body swells by a quarter as the shock gathers (EnemyTellPose).
        /// Built here as a prefab, meshes and all, every time the definitions are.</summary>
        static GameObject WispBody()
        {
            const string Prefab="Assets/_Project/Prefabs/Enemies/FibreWisp.prefab",Meshes="Assets/_Project/Art/Generated/Oriel/";
            System.IO.Directory.CreateDirectory(Meshes);
            const float Middle=1.02f;
            var core=ZoneKit.Toon("wisp-core",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(1.9f,0,0,0));m.SetFloat("_ReceiveShadows",0);});
            var glass=ZoneKit.Toon("wisp-cage",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",3);m.SetColor("_ShadowTint",new Color(.42f,.4f,.62f));
                m.SetColor("_RimColor",new Color(.62f,.92f,1.35f));m.SetFloat("_RimPower",1.8f);m.SetFloat("_RimUnlit",.85f);m.SetFloat("_LightFloor",.18f);m.SetColor("_EmissionColor",new Color(.05f,.06f,.13f));});
            var root=new GameObject("FibreWisp");
            try
            {
                GameObject Part(string name,ZoneKit.Shape shape,Material material,Vector3 at,Quaternion turn,bool shadows)
                {
                    var mesh=ZoneKit.SaveMesh(shape.Mesh("FibreWisp "+name),Meshes+"FibreWisp-"+name.Replace(' ','-')+".asset");
                    var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);go.transform.SetLocalPositionAndRotation(at,turn);
                    go.GetComponent<MeshFilter>().sharedMesh=mesh;var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=material;
                    view.shadowCastingMode=shadows?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;view.receiveShadows=false;return go;
                }
                // The core: a cut knot of light, white at its heart and blue at its facets, and a softer shell of the same round it that the bloom takes.
                var heart=new ZoneKit.Shape();
                heart.Slot(0).Lump(Vector3.zero,new Vector3(.24f,.3f,.24f),5,.28f,new Color(1.25f,1.3f,1.4f),new Color(.55f,.8f,1.25f),6,4);
                heart.Slot(0).Lump(Vector3.zero,new Vector3(.13f,.16f,.13f),9,.2f,new Color(1.6f,1.6f,1.6f),new Color(1.3f,1.4f,1.6f),6,4);
                Part("Core",heart,core,Vector3.up*Middle,Quaternion.identity,false);
                // The cage: three rings of dark glass, each on its own axis and turning at its own rate, so it reads as alive and round from the fixed camera.
                var tones=new[]{new Color(.2f,.16f,.36f),new Color(.16f,.14f,.32f),new Color(.24f,.18f,.4f)};
                var axes=new[]{(Quaternion.Euler(72,0,18),Vector3.up,70f),(Quaternion.Euler(-24,0,66),Vector3.up,-95f),(Quaternion.Euler(10,64,-40),Vector3.up,120f)};
                for(int k=0;k<3;k++)
                {
                    float r=.5f+.07f*k;var loop=new System.Collections.Generic.List<Vector3>();var thick=new System.Collections.Generic.List<float>();
                    for(int i=0;i<=40;i++){float a=i*Mathf.PI*2/40;loop.Add(new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));thick.Add(.05f+.018f*Mathf.Sin(a*3+k));}
                    var ring=new ZoneKit.Shape();ring.Slot(0).Tube(loop,thick,7,tones[k],ZoneKit.Shape.Dim(tones[k],1.25f),1,.5f,false);
                    // Beads of its light set in the glass, three to a ring.
                    for(int b=0;b<3;b++){float a=b*Mathf.PI*2/3+k;ring.Slot(1).Lump(new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r),Vector3.one*.075f,b+k*3,.15f,new Color(1.1f,1.3f,1.6f),new Color(.6f,.85f,1.3f),5,3);}
                    var (turn,axis,rate)=axes[k];var drawn=Part("Cage "+(k+1),ring,glass,Vector3.up*Middle,turn,true);drawn.GetComponent<MeshRenderer>().sharedMaterials=new[]{glass,core};
                    var spin=drawn.AddComponent<Lattice.World.Turning>();spin.axis=axis;spin.degrees=rate;
                }
                // Threads of its light trailing down toward the fibre, swept a little back, thinning to nothing a hand over the floor.
                var threads=new ZoneKit.Shape();
                for(int t=0;t<4;t++)
                {
                    float a=t*Mathf.PI*.5f+.4f;var off=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var path=new Vector3[6];var radii=new float[6];
                    for(int i=0;i<6;i++){float s=i/5f;path[i]=off*(.12f+.16f*s+.06f*Mathf.Sin(s*3+t))+Vector3.up*Mathf.Lerp(-.18f,-.84f,s)+Vector3.back*(.1f*s*s);radii[i]=Mathf.Lerp(.028f,.004f,s);}
                    threads.Slot(0).Tube(path,radii,5,new Color(.95f,1.1f,1.35f),new Color(.5f,.75f,1.1f),1,1);
                }
                Part("Threads",threads,core,Vector3.up*Middle,Quaternion.identity,false);
                // Its own cold light, pooled on the floor under it: what finds it on the Choir's dim deck.
                var glow=new GameObject("Wisp light",typeof(Light));glow.transform.SetParent(root.transform,false);glow.transform.localPosition=Vector3.up*(Middle+.1f);
                var light=glow.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(.62f,.84f,1);light.range=3.6f;light.intensity=2.4f;light.shadows=LightShadows.None;
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }
            finally{Object.DestroyImmediate(root);}
            return AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        }
    }
}
