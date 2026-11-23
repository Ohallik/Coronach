using System;
using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 8, Harrowgate (the Vaskar Reach; brief: docs/chapters/ch08-harrowgate.md; script wiring: docs/writing/ch08-manifest.md). Isolated builder for its definitions and four
    /// zones, on <see cref="ZoneKit"/>:
    /// <list type="bullet">
    /// <item>The Picket of Teeth (space combat, <c>HarrowgateBuilder.Picket.cs</c>): the Muster's border. Rule: staged detection (warning buoys, a hunter wing, the carrier), shown on
    /// screen; the Compact route lock across the one clean lane, taken apart to Idra's count. Map brief: docs/maps/PicketOfTeeth.md.</item>
    /// <item>The Scoria Flows (wild, <c>HarrowgateBuilder.Scoria.cs</c>): the second world engineer's. The crust, the garrisons, the Unsworn camp.</item>
    /// <item>The Bridle Foundry (dungeon and the Slag Colossus's heart, <c>HarrowgateBuilder.Foundry.cs</c>): the third world engineer's.</item>
    /// <item>Harrowgate (town, then the duel, <c>HarrowgateBuilder.Town.cs</c>): the town engineer's.</item>
    /// </list>
    /// Ordinary enemies, the flight fights and Vessa Korl are in <c>HarrowgateBuilder.Enemies.cs</c> (the first combat engineer's); the Slag Colossus and Drask Vaal in
    /// <c>HarrowgateBuilder.Bosses.cs</c> (the second's), which <c>DefineEnemies</c> calls. Kit pieces from batch C15-Vaskar (tools/c15_intake.py). Members of every file share one class,
    /// so each zone's own helpers carry its name or live in a nested class.
    /// Runs: <c>Definitions</c>, <c>BuildZones</c> with <c>-harrowgate-zones PicketOfTeeth,ScoriaFlows,BridleFoundry,Harrowgate</c>, <c>Dialogue</c>, <c>StageApproach</c>; then
    /// <c>ZoneLook.Apply</c> for what was built.
    /// </summary>
    public static partial class HarrowgateBuilder
    {
        internal const string PicketZone="PicketOfTeeth",ScoriaZone="ScoriaFlows",FoundryZone="BridleFoundry",TownZone="Harrowgate";
        public static readonly string[] Zones={PicketZone,ScoriaZone,FoundryZone,TownZone,HarbourZone};
        public static void Definitions()=>BatchTools.Run(()=>{Define();AssetDatabase.SaveAssets();Debug.Log("HARROWGATE_DEFS_OK");});
        public static void BuildZones()=>BatchTools.Run(()=>
        {
            string arg=DevArgs.Value("-harrowgate-zones");var zones=string.IsNullOrEmpty(arg)?Zones:arg.Split(',',StringSplitOptions.RemoveEmptyEntries);
            Define();foreach(var zone in zones)BuildZone(zone.Trim());
            BatchTools.RegisterScenes();AssetDatabase.SaveAssets();Debug.Log("HARROWGATE_ZONES_OK "+string.Join(",",zones));
        });
        /// <summary>Copies docs/writing/*.yarn into Resources and recompiles the Yarn project: a new .yarn file is not in the compiled program until the project reimports, and a missing
        /// node fails silently at runtime.</summary>
        public static void Dialogue()=>BatchTools.Run(()=>{RefreshDialogue();Debug.Log("HARROWGATE_DIALOGUE_OK");});
        /// <summary>The way in from chapter 7 and the way home (Many Hands and its hull): re-run after Many Hands' own rebuild and after OrielBuilder.StageApproach.</summary>
        [Restage.Step("chapter8.many-hands","ManyHands,ManyHandsHull",After="chapter7.many-hands",Chapter=8,Leaves=new[]{"ManyHands: Harrowgate: Brekk on the dock #1","ManyHands: Harrowgate: Brekk's word #1","ManyHands: Brekk's word from Scoria <DiscoveryPoint> #1","ManyHands: Harrowgate: Idra's plan for the Picket #1","ManyHands: Idra's plan for the Picket <DiscoveryPoint> #1","ManyHands: Harrowgate: Tovra aboard #1","ManyHands: Harrowgate: Brekk aboard #1","ManyHands: Harrowgate: Drask aboard #1","ManyHandsHull: Harrowgate: warp to the Picket <WarpBeacon> #1","ManyHandsHull: Sign_Warp — The Picket of Teeth #1","ManyHandsHull: Harrowgate: warp sign #1","ManyHandsHull: Harrowgate: spawn from the Picket <SpawnPoint> #1"})]
        public static void StageApproach()=>BatchTools.Run(()=>{Define();StageManyHands();AssetDatabase.SaveAssets();Debug.Log("HARROWGATE_APPROACH_OK");});
        /// <summary>Every node of the chapter's script (manifest section 2): one that didn't compile stops the build here instead of doing nothing when talked to.</summary>
        static readonly string[] Nodes=
        {
            "BrekkScoriaWord","IdraPicketPlan","AbriTwentySeven","AbriThreeWords",
            "IdraPicketEdge","IdraLockReached","IdraLockOff",
            "BrekkCrust","BrekkCrossing","TovraFirst","TovraRepeat","TovraCages","TovraQuietOffer","TovraQuietReturn","BrekkCampStall","BrekkCampRepeat","BrekkPikeHead",
            "IdraFirstSluice","VessaGallery","VessaFallsBack","SelaCagedRoad","TovraRelease","BrekkAnvil",
            "TovraHarrowgateRoad","HessaSisters","HessaParley","HessaGateRepeat","HessaWarmaster",
            "PipFirst","PipField","PipOathHall","PipWashing","PipStairGame","PipArmourers","PipHarbourWall","SelaTrophyGallery","PipGallery",
            "OdrunFirst","OdrunRepeat","OdrunSleepless","OdrunKitchenOffer","OdrunKitchenReturn","OdrunShop","OdrunTovra","BrekkKitchen",
            "DraskReceives","DraskOnTheField","DraskLeavesField","OdrunVerdict","HessaArmouryGate","PipAfterDuel","PipOathDay","PipPost","IdraLockCounted","DraskCustody",
            "BrynNacreTraffic","MeretAllStations",
            "TovraAboard","BrekkAboard","AbriSmiths","DraskAboard","PellAumiAnswers",
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
                case PicketZone:BuildPicket();break;
                case ScoriaZone:BuildScoria();break;
                case FoundryZone:BuildFoundry();break;
                case TownZone:BuildTown();break;
                case HarbourZone:BuildHarbour();break;
                default:throw new InvalidOperationException("Unknown Harrowgate zone "+zone);
            }
        }

        // ---------------------------------------------------------------- definitions
        static T Find<T>(string id)where T:ScriptableObject=>ZoneKit.Find<T>(id);
        static Objective Step(ObjectiveKind kind,string target,int count=1)=>new(){kind=kind,target=target,count=count};
        static QuestTarget At(string scene,string speaker=null,string report=null,string encounter=null,string brief=null,params string[] via)=>new(){scene=scene,speaker=speaker,report=report,encounter=encounter,brief=brief,via=via};
        static ShopOffer Sell(string id,int price,int lot=0,string requires="")=>new(){id=id,price=price,lot=lot,requires=requires};
        /// <summary>The first of these bodies that has been taken in.</summary>
        internal static GameObject FirstBody(params string[] ids)=>ids.Select(id=>ZoneKit.Prefab("Characters",id)).FirstOrDefault(body=>body!=null);

        static void Define()
        {
            // The Warmaster's own body (the town engineer's: HarrowgateBuilder.TownLook.cs), made before the definitions that wear it: his duel's (BossDefinitions) and his person's
            // (below). If it can't be made, he keeps the bodies he had, and the build says so.
            GameObject warmaster=null;
            try{warmaster=Town.Warmaster().person;}catch(Exception e){Debug.LogWarning("HARROWGATE_WARMASTER_PENDING "+e.Message);}
            var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");
            // The Picket: the buoys' amber and the flak curtain's lamps are the border's state, so they are what blooms. No tilt anywhere (D060).
            var picket=ArenaBuilder.Asset<Hd2dProfile>("Picket",p=>{p.tint=new Color(.98f,.97f,1);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.72f;});
            // Scoria by day under ash: a warm tint, and the seams and the bright flow are what blooms.
            var scoria=ArenaBuilder.Asset<Hd2dProfile>("Scoria",p=>{p.tint=new Color(1,.95f,.9f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.66f;});
            // Harrowgate's terraces: basalt under cook-smoke, a plainer grade than the Flows.
            var basalt=ArenaBuilder.Asset<Hd2dProfile>("Basalt",p=>{p.tint=new Color(1,.97f,.94f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.55f;});
            var source=ZoneKit.WorldMaterial("tallow-sky");
            // The panorama turned to a part no earlier lane shows and dulled toward iron and rust: the Picket is neither the Orrery's brass, the Veil's green-violet nor the Shear's teal.
            var space=ZoneKit.Sky("vaskar-picket-sky",source,new Color(.56f,.46f,.46f,.5f),71,.7f);
            // Over Scoria and Harrowgate the sky is only the frame's far edge: ash-brown with the furnace under it.
            var ash=ZoneKit.Sky("vaskar-ash-sky",source,new Color(.66f,.5f,.42f,.5f),252,1.5f);
            // Enemy damage is authored at its real weight (HarrowgateBuilder.Enemies.cs), so every zone's threat is 1.
            ArenaBuilder.Asset<ZoneDef>(PicketZone,z=>{z.id=z.scene=PicketZone;z.displayName="The Picket of Teeth";z.chapter=8;z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=picket;z.skybox=space;
                z.enemyThreat=1;z.flightPlane=1;z.cameraClearsScenery=false;z.musicId="Galactic Conquest";z.fightMusicId="Starfight";
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(PicketBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=PicketLevel;}});
            ArenaBuilder.Asset<ZoneDef>(ScoriaZone,z=>{z.id=z.scene=ScoriaZone;z.displayName="The Scoria Flows";z.chapter=8;z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=scoria;z.skybox=ash;
                z.enemyThreat=1;z.cameraClearsScenery=true;z.musicId="Alien Pulse";z.shopMusicId="Shop Theme";
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(ScoriaBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=ScoriaLevel;}
                // What the Flows' own engineer sets on its definition (a pocket's music, a bright floor): HarrowgateBuilder.Scoria.cs.
                ScoriaDefinition(z);});
            ArenaBuilder.Asset<ZoneDef>(FoundryZone,z=>{z.id=z.scene=FoundryZone;z.displayName="The Bridle Foundry";z.chapter=8;z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=scoria;z.skybox=ash;
                z.enemyThreat=1;z.cameraClearsScenery=true;z.musicId="Alien Pulse";
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(FoundryBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=FoundryLevel;}
                // What the Foundry's own engineer sets on its definition (its profile, its sky or none): HarrowgateBuilder.Foundry.cs.
                FoundryDefinition(z);});
            ArenaBuilder.Asset<ZoneDef>(TownZone,z=>{z.id=z.scene=TownZone;z.displayName="Harrowgate";z.chapter=8;z.kind=ZoneKind.GroundSafe;z.cameraProfile=ground;z.hd2dProfile=basalt;z.skybox=ash;
                z.enemyThreat=1;z.cameraClearsScenery=true;z.musicId="Galactic Conquest";z.shopMusicId="Shop Theme";
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(TownBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=TownLevel;}
                // What the town's own engineer sets on its definition (a bright floor, a fight pocket's music): HarrowgateBuilder.Town.cs.
                TownDefinition(z);});
            // The war harbour, flown in the pursuit (the Warmaster's fight, part two: docs/maps/Harrowgate.md, "Pursuit contract"): a town is a ground zone, so his run is a flight zone of
            // its own, reached only from the forecourt once the duel is won. Its bed is the town's.
            ArenaBuilder.Asset<ZoneDef>(HarbourZone,z=>{z.id=z.scene=HarbourZone;z.displayName="The War Harbour";z.chapter=8;z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=basalt;z.skybox=ash;
                z.enemyThreat=1;z.flightPlane=1;z.cameraClearsScenery=false;z.musicId="Galactic Conquest";
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(TownBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=TownLevel;}});

            DefineEnemies();
            Quests();

            // Harrowgate on Sela's chart (guide 3.10): charted by arriving, joined to Many Hands (whose hull the lane leaves from), in the last column, the chapter's own slot.
            ArenaBuilder.Asset<ChartStop>(TownZone,s=>{s.id=TownZone;s.displayName="Harrowgate";s.region="The Vaskar Reach";
                s.blurb="Basalt terraces round the Muster's war harbour: the armourers' street, a bench, a mender, and a kitchen nobody eats in.";
                s.zone=TownZone;s.spawn="Arrival";s.position=new Vector2(1,.66f);s.routes=new[]{"ManyHands"};s.shop=s.bench=s.repair=true;s.chapter=8;
                s.reaches=new[]{PicketZone,ScoriaZone,FoundryZone,HarbourZone};});

            // The new speakers (manifest section 5). Brekk, Hessa, Vessa, Idra, Meret, Abri, Pell and Oda already have what they need and keep it.
            void Person(string id,string display,bool female,GameObject body)
                =>ArenaBuilder.Asset<CharacterDef>(id,c=>{c.id=c.speakerId=id;c.displayName=display;c.female=female;c.natural=body;
                    // The sheets were taken in before these definitions existed, so PortraitIntake had nothing to bind them to.
                    if(c.portraitNatural==null)c.portraitNatural=AssetDatabase.LoadAssetAtPath<PortraitSet>("Assets/_Project/Resources/Portraits/"+id+"_Natural.asset");});
            // His war-frame under his mantle, the body he fights in: at his table, in his cell and aboard Many Hands (a smith's red apron until November 21, s51/149).
            Person("Drask","Drask Vaal",false,warmaster??FirstBody("DraskBody","BrekkBody"));
            Person("Tovra","Tovra Kesh",true,FirstBody("TovraBody","BrekkBody"));
            Person("Odrun","Mother Odrun",true,FirstBody("OdrunBody","BrekkBody"));
            Person("Pip","Pip",false,FirstBody("PipBody","HessaBody","BrekkBody"));
            // Heard by radio only in this chapter; chapter 9 stands her in the world.
            Person("Bryn","Bryn Asta",true,FirstBody("BrynBody","CorraBody"));

            // Two counters (manifest 8.13), opened by who is talking (ShopUi.For): Brekk's stall at the Unsworn camp, and Odrun's back table in the hidden quarter, which only her
            // stocked kitchen's talks open.
            ProgressionBuilder.Stall(ScoriaZone+"_Brekk","THE CAMP STALL",Sell("RepairGel",28),Sell("RepairSalve",68),Sell("ChargeCell",32),Sell("ChargeBank",68),Sell("SyncTonic",100),Sell("RelinkSpike",100),
                Sell("PlatingWax",56),Sell("SurgeDraught",56),Sell("Quickening",56),
                Sell("EmittersT4",960),Sell("EdgesT4",1020),Sell("FramesT4",1050),Sell("DrivesT4",920),Sell("ModulesT4",1080),Sell("ScrapAlloy",16,3));
            ProgressionBuilder.Stall(TownZone+"_Odrun","THE BACK TABLE",Sell("RepairGel",28),Sell("RepairSalve",68),Sell("ChargeBank",68),Sell("SyncTonic",100),Sell("RelinkSpike",100),
                Sell("PlatingWax",56),Sell("SurgeDraught",56),Sell("Quickening",56),
                Sell("EmittersT4",960),Sell("EdgesT4",1020),Sell("FramesT4",1050),Sell("DrivesT4",920),Sell("ModulesT4",1080),Sell("ScrapAlloy",16,3));
        }

        /// <summary>The five quests, step for step as the script's manifest has them (section 4). A step the world reports (the Picket crossed) is an Interact step on the report of the
        /// point that stands there, so it moves the moment it happens: a flag set by a point rather than by <c>&lt;&lt;setFlag&gt;&gt;</c> would wait for the next load. The pursuit's step is a
        /// Kill on the skiff, which its death reports. The reports and speakers each zone stands are in its map doc. Rewards are materials, parts and scrip, never a capped consumable
        /// (CHAPTER_GUIDE 0.1), and every card names exactly what is paid.</summary>
        static void Quests()
        {
            const string City="ManyHands",Hull="ManyHandsHull";
            ArenaBuilder.Asset<QuestDef>("Harrowgate",q=>{q.id="Harrowgate";q.title="Harrowgate";q.main=true;q.chapter=8;
                q.description="The Muster's foundry on Scoria has a young road in a cage, and its Warmaster has orders in for five more.";
                q.steps=new[]{Step(ObjectiveKind.Flag,"vask.lockAdmitted"),Step(ObjectiveKind.Reach,PicketZone),Step(ObjectiveKind.Interact,"PicketCrossed"),Step(ObjectiveKind.Flag,"vask.lockRemoved"),
                    Step(ObjectiveKind.Flag,"vask.unswornMet"),Step(ObjectiveKind.Reach,FoundryZone),Step(ObjectiveKind.Flag,"vask.vessaBeaten"),Step(ObjectiveKind.Kill,"SlagColossus"),
                    Step(ObjectiveKind.Flag,"vask.juvenileFreed"),Step(ObjectiveKind.Flag,"vask.roadSaid"),Step(ObjectiveKind.Flag,"vask.parley"),Step(ObjectiveKind.Flag,"vask.draskMet"),
                    Step(ObjectiveKind.Flag,"vask.duelWon"),Step(ObjectiveKind.Kill,"DraskSkiff"),Step(ObjectiveKind.Flag,"vask.deposed"),Step(ObjectiveKind.Flag,"vask.nacreHeard")};
                q.stepText=new[]{"Hear Idra's plan for the Picket at the chart table","Take the lane to the Muster's border","Cross the Picket, from shadow to shadow or straight through",
                    "Take the route lock apart to Idra's count","Follow Brekk across the Flows to the Unsworn camp","Go into the Bridle Foundry","Take the casting gallery from Vessa Korl",
                    "Cool the Slag Colossus at the sluices, and break it","Stand back while the smiths open the cage","Go back to Tovra at the camp",
                    "Walk in at Harrowgate's harbour gate under a parley flag","See the town, and then the Warmaster","Answer the Warmaster on the muster field",
                    "Stop his skiff before it reaches the carrier","Stand on the field while Harrowgate answers","See the Warmaster in the harbour cells"};
                // Step 0's arrow finds Idra's place at the chart table (report PicketPlan, StageManyHands); step 2 the line across the Picket in sight of the lock, step 3 the next piece
                // of the lock, which is the only one offered at a time (both the Picket's own: docs/maps/PicketOfTeeth.md).
                q.stepTargets=new[]{At(City,null,"PicketPlan"),At(PicketZone,null,null,null,null,Hull),At(PicketZone,null,"PicketCrossed",null,null,Hull),At(PicketZone,null,"LockPiece",null,null,Hull),
                    At(ScoriaZone,"Tovra",null,null,null,PicketZone),At(FoundryZone,null,null,null,null,ScoriaZone),At(FoundryZone,null,null,"VessaKorl",null,ScoriaZone),
                    At(FoundryZone,null,null,"SlagColossus",null,ScoriaZone),At(FoundryZone,null,null,null,null,ScoriaZone),At(ScoriaZone,"Tovra",null,null,null,FoundryZone),
                    At(TownZone,"Hessa",null,null,null,ScoriaZone),At(TownZone,"Drask",null,null,null,ScoriaZone),At(TownZone,null,null,"DraskVaal",null,ScoriaZone),
                    At(HarbourZone,null,null,"DraskSkiff",null,TownZone),At(TownZone,null,null,null,null,ScoriaZone),At(TownZone,"Drask",null,null,null,ScoriaZone)};
                q.closingLine="Chapter 8 complete. The Warmaster's last order outlived his command. Nacre is calling.";
                // Chapter 9 begins by itself: Abri turns the convoy ten seconds after this chapter's card, at the cells, or aboard Many Hands for a save that left before she spoke
                // (HomeBuilder.Approach). So the arrow leads aboard only for whoever is neither place.
                q.next=At(City,null,null,null,null,Hull,PicketZone);
                // The chapter's card waits for Meret's all-stations, the chapter's last talk, which sets this flag; its body is two lines of the card and no more.
                q.cardFlag="chapter8.complete";
                q.cardBody="The route lock is off the Picket, and the young road went free with sixty smiths watching. Harrowgate took its Warmaster's tabs. His last order closed the Nacre road.";
                // What the story has set by its end that the steps above don't say: the world's own events on the main path, who has been met and heard, the fights. A later chapter's
                // fixture starts with these (DevLoadout). The Flows', the Foundry's and the town's are as their map docs give them.
                q.storyFlags=new[]{"vask.brekkWord","vask.picketRead","vask.picketCrossed","vask.lock1","vask.lock2","vask.lock3","vask.lock4",
                    "vask.crustRead","vask.garrisonSeen","met.Tovra","vask.sluiceRead","vask.sluice1","vask.sluice2","vask.vessaMet","vask.cageSeen","bossdown.SlagColossus","vask.anvilSeen",
                    "vask.sistersHeard","met.Hessa","vask.pipMet","vask.odrunMet","met.Odrun","vask.gallerySeen","vask.townSeen","met.Drask","vask.fieldTaken","bossdown.DraskVaal","bossdown.DraskSkiff",
                    "vask.pursuitWon","vask.gateDenied","idra.lockCounted","vask.orderSaid","chapter8.complete"};
                // A Tempered edge of the party's own luck and the region's two commons: materials, parts and scrip. The XP is the chapter's ledger's (HarrowgateBuilder.Enemies.cs): 1,000 for the
                // main quest and 800 for the four side quests, which with the fights bring the party from Sync 22 to 25.
                q.rewardXp=1000;q.rewardScrip=1200;q.rewards=new[]{new Ingredient{id="EdgesT4",count=1},new Ingredient{id="BridleWire",count=4},new Ingredient{id="ScoriaGlass",count=4}};
                q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("QuietHands",q=>{q.id="QuietHands";q.title="Quiet Hands";q.chapter=8;
                q.description="Half of Tovra's smiths have family holding pikes at the crossings. Three of them have written, and a pikeguard seen taking paper is flogged.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"KettleStone",3),Step(ObjectiveKind.Flag,"tovra.messagesLeft")};
                q.stepText=new[]{"Leave a message under the kettle stone at each of three garrison cookfires","Tell Tovra they're left"};
                q.stepTargets=new[]{At(ScoriaZone,null,"KettleStone",null,"Leave the messages"),At(ScoriaZone,"Tovra",null,null,"Tell Tovra")};
                // "Frame plate, off the cage's own run": a Tempered frame.
                q.rewardXp=220;q.rewardScrip=180;q.rewards=new[]{new Ingredient{id="FramesT4",count=1}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("RelayGlass",q=>{q.id="RelayGlass";q.title="What Was Taken";q.chapter=8;
                q.description="Lanternwick's relay glass is in Harrowgate's trophy galleries, under a card that says the canyon yielded it.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"GlassCase",3)};
                q.stepText=new[]{"Take the relay glass out of three cases: the gallery, the east gallery, the oath hall"};
                q.stepTargets=new[]{At(TownZone,null,"GlassCase",null,"Take back the glass")};
                q.rewardXp=200;q.rewardScrip=160;q.rewards=new[]{new Ingredient{id="ModulesT4",count=1}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("OdrunsKitchen",q=>{q.id="OdrunsKitchen";q.title="Odrun's Kitchen";q.chapter=8;
                q.description="A kitchen that doesn't exist can't draw rations. Mother Odrun wants three things she can't buy with her own face.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"KitchenWant",3),Step(ObjectiveKind.Flag,"odrun.kitchenStocked")};
                q.stepText=new[]{"Find garrison salt, ash-root flour and a regulation mess kettle","Take them to Mother Odrun"};
                q.stepTargets=new[]{At(TownZone,null,"KitchenWant",null,"Find Odrun's things"),At(TownZone,"Odrun",null,null,"Take them to Odrun")};
                // Her provisions would be a capped consumable, paid as scrip to a full pouch: the kitchen pays in what the harbour scales didn't count instead. The back table is her shop.
                q.rewardXp=180;q.rewardScrip=150;q.rewards=new[]{new Ingredient{id="ScoriaGlass",count=3},new Ingredient{id="BridleWire",count=3}};q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("PipsOath",q=>{q.id="PipsOath";q.title="The Oath";q.chapter=8;
                q.description="Cadet Pip takes the Muster's oath tomorrow, in the third file, by the drain. He is showing you the town because nobody told him not to.";
                q.steps=new[]{Step(ObjectiveKind.Flag,"pip.fieldTalked"),Step(ObjectiveKind.Flag,"pip.galleryTalked"),Step(ObjectiveKind.Flag,"pip.oathLeft")};
                q.stepText=new[]{"Let Pip show you the muster field","Find Pip in the trophy gallery, by the door","Find Pip after the duel"};
                q.stepTargets=new[]{At(TownZone,"Pip",null,null,"See the field with Pip"),At(TownZone,"Pip",null,null,"Find Pip in the gallery"),At(TownZone,"Pip",null,null,"Find Pip after the duel")};
                // "A Response token": a Tempered drive.
                q.rewardXp=200;q.rewardScrip=160;q.rewards=new[]{new Ingredient{id="DrivesT4",count=1}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
        }

        // ---------------------------------------------------------------- light
        static Color C(float r,float g,float b)=>new(r,g,b,1);

        // ---------------------------------------------------------------- Many Hands: the way in and the way home
        /// <summary>Where chapter 8 stands in chapter 5's two scenes, and only that: each piece is named "Harrowgate: ..." and a rerun destroys what it made before. In the city: Brekk on the
        /// dock from the moment chapter 7 is done until the party has left for the Picket, and his word as a crossing past him (BrekkScoriaWord, which starts the chapter); Idra's plan at the
        /// chart table once he has said it (IdraPicketPlan); once the chapter is done, Tovra in the freight hall with Brekk at her forge, and Drask in custody by the galley. Outside: the warp to
        /// the Picket off the stern, beside Hoarfell's and Oriel's, locked until Idra has laid the Picket out, and its spawn.</summary>
        static void StageManyHands()
        {
            const string Mark="Harrowgate: ";
            void Clear(){foreach(var go in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t!=null&&t.parent==null&&t.name.StartsWith(Mark)).Select(t=>t.gameObject).ToArray())UnityEngine.Object.DestroyImmediate(go);}
            Vector3 Floor(Vector2 p)
            {
                Physics.SyncTransforms();
                var hits=Physics.RaycastAll(new Vector3(p.x,60,p.y),Vector3.down,120,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).ToArray();
                if(hits.Length==0)throw new InvalidOperationException("Harrowgate staging: no floor under "+p+" in "+EditorSceneManager.GetActiveScene().name);
                return hits[0].point;
            }
            void OffTheWays(Vector2 p,string who){float d=ManyHandsBuilder.Hands.FromWays(p);if(d<2)throw new InvalidOperationException("Harrowgate staging: "+who+" at "+p+" stands "+d.ToString("0.0")+" m from a way people walk");}
            GameObject Holder(string name)=>new(Mark+name);
            void Where(GameObject holder,string from,string goneAt,params GameObject[] who)
            {
                var where=holder.AddComponent<Whereabouts>();where.from=from??"";where.goneAt=goneAt??"";where.awayFrom=where.backAt="";where.live=false;where.targets=who;
            }
            Npc Aboard(string id,Vector2 at,float yaw,string node)
            {
                OffTheWays(at,id+" aboard");var holder=Holder(id+" aboard");var npc=WorldBuilder.Npc(id,Floor(at),node);npc.transform.SetParent(holder.transform,true);npc.transform.rotation=Quaternion.Euler(0,yaw,0);
                npc.firstNode=npc.repeatNode=node;npc.postNode="";npc.postFlag="";npc.finalFlag=npc.finalNode=null;npc.name=id+" aboard";
                Where(holder,"chapter8.complete",null,npc.gameObject);return npc;
            }

            // ---- the city
            var city=EditorSceneManager.OpenScene("Assets/_Project/Scenes/ManyHands.unity");Clear();
            // Brekk on the dock with his tool roll, come from Lanternwick himself: from chapter 7's end until the party has gone for the Picket.
            var dockHolder=Holder("Brekk on the dock");var brekkAt=new Vector2(6.4f,-46.6f);OffTheWays(brekkAt,"Brekk on the dock");
            var brekkBody=ZoneKit.Prefab("Characters","BrekkBody");
            if(brekkBody!=null)
            {
                var body=(GameObject)PrefabUtility.InstantiatePrefab(brekkBody);body.transform.SetParent(dockHolder.transform,false);body.transform.SetPositionAndRotation(Floor(brekkAt),Quaternion.Euler(0,250,0));body.name="Brekk with his tool roll";
                var solid=body.AddComponent<BoxCollider>();solid.size=new Vector3(.8f,2,.8f);solid.center=Vector3.up;
                foreach(var rig in body.GetComponentsInChildren<Animator>())rig.cullingMode=AnimatorCullingMode.CullCompletely;
                Where(dockHolder,"chapter7.complete","vask.lockAdmitted",body);
            }
            // His word: walked into stepping up off the dock, just past him, so the one follows the other.
            var wordHolder=Holder("Brekk's word");var word=new GameObject("Brekk's word from Scoria",typeof(DiscoveryPoint),typeof(BoxCollider));word.transform.SetParent(wordHolder.transform,false);word.transform.position=Floor(new Vector2(0,-44.4f));
            var said=word.GetComponent<DiscoveryPoint>();said.prompt="Brekk's word from Scoria";said.flag="vask.brekkWord";said.report="";said.situation="";said.dialogueNode="BrekkScoriaWord";said.dialogueSpeaker="Brekk";
            said.requiredQuest="";said.requiredStep=0;said.requiredFlag="chapter7.complete";said.range=2;said.startsOnCrossing=true;
            var mouth=word.GetComponent<BoxCollider>();mouth.isTrigger=true;mouth.size=new Vector3(16,5,3);mouth.center=Vector3.up*2.5f;
            // Idra's plan at the chart table, once Brekk has said it: what the main quest's first step points at (report PicketPlan; the talk sets the flag itself).
            var planHolder=Holder("Idra's plan for the Picket");var plan=new GameObject("Idra's plan for the Picket",typeof(DiscoveryPoint));plan.transform.SetParent(planHolder.transform,false);
            plan.transform.position=Floor(ManyHandsBuilder.Hands.ChartTalk);
            var lay=plan.GetComponent<DiscoveryPoint>();lay.prompt="Hear Idra's plan for the Picket";lay.flag="vask.lockAdmitted";lay.report="PicketPlan";lay.situation="";lay.dialogueNode="IdraPicketPlan";lay.dialogueSpeaker="Idra";
            lay.requiredQuest="";lay.requiredStep=0;lay.requiredFlag="vask.brekkWord";lay.range=2.4f;
            // Once the chapter is done: Tovra in the freight hall between the herd and the garden, Brekk at her forge, Drask in his cell on the freight yard.
            // Where Many Hands' arrivals leave them room (ManyHandsBuilder.Hands.Arrivals): Tovra at her anvil, Brekk at the bellows, Drask in his cell, talked to through the bars.
            Aboard("Tovra",ManyHandsBuilder.Hands.Arrivals.TovraAt,180,"TovraAboard");
            Aboard("Brekk",ManyHandsBuilder.Hands.Arrivals.BrekkAt,165,"BrekkAboard");
            Aboard("Drask",ManyHandsBuilder.Hands.Arrivals.DraskAt,0,"DraskAboard");
            EditorSceneManager.MarkSceneDirty(city);EditorSceneManager.SaveScene(city);Debug.Log("MANYHANDS_HARROWGATE_STAGED city brekk="+brekkAt+" word=(0,-44.4) plan="+ManyHandsBuilder.Hands.ChartTalk);

            // ---- the hull: the warp to the Picket off the stern, and the spawn a ship back from it arrives at.
            var hull=EditorSceneManager.OpenScene("Assets/_Project/Scenes/ManyHandsHull.unity");Clear();
            // Fifteen metres in from the harbour line's west wall and fifteen past Oriel's: at x -150 it stood inside the wall, and a hull could take it only from a band 3.2 to 5 m east.
            var warpAt=new Vector3(-135,ZoneKit.FlightPlane,-176);
            ChapterOneStaging.ClearSigns("Warp — The Picket of Teeth"); // the warp's own sign carries no "Harrowgate: " name, so Clear() left it and every run added another (Restage)
            var warp=WorldBuilder.Warp("Warp — The Picket of Teeth",warpAt,PicketZone,"vask.lockAdmitted","Arrival");warp.lockedPrompt="No lane to the Muster's border yet: Idra lays out the Picket at the chart table first";warp.name=Mark+"warp to the Picket";
            WorldBuilder.Label("Warp — The Picket of Teeth",warpAt+new Vector3(0,-.7f,-7),3.2f).name=Mark+"warp sign";
            var spawn=WorldBuilder.Spawn(HomeSpawn,new Vector3(-142,ZoneKit.FlightPlane,-166));spawn.name=Mark+"spawn from the Picket";
            EditorSceneManager.MarkSceneDirty(hull);EditorSceneManager.SaveScene(hull);Debug.Log("MANYHANDS_HARROWGATE_STAGED hull warp="+warpAt+" spawn=(-142,1,-166)");
        }
        /// <summary>The spawn in Many Hands' hull a ship back from the Picket arrives at.</summary>
        internal const string HomeSpawn="Vaskar";

        // ---------------------------------------------------------------- what the region is made of
        /// <summary>The region's beds, made by tools/harrowgate_ambience.py (synthesised, nothing sourced). Until a file is there its zone keeps whatever its definition already holds.</summary>
        const string PicketBed="Assets/_Project/Audio/Ambience/AMB_PicketOfTeeth.ogg",ScoriaBed="Assets/_Project/Audio/Ambience/AMB_ScoriaFlows.ogg",
            FoundryBed="Assets/_Project/Audio/Ambience/AMB_BridleFoundry.ogg",TownBed="Assets/_Project/Audio/Ambience/AMB_Harrowgate.ogg";
        /// <summary>Measured, since nobody can listen to them: every earlier bed plays at its file's RMS (mixed to one channel) times its volume of about .0224, -33 dB. The files' RMS
        /// are .0752, .0730, .0746 and .0701 (the script prints them), so these put the four level with the rest.</summary>
        const float PicketLevel=.298f,ScoriaLevel=.307f,FoundryLevel=.3f,TownLevel=.32f;
    }
}
