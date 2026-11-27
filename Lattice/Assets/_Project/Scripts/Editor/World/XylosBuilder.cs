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
    /// Chapter 3, Carrying a Place (brief: docs/chapters/ch03-carrying-a-place.md; script wiring: docs/writing/ch03-manifest.md). Isolated builder for its definitions and four zones,
    /// on <see cref="ZoneKit"/>:
    /// <list type="bullet">
    /// <item>Xylos Nearspace (space combat, <c>XylosBuilder.Nearspace.cs</c>): three seed barges held by snagvine in a debris stream under a cracked weather mirror; cut all three free and the delta beacon lands.</item>
    /// <item>Rootmarket (town, <c>XylosBuilder.Rootmarket.cs</c>): root platforms round a landing pool, the seed house at the back, a rope to haul before anyone talks terms.</item>
    /// <item>The Braided River (wild, <c>XylosBuilder.River.cs</c>) and the Walking Canopy (dungeon, <c>XylosBuilder.Canopy.cs</c>): stubs here that load and connect, for the second world engineer to build out.</item>
    /// </list>
    /// Enemies are in <c>XylosBuilder.Enemies.cs</c> (placeholders on existing archetypes until the combat engineer takes that file). Kit pieces generated for the chapter
    /// (tools/meshy/c10_xylos_batch.py) are used when staged; until then each falls back to something built here or an existing piece.
    /// Runs: <c>Definitions</c>, <c>BuildZones</c> with <c>-xylos-zones XylosNearspace,Rootmarket,...</c>, <c>Dialogue</c>, <c>StageApproach</c>; then <c>ZoneLook.Apply</c> for what was built.
    /// </summary>
    public static partial class XylosBuilder
    {
        public static readonly string[] Zones={"XylosNearspace","Rootmarket","BraidedRiver","WalkingCanopy"};
        public static void Definitions()=>BatchTools.Run(()=>{Define();AssetDatabase.SaveAssets();Debug.Log("XYLOS_DEFS_OK");});
        public static void BuildZones()=>BatchTools.Run(()=>
        {
            string arg=DevArgs.Value("-xylos-zones");var zones=string.IsNullOrEmpty(arg)?Zones:arg.Split(',',StringSplitOptions.RemoveEmptyEntries);
            Define();foreach(var zone in zones)BuildZone(zone.Trim());
            BatchTools.RegisterScenes();AssetDatabase.SaveAssets();Debug.Log("XYLOS_ZONES_OK "+string.Join(",",zones));
        });
        [Restage.Step("chapter3.tallow-warp","TallowApproach",After="chapter1.nov10",Chapter=3,Leaves=new[]{"TallowApproach: Warp — Xylos Nearspace <WarpBeacon> #1","TallowApproach: Spawn_Xylos #1"})]
        public static void StageApproach()=>BatchTools.Run(()=>{StageTallowApproach();AssetDatabase.SaveAssets();Debug.Log("XYLOS_APPROACH_OK");});
        /// <summary>Copies docs/writing/*.yarn into Resources and recompiles the Yarn project: a new .yarn file is not in the compiled program until the project reimports, and a missing
        /// node fails silently at runtime. From the moment Xylos.yarn is in, the dialogue tests want every speaker's portrait set and every quest it starts (manifest section 8).</summary>
        public static void Dialogue()=>BatchTools.Run(()=>{RefreshDialogue();Debug.Log("XYLOS_DIALOGUE_OK");});
        /// <summary>Every node the chapter's zones name: one that didn't compile stops the build here instead of doing nothing when talked to.</summary>
        static readonly string[] Nodes=
        {
            "AbriSeedRoad","AbriSeedRoadRepeat","AbriSeedRoadCharted","AbriSeedRoadWaiting","PellLaneLantern","PellLaneRepeat","AbriDinnerCall","AbriDinnerServed","PellAfterVote",
            "TeoBargeHail","PatrolHail","TeoClaimDrones","SelaWeatherMirror",
            "RuvaArrival","RuvaRopeHold","BinnaRopeHold","RuvaTerms","RuvaRepeat","RuvaCrewsHome","RuvaVote","RuvaAfterVote","RuvaAfterVoteRepeat","SelaSeedHouse","SelaCompactNotice",
            "TeoFirst","TeoRepeat","TeoTagsReturn","TeoPost","BinnaFirst","BinnaRepeat","BinnaBasketsOffer","BinnaBasketsReturn","BinnaAfterBaskets","BinnaPost",
            "JaskFirst","JaskAsksMore","JaskRepeat","SelaJaskPole","SennFirst","SennRepeat","SennPostOffer","SennPostReturn","SennAfterVote","SennLastWord","HaddaFirst","HaddaRepeat","HaddaPost","HaddaAir",
            "SelaRiverArch","SelaSluiceA","GaroRelief","GaroRepeat","DabbRelief","DabbRepeat","JaskFerryFirst","JaskFerry","JaskFerryRepeat","SelaMooringPost","SelaSluiceB","SelaSeedfall",
            "SelaSeedthaneGone","TeoSeedfall","TeoSeedfallRepeat","SelaCanopyMooring","SelaCanopyAnchor","SelaSeedVault","SelaFloodcrownBasin","SelaFloodcrownGone",
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
        /// <summary>Refuses a scene holding a mesh whose points aren't all numbers. Unity culls such a mesh from every frame and logs "abnormal mesh bounds" on every load, and every
        /// test that heeds the log fails on it: a husk's horn once took a fractional power of the sine of pi, which in floats is a hair under zero, and blanked the whole debris stream.</summary>
        static void NoBrokenMeshes(string zone)
        {
            // Every point, not the bounds: a mesh's bounds start from its first point and a comparison with NaN is false, so a mesh with a bad point anywhere but first has
            // ordinary-looking bounds and draws a spike or a hole instead (two of the debris stream's stretches were caught by Unity's log; the rest weren't).
            static bool Real(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
            var seen=new System.Collections.Generic.HashSet<Mesh>();var broken=new System.Collections.Generic.List<string>();
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var mesh=filter.sharedMesh;if(mesh==null||!mesh.isReadable||!seen.Add(mesh))continue;
                if(mesh.vertices.Any(p=>!Real(p.x)||!Real(p.y)||!Real(p.z))||mesh.normals.Any(n=>!Real(n.x)||!Real(n.y)||!Real(n.z)))broken.Add(filter.name);
            }
            if(broken.Count>0)throw new InvalidOperationException(zone+": meshes with points that aren't numbers (abnormal bounds): "+string.Join(", ",broken.Distinct()));
            Debug.Log("XYLOS_MESHES_OK "+zone);
        }
        static void BuildZone(string zone)
        {
            switch(zone)
            {
                case "XylosNearspace":Nearspace();break;
                case "Rootmarket":Rootmarket();break;
                case "BraidedRiver":River();break;
                case "WalkingCanopy":Canopy();break;
                default:throw new InvalidOperationException("Unknown Xylos zone "+zone);
            }
        }

        // ---------------------------------------------------------------- definitions
        static T Find<T>(string id)where T:ScriptableObject=>ZoneKit.Find<T>(id);
        static Objective Step(ObjectiveKind kind,string target,int count=1)=>new(){kind=kind,target=target,count=count};
        static QuestTarget At(string scene,string speaker=null,string report=null,string encounter=null,string brief=null,params string[] via)=>new(){scene=scene,speaker=speaker,report=report,encounter=encounter,brief=brief,via=via};
        /// <summary>The first of these bodies that has been taken in: a named reskin, then the plain Okhari body, then whoever stands in.</summary>
        internal static GameObject FirstBody(params string[] ids)=>ids.Select(id=>ZoneKit.Prefab("Characters",id)).FirstOrDefault(body=>body!=null);
        /// <summary>The body a grower with no name wears, and what stands in for it until the Okhari body is staged.</summary>
        internal static string Grower(string standIn)=>ZoneKit.Prefab("Characters","OkhariGrower")!=null?"OkhariGrower":ZoneKit.Prefab("Characters","OkhariBody")!=null?"OkhariBody":standIn;

        static void Define()
        {
            Materials();
            var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");var cold=Find<Hd2dProfile>("Cold");var cave=Find<Hd2dProfile>("Cave")??cold;
            // Green daylight under a canopy: a little bloom for the water's glint and the seed lanterns, and no tilt (D060).
            var jungle=ArenaBuilder.Asset<Hd2dProfile>("Jungle",p=>{p.tint=new Color(.96f,1,.93f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.5f;});
            var source=ZoneKit.WorldMaterial("tallow-sky");
            // Sunward of Tallow: the same panorama turned again and warmed toward gold-green, so the lane ends somewhere that isn't the Wick's amber or the Halo's blue.
            var space=ZoneKit.Sky("xylos-sky",source,new Color(.5f,.62f,.42f,.5f),251,.9f);
            // Over the town the sky is only ever the far edge of the frame: pale, hazed green.
            var day=ZoneKit.Sky("xylos-day-sky",source,new Color(.62f,.86f,.74f,.5f),40,1.9f);
            // Threat steps on from chapter 2's 1.5 to 1.7 (D194). Placeholders until the combat engineer tunes the chapter against a Sync 10 to 13 party.
            ArenaBuilder.Asset<ZoneDef>("XylosNearspace",z=>{z.id=z.scene="XylosNearspace";z.displayName="Xylos Nearspace";z.chapter=3;z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=cold;z.skybox=space;
                z.enemyThreat=1f;z.flightPlane=1;z.cameraClearsScenery=false;z.musicId="Starfield Frontier";z.fightMusicId="Starfight";});
            ArenaBuilder.Asset<ZoneDef>("Rootmarket",z=>{z.id=z.scene="Rootmarket";z.displayName="Rootmarket";z.chapter=3;z.kind=ZoneKind.GroundSafe;z.cameraProfile=ground;z.hd2dProfile=jungle;z.skybox=day;
                z.enemyThreat=1;z.cameraClearsScenery=true;z.musicId="Seed Shakers of Xylos";z.shopMusicId="Shop Theme";z.brightFloor=false;
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(RiverTownBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=RiverTownLevel;}});
            ArenaBuilder.Asset<ZoneDef>("BraidedRiver",z=>{z.id=z.scene="BraidedRiver";z.displayName="The Braided River";z.chapter=3;z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=jungle;z.skybox=day;
                z.enemyThreat=1f;z.cameraClearsScenery=true;z.musicId="Jungle Planet Groove";z.fightMusicId="Riverbed Stomp";
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(RiverTownBed);if(bed!=null&&z.ambientAudio==null){z.ambientAudio=bed;z.ambientVolume=RiverTownLevel;}});
            ArenaBuilder.Asset<ZoneDef>("WalkingCanopy",z=>{z.id=z.scene="WalkingCanopy";z.displayName="The Walking Canopy";z.chapter=3;z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=cave;z.skybox=null;
                z.enemyThreat=1.1f;z.cameraClearsScenery=true;z.musicId="Riverbed Stomp";});

            DefineEnemies();

            // The five quests, step for step as the script's manifest has them (section 4). SeedRoad's steps 5 to 8 are flags so the river can be done in any order; its two counted
            // steps are reported by the world (a barge freed, an anchor root worked), never by a conversation that sets its own flag. A thing offered only at a quest's step
            // (a claim tag, Senn's post, Jask's pole) answers an Interact step with its own report, for the same reason.
            const string Near="XylosNearspace",Town="Rootmarket",River="BraidedRiver",Canopy="WalkingCanopy";
            ArenaBuilder.Asset<QuestDef>("SeedRoad",q=>{q.id="SeedRoad";q.title="Carrying a Place";q.main=true;q.chapter=3;
                q.description="Tallow's cabins need feeding, and the river growers of Xylos won't sell a field they would have to pull up by the root.";
                q.steps=new[]{Step(ObjectiveKind.Flag,"xylos.charted"),Step(ObjectiveKind.Reach,Near),Step(ObjectiveKind.Interact,"SeedBarge",3),Step(ObjectiveKind.Reach,Town),Step(ObjectiveKind.Flag,"xylos.termsHeard"),
                    Step(ObjectiveKind.Flag,"xylos.sluiceA"),Step(ObjectiveKind.Flag,"xylos.marrow1"),Step(ObjectiveKind.Flag,"xylos.marrow2"),Step(ObjectiveKind.Flag,"xylos.sluiceB"),Step(ObjectiveKind.Kill,"Seedthane"),
                    Step(ObjectiveKind.Flag,"xylos.seedthaneGone"),Step(ObjectiveKind.Interact,"AnchorRoot",3),Step(ObjectiveKind.Kill,"Floodcrown"),Step(ObjectiveKind.Flag,"xylos.voteHeard")};
                q.stepText=new[]{"Teach Pell's lantern the lane to Xylos","Take the Xylos warp off Tallow's approach","Cut Teo Marrow's seed barges free of the snagvine","Follow the delta beacon down to Rootmarket",
                    "Take the mooring line, then hear Ruva Okh out","Open the middle sluice on the Braided River's north bank","Relieve the crew shut inside Marrow-1, on the first island",
                    "Relieve the crew shut inside Marrow-2, on the second island","Open the south sluice to reach Seedfall bank","Drive the Seedthane off Marrow-3","Ask Teo Marrow what still blocks the crossing",
                    "Work the Walking Canopy's anchor roots to reach its basin","Stop the Floodcrown holding the basin's flood gates","Tell Ruva Okh the crossing is open, and hear the vote"};
                q.stepTargets=new[]{At("TallowDrift","Pell"),At(Near,null,null,null,null,"TallowApproach"),At(Near,null,"SeedBarge"),At(Town,null,null,null,null,Near),At(Town,"Ruva"),
                    At(River,null,"SluiceA",null,null,Town),At(River,"Garo",null,"River_Marrow1",null,Town),At(River,"Dabb",null,"River_Marrow2",null,Town),At(River,null,"SluiceB",null,null,Town),At(River,null,null,"Seedthane",null,Town),
                    At(River,"Teo",null,null,null,Town),At(Canopy,null,"AnchorRoot",null,null,River,Town),At(Canopy,null,null,"Floodcrown",null,River,Town),At(Town,"Ruva",null,null,null,River,Canopy)};
                q.closingLine="Chapter 3 complete. Tallow is fed, and its air is going thick: see Keeper Abri.";
                // Where chapter 4 begins (Abri's offer aboard Tallow): from anywhere else, the way home.
                q.next=At("TallowDrift","Keeper",null,null,null,"TallowApproach",Near);
                // The chapter's card waits for Abri's call, not the vote that completes the quest; its body is two lines of the card and no more.
                q.cardFlag="chapter3.complete";q.cardBody="Tallow has dinner, and growers aboard who'll vote on where it goes. Three more settlements have asked to follow, and the air aboard is going thick.";
                // What the story has set by its end that the steps above don't say: the lane and the EXTRA moves Pell's lantern unlocks with it, the nearspace and town events the
                // world owns, who has been met and heard on the main path, and the two fights. A later chapter's fixture starts with these (DevLoadout).
                q.storyFlags=new[]{"extra.unlocked","xylos.teoHailed","xylos.patrolHeard","xylos.claimSeen","xylos.barge1","xylos.barge2","xylos.barge3","xylos.bargesFree","clear.Barge1_Knots","clear.Barge2_Knots","clear.Barge3_Knots",
                    "xylos.landed","xylos.arrivalSeen","xylos.ropeHauled","met.Ruva","xylos.archSeen","xylos.crewsThanked","bossdown.Seedthane","xylos.anchor1","xylos.anchor2","xylos.anchor3","bossdown.Floodcrown","xylos.floodcrownGone",
                    "ruva.lineHeld","chapter3.complete"};
                // What the growers have to give: the module their flood gates taught them (it holds a charge as a basin holds water), a heartwood node, and three jars of amber sap
                // from the seed house's stores. Not salve: a party at chapter 3's end carries its six already, and a reward over the pouch's cap is paid out in scrip instead, so
                // the card named salve the bag never got (QaPlanWorldTests.EveryQuestsCardsFollowItsStepsAndItsRewardIsPaidOnce).
                q.rewardXp=900;q.rewardScrip=500;q.rewards=new[]{new Ingredient{id="ModulesReservoir",count=1},new Ingredient{id="HeartwoodNode",count=1},new Ingredient{id="AmberSap",count=3}};q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("ThreeBaskets",q=>{q.id="ThreeBaskets";q.title="Three Baskets";q.chapter=3;
                q.description="The storm stripped Binna's spice garden, and she is cooking for a vote.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"SpicePods",3),Step(ObjectiveKind.Flag,"binna.basketsReturned")};
                q.stepText=new[]{"Pick red, blue and yellow spice pods: one plant on each river island, one at Seedfall","Bring the three baskets back to Binna's kitchen"};
                q.stepTargets=new[]{At(River,null,"SpicePods",null,"Pick spice pods",Town),At(Town,"Binna",null,null,"Bring the baskets to Binna",River)};
                // Binna pays in jars of the amber sap she cures her pods in; her cooking is the other half of it, on her counter from then on (ShopOffer requires the quest).
                // Not the food itself: a meal's pouch holds three, she sells them from the moment this is done, and whatever is over a cap is paid out in scrip, so a card
                // naming two stews could pay one and some coin. Every chapter 3 reward is a material or a part, which no pouch caps: what a card names is what is paid.
                q.rewardXp=220;q.rewardScrip=120;q.rewards=new[]{new Ingredient{id="AmberSap",count=2}};q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("MooringPost",q=>{q.id="MooringPost";q.title="The Mooring Post";q.chapter=3;
                q.description="Old Senn carved a mooring post for his wife's island and never carried it out there.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"SennPost"),Step(ObjectiveKind.Flag,"senn.postReported")};
                q.stepText=new[]{"Set Senn's mooring post on Ulma's island, off the Braided River's second island","Tell Old Senn where the post went"};
                q.stepTargets=new[]{At(River,null,"SennPost",null,"Set Senn's mooring post",Town),At(Town,"Senn",null,null,"Tell Senn where it went",River)};
                q.rewardXp=240;q.rewardScrip=100;q.rewards=new[]{new Ingredient{id="ModulesTwinThread",count=1}};q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("Questions",q=>{q.id="Questions";q.title="Questions for a Surveyor";q.chapter=3;
                q.description="Jask poles the Rootmarket ferry and has never seen a place with no ground under it. He has questions, mostly for Sela.";
                q.steps=new[]{Step(ObjectiveKind.Flag,"jask.ferryTalk"),Step(ObjectiveKind.Interact,"JaskPole")};
                q.stepText=new[]{"Find Jask at his ferry stage, mid-river","Find Jask at Rootmarket's ferry stage once the growers have voted"};
                q.stepTargets=new[]{At(River,"Jask",null,null,"Find Jask at his ferry",Town),At(Town,null,"JaskPole",null,"Find Jask after the vote",River)};
                // What a ferry hand leaves wound on his pole: a hank of rootfibre line. Not his lunch: river skewers are on Binna's counter from the start and a pouch holds
                // three, so a card naming two could pay scrip instead (see ThreeBaskets).
                q.rewardXp=200;q.rewardScrip=60;q.rewards=new[]{new Ingredient{id="Rootfibre",count=3}};q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("ClaimTags",q=>{q.id="ClaimTags";q.title="Unmoored";q.chapter=3;
                q.description="A salvage crew has tagged Teo's barges as vessels off their registered mooring, the moment they were worth taking.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"ClaimTag",3),Step(ObjectiveKind.Flag,"xylos.tagsReported")};
                q.stepText=new[]{"Burn Hulljack's claim tags off Teo's three seed barges","Tell Teo Marrow his barges are unclaimed"};
                q.stepTargets=new[]{At(Near,null,"ClaimTag",null,"Burn off the claim tags",Town),At(Town,"Teo",null,null,"Tell Teo they're gone",Near)};
                q.rewardXp=260;q.rewardScrip=220;q.rewards=new[]{new Ingredient{id="DrivesT3Scout",count=1}};q.flagsOnComplete=Array.Empty<string>();});

            // Rootmarket on Sela's chart (guide 3.10): charted by arriving, joined to Tallow, the middle column of the chart's field. What stands there is what the page lists.
            ArenaBuilder.Asset<ChartStop>("Rootmarket",s=>{s.id="Rootmarket";s.displayName="Rootmarket";s.region="Xylos";s.blurb="The river town on the roots: Binna's kitchen, Hadda's parts, the press and the mender's cradle.";
                s.zone="Rootmarket";s.spawn="Arrival";s.position=new Vector2(.52f,.66f);s.routes=new[]{"TallowDrift"};s.shop=s.bench=s.repair=true;s.chapter=3;
                s.reaches=new[]{"XylosNearspace","BraidedRiver","WalkingCanopy"};});

            // Everyone the script gives a line to. A body is the named reskin once the art runner has staged it, then the plain Okhari body, then a stand-in from the cast.
            void Person(string id,string display,bool female,string standIn,params string[] bodies)
                =>ArenaBuilder.Asset<CharacterDef>(id,c=>{c.id=c.speakerId=id;c.displayName=display;c.female=female;c.natural=FirstBody(bodies)??ZoneKit.Prefab("Characters",standIn);});
            Person("Ruva","Ruva Okh",true,"Neve","RuvaBody","OkhariBody");
            Person("Teo","Teo Marrow",false,"Hal","TeoBody","OkhariBody");
            Person("Binna","Binna",true,"Mira","BinnaBody","OkhariBody");
            Person("Jask","Jask",false,"Orrin","JaskBody","OkhariBody");
            Person("Senn","Old Senn",false,"Hal","SennBody","OkhariBody");
            Person("Hadda","Hadda",true,"Mira","HaddaBody");
            Person("Garo","Garo",false,"Orrin","GaroBody","OkhariGrower","OkhariBody");
            Person("Dabb","Dabb",false,"Hal","DabbBody","OkhariGrower","OkhariBody");
            // Heard and never seen: a voice on the Compact band. It still wants a face of its own for the panel (a radio card).
            ArenaBuilder.Asset<CharacterDef>("Patrol",c=>{c.id=c.speakerId="Patrol";c.displayName="Steady Hand";c.female=false;});
        }

        // ---------------------------------------------------------------- light
        static Color C(float r,float g,float b)=>new(r,g,b,1);
        /// <summary>The region's authored light and grade (ZoneLook finds this by its attribute). Xylos is green daylight: a high warm sun through a canopy nobody sees, cool green
        /// fill so shade reads as leaf shade and not as dusk, river haze for distance. The Braided River's and the Walking Canopy's are registered from their own files.</summary>
        [ZoneLook.Source] static void Looks()
        {
            // Over the planet's day side: a hard warm star, a green-gold fill off Xylos below, so a hull's shaded side is never black against the dark.
            ZoneLook.Register("XylosNearspace",new ZoneLook.Spec{sun=C(1f,.96f,.84f),sunIntensity=1.4f,shadowStrength=.85f,
                sky=C(.3f,.42f,.4f),equator=C(.24f,.3f,.26f),ground=C(.16f,.2f,.13f),
                contrast=14,saturation=8,splitShadows=C(.42f,.5f,.54f),splitHighlights=C(.57f,.54f,.45f),
                bloomThreshold=.9f,bloomIntensity=.7f,vignette=.24f});
            ZoneLook.Register("Rootmarket",new ZoneLook.Spec{sun=C(1f,.96f,.84f),sunIntensity=1.35f,sunPitch=54,shadowStrength=.8f,
                sky=C(.5f,.64f,.62f),equator=C(.42f,.5f,.4f),ground=C(.24f,.27f,.18f),
                fog=true,fogColor=C(.62f,.78f,.68f),fogStart=55,fogEnd=210,
                contrast=10,saturation=8,splitShadows=C(.42f,.5f,.55f),splitHighlights=C(.56f,.54f,.45f),
                bloomThreshold=1f,bloomIntensity=.5f,vignette=.2f});
            // The Braided River's light is registered beside its builder (XylosBuilder.River.cs, RiverLooks).
        }

        // ---------------------------------------------------------------- what the region is made of
        /// <summary>A river-town bed (water on roots, insects, far birds), made by tools/xylos_ambience.py. Until it's there the zones keep whatever their definitions already hold.</summary>
        const string RiverTownBed="Assets/_Project/Audio/Ambience/AMB_RiverTown.ogg";
        /// <summary>Measured, since nobody can listen to it: every earlier bed plays at its file's RMS times its volume of about -33 dB (Lanternwick's city .1024 at .219, the gorge's wind
        /// .2117 at .106, Sorrel's .1471 at .151, the relay's hollow .0776 at .288). This file's RMS is .0634, so .353 puts it level with them; at .16 it was 7 dB under.</summary>
        const float RiverTownLevel=.353f;
        internal static Material Loam,Bark,RootWall,Plank,Thatch,Leaf,CanopyLeaf,Weave,Water,Lantern,Rope,Husk,Vine,Snag,SnagGlow,Pane,PaneLit,PaneEdge,Mirror,Land,LandBank,Sea,Clouds,CloudShade,Cloth,Strut;
        static Color Grey(float v,float warm=0)=>new(v+warm,v,v-warm,1);
        /// <summary>Everything built in code here is painted near neutral and tinted in its vertices, so one bark covers every root and trunk on Xylos and one plank every deck.</summary>
        static void Materials()
        {
            // Loam: dark mottled river earth with pale flecks of leaf and seed. The ground kit's layers lie over it.
            var loam=ZoneKit.Texture("Xylos/xylos-loam",512,(u,v)=>
            {
                float big=ZoneKit.Cloud(u,v,5,3),fine=ZoneKit.Cloud(u,v,24,9),fleck=ZoneKit.Tile(u*96,v*96,96,5);
                float tone=.72f+.26f*(big-.5f)+.2f*(fine-.5f)+(fleck>.9f?.14f:fleck<.07f?-.14f:0);
                return new Color(tone*1.02f,tone,tone*.9f,1);
            });
            // Bark: plates between furrows that wander and fork, each strip breaking into plates at its own heights, so it has grain both ways. Straight ridges from top to
            // bottom read as stripes on anything taller than a post.
            var bark=ZoneKit.Texture("Xylos/xylos-bark",512,(u,v)=>
            {
                float warp=ZoneKit.Cloud(u,v,6,21)*2.4f+ZoneKit.Tile(u*24,v*24,24,5)*.5f,across=u*8+warp;int furrow=Mathf.FloorToInt(across);
                float ridge=Mathf.Abs(Mathf.Sin(across*Mathf.PI)),own=ZoneKit.Tile(furrow*1.7f+.5f,.5f,8,31);
                float plate=Mathf.Repeat(v*6+own*5,1),crack=Mathf.Min(plate,1-plate),knot=ZoneKit.Cloud(u,v,12,2),grain=ZoneKit.Tile(u*96,v*32,32,4);
                float tone=.5f+.34f*Mathf.Pow(ridge,.6f)+.16f*(knot-.5f)+.07f*(grain-.5f)-(crack<.05f?.22f*ridge:0)-.1f*(own-.5f);
                return new Color(tone*1.03f,tone*.98f,tone*.92f,1);
            });
            // Planks: six boards a repeat laid along u, each its own tone, with a dark seam and a little grain.
            var plank=ZoneKit.Texture("Xylos/xylos-plank",512,(u,v)=>
            {
                float row=v*6;int board=Mathf.FloorToInt(row);float across=row-board,seam=Mathf.Min(across,1-across);
                float own=ZoneKit.Tile(board*3.7f,.5f,6,31),grain=ZoneKit.Tile(u*8+board*2.3f,v*90,90,4),end=Mathf.Abs(Mathf.Repeat(u+own*.63f,1)-.5f);
                float tone=.6f+.2f*(own-.5f)+.1f*(grain-.5f)-(seam<.06f?.3f:0)-(end<.006f?.25f:0);
                return new Color(tone*1.04f,tone,tone*.9f,1);
            });
            // Thatch: rows of big overlapping leaves, each darker under the one above.
            var thatch=ZoneKit.Texture("Xylos/xylos-thatch",512,(u,v)=>
            {
                float row=v*7;int r=Mathf.FloorToInt(row);float down=1-(row-r),col=u*9+(r%2)*.5f;float across=col-Mathf.Floor(col)-.5f;
                float leaf=1-Mathf.Clamp01((across*across*4+down*down)*.9f),vein=Mathf.Abs(across)<.03f?.08f:0,own=ZoneKit.Tile(Mathf.Floor(col)*1.3f,r*1.7f,9,12);
                float tone=.44f+.36f*down*leaf+.16f*(own-.5f)+vein;
                return new Color(tone*.98f,tone*1.02f,tone*.9f,1);
            });
            // Leaf: soft clumps for crowns and bushes, a centre line for a frond's own uv.
            var leaf=ZoneKit.Texture("Xylos/xylos-leaf",256,(u,v)=>
            {
                float clump=ZoneKit.Cloud(u,v,7,17),fine=ZoneKit.Cloud(u,v,20,4),vein=Mathf.Abs(u-.5f)<.035f?.1f:0;
                float tone=.62f+.3f*(clump-.5f)+.16f*(fine-.5f)+vein;
                return new Color(tone,tone*1.02f,tone*.96f,1);
            });
            // Weave: thick roots basket-woven, over and under.
            var weave=ZoneKit.Texture("Xylos/xylos-weave",512,(u,v)=>
            {
                float x=u*10,y=v*10;int cx=Mathf.FloorToInt(x),cy=Mathf.FloorToInt(y);float fx=x-cx-.5f,fy=y-cy-.5f;bool over=(cx+cy)%2==0;
                float strand=over?1-Mathf.Abs(fy)*1.7f:1-Mathf.Abs(fx)*1.7f,edge=over?Mathf.Abs(fx):Mathf.Abs(fy);
                float tone=.4f+.42f*Mathf.Clamp01(strand)-(edge>.44f?.16f:0)+.1f*(ZoneKit.Cloud(u,v,30,6)-.5f);
                return new Color(tone*1.04f,tone,tone*.9f,1);
            });
            // A seed barge's husk: long ribs from stem to stern, as a pod has.
            var husk=ZoneKit.Texture("Xylos/xylos-husk",512,(u,v)=>
            {
                float rib=Mathf.Abs(Mathf.Sin(u*Mathf.PI*9)),fibre=ZoneKit.Tile(u*60,v*6,60,3),blot=ZoneKit.Cloud(u,v,5,14);
                float tone=.5f+.3f*Mathf.Pow(rib,.5f)+.1f*(fibre-.5f)+.16f*(blot-.5f);
                return new Color(tone*1.05f,tone,tone*.86f,1);
            });
            // A mirror pane is a mirror: what it shows is the sky it faces, the nebula's bands drawn across it in soft swathes, a sheen where the far star lies along it, and the
            // seams of its cells only faintly. Its grid of cells at a fifth of its tone, and its flat blue, read from the flight camera as blockout under the ships (s17 200 and
            // 214, November 20). One repeat is about one pane.
            // Silvered, not the sky's own navy: a pane as dark as deep space read as a hole cut in the nebula, a loose shard as "a tear in the sky" (nov25-whole s17 123 and
            // 298). So its ground is a pale blue-silver, the nebula's colours lie in it at half strength, and a broad swell of light crosses each pane (whole periods of a sine
            // over the repeat, so no repeat shows an edge).
            static float Swell(float u,float v)=>.5f+.5f*Mathf.Sin((u+v)*Mathf.PI*2);
            var pane=ZoneKit.Texture("Xylos/xylos-pane",512,(u,v)=>
            {
                float gx=Mathf.Abs(Mathf.Repeat(u*8,1)-.5f),gy=Mathf.Abs(Mathf.Repeat(v*8,1)-.5f),seam=Mathf.Max(gx,gy)>.492f?-.022f:0;
                float band=ZoneKit.Cloud(u,v,2,61),wisp=ZoneKit.Cloud(u,v,6,17),sheen=Mathf.Pow(Mathf.Clamp01(1-Mathf.Abs(Mathf.Repeat(u*.7f+v*.4f,1)-.5f)*2.4f),4);
                float glow=ZoneKit.Smooth(.45f,.8f,band*.7f+wisp*.3f);
                var silver=new Color(.25f,.3f,.44f);var nebula=Color.Lerp(new Color(.36f,.56f,.46f),new Color(.62f,.52f,.3f),ZoneKit.Smooth(.4f,.7f,wisp));
                var tone=Color.Lerp(silver,nebula,glow*.5f)+new Color(.3f,.3f,.28f)*sheen*.8f+new Color(.06f,.07f,.08f)*(Swell(u,v)*2-1);tone+=new Color(seam,seam,seam);tone.a=1;return tone;
            });
            // Its stars, as the glass catches them: points of light a hand or two across at the pane's scale (a texel at this scale is invisible from the plane), and the sheen's
            // brightest line. The emission map, so they read in the panes' shade.
            var stars=ZoneKit.Texture("Xylos/xylos-pane-stars",512,(u,v)=>
            {
                float star=0;
                for(int layer=0;layer<2;layer++)
                {
                    int cells=layer==0?22:9;float x=u*cells,y=v*cells;int cx=Mathf.FloorToInt(x),cy=Mathf.FloorToInt(y);
                    float px=ZoneKit.Tile(cx*1.3f,cy*.7f,cells,11+layer),py=ZoneKit.Tile(cx*.9f,cy*1.7f,cells,23+layer),keep=ZoneKit.Tile(cx*2.1f,cy*2.9f,cells,37+layer);
                    if(keep<(layer==0?.55f:.7f))continue;
                    float d=new Vector2(x-cx-.2f-.6f*px,y-cy-.2f-.6f*py).magnitude,size=layer==0?.07f:.11f;
                    star=Mathf.Max(star,Mathf.Clamp01(1-d/size)*(layer==0?.7f:1));
                }
                float sheen=Mathf.Pow(Mathf.Clamp01(1-Mathf.Abs(Mathf.Repeat(u*.7f+v*.4f,1)-.5f)*2.4f),12)*.35f;
                // And a little light of its own under them, swelling with the face's, so a pane in shade is still glass that holds the light and not a gap to the dark.
                float t=Mathf.Clamp01(star+sheen+.1f+.08f*Swell(u,v));return new Color(t,t,t,1);
            });
            // The weather mirror's glass is one mirror, broken: one reflection laid across the whole disc in its own plane (XylosBuilder.Nearspace.cs, MirrorUv: MirrorSpan metres
            // from its middle to this map's edge), each pane reading it a little turned and shifted from its neighbour, as a cracked mirror's pieces do. Mapped per pane at the
            // pane's own angle, in royal blue with the farmland between, it was "a jumble of huge flat triangles" (nov26-r7-chain s17 211). What it shows is the sky it faces,
            // silvered: the zone's teal and green nebula in a few big soft shapes, darker between, sparse dim stars, and two broad bands of light across the whole disc.
            var mirrorSky=ZoneKit.Exact(ZoneKit.Texture("Xylos/xylos-mirror",1024,(u,v)=>
            {
                float x=(u*2-1)*MirrorSpan,z=(v*2-1)*MirrorSpan;
                float shape=ZoneKit.Cloud(u,v,3,41)*.75f+ZoneKit.Cloud(u,v,7,29)*.25f,hue=ZoneKit.Cloud(u,v,2,57);
                var cloud=Color.Lerp(new Color(.24f,.36f,.39f),new Color(.2f,.31f,.37f),ZoneKit.Smooth(.45f,.65f,hue));cloud=Color.Lerp(cloud,new Color(.34f,.34f,.3f),ZoneKit.Smooth(.62f,.76f,hue)*.5f);
                var tone=Color.Lerp(new Color(.17f,.27f,.31f),cloud,ZoneKit.Smooth(.38f,.68f,shape));
                // Each band full across its middle and soft over its last metres, in a faint wide halo, the first with a brighter streak down its middle: a gaussian alone faded
                // into the face and read as no sheen at all. They cross the flight camera's screen corner to corner where the second barge lies over the disc (s17 203 to 226).
                float Off(float angle,float offset)=>Mathf.Abs(x*Mathf.Cos(angle)+z*Mathf.Sin(angle)-offset);
                float wide=Off(-.3f,8),narrow=Off(-.18f,45);
                float sheen=ZoneKit.Smooth(12,5,wide)+.25f*ZoneKit.Smooth(2.5f,1,wide)+.15f*Mathf.Exp(-wide*wide/484)+.7f*ZoneKit.Smooth(6.5f,2.5f,narrow);
                // Silver on the teal, its blue a little over its green: the zone's grade warms highlights, and a band of even parts came out yellow-green. At a third more than this
                // the streak was pale sky blue at the ships' own brightness (s17 203).
                tone+=new Color(.24f,.28f,.3f)*sheen;
                // Stars: one in about three cells of seven metres, a third of a metre across.
                int cx=Mathf.FloorToInt(x/7),cz=Mathf.FloorToInt(z/7);float keep=ZoneKit.Tile(cx,cz,97,71);
                if(keep>.66f)
                {
                    var star=new Vector2((cx+.2f+.6f*ZoneKit.Tile(cx,cz,97,73))*7,(cz+.2f+.6f*ZoneKit.Tile(cx,cz,97,79))*7);
                    tone+=new Color(.26f,.28f,.28f)*Mathf.Clamp01(1-Vector2.Distance(star,new Vector2(x,z))/.36f)*(.5f+.5f*keep);
                }
                tone.a=1;return tone;
            },false));
            // The frame's struts: plates riveted along their length, weathered, a seam every metre or so.
            var strut=ZoneKit.Texture("Xylos/xylos-strut",256,(u,v)=>
            {
                float plate=Mathf.Repeat(u*4,1),rivet=new Vector2(Mathf.Repeat(u*16,1)-.5f,Mathf.Repeat(v*2,1)-.5f).magnitude,wear=ZoneKit.Cloud(u,v,6,8),scratch=ZoneKit.Tile(u*80,v*6,80,4);
                float tone=.58f+.14f*(wear-.5f)+.05f*(scratch-.5f)-(plate<.03f?.2f:0)+(rivet<.16f?.12f:0);
                return new Color(tone*.92f,tone*.96f,tone,1);
            });
            // A frond: a rib with leaflets swept toward its tip, cut out, so a crown is leaves and the gaps between them and not a green lump.
            var frond=ZoneKit.Exact(ZoneKit.Texture("Xylos/xylos-frond",512,(u,v)=>
            {
                float side=Mathf.Abs(u-.5f)*2,sweep=v*13-side*1.5f,along=sweep-Mathf.Floor(sweep)-.5f;
                // A leaflet narrows to its point at the frond's edge; the rib runs the whole length.
                float inLeaf=.42f*(1-side*side*.85f)-Mathf.Abs(along),cut=Mathf.Max(inLeaf*2.2f,(.06f-side)*8);
                float tone=.62f+.3f*Mathf.Clamp01(inLeaf*3)+(side<.06f?.1f:0)-.1f*side+.1f*(ZoneKit.Cloud(u,v,9,4)-.5f);
                return new Color(tone,tone*1.03f,tone*.94f,Mathf.Clamp01(.5f+cut*3));
            },false,true));
            // Xylos from orbit, in layers (XylosBuilder.Nearspace.cs, Planet). The land's map carries its coast in its alpha as a smooth field, cut at one level by the shader: an
            // edge as crisp as the screen however close the camera is, where a painted river went to blocks a metre across. Its colour is only the ground between forest and field.
            var land=ZoneKit.Exact(ZoneKit.Texture("Xylos/xylos-land",1024,(u,v)=>
            {
                float patch=ZoneKit.Cloud(u,v,9,5),fine=ZoneKit.Cloud(u,v,40,7);
                var tone=Color.Lerp(new Color(.52f,.58f,.27f),new Color(.3f,.46f,.22f),ZoneKit.Smooth(.35f,.65f,patch))*(.82f+.36f*fine);
                tone.a=Mathf.Clamp01(.5f+GlobeLand(u,v));return tone;
            },false,true));
            // Fields as they are from orbit: parcels with a hedge between, each its own crop and ploughed its own way. A paving laid at this scale read as a paved floor under the
            // ships. A parcel's alpha is its height, one value all over it, so where field gives way to forest or shore the shader's cut follows whole parcels.
            // Blocks between hedges, each cut into strips its own way and of its own widths, each strip a crop of its own with its rows: farmland as it looks from above. Parcels
            // round a scatter of points (Voronoi) read from the flight camera as a floor of striped hexagon tiles (s17 200 to 238, November 20).
            var fields=ZoneKit.Texture("Xylos/xylos-fields",512,(u,v)=>
            {
                const int Blocks=4;float Of(int id,int what)=>ZoneKit.Tile(id,what,97,5);
                // A low warp, periodic over the tile, so no hedge runs dead straight across it.
                float wx=u+.016f*Mathf.Sin(v*Mathf.PI*4+1.3f)+.008f*Mathf.Sin(v*Mathf.PI*10),wy=v+.016f*Mathf.Sin(u*Mathf.PI*4+.4f)+.008f*Mathf.Sin(u*Mathf.PI*14+2);
                float x=Mathf.Repeat(wx,1)*Blocks,y=Mathf.Repeat(wy,1)*Blocks;int bx=Mathf.FloorToInt(x),by=Mathf.FloorToInt(y);float fx=x-bx,fy=y-by;int block=by*Blocks+bx;
                bool alongX=Of(block,1)>.5f;int strips=3+Mathf.FloorToInt(Of(block,2)*5);float across=alongX?fy:fx,along=alongX?fx:fy,bend=.75f+.5f*Of(block,3);
                float q=Mathf.Pow(across,bend)*strips;int strip=Mathf.Min(strips-1,Mathf.FloorToInt(q));float inStrip=q-strip;int own=block*13+strip;
                float crop=Of(own,4),rows=Mathf.Sin((along*(18+14*Of(own,6))+Of(own,7))*Mathf.PI*2);
                var tone=crop<.24f?new Color(.9f,.8f,.34f):crop<.48f?new Color(.74f,.78f,.36f):crop<.7f?new Color(.6f,.68f,.3f):crop<.86f?new Color(.86f,.7f,.42f):new Color(.46f,.58f,.3f);
                tone*=.9f+.1f*rows;
                // A furrow between strips; a hedge round every block, with here and there a pale track beside it.
                if(Mathf.Min(inStrip,1-inStrip)<.05f)tone*=.82f;
                float edge=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy));
                if(edge<.022f)tone=new Color(.2f,.34f,.19f);else if(edge<.04f&&Of(block,8)>.6f)tone=new Color(.82f,.76f,.56f);
                tone.a=.3f+.5f*Of(own,5);return tone;
            },true,true);
            // The sea under it: pale over the shallows and up every river, deep offshore. Soft, as depth is.
            var sea=ZoneKit.Texture("Xylos/xylos-sea",512,(u,v)=>
            {
                float shallow=ZoneKit.Smooth(-6,-.3f,GlobeLand(u,v)),swell=ZoneKit.Cloud(u,v,11,23);
                var tone=Color.Lerp(new Color(.05f,.2f,.3f),new Color(.14f,.5f,.5f),shallow)*(.86f+.28f*swell);tone.a=1;return tone;
            },false);
            // Cloud over it: banks out of a smooth field, thinning to nothing over their last few metres rather than cut at one level (cut, they were white cut-outs laid on the
            // land), lit on the side that rises toward the sun and blue-grey on the side that falls away, a little brighter where they are thickest. Blended, not clipped.
            var cloud=ZoneKit.Exact(ZoneKit.Texture("Xylos/xylos-cloud",1024,(u,v)=>
            {
                float Bank(float x,float y)=>ZoneKit.Cloud(x,y,9,91)*.6f+ZoneKit.Cloud(x,y,23,19)*.4f;
                float here=Bank(u,v),toSun=here-Bank(u+.004f,v+.004f),thick=ZoneKit.Smooth(.62f,.8f,here);
                float lit=Mathf.Clamp01(.5f+toSun*55)*(.6f+.4f*thick);
                var tone=Color.Lerp(new Color(.6f,.66f,.74f),new Color(1,.99f,.95f),lit);tone.a=ZoneKit.Smooth(.56f,.7f,here);return tone;
            },false,true));
            Loam=ZoneKit.Toon("xylos-loam",loam,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.56f,.62f));});
            Bark=ZoneKit.Toon("xylos-bark",bark,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.66f,.5f,.36f));m.SetColor("_ShadowTint",new Color(.5f,.5f,.62f));});
            // What holds a platform's earth in: root woven like a basket, each strand as thick as an arm at the scale a wall lays its texture (7 m along, 5 m up).
            RootWall=ZoneKit.Toon("xylos-root-wall",weave,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.6f,.46f,.33f));m.SetColor("_ShadowTint",new Color(.5f,.5f,.62f));m.SetTextureScale("_BaseMap",new Vector2(1.4f,1.25f));});
            Plank=ZoneKit.Toon("xylos-plank",plank,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.55f,.52f,.64f));});
            Thatch=ZoneKit.Toon("xylos-thatch",thatch,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.5f,.56f,.6f));});
            Leaf=ZoneKit.Toon("xylos-leaf",leaf,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);ZoneKit.Swaying(m,.05f,1.4f,1.2f);m.SetColor("_ShadowTint",new Color(.46f,.58f,.56f));});
            // A crown's fronds: cut out, seen from both sides, and moving, which is most of what makes a mass of leaves read as leaves.
            CanopyLeaf=ZoneKit.Toon("xylos-canopy",frond,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);ZoneKit.Swaying(m,.16f,1.1f,4,.5f);ZoneKit.With(m,"_ALPHATEST_ON","_AlphaTest");m.SetFloat("_Cutoff",.5f);
                m.SetColor("_ShadowTint",new Color(.42f,.56f,.52f));m.SetFloat("_LightFloor",.25f);});
            Weave=ZoneKit.Toon("xylos-weave",weave,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.8f,.64f,.44f));});
            Cloth=ZoneKit.Toon("xylos-cloth",null,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);});
            Husk=ZoneKit.Toon("xylos-husk",husk,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.86f,.66f,.38f));m.SetColor("_RimColor",new Color(.5f,.42f,.3f));m.SetFloat("_LightFloor",.3f);});
            Vine=ZoneKit.Toon("xylos-vine",bark,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.56f,.8f,.34f));m.SetFloat("_LightFloor",.3f);});
            // Every material a fading piece is made of carries the fade's variant from its paint, so no later paint of it can take it away (D359: these drew their pieces solid while the rest faded).
            foreach(var fadeMat in new[]{Bark,Vine}){ZoneKit.With(fadeMat,"_OCCFADE_ON","_OccFadeToggle");fadeMat.SetFloat("_OccFade",0);}
            // Snagvine is the river's own weed gone up with the storm: bark-skinned, deep green going to umber, darker than a barge's bright lashings, with a bud of violet light
            // in every knot (the colour of the knots that are alive). Nothing about it is the flat magenta of a shader that failed to load.
            Snag=ZoneKit.Toon("xylos-snag",bark,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.5f,.52f,.36f));m.SetColor("_RimColor",new Color(.26f,.16f,.36f));m.SetFloat("_LightFloor",.3f);});
            // Lit and shaded as a bud is, with a blue-violet glow under it: at red .8 to blue 1.5 the buds came out flat pink-magenta discs, the colour this was meant never to be.
            SnagGlow=ZoneKit.Toon("xylos-snag-glow",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.42f,.26f,.8f));m.SetColor("_EmissionColor",new Color(.2f,.06f,.6f));m.SetFloat("_ReceiveShadows",0);});
            // Seed lanterns and a caught sun: lit from inside, so they read in shade and bloom a little.
            Lantern=ZoneKit.Toon("xylos-lantern",null,m=>{m.SetColor("_BaseColor",new Color(1,.78f,.42f));m.SetColor("_EmissionColor",new Color(2.2f,1.3f,.45f));});
            // Rootmarket's press fades in these three (SceneryOcclusion). The fade's variant is theirs from the paint: given by a repair alone, any Xylos zone's rebuild took it off them again
            // (D405: D393's rebuild of the Walking Canopy did, and the press drew solid; SceneFadeMaterialTests, suite nov24b).
            foreach(var fadeMat in new[]{Plank,Cloth,Lantern}){ZoneKit.With(fadeMat,"_OCCFADE_ON","_OccFadeToggle");fadeMat.SetFloat("_OccFade",0);}
            // The glass lit mostly by what it reflects: a high light floor and its stars in the emission, so a pane in the ship's shadow is still sky.
            Pane=ZoneKit.Toon("xylos-pane",pane,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_BaseColor",new Color(.82f,.86f,.98f));m.SetTexture("_EmissionMap",stars);m.SetColor("_EmissionColor",new Color(.9f,.92f,1.05f));
                m.SetFloat("_LightFloor",.62f);m.SetFloat("_ReceiveShadows",0);m.SetColor("_RimColor",new Color(.5f,.56f,.7f));});
            // The pane that still catches the sun: warm and bright, but under white, so its cells and the streak across it still show (at full white it was a blank cut out of the frame).
            // Its base is half what it was when the glass's ground was navy (the silvered ground is about that much brighter), so it stays as warm and as far under white.
            // It reads the mirror's one reflection, as its neighbours do (its emission the same map, at about the light the stars gave it).
            PaneLit=ZoneKit.Toon("xylos-pane-lit",mirrorSky,m=>{ZoneKit.TwoSided(m);m.SetColor("_BaseColor",new Color(.5f,.43f,.3f));m.SetTexture("_EmissionMap",mirrorSky);m.SetColor("_EmissionColor",new Color(.8f,.62f,.34f));m.SetFloat("_ReceiveShadows",0);});
            // A pane's cut edge: the cool pale line cut glass shows along its edges, so a pane or a shard is a piece of glass and not a hole in the picture. A little light of its own
            // keeps it pale in shade; under the bloom's threshold, so it is a line and not neon.
            PaneEdge=ZoneKit.Toon("xylos-pane-edge",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.7f,.77f,.88f));m.SetColor("_EmissionColor",new Color(.06f,.07f,.1f));
                m.SetFloat("_LightFloor",.55f);m.SetFloat("_ReceiveShadows",0);m.SetColor("_RimColor",new Color(.6f,.66f,.78f));});
            // The weather mirror's own glass and its loose panes: the one reflection above, white in the material so the map and the vertices say everything, a little light of its own
            // so the bands hold in any light, no keyword but the vertex colour every world material here has. The stream's shards keep Pane (they passed, nov26-r7-chain s17 123, 298).
            Mirror=ZoneKit.Toon("xylos-mirror",mirrorSky,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_BaseColor",Color.white);m.SetTexture("_EmissionMap",mirrorSky);m.SetColor("_EmissionColor",new Color(.16f,.16f,.16f));
                m.SetFloat("_LightFloor",.62f);m.SetFloat("_ReceiveShadows",0);m.SetColor("_RimColor",new Color(.24f,.3f,.3f));});
            Strut=ZoneKit.Toon("xylos-strut",strut,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.62f,.66f,.74f));m.SetColor("_ShadowTint",new Color(.42f,.46f,.6f));m.SetColor("_RimColor",new Color(.56f,.6f,.72f));m.SetFloat("_LightFloor",.3f);});
            // The planet's layers. Every point of them faces straight up, so the sun lights the whole of each evenly (a globe under the toon ramp went to two flat bands with a hard
            // line between them) and the limb darkens in the vertices. Fields are painted above; forest and shore are the ground kit's cobble and sand at a scale where a cobble
            // is a tree's crown.
            Texture2D Layer(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(GroundKit.Art+"ground-"+name+".png");
            void Flat(Material m){m.SetFloat("_ReceiveShadows",0);m.SetColor("_RimColor",Color.black);m.SetFloat("_RampSteps",2);}
            Land=ZoneKit.Toon("xylos-land",land,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_SPLAT_ON","_Splat");ZoneKit.With(m,"_ALPHATEST_ON","_AlphaTest");m.SetFloat("_Cutoff",.5f);Flat(m);
                m.SetTexture("_SplatMap1",fields);m.SetTexture("_SplatMap2",Layer("cobble"));m.SetTexture("_SplatMap3",Layer("sand"));
                m.SetColor("_SplatTint1",new Color(.7f,.68f,.6f,1));m.SetColor("_SplatTint2",new Color(.12f,.28f,.16f,1));m.SetColor("_SplatTint3",new Color(.74f,.68f,.46f,1));
                m.SetVector("_SplatScale",new Vector4(1/44f,1/12f,1/11f,.3f));m.SetVector("_SplatEdge",new Vector4(.03f,.34f,.4f,.4f));m.SetColor("_BaseColor",new Color(.78f,.78f,.78f));});
            // The same land a little lower and a little wider, dark: the bank a coast has, and the line a cel painter would ink along it.
            LandBank=ZoneKit.Toon("xylos-land-bank",land,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_ALPHATEST_ON","_AlphaTest");m.SetFloat("_Cutoff",.45f);Flat(m);m.SetFloat("_BaseMapStrength",0);m.SetColor("_BaseColor",new Color(.07f,.13f,.11f));});
            Sea=ZoneKit.Toon("xylos-sea",sea,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_SPLAT_ON","_Splat");Flat(m);m.SetTexture("_SplatMap1",Layer("sand"));m.SetColor("_SplatTint1",new Color(.4f,.8f,.76f,.3f));
                m.SetColor("_SplatTint2",new Color(1,1,1,0));m.SetColor("_SplatTint3",new Color(1,1,1,0));m.SetVector("_SplatScale",new Vector4(1/9f,1,1,.3f));m.SetVector("_SplatEdge",new Vector4(.04f,0,0,0));m.SetColor("_BaseColor",new Color(.8f,.8f,.8f));});
            void Blended(Material m,int queue){m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);
                m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent+queue;}
            Clouds=ZoneKit.Toon("xylos-cloud",cloud,m=>{ZoneKit.Tinted(m);Flat(m);m.SetFloat("_LightFloor",.85f);m.SetColor("_BaseColor",new Color(.9f,.93f,.92f,.94f));Blended(m,-35);});
            // A cloud's shadow on the land under it: the same banks, dark and thin and as soft at the edge, drawn after everything solid and before the cloud.
            CloudShade=ZoneKit.Toon("xylos-cloud-shade",cloud,m=>{Flat(m);m.SetFloat("_BaseMapStrength",0);m.SetColor("_BaseColor",new Color(.02f,.07f,.09f,.3f));Blended(m,-40);});
            Water=ZoneKit.Toon("xylos-water",null,m=>ZoneKit.WaterLook(m,new Color(.34f,.62f,.52f,.62f),new Color(.07f,.3f,.3f,.94f),new Color(.78f,.92f,.84f,1),2.4f,.5f,.3f,.3f));
            // Rope and vine drawn as lines: unlit, so a line's facing can't shade it.
            Rope=ZoneKit.Toon("xylos-rope",null,m=>m.SetColor("_BaseColor",new Color(.78f,.66f,.44f)),"Universal Render Pipeline/Unlit");
            // What an earlier build made and nothing uses now.
            foreach(var gone in new[]{"Assets/_Project/Resources/WorldMaterials/xylos-globe.mat","Assets/_Project/Resources/WorldMaterials/xylos-snag-line.mat","Assets/_Project/Art/Generated/Xylos/xylos-globe.png"})
                if(File.Exists(gone))AssetDatabase.DeleteAsset(gone);
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- Tallow approach: the warp out
        /// <summary>The lane Pell's lantern learns. Replaces only its own warp, spawn and sign in the Tallow Approach scene, as chapter 2's does; re-run after that scene's own rebuild or staging.</summary>
        static void StageTallowApproach()
        {
            var approach=EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowApproach.unity");
            foreach(var old in UnityEngine.Object.FindObjectsByType<WarpBeacon>(FindObjectsSortMode.None).Where(w=>w.scene=="XylosNearspace").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(var old in UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Where(s=>s.id=="Xylos").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            ChapterOneStaging.ClearSigns("Warp — Xylos Nearspace");
            // South-east of the arrival, clear of the Halo's warp (west) and the Wick's (east): the three stand in an arc round where a ship comes in.
            var warp=WorldBuilder.Warp("Warp — Xylos Nearspace",new Vector3(11,1,-37),"XylosNearspace","xylos.charted","Arrival");
            warp.lockedPrompt="No lane to Xylos yet. Pell's lantern has to learn it first.";
            WorldBuilder.Spawn("Xylos",new Vector3(6,1,-30));
            ChapterOneStaging.Approach();
            EditorSceneManager.MarkSceneDirty(approach);EditorSceneManager.SaveScene(approach);Debug.Log("TALLOW_APPROACH_XYLOS_OK");
        }
    }
}
