using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The Continuance (dungeon on foot, with flight between sections; brief, "Continuance"): Meret's engineering vessel, clean, well-kept and signed in a neat hand. The boarding spine (Idra's crews brace it, IdraSpineBraced), the crew deck, maintenance deck four (the captain of the Steady Hand lets the party pass, PatrolLetsPass), the anchor works (the game's earlier machinery, built properly), the command deck and the launch bay; Compact crews who fight, who stand aside, who help. From the bay's empty cradle the party launches into a flight berth over the hull's back, where Meret's craft is fought ship to ship (phase one) and from where the party docks at the frame gantry (the anchor chamber).
    /// <para>Map brief, every coordinate and the contracts (the berth, phase one's room, the crews' rooms): docs/maps/Continuance.md. Every place another file needs is a constant of
    /// <see cref="Continuance"/>. The decks are one heightfield built as chapter 9's Separation Works was (rooms as rounded boxes at their levels, the hull's inner plating between them,
    /// drawn under the camera's real line on its side of every floor); the spine is built over open space south of it; the flight berth is the space over the hull's back, whose top is
    /// drawn at <see cref="Continuance.HullTop"/>, over every deck and under every ship.</para>
    /// </summary>
    public static partial class LastMooringBuilder
    {
        /// <summary>The zone's own fields on its ZoneDef: its own air (tools/continuance_ambience.py: a tuned drive far aft, clean vents, the board's chime, crews overhead; without it the
        /// ship hummed like the Decks' server room). Its sky is the definition's (the convoy's panorama), seen only through the windows, the spine and the bay's mouth.</summary>
        static void ContinuanceDefinition(ZoneDef z){var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(Continuance.Bed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=Continuance.BedLevel;}}

        /// <summary>The Continuance's light. A clean white ship under a cold high key from the starboard quarter (through the windows, over the spine), cool fill so white plating reads as
        /// white and not as grey, a little navy haze far off that takes the convoy back; each deck's own light is its fixtures (the mess's warm lamps, deck four's work lights, the
        /// presses' heat and the weld's blue-white, the chart's teal, the bay's floods). Bloom held so a marking's orange still wins over a lamp.</summary>
        [ZoneLook.Source] static void ContinuanceLooks()
        {
            ZoneLook.Register(ContinuanceZone,new ZoneLook.Spec{sun=C(1f,.96f,.9f),sunIntensity=.8f,sunPitch=54,shadowStrength=.78f,
                sky=C(.31f,.35f,.42f),equator=C(.26f,.28f,.32f),ground=C(.12f,.13f,.15f),
                fog=true,fogColor=C(.09f,.12f,.18f),fogStart=55,fogEnd=230,
                exposure=0,contrast=14,saturation=9,splitShadows=C(.4f,.48f,.56f),splitHighlights=C(.58f,.54f,.47f),
                bloomThreshold=1f,bloomIntensity=.55f,vignette=.24f});
        }

        internal static class Continuance
        {
            // ---------------------------------------------------------------- the numbers (docs/maps/Continuance.md reads these; so do the tests and the fights)
            /// <summary>The decks' two levels, the spine's foot, the flight berth's plane and the hull's top drawn under it.</summary>
            internal const float Up=0,Down=-3,Foot=-5,Plane=19,HullTop=16,GantryWalk=17.6f;
            /// <summary>The spine: its foot (z), its head (z), the hull's south flank where the heightfield starts, the gangway's half width; the heightfield's east edge (windows) and north edge (the bay's mouth).</summary>
            internal const float SpineFoot=-68,SpineHead=-16,HullFace=-8,SpineHalf=2.6f,EdgeX=32,NorthEdge=252,AlcoveHalf=4.4f;
            internal const string SpineSpawn="Spine",BaySpawn="LaunchBay",DeckFourSpawn="DeckFour",MooringBack="Continuance",GantrySpawn="Gantry",HullBerth="HullBerth";
            /// <summary>The zone's bed and the ZoneDef volume tools/continuance_ambience.py measured for it.</summary>
            internal const string Bed="Assets/_Project/Audio/Ambience/AMB_Continuance.ogg";internal const float BedLevel=.315f;
            internal const string MessFight="Continuance_Mess",WorksFight="Continuance_Works",MeretDoor="MeretShip",WorksPassed="clear."+WorksFight;
            /// <summary>The spine's deck height along it: up from the foot to its head, level beyond.</summary>
            internal static float SpineY(float z)=>Mathf.Lerp(Foot,Up,Mathf.InverseLerp(SpineFoot,SpineHead,z));
            /// <summary>The spine's rings; the two braced open by Idra's crews (Sibb's aft, Kott's forward).</summary>
            internal static readonly float[] Rings={-62,-54,-46,-38,-30,-22};
            internal const float AftRing=-54,ForeRing=-30;

            /// <summary>Plan points a player stands at, named once: the keep-offs, the bake's must-reach list, LookDev's views, the map brief and a route all want them.</summary>
            internal static readonly Vector2 ArrivalAt=new(0,-76),HomePadAt=new(4.2f,-81),SpineCradleAt=new(-4.4f,-80),SibbAt=new(-3.6f,AftRing+.6f),KottAt=new(3.6f,ForeRing+.6f),
                SpineTopAt=new(0,-12.5f),HelperAt=new(4.6f,48.2f),DeckFourCradleAt=new(-19.4f,63),CaptainAt=new(0,92),CaptainAside=new(-15,96),CaptainCrossingAt=new(0,86),
                EastDoorAt=new(27,77),WorksSouthDoor=new(0,112.5f),WorksNorthDoor=new(0,171.5f),ChartAt=new(22,190),CacheAt=new(29.4f,203.6f),BayCradleAt=new(9,216),
                PadAt=new(2,238),FootAt=new(-6,229),EmptyCradleAt=new(2,244.5f);
            internal static readonly Vector2[] CaptainsCrew={new(-7.5f,92),new(-3.8f,92),new(3.8f,92),new(7.5f,92)};
            internal static readonly Vector3 Arrival=new(0,Foot,-76),LaunchBayAt=new(5,Down,230),DeckFourAt=new(-17,Down,66);

            // ---- the flight berth round the hull (wiring 6.1.1; the map doc's "Berth contract")
            internal static readonly Bounds HullBerthBox=new(new Vector3(-4,Plane,212),new Vector3(88,3,104));
            internal static readonly Vector3 HullBerthPad=new(PadAt.x,Down,PadAt.y),HullBerthFoot=new(FootAt.x,Down,FootAt.y),HullBerthLaunch=new(-8,Plane,196),HullBerthFlight=new(-8,Plane,182);
            // ---- phase one's room (the map doc's "Boss arena contract")
            internal static readonly Vector3 HailAt=new(-4,Plane,203),HailSize=new(88,8,4),DoorCentre=new(-4,Plane,234),DoorSize=new(80,8,56),MeretAt=new(-4,Plane,252),
                MeretExit=new(24,Plane,258),GantryDock=new(24,Plane,247.5f),MeretAtRail=new(27,GantryWalk,251.2f);
            internal static readonly Vector3[] ClampSeats={new(-30,HullTop,228),new(-4,HullTop,222),new(22,HullTop,228)};
            /// <summary>The shelter's two wings on the hull's back (x from, x to), z 246..260, solid at the plane; the superstructure's foot from z 262.</summary>
            internal static readonly (float x0,float x1)[] Wings={(-16,-12),(4,8)};
            /// <summary>Her shield relays' heads, west first, at the plane: where her fight stands them from her station (BossController.ShipTune.Relays, out along her facing south), and
            /// where this room stands their masts (MeretRelay.MastName).</summary>
            internal static Vector3[] Relays=>BossController.ShipTune.Relays.Select(r=>MeretAt+Quaternion.Euler(0,180,0)*r).OrderBy(r=>r.x).ToArray();
            internal const float WingSouth=246,WingNorth=260,Superstructure=262;

            // ---------------------------------------------------------------- the decks
            internal enum Room{Hatch,Crew,Stair,DeckFour,DeckDoor,WorksDoor,Works,North,CommandHall,Command,Bay}
            static readonly (Room room,float x0,float x1,float z0,float z1,float round)[] Boxes=
            {
                (Room.Hatch,-4,4,-8,9.5f,.3f),(Room.Crew,-18,31.4f,8,50,.6f),(Room.Stair,-3,3,49,60,.2f),(Room.DeckFour,-24,24,59,104,.6f),(Room.DeckDoor,23,28.6f,74,80,.2f),
                (Room.WorksDoor,-3,3,103,113,.2f),(Room.Works,-30,26,112,172,.8f),(Room.North,-3,3,171,196,.2f),(Room.CommandHall,2,17,182,189,.2f),(Room.Command,16,31.4f,176,206,.5f),
                (Room.Bay,-28,12,195,251.2f,.8f),
            };
            static float RoundBox(Vector2 p,float x0,float x1,float z0,float z1,float round)
            {
                var d=new Vector2(Mathf.Abs(p.x-(x0+x1)*.5f)-(x1-x0)*.5f+round,Mathf.Abs(p.y-(z0+z1)*.5f)-(z1-z0)*.5f+round);
                return Mathf.Min(Mathf.Max(d.x,d.y),0)+new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude-round;
            }
            /// <summary>Metres outside the nearest floor (negative on one), and which floor.</summary>
            internal static float Nearest(Vector2 p,out Room room)
            {
                float best=float.PositiveInfinity;room=Room.Hatch;
                foreach(var (r,x0,x1,z0,z1,round) in Boxes){float c=RoundBox(p,x0,x1,z0,z1,round);if(c<best){best=c;room=r;}}
                return best;
            }
            static float Ramp(float v,float v0,float v1,float a,float b)=>Mathf.Lerp(a,b,Mathf.Clamp01((v-v0)/(v1-v0)));
            internal static float Level(Vector2 p,Room room)
            {
                float floor=room switch{Room.Hatch or Room.Crew=>Up,Room.Stair=>Ramp(p.y,50.5f,58.5f,Up,Down),_=>Down};
                // Laid true: a centimetre of difference across a deck, so it isn't a board, and no more.
                return floor+.01f*(Mathf.PerlinNoise(p.x*.37f+3,p.y*.37f+11)-.5f);
            }
            internal static float FloorAt(Vector2 p,out float level){float clear=Nearest(p,out var room);level=Level(p,room);return clear;}
            internal static bool Walkable(Vector2 p)=>p.y>=HullFace&&FloorAt(p,out _)<=0;
            static readonly Vector2 Away=new(Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));
            /// <summary>The windows: the crew deck's east wall and the command deck's, on the hull's outer edge (z from, z to, the floor's level).</summary>
            internal static readonly (float z0,float z1,float level)[] EastWindows={(12.4f,45.6f,Up),(179.4f,202.6f,Down)};
            internal const float SillTall=.9f,MouthX0=-27.2f,MouthX1=11.2f;
            /// <summary>A window's sill on the outer edge, or the bay's lip across its mouth: low, and nothing beyond it but space.</summary>
            static bool Sill(Vector2 p,out float y)
            {
                y=0;
                if(p.x>31.4f)foreach(var (z0,z1,level) in EastWindows)if(p.y>z0&&p.y<z1){y=level+SillTall*Mathf.SmoothStep(0,1,Mathf.Clamp01((p.x-31.4f)/.3f));return true;}
                if(p.y>251.2f&&p.x>MouthX0&&p.x<MouthX1){y=Down+.45f*Mathf.SmoothStep(0,1,Mathf.Clamp01((p.y-251.2f)/.3f));return true;}
                return false;
            }
            /// <summary>The ground. A floor is its room's level; between floors is the hull's inner plating, which with a floor beyond it along the camera's line stays under the sightline to
            /// a hero at that floor's edge (the Waystation's rule, D119) and climbs away too steep to stand on. South of the hull's flank, the spine's deck (built, not this heightfield).</summary>
            internal static float Height(Vector2 p)
            {
                if(p.y<HullFace)return SpineY(p.y);
                float clear=FloorAt(p,out float level);if(clear<=0)return level;
                if(Sill(p,out float sill))return sill;
                float allowed=float.PositiveInfinity;
                for(float d=.5f;d<=16;d+=.5f){if(FloorAt(p+Away*d,out float there)<0){allowed=Mathf.Min(level,there)+.6f+.82f*d;break;}}
                float rough=Mathf.PerlinNoise((p.x+70)*.11f,(p.y+20)*.09f);
                float top=Mathf.Min(allowed,level+7f+.6f*rough);
                return level+(top-level)*Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/.9f));
            }
            /// <summary>How far over a floor anything on the camera's side of it may be drawn, metres back from it along the camera's line (D272, the Works' rule).</summary>
            static float SeenOver(float back)=>Mathf.Min(SeenMost,Mathf.Max(SeenKerb,-.4f+.72f*back));
            const float SeenKerb=.6f,SeenMost=7,LensBack=15,LensNear=6;
            /// <summary>The plating as it is drawn: a low kerb, then a slope under the line of sight, and nothing within five metres of any lens (the Works' <c>Seen</c>).</summary>
            internal static float Seen(Vector2 p)
            {
                float stood=Height(p),clear=FloorAt(p,out float level);if(clear<=0||p.y<HullFace)return stood;
                if(Sill(p,out _))return stood;
                float seen=stood,rise=Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/.9f));
                for(float d=.5f;d<=16;d+=.5f)
                {
                    if(FloorAt(p+Away*d,out float there)>=0)continue;
                    float top=Mathf.Min(level,there)+SeenOver(d);
                    seen=Mathf.Min(stood,level+(top-level)*rise);break;
                }
                float lens=LensCap(p);
                return float.IsPositiveInfinity(lens)?seen:Mathf.Min(seen,Mathf.Max(lens,level+SeenKerb*rise));
            }
            static float LensCap(Vector2 p)
            {
                float cap=float.PositiveInfinity;var under=p+Away*LensBack;
                for(float x=-LensNear;x<=LensNear;x+=1.5f)for(float z=-LensNear;z<=LensNear;z+=1.5f)
                {
                    if(x*x+z*z>LensNear*LensNear)continue;
                    if(FloorAt(under+new Vector2(x,z),out float level)<0)cap=Mathf.Min(cap,level+SeenMost);
                }
                return cap;
            }
            /// <summary>Each deck's own floor: warm white where people live, cool grey where the cutters are kept, darker teal-grey in the works, deep teal under the director's chart.</summary>
            static Color DeckOf(Room r)=>r switch
            {
                Room.Hatch=>new Color(.72f,.75f,.77f),Room.Crew=>new Color(.9f,.87f,.82f),Room.Stair=>new Color(.8f,.8f,.78f),Room.DeckFour or Room.DeckDoor=>new Color(.72f,.77f,.8f),
                Room.WorksDoor or Room.Works or Room.North=>new Color(.62f,.68f,.7f),Room.CommandHall or Room.Command=>new Color(.44f,.55f,.58f),_=>new Color(.78f,.8f,.82f),
            };
            /// <summary>Each deck's walk painted on its floor in a tone of its own (a ship that marks where its crews go), and the mess's floor warm under its tables: in the deck's
            /// vertices, so the ground kit's details and the lamps' pools still lie over them.</summary>
            static readonly (Vector2 a,Vector2 b,float half,Color tone)[] Bands=
            {
                (new(2.6f,9.8f),new(2.6f,48.6f),1.5f,new Color(.3f,.46f,.5f)),(new(0,60.4f),new(0,103.4f),1.6f,new Color(.56f,.64f,.68f)),
                (new(0,113.6f),new(-8,128),1.5f,new Color(1f,1f,.98f)),(new(-8,128),new(-8,158),1.5f,new Color(1f,1f,.98f)),(new(-8,158),new(0,170.4f),1.5f,new Color(1f,1f,.98f)),(new(-6.5f,155),new(17.6f,155),1.4f,new Color(.18f,.22f,.25f)),(new(8,13.8f),new(8,44.2f),1.3f,new Color(.42f,.16f,.1f)),
                (new(-2,196.4f),new(-2,250.6f),1.7f,new Color(.64f,.7f,.74f)),(new(13,19.6f),new(25,19.6f),.0f,new Color(.84f,.74f,.62f)),
            };
            static readonly (Vector2 min,Vector2 max,Color tone)[] Areas={(new(12.2f,15.6f),new(25.8f,40.4f),new Color(.84f,.75f,.64f)),(new(-17.4f,15.6f),new(-6.2f,42.4f),new Color(.8f,.8f,.84f))};
            /// <summary>Floors painted by what is done on them (the look pass of November 14: ten of thirty-seven stills were one plain tiled sheet, the works' open floor most of a frame).
            /// The works: a deep slate bay under the presses, the sluices' wet cyan-grey, the welding bay's dark steel, the chain's steel runway, the test stand's pale cell, the parts'
            /// staging and three laydown bays in slate, its walk painted pale (<see cref="Bands"/>). The crew deck: a rug of the Compact's teal under each mess table, the muster ground before
            /// the duty board. The command deck: the console well along its windows. In the deck's vertices, under every marking and every detail.</summary>
            static readonly (Vector2 min,Vector2 max,Color tone)[] Floors=
            {
                // (A vertex tone is read as linear: the works' slate written at .42 against the deck's .62 rendered a fifth darker and nobody could see it. Written here as the shader takes them.)
                (new(-25.4f,160.2f),new(-2.6f,170.8f),new Color(.15f,.2f,.22f)),(new(3.2f,161.4f),new(21.8f,170.8f),new Color(.1f,.27f,.33f)),(new(18.2f,117.6f),new(25.8f,162f),new Color(.1f,.11f,.13f)),
                (new(-3.6f,125.6f),new(3.6f,160.4f),new Color(.26f,.3f,.33f)),(new(7.2f,126.4f),new(13.6f,132.8f),new Color(.9f,.78f,.5f)),(new(-29.8f,114.2f),new(-23.2f,156.2f),new Color(.22f,.28f,.31f)),
                (new(-21f,129f),new(-12f,137.4f),new Color(.2f,.27f,.3f)),(new(-21f,141.4f),new(-12f,149.8f),new Color(.2f,.27f,.3f)),(new(5.6f,136.6f),new(14.6f,148.6f),new Color(.2f,.27f,.3f)),
                (new(4.4f,114.6f),new(13.6f,123.4f),new Color(.2f,.27f,.3f)),(new(-19.6f,114.6f),new(-12.4f,123.4f),new Color(.2f,.27f,.3f)),
                (new(12.4f,17.9f),new(25.6f,22.1f),new Color(.12f,.3f,.33f)),(new(12.4f,25.9f),new(25.6f,30.1f),new Color(.12f,.3f,.33f)),(new(12.4f,33.9f),new(25.6f,38.1f),new Color(.12f,.3f,.33f)),
                (new(5.4f,43.2f),new(13.6f,47.8f),new Color(.36f,.5f,.54f)),(new(-17.4f,46.4f),new(-4.4f,48.6f),new Color(.22f,.25f,.28f)),(new(-5.2f,33.6f),new(.4f,44.4f),new Color(.5f,.36f,.24f)),
                (new(-17.2f,22.3f),new(-6.4f,24.7f),new Color(.2f,.32f,.36f)),(new(-17.2f,33.3f),new(-6.4f,35.7f),new Color(.2f,.32f,.36f)),
                (new(25.2f,178.6f),new(31.4f,203.6f),new Color(.1f,.15f,.18f)),
            };
            static Color Tone(Vector2 p)
            {
                float clear=Nearest(p,out var room);float mottle=.95f+.08f*Mathf.PerlinNoise(p.x*.13f+3,p.y*.13f+7);var tone=DeckOf(room);
                int px=Mathf.FloorToInt(p.x*.5f+500),pz=Mathf.FloorToInt(p.y*.5f+500);float plate=H(px,pz);mottle*=.88f+.2f*plate;if(H(px+7,pz+3)<.18f)mottle*=.84f;
                foreach(var (min,max,area) in Areas){float out_=Mathf.Max(Mathf.Max(min.x-p.x,p.x-max.x),Mathf.Max(min.y-p.y,p.y-max.y));tone=Color.Lerp(tone,area,.9f*(1-ZoneKit.Smooth(-.2f,.2f,out_)));}
                foreach(var (min,max,floor) in Floors){float out_=Mathf.Max(Mathf.Max(min.x-p.x,p.x-max.x),Mathf.Max(min.y-p.y,p.y-max.y));tone=Color.Lerp(tone,floor,1-ZoneKit.Smooth(-.1f,.1f,out_));}
                foreach(var (a,b,half,band) in Bands){if(half<=0)continue;float d=ZoneKit.Segment(p,a,b);tone=Color.Lerp(tone,band,.85f*(1-ZoneKit.Smooth(half-.18f,half+.18f,d)));}
                tone=Color.Lerp(tone,tone*.74f,ZoneKit.Smooth(-1.4f,0,clear));
                return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
            }
            static Vector3 On(ZoneKit kit,Vector2 p,float up=0)=>kit.On(p.x,p.y,up);
            static Vector3 W(float x,float y,float z)=>new(x,y,z);

            // ---------------------------------------------------------------- what it is made of
            internal static Material FloorMat,KeelMat,PlateMat,WhiteMat,TealMat,GoldMat,DarkMat,LampTeal,LampWarm,LampWhite,HotMat,CoolMat,HullMat,ChartMat,BraceMat,FabricMat,ArcMat,SpaceMat;
            static readonly Color White=new(.95f,.96f,.97f),Pale=new(.84f,.87f,.89f),Teal=new(.18f,.56f,.58f),TealDeep=new(.1f,.34f,.38f),Gold=new(.86f,.7f,.38f),
                Iron=new(.3f,.32f,.35f),IronPale=new(.62f,.65f,.68f),Orange=new(.95f,.46f,.14f);
            static float H(int a,int b)=>Mathf.Repeat(Mathf.Sin(a*12.9898f+b*78.233f)*43758.5453f,1);
            static void Materials()
            {
                // The deck: plates two metres square, each a hair of its own tone, a fine tread over all, a bolt in each corner; one in six a grille, one in eight a round access
                // cover, one in eight with a strip let into it. Painted near neutral: each deck's colour is its vertices'.
                var deck=ZoneKit.Texture("Continuance/continuance-deck",512,(u,v)=>
                {
                    float x=u*4,y=v*4;int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);float fx=x-ix,fy=y-iy;
                    float own=H(ix,iy),kind=H(ix+7,iy+3),grain=ZoneKit.Cloud(u,v,20,5);
                    float tone=.72f+.06f*(own-.5f)+.03f*(grain-.5f);
                    float edge=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy));
                    if(kind<.05f){if(edge>.09f){tone=.6f;if(Mathf.Repeat(fy*14,1)<.36f&&Mathf.Abs(fx-.5f)<.36f)tone=.5f;}}
                    else if(kind<.1f){float r=new Vector2(fx-.5f,fy-.5f).magnitude;if(Mathf.Abs(r-.28f)<.014f)tone=.46f;else if(r<.28f)tone=.76f;if(r<.035f)tone=.5f;}
                    else if(kind<.2f){if(Mathf.Abs(fy-.5f)<.06f)tone=.6f;if(Mathf.Abs(fy-.5f)<.01f)tone=.5f;}
                    if(Mathf.Abs(Mathf.Repeat((fx+fy)*26,1)-.5f)<.05f)tone-=.012f;
                    if(new Vector2(Mathf.Min(fx,1-fx)-.06f,Mathf.Min(fy,1-fy)-.06f).magnitude<.02f)tone=.88f;
                    if(edge<.011f)tone=.32f;
                    return new Color(tone,tone,tone*1.01f,1);
                });
                // The hull's inner plating, behind every deck's own: pale panels with a rib every few metres, laid along each wall.
                var keel=ZoneKit.Texture("Continuance/continuance-keel",512,(u,v)=>
                {
                    float row=v*4;int course=Mathf.FloorToInt(row);float up=row-course;float across=u*4;int panel=Mathf.FloorToInt(across);float along=across-panel;
                    float own=H(panel+course*7,course),grain=ZoneKit.Cloud(u,v,16,9);
                    float tone=.74f+.06f*(own-.5f)+.04f*(grain-.5f);
                    if(up<.035f||along<.012f)tone=.42f;
                    if(up>.86f&&up<.92f)tone=.6f;
                    return new Color(tone*.98f,tone,tone*1.02f,1);
                });
                // The wall plate on each deck's walls: panels 1.2 m square, a fine seam, small fixings, and on every one, low on its right, the builder's signature: a name and a
                // date in the same neat hand ("Every panel is signed"), too small to read from the camera and there on every panel.
                var plate=ZoneKit.Texture("Continuance/continuance-plate",512,(u,v)=>
                {
                    float x=u*2,y=v*2;int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);float fx=x-ix,fy=y-iy;
                    float own=H(ix+3,iy+9),grain=ZoneKit.Cloud(u,v,18,7);
                    float tone=.86f+.04f*(own-.5f)+.03f*(grain-.5f);
                    float edge=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy));
                    if(edge<.03f&&edge>.018f&&new Vector2(Mathf.Repeat(fx*10,1)-.5f,Mathf.Repeat(fy*10,1)-.5f).magnitude<.18f)tone=.66f;
                    float sx=(fx-.66f)/.24f;if(sx>0&&sx<1&&Mathf.Abs(fy-.12f-.012f*Mathf.Sin(sx*40)-.006f*Mathf.Sin(sx*97))<.0045f)tone=.38f;
                    if(sx>0&&sx<.5f&&Mathf.Abs(fy-.085f)<.002f)tone=.5f;
                    if(own>.82f&&Mathf.Abs(fx-.3f)<.12f&&Mathf.Abs(fy-.78f)<.05f)tone=.62f;
                    if(edge<.008f)tone=.4f;
                    return new Color(tone,tone,tone,1);
                });
                // The hull from outside: plates four metres square, seams, a fine panel line, service hatches and small vents here and there.
                var hull=ZoneKit.Texture("Continuance/continuance-hull",512,(u,v)=>
                {
                    float x=u*2,y=v*2;int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);float fx=x-ix,fy=y-iy;
                    float own=H(ix+21,iy+5),kind=H(ix+2,iy+31),grain=ZoneKit.Cloud(u,v,14,3);
                    float tone=.86f+.05f*(own-.5f)+.04f*(grain-.5f);
                    if(Mathf.Abs(fx-.5f)<.004f||Mathf.Abs(fy-.5f)<.004f)tone=.7f;
                    if(kind<.25f&&Mathf.Abs(fx-.25f)<.1f&&Mathf.Abs(fy-.25f)<.07f){tone=.6f;if(Mathf.Repeat(fy*40,1)<.4f)tone=.45f;}
                    if(kind>.85f&&Mathf.Abs(Mathf.Max(Mathf.Abs(fx-.72f)/.12f,Mathf.Abs(fy-.7f)/.12f)-1)<.08f)tone=.55f;
                    float edge=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy));if(edge<.006f)tone=.46f;
                    return new Color(tone,tone,tone*1.01f,1);
                });
                FloorMat=ZoneKit.Toon("continuance-deck",deck,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_BaseColor",Color.white);m.SetFloat("_BaseMapStrength",.9f);m.SetColor("_ShadowTint",new Color(.56f,.6f,.7f));m.SetFloat("_LightFloor",.26f);});
                KeelMat=ZoneKit.Toon("continuance-keel",keel,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.86f,.88f,.9f));m.SetColor("_ShadowTint",new Color(.5f,.56f,.68f));m.SetFloat("_LightFloor",.26f);});
                PlateMat=ZoneKit.Toon("continuance-plate",plate,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.52f,.58f,.7f));m.SetColor("_RimColor",new Color(.44f,.46f,.5f));m.SetFloat("_LightFloor",.3f);});
                WhiteMat=ZoneKit.Toon("continuance-white",plate,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.98f,.98f,.99f));m.SetColor("_ShadowTint",new Color(.54f,.6f,.72f));m.SetFloat("_LightFloor",.32f);});
                TealMat=ZoneKit.Toon("continuance-teal",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.4f,.5f,.6f));m.SetFloat("_LightFloor",.3f);});
                GoldMat=ZoneKit.Toon("continuance-gold",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.5f,.46f,.5f));m.SetColor("_RimColor",new Color(.8f,.66f,.4f));m.SetFloat("_LightFloor",.32f);});
                DarkMat=ZoneKit.Toon("continuance-dark",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.42f,.46f,.58f));m.SetColor("_RimColor",new Color(.46f,.5f,.54f));m.SetFloat("_LightFloor",.24f);});
                HullMat=ZoneKit.Toon("continuance-hull",hull,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.5f,.56f,.7f));m.SetFloat("_LightFloor",.32f);m.SetFloat("_ReceiveShadows",0);});
                BraceMat=ZoneKit.Toon("continuance-brace",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.55f,.42f,.4f));m.SetFloat("_LightFloor",.3f);});
                FabricMat=ZoneKit.Toon("continuance-fabric",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.5f,.54f,.64f));m.SetFloat("_LightFloor",.28f);});
                // Light fittings: the Compact's teal, the mess's warm, the works' and the bay's white; the press bed's heat and the coolant's cyan; the weld's arc.
                LampTeal=ZoneKit.Toon("continuance-lamp-teal",null,m=>{m.SetColor("_BaseColor",new Color(.6f,.96f,1));m.SetColor("_EmissionColor",new Color(.26f,1.4f,1.5f));});
                LampWarm=ZoneKit.Toon("continuance-lamp-warm",null,m=>{m.SetColor("_BaseColor",new Color(1,.92f,.78f));m.SetColor("_EmissionColor",new Color(1.7f,1.3f,.8f));});
                LampWhite=ZoneKit.Toon("continuance-lamp-white",null,m=>{m.SetColor("_BaseColor",new Color(.96f,.98f,1));m.SetColor("_EmissionColor",new Color(1.5f,1.55f,1.6f));});
                HotMat=ZoneKit.Toon("continuance-hot",null,m=>{m.SetColor("_BaseColor",new Color(.86f,.34f,.14f));m.SetColor("_EmissionColor",new Color(1.25f,.34f,.07f));});
                CoolMat=ZoneKit.Toon("continuance-coolant",null,m=>{m.SetColor("_BaseColor",new Color(.34f,.78f,.86f));m.SetColor("_EmissionColor",new Color(.08f,.62f,.78f));});
                ArcMat=ZoneKit.Toon("continuance-arc",null,m=>{m.SetColor("_BaseColor",new Color(.86f,.95f,1));m.SetColor("_EmissionColor",new Color(2.2f,2.6f,3f));});
                ChartMat=ZoneKit.Toon("continuance-chart",ChartTexture(),m=>{m.SetColor("_BaseColor",new Color(.4f,.7f,.74f));m.SetColor("_EmissionColor",new Color(.4f,1.2f,1.3f));m.SetTexture("_EmissionMap",m.GetTexture("_BaseMap"));});
                SpaceMat=ZoneKit.Toon("continuance-void",null,m=>{m.SetColor("_BaseColor",new Color(.03f,.05f,.08f));m.SetColor("_EmissionColor",Color.black);m.SetFloat("_LightFloor",0);});
                // Every material a fading piece is made of carries the fade's variant from its paint, so no later paint of it can take it away (D359: these drew their pieces solid while the rest faded).
                foreach(var fadeMat in new[]{HotMat,ArcMat,CoolMat,LampWarm,SpaceMat}){ZoneKit.With(fadeMat,"_OCCFADE_ON","_OccFadeToggle");fadeMat.SetFloat("_OccFade",0);}
                AssetDatabase.SaveAssets();
            }
            /// <summary>The director's chart: the road on a dark glass, the convoy as a string of lit beads along it, the anchor as a ring where the road narrows, the number in the corner.</summary>
            static Texture2D ChartTexture()=>ZoneKit.Texture("Continuance/continuance-chart",512,(u,v)=>
            {
                float x=u-.5f,y=v-.5f;float tone=.08f+.04f*ZoneKit.Cloud(u,v,8,4);
                float road=Mathf.Abs(y-.18f*Mathf.Sin(x*5.2f));float wide=.06f+.08f*Mathf.Abs(x);
                if(road<wide)tone=.18f;if(Mathf.Abs(road-wide)<.006f)tone=.62f;
                for(int k=0;k<14;k++){float bx=-.42f+k*.05f;float by=.18f*Mathf.Sin(bx*5.2f);if(new Vector2(x-bx,y-by).magnitude<.011f)tone=.95f;}
                var anchor=new Vector2(.3f,.18f*Mathf.Sin(.3f*5.2f));float r=new Vector2(x,y).magnitude;float a=(new Vector2(x,y)-anchor).magnitude;
                if(Mathf.Abs(a-.05f)<.008f)tone=1;if(a<.012f)tone=.9f;
                if(Mathf.Abs(r-.47f)<.004f)tone=.5f;
                if(Mathf.Abs(Mathf.Repeat(u*10,1)-.5f)<.004f||Mathf.Abs(Mathf.Repeat(v*10,1)-.5f)<.004f)tone=Mathf.Max(tone,.2f);
                return new Color(tone,tone,tone,1);
            });

            // ---------------------------------------------------------------- the Compact's hand: words painted on the plate
            static readonly Dictionary<char,string> Glyphs=new()
            {
                ['A']=".###.#...##...#######...##...##...#",['B']="####.#...##...#####.#...##...#####.",['C']=".#####....#....#....#....#.....####",['D']="####.#...##...##...##...##...#####.",
                ['E']="######....#....####.#....#....#####",['F']="######....#....####.#....#....#....",['G']=".#####....#....#..###...##...#.####",['H']="#...##...##...#######...##...##...#",
                ['I']="#####..#....#....#....#....#..#####",['J']="..###...#....#....#....#.#..#..##..",['K']="#...##..#.#.#..##...#.#..#..#.#...#",['L']="#....#....#....#....#....#....#####",
                ['M']="#...###.###.#.##...##...##...##...#",['N']="#...###..##.#.##..###...##...##...#",['O']=".###.#...##...##...##...##...#.###.",['P']="####.#...##...#####.#....#....#....",
                ['R']="####.#...##...#####.#.#..#..#.#...#",['S']=".#####....#.....###.....#....#####.",['T']="#####..#....#....#....#....#....#..",['U']="#...##...##...##...##...##...#.###.",
                ['V']="#...##...##...##...##...#.#.#...#..",['W']="#...##...##...##.#.##.#.###.###...#",['Y']="#...##...#.#.#...#....#....#....#..",
                ['0']=".###.#..###.#.##.#.##.#.###..#.###.",['1']="..#...##....#....#....#....#...###.",['2']=".###.#...#....#...#...#...#...#####",['3']="####.....#....#.###.....#....#####.",
                ['4']="...#...##..#.#.#..#.#####...#....#.",['5']="######....####.....#....##...#.###.",['6']=".###.#....#....####.#...##...#.###.",['7']="#####....#...#...#...#....#....#...",
                ['8']=".###.#...##...#.###.#...##...#.###.",['9']=".###.#...##...#.####....#....#.###.",[' ']=new string('.',35),['-']=new string('.',15)+"#####"+new string('.',15),
            };
            /// <summary>A painted word: the text in a band across a square texture, each stroke a rounded cell (a neat hand, not a stencil a century old); returns the band's v range.</summary>
            static Texture2D Stencil(string file,string text,out float v0,out float v1)
            {
                int cols=text.Length;float aspect=9f/(cols*6);float b0=.5f-aspect*.5f,b1=.5f+aspect*.5f;v0=b0;v1=b1;
                return ZoneKit.Texture("Continuance/"+file,512,(u,v)=>
                {
                    if(v<b0||v>b1)return new Color(1,1,1,0);
                    float gx=u*cols*6,gy=(v-b0)/(b1-b0)*9;int c=Mathf.Clamp(Mathf.FloorToInt(gx/6),0,cols-1);float lx=gx-c*6,ly=9-gy;
                    int px=Mathf.FloorToInt(lx),py=Mathf.FloorToInt(ly);
                    if(px>4||py<1||py>7||!Glyphs.TryGetValue(text[c],out var g))return new Color(1,1,1,0);
                    bool on=g[(py-1)*5+px]=='#';if(!on)return new Color(1,1,1,0);
                    float cx=lx-px-.5f,cy=ly-py-.5f;bool inside=Mathf.Max(Mathf.Abs(cx),Mathf.Abs(cy))<.42f||new Vector2(cx,cy).magnitude<.5f;
                    return new Color(1,1,1,inside?1:0);
                },false,true);
            }
            /// <summary>A painted word on a wall, a floor or the hull: a quad of its own cut out of the stencil, tinted to the paint.</summary>
            static GameObject Painted(ZoneKit kit,string name,string text,Vector3 centre,Vector3 right,Vector3 facing,float width,Color paint,bool glows=false)
            {
                string slug=name.ToLowerInvariant().Replace(' ','-').Replace("'","");
                var map=Stencil("paint-"+slug,text,out float v0,out float v1);
                var mat=ZoneKit.Toon("continuance-paint-"+slug,map,m=>{ZoneKit.With(m,"_ALPHATEST_ON","_AlphaTest");ZoneKit.With(m,"_OCCFADE_ON","_OccFadeToggle");m.SetFloat("_OccFade",0);m.SetFloat("_Cutoff",.5f);m.SetColor("_BaseColor",paint);m.SetFloat("_LightFloor",.3f);
                    if(glows){m.SetColor("_EmissionColor",new Color(paint.r*1.4f,paint.g*1.4f,paint.b*1.4f));m.SetTexture("_EmissionMap",map);}});
                float tall=width*(v1-v0);right.Normalize();var up=Vector3.Cross(facing.normalized,right).normalized;if(Vector3.Dot(up,Vector3.up)<-.01f||Mathf.Abs(Vector3.Dot(facing.normalized,Vector3.up))>.9f&&Vector3.Dot(up,Vector3.forward)<0)up=-up;
                var at=centre+facing.normalized*.03f;
                var mesh=new Mesh{name="Continuance paint "+name};
                mesh.SetVertices(new List<Vector3>{at-right*width*.5f-up*tall*.5f,at+right*width*.5f-up*tall*.5f,at+right*width*.5f+up*tall*.5f,at-right*width*.5f+up*tall*.5f});
                mesh.SetUVs(0,new List<Vector2>{new(0,v0),new(1,v0),new(1,v1),new(0,v1)});
                mesh.SetNormals(Enumerable.Repeat(facing.normalized,4).ToList());
                bool front=Vector3.Dot(Vector3.Cross(right,up),facing)>=0;mesh.SetTriangles(front?new[]{0,1,2,0,2,3}:new[]{0,2,1,0,3,2},0);mesh.RecalculateBounds();
                var saved=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-paint-"+slug+".asset");
                var go=new GameObject("Painted "+name,typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=saved;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;
                return go;
            }

            // ---------------------------------------------------------------- pieces
            /// <summary>A generated model stood as scenery (no brain, no collider), fitted to a height or a length, its feet at a point of the world.</summary>
            static GameObject Model(string folder,string id,Vector3 foot,float size,float yaw,bool byLength=false,string name=null)
            {
                var prefab=ZoneKit.Prefab(folder,id);if(prefab==null){Debug.LogWarning("CONTINUANCE_NO_MODEL "+folder+"/"+id);return null;}
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(body,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var c in body.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                foreach(var b in body.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);
                foreach(var a in body.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);
                body.transform.rotation=Quaternion.Euler(0,yaw,0);var box=ModelGeometry.BoundsOf(body);
                float span=byLength?Mathf.Max(box.size.x,box.size.z):box.size.y;body.transform.localScale*=size/Mathf.Max(.01f,span);
                box=ModelGeometry.BoundsOf(body);body.transform.position+=foot-new Vector3(box.center.x,box.min.y,box.center.z);body.name=name??id;
                return body;
            }
            /// <summary>Somebody with no lines: a body in a static box (all the bake reads), tinted so nobody takes one for the person whose body it borrows. A Compact crew's body is the
            /// Warden's own (the Compact's crews are chapter 9's), unarmed and drawn only. Moving people (a <c>Guide</c>, a <c>LeavesAfter</c>, a walker) stand in a capsule instead.</summary>
            static GameObject Body(ZoneKit kit,string name,string folder,string id,Vector3 at,float yaw,Color tint,float scale=1,bool still=true,string fallbackFolder="Characters",string fallback="Neve")
            {
                var prefab=ZoneKit.Prefab(folder,id)??ZoneKit.Prefab(fallbackFolder,fallback);if(prefab==null)return null;
                var person=new GameObject(name);person.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
                var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);model.transform.SetParent(person.transform,false);model.name="Generated civilian";model.transform.localScale*=scale;
                if(folder=="Enemies"){foreach(var c in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);foreach(var b in model.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);}
                foreach(var rig in model.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullCompletely;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
                if(still){person.isStatic=true;var box=person.AddComponent<BoxCollider>();box.size=new Vector3(.7f,1.9f,.7f);box.center=Vector3.up*.95f;}
                else{var cap=person.AddComponent<CapsuleCollider>();cap.height=1.9f;cap.radius=.35f;cap.center=Vector3.up*.95f;}
                person.AddComponent<PropTint>().tint=tint;
                return person;
            }
            static readonly Color[] CrewTints={new(1,1,1),new(.9f,.96f,1),new(1,.97f,.92f),new(.86f,.92f,.94f),new(.96f,.94f,1)};
            /// <summary>A crew in a static box. The Warden's body is for the Compact's guards (the captain's detail); a crew at work wears the Compact's own coat (<c>PatrolBody</c>, the
            /// grade of a Vael coat in its white and teal) or a hand's suit, and a crew off watch their own clothes (the civilians other zones use). The look pass of November 14 found
            /// every crew aboard in the Warden's body.</summary>
            static GameObject Crew(ZoneKit kit,string name,Vector2 at,float yaw,int k,bool still=true,string body="CompactWarden")=>
                Body(kit,name,body=="CompactWarden"?"Enemies":"Characters",body,On(kit,at),yaw,body=="CompactWarden"?CrewTints[k%CrewTints.Length]:Coats[k%Coats.Length],1,still,"Characters","Neve");
            /// <summary>Tints for a crew in their own body: near white, each a little apart, so two in the same coat across a deck aren't twins.</summary>
            internal static readonly Color[] Coats={new(.96f,.98f,1),new(.88f,.96f,1),new(1,.96f,.9f),new(.92f,.94f,.98f),new(.98f,.93f,.94f)};
            /// <summary>Somebody drawn only, where nobody walks into them: inside a table's or a bunk row's block, behind a counter, at a machine against its block, behind the welding line, at
            /// the windows' edge of a deck. The look pass put people aboard without making anything solid, so every collider, way and fight stays as the routes and the fights read them.
            /// <paramref name="pose"/>: <c>sit</c> (on a seat <paramref name="seat"/> high, leaning to the table), <c>bend</c> (over a bench or a machine), <c>hand</c> (an arm up), <c>lie</c>
            /// (asleep on a berth: the body laid along -x at <paramref name="at"/>, its feet there), or none (standing in its idle).</summary>
            internal static GameObject Figure(ZoneKit kit,string name,string id,Vector3 at,float yaw,Color tint,string pose=null,float seat=.48f,Transform parent=null)
            {
                var who=Body(kit,name,"Characters",id,at,yaw,tint,1,false,"Characters","Neve");if(who==null)return null;
                foreach(var c in who.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);
                if(pose!=null)Hold(who,pose,seat);
                if(pose=="lie")who.transform.rotation=Quaternion.LookRotation(Vector3.up,Vector3.left);
                if(parent!=null)who.transform.SetParent(parent,true);
                return who;
            }
            /// <summary>Holds a body in a pose no clip has, measured on the body itself from its own idle and kept by <see cref="Posed"/> (as HomeBuilder's townsfolk are seated).</summary>
            static void Hold(GameObject person,string pose,float seat)
            {
                var rig=person.GetComponentInChildren<Animator>(true);if(rig==null||!rig.isHuman||rig.avatar==null)return;
                var clips=rig.runtimeAnimatorController!=null?rig.runtimeAnimatorController.animationClips:Array.Empty<AnimationClip>();
                var idle=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle"));
                using var handler=new HumanPoseHandler(rig.avatar,rig.transform);
                var right=person.transform.right;var forward=person.transform.forward;float floor=person.transform.position.y;
                void Turn(HumanBodyBones bone,Vector3 axis,float degrees){var t=rig.GetBoneTransform(bone);if(t!=null)t.rotation=Quaternion.AngleAxis(degrees,axis)*t.rotation;}
                if(idle!=null)idle.SampleAnimation(rig.gameObject,.3f);
                if(pose=="sit")
                {
                    foreach(var (thigh,shin) in new[]{(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg),(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg)}){Turn(thigh,right,-84);Turn(shin,right,86);}
                    var hips=rig.GetBoneTransform(HumanBodyBones.Hips);if(hips!=null)hips.position=new Vector3(hips.position.x,floor+seat+.1f,hips.position.z)-forward*.12f;
                    Turn(HumanBodyBones.Spine,right,14);Turn(HumanBodyBones.Head,right,12);
                    foreach(var (upper,lower) in new[]{(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm),(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm)}){Turn(upper,right,-38);Turn(lower,right,-52);}
                }
                if(pose=="bend"){Turn(HumanBodyBones.Spine,right,20);Turn(HumanBodyBones.Chest,right,16);Turn(HumanBodyBones.Neck,right,10);Turn(HumanBodyBones.Head,right,18);
                    foreach(var upper in new[]{HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm})Turn(upper,right,-30);}
                if(pose=="hand"){Turn(HumanBodyBones.RightUpperArm,forward,150);Turn(HumanBodyBones.RightLowerArm,forward,10);}
                var hp=new HumanPose();handler.GetHumanPose(ref hp);
                var posed=person.AddComponent<Posed>();posed.poses=new[]{new Posed.Pose{name=pose,body=hp.bodyPosition,turn=hp.bodyRotation,muscles=hp.muscles.ToArray()}};posed.current=pose;
                handler.SetHumanPose(ref hp);
                foreach(var animated in person.GetComponentsInChildren<GeneratedAnimator>(true))animated.enabled=false;
                rig.enabled=false;rig.runtimeAnimatorController=null;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);
            }
            static RepairBay Cradle(ZoneKit kit,string name,Vector2 at,float yaw,string spawn)
            {
                var bay=kit.Prop("DeckConsole",new Vector3(at.x,0,at.y),1.4f,yaw).AddComponent<RepairBay>();bay.free=true;bay.saveSpawn=spawn;bay.prompt=name+" — repair and save";
                bay.gameObject.AddComponent<PropTint>().tint=new Color(.96f,1,1);
                ChapterOneStaging.CradleBay(name,bay.gameObject,On(kit,at),kit.Y(at.x,at.y)+.02f,false,false);return bay;
            }
            static void Gate(string name,string flag,bool showWhenSet,params GameObject[] targets)
            {var gate=new GameObject(name,typeof(FlagGate)).GetComponent<FlagGate>();gate.flag=flag;gate.showWhenSet=showWhenSet;gate.targets=targets.Where(t=>t!=null).ToArray();}
            static FirstSight Sight(string situation,Vector3 at,float range,string once,string needs="")
            {var s=new GameObject("First sight: "+situation+" ("+once+")",typeof(FirstSight)).GetComponent<FirstSight>();s.transform.position=at;s.situation=situation;s.range=range;s.onceFlag=once;s.requiredFlag=needs;return s;}
            /// <summary>Stands a built shape with as many material slots as it is given materials: a mesh with fewer submeshes than materials draws its last one again in each spare
            /// material (a teal band z-fighting with gold).</summary>
            static GameObject Made(ZoneKit kit,string name,ZoneKit.Shape shape,params Material[] materials){shape.Slot(materials.Length-1);shape.Slot(0);return kit.Built(name,shape,materials);}
            /// <summary>How high the wall stands over a floor at <paramref name="level"/>, a metre and a half behind a point on its face: what a fixture hung there may not rise above.</summary>
            static float WallTop(Vector2 at,Vector2 outward,float level)=>Seen(at+outward*1.5f)-level;
            static Light PointLight(Vector3 at,float range,float intensity,Color colour,string name="Lamp light")
            {var go=new GameObject(name,typeof(Light));go.transform.position=at;var l=go.GetComponent<Light>();l.type=LightType.Point;l.color=colour;l.range=range;l.intensity=intensity;l.shadows=LightShadows.None;return l;}
            /// <summary>A seal across a door, shut through a fight: two Compact door leaves meeting in the middle in a teal-lined frame, drawn only while shut, with a carving obstacle.</summary>
            static Membrane Seal(ZoneKit kit,string name,Vector2 at,float width,bool open,float tall=3.8f)
            {
                var seal=WorldBuilder.Membrane(name,On(kit,at,tall*.5f),width,true,tall);
                foreach(var filter in seal.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(filter);
                foreach(var view in seal.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(view);
                var box=seal.GetComponent<BoxCollider>();var carve=seal.gameObject.AddComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.center=box.center;carve.size=box.size;carve.carving=true;carve.carveOnlyStationary=false;carve.enabled=false;
                var s=new ZoneKit.Shape();var foot=On(kit,at);
                foreach(int side in new[]{-1,1})
                {
                    s.Slot(0).Box(foot+new Vector3(side*width*.25f,tall*.5f,0),new Vector3(width*.5f-.06f,tall,.22f),Quaternion.identity,Pale,White);
                    s.Slot(1).Box(foot+new Vector3(side*width*.25f,tall*.55f,-.12f),new Vector3(width*.5f-.5f,.12f,.04f),Quaternion.identity,Teal);
                    s.Slot(1).Box(foot+new Vector3(side*.12f,tall*.5f,-.12f),new Vector3(.06f,tall-.4f,.04f),Quaternion.identity,TealDeep);
                }
                var drawn=Made(kit,name+" leaves",s,WhiteMat,TealMat);drawn.transform.SetParent(seal.transform,true);
                seal.SetOpen(open);
                return seal;
            }
            /// <summary>A fight on the deck: the first pack where WorldBuilder puts it, each further pack its own spawner, all faced as placed.</summary>
            static EncounterVolume Fight(ZoneKit kit,string id,Vector2 at,Vector2 size,string requiredFlag,Membrane[] seals,params (string enemy,string standIn,int count,Vector2 from,float yaw,float radius)[] packs)
            {
                var encounter=WorldBuilder.Encounter(id,kit.On(at.x,at.y,3.5f),new Vector3(size.x,7,size.y),Foe(packs[0].enemy,packs[0].standIn),packs[0].count,seals);
                var spawners=new List<Spawner>{encounter.spawners[0]};
                for(int i=1;i<packs.Length;i++)spawners.Add(ZoneKit.Spawn(encounter,Foe(packs[i].enemy,packs[i].standIn),packs[i].count,Vector3.zero,packs[i].radius));
                for(int i=0;i<packs.Length;i++)
                {
                    var spawner=spawners[i];spawner.transform.SetPositionAndRotation(On(kit,packs[i].from),Quaternion.Euler(0,packs[i].yaw,0));
                    spawner.faceAsPlaced=true;spawner.radius=packs[i].count>1?packs[i].radius:0;
                }
                encounter.spawners=spawners.ToArray();encounter.requiredFlag=requiredFlag??"";return encounter;
            }

            // ---------------------------------------------------------------- the zone
            /// <summary>Builds the Continuance.</summary>
            internal static bool Build()
            {
                Materials();
                var kit=ZoneKit.Ground(ContinuanceZone,Height,1.2f);
                var scene=WorldBuilder.Begin(ContinuanceZone);
                // The key from high on the starboard quarter: through the crew deck's and the command deck's windows, across the spine, its shadows falling toward the camera's side.
                var key=GameObject.Find("KeyLight");if(key!=null)key.transform.rotation=Quaternion.Euler(54,236,0);
                var land=kit.Terrain("Continuance decks",-40,EdgeX,HullFace,NorthEdge,.5f,FloorMat,KeelMat,Tone,new Color(.92f,.94f,.96f),new Color(.66f,.7f,.74f),Seen,p=>-FloorAt(p,out _));
                Skins(kit);Windows(kit);

                // ---- spawns
                var arrival=WorldBuilder.Spawn("Arrival",Arrival);arrival.transform.rotation=Quaternion.identity;
                var spine=WorldBuilder.Spawn(SpineSpawn,Arrival);spine.transform.rotation=Quaternion.identity;
                WorldBuilder.Spawn(DeckFourSpawn,DeckFourAt);
                var bay=WorldBuilder.Spawn(BaySpawn,LaunchBayAt);bay.transform.rotation=Quaternion.identity;

                // ---- the boarding spine, the hull's flank, the way home
                var spineRoot=Spine(kit);HullFlank(kit);
                Cradle(kit,"Spine cradle",SpineCradleAt,90,SpineSpawn);
                var home=kit.Prop("LandingPad",new Vector3(HomePadAt.x,0,HomePadAt.y),4.6f,0);home.name="Launch — Last Mooring";
                var homePad=home.AddComponent<DockingPad>();homePad.prompt="Launch — Last Mooring";homePad.range=4;homePad.scene=MooringZone;homePad.spawn=MooringBack;
                var homeSign=WorldBuilder.Label("Launch — Last Mooring",On(kit,HomePadAt+new Vector2(0,4),.3f),2.2f);
                // Idra on the radio as the party starts up (when she isn't with them; wiring 2, manifest 7), once, before the top.
                var spineHail=new GameObject("Idra's crews on the radio",typeof(RadioHail)).GetComponent<RadioHail>();spineHail.transform.position=Arrival;
                spineHail.speaker="Idra";spineHail.situations=new[]{"hail.spine","hail.spine"};spineHail.heardFlag="end.spineHailed";spineHail.silencedBy="end.spineTop";spineHail.range=24;
                // The spine's top: Idra's line, the main quest's step 5 arrow.
                var top=kit.Crossing("Idra's crews brace the spine",new Vector3(SpineTopAt.x,0,SpineTopAt.y),new Vector3(8,4,3),"end.spineTop","IdraSpineBraced","Idra");top.report="Spine";
                kit.Sign("BOARDING SPINE",0,-72.6f,1.9f);   // Two metres in from the deck's edge: it lay half over the dark beyond it, 2.4 to 1 (D349).

                // ---- the hatch and the crew deck: a fight, crews who stand aside, one who holds the hatch
                Hatch(kit);CrewDeck(kit);
                Fight(kit,MessFight,new Vector2(12,30),new Vector2(34,36),"end.boarded",null,("CompactWarden","MusterPikeguard",2,new Vector2(19,32),200,2.2f));
                kit.Sign("CREW DECK",0,12.4f,1.8f);

                // ---- maintenance deck four: the captain of the Steady Hand and his crew across the way
                DeckFour(kit);
                Cradle(kit,"Deck four cradle",DeckFourCradleAt,0,DeckFourSpawn);
                kit.Sign("MAINTENANCE DECK FOUR",0,63.6f,1.8f);
                kit.Crossing("The captain of the Steady Hand",new Vector3(CaptainCrossingAt.x,0,CaptainCrossingAt.y),new Vector3(48,5,3),"end.captainPassed","PatrolLetsPass","Patrol");
                Captain(kit);
                var aside=new GameObject("The captain to his crews",typeof(RadioHail)).GetComponent<RadioHail>();aside.transform.position=On(kit,CaptainAt);
                aside.speaker="Patrol";aside.situations=new[]{"hail.aside"};aside.needs=new[]{"end.captainPassed"};aside.heardFlag="end.asideHeard";aside.range=30;

                // ---- the anchor works: its fight seals behind the partner, its north door is the last before the bay
                var south=Seal(kit,"Works south door",WorksSouthDoor,6.2f,true);var north=Seal(kit,"Works north door",WorksNorthDoor,6.2f,false);
                AnchorWorks(kit);
                Fight(kit,WorksFight,new Vector2(-2,142.5f),new Vector2(52,54),null,new[]{south,north},("CompactWarden","MusterPikeguard",2,new Vector2(-6,152),180,2.4f),
                    ("WeldArm","MusterExcavator",1,new Vector2(16,158),220,0),("OverrideRig","SnagvineNode",1,new Vector2(-20,158),160,0));
                kit.Sign("ANCHOR WORKS",0,116.6f,1.9f);

                // ---- the command deck (the captain's deck the director kept) and the launch bay
                CommandDeck(kit);kit.Sign("COMMAND DECK",19.2f,179.6f,1.6f);
                LaunchBay(kit);kit.Sign("LAUNCH BAY",-4,199.6f,1.8f);
                Cradle(kit,"Launch bay cradle",BayCradleAt,-90,BaySpawn);
                Aboard(kit);

                // ---- the flight berth over the hull's back (wiring 6.1.1): the bay's empty cradle is its pad
                Hull(kit);
                // The berth is her fight's own (inFight): its launch answers through her fight, as every launch does, so its pad must too, or a party that launched back into the bay
                // mid-fight was refused the way back up for as long as her craft fought within reach of the bay, which is all of her fight.
                Berth.Declare(HullBerth,"After the director's craft",ZoneKind.SpaceCombat,HullBerthBox,HullBerthPad,HullBerthLaunch,HullBerthFoot,HullBerthFlight,BaySpawn,LaunchBayAt,inFight:true,refuseWhileAwake:true);
                var hail=kit.Crossing("Meret hails them from her craft",new Vector3(HailAt.x,0,HailAt.z),HailSize,"end.meretHailed","MeretShipToShip","Meret");
                hail.transform.position=HailAt;hail.GetComponent<BoxCollider>().center=Vector3.zero;
                var door=WorldBuilder.Encounter(MeretDoor,DoorCentre,DoorSize,Foe("MeretShip","Interceptor"),1);
                var post=door.spawners[0];post.transform.SetPositionAndRotation(MeretAt,Quaternion.Euler(0,180,0));post.faceAsPlaced=true;post.radius=0;
                door.requiredFlag="end.meretHailed";door.checkpointSpawn=BaySpawn;
                // Her door holds the works' north door, the only way from the bay back into the ship, from her fight's start to her craft's fall (it is the works' seal too, open
                // since their crews went down). Mid-fight the bay's launch puts a party on foot under her, and the pad home is 300 m south of the reach a boss fight holds a pad in:
                // without this they walked out of her fight and away (QaPlanSystemsTests.NoWayOutAnswersInEveryLaterBossFight).
                door.membranes=new[]{north};
                var show=new GameObject("Her craft on show").transform;show.SetParent(door.transform,false);show.position=HailAt;door.previewPoint=show;door.previewRadius=Vector3.Distance(HailAt,MeretAt)+3;
                var exit=new GameObject("Her craft's way out",typeof(BossExit)).GetComponent<BossExit>();exit.transform.position=MeretExit;exit.boss="MeretShip";
                var gantryTalk=new GameObject("Meret speaks from the gantry",typeof(AfterTalk)).GetComponent<AfterTalk>();gantryTalk.transform.position=MeretAtRail;
                gantryTalk.flag="bossdown.MeretShip";gantryTalk.node="MeretGantry";gantryTalk.speaker="Meret";gantryTalk.heardFlag="end.meretShip";gantryTalk.delay=2.5f;gantryTalk.range=0;
                var gantry=new GameObject(GantryWords,typeof(DockingPad));gantry.transform.position=GantryDock;
                var gantryPad=gantry.GetComponent<DockingPad>();gantryPad.prompt=GantryWords;gantryPad.range=WorldBuilder.FlightDockRange;gantryPad.scene=ChamberZone;gantryPad.spawn=GantrySpawn;
                var gantrySign=WorldBuilder.Label(GantryWords,GantryDock+new Vector3(0,-.7f,-5),2.2f);
                // Her own body standing at the rail (drawn only: a ship flying past the walkway meets nothing of her).
                var meret=Body(kit,"Meret at the gantry's rail","Characters","MeretBody",MeretAtRail,200,Color.white,1,false,"Characters","MeretBody");
                if(meret!=null)foreach(var c in meret.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);
                Gate("Until her craft is beaten: the way home",AwayFlag,false,home,homeSign);
                Gate("Once she has spoken from the gantry: the dock to it",AwayFlag,true,gantry,gantrySign);
                Gate("Once her craft is down: Meret at the rail",BossDownShip,true,meret);
                if(meret!=null)meret.SetActive(false);gantry.SetActive(false);gantrySign.SetActive(false);

                // ---- navigation: every seal open (what is shut at first is play's business)
                var reach=new List<Vector2>{ArrivalAt,ArrivalAt+new Vector2(2,0),HomePadAt+new Vector2(-1.6f,2.2f),SpineCradleAt+new Vector2(1.6f,0),new(0,-60),new(0,-40),new(0,-20),SpineTopAt,new(0,-4),new(0,6),
                    new(-12,14),new(-12,44),new(8,24),new(28,16),new(28,44),new(9,40),HelperAt+new Vector2(-1.6f,0),new(0,54),new(0,62),DeckFourCradleAt+new Vector2(0,1.6f),new(-20,96),new(20,66),new(20,98),
                    CaptainAt+new Vector2(0,-3),CaptainAside,EastDoorAt+new Vector2(-1,0),new(0,108),new(0,118),new(-24,118),new(20,118),new(-24,150),new(18,140),new(-2,164),new(14,162),WorksNorthDoor+new Vector2(0,2),
                    new(0,186),new(10,185.5f),ChartAt+new Vector2(-3.2f,0),new(28,180),CacheAt+new Vector2(-1.6f,0),new(-6,200),new(-24,222),new(8,222),BayCradleAt+new Vector2(-1.6f,0),PadAt,FootAt,
                    new(LaunchBayAt.x,LaunchBayAt.z),new(-10,246),new(6,248)};
                var shut=new List<GameObject>{north.gameObject};foreach(var g in shut)g.SetActive(false);
                kit.BakeNavigation(new Bounds(new Vector3(-4,-2,83),new Vector3(84,24,344)),Arrival,reach.Select(p=>On(kit,p)).ToArray());
                foreach(var g in shut)g.SetActive(true);
                Ground(kit,land);
                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("MOORING_COORDS Continuance arrival="+Arrival+" home="+HomePadAt+" spine-top="+SpineTopAt+" captain="+CaptainAt+" works="+WorksSouthDoor+"/"+WorksNorthDoor+" bay="+LaunchBayAt
                    +" pad="+HullBerthPad+" launch="+HullBerthLaunch+" flight="+HullBerthFlight+" box="+HullBerthBox+" hail="+HailAt+" door="+DoorCentre+"/"+DoorSize+" meret="+MeretAt+" gantry="+GantryDock+" exit="+MeretExit);
                Debug.Log("CONTINUANCE_OK");
                return true;
            }
            const string AwayFlag="end.meretShip",BossDownShip="bossdown.MeretShip";
            /// <summary>The way to the frame gantry names the zone it leads to, as every way to a later zone does (QaPlanWorldTests: "Anchor Chamber").</summary>
            internal const string GantryWords="Dock — The Anchor Chamber's frame gantry";

            // ---------------------------------------------------------------- the spine
            /// <summary>The boarding spine: a gangway from the docking platform at its foot up to the hull's flank, its white deck laid in plates between the rings (U-frames of the tube
            /// whose glass is gone), rails a body can't pass, the two rings Idra's crews hold open with orange jacks, a lamp at each, Kott forward and Sibb aft; the convoy below.</summary>
            static GameObject Spine(ZoneKit kit)
            {
                var root=new GameObject("The boarding spine");var s=new ZoneKit.Shape();var trim=new ZoneKit.Shape();var glow=new ZoneKit.Shape();
                float angle=Mathf.Atan2(Up-Foot,SpineHead-SpineFoot)*Mathf.Rad2Deg;var slope=Quaternion.Euler(-angle,0,0);
                // The docking platform at the foot, the landing at the top.
                s.Slot(0).Box(W(0,Foot-.25f,-77),new Vector3(14,.5f,18),Quaternion.identity,Pale,White,.25f);
                // The landing and the gangway in the hull's pale grey, not paper white (the look pass of November 21: s66/013 read flat and over-bright), the plates alternating a shade.
                s.Slot(0).Box(W(0,Up-.25f,-12),new Vector3(8,.5f,8),Quaternion.identity,Pale,new Color(.7f,.73f,.76f),.25f);
                // The gangway in plates, a seam between each.
                for(float z=SpineFoot;z<SpineHead-.01f;z+=4)
                {
                    float z1=Mathf.Min(z+4,SpineHead),mid=(z+z1)*.5f;
                    s.Slot(0).Box(W(0,SpineY(mid)-.2f,mid),new Vector3(SpineHalf*2,.4f,z1-z-.08f),slope,Pale,Mathf.RoundToInt((z-SpineFoot)/4)%2==0?new Color(.74f,.77f,.8f):new Color(.66f,.7f,.73f),.25f);
                    trim.Slot(0).Box(W(0,SpineY(mid)+.005f,mid),new Vector3(.5f,.02f,z1-z-.6f),slope,Teal);
                    foreach(int side in new[]{-1,1})glow.Slot(0).Box(W(side*(SpineHalf-.25f),SpineY(mid)+.02f,mid),new Vector3(.08f,.03f,z1-z-.6f),slope,Color.white);
                }
                // Alcoves at the braced rings, a floor plate either side.
                foreach(float ring in new[]{AftRing,ForeRing})foreach(int side in new[]{-1,1})
                    s.Slot(0).Box(W(side*(SpineHalf+AlcoveHalf)*.5f,SpineY(ring)-.2f,ring),new Vector3(AlcoveHalf-SpineHalf+.1f,.4f,4),Quaternion.identity,Pale,White,.25f);
                // The rings: a U of the tube's frame each, the deck's beam under it, a teal band, the ring's number on the deck before it.
                var hoops=new ZoneKit.Shape();
                for(int i=0;i<Rings.Length;i++)
                {
                    float z=Rings[i],y=SpineY(z);bool braced=z==AftRing||z==ForeRing;float x=braced?AlcoveHalf+.25f:SpineHalf+.3f;
                    s.Slot(0).Box(W(0,y-.55f,z),new Vector3(x*2+.6f,.4f,.7f),Quaternion.identity,IronPale,Pale);
                    foreach(int side in new[]{-1,1})
                    {
                        hoops.Slot(0).Box(W(side*x,y+1.3f,z),new Vector3(.36f,3.6f,.6f),Quaternion.identity,Pale,White);
                        hoops.Slot(0).Box(W(side*(x-.3f),y+3.05f,z),new Vector3(.8f,.34f,.6f),Quaternion.Euler(0,0,side*-24),Pale,White);
                        trim.Slot(0).Box(W(side*(x-.19f),y+1.6f,z),new Vector3(.04f,1.6f,.3f),Quaternion.identity,Teal);
                        glow.Slot(0).Box(W(side*(x-.2f),y+2.6f,z),new Vector3(.05f,.36f,.18f),Quaternion.identity,Color.white);
                    }
                }
                // The rails: posts every two metres, a handrail and a mid rail, round each alcove.
                var rails=new ZoneKit.Shape();
                void RailRun(float x,float z0,float z1)
                {
                    for(float z=z0;z<=z1+.01f;z+=2)rails.Slot(0).Post(W(x,SpineY(z),z),1.1f,.05f,.04f,6,IronPale,Pale);
                    rails.Slot(0).Beam(W(x,SpineY(z0)+1.05f,z0),W(x,SpineY(z1)+1.05f,z1),.08f,.08f,White);
                    rails.Slot(1).Beam(W(x,SpineY(z0)+.55f,z0),W(x,SpineY(z1)+.55f,z1),.05f,.05f,Teal);
                }
                var runs=new List<(float x0,float z0,float x1,float z1)>();
                foreach(int side in new[]{-1,1})
                {
                    float sx=side*SpineHalf,ax=side*AlcoveHalf;
                    runs.Add((sx,SpineFoot,sx,AftRing-2));runs.Add((sx,AftRing-2,ax,AftRing-2));runs.Add((ax,AftRing-2,ax,AftRing+2));runs.Add((ax,AftRing+2,sx,AftRing+2));
                    runs.Add((sx,AftRing+2,sx,ForeRing-2));runs.Add((sx,ForeRing-2,ax,ForeRing-2));runs.Add((ax,ForeRing-2,ax,ForeRing+2));runs.Add((ax,ForeRing+2,sx,ForeRing+2));
                    runs.Add((sx,ForeRing+2,sx,SpineHead));runs.Add((sx,SpineHead,side*4,SpineHead));runs.Add((side*4,SpineHead,side*4,HullFace));
                    runs.Add((side*7,-86,side*7,SpineFoot));runs.Add((side*7,SpineFoot,sx,SpineFoot));
                }
                runs.Add((-7,-86,7,-86));
                foreach(var (x0,z0,x1,z1) in runs)
                {
                    if(Mathf.Abs(x1-x0)<.01f)RailRun(x0,Mathf.Min(z0,z1),Mathf.Max(z0,z1));
                    else{float y=SpineY(z0);for(float t=0;t<=1.001f;t+=.5f)rails.Slot(0).Post(W(Mathf.Lerp(x0,x1,t),y,z0),1.1f,.05f,.04f,6,IronPale,Pale);rails.Slot(0).Beam(W(x0,y+1.05f,z0),W(x1,y+1.05f,z1),.08f,.08f,White);}
                }
                var deck=Made(kit,"The spine's deck",s,WhiteMat);deck.transform.SetParent(root.transform,true);deck.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.On;
                var ringed=Made(kit,"The spine's rings",hoops,WhiteMat);ringed.transform.SetParent(root.transform,true);
                Made(kit,"The spine's trim",trim,TealMat).transform.SetParent(root.transform,true);
                var lights=Made(kit,"The spine's running lights",glow,LampTeal);lights.transform.SetParent(root.transform,true);lights.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                var railing=Made(kit,"The spine's rails",rails,WhiteMat,TealMat);railing.transform.SetParent(root.transform,true);
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
                // What a body walks on and is stopped by: the platform, the landing, the gangway as one ramp, the alcoves, every rail run.
                void Solid(string name,Vector3 centre,Vector3 size,Quaternion turn){var g=new GameObject(name,typeof(BoxCollider));g.isStatic=true;g.transform.SetParent(deck.transform,true);g.transform.SetPositionAndRotation(centre,turn);g.GetComponent<BoxCollider>().size=size;}
                Solid("Spine foot (walked)",W(0,Foot-.25f,-77),new Vector3(14,.5f,18),Quaternion.identity);
                Solid("Spine landing (walked)",W(0,Up-.25f,-12),new Vector3(8,.5f,8),Quaternion.identity);
                float length=Mathf.Sqrt((SpineHead-SpineFoot)*(SpineHead-SpineFoot)+(Up-Foot)*(Up-Foot));
                Solid("Spine gangway (walked)",W(0,(Foot+Up)*.5f-.2f/Mathf.Cos(angle*Mathf.Deg2Rad),(SpineFoot+SpineHead)*.5f),new Vector3(SpineHalf*2,.4f,length+.2f),slope);
                foreach(float ring in new[]{AftRing,ForeRing})foreach(int side in new[]{-1,1})
                    Solid("Spine alcove (walked)",W(side*(SpineHalf+AlcoveHalf)*.5f,SpineY(ring)-.2f,ring),new Vector3(AlcoveHalf-SpineHalf+.1f,.4f,4),Quaternion.identity);
                foreach(var (x0,z0,x1,z1) in runs)
                {
                    var a=W(x0,0,z0);var b=W(x1,0,z1);var mid=(a+b)*.5f;var run=b-a;float y=SpineY(mid.z);
                    Solid("Spine rail",W(mid.x,y+.55f,mid.z),new Vector3(.3f,1.7f,run.magnitude+.3f),Quaternion.Euler(0,Mathf.Atan2(run.x,run.z)*Mathf.Rad2Deg,0));
                }
                foreach(float ring in Rings){float y=SpineY(ring);bool braced=ring==AftRing||ring==ForeRing;float x=braced?AlcoveHalf+.25f:SpineHalf+.3f;foreach(int side in new[]{-1,1})ZoneKit.Block(ringed,W(side*x,y+1.3f,ring),new Vector3(.36f,3.6f,.6f),0,"Spine ring post");}
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
                ZoneKit.Fadeable(ringed);
                for(int i=0;i<Rings.Length;i++)Painted(kit,"Ring "+(i+1),"RING "+(i+1),W(0,SpineY(Rings[i]-1.6f)+.03f,Rings[i]-1.6f),Vector3.right,Vector3.up,2.2f,new Color(.3f,.62f,.66f)).transform.SetParent(root.transform,true);
                Braces(kit,root);
                // A floodlight over the platform where the party docks, the convoy's cold light coming up from below.
                var flood=new ZoneKit.Shape();foreach(int side in new[]{-1,1}){var foot=W(side*6.2f,Foot,-84.6f);flood.Slot(0).Post(foot,3.4f,.12f,.1f,8,IronPale,Pale).Box(foot+W(0,3.5f,.2f),new Vector3(.8f,.36f,.5f),Quaternion.Euler(30,0,0),IronPale,White);}
                var floods=Made(kit,"The platform's floods",flood,WhiteMat);floods.transform.SetParent(root.transform,true);ZoneKit.Block(floods,W(-6.2f,Foot+1.7f,-84.6f),new Vector3(.4f,3.4f,.4f),0,"Flood post");ZoneKit.Block(floods,W(6.2f,Foot+1.7f,-84.6f),new Vector3(.4f,3.4f,.4f),0,"Flood post");
                PointLight(W(0,Foot+3.4f,-80),14,1.6f,new Color(.9f,.95f,1));
                Convoy(kit,new[]{("Hauler",W(-30,-30,-46),30f,30f,true),("PatrolCutter",W(24,-20,-60),14f,-20f,true),("Skiff",W(18,-34,-24),12f,160f,true),("CompactCutter",W(-20,-16,-78),4f,80f,false),
                    ("SeedBarge",W(-44,-48,-14),8f,40f,false),("TallowStationHull",W(54,-70,-44),60f,15f,true),("AnchorChain",W(-6,-80,-30),70f,62f,true),("Skiff",W(30,-46,-92),10f,-60f,true)},"Below the spine");
                return root;
            }
            /// <summary>Idra's crews at the two rings they hold open: orange jacks from each alcove's floor up to the ring's frame, a power pack and a lamp on a tripod, and the crew at them:
            /// Sibb at the aft ring, Kott (the winch hand, big) at the forward ring, a rescue hand in her frame at each (Many Hands' arrivals' bodies, in the crew's own greys).</summary>
            static void Braces(ZoneKit kit,GameObject root)
            {
                var s=new ZoneKit.Shape();var black=new Color(.12f,.12f,.13f);
                foreach(float ring in new[]{AftRing,ForeRing})
                {
                    float y=SpineY(ring);
                    foreach(int side in new[]{-1,1})
                    {
                        var post=W(side*(AlcoveHalf+.1f),y+2.7f,ring);
                        foreach(float dz in new[]{-1.4f,1.4f})
                        {
                            var foot=W(side*(AlcoveHalf-.6f),y+.05f,ring+dz);
                            s.Slot(0).Tube(new[]{foot,Vector3.Lerp(foot,post,.55f)},new[]{.16f,.16f},10,Orange,Orange,1,.3f);
                            s.Slot(1).Tube(new[]{Vector3.Lerp(foot,post,.5f),post},new[]{.1f,.1f},8,IronPale,IronPale,1,.3f);
                            s.Slot(0).Box(foot+Vector3.up*.05f,new Vector3(.5f,.1f,.5f),Quaternion.identity,black,Orange);
                            for(float t=.1f;t<.5f;t+=.12f)s.Slot(1).Tube(new[]{Vector3.Lerp(foot,post,t),Vector3.Lerp(foot,post,t+.03f)},new[]{.17f,.17f},10,black,black,1,.3f,false);
                        }
                        var pack=W(side*(AlcoveHalf-.5f),y,ring-.2f);s.Slot(0).Box(pack+Vector3.up*.3f,new Vector3(.6f,.6f,.5f),Quaternion.Euler(0,side*12,0),Orange,new Color(1,.6f,.3f));
                    }
                    // A lamp on a tripod in the forward alcove's corner, amber.
                    var tripod=W(-(AlcoveHalf-.4f),y,ring+1.6f);
                    for(int k=0;k<3;k++){float a=k*120*Mathf.Deg2Rad;s.Slot(1).Beam(tripod+W(Mathf.Sin(a)*.4f,0,Mathf.Cos(a)*.4f),tripod+Vector3.up*1.9f,.05f,.05f,black);}
                    s.Slot(0).Box(tripod+Vector3.up*2,new Vector3(.4f,.3f,.3f),Quaternion.Euler(20,40,0),Orange);
                    PointLight(tripod+W(.6f,2.1f,-.4f),8,1.5f,new Color(1,.74f,.42f));
                }
                var built=Made(kit,"Idra's crews' braces",s,BraceMat,DarkMat);built.transform.SetParent(root.transform,true);
                foreach(float ring in new[]{AftRing,ForeRing})foreach(int side in new[]{-1,1})ZoneKit.Block(built,W(side*(AlcoveHalf-.5f),SpineY(ring)+.3f,ring-.2f),new Vector3(.6f,.6f,.5f),0,"Power pack");
                // The crew: in the alcoves, facing the walk.
                var people=new GameObject("Idra's crews on the spine");people.transform.SetParent(root.transform,true);
                void One(string name,string id,Vector2 at,float yaw,Color tint,float scale){var b=Body(kit,name,"Characters",id,W(at.x,SpineY(at.y),at.y),yaw,tint,scale,true,"Characters","IdraNatural");if(b!=null)b.transform.SetParent(people.transform,true);}
                One("Sibb, at the aft ring","IdraNatural",SibbAt,90,new Color(1.02f,.96f,.92f),.94f);
                One("Kott, at the forward ring","IdraNatural",KottAt,-90,new Color(.7f,.72f,.76f),1.1f);
                One("A rescue hand, at the aft ring","IdraBraced",new Vector2(3.6f,AftRing-.8f),-80,new Color(.82f,.84f,.9f),1);
                One("A rescue hand, at the forward ring","IdraBraced",new Vector2(-3.6f,ForeRing-.8f),80,new Color(.9f,.86f,.84f),1);
            }
            /// <summary>The hull's south flank where the spine meets it: white plate stepping up either side of the hatch, the teal line along it, rows of lit windows, and the ship's name
            /// in the Compact's hand; it runs down into the dark under the spine. Drawn only (the decks' own plating is the wall), and faded where it stands before the hatch hall.</summary>
            static void HullFlank(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var windows=new ZoneKit.Shape();var panes=new ZoneKit.Shape();float z=HullFace-.3f;
                float TopAt(float x){float a=Mathf.Abs(x);return a<3.2f?4.2f:a<11?5.2f:a<20?8:10;}
                // The look pass of November 21 (s66/013: the flank read as white tile with bright window grids, flat and over-bright behind the spine's head): its plates in courses of
                // the hull's own whites and the Compact's pale teal-grey, darker toward the dark under the spine, a rib every four strips; one window in three lit warm, the rest dark
                // glass, each in a dark frame.
                for(float x=-40;x<EdgeX-.01f;x+=2)
                {
                    float x1=Mathf.Min(x+2,EdgeX),mid=(x+x1)*.5f,top=TopAt(mid);int ix=Mathf.FloorToInt((x+400)/2);float own=H(ix,17);
                    var plate=own<.22f?new Color(.6f,.68f,.71f):Color.Lerp(new Color(.68f,.71f,.74f),new Color(.86f,.88f,.9f),own);
                    if(Mathf.Abs(mid)<3.2f){s.Slot(0).Quad(W(x,3.8f,z),W(x1,3.8f,z),W(x1,top,z),W(x,top,z),Vector3.back,ZoneKit.Shape.Dim(White,.9f),1/8f);s.Slot(0).Quad(W(x,-30,z),W(x1,-30,z),W(x1,-.5f,z),W(x,-.5f,z),Vector3.back,ZoneKit.Shape.Dim(Pale,.8f),1/8f);continue;}
                    s.Slot(0).Quad(W(x,-30,z),W(x1,-30,z),W(x1,-2,z),W(x,-2,z),Vector3.back,ZoneKit.Shape.Dim(plate,.72f),1/8f);
                    s.Slot(0).Quad(W(x,-2,z),W(x1,-2,z),W(x1,top,z),W(x,top,z),Vector3.back,plate,1/8f);
                    s.Slot(1).Box(W(mid,top-.15f,z+.15f),new Vector3(x1-x+.02f,.3f,.6f),Quaternion.identity,Gold);
                    if(ix%4==0)panes.Slot(0).Box(W(x,(top-30)*.5f,z-.08f),new Vector3(.22f,top+30,.12f),Quaternion.identity,new Color(.42f,.48f,.52f));
                    if(Mathf.Abs(mid)>5)for(float y=-24;y<top-1.6f;y+=3.2f)
                    {
                        if(Mathf.Abs(mid)<11&&y>-3)continue;var at=W(mid,y+1.2f,z-.03f);
                        panes.Slot(0).Box(at+Vector3.back*.01f,new Vector3(1.34f,.76f,.03f),Quaternion.identity,new Color(.16f,.19f,.22f));
                        if(H(ix,Mathf.FloorToInt(y+100))<.34f)windows.Slot(0).Box(at+Vector3.back*.03f,new Vector3(1.1f,.55f,.04f),Quaternion.identity,Color.white);
                        else panes.Slot(1).Box(at+Vector3.back*.03f,new Vector3(1.1f,.55f,.04f),Quaternion.identity,Color.white);
                    }
                }
                s.Slot(1).Box(W(-6,2.4f,z-.05f),new Vector3(68,.36f,.08f),Quaternion.identity,Teal);
                // The hatch's collar: a ring in the flank, teal-lined, a lamp over it.
                foreach(int side in new[]{-1,1})s.Slot(0).Box(W(side*4.3f,1.9f,z-.2f),new Vector3(.5f,3.8f,.5f),Quaternion.identity,Pale,White);
                s.Slot(0).Box(W(0,3.95f,z-.2f),new Vector3(9.1f,.4f,.5f),Quaternion.identity,Pale,White);s.Slot(1).Box(W(0,3.62f,z-.46f),new Vector3(6.4f,.1f,.04f),Quaternion.identity,Teal);
                var flank=Made(kit,"The hull's south flank",s,HullMat,GoldMat);flank.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;ZoneKit.Fadeable(flank);
                var lit=Made(kit,"The hull's south windows",windows,LampWarm);lit.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;lit.transform.SetParent(flank.transform,true);
                var framed=Made(kit,"The hull's south window frames and dark glass",panes,DarkMat,SpaceMat);framed.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;framed.transform.SetParent(flank.transform,true);
                Painted(kit,"Ship name","CONTINUANCE",W(20.6f,6.5f,z-.05f),Vector3.right,Vector3.back,17,new Color(.14f,.4f,.44f)).transform.SetParent(flank.transform,true);
                Painted(kit,"Ship mark","COMPACT 1",W(-22,6.5f,z-.05f),Vector3.right,Vector3.back,12,new Color(.14f,.4f,.44f)).transform.SetParent(flank.transform,true);
            }
            /// <summary>The convoy and the anchor's chains, out in the space past a window, under the spine or beyond the bay's mouth: drawn only, turned, each its own tint.</summary>
            static void Convoy(ZoneKit kit,(string key,Vector3 at,float size,float yaw,bool byLength)[] ships,string name)
            {
                var root=new GameObject(name);var held=new GameObject(name+" (out in space)");held.transform.SetParent(root.transform,true);int k=0;
                foreach(var (key,at,size,yaw,byLength) in ships)
                {
                    string folder=ZoneKit.Prefab("Ships",key)!=null?"Ships":"Environment";
                    var ship=Model(folder,key,at,size,yaw,byLength,name+": "+key+" "+k);if(ship==null)continue;
                    ship.transform.SetParent(held.transform,true);ship.AddComponent<PropTint>().tint=Color.Lerp(Color.white,new Color(.8f,.86f,.94f),(k%3)*.3f);
                    foreach(var r in ship.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
                    k++;
                }
            }

            // ---------------------------------------------------------------- the hatch hall and the crew deck
            /// <summary>The hatch hall: the inner collar, a Compact console at the door, and what Idra's crews left on their way through (orange cases, a coil of line).</summary>
            static void Hatch(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();
                foreach(var (x,z) in new[]{(-3.8f,3.6f),(-3.2f,5.2f)})s.Slot(0).Box(W(x,Up+.32f,z),new Vector3(.9f,.64f,.6f),Quaternion.Euler(0,12*x,0),Orange,new Color(1,.62f,.32f));
                s.Slot(1).Tube(new[]{W(-3.6f,Up+.12f,1.4f),W(-3.1f,Up+.12f,1.9f),W(-3.6f,Up+.12f,2.4f),W(-4.1f,Up+.12f,1.9f),W(-3.6f,Up+.12f,1.4f)},Enumerable.Repeat(.1f,5).ToList(),6,new Color(.9f,.82f,.6f),new Color(.9f,.82f,.6f),1,.4f,false);
                var gear=Made(kit,"Idra's crews' cases",s,BraceMat,FabricMat);ZoneKit.Block(gear,W(-3.5f,Up+.32f,4.4f),new Vector3(1.4f,.64f,2.4f),0,"Cases");gear.isStatic=true;
                var desk=kit.Prop("DeckConsole",new Vector3(3.9f,0,2.6f),1.3f,-90);desk.name="Hatch console";desk.AddComponent<PropTint>().tint=new Color(.96f,1,1);
                PointLight(W(0,Up+3.6f,0),9,1.2f,new Color(.86f,.96f,1));
            }
            /// <summary>The crew deck: the mess by the windows (three long tables, warm lamps on them, the galley counter on the north wall with its cook), the bunk rows on the west with their
            /// curtains drawn back, the locker wall and the duty board; crews standing back out of the way, one holding the stair's hatch.</summary>
            static void CrewDeck(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var cloth=new ZoneKit.Shape();var lamps=new ZoneKit.Shape();var wood=new Color(.86f,.84f,.8f);var seat=new Color(.3f,.52f,.56f);
                var tables=new GameObject("The mess");
                foreach(float z in new[]{20f,28f,36f})
                {
                    var c=W(19,Up,z);
                    s.Slot(0).Box(c+Vector3.up*.76f,new Vector3(12,.08f,1.3f),Quaternion.identity,new Color(.62f,.54f,.46f),new Color(.82f,.72f,.6f));
                    foreach(float x in new[]{-5.4f,0,5.4f})s.Slot(0).Box(c+W(x,.36f,0),new Vector3(.16f,.72f,.9f),Quaternion.identity,IronPale,Pale);
                    foreach(float dz in new[]{-1.05f,1.05f})
                    {
                        s.Slot(1).Box(c+W(0,.44f,dz),new Vector3(11.6f,.08f,.42f),Quaternion.identity,seat,new Color(.36f,.6f,.62f));
                        foreach(float x in new[]{-5,0,5})s.Slot(0).Box(c+W(x,.2f,dz),new Vector3(.1f,.4f,.36f),Quaternion.identity,IronPale);
                    }
                    // Laid for the watch: cups and plates in a row, a warm lamp at each end and one in the middle (what was eaten off them is Aboard's).
                    for(float x=-5;x<=5.01f;x+=1.25f){s.Slot(0).Post(c+W(x,.8f,-.32f),.1f,.045f,.05f,8,White,White);s.Slot(0).Disc(c+W(x+.35f,.81f,.28f),Vector3.up,.16f,12,White,.1f,Pale);}
                    foreach(float x in new[]{-4.6f,0,4.6f}){lamps.Slot(0).Post(c+W(x,.8f,0),.34f,.11f,.07f,10,Color.white,Color.white);s.Slot(0).Post(c+W(x,.8f,0),.04f,.13f,.13f,10,Gold,Gold);}
                    var table=Made(kit,"Mess table "+z,s,WhiteMat,TealMat);s=new ZoneKit.Shape();table.transform.SetParent(tables.transform,true);
                    ZoneKit.Block(table,c+Vector3.up*.45f,new Vector3(12,.9f,3.2f),0,"Mess table");table.isStatic=true;
                    PointLight(c+W(0,2.2f,0),9,1.5f,new Color(1,.8f,.56f));
                }
                Made(kit,"Mess lamps",lamps,LampWarm).transform.SetParent(tables.transform,true);
                // The windows' cold light, low across the mess from the east: the convoy outside.
                foreach(float z in new[]{18f,34f})PointLight(W(30.4f,Up+2.2f,z),10,1.2f,new Color(.62f,.78f,1));
                // The galley counter on the north wall east of the duty board, its hood, the pans on it, and its cook (a walker behind the counter's front).
                var g=new ZoneKit.Shape();var counter=W(22,Up,48.7f);
                g.Slot(0).Box(counter+Vector3.up*.5f,new Vector3(14,1,1.3f),Quaternion.identity,Pale,new Color(.8f,.84f,.86f));
                g.Slot(1).Box(counter+W(0,.92f,-.66f),new Vector3(14,.1f,.04f),Quaternion.identity,Teal);
                foreach(float x in new[]{-5.5f,-3f,1f,4.5f})g.Slot(0).Post(counter+W(x,1,.1f),.26f,.24f,.26f,12,IronPale,IronPale);
                var galley=Made(kit,"The galley counter",g,WhiteMat,TealMat);ZoneKit.Block(galley,counter+Vector3.up*.5f,new Vector3(14,1,1.3f),0,"Counter");galley.isStatic=true;
                var cook=kit.Walker("Neve",new[]{new Vector2(17,46.4f),new Vector2(27,46.4f)},.7f,3.5f,new Color(.96f,.98f,1),"Neve");cook.thereAndBack=true;cook.name="The cook at the galley";
                // The bunk rows: double bunks in rows facing south (the camera's way), each a pod with its curtain drawn back, a locker at each row's end.
                var b=new ZoneKit.Shape();var curtain=new ZoneKit.Shape();Color[] blankets={new(.24f,.54f,.56f),new(.16f,.36f,.42f),new(.7f,.62f,.52f),new(.3f,.46f,.6f)};int bed=0;
                foreach(float z in new[]{18f,29f,40f})
                {
                    for(float x=-16.6f;x<=-7f;x+=2.4f)
                    {
                        var c=W(x+1.1f,Up,z);
                        foreach(float dx in new[]{-1.02f,1.02f})foreach(float dz in new[]{-.46f,.46f})b.Slot(0).Box(c+W(dx,.95f,dz),new Vector3(.08f,1.9f,.08f),Quaternion.identity,IronPale,Pale);
                        foreach(float up in new[]{.42f,1.36f})
                        {
                            b.Slot(0).Box(c+W(0,up,0),new Vector3(2.1f,.08f,1),Quaternion.identity,Pale,White);
                            b.Slot(0).Box(c+W(0,up+.11f,0),new Vector3(2,.14f,.92f),Quaternion.identity,new Color(.86f,.88f,.9f),new Color(.94f,.95f,.96f));
                            var blanket=blankets[bed++%blankets.Length];b.Slot(0).Box(c+W(.28f,up+.2f,0),new Vector3(1.4f,.06f,.94f),Quaternion.identity,ZoneKit.Shape.Dim(blanket,.8f),blanket);
                            b.Slot(0).Box(c+W(-.74f,up+.24f,0),new Vector3(.4f,.12f,.6f),Quaternion.identity,White,Color.white);
                        }
                        for(float up=.6f;up<1.4f;up+=.32f)b.Slot(0).Box(c+W(1.06f,up,-.5f),new Vector3(.06f,.04f,.3f),Quaternion.identity,IronPale);
                        b.Slot(0).Box(c+W(0,.16f,0),new Vector3(1.8f,.3f,.8f),Quaternion.identity,ZoneKit.Shape.Dim(Teal,.7f),Teal);
                        if(bed%3==0)curtain.Slot(0).Box(c+W(-1.06f,1.05f,-.5f),new Vector3(.06f,1.8f,.5f),Quaternion.identity,new Color(.3f,.6f,.62f));
                    }
                }
                var bunks=Made(kit,"The bunk rows",b,WhiteMat);bunks.isStatic=true;Made(kit,"Bunk curtains",curtain,FabricMat).transform.SetParent(bunks.transform,true);
                foreach(float z in new[]{18f,29f,40f})ZoneKit.Block(bunks,W(-11.8f,Up+.95f,z),new Vector3(11.8f,1.9f,1.1f),0,"Bunk row");
                // The locker wall (north, west of the stair) with a name plate on each door, the duty board east of the stair.
                var l=new ZoneKit.Shape();var plates=new ZoneKit.Shape();
                for(float x=-17.4f;x<=-4.4f;x+=.8f)
                {
                    l.Slot(0).Box(W(x,Up+1.1f,49.4f),new Vector3(.76f,2.2f,.6f),Quaternion.identity,Pale,White);
                    plates.Slot(0).Box(W(x,Up+1.75f,49.08f),new Vector3(.44f,.12f,.03f),Quaternion.identity,Color.white);
                    l.Slot(1).Box(W(x+.28f,Up+1.1f,49.08f),new Vector3(.04f,.3f,.03f),Quaternion.identity,IronPale);
                }
                var lockers=Made(kit,"The locker wall",l,WhiteMat,DarkMat);lockers.isStatic=true;ZoneKit.Block(lockers,W(-10.9f,Up+1.1f,49.4f),new Vector3(13.8f,2.2f,.6f),0,"Lockers");
                Made(kit,"Locker name plates",plates,LampWhite).transform.SetParent(lockers.transform,true);ZoneKit.Fadeable(lockers);
                var d=new ZoneKit.Shape();var tiles=new ZoneKit.Shape();var boardAt=W(9.5f,Up+1.9f,49.55f);
                d.Slot(0).Box(boardAt,new Vector3(4.6f,2,.2f),Quaternion.identity,Iron,IronPale);d.Slot(0).Box(W(boardAt.x,Up+.45f,49.3f),new Vector3(4.8f,.9f,.7f),Quaternion.identity,Pale,White);
                for(int i=0;i<8;i++)for(int j=0;j<5;j++){var at=boardAt+W(-1.95f+i*.56f,-.7f+j*.36f,-.12f);tiles.Slot((i*3+j)%4==0?1:0).Box(at,new Vector3(.44f,.26f,.02f),Quaternion.identity,Color.white);}
                var board=Made(kit,"The duty board",d,DarkMat);board.isStatic=true;ZoneKit.Block(board,W(boardAt.x,Up+.45f,49.3f),new Vector3(4.8f,.9f,.7f),0,"Duty cabinet");var lit=Made(kit,"Duty board tiles",tiles,LampTeal,LampWarm);lit.transform.SetParent(board.transform,true);
                Painted(kit,"Duty","DUTY",boardAt+W(0,1.3f,-.1f),Vector3.right,Vector3.back,2.2f,new Color(.2f,.5f,.54f)).transform.SetParent(board.transform,true);
                // Crews who stand back out of the way, and the one holding the hatch to the stair open (its leaf swung back against the wall, his hand on it).
                Crew(kit,"A crew standing aside, by the bunks",new Vector2(-4.4f,23.4f),110,0,true,"Neve");
                Crew(kit,"A crew standing aside, by the bunks",new Vector2(-4.6f,34.2f),80,1,true,"PatrolBody");
                Crew(kit,"A crew standing aside, at the lockers",new Vector2(-13,46.6f),170,2,true,"Mira");
                Crew(kit,"A crew holding the hatch",HelperAt,262,3,true,"SurvivorNatural");
                var leaf=new ZoneKit.Shape();leaf.Slot(0).Box(W(3.6f,Up+1.6f,49.6f),new Vector3(1.6f,3.2f,.16f),Quaternion.identity,Pale,White).Slot(1).Box(W(3.6f,Up+1.7f,49.48f),new Vector3(1.2f,.1f,.04f),Quaternion.identity,Teal);
                var held=Made(kit,"The stair hatch, held open",leaf,WhiteMat,TealMat);ZoneKit.Block(held,W(3.6f,Up+1.6f,49.6f),new Vector3(1.6f,3.2f,.16f),0,"Hatch leaf");held.isStatic=true;ZoneKit.Fadeable(held);
                Sight("standaside",W(-6,Up,28),12,"end.sawCrewAside","end.boarded");
                Sight("helpers",On(kit,HelperAt),9,"end.sawHatchHeld","end.boarded");
                OnPlate(Painted(kit,"Crew deck","CREW DECK 2",W(-11,Up+3.4f,49.95f),Vector3.right,Vector3.back,7,new Color(.18f,.46f,.5f)),"Crew north");
            }

            // ---------------------------------------------------------------- maintenance deck four
            /// <summary>Deck four: a Compact cutter up on its service cradle with its panels off and a light on each side of it, the benches and tool walls along the north wall, the parts racks
            /// east, a hand at work on the cutter; the deck's east door where the captain's crew go out.</summary>
            static void DeckFour(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var c=W(-13,Down,82);
                // The cradle: two rails, cross members, four arms up to the hull.
                foreach(int side in new[]{-1,1})s.Slot(0).Box(c+W(side*2.4f,.3f,0),new Vector3(.5f,.6f,13),Quaternion.identity,IronPale,Pale);
                foreach(float z in new[]{-5.5f,-1.8f,1.8f,5.5f})s.Slot(0).Box(c+W(0,.25f,z),new Vector3(5.4f,.4f,.5f),Quaternion.identity,IronPale,Pale);
                foreach(int side in new[]{-1,1})foreach(float z in new[]{-3.6f,3.6f}){s.Slot(1).Beam(c+W(side*2.4f,.6f,z),c+W(side*1.3f,1.7f,z),.26f,.26f,Teal);}
                s.Slot(1).Box(c+W(0,.62f,0),new Vector3(.6f,.04f,12),Quaternion.identity,Gold);
                // Its panels off and stood against the cradle, a tool cart, a hose.
                foreach(var (x,z,yaw) in new[]{(-3.8f,-3f,10f),(-3.9f,-1.4f,-6f),(3.8f,4.2f,170f)})s.Slot(0).Box(c+W(x,.7f,z),new Vector3(.12f,1.4f,1.2f),Quaternion.Euler(0,yaw,8),White,Pale);
                s.Slot(0).Box(c+W(4.4f,.45f,-3),new Vector3(.9f,.9f,.6f),Quaternion.identity,Teal,new Color(.3f,.62f,.64f));
                var cradle=Made(kit,"The cutter's service cradle",s,WhiteMat,TealMat);cradle.isStatic=true;
                ZoneKit.Block(cradle,c+Vector3.up*.6f,new Vector3(6,1.2f,13.4f),0,"Cradle");
                var cutter=Model("Ships","CompactCutter",c+Vector3.up*1.2f,11,0,true,"The cutter on its cradle");
                if(cutter!=null){cutter.transform.SetParent(cradle.transform,true);var box=ModelGeometry.BoundsOf(cutter);ZoneKit.Block(cradle,box.center,new Vector3(Mathf.Min(box.size.x,4.6f),box.size.y,Mathf.Min(box.size.z,12)),0,"Cutter");ZoneKit.Fadeable(cradle);}
                // Work lights on stands either side, hard and white.
                var stands=new ZoneKit.Shape();
                foreach(var (x,z,yaw) in new[]{(-19.6f,77f,60f),(-6.4f,88f,-120f)}){var foot=W(x,Down,z);stands.Slot(0).Post(foot,3.2f,.1f,.08f,8,IronPale,Pale).Box(foot+W(0,3.3f,0),new Vector3(.9f,.5f,.4f),Quaternion.Euler(25,yaw,0),IronPale,White);PointLight(foot+W(Mathf.Sin(yaw*Mathf.Deg2Rad)*1.4f,3,Mathf.Cos(yaw*Mathf.Deg2Rad)*1.4f),11,2.1f,new Color(.94f,.97f,1));}
                var st=Made(kit,"Work light stands",stands,WhiteMat);ZoneKit.Block(st,W(-19.6f,Down+1.6f,77),new Vector3(.4f,3.2f,.4f),0,"Stand");ZoneKit.Block(st,W(-6.4f,Down+1.6f,88),new Vector3(.4f,3.2f,.4f),0,"Stand");st.isStatic=true;ZoneKit.Fadeable(st);
                // The hoist over it: an A-frame at each end, a beam along the top, the block hanging over the cutter's open panel.
                var h=new ZoneKit.Shape();
                foreach(float z in new[]{73.6f,90.4f})foreach(int side in new[]{-1,1})h.Slot(0).Beam(W(-13+side*3.6f,Down,z),W(-13,Down+6.4f,z),.3f,.3f,Pale,White);
                h.Slot(0).Box(W(-13,Down+6.5f,82),new Vector3(.5f,.5f,17.4f),Quaternion.identity,Pale,White);h.Slot(1).Box(W(-13,Down+6.5f,82),new Vector3(.52f,.12f,17),Quaternion.identity,Teal);
                h.Slot(0).Box(W(-13,Down+6.1f,84.6f),new Vector3(.9f,.5f,.9f),Quaternion.identity,IronPale,Pale);h.Slot(0).Beam(W(-13,Down+5.85f,84.6f),W(-13,Down+4.4f,84.6f),.05f,.05f,Iron);
                h.Slot(1).Box(W(-13,Down+4.3f,84.6f),new Vector3(.4f,.3f,.3f),Quaternion.identity,Orange);
                var hoist=Made(kit,"The hoist over the cutter",h,WhiteMat,TealMat);hoist.isStatic=true;ZoneKit.Fadeable(hoist);
                foreach(float z in new[]{73.6f,90.4f})foreach(int side in new[]{-1,1})ZoneKit.Block(hoist,W(-13+side*3.5f,Down+.4f,z),new Vector3(.5f,.8f,.5f),0,"Hoist foot");
                var tc=new ZoneKit.Shape();
                for(float z=84;z<=99;z+=2.5f){var at=W(22.9f,Down,z);tc.Slot(0).Box(at+Vector3.up*.55f,new Vector3(.8f,1.1f,2.1f),Quaternion.identity,ZoneKit.Shape.Dim(Teal,.8f),Teal);for(float y=.25f;y<1;y+=.24f)tc.Slot(1).Box(at+W(-.41f,y,0),new Vector3(.03f,.03f,1.6f),Quaternion.identity,Pale);}
                var chests=Made(kit,"Deck four's tool chests",tc,TealMat,WhiteMat);chests.isStatic=true;ZoneKit.Block(chests,W(22.9f,Down+.55f,91.5f),new Vector3(.8f,1.1f,17.6f),0,"Tool chests");
                // A hand at work alongside the cutter.
                var hand=kit.Walker("Neve",new[]{new Vector2(-21.4f,74),new Vector2(-21.4f,89)},.6f,5,new Color(.9f,.96f,1),"Neve");hand.thereAndBack=true;hand.name="A hand at the cutter";
                // Benches along the north wall with tool walls over them, racks and parts east, a test stand with a cutter's drive on it.
                var benches=new GameObject("Deck four benches");
                foreach(float x in new[]{-18f,-12f,8f,14f,20f}){var bench=kit.Prop(ZoneKit.Kit("DressWorkbench","DeckBench"),new Vector3(x,0,102.6f),1.1f,180);bench.transform.SetParent(benches.transform,true);bench.AddComponent<PropTint>().tint=new Color(.92f,.98f,1);}
                foreach(var (x,z,yaw) in new[]{(19f,64f,0f),(21.6f,64f,0f),(22.8f,68f,-90f),(22.8f,70.6f,-90f)}){var rack=kit.Prop(ZoneKit.Kit("DressShelfRack","DeckCrate"),new Vector3(x,0,z),2.4f,yaw);rack.transform.SetParent(benches.transform,true);rack.AddComponent<PropTint>().tint=new Color(.9f,.96f,1);}
                foreach(var (x,z) in new[]{(16f,67f),(16.8f,69.2f),(14.6f,68.4f)}){var crate=kit.Prop("DeckCrate",new Vector3(x,0,z),.9f,x*9);crate.transform.SetParent(benches.transform,true);crate.AddComponent<PropTint>().tint=new Color(.94f,.98f,1);}
                var t=new ZoneKit.Shape();var drive=W(16,Down,94);
                t.Slot(0).Box(drive+Vector3.up*.4f,new Vector3(2.4f,.8f,1.6f),Quaternion.identity,IronPale,Pale);
                t.Slot(1).Tube(new[]{drive+W(-1.1f,1.5f,0),drive+W(1.1f,1.5f,0)},new[]{.62f,.5f},14,Teal,new Color(.3f,.66f,.68f),1,.3f);
                t.Slot(0).Disc(drive+W(1.14f,1.5f,0),Vector3.right,.46f,14,Iron,.2f,LampTealColour);
                var test=Made(kit,"A cutter's drive on its test stand",t,WhiteMat,TealMat);ZoneKit.Block(test,drive+Vector3.up*1,new Vector3(2.6f,2,1.8f),0,"Test stand");test.isStatic=true;
                PointLight(W(16,Down+2.6f,96),8,1.2f,new Color(1,.86f,.66f));
                // The deck's east door: where the captain's crew go.
                var dd=new ZoneKit.Shape();var doorAt=W(28.4f,Down,77);
                dd.Slot(0).Box(doorAt+Vector3.up*1.6f,new Vector3(.3f,3.2f,4.2f),Quaternion.identity,new Color(.2f,.24f,.28f));
                foreach(int side in new[]{-1,1})dd.Slot(1).Box(doorAt+W(-.1f,1.7f,side*2.2f),new Vector3(.5f,3.4f,.3f),Quaternion.identity,Pale,White);
                dd.Slot(1).Box(doorAt+W(-.1f,3.45f,0),new Vector3(.5f,.3f,4.7f),Quaternion.identity,Pale,White);
                var eastDoor=Made(kit,"Deck four's east door",dd,DarkMat,WhiteMat);ZoneKit.Block(eastDoor,doorAt+Vector3.up*1.6f,new Vector3(.3f,3.2f,4.2f),0,"Door");eastDoor.isStatic=true;
                OnPlate(Painted(kit,"Deck four","DECK 4",W(-6,Down+4.4f,103.95f),Vector3.right,Vector3.back,5,new Color(.18f,.46f,.5f)),"DeckFour north");
                Painted(kit,"Deck four bay","BAY C",W(-13,Down+.03f,73.6f),Vector3.right,Vector3.up,2.6f,new Color(.84f,.68f,.32f));
            }
            static readonly Color LampTealColour=new(.6f,.95f,1);
            /// <summary>The captain of the Steady Hand across the way with his crew either side: he stands until his talk is done and then steps aside to the west and holds the deck "from over
            /// there" (<c>Guide</c>); his crew walk off by the east door (<c>LeavesAfter</c>, guide 15.10). His body is owed (wiring 11): until <c>PatrolBody</c> is taken in, Dessik's coat in the
            /// Compact's white stands in, and the log says so.</summary>
            static void Captain(ZoneKit kit)
            {
                bool own=ZoneKit.Prefab("Characters","PatrolBody")!=null;
                if(!own)Debug.LogWarning("CONTINUANCE_PATROL_STANDIN the captain of the Steady Hand has no body of his own (PatrolBody, wiring 11): DessikBody stands in, in Compact white");
                var captain=Body(kit,"The captain of the Steady Hand","Characters","PatrolBody",On(kit,CaptainAt),180,own?Color.white:new Color(.86f,.96f,1),1,false,"Characters","DessikBody");
                if(captain!=null)
                {
                    var guide=captain.AddComponent<Guide>();guide.speed=1.6f;guide.patience=4;
                    guide.legs=new[]{new Guide.Leg{flag="",path=new[]{On(kit,CaptainAt)}},new Guide.Leg{flag="end.captainPassed",path=new[]{On(kit,new Vector2(-6,93.4f)),On(kit,CaptainAside)}}};
                    guide.faces=new[]{On(kit,CaptainAt),On(kit,CaptainCrossingAt),On(kit,new Vector2(0,88))};
                }
                for(int i=0;i<CaptainsCrew.Length;i++)
                {
                    var at=CaptainsCrew[i];var man=Crew(kit,"The Steady Hand's crew "+(i+1),at,180,i+1,false);if(man==null)continue;
                    var leave=man.AddComponent<LeavesAfter>();leave.flag="end.captainPassed";leave.speed=1.8f;leave.patience=4;
                    leave.path=new[]{On(kit,at+new Vector2(0,1.6f)),On(kit,new Vector2(14+i*.8f,90-i*.6f)),On(kit,new Vector2(24,79+i*.4f)),On(kit,EastDoorAt+new Vector2(.6f,0))};
                }
            }

            // ---------------------------------------------------------------- the anchor works
            /// <summary>The anchor works: what the game's earlier machinery looks like built properly, in the Compact's white and teal. Along the north wall the Saint's press twice over (each
            /// bed holding a clamp part still hot) and, east of the door, the Foundry's coolant sluices running cyan into a trough; along the east wall the Works' welding line, its two heads
            /// arcing over a jig; a clamp on its test stand; and down the middle, on rollers, a link of the anchor's own chain. Low stacks of finished parts on the west, with two crews standing
            /// back. Everything 2.5 m and taller fades for the camera, nothing as wide as seven metres stands taller than three on the fight's floor.</summary>
            static void AnchorWorks(ZoneKit kit)
            {
                var root=new GameObject("The anchor works");
                // ---- the presses
                foreach(var (x,z,n) in new[]{(-20f,165.6f,1),(-9.4f,165.6f,2)})
                {
                    var s=new ZoneKit.Shape();var foot=W(x,Down,z);float wide=5.2f,deep=3.4f,tall=6.8f;
                    s.Slot(0).Box(foot+Vector3.up*.45f,new Vector3(wide,.9f,deep),Quaternion.identity,IronPale,Pale);
                    foreach(int side in new[]{-1,1}){s.Slot(0).Box(foot+W(side*(wide*.5f-.45f),tall*.5f,0),new Vector3(.8f,tall,.9f),Quaternion.identity,Pale,White);s.Slot(1).Box(foot+W(side*(wide*.5f-.45f),tall*.62f,-.47f),new Vector3(.82f,.3f,.04f),Quaternion.identity,Teal);}
                    s.Slot(0).Box(foot+W(0,tall-.7f,0),new Vector3(wide,1.4f,1.4f),Quaternion.identity,Pale,White);
                    s.Slot(2).Box(foot+W(0,tall-1.5f,-.72f),new Vector3(wide-.6f,.16f,.04f),Quaternion.identity,Gold);
                    s.Slot(0).Box(foot+W(0,3.7f,0),new Vector3(3.2f,1.5f,1.6f),Quaternion.identity,IronPale,Pale);
                    s.Slot(1).Box(foot+W(0,3.7f,-.82f),new Vector3(3,.2f,.04f),Quaternion.identity,Teal);
                    foreach(int side in new[]{-1,1})s.Slot(0).Tube(new[]{foot+W(side*.9f,4.45f,0),foot+W(side*.9f,tall-1.4f,0)},new[]{.18f,.18f},10,new Color(.76f,.8f,.84f),new Color(.86f,.9f,.92f),1,.3f);
                    var press=Made(kit,"Press "+n,s,WhiteMat,TealMat,GoldMat);press.transform.SetParent(root.transform,true);press.isStatic=true;
                    ZoneKit.Block(press,foot+Vector3.up*tall*.5f,new Vector3(wide,tall,deep),0,"Press");ZoneKit.Fadeable(press);
                    // The part on the bed, still hot: a clamp's jaw.
                    var hot=new ZoneKit.Shape();hot.Slot(0).Box(foot+W(0,1.02f,0),new Vector3(1.8f,.24f,1),Quaternion.identity,Color.white);hot.Slot(0).Box(foot+W(.55f,1.24f,0),new Vector3(.5f,.2f,.9f),Quaternion.identity,Color.white);
                    Made(kit,"Press "+n+" work, hot",hot,HotMat).transform.SetParent(press.transform,true);
                    PointLight(foot+W(0,1.8f,-2.4f),7.5f,2.2f,new Color(1,.55f,.22f)).transform.SetParent(root.transform,true);
                    Painted(kit,"Press "+n+" number","P"+n,foot+W(0,tall-.7f,-.71f),Vector3.right,Vector3.back,1.4f,new Color(.18f,.46f,.5f)).transform.SetParent(press.transform,true);
                }
                // ---- the coolant sluices east of the door: three gates in the north wall, their channels running cyan across the floor into a long trough
                var sl=new ZoneKit.Shape();var cool=new ZoneKit.Shape();
                foreach(float x in new[]{8f,13.4f,18.8f})
                {
                    var foot=W(x,Down,170.2f);
                    sl.Slot(0).Box(foot+Vector3.up*1.9f,new Vector3(3.6f,3.8f,.9f),Quaternion.identity,Pale,White);
                    sl.Slot(1).Box(foot+W(0,1.1f,-.47f),new Vector3(2.6f,1.6f,.06f),Quaternion.identity,TealDeep);
                    sl.Slot(0).Disc(foot+W(1.25f,2.9f,-.5f),Vector3.back,.36f,12,IronPale,.12f,Gold);
                    cool.Slot(0).Box(foot+W(0,.02f,-3.2f),new Vector3(.36f,.04f,5.6f),Quaternion.identity,Color.white);
                    sl.Slot(0).Box(foot+W(-.45f,.06f,-3.2f),new Vector3(.1f,.12f,5.6f),Quaternion.identity,IronPale);sl.Slot(0).Box(foot+W(.45f,.06f,-3.2f),new Vector3(.1f,.12f,5.6f),Quaternion.identity,IronPale);
                }
                sl.Slot(0).Box(W(13.4f,Down+.2f,164),new Vector3(13.4f,.4f,1.2f),Quaternion.identity,IronPale,Pale);cool.Slot(0).Box(W(13.4f,Down+.41f,164),new Vector3(12.8f,.02f,.4f),Quaternion.identity,Color.white);
                var sluices=Made(kit,"The coolant sluices",sl,WhiteMat,DarkMat,GoldMat);sluices.transform.SetParent(root.transform,true);sluices.isStatic=true;ZoneKit.Fadeable(sluices);
                ZoneKit.Block(sluices,W(13.4f,Down+1.9f,170.2f),new Vector3(14.4f,3.8f,.9f),0,"Sluices");
                var c2=Made(kit,"Coolant running",cool,CoolMat);c2.transform.SetParent(sluices.transform,true);c2.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                PointLight(W(13.4f,Down+2.2f,165.5f),10,1.6f,new Color(.4f,.9f,1)).transform.SetParent(root.transform,true);
                // ---- the welding line along the east wall: a gantry on two pairs of legs with steel rails, four carriages riding them, each with its torch down to a section of the
                // anchor's link clamped on the jig, its seam still hot; the jig a dark steel bed; a festoon of cable from carriage to carriage; welders behind the line bent to the work.
                // (The look pass, November 14: a white bed and two white beams the line's whole length read from the camera as a long wall with a lamp at each end.)
                var wl=new ZoneKit.Shape();var arc=new ZoneKit.Shape();var seam=new ZoneKit.Shape();float wx=21.6f;var steel=new Color(.8f,.83f,.86f);
                foreach(float z in new[]{122f,158f})foreach(int side in new[]{-1,1})wl.Slot(0).Box(W(wx+side*1.8f,Down+2.2f,z),new Vector3(.36f,4.4f,.36f),Quaternion.identity,IronPale,Pale);
                foreach(int side in new[]{-1,1})wl.Slot(2).Box(W(wx+side*1.8f,Down+4.5f,140),new Vector3(.24f,.32f,37),Quaternion.identity,Iron,IronPale);
                wl.Slot(1).Box(W(wx-1.8f,Down+4.3f,140),new Vector3(.05f,.08f,36),Quaternion.identity,Teal);
                wl.Slot(2).Box(W(wx,Down+.42f,140),new Vector3(2.4f,.84f,32),Quaternion.identity,Iron,new Color(.4f,.42f,.46f));
                for(float z=124.6f;z<155.6f;z+=1.6f)foreach(int side in new[]{-1,1})wl.Slot(0).Box(W(wx+side*1.21f,Down+.4f,z),new Vector3(.05f,.66f,.14f),Quaternion.identity,IronPale);
                var carriages=new[]{127f,136f,145f,154f};
                foreach(float z in carriages)
                {
                    // A link section lying in its fixture: a flat loop of the chain's steel, a teal clamp at each end, the seam where the torch is glowing.
                    var loop=new List<Vector3>();for(int k=0;k<=24;k++){float a=k*Mathf.PI*2/24;loop.Add(W(wx+Mathf.Sin(a)*.62f,Down+1.02f,z+Mathf.Cos(a)*2.4f));}
                    wl.Slot(0).Tube(loop,Enumerable.Repeat(.17f,loop.Count).ToList(),8,steel,ZoneKit.Shape.Dim(steel,.86f),1,.3f,false);
                    foreach(int end in new[]{-1,1})wl.Slot(1).Box(W(wx,Down+.98f,z+end*2.9f),new Vector3(1.6f,.3f,.34f),Quaternion.identity,Teal,new Color(.3f,.66f,.68f));
                    seam.Slot(0).Box(W(wx-.62f,Down+1.04f,z-.9f),new Vector3(.2f,.2f,.7f),Quaternion.identity,Color.white);
                    // The carriage across the rails, its mast and torch down to the seam.
                    wl.Slot(0).Box(W(wx,Down+4.12f,z),new Vector3(4.1f,.46f,1.3f),Quaternion.identity,Pale,White);wl.Slot(1).Box(W(wx,Down+4.12f,z-.66f),new Vector3(3.6f,.14f,.04f),Quaternion.identity,Teal);
                    wl.Slot(2).Box(W(wx-.5f,Down+2.95f,z-.9f),new Vector3(.24f,2.2f,.24f),Quaternion.identity,Iron,IronPale);
                    wl.Slot(1).Box(W(wx-.5f,Down+1.82f,z-.9f),new Vector3(.42f,.36f,.42f),Quaternion.identity,Teal);
                    wl.Slot(2).Post(W(wx-.58f,Down+1.22f,z-.9f),.42f,.05f,.09f,8,Iron,Iron);
                    arc.Slot(0).Box(W(wx-.62f,Down+1.17f,z-.9f),new Vector3(.3f,.12f,.3f),Quaternion.Euler(0,45,0),Color.white);
                }
                // The festoon: power to each carriage hanging in loops along the east rail.
                for(int i=0;i<carriages.Length;i++)
                {
                    float z0=i==0?121.4f:carriages[i-1]+.6f,z1=carriages[i]-.6f;
                    wl.Slot(2).Rope(W(wx+1.8f,Down+4.25f,z0),W(wx+1.8f,Down+4.25f,z1),.9f,.05f,new Color(.12f,.13f,.15f));
                }
                foreach(float z in new[]{131.5f,149.5f})PointLight(W(wx-1.2f,Down+1.7f,z),7f,2.4f,new Color(.7f,.88f,1)).transform.SetParent(root.transform,true);
                var weld=Made(kit,"The welding line",wl,WhiteMat,TealMat,DarkMat);weld.transform.SetParent(root.transform,true);weld.isStatic=true;ZoneKit.Fadeable(weld);
                ZoneKit.Block(weld,W(wx,Down+.45f,140),new Vector3(2.4f,.9f,32),0,"Jig");foreach(float z in new[]{122f,158f})foreach(int side in new[]{-1,1})ZoneKit.Block(weld,W(wx+side*1.8f,Down+2.2f,z),new Vector3(.36f,4.4f,.36f),0,"Leg");
                var arcs=Made(kit,"The welding arcs",arc,ArcMat);arcs.transform.SetParent(weld.transform,true);arcs.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                var seams=Made(kit,"The seams, hot",seam,HotMat);seams.transform.SetParent(weld.transform,true);seams.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                var sparks=kit.Glow("Sparks",false,new Color(1.6f,1.4f,1.1f,1));
                foreach(float z in carriages){var spray=kit.Drift("Weld sparks",W(wx-.62f,Down+1.3f,z-.9f),new Vector3(1.4f,.6f,1.4f),sparks,new Color(1,.86f,.6f,.9f),36,.04f,.09f,1.1f,.5f);spray.transform.SetParent(root.transform,true);}
                // The welders, behind the line on its east side, bent over their sections (drawn only: the strip behind the jig is nobody's way).
                foreach(var (z,body,tint) in new[]{(127.4f,"SurvivorNatural",new Color(.96f,.98f,1)),(136.4f,"PatrolBody",new Color(.9f,.97f,1)),(145.4f,"Hal",new Color(.92f,.96f,1)),(154.4f,"SurvivorNatural",new Color(1,.95f,.9f))})
                    Figure(kit,"A welder at the line",body,On(kit,new Vector2(wx+2.3f,z-.6f)),-90,tint,"bend",0,root.transform);
                // ---- a link of the anchor's chain on rollers down the middle
                var rollers=new ZoneKit.Shape();
                foreach(float z in new[]{131f,137f,143f,149f,155f}){rollers.Slot(0).Box(W(0,Down+.25f,z),new Vector3(4.6f,.5f,.9f),Quaternion.identity,IronPale,Pale);rollers.Slot(1).Tube(new[]{W(-1.8f,Down+.62f,z),W(1.8f,Down+.62f,z)},new[]{.24f,.24f},12,Teal,Teal,1,.3f);}
                var bed=Made(kit,"The chain's rollers",rollers,WhiteMat,TealMat);bed.transform.SetParent(root.transform,true);bed.isStatic=true;
                var link=Model("Environment","AnchorChain",W(0,Down+.8f,143),2.4f,0,false,"A link of the anchor's chain");
                if(link!=null)
                {
                    link.transform.SetParent(bed.transform,true);var box=ModelGeometry.BoundsOf(link);
                    if(box.size.z<box.size.x){link.transform.RotateAround(box.center,Vector3.up,90);box=ModelGeometry.BoundsOf(link);}
                    float along=Mathf.Min(box.size.z,26);ZoneKit.Block(bed,W(0,Down+.8f+box.size.y*.5f,143),new Vector3(Mathf.Min(box.size.x,5),box.size.y+.8f,along),0,"Chain");
                }
                else ZoneKit.Block(bed,W(0,Down+.5f,143),new Vector3(4.6f,1,26),0,"Chain");
                ZoneKit.Block(bed,W(0,Down+.4f,143),new Vector3(4.6f,.8f,25),0,"Rollers");
                // ---- the clamp on its test stand
                var stand=new ZoneKit.Shape();var sat=W(10.4f,Down,129.6f);
                stand.Slot(0).Box(sat+Vector3.up*.3f,new Vector3(3.6f,.6f,3.6f),Quaternion.identity,IronPale,Pale);stand.Slot(1).Box(sat+W(0,.62f,0),new Vector3(3,.04f,3),Quaternion.identity,Teal);
                var testStand=Made(kit,"The clamp's test stand",stand,WhiteMat,TealMat);testStand.transform.SetParent(root.transform,true);testStand.isStatic=true;
                var clamp=Model("Environment","AnchorClamp",sat+Vector3.up*.6f,3.2f,200,false,"An anchor clamp on its test stand");
                if(clamp!=null){clamp.transform.SetParent(testStand.transform,true);var box=ModelGeometry.BoundsOf(clamp);ZoneKit.Block(testStand,box.center,new Vector3(Mathf.Min(box.size.x,3.6f),box.size.y,Mathf.Min(box.size.z,3.6f)),0,"Clamp");ZoneKit.Fadeable(testStand);}
                ZoneKit.Block(testStand,sat+Vector3.up*.3f,new Vector3(3.6f,.6f,3.6f),0,"Stand");
                PointLight(sat+W(-2,3,-2),8,1.2f,new Color(.7f,.95f,1)).transform.SetParent(root.transform,true);
                // ---- finished parts in low stacks on the west, two crews standing back by them
                var parts=new ZoneKit.Shape();var rng=new System.Random(53);
                foreach(var (x,z) in new[]{(-27f,118f),(-27f,124f),(-27f,140f),(-27f,146f),(-27f,152f)})
                    for(int layer=0;layer<2;layer++){var c=W(x+((float)rng.NextDouble()-.5f)*.4f,Down+.32f+layer*.64f,z);parts.Slot(0).Box(c,new Vector3(1.6f,.6f,2.2f),Quaternion.Euler(0,((float)rng.NextDouble()-.5f)*8,0),Pale,White);parts.Slot(1).Box(c+W(0,.1f,-1.12f),new Vector3(1.2f,.1f,.03f),Quaternion.identity,Teal);}
                var stacks=Made(kit,"Finished parts",parts,WhiteMat,TealMat);stacks.transform.SetParent(root.transform,true);stacks.isStatic=true;
                foreach(var (x,z) in new[]{(-27f,118f),(-27f,124f),(-27f,140f),(-27f,146f),(-27f,152f)})ZoneKit.Block(stacks,W(x,Down+.65f,z),new Vector3(1.8f,1.3f,2.4f),0,"Parts");
                Crew(kit,"A crew standing back, by the parts",new Vector2(-24.6f,121),100,4,true,"PatrolBody").transform.SetParent(root.transform,true);
                Crew(kit,"A crew standing back, by the parts",new Vector2(-24.4f,149.4f),70,2,true,"Mira").transform.SetParent(root.transform,true);
                Sight("standaside",W(-22,Down,132),11,"end.sawWorksAside");
                var gear=new ZoneKit.Shape();
                foreach(var (x,z,yaw) in new[]{(-16f,121f,10f),(8f,150f,-20f),(-20f,141f,80f),(14f,118f,0f)})
                {
                    var at=W(x,Down,z);var turn=Quaternion.Euler(0,yaw,0);
                    gear.Slot(0).Box(at+turn*W(0,.5f,0),new Vector3(1.4f,.12f,.8f),turn,Pale,White);gear.Slot(0).Box(at+turn*W(0,.85f,0),new Vector3(1.4f,.1f,.8f),turn,Pale,White);
                    foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})gear.Slot(1).Box(at+turn*W(sx*.62f,.45f,sz*.32f),new Vector3(.06f,.9f,.06f),turn,IronPale);
                    gear.Slot(2).Box(at+turn*W(-.2f,.98f,0),new Vector3(.5f,.16f,.4f),turn,Teal);gear.Slot(1).Box(at+turn*W(.35f,.95f,.1f),new Vector3(.3f,.1f,.2f),turn,Iron);
                }
                foreach(var (x,z) in new[]{(-22f,128f),(6f,124f),(-12f,152f)}){var at=W(x,Down,z);gear.Slot(0).Box(at+Vector3.up*.1f,new Vector3(1.2f,.2f,1.2f),Quaternion.identity,new Color(.62f,.64f,.66f));gear.Slot(2).Box(at+Vector3.up*.45f,new Vector3(1,.5f,1),Quaternion.Euler(0,x*7,0),Pale,White);}
                foreach(var (from,to) in new[]{(W(-20,Down,163.8f),W(-17,Down,158)),(W(-9.4f,Down,163.8f),W(-6.4f,Down,158.6f)),(W(10.4f,Down,128),W(7.4f,Down,125.6f)),(W(19.8f,Down,135),W(16.6f,Down,133.6f))})
                {gear.Slot(1).Tube(new[]{from+Vector3.up*.06f,Vector3.Lerp(from,to,.5f)+W(.4f,.06f,0),to+Vector3.up*.06f},new[]{.06f,.06f,.06f},6,Iron,Iron,1,.3f);gear.Slot(2).Disc(to+Vector3.up*.02f,Vector3.up,.24f,10,Teal,.1f,Pale);}
                var carts=Made(kit,"The works' carts and cables",gear,WhiteMat,DarkMat,TealMat);carts.transform.SetParent(root.transform,true);carts.isStatic=true;
                foreach(var (x,z,yaw) in new[]{(-16f,121f,10f),(8f,150f,-20f),(-20f,141f,80f),(14f,118f,0f)})ZoneKit.Block(carts,W(x,Down+.5f,z),new Vector3(1.4f,1,.8f),yaw,"Cart");
                foreach(var (x,z) in new[]{(-22f,128f),(6f,124f),(-12f,152f)})ZoneKit.Block(carts,W(x,Down+.35f,z),new Vector3(1.2f,.7f,1.2f),0,"Pallet");
                // ---- the works' lamps: a teal line along the north and east walls
                var lamp=new ZoneKit.Shape();for(float x=-28;x<=24;x+=4){if(Mathf.Abs(x)<4||WallTop(new Vector2(x,172),Vector2.up,Down)<4.8f)continue;lamp.Slot(0).Box(W(x,Down+4.3f,171.6f),new Vector3(2.2f,.12f,.12f),Quaternion.identity,Color.white);}
                var line=Made(kit,"The works' light line",lamp,LampTeal);line.transform.SetParent(root.transform,true);
                OnPlate(Painted(kit,"Anchor works","ANCHOR WORKS",W(13.4f,Down+4.6f,171.9f),Vector3.right,Vector3.back,8.6f,new Color(.18f,.46f,.5f)),"Works north");
                Painted(kit,"Section eleven","SECTION 11",W(-14.6f,Down+.03f,128),Vector3.right,Vector3.up,4.4f,new Color(.84f,.68f,.32f)).transform.SetParent(root.transform,true);
            }

            // ---------------------------------------------------------------- the command deck and the launch bay
            /// <summary>The command deck: the director's chart table glowing with the road and the convoy and the anchor on it, a crescent of consoles facing the windows onto the anchor, the
            /// empty chair, an officer at the chart standing back; the director's locker.</summary>
            static void CommandDeck(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var top=new ZoneKit.Shape();var c=On(kit,ChartAt);
                s.Slot(0).Post(c,.95f,2.1f,2.3f,28,TealDeep,new Color(.22f,.42f,.46f));s.Slot(1).Disc(c+Vector3.up*.97f,Vector3.up,2.36f,32,Gold,2.2f,Gold);
                top.Slot(0).Disc(c+Vector3.up*.975f,Vector3.up,2.2f,32,Color.white,0,null,1/4.4f);
                var table=Made(kit,"The director's chart",s,DarkMat,GoldMat);ZoneKit.Block(table,c+Vector3.up*.48f,new Vector3(4.4f,.96f,4.4f),0,"Chart table");table.isStatic=true;
                var chart=Made(kit,"The chart's glass",top,ChartMat);chart.transform.SetParent(table.transform,true);chart.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                PointLight(c+W(0,2.4f,0),9,1.8f,new Color(.4f,.9f,1));
                foreach(var (x,z) in new[]{(27.6f,182.4f),(28.8f,187.6f),(28.8f,193.6f),(27.6f,198.8f)}){var console=kit.Prop("DeckConsole",new Vector3(x,0,z),1.25f,90);console.name="A command console";console.AddComponent<PropTint>().tint=new Color(.9f,.98f,1);}
                // The empty chair, facing the windows.
                var ch=new ZoneKit.Shape();var seat=W(25,Down,190.6f);
                ch.Slot(0).Post(seat,.42f,.1f,.08f,8,IronPale,IronPale).Box(seat+W(0,.5f,0),new Vector3(.7f,.12f,.7f),Quaternion.identity,TealDeep,new Color(.2f,.44f,.48f)).Box(seat+W(-.32f,.95f,0),new Vector3(.12f,.9f,.7f),Quaternion.identity,TealDeep);
                ch.Slot(1).Box(seat+W(-.39f,1.36f,0),new Vector3(.03f,.08f,.6f),Quaternion.identity,Gold);
                var chair=Made(kit,"The director's chair, empty",ch,DarkMat,GoldMat);ZoneKit.Block(chair,seat+Vector3.up*.6f,new Vector3(.8f,1.2f,.8f),0,"Chair");chair.isStatic=true;
                PointLight(seat+W(.6f,2,0),5,1,new Color(1,.86f,.66f));
                Crew(kit,"An officer at the chart, standing back",new Vector2(18.4f,192.4f),110,1,true,"PatrolBody");
                Sight("standaside",W(20,Down,188),9,"end.sawCommandAside");
                // The director's locker, by the door to the bay side.
                var lk=new ZoneKit.Shape();var at=On(kit,CacheAt);
                lk.Slot(0).Box(Vector3.up*.6f,new Vector3(1.4f,1.2f,.8f),Quaternion.identity,Pale,White).Slot(1).Box(W(0,.9f,-.42f),new Vector3(1,.08f,.03f),Quaternion.identity,Teal);
                var locker=Made(kit,"The director's locker",lk,WhiteMat,TealMat);locker.transform.position=at;var box=locker.AddComponent<BoxCollider>();box.center=Vector3.up*.6f;box.size=new Vector3(1.4f,1.2f,.8f);locker.isStatic=true;
                var cache=LootCache.On(locker,"continuance.command","The director's locker",220,ProgressionBuilder.AnyPart(5,GearSlot.Frame,1),ProgressionBuilder.Drop("HaloRivet",2,1),ProgressionBuilder.Drop("RepairGel",1,1));cache.range=2.6f;
                OnPlate(Painted(kit,"Command","COMMAND",W(24,Down+4.2f,205.95f),Vector3.right,Vector3.back,5.2f,new Color(.84f,.68f,.32f)),"Command north");
            }
            /// <summary>The launch bay: two cutters up on their cradles (umbilicals in), the director's cradle empty with her craft's outline still on its plate and its guide lamps lit, the
            /// pad's console where a deck hand holds it ready, floods on stands, the bay's numbers on its east wall, and its mouth open to space behind a field.</summary>
            static void LaunchBay(ZoneKit kit)
            {
                var root=new GameObject("The launch bay");
                foreach(var (z,n) in new[]{(208f,1),(231f,2)})
                {
                    var s=new ZoneKit.Shape();var c=W(-18,Down,z);
                    foreach(int side in new[]{-1,1})s.Slot(0).Box(c+W(side*2.6f,.35f,0),new Vector3(.6f,.7f,14),Quaternion.identity,IronPale,Pale);
                    foreach(float dz in new[]{-5.6f,0,5.6f})s.Slot(0).Box(c+W(0,.3f,dz),new Vector3(5.8f,.5f,.6f),Quaternion.identity,IronPale,Pale);
                    s.Slot(1).Box(c+W(0,.72f,0),new Vector3(.6f,.04f,13),Quaternion.identity,Teal);
                    s.Slot(1).Tube(new[]{c+W(3.2f,.1f,-2),c+W(5.6f,.1f,-1),c+W(6.6f,1.4f,-.6f)},new[]{.12f,.12f,.12f},8,Teal,Teal,1,.4f);
                    var cradle=Made(kit,"Bay cradle "+n,s,WhiteMat,TealMat);cradle.transform.SetParent(root.transform,true);cradle.isStatic=true;ZoneKit.Block(cradle,c+Vector3.up*.4f,new Vector3(6.4f,.8f,14.2f),0,"Cradle");
                    var cutter=Model("Ships","CompactCutter",c+Vector3.up*.8f,13,0,true,"A cutter on bay cradle "+n);
                    if(cutter!=null){cutter.transform.SetParent(cradle.transform,true);var box=ModelGeometry.BoundsOf(cutter);ZoneKit.Block(cradle,box.center,new Vector3(Mathf.Min(box.size.x,5),box.size.y,Mathf.Min(box.size.z,13)),0,"Cutter");ZoneKit.Fadeable(cradle);}
                    var t=new ZoneKit.Shape();var tower=c+W(-4.6f,0,0);
                    t.Slot(0).Box(tower+Vector3.up*2.6f,new Vector3(1,5.2f,1),Quaternion.identity,Pale,White);t.Slot(0).Box(tower+W(1.6f,4.9f,0),new Vector3(3.2f,.4f,.6f),Quaternion.identity,Pale,White);
                    t.Slot(1).Box(tower+W(0,3.6f,-.52f),new Vector3(.8f,.3f,.04f),Quaternion.identity,Teal);t.Slot(1).Tube(new[]{tower+W(3,4.7f,0),tower+W(3.4f,3.4f,0),tower+W(3.6f,2.4f,.4f)},new[]{.1f,.1f,.1f},8,Teal,Teal,1,.4f);
                    var served=Made(kit,"Bay "+n+" service tower",t,WhiteMat,TealMat);served.transform.SetParent(root.transform,true);served.isStatic=true;ZoneKit.Fadeable(served);ZoneKit.Block(served,tower+Vector3.up*2.6f,new Vector3(1,5.2f,1),0,"Service tower");
                    Painted(kit,"Bay "+n,"BAY "+n,W(-18,Down+.03f,z-8.4f),Vector3.right,Vector3.up,3,new Color(.84f,.68f,.32f)).transform.SetParent(root.transform,true);
                }
                // The director's cradle, empty: its plate with her craft's outline in gold, guide lamps down both sides, the console of the pad at its south end.
                var e=new ZoneKit.Shape();var guide=new ZoneKit.Shape();var ec=On(kit,EmptyCradleAt);
                e.Slot(0).Box(ec+Vector3.up*.08f,new Vector3(9,.16f,11),Quaternion.identity,IronPale,new Color(.86f,.88f,.9f));
                var outline=new[]{W(0,0,4.6f),W(3.4f,0,-1.6f),W(4.2f,0,-3.4f),W(2,0,-4),W(0,0,-3.2f),W(-2,0,-4),W(-4.2f,0,-3.4f),W(-3.4f,0,-1.6f),W(0,0,4.6f)};
                for(int i=1;i<outline.Length;i++)e.Slot(1).Beam(ec+outline[i-1]+Vector3.up*.17f,ec+outline[i]+Vector3.up*.17f,.12f,.01f,Gold);
                foreach(int side in new[]{-1,1})for(float z=-4.6f;z<=4.61f;z+=1.15f)guide.Slot(0).Box(ec+W(side*4.2f,.18f,z),new Vector3(.3f,.04f,.5f),Quaternion.identity,Color.white);
                var empty=Made(kit,"The director's cradle, empty",e,WhiteMat,GoldMat);empty.transform.SetParent(root.transform,true);empty.isStatic=true;ZoneKit.Block(empty,ec+Vector3.up*.08f,new Vector3(9,.16f,11),0,"Cradle plate");
                var lit=Made(kit,"The director's cradle's guide lamps",guide,LampTeal);lit.transform.SetParent(empty.transform,true);lit.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                Painted(kit,"Director's mark","VENN",ec+W(0,.19f,1.4f),Vector3.right,Vector3.up,2.4f,new Color(.84f,.68f,.32f)).transform.SetParent(empty.transform,true);
                var pad=kit.Prop("DeckConsole",new Vector3(PadAt.x+2.4f,0,PadAt.y-.4f),1.3f,-90);pad.name="The launch console";pad.AddComponent<PropTint>().tint=new Color(.9f,1,1);pad.transform.SetParent(root.transform,true);
                Crew(kit,"A deck hand holding the cradle ready",new Vector2(PadAt.x+3.9f,PadAt.y+1.8f),-100,0,true,"SurvivorNatural").transform.SetParent(root.transform,true);
                Sight("helpers",On(kit,PadAt),8,"end.sawCradleHeld");
                Crew(kit,"A deck hand standing back, at the cutters",new Vector2(-11.6f,214),-70,3,true,"Neve").transform.SetParent(root.transform,true);
                Crew(kit,"A deck hand standing back, at the cutters",new Vector2(-12,227),-110,1,true,"PatrolBody").transform.SetParent(root.transform,true);
                // Floods on stands, cold.
                var f=new ZoneKit.Shape();
                foreach(var (x,z,yaw) in new[]{(-26f,200f,40f),(-26f,244f,140f),(9.6f,206f,-60f)}){var foot=W(x,Down,z);f.Slot(0).Post(foot,4.2f,.14f,.1f,8,IronPale,Pale).Box(foot+W(0,4.3f,0),new Vector3(1.1f,.6f,.5f),Quaternion.Euler(30,yaw,0),IronPale,White);PointLight(foot+W(Mathf.Sin(yaw*Mathf.Deg2Rad)*2,3.6f,Mathf.Cos(yaw*Mathf.Deg2Rad)*2),14,2,new Color(.9f,.95f,1)).transform.SetParent(root.transform,true);}
                var floods=Made(kit,"The bay's floods",f,WhiteMat);floods.transform.SetParent(root.transform,true);floods.isStatic=true;ZoneKit.Fadeable(floods);
                foreach(var (x,z) in new[]{(-26f,200f),(-26f,244f),(9.6f,206f)})ZoneKit.Block(floods,W(x,Down+2.1f,z),new Vector3(.4f,4.2f,.4f),0,"Flood post");
                OnPlate(Painted(kit,"Launch bay","LAUNCH BAY 1",W(11.95f,Down+4.6f,222),Vector3.back,Vector3.left,9,new Color(.18f,.46f,.5f)),"Bay east");
                // The mouth: jambs, a lip, the field across it, and the stop behind it (there is nothing beyond but space).
                var m=new ZoneKit.Shape();
                foreach(float x in new[]{-27.6f,11.6f})m.Slot(0).Box(W(x,Down+3.4f,251.6f),new Vector3(1.2f,6.8f,1.2f),Quaternion.identity,Pale,White);
                for(float x=-21.6f;x<11;x+=6)m.Slot(0).Box(W(x,Down+.3f,251.6f),new Vector3(.3f,.6f,.5f),Quaternion.identity,IronPale,Pale);
                m.Slot(1).Box(W(-8,Down+.47f,251.4f),new Vector3(38.4f,.06f,.12f),Quaternion.identity,Teal);
                var mouth=Made(kit,"The bay's mouth",m,WhiteMat,TealMat);mouth.transform.SetParent(root.transform,true);mouth.isStatic=true;ZoneKit.Fadeable(mouth);
                foreach(float x in new[]{-27.6f,11.6f})ZoneKit.Block(mouth,W(x,Down+3.4f,251.6f),new Vector3(1.2f,6.8f,1.2f),0,"Jamb");
                ZoneKit.Block(mouth,W(-8,Down+1.6f,252.1f),new Vector3(38.4f,3.2f,.6f),0,"The field's stop");
                var field=kit.Glow("Bay field",true,new Color(.5f,1.25f,1.45f,1));
                var curtains=new GameObject("The bay's field");curtains.transform.SetParent(root.transform,true);
                for(int i=0;i<8;i++)
                {
                    var curtain=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(curtain.GetComponent<Collider>());curtain.name="The bay's field "+(i+1);curtain.transform.SetParent(curtains.transform,true);
                    curtain.transform.SetPositionAndRotation(W(-27.2f+2.4f+i*4.8f,Down+2.4f,251.7f),Quaternion.identity);curtain.transform.localScale=new Vector3(4.8f,4.8f,1);
                    var view=curtain.GetComponent<Renderer>();view.sharedMaterial=field;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
                }
                PointLight(W(-8,Down+2,249),16,1.4f,new Color(.5f,.9f,1)).transform.SetParent(root.transform,true);
                Convoy(kit,new[]{("Hauler",W(-12,-28,292),36f,90f,true),("Skiff",W(14,-14,272),12f,60f,true),("PatrolCutter",W(-34,-36,302),16f,120f,true),("GreatLantern",W(22,-42,320),12f,0f,false),
                    ("RingSegmentA",W(-56,-62,334),40f,30f,true),("CompactCutter",W(-2,-18,268),4f,150f,false)},"Beyond the bay's mouth");
            }

            // ---------------------------------------------------------------- the ship's people at their day (the look pass)
            /// <summary>The flagship of a people who do everything well, on the day their plan is failing (the look pass of November 14: the decks were nearly empty). Off watch, the crew
            /// deck's people have left their meals half eaten to stand at the windows and watch the convoy; two old hands eat on; three sleep in the bunks; a second cook works the galley.
            /// On watch, techs at deck four's benches and its drive stand, operators at the presses and the sluices, officers at the command deck's consoles. All drawn only (see
            /// <see cref="Figure"/>), none on a way, none between the camera and a floor anyone fights on.</summary>
            static void Aboard(ZoneKit kit)
            {
                var root=new GameObject("The ship's people at their day").transform;
                // ---- the crew deck: at the windows, watching the convoy go by (the east edge of the deck; the mess fight's box stops at x 29)
                foreach(var (x,z,yaw,body,pose,k) in new[]{(30.6f,15.4f,84f,"Hal","",0),(30.75f,17.8f,98f,"Mira","hand",1),(30.55f,26.6f,88f,"SurvivorNatural","",2),(30.7f,33f,92f,"Neve","",3),
                    (30.6f,35.4f,78f,"PatrolBody","",1),(30.7f,42.6f,96f,"LumeBody","",4)})
                    Figure(kit,"A crew at the windows, watching the convoy",body,On(kit,new Vector2(x,z)),yaw,Coats[k],pose==""?null:pose,.48f,root);
                // ---- the mess: two still eating at the north table (inside its block, their legs under it), the meals left on all three
                foreach(var (x,body,k) in new[]{(16.4f,"PatrolBody",3),(21.6f,"SurvivorNatural",2)})Figure(kit,"A crew still at their meal",body,On(kit,new Vector2(x,36.8f)),180,Coats[k],"sit",.48f,root);
                var rng=new System.Random(97);Color[] food={new(.78f,.56f,.3f),new(.5f,.62f,.32f),new(.82f,.42f,.22f),new(.9f,.84f,.66f),new(.62f,.34f,.24f)};
                foreach(float tz in new[]{20f,28f,36f})
                {
                    var m=new ZoneKit.Shape();var c=W(19,Up,tz);
                    for(float x=-5;x<=5.01f;x+=1.25f)
                    {
                        var plate=c+W(x+.35f,.82f,.28f);int roll=rng.Next(10);
                        // Half a meal on most plates, a crust on some, one or two scraped clean; a bowl beside one in three; a cup knocked over now and then.
                        // (A lump's seed stays small: a seed of rng.Next()'s size threw Perlin noise a billion metres off and gave each meal NaN bounds.)
                        if(roll<7)for(int b=0;b<2+rng.Next(3);b++)m.Slot(0).Lump(plate+W(((float)rng.NextDouble()-.5f)*.16f,.02f,((float)rng.NextDouble()-.5f)*.14f),new Vector3(.05f,.025f,.045f),rng.Next(1000),.3f,food[rng.Next(food.Length)],ZoneKit.Shape.Dim(food[0],.7f),6,3);
                        if(roll%3==0){var bowl=c+W(x-.2f,.8f,.05f);m.Slot(1).Post(bowl,.08f,.06f,.1f,10,White,White);m.Slot(0).Disc(bowl+Vector3.up*.075f,Vector3.up,.085f,10,food[2],0,null);}
                        if(roll==4)m.Slot(1).Tube(new[]{c+W(x,.85f,-.32f),c+W(x+.13f,.85f,-.26f)},new[]{.045f,.05f},8,White,White,1,.3f);
                        if(roll==2||roll==8)m.Slot(0).Box(c+W(x+.3f,.81f,-.05f),new Vector3(.08f,.04f,.16f),Quaternion.Euler(0,rng.Next(-40,40),0),food[3]);
                    }
                    m.Slot(1).Post(c+W(-1.6f,.8f,0),.26f,.09f,.07f,10,TealDeep,Teal);m.Slot(1).Box(c+W(2.6f,.81f,-.1f),new Vector3(.5f,.02f,.36f),Quaternion.Euler(0,8,0),IronPale,Pale);
                    Made(kit,"A meal left half eaten "+tz,m,FabricMat,WhiteMat).transform.SetParent(root,true);
                }
                // ---- the galley: a second cook behind the counter, bent over the pans
                Figure(kit,"A cook behind the galley counter","LumeBody",On(kit,new Vector2(24.6f,49.68f)),180,Coats[2],"bend",0,root);
                // ---- the bunks: three asleep through it, each under a blanket, the head on the pillow (inside the bunk rows' blocks)
                var sleep=new ZoneKit.Shape();
                foreach(var (bx,bz,upper,body,k) in new[]{(-10.7f,18f,false,"Hal",0),(-13.1f,29f,true,"Mira",1),(-8.3f,40f,true,"SurvivorNatural",2)})
                {
                    float top=Up+(upper?1.36f:.42f)+.18f;
                    Figure(kit,"A crew asleep in the bunks",body,W(bx+.86f,top+.1f,bz),0,Coats[k],"lie",0,root);
                    sleep.Slot(0).Lump(W(bx+.16f,top+.19f,bz),new Vector3(.88f,.21f,.47f),k*7+3,.18f,new[]{new Color(.24f,.54f,.56f),new Color(.7f,.62f,.52f),new Color(.3f,.46f,.6f)}[k],new Color(.16f,.36f,.42f),12,6);
                }
                Made(kit,"Blankets over the sleepers",sleep,FabricMat).transform.SetParent(root,true);
                // ---- deck four: techs at the benches on the north wall and at the cutter's drive on its stand (off the captain's line and his crew's way out)
                Figure(kit,"A tech at the benches","SurvivorNatural",On(kit,new Vector2(-18,101.5f)),0,Coats[0],"bend",0,root);
                Figure(kit,"A tech at the benches","Neve",On(kit,new Vector2(14,101.5f)),0,Coats[1],"bend",0,root);
                Figure(kit,"A tech at the drive's test stand","Hal",On(kit,new Vector2(16.4f,93.3f)),0,Coats[3],"bend",0,root);
                // ---- the anchor works: an operator at each press's bed and a hand at the sluices' valves (along the north wall, behind any fight)
                Figure(kit,"An operator at press 1","SurvivorNatural",On(kit,new Vector2(-16.7f,163.1f)),-30,Coats[1],"bend",0,root);
                Figure(kit,"An operator at press 2","PatrolBody",On(kit,new Vector2(-12.6f,162.9f)),30,Coats[4],"bend",0,root);
                Figure(kit,"A hand at the sluices' valves","Neve",On(kit,new Vector2(9.4f,168.9f)),0,Coats[0],null,0,root);
                // ---- the command deck: officers at two of the consoles along the windows
                Figure(kit,"An officer at a console","PatrolBody",On(kit,new Vector2(27.7f,187.6f)),90,Coats[1],"bend",0,root);
                Figure(kit,"An officer at a console","Mira",On(kit,new Vector2(26.5f,198.8f)),90,Coats[2],null,0,root);
            }

            // ---------------------------------------------------------------- walls, windows and what is let into the floor
            static readonly Dictionary<string,GameObject> Plates=new();
            /// <summary>Hangs a fixture on the wall plate it is fixed to, so it is part of that wall (a word painted a few metres up a wall, measured alone, is a thing floating in the air).</summary>
            static GameObject OnPlate(GameObject fixture,string wall){if(fixture==null)return null;if(Plates.TryGetValue(wall,out var plate))fixture.transform.SetParent(plate.transform,true);else Debug.LogWarning("CONTINUANCE_FIXTURE_LOOSE "+fixture.name+" ("+wall+")");return fixture;}
            /// <summary>Each deck's own plate on the walls the camera sees the inside of (north and east): white panels with the builder's signature on each, a teal band at hand height, a gold
            /// line over it, a light strip under the top; no higher than the plating drawn behind it (D272). The windows' stretches are left open.</summary>
            static void Skins(ZoneKit kit)
            {
                Plates.Clear();int walls=0;
                Color Paint(Room r)=>r switch{Room.Crew or Room.Hatch=>new Color(.96f,.94f,.9f),Room.DeckFour or Room.DeckDoor=>new Color(.88f,.92f,.94f),Room.Works=>new Color(.82f,.86f,.88f),Room.Command=>new Color(.42f,.54f,.58f),_=>new Color(.92f,.94f,.95f)};
                var glow=new ZoneKit.Shape();
                void Wall(string name,Vector2 a,Vector2 b,Vector2 outward,Room room,float level)
                {
                    if(Vector2.Dot(outward,Away)<.05f)return;
                    var s=new ZoneKit.Shape();var paint=Paint(room);float length=Vector2.Distance(a,b);int steps=Mathf.Max(1,Mathf.RoundToInt(length/2.4f));bool any=false;var inward=new Vector3(-outward.x,0,-outward.y);
                    for(int i=0;i<steps;i++)
                    {
                        var p0=Vector2.Lerp(a,b,i/(float)steps);var p1=Vector2.Lerp(a,b,(i+1)/(float)steps);var m=(p0+p1)*.5f;
                        if(Walkable(m+outward*1.1f)||Sill(m+outward*.4f,out _))continue;
                        float top=Mathf.Min(Seen(m+outward*1.5f),Seen(p0+outward*1.5f),Seen(p1+outward*1.5f))-level-.1f;if(top<.6f)continue;top=Mathf.Min(top,6.8f);any=true;
                        var f0=new Vector3(p0.x,level-.05f,p0.y)+inward*.02f;var f1=new Vector3(p1.x,level-.05f,p1.y)+inward*.02f;
                        s.Slot(0).Quad(f0,f1,f1+Vector3.up*top,f0+Vector3.up*top,inward,paint,1/2.4f);
                        s.Slot(1).Box(f0+Vector3.up*top*.5f+inward*.08f,new Vector3(.16f,top,.14f),Quaternion.LookRotation(inward),ZoneKit.Shape.Dim(paint,.82f),paint);
                        if(top>1.3f)s.Slot(2).Box((f0+f1)*.5f+Vector3.up*1.1f+inward*.05f,new Vector3(Vector3.Distance(f0,f1),.22f,.08f),Quaternion.LookRotation(inward),room==Room.Command?Gold:Teal);
                        if(top>2.6f)s.Slot(3).Box((f0+f1)*.5f+Vector3.up*2.4f+inward*.04f,new Vector3(Vector3.Distance(f0,f1),.05f,.06f),Quaternion.LookRotation(inward),Gold);
                        if(top>3.6f)glow.Slot(0).Box((f0+f1)*.5f+Vector3.up*(top-.45f)+inward*.06f,new Vector3(Vector3.Distance(f0,f1)-.5f,.08f,.06f),Quaternion.LookRotation(inward),Color.white);
                    }
                    if(!any)return;
                    var drawn=Made(kit,"Wall plate "+name,s,PlateMat,WhiteMat,TealMat,GoldMat);drawn.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;walls++;Plates[name]=drawn;
                }
                foreach(var (room,x0,x1,z0,z1,round) in Boxes)
                {
                    if(room==Room.Stair||room==Room.WorksDoor||room==Room.North||room==Room.CommandHall||room==Room.DeckDoor)continue;
                    float level=Level(new Vector2((x0+x1)*.5f,(z0+z1)*.5f),room);
                    Wall(room+" north",new Vector2(x0+round,z1),new Vector2(x1-round,z1),Vector2.up,room,level);
                    Wall(room+" east",new Vector2(x1,z0+round),new Vector2(x1,z1-round),Vector2.right,room,level);
                }
                var strips=Made(kit,"Wall light strips",glow,LampTeal);strips.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                Debug.Log("CONTINUANCE_WALLS "+walls);
            }
            /// <summary>The windows in the hull's outer edge: mullions on the sill every three metres and a half, a head beam, the sill's cap, a stop in each opening (nothing beyond but space),
            /// and outside, the convoy past the crew deck and the anchor past the command deck.</summary>
            static void Windows(ZoneKit kit)
            {
                foreach(var (z0,z1,level) in EastWindows)
                {
                    var s=new ZoneKit.Shape();float x=31.75f;
                    int panes=Mathf.Max(1,Mathf.RoundToInt((z1-z0)/3.4f));
                    for(int i=0;i<=panes;i++){float z=Mathf.Lerp(z0,z1,i/(float)panes);s.Slot(0).Box(W(x,level+2.2f,z),new Vector3(.4f,4.4f,.3f),Quaternion.identity,Pale,White);}
                    s.Slot(0).Box(W(x,level+4.5f,(z0+z1)*.5f),new Vector3(.6f,.4f,z1-z0+.3f),Quaternion.identity,Pale,White);
                    s.Slot(1).Box(W(x-.18f,level+SillTall+.04f,(z0+z1)*.5f),new Vector3(.5f,.08f,z1-z0),Quaternion.identity,Teal);
                    s.Slot(1).Box(W(x-.2f,level+4.25f,(z0+z1)*.5f),new Vector3(.06f,.06f,z1-z0),Quaternion.identity,Gold);
                    var frame=Made(kit,"Windows "+(level>-1?"crew deck":"command deck"),s,WhiteMat,TealMat);frame.isStatic=true;ZoneKit.Fadeable(frame);
                    ZoneKit.Block(frame,W(x+.1f,level+2.4f,(z0+z1)*.5f),new Vector3(.6f,4,z1-z0),0,"Window stop");
                }
                Convoy(kit,new[]{("Hauler",W(72,-28,32),34f,8f,true),("Skiff",W(52,-16,44),12f,-30f,true),("PatrolCutter",W(60,-40,8),16f,20f,true),("CrawlTread",W(104,-58,24),12f,-20f,false),
                    ("ReedRaft",W(86,-46,56),5f,0f,false),("CompactCutter",W(46,-10,22),4f,-60f,false)},"Past the crew deck's windows");
                Convoy(kit,new[]{("StillwaterClamp",W(86,-58,192),56f,-90f,false),("AnchorChain",W(62,-30,180),46f,60f,true),("AnchorChain",W(64,-34,206),46f,120f,true),("PatrolCutter",W(50,-14,198),12f,180f,true)},"The anchor past the command deck");
            }
            /// <summary>What is let into the deck, flush with it (under a marking's lift, never solid): a teal line either side of the main walk, a white band at every door, the bay's guide
            /// lines to its cradles, deck four's bay outline round the cutter, and the works' safe lines round its machines. So no deck is one plain sheet.</summary>
            static void FloorWork(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var glow=new ZoneKit.Shape();
                void Line(Vector2 a,Vector2 b,float wide,Color tone,bool lit=false)
                {
                    var p0=On(kit,a,.012f);var p1=On(kit,b,.012f);
                    if(lit)glow.Slot(0).Beam(p0,p1,wide,.02f,Color.white);else s.Slot(0).Beam(p0,p1,wide,.024f,ZoneKit.Shape.Dim(tone,.9f),tone);
                }
                // The main walk's guide lines, lit.
                var main=new[]{new Vector2(0,-6),new Vector2(0,8)};foreach(int side in new[]{-1,1})Line(main[0]+new Vector2(side*1.8f,0),main[1]+new Vector2(side*1.8f,0),.1f,Teal,true);
                foreach(int side in new[]{-1,1})Line(new Vector2(side*1.8f,60),new Vector2(side*1.8f,103),.1f,Teal,true);
                foreach(int side in new[]{-1,1})Line(new Vector2(side*1.8f,173),new Vector2(side*1.8f,195),.1f,Teal,true);
                // White bands at the doors.
                foreach(var (x,z,w) in new[]{(0f,9.6f,9.4f),(0f,50.8f,5.6f),(0f,59.6f,5.6f),(0f,103.6f,5.6f),(0f,113.6f,5.6f),(0f,170.6f,5.6f),(0f,195.6f,5.6f),(13.6f,185.5f,6f)})
                    for(int k=0;k<3;k++)Line(new Vector2(x-w*.5f,z+k*.36f),new Vector2(x+w*.5f,z+k*.36f),.16f,new Color(.92f,.94f,.95f));
                // Deck four's bay round the cutter, in gold; the works' safe lines round the presses and the chain.
                void Rect(Vector2 c,Vector2 size,Color tone,float wide=.14f){var h=size*.5f;var pts=new[]{c+new Vector2(-h.x,-h.y),c+new Vector2(h.x,-h.y),c+new Vector2(h.x,h.y),c+new Vector2(-h.x,h.y)};for(int i=0;i<4;i++)Line(pts[i],pts[(i+1)%4],wide,tone);}
                Rect(new Vector2(-13,82),new Vector2(9,17),Gold);Painted(kit,"Deck four bay D","BAY D",W(6,Down+.03f,73.4f),Vector3.right,Vector3.up,2.6f,new Color(.84f,.68f,.32f));Painted(kit,"Deck four bay E","BAY E",W(16,Down+.03f,73.4f),Vector3.right,Vector3.up,2.6f,new Color(.84f,.68f,.32f));Rect(new Vector2(-20,165.4f),new Vector2(7.6f,6),Gold);Rect(new Vector2(-9.4f,165.4f),new Vector2(7.6f,6),Gold);Rect(new Vector2(0,143),new Vector2(6.6f,28),Teal);
                Rect(new Vector2(21.6f,140),new Vector2(5,36),Gold);Rect(new Vector2(-18,208),new Vector2(8,17),Teal);Rect(new Vector2(-18,231),new Vector2(8,17),Teal);
                // The bay's guide lines from its mouth to each cradle, lit.
                foreach(var x in new[]{-18f,2f})Line(new Vector2(x,239.6f+(x<0?0:10)),new Vector2(x,250.8f),.18f,Teal,true);
                // The crew deck's carpet runner down the mess (warm), the command deck's ring round the chart (gold).

                var ring=new List<Vector3>();for(int k=0;k<=32;k++){float a=k*Mathf.PI*2/32;ring.Add(On(kit,ChartAt+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*3.4f,.014f));}
                for(int k=1;k<ring.Count;k++)s.Slot(0).Beam(ring[k-1],ring[k],.1f,.02f,Gold);
                // The Compact's hazard band (teal and white, not yellow and black) before every machine that moves, a work cell round each stand, chevrons down the bay's launch lanes.
                void Band(Vector2 a,Vector2 b,float wide)
                {
                    var along=(b-a);float length=along.magnitude;along/=length;var across=new Vector2(along.y,-along.x);int k=0;
                    for(float d=.3f;d<length-.2f;d+=.6f,k++){var c=a+along*d;var at=On(kit,c,.014f);var turn=Quaternion.LookRotation(new Vector3(along.x,0,along.y))*Quaternion.Euler(0,35,0);
                        s.Slot(0).Box(at,new Vector3(wide*.9f,.024f,.34f),turn,k%2==0?Teal:new Color(.94f,.95f,.96f));}
                }
                Band(new Vector2(-24,161.4f),new Vector2(-5.6f,161.4f),1.1f);Band(new Vector2(5.2f,160.4f),new Vector2(21.6f,160.4f),1);Band(new Vector2(19,121),new Vector2(19,159),.9f);
                Band(new Vector2(-17.4f,72.6f),new Vector2(-8.6f,72.6f),1);Band(new Vector2(-17.4f,91.4f),new Vector2(-8.6f,91.4f),1);
                Rect(new Vector2(10.4f,129.6f),new Vector2(6,6),Gold);Rect(new Vector2(-26,135),new Vector2(5,40),Teal);Rect(new Vector2(13.4f,165),new Vector2(15,6),Teal);
                Rect(new Vector2(6,80),new Vector2(9,16),Gold);Rect(new Vector2(16,80),new Vector2(9,16),Gold);Rect(new Vector2(16,94),new Vector2(5,5),Gold);
                foreach(float x in new[]{-18f,2f})for(float z=x<0?241f:251f;z<250.6f;z+=2.4f){var c=On(kit,new Vector2(x,z),.016f);s.Slot(0).Triangle(c+W(-.9f,0,-.5f),c+W(.9f,0,-.5f),c+W(0,0,.7f),Vector3.up,Teal);}
                for(float z=200;z<=236;z+=4){var c=On(kit,new Vector2(-2,z),.016f);s.Slot(0).Triangle(c+W(-.8f,0,-.45f),c+W(.8f,0,-.45f),c+W(0,0,.65f),Vector3.up,new Color(.94f,.95f,.96f));}
                for(int k=0;k<12;k++){float a=k*Mathf.PI*2/12;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Line(ChartAt+d*3.6f,ChartAt+d*6.4f,.06f,Gold);}
                // The works' floor is an assembly floor and the bay's a launch deck: a layout grid painted on each (four metres in the works, six in the bay), as a yard that builds to drawings marks its floor.
                for(float x=-28;x<=24.01f;x+=4)Line(new Vector2(x,113.4f),new Vector2(x,170.6f),.05f,new Color(.36f,.5f,.52f));
                for(float z=116;z<=168.01f;z+=4)Line(new Vector2(-29.2f,z),new Vector2(25.2f,z),.05f,new Color(.36f,.5f,.52f));
                for(float x=-26;x<=10.01f;x+=6)Line(new Vector2(x,196.2f),new Vector2(x,250.6f),.06f,new Color(.86f,.88f,.9f));
                for(float z=198;z<=250.01f;z+=6)Line(new Vector2(-27.2f,z),new Vector2(11.2f,z),.06f,new Color(.86f,.88f,.9f));
                // The look pass (November 14), over the floors painted in the deck's vertices (Floors): each laydown bay's corners marked and the bay numbered; the chain trolley's
                // rails let into its runway; a drain along the sluices' trough; a gold line round each mess rug; the muster marks before the duty board, a square for each of a watch;
                // a mat before every bunk, each its sleeper's own colour.
                void Corners(Vector2 min,Vector2 max,float arm,Color tone){foreach(var (c,dx,dz) in new[]{(min,1,1),(new Vector2(max.x,min.y),-1,1),(max,-1,-1),(new Vector2(min.x,max.y),1,-1)}){Line(c,c+new Vector2(dx*arm,0),.16f,tone);Line(c,c+new Vector2(0,dz*arm),.16f,tone);}}
                foreach(var (min,max,word) in new[]{(new Vector2(-21,129),new Vector2(-12,137.4f),"L1"),(new Vector2(-21,141.4f),new Vector2(-12,149.8f),"L2"),(new Vector2(5.6f,136.6f),new Vector2(14.6f,148.6f),"L3"),(new Vector2(4.4f,114.6f),new Vector2(13.6f,123.4f),"L4"),(new Vector2(-19.6f,114.6f),new Vector2(-12.4f,123.4f),"L5")})
                {Corners(min+Vector2.one*.3f,max-Vector2.one*.3f,1.6f,Gold);Painted(kit,"Laydown "+word,word,On(kit,new Vector2(min.x+1.8f,max.y-1.3f),.03f),Vector3.right,Vector3.up,1.8f,new Color(.84f,.68f,.32f));}
                foreach(int side in new[]{-1,1})Line(new Vector2(side*2.2f,125.9f),new Vector2(side*2.2f,160.1f),.16f,Iron);
                Line(new Vector2(6.2f,162.6f),new Vector2(20.6f,162.6f),.44f,new Color(.16f,.2f,.24f));for(float x=6.6f;x<20.4f;x+=.5f)Line(new Vector2(x,162.42f),new Vector2(x,162.78f),.06f,IronPale);
                foreach(float z in new[]{20f,28f,36f})Rect(new Vector2(19,z),new Vector2(12.6f,3.6f),Gold,.09f);
                Rect(new Vector2(-2.4f,39),new Vector2(5,10.2f),Gold,.09f);foreach(int side in new[]{-1,1})Line(new Vector2(8+side*1.18f,14),new Vector2(8+side*1.18f,44),.08f,Gold);
                foreach(int side in new[]{-1,1})Line(new Vector2(-6.2f,155+side*1.45f),new Vector2(17.4f,155+side*1.45f),.12f,new Color(.92f,.94f,.95f));foreach(var (min,max) in new[]{(new Vector2(-17.2f,22.3f),new Vector2(-6.4f,24.7f)),(new Vector2(-17.2f,33.3f),new Vector2(-6.4f,35.7f))})Rect((min+max)*.5f,max-min-Vector2.one*.3f,Gold,.06f);
                for(float z=44.2f;z<47.2f;z+=1.2f)for(float x=6.4f;x<12.8f;x+=1.24f)s.Slot(0).Box(On(kit,new Vector2(x,z),.014f),new Vector3(.46f,.024f,.46f),Quaternion.identity,TealDeep);
                Painted(kit,"Muster","MUSTER",On(kit,new Vector2(9.5f,43.6f),.03f),Vector3.right,Vector3.up,3.4f,new Color(.84f,.68f,.32f));
                Color[] mats={new(.24f,.54f,.56f),new(.7f,.62f,.52f),new(.3f,.46f,.6f),new(.62f,.4f,.34f),new(.16f,.36f,.42f)};int mat=0;
                foreach(float z in new[]{18f,29f,40f})for(float x=-15.5f;x<=-8.2f;x+=2.4f)s.Slot(0).Box(On(kit,new Vector2(x,z-1.3f),.014f),new Vector3(1.5f,.024f,.76f),Quaternion.Euler(0,(mat*37%9)-4,0),mats[mat++%mats.Length]);
                var work=Made(kit,"Let into the deck",s,TealMat);work.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                var lit=Made(kit,"Guide lines",glow,LampTeal);lit.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }

            // ---------------------------------------------------------------- the hull from outside (the flight berth)
            /// <summary>The hull's back as the flight berth sees it, three metres under the ships and over every deck: its plate with the Compact's teal bands across it and its name, the open
            /// launch hatch with its doors laid back, the anchor clamps' three seats, vents and hatches low on it, lit windows down both flanks; north of every lane the shelter's wings, the
            /// superstructure stepping up with its lit bands, the frame gantry with its walkway and rail; the berth's edges marked by lane lamps; the convoy under the flanks. Drawn only, no
            /// shadows, except what is solid at the plane (the wings, the superstructure's foot, the gantry's legs).</summary>
            static void Hull(ZoneKit kit)
            {
                var root=new GameObject("The hull from outside");
                var s=new ZoneKit.Shape();var trim=new ZoneKit.Shape();var lamps=new ZoneKit.Shape();var dark=new ZoneKit.Shape();
                const float X0=-40,X1=EdgeX,Z0=126,Z1=NorthEdge+10,HatchX0=-13,HatchX1=-3,HatchZ0=190,HatchZ1=202;
                // The back, in strips (the hatch's opening left), and its two flanks curving down.
                // The back in plates eight metres square, each its own white, one in five the Compact's pale teal-grey: from the flight camera one white sheet was a glare with nothing in it.
                void Back(float xa,float xb,float za,float zb)
                {
                    if(xb-xa<.01f||zb-za<.01f)return;
                    for(float x=xa;x<xb-.01f;x+=8)
                    {
                        float x1=Mathf.Min(x+8,xb);int ix=Mathf.FloorToInt((x+400)/8),iz=Mathf.FloorToInt((za+400)/8);float own=H(ix,iz),kind=H(ix+5,iz+11);
                        var tone=kind<.2f?new Color(.4f,.56f,.6f):kind<.34f?new Color(.52f,.55f,.6f):Color.Lerp(new Color(.8f,.82f,.85f),new Color(.95f,.95f,.96f),own);
                        // The working deck her lanes lie over: plates in the Compact's deep teal-grey, each its own, a white rim of hull round them (the look pass, November 14:
                        // from the flight camera the whole back was one white glare, and a ship over it had nothing to stand out against).
                        float mx=(x+x1)*.5f,mz=(za+zb)*.5f;
                        if(mx>-36&&mx<28&&mz>197&&mz<246)tone=kind<.25f?new Color(.07f,.1f,.12f):Color.Lerp(new Color(.13f,.17f,.2f),new Color(.27f,.32f,.35f),own);
                        s.Slot(0).Quad(W(x,HullTop,za),W(x1,HullTop,za),W(x1,HullTop,zb),W(x,HullTop,zb),Vector3.up,tone,1/8f);
                    }
                }
                for(float z=Z0;z<Z1-.01f;z+=8)
                {
                    float z1=Mathf.Min(z+8,Z1);
                    if(z1>HatchZ0&&z<HatchZ1){Back(X0,HatchX0,z,z1);Back(HatchX1,X1,z,z1);Back(HatchX0,HatchX1,z,Mathf.Min(z1,HatchZ0));Back(HatchX0,HatchX1,Mathf.Max(z,HatchZ1),z1);}
                    else Back(X0,X1,z,z1);
                    s.Slot(0).Quad(W(X0-5,HullTop-8,z),W(X0,HullTop,z),W(X0,HullTop,z1),W(X0-5,HullTop-8,z1),Vector3.left+Vector3.up*.6f,Pale,1/8f);
                    s.Slot(0).Quad(W(X0-6,HullTop-30,z),W(X0-5,HullTop-8,z),W(X0-5,HullTop-8,z1),W(X0-6,HullTop-30,z1),Vector3.left,ZoneKit.Shape.Dim(Pale,.85f),1/8f);
                    s.Slot(0).Quad(W(X1,HullTop,z),W(X1+5,HullTop-8,z),W(X1+5,HullTop-8,z1),W(X1,HullTop,z1),Vector3.right+Vector3.up*.6f,Pale,1/8f);
                    s.Slot(0).Quad(W(X1+5,HullTop-8,z),W(X1+6,HullTop-30,z),W(X1+6,HullTop-30,z1),W(X1+5,HullTop-8,z1),Vector3.right,ZoneKit.Shape.Dim(Pale,.85f),1/8f);
                    // Lit windows down both flanks, two rows.
                    for(float wz=z+1;wz<z1-.5f;wz+=2.6f)foreach(var (x,y) in new[]{(X0-2.6f,HullTop-4.2f),(X0-5.4f,HullTop-14),(X1+2.6f,HullTop-4.2f),(X1+5.4f,HullTop-14)})
                        lamps.Slot(0).Box(W(x,y,wz),new Vector3(.5f,.4f,1.3f),Quaternion.identity,Color.white);
                }
                // The teal bands across the back and a gold line either side of each, the edge's teal lip.
                foreach(float z in new[]{140f,172f,214f,240f}){trim.Slot(0).Box(W((X0+X1)*.5f,HullTop+.02f,z),new Vector3(X1-X0,.04f,2.4f),Quaternion.identity,Teal);foreach(int side in new[]{-1,1})trim.Slot(1).Box(W((X0+X1)*.5f,HullTop+.02f,z+side*1.6f),new Vector3(X1-X0,.04f,.2f),Quaternion.identity,Gold);}
                foreach(float x in new[]{X0+.4f,X1-.4f})trim.Slot(0).Box(W(x,HullTop+.1f,(Z0+Z1)*.5f),new Vector3(.8f,.2f,Z1-Z0),Quaternion.identity,Teal);
                // The launch hatch, open: a pit with guide lamps down its walls, its doors laid back flat either side.
                dark.Slot(0).Box(W(-8,HullTop-4,196),new Vector3(10,.2f,12),Quaternion.identity,Color.white);
                foreach(int side in new[]{-1,1}){dark.Slot(0).Quad(W(-8+side*5,HullTop-4,190),W(-8+side*5,HullTop-4,202),W(-8+side*5,HullTop,202),W(-8+side*5,HullTop,190),Vector3.right*-side,Color.white,.25f);
                    s.Slot(1).Box(W(-8+side*10.2f,HullTop+.12f,196),new Vector3(5,.24f,12),Quaternion.identity,Pale,White);for(float z=191;z<202;z+=2)lamps.Slot(0).Box(W(-8+side*4.9f,HullTop-1.4f,z),new Vector3(.1f,.3f,.6f),Quaternion.identity,Color.white);}
                foreach(int side in new[]{-1,1})dark.Slot(0).Quad(W(-13,HullTop-4,196+side*6),W(-3,HullTop-4,196+side*6),W(-3,HullTop,196+side*6),W(-13,HullTop,196+side*6),Vector3.forward*-side,Color.white,.25f);
                // The anchor clamps' seats: rings in the plate where her clamps stand (the fight's to raise).
                foreach(var seat in ClampSeats){trim.Slot(1).Disc(seat+Vector3.up*.05f,Vector3.up,2.6f,24,Gold,2.2f,Gold);s.Slot(1).Disc(seat+Vector3.up*.04f,Vector3.up,2.2f,24,Pale,0,null);trim.Slot(0).Disc(seat+Vector3.up*.06f,Vector3.up,.8f,16,TealDeep);}
                // Low things on the back: frames across it every sixteen metres, two raised walkways along it with their rails, the decks' skylights in rows (lit from below),
                // gold chevrons down its middle toward her shelter, vents, service hatches and short lamp posts. Nothing over a metre and three quarters: the ships are three over it.
                for(float z=Z0+8;z<NorthEdge-4;z+=16)
                {
                    if(z>HatchZ0-1&&z<HatchZ1+1){s.Slot(1).Box(W((X0+HatchX0)*.5f,HullTop+.25f,z),new Vector3(HatchX0-X0,.5f,.9f),Quaternion.identity,IronPale,Pale);s.Slot(1).Box(W((HatchX1+X1)*.5f,HullTop+.25f,z),new Vector3(X1-HatchX1,.5f,.9f),Quaternion.identity,IronPale,Pale);}
                    else s.Slot(1).Box(W((X0+X1)*.5f,HullTop+.25f,z),new Vector3(X1-X0,.5f,.9f),Quaternion.identity,IronPale,Pale);
                    trim.Slot(1).Box(W((X0+X1)*.5f,HullTop+.51f,z),new Vector3(X1-X0,.02f,.2f),Quaternion.identity,Gold);
                }
                foreach(float x in new[]{-26f,18f})
                {
                    s.Slot(1).Box(W(x,HullTop+.18f,(Z0+NorthEdge)*.5f),new Vector3(2.2f,.36f,NorthEdge-Z0),Quaternion.identity,ZoneKit.Shape.Dim(Pale,.86f),new Color(.62f,.66f,.7f));
                    foreach(int side in new[]{-1,1})for(float z=Z0+1;z<NorthEdge;z+=3)s.Slot(1).Post(W(x+side*1.05f,HullTop+.36f,z),1,.03f,.03f,5,IronPale,IronPale);
                    foreach(int side in new[]{-1,1})trim.Slot(0).Box(W(x+side*1.05f,HullTop+1.35f,(Z0+NorthEdge)*.5f),new Vector3(.06f,.06f,NorthEdge-Z0),Quaternion.identity,Teal);
                }
                // The decks' skylights in rows, lit from below: each a pane of the Compact's lit teal-white in a dark frame, every other plate's length (the look pass of November 21: rows
                // of bare white boxes every 3.4 m were the brightest thing over the working deck, and the deck read as flat grey between them).
                foreach(float x in new[]{-34f,-14f,8f,27f})for(float z=Z0+2;z<NorthEdge-2;z+=6.8f)
                {
                    if(x<-12&&x>-16&&z>HatchZ0-2&&z<HatchZ1+2)continue;if(Relays.Any(r=>Vector2.Distance(new Vector2(x,z),new Vector2(r.x,r.z))<4))continue;
                    dark.Slot(0).Box(W(x,HullTop+.03f,z),new Vector3(1.7f,.04f,2.1f),Quaternion.identity,Color.white);
                    lamps.Slot(1).Box(W(x,HullTop+.05f,z),new Vector3(1.2f,.04f,1.6f),Quaternion.identity,Color.white);
                    trim.Slot(0).Box(W(x,HullTop+.06f,z),new Vector3(.08f,.04f,1.6f),Quaternion.identity,new Color(.22f,.3f,.34f));
                }
                // The working deck's plates read as plates: a seam round each (a dark line with a pale lip), a panel line across its middle, a row of fixings at each end, and here and there
                // a service hatch let into it and the deck's number painted in a corner.
                for(float px=-36;px<28-.01f;px+=8)for(float pz=198;pz<246-.01f;pz+=8)
                {
                    int ix=Mathf.FloorToInt((px+400)/8),iz=Mathf.FloorToInt((pz+400)/8);float kind=H(ix+9,iz+2);
                    trim.Slot(0).Box(W(px+4,HullTop+.032f,pz),new Vector3(8,.03f,.14f),Quaternion.identity,new Color(.05f,.07f,.08f));trim.Slot(0).Box(W(px+4,HullTop+.033f,pz+.12f),new Vector3(8,.03f,.06f),Quaternion.identity,new Color(.42f,.5f,.54f));
                    trim.Slot(0).Box(W(px,HullTop+.032f,pz+4),new Vector3(.14f,.03f,8),Quaternion.identity,new Color(.05f,.07f,.08f));trim.Slot(0).Box(W(px+.12f,HullTop+.033f,pz+4),new Vector3(.06f,.03f,8),Quaternion.identity,new Color(.42f,.5f,.54f));
                    trim.Slot(0).Box(W(px+4,HullTop+.031f,pz+4),new Vector3(7.4f,.03f,.05f),Quaternion.identity,new Color(.1f,.13f,.15f));
                    for(float fx=px+.6f;fx<px+8;fx+=.9f)foreach(float fz in new[]{pz+.45f,pz+7.55f})trim.Slot(0).Box(W(fx,HullTop+.034f,fz),new Vector3(.12f,.03f,.12f),Quaternion.identity,new Color(.46f,.52f,.56f));
                    if(kind<.3f)
                    {
                        var c=W(px+2.2f+kind*8,HullTop+.035f,pz+5.6f);if(ClampSeats.Any(seat=>Vector2.Distance(new Vector2(c.x,c.z),new Vector2(seat.x,seat.z))<4))continue;
                        trim.Slot(0).Box(c,new Vector3(1.6f,.03f,1.1f),Quaternion.identity,new Color(.06f,.08f,.1f));trim.Slot(1).Box(c+Vector3.up*.004f,new Vector3(1.3f,.03f,.08f),Quaternion.identity,Gold);
                    }
                    else if(kind>.82f)foreach(int k in new[]{0,1,2})trim.Slot(1).Triangle(W(px+1+k*.7f,HullTop+.035f,pz+1),W(px+1.5f+k*.7f,HullTop+.035f,pz+1),W(px+1.25f+k*.7f,HullTop+.035f,pz+1.7f),Vector3.up,Gold);
                }
                // Her shield's two relays' masts (MeretRelay.MastName): a plinth on the hull in a painted seat as her clamps' are, a short white column banded in teal, a gold cradle at the
                // plane's foot that the fight's lit head sits in. Stood where the fight stands each relay (BossController.ShipTune.Relays from her station), seen before she wakes.
                // Each hangs under the hull's back it stands on (drawn only, as the back is; on the scene's root it was a thing of its own nineteen metres over the decks under it).
                var mastHolders=new List<GameObject>();
                for(int i=0;i<Relays.Length;i++)
                {
                    var r=Relays[i];var foot=W(r.x,HullTop,r.z);var masts=new ZoneKit.Shape();var mastLamps=new ZoneKit.Shape();
                    trim.Slot(1).Disc(foot+Vector3.up*.05f,Vector3.up,3.4f,32,Gold,3f,Gold);trim.Slot(0).Disc(foot+Vector3.up*.045f,Vector3.up,3f,32,TealDeep,0,null);
                    for(int k=0;k<12;k++){float a=k*30*Mathf.Deg2Rad;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));trim.Slot(1).Beam(foot+Vector3.up*.05f+d*2.1f,foot+Vector3.up*.05f+d*2.8f,.16f,.01f,Gold);}
                    masts.Slot(0).Box(foot+Vector3.up*.25f,new Vector3(2.4f,.5f,2.4f),Quaternion.Euler(0,45,0),IronPale,Pale);
                    masts.Slot(0).Post(foot+Vector3.up*.5f,1.05f,.46f,.34f,14,Pale,White);
                    masts.Slot(1).Post(foot+Vector3.up*1.05f,.22f,.42f,.4f,14,Teal,Teal);
                    for(int k=0;k<4;k++){float a=(k*90+45)*Mathf.Deg2Rad;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));masts.Slot(2).Beam(foot+Vector3.up*1.5f+d*.3f,foot+Vector3.up*2.05f+d*1.05f,.14f,.14f,Gold);}
                    masts.Slot(2).Post(foot+Vector3.up*1.45f,.12f,.5f,.5f,14,Gold,Gold);
                    foreach(int k in new[]{0,1,2,3}){float a=k*90*Mathf.Deg2Rad;mastLamps.Slot(0).Box(foot+Vector3.up*.52f+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*1.22f,new Vector3(.26f,.06f,.26f),Quaternion.identity,Color.white);}
                    var holder=new GameObject(MeretRelay.MastName(i));holder.transform.position=W(r.x,Plane,r.z);mastHolders.Add(holder);
                    var drawn=Made(kit,"Shield relay mast "+(i+1),masts,WhiteMat,TealMat,GoldMat);drawn.transform.SetParent(holder.transform,true);
                    Made(kit,"Shield relay mast lamps "+(i+1),mastLamps,LampTeal).transform.SetParent(holder.transform,true);
                }
                Debug.Log("CONTINUANCE_RELAY_MASTS "+string.Join(" ",Relays.Select(r=>r.ToString())));
                for(float z=Z0+38;z<WingSouth-6;z+=8){var c=W(-4,HullTop+.035f,z);if(Vector2.Distance(new Vector2(c.x,c.z),new Vector2(-8,174))<6.4f)continue;trim.Slot(1).Triangle(c+W(-.9f,0,-.6f),c+W(.9f,0,-.6f),c+W(0,0,.8f),Vector3.up,Gold);}
                // Her lanes over the working deck, dashed pale: the deck reads as a flight deck, and the dark plates aren't one sheet.
                foreach(float x in new[]{-20f,-9f,1f,13f})for(float z=200;z<244;z+=4){if(ClampSeats.Any(c=>Vector2.Distance(new Vector2(x,z+1),new Vector2(c.x,c.z))<3.6f))continue;trim.Slot(0).Box(W(x,HullTop+.03f,z+1),new Vector3(.32f,.03f,2),Quaternion.identity,new Color(.82f,.86f,.88f));}
                // Radiator banks either side of the launch hatch, rows of dark fins across the white (low: half a metre), and the Compact's roundel between the name and the hatch.
                var fins=new ZoneKit.Shape();var banks=new[]{(x0:-24.6f,x1:-17.4f,z0:151f,z1:165f),(x0:-24.6f,x1:-17.4f,z0:167f,z1:181f),(x0:.2f,x1:6.8f,z0:151f,z1:165f),(x0:.2f,x1:6.8f,z0:167f,z1:181f)};
                foreach(var (bx0,bx1,bz0,bz1) in banks)
                {
                    fins.Slot(0).Box(W((bx0+bx1)*.5f,HullTop+.04f,(bz0+bz1)*.5f),new Vector3(bx1-bx0+.4f,.08f,bz1-bz0+.4f),Quaternion.identity,new Color(.2f,.24f,.28f));
                    for(float z=bz0+.25f;z<bz1;z+=.55f)fins.Slot(0).Box(W((bx0+bx1)*.5f,HullTop+.3f,z),new Vector3(bx1-bx0,.46f,.1f),Quaternion.identity,new Color(.24f,.3f,.36f),new Color(.5f,.58f,.64f));
                    foreach(float x in new[]{bx0,bx1})fins.Slot(1).Box(W(x,HullTop+.36f,(bz0+bz1)*.5f),new Vector3(.24f,.6f,bz1-bz0+.4f),Quaternion.identity,Pale,White);
                }
                var roundel=W(-8,HullTop,174);
                trim.Slot(1).Disc(roundel+Vector3.up*.035f,Vector3.up,5.6f,48,Gold,5f,Gold);trim.Slot(0).Disc(roundel+Vector3.up*.03f,Vector3.up,4.9f,48,TealDeep,0,null);
                trim.Slot(1).Disc(roundel+Vector3.up*.04f,Vector3.up,2.9f,40,Gold,2.5f,Gold);trim.Slot(0).Disc(roundel+Vector3.up*.045f,Vector3.up,1.1f,24,Teal,0,null);
                for(int k=0;k<8;k++){float a=k*Mathf.PI/4;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));trim.Slot(1).Beam(roundel+Vector3.up*.042f+d*3f,roundel+Vector3.up*.042f+d*4.8f,.22f,.01f,Gold);}
                var rng=new System.Random(61);
                for(int i=0;i<80;i++)
                {
                    var p=new Vector2(Mathf.Lerp(X0+3,X1-3,(float)rng.NextDouble()),Mathf.Lerp(Z0+4,NorthEdge-6,(float)rng.NextDouble()));
                    if(p.x>-15&&p.x<-1&&p.y>186&&p.y<206)continue;if(ClampSeats.Any(c=>Vector2.Distance(p,new Vector2(c.x,c.z))<4))continue;if(Mathf.Abs(p.x+26)<2||Mathf.Abs(p.x-18)<2)continue;
                    if(Relays.Any(r=>Vector2.Distance(p,new Vector2(r.x,r.z))<4.5f))continue;
                    if(banks.Any(b=>p.x>b.x0-2&&p.x<b.x1+2&&p.y>b.z0-2&&p.y<b.z1+2)||Vector2.Distance(p,new Vector2(roundel.x,roundel.z))<7)continue;
                    if(i%3==0)s.Slot(1).Box(W(p.x,HullTop+.4f,p.y),new Vector3(2.4f,.8f,1.4f),Quaternion.Euler(0,(i%2)*90,0),Pale,ZoneKit.Shape.Dim(Pale,.8f));
                    else if(i%3==1){s.Slot(1).Box(W(p.x,HullTop+.03f,p.y),new Vector3(1.8f,.06f,1.8f),Quaternion.identity,new Color(.4f,.46f,.5f),new Color(.46f,.52f,.56f));trim.Slot(0).Box(W(p.x,HullTop+.04f,p.y-1),new Vector3(2.1f,.03f,.1f),Quaternion.identity,Teal);}
                    else{s.Slot(1).Post(W(p.x,HullTop,p.y),1.4f,.08f,.06f,8,IronPale,Pale);lamps.Slot(0).Box(W(p.x,HullTop+1.5f,p.y),new Vector3(.24f,.24f,.24f),Quaternion.identity,Color.white);}
                }
                // The name across the back, and the Compact's mark.
                Painted(kit,"Hull name","CONTINUANCE",W(-4,HullTop+.03f,156),Vector3.right,Vector3.up,52,new Color(.16f,.44f,.48f)).transform.SetParent(root.transform,true);
                Painted(kit,"Hull number","C-1",W(-24,HullTop+.03f,228),Vector3.right,Vector3.up,10,new Color(.84f,.68f,.32f)).transform.SetParent(root.transform,true);
                // ---- north of every lane: the shelter's wings, the superstructure stepping up, the frame gantry (each its own piece, all of it north of z 246)
                var wings=new ZoneKit.Shape();var sup=new ZoneKit.Shape();var supLit=new ZoneKit.Shape();var gan=new ZoneKit.Shape();
                foreach(var (x0,x1) in Wings)
                {
                    float cx=(x0+x1)*.5f,w=x1-x0;
                    wings.Slot(0).Box(W(cx,HullTop+4,(WingSouth+WingNorth)*.5f),new Vector3(w,8,WingNorth-WingSouth),Quaternion.identity,Pale,White);
                    wings.Slot(0).Box(W(cx,HullTop+8.4f,WingSouth+2.3f),new Vector3(w+.6f,.8f,4),Quaternion.Euler(-12,0,0),Pale,White);
                    wings.Slot(1).Box(W(cx,HullTop+5.4f,WingSouth+.04f),new Vector3(w-.4f,.4f,.06f),Quaternion.identity,Teal);
                    for(float y=HullTop+1;y<HullTop+4.6f;y+=1.2f)supLit.Slot(0).Box(W(cx,y,WingSouth+.04f),new Vector3(w-1.2f,.3f,.06f),Quaternion.identity,Color.white);
                }
                foreach(var (x0,x1,y0,y1,z0,z1) in new[]{(-30f,22f,HullTop,HullTop+10,Superstructure,NorthEdge+42),(-22f,14f,HullTop+10,HullTop+17,Superstructure+5,NorthEdge+40),(-13f,5f,HullTop+17,HullTop+23,Superstructure+10,NorthEdge+36)})
                {
                    sup.Slot(0).Box(W((x0+x1)*.5f,(y0+y1)*.5f,(z0+z1)*.5f),new Vector3(x1-x0,y1-y0,z1-z0),Quaternion.identity,Pale,White);
                    sup.Slot(1).Box(W((x0+x1)*.5f,y1-1.2f,z0-.03f),new Vector3(x1-x0-1,.5f,.06f),Quaternion.identity,Teal);
                    for(float x=x0+1.4f;x<x1-1;x+=1.8f)supLit.Slot(0).Box(W(x,(y0+y1)*.5f,z0-.04f),new Vector3(1.1f,.7f,.06f),Quaternion.identity,Color.white);
                }
                sup.Slot(0).Post(W(-4,HullTop+23,NorthEdge+24),9,.3f,.16f,8,IronPale,Pale);supLit.Slot(0).Box(W(-4,HullTop+32.2f,NorthEdge+24),new Vector3(.5f,.5f,.5f),Quaternion.identity,Color.white);
                // The frame gantry: two legs off the back, a beam over, its walkway under the plane with a rail at hand height, where she will stand.
                var gx0=16f;var gx1=32f;var gz=256f;
                foreach(float x in new[]{gx0,gx1}){gan.Slot(0).Box(W(x,HullTop+7,gz),new Vector3(1.4f,14,1.4f),Quaternion.identity,Pale,White);gan.Slot(2).Box(W(x,HullTop+9,gz-.73f),new Vector3(1.2f,.4f,.06f),Quaternion.identity,Teal);}
                gan.Slot(0).Box(W((gx0+gx1)*.5f,HullTop+14.4f,gz),new Vector3(gx1-gx0+1.6f,1.6f,1.8f),Quaternion.identity,Pale,White);
                gan.Slot(0).Box(W((gx0+gx1)*.5f,GantryWalk-.15f,250.4f),new Vector3(gx1-gx0-2,.3f,3.2f),Quaternion.identity,IronPale,Pale);
                for(float x=gx0+1.2f;x<=gx1-1.19f;x+=1.6f){gan.Slot(1).Post(W(x,GantryWalk,248.9f),1,.04f,.04f,6,Gold,Gold);}
                gan.Slot(1).Beam(W(gx0+1,GantryWalk+1,248.9f),W(gx1-1,GantryWalk+1,248.9f),.08f,.08f,Gold);
                foreach(float x in new[]{gx0+1.4f,gx1-1.4f})gan.Slot(0).Box(W(x,(HullTop+GantryWalk)*.5f,251.6f),new Vector3(.4f,GantryWalk-HullTop,.4f),Quaternion.identity,IronPale,Pale);
                var shelter=Made(kit,"Her shelter's wings",wings,WhiteMat,TealMat);shelter.transform.SetParent(root.transform,true);shelter.isStatic=true;
                foreach(var (x0,x1) in Wings)ZoneKit.Block(shelter,W((x0+x1)*.5f,HullTop+4,(WingSouth+WingNorth)*.5f),new Vector3(x1-x0,8,WingNorth-WingSouth),0,"Shelter wing");
                var superstructure=Made(kit,"The superstructure",sup,WhiteMat,TealMat);superstructure.transform.SetParent(root.transform,true);superstructure.isStatic=true;
                ZoneKit.Block(superstructure,W(-4,HullTop+5,(Superstructure+NorthEdge+42)*.5f),new Vector3(52,10,NorthEdge+42-Superstructure),0,"Superstructure");
                Made(kit,"The superstructure, lit",supLit,LampWarm).transform.SetParent(superstructure.transform,true);
                var frame=Made(kit,"The frame gantry",gan,WhiteMat,GoldMat,TealMat);frame.transform.SetParent(root.transform,true);frame.isStatic=true;
                foreach(float x in new[]{gx0,gx1})ZoneKit.Block(frame,W(x,HullTop+7,gz),new Vector3(1.4f,14,1.4f),0,"Gantry leg");
                // The dock's plate on the walkway.
                trim.Slot(1).Disc(W(GantryDock.x,GantryWalk+.02f,GantryDock.z+1.2f),Vector3.up,1.3f,20,Gold,1.1f,Gold);
                // ---- the berth's edges, marked: lamp posts on the back along its south edge, buoys over the space beyond each flank and past the north where nothing else stands
                var buoys=new ZoneKit.Shape();var b=HullBerthBox;
                for(float x=b.min.x;x<=b.max.x+.01f;x+=8)
                {
                    bool overHull=x>X0&&x<X1;float foot=overHull?HullTop:Plane-2.2f;
                    if(overHull)buoys.Slot(0).Post(W(x,foot,b.min.z),Plane-.3f-foot,.1f,.08f,8,IronPale,Pale);else buoys.Slot(0).Post(W(x,foot,b.min.z),1.6f,.3f,.2f,10,Pale,White);
                    lamps.Slot(1).Box(W(x,Plane-.15f,b.min.z),new Vector3(.3f,.3f,.3f),Quaternion.identity,Color.white);
                    if(x<-30||x>14){buoys.Slot(0).Post(W(x,Plane-2.2f,b.max.z),1.6f,.3f,.2f,10,Pale,White);lamps.Slot(1).Box(W(x,Plane-.15f,b.max.z),new Vector3(.3f,.3f,.3f),Quaternion.identity,Color.white);}
                }
                for(float z=b.min.z+8;z<b.max.z;z+=8)foreach(float x in new[]{b.min.x,b.max.x}){buoys.Slot(0).Post(W(x,Plane-2.2f,z),1.6f,.3f,.2f,10,Pale,White);lamps.Slot(1).Box(W(x,Plane-.15f,z),new Vector3(.3f,.3f,.3f),Quaternion.identity,Color.white);}
                Made(kit,"The berth's lane marks",buoys,WhiteMat).transform.SetParent(root.transform,true);
                var back=Made(kit,"The hull's back",s,HullMat,WhiteMat);back.transform.SetParent(root.transform,true);
                foreach(var m in mastHolders)m.transform.SetParent(back.transform,true);
                Made(kit,"The radiator banks",fins,DarkMat,WhiteMat).transform.SetParent(root.transform,true);
                Made(kit,"The hull's back, banded",trim,TealMat,GoldMat).transform.SetParent(root.transform,true);
                var holes=Made(kit,"The hull's back, open",dark,SpaceMat);holes.transform.SetParent(back.transform,true);
                var litUp=Made(kit,"The hull's lit windows",lamps,LampWarm,LampTeal);litUp.transform.SetParent(root.transform,true);
                foreach(var r in root.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
                // A cold light on her shelter and on the gantry, and the convoy holding beyond the flanks.
                PointLight(W(-4,Plane+6,246),24,1.6f,new Color(.8f,.92f,1)).transform.SetParent(root.transform,true);
                PointLight(W(24,Plane+5,250),18,1.4f,new Color(1,.9f,.74f)).transform.SetParent(root.transform,true);
                Convoy(kit,new[]{("Hauler",W(-78,-14,200),36f,0f,true),("Skiff",W(-64,-6,236),12f,20f,true),("PatrolCutter",W(-70,-24,168),16f,-10f,true),("TallowStationHull",W(84,-40,220),60f,90f,true),
                    ("Hauler",W(70,-18,176),30f,180f,true),("CompactCutter",W(56,-4,246),4f,200f,false),("SeedBarge",W(-88,-34,264),9f,40f,false),("AnchorChain",W(-60,-70,214),90f,0f,true)},"The convoy beside the hull");
            }

            // ---------------------------------------------------------------- the floor
            /// <summary>The walked lines, deck by deck: the ground kit keeps them clear.</summary>
            internal static readonly Vector2[][] Ways=
            {
                new Vector2[]{new(0,-8),new(0,8),new(3,20),new(4,44),new(0,50),new(0,60),new(0,86),new(0,104),new(0,114),new(-8,130),new(-8,156),new(0,170),new(0,190),new(-2,214),new(2,236)},
                new Vector2[]{new(0,185.5f),new(14,185.5f),new(20,188)},new Vector2[]{new(0,72),new(20,77),new(27,77)},
            };
            /// <summary>The decks' floor: polish where feet go, a little dust only where work is done, the painted lines a well-run ship keeps, trolley tracks in the works and the bay, oil under
            /// the cutters, scorch round the welding line, a warm pool under every lamp. Nothing orange, red or purple on it.</summary>
            static void Ground(ZoneKit kit,ZoneKit.Land land)
            {
                FloorWork(kit);
                var ground=GroundKit.Open(kit.zone,land.FloorFilter,83,1.1f,false,v=>{var plan=new Vector2(v.x,v.z);return FloorAt(plan,out float level)<=.15f&&Mathf.Abs(v.y-level)<.3f;});
                float Smooth(float a,float b,float x)=>GroundKit.Smooth(a,b,x);
                bool Working(Vector2 p){Nearest(p,out var r);return r==Room.Works||r==Room.DeckFour||r==Room.Bay;}
                foreach(var way in Ways)ground.ClearLane(way,1.2f);
                ground.Look("Canyon",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Continuance/continuance-keel.png"));
                ground.Layers(new Vector4(.04f,.3f,.4f,.2f),1.1f,new GroundKit.Layer("damp",new Color(.9f,.93f,.95f),4),new GroundKit.Layer("veins",new Color(.72f,.76f,.78f),3),new GroundKit.Layer("litter",new Color(.66f,.68f,.7f,.8f),2.4f));
                // Polish where the crews walk: patches over the decks, more along the ways.
                ground.Paint(0,p=>Working(p)?.4f*Smooth(.58f,.74f,ground.Noise(p,6,3)):0);
                // Fine dust at the wall feet, broken.
                ground.PaintFoot(1,.8f,.6f,7);
                // Scraps only where work is done.
                ground.Paint(2,p=>(Working(p)?.45f:0)*Smooth(.62f,.76f,ground.Noise(p,3,19)));
                ground.Shade(p=>.12f*Smooth(1.6f,.3f,ground.Edge(p)));
                ground.Decals(new[]{"SurveyLine"},4,p=>Working(p)?.8f:.45f,1.6f,3.2f,new Color(.92f,.9f,.8f,.65f),false);
                ground.Decals(new[]{"Litter"},2,p=>Working(p)?.6f:.15f,.6f,1.2f,new Color(.6f,.62f,.64f,.7f),false);
                ground.Decals(new[]{"Tracks"},2.6f,p=>Working(p)?.85f:.2f,.8f,1.6f,new Color(.3f,.32f,.34f,.5f),false);
                ground.Decals(new[]{"Stain"},1.4f,p=>Working(p)?.5f:0,1.2f,2.6f,new Color(.26f,.28f,.3f,.5f),false);
                ground.Decals(new[]{"Scorch"},5,p=>{Nearest(p,out var r);return r==Room.Works&&p.x>14?.9f:0;},1,2.2f,new Color(.18f,.18f,.2f,.7f),false);
                ground.LampPools(2.8f,new Color(1,.93f,.8f,.3f));
                ground.Finish();
            }

            // ---------------------------------------------------------------- stills
            /// <summary>LookDev's views (LookDev.Views names this): each deck as it is walked and from its middle, the spine from its foot to its top, the captain's line, the works' machines,
            /// the chart, the bay and its mouth, the flight berth from where the party comes out to the gantry, and wide views.</summary>
            internal static readonly LookDev.View[] Views=
            {
                new("arrival",Arrival),new("spine-aft",W(0,SpineY(-56),-56)),new("spine-fore",W(0,SpineY(-32),-32)),new("spine-top",W(0,Up,-12)),new("hatch",W(0,Up,2)),
                new("crew-mess",W(14,Up,24)),new("crew-bunks",W(-10,Up,24)),new("crew-north",W(4,Up,42)),new("crew-windows",W(26,Up,30)),new("stair",W(0,-1.6f,55)),
                new("deck-four",W(-4,Down,70)),new("cutter",W(-10,Down,80)),new("captain",W(0,Down,87)),new("deck-east",W(14,Down,82)),new("deck-north",W(4,Down,98)),
                new("works-south",W(0,Down,118)),new("works",W(-8,Down,140)),new("presses",W(-14,Down,160)),new("welding",W(16,Down,138)),new("sluices",W(10,Down,162)),new("works-door",W(0,Down,166)),
                new("north",W(0,Down,184)),new("command",W(21,Down,186)),new("command-window",W(27,Down,196)),new("bay",W(-6,Down,212)),new("bay-cradle",W(2,Down,234)),new("bay-mouth",W(-8,Down,246)),
                new("berth-out",HullBerthFlight,true),new("hail",HailAt,true),new("arena",W(-4,Plane,226),true),new("shelter",W(-4,Plane,242),true),new("gantry",W(24,Plane,242),true),
                new("bay-cutters",W(-12,Down,221)),new("overview-spine",W(0,-3,-44),false,3.2f),new("overview-crew",W(6,Up,30),false,3.4f),new("overview-deck",W(0,Down,82),false,3.4f),
                new("overview-berth",W(-4,Plane,214),true,2.6f),
            };
        }
    }
}
