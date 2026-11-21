using System;
using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Chapter 10, A Route With Room To Turn Back (brief: docs/chapters/ch10-a-route-with-room-to-turn-back.md; script: docs/writing/LastMooring.yarn and docs/writing/ch10-manifest.md;
    /// who places what, every flag, the contracts between files and the order they are built in: docs/chapters/ch10-wiring.md). Isolated builder for the chapter's definitions and its six
    /// zones, on <see cref="ZoneKit"/>. Members of every file share one class, so each place's own helpers live in its nested class, in its own file, and nobody edits another's:
    /// <list type="bullet">
    /// <item>Sorrel's second sky (space combat with berths on foot, <c>LastMooringBuilder.Sorrel.cs</c>): the first moon under Stillwater's clamp, Mother of Doors and her four clamps.</item>
    /// <item>Last Mooring (space combat, <c>LastMooringBuilder.Mooring.cs</c>): arenas divided by anchor chains, the convoy in the lee, the Tollkeeper's cage across the lane.</item>
    /// <item>The Continuance (dungeon on foot, with the ship-to-ship in a flight berth, <c>LastMooringBuilder.Continuance.cs</c>): the boarding spine, the maintenance decks, the works.</item>
    /// <item>The anchor chamber (zone <c>StillwaterChamber</c>, <c>LastMooringBuilder.Chamber.cs</c>): the frame gantry and the control well; the anchor's release.</item>
    /// <item>The Returning Passage (space combat, <c>LastMooringBuilder.Passage.cs</c>): the run with the convoy, the turn back, the tug, the rendezvous.</item>
    /// <item>Neighbourhood Afloat (town, <c>LastMooringBuilder.Neighbourhood.cs</c>): the epilogue and the last scene.</item>
    /// </list>
    /// The fights' definitions are each their own file: ordinary enemies <c>LastMooringBuilder.Enemies.cs</c>, Mother of Doors <c>LastMooringBuilder.Mother.cs</c>, the Tollkeeper
    /// <c>LastMooringBuilder.Tollkeeper.cs</c>, Meret's three phases <c>LastMooringBuilder.Meret.cs</c>; <see cref="Define"/> calls the enemies first, so a boss can name one. The chapter's
    /// beat in older scenes (Many Hands' chart table and Orrin's board, the warps off the hull's stern) is <c>LastMooringBuilder.Approach.cs</c>, registered with the restage runner.
    /// Runs: <c>Definitions</c> (marker <c>MOORING_DEFS_OK</c>), <c>BuildZones</c> with <c>-mooring-zones SorrelSecondSky,LastMooring,Continuance,StillwaterChamber,ReturningPassage,NeighbourhoodAfloat</c>
    /// (<c>MOORING_ZONES_OK</c>; a zone whose own build isn't written yet stands as an empty loadable placeholder, D270), <c>Dialogue</c> (<c>MOORING_DIALOGUE_OK</c>), <c>StageApproach</c>
    /// (<c>MOORING_APPROACH_OK</c>; normally through <c>Restage.All</c>); then <c>ZoneLook.Apply</c> for what was built, and <c>Restage.All</c> for every scene rebuilt.
    /// </summary>
    public static partial class LastMooringBuilder
    {
        internal const string SorrelZone="SorrelSecondSky",MooringZone="LastMooring",ContinuanceZone="Continuance",ChamberZone="StillwaterChamber",PassageZone="ReturningPassage",AfloatZone="NeighbourhoodAfloat";
        /// <summary>The older scenes the chapter stands its opening in (<see cref="StageApproach"/>).</summary>
        internal const string City="ManyHands",Hull="ManyHandsHull";
        public static readonly string[] Zones={SorrelZone,MooringZone,ContinuanceZone,ChamberZone,PassageZone,AfloatZone};

        /// <summary>The chapter's tracks (brief, "Music"). Three of its cues wait on the creator (docs/music/NEW_TRACKS.md cues 4 to 6, one melody family with the Coronach): until each
        /// lands, an existing track stands in, and the swap is the one line here. The Compact's theme ("Last Mooring", cue 4) stands in as Regal Alien Town, Meret's own since chapter 5;
        /// the anchor's release and the convoy run ("Room To Turn Back", cue 5) as Endless Flight, the brief's; the epilogue ("Neighbourhood Afloat", cue 6) as Hub Town Groove, home's.
        /// Last Mooring keeps the brief's Starfight until cue 4 lands. The bosses' tracks are on their definitions (Mother of Doors: Boss of the Ages; the Tollkeeper: Glitch Boss;
        /// Meret, all three phases: Final Stand); the credits' Title Theme is the credits' own.</summary>
        internal const string SecondSkyTheme="Adventure Awaits",MooringTheme="Starfight",CompactTheme="Regal Alien Town",DepartureTheme="Endless Flight",AfloatTheme="Hub Town Groove",FightTheme="Starfight";

        public static void Definitions()=>BatchTools.Run(()=>{Define();AssetDatabase.SaveAssets();Debug.Log("MOORING_DEFS_OK");});
        public static void BuildZones()=>BatchTools.Run(()=>
        {
            string arg=DevArgs.Value("-mooring-zones");var zones=string.IsNullOrEmpty(arg)?Zones:arg.Split(',',StringSplitOptions.RemoveEmptyEntries);
            Define();foreach(var zone in zones)BuildZone(zone.Trim());
            Placeholders();
            BatchTools.RegisterScenes();AssetDatabase.SaveAssets();Debug.Log("MOORING_ZONES_OK "+string.Join(",",zones));
        });
        /// <summary>Copies docs/writing/*.yarn into Resources and recompiles the Yarn project: a new .yarn file is not in the compiled program until the project reimports, and a missing
        /// node fails silently at runtime.</summary>
        public static void Dialogue()=>BatchTools.Run(()=>{RefreshDialogue();Debug.Log("MOORING_DIALOGUE_OK");});
        /// <summary>The chapter's way in, in Many Hands and its hull (<c>LastMooringBuilder.Approach.cs</c>). Re-run after either scene's own rebuild or staging (Restage.All does).
        /// Safe to run twice.</summary>
        public static void StageApproach()=>BatchTools.Run(()=>{Define();Approach.Stage();AssetDatabase.SaveAssets();Debug.Log("MOORING_APPROACH_OK");});

        /// <summary>Every node of the chapter's script (manifest section 2: 78): one that didn't compile stops the build here instead of doing nothing when talked to.</summary>
        internal static readonly string[] Nodes=
        {
            "IdraOnePlan","OrrinRoll","AbriFortiethDay","DraskBeforeTheField","SelaLastSurveyOffer","SelaSurveyHome",
            "SurvivorSecondSky","SurvivorMotherGoes","MaruSectionHeld","OrrinGateOpen",
            "IdraSpineBraced","PatrolLetsPass","MeretShipToShip","MeretGantry","MeretWell","MeretYields","IshenAnchorSequence","MeretSeesItOpen","IloCustody",
            "IloFirstName","BrynFallsBehind","BrynOnTheLine","OrrinRendezvous",
            "RuvaPassageRules","IruTernsMessage","PellAumisMessage","RuvaSennsMessage","PimQuillsMessage","MeretAccount","DraskWatches","OrrinDirectorship","SelaReturnLeg","IdraTiedUp",
            "JaskNewSky","PipRota","TamSkyGarden",
            "AbriHomecoming","TeoHomecoming","MaruHomecoming","HaskHomecoming","PellHomecoming","TovraHomecoming","OdaHomecoming","DorrowHomecoming","MiraHomecoming",
            "MiraHomeCrystals","NeveHomeParcels","OdaHomeStamp","SurvivorHomeLamps","PellHomeMothersLamp","IshenHomeShelves","BrekkHomeAwning","BinnaHomeBaskets","RuvaHomePost",
            "JaskHomeThree","TeoHomeTags","MaruHomeHarbourLamp","DovHomeSmokehouse","DovHomeBell","PimHomeKey","CorraHomeKettles","JaskHomeFare","IloHomeBook","HaskHomeStrays",
            "IruHomeLamps","IdraHomeCrew","RuvaHomeShoot","IshenHomeMoons","PellHomeOriel","TovraHomePikes","AbriHomeGlass","BrekkHomeKettle","PipHomeFile","MiraHomeTable",
            "HalHomeBenches","NeveHomeSocks","TamHomeFrame",
            "JunLastLine",
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
        /// <summary>A chapter 10 zone that is defined and has no scene yet stands as a placeholder (D270), so the build list holds every ZoneDef and no other chapter's shared test goes red
        /// on a zone that can't be loaded: a floor or an open plane, its Arrival, its walls, its light, nothing else, logged <c>MOORING_ZONE_PLACEHOLDER</c>. Only where no scene exists,
        /// so a zone's own build always replaces it and a built zone is never touched.</summary>
        static void Placeholders()
        {
            foreach(var zone in Zones)
            {
                if(File.Exists("Assets/_Project/Scenes/"+zone+".unity"))continue;
                var def=Find<ZoneDef>(zone);if(def==null)continue;bool flight=def.kind==ZoneKind.SpaceCombat||def.kind==ZoneKind.SpaceSafe;
                if(!ZoneLook.Has(zone))ZoneLook.Register(zone,new ZoneLook.Spec{sun=C(1,.95f,.88f),sunIntensity=1.2f,sky=C(.46f,.48f,.56f),equator=C(.4f,.4f,.44f),ground=C(.2f,.2f,.22f),contrast=8,saturation=4,vignette=.2f});
                var scene=WorldBuilder.Begin(zone);
                if(flight){var kit=ZoneKit.Flight(zone);WorldBuilder.Spawn("Arrival",new Vector3(0,ZoneKit.FlightPlane,0));kit.FlightLimits(-30,30,-20,40);}
                else
                {
                    var kit=ZoneKit.Ground(zone,_=>0,3);
                    WorldBuilder.Piece(ZoneKit.Kit("DeckFloor","LandingPad"),new Vector3(0,-.25f,10),new Vector3(40,.5f,40));
                    foreach(var (at,size) in new[]{(new Vector3(-20,2,10),new Vector3(1,4,40)),(new Vector3(20,2,10),new Vector3(1,4,40)),(new Vector3(0,2,-10),new Vector3(40,4,1)),(new Vector3(0,2,30),new Vector3(40,4,1))})
                        WorldBuilder.Boundary("Placeholder limit",at,size);
                    WorldBuilder.Spawn("Arrival",Vector3.zero);
                    kit.BakeNavigation(new Bounds(new Vector3(0,2,10),new Vector3(44,10,44)),Vector2.zero,new[]{new Vector2(4,14)});
                }
                WorldBuilder.Save(scene,zone);
                ZoneLook.Configure(zone,ZoneLook.SceneVolume(zone).sharedProfile);EditorUtility.SetDirty(ZoneLook.SceneVolume(zone).sharedProfile);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                Debug.LogWarning("MOORING_ZONE_PLACEHOLDER "+zone+": not built by its own engineer yet; an empty loadable scene stands in its place until "+zone+"'s own build replaces it");
            }
        }
        /// <summary>One build call per zone, each in its owner's file: it returns false while its build isn't written, and the zone keeps (or gets) its placeholder.</summary>
        static void BuildZone(string zone)
        {
            bool built=zone switch
            {
                SorrelZone=>Sorrel.Build(),MooringZone=>Mooring.Build(),ContinuanceZone=>Continuance.Build(),ChamberZone=>Chamber.Build(),PassageZone=>Passage.Build(),AfloatZone=>Neighbourhood.Build(),
                _=>throw new InvalidOperationException("Unknown chapter 10 zone "+zone),
            };
            if(!built)Debug.LogWarning("MOORING_ZONE_UNBUILT "+zone+": its build (LastMooringBuilder."+zone+") isn't written yet");
        }

        // ---------------------------------------------------------------- shared helpers (every file of the chapter may use these)
        internal static T Find<T>(string id)where T:ScriptableObject=>ZoneKit.Find<T>(id);
        internal static Objective Step(ObjectiveKind kind,string target,int count=1)=>new(){kind=kind,target=target,count=count};
        internal static QuestTarget At(string scene,string speaker=null,string report=null,string encounter=null,string brief=null,params string[] via)=>new(){scene=scene,speaker=speaker,report=report,encounter=encounter,brief=brief,via=via};
        internal static ShopOffer Sell(string id,int price,int lot=0,string requires="")=>new(){id=id,price=price,lot=lot,requires=requires};
        /// <summary>The first of these bodies that has been taken in.</summary>
        internal static GameObject FirstBody(params string[] ids)=>ids.Select(id=>ZoneKit.Prefab("Characters",id)).FirstOrDefault(body=>body!=null);
        /// <summary>An enemy of the chapter's roster if the combat engineers have defined it, else a stand-in that fights the same way, said in the log so nobody takes it for the real one.</summary>
        internal static string Foe(string id,string standIn)
        {
            if(Find<EnemyDef>(id)!=null)return id;
            Debug.LogWarning("MOORING_FOE_STANDIN "+id+" -> "+(standIn??"nothing")+": its EnemyDef isn't defined yet (LastMooringBuilder.Enemies.cs, .Mother.cs, .Tollkeeper.cs, .Meret.cs)");return standIn;
        }
        static Color C(float r,float g,float b)=>new(r,g,b,1);

        // ---------------------------------------------------------------- definitions
        static void Define()
        {
            var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");
            // No tilt anywhere (D060). What blooms in each: the clamp's cyan works and the young's glow at Sorrel; the chains' lamps and the convoy's lit windows at Last Mooring; the
            // Compact's pale cyan lights aboard; the lantern chain in the passage; lamps and windows in the neighbourhood. Each place's engineer tunes its own.
            var secondSky=ArenaBuilder.Asset<Hd2dProfile>("SecondSky",p=>{p.tint=new Color(.97f,.98f,1);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.68f;});
            var mooring=ArenaBuilder.Asset<Hd2dProfile>("Mooring",p=>{p.tint=new Color(.98f,.98f,1);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.7f;});
            var compact=ArenaBuilder.Asset<Hd2dProfile>("Compact",p=>{p.tint=new Color(.97f,.99f,1);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.55f;});
            var passage=ArenaBuilder.Asset<Hd2dProfile>("Passage",p=>{p.tint=new Color(1,.98f,.96f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.75f;});
            var afloat=ArenaBuilder.Asset<Hd2dProfile>("Afloat",p=>{p.tint=new Color(1,.96f,.9f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.5f;});
            var halo=ZoneKit.WorldMaterial("halo-sky");var source=ZoneKit.WorldMaterial("tallow-sky");
            // Sorrel's own sky (chapter 1's halo-sky), turned and cooled: the same moon's skyline under something that wasn't there before. The others start from the panorama every
            // later chapter turns; an unfamiliar sky over the neighbourhood is a part of it no earlier zone shows. Each place's engineer turns and tints their own.
            var sorrelSky=ZoneKit.Sky("sorrel-second-sky",halo,new Color(.46f,.5f,.6f,.5f),32,.8f);
            var mooringSky=ZoneKit.Sky("last-mooring-sky",source,new Color(.42f,.46f,.54f,.5f),96,.7f);
            var continuanceSky=ZoneKit.Sky("continuance-sky",source,new Color(.46f,.52f,.56f,.5f),262,.75f);
            var passageSky=ZoneKit.Sky("returning-passage-sky",source,new Color(.5f,.48f,.56f,.5f),170,.9f);
            var afloatSky=ZoneKit.Sky("neighbourhood-afloat-sky",source,new Color(.62f,.55f,.5f,.5f),12,1.15f);
            // Enemy damage is authored at its real weight (the fights' own files), so every zone's threat is 1.
            ArenaBuilder.Asset<ZoneDef>(SorrelZone,z=>{z.id=z.scene=SorrelZone;z.displayName="Sorrel's Second Sky";z.chapter=10;z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=secondSky;
                z.skybox=sorrelSky;z.enemyThreat=1;z.flightPlane=1;z.cameraClearsScenery=false;z.musicId=SecondSkyTheme;z.fightMusicId=FightTheme;SorrelDefinition(z);});
            ArenaBuilder.Asset<ZoneDef>(MooringZone,z=>{z.id=z.scene=MooringZone;z.displayName="Last Mooring";z.chapter=10;z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=mooring;
                z.skybox=mooringSky;z.enemyThreat=1;z.flightPlane=1;z.cameraClearsScenery=false;z.musicId=MooringTheme;MooringDefinition(z);});
            ArenaBuilder.Asset<ZoneDef>(ContinuanceZone,z=>{z.id=z.scene=ContinuanceZone;z.displayName="The Continuance";z.chapter=10;z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=compact;
                z.skybox=continuanceSky;z.enemyThreat=1;z.cameraClearsScenery=false;z.musicId=CompactTheme;z.fightMusicId=FightTheme;ContinuanceDefinition(z);});
            ArenaBuilder.Asset<ZoneDef>(ChamberZone,z=>{z.id=z.scene=ChamberZone;z.displayName="The Anchor Chamber";z.chapter=10;z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=compact;
                z.skybox=continuanceSky;z.enemyThreat=1;z.cameraClearsScenery=false;z.musicId=CompactTheme;ChamberDefinition(z);});
            ArenaBuilder.Asset<ZoneDef>(PassageZone,z=>{z.id=z.scene=PassageZone;z.displayName="The Returning Passage";z.chapter=10;z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=passage;
                z.skybox=passageSky;z.enemyThreat=1;z.flightPlane=1;z.cameraClearsScenery=false;z.musicId=DepartureTheme;z.fightMusicId=FightTheme;PassageDefinition(z);});
            ArenaBuilder.Asset<ZoneDef>(AfloatZone,z=>{z.id=z.scene=AfloatZone;z.displayName="Neighbourhood Afloat";z.chapter=10;z.kind=ZoneKind.GroundSafe;z.cameraProfile=ground;z.hd2dProfile=afloat;
                z.skybox=afloatSky;z.enemyThreat=1;z.cameraClearsScenery=false;z.musicId=AfloatTheme;NeighbourhoodDefinition(z);});

            Enemies.Define();
            Mother.Define();
            Tollkeeper.Define();
            Meret.Define();
            People();
            Quests();
            Chart();
        }

        /// <summary>Nobody new speaks (manifest section 5): every one of the thirty-three has a definition and a portrait set from an earlier chapter, and Meret's has the two expressions
        /// she uses for the first time (Determined, Shocked). The people named and never speaking (the navigator with the pencil, Kott and Sibb, Ket) are bodies, not definitions.
        /// Kept as the place a later need goes: a definition made here runs after every earlier chapter's, so it must only add.</summary>
        static void People(){}

        /// <summary>The three quests, step for step as the manifest has them (section 4). Rewards are materials, parts and scrip, never a capped consumable (CHAPTER_GUIDE 0.1). Where each
        /// step's thing stands, and who places it, is docs/chapters/ch10-wiring.md, sections 2 to 4: every name a step target uses below must exist under that name in its zone.</summary>
        static void Quests()
        {
            ArenaBuilder.Asset<QuestDef>("TurnBack",q=>{q.id="TurnBack";q.title="A Route With Room To Turn Back";q.main=true;q.chapter=10;
                q.description="Every road out runs through one passage. Stillwater's anchor is closing on it at Sorrel, over the nursery, and the whole convoy is going through together or not at all.";
                q.steps=new[]{Step(ObjectiveKind.Flag,"end.planSaid"),Step(ObjectiveKind.Reach,SorrelZone),Step(ObjectiveKind.Flag,"end.motherFreed"),Step(ObjectiveKind.Reach,MooringZone),
                    Step(ObjectiveKind.Flag,"end.gateOpen"),Step(ObjectiveKind.Flag,"end.boarded"),Step(ObjectiveKind.Flag,"end.meretShip"),Step(ObjectiveKind.Flag,"end.meretGantry"),
                    Step(ObjectiveKind.Flag,"end.crewsOrdered"),Step(ObjectiveKind.Flag,"end.anchorReleased"),Step(ObjectiveKind.Flag,"end.passageRun"),Step(ObjectiveKind.Flag,"end.tugFetched"),
                    Step(ObjectiveKind.Flag,"end.rendezvous"),Step(ObjectiveKind.Flag,"end.handrail"),Step(ObjectiveKind.Flag,"game.complete")};
                q.stepText=new[]{"Hear the plan, and who is doing which part of it","Go back to the first moon","Open the four clamps on Mother of Doors: take the weight, wait for the slack",
                    "Bring the convoy up to Last Mooring","Break the Tollkeeper's braces, and stop its gate motor on foot","Go up the Continuance's boarding spine",
                    "Put out the three shield projectors on the director's craft","Open the two arm couplings on her frame","Answer her in the control well",
                    "Take the last collar: one hold, on Idra's call","Run the passage with the convoy","Go back up the lane for the tug Welcome Back","Bring her through to the rendezvous",
                    "See the new neighbourhood, and then Orrin","Mend the handrail on the garden stair, and repaint the line"};
                // Each step's arrow, as the wiring's section 4 has it. Every via runs back along the convoy's road to Many Hands' hull, so from anywhere behind the party the arrow finds
                // the way on, and while a place has never been stood in the chart's diamond stands on Many Hands (its stop reaches the hull).
                string[] toSorrel={Hull},toMooring={SorrelZone,Hull},toShip={MooringZone,SorrelZone,Hull},toChamber={ContinuanceZone,MooringZone,SorrelZone,Hull};
                q.stepTargets=new[]{At(City,"Orrin"),At(SorrelZone,null,null,null,null,toSorrel),At(SorrelZone,null,"Clamp",null,null,toSorrel),At(MooringZone,null,null,null,null,toMooring),
                    At(MooringZone,null,null,"Tollkeeper",null,toMooring),At(ContinuanceZone,null,"Spine",null,null,toShip),At(ContinuanceZone,null,null,"MeretShip",null,toShip),
                    At(ChamberZone,null,null,"MeretGantry",null,toChamber),At(ChamberZone,null,null,"MeretWell",null,toChamber),At(ChamberZone,null,"LastCollar",null,null,toChamber),
                    At(PassageZone,null,"FarEnd"),At(PassageZone,null,"TugLine","Passage_Tug"),At(PassageZone,null,"FarMouth"),At(AfloatZone,"Orrin"),At(AfloatZone,null,"Handrail")};
                // After the last scene the credits roll and the title returns (manifest section 10): a Continue from the save after it stands in the neighbourhood, where this is the line.
                q.closingLine="The convoy is through, and everyone who came is here. There is no hurry.";
                // The closing card waits for the game's last line. The manifest asks for no "chapter complete" plate at all, only the credits: that is the credits' own work (wiring,
                // section 8, "The credits"); until it lands the short plate shows after Taren's last word.
                q.cardFlag="game.complete";q.cardBody="";
                // What the story has set by its end that the steps don't say (nothing reads it after the last chapter; a later fixture would). Each zone's engineer adds their own world
                // flags on the main path here by exact edit, as the wiring's section 3 lists them.
                q.storyFlags=new[]{"end.planHeard","end.sorrelSeen","end.clamp1","end.clamp2","end.clamp3","end.clamp4","bossdown.MotherOfDoors","end.sectionStruck","end.sectionHeld","end.brace1","end.brace2","end.brace3",
                    "bossdown.Tollkeeper","end.spineTop","end.captainPassed","clear.Continuance_Works","end.meretHailed","bossdown.MeretShip","bossdown.MeretGantry","bossdown.MeretWell","end.meretYielded","end.sequenceRead","end.collarTaken",
                    "end.custody","end.namesBegun","end.farEnd","end.turnedBack","clear.Passage_Tug","end.lineOn","end.mouthReached","end.epilogue","game.complete"};
                // Reward: the ending (brief). It pays after the last scene, so it carries nothing a player could spend: the climb from Sync 27 to 30 is the fights' and the two side quests'
                // (the combat ledger, LastMooringBuilder.Enemies.cs).
                q.rewardXp=0;q.rewardScrip=0;q.rewards=Array.Empty<Ingredient>();q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("LastSurvey",q=>{q.id="LastSurvey";q.title="The Last Nursery Survey";q.chapter=10;
                q.description="The young cut a road of their own on the way out. It is narrow, it turns twice on itself, and it isn't on the way to anything.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"NurseryRoad"),Step(ObjectiveKind.Flag,"sela.surveyFiled")};
                q.stepText=new[]{"Fly the nursery's new road to its far end, and back","Put it on the route chart"};
                q.stepTargets=new[]{At(SorrelZone,null,"NurseryRoad",null,"Fly the nursery's new road",Hull),At(null,null,"SurveyHome",null,"Put it on the chart")};
                // A survey drive (a Masterwork scout's), and the prospect on the chart is the story's own (wiring, section 4).
                q.rewardXp=200;q.rewardScrip=160;q.rewards=new[]{new Ingredient{id="DrivesT5Scout",count=1}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("Homecomings",q=>{q.id="Homecomings";q.title="Homecomings";q.chapter=10;
                q.description="Nine communities came through the passage. Ruva is chairing motion nine and would like somebody else to go and see where they have all settled.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"Homecoming",9)};
                q.stepText=new[]{"Find where each of the nine has settled in the neighbourhood"};
                q.stepTargets=new[]{At(AfloatZone,null,"Homecoming",null,"Call on all nine")};
                // The game's last cache: a Masterwork emitter of the party's own luck (the one slot chapter 9's side stories didn't fill) and the home region's rivets.
                q.rewardXp=200;q.rewardScrip=600;q.rewards=new[]{new Ingredient{id="EmittersT5",count=1},new Ingredient{id="HaloRivet",count=6}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
        }

        /// <summary>Sela's chart (guide 3.10): one stop, the neighbourhood, in the last column's top slot (clear of Harrowgate's name below it by .31 and of Lastlight's beside it by .24).
        /// Charted by arriving (<c>visited.NeighbourhoodAfloat</c>). What really stands there is the town's own (<see cref="NeighbourhoodStop"/>): RouteChartTests loads the zone and
        /// looks for each service the stop lists. The operation's five places are reached from Many Hands' hull and are in no stop's reaches; the main quest's every step goes there
        /// by a via that ends at the hull, so the chart's diamond stands on Many Hands until each is stood in.</summary>
        static void Chart()
        {
            ArenaBuilder.Asset<ChartStop>(AfloatZone,s=>{s.id=AfloatZone;s.displayName="Neighbourhood Afloat";s.region="The new sky";
                s.blurb="Familiar streets under an unfamiliar sky: the Halo's market on Many Hands' arcade, the garden, the raft bay, and everyone who came.";
                s.zone=AfloatZone;s.spawn="Arrival";s.position=new Vector2(1,.97f);s.routes=new[]{"ManyHands"};s.chapter=10;s.shop=s.bench=s.repair=false;
                s.reaches=Array.Empty<string>();NeighbourhoodStop(s);});
        }
    }
}
