using System;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 4, Breathing Room (brief: docs/chapters/ch04-breathing-room.md; script wiring: docs/writing/ch04-manifest.md). Isolated builder for its definitions and three zones,
    /// on <see cref="ZoneKit"/>:
    /// <list type="bullet">
    /// <item>The Spore Veil (space combat, <c>WendmireBuilder.SporeVeil.cs</c>): a spore nebula over Wendmire where light draws swarms; three legs between Driftreed's pulse beacons,
    /// one beacon dark under a Veil Ray's circuit, and the tide-gate down to the town.</item>
    /// <item>Driftreed (town, <c>WendmireBuilder.Driftreed.cs</c>): woven-reed rafts round a stilt market, the weaving hall at the back, a stern line to hold before anyone talks.</item>
    /// <item>The Drowned Waystation (wild approach and dungeon, <c>WendmireBuilder.Waystation.cs</c>): the second world engineer's.</item>
    /// </list>
    /// Enemies and the Rotbloom are in <c>WendmireBuilder.Enemies.cs</c> (the combat engineer's). Kit pieces from batch C11-Wendmire (tools/c11_intake.py) are used when staged;
    /// until then each falls back to something built here or an existing piece.
    /// Runs: <c>Definitions</c>, <c>BuildZones</c> with <c>-wendmire-zones SporeVeil,Driftreed,DrownedWaystation</c>, <c>Dialogue</c>, <c>StageApproach</c>; then <c>ZoneLook.Apply</c> for what was built.
    /// </summary>
    public static partial class WendmireBuilder
    {
        public static readonly string[] Zones={"SporeVeil","Driftreed","DrownedWaystation"};
        public static void Definitions()=>BatchTools.Run(()=>{Define();AssetDatabase.SaveAssets();Debug.Log("WENDMIRE_DEFS_OK");});
        public static void BuildZones()=>BatchTools.Run(()=>
        {
            string arg=DevArgs.Value("-wendmire-zones");var zones=string.IsNullOrEmpty(arg)?Zones:arg.Split(',',StringSplitOptions.RemoveEmptyEntries);
            Define();foreach(var zone in zones)BuildZone(zone.Trim());
            BatchTools.RegisterScenes();AssetDatabase.SaveAssets();Debug.Log("WENDMIRE_ZONES_OK "+string.Join(",",zones));
        });
        [Restage.Step("chapter4.tallow-warp","TallowApproach",After="chapter3.tallow-warp",Chapter=4,Leaves=new[]{"TallowApproach: Warp — The Spore Veil <WarpBeacon> #1","TallowApproach: Spawn_Wendmire #1"})]
        public static void StageApproach()=>BatchTools.Run(()=>{StageTallowApproach();AssetDatabase.SaveAssets();Debug.Log("WENDMIRE_APPROACH_OK");});
        /// <summary>Copies docs/writing/*.yarn into Resources and recompiles the Yarn project: a new .yarn file is not in the compiled program until the project reimports, and a missing
        /// node fails silently at runtime. From the moment Wendmire.yarn is in, the dialogue tests want every speaker's portrait set and every quest it starts (manifest section 8).</summary>
        public static void Dialogue()=>BatchTools.Run(()=>{RefreshDialogue();Debug.Log("WENDMIRE_DIALOGUE_OK");});
        /// <summary>Every node of the chapter's script (manifest section 2): one that didn't compile stops the build here instead of doing nothing when talked to.</summary>
        static readonly string[] Nodes=
        {
            "AbriReedRoad","AbriReedRoadRepeat","AbriReedRoadCharted","AbriReedRoadWaiting","PellVeilLantern","PellVeilRepeat","AbriManyHandsCall","AbriManyHands","PellManyHands",
            "MaruVeilHail","MaruVeilStray","MaruBeaconsAsk","MaruDeadBeacon",
            "MaruArrival","MaruLineHold","OnaLineHold","MaruFirst","MaruRepeat","OnaTerms","OnaRepeat","OnaPimBack","OnaDecides","OnaAfter","MaruTide","MaruOnaWaits","MaruComes",
            "MaruBeaconsOffer","MaruBeaconsReturn","MaruPost","DovFirst","DovRepeat","DovOrderOffer","DovOrderReturn","DovBellOffer","DovBellReturn","DovPost",
            "QuillFirst","QuillRepeat","QuillPumpOffer","QuillPumpReturn","QuillPost","PimHome","SelaWendNotice","SelaSchoolRaft",
            "MaruMireGate","MaruTideCord","MaruMireRepeat","MaruLureLesson","SelaBellBuoy","MaruPortico","SelaWaystationHall","SelaPumpOne","PimFound","PimPortico",
            "SelaVaultMark","SelaBloomDoor","SelaBloomDead","MaruAfterBloom",
        };
        static void RefreshDialogue()
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            foreach(var file in Directory.GetFiles(Path.Combine(root,"docs/writing"),"*.yarn"))File.Copy(file,"Assets/_Project/Resources/Dialogue/"+Path.GetFileName(file),true);
            File.Copy(Path.Combine(root,"docs/writing/barks.json"),"Assets/_Project/Resources/Audio/barks.json",true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset("Assets/_Project/Resources/Dialogue/Lattice.yarnproject",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            var yarn=AssetDatabase.LoadAssetAtPath<Yarn.Unity.YarnProject>("Assets/_Project/Resources/Dialogue/Lattice.yarnproject");
            foreach(var node in Nodes)if(yarn==null||!yarn.Program.Nodes.ContainsKey(node))throw new InvalidOperationException("Yarn project did not compile "+node);
            Debug.Log("YARN_NODES "+yarn.Program.Nodes.Count);
        }
        static void BuildZone(string zone)
        {
            switch(zone)
            {
                case "SporeVeil":SporeVeil();break;
                case "Driftreed":Driftreed();break;
                case "DrownedWaystation":BuildWaystation();break;
                default:throw new InvalidOperationException("Unknown Wendmire zone "+zone);
            }
        }

        // ---------------------------------------------------------------- definitions
        static T Find<T>(string id)where T:ScriptableObject=>ZoneKit.Find<T>(id);
        static Objective Step(ObjectiveKind kind,string target,int count=1)=>new(){kind=kind,target=target,count=count};
        static QuestTarget At(string scene,string speaker=null,string report=null,string encounter=null,string brief=null,params string[] via)=>new(){scene=scene,speaker=speaker,report=report,encounter=encounter,brief=brief,via=via};
        /// <summary>The first of these bodies that has been taken in.</summary>
        internal static GameObject FirstBody(params string[] ids)=>ids.Select(id=>ZoneKit.Prefab("Characters",id)).FirstOrDefault(body=>body!=null);

        internal const string Veil="SporeVeil",Town="Driftreed",Station="DrownedWaystation";
        static void Define()
        {
            Materials();
            var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");var cave=Find<Hd2dProfile>("Cave")??Find<Hd2dProfile>("Cold");
            // Wendmire at dusk: soft violet light off three moons, a little more bloom than Xylos for lamps and glow-spots, and no tilt (D060).
            var mire=ArenaBuilder.Asset<Hd2dProfile>("Mire",p=>{p.tint=new Color(.97f,.96f,1);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.6f;});
            // The Veil: the nebula is lit from inside, so the spores and the beacons are what blooms.
            var spore=ArenaBuilder.Asset<Hd2dProfile>("Veil",p=>{p.tint=new Color(.96f,1,.98f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.7f;});
            var source=ZoneKit.WorldMaterial("tallow-sky");
            // The panorama turned to a part nobody has flown and cooled toward green-violet: the Veil is neither the Wick's amber, the Halo's blue nor Xylos's gold.
            var space=ZoneKit.Sky("wendmire-veil-sky",source,new Color(.42f,.5f,.6f,.5f),137,.75f);
            // Over the mire the sky is only the frame's far edge: dusk lilac.
            var dusk=ZoneKit.Sky("wendmire-dusk-sky",source,new Color(.66f,.6f,.76f,.5f),300,1.7f);
            // Enemy damage is authored at its real weight (WendmireBuilder.Enemies.cs), so every zone's threat is 1 until the combat engineer says otherwise.
            ArenaBuilder.Asset<ZoneDef>(Veil,z=>{z.id=z.scene=Veil;z.displayName="The Spore Veil";z.chapter=4;z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=spore;z.skybox=space;
                z.enemyThreat=1;z.flightPlane=1;z.cameraClearsScenery=false;z.musicId="Starfield Frontier";z.fightMusicId="Starfight";
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(VeilBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=VeilLevel;}});
            ArenaBuilder.Asset<ZoneDef>(Town,z=>{z.id=z.scene=Town;z.displayName="Driftreed";z.chapter=4;z.kind=ZoneKind.GroundSafe;z.cameraProfile=ground;z.hd2dProfile=mire;z.skybox=dusk;
                z.enemyThreat=1;z.cameraClearsScenery=true;z.musicId="Despano Lopo";z.shopMusicId="Shop Theme";z.brightFloor=false;
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(TownBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=TownLevel;}});
            ArenaBuilder.Asset<ZoneDef>(Station,z=>{z.id=z.scene=Station;z.displayName="The Drowned Waystation";z.chapter=4;z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=mire;z.skybox=dusk;
                z.enemyThreat=1;z.cameraClearsScenery=true;z.musicId="Swamp Sprint";
                // The mire's own bed once it is made (it replaces the Braided River's, which stood in); until then whatever the waystation's builder set.
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(MireBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=MireLevel;}});
            _=cave;

            DefineEnemies();
            Quests();

            // Driftreed on Sela's chart (guide 3.10): charted by arriving, joined to Tallow (the warp off its approach), in the chart's middle column below Rootmarket.
            ArenaBuilder.Asset<ChartStop>(Town,s=>{s.id=Town;s.displayName="Driftreed";s.region="Wendmire";s.blurb="The raft village on the mire: Dov's smokehouse, Quill's pump raft and press, and the weaving hall.";
                s.zone=Town;s.spawn="Arrival";s.position=new Vector2(.52f,.14f);s.routes=new[]{"TallowDrift"};s.shop=s.bench=s.repair=true;s.chapter=4;
                s.reaches=new[]{Veil,Station};});

            // Everyone the script gives a line to (manifest section 5). The Ilu are one mesh in five coats: Maru's, and four graded to each portrait sheet.
            void Person(string id,string display,bool female,string standIn,params string[] bodies)
                =>ArenaBuilder.Asset<CharacterDef>(id,c=>{c.id=c.speakerId=id;c.displayName=display;c.female=female;c.natural=FirstBody(bodies)??ZoneKit.Prefab("Characters",standIn);
                    // The sheets were taken in before these definitions existed, so PortraitIntake had nothing to bind them to.
                    if(c.portraitNatural==null)c.portraitNatural=AssetDatabase.LoadAssetAtPath<PortraitSet>("Assets/_Project/Resources/Portraits/"+id+"_Natural.asset");});
            Person("Maru","Maru Wend",true,"Neve","MaruBody");
            Person("Ona","Ona Sedge",true,"Mira","OnaBody","MaruBody");
            Person("Dov","Dov Hale",false,"Hal","DovBody","MaruBody");
            Person("Quill","Quill",false,"Orrin","QuillBody","MaruBody");
            Person("Pim","Pim",false,"Neve","PimBody","MaruBody");

            // Two counters in Driftreed (manifest 8.3), opened by who is talking (ShopUi.For): Dov's provisions, three of them on his counter once his order is filled, and Quill's parts.
            ProgressionBuilder.Stall("Driftreed_Dov","THE SMOKEHOUSE",Sell("RepairGel",24),Sell("RepairSalve",60),Sell("ChargeCell",28),Sell("ChargeBank",60),Sell("SyncTonic",90),Sell("RelinkSpike",90),
                Sell("SurgeDraught",50,0,"dov.orderFilled"),Sell("PlatingWax",50,0,"dov.orderFilled"),Sell("Quickening",50,0,"dov.orderFilled"));
            ProgressionBuilder.Stall("Driftreed_Quill","QUILL'S PUMP RAFT",Sell("EmittersT3",560),Sell("EdgesT3",600),Sell("FramesT3",620),Sell("DrivesT3",540),Sell("ModulesT3",640),
                Sell("ScrapAlloy",16,3),Sell("ReedWeave",54,3),Sell("Glowspore",63,3));
        }
        static ShopOffer Sell(string id,int price,int lot=0,string requires="")=>new(){id=id,price=price,lot=lot,requires=requires};

        /// <summary>The five quests, step for step as the script's manifest has them (section 4). A thing offered only at a quest's step (an off-lane beacon, the bell-buoy, the fourth
        /// pump, the vault wall) answers an Interact step with its own report; a talk that set the point's flag would stop the report.</summary>
        static void Quests()
        {
            ArenaBuilder.Asset<QuestDef>("ReedRoad",q=>{q.id="ReedRoad";q.title="Breathing Room";q.main=true;q.chapter=4;
                q.description="The convoy is running out of air, and Driftreed's weavers won't send a mat anywhere without a weaver to mend it.";
                q.steps=new[]{Step(ObjectiveKind.Flag,"wend.charted"),Step(ObjectiveKind.Reach,Veil),Step(ObjectiveKind.Flag,"wend.beaconRelit"),Step(ObjectiveKind.Reach,Town),Step(ObjectiveKind.Flag,"wend.onaHeard"),
                    Step(ObjectiveKind.Flag,"wend.tideTurned"),Step(ObjectiveKind.Flag,"wend.lureLearned"),Step(ObjectiveKind.Interact,"Pump",2),Step(ObjectiveKind.Flag,"wend.pimFound"),Step(ObjectiveKind.Interact,"LastPump"),
                    Step(ObjectiveKind.Kill,"Rotbloom"),Step(ObjectiveKind.Flag,"wend.decided")};
                q.stepText=new[]{"Teach Pell's lantern the lane to Wendmire","Take the Wendmire warp off Tallow's approach","Follow Maru's beacons and relight the dark one","Come down dark through the tide-gate to Driftreed",
                    "Hold the pilots' line, then ask Ona Sedge about mats","Ask Maru Wend to take you into the mire","Follow Maru's lamp across the mire: two short, one long",
                    "Run the pumps that drain the hall and the lower gallery","Find Ona's apprentice in the lower gallery","Run the last pump, at the great hall's door","Kill the Rotbloom",
                    "Go back to Ona Sedge and hear what Driftreed decides"};
                q.stepTargets=new[]{At("TallowDrift","Pell"),At(Veil,null,null,null,null,"TallowApproach"),At(Veil,null,"DeadBeacon"),At(Town,null,null,null,null,Veil),At(Town,"Ona"),At(Town,"Maru"),
                    At(Station,null,"LureLesson",null,null,Town),At(Station,null,"Pump",null,null,Town),At(Station,"Pim",null,null,null,Town),At(Station,null,"LastPump",null,null,Town),At(Station,null,null,null,null,Town),
                    At(Town,"Ona",null,null,null,Station)};
                q.closingLine="Chapter 4 complete. Many Hands has a dock off Tallow Drift, and a tug waiting with a ledger: launch for it.";
                // Where chapter 5 begins (Abri's welcome on Many Hands' dock, which starts its quest): the new dock off Tallow's approach. From Driftreed that is the Veil and the
                // warp home, or the chart to Tallow Drift (its diamond stands there until Many Hands has been stood on) and the pressure lift.
                q.next=At("ManyHands","Keeper",null,null,null,"TallowApproach",Veil);
                // The chapter's card waits for Abri's call, not Ona's decision that completes the quest; its body is two lines of the card and no more.
                q.cardFlag="chapter4.complete";q.cardBody="Driftreed's weavers are coming up on their own rafts, tied to nobody, and the mire has only its own lamps again. The convoy has a name now: Many Hands.";
                // What the story has set by its end that the steps above don't say: the world's own events on the main path, who has been met and heard, the fight. A later
                // chapter's fixture starts with these (DevLoadout).
                q.storyFlags=new[]{"wend.maruHailed","wend.beacon1","wend.beacon2","wend.beacon3","wend.arrived","wend.arrivalSeen","wend.lineHeld","met.Maru","met.Ona","wend.mireEntered",
                    "wend.pump1","wend.pump2","wend.pump3","wend.porticoReached","wend.hallSeen","wend.bloomSeen","bossdown.Rotbloom","wend.rotbloomDead","wend.lampCarried","chapter4.complete"};
                // A Prime drive of the party's own luck and a bolt of Driftreed's own weave: materials, parts and scrip, never a capped consumable (CHAPTER_GUIDE 0.1: two Repair
                // Salves on the card went to a full pouch as scrip, and the card was wrong).
                q.rewardXp=1000;q.rewardScrip=600;q.rewards=new[]{new Ingredient{id="DrivesT3",count=1},new Ingredient{id="ReedWeave",count=3}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("BeaconKeeper",q=>{q.id="BeaconKeeper";q.title="Two Short, One Long";q.chapter=4;
                q.description="Two of Driftreed's beacons in the Spore Veil have slipped their count, and a lamp with the wrong rhythm is worse than a dark one.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"OffLaneBeacon",2),Step(ObjectiveKind.Flag,"maru.beaconsReported")};
                q.stepText=new[]{"Set the two beacons off the Spore Veil's lane back to two short, one long","Tell Maru Wend both beacons are keeping time"};
                q.stepTargets=new[]{At(Veil,null,"OffLaneBeacon",null,"Retune the beacons",Town),At(Town,"Maru",null,null,"Tell Maru they're in time",Veil)};
                q.rewardXp=240;q.rewardScrip=160;q.rewards=new[]{new Ingredient{id="DrivesT3Scout",count=1}};q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("SmokehouseOrder",q=>{q.id="SmokehouseOrder";q.title="The Smokehouse Order";q.chapter=4;
                q.description="Dov Hale smokes his eel over Glowspore, and every cap of it grows in the mire.";
                q.steps=new[]{Step(ObjectiveKind.Collect,"Glowspore",6),Step(ObjectiveKind.Flag,"dov.orderFilled")};
                q.stepText=new[]{"Gather six Glowspore from the wet side of the mire's peat islands","Bring the Glowspore to Dov's smokehouse"};
                q.stepTargets=new[]{At(Station,null,null,null,"Gather Glowspore",Town),At(Town,"Dov",null,null,"Bring Dov the Glowspore",Station)};
                q.rewardXp=200;q.rewardScrip=90;q.rewards=Array.Empty<Ingredient>();q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("SchoolBell",q=>{q.id="SchoolBell";q.title="The School Bell";q.chapter=4;
                q.description="The school raft's bell-buoy slipped its line and is ringing out in the mire with no count to it.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"BellBuoy"),Step(ObjectiveKind.Flag,"dov.bellReported")};
                q.stepText=new[]{"Find the school raft's bell-buoy in the mire and ring it true","Tell Dov the bell is ringing true"};
                q.stepTargets=new[]{At(Station,null,"BellBuoy",null,"Ring the bell-buoy",Town),At(Town,"Dov",null,null,"Tell Dov it's ringing",Station)};
                q.rewardXp=220;q.rewardScrip=100;q.rewards=new[]{new Ingredient{id="ModulesPrismPulse",count=1}};q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("FourthPump",q=>{q.id="FourthPump";q.title="The Fourth Pump";q.chapter=4;
                q.description="Quill has counted the pipes for a pump that drains one small room, and can't say what the old engineers kept dry in it.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"FourthPump"),Step(ObjectiveKind.Interact,"VaultMark"),Step(ObjectiveKind.Flag,"quill.markRead")};
                q.stepText=new[]{"Find the fourth pump off the lower gallery's east side and run it","Take a rubbing of the marks in the drained vault","Show Quill the old engineers' mark"};
                q.stepTargets=new[]{At(Station,null,"FourthPump",null,"Run the fourth pump",Town),At(Station,null,"VaultMark",null,"Take a rubbing in the vault",Town),At(Town,"Quill",null,null,"Show Quill the rubbing",Station)};
                q.rewardXp=260;q.rewardScrip=160;q.rewards=new[]{new Ingredient{id="ModulesPrismPlasma",count=1}};q.flagsOnComplete=Array.Empty<string>();});
        }

        // ---------------------------------------------------------------- light
        static Color C(float r,float g,float b)=>new(r,g,b,1);
        /// <summary>The region's authored light and grade (ZoneLook finds this by its attribute). The Drowned Waystation's is registered from its own file.</summary>
        [ZoneLook.Source] static void Looks()
        {
            // Inside the Veil: a cool far star through spore haze, a green-violet fill off the nebula so a hull's shaded side reads against the dark.
            ZoneLook.Register(Veil,new ZoneLook.Spec{sun=C(.94f,1f,.92f),sunIntensity=1.3f,shadowStrength=.8f,
                sky=C(.3f,.4f,.42f),equator=C(.26f,.24f,.34f),ground=C(.12f,.12f,.16f),
                contrast=14,saturation=10,splitShadows=C(.45f,.42f,.58f),splitHighlights=C(.5f,.58f,.46f),
                bloomThreshold=.85f,bloomIntensity=.8f,vignette=.24f});
            // Dusk on the mire: a low warm sun under the moons, a lilac fill so shade on reed reads as evening and not as mud; haze across the water.
            ZoneLook.Register(Town,new ZoneLook.Spec{sun=C(1f,.88f,.74f),sunIntensity=1.3f,sunPitch=38,shadowStrength=.8f,
                sky=C(.5f,.5f,.66f),equator=C(.44f,.42f,.48f),ground=C(.22f,.22f,.2f),
                fog=true,fogColor=C(.6f,.58f,.7f),fogStart=55,fogEnd=200,
                contrast=10,saturation=6,splitShadows=C(.44f,.44f,.6f),splitHighlights=C(.58f,.53f,.44f),
                bloomThreshold=.95f,bloomIntensity=.6f,vignette=.2f});
        }

        // ---------------------------------------------------------------- what the region is made of
        /// <summary>The region's beds, made by tools/wendmire_ambience.py (synthesised, nothing sourced): the raft town, the mire, the spore veil. Until a file is there its zone keeps
        /// whatever its definition already holds.</summary>
        const string TownBed="Assets/_Project/Audio/Ambience/AMB_Driftreed.ogg",MireBed="Assets/_Project/Audio/Ambience/AMB_Mire.ogg",VeilBed="Assets/_Project/Audio/Ambience/AMB_SporeVeil.ogg";
        /// <summary>Measured, since nobody can listen to them: every earlier bed plays at its file's RMS (mixed to one channel) times its volume of about .0224, -33 dB. The files' RMS are
        /// .0676, .0773 and .0629 (the script prints them), so these put the three level with the rest.</summary>
        const float TownLevel=.331f,MireLevel=.29f,VeilLevel=.356f;
        /// <summary>Materials shared by the region's zones; each zone file adds its own.</summary>
        static void Materials()
        {
            DriftreedMaterials();
            VeilMaterials();
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- Tallow approach: the warp out
        /// <summary>The lane Pell's lantern learns. Replaces only its own warp, spawn and sign in the Tallow Approach scene, as chapter 3's does; re-run after that scene's own rebuild or staging
        /// (and after XylosBuilder.StageApproach, which re-stages the approach's shared pieces).</summary>
        static void StageTallowApproach()
        {
            var approach=EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowApproach.unity");
            foreach(var old in UnityEngine.Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Where(w=>w.scene==Veil).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(var old in UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Where(s=>s.id=="Wendmire").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            ChapterOneStaging.ClearSigns("Warp — The Spore Veil");
            // South-west of the arrival, the arc's fourth: the Halo's warp west, the Wick's east, Xylos's south-east.
            var warp=WorldBuilder.Warp("Warp — The Spore Veil",new Vector3(-11,1,-37),Veil,"wend.charted","Arrival");
            warp.lockedPrompt="No lane to Wendmire yet. Pell's lantern has to learn it first.";
            WorldBuilder.Spawn("Wendmire",new Vector3(-6,1,-30));
            ChapterOneStaging.Approach();
            EditorSceneManager.MarkSceneDirty(approach);EditorSceneManager.SaveScene(approach);Debug.Log("TALLOW_APPROACH_WENDMIRE_OK");
        }
    }
}
