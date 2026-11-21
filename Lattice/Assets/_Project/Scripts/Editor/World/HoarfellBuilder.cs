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
    /// Chapter 6, The Long Sleep (brief: docs/chapters/ch06-the-long-sleep.md; script wiring: docs/writing/ch06-manifest.md). Isolated builder for its definitions and three zones,
    /// on <see cref="ZoneKit"/>:
    /// <list type="bullet">
    /// <item>The Aurora Shear (space combat, <c>HoarfellBuilder.AuroraShear.cs</c>): magnetic currents through Hoarfell's shattered ice ring. Flying with one is fast and fills Thrust,
    /// against one is slow and drains it.</item>
    /// <item>Lastlight (town, <c>HoarfellBuilder.Lastlight.cs</c>): the Kept's village inside the ribcage of an adult Choir frozen in the last Severance.</item>
    /// <item>The Frozen Host (wild approach and dungeon, <c>HoarfellBuilder.FrozenHost.cs</c>): the second world engineer's.</item>
    /// </list>
    /// Enemies and the Host Marshal are in <c>HoarfellBuilder.Enemies.cs</c> (the combat engineer's). Kit pieces from batch C13-Hoarfell (tools/c13_hoarfell_intake.py) are used when
    /// staged. Members of every file share one class, so each zone's own helpers carry its name or live in a nested class.
    /// Runs: <c>Definitions</c>, <c>BuildZones</c> with <c>-hoarfell-zones AuroraShear,Lastlight,FrozenHost</c>, <c>Dialogue</c>, <c>StageApproach</c>; then <c>ZoneLook.Apply</c> for what was built.
    /// </summary>
    public static partial class HoarfellBuilder
    {
        internal const string Shear="AuroraShear",Town="Lastlight",Host="FrozenHost";
        public static readonly string[] Zones={Shear,Town,Host};
        public static void Definitions()=>BatchTools.Run(()=>{Define();AssetDatabase.SaveAssets();Debug.Log("HOARFELL_DEFS_OK");});
        public static void BuildZones()=>BatchTools.Run(()=>
        {
            string arg=DevArgs.Value("-hoarfell-zones");var zones=string.IsNullOrEmpty(arg)?Zones:arg.Split(',',StringSplitOptions.RemoveEmptyEntries);
            Define();foreach(var zone in zones)BuildZone(zone.Trim());
            BatchTools.RegisterScenes();AssetDatabase.SaveAssets();Debug.Log("HOARFELL_ZONES_OK "+string.Join(",",zones));
        });
        /// <summary>The way in from chapter 5 and the way home (StageManyHands): re-run after Many Hands' own rebuild.</summary>
        [Restage.Step("chapter6.many-hands","ManyHands,ManyHandsHull",Chapter=6,Leaves=new[]{"ManyHands: Hoarfell: the chart for Hoarfell #1","ManyHands: The chart for Hoarfell <DiscoveryPoint> #1","ManyHands: Hoarfell: the lift figures #1","ManyHands: Idra's cutters come home <DiscoveryPoint> #1","ManyHands: Hoarfell: Hask aboard #1","ManyHands: Hoarfell: Iru aboard #1","ManyHandsHull: Hoarfell: warp to the Aurora Shear <WarpBeacon> #1","ManyHandsHull: Sign_Warp — The Aurora Shear #1","ManyHandsHull: Hoarfell: warp sign #1","ManyHandsHull: Hoarfell: spawn from the Aurora Shear <SpawnPoint> #1","ManyHandsHull: Hoarfell: Idra's cutters at the stern #1"})]
        public static void StageApproach()=>BatchTools.Run(()=>{Define();StageManyHands();AssetDatabase.SaveAssets();Debug.Log("HOARFELL_APPROACH_OK");});
        /// <summary>Copies docs/writing/*.yarn into Resources and recompiles the Yarn project: a new .yarn file is not in the compiled program until the project reimports, and a missing
        /// node fails silently at runtime.</summary>
        public static void Dialogue()=>BatchTools.Run(()=>{RefreshDialogue();Debug.Log("HOARFELL_DIALOGUE_OK");});
        /// <summary>Every node of the chapter's script (manifest section 2): one that didn't compile stops the build here instead of doing nothing when talked to.</summary>
        static readonly string[] Nodes=
        {
            "SelaHoarfellChart","AbriThirtySix","IloLastlightEntry",
            "IdraShearHail","IdraShearLanes","IdraCutterFound",
            "SelaLastlightArrival","TernFirst","TernRepeat","IdraPens","IdraPlan","IdraRepeat","HaskFirst","HaskRepeat","HaskStraysOffer","HaskStraysWaiting","HaskStraysReturn",
            "IruFirst","IruRepeat","IruLampsOffer","IruLampsReturn","SelaRibTally",
            "SelaReachGate","IdraHerd","IdraWhiteout","VessaLoudhailer","IdraDigGalleries","VessaSecondLevel","SelaDigShortcut","SelaMusterSlate","IdraJoins","IdraMarshalWakes","IdraMarshalSleeps",
            "TernLongSleep","TernAfter","HaskOwes","HaskGoes","IruSpeaksUp","IruGoes","SelaVentBath","SelaClampScars","IdraTally",
            "IdraLiftFigures","BinnaCutterCrews","BinnaKottSibb","AbriCutters","HaskAboard","IruAboard","MaruCutters","RuvaBeasts","TeoLiftFigures","IloHoarfellPost",
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
                case Shear:BuildAuroraShear();break;
                case Town:BuildLastlight();break;
                case Host:BuildFrozenHost();break;
                default:throw new InvalidOperationException("Unknown Hoarfell zone "+zone);
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
            // Hoarfell by day: low white sun on snow, a little bloom for lamps and steam, no tilt (D060).
            var rime=ArenaBuilder.Asset<Hd2dProfile>("Rime",p=>{p.tint=new Color(.97f,.99f,1);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.55f;});
            // The Shear: the aurora is what blooms.
            var aurora=ArenaBuilder.Asset<Hd2dProfile>("Aurora",p=>{p.tint=new Color(.96f,1,1);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.7f;});
            var source=ZoneKit.WorldMaterial("tallow-sky");
            // The panorama turned to a part of it no earlier zone shows and cooled toward teal: the Shear is neither the Veil's green-violet nor the Hull's slate.
            var space=ZoneKit.Sky("hoarfell-shear-sky",source,new Color(.4f,.56f,.62f,.5f),61,.8f);
            // Over the ice the sky is the frame's far edge: a pale cold blue.
            var day=ZoneKit.Sky("hoarfell-day-sky",source,new Color(.7f,.8f,.9f,.5f),250,1.8f);
            // Enemy damage is authored at its real weight (HoarfellBuilder.Enemies.cs), so every zone's threat is 1.
            ArenaBuilder.Asset<ZoneDef>(Shear,z=>{z.id=z.scene=Shear;z.displayName="The Aurora Shear";z.chapter=6;z.kind=ZoneKind.SpaceCombat;z.cameraProfile=flight;z.hd2dProfile=aurora;z.skybox=space;
                z.enemyThreat=1;z.flightPlane=1;z.cameraClearsScenery=false;z.musicId="Starfield Frontier";z.fightMusicId="Starfight";
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(ShearBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=ShearLevel;}});
            ArenaBuilder.Asset<ZoneDef>(Town,z=>{z.id=z.scene=Town;z.displayName="Lastlight";z.chapter=6;z.kind=ZoneKind.GroundSafe;z.cameraProfile=ground;z.hd2dProfile=rime;z.skybox=day;
                z.enemyThreat=1;z.cameraClearsScenery=true;z.musicId="Village Circle";z.shopMusicId="Moonbase Market";z.brightFloor=true;
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(TownBed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=TownLevel;}});
            ArenaBuilder.Asset<ZoneDef>(Host,z=>{z.id=z.scene=Host;z.displayName="The Frozen Host";z.chapter=6;z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=rime;z.skybox=day;
                z.enemyThreat=1;z.cameraClearsScenery=true;z.musicId="March of the Frozen Host";FrozenHost.Air(z);});

            DefineEnemies();
            Quests();

            // Lastlight on Sela's chart (guide 3.10): charted by arriving, joined to Many Hands (whose hull the lane leaves from), in the fourth column at the top.
            ArenaBuilder.Asset<ChartStop>(Town,s=>{s.id=Town;s.displayName="Lastlight";s.region="Hoarfell";s.blurb="The herders' town inside a frozen road's ribs: Hask's pens and counter, the vent bath-house and the carving yard.";
                s.zone=Town;s.spawn="Arrival";s.position=new Vector2(.76f,.92f);s.routes=new[]{"ManyHands"};s.shop=s.bench=s.repair=true;s.chapter=6;
                s.reaches=new[]{Shear,Host};});

            // The new speakers (manifest section 5). Idra's definition is IdraBuilder's (her forms, skills and gear), and is left as it is. The Kept are one mesh in four coats:
            // Tern's, Hask's and Iru's own, and the herders' grade of Tern's for townsfolk.
            void Person(string id,string display,bool female,string standIn,params string[] bodies)
                =>ArenaBuilder.Asset<CharacterDef>(id,c=>{c.id=c.speakerId=id;c.displayName=display;c.female=female;c.natural=FirstBody(bodies)??ZoneKit.Prefab("Characters",standIn);
                    // The sheets were taken in before these definitions existed, so PortraitIntake had nothing to bind them to.
                    if(c.portraitNatural==null)c.portraitNatural=AssetDatabase.LoadAssetAtPath<PortraitSet>("Assets/_Project/Resources/Portraits/"+id+"_Natural.asset");});
            Person("Tern","Old Tern",true,"Neve","TernBody");
            Person("Hask","Hask",false,"Orrin","HaskBody","TernBody");
            Person("Iru","Iru",true,"Neve","IruBody","TernBody");
            Person("Vessa","Vessa Korl",true,"Mira","VessaBody");

            // One counter in Lastlight (manifest 8.5), the zone's own, opened by Hask: what a herder trades (hide and rib-bone made into frames and plating, rime-wool padding for a
            // module's housing), the road's provisions, and the ice's glass. The Host's alloy comes out of the dig, so it is on his counter once the Marshal sleeps.
            ProgressionBuilder.Stall(Town,"HASK'S TRADE",Sell("RepairGel",26),Sell("RepairSalve",64),Sell("ChargeCell",30),Sell("ChargeBank",64),Sell("SyncTonic",96),Sell("RelinkSpike",96),
                Sell("SurgeDraught",54),Sell("PlatingWax",54),Sell("Quickening",54),
                Sell("EmittersT4",900),Sell("EdgesT4",960),Sell("FramesT4",990),Sell("DrivesT4",860),Sell("ModulesT4",1020),
                // A lot of three at three times what a shop pays for each, as every region's stall prices its own materials (Driftreed's Reed Weave, value 6, at 54): at 27 and 30
                // they sold for exactly what a shop paid back for them (ProgressionTests).
                Sell("ScrapAlloy",16,3),Sell("RimeGlass",81,3),Sell("HostAlloy",90,3,"hoar.marshalAsleep"));
        }

        /// <summary>The four quests, step for step as the script's manifest has them (section 4). The couplings are the Host Marshal's break targets (brief, Enemies), not points on
        /// the way down, so the manifest's step 6 ("Interact x3 Coupling") is left out and the Marshal's step carries them, as section 4 says to. A thing offered only at a quest's
        /// step (a stray, a high lamp, the crew pod) answers an Interact step with its own report.</summary>
        static void Quests()
        {
            ArenaBuilder.Asset<QuestDef>("LongSleep",q=>{q.id="LongSleep";q.title="The Long Sleep";q.main=true;q.chapter=6;
                q.description="Sorrel's nursery can only leave by a road that runs cold, and the herders of Lastlight are the only people said to know how a young road sleeps.";
                q.steps=new[]{Step(ObjectiveKind.Reach,Shear),Step(ObjectiveKind.Flag,"hoar.idraHailed"),Step(ObjectiveKind.Reach,Town),Step(ObjectiveKind.Flag,"hoar.ternHeard"),
                    Step(ObjectiveKind.Flag,"hoar.planTaken"),Step(ObjectiveKind.Flag,"hoar.idraJoined"),Step(ObjectiveKind.Kill,"HostMarshal"),Step(ObjectiveKind.Flag,"hoar.sleepLearned"),
                    Step(ObjectiveKind.Flag,"hoar.liftFixed")};
                q.stepText=new[]{"Take the lane to Hoarfell through the Aurora Shear","Ride the currents down the ice ring","Follow the striped cutter down to Lastlight",
                    "Find Old Tern under the fourth rib","Hear Idra Sorn's plan at the carving yard","Cross the White Reach and go down to the bottom of the dig",
                    "Put the Host Marshal back to sleep","Go back to Old Tern, and sit down","Bring Idra's cutters home to Many Hands"};
                // Step 1's arrow finds the line where the first current turns (report CurrentTurns, on Idra's hail); step 5's the bottom gallery's crossing in the dig (report
                // BottomGallery, the Frozen Host's to stand: docs/maps/Lastlight.md, "For the Frozen Host"); step 8's the dock's crossing aboard (report LiftFigures).
                q.stepTargets=new[]{At(Shear,null,null,null,null,"ManyHandsHull"),At(Shear,null,"CurrentTurns"),At(Town,null,null,null,null,Shear),At(Town,"Tern"),At(Town,"Idra"),
                    At(Host,null,"BottomGallery",null,null,Town),At(Host,null,null,"HostMarshal",null,Town),At(Town,"Tern",null,null,null,Host),At("ManyHands",null,"LiftFigures",null,null,"ManyHandsHull",Shear)};
                q.closingLine="Chapter 6 complete. The Muster has the plans for a cage. Ishen knows who drew the first one: he's on the dock.";
                // Where chapter 7 begins (Ishen arrives, walked into stepping up off Many Hands' dock: OrielBuilder's staging), five metres from where this chapter's last talk ends.
                q.next=At("ManyHands",null,null,null,null,"ManyHandsHull");q.next.hasPoint=true;q.next.point=new Vector3(0,0,-49.4f);
                // The chapter's card waits for the lift figures, the closing talk aboard; two lines of the card and no more.
                q.cardFlag="chapter6.complete";q.cardBody="Cold slowly, never alone, sung to: the herders are aboard with what they know, and Old Tern has stayed with her roof. Idra Sorn's cutters give Many Hands its engines.";
                // What the story has set by its end that the steps above don't say: who has been met and heard, the world's own events on the main path, the fight. A later
                // chapter's fixture starts with these (DevLoadout).
                q.storyFlags=new[]{"hoar.charted","hoar.arrived","hoar.ribsSeen","met.Tern","hoar.pensSeen","met.Idra","hoar.reachEntered","hoar.herdMet","hoar.herdPassed","hoar.whiteoutMet",
                    "hoar.vessaHeard","hoar.ranksSeen","hoar.offerHeard","hoar.slateRead","hoar.coupling1","hoar.coupling2","hoar.coupling3","hoar.marshalSeen","bossdown.HostMarshal",
                    "hoar.marshalAsleep","chapter6.complete"};
                // Placeholder numbers until the systems stream sets the chapter's rewards: a Tempered part of the party's own luck and something to mend with on the way home.
                q.rewardXp=1600;q.rewardScrip=900;q.rewards=new[]{new Ingredient{id="EdgesT4",count=1},new Ingredient{id="RepairSalve",count=2}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("StrayBeasts",q=>{q.id="StrayBeasts";q.title="Count Them Twice";q.chapter=6;
                q.description="Three of Hask's rime-beasts went through the fence when the vents blew, and a beast alone doesn't know to come home.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"StrayBeast",3),Step(ObjectiveKind.Flag,"hask.straysCounted")};
                q.stepText=new[]{"Walk the three strays in from the steam on the White Reach, without running","Let Hask count them"};
                q.stepTargets=new[]{At(Host,null,"StrayBeast",null,"Bring in the strays",Town),At(Town,"Hask",null,null,"Let Hask count them",Host)};
                q.rewardXp=300;q.rewardScrip=160;q.rewards=new[]{new Ingredient{id="FramesT4",count=1}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("ShrineLamps",q=>{q.id="ShrineLamps";q.title="The Skull Lamps";q.chapter=6;
                q.description="The lamp shrine's three high lamps went out in the thaw, and Iru can't climb the rib-halls alone.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"SkullLamp",3),Step(ObjectiveKind.Flag,"iru.lampsLit")};
                q.stepText=new[]{"Climb the rib-halls and light the three high lamps from the low flame","Tell Iru the shrine is lit"};
                q.stepTargets=new[]{At(Town,null,"SkullLamp",null,"Light the high lamps"),At(Town,"Iru",null,null,"Tell Iru it's lit")};
                q.rewardXp=280;q.rewardScrip=140;q.rewards=new[]{new Ingredient{id="ModulesT4",count=1}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
            ArenaBuilder.Asset<QuestDef>("CutterDown",q=>{q.id="CutterDown";q.title="Cutter Down";q.chapter=6;
                q.description="One of Idra Sorn's three cutters is down in the Aurora Shear with two of her crew in its pod.";
                q.steps=new[]{Step(ObjectiveKind.Interact,"CrewPod")};
                q.stepText=new[]{"Find the striped cutter under the ice shelf in the Aurora Shear and tow its pod in"};
                q.stepTargets=new[]{At(Shear,null,"CrewPod",null,"Find Idra's cutter",Town)};
                q.rewardXp=300;q.rewardScrip=180;q.rewards=new[]{new Ingredient{id="DrivesT4",count=1}};q.rewardsRolled=true;q.flagsOnComplete=Array.Empty<string>();});
        }

        // ---------------------------------------------------------------- light
        static Color C(float r,float g,float b)=>new(r,g,b,1);
        /// <summary>The region's authored light and grade for the Shear and the town (ZoneLook finds this by its attribute). The Frozen Host's is registered from its own file.</summary>
        [ZoneLook.Source] static void Looks()
        {
            // Inside the Shear: a hard white star off the ice ring, a teal fill off the aurora so a hull's shaded side reads against the dark.
            ZoneLook.Register(Shear,new ZoneLook.Spec{sun=C(.96f,.98f,1f),sunIntensity=1.35f,shadowStrength=.8f,
                sky=C(.3f,.42f,.46f),equator=C(.24f,.3f,.36f),ground=C(.1f,.12f,.16f),
                contrast=14,saturation=8,splitShadows=C(.4f,.48f,.58f),splitHighlights=C(.52f,.58f,.5f),
                bloomThreshold=.85f,bloomIntensity=.8f,vignette=.24f});
            // Lastlight: a low white sun on snow with a blue fill, so snow in shadow reads blue and the vents' lamplight reads warm against it.
            ZoneLook.Register(Town,new ZoneLook.Spec{sun=C(1f,.95f,.88f),sunIntensity=1.2f,sunPitch=34,shadowStrength=.75f,
                sky=C(.52f,.6f,.72f),equator=C(.48f,.52f,.58f),ground=C(.3f,.3f,.32f),
                fog=true,fogColor=C(.72f,.78f,.86f),fogStart=60,fogEnd=220,
                contrast=10,saturation=2,splitShadows=C(.42f,.5f,.62f),splitHighlights=C(.58f,.54f,.48f),
                bloomThreshold=.95f,bloomIntensity=.55f,vignette=.2f});
        }

        // ---------------------------------------------------------------- Many Hands: the way in and the way home
        /// <summary>What chapter 6 stands in chapter 5's two scenes, and only that: each piece is named "Hoarfell: ..." and a rerun destroys what it made before. Re-run after either
        /// scene's own rebuild (ManyHandsBuilder.BuildZones). In the city: the chart table's reading for Hoarfell (SelaHoarfellChart, once chapter 5 is done), the lift figures on the
        /// dock as Idra's cutters come home (IdraLiftFigures), and Hask and Iru aboard once the chapter is done. Outside: the warp to the Aurora Shear off the stern, its spawn, and
        /// Idra's three cutters lashed at the stern dock once the long sleep is told.</summary>
        static void StageManyHands()
        {
            const string Mark="Hoarfell: ";
            void Clear(){foreach(var go in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t!=null&&t.parent==null&&t.name.StartsWith(Mark)).Select(t=>t.gameObject).ToArray())UnityEngine.Object.DestroyImmediate(go);}
            Vector3 Floor(Vector2 p)
            {
                Physics.SyncTransforms();
                var hits=Physics.RaycastAll(new Vector3(p.x,60,p.y),Vector3.down,120,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).ToArray();
                if(hits.Length==0)throw new InvalidOperationException("Hoarfell staging: no floor under "+p+" in "+EditorSceneManager.GetActiveScene().name);
                return hits[0].point;
            }
            GameObject Holder(string name){var go=new GameObject(Mark+name);return go;}
            void Where(GameObject holder,GameObject who,string from,string goneAt)
            {
                var where=holder.AddComponent<Whereabouts>();where.from=from??"";where.awayFrom=where.backAt="";where.goneAt=goneAt??"";where.live=false;where.targets=new[]{who};
            }

            // ---- the city
            var city=EditorSceneManager.OpenScene("Assets/_Project/Scenes/ManyHands.unity");Clear();
            var talk=ManyHandsBuilder.Hands.ChartTalk;
            var chartHolder=Holder("the chart for Hoarfell");
            var chart=new GameObject("The chart for Hoarfell",typeof(DiscoveryPoint));chart.transform.SetParent(chartHolder.transform,false);chart.transform.position=Floor(talk);
            var read=chart.GetComponent<DiscoveryPoint>();read.prompt="Read the chart for Hoarfell";read.flag="hoar.charted";read.report="";read.situation="";read.dialogueNode="SelaHoarfellChart";read.dialogueSpeaker="Sela";
            read.requiredQuest="";read.requiredStep=0;read.requiredFlag="chapter5.complete";read.range=2;
            // The lift figures: walked into stepping off the dock once Tern has told the long sleep, the cutters lashing on behind.
            var dockHolder=Holder("the lift figures");var liftAt=Floor(new Vector2(0,-54.6f));
            var lift=new GameObject("Idra's cutters come home",typeof(DiscoveryPoint),typeof(BoxCollider));lift.transform.SetParent(dockHolder.transform,false);lift.transform.position=liftAt;
            var figures=lift.GetComponent<DiscoveryPoint>();figures.prompt="Idra's cutters come home";figures.flag="chapter6.complete";figures.report="LiftFigures";figures.situation="";figures.dialogueNode="IdraLiftFigures";
            figures.dialogueSpeaker="Idra";figures.requiredQuest="";figures.requiredStep=0;figures.requiredFlag="hoar.sleepLearned";figures.range=2;figures.startsOnCrossing=true;
            var mouth=lift.GetComponent<BoxCollider>();mouth.isTrigger=true;mouth.size=new Vector3(16,5,3);mouth.center=Vector3.up*2.5f;
            // Hask with the beasts by the freight hall, Iru on the lamp gallery with Pell: aboard once the chapter is done.
            foreach(var (id,at,yaw,node) in new[]{("Hask",new Vector2(28.5f,-48),200f,"HaskAboard"),("Iru",new Vector2(15.4f,46.4f),220f,"IruAboard")})
            {
                var holder=Holder(id+" aboard");var npc=WorldBuilder.Npc(id,Floor(at),node);npc.transform.SetParent(holder.transform,true);npc.transform.rotation=Quaternion.Euler(0,yaw,0);
                npc.firstNode=npc.repeatNode=node;npc.postNode="";npc.postFlag="";npc.finalFlag=npc.finalNode=null;npc.name=id+" aboard";
                Where(holder,npc.gameObject,"chapter6.complete",null);
            }
            EditorSceneManager.MarkSceneDirty(city);EditorSceneManager.SaveScene(city);Debug.Log("MANYHANDS_HOARFELL_STAGED city");

            // ---- the hull
            var hull=EditorSceneManager.OpenScene("Assets/_Project/Scenes/ManyHandsHull.unity");Clear();
            var warpAt=new Vector3(-90,ZoneKit.FlightPlane,-176);
            ChapterOneStaging.ClearSigns("Warp — The Aurora Shear"); // the warp's own sign carries no "Hoarfell: " name, so Clear() left it and every run added another (Restage)
            var warp=WorldBuilder.Warp("Warp — The Aurora Shear",warpAt,Shear,"hoar.charted","Arrival");warp.lockedPrompt="No lane to Hoarfell yet: Sela has to read it off the chart on the lamp gallery";warp.name=Mark+"warp to the Aurora Shear";
            WorldBuilder.Label("Warp — The Aurora Shear",warpAt+new Vector3(0,-.7f,-7),3.2f).name=Mark+"warp sign";
            var spawn=WorldBuilder.Spawn("Hoarfell",new Vector3(-82,ZoneKit.FlightPlane,-166));spawn.name=Mark+"spawn from the Aurora Shear";
            // Idra's three cutters lashed off the stern dock once the long sleep is told: her own hull, three times, a little under the plane.
            var cutters=Holder("Idra's cutters at the stern");var shown=new GameObject("Idra's cutters",typeof(FlagGate));shown.transform.SetParent(cutters.transform,false);
            var lashed=new GameObject("Lashed cutters");lashed.transform.SetParent(cutters.transform,false);
            var prefab=ZoneKit.Prefab("Characters","IdraFlight");
            if(prefab!=null)for(int k=0;k<3;k++)
            {
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(body,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var c in body.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);foreach(var b in body.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);
                body.transform.SetParent(lashed.transform,false);body.transform.SetPositionAndRotation(new Vector3(-58+k*6.5f,ZoneKit.FlightPlane-2.6f,-152-k*1.5f),Quaternion.Euler(0,10-k*8,0));body.name="Striped cutter "+(k+1);
            }
            var gate=shown.GetComponent<FlagGate>();gate.flag="hoar.sleepLearned";gate.showWhenSet=true;gate.targets=new[]{lashed};
            EditorSceneManager.MarkSceneDirty(hull);EditorSceneManager.SaveScene(hull);Debug.Log("MANYHANDS_HOARFELL_STAGED hull warp="+warpAt+" spawn=(-82,1,-166)");
        }

        // ---------------------------------------------------------------- what the region is made of
        /// <summary>The region's beds, made by tools/hoarfell_ambience.py (synthesised, nothing sourced). Until a file is there its zone keeps whatever its definition already holds.</summary>
        const string TownBed="Assets/_Project/Audio/Ambience/AMB_Lastlight.ogg",ShearBed="Assets/_Project/Audio/Ambience/AMB_AuroraShear.ogg";
        /// <summary>Measured, since nobody can listen to them: every earlier bed plays at its file's RMS (mixed to one channel) times its volume of about .0224, -33 dB. The files' RMS
        /// are .0619 and .0681 (the script prints them), so these put the two level with the rest.</summary>
        const float TownLevel=.362f,ShearLevel=.329f;
    }
}
