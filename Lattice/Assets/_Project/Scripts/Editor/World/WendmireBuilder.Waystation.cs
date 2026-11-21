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

namespace Lattice.EditorTools
{
    /// <summary>
    /// The Drowned Waystation (wild approach, then dungeon; rule: the pumps drain the station a stage at a time, and what the water leaves wakes). Map brief, coordinates and the boss
    /// arena contract: docs/maps/DrownedWaystation.md. Everything of the zone's own is in <see cref="Waystation"/>, so nothing here can collide with the other Wendmire files' members.
    /// <c>-waystation-greybox</c> leaves out dressing and the ground kit (the blockout).
    /// </summary>
    public static partial class WendmireBuilder
    {
        static void BuildWaystation()=>Waystation.Build();

        /// <summary>The mire's and the station's light, registered beside its builder (ZoneLook finds it by its attribute). Dusk on Wendmire: a low warm sun under the moons, a lilac fill so
        /// shade on peat and wet stone reads as evening rather than mud, haze over the water. The station is roofless (the roof fell in), so it takes the same sky; its lamps are the old
        /// engineers' amber shards and the mire's own glow-fungus.</summary>
        [ZoneLook.Source] static void WaystationLooks()
        {
            ZoneLook.Register(Station,new ZoneLook.Spec{sun=new Color(1f,.82f,.66f),sunIntensity=1.25f,sunPitch=38,shadowStrength=.72f,
                sky=new Color(.5f,.5f,.6f),equator=new Color(.44f,.42f,.44f),ground=new Color(.2f,.2f,.18f),
                fog=true,fogColor=new Color(.56f,.54f,.66f),fogStart=45,fogEnd=170,
                exposure=.15f,contrast=12,saturation=7,splitShadows=new Color(.44f,.47f,.54f),splitHighlights=new Color(.6f,.53f,.44f),
                bloomThreshold=.9f,bloomIntensity=.65f,vignette=.22f});
        }

        internal static class Waystation
        {
            // ---------------------------------------------------------------- the plan
            // x runs east and z north, so north is the back of every view (the fixed camera looks north-north-east from the south-south-west). The mire is three peat islands, south to
            // north, with water between: the first island (the arrival and a Bogstrider pocket), the second (the lure lesson), the third (strider and lure together), joined by a ford, a
            // leech channel with a dry spit round it, and the drowned causeway to the station's portico on the north shore. The station is roofless and half sunk: the entry hall, a stair
            // down to the lower gallery (a terrace on its east side, the low floor west of it, Pim's pocket off the west wall, the vault off the east), a stair up to the landing at the great
            // hall's door, and the great hall. Its water is one body that the pumps take down in stages: 0, then -2.7 (pump one: the hall and the terrace), then -4.5 (pump two: the low floor).
            internal const float MireWater=0,Peat=.55f,DeepBed=-1.5f;
            internal const float PorticoLevel=.5f,HallTop=.3f,HallSunk=-1.2f,Terrace=-2.4f,Low=-4f,SumpBed=-4.85f,Pocket=-2f,AnteFloor=-4.2f,VaultFloor=-2.4f,Landing=-.5f,ArenaFloor=-1.6f;
            // The landing stands half a metre under the mire's level, not six tenths: at six tenths the drowned station's first deep-water boxes took it in, and the Rotbloom's checkpoint (spawn
            // Door, on the landing) had no walkable floor under it in a station loaded before its pumps (QaStoryWorldTests.EveryBossDoorIsACheckpoint). Under the first stage it is still
            // cut off by the great stair's deep water, and dry from the second stage on, as before.
            internal const float StageOne=-2.7f,StageTwo=-4.5f,AnteWater=-2.6f,AnteDrained=-4.6f,GreatFull=-1.15f,GreatEmpty=-2.3f,PlinthTop=-.85f;
            internal const float ArenaRadius=18,PlinthOut=15,PlinthRadius=2.6f,WalkEdge=7,RampSouth=240;
            /// <summary>The great hall's centre, C in the boss arena contract.</summary>
            internal static readonly Vector2 C=new(0,289);
            static readonly (float x,float z,float r)[] IslandOne={(-8,5,10),(5,11,11),(6,25,10.5f),(-4,33,8.5f)};
            static readonly (float x,float z,float r)[] IslandTwo={(22,57,8.5f),(36,64,10.5f),(30,78,9.5f),(15,79,6.5f)};
            static readonly (float x,float z,float r)[] DryWay={(10,80,4),(-4,82,4),(-16,88,4.2f),(-22,98,4.6f)};
            static readonly (float x,float z,float r)[] IslandThree={(-22,106,7.5f),(-9,104,8.5f),(-3,118,10.5f),(4,131,8.5f),(1,139,6)};
            static readonly (float x,float z,float r)[] Hummock={(53,68,4.6f),(56,73,3.6f)};
            static readonly (float x,float z,float r)[] NorthShore={(-24,161,6),(0,159,8.5f),(24,161,6)};
            /// <summary>Wading ways across the water, each a band of bed raised to a wading depth: the ford to the second island, the leech channel, the drowned causeway, the bell ford.</summary>
            internal static readonly (Vector2 a,Vector2 b,float half,float bed)[] Fords={(new(12,32),new(20,52),2.6f,-.3f),(new(24,85),new(-3,101),3.6f,-.34f),(new(1,140),new(0,153.5f),2.4f,-.24f),(new(44.5f,63.5f),new(50.5f,67.5f),2.2f,-.28f)};
            /// <summary>Plan points a player stands at, named once: the scatter's keep-off, the navigation's must-reach list, LookDev's views, the map brief and a route all want them.</summary>
            internal static readonly Vector2 Arrival=new(-4,2.5f),Raft=new(-11,-5.5f),Cradle=new(-12.5f,7),MaruIsland=new(8.5f,22),StriderPocket=new(2,32),LureLesson=new(28,60),
                RealLamp=new(33,75.5f),BellBuoy=new(59,69.5f),BellHand=new(55.6f,69.6f),BellCache=new(53.6f,74.6f),LeechMiddle=new(10.5f,93),DryCache=new(-19.6f,93.2f),ThirdPocket=new(-4,120),
                Bench=new(-10,157.5f),MaruPortico=new(-6.5f,162.5f),PimPortico=new(-4,161.4f),PumpOne=new(13.6f,184.5f),PumpOneHand=new(12.1f,182.9f),PumpTwo=new(18.5f,248),PumpTwoHand=new(17,246.2f),
                PimAt=new(-31.5f,221.5f),Grille=new(-27.6f,221.5f),PimCache=new(-35,226),PumpFour=new(26.5f,219.6f),PumpFourHand=new(25.3f,217.9f),VaultMark=new(47.4f,218),VaultChest=new(45.5f,211.6f),
                PumpThree=new(-8.4f,264.2f),PumpThreeHand=new(-9,262.5f),DoorSpawn=new(-2.5f,261.5f),DoorCradle=new(7.5f,261.6f);
            internal static readonly (float x,float z)[] LureHides={(24,67),(17,74),(37,67),(27,81)},ThirdHides={(-12,116),(4,127)};
            /// <summary>Alcoves: each a notch in a wall at its room's floor, where a sentry stands drowned until the water leaves it (x0, x1, z0, z1, level).</summary>
            static readonly (float x0,float x1,float z0,float z1,float level)[] Alcoves=
            {
                (-10.6f,-7.4f,191.4f,194.4f,HallSunk),(-3.1f,.1f,191.4f,194.4f,HallSunk),
                (21.6f,24.4f,210,213,Terrace),(21.6f,24.4f,225,228,Terrace),(21.6f,24.4f,239,242,Terrace),
                (-24.4f,-21.6f,208,211,Low),(-24.4f,-21.6f,236,239,Low),(-17.5f,-14.5f,249.6f,252.4f,Low),(-11.5f,-8.5f,249.6f,252.4f,Low),(15,18,249.6f,252.4f,Terrace),
            };
            static readonly (Vector2 at,float radius)[] Sumps={(new(-12,214.5f),2.8f),(new(-6.5f,232),3.2f)};
            /// <summary>The plinths, compass bearings from C (45 north-east, then clockwise), as the contract has them; and the order the boss counts them in (0 NE, 1 NW, 2 SW, 3 SE).</summary>
            internal static readonly float[] PlinthBearings={45,135,225,315};
            internal static Vector2 Plinth(float bearing)=>C+new Vector2(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad))*PlinthOut;

