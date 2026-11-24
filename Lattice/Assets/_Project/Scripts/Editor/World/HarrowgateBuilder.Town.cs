using System;
using System.Collections.Generic;
using System.IO;
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
    /// <summary>Harrowgate (town, safe, then the duel on the muster field). Map brief, plan, coordinates and the duel ground's contract: docs/maps/Harrowgate.md. Everything of the
    /// zone's own is in <see cref="Town"/>, so nothing here can collide with the other chapter 8 files' members.</summary>
    public static partial class HarrowgateBuilder
    {
        /// <summary>What the town sets on its own definition after HarrowgateBuilder.Define's common fields: basalt is dark, so floor signs keep the dark plate.</summary>
        static void TownDefinition(ZoneDef z){z.brightFloor=false;}
        static void BuildTown()=>Town.Build();
        [ZoneLook.Source] static void TownLooks()
        {
            // A clear cool evening over basalt and cook-smoke: a low sun from the south-west, near white, so every south face the town turns to the camera is lit; a cool fill and haze,
            // so the town's fires (the forges, the braziers round the field and the hall, the lamps at the doors and on the walk) carry the warm colour and lead the eye, and each
            // district's own stone shows. The first look was a warm afternoon, one brown from end to end; a cold blue evening (harrow-07) turned every district the same blue.
            ZoneLook.Register(TownZone,new ZoneLook.Spec{sun=C(1f,.95f,.88f),sunIntensity=1.25f,sunPitch=36,shadowStrength=.78f,
                sky=C(.47f,.5f,.57f),equator=C(.41f,.41f,.43f),ground=C(.17f,.16f,.16f),
                fog=true,fogColor=C(.52f,.54f,.6f),fogStart=55,fogEnd=230,
                contrast=12,saturation=10,splitShadows=C(.42f,.46f,.58f),splitHighlights=C(.62f,.55f,.45f),
                bloomThreshold=.88f,bloomIntensity=.7f,vignette=.22f});
        }

        internal static partial class Town
        {
            // ---------------------------------------------------------------- levels (the floor's height on each terrace)
            internal const float Quay=0,Lower=2.4f,Upper=4.8f,Podium=7.2f,Cells=1f,Sea=-1.6f,Bed=-4.5f,CliffTop=8;

            // ---------------------------------------------------------------- who and what stands where (plan coordinates; routes, tests and LookDev read these)
            internal static readonly Vector2 Arrival=new(-40,8),Pad=new(-26,3.5f),RoadArch=new(-52.6f,9),GateFoot=new(8,13),GateHead=new(8,28),HessaGate=new(16.6f,14.6f),VessaGate=new(19.2f,15.8f),
                PipWaits=new(11.6f,30.6f),PipFirstAt=new(8,29.6f),Scales=new(34,10.6f),FlourAt=new(31.4f,12.2f),
                WashingAt=new(-11,42.6f),KitchenDoor=new(-25,40),OdrunAt=new(-21.8f,46.5f),TableAt=new(-27.2f,43.4f),TovraAt=new(-30.3f,43.4f),BrekkAt=new(-24,53.4f),
                StairFoot=new(-38.5f,52),StairGameAt=new(-38.5f,49.6f),ForgesAt=new(23,41),MenderAt=new(16,44.2f),ArmourersSpawn=new(19,39),KettleAt=new(29.5f,44.2f),AnvilAt=new(35.5f,44.2f),
                SaltAt=new(45.4f,49.6f),FieldSpawn=new(0,64),VerdictSpawn=new(-4,80),FieldCradle=new(7,62.6f),LaunchPad=new(-11,62.4f),PipFieldAt=new(-8,70.2f),
                F=new(0,87),DuelMiddle=new(0,85),GuardPost=new(14,95),PipThirdFile=new(-20.2f,80),BellAt=new(20.6f,99.8f),SkiffAt=new(30,93),WreckAt=new(8.4f,92),
                DraskAt=new(0,109.4f),NamesAt=new(-8,110),Case3=new(9,109.6f),Case1=new(-43,109.4f),DoorCase=new(-31,109.4f),Case2=new(34,109.4f),
                Lookout=new(54,121),ArmouryGate=new(47.6f,80.25f),HessaArmoury=new(44.4f,82.8f),StairMid=new(50.8f,88.5f),StairFootAt=new(50.8f,95),CellAt=new(56,110.6f),CellFront=new(56,107.4f),
                OdrunVerdictAt=new(-2.6f,89.2f),DraskVerdictAt=new(2.4f,89.6f),PipAfterAt=new(-21.8f,38.6f);
            /// <summary>The muster field inside its kerb (the duel's floor), and the kerb's three openings.</summary>
            internal const float FieldHalfX=17.5f,FieldSouth=72.5f,FieldNorth=97.5f,KerbTall=.5f,SideOpen=3,SouthOpen=4;
            /// <summary>The ramps through the risers: centre x, half-width, where they start and end, and the levels at each end.</summary>
            internal static readonly (float x,float half,float from,float to,float low,float high)[] Ramps=
            {
                (8,4,13,27,Quay,Lower),(0,4,56,66,Lower,Upper),(-38.5f,2.5f,56,66,Lower,Upper),(42.5f,2.5f,56,66,Lower,Upper),(0,14,98.4f,105.5f,Upper,Podium),
            };
            /// <summary>The ways feet go between the town's places: nothing is put down within reach of these (<see cref="NothingStandsOnTheWays"/>), and the ground kit keeps them clear
            /// and paints them trodden.</summary>
            internal static readonly Vector2[][] Ways=
            {
                new[]{Arrival,new Vector2(-26,7.6f),new Vector2(-12,8.4f),new Vector2(2,10.4f),GateFoot,new Vector2(8,27),new Vector2(8,32)},
                new[]{Arrival,new Vector2(-50.4f,9)},new[]{new Vector2(-26,7.6f),Pad+new Vector2(0,1.8f)},new[]{new Vector2(2,10.4f),new Vector2(20,9.2f),new Vector2(31,9.2f)},
                new[]{new Vector2(-50,35),new Vector2(8,35),new Vector2(44,35)},new[]{new Vector2(8,32),new Vector2(8,35)},
                new[]{new Vector2(-25,35),KitchenDoor,new Vector2(-25,44.4f),new Vector2(-24.4f,48),new Vector2(-24.4f,50.6f),new Vector2(-24,52)},
                new[]{new Vector2(-38.5f,35),StairFoot,new Vector2(-38.5f,66)},new[]{new Vector2(0,35),new Vector2(0,52),new Vector2(0,66)},new[]{new Vector2(42.5f,35),new Vector2(42.5f,52),new Vector2(42.5f,66)},
                new[]{new Vector2(16,35),new Vector2(16,42.4f)},new[]{new Vector2(29.5f,35),new Vector2(29.5f,42.4f)},new[]{new Vector2(35.5f,35),new Vector2(35.5f,42.4f)},
                new[]{new Vector2(-50,66),new Vector2(0,66),new Vector2(43.6f,66)},new[]{new Vector2(0,66),new Vector2(0,FieldSouth),F},
                new[]{new Vector2(-24,85),new Vector2(-FieldHalfX,85),new Vector2(FieldHalfX,85),new Vector2(43.6f,85)},
                new[]{new Vector2(43.6f,66),new Vector2(43.6f,99),new Vector2(44,113.6f),Lookout},
                new[]{new Vector2(-24,85),new Vector2(-24,104)},new[]{new Vector2(-52,104),new Vector2(-20,104)},new[]{new Vector2(22,104),new Vector2(38,104),new Vector2(43.6f,99)},
                new[]{F,new Vector2(0,98),new Vector2(0,107.8f)},new[]{new Vector2(-10,108.2f),new Vector2(10,108.2f)},
                // Round the Warmaster on his post at F, east of him, while he stands there before the duel (he stops a hero: Duel's "The Warmaster standing on his post").
                new[]{new Vector2(0,85),new Vector2(2.4f,86),new Vector2(2.4f,88.4f),new Vector2(0,90)},
                new[]{new Vector2(43.6f,80.25f),new Vector2(48,80.25f),new Vector2(50.8f,81.4f),StairFootAt,CellFront},
            };
            internal static readonly (Vector2 at,float half)[] Signs=
            {
                (new(-1.2f,7.6f),2.8f),(new(8.6f,69.4f),2.6f),(new(-7.6f,95.4f),2.4f),(new(23.4f,38.2f),3.6f),(new(-31,38.2f),3.2f),(new(-38,102.4f),3.4f),
                (new(31,102.4f),2.8f),(new(48.6f,116.4f),2.8f),(new(44.6f,76.4f),2.6f),(new(58.6f,104),2.8f),
            };
            static readonly string[] SignWords={"HARBOUR GATE","MUSTER FIELD","OATH HALL","ARMOURERS' STREET","FAMILY QUARTER","TROPHY GALLERY","EAST GALLERY","HARBOUR WALL","ARMOURY GATE","HARBOUR CELLS"};
            static readonly Vector2[] Named={HessaGate,OdrunAt,BrekkAt,DraskAt,CellAt};

            /// <summary>LookDev's stills of the town (registered in LookDev.Views): each place as it is first walked, at its floor's height, the field as the duel uses it, then wide views.</summary>
            internal static LookDev.View[] Views=>new[]
            {
                V("arrival",Arrival,Quay),V("pad",Pad+new Vector2(4,4),Quay),V("sisters",new Vector2(0,9),Quay),V("gate",new Vector2(10,12),Quay),V("scales",new Vector2(30,8),Quay),
                V("gate-landing",new Vector2(8,31),Lower),V("family",new Vector2(-46,38),Lower),V("washing",new Vector2(-12,38.6f),Lower),V("kitchen",new Vector2(-25,43),Lower),
                V("hidden-quarter",new Vector2(-25,52.4f),Lower),V("family-stair",new Vector2(-38.5f,47),Lower),V("armourers",new Vector2(22,38),Lower),V("forges-east",new Vector2(36,40),Lower),
                V("central-ramp",new Vector2(0,48),Lower),V("forecourt",new Vector2(-6,64),Upper),V("cadet-yard",new Vector2(-38,82),Upper),V("field-south",new Vector2(0,74),Upper),
                V("field",new Vector2(0,85),Upper),V("field-north",new Vector2(4,94),Upper),V("field-east",new Vector2(12,86),Upper),V("field-west",new Vector2(-12,84),Upper),
                V("steps",new Vector2(0,101),Upper+.8f),V("portico",new Vector2(0,107.6f),Podium),V("trophy-gallery",new Vector2(-36,104),Upper),V("door-case",new Vector2(-30,105),Upper),
                V("east-gallery",new Vector2(32,104),Upper),V("bell",new Vector2(22,96),Upper),V("armoury",new Vector2(36,84),Upper),V("wall-walk",new Vector2(43.6f,92),Upper),
                V("bastion",new Vector2(52,118),Upper),V("lookout",Lookout,Upper),V("armoury-gate",new Vector2(45,80),Upper),V("stair",new Vector2(50.8f,88),2.9f),V("cells",new Vector2(56,104.6f),Cells),
                new LookDev.View("overview",new Vector3(0,Lower,52),false,4.4f),new LookDev.View("overview-field",new Vector3(4,Upper,90),false,2.4f),new LookDev.View("overview-harbour",new Vector3(40,Upper,90),false,3.2f),
            };
            static LookDev.View V(string name,Vector2 at,float floor)=>new(name,new Vector3(at.x,floor,at.y));

            // ---------------------------------------------------------------- the ground
            static float Smooth(float a,float b,float x)=>ZoneKit.Smooth(a,b,x);
            /// <summary>Metres inside an axis-aligned rectangle (negative outside it).</summary>
            static float Inside(Vector2 p,float x0,float x1,float z0,float z1)=>Mathf.Min(Mathf.Min(p.x-x0,x1-p.x),Mathf.Min(p.y-z0,z1-p.y));
            /// <summary>Metres inside the town's land and the shores round the harbour; negative over the water.</summary>
            internal static float Land(Vector2 p)
            {
                float d=Inside(p,-90,50,-6,22.6f);
                d=Mathf.Max(d,Inside(p,-90,48,22,112.4f));
                d=Mathf.Max(d,Inside(p,-90,64.5f,112,126.5f));     // the bastion over the cells' landing
                d=Mathf.Max(d,Inside(p,-90,38,126,300));           // the crag behind the hall and the galleries
                d=Mathf.Max(d,Inside(p,47,53.6f,78.5f,96.2f));     // the stair down outside the harbour wall
                d=Mathf.Max(d,Inside(p,47,64.5f,95.8f,112.4f));    // the cells' landing at the water
                d=Mathf.Max(d,Inside(p,116,300,-60,300));          // the harbour's far shore
                d=Mathf.Max(d,Inside(p,-90,300,172,300));          // and its north shore, past the carrier's berth
                return d;
            }
            static float Riser(float z,float line)=>Smooth(line-.7f,line+.7f,z);
            /// <summary>The stair outside the harbour wall and the landing at its foot: level with the upper terrace through the armoury gate, down to the landing.</summary>
            static float StairLevel(float z)=>z<82?Upper:z<94?Mathf.Lerp(Upper,Cells,(z-82)/12):Cells;
            static float CragTop(Vector2 p)=>13+2.6f*(Mathf.PerlinNoise(p.x*.08f+3,p.y*.08f+7)-.5f)+1.3f*(Mathf.PerlinNoise(p.x*.23f+1,p.y*.23f+5)-.5f);
            /// <summary>The town's ground: four levels stepped up toward the north (the quay, the lower and upper terraces, the oath hall's podium), each riser a masonry wall facing the
            /// camera; ramps through the risers; the stair and the landing outside the harbour wall; basalt crag west and north; the harbour's bed under the water.</summary>
            internal static float Height(Vector2 p)
            {
                float x=p.x,z=p.y;
                float y=(Lower-Quay)*Riser(z,22.2f)+(Upper-Lower)*Riser(z,58.2f);
                // The podium under the oath hall; its steps are the last ramp.
                y=Mathf.Lerp(y,Podium,Smooth(-.3f,.3f,Inside(p,-16,16,105.5f,400)));
                foreach(var (cx,half,from,to,low,high) in Ramps)
                {
                    float across=1-Smooth(half-.3f,half+.3f,Mathf.Abs(x-cx));if(across<=0)continue;
                    float along=Smooth(from-.6f,from,z)*(1-Smooth(to,to+.6f,z));if(along<=0)continue;
                    y=Mathf.Lerp(y,Mathf.Lerp(low,high,Mathf.Clamp01((z-from)/(to-from))),across*along);
                }
                // Outside the harbour wall: the stair down from the armoury gate and the landing at the water.
                float outside=Smooth(47.7f,48.3f,x)*Smooth(78.2f,78.8f,z)*(1-Smooth(111.7f,112.3f,z));
                if(outside>0)y=Mathf.Lerp(y,StairLevel(z),outside);
                // Crag: west of the town, behind the galleries and the hall, and round the harbour's far shores.
                float crag=Smooth(-54.4f,-57.4f,x);
                float back=Mathf.Abs(x)<16.5f?124.8f:113.2f;crag=Mathf.Max(crag,Smooth(back,back+3.2f,z)*(1-Smooth(37.6f,38.4f,x)));
                crag=Mathf.Max(crag,Mathf.Max(Smooth(116,120,x),Smooth(172,176,z)));
                if(crag>0)y=Mathf.Lerp(y,CragTop(p),crag);
                return Mathf.Lerp(Bed,y,Smooth(-.5f,.2f,Land(p)));
            }
            static float FromWays(Vector2 p){float d=float.PositiveInfinity;foreach(var way in Ways)d=Mathf.Min(d,ZoneKit.FromPath(p,way));return d;}
            internal static bool OnTheField(Vector2 p)=>Mathf.Abs(p.x)<FieldHalfX&&p.y>FieldSouth&&p.y<FieldNorth;
            /// <summary>The floor's own colour, each district its own stone so a still says where it is before anything in it is read: the quay's cold blue basalt, darker and wet toward
            /// the water; the family quarter's warm limewashed sandstone; the armourers' street black with soot; the terraces' dressed grey; the field dark cinder slate, so its pale
            /// inlay reads; the oath hall's podium honey stone; the stair and the cells cold. Paler where feet go. (One grey-brown from end to end in harrow-06.)</summary>
            static Color Tone(Vector2 p)
            {
                float x=p.x,z=p.y;
                float mottle=.9f+.2f*Mathf.PerlinNoise(x*.19f+5,z*.19f+1),patch=Mathf.PerlinNoise(x*.06f+11,z*.06f+3);
                Color tone,worn;
                // The harbour's bed: dark silt. Paved like the town, its flags and the ground kit's patches showed through the water as a camouflage pattern (the second stills).
                if(Land(p)<.05f){float silt=.85f+.15f*patch;return new Color(.12f*silt,.13f*silt,.14f*silt,1);}
                // Each a colour the camera reads at a glance (harrow-08's were a shade apart and read as one grey): the quay's cold slate, wet and darker toward the water; the family
                // quarter's warm sandstone; the armourers' soot, blacker toward the forges; the cadet yard's sand-washed drill floor; the field's dark slate; the hall's honey stone; the
                // harbour wall and the bastion weathered green-grey; the stair and the cells cold; the terraces between pale dressed grey.
                if(x>47.8f&&z>78&&z<112.2f){tone=new Color(.4f,.46f,.52f);worn=new Color(.56f,.6f,.64f);}
                else if(z<22.6f){tone=Color.Lerp(new Color(.4f,.48f,.6f),new Color(.28f,.35f,.44f),1-Smooth(.4f,3,Land(p)));worn=new Color(.6f,.66f,.74f);}
                else if(z<58.6f&&x<-2){tone=Color.Lerp(new Color(1f,.74f,.46f),new Color(1f,.78f,.5f),patch);worn=new Color(1f,.86f,.64f);}
                else if(z<58.6f){tone=Color.Lerp(new Color(.32f,.29f,.28f),new Color(.24f,.22f,.21f),Smooth(46,52,z)*Smooth(10,14,x));worn=new Color(.46f,.42f,.4f);}
                else if(x<-24.5f&&x>-54&&z>70&&z<100.5f){tone=Color.Lerp(new Color(.84f,.76f,.6f),new Color(.88f,.8f,.64f),patch);worn=new Color(.92f,.86f,.72f);}
                else if(OnTheField(p)){tone=Color.Lerp(new Color(.34f,.36f,.42f),new Color(.38f,.4f,.46f),patch);worn=new Color(.46f,.48f,.52f);}
                else if(z>105.4f&&Mathf.Abs(x)<16){tone=new Color(.96f,.8f,.54f);worn=new Color(1f,.88f,.66f);}
                else if(x>41.5f&&z>61){tone=Color.Lerp(new Color(.5f,.57f,.52f),new Color(.44f,.52f,.47f),patch);worn=new Color(.62f,.66f,.62f);}
                else{tone=Color.Lerp(new Color(.74f,.74f,.74f),new Color(.78f,.77f,.75f),patch);worn=new Color(.86f,.85f,.82f);}
                tone=Color.Lerp(tone,worn,(1-Smooth(.9f,2.6f,FromWays(p)))*.35f);
                return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
            }
            /// <summary>Refuses anything put down on a riser, a ramp's side, in the water or over the edge: a terraced town is shelves, and a coordinate a metre out is a body in a wall.</summary>
            static void OnTheFloor(ZoneKit kit,string what,params Vector2[] spots)
            {
                foreach(var at in spots)
                {
                    float y=kit.Y(at.x,at.y),spread=0;
                    foreach(var (dx,dz) in new[]{(.6f,0f),(-.6f,0f),(0f,.6f),(0f,-.6f)})spread=Mathf.Max(spread,Mathf.Abs(kit.Y(at.x+dx,at.y+dz)-y));
                    if(Land(at)<.3f||spread>.25f||y>CliffTop)throw new InvalidOperationException($"Harrowgate: {what} at {at} is not on level floor (height {y:0.00}, rise {spread:0.00} over .6 m)");
                }
            }

            // ---------------------------------------------------------------- what the town is made of
            internal static Material Flags,Masonry,Stone,Slate,Timber,Iron,Bronze,Cloth,Lamp,Rope,Dark,Coal,Water,Glass,Inlay,Flame,FlameCore;
            static void Materials()
            {
                // The floor: basalt flags laid in courses of their own widths, each stone its own size and tone, a dark joint between. Sixteen metres to a repeat (the floor's uv is a
                // repeat to eight, halved on the material), sixty-four texels a metre.
                const float Span=16;var lay=new System.Random(61);var courses=new List<(float z0,float z1,float[] breaks,float[] tones)>();
                for(float at=0;at<Span-.01f;)
                {
                    float deep=Mathf.Min(Span-at,.7f+(float)lay.NextDouble()*.7f);if(Span-at-deep<.6f)deep=Span-at;
                    var breaks=new List<float>();var tones=new List<float>();float x=(float)lay.NextDouble()*1.2f;breaks.Add(0);
                    while(x<Span-.5f){breaks.Add(x);tones.Add(.5f+.16f*(float)lay.NextDouble());x+=.8f+(float)lay.NextDouble()*1.1f;}
                    tones.Add(.5f+.16f*(float)lay.NextDouble());courses.Add((at,at+deep,breaks.ToArray(),tones.ToArray()));at+=deep;
                }
                var flags=ZoneKit.Texture("Harrowgate/harrow-flags",1024,(u,v)=>
                {
                    float mx=u*Span,my=v*Span;var c=courses[courses.Count-1];foreach(var each in courses)if(my<each.z1){c=each;break;}
                    int k=0;for(int i=1;i<c.breaks.Length;i++)if(mx>=c.breaks[i])k=i;
                    float left=mx-c.breaks[k],right=(k+1<c.breaks.Length?c.breaks[k+1]:Span+c.breaks[1])-mx,edge=Mathf.Min(Mathf.Min(left,right),Mathf.Min(my-c.z0,c.z1-my));
                    float tone=c.tones[Mathf.Min(k,c.tones.Length-1)]+.08f*(ZoneKit.Tile(u*180,v*180,180,3)-.5f)+.06f*(ZoneKit.Cloud(u,v,12,5)-.5f);
                    // Chisel-dressed faces: a few pits, a softened arris, a black joint.
                    if(ZoneKit.Tile(u*300,v*300,300,9)>.93f)tone-=.1f;
                    tone-=edge<.025f?.32f:edge<.06f?.08f:0;
                    return new Color(tone,tone*.985f,tone*.97f,1);
                });
                // Walls: coursed basalt, ten courses to a repeat (the landform lays its walls' texture 7 m along by 5 m up), each block its own length and tone, the joints recessed.
                var rows=new List<(float[] breaks,float[] tones)>();var cut=new System.Random(67);
                for(int r=0;r<10;r++){var b=new List<float>{0};var t=new List<float>();float at=(float)cut.NextDouble()*.12f;while(at<.93f){b.Add(at);t.Add(.48f+.18f*(float)cut.NextDouble());at+=.1f+(float)cut.NextDouble()*.14f;}t.Add(.48f+.18f*(float)cut.NextDouble());rows.Add((b.ToArray(),t.ToArray()));}
                var masonry=ZoneKit.Texture("Harrowgate/harrow-masonry",512,(u,v)=>
                {
                    float row=v*10;int r=Mathf.Min(9,Mathf.FloorToInt(row));float up=row-r;var (breaks,tones)=rows[r];
                    int k=0;for(int i=1;i<breaks.Length;i++)if(u>=breaks[i])k=i;
                    float left=u-breaks[k],right=(k+1<breaks.Length?breaks[k+1]:1+breaks[1])-u,side=Mathf.Min(left,right)*10;
                    float tone=tones[Mathf.Min(k,tones.Length-1)]+.1f*(ZoneKit.Tile(u*128,v*128,128,7)-.5f)+.05f*(ZoneKit.Cloud(u,v,8,2)-.5f);
                    // Each block's face is pillowed: lit along its top edge, shaded along its bottom; a dark recessed joint all round.
                    tone+=.06f*(up-.5f);
                    // Weather (s53/014: plain grey brick with no wear): a block here and there sooted or bleached, grime run down from the joints in streaks, lichen on some faces,
                    // pits, and arrises knocked off at the corners showing paler stone.
                    int block=r*31+k;float own=Hash(block);
                    if(own>.86f)tone-=.07f;else if(own<.1f)tone+=.05f;
                    float streak=ZoneKit.Smooth(.58f,.9f,ZoneKit.Tile(u*70,r*3.3f+.5f,70,8))*(.35f+.65f*(1-up));tone-=.08f*streak;
                    if(ZoneKit.Tile(u*300,v*300,300,9)>.92f)tone-=.06f;
                    bool chipped=Hash(block*3+(up<.5f?1:2)+(left<right?4:8))>.72f;
                    if(chipped&&side<.16f&&(up<.2f||up>.82f)&&side+(up<.5f?up:1-up)*.8f<.2f)tone+=.09f;
                    if(up<.06f||up>.95f||side<.05f)tone=tones[Mathf.Min(k,tones.Length-1)]*.42f+.04f*(ZoneKit.Tile(u*200,v*200,200,10)-.5f);else if(up<.12f||side<.1f)tone-=.08f;
                    float lichen=ZoneKit.Smooth(.58f,.78f,ZoneKit.Cloud(u,v,6,9))*(own>.5f?1:.3f);
                    return new Color(tone*(1-.06f*lichen),tone*(.98f+.01f*lichen),tone*(.965f-.09f*lichen),1);
                });
                // Slate: roofs in overlapping courses, each slate's tail a shade darker.
                var slate=ZoneKit.Texture("Harrowgate/harrow-slate",512,(u,v)=>
                {
                    float row=v*8;int r=Mathf.FloorToInt(row);float down=row-r,x=u*10+(r%2)*.5f;float side=Mathf.Abs(x-Mathf.Round(x));
                    float tone=.42f+.16f*down+.08f*(ZoneKit.Tile(Mathf.Floor(x)*3.1f,r*1.7f,64,4)-.5f)+.05f*(ZoneKit.Tile(u*200,v*200,200,8)-.5f);
                    if(down<.08f||side<.03f)tone-=.18f;return new Color(tone,tone*1.01f,tone*1.04f,1);
                });
                var timber=ZoneKit.Texture("Harrowgate/harrow-timber",512,(u,v)=>
                {
                    float row=v*5;int board=Mathf.FloorToInt(row);float across=row-board,seam=Mathf.Min(across,1-across);
                    float own=ZoneKit.Tile(board*3.7f,.5f,5,41),grain=ZoneKit.Tile(u*8+board*2.3f,v*80,80,6);
                    float tone=.56f+.18f*(own-.5f)+.1f*(grain-.5f)-(seam<.05f?.28f:0);return new Color(tone*1.04f,tone,tone*.9f,1);
                });
                var iron=ZoneKit.Texture("Harrowgate/harrow-iron",256,(u,v)=>{float tone=.5f+.12f*(ZoneKit.Cloud(u,v,9,3)-.5f)+.08f*(ZoneKit.Tile(u*120,v*120,120,5)-.5f);return new Color(tone,tone*.98f,tone*.97f,1);});
                var cloth=ZoneKit.Texture("Harrowgate/harrow-cloth",256,(u,v)=>{float weave=(Mathf.Sin(u*Mathf.PI*96)*Mathf.Sin(v*Mathf.PI*96))*.04f;float tone=.78f+weave+.08f*(ZoneKit.Cloud(u,v,6,4)-.5f);return new Color(tone,tone,tone,1);});
                Flags=ZoneKit.Toon("harrow-flags",flags,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.5f,.62f));m.SetTextureScale("_BaseMap",new Vector2(.5f,.5f));});
                Masonry=ZoneKit.Toon("harrow-masonry",masonry,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.48f,.48f,.6f));});
                Stone=ZoneKit.Toon("harrow-stone",masonry,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.48f,.48f,.6f));});
                Slate=ZoneKit.Toon("harrow-slate",slate,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.46f,.48f,.6f));});
                Timber=ZoneKit.Toon("harrow-timber",timber,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.5f,.48f,.6f));});
                Iron=ZoneKit.Toon("harrow-iron",iron,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",5);m.SetColor("_RimColor",new Color(.7f,.72f,.78f));});
                Bronze=ZoneKit.Toon("harrow-bronze",iron,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.95f,.72f,.42f));m.SetColor("_RimColor",new Color(1,.86f,.6f));m.SetFloat("_RampSteps",5);});
                Cloth=ZoneKit.Toon("harrow-cloth",cloth,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);ZoneKit.Swaying(m,.05f,1.4f,1.4f,.5f);});
                Lamp=ZoneKit.Toon("harrow-lamp",null,m=>{m.SetColor("_BaseColor",new Color(1,.82f,.5f));m.SetColor("_EmissionColor",new Color(2.1f,1.4f,.55f));});
                Coal=ZoneKit.Toon("harrow-coal",null,m=>{m.SetColor("_BaseColor",new Color(.9f,.6f,.36f));m.SetColor("_EmissionColor",new Color(1.6f,.72f,.26f));});
                Glass=ZoneKit.Toon("harrow-glass",null,m=>{ZoneKit.TwoSided(m);m.SetColor("_BaseColor",new Color(.86f,.92f,.94f));m.SetColor("_EmissionColor",new Color(.22f,.26f,.28f));});
                Dark=ZoneKit.Toon("harrow-dark",null,m=>m.SetColor("_BaseColor",new Color(.07f,.065f,.07f)));
                Rope=ZoneKit.Toon("harrow-rope",null,m=>m.SetColor("_BaseColor",new Color(.78f,.72f,.6f)),"Universal Render Pipeline/Unlit");
                // The harbour: slate-teal, paler for five metres out from every wall with a line of foam where it laps, the ripple's grain small enough to read as a sheen and glints on it.
                // Near black with a third-of-a-metre ripple (harrow-06) it read as dark camouflage, not water: no colour, no edge, nothing catching the light.
                // The ripple is a swell a metre or so across, and its glints are few: at a seventh of a metre (harrow-08 to harrow-10) the second octave was under eight pixels at the
                // play camera, and the glints thresholded off it shimmered as static over the whole harbour (s51/107, s51/123).
                Water=ZoneKit.Toon("harrow-water",null,m=>ZoneKit.WaterLook(m,new Color(.36f,.53f,.56f,.9f),new Color(.15f,.27f,.31f,.96f),new Color(.84f,.9f,.9f,1),5,1f,.022f,.5f));
                // Fire: tongues that flicker from the root up (the cloth's sway, quick and fluttering), deep orange at the root going to yellow at the tip in their vertex colours, and a
                // near-white core. Lit to one emission the tongues read as a flat yellow card (s53/014): the outside glows less, so the core is what's brightest.
                Flame=ZoneKit.Toon("harrow-flame",null,m=>{ZoneKit.TwoSided(m);ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_EmissionColor",new Color(1.5f,.5f,.1f));ZoneKit.Swaying(m,.07f,3.6f,.7f,.95f);});
                FlameCore=ZoneKit.Toon("harrow-flame-core",null,m=>{ZoneKit.TwoSided(m);ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_EmissionColor",new Color(2.6f,1.8f,.8f));ZoneKit.Swaying(m,.05f,4.2f,.5f,.95f);});
                // The field's inlay: polished stone, flat colour with no coursing (drawn with the town's masonry it read as brickwork laid in the paving).
                Inlay=ZoneKit.Toon("harrow-inlay",null,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",3);m.SetColor("_ShadowTint",new Color(.6f,.6f,.7f));m.SetColor("_RimColor",new Color(.9f,.9f,.94f));});
                LookMaterials();
                AssetDatabase.SaveAssets();
            }

            // ---------------------------------------------------------------- the zone
            internal static void Build()
            {
                Materials();
                var kit=ZoneKit.Ground(TownZone,Height,CliffTop);
                var scene=WorldBuilder.Begin(kit.zone);
                var land=kit.Terrain("Basalt terraces",-66,122,-24,176,.5f,Flags,Masonry,Tone,new Color(.74f,.72f,.7f),new Color(.95f,.93f,.9f));
                var water=kit.Water("Harbour water",-66,122,-24,176,1,Sea,Water,6);
                var patch=water.AddComponent<BiomePatch>();patch.resourceKey="Harrowgate-harbour";patch.palette="River";patch.water=true;

                WorldBuilder.Spawn("Arrival",kit.On(Arrival.x,Arrival.y));WorldBuilder.Spawn("Armourers",kit.On(ArmourersSpawn.x,ArmourersSpawn.y));
                WorldBuilder.Spawn("Field",kit.On(FieldSpawn.x,FieldSpawn.y));WorldBuilder.Spawn("Verdict",kit.On(VerdictSpawn.x,VerdictSpawn.y));

                OnTheFloor(kit,"someone with a name",Named);OnTheFloor(kit,"a floor sign",Signs.Select(s=>s.at).ToArray());
                OnTheFloor(kit,"a place to stand",Arrival,Pad,GateFoot,GateHead,PipWaits,Scales,WashingAt,KitchenDoor,TableAt,TovraAt,StairGameAt,ForgesAt,MenderAt,ArmourersSpawn,KettleAt,AnvilAt,SaltAt,
                    FieldSpawn,VerdictSpawn,FieldCradle,LaunchPad,PipFieldAt,F,GuardPost,PipThirdFile,BellAt,SkiffAt,WreckAt,NamesAt,Case3,Case1,DoorCase,Case2,Lookout,ArmouryGate,HessaArmoury,StairFootAt,CellFront,
                    OdrunVerdictAt,DraskVerdictAt,PipAfterAt);
                foreach(var way in Ways)OnTheFloor(kit,"a way's corner",way);
                OnTheFloor(kit,"a townsperson",Folk.Select(f=>f.at).ToArray());
                OnTheFloor(kit,"the parley flag",ParleyAt);OnTheFloor(kit,"a guard at the cells",CellGuards.Select(g=>g.at).ToArray());

                Parapets(kit);
                Quayside(kit);
                HarbourGate(kit);
                FamilyQuarter(kit);
                Kitchen(kit);
                Armourers(kit);
                UpperTerrace(kit);
                OathHall(kit);
                Galleries(kit);
                Field(kit);
                StairAndCells(kit);
                Harbour(kit);
                Dressing(kit);
                FloorMarks(kit);
                Braziers(kit);
                for(int i=0;i<Signs.Length;i++)kit.Sign(SignWords[i],Signs[i].at.x,Signs[i].at.y);

                People(kit);
                Life(kit);
                Story(kit);
                Duel(kit);
                Streets(kit);
                Smoke(kit);
                FaceSomething();

                SeatBlocks();
                NothingStandsOnTheWays();
                ErrandsWalkClear(kit);
                Bake(kit);
                Ground(kit,land);
                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("HARROWGATE_COORDS arrival="+Arrival+" pad="+Pad+" road="+RoadArch+" gate="+GateFoot+"->"+GateHead+" hessa="+HessaGate+" pip-first="+PipFirstAt+" washing="+WashingAt+" kitchen-door="+KitchenDoor
                    +" odrun="+OdrunAt+" table="+TableAt+" brekk="+BrekkAt+" stair-foot="+StairFoot+" forges="+ForgesAt+" mender="+MenderAt+" anvil="+AnvilAt+" kettle="+KettleAt+" salt="+SaltAt+" flour="+FlourAt
                    +" field-spawn="+FieldSpawn+" verdict="+VerdictSpawn+" F="+F+" guard-post="+GuardPost+" pip-field="+PipFieldAt+" drask="+DraskAt+" names="+NamesAt+" case1="+Case1+" door-case="+DoorCase+" case2="+Case2+" case3="+Case3
                    +" lookout="+Lookout+" armoury-gate="+ArmouryGate+" stair-mid="+StairMid+" cell="+CellAt+" launch="+LaunchPad);
                Debug.Log("HARROWGATE_TOWN_OK");
            }

            // ---------------------------------------------------------------- parapets: every drop a body could walk off has one
            /// <summary>The town's parapets and kerbs: plan line, height over the floor, and how wide. Built pieces on static boxes, so a body stops at something drawn and the bake reads it.</summary>
            static readonly (string name,Vector2 a,Vector2 b,float tall,float wide)[] ParapetLines=
            {
                ("Quay kerb",new(-54.4f,-5.4f),new(49.4f,-5.4f),.6f,.7f),("Quay kerb",new(49.4f,-5.4f),new(49.4f,22.4f),.6f,.7f),
                ("Gate terrace parapet",new(-54.4f,23.3f),new(1.3f,23.3f),.75f,.6f),("Gate terrace parapet",new(14.7f,23.3f),new(47.4f,23.3f),.75f,.6f),
                ("Harbour wall",new(47.4f,22.6f),new(47.4f,78.5f),1.05f,.7f),("Harbour wall",new(47.4f,82),new(47.4f,112.4f),1.05f,.7f),
                ("Upper terrace parapet",new(-54.4f,59.3f),new(-41.6f,59.3f),.75f,.6f),("Upper terrace parapet",new(-35.4f,59.3f),new(-4.6f,59.3f),.75f,.6f),
                ("Upper terrace parapet",new(4.6f,59.3f),new(39.6f,59.3f),.75f,.6f),("Upper terrace parapet",new(45.4f,59.3f),new(47,59.3f),.75f,.6f),
                ("Ramp parapet",new(-41.4f,59.4f),new(-41.4f,64.4f),.75f,.5f),("Ramp parapet",new(-35.6f,59.4f),new(-35.6f,64.4f),.75f,.5f),
                ("Ramp parapet",new(-4.4f,59.4f),new(-4.4f,64.4f),.75f,.5f),("Ramp parapet",new(4.4f,59.4f),new(4.4f,64.4f),.75f,.5f),
                ("Ramp parapet",new(39.6f,59.4f),new(39.6f,64.4f),.75f,.5f),("Ramp parapet",new(45.4f,59.4f),new(45.4f,64.4f),.75f,.5f),
                ("Gate ramp parapet",new(3.6f,14.6f),new(3.6f,20.2f),.7f,.5f),("Gate ramp parapet",new(12.4f,14.6f),new(12.4f,20.2f),.7f,.5f),
                ("Podium parapet",new(-15.6f,105.8f),new(-15.6f,124.6f),.8f,.5f),("Podium parapet",new(15.6f,105.8f),new(15.6f,124.6f),.8f,.5f),
                ("Podium parapet",new(-15.6f,105.8f),new(-14.2f,105.8f),.8f,.5f),("Podium parapet",new(14.2f,105.8f),new(15.6f,105.8f),.8f,.5f),
                ("Steps parapet",new(-14.3f,98.8f),new(-14.3f,105.6f),.6f,.5f),("Steps parapet",new(14.3f,98.8f),new(14.3f,105.6f),.6f,.5f),
                ("Stair parapet",new(53,78.6f),new(53,96.6f),1,.6f),("Landing kerb",new(53,96.4f),new(63.9f,96.4f),.6f,.6f),("Landing kerb",new(63.9f,96.4f),new(63.9f,111.8f),.6f,.6f),
                ("Bastion parapet",new(47.4f,112.7f),new(63.9f,112.7f),1.05f,.7f),("Bastion parapet",new(63.9f,112.7f),new(63.9f,125.9f),1.05f,.7f),("Bastion parapet",new(38.4f,125.9f),new(63.9f,125.9f),1.05f,.7f),
                // The muster field's kerb: knee-high, open to the south, west, east and north (the hall steps): the duel's four seals.
                ("Field kerb",new(-FieldHalfX-.35f,FieldSouth-.35f),new(-SouthOpen,FieldSouth-.35f),KerbTall,.6f),("Field kerb",new(SouthOpen,FieldSouth-.35f),new(FieldHalfX+.35f,FieldSouth-.35f),KerbTall,.6f),
                ("Field kerb",new(-FieldHalfX-.35f,FieldNorth+.35f),new(-SouthOpen,FieldNorth+.35f),KerbTall,.6f),("Field kerb",new(SouthOpen,FieldNorth+.35f),new(FieldHalfX+.35f,FieldNorth+.35f),KerbTall,.6f),
                ("Field kerb",new(-FieldHalfX-.35f,FieldSouth-.35f),new(-FieldHalfX-.35f,85-SideOpen),KerbTall,.6f),("Field kerb",new(-FieldHalfX-.35f,85+SideOpen),new(-FieldHalfX-.35f,FieldNorth+.35f),KerbTall,.6f),
                ("Field kerb",new(FieldHalfX+.35f,FieldSouth-.35f),new(FieldHalfX+.35f,85-SideOpen),KerbTall,.6f),("Field kerb",new(FieldHalfX+.35f,85+SideOpen),new(FieldHalfX+.35f,FieldNorth+.35f),KerbTall,.6f),
            };
            static void Parapets(ZoneKit kit)
            {
                var holder=new GameObject("Parapets");var shape=new ZoneKit.Shape();Color face=new(.7f,.68f,.66f),cap=new(.86f,.84f,.8f),kerb=new(.62f,.6f,.58f);
                foreach(var (name,a,b,tall,wide) in ParapetLines)
                {
                    float length=Vector2.Distance(a,b);int pieces=Mathf.Max(1,Mathf.CeilToInt(length/2.2f));var dir=(b-a)/length;var across=new Vector2(-dir.y,dir.x)*wide*.5f;
                    for(int k=0;k<pieces;k++)
                    {
                        var p0=a+(b-a)*(k/(float)pieces);var p1=a+(b-a)*((k+1)/(float)pieces);var mid=(p0+p1)*.5f;
                        // A piece on a ramp's side or a sloping stair stands from the lowest floor under it to its height over the highest: no gap under it, no step on its top.
                        float low=float.PositiveInfinity,high=float.NegativeInfinity;
                        foreach(var q in new[]{p0+across,p0-across,p1+across,p1-across,mid}){float y=kit.Y(q.x,q.y);if(Land(q)<0)continue;low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
                        if(float.IsInfinity(low))continue;
                        float bottom=low-.4f,top=high+tall;var centre=new Vector3(mid.x,(bottom+top)*.5f,mid.y);var turn=Quaternion.LookRotation(new Vector3(dir.x,0,dir.y));
                        bool low_=tall<.7f;
                        shape.Slot(0).Box(centre,new Vector3(wide,top-bottom,Vector2.Distance(p0,p1)+.02f),turn,low_?kerb:face,null,.2f);
                        shape.Slot(0).Box(new Vector3(mid.x,top+.05f,mid.y),new Vector3(wide+.12f,.1f,Vector2.Distance(p0,p1)+.02f),turn,cap,cap,.2f);
                        ZoneKit.Block(holder,new Vector3(mid.x,top-.9f,mid.y),new Vector3(wide,1.8f,Vector2.Distance(p0,p1)),Mathf.Atan2(dir.x,dir.y)*Mathf.Rad2Deg,name);
                    }
                }
                kit.Built("Parapets drawn",shape,Stone).transform.SetParent(holder.transform,true);
            }

            // ---------------------------------------------------------------- built pieces the town uses everywhere
            /// <summary>A house of the town's own (HarrowgateHouse, the canyon house cut in basalt): its round door to the south, as every front here faces the camera.</summary>
            static GameObject House(ZoneKit kit,string name,Vector2 at,float size,float yaw,Color tint)
            {
                var house=kit.Prop(ZoneKit.Kit("HarrowgateHouse","CanyonHouse"),new Vector3(at.x,0,at.y),size,yaw);house.name=name;house.AddComponent<PropTint>().tint=tint;return house;
            }
            /// <summary>A lamp on a bracket or a post: a lit lantern drawn, and a light only where <paramref name="light"/> says (a floor renderer takes six; past that, the ground kit paints a pool).</summary>
            static void Lantern(ZoneKit kit,ZoneKit.Shape shape,Vector3 at,bool light,float range=8,float intensity=1.3f,int lampSlot=2,int capSlot=1)
            {
                shape.Slot(lampSlot).Lump(at,new Vector3(.2f,.28f,.2f),(int)(at.x*3+at.z),.04f,Color.white,Color.white,8,5);
                shape.Slot(capSlot).Box(at+Vector3.up*.33f,new Vector3(.34f,.06f,.34f),Quaternion.identity,new Color(.3f,.3f,.32f));
                if(light){var l=kit.Lamp(new Vector3(at.x,0,at.z),range,intensity,new Color(1,.8f,.52f));l.transform.position=at;}
            }
            /// <summary>A banner of the Muster's: dark crimson cloth hung in folds from a bar with knobbed ends, a darker hem, a gold fringe on its pointed tail and the iron-grey crest-sail on
            /// its face (<see cref="Hang"/>; one flat plane until November 21, s53/014).</summary>
            static void Banner(ZoneKit.Shape shape,Vector3 top,float wide,float tall,Vector3 facing,int barSlot,int clothSlot)
            {
                var across=Vector3.Cross(Vector3.up,facing).normalized*wide*.5f;
                shape.Slot(barSlot).Beam(top-across*1.15f,top+across*1.15f,.08f,.08f,new Color(.3f,.28f,.26f));
                foreach(int s in new[]{-1,1})shape.Slot(barSlot).Box(top+across*1.2f*s,new Vector3(.13f,.13f,.13f),Quaternion.LookRotation(facing),new Color(.62f,.5f,.32f));
                Hang(shape,top+Vector3.down*.04f+facing.normalized*.03f,wide,tall*.9f,facing,clothSlot,new Color(.48f,.11f,.11f),new Color(.64f,.64f,.66f),new Color(.3f,.07f,.07f),new Color(.78f,.62f,.34f));
            }
            static Vector3 At(ZoneKit kit,Vector2 p,float up=0)=>kit.On(p.x,p.y,up);

            // ---------------------------------------------------------------- the quay
            /// <summary>The quay: the road's arch in the west crag, the landing pad, dock stores against the gate terrace, the harbour scales and two cranes over the water, bollards and moored hulls.</summary>
            static void Quayside(ZoneKit kit)
            {
                Color wood=new(.8f,.68f,.5f),dark=new(.42f,.38f,.36f),stone=new(.72f,.7f,.68f);
                // ---- the road arch: a gatehouse cut into the crag, its doors shut; the road to the Flows is behind them.
                var arch=new ZoneKit.Shape();var archFoot=At(kit,RoadArch);
                foreach(int side in new[]{-1,1})arch.Slot(0).Box(archFoot+new Vector3(-.4f,3,side*3.4f),new Vector3(2.4f,6,1.6f),Quaternion.identity,stone,null,.2f);
                arch.Slot(0).Box(archFoot+new Vector3(-.4f,6.5f,0),new Vector3(2.6f,1.2f,8.4f),Quaternion.identity,stone,new Color(.84f,.82f,.78f),.2f);
                arch.Slot(1).Box(archFoot+new Vector3(-.9f,2.6f,0),new Vector3(.3f,5.2f,5.2f),Quaternion.identity,wood,null,.5f);
                for(int k=0;k<3;k++)arch.Slot(2).Box(archFoot+new Vector3(-.72f,1+k*1.6f,0),new Vector3(.08f,.16f,5),Quaternion.identity,dark);
                Banner(arch,archFoot+new Vector3(.5f,5.8f,0),2.2f,3.2f,Vector3.right,2,3);
                var archBody=kit.Built("Road arch",arch,Stone,Timber,Iron,Cloth);ZoneKit.Block(archBody,archFoot+new Vector3(-.4f,3,0),new Vector3(2.6f,6,8.4f),0,"Road arch");
                var road=new GameObject("The road to the Flows");road.transform.position=archFoot+Vector3.up*.2f;archBody.transform.SetParent(road.transform,true);
                kit.Way(road,"Road — The Scoria Flows",ScoriaZone,"Harrowgate",null,null,3.8f,new Vector2(RoadArch.x+4.4f,RoadArch.y-4.2f));

                // ---- the landing pad: a ring of dressed stone on the quay with mooring posts and a light at each quarter
                var padAt=At(kit,Pad);var pad=new ZoneKit.Shape();
                pad.Slot(0).Disc(padAt+Vector3.up*.06f,Vector3.up,3,28,new Color(.58f,.56f,.55f),0,new Color(.66f,.64f,.62f),.25f).Tube(new[]{padAt+Vector3.down*.1f,padAt+Vector3.up*.06f},new[]{3.06f,3},28,dark,dark,8,1,false);
                pad.Slot(1).Disc(padAt+Vector3.up*.065f,Vector3.up,2.4f,28,new Color(.86f,.82f,.72f),2.15f).Disc(padAt+Vector3.up*.065f,Vector3.up,.45f,14,new Color(.86f,.82f,.72f));
                for(int k=0;k<4;k++){float a=k*Mathf.PI*.5f+.785f;var q=padAt+new Vector3(Mathf.Cos(a)*3.3f,0,Mathf.Sin(a)*3.3f);pad.Slot(2).Post(q,.8f,.16f,.12f,8,new Color(.34f,.33f,.33f),new Color(.5f,.48f,.48f));Lantern(kit,pad,q+Vector3.up*1.05f,false,8,1.3f,3,2);}
                var padBody=kit.Built("Landing pad drawn",pad,Stone,Cloth,Iron,Lamp);ZoneKit.Block(padBody,padAt+Vector3.down*.02f,new Vector3(4.2f,.16f,4.2f),0,"Pad deck");
                var point=new GameObject("Landing pad",typeof(DockingPad));point.transform.position=padAt;padBody.transform.SetParent(point.transform,true);
                var dock=point.GetComponent<DockingPad>();dock.prompt="Launch — The Picket of Teeth";dock.range=4;dock.scene=PicketZone;dock.spawn="Harrowgate";

                // ---- dock stores against the gate terrace's wall, fronts to the quay
                House(kit,"Dock store",new Vector2(-32,18.2f),5.2f,180,new Color(.86f,.84f,.82f));House(kit,"Dock store",new Vector2(-21,18.6f),5.6f,180,new Color(.8f,.8f,.82f));
                House(kit,"Harbourmaster's house",new Vector2(26,18.2f),5.4f,180,new Color(.84f,.8f,.78f));

                // ---- the harbour scales: a beam on a post, pans on chains, sacks and a tally board
                var scales=new ZoneKit.Shape();var foot=At(kit,Scales+new Vector2(1.6f,1.6f));
                scales.Slot(0).Post(foot,3.6f,.18f,.14f,8,dark,new Color(.56f,.52f,.5f)).Beam(foot+new Vector3(-2.2f,3.5f,0),foot+new Vector3(2.2f,3.5f,0),.16f,.16f,dark);
                foreach(int side in new[]{-1,1}){var hang=foot+new Vector3(side*2,3.4f,0);scales.Slot(1).Rope(hang,hang+Vector3.down*2,0,.02f,dark,4,2);scales.Slot(0).Disc(hang+Vector3.down*2.05f,Vector3.up,.55f,14,new Color(.6f,.5f,.36f));}
                var sackLump=new Color(.86f,.82f,.72f);
                scales.Slot(2);foreach(var (dx,dz,s) in new[]{(-1.6f,-.4f,.5f),(-2.2f,.4f,.46f),(-1.1f,.6f,.44f),(2.4f,.2f,.5f),(2.9f,-.5f,.42f)})Sack(scales,foot+new Vector3(dx,0,dz),s*1.9f,ZoneKit.Shape.Dim(sackLump,.92f+.08f*Mathf.Abs(dx%1)),(int)(dx*10)+20);
                scales.Slot(3).Box(foot+new Vector3(0,.5f,1.4f),new Vector3(2.4f,1,.7f),Quaternion.identity,wood,new Color(.88f,.78f,.6f),.5f);
                var scalesBody=kit.Built("Harbour scales",scales,Timber,Iron,Burlap,Timber);ZoneKit.Block(scalesBody,foot+new Vector3(0,.8f,.4f),new Vector3(6.2f,1.6f,2.4f),0,"Harbour scales");

                // ---- cranes on the quay's east edge, jibs out over the water
                Crane(kit,"Quay crane",new Vector3(44.2f,Quay,1.6f),14,80,new Color(.42f,.38f,.36f));Crane(kit,"Quay crane",new Vector3(44.2f,Quay,15.2f),12,100,new Color(.46f,.4f,.36f));
                // ---- bollards along the sea edges, and the quay's clutter: crates of the Muster's, coils, a cart
                var gear=new ZoneKit.Shape();var gearBlocks=new GameObject("Quay gear");
                for(float x=-46;x<46;x+=7.5f){var b=At(kit,new Vector2(x,-4.4f));gear.Slot(0).Post(b,.7f,.22f,.18f,8,dark,new Color(.5f,.48f,.48f));ZoneKit.Block(gearBlocks,b+Vector3.up*.35f,new Vector3(.44f,.7f,.44f),0,"Bollard");}
                for(float z=-2;z<20;z+=6.5f){var b=At(kit,new Vector2(48.2f,z));gear.Slot(0).Post(b,.7f,.22f,.18f,8,dark,new Color(.5f,.48f,.48f));ZoneKit.Block(gearBlocks,b+Vector3.up*.35f,new Vector3(.44f,.7f,.44f),0,"Bollard");}
                kit.Built("Bollards",gear,Iron).transform.SetParent(gearBlocks.transform,true);
                foreach(var (x,z,yaw,size) in new[]{(-44f,15.6f,10f,1.3f),(-42.4f,16.4f,-20f,1.1f),(-12.6f,17.8f,5f,1.2f),(37.6f,17f,30f,1.3f),(39.4f,16.2f,-10f,1.1f),(18f,-3.2f,15f,1.1f),(-34f,-3.4f,-10f,1.2f)})
                    kit.Prop(ZoneKit.Kit("MusterCrate","DressCargoPallet"),new Vector3(x,0,z),size,yaw);
            }
            /// <summary>A harbour crane: a lattice tower on four legs, a cab, a jib slung out over the water and a hook on its line.</summary>
            static void Crane(ZoneKit kit,string name,Vector3 foot,float tall,float yaw,Color paint,float jib=12,bool solid=true)
            {
                var shape=new ZoneKit.Shape();var turn=Quaternion.Euler(0,yaw,0);float half=1.2f;
                var legs=new[]{new Vector3(-half,0,-half),new Vector3(half,0,-half),new Vector3(half,0,half),new Vector3(-half,0,half)};
                for(int k=0;k<4;k++){var a=foot+turn*legs[k];var top=foot+turn*(legs[k]*.55f)+Vector3.up*tall;shape.Slot(0).Beam(a,top,.22f,.22f,paint);
                    var b=foot+turn*legs[(k+1)%4];var bTop=foot+turn*(legs[(k+1)%4]*.55f)+Vector3.up*tall;
                    for(float t=0;t<.95f;t+=.2f){shape.Slot(0).Beam(Vector3.Lerp(a,top,t),Vector3.Lerp(bTop,b,1-t-.2f),.08f,.08f,ZoneKit.Shape.Dim(paint,.9f));}}
                var head=foot+Vector3.up*tall;
                shape.Slot(1).Box(head+Vector3.up*1.1f,new Vector3(2.4f,2.2f,2.6f),turn,ZoneKit.Shape.Dim(paint,1.15f),new Color(.4f,.38f,.38f),.4f);
                var tip=head+turn*new Vector3(0,2.6f,jib);var tail=head+turn*new Vector3(0,1.6f,-4);
                shape.Slot(0).Beam(head+Vector3.up*1.6f,tip,.4f,.5f,paint).Beam(tail,head+Vector3.up*1.6f,.4f,.4f,paint).Box(tail+Vector3.down*.4f,new Vector3(1.6f,1.2f,1.4f),turn,new Color(.3f,.3f,.32f));
                shape.Slot(2).Rope(head+Vector3.up*4.2f,tip,0,.03f,new Color(.2f,.2f,.2f),4,2).Rope(tip,tip+Vector3.down*(tall-1),0,.03f,new Color(.2f,.2f,.2f),4,2);
                shape.Slot(1).Box(tip+Vector3.down*(tall-.4f),new Vector3(.5f,.6f,.3f),turn,new Color(.3f,.3f,.32f));
                var body=kit.Built(name,shape,Iron,Iron,Rope);ZoneKit.Fadeable(body);
                if(solid)foreach(var leg in legs)ZoneKit.Block(body,foot+turn*leg+Vector3.up*1,new Vector3(.5f,2,.5f),yaw,"Crane leg");
            }

            // ---------------------------------------------------------------- the harbour gate
            /// <summary>The town's front gate: two towers on the gate terrace's wall, an arch over the ramp, the Muster's banners, the leaves shut until a parley flag is vouched, and the
            /// guard's door in the east tower that Vessa goes in by.</summary>
            static void HarbourGate(ZoneKit kit)
            {
                Color stone=new(.7f,.68f,.66f),cap=new(.84f,.82f,.78f),wood=new(.62f,.5f,.38f),iron=new(.34f,.33f,.34f);
                var shape=new ZoneKit.Shape();var body=new GameObject("Harbour gate");
                foreach(float cx in new[]{3f,13f})
                {
                    var foot=new Vector3(cx,Quay,22);
                    shape.Slot(0).Box(foot+new Vector3(0,3.6f,0),new Vector3(3,7.2f,5),Quaternion.identity,stone,null,.2f).Box(foot+new Vector3(0,7.45f,0),new Vector3(3.4f,.5f,5.4f),Quaternion.identity,cap,cap,.2f);
                    for(int k=-1;k<=1;k+=2)shape.Slot(0).Box(foot+new Vector3(k*1.1f,8.1f,-2.3f),new Vector3(.8f,.8f,.6f),Quaternion.identity,stone,cap,.2f);
                    Banner(shape,foot+new Vector3(0,6.8f,-2.6f),1.8f,3.6f,Vector3.back,1,2);
                    ZoneKit.Block(body,foot+new Vector3(0,3.6f,0),new Vector3(3,7.2f,5),0,"Gate tower");
                }
                // The guard's door in the east tower's south face: a framed doorway with its leaf open and the lit passage behind (a flat black rectangle until November 21, s50/052).
                GuardDoor(kit,body.transform);
                // The arch over the ramp, its soffit high enough to see under.
                shape.Slot(0).Box(new Vector3(8,6.2f,22),new Vector3(7,1.8f,3.2f),Quaternion.identity,stone,cap,.2f).Box(new Vector3(8,7.2f,22),new Vector3(7.6f,.3f,3.6f),Quaternion.identity,cap,cap,.2f);
                Banner(shape,new Vector3(8,6.9f,20.3f),3,2,Vector3.back,1,2);
                // Three slots: with the guard's door drawn on its own, a fourth material (Dark) would draw again over the cloth's slot, and the banners came out black.
                var drawn=kit.Built("Harbour gate drawn",shape,Stone,Timber,Cloth);drawn.transform.SetParent(body.transform,true);ZoneKit.Fadeable(drawn);
                var fade=drawn.GetComponent<SceneryOcclusion>();if(fade!=null)fade.opening=new Bounds(new Vector3(8,2.6f,22),new Vector3(7.4f,5.2f,4));
                // The leaves: shut across the ramp until the flag is vouched (solid, and carving the paths while they stand), then swung back against the towers.
                // The shut leaves are a seal (a Membrane, as Many Hands' cargo door is): solid and carving while shut, and the world tests open every seal. As a plain blocker it made the
                // whole town unreachable to them (EveryPromptCanBeReachedOnFoot, the first shared run).
                var shut=WorldBuilder.Membrane("Harbour gate leaves",new Vector3(8,kit.Y(8,22.4f)+2.1f,22.4f),8,false,4.2f);
                foreach(var f in shut.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(f);foreach(var r in shut.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(r);
                var leaves=new ZoneKit.Shape();
                foreach(int side in new[]{-1,1})
                {
                    var c=new Vector3(8+side*1.95f,0,22.4f);c.y=kit.Y(c.x,c.z)+2.1f;
                    leaves.Slot(0).Box(c,new Vector3(3.9f,4.2f,.24f),Quaternion.identity,wood,null,.5f);
                    for(int k=0;k<3;k++)leaves.Slot(1).Box(c+new Vector3(0,-1.4f+k*1.4f,-.14f),new Vector3(3.9f,.2f,.06f),Quaternion.identity,iron);
                }
                kit.Built("Harbour gate leaves drawn",leaves,Timber,Iron).transform.SetParent(shut.transform,true);
                var stop=shut.GetComponent<BoxCollider>();
                var carve=shut.gameObject.AddComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.center=stop.center;carve.size=stop.size+new Vector3(0,0,.6f);carve.carving=true;carve.carveOnlyStationary=false;shut.SetOpen(false);
                var open=new GameObject("Harbour gate leaves, open");var swung=new ZoneKit.Shape();
                foreach(int side in new[]{-1,1})
                {
                    var c=new Vector3(8+side*3.82f,0,24.2f);c.y=kit.Y(c.x,c.z)+2.1f;
                    swung.Slot(0).Box(c,new Vector3(.24f,4.2f,3.6f),Quaternion.identity,wood,null,.5f);
                    for(int k=0;k<3;k++)swung.Slot(1).Box(c+new Vector3(-side*.14f,-1.4f+k*1.4f,0),new Vector3(.06f,.2f,3.6f),Quaternion.identity,iron);
                }
                kit.Built("Harbour gate leaves open",swung,Timber,Iron).transform.SetParent(open.transform,true);
                Gate("The harbour gate is shut until the flag is vouched","vask.parley",false,shut.gameObject);Gate("The harbour gate stands open","vask.parley",true,open);
                // Lanterns either side of the arch, one of them lit.
                var lamps=new ZoneKit.Shape();Lantern(kit,lamps,new Vector3(4.9f,4.2f,19.2f),true,9,1.2f);Lantern(kit,lamps,new Vector3(11.1f,4.2f,19.2f),false);
                lamps.Slot(0).Beam(new Vector3(4.6f,4.6f,19.4f),new Vector3(4.9f,4.6f,19.2f),.06f,.06f,iron);
                kit.Built("Gate lanterns",lamps,Iron,Iron,Lamp).transform.SetParent(body.transform,true);
                // The party's parley flag, planted beside the ramp's foot where they ask to be let in.
                ParleyFlag(kit,ParleyAt);
            }
            /// <summary>Shows its targets while a flag is set (or while it isn't), from an always-active object of its own.</summary>
            static FlagGate Gate(string name,string flag,bool showWhenSet,params GameObject[] targets)
            {
                var gate=new GameObject(name,typeof(FlagGate)).GetComponent<FlagGate>();gate.flag=flag;gate.showWhenSet=showWhenSet;gate.targets=targets;
                // As the town stands before the flag: what shows while it is unset is up (the stills and the bake see the town as a player first does).
                foreach(var t in targets)t.SetActive(!showWhenSet);
                return gate;
            }
            /// <summary>Shown only between two flags: the outer holder from <paramref name="from"/>, the inner one hidden from <paramref name="until"/>.</summary>
            static GameObject Between(string name,string from,string until)
            {
                var outer=new GameObject(name);var inner=new GameObject(name+", until "+until);inner.transform.SetParent(outer.transform,false);
                Gate(name+" (from)",from,true,outer);Gate(name+" (until)",until,false,inner);return inner;
            }

            // ---------------------------------------------------------------- the lower terrace: the family quarter
            /// <summary>Homes against the upper terrace's wall, fronts to the walk; washing on lines across the yards (grey sashes, the stair's rota); a bench and pots at the doors; the family
            /// stair up to the cadet yard, where the small ones play parley.</summary>
            static void FamilyQuarter(ZoneKit kit)
            {
                House(kit,"House on the family quarter",new Vector2(-46.5f,51.2f),6,180,new Color(.9f,.86f,.82f));
                House(kit,"House on the family quarter",new Vector2(-12.6f,51.6f),5.6f,180,new Color(.84f,.84f,.86f));
                House(kit,"House on the family quarter",new Vector2(9.4f,51.4f),5.4f,180,new Color(.88f,.84f,.8f));
                var lines=new ZoneKit.Shape();var poles=new GameObject("Washing poles");
                void Line(Vector2 a,Vector2 b,float high,int seed)
                {
                    var rng=new System.Random(seed);var pa=At(kit,a);var pb=At(kit,b);
                    foreach(var p in new[]{pa,pb}){lines.Slot(0).Post(p,high+.2f,.07f,.06f,6,new Color(.42f,.36f,.3f),new Color(.6f,.52f,.42f));ZoneKit.Block(poles,p+Vector3.up*1,new Vector3(.2f,2,.2f),0,"Washing pole");}
                    var top0=pa+Vector3.up*high;var top1=pb+Vector3.up*high;lines.Slot(1).Rope(top0,top1,.35f,.015f,new Color(.82f,.78f,.68f),4,10);
                    float length=Vector3.Distance(top0,top1);var along=(top1-top0).normalized;var face=Vector3.Cross(along,Vector3.up).normalized;
                    Color[] wash={new(.56f,.56f,.58f),new(.6f,.6f,.62f),new(.88f,.86f,.8f),new(.5f,.54f,.6f),new(.72f,.62f,.48f),new(.56f,.56f,.58f),new(.84f,.78f,.7f)};
                    for(float s=.6f;s<length-.6f;s+=.5f+(float)rng.NextDouble()*.5f)
                    {
                        float t=s/length;var at=Vector3.Lerp(top0,top1,t)+Vector3.down*.35f*4*t*(1-t);float wide=.35f+(float)rng.NextDouble()*.5f,drop=.5f+(float)rng.NextDouble()*.7f;
                        // The stair's grey sashes are long and narrow; the rest is shirts and cloths.
                        bool sash=rng.Next(3)==0;if(sash){wide=.18f;drop=1.1f;}
                        var tone=sash?new Color(.54f,.55f,.57f):wash[rng.Next(wash.Length)];
                        lines.Slot(2).Quad(at-along*wide*.5f,at+along*wide*.5f,at+along*wide*.5f+Vector3.down*drop,at-along*wide*.5f+Vector3.down*drop,face,tone,.6f);
                        s+=wide;
                    }
                }
                Line(new Vector2(-51.2f,45.6f),new Vector2(-42.2f,46.2f),2.6f,3);Line(new Vector2(-16.2f,46.4f),new Vector2(-6.8f,46),2.7f,5);Line(new Vector2(4.8f,46.4f),new Vector2(12.6f,46.6f),2.6f,7);
                var lineBody=kit.Built("Washing lines",lines,Timber,Rope,Cloth);lineBody.transform.SetParent(poles.transform,true);lineBody.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.On;
                // At the doors: a bench, pots of herbs, a water butt; a cook-pot on a fire-ring in front of the west house.
                var doors=new ZoneKit.Shape();var doorBlocks=new GameObject("Doorsteps");
                void Bench(Vector2 at,float yaw){var c=At(kit,at,.24f);var t=Quaternion.Euler(0,yaw,0);doors.Slot(0).Box(c,new Vector3(1.6f,.08f,.42f),t,new Color(.72f,.6f,.44f)).Box(c+t*new Vector3(-.6f,-.12f,0),new Vector3(.1f,.24f,.36f),t,new Color(.5f,.42f,.32f)).Box(c+t*new Vector3(.6f,-.12f,0),new Vector3(.1f,.24f,.36f),t,new Color(.5f,.42f,.32f));ZoneKit.Block(doorBlocks,c,new Vector3(1.6f,.5f,.45f),yaw,"Bench");}
                void Pot(Vector2 at,float s,Color plant){var c=At(kit,at);doors.Slot(1).Tube(new[]{c,c+Vector3.up*s},new[]{s*.42f,s*.5f},10,new Color(.5f,.36f,.3f),new Color(.6f,.44f,.36f),2,1,false).Slot(2).Lump(c+Vector3.up*s,new Vector3(s*.5f,s*.4f,s*.5f),(int)(at.x*5),.3f,plant,ZoneKit.Shape.Dim(plant,.6f),8,4);ZoneKit.Block(doorBlocks,c+Vector3.up*s*.5f,new Vector3(s,s,s),0,"Pot");}
                Bench(new Vector2(-40.8f,47.4f),0);Bench(new Vector2(-8.2f,47.4f),0);
                Pot(new Vector2(-51.4f,46.4f),.5f,new Color(.5f,.66f,.38f));Pot(new Vector2(-17,47.6f),.45f,new Color(.62f,.7f,.4f));Pot(new Vector2(5.4f,47.8f),.5f,new Color(.46f,.62f,.42f));Pot(new Vector2(-6.4f,47.8f),.4f,new Color(.7f,.6f,.36f));
                var fire=At(kit,new Vector2(-40.6f,42.6f));
                for(int k=0;k<7;k++){float a=k*Mathf.PI*2/7;doors.Slot(3).Lump(fire+new Vector3(Mathf.Cos(a)*.55f,.1f,Mathf.Sin(a)*.55f),new Vector3(.16f,.12f,.14f),k,.2f,new Color(.4f,.4f,.42f),new Color(.3f,.3f,.32f),6,3);}
                doors.Slot(4).Tube(new[]{fire+Vector3.up*.25f,fire+Vector3.up*.75f},new[]{.32f,.36f},10,new Color(.22f,.2f,.2f),new Color(.3f,.28f,.28f),1,1,false);
                ZoneKit.Block(doorBlocks,fire+Vector3.up*.4f,new Vector3(1.2f,.8f,1.2f),0,"Fire-ring");
                kit.Built("Doorsteps",doors,Timber,Stone,Cloth,Stone,Iron).transform.SetParent(doorBlocks.transform,true);
            }

            // ---------------------------------------------------------------- Odrun's kitchen and the quarter behind it
            /// <summary>A walled yard with no window on the street: a low wall and a narrow door to the walk, a cook-room under a lean-to along its back, the range, the long table and
            /// its forty bowls; through a curtain at the back, the Unsworn's quarter against the upper terrace's wall. No sign: it doesn't exist.</summary>
            static void Kitchen(ZoneKit kit)
            {
                Color wall=new(.7f,.66f,.62f),cap=new(.84f,.8f,.74f),wood=new(.7f,.56f,.4f),dark=new(.42f,.36f,.3f);
                var shape=new ZoneKit.Shape();var body=new GameObject("Odrun's kitchen");float y=Lower;
                Vector3 P(float x,float z,float up)=>new(x,y+up,z);
                // The yard's walls: low to the street (a door gap at x -26..-24), head-high at the sides, the cook-room's back wall tall.
                void Wall(Vector2 a,Vector2 b,float tall,float wide=.4f)
                {
                    var mid=(a+b)*.5f;float length=Vector2.Distance(a,b);var dir=(b-a)/length;var turn=Quaternion.LookRotation(new Vector3(dir.x,0,dir.y));
                    shape.Slot(0).Box(P(mid.x,mid.y,tall*.5f-.2f),new Vector3(wide,tall+.4f,length),turn,wall,null,.2f).Box(P(mid.x,mid.y,tall+.05f),new Vector3(wide+.1f,.1f,length),turn,cap,cap,.2f);
                    ZoneKit.Block(body,P(mid.x,mid.y,tall*.5f),new Vector3(wide,tall,length),Mathf.Atan2(dir.x,dir.y)*Mathf.Rad2Deg,"Kitchen wall");
                }
                Wall(new Vector2(-31,40),new Vector2(-26.1f,40),1.2f);Wall(new Vector2(-23.9f,40),new Vector2(-19,40),1.2f);
                Wall(new Vector2(-31,40),new Vector2(-31,57),2.2f);Wall(new Vector2(-19,40),new Vector2(-19,57),2.2f);
                Wall(new Vector2(-31,49.2f),new Vector2(-25.6f,49.2f),3.8f,.5f);Wall(new Vector2(-23.2f,49.2f),new Vector2(-19,49.2f),3.8f,.5f);
                // The door: two posts and a lintel over the gap; a plain plank leaf stood open.
                shape.Slot(1).Box(P(-26.1f,40,1.2f),new Vector3(.2f,2.4f,.3f),Quaternion.identity,dark).Box(P(-23.9f,40,1.2f),new Vector3(.2f,2.4f,.3f),Quaternion.identity,dark).Box(P(-25,40,2.45f),new Vector3(2.5f,.2f,.34f),Quaternion.identity,dark)
                    .Box(P(-26.6f,40.9f,1.05f),new Vector3(.1f,2.1f,1.6f),Quaternion.Euler(0,-20,0),wood,null,.5f);
                // The lean-to over the cook-room, the range against the back wall with its pot and flue, and a curtain over the way through to the quarter.
                foreach(float x in new[]{-30.4f,-26.4f,-22.4f,-19.6f})shape.Slot(1).Post(P(x,46.1f,0),2.9f,.1f,.09f,6,dark,wood);
                shape.Slot(2).Quad(P(-31.2f,45.7f,2.85f),P(-18.8f,45.7f,2.85f),P(-18.8f,49.4f,3.9f),P(-31.2f,49.4f,3.9f),Vector3.up+Vector3.back*.3f,new Color(.62f,.6f,.62f),.5f);
                var range=P(-21.6f,48.2f,0);
                shape.Slot(0).Box(range+Vector3.up*.55f,new Vector3(2.2f,1.1f,1.1f),Quaternion.identity,new Color(.5f,.46f,.44f),new Color(.36f,.32f,.3f),.3f);
                shape.Slot(3).Box(range+new Vector3(0,.7f,-.56f),new Vector3(1.2f,.4f,.04f),Quaternion.identity,Color.white);
                shape.Slot(4).Tube(new[]{range+Vector3.up*1.1f,range+Vector3.up*1.55f},new[]{.36f,.4f},12,new Color(.24f,.22f,.22f),new Color(.34f,.32f,.3f),1,1,false)
                    .Tube(new[]{range+new Vector3(.6f,1.1f,.3f),range+new Vector3(.6f,4.6f,.3f)},new[]{.2f,.18f},8,new Color(.3f,.28f,.28f),new Color(.3f,.28f,.28f),1,1);
                shape.Slot(5).Quad(P(-25.6f,49.0f,0),P(-23.2f,49.0f,0),P(-23.2f,49.0f,2.6f),P(-25.6f,49.0f,2.6f),Vector3.back,new Color(.56f,.32f,.26f),.5f);
                // Shelves of crocks and a ladle rack on the back wall.
                for(int k=0;k<2;k++){shape.Slot(1).Box(P(-28.4f,48.9f,1.4f+k*.6f),new Vector3(2.8f,.06f,.3f),Quaternion.identity,wood);for(int j=0;j<6;j++)shape.Slot(4).Tube(new[]{P(-29.6f+j*.46f,48.9f,1.44f+k*.6f),P(-29.6f+j*.46f,48.9f,1.7f+k*.6f)},new[]{.12f,.14f},8,new Color(.6f,.46f,.36f),new Color(.7f,.56f,.42f),1,1);}
                // Two long tables along the side walls, a bench each, forty bowls on them (nobody's); the walk from the door to the range and the curtain left clear between them.
                foreach(var (cx,z0,z1,bench) in new[]{(-28.9f,40.8f,46f,.85f),(-21.2f,40.6f,45f,-1.2f)})
                {
                    var c=P(cx,(z0+z1)*.5f,.78f);float length=z1-z0;
                    shape.Slot(1).Box(c,new Vector3(1,.1f,length),Quaternion.identity,wood,new Color(.82f,.7f,.52f),.5f);
                    foreach(var (dx,dz) in new[]{(-.4f,-length*.45f),(.4f,-length*.45f),(-.4f,length*.45f),(.4f,length*.45f)})shape.Slot(1).Box(c+new Vector3(dx,-.4f,dz),new Vector3(.12f,.78f,.12f),Quaternion.identity,dark);
                    shape.Slot(1).Box(c+new Vector3(bench,-.32f,0),new Vector3(.36f,.08f,length-.2f),Quaternion.identity,wood);
                    for(int k=0;k<20;k++){float bz=-length*.45f+(k%10)*length*.1f,bx=k<10?-.24f:.24f;shape.Slot(4).Tube(new[]{c+new Vector3(bx,.05f,bz),c+new Vector3(bx,.13f,bz)},new[]{.07f,.11f},8,new Color(.66f,.56f,.44f),new Color(.78f,.66f,.5f),1,1,false);}
                    ZoneKit.Block(body,c+new Vector3(bench*.5f,-.4f,0),new Vector3(1+Mathf.Abs(bench)+.2f,.9f,length),0,"Kitchen table");
                }
                var drawn=kit.Built("Odrun's kitchen drawn",shape,Stone,Timber,Slate,Lamp,Iron,Cloth);drawn.transform.SetParent(body.transform,true);
                ZoneKit.Block(body,range+Vector3.up*.55f,new Vector3(2.2f,1.1f,1.1f),0,"Range");
                foreach(float x in new[]{-30.4f,-26.4f,-22.4f,-19.6f})ZoneKit.Block(body,P(x,46.1f,1),new Vector3(.24f,2,.24f),0,"Lean-to post");
                kit.Lamp(new Vector3(-21.6f,0,47),7,1.2f,new Color(1,.74f,.46f)).transform.position=P(-21.6f,47.2f,1.8f);

                // ---- the hidden quarter: bedrolls and lean-tos against the terrace wall, a cooking fire, the Unsworn's pikes under a tarp, Odrun's back table and its jar.
                var quarter=new ZoneKit.Shape();var qBlocks=new GameObject("The hidden quarter");
                foreach(var (x0,x1,tone) in new[]{(-30.6f,-27.2f,new Color(.56f,.5f,.42f)),(-22,-19.4f,new Color(.5f,.52f,.5f))})
                {
                    quarter.Slot(0).Quad(P(x0,55,2.2f),P(x1,55,2.2f),P(x1,57,2.9f),P(x0,57,2.9f),Vector3.up+Vector3.back*.4f,tone,.5f);
                    quarter.Slot(1).Post(P(x0+.1f,55,0),2.2f,.06f,.05f,6,dark,wood).Post(P(x1-.1f,55,0),2.2f,.06f,.05f,6,dark,wood);
                    for(int k=0;k<2;k++)quarter.Slot(0).Box(P(x0+.9f+k*1.4f,56.1f,.12f),new Vector3(.8f,.24f,1.8f),Quaternion.identity,ZoneKit.Shape.Dim(tone,.8f+.1f*k));
                    ZoneKit.Block(qBlocks,P((x0+x1)*.5f,56,1),new Vector3(x1-x0,2,2),0,"Lean-to");
                }
                var pikes=P(-28.6f,51.4f,0);
                for(int k=0;k<7;k++)quarter.Slot(2).Tube(new[]{pikes+new Vector3(k*.14f-.4f,0,.2f),pikes+new Vector3(k*.1f-.3f,2.6f,.5f)},new[]{.03f,.03f},5,new Color(.46f,.42f,.4f),new Color(.66f,.64f,.62f),1,1);
                quarter.Slot(0).Quad(pikes+new Vector3(-.8f,1.6f,-.2f),pikes+new Vector3(.8f,1.6f,-.2f),pikes+new Vector3(.8f,.1f,-.5f),pikes+new Vector3(-.8f,.1f,-.5f),Vector3.back,new Color(.46f,.44f,.36f),.5f);
                ZoneKit.Block(qBlocks,pikes+Vector3.up*1,new Vector3(1.6f,2,1),0,"Pike stack");
                var back=P(-21,52.4f,0);
                quarter.Slot(1).Box(back+Vector3.up*.75f,new Vector3(2.4f,.08f,.9f),Quaternion.identity,wood).Box(back+new Vector3(-1.1f,.37f,0),new Vector3(.1f,.74f,.8f),Quaternion.identity,dark).Box(back+new Vector3(1.1f,.37f,0),new Vector3(.1f,.74f,.8f),Quaternion.identity,dark);
                quarter.Slot(4).Tube(new[]{back+new Vector3(-.7f,.8f,0),back+new Vector3(-.7f,1.1f,0)},new[]{.12f,.1f},10,new Color(.8f,.86f,.84f),new Color(.86f,.9f,.88f),1,1);
                foreach(var (dx,c) in new[]{(-.2f,new Color(.5f,.54f,.6f)),(.3f,new Color(.6f,.5f,.36f)),(.8f,new Color(.54f,.52f,.5f))})quarter.Slot(0).Box(back+new Vector3(dx,.88f,0),new Vector3(.38f,.22f,.3f),Quaternion.Euler(0,dx*40,0),c);
                ZoneKit.Block(qBlocks,back+Vector3.up*.45f,new Vector3(2.4f,.9f,.9f),0,"Back table");
                var ring=P(-27.4f,53.6f,0);for(int k=0;k<6;k++){float a=k*Mathf.PI/3;quarter.Slot(4).Lump(ring+new Vector3(Mathf.Cos(a)*.45f,.08f,Mathf.Sin(a)*.45f),new Vector3(.14f,.1f,.12f),k,.2f,new Color(.4f,.4f,.42f),new Color(.3f,.3f,.3f),6,3);}
                quarter.Slot(3).Lump(ring+Vector3.up*.1f,new Vector3(.3f,.08f,.3f),3,.3f,Color.white,Color.white,8,3);
                ZoneKit.Block(qBlocks,ring+Vector3.up*.25f,new Vector3(1.1f,.5f,1.1f),0,"Fire");
                kit.Built("The hidden quarter drawn",quarter,Cloth,Timber,Iron,Coal,Stone).transform.SetParent(qBlocks.transform,true);
            }

            // ---------------------------------------------------------------- the armourers' street
            /// <summary>Four forges against the upper terrace's wall, fronts open to the walk: a hearth and hood, an anvil, a quench trough, racks of pike-heads. Half are cold until the town
            /// has its answer; from <c>vask.deposed</c> every hearth is lit and the hammers go. The mender's frame, the armourers' anvil (the bench), the regulation kettle, and the
            /// quartermaster's stair with the garrison salt at its foot.</summary>
            static void Armourers(ZoneKit kit)
            {
                Color wall=new(.62f,.6f,.58f),wood=new(.6f,.48f,.36f),dark=new(.32f,.3f,.3f),soot=new(.2f,.19f,.19f);
                var shape=new ZoneKit.Shape();var body=new GameObject("Armourers' street");var lit=new ZoneKit.Shape();var cold=new ZoneKit.Shape();var later=new ZoneKit.Shape();
                float[] forges={16,22.5f,29,35.5f};bool[] litAtFirst={false,true,false,true};
                for(int i=0;i<forges.Length;i++)
                {
                    float cx=forges[i];Vector3 P(float dx,float dz,float up)=>new(cx+dx,Lower+up,dz);
                    // Back wall against the terrace, side walls, a slate lean-to over the hearth's back only: a roof out to the walk hid the fire from the camera.
                    shape.Slot(0).Box(P(0,55.4f,2.1f),new Vector3(6.2f,4.6f,.6f),Quaternion.identity,wall,null,.2f);
                    foreach(int side in new[]{-1,1})shape.Slot(0).Box(P(side*2.95f,51.6f,1.3f),new Vector3(.4f,2.6f,7.6f),Quaternion.identity,wall,null,.2f);
                    shape.Slot(1).Quad(P(-3.3f,52.2f,3.7f),P(3.3f,52.2f,3.7f),P(3.3f,55.6f,4.4f),P(-3.3f,55.6f,4.4f),Vector3.up+Vector3.back*.2f,new Color(.58f,.56f,.58f),.5f);
                    foreach(int side in new[]{-1,1})shape.Slot(2).Post(P(side*2.9f,52.3f,0),3.7f,.12f,.1f,6,dark,wood);
                    // Hearth and hood, the coals dark or lit.
                    var hearth=P(-1.2f,53.6f,0);
                    shape.Slot(0).Box(hearth+Vector3.up*.45f,new Vector3(2.2f,.9f,1.6f),Quaternion.identity,new Color(.46f,.42f,.4f),soot,.3f);
                    shape.Slot(3).Tube(new[]{hearth+Vector3.up*1.6f,hearth+Vector3.up*3.4f,hearth+Vector3.up*5.4f},new[]{1.1f,.5f,.36f},4,soot,dark,1,1);
                    var coals=hearth+Vector3.up*.92f;
                    (litAtFirst[i]?lit:cold).Slot(0).Lump(coals,new Vector3(.8f,.1f,.55f),i,.4f,Color.white,Color.white,10,3);
                    if(!litAtFirst[i])later.Slot(0).Lump(coals,new Vector3(.8f,.1f,.55f),i,.4f,Color.white,Color.white,10,3);
                    // The hearth's mouth on its front, toward the walk: the fire is what this street is about, so it shows from the camera, not only its light.
                    var mouth=hearth+new Vector3(-.62f,.16f,-.81f);
                    (litAtFirst[i]?lit:cold).Slot(0).Quad(mouth,mouth+new Vector3(1.24f,0,0),mouth+new Vector3(1.24f,.52f,0),mouth+new Vector3(0,.52f,0),Vector3.back,Color.white,.5f);
                    if(!litAtFirst[i])later.Slot(0).Quad(mouth,mouth+new Vector3(1.24f,0,0),mouth+new Vector3(1.24f,.52f,0),mouth+new Vector3(0,.52f,0),Vector3.back,Color.white,.5f);
                    // Anvil on its block, the quench trough, a rack of pike-heads and hafts.
                    var anvil=P(1.2f,51,0);
                    shape.Slot(3).Box(anvil+Vector3.up*.3f,new Vector3(.5f,.6f,.5f),Quaternion.identity,new Color(.4f,.32f,.26f)).Box(anvil+Vector3.up*.72f,new Vector3(.9f,.24f,.34f),Quaternion.identity,new Color(.28f,.28f,.3f));
                    shape.Slot(3).Box(P(2.1f,53.8f,.35f),new Vector3(.7f,.7f,1.6f),Quaternion.identity,new Color(.3f,.32f,.34f),new Color(.18f,.24f,.28f));
                    for(int k=0;k<5;k++)shape.Slot(3).Tube(new[]{P(-2.4f+k*.24f,55,.1f),P(-2.3f+k*.24f,55,2.5f)},new[]{.03f,.025f},5,new Color(.5f,.42f,.34f),new Color(.7f,.7f,.72f),1,1);
                    ZoneKit.Block(body,P(0,55.4f,1.6f),new Vector3(6.2f,3.2f,.6f),0,"Forge wall");
                    foreach(int side in new[]{-1,1})ZoneKit.Block(body,P(side*2.95f,51.6f,1),new Vector3(.4f,2,7.6f),0,"Forge wall");
                    ZoneKit.Block(body,hearth+Vector3.up*.45f,new Vector3(2.2f,.9f,1.6f),0,"Hearth");ZoneKit.Block(body,anvil+Vector3.up*.4f,new Vector3(.9f,.8f,.5f),0,"Anvil");ZoneKit.Block(body,P(2.1f,53.8f,.35f),new Vector3(.7f,.7f,1.6f),0,"Trough");
                }
                var drawn=kit.Built("Forges",shape,Stone,Slate,Timber,Iron);drawn.transform.SetParent(body.transform,true);
                var litBody=kit.Built("Forges lit",lit,Coal);litBody.transform.SetParent(body.transform,true);
                var coldBody=kit.Built("Forges cold",cold,Dark);var laterBody=kit.Built("Forges lit again",later,Coal);
                // Cold until the town has answered; then every hearth lit.
                Gate("The cold forges","vask.deposed",false,coldBody);Gate("Every forge lit again","vask.deposed",true,laterBody);
                // The street is about its fire: each hearth that burns from the start throws its light out over the walk (the ground kit lays a pool and embers under it).
                for(int i=0;i<forges.Length;i++)if(litAtFirst[i])kit.Lamp(new Vector3(forges[i]-1.2f,0,53),8,2.2f,new Color(1,.6f,.3f)).transform.position=new Vector3(forges[i]-1.2f,Lower+1.4f,52.2f);

                // ---- the mender's frame (paid repair; saves at the street's spawn) and the armourers' anvil (the bench)
                var frame=kit.Prop("DeckConsole",new Vector3(MenderAt.x,0,MenderAt.y+1.2f),1.4f,180).AddComponent<RepairBay>();frame.saveSpawn="Armourers";frame.prompt="Armourer's frame — repair and save";
                var bench=kit.Prop("LatticeAnvil",new Vector3(AnvilAt.x,0,AnvilAt.y+1.3f),1.6f,180).AddComponent<Bench>();bench.prompt="Fabricate — The Armourers' Anvil";
                // ---- the quartermaster's store at the stair's foot: sacks of garrison salt stencilled with the Muster's mark, a counter.
                var store=new ZoneKit.Shape();var foot=At(kit,new Vector2(46.2f,51));
                // Sacks of sacking stacked on a pallet, their necks tied, each stencilled with the Muster's bar (untextured white lumps on a crate until November 21, s51/107).
                store.Slot(1).Box(foot+Vector3.up*.08f,new Vector3(1.5f,.16f,1.1f),Quaternion.identity,new Color(.58f,.46f,.34f),new Color(.66f,.54f,.4f));store.Slot(0);
                for(int k=0;k<6;k++)Sack(store,foot+new Vector3((k%2)*.62f-.31f,.16f+(k/2)*.42f,(k/2)*.08f-.1f),.62f-.04f*(k/2),ZoneKit.Shape.Dim(new Color(.9f,.88f,.84f),.94f+.04f*(k%3)),k+11,Quaternion.Euler(90,0,(k%2==0?-1:1)*(8+k*3)),new Color(.36f,.34f,.36f));
                kit.Built("Salt store",store,Burlap,Timber);
            }

            // ---------------------------------------------------------------- the upper terrace: the forecourt, the cadet yard, the armoury, the bastion
            static void UpperTerrace(ZoneKit kit)
            {
                Color stone=new(.68f,.66f,.64f),cap=new(.84f,.82f,.78f),wood=new(.62f,.5f,.38f),dark=new(.32f,.3f,.3f);
                // ---- garrison families' houses along the cadet yard's south side
                // No houses here: south of the cadet yard, anything taller than a wall stands between the camera and the yard (the first stills hid it behind two roofs).
                // ---- the cadet yard: drill posts, a rack of practice pikes, a trough, a vaulting block
                var yard=new ZoneKit.Shape();var yardBlocks=new GameObject("Cadet yard");
                foreach(var (x,z) in new[]{(-46f,84f),(-42f,84f),(-38f,84f),(-46f,90f),(-42f,90f),(-38f,90f)}){var p=At(kit,new Vector2(x,z));yard.Slot(0).Post(p,1.8f,.14f,.12f,6,new Color(.5f,.4f,.3f),new Color(.66f,.54f,.4f));ZoneKit.Block(yardBlocks,p+Vector3.up*.9f,new Vector3(.32f,1.8f,.32f),0,"Drill post");}
                var rack=At(kit,new Vector2(-50.6f,92));yard.Slot(1).Beam(rack+new Vector3(-1.6f,1.4f,0),rack+new Vector3(1.6f,1.4f,0),.1f,.1f,dark);
                for(int k=0;k<9;k++)yard.Slot(1).Tube(new[]{rack+new Vector3(-1.4f+k*.35f,0,.2f),rack+new Vector3(-1.4f+k*.35f,2.4f,-.1f)},new[]{.03f,.025f},5,new Color(.58f,.48f,.36f),new Color(.66f,.56f,.42f),1,1);
                ZoneKit.Block(yardBlocks,rack+Vector3.up*1,new Vector3(3.4f,2,.6f),0,"Pike rack");
                var trough=At(kit,new Vector2(-30.4f,92.6f));yard.Slot(2).Box(trough+Vector3.up*.4f,new Vector3(2.8f,.8f,.9f),Quaternion.identity,stone,new Color(.3f,.36f,.4f),.2f);ZoneKit.Block(yardBlocks,trough+Vector3.up*.4f,new Vector3(2.8f,.8f,.9f),0,"Trough");
                kit.Built("Cadet yard",yard,Timber,Iron,Stone).transform.SetParent(yardBlocks.transform,true);
                // ---- the armoury: a squat block with a barred door to the south and a slit window either side; the Muster's banner over the door
                // Squat, so it hides nothing: the walk from the field to the wall passes 7 m north of its back, and a block 8 m tall there hid the walk from the camera (the first stills).
                var armoury=new ZoneKit.Shape();var aFoot=At(kit,new Vector2(35.5f,73));
                armoury.Slot(0).Box(aFoot+Vector3.up*2.3f,new Vector3(11,4.6f,10),Quaternion.identity,stone,null,.2f).Box(aFoot+Vector3.up*4.8f,new Vector3(11.6f,.4f,10.6f),Quaternion.identity,cap,cap,.2f);
                for(float x=-5.2f;x<=5.3f;x+=1.3f)armoury.Slot(0).Box(aFoot+new Vector3(x,5.3f,-5),new Vector3(.7f,.6f,.6f),Quaternion.identity,stone,cap,.2f);
                armoury.Slot(1).Box(aFoot+new Vector3(0,1.6f,-5.02f),new Vector3(2.6f,3.2f,.1f),Quaternion.identity,wood,null,.5f);
                for(int k=0;k<5;k++)armoury.Slot(2).Box(aFoot+new Vector3(-1.04f+k*.52f,1.6f,-5.1f),new Vector3(.08f,3.2f,.08f),Quaternion.identity,dark);
                foreach(int side in new[]{-1,1})armoury.Slot(3).Box(aFoot+new Vector3(side*3.4f,2.9f,-5.02f),new Vector3(.3f,1,.06f),Quaternion.identity,Color.white);
                Banner(armoury,aFoot+new Vector3(0,4.5f,-5.3f),1.6f,1.2f,Vector3.back,2,4);
                var aBody=kit.Built("Armoury",armoury,Stone,Timber,Iron,Dark,Cloth);ZoneKit.Fadeable(aBody);ZoneKit.Block(aBody,aFoot+Vector3.up*2.3f,new Vector3(11,4.6f,10),0,"Armoury");
                // ---- the bastion: the Muster's flag on a mast, a signal brazier, a spyglass stand
                var bastion=new ZoneKit.Shape();var mast=At(kit,new Vector2(59,122.6f));
                bastion.Slot(0).Post(mast,9,.16f,.1f,8,dark,new Color(.5f,.48f,.48f));Banner(bastion,mast+new Vector3(1.2f,8.6f,0),2.2f,3.4f,Vector3.back,0,1);
                var brazier=At(kit,new Vector2(41.6f,124.6f));bastion.Slot(2).Tube(new[]{brazier,brazier+Vector3.up*1.2f},new[]{.2f,.5f},10,dark,new Color(.4f,.38f,.36f),1,1);bastion.Slot(3).Lump(brazier+Vector3.up*1.25f,new Vector3(.42f,.16f,.42f),2,.3f,Color.white,Color.white,10,3);
                var bBody=kit.Built("Bastion",bastion,Iron,Cloth,Iron,Coal);ZoneKit.Block(bBody,mast+Vector3.up*1,new Vector3(.4f,2,.4f),0,"Flag mast");ZoneKit.Block(bBody,brazier+Vector3.up*.6f,new Vector3(1,1.2f,1),0,"Brazier");
            }

            // ---------------------------------------------------------------- the oath hall: the town's landmark
            /// <summary>On its podium at the head of the field: a portico of six columns under a pitched slate roof, the Muster's crest in bronze on the pediment, banners between the outer
            /// columns, the hall behind with its great doors shut; under the portico the wall of names (every company the Warmaster brought home) and his plain table. Broad steps down to
            /// the field, which are the town's stand on a muster day; braziers at their feet; the oath bell in its frame at the field's north-east corner.</summary>
            static void OathHall(ZoneKit kit)
            {
                Color stone=new(.7f,.68f,.66f),pale=new(.86f,.84f,.8f),wood=new(.6f,.48f,.36f),dark=new(.3f,.28f,.28f);
                var shape=new ZoneKit.Shape();var body=new GameObject("Oath hall");float y=Podium;
                Vector3 P(float x,float z,float up)=>new(x,y+up,z);
                // The hall: walls 11 m, the roof's ridge east to west.
                shape.Slot(0).Box(P(0,118.4f,5.5f),new Vector3(30,11,12.4f),Quaternion.identity,stone,null,.2f);
                shape.Slot(1).Quad(P(-16,105.4f,10.6f),P(16,105.4f,10.6f),P(16,115.8f,15.6f),P(-16,115.8f,15.6f),Vector3.up+Vector3.back,new Color(.56f,.56f,.6f),.4f)
                    .Quad(P(16,125.2f,10.6f),P(-16,125.2f,10.6f),P(-16,115.8f,15.6f),P(16,115.8f,15.6f),Vector3.up+Vector3.forward,new Color(.5f,.5f,.54f),.4f);
                // The pediment over the portico, and the crest in bronze: two swept crest-sails over a ring.
                shape.Slot(0).Triangle(P(-15.6f,105.6f,10.7f),P(15.6f,105.6f,10.7f),P(0,105.6f,15.3f),Vector3.back,pale,.2f);
                shape.Slot(0).Box(P(0,106,9.9f),new Vector3(31,1.4f,1.4f),Quaternion.identity,pale,pale,.2f);
                var crest=P(0,105.5f,12.4f);
                shape.Slot(3).Disc(crest,Vector3.back,1,20,new Color(.86f,.66f,.38f),.6f).Triangle(crest+new Vector3(-.4f,-.2f,0),crest+new Vector3(-3.2f,1.6f,0),crest+new Vector3(-1.4f,-1,0),Vector3.back,new Color(.86f,.66f,.38f),.5f)
                    .Triangle(crest+new Vector3(.4f,-.2f,0),crest+new Vector3(3.2f,1.6f,0),crest+new Vector3(1.4f,-1,0),Vector3.back,new Color(.86f,.66f,.38f),.5f);
                // Six columns of stacked basalt drums.
                float[] columns={-12.5f,-7.5f,-2.5f,2.5f,7.5f,12.5f};
                foreach(float cx in columns)
                {
                    var foot=P(cx,106.3f,0);shape.Slot(0).Box(foot+Vector3.up*.2f,new Vector3(1.6f,.4f,1.6f),Quaternion.identity,pale,pale,.2f);
                    for(int k=0;k<6;k++)shape.Slot(0).Tube(new[]{foot+Vector3.up*(.4f+k*1.55f),foot+Vector3.up*(.4f+(k+1)*1.55f-.04f)},new[]{.62f-.01f*k,.61f-.01f*k},12,ZoneKit.Shape.Dim(stone,.94f+.04f*(k%2)),stone,1,.4f);
                    shape.Slot(0).Box(foot+Vector3.up*9.6f,new Vector3(1.6f,.5f,1.6f),Quaternion.identity,pale,pale,.2f);
                    ZoneKit.Block(body,foot+Vector3.up*2,new Vector3(1.3f,4,1.3f),0,"Column");
                }
                foreach(float cx in new[]{-10f,10f})Banner(shape,P(cx,106.6f,8.6f),2,5,Vector3.back,4,5);
                // The wall of names: rows of bronze plates, west of the great doors; the doors shut, bronze-studded.
                for(int row=0;row<7;row++)for(int k=0;k<14;k++)shape.Slot(3).Box(P(-13.2f+k*.7f,112.12f,1.2f+row*.62f),new Vector3(.56f,.34f,.04f),Quaternion.identity,ZoneKit.Shape.Dim(new Color(.86f,.66f,.4f),.86f+.12f*((row*7+k)%3)/2f));
                shape.Slot(2).Box(P(5.4f,112.1f,2.6f),new Vector3(5,5.2f,.2f),Quaternion.identity,wood,null,.5f);
                for(int k=0;k<4;k++)for(int j=0;j<6;j++)shape.Slot(3).Lump(P(3.4f+k*1.3f,112,1+j*.8f),Vector3.one*.07f,k*7+j,.02f,new Color(.86f,.66f,.38f),new Color(.6f,.44f,.26f),6,3);
                // The Warmaster's table: plain boards on trestles, soldiers' bowls and bread, a jug; a bench along its far side.
                var table=P(0,110.9f,.8f);
                shape.Slot(2).Box(table,new Vector3(7,.1f,1.1f),Quaternion.identity,wood,new Color(.78f,.66f,.5f),.5f);
                foreach(int side in new[]{-1,1})shape.Slot(2).Box(table+new Vector3(side*3.1f,-.4f,0),new Vector3(.14f,.8f,1),Quaternion.identity,dark);
                shape.Slot(2).Box(table+new Vector3(0,-.34f,.9f),new Vector3(6.6f,.08f,.36f),Quaternion.identity,wood);
                for(int k=0;k<7;k++)shape.Slot(4).Tube(new[]{table+new Vector3(-3+k,.05f,-.2f),table+new Vector3(-3+k,.13f,-.2f)},new[]{.08f,.12f},8,new Color(.6f,.5f,.4f),new Color(.72f,.6f,.46f),1,1,false);
                shape.Slot(4).Tube(new[]{table+new Vector3(1.6f,.05f,.2f),table+new Vector3(1.6f,.4f,.2f)},new[]{.12f,.09f},8,new Color(.6f,.44f,.34f),new Color(.7f,.52f,.4f),1,1);
                // The steps down to the field, each a course of dressed stone laid on the slope.
                var steps=new ZoneKit.Shape();var (_,half,from,to,low,high)=Ramps[4];int count=8;float run=(to-from)/count,rise=(high-low)/count;
                // Each a long dressed slab: the masonry laid at a twentieth of a metre a repeat, so a step reads as stone blocks two metres long and not as brick paving.
                for(int k=0;k<count;k++){float z0=from+k*run;steps.Slot(0).Box(new Vector3(0,low+(k+1)*rise-.25f,z0+run*.5f),new Vector3(half*2,.5f,run+.02f),Quaternion.identity,k%2==0?new Color(.72f,.7f,.66f):new Color(.68f,.66f,.62f),new Color(.8f,.78f,.74f),.05f);}
                kit.Built("Hall steps",steps,Stone).transform.SetParent(body.transform,true);
                var drawn=kit.Built("Oath hall drawn",shape,Stone,Slate,Timber,Bronze,Iron,Cloth);drawn.transform.SetParent(body.transform,true);
                ZoneKit.Block(body,P(0,118.4f,4),new Vector3(30,8,12.4f),0,"Oath hall");ZoneKit.Block(body,table+Vector3.down*.4f,new Vector3(7,.9f,1.2f),0,"Warmaster's table");
                // The roof hides nobody: the portico is six metres deep under a roof eleven metres up. The columns fade.
                ZoneKit.Fadeable(drawn);
                // Braziers at the steps' feet, both burning on the day of a duel: the chapter's fire in each bowl (HarrowgateBuilder.Hearths), its light breathing with its coals.
                var fires=new ZoneKit.Shape();
                foreach(int side in new[]{-1,1}){var b=At(kit,new Vector2(side*15.4f,99.2f));fires.Slot(0).Tube(new[]{b,b+Vector3.up*1.3f},new[]{.22f,.55f},10,dark,new Color(.4f,.38f,.36f),1,1);}
                var fireBody=kit.Built("Hall braziers",fires,Iron);foreach(int side in new[]{-1,1})ZoneKit.Block(fireBody,At(kit,new Vector2(side*15.4f,99.2f),.65f),new Vector3(1.1f,1.3f,1.1f),0,"Brazier");
                foreach(int side in new[]{-1,1})Hearths.Fire(kit,"Hall brazier fire "+(side<0?"west":"east"),At(kit,new Vector2(side*15.4f,99.2f),1.3f),.44f,1.1f,9,1.6f,side+41).transform.SetParent(fireBody.transform,true);
                // The oath bell: a bronze bell in a frame of two basalt posts and an iron beam, its rope to hand.
                var bell=new ZoneKit.Shape();var bFoot=At(kit,BellAt);
                foreach(int side in new[]{-1,1})bell.Slot(0).Box(bFoot+new Vector3(side*1.6f,2.8f,0),new Vector3(.7f,5.6f,.7f),Quaternion.identity,stone,pale,.2f);
                bell.Slot(1).Beam(bFoot+new Vector3(-2,5.4f,0),bFoot+new Vector3(2,5.4f,0),.3f,.3f,dark);
                // Crown, shoulder, straight waist, then the flare to the lip: the first one was a cone and read as a road marker.
                bell.Slot(2).Tube(new[]{bFoot+Vector3.up*5.2f,bFoot+Vector3.up*5.05f,bFoot+Vector3.up*4.8f,bFoot+Vector3.up*4.2f,bFoot+Vector3.up*3.85f,bFoot+Vector3.up*3.62f,bFoot+Vector3.up*3.55f},new[]{.12f,.42f,.55f,.6f,.74f,.92f,.94f},18,new Color(.36f,.32f,.24f),new Color(.56f,.46f,.3f),1,1);
                bell.Slot(1).Rope(bFoot+Vector3.up*3.6f,bFoot+new Vector3(.4f,1,.2f),.1f,.025f,new Color(.7f,.6f,.44f),4,3);
                var bellBody=kit.Built("The oath bell",bell,Stone,Iron,Iron);ZoneKit.Fadeable(bellBody);foreach(int side in new[]{-1,1})ZoneKit.Block(bellBody,bFoot+new Vector3(side*1.6f,1,0),new Vector3(.7f,2,.7f),0,"Bell post");
            }

            // ---------------------------------------------------------------- the trophy galleries
            /// <summary>Two long walks along the north wall, each with a lean-to roof over its cases (iron frames on plinths, the glass suggested by a glint) and the walk in front open to
            /// the sky. In the cases: a harbour bell, somebody's door, banners, a figurehead, a mask, and Lanternwick's relay glass under a card that says the canyon yielded it.</summary>
            static readonly (Vector2 at,string what)[] Cases=
            {
                (new(-49,111),"banner"),(new(-43,111),"glass"),(new(-37,111),"bell"),(new(-31,111),"door"),(new(-25,111),"figurehead"),
                (new(28,111),"mask"),(new(34,111),"glass"),
            };
            static readonly Dictionary<Vector2,GameObject> GlassIn=new();
            static void Galleries(ZoneKit kit)
            {
                GlassIn.Clear();
                Color stone=new(.68f,.66f,.64f),pale=new(.86f,.84f,.8f),wood=new(.6f,.48f,.36f),iron=new(.28f,.28f,.3f),card=new(.92f,.9f,.84f);
                foreach(var (x0,x1,name) in new[]{(-54f,-20f,"Trophy gallery"),(22f,38f,"East gallery")})
                {
                    var shape=new ZoneKit.Shape();var body=new GameObject(name);float y=Upper;
                    shape.Slot(0).Box(new Vector3((x0+x1)*.5f,y+2.4f,112.8f),new Vector3(x1-x0,4.8f+.4f,.8f),Quaternion.identity,stone,pale,.2f);
                    shape.Slot(1).Quad(new Vector3(x0,y+3.9f,108.4f),new Vector3(x1,y+3.9f,108.4f),new Vector3(x1,y+5,112.6f),new Vector3(x0,y+5,112.6f),Vector3.up+Vector3.back*.3f,new Color(.58f,.58f,.6f),.4f);
                    // The banners of the worlds the Muster has reached, one in every bay between the cases, each its own colours: what a still of the gallery says before any case is read.
                    Color[][] colours={new[]{new Color(.28f,.46f,.36f),new Color(.86f,.8f,.56f)},new[]{new Color(.32f,.42f,.6f),new Color(.9f,.88f,.82f)},new[]{new Color(.74f,.6f,.28f),new Color(.3f,.24f,.2f)},new[]{new Color(.84f,.82f,.76f),new Color(.36f,.5f,.56f)},new[]{new Color(.52f,.34f,.24f),new Color(.82f,.7f,.4f)},new[]{new Color(.3f,.5f,.54f),new Color(.86f,.84f,.7f)}};
                    int bay=0;for(float bx=x0+3.4f;bx<x1-1;bx+=6){var top=new Vector3(bx,y+3.85f,108.3f);var cl=colours[(bay+(int)(x0*.1f+20))%colours.Length];bay++;
                        shape.Slot(8).Quad(top+new Vector3(-.55f,0,0),top+new Vector3(.55f,0,0),top+new Vector3(.55f,-1.9f,0),top+new Vector3(-.55f,-1.9f,0),Vector3.back,cl[0],.5f).Triangle(top+new Vector3(-.55f,-1.9f,0),top+new Vector3(.55f,-1.9f,0),top+new Vector3(0,-2.3f,0),Vector3.back,cl[0],.5f)
                            .Quad(top+new Vector3(-.55f,-.5f,-.01f),top+new Vector3(.55f,-.5f,-.01f),top+new Vector3(.55f,-.75f,-.01f),top+new Vector3(-.55f,-.75f,-.01f),Vector3.back,cl[1],.5f).Disc(top+new Vector3(0,-1.25f,-.012f),Vector3.back,.26f,12,cl[1]);}
                    for(float x=x0+.4f;x<=x1;x+=4)shape.Slot(2).Post(new Vector3(x,y,108.6f),3.9f,.14f,.12f,8,new Color(.36f,.34f,.34f),new Color(.5f,.48f,.48f));
                    ZoneKit.Block(body,new Vector3((x0+x1)*.5f,y+2,112.8f),new Vector3(x1-x0,4,.8f),0,"Gallery wall");
                    for(float x=x0+.4f;x<=x1;x+=4)ZoneKit.Block(body,new Vector3(x,y+1,108.6f),new Vector3(.3f,2,.3f),0,"Gallery post");
                    foreach(var (at,what) in Cases.Where(c=>c.at.x>x0&&c.at.x<x1))
                    {
                        var c=new Vector3(at.x,y,at.y);
                        // The case: a plinth, an iron frame, a glint of glass on its front, and the card on the plinth's face.
                        shape.Slot(0).Box(c+Vector3.up*.45f,new Vector3(2.2f,.9f,1.2f),Quaternion.identity,pale,pale,.2f);
                        foreach(var (dx,dz) in new[]{(-1f,-.5f),(1f,-.5f),(-1f,.5f),(1f,.5f)})shape.Slot(3).Box(c+new Vector3(dx,1.9f,dz),new Vector3(.06f,2,.06f),Quaternion.identity,iron);
                        shape.Slot(3).Box(c+Vector3.up*2.92f,new Vector3(2.12f,.06f,1.12f),Quaternion.identity,iron);
                        shape.Slot(5).Box(c+new Vector3(0,.55f,-.62f),new Vector3(.7f,.36f,.03f),Quaternion.identity,card);
                        // Inside: a dark crimson back the trophy stands against, and a lit strip under the case's lid, so what is in it reads from the walk (in harrow-06 they were small
                        // grey shapes in grey frames under a roof's shade, and the banners between the bays said more than the cases).
                        shape.Slot(5).Quad(c+new Vector3(-.96f,.92f,.44f),c+new Vector3(.96f,.92f,.44f),c+new Vector3(.96f,2.86f,.44f),c+new Vector3(-.96f,2.86f,.44f),Vector3.back,new Color(.36f,.1f,.12f),.5f);
                        shape.Slot(9).Box(c+Vector3.up*2.86f,new Vector3(1.86f,.04f,.86f),Quaternion.identity,Color.white);
                        ZoneKit.Block(body,c+Vector3.up*.9f,new Vector3(2.2f,1.8f,1.2f),0,"Trophy case");
                        switch(what)
                        {
                            case "bell":shape.Slot(6).Tube(new[]{c+Vector3.up*2.6f,c+Vector3.up*2.2f,c+Vector3.up*1.4f,c+Vector3.up*1.02f},new[]{.14f,.38f,.62f,.68f},16,new Color(.92f,.74f,.46f),new Color(1,.86f,.58f),1,1);break;
                            case "door":shape.Slot(7).Box(c+Vector3.up*1.85f,new Vector3(1.3f,1.86f,.1f),Quaternion.Euler(0,8,0),new Color(.24f,.5f,.66f),null,.5f).Box(c+new Vector3(.42f,1.85f,-.08f),new Vector3(.14f,.14f,.06f),Quaternion.identity,new Color(.95f,.8f,.42f))
                                .Box(c+new Vector3(0,2.4f,-.07f),new Vector3(.9f,.08f,.03f),Quaternion.Euler(0,8,0),new Color(.92f,.86f,.7f)).Box(c+new Vector3(0,1.3f,-.07f),new Vector3(.9f,.08f,.03f),Quaternion.Euler(0,8,0),new Color(.92f,.86f,.7f));break;
                            case "banner":shape.Slot(8).Quad(c+new Vector3(-.86f,2.76f,0),c+new Vector3(.86f,2.76f,0),c+new Vector3(.74f,1,0),c+new Vector3(-.74f,1,0),Vector3.back,new Color(.18f,.5f,.42f),.5f)
                                .Quad(c+new Vector3(-.8f,2.1f,-.01f),c+new Vector3(.8f,2.1f,-.01f),c+new Vector3(.78f,1.8f,-.01f),c+new Vector3(-.78f,1.8f,-.01f),Vector3.back,new Color(.94f,.86f,.56f),.5f);break;
                            case "figurehead":shape.Slot(6).Lump(c+Vector3.up*1.9f,new Vector3(.4f,1,.45f),11,.2f,new Color(.86f,.42f,.28f),new Color(.95f,.84f,.62f),10,6);break;
                            case "mask":shape.Slot(6).Lump(c+Vector3.up*1.8f,new Vector3(.5f,.64f,.2f),13,.1f,new Color(1,.84f,.46f),new Color(.82f,.62f,.32f),12,6);break;
                        }
                    }
                    var drawn=kit.Built(name+" drawn",shape,Stone,Slate,Iron,Iron,Glass,Cloth,Bronze,Timber,Cloth,Lamp);drawn.transform.SetParent(body.transform,true);
                    // A warm light under the roof over the cases: the walk in front stays in the evening, the cases are lit.
                    foreach(float lx in x0<0?new[]{-45f,-31f}:new[]{31f}){var l=kit.Lamp(new Vector3(lx,0,110.4f),7,1.6f,new Color(1,.82f,.56f));l.transform.position=new Vector3(lx,y+3.3f,110.2f);}
                    foreach(var (at,what) in Cases.Where(c=>c.what=="glass"&&c.at.x>x0&&c.at.x<x1))GlassIn[at]=RelayGlass(kit,new Vector3(at.x,y,at.y),body.transform);
                }
                // The oath hall's case, under the portico.
                var hall=GameObject.Find("Oath hall");var c3=new Vector3(Case3.x,Podium,111);var caseShape=new ZoneKit.Shape();
                caseShape.Slot(0).Box(c3+Vector3.up*.45f,new Vector3(2.2f,.9f,1.2f),Quaternion.identity,pale,pale,.2f);
                foreach(var (dx,dz) in new[]{(-1f,-.5f),(1f,-.5f),(-1f,.5f),(1f,.5f)})caseShape.Slot(1).Box(c3+new Vector3(dx,1.9f,dz),new Vector3(.06f,2,.06f),Quaternion.identity,iron);
                caseShape.Slot(1).Box(c3+Vector3.up*2.92f,new Vector3(2.12f,.06f,1.12f),Quaternion.identity,iron).Slot(2).Box(c3+new Vector3(0,.55f,-.62f),new Vector3(.7f,.36f,.03f),Quaternion.identity,card);
                var caseBody=kit.Built("Oath hall case",caseShape,Stone,Iron,Cloth);if(hall!=null)caseBody.transform.SetParent(hall.transform,true);
                ZoneKit.Block(caseBody,c3+Vector3.up*.9f,new Vector3(2.2f,1.8f,1.2f),0,"Trophy case");
                GlassIn[Case3]=RelayGlass(kit,c3,caseBody.transform);
                // Before each gallery case a slab let into the walk: where its trophy was taken (the walks were bare flagstone from the cases to the camera, s50/131).
                CampaignStones(kit,Cases.Select(c=>c.at.x));
            }
            /// <summary>A plate of Lanternwick's relay glass in its case: the generated shard where it is taken in, else a pane of pale gold glass; shown only while it can be taken.</summary>
            static GameObject RelayGlass(ZoneKit kit,Vector3 caseAt,Transform parent)
            {
                GameObject glass;
                if(ZoneKit.Landed("LanternShard")){glass=kit.Prop("LanternShard",new Vector3(caseAt.x,caseAt.y+1.1f,caseAt.z),1.1f,15,false,false);}
                else{var pane=new ZoneKit.Shape().Box(caseAt+Vector3.up*1.8f,new Vector3(.9f,1.1f,.08f),Quaternion.Euler(0,15,6),new Color(1,.86f,.5f));glass=kit.Built("Relay glass pane",pane,Lamp);}
                glass.name="Lanternwick's relay glass";glass.transform.SetParent(parent,true);return glass;
            }

            // ---------------------------------------------------------------- the muster field
            /// <summary>The parade ground: cinder-grey inside its kerb, the drain along its west side (where the third file stands), the low timber stands on its east side, the Warmaster's
            /// skiff on its cradle beyond them. The duel's own pieces are <see cref="Duel"/>'s.</summary>
            static void Field(ZoneKit kit)
            {
                Color wood=new(.58f,.46f,.34f),dark=new(.3f,.28f,.28f),iron=new(.26f,.26f,.28f);
                var shape=new ZoneKit.Shape();var body=new GameObject("Muster field");
                // The drain: a stone gutter along the outside of the west kerb, an iron grate every few metres; the third file stands by it.
                for(float z=FieldSouth-.6f;z<FieldNorth+.4f;z+=1.2f)shape.Slot(0).Box(new Vector3(-FieldHalfX-1.25f,Upper+.012f,z+.6f),new Vector3(.5f,.02f,1.16f),Quaternion.identity,new Color(.2f,.2f,.22f));
                for(float z=FieldSouth+2;z<FieldNorth;z+=4.2f)for(int k=0;k<5;k++)shape.Slot(1).Box(new Vector3(-FieldHalfX-1.25f,Upper+.03f,z-.4f+k*.2f),new Vector3(.5f,.02f,.06f),Quaternion.identity,iron);
                // The east stands: three tiers of benches, low, in two blocks either side of the east opening.
                foreach(var (z0,z1) in new[]{(74f,81f),(89f,96f)})
                {
                    for(int tier=0;tier<3;tier++)
                    {
                        float x=20.8f+tier*1.05f,h=.38f+tier*.36f;
                        shape.Slot(2).Box(new Vector3(x,Upper+h,(z0+z1)*.5f),new Vector3(.9f,.1f,z1-z0),Quaternion.identity,wood,new Color(.74f,.62f,.46f),.5f);
                        for(float z=z0+.3f;z<z1;z+=1.8f)shape.Slot(2).Box(new Vector3(x,Upper+h*.5f,z),new Vector3(.1f,h,.1f),Quaternion.identity,dark);
                    }
                    ZoneKit.Block(body,new Vector3(21.85f,Upper+.6f,(z0+z1)*.5f),new Vector3(3.2f,1.2f,z1-z0),0,"Stand");
                }
                kit.Built("Muster field drawn",shape,Stone,Iron,Timber).transform.SetParent(body.transform,true);
                // The field's own floor, laid in the paving (drawn only; "Floor detail" so the box checks read it as flat): the Muster's ground mark at its middle, a border of slate slabs
                // between kerbstones inside the kerb, the Muster's standards at its corners (TownLook's FieldMark). Until November 21 the mark was a flat white ring, grey wings and a
                // white disc: colour on a mesh, no stone in it (s51/180).
                FieldMark(kit,body.transform);
                // The skiff's cradle: iron trestles; the skiff on it until he takes it.
                var cradle=new ZoneKit.Shape();var c=At(kit,SkiffAt);
                foreach(float dx in new[]{-2.2f,0f,2.2f}){cradle.Slot(0).Box(c+new Vector3(dx,.5f,0),new Vector3(.3f,1,2.6f),Quaternion.identity,iron).Box(c+new Vector3(dx,1.05f,0),new Vector3(.5f,.12f,3),Quaternion.identity,iron);}
                var cradleBody=kit.Built("Skiff cradle",cradle,Iron);ZoneKit.Block(cradleBody,c+Vector3.up*.55f,new Vector3(5,1.1f,3),0,"Skiff cradle");
                var skiff=SkiffBody(kit,new Vector3(SkiffAt.x,kit.Y(SkiffAt.x,SkiffAt.y)+1.2f,SkiffAt.y),Quaternion.Euler(0,90,0));
                if(skiff!=null){skiff.name="The Warmaster's skiff on its cradle";Gate("The skiff is on its cradle until he takes it","vask.duelWon",false,skiff);}
            }
            /// <summary>The Warmaster's war-skiff (the Lancer in his colours), drawn only.</summary>
            static GameObject SkiffBody(ZoneKit kit,Vector3 at,Quaternion turn)
            {
                var prefab=ZoneKit.Prefab("Enemies","DraskSkiff")??ZoneKit.Prefab("Enemies","MusterLancer");if(prefab==null)return null;
                var skiff=(GameObject)PrefabUtility.InstantiatePrefab(prefab);skiff.transform.SetPositionAndRotation(at,turn);
                foreach(var c in skiff.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
                foreach(var b in skiff.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
                return skiff;
            }

            // ---------------------------------------------------------------- the stair down and the harbour cells
            /// <summary>Outside the harbour wall: the armoury gate (an iron grille, shut until the town has answered), the stair down along the wall, and the landing at the water under the
            /// bastion where the cells are: three barred alcoves against the bastion's south face. Auntie stands over the landing's far corner. A radio on a cask by the cell door, a bench
            /// with Odrun's tray on it, the harbour's lamps beyond.</summary>
            static void StairAndCells(ZoneKit kit)
            {
                Color stone=new(.66f,.64f,.62f),pale=new(.84f,.82f,.78f),iron=new(.26f,.26f,.28f),wood=new(.58f,.46f,.34f);
                // ---- the armoury gate: two piers in the parapet's gap and a grille between them.
                var gate=new ZoneKit.Shape();var piers=new GameObject("Armoury gate");
                foreach(float z in new[]{78.2f,82.3f}){var p=new Vector3(47.4f,Upper,z);gate.Slot(0).Box(p+Vector3.up*1.5f,new Vector3(1,3,.7f),Quaternion.identity,stone,pale,.2f);ZoneKit.Block(piers,p+Vector3.up*1.5f,new Vector3(1,3,.7f),0,"Gate pier");}
                kit.Built("Armoury gate piers",gate,Stone).transform.SetParent(piers.transform,true);
                var grille=new ZoneKit.Shape();for(int k=0;k<8;k++)grille.Box(new Vector3(47.6f,Upper+1.3f,78.7f+k*.46f),new Vector3(.06f,2.6f,.06f),Quaternion.identity,iron);
                grille.Box(new Vector3(47.6f,Upper+2.5f,80.25f),new Vector3(.08f,.1f,3.4f),Quaternion.identity,iron).Box(new Vector3(47.6f,Upper+.4f,80.25f),new Vector3(.08f,.1f,3.4f),Quaternion.identity,iron);
                var shut=WorldBuilder.Membrane("Armoury gate grille",new Vector3(47.6f,Upper+1.3f,80.25f),3.4f,false,2.6f);shut.transform.rotation=Quaternion.Euler(0,90,0);
                foreach(var f in shut.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(f);foreach(var r in shut.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(r);
                kit.Built("Armoury gate grille drawn",grille,Iron).transform.SetParent(shut.transform,true);
                var gateBox=shut.GetComponent<BoxCollider>();var carve=shut.gameObject.AddComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.center=gateBox.center;carve.size=gateBox.size+new Vector3(0,0,.6f);carve.carving=true;carve.carveOnlyStationary=false;shut.SetOpen(false);
                Gate("The armoury gate is barred until the town has answered","vask.deposed",false,shut.gameObject);
                // ---- the cells: barred alcoves against the bastion's south face.
                var cells=new ZoneKit.Shape();var cellBlocks=new GameObject("Harbour cells");float y=Cells;
                foreach(float cx in new[]{51.6f,56f,60.4f})
                {
                    var back=new Vector3(cx,y,112);
                    foreach(int side in new[]{-1,1}){cells.Slot(0).Box(back+new Vector3(side*1.55f,1.5f,-1.3f),new Vector3(.5f,3,2.6f),Quaternion.identity,stone,null,.2f);ZoneKit.Block(cellBlocks,back+new Vector3(side*1.55f,1.5f,-1.3f),new Vector3(.5f,3,2.6f),0,"Cell wall");}
                    cells.Slot(0).Box(back+new Vector3(0,3.15f,-1.3f),new Vector3(3.6f,.3f,2.8f),Quaternion.identity,stone,pale,.2f);
                    cells.Slot(2).Box(back+new Vector3(0,1.5f,-.08f),new Vector3(2.6f,3,.1f),Quaternion.identity,new Color(.16f,.15f,.15f));
                    for(int k=0;k<9;k++)cells.Slot(1).Box(back+new Vector3(-1.2f+k*.3f,1.45f,-2.5f),new Vector3(.06f,2.9f,.06f),Quaternion.identity,iron);
                    foreach(float h in new[]{.25f,2.7f})cells.Slot(1).Box(back+new Vector3(0,h,-2.5f),new Vector3(2.6f,.1f,.08f),Quaternion.identity,iron);
                    ZoneKit.Block(cellBlocks,back+new Vector3(0,1.45f,-2.5f),new Vector3(2.6f,2.9f,.3f),0,"Cell bars");
                    // A pallet and a blanket inside.
                    cells.Slot(3).Box(back+new Vector3(.6f,.15f,-1),new Vector3(1,.3f,1.9f),Quaternion.identity,wood,new Color(.5f,.46f,.42f));
                }
                // Outside Drask's cell: a bench with Odrun's tray on it, and a radio set on a cask, its lamp lit.
                var bench=new Vector3(59.4f,y,108.6f);
                cells.Slot(3).Box(bench+Vector3.up*.45f,new Vector3(1.5f,.08f,.42f),Quaternion.identity,wood).Box(bench+new Vector3(-.6f,.22f,0),new Vector3(.1f,.44f,.36f),Quaternion.identity,wood).Box(bench+new Vector3(.6f,.22f,0),new Vector3(.1f,.44f,.36f),Quaternion.identity,wood);
                cells.Slot(3).Box(bench+new Vector3(-.2f,.53f,0),new Vector3(.5f,.04f,.34f),Quaternion.identity,new Color(.66f,.56f,.44f));
                cells.Slot(4).Tube(new[]{bench+new Vector3(-.3f,.55f,0),bench+new Vector3(-.3f,.64f,0)},new[]{.08f,.11f},8,new Color(.7f,.6f,.48f),new Color(.8f,.7f,.56f),1,1,false);
                var cask=new Vector3(52.2f,y,106.6f);
                cells.Slot(3).Tube(new[]{cask,cask+Vector3.up*.9f},new[]{.36f,.36f},12,new Color(.5f,.4f,.3f),new Color(.6f,.48f,.36f),2,1);
                cells.Slot(1).Box(cask+Vector3.up*1.1f,new Vector3(.56f,.36f,.36f),Quaternion.Euler(0,-20,0),new Color(.34f,.36f,.34f),new Color(.4f,.42f,.4f));
                cells.Slot(5).Lump(cask+new Vector3(.12f,1.18f,-.2f),Vector3.one*.05f,2,.01f,Color.white,Color.white,6,3);
                cells.Slot(1).Tube(new[]{cask+new Vector3(-.18f,1.26f,0),cask+new Vector3(-.3f,2.3f,.1f)},new[]{.012f,.008f},4,iron,iron,1,1);
                ZoneKit.Block(cellBlocks,bench+Vector3.up*.3f,new Vector3(1.5f,.6f,.45f),0,"Bench");ZoneKit.Block(cellBlocks,cask+Vector3.up*.6f,new Vector3(.8f,1.2f,.8f),0,"Cask");
                // A lamp on a bracket over the cells, lit: the landing's one light.
                var lampAt=new Vector3(53.8f,y+3.2f,111.6f);
                kit.Built("Harbour cells drawn",cells,Stone,Iron,Dark,Timber,Cloth,Lamp).transform.SetParent(cellBlocks.transform,true);
                var lamp=new ZoneKit.Shape();Lantern(kit,lamp,lampAt,true,10,1.3f);kit.Built("Cell lamp",lamp,Stone,Iron,Lamp).transform.SetParent(cellBlocks.transform,true);
                // Auntie: the big crane, over the landing's south-east corner, jib out over the berth.
                Crane(kit,"Auntie",new Vector3(62.2f,Cells,99.4f),20,35,new Color(.5f,.42f,.36f),18);
            }

            // ---------------------------------------------------------------- the harbour: what the wall looks down on
            /// <summary>Everything below the town: the east mole with its cranes and lamps, the flak mole and its guns, the carrier the Long Patience at her berth with the slip at her stern,
            /// moored hulls along the quay. Drawn only; nobody walks on any of it.</summary>
            static void Harbour(ZoneKit kit)
            {
                Color stone=new(.6f,.58f,.56f),pale=new(.76f,.74f,.7f),hull=new(.28f,.27f,.28f),rust=new(.44f,.16f,.14f),iron=new(.3f,.3f,.32f);
                var shape=new ZoneKit.Shape();
                // The east mole and the flak mole: stone piers a metre over the water.
                shape.Slot(0).Box(new Vector3(107,-.3f,40),new Vector3(8,2.6f,124),Quaternion.identity,stone,pale,.2f);
                shape.Slot(0).Box(new Vector3(88,-.3f,63),new Vector3(36,2.6f,6),Quaternion.identity,stone,pale,.2f);
                // The flak mole's three guns: drums, shields, paired barrels pointing up the lane.
                foreach(float x in new[]{76f,86f,96f})
                {
                    var g=new Vector3(x,1,63);shape.Slot(1).Tube(new[]{g,g+Vector3.up*1.4f},new[]{1.4f,1.2f},12,iron,new Color(.38f,.38f,.4f),1,1).Dome(g+Vector3.up*1.4f,1.2f,.8f,4,12,new Color(.4f,.4f,.42f),iron);
                    foreach(int side in new[]{-1,1})shape.Slot(1).Tube(new[]{g+new Vector3(side*.35f,2,0),g+new Vector3(side*.35f+1.2f,6,-2.4f)},new[]{.16f,.12f},8,iron,iron,1,1);
                }
                // The Long Patience: a carrier's long hull, its flight deck, an island, rust-red bands; the slip a ramp down from her stern.
                // Nearer the bastion than first built, so the lookout's frame takes in her flank, and longer, with a bow: the first was a grey box off to the east.
                var c=new Vector3(76,0,146);
                shape.Slot(2).Box(c+Vector3.up*3,new Vector3(20,8,30),Quaternion.identity,hull,new Color(.36f,.35f,.36f),.2f).Box(c+new Vector3(0,3,15),new Vector3(14.2f,8,14.2f),Quaternion.Euler(0,45,0),hull,new Color(.36f,.35f,.36f),.2f);
                shape.Slot(2).Box(c+new Vector3(0,7.4f,0),new Vector3(22,.8f,32),Quaternion.identity,new Color(.34f,.33f,.34f),new Color(.4f,.4f,.42f),.2f);
                shape.Slot(3).Box(c+new Vector3(-10.04f,4.6f,0),new Vector3(.1f,.9f,30),Quaternion.identity,rust).Box(c+new Vector3(10.04f,4.6f,0),new Vector3(.1f,.9f,30),Quaternion.identity,rust);
                shape.Slot(2).Box(c+new Vector3(7,12,4),new Vector3(6,9,12),Quaternion.identity,hull,new Color(.36f,.35f,.36f),.2f).Box(c+new Vector3(7,17.2f,4),new Vector3(4,1.4f,8),Quaternion.identity,iron);
                shape.Slot(1).Post(c+new Vector3(7,17.9f,4),6,.12f,.06f,6,iron,iron);
                for(float z=-14;z<=14;z+=4)foreach(int side in new[]{-1,1})shape.Slot(4).Lump(c+new Vector3(side*10.6f,7.9f,z),new Vector3(.18f,.18f,.18f),(int)z+side,.02f,Color.white,Color.white,6,4);
                shape.Slot(2).Quad(c+new Vector3(-6,7.6f,-15),c+new Vector3(6,7.6f,-15),c+new Vector3(6,-1.2f,-26),c+new Vector3(-6,-1.2f,-26),Vector3.up+Vector3.back,new Color(.36f,.34f,.32f),.3f);
                var shore=kit.Built("The harbour",shape,Stone,Iron,Iron,Cloth,Lamp);foreach(var r in shore.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
                // Lamps along the moles: lit heads, no lights (the floor takes six; these are out over the water).
                var lamps=new ZoneKit.Shape();
                for(float z=-14;z<100;z+=12){var p=new Vector3(104,1,z);lamps.Slot(0).Post(p,3.2f,.08f,.06f,6,iron,iron);lamps.Slot(1).Lump(p+Vector3.up*3.3f,new Vector3(.22f,.28f,.22f),(int)z,.04f,Color.white,Color.white,8,5);}
                for(float x=74;x<106;x+=8){var p=new Vector3(x,1,60.6f);lamps.Slot(0).Post(p,3.2f,.08f,.06f,6,iron,iron);lamps.Slot(1).Lump(p+Vector3.up*3.3f,new Vector3(.22f,.28f,.22f),(int)x,.04f,Color.white,Color.white,8,5);}
                kit.Built("Harbour lamps",lamps,Iron,Lamp);
                // Cranes on the east mole and by the berth: with the quay's two and Auntie, nine.
                foreach(var (z,yaw) in new[]{(-6f,250f),(22f,270f),(50f,280f),(84f,300f)})Crane(kit,"Mole crane",new Vector3(106,1,z),14,yaw,new Color(.44f,.4f,.38f),12,false);
                Crane(kit,"Berth crane",new Vector3(67,-1.6f,127),16,120,new Color(.46f,.4f,.36f),14,false);Crane(kit,"Berth crane",new Vector3(103,1,114),15,240,new Color(.42f,.4f,.38f),14,false);
                // Moored hulls: skiffs along the quay's east face, a hauler in the basin, Lancers off the flak mole.
                foreach(var (key,x,z,yaw,size) in new[]{("Skiff",54.4f,3.6f,0f,5f),("Skiff",54.6f,13.4f,4f,5.4f),("Hauler",72f,26f,-8f,14f),("Skiff",-20f,-11f,90f,5f),("Skiff",-4f,-11.4f,96f,4.6f)})
                {
                    if(!ZoneKit.Landed(key))continue;var ship=kit.Prop(key,new Vector3(x,Sea-.25f,z),size,yaw,false,false);ship.name="Moored "+key;ship.AddComponent<PropTint>().tint=new Color(.7f,.66f,.66f);
                }
                foreach(var (x,z,yaw) in new[]{(78f,54f,180f),(92f,54f,190f)})
                {
                    var prefab=ZoneKit.Prefab("Enemies","MusterLancer");if(prefab==null)break;
                    var lancer=(GameObject)PrefabUtility.InstantiatePrefab(prefab);lancer.name="Moored Lancer";lancer.transform.SetPositionAndRotation(new Vector3(x,Sea+.6f,z),Quaternion.Euler(0,yaw,0));var keel=ZoneKit.Drawn(lancer);lancer.transform.position+=Vector3.up*(Sea-.2f-keel.min.y);
                    foreach(var col in lancer.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(col);foreach(var b in lancer.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
                }
            }

            // ---------------------------------------------------------------- what a working town leaves about
            /// <summary>The town's clutter, each piece where the work it belongs to is done, every one off the ways, the errands, the signs and the prompts: casks and Muster crates on the
            /// quay, a hand cart at the road's end, nets drying on the sea kerb, lamp posts and the Muster's banners along the walks; a well, wood piles and a cart in the family quarter; a
            /// work yard before the forges (pike racks, bar stock, a grindstone, charcoal, quench casks, a cart of charcoal); a notice board of orders on the forecourt; dummies, targets and a
            /// sentry box in the cadet yard; boats on the water under the bastion and a line of lamp buoys up the harbour.</summary>
            static void Dressing(ZoneKit kit)
            {
                Color wood=new(.6f,.48f,.36f),dark=new(.3f,.28f,.28f),iron=new(.3f,.3f,.32f),crimson=new(.46f,.12f,.12f);
                var shape=new ZoneKit.Shape();var blocks=new GameObject("Town dressing");
                void Solid(Vector2 at,Vector3 size,float yaw=0){ZoneKit.Block(blocks,kit.On(at.x,at.y,size.y*.5f),size,yaw,"Town dressing");}
                void Cask(Vector2 at,float tall=.9f){var f=kit.On(at.x,at.y);shape.Slot(0).Tube(new[]{f,f+Vector3.up*tall*.5f,f+Vector3.up*tall},new[]{.3f,.36f,.3f},12,wood,new Color(.7f,.56f,.4f),2,1);
                    foreach(float h in new[]{.12f,.5f,.88f})shape.Slot(1).Tube(new[]{f+Vector3.up*(tall*h-.03f),f+Vector3.up*(tall*h+.03f)},new[]{.33f+.03f*(1-Mathf.Abs(h-.5f)*2),.33f+.03f*(1-Mathf.Abs(h-.5f)*2)},12,iron,iron,1,1,false);Solid(at,new Vector3(.74f,tall,.74f));}
                void Cart(Vector2 at,float yaw,Color load)
                {
                    var t=Quaternion.Euler(0,yaw,0);var f=kit.On(at.x,at.y);
                    shape.Slot(0).Box(f+Vector3.up*.75f,new Vector3(1.3f,.12f,2),t,wood,new Color(.7f,.58f,.42f),.5f);
                    foreach(int side in new[]{-1,1}){shape.Slot(0).Box(f+t*new Vector3(side*.62f,.98f,0),new Vector3(.06f,.36f,2),t,wood);var hub=f+t*new Vector3(side*.78f,.46f,-.2f);shape.Slot(1).Tube(new[]{hub-t*Vector3.right*.06f*side,hub+t*Vector3.right*.06f*side},new[]{.46f,.46f},14,dark,dark,1,1);
                        shape.Slot(0).Beam(f+t*new Vector3(side*.4f,.7f,1),f+t*new Vector3(side*.3f,.1f,2.2f),.08f,.08f,wood);}
                    shape.Slot(3).Lump(f+Vector3.up*1.02f,new Vector3(.55f,.26f,.8f),(int)(at.x*3),.3f,load,ZoneKit.Shape.Dim(load,.7f),10,4,.25f,t);
                    Solid(at,new Vector3(1.6f,1.2f,2.6f),yaw);
                }
                // A banner on its pole: the Muster's crimson with the iron crest-sail, or a house's own two colours with the crest-sail in the second.
                void Pole(Vector2 at,Color? cloth=null,Color? mark=null)
                {
                    Color c0=cloth??crimson,c1=mark??new Color(.62f,.62f,.64f);
                    var f=kit.On(at.x,at.y);shape.Slot(1).Post(f,4.6f,.09f,.06f,6,dark,iron).Beam(f+new Vector3(-.7f,4.3f,0),f+new Vector3(.7f,4.3f,0),.06f,.06f,iron);
                    shape.Slot(1).Lump(f+Vector3.up*4.66f,Vector3.one*.07f,(int)at.x,.05f,new Color(.62f,.5f,.32f),new Color(.42f,.34f,.22f),6,4);
                    // In folds, hemmed, fringed (one flat plane until November 21): the house's two colours or the Muster's crimson with its crest-sail.
                    Hang(shape,f+new Vector3(0,4.25f,-.05f),1.2f,2.1f,Vector3.back,3,c0,c1,cloth!=null?c1:ZoneKit.Shape.Dim(c0,.62f),cloth!=null?ZoneKit.Shape.Dim(c1,.9f):new Color(.78f,.62f,.34f));
                    Solid(at,new Vector3(.3f,2,.3f));
                }
                // Pennants on a cord between two poles' heads, for the day the town holds a duel.
                void Bunting(Vector2 one,Vector2 two,float sag,Color[] colours)
                {
                    Vector3 a=kit.On(one.x,one.y,4.45f),b=kit.On(two.x,two.y,4.45f);var along=b-a;along.y=0;along=along.normalized;
                    int n=Mathf.Max(2,Mathf.RoundToInt(Vector3.Distance(a,b)/.8f));var prev=a;
                    for(int k=1;k<=n;k++)
                    {
                        float t=k/(float)n;var at=Vector3.Lerp(a,b,t)+Vector3.down*sag*4*t*(1-t);
                        shape.Slot(1).Beam(prev,at,.018f,.018f,dark);
                        if(k<n)shape.Slot(3).Triangle(at-along*.2f,at+along*.2f,at+Vector3.down*.44f,Vector3.back,colours[k%colours.Length],.5f);
                        prev=at;
                    }
                }
                void LampPost(Vector2 at,Color? light=null){var f=kit.On(at.x,at.y);shape.Slot(1).Post(f,3.2f,.08f,.06f,6,dark,iron).Beam(f+Vector3.up*3.1f,f+new Vector3(.5f,3.1f,0),.05f,.05f,iron);
                    shape.Slot(4).Lump(f+new Vector3(.5f,2.8f,0),new Vector3(.17f,.24f,.17f),(int)(at.x*5+at.y),.03f,Color.white,Color.white,8,5);Solid(at,new Vector3(.26f,2,.26f));
                    if(light!=null){var l=kit.Lamp(new Vector3(at.x+.5f,0,at.y),8,1.5f,light.Value);l.transform.position=f+new Vector3(.5f,2.7f,0);}}
                // The street lamps burn without a light of their own: by day a lamp's light laid a flat ochre disc on the paving (s53/016); the lantern itself is still lit.
                Color? warm=null,cold=null;
                void PikeRack(Vector2 at,float yaw)
                {
                    var t=Quaternion.Euler(0,yaw,0);var f=kit.On(at.x,at.y);
                    foreach(int side in new[]{-1,1})shape.Slot(0).Beam(f+t*new Vector3(side*1,0,-.4f),f+t*new Vector3(side*1,1.5f,0),.07f,.07f,wood).Beam(f+t*new Vector3(side*1,0,.4f),f+t*new Vector3(side*1,1.5f,0),.07f,.07f,wood);
                    shape.Slot(0).Beam(f+t*new Vector3(-1.1f,1.45f,0),f+t*new Vector3(1.1f,1.45f,0),.08f,.08f,wood);
                    for(int k=0;k<8;k++){var b=f+t*new Vector3(-.85f+k*.24f,0,-.5f);shape.Slot(1).Tube(new[]{b,b+t*new Vector3(0,2.8f,.55f)},new[]{.03f,.025f},5,new Color(.5f,.42f,.34f),new Color(.72f,.72f,.74f),1,1);}
                    Solid(at,new Vector3(2.4f,1.6f,1),yaw);
                }
                void Bars(Vector2 at,float yaw){var t=Quaternion.Euler(0,yaw,0);var f=kit.On(at.x,at.y);for(int row=0;row<3;row++)for(int k=0;k<5-row;k++)shape.Slot(1).Box(f+t*new Vector3(-.4f+k*.2f+row*.1f,.08f+row*.13f,0),new Vector3(.12f,.12f,1.6f),t,new Color(.34f,.32f,.32f),new Color(.44f,.4f,.38f));Solid(at,new Vector3(1.1f,.5f,1.7f),yaw);}
                void Grindstone(Vector2 at,float yaw){var t=Quaternion.Euler(0,yaw,0);var f=kit.On(at.x,at.y);shape.Slot(0).Box(f+Vector3.up*.35f,new Vector3(.9f,.7f,.5f),t,wood);
                    shape.Slot(2).Tube(new[]{f+t*new Vector3(-.09f,.95f,0),f+t*new Vector3(.09f,.95f,0)},new[]{.42f,.42f},16,new Color(.6f,.58f,.54f),new Color(.66f,.64f,.6f),1,1);Solid(at,new Vector3(1,1.3f,.7f),yaw);}
                void Heap(Vector2 at,float size,Color top){var f=kit.On(at.x,at.y);shape.Slot(0).Box(f+Vector3.up*.2f,new Vector3(size*1.5f,.4f,size*1.3f),Quaternion.identity,wood).Slot(2).Lump(f+Vector3.up*size*.3f,new Vector3(size*.8f,size*.42f,size*.7f),(int)(at.x*7),.35f,top,ZoneKit.Shape.Dim(top,.6f),10,4);Solid(at,new Vector3(size*1.4f,size*.6f,size*1.2f));}
                void Woodpile(Vector2 at,float yaw){var t=Quaternion.Euler(0,yaw,0);var f=kit.On(at.x,at.y);for(int row=0;row<3;row++)for(int k=0;k<5-row;k++){var c=f+t*new Vector3(-.6f+k*.3f+row*.15f,.15f+row*.26f,0);shape.Slot(0).Tube(new[]{c-t*Vector3.forward*.45f,c+t*Vector3.forward*.45f},new[]{.14f,.14f},7,new Color(.5f,.4f,.3f),new Color(.66f,.54f,.4f),1,1);}Solid(at,new Vector3(1.6f,.9f,1),yaw);}
                void Well(Vector2 at){var f=kit.On(at.x,at.y);shape.Slot(2).Tube(new[]{f,f+Vector3.up*.8f},new[]{.9f,.9f},16,new Color(.6f,.58f,.56f),new Color(.74f,.72f,.68f),2,1);shape.Slot(5).Disc(f+Vector3.up*.6f,Vector3.up,.72f,16,new Color(.1f,.12f,.14f));
                    foreach(int side in new[]{-1,1})shape.Slot(0).Post(f+new Vector3(side*.8f,.7f,0),1.4f,.07f,.06f,6,wood,wood);shape.Slot(0).Beam(f+new Vector3(-.9f,2,0),f+new Vector3(.9f,2,0),.12f,.12f,wood);
                    shape.Slot(1).Rope(f+new Vector3(.1f,2,0),f+new Vector3(.1f,1.1f,0),0,.015f,dark,4,2);shape.Slot(0).Tube(new[]{f+new Vector3(.1f,.9f,0),f+new Vector3(.1f,1.15f,0)},new[]{.14f,.16f},8,wood,wood,1,1);Solid(at,new Vector3(1.9f,.9f,1.9f));}
                void NetRack(Vector2 at){var f=kit.On(at.x,at.y);foreach(int side in new[]{-1,1})shape.Slot(0).Post(f+new Vector3(side*1.3f,0,0),1.9f,.06f,.05f,6,wood,wood);shape.Slot(0).Beam(f+new Vector3(-1.4f,1.85f,0),f+new Vector3(1.4f,1.85f,0),.06f,.06f,wood);
                    shape.Slot(3).Quad(f+new Vector3(-1.25f,1.82f,-.02f),f+new Vector3(1.25f,1.82f,-.02f),f+new Vector3(1.1f,.4f,-.1f),f+new Vector3(-1.2f,.5f,-.1f),Vector3.back,new Color(.42f,.36f,.28f),.5f);Solid(at,new Vector3(2.8f,1.9f,.3f));}
                void Dummy(Vector2 at,float yaw){var t=Quaternion.Euler(0,yaw,0);var f=kit.On(at.x,at.y);shape.Slot(0).Post(f,1.9f,.07f,.06f,6,wood,wood).Beam(f+t*new Vector3(-.6f,1.4f,0),f+t*new Vector3(.6f,1.4f,0),.06f,.06f,wood);
                    shape.Slot(3).Lump(f+Vector3.up*1.25f,new Vector3(.28f,.42f,.22f),(int)at.x,.15f,new Color(.74f,.66f,.5f),new Color(.56f,.48f,.36f),10,5).Lump(f+Vector3.up*1.85f,new Vector3(.16f,.16f,.16f),2,.1f,new Color(.74f,.66f,.5f),new Color(.56f,.48f,.36f),8,4);Solid(at,new Vector3(.5f,1.9f,.5f),yaw);}
                void Target(Vector2 at,float yaw){var t=Quaternion.Euler(0,yaw,0);var f=kit.On(at.x,at.y);var n=t*Vector3.forward;shape.Slot(0).Beam(f+t*new Vector3(0,0,.4f),f+t*new Vector3(0,1.5f,0),.06f,.06f,wood);
                    shape.Slot(3).Disc(f+t*new Vector3(0,1.3f,-.05f),-n,.55f,18,new Color(.84f,.8f,.68f)).Disc(f+t*new Vector3(0,1.3f,-.06f),-n,.36f,18,crimson).Disc(f+t*new Vector3(0,1.3f,-.07f),-n,.16f,14,new Color(.84f,.8f,.68f));Solid(at,new Vector3(1.2f,1.6f,.6f),yaw);}
                void Board(Vector2 at){var f=kit.On(at.x,at.y);foreach(int side in new[]{-1,1})shape.Slot(0).Post(f+new Vector3(side*1.1f,0,0),2.5f,.08f,.07f,6,wood,wood);shape.Slot(0).Box(f+new Vector3(0,1.7f,0),new Vector3(2.4f,1.3f,.08f),Quaternion.identity,wood);
                    var rng=new System.Random((int)(at.x*13));for(int k=0;k<7;k++)shape.Slot(3).Box(f+new Vector3(-.9f+(float)rng.NextDouble()*1.8f,1.3f+(float)rng.NextDouble()*.8f,-.05f),new Vector3(.3f,.38f,.01f),Quaternion.Euler(0,0,((float)rng.NextDouble()-.5f)*12),new Color(.9f,.88f,.8f));
                    shape.Slot(0).Box(f+new Vector3(0,2.45f,-.1f),new Vector3(2.6f,.1f,.3f),Quaternion.identity,dark);Solid(at,new Vector3(2.5f,2.5f,.3f));}
                void SentryBox(Vector2 at,float yaw){var t=Quaternion.Euler(0,yaw,0);var f=kit.On(at.x,at.y);shape.Slot(2).Box(f+t*new Vector3(0,1.2f,.5f),new Vector3(1.5f,2.4f,.15f),t,new Color(.6f,.58f,.56f),null,.2f);
                    foreach(int side in new[]{-1,1})shape.Slot(2).Box(f+t*new Vector3(side*.7f,1.2f,0),new Vector3(.15f,2.4f,1.1f),t,new Color(.6f,.58f,.56f),null,.2f);shape.Slot(0).Box(f+t*new Vector3(0,2.5f,-.05f),new Vector3(1.8f,.12f,1.5f),t,dark);Solid(at,new Vector3(1.6f,2.5f,1.3f),yaw);}
                // ---- the quay
                foreach(var at in new[]{new Vector2(-50.6f,4.6f),new Vector2(-49.8f,3.4f),new Vector2(-48.8f,4.4f),new Vector2(24,-1.6f),new Vector2(25.2f,-2.4f),new Vector2(-9.2f,-2.2f)})Cask(at);
                Cart(new Vector2(-46.6f,3.4f),20,new Color(.82f,.78f,.66f));
                foreach(var (x,z,yaw,size) in new[]{(-36.6f,2.4f,15f,1.3f),(-35.2f,2.8f,-10f,1.1f),(30.6f,1.6f,5f,1.2f),(-2.6f,-2f,30f,1.2f)}){kit.Prop(ZoneKit.Kit("MusterCrate","DressCargoPallet"),new Vector3(x,0,z),size,yaw);}
                kit.Prop(ZoneKit.Kit("MusterCrate","DressCargoPallet"),new Vector3(-36.4f,1.3f,2.5f),1,40);
                NetRack(new Vector2(-19.4f,-3.6f));NetRack(new Vector2(11.6f,-3.6f));
                foreach(var at in new[]{new Vector2(-30,12.6f),new Vector2(-6,12.8f),new Vector2(26.4f,12.6f)})LampPost(at,warm);
                Pole(new Vector2(2.6f,12.2f));Pole(new Vector2(13.4f,12.2f));
                // Rope coiled by the bollards, the catch in baskets, and a gutting table by the one mending nets.
                var coils=new ZoneKit.Shape();
                foreach(var (x,z) in new[]{(-40.6f,-2.6f),(-22.6f,-2.4f),(6.4f,-2.4f),(33.6f,-2.6f),(48,6.4f)})for(int turn=0;turn<3;turn++){var ring=new Vector3[13];var thick=new float[13];for(int k=0;k<13;k++){float a=k*Mathf.PI*2/12;ring[k]=kit.On(x+Mathf.Cos(a)*(.5f-turn*.11f),z+Mathf.Sin(a)*(.5f-turn*.11f),.05f+turn*.08f);thick[k]=.05f;}coils.Tube(ring,thick,5,new Color(.74f,.66f,.5f),new Color(.74f,.66f,.5f),1,1,false);}
                kit.Built("Floor detail rope coils",coils,Timber).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                // (-4.9, 2.2): the catch the woman with the basket is sorting (she stood facing open paving).
                foreach(var (x,z,fish) in new[]{(-31.8f,11.4f,new Color(.62f,.66f,.7f)),(-33,10.8f,new Color(.7f,.6f,.5f)),(-10.6f,-.6f,new Color(.6f,.66f,.7f)),(-4.9f,2.2f,new Color(.66f,.68f,.72f))}){var f=kit.On(x,z);shape.Slot(0).Tube(new[]{f,f+Vector3.up*.5f},new[]{.32f,.4f},10,new Color(.6f,.5f,.36f),new Color(.72f,.6f,.44f),2,1,false).Slot(3).Lump(f+Vector3.up*.5f,new Vector3(.34f,.12f,.34f),(int)x,.3f,fish,ZoneKit.Shape.Dim(fish,.7f),8,3);Solid(new Vector2(x,z),new Vector3(.8f,.6f,.8f));}
                // The quay sergeant's tally desk: a standing desk with the ledger open on it and a lamp-less inkwell, by the harbourmaster's house.
                var desk=kit.On(21.3f,17.3f);shape.Slot(0).Box(desk+Vector3.up*1.02f,new Vector3(1.1f,.06f,.6f),Quaternion.Euler(-12,0,0),wood,new Color(.7f,.58f,.42f));
                foreach(int s in new[]{-1,1})shape.Slot(0).Box(desk+new Vector3(s*.48f,.5f,0),new Vector3(.08f,1,.5f),Quaternion.identity,wood);
                shape.Slot(3).Box(desk+new Vector3(-.12f,1.07f,0),new Vector3(.5f,.03f,.36f),Quaternion.Euler(-12,0,0),new Color(.9f,.88f,.8f)).Slot(1).Tube(new[]{desk+new Vector3(.36f,1.06f,.08f),desk+new Vector3(.36f,1.14f,.08f)},new[]{.04f,.035f},6,iron,iron,1,1);
                Solid(new Vector2(21.3f,17.3f),new Vector3(1.1f,1.1f,.6f));
                var table=kit.On(-12.4f,1.2f);shape.Slot(0).Box(table+Vector3.up*.8f,new Vector3(2,.1f,.9f),Quaternion.identity,wood,new Color(.7f,.6f,.46f)).Box(table+new Vector3(-.85f,.4f,0),new Vector3(.12f,.8f,.8f),Quaternion.identity,wood).Box(table+new Vector3(.85f,.4f,0),new Vector3(.12f,.8f,.8f),Quaternion.identity,wood)
                    .Slot(3).Lump(table+new Vector3(-.3f,.92f,0),new Vector3(.4f,.08f,.14f),3,.1f,new Color(.62f,.66f,.7f),new Color(.5f,.52f,.56f),8,3);Solid(new Vector2(-12.4f,1.2f),new Vector3(2.1f,.9f,1));
                // ---- the lower terrace: the walk's banners and lamps, the family quarter's well and wood, the armourers' work yard
                // The lower walk's banners are the houses' (the family quarter's own, and the armourers'), each in its two colours, with the day's pennants strung between them.
                Color[][] houses={new[]{new Color(.74f,.5f,.18f),new Color(.14f,.12f,.1f)},new[]{new Color(.16f,.42f,.44f),new Color(.9f,.86f,.74f)},new[]{new Color(.24f,.3f,.52f),new Color(.86f,.72f,.42f)},
                    new[]{new Color(.3f,.44f,.22f),new Color(.9f,.86f,.74f)},new[]{new Color(.6f,.26f,.14f),new Color(.92f,.9f,.86f)},new[]{new Color(.38f,.22f,.44f),new Color(.9f,.86f,.74f)}};
                float[] walkPoles={-46f,-34.6f,-14f,4f,20f,40f};
                for(int i=0;i<walkPoles.Length;i++)Pole(new Vector2(walkPoles[i],38.4f),houses[i][0],houses[i][1]);
                var pennants=houses.Select(h=>h[0]).Concat(new[]{new Color(.92f,.9f,.86f)}).ToArray();
                for(int i=1;i<walkPoles.Length;i++)Bunting(new Vector2(walkPoles[i-1],38.4f),new Vector2(walkPoles[i],38.4f),.8f,pennants);
                foreach(float x in new[]{-40f,-20f,26f})LampPost(new Vector2(x,31.6f),warm);
                Well(new Vector2(-31.4f,38.2f));Woodpile(new Vector2(-52.4f,43.6f),90);Woodpile(new Vector2(-18.2f,44.4f),90);Cart(new Vector2(-35.4f,41.6f),10,new Color(.6f,.5f,.36f));
                PikeRack(new Vector2(19.4f,43.4f),0);Bars(new Vector2(25.6f,43.2f),80);Grindstone(new Vector2(22.4f,43.6f),0);Heap(new Vector2(32.6f,44.8f),.9f,new Color(.32f,.31f,.3f));
                Cask(new Vector2(38.8f,44));Cask(new Vector2(39.6f,43.1f),.8f);Cart(new Vector2(11.6f,40.2f),80,new Color(.2f,.19f,.19f));
                // ---- the upper terrace: banners along the forecourt, the board of orders, lamps on the wall walk, the cadet yard's dummies, targets and sentry box
                foreach(float x in new[]{-30f,-18f,18f,26f})Pole(new Vector2(x,69.8f));
                Bunting(new Vector2(-30,69.8f),new Vector2(-18,69.8f),.6f,new[]{crimson,new Color(.92f,.9f,.86f)});Bunting(new Vector2(18,69.8f),new Vector2(26,69.8f),.5f,new[]{crimson,new Color(.92f,.9f,.86f)});
                // The harbour wall's lamps burn cold: the wall and the water are the town's coldest place.
                Board(new Vector2(-40.6f,71.2f));LampPost(new Vector2(46.2f,70),cold);LampPost(new Vector2(46.2f,92),cold);
                foreach(float x in new[]{-46f,-42f,-38f})Dummy(new Vector2(x,95.4f),180);
                Target(new Vector2(-52.6f,86),90);Target(new Vector2(-52.6f,90.6f),90);SentryBox(new Vector2(-52.6f,76),90);
                kit.Built("Town dressing drawn",shape,Timber,Iron,Stone,Cloth,Lamp,Dark).transform.SetParent(blocks.transform,true);
                // ---- on the water: boats moored under the bastion and off the landing, and lamp buoys marking the run up to the slip
                foreach(var (key,x,z,yaw,size) in new[]{("Skiff",58f,131f,90f,5f),("Skiff",66.6f,121f,4f,4.6f),("Skiff",60.4f,92.4f,85f,4.8f),("Hauler",86f,100f,-20f,12f)})
                {if(!ZoneKit.Landed(key))continue;var ship=kit.Prop(key,new Vector3(x,Sea-.25f,z),size,yaw,false,false);ship.name="Moored "+key;ship.AddComponent<PropTint>().tint=new Color(.72f,.68f,.66f);}
                var buoys=new ZoneKit.Shape();
                foreach(var (x,z) in new[]{(70f,104f),(71f,92f),(73f,80f),(76f,70f),(66f,112f)}){var b=new Vector3(x,Sea,z);buoys.Slot(0).Tube(new[]{b+Vector3.down*.2f,b+Vector3.up*.5f},new[]{.5f,.3f},10,new Color(.46f,.14f,.12f),new Color(.36f,.34f,.34f),1,1).Slot(1).Lump(b+Vector3.up*.75f,Vector3.one*.18f,(int)z,.02f,Color.white,Color.white,6,4);}
                kit.Built("Lamp buoys",buoys,Iron,Lamp).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                _=wood;
            }

            // ---------------------------------------------------------------- people
            /// <summary>People who live here, standing at their work: body, where, facing, tint, what they are doing (which is what they carry), and how big (a child, a cadet). Off every way,
            /// off the signs, out of the camera's line to anyone with a name, never two coats alike side by side, and never in a named person's coat near that person.</summary>
            static readonly (string body,Vector2 at,float yaw,Color tint,string doing,float scale)[] Folk=
            {
                ("BrekkBody",new(39.8f,13.6f),200,new(.92f,.94f,1f),"a sack",1),("OdrunBody",new(30.6f,14.4f),160,new(1f,.96f,.9f),"a slate",1),
                ("TovraBody",new(-14,-2.8f),160,new(1f,.98f,.94f),"a net",1),
                // The gate's sentry keeps an eye on the party's parley flag; the sergeant by the harbourmaster's house keeps the quay's tally at his desk (both stood facing open paving).
                ("MusterPikeguard",new(-.6f,15.8f),155,new(1,1,1),"",1),("MusterSergeant",new(21.4f,18.6f),190,new(1,1,1),"",1),
                ("TovraBody",new(-48.6f,43),170,new(.94f,.98f,1.04f),"a basket",1),("DraskBody",new(-4.6f,44.6f),200,new(.86f,.84f,.86f),"a pot",1),
                ("BrekkBody",new(23.6f,50.2f),340,new(1.02f,.96f,.9f),"a hammer",1),("TovraBody",new(36.6f,50.2f),20,new(.92f,.92f,.98f),"tongs",1),("PipBody",new(13.2f,45.6f),210,new(.96f,1f,1.04f),"",.84f),
                ("MusterPikeguard",new(28.4f,71.2f),220,new(.96f,.96f,1),"",1),("MusterSergeant",new(-48.6f,97.8f),133,new(1,1,1),"",1),
                ("OdrunBody",new(-48.6f,68.8f),150,new(.94f,.98f,1.04f),"a basket",1),("BrekkBody",new(-46.2f,70.6f),210,new(.9f,.92f,.98f),"",1),
                ("MusterScav",new(57.4f,123.4f),20,new(1,1,1),"",1),("DraskBody",new(-28.4f,96),200,new(.92f,.9f,.92f),"a tray",1),
                // ---- the town at its work on the day of a duel (the look pass of November 14): every one of these stands in a body, off the ways by more than a body's reach, off the
                // prompts' floor and the camera's line to them, off the errands and the drill (checked as placed by the first build with Town.Free, which left none of them out)

                // the quay
                ("BrekkBody",new(-44.4f,1.6f),80,new(.9f,.92f,.98f),"a sack",1),("TovraBody",new(-20.4f,1.8f),90,new(1.02f,.96f,.9f),"",1),("DraskBody",new(-18.8f,2),270,new(.86f,.86f,.92f),"",1),
                ("BrekkBody",new(-12.4f,2.4f),180,new(.96f,.92f,.88f),"",1),("OdrunBody",new(-4.4f,3.6f),200,new(.94f,.98f,1.04f),"a basket",1),
                ("TovraBody",new(16.4f,.4f),260,new(.92f,.94f,1f),"a sack",1),("BrekkBody",new(20.6f,.8f),90,new(1f,.94f,.9f),"",1),("OdrunBody",new(22.2f,.6f),270,new(.96f,.94f,1f),"a jar",1),
                ("DraskBody",new(40.4f,4.2f),300,new(.9f,.9f,.94f),"a sack",1),("BrekkBody",new(-33.4f,12.6f),200,new(.94f,.92f,.9f),"a sack",1),("TovraBody",new(-42.8f,4.6f),300,new(1f,.98f,.96f),"a net",1),
                // the family quarter
                ("OdrunBody",new(-34.2f,44.2f),250,new(1.04f,.98f,.92f),"a jar",1),("TovraBody",new(-35.8f,43.6f),70,new(.96f,1f,1.04f),"",1),
                ("BrekkBody",new(-44.4f,44.6f),80,new(.98f,.94f,.9f),"",1),("OdrunBody",new(-42.8f,45),260,new(.92f,.96f,1.02f),"a basket",1),
                ("DraskBody",new(-16.6f,45.8f),250,new(.9f,.88f,.9f),"a hammer",1),("TovraBody",new(-4.2f,40.8f),160,new(1.02f,.98f,.94f),"a basket",1),
                // the forges: apprentices at the burning hearths, a smith idle at each cold one
                ("PipBody",new(20.2f,51.9f),30,new(1f,.96f,.92f),"tongs",.9f),("PipBody",new(33.2f,51.9f),30,new(.96f,.96f,1.02f),"tongs",.9f),
                ("BrekkBody",new(17.4f,50.4f),190,new(.88f,.86f,.88f),"",1),("DraskBody",new(30.4f,50.4f),170,new(.86f,.84f,.86f),"",1),
                // sentries on the bastion and the harbour wall
                ("MusterPikeguard",new(62.4f,118),90,new(1,1,1),"a pike",1),("MusterPikeguard",new(58,124.6f),0,new(.96f,.96f,1),"a pike",1),("MusterPikeguard",new(46.6f,73),90,new(1,1,1),"a pike",1),
                // the forecourt: two reading the orders, two soldiers by the field's corner; two more by the armoury
                ("TovraBody",new(-41.8f,69.2f),0,new(.96f,.98f,1.02f),"",1),("BrekkBody",new(-39.4f,69.4f),350,new(1f,.96f,.92f),"",1),
                ("MusterPikeguard",new(-14,69.4f),60,new(.96f,.96f,1),"a pike",1),("BrekkBody",new(-12.6f,70.4f),240,new(.92f,.94f,.98f),"",1),("MusterPikeguard",new(12.8f,70.2f),80,new(1,1,1),"a pike",1),("MusterSergeant",new(14.4f,70.4f),260,new(1,1,1),"",1),
                ("MusterPikeguard",new(28.8f,80.6f),60,new(.96f,.96f,1),"a pike",1),("MusterSergeant",new(30.4f,81.6f),240,new(1,1,1),"",1),
                // the cadet yard: three at the pells, their instructor by the ranks
                ("PipBody",new(-46,93.2f),0,new(.94f,.96f,1f),"a pike",.84f),("PipBody",new(-42,93.2f),0,new(1f,.96f,.94f),"a pike",.84f),("PipBody",new(-38,93.2f),0,new(.96f,1f,.96f),"a pike",.84f),
                ("MusterSergeant",new(-27.6f,80.4f),270,new(1,1,1),"",1),
                // looking at the trophies
                ("OdrunBody",new(-49,107.4f),0,new(.98f,.96f,1.02f),"",1),("BrekkBody",new(-37.4f,107.2f),350,new(.94f,.94f,.98f),"",1),("TovraBody",new(-25.4f,107.6f),10,new(1.02f,.98f,.94f),"",1),("OdrunBody",new(37,107.4f),350,new(.96f,.96f,1.02f),"a basket",1),
                ("MusterPikeguard",new(46.2f,86.4f),90,new(.96f,.96f,1),"a pike",1),
            };
            /// <summary>Children: playing parley on the family stair's foot (one is the Shapers, and is being pelted with washing pegs), and two at the cadet yard's trough.</summary>
            static readonly (string body,Vector2 at,float yaw,Color tint,float scale)[] Children=
            {
                ("PipBody",new(-41.4f,52.6f),60,new(1.04f,.98f,.94f),.68f),("TovraBody",new(-36.6f,54.4f),280,new(.96f,1f,1.06f),.62f),("OdrunBody",new(-40.8f,55.6f),160,new(.94f,.96f,1.04f),.62f),
                ("OdrunBody",new(-27.6f,94.4f),120,new(1.02f,1f,.96f),.64f),
            };
            /// <summary>People on errands (WalkLoop): body, the loop on the plan, pace, the wait at each stop, tint, there-and-back, and what they are about. Each loop is off every way and
            /// checked clear of anything solid as built.</summary>
            static readonly (string body,Vector2[] loop,float speed,float pace,Color tint,bool back,string name)[] Errands=
            {
                ("BrekkBody",new[]{new Vector2(14,4.4f),new Vector2(28,5)},.9f,2.6f,new(.96f,.92f,.88f),true,"A docker with a sack"),
                ("OdrunBody",new[]{new Vector2(-48,13.6f),new Vector2(-36,12.6f)},.8f,3,new(.92f,.96f,1.02f),true,"A woman carrying a jar to the store"),
                ("TovraBody",new[]{new Vector2(-50.2f,40.6f),new Vector2(-42,40.4f)},.8f,2.8f,new(1.02f,.96f,.92f),true,"A mother taking down the washing, with a basket"),
                ("DraskBody",new[]{new Vector2(-21.2f,38.2f),new Vector2(-15.8f,38.4f)},.85f,2.4f,new(.88f,.86f,.9f),true,"A cook carrying a pot"),
                ("BrekkBody",new[]{new Vector2(19.6f,46),new Vector2(26.4f,46)},.9f,2.2f,new(.94f,.94f,1f),true,"A smith carrying pikes"),
                ("MusterPikeguard",new[]{new Vector2(-32,62),new Vector2(-14,62.4f)},1,2,new(1,1,1),true,"A soldier on his round"),
                ("OdrunBody",new[]{new Vector2(22.8f,106.8f),new Vector2(28.4f,106.8f)},.6f,4,new(1.04f,1f,.96f),true,"An old woman looking at the cases"),
            };
            static GameObject Body(ZoneKit kit,string id,Vector3 at,float yaw,Color tint,bool solid=true)
            {
                if(ZoneKit.Prefab("Characters",id)!=null){var one=kit.Townsperson(id,at,yaw,tint,"BrekkBody");if(!solid)foreach(var c in one.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);return one;}
                // The garrison wear the Muster's frames: the same standing as a townsperson, from the enemies' bodies.
                var person=new GameObject("Townsperson");person.transform.SetPositionAndRotation(kit.On(at),Quaternion.Euler(0,yaw,0));person.isStatic=solid;
                var prefab=ZoneKit.Prefab("Enemies",id)??ZoneKit.Prefab("Characters","BrekkBody");var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);model.transform.SetParent(person.transform,false);model.name="Generated civilian";
                foreach(var b in model.GetComponentsInChildren<GroundLifecycleSupport>())UnityEngine.Object.DestroyImmediate(b);
                if(id=="MusterSergeant"||id=="MusterScav")foreach(var part in model.GetComponentsInChildren<Renderer>(true))part.sharedMaterials=part.sharedMaterials.Select(PlainRim).ToArray();
                if(solid){var box=person.AddComponent<BoxCollider>();box.size=new Vector3(.7f,1.9f,.7f);box.center=Vector3.up*.95f;}
                person.AddComponent<PropTint>().tint=tint;
                foreach(var rig in model.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullCompletely;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
                return person;
            }
            /// <summary>A garrison frame worn in town takes the Pikeguard's plain rim ((.25, .25, .3), power 4, none in shade), saved as a material of its own so the player draws what
            /// the editor does. The Sergeant's and the Scav's hot orange rim was made for the Lantern Road's dark floors; under PropTint's base of 1 it covered the drill sergeant on
            /// the muster field, a glowing shell (whole-game capture of November 22, s51/147, s51/180).</summary>
            static Material PlainRim(Material source)
            {
                if(source==null||!source.HasProperty("_RimColor"))return source;
                const string folder="Assets/_Project/Art/Generated/Harrowgate";var path=$"{folder}/{source.name}_Town.mat";
                var plain=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(plain==null){Directory.CreateDirectory(folder);plain=new Material(source);AssetDatabase.CreateAsset(plain,path);}
                else{plain.shader=source.shader;plain.CopyPropertiesFromMaterial(source);plain.shaderKeywords=source.shaderKeywords;}
                plain.SetColor("_RimColor",new Color(.25f,.25f,.3f));plain.SetFloat("_RimPower",4);if(plain.HasProperty("_RimUnlit"))plain.SetFloat("_RimUnlit",0);
                EditorUtility.SetDirty(plain);return plain;
            }
            /// <summary>What someone is carrying or holding, which is how a still says what they are doing.</summary>
            static void Carry(ZoneKit kit,GameObject who,string doing)
            {
                if(string.IsNullOrEmpty(doing))return;
                ZoneKit.Shape load=new();Material[] stuff;
                // A sack of sacking over the shoulder, its neck tied (an untextured pale lump until November 21).
                if(doing.Contains("sack")){Sack(load,new Vector3(-.1f,1.36f,.05f),.6f,new Color(.84f,.76f,.6f),3,Quaternion.Euler(0,0,-70));stuff=new[]{Burlap};}
                else if(doing.Contains("basket")){load.Slot(0).Tube(new[]{new Vector3(.32f,.78f,.12f),new Vector3(.32f,1.06f,.12f)},new[]{.24f,.3f},10,new Color(.66f,.56f,.42f),new Color(.78f,.66f,.48f),2,.8f).Slot(1).Lump(new Vector3(.32f,1.08f,.12f),new Vector3(.26f,.1f,.26f),2,.3f,new Color(.58f,.58f,.6f),new Color(.82f,.8f,.74f),8,3);stuff=new[]{Timber,Cloth};}
                else if(doing.Contains("net")){load.Lump(new Vector3(0,.95f,.4f),new Vector3(.5f,.2f,.3f),4,.4f,new Color(.7f,.66f,.54f),new Color(.5f,.46f,.38f),10,4);stuff=new[]{Cloth};}
                else if(doing.Contains("slate")){load.Box(new Vector3(.18f,1.2f,.38f),new Vector3(.4f,.3f,.03f),Quaternion.Euler(-30,0,0),new Color(.22f,.22f,.24f));stuff=new[]{Iron};}
                else if(doing.Contains("hammer")||doing.Contains("tongs")){load.Tube(new[]{new Vector3(.34f,.9f,.18f),new Vector3(.36f,1.4f,.42f)},new[]{.03f,.03f},5,new Color(.5f,.42f,.34f),new Color(.4f,.38f,.38f),1,1).Box(new Vector3(.36f,1.44f,.44f),new Vector3(.12f,.12f,.24f),Quaternion.identity,new Color(.3f,.3f,.32f));stuff=new[]{Iron};}
                else if(doing.Contains("pikes")){for(int k=0;k<3;k++)load.Tube(new[]{new Vector3(.3f+k*.06f,.6f,-1.2f),new Vector3(.3f+k*.06f,1.5f,1.4f)},new[]{.03f,.03f},5,new Color(.56f,.46f,.36f),new Color(.66f,.66f,.68f),1,1);stuff=new[]{Iron};}
                else if(doing.Contains("pike")){load.Tube(new[]{new Vector3(.36f,0,.1f),new Vector3(.36f,3,.1f),new Vector3(.36f,3.5f,.1f)},new[]{.035f,.03f,.01f},5,new Color(.5f,.42f,.34f),new Color(.72f,.72f,.74f),1,1);stuff=new[]{Iron};}
                // A copper cook-pot held before them, and below an iron kettle on its bail: a flat grey cylinder each until November 21 (s50/201, s52/254).
                else if(doing.Contains("pot")){CookPot(load,new Vector3(0,.98f,.44f),.5f);stuff=new[]{Iron};}
                else if(doing.Contains("jar")){load.Tube(new[]{new Vector3(.3f,1.5f,0),new Vector3(.3f,1.86f,0),new Vector3(.3f,2,0)},new[]{.16f,.18f,.08f},10,new Color(.6f,.44f,.34f),new Color(.7f,.52f,.4f),1,1);stuff=new[]{Timber};}
                else if(doing.Contains("tray")){load.Slot(0).Box(new Vector3(0,1.08f,.42f),new Vector3(.66f,.06f,.42f),Quaternion.identity,new Color(.6f,.48f,.36f)).Slot(1).Lump(new Vector3(0,1.16f,.42f),new Vector3(.24f,.08f,.14f),3,.3f,new Color(.82f,.66f,.44f),new Color(.66f,.5f,.32f),8,3);stuff=new[]{Timber,Cloth};}
                else if(doing.Contains("kettle")){Kettle(load,new Vector3(.34f,.5f,.12f),.56f);stuff=new[]{Iron};}
                else return;
                var carried=kit.Built(who.name+" carries",load,stuff);carried.transform.SetParent(who.transform,false);carried.transform.localPosition=Vector3.zero;carried.transform.localRotation=Quaternion.identity;
                carried.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            static void People(ZoneKit kit)
            {
                foreach(var (body,at,yaw,tint,doing,scale) in Folk)
                {
                    var one=Body(kit,body,new Vector3(at.x,0,at.y),yaw,tint);one.name="Townsperson, "+(string.IsNullOrEmpty(doing)?"at their work":"with "+doing);
                    if(scale!=1)one.transform.localScale=Vector3.one*scale;Carry(kit,one,doing);
                }
                foreach(var (body,at,yaw,tint,scale) in Children){var child=Body(kit,body,new Vector3(at.x,0,at.y),yaw,tint);child.transform.localScale=Vector3.one*scale;child.name="A child playing parley";}
                // The pegs on the stair: the Shapers' share of them.
                var pegs=new ZoneKit.Shape();var rng=new System.Random(13);
                for(int k=0;k<14;k++){var p=At(kit,new Vector2(-38.6f+((float)rng.NextDouble()-.5f)*3.6f,53.6f+((float)rng.NextDouble()-.5f)*2.4f),.02f);pegs.Box(p,new Vector3(.08f,.03f,.02f),Quaternion.Euler(0,(float)rng.NextDouble()*180,0),new Color(.8f,.72f,.58f));}
                kit.Built("Floor detail washing pegs",pegs,Timber).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                foreach(var (body,loop,speed,pause,tint,back,name) in Errands)
                {
                    WalkLoop walker;
                    if(ZoneKit.Prefab("Characters",body)!=null)walker=kit.Walker(body,loop,speed,pause,tint,"BrekkBody");
                    else
                    {
                        var person=new GameObject("Townsperson on an errand");person.transform.position=kit.On(loop[0].x,loop[0].y);
                        var model=(GameObject)PrefabUtility.InstantiatePrefab(ZoneKit.Prefab("Enemies",body)??ZoneKit.Prefab("Characters","BrekkBody"));model.transform.SetParent(person.transform,false);model.name="Generated civilian";
                        foreach(var b in model.GetComponentsInChildren<GroundLifecycleSupport>())UnityEngine.Object.DestroyImmediate(b);
                        var solid=person.AddComponent<CapsuleCollider>();solid.height=1.9f;solid.radius=.35f;solid.center=Vector3.up*.95f;person.AddComponent<PropTint>().tint=tint;
                        foreach(var rig in model.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullCompletely;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
                        walker=person.AddComponent<WalkLoop>();walker.path=loop.Select(p=>kit.On(p.x,p.y)).ToArray();walker.speed=speed;walker.pause=pause;
                    }
                    walker.thereAndBack=back;walker.name=name;if(body=="PipBody")walker.transform.localScale=Vector3.one*.84f;
                    Carry(kit,walker.gameObject,name.Contains("sack")?"a sack":name.Contains("jar")?"a jar":name.Contains("basket")?"a basket":name.Contains("pot")?"a pot":name.Contains("pikes")?"pikes":name.Contains("soldier")?"a pike":"");
                }
            }
            // ---------------------------------------------------------------- the town at its work (the look pass of November 14)
            /// <summary>Every place a prompt is pressed or a named person stands: nobody new stands within three metres of one, or in the camera's line just south of one.</summary>
            static Vector2[] Stands=>new[]{Arrival,Pad,RoadArch,GateFoot,GateHead,HessaGate,VessaGate,PipWaits,PipFirstAt,Scales,FlourAt,WashingAt,KitchenDoor,OdrunAt,TableAt,TovraAt,BrekkAt,StairFoot,StairGameAt,
                ForgesAt,MenderAt,ArmourersSpawn,KettleAt,AnvilAt,SaltAt,FieldSpawn,VerdictSpawn,FieldCradle,LaunchPad,PipFieldAt,F,GuardPost,PipThirdFile,BellAt,WreckAt,DraskAt,NamesAt,Case3,Case1,DoorCase,Case2,
                Lookout,ArmouryGate,HessaArmoury,StairMid,StairFootAt,CellAt,CellFront,OdrunVerdictAt,DraskVerdictAt,PipAfterAt};
            static readonly List<Vector2> Placed=new();static readonly List<(Vector2 a,Vector2 b)> Drill=new();
            static float ToSegment(Vector2 p,Vector2 a,Vector2 b){var ab=b-a;float t=ab.sqrMagnitude<1e-4f?0:Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);return Vector2.Distance(p,a+ab*t);}
            /// <summary>Whether something new may stand at <paramref name="p"/>: level floor, <paramref name="fromWays"/> off every way, clear of the prompts and the camera's line to them,
            /// the signs, the errands and the drill, everyone already placed, and anything solid. What can't is left out and logged (HARROWGATE_LIFE_SKIP), not forced.</summary>
            static bool Free(ZoneKit kit,Vector2 p,float fromWays,string what,float radius=.32f)
            {
                string why=null;float y=kit.Y(p.x,p.y),spread=0;
                foreach(var (dx,dz) in new[]{(.6f,0f),(-.6f,0f),(0f,.6f),(0f,-.6f)})spread=Mathf.Max(spread,Mathf.Abs(kit.Y(p.x+dx,p.y+dz)-y));
                if(Land(p)<.6f||spread>.2f||y>CliffTop)why="not on level floor";
                else if(FromWays(p)<fromWays)why="on a way";
                else if(Stands.Any(s=>Vector2.Distance(s,p)<3||(p.y<s.y&&p.y>s.y-6&&Mathf.Abs(p.x-s.x)<1.6f)))why="by a prompt, or between the camera and one";
                else if(Signs.Any(s=>Vector2.Distance(s.at,p)<s.half+1))why="on a sign";
                else if(Errands.Any(e=>Enumerable.Range(1,e.loop.Length-1).Any(i=>ToSegment(p,e.loop[i-1],e.loop[i])<1.8f)))why="on an errand";
                else if(Drill.Any(d=>ToSegment(p,d.a,d.b)<1.4f))why="in the drill";
                else if(Placed.Any(q=>Vector2.Distance(q,p)<1.2f))why="on someone";
                else if(Physics.CheckCapsule(new Vector3(p.x,y+radius+.06f,p.y),new Vector3(p.x,y+1.6f,p.y),radius,~0,QueryTriggerInteraction.Ignore))why="in something solid";
                if(why!=null){Debug.Log($"HARROWGATE_LIFE_SKIP {what} at ({p.x:0.0}, {p.y:0.0}): {why}");return false;}
                Placed.Add(p);return true;
            }
            /// <summary>Iron braziers burning at the town's gathering places (the gate's head, the field's four corners and its south opening, the oath hall's podium): warm pools against
            /// the cold stone, so the eye goes where the town gathers.</summary>
            static void Braziers(ZoneKit kit)
            {
                Placed.Clear();Drill.Clear();Placed.AddRange(Folk.Select(f=>f.at));Placed.AddRange(Children.Select(c=>c.at));Physics.SyncTransforms();
                var shape=new ZoneKit.Shape();var holder=new GameObject("Braziers");Color iron=new(.24f,.24f,.26f);int lit=0;var solids=new List<Vector3>();
                foreach(var at in new[]{new Vector2(3.4f,30.8f),new Vector2(14.6f,27.4f),new Vector2(-3.6f,69),new Vector2(3.6f,69),new Vector2(-22.8f,72.6f),new Vector2(19.6f,72.8f),new Vector2(-20.6f,98.4f),
                    new Vector2(18.6f,95.6f),new Vector2(-14.8f,107.4f),new Vector2(14.8f,107.4f)})
                {
                    if(!Free(kit,at,2.4f,"a brazier",.5f))continue;
                    var f=kit.On(at.x,at.y);
                    for(int k=0;k<3;k++){float a=k*Mathf.PI*2/3;shape.Slot(0).Beam(f+new Vector3(Mathf.Cos(a)*.42f,0,Mathf.Sin(a)*.42f),f+new Vector3(Mathf.Cos(a)*.3f,.86f,Mathf.Sin(a)*.3f),.04f,.04f,iron);}
                    shape.Slot(0).Tube(new[]{f+Vector3.up*.78f,f+Vector3.up*.9f,f+Vector3.up*1.02f},new[]{.22f,.4f,.46f},14,iron,new Color(.36f,.34f,.32f),1,1);
                    // The fire is the chapter's (HarrowgateBuilder.Hearths): a bed of coals in the bowl with the fire in its gaps, flame sheets leaning back to the camera, embers and
                    // a little smoke going up, and a light that breathes with the coals. The town's own five tongues read as one flat yellow card at the play camera (s53/014).
                    Hearths.Fire(kit,"Brazier fire "+(lit+1),f+Vector3.up*.98f,.36f,.95f,8,2,(int)(at.x*31+at.y*7)).transform.SetParent(holder.transform,true);
                    solids.Add(f+Vector3.up*.55f);lit++;
                }
                var burning=kit.Built("Braziers drawn",shape,Iron);burning.transform.SetParent(holder.transform,true);
                // Each bowl's solid under the drawn bowls: seated under the holder's first drawn child, every block went under the first fire's coals, and the check took those
                // coals for floating (their only support was their own child).
                foreach(var c in solids)ZoneKit.Block(burning,c,new Vector3(.9f,1.1f,.9f),0,"Brazier");
                foreach(var r in burning.GetComponentsInChildren<MeshRenderer>())r.shadowCastingMode=ShadowCastingMode.Off;
                Debug.Log($"HARROWGATE_BRAZIERS {lit}");
            }
            /// <summary>Marks laid flat in the floor, each district's own and wide enough for the fixed camera (the ground kit's survey line read as a hair): the town's two marching walks
            /// carry a runner of dark slate edged in pale stone (the field's own stones, the Muster's way to it); the cadet yard is ruled in black for the ranks with a border; the
            /// bastion's gun has its traverse rails round a pivot plate; the wall walk has its drain, with a grate every few metres. No orange, red or purple: on a floor those say danger
            /// (D217). Drawn only, "Floor detail" so the checks read them as flat.</summary>
            static void FloorMarks(ZoneKit kit)
            {
                var paint=new ZoneKit.Shape();var iron=new ZoneKit.Shape();Color ink=new(.1f,.1f,.12f),rail=new(.2f,.2f,.22f);
                void Band(ZoneKit.Shape shape,Vector2 a,Vector2 b,float wide,Color colour,float lift)
                {
                    var along=(b-a).normalized;var side=new Vector2(-along.y,along.x)*wide*.5f;int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/1.5f));
                    for(int k=0;k<n;k++)
                    {
                        var p0=Vector2.Lerp(a,b,k/(float)n);var p1=Vector2.Lerp(a,b,(k+1)/(float)n);
                        shape.Quad(kit.On(p0.x-side.x,p0.y-side.y,lift),kit.On(p1.x-side.x,p1.y-side.y,lift),kit.On(p1.x+side.x,p1.y+side.y,lift),kit.On(p0.x+side.x,p0.y+side.y,lift),Vector3.up,colour,.25f);
                    }
                }
                // The lower walk (flat end to end) and the forecourt's walk, which stops short of the three ramps' heads (a band across a ramp's edge would float): laid in the runner's
                // own stone, slate slabs between kerbstones (a smooth lavender band with white edges until November 21, s52/254).
                Walks(kit);
                // The cadet yard ruled for the ranks: a line every 2.4 m and its border.
                for(float z=73.2f;z<97.5f;z+=2.4f)Band(paint,new Vector2(-52,z),new Vector2(-27.2f,z),.16f,ink,.016f);
                foreach(var (a,b) in new[]{(new Vector2(-52.4f,72.4f),new Vector2(-52.4f,98.2f)),(new Vector2(-26.8f,72.4f),new Vector2(-26.8f,98.2f)),(new Vector2(-52.4f,72.4f),new Vector2(-26.8f,72.4f)),(new Vector2(-52.4f,98.2f),new Vector2(-26.8f,98.2f))})
                    Band(paint,a,b,.24f,ink,.016f);
                // The bastion's gun: a pivot plate and two traverse rails swinging through the harbour side.
                var gun=new Vector2(60.2f,121.4f);
                iron.Disc(kit.On(gun.x,gun.y,.02f),Vector3.up,.6f,16,rail,0,null,.25f);
                foreach(float r in new[]{2.4f,3.1f})for(int k=0;k<18;k++)
                {
                    float a0=(-95+k*10.5f)*Mathf.Deg2Rad,a1=(-95+(k+1)*10.5f)*Mathf.Deg2Rad;
                    Band(iron,gun+new Vector2(Mathf.Cos(a0),Mathf.Sin(a0))*r,gun+new Vector2(Mathf.Cos(a1),Mathf.Sin(a1))*r,.14f,rail,.02f);
                }
                // The wall walk's drain along the parapet's foot, and a grate across it every three metres.
                Band(iron,new Vector2(46.72f,63.4f),new Vector2(46.72f,111.6f),.36f,ink,.012f);
                for(float z=64.6f;z<111;z+=3)Band(iron,new Vector2(46.5f,z),new Vector2(46.94f,z),.08f,rail,.018f);
                kit.Built("Floor detail drill lines",paint,Inlay).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                kit.Built("Floor detail gun tracks and drain",iron,Iron).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            static void Life(ZoneKit kit)
            {
                Physics.SyncTransforms();var holder=new GameObject("The town at its work");int stood=0,marched=0;
                // The drill: three ranks of five marching up and down the cadet yard in step (the same pace and the same wait), pikes shouldered.
                var cadetBody=ZoneKit.Prefab("Characters","PipBody")!=null?"PipBody":"BrekkBody";
                for(int rank=0;rank<3;rank++)for(int file=0;file<5;file++)
                {
                    float x=-36.4f+file*1.4f,z0=74.4f+rank*1.6f;var a=new Vector2(x,z0);var b=new Vector2(x,z0+8);
                    if(FromWays(a)<1.8f||FromWays(b)<1.8f){Debug.Log($"HARROWGATE_LIFE_SKIP a cadet's march at ({x:0.0}, {z0:0.0}): on a way");continue;}
                    var start=kit.On(a.x,a.y)+Vector3.up*.95f;var run=kit.On(b.x,b.y)+Vector3.up*.95f-start;
                    if(Physics.SphereCastAll(start,.4f,run.normalized,run.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>h.collider.GetComponentInParent<WalkLoop>()==null)){Debug.Log($"HARROWGATE_LIFE_SKIP a cadet's march at ({x:0.0}, {z0:0.0}): blocked");continue;}
                    var cadet=kit.Walker(cadetBody,new[]{a,b},1.3f,1.2f,new Color(.94f+.02f*file,.96f,1f-.02f*rank),"BrekkBody");cadet.thereAndBack=true;cadet.name="A cadet at drill in the yard";
                    cadet.transform.localScale=Vector3.one*.84f;cadet.transform.SetParent(holder.transform,true);Carry(kit,cadet.gameObject,"a pike");Drill.Add((a,b));marched++;
                }
                // A crew at the Warmaster's skiff on its cradle, gone with it once he takes it.
                var crew=new GameObject("The skiff's crew");crew.transform.SetParent(holder.transform,true);
                foreach(var (at,yaw,doing) in new[]{(new Vector2(27.2f,91.2f),60f,"a hammer"),(new Vector2(32.9f,91),300f,"tongs")})
                {
                    if(!Free(kit,at,2.2f,"a skiff hand"))continue;
                    var one=Body(kit,"BrekkBody",new Vector3(at.x,0,at.y),yaw,new Color(.9f,.92f,.96f));one.name="Townsperson, at the skiff with "+doing;Carry(kit,one,doing);one.transform.SetParent(crew.transform,true);stood++;
                }
                Gate("The skiff's crew until he takes it","vask.duelWon",false,crew);
                // The garrison's gaoler at the cells, until the town has its answer (from vask.deposed the Unsworn keep them).
                if(Free(kit,new Vector2(61.6f,107.6f),2.2f,"the gaoler"))
                {var gaoler=Body(kit,"MusterSergeant",new Vector3(61.6f,0,107.6f),290,Color.white);gaoler.name="Townsperson, the garrison's gaoler";gaoler.transform.SetParent(holder.transform,true);
                    Gate("The garrison's gaoler until the town has its answer","vask.deposed",false,gaoler);stood++;}
                Debug.Log($"HARROWGATE_LIFE stood={stood} marched={marched}");
            }
            /// <summary>Every errand's loop clear of anything solid at a body's height: a walker stands still for good at whatever is in its way.</summary>
            static void ErrandsWalkClear(ZoneKit kit)
            {
                Physics.SyncTransforms();var blocked=new List<string>();
                foreach(var (_,loop,_,_,_,back,name) in Errands)
                    for(int k=0;k<loop.Length-(back?1:0);k++)
                    {
                        var a=kit.On(loop[k].x,loop[k].y)+Vector3.up*.95f;var next=loop[(k+1)%loop.Length];var b=kit.On(next.x,next.y)+Vector3.up*.95f;var run=b-a;run.y=0;
                        foreach(var hit in Physics.SphereCastAll(a,.4f,run.normalized,run.magnitude,~0,QueryTriggerInteraction.Ignore))
                            if(hit.collider.GetComponentInParent<WalkLoop>()==null&&hit.collider.GetComponentInParent<Npc>()==null)blocked.Add($"{name}: {hit.collider.name} at ({hit.point.x:0.0}, {hit.point.z:0.0})");
                    }
                if(blocked.Count>0)throw new InvalidOperationException("Harrowgate: an errand runs into something: "+string.Join("; ",blocked.Distinct()));
            }

            // ---------------------------------------------------------------- the chapter's story, wired into the town (manifest sections 2 and 8)
            static void Where(string name,GameObject who,string from,string awayFrom,string backAt,string goneAt,bool live)
            {
                var where=new GameObject(name,typeof(Whereabouts)).GetComponent<Whereabouts>();where.from=from??"";where.awayFrom=awayFrom??"";where.backAt=backAt??"";where.goneAt=goneAt??"";where.live=live;where.targets=new[]{who};
                // As the town stands before any of the story's flags: the stills and the bake see it as a player first does (the verdict's crowd stood in the first stills).
                who.SetActive(string.IsNullOrEmpty(from));
            }
            static void All(string name,string result,params string[] flags){var all=new GameObject(name,typeof(FlagAll)).GetComponent<FlagAll>();all.flags=flags;all.result=result;}
            static Npc Person(ZoneKit kit,string id,Vector2 at,float yaw,string first,string repeat)
            {
                var npc=kit.Person(id,new Vector3(at.x,0,at.y),yaw,"",false,null,null,first);npc.repeatNode=repeat??"";return npc;
            }
            static void Story(ZoneKit kit)
            {
                // ---- the harbour gate: Vessa on her way in, the sisters overheard across the quay, Hessa and the parley.
                var vessa=Body(kit,"VessaBody",new Vector3(VessaGate.x,0,VessaGate.y),250,Color.white,false);vessa.name="Vessa at the harbour gate";vessa.isStatic=false;
                var leave=vessa.AddComponent<LeavesAfter>();leave.flag="vask.sistersHeard";leave.path=new[]{kit.On(16.8f,17.4f),kit.On(DoorX,18.9f)};
                kit.Crossing("The sisters at the harbour gate",new Vector3(-12,0,8),new Vector3(4,5,28),"vask.sistersHeard","HessaSisters","Hessa","vask.roadSaid");
                var hessa=Person(kit,"Hessa",HessaGate,225,"HessaParley","HessaGateRepeat");
                Where("Hessa is on the harbour gate",hessa.gameObject,null,null,null,"vask.deposed",true);
                var hessaArmoury=Person(kit,"Hessa",HessaArmoury,135,"HessaParley","HessaGateRepeat");
                Where("Hessa is at the armoury gate",hessaArmoury.gameObject,"vask.deposed",null,null,null,true);
                // ---- Pip: waiting inside the gate; his first word at the ramp's head; following the party until the oath bell, then in the third file by the drain.
                // His first word is a crossing, and only a talk with an Npc marks someone met: his beat after the duel (SideStories) waits on it.
                All("Pip has been met","met.Pip","vask.pipMet");
                kit.Crossing("Pip at the gate",new Vector3(PipFirstAt.x,0,PipFirstAt.y),new Vector3(9,5,2.4f),"vask.pipMet","PipFirst","Pip","vask.parley");
                var pip=new GameObject("Pip");pip.transform.SetPositionAndRotation(At(kit,PipWaits),Quaternion.Euler(0,200,0));
                var pipBody=(GameObject)PrefabUtility.InstantiatePrefab(ZoneKit.Prefab("Characters","PipBody")??ZoneKit.Prefab("Characters","BrekkBody"));pipBody.transform.SetParent(pip.transform,false);pipBody.name="Generated civilian";
                var pace=pip.AddComponent<KeepsPace>();pace.goneFlag="vask.deposed";pace.gap=7;pace.calm=14;
                pace.stages=new[]
                {
                    new KeepsPace.Stage{when="",point=At(kit,PipWaits),faces=At(kit,GateHead)},
                    new KeepsPace.Stage{when="vask.pipMet",follows=true},
                    new KeepsPace.Stage{when="vask.draskMet",point=At(kit,PipThirdFile),faces=At(kit,F)},
                };
                // His talks are the places he stops at. Offered only while he is with the party: from his first word until the oath bell.
                var places=new GameObject("Pip's places");
                DiscoveryPoint Stop(string name,Vector2 at,string prompt,string flag,string node,string quest=null,int step=0)
                {var p=kit.Discovery(name,new Vector3(at.x,0,at.y),prompt,flag,"",node,"Pip",quest,step,2.6f);p.requiredFlag="vask.pipMet";p.transform.SetParent(places.transform,true);return p;}
                // The oath's two talks are Flag steps their own lines set, so they aren't gated on the quest's step (a point offered at a step must answer an Interact step:
                // EveryConversationLockAndQuestStepIsWiredToSomethingInTheWorld). Their order is kept by flags: the gallery's talk waits for the field's.
                Stop("Pip at the field's wall",PipFieldAt,"Look out over the muster field","pip.fieldTalked","PipField");
                Stop("Pip at the case with the door",DoorCase,"Look at the case with the door","pip.galleryTalked","PipGallery").requiredFlag="pip.fieldTalked";
                Stop("Pip at the wall of names",NamesAt,"Look at the wall of names","pip.hallSeen","PipOathHall");
                Stop("Pip under the washing",WashingAt,"Look up at the washing","pip.washingSeen","PipWashing");
                Stop("Pip on the family stair",StairGameAt,"Watch the stair game","pip.gameSeen","PipStairGame");
                Stop("Pip on the armourers' street",ForgesAt,"Look along the forges","pip.forgesSeen","PipArmourers");
                Stop("Pip on the harbour wall",Lookout,"Look down at the harbour","pip.harbourSeen","PipHarbourWall");
                Gate("Pip's places, until the oath bell","vask.draskMet",false,places);
                // After the verdict he is at Odrun's door with the kettle she wanted carried.
                var pipAfter=Person(kit,"Pip",PipAfterAt,200,"PipOathDay","PipOathDay");pipAfter.postFlag="pip.oathLeft";pipAfter.postNode="PipPost";
                Carry(kit,pipAfter.gameObject,"a kettle");
                Where("Pip at Odrun's door",pipAfter.gameObject,"vask.deposed",null,null,null,true);

                // ---- the trophy gallery: Sela finds the glass; the cases open once Pip has said where the key is, or once the town has answered.
                kit.Crossing("The trophy gallery",new Vector3(-37,0,103.7f),new Vector3(34,5,9.4f),"vask.gallerySeen","SelaTrophyGallery","Sela","vask.parley");
                All("Pip has said where the key is","vask.casesOpen","pip.galleryTalked");All("The town has answered: the cases are anybody's","vask.casesOpen","vask.deposed");
                int n=0;
                foreach(var at in new[]{Case1,Case2,Case3})
                {
                    n++;var point=kit.Discovery("Glass case "+n,new Vector3(at.x,0,at.y),"Open the case","vask.glass"+n,"GlassCase",null,"Sela","RelayGlass",0,2.4f);
                    point.situation="glass";point.requiredFlag="vask.casesOpen";
                    var key=at==Case1?Cases[1].at:at==Case2?Cases[6].at:Case3;if(GlassIn.TryGetValue(key,out var glass)){point.visual=glass;}
                }
                // ---- Odrun's kitchen: Odrun at her range; the back table once the kitchen is stocked; Tovra at the table from the time the Warmaster has spoken; Brekk in the quarter.
                var odrun=kit.Person("Odrun",new Vector3(OdrunAt.x,0,OdrunAt.y),180,"odrun.kitchenStocked",true,null,null,"OdrunFirst");odrun.repeatNode="OdrunRepeat";odrun.postNode="OdrunShop";
                var tovra=Body(kit,"TovraBody",new Vector3(TovraAt.x,0,TovraAt.y),100,Color.white,false);tovra.name="Tovra at the kitchen table";tovra.isStatic=false;
                Where("Tovra has come in the back way",tovra,"vask.draskMet",null,null,"vask.deposed",true);
                var table=kit.Discovery("The kitchen table",new Vector3(TableAt.x,0,TableAt.y),"Sit at the kitchen table","vask.kitchenTable","","OdrunTovra","Odrun",null,0,2.4f);table.requiredFlag="vask.draskMet";
                var veteran=Body(kit,"DraskBody",new Vector3(-19.9f,0,47.6f),300,new Color(.82f,.82f,.86f));veteran.name="Townsperson, the one by the stove";
                Person(kit,"Brekk",BrekkAt,180,"BrekkKitchen","BrekkKitchen");
                foreach(var (body,at,yaw,tint) in new[]{("DraskBody",new Vector2(-29.2f,52.4f),90f,new Color(.96f,.94f,.98f)),("TovraBody",new Vector2(-20.4f,54.2f),250f,new Color(1.04f,1f,.94f))})
                {var one=Body(kit,body,new Vector3(at.x,0,at.y),yaw,tint);one.name="Townsperson, Unsworn in the quarter";}
                // Odrun's three things, each offered only at that step and shown only while it can be taken.
                foreach(var (name,at,prompt,flag,situation,look) in new[]{("Garrison salt",SaltAt,"Take the salt","odrun.salt","kitchen.salt","salt"),("Ash-root flour",FlourAt,"Take the flour","odrun.flour","kitchen.flour","flour"),("A regulation mess kettle",KettleAt,"Take the kettle","odrun.kettle","kitchen.kettle","kettle")})
                {
                    var point=kit.Discovery(name,new Vector3(at.x,0,at.y),prompt,flag,"KitchenWant",null,"Sela","OdrunsKitchen",0,2.4f);point.situation=situation;
                    var thing=new ZoneKit.Shape();var foot=At(kit,at+new Vector2(.9f,1.1f));
                    if(look=="kettle"){thing.Slot(0).Box(foot+Vector3.up*.35f,new Vector3(.7f,.7f,.6f),Quaternion.identity,new Color(.62f,.5f,.36f)).Slot(1);Kettle(thing,foot+Vector3.up*.7f,.7f);}
                    // Sacks of sacking with their necks tied, on a crate (s51/107, s51/208: untextured white lumps): the garrison's salt stencilled with the Muster's bar, the flour plain.
                    else{var tone=look=="salt"?new Color(.9f,.88f,.84f):new Color(.86f,.74f,.54f);thing.Slot(0).Box(foot+Vector3.up*.25f,new Vector3(.8f,.5f,.6f),Quaternion.identity,new Color(.58f,.46f,.34f)).Slot(1);
                        Sack(thing,foot+new Vector3(-.12f,.5f,0),.62f,tone,look.Length,Quaternion.Euler(0,20,8),look=="salt"?(Color?)new Color(.36f,.34f,.36f):null);Sack(thing,foot+new Vector3(.2f,.5f,.12f),.5f,ZoneKit.Shape.Dim(tone,.94f),look.Length+3,Quaternion.Euler(0,-30,-10));}
                    var visual=kit.Built(name+" to take",thing,Timber,look=="kettle"?Iron:Burlap);visual.transform.SetParent(point.transform,true);point.visual=visual;visual.SetActive(false);
                }

                // ---- the Warmaster: at his table from the time the town is seen; he receives them, and goes out to the field.
                All("The town has been seen","vask.townSeen","vask.odrunMet","vask.gallerySeen");
                var drask=Person(kit,"Drask",DraskAt,180,"DraskReceives","DraskReceives");
                var draskHere=new GameObject("The Warmaster at his table");drask.transform.SetParent(draskHere.transform,true);
                Where("The Warmaster comes to his table",draskHere,"vask.townSeen",null,null,null,true);
                var goes=drask.gameObject.AddComponent<LeavesAfter>();goes.flag="vask.draskMet";goes.path=new[]{At(kit,new Vector2(0,106.4f)),At(kit,new Vector2(0,99)),At(kit,new Vector2(10,92)),At(kit,new Vector2(17,85)),At(kit,new Vector2(24,85))};
                foreach(var (x,yaw) in new[]{(-4.4f,90f),(4.6f,270f)}){var s=Body(kit,"MusterPikeguard",new Vector3(x,0,110.4f),yaw,new Color(.96f,.96f,1));s.name="Townsperson, a soldier at the Warmaster's table";}

                // ---- the end: the armoury gate, the stair, the cell, and the radio.
                kit.Crossing("Hessa at the armoury gate",new Vector3(ArmouryGate.x,0,ArmouryGate.y),new Vector3(2.6f,5,3.6f),"vask.gateDenied","HessaArmouryGate","Hessa","vask.deposed");
                kit.Crossing("Idra on the stair down",new Vector3(StairMid.x,0,StairMid.y),new Vector3(4.6f,5,2),"idra.lockCounted","IdraLockCounted","Idra","vask.gateDenied");
                var cell=kit.Person("Drask",new Vector3(CellAt.x,0,CellAt.y),180,"",false,"vask.deposed","DraskCustody","DraskCustody");cell.repeatNode="DraskCustody";cell.finalCompletionFlag="vask.orderSaid";cell.finalRepeatNode="DraskCustody";cell.range=3.6f;
                // In his cell from the verdict, as it happens (the party walk down to him in the same visit); gone aboard Many Hands once the chapter is over, but only as the town next
                // loads: with one live Whereabouts he vanished from behind his bars the moment the chapter's card came up, the party standing in front of them (s52/339 to 370).
                var cellHolder=new GameObject("The Warmaster's cell");cell.transform.SetParent(cellHolder.transform,true);
                Where("Drask is in his cell",cellHolder,"vask.deposed",null,null,null,true);
                Where("Drask leaves his cell once the chapter is over",cell.gameObject,null,null,null,"chapter8.complete",false);
                foreach(var (at,yaw) in CellGuards){var g=Body(kit,"TovraBody",new Vector3(at.x,0,at.y),yaw,new Color(.92f,.94f,1));g.name="Townsperson, an Unsworn smith on guard";Carry(kit,g,"a pike");Where("The cell's guard",g,"vask.deposed",null,null,null,true);}
                var radio=new GameObject("Every radio in the harbour",typeof(AfterTalk)).GetComponent<AfterTalk>();radio.transform.position=At(kit,CellFront);
                radio.flag="vask.orderSaid";radio.node="BrynNacreTraffic";radio.speaker="Bryn";radio.heardFlag="vask.nacreHeard";radio.delay=1.2f;
                var meret=new GameObject("All stations",typeof(AfterTalk)).GetComponent<AfterTalk>();meret.transform.position=At(kit,CellFront);
                meret.flag="vask.nacreHeard";meret.node="MeretAllStations";meret.speaker="Meret";meret.heardFlag="chapter8.complete";meret.delay=.8f;
            }

            // ---------------------------------------------------------------- the duel ground (docs/maps/Harrowgate.md, "Duel ground contract")
            static void Duel(ZoneKit kit)
            {
                Color iron=new(.3f,.3f,.32f);
                // ---- the seals: one per opening of the kerb, strands of light while shut, solid and carving the paths.
                var beam=kit.Glow("Harrowgate seal",true,new Color(.86f,.9f,1,1));var wave=new System.Random(89);
                Membrane Seal(string name,Vector3 at,float width,bool across)
                {
                    var seal=WorldBuilder.Membrane(name,at,width,true,3.2f);if(!across)seal.transform.rotation=Quaternion.Euler(0,90,0);
                    foreach(var f in seal.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(f);foreach(var r in seal.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(r);
                    var door=seal.GetComponent<BoxCollider>();var obstacle=seal.gameObject.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=door.center;obstacle.size=door.size;obstacle.carving=true;obstacle.carveOnlyStationary=false;obstacle.enabled=false;
                    var dir=across?Vector3.right:Vector3.forward;
                    for(float s=-width*.5f+.4f;s<=width*.5f-.3f;s+=.9f){float rise=2.4f+(float)wave.NextDouble()*.7f,lean=((float)wave.NextDouble()-.5f)*.3f;var foot=new Vector3(at.x,Upper,at.z)+dir*s;ZoneKit.Strand(seal.transform,foot,foot+dir*lean+Vector3.up*rise,beam,1.4f,new Color(.8f,.86f,1,.6f),new Color(.8f,.86f,1,0));}
                    foreach(var strand in seal.GetComponentsInChildren<LineRenderer>())strand.enabled=false;
                    return seal;
                }
                var seals=new[]{Seal("Muster field seal, south",new Vector3(0,Upper+1.6f,FieldSouth-.35f),SouthOpen*2+.4f,true),Seal("Muster field seal, west",new Vector3(-FieldHalfX-.35f,Upper+1.6f,85),SideOpen*2+.4f,false),
                    Seal("Muster field seal, east",new Vector3(FieldHalfX+.35f,Upper+1.6f,85),SideOpen*2+.4f,false),Seal("Muster field seal, north",new Vector3(0,Upper+1.6f,FieldNorth+.35f),SouthOpen*2+.4f,true)};
                // ---- the duel's own component (S/Combat/DuelGround.cs, the duel engineer's): the field's middle, its half-size inside the kerb, and the guard wing's post.
                var middle=new GameObject("Duel ground");middle.transform.position=At(kit,DuelMiddle);
                var post=new GameObject("Guard wing's post");post.transform.position=At(kit,GuardPost);post.transform.SetParent(middle.transform,true);
                var duelGround=middle.AddComponent<DuelGround>();duelGround.boss="DraskVaal";duelGround.half=new Vector2(FieldHalfX,(FieldNorth-FieldSouth)*.5f);duelGround.guardPost=post.transform;
                // ---- the door: the Warmaster on the field, the talk as the party steps on, the encounter that waits for it.
                var talk=kit.Crossing("The Warmaster on the muster field",new Vector3(0,0,85),new Vector3(FieldHalfX*2-1,5,FieldNorth-FieldSouth-1),"vask.fieldTaken","DraskOnTheField","Drask","vask.draskMet");
                talk.saveSpawn="Field";
                if(ZoneKit.Find<EnemyDef>("DraskVaal")!=null)
                {
                    var duel=WorldBuilder.Encounter("DraskVaal",new Vector3(0,Upper+1,85),new Vector3(FieldHalfX*2,5,FieldNorth-FieldSouth),"DraskVaal",1,seals);
                    var spawner=duel.GetComponentInChildren<Spawner>();spawner.transform.position=At(kit,F);spawner.transform.rotation=Quaternion.Euler(0,180,0);spawner.faceAsPlaced=true;spawner.radius=0;
                    duel.requiredFlag="vask.fieldTaken";duel.checkpointSpawn="Field";
                    var viewpoint=new GameObject("The Warmaster on show");viewpoint.transform.position=At(kit,new Vector2(0,FieldSouth));duel.previewPoint=viewpoint.transform;duel.previewRadius=20;talk.previewEncounter=duel;
                    // Shown before the fight he is a dormant body with every collider off, so nothing stopped a hero walking into him (Taren stood .44 m from his middle, s51/147).
                    // While he stands on his post as a person, this does; it goes as the duel begins (his own capsule then), and after it Drask, yielded, has his. An obstacle, so the
                    // companion steps round him too.
                    var standing=new GameObject("The Warmaster standing on his post",typeof(CapsuleCollider),typeof(NavMeshObstacle));standing.transform.position=At(kit,F);
                    var stood=standing.GetComponent<CapsuleCollider>();stood.radius=.75f;stood.height=2.5f;stood.center=Vector3.up*1.25f;
                    var round=standing.GetComponent<NavMeshObstacle>();round.shape=NavMeshObstacleShape.Capsule;round.radius=.75f;round.height=2.5f;round.center=Vector3.up*1.25f;round.carving=false;
                    // And he is on the field only once he has received the party at his table and gone out to it (DraskReceives sets vask.draskMet): until then he sits in the hall,
                    // and the body on show at F with what stops a hero there made two of him, 22 m apart (whole-game capture of November 22, s52/014 and 017; D383).
                    duel.previewFlag="vask.draskMet";
                    var goneOut=new GameObject("The Warmaster has gone out to the field");goneOut.transform.position=At(kit,F);goneOut.transform.SetParent(duel.transform,true);
                    standing.transform.SetParent(goneOut.transform,true);
                    Gate("The Warmaster is on the field once he has received the party","vask.draskMet",true,goneOut);
                    Gate("The Warmaster stands on his post until the duel begins","vask.fieldTaken",false,standing);
                }
                else Debug.LogWarning("HARROWGATE_DUEL_PENDING: no DraskVaal EnemyDef yet, so the field has its seals and no encounter");
                // ---- the room: a combat place while the duel is owed and the lead is on the field.
                var room=new GameObject("Muster field",typeof(BoxCollider));room.transform.position=new Vector3(0,Upper+4,85);var box=room.GetComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(FieldHalfX*2,8,FieldNorth-FieldSouth);
                var pocket=room.AddComponent<FightPocket>();pocket.from="vask.draskMet";pocket.until="vask.duelWon";
                // before: the field at drill (cadets marching in files, a sergeant calling it); during: the town on the steps and the kerbs, the guard wing at its post; after: as during, the
                // crowd staying to see what the harbour does.
                var before=new GameObject("The field at drill");
                foreach(var (a,b,tint) in new[]{(new Vector2(-14,76.6f),new Vector2(-4.2f,76.6f),new Color(1,.98f,1.04f)),(new Vector2(-14,79),new Vector2(-4.2f,79),new Color(.96f,1,1)),(new Vector2(4.2f,76.6f),new Vector2(14,76.6f),new Color(1.04f,1,.96f)),(new Vector2(4.2f,79),new Vector2(14,79),new Color(1,1.02f,.98f))})
                {var cadet=kit.Walker(ZoneKit.Prefab("Characters","PipBody")!=null?"PipBody":"BrekkBody",new[]{a,b},1.3f,1.2f,tint,"BrekkBody");cadet.thereAndBack=true;cadet.name="A cadet at drill";cadet.transform.localScale=Vector3.one*.84f;cadet.transform.SetParent(before.transform,true);}
                // By the east files' head, turned to them (FaceSomething): he stood in the open between the two drills, facing neither.
                var sergeant=Body(kit,"MusterSergeant",new Vector3(1.6f,0,81.2f),120,Color.white,false);sergeant.name="Townsperson, the drill sergeant";sergeant.transform.SetParent(before.transform,true);
                // Before the duel is called, a few already on the hall steps to watch the drill; once it is, they are the crowd.
                foreach(var (x,z,body,yaw) in new[]{(-7.4f,101.2f,"OdrunBody",170f),(-5.8f,101.8f,"BrekkBody",200f),(8.2f,102.4f,"TovraBody",190f)})
                {var one=Body(kit,body,new Vector3(x,0,z),yaw,new Color(.96f,.96f,1));one.name="Townsperson, watching the drill";foreach(var t in one.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;one.transform.SetParent(before.transform,true);}
                var during=new GameObject("The town watching");var after=new GameObject("The town waits on the steps");
                GameObject Watcher(GameObject set,string body,Vector2 at,float yaw,Color tint,string doing,float scale=1)
                {var one=Body(kit,body,new Vector3(at.x,0,at.y),yaw,tint);one.name="Townsperson, watching the duel";foreach(var t in one.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
                    if(scale!=1)one.transform.localScale=Vector3.one*scale;Carry(kit,one,doing);if(body=="MusterPikeguard")Carry(kit,one,"pike");one.transform.SetParent(set.transform,true);return one;}
                string[] civil={"BrekkBody","TovraBody","OdrunBody"};string[] guard={"MusterPikeguard","MusterSergeant","MusterScav"};
                int w=0;
                foreach(var (x,z) in new[]{(-11f,100.4f),(-7.6f,100.6f),(-4.4f,100.4f),(4.4f,100.6f),(7.8f,100.4f),(11.2f,100.6f),(-12.4f,102.8f),(-9.2f,103f),(-3.2f,102.8f),(3.4f,103f),(9.4f,102.8f),(12.4f,103f)})
                {w++;Watcher(during,w%3==0?guard[w%guard.Length]:civil[w%civil.Length],new Vector2(x,z),180+(x<0?-12:12)*(Mathf.Abs(x)/12),new Color(.92f+.04f*(w%3),.94f+.03f*(w%2),.96f+.02f*(w%4)),"");
                    if(Mathf.Abs(x)<9)Watcher(after,civil[(w+1)%civil.Length],new Vector2(x,z),180,new Color(.94f,.96f,1),"");}
                // The cadets in their files along the west kerb (the third file by the drain is Pip's), and the east stands.
                foreach(var (x,z) in new[]{(-20.2f,74.6f),(-20.2f,77f),(-21.6f,74.6f),(-21.6f,77f),(-20.2f,91.4f),(-20.2f,93.8f),(-21.6f,91.4f),(-21.6f,93.8f),(-20.2f,96.2f)})
                    Watcher(during,"PipBody",new Vector2(x,z),90,new Color(.96f+.02f*((int)z%3),1,1.02f),"",.84f);
                foreach(var (x,z,body) in new[]{(21.9f,75.6f,"BrekkBody"),(22.9f,78.6f,"TovraBody"),(21.9f,90.6f,"OdrunBody"),(22.9f,93.2f,"MusterSergeant"),(23.9f,95.4f,"BrekkBody")})
                    Watcher(during,body,new Vector2(x,z),270,new Color(.96f,.96f,1),"");
                foreach(var (x,z,body) in new[]{(-19.4f,70.6f,"BrekkBody"),(19.6f,70.6f,"OdrunBody")})Watcher(during,body,new Vector2(x,z),x<0?40:320,new Color(.94f,.96f,1),"");
                // The guard wing at its post outside the kerb: Arrek and Sool in front (they go in at the first shout); two behind who stay where they are.
                var front=new GameObject("The guard wing's front rank");front.transform.SetParent(during.transform,true);
                var arrek=Watcher(front,"MusterPikeguard",new Vector2(14.6f,99.6f),200,new Color(1,1,1),"");arrek.name="Townsperson, Sergeant Arrek of the guard wing";
                var sool=Watcher(front,"MusterPikeguard",new Vector2(16.8f,99.4f),205,new Color(.94f,.96f,1),"");sool.name="Townsperson, Sool of the guard wing";sool.transform.rotation*=Quaternion.Euler(0,0,3);
                Gate("The guard wing at its post until the first shout","vask.shout1",false,front);
                foreach(var (x,z) in new[]{(15.6f,101.6f),(17.8f,101.4f)})Watcher(during,"MusterPikeguard",new Vector2(x,z),205,new Color(.92f,.94f,.98f),"");
                var soolAfter=Watcher(during,"MusterPikeguard",new Vector2(16.8f,99.4f),205,new Color(.94f,.96f,1),"");soolAfter.name="Townsperson, Sool, who stayed";soolAfter.transform.rotation*=Quaternion.Euler(0,0,3);
                Gate("Sool stays where he is once the Warmaster has yielded","bossdown.DraskVaal",true,soolAfter);
                // From the second shout: smiths with pikes from the armoury, standing behind the crowd on the steps.
                var smiths=new GameObject("The Unsworn come in behind the steps");smiths.transform.SetParent(during.transform,true);
                foreach(var (x,z) in new[]{(-10.6f,105.2f),(-6.4f,105.2f),(6.6f,105.2f),(10.8f,105.2f),(-13.4f,105.4f),(13.2f,105.4f)})Watcher(smiths,x<0?"TovraBody":"BrekkBody",new Vector2(x,z),180,new Color(.86f,.86f,.9f),"a pike");
                Gate("The Unsworn come in from the second shout","vask.shout2",true,smiths);
                // The free cradle by the checkpoint while the duel is owed.
                var cradleSet=Between("A field surgeon's cradle while the duel is owed","vask.draskMet","vask.duelWon");
                var cradle=kit.Prop("DeckConsole",new Vector3(FieldCradle.x,0,FieldCradle.y),1.4f,200).AddComponent<RepairBay>();cradle.free=true;cradle.saveSpawn="Field";cradle.prompt="Field surgeon's cradle — repair and save";
                cradle.transform.SetParent(cradleSet.transform,true);foreach(var t in cradle.GetComponentsInChildren<Transform>())t.gameObject.isStatic=false;
                pocket.before=new[]{before};pocket.during=new[]{during};pocket.after=new[]{after};
                before.SetActive(true);during.SetActive(false);after.SetActive(false);
                // ---- Drask the person after the yield: in his war-frame, where he yielded; his word to Sool; then off the field toward the harbour.
                var person=kit.Person("Drask",new Vector3(F.x,0,F.y),180,"",false,"bossdown.DraskVaal","DraskLeavesField","DraskLeavesField");person.repeatNode="DraskLeavesField";person.name="Drask, yielded";
                // His person body is his war-frame under his mantle (his CharacterDef's natural, TownLook's Warmaster): the same as the duel's. Only while it isn't taken in is he put
                // in the bare frame here.
                var frame=ZoneKit.Prefab("Enemies","DraskVaal");
                if(frame!=null&&AssetDatabase.LoadAssetAtPath<GameObject>(PersonWarmaster)==null)
                {
                    var civilian=person.transform.Find("Generated civilian");if(civilian!=null)UnityEngine.Object.DestroyImmediate(civilian.gameObject);
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(frame);model.transform.SetParent(person.transform,false);model.name="Generated civilian";
                    foreach(var b in model.GetComponentsInChildren<GroundLifecycleSupport>())UnityEngine.Object.DestroyImmediate(b);
                }
                var walksOff=person.gameObject.AddComponent<LeavesAfter>();walksOff.flag="vask.duelWon";walksOff.path=new[]{At(kit,new Vector2(10,85)),At(kit,new Vector2(FieldHalfX+2,85)),At(kit,new Vector2(36,85)),At(kit,new Vector2(43.6f,82))};
                var yieldedHere=new GameObject("Drask after the duel");person.transform.SetParent(yieldedHere.transform,true);Where("Drask has gone down to the harbour",yieldedHere,null,null,null,"vask.duelWon",false);
                var yielded=new GameObject("Drask Vaal yields",typeof(YieldedCaptain)).GetComponent<YieldedCaptain>();yielded.npc=person;person.gameObject.SetActive(false);
                yielded.bossId="DraskVaal";yielded.downFlag="bossdown.DraskVaal";yielded.heardFlag="vask.duelWon";yielded.guards=Array.Empty<GameObject>();yielded.guardedFlag="";yielded.guardedNode="";
                // ---- the way up after him (the duel engineer's "Pursuit contract"): a pad on the forecourt, locked until he has gone.
                var launchAt=At(kit,LaunchPad);var pad=new ZoneKit.Shape();
                pad.Slot(0).Disc(launchAt+Vector3.up*.06f,Vector3.up,2.8f,28,new Color(.58f,.56f,.55f),0,new Color(.66f,.64f,.62f),.25f).Slot(1).Disc(launchAt+Vector3.up*.065f,Vector3.up,2.2f,28,new Color(.86f,.82f,.72f),1.95f);
                var padBody=kit.Built("Launch pad",pad,Stone,Cloth);
                if(ZoneKit.Find<ZoneDef>("HarrowgateHarbour")!=null)
                {
                    var holder=new GameObject("The way up after the Warmaster");holder.transform.position=launchAt;padBody.transform.SetParent(holder.transform,true);
                    kit.Way(holder,"Launch — The War Harbour","HarrowgateHarbour","Launch","vask.duelWon","Answer the Warmaster on the muster field first",4,LaunchPad+new Vector2(4.2f,-1.6f));
                }
                else Debug.LogWarning("HARROWGATE_PURSUIT_PENDING: no HarrowgateHarbour ZoneDef yet, so the forecourt's pad leads nowhere");

                // ---- after the pursuit: his skiff down on his own field, the Unsworn and half the garrison standing on it, Odrun's verdict at once.
                var verdict=new GameObject("The field after the pursuit");
                var wreck=SkiffBody(kit,new Vector3(WreckAt.x,Upper+.5f,WreckAt.y),Quaternion.Euler(8,128,-14));
                if(wreck!=null){wreck.name="The Warmaster's skiff, down";wreck.AddComponent<PropTint>().tint=new Color(.46f,.42f,.42f);wreck.transform.SetParent(verdict.transform,true);}
                var odrunThere=Body(kit,"OdrunBody",new Vector3(OdrunVerdictAt.x,0,OdrunVerdictAt.y),80,Color.white,false);odrunThere.name="Mother Odrun on the field";odrunThere.transform.SetParent(verdict.transform,true);
                var draskThere=WarmasterStanding(kit,DraskVerdictAt,260);draskThere.name="Drask Vaal on his field";draskThere.transform.SetParent(verdict.transform,true);
                foreach(var (x,z,body,doing) in new[]{(-6f,93.4f,"TovraBody","a pike"),(-3f,94.6f,"BrekkBody","a pike"),(1.6f,95f,"OdrunBody",""),(5.2f,94.2f,"TovraBody","a pike"),(-11.4f,90.6f,"BrekkBody","a pike"),
                    (-12.6f,82.4f,"MusterPikeguard",""),(-10.2f,80.6f,"MusterSergeant",""),(-13.8f,78.6f,"MusterPikeguard",""),(11.6f,80.2f,"MusterPikeguard",""),(13.8f,82.6f,"MusterScav","")})
                {var one=Body(kit,body,new Vector3(x,0,z),Mathf.Atan2(-x,DraskVerdictAt.y-z)*Mathf.Rad2Deg+180,new Color(.94f,.94f,.98f),false);one.name="Townsperson, standing on the field";Carry(kit,one,doing);one.transform.SetParent(verdict.transform,true);}
                Where("The town is on the field for the verdict",verdict,"vask.pursuitWon",null,null,"vask.deposed",false);
                var answer=new GameObject("Harrowgate answers",typeof(AfterTalk)).GetComponent<AfterTalk>();answer.transform.position=At(kit,new Vector2(0,85));
                answer.flag="vask.pursuitWon";answer.node="OdrunVerdict";answer.speaker="Odrun";answer.heardFlag="vask.deposed";answer.delay=1.5f;
                _=iron;
            }

            // ---------------------------------------------------------------- smoke over the roofs
            static void Smoke(ZoneKit kit)
            {
                string path="Assets/_Project/Scenes/"+kit.zone+"-Smoke-soft.mat";var smokeMat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(smokeMat==null){smokeMat=new Material(Resources.Load<Material>("Effects/flare_01"));AssetDatabase.CreateAsset(smokeMat,path);}
                smokeMat.SetColor("_BaseColor",new Color(.66f,.64f,.66f,.45f));EditorUtility.SetDirty(smokeMat);
                foreach(var (at,up) in new[]{(new Vector2(-46,52.4f),Lower+7.2f),(new Vector2(-12.6f,52.6f),Lower+6.8f),(new Vector2(-21,48.5f),Lower+4.8f),(new Vector2(22.5f,53.6f),Lower+5.6f),(new Vector2(35.5f,53.6f),Lower+5.6f),(new Vector2(-47,76),Upper+7)})
                {
                    var smoke=kit.Drift("Chimney smoke",new Vector3(at.x,up,at.y),new Vector3(.5f,.3f,.5f),kit.Glow("harrow-smoke",false,new Color(.62f,.62f,.66f,1)),new Color(.68f,.66f,.68f,.32f),50,.8f,1.8f,.7f,7);
                    var main=smoke.main;main.startSpeed=.8f;var shape=smoke.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;smoke.transform.rotation=Quaternion.Euler(-80,60,0);
                    smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial=smokeMat;
                }
            }

            // ---------------------------------------------------------------- checks and the bake
            /// <summary>Every block under the mesh that draws it. Most sections stand their blocks first and draw afterwards, so a block was left under a bare holder, which the world
            /// tests read as an invisible wall (NoInvisibleWallStandsInTheOpen found the gate towers so). ZoneKit.Block seats a block under a drawn child only if one is there yet.</summary>
            static void SeatBlocks()
            {
                foreach(var box in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    if(box.isTrigger||!box.gameObject.isStatic||box.GetComponentInChildren<Renderer>(true)!=null||box.GetComponentInParent<Renderer>(true)!=null||box.GetComponentInParent<Membrane>(true)!=null)continue;
                    var parent=box.transform.parent;var drawn=parent!=null?parent.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault():null;
                    if(drawn!=null)box.transform.SetParent(drawn.transform,true);else Debug.LogWarning("HARROWGATE_BLOCK_UNDRAWN "+box.name+" at "+box.transform.position);
                }
            }
            /// <summary>How near a way's line the edge of anything solid may come (the bake's agent is .55 m round), and a townsperson's whole body.</summary>
            const float WayClear=.9f,FolkClear=2;
            /// <summary>Refuses the finished town if anything solid the builder stood in it, by hand or by any helper, cuts a way (Rootmarket's rule): its footprint within <see cref="WayClear"/>
            /// of a way's line, a townsperson within <see cref="FolkClear"/>, or a walker's loop within 1.6 m. The named stand at the ways' ends, so they don't count.</summary>
            static void NothingStandsOnTheWays()
            {
                // Every state of the town at once: what shows only before or after a flag stands on the ways as much as what always does.
                var hidden=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>!t.gameObject.activeSelf).Select(t=>t.gameObject).ToArray();
                foreach(var go in hidden)go.SetActive(true);
                try{Check();}finally{foreach(var go in hidden)go.SetActive(false);}
                void Check(){
                Physics.SyncTransforms();var faults=new List<string>();
                float Outside(Vector2 p,Vector2 centre,float yaw,Vector2 half)
                {
                    var d=p-centre;float r=yaw*Mathf.Deg2Rad,x=d.x*Mathf.Cos(r)-d.y*Mathf.Sin(r),z=d.x*Mathf.Sin(r)+d.y*Mathf.Cos(r);
                    float ox=Mathf.Abs(x)-half.x,oz=Mathf.Abs(z)-half.y;return new Vector2(Mathf.Max(ox,0),Mathf.Max(oz,0)).magnitude+Mathf.Min(Mathf.Max(ox,oz),0);
                }
                (float gap,Vector2 from,Vector2 to) Nearest(Vector2 centre,float yaw,Vector2 half)
                {
                    var best=(float.PositiveInfinity,Vector2.zero,Vector2.zero);
                    foreach(var way in Ways)for(int i=1;i<way.Length;i++)
                    {
                        int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(way[i-1],way[i])/.1f));
                        for(int k=0;k<=steps;k++){float gap=Outside(Vector2.Lerp(way[i-1],way[i],k/(float)steps),centre,yaw,half);if(gap<best.Item1)best=(gap,way[i-1],way[i]);}
                    }
                    return best;
                }
                foreach(var solid in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    if(!solid.enabled||solid.isTrigger||solid.GetComponentInParent<Npc>(true)!=null||solid.GetComponentInParent<Membrane>(true)!=null||solid.GetComponentInParent<NavMeshObstacle>(true)!=null||solid.GetComponent<WalkLoop>()!=null)continue;
                    var bounds=solid.bounds;if(bounds.size.x>40||bounds.size.z>40)continue;
                    var at=new Vector2(bounds.center.x,bounds.center.z);if(bounds.max.y<Height(at)+.32f)continue;
                    bool folk=solid.name.StartsWith("Townsperson");float yaw=0;var half=new Vector2(bounds.extents.x,bounds.extents.z);
                    if(solid is BoxCollider box&&Mathf.Abs(solid.transform.eulerAngles.x)<.5f&&Mathf.Abs(Mathf.DeltaAngle(solid.transform.eulerAngles.z,0))<.5f)
                    {
                        var c=solid.transform.TransformPoint(box.center);at=new Vector2(c.x,c.z);yaw=solid.transform.eulerAngles.y;
                        var size=Vector3.Scale(box.size,solid.transform.lossyScale);half=new Vector2(Mathf.Abs(size.x),Mathf.Abs(size.z))*.5f;
                    }
                    if(folk){half=Vector2.zero;yaw=0;}
                    var (gap,from,to)=Nearest(at,yaw,half);float need=folk?FolkClear:WayClear;
                    if(gap<need)faults.Add($"{(solid.transform.parent!=null?solid.transform.parent.name+"/":"")}{solid.name} at ({at.x:0.0}, {at.y:0.0}) is {gap:0.00} m from the way from {from} to {to} (needs {need})");
                }
                foreach(var walk in UnityEngine.Object.FindObjectsByType<WalkLoop>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var p in walk.path)
                {
                    var (gap,from,to)=Nearest(new Vector2(p.x,p.z),0,Vector2.zero);
                    if(gap<1.6f)faults.Add($"{walk.name}'s loop at ({p.x:0.0}, {p.z:0.0}) is {gap:0.00} m from the way from {from} to {to}");
                }
                if(faults.Count>0)throw new InvalidOperationException("Harrowgate: things stand on the town's ways:\n"+string.Join("\n",faults.Distinct()));
                Debug.Log("HARROWGATE_WAYS_CLEAR");
                }
            }
            /// <summary>The bake, with every door the story opens open and every seal open (the encounter shuts them; their obstacles carve). Every place a player must stand is listed.</summary>
            static void Bake(ZoneKit kit)
            {
                var points=new List<Vector2>{Pad+new Vector2(0,-3.4f),RoadArch+new Vector2(3,0),HessaGate+new Vector2(-1.6f,-.6f),GateHead,PipWaits,FlourAt+new Vector2(0,-1.2f),WashingAt,KitchenDoor,OdrunAt+new Vector2(-1.6f,0),TableAt,
                    BrekkAt+new Vector2(0,-1.6f),StairGameAt,ForgesAt,MenderAt+new Vector2(0,-1.4f),KettleAt+new Vector2(0,-1.2f),AnvilAt+new Vector2(0,-1.4f),SaltAt+new Vector2(-1.4f,0),FieldSpawn,VerdictSpawn,F,
                    PipFieldAt,DraskAt+new Vector2(0,-1.6f),NamesAt,Case3,Case1,DoorCase,Case2,Lookout,new Vector2(-24,104),new Vector2(32,104),ArmouryGate,StairMid,StairFootAt,CellFront,LaunchPad,new Vector2(0,107.8f)};
                // A shut door's carving obstacle cuts the editor's own paths too (Driftreed's tide seals): the harbour gate's leaves made the town unreachable in the bake's check. Every
                // obstacle is off while the bake checks the town with its doors open, and back as the town stands after.
                var obstacles=UnityEngine.Object.FindObjectsByType<NavMeshObstacle>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(o=>(o,o.enabled)).ToArray();
                foreach(var (o,_) in obstacles)o.enabled=false;
                try{kit.BakeNavigation(new Bounds(new Vector3(4,6,62),new Vector3(124,30,148)),Arrival,points);}
                finally{foreach(var (o,on) in obstacles)o.enabled=on;}
            }

            // ---------------------------------------------------------------- the floor's dressing, last
            /// <summary>The ground kit: dressed pavers where feet go and on the upper terrace and podium, cobbles on the quay and the armourers' street, damp toward the water; cracks, soot by
            /// the forges, litter at the doors, the field's drill lines; weeds in the joints at the parapets' feet; lamp pools; the smoke's shadow moving over the town.</summary>
            static void Ground(ZoneKit kit,ZoneKit.Land land)
            {
                if(!File.Exists(GroundKit.Art+"ground-pavers.png")||!File.Exists(GroundKit.Art+"ground-decals.png")||!File.Exists(GroundKit.Art+"ground-cover.png")||!File.Exists(GroundKit.Art+"ground-shade-cloud.png"))
                {Debug.LogWarning("HARROWGATE_GROUND_KIT_PENDING: the ground kit's textures aren't painted yet, so the floor has its vertex tones only");return;}
                var ground=GroundKit.Open(kit.zone,land.FloorFilter,83,CliffTop);
                foreach(var way in Ways)ground.ClearLane(way,1.4f);
                // Nothing the kit lays goes under the water: the bed is out of sight and should stay so.
                float Dry(Vector2 p)=>Land(p)>.3f?1:0;
                ground.Layers(new Vector4(.05f,.34f,.4f,.28f),1.1f,new GroundKit.Layer("pavers",new Color(.78f,.76f,.72f,1),3.6f),new GroundKit.Layer("cobble",new Color(.66f,.64f,.62f,1),3.2f),
                    new GroundKit.Layer("gravel",new Color(.5f,.52f,.56f,1),3));
                // Every floor here is the town's own coursed basalt, laid by the Muster: the kit's irregular pavers (a path layer for wild ground) lay in blotches across half of every
                // still and read as nobody's floor, so they're gone. Grit lies against the cadet yard's kerbs, where the drill kicks it, not across the yard (raked edge to edge it measured
                // .81 plain); the field stays paved and is told apart by its inlay (raked over, it read as one plain stretch of brown dirt, .73 of the frame).
                float Yard(Vector2 p)=>Mathf.Min(Mathf.Min(p.x+54,-24-p.x),Mathf.Min(p.y-72,98-p.y));
                ground.Paint(2,p=>Dry(p)*(Yard(p)>0?.8f*(1-ZoneKit.Smooth(.6f,2.2f,Yard(p)))*ZoneKit.Smooth(.3f,.7f,ground.Noise(p,3,9)+.3f):0));
                ground.Shade(p=>.08f*(1-ZoneKit.Smooth(.4f,1.8f,FromWays(p)))*-1);
                Color crack=new(.3f,.3f,.32f,.8f),soot=new(.2f,.19f,.19f,.75f),litter=new(.6f,.54f,.44f,.8f),stain=new(.3f,.3f,.3f,.55f);
                ground.Decals(new[]{"CracksA","CracksB"},4,p=>Dry(p)*(OnTheField(p)?.4f:1),1,2.4f,crack,true,.1f,1);
                ground.Decals(new[]{"Scorch","Stain"},12,p=>p.y>40&&p.y<56&&p.x>11&&p.x<40?1:0,1,2.4f,soot,false,.12f,2);
                ground.Decals(new[]{"Litter","Stain"},6,p=>p.y>36&&p.y<56&&p.x<-2?ZoneKit.Smooth(.6f,2,FromWays(p)):0,.6f,1.4f,litter,false,.12f,3);
                ground.Decals(new[]{"Stain","Slabs"},3,p=>Dry(p)*(p.y<22.6f?1:0),1.2f,3,stain,false,.1f,4);
                // Wear along the streets the town walks (D371): stains, chipped slabs and litter at their edges, where carts stand and stalls trade (bare paving, s50/087, s51/136).
                ground.Decals(new[]{"Stain","Slabs","Litter"},5,p=>Dry(p)*(!OnTheField(p)&&FromWays(p)>1.4f&&FromWays(p)<5f?1:0),.8f,2.2f,new Color(.3f,.29f,.28f,.55f),false,.1f,31);
                ground.Decals(new[]{"Flagstones","Slabs"},2,p=>Dry(p)*(p.y>58.6f&&!OnTheField(p)?1:0),1.4f,2.8f,new Color(.7f,.68f,.64f,.7f),true,.1f,5);
                ground.Decals(new[]{"PebblesA","PebblesB"},3,p=>OnTheField(p)?1:0,.8f,1.6f,new Color(.44f,.43f,.44f,.8f),false,.1f,6);
                // The field's drill lines: the files' marks, pale, north to south, and the line the front rank dresses on.
                // Inside the medallion the files' marks stop at its rim: the ground mark is laid stone, and nothing is painted across it.
                foreach(float x in new[]{-12f,-6f,6f,12f})
                {
                    float chord=Mathf.Abs(x)<MedallionR?Mathf.Sqrt(MedallionR*MedallionR-x*x)+.3f:0;
                    if(chord==0)ground.Strip("SurveyLine",new[]{new Vector2(x,FieldSouth+1.2f),new Vector2(x,FieldNorth-1.2f)},0,.36f,new Color(.94f,.92f,.88f,.9f));
                    else foreach(var (z0,z1) in new[]{(FieldSouth+1.2f,DuelMiddle.y-chord),(DuelMiddle.y+chord,FieldNorth-1.2f)})ground.Strip("SurveyLine",new[]{new Vector2(x,z0),new Vector2(x,z1)},0,.36f,new Color(.94f,.92f,.88f,.9f));
                }
                ground.Strip("SurveyLine",new[]{new Vector2(-FieldHalfX+1.6f,FieldSouth+3.4f),new Vector2(FieldHalfX-1.6f,FieldSouth+3.4f)},0,.36f,new Color(.94f,.92f,.88f,.9f));
                ground.Strip("Tracks",new[]{new Vector2(-46,10),new Vector2(-12,10.6f),new Vector2(20,10),new Vector2(36,9)},.4f,1.2f,new Color(.36f,.36f,.38f,.5f));
                ground.Cover(new[]{"TuftA","Sprouts","TuftC"},3,p=>Dry(p)*(!OnTheField(p)&&FromWays(p)>2?1:0),.12f,.3f,new Color(.58f,.66f,.44f,1),3,.14f,7);
                // ---- each district's own marks, big enough for the camera (the look pass's second round):
                // (the walks' runners, the cadet yard's drill lines, the bastion's gun tracks and the wall walk's drain are built flat: FloorMarks) the tracks the drill's files have worn;
                for(int file=0;file<5;file++)ground.Strip("Tracks",new[]{new Vector2(-36.4f+file*1.4f,73.6f),new Vector2(-36.4f+file*1.4f,86.4f)},.3f,1,new Color(.38f,.32f,.26f,.7f));
                // the armourers' street: soot and scorch thick before the forges, ash drifted pale over it;
                ground.Decals(new[]{"Scorch"},40,p=>p.x>12&&p.x<40&&p.y>45.6f&&p.y<52.4f?1:0,1.2f,3,new Color(.1f,.09f,.09f,.9f),false,.12f,21);
                ground.Decals(new[]{"SandDrift"},14,p=>p.x>12&&p.x<40&&p.y>43.6f&&p.y<52.4f?1:0,1,2.4f,new Color(.7f,.68f,.66f,.75f),false,.1f,22);
                // the harbour wall and the bastion: rust and weather;
                ground.Decals(new[]{"Stain","Lichen"},18,p=>p.x>41.5f&&p.y>61&&Land(p)>.3f&&!(p.x>47.8f&&p.y>78&&p.y<112.2f)?1:0,1,3,new Color(.26f,.32f,.26f,.6f),false,.12f,23);
                // and by the water, moss and weeds in the seams.
                ground.Decals(new[]{"Lichen"},26,p=>Land(p)>.25f&&Land(p)<2.4f?1:0,.8f,2.2f,new Color(.3f,.44f,.22f,.8f),false,.12f,24);
                ground.Cover(new[]{"TuftA","Sprouts","TuftB"},34,p=>Land(p)>.3f&&Land(p)<1.8f?1:0,.12f,.32f,new Color(.42f,.58f,.28f,1),3,.14f,25);
                // Embers kicked out in front of the two hearths that burn from the start.
                ground.Glows(140,p=>p.y>50.4f&&p.y<52.6f&&(Mathf.Abs(p.x-21.3f)<1.3f||Mathf.Abs(p.x-34.3f)<1.3f)?1:0,.08f,.22f,new Color(1,.5f,.2f,1),11);
                ground.Cover(new[]{"Shards","Twigs"},4,p=>p.y>40&&p.y<56&&p.x>11&&p.x<40&&FromWays(p)>1.6f?1:0,.08f,.2f,new Color(.6f,.58f,.56f,1),2,.14f,8);
                // No painted lamp pools: the town is seen by day, and a pool of lamplight belongs to night or to shade (flat ochre discs on the paving, s50/087, s53/016).
                ground.Look("Canyon",null);
                ground.MovingShade("cloud",40,new Vector2(.5f,.3f),.2f,.5f,.05f,default,new Vector2(12,24));
                ground.Finish();
            }
        }
    }
}
