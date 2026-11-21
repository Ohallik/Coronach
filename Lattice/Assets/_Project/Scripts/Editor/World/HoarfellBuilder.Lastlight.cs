using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>Lastlight (town; the Kept's village inside a frozen Choir's ribcage). Map brief, plan and coordinates: docs/maps/Lastlight.md. Everything of the zone's own is in
    /// <see cref="TownZone"/>, so nothing here can collide with the other Hoarfell files' members.</summary>
    public static partial class HoarfellBuilder
    {
        static void BuildLastlight()=>TownZone.Build();

        internal static class TownZone
        {
            // ---------------------------------------------------------------- who and what stands where (plan coordinates; routes, tests and LookDev read these)
            internal static readonly Vector2 Arrival=new(0,4),Pad=new(-7,7),Hask=new(-25,26.5f),IdraArguing=new(-24,24.2f),PensCrossing=new(-20,25),IdraYard=new(-12,48),MapStand=new(-12,51.4f),
                BenchAt=new(-18.5f,46),TallyAt=new(-10,47.6f),BathHouse=new(28,46),Cradle=new(25.6f,37.6f),BathSpawn=new(23.4f,34.6f),VentSeat=new(31.8f,38.6f),Tern=new(8,60.5f),TernHall=new(8,68),
                RibTally=new(12,58.4f),IruShrine=new(44,44),IruHall=new(5.2f,59.6f),LowLamp=new(48.5f,48),Seats=new(58,46),ScarsAt=new(55.4f,44.2f),ReachGate=new(-63.5f,42),ReachSpawn=new(-57,42);
            /// <summary>The ribs, numbered from the skull: each a generated RibArch standing over the back of the town, spanning north to south from z 60 to z 76.</summary>
            internal static readonly float[] Ribs={50,38,26,14,2,-10,-22,-34,-46,-58};
            internal const float RibAt=68,RibTall=12,RibFront=60.5f,RibBack=75.5f;
            /// <summary>The high rib-hall at the east end: a bank of packed snow over bone, its top at <see cref="Terrace"/>, west face at x 44, south face at z 57.5; a knoll at its east
            /// end by the skull. The climb's ledge runs up its south face.</summary>
            internal const float Terrace=4.8f,TerraceWest=44,TerraceSouth=57.5f,Knoll=6.2f;
            internal static readonly Vector2 KnollAt=new(60,68);
            /// <summary>The climb: the ledge up the bank's south face, each point (x, height, z), its landing for the first lamp, and the three high lamps (L1 on the landing, L2 on the bank
            /// under the first rib, L3 on the knoll by the skull).</summary>
            internal static readonly Vector3[] Climb={new(38,0,51.5f),new(43,1.2f,53.2f),new(47.5f,2.4f,53.8f),new(52.5f,3.6f,54.4f),new(57,4.8f,55.2f),new(58.5f,4.8f,57.8f)};
            internal static readonly Vector2[] HighLamps={new(47.5f,53.8f),new(52,63),new(60,68)};
            internal const float LedgeHalf=2.1f,Landing=2.8f,RimTall=.45f,RimWide=.7f,Bank=1.1f;
            internal static readonly Vector2[] Vents={new(-4,21),new(7,28),new(16,17),new(23,30.5f),new(36,29),new(-30,48)};
            internal static readonly Vector2 PenMin=new(-48,14),PenMax=new(-29,34);
            /// <summary>The ways feet go between the town's places: nothing is scattered within reach of these, and the ground kit keeps them clear and paints them trodden.</summary>
            internal static readonly Vector2[][] Ways=
            {
                new[]{Arrival,new Vector2(0,12),new Vector2(0,22),new Vector2(-2,34),new Vector2(-8,42),new Vector2(-12,45.6f)},
                new[]{new Vector2(0,22),new Vector2(-12,26),new Vector2(-20,25.8f),new Vector2(-23.4f,26.4f)},
                new[]{new Vector2(-2,34),new Vector2(8,38),new Vector2(18,37),BathSpawn,new Vector2(25.6f,38)},
                new[]{BathSpawn,new Vector2(31.5f,34.4f),new Vector2(37.2f,36.6f),new Vector2(39.6f,41),new Vector2(42.4f,44)},
                new[]{new Vector2(-2,34),new Vector2(2,46),new Vector2(6,54),new Vector2(8,58.6f)},
                new[]{new Vector2(-8,42),new Vector2(-24,42),new Vector2(-40,42),ReachSpawn,new Vector2(-60.8f,42)},
                new[]{Arrival,Pad},
                new[]{new Vector2(42.4f,44),new Vector2(38,51.5f)},
                new[]{new Vector2(39.6f,41),new Vector2(45,42.6f),new Vector2(48.5f,46.4f),new Vector2(55.4f,44.6f)},
                new[]{new Vector2(58.5f,57.8f),new Vector2(55,60.5f),HighLamps[1],new Vector2(56,64.8f),HighLamps[2]},
            };
            internal static readonly (Vector2 at,float half)[] Signs={(new(14.8f,56.4f),2.4f),(new(3.6f,56.2f),2.6f),(new(-33,36.6f),2.6f),(new(-17,40.8f),2.8f),(new(31.6f,32),3.2f),(new(47,40.6f),2.6f),(new(-59.5f,37.4f),2.6f)};
            static readonly string[] SignWords={"FOURTH RIB","TERN'S HALL","BEAST PENS","CARVING YARD","VENT BATH-HOUSE","LAMP SHRINE","REACH GATE"};
            static readonly Vector2[] Named={Tern,Hask,IruShrine,IdraYard,IdraArguing};
            /// <summary>The Kept standing at their work: coat, where, facing, tint. Off every way, off the signs, out of sight-lines to the named, never two coats alike side by side.</summary>
            static readonly (string body,float x,float z,float yaw,Color tint)[] Folk=
            {
                ("KeptHerder",-38.4f,36.4f,180,new(1,.96f,.92f)),("IruBody",-44,11.4f,10,new(.92f,.98f,1.08f)),
                ("HaskBody",-17.8f,48.8f,10,new(1.06f,.98f,.9f)),("TernBody",-6.4f,49.2f,350,new(.94f,.9f,1.04f)),
                ("KeptHerder",34.6f,39.6f,210,new(.92f,1,1.06f)),("HaskBody",5,24.6f,40,new(.96f,.94f,1.06f)),
                ("IruBody",-17.2f,61,175,new(1.08f,.96f,.9f)),("KeptHerder",21.4f,61.2f,165,new(1.04f,1.02f,.92f)),("TernBody",35.4f,47,120,new(1.04f,.96f,1.04f)),
                ("HaskBody",-48.6f,50.2f,240,new(.9f,.96f,1.04f)),
            };
            static readonly (string body,float x,float z,float yaw,Color tint)[] Children={("IruBody",13.2f,21.4f,210,new(1.06f,.98f,.92f)),("KeptHerder",14.8f,20.2f,160,new(.94f,1.04f,1.06f))};
            /// <summary>The Kept on errands (WalkLoop): coat, loop, pace, wait at each stop, tint, there-and-back, name. Each is checked clear of anything solid as built.</summary>
            static readonly (string body,Vector2[] loop,float speed,float pause,Color tint,bool back,string name)[] Errands=
            {
                ("KeptHerder",new[]{new Vector2(-22,10),new Vector2(-11,15.6f)},.9f,2.4f,new(1,.94f,.9f),true,"A herder carrying a ladder"),
                ("HaskBody",new[]{new Vector2(9,9),new Vector2(20,12.6f)},.9f,2.6f,new(.94f,.98f,1.06f),true,"A herder with a ladder to the pens"),
                ("IruBody",new[]{new Vector2(-6,56.4f),new Vector2(-22,57.6f)},.85f,2.8f,new(1.06f,1,.94f),true,"A herder carrying hides"),
                ("TernBody",new[]{new Vector2(41,25),new Vector2(45,33.4f)},.8f,3,new(.96f,1,1.06f),true,"A bather with buckets"),
                ("KeptHerder",new[]{new Vector2(-46.8f,16.4f),new Vector2(-46.8f,31.6f)},.7f,3.2f,new(1.04f,.96f,1),true,"A herder walking the pens' fence"),
                ("HaskBody",new[]{new Vector2(49,39.6f),new Vector2(57,40.2f)},.8f,2.6f,new(1.02f,1.04f,.92f),true,"A lamp-keeper's helper with oil"),
                ("IruBody",new[]{new Vector2(18,22),new Vector2(26,25.6f),new Vector2(29.4f,20.4f)},2,.5f,new(1.08f,.96f,.96f),false,"A child running between the vents"),
            };
            /// <summary>Beasts in the pens and two being readied by the Reach gate: where, facing, size.</summary>
            static readonly (float x,float z,float yaw,float size)[] Beasts={(-44,18.4f,30,2.8f),(-39.6f,22.4f,200,3),(-34.6f,17.2f,95,2.6f),(-43.2f,27,150,2.9f),(-38,30.4f,10,2.7f),(-33.2f,26.4f,250,3.1f),(-41,32.2f,120,2.5f),
                (-52,48.2f,80,2.8f),(-47.6f,52.4f,250,2.6f)};
            internal static readonly Vector2[] Longhouses={TernHall,new(20,68),new(32,68),new(-16,68),new(-40,68)};

            /// <summary>LookDev's stills of the town (registered in LookDev.Views): each place as it is first walked, at its floor's height, then two wide.</summary>
            internal static LookDev.View[] Views=>new[]{new LookDev.View("arrival",new Vector3(0,0,4)),new LookDev.View("pad",new Vector3(-7,0,7)),new LookDev.View("commons",new Vector3(2,0,24)),
                new LookDev.View("pens",new Vector3(-32,0,24)),new LookDev.View("hask",new Vector3(-23,0,26)),new LookDev.View("yard",new Vector3(-12,0,45)),new LookDev.View("bath",new Vector3(27,0,39)),
                new LookDev.View("tern",new Vector3(8,0,57.5f)),new LookDev.View("fourth-rib",new Vector3(13,0,56.5f)),new LookDev.View("longhouses",new Vector3(-16,0,58)),new LookDev.View("west",new Vector3(-46,0,46)),
                new LookDev.View("shrine",new Vector3(46,0,44)),new LookDev.View("skull",new Vector3(56,0,44)),new LookDev.View("climb",new Vector3(47.5f,2.4f,53.8f)),new LookDev.View("high-hall",new Vector3(52,4.8f,63)),
                new LookDev.View("knoll",new Vector3(60,6.2f,68)),new LookDev.View("reach-gate",new Vector3(-58,0,42)),new LookDev.View("overview",new Vector3(0,0,36),false,3.6f),
                new LookDev.View("overview-east",new Vector3(40,0,52),false,2.4f)};
            // ---------------------------------------------------------------- the ground
            static float Smooth(float a,float b,float x)=>ZoneKit.Smooth(a,b,x);
            /// <summary>How far a plan point is from the climb's ledge (negative on it; its landings wider), and the ledge's height there.</summary>
            static float Ledge(Vector2 p,out float top)
            {
                float best=float.PositiveInfinity;top=0;
                for(int i=1;i<Climb.Length;i++)
                {
                    var a=new Vector2(Climb[i-1].x,Climb[i-1].z);var b=new Vector2(Climb[i].x,Climb[i].z);var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);
                    float gap=Vector2.Distance(p,a+d*t)-LedgeHalf;if(gap<best){best=gap;top=Mathf.Lerp(Climb[i-1].y,Climb[i].y,t);}
                }
                // The landing widens the ledge round the first lamp and keeps the ramp's own height: a flat disc set into the ramp left a step all round it that the bake
                // couldn't climb (the first two builds).
                return Mathf.Min(best,Vector2.Distance(p,HighLamps[0])-Landing);
            }
            /// <summary>The high rib-hall's bank: its top, with the knoll, where it stands; nothing (below zero) outside it.</summary>
            static float BankTop(Vector2 p)
            {
                float inside=Smooth(TerraceSouth-.9f,TerraceSouth,p.y)*Smooth(TerraceWest-.9f,TerraceWest,p.x);
                if(inside<=0)return -1;
                // The knoll rises over five metres: over three its steepest was past the bake's 35 degrees and the way from the second lamp went round it (9.8 m for 5.5).
                float knoll=(Knoll-Terrace)*(1-Smooth(2.5f,7.5f,Vector2.Distance(p,KnollAt)));
                return (Terrace+knoll)*inside;
            }
            /// <summary>The town's ground. Snow over ice, a few centimetres of wind-cut relief; a shallow bowl of bare stone at each vent; a steep bank of snow a metre high at the
            /// south and west edges (the camera's side: low), the spine's wall of snow and ice to the north, the skull and an ice wall to the east; and the high rib-hall's bank at
            /// the east end with its ledge climbing the south face behind a knee-high rim.</summary>
            internal static float Height(Vector2 p)
            {
                float relief=.06f*(Mathf.PerlinNoise(p.x*.11f+4,p.y*.11f+2)-.5f)+.035f*(Mathf.PerlinNoise(p.x*.37f,p.y*.37f+9)-.5f);
                float y=relief;
                foreach(var v in Vents)y-=.12f*(1-Smooth(1.2f,3,Vector2.Distance(p,v)));
                y+=Bank*Smooth(-3.4f,-4.4f,p.y)+Bank*Smooth(-66.4f,-67.4f,p.x);
                y+=8.5f*Smooth(77,81,p.y)+8*Smooth(66,70,p.x);
                float bank=BankTop(p);if(bank>0)y=Mathf.Max(y,bank);
                float d=Ledge(p,out float top);
                if(d<RimWide+1.2f)
                {
                    float rim=RimTall*Smooth(.6f,1.2f,top)*(1-Smooth(RimWide-.1f,RimWide+.05f,d))*Smooth(0,.3f,d)*(1-Smooth(top-.8f,top-.2f,bank));
                    // Where the ledge meets the high rib-hall at its own height there is no rim, and nothing falls from one: the rim's outer fall at full height left a ring of
                    // rim standing on the hall round the ledge's end.
                    float lip=top+RimTall*Smooth(.6f,1.2f,top)*(1-Smooth(top-.8f,top-.2f,bank));
                    float ledge=d<=0?top:d<=RimWide?top+rim:Mathf.Lerp(lip,y,Smooth(RimWide,RimWide+1.2f,d));
                    // Carved into the bank where the bank is higher: the ledge's floor is its own, its rim stands against the face.
                    y=d<=0?ledge:Mathf.Max(y,ledge);
                }
                return y;
            }
            static float FromWays(Vector2 p){float d=float.PositiveInfinity;foreach(var way in Ways)d=Mathf.Min(d,ZoneKit.FromPath(p,way));return d;}
            static bool InPens(Vector2 p)=>p.x>PenMin.x&&p.x<PenMax.x&&p.y>PenMin.y&&p.y<PenMax.y;
            /// <summary>The floor's own colour: snow, blue in its hollows; trodden grey-brown on the ways; dark wet stone round each vent with a ring of slush; straw and dung in the pens;
            /// bone chips in the carving yard; packed and swept on the shrine floor.</summary>
            static Color Tone(Vector2 p)
            {
                float hollow=Mathf.PerlinNoise(p.x*.08f+7,p.y*.08f+3),fine=Mathf.PerlinNoise(p.x*.31f,p.y*.31f+5);
                var snow=Color.Lerp(new Color(.8f,.86f,.96f),new Color(.97f,.98f,1),Smooth(.3f,.7f,hollow)*.8f+.2f*fine);
                // Worn ways mottled, packed grey here and fresh snow kicked over it there: one flat grey where three ways met by the shrine was the town's largest plain stretch.
                float worn=(1-Smooth(.8f,2.6f,FromWays(p)))*(.55f+.45f*Mathf.PerlinNoise(p.x*.45f+13,p.y*.45f+2));
                var tone=Color.Lerp(snow,Color.Lerp(new Color(.6f,.6f,.64f),new Color(.74f,.74f,.76f),Mathf.PerlinNoise(p.x*.9f,p.y*.9f+7)),worn*.7f);
                if(InPens(p))tone=Color.Lerp(tone,new Color(.62f,.56f,.46f),.55f+.2f*fine);
                float yard=1-Smooth(5,8,Vector2.Distance(p,new Vector2(-13,46)));tone=Color.Lerp(tone,new Color(.86f,.82f,.74f),yard*.45f);
                float shrine=1-Smooth(5,9,Vector2.Distance(p,new Vector2(50,45)));tone=Color.Lerp(tone,new Color(.76f,.76f,.8f),shrine*.4f);
                foreach(var v in Vents)
                {
                    float r=Vector2.Distance(p,v);
                    tone=Color.Lerp(tone,new Color(.74f,.78f,.84f),1-Smooth(2.4f,3.8f,r));
                    tone=Color.Lerp(tone,new Color(.3f,.3f,.32f),1-Smooth(1.4f,2.5f,r+(fine-.5f)*.6f));
                }
                return tone;
            }

            // ---------------------------------------------------------------- what the town is made of
            internal static Material Snow,SnowWall,Bone,Hide,Stone,Rope,Lamp,Fur,Water;
            static void Materials(ZoneKit kit)
            {
                // Snow from about two metres up: wind-cut ridges (sastrugi) running north-east, each lit on its windward side and shadowed in its lee, a crust that breaks into plates,
                // and fine sparkle. Painted near white and tinted in the vertices, so the floor's blue hollows and trodden ways are the tone function's.
                var snow=ZoneKit.Texture("Hoarfell/hoar-snow",1024,(u,v)=>
                {
                    // Whole numbers of ridges across the sheet each way, so a ridge carries on over the tile's edge: at .7 and .3 of 22 every edge was a straight seam.
                    float ridge=Mathf.Repeat(u*15+v*6+.08f*22*ZoneKit.Cloud(u,v,6,3)+ZoneKit.Cloud(u,v,9,5)*2.2f,1);
                    float lit=ZoneKit.Smooth(.0f,.12f,ridge)*(1-ZoneKit.Smooth(.18f,.6f,ridge)),lee=ZoneKit.Smooth(.6f,.96f,ridge)*.6f;
                    float crust=1-Mathf.Abs(2*ZoneKit.Tile(u*10,v*10,10,7)-1),plate=Mathf.Pow(crust,24)*.18f;
                    float sparkle=ZoneKit.Tile(u*400,v*400,400,9)>.985f?.12f:0;
                    float tone=.86f+.1f*lit-.12f*lee-plate+sparkle+.05f*(ZoneKit.Cloud(u,v,4,11)-.5f);
                    return new Color(tone,tone,tone,1);
                });
                Snow=ZoneKit.Toon("hoar-snow",snow,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.6f,.84f));m.SetFloat("_LightFloor",.4f);m.SetTextureScale("_BaseMap",new Vector2(.45f,.45f));});
                // A snow wall: packed snow broken into blocks by fractures that wander (cells, not courses: level courses read as a layered cake), a few blocks of old blue ice
                // showing through, frost along the fractures.
                var wall=ZoneKit.Texture("Hoarfell/hoar-snowwall",512,(u,v)=>
                {
                    float wu=u+.06f*(ZoneKit.Cloud(u,v,5,13)-.5f),wv=v+.06f*(ZoneKit.Cloud(u,v,5,14)-.5f);float best=9,second=9;int cell=0;
                    for(int k=0;k<34;k++){float cx=ZoneKit.Tile(k*3.3f,.5f,64,15),cy=ZoneKit.Tile(.5f,k*2.9f,64,16);
                        for(int ox=-1;ox<=1;ox++)for(int oy=-1;oy<=1;oy++){float d=Mathf.Sqrt((wu-cx-ox)*(wu-cx-ox)+(wv-cy-oy)*(wv-cy-oy)*1.6f);if(d<best){second=best;best=d;cell=k;}else if(d<second)second=d;}}
                    float fracture=second-best,ice=cell%5==0?1:0,shade=.06f*(ZoneKit.Cloud(u,v,10,19)-.5f);
                    float tone=(ice>0?.78f:.9f)+shade-(fracture<.01f?.1f:fracture<.025f?.03f:0)+(fracture<.045f&&fracture>.025f?.03f:0);
                    return ice>0?new Color(tone*.86f,tone*.94f,tone*1.04f,1):new Color(tone,tone*1.01f,tone*1.03f,1);
                });
                SnowWall=ZoneKit.Toon("hoar-snowwall",wall,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.46f,.56f,.8f));m.SetFloat("_LightFloor",.32f);});
                // Bone: old ivory with growth lines along it, pits and fine cracks, darker in its grooves. One bone covers the ribs' feet, posts, the skull and the carvers' pieces.
                var bone=ZoneKit.Texture("Hoarfell/hoar-bone",512,(u,v)=>
                {
                    // Porous ivory: fine pores and pits, a few hairline cracks, weathering in broad soft patches. A grain along it read as wood on the skull's jaw.
                    float pore=ZoneKit.Tile(u*90,v*90,90,23),pit=ZoneKit.Tile(u*36,v*36,36,24)>.95f?.12f:0,crack=1-Mathf.Abs(2*ZoneKit.Tile(u*6,v*8,8,25)-1);
                    float tone=.82f+.06f*(pore-.5f)-pit-.14f*Mathf.Pow(crack,30)+.08f*(ZoneKit.Cloud(u,v,4,27)-.5f);
                    return new Color(tone*1.02f,tone,tone*.9f,1);
                });
                Bone=ZoneKit.Toon("hoar-bone",bone,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.5f,.54f,.72f));m.SetFloat("_RampSteps",4);});
                // Hide: a pelt's hair in tufts lying one way, with the stitching where two skins meet.
                var hide=ZoneKit.Texture("Hoarfell/hoar-hide",512,(u,v)=>
                {
                    float tuft=ZoneKit.Tile(u*40+v*8,v*90,90,31),lie=Mathf.Sin((u*14+v*3+ZoneKit.Cloud(u,v,6,33)*2)*Mathf.PI)*.5f+.5f,seam=Mathf.Abs(Mathf.Repeat(u*3,1)-.5f);
                    float tone=.6f+.18f*(tuft-.5f)+.1f*lie-(seam<.012f?.25f:0);
                    return new Color(tone,tone*.96f,tone*.9f,1);
                });
                Hide=ZoneKit.Toon("hoar-hide",hide,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.5f,.5f,.66f));});
                Fur=ZoneKit.Toon("hoar-fur",hide,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.92f,.9f,.86f));m.SetColor("_ShadowTint",new Color(.5f,.52f,.7f));m.SetFloat("_RampSteps",5);});
                // The vents' stone: dark basalt, wet, flecked.
                var stone=ZoneKit.Texture("Hoarfell/hoar-ventstone",512,(u,v)=>
                {
                    float big=ZoneKit.Cloud(u,v,5,41),fleck=ZoneKit.Tile(u*80,v*80,80,43),crack=1-Mathf.Abs(2*ZoneKit.Tile(u*7,v*7,7,45)-1);
                    float tone=.4f+.16f*(big-.5f)+(fleck>.9f?.1f:0)-.14f*Mathf.Pow(crack,16);return new Color(tone,tone,tone*1.04f,1);
                });
                Stone=ZoneKit.Toon("hoar-ventstone",stone,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.42f,.44f,.6f));m.SetColor("_RimColor",new Color(.7f,.8f,.9f));});
                Rope=ZoneKit.Toon("hoar-rope",null,m=>m.SetColor("_BaseColor",new Color(.62f,.5f,.36f)),"Universal Render Pipeline/Unlit");
                Lamp=ZoneKit.Toon("hoar-lamp-warm",null,m=>{m.SetColor("_BaseColor",new Color(1,.84f,.56f));m.SetColor("_EmissionColor",new Color(2.4f,1.6f,.8f));});
                // The bath's water: clear and green-blue over dark stone, its glints small (chapter 3's rule).
                Water=ZoneKit.Toon("hoar-bathwater",null,m=>ZoneKit.WaterLook(m,new Color(.34f,.52f,.56f,.84f),new Color(.12f,.2f,.24f,.96f),new Color(.86f,.92f,.94f,1),.9f,3.2f,.06f,.3f));
                // Where feet go: snow packed to a dull crust, boot prints and the beasts' split hooves pressed into it. A ground-kit layer of Lastlight's own.
                ZoneKit.Texture("Ground/ground-hoar-tracks",512,(u,v)=>
                {
                    float crust=ZoneKit.Cloud(u,v,8,51),print=0;
                    for(int k=0;k<26;k++){float cx=ZoneKit.Tile(k*2.1f,.5f,32,53),cy=ZoneKit.Tile(.5f,k*1.7f,32,55);float dx=Mathf.Repeat(u-cx+.5f,1)-.5f,dy=Mathf.Repeat(v-cy+.5f,1)-.5f;
                        float ellipse=(dx*dx)/(.012f*.012f)+(dy*dy)/(.03f*.03f);if(ellipse<1)print=Mathf.Max(print,.3f*Mathf.Sqrt(1-ellipse));}
                    float tone=.74f+.1f*(crust-.5f)-print+.05f*(ZoneKit.Tile(u*128,v*128,128,57)-.5f);return new Color(tone,tone,tone*1.02f,1);
                });
            }

            internal static void Build()
            {
                var kit=ZoneKit.Ground(Town,Height,7.2f);
                var scene=WorldBuilder.Begin(kit.zone);
                Materials(kit);
                // The walls in blue shade at their feet, where the floor meets them, and lit toward their tops: a near-white foot left the saw edge between floor and wall at full
                // contrast (QaChapterTwoLookTests.FloorAndWallMeetInShade).
                var land=kit.Terrain("Snow over ice",-80,84,-14,96,.5f,Snow,SnowWall,Tone,new Color(.74f,.8f,.92f),new Color(.6f,.66f,.8f));
                WorldBuilder.Spawn("Arrival",kit.On(Arrival.x,Arrival.y));WorldBuilder.Spawn("Reach",kit.On(ReachSpawn.x,ReachSpawn.y));WorldBuilder.Spawn("Bath",kit.On(BathSpawn.x,BathSpawn.y));

                OnTheSnow("someone with a name",Named);OnTheSnow("a floor sign",Signs.Select(s=>s.at).ToArray());OnTheSnow("a townsperson",Folk.Select(f=>new Vector2(f.x,f.z)).ToArray());
                OnTheSnow("a place to stand",Arrival,Pad,BenchAt,Cradle,BathSpawn,VentSeat,RibTally,LowLamp,ScarsAt,ReachSpawn,MapStand-new Vector2(0,1.6f));

                // ---- the landing: a pad of packed snow ringed with bone posts and hide pennants, and the way back up to the Shear
                var padAt=kit.On(Pad.x,Pad.y);var pad=new ZoneKit.Shape();Color bone=new(.92f,.88f,.78f),dark=new(.62f,.58f,.5f),hideTone=new(.62f,.46f,.34f);
                pad.Slot(0).Disc(padAt+Vector3.up*.06f,Vector3.up,3,28,new Color(.74f,.78f,.86f),0,new Color(.6f,.64f,.72f),.4f).Slot(1).Disc(padAt+Vector3.up*.07f,Vector3.up,2.4f,28,new Color(.98f,.7f,.38f),2.1f);
                for(int k=0;k<6;k++){float a=k*Mathf.PI/3;var q=padAt+new Vector3(Mathf.Cos(a)*3.3f,0,Mathf.Sin(a)*3.3f);pad.Slot(2).Post(q,1.3f,.1f,.06f,6,dark,bone).Slot(3).Triangle(q+Vector3.up*1.25f,q+Vector3.up*.85f,q+Vector3.up*1.05f+new Vector3(Mathf.Cos(a+1.4f)*.6f,0,Mathf.Sin(a+1.4f)*.6f),Vector3.up,new Color(.9f,.56f,.3f),1);}
                var padView=kit.Built("Landing pad",pad,Snow,Lamp,Bone,Hide);
                var launch=new GameObject("Landing pad point",typeof(DockingPad));launch.transform.position=padAt;padView.transform.SetParent(launch.transform,true);
                var dock=launch.GetComponent<DockingPad>();dock.prompt="Launch — The Aurora Shear";dock.range=4;dock.scene=Shear;dock.spawn=Town;

                RibCage(kit);
                Skull(kit);
                Halls(kit);
                Pens(kit);
                Yard(kit);
                Baths(kit);
                VentsAndSteam(kit);
                Lamps(kit);
                Gate(kit);
                for(int i=0;i<Signs.Length;i++)kit.Sign(SignWords[i],Signs[i].at.x,Signs[i].at.y);

                // ---- people
                // Old Tern, seated in three hides by her door under the fourth rib: her body lowered into the hides, so she reads as sitting whatever her clip (she has no seated one).
                var tern=kit.Person("Tern",new Vector3(Tern.x,0,Tern.y),180,"",false,"hoar.sleepLearned","TernAfter","TernFirst");tern.repeatNode="TernRepeat";
                var ternBody=tern.transform.Find("Generated civilian");if(ternBody!=null)ternBody.localPosition=new Vector3(0,-.3f,0);
                TernsSeat(kit,tern.transform);
                var hask=kit.Person("Hask",new Vector3(Hask.x,0,Hask.y),116,"hoar.sleepLearned");hask.postNode="HaskGoes";
                Where("Hask is at the pens until the herders go",hask.gameObject,null,null,null,"chapter6.complete",false);
                var iru=kit.Person("Iru",new Vector3(IruShrine.x,0,IruShrine.y),200,"hoar.sleepLearned",true,"iru.spokeUp","IruRepeat");iru.postNode="IruGoes";
                var iruHall=kit.Person("Iru",new Vector3(IruHall.x,0,IruHall.y),150,"hoar.sleepLearned",true,"iru.spokeUp","IruRepeat");iruHall.postNode="IruGoes";iruHall.name="Iru in Tern's hall";
                var both=new GameObject("Iru stands in Tern's hall",typeof(FlagAll)).GetComponent<FlagAll>();both.flags=new[]{"quest.ShrineLamps.complete","hoar.sleepLearned"};both.result="iru.inTernsHall";
                Where("Iru is at the shrine",iru.gameObject,null,"iru.inTernsHall","iru.spokeUp","chapter6.complete",true);
                Where("Iru is in Tern's hall to speak up",iruHall.gameObject,"iru.inTernsHall",null,null,"iru.spokeUp",true);
                // Idra: arguing with Hask at the pens until that talk (she walks off to the carving yard as it closes), then at the yard until the party takes the Reach, and never
                // once she is in the party.
                var arguing=kit.Townsperson("IdraNatural",new Vector3(IdraArguing.x,0,IdraArguing.y),296,Color.white,"Mira");arguing.name="Idra arguing with Hask";arguing.isStatic=false;
                foreach(var box in arguing.GetComponents<BoxCollider>())box.enabled=false;
                var leaves=arguing.AddComponent<LeavesAfter>();leaves.flag="hoar.pensSeen";leaves.speed=1.6f;leaves.path=new[]{kit.On(-19,28),kit.On(-14,38),kit.On(-12,44.4f)};
                var idra=kit.Person("Idra",new Vector3(IdraYard.x,0,IdraYard.y),180,"",false,null,null,"IdraPlan");idra.repeatNode="IdraRepeat";idra.hideWhenJoined="Idra";
                Where("Idra is at the carving yard",idra.gameObject,"hoar.pensSeen",null,null,"hoar.reachEntered",true);
                Townsfolk(kit);

                // ---- what's said where
                var arrived=kit.Crossing("Sela looks up at the ribs",new Vector3(0,0,12),new Vector3(24,5,3),"hoar.ribsSeen","SelaLastlightArrival","Sela");
                var landed=new GameObject("Landing in Lastlight",typeof(FlagAll)).GetComponent<FlagAll>();landed.flags=new[]{"visited."+Town};landed.result="hoar.arrived";
                var pens=kit.Crossing("Idra and Hask at the pens",new Vector3(PensCrossing.x,0,PensCrossing.y),new Vector3(5,4,10),"hoar.pensSeen","IdraPens","Idra","hoar.ternHeard");_=pens;
                kit.Discovery("The fourth rib",new Vector3(RibTally.x,0,RibTally.y),"Look at the cuts in the rib","hoar.tallySeen","","SelaRibTally","Sela",null,0,2.4f);
                var bath=kit.Discovery("The vent bath",new Vector3(VentSeat.x,0,VentSeat.y),"Sit by the vent","hoar.bathSeen","","SelaVentBath","Sela",null,0,2.4f);bath.requiredFlag="hoar.marshalAsleep";
                var scars=kit.Discovery("The cut seats",new Vector3(ScarsAt.x,0,ScarsAt.y),"Look at the cut seats","hoar.scarsSeen","","SelaClampScars","Taren",null,0,2.6f);scars.requiredFlag="hoar.sleepLearned";
                var tally=kit.Discovery("Idra's brace",new Vector3(TallyAt.x,0,TallyAt.y),"Ask Idra about the scratches","idra.tallyShown","","IdraTally","Idra",null,0,2.4f);tally.requiredFlag="hoar.marshalAsleep";
                // The three high lamps, lit from the low flame: offered while the shrine's quest is at its first step, each a light and a glow that come on with it.
                for(int n=0;n<HighLamps.Length;n++)
                {
                    var at=HighLamps[n];var lamp=kit.Discovery("High lamp "+(n+1),new Vector3(at.x,0,at.y),"Light the high lamp","hoar.skullLamp"+(n+1),"SkullLamp",null,"Sela","ShrineLamps",0,2.4f);lamp.situation="lamp";
                    HighLamp(kit,at,"hoar.skullLamp"+(n+1),n);
                }

                LowFlame(kit);
                // ---- what lives along the edges: hide bundles, bone stacks, sledges, cut ice, lamp-oil jars
                Goods(kit);

                // Every errand's loop clear of anything solid at a body's height: a walker stands still for good at whatever is in its way.
                Physics.SyncTransforms();var blocked=new List<string>();
                foreach(var (_,loop,_,_,_,back,name) in Errands)
                    for(int k=0;k<loop.Length-(back?1:0);k++)
                    {
                        var a=kit.On(loop[k].x,loop[k].y)+Vector3.up*.95f;var next=loop[(k+1)%loop.Length];var b=kit.On(next.x,next.y)+Vector3.up*.95f;var run=b-a;run.y=0;
                        foreach(var hit in Physics.SphereCastAll(a,.4f,run.normalized,run.magnitude,~0,QueryTriggerInteraction.Ignore))
                            if(hit.collider.GetComponentInParent<WalkLoop>()==null)blocked.Add($"{name}: {hit.collider.name} at ({hit.point.x:0.0}, {hit.point.z:0.0})");
                    }
                if(blocked.Count>0)throw new InvalidOperationException("Lastlight: an errand runs into something: "+string.Join("; ",blocked.Distinct()));
                NothingStandsOnTheWays(kit);

                var must=new List<Vector2>{Pad+new Vector2(0,-3.6f),Hask+new Vector2(1.6f,0),IdraYard+new Vector2(0,-1.6f),BenchAt+new Vector2(1.4f,-.8f),TallyAt,Cradle,BathSpawn,VentSeat,Tern+new Vector2(0,-1.8f),
                    RibTally,IruShrine+new Vector2(-1.4f,-.8f),IruHall+new Vector2(0,-1.6f),LowLamp+new Vector2(0,-1.4f),ScarsAt,ReachSpawn,ReachGate+new Vector2(2.4f,0),new Vector2(0,12),PensCrossing};
                must.AddRange(HighLamps);
                kit.BakeNavigation(new Bounds(new Vector3(0,4,42),new Vector3(160,24,110)),Arrival,must);
                TownGround(kit,land);
                var flakes=kit.Glow("Snowfall",false,new Color(1.2f,1.25f,1.35f,1));
                var fall=kit.Drift("Snowfall",new Vector3(0,7,42),new Vector3(150,10,100),flakes,new Color(.95f,.97f,1,.55f),900,.04f,.11f,.2f,14);var fallMain=fall.main;fallMain.gravityModifier=.012f;
                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("HOARFELL_COORDS Lastlight arrival="+Arrival+" pad="+Pad+" hask="+Hask+" idra-pens="+IdraArguing+" pens-crossing="+PensCrossing+" idra-yard="+IdraYard+" bench="+BenchAt+" tally="+TallyAt
                    +" cradle="+Cradle+" bath-spawn="+BathSpawn+" vent-seat="+VentSeat+" tern="+Tern+" rib-tally="+RibTally+" iru="+IruShrine+" iru-hall="+IruHall+" low-lamp="+LowLamp+" scars="+ScarsAt
                    +" lamps="+string.Join(" ",HighLamps.Select(l=>l+"@"+Height(l).ToString("0.0")))+" reach-gate="+ReachGate+" reach-spawn="+ReachSpawn+" ribs="+string.Join(",",Ribs));
                Debug.Log("LASTLIGHT_OK");
            }
            /// <summary>How near a way's line the edge of anything solid may come: the bake's agent is .55 m round and its cells .15, so nearer than this a straight walk along the way is
            /// cut. Someone standing with no name keeps <see cref="FolkClear"/> from it (Lanternwick's rule, 1.6 m, with room to spare). Chapter 3's town's rule.</summary>
            const float WayClear=.9f,FolkClear=2;
            /// <summary>Refuses the finished town if anything solid it stands cuts a way (the town's Ways and the climb's ledge): its footprint (a box's own turned rectangle, otherwise
            /// its bounds) within <see cref="WayClear"/> of a way's line, a townsperson within <see cref="FolkClear"/>, or a walker's loop within 1.6 m. The named stand at the ways'
            /// ends, so they don't count; the landform is the only thing wider than forty metres. Chapter 3's town's check (XylosBuilder.Rootmarket), on this town's ways: the way to
            /// the shrine once turned 0.4 m from the bath-house's wall and the high hall's way ran through a lamp's post, and nothing said so until a route was walked.</summary>
            static void NothingStandsOnTheWays(ZoneKit kit)
            {
                Physics.SyncTransforms();var faults=new List<string>();
                var ways=Ways.Select(w=>w).ToList();ways.Add(Climb.Select(c=>new Vector2(c.x,c.z)).ToArray());
                float Outside(Vector2 p,Vector2 centre,float yaw,Vector2 half)
                {
                    var d=p-centre;float r=yaw*Mathf.Deg2Rad,x=d.x*Mathf.Cos(r)-d.y*Mathf.Sin(r),z=d.x*Mathf.Sin(r)+d.y*Mathf.Cos(r);
                    float ox=Mathf.Abs(x)-half.x,oz=Mathf.Abs(z)-half.y;return new Vector2(Mathf.Max(ox,0),Mathf.Max(oz,0)).magnitude+Mathf.Min(Mathf.Max(ox,oz),0);
                }
                (float gap,Vector2 from,Vector2 to) Nearest(Vector2 centre,float yaw,Vector2 half)
                {
                    var best=(float.PositiveInfinity,Vector2.zero,Vector2.zero);
                    foreach(var way in ways)for(int i=1;i<way.Length;i++)
                    {
                        int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(way[i-1],way[i])/.1f));
                        for(int k=0;k<=steps;k++){float gap=Outside(Vector2.Lerp(way[i-1],way[i],k/(float)steps),centre,yaw,half);if(gap<best.Item1)best=(gap,way[i-1],way[i]);}
                    }
                    return best;
                }
                foreach(var solid in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                {
                    if(!solid.enabled||solid.isTrigger||solid.GetComponentInParent<Npc>()!=null)continue;
                    var bounds=solid.bounds;if(bounds.size.x>40||bounds.size.z>40)continue;
                    var at=new Vector2(bounds.center.x,bounds.center.z);if(bounds.max.y<kit.Y(at.x,at.y)+.32f)continue;
                    bool folk=solid.name=="Townsperson",walker=solid.GetComponentInParent<WalkLoop>()!=null;if(walker)continue;
                    float yaw=0;var half=new Vector2(bounds.extents.x,bounds.extents.z);
                    if(solid is BoxCollider box&&Mathf.Abs(Mathf.DeltaAngle(solid.transform.eulerAngles.x,0))<.5f&&Mathf.Abs(Mathf.DeltaAngle(solid.transform.eulerAngles.z,0))<.5f)
                    {
                        var c=solid.transform.TransformPoint(box.center);at=new Vector2(c.x,c.z);yaw=solid.transform.eulerAngles.y;
                        var size=Vector3.Scale(box.size,solid.transform.lossyScale);half=new Vector2(Mathf.Abs(size.x),Mathf.Abs(size.z))*.5f;
                    }
                    if(folk){half=Vector2.zero;yaw=0;}
                    var (gap,from,to)=Nearest(at,yaw,half);float need=folk?FolkClear:WayClear;
                    if(gap<need)faults.Add($"{(solid.transform.parent!=null?solid.transform.parent.name+"/":"")}{solid.name} at ({at.x:0.0}, {at.y:0.0}) is {gap:0.00} m from the way from {from} to {to} (needs {need})");
                }
                foreach(var walk in UnityEngine.Object.FindObjectsByType<WalkLoop>(FindObjectsSortMode.None))foreach(var p in walk.path)
                {
                    var (gap,from,to)=Nearest(new Vector2(p.x,p.z),0,Vector2.zero);
                    if(gap<1.6f)faults.Add($"{walk.name}'s loop at ({p.x:0.0}, {p.z:0.0}) is {gap:0.00} m from the way from {from} to {to}");
                }
                if(faults.Count>0)throw new InvalidOperationException("Lastlight: things stand on the town's ways:\n"+string.Join("\n",faults.Distinct()));
                Debug.Log("HOARFELL_WAYS_CLEAR Lastlight");
            }
            static void Where(string name,GameObject who,string from,string awayFrom,string backAt,string goneAt,bool live)
            {
                var where=new GameObject(name,typeof(Whereabouts)).GetComponent<Whereabouts>();where.from=from??"";where.awayFrom=awayFrom??"";where.backAt=backAt??"";where.goneAt=goneAt??"";where.live=live;where.targets=new[]{who};
            }
            /// <summary>Refuses a thing that stands on the floor but was put on the climb's rim, in a bank, or in a vent's mouth: a number a metre out is a herder up to the knees in snow.</summary>
            static void OnTheSnow(string what,params Vector2[] spots)
            {
                foreach(var at in spots)
                {
                    float d=Ledge(at,out float top);bool onRim=d>0&&d<RimWide+.3f&&top>.6f;
                    bool inBank=BankTop(at)>0&&Height(at)<BankTop(at)-.2f;
                    bool steep=Mathf.Abs(Height(at+new Vector2(.3f,0))-Height(at-new Vector2(.3f,0)))>.12f||Mathf.Abs(Height(at+new Vector2(0,.3f))-Height(at-new Vector2(0,.3f)))>.12f;
                    if(onRim||inBank||steep)throw new InvalidOperationException($"Lastlight: {what} at {at} stands on a rim, in a bank or on a slope");
                    foreach(var v in Vents)if(Vector2.Distance(at,v)<1.4f)throw new InvalidOperationException($"Lastlight: {what} at {at} stands in a vent's mouth");
                }
            }
            static bool KeepsOff(Vector2 p)
            {
                foreach(var person in Named)if(ZoneKit.InSightOf(p,person,2.6f,4.5f,1.6f))return true;
                if(ZoneKit.OnASign(p,Signs))return true;
                foreach(var (_,x,z,_,_) in Folk)if(Vector2.Distance(p,new Vector2(x,z))<2.2f)return true;
                foreach(var (_,x,z,_,_) in Children)if(Vector2.Distance(p,new Vector2(x,z))<1.8f)return true;
                foreach(var errand in Errands)for(int k=0;k<errand.loop.Length;k++){if(!errand.back||k+1<errand.loop.Length){if(ZoneKit.Segment(p,errand.loop[k],errand.loop[(k+1)%errand.loop.Length])<1.4f)return true;}}
                foreach(var spot in new[]{Arrival,Pad,BenchAt,Cradle,BathSpawn,VentSeat,RibTally,LowLamp,ScarsAt,ReachSpawn,ReachGate,MapStand,TallyAt,IruHall})if(Vector2.Distance(p,spot)<3)return true;
                foreach(var v in Vents)if(Vector2.Distance(p,v)<3.4f)return true;
                foreach(var l in HighLamps)if(Vector2.Distance(p,l)<3)return true;
                if(Ledge(p,out _)<RimWide+1.2f)return true;
                return FromWays(p)<2.2f;
            }

            // ---------------------------------------------------------------- the frozen road
            /// <summary>The ribs: the generated arch at 12 m, turned so it spans north to south, its feet on the ground at z 60 and 76; a box at each foot (an arch's own box would wall
            /// off the hall under it). The first two stand on the high rib-hall's bank. The spine runs along the top of the north wall: a row of vertebrae half sunk in the snow.</summary>
            static void RibCage(ZoneKit kit)
            {
                var root=new GameObject("The ribs");
                for(int i=0;i<Ribs.Length;i++)
                {
                    float x=Ribs[i];// The arch spans along its own z (the Reach gate, unturned, showed it), so a rib stands unturned or turned round; turned 90 its feet stood east and west of
                    // where the foot boxes were.
                    var rib=kit.Prop(ZoneKit.Kit("RibArch","AncelaneArch"),new Vector3(x,0,RibAt),RibTall,i%2==0?0:180,false);rib.name="Rib "+(i+1);rib.transform.SetParent(root.transform,true);
                    ZoneKit.Block(rib,kit.On(x,RibFront,1.6f),new Vector3(2,3.2f,2.2f),0,"Rib foot");ZoneKit.Block(rib,kit.On(x,RibBack,1.6f),new Vector3(2,3.2f,2.2f),0,"Rib foot");
                }
                var spine=new ZoneKit.Shape();var rng=new System.Random(91);
                for(float x=-66;x<64;x+=4.2f)
                {
                    var at=new Vector3(x,Height(new Vector2(x,82))-.6f,82+(float)rng.NextDouble());float size=1.6f+(float)rng.NextDouble()*.5f;
                    spine.Lump(at,new Vector3(size,size*.9f,size*1.2f),rng.Next(999),.08f,new Color(.94f,.9f,.82f),new Color(.6f,.58f,.56f),12,6,.4f);
                    spine.Tube(new[]{at+Vector3.up*size*.6f,at+Vector3.up*(size*1.9f)+Vector3.back*.4f},new[]{size*.32f,size*.12f},7,new Color(.9f,.86f,.78f),new Color(.98f,.96f,.9f),1,.5f);
                }
                kit.Built("The spine",spine,Bone).transform.SetParent(root.transform,true);
            }
            /// <summary>The skull at the east end, the town's landmark: its cranium half in the ice wall, the brow and the empty eye toward the shrine, the long upper jaw and the lower jaw
            /// lying along the shrine floor's east side with their teeth, and in the jaw's side the four seats the clamps were cut into, each as wide as a door.</summary>
            static void Skull(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var blocks=new GameObject("The skull");Color ivory=new(.95f,.92f,.84f),shade=new(.62f,.6f,.62f),hollow=new(.16f,.18f,.24f);
                var cranium=new Vector3(73,5.4f,60);
                s.Slot(0).Lump(cranium,new Vector3(9.5f,7.5f,10.5f),17,.05f,ivory,shade,28,14,.18f,Quaternion.Euler(0,-35,8));
                // The brow: a heavy ridge over the eye, toward the shrine.
                s.Slot(0).Tube(new[]{new Vector3(65.6f,9.4f,63.4f),new Vector3(64.2f,9.6f,58.6f),new Vector3(66.8f,9,53.8f)},new[]{1.2f,1.6f,1.1f},10,ivory,new Color(.98f,.96f,.9f),1,.4f);
                // The eye: a deep hollow with frost at its lip.
                s.Slot(1).Lump(new Vector3(64.6f,7.2f,57.8f),new Vector3(2.6f,2.4f,2.8f),5,.04f,hollow,hollow,16,8);
                s.Slot(0).Tube(Enumerable.Range(0,13).Select(k=>{float a=k*Mathf.PI*2/12;return new Vector3(64.1f,7.2f+Mathf.Sin(a)*2.5f,57.8f+Mathf.Cos(a)*2.6f);}).ToArray(),Enumerable.Repeat(.32f,13).ToArray(),6,new Color(.98f,.99f,1),ivory,1,.5f,false);
                // The upper jaw: long, tapering south-west to the shrine, its teeth hanging; the lower jaw lying under it on the snow.
                // Low along the floor and tapering to a point: raised toward the cranium and cut off flat at its tip, the jaw read as a felled log.
                var jawTop=new[]{new Vector3(67,3.4f,54.8f),new Vector3(64,2.4f,50.6f),new Vector3(61.2f,1.7f,46.6f),new Vector3(59.2f,1.3f,43),new Vector3(58.2f,1.2f,41.2f)};
                s.Slot(0).Tube(jawTop,new[]{2.1f,1.7f,1.25f,.7f,.12f},18,ivory,new Color(.9f,.88f,.82f),1,.9f,false);
                // Snow drifted against the jaw and over the cranium's crown: the skull has lain here longer than anyone has counted.
                foreach(var (c,r) in new[]{(new Vector3(65.6f,.6f,52.8f),new Vector3(2.8f,1.2f,2.4f)),(new Vector3(63.2f,.4f,48.6f),new Vector3(2.2f,.9f,2)),(new Vector3(61.4f,.3f,45),new Vector3(1.6f,.7f,1.4f)),
                    (new Vector3(73,11.6f,61),new Vector3(6.5f,1.6f,7)),(new Vector3(68.6f,8.6f,55.4f),new Vector3(2.6f,.9f,2.4f))})
                    s.Slot(3).Lump(c,r,(int)(c.x*7+c.z),.12f,new Color(.96f,.98f,1),new Color(.74f,.8f,.92f),14,6);
                var jawLow=new[]{new Vector3(67.6f,.5f,53.4f),new Vector3(64.8f,.45f,49.2f),new Vector3(62.2f,.4f,45.2f),new Vector3(60.6f,.35f,41.8f),new Vector3(59.8f,.3f,40.4f)};
                s.Slot(0).Tube(jawLow,new[]{1f,.85f,.7f,.4f,.08f},14,new Color(.86f,.84f,.78f),ivory,1,.9f,false);
                for(int k=0;k<12;k++)
                {
                    float t=k/11f;var top=Vector3.Lerp(jawTop[0],jawTop[3],t);var low=Vector3.Lerp(jawLow[0],jawLow[3],t);var west=new Vector3(-.8f,0,.55f);float r=Mathf.Lerp(2,.8f,t);
                    s.Slot(0).Post(top+west*r*.45f+Vector3.down*r*.6f,-(.8f+.4f*(k%3)),.2f,.03f,7,ivory,new Color(1,.99f,.95f));
                    s.Slot(0).Post(low+west*.6f+Vector3.up*.5f,.7f+.3f*((k+1)%3),.18f,.03f,7,ivory,new Color(1,.99f,.95f));
                }
                // The four cut seats in the upper jaw's west side, facing the shrine: door-wide notches, their cut faces paler and frost-rimmed.
                var seats=new List<Vector3>();
                for(int k=0;k<4;k++)
                {
                    var at=Vector3.Lerp(new Vector3(59.2f,0,45.6f),new Vector3(63,0,50.4f),k/3f);at.y=.3f;var turn=Quaternion.Euler(0,-38,0);
                    s.Slot(2).Box(at+turn*new Vector3(0,.2f,.2f),new Vector3(1.1f,.5f,1.2f),turn,new Color(.98f,.96f,.92f),new Color(1,1,1),.6f).Box(at+turn*new Vector3(0,1.1f,.7f),new Vector3(1.1f,2.2f,.25f),turn,new Color(.9f,.88f,.84f),null,.6f);
                    seats.Add(at+Vector3.up*.6f);
                }
                var view=kit.Built("The skull",s,Bone,Stone,Bone,Snow);view.transform.SetParent(blocks.transform,true);ZoneKit.Fadeable(view);
                foreach(var seat in seats)ZoneKit.Block(view,seat,new Vector3(1.4f,1.4f,1.6f),-38,"Cut seat");
                for(int i=1;i<jawLow.Length-1;i++)ZoneKit.BlockAlong(view,jawLow[i-1],jawLow[i],2.6f,2.4f,"Jaw");
                ZoneKit.Block(view,cranium,new Vector3(14,10,16),-35,"Cranium");
            }
            /// <summary>The rib-halls' longhouses (the generated longhouse, its long side along the hall), Tern's among them; between them a rack of hides drying, a beast shelter, the
            /// carvers' store.</summary>
            static void Halls(ZoneKit kit)
            {
                var root=new GameObject("Longhouses");
                foreach(var at in Longhouses)
                {
                    var house=kit.Prop(ZoneKit.Kit("KeptLonghouse","CanyonHouse"),new Vector3(at.x,0,at.y),Tall("KeptLonghouse",10),90,true);house.transform.SetParent(root.transform,true);house.name=at==TernHall?"Tern's hall":"Longhouse";
                    kit.Lamp(new Vector3(at.x+1.2f,1.9f,at.y-5.8f),6,.9f,new Color(1,.74f,.46f));
                }
                // Hides drying on frames in the hall between the fifth and sixth ribs; a lean-to for beasts between the seventh and eighth.
                var racks=new ZoneKit.Shape();Color bone=new(.9f,.86f,.76f),dark=new(.6f,.56f,.5f);
                for(int r=0;r<3;r++)
                {
                    float z=63+r*3.6f;var a=kit.On(-7,z);var b=kit.On(-1,z);
                    racks.Slot(0).Post(a,2.6f,.1f,.08f,6,dark,bone).Post(b,2.6f,.1f,.08f,6,dark,bone).Beam(a+Vector3.up*2.5f,b+Vector3.up*2.5f,.12f,.12f,bone);
                    for(int k=0;k<3;k++){var c=Vector3.Lerp(a,b,.2f+k*.3f)+Vector3.up*2.4f;Color pelt=k%2==0?new(.66f,.5f,.36f):new(.86f,.82f,.74f);
                        racks.Slot(1).Quad(c+new Vector3(-.8f,0,0),c+new Vector3(.8f,0,0),c+new Vector3(.7f,-1.5f,.1f),c+new Vector3(-.7f,-1.5f,.1f),Vector3.back,pelt,.6f);}
                }
                var rackView=kit.Built("Hides drying",racks,Bone,Hide);ZoneKit.Block(rackView,kit.On(-4,66.6f,1.2f),new Vector3(7,2.4f,8),0,"Hide racks");
                var shelter=new ZoneKit.Shape();
                for(int k=0;k<4;k++){var foot=kit.On(-32+k*2.6f,62);shelter.Slot(0).Post(foot,2.4f,.12f,.1f,6,dark,bone);}
                shelter.Slot(1).Quad(kit.On(-32.4f,61.6f,2.5f),kit.On(-23.8f,61.6f,2.5f),kit.On(-23.8f,66,1.4f),kit.On(-32.4f,66,1.4f),Vector3.up,new Color(.6f,.46f,.34f),.4f);
                var shelterView=kit.Built("Beast shelter",shelter,Bone,Hide);ZoneKit.Block(shelterView,kit.On(-28,64,1.1f),new Vector3(9,2.2f,4.6f),0,"Shelter");
                var store=new ZoneKit.Shape();
                for(int k=0;k<5;k++){var foot=kit.On(-55+(k%3)*1.6f,63+(k/3)*2.4f);store.Slot(0).Box(foot+Vector3.up*.5f,new Vector3(1.3f,1,2.2f),Quaternion.Euler(0,k*7,0),dark,bone,.6f);}
                var storeView=kit.Built("Carvers' store",store,Bone);ZoneKit.Block(storeView,kit.On(-53.4f,64.2f,.5f),new Vector3(5,1,5),0,"Store");
            }
            /// <summary>Tern's seat: three hides heaped round her, a fur over her knees, her counting-stick, a brazier at her side and the hall's door behind her.</summary>
            static void TernsSeat(ZoneKit kit,Transform tern)
            {
                var at=kit.On(Tern.x,Tern.y);var s=new ZoneKit.Shape();
                // The hides she sits on, heaped behind and beside her, and a small fur over her lap (built below, under her own object): in front of her, the heap hid her and she
                // read as a white lump in a pile.
                var brazier=at+new Vector3(1.3f,0,-.2f);
                s.Slot(1).Post(brazier,.55f,.08f,.06f,6,new Color(.5f,.48f,.46f),new Color(.6f,.58f,.56f)).Tube(new[]{brazier+Vector3.up*.55f,brazier+Vector3.up*.8f},new[]{.25f,.4f},10,new Color(.36f,.34f,.34f),new Color(.44f,.42f,.42f),1,1,false);
                s.Slot(2).Lump(brazier+Vector3.up*.8f,new Vector3(.3f,.12f,.3f),2,.3f,Color.white,Color.white,8,4);
                // The hides are hers, under her own object (a seat she sits in counted as a prop she stood inside); the brazier beside her is its own.
                var view=kit.Built("Tern's seat",s,Fur,Stone,Lamp);
                var hides=new ZoneKit.Shape();
                hides.Lump(at+new Vector3(0,.16f,.35f),new Vector3(.8f,.3f,.7f),3,.18f,new Color(.66f,.5f,.38f),new Color(.42f,.32f,.26f),16,6)
                    .Lump(at+new Vector3(-.62f,.26f,.62f),new Vector3(.6f,.44f,.55f),5,.2f,new Color(.84f,.8f,.72f),new Color(.56f,.52f,.48f),14,6)
                    .Lump(at+new Vector3(.66f,.24f,.6f),new Vector3(.55f,.4f,.55f),7,.2f,new Color(.5f,.42f,.36f),new Color(.32f,.28f,.26f),14,6)
                    .Lump(at+new Vector3(0,.34f,-.12f),new Vector3(.34f,.1f,.24f),9,.12f,new Color(.9f,.88f,.82f),new Color(.6f,.58f,.56f),12,5);
                var heap=kit.Built("Tern's hides",hides,Fur);heap.transform.SetParent(tern,true);heap.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.On;
                ZoneKit.Block(view,at+new Vector3(1.3f,.45f,-.2f),new Vector3(.7f,.9f,.7f),0,"Brazier");
                kit.Lamp(brazier+Vector3.up*1.1f,5,1.2f,new Color(1,.62f,.32f));
                var smoke=kit.Drift("Brazier smoke",brazier+Vector3.up*1.2f,new Vector3(.3f,.2f,.3f),kit.Glow("Brazier smoke",false,new Color(.7f,.7f,.74f,1)),new Color(.8f,.8f,.84f,.3f),30,.3f,.8f,.5f,4);
                var main=smoke.main;main.startSpeed=.6f;var shape=smoke.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=8;smoke.transform.rotation=Quaternion.Euler(-90,0,0);
                // The tally on the fourth rib's foot: a plaque of bone at its base cut with eleven rows of marks, the last row stopping halfway.
                var tally=new ZoneKit.Shape();var plate=kit.On(13.1f,59.2f,1.1f);var turn=Quaternion.Euler(0,-20,0);
                tally.Slot(0).Box(plate,new Vector3(1.4f,1.6f,.12f),turn,new Color(.9f,.86f,.78f),null,.6f);
                for(int row=0;row<11;row++)for(int k=0;k<(row==10?7:14);k++)tally.Slot(1).Box(plate+turn*new Vector3(-.6f+k*.09f,.68f-row*.13f,-.07f),new Vector3(.02f,.09f,.01f),turn,new Color(.2f,.18f,.18f));
                kit.Built("The tally on the fourth rib",tally,Bone,Stone);
            }
            /// <summary>The beast pens on the west: a fence of bone posts and hide ropes round a trampled yard, its gate open on the east, the herd inside, two beasts being readied by the
            /// Reach gate. The beasts are the generated rime-beast, each its own size and turn.</summary>
            static void Pens(ZoneKit kit)
            {
                var fence=new ZoneKit.Shape();var posts=new List<Vector2>();Color bone=new(.9f,.86f,.76f),dark=new(.6f,.56f,.5f);
                void Side(Vector2 a,Vector2 b,float gapFrom=-1,float gapTo=-1)
                {
                    float length=Vector2.Distance(a,b);int n=Mathf.CeilToInt(length/2.4f);Vector2? last=null;
                    for(int k=0;k<=n;k++)
                    {
                        var p=Vector2.Lerp(a,b,k/(float)n);bool gap=gapFrom>=0&&p.y>gapFrom&&p.y<gapTo;
                        if(gap){last=null;continue;}
                        var foot=kit.On(p.x,p.y);fence.Slot(0).Post(foot,1.5f,.11f,.08f,6,dark,bone);posts.Add(p);
                        if(last is Vector2 l){var from=kit.On(l.x,l.y);fence.Slot(1).Rope(from+Vector3.up*1.2f,foot+Vector3.up*1.2f,.12f,.04f,new Color(.56f,.42f,.3f)).Rope(from+Vector3.up*.65f,foot+Vector3.up*.65f,.1f,.04f,new Color(.56f,.42f,.3f));}
                        last=p;
                    }
                }
                Side(PenMin,new Vector2(PenMax.x,PenMin.y));Side(new Vector2(PenMax.x,PenMin.y),PenMax,21.6f,27.4f);Side(PenMax,new Vector2(PenMin.x,PenMax.y));Side(new Vector2(PenMin.x,PenMax.y),PenMin);
                var view=kit.Built("Pen fence",fence,Bone,Rope);
                foreach(var p in posts)ZoneKit.Block(view,kit.On(p.x,p.y,.75f),new Vector3(.3f,1.5f,.3f),0,"Fence post");
                // Between the posts, a rail a beast can't step over (the ropes are drawn only).
                foreach(var (a,b) in new[]{(PenMin,new Vector2(PenMax.x,PenMin.y)),(new Vector2(PenMax.x,PenMin.y),new Vector2(PenMax.x,21.6f)),(new Vector2(PenMax.x,27.4f),PenMax),(PenMax,new Vector2(PenMin.x,PenMax.y)),(new Vector2(PenMin.x,PenMax.y),PenMin)})
                    ZoneKit.BlockAlong(view,kit.On(a.x,a.y,.6f),kit.On(b.x,b.y,.6f),.2f,1.2f,"Fence rail");
                var herd=new GameObject("The herd");
                foreach(var (x,z,yaw,size) in Beasts){var beast=kit.Prop(ZoneKit.Kit("RimeBeast","DressBoulders"),new Vector3(x,0,z),Tall("RimeBeast",size),yaw,true);beast.name="Rime-beast";beast.transform.SetParent(herd.transform,true);}
                // Fodder: a rack of dried moss and lichen bales inside the gate, and the trough by it.
                var fodder=new ZoneKit.Shape();
                for(int k=0;k<5;k++){var foot=kit.On(-31.6f,16+k*1.1f);fodder.Slot(0).Lump(foot+Vector3.up*.35f,new Vector3(.5f,.36f,.45f),k,.25f,new Color(.56f,.6f,.36f),new Color(.36f,.38f,.24f),10,5);}
                fodder.Slot(1).Box(kit.On(-31.4f,30,.3f),new Vector3(.8f,.5f,3),Quaternion.identity,new Color(.6f,.56f,.5f),new Color(.86f,.82f,.74f),.6f);
                var fodderView=kit.Built("Fodder and trough",fodder,Hide,Bone);ZoneKit.Block(fodderView,kit.On(-31.6f,18.2f,.4f),new Vector3(1,.8f,5.6f),0,"Fodder");ZoneKit.Block(fodderView,kit.On(-31.4f,30,.3f),new Vector3(.8f,.6f,3),0,"Trough");
            }
            /// <summary>The carving yard: rib sections lying on trestles, a carving horse with a half-shaped plate on it, chips and shavings, and the herders' bone map on its stand, a
            /// flat of bone cut with the dig's three levels; the bench (the carvers' tools) on the yard's west side.</summary>
            static void Yard(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var blocks=new GameObject("Carving yard");Color bone=new(.92f,.88f,.78f),dark=new(.6f,.56f,.5f),wood=new(.5f,.44f,.38f);
                // The map: a slab tilted toward the yard on two posts, its levels cut as dark lines, three marks where the couplings are.
                var mapFoot=kit.On(MapStand.x,MapStand.y);var tilt=Quaternion.Euler(-38,0,0);
                s.Slot(0).Post(mapFoot+new Vector3(-1,0,.3f),1.2f,.09f,.08f,6,dark,bone).Post(mapFoot+new Vector3(1,0,.3f),1.2f,.09f,.08f,6,dark,bone).Box(mapFoot+Vector3.up*1.2f,new Vector3(2.6f,1.7f,.14f),tilt,bone,null,.6f);
                for(int k=0;k<3;k++){var row=mapFoot+Vector3.up*1.2f+tilt*new Vector3(0,.5f-k*.45f,-.08f);s.Slot(1).Box(row,new Vector3(2-.3f*k,.04f,.02f),tilt,new Color(.22f,.2f,.2f)).Lump(row+tilt*new Vector3(.6f-k*.4f,0,-.02f),new Vector3(.07f,.07f,.04f),k,.05f,new Color(.9f,.4f,.22f),new Color(.9f,.4f,.22f),6,4);}
                var boxes=new List<(Vector3 at,Vector3 size,float yaw,string name)>{(mapFoot+Vector3.up*.9f,new Vector3(2.6f,1.8f,.6f),0,"Bone map")};
                // Rib sections on trestles, east and west of the yard.
                foreach(var (x,z,yaw) in new[]{(-17.4f,50.6f,10f),(-6.6f,51,-14f)})
                {
                    var c=kit.On(x,z);var t=Quaternion.Euler(0,yaw,0);
                    foreach(int end in new[]{-1,1})s.Slot(0).Post(c+t*new Vector3(end*1.2f,0,0),.7f,.08f,.07f,5,dark,wood);
                    s.Slot(0).Tube(new[]{c+t*new Vector3(-1.8f,.85f,0),c+t*new Vector3(0,1.05f,.2f),c+t*new Vector3(1.8f,.85f,0)},new[]{.32f,.38f,.3f},9,bone,new Color(.98f,.95f,.88f),1,.4f);
                    boxes.Add((c+Vector3.up*.6f,new Vector3(4,1.2f,1),yaw,"Trestle"));
                }
                // The carvers' bench: a long low bench with tools laid out, a vice of bone, a basket of chips.
                var bench=kit.On(BenchAt.x-1.2f,BenchAt.y+.4f);
                s.Slot(0).Box(bench+Vector3.up*.45f,new Vector3(.9f,.9f,2.4f),Quaternion.identity,wood,new Color(.62f,.56f,.48f),.6f);
                for(int k=0;k<5;k++)s.Slot(1).Box(bench+new Vector3(.1f,.92f,-.9f+k*.42f),new Vector3(.08f,.03f,.3f),Quaternion.Euler(0,k*14,0),new Color(.5f,.5f,.54f));
                boxes.Add((bench+Vector3.up*.45f,new Vector3(.9f,.9f,2.4f),0,"Carving bench"));
                // Boxes under what is drawn, made after it: made first, they hung under an empty holder and read as invisible walls.
                var view=kit.Built("Carving yard",s,Bone,Stone);view.transform.SetParent(blocks.transform,true);
                foreach(var (at,size,yaw,name) in boxes)ZoneKit.Block(view,at,size,yaw,name);
                var point=new GameObject("The carving yard's bench",typeof(Bench)).GetComponent<Bench>();point.transform.position=kit.On(BenchAt.x,BenchAt.y);point.prompt="Fabricate — The Carving Yard";point.range=2.6f;
            }
            /// <summary>The vent bath-house: a longhouse over the warmest vent, steam rolling out of its smoke hole all day, and in front of it the basin where a hurt hunter is put to
            /// soak (the repair cradle: repair and save).</summary>
            static void Baths(ZoneKit kit)
            {
                var house=kit.Prop(ZoneKit.Kit("KeptLonghouse","CanyonHouse"),new Vector3(BathHouse.x,0,BathHouse.y),Tall("KeptLonghouse",10),0,true);house.name="Vent bath-house";
                var steam=kit.Drift("Bath-house steam",new Vector3(BathHouse.x,5.2f,BathHouse.y),new Vector3(1.2f,.4f,1.2f),kit.Glow("Steam",false,new Color(.86f,.9f,.96f,1)),new Color(.9f,.93f,1f,.32f),90,1,2.6f,.9f,6);
                var main=steam.main;main.startSpeed=1.2f;var shape=steam.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=16;steam.transform.rotation=Quaternion.Euler(-80,40,0);
                var basinAt=kit.On(Cradle.x+3,Cradle.y-.2f);var s=new ZoneKit.Shape();
                s.Slot(0).Tube(Enumerable.Range(0,17).Select(k=>{float a=k*Mathf.PI*2/16;return basinAt+new Vector3(Mathf.Cos(a)*1.9f,.18f,Mathf.Sin(a)*1.3f);}).ToArray(),Enumerable.Repeat(.32f,17).ToArray(),7,new Color(.42f,.42f,.46f),new Color(.56f,.56f,.6f),1,.6f,false);
                s.Slot(1).Disc(basinAt+Vector3.up*.22f,Vector3.up,1.75f,24,new Color(.5f,.66f,.7f),0,new Color(.4f,.54f,.6f));
                var basin=kit.Built("The soaking basin",s,Stone,Water);ZoneKit.Block(basin,basinAt+Vector3.up*.25f,new Vector3(4,.5f,2.8f),0,"Basin");
                var soak=kit.Drift("Basin steam",basinAt+Vector3.up*.5f,new Vector3(3,.2f,2),kit.Glow("Basin steam",false,new Color(.86f,.9f,.96f,1)),new Color(.9f,.93f,1f,.22f),40,.5f,1.4f,.3f,5);_=soak;
                var repair=new GameObject("Vent bath-house cradle",typeof(RepairBay)).GetComponent<RepairBay>();repair.transform.position=kit.On(Cradle.x,Cradle.y);
                repair.prompt="Vent bath-house — repair and save";repair.saveSpawn="Bath";repair.range=2.8f;
                kit.Lamp(new Vector3(Cradle.x+3,1.6f,Cradle.y-1.6f),6,1,new Color(1,.74f,.46f));
            }
            /// <summary>The vents: a mouth of dark stone in each, steam always rising, a warm light under the steam.</summary>
            static void VentsAndSteam(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var glow=kit.Glow("Vent steam",false,new Color(.86f,.9f,.96f,1));
                foreach(var v in Vents)
                {
                    var at=kit.On(v.x,v.y);int seed=(int)(v.x*13+v.y*7);
                    // Stones heaped round the mouth, unevenly, the mouth itself dark, frost on the outer stones. A ring of seven alike, lit warm, read as a cookie.
                    var rng=new System.Random(seed);
                    for(int k=0;k<11;k++){float a=k*.57f+(float)rng.NextDouble()*.5f,r=.9f+(float)rng.NextDouble()*.9f;var stone=at+new Vector3(Mathf.Cos(a)*r,.05f,Mathf.Sin(a)*r);float size=.25f+(float)rng.NextDouble()*.35f;
                        s.Slot(0).Lump(stone,new Vector3(size*1.3f,size*.7f,size),seed+k,.3f,r>1.4f?new Color(.82f,.86f,.92f):new Color(.46f,.47f,.52f),new Color(.26f,.27f,.31f),9,5);}
                    s.Slot(1).Disc(at+Vector3.up*.03f,Vector3.up,.6f,14,new Color(.06f,.07f,.09f),0,new Color(.16f,.17f,.2f));
                    var steam=kit.Drift("Vent steam",at+Vector3.up*.3f,new Vector3(.8f,.2f,.8f),glow,new Color(.92f,.95f,1f,.26f),46,.6f,1.8f,.7f,5);
                    var main=steam.main;main.startSpeed=.8f;var shape=steam.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=10;steam.transform.rotation=Quaternion.Euler(-88,0,0);
                }
                kit.Built("Vent stones",s,Stone,Stone).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            /// <summary>Lamp posts of bone along the ways, each a horn lamp hung from a crook; one in three gives a light.</summary>
            static void Lamps(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var blocks=new GameObject("Lamp posts");int n=0;var posts=new List<Vector3>();
                foreach(var p in new[]{new Vector2(-4.2f,14.4f),new Vector2(5.4f,14.6f),new Vector2(-10.6f,30.6f),new Vector2(10.6f,33.4f),new Vector2(-21.6f,38.6f),new Vector2(1.2f,52.4f),new Vector2(15.8f,50.8f),
                    new Vector2(-34.4f,38.8f),new Vector2(24.6f,52),new Vector2(40.6f,37.6f),new Vector2(53,37.4f),new Vector2(-51.6f,38.4f),new Vector2(-26.8f,11.6f),new Vector2(19,26.4f)})
                {
                    if(FromWays(p)<1.6f)throw new InvalidOperationException("Lastlight: a lamp post at "+p+" stands on a way");
                    var foot=kit.On(p.x,p.y);
                    s.Slot(0).Post(foot,2.8f,.09f,.07f,6,new Color(.62f,.58f,.52f),new Color(.92f,.88f,.8f)).Tube(new[]{foot+Vector3.up*2.7f,foot+new Vector3(.25f,3,0),foot+new Vector3(.6f,2.95f,0)},new[]{.06f,.05f,.045f},5,new Color(.9f,.86f,.78f),new Color(.9f,.86f,.78f),1,.5f);
                    s.Slot(1).Lump(foot+new Vector3(.62f,2.6f,0),new Vector3(.16f,.22f,.16f),n,.05f,Color.white,Color.white,8,5);
                    posts.Add(foot+Vector3.up*1.4f);
                    if(n%3==0)kit.Lamp(foot+new Vector3(.62f,2.4f,0),7,1.1f,new Color(1,.76f,.48f));
                    n++;
                }
                var view=kit.Built("Lamp posts",s,Bone,Lamp);view.transform.SetParent(blocks.transform,true);
                foreach(var post in posts)ZoneKit.Block(view,post,new Vector3(.28f,2.8f,.28f),0,"Lamp post");
            }
            /// <summary>A high lamp: a horn lamp on a bone crook at a platform's back, dark until it is lit, and its light then.</summary>
            static void HighLamp(ZoneKit kit,Vector2 at,string flag,int n)
            {
                // Behind each lamp's spot and off its ways: the second's, north-east of its spot, stood on the way on to the knoll.
                var foot=n==1?kit.On(at.x-1.2f,at.y+1):kit.On(at.x+(n==0?0:.4f),at.y+1.3f);var s=new ZoneKit.Shape();
                s.Slot(0).Post(foot,2.2f,.1f,.08f,6,new Color(.62f,.58f,.52f),new Color(.92f,.88f,.8f)).Tube(new[]{foot+Vector3.up*2.1f,foot+new Vector3(0,2.4f,-.3f),foot+new Vector3(0,2.3f,-.6f)},new[]{.07f,.06f,.05f},5,new Color(.9f,.86f,.78f),new Color(.9f,.86f,.78f),1,.5f);
                var post=kit.Built("High lamp "+(n+1)+" post",s,Bone);ZoneKit.Block(post,foot+Vector3.up*1.1f,new Vector3(.3f,2.2f,.3f),0,"High lamp post");
                var dark=kit.Built("High lamp "+(n+1)+" unlit",new ZoneKit.Shape().Lump(foot+new Vector3(0,2,-.62f),new Vector3(.2f,.28f,.2f),n,.05f,new Color(.4f,.36f,.32f),new Color(.3f,.28f,.26f),8,5),Bone);
                var lit=kit.Built("High lamp "+(n+1)+" lit",new ZoneKit.Shape().Lump(foot+new Vector3(0,2,-.62f),new Vector3(.2f,.28f,.2f),n,.05f,Color.white,Color.white,8,5),Lamp);
                var light=kit.Lamp(foot+new Vector3(0,1.8f,-.62f),9,1.6f,new Color(1,.76f,.46f));light.transform.SetParent(lit.transform,true);
                var show=new GameObject("High lamp "+(n+1)+" lights",typeof(FlagGate)).GetComponent<FlagGate>();show.flag=flag;show.showWhenSet=true;show.targets=new[]{lit};
                var hide=new GameObject("High lamp "+(n+1)+" was dark",typeof(FlagGate)).GetComponent<FlagGate>();hide.flag=flag;hide.showWhenSet=false;hide.targets=new[]{dark};
            }
            /// <summary>The shrine's low lamp, the flame the high ones are lit from: a broad bowl of bone on a carved stand, always burning.</summary>
            static void LowFlame(ZoneKit kit)
            {
                var foot=kit.On(LowLamp.x,LowLamp.y);var s=new ZoneKit.Shape();
                s.Slot(0).Post(foot,.9f,.18f,.1f,8,new Color(.66f,.62f,.56f),new Color(.92f,.88f,.8f)).Tube(new[]{foot+Vector3.up*.9f,foot+Vector3.up*1.15f},new[]{.32f,.55f},14,new Color(.86f,.82f,.74f),new Color(.96f,.94f,.88f),1,1,false);
                s.Slot(1).Lump(foot+Vector3.up*1.18f,new Vector3(.34f,.2f,.34f),3,.3f,Color.white,Color.white,10,5);
                // The shrine floor round it: small lamps on bone stands in an arc behind it (the ones Iru keeps lit at the turn of the night, dark by day), and two hide mats where
                // the keepers kneel. A bare floor of snow measured at the limit of plain ground.
                for(int k=0;k<5;k++)
                {
                    float a=(30+k*30)*Mathf.Deg2Rad;var stand=kit.On(LowLamp.x+Mathf.Cos(a)*2.6f,LowLamp.y+Mathf.Sin(a)*2.6f);
                    s.Slot(0).Post(stand,.7f+.15f*(k%2),.06f,.05f,6,new Color(.7f,.66f,.6f),new Color(.94f,.9f,.82f)).Tube(new[]{stand+Vector3.up*(.7f+.15f*(k%2)),stand+Vector3.up*(.82f+.15f*(k%2))},new[]{.1f,.16f},9,new Color(.86f,.82f,.74f),new Color(.96f,.94f,.88f),1,1,false);
                }
                // Pelts where the keepers kneel, each its own shape and colour: squares of hide read as boards.
                foreach(var (x,z,seed) in new[]{(51.5f,41.5f,3),(45.5f,40.6f,7)})
                    s.Slot(2).Disc(kit.On(x,z,.03f),Vector3.up,.85f+.1f*(seed%3),9,seed%2==0?new Color(.84f,.8f,.72f):new Color(.62f,.48f,.36f),0,seed%2==0?new Color(.7f,.66f,.6f):new Color(.46f,.36f,.28f),.6f);
                // Three carved posts east of the low lamp, hung with strips of hide and bone charms that knock together in the wind: the shrine's own marker.
                foreach(var (x,z,tall) in new[]{(53.4f,49.6f,2.9f),(54.8f,48.4f,2.3f),(52.4f,51,2.5f)})
                {
                    var postFoot=kit.On(x,z);s.Slot(0).Post(postFoot,tall,.14f,.09f,7,new Color(.7f,.66f,.6f),new Color(.96f,.92f,.84f));
                    for(int k=0;k<4;k++){float a=k*1.57f+x;var hang=postFoot+Vector3.up*(tall-.15f)+new Vector3(Mathf.Cos(a)*.16f,0,Mathf.Sin(a)*.16f);
                        s.Slot(2).Quad(hang+new Vector3(-.05f,0,0),hang+new Vector3(.05f,0,0),hang+new Vector3(.06f,-.7f-.2f*(k%2),.02f),hang+new Vector3(-.06f,-.7f-.2f*(k%2),.02f),new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),k%2==0?new Color(.62f,.46f,.34f):new Color(.9f,.4f,.24f),.6f)
                            .Slot(0).Lump(hang+new Vector3(0,-.85f-.2f*(k%2),0),new Vector3(.06f,.09f,.06f),k,.1f,new Color(.96f,.94f,.88f),new Color(.7f,.66f,.6f),6,4);}
                }
                var view=kit.Built("The low lamp",s,Bone,Lamp,Fur);
                foreach(var (x,z,_) in new[]{(53.4f,49.6f,0f),(54.8f,48.4f,0f),(52.4f,51,0f)})ZoneKit.Block(view,kit.On(x,z,1.2f),new Vector3(.36f,2.4f,.36f),0,"Carved post");ZoneKit.Block(view,foot+Vector3.up*.6f,new Vector3(.8f,1.2f,.8f),0,"Low lamp");
                ZoneKit.Block(view,foot+new Vector3(0,.45f,1.6f),new Vector3(5.4f,.9f,1.6f),0,"Lamp stands");
                kit.Lamp(foot+Vector3.up*1.7f,8,1.8f,new Color(1,.7f,.4f));
            }
            /// <summary>The Reach gate on the west: two great tusks crossed over the way out, hung with charms, and the drift behind it.</summary>
            static void Gate(ZoneKit kit)
            {
                var arch=kit.Prop(ZoneKit.Kit("RibArch","AncelaneArch"),new Vector3(ReachGate.x,0,ReachGate.y),6.6f,0,false);arch.name="Reach gate";
                var holder=new GameObject("Reach gate way");holder.transform.position=kit.On(ReachGate.x,ReachGate.y);arch.transform.SetParent(holder.transform,true);
                ZoneKit.Block(arch,kit.On(ReachGate.x,ReachGate.y-4.2f,1.4f),new Vector3(1.6f,2.8f,1.6f),0,"Gate foot");ZoneKit.Block(arch,kit.On(ReachGate.x,ReachGate.y+4.2f,1.4f),new Vector3(1.6f,2.8f,1.6f),0,"Gate foot");
                kit.Way(holder,"Reach gate — The Frozen Host",Host,"Arrival","hoar.planTaken","The Reach gate stays shut until there's a plan: hear Idra Sorn's at the carving yard",4.4f,null);
                var charms=new ZoneKit.Shape();
                for(int k=0;k<6;k++){var top=kit.On(ReachGate.x,ReachGate.y-3+k*1.2f,4.6f+.4f*Mathf.Sin(k));charms.Slot(0).Rope(top+Vector3.up*.9f,top,.05f,.025f,new Color(.56f,.42f,.3f)).Slot(1).Lump(top,new Vector3(.12f,.18f,.12f),k,.1f,new Color(.94f,.9f,.82f),new Color(.7f,.66f,.6f),7,4);}
                // Under the arch they hang from: as a mesh of their own they stood four metres off the ground (QaPlanWorldTests.NothingFloatsOrSinksIn).
                kit.Built("Gate charms",charms,Rope,Bone).transform.SetParent(arch.transform,true);
            }
            /// <summary>The Kept: standing at their work, children by a vent, and seven on errands, each carrying what the errand is about.</summary>
            static void Townsfolk(ZoneKit kit)
            {
                kit.Townsfolk(Folk.Select(f=>(Coat(f.body),f.x,f.z,f.yaw,f.tint)));
                foreach(var (body,x,z,yaw,tint) in Children){var child=kit.Townsperson(Coat(body),new Vector3(x,0,z),yaw,tint);child.transform.localScale=Vector3.one*.72f;child.name="A child by the vent";}
                foreach(var (body,loop,speed,pause,tint,back,name) in Errands)
                {
                    var walker=kit.Walker(Coat(body),loop,speed,pause,tint);walker.thereAndBack=back;walker.name=name;
                    if(name.StartsWith("A child"))walker.transform.localScale=Vector3.one*.72f;
                    ZoneKit.Shape load=null;Material[] stuff=null;
                    if(name.Contains("ladder")){load=new ZoneKit.Shape().Slot(0).Beam(new Vector3(.3f,1.55f,-1.8f),new Vector3(.3f,1.55f,1.8f),.06f,.06f,new Color(.9f,.86f,.78f)).Beam(new Vector3(-.15f,1.55f,-1.8f),new Vector3(-.15f,1.55f,1.8f),.06f,.06f,new Color(.9f,.86f,.78f));
                        for(int k=0;k<8;k++)load.Beam(new Vector3(-.15f,1.55f,-1.6f+k*.45f),new Vector3(.3f,1.55f,-1.6f+k*.45f),.04f,.04f,new Color(.84f,.8f,.72f));stuff=new[]{Bone};}
                    else if(name.Contains("hides")){load=new ZoneKit.Shape().Slot(0).Lump(new Vector3(0,1.6f,-.1f),new Vector3(.7f,.24f,.45f),3,.2f,new Color(.66f,.5f,.38f),new Color(.44f,.34f,.28f),10,5);stuff=new[]{Hide};}
                    else if(name.Contains("buckets")){load=new ZoneKit.Shape().Slot(0).Tube(new[]{new Vector3(.45f,.45f,0),new Vector3(.45f,.85f,0)},new[]{.17f,.2f},9,new Color(.6f,.56f,.5f),new Color(.86f,.82f,.74f),1,1,false).Tube(new[]{new Vector3(-.45f,.45f,0),new Vector3(-.45f,.85f,0)},new[]{.17f,.2f},9,new Color(.6f,.56f,.5f),new Color(.86f,.82f,.74f),1,1,false);stuff=new[]{Bone};}
                    else if(name.Contains("oil")){load=new ZoneKit.Shape().Slot(0).Lump(new Vector3(.36f,.85f,.1f),new Vector3(.2f,.28f,.2f),5,.05f,new Color(.76f,.62f,.44f),new Color(.5f,.4f,.3f),9,5);stuff=new[]{Bone};}
                    if(load!=null){var carried=kit.Built(name+" carries",load,stuff);carried.transform.SetParent(walker.transform,false);carried.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
                }
            }
            /// <summary>One of the Kept coats, or Tern's own while the others aren't taken in.</summary>
            /// <summary>The height to ask of a kit piece that should come out <paramref name="length"/> long (Prop fits by height; the longhouse and the beasts were taken in by length).</summary>
            static float Tall(string key,float length)
            {
                var prefab=ZoneKit.Prefab("Environment",key);if(prefab==null)return length*.45f;
                var probe=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var b=ModelGeometry.BoundsOf(probe);UnityEngine.Object.DestroyImmediate(probe);
                return length*b.size.y/Mathf.Max(.01f,Mathf.Max(b.size.x,b.size.z));
            }
            static string Coat(string coat)=>ZoneKit.Prefab("Characters",coat)!=null?coat:ZoneKit.Prefab("Characters","TernBody")!=null?"TernBody":"Neve";
            /// <summary>What a herders' town keeps outside its doors, along the edges and off the ways: bundled hides, stacked bone, sledges, blocks of cut ice, lamp-oil jars. Every draw is
            /// taken whether or not its piece is kept (D213).</summary>
            static void Goods(ZoneKit kit)
            {
                var rng=new System.Random(73);var s=new ZoneKit.Shape();var blocks=new GameObject("Goods");int kept=0;var boxes=new List<(Vector3 at,Vector3 size,float yaw)>();
                for(int tries=0;tries<300&&kept<54;tries++)
                {
                    var p=new Vector2((float)rng.NextDouble()*124-62,(float)rng.NextDouble()*72-4);int kind=rng.Next(5);float size=.5f+(float)rng.NextDouble()*.5f,yaw=(float)rng.NextDouble()*360;
                    if(KeepsOff(p)||InPens(p)||Height(p)>.3f||p.y>58||BankTop(p)>0||Mathf.Abs(Height(p+Vector2.right*.6f)-Height(p))>.08f)continue;
                    if(Longhouses.Any(h=>Mathf.Abs(p.x-h.x)<3.8f&&Mathf.Abs(p.y-h.y)<6.4f)||Vector2.Distance(p,BathHouse)<7||p.x>54)continue;
                    kept++;var foot=kit.On(p.x,p.y);var turn=Quaternion.Euler(0,yaw,0);
                    if(kind==0){s.Slot(0).Lump(foot+Vector3.up*size*.3f,new Vector3(size*.8f,size*.32f,size*.55f),kept,.2f,new Color(.64f,.5f,.38f),new Color(.4f,.32f,.26f),10,5,.3f,turn);s.Slot(0).Lump(foot+Vector3.up*size*.62f,new Vector3(size*.7f,size*.26f,size*.5f),kept+1,.2f,new Color(.86f,.82f,.74f),new Color(.56f,.52f,.48f),10,5,.3f,turn);}
                    else if(kind==1){for(int k=0;k<4;k++)s.Slot(1).Tube(new[]{foot+turn*new Vector3(-size,.15f+k*.17f,(k%2)*.22f-.1f),foot+turn*new Vector3(size,.15f+k*.17f,(k%2)*.22f-.1f)},new[]{.09f,.08f},6,new Color(.86f,.82f,.72f),new Color(.96f,.94f,.88f),1,.5f);}
                    else if(kind==2){s.Slot(1).Box(foot+Vector3.up*.25f,new Vector3(size*1.1f,.12f,size*2.4f),turn,new Color(.82f,.78f,.7f),null,.6f);foreach(int side in new[]{-1,1})s.Slot(1).Tube(new[]{foot+turn*new Vector3(side*size*.5f,.08f,-size*1.3f),foot+turn*new Vector3(side*size*.5f,.08f,size*1.1f),foot+turn*new Vector3(side*size*.5f,.4f,size*1.45f)},new[]{.05f,.05f,.04f},5,new Color(.9f,.86f,.78f),new Color(.9f,.86f,.78f),1,.5f);}
                    else if(kind==3){s.Slot(2).Box(foot+Vector3.up*size*.35f,new Vector3(size*.9f,size*.7f,size*.7f),turn,new Color(.62f,.78f,.94f),new Color(.86f,.94f,1f),.5f);}
                    else{s.Slot(1).Lump(foot+Vector3.up*size*.45f,new Vector3(size*.32f,size*.45f,size*.32f),kept,.04f,new Color(.74f,.6f,.44f),new Color(.5f,.4f,.32f),10,6);}
                    boxes.Add((foot+Vector3.up*.35f,new Vector3(size*1.4f,.7f,kind==2?size*2.4f:size*1.2f),yaw));
                }
                var view=kit.Built("Goods",s,Hide,Bone,SnowWall);view.transform.SetParent(blocks.transform,true);
                foreach(var (at,size,yaw) in boxes)ZoneKit.Block(view,at,size,yaw,"Goods");
            }

            /// <summary>The floor's dressing, last: the ground kit's layers (trodden snow along the ways, bare wet stone round the vents, melt), flat details, low cover, and slow cloud
            /// shade. Without the kit's textures the floor keeps its vertex tones and says so.</summary>
            static void TownGround(ZoneKit kit,ZoneKit.Land land)
            {
                if(!File.Exists(GroundKit.Art+"ground-scree.png")||!File.Exists(GroundKit.Art+"ground-decals.png")||!File.Exists(GroundKit.Art+"ground-cover.png")||!File.Exists(GroundKit.Art+"ground-shade-cloud.png"))
                {Debug.LogWarning("HOARFELL_GROUND_KIT_PENDING Lastlight: the ground kit's textures aren't painted yet, so the floor has its vertex tones only");return;}
                var ground=GroundKit.Open(kit.zone,land.FloorFilter,47,7.2f);
                foreach(var way in Ways)ground.ClearLane(way,1.4f);
                float Bare(Vector2 p){float b=0;foreach(var v in Vents)b=Mathf.Max(b,1-ZoneKit.Smooth(1.4f,2.6f,Vector2.Distance(p,v)+(ground.Noise(p,1.1f,5)-.5f)*1.4f));return b;}
                ground.Layers(new Vector4(.05f,.3f,.36f,.3f),1.1f,new GroundKit.Layer("hoar-tracks",new Color(.84f,.86f,.92f,1),4),new GroundKit.Layer("scree",new Color(.42f,.44f,.5f,1),2.6f),new GroundKit.Layer("damp",new Color(.6f,.66f,.76f,.9f),3.2f));
                foreach(var way in Ways)ground.PaintPath(0,way,1.5f,.85f,.5f,.7f,way.Length);
                foreach(var at in new[]{Hask,IdraYard,IruShrine,Tern,Cradle,LowLamp,ReachSpawn,Pad})ground.PaintAround(0,at,2.4f,1,(int)at.x);
                foreach(var at in Longhouses)ground.PaintAround(0,at+new Vector2(0,-5.6f),2.2f,1,(int)at.x);
                ground.Paint(1,p=>Bare(p));
                // Melt in a ragged ring round each vent and nowhere else: laid over the pens it drew them as camouflage.
                ground.Paint(2,p=>{float r=Vents.Min(v=>Vector2.Distance(p,v));return (1-ZoneKit.Smooth(2.6f,3.6f,r+(ground.Noise(p,1.4f,9)-.5f)*1.2f))*ZoneKit.Smooth(1.2f,1.8f,r)*.8f;});
                ground.PaintPatches(0,5,.32f,p=>ZoneKit.Smooth(1.6f,3.4f,FromWays(p))*(InPens(p)?0:1),3);
                ground.Shade(p=>.08f*(1-ZoneKit.Smooth(1.4f,4,FromWays(p)))-.04f*Bare(p));
                Color snowTint=new(.78f,.84f,.96f,.85f),boneTint=new(.9f,.86f,.78f,1);
                ground.Decals(new[]{"Tracks","Ruts"},18,p=>1-ZoneKit.Smooth(1.6f,3.6f,FromWays(p)),.9f,2.4f,new Color(.5f,.54f,.66f,.8f),false,.12f,5);
                ground.Decals(new[]{"CracksA","CracksB"},4,p=>ZoneKit.Smooth(3,5,FromWays(p))*(InPens(p)?0:1),1.6f,3.2f,new Color(.7f,.76f,.88f,.75f),false,.1f,9);
                ground.Decals(new[]{"PebblesA","PebblesB"},5,p=>Bare(p)+(1-ZoneKit.Smooth(5,8,Vector2.Distance(p,new Vector2(-13,46))))*.6f,.5f,1.2f,boneTint,true,.1f,11);
                ground.Decals(new[]{"Litter","Stain"},9,p=>InPens(p)?1:0,.8f,1.8f,new Color(.56f,.5f,.38f,.85f),false,.12f,13);
                ground.Decals(new[]{"Scorch"},1.2f,p=>Bare(p),.8f,1.6f,new Color(.3f,.3f,.32f,.6f),false,.1f,15);
                ground.Cover(new[]{"BonesA","BonesB","Shards"},3,p=>ZoneKit.Smooth(2.2f,3.6f,FromWays(p))*(KeepsOff(p)?0:1)*(InPens(p)?0:1),.12f,.3f,new Color(.94f,.92f,.86f,1),2,.12f,3);
                ground.Cover(new[]{"TuftC","Twigs"},2.6f,p=>ZoneKit.Smooth(2.6f,4,FromWays(p))*(KeepsOff(p)?0:1),.14f,.32f,new Color(.7f,.68f,.6f,1),3,.14f,5);
                ground.Rocks(2,p=>ZoneKit.Smooth(3,5,FromWays(p))*(KeepsOff(p)?0:1)*(InPens(p)?0:1),.12f,.34f,new Color(.62f,.64f,.7f),3,.12f,7);
                ground.Look("Canyon",null).LampPools(2.6f,new Color(1,.74f,.46f,.45f));
                ground.MovingShade("cloud",70,new Vector2(.5f,.3f),.22f,.5f,.05f,default,new Vector2(10,24));
                ground.Finish();
            }
        }
    }
}
