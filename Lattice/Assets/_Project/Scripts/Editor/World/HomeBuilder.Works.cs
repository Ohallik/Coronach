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
    /// The Separation Works (dungeon, the Foundry Saint's press hall; rule: a Compact rig is clamped to whatever has been turned on the party, a cyan cable runs from the rig to the machine,
    /// and cutting the rig gives the machine back). Map brief, the six joints, coordinates and the boss arena contract: docs/maps/SeparationWorks.md. The dungeon engineer's file; everything
    /// of the zone's own is in <see cref="Works"/>, so nothing here can collide with the other chapter 9 files' members. <c>-works-greybox</c> leaves out dressing and the ground kit (the
    /// blockout); <c>-works-open</c> builds it with every joint open and every rig cut, for stills only (never saved as the zone: rebuild without it).
    /// </summary>
    public static partial class HomeBuilder
    {
        /// <summary>Under the promenade: its own grade (cool, low bloom, so a marking's orange wins over every lamp), and no sky.</summary>
        static void WorksDefinition(ZoneDef z)
        {
            z.hd2dProfile=ArenaBuilder.Asset<Hd2dProfile>("Works",p=>{p.tint=new Color(1,.99f,.97f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.42f;});
            z.skybox=null;
            // Its own air (tools/works_ambience.py): the keel's iron under strain, the town crating its life overhead through the deck, the plant's fans. Without it the Works hummed
            // like the Decks' server room.
            var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(Works.Bed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=Works.BedLevel;}
        }
        /// <summary>The Works' light. Under the promenade's deck grating: a high, cool key through the grates (enough to model plate and iron, too weak to bleach the deck), a cool fill so
        /// shade on painted steel reads as steel and not as one grey, a little haze. The plant's own light is its fixtures: warm work lamps on the walls, the presses' lamps. Bloom held high:
        /// nothing lit may bloom over a marking's rim.</summary>
        [ZoneLook.Source] static void WorksLooks()
        {
            ZoneLook.Register(WorksZone,new ZoneLook.Spec{sun=C(1f,.95f,.88f),sunIntensity=1f,sunPitch=64,shadowStrength=.62f,
                sky=C(.34f,.34f,.37f),equator=C(.3f,.29f,.29f),ground=C(.14f,.13f,.12f),
                fog=true,fogColor=C(.15f,.15f,.17f),fogStart=55,fogEnd=180,
                exposure=.14f,contrast=12,saturation=9,splitShadows=C(.45f,.47f,.53f),splitHighlights=C(.6f,.55f,.46f),
                bloomThreshold=1.05f,bloomIntensity=.42f,vignette=.24f});
        }

        internal static class Works
        {
            // ---------------------------------------------------------------- the plan
            // x runs east and z north, so north is the back of every view. The route walks north through the hulls the station was made from, a collar between each: the tug (the
            // hatch comes down into it), the hauler, the liner, and down the stair into the ore barge, whose hold is the plant's press hall. Each hull is its own level.
            internal const float TugFloor=0,HaulerFloor=-1.5f,LinerFloor=-3,BargeFloor=-6,ControlFloor=0;
            /// <summary>The press hall's centre, where the Foundry Saint stands (P in the boss arena contract).</summary>
            internal static readonly Vector2 P=new(0,196);
            /// <summary>Plan points a player stands at, named once: the keep-offs, the navigation's must-reach list, LookDev's views, the map brief and a route all want them.</summary>
            internal static readonly Vector2 Arrival=new(0,-5),HatchCradle=new(-4.6f,-1.4f),DorrowAt=new(-4.2f,32.2f),
                ThroatRig=new(9.8f,57.4f),ControlRig=new(25,90.2f),GalleryRig=new(14.2f,121.2f),
                ModuleCache=new(25.6f,15),BaggageCache=new(25.8f,44.4f),SkiffCache=new(29.4f,129.8f),TableAt=new(20.4f,52.6f),TableHand=new(20.4f,49.4f),
                Checkpoint=new(-3,165),HallCradle=new(6.6f,165),LiftAt=new(20.8f,209.8f),HallPreview=new(0,168);
            /// <summary>The way up the hatch stair, and the lift after the Saint: spawns in DecksLeaving the Decks engineer stands.</summary>
            internal const string DecksFromWorks="Works",DecksFromLift="Lift";
            /// <summary>The zone's bed and the ZoneDef volume tools/works_ambience.py measured for it (file RMS times volume at the earlier beds' -33 dB).</summary>
            internal const string Bed="Assets/_Project/Audio/Ambience/AMB_SeparationWorks.ogg";internal const float BedLevel=.305f;

            internal enum Room{Hatch,Tug,Bow,J4Pass,Module,Collar1,HaulerSouth,Throat,HaulerNorth,J5Pass,Baggage,ControlStair,Control,Collar2,Liner,J6Pass,Skiff,Collar3,Stair,Landing,HallDoor,Hall}
            /// <summary>The six hull standards the station was built from, and the plant's own iron (the collars, the control room).</summary>
            internal enum Hull{Tug,Hauler,Liner,Barge,Module,Skiff,Plant}
            static readonly (Room room,float x0,float x1,float z0,float z1,float round)[] Boxes=
            {
                (Room.Hatch,-7,7,-12,5,.5f),(Room.Tug,-11,11,3,28,.6f),(Room.J4Pass,10.2f,13.6f,17.6f,22.4f,.2f),(Room.Module,12.6f,28,11.5f,28.5f,.6f),
                (Room.Collar1,-2.7f,2.7f,34.5f,42,.2f),(Room.HaulerSouth,-11,11,41,60,.6f),(Room.Throat,-7.5f,7.5f,59,71,.2f),(Room.HaulerNorth,-11,11,70,97.2f,.6f),
                (Room.J5Pass,10.2f,13.6f,47.6f,52.4f,.2f),(Room.Baggage,12.6f,28,42,58,.6f),(Room.ControlStair,10.2f,18.2f,79.4f,84.6f,.2f),(Room.Control,17.4f,32,74,94,.6f),
                (Room.Collar2,-2.7f,2.7f,96.2f,103,.2f),(Room.Liner,-16,16,102,146.2f,.6f),(Room.J6Pass,15.2f,18.6f,131.6f,136.4f,.2f),(Room.Skiff,17.8f,32,126,142,.6f),
                (Room.Collar3,-2.7f,2.7f,145.2f,152.5f,.2f),(Room.Stair,-3,3,151.5f,163,.3f),(Room.Landing,-10,10,162,170,.5f),(Room.HallDoor,-3,3,169,175,.2f),(Room.Hall,-24,24,174,214,.8f),
            };
            /// <summary>The tug's bow: its hold narrows from the full width at z 27 to the collar at z 36.2, so the first room reads as the inside of a prow.</summary>
            const float BowFrom=27,BowTo=36.2f,BowWide=11,BowTip=5.6f;
            static float BowHalf(float z)=>Mathf.Lerp(BowWide,BowTip,Mathf.Clamp01((z-BowFrom)/(BowTo-BowFrom)));
            static float RoundBox(Vector2 p,float x0,float x1,float z0,float z1,float round)
            {
                var d=new Vector2(Mathf.Abs(p.x-(x0+x1)*.5f)-(x1-x0)*.5f+round,Mathf.Abs(p.y-(z0+z1)*.5f)-(z1-z0)*.5f+round);
                return Mathf.Min(Mathf.Max(d.x,d.y),0)+new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude-round;
            }
            /// <summary>Metres outside the nearest floor (negative on one), and which floor. Rooms that overlap meet at one height, so there is no step where they join.</summary>
            internal static float Nearest(Vector2 p,out Room room)
            {
                // The bow: a taper, its sides measured across (near enough for a wall's foot).
                float bow=Mathf.Max(Mathf.Abs(p.x)-BowHalf(p.y),Mathf.Max(BowFrom-.5f-p.y,p.y-BowTo));
                float best=bow;room=Room.Bow;
                foreach(var (r,x0,x1,z0,z1,round) in Boxes){float c=RoundBox(p,x0,x1,z0,z1,round);if(c<best){best=c;room=r;}}
                return best;
            }
            static float Ramp(float v,float v0,float v1,float a,float b)=>Mathf.Lerp(a,b,Mathf.Clamp01((v-v0)/(v1-v0)));
            internal static float Level(Vector2 p,Room room)
            {
                float floor=room switch
                {
                    Room.Collar1=>Ramp(p.y,37.6f,41,TugFloor,HaulerFloor),
                    Room.HaulerSouth or Room.Throat or Room.HaulerNorth or Room.J5Pass or Room.Baggage=>HaulerFloor,
                    Room.ControlStair=>Ramp(p.x,11.4f,17.4f,HaulerFloor,ControlFloor),
                    Room.Control=>ControlFloor,
                    Room.Collar2=>Ramp(p.y,98.2f,102,HaulerFloor,LinerFloor),
                    Room.Liner or Room.J6Pass or Room.Skiff or Room.Collar3=>LinerFloor,
                    Room.Stair=>Ramp(p.y,152.5f,162,LinerFloor,BargeFloor),
                    Room.Landing or Room.HallDoor or Room.Hall=>BargeFloor,
                    _=>TugFloor,
                };
                // A hand of unevenness in every deck, so no floor is a board: plates laid by hand a century ago.
                return floor+.022f*(Mathf.PerlinNoise(p.x*.41f+5,p.y*.41f+13)-.5f);
            }
            internal static float FloorAt(Vector2 p,out float level){float clear=Nearest(p,out var room);level=Level(p,room);return clear;}
            internal static bool Walkable(Vector2 p)=>FloorAt(p,out _)<=0;
            static readonly Vector2 Away=new(Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));
            /// <summary>The ground. A floor is its room's level; between floors is the keel's plating, which, with a floor beyond it along the camera's line, stays under the sightline to a hero
            /// at that floor's edge (the Waystation's rule, D119) and climbs away too steep to stand on.</summary>
            internal static float Height(Vector2 p)
            {
                float clear=FloorAt(p,out float level);if(clear<=0)return level;
                float allowed=float.PositiveInfinity;
                for(float d=.5f;d<=16;d+=.5f){if(FloorAt(p+Away*d,out float there)<0){allowed=Mathf.Min(level,there)+.6f+.82f*d;break;}}
                float rough=Mathf.PerlinNoise((p.x+70)*.11f,(p.y+20)*.09f);
                float top=Mathf.Min(allowed,level+6.8f+1.2f*rough);
                return level+(top-level)*Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/.9f));
            }
            /// <summary>How far over a floor anything on the camera's side of it may be drawn, <paramref name="back"/> metres back from that floor along the camera's line (D272, as the
            /// Walking Canopy draws it). The camera hangs 12.1 m over the hero and 15 m back, so its line to a hero's feet rises .81 a metre: at the floor's edge this is the deck's kerb
            /// (six tenths), from a metre and a half back it stays four tenths and more under the line to the feet, and it never comes within five metres of the lens.</summary>
            static float SeenOver(float back)=>Mathf.Min(SeenMost,Mathf.Max(SeenKerb,-.4f+.72f*back));
            const float SeenKerb=.6f,SeenMost=7,LensBack=15,LensNear=6;
            /// <summary>The keel as it is drawn. Bodies and the bake meet <see cref="Height"/>, which on the camera's side of a floor climbs at the camera's own pitch from six tenths over
            /// the line to a hero's feet: the Walking Canopy built so hid a whole boss fight's aftermath behind its own near wall (D272), and here it stood the liner's south keel to
            /// a hero's shins at the liner's edge and the press hall's south rim over anyone fighting along it. What is drawn there is a low kerb and a slope kept under the line of
            /// sight with room to spare, and nothing within five metres of the lens of anyone on any floor; the steeper wall bodies meet is in front of nobody.</summary>
            internal static float Seen(Vector2 p)
            {
                float stood=Height(p),clear=FloorAt(p,out float level);if(clear<=0)return stood;
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
            /// <summary>The lens itself: with a hero on a floor the camera hangs over that floor 15 m back along its line, and whatever stands within six metres of that place on the plan
            /// is in front of the glass. Nothing there is drawn higher than that floor and <see cref="SeenMost"/>. Unbounded where no camera ever hangs.</summary>
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
            internal static Hull HullOf(Room room)=>room switch
            {
                Room.Hatch or Room.Tug or Room.Bow=>Hull.Tug,
                Room.J4Pass or Room.Module=>Hull.Module,
                Room.HaulerSouth or Room.Throat or Room.HaulerNorth=>Hull.Hauler,
                Room.J5Pass or Room.Baggage or Room.Liner=>Hull.Liner,
                Room.J6Pass or Room.Skiff=>Hull.Skiff,
                Room.Landing or Room.HallDoor or Room.Hall=>Hull.Barge,
                _=>Hull.Plant,
            };
            internal static Hull HullAt(Vector2 p){Nearest(p,out var room);return HullOf(room);}
            /// <summary>Each hull's paint on its walls: red oxide, ochre, pale teal, dark iron, the Compact's white, cream, and the plant's blue-grey.</summary>
            internal static Color PlateOf(Hull hull)=>hull switch
            {
                Hull.Tug=>new Color(.76f,.3f,.22f),Hull.Hauler=>new Color(.84f,.62f,.28f),Hull.Liner=>new Color(.4f,.72f,.7f),Hull.Barge=>new Color(.42f,.42f,.46f),
                Hull.Module=>new Color(.93f,.94f,.96f),Hull.Skiff=>new Color(.92f,.82f,.62f),_=>new Color(.48f,.56f,.68f),
            };
            /// <summary>Each hull's deck: the same plate everywhere (the station's), each hull's its own cast, so no two rooms read as one colour.</summary>
            static Color DeckOf(Hull hull)=>hull switch
            {
                Hull.Tug=>new Color(.84f,.58f,.5f),Hull.Hauler=>new Color(.8f,.7f,.46f),Hull.Liner=>new Color(.5f,.74f,.7f),Hull.Barge=>new Color(.5f,.5f,.55f),
                Hull.Module=>new Color(.88f,.9f,.92f),Hull.Skiff=>new Color(.88f,.8f,.62f),_=>new Color(.58f,.64f,.76f),
            };
            static Color Tone(Vector2 p)
            {
                float clear=FloorAt(p,out _);float mottle=.9f+.2f*Mathf.PerlinNoise(p.x*.17f+3,p.y*.17f+7);
                var tone=DeckOf(HullAt(p));
                tone=Color.Lerp(tone,tone*.7f,ZoneKit.Smooth(-1.6f,0,clear));
                return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
            }
            static Vector3 On(ZoneKit kit,Vector2 p,float up=0)=>kit.On(p.x,p.y,up);

            // ---------------------------------------------------------------- the joints
            /// <summary>The six joints: number, flag, where the collar stands (on the wall's face), which way its face looks (into the room its jack is in), whether it is on the
            /// critical path (its hand reports <c>Joint</c>), the two hulls it joins, and what it is.</summary>
            internal static readonly (int n,string flag,Vector2 collar,Vector2 faces,bool critical,Hull near,Hull far,string what)[] Joints=
            {
                (1,"home.joint1",new(0,36.3f),new(0,-1),true,Hull.Tug,Hull.Hauler,"the tug's bow into the hauler"),
                (2,"home.joint2",new(0,97.3f),new(0,-1),true,Hull.Hauler,Hull.Liner,"the hauler's ribs into the liner"),
                (3,"home.joint3",new(0,146.3f),new(0,-1),true,Hull.Liner,Hull.Barge,"the liner into the ore barge, Taren's repair on it"),
                (4,"home.joint4",new(11.1f,20),new(-1,0),false,Hull.Tug,Hull.Module,"the tug into the Compact's module"),
                (5,"home.joint5",new(11.1f,50),new(-1,0),false,Hull.Hauler,Hull.Liner,"the hauler into the liner's baggage bay"),
                (6,"home.joint6",new(16.1f,134),new(-1,0),false,Hull.Liner,Hull.Skiff,"the liner into the courier skiff"),
            };
            static Vector2 SideOf(Vector2 faces)=>new(-faces.y,faces.x);
            /// <summary>Where a joint's jack stands and where a hero stands to work it.</summary>
            internal static Vector2 JackOf(int n){var j=Joints[n-1];return j.collar+j.faces*.95f+SideOf(j.faces)*(j.critical?4.4f:3.6f);}
            internal static Vector2 HandOf(int n){var j=Joints[n-1];return j.collar+j.faces*2.3f+SideOf(j.faces)*(j.critical?4.4f:3.6f);}
            /// <summary>The rig whose encounter's clearing frees joint 2's pinning press.</summary>
            internal const string ControlRigFight="Works_ControlRig",ThroatRigFight="Works_Throat",GalleryRigFight="Works_GalleryRig";

            // ---------------------------------------------------------------- what it is made of
            internal static Material FloorMat,KeelMat,PlateMat,IronMat,BrassMat,HazardMat,WhiteMat,CableMat,LampCyan,LampWarm,LampDim,WoodMat,Glass,CutHot;
            static readonly Color IronDark=new(.36f,.38f,.42f),IronPale=new(.72f,.74f,.78f),Rust=new(.78f,.56f,.44f);
            static float H(int a,int b)=>Mathf.Repeat(Mathf.Sin(a*12.9898f+b*78.233f)*43758.5453f,1);
            static void Materials()
            {
                // The station's deck plate, as the Decks have it above: square plates two metres across, each its own wear; a diamond tread, a grating over the keel's
                // service runs, an access hatch with its hinges and handle, and plain plate bolted down its edges. The joints dark enough to read from the camera.
                var deck=ZoneKit.Texture("Works/works-deck",512,(u,v)=>
                {
                    float x=u*4,y=v*4;int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);float fx=x-ix,fy=y-iy;
                    float own=H(ix,iy),kind=H(ix+11,iy+5),grain=ZoneKit.Cloud(u,v,24,3),scuff=ZoneKit.Cloud(u,v,9,17),pit=ZoneKit.Tile(u*80,v*80,80,9);
                    float edge=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy));
                    float tone=.46f+.12f*(own-.5f)+.08f*(grain-.5f)+.07f*(scuff-.5f)-(pit>.9f?.06f:0);
                    if(kind<.2f)
                    {
                        // Grating: slots across it and a frame round it.
                        tone=.3f+.05f*(grain-.5f);
                        if(edge>.07f&&Mathf.Repeat(fy*11,1)<.42f&&Mathf.Repeat(fx*3,1)>.06f)tone=.13f;
                    }
                    else if(kind<.4f)
                    {
                        float tread=Mathf.Abs(Mathf.Repeat((fx+fy)*14,1)-.5f),cross=Mathf.Abs(Mathf.Repeat((fx-fy)*14,1)-.5f);
                        tone=.4f+.05f*(grain-.5f)+(tread<.1f&&cross>.3f?.08f:0);
                    }
                    else if(kind<.52f)
                    {
                        // An access hatch: an inset square, two hinges, a recessed handle.
                        float inset=Mathf.Max(Mathf.Abs(fx-.5f),Mathf.Abs(fy-.5f));if(Mathf.Abs(inset-.36f)<.012f)tone=.2f;
                        if(fy>.7f&&fy<.76f&&(Mathf.Abs(fx-.3f)<.05f||Mathf.Abs(fx-.7f)<.05f))tone=.6f;
                        if(Mathf.Abs(fx-.5f)<.08f&&Mathf.Abs(fy-.24f)<.025f)tone=.18f;
                    }
                    // Bolts down two edges of each plate.
                    if((fx<.06f||fy<.06f)&&new Vector2(Mathf.Repeat(fx*8,1)-.5f,Mathf.Repeat(fy*8,1)-.5f).magnitude<.18f)tone=.62f;
                    if(edge<.016f)tone=.12f+.03f*own;
                    return new Color(tone,tone,tone*1.01f,1);
                });
                // The keel's own plating, behind every hull's paint, as a hull being taken apart shows it (the lead, November 14: drawn low on the camera's side, the keel between rooms
                // read as heavy dark slabs; it was dark riveted courses, one tone from the camera). Plates a metre and three quarters by one and a quarter, each its own wear, riveted round
                // and seamed dark; a frame standing at each plate's edge, lit on one side and shaded on the other; a plate here and there cut away to the dark between the frames with the
                // next hull's ribs inside; torch cuts across some, a black kerf in a scorched rim with bright metal either side; and a run of three cables across each tile on its clamps.
                var keel=ZoneKit.Texture("Works/works-keel",512,(u,v)=>
                {
                    float x=u*4,y=v*4;int px=Mathf.FloorToInt(x),py=Mathf.FloorToInt(y);float fx=x-px,fy=y-py;
                    float own=H(px+17,py+29),grain=ZoneKit.Cloud(u,v,16,5),streak=ZoneKit.Cloud(u*2,v*.5f,6,21);
                    float tone=.52f+.16f*(own-.5f)+.07f*(grain-.5f)-.1f*ZoneKit.Smooth(.55f,.8f,streak)*(1-fy);
                    if(H(px+3,py+11)<.18f){tone=.11f+.03f*grain;if(Mathf.Abs(fx-.36f)<.035f||Mathf.Abs(fx-.68f)<.035f)tone=.3f;if(Mathf.Abs(fy-.5f)<.03f)tone=.24f;}
                    else
                    {
                        if((fx<.07f||fx>.93f||fy<.06f||fy>.94f)&&new Vector2(Mathf.Repeat(fx*10,1)-.5f,Mathf.Repeat(fy*8,1)-.5f).magnitude<.2f)tone=.74f;
                        if(H(px+41,py+7)<.22f)
                        {
                            // A torch cut across the plate.
                            float d=Mathf.Abs((fx-.5f)*.8f-(fy-.5f)*.6f);if(d<.11f)tone=Mathf.Min(tone,.24f);if(d<.035f&&d>.018f)tone=.82f;if(d<.018f)tone=.05f;
                        }
                    }
                    if(fy<.02f||fy>.98f||fx>.985f)tone=.1f;
                    // The frame at the plate's edge: its lit face, its crown, its shadow.
                    if(fx<.09f){tone=fx<.025f?.88f:.66f;}else if(fx<.13f)tone=Mathf.Min(tone,.16f);
                    // The cable run, a third of the way up each tile: three lines on their clamps.
                    float cy=Mathf.Repeat(v,1);for(int k=0;k<3;k++){float c=Mathf.Abs(cy-(.3f+k*.026f));if(c<.009f)tone=k==1?.2f:.08f;if(c<.003f)tone=.42f;}
                    if(cy>.285f&&cy<.36f&&Mathf.Abs(Mathf.Repeat(u*8,1)-.5f)<.05f)tone=.62f;
                    return new Color(tone*.97f,tone,tone*1.04f,1);
                });
                // Hull plate, pale so each hull's own paint (vertex colour) is what shows: plates a metre and a fifth square, a rivet line round each, grime run down from each seam,
                // the paint worn through to bare metal at the edges, and here and there a patch welded over.
                var plate=ZoneKit.Texture("Works/works-plate",512,(u,v)=>
                {
                    float x=u*2,y=v*2;int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);float fx=x-ix,fy=y-iy;
                    float own=H(ix+3,iy+9),grain=ZoneKit.Cloud(u,v,20,7),run=ZoneKit.Cloud(u*6,v*.6f,6,29),chip=ZoneKit.Tile(u*60,v*60,60,31);
                    float tone=.78f+.08f*(own-.5f)+.06f*(grain-.5f)-.16f*ZoneKit.Smooth(.55f,.8f,run)*(1-fy);
                    float edge=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy));
                    if(edge<.05f&&chip>.6f)tone=.95f;
                    if(edge<.07f&&edge>.03f&&new Vector2(Mathf.Repeat(fx*14,1)-.5f,Mathf.Repeat(fy*14,1)-.5f).magnitude<.22f)tone=.52f;
                    if(edge<.014f)tone=.3f;
                    if(own>.86f&&Mathf.Abs(fx-.5f)<.22f&&Mathf.Abs(fy-.45f)<.16f){tone=.66f+.06f*grain;if(Mathf.Abs(Mathf.Max(Mathf.Abs(fx-.5f)/.22f,Mathf.Abs(fy-.45f)/.16f)-1)<.08f)tone=.42f;}
                    return new Color(tone,tone,tone,1);
                });
                // Machinery steel: fine rivets, scuffed paint, oil at the seams.
                var iron=ZoneKit.Texture("Works/works-iron",256,(u,v)=>
                {
                    float fx=Mathf.Repeat(u*2,1),fy=Mathf.Repeat(v*2,1);float grain=ZoneKit.Cloud(u,v,12,31),streak=ZoneKit.Cloud(u*6,v*.8f,6,17);
                    float tone=.56f+.1f*(grain-.5f)-.12f*ZoneKit.Smooth(.55f,.75f,streak);
                    if(fx<.02f||fy<.02f)tone=.3f;
                    if((fx<.07f||fx>.93f||fy<.07f||fy>.93f)&&new Vector2(Mathf.Repeat(u*20,1)-.5f,Mathf.Repeat(v*20,1)-.5f).magnitude<.2f)tone=.74f;
                    return new Color(tone,tone,tone*1.03f,1);
                });
                // The station's joint paint: yellow and black bands, worn through to the plate where boots go.
                var hazard=ZoneKit.Texture("Works/works-hazard",256,(u,v)=>
                {
                    float band=Mathf.Repeat((u+v)*4,1);float wear=ZoneKit.Cloud(u,v,10,41);bool yellow=band<.5f;
                    float worn=ZoneKit.Smooth(.62f,.78f,wear);
                    var c=yellow?new Color(.92f,.74f,.16f):new Color(.12f,.12f,.13f);
                    return Color.Lerp(c,new Color(.42f,.43f,.46f),worn*.85f);
                });
                var wood=ZoneKit.Texture("Works/works-wood",256,(u,v)=>
                {
                    float ring=Mathf.Repeat(v*14+ZoneKit.Cloud(u,v,4,51)*2.4f,1);float grain=ZoneKit.Cloud(u*.5f,v*8,4,53);
                    float tone=.62f+.12f*(grain-.5f)-(ring<.12f?.1f:0);if(Mathf.Repeat(u*4,1)<.012f)tone=.3f;
                    return new Color(tone*1.08f,tone*.84f,tone*.6f,1);
                });
                FloorMat=ZoneKit.Toon("works-deck",deck,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_BaseColor",Color.white);m.SetFloat("_BaseMapStrength",.92f);m.SetColor("_ShadowTint",new Color(.54f,.54f,.62f));m.SetFloat("_LightFloor",.22f);});
                KeelMat=ZoneKit.Toon("works-keel",keel,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.62f,.64f,.68f));m.SetColor("_ShadowTint",new Color(.44f,.48f,.62f));m.SetFloat("_LightFloor",.22f);});
                PlateMat=ZoneKit.Toon("works-plate",plate,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.48f,.5f,.64f));m.SetColor("_RimColor",new Color(.42f,.42f,.44f));m.SetFloat("_LightFloor",.26f);});
                IronMat=ZoneKit.Toon("works-iron",iron,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.78f,.8f,.84f));m.SetColor("_ShadowTint",new Color(.44f,.48f,.62f));m.SetColor("_RimColor",new Color(.48f,.5f,.54f));m.SetFloat("_LightFloor",.26f);});
                BrassMat=ZoneKit.Toon("works-brass",iron,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.96f,.78f,.46f));m.SetColor("_ShadowTint",new Color(.5f,.48f,.58f));m.SetColor("_RimColor",new Color(.72f,.58f,.36f));m.SetFloat("_LightFloor",.3f);});
                HazardMat=ZoneKit.Toon("works-hazard",hazard,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_LightFloor",.3f);});
                WhiteMat=ZoneKit.Toon("works-compact",plate,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.96f,.97f,.98f));m.SetColor("_ShadowTint",new Color(.52f,.56f,.68f));m.SetFloat("_LightFloor",.32f);});
                WoodMat=ZoneKit.Toon("works-wood",wood,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.5f,.48f,.58f));m.SetFloat("_LightFloor",.28f);});
                // A rig's cable: unlit, its colour the line's own (cyan with a pulse running from the rig to the machine; dark once cut).
                CableMat=ZoneKit.Toon("works-cable",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(1.25f,0,0,0));m.SetFloat("_ReceiveShadows",0);});
                // The Compact's cyan (a rig's lamps, a driven press's), the plant's warm work light (a freed press, an open joint, the work lamps), and unlit glass.
                LampCyan=ZoneKit.Toon("works-lamp-cyan",null,m=>{m.SetColor("_BaseColor",new Color(.6f,.95f,1));m.SetColor("_EmissionColor",new Color(.3f,1.5f,1.7f));});
                LampWarm=ZoneKit.Toon("works-lamp-warm",null,m=>{m.SetColor("_BaseColor",new Color(1,.95f,.84f));m.SetColor("_EmissionColor",new Color(1.6f,1.36f,.96f));});
                LampDim=ZoneKit.Toon("works-lamp-dim",null,m=>{m.SetColor("_BaseColor",new Color(.3f,.33f,.37f));m.SetColor("_EmissionColor",Color.black);m.SetFloat("_LightFloor",.3f);});
                Glass=ZoneKit.Toon("works-porthole",null,m=>{m.SetColor("_BaseColor",new Color(.16f,.22f,.26f));m.SetColor("_EmissionColor",new Color(.04f,.08f,.1f));m.SetFloat("_LightFloor",.4f);});
                // A torch cut in the keel where work is going on: unlit, white-gold at its core, coloured in its vertices; never a marking's orange.
                CutHot=ZoneKit.Toon("works-cut-hot",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(2.1f,0,0,0));m.SetFloat("_ReceiveShadows",0);});
                AssetDatabase.SaveAssets();
            }

            // ---------------------------------------------------------------- stencils: words painted on the iron
            /// <summary>A five-by-seven capital for each letter the Works paints: the tug's name, a lock number, the Compact's mark, the joints' numbers.</summary>
            static readonly Dictionary<char,string> Glyphs=new()
            {
                ['A']=".###.#...##...#######...##...##...#",['C']=".#####....#....#....#....#.....####",['E']="######....#....####.#....#....#####",
                ['H']="#...##...##...#######...##...##...#",['I']="#####..#....#....#....#....#..#####",['K']="#...##..#.#.#..##...#.#..#..#.#...#",
                ['L']="#....#....#....#....#....#....#####",['M']="#...###.###.#.##...##...##...##...#",['O']=".###.#...##...##...##...##...#.###.",
                ['P']="####.#...##...#####.#....#....#....",['S']=".#####....#.....###.....#....#####.",['T']="#####..#....#....#....#....#....#..",
                ['W']="#...##...##...##.#.##.#.###.###...#",['Y']="#...##...#.#.#...#....#....#....#..",
                ['1']="..#...##..#.#....#....#....#..#####",['2']=".###.#...#....#...#...#...#...#####",['3']="####.....#....#.###.....#....#####.",
                ['4']="...#...##..#.#.#..#.#####...#....#.",['5']="######....####.....#....#....#####.",['6']=".###.#....#....####.#...##...#.###.",
                ['7']="#####....#...#...#...#....#....#...",['9']=".###.#...##...#.####....#....#.###.",[' ']=new string('.',35),['-']=new string('.',15)+"#####"+new string('.',15),
            };
            /// <summary>A painted word: a square texture with the text in a band across its middle, worn at random like a stencil a century old; returns the band's v range.</summary>
            static Texture2D Stencil(string file,string text,out float v0,out float v1)
            {
                int cols=text.Length;float aspect=9f/(cols*6);float b0=.5f-aspect*.5f,b1=.5f+aspect*.5f;v0=b0;v1=b1;
                return ZoneKit.Texture("Works/"+file,512,(u,v)=>
                {
                    if(v<b0||v>b1)return new Color(1,1,1,0);
                    float gx=u*cols*6,gy=(v-b0)/(b1-b0)*9;int c=Mathf.Clamp(Mathf.FloorToInt(gx/6),0,cols-1);int px=Mathf.FloorToInt(gx-c*6),py=8-Mathf.FloorToInt(gy);
                    if(px>4||py<1||py>7||!Glyphs.TryGetValue(text[c],out var g))return new Color(1,1,1,0);
                    bool on=g[(py-1)*5+px]=='#';float wear=ZoneKit.Cloud(u*2,v*6,10,61);
                    return new Color(1,1,1,on&&wear>.3f?1:0);
                },false,true);
            }
            /// <summary>A painted word on a wall or a plate: a quad of its own (its texture runs once across it), cut out of the stencil's alpha, tinted to the paint's colour.</summary>
            static GameObject Painted(ZoneKit kit,string name,string text,Vector3 centre,Vector3 right,Vector3 facing,float width,Color paint)
            {
                var map=Stencil("works-stencil-"+name.ToLowerInvariant().Replace(' ','-'),text,out float v0,out float v1);
                var mat=ZoneKit.Toon("works-paint-"+name.ToLowerInvariant().Replace(' ','-'),map,m=>{ZoneKit.With(m,"_ALPHATEST_ON","_AlphaTest");m.SetFloat("_Cutoff",.5f);m.SetColor("_BaseColor",paint);m.SetFloat("_LightFloor",.3f);});
                float tall=width*(v1-v0);right.Normalize();var up=Vector3.Cross(facing.normalized,right).normalized;if(Vector3.Dot(up,Vector3.up)<0)up=-up;
                var at=centre+facing.normalized*.03f;
                var mesh=new Mesh{name="Works paint "+name};
                mesh.SetVertices(new List<Vector3>{at-right*width*.5f-up*tall*.5f,at+right*width*.5f-up*tall*.5f,at+right*width*.5f+up*tall*.5f,at-right*width*.5f+up*tall*.5f});
                mesh.SetUVs(0,new List<Vector2>{new(0,v0),new(1,v0),new(1,v1),new(0,v1)});
                mesh.SetNormals(Enumerable.Repeat(facing.normalized,4).ToList());
                bool front=Vector3.Dot(Vector3.Cross(right,up),facing)>=0;mesh.SetTriangles(front?new[]{0,2,1,0,3,2}:new[]{0,1,2,0,2,3},0);mesh.RecalculateBounds();
                var saved=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-paint-"+name.Replace(' ','-')+".asset");
                var go=new GameObject("Painted "+name,typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=saved;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;
                return go;
            }

            // ---------------------------------------------------------------- pieces
            static EncounterVolume Fight(ZoneKit kit,string id,Vector2 at,Vector2 size,string requiredFlag,params (string enemy,string standIn,int count,Vector2 from,float yaw,float radius)[] packs)
            {
                var encounter=WorldBuilder.Encounter(id,kit.On(at.x,at.y,1.5f),new Vector3(size.x,7,size.y),Foe(packs[0].enemy,packs[0].standIn),packs[0].count);
                var spawners=new List<Spawner>{encounter.spawners[0]};
                for(int i=1;i<packs.Length;i++)spawners.Add(ZoneKit.Spawn(encounter,Foe(packs[i].enemy,packs[i].standIn),packs[i].count,Vector3.zero,packs[i].radius));
                for(int i=0;i<packs.Length;i++)
                {
                    var spawner=spawners[i];spawner.transform.SetPositionAndRotation(kit.On(packs[i].from.x,packs[i].from.y),Quaternion.Euler(0,packs[i].yaw,0));
                    spawner.faceAsPlaced=true;spawner.radius=packs[i].count>1?packs[i].radius:0;
                }
                encounter.spawners=spawners.ToArray();encounter.requiredFlag=requiredFlag??"";return encounter;
            }
            /// <summary>A rig: an encounter of one Override Rig at its post, which is all that frees the machines on its cable (they read <c>clear.</c> its id). Shown inert from where the
            /// party first sees it, so rig, cable and machine are in one look before anything wakes.</summary>
            static EncounterVolume Rig(ZoneKit kit,string id,Vector2 at,float yaw,Vector2 trigger,Vector2 size,Vector2 view,float viewRadius)
            {
                var fight=Fight(kit,id,trigger,size,null,("OverrideRig","SnagvineNode",1,at,yaw,0));
                var preview=new GameObject(id+" preview");preview.transform.position=On(kit,view);fight.previewPoint=preview.transform;fight.previewRadius=viewRadius;
                return fight;
            }
            static RepairBay Cradle(ZoneKit kit,string name,Vector2 at,float yaw,string spawn)
            {
                var bay=kit.Prop("DeckConsole",new Vector3(at.x,0,at.y),1.4f,yaw).AddComponent<RepairBay>();bay.free=true;bay.saveSpawn=spawn;bay.prompt=name+" — repair and save";
                ChapterOneStaging.CradleBay(name,bay.gameObject,On(kit,at),kit.Y(at.x,at.y)+.02f,false,false);return bay;
            }
            /// <summary>A station footlocker in a hull's own paint: a cache.</summary>
            static LootCache Stash(ZoneKit kit,string id,string label,Vector2 at,float yaw,int scrip,Hull hull,params LootEntry[] contents)
            {
                var paint=PlateOf(hull);var shape=new ZoneKit.Shape();
                shape.Slot(0).Box(new Vector3(0,.38f,0),new Vector3(1.3f,.76f,.8f),Quaternion.identity,ZoneKit.Shape.Dim(paint,.8f),paint).Box(new Vector3(0,.82f,0),new Vector3(1.36f,.14f,.86f),Quaternion.identity,paint);
                foreach(float x in new[]{-.44f,.44f})shape.Slot(1).Box(new Vector3(x,.45f,0),new Vector3(.09f,.92f,.9f),Quaternion.identity,IronDark,IronPale);
                shape.Slot(1).Box(new Vector3(0,.55f,-.42f),new Vector3(.24f,.2f,.06f),Quaternion.identity,IronPale);
                var body=kit.Built("Footlocker "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),shape,PlateMat,IronMat);
                body.transform.SetPositionAndRotation(On(kit,at),Quaternion.Euler(0,yaw,0));
                var box=body.AddComponent<BoxCollider>();box.center=Vector3.up*.45f;box.size=new Vector3(1.4f,.9f,.9f);body.isStatic=true;
                var cache=LootCache.On(body,id,label,scrip,contents);cache.range=2.6f;return cache;
            }
            static void Hide(GameObject who,string name,string from,string gone,bool live=true)
            {var where=new GameObject(name+" (where)",typeof(Whereabouts)).GetComponent<Whereabouts>();where.from=from??"";where.goneAt=gone??"";where.awayFrom=where.backAt="";where.targets=new[]{who};where.live=live;}
            static void Gate(string name,string flag,bool showWhenSet,params GameObject[] targets)
            {var gate=new GameObject(name,typeof(FlagGate)).GetComponent<FlagGate>();gate.flag=flag;gate.showWhenSet=showWhenSet;gate.targets=targets;}
            /// <summary>A seal across a door, shut through a fight: iron bars dropped across it, drawn only while shut, with a carving obstacle (the Foundry's).</summary>
            static Membrane Seal(ZoneKit kit,string name,Vector2 at,float width,float tall=3.8f)
            {
                var seal=WorldBuilder.Membrane(name,On(kit,at,1.7f),width,true,tall);
                foreach(var filter in seal.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(filter);
                foreach(var view in seal.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(view);
                var box=seal.GetComponent<BoxCollider>();var carve=seal.gameObject.AddComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.center=box.center;carve.size=box.size;carve.carving=true;carve.carveOnlyStationary=false;carve.enabled=false;
                var bars=new ZoneKit.Shape();int count=Mathf.RoundToInt(width/.4f);
                for(int i=0;i<=count;i++){var foot=On(kit,at+new Vector2(-width*.5f+i*width/count,0),-.2f);bars.Slot(0).Post(foot,tall,.06f,.05f,6,IronDark,IronPale);}
                bars.Slot(0).Beam(On(kit,at+new Vector2(-width*.5f,0),tall-.3f),On(kit,at+new Vector2(width*.5f,0),tall-.3f),.16f,.2f,IronDark,IronPale);
                var drawn=kit.Built(name+" bars",bars,IronMat);drawn.transform.SetParent(seal.transform,true);drawn.GetComponent<Renderer>().enabled=false;
                return seal;
            }

            // ---------------------------------------------------------------- a joint
            /// <summary>A separation collar in its wall: the ring with its flange and eight locking dogs, the two hull leaves in the seam (each in its own hull's paint), the jack with its
            /// red hand wheel, the collar's lamp, its painted number, the hand, and the way it holds shut (a seal under the leaves). The <see cref="SeparationJoint"/> answers the hand.</summary>
            static SeparationJoint Joint(ZoneKit kit,int n,out DiscoveryPoint hand)
            {
                var (_,flag,collar,faces,critical,near,far,what)=Joints[n-1];
                // Set low in the floor (its lower third under the deck): standing tall, a ring was the next hull's whole foreground.
                float floor=kit.Y(collar.x,collar.y);float inner=critical?3.2f:2.8f,outer=critical?3.9f:3.3f,centreUp=critical?1.3f:1.1f;
                var face=new Vector3(faces.x,0,faces.y);var turn=Quaternion.LookRotation(-face);var side=new Vector3(SideOf(faces).x,0,SideOf(faces).y);
                var centre=new Vector3(collar.x,floor+centreUp,collar.y);
                var root=new GameObject("Joint "+n+" ("+what+")");root.transform.position=new Vector3(collar.x,floor,collar.y);
                // The ring, its flange and a backing plate, in its own space (it turns about its own forward as it gives).
                var ring=new ZoneKit.Shape();var loop=new List<Vector3>();var thick=new List<float>();float mid=(inner+outer)*.5f,half=(outer-inner)*.5f;
                for(int k=0;k<=36;k++){float a=k*Mathf.PI*2/36;loop.Add(new Vector3(Mathf.Cos(a)*mid,Mathf.Sin(a)*mid,0));thick.Add(half);}
                ring.Slot(0).Tube(loop,thick,10,IronDark,IronPale,1,.6f,false);
                ring.Slot(0).Disc(new Vector3(0,0,.32f),Vector3.back,outer+.35f,36,ZoneKit.Shape.Dim(IronDark,.8f),inner-.05f);
                // Bolt heads round the flange, brass.
                for(int k=0;k<24;k++){float a=k*Mathf.PI*2/24;var p=new Vector3(Mathf.Cos(a)*(outer+.18f),Mathf.Sin(a)*(outer+.18f),.2f);if(p.y<-centreUp+.1f)continue;ring.Slot(1).Box(p,new Vector3(.14f,.14f,.12f),Quaternion.identity,new Color(.86f,.7f,.4f));}
                var drawnRing=kit.Built("Joint "+n+" collar",ring,IronMat,BrassMat);drawnRing.transform.SetPositionAndRotation(centre,turn);drawnRing.transform.SetParent(root.transform,true);
                // The dogs: eight blocks bridging the seam round the ring, each its own piece so each draws back in its turn (along its own forward, outward).
                var dogs=new List<Transform>();int dn=0;
                foreach(float deg in new[]{-105f,-75f,-45f,-15f,15f,45f,75f,105f})
                {
                    float a=deg*Mathf.Deg2Rad;var radial=new Vector3(Mathf.Sin(a),Mathf.Cos(a),0);
                    if(centreUp+radial.y*mid<.35f)continue;
                    var dog=new ZoneKit.Shape();dog.Slot(0).Box(new Vector3(0,0,-.1f),new Vector3(.5f,.36f,.9f),Quaternion.identity,new Color(.8f,.64f,.34f),new Color(.96f,.8f,.48f)).Box(new Vector3(0,0,.45f),new Vector3(.3f,.24f,.3f),Quaternion.identity,IronDark,IronPale);
                    var drawnDog=kit.Built("Joint "+n+" dog "+dn++,dog,BrassMat);
                    var worldRadial=turn*radial;drawnDog.transform.SetPositionAndRotation(centre+worldRadial*(inner+.12f)+face*.05f,Quaternion.LookRotation(worldRadial,face));
                    // Part of the collar it locks (the joint sets each dog's place itself as it draws back): stood apart, the top dogs read as hanging in the air over the way.
                    drawnDog.transform.SetParent(drawnRing.transform,true);dogs.Add(drawnDog.transform);
                }
                // The leaves: the seam's two halves, each a hull's own plate, a half-disc above the floor; they part sideways into the walls.
                var leaves=new List<Transform>();var travel=new List<Vector3>();
                foreach(int s in new[]{-1,1})
                {
                    var leaf=new ZoneKit.Shape();var paint=PlateOf(s<0?near:far);var arc=new List<Vector3>();
                    float bottom=-centreUp+.02f,start=Mathf.Asin(Mathf.Clamp(bottom/inner,-1,1));
                    for(int k=0;k<=14;k++){float a=Mathf.Lerp(Mathf.PI*.5f,s<0?Mathf.PI-start:start,k/14f);arc.Add(new Vector3(Mathf.Cos(a)*inner*.99f,Mathf.Sin(a)*inner*.99f,0));}
                    var foot=new Vector3(0,bottom,0);
                    for(int k=0;k<14;k++)
                    {
                        foreach(float z in new[]{-.06f,.06f})
                            leaf.Slot(0).Triangle(foot+Vector3.forward*z,arc[k]+Vector3.forward*z,arc[k+1]+Vector3.forward*z,z<0?Vector3.back:Vector3.forward,ZoneKit.Shape.Dim(paint,.94f+.06f*(k%2)),.45f);
                    }
                    // Its seam edge: a heavy bar where the two leaves meet.
                    leaf.Slot(1).Box(new Vector3(s*.07f,(inner+bottom)*.5f,0),new Vector3(.14f,inner-bottom,.3f),Quaternion.identity,IronDark,IronPale);
                    var drawnLeaf=kit.Built("Joint "+n+" leaf "+(s<0?"near":"far"),leaf,PlateMat,IronMat);drawnLeaf.transform.SetPositionAndRotation(centre+face*-.55f,turn);
                    drawnLeaf.transform.SetParent(root.transform,true);leaves.Add(drawnLeaf.transform);travel.Add(turn*new Vector3(s*(inner+1.1f),0,0));
                }
                // What it holds shut: a seal in the seam, a solid box and a carving obstacle (the world tests open every seal).
                var blocks=new GameObject("Joint "+n+" held",typeof(Membrane));blocks.transform.SetParent(root.transform,true);
                var solid=new GameObject("Held seam",typeof(BoxCollider),typeof(NavMeshObstacle));solid.transform.SetParent(blocks.transform,false);
                var seamSize=new Vector3(inner*1.95f,3.6f,.9f);solid.transform.SetPositionAndRotation(new Vector3(collar.x,floor+1.8f,collar.y)-face*.55f,turn);
                solid.GetComponent<BoxCollider>().size=seamSize;var carve=solid.GetComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.size=seamSize;carve.carving=true;carve.carveOnlyStationary=false;
                // The jack at its foot: a squat iron post, its screw and the red hand wheel facing the room.
                var jackAt=JackOf(n);var jackFoot=On(kit,jackAt);var jack=new ZoneKit.Shape();
                jack.Slot(0).Box(jackFoot+Vector3.up*.35f,new Vector3(.9f,.7f,.9f),Quaternion.LookRotation(face),IronDark,IronPale).Post(jackFoot+Vector3.up*.7f,1.6f,.16f,.14f,10,IronPale,IronDark)
                    .Box(jackFoot+Vector3.up*2.36f,new Vector3(.7f,.3f,.7f),Quaternion.LookRotation(face),IronDark,IronPale);
                jack.Slot(0).Beam(jackFoot+Vector3.up*2.36f,centre+(jackFoot+Vector3.up*2.36f-centre).normalized*(outer+.1f)-face*.1f,.22f,.22f,IronDark,IronPale);
                var drawnJack=kit.Built("Joint "+n+" jack",jack,IronMat);drawnJack.transform.SetParent(root.transform,true);ZoneKit.Block(drawnJack,jackFoot+Vector3.up*.5f,new Vector3(.9f,1,.9f),Quaternion.LookRotation(face).eulerAngles.y,"Jack");
                drawnJack.isStatic=true;
                var wheel=WaterKit.Wheel(kit,"Joint "+n+" wheel",.48f,IronMat,new Color(.84f,.22f,.16f),new Color(.6f,.2f,.14f),6);
                wheel.transform.SetPositionAndRotation(jackFoot+Vector3.up*1.25f+face*.52f,Quaternion.LookRotation(face));wheel.transform.SetParent(root.transform,true);
                // The collar's lamp, on the jack's head where the hand works it: dim glass while it holds, the plant's warm white once it has given. Over the ring it hung five metres up
                // on nothing (the ring stands in an opening), and out of frame at the jack.
                var lampAt=jackFoot+Vector3.up*2.76f+face*.05f;var housing=new ZoneKit.Shape();housing.Slot(0).Box(lampAt,new Vector3(1.1f,.5f,.5f),turn,IronDark,IronPale);
                var drawnHousing=kit.Built("Joint "+n+" lamp housing",housing,IronMat);drawnHousing.transform.SetParent(drawnJack.transform,true);
                GameObject LampGlass(string state,Material mat){var g=new ZoneKit.Shape();g.Slot(0).Box(lampAt+face*.27f,new Vector3(.8f,.26f,.06f),turn,Color.white);var go=kit.Built("Joint "+n+" lamp "+state,g,mat);go.transform.SetParent(drawnJack.transform,true);return go;}
                var shutLamp=LampGlass("shut",LampDim);var openLamp=LampGlass("open",LampWarm);openLamp.SetActive(false);
                var glow=kit.Lamp(new Vector3(lampAt.x,0,lampAt.z)+face*1.4f+Vector3.up*(lampAt.y-kit.Y(lampAt.x,lampAt.z)),9,.4f,new Color(1,.9f,.74f));glow.name="Joint light";glow.transform.SetParent(root.transform,true);
                // Its number, painted on the flange's top in the station's white.
                var number=Painted(kit,"joint "+n,n.ToString(),centre+Vector3.up*(outer-.15f)+face*.36f-side*1.5f,side,face,.75f,new Color(.92f,.9f,.84f));number.transform.SetParent(drawnRing.transform,true);
                // The hand.
                var at=HandOf(n);
                hand=kit.Discovery("Joint "+n+" jack",new Vector3(at.x,0,at.y),"Take the joint's weight",flag,critical?"Joint":"",null,"Taren",null,0,2.6f);hand.situation="joint";
                var joint=root.AddComponent<SeparationJoint>();joint.flag=flag;joint.point=hand;joint.wheel=wheel.transform;joint.wheelAxis=Vector3.forward;joint.wheelTurns=2;
                joint.dogs=dogs.ToArray();joint.collar=drawnRing.transform;joint.leaves=leaves.ToArray();joint.leafTravel=travel.ToArray();
                joint.blocks=blocks;joint.shutLamps=shutLamp;joint.openLamps=openLamp;joint.glow=glow;
                var shows=new Bounds(centre,new Vector3(outer*2.2f,outer*2,2));shows.Encapsulate(On(kit,at));joint.shows=shows;
                foreach(var t in new[]{drawnRing,drawnHousing,shutLamp,openLamp})ZoneKit.Fadeable(t);
                return joint;
            }

            // ---------------------------------------------------------------- a press
            /// <summary>A press frame across one or more bays: columns at the given plan points, a crown over them at <paramref name="crown"/> metres, and under the crown a ram for each bay,
            /// each its own <see cref="DrivenPress"/> on the rig's flag, its lamps (cyan while driven, warm once freed), and its cable to the rig's clamp through <paramref name="via"/>.
            /// The bays' floor is dark anvil plate with the station's yellow-and-black paint round it, outside the marking, so a marking's rim always lies on dark iron.</summary>
            static DrivenPress[] Press(ZoneKit kit,string name,(Vector2 origin,float width)[] bays,Vector2 along,float length,Vector2[] columns,float crown,string rigFlag,Vector3 clamp,Vector3[] via,bool pins=false,bool plateWhole=true,float downAt=.6f,bool paired=false)
            {
                var root=new GameObject("Press "+name);var axis=new Vector3(along.x,0,along.y).normalized;var across=Vector3.Cross(Vector3.up,axis);
                float floor=kit.Y(bays[0].origin.x,bays[0].origin.y);root.transform.position=new Vector3(bays[0].origin.x,floor,bays[0].origin.y);
                // Paired, each bay is a press of its own: its two columns, its crown and its rods are one frame, and its ram and control box hang from it. Drawn as one, a row of three
                // was a gantry 24 m across standing between the camera and the gallery's fight (QaWorldSecondTests), and the rams, hung from nothing solid, read as floating.
                int frameCount=paired?bays.Length:1;var frames=new ZoneKit.Shape[frameCount];for(int f=0;f<frameCount;f++)frames[f]=new ZoneKit.Shape();
                int FrameOf(int column)=>paired?Mathf.Min(frameCount-1,column/2):0;
                for(int ci=0;ci<columns.Length;ci++)
                {
                    var foot=On(kit,columns[ci]);
                    frames[FrameOf(ci)].Slot(0).Box(foot+Vector3.up*crown*.5f,new Vector3(.9f,crown,.9f),Quaternion.LookRotation(axis),IronDark,IronPale).Box(foot+Vector3.up*.25f,new Vector3(1.4f,.5f,1.4f),Quaternion.LookRotation(axis),IronDark,IronPale);
                }
                // A crown joins the columns in a line, or (paired) each two: a frame over each bay alone, its crown running away from the camera, so nothing wide stands across a fight.
                for(int i=1;i<columns.Length;i+=paired?2:1){var a=On(kit,columns[i-1],crown+.4f);var b=On(kit,columns[i],crown+.4f);a.y=b.y=floor+crown+.4f;frames[FrameOf(i)].Slot(0).Beam(a,b,.8f,.9f,IronDark,IronPale);}
                // The bays' plate and paint: one plate under a line of bays that touch, each its own where they stand apart.
                var plate=new ZoneKit.Shape();var paint=new ZoneKit.Shape();
                void Bed(Vector3 o,float len,float wide)
                {
                    var c=o+axis*len*.5f;c.y=floor+.018f;plate.Slot(0).Box(c,new Vector3(wide+1,.036f,len+1),Quaternion.LookRotation(axis),ZoneKit.Shape.Dim(IronDark,.62f),ZoneKit.Shape.Dim(IronDark,.72f),.5f);
                    // The paint: a band .4 wide round the plate's rim, a hand outside the marking's .5 margin.
                    foreach(int s in new[]{-1,1})
                    {
                        var e=c+across*s*(wide*.5f+.7f);e.y=floor+.024f;paint.Slot(0).Box(e,new Vector3(.4f,.012f,len+1.8f),Quaternion.LookRotation(axis),Color.white,Color.white,.8f);
                        var f=c+axis*s*(len*.5f+.7f);f.y=floor+.024f;paint.Slot(0).Box(f,new Vector3(wide+1,.012f,.4f),Quaternion.LookRotation(axis),Color.white,Color.white,.8f);
                    }
                }
                if(plateWhole)
                {
                    var lo=bays.Select(b=>Vector3.Dot(new Vector3(b.origin.x,0,b.origin.y),across)-b.width*.5f).Min();var hi=bays.Select(b=>Vector3.Dot(new Vector3(b.origin.x,0,b.origin.y),across)+b.width*.5f).Max();
                    var o0=new Vector3(bays[0].origin.x,floor,bays[0].origin.y);var o=o0+across*((lo+hi)*.5f-Vector3.Dot(o0,across));Bed(o,length,hi-lo);
                }
                else foreach(var b in bays)Bed(new Vector3(b.origin.x,floor,b.origin.y),length,b.width);
                // Each ram's two guide rods, fixed in the crown: the ram rides down them.
                for(int bi=0;bi<bays.Length;bi++)
                {
                    var (origin,width)=bays[bi];
                    var mid=new Vector3(origin.x,floor,origin.y)+axis*length*.5f;float low=floor+(pins?downAt:.6f)+.95f;
                    foreach(int s in new[]{-1,1}){var foot=mid+across*s*(width*.5f-.7f);foot.y=low;frames[paired?bi:0].Slot(0).Post(foot,floor+crown-low,.11f,.11f,8,IronPale,IronDark);}
                }
                var drawnFrames=new GameObject[frameCount];
                for(int f=0;f<frameCount;f++)
                {
                    drawnFrames[f]=kit.Built("Press "+name+" frame"+(paired?" "+(f+1):""),frames[f],IronMat);drawnFrames[f].transform.SetParent(root.transform,true);drawnFrames[f].isStatic=true;
                }
                for(int ci=0;ci<columns.Length;ci++)ZoneKit.Block(drawnFrames[FrameOf(ci)],On(kit,columns[ci],crown*.5f),new Vector3(.9f,crown,.9f),Quaternion.LookRotation(axis).eulerAngles.y,"Press column");
                var bed=kit.Built("Press "+name+" bed",plate,IronMat);bed.transform.SetParent(root.transform,true);bed.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                var band=kit.Built("Press "+name+" paint",paint,HazardMat);band.transform.SetParent(root.transform,true);band.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                var presses=new List<DrivenPress>();
                for(int i=0;i<bays.Length;i++)
                {
                    var (origin,width)=bays[i];var o=new Vector3(origin.x,floor+.02f,origin.y);var mid=o+axis*length*.5f;
                    var hangsFrom=drawnFrames[paired?i:0].transform;
                    var holder=new GameObject("Press "+name+" "+(i+1));holder.transform.SetParent(hangsFrom,true);holder.transform.position=o;
                    // The ram, in its own space: a heavy block, its striking face, a crosshead on top, and the station's yellow-and-black round its foot (what drops is painted so).
                    var ram=new ZoneKit.Shape();var turn=Quaternion.LookRotation(axis);
                    ram.Slot(0).Box(Vector3.up*.1f,new Vector3(width-.5f,.9f,length-.5f),turn,ZoneKit.Shape.Dim(IronDark,.8f),IronPale).Box(Vector3.down*.62f,new Vector3(width-.3f,.16f,length-.3f),turn,ZoneKit.Shape.Dim(IronDark,.6f))
                        .Box(Vector3.up*.75f,new Vector3(width-1.6f,.4f,length-1.6f),turn,ZoneKit.Shape.Dim(IronDark,.8f),IronPale);
                    ram.Slot(1).Box(Vector3.down*.4f,new Vector3(width-.46f,.32f,length-.46f),turn,Color.white,Color.white,.9f);
                    var drawnRam=kit.Built("Press "+name+" "+(i+1)+" ram",ram,IronMat,HazardMat);drawnRam.transform.SetParent(holder.transform,false);
                    var ramUp=mid+Vector3.up*(crown-2.7f);var ramDown=mid+Vector3.up*downAt;drawnRam.transform.position=pins?ramDown:ramUp;drawnRam.transform.rotation=Quaternion.identity;
                    // The control box on the crown over the bay, where its cable comes in.
                    var lampAt=mid+Vector3.up*(crown+.2f)-axis*.55f;
                    var cabinet=new ZoneKit.Shape();cabinet.Slot(0).Box(lampAt+Vector3.up*.35f,new Vector3(1.1f,.6f,.7f),turn,IronDark,IronPale);
                    var drawnBox=kit.Built("Press "+name+" "+(i+1)+" control box",cabinet,IronMat);drawnBox.transform.SetParent(hangsFrom,true);
                    // Its lamps on the ram's own face, toward the camera, so a player at the bay's edge sees whose it is: the rig's cyan while driven, the plant's warm once it is home's.
                    var face=-axis*((length-.5f)*.5f+.035f)+Vector3.up*.18f;
                    GameObject Lamps(string state,Material mat,Color light)
                    {
                        var g=new ZoneKit.Shape();g.Slot(0).Box(face,new Vector3(width-1.1f,.2f,.05f),turn,Color.white);
                        foreach(int side in new[]{-1,1})g.Slot(0).Box(face+across*side*(width*.5f-.45f)+Vector3.up*.02f,new Vector3(.3f,.3f,.06f),turn,Color.white);
                        var go=kit.Built("Press "+name+" "+(i+1)+" lamps "+state,g,mat);go.transform.SetParent(drawnRam.transform,false);go.transform.localPosition=Vector3.zero;
                        if(i==bays.Length/2){var l=new GameObject("Press light",typeof(Light)).GetComponent<Light>();l.type=LightType.Point;l.color=light;l.range=8;l.intensity=1.2f;l.shadows=LightShadows.None;
                            l.transform.SetParent(go.transform,false);l.transform.localPosition=face-axis*1.2f;}
                        return go;
                    }
                    var driven=Lamps("driven",LampCyan,new Color(.5f,.95f,1));var local=Lamps("local",LampWarm,new Color(1,.9f,.74f));local.SetActive(false);
                    var press=holder.AddComponent<DrivenPress>();press.along=axis;press.length=length;press.width=width;press.rigFlag=rigFlag;press.pins=pins;
                    press.ram=drawnRam.transform;press.ramUp=ramUp;press.ramDown=ramDown;press.drivenLamps=driven;press.localLamps=local;press.climb=.35f;
                    // Its cable: from the crown over the bay to the rig's clamp, through the hangers given; drawn taut here so a still shows it as play first does.
                    var line=new GameObject("Press "+name+" "+(i+1)+" cable",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(holder.transform,true);
                    line.sharedMaterial=CableMat;line.widthMultiplier=.075f;line.numCapVertices=2;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.useWorldSpace=true;
                    var hangers=new List<Vector3>{lampAt+axis*.3f+Vector3.up*.3f};if(via!=null)hangers.AddRange(via);hangers.Add(clamp);
                    press.cable=line;press.hangers=hangers.ToArray();press.floor=floor;Taut(line,press.hangers,floor,press.cableBody);
                    if(pins)
                    {
                        var hold=new GameObject("Press "+name+" "+(i+1)+" pinned",typeof(Membrane));hold.transform.SetParent(holder.transform,true);
                        var box=new GameObject("Pinned ram",typeof(BoxCollider),typeof(NavMeshObstacle));box.transform.SetParent(hold.transform,false);
                        var size=new Vector3(width,2.2f,length);box.transform.SetPositionAndRotation(mid+Vector3.up*1.1f,Quaternion.LookRotation(axis));box.GetComponent<BoxCollider>().size=size;
                        var c=box.GetComponent<NavMeshObstacle>();c.shape=NavMeshObstacleShape.Box;c.size=size;c.carving=true;c.carveOnlyStationary=false;press.pinned=hold;
                    }
                    presses.Add(press);
                }
                foreach(var f in drawnFrames)ZoneKit.Fadeable(f);
                return presses.ToArray();
            }
            /// <summary>A frame's two columns for each bay in a row, at the bay's two ends (a crown running along the bay, away from the camera).</summary>
            static Vector2[] Ends(float[] xs,float z0,float length)=>xs.SelectMany(x=>new[]{new Vector2(x,z0-.75f),new Vector2(x,z0+length+.75f)}).ToArray();
            /// <summary>A cable drawn taut through its hangers, as <see cref="DrivenPress"/> draws it while driven (for editor stills: play redraws it every frame).</summary>
            static void Taut(LineRenderer line,Vector3[] hangers,float floor,Color body)
            {
                const int Steps=8;int count=(hangers.Length-1)*Steps+1;line.positionCount=count;
                for(int i=0;i+1<hangers.Length;i++)
                {
                    Vector3 a=hangers[i],b=hangers[i+1];float span=Vector3.Distance(a,b);float sag=Mathf.Min(.04f*span,(Mathf.Min(a.y,b.y)-floor-.06f)*.5f);
                    for(int k=0;k<Steps;k++){float t=k/(float)Steps;line.SetPosition(i*Steps+k,Vector3.Lerp(a,b,t)+Vector3.down*sag*4*t*(1-t));}
                }
                line.SetPosition(count-1,hangers[^1]);BridleHold.Pulse(line,.3f,body,new Color(.72f,1,1),.9f);
            }
            /// <summary>A rig's post: a plant control cabinet with the Compact's white clamp on its face (its cyan strip lit until the rig is cut, dark after). The rig itself is the
            /// encounter's body, standing at the cabinet. Returns the clamp's point, where the cables end.</summary>
            static Vector3 RigPost(ZoneKit kit,string name,Vector2 at,Vector2 faces,string rigFlag)
            {
                var foot=On(kit,at);var face=new Vector3(faces.x,0,faces.y);var turn=Quaternion.LookRotation(face);
                var cabinet=new ZoneKit.Shape();cabinet.Slot(0).Box(foot+Vector3.up*.75f,new Vector3(1.1f,1.5f,.7f),turn,IronDark,IronPale).Box(foot+Vector3.up*1.6f,new Vector3(1.2f,.2f,.8f),turn,IronDark,IronPale);
                cabinet.Slot(1).Box(foot+Vector3.up*1.05f+face*.38f,new Vector3(.8f,.6f,.16f),turn,new Color(.92f,.93f,.95f));
                var drawn=kit.Built(name+" post",cabinet,IronMat,WhiteMat);ZoneKit.Block(drawn,foot+Vector3.up*.8f,new Vector3(1.1f,1.6f,.8f),turn.eulerAngles.y,"Rig post");drawn.isStatic=true;
                var strip=new ZoneKit.Shape();strip.Slot(0).Box(foot+Vector3.up*1.22f+face*.47f,new Vector3(.62f,.08f,.03f),turn,Color.white);
                var lit=kit.Built(name+" clamp lit",strip,LampCyan);lit.transform.SetParent(drawn.transform,true);
                var dead=kit.Built(name+" clamp dead",strip,LampDim);dead.transform.SetParent(drawn.transform,true);
                var light=kit.Lamp(new Vector3(at.x,1.4f,at.y)+face*.9f,5,1.1f,new Color(.5f,.95f,1));light.name="Rig light";light.transform.SetParent(lit.transform,true);
                Gate(name+" clamp (lit until cut)",rigFlag,false,lit);Gate(name+" clamp (dark once cut)",rigFlag,true,dead);dead.SetActive(false);
                return foot+Vector3.up*1.3f+face*.42f;
            }

            // ---------------------------------------------------------------- the zone
            internal static void Build()
            {
                Materials();
                var kit=ZoneKit.Ground(WorksZone,Height,1.2f);bool greybox=DevArgs.Has("-works-greybox");
                var scene=WorldBuilder.Begin(WorksZone);
                // Bodies meet the keel as Height has it; the camera sees it as Seen draws it (D272), and only the rooms' own floors are drawn as floor.
                kit.Terrain("Works floors",-36,38,-16,220,.5f,FloorMat,KeelMat,Tone,new Color(.84f,.86f,.9f),new Color(.6f,.62f,.66f),Seen,p=>-FloorAt(p,out _));

                // ---- the hatch: the way up, the cradle, the arrival
                WorldBuilder.Spawn("Arrival",On(kit,Arrival));WorldBuilder.Spawn("PressHall",On(kit,Checkpoint));
                var stair=HatchStair(kit);kit.Way(stair,"Climb to the promenade — The Decks",DecksZone,DecksFromWorks,null,null,3.4f,new Vector2(3.4f,-10));
                Cradle(kit,"Hatch cradle",HatchCradle,70,"Arrival");

                // ---- the tug's hold: the first joint, by hand, with nothing awake; Dorrow at it
                kit.Crossing("The first joint",new Vector3(0,0,29.6f),new Vector3(23,5,3),"home.worksRead","DorrowWorks","Taren");
                var joints=new SeparationJoint[7];var hands=new DiscoveryPoint[7];
                for(int n=1;n<=6;n++)joints[n]=Joint(kit,n,out hands[n]);
                Dorrow(kit);

                // ---- the Compact module (through joint 4): two crew and a cache
                Fight(kit,"Works_Module",new Vector2(20.2f,20),new Vector2(15,16.5f),"home.joint4",("CompactWarden","MusterPikeguard",2,new Vector2(19,22),270,2.1f));
                Stash(kit,"home.works.module","Compact module locker",ModuleCache,-90,180,Hull.Module,ProgressionBuilder.AnyPart(5,GearSlot.Module,1),ProgressionBuilder.Drop("HaloRivet",3,1),ProgressionBuilder.Drop("RepairGel",2,1));

                // ---- the hauler's throat: the rule taught, the rig alone awake. Three presses across the only way north, their rig at the east jamb, their cables in plain sight.
                var throatClamp=RigPost(kit,"Throat rig",ThroatRig,new Vector2(-.6f,-.8f).normalized,"clear."+ThroatRigFight);
                                Press(kit,"throat",new[]{(new Vector2(-4.9f,62.4f),4.8f),(new Vector2(0,62.4f),4.8f),(new Vector2(4.9f,62.4f),4.8f)},Vector2.up,5,new[]{new Vector2(-8.1f,64.9f),new Vector2(8.1f,64.9f)},5.8f,
                    // Each cable straight from its press to the rig: three lines fanning in to one post read as "these three, driven from there".
                    "clear."+ThroatRigFight,throatClamp,null);
                var throatRig=Rig(kit,ThroatRigFight,ThroatRig+new Vector2(-.8f,-1.1f),220,new Vector2(1.5f,57),new Vector2(24,9),new Vector2(0,47),18);
                var sight=new GameObject("First sight of a rig",typeof(FirstSight)).GetComponent<FirstSight>();sight.transform.position=On(kit,ThroatRig);sight.situation="override";sight.range=15;sight.onceFlag="home.works.sawRig";

                // ---- the baggage bay (through joint 5): the wedding table, a cache
                WeddingTable(kit);
                Stash(kit,"home.works.baggage","Baggage-bay trunk",BaggageCache,90,160,Hull.Liner,ProgressionBuilder.AnyPart(5,GearSlot.Drive,1),ProgressionBuilder.Drop("NacreShard",2,1),ProgressionBuilder.Drop("ChargeCell",2,1));

                // ---- joint 2: a press stands on its jack, driven from the control room. Its cable runs along the hauler's north wall, down its east wall and up the stair to the rig.
                var controlClamp=RigPost(kit,"Control rig",ControlRig+new Vector2(0,1.4f),new Vector2(0,-1),"clear."+ControlRigFight);
                var jack2=JackOf(2);
                Press(kit,"on joint 2",new[]{(new Vector2(jack2.x,jack2.y-3.7f),3.6f)},Vector2.up,4.4f,new[]{new Vector2(jack2.x,jack2.y-4.1f),new Vector2(jack2.x,jack2.y+1.2f)},6.4f,
                    "clear."+ControlRigFight,controlClamp,new[]{new Vector3(9.5f,HaulerFloor+5.6f,96.6f),new Vector3(10.5f,HaulerFloor+5.2f,96.4f),new Vector3(10.6f,HaulerFloor+4.4f,86.4f),
                        new Vector3(15.6f,ControlFloor+3.4f,85.2f),new Vector3(20.4f,ControlFloor+2.6f,90.6f)},pins:true,downAt:3.2f);
                hands[2].requiredFlag="clear."+ControlRigFight;
                kit.Sign("CONTROL ROOM",19.6f,80.4f,1.6f);
                Fight(kit,ControlRigFight,new Vector2(24.6f,84),new Vector2(15,19),null,("OverrideRig","SnagvineNode",1,ControlRig,180,0));
                Fight(kit,"Works_Control",new Vector2(24.6f,84),new Vector2(15,19),null,("CompactWarden","MusterPikeguard",2,new Vector2(22.6f,84),180,2.4f));
                ControlRoom(kit);

                // ---- the liner's press gallery: five hunting presses on one rig, a crew and a Weld Arm. Use the hammers on the crew, or cut the rig first.
                var galleryClamp=RigPost(kit,"Gallery rig",GalleryRig,new Vector2(-1,0),"clear."+GalleryRigFight);
                var galleryVia=new[]{new Vector3(15.6f,LinerFloor+5.4f,121.4f)};
                Press(kit,"gallery south",new[]{(new Vector2(-10,112),5f),(new Vector2(0,112),5f),(new Vector2(10,112),5f)},Vector2.up,5,Ends(new[]{-10f,0,10},112,5),5.6f,
                    "clear."+GalleryRigFight,galleryClamp,galleryVia,false,false,.6f,true);
                Press(kit,"gallery north",new[]{(new Vector2(-5,127.5f),5f),(new Vector2(5,127.5f),5f)},Vector2.up,5,Ends(new[]{-5f,5},127.5f,5),5.6f,
                    "clear."+GalleryRigFight,galleryClamp,galleryVia,false,false,.6f,true);
                Fight(kit,GalleryRigFight,new Vector2(0,123),new Vector2(31,40),null,("OverrideRig","SnagvineNode",1,GalleryRig+new Vector2(-1.1f,0),270,0));
                // The crews give ground and walk off at nought (the fights contract): each crew stands with nine metres of open floor behind it, away from the way the party comes in.
                // No BossExit for them: BossExit.For takes the first exit of an id in the scene, so a hatch in each room would send every crew toward one room's hatch.
                var gallery=Fight(kit,"Works_Gallery",new Vector2(0,123),new Vector2(31,40),null,("CompactWarden","MusterPikeguard",3,new Vector2(0,122.5f),180,2.6f),("WeldArm","MusterExcavator",1,new Vector2(-11,140.5f),200,0));
                var arm=new GameObject("First sight of a weld arm",typeof(FirstSight)).GetComponent<FirstSight>();arm.transform.position=On(kit,new Vector2(-11,140));arm.situation="weldarm";arm.encounter=gallery;arm.onceFlag="home.works.sawArm";

                // ---- the courier skiff (through joint 6): a Weld Arm, a cache
                var skiff=Fight(kit,"Works_Skiff",new Vector2(25,134),new Vector2(14,16),"home.joint6",("WeldArm","MusterExcavator",1,new Vector2(29.6f,138.6f),225,0));
                var arm2=new GameObject("First sight of a weld arm (the skiff)",typeof(FirstSight)).GetComponent<FirstSight>();arm2.transform.position=On(kit,new Vector2(28,138));arm2.situation="weldarm";arm2.encounter=skiff;arm2.onceFlag="home.works.sawArm";
                Stash(kit,"home.works.skiff","Courier's lockbox",SkiffCache,-90,200,Hull.Skiff,ProgressionBuilder.AnyPart(5,GearSlot.Edge,1),ProgressionBuilder.Drop("RingKeystone",1,.4f),ProgressionBuilder.Drop("HaloRivet",2,1));

                // ---- the landing and the press hall, to the boss arena contract
                kit.Sign("PRESS HALL",-6.4f,167.4f,1.8f);
                Cradle(kit,"Press hall cradle",HallCradle,-100,"PressHall");
                PressHall(kit);

                // ---- limits: the keel's walls are the stop; a box on each side keeps a body from finding a way up one.
                WorldBuilder.Boundary("South limit",new Vector3(0,4,-15),new Vector3(80,30,2));WorldBuilder.Boundary("North limit",new Vector3(0,4,219),new Vector3(80,30,2));
                WorldBuilder.Boundary("West limit",new Vector3(-35,4,102),new Vector3(2,30,240));WorldBuilder.Boundary("East limit",new Vector3(37,4,102),new Vector3(2,30,240));

                if(!greybox)Dress(kit);
                // For stills of the Works as the party leaves it (never saved as the zone: rebuild without it): every joint open, every rig cut, every press at rest.
                if(DevArgs.Has("-works-open"))Opened(joints);
                if(DevArgs.Has("-works-stills"))Stills(kit);
                // ---- navigation, with every joint open and every pin lifted: what is shut at first is play's business.
                var shut=new List<GameObject>();
                foreach(var j in joints)if(j!=null&&j.blocks!=null)shut.Add(j.blocks);
                foreach(var p in UnityEngine.Object.FindObjectsByType<DrivenPress>(FindObjectsSortMode.None))if(p.pinned!=null)shut.Add(p.pinned);
                var were=shut.Select(g=>g.activeSelf).ToArray();foreach(var g in shut)g.SetActive(false);
                var reach=new List<Vector2>{Arrival,Arrival+new Vector2(2,0),HatchCradle+new Vector2(1.6f,0),new(3.4f,-6),new(0,10),new(0,22),new(-6,24),new(6,14),DorrowAt+new Vector2(1.4f,-1),
                    new(0,38),new(0,46),new(-10,50),new(10,50),new(0,55),new(0,65),new(0,75),new(-10,88),new(10,90),new(0,99),
                    new(15,82),new(20,82),new(28,80),new(24.6f,88),new(14,50),new(18,46),TableHand,BaggageCache+new Vector2(1.6f,0),new(14,20),new(20,16),new(24,24),ModuleCache+new Vector2(-1.6f,0),
                    new(0,106),new(-12,106),new(12,106),new(-5,120),new(8,122),new(0,136),new(-12,138),new(12,140),GalleryRig+new Vector2(-2,-1.6f),new(20,134),new(26,130),SkiffCache+new Vector2(-1.6f,0),
                    new(0,149),new(0,156),new(0,160),Checkpoint,HallCradle+new Vector2(-1.6f,0),new(0,172),new(0,178),new(-14,184),new(14,184),new(-18,200),new(18,200),new(0,208),LiftAt+new Vector2(-2.4f,-1)};
                for(int n=1;n<=6;n++)reach.Add(HandOf(n));
                kit.BakeNavigation(new Bounds(new Vector3(1,-2,102),new Vector3(74,30,240)),Arrival,reach);
                for(int i=0;i<shut.Count;i++)shut[i].SetActive(were[i]);
                if(!greybox)Ground(kit);
                WorldBuilder.Save(scene,WorksZone);Debug.Log("SEPARATION_WORKS_OK");
            }
            /// <summary>For stills only: every joint open, every rig's presses at rest and their cables dark.</summary>
            static void Opened(SeparationJoint[] joints)
            {
                foreach(var j in joints)
                {
                    if(j==null)continue;if(j.blocks!=null)j.blocks.SetActive(false);if(j.shutLamps!=null)j.shutLamps.SetActive(false);if(j.openLamps!=null)j.openLamps.SetActive(true);
                    for(int i=0;i<j.leaves.Length;i++)j.leaves[i].position+=j.leafTravel[i];
                }
                foreach(var p in UnityEngine.Object.FindObjectsByType<DrivenPress>(FindObjectsSortMode.None))
                {
                    if(p.pinned!=null)p.pinned.SetActive(false);p.ram.position=p.ramUp;p.drivenLamps.SetActive(false);p.localLamps.SetActive(true);
                    BridleHold.Pulse(p.cable,0,p.cableDead,p.cableDead,0);
                }
                Debug.Log("WORKS_STILLS_OPEN");
            }

            /// <summary>For stills only (never saved as the zone: rebuild without it). What play stands in the scene and an editor still can't show: each rig's Override Rig at its
            /// post (the encounter's own body, shown inert from the room's door in play), and the Foundry Saint at P with its six arms raised (the fight's bodies), posed as play poses them.</summary>
            static void Stills(ZoneKit kit)
            {
                var holder=new GameObject("Stills only (never saved as the zone)");int n=0;
                GameObject Model(string folder,string id)
                {
                    var prefab=ZoneKit.Prefab(folder,id);if(prefab==null)return null;var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    foreach(var mb in go.GetComponentsInChildren<MonoBehaviour>())UnityEngine.Object.DestroyImmediate(mb);foreach(var c in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
                    go.transform.SetParent(holder.transform,true);n++;return go;
                }
                foreach(var (at,yaw) in new[]{(ThroatRig+new Vector2(-.8f,-1.1f),220f),(ControlRig,180f),(GalleryRig+new Vector2(-1.1f,0),270f)})
                {
                    var rig=Model("Enemies","OverrideRig");if(rig==null)continue;rig.transform.rotation=Quaternion.Euler(0,yaw,0);
                    var b=ModelGeometry.BoundsOf(rig);rig.transform.position+=On(kit,at)-new Vector3(b.center.x,b.min.y,b.center.z);
                }
                var saint=Model("Enemies","FoundrySaint");
                if(saint!=null){var b=ModelGeometry.BoundsOf(saint);saint.transform.localScale*=12/Mathf.Max(.01f,b.size.y);b=ModelGeometry.BoundsOf(saint);saint.transform.position+=On(kit,P)-new Vector3(b.center.x,b.min.y,b.center.z);}
                foreach(var (index,at,bearing) in Arms)
                {
                    var arm=Model("Environment","SaintArm");if(arm==null)continue;
                    // As SaintArm.Align lays it: along its swing's forward, its ring end at the collar, seven metres long; the swing pitched fifty degrees up along its bearing.
                    var swing=new GameObject("Raised arm "+index).transform;swing.SetParent(holder.transform,false);swing.SetPositionAndRotation(On(kit,at,2),Quaternion.Euler(0,bearing,0)*Quaternion.Euler(-50,0,0));
                    arm.transform.SetParent(swing,false);arm.transform.localRotation=Quaternion.identity;arm.transform.localPosition=Vector3.zero;arm.transform.localScale=Vector3.one;
                    var box=new Bounds();bool first=true;
                    foreach(var f in arm.GetComponentsInChildren<MeshFilter>())
                    {
                        if(f.sharedMesh==null)continue;var mb=f.sharedMesh.bounds;
                        for(int k=0;k<8;k++){var c=new Vector3((k&1)==0?mb.min.x:mb.max.x,(k&2)==0?mb.min.y:mb.max.y,(k&4)==0?mb.min.z:mb.max.z);var p=arm.transform.InverseTransformPoint(f.transform.TransformPoint(c));if(first){box=new Bounds(p,Vector3.zero);first=false;}else box.Encapsulate(p);}
                    }
                    // Which end carries the ring is SaintArm's to say (Tune.RingEnd: the model can't be read to tell); copied as a guess, the stills stood the flanges at the hands.
                    bool alongX=box.size.x>box.size.z;float halfLength=(alongX?box.size.x:box.size.z)*.5f;float ringEnd=SaintArm.Tune.RingEnd;
                    var turn=alongX?Quaternion.Euler(0,ringEnd>0?90:-90,0):Quaternion.Euler(0,ringEnd>0?180:0,0);
                    var ring=alongX?new Vector3(box.center.x+ringEnd*halfLength,box.center.y,box.center.z):new Vector3(box.center.x,box.center.y,box.center.z+ringEnd*halfLength);
                    arm.transform.localRotation=turn;arm.transform.localPosition=-(turn*ring);float scale=SaintArm.Tune.Length/Mathf.Max(.1f,halfLength*2);arm.transform.localScale=Vector3.one*scale;arm.transform.localPosition*=scale;
                }
                Debug.Log("WORKS_STILLS "+n);
            }
            /// <summary>Work let into each deck, flush with it (under a marking's lift, never solid): the hauler's cargo rails down its length and its lashing rings, the tug's tow track,
            /// the press hall's old separation rails across its south half and iron decking along its walls. So no deck is one plain sheet.</summary>
            static void FloorWork(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var rails=new Color(.6f,.62f,.64f);var dark=new Color(.26f,.27f,.29f);
                void Rail(Vector2 a,Vector2 b,float wide=.16f){var p0=new Vector3(a.x,kit.Y(a.x,a.y)+.012f,a.y);var p1=new Vector3(b.x,kit.Y(b.x,b.y)+.012f,b.y);s.Slot(0).Beam(p0,p1,wide,.024f,ZoneKit.Shape.Dim(rails,.8f),rails);}
                // The hauler's cargo rails, two pairs down its length, broken where the throat's bays and joint 2's press stand.
                foreach(float x in new[]{-6.5f,-5.5f,5.5f,6.5f}){Rail(new(x,42.4f),new(x,60.4f));Rail(new(x,69.6f),new(x,90.6f));}
                foreach(float z in new[]{44f,48f,52f,56f,72f,76f,80f,84f,88f})foreach(float x in new[]{-8.6f,8.2f})s.Slot(0).Disc(new Vector3(x,kit.Y(x,z)+.02f,z),Vector3.up,.32f,12,dark,.2f,rails);
                // The tug's tow track down its middle, from the hatch to the bow.
                foreach(float x in new[]{-.9f,.9f})Rail(new(x,-9),new(x,30),.12f);
                // The press hall: two separation rails across its south half (clear of every arm's strip and the head's circle), and iron decking along its walls.
                foreach(float z in new[]{177.6f,179.4f})Rail(new(-22,z),new(22,z),.22f);
                var rng=new System.Random(37);var deck=new Color(.44f,.45f,.48f);
                void Plate(Vector3 c,float yaw){float shade=.7f+.3f*(float)rng.NextDouble();s.Slot(1).Box(c,new Vector3(1.54f,.03f,1.54f),Quaternion.Euler(0,yaw,0),ZoneKit.Shape.Dim(dark,shade),ZoneKit.Shape.Dim(deck,shade),.5f);}
                for(float x=-22.4f;x<=22.4f;x+=1.6f)foreach(float z in new[]{211.8f,210.2f})Plate(new Vector3(x,kit.Y(x,z)+.015f,z),((float)rng.NextDouble()-.5f)*1.5f);
                for(float z=176.4f;z<=208.6f;z+=1.6f)foreach(float x in new[]{-22.2f,22.2f})Plate(new Vector3(x,kit.Y(x,z)+.015f,z),0);
                var work=kit.Built("Floor work",s,IronMat,IronMat);work.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            /// <summary>The steel stair up to the promenade's hatch, along the hatch room's east wall: stringers, treads, a rail, the hatch's coaming overhead with the promenade's light
            /// falling through it. Solid underneath (nobody walks under it). The way up's body.</summary>
            static GameObject HatchStair(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var foot=new Vector3(5.6f,TugFloor,-10.6f);const float rise=4.6f,run=7.4f;var dir=Vector3.forward;
                for(int i=0;i<16;i++){float t=(i+.5f)/16;s.Slot(0).Box(foot+dir*run*t+Vector3.up*rise*t,new Vector3(1.8f,.08f,.36f),Quaternion.identity,IronDark,IronPale);}
                foreach(int k in new[]{-1,1}){s.Slot(0).Beam(foot+Vector3.right*k*.95f,foot+Vector3.right*k*.95f+dir*run+Vector3.up*rise,.12f,.3f,IronDark,IronPale);
                    s.Slot(0).Beam(foot+Vector3.right*k*.95f+Vector3.up*1,foot+Vector3.right*k*.95f+dir*run+Vector3.up*(rise+1),.06f,.06f,new Color(.86f,.7f,.2f));}
                s.Slot(0).Box(foot+dir*(run+.9f)+Vector3.up*(rise-.1f),new Vector3(2.2f,.16f,1.8f),Quaternion.identity,IronDark,IronPale);
                foreach(int k in new[]{-1,1})s.Slot(0).Box(foot+dir*(run+.9f)+Vector3.right*k*1.05f+Vector3.up*(rise*.5f),new Vector3(.2f,rise,.2f),Quaternion.identity,IronDark,IronPale);
                // The hatch's coaming over the landing, and the promenade's light through it.
                var c=foot+dir*(run+.9f)+Vector3.up*(rise+2.4f);
                s.Slot(0).Box(c,new Vector3(2.6f,.3f,2.4f),Quaternion.identity,ZoneKit.Shape.Dim(IronDark,.8f),IronPale);
                var stair=kit.Built("Hatch stair",s,IronMat);
                ZoneKit.Block(stair,foot+dir*run*.5f+Vector3.up*rise*.4f,new Vector3(2.1f,rise*.8f,run),0,"Stair");stair.isStatic=true;
                var shaft=kit.Lamp(new Vector3(5.6f,rise+1.4f,-2.4f),10,1.6f,new Color(.9f,.95f,1));shaft.name="Promenade light";shaft.transform.SetParent(stair.transform,true);
                ZoneKit.Fadeable(stair);
                // A prompt measures from its own object, and a built mesh's object stands at the world's origin: the way stands on a holder at the stair's foot.
                var holder=new GameObject("Hatch stair (the way up)");holder.transform.position=foot+dir*.6f+Vector3.left*1.4f;stair.transform.SetParent(holder.transform,true);
                return holder;
            }
            /// <summary>Dorrow at the first joint, his ear to the collar, from the meeting until the Saint is released.</summary>
            static void Dorrow(ZoneKit kit)
            {
                var prefab=FirstBody("DorrowBody","Hal");if(prefab==null)return;
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.name="Dorrow at the first joint";
                body.transform.SetPositionAndRotation(On(kit,DorrowAt),Quaternion.Euler(0,24,0));
                var solid=body.AddComponent<BoxCollider>();solid.size=new Vector3(.8f,1.4f,.8f);solid.center=Vector3.up*.7f;
                foreach(var rig in body.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullCompletely;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
                Hide(body,"Dorrow at the first joint",null,"home.saintReleased");
            }
            /// <summary>The Arrens' wedding table in the liner's baggage bay: wider than any hatch, built in here forty years ago, its names cut under the edge; two benches, a lamp over it,
            /// the luggage of a liner stacked round. Mira's third disputed lot (<c>SelaWeddingTable</c>).</summary>
            static void WeddingTable(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var c=On(kit,TableAt);var wood=new Color(.86f,.7f,.5f);
                s.Slot(0).Box(c+Vector3.up*.82f,new Vector3(5.6f,.12f,1.7f),Quaternion.identity,ZoneKit.Shape.Dim(wood,.8f),wood);
                s.Slot(0).Box(c+Vector3.up*.7f,new Vector3(5.4f,.14f,1.5f),Quaternion.identity,ZoneKit.Shape.Dim(wood,.6f));
                foreach(float x in new[]{-2.5f,0,2.5f})foreach(float z in new[]{-.6f,.6f})s.Slot(0).Post(c+new Vector3(x,0,z),.72f,.07f,.06f,8,ZoneKit.Shape.Dim(wood,.7f),wood);
                foreach(float z in new[]{-1.35f,1.35f}){s.Slot(0).Box(c+new Vector3(0,.46f,z),new Vector3(4.8f,.08f,.42f),Quaternion.identity,ZoneKit.Shape.Dim(wood,.8f),wood);
                    foreach(float x in new[]{-2.1f,2.1f})s.Slot(0).Box(c+new Vector3(x,.22f,z),new Vector3(.08f,.44f,.36f),Quaternion.identity,ZoneKit.Shape.Dim(wood,.7f));}
                // What was left on it: a cloth runner, a jug and four cups.
                s.Slot(1).Box(c+Vector3.up*.885f,new Vector3(4.2f,.01f,.5f),Quaternion.identity,new Color(.86f,.84f,.78f));
                s.Slot(0).Post(c+new Vector3(-.4f,.88f,.1f),.3f,.1f,.07f,8,new Color(.6f,.66f,.7f),new Color(.7f,.76f,.8f));
                foreach(float x in new[]{.3f,.7f,1.1f,1.5f})s.Slot(0).Post(c+new Vector3(x,.88f,-.2f),.1f,.04f,.05f,6,new Color(.7f,.74f,.78f),new Color(.8f,.84f,.86f));
                var table=kit.Built("The Arrens' wedding table",s,WoodMat,PlateMat);ZoneKit.Block(table,c+Vector3.up*.45f,new Vector3(5.6f,.9f,3.3f),0,"Table");table.isStatic=true;
                var lamp=kit.Lamp(new Vector3(TableAt.x,3.2f,TableAt.y),7,1.4f,new Color(1,.88f,.7f));lamp.name="Lamp light";
                var look=kit.Discovery("The wedding table",new Vector3(TableHand.x,0,TableHand.y),"Look at the table","mira.lotTable","Consignment","SelaWeddingTable","Sela","SecondInventory",0,2.8f);
            }
            /// <summary>The plant's control room over the hauler: the separation board on its north wall (a lamp for each joint: dim while shut, warm once open, read by flag), the desks in
            /// front of it, and the rig clamped on the main desk with its cable running out to joint 2's press.</summary>
            static void ControlRoom(ZoneKit kit)
            {
                var board=new ZoneKit.Shape();var wall=new Vector3(24.6f,ControlFloor,93.4f);
                board.Slot(0).Box(wall+Vector3.up*2.6f,new Vector3(11,3.6f,.3f),Quaternion.identity,IronDark,IronPale);
                // Its console under it, standing on the floor (the board alone hung at shoulder height off a wall nobody could see behind it).
                board.Slot(0).Box(wall+new Vector3(0,.42f,-.12f),new Vector3(11.2f,.84f,.62f),Quaternion.identity,ZoneKit.Shape.Dim(IronDark,.8f),IronPale);
                // The station drawn on it: six hull outlines in a row along a keel line, a ring for each joint between them.
                board.Slot(1).Box(wall+new Vector3(0,2.1f,-.17f),new Vector3(9.6f,.06f,.02f),Quaternion.identity,new Color(.84f,.8f,.66f));
                for(int i=0;i<6;i++){var h=wall+new Vector3(-4.2f+i*1.68f,2.6f,-.17f);board.Slot(1).Box(h,new Vector3(1.2f,.9f,.02f),Quaternion.identity,ZoneKit.Shape.Dim(PlateOf((Hull)i),.9f));}
                var drawn=kit.Built("Separation board",board,IronMat,PlateMat);ZoneKit.Block(drawn,wall+Vector3.up*2.2f,new Vector3(11.2f,4.4f,.62f),0,"Board");drawn.isStatic=true;
                for(int n=1;n<=6;n++)
                {
                    var at=wall+new Vector3(-4.2f+(n-.5f)*1.68f,3.6f,-.2f);
                    GameObject Lamp(string state,Material mat){var g=new ZoneKit.Shape();g.Slot(0).Disc(at,Vector3.back,.26f,12,Color.white);var go=kit.Built("Board lamp "+n+" "+state,g,mat);go.transform.SetParent(drawn.transform,true);return go;}
                    var dim=Lamp("shut",LampDim);var lit=Lamp("open",LampWarm);Gate("Board lamp "+n+" (shut)",Joints[n-1].flag,false,dim);Gate("Board lamp "+n+" (open)",Joints[n-1].flag,true,lit);lit.SetActive(false);
                }
                // The desks: the plant's consoles in two rows facing the board.
                foreach(var (x,z,yaw) in new[]{(20.4f,88.6f,0f),(28.8f,88.6f,0f),(21.2f,79f,0f),(28f,79f,0f)})kit.Prop("DeckConsole",new Vector3(x,0,z),1.3f,yaw).name="Plant console";
                var lamp=kit.Lamp(new Vector3(24.6f,3.4f,86),11,1.6f,new Color(1,.9f,.74f));lamp.name="Lamp light";
            }

            // ---------------------------------------------------------------- the press hall (the boss arena contract)
            /// <summary>The six arms' collars on the press's back, west to east, and the bearing each lies along (the contract's).</summary>
            internal static readonly (int index,Vector2 at,float bearing)[] Arms={(1,new(-4.3f,193.2f),225),(2,new(-2.6f,193.2f),205),(3,new(-.9f,193.2f),188),(4,new(.9f,193.2f),172),(5,new(2.6f,193.2f),155),(6,new(4.3f,193.2f),135)};
            internal static readonly Vector2[] HallCover={new(-8,184.5f),new(8,184.5f)};
            /// <summary>The Saint's door's trigger: 46 m across the hall, from its south wall (z 174) to 4 m behind P (z 200).</summary>
            internal static readonly Vector2 HallFight=new(0,187);internal const float HallFightDepth=26;
            /// <summary>The hall to its contract: the Saint's post facing north, its six arms (each a <c>SaintArm</c> at its collar, two metres up), two stacks of separation bolts for cover,
            /// the door and its seal, the press as scenery once it has settled, Orrin's voice from its speaker, and the lift home.</summary>
            static void PressHall(ZoneKit kit)
            {
                var seal=Seal(kit,"Press hall door seal",new Vector2(0,172),6.4f);
                // The trigger lies over the floor the fight is fought on, from the door's side of the hall to just behind the press: its middle is where the party stands to fight, not
                // inside the Saint's own body (on (0, 194), 2 m inside its capsule, the shared camera check found the press's own raised arms "in front of" the fight). Nobody comes
                // in by the hall's north half: the door is the only way in, and every hero is through it before the seal shuts (EncounterVolume.TryEnter).
                var duel=WorldBuilder.Encounter("FoundrySaint",kit.On(HallFight.x,HallFight.y,3),new Vector3(46,8,HallFightDepth),Foe("FoundrySaint","SlagColossus"),1,new[]{seal});
                var post=duel.spawners[0];post.transform.SetPositionAndRotation(On(kit,P),Quaternion.Euler(0,0,0));post.faceAsPlaced=true;post.radius=0;
                duel.requiredFlag="home.joint3";duel.checkpointSpawn="PressHall";
                var view=new GameObject("Saint preview (the landing)");view.transform.position=On(kit,HallPreview);duel.previewPoint=view.transform;duel.previewRadius=30;
                // The arms: an empty at each collar, two metres over the floor, its forward along its bearing. The component is the boss engineer's (S/Combat/SaintArm.cs): until it exists
                // the empties stand ready and the log says so.
                // The press's bed: the iron plate let into the barge's deck that the press was bolted down on a century ago, flush, darker than the hall's deck. Its arms hang from the
                // press standing on it, so they are the bed's (stood apart, nothing under them, they read as six arms hanging in the air whenever the press wasn't drawn).
                var bedShape=new ZoneKit.Shape();var bedAt=On(kit,P);
                bedShape.Slot(0).Box(bedAt+new Vector3(0,.016f,0),new Vector3(11.4f,.032f,6.4f),Quaternion.identity,ZoneKit.Shape.Dim(IronDark,.55f),ZoneKit.Shape.Dim(IronDark,.66f),.5f);
                foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})for(int k=0;k<4;k++)
                    bedShape.Slot(0).Disc(bedAt+new Vector3(sx*(5.2f-k*.5f),.035f,sz*2.9f),Vector3.up,.11f,8,IronPale);
                var bed=kit.Built("The press's bed",bedShape,IronMat);bed.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                var armType=Type.GetType("Lattice.Combat.SaintArm, Lattice.Combat");var arms=new GameObject("The Saint's arms");arms.transform.SetParent(bed.transform,true);
                foreach(var (index,at,bearing) in Arms)
                {
                    var collar=new GameObject("Saint arm "+index);collar.transform.SetParent(arms.transform,true);
                    collar.transform.SetPositionAndRotation(On(kit,at,2),Quaternion.Euler(0,bearing,0));
                    if(armType==null)continue;
                    var c=collar.AddComponent(armType);var so=new SerializedObject(c);
                    var i=so.FindProperty("index");if(i!=null)i.intValue=index;var s=so.FindProperty("settledBy");if(s!=null)s.stringValue="FoundrySaint";so.ApplyModifiedPropertiesWithoutUndo();
                }
                if(armType==null)Debug.LogWarning("WORKS_NO_SAINTARM: S/Combat/SaintArm.cs isn't in yet; the six collars stand as empties");
                // Cover: crated separation bolts, two stacks between the arms' feet, long side across the line to P.
                int n=0;
                foreach(var at in HallCover)
                {
                    var toP=(P-at).normalized;var across=Quaternion.LookRotation(new Vector3(toP.x,0,toP.y));var foot=On(kit,at);var s=new ZoneKit.Shape();var rng=new System.Random(71+n);
                    for(int layer=0;layer<3;layer++)for(int k=-1;k<=1;k++)
                    {
                        if(layer==2&&k!=0&&rng.NextDouble()<.5)continue;
                        var c=foot+across*new Vector3(k*.86f+((float)rng.NextDouble()-.5f)*.06f,.4f+layer*.8f,0);
                        s.Slot(0).Box(c,new Vector3(.82f,.78f,1.56f),across*Quaternion.Euler(0,((float)rng.NextDouble()-.5f)*4,0),new Color(.5f,.42f,.32f),new Color(.66f,.56f,.42f));
                        s.Slot(1).Box(c+Vector3.up*.2f,new Vector3(.86f,.06f,1.6f),across,IronDark,IronPale);
                    }
                    var stack=kit.Built("Separation bolts "+n,s,WoodMat,IronMat);ZoneKit.Block(stack,foot+Vector3.up*1.2f,new Vector3(2.6f,2.4f,1.6f),across.eulerAngles.y,"Bolt crates");
                    foreach(var t in stack.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;ZoneKit.Fadeable(stack);n++;
                }
                // The press as scenery once it has settled (the body goes with the encounter on a reload): the Saint's model at P, a solid box round its frame and legs, warm-lit.
                var settled=new GameObject("The Foundry Saint, settled");
                var model=ZoneKit.Prefab("Enemies","FoundrySaint");
                if(model!=null)
                {
                    var press=(GameObject)PrefabUtility.InstantiatePrefab(model);press.name="The press";press.transform.SetParent(settled.transform,false);
                    var box=ModelGeometry.BoundsOf(press);float fit=12/Mathf.Max(.01f,box.size.y);press.transform.localScale*=fit;box=ModelGeometry.BoundsOf(press);
                    press.transform.position+=On(kit,P)-new Vector3(box.center.x,box.min.y,box.center.z);press.transform.rotation=Quaternion.identity;
                    foreach(var mb in press.GetComponentsInChildren<MonoBehaviour>())UnityEngine.Object.DestroyImmediate(mb);foreach(var c in press.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
                }
                // Under the model (so it is never taken for an invisible wall), its size given back the model's own scale.
                var frame=ZoneKit.Block(settled,On(kit,P,6),new Vector3(10.1f,12,4.9f),0,"Settled press");var scale=frame.transform.lossyScale;
                frame.size=new Vector3(10.1f/Mathf.Max(.001f,Mathf.Abs(scale.x)),12/Mathf.Max(.001f,Mathf.Abs(scale.y)),4.9f/Mathf.Max(.001f,Mathf.Abs(scale.z)));
                // Cut out of the paths while it stands (D316): the bake has floor under it (it is off while the bake is made), so after a reload the partner's way, and the route's,
                // ran straight through the press. The fight's own press carves the same footprint while its body stands (BossController.FoundrySaint).
                var hole=frame.gameObject.AddComponent<NavMeshObstacle>();hole.shape=NavMeshObstacleShape.Box;hole.center=frame.center;hole.size=frame.size;hole.carving=true;hole.carveOnlyStationary=false;
                var warm=kit.Lamp(new Vector3(P.x,7,P.y-5),14,1.8f,new Color(1,.88f,.7f));warm.name="Settled press light";warm.transform.SetParent(settled.transform,true);
                Gate("The press, settled (from its fall)",HomeBossDown,true,settled);settled.SetActive(false);
                // Orrin's voice from the press's speaker.
                var talk=new GameObject("Orrin on the press's speaker",typeof(AfterTalk)).GetComponent<AfterTalk>();talk.transform.position=On(kit,new Vector2(0,193),3);
                talk.flag=HomeBossDown;talk.node="OrrinSaintSpeaks";talk.speaker="Orrin";talk.heardFlag="home.saintReleased";talk.delay=2.2f;
                // The lift home, at the north-east corner: barred until the Saint has settled.
                var cage=new ZoneKit.Shape();var liftFoot=On(kit,LiftAt);
                cage.Slot(0).Box(liftFoot+Vector3.up*.1f,new Vector3(3.6f,.2f,3.6f),Quaternion.identity,IronDark,IronPale);
                foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1})cage.Slot(0).Box(liftFoot+new Vector3(a*1.7f,3.6f,b*1.7f),new Vector3(.24f,7.2f,.24f),Quaternion.identity,IronDark,IronPale);
                cage.Slot(0).Box(liftFoot+Vector3.up*7.1f,new Vector3(3.8f,.3f,3.8f),Quaternion.identity,IronDark,IronPale);
                for(int i=0;i<=8;i++)foreach(int b in new[]{1})cage.Slot(0).Post(liftFoot+new Vector3(-1.6f+i*.4f,0,b*1.7f),3.2f,.04f,.04f,6,IronDark,IronPale);
                cage.Slot(1).Box(liftFoot+new Vector3(1.75f,1.4f,0),new Vector3(.12f,.6f,.5f),Quaternion.identity,new Color(.84f,.22f,.16f));
                var lift=kit.Built("The lift home",cage,IronMat,PlateMat);ZoneKit.Block(lift,liftFoot+new Vector3(0,3.6f,1.75f),new Vector3(3.8f,7.2f,.4f),0,"Lift back");lift.isStatic=true;ZoneKit.Fadeable(lift);
                // On a holder at the cage's open side: a prompt measures from its own object.
                var gate=new GameObject("The lift home (the way up)");gate.transform.position=liftFoot+new Vector3(-1.2f,0,-1.2f);lift.transform.SetParent(gate.transform,true);
                kit.Way(gate,"Ride the lift up — The Decks",DecksZone,DecksFromLift,HomeBossDown,"The lift answers the press hall's board: release the Foundry Saint first",3.6f,new Vector2(LiftAt.x-3.6f,LiftAt.y-2.4f));
            }
            const string HomeBossDown="bossdown.FoundrySaint";

            // ---------------------------------------------------------------- dressing
            /// <summary>The walked lines, room by room: the ground kit keeps them clear, the dressing keeps its tall things off them.</summary>
            internal static readonly Vector2[][] Ways=
            {
                new Vector2[]{new(0,-8),new(0,10),new(0,30),new(0,40),new(0,56),new(0,72),new(0,94),new(0,104),new(0,124),new(0,144),new(0,152),new(0,166),new(0,180)},
                new Vector2[]{new(0,20),new(9,18),new(14,20),new(22,20)},new Vector2[]{new(0,50),new(8,47),new(14,50),new(20,48)},new Vector2[]{new(0,84),new(12,82),new(18,82),new(24,84)},
                new Vector2[]{new(0,134),new(13,132),new(18,134),new(25,134)},new Vector2[]{new(0,96),new(4.4f,95)},
            };
            static bool NearWay(Vector2 p,float within){foreach(var way in Ways)if(ZoneKit.FromPath(p,way)<within)return true;return false;}
            static Vector2[] Prompts()
            {
                var list=new List<Vector2>{Arrival,HatchCradle,new(4.6f,-6),ThroatRig,ControlRig,GalleryRig,ModuleCache,BaggageCache,SkiffCache,TableHand,Checkpoint,HallCradle,LiftAt,DorrowAt};
                for(int n=1;n<=6;n++){list.Add(HandOf(n));list.Add(JackOf(n));}
                return list.ToArray();
            }
            static bool NearPrompt(Vector2 p,float within){foreach(var q in Prompts())if(Vector2.Distance(p,q)<within)return true;return false;}
            /// <summary>The fights' floors and the press bays: nothing stands in them.</summary>
            static readonly (Vector2 at,Vector2 size)[] Clear={(new(1.5f,57),new(24,9)),(new(0,65),new(17,12)),(new(20.2f,20),new(15,16.5f)),(new(24.6f,84),new(15,19)),(new(0,123),new(31,40)),(new(25,134),new(14,16)),(new(0,194),new(46,38)),(new(4.4f,94.6f),new(6,7))};
            static bool InClear(Vector2 p,float inset){foreach(var (at,size) in Clear)if(Mathf.Abs(p.x-at.x)<size.x*.5f-inset&&Mathf.Abs(p.y-at.y)<size.y*.5f-inset)return true;return false;}
            static bool NearCollar(Vector2 p,float within){foreach(var j in Joints)if(Vector2.Distance(p,j.collar)<within)return true;return false;}
            static bool ShadowsAFloor(Vector2 p,float tall)
            {
                float reach=ZoneKit.Shadowed(tall)+1.2f;var side=new Vector2(Away.y,-Away.x);
                for(float d=.75f;d<=reach;d+=.75f)foreach(float s in new[]{-1.6f,0,1.6f})if(Walkable(p+Away*d+side*s))return true;
                return false;
            }
            static readonly (string key,string fallback,float small,float large)[] Low={("DeckCrate","DressCargoPallet",.8f,1.2f),("DressCargoPallet","DressCargoPallet",.7f,1),("DressWaterBarrels","DressCargoPallet",1,1.3f),
                ("DressCableDrum","DressCargoPallet",.9f,1.2f),("HushwellCrates","DressCargoPallet",1,1.4f),("DressPowerCell","DressCargoPallet",.9f,1.3f)};
            static readonly (string key,string fallback,float small,float large)[] Tall={("DressShelfRack","DressShelfRack",2.2f,2.8f),("DressCoreRack","DressShelfRack",2.2f,2.8f),("DressFuelTank","DressWaterBarrels",2,2.6f),
                ("DressPipeRun","DressShelfRack",2.2f,3),("DressShelfRack","DressShelfRack",2.2f,2.8f),("DressCoreRack","DressShelfRack",2.2f,2.8f)};
            static void Dress(ZoneKit kit)
            {
                var rng=new System.Random(97);float R()=>(float)rng.NextDouble();
                Skins(kit);Ribs(kit);Portholes(kit);Names(kit);Pipes(kit);Lamps(kit);Haze(kit);KeelWork(kit);
                // ---- along every wall foot: a point every few metres a metre or two in from the wall, facing the room; dropped (after its draws) where it would stand on a way, a bay,
                // a prompt, a fight's middle or the camera's line to a floor.
                int placed=0;
                for(float x=-34;x<36;x+=2.6f)for(float z=-14;z<216;z+=2.6f)
                {
                    var p=new Vector2(x+R()*1.6f,z+R()*1.6f);float clear=FloorAt(p,out _);bool keep=R()<.5f;int pick=rng.Next(Tall.Length);float size=R(),yaw=R()*360;
                    if(!keep||clear>-.9f||clear<-2.4f)continue;
                    var outward=new Vector2(FloorAt(p+Vector2.right*.6f,out _)-FloorAt(p-Vector2.right*.6f,out _),FloorAt(p+Vector2.up*.6f,out _)-FloorAt(p-Vector2.up*.6f,out _));
                    if(outward.sqrMagnitude<.0001f)continue;outward.Normalize();
                    bool back=Vector2.Dot(outward,Away)>.35f;
                    var (key,fallback,small,large)=back?Tall[pick]:Low[pick%Low.Length];float tall=Mathf.Lerp(small,large,size);
                    if(NearWay(p,2.4f+tall*.3f)||NearPrompt(p,3)||InClear(p,-1)||NearCollar(p,5.2f)||(tall>2.4f&&ShadowsAFloor(p,tall))||Vector2.Distance(p,TableAt)<4.5f)continue;
                    var prop=kit.Prop(ZoneKit.Kit(key,fallback),new Vector3(p.x,0,p.y),tall,yaw);prop.name="Works clutter "+placed++;
                }
                Debug.Log("WORKS_CLUTTER "+placed);
                TugGear(kit);HaulerGear(kit);Luggage(kit,rng);ModuleGear(kit);HallGear(kit);FloorWork(kit);
            }
            /// <summary>The keel's cut faces, as a player reads them: the inside of a hull being taken apart (the lead, November 14: drawn low on the camera's side, the keel between
            /// rooms read as heavy dark slabs). Laid on the keel as it is drawn (<see cref="Seen"/>) and never more than a hand above it, in the band a play frame shows round every floor
            /// (from past the kerb to nine metres out): its frames every metre and a half and a longitudinal every three, standing out of it in the plant's iron; plating left on between
            /// them in the hull's own paint, and bays where it has come off and the dark between the frames shows, with the next frames' edges inside; cable runs along the camera's side
            /// of each floor, lashed at the frames; torch cuts across the plating, a dark kerf in a scorched rim with bright metal either side; and where a cut is being made, its last
            /// metre white-hot with sparks off it, the torch's bottles laid by it on their sides and its hose run to the cut. Drawn only: no box, no light, nothing on a floor.</summary>
            static void KeelWork(ZoneKit kit)
            {
                const float Step=.5f,X0=-35,X1=37,Z0=-15,Z1=219,Frame=1.5f,Longitudinal=3,Near=.9f,Far=9;
                int nx=Mathf.RoundToInt((X1-X0)/Step)+1,nz=Mathf.RoundToInt((Z1-Z0)/Step)+1;var top=new float[nx,nz];var clear=new float[nx,nz];
                for(int i=0;i<nx;i++)for(int k=0;k<nz;k++){var p=new Vector2(X0+i*Step,Z0+k*Step);clear[i,k]=FloorAt(p,out _);top[i,k]=Seen(p);}
                float Top(Vector2 p){float fx=(p.x-X0)/Step,fz=(p.y-Z0)/Step;int i=Mathf.Clamp(Mathf.FloorToInt(fx),0,nx-2),k=Mathf.Clamp(Mathf.FloorToInt(fz),0,nz-2);fx-=i;fz-=k;
                    return Mathf.Lerp(Mathf.Lerp(top[i,k],top[i+1,k],fx),Mathf.Lerp(top[i,k+1],top[i+1,k+1],fx),fz);}
                float Off(Vector2 p){int i=Mathf.Clamp(Mathf.RoundToInt((p.x-X0)/Step),0,nx-1),k=Mathf.Clamp(Mathf.RoundToInt((p.y-Z0)/Step),0,nz-1);return clear[i,k];}
                // Where the keel is dressed: off the floor past its kerb, inside the band a frame shows, on its tops, its slopes and the faces of its ridges (the north and east walls' sheer faces are Skins' plate).
                bool Dressed(Vector2 p){float c=Off(p);if(c<Near||c>Far)return false;
                    float sx=Mathf.Abs(Top(p+Vector2.right*.5f)-Top(p-Vector2.right*.5f)),sz=Mathf.Abs(Top(p+Vector2.up*.5f)-Top(p-Vector2.up*.5f));return Mathf.Max(sx,sz)<3.5f;}
                // Laid off the keel along its own normal: on a ridge's face a lift straight up would sink into it.
                Vector3 Normal(Vector2 p){float dx=(Top(p+Vector2.right*.25f)-Top(p-Vector2.right*.25f))*2,dz=(Top(p+Vector2.up*.25f)-Top(p-Vector2.up*.25f))*2;return new Vector3(-dx,1,-dz).normalized;}
                Vector3 OnKeel(Vector2 p,float up)=>new Vector3(p.x,Top(p),p.y)+Normal(p)*up;
                var rng=new System.Random(1914);float R()=>(float)rng.NextDouble();
                var iron=new ZoneKit.Shape();var plate=new ZoneKit.Shape();var cable=new ZoneKit.Shape();var hot=new ZoneKit.Shape();
                Color frameTone=new(.66f,.7f,.76f),frameTop=new(.9f,.92f,.96f),inner=new(.08f,.08f,.1f),scorch=new(.12f,.1f,.09f),kerf=new(.04f,.04f,.05f),bright=new(.82f,.84f,.86f);
                // A raised strip along the surface between two plan points: a top and two sides, so a frame stands out of the keel and catches the light on its edge.
                void Rib(ZoneKit.Shape s,Vector2 a,Vector2 b,float wide,float high,Color side,Color crown)
                {
                    var along=(b-a).normalized;var across=new Vector2(-along.y,along.x)*wide*.5f;
                    Vector3 A(Vector2 q,float up)=>OnKeel(q,up);
                    // Lower by the kerb, where the keel is still close under the line to a hero's feet.
                    high*=.35f+.65f*ZoneKit.Smooth(.9f,1.7f,Mathf.Min(Off(a),Off(b)));
                    var a0=A(a-across,.02f);var a1=A(a+across,.02f);var b0=A(b-across,.02f);var b1=A(b+across,.02f);var n=Normal((a+b)*.5f);var lift=n*high;
                    s.Quad(a0+lift,b0+lift,b1+lift,a1+lift,n,crown,.5f);
                    var out0=new Vector3(-across.x,0,-across.y);s.Quad(a0,b0,b0+lift,a0+lift,out0,side,.5f);s.Quad(a1,b1,b1+lift,a1+lift,-out0,side,.5f);
                }
                // A flat piece laid on the surface (a plate, a scorch, a kerf), a few centimetres up, its corners on the keel as drawn.
                void Laid(ZoneKit.Shape s,Vector2 c,Vector2 along,float length,float wide,float up,Color tone,float texel=.5f)
                {
                    var u=along.normalized*length*.5f;var v=new Vector2(-along.normalized.y,along.normalized.x)*wide*.5f;
                    s.Quad(OnKeel(c-u-v,up),OnKeel(c+u-v,up),OnKeel(c+u+v,up),OnKeel(c-u+v,up),Normal(c),tone,texel);
                }
                int frames=0,plates=0,bays=0,cuts=0;
                // ---- the frames (across, every metre and a half north) and the longitudinals (every three metres east), each in pieces a metre long where the keel is dressed
                for(float z=Z0+.75f;z<Z1;z+=Frame)for(float x=X0;x<X1-1;x+=1){var a=new Vector2(x,z);var b=new Vector2(x+1,z);if(Dressed(a)&&Dressed(b)){Rib(iron,a,b,.24f,.14f,frameTone,frameTop);frames++;}}
                for(float x=X0+1.5f;x<X1;x+=Longitudinal)for(float z=Z0;z<Z1-1;z+=1){var a=new Vector2(x,z);var b=new Vector2(x,z+1);if(Dressed(a)&&Dressed(b))Rib(iron,a,b,.14f,.08f,frameTone,frameTop);}
                // ---- each bay between them: plated in its hull's paint, or stripped to the dark with the frames of the hull beneath showing, or plated and cut across
                for(float z=Z0+.75f;z<Z1-Frame;z+=Frame)for(float x=X0+1.5f;x<X1-Longitudinal;x+=Longitudinal)
                {
                    var c=new Vector2(x+Longitudinal*.5f,z+Frame*.5f);float roll=R(),turn=R(),len=R();
                    bool whole=Dressed(c)&&Dressed(c+new Vector2(-1.3f,-.6f))&&Dressed(c+new Vector2(1.3f,.6f))&&Dressed(c+new Vector2(1.3f,-.6f))&&Dressed(c+new Vector2(-1.3f,.6f));if(!whole)continue;
                    if(roll<.3f)
                    {
                        // Stripped: the plate gone, the next hull's frames inside the bay, dark.
                        Laid(plate,c,Vector2.right,Longitudinal-.3f,Frame-.3f,.025f,inner);for(int k=1;k<3;k++)Rib(iron,c+new Vector2(-1.3f+k*.87f,-.6f),c+new Vector2(-1.3f+k*.87f,.6f),.12f,.05f,new Color(.3f,.32f,.36f),new Color(.44f,.46f,.5f));bays++;continue;
                    }
                    var paint=PlateOf(HullAt(c));var tone=ZoneKit.Shape.Dim(paint,.88f+.16f*R());tone.a=1;
                    Laid(plate,c,Vector2.right,Longitudinal-.28f,Frame-.28f,.035f,tone,1/2.4f);plates++;
                    if(roll>.8f)
                    {
                        // A torch cut across it: a scorched rim, the dark kerf, bright metal either side of it.
                        var dir=new Vector2(Mathf.Cos(turn*Mathf.PI),Mathf.Sin(turn*Mathf.PI));float l=.9f+.8f*len;
                        Laid(plate,c,dir,l+.2f,.34f,.045f,scorch);Laid(plate,c,dir,l,.06f,.055f,kerf);var side=new Vector2(-dir.y,dir.x)*.06f;
                        Laid(plate,c+side,dir,l,.025f,.056f,bright);Laid(plate,c-side,dir,l,.025f,.056f,bright);cuts++;
                    }
                }
                // ---- cable runs: three lines along the camera's side of every room, two metres out on the keel, lashed at each frame
                Color[] lines={new(.08f,.08f,.09f),new(.2f,.24f,.15f),new(.3f,.36f,.44f)};int runs=0;
                foreach(var (room,x0,x1,z0,z1,round) in Boxes)
                {
                    if(x1-x0<10&&z1-z0<10)continue;
                    foreach(var (a,b,outward) in new[]{(new Vector2(x0+1,z0),new Vector2(x1-1,z0),Vector2.down),(new Vector2(x0,z0+1),new Vector2(x0,z1-1),Vector2.left)})
                    {
                        var path=new List<Vector3>();var radii=new List<float>();int line=0;
                        void Flush(){if(path.Count>2){foreach(int k in new[]{0,1,2}){var off=new Vector3(outward.x,0,outward.y)*k*.18f;cable.Tube(path.Select(q=>q+off+Vector3.up*(.07f+.02f*k)).ToList(),radii,6,lines[(line+k)%3],ZoneKit.Shape.Dim(lines[(line+k)%3],1.2f),1,.5f,false);}runs++;}path.Clear();radii.Clear();line++;}
                        float length=Vector2.Distance(a,b);var dir=(b-a)/length;
                        for(float d=0;d<=length;d+=.75f){var q=a+dir*d+outward*2.1f;if(!Dressed(q)||!Dressed(q+outward*.4f)){Flush();continue;}path.Add(OnKeel(q,0));radii.Add(.065f);
                            if(Mathf.Repeat(d,3)<.75f)iron.Box(OnKeel(q+outward*.18f,.12f),new Vector3(.12f,.12f,.6f),Quaternion.LookRotation(new Vector3(outward.x,0,outward.y)),new Color(.42f,.44f,.48f));}
                        Flush();
                    }
                }
                // ---- where a cut is being made: on the camera's side of a room, three metres out, its last metre glowing, sparks, the bottles and the hose
                int sites=0;var sparkRoots=new List<Transform>();var sparkLook=kit.Glow("Cutting sparks",false,new Color(2.2f,1.9f,1.4f,1));var glareLook=kit.Glow("Cutting glare",false,new Color(1.6f,1.4f,1.05f,1));
                foreach(var c in WorkSites)
                {
                    // The cut runs along the keel, its bottles laid further from the floor: which way is out is which way the floor falls away.
                    var outward=new Vector2(Off(c+Vector2.right*.5f)-Off(c-Vector2.right*.5f),Off(c+Vector2.up*.5f)-Off(c-Vector2.up*.5f));outward=outward.sqrMagnitude>1e-4f?outward.normalized:Vector2.right;var dir=new Vector2(-outward.y,outward.x);
                    if(!Dressed(c)||!Dressed(c-dir*2f)||!Dressed(c+dir*1.2f)||!Dressed(c+outward*1.6f)){Debug.Log("WORKS_KEEL_SITE_UNDRESSED "+c);continue;}
                    // Nothing of it within four metres of a floor along the camera's line: its glare and its bottles never stand before anyone.
                    if(Enumerable.Range(1,8).Any(k=>FloorAt(c+Away*(k*.5f),out _)<=0)){Debug.Log("WORKS_KEEL_SITE_BEFORE_A_FLOOR "+c);continue;}
                    Laid(plate,c-dir*.7f,dir,2.6f,.5f,.06f,scorch);Laid(plate,c-dir*.9f,dir,2.2f,.07f,.07f,kerf);
                    // The hot end: a white-gold core, a softer rim of heat round it.
                    var tip=c+dir*.6f;Laid(hot,tip,dir,1f,.22f,.075f,new Color(1,.86f,.62f));Laid(hot,tip,dir,.9f,.07f,.08f,Color.white);
                    // The bottles, laid on their sides on the keel, and the hose to the cut.
                    var lie=c+outward*1.4f-dir*.4f;var axis=new Vector3(dir.x,0,dir.y);
                    foreach(var (off,tone) in new[]{(-.25f,new Color(.24f,.3f,.24f)),(.25f,new Color(.62f,.58f,.5f))})
                    {
                        var mid=OnKeel(lie+outward*off,.2f);iron.Tube(new[]{mid-axis*.55f,mid+axis*.55f},new[]{.19f,.19f},8,tone,ZoneKit.Shape.Dim(tone,1.2f),1,.5f,true);
                    }
                    cable.Rope(OnKeel(lie,.3f),OnKeel(tip-dir*.4f,.12f),.05f,.035f,new Color(.16f,.16f,.18f),5,6);
                    var sparks=new GameObject("Sparks off a cut in the keel",typeof(ParticleSystem));sparks.transform.position=OnKeel(tip+dir*.45f,.15f);
                    var ps=sparks.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.loop=true;main.prewarm=true;main.duration=2;
                    main.startLifetime=new ParticleSystem.MinMaxCurve(.25f,.7f);main.startSpeed=new ParticleSystem.MinMaxCurve(1.2f,3f);main.startSize=new ParticleSystem.MinMaxCurve(.03f,.07f);
                    main.startColor=new Color(1,.86f,.6f,1);main.gravityModifier=1.4f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=80;main.playOnAwake=true;
                    var em=ps.emission;em.rateOverTime=55;var sh=ps.shape;sh.shapeType=ParticleSystemShapeType.Cone;sh.angle=40;sh.radius=.05f;sparks.transform.rotation=Quaternion.Euler(-70,0,0);
                    var pr=sparks.GetComponent<ParticleSystemRenderer>();pr.sharedMaterial=sparkLook;pr.shadowCastingMode=ShadowCastingMode.Off;pr.receiveShadows=false;
                    // The cut's glare, turned to the fixed camera, so where work is going on reads across a room.
                    var glare=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(glare.GetComponent<Collider>());glare.name="Glare of a cut in the keel";glare.transform.SetParent(sparks.transform,true);
                    glare.transform.SetPositionAndRotation(OnKeel(tip,.3f),Quaternion.Euler(40,SightlineAudit.Yaw,0));glare.transform.localScale=Vector3.one*1.7f;
                    var gr=glare.GetComponent<Renderer>();gr.sharedMaterial=glareLook;gr.shadowCastingMode=ShadowCastingMode.Off;gr.receiveShadows=false;sparkRoots.Add(sparks.transform);
                    sites++;
                }
                var keelFrames=kit.Built("Keel frames",iron,IronMat);keelFrames.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                // Each cut's sparks and glare are part of the keel they are cut in (alone, a glare a hand off the keel read to the suite as a prop hanging over nothing).
                foreach(var sr in sparkRoots)sr.SetParent(keelFrames.transform,true);
                kit.Built("Keel plating",plate,PlateMat).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                kit.Built("Keel cable runs",cable,IronMat).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                if(!hot.Empty)kit.Built("Keel cuts glowing",hot,CutHot).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                Debug.Log($"WORKS_KEEL frames={frames} plates={plates} stripped={bays} cuts={cuts} cables={runs} sites={sites}");
            }
            /// <summary>Where a cut is being made in the keel: the room whose camera's side it is on, how far along that side, and which side (south or west).</summary>
            /// <summary>Where a cut is being made in the keel: on the keel beside or behind where the walk goes, never on the camera's side of a floor, so a frame from the walk has one in it.</summary>
            static readonly Vector2[] WorkSites={new(6.5f,156),new(13.2f,64),new(8.4f,33.4f),new(-9,149),new(-12,216.5f),new(14.6f,40.5f),new(-7,149.5f),new(13,149),new(-11,99.4f)};
            /// <summary>The hull plate stood on each wall, by its wall's name ("Tug east", "Bow east"): what a wall's paint and pipes are fixed to, so each is part of the wall it is on.</summary>
            static readonly Dictionary<string,GameObject> Plates=new();
            /// <summary>Hangs a fixture on the hull plate it is fixed to: the named wall's, or the plate nearest it on the plan within a metre and a half. A pipe a third of a metre off a
            /// plate with nothing solid behind it read as hanging in the air (every fixture is measured as a prop: QaPlanWorldTests.NothingFloatsOrSinksIn).</summary>
            static void OnPlate(GameObject fixture,string wall=null)
            {
                GameObject plate=null;
                if(wall!=null)Plates.TryGetValue(wall,out plate);
                else
                {
                    var at=ZoneKit.Drawn(fixture).center;float best=1.5f;
                    foreach(var p in Plates.Values){var b=ZoneKit.Drawn(p);var flat=new Vector3(Mathf.Clamp(at.x,b.min.x,b.max.x),at.y,Mathf.Clamp(at.z,b.min.z,b.max.z));float d=Vector2.Distance(new Vector2(at.x,at.z),new Vector2(flat.x,flat.z));if(d<best){best=d;plate=p;}}
                }
                if(plate!=null)fixture.transform.SetParent(plate.transform,true);else Debug.LogWarning("WORKS_FIXTURE_LOOSE "+fixture.name);
            }
            /// <summary>Each hull's own plate along its walls, in its own paint: panels with a frame at each, a kick strip, a stringer, standing in front of the keel's plating on every wall
            /// the camera sees the inside of (north and east; west and south are the camera's side and kept low by the sightline rule). Each wall its own mesh, so no box spans a room.</summary>
            static void Skins(ZoneKit kit)
            {
                var rng=new System.Random(23);int walls=0;
                void Wall(string name,Vector2 a,Vector2 b,Vector2 outward,Hull hull,float level)
                {
                    if(Vector2.Dot(outward,Away)<.05f)return;
                    var s=new ZoneKit.Shape();var paint=PlateOf(hull);float length=Vector2.Distance(a,b);int steps=Mathf.Max(1,Mathf.RoundToInt(length/2.4f));bool any=false;var inward=new Vector3(-outward.x,0,-outward.y);
                    for(int i=0;i<steps;i++)
                    {
                        var p0=Vector2.Lerp(a,b,i/(float)steps);var p1=Vector2.Lerp(a,b,(i+1)/(float)steps);var m=(p0+p1)*.5f;
                        if(Walkable(m+outward*1.1f))continue;
                        // No higher than the keel is drawn behind it: a north wall here is often the next room's foreground (D272).
                        float top=Mathf.Min(Seen(m+outward*1.5f),Seen(p0+outward*1.5f),Seen(p1+outward*1.5f))-level-.1f;if(top<.6f)continue;top=Mathf.Min(top,7.2f);any=true;
                        var f0=new Vector3(p0.x,level-.05f,p0.y)+inward*.02f;var f1=new Vector3(p1.x,level-.05f,p1.y)+inward*.02f;float shade=.86f+.18f*(float)rng.NextDouble();
                        float split=Mathf.Min(top,1.15f+.4f*(float)rng.NextDouble());
                        s.Slot(0).Quad(f0,f1,f1+Vector3.up*split,f0+Vector3.up*split,inward,ZoneKit.Shape.Dim(paint,shade*.84f),1/2.4f);
                        if(top>split)s.Slot(0).Quad(f0+Vector3.up*split,f1+Vector3.up*split,f1+Vector3.up*top,f0+Vector3.up*top,inward,ZoneKit.Shape.Dim(paint,shade),1/2.4f);
                        // A frame at each panel's start, a kick strip, and a stringer under the top.
                        s.Slot(1).Box(f0+Vector3.up*top*.5f+inward*.1f,new Vector3(.24f,top,.2f),Quaternion.LookRotation(inward),ZoneKit.Shape.Dim(paint,.6f),ZoneKit.Shape.Dim(paint,.75f));
                        s.Slot(1).Beam(f0+Vector3.up*split+inward*.06f,f1+Vector3.up*split+inward*.06f,.12f,.1f,ZoneKit.Shape.Dim(paint,.55f));
                        if(top>3.2f)s.Slot(1).Beam(f0+Vector3.up*(top-.35f)+inward*.08f,f1+Vector3.up*(top-.35f)+inward*.08f,.14f,.16f,ZoneKit.Shape.Dim(paint,.62f));
                    }
                    if(!any)return;
                    var drawn=kit.Built("Hull plate "+name,s,PlateMat,IronMat);drawn.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;walls++;Plates[name]=drawn;
                }
                Plates.Clear();
                foreach(var (room,x0,x1,z0,z1,round) in Boxes)
                {
                    if(room==Room.J4Pass||room==Room.J5Pass||room==Room.J6Pass||room==Room.Collar1||room==Room.Collar2||room==Room.Collar3||room==Room.Stair||room==Room.HallDoor||room==Room.ControlStair)continue;
                    var hull=HullOf(room);float level=Level(new Vector2((x0+x1)*.5f,(z0+z1)*.5f),room);
                    Wall(room+" north",new Vector2(x0+round,z1),new Vector2(x1-round,z1),Vector2.up,hull,level);
                    Wall(room+" east",new Vector2(x1,z0+round),new Vector2(x1,z1-round),Vector2.right,hull,level);
                    Wall(room+" west",new Vector2(x0,z0+round),new Vector2(x0,z1-round),Vector2.left,hull,level);
                }
                // The bow's two sides: the tug's prow from inside.
                var tipE=new Vector2(BowTip,BowTo);var footE=new Vector2(BowWide,BowFrom);var nE=new Vector2(tipE.y-footE.y,-(tipE.x-footE.x)).normalized;if(nE.x<0)nE=-nE;
                Wall("Bow east",footE,tipE,nE,Hull.Tug,TugFloor);
                // Joint 2's and joint 3's walls are where two hulls meet: east of each collar, the next hull's plate.
                Wall("Liner stern (joint 2)",new Vector2(4.6f,97.2f),new Vector2(12.4f,97.2f),Vector2.up,Hull.Liner,HaulerFloor);
                Wall("Barge stern (joint 3)",new Vector2(4.6f,146.2f),new Vector2(15.4f,146.2f),Vector2.up,Hull.Barge,LinerFloor);
                Debug.Log("WORKS_HULL_WALLS "+walls);
            }
            /// <summary>The hauler's frames: great iron ribs rising out of the floor along its east wall and bending in over it, and their stumps along the low west wall.</summary>
            static void Ribs(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var tone=PlateOf(Hull.Hauler);
                // Each frame no taller than the wall it stands against is drawn (D272): the east wall is the baggage bay's and the control stair's foreground, and a frame 7.4 m
                // tall stood across the line to anyone there; where the wall is too low for a frame, its stump only.
                foreach(float z in new[]{43.5f,57.5f,73f,77.5f,87f,91.5f,95.6f})
                {
                    float x=10.85f,tall=Mathf.Min(7.4f,Seen(new Vector2(12.6f,z))-HaulerFloor-.2f);var path=new List<Vector3>();var radii=new List<float>();
                    if(tall<1.4f){s.Slot(0).Post(new Vector3(x,HaulerFloor,z),1.1f,.34f,.3f,8,ZoneKit.Shape.Dim(tone,.6f),ZoneKit.Shape.Dim(tone,.75f));continue;}
                    for(int k=0;k<=8;k++){float t=k/8f;path.Add(new Vector3(x-Mathf.Sin(t*Mathf.PI*.5f)*1.6f*t*tall/7.4f,HaulerFloor+t*tall,z));radii.Add(Mathf.Lerp(.34f,.24f,t));}
                    s.Slot(0).Tube(path,radii,8,ZoneKit.Shape.Dim(tone,.62f),ZoneKit.Shape.Dim(tone,.8f),1,.4f);
                    if(Seen(new Vector2(-12.4f,z))-HaulerFloor>.9f)s.Slot(0).Post(new Vector3(-10.85f,HaulerFloor,z),Mathf.Min(1.1f,Seen(new Vector2(-12.4f,z))-HaulerFloor-.1f),.3f,.28f,8,ZoneKit.Shape.Dim(tone,.6f),ZoneKit.Shape.Dim(tone,.75f));
                }
                // Across joint 2's wall, west of the collar: the last frames, bolted to the liner's plate.
                // (No higher than joint 2's wall is drawn: that wall is the liner's foreground, and at 7.2 m the two stood like posts before the lens.)
                foreach(float x in new[]{-10.2f,-5f}){float tall=Mathf.Min(7.2f,Seen(new Vector2(x,98.6f))-HaulerFloor-.2f);if(tall<1.2f)continue;
                    var path=new[]{new Vector3(x,HaulerFloor,97.05f),new Vector3(x,HaulerFloor+tall*.5f,96.9f),new Vector3(x+.2f,HaulerFloor+tall,96.6f)};s.Slot(0).Tube(path,new[]{.36f,.32f,.28f},8,ZoneKit.Shape.Dim(tone,.62f),ZoneKit.Shape.Dim(tone,.8f),1,.4f);}
                kit.Built("Hauler ribs",s,IronMat);
            }
            /// <summary>The liner's portholes along its east wall and its baggage bay's north wall: round dark glass in a brass ring, a passenger ship's.</summary>
            static void Portholes(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var g=new ZoneKit.Shape();
                void Hole(Vector3 at,Vector3 facing){s.Slot(0).Tube(Ring(at,facing,.42f),Enumerable.Repeat(.08f,25).ToList(),6,new Color(.86f,.7f,.42f),new Color(.96f,.8f,.5f),1,.5f,false);g.Slot(0).Disc(at+facing*.02f,facing,.4f,16,Color.white);}
                foreach(float z in new[]{106f,111f,116f,126f,131f,141f})Hole(new Vector3(15.95f,LinerFloor+3.1f,z),Vector3.left);
                foreach(float x in new[]{16f,20f,24f})Hole(new Vector3(x,HaulerFloor+3f,57.95f),Vector3.back);
                foreach(float x in new[]{-12f,-6f})Hole(new Vector3(x,LinerFloor+3.2f,146.15f),Vector3.back);
                kit.Built("Liner portholes",s,BrassMat);kit.Built("Liner porthole glass",g,Glass);
            }
            static List<Vector3> Ring(Vector3 at,Vector3 facing,float r)
            {
                var u=Vector3.Cross(facing,Vector3.up).normalized;var list=new List<Vector3>();
                for(int k=0;k<=24;k++){float a=k*Mathf.PI*2/24;list.Add(at+u*Mathf.Cos(a)*r+Vector3.up*Mathf.Sin(a)*r);}
                return list;
            }
            /// <summary>Words on the iron: the tug's name along its prow, the courier skiff's lock number on its nose, the Compact's mark on its module, Taren's repair on joint 3.</summary>
            static void Names(ZoneKit kit)
            {
                var tipE=new Vector3(BowTip,0,BowTo);var footE=new Vector3(BowWide,0,BowFrom);var along=(tipE-footE).normalized;var inward=Vector3.Cross(Vector3.up,along);if(inward.x>0)inward=-inward;
                var mid=Vector3.Lerp(footE,tipE,.46f);mid.y=TugFloor+3.9f;
                // Each painted on its own hull's plate, and part of it.
                OnPlate(Painted(kit,"Tug name","STAY A WHILE",mid+inward*.1f,along,inward,9.2f,new Color(.94f,.92f,.86f)),"Bow east");
                OnPlate(Painted(kit,"Lock nine","LOCK 9",new Vector3(31.8f,LinerFloor+2.8f,134),Vector3.back,Vector3.left,4.2f,new Color(.24f,.26f,.3f)),"Skiff east");
                OnPlate(Painted(kit,"Compact mark","COMPACT 7",new Vector3(20.2f,TugFloor+3.6f,28.35f),Vector3.right,Vector3.back,6.2f,new Color(.2f,.62f,.68f)),"Module north");
                // Taren's repair: a bright patch plate across a crack in joint 3's collar, his weld bead round it, eleven scratches under it, one a year.
                var j=Joints[2];var c=new Vector3(j.collar.x,LinerFloor+2,j.collar.y);var s=new ZoneKit.Shape();var at=c+new Vector3(-2.9f,2.2f,-.45f);
                s.Slot(0).Box(at,new Vector3(1.2f,.8f,.06f),Quaternion.Euler(0,0,-38),new Color(.86f,.88f,.9f));
                s.Slot(1).Tube(new[]{at+Quaternion.Euler(0,0,-38)*new Vector3(-.64f,-.44f,-.05f),at+Quaternion.Euler(0,0,-38)*new Vector3(.64f,-.44f,-.05f),at+Quaternion.Euler(0,0,-38)*new Vector3(.64f,.44f,-.05f),at+Quaternion.Euler(0,0,-38)*new Vector3(-.64f,.44f,-.05f),at+Quaternion.Euler(0,0,-38)*new Vector3(-.64f,-.44f,-.05f)},
                    Enumerable.Repeat(.045f,5).ToList(),5,new Color(.5f,.48f,.46f),new Color(.6f,.58f,.56f),1,.5f,false);
                for(int k=0;k<11;k++){var p=c+new Vector3(-3.7f+k*.12f,.95f,-.48f);s.Slot(0).Box(p,new Vector3(.025f,.26f,.02f),Quaternion.Euler(0,0,8),new Color(.92f,.92f,.9f));}
                var patch=kit.Built("Taren's repair on joint 3",s,PlateMat,IronMat);patch.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                // On the collar's flange, and part of it: it turns with the collar as the joint gives.
                var collar3=GameObject.Find("Joint 3 collar");if(collar3!=null)patch.transform.SetParent(collar3.transform,true);
            }
            /// <summary>The keel's services along the north and east walls: pipe runs on brackets at three metres and a half, a fat main lower, and a cable tray.</summary>
            static void Pipes(ZoneKit kit)
            {
                int runs=0;
                void Run(Vector2 a,Vector2 b,float level,float up,float r,Color tone,Vector2 off)
                {
                    // On the wall as it is drawn: where the next room's foreground keeps it low (D272) the run comes down under its top, and where that is too low for a pipe it isn't laid.
                    var back=-off.normalized;float wall=float.PositiveInfinity;for(float t=0;t<=1;t+=.1f)wall=Mathf.Min(wall,Seen(Vector2.Lerp(a,b,t)+back*1.6f)-level);
                    up=Mathf.Min(up,wall-.45f);if(up<1.9f){Debug.Log($"WORKS_PIPE_UNLAID {a}-{b} wall {wall:0.0}");return;}
                    var s=new ZoneKit.Shape();var p0=new Vector3(a.x+off.x,level+up,a.y+off.y);var p1=new Vector3(b.x+off.x,level+up,b.y+off.y);
                    s.Slot(0).Tube(new[]{p0,p1},new[]{r,r},10,tone,ZoneKit.Shape.Dim(tone,.9f),1,.3f);
                    float length=Vector3.Distance(p0,p1);for(float t=1.2f;t<length;t+=2.4f){var q=Vector3.Lerp(p0,p1,t/length);s.Slot(0).Tube(new[]{q-(p1-p0).normalized*.08f,q+(p1-p0).normalized*.08f},new[]{r+.05f,r+.05f},10,IronPale,IronPale,1,.3f,false);
                        s.Slot(0).Box(q+new Vector3(off.x,0,off.y).normalized*-(r+.12f),new Vector3(.12f,.12f,.12f)+new Vector3(Mathf.Abs(off.x),0,Mathf.Abs(off.y))*0+Vector3.one*.06f,Quaternion.identity,IronDark);}
                    var pipe=kit.Built("Pipe run "+runs++,s,IronMat);pipe.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;OnPlate(pipe);
                }
                var steam=new Color(.62f,.64f,.66f);var water=new Color(.42f,.56f,.6f);var power=new Color(.24f,.24f,.26f);
                // (The tug's steam main ran along its west wall, on the camera's side, at three and a half metres: across the line to anyone at that wall, and off a wall drawn low.)
                Run(new(10.6f,4),new(10.6f,16.6f),TugFloor,3.4f,.22f,water,new(-.25f,0));Run(new(10.6f,23.4f),new(10.6f,27),TugFloor,3.4f,.22f,water,new(-.25f,0));
                Run(new(-10.6f,96.8f),new(-4.4f,96.8f),HaulerFloor,4.6f,.26f,water,new(0,-.3f));Run(new(5,96.8f),new(10.4f,96.8f),HaulerFloor,3.4f,.18f,steam,new(0,-.25f));
                Run(new(10.6f,41.6f),new(10.6f,45.6f),HaulerFloor,2.8f,.14f,power,new(-.2f,0));Run(new(10.6f,71),new(10.6f,78.6f),HaulerFloor,2.8f,.14f,power,new(-.2f,0));
                Run(new(15.6f,103),new(15.6f,129.4f),LinerFloor,4.4f,.2f,water,new(-.25f,0));Run(new(-15.4f,145.8f),new(-4,145.8f),LinerFloor,4.6f,.24f,steam,new(0,-.3f));
                Run(new(-23,213.4f),new(23,213.4f),BargeFloor,4.2f,.3f,water,new(0,-.35f));Run(new(23.4f,176),new(23.4f,205),BargeFloor,3.6f,.22f,steam,new(-.3f,0));
                Run(new(13.4f,57.6f),new(27.4f,57.6f),HaulerFloor,4.2f,.16f,steam,new(0,-.2f));Run(new(31.6f,127),new(31.6f,141),LinerFloor,3.2f,.16f,power,new(-.2f,0));
            }
            /// <summary>The plant's work lamps: a bracket on a north or east wall with warm glass, a light under each only where a room needs one.</summary>
            static void Lamps(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();var glass=new ZoneKit.Shape();
                // Three hung in the air and are gone or moved: one over the hatch where no wall is, one before the bow's west wall two metres short of it, one on the throat's west
                // jamb (the camera's side, drawn low); the landing's stood on its north wall, the press hall's foreground, and is on the landing's east wall now.
                foreach(var (x,z,floor,facing,lit) in new[]{(10.6f,9f,TugFloor,Vector3.left,true),(10.6f,43f,HaulerFloor,Vector3.left,true),
                    (10.6f,90f,HaulerFloor,Vector3.left,true),(27.6f,50f,HaulerFloor,Vector3.left,false),(15.6f,108f,LinerFloor,Vector3.left,true),
                    (15.6f,140f,LinerFloor,Vector3.left,false),(-8f,145.8f,LinerFloor,Vector3.back,true),(31.6f,132f,LinerFloor,Vector3.left,true),(27.6f,27.9f,TugFloor,Vector3.back,true),
                    (9.6f,166f,BargeFloor,Vector3.left,true),(23.6f,186f,BargeFloor,Vector3.left,true),(-14f,213.6f,BargeFloor,Vector3.back,true),(14f,213.6f,BargeFloor,Vector3.back,false)})
                {
                    // On the wall as it is drawn: no higher than its top behind the bracket, and none where that is lower than a lamp is hung.
                    // Read where the wall stands, a metre and more past the floor's edge: at its foot the keel is still rising (it climbs over its first .9 m).
                    float top=Seen(new Vector2(x,z)-new Vector2(facing.x,facing.z)*1.7f)-floor,up=Mathf.Min(4.2f,top-.4f);
                    if(up<2.4f){Debug.Log($"WORKS_LAMP_UNMOUNTED ({x}, {z}) wall {top:0.0}");continue;}
                    var wall=new Vector3(x,floor,z);var arm=wall+Vector3.up*up;
                    shape.Slot(0).Box(arm,new Vector3(.3f,.4f,.3f),Quaternion.identity,IronDark,IronPale).Beam(arm,arm+facing*.8f,.08f,.08f,IronDark,IronPale).Box(arm+facing*.9f+Vector3.down*.12f,new Vector3(.44f,.22f,.44f),Quaternion.identity,IronDark,IronPale);
                    glass.Slot(0).Box(arm+facing*.9f+Vector3.down*.27f,new Vector3(.34f,.08f,.34f),Quaternion.identity,Color.white);
                    if(lit){var l=kit.Lamp(new Vector3(x,up-.2f,z)+facing*1.4f,11,1.7f,new Color(1,.9f,.76f));l.name="Lamp light";}
                }
                kit.Built("Work lamp brackets",shape,IronMat);kit.Built("Work lamp glass",glass,LampWarm);
            }
            /// <summary>Dust in the light under the promenade's grating: the station is working overhead.</summary>
            static void Haze(ZoneKit kit)
            {
                var dust=kit.Glow("Dust",false,new Color(.9f,.92f,.96f,1));
                kit.Drift("Dust in the grate light",new Vector3(0,2.6f,100),new Vector3(70,4,236),dust,new Color(.84f,.86f,.9f,.45f),900,.03f,.06f,.12f,16);
                var haze=kit.Glow("Haze",true,new Color(.6f,.64f,.7f,1));
                kit.Drift("Haze under the deck",new Vector3(0,6.4f,100),new Vector3(70,3,236),haze,new Color(.46f,.5f,.56f,.06f),220,4,8,.1f,26);
            }
            /// <summary>The tug's own things: its tow winch on the west side (a drum on a bed, a hawser wound on it), towing bitts, and its engine housing in the north-east corner.</summary>
            static void TugGear(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var red=PlateOf(Hull.Tug);
                var bed=new Vector3(-8.2f,TugFloor,14);s.Slot(0).Box(bed+Vector3.up*.2f,new Vector3(2.4f,.4f,3.6f),Quaternion.identity,IronDark,IronPale);
                s.Slot(0).Tube(new[]{bed+new Vector3(0,1.3f,-1.5f),bed+new Vector3(0,1.3f,1.5f)},new[]{.85f,.85f},16,ZoneKit.Shape.Dim(red,.7f),red,1,.4f);
                for(float z=-1.2f;z<=1.2f;z+=.18f)s.Slot(1).Tube(new[]{bed+new Vector3(-.02f,1.3f,z-.08f),bed+new Vector3(-.02f,1.3f,z+.08f)},new[]{.98f,.98f},16,new Color(.5f,.44f,.36f),new Color(.6f,.52f,.42f),1,.4f,false);
                foreach(float z in new[]{-1.8f,1.8f})s.Slot(0).Box(bed+new Vector3(0,1,z),new Vector3(.4f,2,.3f),Quaternion.identity,IronDark,IronPale);
                foreach(var (x,z) in new[]{(-7.4f,6.4f),(-7.4f,22f)})foreach(float d in new[]{-.5f,.5f})s.Slot(0).Post(new Vector3(x+d,TugFloor,z),.9f,.24f,.22f,10,ZoneKit.Shape.Dim(red,.65f),red).Post(new Vector3(x+d,TugFloor+.9f,z),.08f,.3f,.3f,10,red,red);
                var winch=kit.Built("The tug's tow winch",s,IronMat,PlateMat);ZoneKit.Block(winch,bed+Vector3.up*1.1f,new Vector3(2.4f,2.2f,3.8f),0,"Winch");winch.isStatic=true;
                foreach(var (x,z) in new[]{(-7.4f,6.4f),(-7.4f,22f)})ZoneKit.Block(winch,new Vector3(x,TugFloor+.5f,z),new Vector3(1.6f,1,.7f),0,"Bitts");
                // The engine housing: a stepped iron box against the north-east, its exhaust trunk climbing the wall.
                var e=new ZoneKit.Shape();var at=new Vector3(7.8f,TugFloor,24.6f);
                e.Slot(0).Box(at+Vector3.up*1.2f,new Vector3(3.6f,2.4f,3.2f),Quaternion.Euler(0,-24,0),ZoneKit.Shape.Dim(red,.8f),red).Box(at+new Vector3(.4f,2.8f,.6f),new Vector3(2.4f,.8f,2),Quaternion.Euler(0,-24,0),ZoneKit.Shape.Dim(red,.7f),ZoneKit.Shape.Dim(red,.9f));
                e.Slot(1).Tube(new[]{at+new Vector3(.8f,3,1),at+new Vector3(1.4f,5,1.8f),at+new Vector3(1.6f,7.4f,2)},new[]{.42f,.42f,.42f},10,IronDark,IronPale,1,.4f);
                var housing=kit.Built("The tug's engine housing",e,IronMat,IronMat);ZoneKit.Block(housing,at+Vector3.up*1.4f,new Vector3(3.6f,2.8f,3.2f),-24,"Engine housing");housing.isStatic=true;ZoneKit.Fadeable(housing);
            }
            /// <summary>The hauler's hold: cargo frames along the east wall, a hopper chute high on the north wall west of joint 2, chains hanging from the ribs' heads.</summary>
            static void HaulerGear(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var ochre=PlateOf(Hull.Hauler);
                // The chute and the chains only where the wall drawn behind them stands high enough to carry them (joint 2's wall is the liner's foreground: D272).
                float chuteWall=Seen(new Vector2(-7.6f,98.6f))-HaulerFloor;
                if(chuteWall>=5.2f){var chute=new Vector3(-7.6f,HaulerFloor+4.4f,96.4f);s.Slot(0).Box(chute,new Vector3(3,2.4f,1.4f),Quaternion.Euler(-20,0,0),ZoneKit.Shape.Dim(ochre,.7f),ochre).Box(chute+new Vector3(0,-1.4f,-.6f),new Vector3(1.6f,.3f,1),Quaternion.Euler(-35,0,0),IronDark,IronPale);}
                foreach(float z in new[]{43.5f,77.5f,91.5f}){float rib=Mathf.Min(7.4f,Seen(new Vector2(12.6f,z))-HaulerFloor-.2f);if(rib<5.4f)continue;var top=new Vector3(9.4f,HaulerFloor+rib-.5f,z);for(float h=0;h<3.4f;h+=.2f)s.Slot(0).Box(top+Vector3.down*h,new Vector3(.06f,.18f,.1f),Quaternion.Euler(0,Mathf.Repeat(h*5,2)<1?0:90,0),IronDark,IronPale);}
                if(!s.Empty)kit.Built("Hauler gear",s,PlateMat);
            }
            /// <summary>A liner's luggage in the baggage bay and along the liner's walls: trunks, cases, a steamer's labels, stacked.</summary>
            static void Luggage(ZoneKit kit,System.Random rng)
            {
                var s=new ZoneKit.Shape();Color[] skins={new(.46f,.32f,.24f),new(.3f,.36f,.44f),new(.56f,.5f,.4f),new(.4f,.42f,.3f)};
                foreach(var (x,z,count) in new[]{(26.8f,55.6f,4),(26.8f,48f,3),(15.2f,56.6f,3),(23.6f,56.8f,3)})
                    for(int i=0;i<count;i++)
                    {
                        var c=new Vector3(x+((float)rng.NextDouble()-.5f)*.3f,HaulerFloor,z+((float)rng.NextDouble()-.5f)*.3f);float h=.5f+.2f*(float)rng.NextDouble();var tone=skins[rng.Next(skins.Length)];var turn=Quaternion.Euler(0,((float)rng.NextDouble()-.5f)*20,0);
                        s.Slot(0).Box(c+Vector3.up*(i*.62f+h*.5f),new Vector3(1.1f-i*.1f,h,.7f),turn,ZoneKit.Shape.Dim(tone,.8f),tone);
                        s.Slot(1).Box(c+Vector3.up*(i*.62f+h*.5f),new Vector3(1.14f-i*.1f,.06f,.74f),turn,new Color(.8f,.66f,.4f));
                    }
                var bags=kit.Built("Liner luggage",s,WoodMat,BrassMat);
                foreach(var (x,z,_) in new[]{(26.8f,55.6f,4),(26.8f,48f,3),(15.2f,56.6f,3),(23.6f,56.8f,3)})ZoneKit.Block(bags,new Vector3(x,HaulerFloor+.9f,z),new Vector3(1.2f,1.8f,.8f),0,"Luggage");
                bags.isStatic=true;
            }
            /// <summary>The Compact's module: white crates stencilled with its mark, a rack of its sealed cells.</summary>
            static void ModuleGear(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();
                foreach(var (x,z,h) in new[]{(26.6f,26.6f,2),(24.4f,26.8f,1),(26.8f,12.8f,2),(14.4f,26.8f,1)})for(int i=0;i<h;i++)s.Slot(0).Box(new Vector3(x,TugFloor+.45f+i*.92f,z),new Vector3(1.6f,.9f,1.1f),Quaternion.identity,new Color(.82f,.84f,.86f),new Color(.96f,.97f,.98f));
                foreach(var (x,z,h) in new[]{(26.6f,26.6f,2),(24.4f,26.8f,1),(26.8f,12.8f,2),(14.4f,26.8f,1)})s.Slot(1).Box(new Vector3(x,TugFloor+.45f+(h-1)*.92f+.2f,z-.56f),new Vector3(1.2f,.08f,.02f),Quaternion.identity,Color.white);
                var crates=kit.Built("Compact crates",s,WhiteMat,LampCyan);
                foreach(var (x,z,h) in new[]{(26.6f,26.6f,2),(24.4f,26.8f,1),(26.8f,12.8f,2),(14.4f,26.8f,1)})ZoneKit.Block(crates,new Vector3(x,TugFloor+h*.46f,z),new Vector3(1.6f,h*.92f,1.1f),0,"Crate");
                crates.isStatic=true;
            }
            /// <summary>The ore barge's hold round the press: its old ore chutes high on the north wall, the plant's sequence board beside them (a row of lamps waiting for the board's word),
            /// chains, and the barge's frames along its east wall.</summary>
            static void HallGear(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var iron=PlateOf(Hull.Barge);
                foreach(float x in new[]{-16f,-8f,8f,16f}){var at=new Vector3(x,BargeFloor+6,213.2f);s.Slot(0).Box(at,new Vector3(3.6f,3.2f,1.6f),Quaternion.Euler(-16,0,0),ZoneKit.Shape.Dim(iron,.8f),iron).Box(at+new Vector3(0,-2,-.8f),new Vector3(1.8f,.4f,1.2f),Quaternion.Euler(-30,0,0),IronDark,IronPale);}
                foreach(float z in new[]{178f,186f,194f,202f,210f}){var path=new[]{new Vector3(23.85f,BargeFloor,z),new Vector3(23.6f,BargeFloor+4,z),new Vector3(22.8f,BargeFloor+8,z)};s.Slot(0).Tube(path,new[]{.42f,.38f,.32f},8,ZoneKit.Shape.Dim(iron,.6f),iron,1,.4f);}
                var board=new Vector3(0,BargeFloor+4.6f,213.6f);s.Slot(0).Box(board,new Vector3(6,2.2f,.3f),Quaternion.identity,IronDark,IronPale);
                var hold=kit.Built("Barge hold",s,PlateMat);
                // The sequence board's lamps are the board's (alone they hung in the air 4.7 m up).
                var lamps=new ZoneKit.Shape();for(int i=0;i<8;i++)lamps.Slot(0).Disc(board+new Vector3(-2.45f+i*.7f,.3f,-.17f),Vector3.back,.2f,12,Color.white);kit.Built("Sequence lamps",lamps,LampWarm).transform.SetParent(hold.transform,true);
            }

            // ---------------------------------------------------------------- the floor
            /// <summary>The floor: each hull's deck plate, oil and wear in it, grit at the wall feet, scorch where the welding arms work, trolley tracks down the keel, painted lines where the
            /// station marked its walkways, a pool of warm light under each work lamp, and the light through the promenade's grating drifting over it all. Nothing on it orange, red or purple.</summary>
            static void Ground(ZoneKit kit)
            {
                var ground=GroundKit.Open(kit.zone,GroundKit.FindFloor(),71,1.1f,false,v=>{var plan=new Vector2(v.x,v.z);return FloorAt(plan,out float level)<=.15f&&Mathf.Abs(v.y-level)<.3f;});
                float Smooth(float a,float b,float x)=>GroundKit.Smooth(a,b,x);
                foreach(var way in Ways)ground.ClearLane(way,1.3f);
                ground.Look("Canyon",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Works/works-keel.png"));
                // Oil, grit and litter: no paving (crazy paving on a ship's deck read as a garden path, as it did in the Foundry), no brown (it read as soil).
                ground.Layers(new Vector4(.04f,.3f,.4f,.24f),1.1f,new GroundKit.Layer("damp",new Color(.74f,.75f,.78f),4),new GroundKit.Layer("gravel",new Color(.33f,.33f,.35f),2.6f),new GroundKit.Layer("litter",new Color(.62f,.62f,.62f,.85f),2.4f));
                // Oil and wet where the keel sweats: patches over a third of every deck, more in the hauler's hold and the barge's.
                ground.Paint(0,p=>(HullAt(p)==Hull.Hauler||HullAt(p)==Hull.Barge?.7f:.5f)*Smooth(.54f,.7f,ground.Noise(p,5,3)));
                // Rust flakes and grit at the wall feet, broken.
                ground.PaintFoot(1,1f,.6f,7).Fringe(1,0,.16f,.5f);
                // What a hold leaves on its deck: scraps and packing, most in the cargo hulls.
                ground.Paint(2,p=>(HullAt(p)==Hull.Hauler||HullAt(p)==Hull.Barge?.6f:.3f)*Smooth(.6f,.74f,ground.Noise(p,3,19)));
                ground.Shade(p=>.16f*Smooth(1.8f,.3f,ground.Edge(p)));
                ground.Decals(new[]{"Stain"},6,p=>.8f,1.6f,3.4f,new Color(.22f,.22f,.24f,.6f),false);
                ground.Decals(new[]{"CracksA","CracksB"},4,p=>.7f,1.2f,2.6f,new Color(.3f,.3f,.32f,.85f));
                ground.Decals(new[]{"Scorch"},6,p=>HullAt(p)==Hull.Liner||HullAt(p)==Hull.Skiff?.8f:.25f,1.2f,2.6f,new Color(.16f,.16f,.17f,.8f),false);
                ground.Decals(new[]{"PebblesA","PebblesB"},5,p=>.35f+.65f*(1-Smooth(.5f,3,ground.Edge(p))),.6f,1.2f,new Color(.4f,.4f,.42f));
                ground.Decals(new[]{"SurveyLine"},2,p=>Mathf.Abs(p.x)>3&&Mathf.Abs(p.x)<9?.6f:0,1.6f,3,new Color(.9f,.88f,.8f,.6f),false);
                ground.Decals(new[]{"Tracks"},1.6f,p=>HullAt(p)==Hull.Hauler||HullAt(p)==Hull.Barge?.8f:.2f,.8f,1.4f,new Color(.24f,.24f,.26f,.55f),false);
                ground.LampPools(2.6f,new Color(1,.92f,.78f,.32f));
                ground.Cover(new[]{"Shards"},14,p=>(1-Smooth(.5f,2.4f,ground.Edge(p)))*(ground.InAFight(p)?.3f:1),.08f,.2f,new Color(.36f,.37f,.4f),3);
                ground.Rocks(3,p=>(1-Smooth(.5f,2.2f,ground.Edge(p)))*.6f,.06f,.2f,new Color(.4f,.41f,.43f));
                ground.MovingShade("dapple",9,new Vector2(.04f,.02f),.32f,.45f,.04f);
                ground.Finish();
            }

            // ---------------------------------------------------------------- stills
            /// <summary>LookDev's views (LookDev.Views names this): each hull as it is walked, each joint from its jack, the rigs and what they drive, the control room, the pockets, the
            /// landing and the press hall, and wide views.</summary>
            internal static readonly LookDev.View[] Views=
            {
                new("arrival",new Vector3(0,TugFloor,-5)),new("tug",new Vector3(0,TugFloor,14)),new("joint-1",new Vector3(2.6f,TugFloor,31)),new("joint-4",new Vector3(7,TugFloor,18)),
                new("module",new Vector3(19,TugFloor,18)),new("hauler",new Vector3(0,HaulerFloor,48)),new("throat-rig",new Vector3(5,HaulerFloor,55)),new("throat",new Vector3(0,HaulerFloor,62)),
                new("hauler-north",new Vector3(0,HaulerFloor,82)),new("joint-2",new Vector3(3.4f,HaulerFloor,91)),new("joint-5",new Vector3(6.6f,HaulerFloor,46)),new("baggage",new Vector3(20,HaulerFloor,47)),
                new("control-stair",new Vector3(13,HaulerFloor,82)),new("control",new Vector3(24,ControlFloor,85)),new("liner",new Vector3(0,LinerFloor,106)),new("gallery",new Vector3(0,LinerFloor,120)),
                new("gallery-rig",new Vector3(10,LinerFloor,119)),new("gallery-north",new Vector3(0,LinerFloor,136)),new("joint-6",new Vector3(12,LinerFloor,131)),new("skiff",new Vector3(24,LinerFloor,132)),
                new("joint-3",new Vector3(2.6f,LinerFloor,141)),new("stair",new Vector3(0,LinerFloor-1.5f,157)),new("landing",new Vector3(0,BargeFloor,165)),new("hall",new Vector3(0,BargeFloor,180)),
                new("hall-arms",new Vector3(0,BargeFloor,186)),new("hall-lift",new Vector3(16,BargeFloor,204)),
                new("overview-tug",new Vector3(4,TugFloor,18),false,3.2f),new("overview-hauler",new Vector3(4,HaulerFloor,72),false,3.6f),new("overview-liner",new Vector3(4,LinerFloor,124),false,3.6f),new("overview-hall",new Vector3(0,BargeFloor,192),false,3.4f),
            };
        }
    }
}
