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
    /// The Frozen Host (wild approach on the White Reach, then the dig; rule: a thaw-rig turns the ice amber, and amber ice lets go of what is in it). Map brief, coordinates and the boss
    /// arena contract: docs/maps/FrozenHost.md. Everything of the zone's own is in <see cref="FrozenHost"/>, so nothing here can collide with the other Hoarfell files' members.
    /// <c>-host-greybox</c> leaves out dressing and the ground kit (the blockout).
    /// </summary>
    public static partial class HoarfellBuilder
    {
        static void BuildFrozenHost()=>FrozenHost.Build();

        /// <summary>The Reach's and the dig's light, registered beside its builder (ZoneLook finds it by its attribute). A white sun low over snow and a blue fill, so snow in shadow
        /// reads blue and the thaw's amber reads warm against it; the dig is open to the same sky, its depth read in the shade of its south slopes.</summary>
        [ZoneLook.Source] static void FrozenHostLooks()
        {
            ZoneLook.Register(Host,new ZoneLook.Spec{sun=new Color(1f,.95f,.88f),sunIntensity=1.15f,sunPitch=34,shadowStrength=.72f,
                sky=new Color(.5f,.6f,.74f),equator=new Color(.44f,.5f,.58f),ground=new Color(.26f,.28f,.32f),
                fog=true,fogColor=new Color(.76f,.82f,.9f),fogStart=55,fogEnd=200,
                exposure=-.05f,contrast=12,saturation=4,splitShadows=new Color(.4f,.5f,.64f),splitHighlights=new Color(.6f,.55f,.48f),
                bloomThreshold=.92f,bloomIntensity=.6f,vignette=.2f});
        }

        internal static class FrozenHost
        {
            // ---------------------------------------------------------------- the plan
            // x runs east and z north, so north is the back of every view (the fixed camera looks north-north-east from the south-south-west). The Reach runs north from the gate:
            // the spear field, crevasse one, the herd field, the serac ridge with its gap, the whiteout field, crevasse two, the Muster's camp at the dig's head. The dig is a trench cut
            // east into the glacier from the camp, stepping down a level at a time: the upper gallery, the second level (the left gallery north off it, the Excavators' cut straight on),
            // the third level north-east of it, the bottom gallery's mouth and the bottom gallery itself. Every north wall is the glacier's full height, with the host frozen at its foot;
            // every south side slopes under the camera's line to the floor beyond it.
            internal const float Snow=0,L1=-4,L2=-8.5f,L3=-13,Bottom=-17.5f,CliffTop=1.6f;
            internal const float ArenaRadius=18,ShelfRadius=20.5f,CouplingOut=13.5f,RankOut=16,MarshalOut=10;
            /// <summary>A plug of ice across a way down: nobody climbs 2.9 m of ice, and it stays under the 3 m that a thing across the camera's line to a fight may stand (plug two is
            /// south of the left gallery's fight, between it and the camera, until it melts).</summary>
            internal const float PlugTall=2.9f;
            /// <summary>The bottom gallery's centre, C in the boss arena contract.</summary>
            internal static readonly Vector2 C=new(208,218);
            /// <summary>The couplings' bearings from C (degrees from north toward east) in the order the contract counts them, 1 to 3.</summary>
            internal static readonly float[] CouplingBearings={330,30,90};
            internal static Vector2 Bearing(float bearing,float radius)=>C+new Vector2(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad))*radius;
            /// <summary>Plan points a player stands at, named once: the scatter's keep-off, the navigation's must-reach list, LookDev's views, the map brief and a route all want them.</summary>
            internal static readonly Vector2 Arrival=new(0,2),GateWay=new(0,-10.5f),Cradle=new(-9,4),MainBridge=new(2,50),WestBridge=new(-30,51),Sledge=new(-40,60.5f),
                StrayOne=new(-36,64.5f),VentWest=new(-38,66),VentOne=new(-30,104),VentTwo=new(34,118),StrayTwo=new(-33.5f,107),StrayThree=new(36.5f,121),
                Bench=new(-10,151),Boiler=new(43,163),Mast=new(30,165.5f),
                RigOne=new(60.5f,194.2f),RigOneHand=new(58.5f,189.5f),RigTwo=new(100.5f,197.4f),RigTwoHand=new(99,192.5f),RigThree=new(147.5f,233.2f),RigThreeHand=new(145.5f,228.5f),
                GapAt=new(85.6f,206.5f),Tools=new(131,190.5f),ThirdCache=new(115.5f,232),GapSpawn=new(168.5f,210),DoorSpawn=new(177,211),DoorCradle=new(173.5f,206),
                AnteBench=new(184,229.5f),Winch=new(176,231),WinchHand=new(176,228.5f),Passage=new(190,218);
            /// <summary>The ambusher's hides: drifts on the spear field and in the stalkers' field.</summary>
            internal static readonly Vector2[] FieldHides={new(-10,30),new(9,35),new(-1,40)},WhiteHides={new(0,116),new(13,121),new(-6,125)};
            /// <summary>Windows the wind has scoured to blue ice over the host, in the spear field and the stalkers' field (centre, radius): the host shows through them.</summary>
            static readonly (Vector2 at,float r)[] IceWindows={(new(-7,17),3.6f),(new(9,25),3.2f),(new(-12,35),3.4f),(new(13,41),3f),(new(-16,116),3.2f),(new(18,126),3f)};
            /// <summary>The crevasses: the line of each (z at x by a gentle wave), its half width, and its bridges (centre x, width).</summary>
            static readonly (float z,float amp,float freq,float phase,float half,(float x,float wide)[] bridges)[] Crevasses=
            {
                (50,2,.06f,.4f,2.2f,new[]{(2f,6.4f),(-30f,4.4f)}),
                (136,1.5f,.08f,2.1f,2f,new[]{(6f,6.4f)}),
            };
            /// <summary>The vents: warm ground in a hollow, steam over it (centre, radius). The first two are the vent pockets.</summary>
            static readonly (Vector2 at,float radius)[] Vents={(VentOne,6),(VentTwo,5.5f),(VentWest,4)};
            /// <summary>The herd's line: in from behind the west end of the ridge, south through the gap's west half, then away east across the herd field.</summary>
            internal static readonly Vector2[] HerdLine={new(-48,91),new(-26,92),new(-9,89),new(-5.5f,81),new(-4,72),new(4,66),new(24,62),new(48,61)};

            // ---------------------------------------------------------------- the ground
            enum Room{Reach,Gate,Gap,Mouth,L1,Ramp1,L2,Left,Cut,Ramp2,L3,Ramp3,Ante,Door,Niche,Arena}
            static readonly (Room room,float x0,float x1,float z0,float z1,float round)[] Boxes=
            {
                (Room.Gate,-5,5,-15,-3,.6f),(Room.Reach,-50,50,-8,76,3),(Room.Gap,-10,10,68,94,2.5f),(Room.Reach,-50,50,88,167,3),
                (Room.Mouth,18,30,158,180,.6f),(Room.L1,14,64,176,198,.8f),(Room.Ramp1,62,82,178,196,.5f),(Room.L2,80,114,170,198,.8f),(Room.Left,84,96,196,220,.6f),
                (Room.Cut,112,134,174,194,.8f),(Room.Ramp2,94,114,208,220,.5f),(Room.L3,112,152,204,236,.8f),(Room.Ramp3,150,168,210,228,.5f),(Room.Ante,166,188,202,234,.8f),
                (Room.Door,186,193,214,222,.4f),(Room.Niche,203,213,236,242,.6f),
            };
            static float RoundBox(Vector2 p,float x0,float x1,float z0,float z1,float round=.8f)
            {
                var d=new Vector2(Mathf.Abs(p.x-(x0+x1)*.5f)-(x1-x0)*.5f+round,Mathf.Abs(p.y-(z0+z1)*.5f)-(z1-z0)*.5f+round);
                return Mathf.Min(Mathf.Max(d.x,d.y),0)+new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude-round;
            }
            /// <summary>How far the bottom gallery's floor reaches from C on a bearing: to the rank shelf under the tall north and east wall (bearings 292 to 128), to r 18 elsewhere.</summary>
            static float Reach(float bearing)
            {
                float b=Mathf.Repeat(bearing+68,360);
                return Mathf.Lerp(ArenaRadius,ShelfRadius,ZoneKit.Smooth(0,10,b)*(1-ZoneKit.Smooth(186,196,b)));
            }
            static float BearingOf(Vector2 p){var d=p-C;return Mathf.Repeat(Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg,360);}
            /// <summary>Metres outside the nearest floor (negative on one), and which floor.</summary>
            static float Nearest(Vector2 p,out Room room)
            {
                float best=Vector2.Distance(p,C)-Reach(BearingOf(p));room=Room.Arena;
                foreach(var (which,x0,x1,z0,z1,round) in Boxes){float clear=RoundBox(p,x0,x1,z0,z1,round);if(clear<best){best=clear;room=which;}}
                return best;
            }
            /// <summary>The Reach's snow: drifts at two scales and the wind's ridges a hand high (painted by the ground kit), flatter where the Muster have trodden the camp; crevasses cut
            /// through it (a V of ice to 7 m, bridged), and the vents' hollows of bare warm ground.</summary>
            internal static float ReachHeight(Vector2 p)
            {
                float trodden=ZoneKit.Smooth(140,150,p.y);
                float h=(.34f*(Mathf.PerlinNoise(p.x*.045f+3,p.y*.045f+7)-.5f)+.14f*(Mathf.PerlinNoise(p.x*.13f+1,p.y*.13f+5)-.5f))*(1-.7f*trodden)+Drifts(p)*(1-trodden);
                foreach(var (at,radius) in Vents)h=Mathf.Lerp(h,-.38f,1-ZoneKit.Smooth(radius*.55f,radius,Vector2.Distance(p,at)));
                float open=Crevasse(p);
                if(open<0)h-=7*ZoneKit.Smooth(0,1.4f,-open);
                return h;
            }
            /// <summary>Wind-packed drifts: hand-high humps, painted by the ground kit (ShadeRelief), so the snow reads as blown and not as a board.</summary>
            internal static float Drifts(Vector2 p)=>GroundRelief.Mounds(p,3.4f,.1f,.4f,13);
            /// <summary>Metres outside the nearest open crevasse (negative inside one): the crevasse's band less its bridges.</summary>
            internal static float Crevasse(Vector2 p)
            {
                float best=float.PositiveInfinity;
                foreach(var (z,amp,freq,phase,half,bridges) in Crevasses)
                {
                    if(p.x<-54||p.x>54)continue;
                    float band=Mathf.Abs(p.y-(z+amp*Mathf.Sin(p.x*freq+phase)))-half;
                    float bridge=float.PositiveInfinity;foreach(var (x,wide) in bridges)bridge=Mathf.Min(bridge,Mathf.Abs(p.x-x)-wide*.5f);
                    best=Mathf.Min(best,Mathf.Max(band,-bridge));
                }
                return best;
            }
            internal static float CrevasseZ(int which,float x){var c=Crevasses[which];return c.z+c.amp*Mathf.Sin(x*c.freq+c.phase);}
            static float Level(Vector2 p,Room room)
            {
                switch(room)
                {
                    case Room.Reach:case Room.Gap:return ReachHeight(p);
                    case Room.Gate:return Snow+.04f*(Mathf.PerlinNoise(p.x*.3f,p.y*.3f)-.5f);
                    case Room.Mouth:return Mathf.Lerp(ReachHeight(p),L1,Mathf.Clamp01((p.y-166)/10));
                    case Room.L1:return L1;
                    case Room.Ramp1:return Mathf.Lerp(L1,L2,Mathf.Clamp01((p.x-66)/12));
                    case Room.L2:case Room.Left:case Room.Cut:return L2;
                    case Room.Ramp2:return Mathf.Lerp(L2,L3,Mathf.Clamp01((p.x-97)/14));
                    case Room.L3:return L3;
                    case Room.Ramp3:return Mathf.Lerp(L3,Bottom,Mathf.Clamp01((p.x-153)/12));
                    default:return Bottom;
                }
            }
            internal static float FloorAt(Vector2 p,out float level){float clear=Nearest(p,out var room);level=Level(p,room);return clear;}
            static readonly Vector2 Away=new(Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));
            static bool InDig(Vector2 p)=>p.y>170||p.x>60;
            /// <summary>The ground. A floor is its room's level; between floors is the glacier, which, with a floor beyond it along the camera's line, stays under the sightline to a hero at
            /// that floor's edge (measured from the lower of the two, as the Drowned Waystation's walls are, D119) and otherwise rises to the glacier's top: the Reach's edges and the
            /// serac ridge 5 to 8 m over the snow, the dig's walls to the glacier's surface at +3.6 to +6.2.</summary>
            internal static float Height(Vector2 p)
            {
                float clear=FloorAt(p,out float level);if(clear<=0)return level;
                bool dig=InDig(p);
                float allowed=float.PositiveInfinity;
                for(float d=.5f;d<=16;d+=.5f){if(FloorAt(p+Away*d,out float there)<0){allowed=Mathf.Min(level,there)+.6f+.82f*d;break;}}
                float rough=Mathf.PerlinNoise((p.x+90)*.09f,(p.y+30)*.08f),fine=Mathf.PerlinNoise(p.x*.31f+4,p.y*.31f+9);
                float top=Mathf.Min(allowed,Mathf.Max(level+4.6f+3.2f*rough+.5f*fine,3.6f+2.6f*rough+.02f*Mathf.Max(0,p.y-170)));
                return level+(top-level)*Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/(dig?.9f:2.4f)));
            }
            /// <summary>Whether a body could stand here: a floor, and not down a crevasse.</summary>
            internal static bool Walkable(Vector2 p){if(FloorAt(p,out _)>0)return false;return InDig(p)||Crevasse(p)>.6f;}
            static bool InArena(Vector2 p,float inset=0)=>Vector2.Distance(p,C)<ArenaRadius-inset;

            /// <summary>Snow white with a blue cast, bluer in its hollows and down a crevasse; the vents' ground dark and wet; the camp trodden grey. In the dig the cut ice is grey-blue,
            /// going bluer and darker the deeper it lies. Kept darker than what stands on it.</summary>
            static Color Tone(Vector2 p)
            {
                FloorAt(p,out float level);float mottle=.93f+.12f*Mathf.PerlinNoise(p.x*.17f+3,p.y*.17f+7);
                Color tone;
                if(!InDig(p))
                {
                    float hollow=ZoneKit.Smooth(.12f,-.25f,level);
                    tone=Color.Lerp(new Color(.97f,.98f,1f),new Color(.8f,.87f,.98f),hollow);
                    tone=Color.Lerp(tone,new Color(.34f,.44f,.62f),ZoneKit.Smooth(-.6f,-4,level));
                    // The vents' ground: dark and wet, moss green in patches.
                    foreach(var (at,radius) in Vents){float warm=1-ZoneKit.Smooth(radius*.35f,radius*.95f,Vector2.Distance(p,at));
                        tone=Color.Lerp(tone,Color.Lerp(new Color(.46f,.42f,.36f),new Color(.5f,.56f,.4f),ZoneKit.Smooth(.45f,.65f,Mathf.PerlinNoise(p.x*.6f+11,p.y*.6f+3))),warm);}
                    tone=Color.Lerp(tone,new Color(.8f,.82f,.86f),ZoneKit.Smooth(142,152,p.y)*.6f);
                }
                else
                {
                    tone=Color.Lerp(new Color(.9f,.94f,.98f),new Color(.76f,.83f,.93f),ZoneKit.Smooth(L1,Bottom,level));
                    if(Vector2.Distance(p,C)<ShelfRadius+1)tone=Color.Lerp(tone,new Color(.66f,.72f,.84f),.4f);
                    // The second level, which the Muster's thaw went over and left: duller, as ice that has thawed and frozen again is.
                    if(Mathf.Abs(level-L2)<.05f)tone=Color.Lerp(tone,new Color(.84f,.87f,.9f),.4f);
                }
                return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
            }

            // ---------------------------------------------------------------- what it is made of
            internal static Material SnowFloor,IceWall,IceBlock,AmberBlock,Iron,Timber,Tarp,Bronze,Cloth,Lamp,Stone,Below,InIce,Rock,Refrozen;
            static readonly Color SnowWhite=new(.96f,.98f,1f),SnowShade=new(.78f,.86f,.98f),IronDark=new(.42f,.42f,.44f),IronPale=new(.78f,.76f,.74f),Hazard=new(1f,.56f,.2f),
                WoodDark=new(.62f,.5f,.4f),WoodPale=new(.96f,.86f,.7f),BronzeDark=new(.66f,.56f,.42f),BronzePale=new(1f,.9f,.72f),Ochre=new(.86f,.5f,.3f),
                StoneDark=new(.6f,.58f,.54f),StonePale=new(.9f,.88f,.82f),RockDark=new(.34f,.35f,.4f),RockPale=new(.56f,.57f,.62f);
            static void Materials()
            {
                // Snow: near white, polished by the wind into shallow dimples about a stride across, each falling smoothly into the next (no rims, so no cells show), mottled cool at
                // two scales, a few bright glints. Where the ground kit's crust and scoured ice don't lie, this is the Reach: a surface of its own, not the crust's ridges again.
                var snow=ZoneKit.Texture("Hoarfell/host-snow",512,(u,v)=>
                {
                    float wu=u+(ZoneKit.Cloud(u,v,3,31)-.5f)*.12f,wv=v+(ZoneKit.Cloud(u,v,3,37)-.5f)*.12f;
                    // Two fields of dimples at different sizes, their centres thrown anywhere in their cells, so no row or grid of them shows.
                    static float Dimples(float x,float y,int cells,int seed)
                    {
                        float best=9;
                        for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                        {
                            int cx=Mathf.FloorToInt(x*cells)+dx,cy=Mathf.FloorToInt(y*cells)+dy;int wx=(cx%cells+cells)%cells,wy=(cy%cells+cells)%cells;
                            best=Mathf.Min(best,new Vector2(x*cells-(cx+.05f+.9f*ZoneKit.Tile(wx*1.3f,wy*2.1f,64,seed)),y*cells-(cy+.05f+.9f*ZoneKit.Tile(wx*2.7f,wy*1.1f,64,seed+3))).magnitude);
                        }
                        return ZoneKit.Smooth(.05f,.7f,best);
                    }
                    float big=ZoneKit.Cloud(u,v,4,3),fine=ZoneKit.Cloud(u,v,24,9),glint=ZoneKit.Tile(u*128,v*128,128,17),dimple=.6f*Dimples(wu,wv,8,5)+.4f*Dimples(wu+.37f,wv+.61f,13,11);
                    float tone=.84f+.1f*(big-.5f)+.07f*(fine-.5f)+.12f*(dimple-.5f)+(glint>.93f?.08f:0);
                    return new Color(tone*.97f,tone*.99f,tone*1.02f,1);
                });
                // Glacier ice for every wall: bands of the years laid down (blue, paler, a grey line of dust now and then), bubbles, and cracks running down across them.
                var ice=ZoneKit.Texture("Hoarfell/host-ice",512,(u,v)=>
                {
                    // The years' layers lie nearly level: broad pale bands, a thin blue line between some of them, a grey seam of dust now and then; fracture planes cut down across.
                    float lay=v*7+ZoneKit.Cloud(u,v*.25f,4,11)*.9f;int year=Mathf.FloorToInt(lay);float within=lay-year;
                    float own=ZoneKit.Tile(year*3.1f,.5f,40,5),grain=ZoneKit.Cloud(u,v,12,7),fine=ZoneKit.Cloud(u,v,40,3);
                    float tone=.84f+.08f*(own-.5f)+.06f*(grain-.5f)+.03f*(fine-.5f);float blue=.06f+.1f*own;
                    if(within<.035f){tone-=.12f;blue+=.14f;}
                    if(within>.5f&&within<.53f&&own>.7f)tone-=.2f;
                    float plane=Mathf.Abs(Mathf.Repeat(u*3+v*.8f+ZoneKit.Cloud(u,v,5,31)*.6f,1)-.5f);if(plane<.006f){tone+=.08f;blue+=.08f;}
                    // Faces (D378): the wall broken down its height into three uneven faces to a repeat, each at its own angle to the light (its own tone), a dark fracture
                    // between each and the next with a pale lip, wandering as a break does. Without them every wall read as one smooth extruded ribbon (nov22b s35).
                    float wob=(ZoneKit.Cloud(u,v,3,41)-.5f)*.12f+(ZoneKit.Cloud(u,v,9,43)-.5f)*.03f,fu=Mathf.Repeat(u+wob,1);
                    int face=fu<.27f?0:fu<.61f?1:2;float gap=Mathf.Min(Mathf.Min(fu,Mathf.Abs(fu-.27f)),Mathf.Min(Mathf.Abs(fu-.61f),1-fu));
                    // Each fracture runs part of the wall's height and dies out (drawn full height and full strength, the faces read as a striped wallpaper, ch45-d378-b).
                    float broken=ZoneKit.Smooth(.42f,.58f,ZoneKit.Cloud(u,v,3,45+face)),facet=ZoneKit.Cloud(u,v,2,49);
                    tone+=(face==0?.03f:face==1?-.03f:.01f)*facet;blue+=face==1?.03f*facet:0;
                    if(gap<.005f){tone-=.22f*broken;blue+=.1f*broken;}else if(gap<.011f)tone+=.06f*broken;
                    float bubble=ZoneKit.Tile(u*90,v*90,90,23);if(bubble>.95f)tone+=.06f;
                    return new Color(tone*(1-blue*1.4f),tone*(1-blue*.5f),Mathf.Min(1,tone*(1+blue*.3f)),1);
                });
                // The Muster's iron: soot-dark plates with rivet rows, scuffs gone pale, and the hazard paint their kit carries in stripes.
                var iron=ZoneKit.Texture("Hoarfell/host-iron",256,(u,v)=>
                {
                    float plate=Mathf.Repeat(u*4,1),row=Mathf.Repeat(v*4,1),scuff=ZoneKit.Cloud(u,v,8,5),rivet=Mathf.Min(Mathf.Abs(plate-.08f),Mathf.Abs(plate-.92f));
                    float tone=.52f+.16f*(scuff-.5f)-(row<.03f||plate<.02f?.16f:0)+(rivet<.02f&&Mathf.Repeat(v*24,1)<.3f?.12f:0);
                    return new Color(tone,tone*.98f,tone*.96f,1);
                });
                // Timber: boards, grain along them, dark at the joins.
                var timber=ZoneKit.Texture("Hoarfell/host-timber",256,(u,v)=>
                {
                    float board=Mathf.Repeat(u*5,1),grain=ZoneKit.Cloud(u*5,v*.5f,6,7),knot=ZoneKit.Tile(u*30,v*12,30,41);
                    float tone=.66f+.16f*(grain-.5f)-(board<.05f?.22f:0)-(knot>.95f?.12f:0);
                    return new Color(tone,tone*.92f,tone*.82f,1);
                });
                // Old bronze, frosted: warm metal gone green in streaks, rime in its pits.
                var bronze=ZoneKit.Texture("Hoarfell/host-bronze",256,(u,v)=>
                {
                    float streak=ZoneKit.Cloud(u*4,v*.6f,4,9),pit=ZoneKit.Tile(u*48,v*48,48,3),rime=ZoneKit.Cloud(u,v,9,2);
                    float green=ZoneKit.Smooth(.5f,.72f,streak);float tone=.62f+.18f*(rime-.5f)-(pit>.88f?.12f:0);
                    var c=Color.Lerp(new Color(tone*1.1f,tone*.86f,tone*.56f),new Color(tone*.66f,tone*.96f,tone*.86f),green*.7f);
                    return Color.Lerp(c,new Color(.9f,.94f,1),ZoneKit.Smooth(.6f,.8f,rime)*.5f);
                });
                // A canvas tarp: a weave, stains and a seam.
                var tarp=ZoneKit.Texture("Hoarfell/host-tarp",256,(u,v)=>
                {
                    float weave=(Mathf.Repeat(u*96,1)<.5f^Mathf.Repeat(v*96,1)<.5f)?.03f:-.03f,stain=ZoneKit.Cloud(u,v,5,17);
                    float tone=.62f+weave+.14f*(stain-.5f)-(Mathf.Abs(v-.5f)<.006f?.12f:0);
                    return new Color(tone,tone*.98f,tone*.9f,1);
                });
                SnowFloor=ZoneKit.Toon("host-snow",snow,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_BaseColor",Color.white);m.SetFloat("_BaseMapStrength",.9f);
                    m.SetColor("_ShadowTint",new Color(.56f,.66f,.88f));m.SetFloat("_LightFloor",.3f);});
                IceWall=ZoneKit.Toon("host-ice",ice,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_ICE_ON","_Ice");m.SetVector("_IceParams",new Vector4(2.8f,.26f,.24f,.18f));
                    m.SetColor("_IceDeepColor",new Color(.42f,.62f,.82f));m.SetColor("_IceRimColor",new Color(.7f,.9f,1.05f));m.SetColor("_BaseColor",new Color(.9f,.96f,1));
                    m.SetColor("_ShadowTint",new Color(.42f,.54f,.8f));m.SetFloat("_LightFloor",.3f);});
                // Clear ice in blocks, with the host standing in it: drawn after everything solid, writing no depth, before what lies on the floor and the markings, so a body in it shows
                // through and a marking on the floor beyond it is never hidden. Its dissolve is on, for a block that opens (Membrane.SetOpen).
                Material Clear(string name,Color tint,Color glow)=>ZoneKit.Toon(name,ice,m=>{ZoneKit.With(m,"_ICE_ON","_Ice");ZoneKit.With(m,"_DISSOLVE_ON","_Dissolve");
                    m.SetVector("_IceParams",new Vector4(2.2f,.5f,.3f,.3f));m.SetColor("_IceDeepColor",new Color(.24f,.46f,.7f));m.SetColor("_IceRimColor",new Color(.7f,.92f,1.1f));
                    m.SetColor("_BaseColor",tint);m.SetColor("_EmissionColor",glow);m.SetFloat("_LightFloor",.4f);
                    m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);
                    m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent-30;});
                IceBlock=Clear("host-ice-block",new Color(.66f,.84f,1,.56f),new Color(.05f,.16f,.3f));
                AmberBlock=Clear("host-ice-amber",new Color(1,.66f,.34f,.6f),new Color(1.3f,.5f,.1f));
                Iron=ZoneKit.Toon("host-iron",iron,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.42f,.48f,.62f));m.SetFloat("_LightFloor",.28f);});
                Timber=ZoneKit.Toon("host-timber",timber,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.46f,.5f,.66f));m.SetFloat("_LightFloor",.28f);});
                Tarp=ZoneKit.Toon("host-tarp",tarp,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.46f,.5f,.66f));m.SetFloat("_LightFloor",.3f);});
                Bronze=ZoneKit.Toon("host-bronze",bronze,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.44f,.5f,.66f));m.SetColor("_RimColor",new Color(.6f,.5f,.36f));m.SetFloat("_LightFloor",.3f);});
                Cloth=ZoneKit.Toon("host-cloth",tarp,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);ZoneKit.Swaying(m,.12f,1.6f,1.2f,.6f);m.SetColor("_ShadowTint",new Color(.5f,.48f,.62f));m.SetFloat("_LightFloor",.3f);});
                Lamp=ZoneKit.Toon("host-lamp",null,m=>{m.SetColor("_BaseColor",new Color(1,.86f,.6f));m.SetColor("_EmissionColor",new Color(2.2f,1.5f,.7f));});
                // Ancelane dressed stone: blocks in courses, each its own tone, dark joints with frost in them, worn and pitted.
                var stone=ZoneKit.Texture("Hoarfell/host-stone",256,(u,v)=>
                {
                    int course=Mathf.FloorToInt(v*4);float shift=course%2==0?0:.5f,bu=Mathf.Repeat(u*2+shift,1),bv=Mathf.Repeat(v*4,1);int block=Mathf.FloorToInt(Mathf.Repeat(u*2+shift,2))*7+course*3;
                    float own=ZoneKit.Tile(block*1.3f,course*2.1f,64,9),grain=ZoneKit.Cloud(u,v,16,5),pit=ZoneKit.Tile(u*64,v*64,64,21);
                    float joint=Mathf.Min(Mathf.Min(bu,1-bu)*.5f,Mathf.Min(bv,1-bv)*.25f);
                    float tone=.72f+.12f*(own-.5f)+.08f*(grain-.5f)-(pit>.9f?.08f:0);if(joint<.01f)tone=.5f;else if(joint<.016f)tone+=.06f;
                    return new Color(tone*1.02f,tone*.98f,tone*.9f,1);
                });
                Stone=ZoneKit.Toon("host-stone",stone,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.46f,.5f,.66f));m.SetFloat("_LightFloor",.3f);});
                // Rough dark rock for what herders stack: grained, speckled, a crack or two.
                var rock=ZoneKit.Texture("Hoarfell/host-rock",256,(u,v)=>
                {
                    float grain=ZoneKit.Cloud(u,v,8,61),fine=ZoneKit.Cloud(u,v,32,63),speck=ZoneKit.Tile(u*96,v*96,96,65),crack=Mathf.Abs(ZoneKit.Cloud(u,v,6,67)-.5f);
                    float tone=.7f+.16f*(grain-.5f)+.08f*(fine-.5f)+(speck>.9f?.08f:speck<.08f?-.08f:0)-(crack<.01f?.14f:0);
                    return new Color(tone,tone*.98f,tone*.96f,1);
                });
                Rock=ZoneKit.Toon("host-rock",rock,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.44f,.48f,.62f));m.SetFloat("_LightFloor",.28f);});
                // The second level's refrozen sills: ice the thaw went over and left, frozen again blue-white and cloudy with rime; no glow (amber is kept for what wakes).
                Refrozen=Clear("host-ice-refrozen",new Color(.86f,.93f,1,.7f),new Color(.03f,.06f,.1f));
                // The host beneath the ice (BelowArt): laid flat on a floor just before the ground kit's own flat details, so a crack or a drift lies over it and every danger marking
                // far over it. Lit as the floor is, so it darkens with it in shade.
                var below=ZoneKit.Texture("Hoarfell/host-below",1024,BelowArt,false,true);
                Below=ZoneKit.Toon("host-below",below,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetFloat("_LightFloor",.35f);m.SetColor("_ShadowTint",new Color(.56f,.66f,.88f));
                    m.SetFloat("_Cull",0);m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");m.renderQueue=GroundKit.DetailQueue-1;});
                // A rank's light from within its ice: a soft glow on a card behind the host, drawn before the clear ice that lies over it (no light of its own: past six a renderer,
                // a light is lost on the floor anyway).
                InIce=ZoneKit.Toon("host-ice-light",below,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(1.9f,.4f,0,0));m.SetColor("_EmissionColor",Color.black);
                    m.SetColor("_BaseColor",Color.white);m.SetFloat("_Cull",0);m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.One);
                    m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent-31;});
                AssetDatabase.SaveAssets();
            }

            // ---------------------------------------------------------------- the host beneath the ice: its atlas
            // Rows from the top: a rank seen close under clear ice, the same a metre or two deeper (softer), deeper still (a blur of ranks); then a row of four cells: kit lying in the
            // ice, the Marshal's sign, a soft line (the paving's joints), a soft round light. Each rank row is ten metres of the host abreast and two and a half up, laid on the floor
            // with "up" running away from the camera, so the host stands upright in the frame, as a body under clear ice does to a camera looking down through it.
            static Rect RankUv(int row)=>new(0,.75f-row*.25f+.004f,1,.242f);
            static readonly Rect KitUv=new(.008f,.008f,.234f,.234f),SignUv=new(.258f,.008f,.234f,.234f),LineUv=new(.625f,.008f,0,.234f),SoftUv=new(.758f,.008f,.234f,.234f);
            static float Capsule(float px,float py,float ax,float ay,float bx,float by,float r)
            {var p=new Vector2(px,py);var a=new Vector2(ax,ay);var ab=new Vector2(bx,by)-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(1e-6f,ab.sqrMagnitude));return Vector2.Distance(p,a+ab*t)-r;}
            static float Ellipse(float px,float py,float cx,float cy,float rx,float ry){float dx=(px-cx)/rx,dy=(py-cy)/ry;return (Mathf.Sqrt(dx*dx+dy*dy)-1)*Mathf.Min(rx,ry);}
            static float RoundRect(float px,float py,float cx,float cy,float hx,float hy,float r)
            {float dx=Mathf.Abs(px-cx)-hx+r,dy=Mathf.Abs(py-cy)-hy+r;return Mathf.Min(Mathf.Max(dx,dy),0)+new Vector2(Mathf.Max(dx,0),Mathf.Max(dy,0)).magnitude-r;}
            /// <summary>One of the host standing, front on, feet at the strip's foot: legs a stride apart, a skirt of plates, a cuirass, a crested helm, a round shield on the left arm,
            /// the spear upright in the right hand. Metres from its outline (negative inside).</summary>
            static float Soldier(float x,float y,float cx,float scale,int kind)
            {
                float px=(x-cx)/scale,py=(y-.06f)/scale;if(py>2.45f||py<-.15f)return 9;
                float stride=(kind%3)*.06f+.04f,lean=((kind*37)%7-3)*.01f,shieldUp=(kind%4)*.04f;
                float d=Capsule(px,py,-.1f-stride,0,-.09f,.84f,.085f);d=Mathf.Min(d,Capsule(px,py,.1f+stride*.6f,0,.09f,.84f,.085f));
                d=Mathf.Min(d,RoundRect(px,py,0,.92f,.25f,.12f,.05f));d=Mathf.Min(d,RoundRect(px,py,0,1.2f,.2f,.3f,.09f));
                d=Mathf.Min(d,Ellipse(px,py,-.25f,1.46f,.13f,.09f));d=Mathf.Min(d,Ellipse(px,py,.25f,1.46f,.13f,.09f));
                d=Mathf.Min(d,Ellipse(px,py,0,1.69f,.12f,.14f));d=Mathf.Min(d,Ellipse(px,py,.01f,1.86f,.035f,.1f));
                d=Mathf.Min(d,Ellipse(px,py,-.31f,1.1f+shieldUp,.33f,.35f));d=Mathf.Min(d,Capsule(px,py,.22f,1.42f,.32f,1.14f,.06f));
                d=Mathf.Min(d,Capsule(px,py,.32f,.02f,.32f+lean*2.2f,2.2f,.022f));d=Mathf.Min(d,Ellipse(px,py,.32f+lean*2.3f,2.28f,.045f,.11f));
                return d*scale;
            }
            static Color BelowArt(float u,float v)
            {
                if(v>=.25f)
                {
                    int row=Mathf.Clamp((int)((1-v)*4),0,2);float y=(v-(.75f-row*.25f))*10,x=u*10;
                    float blur=row==0?.035f:row==1?.075f:.15f,scale=row==2?.9f:1,d=9;int count=row==2?7:6;float step=9.2f/count;
                    for(int i=0;i<count;i++)
                    {
                        float cx=.4f+step*(i+.5f)+(ZoneKit.Tile(i*3.1f+row*11,1.5f,64,19)-.5f)*.36f;if(Mathf.Abs(x-cx)>1.3f)continue;
                        d=Mathf.Min(d,Soldier(x,y,cx,scale,i+row*3));
                    }
                    float a=1-ZoneKit.Smooth(-blur,blur,d),core=ZoneKit.Smooth(-blur,-blur*3-.08f,d);
                    a*=ZoneKit.Smooth(0,.05f,y)*ZoneKit.Smooth(2.5f,2.42f,y)*ZoneKit.Smooth(0,.12f,x)*ZoneKit.Smooth(10,9.88f,x);
                    float shade=1-.28f*core;return new Color(shade,shade,shade,a*(.72f+.28f*core));
                }
                int cell=Mathf.Clamp((int)(u*4),0,3);float s=u*4-cell,t=v*4,alpha;
                float edge=ZoneKit.Smooth(0,.03f,s)*ZoneKit.Smooth(1,.97f,s)*ZoneKit.Smooth(0,.03f,t)*ZoneKit.Smooth(1,.97f,t);
                switch(cell)
                {
                    case 0:
                    {
                        // Kit lying in the ice: two shields, two spears crossed, a helm.
                        float d=Mathf.Min(Ellipse(s,t,.3f,.36f,.17f,.15f),Ellipse(s,t,.7f,.64f,.16f,.1f));
                        d=Mathf.Min(d,Capsule(s,t,.1f,.14f,.9f,.78f,.012f));d=Mathf.Min(d,Capsule(s,t,.14f,.86f,.86f,.22f,.012f));
                        d=Mathf.Min(d,Ellipse(s,t,.58f,.26f,.07f,.06f));d=Mathf.Min(d,Ellipse(s,t,.9f,.79f,.02f,.06f));
                        alpha=1-ZoneKit.Smooth(-.012f,.012f,d);break;
                    }
                    case 1:
                    {
                        // The Marshal's sign: two rings, eight spokes between them, a cross of bars round a boss.
                        var p=new Vector2(s-.5f,t-.5f);float r=p.magnitude,a=Mathf.Atan2(p.y,p.x);
                        float d=Mathf.Min(Mathf.Abs(r-.41f)-.022f,Mathf.Abs(r-.28f)-.016f);
                        if(r>.27f&&r<.42f)d=Mathf.Min(d,Mathf.Abs(Mathf.Repeat(a/(Mathf.PI/4)+.5f,1)-.5f)*(Mathf.PI/4)*r-.012f);
                        if(r>.09f&&r<.22f)d=Mathf.Min(d,Mathf.Min(Mathf.Abs(p.x),Mathf.Abs(p.y))-.018f);
                        d=Mathf.Min(d,r-.06f);alpha=1-ZoneKit.Smooth(-.008f,.008f,d);break;
                    }
                    case 2:alpha=1-ZoneKit.Smooth(.12f,.42f,Mathf.Abs(t-.5f));edge=1;break;
                    default:alpha=Mathf.Pow(1-ZoneKit.Smooth(0,1,new Vector2(s-.5f,t-.5f).magnitude*2),1.6f);break;
                }
                return new Color(1,1,1,alpha*edge);
            }

            // ---------------------------------------------------------------- pieces
            /// <summary>The enemy if the combat engineer has defined it, else a stand-in of the same family (logged), so the zone is walkable and fought before the roster lands.</summary>
            static string Enemy(string id)
            {
                if(ZoneKit.Find<EnemyDef>(id)!=null)return id;
                string stand=id switch{"RimeStalker"=>"Reedstalker","FrostMotes"=>"MireflySwarm","ThawedGuard"=>"DrownedSentry","MusterExcavator"=>"Rootwarden","BridleHound"=>"Ridgehound","HostMarshal"=>"Floodcrown",_=>id};
                Debug.Log("FROZEN_HOST_STAND_IN "+stand+" for "+id);return stand;
            }
            static EncounterVolume Fight(ZoneKit kit,string id,Vector2 at,Vector2 size,string requiredFlag,params (string enemy,int count,Vector2 from,float yaw)[] packs)
            {
                var encounter=WorldBuilder.Encounter(id,kit.On(at.x,at.y,1.5f),new Vector3(size.x,8,size.y),Enemy(packs[0].enemy),packs[0].count);
                var spawners=new List<Spawner>{encounter.spawners[0]};
                for(int i=1;i<packs.Length;i++)spawners.Add(ZoneKit.Spawn(encounter,Enemy(packs[i].enemy),packs[i].count,Vector3.zero,2.4f));
                for(int i=0;i<packs.Length;i++)
                {
                    var spawner=spawners[i];spawner.transform.SetPositionAndRotation(kit.On(packs[i].from.x,packs[i].from.y),Quaternion.Euler(0,packs[i].yaw,0));
                    spawner.faceAsPlaced=packs[i].yaw!=0;spawner.radius=packs[i].count>1?2.4f:0;
                }
                encounter.spawners=spawners.ToArray();encounter.requiredFlag=requiredFlag??"";return encounter;
            }
            static void Preview(ZoneKit kit,EncounterVolume fight,Vector2 from,float radius)
            {
                var view=new GameObject(fight.encounterId+" view");view.transform.position=kit.On(from.x,from.y);fight.previewPoint=view.transform;fight.previewRadius=radius;
            }
            /// <summary>A drift the ambusher hides under: a ringed hide (AmbushSpot) and a hump of snow over it.</summary>
            static void Hide(ZoneKit kit,ZoneKit.Shape drifts,Vector2 at,int seed)
            {
                var spot=new GameObject("Snow hide "+at.x+" "+at.y,typeof(AmbushSpot));spot.transform.position=kit.On(at.x,at.y);spot.GetComponent<AmbushSpot>().radius=3.2f;
                drifts.Slot(0).Lump(kit.On(at.x,at.y,.06f),new Vector3(1.4f,.42f,1.1f),seed,.18f,SnowWhite,SnowShade,12,5,.25f,Quaternion.Euler(0,seed*37,0));
            }
            /// <summary>A block of ice built round its own foot (so it can slump where it stands): a box with its edges chipped and its faces a little out of true. Drawn with clear ice,
            /// or amber. Solid unless told not to be; a seal (a Membrane, a carving obstacle, not static) if it is to melt.</summary>
            static GameObject Block(ZoneKit kit,string name,Vector2 at,float y,Vector3 size,float yaw,Material look,bool solid,bool melts,int seed)
            {
                var shape=new ZoneKit.Shape();var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();
                var tone=Color.white;
                // Three slabs a little askew make one block: cut ice, not a crate.
                for(int k=0;k<3;k++)
                {
                    float h0=size.y*k/3f,h1=size.y*(k+1)/3f;var c=new Vector3((R()-.5f)*.08f,(h0+h1)*.5f,(R()-.5f)*.08f);
                    shape.Slot(0).Box(c,new Vector3(size.x*(1-.03f*k),h1-h0+.02f,size.z*(1-.04f*k)),Quaternion.Euler((R()-.5f)*2,(R()-.5f)*3,(R()-.5f)*2),tone,tone,.6f);
                }
                var go=kit.Built(name,shape,look);go.transform.SetPositionAndRotation(new Vector3(at.x,y,at.y),Quaternion.Euler(0,yaw,0));
                go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                if(!solid)return go;
                if(melts)
                {
                    go.AddComponent<Membrane>();
                    var body=new GameObject(name+" (solid)",typeof(BoxCollider),typeof(NavMeshObstacle));body.transform.SetParent(go.transform,false);body.transform.localPosition=Vector3.up*size.y*.5f;
                    body.GetComponent<BoxCollider>().size=size;var carve=body.GetComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.size=size;carve.carving=true;carve.carveOnlyStationary=false;
                }
                else{ZoneKit.Block(go,new Vector3(at.x,y+size.y*.5f,at.y),size,yaw,name+" (solid)");go.isStatic=true;}
                return go;
            }
            // ---- the host in the ice: the guard's body baked in four steps of its walk, so a rank stands frozen in the stride it was taking.
            static Mesh[] poses;static Material[] guardLooks;
            static void Poses()
            {
                poses=null;var prefab=ZoneKit.Prefab("Enemies","ThawedGuard")??ZoneKit.Prefab("Enemies","AncelaneGuard");if(prefab==null)return;
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                try
                {
                    var skin=body.GetComponentInChildren<SkinnedMeshRenderer>();var animator=body.GetComponentInChildren<Animator>();if(skin==null)return;
                    var clips=animator!=null&&animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:Array.Empty<AnimationClip>();
                    var walk=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("walk"))??clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle"));
                    var made=new List<Mesh>();
                    for(int k=0;k<4;k++)
                    {
                        if(walk!=null&&animator!=null)walk.SampleAnimation(animator.gameObject,walk.length*(.1f+k*.22f));
                        var baked=new Mesh();skin.BakeMesh(baked,true);
                        var v=baked.vertices;var n=baked.normals;var turn=skin.transform.rotation;var at=skin.transform.position;
                        for(int i=0;i<v.Length;i++)v[i]=at+turn*v[i];for(int i=0;i<n.Length;i++)n[i]=turn*n[i];
                        baked.vertices=v;baked.normals=n;baked.RecalculateBounds();
                        made.Add(ZoneKit.SaveMesh(baked,"Assets/_Project/Scenes/"+Host+"-Guard-pose-"+k+".asset"));
                    }
                    poses=made.ToArray();guardLooks=skin.sharedMaterials;
                }
                finally{UnityEngine.Object.DestroyImmediate(body);}
            }
            /// <summary>A rank of the host standing in the ice: <paramref name="count"/> guards abreast in one step of the march, facing <paramref name="yaw"/>, rimed pale. Drawn only.</summary>
            static GameObject Statues(string name,Vector2 centre,float y,float yaw,int count,int pose,float spacing=1.9f,Color? tint=null)
            {
                var holder=new GameObject(name);if(poses==null)return holder;
                var turn=Quaternion.Euler(0,yaw,0);var across=turn*Vector3.right;
                for(int i=0;i<count;i++)
                {
                    var at=new Vector3(centre.x,y,centre.y)+across*(i-(count-1)*.5f)*spacing;
                    var guard=new GameObject(name+" "+i,typeof(MeshFilter),typeof(MeshRenderer));guard.transform.SetParent(holder.transform,true);guard.transform.SetPositionAndRotation(at,turn);
                    guard.GetComponent<MeshFilter>().sharedMesh=poses[(pose+i*0)%poses.Length];guard.GetComponent<MeshRenderer>().sharedMaterials=guardLooks;guard.isStatic=true;
                    guard.AddComponent<PropTint>().tint=tint??new Color(.72f,.84f,1f);
                }
                return holder;
            }
            /// <summary>A rank frozen blue against a wall: its block of clear ice (solid, in the bake), three of the host standing in it, lit from within.</summary>
            static void FrozenRank(ZoneKit kit,string name,Vector2 foot,float y,float yaw,int pose,int seed,float wide=6.4f)
            {
                Block(kit,name+" ice",foot,y-.05f,new Vector3(wide,3.5f,2.4f),yaw,IceBlock,true,false,seed);
                Statues(name,foot,y,yaw,3,pose);
                LitFromWithin(new Vector3(foot.x,y,foot.y),yaw,3*1.9f+.6f,3.1f,new Color(.36f,.62f,1f,.62f));
            }
            /// <summary>A thaw-rig of the Muster's worked by a hero: the kit's ThawRig with a valve wheel on its flank that turns and a hose to the ice it heats; the hand is a discovery at its
            /// camera side; the rig is a FlowSwitch (the wheel, the burner's roar, the view drawn back over the ice), and the ice an IceThaw on the same flag.</summary>
            static IceThaw Rig(ZoneKit kit,string name,Vector2 at,float yaw,Vector2 hand,string flag,Vector3[] hose,Bounds shows,Renderer[] ice,Membrane[] melts,Light[] glow)
            {
                var body=kit.Prop(ZoneKit.Kit("ThawRig","HushwellVent"),new Vector3(at.x,0,at.y),3,yaw);body.name=name;
                var side=Quaternion.Euler(0,yaw,0)*Vector3.right;var wheelAt=kit.On(at.x,at.y,1.5f)+side*1.25f;
                var wheel=new ZoneKit.Shape();wheel.Slot(0).Disc(Vector3.zero,Vector3.right,.42f,14,Hazard,.34f,Hazard);
                for(int k=0;k<4;k++){var spoke=Quaternion.AngleAxis(k*45,Vector3.right)*Vector3.up*.38f;wheel.Slot(0).Beam(-spoke,spoke,.05f,.05f,IronPale);}
                wheel.Slot(0).Post(Vector3.left*.3f,.3f,.06f,.06f,6,IronDark,IronDark);
                var turns=kit.Built(name+" wheel",wheel,Iron);turns.transform.SetPositionAndRotation(wheelAt,Quaternion.Euler(0,yaw,0));
                var lines=new ZoneKit.Shape();lines.Slot(0).Tube(hose,hose.Select(_=>.14f).ToArray(),7,new Color(.32f,.3f,.3f),new Color(.5f,.46f,.42f),1,.4f);
                for(int i=1;i<hose.Length;i++){var c=Vector3.Lerp(hose[i-1],hose[i],.5f);var d=(hose[i]-hose[i-1]).normalized*.1f;lines.Slot(0).Tube(new[]{c-d,c+d},new[]{.2f,.2f},7,Hazard,Hazard);}
                kit.Built(name+" hose",lines,Iron);
                var point=kit.Discovery(name+" valve",new Vector3(hand.x,0,hand.y),"Run the thaw rig",flag,"",null,"Taren",null,0,2.9f);point.situation="";
                var run=body.AddComponent<FlowSwitch>();run.flag=flag;run.wheel=turns.transform;run.axis=Vector3.right;run.turns=2.5f;run.seconds=2.6f;run.point=point;run.shows=shows;
                run.marker="RIG_RUN";run.sound="thrusterFire_000";run.volume=.45f;
                var heat=new GameObject(name+" heat",typeof(IceThaw)).GetComponent<IceThaw>();heat.transform.position=shows.center;
                heat.flag=flag;heat.ice=ice;heat.melts=melts;heat.glow=glow;heat.refreezeFlag="";
                return heat;
            }
            static Light Glow(ZoneKit kit,Vector3 at,float range,float intensity,Color colour,string name)
            {var light=kit.Lamp(at,range,intensity,colour);light.name=name;light.transform.position=at;return light;}
            /// <summary>A cache: a Muster crate with its lid strapped, or an Ancelane coffer of bronze-bound stone.</summary>
            static LootCache Stash(ZoneKit kit,string id,string label,Vector2 at,float yaw,int scrip,bool ancelane,params LootEntry[] contents)
            {
                var shape=new ZoneKit.Shape();GameObject body;
                if(ancelane)
                {
                    shape.Slot(0).Box(new Vector3(0,.36f,0),new Vector3(1.3f,.72f,.8f),Quaternion.identity,BronzeDark,BronzePale).Box(new Vector3(0,.8f,0),new Vector3(1.36f,.16f,.86f),Quaternion.identity,BronzePale);
                    shape.Slot(0).Lump(new Vector3(0,.9f,0),new Vector3(.62f,.08f,.36f),3,.1f,SnowWhite,SnowShade,10,4);
                    body=kit.Built("Coffer "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),shape,Bronze);
                }
                else
                {
                    shape.Slot(0).Box(new Vector3(0,.42f,0),new Vector3(1.3f,.84f,.9f),Quaternion.identity,IronDark,IronPale);
                    foreach(float x in new[]{-.4f,.4f})shape.Slot(0).Box(new Vector3(x,.43f,0),new Vector3(.12f,.88f,.94f),Quaternion.identity,Hazard);
                    shape.Slot(1).Lump(new Vector3(0,.88f,0),new Vector3(.6f,.07f,.4f),5,.1f,SnowWhite,SnowShade,10,4);
                    body=kit.Built("Crate "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),shape,Iron,SnowFloor);
                }
                body.transform.SetPositionAndRotation(kit.On(at.x,at.y),Quaternion.Euler(0,yaw,0));
                var box=body.AddComponent<BoxCollider>();box.center=Vector3.up*.45f;box.size=new Vector3(1.4f,.9f,1f);body.isStatic=true;
                var cache=LootCache.On(body,id,label,scrip,contents);cache.range=2.6f;return cache;
            }
            /// <summary>Rime glass growing out of a wall's foot, or alloy prised out of a broken guard: gone once taken.</summary>
            static void Harvest(ZoneKit kit,string id,Vector2 at,string material,string prompt,int seed)
            {
                var shape=new ZoneKit.Shape();var rng=new System.Random(seed);
                for(int k=0;k<6;k++)
                {
                    var c=new Vector3(((float)rng.NextDouble()-.5f)*.8f,0,((float)rng.NextDouble()-.5f)*.8f);float tall=.3f+.5f*(float)rng.NextDouble();
                    var lean=Quaternion.Euler(((float)rng.NextDouble()-.5f)*40,0,((float)rng.NextDouble()-.5f)*40);
                    if(material=="RimeGlass")shape.Slot(0).Post(c,tall,.12f,.01f,5,new Color(.7f,.9f,1.1f),new Color(1.1f,1.2f,1.3f));
                    else shape.Slot(0).Box(c+lean*Vector3.up*tall*.3f,new Vector3(.3f,tall*.6f,.18f),lean,BronzeDark,BronzePale);
                }
                var body=kit.Built((material=="RimeGlass"?"Rime glass ":"Host alloy ")+id,shape,material=="RimeGlass"?IceWall:Bronze);body.transform.position=kit.On(at.x,at.y);
                var solid=body.AddComponent<BoxCollider>();solid.center=Vector3.up*.3f;solid.size=new Vector3(1,.6f,1);body.isStatic=true;
                var node=body.AddComponent<MineNode>();node.nodeId=id;node.material=material;node.amount=2;node.prompt=prompt;node.range=2.6f;
            }
            /// <summary>A stray of Hask's in the steam: the kit's rime-beast, and the point that walks it in. Offered only at its quest's step; once walked in it plods off south toward the
            /// herd's line and is gone (LeavesAfter), and a zone loaded after never shows it.</summary>
            static void Stray(ZoneKit kit,int index,Vector2 at,float yaw,Vector2[] away)
            {
                var beast=kit.Prop(ZoneKit.Kit("RimeBeast","DressBoulders"),new Vector3(at.x,0,at.y),1.7f,yaw,false);beast.name="Stray "+index;beast.isStatic=false;
                foreach(var t in beast.GetComponentsInChildren<Transform>())t.gameObject.isStatic=false;
                var leaves=beast.AddComponent<LeavesAfter>();leaves.flag="hoar.stray"+index;leaves.speed=1.6f;leaves.patience=4;leaves.path=away.Select(p=>kit.On(p.x,p.y)).ToArray();
                var point=kit.Discovery("Stray "+index+" (walk it in)",new Vector3(at.x,0,at.y-1.2f),"Walk the stray in","hoar.stray"+index,"StrayBeast",null,"Taren","StrayBeasts",0,3.2f);point.situation="stray";
                point.transform.SetParent(beast.transform,true);
            }
            static Vector2 Toward(float bearing)=>new(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad));
            /// <summary>A vent pocket, somewhere a herder is glad to reach in a whiteout: warm wet ground in a hollow (the height function's) with moss on it (the ground kit's), a pool
            /// steaming at its heart, the herders' windbreak of stacked stone and hide round its open side (bearings <paramref name="from"/> to <paramref name="to"/>, solid), their
            /// cairn at its edge with a pole of beast-hair tassels, steam rising, a faint warm light, and room inside for the party. A pocket with no windbreak (from equal to to) keeps
            /// a few loose stones instead.</summary>
            static void Vent(ZoneKit kit,ZoneKit.Shape stones,ZoneKit.Shape hides,GameObject solids,Vector2 at,float radius,float from,float to,float cairn,Material steam,int seed)
            {
                var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();var grey=new Color(.5f,.48f,.46f);var dark=new Color(.26f,.25f,.25f);
                if(to>from)
                {
                    Vector3? last=null;float r=radius*.92f;
                    for(float b=from;b<=to+.1f;b+=6.5f)
                    {
                        var p=at+Toward(b)*r;var foot=kit.On(p.x,p.y);
                        // Four courses of flat stones laid dry, each set a little off the one under it, smaller as they rise, to a hand over a metre.
                        for(int k=0;k<4;k++){var off=new Vector3(R()-.5f,0,R()-.5f)*.18f;float w=.62f-k*.07f+(R()-.5f)*.16f;
                            stones.Slot(0).Lump(foot+off+Vector3.up*(.13f+k*.26f),new Vector3(w,.15f+R()*.05f,w*(.6f+.25f*R())),seed+(int)b*5+k,.35f,Color.Lerp(grey,dark,R()*.5f),dark,9,5,.25f,Quaternion.Euler((R()-.5f)*8,b+90+(R()-.5f)*50,(R()-.5f)*8));}
                        if(last is Vector3 l)ZoneKit.BlockAlong(solids,l+Vector3.up*.55f,foot+Vector3.up*.55f,.7f,1.1f,"Windbreak");
                        last=foot;
                    }
                    // A hide stretched between two poles over the middle of it, the herders' own patching.
                    float mid=(from+to)*.5f;var a=kit.On(at.x+Toward(mid-14).x*r,at.y+Toward(mid-14).y*r);var c=kit.On(at.x+Toward(mid+14).x*r,at.y+Toward(mid+14).y*r);var inward=(new Vector3(at.x,0,at.y)-(a+c)*.5f);inward.y=0;inward.Normalize();
                    foreach(var pole in new[]{a,c})hides.Slot(0).Post(pole-inward*.1f,1.9f,.05f,.04f,5,WoodDark,WoodPale);
                    var sag=(a+c)*.5f-inward*.2f;
                    hides.Slot(1).Quad(a-inward*.16f+Vector3.up*.45f,sag+Vector3.up*.4f,sag+Vector3.up*1.7f,a-inward*.16f+Vector3.up*1.85f,-inward,new Color(.6f,.5f,.4f),.6f)
                        .Quad(sag+Vector3.up*.4f,c-inward*.16f+Vector3.up*.45f,c-inward*.16f+Vector3.up*1.85f,sag+Vector3.up*1.7f,-inward,new Color(.56f,.47f,.38f),.6f);
                }
                else for(int k=0;k<6;k++)
                {
                    float b=k*60+R()*40;var p=at+Toward(b)*radius*(.75f+.2f*R());
                    stones.Slot(0).Lump(kit.On(p.x,p.y,.08f),new Vector3(.5f,.3f,.42f)*(.8f+.5f*R()),seed+k,.25f,grey,dark,10,5);
                }
                // The cairn: stones stacked to a stride high on the pocket's rim, a pole out of its top with tassels of rime-beast hair.
                var heap=at+Toward(cairn)*radius*.96f;var base_=kit.On(heap.x,heap.y);float up=0;
                // Flat slabs, each set off the last and turned, as a cairn is built: not a stack of balls.
                foreach(var (w,h) in new[]{(.7f,.17f),(.62f,.16f),(.55f,.17f),(.46f,.15f),(.38f,.15f),(.28f,.14f)})
                {var off=new Vector3(R()-.5f,0,R()-.5f)*.16f;stones.Slot(0).Lump(base_+off+Vector3.up*(up+h),new Vector3(w,h,w*(.65f+.2f*R())),seed+(int)(up*10)+91,.35f,Color.Lerp(grey,dark,R()*.4f),dark,9,5,.25f,Quaternion.Euler((R()-.5f)*10,R()*180,(R()-.5f)*10));up+=h*1.75f;}
                hides.Slot(0).Post(base_+Vector3.up*(up-.2f),1.3f,.045f,.035f,5,WoodDark,WoodPale);
                for(int k=0;k<4;k++){var d=Quaternion.Euler(0,k*90+cairn,0)*Vector3.forward*.07f;var top=base_+Vector3.up*(up+.95f-k*.06f)+d;
                    hides.Slot(2).Quad(top-d*.4f,top+d*.4f+Vector3.down*.02f,top+d*.9f+Vector3.down*.55f,top+d*.2f+Vector3.down*.5f,d,new Color(.62f,.52f,.42f),.6f);}
                ZoneKit.Block(solids,base_+Vector3.up*.75f,new Vector3(1.1f,1.5f,1.1f),0,"Cairn");
                for(int k=0;k<4;k++){var p=at+new Vector2(R()-.5f,R()-.5f)*radius*.6f;
                    kit.Drift("Vent steam "+Mathf.RoundToInt(at.x)+" "+k,kit.On(p.x,p.y,1.2f),new Vector3(1.6f,2.8f,1.6f),steam,new Color(.95f,.97f,1,.36f),90,1f,2.6f,.55f,5);}
                kit.Drift("Vent pool steam "+Mathf.RoundToInt(at.x),kit.On(at.x,at.y,.4f),new Vector3(radius*.5f,.6f,radius*.4f),steam,new Color(1,.98f,.96f,.3f),60,.6f,1.6f,.3f,4);
                var warmth=kit.Lamp(new Vector3(at.x,1.2f,at.y),radius*1.4f,.7f,new Color(1,.86f,.7f));warmth.name="Vent warmth";
            }

            // ---------------------------------------------------------------- the host beneath the ice: laying it
            // The floors the host is laid under, by level: the Reach, the upper gallery, the second level (with the left gallery and the cut), the third, the bottom (the gallery's
            // mouth, the door and the gallery). Ramps and the mouth down from the Reach take none: what's laid on a floor stays on its own level.
            static int Floor(Room room)=>room switch{Room.Reach or Room.Gap=>0,Room.L1=>1,Room.L2 or Room.Left or Room.Cut=>2,Room.L3=>3,Room.Ante or Room.Door or Room.Arena or Room.Niche=>4,_=>-1};
            static int FloorOf(Vector2 p){Nearest(p,out var room);return Floor(room);}
            static readonly Dictionary<(int x,int z,int floor),(List<Vector3> v,List<Vector2> uv,List<Color32> c,List<int> t)> beneath=new();
            static readonly Vector2 Across=new(Away.y,-Away.x);
            static Color32 Baked(Color tint){var linear=tint.linear;return new Color(Mathf.Clamp01(linear.r),Mathf.Clamp01(linear.g),Mathf.Clamp01(linear.b),Mathf.Clamp01(tint.a));}
            /// <summary>One piece of the atlas laid flat on a floor, centred at <paramref name="at"/>: <paramref name="wide"/> along <paramref name="across"/> (the atlas's u) and
            /// <paramref name="tall"/> along <paramref name="along"/> (its v), point by point on the floor's own level, each point's alpha the tint's times <paramref name="clear"/> there.
            /// Nothing if any of it would leave its floor or reach a crevasse. <paramref name="u0"/> and <paramref name="u1"/> take part of a rank's row.</summary>
            static bool Beneath(Vector2 at,Vector2 across,float wide,Vector2 along,float tall,Rect uv,Color tint,Func<Vector2,float> clear,float u0=0,float u1=1)
            {
                if(Nearest(at,out var room)>-.2f)return false;int floor=Floor(room);if(floor<0)return false;
                int nx=Mathf.Max(1,Mathf.CeilToInt(wide/.6f)),nz=Mathf.Max(1,Mathf.CeilToInt(tall/.6f));
                var v=new List<Vector3>();var uvs=new List<Vector2>();var c=new List<Color32>();float shown=0;
                for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++)
                {
                    var p=at+across*(wide*(i/(float)nx-.5f))+along*(tall*(j/(float)nz-.5f));
                    if(Nearest(p,out var here)>-.15f||Floor(here)!=floor||floor==0&&Crevasse(p)<1)return false;
                    float a=tint.a*Mathf.Clamp01(clear(p));shown+=a;
                    v.Add(new Vector3(p.x,(floor==0?ReachHeight(p)+.035f:Level(p,here)+.012f),p.y));
                    uvs.Add(new Vector2(Mathf.Lerp(uv.xMin+uv.width*u0,uv.xMin+uv.width*u1,i/(float)nx),Mathf.Lerp(uv.yMin,uv.yMax,j/(float)nz)));c.Add(Baked(new Color(tint.r,tint.g,tint.b,a)));
                }
                if(shown<.02f)return false;
                var key=(Mathf.FloorToInt(at.x/12),Mathf.FloorToInt(at.y/12),floor);
                if(!beneath.TryGetValue(key,out var batch))beneath[key]=batch=(new List<Vector3>(),new List<Vector2>(),new List<Color32>(),new List<int>());
                int first=batch.v.Count;batch.v.AddRange(v);batch.uv.AddRange(uvs);batch.c.AddRange(c);
                for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){int a=first+j*(nx+1)+i,b=a+1,d=a+nx+1,e=d+1;batch.t.AddRange(new[]{a,d,b,d,e,b});}
                return true;
            }
            /// <summary>A rank of the host under the floor, square to the camera: the whole row of the atlas, or failing that either half of it.</summary>
            static int Rank(Vector2 at,int row,Color tint,Func<Vector2,float> clear)
            {
                if(Beneath(at,Across,10,Away,2.5f,RankUv(row),tint,clear))return 1;
                int laid=0;foreach(var (half,u0,u1) in new[]{(-2.5f,0f,.5f),(2.5f,.5f,1f)})if(Beneath(at+Across*half,Across,5,Away,2.5f,RankUv(row),tint,clear,u0,u1))laid++;
                return laid;
            }
            /// <summary>A line of the atlas's soft band from one point to another, in pieces that each keep to the floor: the paving's joints, the muster ground's rings.</summary>
            static void Line(Vector2 a,Vector2 b,float wide,Color tint,Func<Vector2,float> clear)
            {
                float length=Vector2.Distance(a,b);int pieces=Mathf.Max(1,Mathf.CeilToInt(length/2.4f));var along=(b-a)/length;var side=new Vector2(-along.y,along.x);
                for(int k=0;k<pieces;k++)Beneath(Vector2.Lerp(a,b,(k+.5f)/pieces),along,length/pieces+.04f,side,wide,LineUv,tint,clear);
            }
            /// <summary>Writes what was laid as flat batches, one per 12 m cell and floor (so each takes its own nearby lamps and stays level), named as floor detail so the checks read
            /// them as lying on the ground; their meshes are one asset beside the scene.</summary>
            static void EmitBeneath()
            {
                string path="Assets/_Project/Scenes/"+Host+"-Beneath.asset";if(AssetDatabase.LoadMainAssetAtPath(path)!=null)AssetDatabase.DeleteAsset(path);
                var holder=new GameObject("The host beneath the ice");var meshes=new List<Mesh>();int quads=0;
                foreach(var pair in beneath.OrderBy(p=>p.Key.floor).ThenBy(p=>p.Key.x).ThenBy(p=>p.Key.z))
                {
                    var (x,z,floor)=pair.Key;var (v,uv,c,t)=pair.Value;
                    var mesh=new Mesh{name=$"{Host} beneath {x},{z} {floor}",indexFormat=v.Count>65000?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16};
                    mesh.SetVertices(v);mesh.SetNormals(Enumerable.Repeat(Vector3.up,v.Count).ToList());mesh.SetUVs(0,uv);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateBounds();meshes.Add(mesh);
                    var go=new GameObject($"Floor detail host beneath {x},{z}",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(holder.transform,false);
                    go.GetComponent<MeshFilter>().sharedMesh=mesh;var drawn=go.GetComponent<MeshRenderer>();drawn.sharedMaterial=Below;drawn.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    quads+=t.Count/6;
                }
                if(meshes.Count>0){AssetDatabase.CreateAsset(meshes[0],path);foreach(var made in meshes.Skip(1))AssetDatabase.AddObjectToAsset(made,path);AssetDatabase.ImportAsset(path);}
                Debug.Log($"FROZEN_HOST_BENEATH batches={meshes.Count} cells={quads}");beneath.Clear();
            }
            /// <summary>Where a rig or a thaw is at the ice now: 1 at a rig or an amber rank, falling off over a dozen metres.</summary>
            static float Warmth(Vector2 p,int floor)
            {
                float w=0;
                foreach(var rig in new[]{RigOne,RigTwo,RigThree})w=Mathf.Max(w,ZoneKit.Smooth(12,4,Vector2.Distance(p,rig)));
                foreach(var amber in AmberAt)w=Mathf.Max(w,ZoneKit.Smooth(10,3,Vector2.Distance(p,amber)));
                return w;
            }
            /// <summary>The ambers that wake: the Muster's own thaw in the upper gallery and the mouth.</summary>
            static readonly Vector2[] AmberAt={new(40.2f,196.8f),new(169.6f,231.6f)};
            /// <summary>The host under the dig's floors and the bottom gallery's, and under the Reach wherever the wind has scoured the snow to ice: blocks of ranks three deep with lanes
            /// between, in a brick bond so no lane runs straight through, square to the camera so every rank stands upright in the frame. Far below the upper gallery (soft and faint),
            /// nearer under the second level (and warm), close under the third level and the bottom, where the Ancelane paving they mustered on shows between them, and the bottom
            /// gallery's rings and the Marshal's sign. Clearer where a rig has warmed the ice; never under snow; dim and grey-blue, so a danger marking always wins over it.</summary>
            static void HostBeneath(Func<Vector2,float> snow,Func<Vector2,float> scoured)
            {
                var rng=new System.Random(91);float R()=>(float)rng.NextDouble();int ranks=0;
                var cold=new Color(.13f,.2f,.38f);var warm=new Color(.42f,.27f,.18f);
                float Clear(Vector2 p){int f=FloorOf(p);float c=f==0?ZoneKit.Smooth(.45f,.75f,scoured(p)):1-ZoneKit.Smooth(.3f,.6f,snow(p));return c*ZoneKit.Smooth(.15f,1.4f,-FloorAt(p,out _));}
                // The dig and the bottom: a block every 12.4 m across the view and 7.8 m along it.
                var corners=new[]{new Vector2(-56,-12),new Vector2(232,-12),new Vector2(-56,246),new Vector2(232,246)};
                float r0=corners.Min(c=>Vector2.Dot(c,Across)),r1=corners.Max(c=>Vector2.Dot(c,Across)),a0=corners.Min(c=>Vector2.Dot(c,Away)),a1=corners.Max(c=>Vector2.Dot(c,Away));
                int band=0;
                for(float a=a0;a<a1;a+=7.8f,band++)for(float r=r0+(band%2)*6.2f;r<r1;r+=12.4f)
                {
                    var centre=Across*r+Away*a;int floor=FloorOf(centre);if(floor<1)continue;
                    float w=Warmth(centre,floor);int depth=floor==1?2:1;int row=Mathf.Max(0,depth-(w>.55f?1:0));
                    float alpha=floor switch{1=>.36f,2=>.36f,_=>InArena(centre,-1)?.36f:.42f}+.08f*w;
                    var tint=Color.Lerp(cold,warm,w*(floor==2?1:.7f));tint.a=alpha;
                    // Three ranks, each a little further down than the one in front of it: the back one softer and fainter.
                    for(int k=0;k<3;k++){var t=tint;t.a*=1-.12f*k;ranks+=Rank(centre+Away*((k-1)*1.9f)+Across*((R()-.5f)*.8f),Mathf.Min(2,row+(k==2?1:0)),t,Clear);}
                    // Deeper ranks between the blocks on the third level and the bottom, the host going on down into the blue.
                    if(floor>=3){var deep=new Color(cold.r,cold.g,cold.b,.2f);ranks+=Rank(centre+Across*6.2f+Away*3.9f,2,deep,Clear);}
                }
                // Under the Reach's scoured ice: ranks under each window over the host, and down the herd's line.
                var reach=new Color(cold.r,cold.g,cold.b,.4f);
                foreach(var (at,_) in IceWindows)for(int k=-1;k<=1;k++)ranks+=Rank(at+Away*(k*1.9f)+Across*(k*1.3f),1,reach,Clear);
                for(int i=1;i<HerdLine.Length;i++)
                {
                    var a=HerdLine[i-1];var b=HerdLine[i];float length=Vector2.Distance(a,b);
                    for(float d=3;d<length;d+=7){var p=Vector2.Lerp(a,b,d/length);foreach(float k in new[]{-.9f,.9f})ranks+=Rank(p+Away*k,2,reach,Clear);}
                }
                // Their kit, lying in the ice on the upper levels.
                for(int k=0;k<60;k++)
                {
                    var p=new Vector2(14+R()*120,170+R()*66);int floor=FloorOf(p);float turn=R()*360;if(floor!=1&&floor!=2)continue;
                    var across=new Vector2(Mathf.Cos(turn*Mathf.Deg2Rad),Mathf.Sin(turn*Mathf.Deg2Rad));
                    Beneath(p,across,2.4f,new Vector2(-across.y,across.x),2.4f,KitUv,new Color(cold.r,cold.g,cold.b,.3f),Clear);
                }
                // The paving they mustered on, under the third level and the bottom's mouth: joints on a 3.2 m grid.
                var paving=new Color(.2f,.24f,.33f,.2f);
                foreach(var (x0,x1,z0,z1) in new[]{(112f,152f,204f,236f),(166f,188f,202f,234f)})
                {
                    for(float x=Mathf.Ceil(x0/3.2f)*3.2f;x<x1;x+=3.2f)Line(new Vector2(x,z0),new Vector2(x,z1),.3f,paving,Clear);
                    for(float z=Mathf.Ceil(z0/3.2f)*3.2f;z<z1;z+=3.2f)Line(new Vector2(x0,z),new Vector2(x1,z),.3f,paving,Clear);
                }
                // The bottom gallery is their muster ground: rings about its centre, lines out from it, and the Marshal's sign under its post.
                foreach(float radius in new[]{6.5f,11.5f,16.5f})for(float b=0;b<360;b+=360/Mathf.Ceil(radius*6.283f/2.3f))
                {var p=Bearing(b,radius);var q=Bearing(b+360/Mathf.Ceil(radius*6.283f/2.3f),radius);Line(p,q,.34f,paving,Clear);}
                for(float b=15;b<360;b+=30)Line(Bearing(b,3.6f),Bearing(b,17.4f),.3f,paving,Clear);
                Beneath(Bearing(0,MarshalOut),Vector2.right,5.4f,Vector2.up,5.4f,SignUv,new Color(.2f,.25f,.36f,.42f),Clear);
                Debug.Log("FROZEN_HOST_RANKS_BENEATH "+ranks);
            }

            // ---------------------------------------------------------------- the host in the walls: forward ranks and their light
            static readonly List<(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color tint)> lit=new();
            /// <summary>A cold light inside a rank's ice, behind the host standing in it: the rank stands dark against it.</summary>
            static void LitFromWithin(Vector3 foot,float yaw,float wide,float tall,Color tint)
            {
                var turn=Quaternion.Euler(0,yaw,0);var across=turn*Vector3.right*wide*.5f;var low=foot+turn*Vector3.back*.85f+Vector3.up*.1f;var high=low+Vector3.up*tall;
                lit.Add((low-across,low+across,high+across,high-across,tint));
            }
            static void EmitLight()
            {
                var v=new List<Vector3>();var uv=new List<Vector2>();var c=new List<Color32>();var t=new List<int>();
                foreach(var (a,b,cc,d,tint) in lit)
                {
                    int i=v.Count;v.AddRange(new[]{a,b,cc,d});uv.AddRange(new[]{new Vector2(SoftUv.xMin,SoftUv.yMin),new Vector2(SoftUv.xMax,SoftUv.yMin),new Vector2(SoftUv.xMax,SoftUv.yMax),new Vector2(SoftUv.xMin,SoftUv.yMax)});
                    var baked=Baked(tint);c.AddRange(new[]{baked,baked,baked,baked});t.AddRange(new[]{i,i+2,i+1,i,i+3,i+2});
                }
                lit.Clear();if(v.Count==0)return;
                var mesh=new Mesh{name=Host+" rank light"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject("Ranks' light from within",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+Host+"-Rank-light.asset");
                var drawn=go.GetComponent<MeshRenderer>();drawn.sharedMaterial=InIce;drawn.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;drawn.receiveShadows=false;
            }
            /// <summary>A rank stood out from its wall in ice the Muster haven't cut: clear ice from the wall to a face 0.75 m in front of the host, so the rank stands at full height
            /// where the camera looks from the walked line; solid, in the bake; lit from within. <paramref name="facing"/> 180 for a north wall, 270 for an east one. Warmed: the
            /// second level's, where the Muster's thaw went over the ice and went cold before it let them go: the host stands out of it to the hips in ice frozen again, blue-white
            /// and rimed, with no glow (amber is kept for what wakes).</summary>
            static void ForwardRank(ZoneKit kit,string name,Rect plan,float facing,float y,int pose,int seed,bool warmed=false)
            {
                bool south=Mathf.Approximately(facing,180);float wide=south?plan.width:plan.height;var centre=plan.center;
                var line=south?new Vector2(centre.x,plan.yMin+.75f):new Vector2(plan.xMin+.75f,centre.y);int count=Mathf.Max(2,Mathf.FloorToInt((wide-.6f)/1.9f));
                if(!warmed)
                {
                    Block(kit,name+" ice",centre,y-.05f,south?new Vector3(plan.width,3.5f,plan.height):new Vector3(plan.height,3.5f,plan.width),south?0:90,IceBlock,true,false,seed);
                    Statues(name,line,y,facing,count,pose);LitFromWithin(new Vector3(line.x,y,line.y),facing,count*1.9f+.6f,3.1f,new Color(.36f,.62f,1f,.7f));return;
                }
                // Warmed and frozen again: the sill slumped into mounds while it ran, highest round each body (to its hips), lower between and behind, running off at the front, then
                // froze where it lay; rime over the mounds and over the bodies. Drawn as the mounds; solid as the box they fill, 0.75 m high.
                Statues(name,line,y-.25f,facing,count,pose,1.9f,new Color(.74f,.84f,1f));
                var slump=new ZoneKit.Shape();var turn=Quaternion.Euler(0,facing,0);var across=turn*Vector3.right;var front=turn*Vector3.forward;var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();
                for(int i=0;i<count;i++)
                {
                    var body=new Vector3(line.x,y,line.y)+across*(i-(count-1)*.5f)*1.9f;
                    slump.Slot(0).Lump(body+Vector3.up*.42f,new Vector3(.62f,.62f,.56f),seed+i,.25f,Color.white,Color.white,12,6,.25f,Quaternion.Euler(0,R()*60,0));
                    slump.Slot(0).Lump(body+across*.95f-front*.2f+Vector3.up*.12f,new Vector3(.7f,.36f,.75f),seed+i+40,.3f,Color.white,Color.white,10,5);
                    slump.Slot(0).Lump(body+front*.55f+Vector3.up*.02f,new Vector3(.75f,.14f,.5f),seed+i+80,.3f,Color.white,Color.white,10,4);
                }
                for(float t=-.5f;t<=.5f;t+=.25f)slump.Slot(0).Lump(new Vector3(centre.x,y,centre.y)+across*t*wide-front*.4f+Vector3.up*.18f,new Vector3(wide*.16f,.42f,(south?plan.height:plan.width)*.42f),seed+(int)(t*40)+120,.25f,Color.white,Color.white,10,5);
                var sill=kit.Built(name+" sill",slump,Refrozen);sill.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                ZoneKit.Block(sill,new Vector3(centre.x,y+.37f,centre.y),south?new Vector3(plan.width,.75f,plan.height):new Vector3(plan.height,.75f,plan.width),south?0:90,name+" sill (solid)");
                // Rime: white crusts on the mounds' tops and on each body's shoulders and helm.
                var rime=new ZoneKit.Shape();
                for(int i=0;i<count;i++)
                {
                    var body=new Vector3(line.x,y,line.y)+across*(i-(count-1)*.5f)*1.9f;
                    rime.Slot(0).Lump(body+Vector3.up*.98f,new Vector3(.5f,.12f,.44f),seed+i+200,.35f,SnowWhite,SnowShade,10,4);
                    rime.Slot(0).Lump(body+Vector3.up*1.5f,new Vector3(.34f,.08f,.22f),seed+i+240,.3f,SnowWhite,SnowShade,8,4);
                    rime.Slot(0).Lump(body+Vector3.up*1.78f,new Vector3(.15f,.07f,.15f),seed+i+280,.3f,SnowWhite,SnowShade,8,4);
                    rime.Slot(0).Lump(body+across*.95f-front*.2f+Vector3.up*.46f,new Vector3(.5f,.1f,.5f),seed+i+320,.35f,SnowWhite,SnowShade,10,4);
                }
                var crust=kit.Built(name+" rime",rime,SnowFloor);crust.transform.SetParent(sill.transform,true);
            }

            /// <summary>The zone's bed, made by tools/frozen_host_ambience.py (synthesised, nothing sourced): open wind and spindrift, the glacier groaning and cracking, the rigs
            /// thumping below, the herd far off. Measured: the file's RMS (mixed to one channel) is .0594, so .377 plays it at about .0224, the level every earlier bed plays at. Called
            /// from the zone's definition (HoarfellBuilder.Define); until the file is there the definition keeps whatever it holds.</summary>
            internal static void Air(ZoneDef zone)
            {
                var bed=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Ambience/AMB_FrozenHost.ogg");if(bed!=null){zone.ambientAudio=bed;zone.ambientVolume=.377f;}
            }

            // ---------------------------------------------------------------- the zone
            internal static void Build()
            {
                Materials();Poses();beneath.Clear();lit.Clear();
                var kit=ZoneKit.Ground(Host,Height,CliffTop);bool greybox=DevArgs.Has("-host-greybox");
                var scene=WorldBuilder.Begin(Host);
                kit.Terrain("Reach and dig",-58,230,-18,252,.5f,SnowFloor,IceWall,Tone,new Color(1,1,1),new Color(.72f,.82f,.94f));
                var steam=kit.Glow("Steam",true,new Color(1.05f,1.08f,1.12f,1));
                var drifts=new ZoneKit.Shape();var stones=new ZoneKit.Shape();var hides=new ZoneKit.Shape();var ventSolids=new GameObject("Vent pockets' stone");

                // ---- ways in and out, services
                WorldBuilder.Spawn("Arrival",kit.On(Arrival.x,Arrival.y));WorldBuilder.Spawn("Gap",kit.On(GapSpawn.x,GapSpawn.y));WorldBuilder.Spawn("Door",kit.On(DoorSpawn.x,DoorSpawn.y));
                // The Kept's gate onto the Reach: one of the road's ribs, stood on end in the snow; drawn only (its feet stand in the walls either side of the way).
                var arch=kit.Prop(ZoneKit.Kit("RibArch","AncelaneArch"),new Vector3(0,0,-11.5f),9,0,false);arch.name="Reach gate rib";ZoneKit.Fadeable(arch);
                var home=new GameObject("Reach gate — Lastlight");home.transform.position=kit.On(GateWay.x,GateWay.y,1);
                kit.Way(home,"Reach gate — Lastlight",Town,"Reach",null,null,3.6f,new Vector2(3.6f,-6.4f));
                var cradle=kit.Prop("DeckConsole",new Vector3(Cradle.x,0,Cradle.y),1.4f,80).AddComponent<RepairBay>();cradle.free=true;cradle.saveSpawn="Arrival";cradle.prompt="Herders' cradle — repair and save";
                ChapterOneStaging.CradleBay("Herders' cradle",cradle.gameObject,kit.On(Cradle.x,Cradle.y),kit.Y(Cradle.x,Cradle.y)+.02f,false,true);
                var anvil=kit.Prop("LatticeAnvil",new Vector3(Bench.x,0,Bench.y),1.8f,40).AddComponent<Lattice.World.Bench>();anvil.prompt="Fabricate — Lattice Anvil";

                // ---- the spear field: Sela and Taren at the gate, the army's spears up through the snow, and the first stalkers
                kit.Sign("THE WHITE REACH",5.5f,7.5f,1.8f);
                kit.Crossing("Out on the Reach",new Vector3(0,0,12),new Vector3(36,5,3),"hoar.reachEntered","SelaReachGate","Sela");
                Fight(kit,"Reach_Stalkers",new Vector2(0,32),new Vector2(36,20),null,("RimeStalker",1,FieldHides[0],160),("RimeStalker",1,FieldHides[1],200));
                int seed=0;foreach(var hide in FieldHides)Hide(kit,drifts,hide,40+seed++);
                var reachHail=new GameObject("Idra on the Reach",typeof(RadioHail)).GetComponent<RadioHail>();reachHail.transform.position=kit.On(2,44);
                reachHail.speaker="Idra";reachHail.situations=new[]{"hail.reach"};reachHail.heardFlag="hoar.reachHailed";reachHail.range=12;

                // ---- crevasse one and the west pocket: a Muster sledge through the crust, a vent with a stray, and motes round the warm ground
                Cornices(kit,0);
                Stash(kit,"hoar.reach.sledge","Muster supply sledge",Sledge,30,90,false,ProgressionBuilder.AnyPart(4,GearSlot.Frame,1),ProgressionBuilder.Drop("RimeGlass",3,1),ProgressionBuilder.Drop("RepairGel",2,1));
                Vent(kit,stones,hides,ventSolids,VentWest,4,0,0,330,steam,61);
                Stray(kit,1,StrayOne,140,new[]{new Vector2(-30,62),new Vector2(-14,60),new Vector2(-2,58)});
                Fight(kit,"Reach_West",new Vector2(-37,61),new Vector2(20,16),null,("FrostMotes",1,new Vector2(-42,57),0),("FrostMotes",1,new Vector2(-31,66),0));

                // ---- the herd: Idra's talk as it comes into view, the herd itself, and a silent line north of the gap that says it has been walked through
                kit.Crossing("The herd in the gap",new Vector3(0,0,57),new Vector3(40,5,3),"hoar.herdMet","IdraHerd","Idra");
                Herd(kit);
                kit.Crossing("Through the herd",new Vector3(8,0,95),new Vector3(24,5,3),"hoar.herdPassed",null,"Taren").situation="";

                // ---- the whiteout field: Idra's lesson (the first whiteout, with nothing about) and vent pocket one forty paces west; the stalkers' field, where the second closes, with
                // vent pocket two to the east; a stray in each pocket's steam
                kit.Crossing("The wind drops",new Vector3(0,0,100),new Vector3(44,5,3),"hoar.whiteoutMet","IdraWhiteout","Idra");
                kit.Sign("VENT POCKET",-24.5f,99.5f,1.6f);
                // Each pocket's windbreak stands round the side away from the way in (and from its stray), its cairn on the rim.
                Vent(kit,stones,hides,ventSolids,VentOne,6,-20,60,250,steam,62);Vent(kit,stones,hides,ventSolids,VentTwo,5.5f,95,175,300,steam,63);
                Stray(kit,2,StrayTwo,200,new[]{new Vector2(-30,98),new Vector2(-18,93),new Vector2(-12,90)});
                Stray(kit,3,StrayThree,240,new[]{new Vector2(30,112),new Vector2(22,104),new Vector2(12,99)});
                var white=Fight(kit,"Reach_Whiteout",new Vector2(6,120),new Vector2(40,22),"hoar.whiteoutMet",("RimeStalker",1,WhiteHides[0],180),("RimeStalker",1,WhiteHides[1],200),
                    ("FrostMotes",1,new Vector2(-6,119),0),("FrostMotes",1,new Vector2(18,114),0));
                foreach(var hide in WhiteHides)Hide(kit,drifts,hide,40+seed++);
                Weather(kit,white);
                Cornices(kit,1);

                // ---- the camp at the dig's head: the bench, the Muster's boiler with motes drawn to its heat, the loudhailer mast, and Vessa's first words
                // The camp's motes, drawn to the boiler's heat, fight on the trodden snow south of it: the boiler and its tank stand at the rim, north of the fight, never between it and the camera.
                // The fight's ground takes in the way the party walks through the camp (in at about 11, 148, out at 22, 160), so walking the route wakes it.
                Fight(kit,"Reach_Camp",new Vector2(26,152),new Vector2(30,16),null,("FrostMotes",1,new Vector2(28,147),0),("FrostMotes",1,new Vector2(38,154),0));
                kit.Crossing("The rig's glass",new Vector3(24,0,163),new Vector3(16,5,3),"hoar.vessaHeard","VessaLoudhailer","Vessa");
                Harvest(kit,"hoar_glass_0",new Vector2(-46,40),"RimeGlass","Gather Rime Glass",501);Harvest(kit,"hoar_glass_1",new Vector2(46,108),"RimeGlass","Gather Rime Glass",502);
                Harvest(kit,"hoar_glass_2",new Vector2(-44,140),"RimeGlass","Gather Rime Glass",503);

                // ---- the upper gallery: ranks frozen blue in the north wall, one amber (the Muster's own thaw: go round it); motes; rig one, whose heat reaches only the plug over the
                // ramp down (the rule, introduced with nothing to wake)
                kit.Sign("UPPER GALLERY",28,182.5f,1.8f);
                kit.Crossing("The first rank",new Vector3(24,0,181),new Vector3(14,5,3),"hoar.ranksSeen","IdraDigGalleries","Idra");
                foreach(var (x,pose) in new[]{(33.5f,2),(47f,3)})FrozenRank(kit,"Upper rank "+x,new Vector2(x,196.8f),L1,180,pose,700+(int)x);
                // West of the Muster's amber and east of it the face the Muster haven't cut yet stands out from the wall, so the host in it stands where the camera looks from the road.
                ForwardRank(kit,"Upper rank 19",Rect.MinMaxRect(13.6f,193.45f,22.75f,198.4f),180,L1,0,719);
                ForwardRank(kit,"Upper rank 26.5",Rect.MinMaxRect(22.75f,193.45f,30.25f,198.4f),180,L1,1,726);
                ForwardRank(kit,"Upper rank 54",Rect.MinMaxRect(50.2f,193.45f,58.4f,198.4f),180,L1,0,754);
                var amber=AmberRank(kit,"Upper amber rank",new Vector2(40.2f,196.8f),L1,180,1);
                var amberFight=Fight(kit,"Dig_Amber",new Vector2(40.2f,194.5f),new Vector2(14,7),null,("ThawedGuard",3,new Vector2(40.2f,196.4f),180));amberFight.spawners[0].radius=1.9f;
                Preview(kit,amberFight,new Vector2(40.2f,184),14);
                var rank1=new GameObject("Vessa: rank one",typeof(RadioHail)).GetComponent<RadioHail>();rank1.transform.position=kit.On(40,186);
                rank1.speaker="Vessa";rank1.situations=new[]{"hail.rank1"};rank1.heardFlag="hoar.rank1Hailed";rank1.needs=new[]{"hoar.ranksSeen"};rank1.range=12;
                Fight(kit,"Dig_Motes",new Vector2(52,183.5f),new Vector2(16,11),null,("FrostMotes",1,new Vector2(48,184),0),("FrostMotes",1,new Vector2(56,182),0));
                var plug1=Block(kit,"Plug one",new Vector2(66,187),L1-.1f,new Vector3(2.4f,PlugTall,20),0,IceBlock,true,true,801);
                var plug1Glow=Glow(kit,new Vector3(64,L1+2.6f,187),9,.6f,new Color(.5f,.75f,1f),"Plug one glow");
                Rig(kit,"Rig one",RigOne,135,RigOneHand,"hoar.rig1",new[]{kit.On(61.6f,193.2f,.15f),kit.On(63.4f,191,.15f),new Vector3(65,L1+.3f,189)},
                    new Bounds(new Vector3(66,L1,187),new Vector3(6,4,20)),plug1.GetComponentsInChildren<Renderer>(),new[]{plug1.GetComponent<Membrane>()},new[]{plug1Glow});
                // The wedge between the rig's north-west face and the upper rank's ice (1.4 to 2.2 m), packed to the rig's height and on to the north wall: the same pocket as rig
                // three's, which two hounds wedged themselves in (HoarfellWorldTests.NoThawRigLeavesAGapAHoundFitsInAndAHeroCantFightIn).
                Block(kit,"Rig one back ice",new Vector2(60.15f,196.65f),L1-.05f,new Vector3(3.7f,3,4.1f),0,IceBlock,true,false,807);

                // ---- the second level: Vessa's offer, her excavator and hounds on the floor; the left gallery (rig two: the plug across its mouth and the rank in its west wall, who step out
                // as the party walks in; then the gap they leave); the Excavators' cut straight on, with the Muster's tool chest
                kit.Crossing("Vessa's offer",new Vector3(86,0,184),new Vector3(3,5,26),"hoar.offerHeard","VessaSecondLevel","Idra");
                Fight(kit,"Dig_Second",new Vector2(104,181),new Vector2(20,22),"hoar.offerHeard",("MusterExcavator",1,new Vector2(110,186),250),("BridleHound",1,new Vector2(106,175.5f),280),("BridleHound",1,new Vector2(100,179),300));
                var excavators=new GameObject("Vessa: excavators",typeof(RadioHail)).GetComponent<RadioHail>();excavators.transform.position=kit.On(98,184);
                excavators.speaker="Vessa";excavators.situations=new[]{"hail.excavator"};excavators.heardFlag="hoar.excavatorHailed";excavators.needs=new[]{"hoar.offerHeard"};excavators.range=12;
                // The second level is where the Muster's thaw went over the ice and went cold before it let the host go: its ranks stand out of it to the hips, frozen in again, rimed.
                ForwardRank(kit,"Second rank",Rect.MinMaxRect(103.4f,193.2f,114.4f,198.4f),180,L2,2,727,true);
                ForwardRank(kit,"Warmed rank",Rect.MinMaxRect(80.3f,190.9f,84.1f,193.5f),180,L2,1,781,true);
                var plug2=Block(kit,"Plug two",new Vector2(90,200.3f),L2-.1f,new Vector3(13.6f,PlugTall,2.4f),0,IceBlock,true,true,802);
                var rankBlock=Block(kit,"Left gallery rank ice",new Vector2(85.3f,206.5f),L2-.05f,new Vector3(2.4f,3.5f,6.6f),0,IceBlock,true,true,803);
                var rankGlow=Glow(kit,new Vector3(86.5f,L2+2.2f,206.5f),8,.6f,new Color(.5f,.75f,1f),"Left gallery rank glow");
                var plug2Glow=Glow(kit,new Vector3(90,L2+2.6f,198.5f),9,.6f,new Color(.5f,.75f,1f),"Plug two glow");
                Rig(kit,"Rig two",RigTwo,250,RigTwoHand,"hoar.rig2",new[]{kit.On(99.4f,196.8f,.15f),kit.On(97.2f,199.4f,.15f),new Vector3(95.5f,L2+.3f,200)},
                    new Bounds(new Vector3(89,L2,204),new Vector3(14,4,12)),plug2.GetComponentsInChildren<Renderer>().Concat(rankBlock.GetComponentsInChildren<Renderer>()).ToArray(),
                    new[]{plug2.GetComponent<Membrane>(),rankBlock.GetComponent<Membrane>()},new[]{plug2Glow,rankGlow});
                var rank=Fight(kit,"Dig_Rank",new Vector2(90,207.5f),new Vector2(12,13),"hoar.rig2",("ThawedGuard",3,new Vector2(85.3f,206.5f),90));rank.spawners[0].radius=1.9f;
                Preview(kit,rank,RigTwoHand,18);
                kit.Crossing("A rank steps out",new Vector3(90,0,202.6f),new Vector3(12,5,3),"hoar.rankStepped",null,"Taren","hoar.rig2").situation="thaw";
                var rank2=new GameObject("Vessa: rank two",typeof(RadioHail)).GetComponent<RadioHail>();rank2.transform.position=kit.On(90,203);
                rank2.speaker="Vessa";rank2.situations=new[]{"hail.rank2"};rank2.heardFlag="hoar.rank2Hailed";rank2.needs=new[]{"hoar.rig2"};rank2.range=9;
                // The gap the rank leaves: a cleft in the gallery's west wall, behind where they stood, and the jump down it.
                var gap=kit.Discovery("The gap down",new Vector3(GapAt.x,0,GapAt.y),"Take the gap down","hoar.gapTaken","",null,"Sela",null,0,2.6f);
                gap.dialogueNode="SelaDigShortcut";gap.dialogueSpeaker="Sela";gap.requiredFlag="clear.Dig_Rank";gap.situation="";
                var leap=gap.gameObject.AddComponent<LeapDown>();leap.flag="hoar.gapTaken";leap.spawn="Gap";
                Cleft(kit,new Vector2(83.7f,206.5f),L2);
                Fight(kit,"Dig_Excavator",new Vector2(124,184),new Vector2(20,18),null,("MusterExcavator",1,new Vector2(128.5f,184),270));
                Stash(kit,"hoar.dig.tools","Muster tool chest",Tools,-10,140,false,ProgressionBuilder.AnyPart(4,GearSlot.Edge,1),ProgressionBuilder.Drop("HostAlloy",2,1),ProgressionBuilder.Drop("ChargeCell",2,1));
                ForwardRank(kit,"Cut rank",Rect.MinMaxRect(114.6f,191.4f,128.4f,194.3f),180,L2,1,741,true);

                // ---- the third level: motes, an excavator and hounds on arrival; rig three by the last ramp, whose heat takes the plug and the rank beside it and opens the hounds' pen
                // behind it: worked, it brings the thaw straight at the party (the rule with enemies)
                Fight(kit,"Dig_Third",new Vector2(124,219),new Vector2(22,26),null,("MusterExcavator",1,new Vector2(130,228),220),("BridleHound",1,new Vector2(122,212),300),
                    ("BridleHound",1,new Vector2(128,214),280),("FrostMotes",1,new Vector2(118,226),0),("FrostMotes",1,new Vector2(133,219),0));
                foreach(var (x,pose) in new[]{(117f,2),(124f,0),(131f,1)})FrozenRank(kit,"Third rank "+x,new Vector2(x,234.6f),L3,180,pose,760+(int)x);
                Stash(kit,"hoar.dig.third","Ancelane coffer",ThirdCache,15,160,true,ProgressionBuilder.AnyPart(4,GearSlot.Module,1),ProgressionBuilder.Drop("HostAlloy",2,1),ProgressionBuilder.Drop("SyncTonic",1,.5f));
                Harvest(kit,"hoar_alloy_0",new Vector2(138,207),"HostAlloy","Prise out Host Alloy",511);Harvest(kit,"hoar_alloy_1",new Vector2(56,178.5f),"HostAlloy","Prise out Host Alloy",512);
                var plug3=Block(kit,"Plug three",new Vector2(152,219),L3-.1f,new Vector3(2.4f,PlugTall,20),0,IceBlock,true,true,804);
                var rank3Block=Block(kit,"Third rank ice",new Vector2(138.5f,234.6f),L3-.05f,new Vector3(6.6f,3.5f,2.4f),0,IceBlock,true,true,805);
                var rank3Glow=Glow(kit,new Vector3(138.5f,L3+2.2f,233),8,.6f,new Color(.5f,.75f,1f),"Third rank glow");
                var plug3Glow=Glow(kit,new Vector3(150,L3+2.6f,219),9,.6f,new Color(.5f,.75f,1f),"Plug three glow");
                Rig(kit,"Rig three",RigThree,200,RigThreeHand,"hoar.rig3",new[]{kit.On(147.6f,231.6f,.15f),kit.On(149.6f,227,.15f),new Vector3(151,L3+.3f,224)},
                    new Bounds(new Vector3(146,L3,226),new Vector3(20,4,20)),plug3.GetComponentsInChildren<Renderer>().Concat(rank3Block.GetComponentsInChildren<Renderer>()).ToArray(),
                    new[]{plug3.GetComponent<Membrane>(),rank3Block.GetComponent<Membrane>()},new[]{plug3Glow,rank3Glow});
                // The gap between the rig's back and the north wall (1.2 to 1.8 m, the wall at z 236), packed with old ice to the rig's height and on to the east wall: both of the rank's
                // hounds wedged themselves in it, one on the other's back, round the rig's corner from the lead, and the fight never cleared (nov14-recon24 s35;
                // HoarfellWorldTests.NoThawRigLeavesAGapAHoundFitsInAndAHeroCantFightIn).
                Block(kit,"Rig three back ice",new Vector2(149.2f,235.7f),L3-.05f,new Vector3(6.8f,3,2.2f),0,IceBlock,true,false,806);
                var rank3=Fight(kit,"Dig_Rank3",new Vector2(146,221),new Vector2(18,22),"hoar.rig3",("ThawedGuard",3,new Vector2(138.5f,234.4f),180),("BridleHound",1,new Vector2(158,215),270),("BridleHound",1,new Vector2(158,223),270));
                rank3.spawners[0].radius=1.9f;Preview(kit,rank3,RigThreeHand,26);
                kit.Crossing("The thaw comes",new Vector3(RigThreeHand.x,0,RigThreeHand.y),new Vector3(6,5,6),"hoar.rank3Stepped",null,"Taren","hoar.rig3").situation="thaw";

                // ---- the bottom gallery's mouth: where the gap lands; the loading winch and Vessa's slate; the checkpoint, a free cradle and a bench; Idra joins
                kit.Sign("LOADING WINCH",170.5f,225.5f,1.6f);
                kit.Crossing("Vessa's slate",new Vector3(172,0,218),new Vector3(3,5,30),"hoar.slateRead","SelaMusterSlate","Sela");
                var doorBay=kit.Prop("DeckConsole",new Vector3(DoorCradle.x,0,DoorCradle.y),1.4f,-30).AddComponent<RepairBay>();doorBay.free=true;doorBay.saveSpawn="Door";doorBay.prompt="Dig cradle — repair and save";
                ChapterOneStaging.CradleBay("Dig cradle",doorBay.gameObject,kit.On(DoorCradle.x,DoorCradle.y),kit.Y(DoorCradle.x,DoorCradle.y)+.02f,false,false);
                var deep=kit.Prop("LatticeAnvil",new Vector3(AnteBench.x,0,AnteBench.y),1.8f,200).AddComponent<Lattice.World.Bench>();deep.prompt="Fabricate — Lattice Anvil";
                kit.Sign("BOTTOM GALLERY",181.5f,207,1.8f);
                var joins=kit.Crossing("The bottom gallery",new Vector3(182,0,218),new Vector3(3,5,30),"hoar.idraJoined","IdraJoins","Idra");joins.report="BottomGallery";
                WinchWay(kit);
                AmberRank(kit,"Mouth amber rank",new Vector2(169.6f,231.6f),Bottom,180,2);
                // The host in the mouth's east wall either side of the door, at full height beside the walked line.
                ForwardRank(kit,"Mouth east rank",Rect.MinMaxRect(185.6f,203.6f,188.6f,212.4f),270,Bottom,3,861);
                ForwardRank(kit,"Mouth east rank north",Rect.MinMaxRect(185.6f,223.4f,188.6f,227.8f),270,Bottom,1,862);

                // ---- the bottom gallery: the Host Marshal's room, built to the boss arena contract
                Arena(kit,greybox);

                // ---- Idra, ahead
                Ahead(kit);

                if(!greybox)
                {
                    kit.Built("Snow drifts",drifts,SnowFloor);var heaped=kit.Built("Vent stones",stones,Rock);kit.Built("Vent hides",hides,Timber,Tarp,Cloth);
                    ventSolids.transform.SetParent(heaped.transform,true);foreach(var box in ventSolids.GetComponentsInChildren<BoxCollider>())box.gameObject.isStatic=true;
                }
                if(!greybox)Dress(kit,steam);

                // ---- limits: the glacier's walls are the stop; a box on each side keeps a body from finding a way up one.
                WorldBuilder.Boundary("South limit",new Vector3(86,4,-17),new Vector3(290,30,2));WorldBuilder.Boundary("North limit",new Vector3(86,4,251),new Vector3(290,40,2));
                WorldBuilder.Boundary("West limit",new Vector3(-57,4,117),new Vector3(2,40,272));WorldBuilder.Boundary("East limit",new Vector3(229,4,117),new Vector3(2,40,272));

                // ---- navigation, with every plug and block thawed and every seal open: what is shut at first is play's business.
                var shut=UnityEngine.Object.FindObjectsByType<Membrane>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(m=>m.gameObject).ToList();
                var were=shut.Select(g=>g.activeSelf).ToArray();foreach(var g in shut)g.SetActive(false);
                kit.BakeNavigation(new Bounds(new Vector3(86,0,117),new Vector3(290,50,272)),Arrival,MustReach());
                for(int i=0;i<shut.Count;i++)shut[i].SetActive(were[i]);
                if(!greybox)Ground(kit);
                // For stills of the dig as it is walked (never saved as the zone: rebuild without it): -host-thawed stands every plug and rank's ice melted.
                if(DevArgs.Has("-host-thawed"))foreach(var heat in UnityEngine.Object.FindObjectsByType<IceThaw>(FindObjectsSortMode.None))foreach(var plug in heat.melts)if(plug!=null){plug.gameObject.SetActive(false);Debug.Log("FROZEN_HOST_STILL_THAWED "+plug.name);}
                WorldBuilder.Save(scene,Host);Debug.Log("FROZEN_HOST_OK");
            }
            /// <summary>Every place a player must stand: each prompt, each fight's floor, each spawn, the arena's rings.</summary>
            static List<Vector2> MustReach()
            {
                var reach=new List<Vector2>{Arrival,new(0,-8.5f),Cradle+new Vector2(1.6f,0),new(0,12),new(0,32),new(2,44),MainBridge,new(2,56),WestBridge,new(-30,56),Sledge+new Vector2(0,-1.6f),
                    StrayOne+new Vector2(0,-1.6f),new(-37,61),new(0,57),new(2,66),new(4,81),new(8,95),new(0,100),VentOne,StrayTwo+new Vector2(0,-1.6f),VentTwo,StrayThree+new Vector2(0,-1.6f),
                    new(6,120),new(6,131),new(6,141),Bench+new Vector2(1.6f,0),new(30,150),new(24,163),new(24,172),new(24,181),new(40,184),new(40,192),new(52,183.5f),RigOneHand,
                    new(72,187),new(86,184),new(104,181),RigTwoHand,new(90,203),new(90,214),GapAt+new Vector2(1.4f,0),new(104,214),new(124,184),Tools+new Vector2(0,-1.6f),
                    new(124,219),ThirdCache+new Vector2(0,-1.6f),RigThreeHand,new(140,226),new(158,219),GapSpawn,new(172,218),DoorSpawn,DoorCradle+new Vector2(1.6f,0),
                    AnteBench+new Vector2(-1.6f,0),new(182,218),WinchHand,Passage,C,
                    new(-46,40.5f),new(46,108.5f),new(-44,140.5f),new(138,207.5f),new(56,180)};
                for(int k=0;k<12;k++)reach.Add(Bearing(k*30,17));
                return reach;
            }

            // ---------------------------------------------------------------- the dig's things
            /// <summary>A rank already going amber in the Muster's own thaw: the wall behind it lit amber and running wet, three of the host standing in front of it in what ice is left (drawn,
            /// not solid), a warm light. Blue again once the Marshal sleeps.</summary>
            static IceThaw AmberRank(ZoneKit kit,string name,Vector2 foot,float y,float yaw,int pose)
            {
                var back=Block(kit,name+" ice",foot,y-.05f,new Vector3(6.6f,3.6f,.9f),yaw,AmberBlock,false,false,900+(int)foot.x);
                back.transform.position+=Quaternion.Euler(0,yaw,0)*Vector3.forward*-.9f;
                var turn=Quaternion.Euler(0,yaw,0);
                var glow=Glow(kit,new Vector3(foot.x,y+2.4f,foot.y)+turn*Vector3.forward*-1.5f,9,2.2f,new Color(1,.6f,.28f),name+" glow");
                var heat=new GameObject(name+" (thawing)",typeof(IceThaw)).GetComponent<IceThaw>();heat.transform.position=new Vector3(foot.x,y,foot.y);
                heat.flag="";heat.ice=back.GetComponentsInChildren<Renderer>();heat.glow=new[]{glow};heat.refreezeFlag="hoar.marshalAsleep";
                // The upper gallery's rank is the encounter's own bodies, shown inert until they wake; the mouth's is only ever seen.
                if(!name.StartsWith("Upper"))Statues(name,foot,y,yaw,3,pose,1.9f,new Color(1,.86f,.74f));
                return heat;
            }
            /// <summary>The gap a rank leaves: a dark cleft down the wall behind where it stood, going down into the blue.</summary>
            static void Cleft(ZoneKit kit,Vector2 at,float y)
            {
                var shape=new ZoneKit.Shape();var deep=new Color(.08f,.14f,.24f);var lip=new Color(.3f,.42f,.6f);
                var p=new Vector3(at.x,y,at.y);
                shape.Slot(0).Quad(p+new Vector3(.05f,-.1f,-1.2f),p+new Vector3(.05f,-.1f,1.2f),p+new Vector3(.05f,3.2f,.8f),p+new Vector3(.05f,3.2f,-.7f),Vector3.right,deep,.5f);
                shape.Slot(0).Quad(p+new Vector3(.12f,-.1f,-1.5f),p+new Vector3(.12f,-.1f,-1.2f),p+new Vector3(.12f,3.2f,-.7f),p+new Vector3(.12f,3.4f,-1.1f),Vector3.right,lip,.5f);
                shape.Slot(0).Quad(p+new Vector3(.12f,-.1f,1.2f),p+new Vector3(.12f,-.1f,1.5f),p+new Vector3(.12f,3.4f,1.1f),p+new Vector3(.12f,3.2f,.8f),Vector3.right,lip,.5f);
                kit.Built("The gap",shape,IceWall);
            }
            /// <summary>The loading winch at the mouth's north wall: a gantry of the Muster's over a cage, its cable going up the wall to the glacier's top, Vessa's slate hung on it. Once the
            /// Marshal sleeps it carries the party up and home.</summary>
            static void WinchWay(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();var foot=kit.On(Winch.x,Winch.y+1.6f);
                foreach(int side in new[]{-1,1})shape.Slot(0).Beam(foot+new Vector3(side*2.4f,0,0),foot+new Vector3(side*2.1f,6.2f,0),.32f,.32f,IronDark,IronPale);
                shape.Slot(0).Beam(foot+new Vector3(-2.6f,6.1f,0),foot+new Vector3(2.6f,6.1f,0),.36f,.4f,IronDark,Hazard);
                // The cage: four posts, rails, a floor grate.
                for(int k=0;k<4;k++){var c=foot+new Vector3(k%2==0?-1.1f:1.1f,0,k<2?-1.4f:.4f);shape.Slot(0).Post(c,2.6f,.07f,.07f,6,IronDark,IronPale);}
                foreach(float up in new[]{.1f,1.3f,2.55f}){shape.Slot(0).Beam(foot+new Vector3(-1.1f,up,-1.4f),foot+new Vector3(1.1f,up,-1.4f),.06f,.06f,Hazard);shape.Slot(0).Beam(foot+new Vector3(-1.1f,up,.4f),foot+new Vector3(1.1f,up,.4f),.06f,.06f,IronPale);}
                shape.Slot(1).Rope(foot+new Vector3(0,6.1f,0),foot+new Vector3(0,2.7f,-.5f),.02f,.04f,new Color(.3f,.28f,.26f));
                shape.Slot(1).Rope(foot+new Vector3(0,6.3f,0),foot+new Vector3(0,22,2.4f),.3f,.05f,new Color(.3f,.28f,.26f),5,14);
                // The slate: a dark board on the gantry's leg, chalked.
                shape.Slot(2).Box(foot+new Vector3(-2.15f,1.7f,-.3f),new Vector3(.9f,.7f,.06f),Quaternion.Euler(0,10,0),new Color(.24f,.26f,.3f));
                var body=kit.Built("Loading winch",shape,Iron,Iron,Timber);
                ZoneKit.BlockAlong(body,foot+new Vector3(-2.4f,1,0),foot+new Vector3(-2.4f,1,.1f),.6f,2.6f,"Winch leg");ZoneKit.BlockAlong(body,foot+new Vector3(2.4f,1,0),foot+new Vector3(2.4f,1,.1f),.6f,2.6f,"Winch leg");
                ZoneKit.Block(body,foot+new Vector3(0,1.3f,-.5f),new Vector3(2.4f,2.6f,2),0,"Winch cage");
                var hand=new GameObject("Ride the winch up — Lastlight");hand.transform.position=kit.On(WinchHand.x,WinchHand.y,1);
                kit.Way(hand,"Ride the winch up — Lastlight",Town,"Reach","hoar.marshalAsleep","Put the Host Marshal to sleep first",3.2f,new Vector2(179.5f,226));
            }

            // ---------------------------------------------------------------- the bottom gallery (docs/maps/FrozenHost.md, "Boss arena contract")
            static void Arena(ZoneKit kit,bool greybox)
            {
                // The door: Idra's line as the Marshal steps out of the wall, the seal across the passage, the encounter over the floor.
                var confront=kit.Crossing("The Marshal steps out",new Vector3(Passage.x-2.5f,0,Passage.y),new Vector3(3,5,8),"hoar.marshalSeen","IdraMarshalWakes","Idra","hoar.idraJoined");confront.saveSpawn="Door";
                var seal=WorldBuilder.Membrane("Bottom gallery seal",kit.On(Passage.x+1,Passage.y,1.7f),8.8f,true,3.6f);seal.transform.rotation=Quaternion.Euler(0,90,0);
                foreach(var filter in seal.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(filter);
                foreach(var view in seal.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(view);
                var sealBox=seal.GetComponent<BoxCollider>();var sealCarve=seal.gameObject.AddComponent<NavMeshObstacle>();sealCarve.shape=NavMeshObstacleShape.Box;sealCarve.center=sealBox.center;sealCarve.size=sealBox.size;sealCarve.carving=true;sealCarve.carveOnlyStationary=false;sealCarve.enabled=false;
                // The seal is a wall of ice rising across behind the party as the fight starts, gone when it ends.
                var wall=new ZoneKit.Shape();var x=Passage.x+1;
                for(int k=0;k<9;k++){float z=Passage.y-4.2f+k*1.05f;wall.Slot(0).Post(new Vector3(x+(k%2)*.2f,Bottom-.2f,z),3.2f+(k%3)*.4f,.5f,.12f,6,Color.white,new Color(.8f,.9f,1));}
                var sealIce=kit.Built("Bottom gallery seal ice",wall,IceBlock);sealIce.transform.SetParent(seal.transform,true);sealIce.GetComponent<Renderer>().enabled=false;
                var duel=WorldBuilder.Encounter("HostMarshal",new Vector3(C.x,Bottom+3,C.y),new Vector3(36,8,36),Enemy("HostMarshal"),1,new[]{seal});
                var post=duel.spawners[0];var spawnAt=Bearing(0,MarshalOut);post.transform.SetPositionAndRotation(new Vector3(spawnAt.x,Bottom,spawnAt.y),Quaternion.Euler(0,250,0));post.faceAsPlaced=true;post.radius=0;
                duel.requiredFlag="hoar.marshalSeen";duel.checkpointSpawn="Door";
                var door=new GameObject("Host Marshal preview (the door)");door.transform.position=kit.On(Passage.x,Passage.y);duel.previewPoint=door.transform;duel.previewRadius=Vector2.Distance(Passage,spawnAt)+2;confront.previewEncounter=duel;
                var asleep=new GameObject("The gallery after the Marshal",typeof(AfterTalk)).GetComponent<AfterTalk>();asleep.flag="bossdown.HostMarshal";asleep.node="IdraMarshalSleeps";asleep.speaker="Idra";asleep.heardFlag="hoar.marshalAsleep";asleep.delay=3;
                // Its niche in the north wall, and on a later visit the Marshal itself kneeling in front of it, put back to sleep.
                Kneeling(kit,spawnAt);
                // The three couplings, each with its third of the wall (thawing amber and refrozen blue, the soldiers standing in it) and the floor its rank steps out on.
                var couplingSteam=kit.Glow("Coupling steam",true,new Color(1.05f,1.02f,.98f,1));
                for(int i=0;i<3;i++)Coupling(kit,i+1,CouplingBearings[i],couplingSteam);
                // Cold lamps of the Muster's that keep the gallery readable once every coupling is out: on the south rim and at the door.
                foreach(var (bearing,radius) in new[]{(150f,17.6f),(215f,17.6f)}){var at=Bearing(bearing,radius);WorkLamp(kit,at,bearing+180);}
                Ranks(kit);
            }
            /// <summary>The bearings of each coupling's stretch of the wall, as the contract has them: 300 to 0, 0 to 60, 60 to 120.</summary>
            static (float from,float to) Stretch(int index)=>index switch{1=>(300,360),2=>(0,60),_=>(60,120)};
            static void Coupling(ZoneKit kit,int index,float bearing,Material steam)
            {
                var at=Bearing(bearing,CouplingOut);var foot=new Vector3(at.x,Bottom,at.y);
                var root=new GameObject("Thaw coupling "+index);root.transform.SetPositionAndRotation(foot,Quaternion.LookRotation(new Vector3(C.x-at.x,0,C.y-at.y)));
                var whole=new GameObject("Coupling running");whole.transform.SetParent(root.transform,false);
                var rig=kit.Prop(ZoneKit.Kit("ThawRig","HushwellVent"),foot+Vector3.up*.2f,2.8f,bearing+180,false,false);rig.transform.SetParent(whole.transform,true);
                var amberLight=Glow(kit,foot+Vector3.up*2.6f,10,2.6f,new Color(1,.6f,.26f),"Coupling light");amberLight.transform.SetParent(whole.transform,true);
                // Steam off the running rig, thin enough to see through: ninety additive puffs up to two metres across, in a box five by two and a half, stacked to a white glare over
                // the rig that hid whoever stood at it, which is where a coupling is broken from (the first player run, nov14-recon5 r36, stills 056 and 061; and with the wall asleep too,
                // since the steam never stopped). A rig iced over gives off none.
                var puff=kit.Drift("Coupling "+index+" steam",foot+Vector3.up*2.2f,new Vector3(2.6f,1.6f,1.4f),steam,new Color(1,.94f,.88f,.1f),20,.5f,1.1f,.35f,5);puff.transform.SetParent(whole.transform,true);
                // Broken: the same rig iced over and dark.
                var broken=new GameObject("Coupling iced");broken.transform.SetParent(root.transform,false);
                var dead=kit.Prop(ZoneKit.Kit("ThawRig","HushwellVent"),foot+Vector3.up*.2f,2.8f,bearing+180,false,false);dead.transform.SetParent(broken.transform,true);dead.AddComponent<PropTint>().tint=new Color(.55f,.66f,.82f);
                var rime=new ZoneKit.Shape();for(int k=0;k<7;k++){var d=Quaternion.Euler(0,k*51,0)*Vector3.forward;rime.Slot(0).Lump(d*(.7f+.2f*(k%2))+Vector3.up*(.4f+.35f*k),new Vector3(.7f,.4f,.6f),k+index*10,.25f,Color.white,new Color(.8f,.9f,1),10,5);}
                var crust=kit.Built("Coupling "+index+" rime",rime,IceBlock);crust.transform.SetParent(broken.transform,false);crust.transform.localPosition=Vector3.zero;crust.transform.localRotation=Quaternion.identity;
                var health=root.AddComponent<Health>();health.id="Thaw coupling";
                var sphere=root.AddComponent<SphereCollider>();sphere.isTrigger=true;sphere.radius=1.3f;sphere.center=Vector3.up*1.5f;root.AddComponent<Hurtbox>().owner=health;
                // The iron skid it stands on, whichever state it is in; solid as the rig is, so nobody walks through it, and carved, not baked, so the bake leaves a floor round it. The
                // box hangs under the skid: a box with nothing drawn over it is what the invisible-wall check takes for a wall nobody can see.
                var skid=new ZoneKit.Shape();skid.Slot(0).Box(new Vector3(0,.1f,0),new Vector3(2.3f,.2f,2.3f),Quaternion.identity,IronDark,IronPale);
                foreach(int side in new[]{-1,1})skid.Slot(0).Box(new Vector3(side*1.05f,.22f,0),new Vector3(.16f,.12f,2.5f),Quaternion.identity,Hazard);
                var plate=kit.Built("Coupling "+index+" skid",skid,Iron);plate.transform.SetParent(root.transform,false);plate.transform.localPosition=Vector3.zero;plate.transform.localRotation=Quaternion.identity;
                var solid=new GameObject("Coupling body",typeof(BoxCollider),typeof(NavMeshObstacle));solid.transform.SetParent(plate.transform,false);solid.transform.localPosition=Vector3.up*1.3f;solid.GetComponent<BoxCollider>().size=new Vector3(1.8f,2.6f,1.8f);
                var round=solid.GetComponent<NavMeshObstacle>();round.shape=NavMeshObstacleShape.Box;round.size=new Vector3(1.8f,2.6f,1.8f);round.carving=true;
                var coupling=root.AddComponent<ThawCoupling>();coupling.index=index;coupling.settledBy="HostMarshal";coupling.defeatedFlag="hoar.coupling"+index;coupling.whole=whole;coupling.broken=broken;
                // Its third of the wall: the ice there in amber with its light (running) and in blue (refrozen), the soldiers standing in it either way.
                var (from,to)=Stretch(index);
                var thawing=WallIce(kit,"Coupling "+index+" wall thawing",from,to,AmberBlock,new Color(1,.6f,.28f),2.4f);
                var refrozen=WallIce(kit,"Coupling "+index+" wall refrozen",from,to,IceBlock,new Color(.5f,.75f,1f),.6f);
                thawing.transform.SetParent(root.transform,true);refrozen.transform.SetParent(root.transform,true);refrozen.SetActive(false);broken.SetActive(false);
                coupling.thawing=thawing;coupling.refrozen=refrozen;
                // Where its rank steps out of the wall: r 16 at its bearing and 16 either side.
                coupling.ranks=new[]{-16f,0,16f}.Select(off=>{var p=Bearing(bearing+off,RankOut);var t=new GameObject("Coupling "+index+" rank "+off).transform;t.position=new Vector3(p.x,Bottom,p.y);
                    t.rotation=Quaternion.LookRotation(new Vector3(C.x-p.x,0,C.y-p.y));t.SetParent(root.transform,true);return t;}).ToArray();
            }
            /// <summary>A stretch of the bottom gallery's ice shelf (r 18.3 to 20.4) between two bearings: a curved band of clear ice 3.8 m tall, and a light behind it.</summary>
            static GameObject WallIce(ZoneKit kit,string name,float from,float to,Material look,Color light,float intensity)
            {
                var shape=new ZoneKit.Shape();int steps=Mathf.CeilToInt((to-from)/5);var rng=new System.Random(name.Length*31+(int)from);
                for(int k=0;k<steps;k++)
                {
                    float a=from+(k+.5f)*(to-from)/steps;var mid=Bearing(a,19.35f);var outward=(mid-C).normalized;float yaw=Mathf.Atan2(outward.x,outward.y)*Mathf.Rad2Deg;
                    shape.Slot(0).Box(new Vector3(mid.x,Bottom+1.85f+(float)rng.NextDouble()*.15f,mid.y),new Vector3(19.35f*(to-from)/steps*Mathf.Deg2Rad+.12f,3.8f+(float)rng.NextDouble()*.5f,2.1f),Quaternion.Euler(0,yaw,0),Color.white,Color.white,.6f);
                }
                var go=kit.Built(name,shape,look);go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                // Only the refrozen ice brings a light of its own: a running stretch glows by its own amber and its coupling's lamp, which keeps the gallery to the contract's six lights
                // (three couplings and two cold lamps while it runs; three cold stretches and the two lamps once all are broken).
                // In front of the ice and over the heads, never in it: at r 19.6, 3 m up, the light stood inside its own stretch of translucent ice and lit it from within into a white
                // glare right behind the rig, which is where a hero stands to break a coupling and where the first player run lost him (nov14-recon5 r36, stills 056 and 061).
                if(intensity<1){var centre=Bearing((from+to)*.5f,16.6f);var glow=Glow(kit,new Vector3(centre.x,Bottom+5.2f,centre.y),11,intensity*.75f,light,name+" light");glow.transform.SetParent(go.transform,true);}
                return go;
            }
            /// <summary>The host standing in the bottom gallery's shelf ice, all the way round its north and east (a rank of three every 10 degrees), and the shelf's solid face at r 18.2 so
            /// nobody walks into the ice; in the bake.</summary>
            static void Ranks(ZoneKit kit)
            {
                var holder=new GameObject("Bottom gallery shelf");
                for(float b=304;b<=476;b+=10){var at=Bearing(b,19.4f);var toC=C-at;float yaw=Mathf.Atan2(toC.x,toC.y)*Mathf.Rad2Deg;
                    Statues("Shelf rank "+Mathf.RoundToInt(Mathf.Repeat(b,360)),at,Bottom,yaw,3,(int)(b/10)%4,1.4f).transform.SetParent(holder.transform,true);}
                for(float b=296;b<=484;b+=6){var a=Bearing(b,19.3f);var outward=(a-C).normalized;ZoneKit.Block(holder,new Vector3(a.x,Bottom+2,a.y),new Vector3(2.2f,4,2.2f),Mathf.Atan2(outward.x,outward.y)*Mathf.Rad2Deg,"Shelf ice");}
                foreach(var box in holder.GetComponentsInChildren<BoxCollider>())box.gameObject.isStatic=true;
            }
            /// <summary>The Marshal's niche in the north wall, and on a later visit the Marshal kneeling in front of it, iced over: shown at load once it is down (Whereabouts, never live,
            /// so it doesn't appear beside the body the fight leaves).</summary>
            static void Kneeling(ZoneKit kit,Vector2 at)
            {
                var prefab=ZoneKit.Prefab("Enemies","HostMarshal");if(prefab==null)return;
                // Knelt as the fight leaves it (BossController.Kneel): its clip's buckle, sunk and bowed into the frost.
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.name="The Host Marshal, asleep";
                body.transform.SetPositionAndRotation(new Vector3(at.x,Bottom-BossController.MarshalTune.KneelSink,at.y),Quaternion.Euler(0,250,0)*Quaternion.Euler(BossController.MarshalTune.KneelBow,0,0));
                var animator=body.GetComponentInChildren<Animator>();var clips=animator!=null&&animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:Array.Empty<AnimationClip>();
                var down=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("death"))??clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("down"));
                PrefabUtility.UnpackPrefabInstance(body,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                if(down!=null&&animator!=null)down.SampleAnimation(animator.gameObject,down.length*.28f);
                foreach(var behaviour in body.GetComponentsInChildren<MonoBehaviour>())UnityEngine.Object.DestroyImmediate(behaviour);
                foreach(var rig in body.GetComponentsInChildren<Animator>())UnityEngine.Object.DestroyImmediate(rig);
                foreach(var shape in body.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(shape);
                body.AddComponent<PropTint>().tint=new Color(.7f,.82f,1f);
                var where=new GameObject("The Host Marshal, asleep (from its fall)",typeof(Whereabouts)).GetComponent<Whereabouts>();where.from="bossdown.HostMarshal";where.targets=new[]{body};where.live=false;
                body.SetActive(false);
            }
            /// <summary>A work lamp of the Muster's: a tripod of iron, a caged lamp on it, a cold white light.</summary>
            static void WorkLamp(ZoneKit kit,Vector2 at,float yaw)
            {
                var shape=new ZoneKit.Shape();var foot=kit.On(at.x,at.y);
                for(int k=0;k<3;k++){var d=Quaternion.Euler(0,yaw+k*120,0)*Vector3.forward*.55f;shape.Slot(0).Beam(foot+d,foot+Vector3.up*2.3f,.06f,.06f,IronDark,IronPale);}
                shape.Slot(1).Lump(foot+Vector3.up*2.5f,new Vector3(.22f,.18f,.22f),(int)at.x,.02f,Color.white,Color.white,8,5);
                var lamp=kit.Built("Work lamp "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),shape,Iron,Lamp);
                ZoneKit.Block(lamp,foot+Vector3.up*1.2f,new Vector3(.5f,2.4f,.5f),0,"Lamp post");
                kit.Lamp(new Vector3(at.x,2.6f,at.y),12,2.2f,new Color(.86f,.92f,1f));
            }

            // ---------------------------------------------------------------- the Reach's things
            /// <summary>The crevasse's lips: a cornice of packed snow along each edge (and along each bridge's sides), and a knee-high block under it that stops a walker at the edge.</summary>
            static void Cornices(ZoneKit kit,int which)
            {
                var (z0,amp,freq,phase,half,bridges)=Crevasses[which];var shape=new ZoneKit.Shape();var holder=new GameObject("Crevasse "+(which+1)+" lips");
                bool OnBridge(float x,float pad)=>bridges.Any(b=>Mathf.Abs(x-b.x)<b.wide*.5f+pad);
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3? last=null;
                    for(float x=-52;x<=52;x+=1.2f)
                    {
                        if(OnBridge(x,-.2f)){last=null;continue;}
                        float z=z0+amp*Mathf.Sin(x*freq+phase)+side*(half+.25f);var p=new Vector3(x,Height(new Vector2(x,z-side*.6f))+.02f,z);
                        shape.Slot(0).Lump(p+new Vector3(0,.14f,side*.1f),new Vector3(1.05f,.3f,.7f),(int)(x*7)+side,.3f,SnowWhite,SnowShade,10,5,.25f,Quaternion.Euler(0,(x*37)%25-12,0));
                        if(last is Vector3 l)ZoneKit.BlockAlong(holder,l+Vector3.up*.5f,p+Vector3.up*.5f,.6f,1.1f,"Crevasse lip");
                        last=p;
                    }
                }
                foreach(var (bx,wide) in bridges)foreach(int side in new[]{-1,1})
                {
                    float x=bx+side*(wide*.5f-.25f);float za=z0+amp*Mathf.Sin(x*freq+phase)-half-.3f,zb=z0+amp*Mathf.Sin(x*freq+phase)+half+.3f;
                    var a=new Vector3(x,Height(new Vector2(x-side*.6f,za))+.02f,za);var b=new Vector3(x,Height(new Vector2(x-side*.6f,zb))+.02f,zb);
                    for(float t=0;t<=1.01f;t+=.34f)shape.Slot(0).Lump(Vector3.Lerp(a,b,t)+Vector3.up*.1f,new Vector3(.55f,.26f,1.6f),(int)(bx*5+t*20)+side,.35f,SnowWhite,SnowShade,12,5,.25f,Quaternion.Euler(0,(t*40)-20,0));
                    ZoneKit.BlockAlong(holder,a+Vector3.up*.5f,b+Vector3.up*.5f,.6f,1.1f,"Bridge lip");
                }
                var drawn=kit.Built("Crevasse "+(which+1)+" cornice",shape,SnowFloor);holder.transform.SetParent(drawn.transform,true);
                foreach(var box in holder.GetComponentsInChildren<BoxCollider>())box.gameObject.isStatic=true;
            }
            /// <summary>The herd: rime-beasts on the herd's line, spaced along it and spread across it, each a kit beast on a kinematic body, the bob a child.</summary>
            static void Herd(ZoneKit kit)
            {
                var line=new List<Vector3>();
                for(int i=1;i<HerdLine.Length;i++){var a=HerdLine[i-1];var b=HerdLine[i];int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)));for(int k=0;k<steps;k++){var p=Vector2.Lerp(a,b,k/(float)steps);line.Add(kit.On(p.x,p.y));}}
                line.Add(kit.On(HerdLine[^1].x,HerdLine[^1].y));
                var herd=new GameObject("Rime-beast herd",typeof(RimeHerd)).GetComponent<RimeHerd>();herd.line=line.ToArray();
                var rng=new System.Random(77);float R()=>(float)rng.NextDouble();int count=22;float length=0;for(int i=1;i<line.Count;i++)length+=Vector3.Distance(line[i-1],line[i]);
                var beasts=new List<Transform>();var across=new List<float>();var along=new List<float>();
                for(int i=0;i<count;i++)
                {
                    var root=new GameObject("Rime-beast "+i);root.transform.SetParent(herd.transform,true);
                    var body=kit.Prop(ZoneKit.Kit("RimeBeast","DressBoulders"),Vector3.zero,1.55f+R()*.35f,0,false,false);body.transform.SetParent(root.transform,true);
                    var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,.8f,0);box.size=new Vector3(1.3f,1.5f,2.6f);var rb=root.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
                    float side=(R()-.5f)*3.6f,at=length*i/count+R()*2;beasts.Add(root.transform);across.Add(side);along.Add(at);
                    // Stood where it walks from, so a still (and the first frame) shows the herd on its line.
                    float walked=0;for(int k=1;k<line.Count;k++){float leg=Vector3.Distance(line[k-1],line[k]);if(walked+leg>=at){var p=Vector3.Lerp(line[k-1],line[k],(at-walked)/leg);var ahead=(line[k]-line[k-1]);ahead.y=0;ahead.Normalize();
                        root.transform.SetPositionAndRotation(p+new Vector3(ahead.z,0,-ahead.x)*side,Quaternion.LookRotation(ahead));break;}walked+=leg;}
                }
                herd.beasts=beasts.ToArray();herd.across=across.ToArray();herd.along=along.ToArray();
            }
            /// <summary>The whiteouts, over the field north of the ridge: the first after Idra's lesson, the second as the lead walks into the stalkers' field; vent pockets one and two are
            /// the shelters. The blown snow is a system the weather moves with the lead.</summary>
            static void Weather(ZoneKit kit,EncounterVolume stalkers)
            {
                var flake=kit.Glow("Snow",false,new Color(1.1f,1.12f,1.18f,1));
                var snow=kit.Drift("Blown snow",kit.On(0,110,2),new Vector3(30,6,24),flake,new Color(1,1,1,.7f),2400,.05f,.16f,4,3);
                var main=snow.main;main.startSpeed=new ParticleSystem.MinMaxCurve(4,9);var velocity=snow.velocityOverLifetime;velocity.enabled=true;velocity.x=new ParticleSystem.MinMaxCurve(-7,-4);velocity.y=new ParticleSystem.MinMaxCurve(-.6f,.2f);velocity.z=new ParticleSystem.MinMaxCurve(-1,1);
                var weather=new GameObject("Whiteouts",typeof(Whiteout)).GetComponent<Whiteout>();weather.startFlag="hoar.whiteoutMet";weather.liftedFlag="hoar.whiteoutLifted";
                weather.field=new Bounds(new Vector3(0,0,117),new Vector3(104,40,52));
                var front=stalkers.GetComponent<BoxCollider>();weather.fronts=new[]{new Bounds(front.transform.position,front.size)};
                weather.shelters=new[]{new Bounds(new Vector3(VentOne.x,0,VentOne.y),new Vector3(13,20,13)),new Bounds(new Vector3(VentTwo.x,0,VentTwo.y),new Vector3(12,20,12))};
                weather.snow=snow;
            }
            /// <summary>Idra's braced body walking ahead of the party, waiting at each of her talks (Guide), a step off the walked line. The body has no collider and no agent and is
            /// in no bake (a skinned body is drawn only): it never stops anyone, and it waits for a body in its own way rather than walk through one.</summary>
            static void Ahead(ZoneKit kit)
            {
                var prefab=ZoneKit.Prefab("Characters","IdraBraced")??ZoneKit.Prefab("Characters","IdraNatural");if(prefab==null)return;
                var holder=new GameObject("Idra, ahead");var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.transform.SetParent(holder.transform,false);body.name="Idra's body";
                foreach(var rig in body.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullCompletely;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
                Vector3[] P(params Vector2[] points)=>points.Select(p=>kit.On(p.x,p.y)).ToArray();
                var guide=holder.AddComponent<Guide>();guide.goneFlag="hoar.idraJoined";holder.transform.SetPositionAndRotation(kit.On(7,60.5f),Quaternion.Euler(0,200,0));
                guide.legs=new[]
                {
                    new Guide.Leg{flag="",path=P(new Vector2(7,60.5f))},
                    new Guide.Leg{flag="hoar.herdMet",path=P(new Vector2(6,70),new Vector2(5,82),new Vector2(5,92),new Vector2(9,102))},
                    // Into vent pocket one by its open east side and up under the windbreak, three metres off the pocket's middle: her spot was where a party sheltering from the east
                    // stops, and the lead stood inside her body for the whole whiteout (s35/097).
                    new Guide.Leg{flag="hoar.whiteoutMet",path=P(new Vector2(-12,103),new Vector2(-24,103),new Vector2(-28.6f,106.6f))},
                    new Guide.Leg{flag="hoar.whiteoutLifted",path=P(new Vector2(-12,112),new Vector2(6,128),new Vector2(6,136),new Vector2(8,146),new Vector2(17,158),new Vector2(18.5f,161))},
                    new Guide.Leg{flag="hoar.vessaHeard",path=P(new Vector2(24,170),new Vector2(24,178),new Vector2(31.5f,186.6f))},
                    new Guide.Leg{flag="hoar.ranksSeen",path=P(new Vector2(40,182),new Vector2(50,180),new Vector2(57,184))},
                    new Guide.Leg{flag="hoar.rig1",path=P(new Vector2(64,187),new Vector2(76,187),new Vector2(84,189.5f))},
                    new Guide.Leg{flag="hoar.offerHeard",path=P(new Vector2(92,192),new Vector2(93.4f,193.4f))},
                    new Guide.Leg{flag="clear.Dig_Rank",path=P(new Vector2(91,206),new Vector2(91,214),new Vector2(100,215.6f),new Vector2(110,216.2f))},
                    new Guide.Leg{flag="clear.Dig_Rank3|hoar.gapTaken",path=P(new Vector2(140,222),new Vector2(156,219),new Vector2(166,216),new Vector2(178,224))},
                };
                guide.faces=new[]{new Vector3(0,0,50),new Vector3(5,0,94),new Vector3(-10,0,100),new Vector3(20,0,150),new Vector3(24,0,175),new Vector3(40,0,184),new Vector3(70,0,187),
                    new Vector3(80,0,184),new Vector3(92,0,192),new Vector3(100,0,208),new Vector3(170,0,214)};
            }

            // ---------------------------------------------------------------- dressing (after the blockout)
            /// <summary>The walked lines, the Reach's and the dig's: the ground kit keeps them clear, the scatter keeps its tall things off them, Idra's steps are pressed along the first.</summary>
            internal static readonly Vector2[][] Ways=
            {
                new Vector2[]{new(0,2),new(0,12),new(1,30),new(2,46),new(2,56),new(4,66),new(4.5f,82),new(5,93),new(3,100),new(6,118),new(6,134),new(8,145),new(18,156),new(24,162),new(24,178)},
                new Vector2[]{new(-4,48),new(-30,53),new(-38,58)},new Vector2[]{new(3,100),new(-28,104)},new Vector2[]{new(6,118),new(33,118)},new Vector2[]{new(8,145),new(-8,151)},
                new Vector2[]{new(24,178),new(30,184),new(58,186),new(72,187),new(86,184),new(100,188),new(92,198),new(90,212),new(100,214),new(112,214),new(124,218),new(146,224),new(158,219),new(172,218),new(184,218),new(192,218),new(208,214)},
                new Vector2[]{new(104,182),new(130,186)},
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
            static readonly (Vector2 at,Vector2 size)[] Fights=
            {
                (new(0,32),new(36,20)),(new(-37,61),new(20,16)),(new(6,120),new(40,22)),(new(26,152),new(30,16)),(new(52,183.5f),new(16,11)),(new(40.2f,194.5f),new(14,7)),
                (new(104,181),new(20,22)),(new(90,207.5f),new(12,13)),(new(124,184),new(20,18)),(new(124,219),new(22,26)),(new(146,221),new(18,22)),
            };
            static bool InFight(Vector2 p,float inset=0){foreach(var (at,size) in Fights)if(Mathf.Abs(p.x-at.x)<size.x*.5f-inset&&Mathf.Abs(p.y-at.y)<size.y*.5f-inset)return true;return InArena(p,-1);}
            /// <summary>A bed where blocks have come out of the floor (D378): a grid of cells 1.2 by 0.9 m sawn round, some lifted, about a point and turned by a yaw. Off every walked
            /// line and every fight; a duckboard and spoil beside each (Dress).</summary>
            sealed class Bed
            {
                const float W=1.2f,D=.9f;readonly int cols,rows,seed;readonly float yaw;internal readonly Vector2 at;
                internal Bed(float x,float z,int cols,int rows,float yaw,int seed){at=new Vector2(x,z);this.cols=cols;this.rows=rows;this.yaw=yaw;this.seed=seed;}
                Vector2 Right=>new(Mathf.Cos(yaw*Mathf.Deg2Rad),-Mathf.Sin(yaw*Mathf.Deg2Rad));Vector2 Fore=>new(Mathf.Sin(yaw*Mathf.Deg2Rad),Mathf.Cos(yaw*Mathf.Deg2Rad));
                Vector2 Corner=>at-Right*cols*W*.5f-Fore*rows*D*.5f;
                bool Out(int c,int r)=>new System.Random(seed*97+c*13+r*7).NextDouble()<.5;
                internal bool Lifted(Vector2 p)
                {
                    var d=p-Corner;float u=Vector2.Dot(d,Right)/W,v=Vector2.Dot(d,Fore)/D;if(u<0||v<0||u>=cols||v>=rows)return false;
                    float fu=u-Mathf.Floor(u),fv=v-Mathf.Floor(v);return Out((int)u,(int)v)&&fu>.06f&&fu<.94f&&fv>.08f&&fv<.92f;
                }
                internal IEnumerable<(Vector2 a,Vector2 b)> Kerfs()
                {
                    for(int c=0;c<=cols;c++)yield return (Corner+Right*c*W,Corner+Right*c*W+Fore*rows*D);
                    for(int r=0;r<=rows;r++)yield return (Corner+Fore*r*D,Corner+Fore*r*D+Right*cols*W);
                }
                /// <summary>A point beside the bed, <paramref name="out"/> metres past its east end (negative: its west).</summary>
                internal Vector2 Side(float @out)=>at+Right*(Mathf.Sign(@out)*cols*W*.5f+@out);
            }
            static readonly Bed[] Beds={new(18.8f,183.9f,3,2,0,1),new(85.6f,175.6f,4,3,8,2),new(90f,218.4f,3,1,0,3),new(182f,206.4f,3,2,-6,4)};
            /// <summary>A serac: a leaning column of glacier ice, five to seven faces, cut off ragged at the top under a cap of snow.</summary>
            static void Serac(ZoneKit.Shape shape,Vector3 foot,float tall,float girth,int seed)
            {
                var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();
                int sides=5+rng.Next(3);var lean=new Vector3(R()-.5f,0,R()-.5f)*.22f*tall;var bottom=new Vector3[sides];var top=new Vector3[sides];float turn=R()*6.28f;
                for(int k=0;k<sides;k++)
                {
                    float a=turn+k*6.28f/sides+(R()-.5f)*.4f,r=girth*(.75f+.5f*R()),h=tall*(.72f+.28f*R());
                    bottom[k]=foot+new Vector3(Mathf.Cos(a)*r,-.6f,Mathf.Sin(a)*r);top[k]=foot+lean*(h/tall)+new Vector3(Mathf.Cos(a)*r*.5f,h,Mathf.Sin(a)*r*.5f);
                }
                var peak=foot+lean+Vector3.up*tall*(1+.08f*R());
                for(int k=0;k<sides;k++)
                {
                    int n=(k+1)%sides;var outward=(bottom[k]+bottom[n])*.5f-foot;outward.y=0;
                    shape.Slot(0).Quad(bottom[k],bottom[n],top[n],top[k],outward,Color.Lerp(new Color(.74f,.84f,.96f),Color.white,(k%3)*.3f),.5f);
                    shape.Slot(1).Triangle(top[k],top[n],peak,Vector3.up,SnowWhite,.5f);
                }
            }
            /// <summary>Where the host stands under the snow: a block of its ranks, the spears up through the crust in rows (head high and over, some leaning as the ice has pushed
            /// them, one in five snapped short), a standard now and then a stride over a head, shields and helms at the surface. Thin, drawn only; snow banked against each.</summary>
            static void Formation(ZoneKit.Shape bronze,ZoneKit.Shape cloth,ZoneKit.Shape banks,Vector2 centre,float yaw,int rows,int files,int seed,ZoneKit kit)
            {
                var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();var turn=Quaternion.Euler(0,yaw,0);
                for(int r=0;r<rows;r++)for(int f=0;f<files;f++)
                {
                    var off=turn*new Vector3((f-(files-1)*.5f)*1.5f,0,(r-(rows-1)*.5f)*1.7f);var p=centre+new Vector2(off.x,off.z)+new Vector2(R()-.5f,R()-.5f)*.4f;
                    bool hide=false;foreach(var h in FieldHides.Concat(WhiteHides))hide|=Vector2.Distance(p,h)<3.6f;
                    if(R()<.12f||NearWay(p,1.4f)||hide||!Walkable(p)||Crevasse(p)<2)continue;
                    var foot=kit.On(p.x,p.y);float roll=R();
                    if(roll<.08f&&!NearWay(p,2.6f))
                    {
                        // A standard: a bronze pole a stride over a head, leaning a little, its crossbar, what is left of the cloth (faded ochre, a metre across at most), the host's
                        // ring at the top.
                        var lean=Quaternion.Euler((R()-.5f)*14,0,(R()-.5f)*14);var up=lean*Vector3.up;float tall=3.2f+R()*1.1f;var bar=foot+up*(tall-.8f);
                        bronze.Slot(0).Tube(new[]{foot+Vector3.down*.3f,foot+up*tall},new[]{.065f,.05f},6,BronzeDark,BronzePale,1,.25f).Beam(bar-turn*Vector3.right*.55f,bar+turn*Vector3.right*.55f,.06f,.06f,BronzePale);
                        var wheel=foot+up*(tall+.12f);bronze.Slot(0).Disc(wheel,turn*Vector3.back,.28f,14,BronzePale,.21f,BronzeDark).Disc(wheel,turn*Vector3.forward,.28f,14,BronzePale,.21f,BronzeDark);
                        for(int k=0;k<4;k++){var spoke=turn*(Quaternion.Euler(0,0,k*45)*Vector3.up)*.22f;bronze.Slot(0).Beam(wheel-spoke,wheel+spoke,.03f,.03f,BronzePale);}
                        bronze.Slot(0).Lump(wheel,new Vector3(.06f,.06f,.06f),seed+f,.05f,BronzePale,BronzeDark,8,4);
                        for(int k=0;k<3;k++){float x=-.48f+k*.33f,low=1.1f+R()*.5f;var a=bar+turn*new Vector3(x,0,.02f);var b=bar+turn*new Vector3(x+.3f,0,.02f);
                            cloth.Slot(0).Quad(a,b,b+Vector3.down*(low+R()*.2f),a+Vector3.down*low,turn*Vector3.back,Color.Lerp(Ochre,new Color(.7f,.6f,.5f),R()*.5f),.6f);}
                        banks.Slot(0).Lump(foot+new Vector3(-.5f,.02f,0),new Vector3(.9f,.22f,.5f),seed+r*31+f,.3f,SnowWhite,SnowShade,10,4);
                        continue;
                    }
                    if(roll<.22f)
                    {
                        // A shield at the surface: round bronze, tipped up out of the crust, its boss and rim; snow banked on its windward face.
                        var tip=Quaternion.Euler(25+R()*40,R()*360,0);var face=tip*Vector3.up;float radius=.4f+R()*.08f;var at=foot+Vector3.up*(radius*.35f);
                        bronze.Slot(0).Disc(at,face,radius,16,BronzePale,0,BronzeDark).Disc(at-face*.03f,-face,radius,16,BronzeDark,0,BronzeDark);
                        bronze.Slot(0).Disc(at+face*.01f,face,radius,16,BronzeDark,radius*.86f,BronzeDark).Lump(at+face*.06f,new Vector3(.09f,.09f,.09f),seed+f,.05f,BronzePale,BronzeDark,8,4,.25f,Quaternion.LookRotation(face));
                        banks.Slot(0).Lump(foot+new Vector3(.35f,.02f,0),new Vector3(.55f,.2f,.45f),seed+r*13+f,.3f,SnowWhite,SnowShade,10,4);
                        continue;
                    }
                    if(roll<.3f){bronze.Slot(0).Dome(foot+Vector3.down*.12f,.26f,.32f,4,10,BronzePale,BronzeDark).Beam(foot+new Vector3(0,.16f,-.24f),foot+new Vector3(0,.2f,.24f),.05f,.08f,Ochre);continue;}
                    // A spear: a shaft head high and over, at a soldier's lean or pushed further by the ice, its leaf-shaped head and collar; one in five snapped off short.
                    bool pushed=R()<.3f;float showing=R()<.2f?.4f+R()*.4f:1.5f+R()*1.1f;
                    var tilt=pushed?Quaternion.Euler((R()<.5f?-1:1)*(15+R()*13),0,(R()-.5f)*16):Quaternion.Euler((R()-.5f)*14,0,(R()-.5f)*14);var up2=tilt*Vector3.up;
                    bronze.Slot(0).Tube(new[]{foot+Vector3.down*.3f,foot+up2*showing},new[]{.085f,.065f},6,BronzeDark,BronzePale,1,.25f);
                    if(showing>.9f)
                    {
                        var head=foot+up2*showing;var across=turn*Vector3.right*.17f;
                        bronze.Slot(0).Tube(new[]{head-up2*.08f,head+up2*.06f},new[]{.1f,.1f},6,BronzeDark,BronzeDark);
                        bronze.Slot(0).Triangle(head-across,head+across,head+up2*.85f,turn*Vector3.back,BronzePale,.4f).Triangle(head+across,head-across,head+up2*.85f,turn*Vector3.forward,BronzePale,.4f);
                    }
                    banks.Slot(0).Lump(foot+new Vector3(-.2f,.02f,0),new Vector3(.4f,.16f,.28f),seed+f*7+r,.3f,SnowWhite,SnowShade,8,4);
                }
            }
            /// <summary>Snow the wind has piled against something standing: a bank on its east face, where the wind comes from, and a tail drawn out west behind it, low enough to
            /// walk through (a hand to a shin); kept off the walked lines.</summary>
            static void DriftAgainst(ZoneKit kit,ZoneKit.Shape banks,Vector2 at,float girth,int seed)
            {
                var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();
                var bank=at+new Vector2(girth*.8f,0);if(!NearWay(bank,1.4f)&&Walkable(bank))banks.Slot(0).Lump(kit.On(bank.x,bank.y,-.04f),new Vector3(girth*.55f,.32f,girth*.9f),seed,.25f,SnowWhite,SnowShade,10,5);
                for(int k=1;k<=2;k++)
                {
                    var tail=at-new Vector2(girth*(.9f+k*1.1f),(R()-.5f)*girth*.4f);if(NearWay(tail,1.4f)||!Walkable(tail))break;
                    banks.Slot(0).Lump(kit.On(tail.x,tail.y,-.05f),new Vector3(girth*(1.3f-k*.25f),.3f-k*.06f,girth*(.75f-k*.12f)),seed+k,.25f,SnowWhite,SnowShade,10,5,.25f,Quaternion.Euler(0,(R()-.5f)*20,0));
                }
            }
            /// <summary>Old ice breaking through the Reach's snow: a ridge of blue slabs pushed up and tipped, knee to chest high, along a line; solid along it.</summary>
            static void IceRidge(ZoneKit kit,ZoneKit.Shape ice,GameObject solids,Vector2 from,Vector2 to,int seed)
            {
                var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();float length=Vector2.Distance(from,to);var along=(to-from)/length;float yaw=Mathf.Atan2(along.x,along.y)*Mathf.Rad2Deg;
                for(float s=.4f;s<length;s+=.8f+R()*.5f)
                {
                    var p=from+along*s+new Vector2(-along.y,along.x)*(R()-.5f)*.6f;float tall=.5f+R()*.7f;
                    ice.Slot(0).Box(kit.On(p.x,p.y,tall*.35f),new Vector3(1+R()*.6f,tall,.35f+R()*.25f),Quaternion.Euler((R()-.5f)*40,yaw+(R()-.5f)*30,(R()-.5f)*24),new Color(.72f,.84f,.98f),Color.white);
                }
                ZoneKit.BlockAlong(solids,kit.On(from.x,from.y,.6f),kit.On(to.x,to.y,.6f),1.1f,1.2f,"Old ice");
            }
            /// <summary>Icicles along an edge between two points, hanging down from it.</summary>
            static void Icicles(ZoneKit.Shape shape,Vector3 from,Vector3 to,int seed,float longest=.9f)
            {
                var rng=new System.Random(seed);float length=Vector3.Distance(from,to);
                for(float t=0;t<length;t+=.22f+(float)rng.NextDouble()*.3f){var at=Vector3.Lerp(from,to,t/length);float drop=.15f+(float)rng.NextDouble()*longest;shape.Slot(0).Post(at+Vector3.down*drop,drop,.006f,.06f,5,new Color(.8f,.92f,1.05f),Color.white);}
            }
            /// <summary>The dig's walls as an excavation leaves them (D378; they were the heightfield's smooth ribbons): the faces the blades cut, flat, each at its own lean and a
            /// hand or two proud of the glacier, a fracture down some; and what the glacier took in long before the Muster came: moraine boulders, the host's spears and shields,
            /// a sledge and its timbers lost down a crevasse. Only on walls the camera looks at across a floor; never over a floor, a frozen rank, a plug, a rig or a coupling, and
            /// nothing tall enough to stand across a floor behind it. Drawn only (the glacier is the stop), one small mesh to each six metres so no mesh's bounds span a floor.</summary>
            static void WallFaces(ZoneKit kit)
            {
                var keep=new List<Bounds>();var held=new HashSet<Material>{IceBlock,AmberBlock,Refrozen,InIce};if(guardLooks!=null)held.UnionWith(guardLooks.Where(m=>m!=null));
                foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    var b=r.bounds;if(b.size.x>24||b.size.z>24||!InDig(new Vector2(b.center.x,b.center.z)))continue;
                    if(r.sharedMaterials.Any(m=>m!=null&&held.Contains(m))||r.GetComponentInParent<IceThaw>()!=null||r.GetComponentInParent<Membrane>()!=null){b.Expand(new Vector3(2.4f,0,2.4f));keep.Add(b);}
                }
                bool Kept(Vector2 p,float half)=>KeptTop(p,half)>float.NegativeInfinity;
                float KeptTop(Vector2 p,float half){float top=float.NegativeInfinity;foreach(var b in keep)if(p.x>b.min.x-half&&p.x<b.max.x+half&&p.y>b.min.z-half&&p.y<b.max.z+half)top=Mathf.Max(top,b.max.y);return top;}
                bool HidesBeyond(Vector2 p,float tall){float reach=ZoneKit.Shadowed(tall)+1.5f;for(float d=.6f;d<=reach;d+=.5f)if(Walkable(p+Away*d))return true;return false;}
                bool NearCoupling(Vector2 p){if(Vector2.Distance(p,C)>ShelfRadius+5)return false;float b=BearingOf(p);foreach(float c in CouplingBearings)if(Mathf.Abs(Mathf.DeltaAngle(b,c))<18)return true;return false;}
                float Clear(Vector2 p)=>FloorAt(p,out _);
                var rng=new System.Random(5113);float R()=>(float)rng.NextDouble();
                var stretches=new Dictionary<(int x,int z),ZoneKit.Shape>();var placed=new List<Vector2>();int faces=0,things=0;
                ZoneKit.Shape At(Vector2 p){var key=(Mathf.FloorToInt(p.x/6),Mathf.FloorToInt(p.y/6));if(!stretches.TryGetValue(key,out var s))stretches[key]=s=new ZoneKit.Shape().Slot(4);return s;}
                Color fracture=new(.22f,.32f,.5f),side=new(.5f,.62f,.82f);var low=new List<Vector2>();int set=0;
                for(float x=12;x<=232;x+=.5f)for(float z=168;z<=246;z+=.5f)
                {
                    var p=new Vector2(x,z);if(!InDig(p))continue;float clear=FloorAt(p,out float level);if(clear<.25f||clear>.55f)continue;
                    var n=new Vector2(Clear(p+new Vector2(.25f,0))-Clear(p-new Vector2(.25f,0)),Clear(p+new Vector2(0,.25f))-Clear(p-new Vector2(0,.25f)));if(n.sqrMagnitude<1e-4f)continue;n.Normalize();
                    float facing=Vector2.Dot(n,Away);
                    float wall=Height(p+n*1.6f)-level;if(wall<2.8f)continue;
                    if(facing<.3f)
                    {
                        // A wall beside a floor, not behind it: anything tall on it stands across the floor from the camera. Only what lies low in its foot, under a chest:
                        // a boulder, a sledge's runner, the host's spears, frozen in where the blades cut down to them.
                        if(facing<-.6f||NearCoupling(p))continue;bool close=false;foreach(var q in low)if((q-p).sqrMagnitude<3.4f*3.4f){close=true;break;}
                        if(close||Kept(p,.8f)||R()>.55f)continue;low.Add(p);var s=At(p);var o3=new Vector3(n.x,0,n.y);var a3=Vector3.Cross(Vector3.up,o3).normalized;
                        var at=new Vector3(p.x,level+.25f+R()*.35f,p.y)+o3*.15f;set++;
                        if(R()<.55f)for(int k=0;k<2+rng.Next(2);k++)s.Slot(1).Lump(at+a3*(R()-.5f)*1.4f+o3*R()*.3f,new Vector3(.3f+R()*.25f,.26f+R()*.2f,.3f+R()*.25f),1300+set*5+k,.35f,RockPale,RockDark,10,5);
                        else if(R()<.5f){var run=Quaternion.AngleAxis(8+R()*14,o3)*a3*1.3f;s.Slot(4).Beam(at-run,at+run,.1f,.12f,IronDark,IronPale);
                            for(int k=-1;k<=1;k++){var c=at+run*k*.55f+o3*.2f+Vector3.up*.06f;s.Slot(3).Beam(c-o3*.5f,c+o3*.5f,.34f,.05f,WoodDark,WoodPale);}}
                        else for(int k=0;k<3;k++){var c=at+a3*(k-1)*.4f;var tilt=Quaternion.AngleAxis(-15-R()*20,a3)*(-o3);s.Slot(2).Tube(new[]{c+o3*1.1f,c+tilt*.5f},new[]{.06f,.05f},6,BronzeDark,BronzePale,1,.25f);}
                        continue;
                    }
                    bool near=false;foreach(var q in placed)if((q-p).sqrMagnitude<2.6f*2.6f){near=true;break;}if(near)continue;
                    float wide=1.5f+R()*1.8f,start=level-.35f,above=KeptTop(p,wide*.5f);
                    // Over a frozen rank, a plug or a rig the face starts above it: the host stands at the wall's foot, the blades cut the glacier over its heads.
                    if(above>float.NegativeInfinity){start=above+.25f;if(level+wall-start<2.4f)continue;}
                    float tall=Mathf.Min((level+wall-start)*(.55f+.4f*R()),6.2f);
                    if(HidesBeyond(p,start+tall-level)||NearCoupling(p))continue;
                    placed.Add(p);var shape=At(p);faces++;
                    // A cut face: a slab of the glacier's own ice, fresh and pale where it was cut, its front nearly upright (the wall behind leans back further), its top broken off
                    // ragged, its sides in the wall's own blue.
                    var into=Quaternion.Euler(0,(R()-.5f)*18,0)*new Vector3(n.x,0,n.y);var along=Vector3.Cross(Vector3.up,into).normalized;
                    float lean=.02f+R()*.08f,depth=.7f+R()*.5f;var foot=new Vector3(p.x,start,p.y)+into*(start>level?.25f:0);
                    Vector3 Up(float h)=>Vector3.up*h+into*lean*h;
                    var bl=foot-along*wide*.5f;var br=foot+along*wide*.5f;
                    var tl=foot-along*wide*(.3f+.16f*R())+Up(tall*(.7f+.22f*R()));var tr=foot+along*wide*(.3f+.16f*R())+Up(tall*(.7f+.22f*R()));var peak=foot+along*wide*(R()-.5f)*.5f+Up(tall);
                    var lit=Color.Lerp(new Color(.84f,.92f,1),Color.white,R()*.6f);var middle=foot+Up(tall*.5f);
                    shape.Slot(0).Quad(bl,br,tr,tl,-into,lit,.3f).Triangle(tl,tr,peak,-into,lit,.3f);
                    var rim=new[]{br,tr,peak,tl,bl};
                    for(int k=0;k<rim.Length-1;k++){var a=rim[k];var b=rim[k+1];var outward=Vector3.ProjectOnPlane((a+b)*.5f-middle,into);
                        shape.Slot(0).Quad(a,b,b+into*depth,a+into*depth,outward,outward.y>.3f?Color.Lerp(lit,Color.white,.4f):side,.3f);}
                    // A fracture down about half of them: a dark line from the broken top toward the foot, wandering.
                    if(R()<.5f)
                    {
                        var top=Vector3.Lerp(tl,tr,.2f+.6f*R());var end=Vector3.Lerp(bl,br,.2f+.6f*R())+Vector3.up*.3f;var at=top;var w=along*(.03f+.03f*R());
                        for(int k=1;k<=4;k++){var next=Vector3.Lerp(top,end,k/4f)+(k<4?along*(R()-.5f)*.4f:Vector3.zero);var lift=-into*.025f;
                            shape.Slot(0).Quad(at-w+lift,at+w+lift,next+w+lift,next-w+lift,-into,fracture,.3f);at=next;}
                    }
                    // Beside one face in three, something the glacier took in, set into the wall beside it.
                    if(R()>.36f)continue;
                    var q2=p+new Vector2(along.x,along.z)*(wide*.5f+.5f+R()*.6f)*(R()<.5f?-1:1);float qc=Clear(q2);if(qc<.2f||qc>1.1f||Kept(q2,.8f))continue;
                    var n3=new Vector3(n.x,0,n.y);var spot=new Vector3(q2.x,level+.7f+R()*Mathf.Min(wall*.55f,3.2f),q2.y)+n3*(.6f-qc)*.6f;float roll=R();things++;
                    if(roll<.5f)
                        for(int k=0;k<1+rng.Next(3);k++)shape.Slot(1).Lump(spot+n3*.25f+along*(R()-.5f)*1.2f+Vector3.up*(R()-.5f)*.8f,Vector3.one*(.35f+R()*.45f),900+faces*5+k,.35f,RockPale,RockDark,10,5);
                    else if(roll<.72f)
                        for(int k=0;k<2+rng.Next(2);k++){var o=spot+along*(k-.5f)*.45f;var tilt=Quaternion.AngleAxis(-25-R()*25,along)*(-n3);
                            shape.Slot(2).Tube(new[]{o+n3*1.2f,o+tilt*.7f},new[]{.06f,.05f},6,BronzeDark,BronzePale,1,.25f);}
                    else if(roll<.84f)
                    {
                        var face=Quaternion.AngleAxis((R()-.5f)*50,Vector3.up)*Quaternion.AngleAxis(-20-R()*30,along)*(-n3);
                        shape.Slot(2).Disc(spot+n3*.18f,face,.44f,16,BronzePale,0,BronzeDark).Disc(spot+n3*.18f-face*.03f,-face,.44f,16,BronzeDark,0,BronzeDark);
                    }
                    else
                    {
                        // A sledge lost down a crevasse: a runner and three of its boards, tipped, half in the ice.
                        var tip=Quaternion.AngleAxis(15+R()*20,n3);var run=tip*along*1.6f;
                        shape.Slot(4).Beam(spot+n3*.2f-run,spot+n3*.2f+run,.1f,.12f,IronDark,IronPale);
                        for(int k=-1;k<=1;k++){var c=spot+n3*.45f+run*k*.5f+Vector3.up*.1f;shape.Slot(3).Beam(c-n3*.55f,c+n3*.55f,.36f,.05f,WoodDark,WoodPale);}
                    }
                }
                foreach(var ((sx,sz),shape) in stretches)kit.Built($"Dig wall faces {sx}-{sz}",shape,IceWall,Rock,Bronze,Timber,Iron);
                Debug.Log($"FROZEN_HOST_WALL_FACES faces={faces} things={things} low={low.Count} meshes={stretches.Count}");
                Debug.Log("FROZEN_HOST_WALL_AT "+string.Join(";",placed.Concat(low).Select(p=>p.x.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+","+p.y.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture))));
            }
            static void Dress(ZoneKit kit,Material steam)
            {
                var rng=new System.Random(61);float R()=>(float)rng.NextDouble();
                // ---- the host under the Reach: its ranks' spears and standards in blocks, the way it was marching (north).
                var bronze=new ZoneKit.Shape();var cloth=new ZoneKit.Shape();var banks=new ZoneKit.Shape();int block=0;
                foreach(var (x,z,yaw,rows,files) in new[]{(-14f,8f,4f,4,9),(22f,10f,-6f,4,8),(-30f,24f,2f,5,8),(30f,30f,8f,5,7),(-22f,42f,-4f,3,10),(24f,44f,0f,3,9),
                    (-8f,27f,0f,4,7),(9f,34f,6f,4,6),(-3f,40.5f,-4f,2,8),(14f,20f,2f,3,6),(-12f,96f,4f,3,7),(16f,108f,-6f,3,7),
                    (28f,58f,10f,4,8),(-14f,62f,-8f,3,6),(30f,78f,4f,3,6),(-30f,82f,6f,3,6),(-14f,118f,-2f,4,8),(26f,130f,6f,4,8),(-28f,130f,0f,4,7),(-30f,148f,-10f,3,8)})
                    Formation(bronze,cloth,banks,new Vector2(x,z),yaw,rows,files,900+block++,kit);
                kit.Built("The host under the snow",bronze,Bronze,SnowFloor);kit.Built("Standards' cloth",cloth,Cloth);
                // ---- seracs: along the Reach's sides, on the ridge with the gap in it, by the crevasses' ends: wherever nothing walked lies in their shadow. Snow drifted against each.
                var seracs=new ZoneKit.Shape();int count=0;
                for(int k=0;k<420;k++)
                {
                    var p=new Vector2(-56+R()*112,-12+R()*170);float tall=3.5f+R()*5,girth=1+R()*1.4f;
                    float clear=FloorAt(p,out _);bool ridge=p.y>74&&p.y<90&&Mathf.Abs(p.x)>11;
                    if(!(ridge||clear>2.5f)||clear>18||NearWay(p,6)||ShadowsAFloor(p,tall))continue;
                    Serac(seracs,new Vector3(p.x,Height(p),p.y),tall,girth,1100+k);count++;
                    if(clear<4)DriftAgainst(kit,banks,p,girth*1.2f,2100+k);
                }
                kit.Built("Seracs",seracs,IceWall,SnowFloor);Debug.Log("FROZEN_HOST_SERACS "+count);
                // ---- the Reach broken at a larger scale than its snow: dark rock and old blue ice coming up through it, never over a stride high (a body behind one is still seen),
                // kept off the walked lines, the herd's line, the fights and the pockets; snow capped and drifted against.
                var rockSolids=new GameObject("Reach rock");var oldIce=new ZoneKit.Shape();var rocks=new System.Random(77);float Q()=>(float)rocks.NextDouble();
                bool Open(Vector2 p,float margin)=>Walkable(p)&&!NearWay(p,margin)&&ZoneKit.Segment(p,Arrival,GateWay)>margin+2&&ZoneKit.FromPath(p,HerdLine)>margin+2&&!InFight(p,-margin)&&Crevasse(p)>margin
                    &&Vents.All(v=>Vector2.Distance(p,v.at)>v.radius+margin)&&Vector2.Distance(p,Sledge)>margin+3&&Vector2.Distance(p,Bench)>margin+2&&Vector2.Distance(p,Cradle)>margin+2;
                string[] rockKeys={"RidgeRock1","RidgeRock2","RidgeRock3","DressStrataOutcrop"};int outcrops=0;
                foreach(var (x,z,tall) in new[]{(-34f,16f,1.5f),(33f,22f,1.4f),(-42f,94f,1.6f),(40f,70f,1.5f),(-22f,142f,1.3f),(-45f,118f,1.6f),(45f,138f,1.6f),(38f,48f,1.4f),
                    (-40f,40f,1.6f),(-18f,74f,1.2f),(24f,92f,1.3f),(-44f,156f,1.5f),(14f,-4f,1.2f),(-26f,-2f,1.4f),(42f,98f,1.6f),(-36f,140f,1.5f)})
                {
                    var p=new Vector2(x,z);if(!Open(p,3))continue;
                    var rock=kit.Prop(ZoneKit.Kit(rockKeys[outcrops%rockKeys.Length],"DressBoulders"),new Vector3(x,-.25f,z),tall,Q()*360);rock.name="Reach outcrop";rock.AddComponent<PropTint>().tint=new Color(.46f,.47f,.54f);
                    var top=ZoneKit.Drawn(rock);banks.Slot(0).Lump(new Vector3(top.center.x,top.max.y-.12f,top.center.z),new Vector3(top.extents.x*.7f,.18f,top.extents.z*.7f),3000+outcrops,.3f,SnowWhite,SnowShade,10,4);
                    DriftAgainst(kit,banks,p,Mathf.Max(top.extents.x,top.extents.z),3100+outcrops);outcrops++;
                }
                int ridges=0;
                foreach(var (x0,z0,x1,z1) in new[]{(-28f,28f,-21f,35f),(18f,56f,25f,60f),(-38f,120f,-31f,127f),(35f,84f,42f,80f),(-24f,159f,-17f,163f),(-46f,70f,-41f,77f),(40f,152f,46f,146f)})
                {
                    var a=new Vector2(x0,z0);var b=new Vector2(x1,z1);if(!Open(a,2.5f)||!Open(b,2.5f)||!Open((a+b)*.5f,2.5f))continue;
                    IceRidge(kit,oldIce,rockSolids,a,b,3300+ridges);DriftAgainst(kit,banks,(a+b)*.5f,1.4f,3400+ridges);ridges++;
                }
                var ridged=kit.Built("Old ice through the snow",oldIce,IceWall);rockSolids.transform.SetParent(ridged.transform,true);foreach(var box in rockSolids.GetComponentsInChildren<BoxCollider>())box.gameObject.isStatic=true;
                Debug.Log($"FROZEN_HOST_BREAKS outcrops={outcrops} ridges={ridges}");
                // ---- the Muster's sledge in the west pocket, through the crust and tipped, its cargo spilled in the snow.
                var sledge=new ZoneKit.Shape();var foot=kit.On(Sledge.x-1.4f,Sledge.y+2.2f);var tip=Quaternion.Euler(-8,30,6);
                foreach(int side in new[]{-1,1})sledge.Slot(0).Beam(foot+tip*new Vector3(side*.8f,-.1f,-2.4f),foot+tip*new Vector3(side*.8f,.35f,2.6f),.12f,.14f,IronDark,IronPale);
                sledge.Slot(1).Deck(foot+tip*new Vector3(0,.3f,-2.2f),foot+tip*new Vector3(0,.3f,2.2f),1.7f,.08f,WoodDark,rng);
                for(int k=0;k<3;k++)sledge.Slot(0).Box(foot+tip*new Vector3(-.3f+k*.3f,.75f,-1.2f+k*1.1f),new Vector3(.9f,.8f,.9f),tip*Quaternion.Euler(0,R()*20,0),IronDark,Hazard);
                sledge.Slot(2).Quad(foot+tip*new Vector3(-.9f,1.3f,-1.8f),foot+tip*new Vector3(.9f,1.3f,-1.8f),foot+tip*new Vector3(.9f,.4f,1.4f),foot+tip*new Vector3(-.9f,.4f,1.4f),tip*Vector3.up,new Color(.62f,.66f,.6f),.6f);
                kit.Built("Muster sledge",sledge,Iron,Timber,Tarp);
                // ---- the camp: the thaw boiler with its tank and chimney, the loudhailer mast at the rim, a tent, sledges of cut ice, crates, the cable down into the dig.
                var camp=new ZoneKit.Shape();
                kit.Prop(ZoneKit.Kit("ThawRig","HushwellVent"),new Vector3(Boiler.x,0,Boiler.y),4.6f,200).name="Thaw boiler";
                var tank=kit.On(Boiler.x-4.5f,Boiler.y+1.2f);camp.Slot(0).Post(tank,2.8f,1.1f,1.1f,12,IronDark,IronPale).Post(tank+Vector3.up*2.8f,.3f,1.1f,.7f,12,IronPale,IronPale);
                camp.Slot(0).Tube(new[]{tank+new Vector3(.8f,2,0),tank+new Vector3(2.6f,2.4f,0),kit.On(Boiler.x-.6f,Boiler.y,2.4f)},new[]{.16f,.16f,.16f},8,IronDark,IronPale);
                var mast=kit.On(Mast.x,Mast.y);camp.Slot(0).Post(mast+Vector3.down*.3f,7.4f,.14f,.09f,8,IronDark,IronPale);
                foreach(float turn in new[]{-35f,10f,55f}){var d=Quaternion.Euler(0,turn,0)*Vector3.forward;camp.Slot(0).Tube(new[]{mast+Vector3.up*6.6f,mast+Vector3.up*6.7f+d*.5f,mast+Vector3.up*6.8f+d*.9f},new[]{.06f,.22f,.4f},10,IronDark,Hazard,1,.3f,false);}
                foreach(int side in new[]{-1,1})camp.Slot(1).Rope(mast+Vector3.up*6.2f,kit.On(Mast.x+side*3.2f,Mast.y-2.4f),.1f,.02f,new Color(.3f,.28f,.26f));
                var tent=kit.On(44,143);var ridge2=Vector3.up*2.1f;
                camp.Slot(2).Quad(tent+new Vector3(-1.8f,0,-2.2f),tent+new Vector3(-1.8f,0,2.2f),tent+ridge2+Vector3.forward*2.2f,tent+ridge2+Vector3.back*2.2f,Vector3.left,new Color(.62f,.66f,.6f),.6f);
                camp.Slot(2).Quad(tent+ridge2+Vector3.back*2.2f,tent+ridge2+Vector3.forward*2.2f,tent+new Vector3(1.8f,0,2.2f),tent+new Vector3(1.8f,0,-2.2f),Vector3.right,new Color(.58f,.62f,.56f),.6f);
                foreach(var (x,z,yaw) in new[]{(15.1f,148.6f,42f),(46f,158f,-70f),(-2f,158f,10f)})
                {
                    var f=kit.On(x,z);var t=Quaternion.Euler(0,yaw,0);
                    foreach(int side in new[]{-1,1})camp.Slot(0).Beam(f+t*new Vector3(side*.8f,.1f,-2.2f),f+t*new Vector3(side*.8f,.25f,2.4f),.12f,.12f,IronDark,IronPale);
                    camp.Slot(3).Deck(f+t*new Vector3(0,.28f,-2f),f+t*new Vector3(0,.28f,2f),1.7f,.08f,WoodDark,rng);
                    for(int k=0;k<4;k++)camp.Slot(4).Box(f+t*new Vector3((k%2-.5f)*.8f,.72f+(k/2)*.86f,-.6f+(k%3)*.5f),new Vector3(.76f,.8f,.9f),t,new Color(.8f,.9f,1),new Color(.95f,.98f,1));
                }
var campBody=kit.Built("Muster camp",camp,Iron,Timber,Tarp,Timber,IceWall);
                // The sledges of cut ice are solid (the camp's fight takes in the first, which lies along the walked line, clear of it): nobody walks through a load of blocks.
                foreach(var (x,z,yaw) in new[]{(15.1f,148.6f,42f),(46f,158f,-70f),(-2f,158f,10f)})ZoneKit.Block(campBody,kit.On(x,z,.85f),new Vector3(1.8f,1.7f,4.7f),yaw,"Ice sledge");
                foreach(var (x,z,yaw) in new[]{(40f,146f,10f),(41.5f,147.4f,30f),(-4f,147f,-20f),(16f,160.5f,5f)})kit.Prop(ZoneKit.Kit("MusterCrate","DressCargoPallet"),new Vector3(x,0,z),.9f,yaw);
                // Snow drifted against the camp and the west pocket's sledge, as against the seracs.
                int drift=0;foreach(var (x,z,girth) in new[]{(44f,143f,2f),(15.1f,148.6f,1.6f),(46f,158f,1.6f),(-2f,158f,1.6f),(40.6f,146.6f,1f),(-4f,147f,.8f),(16f,160.5f,.8f),(Boiler.x,Boiler.y,2f),(Sledge.x-1.4f,Sledge.y+2.2f,1.6f)})
                    DriftAgainst(kit,banks,new Vector2(x,z),girth,3600+drift++);
                kit.Built("Wind drifts",banks,SnowFloor);
                // The Muster's cable from the boiler down the mouth, then round the upper gallery's walls to its lamps and rig one: it lies where it was dragged, in easy bends.
                var cable=new ZoneKit.Shape();var dark=new Color(.24f,.22f,.22f);var worn=new Color(.36f,.32f,.3f);
                Vector3 Lying(float x,float z)=>new(x,kit.Y(x,z)+.1f,z);
                void Run(params Vector3[] path){cable.Slot(0).Tube(path,Enumerable.Repeat(.07f,path.Length).ToArray(),6,dark,worn,1,.4f);}
                Run(kit.On(Boiler.x-1.6f,Boiler.y+1.4f,.1f),kit.On(30,160,.1f),kit.On(30.6f,168,.1f),new Vector3(30.4f,-1.6f,173),Lying(29.6f,178.2f),Lying(26.5f,179.6f),Lying(21f,180.8f),Lying(18.6f,182.6f),Lying(16f,184.6f),
                    Lying(15.4f,187f),Lying(16.1f,191.6f),Lying(19.5f,192.6f),Lying(22.6f,192.2f),Lying(27.8f,192.7f),Lying(31.2f,191.4f),Lying(33.5f,190.4f),Lying(38.4f,190.9f),Lying(42.8f,190.3f),
                    Lying(46.2f,190.6f),Lying(49f,192.4f),Lying(54.6f,192.8f),Lying(58.6f,193f),Lying(59.6f,193.4f));
                // ---- the dig: shoring between the ranks, plank treads on every ramp, cables to the rigs, work lamps, spoil and cut blocks, a fallen guard by each seam of alloy.
                var dig=new ZoneKit.Shape();
                void Shore(Vector2 at,float y,float yaw){var f=new Vector3(at.x,y,at.y);var t=Quaternion.Euler(0,yaw,0);
                    foreach(int side in new[]{-1,1})dig.Slot(0).Post(f+t*new Vector3(side*1.3f,-.1f,0),3.6f,.16f,.14f,6,WoodDark,WoodPale);
                    dig.Slot(0).Beam(f+t*new Vector3(-1.6f,3.4f,0),f+t*new Vector3(1.6f,3.4f,0),.22f,.24f,WoodDark,WoodPale);
                    dig.Slot(0).Beam(f+t*new Vector3(-1.3f,.4f,0),f+t*new Vector3(1.3f,3,0),.1f,.1f,WoodDark,WoodPale);}
                foreach(float x in new[]{36.8f,43.6f})Shore(new Vector2(x,197.6f),L1,0);
                foreach(float x in new[]{121.5f,128f})Shore(new Vector2(x,235.6f),L3,0);
                void Treads(Vector2 from,Vector2 to,float wide){var run=to-from;float length=run.magnitude;var along=run/length;float yaw=Mathf.Atan2(along.x,along.y)*Mathf.Rad2Deg;
                    for(float s=.4f;s<length;s+=.9f){var c=from+along*s;dig.Slot(1).Box(new Vector3(c.x,kit.Y(c.x,c.y)+.03f,c.y),new Vector3(wide,.08f,.5f),Quaternion.Euler(0,yaw,0),WoodDark,WoodPale,.5f);}}
                Treads(new Vector2(24,166),new Vector2(24,176),3.4f);Treads(new Vector2(66,186.5f),new Vector2(78,186.5f),3.2f);Treads(new Vector2(97,214),new Vector2(111,214),3.2f);Treads(new Vector2(153,219),new Vector2(165,219),3.2f);
                Run(new Vector3(61.6f,L1+.1f,194.4f),new Vector3(64.2f,L1+.1f,196.6f),new Vector3(66.5f,L1+.1f,197.3f),new Vector3(70,(L1+L2)*.5f+.1f,195.6f),new Vector3(75,L2+.6f,196.2f),new Vector3(80,L2+.1f,197.2f),new Vector3(98,L2+.1f,197.6f));
                Run(new Vector3(113,L3+.1f,232.9f),new Vector3(124,L3+.1f,233),new Vector3(131,L3+.1f,232.8f),new Vector3(140,L3+.1f,232.9f),new Vector3(146,L3+.1f,232.6f));
                kit.Built("Cables",cable,Iron);
                foreach(var (x,z,yaw) in new[]{(31f,190f,200f),(81f,194.5f,160f),(114f,206f,200f),(168f,206f,160f),(22.6f,191.6f,190f),(46.2f,189.8f,170f)})WorkLamp(kit,new Vector2(x,z),yaw);
                // Spoil: heaps of cut chips along the walls' feet, and cut blocks stacked for loading.
                for(int k=0;k<320;k++)
                {
                    var p=new Vector2(14+R()*176,170+R()*66);float clear=FloorAt(p,out float level);if(clear>-.4f||clear<-3.2f||NearWay(p,2.4f)||InFight(p,2)||!InDig(p))continue;
                    for(int j=0;j<4;j++)dig.Slot(2).Lump(new Vector3(p.x+(R()-.5f)*1.2f,level+.05f,p.y+(R()-.5f)*1.2f),new Vector3(.5f+R()*.5f,.2f+R()*.25f,.5f+R()*.4f),k*7+j,.3f,new Color(.86f,.92f,1),new Color(.62f,.74f,.9f),10,5);
                }
                foreach(var (x,z,yaw) in new[]{(19.8f,183.8f,10f),(60f,180f,-20f),(88.5f,195.6f,0f),(132f,176.5f,15f),(150f,232f,-10f),(170.2f,203.6f,30f)})
                {
                    var f=new Vector3(x,kit.Y(x,z),z);var t=Quaternion.Euler(0,yaw,0);
                    for(int k=0;k<5;k++)dig.Slot(3).Box(f+t*new Vector3((k%3-1)*.85f,.4f+(k/3)*.8f,0),new Vector3(.8f,.78f,.8f),t*Quaternion.Euler(0,R()*8-4,0),new Color(.78f,.9f,1),Color.white,.5f);
                }
                // The Muster's plank roads over the cut ice, where their sledges run: down each level's length to its ramp.
                foreach(var (a,b) in new[]{(new Vector2(28,183.5f),new Vector2(62,186.5f)),(new Vector2(80,185),new Vector2(98,189)),(new Vector2(113,214),new Vector2(150,222)),(new Vector2(167,216),new Vector2(186,218))})
                {
                    var run=b-a;var along=run.normalized;var side=new Vector2(-along.y,along.x);float y=Mathf.Max(kit.Y(a.x,a.y),kit.Y(b.x,b.y))+.04f;
                    foreach(int lane in new[]{-1,1})dig.Slot(1).Deck(new Vector3(a.x+side.x*lane*.8f,y,a.y+side.y*lane*.8f),new Vector3(b.x+side.x*lane*.8f,y,b.y+side.y*lane*.8f),1.3f,.06f,WoodDark,rng,.38f,.05f);
                }
                // Duckboards where the Muster stand to work (D378): beside each bed where blocks come out. Slats on two bearers, a hand off the ice; none on a walked line.
                foreach(var (a,b,w) in new[]{(new Vector2(15.2f,181.2f),new Vector2(15.6f,185.8f),.9f),(new Vector2(81.8f,173.6f),new Vector2(82.2f,179.4f),.9f),
                    (new Vector2(178.6f,204.2f),new Vector2(184f,203.8f),.9f),(new Vector2(85.8f,215.6f),new Vector2(85.8f,219.2f),.8f)})
                {
                    if(NearWay(a,1.6f)||NearWay(b,1.6f)||InFight(a)||InFight(b))throw new InvalidOperationException("FROZEN_HOST duckboard on a walked line "+a+" "+b);
                    var from=new Vector3(a.x,kit.Y(a.x,a.y)+.07f,a.y);var to=new Vector3(b.x,kit.Y(b.x,b.y)+.07f,b.y);var across=Vector3.Cross(Vector3.up,(to-from).normalized)*w*.36f;
                    foreach(int s in new[]{-1,1})dig.Slot(1).Beam(from+across*s+Vector3.down*.045f,to+across*s+Vector3.down*.045f,.09f,.05f,WoodDark);
                    dig.Slot(1).Deck(from,to,w,.035f,WoodPale,rng,.17f,.06f);
                }
                // Spoil beside every bed: the chips the saws threw out, heaped.
                foreach(var bed in Beds)foreach(float side in new[]{1f,-1f})
                {
                    var p=bed.Side(side*(1+R()*.4f))+new Vector2(0,(R()-.5f)*1.2f);if(NearWay(p,1.8f)||InFight(p)||FloorAt(p,out float level)>-.6f)continue;
                    for(int j=0;j<5;j++)dig.Slot(2).Lump(new Vector3(p.x+(R()-.5f)*1.1f,level+.04f,p.y+(R()-.5f)*1.1f),new Vector3(.3f+R()*.3f,.14f+R()*.16f,.3f+R()*.25f),(int)(p.x*7+p.y)+j,.35f,new Color(.9f,.95f,1),new Color(.6f,.72f,.88f),10,5);
                }
                kit.Built("Dig works",dig,Timber,Timber,SnowFloor,IceWall);
                DigLevels(kit,steam);
                WallFaces(kit);
                var hoses=new ZoneKit.Shape();
                for(int i=0;i<3;i++)foreach(float off in new[]{-9f,0f,9f})
                {
                    var from=Bearing(CouplingBearings[i],CouplingOut+.9f);var to=Bearing(CouplingBearings[i]+off,ShelfRadius-2.1f);var mid=Vector2.Lerp(from,to,.5f)+(Vector2)(Quaternion.Euler(0,0,off*2)*(to-from).normalized)*.6f;
                    hoses.Slot(0).Tube(new[]{new Vector3(from.x,Bottom+.5f,from.y),new Vector3(mid.x,Bottom+.16f,mid.y),new Vector3(to.x,Bottom+.2f,to.y),new Vector3(to.x,Bottom+1.2f,to.y)},new[]{.18f,.18f,.18f,.18f},8,new Color(.3f,.28f,.28f),new Color(.46f,.42f,.4f),1,.4f);
                    for(int k=1;k<4;k++){var c=Vector2.Lerp(from,to,k/4f);hoses.Slot(0).Tube(new[]{new Vector3(c.x,Bottom+.16f,c.y)-new Vector3(to.x-from.x,0,to.y-from.y).normalized*.08f,new Vector3(c.x,Bottom+.16f,c.y)+new Vector3(to.x-from.x,0,to.y-from.y).normalized*.08f},new[]{.24f,.24f},8,Hazard,Hazard);}
                }
                kit.Built("Coupling hoses",hoses,Iron);
                if(poses!=null)
                    foreach(var (x,z,y,yaw) in new[]{(138f,208.4f,L3,80f),(56f,179.6f,L1,250f)})
                    {
                        var fallen=Statues("Fallen guard "+x,new Vector2(x,z),y+.35f,yaw,1,1);fallen.transform.GetChild(0).rotation=Quaternion.Euler(0,yaw,84);
                    }
                // ---- icicles under every block's top edge and along the bottom gallery's shelf.
                var ice=new ZoneKit.Shape();int seed=0;
                foreach(var b in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.EndsWith(" ice")&&r.sharedMaterial==IceBlock&&r.GetComponentInParent<Membrane>()==null).Select(r=>r.bounds).ToArray())
                    Icicles(ice,new Vector3(b.min.x+.2f,b.max.y-.05f,b.min.z-.02f),new Vector3(b.max.x-.2f,b.max.y-.05f,b.min.z-.02f),400+seed++,.6f);
                for(float b=300;b<=480;b+=4){var a=Bearing(b,18.3f);var c=Bearing(b+4,18.3f);Icicles(ice,new Vector3(a.x,Bottom+3.9f,a.y),new Vector3(c.x,Bottom+3.9f,c.y),600+seed++,1.1f);}
                var hung=kit.Built("Icicles",ice,IceBlock);hung.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                // ---- air: spindrift low over the Reach, rime glitter in the dig, steam off the amber.
                var flake=kit.Glow("Spindrift",true,new Color(1.05f,1.08f,1.12f,1));
                kit.Drift("Spindrift",new Vector3(0,.5f,78),new Vector3(110,.8f,170),flake,new Color(1,1,1,.16f),700,.4f,1.4f,.8f,9);
                var glitter=kit.Glow("Rime",false,new Color(1.2f,1.3f,1.5f,1));
                kit.Drift("Rime glitter",new Vector3(110,-8,206),new Vector3(200,10,72),glitter,new Color(.85f,.95f,1,.5f),900,.03f,.08f,.15f,12);
                // The couplings' own steam is theirs (Coupling): thin, and only while each runs.
                foreach(var (x,y,z) in new[]{(40.2f,L1,196f),(169.6f,Bottom,231f)})
                    kit.Drift("Thaw steam "+Mathf.RoundToInt(x),new Vector3(x,y+1.6f,z),new Vector3(5,2.4f,1.6f),steam,new Color(1,.92f,.84f,.22f),90,.8f,2,.4f,5);
                EmitLight();
            }

            /// <summary>What makes each level of the dig its own (docs/maps/FrozenHost.md, "The three levels"). The upper gallery is the Muster's worksite: a lean-to, shear legs over a
            /// cut block, tool racks, sledges of cut ice, spoil heaps, lamps on tripods (the cable and the ruts are elsewhere). The second level is where their thaw has been at the ice:
            /// the warmed ranks (built with the others), thaw lines along its walls, amber light and steam over the slumped sills. The third is the oldest: Ancelane columns standing at
            /// its ways in and out, the footings of a colonnade flush in its floor, the host's standards in front of its ranks and leaning up out of its ice. The bottom gallery is their
            /// muster ground (the rings under its ice are the host beneath's), the Marshal's post inlaid in its floor where it stands, and its dais in the niche behind the shelf.</summary>
            static void DigLevels(ZoneKit kit,Material steam)
            {
                var timber=new ZoneKit.Shape();var iron=new ZoneKit.Shape();var held=new GameObject("Worksite solids");var rng=new System.Random(4401);float R()=>(float)rng.NextDouble();
                // ---- the upper gallery: the Muster's worksite
                kit.Prop(ZoneKit.Kit("MusterLeanTo","DressCargoPallet"),new Vector3(16.4f,0,178.4f),2.6f,60).name="Muster lean-to";
                // Shear legs over a cut block: three timbers to a head, a chain down, a block hanging off the floor over the one it was cut beside. A mesh of their own: the
                // worksite's low pieces share one that spans the gallery, and a tall thing in that would make the whole of it a wall to the camera's checks.
                {
                    var timberAll=timber;var ironAll=iron;timber=new ZoneKit.Shape();iron=new ZoneKit.Shape();
                    var foot=new Vector3(18.2f,L1,186.4f);var head=foot+Vector3.up*4.4f;
                    for(int k=0;k<3;k++){var leg=foot+Quaternion.Euler(0,40+k*120,0)*Vector3.forward*1.35f;timber.Slot(0).Beam(leg,head,.16f,.16f,WoodDark,WoodPale);ZoneKit.Block(held,leg+Vector3.up*.6f,new Vector3(.35f,1.2f,.35f),0,"Shear leg");}
                    timber.Slot(0).Box(head,new Vector3(.36f,.3f,.36f),Quaternion.identity,WoodDark,WoodPale);
                    iron.Slot(0).Beam(head,foot+Vector3.up*1.9f,.05f,.05f,IronDark).Box(foot+Vector3.up*1.9f,new Vector3(.5f,.06f,.5f),Quaternion.identity,Hazard);
                    iron.Slot(1).Box(foot+Vector3.up*1.45f,new Vector3(.82f,.8f,.82f),Quaternion.Euler(0,14,4),new Color(.78f,.9f,1),Color.white);
                    iron.Slot(1).Box(foot+new Vector3(.9f,.4f,.3f),new Vector3(.8f,.78f,.8f),Quaternion.Euler(0,-8,0),new Color(.78f,.9f,1),Color.white);
                    ZoneKit.Block(held,foot+new Vector3(.3f,.9f,.15f),new Vector3(1.5f,1.8f,1),0,"Hanging block");
                    kit.Built("Shear legs",timber,Timber);kit.Built("Shear legs' chain and blocks",iron,Iron,IceWall);timber=timberAll;iron=ironAll;
                }
                ToolRack(timber,iron,held,new Vector3(14.75f,L1,189.6f),-90,1);ToolRack(timber,iron,held,new Vector3(36,L1,189.7f),0,2);
                foreach(var (x,z,yaw) in new[]{(19.8f,190.6f,85f),(51.6f,191.1f,95f)})LoadedSledge(timber,iron,held,new Vector3(x,L1,z),yaw);
                foreach(var (x,z,size) in new[]{(42f,178.3f,1.1f),(62.8f,178.2f,1.3f)}){var heap=kit.Prop(ZoneKit.Kit("DressSpoilHeap","DressBoulders"),new Vector3(x,0,z),size,R()*360);heap.name="Spoil heap";heap.AddComponent<PropTint>().tint=new Color(.86f,.92f,1f);}
                // ---- the second level: the thaw lines the Muster ran along its walls from rig two to its ranks, frosted and dead (pale with rime, their paint gone dull, rime
                // crusted along them); nothing warm over the ranks they fed.
                var lines=new ZoneKit.Shape();var deadPaint=new Color(.7f,.64f,.6f);
                void Hose(params Vector3[] path)
                {
                    lines.Slot(0).Tube(path,Enumerable.Repeat(.13f,path.Length).ToArray(),7,new Color(.66f,.72f,.8f),new Color(.82f,.88f,.95f),1,.4f);
                    for(int i=1;i<path.Length;i++)
                    {
                        var c=Vector3.Lerp(path[i-1],path[i],.5f);var d=(path[i]-path[i-1]).normalized*.09f;lines.Slot(0).Tube(new[]{c-d,c+d},new[]{.18f,.18f},7,deadPaint,deadPaint);
                        float length=Vector3.Distance(path[i-1],path[i]);
                        for(float t=.6f;t<length;t+=1.4f){var at=Vector3.Lerp(path[i-1],path[i],t/length);lines.Slot(1).Lump(at+Vector3.up*.1f,new Vector3(.26f,.09f,.22f),(int)(at.x*10+at.z),.35f,SnowWhite,SnowShade,8,4);}
                    }
                }
                float y2=L2+.13f;
                Hose(new Vector3(99.4f,y2,196.2f),new Vector3(97.6f,y2,197.2f),new Vector3(93,y2,197.6f),new Vector3(88,y2,197.4f),new Vector3(85.2f,y2,196.6f),new Vector3(84.8f,y2,194.2f));
                Hose(new Vector3(101.8f,y2,195.4f),new Vector3(103.6f,y2,192.6f),new Vector3(108,y2,192.4f),new Vector3(113.2f,y2,192.6f),new Vector3(115.6f,y2,190.6f),new Vector3(121,y2,190.7f),new Vector3(127.6f,y2,190.6f));
                kit.Built("Thaw lines",lines,Iron,SnowFloor);
                // ---- the third level: the oldest, where the Ancelane stonework the host stood on comes up through the ice
                var works=new ZoneKit.Shape();
                // Broken off under three metres: they stand beside the third level's fights, where nothing taller and broad may stand on the camera's side.
                foreach(var (x,z,tall,yaw) in new[]{(113.4f,206f,2.8f,10f),(113.4f,222.2f,2.2f,70f),(149.8f,207.9f,2.9f,40f)})
                {var column=kit.Prop(ZoneKit.Kit("AncelaneColumn","HushwellColumn"),new Vector3(x,-.3f,z),tall,yaw);column.name="Ancelane column";column.AddComponent<PropTint>().tint=new Color(.84f,.88f,.98f);}
                // A drum of a fallen column, half in the ice.
                works.Slot(1).Tube(new[]{new Vector3(116.4f,L3+.3f,204.9f),new Vector3(118,L3+.3f,205.3f)},new[]{.48f,.46f},10,StoneDark,StonePale,1,.5f);
                ZoneKit.BlockAlong(held,new Vector3(116.4f,L3+.4f,204.9f),new Vector3(118,L3+.4f,205.3f),.9f,.8f,"Column drum");
                // The host's standards in front of its ranks, and two leaning up out of the floor's ice where the ice has pushed them.
                foreach(var (x,z,lean,tall) in new[]{(120.5f,232.6f,0f,4.4f),(127.5f,232.6f,4f,4.6f),(134.5f,232.6f,-3f,4.2f),(113.6f,229f,14f,3.6f),(141.5f,206.2f,-12f,3.4f)})
                    Standard(works,held,new Vector3(x,L3,z),lean,tall,(int)(x*10));
                // Along the road: more of the host's standards leaning up out of the ice, and drums of the colonnade lying half in it.
                foreach(var (x,z,lean,tall) in new[]{(119f,211.2f,-9f,3.6f),(131.2f,221.6f,11f,3.9f),(143.4f,217.2f,-15f,3.3f),(124.6f,224.6f,6f,3.4f)})
                    Standard(works,held,new Vector3(x,L3,z),lean,tall,(int)(x*10));
                foreach(var (x0,z0,x1,z1) in new[]{(127f,209.4f,128.6f,209f),(140.2f,213.6f,141.1f,212.4f)})
                {works.Slot(1).Tube(new[]{new Vector3(x0,L3+.3f,z0),new Vector3(x1,L3+.3f,z1)},new[]{.46f,.44f},10,StoneDark,StonePale,1,.5f);ZoneKit.BlockAlong(held,new Vector3(x0,L3+.4f,z0),new Vector3(x1,L3+.4f,z1),.9f,.8f,"Column drum");}
                // ---- the bottom gallery: the Marshal's post inlaid where it stands (flat: the floor stays the contract's), and its dais in the niche behind the shelf, with the great
                // standard of the host over it, seen over the shelf's ranks.
                var post=new ZoneKit.Shape();var mark=Bearing(0,MarshalOut);var centre=new Vector3(mark.x,Bottom+.012f,mark.y);
                post.Slot(0).Disc(centre,Vector3.up,1.15f,24,StonePale,0,StoneDark);
                post.Slot(1).Disc(centre+Vector3.up*.003f,Vector3.up,2.7f,40,BronzeDark,2.45f,BronzePale).Disc(centre+Vector3.up*.003f,Vector3.up,1.32f,24,BronzePale,1.18f,BronzeDark);
                for(int k=0;k<8;k++){var d=Quaternion.Euler(0,k*45+22.5f,0)*Vector3.forward;post.Slot(1).Box(centre+d*1.85f+Vector3.up*.004f,new Vector3(.16f,.006f,1.05f),Quaternion.LookRotation(d),BronzeDark,BronzePale);}
                var inlaid=kit.Built("Floor detail the Marshal's post",post,Stone,Bronze);inlaid.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                {
                    var dais=new Vector3(C.x,Bottom,239.6f);
                    works.Slot(1).Box(dais+Vector3.up*.2f,new Vector3(6.4f,.4f,3.4f),Quaternion.identity,StoneDark,StonePale).Box(dais+new Vector3(0,.6f,.4f),new Vector3(4.6f,.4f,2.4f),Quaternion.identity,StoneDark,StonePale);
                    Standard(works,null,dais+new Vector3(0,.8f,.9f),0,6.4f,7,1.9f);
                    foreach(int side in new[]{-1,1})
                    {
                        var brazier=dais+new Vector3(side*2.6f,.4f,-.6f);
                        for(int k=0;k<3;k++){var leg=brazier+Quaternion.Euler(0,k*120,0)*Vector3.forward*.4f;works.Slot(0).Beam(leg,brazier+Vector3.up*1.1f,.06f,.06f,BronzeDark,BronzePale);}
                        works.Slot(0).Lump(brazier+Vector3.up*1.2f,new Vector3(.48f,.18f,.48f),side+9,.08f,BronzePale,BronzeDark,12,5);
                    }
                }
                var ancelane=kit.Built("Ancelane works",works,Bronze,Stone);
                var worksite=kit.Built("Muster worksite",timber,Timber);var tools=kit.Built("Muster tools",iron,Iron,IceWall);
                held.transform.SetParent(worksite.transform,true);foreach(var box in held.GetComponentsInChildren<BoxCollider>())box.gameObject.isStatic=true;
                var cloth=new ZoneKit.Shape();foreach(var (at,lean,tall,seed,wide) in standards)Banner(cloth,at,lean,tall,seed,wide);standards.Clear();
                kit.Built("Host standards' cloth",cloth,Cloth);
            }
            static readonly List<(Vector3 at,float lean,float tall,int seed,float wide)> standards=new();
            /// <summary>A standard of the host's: a bronze pole (leaning <paramref name="lean"/> degrees east or west), its crossbar, the ring at its top; its cloth is hung by
            /// <see cref="Banner"/>. Solid at its foot when <paramref name="held"/> is given.</summary>
            static void Standard(ZoneKit.Shape works,GameObject held,Vector3 foot,float lean,float tall,int seed,float wide=1)
            {
                var up=Quaternion.Euler(0,0,lean)*Vector3.up;var bar=foot+up*(tall-.5f);
                works.Slot(0).Tube(new[]{foot+Vector3.down*.4f,foot+up*tall},new[]{.08f*wide,.06f*wide},8,BronzeDark,BronzePale,1,.25f).Beam(bar-Vector3.right*.62f*wide,bar+Vector3.right*.62f*wide,.07f*wide,.07f*wide,BronzePale);
                var ring=foot+up*(tall+.3f*wide);
                works.Slot(0).Disc(ring,Vector3.back,.34f*wide,16,BronzePale,.24f*wide,BronzeDark).Disc(ring,Vector3.forward,.34f*wide,16,BronzePale,.24f*wide,BronzeDark);
                for(int k=0;k<4;k++){var d=Quaternion.Euler(0,0,k*45)*Vector3.up*.24f*wide;works.Slot(0).Beam(ring-d,ring+d,.035f*wide,.035f*wide,BronzePale);}
                standards.Add((bar,lean,tall,seed,wide));
                if(held!=null)ZoneKit.Block(held,foot+Vector3.up*1,new Vector3(.35f,2,.35f),0,"Standard");
            }
            static void Banner(ZoneKit.Shape cloth,Vector3 bar,float lean,float tall,int seed,float wide)
            {
                var rng=new System.Random(seed);
                for(int k=0;k<3;k++){float x=(-.55f+k*.37f)*wide,drop=(1.2f+(float)rng.NextDouble()*.5f)*wide;var a=bar+new Vector3(x,0,-.03f);var b=a+Vector3.right*.33f*wide;
                    cloth.Slot(0).Quad(a,b,b+Vector3.down*(drop+(float)rng.NextDouble()*.25f),a+Vector3.down*drop,Vector3.back,Color.Lerp(Ochre,new Color(.7f,.6f,.5f),(float)rng.NextDouble()*.5f),.6f);}
            }
            /// <summary>A tool rack of the Muster's: a timber A-frame, and on it ice saws, picks, tongs and a pry bar; solid.</summary>
            static void ToolRack(ZoneKit.Shape timber,ZoneKit.Shape iron,GameObject held,Vector3 foot,float yaw,int seed)
            {
                var t=Quaternion.Euler(0,yaw,0);
                foreach(int side in new[]{-1,1}){var f=foot+t*new Vector3(side*1.1f,0,0);var top=f+Vector3.up*1.6f;timber.Slot(0).Beam(f+t*new Vector3(0,0,-.38f),top,.1f,.1f,WoodDark,WoodPale).Beam(f+t*new Vector3(0,0,.38f),top,.1f,.1f,WoodDark,WoodPale);}
                timber.Slot(0).Beam(foot+t*new Vector3(-1.3f,1.55f,0),foot+t*new Vector3(1.3f,1.55f,0),.12f,.12f,WoodDark,WoodPale).Beam(foot+t*new Vector3(-1.2f,.7f,-.2f),foot+t*new Vector3(1.2f,.7f,-.2f),.08f,.08f,WoodDark,WoodPale);
                for(int k=0;k<5;k++)
                {
                    var hang=foot+t*new Vector3(-.9f+k*.45f,1.5f,-.1f);
                    switch((k+seed)%4)
                    {
                        case 0:iron.Slot(0).Box(hang+t*new Vector3(0,-.55f,0),new Vector3(.22f,.95f,.02f),t,IronPale,IronPale);timber.Slot(0).Beam(hang,hang+Vector3.down*.14f,.06f,.06f,WoodDark);break;
                        case 1:timber.Slot(0).Beam(hang,hang+t*new Vector3(.05f,-1.1f,-.05f),.05f,.05f,WoodDark,WoodPale);iron.Slot(0).Box(hang+t*new Vector3(.05f,-1.12f,-.05f),new Vector3(.42f,.07f,.06f),t,IronDark,IronPale);break;
                        case 2:iron.Slot(0).Beam(hang,hang+t*new Vector3(-.12f,-.8f,0),.03f,.03f,IronDark).Beam(hang,hang+t*new Vector3(.12f,-.8f,0),.03f,.03f,IronDark);break;
                        default:iron.Slot(0).Beam(hang,hang+t*new Vector3(.1f,-1.25f,-.1f),.04f,.04f,Hazard,IronDark);break;
                    }
                }
                ZoneKit.Block(held,foot+Vector3.up*.8f,new Vector3(2.6f,1.6f,.9f),yaw,"Tool rack");
            }
            /// <summary>A Muster sledge loaded with cut blocks for the winch; solid.</summary>
            static void LoadedSledge(ZoneKit.Shape timber,ZoneKit.Shape iron,GameObject held,Vector3 foot,float yaw)
            {
                var t=Quaternion.Euler(0,yaw,0);
                foreach(int side in new[]{-1,1})iron.Slot(0).Beam(foot+t*new Vector3(side*.8f,.1f,-2.2f),foot+t*new Vector3(side*.8f,.25f,2.4f),.12f,.12f,IronDark,IronPale);
                timber.Slot(0).Box(foot+t*new Vector3(0,.3f,0),new Vector3(1.7f,.08f,4.2f),t,WoodDark,WoodPale);
                for(int k=0;k<5;k++)iron.Slot(1).Box(foot+t*new Vector3((k%2-.5f)*.8f,.74f+(k/4)*.82f,-1.3f+(k%4)*.85f),new Vector3(.76f,.8f,.8f),t,new Color(.8f,.9f,1),new Color(.95f,.98f,1));
                ZoneKit.Block(held,foot+Vector3.up*.7f,new Vector3(1.8f,1.4f,4.6f),yaw,"Sledge");
            }

            /// <summary>The floor: snow wind-crusted in ridges over most of the Reach, bluer in its hollows and scoured to bare blue ice in a few, moraine grit only in thin streaks and at
            /// the vents' hearts, Idra's steps and the herd's churned line pressed into it, the sledges' runs into the dig; in the dig, cut ice in slabs with dark joints, snow drifted in
            /// against the walls, ice chips and spoil at the walls' feet, meltwater stains where the amber is, scorch where the rigs work, frost cracking the bottom gallery.</summary>
            static void Ground(ZoneKit kit)
            {
                // The zone's own two layers. Wind crust over twelve metres: ridges a long stride apart in three fields that run three ways (east-west and either diagonal) and meet in
                // soft seams, smooth packed patches between, each ridge lit on its windward face with a blue lee behind it. And the dig's cut ice (slabs the Muster's blades cut, each its
                // own tone, wandering joints between, a few scores and chips), which is also the Reach's scoured ice.
                ZoneKit.Texture("Ground/ground-host-crust",1024,(u,v)=>
                {
                    float warp=ZoneKit.Cloud(u,v,3,7)*2.2f+ZoneKit.Cloud(u,v,9,3)*.5f,field=ZoneKit.Cloud(u,v,2,41);
                    float wa=1-ZoneKit.Smooth(.38f,.5f,field),wc=ZoneKit.Smooth(.5f,.62f,field),wb=Mathf.Clamp01(1-wa-wc);
                    static float Ridge(float phase){float r=Mathf.Repeat(phase,1);return r<=.66f?.05f*Mathf.SmoothStep(0,1,r/.66f):-.11f*(1-.5f*Mathf.SmoothStep(0,1,(r-.66f)/.34f));}
                    // Where two fields meet the ridges die out into smooth snow, so no seam zig-zags.
                    float ridged=(wa*Ridge(v*20+warp)+wb*Ridge((u+v)*14+warp*1.2f)+wc*Ridge((u-v)*16+warp*1.1f))*ZoneKit.Smooth(.6f,.9f,Mathf.Max(wa,Mathf.Max(wb,wc)));
                    float flat=ZoneKit.Smooth(.56f,.7f,ZoneKit.Cloud(u,v,3,59)),plate=ZoneKit.Cloud(u,v,12,19),crack=Mathf.Abs(ZoneKit.Cloud(u,v,16,29)-.5f),grain=ZoneKit.Cloud(u,v,96,5);
                    float shown=ridged*(1-.75f*flat),lee=Mathf.Max(0,-shown)/.11f;
                    float tone=.9f+shown+.02f*flat+.05f*(plate-.5f)+.03f*(grain-.5f)-(crack<.006f?.1f:0);
                    return new Color(tone*(1-.1f*lee),tone*(1-.05f*lee),Mathf.Min(1,tone*1.02f),1);
                });
                ZoneKit.Texture("Ground/ground-host-cutice",1024,(u,v)=>
                {
                    // The floor the Muster's blades left: a flat cut through the glacier's years (D378; it was a cell pattern of plates, one crack network from wall to wall).
                    // Each year a band across the floor, folded as the ice moved, its own tone, a grey seam of dust at some and a bluer, denser line at others; a fracture now and
                    // then wandering across the bands with a pale lip beside it; the blades' scores in runs of parallel grooves a hand apart, each run its own way, where a block was
                    // cut; chips and bubbles. Seven metres to a repeat; the zone's own strata (Ground: Strata) lie over it at a larger scale so the repeat doesn't show.
                    float fold=ZoneKit.Cloud(u,v,2,81)*2.2f+ZoneKit.Cloud(u,v,5,83)*.5f+.3f*Mathf.Sin((u+v)*Mathf.PI*2);
                    float lay=v*11+fold;int year=Mathf.FloorToInt(lay);float within=lay-year;int y11=((year%11)+11)%11;
                    float own=ZoneKit.Tile(y11*3.7f,y11*1.3f,64,85),grain=ZoneKit.Cloud(u,v,14,87),fine=ZoneKit.Cloud(u,v,48,89),chip=ZoneKit.Tile(u*160,v*160,160,91);
                    float tone=.85f+.2f*(own-.5f)+.05f*(grain-.5f)+.025f*(fine-.5f),blue=.05f+.08f*own;
                    float seam=Mathf.Min(within,1-within);
                    if(own>.55f&&seam<.06f)tone-=.17f*(1-seam/.06f);else if(own<.25f&&seam<.07f){tone-=.06f;blue+=.12f;}else if(seam<.02f)tone-=.07f;
                    // (Fractures are the ground kit's crack decals, branching and placed: a noise field's contours here read as wandering outlines, ch45-d378-a.)
                    // Scores: three ways a run can lie (whole repeats across the texture, so it tiles), each where its own field is strongest, over about a third of the floor.
                    float ra=ZoneKit.Cloud(u,v,3,97),rb=ZoneKit.Cloud(u,v,3,99),rc=ZoneKit.Cloud(u,v,3,101),best=Mathf.Max(ra,Mathf.Max(rb,rc));
                    float groove=best==ra?Mathf.Repeat(u*27+v*6,1):best==rb?Mathf.Repeat(v*26-u*7,1):Mathf.Repeat((u-v)*19,1);
                    float scored=ZoneKit.Smooth(.5f,.58f,best)*ZoneKit.Smooth(.3f,.45f,ZoneKit.Cloud(u,v,6,103));
                    if(groove<.18f)tone-=.1f*scored;else if(groove<.27f)tone+=.04f*scored;
                    if(chip>.965f)tone+=.07f;
                    return new Color(tone*(1-blue*1.3f),tone*(1-blue*.45f),Mathf.Min(1,tone*(1+blue*.25f)),1);
                });
                var ground=GroundKit.Open(kit.zone,GroundKit.FindFloor(),37,CliffTop-.2f);
                float Smooth(float a,float b,float x)=>GroundKit.Smooth(a,b,x);
                float Lvl(Vector2 p){FloorAt(p,out float l);return l;}
                bool Dig(Vector2 p)=>InDig(p);
                float Vent(Vector2 p){float v=0;foreach(var (at,radius) in Vents)v=Mathf.Max(v,1-Smooth(radius*.2f,radius*.55f,Vector2.Distance(p,at)));return v;}
                float Hollow(Vector2 p){float v=0;foreach(var (at,radius) in Vents)v=Mathf.Max(v,1-Smooth(radius,radius*1.4f,Vector2.Distance(p,at)));return v;}
                // Snow drifted into the dig: against the walls' feet and in patches across the cut ice; less of it on the second level, which the thaw has been at, and on the third.
                // The ice is a higher layer than the snow, so it is left off wherever the snow lies.
                float DigSnow(Vector2 p){int f=FloorOf(p);return Mathf.Min(1,.9f*Smooth(2.6f,.6f,ground.Edge(p))*Smooth(.3f,.5f,ground.Noise(p,4,9))+.85f*Smooth(.5f,.62f,ground.Noise(p,8,15)))*(f==2?.6f:f==3?.8f:f==4?.8f:1);}
                // The Reach scoured to blue ice by the wind: down the herd's line (their hooves and the wind keep it bare), in windows over the host (the spear field's and the stalkers'
                // field's), and in its lowest hollows and down the crevasses.
                float Scoured(Vector2 p)
                {
                    if(Dig(p))return 0;
                    float herd=1-Smooth(2.4f,4.4f,ZoneKit.FromPath(p,HerdLine)+(ground.Noise(p,3,61)-.5f)*2.6f),window=0;
                    foreach(var (at,r) in IceWindows)window=Mathf.Max(window,1-Smooth(r*.55f,r,Vector2.Distance(p,at)+(ground.Noise(p,2.5f,63)-.5f)*r*.7f));
                    float hollow=Smooth(.02f,-.24f,Lvl(p))*Smooth(.48f,.62f,ground.Noise(p,10,13));
                    return Mathf.Max(Mathf.Max(herd,window),hollow)*(1-Hollow(p));
                }
                foreach(var way in Ways)ground.ClearLane(way,1.4f);
                ground.Look("Canyon",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Hoarfell/host-ice.png"));
                ground.Layers(new Vector4(.05f,.3f,.38f,.3f),1.1f,new GroundKit.Layer("host-crust",new Color(1,1,1),12),new GroundKit.Layer("host-cutice",new Color(.94f,.97f,1),7),
                    new GroundKit.Layer("scree",new Color(.6f,.6f,.64f),3.6f));
                // Crust over most of the Reach in fields a dozen metres across, the polished snow between; thinning in the hollows; in the dig, snow drifted in against the walls' feet and in a few patches.
                ground.Paint(0,p=>Dig(p)?DigSnow(p):Smooth(.33f,.49f,ground.Noise(p,13,3))*(1-Vent(p))*Smooth(-.3f,0,Lvl(p)));
                // Cut ice over the dig's floors; blue ice the wind has scoured bare on the Reach.
                ground.Paint(1,p=>Dig(p)?.95f*(1-DigSnow(p)):Scoured(p));
                // Grit: thin moraine streaks on the Reach, the vents' warm hearts, spoil right at the dig's wall feet.
                ground.Paint(2,p=>Dig(p)?.5f*(1-Smooth(.3f,1.2f,ground.Edge(p))):Smooth(.64f,.7f,ground.Noise(p,16,17))*.55f*(1-Hollow(p)));
                // The years again at the floor's own scale (D378): broad bands, folded, each a shade of its own, so the cut ice's seven-metre repeat lies under something that never
                // repeats; and the beds where blocks have come out, their lifted cells a hand lower and in shade.
                float Strata(Vector2 p)
                {
                    float s=(p.x*.38f+p.y*.92f+6*Mathf.Sin(p.x*.05f+1.3f)+4*Mathf.Sin(p.y*.08f+.4f))/3.1f+ground.Noise(p,16,91)*1.6f;int band=Mathf.FloorToInt(s);
                    float own=Mathf.PerlinNoise(band*.73f+.31f,band*.29f+.17f)-.5f,next=Mathf.PerlinNoise((band+1)*.73f+.31f,(band+1)*.29f+.17f)-.5f;
                    return .55f*Mathf.Lerp(own,next,Smooth(.85f,1,s-band));
                }
                float Lifted(Vector2 p){foreach(var bed in Beds)if(bed.Lifted(p))return .16f;return 0;}
                ground.Shade(p=>Dig(p)?.16f*Smooth(1.8f,.3f,ground.Edge(p))+Strata(p)+Lifted(p):.08f*Smooth(.1f,-.3f,Lvl(p))).ShadeRelief(p=>Dig(p)?0:Drifts(p),3);
                // Idra's steps across the Reach ("stay in my steps"), the herd's churned line, sledge runners down into the dig.
                ground.Strip("Tracks",Ways[0],.35f,1.1f,new Color(.56f,.66f,.86f,.85f));
                ground.Strip("Tracks",HerdLine,0,3.2f,new Color(.52f,.6f,.8f,.6f));ground.Strip("Tracks",HerdLine,1.6f,2.2f,new Color(.56f,.64f,.82f,.5f));
                ground.Strip("Ruts",new[]{new Vector2(12,146),new Vector2(20,156),new Vector2(24,166),new Vector2(24,178),new Vector2(40,186),new Vector2(62,187)},0,2.2f,new Color(.56f,.64f,.8f,.75f));
                // The upper gallery is the Muster's worksite: their sledges' ruts from the shear legs and the loaded sledges to the road, and the lines they have scribed on the ice
                // where the next blocks come out.
                foreach(var run in new[]{new[]{new Vector2(19.8f,190.6f),new Vector2(26,188.4f),new Vector2(33,187.6f)},new[]{new Vector2(51.6f,191.1f),new Vector2(45,189.2f),new Vector2(38,188.2f)},
                    new[]{new Vector2(18.2f,186.4f),new Vector2(21,183.4f),new Vector2(24,180)}})ground.Strip("Ruts",run,0,1.8f,new Color(.58f,.66f,.82f,.7f));
                var scribed=new Color(.86f,.46f,.28f,.9f);
                foreach(float z in new[]{179.6f,189.4f})ground.Strip("SurveyLine",new[]{new Vector2(15.4f,z),new Vector2(62.4f,z)},0,.5f,scribed);
                foreach(float x in new[]{17.5f,29f,40.5f,52f,60.2f})ground.Strip("SurveyLine",new[]{new Vector2(x,177.2f),new Vector2(x,192.8f)},0,.5f,scribed);
                // Pools first, before any crack or pebbles can lie where one would: a steaming pool at each vent's heart.
                int pools=0;
                foreach(var (at,radius) in Vents)if(ground.Puddle(at+new Vector2(.4f,-.3f),radius*.26f,radius*.2f,20))pools++;
                Debug.Log("FROZEN_HOST_POOLS "+pools);
                // Meltwater in the dig (D378): where the rigs' heat ran off and lies on the cut ice, over the second level the thaw went across, in the beds, by the winch.
                int melt=0;
                foreach(var (x,z,rx,rz,yaw) in new[]{(84.2f,175.4f,1.5f,1f,20f),(89f,179.6f,1.1f,.8f,-30f),(88f,218.2f,1.2f,.8f,15f),(170.4f,224.4f,1.5f,1f,0f),(182.6f,227.6f,1.2f,.9f,30f),
                    (171.4f,209.6f,1f,.8f,-15f),(62.2f,182f,1f,.8f,0f),(16.6f,188.6f,.8f,.6f,0f),(136f,234f,1f,.7f,0f),(180.6f,208.6f,.9f,.7f,10f)})
                    if(ground.Puddle(new Vector2(x,z),rx,rz,yaw))melt++;
                Debug.Log("FROZEN_HOST_MELTWATER "+melt);
                // The beds' kerfs: sawn lines a hand wide round every block, dark with the cold in them.
                foreach(var bed in Beds)foreach(var (a,b) in bed.Kerfs())ground.Strip("SurveyLine",new[]{a,b},0,.12f,new Color(.3f,.4f,.58f,.9f));
                // Drift shadows on the Reach (blue), snow patches on the dig's ice (white): what makes one stretch of either unlike the next.
                ground.Decals(new[]{"Shadow"},4,p=>Dig(p)?0:Smooth(.45f,.6f,ground.Noise(p,8,23))*(1-Vent(p)),2.6f,5,new Color(.5f,.62f,.9f,.45f),false,.1f,25);
                // Meltwater lying on the ice where the thaw has been at it.
                ground.Decals(new[]{"Shadow"},4,p=>Dig(p)?Smooth(.45f,.6f,ground.Noise(p,7,45)):0,2.4f,4.6f,new Color(.55f,.64f,.84f,.5f),false,.1f,47);
                ground.Decals(new[]{"CracksA","CracksB"},7,p=>Dig(p)?.9f:Smooth(0,-.2f,Lvl(p))*.8f,1.4f,3.2f,new Color(.36f,.46f,.66f,.9f),true,.08f,27);
                ground.Decals(new[]{"PebblesA","PebblesB"},8,p=>Dig(p)?.5f+.5f*Smooth(2.4f,.6f,ground.Edge(p)):Mathf.Max(Vent(p),Smooth(.6f,.7f,ground.Noise(p,16,17))*.6f),.8f,1.8f,new Color(.9f,.95f,1),true,.1f,29);
                ground.Decals(new[]{"Stain"},4,p=>Dig(p)?Mathf.Max(Smooth(9,3,Vector2.Distance(p,new Vector2(40,196))),Smooth(10,3,Vector2.Distance(p,new Vector2(169,231)))):Vent(p),2,4,new Color(.42f,.5f,.62f,.55f),false,.1f,31);
                ground.Decals(new[]{"Scorch"},4,p=>{float d=Mathf.Min(Mathf.Min(Vector2.Distance(p,RigOne),Vector2.Distance(p,RigTwo)),Mathf.Min(Vector2.Distance(p,RigThree),Vector2.Distance(p,Boiler)));return Smooth(8,2.5f,d);},1.4f,2.6f,new Color(.36f,.32f,.3f,.7f),false,.1f,33);
                ground.Decals(new[]{"Litter"},5,p=>Dig(p)?.35f:Smooth(146,154,p.y)*.8f,.9f,1.8f,new Color(.5f,.48f,.46f,.85f),false,.1f,35);
                // The bottom gallery: frost cracking out across its floor, darker meltwater toward the couplings.
                ground.Decals(new[]{"CracksA","CracksB"},12,p=>InArena(p)?.9f:0,2,3.6f,new Color(.82f,.9f,1,.9f),false,.1f,41);
                ground.Decals(new[]{"Stain"},12,p=>InArena(p)?.35f+.65f*Smooth(8,14,Vector2.Distance(p,C)):0,2,4,new Color(.44f,.42f,.42f,.55f),false,.1f,43);
                // The second level, where the thaw has been and gone: the stains its meltwater left, frozen into the ice.
                ground.Decals(new[]{"Stain"},5,p=>FloorOf(p)==2?.6f:0,2,4.4f,new Color(.48f,.52f,.58f,.45f),false,.1f,73);
                // The third level and the bottom's mouth: the Ancelane paving the host mustered on, come up through the ice in patches.
                ground.Decals(new[]{"Flagstones","Slabs"},2.6f,p=>{int f=FloorOf(p);return f==3?.7f*(1-DigSnow(p)):f==4&&!InArena(p,-1)?.45f*(1-DigSnow(p)):0;},2.2f,3.8f,new Color(.7f,.71f,.74f,.75f),false,.1f,71);
                // The vent pockets: lichen and moss on the warm ground, a pool steaming at each heart.
                ground.Decals(new[]{"Lichen"},10,p=>Vent(p)*.9f+Hollow(p)*.3f,.6f,1.4f,new Color(.62f,.66f,.36f,.9f),false,.12f,75);
                ground.LampPools(3,new Color(1,.96f,.9f,.3f));
                ground.Cover(new[]{"Shards"},18,p=>Dig(p)?.8f*Smooth(1.8f,.3f,ground.Edge(p))+.2f:Smooth(-.2f,-.6f,Crevasse(p)-1.4f)+.3f*Smooth(1.4f,.4f,ground.Edge(p)),.12f,.34f,new Color(.86f,.94f,1.05f),3);
                ground.Cover(new[]{"TuftA","TuftB","TuftC"},60,p=>Mathf.Max(Vent(p),Hollow(p)*.5f)*Smooth(.1f,.6f,Mathf.Max(Vent(p),Hollow(p)*.5f)),.12f,.32f,new Color(.56f,.7f,.4f),3);
                ground.Cover(new[]{"BonesA","BonesB"},5,p=>{float d=float.PositiveInfinity;foreach(var h in FieldHides.Concat(WhiteHides))d=Mathf.Min(d,Vector2.Distance(p,h));return Smooth(7,3,d);},.12f,.24f,new Color(.9f,.88f,.82f),2);
                ground.Rocks(4,p=>Dig(p)?.25f+.6f*Smooth(2,.4f,ground.Edge(p)):.4f*Smooth(1.4f,.4f,ground.Edge(p))+.3f*Vent(p),.12f,.4f,new Color(.84f,.9f,1));
                ground.Glows(2,p=>Dig(p)?Mathf.Max(Smooth(8,2,Vector2.Distance(p,new Vector2(40,196))),InArena(p,-1)?.5f:0):0,.05f,.12f,new Color(1,.7f,.4f,.8f));
                ground.MovingShade("cloud",26,new Vector2(.2f,.06f),.34f,.5f,.04f);
                HostBeneath(DigSnow,Scoured);EmitBeneath();
                ground.Finish();
            }

            /// <summary>LookDev's views: the Reach as it is walked, then the dig level by level (each point at its floor's height) and the bottom gallery, and two wide views.</summary>
            internal static LookDev.View[] Views=>new[]
            {
                new LookDev.View("arrival",new Vector3(0,0,3)),new LookDev.View("spear-field",new Vector3(2,0,30)),new LookDev.View("bridge",new Vector3(2,0,47)),
                new LookDev.View("west-pocket",new Vector3(-36,0,60)),new LookDev.View("herd",new Vector3(3,0,63)),new LookDev.View("gap",new Vector3(4,0,80)),
                new LookDev.View("whiteout-lesson",new Vector3(0,0,101)),new LookDev.View("vent-pocket",new Vector3(-29,0,103)),new LookDev.View("stalkers",new Vector3(6,0,118)),
                new LookDev.View("camp",new Vector3(16,0,152)),new LookDev.View("mouth",new Vector3(24,-2,170)),new LookDev.View("upper-gallery",new Vector3(30,L1,184)),
                new LookDev.View("amber-rank",new Vector3(42,L1,188)),new LookDev.View("rig-one",new Vector3(58,L1,189)),new LookDev.View("second-level",new Vector3(90,L2,184)),
                new LookDev.View("left-gallery",new Vector3(90,L2,205)),new LookDev.View("cut",new Vector3(122,L2,182)),new LookDev.View("third-level",new Vector3(126,L3,219)),
                new LookDev.View("rig-three",new Vector3(144,L3,226)),new LookDev.View("winch",new Vector3(174,Bottom,220)),new LookDev.View("door",new Vector3(184,Bottom,216)),
                new LookDev.View("arena",new Vector3(206,Bottom,208)),new LookDev.View("arena-north",new Vector3(208,Bottom,222)),new LookDev.View("couplings",new Vector3(214,Bottom,214)),
                new LookDev.View("warmed-ranks",new Vector3(88,L2,188)),new LookDev.View("ante-ranks",new Vector3(181,Bottom,210)),new LookDev.View("marshal-post",new Vector3(208,Bottom,223)),
                new LookDev.View("vent-two",new Vector3(32,0,116)),new LookDev.View("herd-track",new Vector3(-3,0,74)),
                new LookDev.View("overview-reach",new Vector3(0,0,80),false,4.2f),new LookDev.View("overview-dig",new Vector3(110,-8,205),false,4.6f),
                // Where the whole-game capture's stills stood (nov20-whole s35 097: the first whiteout in vent pocket one; s35 298: the left gallery; s36 062: the Marshal's fight).
                new LookDev.View("pocket-s35-097",new Vector3(-25.6f,0,104)),new LookDev.View("gallery-s35-298",new Vector3(91,L2,205.8f)),
                new LookDev.View("arena-s36-062",new Vector3(212.5f,Bottom,203.3f)),
            };
        }
    }
}
