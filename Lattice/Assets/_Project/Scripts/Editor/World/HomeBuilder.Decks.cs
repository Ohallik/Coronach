using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The Decks on separation day (chapter 9, beats 4, 5 and 7; zone <c>DecksLeaving</c>): the town the game opened in, crated, chalked and coming apart on purpose. The Decks
    /// engineer's file. Map brief and the Coronach's contract: docs/maps/DecksLeaving.md.
    /// <para><b>A copy, not a rebuild.</b> Chapter 1's Hub_Decks is the product of nine staging steps that each know it by name (the redesign, its surfaces, chapter 1's staging twice,
    /// the dressing, the market morning, the town's people). Rebuilding it under another name would mean changing every one of them; copying the finished scene and changing
    /// it here is the same streets to the centimetre, every time Hub_Decks is restaged (the restage runner runs this after chapter 1's last step). The copy is read from the file,
    /// so Hub_Decks is opened only to fingerprint it, never saved: <see cref="Decks.Build"/> refuses to finish if its object count, its hierarchy hash or its file's bytes changed.</para>
    /// <para>From the copy go every chapter 1 piece that can talk, walk, open or fire (its four people, the market morning, the service job, the town's chapter 1 people, Hal's bench
    /// as a bench), and the day goes in: crates on the promenade, shutters down, chalk on the joints Taren has repaired, Crawl engineers in the hatches, families at the airlocks, the
    /// fruit stall with Jun behind the pears, Dace's crew on the tug dock, Neve at lock nine, the way down under the promenade, the town meeting in the dock office (one room,
    /// eight talks: LineCues turns the room to whoever has the floor), and Hal's bench, which comes apart in nine pieces as he says it.</para>
    /// Rebuild: <c>exec-when-green.ps1 -Method Lattice.EditorTools.HomeBuilder.BuildZones -Marker HOME_ZONES_OK -Graphics -TimeoutSec 1500 -ExtraArgs @('-home-zones','DecksLeaving')</c>,
    /// then <c>ZoneLook.Apply -look-zones DecksLeaving</c> (the build applies its look itself; the step is safe to run again). Stills of the meeting and of Hal's bench mid-way:
    /// add <c>'-decks-still','meeting'</c> or <c>'hal'</c> (never commit such a scene: build again without it).
    /// </summary>
    public static partial class HomeBuilder
    {
        /// <summary>The Decks on the day stand in for chapter 1's from the moment the story sends the party home until the districts fly (Lattice.Core.StandIns): the Halo's docks, the
        /// chart's chapter 1 stop and a save all lead here in that window, and to chapter 1's scene before it. Mira's stall on the day is her own counter (ShopUi.For: zone_seller).</summary>
        static void DecksDefinition(ZoneDef z)
        {
            z.standsFor="Hub_Decks";z.standsFrom=Decks.OpensAt;z.standsUntil=Decks.GoneAt;z.brightFloor=false;Decks.Shop();
            // Chapter 1's side quests that point at the Decks or the Halo (Orrin's token and Oda at her hauler; Neve at the Halo and Dace, who is on the Decks by then): left undone
            // when the districts fly, they leave the log without a word (QuestDef.letGoAt), since nothing they point at is there any more.
            foreach(var id in new[]{"OdaBerth","NeveParcel"})if(Find<QuestDef>(id) is QuestDef q&&q.letGoAt!=Decks.GoneAt){q.letGoAt=Decks.GoneAt;EditorUtility.SetDirty(q);}
        }
        /// <summary>Mira's crates (a shop), Hal's folding bench and his crated repair console are in the scene (RouteChartTests looks). Charted the moment the story sends the party
        /// home, so the story's diamond never sits on an uncharted stop. Chapter 1's own Decks stop closes when the districts fly: after the launch there is no Decks to go to.</summary>
        static void DecksStop(ChartStop s)
        {
            s.unlockFlag=Decks.OpensAt;s.shop=s.bench=s.repair=true;
            if(Find<ChartStop>("Decks")!=null)ArenaBuilder.Asset<ChartStop>("Decks",old=>{old.lockedFlag=Decks.GoneAt;old.lockedReason="The Decks came apart on purpose: their districts fly with the convoy now.";});
        }
        /// <summary>Chapter 1's hull light, a shade later in the day: the same neutral key and cool fill, a little less colour and a little more shade, so the crates and the chalk
        /// read and nothing looks like a different place.</summary>
        [ZoneLook.Source] static void DecksLooks()=>ZoneLook.Register(DecksZone,new ZoneLook.Spec{practicals=true,sun=C(1f,.97f,.93f),sunIntensity=1.15f,shadowStrength=.82f,
            sky=C(.39f,.44f,.55f),equator=C(.33f,.33f,.36f),ground=C(.19f,.16f,.15f),contrast=11,saturation=-7,splitShadows=C(.47f,.49f,.55f),splitHighlights=C(.53f,.51f,.48f),
            bloomThreshold=.9f,bloomIntensity=.6f,vignette=.22f});
        /// <summary>Hub_Decks is restaged as a whole (chapter 1's steps); the day is a copy of what they leave, so it is made again from it (<c>From</c>) after every step that changes it,
        /// and before anything that stages into the day.</summary>
        [Restage.Step("chapter9.decks-leaving","DecksLeaving",From="Hub_Decks",Chapter=9,Leaves=new[]{"DecksLeaving: The day on the Decks #1","DecksLeaving: The town meeting #1",
            "DecksLeaving: Hal's bench in nine pieces #1","DecksLeaving: The way down #1","DecksLeaving: Walkable paths <GroundNavigation> #1"})]
        public static void RestageDecksLeaving()=>BatchTools.Run(()=>{Define();Decks.Build(Coronach.Stage);AssetDatabase.SaveAssets();Debug.Log("HOME_DECKS_RESTAGED_OK");});

        internal static class Decks
        {
            // ---------------------------------------------------------------- the story's window, and the flags the day keeps
            internal const string OpensAt="home.treadRecovered",GoneAt="home.launched",Returned="home.returned",Gathered="home.meetingGathered",Held="home.meetingHeld",
                SaintFreed="home.saintReleased",HalChose="home.halChose",Called="home.coronachCalled";
            internal const string DayRoot="The day on the Decks",MeetingRoot="The town meeting",CarryingRoot="The Decks carrying things",BenchRoot="Hal's bench in nine pieces",WayDownRoot="The way down";
            const string Source="Assets/_Project/Scenes/Hub_Decks.unity",Target="Assets/_Project/Scenes/DecksLeaving.unity",Provenance="Assets/_Project/Scenes/DecksLeaving-Source.txt";

            // ---------------------------------------------------------------- the places (plan x,z; docs/maps/DecksLeaving.md). The Coronach reads these and never repeats them.
            /// <summary>Hull centres as chapter 1 built them (StationRedesign): the dock office's, the market's, the repair quarter's.</summary>
            internal static readonly float[] Hulls={-28,0,28};
            /// <summary>Mira's stall, crated, exactly where chapter 1 leaves it after the first morning, and where she stands behind it facing the promenade.</summary>
            internal static readonly (Vector2 at,Vector2 stand) PitchNine=(new(-9.7f,-1.5f),new(-8.9f,.35f));
            /// <summary>Taren's yellow line across the walkway end of the stall (chapter 1's "Walkway safety line"), its two ends.</summary>
            internal static readonly (Vector2 a,Vector2 b) YellowLine=(new(-10.6f,-5.6f),new(-10.6f,-.4f));
            /// <summary>Jun's crate of pears, one crate past the line.</summary>
            internal static readonly Vector2 PearCrate=new(-11.25f,-1.35f);
            /// <summary>Orrin's board: a slate on two legs at the dock office's north wall, facing the room.</summary>
            internal static readonly (Vector2 at,Vector2 stand) Board=(new(-33.2f,9.35f),new(-31.6f,7.7f));
            /// <summary>The dock office proper: a raised booth at the office's west end, its stamping window on its east face (where berths were stamped), its two steps beside it.</summary>
            internal static readonly (Vector2 at,Vector2 stand) StampingWindow=(new(-36.25f,2.2f),new(-35.2f,2.2f));
            internal static readonly Vector2 DockOfficeDoor=new(-38f,.2f),WornStep=new(-35.6f,5.4f);
            /// <summary>Where the capacity line was pinned: a pinboard in the arrivals vestibule, on the promenade wall's south face.</summary>
            internal static readonly (Vector2 at,Vector2 stand) NoticeBoard=(new(-24.4f,-7.45f),new(-24.4f,-8.9f));
            /// <summary>The departure gate Sela missed (the dock office's own dock), lock nine (the market's: the district Neve flies out) and the tug dock (the repair quarter's).</summary>
            internal static readonly (Vector2 at,Vector2 stand) GateSix=(new(-28,-16.2f),new(-30.6f,-14.2f)),LockNine=(new(0,-16.2f),new(-3.4f,-12.6f));
            /// <summary>The rail on the tug dock that Dace's crew lean on, and where he stands at it.</summary>
            internal static readonly (Vector2 at,Vector2 stand) DockRail=(new(31.4f,-14f),new(32.3f,-13.2f));
            /// <summary>The rail's two ends.</summary>
            internal static readonly (Vector2 a,Vector2 b) DockRailEnds=(new(31.4f,-15.6f),new(31.4f,-12.4f));
            /// <summary>Hal's bench (chapter 1's anvil) and, from the day he takes it down, the four bolt holes where it stood; where he stands working.</summary>
            internal static readonly (Vector2 at,Vector2 stand) BoltHoles=(new(32,6),new(34.4f,3.6f));
            /// <summary>The workshop has no door: its front is open to the promenade along z 0. The stretch in front of Hal's corner of it.</summary>
            internal static readonly (Vector2 centre,float width) HalsDoor=(new(31,0),8);
            internal static readonly float HalsDoorWidth=HalsDoor.width;
            /// <summary>The walkway joint's service hatch from the first scene (chapter 1's cover and repair point).</summary>
            internal static readonly Vector2 WalkwayHatch=new(-13.6f,-3),WalkwayRepair=new(-15,-3);
            /// <summary>The separation collars where the hulls are joined: the four bridge ends at the promenade and the four at the service gallery. Each is chalked (renderers "Chalked joint").</summary>
            internal static readonly (Vector2 a,Vector2 b)[] ChalkedJoints=
                new[]{-15.45f,-12.55f,12.55f,15.45f}.SelectMany(x=>new[]{(new Vector2(x,-5.7f),new Vector2(x,-.3f)),(new Vector2(x,19.3f),new Vector2(x,23.7f))}).ToArray();
            /// <summary>The way down to the Separation Works: a stairwell opened in the market vestibule's floor, running north under the promenade, behind a gate.</summary>
            internal static readonly Rect StairWell=Rect.MinMaxRect(4,-10,8,-7);
            internal static readonly Vector2 WayDownGate=new(6,-10.15f);
            /// <summary>Spawns: chapter 1's four as they stand, the meeting's (the back of the room), the stair up from the Works and the Saint's lift (by Hal's workshop).</summary>
            internal static readonly Vector2 MeetingSpawn=new(-28,-1.6f),WorksSpawn=new(6,-12.4f),LiftSpawn=new(28.6f,1.4f);
            /// <summary>Every way people walk the day: nothing solid stands within .9 m of these, nobody standing within 2 m, no walker's corner within 1.6 (<see cref="NothingStandsOnTheWays"/>).</summary>
            internal static readonly Vector2[][] Ways=
            {
                new[]{new Vector2(-39,-3),new Vector2(-12.6f,-3),new Vector2(-11.3f,-3.75f),new Vector2(-7.2f,-3.75f),new Vector2(-6,-3),new Vector2(39,-3)},   // the promenade, round the stall
                new[]{new Vector2(-28,-14),new Vector2(-28,9.4f)},                                                                              // gate six to the cabins
                new[]{new Vector2(0,-14),new Vector2(0,15)},                                                                                    // lock nine to the galley
                new[]{new Vector2(28,-14),new Vector2(28,-3),new Vector2(30.2f,1.4f),new Vector2(33,2.4f)},                                      // the tug dock to Hal's bench
                new[]{new Vector2(0,-11),new Vector2(6,-11.2f)},                                                                                // to the way down
                new[]{new Vector2(-28,-8.6f),new Vector2(-25.2f,-8.6f)},                                                                        // to the notice board
                new[]{new Vector2(28,-11),new Vector2(30.4f,-11.2f)},                                                                           // to the dock rail
                new[]{new Vector2(-34.4f,6.8f),new Vector2(-21,6.8f)},                                                                                // the dock office's front, under the board
            };
            const float WayClear=.9f,FolkClear=2;

            // ---------------------------------------------------------------- the build
            static ZoneKit kit;static Transform day,meeting,carrying,wayDown;static readonly List<Npc> named=new();
            static string Still=>DevArgs.Value("-decks-still")??"";

            internal static void Build(Action<ZoneKit> coronach)
            {
                var before=Fingerprint();
                // The copy overwrites the day's scene before anything else runs: a build that fails after it put it back, or the day stood as chapter 1's Decks until the next build
                // (an engineer's stopped build left DecksLeaving.unity a byte copy of Hub_Decks, November 14).
                var kept=File.Exists(Target)?File.ReadAllBytes(Target):null;
                try{BuildDay(before,coronach);}
                catch{if(kept!=null){File.WriteAllBytes(Target,kept);AssetDatabase.ImportAsset(Target,ImportAssetOptions.ForceUpdate);Debug.LogWarning("DECKS_LEAVING_RESTORED: the build failed, the last built day is back");}throw;}
            }
            static void BuildDay((int objects,string hash,string file) before,Action<ZoneKit> coronach)
            {
                kit=ZoneKit.Ground(DecksZone,p=>0,3.2f);named.Clear();built=0;forBerth.Clear();
                var scene=CopyHome();
                var keep=StripChapterOne();
                day=new GameObject(DayRoot).transform;meeting=new GameObject(MeetingRoot).transform;carrying=new GameObject(CarryingRoot).transform;
                keep.SetParent(day,true);
                Office();Market();Repair();Promenade();WayDown();Errands();StillThings();
                HalsBench();HalsScene(named.First(n=>n.speaker=="Hal"));Meeting();
                NothingStandsOnTheWays();
                District();
                coronach(kit);
                Bake();
                meeting.gameObject.SetActive(false);
                ApplyStill();
                var volume=ZoneLook.SceneVolume(DecksZone);ZoneLook.Configure(DecksZone,volume.sharedProfile);EditorUtility.SetDirty(volume.sharedProfile);
                WorldBuilder.Save(scene,DecksZone);
                AssetDatabase.SaveAssets();
                var after=Fingerprint();
                File.WriteAllText(Provenance,$"Hub_Decks before objects={before.objects} hash={before.hash} file={before.file}\nHub_Decks after objects={after.objects} hash={after.hash} file={after.file}\n"+
                    $"DecksLeaving built {DateTime.Now:yyyy-MM-dd HH:mm} still={(Still==""?"none":Still)}\n");
                AssetDatabase.ImportAsset(Provenance);
                if(before.objects!=after.objects||before.hash!=after.hash||before.file!=after.file)
                    throw new InvalidOperationException($"DECKS_SOURCE_CHANGED Hub_Decks changed during the Decks' build: objects {before.objects}>{after.objects} hash {before.hash}>{after.hash} file {before.file}>{after.file}");
                Debug.Log($"DECKS_SOURCE_UNCHANGED objects={after.objects} hash={after.hash} file={after.file}");
                Debug.Log($"DECKS_LEAVING_OK people={named.Count} still={(Still==""?"none":Still)}");
            }

            // ---------------------------------------------------------------- Hub_Decks, read and never written
            /// <summary>Hub_Decks as it stands: every object's place in the hierarchy, whether it is on, its local pose and its components (Restage's own reading of a scene), and the
            /// file's bytes. Opened, read and closed without saving.</summary>
            static (int objects,string hash,string file) Fingerprint()
            {
                var scene=EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);
                var lines=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Select(Line).ToList();lines.Sort(StringComparer.Ordinal);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                using var sha=SHA256.Create();
                string Hex(byte[] b)=>string.Concat(b.Take(8).Select(x=>x.ToString("x2")));
                return (lines.Count,Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n",lines)))),Hex(sha.ComputeHash(File.ReadAllBytes(Source))));
            }
            static string Line(Transform t)
            {
                var names=new List<string>();for(var p=t;p!=null;p=p.parent)names.Add(p.name);names.Reverse();
                var sb=new StringBuilder(string.Join("/",names)).Append('|').Append(t.gameObject.activeSelf?'1':'0').Append('|').Append(t.localPosition.ToString("F3")).Append(t.localRotation.ToString("F4")).Append(t.localScale.ToString("F3"));
                foreach(var c in t.GetComponents<Component>())if(c!=null&&c is not Transform)sb.Append('|').Append(c.GetType().Name);
                return sb.ToString();
            }
            /// <summary>The finished Hub_Decks, copied file for file (the copy keeps its own GUID: the build list and every reference to DecksLeaving stay put), opened, and given its own
            /// zone and its own grade: the copy's volume pointed at Hub_Decks' profile, and ZoneLook writes into whatever profile a scene's volume holds.</summary>
            static UnityEngine.SceneManagement.Scene CopyHome()
            {
                if(!File.Exists(Source))throw new InvalidOperationException("Hub_Decks isn't built: "+Source);
                File.Copy(Source,Target,true);AssetDatabase.ImportAsset(Target,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                var scene=EditorSceneManager.OpenScene(Target,OpenSceneMode.Single);
                var zone=Object.FindFirstObjectByType<ZoneController>()??throw new InvalidOperationException("The Decks' copy has no ZoneController");
                zone.definition=Find<ZoneDef>(DecksZone)??throw new InvalidOperationException("No ZoneDef "+DecksZone+": run HomeBuilder.Definitions");
                var volume=ZoneLook.SceneVolume(DecksZone);string own="Assets/_Project/Settings/"+DecksZone+"Volume.asset";
                if(AssetDatabase.LoadAssetAtPath<VolumeProfile>(own)==null&&!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(volume.sharedProfile),own))throw new InvalidOperationException("Couldn't copy the Decks' grade to "+own);
                volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(own);
                return scene;
            }

            // ---------------------------------------------------------------- what of chapter 1 goes
            static T[] All<T>()where T:Component=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            static void Gone(GameObject go){if(go!=null)Object.DestroyImmediate(go);}
            static Transform Named(string name,Vector2 near,float within=.9f)
            {
                var found=All<Transform>().Where(t=>t!=null&&t.name==name&&(t.parent==null||t.parent.name!=name)&&Vector2.Distance(new Vector2(t.position.x,t.position.z),near)<within).ToArray();
                if(found.Length==0)throw new InvalidOperationException($"DECKS_SOURCE_MOVED: no {name} at {near} in Hub_Decks any more (chapter 1's dressing changed: move the day's change with it)");
                return found.OrderBy(t=>Vector2.Distance(new Vector2(t.position.x,t.position.z),near)).First();
            }
            /// <summary>Takes away every chapter 1 piece that talks, walks, opens or fires, keeping only what stands on the day: the fruit stall where the first morning leaves it, the line,
            /// the walkway hatch's collar and its closed lid. Refuses if any such piece is left afterwards.</summary>
            static Transform StripChapterOne()
            {
                var keep=new GameObject("Kept from the first morning").transform;
                var job=GameObject.Find("Market walkway service job")??throw new InvalidOperationException("No market morning in Hub_Decks");
                foreach(var name in new[]{"Walkway safety line","Mira's mobile produce stall","Service hatch collar","Service cover hinge","Walkway service opening"})
                {
                    var part=job.transform.Find(name)??throw new InvalidOperationException("The market morning has no "+name);
                    part.SetParent(keep,true);foreach(var t in part.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;
                }
                var stall=keep.Find("Mira's mobile produce stall");stall.position=new Vector3(PitchNine.at.x,0,PitchNine.at.y);
                keep.Find("Service cover hinge").localRotation=Quaternion.identity;
                Gone(job);
                foreach(var name in new[]{"Service job kit","Service job kit goes","Service job edges go","Service job kit comes","Service job kit packed",TownLife.RootName,"Baked room navigation"})
                    foreach(var t in All<Transform>().Where(t=>t!=null&&t.name==name&&t.parent==null).ToArray())Gone(t.gameObject);
                foreach(var npc in All<Npc>())Gone(npc.gameObject);
                foreach(var nav in All<GroundNavigation>())Gone(nav.gameObject);
                // Hal's bench is the machine he takes down; its prompt goes to a folding bench (Repair()).
                foreach(var bench in All<Bench>())Object.DestroyImmediate(bench);
                var left=All<Npc>().Length+All<ScriptedWalk>().Length+All<OpeningMarketSequence>().Length+All<DiscoveryPoint>().Length+All<FlagGate>().Length+All<FlagAll>().Length+
                    All<Whereabouts>().Length+All<WalkLoop>().Length+All<QuestStarter>().Length+All<Bench>().Length;
                if(left>0)throw new InvalidOperationException("CHAPTER_ONE_LEFT "+left+" pieces of chapter 1's story are still in the Decks' copy");
                // What the day moves or takes away to put its own in the same place.
                foreach(var (key,x,z) in new[]{("DeckConsole",-34f,7f),("DressPlanter",-39.2f,1.6f),("DressLightBollard",-35.6f,6.4f),("DeckBench",-22f,3f),("DeckBench",-22f,7f),("DressPlanter",-16.9f,5f),
                    ("DressLightBollard",-17f,1.2f),("DeckBench",-21.6f,-8f),("DressPlanter",-37f,-6.2f),("DressPlanter",-20.6f,-6.2f),("DressPlanter",-7.5f,-6.2f),("DressPlanter",7.5f,-6.2f),
                    ("DressMarketStall",1.5f,5f)})
                    Gone(Named(key,new Vector2(x,z)).gameObject);
                var desk=Named("DressOfficeDesk",new Vector2(-37.8f,5));desk.position=new Vector3(-38.4f,.5f,4.6f);
                return keep;
            }

            // ---------------------------------------------------------------- materials and made things
            static Material paint,chalk,slate,glow,dark,cloth,shutter;
            static Material Steel=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/market-gate-steel.mat")??paint;
            static Material Hazard=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/dress-detail-DecalHazard.mat");
            static Material Kick=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/market-gate-kick.mat")??Hazard;
            static void Materials()
            {
                // Painted steel and boards: a quiet worn grain, tinted in the vertices, so one material is every bench, rail and shutter the day adds.
                var grain=ZoneKit.Texture("Home/decks-day-grain",256,(u,v)=>{float n=ZoneKit.Tile(u*16,v*16,16,3)*.5f+ZoneKit.Tile(u*64,v*4,64,9)*.3f+ZoneKit.Tile(u*5,v*5,5,5)*.2f;float g=.72f+.28f*n;return new Color(g,g,g);});
                paint=ZoneKit.Toon("decks-day-paint",grain,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.5f,.54f,.62f));});
                // Chalk: hand-drawn strokes, ragged at both edges and broken along their length (alpha cut, so a line stays crisp at the camera's distance).
                var strokes=ZoneKit.Texture("Home/decks-chalk",256,(u,v)=>
                {
                    float edge=Mathf.Min(u,1-u)*2,rag=ZoneKit.Tile(u*8,v*32,8,21)*.55f,grit=ZoneKit.Tile(u*64,v*64,64,23),gap=ZoneKit.Tile(0,v*6,1,29);
                    float a=Mathf.Clamp01((edge-.18f-rag*.5f)*4)*(grit>.28f?1:.2f)*(gap>.2f?1:.15f);
                    return new Color(1,1,1,a);
                },true,true);
                chalk=ZoneKit.Toon("decks-chalk",ZoneKit.Exact(strokes),m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_ALPHATEST_ON","_AlphaTest");m.SetFloat("_Cutoff",.45f);m.SetFloat("_LightFloor",.62f);
                    m.SetColor("_EmissionColor",new Color(.06f,.06f,.055f));m.renderQueue=2460;});
                // The board's slate: dark green-grey with years of chalk wiped across it, and the day's lines on it (rows of hand strokes, so a still reads as a board in use).
                var board=ZoneKit.Texture("Home/decks-board",512,(u,v)=>
                {
                    float wipe=ZoneKit.Tile(u*3,v*2,3,41)*.12f,row=Mathf.Repeat(v*9,1),inRow=row>.32f&&row<.62f?1:0,word=ZoneKit.Tile(u*22,Mathf.Floor(v*9),22,43)>.42f?1:0,stroke=ZoneKit.Tile(u*180,v*40,180,47);
                    float ink=inRow*word*(stroke>.35f?.9f:.3f)*(u>.06f&&u<.94f&&v>.08f&&v<.9f?1:0)*(Mathf.Floor(v*9)%3==2?.5f:1);
                    float g=.16f+wipe+ink*.62f;return new Color(g*.92f,g,g*.96f);
                });
                slate=ZoneKit.Toon("decks-board",board,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_LightFloor",.25f);});
                glow=ZoneKit.Toon("decks-lamp-glow",null,m=>{m.SetColor("_BaseColor",new Color(1,.86f,.6f));m.SetColor("_EmissionColor",new Color(2.4f,1.7f,.9f));});
                dark=ZoneKit.Toon("decks-shaft-dark",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_EmissionColor",Color.black);});
                var weave=ZoneKit.Texture("Home/decks-cloth",256,(u,v)=>{float w=(Mathf.Sin(u*256*Mathf.PI)*Mathf.Sin(v*256*Mathf.PI))*.08f+ZoneKit.Tile(u*8,v*8,8,51)*.2f;float g=.76f+w;return new Color(g,g,g);});
                cloth=ZoneKit.Toon("decks-cloth",weave,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.55f,.52f,.6f));});
                // A roll shutter: ribs across, a little rust in the folds.
                var ribs=ZoneKit.Texture("Home/decks-shutter",256,(u,v)=>{float f=Mathf.Repeat(v*6,1),groove=f>.82f?.5f:f<.08f?.86f:1;float wear=.92f+ZoneKit.Tile(u*4,v*4,4,61)*.08f;float g=.8f*groove*wear;return new Color(g,g,g);});
                shutter=ZoneKit.Toon("decks-shutter",ribs,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.48f,.52f,.6f));});
            }
            static int built;
            static GameObject Made(string name,ZoneKit.Shape shape,Transform parent,params Material[] materials)
            {var go=kit.Built(name+" "+(++built),shape,materials);go.name=name;go.transform.SetParent(parent,true);return go;}
            static GameObject Solid(GameObject drawn,Vector3 centre,Vector3 size,float yaw=0){ZoneKit.Block(drawn,centre,size,yaw);return drawn;}
            static GameObject Prop(string key,Vector2 at,float size,float yaw,Transform parent,bool solid=true,float up=0)
            {var go=kit.Prop(key,new Vector3(at.x,up,at.y),size,yaw,solid);go.transform.SetParent(parent,true);return go;}
            static void Tint(GameObject go,Color tint){var t=go.GetComponent<PropTint>()??go.AddComponent<PropTint>();t.tint=tint;}
            static GameObject Sign(string text,Vector2 at,Transform parent){var s=kit.Sign(text,at.x,at.y,1.4f);s.transform.SetParent(parent,true);return s;}
            static Light Lamp(Vector3 at,Transform parent,float range,float intensity,Color colour){var l=kit.Lamp(at,range,intensity,colour);l.transform.SetParent(parent,true);return l;}
            static readonly Color[] Families={new(.82f,.38f,.3f),new(.36f,.56f,.74f),new(.86f,.72f,.34f),new(.46f,.66f,.44f),new(.62f,.44f,.68f),new(.9f,.56f,.32f),new(.4f,.62f,.66f)};

            /// <summary>A stack of what a household could crate: the station's own crates, a miner's crate or two, tinted toward the family's colour band, with a cloth bundle on top
            /// and the family's number chalked on the front. Solid (the bake walks round it) and under 1.3 m where it stands on the camera's side of a walk.</summary>
            static GameObject Stack(string name,Vector2 at,float yaw,int seed,bool low,Transform parent)
            {
                var rng=new System.Random(seed);var root=new GameObject(name).transform;root.SetParent(parent,true);root.position=new Vector3(at.x,0,at.y);
                var family=Families[rng.Next(Families.Length)];var turn=Quaternion.Euler(0,yaw,0);
                float y=0;int layers=low?1:2;
                for(int layer=0;layer<layers;layer++)
                {
                    int count=layer==0?2+rng.Next(2):1+rng.Next(2);
                    for(int i=0;i<count;i++)
                    {
                        string key=rng.NextDouble()<.62?"DeckCrate":rng.NextDouble()<.5?"MusterCrate":"HushwellCrates";float size=key=="DeckCrate"?.62f:key=="MusterCrate"?.58f:.7f;
                        var off=turn*new Vector3((i-(count-1)*.5f)*.74f+(float)(rng.NextDouble()-.5)*.12f,0,(float)(rng.NextDouble()-.5)*.16f);
                        var crate=kit.Prop(ZoneKit.Kit(key,"DeckCrate"),new Vector3(at.x+off.x,y,at.y+off.z),size,yaw+(float)(rng.NextDouble()-.5)*16,false,false);
                        crate.transform.SetParent(root,true);Tint(crate,Color.Lerp(Color.white,family,.22f+(float)rng.NextDouble()*.12f));
                    }
                    y+=.6f;
                }
                // A bundle of what didn't fit in a crate, tied in the family's cloth.
                var bundle=new ZoneKit.Shape().Lump(new Vector3(at.x,y+.16f,at.y)+turn*new Vector3(.18f,0,0),new Vector3(.34f,.2f,.26f),seed,.18f,family,ZoneKit.Shape.Dim(family,.6f),10,5)
                    .Box(new Vector3(at.x,y+.18f,at.y)+turn*new Vector3(.18f,0,0),new Vector3(.05f,.42f,.55f),turn,new Color(.32f,.28f,.26f));
                if(!low)Made("Bundle",bundle,root,cloth);
                var drawn=ModelGeometry.BoundsOf(root.gameObject);
                var block=new GameObject("Stack footprint",typeof(BoxCollider));block.isStatic=true;block.transform.SetParent(root.GetChild(0),true);block.transform.SetPositionAndRotation(new Vector3(drawn.center.x,drawn.center.y,drawn.center.z),Quaternion.identity);
                block.GetComponent<BoxCollider>().size=drawn.size*.94f;
                Chalk(((seed*7)%90+10).ToString(),new Vector3(at.x,.32f,at.y)+turn*new Vector3(0,0,-.36f),Quaternion.Euler(0,yaw,0),.32f,root);
                return root.gameObject;
            }
            /// <summary>A chalked number or word: plain white type, standing on a face or lying on the floor. Not a sign (SignStyle doesn't restyle it, and it names nobody).</summary>
            static void Chalk(string text,Vector3 at,Quaternion turn,float size,Transform parent)
            {
                var go=new GameObject("Chalk mark "+text,typeof(TextMeshPro));go.transform.SetParent(parent,true);go.transform.SetPositionAndRotation(at,turn);
                // Never under the type floor from the game's camera (QaPlanSystemsTests.WorldLabelsReadFromTheGamesCameraAtTheTypeFloor): Orrin's board read at 21 px and a shutter's
                // GONE ABOARD at 17, so the words are short enough to stand that tall where they are written, on one line each.
                var t=go.GetComponent<TextMeshPro>();t.text=text;t.font=TMP_Settings.defaultFontAsset;t.fontSize=Mathf.Max(size*10,Type(SignStyle.TypeFloorPixels));t.alignment=TextAlignmentOptions.Center;t.color=new Color(.93f,.93f,.88f,.92f);
                t.rectTransform.sizeDelta=new Vector2(2,.6f);t.fontStyle=FontStyles.Bold;t.textWrappingMode=TextWrappingModes.NoWrap;
            }
            /// <summary>The font size at which world text stands this many pixels tall on a 1080p screen from the day's own camera (UiKit.WorldFontSize, which needs a zone loaded).</summary>
            static float Type(float pixels){var view=Find<ZoneDef>(DecksZone).cameraProfile;return pixels/1080f*2*view.distance*Mathf.Tan(view.fov*.5f*Mathf.Deg2Rad)*10;}
            /// <summary>A chalk line lying on the deck from a to b, a hand wide: its own mesh, cut out of the chalk texture.</summary>
            static GameObject ChalkLine(string name,Vector2 a,Vector2 b,float wide,Transform parent,float lift=.026f)
            {
                var d=(b-a);float length=d.magnitude;var across=new Vector2(-d.y,d.x).normalized*wide*.5f;
                var mesh=new Mesh{name=name,vertices=new[]{V(a-across,lift),V(a+across,lift),V(b-across,lift),V(b+across,lift)},uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,length/.7f),new Vector2(1,length/.7f)},
                    colors=Enumerable.Repeat(new Color(.95f,.95f,.9f),4).ToArray(),triangles=new[]{0,2,1,2,3,1}};
                mesh.RecalculateNormals();
                // Laid either way along the deck: wound to face up whichever way a to b runs (a quad facing down is culled, and the chalk was never drawn).
                if(mesh.normals[0].y<0){mesh.triangles=new[]{0,1,2,2,1,3};mesh.RecalculateNormals();}
                mesh.RecalculateBounds();
                var saved=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+DecksZone+"-chalk-"+(++built)+".asset");
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,true);go.GetComponent<MeshFilter>().sharedMesh=saved;
                var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=chalk;r.shadowCastingMode=ShadowCastingMode.Off;go.isStatic=true;return go;
            }
            static Vector3 V(Vector2 p,float y)=>new(p.x,y,p.y);
            static GameObject ChalkRing(string name,Vector2 centre,float radius,Transform parent)
            {
                var root=new GameObject(name).transform;root.SetParent(parent,true);
                for(int i=0;i<14;i++){float a=i*Mathf.PI*2/14,b=(i+.86f)*Mathf.PI*2/14;ChalkLine(name,centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,centre+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,.07f,root);}
                return root.gameObject;
            }

            // ---------------------------------------------------------------- people
            /// <summary>Someone with a name: chapter 1's body for chapter 1's people, the chapter's own for the new ones; tiers set outright (Npc's defaults are chapter 1's), and talk
            /// windows where they wait their turn. Every one of them is listed for the bake and the ways check.</summary>
            static Npc Person(string id,Vector2 at,float yaw,string first,string repeat,Transform parent,string body=null,params TurnToSpeak.Window[] windows)
            {
                var npc=WorldBuilder.Npc(id,new Vector3(at.x,0,at.y),first);npc.transform.rotation=Quaternion.Euler(0,yaw,0);
                var pending=npc.transform.Find("ART_PENDING_Civilian");
                if(pending!=null)
                {
                    var prefab=ZoneKit.Prefab("Characters",body??id+"Body")??throw new InvalidOperationException($"{id} has no body: no CharacterDef with one, and no {body??id+"Body"} prefab");
                    Object.DestroyImmediate(pending.gameObject);var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);model.transform.SetParent(npc.transform,false);model.name="Generated civilian";
                }
                npc.firstNode=first;npc.repeatNode=repeat;npc.postNode="";npc.postFlag="";npc.finalFlag="";npc.finalNode="";npc.transform.SetParent(parent,true);
                // A name stands as tall as PeopleMarks makes it (26 px) from the start: someone not talking yet (Tam before the party is home, Jun) kept the builder's 16 px, under the type floor.
                foreach(var name in npc.GetComponentsInChildren<TextMeshPro>(true))name.fontSize=Mathf.Max(name.fontSize,Type(26)/Mathf.Max(.01f,name.transform.lossyScale.y));
                if(windows.Length>0)npc.gameObject.AddComponent<TurnToSpeak>().windows=windows;
                named.Add(npc);return npc;
            }
            static TurnToSpeak.Window W(string from,string until)=>new(){from=from??"",until=until??""};
            /// <summary>Here or not by the story, decided as the scene loads unless <paramref name="live"/> (then only where it happens out of the party's sight).</summary>
            static Whereabouts When(string name,GameObject who,string from,string awayFrom,string backAt,string goneAt,bool live,Transform parent)
            {
                var w=new GameObject(name+" (when)",typeof(Whereabouts)).GetComponent<Whereabouts>();w.transform.SetParent(parent,true);
                w.from=from??"";w.awayFrom=awayFrom??"";w.backAt=backAt??"";w.goneAt=goneAt??"";w.live=live;w.targets=new[]{who};return w;
            }
            /// <summary>Someone with no name: a townsperson's body in a dye of their own, standing in a static box (the bake walks round them), carrying what they carry.</summary>
            static GameObject Folk(string what,string body,Vector2 at,float yaw,Color tint,Transform parent,float build=1,string carries=null,float up=0)
            {
                var person=kit.Townsperson(TownLife.Body(body),new Vector3(at.x,up,at.y),yaw,tint);person.name="Townsperson: "+what;person.transform.localScale=Vector3.one*build;
                TownLife.Carry(kit,person,carries);person.transform.SetParent(parent,true);return person;
            }
            /// <summary>Holds a body in poses no clip has, measured on the body itself from its own idle (<see cref="Posed"/>): seated on something <paramref name="seat"/> high, a hand
            /// up, bent over a hatch. Its first pose is how it loads and how an editor still shows it.</summary>
            static Posed Pose(GameObject person,float seat,params string[] poses)
            {
                var rig=person.GetComponentInChildren<Animator>(true);if(rig==null||!rig.isHuman||rig.avatar==null)return null;
                var clips=rig.runtimeAnimatorController!=null?rig.runtimeAnimatorController.animationClips:Array.Empty<AnimationClip>();
                var idle=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle"));
                using var handler=new HumanPoseHandler(rig.avatar,rig.transform);
                var right=person.transform.right;var forward=person.transform.forward;float floor=person.transform.position.y;
                void Turn(HumanBodyBones bone,Vector3 axis,float degrees){var t=rig.GetBoneTransform(bone);if(t!=null)t.rotation=Quaternion.AngleAxis(degrees,axis)*t.rotation;}
                void Sit(float height)
                {
                    foreach(var (thigh,shin) in new[]{(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg),(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg)}){Turn(thigh,right,-84);Turn(shin,right,86);}
                    var hips=rig.GetBoneTransform(HumanBodyBones.Hips);if(hips!=null)hips.position=new Vector3(hips.position.x,floor+height+.1f,hips.position.z)-forward*.12f;
                    Turn(HumanBodyBones.Spine,right,6);
                }
                void Hand(){Turn(HumanBodyBones.RightUpperArm,forward,158);Turn(HumanBodyBones.RightLowerArm,forward,8);}
                void Bend(){Turn(HumanBodyBones.Spine,right,20);Turn(HumanBodyBones.Chest,right,16);Turn(HumanBodyBones.Neck,right,10);Turn(HumanBodyBones.Head,right,18);}
                Posed.Pose Capture(string name)
                {
                    if(idle!=null)idle.SampleAnimation(rig.gameObject,.3f);
                    bool seated=seat>0&&name!="stand"&&name!="standhand";
                    if(seated)Sit(seat);
                    if(name=="hand"||name=="standhand")Hand();
                    if(name=="bend")Bend();
                    var hp=new HumanPose();handler.GetHumanPose(ref hp);
                    return new Posed.Pose{name=name,body=hp.bodyPosition,turn=hp.bodyRotation,muscles=hp.muscles.ToArray()};
                }
                var made=poses.Select(Capture).ToArray();
                var posed=person.AddComponent<Posed>();posed.poses=made;posed.current=poses[0];
                var first=made[0];var show=new HumanPose{bodyPosition=first.body,bodyRotation=first.turn,muscles=first.muscles.ToArray()};handler.SetHumanPose(ref show);
                // The rig holds the pose by itself: no clip plays over it, and nothing turns its arms to hang.
                foreach(var animated in person.GetComponentsInChildren<GeneratedAnimator>(true))animated.enabled=false;
                rig.enabled=false;rig.runtimeAnimatorController=null;
                PrefabUtility.RecordPrefabInstancePropertyModifications(rig);
                return posed;
            }

            // ---------------------------------------------------------------- the dock office
            static Transform board;
            static void Office()
            {
                Materials();
                var office=new GameObject("The dock office").transform;office.SetParent(day,true);
                // The booth: the dock office proper, a raised floor at the hull's west end with its stamping window on its east face and two steps up beside it. The middle of the
                // lower step is worn to a dip ("I've been asked to fix it eleven times"). Its deck is the station's plate; its skirt the hull's own panel paint.
                var deck=new Color(.42f,.48f,.52f);var skirt=new Color(.3f,.35f,.4f);var counter=new Color(.56f,.5f,.42f);
                // Slot 0 is the station's floor plate (its own panels), slot 1 the hull's wall panel, slot 2 the day's painted fittings.
                var booth=new ZoneKit.Shape().Slot(1)
                    .Box(new Vector3(-37.95f,.24f,3.7f),new Vector3(3.5f,.48f,6.6f),Quaternion.identity,Color.white,null,2.4f)
                    .Box(new Vector3(-35.6f,.08f,5.4f),new Vector3(.4f,.16f,2.4f),Quaternion.identity,Color.white,null,2.4f)
                    .Box(new Vector3(-36.0f,.16f,5.4f),new Vector3(.4f,.32f,2.4f),Quaternion.identity,Color.white,null,2.4f)
                    .Slot(0)
                    .Quad(new Vector3(-39.7f,.5f,.4f),new Vector3(-36.2f,.5f,.4f),new Vector3(-36.2f,.5f,7f),new Vector3(-39.7f,.5f,7f),Vector3.up,Color.white,4)
                    .Quad(new Vector3(-36.2f,.34f,4.2f),new Vector3(-35.8f,.34f,4.2f),new Vector3(-35.8f,.34f,6.6f),new Vector3(-36.2f,.34f,6.6f),Vector3.up,Color.white,4)
                    .Quad(new Vector3(-35.8f,.17f,4.2f),new Vector3(-35.4f,.17f,4.2f),new Vector3(-35.4f,.17f,6.6f),new Vector3(-35.8f,.17f,6.6f),Vector3.up,Color.white,4)
                    .Slot(2)
                    .Disc(new Vector3(-35.6f,.176f,5.4f),Vector3.up,.32f,14,new Color(.62f,.6f,.56f),0,new Color(.46f,.48f,.5f))
                    .Box(new Vector3(-37.95f,.505f,.45f),new Vector3(3.5f,.02f,.1f),Quaternion.identity,new Color(.9f,.7f,.2f)).Box(new Vector3(-36.25f,.505f,3.7f),new Vector3(.1f,.02f,6.6f),Quaternion.identity,new Color(.9f,.7f,.2f));
                // The window: a counter shelf, two posts and a header on the booth's east face, with its stamp and a tray of tokens on the shelf.
                booth.Slot(2).Box(new Vector3(-36.05f,1.02f,2.2f),new Vector3(.5f,.06f,1.5f),Quaternion.identity,counter)
                     .Box(new Vector3(-36.25f,1.55f,1.42f),new Vector3(.1f,1.1f,.1f),Quaternion.identity,skirt).Box(new Vector3(-36.25f,1.55f,2.98f),new Vector3(.1f,1.1f,.1f),Quaternion.identity,skirt)
                     .Box(new Vector3(-36.25f,2.14f,2.2f),new Vector3(.14f,.12f,1.7f),Quaternion.identity,skirt)
                     .Box(new Vector3(-36.0f,1.1f,2.5f),new Vector3(.12f,.1f,.12f),Quaternion.identity,new Color(.25f,.22f,.2f))
                     .Box(new Vector3(-36.0f,1.07f,1.9f),new Vector3(.3f,.04f,.22f),Quaternion.identity,new Color(.7f,.62f,.4f));
                var boothGo=Made("Dock office booth",booth,office,AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/station-floor-rooms.mat")??paint,
                    AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Generated/Models/DeckWall/DeckWall_Toon.mat")??paint,paint);
                ZoneKit.Block(boothGo,new Vector3(-37.95f,.25f,3.7f),new Vector3(3.5f,.5f,6.6f));
                ZoneKit.Block(boothGo,new Vector3(-35.6f,.085f,5.4f),new Vector3(.4f,.17f,2.4f));ZoneKit.Block(boothGo,new Vector3(-36.0f,.17f,5.4f),new Vector3(.4f,.34f,2.4f));
                // Its records, crated on the booth, and a lamp over the window. (Its floor sign is chapter 1's own, "ARRIVALS / DOCK OFFICE", at the office's front.)
                Stack("The dock office's records",new Vector2(-38.9f,6.2f),90,301,true,office).transform.position+=Vector3.up*.5f;
                Lamp(new Vector3(-36.6f,2.6f,2.2f),office,5,1.4f,new Color(1,.8f,.56f));

                // The board, where the town's news is chalked: a slate on two legs facing the room, the day's sections down it.
                var b=new ZoneKit.Shape();var at=Board.at;
                foreach(float dx in new[]{-1.62f,1.62f})b.Box(new Vector3(at.x+dx,.95f,at.y),new Vector3(.1f,1.9f,.1f),Quaternion.identity,new Color(.34f,.3f,.27f));
                b.Box(new Vector3(at.x,1.62f,at.y+.05f),new Vector3(3.5f,1.78f,.08f),Quaternion.identity,new Color(.3f,.27f,.24f)).Box(new Vector3(at.x,.7f,at.y-.06f),new Vector3(3.3f,.05f,.12f),Quaternion.identity,new Color(.4f,.36f,.3f));
                var frame=Made("Orrin's board frame",b,office,paint);
                var face=new ZoneKit.Shape().Quad(new Vector3(at.x-1.62f,.78f,at.y-.002f),new Vector3(at.x+1.62f,.78f,at.y-.002f),new Vector3(at.x+1.62f,2.44f,at.y-.002f),new Vector3(at.x-1.62f,2.44f,at.y-.002f),Vector3.back,Color.white,3.24f);
                var slateGo=Made("Orrin's board",face,office,slate);board=slateGo.transform;
                ZoneKit.Block(frame,new Vector3(at.x,1.2f,at.y),new Vector3(3.5f,2.4f,.3f));
                // The order of the day, as tall as the camera can read across the room: five lines, each short enough for the slate.
                var lines=new[]{"SEPARATION DAY","TOWN MEETS FIRST","MARKET FIRST OUT","REPAIR QUARTER","CRESCENT, TUG DOCK"};
                for(int i=0;i<lines.Length;i++)Chalk(lines[i],new Vector3(at.x,2.25f-i*.28f,at.y-.02f),Quaternion.identity,.24f,office);
                // The day's answer, under the order once the room has said it.
                var thanks=new GameObject("The board's answer").transform;thanks.SetParent(office,true);Chalk("BERTH: DECLINED",new Vector3(at.x,.9f,at.y-.02f),Quaternion.identity,.24f,thanks);
                When("The board's answer",thanks.gameObject,Held,null,null,null,true,office);

                // The notice board in the arrivals vestibule, where the capacity line was pinned: four pin holes where it was, and the day's notices.
                var n=new ZoneKit.Shape();var nb=NoticeBoard.at;
                n.Box(new Vector3(nb.x,1.3f,nb.y),new Vector3(2.4f,1.3f,.08f),Quaternion.identity,new Color(.55f,.44f,.32f)).Box(new Vector3(nb.x,1.3f,nb.y+.03f),new Vector3(2.56f,1.46f,.06f),Quaternion.identity,new Color(.3f,.32f,.36f));
                var rng=new System.Random(71);
                for(int i=0;i<9;i++){float px=nb.x-1+(float)rng.NextDouble()*2,py=.9f+(float)rng.NextDouble()*.8f;n.Box(new Vector3(px,py,nb.y-.05f),new Vector3(.28f+(float)rng.NextDouble()*.18f,.36f,.01f),Quaternion.Euler(0,0,(float)(rng.NextDouble()-.5)*8),new Color(.9f,.88f,.8f));}
                foreach(var (px,py) in new[]{(-.3f,1.62f),(.3f,1.62f),(-.3f,1.22f),(.3f,1.22f)})n.Disc(new Vector3(nb.x+px,py,nb.y-.045f),Vector3.back,.025f,6,new Color(.12f,.1f,.1f));
                var notice=Made("Notice board",n,office,paint);ZoneKit.Block(notice,new Vector3(nb.x,1,nb.y),new Vector3(2.56f,2,.2f));
                Sign("NOTICE BOARD",new Vector2(-20.6f,-9.8f),office);

                // Gate six: the dock office's own dock, where departures went. A family waits at it with what they could carry: a grandmother sitting on the trunk.
                Sign("GATE SIX",new Vector2(-25.0f,-12.6f),office);
                var gate=new GameObject("Waiting at gate six").transform;gate.SetParent(day,true);
                var trunk=Made("A family's trunk",new ZoneKit.Shape().Box(new Vector3(-32.6f,.25f,-14.5f),new Vector3(.9f,.5f,.55f),Quaternion.Euler(0,12,0),new Color(.46f,.34f,.26f),new Color(.56f,.42f,.3f))
                    .Box(new Vector3(-32.6f,.51f,-14.5f),new Vector3(.94f,.04f,.58f),Quaternion.Euler(0,12,0),new Color(.3f,.26f,.22f)),gate,paint);
                ZoneKit.Block(trunk,new Vector3(-32.6f,.25f,-14.5f),new Vector3(.9f,.5f,.55f),12);
                Pose(Folk("Sitting on what she owns","AumiBody",new Vector2(-32.6f,-14.25f),160,new Color(.7f,.66f,.72f),gate,.92f),.5f,"base");
                Folk("Holding the family's place at the gate","IshenBody",new Vector2(-30.8f,-13.4f),175,new Color(.84f,.64f,.5f),gate,1,"pack");
                Folk("A child with a pack bigger than them","PellBody",new Vector2(-34.4f,-13.2f),200,new Color(.62f,.76f,.86f),gate,.72f,"pack");
                Made("What the family carried",new ZoneKit.Shape().Lump(new Vector3(-31.4f,.22f,-15.1f),new Vector3(.3f,.22f,.24f),5,.16f,Families[1],ZoneKit.Shape.Dim(Families[1],.6f))
                    .Lump(new Vector3(-33.6f,.18f,-15.2f),new Vector3(.26f,.18f,.22f),7,.2f,Families[1],ZoneKit.Shape.Dim(Families[1],.55f)),gate,cloth);

                // The office's own papers and furniture crated on the day, where the meeting's benches go when the town gathers.
                var packing=new GameObject("The dock office packing").transform;packing.SetParent(day,true);
                Stack("The office's papers",new Vector2(-24.0f,4.4f),20,311,false,packing);Stack("The office's chairs",new Vector2(-21.6f,2.0f),-15,313,true,packing);Stack("The visitors' benches, crated",new Vector2(-32.0f,3.4f),90,317,true,packing);
                When("The dock office packing",packing.gameObject,null,Gathered,Held,null,false,day);
                // The people of the office on the day.
                var orrin=Person("Orrin",Board.stand,200,"OrrinHomecoming","OrrinHomecoming",day,null,W(null,Returned),W(Gathered,"home.meetingOpened"),W("home.odaStood",Held));
                When("Orrin at his board",orrin.gameObject,null,null,null,SaintFreed,true,day);
                Holder("Oda at the window",StampingWindow.stand,270,"Oda","OdaDecks","OdaDecks","OdaBody",null);
                Holder("Ilo at the notice board",NoticeBoard.stand,8,"Ilo","IloDecks","IloDecks","IloBody",null);
            }
            /// <summary>One of the day's people, under a holder that steps aside while the town is at the meeting (decided as the scene loads: the meeting is a load of its own) and
            /// a person who goes aboard at the Coronach's call (live, out of the party's sight: the Coronach's own people take their places then).</summary>
            static Npc Holder(string what,Vector2 at,float yaw,string id,string first,string repeat,string body,string awayFrom,string backAt=null,params TurnToSpeak.Window[] windows)
            {
                var holder=new GameObject(what).transform;holder.SetParent(day,true);
                var npc=Person(id,at,yaw,first,repeat,holder,body,windows);
                When(what,holder.gameObject,null,awayFrom??Gathered,backAt??Held,null,false,day);
                When(what+" goes aboard",npc.gameObject,null,null,null,Called,true,holder);
                return npc;
            }

            // ---------------------------------------------------------------- the market
            static void Market()
            {
                var market=new GameObject("The market on the day").transform;market.SetParent(day,true);
                // Jun's pears, one crate past the line: the station's own crate, tinted pear-green, heaped.
                var pears=Prop("DeckCrate",PearCrate,.5f,8,market);Tint(pears,new Color(.86f,.92f,.66f));pears.name="Jun's crate of pears";
                var heap=new ZoneKit.Shape();var prng=new System.Random(5);
                for(int i=0;i<16;i++){var p=new Vector3(PearCrate.x+(float)(prng.NextDouble()-.5)*.42f,.52f+(float)prng.NextDouble()*.1f,PearCrate.y+(float)(prng.NextDouble()-.5)*.42f);heap.Lump(p,new Vector3(.06f,.08f,.06f),i,.06f,new Color(.84f,.86f,.4f),new Color(.56f,.62f,.26f),7,4);}
                Made("Pears",heap,pears.transform,paint);
                // Mira's stock, crated, behind and beside the stall: forty-one lots of other people's and nine of her own.
                Stack("Mira's lots",new Vector2(-7.0f,.9f),0,401,false,market);Stack("Mira's lots, the nine",new Vector2(-5.2f,.6f),180,409,true,market);
                Sign("PITCH NINE",new Vector2(-7.0f,-4.9f),market);
                // Jun's pear tree, grown leaning toward the market's lamp for thirty years: chapter 2's orchard tree in a station planter.
                var planter=Prop("DressPlanter",new Vector2(-6.4f,2.6f),.8f,0,market);
                var tree=kit.Prop(ZoneKit.Kit("OrchardTree","DressFanShrub"),new Vector3(-6.4f,.4f,2.6f),2.9f,40,false);tree.name="Jun's pear tree";tree.transform.SetParent(market,true);
                var toLamp=new Vector3(-2.6f,0,5.3f)-new Vector3(-6.4f,0,2.6f);var lean=Vector3.Cross(Vector3.up,toLamp.normalized);
                tree.transform.RotateAround(new Vector3(-6.4f,.45f,2.6f),lean,11);
                // Shutters down on the two stalls whose owners have already gone aboard, and on the galley's counter.
                foreach(var (x,z,w) in new[]{(-4.6f,8.2f,3.0f),(4.6f,8.2f,3.0f),(8f,16.2f,2.0f)})Shutter(new Vector3(x,0,z),w,1.55f,market);
                // Two neighbours arguing over a crate on the promenade's north side: whose it is, or whether it goes.
                var argued=Prop("DeckCrate",new Vector2(5.6f,-.4f),.62f,20,market);Tint(argued,new Color(.94f,.82f,.7f));
                Folk("Arguing that the crate is hers","CorraBody",new Vector2(4.5f,.05f),110,new Color(.78f,.5f,.46f),market,1.02f);
                Folk("Arguing that it doesn't go","BrekkBody",new Vector2(6.7f,.0f),250,new Color(.46f,.58f,.72f),market,.96f);
                // The galley packed: crates on its tables.
                foreach(var (x,z) in new[]{(-5.2f,13.4f),(5.2f,16.9f)}){var c=Prop("DeckCrate",new Vector2(x,z),.5f,15,market,false,.78f);Tint(c,new Color(.9f,.86f,.78f));}

                // People: Jun behind her pears, Mira at her crated pitch.
                Holder("Jun behind the pears",new Vector2(-10.9f,-.2f),215,"Jun","JunFirst","JunRepeat","JunBody",null);
                Holder("Mira at pitch nine",new Vector2(-7.7f,-.9f),215,"Mira","MiraCratedRepeat","MiraCratedRepeat","Mira",null);
                // Lock nine: the market's dock, the district Neve flies out first; a family waiting at it.
                Sign("LOCK NINE",new Vector2(-4.4f,-14.4f),market);
                Holder("Neve at lock nine",LockNine.stand,140,"Neve","NeveFlyingAStreet","NeveFlyingAStreet","Neve","home.never");
                var queue=new GameObject("Waiting at lock nine").transform;queue.SetParent(day,true);
                Folk("Carrying a child's bedding","BrekkBody",new Vector2(-6.6f,-12.4f),165,new Color(.8f,.72f,.5f),queue,1.04f,"pack");
                Folk("Counting the family's crates","CorraBody",new Vector2(-8.4f,-13.4f),130,new Color(.54f,.66f,.58f),queue,.97f,"parcel");
                Stack("Lock nine's waiting crates",new Vector2(-10.2f,-13.6f),90,431,true,queue);
            }
            /// <summary>A roll shutter pulled down over a counter: ribbed panel, its box at the top, two guides.</summary>
            static void Shutter(Vector3 at,float width,float height,Transform parent)
            {
                var s=new ZoneKit.Shape();var tone=new Color(.6f,.64f,.66f);
                s.Box(at+new Vector3(0,height*.5f,0),new Vector3(width,height,.06f),Quaternion.identity,tone,tone,1);
                s.Box(at+new Vector3(0,height+.12f,.06f),new Vector3(width+.12f,.24f,.24f),Quaternion.identity,ZoneKit.Shape.Dim(tone,.75f));
                foreach(float dx in new[]{-.5f,.5f})s.Box(at+new Vector3(dx*(width+.06f),height*.5f,0),new Vector3(.06f,height,.1f),Quaternion.identity,ZoneKit.Shape.Dim(tone,.6f));
                var go=Made("Shutter down",s,parent,shutter);ZoneKit.Block(go,at+new Vector3(0,height*.5f,0),new Vector3(width,height,.14f));
                Chalk("GONE ABOARD",at+new Vector3(0,height*.62f,-.05f),Quaternion.identity,.16f,parent);
            }

            // ---------------------------------------------------------------- the repair quarter and the tug dock
            static void Repair()
            {
                var shop=new GameObject("The repair quarter on the day").transform;shop.SetParent(day,true);
                // Hal's workshop crated: its stock along the walls, three benches that fold standing ready, the repair console with its crates round it (it still works: HalCrated
                // offers it, and so does the console), and a folding bench that is a bench (the other way HalCrated's "Use the bench" opens).
                foreach(var (x,z,yaw,seed,low) in new[]{(21.6f,9.0f,0f,501,false),(25.6f,9.1f,0f,503,false),(37.8f,9.0f,180f,505,false),(23.0f,3.6f,90f,507,true),(37.6f,1.4f,0f,509,true)})
                    Stack("Hal's stock",new Vector2(x,z),yaw,seed,low,shop);
                var folding=new ZoneKit.Shape();
                for(int i=0;i<3;i++)
                {
                    var foot=new Vector3(39.3f,0,2.2f+i*.5f);var turn=Quaternion.Euler(0,0,-12);
                    folding.Box(foot+new Vector3(-.3f,.62f,0),new Vector3(.12f,1.24f,.42f),turn,new Color(.46f,.48f,.5f)).Box(foot+new Vector3(-.36f,1.0f,0),new Vector3(.06f,.5f,.4f),turn,new Color(.62f,.56f,.44f));
                }
                Made("Three benches that fold",folding,shop,paint);
                var benchAt=new GameObject("Hal's folding bench",typeof(Bench));benchAt.transform.SetParent(shop,true);benchAt.transform.position=new Vector3(38.2f,.8f,4.4f);
                var bench=benchAt.GetComponent<Bench>();bench.prompt="Fabricate — Hal's folding bench";bench.range=2.6f;
                When("Hal's folding bench",benchAt,null,null,null,Called,true,shop);
                var console=All<RepairBay>().FirstOrDefault()??throw new InvalidOperationException("Hal's repair console isn't in the copy");
                var repairAt=new GameObject("Hal's repair console, working",typeof(RepairBay));repairAt.transform.SetParent(console.transform,false);
                var repair=repairAt.GetComponent<RepairBay>();repair.prompt=console.prompt;repair.range=console.range;repair.free=console.free;repair.saveSpawn=console.saveSpawn;
                Object.DestroyImmediate(console);
                When("Hal's repair console",repairAt,null,null,null,Called,true,shop);
                Holder("Hal in his workshop",BoltHoles.stand,235,"Hal","HalCrated","HalCrated","Hal",null);
                // The tug dock: Dace and nine of his crew from the Sorrel outpost, who came in with an hour's notice and a receipt, at the rail; Marn waiting with a folding bench.
                Sign("TUG DOCK",new Vector2(23.2f,-14.6f),shop);
                var rail=new ZoneKit.Shape();var a=DockRailEnds.a;var bb=DockRailEnds.b;
                for(float z=a.y;z<=bb.y+.01f;z+=1.6f)rail.Post(new Vector3(a.x,0,z),1.05f,.06f,.05f,8,new Color(.32f,.35f,.38f),new Color(.4f,.42f,.45f));
                foreach(float y in new[]{1.02f,.62f})rail.Box(new Vector3(a.x,y,(a.y+bb.y)*.5f),new Vector3(.07f,.07f,bb.y-a.y),Quaternion.identity,new Color(.86f,.66f,.2f));
                var railGo=Made("The dock rail",rail,shop,paint);ZoneKit.Block(railGo,new Vector3(a.x,.55f,(a.y+bb.y)*.5f),new Vector3(.16f,1.1f,bb.y-a.y));
                var crew=new GameObject("Dace's crew on the tug dock").transform;crew.SetParent(day,true);
                var crewAt=new (string body,float x,float z,float yaw,Color tint,float build,string carries,bool sits)[]
                {
                    ("SurvivorNatural",33.8f,-14.8f,270,new(.76f,.7f,.58f),1.02f,null,false),("BrekkBody",35.2f,-13.4f,250,new(.6f,.66f,.54f),.98f,"pack",false),
                    ("AumiBody",36.6f,-14.6f,280,new(.7f,.6f,.5f),.95f,null,true),("SurvivorNatural",34.4f,-11.6f,230,new(.66f,.72f,.76f),.96f,"coil",false),
                    ("CorraBody",36.4f,-11.4f,260,new(.74f,.66f,.54f),1.03f,"parcel",false),("IshenBody",38.4f,-13.0f,240,new(.62f,.6f,.66f),1,null,false),
                    ("PellBody",37.8f,-15.2f,300,new(.72f,.7f,.6f),.92f,null,true),("SurvivorNatural",39.0f,-11.4f,215,new(.7f,.62f,.5f),1.05f,"crate",false),
                    ("BrekkBody",33.4f,-9.4f,200,new(.56f,.6f,.66f),.99f,null,false),
                };
                foreach(var c in crewAt)
                {
                    var p=Folk(c.sits?"One of Dace's crew sitting on their kit":"One of Dace's crew","SurvivorNatural"==c.body?"SurvivorNatural":c.body,new Vector2(c.x,c.z),c.yaw,c.tint,crew,c.build,c.carries);
                    if(c.sits){Made("Their kit",new ZoneKit.Shape().Box(new Vector3(c.x,.22f,c.z),new Vector3(.62f,.44f,.42f),Quaternion.Euler(0,c.yaw,0),new Color(.5f,.48f,.38f)),crew,paint);Pose(p,.44f,"base");}
                }
                Holder("Dace at the dock rail",DockRail.stand,265,"Survivor","SurvivorDecks","SurvivorDecks","SurvivorNatural",null);
                var marn=Folk("Marn, waiting for a bench","TeoBody",new Vector2(22.6f,-12.2f),135,new Color(.7f,.76f,.6f),crew,.94f);
                var hand=kit.Discovery("Marn's bench",new Vector3(22.6f,0,-10.9f),"Hand over the bench","bench.marn","PortableBench",null,"Taren","PortableBenches",0,2.6f);
                hand.situation="bench.marn";hand.transform.SetParent(shop,true);
            }

            // ---------------------------------------------------------------- the promenade
            static void Promenade()
            {
                var prom=new GameObject("The promenade on the day").transform;prom.SetParent(day,true);
                // Evacuation crates: low along the camera's side of the walk, higher behind it where a hull's front is open, never on a way (NothingStandsOnTheWays).
                int seed=601;
                foreach(var (x,z,yaw,low) in new[]{(-37.4f,-6.1f,0f,true),(-35.0f,-6.2f,0f,true),(-22.4f,-6.2f,0f,true),(-8.8f,-6.2f,0f,true),(-6.4f,-6.3f,0f,true),
                    (18.2f,-6.2f,0f,true),(20.6f,-6.3f,0f,true),(35.4f,-6.2f,0f,true),(37.8f,-6.1f,0f,true),(19.0f,-.6f,180f,false),(21.5f,-.5f,180f,false),
                    (-32.6f,-6.2f,0f,true),(-20.0f,-6.3f,0f,true),(-4.0f,-6.3f,0f,true),(23.0f,-6.3f,0f,true),(33.4f,-6.3f,0f,true),(-3.2f,-.5f,180f,false),(24.6f,-.6f,180f,false),
                    (-37.2f,-12.0f,90f,false),(20.6f,-9.4f,0f,false)})
                {
                    // Not the one where Dorrow kneels at the walkway's hatch: he stood inside it (QaPlanWorldTests.NobodyStandsInsideAProp). Its seed is still drawn, so the rest keep theirs.
                    int own=seed++;if(Mathf.Abs(x+20f)<.05f&&Mathf.Abs(z+6.3f)<.05f)continue;
                    Stack("Evacuation crates",new Vector2(x,z),yaw,own,low,prom);
                }
                // Chalk along every joint Taren has tightened since the first scene: the collars at both ends of both bridges, on the promenade and in the service gallery, each
                // numbered, and a ring round the walkway hatch where the first morning's job was.
                int number=1;
                foreach(var (a,b) in ChalkedJoints)
                {
                    float side=Mathf.Abs(a.x)>14?1:-1;var off=new Vector2(Mathf.Sign(a.x)*side*.3f,0);
                    ChalkLine("Chalked joint",a+off,b+off,.08f,prom);ChalkLine("Chalked joint",a+off*1.6f,b+off*1.6f,.05f,prom);
                    Chalk((number++).ToString(),new Vector3(a.x+off.x*3.4f,.03f,a.y+.5f),Quaternion.Euler(90,0,0),.5f,prom);
                }
                ChalkRing("Chalked walkway joint",WalkwayHatch,1.25f,prom);
                // The service hatches open for the Crawl's engineers: Dorrow with his ear to the deck by the walkway's bridge, two of his people at the market's, one in the gallery.
                Hatch(new Vector2(-18.4f,-5.3f),prom,1);Hatch(new Vector2(9.8f,-5.3f),prom);Hatch(new Vector2(10.2f,22.9f),prom);
                var crawl=new GameObject("Crawl engineers in the hatches").transform;crawl.SetParent(day,true);
                Pose(Folk("A Crawl engineer looking down a hatch","TamBody",new Vector2(11.2f,-5.6f),280,new Color(.62f,.5f,.36f),crawl,1.1f),0,"bend");
                Folk("A Crawl engineer paying out cable","DorrowBody",new Vector2(9.0f,-6.6f),20,new Color(.46f,.52f,.6f),crawl,.92f,"coil");
                Pose(Folk("A Crawl engineer listening to the gallery","TamBody",new Vector2(11.4f,22.6f),270,new Color(.5f,.56f,.48f),crawl,1.06f),0,"bend");
                var dorrow=Holder("Dorrow at the walkway's hatch",new Vector2(-19.6f,-5.7f),80,"Dorrow","DorrowListens","DorrowListens","DorrowBody",Held,SaintFreed);
                Pose(dorrow.gameObject,0,"bend");
                Holder("Tam on the bridge",new Vector2(14.0f,-.9f),200,"Tam","TamFirst","TamStillOffer","TamBody",null,null,W(Returned,"tam.goingBack"));
            }
            /// <summary>A service hatch opened in the deck: a dark well inside a striped collar, its lid stood up, a work lamp and a cable going down. Fenced (the box is solid and
            /// drawn round), so nobody walks into it.</summary>
            static void Hatch(Vector2 at,Transform parent,float lampSide=-1)
            {
                var root=new GameObject("Open service hatch").transform;root.SetParent(parent,true);
                var h=new ZoneKit.Shape();
                h.Box(new Vector3(at.x,.028f,at.y),new Vector3(1.0f,.01f,1.0f),Quaternion.identity,new Color(.03f,.035f,.045f));
                Made("Hatch well",h,root,dark);
                var collar=new ZoneKit.Shape();
                foreach(var (dx,dz,sx,sz) in new[]{(0f,-.56f,1.2f,.12f),(0f,.56f,1.2f,.12f),(-.56f,0f,.12f,1f),(.56f,0f,.12f,1f)})collar.Box(new Vector3(at.x+dx,.08f,at.y+dz),new Vector3(sx,.16f,sz),Quaternion.identity,Color.white);
                Made("Hatch collar",collar,root,Kick);
                var lid=new ZoneKit.Shape().Box(new Vector3(at.x,.62f,at.y+.66f),new Vector3(1.1f,1.1f,.05f),Quaternion.Euler(-12,0,0),new Color(.4f,.46f,.5f));
                var lidGo=Made("Hatch lid",lid,root,paint);
                ZoneKit.Block(lidGo,new Vector3(at.x,.45f,at.y),new Vector3(1.24f,.9f,1.24f));
                // The work lamp on the side away from whoever kneels at it (Dorrow's is on the bridge side: on his, he stood inside the hatch's things).
                Prop("DressWorkLamp",at+new Vector2(.9f*lampSide,.4f),1.1f,140,root,false);
                Made("Cable down the hatch",new ZoneKit.Shape().Tube(new[]{new Vector3(at.x-.9f*lampSide,.05f,at.y-.2f),new Vector3(at.x-.5f*lampSide,.12f,at.y-.1f),new Vector3(at.x-.2f*lampSide,-.4f,at.y)},new[]{.035f},5,new Color(.18f,.18f,.2f),new Color(.24f,.24f,.26f)),root,paint);
            }

            // ---------------------------------------------------------------- the way down
            /// <summary>The new access under the promenade (brief): one floor plate of the market vestibule taken up, a stair going down north under the promenade into the
            /// Separation Works, lit, striped, railed, chalked, with a gate whose prompt takes the party down once the town has met.</summary>
            static void WayDown()
            {
                var root=wayDown=new GameObject(WayDownRoot).transform;
                var well=StairWell;
                // The plate itself, and the keel's module that would stand in the hole.
                var tile=All<Transform>().Where(t=>t!=null&&t.name=="DeckFloor"&&t.parent!=null&&t.parent.name=="Deck floor modules"&&Mathf.Abs(t.position.x-well.center.x)<.3f&&Mathf.Abs(t.position.z-well.center.y)<.3f&&t.position.y<.1f&&t.position.y>-.3f).ToArray();
                if(tile.Length!=1)throw new InvalidOperationException($"DECKS_SOURCE_MOVED: expected one vestibule floor plate at {well.center}, found {tile.Length}");
                var band=tile[0].parent;Gone(tile[0].gameObject);
                foreach(var keel in All<Transform>().Where(t=>t!=null&&t.name=="DeckWall"&&Mathf.Abs(t.position.x-8)<.6f&&t.position.y<-1&&t.position.z>-11.2f&&t.position.z<-5.8f).ToArray())if(keel!=null)Gone(keel.gameObject);
                // The band's pressure plate lay under the whole vestibule: under the hole it would be a floor at 18 cm. Three in its place, round the well.
                var backing=band!=null?band.Find("Sealed deck backing"):null;
                if(backing!=null)
                {
                    var plate=backing.GetComponentInChildren<Renderer>().sharedMaterial;var y=backing.position.y;Gone(backing.gameObject);
                    foreach(var r in new[]{Rect.MinMaxRect(-12,-16,4,-7),Rect.MinMaxRect(8,-16,12,-7),Rect.MinMaxRect(4,-16,8,-10)})
                    {
                        var p=new GameObject("Sealed deck backing");var body=(GameObject)PrefabUtility.InstantiatePrefab(ZoneKit.Prefab("Environment","DeckWall"));body.transform.SetParent(p.transform,false);body.transform.localRotation=Quaternion.Euler(90,0,0);
                        StationSurfaces.Fit(p,new Vector3(r.width,.035f,r.height));p.transform.position+=new Vector3(r.center.x,-.185f,r.center.y)-ModelGeometry.BoundsOf(p).center;
                        foreach(var c in p.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);foreach(var rr in p.GetComponentsInChildren<Renderer>())rr.sharedMaterial=plate;
                        p.isStatic=true;p.transform.SetParent(band,true);
                    }
                }
                // The shaft's sides under the floor, the stair going down north, and the dark under the promenade it runs into.
                var s=new ZoneKit.Shape();var wall=new Color(.26f,.3f,.34f);
                s.Box(new Vector3(well.xMin-.05f,-1.5f,well.center.y),new Vector3(.1f,3,well.height),Quaternion.identity,wall).Box(new Vector3(well.xMax+.05f,-1.5f,well.center.y),new Vector3(.1f,3,well.height),Quaternion.identity,wall)
                 .Box(new Vector3(well.center.x,-1.5f,well.yMin-.05f),new Vector3(well.width,3,.1f),Quaternion.identity,wall);
                for(int i=0;i<10;i++)
                {
                    float z0=well.yMin+.1f+i*.44f,top=-.26f-i*.27f;
                    s.Box(new Vector3(well.center.x,top-.1f,z0+.22f),new Vector3(well.width-.5f,.2f,.44f),Quaternion.identity,new Color(.36f,.4f,.44f),new Color(.44f,.48f,.5f));
                    s.Box(new Vector3(well.center.x,top+.005f,z0+.04f),new Vector3(well.width-.5f,.012f,.07f),Quaternion.identity,new Color(.9f,.7f,.2f));
                }
                s.Box(new Vector3(well.center.x,-.4f,-5.6f),new Vector3(well.width,.05f,2.8f),Quaternion.identity,new Color(.02f,.02f,.025f));
                Made("The stair down",s,root,paint);
                // Hazard paint round the lip, a lamp hung over the stair, a second lit halfway down.
                foreach(var (cx,cz,w,yaw) in new[]{(well.xMin-.2f,well.center.y,well.height,0f),(well.xMax+.2f,well.center.y,well.height,0f),(well.center.x,well.yMin-.2f,well.width,90f)})
                {var q=GameObject.CreatePrimitive(PrimitiveType.Quad);Object.DestroyImmediate(q.GetComponent<Collider>());q.name="Floor detail hazard edge";q.transform.SetParent(root,true);q.transform.SetPositionAndRotation(new Vector3(cx,.022f,cz),Quaternion.Euler(90,yaw,0));q.transform.localScale=new Vector3(.28f,w,1);q.GetComponent<Renderer>().sharedMaterial=Hazard;}
                Lamp(new Vector3(well.center.x,2.4f,well.yMin+.6f),root,6,1.8f,new Color(1,.78f,.5f));Lamp(new Vector3(well.center.x,-1.2f,well.yMin+2.2f),root,5,2.2f,new Color(1,.72f,.42f));
                Made("Lamp over the stair",new ZoneKit.Shape().Box(new Vector3(well.center.x,2.3f,well.yMin+.6f),new Vector3(.5f,.12f,.3f),Quaternion.identity,Color.white),root,glow);
                // The rails, the gate at the top of the stair (shut: the prompt takes the party down), its posts and a lit plate over it.
                var r2=new ZoneKit.Shape();var steel=new Color(.32f,.35f,.38f);var amber=new Color(.92f,.68f,.18f);
                void Rail(Vector2 a,Vector2 b){foreach(var p in new[]{a,b,(a+b)*.5f})r2.Post(new Vector3(p.x,0,p.y),1.05f,.06f,.05f,8,steel,ZoneKit.Shape.Dim(steel,1.2f));
                    foreach(float y in new[]{1.02f,.6f})r2.Beam(new Vector3(a.x,y,a.y),new Vector3(b.x,y,b.y),.06f,.06f,amber);}
                Rail(new Vector2(well.xMin-.1f,well.yMin-.1f),new Vector2(well.xMin-.1f,well.yMax));Rail(new Vector2(well.xMax+.1f,well.yMin-.1f),new Vector2(well.xMax+.1f,well.yMax));
                Rail(new Vector2(well.xMin-.1f,well.yMin-.1f),new Vector2(well.xMax+.1f,well.yMin-.1f));
                r2.Post(new Vector3(well.xMin+.2f,0,well.yMin-.1f),2.5f,.08f,.07f,8,steel,steel).Post(new Vector3(well.xMax-.2f,0,well.yMin-.1f),2.5f,.08f,.07f,8,steel,steel)
                  .Box(new Vector3(well.center.x,2.42f,well.yMin-.1f),new Vector3(well.width-.2f,.36f,.08f),Quaternion.identity,new Color(.2f,.22f,.25f));
                var rails=Made("The way down's rails and gate",r2,root,paint);
                ZoneKit.Block(rails,new Vector3(well.xMin-.1f,.55f,well.center.y),new Vector3(.16f,1.1f,well.height+.2f));ZoneKit.Block(rails,new Vector3(well.xMax+.1f,.55f,well.center.y),new Vector3(.16f,1.1f,well.height+.2f));
                ZoneKit.Block(rails,new Vector3(well.center.x,.55f,well.yMin-.1f),new Vector3(well.width+.36f,1.1f,.16f));
                // The plate and the lamps hang on the gate's posts: one thing with them (measured on its own, the plate floated a metre over the deck).
                var hung=Made("The way down's plate",new ZoneKit.Shape().Quad(new Vector3(well.xMin+.4f,2.28f,well.yMin-.15f),new Vector3(well.xMax-.4f,2.28f,well.yMin-.15f),new Vector3(well.xMax-.4f,2.56f,well.yMin-.15f),new Vector3(well.xMin+.4f,2.56f,well.yMin-.15f),Vector3.back,Color.white,.6f),root,Kick);
                Made("The way down's lamps",new ZoneKit.Shape().Box(new Vector3(well.xMin+.2f,2.62f,well.yMin-.1f),new Vector3(.16f,.12f,.16f),Quaternion.identity,Color.white).Box(new Vector3(well.xMax-.2f,2.62f,well.yMin-.1f),new Vector3(.16f,.12f,.16f),Quaternion.identity,Color.white),root,glow).transform.SetParent(rails.transform,true);hung.transform.SetParent(rails.transform,true);
                Chalk("SEPARATION WORKS",new Vector3(well.center.x,2.42f,well.yMin-.2f),Quaternion.identity,.24f,root);
                // Chalk arrows on the promenade and the vestibule, from the market door to the gate.
                foreach(var (a,b) in new[]{(new Vector2(2.6f,-7.8f),new Vector2(4.4f,-10.6f)),(new Vector2(4.4f,-10.6f),new Vector2(3.6f,-10.2f)),(new Vector2(4.4f,-10.6f),new Vector2(4.6f,-9.8f))})ChalkLine("Chalk arrow",a,b,.09f,root);
                // The gate's prompt: a way out like any other, locked until the meeting is held, and saying so.
                var gateBody=new GameObject("Gate down to the Separation Works");gateBody.transform.SetParent(root,true);gateBody.transform.position=new Vector3(WayDownGate.x,1,WayDownGate.y);
                kit.Way(gateBody,"Go down — The Separation Works",WorksZone,"Arrival",Held,"The town meets first: Orrin has called it in the dock office",3,new Vector2(WayDownGate.x,-12.9f));
                foreach(var sign in All<TextMeshPro>().Where(t=>t.name=="Sign_Go down — The Separation Works"))sign.transform.SetParent(root,true);
                WorldBuilder.Spawn("Works",new Vector3(WorksSpawn.x,0,WorksSpawn.y)).transform.SetParent(root,true);
                WorldBuilder.Spawn("Lift",new Vector3(LiftSpawn.x,0,LiftSpawn.y)).transform.SetParent(day,true);
            }

            // ---------------------------------------------------------------- the day's walkers
            /// <summary>People carrying the day's crates toward the airlocks (the Coronach thins them out as doors are said). Each loop's corners keep 1.6 m off every way; a loop may cross
            /// a way, stopping for whoever is on it.</summary>
            static void Errands()
            {
                var loops=new (string what,string body,Vector2[] loop,float pause,Color tint,string carries,float build)[]
                {
                    ("Carrying a crate to gate six","CorraBody",new[]{new Vector2(-37.6f,-9.4f),new Vector2(-33.0f,-11.4f)},2.4f,new(.72f,.62f,.5f),"crate",1),
                    ("Carrying the workshop's parts out to the crates","IshenBody",new[]{new Vector2(24.6f,1.8f),new Vector2(20.6f,.9f)},2.2f,new(.66f,.7f,.6f),"parcel",.98f),
                    ("Hauling crates at the tug dock","BrekkBody",new[]{new Vector2(37.4f,-8.6f),new Vector2(38.8f,-10.4f)},2.6f,new(.8f,.66f,.5f),"crate",1.02f),
                    ("Stacking the galley's crates","PellBody",new[]{new Vector2(-8.2f,12.4f),new Vector2(-8.2f,17.6f)},1.8f,new(.86f,.78f,.62f),"crate",.95f),
                    ("Carrying bedding from the cabins","AumiBody",new[]{new Vector2(-26.0f,12.6f),new Vector2(-26.0f,17.4f)},2,new(.62f,.72f,.82f),"pack",.97f),
                    // The office's end of the promenade read as an ordinary morning with crates by the walls (LookDev, November 14): somebody is carrying something there too.
                    ("Carrying a family's crate along the promenade","GaroBody",new[]{new Vector2(-25.8f,-5.0f),new Vector2(-31.2f,-5.1f)},2.2f,new(.7f,.64f,.56f),"crate",.97f),
                    ("Carrying the office's chairs out to the crates","QuillBody",new[]{new Vector2(-22.6f,1.4f),new Vector2(-22.6f,-4.9f)},2.4f,new(.6f,.68f,.74f),"parcel",.95f),
                };
                foreach(var e in loops)
                {
                    string body=TownLife.Body(e.body);
                    var walker=kit.Walker(body,e.loop,TownLife.Pace(body,e.build),e.pause,e.tint);walker.thereAndBack=true;walker.name=e.what;walker.transform.localScale=Vector3.one*e.build;
                    TownLife.Carry(kit,walker.gameObject,e.carries);walker.transform.SetParent(carrying,true);
                }
                // A child chalking the family's door in the cabins' corridor.
                var child=Folk("A child chalking the family's door","PellBody",new Vector2(-23.9f,12.3f),90,new Color(.9f,.78f,.6f),day,.7f);
                Chalk("ARREN",new Vector3(-23.25f,1.1f,12.3f),Quaternion.Euler(0,-90,0),.18f,day);Chalk("4",new Vector3(-23.25f,.86f,12.3f),Quaternion.Euler(0,-90,0),.24f,day);
            }

            // ---------------------------------------------------------------- Still Ground: three still things to show Tam
            static void StillThings()
            {
                var root=new GameObject("Three still things").transform;root.SetParent(day,true);
                // Cuts in the cabins' doorframe, with names, going up.
                var cuts=new ZoneKit.Shape();
                for(int i=0;i<9;i++)cuts.Box(new Vector3(-29.86f,.62f+i*.11f,9.68f),new Vector3(.16f+(i%3)*.03f,.012f,.01f),Quaternion.identity,new Color(.16f,.14f,.12f));
                Made("Height marks on the doorframe",cuts,root,paint);
                foreach(var (name,at,node) in new[]{("The marked doorframe",new Vector2(-29.4f,8.9f),"TamDoorMarks"),("Jun's pear tree",new Vector2(-6.4f,1.3f),"TamLeaningTree"),("The worn step",new Vector2(WornStep.x,WornStep.y),"TamWornStep")})
                {
                    string flag=node=="TamDoorMarks"?"tam.door":node=="TamLeaningTree"?"tam.tree":"tam.step";
                    var p=kit.Discovery(name,new Vector3(at.x,0,at.y),"Show Tam",flag,"StillThing",node,"Tam","StillGround",0,2.4f);p.situation="";p.transform.SetParent(root,true);
                }
            }

            // ---------------------------------------------------------------- Hal's bench
            static Transform[] pieces;static Vector3[] laidOut;static Quaternion[] laidTurn;static GameObject benchBlock;
            /// <summary>Chapter 1's bench (the Lattice Anvil Hal fabricates at) cut into nine pieces along its own model: its triangles grouped into nine by where they lie (a seeded k-means),
            /// each group its own mesh, so put together they are the bench exactly and taken apart each piece is part of it. They come off in HalLastMachine as Hal says them, and lie
            /// in a row on the workshop floor; from the next load they are packed in a crate, and four bolt holes are where it stood.</summary>
            static void HalsBench()
            {
                var anvil=Named("LatticeAnvil",BoltHoles.at,1.2f);
                var filters=anvil.GetComponentsInChildren<MeshFilter>();
                var verts=new List<Vector3>();var uvs=new List<Vector2>();var norms=new List<Vector3>();var tris=new List<(int a,int b,int c,int material)>();var mats=new List<Material>();
                foreach(var f in filters)
                {
                    var mesh=f.sharedMesh;if(mesh==null)continue;var r=f.GetComponent<MeshRenderer>();var world=f.transform.localToWorldMatrix;int start=verts.Count;
                    verts.AddRange(mesh.vertices.Select(v=>world.MultiplyPoint3x4(v)));
                    var uv=mesh.uv;uvs.AddRange(uv.Length==mesh.vertexCount?uv:new Vector2[mesh.vertexCount]);
                    var normal=mesh.normals;norms.AddRange(normal.Length==mesh.vertexCount?normal.Select(n=>world.MultiplyVector(n).normalized):Enumerable.Repeat(Vector3.up,mesh.vertexCount));
                    for(int sub=0;sub<mesh.subMeshCount;sub++)
                    {
                        var m=r!=null&&sub<r.sharedMaterials.Length?r.sharedMaterials[sub]:null;int mi=mats.IndexOf(m);if(mi<0){mats.Add(m);mi=mats.Count-1;}
                        var t=mesh.GetTriangles(sub);for(int i=0;i<t.Length;i+=3)tris.Add((start+t[i],start+t[i+1],start+t[i+2],mi));
                    }
                }
                if(tris.Count<9)throw new InvalidOperationException("Hal's bench has no model to take apart");
                var centres=tris.Select(t=>(verts[t.a]+verts[t.b]+verts[t.c])/3).ToArray();
                var rng=new System.Random(9);var seeds=Enumerable.Range(0,9).Select(_=>centres[rng.Next(centres.Length)]).ToArray();var group=new int[centres.Length];
                for(int pass=0;pass<14;pass++)
                {
                    for(int i=0;i<centres.Length;i++){float best=float.MaxValue;for(int k=0;k<9;k++){float d=(Vector3.Scale(centres[i]-seeds[k],new Vector3(1,1.4f,1))).sqrMagnitude;if(d<best){best=d;group[i]=k;}}}
                    for(int k=0;k<9;k++){var members=Enumerable.Range(0,centres.Length).Where(i=>group[i]==k).ToArray();seeds[k]=members.Length>0?members.Aggregate(Vector3.zero,(s,i)=>s+centres[i])/members.Length:centres[rng.Next(centres.Length)];}
                }
                var root=new GameObject(BenchRoot).transform;
                // Taken off top first: the order a bench comes apart (the vice and the top before the frame and the feet).
                var order=Enumerable.Range(0,9).OrderByDescending(k=>seeds[k].y).ToArray();
                pieces=new Transform[9];laidOut=new Vector3[9];laidTurn=new Quaternion[9];
                for(int n=0;n<9;n++)
                {
                    int k=order[n];var mine=Enumerable.Range(0,tris.Count).Where(i=>group[i]==k).ToArray();
                    if(mine.Length==0)throw new InvalidOperationException("Hal's bench came apart into fewer than nine pieces");
                    var used=new Dictionary<int,int>();var local=new List<Vector3>();var localUv=new List<Vector2>();var localNormal=new List<Vector3>();var subs=new List<int>[mats.Count];for(int m=0;m<mats.Count;m++)subs[m]=new List<int>();
                    var low=mine.Min(i=>Mathf.Min(verts[tris[i].a].y,verts[tris[i].b].y,verts[tris[i].c].y));var pivot=new Vector3(seeds[k].x,low,seeds[k].z);
                    int Map(int v){if(!used.TryGetValue(v,out int at)){at=local.Count;local.Add(verts[v]-pivot);localUv.Add(uvs[v]);localNormal.Add(norms[v]);used[v]=at;}return at;}
                    foreach(var i in mine){var t=tris[i];subs[t.material].Add(Map(t.a));subs[t.material].Add(Map(t.b));subs[t.material].Add(Map(t.c));}
                    var mesh=new Mesh{name="Hal's bench, piece "+(n+1),indexFormat=IndexFormat.UInt32};mesh.SetVertices(local);var live=Enumerable.Range(0,mats.Count).Where(m=>subs[m].Count>0).ToArray();mesh.subMeshCount=live.Length;
                    mesh.SetUVs(0,localUv);mesh.SetNormals(localNormal);
                    for(int s=0;s<live.Length;s++)mesh.SetTriangles(subs[live[s]],s);mesh.RecalculateBounds();
                    var saved=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+DecksZone+"-bench-piece-"+(n+1)+".asset");
                    var go=new GameObject("Bench piece "+(n+1),typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,true);go.transform.position=pivot;
                    go.GetComponent<MeshFilter>().sharedMesh=saved;go.GetComponent<MeshRenderer>().sharedMaterials=live.Select(m=>mats[m]).ToArray();
                    pieces[n]=go.transform;
                    // Laid on the floor in a row of three by three east of where the bench stood, each on its own foot, a little turned as it was put down.
                    laidOut[n]=new Vector3(35.0f+(n%3)*1.2f,0,5.4f+(n/3)*1.2f);laidTurn[n]=Quaternion.Euler(0,(n*37)%40-20,0);
                }
                // The bench was one solid box; it stays one until the last piece is off.
                var drawn=ModelGeometry.BoundsOf(anvil.gameObject);
                benchBlock=new GameObject("Hal's bench, standing",typeof(BoxCollider));benchBlock.isStatic=true;benchBlock.transform.SetParent(pieces[pieces.Length-1],true);
                benchBlock.transform.position=drawn.center;benchBlock.GetComponent<BoxCollider>().size=drawn.size;
                Gone(anvil.gameObject);
                When("Hal's bench",root.gameObject,null,null,null,HalChose,false,day);
                // From the next load: the nine pieces in a crate, the four bolt holes where it stood, and the anvil's work-zone paint round nothing.
                var after=new GameObject("Hal's bench, packed").transform;after.SetParent(day,true);
                var packed=new ZoneKit.Shape().Box(new Vector3(36.2f,.3f,6.6f),new Vector3(2.6f,.6f,1.3f),Quaternion.identity,new Color(.5f,.42f,.32f),new Color(.6f,.5f,.36f));
                Made("The crate of nine pieces",packed,after,paint);
                for(int n=0;n<9;n++){var copy=Object.Instantiate(pieces[n].gameObject,after);copy.name="Packed piece "+(n+1);copy.transform.position=new Vector3(35.2f+(n%5)*.5f,.42f,6.3f+(n/5)*.6f);copy.transform.rotation=Quaternion.Euler(0,(n*53)%90,(n%2)*90);
                    foreach(var c in copy.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);copy.transform.localScale=Vector3.one*.62f;}
                ZoneKit.Block(after.GetChild(0).gameObject,new Vector3(36.2f,.3f,6.6f),new Vector3(2.6f,.6f,1.3f));
                var holes=new ZoneKit.Shape();foreach(var (dx,dz) in new[]{(-.7f,-.45f),(.7f,-.45f),(-.7f,.45f),(.7f,.45f)})holes.Disc(new Vector3(BoltHoles.at.x+dx,.024f,BoltHoles.at.y+dz),Vector3.up,.07f,10,new Color(.04f,.04f,.05f),0,new Color(.5f,.48f,.44f));
                Made("Four bolt holes",holes,after,dark);
                When("Hal's bench, packed",after.gameObject,HalChose,null,null,null,false,day);
                after.gameObject.SetActive(false);
                root.SetParent(day,true);
            }
            /// <summary>Hal's last machine (beat 7). Hal does all of it: he steps round the bench and the pieces come off as he speaks, top first, and lie in a row; Taren takes the lamp
            /// on the fourth line ("No. Hold the lamp. Higher. There.") and holds it to the end, and does nothing else; nobody else moves or speaks. No bark, no prompt for Taren.</summary>
            static void HalsScene(Npc hal)
            {
                var cues=new GameObject("Hal's last machine, line by line",typeof(LineCues)).GetComponent<LineCues>();cues.transform.SetParent(day,true);
                cues.node="HalLastMachine";cues.doneFlag=HalChose;
                var list=new List<LineCues.Cue>();
                var lamp=HandLamp(cues.transform);var rest=new GameObject("Where the lamp is put down").transform;rest.SetParent(cues.transform,true);rest.position=new Vector3(34.9f,.32f,2.6f);
                var mark=new GameObject("The bench, to face").transform;mark.SetParent(cues.transform,true);mark.position=new Vector3(BoltHoles.at.x,1,BoltHoles.at.y);
                void Off(int line,int piece,float delay){list.Add(new LineCues.Cue{line=line,act=LineCues.Act.Move,who=pieces[piece],to=laidOut[piece],turn=laidTurn[piece].eulerAngles,seconds=1.3f,lift=.7f,delay=delay});}
                void Step(int line,Vector2 at){list.Add(new LineCues.Cue{line=line,act=LineCues.Act.Move,who=hal.transform,to=new Vector3(at.x,0,at.y),turn=Quaternion.LookRotation(new Vector3(BoltHoles.at.x-at.x,0,BoltHoles.at.y-at.y)).eulerAngles,seconds=1.1f});}
                void Face(int line){list.Add(new LineCues.Cue{line=line,act=LineCues.Act.Face,who=hal.transform,toward=mark});}
                Step(1,new Vector2(33.6f,4.7f));Face(1);Off(1,0,.9f);
                Off(2,1,.4f);
                list.Add(new LineCues.Cue{line=4,act=LineCues.Act.Hold,who=lamp,hero="Taren",toward=rest});
                Step(5,new Vector2(30.6f,4.7f));Face(5);Off(5,2,.8f);Off(5,3,1.9f);
                Off(6,4,.3f);Off(6,5,1.2f);Off(6,6,2.1f);
                Step(7,new Vector2(33.4f,5.0f));Face(7);Off(7,7,.3f);Off(7,8,1.2f);
                list.Add(new LineCues.Cue{line=7,act=LineCues.Act.Hide,who=benchBlock.transform});
                cues.cues=list.ToArray();
            }
            /// <summary>A hand lamp: a little caged lantern with a warm light, put away until somebody holds it.</summary>
            static Transform HandLamp(Transform parent)
            {
                var root=new GameObject("The hand lamp").transform;root.SetParent(parent,false);
                var s=new ZoneKit.Shape();s.Box(new Vector3(0,-.18f,0),new Vector3(.16f,.22f,.16f),Quaternion.identity,new Color(.3f,.3f,.32f)).Box(new Vector3(0,-.04f,0),new Vector3(.04f,.08f,.04f),Quaternion.identity,new Color(.3f,.3f,.32f));
                var body=kit.Built("Hand lamp",s,paint);body.transform.SetParent(root,false);
                var core=kit.Built("Hand lamp glass",new ZoneKit.Shape().Box(new Vector3(0,-.18f,0),new Vector3(.12f,.16f,.12f),Quaternion.identity,Color.white),glow);core.transform.SetParent(root,false);
                var l=new GameObject("Lamp light",typeof(Light)).GetComponent<Light>();l.transform.SetParent(root,false);l.transform.localPosition=Vector3.down*.18f;
                l.type=LightType.Point;l.color=new Color(1,.8f,.52f);l.range=4.5f;l.intensity=2.2f;l.shadows=LightShadows.None;
                root.gameObject.SetActive(false);return root;
            }

            // ---------------------------------------------------------------- the town meeting
            /// <summary>The meeting (beat 5): one room, eight walks. The town gathers in the dock office when the party says so ("Join the town meeting", which loads the zone again at the
            /// back of the room, the day's people gone to it). The room faces the board on its north wall: Orrin at the board, Meret standing beside it, the front benches empty
            /// (one has a crate on it), the town seated behind in two blocks either side of the aisle, Hal standing at the east end with his nine apprentices behind him, Ilo on the
            /// dock office's step with the footnotes, Oda at the aisle end of the third row, Jun near her in the second, Dace's ten at the back by the promenade. Each talk is
            /// offered in its turn (TurnToSpeak), and on each line the room turns to whoever has the floor (LineCues): to Hal when he asks, to Jun when her name is read, to
            /// Dace's crew at "by the door", to Oda's row as it stands up; four hands for the berth, then the whole room against.</summary>
            static Npc meret,mHal,mMira,mIlo,mOda,mJun,mDace,orrin;
            static Transform crowd,apprentices,crewRoom;static readonly List<Transform> forBerth=new();
            static void Meeting()
            {
                meeting.gameObject.SetActive(true);
                orrin=named.First(n=>n.speaker=="Orrin");
                // The way in: a prompt at the office's front once Orrin has called it, and the zone loaded again with the room full.
                var join=kit.Discovery("Join the town meeting",new Vector3(-28,0,-.8f),"Join the town meeting",Gathered,"Meeting",null,"Taren",null,0,2.8f);
                join.requiredFlag=Returned;join.situation="";join.saveSpawn="Meeting";join.transform.SetParent(day,true);
                var leap=join.gameObject.AddComponent<LeapDown>();leap.flag=Gathered;leap.spawn="Meeting";
                WorldBuilder.Spawn("Meeting",new Vector3(MeetingSpawn.x,0,MeetingSpawn.y)).transform.SetParent(day,true);
                // The benches: two blocks of three rows either side of the aisle, the station's own steel and slatted seats. The front two are empty; the west one has a crate on it.
                var benches=new GameObject("The meeting's benches").transform;benches.SetParent(meeting,true);
                foreach(float x in new[]{-31.9f,-23.2f})foreach(float z in new[]{5.6f,3.9f,2.2f})Bench(new Vector2(x,z),benches);
                var crate=Prop("DeckCrate",new Vector2(-31.3f,5.6f),.62f,12,benches,false,.45f);crate.name="The crate on the front bench";Tint(crate,new Color(.92f,.86f,.76f));
                Chalk("HOLD",crate.transform.position+new Vector3(0,.31f,-.32f),Quaternion.identity,.12f,benches);
                // The named, standing or sitting where the script has them.
                meret=Person("Meret",new Vector2(-35.2f,8.1f),165,"MeretBerthOffer","MeretBerthOffer",meeting,"MeretBody",W("home.meetingOpened","home.offerMade"));
                mHal=Person("Hal",new Vector2(-19.5f,5.7f),300,"HalCrated","HalCrated",meeting,null,W("home.offerMade","home.halAsked"),W(Held,null));
                mMira=Person("Mira",new Vector2(-24.6f,6.7f),330,"MiraCratedRepeat","MiraCratedRepeat",meeting,null,W("home.halAsked","home.manifestShown"),W(Held,null));
                mIlo=Person("Ilo",new Vector2(-36.0f,5.4f),80,"IloDecks","IloDecks",meeting,"IloBody",W("home.manifestShown","home.namesRead"));mIlo.transform.position+=Vector3.up*.34f;
                mOda=Person("Oda",new Vector2(-30.9f,2.3f),0,"OdaDecks","OdaDecks",meeting,"OdaBody",W("home.namesRead","home.odaStood"));
                mJun=Person("Jun",new Vector2(-24.8f,4.0f),0,"JunRepeat","JunRepeat",meeting,"JunBody",W("home.never",null));
                mDace=Person("Survivor",new Vector2(-33.4f,-.3f),25,"SurvivorDecks","SurvivorDecks",meeting,"SurvivorNatural",W("home.never",null));
                Pose(mOda.gameObject,.45f,"base","stand");Pose(mJun.gameObject,.45f,"base","stand");
                // The footnotes in Ilo's hand, the bound copy in Meret's (it goes to Mira on the manifest's second line).
                Papers(mIlo.transform,"The footnotes",new Color(.94f,.92f,.84f));var copy=Papers(meret.transform,"The bound copy",new Color(.56f,.22f,.2f));
                // The town, seated and standing. No two neighbours alike; each turns to whoever has the floor.
                crowd=new GameObject("The town at the meeting").transform;crowd.SetParent(meeting,true);
                var seated=new (string body,float x,float z,Color tint,float build)[]
                {
                    ("BrekkBody",-33.5f,4.0f,new(.66f,.58f,.5f),1),("AumiBody",-32.2f,4.0f,new(.58f,.66f,.74f),.94f),("PellBody",-30.9f,4.0f,new(.82f,.66f,.56f),.97f),
                    ("IshenBody",-33.5f,2.3f,new(.64f,.62f,.7f),1.02f),("CorraBody",-22.1f,4.0f,new(.72f,.6f,.56f),.98f),("AumiBody",-23.4f,2.3f,new(.6f,.72f,.6f),.95f),
                    ("BrekkBody",-24.8f,2.3f,new(.78f,.7f,.56f),1.03f),("PellBody",-22.1f,2.3f,new(.56f,.62f,.76f),.93f),("IshenBody",-23.4f,4.0f,new(.7f,.7f,.6f),.96f),
                };
                for(int i=0;i<seated.Length;i++){var s=seated[i];var p=Folk("Sitting at the meeting",s.body,new Vector2(s.x,s.z),0,s.tint,crowd,s.build);Pose(p,.45f,"base","hand");if(i==1||i==4||i==6)forBerth.Add(p.transform);}
                var mother=Folk("Oda's mother","CorraBody",new Vector2(-32.2f,2.3f),0,new Color(.78f,.62f,.7f),meeting,.84f);Pose(mother,.45f,"base","stand");
                var standing=new (string body,float x,float z,float yaw,Color tint,float build)[]
                {
                    ("CorraBody",-25.4f,-.4f,10,new(.84f,.72f,.5f),1.02f),("IshenBody",-23.6f,-.2f,350,new(.6f,.7f,.66f),.99f),("PellBody",-21.8f,-.5f,330,new(.74f,.62f,.78f),.95f),
                    ("BrekkBody",-37.0f,6.2f,90,new(.7f,.66f,.6f),1),
                };
                foreach(var s in standing){var p=Folk("Standing at the back of the meeting",s.body,new Vector2(s.x,s.z),s.yaw,s.tint,crowd,s.build,null,s.x<-36.2f?.5f:0);Pose(p,0,"base","hand");if(s.x>-24&&s.x<-23)forBerth.Add(p.transform);}
                // Hal's nine apprentices behind him: three born on the ring, six off the convoy.
                apprentices=new GameObject("Hal's apprentices at the meeting").transform;apprentices.SetParent(meeting,true);
                var nine=new (string body,float x,float z,Color tint,float build)[]
                {
                    ("PellBody",-20.4f,3.9f,new(.86f,.7f,.52f),.9f),("IshenBody",-18.6f,4.2f,new(.6f,.66f,.74f),.94f),("CorraBody",-17.0f,3.6f,new(.76f,.6f,.66f),.92f),
                    ("TeoBody",-20.0f,2.4f,new(.62f,.74f,.56f),.96f),("JaskBody",-18.2f,2.6f,new(.84f,.72f,.6f),.9f),("DovBody",-16.8f,1.9f,new(.58f,.66f,.8f),.93f),
                    ("PimBody",-19.6f,.9f,new(.8f,.62f,.5f),.88f),("GaroBody",-18.0f,1.0f,new(.66f,.7f,.6f),.95f),("QuillBody",-16.9f,.2f,new(.74f,.66f,.78f),.91f),
                };
                foreach(var a in nine){var p=Folk("One of Hal's apprentices",a.body,new Vector2(a.x,a.z),305,a.tint,apprentices,a.build);Pose(p,0,"base","hand");}
                // Dace's ten at the back, by the promenade.
                crewRoom=new GameObject("Dace's crew at the meeting").transform;crewRoom.SetParent(meeting,true);
                var ten=new (string body,float x,float z,Color tint,float build)[]
                {
                    ("SurvivorNatural",-39.2f,-.1f,new(.74f,.68f,.56f),1.02f),("BrekkBody",-38.2f,-.9f,new(.6f,.66f,.54f),.98f),("AumiBody",-37.2f,-.2f,new(.7f,.6f,.5f),.95f),
                    ("SurvivorNatural",-36.2f,-.9f,new(.66f,.72f,.76f),.96f),("CorraBody",-35.2f,-.3f,new(.74f,.66f,.54f),1.03f),("IshenBody",-34.2f,-.9f,new(.62f,.6f,.66f),1),
                    ("PellBody",-35.4f,1.0f,new(.72f,.7f,.6f),.92f),("SurvivorNatural",-34.6f,1.6f,new(.7f,.62f,.5f),1.05f),("BrekkBody",-35.3f,2.2f,new(.56f,.6f,.66f),.99f),
                };
                foreach(var c in ten){var p=Folk("One of Dace's crew at the meeting",c.body,new Vector2(c.x,c.z),20,c.tint,crewRoom,c.build);Pose(p,0,"base","hand");}
                When("The town meeting",meeting.gameObject,Gathered,null,null,Held,false,day);
                // What points the main quest's arrow at whoever's turn it is (report Meeting; never offered: range 0).
                foreach(var (who,flag,after) in new[]{(orrin,"home.meetingOpened",Gathered),(meret,"home.offerMade","home.meetingOpened"),(mHal,"home.halAsked","home.offerMade"),(mMira,"home.manifestShown","home.halAsked"),
                    (mIlo,"home.namesRead","home.manifestShown"),(mOda,"home.odaStood","home.namesRead"),(orrin,"home.orrinSaid","home.odaStood"),(orrin,Held,"home.orrinSaid")})
                {
                    var turn=kit.Discovery("The meeting's turn: "+flag,new Vector3(who.transform.position.x,0,who.transform.position.z),"","flag","Meeting",null,"Taren",null,0,0);
                    turn.flag=flag;turn.requiredFlag=after;turn.range=0;turn.situation="";turn.transform.SetParent(meeting,true);
                }
                MeetingCues(copy);
            }
            static void Bench(Vector2 at,Transform parent)
            {
                var s=new ZoneKit.Shape();var steel=new Color(.3f,.33f,.37f);var seat=new Color(.56f,.5f,.42f);
                foreach(float dx in new[]{-1.9f,0f,1.9f})s.Box(new Vector3(at.x+dx,.21f,at.y),new Vector3(.08f,.42f,.36f),Quaternion.identity,steel);
                for(int i=0;i<3;i++)s.Box(new Vector3(at.x,.44f,at.y-.13f+i*.13f),new Vector3(4.2f,.04f,.11f),Quaternion.identity,ZoneKit.Shape.Dim(seat,.92f+i*.05f));
                var go=Made("A meeting bench",s,parent,paint);ZoneKit.Block(go,new Vector3(at.x,.23f,at.y),new Vector3(4.2f,.46f,.42f));
            }
            static Transform Papers(Transform who,string name,Color cover)
            {
                var s=new ZoneKit.Shape().Box(Vector3.zero,new Vector3(.2f,.03f,.28f),Quaternion.identity,cover).Box(new Vector3(0,.018f,0),new Vector3(.18f,.006f,.26f),Quaternion.identity,new Color(.95f,.93f,.86f));
                var go=kit.Built(name,s,paint);go.name=name;var rig=who.GetComponentInChildren<Animator>(true);var hand=rig!=null&&rig.isHuman?rig.GetBoneTransform(HumanBodyBones.RightHand):null;
                go.transform.SetParent(hand!=null?hand:who,false);go.transform.localPosition=Vector3.zero;return go.transform;
            }
            /// <summary>The meeting's eight talks, line by line (Home.yarn): the room turns to the speaker of each line; a speaker addressed turns to whom they answer.</summary>
            static void MeetingCues(Transform copy)
            {
                LineCues Cues(string node,string flag){var c=new GameObject("The meeting, line by line: "+node,typeof(LineCues)).GetComponent<LineCues>();c.transform.SetParent(meeting,true);c.node=node;c.doneFlag=flag;return c;}
                LineCues.Cue Room(int line,Transform to,string hero=null)=>new(){line=line,act=LineCues.Act.Face,who=crowd,toward=to,hero=hero,group=true};
                LineCues.Cue[] Everyone(int line,Transform to,string hero=null)=>new[]{Room(line,to,hero),new(){line=line,act=LineCues.Act.Face,who=apprentices,toward=to,hero=hero,group=true},new(){line=line,act=LineCues.Act.Face,who=crewRoom,toward=to,hero=hero,group=true}};
                LineCues.Cue Turn(int line,Npc who,Transform to,string hero=null)=>new(){line=line,act=LineCues.Act.Face,who=who.transform,toward=to,hero=hero};
                LineCues.Cue Stance(int line,Transform who,string pose,bool group=false)=>new(){line=line,act=LineCues.Act.Pose,who=who,pose=pose,group=group};
                var o=orrin.transform;var m=meret.transform;
                Cues("OrrinMeetingOpens","home.meetingOpened").cues=Everyone(1,o).Concat(new[]{Turn(1,meret,o)}).Concat(Everyone(2,m)).Concat(Everyone(3,o)).ToArray();
                Cues("MeretBerthOffer","home.offerMade").cues=Everyone(1,m).Concat(new[]{Turn(1,orrin,m),Turn(4,meret,null,"Taren")}).Concat(Everyone(4,null,"Taren")).Concat(Everyone(5,m)).ToArray();
                Cues("HalApprentices","home.halAsked").cues=Everyone(1,mHal.transform).Concat(new[]{Turn(1,meret,mHal.transform),Turn(1,orrin,mHal.transform)}).Concat(Everyone(2,m)).Concat(Everyone(3,mHal.transform))
                    .Concat(new[]{Room(4,m),new LineCues.Cue{line=4,act=LineCues.Act.Face,who=apprentices,toward=mHal.transform,group=true}}).Concat(Everyone(5,mHal.transform)).ToArray();
                Cues("MiraManifest","home.manifestShown").cues=Everyone(1,mMira.transform).Concat(new[]{Turn(1,meret,mMira.transform),
                    new LineCues.Cue{line=2,act=LineCues.Act.Move,who=m,to=new Vector3(-27.6f,0,7.2f),turn=new Vector3(0,80,0),seconds=2.4f},
                    new LineCues.Cue{line=2,act=LineCues.Act.Hold,who=copy,toward=mMira.transform,seconds=0},Turn(3,mMira,mJun.transform)}).Concat(Everyone(3,mJun.transform))
                    .Concat(new[]{Turn(4,meret,mMira.transform),new LineCues.Cue{line=4,act=LineCues.Act.Move,who=m,to=new Vector3(-35.2f,0,8.1f),turn=new Vector3(0,165,0),seconds=2.4f}}).Concat(Everyone(4,m)).Concat(Everyone(5,mMira.transform)).ToArray();
                Cues("IloReadsNames","home.namesRead").cues=Everyone(1,mIlo.transform).Concat(Everyone(2,mJun.transform)).Concat(Everyone(3,mDace.transform)).Concat(Everyone(4,mOda.transform)).ToArray();
                Cues("OdaStands","home.odaStood").cues=new[]{Stance(1,mOda.transform,"stand")}.Concat(Everyone(1,mOda.transform)).Concat(Everyone(2,mDace.transform))
                    .Concat(new[]{Turn(2,mDace,m),Stance(3,mJun.transform,"stand"),Turn(3,mMira,mJun.transform)}).Concat(Everyone(3,mJun.transform))
                    .Concat(new[]{Stance(4,mOda.transform.parent.Find("Townsperson: Oda's mother")??mOda.transform,"stand")}).Concat(Everyone(4,mOda.transform)).ToArray();
                Cues("OrrinPostponed","home.orrinSaid").cues=Everyone(1,o).Concat(new[]{Turn(1,meret,o),Turn(1,mOda,o),Turn(1,mJun,o)}).ToArray();
                var withdraws=Everyone(1,o).ToList();
                foreach(var p in forBerth)withdraws.Add(Stance(1,p,"hand"));
                withdraws.AddRange(new[]{Stance(2,crowd,"hand",true),Stance(2,apprentices,"hand",true),Stance(2,crewRoom,"hand",true)});foreach(var p in forBerth)withdraws.Add(Stance(2,p,"base"));
                withdraws.AddRange(new[]{Stance(3,crowd,"base",true),Stance(3,apprentices,"base",true),Stance(3,crewRoom,"base",true)});
                withdraws.AddRange(Everyone(4,m));withdraws.Add(Turn(4,orrin,m));withdraws.AddRange(Everyone(6,mHal.transform));withdraws.Add(Turn(6,meret,mHal.transform));
                Cues("MeretWithdraws",Held).cues=withdraws.ToArray();
            }

            // ---------------------------------------------------------------- Mira's stall on the day (DecksDefinition)
            /// <summary>The flag that opens Mira's road crates: the town meeting held, the Decks committed to leaving.</summary>
            internal const string MiraOpens="home.meetingHeld";
            internal static void Shop()
            {
                ShopOffer S(string id,int price,int lot=0)=>Sell(id,price,lot);
                var mira=ProgressionBuilder.Stall(DecksZone+"_Mira","PITCH NINE, CRATED",S("RepairGel",28),S("RepairSalve",68),S("ChargeCell",32),S("ChargeBank",68),S("SyncTonic",100),S("RelinkSpike",100),
                    S("EmittersT4",960),S("EdgesT4",1020),S("FramesT4",1050),S("DrivesT4",920),S("ModulesT4",1080),S("ScrapAlloy",16,3),S("HaloRivet",60,3));
                // Once the Decks have voted to go (the meeting held) she opens the crates she packed for the road: the Choirwrought chapter 10 is sized on (D326). Before this
                // nothing sold it until Neighbourhood Afloat, after the last boss.
                mira.offers=mira.offers.Concat(ProgressionBuilder.Choirwrought(MiraOpens)).ToArray();mira.noteFlag=MiraOpens;mira.note="THE ROAD CRATES ARE OPEN  ·  CHOIRWROUGHT";
                UnityEditor.EditorUtility.SetDirty(mira);
            }

            // ---------------------------------------------------------------- the market district (the Coronach's launch lifts it)
            /// <summary>Everything that lifts with the market at the launch, under one root at the scene's top (docs/maps/DecksLeaving.md, "For the Coronach"): the market hull's floors, walls,
            /// keel, dressing, docks and people, east of Taren's yellow line on the promenade and inside the hull's own walls everywhere else. The promenade parts exactly under the line:
            /// the floor plates the line crosses are cut in two along it, and so are the line itself (a half to each side), the promenade's runner and the floor details batched across it.
            /// The bridges stay with their other hulls. Moved, not rebuilt: everything keeps its static flags and its place (the Coronach takes BatchingStatic off what moves).</summary>
            static void District()
            {
                const float Line=-10.6f,West=-12.5f,East=12.5f,BandSouth=-6.75f,BandNorth=0f;
                var district=new GameObject("Market district").transform;
                // In the promenade's band the line is the edge; elsewhere the hull's walls are. A wall running mostly outside the band, with a foot in it, goes by the hull's rule.
                bool Market(Bounds b)=>b.min.x>=West&&b.max.x<=East&&(b.min.x>=Line-.02f||Mathf.Min(b.max.z,BandNorth)-Mathf.Max(b.min.z,BandSouth)<.8f);
                bool Office(Bounds b)=>b.max.x<=Line+.02f||b.max.x<West+.4f||b.min.x>East-.4f;
                // The two floor plates the line crosses, cut along it; their band's pressure plate cut with them.
                foreach(var tile in All<Transform>().Where(t=>t!=null&&t.name=="DeckFloor"&&t.parent!=null&&t.parent.name=="Deck floor modules"&&Mathf.Abs(t.position.x+10)<.3f&&t.position.y>-.3f&&t.position.y<.1f&&t.position.z<0&&t.position.z>-7).ToArray())
                {
                    var band=tile.parent;float z=tile.position.z;Gone(tile.gameObject);
                    foreach(var (x0,x1) in new[]{(-12f,Line),(Line,-8f)})
                    {
                        var plate=WorldBuilder.Piece("DeckFloor",new Vector3((x0+x1)*.5f,-.08f,z),new Vector3(x1-x0,.16f,3.5f),"Ground",true);StationSurfaces.FloorFinish(plate,"public");
                        plate.name="DeckFloor";plate.transform.SetParent(band,true);
                    }
                    var backing=band!=null?band.Find("Sealed deck backing"):null;
                    if(backing!=null&&ModelGeometry.BoundsOf(backing.gameObject).size.x>20)
                    {
                        var plate=backing.GetComponentInChildren<Renderer>().sharedMaterial;Gone(backing.gameObject);
                        var p=new GameObject("Sealed deck backing");var body=(GameObject)PrefabUtility.InstantiatePrefab(ZoneKit.Prefab("Environment","DeckWall"));body.transform.SetParent(p.transform,false);body.transform.localRotation=Quaternion.Euler(90,0,0);
                        StationSurfaces.Fit(p,new Vector3(20,.035f,7));p.transform.position+=new Vector3(2,-.185f,-3.5f)-ModelGeometry.BoundsOf(p).center;
                        foreach(var c in p.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);foreach(var r in p.GetComponentsInChildren<Renderer>())r.sharedMaterial=plate;p.isStatic=true;p.transform.SetParent(band,true);
                    }
                }
                // Taren's line, a half to each side.
                var line=All<Transform>().First(t=>t.name=="Walkway safety line");
                foreach(var (name,dx) in new[]{("Walkway safety line, office half",-.06f),("Walkway safety line, market half",.06f)})
                {
                    var half=Object.Instantiate(line.gameObject,line.parent);half.name=name;half.transform.position=line.position+new Vector3(dx,0,0);half.transform.localScale=new Vector3(line.localScale.x*.5f,line.localScale.y,line.localScale.z);
                    // Never batched, as chapter 1's line isn't: a batched quad reports the whole batch's mesh, and the line read as 34 m of stretched paint.
                    GameObjectUtility.SetStaticEditorFlags(half,GameObjectUtility.GetStaticEditorFlags(half)&~StaticEditorFlags.BatchingStatic);
                }
                Gone(line.gameObject);
                // The promenade's and the service gallery's runners, each one quad end to end, cut at the district's edges.
                foreach(var runner in All<Transform>().Where(t=>t!=null&&t.name=="Walkway runner"&&Mathf.Abs(t.position.x)<.2f&&(Mathf.Abs(t.position.z+3)<.2f||Mathf.Abs(t.position.z-21.5f)<.2f)).ToArray())
                {
                    var view=runner.GetComponent<MeshRenderer>();var mesh=runner.GetComponent<MeshFilter>().sharedMesh;float width=mesh.bounds.size.x,y=runner.position.y,z=runner.position.z;
                    float cut=z<0?Line:-12;
                    foreach(var (x0,x1) in new[]{(-39.7f,cut),(cut,12f),(12f,39.7f)})
                    {
                        float hl=(x1-x0)*.5f,hw=width*.5f,v=(x1-x0)/width;
                        var m=new Mesh{name="DecksLeaving runner "+x0+".."+x1+" "+z,vertices=new[]{new Vector3(-hw,0,-hl),new Vector3(hw,0,-hl),new Vector3(-hw,0,hl),new Vector3(hw,0,hl)},uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,v),new Vector2(1,v)},triangles=new[]{0,2,1,2,3,1}};
                        m.RecalculateNormals();m.RecalculateBounds();var saved=ZoneKit.SaveMesh(m,"Assets/_Project/Scenes/"+DecksZone+"-runner-"+(++built)+".asset");
                        var go=new GameObject("Walkway runner",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(runner.parent,false);go.transform.SetPositionAndRotation(new Vector3((x0+x1)*.5f,y,z),runner.rotation);
                        go.GetComponent<MeshFilter>().sharedMesh=saved;var r=go.GetComponent<MeshRenderer>();r.sharedMaterials=view.sharedMaterials;r.shadowCastingMode=ShadowCastingMode.Off;go.isStatic=true;
                    }
                    Gone(runner.gameObject);
                }
                // Floor details batched across the district's edge (studs, paint, grates): each batch cut by where its triangles lie.
                foreach(var detail in All<MeshFilter>().Where(f=>f!=null&&f.name.StartsWith("Floor detail")&&f.sharedMesh!=null&&f.sharedMesh.isReadable).ToArray())
                {
                    var view=detail.GetComponent<MeshRenderer>();if(view==null)continue;var b=view.bounds;if(b.max.x<West||b.min.x>East||Market(b))continue;
                    var mesh=detail.sharedMesh;var world=detail.transform.localToWorldMatrix;var v=mesh.vertices;var uv=mesh.uv;var col=mesh.colors;var tris=mesh.triangles;
                    var parts=new[]{new List<int>(),new List<int>()};
                    for(int i=0;i<tris.Length;i+=3){var c=world.MultiplyPoint3x4((v[tris[i]]+v[tris[i+1]]+v[tris[i+2]])/3);var tb=new Bounds(c,Vector3.zero);parts[Market(tb)?0:1].AddRange(new[]{tris[i],tris[i+1],tris[i+2]});}
                    if(parts[0].Count==0||parts[1].Count==0)continue;
                    for(int k=0;k<2;k++)
                    {
                        var m=new Mesh{name=mesh.name+(k==0?" market":" rest"),indexFormat=IndexFormat.UInt32,vertices=v};if(uv.Length==v.Length)m.uv=uv;if(col.Length==v.Length)m.colors=col;m.triangles=parts[k].ToArray();m.RecalculateNormals();m.RecalculateBounds();
                        var saved=ZoneKit.SaveMesh(m,"Assets/_Project/Scenes/"+DecksZone+"-detail-"+(++built)+".asset");
                        var go=new GameObject(detail.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(detail.transform.parent,false);go.transform.SetPositionAndRotation(detail.transform.position,detail.transform.rotation);go.transform.localScale=detail.transform.localScale;
                        go.GetComponent<MeshFilter>().sharedMesh=saved;var r=go.GetComponent<MeshRenderer>();r.sharedMaterials=view.sharedMaterials;r.shadowCastingMode=view.shadowCastingMode;go.isStatic=true;
                    }
                    Gone(detail.gameObject);
                }
                Physics.SyncTransforms();
                // Everything wholly the market's goes under the district, the deepest whole thing first found; what straddles is opened and its children asked in turn.
                Bounds? Extent(Transform t)
                {
                    Bounds? all=null;void Add(Bounds b){if(all is Bounds a){a.Encapsulate(b);all=a;}else all=b;}
                    foreach(var r in t.GetComponentsInChildren<Renderer>(true))if(r is not ParticleSystemRenderer)Add(r.bounds);
                    foreach(var c in t.GetComponentsInChildren<Collider>(true))Add(c.bounds);
                    foreach(var l in t.GetComponentsInChildren<Light>(true))if(l.type!=LightType.Directional)Add(new Bounds(l.transform.position,Vector3.one*.2f));
                    return all;
                }
                int moved=0;var straddling=new List<string>();
                void Collect(Transform t)
                {
                    if(t==district||t==carrying||t.GetComponent<ZoneController>()!=null||t.GetComponent<Lattice.Combat.GroundNavigation>()!=null)return;
                    var e=Extent(t);if(e==null)return;
                    if(Market(e.Value)){t.SetParent(district,true);moved++;return;}
                    if(e.Value.max.x<West||e.Value.min.x>East)return;
                    if(t.childCount==0){if(!Office(e.Value))straddling.Add(t.name+" "+e.Value.center);return;}
                    foreach(var child in t.Cast<Transform>().ToArray())Collect(child);
                }
                var scene=EditorSceneManager.GetActiveScene();
                // The keel's plates and beams under the promenade that the line crosses, cut along it as the floor is, so nothing of the office's side shows where the market was.
                var crossing=All<Transform>().Where(t=>t!=null&&(t.name=="DeckFloor"||t.name=="DeckWall")&&(t.parent==null||t.parent.name!=t.name)&&t.position.y<-.5f&&t.position.z>BandSouth-.5f&&t.position.z<BandNorth+.5f).ToArray();
                foreach(var wrap in crossing)
                {
                    if(wrap==null)continue;var b=ModelGeometry.BoundsOf(wrap.gameObject);if(!(b.min.x<Line-.05f&&b.max.x>Line+.05f))continue;
                    bool solid=wrap.GetComponent<BoxCollider>()!=null;var parent=wrap.parent;
                    foreach(var (x0,x1) in new[]{(b.min.x,Line),(Line,b.max.x)})
                    {var piece=WorldBuilder.Piece(wrap.name,new Vector3((x0+x1)*.5f,b.center.y,b.center.z),new Vector3(x1-x0,b.size.y,b.size.z),"Rock",solid);piece.transform.SetParent(parent,true);}
                    Gone(wrap.gameObject);
                }
                Physics.SyncTransforms();
                foreach(var root in scene.GetRootGameObjects().Select(g=>g.transform).ToArray())Collect(root);
                district.SetAsLastSibling();
                Debug.Log($"DECKS_MARKET_DISTRICT moved={moved} straddling={straddling.Count} {string.Join("; ",straddling.Take(12))}");
            }

            // ---------------------------------------------------------------- checks and the bake
            /// <summary>Refuses the finished town if anything solid it stands cuts a way (Rootmarket's rule): its footprint within .9 m of a way's line, a townsperson within 2 m, a
            /// walker's corner within 1.6 m. Every state at once: the meeting's room and Hal's packed crate stand on the ways as much as the day does. Chapter 1's walls and floors are
            /// what the ways run between, so only what the day adds is held to it.</summary>
            static void NothingStandsOnTheWays()
            {
                var hidden=new[]{day,meeting,carrying,wayDown}.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>!t.gameObject.activeSelf).Select(t=>t.gameObject).ToArray();
                foreach(var go in hidden)go.SetActive(true);
                try{Check();}finally{foreach(var go in hidden)go.SetActive(false);}
                void Check()
                {
                    Physics.SyncTransforms();var faults=new List<string>();
                    float Outside(Vector2 p,Vector2 centre,float yaw,Vector2 half)
                    {var d=p-centre;float r=yaw*Mathf.Deg2Rad,x=d.x*Mathf.Cos(r)-d.y*Mathf.Sin(r),z=d.x*Mathf.Sin(r)+d.y*Mathf.Cos(r);float ox=Mathf.Abs(x)-half.x,oz=Mathf.Abs(z)-half.y;return new Vector2(Mathf.Max(ox,0),Mathf.Max(oz,0)).magnitude+Mathf.Min(Mathf.Max(ox,oz),0);}
                    (float gap,Vector2 from,Vector2 to) Nearest(Vector2 centre,float yaw,Vector2 half)
                    {
                        var best=(float.PositiveInfinity,Vector2.zero,Vector2.zero);
                        foreach(var way in Ways)for(int i=1;i<way.Length;i++){int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(way[i-1],way[i])/.1f));for(int k=0;k<=steps;k++){float gap=Outside(Vector2.Lerp(way[i-1],way[i],k/(float)steps),centre,yaw,half);if(gap<best.Item1)best=(gap,way[i-1],way[i]);}}
                        return best;
                    }
                    foreach(var solid in new[]{day,meeting,carrying,wayDown}.SelectMany(r=>r.GetComponentsInChildren<Collider>(true)))
                    {
                        if(!solid.enabled||solid.isTrigger||solid.GetComponentInParent<Npc>(true)!=null||solid.GetComponentInParent<WalkLoop>(true)!=null)continue;
                        var bounds=solid.bounds;if(bounds.max.y<.32f)continue;
                        bool folk=solid.name.StartsWith("Townsperson");var at=new Vector2(bounds.center.x,bounds.center.z);float yaw=0;var half=new Vector2(bounds.extents.x,bounds.extents.z);
                        if(solid is BoxCollider box&&Mathf.Abs(solid.transform.eulerAngles.x)<.5f&&Mathf.Abs(Mathf.DeltaAngle(solid.transform.eulerAngles.z,0))<.5f)
                        {var c=solid.transform.TransformPoint(box.center);at=new Vector2(c.x,c.z);yaw=solid.transform.eulerAngles.y;var size=Vector3.Scale(box.size,solid.transform.lossyScale);half=new Vector2(Mathf.Abs(size.x),Mathf.Abs(size.z))*.5f;}
                        if(folk){half=Vector2.zero;yaw=0;}
                        var (gap,from,to)=Nearest(at,yaw,half);float need=folk?FolkClear:WayClear;
                        if(gap<need)faults.Add($"{(solid.transform.parent!=null?solid.transform.parent.name+"/":"")}{solid.name} at ({at.x:0.0}, {at.y:0.0}) is {gap:0.00} m from the way from {from} to {to} (needs {need})");
                    }
                    foreach(var walk in carrying.GetComponentsInChildren<WalkLoop>(true))foreach(var p in walk.path)
                    {var (gap,from,to)=Nearest(new Vector2(p.x,p.z),0,Vector2.zero);if(gap<1.6f)faults.Add($"{walk.name}'s loop at ({p.x:0.0}, {p.z:0.0}) is {gap:0.00} m from the way from {from} to {to}");}
                    if(faults.Count>0)throw new InvalidOperationException("DecksLeaving: things stand on the day's ways:\n"+string.Join("\n",faults.Distinct()));
                    Debug.Log("DECKS_WAYS_CLEAR "+Ways.Length);
                }
            }
            /// <summary>The walking paths with the day standing, Hal's bench standing, and every place a player must reach. The station's floors are boxes and meshes; chapter 1's own bake
            /// is replaced. The meeting's room is not in the bake: it stands only from the gathering to the vote, and baked, its forty people and six benches stayed holes in the dock
            /// office's floor all day (Oda's door at the stamping window had no floor in reach, and the partner walked round people who had gone). It carves its own while it stands.</summary>
            static void Bake()
            {
                var hidden=new[]{day}.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>!t.gameObject.activeSelf&&t.GetComponent<Collider>()!=null).Select(t=>t.gameObject).ToArray();
                // Shown for the bake, then as they were: what stands from a flag is in the walking paths whenever it stands.
                var shown=new List<GameObject>();foreach(var go in hidden){if(!go.activeSelf){go.SetActive(true);shown.Add(go);}}
                bool room=meeting.gameObject.activeSelf;meeting.gameObject.SetActive(true);Physics.SyncTransforms();
                foreach(var solid in meeting.GetComponentsInChildren<Collider>(true))
                {
                    if(solid.isTrigger||solid.GetComponent<UnityEngine.AI.NavMeshObstacle>()!=null)continue;
                    var cut=solid.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();cut.shape=UnityEngine.AI.NavMeshObstacleShape.Box;cut.carving=true;cut.carveOnlyStationary=true;
                    if(solid is BoxCollider box){cut.center=box.center;cut.size=box.size;}
                    else{var b=solid.bounds;cut.center=solid.transform.InverseTransformPoint(b.center);var s=solid.transform.lossyScale;cut.size=new Vector3(b.size.x/Mathf.Max(.01f,Mathf.Abs(s.x)),b.size.y/Mathf.Max(.01f,Mathf.Abs(s.y)),b.size.z/Mathf.Max(.01f,Mathf.Abs(s.z)));}
                }
                meeting.gameObject.SetActive(false);
                // Whatever carves its own at runtime (the walk's people, shown and hidden by the story) is out of the bake, as the meeting is.
                var carving=Object.FindObjectsByType<UnityEngine.AI.NavMeshObstacle>(FindObjectsSortMode.None).Where(o=>o.carving&&o.gameObject.activeSelf).Select(o=>o.gameObject).Distinct().ToArray();
                foreach(var go in carving)go.SetActive(false);
                try
                {
                    var points=new List<Vector2>{Board.stand+new Vector2(1.6f,-.4f),NoticeBoard.stand+new Vector2(0,-1.2f),new Vector2(-12.2f,-1.2f),new Vector2(-7.7f,-2.4f),
                        LockNine.stand+new Vector2(1.6f,0),new Vector2(6,-11.4f),DockRail.stand+new Vector2(-1.4f,1.6f),BoltHoles.stand+new Vector2(-1.4f,-1.2f),new Vector2(-19.6f,-4.2f),new Vector2(14,-2.4f),
                        new Vector2(-29.4f,8.3f),new Vector2(-6.4f,.4f),new Vector2(-34.6f,6.4f),MeetingSpawn,WorksSpawn,LiftSpawn,new Vector2(-28,-.8f),new Vector2(22.6f,-10.6f),
                        new Vector2(-21,7),new Vector2(-28.9f,2.3f),new Vector2(-27,4.0f),new Vector2(38.2f,3.4f),new Vector2(24,4.3f)};
                    kit.BakeNavigation(new Bounds(new Vector3(0,1,4),new Vector3(84,8,48)),new Vector2(-28,-11),points);
                    var nav=All<GroundNavigation>().First();nav.transform.SetParent(null,true);
                }
                finally{foreach(var go in shown)go.SetActive(false);meeting.gameObject.SetActive(room);foreach(var go in carving)go.SetActive(true);}
            }
            /// <summary>For stills only (<c>-decks-still meeting|hal</c>): the scene saved in that moment so LookDev can see it. Never committed: build again without it.</summary>
            static void ApplyStill()
            {
                var still=Still;if(still=="")return;
                Debug.LogWarning("DECKS_STILL "+still+": this DecksLeaving is posed for stills; build it again without -decks-still");
                if(still=="meeting")
                {
                    meeting.gameObject.SetActive(true);
                    foreach(var w in day.GetComponentsInChildren<Whereabouts>(true).Where(w=>w.awayFrom==Gathered))foreach(var t in w.targets)t.SetActive(false);
                    foreach(var w in day.GetComponentsInChildren<Whereabouts>(true))if(w.name=="Orrin at his board (when)")foreach(var t in w.targets)t.SetActive(true);
                    // Mid-vote: the room's hands up against the berth, the four for it down, turned to Orrin at the board.
                    foreach(var body in crowd.Cast<Transform>().Concat(apprentices.Cast<Transform>()).Concat(crewRoom.Cast<Transform>()))
                    {
                        var p=body.GetComponent<Posed>();if(p!=null&&!forBerth.Contains(body))Turned(p,"hand");
                        var look=orrin.transform.position-body.position;look.y=0;body.rotation=Quaternion.LookRotation(look);
                    }
                    Turned(mOda.GetComponent<Posed>(),"stand");Turned(mJun.GetComponent<Posed>(),"stand");
                }
                else if(still=="hal")
                {
                    // Hal's fifth line: two pieces off and lying in the row, a third in the air, Taren holding the lamp beside him.
                    for(int n=0;n<4;n++){pieces[n].SetPositionAndRotation(n<3?laidOut[n]:Vector3.Lerp(pieces[n].position,laidOut[n],.5f)+Vector3.up*.6f,laidTurn[n]);}
                    var hal=named.First(n=>n.speaker=="Hal");hal.transform.SetPositionAndRotation(new Vector3(30.6f,0,4.7f),Quaternion.LookRotation(new Vector3(1.4f,0,1.3f)));
                    var taren=(GameObject)PrefabUtility.InstantiatePrefab(ZoneKit.Prefab("Characters","TarenNatural"));taren.name="Still: Taren holding the lamp";
                    taren.transform.SetPositionAndRotation(new Vector3(31.0f,0,3.0f),Quaternion.LookRotation(new Vector3(1,0,2.6f)));taren.transform.SetParent(day,true);
                    var lamp=day.GetComponentsInChildren<Transform>(true).First(t=>t.name=="The hand lamp");lamp.gameObject.SetActive(true);
                    var rig=taren.GetComponentInChildren<Animator>();var hand=rig!=null?rig.GetBoneTransform(HumanBodyBones.RightHand):null;
                    if(hand!=null){lamp.SetParent(hand,false);lamp.localPosition=Vector3.zero;}else lamp.position=new Vector3(31.4f,1.1f,3.3f);
                }
            }
            static void Turned(Posed posed,string pose)
            {
                if(posed==null)return;var shown=posed.poses.FirstOrDefault(p=>p.name==pose);if(shown==null)return;
                var rig=posed.GetComponentInChildren<Animator>(true);using var handler=new HumanPoseHandler(rig.avatar,rig.transform);
                var hp=new HumanPose{bodyPosition=shown.body,bodyRotation=shown.turn,muscles=shown.muscles.ToArray()};handler.SetHumanPose(ref hp);posed.current=pose;
            }

            // ---------------------------------------------------------------- LookDev's views (LookDev.Views names these)
            internal static readonly LookDev.View[] Views=
            {
                new("arrival",new Vector3(-28,0,-11)),new("promenade-office",new Vector3(-26,0,-3)),new("walkway-joint",new Vector3(-14,0,-3)),new("stall",new Vector3(-9.6f,0,-2.6f)),
                new("market",new Vector3(-2,0,2)),new("promenade-market",new Vector3(4,0,-3)),new("way-down",new Vector3(5.4f,0,-11.6f)),new("lock-nine",new Vector3(-2,0,-12.6f)),
                new("bridge-east",new Vector3(14,0,-3)),new("workshop",new Vector3(30,0,2.4f)),new("hal-bench",new Vector3(32.6f,0,3.4f)),new("tug-dock",new Vector3(32,0,-12)),
                new("dock-office",new Vector3(-30,0,2)),new("meeting",new Vector3(-28,0,2.6f)),new("meeting-wide",new Vector3(-28,0,3.4f),false,1.6f),new("notice-board",new Vector3(-25,0,-9.6f)),
                new("galley",new Vector3(0,0,14)),new("cabins",new Vector3(-28,0,14)),new("service",new Vector3(10,0,21.5f)),new("overview",new Vector3(0,0,3),false,3.6f),
            };
        }
    }
}