            static float Chain(Vector2 p,(float x,float z,float r)[] chain)
            {
                float d=float.PositiveInfinity;
                for(int i=1;i<chain.Length;i++)
                {
                    Vector2 a=new(chain[i-1].x,chain[i-1].z),ab=new Vector2(chain[i].x,chain[i].z)-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);
                    d=Mathf.Min(d,Vector2.Distance(p,a+ab*t)-Mathf.Lerp(chain[i-1].r,chain[i].r,t));
                }
                if(chain.Length==1)d=Vector2.Distance(p,new Vector2(chain[0].x,chain[0].z))-chain[0].r;
                return d;
            }
            /// <summary>Metres outside the nearest peat (negative on it). A shore wanders, so no island is a row of arcs.</summary>
            internal static float MireShore(Vector2 p)
            {
                float d=Mathf.Min(Mathf.Min(Chain(p,IslandOne),Chain(p,IslandTwo)),Mathf.Min(Mathf.Min(Chain(p,DryWay),Chain(p,IslandThree)),Mathf.Min(Chain(p,Hummock),Chain(p,NorthShore))));
                return d+(Mathf.PerlinNoise(p.x*.07f+11,p.y*.07f+3)-.5f)*3.2f+(Mathf.PerlinNoise(p.x*.21f+5,p.y*.21f+9)-.5f)*1.1f;
            }
            /// <summary>The mire's ground: tussocky peat on the islands, a shelf of shallows round each, the bed of the deep water beyond, and the wading bands raised through it.</summary>
            internal static float MireHeight(Vector2 p)
            {
                float shore=MireShore(p);
                float top=Peat+.18f*(Mathf.PerlinNoise(p.x*.13f+7,p.y*.13f+2)-.5f)+.08f*(Mathf.PerlinNoise(p.x*.5f+3,p.y*.5f+1)-.5f)+Tussocks(p)*ZoneKit.Smooth(-.8f,-3,shore);
                float bed=DeepBed+.4f*(Mathf.PerlinNoise(p.x*.06f+31,p.y*.06f+7)-.5f);
                float h=Mathf.Lerp(top,bed,ZoneKit.Smooth(-.6f,4.8f,shore));
                foreach(var (a,b,half,ford) in Fords)
                {
                    float wade=ford+.06f*(Mathf.PerlinNoise(p.x*.4f,p.y*.4f)-.5f);
                    h=Mathf.Max(h,Mathf.Lerp(wade,-9,ZoneKit.Smooth(half,half+2.2f,ZoneKit.Segment(p,a,b))));
                }
                return h;
            }
            /// <summary>The peat's tussocks: hands-high humps, painted by the ground kit (ShadeRelief), so an island reads as grown and not as a board.</summary>
            internal static float Tussocks(Vector2 p)=>GroundRelief.Mounds(p,2.2f,.12f,.42f,7);
            static float RoundBox(Vector2 p,float x0,float x1,float z0,float z1,float round=.8f)
            {
                var d=new Vector2(Mathf.Abs(p.x-(x0+x1)*.5f)-(x1-x0)*.5f+round,Mathf.Abs(p.y-(z0+z1)*.5f)-(z1-z0)*.5f+round);
                return Mathf.Min(Mathf.Max(d.x,d.y),0)+new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude-round;
            }
            enum Room{Mire,Doorway,Hall,Alcove,Stair,Terrace,Low,PimStair,Pocket,VaultPassage,Ante,Vault,GreatStair,Landing,Passage,Arena}
            /// <summary>Metres outside the nearest floor (negative on one), and which floor. Each room meets its neighbour at one height, so there is no step where two overlap.</summary>
            static float Nearest(Vector2 p,out Room room,out int alcove)
            {
                float best=float.PositiveInfinity;var found=Room.Mire;
                void Try(float clear,Room which){if(clear<best){best=clear;found=which;}}
                Try(RoundBox(p,-64,78,-18,166,2),Room.Mire);
                Try(RoundBox(p,-4.5f,4.5f,163,172.5f,.5f),Room.Doorway);
                Try(RoundBox(p,-15,15,170,192.2f,.6f),Room.Hall);
                Try(RoundBox(p,9,15,191,207,.5f),Room.Stair);
                Try(RoundBox(p,8,22,206,250,.6f),Room.Terrace);
                Try(RoundBox(p,-22,8,206,250,.6f),Room.Low);
                Try(RoundBox(p,-28.2f,-21.5f,218.5f,224.5f,.4f),Room.PimStair);
                Try(RoundBox(p,-37,-27.5f,215,228,.6f),Room.Pocket);
                Try(RoundBox(p,21.5f,29.4f,214.5f,220.5f,.4f),Room.VaultPassage);
                Try(RoundBox(p,29,37.6f,211,225,.5f),Room.Ante);
                Try(RoundBox(p,37.4f,49,208,228,.6f),Room.Vault);
                Try(RoundBox(p,-4.4f,4.4f,249.5f,259.5f,.4f),Room.GreatStair);
                Try(RoundBox(p,-11,11,259,265,.5f),Room.Landing);
                Try(RoundBox(p,-4.4f,4.4f,264.5f,272,.4f),Room.Passage);
                Try(Vector2.Distance(p,C)-ArenaRadius,Room.Arena);
                alcove=-1;
                for(int i=0;i<Alcoves.Length;i++){var (x0,x1,z0,z1,_)=Alcoves[i];float c=RoundBox(p,x0,x1,z0,z1,.3f);if(c<best){best=c;found=Room.Alcove;alcove=i;}}
                room=found;return best;
            }
            static float Level(Vector2 p,Room room,int alcove)
            {
                switch(room)
                {
                    case Room.Mire:return MireHeight(p);
                    case Room.Doorway:return Mathf.Lerp(PorticoLevel,HallTop,ZoneKit.Smooth(166,171,p.y));
                    case Room.Hall:return Mathf.Lerp(HallTop,Mathf.Lerp(HallTop,HallSunk,ZoneKit.Smooth(175.5f,181.5f,p.y)),ZoneKit.Smooth(WalkEdge+.25f,WalkEdge-.25f,p.x));
                    case Room.Alcove:return Alcoves[alcove].level;
                    case Room.Stair:return Mathf.Lerp(HallTop,Terrace,Mathf.Clamp01((p.y-192)/14));
                    case Room.Terrace:return Terrace;
                    case Room.Low:
                    {
                        float floor=Low+.05f*(Mathf.PerlinNoise(p.x*.3f,p.y*.3f)-.5f);
                        foreach(var (at,r) in Sumps)floor=Mathf.Min(floor,Mathf.Lerp(SumpBed,Low,ZoneKit.Smooth(r-1.2f,r,Vector2.Distance(p,at))));
                        // The ramp up to the terrace is the floor's own north-east corner, so it meets the terrace at the terrace's height and the floor at the floor's: a wall along its south side.
                        if(p.y>RampSouth)floor=Mathf.Max(floor,Mathf.Lerp(Low,Terrace,Mathf.Clamp01((p.x-1.4f)/6.6f)));
                        return floor;
                    }
                    case Room.PimStair:return Mathf.Lerp(Low,Pocket,Mathf.Clamp01((-21.8f-p.x)/6));
                    case Room.Pocket:return Pocket;
                    case Room.VaultPassage:return Terrace;
                    case Room.Ante:return p.x<33.3f?Mathf.Lerp(Terrace,AnteFloor,Mathf.Clamp01((p.x-29.2f)/3.2f)):Mathf.Lerp(AnteFloor,VaultFloor,Mathf.Clamp01((p.x-34.2f)/3.2f));
                    case Room.Vault:return VaultFloor;
                    case Room.GreatStair:return Mathf.Lerp(Low,Landing,Mathf.Clamp01((p.y-250)/9));
                    case Room.Landing:return Landing;
                    case Room.Passage:return Mathf.Lerp(Landing,ArenaFloor,Mathf.Clamp01((p.y-265)/6));
                    case Room.Arena:return ArenaAt(p);
                }
                return 0;
            }
            /// <summary>The great hall: near level (a shallow bowl, never a step inside 17 m), with the four plinths at the rim, each with its ramp on the side that faces C.</summary>
            internal static float ArenaAt(Vector2 p)
            {
                float floor=ArenaFloor-.08f*(1-Mathf.Clamp01(Vector2.Distance(p,C)/ArenaRadius))+.03f*(Mathf.PerlinNoise(p.x*.4f+2,p.y*.4f+5)-.5f);
                foreach(float bearing in PlinthBearings)
                {
                    var at=Plinth(bearing);var off=p-at;float r=off.magnitude;if(r>PlinthRadius+4)continue;
                    // Steep all round but toward C, where it is a ramp a hero walks up (under 20 degrees).
                    var toC=(C-at).normalized;float facing=r<.01f?1:Vector2.Dot(off/r,toC);
                    float slope=Mathf.Lerp(4,.32f,ZoneKit.Smooth(.62f,.9f,facing));
                    floor=Mathf.Max(floor,PlinthTop-slope*Mathf.Max(0,r-PlinthRadius));
                }
                return floor;
            }
            internal static float FloorAt(Vector2 p,out float level){float clear=Nearest(p,out var room,out int alcove);level=Level(p,room,alcove);return clear;}
            static readonly Vector2 Away=new(Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));
            /// <summary>The ground. A floor is its room's level; between floors is the station's masonry (or, round the mire, its banks), which, with a floor beyond it along the camera's line,
            /// stays under the sightline to a hero at that floor's edge (measured from the lower of the two levels, as the Walking Canopy's root does, D119) and climbs away too steep to stand on.</summary>
            internal static float Height(Vector2 p)
            {
                float clear=FloorAt(p,out float level);if(clear<=0)return level;
                bool station=p.y>165;
                float allowed=float.PositiveInfinity;
                for(float d=.5f;d<=14;d+=.5f){if(FloorAt(p+Away*d,out float there)<0){allowed=Mathf.Min(level,there)+.6f+.82f*d;break;}}
                float rough=Mathf.PerlinNoise((p.x+70)*.11f,(p.y+20)*.09f);
                float top=Mathf.Min(allowed,level+(station?4.4f+2.2f*rough:1.6f+1.4f*rough));
                return level+(top-level)*Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/(station?.9f:2.4f)));
            }
            /// <summary>Whether a body could stand here: a floor, and in the mire above deep water.</summary>
            internal static bool Walkable(Vector2 p){if(FloorAt(p,out float level)>0)return false;return p.y>165||level>MireWater-.55f;}
            internal static bool InMire(Vector2 p)=>p.y<165;
            static bool InPocket(Vector2 p)=>p.x<-27.6f&&p.x>-38&&p.y>214&&p.y<229;
            static bool InVaultSide(Vector2 p)=>p.x>28.6f&&p.y>206&&p.y<230;
            static bool InArenaSide(Vector2 p)=>p.y>266.5f;

            /// <summary>Peat on the islands, the wet margin darker, silt under the water; the station's floors warm worn stone, greener the lower they lie (the water stood longest there).
            /// Kept darker than what stands on it.</summary>
            static Color Tone(Vector2 p)
            {
                FloorAt(p,out float level);float mottle=.9f+.2f*Mathf.PerlinNoise(p.x*.19f+3,p.y*.19f+7);
                Color tone;
                if(InMire(p))
                {
                    float patch=Mathf.PerlinNoise(p.x*.05f+2,p.y*.05f+40);
                    tone=Color.Lerp(new Color(.46f,.43f,.31f),new Color(.4f,.44f,.3f),ZoneKit.Smooth(.35f,.7f,patch));
                    tone=Color.Lerp(tone,new Color(.32f,.33f,.25f),ZoneKit.Smooth(MireWater+.35f,MireWater+.02f,level));
                    tone=Color.Lerp(tone,new Color(.26f,.32f,.28f),ZoneKit.Smooth(MireWater,MireWater-.6f,level));
                }
                else
                {
                    tone=Color.Lerp(new Color(.74f,.58f,.46f),new Color(.5f,.56f,.48f),ZoneKit.Smooth(.2f,-4.2f,level));
                    if(Vector2.Distance(p,C)<ArenaRadius+1)tone=Color.Lerp(tone,new Color(.46f,.5f,.44f),.5f);
                }
                return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
            }

            // ---------------------------------------------------------------- what it is made of
            internal static Material PeatFloor,Masonry,Bronze,Bark,Leaf,Pad,Lantern,Glow,Reed,Weave,Plaque,MireLook,StationLook,HallLook;
            static readonly Color BarkDark=new(.62f,.6f,.56f),BarkPale=new(1,.97f,.92f),LeafTop=new(.48f,.66f,.4f),LeafUnder=new(.24f,.34f,.26f),StoneWarm=new(1,.9f,.82f),StoneDark=new(.7f,.62f,.58f);
            static void Materials()
            {
                // Peat: dark earth mottled at two scales, with fine pale root threads wandering through it every way (a stretched strand read as rain streaks across a whole island) and
                // dark flecks. The ground kit's layers lie over it.
                var peat=ZoneKit.Texture("Wendmire/waystation-peat",512,(u,v)=>
                {
                    float big=ZoneKit.Cloud(u,v,5,3),fine=ZoneKit.Cloud(u,v,26,9),fleck=ZoneKit.Tile(u*90,v*90,90,17);
                    float thread=Mathf.Abs(ZoneKit.Cloud(u,v,9,41)-.5f),thin=Mathf.Abs(ZoneKit.Cloud(u,v,17,57)-.5f);
                    float tone=.6f+.24f*(big-.5f)+.16f*(fine-.5f)+(thread<.012f?.13f:0)+(thin<.008f?.08f:0)+(fleck<.06f?-.12f:0);
                    return new Color(tone*1.03f,tone,tone*.88f,1);
                });
                // Ashlar: courses of carved blocks, each its own tone, a dark joint between, and here and there a block carved with the old engineers' ring. Seven metres along by five up
                // a repeat on a wall (nine courses), eight by eight on a wall's top, where it reads as the blocks seen from above.
                var ashlar=ZoneKit.Texture("Wendmire/waystation-ashlar",512,(u,v)=>
                {
                    float row=v*9;int course=Mathf.FloorToInt(row);float up=row-course;float across=u*5+(course%2)*.5f+ZoneKit.Tile(course*1.7f,.3f,9,4)*.3f;int block=Mathf.FloorToInt(across);float along=across-block;
                    float own=ZoneKit.Tile(block*2.3f+course*7.1f,course*1.3f,45,11),grain=ZoneKit.Cloud(u,v,18,7),chip=ZoneKit.Tile(u*60,v*60,60,23);
                    bool joint=up<.07f||along<.025f;float bevel=Mathf.Min(Mathf.Min(up,1-up)*9,Mathf.Min(along,1-along)*14);
                    float tone=.66f+.16f*(own-.5f)+.12f*(grain-.5f)-(chip>.9f?.08f:0)+.06f*Mathf.Clamp01(1-bevel);
                    // A carved ring on about one block in nine: the works mark the pumps and the sentries share.
                    if(Mathf.Repeat(block*5+course*3,9)<1){var c=new Vector2(along-.5f,(up-.5f)*.36f);float ring=Mathf.Abs(c.magnitude-.17f);if(ring<.025f)tone-=.18f;}
                    if(joint)tone=.3f+.06f*own;
                    return new Color(tone*1.04f,tone*.96f,tone*.9f,1);
                });
                // Bronze gone green: streaks of verdigris down a warm metal, darker in the pits.
                var bronze=ZoneKit.Texture("Wendmire/waystation-bronze",256,(u,v)=>
                {
                    float streak=ZoneKit.Cloud(u*4,v*.6f,4,9),pit=ZoneKit.Tile(u*48,v*48,48,3),blot=ZoneKit.Cloud(u,v,6,2);
                    float green=ZoneKit.Smooth(.45f,.7f,streak*.7f+blot*.3f);float tone=.6f+.2f*(blot-.5f)-(pit>.88f?.12f:0);
                    return Color.Lerp(new Color(tone*1.1f,tone*.86f,tone*.56f),new Color(tone*.66f,tone*.98f,tone*.86f),green*.85f);
                });
                // Mangrove bark: smooth and pale, ringed with lenticels, a little lichen.
                var bark=ZoneKit.Texture("Wendmire/waystation-bark",256,(u,v)=>
                {
                    float ring=ZoneKit.Tile(u*6,v*30,6,4),lichen=ZoneKit.Cloud(u,v,7,8),dot=ZoneKit.Tile(u*24,v*40,24,13);
                    float tone=.66f+.1f*(ring-.5f)+(dot>.9f?-.12f:0);float green=ZoneKit.Smooth(.58f,.72f,lichen);
                    return Color.Lerp(new Color(tone*1.02f,tone,tone*.94f),new Color(tone*.82f,tone*.94f,tone*.76f),green*.6f);
                });
                // A leaf: a midrib and veins, for a frond's own uv (u across, v along).
                var leaf=ZoneKit.Texture("Wendmire/waystation-leaf",256,(u,v)=>
                {
                    float mid=Mathf.Abs(u-.5f),vein=Mathf.Abs(Mathf.Repeat(v*9-mid*5,1)-.5f);
                    float tone=.66f+.16f*(ZoneKit.Cloud(u,v,6,5)-.5f)+(mid<.03f?.16f:0)+(vein<.05f?.06f:0)-.18f*mid;
                    return new Color(tone,tone*1.02f,tone*.92f,1);
                });
                // A brass-rimmed plaque for the vault's wall: the ring and bar the pumps and the sentries carry, and rows of tally strokes under it, running out at the bottom.
                var plaque=ZoneKit.Texture("Wendmire/waystation-plaque",512,(u,v)=>
                {
                    float tone=.62f+.1f*(ZoneKit.Cloud(u,v,9,3)-.5f);var mark=new Vector2(u-.5f,v-.78f);
                    float ring=Mathf.Abs(mark.magnitude-.11f);if(ring<.018f)tone=.26f;
                    if(Mathf.Abs(mark.y)<.015f&&Mathf.Abs(mark.x)<.19f)tone=.26f;
                    for(int row=0;row<6;row++)
                    {
                        float y=.56f-row*.085f;if(Mathf.Abs(v-y)>.032f)continue;int strokes=row<5?20:9;
                        for(int k=0;k<strokes;k++){float x=.1f+k*.04f+(k/5)*.015f;if(Mathf.Abs(u-x)<.006f)tone=.3f;if(k%5==4&&Mathf.Abs((u-x+.08f)-(v-y)*1.6f)<.008f&&u>x-.17f&&u<x+.01f)tone=.3f;}
                    }
                    float edge=Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v));if(edge<.03f)tone=.48f;
                    return new Color(tone*1.05f,tone*.95f,tone*.86f,1);
                });
                PeatFloor=ZoneKit.Toon("waystation-floor",peat,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_BaseColor",Color.white);m.SetFloat("_BaseMapStrength",.9f);m.SetColor("_ShadowTint",new Color(.5f,.52f,.66f));m.SetFloat("_LightFloor",.18f);});
                Masonry=ZoneKit.Toon("waystation-masonry",ashlar,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.94f,.66f,.5f));m.SetColor("_ShadowTint",new Color(.5f,.5f,.66f));m.SetFloat("_LightFloor",.26f);});
                Bronze=ZoneKit.Toon("waystation-bronze",bronze,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.9f,.82f,.62f));m.SetColor("_ShadowTint",new Color(.48f,.52f,.62f));m.SetColor("_RimColor",new Color(.6f,.5f,.36f));m.SetFloat("_LightFloor",.3f);});
                Bark=ZoneKit.Toon("waystation-bark",bark,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.78f,.74f,.66f));m.SetColor("_ShadowTint",new Color(.48f,.5f,.64f));m.SetFloat("_LightFloor",.28f);});
                Leaf=ZoneKit.Toon("waystation-leaf",leaf,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);ZoneKit.Swaying(m,.06f,1.1f,1.6f);m.SetColor("_ShadowTint",new Color(.42f,.5f,.58f));m.SetFloat("_LightFloor",.3f);});
                // What floats (pads, scum): drawn after the water and before danger markings, writing no depth, so a marking on the floor under a pad still shows over it.
                Pad=ZoneKit.Toon("waystation-pad",leaf,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.42f,.5f,.58f));m.SetFloat("_LightFloor",.3f);
                    m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);
                    m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent-14;});
                Reed=ZoneKit.Toon("waystation-reed",null,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);ZoneKit.Swaying(m,.05f,1.3f,1.2f);m.SetFloat("_LightFloor",.32f);});
                Weave=ZoneKit.Toon("waystation-weave",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Xylos/xylos-weave.png"),m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.86f,.74f,.54f));m.SetFloat("_LightFloor",.28f);});
                Plaque=ZoneKit.Toon("waystation-plaque",plaque,m=>{m.SetColor("_BaseColor",new Color(.95f,.72f,.5f));m.SetFloat("_LightFloor",.3f);});
                // A kept lamp's glass and a glow-fungus cap: lit from inside, so they read in shade and bloom a little. A RhythmLamp sets the emission in play.
                Lantern=ZoneKit.Toon("waystation-lantern",null,m=>{m.SetColor("_BaseColor",new Color(1,.8f,.5f));m.SetColor("_EmissionColor",new Color(2f,1.3f,.5f));});
                Glow=ZoneKit.Toon("waystation-glow",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.6f,.9f,.8f));m.SetColor("_EmissionColor",new Color(.5f,1.3f,1f));});
                // The mire's water: tea-brown over the shallows going to a deep green-black, with fine glints. The station's is stiller and darker; the great hall's darker still.
                // Murky olive over the shallows going to a deep tea-green (a blue-black deep read as night, and its glints as grey blotches); the glints' grain small and faint.
                MireLook=WaterKit.Look("waystation-mire-water",new Color(.42f,.44f,.3f,.5f),new Color(.12f,.19f,.14f,.95f),new Color(.82f,.84f,.7f,1),1.1f,2.6f,.05f);
                StationLook=WaterKit.Look("waystation-station-water",new Color(.36f,.42f,.32f,.52f),new Color(.1f,.16f,.13f,.96f),new Color(.8f,.84f,.72f,1),.9f,2.6f,.04f);
                HallLook=WaterKit.Look("waystation-hall-water",new Color(.32f,.42f,.34f,.5f),new Color(.11f,.17f,.14f,.92f),new Color(.84f,.88f,.76f,1),1.2f,2.4f,.05f);
                // Drawn after the ground kit's flat details (Transparent-20) and still before danger markings (-10): the station's floors are dressed for when they are drained, and a crack
                // or a drift of silt under a stage's water has to be seen through the water, not lying on top of it.
                foreach(var look in new[]{MireLook,StationLook,HallLook})look.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent-15;
                AssetDatabase.SaveAssets();
            }

            // ---------------------------------------------------------------- pieces
            /// <summary>The enemy if the combat engineer has defined it, else a stand-in of the same family (logged), so the zone is walkable and fought before the roster lands.</summary>
            static string Enemy(string id)
            {
                if(ZoneKit.Find<EnemyDef>(id)!=null)return id;
                string stand=id switch{"Bogstrider"=>"Thornback","Lurelight"=>"Reedstalker","MireLeeches"=>"MireflySwarm","RotPod"=>"Sporecaster","DrownedSentry"=>"Rootwarden","Rotbloom"=>"Floodcrown",_=>id};
                Debug.Log("WAYSTATION_STAND_IN "+stand+" for "+id);return stand;
            }
            static EncounterVolume Fight(ZoneKit kit,string id,Vector2 at,Vector2 size,string requiredFlag,params (string enemy,int count,Vector2 from,float yaw)[] packs)
            {
                var encounter=WorldBuilder.Encounter(id,kit.On(at.x,at.y,1.5f),new Vector3(size.x,7,size.y),Enemy(packs[0].enemy),packs[0].count);
                var spawners=new List<Spawner>{encounter.spawners[0]};
                for(int i=1;i<packs.Length;i++)spawners.Add(ZoneKit.Spawn(encounter,Enemy(packs[i].enemy),packs[i].count,Vector3.zero,2.4f));
                for(int i=0;i<packs.Length;i++)
                {
                    var spawner=spawners[i];spawner.transform.SetPositionAndRotation(kit.On(packs[i].from.x,packs[i].from.y),Quaternion.Euler(0,packs[i].yaw,0));
                    spawner.faceAsPlaced=packs[i].yaw!=0;spawner.radius=packs[i].count>1?2.4f:0;
                }
                encounter.spawners=spawners.ToArray();encounter.requiredFlag=requiredFlag??"";return encounter;
            }
            /// <summary>A pump of the old engineers' (the kit's AncelanePump: a carved drum with a wheel), with a bronze crank on its head that turns as it runs. The hand is a discovery at its camera
            /// side; the switch is what answers (the crank, the rush of water, and the view drawn back over what drains).</summary>
            static FlowSwitch Pump(ZoneKit kit,string name,Vector2 at,float yaw,Vector2 hand,string flag,string report,string node,string speaker,string quest,int step,string required,Bounds shows,string situation)
            {
                var body=kit.Prop(ZoneKit.Kit("AncelanePump","HushwellColumn"),new Vector3(at.x,0,at.y),2.2f,yaw);body.name=name;
                var head=kit.On(at.x,at.y,2.36f);
                var crank=new ZoneKit.Shape();
                crank.Slot(0).Post(Vector3.down*.3f,.62f,.07f,.06f,8,StoneDark,BarkPale).Beam(new Vector3(-.62f,.26f,0),new Vector3(.62f,.26f,0),.09f,.09f,BarkPale);
                foreach(int side in new[]{-1,1})crank.Slot(0).Post(new Vector3(side*.6f,.2f,0),.32f,.06f,.06f,6,BarkPale,BarkPale);
                var turns=kit.Built(name+" crank",crank,Bronze);turns.transform.SetPositionAndRotation(head,Quaternion.Euler(0,yaw,0));
                var point=kit.Discovery(name+" handle",new Vector3(hand.x,0,hand.y),"Run the pump",flag,report,node,speaker,quest,step,2.9f);
                point.requiredFlag=required??"";point.situation=situation??"";
                var run=body.AddComponent<FlowSwitch>();run.flag=flag;run.wheel=turns.transform;run.axis=Vector3.up;run.turns=2;run.seconds=3.4f;run.point=point;run.shows=shows;run.marker="PUMP_RUN";run.volume=.5f;
                return run;
            }
            /// <summary>A kept lamp: the kit's pulse beacon (a lantern on a reed-bound post), its light, and the count it keeps.</summary>
            static RhythmLamp PoleLamp(ZoneKit kit,string name,Vector2 at,float yaw,string count,float offset,Color colour,float range=9,float intensity=2.6f)
            {
                var post=kit.Prop(ZoneKit.Kit("PulseBeacon","LampPost"),new Vector3(at.x,0,at.y),3.6f,yaw);post.name=name;
                // Solid as a post is, and no wider: the piece's own box took in the lantern's arm and stood a body's width across, hiding a hero a few metres north of it in a fight.
                foreach(var box in post.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(box);
                ZoneKit.Block(post,kit.On(at.x,at.y,1.6f),new Vector3(.45f,3.2f,.45f),0,"Lamp post");
                var light=kit.Lamp(new Vector3(at.x,3.15f,at.y),range,intensity,colour);light.transform.SetParent(post.transform,true);
                var lamp=post.AddComponent<RhythmLamp>();lamp.count=count;lamp.glow=light;lamp.lamps=post.GetComponentsInChildren<Renderer>();lamp.colour=colour;lamp.offset=offset;lamp.intensity=intensity;
                return lamp;
            }
            /// <summary>A mangrove: a trunk standing on arched prop roots that reach out and down into the water, and a crown of long leaves at its head. Bark in slot 0, leaves in slot 1.</summary>
            static void Mangrove(ZoneKit.Shape shape,Vector3 foot,float tall,float girth,int seed,bool crown=true)
            {
                var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();
                var lean=new Vector3(R()-.5f,0,R()-.5f)*.3f;var bole=foot+Vector3.up*tall*.22f;var path=new Vector3[5];var radii=new float[5];
                for(int i=0;i<5;i++){float t=i/4f;path[i]=bole+Vector3.up*(tall*.78f)*t+lean*tall*t*t;radii[i]=girth*Mathf.Lerp(1,.55f,t);}
                shape.Slot(0).Tube(path,radii,7,BarkDark,BarkPale,2,.25f);
                int roots=6+rng.Next(4);float turn=R()*360;
                for(int k=0;k<roots;k++)
                {
                    var away=Quaternion.Euler(0,turn+k*360f/roots+R()*20,0)*Vector3.forward;float reach=girth*(3.2f+R()*2.4f),from=tall*(.12f+.14f*R());
                    var start=bole+Vector3.up*(from-tall*.22f)+away*girth*.5f;var land=foot+away*reach+Vector3.down*.5f;
                    var knee=Vector3.Lerp(start,land,.45f)+Vector3.up*(from*.45f+.4f);
                    shape.Tube(new[]{start,Vector3.Lerp(start,knee,.6f),knee,Vector3.Lerp(knee,land,.55f),land},new[]{girth*.32f,girth*.27f,girth*.23f,girth*.2f,girth*.18f},5,BarkDark,BarkPale,1,.3f);
                }
                if(!crown)return;
                // A mangrove's crown is a rounded dome of short leaves, not a palm's head: many small sprays over a half-sphere.
                var head=path[4];int sprays=11+rng.Next(6);float dome=1.2f+tall*.16f;
                for(int k=0;k<sprays;k++)
                {
                    float a=R()*6.28f,up=R();var at=head+new Vector3(Mathf.Cos(a)*dome*Mathf.Sqrt(1-up*up),dome*.55f*up-.2f,Mathf.Sin(a)*dome*Mathf.Sqrt(1-up*up));
                    shape.Slot(1).Fern(at,.8f+R()*.6f,9,rng,Color.Lerp(LeafUnder,LeafTop,.4f+.3f*up),Color.Lerp(LeafTop,new Color(.62f,.7f,.4f),R()*.5f));
                }
                shape.Slot(0);
            }
            /// <summary>A clump of reeds standing in the shallows: a fan of thin blades with seed heads. Slot as given.</summary>
            static void Reeds(ZoneKit.Shape shape,int slot,Vector3 foot,float spread,int count,int seed,float tallest=1.4f)
            {
                var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();
                for(int k=0;k<count;k++)
                {
                    float a=R()*6.28f,far=spread*Mathf.Sqrt(R()),tall=tallest*(.55f+.45f*R());var at=foot+new Vector3(Mathf.Cos(a)*far,-.1f,Mathf.Sin(a)*far);var lean=new Vector3((R()-.5f)*.35f,1,(R()-.5f)*.35f);
                    shape.Slot(slot).Tube(new[]{at,at+lean*tall*.5f,at+lean*tall+Vector3.right*.05f},new[]{.03f,.024f,.01f},4,new Color(.44f,.5f,.3f),new Color(.7f,.72f,.44f),1,1,false);
                    if(k%3==0)shape.Slot(slot).Tube(new[]{at+lean*tall*.86f,at+lean*tall*1.02f},new[]{.05f,.035f},5,new Color(.46f,.34f,.24f),new Color(.36f,.26f,.18f),1,1,true);
                }
            }
            /// <summary>A tussock of sedge: a dense fan of broad blades, olive at the foot and straw at the tips, bent out from a dark crown. Thick enough to read at the camera's distance,
            /// as single reeds don't.</summary>
            static void Sedge(ZoneKit.Shape shape,Vector3 foot,float spread,int count,int seed,float tallest)
            {
                var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();
                shape.Slot(0).Lump(foot+Vector3.up*.03f,new Vector3(spread*.7f,.09f,spread*.7f),seed,.3f,new Color(.36f,.34f,.2f),new Color(.24f,.22f,.14f),8,4);
                for(int k=0;k<count;k++)
                {
                    float a=R()*6.28f,out_=.35f+.65f*R(),tall=tallest*(.6f+.4f*R());var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var at=foot+dir*spread*.25f*R();
                    var tip=at+dir*spread*out_*1.6f+Vector3.up*tall;var mid=Vector3.Lerp(at,tip,.5f)+Vector3.up*tall*.18f;
                    var top=Color.Lerp(new Color(.74f,.7f,.4f),new Color(.56f,.62f,.32f),R());
                    shape.Slot(0).Tube(new[]{at,mid,tip},new[]{.05f,.04f,.012f},4,new Color(.4f,.44f,.24f),top,1,1,false);
                }
            }
            /// <summary>Pads of mire-lily on still water: a few round leaves to a raft, each with its notch, a pale flower now and then. Slot 0 leaves, slot 1 flowers. A leaf lies only over
            /// water between the two depths.</summary>
            static int Pads(ZoneKit.Shape shape,System.Random rng,Vector2 centre,float level,int leaves,float size,Func<Vector2,float> depth,float shallowest=.25f,float deepest=9)
            {
                float turn=(float)rng.NextDouble()*6.28f;int laid=0;
                for(int k=0;k<leaves;k++)
                {
                    float a=turn+k*2.4f,far=(.25f+.42f*Mathf.Sqrt(k))*size*2.2f,r=size*(.6f+.6f*(float)rng.NextDouble());var at=new Vector3(centre.x+Mathf.Cos(a)*far,level+.02f+.004f*k,centre.y+Mathf.Sin(a)*far);
                    float under=depth(new Vector2(at.x,at.z));if(under<shallowest||under>deepest)continue;
                    // A pad is a disc with a wedge out of it: eight of nine segments, turned at random.
                    var tone=Color.Lerp(new Color(.42f,.56f,.32f),new Color(.6f,.64f,.36f),(float)rng.NextDouble());float spin=(float)rng.NextDouble()*6.28f;
                    for(int s=0;s<8;s++){float a0=spin+s*.7f,a1=a0+.7f;shape.Slot(0).Triangle(at,at+new Vector3(Mathf.Cos(a0),0,Mathf.Sin(a0))*r,at+new Vector3(Mathf.Cos(a1),0,Mathf.Sin(a1))*r,Vector3.up,s%2==0?tone:ZoneKit.Shape.Dim(tone,.9f),.6f);}
                    if(rng.Next(6)==0)shape.Slot(1).Lump(at+Vector3.up*.07f,new Vector3(.12f,.08f,.12f),k+(int)(centre.x*7),.25f,new Color(1.2f,1.1f,1.15f),new Color(1.1f,.8f,1),6,4);
                    laid++;
                }
                return laid;
            }
            /// <summary>A grille of bronze bars across a way, solid and cut out of the paths while it stands: Pim's, and the great hall's door until pump three lifts it. A Membrane, shut, as
            /// the Walking Canopy's root doors are, so whatever opens every seal to read the baked floor opens it too; a FlagGate takes it away.</summary>
            /// <summary>How far Pim's talk reaches: across the grille and a good stride of the stair's head in front of it.</summary>
            internal const float PimHeardFrom=6;
            static GameObject GrilleDoor(ZoneKit kit,string name,Vector3 from,Vector3 to,float tall)
            {
                var shape=new ZoneKit.Shape();var run=to-from;float length=run.magnitude;var along=run/length;var across=Vector3.Cross(Vector3.up,along);
                int bars=Mathf.Max(4,Mathf.RoundToInt(length/.32f));
                for(int i=0;i<=bars;i++){var foot=from+along*(length*i/bars)+Vector3.down*.2f;shape.Slot(0).Post(foot,tall+.2f,.045f,.04f,6,StoneDark,BarkPale);}
                foreach(float high in new[]{.12f,.55f,.95f})shape.Slot(0).Beam(from+Vector3.up*tall*high,to+Vector3.up*tall*high,.09f,.12f,BarkDark,BarkPale);
                var door=kit.Built(name,shape,Bronze);door.AddComponent<Membrane>();
                var solid=new GameObject("Grille bars",typeof(BoxCollider),typeof(NavMeshObstacle));solid.transform.SetParent(door.transform,true);
                solid.transform.SetPositionAndRotation((from+to)*.5f+Vector3.up*tall*.5f,Quaternion.LookRotation(across));
                var size=new Vector3(length+.6f,tall+.4f,.8f);solid.GetComponent<BoxCollider>().size=size;
                var carve=solid.GetComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.size=size;carve.carving=true;carve.carveOnlyStationary=false;
                return door;
            }
            /// <summary>A cache of the mire's: an Ilu basket with its lid tied down; in the station, a bronze-bound coffer of the old engineers'.</summary>
            static LootCache Stash(ZoneKit kit,string id,string label,Vector2 at,float yaw,int scrip,bool coffer,params LootEntry[] contents)
            {
                var shape=new ZoneKit.Shape();GameObject body;
                if(coffer)
                {
                    shape.Slot(0).Box(new Vector3(0,.36f,0),new Vector3(1.3f,.72f,.8f),Quaternion.identity,StoneDark,StoneWarm).Box(new Vector3(0,.8f,0),new Vector3(1.36f,.16f,.86f),Quaternion.identity,StoneWarm);
                    foreach(float x in new[]{-.42f,.42f})shape.Slot(1).Box(new Vector3(x,.45f,0),new Vector3(.1f,.92f,.9f),Quaternion.identity,BarkPale);
                    shape.Slot(1).Box(new Vector3(0,.5f,-.42f),new Vector3(.22f,.26f,.06f),Quaternion.identity,BarkPale);
                    body=kit.Built("Coffer "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),shape.Slot(1),Masonry,Bronze);
                }
                else
                {
                    shape.Slot(0).Post(Vector3.zero,.7f,.48f,.58f,10,new Color(.86f,.8f,.66f),Color.white,.8f).Lump(new Vector3(0,.74f,0),new Vector3(.6f,.18f,.6f),3,.05f,Color.white,new Color(.7f,.64f,.5f),10,5);
                    shape.Slot(1).Rope(new Vector3(-.6f,.56f,0),new Vector3(.6f,.56f,0),-.3f,.035f,new Color(.84f,.76f,.5f)).Rope(new Vector3(0,.56f,-.6f),new Vector3(0,.56f,.6f),-.3f,.035f,new Color(.84f,.76f,.5f));
                    body=kit.Built("Basket "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),shape.Slot(1),Weave,Bronze);
                }
                body.transform.SetPositionAndRotation(kit.On(at.x,at.y),Quaternion.Euler(0,yaw,0));
                var box=body.AddComponent<BoxCollider>();box.center=Vector3.up*.45f;box.size=coffer?new Vector3(1.4f,.9f,.9f):new Vector3(1.2f,.9f,1.2f);body.isStatic=true;
                var cache=LootCache.On(body,id,label,scrip,contents);cache.range=2.6f;return cache;
            }
            /// <summary>A shelf of glow-fungus: three or four caps stepped up a surface, teal at the gills. What lights the mire's dark places and the station's walls.</summary>
            static void Fungus(ZoneKit.Shape shape,Vector3 at,Vector3 outward,int seed,float size=.3f)
            {
                var rng=new System.Random(seed);outward.y=0;outward.Normalize();
                for(int k=0;k<3+rng.Next(2);k++)
                {
                    var c=at+Vector3.up*(k*size*.9f)+Quaternion.Euler(0,(float)rng.NextDouble()*60-30,0)*outward*size*(.3f+.3f*(float)rng.NextDouble());float r=size*(.6f+.5f*(float)rng.NextDouble());
                    shape.Slot(1).Disc(c+Vector3.down*.02f,Vector3.down,r*.92f,10,new Color(.7f,1.3f,1.1f));
                    shape.Slot(0).Disc(c,Vector3.up,r,10,new Color(.9f,.86f,.74f),0,new Color(.66f,.58f,.5f));
                }
            }
            static Vector3 OnFloor(ZoneKit kit,Vector2 p,float up=0)=>kit.On(p.x,p.y,up);
            /// <summary>A kit piece fitted by how long it lies, where Prop fits by how tall it stands; drawn only, at a world height (a raft on the water).</summary>
            static GameObject Lying(ZoneKit kit,string key,Vector3 at,float length,float yaw,bool solid=false,bool grounded=false)
            {
                var probe=WorldBuilder.Piece(key,Vector3.zero,Vector3.one,"Rock",false);var box=ModelGeometry.BoundsOf(probe);UnityEngine.Object.DestroyImmediate(probe);
                return kit.Prop(key,at,length*box.size.y/Mathf.Max(box.size.x,box.size.z),yaw,solid,grounded);
            }

            // ---------------------------------------------------------------- the zone
            internal static void Build()
            {
                Materials();
                var kit=ZoneKit.Ground(Station,Height,1.2f);bool greybox=DevArgs.Has("-waystation-greybox");
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Ambience/AMB_Mire.ogg")??AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Ambience/AMB_BraidedRiver.ogg");
                if(bed!=null){var def=ZoneKit.Find<ZoneDef>(Station);if(def!=null&&def.ambientAudio==null){def.ambientAudio=bed;def.ambientVolume=.18f;EditorUtility.SetDirty(def);}}
                var scene=WorldBuilder.Begin(Station);
                kit.Terrain("Mire and waystation",-64,78,-18,312,.5f,PeatFloor,Masonry,Tone,new Color(1,.96f,.9f),new Color(.6f,.68f,.6f));

                // ---- water. The mire never moves. The station's is one body the pumps take down a stage at a time (StagedWater); the vault's sump is its own, behind its sill; the
                // great hall's is the arena's (FloodBasin), drained by the Rotbloom at its shift and for good once it is dead.
                var mireSheet=WaterKit.Sheet(kit,"Mire water",-64,78,-18,166,1,MireWater,MireLook,InMire,float.NaN,"River");
                var mire=WaterKit.Volume(mireSheet,MireWater,(new Vector2(7,74),new Vector2(142,184),0));
                WaterKit.Deep(kit,mire,-64,78,-18,166,p=>p.y<164.5f);
                var stationSheet=WaterKit.Sheet(kit,"Station water",-30,26,166,266,.5f,MireWater,StationLook,p=>p.y>167&&p.y<265.6f&&!InPocket(p)&&!InVaultSide(p),float.NaN,"River");
                var station=WaterKit.Volume(stationSheet,MireWater,(new Vector2(0,181.5f),new Vector2(32,24),0),(new Vector2(12,199),new Vector2(8,16),0),(new Vector2(-2,228),new Vector2(54,48),0),(new Vector2(0,258),new Vector2(24,14),0));
                var stageOne=WaterKit.Deep(kit,station,-30,26,166,266,p=>p.y>167&&kit.height(p)>StageOne-.55f&&!InPocket(p)&&!InVaultSide(p));
                station.level=StageOne;var stageTwo=WaterKit.Deep(kit,station,-30,26,166,266,p=>p.y>167&&!InPocket(p)&&!InVaultSide(p));station.level=MireWater;
                var holders=station.deep.ToArray();station.deep=Array.Empty<GameObject>();
                var staged=station.gameObject.AddComponent<StagedWater>();staged.water=station;staged.flags=new[]{"wend.pump1","wend.pump2"};staged.levels=new[]{StageOne,StageTwo};staged.blockers=holders;staged.seconds=4;
                var anteSheet=WaterKit.Sheet(kit,"Vault sump water",28,39,209,227,.5f,AnteWater,StationLook,p=>p.x>29.2f&&p.x<37.6f&&p.y>210&&p.y<226,float.NaN,"River");
                var ante=WaterKit.Volume(anteSheet,AnteWater,(new Vector2(33.4f,218),new Vector2(8.6f,15),0));ante.drainFlag="wend.pump4";ante.drainedLevel=AnteDrained;ante.drainSeconds=3.2f;
                WaterKit.Deep(kit,ante,28,39,209,227,p=>p.x>29.2f&&p.x<37.6f);
                var hallSheet=WaterKit.Sheet(kit,"Great hall water",-20,20,265,309,.5f,GreatFull,HallLook,InArenaSide,float.NaN,"River");
                var hall=WaterKit.Volume(hallSheet,GreatFull,(new Vector2(0,288.5f),new Vector2(40,45),0));hall.drainFlag="bossdown.Rotbloom";hall.drainedLevel=GreatEmpty;hall.drainSeconds=3;
                var basin=new GameObject("Great hall flood",typeof(FloodBasin)).GetComponent<FloodBasin>();basin.water=hall;basin.fullLevel=GreatFull;basin.emptyLevel=GreatEmpty;basin.seconds=3;basin.followsGates=false;
                // A basin that doesn't follow gates fills itself on its first reading: a won hall loads with it taken away, so the water stays where its drain flag left it.
                var won=new GameObject("Great hall flood (gone once the Rotbloom is dead)",typeof(FlagGate)).GetComponent<FlagGate>();won.flag="bossdown.Rotbloom";won.showWhenSet=false;won.targets=new[]{basin.gameObject};

                // ---- ways in and out, services
                WorldBuilder.Spawn("Arrival",OnFloor(kit,Arrival));WorldBuilder.Spawn("Portico",OnFloor(kit,new Vector2(2.5f,158)));WorldBuilder.Spawn("Door",OnFloor(kit,DoorSpawn));
                var raft=Lying(kit,ZoneKit.Kit("ReedRaft","CargoPallet"),new Vector3(Raft.x,MireWater-.28f,Raft.y),8,-18);raft.name="Pilots' raft";
                var home=new GameObject("Mire gate — Driftreed");home.transform.position=OnFloor(kit,new Vector2(-9.2f,-1.6f),1);
                kit.Way(home,"Mire gate — Driftreed",Town,"Mire",null,null,3.6f,new Vector2(-3.4f,-1.2f));
                var cradle=kit.Prop("DeckConsole",new Vector3(Cradle.x,0,Cradle.y),1.4f,80).AddComponent<RepairBay>();cradle.free=true;cradle.saveSpawn="Arrival";cradle.prompt="Pilots' cradle — repair and save";
                ChapterOneStaging.CradleBay("Pilots' cradle",cradle.gameObject,OnFloor(kit,Cradle),kit.Y(Cradle.x,Cradle.y)+.02f,false,true);
                var anvil=kit.Prop("LatticeAnvil",new Vector3(Bench.x,0,Bench.y),1.8f,50).AddComponent<Lattice.World.Bench>();anvil.prompt="Fabricate — Lattice Anvil";

                // ---- the first island: the landing, Maru with her lamp, and a Bogstrider pocket on the far side of her
                kit.Sign("FIRST ISLAND",3.5f,5.2f,1.8f);
                Maru(kit,"Maru on the first island",MaruIsland,225,"wend.tideTurned","wend.lureLearned",null);
                kit.Crossing("Step where Maru steps",new Vector3(2,0,17.6f),new Vector3(40,5,3),"wend.mireEntered","MaruMireGate","Sela");
                Fight(kit,"Mire_Strider",StriderPocket,new Vector2(30,18),null,("Bogstrider",1,new Vector2(-3,37),160),("Bogstrider",1,new Vector2(9,36.5f),200));
                PoleLamp(kit,"Ford lamp",new Vector2(14,43.6f),200,"SSL",0,new Color(1,.78f,.46f));

                // ---- the second island: the lure lesson. A kept lamp at the back keeping two short, one long; nearer, the lure posts, one flickering shorts with a Lurelight on it.
                kit.Sign("SECOND ISLAND",24.5f,52.6f,1.8f);
                var lures=Fight(kit,"Mire_Lures",new Vector2(27,72.5f),new Vector2(32,22),null,("Lurelight",1,new Vector2(LureHides[0].x,LureHides[0].z),180),("Lurelight",1,new Vector2(LureHides[2].x,LureHides[2].z),200));
                var lesson=kit.Crossing("Two lamps ahead",new Vector3(LureLesson.x,0,LureLesson.y),new Vector3(32,5,3),"wend.lureLearned","MaruLureLesson","Maru");
                lesson.report="LureLesson";lesson.previewEncounter=lures;
                var lessonView=new GameObject("Lure lesson view");lessonView.transform.position=OnFloor(kit,LureLesson);lures.previewPoint=lessonView.transform;lures.previewRadius=9;
                PoleLamp(kit,"Kept lamp",RealLamp,200,"SSL",.4f,new Color(1,.78f,.46f),10,3);
                var lureLight=new GameObject("Lure flicker",typeof(Light));lureLight.transform.position=kit.On(LureHides[0].x,LureHides[0].z,3);
                var flick=lureLight.GetComponent<Light>();flick.type=LightType.Point;flick.range=8;flick.color=new Color(.92f,1,.62f);flick.shadows=LightShadows.None;
                var flicker=lureLight.AddComponent<RhythmLamp>();flicker.count="";flicker.glow=flick;flicker.intensity=3.2f;flicker.darkWhen="clear.Mire_Lures";
                foreach(var (x,z) in LureHides)Hide(kit,x,z);
                // The bell pocket off the island's east side, across a short ford: the school raft's bell-buoy fouled in a root, and a fisher's tin.
                var bell=kit.Discovery("Bell-buoy",new Vector3(BellHand.x,0,BellHand.y),"Ring the bell-buoy","wend.bellRung","BellBuoy","SelaBellBuoy","Taren","SchoolBell",0,3);
                Stash(kit,"wend.mire.bell","Fisher's tin",BellCache,30,70,false,ProgressionBuilder.Drop("Glowspore",2,1),ProgressionBuilder.Drop("RepairGel",2,1),ProgressionBuilder.Drop("ReedWeave",2,1));
                BellBuoyBody(kit,bell);

                // ---- the leech channel, and the dry spit round it
                kit.Sign("LEECH CHANNEL",15.5f,85.5f,1.8f);
                Fight(kit,"Mire_Leeches",LeechMiddle,new Vector2(24,16),null,("MireLeeches",1,new Vector2(12,92),0),("MireLeeches",1,new Vector2(3,97.5f),0));
                var leechHail=new GameObject("Maru calls the leech channel",typeof(RadioHail)).GetComponent<RadioHail>();leechHail.transform.position=OnFloor(kit,new Vector2(20,84));
                leechHail.speaker="Maru";leechHail.situations=new[]{"hail.leech"};leechHail.heardFlag="wend.leechHailed";leechHail.range=10;
                // The party's own word on it (barks.json "leech"), from whoever leads, as the first steps into the channel wake the leeches: five metres in from the mouth, past Maru's hail.
                kit.Crossing("The leech channel",new Vector3(19.3f,0,87.8f),new Vector3(8,5,8),"wend.leechSeen",null,"Taren").situation="leech";
                Stash(kit,"wend.mire.spit","Drowned pilot's chest",DryCache,-40,120,false,ProgressionBuilder.AnyPart(3,GearSlot.Drive,1),ProgressionBuilder.Drop("ChargeCell",2,1));

                // ---- the third island: a strider and a lure together, the first fallen stone of the station, and the causeway north
                Fight(kit,"Mire_Third",ThirdPocket,new Vector2(26,24),null,("Bogstrider",1,new Vector2(-1,126),200),("Lurelight",1,new Vector2(ThirdHides[0].x,ThirdHides[0].z),180));
                foreach(var (x,z) in ThirdHides)Hide(kit,x,z);
                PoleLamp(kit,"Causeway lamp west",new Vector2(-3.8f,145),160,"SSL",.9f,new Color(1,.78f,.46f));
                PoleLamp(kit,"Causeway lamp east",new Vector2(4,151.5f),200,"SSL",1.6f,new Color(1,.78f,.46f));
                int n=0;
                foreach(var (x,z) in new[]{(-9,37.5f),(33.5f,83.5f),(-15,91.5f),(-26,111),(54.5f,65.6f)})Harvest(kit,"wend_glow_"+n,new Vector2(x,z),300+n++);

                // ---- the portico. Maru waits here from the lure lesson on, with her lamp; Pim joins her once found.
                kit.Sign("PORTICO",6.2f,156.4f,1.8f);
                kit.Crossing("The station's door",new Vector3(0,0,160.4f),new Vector3(34,5,3),"wend.porticoReached","MaruPortico","Maru");
                var after=kit.Crossing("Out under Maru's lamp",new Vector3(0,0,165.2f),new Vector3(9,5,2.4f),"wend.lampCarried","MaruAfterBloom","Maru","wend.rotbloomDead");
                Maru(kit,"Maru at the portico",MaruPortico,155,"wend.lureLearned","wend.lampCarried",new Vector2(-8.2f,163.6f));
                var pimWaits=kit.Person("Pim",new Vector3(PimPortico.x,0,PimPortico.y),165,"",false);pimWaits.repeatNode="PimPortico";pimWaits.firstNode="PimPortico";
                Placed(pimWaits.gameObject,"Pim at the portico","wend.pimFound","wend.rotbloomDead",true);
                kit.Arch("Station portico",new Vector3(0,0,167.6f),7.6f,0);

                // ---- the entry hall. The walk along its east side is dry; the floor west of it lies under the water with two sentries standing in their alcoves at the back. Pump one
                // stands on the walk; the stair down to the gallery leaves from its north end.
                kit.Sign("ENTRY HALL",-6,172,1.8f);
                kit.Crossing("The old engineers' hall",new Vector3(0,0,173.6f),new Vector3(28,5,3),"wend.hallSeen","SelaWaystationHall","Sela");
                Pump(kit,"Pump one",PumpOne,200,PumpOneHand,"wend.pump1","Pump","SelaPumpOne","Taren",null,0,null,new Bounds(new Vector3(-3,-1,185),new Vector3(26,4,18)),"");
                var hallFight=Fight(kit,"Hall_Sentries",new Vector2(-1,190.2f),new Vector2(32,7.6f),"wend.pump1",("DrownedSentry",1,new Vector2(-9,193.2f),180),("DrownedSentry",1,new Vector2(-1.5f,193.2f),180));
                var hallView=new GameObject("Hall sentries view");hallView.transform.position=OnFloor(kit,PumpOneHand);hallFight.previewPoint=hallView.transform;hallFight.previewRadius=5;

                // ---- the lower gallery: the terrace on its east side, dry once pump one has run, with pump two at its north end; the low floor west of it, dry once pump two has.
                kit.Sign("LOWER GALLERY",12.5f,208.4f,1.8f);
                Pump(kit,"Pump two",PumpTwo,200,PumpTwoHand,"wend.pump2","Pump",null,"Taren",null,0,null,new Bounds(new Vector3(-7,-4,228),new Vector3(30,4,44)),"pump");
                var terraceFight=Fight(kit,"Gallery_Terrace",new Vector2(15,229),new Vector2(14,40),"wend.pump1",("DrownedSentry",1,new Vector2(23.2f,226.5f),270),("RotPod",1,new Vector2(17.5f,237.5f),200),("RotPod",1,new Vector2(11.5f,219),160));
                var terraceView=new GameObject("Terrace view");terraceView.transform.position=OnFloor(kit,new Vector2(12,207));terraceFight.previewPoint=terraceView.transform;terraceFight.previewRadius=6;
                var floorFight=Fight(kit,"Gallery_Floor",new Vector2(-7,228),new Vector2(30,44),"wend.pump2",("DrownedSentry",1,new Vector2(-23.2f,209.5f),90),("DrownedSentry",1,new Vector2(-23.2f,237.5f),90),
                    ("MireLeeches",1,Sumps[0].at,0),("MireLeeches",1,Sumps[1].at,0));
                var floorView=new GameObject("Low floor view");floorView.transform.position=OnFloor(kit,PumpTwoHand);floorFight.previewPoint=floorView.transform;floorFight.previewRadius=6;

                // ---- Pim's pocket: up a stair off the low floor's west side, behind a grille Pim tied shut. Pim waits inside until found, then goes out to the portico (next time the
                // station is entered); the grille is open from then on and the basket inside can be had.
                // Pim leaves at the end of that talk, there and then: out through the grille, down the stair and across the low floor toward the way out, and gone at the ramp (LeavesAfter).
                // Until then the only line here is the one that finds them; nothing offers the portico's.
                var pim=kit.Person("Pim",new Vector3(PimAt.x,0,PimAt.y),90,"",false);pim.firstNode="PimFound";pim.repeatNode="PimFound";
                // Spoken to through the bars. They stand 3.9 m in front of Pim and a talk reaches 3.2, so with the grille shut nobody could start the talk that finds them,
                // and pump three and the great hall's door wait on it (found by the routes' press check before anyone played it: November 13).
                pim.range=PimHeardFrom;
                var leaves=pim.gameObject.AddComponent<LeavesAfter>();leaves.flag="wend.pimFound";
                leaves.path=new[]{new Vector2(-28.4f,219.8f),new Vector2(-21.4f,219.6f),new Vector2(-12,227),new Vector2(-2,239),new Vector2(5,244.6f)}.Select(q=>OnFloor(kit,q)).ToArray();
                var pimLamp=new GameObject("Pim's glow-spots",typeof(Light));pimLamp.transform.position=OnFloor(kit,PimAt,1.9f);pimLamp.transform.SetParent(pim.transform,true);
                var spots=pimLamp.GetComponent<Light>();spots.type=LightType.Point;spots.range=4.5f;spots.color=new Color(.8f,.72f,1);spots.shadows=LightShadows.None;
                var spotCount=pimLamp.AddComponent<RhythmLamp>();spotCount.count="LLS";spotCount.glow=spots;spotCount.intensity=2.2f;spotCount.colour=spots.color;
                var grille=GrilleDoor(kit,"Pim's grille",OnFloor(kit,new Vector2(Grille.x,218.6f)),OnFloor(kit,new Vector2(Grille.x,224.4f)),2.4f);
                var opens=new GameObject("Pim's grille opens",typeof(FlagGate)).GetComponent<FlagGate>();opens.flag="wend.pimFound";opens.showWhenSet=false;opens.targets=new[]{grille};
                Stash(kit,"wend.station.pim","Pim's weaving basket",PimCache,20,90,false,ProgressionBuilder.Drop("ReedWeave",3,1),ProgressionBuilder.Drop("RepairSalve",1,1),ProgressionBuilder.AnyPart(3,GearSlot.Frame,1));

                // ---- the vault, off the terrace's east side behind pump four: a passage, a sump that pump four drains, and the dry room beyond it with the wall of marks.
                kit.Sign("THE VAULT",40.5f,212.6f,1.6f);
                Pump(kit,"Pump four",PumpFour,200,PumpFourHand,"wend.pump4","FourthPump",null,"Taren","FourthPump",0,null,new Bounds(new Vector3(36,-3,218),new Vector3(14,4,16)),"");
                kit.Discovery("Wall of marks",new Vector3(VaultMark.x,0,VaultMark.y),"Take a rubbing of the marks","wend.vaultRead","VaultMark","SelaVaultMark","Sela","FourthPump",1,3);
                Stash(kit,"wend.station.vault","Old engineers' coffer",VaultChest,10,200,true,ProgressionBuilder.AnyPart(3,GearSlot.Module,1),ProgressionBuilder.Drop("ChargeCell",2,1),ProgressionBuilder.Drop("LureBulb",1,.5f));

                // ---- the landing at the great hall's door: the checkpoint. Pump three (once Pim is found) lifts the grille across the door; the talk inside it starts the fight.
                kit.Sign("GREAT HALL",5.4f,260.2f,1.8f);
                var doorBay=kit.Prop("DeckConsole",new Vector3(DoorCradle.x,0,DoorCradle.y),1.4f,-110).AddComponent<RepairBay>();doorBay.free=true;doorBay.saveSpawn="Door";doorBay.prompt="Station cradle — repair and save";
                ChapterOneStaging.CradleBay("Station cradle",doorBay.gameObject,OnFloor(kit,DoorCradle),kit.Y(DoorCradle.x,DoorCradle.y)+.02f,false,false);
                Pump(kit,"Pump three",PumpThree,200,PumpThreeHand,"wend.pump3","LastPump",null,"Taren",null,0,"wend.pimFound",new Bounds(new Vector3(0,-1,286),new Vector3(36,4,36)),"pump");
                var doorGrille=GrilleDoor(kit,"Great hall grille",OnFloor(kit,new Vector2(-4.4f,266.6f)),OnFloor(kit,new Vector2(4.4f,266.6f)),3.2f);
                var lifts=new GameObject("The great hall's grille lifts",typeof(FlagGate)).GetComponent<FlagGate>();lifts.flag="wend.pump3";lifts.showWhenSet=false;lifts.targets=new[]{doorGrille};
                var confront=kit.Crossing("The Rotbloom in its hall",new Vector3(0,0,265.6f),new Vector3(8.8f,5,2.2f),"wend.bloomSeen","SelaBloomDoor","Taren","wend.pump3");confront.saveSpawn="Door";
                // 8.8 wide: the width the Walking Canopy's and Hushwell's seals are, so the material the three share is left as it was.
                var seal=WorldBuilder.Membrane("Great hall seal",OnFloor(kit,new Vector2(0,267.6f),1.7f),8.8f,true,3.6f);
                foreach(var filter in seal.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(filter);
                foreach(var view in seal.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(view);
                var sealBox=seal.GetComponent<BoxCollider>();var sealCarve=seal.gameObject.AddComponent<NavMeshObstacle>();sealCarve.shape=NavMeshObstacleShape.Box;sealCarve.center=sealBox.center;sealCarve.size=sealBox.size;sealCarve.carving=true;sealCarve.carveOnlyStationary=false;sealCarve.enabled=false;
                // The seal is bronze bars dropped across behind the party as the fight starts, gone when it ends.
                var bars=new ZoneKit.Shape();for(int i=0;i<=22;i++){var foot=OnFloor(kit,new Vector2(-4.4f+i*.4f,267.6f),-.2f);bars.Slot(0).Post(foot,3.6f,.05f,.045f,6,StoneDark,BarkPale);}
                bars.Slot(0).Beam(OnFloor(kit,new Vector2(-4.4f,267.6f),3.3f),OnFloor(kit,new Vector2(4.4f,267.6f),3.3f),.14f,.16f,BarkDark,BarkPale);
                var sealBars=kit.Built("Great hall seal bars",bars,Bronze);sealBars.transform.SetParent(seal.transform,true);sealBars.GetComponent<Renderer>().enabled=false;
                var duel=WorldBuilder.Encounter("Rotbloom",kit.On(C.x,C.y,2),new Vector3(38,8,38),Enemy("Rotbloom"),1,new[]{seal});
                var post=duel.spawners[0];post.transform.SetPositionAndRotation(kit.On(C.x,C.y),Quaternion.Euler(0,180,0));post.faceAsPlaced=true;post.radius=0;
                duel.requiredFlag="wend.bloomSeen";duel.checkpointSpawn="Door";
                var door=new GameObject("Rotbloom preview (the door)");door.transform.position=OnFloor(kit,new Vector2(0,266));duel.previewPoint=door.transform;duel.previewRadius=26;confront.previewEncounter=duel;
                var dead=new GameObject("The hall after the Rotbloom",typeof(AfterTalk)).GetComponent<AfterTalk>();dead.flag="bossdown.Rotbloom";dead.node="SelaBloomDead";dead.speaker="Sela";dead.heardFlag="wend.rotbloomDead";dead.delay=3;
                var plinths=new GameObject("Plinths");
                foreach(float bearing in PlinthBearings)PlinthStone(kit,plinths,bearing);
                Stalks(kit);
                // Fixed lamps that keep the hall readable with every lure out: at the door and on the east rim between the plinths. With the four bulbs that is the contract's six.
                foreach(var (x,z,yaw) in new[]{(0f,270.4f,0f),(17.6f,289f,270f)})HallLamp(kit,new Vector2(x,z),yaw);
                if(!greybox)HallDress(kit);
                if(!greybox)Rafts(kit,hallSheet.transform,GreatFull,p=>Vector2.Distance(p,C)<ArenaRadius-1.5f&&Vector2.Distance(p,C)>5?GreatFull-ArenaAt(p):-1,"Great hall pads",-17,17,272,306,260,401);

                if(!greybox)Dress(kit,mire,mireSheet.transform);
                // For stills of the station as the pumps leave it (never saved as the zone: rebuild without these): -waystation-stage 1 or 2 stands its water where that stage puts it, and
                // -waystation-drained empties the vault's sump and the great hall too.
                int still=int.TryParse(DevArgs.Value("-waystation-stage"),out int s)?s:0;bool drained=DevArgs.Has("-waystation-drained");
                if(still>0)Stand(station,staged.LevelOf(still),holders.Take(still));
                if(drained){Stand(ante,AnteDrained,ante.deep);Stand(hall,GreatEmpty,Array.Empty<GameObject>());basin.gameObject.SetActive(false);}

                // ---- limits: the deep water and the station's walls are the stop; a box on each side keeps a body from finding a way up one.
                WorldBuilder.Boundary("Mire south limit",new Vector3(7,4,-17),new Vector3(144,20,2));WorldBuilder.Boundary("Station north limit",new Vector3(7,4,311),new Vector3(144,20,2));
                WorldBuilder.Boundary("West limit",new Vector3(-63,4,147),new Vector3(2,20,330));WorldBuilder.Boundary("East limit",new Vector3(77,4,147),new Vector3(2,20,330));

                // ---- navigation, with every stage drained, every grille up and the sump empty: what is shut at first is play's business.
                var shut=new List<GameObject>{grille,doorGrille};shut.AddRange(holders);var were=shut.Select(g=>g.activeSelf).ToArray();foreach(var g in shut)g.SetActive(false);
                var reach=new List<Vector2>{Arrival,Cradle+new Vector2(1.6f,0),new(-6,-.6f),MaruIsland+new Vector2(-1.6f,-1.6f),StriderPocket,new(16,42),new(20,52),LureLesson,RealLamp+new Vector2(-1.6f,-1.6f),
                    BellHand,BellCache+new Vector2(0,-1.4f),new(12,90),LeechMiddle,new(-4,100),DryCache+new Vector2(1.4f,0),new(-16,88),ThirdPocket,new(0,146),Bench+new Vector2(1.6f,0),
                    new(0,160),new(-4,164),new(2.5f,158),new(0,174),PumpOneHand,new(-4,186),new(-9,192.2f),new(12,196),new(12,207),PumpTwoHand,new(23,226.5f),new(15,240),new(4,245),
                    new(-7,228),new(-23,209.5f),new(-23,237.5f),new(-24,221.5f),new(-26.8f,221.5f),new(-32,224),PimCache+new Vector2(1.4f,0),PumpFourHand,new(33.3f,218),new(42,218),
                    new(VaultMark.x-1.6f,VaultMark.y),VaultChest+new Vector2(0,1.4f),new(0,255),DoorSpawn,DoorCradle+new Vector2(-1.6f,0),PumpThreeHand,new(0,266),C,new(0,301),new(12,289),new(0,277),new(-12,289)};
                foreach(float bearing in PlinthBearings)reach.Add(Plinth(bearing));
                reach.AddRange(new Vector2[]{new(-7.8f,36.3f),new(32.6f,82.2f),new(-14.6f,90),new(-24.8f,109.8f),new(53.4f,67)});
                WaterKit.WithOpenWater(()=>kit.BakeNavigation(new Bounds(new Vector3(7,0,147),new Vector3(142,30,330)),Arrival,reach));
                for(int i=0;i<shut.Count;i++)shut[i].SetActive(were[i]);
                if(!greybox)Ground(kit);
                WorldBuilder.Save(scene,Station);Debug.Log("DROWNED_WAYSTATION_OK");
            }

            // ---------------------------------------------------------------- people and things to find
            /// <summary>Maru in one of her two places here: there from one flag, gone at another, each followed as it changes (both happen out of sight of the place). Her lamp beside her,
            /// keeping Driftreed's count.</summary>
            static void Maru(ZoneKit kit,string name,Vector2 at,float yaw,string from,string gone,Vector2? lamp)
            {
                var maru=kit.Person("Maru",new Vector3(at.x,0,at.y),yaw,"",false);maru.repeatNode="MaruMireRepeat";maru.firstNode="MaruMireRepeat";
                var holder=new GameObject(name);holder.transform.position=maru.transform.position;maru.transform.SetParent(holder.transform,true);
                var hand=lamp??at+new Vector2(-1.4f,.6f);
                var pole=new ZoneKit.Shape();var foot=kit.On(hand.x,hand.y);
                pole.Slot(0).Tube(new[]{foot+Vector3.down*.3f,foot+Vector3.up*1.4f+Vector3.right*.05f,foot+Vector3.up*2.5f},new[]{.05f,.045f,.04f},6,BarkDark,BarkPale,1,.3f).Rope(foot+Vector3.up*2.45f,foot+new Vector3(.32f,2.25f,0),.05f,.015f,new Color(.84f,.76f,.5f));
                pole.Slot(1).Lump(foot+new Vector3(.34f,2.02f,0),new Vector3(.13f,.19f,.13f),(int)(at.x*3),.05f,Color.white,Color.white,8,6);
                var drawn=kit.Built(name+" lamp",pole,Bark,Lantern);drawn.transform.SetParent(holder.transform,true);
                var light=kit.Lamp(new Vector3(hand.x+.34f,2.05f,hand.y),7,2.2f,new Color(1,.8f,.5f));light.transform.SetParent(drawn.transform,true);
                var count=drawn.AddComponent<RhythmLamp>();count.count="SSL";count.glow=light;count.lamps=new Renderer[]{drawn.GetComponent<Renderer>()};count.intensity=2.2f;count.offset=at.y*.1f;
                // She is somewhere else before the tide and after the lamp is carried: decided as the flags change, since neither happens where she can be seen.
                var where=new GameObject(name+" (where she is)",typeof(Whereabouts)).GetComponent<Whereabouts>();where.from=from;where.goneAt=gone;where.targets=new[]{holder};where.live=true;
            }
            /// <summary>A still's water: the surface at a level, its depths written again, and the blockers it no longer needs switched off.</summary>
            static void Stand(WaterVolume water,float level,IEnumerable<GameObject> gone)
            {
                water.surface.position=new Vector3(0,level,0);foreach(var block in gone)block.SetActive(false);
                var mesh=UnityEngine.Object.Instantiate(water.surface.GetComponent<MeshFilter>().sharedMesh);var depth=new List<Vector2>();mesh.GetUVs(1,depth);
                for(int i=0;i<depth.Count;i++)depth[i]=new Vector2(depth[i].x,level-depth[i].x);mesh.SetUVs(1,depth);water.surface.GetComponent<MeshFilter>().sharedMesh=mesh;
                Debug.Log("WAYSTATION_STILL_WATER "+water.name+" "+level);
            }
            static void Placed(GameObject who,string name,string from,string gone,bool live)
            {var where=new GameObject(name+" (where)",typeof(Whereabouts)).GetComponent<Whereabouts>();where.from=from??"";where.goneAt=gone??"";where.targets=new[]{who};where.live=live;}
            /// <summary>A lure post: where a Lurelight hides between bites, ringed while one is in it. A broken lamp-post of Driftreed's, its lantern gone, so a lure standing on it reads as a lamp.</summary>
            static void Hide(ZoneKit kit,float x,float z)
            {
                var spot=new GameObject("Lure post "+x+" "+z,typeof(AmbushSpot));spot.transform.position=kit.On(x,z);spot.GetComponent<AmbushSpot>().radius=3.2f;
                var stump=new ZoneKit.Shape();var foot=kit.On(x,z);
                stump.Slot(0).Tube(new[]{foot+Vector3.down*.3f,foot+Vector3.up*.7f,foot+Vector3.up*1.3f+Vector3.right*.06f},new[]{.13f,.11f,.09f},6,BarkDark,BarkPale,1,.3f);
                for(int k=0;k<3;k++)stump.Slot(1).Rope(foot+Vector3.up*(.4f+.25f*k)+Vector3.forward*.12f,foot+Vector3.up*(.5f+.25f*k)+Vector3.back*.12f,-.04f,.02f,new Color(.82f,.74f,.5f));
                Reeds(stump,0,foot,1.2f,10,(int)(x*7+z*3),1.1f);
                kit.Built("Lure post "+Mathf.RoundToInt(x)+" "+Mathf.RoundToInt(z),stump,Reed,Weave);
            }
            /// <summary>The school raft's bell-buoy: a drum float with a bell frame on it, heeled where a mangrove root has it by the line. The buoy is the thing the prompt is for.</summary>
            static void BellBuoyBody(ZoneKit kit,DiscoveryPoint point)
            {
                var shape=new ZoneKit.Shape();var at=new Vector3(BellBuoy.x,MireWater-.1f,BellBuoy.y);var heel=Quaternion.Euler(12,30,-8);
                shape.Slot(0).Lump(at,new Vector3(.62f,.42f,.62f),17,.04f,new Color(.9f,.76f,.6f),new Color(.6f,.5f,.4f),10,6);
                foreach(int side in new[]{-1,1})shape.Slot(1).Tube(new[]{at+heel*new Vector3(side*.36f,.3f,0),at+heel*new Vector3(side*.2f,1.25f,0)},new[]{.04f,.035f},5,BarkDark,BarkPale);
                shape.Slot(1).Beam(at+heel*new Vector3(-.24f,1.22f,0),at+heel*new Vector3(.24f,1.22f,0),.06f,.06f,BarkPale);
                shape.Slot(1).Lump(at+heel*new Vector3(0,.98f,0),new Vector3(.17f,.2f,.17f),5,.02f,BarkPale,BarkDark,8,5);
                shape.Slot(2).Rope(at+heel*new Vector3(.2f,.2f,0),at+new Vector3(1.6f,.2f,1.2f),.2f,.025f,new Color(.84f,.76f,.5f));
                var body=kit.Built("Bell-buoy",shape.Slot(2),Weave,Bronze,Reed);body.transform.SetParent(point.transform,true);
            }
            /// <summary>A Glowspore cap cluster on a wet bank: gone once picked, like a mined crystal.</summary>
            static void Harvest(ZoneKit kit,string id,Vector2 at,int seed)
            {
                var shape=new ZoneKit.Shape();var rng=new System.Random(seed);
                for(int k=0;k<6;k++)
                {
                    var c=new Vector3(((float)rng.NextDouble()-.5f)*.8f,0,((float)rng.NextDouble()-.5f)*.8f);float tall=.12f+.2f*(float)rng.NextDouble(),r=.1f+.08f*(float)rng.NextDouble();
                    shape.Slot(0).Post(c,tall,.03f,.025f,5,new Color(.9f,.88f,.78f),Color.white);
                    shape.Slot(1).Lump(c+Vector3.up*tall,new Vector3(r,r*.5f,r),seed+k,.1f,new Color(.7f,1.3f,1.1f),new Color(.4f,.9f,.8f),8,5);
                }
                var body=kit.Built("Glowspore "+id,shape.Slot(1),Bark,Glow);body.transform.position=kit.On(at.x,at.y);
                var solid=body.AddComponent<BoxCollider>();solid.center=Vector3.up*.2f;solid.size=new Vector3(1,.4f,1);body.isStatic=true;
                var node=body.AddComponent<MineNode>();node.nodeId=id;node.material="Glowspore";node.amount=2;node.prompt="Gather Glowspore";node.range=2.6f;
                var glow=kit.Lamp(new Vector3(at.x,.6f,at.y),3.5f,.9f,new Color(.5f,1,.85f));glow.name="Glowspore light";glow.transform.SetParent(body.transform,true);
            }
            /// <summary>A plinth of the great hall: a drum of carved stone with a bronze collar, its ramp toward C (the height function's), and a lip of fallen masonry round its outer side.</summary>
            static void PlinthStone(ZoneKit kit,GameObject parent,float bearing)
            {
                var at=Plinth(bearing);var shape=new ZoneKit.Shape();var toC=(C-at).normalized;float ramp=Mathf.Atan2(toC.x,toC.y)*Mathf.Rad2Deg;
                // The drum's face round all but the ramp: carved courses, the collar at the top.
                for(int k=0;k<24;k++)
                {
                    float a0=k*15f,a1=a0+15;var d0=new Vector2(Mathf.Sin(a0*Mathf.Deg2Rad),Mathf.Cos(a0*Mathf.Deg2Rad));var d1=new Vector2(Mathf.Sin(a1*Mathf.Deg2Rad),Mathf.Cos(a1*Mathf.Deg2Rad));
                    if(Vector2.Dot((d0+d1).normalized,toC)>.62f)continue;
                    Vector3 p0=new(at.x+d0.x*(PlinthRadius+.05f),ArenaFloor-.3f,at.y+d0.y*(PlinthRadius+.05f)),p1=new(at.x+d1.x*(PlinthRadius+.05f),ArenaFloor-.3f,at.y+d1.y*(PlinthRadius+.05f));
                    var outward=new Vector3(d0.x+d1.x,0,d0.y+d1.y);
                    shape.Slot(0).Quad(p0,p1,p1+Vector3.up*(PlinthTop-ArenaFloor+.28f),p0+Vector3.up*(PlinthTop-ArenaFloor+.28f),outward,StoneWarm,.55f);
                    shape.Slot(1).Quad(p0+Vector3.up*(PlinthTop-ArenaFloor+.18f)+outward.normalized*.06f,p1+Vector3.up*(PlinthTop-ArenaFloor+.18f)+outward.normalized*.06f,
                        p1+Vector3.up*(PlinthTop-ArenaFloor+.34f)+outward.normalized*.06f,p0+Vector3.up*(PlinthTop-ArenaFloor+.34f)+outward.normalized*.06f,outward,BarkPale,.8f);
                }
                // Steps laid on the ramp, so it reads as built and not as a mound.
                for(int s=1;s<=5;s++){float back=PlinthRadius+s*.48f;var c=at+toC*back;float y=PlinthTop-.32f*(back-PlinthRadius);shape.Slot(0).Box(new Vector3(c.x,y-.06f,c.y),new Vector3(2.6f,.12f,.46f),Quaternion.Euler(0,ramp,0),StoneDark,StoneWarm);}
                var built=kit.Built("Plinth "+bearing,shape,Masonry,Bronze);built.transform.SetParent(parent.transform,true);
            }
            /// <summary>The four lure-stalks, as the contract has them: on each plinth's top, an object with a Health (id "Lure-stalk", its integrity the boss's to set), a trigger sphere 1.3 m
            /// across its bulb's height and a hurtbox, and a LureStalk counting NE, NW, SW, SE as 0 to 3; the kit's stalk and its flickering light as <c>whole</c>, a scorched stump as <c>burned</c>.</summary>
            static void Stalks(ZoneKit kit)
            {
                var prefab=ZoneKit.Prefab("Enemies","LureStalk");
                foreach(var (bearing,index,name) in new[]{(45f,0,"ne"),(315f,1,"nw"),(225f,2,"sw"),(135f,3,"se")})
                {
                    var at=Plinth(bearing);var foot=new Vector3(at.x,PlinthTop,at.y);var toC=(C-at).normalized;
                    var stalk=new GameObject("Lure-stalk "+name.ToUpperInvariant());stalk.SetActive(false);stalk.transform.SetPositionAndRotation(foot,Quaternion.LookRotation(new Vector3(toC.x,0,toC.y)));
                    var whole=new GameObject("Lure-stalk standing");whole.transform.SetParent(stalk.transform,false);
                    if(prefab!=null){var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.transform.SetParent(whole.transform,false);}
                    else{var stem=new ZoneKit.Shape();stem.Slot(0).Tube(new[]{Vector3.zero,Vector3.up*2,Vector3.up*3.6f+Vector3.forward*.6f},new[]{.25f,.16f,.1f},6,BarkDark,BarkPale).Slot(1).Lump(Vector3.up*3.3f+Vector3.forward*.9f,new Vector3(.3f,.42f,.3f),bearing.GetHashCode(),.05f,Color.white,Color.white);
                        var drawn=kit.Built("Lure-stalk stand-in "+name,stem,Bark,Lantern);drawn.transform.SetParent(whole.transform,false);}
                    var bulb=new GameObject("Lure bulb light",typeof(Light));bulb.transform.SetParent(whole.transform,false);bulb.transform.localPosition=new Vector3(0,3.4f,.5f);
                    var light=bulb.GetComponent<Light>();light.type=LightType.Point;light.range=9;light.intensity=2.6f;light.color=new Color(.86f,1,.56f);light.shadows=LightShadows.None;
                    // What a burned stalk leaves: a charred stump and a ring of ash on the plinth.
                    var stump=new ZoneKit.Shape();
                    stump.Slot(0).Tube(new[]{Vector3.down*.1f,Vector3.up*.6f,Vector3.up*1.05f+Vector3.forward*.12f},new[]{.32f,.22f,.12f},7,new Color(.16f,.14f,.13f),new Color(.32f,.24f,.2f),1,.4f);
                    for(int k=0;k<5;k++){var d=Quaternion.Euler(0,k*72+20,0)*Vector3.forward;stump.Slot(0).Tube(new[]{d*.15f+Vector3.up*.15f,d*.7f,d*1.1f+Vector3.down*.05f},new[]{.12f,.08f,.04f},5,new Color(.18f,.15f,.14f),new Color(.3f,.24f,.2f),1,.4f);}
                    stump.Slot(0).Disc(Vector3.up*.02f,Vector3.up,1.3f,14,new Color(.26f,.24f,.22f),0,new Color(.4f,.38f,.34f));
                    var burned=kit.Built("Lure-stalk burned "+name,stump,Bark);burned.transform.SetParent(stalk.transform,false);burned.transform.localPosition=Vector3.zero;burned.transform.localRotation=Quaternion.identity;
                    var health=stalk.AddComponent<Health>();health.id="Lure-stalk";
                    var shape=stalk.AddComponent<SphereCollider>();shape.isTrigger=true;shape.radius=1.3f;shape.center=Vector3.up*2.2f;stalk.AddComponent<Hurtbox>().owner=health;
                    var lure=stalk.AddComponent<LureStalk>();lure.whole=whole;lure.burned=burned;lure.index=index;lure.settledBy="Rotbloom";lure.defeatedFlag="wend.stalk."+name;
                    burned.SetActive(false);stalk.SetActive(true);
                }
            }
            /// <summary>A lamp of the old engineers': a carved stone foot, a bronze post with a collar, an arm reaching into the room with a caged globe of their amber glass on it, still
            /// faintly lit; and the light it gives. <paramref name="yaw"/> is the way the arm reaches.</summary>
            static void HallLamp(ZoneKit kit,Vector2 at,float yaw)
            {
                var shape=new ZoneKit.Shape();var foot=kit.On(at.x,at.y);var arm=Quaternion.Euler(0,yaw,0)*Vector3.forward;var side=Vector3.Cross(Vector3.up,arm);
                shape.Slot(0).Post(foot+Vector3.down*.1f,.5f,.34f,.27f,10,StoneDark,StoneWarm).Post(foot+Vector3.up*.4f,.12f,.3f,.3f,10,StoneWarm,StoneWarm);
                shape.Slot(1).Post(foot+Vector3.up*.5f,2.05f,.075f,.06f,8,StoneDark,BarkPale).Post(foot+Vector3.up*1.6f,.14f,.11f,.11f,8,BarkPale,BarkPale)
                    .Tube(new[]{foot+Vector3.up*2.5f,foot+Vector3.up*2.78f+arm*.2f,foot+Vector3.up*2.72f+arm*.58f},new[]{.05f,.05f,.045f},6,BarkPale,BarkPale);
                var globe=foot+Vector3.up*2.36f+arm*.62f;
                for(int k=0;k<4;k++){var r=Quaternion.AngleAxis(k*90+45,Vector3.up)*arm*.21f;shape.Slot(1).Tube(new[]{globe+Vector3.up*.3f,globe+r+Vector3.up*.05f,globe+r*.8f+Vector3.down*.24f},new[]{.022f,.022f,.02f},4,BarkPale,BarkPale,1,1,false);}
                shape.Slot(1).Tube(new[]{globe+Vector3.up*.36f,globe+Vector3.up*.3f},new[]{.06f,.08f},6,BarkPale,BarkPale);
                shape.Slot(2).Lump(globe,new Vector3(.16f,.21f,.16f),(int)(at.x*7+at.y),.04f,Color.white,new Color(.9f,.8f,.7f),10,6);
                var lamp=kit.Built("Old engineers' lamp "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),shape,Masonry,Bronze,Lantern);
                ZoneKit.Block(lamp,foot+Vector3.up*1.1f,new Vector3(.6f,2.2f,.6f),0,"Lamp post");
                kit.Lamp(new Vector3(globe.x,2.3f,globe.z),12,2.4f,new Color(1,.76f,.46f));
            }
            /// <summary>The great hall's own things, all outside r 12.5 and off the plinths and the rot-pods' places (the contract): a column fallen across the east side half under the
            /// water, the roots of the mire coming in over the north and east rims and running out along the floor, and a broken ring of the old roof lying in the water to the west.</summary>
            static void HallDress(ZoneKit kit)
            {
                var col=Lying(kit,ZoneKit.Kit("AncelaneColumn","HushwellColumn"),new Vector3(0,ArenaFloor,0),4.6f,0);
                var at=C+new Vector2(Mathf.Sin(108*Mathf.Deg2Rad),Mathf.Cos(108*Mathf.Deg2Rad))*15.4f;col.transform.rotation=Quaternion.Euler(0,18,87);
                var box=ModelGeometry.BoundsOf(col);col.transform.position+=new Vector3(at.x-box.center.x,ArenaFloor-.35f-box.min.y,at.y-box.center.z);col.name="Fallen column";
                // Each root and the rib their own piece: drawn as one, their box was 37 by 23 m over the hall and the camera checks took it for a wall across the fight.
                foreach(float bearing in new[]{-28f,-8f,12f,62f,78f,100f})
                {
                    var roots=new ZoneKit.Shape();
                    var rim=C+new Vector2(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad))*(ArenaRadius+1.6f);var inward=(C-rim).normalized;var side=new Vector2(-inward.y,inward.x);
                    var path=new List<Vector3>();var radii=new List<float>();
                    for(int i=0;i<=8;i++)
                    {
                        float t=i/8f;var q=rim+inward*(5.2f*t)+side*Mathf.Sin(t*4+bearing)*.6f;float y=i<2?Height(q)-.3f*i:ArenaAt(q)-.08f;
                        if(i>=2&&Vector2.Distance(q,C)<13.2f)break;path.Add(new Vector3(q.x,y,q.y));radii.Add(Mathf.Lerp(.32f,.1f,t));
                    }
                    if(path.Count>=3){roots.Slot(0).Tube(path.ToArray(),radii.ToArray(),6,BarkDark,BarkPale,1,.3f);kit.Built("Great hall root "+Mathf.RoundToInt(bearing),roots,Bark);}
                }
                var ring=new Vector3[13];var thick=new float[13];var ringAt=C+new Vector2(Mathf.Sin(258*Mathf.Deg2Rad),Mathf.Cos(258*Mathf.Deg2Rad))*15.2f;
                for(int k=0;k<13;k++){float a=k/12f*Mathf.PI*1.15f;ring[k]=new Vector3(ringAt.x+Mathf.Cos(a)*2.2f,ArenaFloor+Mathf.Sin(a)*.5f-.05f,ringAt.y+Mathf.Sin(a)*1.1f);thick[k]=.2f;}
                var rib=new ZoneKit.Shape();rib.Slot(0).Tube(ring,thick,7,StoneDark,BarkPale,1,.4f);
                kit.Built("Great hall fallen rib",rib,Bronze);
            }
            static void Rafts(ZoneKit kit,Transform surface,float level,Func<Vector2,float> depth,string name,float xMin,float xMax,float zMin,float zMax,int rafts,int seed)
            {
                var shape=new ZoneKit.Shape();var rng=new System.Random(seed);
                for(int i=0;i<rafts;i++){var c=new Vector2(Mathf.Lerp(xMin,xMax,(float)rng.NextDouble()),Mathf.Lerp(zMin,zMax,(float)rng.NextDouble()));Pads(shape,rng,c,level,6+rng.Next(9),.36f+.12f*(float)rng.NextDouble(),depth,.15f);}
                if(shape.Vertices==0)return;var lying=kit.Built(name,shape.Slot(1),Pad,Lantern);lying.transform.SetParent(surface,true);
            }

            // ---------------------------------------------------------------- dressing
            /// <summary>The walked lines, island by island and room by room: the ground kit keeps them clear, the scatter keeps its tall things off them.</summary>
            internal static readonly Vector2[][] Ways=
            {
                new Vector2[]{new(-4,2.5f),new(2,16),new(4,30),new(14,38),new(20,52),new(27,62),new(28,72),new(22,82),new(10,92),new(-3,101),new(-4,118),new(1,136),new(0,154),new(0,166)},
                new Vector2[]{new(15,79),new(-4,82),new(-16,88),new(-22,98),new(-14,104)},new Vector2[]{new(36,64),new(46,64.5f),new(55,69)},
                new Vector2[]{new(0,168),new(4,174),new(11,180),new(12,190),new(12,206),new(14,226),new(16,244),new(6,245),new(-2,245),new(-12,230),new(-22,221.5f),new(-27,221.5f),new(-32,223)},
                new Vector2[]{new(14,217.5f),new(28,217.5f),new(33.3f,218),new(42,218),new(46,214)},new Vector2[]{new(-2,245),new(0,252),new(0,262),new(0,272),new(0,289)},
                new Vector2[]{new(-4,186),new(-6,190)},
            };
            static bool NearWay(Vector2 p,float within){foreach(var way in Ways)if(ZoneKit.FromPath(p,way)<within)return true;return false;}
            /// <summary>Nothing this tall at a plan point may stand where a walked floor lies within the fixed camera's shadow of it (it would stand between the camera and a hero): checked down
            /// the camera's line and a little either side.</summary>
            static bool ShadowsAFloor(Vector2 p,float tall)
            {
                float reach=ZoneKit.Shadowed(tall)+1.5f;var side=new Vector2(Away.y,-Away.x);
                for(float d=0;d<=reach;d+=.75f)foreach(float s in new[]{-2f,0,2f})if(Walkable(p+Away*d+side*s))return true;
                return false;
            }
            static void Dress(ZoneKit kit,WaterVolume mire,Transform mireSurface)
            {
                var rng=new System.Random(53);float R()=>(float)rng.NextDouble();
                // ---- mangroves in the deep water, wherever their crowns can't come between the camera and anywhere walked. Each region its own piece.
                int trees=0;var groves=new Dictionary<int,ZoneKit.Shape>();
                for(float x=-60;x<76;x+=4.2f)for(float z=-14;z<164;z+=4.2f)
                {
                    var p=new Vector2(x+R()*3,z+R()*3);float tall=6.5f+R()*5,girth=.32f+R()*.22f;bool keep=R()<.55f;
                    if(!keep||MireHeight(p)>-.75f||NearWay(p,6)||ShadowsAFloor(p,tall))continue;
                    bool near=false;for(float a=0;a<6.28f&&!near;a+=.8f)near=Walkable(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*3.2f);if(near)continue;
                    int region=Mathf.FloorToInt(z/45);if(!groves.TryGetValue(region,out var grove))groves[region]=grove=new ZoneKit.Shape();
                    Mangrove(grove,new Vector3(p.x,MireWater,p.y),tall,girth,(int)(x*31+z*7));trees++;
                }
                foreach(var (region,grove) in groves)kit.Built("Mangroves "+region,grove.Slot(1),Bark,Leaf);
                Debug.Log("WAYSTATION_MANGROVES "+trees);
                // ---- shrubs of mangrove at the islands' edges: knee-high roots and a little leaf, where the camera's line stays clear of them.
                var shrubs=new ZoneKit.Shape();int shrubCount=0;
                for(float x=-40;x<66;x+=3.1f)for(float z=-8;z<160;z+=3.1f)
                {
                    var p=new Vector2(x+R()*2,z+R()*2);float h=MireHeight(p);float tall=2.2f+R()*1.6f;
                    if(h>-.2f||h<-.9f||NearWay(p,4.5f)||ShadowsAFloor(p,tall)||R()>.5f)continue;
                    Mangrove(shrubs,new Vector3(p.x,MireWater,p.y),tall,.12f+R()*.06f,(int)(x*17+z),true);shrubCount++;
                }
                // None passes the camera-line rule at the islands' edges as they stand, and an empty mesh is drawn with no uvs over the floor (QaChapterTwoLookTests): build it only with shrubs in it.
                if(shrubCount>0)kit.Built("Mangrove shrubs",shrubs.Slot(1),Bark,Leaf);Debug.Log("WAYSTATION_SHRUBS "+shrubCount);
                // ---- reeds in the shallows round every island: what a shore is made of here. Kept off the fords and the walked lines.
                var reeds=new ZoneKit.Shape();int clumps=0;
                for(float x=-40;x<66;x+=1.8f)for(float z=-10;z<162;z+=1.8f)
                {
                    var p=new Vector2(x+R()*1.6f,z+R()*1.6f);float h=MireHeight(p);
                    if(h>MireWater+.05f||h<MireWater-.6f||NearWay(p,2.4f)||R()>.42f)continue;
                    bool ford=false;foreach(var (a,b,half,_) in Fords)if(ZoneKit.Segment(p,a,b)<half+.6f)ford=true;if(ford)continue;
                    Reeds(reeds,0,new Vector3(p.x,h,p.y),.7f+R()*.6f,7+rng.Next(8),(int)(x*13+z*5),1.1f+R()*.8f);clumps++;
                }
                kit.Built("Shore reeds",reeds,Reed);
                Debug.Log("WAYSTATION_REEDS "+clumps);
                // ---- pads on the open water, riding it; scum and bubbles on the leech channel, so it reads as water something lives in.
                var pads=new ZoneKit.Shape();int padRafts=0;
                for(float x=-50;x<70;x+=5)for(float z=-12;z<160;z+=5)
                {
                    var c=new Vector2(x+R()*5,z+R()*5);if(R()>.5f||NearWay(c,3.2f))continue;
                    padRafts+=Pads(pads,rng,c,MireWater,4+rng.Next(7),.32f+R()*.18f,p=>MireWater-MireHeight(p),.3f,2.2f)>0?1:0;
                }
                if(pads.Vertices>0){var lying=kit.Built("Mire pads",pads.Slot(1),Pad,Lantern);lying.transform.SetParent(mireSurface,true);}
                // Scum: drifts of small dark flecks, thick along the channel's middle and thinning out, the way weed gathers on still water with something stirring under it.
                var scum=new ZoneKit.Shape();
                for(int k=0;k<420;k++)
                {
                    var along=Vector2.Lerp(Fords[1].a,Fords[1].b,.1f+.8f*R());var c=along+new Vector2(R()-.5f,R()-.5f)*(2+R()*6);float d=MireWater-MireHeight(c);if(d<.08f)continue;
                    float r=.08f+R()*R()*.3f;int sides=5+rng.Next(3);var tone=Color.Lerp(new Color(.1f,.12f,.05f),new Color(.2f,.2f,.09f),R());
                    scum.Slot(0).Disc(new Vector3(c.x,MireWater+.012f+.0004f*(k%40),c.y),Vector3.up,r,sides,tone,0,ZoneKit.Shape.Dim(tone,.8f));
                }
                var scummed=kit.Built("Leech channel scum",scum,Pad);scummed.transform.SetParent(mireSurface,true);
                var murk=kit.Glow("Murk",false,new Color(.6f,.66f,.4f,1));
                var bubbles=kit.Drift("Leech channel bubbles",new Vector3(10.5f,MireWater+.1f,93),new Vector3(20,.15f,14),murk,new Color(.7f,.76f,.5f,.5f),220,.04f,.1f,.05f,3);
                // ---- snags: trunks the mire took, lying half under where the water is deep, and stumps standing out of it.
                var snags=new ZoneKit.Shape();
                for(int k=0;k<60;k++)
                {
                    var p=new Vector2(-50+R()*120,-10+R()*170);if(MireHeight(p)>-.6f||NearWay(p,5)||ShadowsAFloor(p,1.5f))continue;
                    var along=Quaternion.Euler(0,R()*360,0)*Vector3.forward;float length=3+R()*4,girth=.2f+R()*.18f;var at=new Vector3(p.x,MireWater,p.y);
                    if(R()<.4f){snags.Slot(0).Tube(new[]{at+Vector3.down*.6f,at+Vector3.up*.6f,at+Vector3.up*(1.1f+R())},new[]{girth*1.4f,girth,girth*.6f},6,BarkDark,BarkPale,1,.3f);continue;}
                    snags.Slot(0).Tube(new[]{at-along*length*.5f+Vector3.down*.3f,at+Vector3.up*.08f,at+along*length*.5f+Vector3.up*.3f},new[]{girth*1.2f,girth,girth*.6f},7,BarkDark,BarkPale,1,.25f);
                }
                kit.Built("Snags",snags,Bark);
                // ---- the Ilu's fish traps: woven cones staked in the shallows by the first two islands. People come out here, or did.
                var traps=new ZoneKit.Shape();
                foreach(var (x,z,yaw) in new[]{(-17f,10f,250f),(-15.5f,13.5f,270f),(17.5f,4f,110f),(44f,72f,80f),(26f,90f,10f)})
                {
                    var at=new Vector3(x,MireWater-.15f,z);var dir=Quaternion.Euler(0,yaw,0)*Vector3.forward;
                    traps.Slot(0).Tube(new[]{at,at+dir*.7f,at+dir*1.5f},new[]{.42f,.34f,.08f},9,new Color(.9f,.82f,.62f),new Color(.78f,.7f,.5f),3,1.2f,false);
                    foreach(int side in new[]{-1,1})traps.Slot(1).Post(at+Vector3.Cross(Vector3.up,dir)*side*.55f+Vector3.down*.6f,1.6f,.04f,.035f,5,BarkDark,BarkPale);
                }
                kit.Built("Fish traps",traps,Weave,Bark);
                // ---- the station's stones out in the mire: blocks fallen from the portico along the causeway, a column down on the third island, bronze bollards that marked the old road.
                var stones=new ZoneKit.Shape();
                foreach(var (x,z,size,yaw,sink) in new[]{(-6.4f,141f,1.1f,20f,.3f),(5.2f,143.6f,.9f,-35f,.4f),(-5.8f,152f,1.3f,60f,.2f),(6.4f,155f,1f,10f,.25f),(-15f,122f,1.2f,30f,.2f),(-17f,124.5f,.8f,75f,.1f),
                    (-13f,128f,.9f,-20f,.2f),(-20f,114.5f,1.1f,45f,.25f),(10.5f,160f,1.2f,15f,.2f),(-12.5f,161.5f,1f,-30f,.15f)})
                {
                    var foot=new Vector3(x,Mathf.Max(MireHeight(new Vector2(x,z)),MireWater-.3f)-sink,z);var turn=Quaternion.Euler(R()*14-7,yaw,R()*14-7);
                    stones.Slot(0).Box(foot+Vector3.up*size*.4f,new Vector3(size*1.5f,size*.8f,size),turn,StoneDark,StoneWarm,.55f);
                }
                for(float z=142;z<=153;z+=2.75f)foreach(int side in new[]{-1,1})
                {
                    var foot=new Vector3(side*3.3f+(z-142)*-.07f,MireWater-.4f,z);if(R()<.25f)continue;
                    stones.Slot(1).Post(foot,.8f+R()*.3f,.17f,.15f,8,StoneDark,BarkPale).Lump(foot+Vector3.up*(1f+R()*.1f),new Vector3(.2f,.12f,.2f),(int)z,.05f,BarkPale,StoneDark,8,4);
                }
                var fallen=kit.Built("Fallen stones",stones,Masonry,Bronze);
                var toppled=Lying(kit,ZoneKit.Kit("AncelaneColumn","HushwellColumn"),new Vector3(-14.5f,-.1f,125.5f),4.4f,62);toppled.transform.rotation=Quaternion.Euler(0,62,86);
                var sunk=ModelGeometry.BoundsOf(toppled);toppled.transform.position+=new Vector3(-14.5f-sunk.center.x,kit.Y(-14.5f,125.5f)-.25f-sunk.min.y,125.5f-sunk.center.z);
                // ---- the portico: columns either side of the arch, one fallen across the shore's east end.
                foreach(float x in new[]{-6.6f,6.6f})kit.Prop(ZoneKit.Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,0,168.4f),4.6f,x*3);
                var down=Lying(kit,ZoneKit.Kit("AncelaneColumn","HushwellColumn"),new Vector3(15,0,162),4.4f,0);down.transform.rotation=Quaternion.Euler(0,-24,88);
                var downBox=ModelGeometry.BoundsOf(down);down.transform.position+=new Vector3(15-downBox.center.x,kit.Y(15,162)-.3f-downBox.min.y,162-downBox.center.z);
                // ---- fireflies over the water, and a low mist the light catches.
                var motes=kit.Glow("Motes",false,new Color(1.2f,1.4f,.8f,1));
                kit.Drift("Fireflies",new Vector3(7,1.4f,74),new Vector3(130,2.4f,180),motes,new Color(.9f,1,.6f,.7f),900,.05f,.11f,.35f,10);
                kit.Drift("Spores",new Vector3(0,1,236),new Vector3(80,4,150),motes,new Color(.7f,1,.86f,.4f),700,.04f,.1f,.12f,14);
                var mist=kit.Glow("Mist",true,new Color(.8f,.78f,.92f,1));
                kit.Drift("Mist on the water",new Vector3(7,.3f,74),new Vector3(130,.4f,180),mist,new Color(.74f,.72f,.86f,.08f),260,3.5f,7,.08f,24);
                IslandDress(kit);
                StationDress(kit);
            }
            /// <summary>The mire's fights, as rectangles: tall growth keeps out of them, so nothing stands over a marking or between a hero and the answer to a strider.</summary>
            static readonly (Vector2 at,Vector2 size)[] MireFights={(new(2,32),new(30,18)),(new(27,72.5f),new(32,22)),(new(10.5f,93),new(24,16)),(new(-4,120),new(26,24))};
            static bool InMireFight(Vector2 p,float inset=0){foreach(var (at,size) in MireFights)if(Mathf.Abs(p.x-at.x)<size.x*.5f-inset&&Mathf.Abs(p.y-at.y)<size.y*.5f-inset)return true;return false;}
            /// <summary>What says people come out here: sedge in tussocks, peat cut and stacked to dry, stumps of the mangroves that were cut, Driftreed's way-poles with their knotted
            /// cords along the route, and a drying rack by the landing.</summary>
            static void IslandDress(ZoneKit kit)
            {
                var rng=new System.Random(91);float R()=>(float)rng.NextDouble();
                // Sedge: tussocks of grass a hand over knee-high, thick on the islands' rims and thinning inland; never in a fight or on the walked line.
                var sedge=new ZoneKit.Shape();int tufts=0;
                for(float x=-34;x<62;x+=1.4f)for(float z=-8;z<160;z+=1.4f)
                {
                    var p=new Vector2(x+R()*1.2f,z+R()*1.2f);float shore=MireShore(p),h=MireHeight(p);bool keep=R()<Mathf.Lerp(.6f,.3f,ZoneKit.Smooth(-1,-6,shore));
                    if(!keep||h<MireWater+.12f||NearWay(p,2)||InMireFight(p,-1.5f))continue;
                    Sedge(sedge,new Vector3(p.x,h,p.y),.3f+R()*.18f,16+rng.Next(10),(int)(x*29+z*13),.42f+R()*.16f);tufts++;
                }
                kit.Built("Sedge",sedge,Reed);
                Debug.Log("WAYSTATION_SEDGE "+tufts);
                // Peat cut and stacked to dry, by the landing and on the third island: the Ilu burn it in the smokehouse.
                var peat=new ZoneKit.Shape();
                foreach(var (x,z,yaw) in new[]{(-15.5f,1.5f,20f),(-14.2f,-1.8f,35f),(-26.5f,116.5f,-10f),(-18.5f,101.5f,60f),(-8f,86.6f,-25f)})
                {
                    var foot=kit.On(x,z);var turn=Quaternion.Euler(0,yaw,0);
                    for(int layer=0;layer<3;layer++)for(int k=0;k<4-layer;k++)
                    {
                        var c=foot+turn*new Vector3((k-(3-layer)*.5f)*.42f+(layer%2)*.1f,.13f+layer*.24f,(R()-.5f)*.1f);
                        peat.Slot(0).Box(c,new Vector3(.38f,.22f,.72f),turn*Quaternion.Euler(0,R()*10-5,0),new Color(.3f,.24f,.18f),new Color(.42f,.34f,.24f),.6f);
                    }
                    peat.Slot(1).Beam(foot+turn*new Vector3(-.95f,.05f,.5f),foot+turn*new Vector3(.95f,.05f,.5f),.1f,.08f,BarkDark,BarkPale);
                }
                kit.Built("Peat stacks",peat,PeatFloor,Bark);
                // Stumps of mangrove the Ilu cut for poles, each on its flare of roots.
                var stumps=new ZoneKit.Shape();
                for(int k=0;k<40;k++)
                {
                    var p=new Vector2(-30+R()*88,-6+R()*160);float h=MireHeight(p);if(h<MireWater+.15f||NearWay(p,3)||InMireFight(p,-1))continue;
                    var foot=new Vector3(p.x,h,p.y);float girth=.18f+R()*.16f,tall=.35f+R()*.5f;
                    stumps.Slot(0).Tube(new[]{foot+Vector3.down*.15f,foot+Vector3.up*tall*.6f,foot+Vector3.up*tall},new[]{girth*1.3f,girth,girth*.95f},7,BarkDark,BarkPale,1,.4f);
                    stumps.Slot(0).Disc(foot+Vector3.up*(tall+.01f),Vector3.up,girth*.95f,7,new Color(.92f,.82f,.66f),0,new Color(.6f,.5f,.4f));
                    for(int r=0;r<4;r++){var d=Quaternion.Euler(0,r*90+R()*40,0)*Vector3.forward;stumps.Slot(0).Tube(new[]{foot+Vector3.up*tall*.3f+d*girth*.6f,foot+d*girth*2.4f+Vector3.down*.05f},new[]{girth*.35f,girth*.15f},5,BarkDark,BarkPale);}
                }
                kit.Built("Stumps",stumps,Bark);
                // Way-poles: a pole every twenty metres or so beside the walked line, its knotted cord and a strip of dyed cloth, so the route through the mire reads as kept.
                var poles=new ZoneKit.Shape();var way=Ways[0];float walked=0;
                for(int i=1;i<way.Length;i++)
                {
                    var a=way[i-1];var b=way[i];float leg=Vector2.Distance(a,b);var dir=(b-a)/leg;var left=new Vector2(-dir.y,dir.x);
                    for(float t=8-walked%20;t<leg;t+=20)
                    {
                        if(t<0)continue;
                        var p=a+dir*t+left*(t%40<20?2.2f:-2.2f);float h=MireHeight(p);if(p.y>150||h<MireWater-.35f)continue;
                        var foot=new Vector3(p.x,Mathf.Max(h,MireWater-.3f),p.y);float tall=1.9f+R()*.4f;
                        poles.Slot(0).Tube(new[]{foot+Vector3.down*.3f,foot+Vector3.up*tall*.5f+Vector3.right*.04f,foot+Vector3.up*tall},new[]{.055f,.048f,.035f},6,BarkDark,BarkPale);
                        poles.Slot(1).Rope(foot+Vector3.up*(tall-.2f),foot+Vector3.up*(tall-.95f)+new Vector3(.12f,0,.08f),.05f,.018f,new Color(.86f,.78f,.52f));
                        for(int k=0;k<3;k++)poles.Slot(1).Lump(foot+Vector3.up*(tall-.4f-.18f*k)+new Vector3(.08f,0,.05f),Vector3.one*.035f,k,.1f,new Color(.82f,.72f,.48f),new Color(.6f,.5f,.34f),6,4);
                        poles.Slot(2).Quad(foot+Vector3.up*(tall-.1f),foot+Vector3.up*(tall-.1f)+new Vector3(.36f,-.08f,.05f),foot+Vector3.up*(tall-.5f)+new Vector3(.4f,-.1f,.06f),foot+Vector3.up*(tall-.5f),new Vector3(-.2f,0,-1),new Color(.9f,.6f,.26f),.8f);
                    }
                    walked+=leg;
                }
                kit.Built("Way-poles",poles,Bark,Reed,Weave);
                // A drying rack by the landing: two forked posts, a pole across, bundles of reed hung over it.
                var rack=new ZoneKit.Shape();Vector3 r0=kit.On(-17.8f,6.5f),r1=kit.On(-17.2f,10.4f);
                foreach(var f in new[]{r0,r1})rack.Slot(0).Tube(new[]{f+Vector3.down*.3f,f+Vector3.up*1.7f,f+Vector3.up*1.95f+Vector3.right*.15f},new[]{.07f,.06f,.04f},6,BarkDark,BarkPale);
                rack.Slot(0).Tube(new[]{r0+Vector3.up*1.72f,r1+Vector3.up*1.72f},new[]{.05f,.05f},6,BarkPale,BarkPale);
                for(int k=0;k<6;k++){var at=Vector3.Lerp(r0,r1,(k+.5f)/6)+Vector3.up*1.72f;foreach(int side in new[]{-1,1})rack.Slot(1).Tube(new[]{at,at+new Vector3(side*.18f,-.5f,0),at+new Vector3(side*.24f,-1.05f,.04f)},new[]{.08f,.1f,.06f},5,new Color(.8f,.74f,.5f),new Color(.66f,.6f,.4f),1,1,false);}
                kit.Built("Drying rack",rack,Bark,Reed);
            }
            /// <summary>The station: its walls' broken tops and what grows on them, its columns and lamps, the pipes the pumps drive, each room's own thing to look at. At the back and the edges.</summary>
            static void StationDress(ZoneKit kit)
            {
                var rng=new System.Random(71);float R()=>(float)rng.NextDouble();
                // ---- coping: the station's walls end in a course of big blocks, broken and fallen in places, along every edge of a wall's top that looks down on a floor.
                var coping=new ZoneKit.Shape();var vines=new ZoneKit.Shape();int blocks=0;
                for(float x=-40;x<50;x+=1.2f)for(float z=163;z<310;z+=1.2f)
                {
                    var p=new Vector2(x+R()*.4f,z+R()*.4f);float clear=FloorAt(p,out _);if(clear<1.1f||clear>2.6f)continue;
                    float top=Height(p);FloorAt(p,out float floor);if(top-floor<2.4f||R()<.28f)continue;
                    var grade=new Vector2(Height(p+Vector2.right*.6f)-Height(p-Vector2.right*.6f),Height(p+Vector2.up*.6f)-Height(p-Vector2.up*.6f));if(grade.sqrMagnitude>.6f)continue;
                    float yaw=R()*8-4+(R()<.5f?0:90);var size=new Vector3(1+R()*.5f,.45f+R()*.3f,.8f+R()*.3f);
                    coping.Slot(0).Box(new Vector3(p.x,top+size.y*.4f,p.y),size,Quaternion.Euler(R()*6-3,yaw,R()*6-3),StoneDark,StoneWarm,.55f);blocks++;
                    // Creeper down the face below it, now and then, and a fern on top.
                    if(R()<.3f)
                    {
                        var face=-new Vector2(FloorAt(p+Vector2.right*.6f,out _)-FloorAt(p-Vector2.right*.6f,out _),FloorAt(p+Vector2.up*.6f,out _)-FloorAt(p-Vector2.up*.6f,out _)).normalized;
                        var from=new Vector3(p.x+face.x*.4f,top,p.y+face.y*.4f);float drop=Mathf.Min(top-floor-.4f,1.5f+R()*2.5f);
                        for(int k=0;k<4;k++){var at=from+new Vector3(R()-.5f,0,R()-.5f)*.8f;vines.Slot(0).Tube(new[]{at,at+Vector3.down*drop*.5f+new Vector3(face.x,0,face.y)*.15f,at+Vector3.down*drop*(.7f+.3f*R())},new[]{.04f,.035f,.02f},4,new Color(.5f,.62f,.36f),new Color(.4f,.56f,.32f),1,1,false);}
                    }
                    if(R()<.22f)vines.Slot(1).Fern(new Vector3(p.x,top+size.y*.7f,p.y),.6f+R()*.4f,6,rng,new Color(.4f,.5f,.3f),new Color(.56f,.66f,.38f));
                }
                kit.Built("Wall coping",coping,Masonry);kit.Built("Wall growth",vines.Slot(1),Reed,Leaf);
                Debug.Log("WAYSTATION_COPING "+blocks);
                // ---- columns: half set into the hall's east wall and the terrace's (free-standing on the walk, each hid a hero standing a few metres north of it at the stair head or
                // the next alcove), and on the great hall's north and east rim far enough back that nothing of one overhangs the floor.
                var columns=new List<(float x,float z,float tall)>{(15.7f,173.6f,4.6f),(15.7f,180.4f,4.6f),(15.7f,187.6f,4.4f),(-13.8f,191.2f,4.2f),(5.2f,191.2f,4.2f),(23.9f,222.4f,4.4f),(23.9f,233.5f,4.4f),(23.9f,246.2f,4.2f)};
                foreach(var (x,z,tall) in columns)kit.Prop(ZoneKit.Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,0,z),tall,R()*40-20);
                foreach(float bearing in new[]{-20f,0f,20f,70f,90f,110f})
                {
                    var dir=new Vector2(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad));var at=C+dir*(ArenaRadius+1.4f);
                    var rim=kit.Prop(ZoneKit.Kit("AncelaneColumn","HushwellColumn"),new Vector3(at.x,0,at.y),5.4f,R()*40-20);
                    // Walked back from the floor until the whole of it stands outside r 18.6, then stood on the ground where it ends up.
                    for(int k=0;k<12;k++){var b=ZoneKit.Drawn(rim);var near=new Vector2(b.center.x,b.center.z)-dir*Mathf.Max(b.extents.x,b.extents.z);if(Vector2.Distance(near,C)>=ArenaRadius+.6f)break;rim.transform.position+=new Vector3(dir.x,0,dir.y)*.5f;}
                    var fit=ZoneKit.Drawn(rim);rim.transform.position+=Vector3.up*(kit.Y(fit.center.x,fit.center.z)+.01f-fit.min.y);
                }
                // ---- the pipes the old engineers ran from pump to pump: bronze, along the wall feet and down the stairs. Quill counted them.
                var pipes=new ZoneKit.Shape();
                void Pipe(params Vector3[] path){var radii=path.Select(_=>.16f).ToArray();pipes.Slot(0).Tube(path,radii,8,BarkPale,StoneDark,1,.4f,false);
                    for(int i=1;i<path.Length;i++)for(float t=.25f;t<1;t+=.5f){var c=Vector3.Lerp(path[i-1],path[i],t);pipes.Slot(0).Tube(new[]{c-(path[i]-path[i-1]).normalized*.08f,c+(path[i]-path[i-1]).normalized*.08f},new[]{.22f,.22f},8,BarkPale,BarkPale);}}
                Vector3 W(float x,float z,float up)=>new(x,kit.Y(x,z)+up,z);
                Pipe(W(14.6f,184.5f,.4f),W(14.6f,191.5f,.4f),new Vector3(14.6f,HallTop+.4f,192.5f),new Vector3(14.6f,Terrace+.4f,206.5f),W(22.1f,207,.4f),W(22.1f,246,.4f),W(19.5f,248.8f,.4f));
                Pipe(W(-21.6f,206.6f,.35f),W(-21.6f,249.2f,.35f),W(-4.6f,249.2f,.35f));
                Pipe(W(-4.6f,249.6f,.35f),new Vector3(-4.6f,Landing+.35f,259.3f),W(-8.4f,264.7f,.35f));
                Pipe(W(22.1f,217.5f,.35f),W(29.2f,220.3f,.35f));
                kit.Built("Pump pipes",pipes,Bronze);
                // ---- treads on the stairs, so each reads as built and not as a slope: the hall's stair down, the great stair up, Pim's.
                var treads=new ZoneKit.Shape();
                void Stair(Vector2 from,Vector2 to,float wide,float step){var run=to-from;float length=run.magnitude;var along=run/length;float yaw=Mathf.Atan2(along.x,along.y)*Mathf.Rad2Deg;
                    for(float s=step*.5f;s<length;s+=step){var c=from+along*s;float y=kit.Y(c.x,c.y);treads.Slot(0).Box(new Vector3(c.x,y-.05f,c.y),new Vector3(wide,.12f,step*.94f),Quaternion.Euler(0,yaw,0),StoneDark,StoneWarm,.55f);}}
                Stair(new Vector2(12,192),new Vector2(12,206),5.6f,.5f);Stair(new Vector2(0,250),new Vector2(0,259),8.4f,.42f);Stair(new Vector2(-21.8f,221.5f),new Vector2(-27.8f,221.5f),5.6f,.45f);
                Stair(new Vector2(1.6f,245),new Vector2(8.4f,245),9.4f,.48f);Stair(new Vector2(29.2f,218),new Vector2(32.4f,218),13.2f,.4f);Stair(new Vector2(37.4f,218),new Vector2(34.2f,218),13.2f,.4f);
                kit.Built("Stair treads",treads,Masonry);
                // ---- a bronze rail along the terrace's edge where it drops to the low floor, broken where the ramp goes down.
                var rail=new ZoneKit.Shape();
                for(float z=207;z<239;z+=2.2f){var foot=new Vector3(8.4f,Terrace,z);rail.Slot(0).Post(foot,1.05f,.06f,.05f,6,StoneDark,BarkPale);if(z+2.2f<239)rail.Slot(0).Beam(foot+Vector3.up*1,new Vector3(8.4f,Terrace+1,z+2.2f),.07f,.07f,BarkPale);}
                kit.Built("Terrace rail",rail,Bronze);
                // ---- each alcove has a carved shell over it, a bronze ring round its head: a niche made for something to stand in.
                var niches=new ZoneKit.Shape();
                foreach(var (x0,x1,z0,z1,level) in Alcoves)
                {
                    var c=new Vector2((x0+x1)*.5f,(z0+z1)*.5f);bool ns=x1-x0>z1-z0;var back=ns?new Vector2(0,c.y>200&&c.y<249&&c.x<0?0:1):new Vector2(c.x>0?1:-1,0);
                    if(c.y>249)back=new Vector2(0,1);if(c.y<195)back=new Vector2(0,1);
                    var across=new Vector3(back.y,0,-back.x);float wide=1.6f;var foot=new Vector3(c.x+back.x*1.1f,level,c.y+back.y*1.1f);
                    var arc=new Vector3[9];var thick=new float[9];for(int i=0;i<9;i++){float a=i/8f*Mathf.PI;arc[i]=foot+across*Mathf.Cos(a)*wide+Vector3.up*(2.6f+Mathf.Sin(a)*.9f);thick[i]=.12f;}
                    niches.Slot(0).Tube(arc,thick,6,BarkPale,BarkPale);
                    foreach(int side in new[]{-1,1})niches.Slot(0).Post(foot+across*side*wide,2.6f,.12f,.12f,6,StoneDark,BarkPale);
                }
                kit.Built("Alcove shells",niches,Bronze);
                // ---- a girder of the fallen roof across the hall's west end, and its fellows lying in the gallery: bronze ribs once, green now.
                var ribs=new ZoneKit.Shape();
                foreach(var (a,b) in new[]{(new Vector3(-14.6f,HallSunk+.2f,183),new Vector3(-6,HallSunk+1.6f,191)),(new Vector3(-21,Low+.3f,226),new Vector3(-15,Low+.9f,231)),(new Vector3(-21.5f,Low+.4f,246),new Vector3(-13,Low+1.8f,248.8f))})
                {ribs.Slot(0).Beam(a,b,.42f,.56f,StoneDark,BarkPale);ribs.Slot(0).Beam(a+Vector3.up*.5f,b+Vector3.up*.5f,.16f,.16f,BarkPale);}
                kit.Built("Fallen girders",ribs,Bronze);
                // ---- lamps of the old engineers': shards of their glass on brackets, still faintly lit. One lights each room; glow-fungus does the rest.
                foreach(var (x,z,yaw) in new[]{(-14f,174f,90f),(-6.5f,193.4f,180f),(21.4f,211f,270f),(21.4f,240f,270f),(-21.4f,215f,90f),(-21.4f,230f,90f),(-16f,249.4f,180f),(-35.6f,218f,90f),(48.4f,223f,270f),(-9.5f,259.6f,0f)})
                    HallLamp(kit,new Vector2(x,z),yaw);
                var fungus=new ZoneKit.Shape();
                for(int k=0;k<90;k++)
                {
                    var p=new Vector2(-38+R()*86,166+R()*142);float clear=FloorAt(p,out float floor);if(clear<.2f||clear>1.2f)continue;
                    var face=new Vector2(FloorAt(p+Vector2.right*.6f,out _)-FloorAt(p-Vector2.right*.6f,out _),FloorAt(p+Vector2.up*.6f,out _)-FloorAt(p-Vector2.up*.6f,out _));if(face.sqrMagnitude<.0001f)continue;
                    Fungus(fungus,new Vector3(p.x,floor+.3f+R()*1.6f,p.y),new Vector3(-face.x,0,-face.y),k,.22f+R()*.14f);
                }
                kit.Built("Glow-fungus",fungus.Slot(1),Bark,Glow);
                // ---- Pim's pocket: reed bedding, the half-woven mat on its frame, a cord tied to the grille.
                var nest=new ZoneKit.Shape();var mat=new Vector3(-35.6f,Pocket,219.5f);
                foreach(float z in new[]{218f,221f})nest.Slot(0).Post(new Vector3(-35.9f,Pocket-.1f,z),1.6f,.05f,.045f,6,BarkDark,BarkPale);
                nest.Slot(0).Beam(new Vector3(-35.9f,Pocket+1.45f,217.8f),new Vector3(-35.9f,Pocket+1.45f,221.2f),.06f,.06f,BarkPale);
                nest.Slot(1).Quad(new Vector3(-35.8f,Pocket+.5f,218.2f),new Vector3(-35.8f,Pocket+.5f,220.8f),new Vector3(-35.8f,Pocket+1.4f,220.8f),new Vector3(-35.8f,Pocket+1.4f,218.2f),Vector3.right,Color.white,.8f);
                Reeds(nest,2,new Vector3(-33.5f,Pocket,225.6f),1.1f,30,77,.35f);
                kit.Built("Pim's nest",nest,Bark,Weave,Reed);
                // ---- the vault: empty shelves along its north and south walls, and the wall of marks on its east.
                var shelves=new ZoneKit.Shape();
                foreach(float z in new[]{209.2f,226.8f})for(float x=39.4f;x<48;x+=3.1f)
                {
                    foreach(int side in new[]{-1,1})shelves.Slot(0).Box(new Vector3(x+side*1.35f,VaultFloor+1.1f,z),new Vector3(.14f,2.2f,.7f),Quaternion.identity,StoneDark);
                    for(int s=0;s<3;s++)shelves.Slot(0).Box(new Vector3(x,VaultFloor+.4f+s*.7f,z),new Vector3(2.8f,.1f,.7f),Quaternion.identity,StoneDark,StoneWarm);
                }
                var plaque=new Vector3(48.7f,VaultFloor+1.7f,218);
                shelves.Slot(1).Quad(plaque+new Vector3(0,-1.3f,-1.6f),plaque+new Vector3(0,-1.3f,1.6f),plaque+new Vector3(0,1.3f,1.6f),plaque+new Vector3(0,1.3f,-1.6f),Vector3.left,Color.white,1/3.2f);
                kit.Built("Vault shelves and marks",shelves,Bronze,Plaque);
                // ---- roots of the mire over the station: hanging from the great hall's north wall top and the gallery's west wall, down toward the water that brought them.
                var roots=new ZoneKit.Shape();
                for(int k=0;k<26;k++)
                {
                    float bearing=-40+k*3.6f+R()*2;var top=new Vector3(C.x+Mathf.Sin(bearing*Mathf.Deg2Rad)*(ArenaRadius+2.2f),0,C.y+Mathf.Cos(bearing*Mathf.Deg2Rad)*(ArenaRadius+2.2f));top.y=Height(new Vector2(top.x,top.z));
                    var inward=(new Vector3(C.x,0,C.y)-new Vector3(top.x,0,top.z)).normalized;float drop=1.6f+R()*2.2f;
                    roots.Slot(0).Tube(new[]{top,top+inward*.8f+Vector3.down*drop*.4f,top+inward*1.1f+Vector3.down*drop},new[]{.12f,.08f,.03f},5,BarkDark,BarkPale,1,.4f,false);
                }
                kit.Built("Hanging roots",roots,Bark);
            }

            /// <summary>The floor: peat with reed litter on the islands, wet and dark at every waterline, the old road's paving through the causeway; worn flags in the station, greener
            /// and silted the lower they lie, cracked, with fungus and fern at the wall feet. Cloud shade drifts over all of it.</summary>
            static void Ground(ZoneKit kit)
            {
                var ground=GroundKit.Open(kit.zone,GroundKit.FindFloor(),83,1.1f);
                float Smooth(float a,float b,float x)=>GroundKit.Smooth(a,b,x);
                float Lvl(Vector2 p){FloorAt(p,out float l);return l;}
                bool Station(Vector2 p)=>p.y>166.5f;
                float Dry(Vector2 p)=>Station(p)?1:Smooth(MireWater+.02f,MireWater+.22f,Lvl(p));
                float Wet(Vector2 p)=>Station(p)?0:Smooth(MireWater+.38f,MireWater+.06f,Lvl(p));
                float Causeway(Vector2 p)=>1-Smooth(2.2f,3.4f,ZoneKit.Segment(p,Fords[2].a,Fords[2].b));
                foreach(var way in Ways)ground.ClearLane(way,1.5f);
                ground.Look("River",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Wendmire/waystation-bark.png"));
                ground.Layers(new Vector4(.05f,.34f,.4f,.3f),1.2f,new GroundKit.Layer("pavers",new Color(.86f,.66f,.54f),5.5f),new GroundKit.Layer("damp",new Color(.74f,.82f,.72f),4.6f),new GroundKit.Layer("litter",new Color(.82f,.74f,.5f),3.6f));
                // Paving: whole in the station's rooms, broken toward their walls and in the low rooms where silt lies on it; the causeway's under the water.
                // The station's forecourt paving runs out over the portico's shore, broken and grown over toward the water.
                float Forecourt(Vector2 p)=>p.y>153&&p.y<=166.5f&&Mathf.Abs(p.x)<15?Smooth(153,159,p.y)*(1-Smooth(10,15,Mathf.Abs(p.x)))*Smooth(.34f,.5f,ground.Noise(p,3,19)):0;
                ground.Paint(0,p=>Station(p)?(.55f+.45f*Smooth(.3f,.55f,ground.Noise(p,6,3)))*(p.y<180&&p.x<WalkEdge?.85f:1):Mathf.Max(Causeway(p)*.9f,Forecourt(p)));
                ground.Erase(0,p=>Station(p)?Smooth(-2.8f,-4.3f,Lvl(p))*Smooth(.42f,.62f,ground.Noise(p,5,11))*.8f:0);
                // Damp: every waterline in the mire, and in the station where the water stood longest (the lower, the more).
                ground.Paint(1,p=>Station(p)?Smooth(-1,-4.2f,Lvl(p))*(.35f+.5f*Smooth(.4f,.6f,ground.Noise(p,4,7))):Wet(p)*.9f).PaintFoot(1,1.6f,.5f,3);
                // Reed litter: the islands' own floor, in drifts.
                ground.Paint(2,p=>Station(p)?0:Dry(p)*(.15f+.85f*Smooth(.4f,.6f,ground.Noise(p,6,21)))).PaintPatches(2,4,.24f,p=>Station(p)?0:Dry(p),5);
                ground.Shade(p=>Station(p)?.12f*Smooth(1.6f,.3f,ground.Edge(p)):.1f*Wet(p)).ShadeRelief(p=>Station(p)?0:Tussocks(p));
                ground.Decals(new[]{"CracksA","CracksB"},6,p=>Station(p)?.7f:0,1.4f,2.8f,new Color(.5f,.42f,.36f,.9f));
                ground.Decals(new[]{"Slabs","Flagstones"},2.4f,p=>Station(p)?.6f*Smooth(.5f,.62f,ground.Noise(p,9,13)):Causeway(p)*.4f,1.2f,2.2f,new Color(.9f,.72f,.6f));
                ground.Decals(new[]{"SandDrift"},4,p=>Station(p)?Smooth(-2.5f,-4f,Lvl(p)):0,2,3.6f,new Color(.66f,.68f,.58f,.6f),false);
                ground.Decals(new[]{"Stain"},3,p=>Station(p)?.7f:Wet(p),2.2f,4,new Color(.3f,.34f,.28f,.6f),false);
                ground.Decals(new[]{"Lichen"},6,p=>Smooth(.44f,.62f,ground.Noise(p,11,4))*(Station(p)?.6f:Dry(p)),1.1f,2.4f,new Color(.5f,.6f,.4f,.85f),false);
                ground.Decals(new[]{"Litter"},10,p=>Station(p)?.15f:Dry(p),1,2,new Color(.72f,.64f,.4f,.9f),false);
                ground.Decals(new[]{"PebblesA","PebblesB"},4,p=>Station(p)?.5f:Dry(p)*.6f,.8f,1.6f,new Color(.7f,.64f,.56f));
                ground.Decals(new[]{"Tracks"},.8f,p=>Station(p)?0:Wet(p)*Dry(p),.8f,1.4f,new Color(.3f,.3f,.24f,.6f),false);
                // The ways across the mire are trodden: darker, wetter peat along the line, broken where the ground is firmer, with tracks in it.
                foreach(var way in Ways)if(way[0].y<170){ground.PaintPath(1,way,1.1f,.85f,.45f,.7f,(int)way[0].x+90);ground.Strip("Tracks",way,.3f,1.2f,new Color(.32f,.28f,.2f,.5f));}
                ground.Decals(new[]{"Stain"},4,p=>Station(p)?0:Dry(p)*Smooth(.5f,.66f,ground.Noise(p,6,29)),1.6f,3,new Color(.28f,.26f,.18f,.55f),false,.1f,41);
                ground.Decals(new[]{"Lichen"},5,p=>Station(p)?0:Dry(p)*Smooth(.4f,.56f,ground.Noise(p,8,31)),.9f,1.8f,new Color(.62f,.66f,.42f,.8f),false,.1f,43);
                // Moss in broad cushions on the peat, greener than anything else on an island: what makes one island's floor several things.
                ground.Decals(new[]{"Lichen","Stain"},11,p=>Station(p)?0:Dry(p)*Smooth(.44f,.58f,ground.Noise(p,10,47)),2.2f,4,new Color(.36f,.52f,.26f,.85f),false,.1f,49);
                // The great hall's floor under its flood: algae in broad dark-green beds, silt drifted against the plinths and the rim, sunk leaves. Seen through the water in phase one,
                // left lying on the paving once it has gone.
                bool Hall(Vector2 p)=>Vector2.Distance(p,C)<ArenaRadius-.3f;
                ground.Decals(new[]{"Lichen","Stain"},20,p=>Hall(p)?Smooth(.34f,.5f,ground.Noise(p,6,61)):0,2.4f,4.4f,new Color(.26f,.38f,.22f,.9f),false,.1f,63);
                ground.Decals(new[]{"SandDrift"},6,p=>Hall(p)?.4f+.6f*Smooth(9,16,Vector2.Distance(p,C)):0,1.6f,3,new Color(.66f,.66f,.54f,.7f),false,.1f,67);
                ground.Decals(new[]{"Litter"},8,p=>Hall(p)?1:0,.9f,1.8f,new Color(.62f,.56f,.36f,.85f),false,.1f,71);
                // Paving the flood lifted and dropped askew, and the gravel it brought in: what makes one stretch of the hall's floor unlike the next.
                ground.Decals(new[]{"Slabs","Flagstones"},6,p=>Hall(p)?Smooth(.44f,.58f,ground.Noise(p,5,73)):0,1.2f,2.2f,new Color(.96f,.78f,.64f),true,.1f,75);
                ground.Decals(new[]{"PebblesA","PebblesB"},8,p=>Hall(p)?1:0,.8f,1.5f,new Color(.74f,.7f,.6f),true,.1f,77);
                // Straw: drifts of pale dead reed, the islands' third tone (with the dark leaf litter and the green moss), flat, so they lie in a fight's floor too.
                ground.Decals(new[]{"SandDrift","Litter"},6,p=>Station(p)?0:Dry(p)*Smooth(.46f,.6f,ground.Noise(p,7,53)),1.2f,2.4f,new Color(.7f,.64f,.46f,.55f),false,.1f,59);
                ground.LampPools(2.8f,new Color(1,.8f,.56f,.45f));
                float Foot(Vector2 p)=>1-Smooth(.6f,2.6f,ground.Edge(p));
                ground.Cover(new[]{"Reeds"},22,p=>Station(p)?0:Wet(p)*Smooth(.32f,.5f,ground.Noise(p,6,17)),.34f,.58f,new Color(.66f,.72f,.46f),3);
                ground.Cover(new[]{"TuftA","TuftB","TuftC","Sprouts"},34,p=>Station(p)?Foot(p)*.5f:Dry(p)*Smooth(.4f,.58f,ground.Noise(p,7,9)),.14f,.32f,new Color(.6f,.68f,.42f),3);
                ground.Cover(new[]{"Fern"},10,p=>Station(p)?Foot(p):Dry(p)*Smooth(.5f,.64f,ground.Noise(p,9,19)),.26f,.5f,new Color(.5f,.66f,.4f),2);
                ground.Cover(new[]{"FungusA","FungusB"},14,p=>Station(p)?Foot(p):Dry(p)*Smooth(.58f,.7f,ground.Noise(p,5,23)),.12f,.28f,new Color(.86f,.92f,.84f),3);
                ground.Cover(new[]{"BonesA","BonesB"},5,p=>Vector2.Distance(p,LeechMiddle)<16&&!Station(p)?Dry(p)*.8f:0,.12f,.24f,new Color(.86f,.84f,.76f),2);
                ground.Cover(new[]{"Twigs"},4,p=>Station(p)?.2f:Dry(p)*.5f,.14f,.28f,new Color(.6f,.56f,.44f));
                ground.Rocks(3,p=>Station(p)?Foot(p)*.6f:Dry(p)*.3f,.08f,.26f,new Color(.82f,.7f,.62f));
                ground.Glows(3,p=>Station(p)?Foot(p):Wet(p)*.6f,.06f,.14f,new Color(.4f,1,.85f,.8f));
                ground.MovingShade("cloud",22,new Vector2(.18f,.08f),.38f,.5f,.04f);
                ground.Finish();
            }
        }
    }
}
