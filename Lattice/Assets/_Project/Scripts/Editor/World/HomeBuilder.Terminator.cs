using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The Terminator (wild, combat; brief, "MeridianCrawl and Terminator"): the run against the dawn line to tread-hall four, three couplings re-mated, the engine car ridden out.
    /// No boss. Map brief, the dawn's timetable, coordinates and contracts: docs/maps/Terminator.md. Everything of the zone's own is in <see cref="Terminator"/>.
    /// <para>The run goes due south, toward the fixed camera, so the dawn comes down the screen behind the party (<see cref="DawnLine"/>, axis (0,-1)). The cars stand in file west of the
    /// way on a flattened drag lane, each with its coupling at its front; the way swings seven metres east of each car as it passes (nothing that tall may stand on the camera's line to
    /// the party) and comes back to the coupling's seat south of the car's front. <c>-terminator-greybox</c> leaves out the dressing and the ground kit (the blockout).</para>
    /// </summary>
    public static partial class HomeBuilder
    {
        /// <summary>Its bed (tools/duskline_ambience.py: cold wind over grit, frost ticking, the dawn's low roar behind the ridge, the Crawl far off) at its measured level.</summary>
        static void TerminatorDefinition(ZoneDef z)
        {
            z.brightFloor=false;
            var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(Terminator.Bed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=Terminator.BedLevel;}
        }
        /// <summary>Long blue dusk: the sun is the dawn, low behind the line to the north (the dawn line's runtime turns it as the line nears), and the fill is the night ahead.
        /// The light behind the line is the Lattice/Dawn shader's, brighter than anything else in the frame; the bloom's threshold sits under it.</summary>
        [ZoneLook.Source] static void TerminatorLooks()
        {
            // The night side slate-blue, not lavender: with as much red as green in the fill and the split, and the saturation pushed, the plain read flat purple in the player
            // (the November 20 capture, s56).
            ZoneLook.Register(TerminatorZone,new ZoneLook.Spec{sun=C(1f,.8f,.62f),sunIntensity=.75f,sunPitch=Terminator.SunPitch,shadowStrength=.9f,
                sky=C(.26f,.35f,.54f),equator=C(.3f,.35f,.43f),ground=C(.12f,.13f,.16f),
                fog=true,fogColor=C(.32f,.38f,.5f),fogStart=55,fogEnd=230,
                contrast=13,saturation=0,splitShadows=C(.33f,.43f,.6f),splitHighlights=C(.68f,.55f,.42f),
                bloomThreshold=1.05f,bloomIntensity=.7f,vignette=.24f});
        }

        /// <summary>The dawn line nearer to how it is played. LookDev's stills never run a particle, and the white band along s56's line in the November 20 capture was in no editor still.
        /// This opens the zone with the live pipeline, stands the line where each named view has it, runs its sparks and steam six seconds there (their count is logged) and draws the
        /// view at the play zoom with Taren in it; with <c>-dawn-before</c> each view again with the light and steam as they were before November 21. Unverified: on November 21 the
        /// simulated particles did not show in these draws, and the lit ground stayed far under the player's white, so the player still differs in something these draws lack. Run:
        /// exec-when-green.ps1 -Method Lattice.EditorTools.HomeBuilder.TerminatorDawnStills -Marker TERMINATOR_DAWN_STILLS_OK -Graphics -ExtraArgs @('-look-run','name').</summary>
        public static void TerminatorDawnStills()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-look-run")??throw new ArgumentException("-look-run <name> is required");
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/look",run));
            if(System.IO.Directory.Exists(folder))throw new InvalidOperationException("Preserve earlier look run: "+folder);
            System.IO.Directory.CreateDirectory(folder);
            var before=GraphicsSettings.defaultRenderPipeline;var beforeQuality=QualitySettings.renderPipeline;
            var pipeline=Resources.Load<RenderPipelineAsset>("LocalBiomes/LocalPipeline")??throw new InvalidOperationException("TERMINATOR_DAWN_STILLS the live pipeline is missing");
            try
            {
                GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+TerminatorZone+".unity");
                foreach(var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
                var def=Find<ZoneDef>(TerminatorZone);if(def!=null)RenderSettings.skybox=def.skybox;
                var dawn=UnityEngine.Object.FindFirstObjectByType<DawnLine>()??throw new InvalidOperationException("TERMINATOR_DAWN_STILLS no dawn line in the scene");
                var profile=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Ground.asset");
                var camera=new GameObject("Dawn camera",typeof(Camera),typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData)).GetComponent<Camera>();
                camera.clearFlags=RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.nearClipPlane=.3f;camera.farClipPlane=1500;camera.fieldOfView=profile.fov;
                var data=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;
                var rotation=Quaternion.Euler(profile.pitch,profile.yaw,0);int count=0;
                var taren=(GameObject)PrefabUtility.InstantiatePrefab(ZoneKit.Prefab("Characters","TarenNatural"));
                var rig=taren.GetComponentInChildren<Animator>();var idle=rig!=null&&rig.runtimeAnimatorController!=null?rig.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle")):null;
                var effects=dawn.GetComponentsInChildren<ParticleSystem>(true);
                // With -dawn-before each view is drawn a second time as the zone stood before November 21 (the light added at .9 with a 2.4 edge, the steam additive, 80 a second of
                // 1.4 to 3.2 m puffs), the scene's own objects changed in memory only and the light's material put back after: the band as the capture had it, beside the band now.
                var light=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/terminator-dawn-light.mat");
                var kept=(l:light.GetColor("_Light"),e:light.GetColor("_Edge"),w:light.GetFloat("_EdgeWidth"),s:light.GetFloat("_Shimmer"));
                var steam=effects.FirstOrDefault(p=>p.name=="The dawn's steam");
                void Old()
                {
                    light.SetColor("_Light",new Color(.9f,.76f,.54f));light.SetColor("_Edge",new Color(2.4f,2.1f,1.6f));light.SetFloat("_EdgeWidth",.55f);light.SetFloat("_Shimmer",.6f);
                    if(steam==null)return;var m=steam.main;m.startLifetime=new ParticleSystem.MinMaxCurve(3,5.5f);m.startSize=new ParticleSystem.MinMaxCurve(1.4f,3.2f);m.startColor=new Color(1,.93f,.82f,.2f);m.maxParticles=480;
                    var e=steam.emission;e.rateOverTime=80;var r=steam.GetComponent<ParticleSystemRenderer>();var add=new Material(r.sharedMaterial);add.SetFloat("_DstBlend",(float)BlendMode.One);add.SetFloat("_DstBlendAlpha",(float)BlendMode.One);add.SetColor("_BaseColor",new Color(1.05f,1,.92f,1));r.sharedMaterial=add;
                }
                var passes=DevArgs.Has("-dawn-before")?new[]{false,true}:new[]{false};
                try
                {
                    foreach(bool old in passes)
                    {
                        if(old)Old();
                        foreach(var name in new[]{"s56-009","s56-040","arrival","coupling-1","spines-2"})
                        {
                            var still=Terminator.Stills.First(s=>s.name==name);var at=new Vector3(still.at.x,Terminator.Height(still.at),still.at.y);
                            taren.transform.SetPositionAndRotation(at,Quaternion.Euler(0,180,0));if(idle!=null)idle.SampleAnimation(rig.gameObject,.3f);
                            camera.transform.SetPositionAndRotation(at+Vector3.up-rotation*Vector3.forward*profile.distance*Mathf.Max(1,still.zoom),rotation);
                            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;
                            // The first draw stands the line, its veil and its emitters where this view has them; the sparks and steam then run there as they would in play.
                            camera.Render();foreach(var ps in effects)ps.Simulate(6,false,true,true);camera.Render();camera.Render();
                            var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                            System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,TerminatorZone+"-"+name+(old?"-as-played-before":"-as-played")+".png"),image.EncodeToPNG());
                            RenderTexture.active=prior;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);count++;
                            Debug.Log($"TERMINATOR_DAWN_STILL {name}{(old?" before":"")} particles={effects.Sum(p=>p.particleCount)}");
                        }
                    }
                }
                finally{light.SetColor("_Light",kept.l);light.SetColor("_Edge",kept.e);light.SetFloat("_EdgeWidth",kept.w);light.SetFloat("_Shimmer",kept.s);}
                UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(taren);
                Debug.Log($"TERMINATOR_DAWN_STILLS_OK count={count} folder={folder}");
            }
            finally{GraphicsSettings.defaultRenderPipeline=before;QualitySettings.renderPipeline=beforeQuality;}
        });

        internal static class Terminator
        {
            // ---------------------------------------------------------------- the plan: x east, z north; the run goes south, the dawn comes from the north
            internal const float SunPitch=15,Reach=11,CarLength=14,CliffTop=1.6f,WashZ=655,RideDistance=110,LightStarts=751;
            internal const string Bed="Assets/_Project/Audio/Ambience/AMB_Terminator.ogg";internal const float BedLevel=.378f;
            /// <summary>The cars of tread-hall four, back to front: what each is, the flag its coupling sets, the spawn that coupling makes the checkpoint, its centre on the plan, its tint.</summary>
            internal static readonly (string name,string flag,string spawn,Vector2 at,Color tint)[] Cars=
            {
                ("The schoolroom car","home.coupling1","Schoolroom",new(-9,606),new Color(1.02f,.98f,.94f)),
                ("The kitchens car","home.coupling2","Kitchens",new(-9,416),new Color(.97f,.97f,1.02f)),
                ("The engine car","home.coupling3","Engine",new(-9,172),new Color(1,.98f,.98f)),
            };
            internal static Vector2 Front(int car)=>Cars[car].at-new Vector2(0,CarLength*.5f);
            internal static Vector2 Rear(int car)=>Cars[car].at+new Vector2(0,CarLength*.5f);
            /// <summary>Where the drag's eye meets the car's tow beam, the jack's seat beside it (a hero's hands, and the prompt), and the checkpoint a mating makes.</summary>
            internal static Vector2 Socket(int car)=>Front(car)-new Vector2(0,4.5f);
            internal static Vector2 Seat(int car)=>Socket(car)+new Vector2(3,0);
            internal static Vector2 Checkpoint(int car)=>Seat(car)+new Vector2(2.5f,-3);
            internal static readonly Vector2 Arrival=new(0,740),Cart=new(6.5f,744.5f),Lesson=new(-7,716),Buggy=new(-12.5f,690),Board=new(-1.4f,162.4f),
                Tail=new(-3.5f,51),TailBeside=new(-1.5f,51),Crawler=new(8,45.5f),CrawlerHand=new(5.8f,47.6f);
            /// <summary>The front gallery the party rides on: a grating across the engine car's nose, over its tow beam.</summary>
            internal static readonly Rect Gallery=Rect.MinMaxRect(-13.4f,162.6f,-2.8f,165f);
            internal const float GalleryTop=1.6f;
            /// <summary>The trodden way, north to south: what the floor is worn along, what a route walks, what the bake must join. Seven metres east of each car as it passes.</summary>
            internal static readonly Vector2[] Way=
            {
                new(0,740),new(2,722),new(-2,704),new(3,686),new(6,668),new(2,652),new(5,636),new(7,620),new(6,606),new(1,598),Seat(0),
                new(-3,588),new(1,572),new(4,556),new(2,540),new(-1,522),new(1,506),new(4,490),new(6,474),new(3,458),new(5,442),new(7,428),new(6,416),new(1,408),Seat(1),
                new(-3,398),new(1,382),new(5,366),new(3,350),new(0,334),new(-2,318),new(1,302),new(5,286),new(7,270),new(4,254),new(1,238),new(3,222),new(6,206),new(7,190),new(6,178),new(2,166),
                // Round the gallery's south-east corner, not through it (the route's walk to the seat was cut there).
                new(-1,161.4f),Seat(2),
                Board,
            };
            /// <summary>After the ride: from where it sets the party down to Dorrow's crawler.</summary>
            internal static readonly Vector2[] Home={Tail,new(1,50),CrawlerHand};
            /// <summary>The fights on the run: id, the middle of its box, its box, and its packs (enemy, count, where it comes from). Every spawner east of the drag cable.</summary>
            internal static readonly (string id,Vector2 at,Vector2 size,(string enemy,string standIn,int count,Vector2 from)[] packs)[] Fights=
            {
                ("Terminator_Burrowers",new(1,520),new(30,24),new[]{("HeatScavenger","Glassback",1,new Vector2(-5,532)),("HeatScavenger","Glassback",1,new Vector2(12,530))}),
                ("Terminator_Gull",new(5,462),new(32,22),new[]{("SlagGull","MusterMortar",1,new Vector2(-4,450))}),
                // The two couplings fought at run north past the line's place at its reach, and everything comes at them from the east and north, never from the south: the view a
                // fight frames takes in where its enemies are, and with one coming up from 14 m south of the seat it slid south and put the dawn above the top of the frame while the
                // hands were on (focused runs 3 and 4).
                ("Terminator_Kitchens",new(1,405.5f),new(30,35),new[]{("HeatScavenger","Glassback",1,new Vector2(12,413)),("HeatScavenger","Glassback",1,new Vector2(14,401))}),
                ("Terminator_Gulls",new(1,330),new(32,22),new[]{("SlagGull","MusterMortar",1,new Vector2(-5,320)),("SlagGull","MusterMortar",1,new Vector2(14,322))}),
                ("Terminator_Spines",new(5,262),new(30,24),new[]{("HeatScavenger","Glassback",1,new Vector2(-4,274)),("HeatScavenger","Glassback",1,new Vector2(16,272)),("SlagGull","MusterMortar",1,new Vector2(15,252))}),
                // No gull at the last coupling: a lobber's spit is framed as it flies, and with one over the engine car the view drew back past the dawn while the hands were on (runs 5, 6);
                // it hunts the second spines instead, so the run's count is the same.
                ("Terminator_Engine",new(1,161.5f),new(30,35),new[]{("HeatScavenger","Glassback",1,new Vector2(12,173)),("HeatScavenger","Glassback",1,new Vector2(15,167))}),
            };
            /// <summary>The way's x at a z (the polyline's), for placing things beside it.</summary>
            internal static float WayX(float z)
            {
                for(int i=1;i<Way.Length;i++){var a=Way[i-1];var b=Way[i];if((z<=a.y&&z>=b.y)||(z>=a.y&&z<=b.y)){float t=Mathf.Abs(b.y-a.y)<.01f?0:(z-a.y)/(b.y-a.y);return Mathf.Lerp(a.x,b.x,t);}}
                return 0;
            }
            static float FromWay(Vector2 p)=>Mathf.Min(ZoneKit.FromPath(p,Way),ZoneKit.FromPath(p,Home));
            static float Smooth(float a,float b,float x)=>ZoneKit.Smooth(a,b,x);
            /// <summary>The drag lane: the strip west of the way where the hall's cars stand and run, flattened by them.</summary>
            internal static float Lane(Vector2 p)=>Smooth(-19,-17.2f,p.x)*Smooth(.4f,-1.2f,p.x)*Smooth(42,47,p.y)*Smooth(624,618,p.y);
            internal static float Height(Vector2 p)
            {
                float x=p.x,z=p.y;
                float swell=.6f*(Mathf.PerlinNoise(x*.02f+3.1f,z*.016f+7.7f)-.5f)+.22f*(Mathf.PerlinNoise(x*.09f+1.3f,z*.08f+5.2f)-.5f);
                // The dry wash: a shallow channel across the way, crossed in the opening stretch.
                swell-=1.05f*(1-Smooth(3,7.5f,Mathf.Abs(z-WashZ-3*Mathf.Sin(x*.09f))));
                swell*=1-Lane(p);
                float wobble=1.6f*(Mathf.PerlinNoise(z*.05f,3.3f)-.5f),wander=4f*(Mathf.PerlinNoise(z*.03f,9.1f)-.5f);
                // The camera's side (west, south) stays low: a bank of 2.7 m with its rocks is the stop. The far side (east, north) is the band's broken edge, a scarp.
                float west=2.7f*Smooth(-29,-32,x+wobble),south=2.7f*Smooth(35,31.5f,z+wobble*.5f);
                float east=6.5f*Smooth(27,32,x+wander)+3f*Smooth(34,42,x),north=6.5f*Smooth(767,775,z+wobble*2);
                return swell+west+south+east+north;
            }
            /// <summary>Dark regolith in dusk blue, worn pale along the way, darker on the drag lane; the frost flats paler.</summary>
            static Color Tone(Vector2 p)
            {
                float mottle=.88f+.24f*Mathf.PerlinNoise(p.x*.11f+2,p.y*.11f+8),broad=Mathf.PerlinNoise(p.x*.025f+5,p.y*.02f+1);
                var c=Color.Lerp(new Color(.55f,.56f,.6f),new Color(.64f,.6f,.56f),broad);
                c=Color.Lerp(c,new Color(.72f,.68f,.64f),(1-Smooth(1.3f,3.4f,FromWay(p)))*.42f);
                c=Color.Lerp(c,new Color(.42f,.4f,.42f),Lane(p)*.45f);
                c=Color.Lerp(c,new Color(.66f,.7f,.8f),Frost(p)*.35f);
                return new Color(c.r*mottle,c.g*mottle,c.b*mottle,1);
            }
            /// <summary>Where rime lies (0 to 1): drifts in the opening stretch's hollows and the frost flat. Burnt off as the line comes (the Dawn shader's frost kind).</summary>
            internal static float Frost(Vector2 p)
            {
                float flat=Smooth(452,458,p.y)*Smooth(488,482,p.y),lesson=Smooth(636,644,p.y)*Smooth(736,728,p.y)*.6f;
                float n=Mathf.PerlinNoise(p.x*.12f+11,p.y*.12f+4)*.7f+Mathf.PerlinNoise(p.x*.4f+3,p.y*.4f+9)*.3f;
                return Mathf.Clamp01(Mathf.Max(flat,lesson)*Smooth(.52f,.7f,n)*(1-Lane(p)))*(1-Smooth(26,30,Mathf.Abs(p.x)));
            }

            /// <summary>LookDev's stills: the arrival with the line at its reach, the lesson as the line takes the burrower, each coupling with the line in frame behind it, the stretches
            /// between, the gallery, the ride's end, three wide views. Each view's dawn gap is what an editor still poses the line at.</summary>
            internal static readonly (string name,Vector2 at,float gap,float zoom)[] Stills=
            {
                ("arrival",Arrival,11,1),("lesson",new(-3,707),4,1),("buggy",new(-5,684),3,1),("wash",new(2,654),9,1),("schoolroom",new(3,602),9,1),("coupling-1",Seat(0),9,1),
                ("spines-1",new(2,532),9,1),("frost",new(4,472),5,1),("coupling-2",Seat(1),9,1),("gulls",new(1,332),9,1),("spines-2",new(5,264),9,1),("coupling-3",Seat(2),9,1),
                ("gallery",Board,9,1),("ride-end",Tail,80,1),("crawler",CrawlerHand,80,1),
                ("overview-start",new(0,700),11,3.6f),("overview-middle",new(0,420),11,3.6f),("overview-end",new(-2,160),11,3f),
                // Where the November 20 capture's stills stood (s56 009 and 040), the line about as far behind as it was.
                ("s56-009",new(2.7f,687),6,1),("s56-040",new(3.1f,351),6,1),
            };
            internal static LookDev.View[] Views=>Stills.Select(s=>new LookDev.View(s.name,new Vector3(s.at.x,Height(s.at),s.at.y),false,s.zoom)).ToArray();

            // ---------------------------------------------------------------- materials
            static Material Floor,Hills,SpineRock,EngineLamp,Light,Rime,RutRime,Veil;static Texture2D StrataMap;
            static Material DawnMaterial(string name,int kind,BlendMode src,BlendMode dst)
            {
                string path="Assets/_Project/Resources/WorldMaterials/"+name+".mat";var shader=Shader.Find("Lattice/Dawn");
                if(shader==null)throw new InvalidOperationException("TERMINATOR: the Lattice/Dawn shader isn't compiled (Resources/Effects/Dawn.shader)");
                var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(m==null){m=new Material(shader){name=name};AssetDatabase.CreateAsset(m,path);}else m.shader=shader;
                m.SetFloat("_Kind",kind);m.SetFloat("_SrcBlend",(float)src);m.SetFloat("_DstBlend",(float)dst);m.renderQueue=(int)RenderQueue.Transparent-15;EditorUtility.SetDirty(m);return m;
            }
            static void Materials()
            {
                Crawl.Materials();
                // The band's own regolith: frost-heaved plates two metres across, their cracks dark and rimmed pale with rime, grit between. The Crawl's plain (seen moving, from a
                // deck 7.5 m up) has thin cracks; under a dusk key a walked floor of thin cracks read as one plain sheet (look pass 05).
                float H(int a,int b,int seed){uint h=(uint)(a*374761393+b*668265263+seed*1442695041);h=(h^(h>>13))*1274126177;h^=h>>16;return (h&0xffff)/65535f;}
                var plain=ZoneKit.Texture("Terminator/terminator-regolith",1024,(u,v)=>
                {
                    const int P=9;float wob=.2f*(ZoneKit.Cloud(u,v,12,31)-.5f);float cx=u*P+wob,cy=v*P-wob;int ix=Mathf.FloorToInt(cx),iy=Mathf.FloorToInt(cy);float d1=9,d2=9;int own=0;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int gx=((ix+dx)%P+P)%P,gy=((iy+dy)%P+P)%P;float fx=ix+dx+.15f+.7f*H(gx,gy,11),fy=iy+dy+.15f+.7f*H(gx,gy,12);
                        float d=Mathf.Sqrt((cx-fx)*(cx-fx)+(cy-fy)*(cy-fy));if(d<d1){d2=d1;d1=d;own=gx*31+gy;}else if(d<d2)d2=d;
                    }
                    float tone=.56f+.14f*H(own,7,3)+.1f*(ZoneKit.Cloud(u,v,14,6)-.5f)+.1f*(ZoneKit.Tile(u*300,v*300,300,7)-.5f),edge=d2-d1;
                    if(edge<.035f)tone-=.34f;else if(edge<.07f)tone+=.12f;
                    if(ZoneKit.Tile(u*420,v*420,420,4)>.82f)tone-=.14f;if(ZoneKit.Tile(u*180,v*180,180,21)>.9f)tone+=.12f;
                    return new Color(tone,tone*.97f,tone*1.02f,1);
                });
                Floor=ZoneKit.Toon("terminator-floor",plain,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.4f,.46f,.6f));m.SetTextureScale("_BaseMap",new Vector2(.45f,.45f));});
                // Strata of the band's edge: dark basalt in level beds, a pale ash bed now and then, frost in the cracks.
                var strata=ZoneKit.Texture("Terminator/terminator-strata",512,(u,v)=>
                {
                    float bed=v*9+.35f*Mathf.Sin(u*Mathf.PI*4)+.6f*(ZoneKit.Cloud(u,v,6,2)-.5f);int k=Mathf.FloorToInt(bed);float across=bed-k;
                    float tone=.42f+.18f*ZoneKit.Tile(k*3.1f,.5f,9,7)+.08f*(ZoneKit.Tile(u*128,v*128,128,3)-.5f);
                    if(across<.06f)tone-=.18f;if(k%4==1)tone+=.12f;if(ZoneKit.Tile(u*256,v*256,256,5)>.93f)tone+=.18f;
                    return new Color(tone,tone*.97f,tone*1.02f,1);
                });
                Hills=ZoneKit.Toon("terminator-hills",strata,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.38f,.44f,.58f));});
                SpineRock=ZoneKit.Toon("terminator-spine",strata,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.38f,.44f,.58f));m.SetTextureScale("_BaseMap",new Vector2(1,2));});
                StrataMap=strata;
                // The engine car's lamps are dark until the third coupling wakes it (TreadRide lights them by property block).
                EngineLamp=ZoneKit.Toon("terminator-engine-lamp",null,m=>{m.SetColor("_BaseColor",new Color(.3f,.28f,.24f));m.SetColor("_EmissionColor",new Color(.05f,.04f,.02f));});
                // The light added over the ground, not multiplied into it: multiplied, a dusk floor a few hundredths bright stayed dusk, and the first stills of the run showed
                // no sunrise at all behind the line (look pass 04). Pale gold-white, under a marking's orange.
                // Added at .9 over ground the near sun already lights at 1.15, the light behind the line was white past the top of the range in the player and bloomed into one band
                // with the edge (the November 20 capture, s56 009 to 040): at this it is lit ground, gold, its plates and rocks still read, and only the edge's band is over the
                // bloom's threshold.
                Light=DawnMaterial("terminator-dawn-light",0,BlendMode.One,BlendMode.One);
                Light.SetColor("_Light",new Color(.58f,.46f,.28f));Light.SetColor("_Edge",new Color(1.16f,.86f,.48f));Light.SetFloat("_EdgeWidth",.4f);Light.SetFloat("_Shimmer",.5f);
                Rime=DawnMaterial("terminator-dawn-frost",1,BlendMode.SrcAlpha,BlendMode.OneMinusSrcAlpha);
                // Rime is a dusting in the hollows: at the shader's own strength it lay over the lesson stretch as a sheet of fog.
                Rime.SetColor("_Frost",new Color(.6f,.7f,.88f,.3f));
                // Rime in the shade lies thicker than the hollows' dusting.
                RutRime=DawnMaterial("terminator-dawn-frost-ruts",1,BlendMode.SrcAlpha,BlendMode.OneMinusSrcAlpha);RutRime.SetColor("_Frost",new Color(.66f,.76f,.92f,.42f));
                Veil=DawnMaterial("terminator-dawn-veil",2,BlendMode.One,BlendMode.One);
                AssetDatabase.SaveAssets();
            }

            // ---------------------------------------------------------------- the zone
            static bool greybox;
            static readonly List<DawnLine.Caster> casters=new();
            internal static void Build()
            {
                greybox=DevArgs.Has("-terminator-greybox");casters.Clear();
                Materials();
                var kit=ZoneKit.Ground(TerminatorZone,Height,CliffTop);
                var scene=WorldBuilder.Begin(kit.zone);
                var key=GameObject.Find("KeyLight");if(key!=null)key.transform.rotation=Quaternion.Euler(SunPitch,180,0);
                var land=kit.Terrain("Terminator band",-44,44,22,800,.5f,Floor,Hills,Tone,new Color(.62f,.6f,.66f),new Color(.86f,.84f,.86f));
                foreach(var p in new[]{Arrival,Cart,Seat(0),Seat(1),Seat(2),Checkpoint(0),Checkpoint(1),Checkpoint(2),Board,Tail,TailBeside,CrawlerHand})
                    if(Height(p)>CliffTop-.3f||Mathf.Abs(Height(p+Vector2.right*.6f)-Height(p))>.2f||Mathf.Abs(Height(p+Vector2.up*.6f)-Height(p))>.2f)throw new InvalidOperationException("TERMINATOR_PLAN "+p+" is not on open ground");

                // ---- where the party stands: the arrival and each coupling's checkpoint, the ride's end
                var spawns=new List<Transform>{WorldBuilder.Spawn("Arrival",kit.On(Arrival.x,Arrival.y)).transform};
                for(int i=0;i<Cars.Length;i++)spawns.Add(WorldBuilder.Spawn(Cars[i].spawn,kit.On(Checkpoint(i).x,Checkpoint(i).y)).transform);
                WorldBuilder.Spawn("Tail",kit.On(Tail.x,Tail.y));
                foreach(var s in spawns)s.rotation=Quaternion.Euler(0,180,0);

                // ---- the run's start: the crawler that set the party down, its tool cart
                Runabout(kit,"Dorrow's crawler, at the run's start",new Vector2(-7.5f,747),200);
                var cart=ToolCart(kit,"Crawler's tool cart",Cart,160);var bay=cart.AddComponent<RepairBay>();bay.free=true;bay.saveSpawn="Arrival";bay.prompt="Crawler's tool cart — repair and save";bay.range=2.8f;

                // ---- tread-hall four: three cars in file on the drag lane, their couplings, the drag between them
                var cars=new GameObject[Cars.Length];var couplings=new TreadCoupling[Cars.Length];
                for(int i=0;i<Cars.Length;i++){cars[i]=Car(kit,i);couplings[i]=Coupling(kit,i,cars[i]);}
                var drag=Drag(kit);
                var ride=Ride(kit,cars,drag);

                // ---- the way home, after the ride
                var crawler=Runabout(kit,"Dorrow's crawler, waiting",Crawler,250);
                kit.Way(crawler,"Climb aboard — The Meridian Crawl",CrawlZone,"Gap","home.rideDone","Tread-hall four is still out there: re-mate its three couplings and ride it in",3.4f,new Vector2(Crawler.x-.5f,Crawler.y-4.4f));

                // ---- the fights
                EncounterVolume gulls=null;
                foreach(var (id,at,size,packs) in Fights){var fight=Fight(kit,id,at,size,packs);if(id=="Terminator_Gull")gulls=fight;}
                var sight=new GameObject("First sight of a slag gull",typeof(FirstSight)).GetComponent<FirstSight>();sight.situation="gull";sight.encounter=gulls;
                // ---- the lesson: a burrower hunting along the light, which the light takes; an old buggy whose paint blisters; frost that flashes off
                Lessons(kit);
                Spines(kit);
                if(!greybox){Dress(kit);TheEnd(kit);Road(kit);ShadeFrost(kit);}
                var dawn=Dawn(kit,spawns,ride,crawler.transform);

                // ---- limits: the banks and the scarp are the stop; a box on each keeps a body from finding a way up one
                WorldBuilder.Boundary("West limit",new Vector3(-35,6,410),new Vector3(2,24,780));WorldBuilder.Boundary("East limit",new Vector3(36,6,410),new Vector3(2,24,780));
                WorldBuilder.Boundary("North limit",new Vector3(0,6,779),new Vector3(74,24,2));WorldBuilder.Boundary("South limit",new Vector3(0,6,29),new Vector3(74,24,2));

                // ---- navigation: every place a player must stand
                var reach=new List<Vector2>{Arrival,Cart+new Vector2(-1.6f,-1.2f),Lesson+new Vector2(5,-8),new(2,WashZ),Board,Tail,CrawlerHand};
                for(int i=0;i<Cars.Length;i++){reach.Add(Seat(i));reach.Add(Checkpoint(i));}
                foreach(var f in Fights)reach.Add(f.at);
                kit.BakeNavigation(new Bounds(new Vector3(0,3,402),new Vector3(70,30,760)),Arrival,reach);
                if(!greybox)Ground(kit);
                TheLight(land);
                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("TERMINATOR_COORDS arrival="+Arrival+" seats="+string.Join(" ",Enumerable.Range(0,3).Select(i=>Seat(i).ToString()))+" checkpoints="+string.Join(" ",Enumerable.Range(0,3).Select(i=>Checkpoint(i).ToString()))
                    +" board="+Board+" tail="+Tail+" crawler="+CrawlerHand+" casters="+casters.Count+(greybox?" GREYBOX":""));
                Debug.Log("TERMINATOR_OK");
            }

            // ---------------------------------------------------------------- helpers
            /// <summary>A built shape about its own origin, stood at a point and turned: what moves, swings or turns, and anything a prompt sits on.</summary>
            static GameObject Make(ZoneKit kit,string name,ZoneKit.Shape shape,Vector3 at,Quaternion turn,Transform parent,params Material[] materials)
            {
                var go=kit.Built(name,shape,materials);go.transform.SetPositionAndRotation(at,turn);if(parent!=null)go.transform.SetParent(parent,true);return go;
            }
            /// <summary>A shadow the low sun throws along the line's way: shade the burrowers flee (combat reads it through <see cref="DawnLine.Shade"/>).</summary>
            static void Casts(Bounds box)=>casters.Add(new DawnLine.Caster{min=new Vector2(box.min.x,box.min.z),max=new Vector2(box.max.x,box.max.z),height=box.size.y});
            /// <summary>Something the light warms as it reaches it: from its north edge, over its depth.</summary>
            static DawnTouched Warms(GameObject go,Color tint)
            {
                var box=ZoneKit.Drawn(go);var t=go.AddComponent<DawnTouched>();t.kind=DawnTouched.Kind.Heat;t.at=-box.max.z;t.span=Mathf.Max(1,box.size.z);t.tint=tint;return t;
            }
            static EncounterVolume Fight(ZoneKit kit,string id,Vector2 at,Vector2 size,(string enemy,string standIn,int count,Vector2 from)[] packs)
            {
                var encounter=WorldBuilder.Encounter(id,kit.On(at.x,at.y,1.5f),new Vector3(size.x,7,size.y),Foe(packs[0].enemy,packs[0].standIn),packs[0].count);
                var spawners=new List<Spawner>{encounter.spawners[0]};
                for(int i=1;i<packs.Length;i++)spawners.Add(ZoneKit.Spawn(encounter,Foe(packs[i].enemy,packs[i].standIn),packs[i].count,Vector3.zero,2));
                for(int i=0;i<packs.Length;i++)
                {
                    var from=packs[i].from;if(from.x<-7.5f&&from.y>40&&from.y<620)throw new InvalidOperationException("TERMINATOR: "+id+"'s spawner at "+from+" is west of the drag cable");
                    spawners[i].transform.SetPositionAndRotation(kit.On(from.x,from.y),Quaternion.Euler(0,from.x<at.x?90:270,0));spawners[i].faceAsPlaced=true;spawners[i].radius=0;
                }
                encounter.spawners=spawners.ToArray();encounter.requiredFlag="";return encounter;
            }

            // ---------------------------------------------------------------- tread-hall four
            /// <summary>One car: the generated tread-hall (CrawlTread), its treads along the way, on the drag lane; its front gallery (the engine car's) or its bell frame and chimneys;
            /// a solid box the bake and bodies meet; warmed by the light (not the engine car, which outruns it); its long shadow is shade.</summary>
            static GameObject Car(ZoneKit kit,int i)
            {
                var (name,_,_,at,tint)=Cars[i];var root=new GameObject(name);root.transform.position=new Vector3(at.x,0,at.y);
                var hall=Crawl.CrawlTread(kit,name+", drawn",new Vector3(at.x,0,at.y),CarLength,0,tint);
                hall.transform.RotateAround(new Vector3(at.x,0,at.y),Vector3.up,90);var b=ZoneKit.Drawn(hall);hall.transform.position+=new Vector3(at.x-b.center.x,-b.min.y,at.y-b.center.z);
                hall.transform.SetParent(root.transform,true);
                // The drawn hall is not static (kit.Prop leaves an unsolid piece so), so nothing of a car is batched and the ride can move it; its box is the bake's and a body's.
                if(i<2)UnityEngine.Object.DestroyImmediate(hall.GetComponent<PropTint>());
                b=ZoneKit.Drawn(hall);Debug.Log($"TERMINATOR_CAR {name} size={b.size}");
                ZoneKit.Block(root,new Vector3(b.center.x,b.size.y*.5f,b.center.z),new Vector3(b.size.x-.6f,b.size.y,b.size.z-.4f),0,name+" body");
                Casts(b);
                var s=new ZoneKit.Shape();Color iron=new(.3f,.3f,.33f),brass=new(.92f,.72f,.42f);float roof=b.size.y-.4f;
                if(i==0)
                {
                    // The schoolroom car: a bell frame on its nose and its number painted four times the height of a child.
                    var f=new Vector3(0,roof-3.4f,-CarLength*.5f+.8f);s.Slot(0).Post(f+new Vector3(-.9f,0,0),2.6f,.08f,.07f,6,iron,iron).Post(f+new Vector3(.9f,0,0),2.6f,.08f,.07f,6,iron,iron).Beam(f+new Vector3(-1,2.6f,0),f+new Vector3(1,2.6f,0),.1f,.1f,iron);
                    s.Slot(1).Tube(new[]{f+new Vector3(0,2.45f,0),f+new Vector3(0,1.75f,0)},new[]{.2f,.42f},14,brass,new Color(1,.8f,.5f));
                    s.Slot(2).Box(new Vector3(4.6f,3.2f,-CarLength*.5f-.05f),new Vector3(1.6f,2.2f,.04f),Quaternion.identity,new Color(.92f,.9f,.82f));
                }
                else if(i==1)
                {
                    // The kitchens car: nine chimneys along its roof, one still smoking (Hesk's pot).
                    for(int k=0;k<9;k++){var c=new Vector3(-4.5f+(k%3)*4.5f,roof-1.2f,-4.2f+(k/3)*4.2f);s.Slot(0).Tube(new[]{c,c+Vector3.up*(2.2f+.3f*(k%2))},new[]{.32f,.26f},8,new Color(.36f,.32f,.3f),new Color(.44f,.4f,.36f));}
                }
                else
                {
                    // The engine car: its stack, and the front gallery the party rides out on, railed, with steps up its east end.
                    s.Slot(0).Tube(new[]{new Vector3(3.5f,roof-1,2),new Vector3(3.5f,roof+3.2f,2)},new[]{.7f,.55f},12,new Color(.3f,.28f,.28f),new Color(.4f,.36f,.34f));
                    var g0=new Vector3(Gallery.xMin-at.x,GalleryTop,Gallery.yMin-at.y);var g1=new Vector3(Gallery.xMax-at.x,GalleryTop,Gallery.yMax-at.y);
                    s.Slot(0).Box((g0+g1)*.5f-Vector3.up*.08f,new Vector3(g1.x-g0.x,.16f,g1.z-g0.z),Quaternion.identity,new Color(.42f,.42f,.45f),new Color(.56f,.55f,.56f));
                    for(float x=g0.x;x<=g1.x+.01f;x+=1.5f){s.Slot(0).Post(new Vector3(x,0,g0.z+.1f),GalleryTop,.07f,.06f,6,iron,iron).Post(new Vector3(x,GalleryTop,g0.z+.1f),1.05f,.05f,.04f,6,iron,iron);}
                    s.Slot(0).Beam(new Vector3(g0.x,GalleryTop+1,g0.z+.1f),new Vector3(g1.x,GalleryTop+1,g0.z+.1f),.07f,.07f,new Color(.46f,.44f,.44f));
                    for(int k=0;k<4;k++)s.Slot(0).Box(new Vector3(g1.x+.4f,GalleryTop*(k+.5f)/4,g0.z+.6f+k*.35f),new Vector3(.8f,.08f,.35f),Quaternion.identity,new Color(.44f,.44f,.47f));
                }
                if(s.Vertices>0)Make(kit,name+" fittings",s,new Vector3(at.x,0,at.y),Quaternion.identity,root.transform,Crawl.Iron,Crawl.Brass,Crawl.Cloth).name=name+" fittings";
                if(i==2)
                {
                    // The gallery stands on the bake: nobody walks under it, and a body set down on it stands.
                    ZoneKit.Block(root,new Vector3(Gallery.center.x,GalleryTop*.5f,Gallery.center.y),new Vector3(Gallery.width,GalleryTop,Gallery.height),0,"Engine car gallery");
                }
                if(i<2)Warms(hall,tint);
                return root;
            }
            /// <summary>A coupling at a car's front: the car's tow beam and its jaw, the drag's eye that comes round into it, the pin over the jaw, the jack's frame and wheel beside
            /// it, chocks at the treads, lashings slack and taut, a lamp on a gantry; the seat where a hero's hands go (<see cref="TreadCoupling"/>). Heavy town machinery, lashed and chocked.</summary>
            static TreadCoupling Coupling(ZoneKit kit,int i,GameObject car)
            {
                var (name,flag,spawn,at,_)=Cars[i];var socket=Socket(i);var seat=Seat(i);float front=Front(i).y,y=kit.Y(socket.x,socket.y);
                var holder=new GameObject("Coupling "+(i+1)+", "+name.Replace("The ","the "));holder.transform.SetParent(car.transform,true);
                Color iron=new(.3f,.3f,.33f),dark=new(.22f,.22f,.24f),brass=new(.92f,.72f,.42f),hazard=new(.86f,.7f,.32f);
                // The tow beam out of the car's nose to its jaw, and the jaw.
                var s=new ZoneKit.Shape();var jaw=new Vector3(socket.x,y+1,socket.y);
                s.Slot(0).Beam(new Vector3(socket.x,y+1,front+.5f),jaw+new Vector3(0,0,.5f),.55f,.6f,iron,new Color(.42f,.42f,.44f));
                s.Slot(0).Box(jaw,new Vector3(1.1f,1.0f,1.0f),Quaternion.identity,dark,iron).Box(jaw+new Vector3(0,0,-.35f),new Vector3(.9f,.32f,.34f),Quaternion.identity,new Color(.12f,.12f,.13f));
                for(int k=0;k<4;k++)s.Slot(1).Disc(jaw+new Vector3(k<2?-.56f:.56f,k%2==0?.3f:-.3f,.2f),k<2?Vector3.left:Vector3.right,.07f,8,brass);
                // Hazard bands on the beam: the town's yellow, never a marking's orange.
                for(int k=0;k<3;k++)s.Slot(2).Box(new Vector3(socket.x,y+1,front-.4f-k*.9f),new Vector3(.58f,.63f,.18f),Quaternion.identity,hazard);
                // The jack: a frame of two posts and a beam on the jaw's east side; its wheel faces the seat.
                var jack=new Vector3(socket.x+1.45f,y,socket.y);
                s.Slot(0).Post(jack+new Vector3(0,0,-.55f),1.7f,.09f,.08f,6,iron,iron).Post(jack+new Vector3(0,0,.55f),1.7f,.09f,.08f,6,iron,iron).Beam(jack+new Vector3(0,1.7f,-.6f),jack+new Vector3(0,1.7f,.6f),.12f,.12f,iron);
                s.Slot(0).Beam(jack+new Vector3(0,1.1f,0),jaw+new Vector3(.5f,.1f,0),.14f,.14f,dark);
                // A gantry over it all with its lamp: what marks the coupling from up the way.
                foreach(float dz in new[]{-1.6f,1.6f})s.Slot(0).Post(new Vector3(socket.x+2.2f,y,socket.y+dz),3.6f,.12f,.1f,6,iron,iron).Post(new Vector3(socket.x-1.4f,y,socket.y+dz),3.6f,.12f,.1f,6,iron,iron);
                s.Slot(0).Beam(new Vector3(socket.x-1.5f,y+3.6f,socket.y),new Vector3(socket.x+2.3f,y+3.6f,socket.y),.18f,.22f,iron).Beam(new Vector3(socket.x+2.2f,y+3.6f,socket.y-1.7f),new Vector3(socket.x+2.2f,y+3.6f,socket.y+1.7f),.14f,.14f,iron);
                s.Slot(3).Rope(new Vector3(socket.x,y+3.5f,socket.y),jaw+new Vector3(0,.7f,0),.05f,.05f,new Color(.3f,.3f,.32f),5,4);
                var body=kit.Built(holder.name+" machinery",s,Crawl.Iron,Crawl.Brass,Crawl.Cloth,Crawl.Iron);body.transform.SetParent(holder.transform,true);
                ZoneKit.Block(body,new Vector3(socket.x-.15f,y+.7f,(front+socket.y)*.5f),new Vector3(1.4f,1.4f,front-socket.y+.6f),0,"Tow beam");
                ZoneKit.Block(body,jack+new Vector3(0,.85f,0),new Vector3(.4f,1.7f,1.3f),0,"Jack frame");
                foreach(float dz in new[]{-1.6f,1.6f})foreach(float dx in new[]{2.2f,-1.4f})ZoneKit.Block(body,new Vector3(socket.x+dx,y+1.8f,socket.y+dz),new Vector3(.3f,3.6f,.3f),0,"Gantry leg");
                // The drag's eye, as it sits mated in the jaw (the coupling slides it out to where it lies parted): a forged ring on a shank, the drag's end.
                var e=new ZoneKit.Shape();
                e.Slot(0).Tube(Enumerable.Range(0,17).Select(k=>new Vector3(Mathf.Cos(k*Mathf.PI/8)*.32f,0,-.35f+Mathf.Sin(k*Mathf.PI/8)*.32f)).ToArray(),Enumerable.Repeat(.1f,17).ToArray(),6,iron,new Color(.44f,.42f,.42f),1,1,false);
                e.Slot(0).Beam(new Vector3(0,0,-.65f),new Vector3(0,0,-1.9f),.26f,.26f,iron,new Color(.42f,.4f,.4f));
                var eye=Make(kit,holder.name+" drag eye",e,jaw+new Vector3(0,-.2f,-.1f),Quaternion.identity,holder.transform,Crawl.Iron);
                // The pin over the jaw (mated: through it), the jack's wheel (turned about its own forward, which faces the seat), the chocks at the treads (driven in along their forward).
                var pn=new ZoneKit.Shape();pn.Slot(0).Tube(new[]{new Vector3(0,-.55f,0),new Vector3(0,.75f,0)},new[]{.13f,.13f},10,brass,new Color(1,.82f,.52f)).Slot(0).Disc(new Vector3(0,.76f,0),Vector3.up,.2f,10,brass);
                var pin=Make(kit,holder.name+" pin",pn,jaw+new Vector3(0,.1f,-.35f),Quaternion.identity,holder.transform,Crawl.Brass);
                var w=new ZoneKit.Shape();w.Slot(0).Tube(Enumerable.Range(0,25).Select(k=>new Vector3(Mathf.Cos(k*Mathf.PI/12)*.55f,Mathf.Sin(k*Mathf.PI/12)*.55f,0)).ToArray(),Enumerable.Repeat(.05f,25).ToArray(),6,brass,brass,1,1,false);
                for(int k=0;k<6;k++){float a=k*Mathf.PI/3;w.Slot(0).Beam(Vector3.zero,new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.55f,.04f,.04f,brass);}
                w.Slot(1).Tube(new[]{new Vector3(.55f,0,0),new Vector3(.55f,0,.25f)},new[]{.04f,.04f},6,new Color(.5f,.36f,.24f),new Color(.5f,.36f,.24f));
                var wheel=Make(kit,holder.name+" jack wheel",w,jack+new Vector3(.2f,1.15f,0),Quaternion.LookRotation(Vector3.right),holder.transform,Crawl.Brass,Crawl.Timber);
                var chocks=new List<Transform>();
                foreach(float dx in new[]{-5.2f,5.2f})
                {
                    var c=new ZoneKit.Shape();c.Slot(0).Box(new Vector3(0,.22f,0),new Vector3(.7f,.44f,.6f),Quaternion.Euler(-18,0,0),new Color(.58f,.46f,.32f),new Color(.68f,.56f,.4f)).Slot(1).Box(new Vector3(0,.46f,-.05f),new Vector3(.72f,.06f,.2f),Quaternion.Euler(-18,0,0),hazard);
                    chocks.Add(Make(kit,holder.name+" chock",c,new Vector3(at.x+dx,y,front-.45f),Quaternion.identity,holder.transform,Crawl.Timber,Crawl.Cloth).transform);
                }
                // Lashings from the gantry's legs to the jaw: hanging slack until it is mated, hauled taut after.
                GameObject Lash(string what,float sag)
                {
                    var l=new ZoneKit.Shape();foreach(float dz in new[]{-1.6f,1.6f})l.Slot(0).Rope(new Vector3(socket.x+2.2f,y+2.4f,socket.y+dz),jaw+new Vector3(.4f,.3f,0),sag,.035f,new Color(.74f,.64f,.46f),5,8);
                    var go=kit.Built(holder.name+" "+what,l,Crawl.Rope);go.transform.SetParent(holder.transform,true);return go;
                }
                var slack=Lash("lashings slack",.7f);var taut=Lash("lashings taut",.04f);taut.SetActive(false);
                var lamp=new ZoneKit.Shape();lamp.Slot(0).Lump(Vector3.zero,new Vector3(.2f,.26f,.2f),i,.03f,Color.white,Color.white,8,5).Slot(1).Box(new Vector3(0,.3f,0),new Vector3(.36f,.06f,.36f),Quaternion.identity,brass);
                var lampBody=Make(kit,holder.name+" lamp",lamp,new Vector3(socket.x+2.2f,y+3.95f,socket.y),Quaternion.identity,body.transform,Crawl.Lamp,Crawl.Brass);
                var glow=new GameObject("Coupling lamp light",typeof(Light)).GetComponent<Light>();glow.type=LightType.Point;glow.range=7;glow.intensity=1.4f;glow.color=new Color(1,.7f,.35f);glow.shadows=LightShadows.None;
                glow.transform.position=new Vector3(socket.x+2.2f,y+3.6f,socket.y-.4f);glow.transform.SetParent(holder.transform,true);
                // The seat: the floor a hero stands on at the jack, which is where the prompt is.
                var seatGo=new GameObject("Re-mate the coupling ("+(i+1)+"/3)");seatGo.transform.position=kit.On(seat.x,seat.y);seatGo.transform.SetParent(holder.transform,true);
                var c3=seatGo.AddComponent<TreadCoupling>();c3.index=i+1;c3.flag=flag;c3.requiredFlag=i==0?"":Cars[i-1].flag;c3.spawn=spawn;c3.seatRadius=1.8f;c3.prompt="Re-mate the coupling";c3.range=1.8f;
                c3.drawbar=eye.transform;c3.parted=new Vector3(.9f,0,-1.1f);c3.slew=-14;c3.pin=pin.transform;c3.wheel=wheel.transform;c3.chocks=chocks.ToArray();c3.slack=slack;c3.taut=taut;
                c3.lamp=new[]{lampBody.GetComponent<Renderer>()};c3.lampLight=glow;
                // Amber waiting, pale once mated, each a little over the bloom's threshold: TreadCoupling's own 1.5 to 1.7 white over the lamp's pale glass was a white blob with no body
                // on the engine car's gantry (the November 20 capture, s56 060).
                c3.waiting=new Color(.95f,.6f,.2f);c3.mated=new Color(.82f,.86f,.8f);
                casters.Add(new DawnLine.Caster{min=new Vector2(socket.x-1.5f,socket.y-1.7f),max=new Vector2(socket.x+2.3f,socket.y+1.7f),height=1.4f});
                return c3;
            }
            /// <summary>The drag between the cars: a cable as thick as a man's leg on iron sleds every dozen metres, from each car's tail back to the next car's jaw (the third coupling is
            /// the engine car's own: its drive's dog clutch, at its nose). A low box under it: a cable lying there is stepped round, never through.</summary>
            static GameObject Drag(ZoneKit kit)
            {
                var root=new GameObject("The drag");var s=new ZoneKit.Shape();Color iron=new(.28f,.28f,.3f),cable=new(.24f,.22f,.22f);
                foreach(var (from,to) in new[]{(Rear(2).y,Socket(1).y-1.9f),(Rear(1).y,Socket(0).y-1.9f)})
                {
                    float x=Cars[0].at.x;var path=new List<Vector3>();var radii=new List<float>();
                    for(float z=from;z<=to+.01f;z+=4){float sag=Mathf.Repeat(z-from,12)<1.5f?.55f:.36f;path.Add(new Vector3(x+.15f*Mathf.Sin(z*.07f),sag,z));radii.Add(.17f);}
                    path.Add(new Vector3(x,.55f,to));radii.Add(.17f);
                    s.Slot(0).Tube(path,radii,8,cable,new Color(.32f,.3f,.28f),1,.6f,false);
                    for(float z=from+1;z<to-1;z+=12)
                    {
                        s.Slot(1).Box(new Vector3(x,.22f,z),new Vector3(1.3f,.26f,1.1f),Quaternion.identity,iron,new Color(.4f,.4f,.42f));
                        foreach(int side in new[]{-1,1})s.Slot(1).Beam(new Vector3(x+side*.55f,.06f,z-.8f),new Vector3(x+side*.55f,.06f,z+.8f),.1f,.08f,new Color(.5f,.46f,.4f));
                    }
                    var cableBody=kit.Built("Drag "+Mathf.RoundToInt(from)+" to "+Mathf.RoundToInt(to),s,Crawl.Iron,Crawl.Iron);cableBody.transform.SetParent(root.transform,true);
                    ZoneKit.BlockAlong(cableBody,new Vector3(x,.3f,from),new Vector3(x,.3f,to),1.2f,.6f,"Drag cable");
                    s=new ZoneKit.Shape();
                }
                foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.GetComponent<Renderer>()!=null)GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
                return root;
            }
            /// <summary>The ride out: the prompt at the engine car's gallery steps, the car, the train behind it (the other cars and the drag), where the party rides and where it is set
            /// down. Input is the car's; standing still is all it asks.</summary>
            static TreadRide Ride(ZoneKit kit,GameObject[] cars,GameObject drag)
            {
                var engine=cars[2];var at=new GameObject("Ride the tread-hall out");at.transform.position=kit.On(Board.x,Board.y);at.transform.SetParent(engine.transform,true);
                var ride=at.AddComponent<TreadRide>();ride.prompt="Ride the tread-hall out";ride.range=2.6f;ride.requiredFlag=Cars[2].flag;ride.flag="home.rideDone";ride.spawn="Tail";
                ride.car=engine.transform;ride.train=new[]{cars[0].transform,cars[1].transform,drag.transform};
                Transform Stand(string name,Vector3 p){var t=new GameObject(name).transform;t.position=p;t.SetParent(engine.transform,true);return t;}
                ride.deck=Stand("Where the lead rides",new Vector3(-4.6f,GalleryTop,163.8f));ride.deckBeside=Stand("Where the partner rides",new Vector3(-7f,GalleryTop,163.8f));
                ride.setDown=kit.On(Tail.x,Tail.y,.05f);ride.setDownBeside=kit.On(TailBeside.x,TailBeside.y,.05f);
                ride.way=Vector3.back;ride.distance=RideDistance;ride.top=9;ride.gather=3;
                // What wakes: the lamps along the gallery's rail, the stack's smoke, a headlamp down the way.
                var lamps=new List<Renderer>();
                for(int k=0;k<4;k++)
                {
                    var l=new ZoneKit.Shape();l.Slot(0).Lump(Vector3.zero,new Vector3(.16f,.2f,.16f),k,.03f,Color.white,Color.white,8,5);
                    // On the gallery's rail, under the fittings that hold them up (measured alone, a lamp floats).
                    var fittings=engine.transform.Find("The engine car fittings");
                    lamps.Add(Make(kit,"Engine car lamp "+(k+1),l,new Vector3(Gallery.xMin+1+k*2.9f,GalleryTop+1.25f,Gallery.yMin+.1f),Quaternion.identity,fittings!=null?fittings:engine.transform,EngineLamp).GetComponent<Renderer>());
                }
                ride.lamps=lamps.ToArray();ride.lampsOn=new Color(1.0f,.64f,.26f);
                var stack=new GameObject("Engine car smoke",typeof(ParticleSystem));stack.transform.position=new Vector3(Cars[2].at.x+3.5f,ZoneKit.Drawn(engine).max.y+.2f,Cars[2].at.y+2);stack.transform.SetParent(engine.transform,true);
                var ps=stack.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.loop=true;main.startLifetime=5;main.startSpeed=1.6f;main.startSize=new ParticleSystem.MinMaxCurve(1.2f,2.6f);
                main.startColor=new Color(.32f,.3f,.3f,.55f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.playOnAwake=false;main.maxParticles=120;var em=ps.emission;em.rateOverTime=12;
                var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.4f;stack.transform.rotation=Quaternion.Euler(-90,0,0);
                var pr=stack.GetComponent<ParticleSystemRenderer>();pr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Scenes/MeridianCrawl-Dust-soft.mat")??new Material(Resources.Load<Material>("Effects/flare_01"));pr.shadowCastingMode=ShadowCastingMode.Off;
                ride.smoke=new[]{ps};
                var head=new GameObject("Engine car headlamp",typeof(Light)).GetComponent<Light>();head.type=LightType.Spot;head.spotAngle=40;head.range=40;head.intensity=2.4f;head.color=new Color(1,.86f,.62f);head.shadows=LightShadows.None;
                head.transform.SetPositionAndRotation(new Vector3(Cars[2].at.x,4,Gallery.yMin-.2f),Quaternion.Euler(12,180,0));head.transform.SetParent(engine.transform,true);head.enabled=false;ride.headlamp=head;
                return ride;
            }
            /// <summary>One of the Crawl's runabouts: a cab on two little treads, a lamp, a load lashed on behind. Drawn on a holder at its middle (a way's prompt measures from there).</summary>
            static GameObject Runabout(ZoneKit kit,string name,Vector2 at,float yaw)
            {
                var s=new ZoneKit.Shape();Color iron=new(.3f,.3f,.33f),paint=new(.62f,.3f,.22f),brass=new(.92f,.72f,.42f);
                foreach(int side in new[]{-1,1}){s.Slot(0).Box(new Vector3(side*1.05f,.45f,0),new Vector3(.55f,.9f,3.2f),Quaternion.identity,new Color(.2f,.2f,.22f),new Color(.32f,.32f,.34f));
                    for(int k=0;k<4;k++)s.Slot(1).Disc(new Vector3(side*1.34f,.42f,-1.2f+k*.8f),side<0?Vector3.left:Vector3.right,.3f,10,brass,.12f);}
                s.Slot(2).Box(new Vector3(0,1.15f,-.2f),new Vector3(1.7f,.7f,2.6f),Quaternion.identity,paint,new Color(.72f,.38f,.28f));
                s.Slot(2).Box(new Vector3(0,1.9f,.5f),new Vector3(1.5f,.9f,1.2f),Quaternion.identity,paint,new Color(.74f,.4f,.3f)).Slot(3).Box(new Vector3(0,1.95f,1.12f),new Vector3(1.2f,.5f,.04f),Quaternion.identity,new Color(.95f,.8f,.5f));
                s.Slot(1).Tube(new[]{new Vector3(0,1.5f,1.4f),new Vector3(0,1.5f,1.7f)},new[]{.18f,.2f},10,brass,brass);
                s.Slot(0).Box(new Vector3(0,1.75f,-.9f),new Vector3(1.4f,.5f,.9f),Quaternion.Euler(0,4,0),new Color(.5f,.42f,.32f),new Color(.6f,.52f,.4f));
                var body=Make(kit,name,s,kit.On(at.x,at.y),Quaternion.Euler(0,yaw,0),null,Crawl.Iron,Crawl.Brass,Crawl.Iron,Crawl.Lamp);
                ZoneKit.Block(body,kit.On(at.x,at.y,1.1f),new Vector3(2.8f,2.2f,3.2f),yaw,name+" body");
                Casts(ZoneKit.Drawn(body));return body;
            }
            /// <summary>The crawler's tool cart: a two-wheeled cart with a strapped toolbox and a lamp; the free repair at the run's start.</summary>
            static GameObject ToolCart(ZoneKit kit,string name,Vector2 at,float yaw)
            {
                var s=new ZoneKit.Shape();Color wood=new(.56f,.44f,.32f),iron=new(.3f,.3f,.33f);
                s.Slot(0).Box(new Vector3(0,.7f,0),new Vector3(1.4f,.12f,1),Quaternion.identity,wood,new Color(.66f,.54f,.4f)).Box(new Vector3(0,1.0f,0),new Vector3(1.1f,.5f,.7f),Quaternion.identity,new Color(.5f,.22f,.18f),new Color(.6f,.3f,.24f));
                foreach(int side in new[]{-1,1})s.Slot(1).Disc(new Vector3(side*.75f,.45f,0),side<0?Vector3.left:Vector3.right,.45f,14,iron,.32f,new Color(.4f,.4f,.42f));
                s.Slot(0).Beam(new Vector3(0,.7f,.5f),new Vector3(0,.25f,1.5f),.06f,.06f,wood);
                s.Slot(2).Lump(new Vector3(.45f,1.45f,-.3f),new Vector3(.1f,.14f,.1f),3,.02f,Color.white,Color.white,6,4);
                var body=Make(kit,name,s,kit.On(at.x,at.y),Quaternion.Euler(0,yaw,0),null,Crawl.Timber,Crawl.Iron,Crawl.Lamp);
                ZoneKit.Block(body,kit.On(at.x,at.y,.6f),new Vector3(1.5f,1.2f,1.2f),yaw,"Tool cart");
                return body;
            }

            // ---------------------------------------------------------------- what the light teaches, and what throws shade
            /// <summary>The run's opening stretch: a burrower's body hunting along the warm edge that the light takes (a drawn body, never a fighter), the first sight of it said; an old
            /// buggy in the Crawl's paint west of the way whose paint blisters; frost lying in the hollows that flashes off.</summary>
            static void Lessons(ZoneKit kit)
            {
                var hunter=new GameObject("A burrower hunting along the light");hunter.transform.SetPositionAndRotation(kit.On(Lesson.x,Lesson.y),Quaternion.Euler(0,90,0));
                var prefab=ZoneKit.Prefab("Enemies","HeatScavenger");GameObject body;
                if(prefab!=null)
                {
                    body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.name="Its body";
                    foreach(var c in body.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                    foreach(var m in body.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(m);
                    var b=ZoneKit.Drawn(body);body.transform.localScale*=2.2f/Mathf.Max(.1f,Mathf.Max(b.size.x,b.size.z));
                    body.transform.SetParent(hunter.transform,false);b=ZoneKit.Drawn(body);body.transform.position+=new Vector3(hunter.transform.position.x-b.center.x,hunter.transform.position.y-b.min.y,hunter.transform.position.z-b.center.z);
                }
                else{var s=new ZoneKit.Shape();s.Lump(new Vector3(0,.35f,0),new Vector3(.5f,.32f,1.1f),5,.2f,new Color(.3f,.28f,.26f),new Color(.2f,.18f,.18f),10,6);body=Make(kit,"Its body",s,hunter.transform.position,hunter.transform.rotation,hunter.transform,SpineRock);}
                var t=hunter.AddComponent<DawnTouched>();t.kind=DawnTouched.Kind.Creature;t.at=-Lesson.y;t.body=body.transform;t.skitter=3.2f;t.pace=2.2f;
                var seen=new GameObject("First sight of a burrower",typeof(FirstSight)).GetComponent<FirstSight>();seen.transform.position=kit.On(Lesson.x,Lesson.y);seen.situation="scavenger";seen.range=15;
                // The buggy: an old crawler runabout left here years ago, on its side a little, its paint the Crawl's rust.
                var buggy=new ZoneKit.Shape();Color paint=new(.7f,.32f,.22f),iron=new(.3f,.3f,.33f);
                foreach(int side in new[]{-1,1})buggy.Slot(0).Box(new Vector3(side*1.2f,.45f,0),new Vector3(.6f,.9f,3.6f),Quaternion.identity,new Color(.22f,.22f,.24f));
                buggy.Slot(0).Box(new Vector3(0,1.2f,-.3f),new Vector3(2.1f,.8f,2.8f),Quaternion.identity,paint,new Color(.8f,.4f,.28f)).Box(new Vector3(0,2.05f,.6f),new Vector3(1.8f,1,1.4f),Quaternion.identity,paint,new Color(.82f,.42f,.3f));
                buggy.Slot(0).Box(new Vector3(0,2.1f,1.32f),new Vector3(1.4f,.55f,.05f),Quaternion.identity,new Color(.2f,.24f,.28f)).Box(new Vector3(-.4f,1.6f,-1.6f),new Vector3(.9f,.6f,.5f),Quaternion.Euler(0,12,0),new Color(.6f,.5f,.36f));
                var old=Make(kit,"An old crawler buggy",buggy,kit.On(Buggy.x,Buggy.y,-.15f),Quaternion.Euler(0,-24,6),null,Crawl.Iron);
                ZoneKit.Block(old,kit.On(Buggy.x,Buggy.y,1.1f),new Vector3(2.8f,2.2f,3.8f),-24,"Buggy");
                var p=old.AddComponent<DawnTouched>();p.kind=DawnTouched.Kind.Paint;var box=ZoneKit.Drawn(old);p.at=-box.max.z;p.span=box.size.z;p.tint=Color.white;p.scorched=new Color(.32f,.22f,.17f);
                Casts(box);
                // Frost: one sheet over the floor where rime lies, its alpha the rime; the shader burns it off behind the line.
                var points=new List<Vector3>();var tone=new List<Color>();var tris=new List<int>();var index=new Dictionary<(int,int),int>();
                int At(int x,int z){if(index.TryGetValue((x,z),out int k))return k;var q=new Vector2(x,z);points.Add(new Vector3(x,Height(q)+.035f,z));tone.Add(new Color(1,1,1,Frost(q)));return index[(x,z)]=points.Count-1;}
                for(int z=440;z<740;z++)for(int x=-28;x<28;x++)
                {
                    if(Frost(new Vector2(x,z))+Frost(new Vector2(x+1,z))+Frost(new Vector2(x,z+1))+Frost(new Vector2(x+1,z+1))<.04f)continue;
                    int a=At(x,z),b=At(x+1,z),c=At(x,z+1),d=At(x+1,z+1);tris.AddRange(new[]{a,c,d,a,d,b});
                }
                var mesh=new Mesh{name="Terminator frost",indexFormat=IndexFormat.UInt32};mesh.SetVertices(points);mesh.SetColors(tone);mesh.SetTriangles(tris,0);mesh.SetUVs(0,points.Select(v=>new Vector2(v.x,v.z)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();
                mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-Frost.asset");
                var frost=new GameObject("Floor detail frost",typeof(MeshFilter),typeof(MeshRenderer));frost.GetComponent<MeshFilter>().sharedMesh=mesh;var fr=frost.GetComponent<MeshRenderer>();fr.sharedMaterial=Rime;fr.shadowCastingMode=ShadowCastingMode.Off;fr.receiveShadows=false;
            }
            /// <summary>Wind-cut rock spines in two fields, east and west of the way, and a few by it: the shade the run passes through. Tall ones stand east of the way (the camera is
            /// south-west of the party, so a tall rock east of it never stands between); west of it only short ones, and none on the drag lane, where the cars run.</summary>
            static void Spines(ZoneKit kit)
            {
                var rng=new System.Random(909);float R()=>(float)rng.NextDouble();var s=new ZoneKit.Shape();int n=0;var holder=new GameObject("Rock spines");
                void Spine(Vector2 at,float tall,float wide)
                {
                    var foot=kit.On(at.x,at.y,-.25f);int shards=2+(int)(R()*2);var shape=new ZoneKit.Shape();
                    for(int k=0;k<shards;k++)
                    {
                        var lean=new Vector3(R()-.5f,0,R()-.5f)*.35f;float h=tall*(k==0?1:.55f+.35f*R()),r=wide*(k==0?1:.6f+.3f*R());var root=new Vector3(R()-.5f,0,R()-.5f)*wide*.9f;
                        shape.Slot(0).Tube(new[]{root,root+Vector3.up*h*.5f+lean*h*.25f,root+Vector3.up*h+lean*h*.5f},new[]{r,r*.62f,r*.12f},6,new Color(.46f,.46f,.52f),new Color(.66f,.66f,.72f),1,.4f,true);
                    }
                    var go=Make(kit,"Rock spine "+(++n),shape,foot,Quaternion.Euler(0,R()*360,0),holder.transform,SpineRock);
                    var box=ZoneKit.Drawn(go);ZoneKit.Block(go,new Vector3(box.center.x,foot.y+Mathf.Min(box.size.y,3)*.5f+.25f,box.center.z),new Vector3(box.size.x*.6f,Mathf.Min(box.size.y,3),box.size.z*.6f),0,"Spine");
                    ZoneKit.Fadeable(go);Warms(go,Color.white);Casts(box);
                }
                foreach(var (z0,z1) in new[]{(500f,562f),(240f,302f)})
                {
                    // By the way: a short one west, taller ones east, their shadows lying along it.
                    for(float z=z0+4;z<z1-2;z+=9+R()*5)
                    {
                        float x=WayX(z);bool east=R()<.65f;
                        if(east)Spine(new Vector2(x+2.8f+R()*1.2f,z),3+R()*3,.6f+R()*.3f);
                        else if(x-3.4f>-1)Spine(new Vector2(x-3.4f-R()*.6f,z),2.4f+R()*1.1f,.5f+R()*.2f);
                    }
                    // The fields: east to the scarp's foot, west beyond the drag lane.
                    for(int k=0;k<26;k++)
                    {
                        var p=new Vector2(R()<.6f?9+R()*15:-27+R()*7.5f,z0+R()*(z1-z0));float tall=p.x>0?2.8f+R()*3.4f:2.2f+R()*1.6f,wide=.55f+R()*.45f;
                        if(FromWay(p)<4||Lane(p)>0||Height(p)>.8f)continue;Spine(p,tall,wide);
                    }
                }
                Debug.Log("TERMINATOR_SPINES "+n);
            }

            // ---------------------------------------------------------------- the dawn itself
            static DawnLine Dawn(ZoneKit kit,List<Transform> checkpoints,TreadRide ride,Transform wayHome)
            {
                var go=new GameObject("The dawn");go.transform.position=new Vector3(0,0,LightStarts);go.SetActive(false);
                var dawn=go.AddComponent<DawnLine>();dawn.axis=new Vector2(0,-1);dawn.reach=Reach;dawn.speeds=new[]{1.6f,2.2f,2.7f,2.7f};dawn.rideSpeed=3.2f;dawn.rideGap=6;
                dawn.couplingFlags=Cars.Select(c=>c.flag).ToArray();dawn.checkpoints=checkpoints.ToArray();dawn.rideFlag="home.rideDone";dawn.ride=ride;dawn.home=wayHome;dawn.homeFlag="home.treadRecovered";
                dawn.startWalk=6;dawn.startWait=8;dawn.overBehind=90;dawn.burnFrom=.1f;dawn.burnClimb=.28f;dawn.edgeGrace=.25f;dawn.markAhead=1.6f;dawn.markWidth=40;dawn.pursueWithin=8;dawn.nearWithin=5;
                var key=GameObject.Find("KeyLight");dawn.sun=key!=null?key.GetComponent<Light>():null;dawn.sunYaw=180;dawn.pitchNear=24;dawn.pitchFar=9;dawn.farGap=26;dawn.intensityNear=1.15f;dawn.intensityFar=.62f;
                // The veil: a low wall of light standing over the edge, ninety metres across, which the line carries.
                var v=new Mesh{name="Dawn veil"};var vp=new List<Vector3>();var vu=new List<Vector2>();var vt=new List<int>();
                for(int k=0;k<=18;k++){float x=-45+k*5;vp.Add(new Vector3(x,0,0));vp.Add(new Vector3(x,4.5f,0));vu.Add(new Vector2(k/18f,0));vu.Add(new Vector2(k/18f,1));if(k>0){int a=(k-1)*2;vt.AddRange(new[]{a,a+1,a+3,a,a+3,a+2});}}
                v.SetVertices(vp);v.SetUVs(0,vu);v.SetTriangles(vt,0);v.RecalculateNormals();v.RecalculateBounds();v=ZoneKit.SaveMesh(v,"Assets/_Project/Scenes/"+kit.zone+"-Veil.asset");
                var veil=new GameObject("The dawn's veil",typeof(MeshFilter),typeof(MeshRenderer));veil.GetComponent<MeshFilter>().sharedMesh=v;var vr=veil.GetComponent<MeshRenderer>();vr.sharedMaterial=Veil;vr.shadowCastingMode=ShadowCastingMode.Off;vr.receiveShadows=false;
                veil.transform.SetParent(go.transform,false);dawn.veil=veil.transform;
                // Sparks and steam along the edge, left where they rose (world space) as the line moves on.
                var edge=new GameObject("The dawn's edge",typeof(ParticleSystem));edge.transform.SetParent(go.transform,false);
                var ps=edge.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.loop=true;main.prewarm=true;main.duration=4;
                main.startLifetime=new ParticleSystem.MinMaxCurve(.6f,1.6f);main.startSpeed=new ParticleSystem.MinMaxCurve(.4f,1.4f);main.startSize=new ParticleSystem.MinMaxCurve(.05f,.16f);
                main.startColor=new Color(1,.9f,.7f,.9f);main.gravityModifier=-.15f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=500;main.playOnAwake=true;
                var em=ps.emission;em.rateOverTime=90;var sh=ps.shape;sh.shapeType=ParticleSystemShapeType.Box;sh.scale=new Vector3(70,.2f,.6f);
                var pr=edge.GetComponent<ParticleSystemRenderer>();pr.sharedMaterial=kit.Glow("Dawn sparks",false,new Color(1.5f,1.0f,.5f,1));pr.shadowCastingMode=ShadowCastingMode.Off;pr.receiveShadows=false;
                dawn.edge=ps;
                // Steam off the ground the light has taken: wisps rising for a metre or two behind the edge, left where they rose as the line moves on, so the dawn has a body in the
                // frame and not only an edge (the lead, November 14: the dawn more present in the frame during the run).
                var steam=new GameObject("The dawn's steam",typeof(ParticleSystem));steam.transform.SetParent(go.transform,false);steam.transform.localPosition=new Vector3(0,.4f,1.5f);
                var sp=steam.GetComponent<ParticleSystem>();sp.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var sm=sp.main;sm.loop=true;sm.prewarm=true;sm.duration=6;
                sm.startLifetime=new ParticleSystem.MinMaxCurve(3,5.5f);sm.startSpeed=new ParticleSystem.MinMaxCurve(.05f,.2f);sm.startSize=new ParticleSystem.MinMaxCurve(1.4f,3.2f);
                // Wisps, not a wall: three hundred additive puffs two metres wide overlapping eight deep along a 2.6 m strip summed to white in the player (an editor still never runs
                // a particle, so only the November 20 capture showed it: the blown-out band along s56's dawn line). Fewer, smaller, and laid over the ground, not added to it.
                sm.startLifetime=new ParticleSystem.MinMaxCurve(2.6f,4.4f);sm.startSize=new ParticleSystem.MinMaxCurve(1.1f,2.4f);
                sm.startColor=new Color(1,.95f,.88f,.18f);sm.simulationSpace=ParticleSystemSimulationSpace.World;sm.maxParticles=160;sm.playOnAwake=true;
                var se=sp.emission;se.rateOverTime=24;var ss=sp.shape;ss.shapeType=ParticleSystemShapeType.Box;ss.scale=new Vector3(70,.3f,2.6f);
                var rise=sp.velocityOverLifetime;rise.enabled=true;rise.space=ParticleSystemSimulationSpace.World;rise.x=new ParticleSystem.MinMaxCurve(-.15f,.15f);rise.y=new ParticleSystem.MinMaxCurve(.5f,1.1f);rise.z=new ParticleSystem.MinMaxCurve(.1f,.3f);
                var fade=sp.colorOverLifetime;fade.enabled=true;var ramp=new Gradient();
                ramp.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.3f),new GradientAlphaKey(0,1)});fade.color=ramp;
                var steamLook=kit.Glow("Dawn steam",false,new Color(.94f,.9f,.84f,1));
                steamLook.SetFloat("_Blend",0);steamLook.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);steamLook.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                steamLook.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);steamLook.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);EditorUtility.SetDirty(steamLook);
                var sr=steam.GetComponent<ParticleSystemRenderer>();sr.sharedMaterial=steamLook;sr.shadowCastingMode=ShadowCastingMode.Off;sr.receiveShadows=false;
                dawn.casters=casters.ToArray();
                dawn.stills=Stills.Select(s=>new Vector3(s.at.x,0,s.at.y)).ToArray();dawn.stillGaps=Stills.Select(s=>s.gap).ToArray();dawn.stillGap=9;
                go.SetActive(true);
                return dawn;
            }
            /// <summary>The light behind the line, drawn over everything the landform is (floor and walls alike) by the Dawn shader: the whole landform's own heightfield again, nothing
            /// drawn ahead of the edge. Named as a floor detail: it has no height of its own.</summary>
            static void TheLight(ZoneKit.Land land)
            {
                var light=new GameObject("Floor detail the dawn's light",typeof(MeshFilter),typeof(MeshRenderer));light.GetComponent<MeshFilter>().sharedMesh=land.collision;
                var r=light.GetComponent<MeshRenderer>();r.sharedMaterial=Light;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            }

            // ---------------------------------------------------------------- dressing and the floor
            /// <summary>Drawn only, at the band's edges: boulders along the banks' and the scarp's feet (small on the camera's side), warmed as the light reaches each.</summary>
            static void Dress(ZoneKit kit)
            {
                var rng=new System.Random(71);float R()=>(float)rng.NextDouble();var feet=new List<Vector3>();
                // Each at its bank's own foot (the height function's: where the rise begins), a little up it, so a rock leans on the bank and never stands out on the floor.
                float Wobble(float z)=>1.6f*(Mathf.PerlinNoise(z*.05f,3.3f)-.5f);float Wander(float z)=>4f*(Mathf.PerlinNoise(z*.03f,9.1f)-.5f);
                for(float z=38;z<764;z+=5.5f)foreach(float side in new[]{-1f,1f})
                {
                    // East, on the scarp's upper slope beyond its limit (x 38 to 41) and the navigation: at its foot one stood a crevice from the slope that a body wedged in
                    // (focused run 2), and just under the limit another made one with the limit's box (run 3).
                    float zz=z+R()*3,x=side<0?-29-Wobble(zz)-.3f-R()*.7f:38f+R()*3f;var p=new Vector2(x,zz);if(Lane(p)>0||FromWay(p)<5)continue;feet.Add(new Vector3(p.x,0,p.y));
                }
                for(float x=-27;x<25;x+=6){float zz=767-2*Wobble(768)+.3f+R()*.6f;feet.Add(new Vector3(x+R()*3,0,zz));}
                for(float x=-27;x<25;x+=7){float zz=35-.5f*Wobble(34)-.4f-R()*.5f;var p=new Vector2(x+R()*3,zz);if(FromWay(p)<5)continue;feet.Add(new Vector3(p.x,0,p.y));}
                var small=new[]{("DressBoulders",1.0f,1.8f),("DressStrataOutcrop",1.2f,2f)};var big=new[]{("RidgeRock1",2.2f,4.4f),("RidgeRock2",2.2f,4.4f),("RidgeRock3",2.2f,4.4f),("DressStrataOutcrop",1.8f,3f)};
                var holder=new GameObject("Band edge rocks");
                kit.WallFoot(feet.Where(f=>f.x<0||f.z<40),61,small,rock=>{Warms(rock,new Color(.42f,.44f,.56f));rock.transform.SetParent(holder.transform,true);return true;});
                kit.WallFoot(feet.Where(f=>f.x>0&&f.z>=40),62,big,rock=>{Warms(rock,new Color(.4f,.42f,.54f));rock.transform.SetParent(holder.transform,true);return true;});
            }
            /// <summary>Where the ride sets the party down: links the Crawl threw going by, half buried along its tracks, and the crew's marker post with its lamp beside the crawler,
            /// so the run's end is a place and not a bare stretch of the plain (look pass 06). Clear of the way home and of where the engine car stops.</summary>
            static void TheEnd(ZoneKit kit)
            {
                var rng=new System.Random(55);float R()=>(float)rng.NextDouble();var s=new ZoneKit.Shape();Color iron=new(.3f,.3f,.33f),rust=new(.46f,.32f,.26f);int laid=0;
                for(int k=0;k<40&&laid<16;k++)
                {
                    var p=new Vector2(-24+R()*48,36+R()*30);
                    if(p.x>-18.5f&&p.x<.5f&&p.y>48)continue;if(ZoneKit.FromPath(p,Home)<2.5f||ZoneKit.Segment(p,CrawlerHand,Crawler)<3||Vector2.Distance(p,Tail)<3)continue;
                    var turn=Quaternion.Euler(R()*8-4,R()*180,R()*10-5);var at=kit.On(p.x,p.y,.06f);
                    s.Slot(0).Box(at,new Vector3(1.4f,.18f,.8f),turn,R()<.5f?iron:rust,new Color(.44f,.42f,.42f)).Box(at+turn*new Vector3(0,.13f,0),new Vector3(1.4f,.1f,.14f),turn,new Color(.4f,.38f,.38f));laid++;
                }
                if(s.Vertices>0)kit.Built("Links the Crawl threw",s,Crawl.Iron);
                var post=new ZoneKit.Shape();var foot=kit.On(Crawler.x+2.6f,Crawler.y+2.2f);
                post.Slot(0).Post(foot,3.2f,.08f,.06f,6,iron,iron).Beam(foot+Vector3.up*3.1f,foot+new Vector3(-.6f,3.1f,0),.05f,.05f,iron).Slot(2).Box(foot+new Vector3(.05f,2.2f,0),new Vector3(.03f,.5f,.4f),Quaternion.identity,new Color(.9f,.86f,.72f));
                post.Slot(1).Lump(foot+new Vector3(-.6f,2.75f,0),new Vector3(.13f,.18f,.13f),5,.03f,Color.white,Color.white,8,5);
                var marker=kit.Built("The crew's marker post",post,Crawl.Iron,Crawl.Lamp,Crawl.Cloth);ZoneKit.Block(marker,foot+Vector3.up*1.6f,new Vector3(.25f,3.2f,.25f),0,"Marker post");
                kit.Lamp(new Vector3(Crawler.x+2,2.6f,Crawler.y+2.2f),8,1.2f,new Color(1,.78f,.5f)).name="The crew's marker lamp";
            }
            /// <summary>The road a town has driven for generations (the lead, November 14: long stretches of the run were empty ground, six stills of eighteen plain floor). Cairns the
            /// crews have stacked along it, some with a marker pole and a lamp; what the halls have shed on it (links, a sled's runner, an axle, a coil of drag cable, a spent fuel can);
            /// the tread units of a hall abandoned long ago, left where they gave out; and, as the light reaches each, the cairns warm with the rest. Drawn only, low, and off the way,
            /// the drag lane and every fight's middle: nothing is solid, nothing throws shade (the burrowers' cover is the spines' and the cars'), but for two whole halls the town
            /// left east of the way, which are solid (and throw no shade a burrower reads).</summary>
            static void Road(ZoneKit kit)
            {
                var rng=new System.Random(313);float R()=>(float)rng.NextDouble();var holder=new GameObject("The road the halls have driven");
                bool Free(Vector2 p,float from,bool low=false)=>FromWay(p)>=from&&Lane(p)<=0&&Height(p)<.9f&&(low||!Fights.Any(f=>Vector2.Distance(p,f.at)<7))&&p.x>-27.5f&&p.x<25&&p.y>36&&p.y<764
                    &&Vector2.Distance(p,Arrival)>6&&Vector2.Distance(p,Tail)>4&&ZoneKit.FromPath(p,Home)>3&&Cars.All(c=>Mathf.Abs(p.x-c.at.x)>9||Mathf.Abs(p.y-c.at.y)>14);
                Color stone=new(.5f,.5f,.56f),stoneTop=new(.62f,.62f,.68f),iron=new(.3f,.3f,.33f),rust=new(.46f,.32f,.26f);int cairns=0,shed=0;
                // ---- cairns: one every sixteen metres or so, east of the way and west of the drag lane by turns, a marker pole on every second, a lamp on every fourth
                int n=0;
                for(float z=752;z>44;z-=12+R()*9)
                {
                    bool east=n%2==0;var p=east?new Vector2(WayX(z)+5.5f+R()*3,z):new Vector2(-21-R()*4,z);n++;
                    if(!Free(p,4.5f)){p.x+=east?2.5f:-2;if(!Free(p,4.5f))continue;}
                    var s=new ZoneKit.Shape();float y=0,tall=0;int stones=4+(int)(R()*4);
                    for(int k=0;k<stones;k++){float w=.62f-k*.07f+R()*.08f,h=.16f+R()*.08f;s.Slot(0).Lump(new Vector3((R()-.5f)*.12f,y+h*.5f,(R()-.5f)*.12f),new Vector3(w,h*.5f,w*.85f),k+n*7,.2f,stoneTop,stone,8,4);y+=h*.82f;}
                    tall=y;
                    {
                        // The marker: a pole out of every cairn's top, a band of the Crawl's cream at its head, a strip of cloth that the wind has had for years, and on every second
                        // a lantern the crews keep lit for the halls behind. Tall enough that the low sun lays each one's shadow down the plain ahead of the party: half the cairns had
                        // a pole and one in four a lamp, and the run's stills showed nothing that stood up from the plain or said who drives it (the November 20 capture, s56).
                        const float Pole=3.4f;
                        s.Slot(1).Post(new Vector3(0,tall-.2f,0),Pole,.06f,.045f,6,iron,iron).Box(new Vector3(0,tall+Pole-.5f,0),new Vector3(.15f,.34f,.15f),Quaternion.identity,new Color(.9f,.86f,.72f));
                        s.Slot(2).Quad(new Vector3(.06f,tall+Pole-.36f,0),new Vector3(.7f,tall+Pole-.5f,.08f),new Vector3(.7f,tall+Pole-.74f,.08f),new Vector3(.06f,tall+Pole-.62f,0),Vector3.back,new Color(.36f,.56f,.62f));
                        if(n%2==0)
                        {
                            var head=new Vector3(0,tall+Pole-.12f,0);s.Slot(1).Beam(head,head+new Vector3(.42f,0,0),.04f,.04f,iron).Box(head+new Vector3(.42f,.02f,0),new Vector3(.24f,.04f,.24f),Quaternion.identity,new Color(.92f,.72f,.42f));
                            s.Slot(3).Lump(head+new Vector3(.42f,-.2f,0),new Vector3(.13f,.17f,.13f),n,.03f,Color.white,Color.white,6,4);
                        }
                    }
                    s.Slot(3).Slot(0);var go=Make(kit,"Cairn "+n,s,kit.On(p.x,p.y,-.06f),Quaternion.Euler(0,R()*360,0),holder.transform,SpineRock,Crawl.Iron,Crawl.Cloth,Crawl.Lamp);
                    Warms(go,Color.white);ZoneKit.Fadeable(go);cairns++;
                }
                // ---- what the halls have shed: low iron off the way, in twos and threes
                for(int k=0;k<320;k++)
                {
                    var p=new Vector2(-26+R()*50,40+R()*720);if(!Free(p,3.2f,true))continue;
                    var s=new ZoneKit.Shape();int kind=rng.Next(5);var turn=Quaternion.Euler(0,R()*180,0);
                    switch(kind)
                    {
                        case 0: // a thrown tread link, its pin out
                            s.Slot(0).Box(new Vector3(0,.09f,0),new Vector3(1.4f,.18f,.8f),turn,R()<.5f?iron:rust,new Color(.44f,.42f,.42f)).Box(new Vector3(0,.22f,0),new Vector3(1.4f,.1f,.14f),turn,new Color(.4f,.38f,.38f));
                            if(R()<.6f)s.Slot(0).Box(turn*new Vector3(1.3f,.09f,.3f),new Vector3(1.4f,.18f,.8f),turn*Quaternion.Euler(0,24,0),rust,new Color(.42f,.38f,.36f));break;
                        case 1: // a sled's runner, snapped
                            s.Slot(0).Box(new Vector3(0,.08f,0),new Vector3(.24f,.16f,2.6f),turn*Quaternion.Euler(0,0,8),iron,new Color(.4f,.4f,.42f)).Box(turn*new Vector3(.9f,.06f,.4f),new Vector3(1.3f,.12f,.9f),turn,rust,new Color(.5f,.42f,.36f));break;
                        case 2: // an axle with one wheel still on it
                            s.Slot(0).Tube(new[]{turn*new Vector3(-1,.28f,0),turn*new Vector3(1,.28f,0)},new[]{.09f,.09f},8,iron,iron).Disc(turn*new Vector3(-1,.28f,0),turn*Vector3.left,.42f,12,rust,.12f,iron);break;
                        case 3: // a coil of parted drag cable
                            for(int c=0;c<3;c++){var ring=new List<Vector3>();for(int a=0;a<=16;a++){float t=a*Mathf.PI/8;ring.Add(new Vector3(Mathf.Cos(t)*(.75f-c*.12f),.07f+c*.1f,Mathf.Sin(t)*(.75f-c*.12f)));}
                                s.Slot(0).Tube(ring,Enumerable.Repeat(.07f,ring.Count).ToList(),6,new Color(.24f,.22f,.22f),new Color(.32f,.3f,.28f),1,1,false);}break;
                        default: // a spent fuel can and its lid
                            s.Slot(0).Post(Vector3.zero,.5f,.2f,.2f,10,new Color(.5f,.3f,.24f),new Color(.6f,.36f,.28f)).Disc(turn*new Vector3(.5f,.02f,.2f),Vector3.up,.18f,10,new Color(.44f,.42f,.4f));break;
                    }
                    Make(kit,"Shed by the halls "+(++shed),s,kit.On(p.x,p.y,-.02f),Quaternion.identity,holder.transform,Crawl.Iron);
                }
                // ---- a hall abandoned long ago: one of its tread units where it gave out, east of the way where the frame is widest
                foreach(var (at,yaw) in new[]{(new Vector2(15.5f,612f),8f),(new Vector2(14f,368f),-6f),(new Vector2(16f,205f),14f),(new Vector2(12.5f,84f),-10f)})
                {
                    if(!Free(at,6)){Debug.Log("TERMINATOR_HULK_DROPPED "+at);continue;}
                    var s=new ZoneKit.Shape();var turn=Quaternion.Euler(0,yaw,0);
                    s.Slot(0).Box(new Vector3(0,.35f,0),new Vector3(2.4f,.9f,7.4f),turn,new Color(.26f,.26f,.28f),new Color(.36f,.34f,.34f));
                    for(int k=0;k<5;k++)s.Slot(0).Disc(turn*new Vector3(1.22f,.42f,-2.8f+k*1.4f),turn*Vector3.right,.45f,12,rust,.16f,iron);
                    for(float zz=-3.5f;zz<=3.5f;zz+=.7f)s.Slot(0).Box(turn*new Vector3(0,.82f,zz),new Vector3(2.5f,.08f,.36f),turn,new Color(.34f,.3f,.28f));
                    var hulk=Make(kit,"A hall's tread unit, abandoned",s,kit.On(at.x,at.y,-.08f),Quaternion.identity,holder.transform,Crawl.Iron);Warms(hulk,Color.white);
                }
                // ---- halls the town left where they gave out: whole, stripped, settled into the regolith, east of the way where the frame is widest (never between the camera, south-west
                // of the party, and anyone on the way). From the arrival to the first car nothing on the run was bigger than a buggy, and nothing in its stills said machine (the
                // November 20 capture, s56 009 and 020). Solid.
                foreach(var (at,yaw,sink) in new[]{(new Vector2(19.5f,692f),8f,.7f),(new Vector2(19.5f,574f),-5f,.9f)})
                {
                    var corners=new[]{at,at+new Vector2(-2.6f,-7.4f),at+new Vector2(-2.6f,7.4f),at+new Vector2(2.6f,-7.4f),at+new Vector2(2.6f,7.4f)};
                    if(!corners.All(p=>Free(p,7))){Debug.Log("TERMINATOR_DERELICT_DROPPED "+at);continue;}
                    var c=kit.On(at.x,at.y);var tint=new Color(.64f,.6f,.58f);
                    var hall=Crawl.CrawlTread(kit,"A hall the town left, drawn",c,CarLength,0,tint);
                    hall.transform.RotateAround(c,Vector3.up,90+yaw);hall.transform.RotateAround(c,Quaternion.Euler(0,yaw,0)*Vector3.forward,4);
                    var b=ZoneKit.Drawn(hall);hall.transform.position+=new Vector3(at.x-b.center.x,c.y-sink-b.min.y,at.y-b.center.z);b=ZoneKit.Drawn(hall);
                    UnityEngine.Object.DestroyImmediate(hall.GetComponent<PropTint>());
                    var derelict=new GameObject("A hall the town left where it gave out");derelict.transform.position=c;hall.transform.SetParent(derelict.transform,true);derelict.transform.SetParent(holder.transform,true);
                    ZoneKit.Block(derelict,new Vector3(b.center.x,c.y+(b.max.y-c.y)*.5f,b.center.z),new Vector3(b.size.x-.6f,b.max.y-c.y,b.size.z-.4f),0,"A hall the town left");
                    // Not counted as shade (Casts): its shadow would lie beside the first spines' fight, and the fights stay as they are tuned.
                    Warms(hall,tint);
                    Debug.Log($"TERMINATOR_DERELICT {at} size={b.size}");
                }
                // ---- where the halls are met: the Crawl's crew keep a dump of spares at the run's end, by the crawler (ride-end and the crawler's stills were bare plain).
                var camp=new ZoneKit.Shape();int kept=0;
                foreach(var (x,z,kind) in new[]{(12.5f,53f,0),(14.8f,47.5f,1),(11f,41.2f,2),(2.2f,40.4f,3),(-3.2f,42.6f,4),(-9.5f,41.5f,5),(15.5f,58.5f,2),(-14f,38.5f,0),(17.5f,42f,3),(6.5f,39f,4),
                    (-7f,46f,2),(-10.5f,46.5f,4),(-5.5f,45f,3),(3f,55.5f,0),(5.5f,58f,2),(2.2f,59.5f,5),(4.5f,62f,1),(7.5f,53f,3)})
                {
                    var at=new Vector2(x,z);if(ZoneKit.FromPath(at,Home)<3||Vector2.Distance(at,Tail)<4||Vector2.Distance(at,Crawler)<3.6f||(x>-16.5f&&x<-.5f&&z>51)){Debug.Log("TERMINATOR_CAMP_DROPPED "+at);continue;}
                    var c=kit.On(x,z);var turn=Quaternion.Euler(0,(x*37+z*11)%360,0);
                    switch(kind)
                    {
                        case 0:for(int k=0;k<3;k++)camp.Slot(0).Box(c+Vector3.up*(.1f+k*.2f),new Vector3(1.4f,.18f,.8f),turn*Quaternion.Euler(0,k*14,0),k%2==0?iron:rust,new Color(.44f,.42f,.42f));break;
                        case 1:for(int k=0;k<3;k++){camp.Slot(0).Post(c+turn*new Vector3(-.7f+k*.7f,0,0),.9f,.3f,.3f,10,new Color(.36f,.4f,.46f),new Color(.44f,.48f,.54f)).Disc(c+turn*new Vector3(-.7f+k*.7f,.91f,0),Vector3.up,.3f,10,new Color(.5f,.52f,.56f));}break;
                        case 2:camp.Slot(0).Box(c+Vector3.up*.35f,new Vector3(1.1f,.7f,.8f),turn,new Color(.5f,.42f,.32f),new Color(.6f,.5f,.38f)).Box(c+turn*new Vector3(.9f,.25f,.2f),new Vector3(.7f,.5f,.6f),turn,new Color(.46f,.38f,.3f),new Color(.56f,.46f,.34f));break;
                        case 3:for(int r=0;r<3;r++){var ring=new List<Vector3>();for(int a=0;a<=16;a++){float u=a*Mathf.PI/8;ring.Add(c+new Vector3(Mathf.Cos(u)*(.8f-r*.12f),.07f+r*.1f,Mathf.Sin(u)*(.8f-r*.12f)));}camp.Slot(0).Tube(ring,Enumerable.Repeat(.07f,ring.Count).ToList(),6,new Color(.24f,.22f,.22f),new Color(.32f,.3f,.28f),1,1,false);}break;
                        case 4:camp.Slot(0).Box(c+Vector3.up*.2f,new Vector3(.8f,.4f,.45f),turn,new Color(.58f,.3f,.22f),new Color(.66f,.36f,.26f)).Box(c+turn*new Vector3(0,.42f,0),new Vector3(.82f,.04f,.47f),turn,new Color(.3f,.3f,.33f));
                            camp.Slot(1).Quad(c+turn*new Vector3(-1.4f,.03f,-.6f),c+turn*new Vector3(-1.4f,.03f,.9f),c+turn*new Vector3(-.5f,.03f,.9f),c+turn*new Vector3(-.5f,.03f,-.6f),Vector3.up,new Color(.4f,.46f,.36f));break;
                        default:camp.Slot(0).Box(c+Vector3.up*.18f,new Vector3(1.6f,.12f,1.2f),turn,new Color(.4f,.34f,.28f),new Color(.5f,.42f,.34f));foreach(float s in new[]{-.6f,.6f})camp.Slot(0).Box(c+turn*new Vector3(s,.06f,0),new Vector3(.12f,.12f,2.4f),turn,iron,new Color(.4f,.4f,.42f));break;
                    }
                    kept++;
                }
                if(camp.Vertices>0){camp.Slot(1).Slot(0);Make(kit,"The crew's dump of spares",camp,Vector3.zero,Quaternion.identity,holder.transform,Crawl.Iron,Crawl.Cloth);}
                Debug.Log($"TERMINATOR_ROAD cairns={cairns} shed={shed} camp={kept}");
            }
            /// <summary>Frost in the shade: rime lying for a few metres south of every rock, wreck and car, where the low sun beyond the line has never reached (the Dawn shader's frost kind,
            /// so it flashes to steam and goes as the light comes, as the hollows' does).</summary>
            static void ShadeFrost(ZoneKit kit)
            {
                var points=new List<Vector3>();var tone=new List<Color>();var tris=new List<int>();var index=new Dictionary<(int,int),int>();
                float Shade(Vector2 q)
                {
                    float best=0;
                    foreach(var c in casters)
                    {
                        if(q.x<c.min.x-1||q.x>c.max.x+1||q.y>c.max.y||q.y<c.min.y-Mathf.Max(2,c.height*2.4f))continue;
                        float into=(c.min.y-q.y)/Mathf.Max(2,c.height*2.4f),side=Mathf.Max(c.min.x-q.x,q.x-c.max.x);
                        best=Mathf.Max(best,(1-ZoneKit.Smooth(.4f,1,into))*(1-ZoneKit.Smooth(-.2f,.8f,side)));
                    }
                    float grain=Mathf.PerlinNoise(q.x*.5f+3,q.y*.5f+7);
                    // (Rime laid along the ruts as well read as rain streaking the frame, and before that as shafts of light: it lies in the shade only.)
                    return Mathf.Clamp01(best*ZoneKit.Smooth(.25f,.55f,grain)*(1-Lane(q))*(FromWay(q)<1.2f?.4f:1));
                }
                int At(int x,int z){if(index.TryGetValue((x,z),out int k))return k;var q=new Vector2(x*.5f,z*.5f);points.Add(new Vector3(q.x,Height(q)+.04f,q.y));tone.Add(new Color(1,1,1,Shade(q)));return index[(x,z)]=points.Count-1;}
                for(int z=80;z<1520;z++)for(int x=-56;x<52;x++)
                {
                    var q=new Vector2(x*.5f,z*.5f);if(Shade(q)+Shade(q+new Vector2(.5f,.5f))<.02f)continue;
                    int a=At(x,z),b=At(x+1,z),c=At(x,z+1),d=At(x+1,z+1);tris.AddRange(new[]{a,c,d,a,d,b});
                }
                if(tris.Count==0)return;
                var mesh=new Mesh{name="Terminator shade frost",indexFormat=IndexFormat.UInt32};mesh.SetVertices(points);mesh.SetColors(tone);mesh.SetTriangles(tris,0);mesh.SetUVs(0,points.Select(v=>new Vector2(v.x,v.z)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();
                mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-Shade-frost.asset");
                var frost=new GameObject("Floor detail frost in the shade",typeof(MeshFilter),typeof(MeshRenderer));frost.GetComponent<MeshFilter>().sharedMesh=mesh;var fr=frost.GetComponent<MeshRenderer>();fr.sharedMaterial=RutRime;fr.shadowCastingMode=ShadowCastingMode.Off;fr.receiveShadows=false;
            }
            /// <summary>Where a generation's tread rut runs at a z: its line, wandering a little where each hall found its own.</summary>
            static float RutX(float x,float phase,float z)=>x+1.4f*Mathf.Sin(z*.011f+phase)+.6f*Mathf.Sin(z*.037f+phase*2);
            /// <summary>The generations of tread ruts down the band: each pair's middle line, its wander and how fresh it is.</summary>
            static readonly (float x,float phase,float fresh)[] Ruts=new[]{(-23f,.3f),(-10f,.5f),(-4.5f,.22f),(9f,.42f),(16.5f,.28f),(21f,.36f)}.Select((r,i)=>(r.Item1,(float)new System.Random(77+i*13).NextDouble()*6,r.Item2)).ToArray();
            static void Ground(ZoneKit kit)
            {
                var ground=GroundKit.Open(kit.zone,GroundKit.FindFloor(),131,CliffTop);
                ground.ClearLane(Way,1.3f);ground.ClearLane(Home,1.3f);
                // Grit at .46 in patches over a third of the floor read as oil stains on every stretch (look pass 04): paler, sparser; pale dust drifts give the plain its tone.
                Color dust=new(.8f,.78f,.82f),grit=new(.56f,.55f,.6f),scree=new(.62f,.6f,.66f);
                ground.Layers(new Vector4(.05f,.3f,.36f,.3f),1.2f,new GroundKit.Layer("sand",dust,3.6f),new GroundKit.Layer("gravel",grit,3),new GroundKit.Layer("scree",scree,4));
                ground.PaintPath(0,Way,1.4f,.9f,.35f,.7f,3);ground.PaintPath(0,Home,1.4f,.9f,.35f,.7f,4);
                ground.PaintPatches(0,7,.3f,p=>1-Lane(p),11);
                ground.PaintPatches(1,4,.16f,p=>1-Lane(p)*.6f,5);
                ground.Paint(1,p=>Lane(p)*.35f*GroundKit.Smooth(.4f,.6f,ground.Noise(p,3,8)));
                ground.PaintFoot(2,2.6f,.5f,7);
                ground.Fringe(1,0,.3f,.45f,null,9);
                ground.Decals(new[]{"CracksA","CracksB"},16,p=>GroundKit.Smooth(.4f,.6f,ground.Noise(p,9,3)),1.4f,3,new Color(.36f,.36f,.42f,.9f));
                ground.Decals(new[]{"PebblesA","PebblesB"},12,p=>GroundKit.Smooth(.36f,.54f,ground.Noise(p,6,5)),.8f,1.6f,new Color(.5f,.5f,.56f));
                ground.Strip("Tracks",Way,.2f,1.1f,new Color(.42f,.4f,.44f,.4f));
                // Where the Crawl went by, west, before the hall fell behind: its treads' tracks across the band's south end, where the ride sets the party down.
                foreach(float z in new[]{37.5f,41f,58.5f,62f})ground.Strip("Tracks",new[]{new Vector2(-28,z),new Vector2(26,z+1.2f)},0,2.6f,new Color(.34f,.32f,.36f,.55f));
                // Tread-hall four's own tracks down the drag lane, fresh: its cars ran here before the snatch stopped them (the lane was a plain dark strip in every coupling's still).
                foreach(float side in new[]{-5.2f,5.2f})ground.Strip("Tracks",new[]{new Vector2(Cars[0].at.x,620),new Vector2(Cars[0].at.x,47)},side,2.6f,new Color(.24f,.23f,.27f,.85f));
                // The road the halls have driven for generations: tread ruts down the band, a pair to a hall thirteen metres apart, old ones faint under new, wandering a little where
                // each hall found its own line. (Only the way and the Crawl's crossing were tracked, and long stretches of the run were plain ground: look pass ct9-term-08.)
                // Drawn as strips alone they were too faint to read on the dusk floor (term-road-01): each tread's band is trodden dust as well, pale and crisp-edged, broken where
                // the ground took it badly, and the strip's cleats lie in it.
                var ruts=Ruts;
                foreach(var (x,phase,fresh) in ruts)
                {
                    var path=new List<Vector2>();for(float z=766;z>=32;z-=12)path.Add(new Vector2(RutX(x,phase,z),z));
                    foreach(float side in new[]{-6.5f,6.5f})ground.Strip("Tracks",path.ToArray(),side,2.4f,new Color(.3f,.29f,.33f,Mathf.Min(1,fresh*1.6f)));
                }
                float Trodden(Vector2 p){float best=0;foreach(var (x,phase,fresh) in ruts){float c=RutX(x,phase,p.y);foreach(float side in new[]{-6.5f,6.5f})best=Mathf.Max(best,(1-GroundKit.Smooth(.85f,1.25f,Mathf.Abs(p.x-c-side)))*(.55f+fresh));}
                    return Mathf.Clamp01(best*GroundKit.Smooth(.3f,.48f,ground.Noise(p,6,31)));}
                ground.Paint(0,Trodden);
                // The freshest ruts in the Crawl's own tread prints (Crawl.Track: a grouser's print every plate across a pressed band). As pale dust alone the road the halls drive read
                // as footpaths in the player (the November 20 capture, s56), not as anything a machine made.
                var prints=new List<Vector3>();var printUv=new List<Vector2>();var printTris=new List<int>();int strips=0;
                foreach(var (x,phase,fresh) in ruts)
                {
                    if(fresh<.36f)continue;
                    foreach(float side in new[]{-6.5f,6.5f})
                    {
                        // Not up the scarp's foot.
                        if(x+side>24)continue;
                        float run=0;Vector2 last=default;bool first=true;strips++;
                        for(float z=766;z>=32;z-=1.5f)
                        {
                            var c=new Vector2(RutX(x,phase,z)+side,z);if(!first)run+=Vector2.Distance(last,c);last=c;
                            foreach(float across in new[]{-1.3f,1.3f}){var q=new Vector2(c.x+across,c.y);prints.Add(new Vector3(q.x,Height(q)+.05f,q.y));printUv.Add(new Vector2(run/Crawl.PlainTile,across<0?0:1));}
                            // Wound clockwise seen from above (left, right; the next row south): a quad facing down is culled and never drawn.
                            if(!first){int a=prints.Count-4;printTris.AddRange(new[]{a,a+3,a+2,a,a+1,a+3});}
                            first=false;
                        }
                    }
                }
                var printMesh=new Mesh{name="Terminator tread prints",indexFormat=IndexFormat.UInt32};printMesh.SetVertices(prints);printMesh.SetUVs(0,printUv);printMesh.SetTriangles(printTris,0);
                printMesh.RecalculateNormals();printMesh.RecalculateBounds();printMesh=ZoneKit.SaveMesh(printMesh,"Assets/_Project/Scenes/"+kit.zone+"-Tread-prints.asset");
                var printed=new GameObject("Floor detail tread prints",typeof(MeshFilter),typeof(MeshRenderer));printed.GetComponent<MeshFilter>().sharedMesh=printMesh;
                var printView=printed.GetComponent<MeshRenderer>();printView.sharedMaterial=Crawl.Track;printView.shadowCastingMode=ShadowCastingMode.Off;
                Debug.Log("TERMINATOR_TREAD_PRINTS strips="+strips);
                // Rocks in the band's own strata, not flat-shaded lumps in one dye (the November 20 capture, s56 009), and half as many.
                ground.Look("Canyon",StrataMap);
                ground.Rocks(16,p=>(1-Lane(p))*(.4f+.6f*GroundKit.Smooth(.4f,.6f,ground.Noise(p,7,15))),.1f,.48f,new Color(.58f,.58f,.62f),3,.12f,17);
                ground.Cover(new[]{"Shards"},5,p=>(1-Lane(p))*GroundKit.Smooth(.55f,.7f,ground.Noise(p,10,19)),.1f,.22f,new Color(.44f,.46f,.56f),2);
                ground.Finish();
            }
        }
    }
}
