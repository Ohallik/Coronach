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
    /// Chapter 5, Many Hands (brief: docs/chapters/ch05-many-hands.md; script wiring: docs/writing/ch05-manifest.md). Isolated builder for its definitions and two zones, on
    /// <see cref="ZoneKit"/>:
    /// <list type="bullet">
    /// <item>Many Hands (town and hub, <c>ManyHandsBuilder.City.cs</c>): one ship lashed together from Tallow's cabins, Xylos's seed carriers and Driftreed's rafts, six districts on one
    /// deck plan round a crossing, the lamp gallery up a stair at the back. Its freight hall is where Hulljack's second part is fought (docs/maps/ManyHands.md, "Freight hall contract").</item>
    /// <item>The Hull of Many Hands (flight arena, <c>ManyHandsBuilder.Hull.cs</c>): the second world engineer's.</item>
    /// </list>
    /// Hulljack and Dessik Varn are in <c>ManyHandsBuilder.Enemies.cs</c> (the combat engineer's). No kit was ordered for this city: it is built from earlier chapters' kits, free regrades
    /// and what ZoneKit makes. Members of the three files share one class, so each zone's own helpers live in a nested class or carry its name.
    /// Runs: <c>Definitions</c>, <c>BuildZones</c> with <c>-manyhands-zones ManyHands,ManyHandsHull</c>, <c>Dialogue</c>, <c>StageApproach</c>; then <c>ZoneLook.Apply</c> for what was built.
    /// </summary>
    public static partial class ManyHandsBuilder
    {
        internal const string City="ManyHands",Hull="ManyHandsHull";
        public static readonly string[] Zones={City,Hull};
        public static void Definitions()=>BatchTools.Run(()=>{Define();AssetDatabase.SaveAssets();Debug.Log("MANYHANDS_DEFS_OK");});
        public static void BuildZones()=>BatchTools.Run(()=>
        {
            string arg=DevArgs.Value("-manyhands-zones");var zones=string.IsNullOrEmpty(arg)?Zones:arg.Split(',',StringSplitOptions.RemoveEmptyEntries);
            Define();foreach(var zone in zones)BuildZone(zone.Trim());
            BatchTools.RegisterScenes();AssetDatabase.SaveAssets();Debug.Log("MANYHANDS_ZONES_OK "+string.Join(",",zones));
        });
        [Restage.Step("chapter5.tallow-dock","TallowApproach",After="chapter4.tallow-warp",Chapter=5,Leaves=new[]{"TallowApproach: Dock — Many Hands <WarpBeacon> #1","TallowApproach: Many Hands is moored off Tallow <FlagGate> #1","TallowApproach: Spawn_ManyHands #1"})]
        public static void StageApproach()=>BatchTools.Run(()=>{StageTallowDock();AssetDatabase.SaveAssets();Debug.Log("MANYHANDS_APPROACH_OK");});
        /// <summary>Copies docs/writing/*.yarn into Resources and recompiles the Yarn project: a new .yarn file is not in the compiled program until the project reimports, and a missing
        /// node fails silently at runtime.</summary>
        public static void Dialogue()=>BatchTools.Run(()=>{RefreshDialogue();Debug.Log("MANYHANDS_DIALOGUE_OK");});
        /// <summary>Every node of the chapter's script (manifest section 2): one that didn't compile stops the build here instead of doing nothing when talked to.</summary>
        static readonly string[] Nodes=
        {
            "AbriHandsArrival","SelaDockDoor","AbriHandsRepeat","AbriCourier","AbriDinnerTalk","AbriHandsPost","PellChartLamps","PellHandsRepeat","PellHandsPost","SelaChartOpen",
            "RuvaMooring","MaruMooring","SelaMooringRail","RuvaMooringLost","MaruMooringLost","RuvaHandsRepeat","RuvaHandsPost","MaruHandsRepeat","MaruHandsPost",
            "BinnaVent","BinnaHandsRepeat","BinnaHandsPost","TeoHands","TeoHandsRepeat","TeoHandsPost","TeoDinnerTalk",
            "JaskFound","JaskFoundRepeat","TeoStowawayOffer","RuvaStowaway","TeoStowawayDecide","JaskThanksGarden","JaskThanksFerry","JaskThanksLamps","JaskGardenHand","JaskFerryHand","JaskLampHand",
            "JaskDinnerTalk","JaskHandsPost","HaddaHands","HaddaHandsRepeat","HaddaHandsPost","CorraHands","CorraHandsRepeat","CorraKettleOffer","GaroKettle","OnaKettle","AbriKettle",
            "CorraKettleReturn","CorraHandsPost","DovHands","DovHandsRepeat","DovHandsPost","OnaHands","OnaHandsRepeat","OnaHandsPost","PimHands","PimHandsRepeat","PimHandsPost",
            "GaroHands","GaroHandsRepeat","GaroHandsPost","DabbHands","DabbHandsRepeat","DabbHandsPost","IloArrives","IloHandsRepeat","IloHandsPost",
            "IloLedgerOffer","TeoLedger","MaruLedger","AbriLedger","IloLedgerReturn","DessikClaim","DessikHall","DessikYields","DessikHeld","DessikHeldPost",
            "PatrolBreachSealed","PatrolRepeat","MeretArrives","BinnaDinner","BinnaDinnerTalk","MaruDinnerTalk","MeretStillwater","RuvaDinnerNumber","IloFootnotes","MeretSorrel",
            "MeretDrawings","MeretParting","SelaLampGallery",
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
                case City:BuildCity();break;
                case Hull:BuildHull();break;
                default:throw new InvalidOperationException("Unknown Many Hands zone "+zone);
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
            var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");
            // Inside the ship: lamplight off old liner panels and grow-light over the garden. Warm, a little bloom for the lamps, no tilt (D060).
            var hands=ArenaBuilder.Asset<Hd2dProfile>("Hands",p=>{p.tint=new Color(1,.97f,.93f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.55f;});
            var cold=Find<Hd2dProfile>("Cold");
            var source=ZoneKit.WorldMaterial("tallow-sky");
            // The panorama turned to a part of the sky the convoy hasn't been seen against, cooled toward slate: the ship's windows and the open freight dock look out on it.
            var space=ZoneKit.Sky("manyhands-sky",source,new Color(.5f,.52f,.62f,.5f),212,1.05f);
            ArenaBuilder.Asset<ZoneDef>(City,z=>{z.id=z.scene=City;z.displayName="Many Hands";z.chapter=5;z.kind=ZoneKind.GroundSafe;z.cameraProfile=ground;z.hd2dProfile=hands;z.skybox=space;
                z.enemyThreat=1;z.cameraClearsScenery=false;z.musicId="Many Hands";z.shopMusicId="Moonbase Market";z.brightFloor=false;
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(CityBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=CityLevel;}});
            ArenaBuilder.Asset<ZoneDef>(Hull,z=>{z.id=z.scene=Hull;z.displayName="The Hull of Many Hands";z.chapter=5;z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=cold;z.skybox=space;
                z.enemyThreat=1;z.flightPlane=1;z.cameraClearsScenery=false;z.musicId="Starward Journey";z.fightMusicId="Starfight";});

            DefineEnemies();
            Quests();

            // Many Hands on Sela's chart (guide 3.10): charted by arriving, joined to Tallow (whose approach it lies off), in the fourth column below the middle row.
            ArenaBuilder.Asset<ChartStop>(City,s=>{s.id=City;s.displayName="Many Hands";s.region="The convoy";s.blurb="The ship the convoy became: Corra's stall, Dov's counter, Hadda's pump-and-press, Binna's galley and the garden.";
                s.zone=City;s.spawn="Arrival";s.position=new Vector2(.76f,.42f);s.routes=new[]{"TallowDrift"};s.shop=s.bench=s.repair=true;s.chapter=5;
                s.reaches=new[]{Hull};});

            // The new speakers (manifest section 5). Everyone else who speaks aboard already has a definition from an earlier chapter, and keeps it. Ilo spoke in chapter 1 before
            // speakers had definitions; his nodes here begin with his id, so he gets one, on his own body and sheet.
            void Person(string id,string display,bool female,string standIn,params string[] bodies)
                =>ArenaBuilder.Asset<CharacterDef>(id,c=>{c.id=c.speakerId=id;c.displayName=display;c.female=female;c.natural=FirstBody(bodies)??ZoneKit.Prefab("Characters",standIn);
                    if(c.portraitNatural==null)c.portraitNatural=AssetDatabase.LoadAssetAtPath<PortraitSet>("Assets/_Project/Resources/Portraits/"+id+"_Natural.asset");});
            Person("Meret","Meret Venn",true,"Mira","MeretBody");
            Person("Dessik","Dessik Varn",false,"Orrin","DessikBody");
            Person("Ilo","Ilo",false,"Orrin","IloBody");

            // Two counters aboard (manifest 8.3), opened by who is talking (ShopUi.For): Corra's parts and kettles in the arcade, Dov's provisions beside her. Hadda keeps the bench.
            var corra=ProgressionBuilder.Stall(City+"_Corra","CORRA'S STALL",Sell("EmittersT3",560),Sell("EdgesT3",600),Sell("FramesT3",620),Sell("DrivesT3",540),Sell("ModulesT3",640),
                Sell("ModulesGuardCapacitor",720),Sell("ModulesMedicFilament",680),Sell("ModulesReservoir",700,0,"kettle.reported"),
                Sell("ScrapAlloy",16,3),Sell("LatticeFilament",18,3),Sell("RidgeCrystal",32,3),Sell("Rootfibre",45,3),Sell("AmberSap",54,3),Sell("ReedWeave",54,3),Sell("Glowspore",63,3));
            // "Stall price is yours from now on" (CorraKettleReturn): her own price on everything she sells, said on the stall.
            corra.discountFlag="kettle.reported";corra.discount=.85f;corra.discountNote="STALL PRICE  ·  CORRA'S THANKS FOR THE KETTLES";
            // From the fleet's chart on (chapter 10 opens there) the Decks' convoy is in, and Corra stocks its Choirwrought: chapter 10 is sized on it, and before this only
            // Neighbourhood Afloat sold any, after the last boss (D326).
            corra.offers=corra.offers.Concat(ProgressionBuilder.Choirwrought(ProgressionBuilder.ConvoyIn)).ToArray();corra.noteFlag=ProgressionBuilder.ConvoyIn;corra.note=ProgressionBuilder.ConvoyNote;
            EditorUtility.SetDirty(corra);
            ProgressionBuilder.Stall(City+"_Dov","DOV'S COUNTER",Sell("RepairGel",24),Sell("RepairSalve",60),Sell("ChargeCell",28),Sell("ChargeBank",60),Sell("SyncTonic",90),Sell("RelinkSpike",90),
                Sell("SurgeDraught",50),Sell("PlatingWax",50),Sell("Quickening",50),Sell("RiverSkewer",18),Sell("PodStew",24),Sell("SpiceCake",24));
        }

        /// <summary>The four quests, step for step as the script's manifest has them (section 4). Every step a talk completes is a Flag step, so the three jobs, the kettles and the
        /// ledger's three owners can each be done in any order (QuestService.CatchUp).</summary>
        static void Quests()
        {
            ArenaBuilder.Asset<QuestDef>("ManyHands",q=>{q.id="ManyHands";q.title="Many Hands";q.main=true;q.chapter=5;
                q.description="The convoy has become one ship with a name on its dock door, and nobody has yet made it a town.";
                // Step 0 is the flag Abri's welcome sets as it starts the quest, not a Reach of the zone: the quest starts on the dock, after ArenaRuntime has reported the arrival, so a
                // Reach step would have sat on "Dock at Many Hands" with the party standing on the dock until the next load.
                q.steps=new[]{Step(ObjectiveKind.Flag,"hands.aboard"),Step(ObjectiveKind.Flag,"hands.mooringSettled"),Step(ObjectiveKind.Flag,"hands.ventFixed"),Step(ObjectiveKind.Flag,"hands.chartOpen"),
                    Step(ObjectiveKind.Flag,"hands.iloAboard"),Step(ObjectiveKind.Kill,"Hulljack"),Step(ObjectiveKind.Kill,"DessikVarn"),Step(ObjectiveKind.Flag,"hands.breachSealed"),
                    Step(ObjectiveKind.Flag,"hands.meretMet"),Step(ObjectiveKind.Flag,"hands.dinnerSat"),Step(ObjectiveKind.Flag,"hands.stillwaterHeard"),Step(ObjectiveKind.Flag,"hands.numberHeard"),
                    Step(ObjectiveKind.Flag,"hands.footnotesTabled"),Step(ObjectiveKind.Flag,"hands.sorrelNamed"),Step(ObjectiveKind.Flag,"hands.dinnerDone"),Step(ObjectiveKind.Flag,"hands.chartRead")};
                q.stepText=new[]{"Dock at Many Hands","Settle where the rafts hold on: hear Ruva and Maru out","Fix the galley vent for Binna","Set the chart table by Pell's lamps on the lamp gallery",
                    "Meet the courier asking for you at the dock","Cut the salvage frame's four feet off the hull","Stop Dessik Varn in the freight hall","Get to the breached cabins",
                    "Hear what Director Venn has come for","Find Director Venn something to eat","Let her explain Stillwater","Hear the number","Let Ilo answer Ruva","Ask what the anchor costs",
                    "See Director Venn to the galley door","Read the chart on the lamp gallery"};
                q.stepTargets=new[]{At(City,"Keeper"),At(City,"Ruva"),At(City,"Binna"),At(City,"Pell"),At(City,"Ilo"),At(Hull,null,null,"Hulljack",null,City),At(City,null,null,"DessikVarn",null,Hull),
                    At(City,"Patrol",null,null,null,Hull),At(City,"Meret"),At(City,"Binna"),At(City,"Meret"),At(City,"Ruva"),At(City,"Ilo"),At(City,"Meret"),At(City,"Meret"),At(City,null,"ChartTable")};
                q.closingLine="Chapter 5 complete. The nursery's only way out runs cold: read the chart for Hoarfell at the chart table.";
                // Where chapter 6 begins (the chart for Hoarfell, at the table this chapter's last talk is read at: HoarfellBuilder's staging).
                q.next=At(City,null,null,null,null,Hull);q.next.hasPoint=true;q.next.point=new Vector3(Hands.ChartTalk.x,0,Hands.ChartTalk.y);
                // The chapter's card waits for the last talk on the lamp gallery; its body is the manifest's (section 6), two lines of the card.
                q.cardFlag="chapter5.complete";
                q.cardBody="Many Hands is a town, with a chart that can take it anywhere it has been. Director Venn's anchor will be braced on Sorrel, and she has given you forty days and her own drawings to find something better.";
                // What the story has set by its end that the steps above don't say: who has been heard, the world's own events on the main path, the fight. A later chapter's fixture
                // starts with these (DevLoadout).
                q.storyFlags=new[]{"hands.ruvaHeard","hands.maruHeard","hands.lampsSighted","hands.claimRead","hands.feet1","hands.feet2","hands.feet3","hands.feet4","hands.hullCleared",
                    "bossdown.Hulljack","hands.hallEntered","bossdown.DessikVarn","hands.dessikYielded","met.Meret","met.Ilo","met.Dessik","chapter5.complete"};
                q.rewardXp=1100;q.rewardScrip=700;q.rewards=new[]{new Ingredient{id="FramesT3",count=1}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("KettleTrade",q=>{q.id="KettleTrade";q.title="Somebody Has to Sell a Kettle";q.chapter=5;
                q.description="Corra Vey has three kettles paid for and nobody to carry them the length of the ship.";
                q.steps=new[]{Step(ObjectiveKind.Flag,"kettle.garden"),Step(ObjectiveKind.Flag,"kettle.bay"),Step(ObjectiveKind.Flag,"kettle.cabins"),Step(ObjectiveKind.Flag,"kettle.reported")};
                q.stepText=new[]{"Take a kettle to Marrow-1's crew on the garden deck","Take a kettle to the weaving bench in the raft bay","Take cabin nine's kettle to Keeper Abri's desk","Tell Corra all three are delivered"};
                q.stepTargets=new[]{At(City,"Garo",null,null,"Kettle for the garden deck"),At(City,"Ona",null,null,"Kettle for the weaving bench"),At(City,"Keeper",null,null,"Kettle for cabin nine"),
                    At(City,"Corra",null,null,"Tell Corra they're delivered")};
                q.rewardXp=220;q.rewardScrip=120;q.rewards=new[]{new Ingredient{id="ModulesReservoir",count=1}};q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("StowawayFare",q=>{q.id="StowawayFare";q.title="The Stowaway's Fare";q.chapter=5;
                q.description="Jask rode up from Rootmarket in Marrow-3's seed racks, and a passage has a price.";
                q.steps=new[]{Step(ObjectiveKind.Flag,"hands.ruvaOnJask"),Step(ObjectiveKind.Flag,"hands.jaskPlaced"),Step(ObjectiveKind.Flag,"jask.thanked")};
                q.stepText=new[]{"Hear what Ruva has to say about Jask","Settle with Teo where Jask works off his passage","See how Jask is getting on"};
                q.stepTargets=new[]{At(City,"Ruva",null,null,"Hear Ruva on Jask"),At(City,"Teo",null,null,"Settle it with Teo"),At(City,"Jask",null,null,"See how Jask is doing")};
                q.rewardXp=200;q.rewardScrip=90;q.rewards=new[]{new Ingredient{id="ModulesTwinThread",count=1}};q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("ClaimsLedger",q=>{q.id="ClaimsLedger";q.title="The Claims Ledger";q.chapter=5;
                q.description="Three hulls in Dessik Varn's ledger belong to people standing aboard Many Hands.";
                q.steps=new[]{Step(ObjectiveKind.Flag,"ledger.teo"),Step(ObjectiveKind.Flag,"ledger.maru"),Step(ObjectiveKind.Flag,"ledger.abri"),Step(ObjectiveKind.Flag,"ledger.returned")};
                q.stepText=new[]{"Tell Teo his seed barges are in the ledger","Tell Maru her raft is in the ledger","Tell Keeper Abri that Tallow is in the ledger","Bring the three statements back to Ilo"};
                q.stepTargets=new[]{At(City,"Teo",null,null,"Tell Teo"),At(City,"Maru",null,null,"Tell Maru"),At(City,"Keeper",null,null,"Tell Keeper Abri"),At(City,"Ilo",null,null,"Bring the statements to Ilo")};
                q.rewardXp=240;q.rewardScrip=320;q.rewards=Array.Empty<Ingredient>();q.flagsOnComplete=Array.Empty<string>();});
        }

        // ---------------------------------------------------------------- light
        static Color C(float r,float g,float b)=>new(r,g,b,1);
        /// <summary>The city's authored light and grade (ZoneLook finds it by its attribute). The hull's is registered from its own file.</summary>
        [ZoneLook.Source] static void Looks()
        {
            // Inside the ship: a high work-light through the spine's glass for the sun, cooled a little so the lamps and the galley's warmth have something to stand against; a soft
            // slate fill from the open dock and the windows, warm bounce off the deck plates.
            ZoneLook.Register(City,new ZoneLook.Spec{sun=C(1f,.92f,.82f),sunIntensity=1.25f,sunPitch=52,shadowStrength=.72f,
                sky=C(.46f,.5f,.6f),equator=C(.44f,.42f,.42f),ground=C(.24f,.22f,.2f),
                contrast=10,saturation=8,splitShadows=C(.42f,.46f,.58f),splitHighlights=C(.6f,.54f,.44f),
                bloomThreshold=.92f,bloomIntensity=.55f,vignette=.2f});
        }

        // ---------------------------------------------------------------- what the region is made of
        /// <summary>The ship's bed, made by tools/manyhands_ambience.py (synthesised, nothing sourced). Until the file is there the zone keeps whatever its definition already holds.</summary>
        const string CityBed="Assets/_Project/Audio/Ambience/AMB_ManyHands.ogg";
        /// <summary>Measured to the other beds' level, since nobody can listen to it: every earlier bed plays at its file's RMS times its volume, about .0224 (-33 dB); the file's RMS is .0852 (the script prints it).</summary>
        const float CityLevel=.263f;

        // ---------------------------------------------------------------- Tallow approach: the way in
        /// <summary>Where the convoy lies off Tallow: a dock on the approach that takes a ship in to Many Hands once chapter 4 has closed (Abri's call names it). Hidden until then rather
        /// than locked: a chapter 1 save has never heard of Many Hands and shouldn't read the name. Replaces only its own dock, spawn, sign and gate in the Tallow Approach scene; re-run
        /// after that scene's own rebuild or staging (and after the other chapters' StageApproach, which re-stage its shared pieces).</summary>
        static void StageTallowDock()
        {
            const string Label="Dock — Many Hands",GateName="Many Hands is moored off Tallow";
            var approach=EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowApproach.unity");
            foreach(var old in UnityEngine.Object.FindObjectsByType<WarpBeacon>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(w=>w.scene==City).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(var old in UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(s=>s.id==City).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(var old in UnityEngine.Object.FindObjectsByType<FlagGate>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(g=>g.name==GateName).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(var old in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.text==Label).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            // Due south of the arrival, between Xylos's warp (south-east) and Wendmire's (south-west): the convoy is what the approach comes in past.
            var dock=WorldBuilder.Warp(Label,TallowDockAt,City,"","Arrival");dock.lockedPrompt="";
            var sign=UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.text==Label).gameObject;
            var gate=new GameObject(GateName,typeof(FlagGate)).GetComponent<FlagGate>();gate.flag="chapter4.complete";gate.showWhenSet=true;gate.targets=new[]{dock.gameObject,sign};
            dock.gameObject.SetActive(false);sign.SetActive(false);
            WorldBuilder.Spawn(City,TallowDockAt+new Vector3(0,0,9));
            ChapterOneStaging.Approach();
            EditorSceneManager.MarkSceneDirty(approach);EditorSceneManager.SaveScene(approach);Debug.Log("TALLOW_APPROACH_MANYHANDS_OK");
        }
        static readonly Vector3 TallowDockAt=new(0,1,-44);
    }
}
