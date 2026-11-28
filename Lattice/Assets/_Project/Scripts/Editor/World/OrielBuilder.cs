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
    /// Chapter 7, The Cage Makers (brief: docs/chapters/ch07-the-cage-makers.md; script wiring: docs/writing/ch07-manifest.md). Isolated builder for its definitions and three zones,
    /// on <see cref="ZoneKit"/>. The chapter has no town: its people are met in the zones, and Ishen's camp at the landing is the safe pocket.
    /// <list type="bullet">
    /// <item>The Orrery (space combat, <c>OrielBuilder.Orrery.cs</c>): Oriel's broken moons on their light-rails, and the rings of rail-gates they open and shut in turn. Rule: one
    /// readable machine. Map brief: docs/maps/Orrery.md.</item>
    /// <item>The Skyfields (wild, <c>OrielBuilder.Skyfields.cs</c>): the second world engineer's. Ishen's camp, the lift nodes, the junctions, the gardens.</item>
    /// <item>The Glass Choir (dungeon and the Last Warden's engine hall, <c>OrielBuilder.GlassChoir.cs</c>): the third world engineer's.</item>
    /// </list>
    /// Enemies and the Last Warden are in <c>OrielBuilder.Enemies.cs</c> (the combat engineer's). Kit pieces from batch C14-Oriel (tools/c14_intake.py). Members of every file share one
    /// class, so each zone's own helpers carry its name or live in a nested class.
    /// Runs: <c>Definitions</c>, <c>BuildZones</c> with <c>-oriel-zones Orrery,OrielSkyfields,GlassChoir</c>, <c>Dialogue</c>, <c>StageApproach</c>; then <c>ZoneLook.Apply</c> for what was built.
    /// </summary>
    public static partial class OrielBuilder
    {
        internal const string OrreryZone="Orrery",SkyfieldsZone="OrielSkyfields",ChoirZone="GlassChoir";
        public static readonly string[] Zones={OrreryZone,SkyfieldsZone,ChoirZone};
        public static void Definitions()=>BatchTools.Run(()=>{Define();AssetDatabase.SaveAssets();Debug.Log("ORIEL_DEFS_OK");});
        public static void BuildZones()=>BatchTools.Run(()=>
        {
            string arg=DevArgs.Value("-oriel-zones");var zones=string.IsNullOrEmpty(arg)?Zones:arg.Split(',',StringSplitOptions.RemoveEmptyEntries);
            Define();foreach(var zone in zones)BuildZone(zone.Trim());
            BatchTools.RegisterScenes();AssetDatabase.SaveAssets();Debug.Log("ORIEL_ZONES_OK "+string.Join(",",zones));
        });
        /// <summary>Copies docs/writing/*.yarn into Resources and recompiles the Yarn project: a new .yarn file is not in the compiled program until the project reimports, and a missing
        /// node fails silently at runtime.</summary>
        public static void Dialogue()=>BatchTools.Run(()=>{RefreshDialogue();Debug.Log("ORIEL_DIALOGUE_OK");});
        /// <summary>Every node of the chapter's script (manifest section 2): one that didn't compile stops the build here instead of doing nothing when talked to.</summary>
        static readonly string[] Nodes=
        {
            "IshenArrives","AbriTooManyBooks","IshenMoonNames","IshenLanding","IshenCamp","IshenMoonsOffer","IshenMoonsReturn","LumeFirst","LumeRepeat","LumeLanternOffer","IdraFarLantern",
            "LumeLanternReturn","LumePost","IdraGlassForest","IdraEmptyGarden","IshenJunction","IdraGardenerFollows","IdraGardenerWaits","IdraSweptStair","IdraSealedGarden","IdraChoirDoor",
            "SelaGlassChoir","IshenFirstClamp","WardenFirst","WardenAsks","IshenReads","IshenOrielPost","MeretOrielCall","LumeBrekksWord","IshenAboard","RuvaGardener","IdraGardenerAboard","PellOrielLantern",
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
                case OrreryZone:BuildOrrery();break;
                case SkyfieldsZone:BuildSkyfields();break;
                case ChoirZone:BuildGlassChoir();break;
                default:throw new InvalidOperationException("Unknown Oriel zone "+zone);
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
            // Oriel in its long afternoon: a pale gold sun through clear air, a little more bloom than Hoarfell for the fibres and the glass, no tilt (D060).
            var gold=ArenaBuilder.Asset<Hd2dProfile>("Oriel",p=>{p.tint=new Color(1,.98f,.94f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.62f;});
            // The Orrery: the rail lights and the gates' lamps are the machine's state, so they are what blooms.
            var rails=ArenaBuilder.Asset<Hd2dProfile>("Rails",p=>{p.tint=new Color(.98f,.98f,1);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.72f;});
            var source=ZoneKit.WorldMaterial("tallow-sky");
            // The panorama turned to a part no earlier lane shows and warmed toward old brass: the Orrery is neither the Shear's teal, the Veil's green-violet nor the Wick's amber.
            var space=ZoneKit.Sky("oriel-orrery-sky",source,new Color(.58f,.54f,.5f,.5f),203,.7f);
            // Over the Skyfields the sky is the frame's far edge: a high pale gold-blue.
            var day=ZoneKit.Sky("oriel-day-sky",source,new Color(.78f,.8f,.84f,.5f),35,1.8f);
            // Enemy damage is authored at its real weight (OrielBuilder.Enemies.cs), so every zone's threat is 1.
            ArenaBuilder.Asset<ZoneDef>(OrreryZone,z=>{z.id=z.scene=OrreryZone;z.displayName="The Orrery";z.chapter=7;z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=rails;z.skybox=space;
                z.enemyThreat=1;z.flightPlane=1;z.cameraClearsScenery=false;z.musicId="Orrery Nine Moons";z.fightMusicId="Starfight";
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(OrreryBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=OrreryLevel;}});
            ArenaBuilder.Asset<ZoneDef>(SkyfieldsZone,z=>{z.id=z.scene=SkyfieldsZone;z.displayName="The Skyfields";z.chapter=7;z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=gold;z.skybox=day;
                z.enemyThreat=1;z.cameraClearsScenery=true;z.musicId="Glass Ruans";
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(SkyfieldsBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=SkyfieldsLevel;}
                // What the Skyfields' own engineer sets on its definition (a pocket's music, a bright floor): OrielBuilder.Skyfields.cs.
                SkyfieldsDefinition(z);});
            ArenaBuilder.Asset<ZoneDef>(ChoirZone,z=>{z.id=z.scene=ChoirZone;z.displayName="The Glass Choir";z.chapter=7;z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=gold;z.skybox=day;
                z.enemyThreat=1;z.cameraClearsScenery=true;z.musicId="Glass Ruans";
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(ChoirBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=ChoirLevel;}
                // What the Glass Choir's own engineer sets on its definition (its profile, its sky or none): OrielBuilder.GlassChoir.cs.
                GlassChoirDefinition(z);});

            DefineEnemies();
            Quests();

            // Ishen's camp on Sela's chart (guide 3.10): charted by arriving, joined to Many Hands (whose hull the lane leaves from), in the fourth column at the foot. The camp is
            // the Skyfields' safe pocket at the landing; its spawn is the Skyfields' Arrival (docs/maps/OrielSkyfields.md, "For OrielBuilder.cs").
            ArenaBuilder.Asset<ChartStop>(SkyfieldsZone,s=>{s.id=SkyfieldsZone;s.displayName="Ishen's Camp";s.region="Oriel";s.blurb="The archivist's camp at the landing on Oriel: Lume's stall and the last lantern, a bench and a mending cradle.";
                s.zone=SkyfieldsZone;s.spawn="Arrival";s.position=new Vector2(.76f,.02f);s.routes=new[]{"ManyHands"};s.shop=s.bench=s.repair=true;s.chapter=7;
                s.reaches=new[]{OrreryZone,ChoirZone};});

            // The new speakers (manifest section 5). Ishen, Meret, Idra, Abri, Ruva and Pell already have definitions and keep them. The gardeners never speak and have none.
            void Person(string id,string display,bool female,GameObject body)
                =>ArenaBuilder.Asset<CharacterDef>(id,c=>{c.id=c.speakerId=id;c.displayName=display;c.female=female;c.natural=body;
                    // The sheets were taken in before these definitions existed, so PortraitIntake had nothing to bind them to.
                    if(c.portraitNatural==null)c.portraitNatural=AssetDatabase.LoadAssetAtPath<PortraitSet>("Assets/_Project/Resources/Portraits/"+id+"_Natural.asset");});
            Person("Lume","Lume",true,FirstBody("LumeBody","PellBody"));
            // The Warden's body is the boss's own (Enemies/LastWarden): it is never anyone else, and it talks where it fights.
            Person("Warden","The Last Warden",false,ZoneKit.Prefab("Enemies","LastWarden"));

            // One counter at the camp (manifest 8.11), the Skyfields' own, opened by Lume: what the lantern chain's far end carries. Provisions, the chain's Tempered stores, and
            // Oriel's two commons.
            ProgressionBuilder.Stall(SkyfieldsZone,"LUME'S STALL",Sell("RepairGel",26),Sell("RepairSalve",64),Sell("ChargeCell",30),Sell("ChargeBank",64),Sell("SyncTonic",96),Sell("RelinkSpike",96),
                Sell("SurgeDraught",54),Sell("PlatingWax",54),Sell("Quickening",54),
                Sell("EmittersT4",920),Sell("EdgesT4",980),Sell("FramesT4",1010),Sell("DrivesT4",880),Sell("ModulesT4",1040),
                Sell("ScrapAlloy",16,3),Sell("FibreThread",99,3),Sell("SingingGlass",108,3));
        }

        /// <summary>The four quests, step for step as the script's manifest has them (section 4). A step the world reports (the far gate passed, a named gate read, a clamp opened)
        /// is an Interact step on the report of the point that stands there, so the objective's arrow has something to find and the step moves the moment it happens: a flag set by
        /// a point rather than by <c>&lt;&lt;setFlag&gt;&gt;</c> would wait for the next load. The reports each zone stands are in its map doc, under "For OrielBuilder.cs".</summary>
        static void Quests()
        {
            ArenaBuilder.Asset<QuestDef>("CageMakers",q=>{q.id="CageMakers";q.title="The Cage Makers";q.main=true;q.chapter=7;
                q.description="The Muster left Hoarfell with the plans for a cage. Ishen says they were first drawn on Oriel, and that the way to open one is there if it is anywhere.";
                q.steps=new[]{Step(ObjectiveKind.Reach,OrreryZone),Step(ObjectiveKind.Interact,"FarGate"),Step(ObjectiveKind.Flag,"oriel.ruleSeen"),Step(ObjectiveKind.Reach,ChoirZone),
                    Step(ObjectiveKind.Flag,"oriel.choirSeen"),Step(ObjectiveKind.Flag,"oriel.releaseLearned"),Step(ObjectiveKind.Interact,"Clamp",4),Step(ObjectiveKind.Flag,"oriel.wardenStoodDown"),
                    Step(ObjectiveKind.Flag,"oriel.coronachHeard"),Step(ObjectiveKind.Flag,"oriel.meretHeard"),Step(ObjectiveKind.Flag,"oriel.brekkHeard")};
                q.stepText=new[]{"Take the lane to Oriel","Cross the Orrery on its own time: wait in a moon's lee, go on green","Land at Ishen's camp",
                    "Cross the Skyfields by lift node and junction","Walk the nave","Follow the body down to the first clamp",
                    "Open the four clamps in the engine hall: take the weight, wait for the slack","Answer the Warden","Hear Ishen read the court's record",
                    "Go back to the camp","Read the lantern with Lume"};
                // Step 1's arrow finds the far rail-gate (report FarGate, the Orrery's own); the rest find what the Skyfields and the Glass Choir stand under the reports their map
                // docs give (Landing, Nave, FirstClamp, Clamp, RecordWall, CampReturn).
                q.stepTargets=new[]{At(OrreryZone,null,null,null,null,"ManyHandsHull"),At(OrreryZone,null,"FarGate"),At(SkyfieldsZone,null,"Landing",null,null,OrreryZone),
                    At(ChoirZone,null,null,null,null,SkyfieldsZone),At(ChoirZone,null,"Nave",null,null,SkyfieldsZone),At(ChoirZone,null,"FirstClamp",null,null,SkyfieldsZone),
                    At(ChoirZone,null,"Clamp",null,null,SkyfieldsZone),At(ChoirZone,"Warden",null,null,null,SkyfieldsZone),At(ChoirZone,null,"RecordWall",null,null,SkyfieldsZone),
                    At(SkyfieldsZone,null,"CampReturn",null,null,ChoirZone),At(SkyfieldsZone,"Lume",null,null,null,ChoirZone)};
                q.closingLine="Chapter 7 complete. The first Bridle is open. Brekk has word from Scoria: he's on Many Hands' dock.";
                // Where chapter 8 begins (Brekk's word, walked into stepping up off Many Hands' dock: HarrowgateBuilder's staging): home from the camp by the Orrery and the hull,
                // or by the chart, whose diamond stands on Many Hands meanwhile.
                q.next=At("ManyHands",null,null,null,null,"ManyHandsHull",OrreryZone,SkyfieldsZone);q.next.hasPoint=true;q.next.point=new Vector3(0,0,-44.4f);
                // The chapter's card waits for Lume's reading of Brekk's lamp, the closing talk at the camp. The manifest's body (section 6) runs to three lines of the card; this is
                // its first two sentences, which are the chapter.
                q.cardFlag="chapter7.complete";
                q.cardBody="Take its weight, wait for the slack, let go together: the first Bridle is open, a thousand years late, and the Last Warden has stood down. On Scoria the Muster's cage is nearly built.";
                // What the story has set by its end that the steps above don't say: the world's own events on the main path, who has been met and heard. A later chapter's fixture
                // starts with these (DevLoadout). The Skyfields' and the Choir's are as their map docs give them ("For OrielBuilder.cs").
                q.storyFlags=new[]{"oriel.charted","oriel.podHailed","oriel.lightsRead","oriel.crossed",
                    "oriel.landed","met.Lume","oriel.gardenSeen","oriel.junctionRead","oriel.junction1","oriel.followerSeen","oriel.junction2","oriel.junction3","oriel.doorReached",
                    "oriel.stay1","oriel.stay2","oriel.stay3","oriel.wardenMet","oriel.clamp1","oriel.clamp2","oriel.clamp3","oriel.clamp4","bossdown.LastWarden",
                    "oriel.coronachHeard","oriel.meretHeard","oriel.brekkHeard","chapter7.complete"};
                // Placeholder numbers until the systems stream sets the chapter's rewards: a Tempered part of the party's own luck and something to mend with on the way home. The
                // release is not an item: it is three lines the party can say.
                q.rewardXp=1800;q.rewardScrip=1000;q.rewards=new[]{new Ingredient{id="FramesT4",count=1},new Ingredient{id="RepairSalve",count=2}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("EmptyGardens",q=>{q.id="EmptyGardens";q.title="The Empty Gardens";q.chapter=7;
                q.description="A gardener has followed twelve paces behind since the landing. At a frayed junction, under a garden that was sagging, it put down its rake and held out its hands.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"GardenJunction",3),Step(ObjectiveKind.Interact,"SealedGarden")};
                q.stepText=new[]{"Seat the three frayed garden junctions, and cut none of them","Follow the gardener"};
                q.stepTargets=new[]{At(SkyfieldsZone,null,"GardenJunction",null,"Seat the junctions"),At(SkyfieldsZone,null,"SealedGarden",null,"Follow the gardener")};
                q.rewardXp=320;q.rewardScrip=180;q.rewards=new[]{new Ingredient{id="ModulesMasteryLens",count=1}};q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("MoonNames",q=>{q.id="MoonNames";q.title="The Names of the Moons";q.chapter=7;
                q.description="Three of the Orrery's rail-gates have their moon's name cut on the keystone, and Ishen has brought a pencil.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"NamedGate",3),Step(ObjectiveKind.Flag,"ishen.moonsRead")};
                q.stepText=new[]{"Pass the three named rail-gates on green, slowly enough to be read","Let Ishen show you what he took down"};
                q.stepTargets=new[]{At(OrreryZone,null,"NamedGate",null,"Pass the named gates",SkyfieldsZone),At(SkyfieldsZone,"Ishen",null,null,"Go back to Ishen",OrreryZone)};
                q.rewardXp=300;q.rewardScrip=170;q.rewards=new[]{new Ingredient{id="DrivesT4",count=1}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("LumesLantern",q=>{q.id="LumesLantern";q.title="The Far End";q.chapter=7;
                q.description="The lantern chain once ended on the highest island of the Skyfields. Lume can't leave the lantern she keeps, so she has lit its twin from it.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"LanternSeat"),Step(ObjectiveKind.Flag,"lume.lanternLit")};
                q.stepText=new[]{"Carry the lantern to the highest island and set it in the old seat","Tell Lume it's lit"};
                q.stepTargets=new[]{At(SkyfieldsZone,null,"LanternSeat",null,"Carry the lantern up"),At(SkyfieldsZone,"Lume",null,null,"Tell Lume it's lit")};
                q.rewardXp=300;q.rewardScrip=170;q.rewards=new[]{new Ingredient{id="EmittersT4",count=1}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
        }

        // ---------------------------------------------------------------- light
        static Color C(float r,float g,float b)=>new(r,g,b,1);
        /// <summary>The Orrery's authored light and grade (ZoneLook finds this by its attribute). The Skyfields' and the Glass Choir's are registered from their own files.</summary>
        [ZoneLook.Source] static void Looks()
        {
            // Over Oriel: a low white-gold sun off the planet's limb, a warm brass fill so a hull's shaded side and a moon's dark face read against the dark, and enough bloom that the
            // rail lights read as light and not as paint.
            ZoneLook.Register(OrreryZone,new ZoneLook.Spec{sun=C(1f,.95f,.86f),sunIntensity=1.3f,shadowStrength=.8f,
                sky=C(.36f,.36f,.4f),equator=C(.3f,.27f,.24f),ground=C(.12f,.11f,.12f),
                contrast=14,saturation=6,splitShadows=C(.42f,.44f,.56f),splitHighlights=C(.58f,.52f,.42f),
                bloomThreshold=.85f,bloomIntensity=.8f,vignette=.24f});
        }

        // ---------------------------------------------------------------- Many Hands: the way in and the way home
        /// <summary>The way in from chapter 6 and the way home (StageManyHands): re-run after Many Hands' own rebuild (ManyHandsBuilder.BuildZones) and after HoarfellBuilder.StageApproach.</summary>
        [Restage.Step("chapter7.many-hands","ManyHands,ManyHandsHull",After="chapter6.many-hands",Chapter=7,Leaves=new[]{"ManyHands: Oriel: Ishen on the dock #1","ManyHands: Oriel: Ishen arrives #1","ManyHands: Ishen arrives <DiscoveryPoint> #1","ManyHands: Oriel: Ishen aboard #1","ManyHands: Oriel: the gardener aboard #1","ManyHands: The gardener at work <DiscoveryPoint> #1","ManyHandsHull: Oriel: warp to the Orrery <WarpBeacon> #1","ManyHandsHull: Sign_Warp — The Orrery #1","ManyHandsHull: Oriel: warp sign #1","ManyHandsHull: Oriel: spawn from the Orrery <SpawnPoint> #1"})]
        public static void StageApproach()=>BatchTools.Run(()=>{Define();StageManyHands();AssetDatabase.SaveAssets();Debug.Log("ORIEL_APPROACH_OK");});
        /// <summary>Where chapter 7 stands in chapter 5's two scenes, and only that: each piece is named "Oriel: ..." and a rerun destroys what it made before. In the city: Ishen and his
        /// handcart of books on the dock from the moment Idra's cutters are home, and his talk as a crossing just past the lift figures (IshenArrives, which starts the chapter); once
        /// the chapter is done, Ishen in the corridor he calls the archive (IshenAboard) and, with the Empty Gardens done, the gardener at work in the seed garden with Idra's look
        /// at it (IdraGardenerAboard). Outside: the warp to the Orrery off the stern, beside Hoarfell's, locked until Ishen has told the party where the plans were drawn, and its spawn.</summary>
        static void StageManyHands()
        {
            const string Mark="Oriel: ";
            // Many Hands' city class first, then the places it keeps for arrivals, read in a method of their own: its Hands and Hands.Arrivals initialise each other, and with the
            // arrivals touched first (by naming them in this method) Hands found one of their lists still unmade and threw.
            _=ManyHandsBuilder.Hands.FromWays(Vector2.zero);var (archiveAt,archiveDesk,gardenAt,gardenBed,gardenLook)=ArrivalPlaces();
            void Clear(){foreach(var go in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t!=null&&t.parent==null&&t.name.StartsWith(Mark)).Select(t=>t.gameObject).ToArray())UnityEngine.Object.DestroyImmediate(go);}
            Vector3 Floor(Vector2 p)
            {
                Physics.SyncTransforms();
                var hits=Physics.RaycastAll(new Vector3(p.x,60,p.y),Vector3.down,120,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).ToArray();
                if(hits.Length==0)throw new InvalidOperationException("Oriel staging: no floor under "+p+" in "+EditorSceneManager.GetActiveScene().name);
                return hits[0].point;
            }
            void OffTheWays(Vector2 p,string who){float d=ManyHandsBuilder.Hands.FromWays(p);if(d<2)throw new InvalidOperationException("Oriel staging: "+who+" at "+p+" stands "+d.ToString("0.0")+" m from a way people walk");}
            GameObject Holder(string name)=>new(Mark+name);
            void Where(GameObject holder,string from,string goneAt,string awayFrom,string backAt,params GameObject[] who)
            {
                var where=holder.AddComponent<Whereabouts>();where.from=from??"";where.goneAt=goneAt??"";where.awayFrom=awayFrom??"";where.backAt=backAt??"";where.live=false;where.targets=who;
            }
            GameObject Body(string id,Vector3 at,float yaw,Transform under)
            {
                var prefab=ZoneKit.Prefab("Characters",id);if(prefab==null)throw new InvalidOperationException("Oriel staging: no body "+id);
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.transform.SetParent(under,false);body.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
                var solid=body.AddComponent<BoxCollider>();solid.size=new Vector3(.8f,2,.8f);solid.center=Vector3.up;
                foreach(var rig in body.GetComponentsInChildren<Animator>())rig.cullingMode=AnimatorCullingMode.CullCompletely;
                return body;
            }

            // ---- the city
            var city=EditorSceneManager.OpenScene("Assets/_Project/Scenes/ManyHands.unity");Clear();
            // Ishen on the dock with too many books, from the moment Idra's cutters are home until he has said where the plans were drawn (and is gone with the party).
            // Where Many Hands' map brief keeps room for each later arrival ("Room for later arrivals", and ManyHandsBuilder.Hands.Arrivals for the places it builds): the dock's west end
            // for Ishen and his handcart, the archive's lane for him afterwards, the aisle by Marrow-1's planted bed for the gardener.
            var dockHolder=Holder("Ishen on the dock");var ishenAt=new Vector2(-20,-61);OffTheWays(ishenAt,"Ishen on the dock");
            var ishen=Body("IshenBody",Floor(ishenAt),130,dockHolder.transform);ishen.name="Ishen with his books";
            var cart=new GameObject("Ishen's handcart");cart.transform.SetParent(dockHolder.transform,false);var cartAt=Floor(ishenAt+new Vector2(-1.4f,-1.1f));
            foreach(var (offset,size,yaw) in new[]{(new Vector3(0,.35f,0),new Vector3(1.3f,.7f,.9f),10f),(new Vector3(-.15f,.95f,.05f),new Vector3(.9f,.5f,.7f),24f),(new Vector3(.2f,1.4f,-.1f),new Vector3(.6f,.4f,.5f),-8f)})
            {
                var crate=WorldBuilder.Piece("DeckCrate",Vector3.zero,size,"Rock",false);crate.transform.SetParent(cart.transform,false);crate.transform.SetPositionAndRotation(cartAt+offset,Quaternion.Euler(0,yaw,0));crate.name="Books";
            }
            var cartSolid=cart.AddComponent<BoxCollider>();cartSolid.center=cartAt+Vector3.up*.8f-cart.transform.position;cartSolid.size=new Vector3(1.5f,1.6f,1.1f);
            Where(dockHolder,"chapter6.complete","oriel.charted",null,null,ishen,cart);
            // His talk: walked into stepping up off the dock, just past where the lift figures are told, so the one follows the other.
            var talkHolder=Holder("Ishen arrives");var arrives=new GameObject("Ishen arrives",typeof(DiscoveryPoint),typeof(BoxCollider));arrives.transform.SetParent(talkHolder.transform,false);arrives.transform.position=Floor(new Vector2(0,-49.4f));
            var point=arrives.GetComponent<DiscoveryPoint>();point.prompt="Ishen arrives";point.flag="oriel.charted";point.report="";point.situation="";point.dialogueNode="IshenArrives";point.dialogueSpeaker="Ishen";
            point.requiredQuest="";point.requiredStep=0;point.requiredFlag="chapter6.complete";point.range=2;point.startsOnCrossing=true;
            var mouth=arrives.GetComponent<BoxCollider>();mouth.isTrigger=true;mouth.size=new Vector3(16,5,3);mouth.center=Vector3.up*2.5f;
            // Ishen aboard once the chapter is done: the corridor on the lamp gallery's west end he has called the archive.
            var archiveHolder=Holder("Ishen aboard");
            var aboard=WorldBuilder.Npc("Ishen",Floor(archiveAt),"IshenAboard");aboard.transform.SetParent(archiveHolder.transform,true);
            var desk=archiveDesk-archiveAt;aboard.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(desk.x,desk.y)*Mathf.Rad2Deg,0);
            aboard.firstNode=aboard.repeatNode="IshenAboard";aboard.postNode="";aboard.postFlag="";aboard.finalFlag=aboard.finalNode=null;aboard.name="Ishen aboard";
            Where(archiveHolder,"chapter7.complete",null,null,null,aboard.gameObject);
            // The gardener in the seed garden, working a planted bed by Ruva's, once the Empty Gardens are done and the party is home (here only with both: from one, and away from
            // that one until the other).
            var gardenHolder=Holder("the gardener aboard");var bed=gardenBed-gardenAt;
            var gardener=Body(ZoneKit.Prefab("Characters","Gardener")!=null?"Gardener":"IshenBody",Floor(gardenAt),Mathf.Atan2(bed.x,bed.y)*Mathf.Rad2Deg,gardenHolder.transform);gardener.name="The gardener";
            var look=new GameObject("The gardener at work",typeof(DiscoveryPoint));look.transform.SetParent(gardenHolder.transform,false);look.transform.position=Floor(gardenLook);
            var watch=look.GetComponent<DiscoveryPoint>();watch.prompt="Look at the planted bed";watch.flag="oriel.gardenerHome";watch.report="";watch.situation="";watch.dialogueNode="IdraGardenerAboard";watch.dialogueSpeaker="Idra";
            watch.requiredQuest="";watch.requiredStep=0;watch.requiredFlag="";watch.range=2.4f;
            Where(gardenHolder,"quest.EmptyGardens.complete",null,"quest.EmptyGardens.complete","chapter7.complete",gardener,look);
            EditorSceneManager.MarkSceneDirty(city);EditorSceneManager.SaveScene(city);Debug.Log("MANYHANDS_ORIEL_STAGED city ishen="+ishenAt+" talk=(0,-49.4) archive="+archiveAt+" garden="+gardenAt);

            // ---- the hull
            var hull=EditorSceneManager.OpenScene("Assets/_Project/Scenes/ManyHandsHull.unity");Clear();
            var warpAt=new Vector3(-120,ZoneKit.FlightPlane,-176);
            ChapterOneStaging.ClearSigns("Warp — The Orrery"); // the warp's own sign carries no "Oriel: " name, so Clear() left it and every run added another (three on November 12; Restage)
            var warp=WorldBuilder.Warp("Warp — The Orrery",warpAt,OrreryZone,"oriel.charted","Arrival");warp.lockedPrompt="No lane to Oriel yet: Ishen is waiting on the dock with his books";warp.name=Mark+"warp to the Orrery";
            WorldBuilder.Label("Warp — The Orrery",warpAt+new Vector3(0,-.7f,-8),3.2f).name=Mark+"warp sign";
            var spawn=WorldBuilder.Spawn("Oriel",new Vector3(-112,ZoneKit.FlightPlane,-166));spawn.name=Mark+"spawn from the Orrery";
            EditorSceneManager.MarkSceneDirty(hull);EditorSceneManager.SaveScene(hull);Debug.Log("MANYHANDS_ORIEL_STAGED hull warp="+warpAt+" spawn=(-112,1,-166)");
        }

        /// <summary>Where Many Hands keeps room for chapter 7's arrivals (ManyHandsBuilder.Hands.Arrivals): Ishen's place in the archive and his desk, the gardener's place, its bed and
        /// where Idra looks at it from.</summary>
        static (Vector2 archive,Vector2 desk,Vector2 gardener,Vector2 bed,Vector2 look) ArrivalPlaces()
            =>(ManyHandsBuilder.Hands.Arrivals.IshenAt,ManyHandsBuilder.Hands.Arrivals.ArchiveDesk,ManyHandsBuilder.Hands.Arrivals.GardenerAt,ManyHandsBuilder.Hands.Arrivals.GardenerPlanter,ManyHandsBuilder.Hands.Arrivals.GardenerLook);

        // ---------------------------------------------------------------- what the region is made of
        /// <summary>The region's beds, made by tools/oriel_ambience.py (synthesised, nothing sourced). Until a file is there its zone keeps whatever its definition already holds.</summary>
        const string OrreryBed="Assets/_Project/Audio/Ambience/AMB_Orrery.ogg",SkyfieldsBed="Assets/_Project/Audio/Ambience/AMB_OrielSkyfields.ogg",ChoirBed="Assets/_Project/Audio/Ambience/AMB_GlassChoir.ogg";
        /// <summary>Measured, since nobody can listen to them: every earlier bed plays at its file's RMS (mixed to one channel) times its volume of about .0224, -33 dB. The files' RMS
        /// are .0678, .0504 and .0738 (the script prints them), so these put the three level with the rest.</summary>
        const float OrreryLevel=.331f,SkyfieldsLevel=.444f,ChoirLevel=.304f;
    }
}
