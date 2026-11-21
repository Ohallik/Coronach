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
    /// Chapter 9, The House That Can Leave (brief: docs/chapters/ch09-the-house-that-can-leave.md; script wiring: docs/writing/ch09-manifest.md; who places what: docs/chapters/ch09-wiring.md).
    /// Isolated builder for its definitions and six zones, on <see cref="ZoneKit"/>. Members of every file share one class, so each zone's own helpers live in its nested class:
    /// <list type="bullet">
    /// <item>Nacre Grave (space combat, <c>HomeBuilder.Nacre.cs</c>, the first engineer's): the wreck field round the broken berth. Rule: a wreck field with a spiral; the three beacons.
    /// Map brief: docs/maps/NacreGrave.md.</item>
    /// <item>The Meridian Crawl (town, <c>HomeBuilder.Crawl.cs</c>) and the Terminator (wild, the run against the dawn, <c>HomeBuilder.Terminator.cs</c>).</item>
    /// <item>The Decks on separation day (town, zone <c>DecksLeaving</c>, <c>HomeBuilder.Decks.cs</c>): a second scene, so chapter 1's Hub_Decks is not changed by one object. The Coronach
    /// walk and the launch (<c>HomeBuilder.Coronach.cs</c>) are its own engineer's, staged into that scene by the Decks build itself (<see cref="Decks"/>.Build calls it before the bake).</item>
    /// <item>The Separation Works (dungeon, the Foundry Saint's press hall, <c>HomeBuilder.Works.cs</c>) and the Broken Ring (space combat, the Debris Cutter, <c>HomeBuilder.Ring.cs</c>).</item>
    /// </list>
    /// Ordinary enemies and the flight fights are <c>HomeBuilder.Enemies.cs</c>, the two bosses <c>HomeBuilder.Bosses.cs</c>: <see cref="Define"/> calls the first and then the second, so a boss
    /// can name an enemy. Chapter 9's beats in older scenes (Harrowgate's harbour, Many Hands, the Picket's north end) are <see cref="StageApproach"/>, here.
    /// Runs: <c>Definitions</c>, <c>BuildZones</c> with <c>-home-zones NacreGrave,MeridianCrawl,Terminator,DecksLeaving,SeparationWorks,BrokenRing</c>, <c>Dialogue</c>,
    /// <c>StageApproach</c>; then <c>ZoneLook.Apply</c> for what was built.
    /// </summary>
    public static partial class HomeBuilder
    {
        internal const string NacreZone="NacreGrave",CrawlZone="MeridianCrawl",TerminatorZone="Terminator",DecksZone="DecksLeaving",WorksZone="SeparationWorks",RingZone="BrokenRing";
        /// <summary>The older scenes the chapter stands beats in (<see cref="StageApproach"/>).</summary>
        internal const string City="ManyHands",Hull="ManyHandsHull",Picket="PicketOfTeeth",Harbour="Harrowgate";
        public static readonly string[] Zones={NacreZone,CrawlZone,TerminatorZone,DecksZone,WorksZone,RingZone};
        public static void Definitions()=>BatchTools.Run(()=>{Define();AssetDatabase.SaveAssets();Debug.Log("HOME_DEFS_OK");});
        public static void BuildZones()=>BatchTools.Run(()=>
        {
            string arg=DevArgs.Value("-home-zones");var zones=string.IsNullOrEmpty(arg)?Zones:arg.Split(',',StringSplitOptions.RemoveEmptyEntries);
            Define();foreach(var zone in zones)BuildZone(zone.Trim());
            Placeholders();
            BatchTools.RegisterScenes();AssetDatabase.SaveAssets();Debug.Log("HOME_ZONES_OK "+string.Join(",",zones));
        });
        /// <summary>Copies docs/writing/*.yarn into Resources and recompiles the Yarn project: a new .yarn file is not in the compiled program until the project reimports, and a missing
        /// node fails silently at runtime.</summary>
        public static void Dialogue()=>BatchTools.Run(()=>{RefreshDialogue();Debug.Log("HOME_DIALOGUE_OK");});
        /// <summary>The chapter's beats in older scenes: Abri turning for Nacre at Harrowgate's end, the ways to Nacre off the Picket and Many Hands' stern, and Many Hands' middle beat
        /// (Meret's notice, the pencil, Maru's answer) and its last (the chart). Re-run after any of those scenes' own rebuild or staging. Safe to run twice.</summary>
        public static void StageApproach()=>BatchTools.Run(()=>{Define();Approach.Stage();AssetDatabase.SaveAssets();Debug.Log("HOME_APPROACH_OK");});

        /// <summary>Every node of the chapter's script (manifest section 2): one that didn't compile stops the build here instead of doing nothing when talked to.</summary>
        static readonly string[] Nodes=
        {
            "AbriTurnsForNacre","BrynNacreApproach","OdaFound","BrynWelcomeBack","BrynNavigators","NeveReturnOffer",
            "MeretBerthNotice","BrynChartLesson","IdraTownMovers","OdaAboard","DraskCount","AbriNamesOnTheWall",
            "DorrowFirst","DorrowRepeat","TamFirst","DorrowTreadHome",
            "OrrinHomecoming","JunFirst","JunRepeat","MiraInventoryOffer","MiraCratedRepeat","SelaWeddingTable","MiraInventoryReturn","HalCrated","HalBenchesOffer","HalBenchesReturn",
            "NeveFlyingAStreet","SurvivorDecks","SurvivorNevesLetter","NeveReturnAnswer","OdaDecks","IloDecks","DorrowListens","TamStillOffer","TamDoorMarks","TamLeaningTree","TamWornStep","TamGoesBack",
            "OrrinMeetingOpens","MeretBerthOffer","HalApprentices","MiraManifest","IloReadsNames","OdaStands","OrrinPostponed","MeretWithdraws",
            "DorrowWorks","OrrinSaintSpeaks","HalLastMachine",
            "OrrinCallsCoronach","MiraStallDoor","OrrinBoardDoor","HalBenchDoor","NeveLockDoor","OdaOfficeDoor","JunLineDoor","IloNoticeDoor","SurvivorDockDoor",
            "PellEdge","RuvaEdge","MaruEdge","HaskEdge","TovraEdge","DorrowEdge","AbriEdge","TarenWalkway","SelaGateSix","IdraNoDoor","OrrinGiveItRoom",
            "MiraDoorCrystals","SurvivorDoorSocks","SurvivorDoorBeacons","OdaDoorStamp","PellDoorMothersLamp","IshenDoorRecord","BrekkDoorWalked","BinnaDoorBaskets","RuvaDoorSenn",
            "JaskDoorQuestion","JaskDoorFare","TeoDoorTags","MaruDoorLamps","DovDoorEel","DovDoorBell","IshenDoorQuill","CorraDoorKettles","IloDoorLedger","HaskDoorStrays","IruDoorLamp",
            "IdraDoorCrew","RuvaDoorGardener","IshenDoorMoons","PellDoorOriel","TovraDoorPikes","AbriDoorLanterns","BrekkDoorKettle","PipDoorFile","MiraDoorTable","HalDoorBenches",
            "NeveDoorLetter","TamDoorEdge",
            "NeveFirstOut","BrynTowsCutter","OrrinChalk",
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
        /// <summary>A chapter 9 zone that is defined and has no scene yet stands as a placeholder, so the build list holds every ZoneDef and no other chapter's shared test goes red
        /// on a zone that can't be loaded (ContentFrameworkTests, November 13, while the chapter was paused): a floor or an open plane, its Arrival, its walls, its light, nothing
        /// else, logged <c>HOME_ZONE_PLACEHOLDER</c>. Only where no scene exists, so a zone's own build always replaces it and a built zone is never touched.</summary>
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
                Debug.LogWarning("HOME_ZONE_PLACEHOLDER "+zone+": not built by its own engineer yet; an empty loadable scene stands in its place until "+zone+"'s own build replaces it");
            }
        }
        static void BuildZone(string zone)
        {
            switch(zone)
            {
                case NacreZone:Nacre.Build();break;
                case CrawlZone:Crawl.Build();break;
                case TerminatorZone:Terminator.Build();break;
                // The Coronach and the launch are stood in the Decks' own build, before its bake and its ground pass, so their people and props are in both.
                case DecksZone:Decks.Build(Coronach.Stage);break;
                case WorksZone:Works.Build();break;
                case RingZone:Ring.Build();break;
                default:throw new InvalidOperationException("Unknown chapter 9 zone "+zone);
            }
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
            Debug.LogWarning("HOME_FOE_STANDIN "+id+" -> "+(standIn??"nothing")+": its EnemyDef isn't defined yet (HomeBuilder.Enemies.cs / HomeBuilder.Bosses.cs)");return standIn;
        }
        static Color C(float r,float g,float b)=>new(r,g,b,1);

        // ---------------------------------------------------------------- definitions
        static void Define()
        {
            var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");
            // Nacre Grave: the beacons' lamps, the tracks' markers and the Choir's bruise beyond are what blooms; the wrecks' family colours stay true under a cool white key. No tilt (D060).
            var nacre=ArenaBuilder.Asset<Hd2dProfile>("Nacre",p=>{p.tint=new Color(.98f,.97f,1);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.7f;});
            // Duskline: a sun that never clears the horizon, so everything is lit low and warm from one side and cold from the other.
            var dusk=ArenaBuilder.Asset<Hd2dProfile>("Duskline",p=>{p.tint=new Color(1,.95f,.9f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.6f;});
            // The Broken Ring: the districts' lit windows and the Crawl-rigged engines are what blooms.
            var ring=ArenaBuilder.Asset<Hd2dProfile>("Ring",p=>{p.tint=new Color(.98f,.98f,1);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.72f;});
            var decks=Find<Hd2dProfile>("DeckSoft");var cave=Find<Hd2dProfile>("Cave");
            var source=ZoneKit.WorldMaterial("tallow-sky");
            // Nacre's panorama turned to a part no earlier lane shows and dulled to a steel dark: tinted toward rose and violet it was one saturated purple behind every frame, and the
            // Choir's passage is its own light under the field (HomeBuilder.Nacre's bruise), not the sky's.
            var nacreSky=ZoneKit.Sky("nacre-grave-sky",source,new Color(.4f,.43f,.5f,.5f),143,.5f);
            // Over Duskline the sky is the horizon's band: amber low, blue-black high.
            var duskSky=ZoneKit.Sky("duskline-sky",source,new Color(.7f,.56f,.46f,.5f),318,1.3f);
            // The Halo's own space, as chapter 1 knew it, cooler: the Ring is home's sky with the station coming apart in it.
            var ringSky=ZoneKit.Sky("broken-ring-sky",source,new Color(.5f,.54f,.62f,.5f),205,.85f);
            var halo=ZoneKit.WorldMaterial("halo-sky");
            // Enemy damage is authored at its real weight (HomeBuilder.Enemies.cs, HomeBuilder.Bosses.cs), so every zone's threat is 1.
            ArenaBuilder.Asset<ZoneDef>(NacreZone,z=>{z.id=z.scene=NacreZone;z.displayName="Nacre Grave";z.chapter=9;z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=nacre;z.skybox=nacreSky;
                z.enemyThreat=1;z.flightPlane=1;z.cameraClearsScenery=false;z.musicId="Endless Flight";z.fightMusicId="Starfight";NacreDefinition(z);});
            ArenaBuilder.Asset<ZoneDef>(CrawlZone,z=>{z.id=z.scene=CrawlZone;z.displayName="The Meridian Crawl";z.chapter=9;z.kind=ZoneKind.GroundSafe;z.cameraProfile=ground;z.hd2dProfile=dusk;z.skybox=duskSky;
                z.enemyThreat=1;z.cameraClearsScenery=true;z.musicId="Dventure at Dusk";z.shopMusicId="Shop Theme";CrawlDefinition(z);});
            ArenaBuilder.Asset<ZoneDef>(TerminatorZone,z=>{z.id=z.scene=TerminatorZone;z.displayName="The Terminator";z.chapter=9;z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=dusk;z.skybox=duskSky;
                z.enemyThreat=1;z.cameraClearsScenery=true;z.musicId="Dventure at Dusk";z.fightMusicId="Starfight";TerminatorDefinition(z);});
            ArenaBuilder.Asset<ZoneDef>(DecksZone,z=>{z.id=z.scene=DecksZone;z.displayName="The Decks";z.chapter=9;z.kind=ZoneKind.GroundSafe;z.cameraProfile=ground;z.hd2dProfile=decks;z.skybox=halo;
                z.enemyThreat=1;z.cameraClearsScenery=false;z.musicId="Hub Town Groove";DecksDefinition(z);});
            ArenaBuilder.Asset<ZoneDef>(WorksZone,z=>{z.id=z.scene=WorksZone;z.displayName="The Separation Works";z.chapter=9;z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=cave;z.skybox=null;
                z.enemyThreat=1;z.cameraClearsScenery=true;z.musicId="Moon Caverns";WorksDefinition(z);});
            ArenaBuilder.Asset<ZoneDef>(RingZone,z=>{z.id=z.scene=RingZone;z.displayName="The Broken Ring";z.chapter=9;z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=ring;z.skybox=ringSky;
                z.enemyThreat=1;z.flightPlane=1;z.cameraClearsScenery=false;z.musicId="Starfight";RingDefinition(z);});

            Enemies.Define();
            Bosses.Define();
            People();
            Quests();
            Chart();
        }

        /// <summary>The new speakers (manifest section 5) and Bryn, who chapter 8 defined to be heard and this chapter stands in the world. Everyone else keeps what they have.</summary>
        static void People()
        {
            void Person(string id,string display,bool female,GameObject body)
                =>ArenaBuilder.Asset<CharacterDef>(id,c=>{c.id=c.speakerId=id;c.displayName=display;c.female=female;c.natural=body;
                    // The sheets were taken in before these definitions existed, so PortraitIntake had nothing to bind them to.
                    if(c.portraitNatural==null)c.portraitNatural=AssetDatabase.LoadAssetAtPath<PortraitSet>("Assets/_Project/Resources/Portraits/"+id+"_Natural.asset");});
            Person("Dorrow","Chief Engineer Dorrow",false,FirstBody("DorrowBody","Hal"));
            // Tam is never "he" or "she" in the script (manifest section 9); the flag only picks which pool of generic sounds a body uses.
            Person("Tam","Tam",false,FirstBody("TamBody","DorrowBody","Hal"));
            Person("Jun","Jun",true,FirstBody("JunBody","Mira"));
            Person("Bryn","Bryn Asta",true,FirstBody("BrynBody","CorraBody"));
            // Oda has spoken since chapter 1 with no definition (her portrait is found by name, her hail had no body): the chapter stands her in Many Hands and on the Decks, in her own body.
            Person("Oda","Oda",true,FirstBody("OdaBody","CorraBody"));
        }

        /// <summary>The five quests, step for step as the script's manifest has them (section 4). Rewards are materials, parts and scrip, never a capped consumable (CHAPTER_GUIDE 0.1), and
        /// every card names exactly what is paid. Where each step's thing stands, and who places it, is docs/chapters/ch09-wiring.md.</summary>
        static void Quests()
        {
            ArenaBuilder.Asset<QuestDef>("HouseLeaves",q=>{q.id="HouseLeaves";q.title="The House That Can Leave";q.main=true;q.chapter=9;
                q.description="The Nacre berth is gone and Stillwater is closing early. Cinder Halo has been offered a place in the corridor, if it leaves its neighbours outside.";
                q.steps=new[]{Step(ObjectiveKind.Reach,NacreZone),Step(ObjectiveKind.Flag,"home.odaFound"),Step(ObjectiveKind.Flag,"home.tugFound"),Step(ObjectiveKind.Flag,"home.navigatorsFound"),
                    Step(ObjectiveKind.Flag,"home.crawlNamed"),Step(ObjectiveKind.Reach,CrawlZone),Step(ObjectiveKind.Flag,"home.treadRecovered"),Step(ObjectiveKind.Flag,"home.returned"),
                    Step(ObjectiveKind.Flag,"home.meetingHeld"),Step(ObjectiveKind.Interact,"Joint",3),Step(ObjectiveKind.Flag,"home.saintReleased"),Step(ObjectiveKind.Flag,"home.halChose"),
                    Step(ObjectiveKind.Flag,"home.coronachWalked"),Step(ObjectiveKind.Flag,"home.cutterDisabled"),Step(ObjectiveKind.Flag,"home.chartRead")};
                q.stepText=new[]{"Take the lane to Nacre","Find the hauler on line nine hundred and four","Find the tug Welcome Back","Bring in the navigators' pod",
                    "Go back to Many Hands, and find somebody who has moved a town","Catch up with the Meridian Crawl on Duskline","Re-mate three couplings and bring the tread-hall in ahead of the dawn",
                    "Go home","Attend the town meeting","Reopen the separation joints under the promenade","Release the Foundry Saint's arms, and take the override off its back",
                    "Hold the lamp for Hal","Walk the town. There is no hurry.","Keep the Debris Cutter off the districts as they lift","See where every road out now runs"};
                // From Harrowgate the arrow is the quay's launch to the Picket and then the warp at its north end; from Many Hands the hull's warp off the stern. Steps 1 to 3 are each
                // beacon's own point in the field (docs/maps/NacreGrave.md); the rest are each zone's, as the wiring table has them.
                string[] toNacre={Picket,Hull};
                q.stepTargets=new[]{At(NacreZone,null,null,null,null,toNacre),At(NacreZone,null,"OdaHauler",null,null,toNacre),At(NacreZone,null,"WelcomeBack",null,null,toNacre),
                    At(NacreZone,null,"NavigatorsPod",null,null,toNacre),At(City,null,"TownMovers",null,null,Hull),At(CrawlZone),At(TerminatorZone,null,null,null,null,CrawlZone),
                    At(DecksZone,"Orrin"),At(DecksZone,null,"Meeting"),At(WorksZone,null,"Joint",null,null,DecksZone),At(WorksZone,null,null,"FoundrySaint",null,DecksZone),
                    At(DecksZone,"Hal",null,null,null,WorksZone),At(DecksZone),At(RingZone,null,null,"DebrisCutter",null,DecksZone),At(City,null,"FleetChart",null,null,Hull)};
                // Chapter 10 begins at the same table (LastMooringBuilder.Approach: Idra's one plan, report OnePlan); the arrow and the chart's diamond follow q.next there (D273).
                q.closingLine="Chapter 9 complete. The Halo is a fleet, and every road out runs past Sorrel: hear Idra's plan at the chart table.";
                q.next=At(City,null,"OnePlan",null,null,Hull);
                // The chapter's card waits for the chart, the chapter's last talk, which sets this flag; its body is two lines of the card and no more (the manifest's was twice that).
                q.cardFlag="chapter9.complete";
                q.cardBody="Cinder Halo turned down a berth that left its neighbours in the footnotes. It walked its own streets, said what each door had done, and came apart on purpose. Every road out now runs past Sorrel.";
                // What the story has set by its end that the steps above don't say: the world's own events on the main path, who has been met and heard, the fights. Chapter 10's fixture
                // starts with these (DevLoadout). Each zone's own are as its map doc and the wiring table give them; an engineer whose zone sets another on the main path adds it here.
                q.storyFlags=new[]{"home.turnedForNacre","home.nacreReached","home.nacreSpoken","home.nacreDone","home.noticeHeard","home.chartLent",
                    "home.crawlReached","home.crawlAsked","met.Dorrow","home.coupling1","home.coupling2","home.coupling3","home.rideDone",
                    "home.meetingOpened","home.offerMade","home.halAsked","home.manifestShown","home.namesRead","home.odaStood","home.orrinSaid","home.berthRefused",
                    "home.worksRead","home.joint1","home.joint2","home.joint3","bossdown.FoundrySaint",
                    "home.coronachCalled","coronach.mira","coronach.orrin","coronach.hal","coronach.neve","coronach.oda","coronach.jun","coronach.ilo","coronach.doorsHeard",
                    "coronach.taren","coronach.sela","coronach.idra","coronach.lastHeard","home.launched","bossdown.DebrisCutter","chapter9.complete"};
                // A Masterwork frame of the party's own luck (anyone wears a frame) and the region's two commons. The XP is the chapter's ledger's (HomeBuilder.Enemies.cs): 1,000 for the main
                // quest and 800 for the four side quests between them, as chapter 8's.
                q.rewardXp=1000;q.rewardScrip=1400;q.rewards=new[]{new Ingredient{id="FramesT5",count=1},new Ingredient{id="HaloRivet",count=4},new Ingredient{id="NacreShard",count=4}};
                q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("SecondInventory",q=>{q.id="SecondInventory";q.title="Mira's Second Inventory";q.chapter=9;
                q.description="Mira's evacuation stock is forty-one lots of other people's belongings and nine of her own. Three of the lots have two owners each.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"Consignment",3),Step(ObjectiveKind.Flag,"mira.inventorySettled")};
                q.stepText=new[]{"Find the three disputed lots: at Nacre, on the Crawl, and under the promenade","Tell Mira what each owner decided"};
                q.stepTargets=new[]{At(null,null,"Consignment",null,"Find the disputed lots"),At(DecksZone,"Mira",null,null,"Tell Mira")};
                // "It's from my nine": a Masterwork module.
                q.rewardXp=200;q.rewardScrip=160;q.rewards=new[]{new Ingredient{id="ModulesT5",count=1}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("PortableBenches",q=>{q.id="PortableBenches";q.title="Hal's Apprentices";q.chapter=9;
                q.description="Hal has nine apprentices and a workshop that won't go through a hatch. He has made three benches that fold.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"PortableBench",3),Step(ObjectiveKind.Flag,"hal.benchesLit")};
                q.stepText=new[]{"Carry a bench to Ket on Many Hands, Lo on the tread-hall and Marn at the tug dock","Tell Hal they're lit"};
                q.stepTargets=new[]{At(null,null,"PortableBench",null,"Carry the benches"),At(DecksZone,"Hal",null,null,"Tell Hal")};
                // "Ket made it": a Masterwork edge.
                q.rewardXp=200;q.rewardScrip=160;q.rewards=new[]{new Ingredient{id="EdgesT5",count=1}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("ReturnAddress",q=>{q.id="ReturnAddress";q.title="Neve's Return Address";q.chapter=9;
                q.description="Neve is still a courier, with three parcels and no lanes left to run them down. The third is her own.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"TugParcel"),Step(ObjectiveKind.Interact,"CrawlParcel"),Step(ObjectiveKind.Flag,"dace.letterRead"),Step(ObjectiveKind.Flag,"neve.letterAnswered")};
                q.stepText=new[]{"Take the first parcel to the tug Welcome Back","Take the second to the town that walks","Find where the third one's address has gone","Tell Neve it arrived"};
                q.stepTargets=new[]{At(NacreZone,null,"TugParcel",null,"Parcel for the tug"),At(CrawlZone,null,"CrawlParcel",null,"Parcel for the Crawl"),
                    At(DecksZone,"Survivor",null,null,"Find who the third is for"),At(DecksZone,"Neve",null,null,"Tell Neve")};
                // "Take this drive": a Masterwork drive.
                q.rewardXp=200;q.rewardScrip=160;q.rewards=new[]{new Ingredient{id="DrivesT5",count=1}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("StillGround",q=>{q.id="StillGround";q.title="Still Ground";q.chapter=9;
                q.description="Dorrow's apprentice has never stood on a floor that stays where it is, and has been sick twice.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"StillThing",3),Step(ObjectiveKind.Flag,"tam.goingBack")};
                q.stepText=new[]{"Show Tam a doorframe, a pear tree and a stair","Hear what Tam has decided"};
                q.stepTargets=new[]{At(DecksZone,null,"StillThing",null,"Show Tam still things"),At(DecksZone,"Tam",null,null,"Find Tam")};
                // "A token": the region's rare, the Ring Keystone, and a little scrip.
                q.rewardXp=200;q.rewardScrip=160;q.rewards=new[]{new Ingredient{id="RingKeystone",count=1}};q.flagsOnComplete=Array.Empty<string>();});
        }

        /// <summary>Sela's chart (guide 3.10). The Crawl is charted by Idra's word in Many Hands (manifest step 5: "from the chart"), not by arriving, because the chart is the only way there;
        /// it is the chapter's slot in the last column. The Halo on the day is a stop of its own beside chapter 1's Decks (one stop per zone), shut once its districts have flown. Nacre Grave,
        /// the Terminator, the Works and the Ring are reached from these and are in their reaches.</summary>
        static void Chart()
        {
            ArenaBuilder.Asset<ChartStop>(CrawlZone,s=>{s.id=CrawlZone;s.displayName="The Meridian Crawl";s.region="Duskline";
                s.blurb="A town on treads that has never stopped, walking the twilight between the day and the night: Dorrow's helm, the market street and the sky-garden.";
                s.zone=CrawlZone;s.spawn="Arrival";s.unlockFlag="home.crawlNamed";s.position=new Vector2(1,.21f);s.routes=new[]{"ManyHands"};s.chapter=9;
                s.reaches=new[]{TerminatorZone};CrawlStop(s);});
            ArenaBuilder.Asset<ChartStop>(DecksZone,s=>{s.id=DecksZone;s.displayName="Cinder Halo";s.region="Home";
                s.blurb="The Decks on separation day: crates on the promenade, chalk on the joints, Mira's stall and Hal's workshop, and the way under the promenade.";
                s.zone=DecksZone;s.spawn="Arrival";s.position=new Vector2(.03f,.98f);s.routes=new[]{CrawlZone};s.chapter=9;
                s.lockedFlag="home.launched";s.lockedReason="Cinder Halo came apart on purpose: its districts fly with the convoy now.";
                s.reaches=new[]{WorksZone,RingZone};DecksStop(s);});
        }
    }
}
