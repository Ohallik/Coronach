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
    /// The Skyfields (wild; rule: weight and light follow the fibres). Map brief, coordinates and contracts: docs/maps/OrielSkyfields.md. Islands of pale gold stone over a sea of
    /// cloud, each a row across the map at its own height, climbing north as a switchback: the camp, the garden terrace, the fibre fields, the sagging gardens, the stair gardens, the
    /// forecourt of the Glass Choir, and the highest island off to the west. A lift node's road carries the party up a taut fibre (<see cref="FibreLift"/>); a junction's island is
    /// seated or cut across a gap (<see cref="FibreJunction"/>). Everything of the zone's own is in <see cref="Skyfields"/>, so nothing collides with the other Oriel files' members.
    /// <c>-skyfields-greybox</c> leaves out dressing and the ground kit (the blockout); <c>-skyfields-open</c> builds it with every junction seated (for stills of the crossed route).
    /// </summary>
    public static partial class OrielBuilder
    {
        static void BuildSkyfields()=>Skyfields.Build();
        /// <summary>Pale gold stone in a high sun: floor signs take the lighter plate. The camp's pocket keeps the zone's track: the chapter is the quiet one.</summary>
        static void SkyfieldsDefinition(ZoneDef zone){zone.brightFloor=true;}
        /// <summary>Oriel's long afternoon over the cloud: a high white-gold sun, a cool blue sky fill so the stone's shade reads as air and not as mud, a pale haze that takes the far
        /// islands and the cloud sea back, and enough bloom that a fibre reads as light.</summary>
        [ZoneLook.Source] static void SkyfieldsLooks()
        {
            ZoneLook.Register(SkyfieldsZone,new ZoneLook.Spec{sun=new Color(1f,.94f,.82f),sunIntensity=1.3f,sunPitch=46,shadowStrength=.72f,
                sky=new Color(.58f,.66f,.78f),equator=new Color(.56f,.56f,.54f),ground=new Color(.34f,.32f,.28f),
                fog=true,fogColor=new Color(.78f,.8f,.84f),fogStart=55,fogEnd=230,
                contrast=10,saturation=4,splitShadows=new Color(.44f,.5f,.62f),splitHighlights=new Color(.6f,.55f,.44f),
                bloomThreshold=.86f,bloomIntensity=.7f,vignette=.18f});
        }

        internal static class Skyfields
        {
            // ---------------------------------------------------------------- the plan
            // x runs east and z north, so north is the back of every view (the fixed camera looks north-north-east from the south-south-west, about twelve metres north of the party
            // and five south). Every island is a row across the map with the void and the next island's face at its back; the walked line keeps to each row's north half. The rows
            // climb north in a switchback: walk the camp east, ride up, walk the terrace west, cross, walk the fibre fields east, ride up, walk the sagging gardens west, cross, walk the
            // stair gardens east, cross, and the Choir's door is ahead. The south of each row is the camera's foreground and stays low.
            internal const float Camp=0,Terrace=6,Upper=12,High=18,CloudLevel=-24,Abyss=-30,RimTall=.55f,RimWide=.8f,Corner=3.5f;
            /// <summary>The islands: a rounded box each, with a wandering shore, and its floor's height.</summary>
            internal static readonly (string name,float x0,float x1,float z0,float z1,float level)[] Isles=
            {
                ("The camp",-48,32,4,22,Camp),("The garden terrace",-58,36,35,51,Terrace),("The fibre fields",-42,38,63,79,Terrace),
                ("The sagging gardens",-48,36,93,109,Upper),("The stair gardens",-56,30,121,137,Upper),("The forecourt",-18,30,149,169,Upper),("The highest island",-72,-44,149,163,High),
            };
            /// <summary>The lifts: a node on each island, the two places the road leaves the rims, and the levels. The road is solid between the rims and the ride runs node to node.</summary>
            internal static readonly (string name,Vector2 a,Vector2 rimA,Vector2 rimB,Vector2 b,float from,float to)[] Lifts=
            {
                ("First lift",new(26,17.5f),new(26,22),new(26,35),new(26,39.5f),Camp,Terrace),
                ("Second lift",new(32,74.5f),new(32,79),new(32,93),new(32,97.5f),Terrace,Upper),
                ("Lift to the highest island",new(-40,133),new(-42.98f,137),new(-51.93f,149),new(-56,154.5f),Upper,High),
            };
            /// <summary>The junctions: the gap's x, the rim the party comes from (south) and the one it crosses to (north), the level, and what rides on the island.</summary>
            internal enum Load{Nothing,Garden,Chest}
            internal static readonly (string name,float x,float rimA,float rimB,float level,Load load)[] Junctions=
            {
                ("First junction",-30,51,63,Terrace,Load.Nothing),("Second junction",-38,109,121,Upper,Load.Garden),("Third junction",20,137,149,Upper,Load.Chest),
            };
            internal const float DeckWide=6,RoadWide=3,LiftHalf=2.1f,DockHalf=3.4f;
            /// <summary>Plan points a player stands at, named once: the scatter's keep-off, the navigation's must-reach list, LookDev's views, the map brief and a route all want them.</summary>
            internal static readonly Vector2 Arrival=new(-36,13),CampSpawn=new(-20,12),ChoirSpawn=new(10,166.6f),Pad=new(-44,10),Cradle=new(-25,8.6f),Bench=new(-22,17.6f),
                IshenPost=new(-9,15.6f),Crate=new(-6.4f,17.6f),WallFrom=new(-17,20.2f),WallTo=new(4,20.2f),GardenerHome=new(-2,19.2f),LumeAt=new(9,15.6f),Stall=new(6,18.2f),Lantern=new(13.5f,18.6f),
                LongFrom=new(-12,159.5f),LongTo=new(20,10),
                WalledGarden=new(-14,47.4f),GroveTap=new(-38,45.6f),GroveTree=new(-38.6f,48.2f),GroveCache=new(-55,47.6f),
                StairAt=new(18,76.6f),StairLook=new(18,73.4f),Sweeper=new(16.4f,75.6f),
                GardenerWait=new(-10.6f,106.4f),SealedDoor=new(-45,128),SealedHand=new(-43,128),SealedWait=new(-42.6f,130.4f),
                LanternSeat=new(-64,159),SeatHand=new(-62.4f,157.2f),HighCache=new(-68,153),DoorAt=new(4,169),GardenerStep=new(-2,166.6f);
            /// <summary>The frayed garden junctions of the Empty Gardens: where the hand stands inside the rim, and where the sagging garden hangs off it beyond.</summary>
            internal static readonly (Vector2 hand,Vector2 garden,string flag)[] GardenJunctions={(new(-14,106.4f),new(-14,113.6f),"oriel.garden1"),(new(-24,134.4f),new(-24,141.6f),"oriel.garden2"),(new(6,134.4f),new(6,141.6f),"oriel.garden3")};
            /// <summary>The fibres the Fibre Wisps ride, laid across each wisp fight's floor from rim to rim, .45 m over the stone: (fight, from, to).</summary>
            internal static readonly (string fight,Vector2 from,Vector2 to)[] FloorFibres=
            {
                ("Sky_Wisps",new(-7,63.8f),new(-7,78.2f)),("Sky_Wisps",new(0,63.8f),new(0,78.2f)),("Sky_Wisps",new(7,63.8f),new(7,78.2f)),
                ("Sky_Mixed",new(-4,93.8f),new(2,108.2f)),("Sky_Mixed",new(8,93.8f),new(14,108.2f)),
                ("Sky_Door",new(-2,149.8f),new(-2,161)),("Sky_Door",new(10,149.8f),new(10,161)),
            };
            static float RoundBox(Vector2 p,float x0,float x1,float z0,float z1,float round)
            {
                var d=new Vector2(Mathf.Abs(p.x-(x0+x1)*.5f)-(x1-x0)*.5f+round,Mathf.Abs(p.y-(z0+z1)*.5f)-(z1-z0)*.5f+round);
                return Mathf.Min(Mathf.Max(d.x,d.y),0)+new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude-round;
            }
            /// <summary>Where a rim opens for a road or a junction's deck: (the opening's middle on the rim, its half width along the rim). The shore there is the island's box,
            /// not its wandering line, so a road and a deck meet the floor where the builder put them.</summary>
            static IEnumerable<(Vector2 at,float half)> Openings()
            {
                foreach(var l in Lifts){float slant=Mathf.Max(.6f,Mathf.Abs((l.rimB-l.rimA).normalized.y));yield return (l.rimA,LiftHalf/slant);yield return (l.rimB,LiftHalf/slant);}
                foreach(var j in Junctions){yield return (new Vector2(j.x,j.rimA),DockHalf);yield return (new Vector2(j.x,j.rimB),DockHalf);}
            }
            static readonly (Vector2 at,float half)[] Open=Openings().ToArray();
            /// <summary>1 inside an opening of the rim, 0 away from them; the shore's noise fades in over a few metres beyond.</summary>
            static float Opening(Vector2 p,float margin=0)
            {
                float open=0;foreach(var (at,half) in Open)if(Mathf.Abs(p.y-at.y)<2.4f+margin)open=Mathf.Max(open,1-ZoneKit.Smooth(half+margin,half+margin+.3f,Mathf.Abs(p.x-at.x)));
                return open;
            }
            /// <summary>Metres outside island i's shore (negative on it).</summary>
            internal static float Shore(Vector2 p,int i)
            {
                var (_,x0,x1,z0,z1,_)=Isles[i];
                float noise=(Mathf.PerlinNoise(p.x*.07f+i*13.1f,p.y*.07f+i*5.3f)-.5f)*3.2f+(Mathf.PerlinNoise(p.x*.21f+i*3,p.y*.21f+7)-.5f)*1.1f;
                return RoundBox(p,x0,x1,z0,z1,Corner)+noise*(1-Opening(p,4));
            }
            /// <summary>The nearest island: metres outside its shore, and its level.</summary>
            internal static float Nearest(Vector2 p,out float level,out int isle)
            {
                float best=float.PositiveInfinity;level=Abyss;isle=-1;
                for(int i=0;i<Isles.Length;i++){float d=Shore(p,i);if(d<best){best=d;level=Isles[i].level;isle=i;}}
                return best;
            }
            internal static float Height(Vector2 p)=>Ground(p,true);
            /// <summary>The ground as it is drawn: without the rims, which are carved kerbs laid along the same lines (a kerb half a metre high and two cells wide is a saw of
            /// triangles in a heightfield). Bodies and the bake meet the rim in the collider.</summary>
            static float Drawn(Vector2 p)=>Ground(p,false);
            static float Ground(Vector2 p,bool rimmed)
            {
                float d=Nearest(p,out float level,out _);
                float relief=.05f*(Mathf.PerlinNoise(p.x*.13f+7,p.y*.13f+2)-.5f)+.02f*(Mathf.PerlinNoise(p.x*.6f+3,p.y*.6f+1)-.5f);
                if(d<=0)return level+relief+(rimmed?RimTall*ZoneKit.Smooth(-RimWide,-RimWide+.3f,d)*(1-Opening(p)):0);
                // The island's face: straight down into the cloud, rough, and nobody's floor below it.
                float rough=1.2f*Mathf.PerlinNoise(p.x*.19f+3,p.y*.19f+11);
                return Mathf.Lerp(level+relief,Abyss,ZoneKit.Smooth(0,2.6f+rough,d));
            }
            internal static bool OnAnIsland(Vector2 p,float inside=RimWide+.4f)=>Nearest(p,out _,out _)<-inside;
            /// <summary>The floor just inside island i's rim at x, on its north or south side: where a collar stands so its fibre goes out over the coping.</summary>
            static bool RimFoot(int i,float x,bool north,out Vector2 at)
            {
                var (_,_,_,z0,z1,_)=Isles[i];float mid=(z0+z1)*.5f,step=north?.1f:-.1f;at=new Vector2(x,mid);
                for(float z=mid;north?z<z1+5:z>z0-5;z+=step){var p=new Vector2(x,z);if(Shore(p,i)>-1.15f){at=new Vector2(x,z-step);return Shore(at,i)<-.9f;}}
                return false;
            }

            /// <summary>The stone's own colour before the ground kit lays anything on it: pale gold flags, a cooler grey toward each rim where the old paving is broken, warmer and
            /// paler where feet and rakes have worn it, darker under the gardens' walls.</summary>
            static Color Tone(Vector2 p)
            {
                float d=Nearest(p,out _,out int isle);float mottle=.9f+.2f*Mathf.PerlinNoise(p.x*.17f+3,p.y*.17f+7),patch=Mathf.PerlinNoise(p.x*.05f+11,p.y*.05f+5);
                var tone=Color.Lerp(new Color(.82f,.74f,.58f),new Color(.74f,.72f,.62f),ZoneKit.Smooth(.35f,.7f,patch));
                tone=Color.Lerp(tone,new Color(.64f,.64f,.6f),ZoneKit.Smooth(-5,-1,d)*.6f);
                if(d>.5f)tone=new Color(.6f,.56f,.5f);
                // Each island its own: the camp warm and dusty, the gardens pale, the fibre fields cool, the forecourt the court's richest cream, the highest island weathered.
                var own=isle switch{0=>new Color(1,.96f,.9f),1=>new Color(1.04f,1.03f,1),3=>new Color(1.04f,1.03f,1),4=>new Color(1.05f,1.03f,.98f),2=>new Color(.93f,.98f,1.02f),5=>new Color(1.08f,1.02f,.9f),_=>new Color(.95f,.94f,.9f)};
                return new Color(tone.r*mottle*own.r,tone.g*mottle*own.g,tone.b*mottle*own.b,1);
            }

            // ---------------------------------------------------------------- what it is made of
            internal static Material Stone,Cliff,Kerb,Bronze,Fibre,FibreSoft,Road,CloudSea,Glass,GlassDeep,Soil,Plank,Cloth,Lamp,Jar,Frayed,FrayedSoft,FibreLight,Blade,CloudShade,ChoirGlass,ChoirRib,Waves,InnerLight;
            static readonly Color StonePale=new(1,.96f,.86f),StoneShade=new(.74f,.68f,.58f),BronzeDark=new(.62f,.58f,.5f),BronzePale=new(1,.96f,.88f);
            // The fibres' own colour, the one the Choir's fibres speak (its held fibres pale steel, its slack ones amber): taut, a blue-white too bright to look at with a blue
            // halo; fraying, an amber that gutters. Against the gold stone and the white cloud a fibre is the one cold, lit thing in the frame.
            internal static readonly Color TautCore=new(.9f,1.45f,3.3f,1),TautLit=new(3.2f,4.2f,6.6f,1),TautBase=new(.5f,.7f,1),TautHalo=new(.24f,.48f,1,.55f);
            internal static readonly Color FrayBase=new(1,.6f,.26f),FrayHalo=new(1,.55f,.2f,.34f),ThrownBlue=new(.28f,.46f,1,.6f),ThrownAmber=new(1,.55f,.2f,.5f);
            static void Materials()
            {
                // Flagstones: big irregular flags of pale gold limestone, each its own tone, with a dark joint and a fine grain; now and then a flag carved with the court's ring.
                var flags=ZoneKit.Texture("Oriel/skyfields-stone",512,(u,v)=>
                {
                    var f=CourtFlag(u,v);int flag=f.flag,course=f.course;float up=f.up,along=f.along,own=f.own,grain=ZoneKit.Cloud(u,v,16,3),fleck=ZoneKit.Tile(u*70,v*70,70,21),crack=f.crack;
                    bool joint=up<.035f||along<.028f;float tone=.7f+.12f*(own-.5f)+.1f*(grain-.5f)-(fleck>.93f?.06f:0)-(crack<.008f?.08f:0);
                    if(Mathf.Repeat(flag*7+course*3,11)<1){var c=new Vector2(along-.5f,(up-.5f)*.8f);if(Mathf.Abs(c.magnitude-.2f)<.02f)tone-=.1f;}
                    if(joint)tone=.42f+.05f*own;
                    return new Color(tone*1.04f,tone,tone*.9f,1);
                });
                // The islands' faces: bands of gold and grey sandstone along the height, cracks down them, darker in the bands' undercuts.
                var face=ZoneKit.Texture("Oriel/skyfields-cliff",512,(u,v)=>
                {
                    // Bedded sandstone: bands of gold and grey along the height that wander and pinch out, cracks running down them at a slant, pitted, undercut under each bed.
                    float warp=ZoneKit.Cloud(u,v,4,5);float row=v*5+warp*1.3f;int course=Mathf.FloorToInt(row);float up=row-course;
                    float bed=ZoneKit.Tile(course*1.7f,.5f,5,19),grain=ZoneKit.Cloud(u,v,20,9),blot=ZoneKit.Cloud(u*2,v,6,31),pit=ZoneKit.Tile(u*90,v*90,90,17);
                    float crack=Mathf.Abs(ZoneKit.Cloud(u*.7f+v*.15f,v*2.4f,5,23)-.5f);
                    float tone=.62f+.12f*(bed-.5f)+.12f*(grain-.5f)+.1f*(blot-.5f)-(pit>.9f?.08f:0)-.14f*Mathf.Clamp01(1-up*6)-(crack<.012f?.14f:0);
                    var c=Color.Lerp(new Color(tone*1.08f,tone*.96f,tone*.78f),new Color(tone*.9f,tone*.92f,tone*.95f),ZoneKit.Smooth(.35f,.7f,bed));
                    if(pit>.985f)c=new Color(.8f,.92f,1);
                    return c;
                });
                // Bronze gone green, as the court's bronze is everywhere (the waystation's and the relays' make).
                var bronze=ZoneKit.Texture("Oriel/skyfields-bronze",256,(u,v)=>
                {
                    float streak=ZoneKit.Cloud(u*4,v*.6f,4,9),pit=ZoneKit.Tile(u*48,v*48,48,3),blot=ZoneKit.Cloud(u,v,6,2);
                    float green=ZoneKit.Smooth(.45f,.7f,streak*.7f+blot*.3f);float tone=.62f+.2f*(blot-.5f)-(pit>.88f?.12f:0);
                    return Color.Lerp(new Color(tone*1.1f,tone*.88f,tone*.56f),new Color(tone*.66f,tone*.98f,tone*.86f),green*.8f);
                });
                // The cloud: soft billows, brighter on their tops, so a sheet of it reads as cloud from above and not as a floor.
                var cloud=ZoneKit.Texture("Oriel/skyfields-cloud",512,(u,v)=>
                {
                    float big=ZoneKit.Cloud(u,v,3,41),mid=ZoneKit.Cloud(u,v,8,43),small=ZoneKit.Cloud(u,v,20,47);float billow=big*.55f+mid*.3f+small*.15f;
                    float tone=.78f+.3f*ZoneKit.Smooth(.38f,.72f,billow)-.12f*ZoneKit.Smooth(.5f,.3f,billow);
                    return new Color(tone,tone*.99f,tone*1.02f,1);
                });
                // A raked bed of bare grit: fine, pale, combed into lines by a thousand years of rakes.
                var grit=ZoneKit.Texture("Oriel/skyfields-grit",256,(u,v)=>
                {
                    float comb=Mathf.Abs(Mathf.Sin((v*24+ZoneKit.Cloud(u,v,4,5)*1.5f)*Mathf.PI));float g=ZoneKit.Tile(u*80,v*80,80,7);
                    float tone=.66f+.1f*comb+.08f*(g-.5f);return new Color(tone*1.02f,tone,tone*.92f,1);
                });
                Stone=ZoneKit.Toon("skyfields-floor",flags,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_BaseColor",Color.white);m.SetFloat("_BaseMapStrength",.85f);m.SetColor("_ShadowTint",new Color(.56f,.6f,.74f));m.SetFloat("_LightFloor",.2f);});
                Cliff=ZoneKit.Toon("skyfields-cliff",face,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.96f,.86f,.68f));m.SetColor("_ShadowTint",new Color(.5f,.56f,.72f));m.SetFloat("_LightFloor",.22f);});
                Kerb=ZoneKit.Toon("skyfields-kerb",flags,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(1,.94f,.8f));m.SetColor("_ShadowTint",new Color(.56f,.6f,.74f));m.SetFloat("_LightFloor",.26f);});
                Bronze=ZoneKit.Toon("skyfields-bronze",bronze,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.92f,.84f,.64f));m.SetColor("_ShadowTint",new Color(.48f,.52f,.64f));m.SetColor("_RimColor",new Color(.6f,.52f,.36f));m.SetFloat("_LightFloor",.3f);});
                // A taut fibre is light: unlit, bright enough to bloom, its core blue-white and its halo blue.
                Fibre=ZoneKit.Toon("skyfields-fibre",null,m=>{ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetColor("_BaseColor",TautBase);m.SetVector("_GlowParams",new Vector4(1.5f,.15f,0,0));m.SetColor("_EmissionColor",TautCore);});
                FibreSoft=ZoneKit.Toon("skyfields-fibre-soft",null,m=>{ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetColor("_BaseColor",TautHalo);m.SetVector("_GlowParams",new Vector4(1.5f,.3f,0,0));Added(m,-12);});
                // A fraying fibre: amber, its light broken along it (each length its own brightness, in its vertices), and guttering in play (FibreGlow).
                Frayed=ZoneKit.Toon("skyfields-fibre-frayed",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetColor("_BaseColor",FrayBase);m.SetVector("_GlowParams",new Vector4(2.2f,.15f,0,0));m.SetColor("_EmissionColor",Color.black);});
                FrayedSoft=ZoneKit.Toon("skyfields-fibre-frayed-soft",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetColor("_BaseColor",FrayHalo);m.SetVector("_GlowParams",new Vector4(1.4f,.3f,0,0));Added(m,-12);});
                // The light a fibre throws on the stone and the walls near it: added to what it lies on, soft at its edge (its vertices carry colour and how strong), drawn with the
                // ground's details and before any danger marking.
                FibreLight=ZoneKit.Toon("skyfields-fibre-light",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");ZoneKit.TwoSided(m);m.SetColor("_BaseColor",Color.white);m.SetVector("_GlowParams",new Vector4(1,.2f,0,0));Added(m,GroundKit.DetailQueue+1-(int)UnityEngine.Rendering.RenderQueue.Transparent);});
                // Grass that leans toward a fibre, and the shade an island lays on the cloud under it.
                Blade=ZoneKit.Toon("skyfields-blade",null,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetFloat("_LightFloor",.36f);m.SetColor("_ShadowTint",new Color(.56f,.6f,.7f));});
                CloudShade=ZoneKit.Toon("skyfields-cloud-shade",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetColor("_BaseColor",new Color(.42f,.48f,.64f));m.SetVector("_GlowParams",new Vector4(1,.25f,0,0));
                    m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);
                    m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent-35;});
                // A taut fibre's road: a ribbon of pale light, the cloud seen through it, drawn after the ground's details and before danger markings.
                Road=ZoneKit.Toon("skyfields-road",null,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_BaseColor",new Color(.86f,.94f,1,.34f));m.SetColor("_EmissionColor",new Color(.36f,.38f,.32f));
                    m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);
                    m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent-16;});
                // The cloud sea: the toon shader's water, made opaque and slow, so it moves like weather; its foam is the cloud piling up white against each island's face.
                CloudSea=ZoneKit.WaterLook(ZoneKit.Toon("skyfields-cloud-sea",cloud),new Color(.97f,.95f,.9f,1),new Color(.8f,.84f,.9f,1),new Color(1.1f,1.08f,1.04f,1),22,.06f,0,2.4f);
                CloudSea.SetFloat("_BaseMapStrength",.8f);CloudSea.SetFloat("_ZWrite",1);CloudSea.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.Zero);CloudSea.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.One);
                CloudSea.SetOverrideTag("RenderType","Opaque");CloudSea.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Geometry+10;
                // Glass that reads as glass: its own grade's colour, a bright rim, a sharp glint, a light inside it, and the island seen through it.
                var treeMap=AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoOf("GlassTree"));
                Glass=ZoneKit.Toon("skyfields-glass",treeMap,m=>{ZoneKit.With(m,"_ICE_ON","_Ice");
                    // Clear, not white: a pale aqua body the floor is seen through, deep teal where it is thick, a hard white rim and sharp glints on its facets.
                    m.SetVector("_IceParams",new Vector4(3.2f,1.15f,.75f,1.1f));m.SetColor("_IceDeepColor",new Color(.08f,.26f,.34f));m.SetColor("_IceRimColor",new Color(1.7f,1.65f,1.4f));
                    m.SetColor("_BaseColor",new Color(.62f,.86f,.92f,.42f));m.SetFloat("_BaseMapStrength",.35f);m.SetColor("_RimColor",new Color(1,1,.92f));m.SetFloat("_RimPower",3.4f);
                    m.SetColor("_EmissionColor",new Color(.06f,.14f,.16f));m.SetFloat("_LightFloor",.2f);m.SetFloat("_RampSteps",5);
                    m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",1);
                    m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent-20;});
                var ribMap=AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoOf("GlassRib"));
                GlassDeep=ZoneKit.Toon("skyfields-glass-deep",ribMap,m=>{ZoneKit.With(m,"_ICE_ON","_Ice");m.SetVector("_IceParams",new Vector4(3,1,.8f,.9f));m.SetColor("_IceDeepColor",new Color(.1f,.28f,.38f));
                    m.SetColor("_IceRimColor",new Color(1.6f,1.6f,1.4f));m.SetColor("_BaseColor",new Color(.66f,.86f,.94f,.5f));m.SetFloat("_BaseMapStrength",.4f);m.SetColor("_EmissionColor",new Color(.12f,.2f,.24f));m.SetFloat("_LightFloor",.35f);
                    m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",1);
                    m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent-21;});
                Soil=ZoneKit.Toon("skyfields-grit",grit,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.92f,.88f,.78f));m.SetFloat("_LightFloor",.2f);});
                var plank=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Xylos/xylos-plank.png");
                Plank=ZoneKit.Toon("skyfields-plank",plank,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.9f,.8f,.64f));m.SetFloat("_LightFloor",.26f);});
                Cloth=ZoneKit.Toon("skyfields-cloth",null,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetFloat("_LightFloor",.3f);});
                Lamp=ZoneKit.Toon("skyfields-lamp",null,m=>{m.SetColor("_BaseColor",new Color(1,.86f,.58f));m.SetColor("_EmissionColor",new Color(2.2f,1.5f,.7f));});
                Jar=ZoneKit.Toon("skyfields-jar",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_ICE_ON","_Ice");m.SetVector("_IceParams",new Vector4(2,.6f,.3f,.6f));m.SetColor("_IceRimColor",new Color(1.1f,1.05f,.9f));m.SetFloat("_LightFloor",.3f);});
                // The ground kit's layers of the zone's own (GroundKit reads them from Art/Generated/Ground by name): the gardens' raked grit, combed into long lines with a
                // rake's tines in each; and the fibre fields' crust, pale stone grown through with glass along every crack and in panes where it has spread.
                ZoneKit.Texture("Ground/ground-skyfields-raked",512,(u,v)=>
                {
                    float warp=ZoneKit.Cloud(u,v,3,61)*1.2f+ZoneKit.Cloud(u,v,9,67)*.25f;float row=v*16+warp;float t=row-Mathf.Floor(row);
                    float groove=Mathf.Exp(-Mathf.Pow((t-.5f)/.09f,2)),ridge=Mathf.Exp(-Mathf.Pow((t-.28f)/.1f,2)),tine=Mathf.Abs(Mathf.Sin((v*16*5+warp*5)*Mathf.PI))*.04f;
                    float grain=ZoneKit.Tile(u*140,v*140,140,71),fleck=ZoneKit.Tile(u*60,v*60,60,73);
                    float tone=.8f-.2f*groove+.08f*ridge-tine+.06f*(grain-.5f)-(fleck>.94f?.08f:0)+(fleck<.04f?.06f:0);
                    return new Color(tone*1.02f,tone,tone*.93f,1);
                });
                ZoneKit.Texture("Ground/ground-skyfields-glassfield",512,(u,v)=>
                {
                    // The court's own flags, course for course (CourtFlag, laid at 8 m as the floor's texture is, so every joint falls on the floor's joints), grown through with
                    // glass: every joint run full of it, cracks too, and some flags gone wholly to it, faceted.
                    var f=CourtFlag(u,v);float grain=ZoneKit.Tile(u*120,v*120,120,89);bool glassy=ZoneKit.Tile(f.flag*1.3f+f.course*.7f,f.course*2.1f,64,87)>.62f;
                    float tone=.72f+.12f*(f.own-.5f)+.06f*(grain-.5f);var c=new Color(tone*.99f,tone,tone*.98f);
                    if(glassy){float facet=Mathf.Repeat(f.along*1.6f+f.up*.9f+f.own,1);c=Color.Lerp(new Color(.5f,.74f,.84f),new Color(.86f,.97f,1),facet<.5f?facet*2:2-facet*2);}
                    if(f.crack<.012f)c=Color.Lerp(c,new Color(.84f,.97f,1),.85f);
                    if(f.up<.05f||f.along<.04f)c=new Color(.84f,.98f,1.02f);else if(f.up<.08f||f.along<.065f)c*=.84f;
                    if(glassy&&grain>.94f)c=Color.white;
                    return new Color(c.r,c.g,c.b,1);
                });
                // The same flags on the camp, trodden: joints full of dust, flags polished where feet go, chipped at their corners, stained.
                ZoneKit.Texture("Ground/ground-skyfields-worn",512,(u,v)=>
                {
                    var f=CourtFlag(u,v);float grain=ZoneKit.Tile(u*120,v*120,120,93),stain=ZoneKit.Cloud(u,v,5,95);
                    float polish=1-ZoneKit.Smooth(.15f,.5f,new Vector2(f.along-.5f,f.up-.5f).magnitude),corner=Mathf.Min(Mathf.Min(f.along,1-f.along),Mathf.Min(f.up,1-f.up));
                    float tone=.66f+.1f*(f.own-.5f)+.06f*(grain-.5f)+.06f*polish-.08f*ZoneKit.Smooth(.55f,.75f,stain)-(f.crack<.01f?.08f:0);
                    if(corner<.07f&&ZoneKit.Tile(f.flag*3.1f,f.course*1.7f,48,97)>.5f)tone-=.07f;
                    var c=new Color(tone*1.06f,tone,tone*.86f);if(f.up<.04f||f.along<.032f)c=new Color(.66f,.6f,.5f);
                    return new Color(c.r,c.g,c.b,1);
                });
                // The stair gardens' own raking: rows of fans, each three arcs deep, every row set half a fan over from the last.
                var waves=ZoneKit.Texture("Oriel/skyfields-waves",512,(u,v)=>
                {
                    float best=9;int rows=6,cols=4;
                    for(int r=-1;r<=rows;r++)for(int c=-1;c<=cols;c++)
                    {
                        float cx=(c+(r%2==0?0:.5f))/cols,cy=(r+.5f)/rows;var d=new Vector2((u-cx)*cols,(v-cy)*rows*.66f);
                        if(d.y<-.05f)continue;float rad=d.magnitude;if(rad<best)best=rad;
                    }
                    float t=Mathf.Repeat(best*3.4f,1);float groove=Mathf.Exp(-Mathf.Pow((t-.5f)/.12f,2));float grain=ZoneKit.Tile(u*120,v*120,120,91);
                    float tone=.82f-.22f*groove+.05f*(grain-.5f);return new Color(tone*1.02f,tone,tone*.93f,1);
                });
                Waves=ZoneKit.Toon("skyfields-waves",waves,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.96f,.93f,.84f));m.SetFloat("_LightFloor",.22f);m.SetColor("_ShadowTint",new Color(.56f,.6f,.74f));});
                // The Choir's own glass, so the door and what lies beyond it are one body: the dungeon's engineer tunes it; this only draws with it (ours if theirs isn't made yet).
                // The Choir's light from inside it: unlit, its colour in its vertices.
                InnerLight=ZoneKit.Toon("skyfields-inner-light",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");ZoneKit.TwoSided(m);m.SetColor("_BaseColor",Color.white);m.SetVector("_GlowParams",new Vector4(1.25f,.3f,0,0));});
                ChoirGlass=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/choir-glass.mat")??GlassDeep;
                ChoirRib=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/choir-rib.mat")??GlassDeep;
                AssetDatabase.SaveAssets();
            }
            /// <summary>Where a point of an 8 m tile of the court's paving falls: its course (eight to the tile, each a metre deep), its flag in the course (six, each course set over
            /// from the last), how far across and up that flag it is, the flag's own tone, and the crack field. The floor's texture and the ground kit's two layers of the zone's that
            /// are paving (worn, glass-grown) all lay their flags by it, so their joints meet.</summary>
            static (int course,int flag,float up,float along,float own,float crack) CourtFlag(float u,float v)
            {
                float row=v*8;int course=Mathf.FloorToInt(row);float up=row-course;float across=u*6+(course%2)*.5f+ZoneKit.Tile(course*1.3f,.7f,8,9)*.4f;int flag=Mathf.FloorToInt(across);
                return (course,flag,up,across-flag,ZoneKit.Tile(flag*2.7f+course*5.3f,course*1.9f,48,4),Mathf.Abs(ZoneKit.Cloud(u,v,7,33)-.5f));
            }
            /// <summary>Drawn added to what is behind it, writing no depth, at this place in the transparent queue.</summary>
            static void Added(Material m,int fromTransparent)
            {
                m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.One);m.SetFloat("_ZWrite",0);
                m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent+fromTransparent;
            }
            static string AlbedoOf(string id)
            {
                var guid=AssetDatabase.FindAssets(id+" t:Texture2D",new[]{"Assets/_Project/Art/Generated/Models/"+id}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>p.ToLowerInvariant().Contains("albedo"));
                return guid??"";
            }

            // ---------------------------------------------------------------- pieces
            /// <summary>The enemy if the combat engineer has defined it, else a stand-in of the same family (logged), so the zone is walkable and fought before the roster lands.</summary>
            static string Enemy(string id)
            {
                if(ZoneKit.Find<EnemyDef>(id)!=null)return id;
                string stand=id switch{"Glassback"=>"Thornback","FibreWisp"=>"FrostMotes","Pruner"=>"ThawedGuard",_=>id};
                Debug.Log("SKYFIELDS_STAND_IN "+stand+" for "+id);return stand;
            }
            static EncounterVolume Fight(ZoneKit kit,string id,Vector2 at,Vector2 size,string requiredFlag,params (string enemy,int count,Vector2 from,float yaw)[] packs)
            {
                var encounter=WorldBuilder.Encounter(id,kit.On(at.x,at.y,1.5f),new Vector3(size.x,7,size.y),Enemy(packs[0].enemy),packs[0].count);
                var spawners=new List<Spawner>{encounter.spawners[0]};
                for(int i=1;i<packs.Length;i++)spawners.Add(ZoneKit.Spawn(encounter,Enemy(packs[i].enemy),packs[i].count,Vector3.zero,2.4f));
                for(int i=0;i<packs.Length;i++)
                {
                    var spawner=spawners[i];spawner.transform.SetPositionAndRotation(kit.On(packs[i].from.x,packs[i].from.y),Quaternion.Euler(0,packs[i].yaw,0));
                    spawner.faceAsPlaced=packs[i].yaw!=0;spawner.radius=packs[i].count>1?2.6f:0;
                }
                encounter.spawners=spawners.ToArray();encounter.requiredFlag=requiredFlag??"";return encounter;
            }
            /// <summary>A fibre: a bright thread between two points, sagging a little or not at all, with a soft glow round it. Thick enough to read as the thing the zone is about
            /// from the fixed camera (a hand's width of light with its halo).</summary>
            static void Thread(ZoneKit.Shape core,ZoneKit.Shape glow,Vector3 from,Vector3 to,float sag,float radius=.045f)
            {
                int spans=Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(from,to)/2.5f),2,40);var path=new Vector3[spans+1];
                for(int i=0;i<=spans;i++){float t=i/(float)spans;path[i]=Vector3.Lerp(from,to,t)+Vector3.down*sag*4*t*(1-t);}
                float r=Mathf.Max(.05f,radius*1.8f);
                core.Slot(0).Tube(path,path.Select(_=>r).ToArray(),8,Color.white,Color.white,1,.5f,false);
                glow?.Slot(0).Tube(path,path.Select(_=>r*2.8f).ToArray(),8,Color.white,Color.white,1,.5f,false);
            }
            /// <summary>A fraying fibre: amber, in short lengths each lit its own amount, some gone dark, so its light reads as broken along it even in a still (FibreGlow makes it
            /// gutter in play). Drawn in <see cref="Frayed"/> and <see cref="FrayedSoft"/>.</summary>
            static void FrayedThread(ZoneKit.Shape core,ZoneKit.Shape glow,Vector3 from,Vector3 to,float sag,float radius,System.Random rng,float dark=.18f)
            {
                float length=Vector3.Distance(from,to);int spans=Mathf.Clamp(Mathf.CeilToInt(length/.32f),2,90);
                Vector3 At(float t)=>Vector3.Lerp(from,to,t)+Vector3.down*sag*4*t*(1-t);float r=Mathf.Max(.035f,radius*1.6f);
                for(int i=0;i<spans;i++)
                {
                    float t0=i/(float)spans,t1=(i+1)/(float)spans,lit=.3f+.7f*(float)rng.NextDouble();bool gone=rng.NextDouble()<dark;
                    var tone=gone?new Color(.16f,.12f,.1f,1):new Color(lit,lit,lit,1);
                    core.Slot(0).Tube(new[]{At(t0),At(t1)},new[]{r*(gone?.7f:1),r*(gone?.7f:1)},6,tone,tone,1,.5f,false);
                    var halo=new Color(1,1,1,gone?.15f:lit);glow?.Slot(0).Tube(new[]{At(t0),At(t1)},new[]{r*2.6f,r*2.6f},8,halo,halo,1,.5f,false);
                }
            }
            /// <summary>A soft round of light (or of shade), strongest at its middle and gone at its edge.</summary>
            static void Pool(ZoneKit.Shape shape,Vector3 at,Vector3 facing,float radius,Color tone)=>shape.Disc(at,facing,radius,22,tone,0,new Color(tone.r,tone.g,tone.b,0));
            /// <summary>The light a fibre throws on the floor under it: rounds laid along it on whatever island floor lies under each, overlapping into a soft band.</summary>
            static void LightUnder(ZoneKit.Shape shape,Vector3 from,Vector3 to,float radius,Color tone)
            {
                float length=Vector2.Distance(new Vector2(from.x,from.z),new Vector2(to.x,to.z));int steps=Mathf.Max(1,Mathf.CeilToInt(length/(radius*.8f)));
                var each=new Color(tone.r,tone.g,tone.b,tone.a*.42f);
                for(int i=0;i<=steps;i++)
                {
                    var p=Vector3.Lerp(from,to,i/(float)steps);var flat=new Vector2(p.x,p.z);if(Nearest(flat,out _,out _)>-.3f)continue;
                    float floor=Drawn(flat);if(p.y-floor>2.2f)continue;
                    Pool(shape,new Vector3(p.x,floor+.035f,p.z),Vector3.up,radius,each);
                }
            }

            // ---------------------------------------------------------------- the pull, seen where nothing is happening
            /// <summary>What shows the rule round a fibre with nobody near it, gathered over the whole build and drawn once (<see cref="DrawPulls"/>): grit and dust lying in lines
            /// that run in to the fibre's foot like filings round a magnet, grass leaning to it, petals in a trail toward it and stuck to it, water beaded along it, and the light it
            /// throws. Each zone-wide, drawn only: nothing to stand in, nothing to stop a body.</summary>
            sealed class Pulls
            {
                public readonly ZoneKit.Shape grit=new(),blades=new(),petals=new(),beads=new(),light=new();
                public readonly List<(Vector3 foot,float reach)> motes=new();
            }
            static Pulls pulls;
            /// <summary>The pull round one fibre's foot (a collar on the floor at <paramref name="foot"/>; the fibre leaves it at <paramref name="ring"/> going <paramref name="along"/>).</summary>
            static void Pull(Vector3 foot,Vector3 ring,Vector3 along,float reach,int seed,bool frayed=false,bool beads=true,bool light=true)
            {
                if(pulls==null)return;
                var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();var at=new Vector2(foot.x,foot.z);float level=foot.y;along.Normalize();
                bool Floor(Vector2 p,out float y){y=Drawn(p);return Nearest(p,out _,out _)<-(RimWide+.25f)&&Mathf.Abs(y-level)<.5f;}
                Vector3 V(Vector2 q,float up=.014f)=>new(q.x,Drawn(q)+up,q.y);
                // Grit and dust in lines toward the foot: thick close in, thinning out.
                int count=Mathf.RoundToInt(110*reach*reach/9f);
                for(int k=0;k<count;k++)
                {
                    float a=R()*Mathf.PI*2,d=.45f+(reach-.45f)*Mathf.Pow(R(),1.5f);var p=at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*d;if(!Floor(p,out _))continue;
                    var to=(at-p).normalized;var side=new Vector2(-to.y,to.x);float len=(.08f+.2f*R())*(1.3f-.6f*d/reach),w=.011f+.014f*R();
                    var tone=R()<.72f?new Color(.4f,.36f,.31f):new Color(.7f,.64f,.54f);
                    pulls.grit.Triangle(V(p+side*w),V(p-side*w),V(p+to*len),Vector3.up,tone);
                }
                // Longer drifts of fine sand drawn out toward it, and a little heap against the collar on every side it is drawn from.
                for(int k=0;k<7;k++)
                {
                    float a=R()*Mathf.PI*2,d=.8f+(reach-.8f)*R();var p=at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*d;if(!Floor(p,out _))continue;
                    var to=(at-p).normalized;var side=new Vector2(-to.y,to.x);float len=.5f+.6f*R(),w=.06f+.05f*R();var tip=p+to*Mathf.Min(len,d-.35f);
                    pulls.grit.Quad(V(p+side*w*.4f,.012f),V(p-side*w*.4f,.012f),V(tip-side*w,.016f),V(tip+side*w,.016f),Vector3.up,new Color(.66f,.6f,.5f));
                }
                for(int k=0;k<5;k++){float a=R()*Mathf.PI*2;var p=at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.42f;if(Floor(p,out float y))pulls.grit.Lump(new Vector3(p.x,y,p.y),new Vector3(.22f,.06f,.16f),seed+k,.3f,new Color(.96f,.9f,.76f),new Color(.7f,.64f,.54f),8,4);}
                // Grass leaning to it, every blade.
                int tufts=2+rng.Next(3);
                for(int k=0;k<tufts;k++)
                {
                    float a=R()*Mathf.PI*2,d=.9f+R()*reach*.55f;var p=at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*d;if(!Floor(p,out float y))continue;
                    var to=(at-p).normalized;var root=new Vector3(p.x,y-.01f,p.y);
                    for(int b=0;b<8;b++)
                    {
                        var dir=Quaternion.Euler(0,(R()-.5f)*44,0)*new Vector3(to.x,0,to.y);float len=.2f+.2f*R();
                        pulls.blades.Frond(root+new Vector3((R()-.5f)*.14f,0,(R()-.5f)*.14f),dir,len,.034f+.016f*R(),len*.45f,len*.08f,new Color(.58f,.58f,.34f),new Color(.96f,.92f,.6f),3);
                    }
                }
                // Petals and dry leaves in a trail toward it, and the ones it has taken stuck flat to the thread where it leaves the collar.
                float a0=R()*Mathf.PI*2;var start=at+new Vector2(Mathf.Cos(a0),Mathf.Sin(a0))*(reach*.85f);
                for(int k=0;k<10;k++)
                {
                    float t=k/10f;var p=Vector2.Lerp(start,at,t*.9f)+new Vector2(R()-.5f,R()-.5f)*.3f*(1-t);if(!Floor(p,out float y))continue;
                    var tone=R()<.5f?new Color(1,.93f,.95f):new Color(.84f,.64f,.3f);
                    pulls.petals.Disc(new Vector3(p.x,y+.02f,p.y),(Vector3.up+new Vector3(R()-.5f,0,R()-.5f)*.5f).normalized,.04f+.03f*R(),6,tone);
                }
                for(int k=0;k<3;k++)
                {
                    var q=ring+along*(.3f+k*.32f+R()*.1f)+Vector3.down*.04f;var tone=k%2==0?new Color(1,.93f,.95f):new Color(.84f,.64f,.3f);
                    pulls.petals.Disc(q,(Vector3.Cross(along,Vector3.up)+Vector3.up*(R()-.5f)).normalized,.045f+.02f*R(),6,tone);
                }
                // Water beaded along it, the beads growing as they run out along the thread.
                if(beads)for(float s=.2f;s<1.9f;s+=.15f+.08f*R()){float r=.02f+.028f*(s/1.9f)+.01f*R();var q=ring+along*s+Vector3.down*(.07f+r*.6f);pulls.beads.Lump(q,new Vector3(r,r*1.2f,r),seed+(int)(s*40),.05f,Color.white,new Color(.8f,.9f,1),6,4);}
                // The light it throws on the stone round its foot.
                var tint=frayed?ThrownAmber:ThrownBlue;
                if(light)
                {
                    Pool(pulls.light,new Vector3(foot.x,Drawn(at)+.04f,foot.z),Vector3.up,1.5f,new Color(tint.r,tint.g,tint.b,tint.a*.7f));
                    Pool(pulls.light,new Vector3(foot.x,Drawn(at)+.045f,foot.z),Vector3.up,.7f,new Color(tint.r,tint.g,tint.b,tint.a*.8f));
                }
                pulls.motes.Add((foot,reach));
            }
            /// <summary>Where a fibre is parting, at its collar: out of the ring as one, then its strands splay, most snapped short and sparking, two still running on to where they
            /// close up again a little over a metre out. Returns that point, for the rest of the fibre to run on from.</summary>
            static Vector3 Parting(ZoneKit.Shape shape,Vector3 ring,Vector3 toward,System.Random rng,float severity=1)
            {
                float P()=>(float)rng.NextDouble();var dir=(toward-ring).normalized;var side=Vector3.Cross(dir,Vector3.up);side=side.sqrMagnitude<1e-4f?Vector3.right:side.normalized;var up=Vector3.Cross(side,dir);
                var join=ring+dir*1.25f+Vector3.down*.12f;
                shape.Slot(0).Tube(new[]{ring-dir*.05f,ring+dir*.22f},new[]{.075f,.07f},8,Color.white,Color.white,1,.5f,false);
                const int Strands=8;
                for(int s=0;s<Strands;s++)
                {
                    float a=s*Mathf.PI*2/Strands+P()*.4f;var o=side*Mathf.Cos(a)+up*Mathf.Sin(a);var p0=ring+dir*.22f;var p1=ring+dir*.62f+o*(.13f+.06f*P())*severity;
                    if(s==0||s==Strands/2){shape.Slot(0).Tube(new[]{p0,p1,join+o*.03f},new[]{.026f,.026f,.03f},6,Color.white,new Color(.8f,.8f,.8f,1),1,.5f,false);continue;}
                    var end=ring+dir*(.68f+.42f*P())+o*(.22f+.16f*P())*severity+Vector3.down*.12f*P();float lit=.55f+.45f*P();
                    shape.Slot(0).Tube(new[]{p0,p1,end},new[]{.022f,.02f,.012f},5,new Color(lit,lit,lit,1),new Color(lit,lit,lit,1),1,.5f,false);
                    shape.Slot(0).Lump(end,Vector3.one*(.03f+.025f*P()),s,.2f,Color.white,Color.white,6,4);
                }
                return join;
            }
            /// <summary>Everything the pull gathered, drawn: one piece per kind for the whole zone, and in play a few motes drifting in to each fibre's foot near a walk.</summary>
            static void DrawPulls(ZoneKit kit)
            {
                kit.Built("The pull, grit drawn to the fibres",pulls.grit,Cloth);kit.Built("The pull, grass leaning to the fibres",pulls.blades,Blade);
                kit.Built("The pull, petals drawn to the fibres",pulls.petals,Cloth);kit.Built("The pull, water beaded on the fibres",pulls.beads,Jar);
                var light=kit.Built("The light the fibres throw",pulls.light,FibreLight);light.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var glints=kit.Glow("Drawn grit",false,new Color(1.2f,1.15f,1.1f,1));int made=0;
                foreach(var (foot,reach) in pulls.motes)
                {
                    if(!NearWay(new Vector2(foot.x,foot.z),9))continue;
                    var motes=kit.Drift("Grit drawn to a fibre",foot+Vector3.up*.25f,new Vector3(reach*1.7f,.3f,reach*1.7f),glints,new Color(1,.96f,.88f,.7f),36,.025f,.055f,0,2.8f);
                    var velocity=motes.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;
                    velocity.radial=new ParticleSystem.MinMaxCurve(-.9f);velocity.x=new ParticleSystem.MinMaxCurve(0);velocity.y=new ParticleSystem.MinMaxCurve(.12f);velocity.z=new ParticleSystem.MinMaxCurve(0);
                    made++;
                }
                Debug.Log("SKYFIELDS_PULLS motes="+made+" of "+pulls.motes.Count);
            }
            /// <summary>A bronze collar where a fibre is anchored to a rim: a ring on a squat drum, the court's mark on its face.</summary>
            static void Collar(ZoneKit.Shape shape,Vector3 foot,Vector3 toward,float size=1)
            {
                toward.y=0;if(toward.sqrMagnitude<.01f)toward=Vector3.forward;toward.Normalize();
                shape.Slot(0).Post(foot+Vector3.down*.1f,.55f*size,.36f*size,.3f*size,10,BronzeDark,BronzePale);
                var ring=foot+Vector3.up*.75f*size;var side=Vector3.Cross(Vector3.up,toward);
                var path=new Vector3[13];for(int k=0;k<13;k++){float a=k/12f*Mathf.PI*2;path[k]=ring+(side*Mathf.Cos(a)+Vector3.up*Mathf.Sin(a))*.3f*size;}
                shape.Slot(0).Tube(path,path.Select(_=>.07f*size).ToArray(),6,BronzePale,BronzePale,1,.5f,false);
            }

            /// <summary>One crystal of glass: a six-sided prism from its foot, swelling a little and coming to a point.</summary>
            static void Crystal(ZoneKit.Shape shape,Vector3 foot,Vector3 dir,float length,float radius)
                =>shape.Tube(new[]{foot,foot+dir*length*.15f,foot+dir*length*.78f,foot+dir*length},new[]{radius*.8f,radius,radius*.85f,.004f},6,Color.white,Color.white,1,.5f,false);
            /// <summary>A burst of crystals from one point, leaning out round a direction.</summary>
            static void Cluster(ZoneKit.Shape shape,Vector3 at,Vector3 outward,float size,System.Random rng)
            {
                outward.Normalize();var side=Vector3.Cross(outward,Mathf.Abs(outward.y)>.9f?Vector3.right:Vector3.up).normalized;var up=Vector3.Cross(side,outward);
                int count=5+rng.Next(4);
                for(int k=0;k<count;k++)
                {
                    float a=(float)rng.NextDouble()*Mathf.PI*2,spread=.25f+.55f*(float)rng.NextDouble();var dir=(outward+(side*Mathf.Cos(a)+up*Mathf.Sin(a))*spread).normalized;
                    float length=size*(k==0?1:.4f+.5f*(float)rng.NextDouble());Crystal(shape,at-dir*length*.12f,dir,length,length*.13f);
                }
            }

            // ---------------------------------------------------------------- the zone
            internal static void Build()
            {
                Materials();pulls=new Pulls();
                var kit=ZoneKit.Ground(SkyfieldsZone,Height,30);bool greybox=DevArgs.Has("-skyfields-greybox"),open=DevArgs.Has("-skyfields-open");
                var scene=WorldBuilder.Begin(kit.zone);
                var land=kit.Terrain("Skyfields",-84,56,-10,186,.5f,Stone,Cliff,Tone,new Color(.9f,.86f,.8f),new Color(1,.97f,.9f),Drawn);
                // The heightfield can only draw an island as a mesa standing on the abyss: its faces go straight down to the floor of the void. They are not drawn; each island's
                // keel is (Keels), a tapering underside of rock that hangs in the air, so an island reads as floating on its fibres and not as a terrace of one hill.
                land.walls.enabled=false;
                // The cloud sea under everything: the islands stand out of it, and nobody stands on it.
                CloudSheet(kit);

                // ---- ways in and out, the camp's services
                WorldBuilder.Spawn("Arrival",kit.On(Arrival.x,Arrival.y));WorldBuilder.Spawn("Camp",kit.On(CampSpawn.x,CampSpawn.y));WorldBuilder.Spawn("ChoirDoor",kit.On(ChoirSpawn.x,ChoirSpawn.y));
                Launch(kit);
                var cradle=kit.Prop("DeckConsole",new Vector3(Cradle.x,0,Cradle.y),1.4f,40).AddComponent<RepairBay>();cradle.free=true;cradle.saveSpawn="Camp";cradle.prompt="Camp cradle — repair and save";
                ChapterOneStaging.CradleBay("Camp cradle",cradle.gameObject,kit.On(Cradle.x,Cradle.y),kit.Y(Cradle.x,Cradle.y)+.02f,false,true);
                var anvil=kit.Prop("LatticeAnvil",new Vector3(Bench.x,0,Bench.y),1.8f,150).AddComponent<Lattice.World.Bench>();anvil.prompt="Fabricate — Lattice Anvil";
                var pocket=new GameObject("Ishen's camp (safe)",typeof(BoxCollider),typeof(SafePocket));pocket.transform.position=new Vector3(-8,3,13);
                var pocketBox=pocket.GetComponent<BoxCollider>();pocketBox.isTrigger=true;pocketBox.size=new Vector3(80,10,20);
                // Landed: the world's own flag for the camp (AbriTooManyBooks waits on it), set by arriving.
                var landed=new GameObject("Landed on Oriel",typeof(FlagAll)).GetComponent<FlagAll>();landed.flags=new[]{"visited."+SkyfieldsZone};landed.result="oriel.landed";
                TheCamp(kit,out var landing);

                // ---- the islands' own: kerbs on every rim, the fibres that hold them, the lifts and the junctions
                Rims(kit);
                Keels(kit);
                foreach(var lift in Lifts)Lift(kit,lift);
                var junctions=Junctions.Select(j=>Junction(kit,j,open)).ToArray();
                LongFibre(kit);
                FloorThreads(kit);

                // ---- the garden terrace: the first Glassback, the walled garden, the junction talk, the glass forest
                kit.Sign("WALLED GARDEN",WalledGarden.x+7.4f,WalledGarden.y-4.6f,1.8f);
                WalledBed(kit,"Walled garden",WalledGarden,12,5,true);
                GardenerAtWork(kit,new Vector3(WalledGarden.x-3,0,WalledGarden.y+.2f),200,new Color(1,.97f,.92f),true,false,.12f);
                GardenerAtWork(kit,new Vector3(WalledGarden.x+3.2f,0,WalledGarden.y+.6f),150,new Color(.94f,.96f,1),false,false,.12f);
                kit.Crossing("Two gardeners in a walled bed",new Vector3(-6,0,43),new Vector3(3,5,16),"oriel.gardenSeen","IdraEmptyGarden","Idra");
                kit.Crossing("The first junction",new Vector3(-22,0,43),new Vector3(3,5,16),"oriel.junctionRead","IshenJunction","Taren");
                Fight(kit,"Sky_Glassback",new Vector2(10,43),new Vector2(26,16),null,("Glassback",1,new Vector2(6,47.5f),160));
                var tap=kit.Discovery("A glass tree",new Vector3(GroveTap.x,0,GroveTap.y),"Tap the glass","oriel.forestHeard","","IdraGlassForest","Taren",null,0,3);tap.situation="";
                Fight(kit,"Sky_Grove",new Vector2(-48,43),new Vector2(18,14),null,("Glassback",1,new Vector2(-51,47),120));
                Stash(kit,"oriel.sky.grove","A gardener's chest",GroveCache,40,140,ProgressionBuilder.AnyPart(4,GearSlot.Edge,1),ProgressionBuilder.Drop("SingingGlass",3,1));

                // ---- the fibre fields: the wisps' floor, the swept stair
                Fight(kit,"Sky_Wisps",new Vector2(0,71),new Vector2(24,16),null,("FibreWisp",1,new Vector2(-6,75),180),("FibreWisp",1,new Vector2(6,67),200));
                SweptStair(kit);
                var stair=kit.Discovery("A stair that ends in air",new Vector3(StairLook.x,0,StairLook.y),"Look at the stair","oriel.stairSeen","","IdraSweptStair","Idra",null,0,3);stair.requiredFlag="oriel.gardenSeen";stair.situation="";

                // ---- the sagging gardens: the follower talk, the mixed fight, the first frayed garden junction
                kit.Crossing("Twelve paces behind",new Vector3(22,0,101),new Vector3(3,5,16),"oriel.followerSeen","IdraGardenerFollows","Idra");
                Fight(kit,"Sky_Mixed",new Vector2(6,101),new Vector2(26,16),null,("Glassback",1,new Vector2(2,105.5f),170),("FibreWisp",2,new Vector2(12,97),0));
                var asks=kit.Discovery("The gardener at the frayed junction",new Vector3(GardenerWait.x+1.2f,0,GardenerWait.y-1.6f),"Look at the gardener","oriel.gardenerAsked","","IdraGardenerWaits","Idra",null,0,3.2f);
                asks.requiredFlag="oriel.followerSeen";asks.situation="";
                var gardens=GardenJunctions.Select((g,i)=>GardenJunction(kit,g.hand,g.garden,g.flag,i)).ToArray();
                var seated=new GameObject("Three gardens seated",typeof(FlagAll)).GetComponent<FlagAll>();seated.flags=GardenJunctions.Select(g=>g.flag).ToArray();seated.result="oriel.gardensSeated";

                // ---- the stair gardens: the far end of the second junction's garden and its keepers, the sealed garden, the way up to the highest island
                SecondGardenKeepers(kit,out var keepers);
                Fight(kit,"Sky_Pruners",new Vector2(-34,129),new Vector2(20,16),"oriel.junction2.cut",("Pruner",1,keepers[0],170),("Pruner",1,keepers[1],190));
                SealedGarden(kit);

                // ---- the highest island: the lantern's old seat, a chest
                HighIsland(kit);

                // ---- the forecourt: the Warden's pruners, the door, the gardener's step
                Fight(kit,"Sky_Door",new Vector2(4,155.5f),new Vector2(24,13),null,("Pruner",2,new Vector2(4,159),180),("FibreWisp",1,new Vector2(4,152),0));
                kit.Crossing("The Choir's doorstep",new Vector3(6,0,164),new Vector3(48,5,3),"oriel.doorReached","IdraChoirDoor","Idra");
                ChoirDoor(kit);

                // ---- the two who walk with the party
                var follower=Gardener(kit);
                var ishen=Ishen(kit);

                if(!greybox)Dress(kit);
                ShadowsOnTheCloud(kit,junctions);
                DrawPulls(kit);

                // ---- limits: the rims stop a walk, the cloud is no floor; boxes far out keep anything from leaving the map.
                WorldBuilder.Boundary("South limit",new Vector3(-14,-10,-8),new Vector3(150,70,2));WorldBuilder.Boundary("North limit",new Vector3(-14,-10,184),new Vector3(150,70,2));
                WorldBuilder.Boundary("West limit",new Vector3(-82,-10,88),new Vector3(2,70,196));WorldBuilder.Boundary("East limit",new Vector3(54,-10,88),new Vector3(2,70,196));

                // ---- navigation, with every junction's deck across and every seal open: what is shut at first is play's business.
                var decks=junctions.Select(j=>j.deck).ToList();var shutNow=junctions.SelectMany(j=>j.shut).ToList();var door=GameObject.Find("Sealed garden door");
                var wereDeck=decks.Select(d=>d.activeSelf).ToArray();var wereShut=shutNow.Select(s=>s.activeSelf).ToArray();bool doorWas=door!=null&&door.activeSelf;
                foreach(var d in decks)d.SetActive(true);foreach(var s in shutNow)s.SetActive(false);if(door!=null)door.SetActive(false);
                var reach=new List<Vector2>{Arrival,CampSpawn,Pad+new Vector2(2.4f,0),Cradle+new Vector2(1.6f,1),Bench+new Vector2(1.6f,-1.2f),IshenPost+new Vector2(0,-1.6f),LumeAt+new Vector2(0,-1.6f),LongTo,
                    new(10,43),WalledGarden+new Vector2(0,-3.6f),GroveTap,GroveCache+new Vector2(1.6f,-1),new(-48,43),new(0,71),StairLook,new(6,101),GardenerWait,new(-34,129),SealedHand,SealedWait,
                    SeatHand,HighCache+new Vector2(1.6f,0),new(4,155),ChoirSpawn,DoorAt+new Vector2(0,-1.6f),GardenerStep,LongFrom};
                foreach(var l in Lifts){reach.Add(l.a);reach.Add(l.b);}
                foreach(var j in Junctions){reach.Add(new Vector2(j.x,j.rimA-2));reach.Add(new Vector2(j.x,j.rimB+2));reach.Add(new Vector2(j.x-4.2f,j.rimA-2));reach.Add(new Vector2(j.x+4.2f,j.rimA-2));}
                foreach(var (hand,_,_) in GardenJunctions)reach.Add(hand);
                kit.BakeNavigation(new Bounds(new Vector3(-14,8,88),new Vector3(150,38,200)),Arrival,reach);
                for(int i=0;i<decks.Count;i++)decks[i].SetActive(wereDeck[i]);for(int i=0;i<shutNow.Count;i++)shutNow[i].SetActive(wereShut[i]);if(door!=null)door.SetActive(doorWas);
                if(!greybox)Ground(kit);
                _=landing;_=gardens;_=follower;_=ishen;
                WorldBuilder.Save(scene,kit.zone);Debug.Log("ORIEL_SKYFIELDS_OK");
            }

            /// <summary>The cloud sea: one unbroken sheet under everything (the water sheet the kit makes is cut away wherever ground stands over it, and under a floating island
            /// that hole showed the night panorama). Its second uv, what the water look reads as the distance from a shore, wanders, so the sheet is billow and shadow, not one tone.</summary>
            static void CloudSheet(ZoneKit kit)
            {
                const float x0=-150,x1=120,z0=-80,z1=300,cell=6;int nx=Mathf.CeilToInt((x1-x0)/cell)+1,nz=Mathf.CeilToInt((z1-z0)/cell)+1;
                var points=new Vector3[nx*nz];var uv=new Vector2[points.Length];var shore=new Vector2[points.Length];var normals=new Vector3[points.Length];var triangles=new List<int>();
                for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
                {
                    int i=z*nx+x;var p=new Vector2(x0+x*cell,z0+z*cell);points[i]=new Vector3(p.x,CloudLevel+.6f*(Mathf.PerlinNoise(p.x*.03f,p.y*.03f)-.5f),p.y);uv[i]=p/8;normals[i]=Vector3.up;
                    shore[i]=new Vector2(0,4+18*Mathf.PerlinNoise(p.x*.022f+5,p.y*.022f+9));
                }
                for(int z=0;z<nz-1;z++)for(int x=0;x<nx-1;x++){int i=z*nx+x;triangles.AddRange(new[]{i,i+nx,i+nx+1,i,i+nx+1,i+1});}
                var mesh=new Mesh{name="Cloud sea",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.vertices=points;mesh.normals=normals;mesh.uv=uv;mesh.uv2=shore;mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
                mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-Cloud-sea.asset");
                var go=new GameObject("Cloud sea",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=CloudSea;view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            /// <summary>How far the shade of something hanging over the cloud is laid along the cloud for each metre it hangs above it, and which way: away from the fixed camera,
            /// about one and a half times the drop. The camera looking down past a rim sees the cloud there (1.2 times the drop on from the rim, and on across the gap), so the shade lies
            /// in the gap's own frame. The sun stands off to the side and would throw it under the next island, where no frame sees it: this is the look's licence, not the sun's.</summary>
            static Vector2 ShadeShift()=>new Vector2(Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad))*1.45f;
            /// <summary>Every island's shade on the cloud under it, soft-edged, so the cloud reads as far below and the islands as hanging over it; a sagging junction's island has its
            /// own, which goes with it if it is cut.</summary>
            static void ShadowsOnTheCloud(ZoneKit kit,FibreJunction[] junctions)
            {
                var shift=ShadeShift();var shade=new ZoneKit.Shape();var tone=new Color(1,1,1,.32f);
                foreach(var (_,x0,x1,z0,z1,level) in Isles)
                {
                    float r=(z1-z0)*.62f;var throw_=shift*(level+3-CloudLevel);
                    for(float x=x0+r*.6f;x<=x1-r*.6f+.01f;x+=r*.7f)Pool(shade,new Vector3(x+throw_.x,CloudLevel+.45f,(z0+z1)*.5f+throw_.y),Vector3.up,r,tone);
                }
                var body=kit.Built("Islands' shade on the cloud",shade,CloudShade);body.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                foreach(var j in junctions)
                {
                    var own=new ZoneKit.Shape();var at=j.sagAt;var throw_=shift*(at.y-CloudLevel);
                    foreach(float dz in new[]{-3.4f,0,3.4f})Pool(own,new Vector3(at.x+throw_.x,CloudLevel+.5f,at.z+dz+throw_.y),Vector3.up,4.2f,new Color(1,1,1,.42f));
                    var drawn=kit.Built(j.name+" shade on the cloud",own,CloudShade);drawn.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    drawn.transform.SetParent(j.transform,true);j.shade=drawn;
                }
            }

            // ---------------------------------------------------------------- the camp
            /// <summary>The landing pad at the camp's west end: a ring of the court's paving with bronze bollards, and the way back up to the Orrery.</summary>
            static void Launch(ZoneKit kit)
            {
                var padAt=kit.On(Pad.x,Pad.y);var pad=new ZoneKit.Shape();
                pad.Slot(0).Disc(padAt+Vector3.up*.05f,Vector3.up,3.2f,28,new Color(.92f,.88f,.78f),0,new Color(.78f,.72f,.6f),.4f).Slot(1).Disc(padAt+Vector3.up*.06f,Vector3.up,2.6f,28,new Color(.9f,.82f,.6f),2.3f);
                for(int k=0;k<6;k++){float a=k*Mathf.PI/3+.3f;var q=padAt+new Vector3(Mathf.Cos(a)*3.5f,0,Mathf.Sin(a)*3.5f);pad.Slot(1).Post(q,.8f,.12f,.09f,8,BronzeDark,BronzePale).Slot(2).Lump(q+Vector3.up*.9f,new Vector3(.12f,.14f,.12f),k,.04f,Color.white,Color.white,7,4);}
                var view=kit.Built("Landing pad",pad.Slot(2),Kerb,Bronze,Lamp);
                var point=new GameObject("Landing pad point",typeof(DockingPad));point.transform.position=padAt;view.transform.SetParent(point.transform,true);
                var dock=point.GetComponent<DockingPad>();dock.prompt="Launch — The Orrery";dock.range=4;dock.scene=OrreryZone;dock.spawn=SkyfieldsZone;
            }
            /// <summary>Ishen's camp: his crates of books by the old wall the camp's fibre runs along, the book that falls to the wall, the gardener standing by it with its rake, Lume's
            /// stall and the chain's last lantern, the long fibre's landing; the talk that opens the chapter on the ground and the call that waits for the party's return.</summary>
            static void TheCamp(ZoneKit kit,out DiscoveryPoint landing)
            {
                kit.Sign("ISHEN'S CAMP",-12,13.2f,2);kit.Sign("LUME'S STALL",Stall.x-2.2f,Stall.y-5.2f,1.6f);kit.Sign("LAST LANTERN",Lantern.x+.8f,Lantern.y-4.8f,1.6f);
                // The old wall: a garden wall of the court's, waist high, along the island's north rim. The camp's fibre runs along its top, and everything near it lies on its face.
                var wall=new ZoneKit.Shape();var a=kit.On(WallFrom.x,WallFrom.y);var b=kit.On(WallTo.x,WallTo.y);
                wall.Slot(0).Beam(a+Vector3.up*.65f,b+Vector3.up*.65f,.6f,1.3f,StoneShade,StonePale).Beam(a+Vector3.up*1.36f,b+Vector3.up*1.36f,.78f,.14f,StonePale,StonePale);
                var wallBody=kit.Built("Old garden wall",wall,Kerb);ZoneKit.BlockAlong(wallBody,a+Vector3.up*.7f,b+Vector3.up*.7f,.7f,1.4f,"Old wall");
                // Ishen, his crates, and the book on the top crate that will fall sideways to the wall.
                var crates=new ZoneKit.Shape();var rng=new System.Random(71);
                foreach(var (x,z,yaw,h) in new[]{(Crate.x,Crate.y,8f,.9f),(Crate.x-1.3f,Crate.y+.3f,-14f,.7f),(Crate.x-2.4f,Crate.y-.6f,20f,.8f),(Crate.x+.9f,Crate.y-1.4f,4f,.6f)})
                {
                    var c=kit.On(x,z,h*.5f);crates.Slot(0).Box(c,new Vector3(1.1f,h,.8f),Quaternion.Euler(0,yaw,0),new Color(.72f,.6f,.44f),new Color(.86f,.74f,.56f));
                    // Books on them, spines out, in the archive's colours.
                    for(int k=0;k<4;k++)crates.Slot(1).Box(c+Vector3.up*(h*.5f+.06f+k*.11f)+Quaternion.Euler(0,yaw,0)*new Vector3(((float)rng.NextDouble()-.5f)*.3f,0,((float)rng.NextDouble()-.5f)*.2f),new Vector3(.36f,.1f,.26f),Quaternion.Euler(0,yaw+((float)rng.NextDouble()-.5f)*30,0),
                        Color.Lerp(new Color(.55f,.2f,.16f),new Color(.2f,.32f,.5f),(float)rng.NextDouble()));
                }
                var crateBody=kit.Built("Ishen's crates",crates,Plank,Cloth);ZoneKit.Block(crateBody,kit.On(Crate.x-1,Crate.y,.45f),new Vector3(3.6f,.9f,1.6f),0,"Crates");
                landing=kit.Crossing("Ishen at the landing",new Vector3(-15,0,13),new Vector3(3,5,18),"oriel.ruleSeen","IshenLanding","Ishen");landing.report="Landing";
                var book=new ZoneKit.Shape();book.Slot(0).Box(Vector3.zero,new Vector3(.34f,.09f,.25f),Quaternion.identity,new Color(.5f,.18f,.14f),new Color(.62f,.24f,.18f)).Box(new Vector3(.01f,0,0),new Vector3(.32f,.07f,.24f),Quaternion.identity,new Color(.96f,.92f,.8f));
                var bookBody=kit.Built("Ishen's book",book,Cloth);
                var top=kit.On(Crate.x,Crate.y,.9f+.05f);var edge=top+new Vector3(.2f,0,.4f);var wallFace=new Vector3(Crate.x+.4f,kit.Y(Crate.x,WallFrom.y)+.95f,WallFrom.y-.36f);
                var falls=bookBody.AddComponent<FallsAlong>();falls.startsWith=landing;falls.fallenFlag="oriel.ruleSeen";falls.path=new[]{top,edge,Vector3.Lerp(edge,wallFace,.5f)+Vector3.up*.08f,wallFace};
                falls.restTurn=new Vector3(0,8,0);falls.landTurn=new Vector3(-90,8,0);bookBody.transform.SetPositionAndRotation(top,Quaternion.Euler(falls.restTurn));
                // What the fibre has already taken: leaves, a cup and pebbles lying flat on the wall's face, not at its foot. Seen before anything is said.
                var taken=new ZoneKit.Shape();var face=WallFrom.y-.33f;var prng=new System.Random(77);
                for(int k=0;k<22;k++)
                {
                    float x=Mathf.Lerp(WallFrom.x+1,WallTo.x-1,(float)prng.NextDouble()),y=kit.Y(x,face)+.2f+(float)prng.NextDouble()*1f;var at=new Vector3(x,y,face-.02f);
                    if(k%3==0)taken.Slot(0).Lump(at+Vector3.back*.04f,new Vector3(.07f,.06f,.04f),k,.3f,new Color(.7f,.66f,.58f),new Color(.5f,.48f,.42f),6,4);
                    else taken.Slot(1).Disc(at,Vector3.back,.07f+.05f*(float)prng.NextDouble(),6,Color.Lerp(new Color(.72f,.6f,.32f),new Color(.56f,.62f,.34f),(float)prng.NextDouble()));
                }
                var cup=new Vector3(-4.2f,kit.Y(-4.2f,face)+.62f,face-.06f);taken.Slot(0).Tube(new[]{cup,cup+Vector3.back*.1f},new[]{.05f,.06f},8,new Color(.86f,.84f,.78f),new Color(.96f,.94f,.88f));
                kit.Built("What the fibre took",taken,Kerb,Cloth);
                // The camp's fibre: along the wall's top, anchored in collars at both ends, and on into the air off the rim either side.
                var core=new ZoneKit.Shape();var glow=new ZoneKit.Shape();var collars=new ZoneKit.Shape();
                var west=kit.On(WallFrom.x-4,WallFrom.y+.8f,.6f);var east=kit.On(WallTo.x+5,WallFrom.y+.8f,.6f);
                Thread(core,glow,west+Vector3.up*.75f,east+Vector3.up*.75f,.15f,.05f);
                Thread(core,glow,west+Vector3.up*.75f,new Vector3(WallFrom.x-22,-6,31),3,.04f);Thread(core,glow,east+Vector3.up*.75f,new Vector3(WallTo.x+16,-4,30),3,.04f);
                Collar(collars,west,Vector3.right);Collar(collars,east,Vector3.left);
                kit.Built("Camp fibre",core,Fibre);kit.Built("Camp fibre glow",glow,FibreSoft);kit.Built("Camp fibre collars",collars,Bronze);
                Pull(west,west+Vector3.up*.75f,Vector3.right,2.4f,301);Pull(east,east+Vector3.up*.75f,Vector3.left,2.4f,302);
                CampWallPull(kit,west+Vector3.up*.75f,east+Vector3.up*.75f);
                foreach(float x in new[]{-12f,-1f})kit.Lamp(new Vector3(x,1.7f,20.6f),6,1.1f,new Color(.55f,.7f,1)).name="Camp fibre's light";
                // The runnel: water that should run down the wall's foot climbs it, and goes away up the thread.
                Climbing(kit,"Water climbing the thread",kit.On(-10.5f,WallFrom.y-.6f,.2f),kit.On(-10.5f,WallFrom.y,2),new Vector3(-8.4f,kit.Y(-8,WallFrom.y)+2.2f,WallFrom.y+1.4f));
                // Lume's stall and the last lantern: the chain's far end.
                LumesStall(kit);
                // The long fibre comes down beside the lantern: its landing is a collar ring on the paving.
                var ringShape=new ZoneKit.Shape();Collar(ringShape,kit.On(LongTo.x+2.4f,LongTo.y+1.6f),Vector3.left,1.2f);kit.Built("Long fibre's collar",ringShape,Bronze);
                var meret=kit.Crossing("Back into the camp",new Vector3(19,0,13),new Vector3(4,5,18),"oriel.meretHeard","MeretOrielCall","Meret","oriel.coronachHeard");meret.report="CampReturn";
                // Lume, who keeps the lantern and the archivist.
                var lume=kit.Person("Lume",new Vector3(LumeAt.x,0,LumeAt.y),200,"oriel.coronachHeard",true,null,null,"LumeFirst");lume.repeatNode="LumeRepeat";lume.postNode="LumePost";
            }
            /// <summary>The lesson's wall, lit and drawn to: the fibre's light along the wall's top and down its face and on the paving at its foot, the camp's grit lying in lines
            /// that all run in to the wall, the grass at its foot leaning up it, and the runnel's water beaded along the thread where it climbs.</summary>
            static void CampWallPull(ZoneKit kit,Vector3 west,Vector3 east)
            {
                if(pulls==null)return;var rng=new System.Random(311);float R()=>(float)rng.NextDouble();float face=WallFrom.y-.32f;
                Vector3 OnFibre(float x){float t=Mathf.InverseLerp(west.x,east.x,x);return Vector3.Lerp(west,east,t)+Vector3.down*.15f*4*t*(1-t);}
                for(float x=WallFrom.x+.4f;x<=WallTo.x-.4f;x+=.85f)
                {
                    var top=OnFibre(x);
                    Pool(pulls.light,new Vector3(x,1.44f,WallFrom.y),Vector3.up,.75f,new Color(ThrownBlue.r,ThrownBlue.g,ThrownBlue.b,.3f));
                    Pool(pulls.light,new Vector3(x,.86f,WallFrom.y-.325f),Vector3.back,.62f,new Color(ThrownBlue.r,ThrownBlue.g,ThrownBlue.b,.22f));
                    Pool(pulls.light,new Vector3(x,Drawn(new Vector2(x,face-.5f))+.04f,face-.6f),Vector3.up,.9f,new Color(ThrownBlue.r,ThrownBlue.g,ThrownBlue.b,.16f));
                    _=top;
                }
                // Grit in the camp's paving lying in lines up to the wall, thick at its foot.
                for(int k=0;k<420;k++)
                {
                    float x=Mathf.Lerp(WallFrom.x-1,WallTo.x+1,R()),d=.15f+4.2f*Mathf.Pow(R(),1.6f);var p=new Vector2(x,face-d);if(Nearest(p,out _,out _)>-(RimWide+.2f))continue;
                    var to=new Vector2((R()-.5f)*.3f,1).normalized;var side=new Vector2(-to.y,to.x);float len=(.08f+.22f*R())*(1.2f-.12f*d),w=.011f+.014f*R();
                    var tone=R()<.72f?new Color(.4f,.36f,.31f):new Color(.7f,.64f,.54f);Vector3 V(Vector2 q)=>new(q.x,Drawn(q)+.014f,q.y);
                    pulls.grit.Triangle(V(p+side*w),V(p-side*w),V(p+to*len),Vector3.up,tone);
                }
                // Tufts at the wall's foot leaning up it.
                for(float x=WallFrom.x+.8f;x<WallTo.x-.6f;x+=1.1f+R()*1.4f)
                {
                    var root=new Vector3(x,Drawn(new Vector2(x,face-.12f))-.01f,face-.12f);
                    for(int b=0;b<9;b++){var dir=Quaternion.Euler(0,(R()-.5f)*50,0)*Vector3.forward;float len=.24f+.18f*R();pulls.blades.Frond(root+new Vector3((R()-.5f)*.3f,0,-R()*.1f),dir,len*.5f,.034f+.014f*R(),len*1.1f,len*.05f,new Color(.58f,.58f,.34f),new Color(.96f,.92f,.6f),3);}
                }
                // The runnel's water, beaded along the thread for a couple of metres either side of where it climbs.
                for(float x=-13.2f;x<-7.6f;x+=.14f+.08f*R()){float r=.022f+.03f*R();var q=OnFibre(x)+Vector3.down*(.08f+r*.6f);pulls.beads.Lump(q,new Vector3(r,r*1.25f,r),(int)(x*50),.05f,Color.white,new Color(.8f,.9f,1),6,4);}
            }
            static void LumesStall(ZoneKit kit)
            {
                var stall=new ZoneKit.Shape();var at=kit.On(Stall.x,Stall.y);var rng=new System.Random(91);
                stall.Slot(0).Box(at+Vector3.up*.45f,new Vector3(2.4f,.9f,.9f),Quaternion.identity,new Color(.7f,.58f,.42f),new Color(.86f,.74f,.56f));
                foreach(var (dx,dz) in new[]{(-1.15f,-.4f),(1.15f,-.4f),(-1.15f,.4f),(1.15f,.4f)})stall.Slot(0).Post(at+new Vector3(dx,0,dz),2.3f,.05f,.045f,6,new Color(.62f,.5f,.36f),new Color(.82f,.7f,.52f));
                stall.Slot(1).Quad(at+new Vector3(-1.4f,2.4f,-.8f),at+new Vector3(1.4f,2.4f,-.8f),at+new Vector3(1.4f,2.2f,.7f),at+new Vector3(-1.4f,2.2f,.7f),Vector3.up,new Color(.62f,.66f,.72f),.6f);
                for(int k=0;k<7;k++)stall.Slot(2).Lump(at+new Vector3(-.9f+k*.3f,.98f,((float)rng.NextDouble()-.5f)*.3f),new Vector3(.08f,.1f,.08f),k,.05f,new Color(.7f,1,.9f),new Color(.4f,.7f,.6f),6,4);
                var body=kit.Built("Lume's stall",stall,Plank,Cloth,Lamp);ZoneKit.Block(body,at+Vector3.up*.45f,new Vector3(2.4f,.9f,.9f),0,"Stall");
                // The last lantern: a tall post of the chain's make with its lamp lit, the far end of four thousand.
                var lantern=new ZoneKit.Shape();var foot=kit.On(Lantern.x,Lantern.y);
                lantern.Slot(0).Post(foot+Vector3.down*.1f,.4f,.34f,.28f,10,StoneShade,StonePale).Post(foot+Vector3.up*.3f,3.4f,.09f,.07f,8,BronzeDark,BronzePale)
                    .Tube(new[]{foot+Vector3.up*3.6f,foot+new Vector3(.3f,3.9f,0),foot+new Vector3(.62f,3.8f,0)},new[]{.05f,.05f,.04f},6,BronzePale,BronzePale);
                lantern.Slot(1).Lump(foot+new Vector3(.66f,3.42f,0),new Vector3(.2f,.27f,.2f),5,.03f,Color.white,new Color(.9f,.8f,.7f),10,6);
                // The lamp's tassel does not hang: it leans toward the long fibre's collar by the stall.
                var toward=new Vector3(LongTo.x+2.4f-Lantern.x,0,LongTo.y+1.6f-Lantern.y).normalized;var hook=foot+new Vector3(.62f,3.78f,0);var end=hook+Vector3.down*.62f+toward*.5f;
                lantern.Slot(0).Tube(new[]{hook,Vector3.Lerp(hook,end,.5f)+Vector3.down*.05f,end},new[]{.015f,.015f,.015f},5,BronzeDark,BronzePale,1,.5f,false).Lump(end,new Vector3(.06f,.09f,.06f),17,.1f,BronzePale,BronzeDark,6,4);
                var lanternBody=kit.Built("The last lantern",lantern,Bronze,Lamp);ZoneKit.Block(lanternBody,foot+Vector3.up*1.6f,new Vector3(.6f,3.2f,.6f),0,"Lantern post");
                kit.Lamp(new Vector3(Lantern.x+.66f,3.4f,Lantern.y),9,2,new Color(1,.8f,.52f));
            }
            /// <summary>A stream of water, or of leaves and grit, that leaves the ground and runs up along a fibre: the rule, seen.</summary>
            static void Climbing(ZoneKit kit,string name,Vector3 foot,Vector3 up,Vector3 away)
            {
                var glow=kit.Glow("Climbing glints",false,new Color(1.4f,1.5f,1.6f,1));
                var stream=kit.Drift(name,(foot+up)*.5f,new Vector3(.25f,Vector3.Distance(foot,up),.25f),glow,new Color(.85f,.95f,1,.8f),120,.05f,.14f,0,2.4f);
                stream.transform.rotation=Quaternion.FromToRotation(Vector3.up,(up-foot).normalized);
                var velocity=stream.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;var dir=(away-foot).normalized*2.2f;
                velocity.x=new ParticleSystem.MinMaxCurve(dir.x);velocity.y=new ParticleSystem.MinMaxCurve(dir.y);velocity.z=new ParticleSystem.MinMaxCurve(dir.z);
            }

            // ---------------------------------------------------------------- the islands' rims
            /// <summary>Every rim's kerb, drawn: a coping of the court's pale stone along each island's shore, broken where a road or a deck leaves it. The collider's rim is the
            /// height function's; this is what is seen of it.</summary>
            static void Rims(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();int laid=0;
                for(int i=0;i<Isles.Length;i++)
                {
                    var (_,x0,x1,z0,z1,level)=Isles[i];int island=i;
                    foreach(var line in ZoneKit.Contours(p=>Shore(p,island),-RimWide*.45f,x0-6,x1+6,z0-6,z1+6,.5f))
                    {
                        // Coping stones of about a metre, each its own tone, laid along the line, none across an opening.
                        var run=new List<Vector2>{line[0]};float since=0;
                        for(int k=1;k<line.Count;k++){since+=Vector2.Distance(line[k-1],line[k]);if(since>=1.05f||k==line.Count-1){run.Add(line[k]);since=0;}}
                        for(int k=1;k<run.Count;k++)
                        {
                            var p=run[k-1];var q=run[k];var mid=(p+q)*.5f;if(Opening(mid,.2f)>.05f)continue;
                            float tone=.88f+.12f*Mathf.PerlinNoise(mid.x*.7f,mid.y*.7f);float y=level+RimTall*.5f-.05f;
                            shape.Slot(0).Beam(new Vector3(p.x,y,p.y),new Vector3(q.x,y,q.y),.58f,RimTall+.12f,ZoneKit.Shape.Dim(StoneShade,tone),ZoneKit.Shape.Dim(StonePale,tone));laid++;
                        }
                    }
                }
                kit.Built("Rim copings",shape,Kerb);
                Debug.Log("SKYFIELDS_RIMS "+laid);
            }

            /// <summary>Each island's keel: the rock under it, from the rim down and in to a ragged ridge well under its middle, light at the top and darkening into the haze below,
            /// knobbed with outcrops. Built along the same shore the rims follow, drawn only: nothing stands on it and nobody can reach it.</summary>
            static void Keels(ZoneKit kit)
            {
                int built=0;ZoneKit.Shape shape=null,hang=null;
                for(int i=0;i<Isles.Length;i++)
                {
                    // The highest island's keel is drawn in one piece with the forecourt's: on its own its box (28 m across) is what the ground kit keeps its scatter off, and it
                    // covered the whole island's floor.
                    var (_,x0,x1,z0,z1,level)=Isles[i];int island=i;if(i!=6){shape=new ZoneKit.Shape();hang=new ZoneKit.Shape();hang.Slot(2);}var rng=new System.Random(900+i);float H()=>(float)rng.NextDouble();
                    float half=Mathf.Min(x1-x0,z1-z0)*.5f,deep=Mathf.Lerp(9,15,Mathf.InverseLerp(14,20,z1-z0));
                    // The profile, as (in toward the middle, down), as shares of the half width and the depth: a lip, a shoulder, a long taper to the keel.
                    var profile=new[]{(-.01f,0f),(.03f,.1f),(.12f,.26f),(.32f,.46f),(.58f,.68f),(.82f,.86f),(.97f,1f)};
                    foreach(var line in ZoneKit.Contours(p=>Shore(p,island),0,x0-6,x1+6,z0-6,z1+6,.5f))
                    {
                        var run=new List<Vector2>{line[0]};float since=0;
                        for(int k=1;k<line.Count;k++){since+=Vector2.Distance(line[k-1],line[k]);if(since>=1.6f||k==line.Count-1){run.Add(line[k]);since=0;}}
                        var rings=new Vector3[run.Count,profile.Length];
                        for(int k=0;k<run.Count;k++)
                        {
                            var p=run[k];var grad=new Vector2(Shore(p+Vector2.right*.3f,island)-Shore(p-Vector2.right*.3f,island),Shore(p+Vector2.up*.3f,island)-Shore(p-Vector2.up*.3f,island));
                            var inward=grad.sqrMagnitude>1e-6f?-grad.normalized:Vector2.zero;float wobble=.8f+.4f*Mathf.PerlinNoise(p.x*.13f+i,p.y*.13f);
                            for(int r=0;r<profile.Length;r++)
                            {
                                var (inShare,downShare)=profile[r];float jag=r>=profile.Length-2?(Mathf.PerlinNoise(p.x*.31f,p.y*.31f+i*7)-.5f)*2.4f:0;
                                var q=p+inward*inShare*half*.96f;rings[k,r]=new Vector3(q.x,level-downShare*deep*wobble+jag,q.y);
                            }
                        }
                        for(int k=1;k<run.Count;k++)for(int r=1;r<profile.Length;r++)
                        {
                            float t=(r-.5f)/(profile.Length-1);var tone=Color.Lerp(new Color(.98f,.9f,.76f),new Color(.5f,.52f,.6f),t*t*.9f+.1f*t);
                            var mid=(rings[k-1,r-1]+rings[k,r])*.5f;var outward=mid-new Vector3(mid.x,mid.y,mid.z);var a=rings[k-1,r-1];var b=rings[k,r-1];
                            var away=Vector3.Cross(b-a,rings[k-1,r]-a);outward=away.y>0?-away:away;
                            shape.Slot(0).Quad(rings[k-1,r-1],rings[k,r-1],rings[k,r],rings[k-1,r],outward,tone,.18f);
                        }
                        // Outcrops on the shoulder, and the odd hanging boss of rock under the keel.
                        for(int k=2;k<run.Count-1;k+=3+rng.Next(3))
                        {
                            var c=rings[k,2]+Vector3.down*(.4f+(float)rng.NextDouble());float r=1.2f+(float)rng.NextDouble()*1.6f;
                            shape.Slot(0).Lump(c,new Vector3(r,r*.8f,r),k+i*101,.35f,new Color(.9f,.82f,.68f),new Color(.56f,.56f,.6f),9,5);
                            if(rng.Next(3)==0){var low=rings[k,profile.Length-2];shape.Slot(0).Lump(low+Vector3.down*1.5f,new Vector3(r*.7f,r*1.4f,r*.7f),k+i*131,.4f,new Color(.66f,.64f,.66f),new Color(.44f,.46f,.54f),8,5);}
                        }
                        // What hangs from the keel: roots of old fibre still faintly lit, glass grown down the rock in veins and breaking out of it, and blocks of the court's
                        // stone the rock has kept, one held in its face and one hung under it on old fibres.
                        for(int k=1;k<run.Count-1;k+=3+rng.Next(3))
                        {
                            var low=rings[k,profile.Length-2];var mid=rings[k,3];var outward=rings[k,0]-rings[k,profile.Length-1];outward.y=0;outward=outward.sqrMagnitude>1e-4f?outward.normalized:Vector3.back;
                            int pick=rng.Next(7);
                            if(pick<3)
                            {
                                int strands=2+rng.Next(3);
                                for(int s=0;s<strands;s++)
                                {
                                    var top=Vector3.Lerp(mid,low,H())+new Vector3((H()-.5f)*1.4f,-.3f,(H()-.5f)*1.4f);float drop=4+H()*9;var sway=new Vector3((H()-.5f)*3,0,(H()-.5f)*3);
                                    var path=new Vector3[7];var radii=new float[7];for(int q=0;q<7;q++){float t=q/6f;path[q]=top+Vector3.down*drop*t+sway*t*t;radii[q]=Mathf.Lerp(.08f,.025f,t);}
                                    float lit=.1f+.12f*H();hang.Slot(0).Tube(path,radii,5,new Color(lit,lit,lit,1),new Color(lit*.3f,lit*.3f,lit*.3f,1),1,.5f,false);
                                }
                            }
                            else if(pick<5)
                            {
                                var from=rings[k,1]+outward*.25f;var to=rings[k,4]+outward*.25f;var down=(to-from).normalized;
                                for(int q=0;q<6;q++){var p=Vector3.Lerp(from,to,q/6f)+Vector3.Cross(down,outward)*((H()-.5f)*.5f);Crystal(hang.Slot(1),p,(down+outward*.35f+Vector3.Cross(down,outward)*(H()-.5f)*.6f).normalized,.7f+H()*.6f,.1f+H()*.06f);}
                                Cluster(hang.Slot(1),to,outward+Vector3.down*.3f,1.4f+H()*1.2f,rng);
                            }
                            else
                            {
                                var held=mid+outward*.4f+Vector3.down*.6f;hang.Slot(2).Box(held,new Vector3(1.3f,.8f,1),Quaternion.Euler(H()*24-12,H()*360,H()*24-12),StoneShade,StonePale);
                                var under=low+Vector3.down*(2.5f+H()*3)+outward*(H()*1.5f);var turn=Quaternion.Euler(H()*30-15,H()*360,H()*40-20);
                                if(rng.Next(2)==0)hang.Slot(2).Tube(new[]{under+turn*Vector3.down*.7f,under+turn*Vector3.up*.7f},new[]{.42f,.4f},10,StoneShade,StonePale,1,.6f);
                                else hang.Slot(2).Box(under,new Vector3(1.6f,.6f,.7f),turn,StoneShade,StonePale);
                                foreach(float side in new[]{-.5f,.5f}){var tie=under+turn*new Vector3(side,.5f,0);hang.Slot(0).Tube(new[]{low+new Vector3(side,0,0),Vector3.Lerp(low,tie,.5f)+Vector3.down*.2f,tie},new[]{.05f,.04f,.04f},5,new Color(.22f,.22f,.22f,1),new Color(.16f,.16f,.16f,1),1,.5f,false);}
                            }
                        }
                        built++;
                    }
                    if(i==5)continue;string named=i==6?"The forecourt and the highest island":Isles[i].name;
                    kit.Built(named+" keel",shape,Cliff);
                    var hung=kit.Built(named+" keel's hangings",hang,Frayed,GlassDeep,Kerb);hung.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                Debug.Log("SKYFIELDS_KEELS "+built);
            }

            // ---------------------------------------------------------------- lifts
            /// <summary>A lift node at each end of a road of light: the road is a solid ramp the bake walks (between kerbs of light), drawn as a pale ribbon with the fibre down its
            /// middle; the node is a bronze ring let into the paving with the prompt standing on it, one at each end.</summary>
            static void Lift(ZoneKit kit,(string name,Vector2 a,Vector2 rimA,Vector2 rimB,Vector2 b,float from,float to) lift)
            {
                var (name,a,rimA,rimB,b,from,to)=lift;
                Vector3 A=new(a.x,from,a.y),RA=new(rimA.x,from,rimA.y),RB=new(rimB.x,to,rimB.y),B=new(b.x,to,b.y);
                RoadOfLight(kit,name,RA,RB,true);
                var shape=new ZoneKit.Shape();
                foreach(var node in new[]{A,B})
                {
                    shape.Slot(0).Disc(node+Vector3.up*.03f,Vector3.up,1.5f,24,BronzePale,1.15f).Disc(node+Vector3.up*.035f,Vector3.up,.5f,12,BronzePale);
                    shape.Slot(1).Disc(node+Vector3.up*.04f,Vector3.up,1.1f,24,Color.white,.95f);
                }
                kit.Built(name+" nodes",shape,Bronze,Fibre);
                // The thread runs node to node over the road.
                var core=new ZoneKit.Shape();var glow=new ZoneKit.Shape();
                Thread(core,glow,A+Vector3.up*.55f,RA+Vector3.up*.55f,0);Thread(core,glow,RA+Vector3.up*.55f,RB+Vector3.up*.55f,0);Thread(core,glow,RB+Vector3.up*.55f,B+Vector3.up*.55f,0);
                kit.Built(name+" fibre",core,Fibre);kit.Built(name+" fibre glow",glow,FibreSoft);
                Pull(A,A+Vector3.up*.55f,RA-A,2.4f,800+(int)a.y);Pull(B,B+Vector3.up*.55f,RB-B,2.4f,801+(int)a.y);
                LightUnder(pulls?.light??new ZoneKit.Shape(),A+Vector3.up*.55f,RA+Vector3.up*.55f,.8f,ThrownBlue);LightUnder(pulls?.light??new ZoneKit.Shape(),RB+Vector3.up*.55f,B+Vector3.up*.55f,.8f,ThrownBlue);
                foreach(var (here,there,path) in new[]{(A,B,new[]{A,RA,RB,B}),(B,A,new[]{B,RB,RA,A})})
                {
                    var go=new GameObject(name+" node "+(here==A?"low":"high"),typeof(FibreLift));go.transform.position=here+Vector3.up*.2f;
                    var ride=go.GetComponent<FibreLift>();ride.prompt="Ride the lift node";ride.range=2.4f;ride.path=path.Select(p=>p+Vector3.up*.05f).ToArray();ride.seconds=2.6f;
                }
                kit.Sign("LIFT NODE",a.x+2.2f,a.y-2.4f,1.6f);
            }
            /// <summary>A road of light between two points: a solid ramp (static boxes, so the bake and a body meet it) with a kerb each side, drawn as a pale ribbon, a bright edge
            /// and the cloud seen through it. Returns the drawn ribbon.</summary>
            static GameObject RoadOfLight(ZoneKit kit,string name,Vector3 from,Vector3 to,bool solid,GameObject parent=null)
            {
                var run=to-from;var along=run.normalized;var flat=new Vector3(along.x,0,along.z).normalized;var side=Vector3.Cross(Vector3.up,flat);
                var ribbon=new ZoneKit.Shape();var edges=new ZoneKit.Shape();
                ribbon.Slot(0).Quad(from-side*RoadWide*.5f,from+side*RoadWide*.5f,to+side*RoadWide*.5f,to-side*RoadWide*.5f,Vector3.up,new Color(1,1,1,1),.25f);
                foreach(int s in new[]{-1,1})
                {
                    var off=side*s*(RoadWide*.5f+.12f);
                    edges.Slot(0).Beam(from+off+Vector3.up*.2f,to+off+Vector3.up*.2f,.16f,.4f,Color.white);
                }
                var view=kit.Built(name+" road",ribbon,Road);var edge=kit.Built(name+" road kerbs",edges,FibreSoft);edge.transform.SetParent(view.transform,true);
                if(parent!=null)view.transform.SetParent(parent.transform,true);
                if(!solid)return view;
                // The ramp itself: a slab whose top passes through both points, and a kerb each side half a metre high, all tilted along the road.
                var turn=Quaternion.LookRotation(along,Vector3.Cross(along,side));float length=run.magnitude+.6f;
                Box(view,"Road deck",(from+to)*.5f-turn*Vector3.up*.25f,new Vector3(RoadWide+.6f,.5f,length),turn);
                foreach(int s in new[]{-1,1})Box(view,"Road kerb",(from+to)*.5f+side*s*(RoadWide*.5f+.15f)+turn*Vector3.up*.25f,new Vector3(.3f,.5f,length),turn);
                return view;
            }
            static BoxCollider Box(GameObject parent,string name,Vector3 centre,Vector3 size,Quaternion turn,bool isStatic=true)
            {
                var go=new GameObject(name,typeof(BoxCollider));go.isStatic=isStatic;go.transform.SetParent(parent.transform,true);go.transform.SetPositionAndRotation(centre,turn);
                var box=go.GetComponent<BoxCollider>();box.size=size;return box;
            }
            /// <summary>The long fibre: from the forecourt down over every island to the camp, the one way home that has no road under it. A ride, and a line across the sky in every
            /// frame of the upper islands.</summary>
            static void LongFibre(ZoneKit kit)
            {
                var path=new[]{new Vector3(LongFrom.x,Upper+.25f,LongFrom.y),new Vector3(LongFrom.x+2,Upper+7,LongFrom.y-12),new Vector3(-2,28,122),new Vector3(6,30,90),new Vector3(14,25,56),
                    new Vector3(19,13,30),new Vector3(LongTo.x,Camp+2.5f,LongTo.y+6),new Vector3(LongTo.x,Camp+.25f,LongTo.y)};
                var core=new ZoneKit.Shape();var glow=new ZoneKit.Shape();
                var smooth=new List<Vector3>();for(int i=0;i<path.Length-1;i++)for(int k=0;k<6;k++)smooth.Add(CatmullRom(path,i,k/6f));smooth.Add(path[^1]);
                for(int i=1;i<smooth.Count;i++)Thread(core,glow,smooth[i-1]+Vector3.up*.6f,smooth[i]+Vector3.up*.6f,0,.06f);
                kit.Built("The long fibre",core,Fibre);kit.Built("The long fibre glow",glow,FibreSoft);
                var go=new GameObject("The long fibre's node",typeof(FibreLift));go.transform.position=path[0];
                var ride=go.GetComponent<FibreLift>();ride.prompt="Ride the long fibre — Ishen's Camp";ride.range=2.4f;ride.path=smooth.ToArray();ride.seconds=7;ride.lift=0;
                var node=new ZoneKit.Shape();node.Slot(0).Disc(path[0]+Vector3.down*.2f,Vector3.up,1.5f,24,BronzePale,1.15f).Slot(1).Disc(path[0]+Vector3.down*.19f,Vector3.up,1.1f,24,Color.white,.95f);
                Collar(node,path[0]+Vector3.down*.25f+new Vector3(-2.4f,0,0),Vector3.right,1.2f);
                kit.Built("The long fibre's ring",node,Bronze,Fibre);
                Pull(path[0]+Vector3.down*.25f,path[0]+Vector3.up*.6f,path[1]-path[0],3,820);
                var landing=new Vector3(LongTo.x,Camp,LongTo.y);Pull(landing,landing+Vector3.up*.85f,path[^2]-path[^1],3,821);
            }
            static Vector3 CatmullRom(Vector3[] p,int i,float t)
            {
                Vector3 p0=p[Mathf.Max(0,i-1)],p1=p[i],p2=p[Mathf.Min(p.Length-1,i+1)],p3=p[Mathf.Min(p.Length-1,i+2)];
                return .5f*(2*p1+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t);
            }
            /// <summary>The fibres the wisps ride, drawn the same as every other fibre, low over the stone from a collar in one rim to a collar in the other. Each fight's are named for
            /// it (<c>FibreStrand fight i</c>, with its ends as children), for the combat engineer's wisps to find.</summary>
            static void FloorThreads(ZoneKit kit)
            {
                var collars=new ZoneKit.Shape();int i=0;
                foreach(var (fight,from,to) in FloorFibres)
                {
                    // Low over the stone, a hand up: higher, a thread read as a thing hung in the air beside the floor.
                    var a=kit.On(from.x,from.y,.28f);var b=kit.On(to.x,to.y,.28f);
                    Collar(collars,kit.On(from.x,from.y)-(b-a).normalized*.2f,b-a,.8f);Collar(collars,kit.On(to.x,to.y)+(b-a).normalized*.2f,a-b,.8f);
                    // Each its own renderer, so it can light alone: what the wisp rides is the line on the stone under it (the combat engineer's ForceLine), and the thread reads it.
                    var core=new ZoneKit.Shape();var glow=new ZoneKit.Shape();Thread(core,glow,a,b,0,.03f);
                    var strand=new GameObject("FibreStrand "+fight+" "+i);var line=strand.AddComponent<ForceLine>();
                    strand.transform.position=kit.On(from.x,from.y);line.end=kit.On(to.x,to.y);line.width=1.8f;line.clamp=0;
                    var coreBody=kit.Built("Floor fibre "+fight+" "+i,core,Fibre);coreBody.transform.SetParent(strand.transform,true);
                    var haloBody=kit.Built("Floor fibre "+fight+" "+i+" glow",glow,FibreSoft);haloBody.transform.SetParent(strand.transform,true);
                    var shine=new GameObject("Fibre shine",typeof(Light));shine.transform.SetParent(strand.transform,false);shine.transform.position=(a+b)*.5f+Vector3.up*.6f;
                    var l=shine.GetComponent<Light>();l.type=LightType.Point;l.range=Vector3.Distance(a,b)*.6f;l.color=new Color(.62f,.76f,1);l.intensity=0;l.shadows=LightShadows.None;l.enabled=false;
                    var lights=strand.AddComponent<FibreGlow>();lights.line=line;lights.core=coreBody.GetComponent<Renderer>();lights.halo=haloBody.GetComponent<Renderer>();lights.shine=l;
                    lights.rest=TautCore;lights.lit=TautLit;lights.restBase=TautBase;lights.haloTint=TautHalo;lights.haloRest=TautHalo.a;
                    // At rest it still lights the stone under it, and the floor round each collar shows its pull.
                    LightUnder(pulls.light,a,b,.95f,ThrownBlue);
                    Pull(kit.On(from.x,from.y),a,b-a,2.6f,700+i*2);Pull(kit.On(to.x,to.y),b,a-b,2.6f,701+i*2);
                    i++;
                }
                kit.Built("Floor fibre collars",collars,Bronze);
            }

            // ---------------------------------------------------------------- junctions
            /// <summary>A junction: the island hanging crooked off a frayed collar below the near rim, its deck (the way across once seated or cut), the rails across both openings
            /// and the paths carved off the span while it sags, the two hands, the fibre slack, seated and cut, and whatever rides on the island.</summary>
            static FibreJunction Junction(ZoneKit kit,(string name,float x,float rimA,float rimB,float level,Load load) j,bool open)
            {
                var (name,x,rimA,rimB,level,load)=j;int n=Array.IndexOf(Junctions,j)+1;string flag="oriel.junction"+n;
                float zMid=(rimA+rimB)*.5f,length=rimB-rimA+1.2f;
                var root=new GameObject(name);root.transform.position=new Vector3(x,level,zMid);var junction=root.AddComponent<FibreJunction>();
                junction.seatFlag=flag+".seat";junction.cutFlag=flag+".cut";junction.crossedFlag=flag;
                // The island, built round its own middle, so the junction can move it.
                var isle=new ZoneKit.Shape();var rng=new System.Random(300+n);float J()=>(float)rng.NextDouble();isle.Slot(3);
                isle.Slot(0).Box(new Vector3(0,-.25f,0),new Vector3(DeckWide,.5f,length),Quaternion.identity,StoneShade,new Color(.8f,.74f,.64f));
                // Its top is ground, not a deck: broken stone and grit lying on it, a tuft or two, a shard of glass where its fibre has frayed.
                for(int k=0;k<16;k++){var p=new Vector3((J()-.5f)*(DeckWide-1.2f),0,(J()-.5f)*(length-1.6f));isle.Slot(2).Lump(p,new Vector3(.18f+J()*.3f,.06f+J()*.1f,.16f+J()*.24f),k+n*40,.35f,new Color(.86f,.8f,.68f),new Color(.6f,.56f,.5f),7,4);}
                for(int k=0;k<5;k++){var p=new Vector3((J()-.5f)*(DeckWide-1.4f),0,(J()-.5f)*(length-2));isle.Slot(1).Lump(p+Vector3.up*.02f,new Vector3(.5f+J()*.6f,.03f,.4f+J()*.5f),k+n*60,.4f,new Color(.7f,.62f,.5f),new Color(.6f,.54f,.44f),8,3);}
                for(int k=0;k<9;k++){float t=k/8f;isle.Slot(1).Lump(new Vector3((J()-.5f)*2.4f,-2-J()*.6f,Mathf.Lerp(-length*.42f,length*.42f,t)),new Vector3(2.4f,1.4f+J(),2),k+n*20,.3f,new Color(.86f,.78f,.62f),new Color(.6f,.56f,.52f),10,6);}
                for(int k=0;k<5;k++){var c=new Vector3((J()-.5f)*3,-2.6f,Mathf.Lerp(-length*.3f,length*.3f,k/4f));isle.Slot(1).Lump(c,new Vector3(1.1f,1.6f,1.1f),k+n*31,.35f,new Color(.7f,.66f,.6f),new Color(.5f,.48f,.46f),8,5);}
                // Its own rims: broken copings down both long sides, some gone, so from above it reads as a piece of island and not as a ramp; and old fibre hanging under it.
                foreach(int s in new[]{-1,1})for(float z=-length*.5f+.3f;z<length*.5f-.5f;z+=1.1f){if(J()<.22f)continue;float l=.8f+J()*.25f;isle.Slot(2).Beam(new Vector3(s*(DeckWide*.5f+.1f),.18f,z),new Vector3(s*(DeckWide*.5f+.1f),.18f,z+l),.42f,.42f+J()*.1f,StoneShade,StonePale);}
                for(int k=0;k<4;k++){var top=new Vector3((J()-.5f)*3,-3.2f,(J()-.5f)*length*.7f);var sway=new Vector3((J()-.5f)*2,0,(J()-.5f)*2);float drop=3+J()*5;
                    isle.Slot(3).Tube(new[]{top,top+Vector3.down*drop*.5f+sway*.25f,top+Vector3.down*drop+sway},new[]{.07f,.05f,.02f},5,new Color(.2f,.2f,.2f,1),new Color(.08f,.08f,.08f,1),1,.5f,false);}
                var drawn=kit.Built(name+" island",isle,Stone,Cliff,Kerb,Frayed);
                var pivot=new GameObject(name+" (the island)");drawn.transform.SetParent(pivot.transform,false);pivot.transform.SetParent(root.transform,false);
                junction.island=pivot.transform;junction.seatAt=new Vector3(x,level,zMid);junction.seatTurn=Vector3.zero;
                // Sagging, it hangs well below both rims off the near collar (west), rolled down on its free side and its far end dropped, so the cloud shows past its low edge.
                junction.sagAt=new Vector3(x+2,level-3.2f,zMid+.4f);junction.sagTurn=new Vector3(3,7,-30);
                junction.fallAt=new Vector3(x+3,CloudLevel-34,zMid+5);junction.fallTurn=new Vector3(38,30,-44);
                var lost=new List<GameObject>();var wrecked=new List<GameObject>();
                if(load==Load.Garden)
                {
                    // A garden bed on it, raked bare, in a low wall: its keepers are at the far end.
                    var bed=new ZoneKit.Shape();bed.Slot(0).Box(new Vector3(-.6f,.18f,0),new Vector3(3.4f,.36f,length*.66f),Quaternion.identity,StoneShade,StonePale);
                    bed.Slot(1).Box(new Vector3(-.6f,.3f,0),new Vector3(2.9f,.12f,length*.62f),Quaternion.identity,Color.white);
                    var bedBody=kit.Built(name+" garden bed",bed,Kerb,Soil);bedBody.transform.SetParent(pivot.transform,false);lost.Add(bedBody);
                    // Cut, the bed goes over the rim with the island: soil and stones spilt across the far rim's mouth.
                    var spill=new ZoneKit.Shape();var srng=new System.Random(400+n);
                    for(int k=0;k<14;k++){var p=new Vector3(x+((float)srng.NextDouble()-.5f)*6,0,rimB+.6f+(float)srng.NextDouble()*2.2f);spill.Slot(0).Lump(new Vector3(p.x,kit.Y(p.x,p.z)+.05f,p.z),new Vector3(.3f,.1f,.24f),k,.3f,new Color(.82f,.78f,.66f),new Color(.6f,.56f,.48f),7,4);}
                    var spilt=kit.Built(name+" spilt garden",spill,Soil);spilt.transform.SetParent(root.transform,true);wrecked.Add(spilt);
                }
                if(load==Load.Chest)
                {
                    var chest=Stash(kit,"oriel.sky.junction","A gardener's tool chest",Vector2.zero,90,160,ProgressionBuilder.AnyPart(4,GearSlot.Module,1),ProgressionBuilder.Drop("FibreThread",3,1));
                    chest.transform.SetParent(pivot.transform,false);chest.transform.localPosition=new Vector3(DeckWide*.5f-.9f,0,length*.18f);chest.transform.localRotation=Quaternion.Euler(0,90,0);
                    foreach(var t in chest.GetComponentsInChildren<Transform>())t.gameObject.isStatic=false;lost.Add(chest.gameObject);
                    // Hanging, it is out of reach: it is opened once the island is seated, or lost with it.
                    junction.seatedRange=chest.range;chest.range=0;junction.seatedOnly=new InteractionPrompt[]{chest};
                }
                junction.lost=lost.ToArray();junction.wrecked=wrecked.ToArray();
                // The way across: one solid span, static for the bake, live once seated or cut.
                var deck=new GameObject(name+" deck");deck.transform.SetParent(root.transform,false);
                Box(deck,"Deck",new Vector3(x,level-.3f,zMid),new Vector3(DeckWide,.6f,length),Quaternion.identity);
                foreach(int s in new[]{-1,1})Box(deck,"Deck kerb",new Vector3(x+s*(DeckWide*.5f+.15f),level+.25f,zMid),new Vector3(.3f,.5f,length-1.2f),Quaternion.identity);
                junction.deck=deck;
                // While it sags: a bronze rail across each rim's opening, and the span carved out of the paths. A seal, so whatever opens every seal to read the floor opens these.
                var shut=new GameObject(name+" shut");shut.transform.SetParent(root.transform,false);shut.AddComponent<Membrane>();
                var rails=new ZoneKit.Shape();
                foreach(var z in new[]{rimA-.3f,rimB+.3f})
                {
                    var l=new Vector3(x-DockHalf,level,z);var r=new Vector3(x+DockHalf,level,z);
                    rails.Slot(0).Beam(l+Vector3.up*.85f,r+Vector3.up*.85f,.1f,.12f,BronzePale).Beam(l+Vector3.up*.45f,r+Vector3.up*.45f,.08f,.1f,BronzeDark,BronzePale);
                    for(int k=0;k<=4;k++)rails.Slot(0).Post(Vector3.Lerp(l,r,k/4f)+Vector3.down*.05f,.95f,.07f,.06f,6,BronzeDark,BronzePale);
                    Box(shut,"Rail",new Vector3(x,level+.6f,z),new Vector3(DockHalf*2,1.2f,.4f),Quaternion.identity,false);
                }
                var railBody=kit.Built(name+" rails",rails,Bronze);railBody.transform.SetParent(shut.transform,true);
                var carve=new GameObject("Span shut",typeof(NavMeshObstacle));carve.transform.SetParent(shut.transform,false);carve.transform.position=new Vector3(x,level,zMid);
                var obstacle=carve.GetComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=new Vector3(DeckWide+1,2,length-.4f);obstacle.carving=true;obstacle.carveOnlyStationary=false;
                junction.shut=new[]{shut};
                // The fibres: the collar on the near rim and the frayed one hanging to the island; seated, taut along the island's side; cut, a road of light across the gap.
                var collarAt=kit.On(x-DockHalf-1.1f,rimA-.6f);var farAt=kit.On(x+DockHalf+1.1f,rimB+.6f);
                var collar=new ZoneKit.Shape();Collar(collar,collarAt,Vector3.forward,1.1f);Collar(collar,farAt,Vector3.back,1.1f);kit.Built(name+" collars",collar,Bronze).transform.SetParent(root.transform,true);
                var ring=collarAt+Vector3.up*.82f;var farRing=farAt+Vector3.up*.82f;
                var slack=new ZoneKit.Shape();var slackGlow=new ZoneKit.Shape();var frng=new System.Random(340+n);
                var hang=junction.sagAt+Quaternion.Euler(junction.sagTurn)*new Vector3(-DeckWide*.5f,.1f,-length*.5f+1);var hangFar=junction.sagAt+Quaternion.Euler(junction.sagTurn)*new Vector3(DeckWide*.5f,.1f,length*.5f-1);
                // The fray, at the near collar: the fibre comes out of the ring as one and parts into its strands, most of them snapped and sparking, two still holding the
                // island; past the parting it runs on, amber and guttering, to the island's corner. The far one is going the same way.
                var parted=Parting(slack,ring,hang,frng);FrayedThread(slack,slackGlow,parted,hang,.35f,.05f,frng);
                var partedFar=Parting(slack,farRing,hangFar,frng,.6f);FrayedThread(slack,slackGlow,partedFar,hangFar,.5f,.045f,frng);
                var slackBody=kit.Built(name+" fibre slack",slack,Frayed);var slackHalo=kit.Built(name+" fibre slack glow",slackGlow,FrayedSoft);slackHalo.transform.SetParent(slackBody.transform,true);slackBody.transform.SetParent(root.transform,true);
                // Its light, on the rim round the collar and on the paving: amber, guttering with it.
                var pool=new ZoneKit.Shape();var foot=new Vector3(collarAt.x,Drawn(new Vector2(collarAt.x,collarAt.z))+.04f,collarAt.z);
                Pool(pool,foot,Vector3.up,1.9f,new Color(ThrownAmber.r,ThrownAmber.g,ThrownAmber.b,.42f));Pool(pool,foot+Vector3.up*.005f,Vector3.up,.8f,new Color(ThrownAmber.r,ThrownAmber.g,ThrownAmber.b,.5f));
                LightUnder(pool,ring,parted,.6f,ThrownAmber);
                kit.Built(name+" fray's light",pool,FibreLight).transform.SetParent(slackBody.transform,true);
                var flare=kit.Lamp(new Vector3(collarAt.x,1.1f,collarAt.z+.3f),4.5f,1.3f,new Color(1,.62f,.3f));flare.name="Fray's light";flare.transform.SetParent(slackBody.transform,true);
                var gutters=slackBody.AddComponent<FibreGlow>();gutters.core=slackBody.GetComponent<Renderer>();gutters.halo=slackHalo.GetComponent<Renderer>();gutters.shine=flare;gutters.gutter=.85f;gutters.shineRest=1.3f;
                Pull(collarAt,ring,(hang-ring).normalized,2.4f,360+n,true,false,false);
                var taut=new ZoneKit.Shape();var tautGlow=new ZoneKit.Shape();Thread(taut,tautGlow,ring,farRing,0,.05f);
                var tautBody=kit.Built(name+" fibre seated",taut,Fibre);kit.Built(name+" fibre seated glow",tautGlow,FibreSoft).transform.SetParent(tautBody.transform,true);tautBody.transform.SetParent(root.transform,true);
                var road=RoadOfLight(kit,name+" cut",new Vector3(x,level+.02f,rimA-.4f),new Vector3(x,level+.02f,rimB+.4f),false,root);
                var roadThread=new ZoneKit.Shape();var roadGlow=new ZoneKit.Shape();Thread(roadThread,roadGlow,ring,new Vector3(x,level+.5f,rimA),0,.05f);Thread(roadThread,roadGlow,new Vector3(x,level+.5f,rimA),new Vector3(x,level+.5f,rimB),0,.06f);
                Thread(roadThread,roadGlow,new Vector3(x,level+.5f,rimB),farRing,0,.05f);
                kit.Built(name+" cut fibre",roadThread,Fibre).transform.SetParent(road.transform,true);kit.Built(name+" cut fibre glow",roadGlow,FibreSoft).transform.SetParent(road.transform,true);
                junction.slack=slackBody;junction.taut=tautBody;junction.road=road;
                // The hands: seat at the collar, cut three paces along the fibre's other side. Each sets its own flag; both go once either is worked.
                var seat=kit.Discovery(name+" seat",new Vector3(x-DockHalf-1.1f,0,rimA-2.2f),"Seat the junction",junction.seatFlag,"",null,"Taren",null,0,2.6f);seat.situation="junction.seat";
                var cut=kit.Discovery(name+" cut",new Vector3(x+DockHalf+.9f,0,rimA-2.2f),"Cut the junction",junction.cutFlag,"",null,"Taren",null,0,2.6f);
                // Cutting the garden's junction wrecks the garden: what the party says is that (barks.json "garden"), not the cut.
                cut.situation=load==Load.Garden?"garden":"junction.cut";
                seat.transform.SetParent(root.transform,true);cut.transform.SetParent(root.transform,true);junction.hands=new[]{seat.gameObject,cut.gameObject};
                // As it sags, then: the island hung crooked, the deck off and the rails up (a still of the crossed route seats them all).
                pivot.transform.SetPositionAndRotation(open?junction.seatAt:junction.sagAt,Quaternion.Euler(open?junction.seatTurn:junction.sagTurn));
                deck.SetActive(open);shut.SetActive(!open);tautBody.SetActive(open);slackBody.SetActive(!open);road.SetActive(false);foreach(var w in wrecked)w.SetActive(false);
                return junction;
            }
            /// <summary>A frayed garden junction of the Empty Gardens: a small walled bed hanging crooked off a collar beyond the rim, raised level when seated (seat only: the quest
            /// can't be lost). The hand is offered only at the quest's first step and reports <c>GardenJunction</c>; the party's word is <c>mend</c>.</summary>
            static FibreJunction GardenJunction(ZoneKit kit,Vector2 hand,Vector2 garden,string flag,int index)
            {
                float level=Nearest(hand,out float l,out _)<0?l:Upper;
                var root=new GameObject("Frayed garden "+(index+1));root.transform.position=new Vector3(garden.x,level,garden.y);var junction=root.AddComponent<FibreJunction>();
                junction.seatFlag=flag;junction.cutFlag="";junction.crossedFlag="";
                var bed=new ZoneKit.Shape();var rng=new System.Random(500+index);
                bed.Slot(0).Box(new Vector3(0,-.2f,0),new Vector3(5,.4f,3.6f),Quaternion.identity,StoneShade,StonePale);
                foreach(var (dx,dz,sx,sz) in new[]{(0f,-1.65f,5f,.3f),(0f,1.65f,5f,.3f),(-2.35f,0f,.3f,3.6f),(2.35f,0f,.3f,3.6f)})bed.Slot(0).Box(new Vector3(dx,.15f,dz),new Vector3(sx,.5f,sz),Quaternion.identity,StoneShade,StonePale);
                bed.Slot(2).Box(new Vector3(0,.06f,0),new Vector3(4.3f,.12f,3),Quaternion.identity,Color.white);
                for(int k=0;k<5;k++)bed.Slot(1).Lump(new Vector3(((float)rng.NextDouble()-.5f)*3,-1-(float)rng.NextDouble(),((float)rng.NextDouble()-.5f)*2),new Vector3(1.5f,1.1f,1.2f),k+index*9,.3f,new Color(.84f,.76f,.6f),new Color(.6f,.56f,.5f),9,5);
                var drawn=kit.Built("Frayed garden "+(index+1)+" bed",bed,Kerb,Cliff,Soil);
                var pivot=new GameObject("Frayed garden "+(index+1)+" (the garden)");drawn.transform.SetParent(pivot.transform,false);pivot.transform.SetParent(root.transform,false);
                junction.island=pivot.transform;junction.seatAt=new Vector3(garden.x,level-.3f,garden.y);junction.seatTurn=Vector3.zero;
                junction.sagAt=new Vector3(garden.x+.3f,level-2.4f,garden.y+.4f);junction.sagTurn=new Vector3(9,-6,-8);junction.fallAt=junction.sagAt;junction.fallTurn=junction.sagTurn;junction.rise=3;
                var collarAt=kit.On(hand.x+1.6f,hand.y+2.2f);var shape=new ZoneKit.Shape();Collar(shape,collarAt,Vector3.forward,.9f);kit.Built("Frayed garden "+(index+1)+" collar",shape,Bronze).transform.SetParent(root.transform,true);
                var ring=collarAt+Vector3.up*.7f;
                var slack=new ZoneKit.Shape();var slackGlow=new ZoneKit.Shape();var hang=junction.sagAt+Quaternion.Euler(junction.sagTurn)*new Vector3(0,.1f,-1.6f);
                var frng=new System.Random(520+index);var parted=Parting(slack,ring,hang,frng,.8f);FrayedThread(slack,slackGlow,parted,hang,.4f,.04f,frng);
                var slackBody=kit.Built("Frayed garden "+(index+1)+" fibre slack",slack,Frayed);var slackHalo=kit.Built("Frayed garden "+(index+1)+" fibre slack glow",slackGlow,FrayedSoft);
                slackHalo.transform.SetParent(slackBody.transform,true);slackBody.transform.SetParent(root.transform,true);
                var pool=new ZoneKit.Shape();var foot=new Vector3(collarAt.x,Drawn(new Vector2(collarAt.x,collarAt.z))+.04f,collarAt.z);
                Pool(pool,foot,Vector3.up,1.5f,new Color(ThrownAmber.r,ThrownAmber.g,ThrownAmber.b,.38f));kit.Built("Frayed garden "+(index+1)+" fray's light",pool,FibreLight).transform.SetParent(slackBody.transform,true);
                var gutters=slackBody.AddComponent<FibreGlow>();gutters.core=slackBody.GetComponent<Renderer>();gutters.halo=slackHalo.GetComponent<Renderer>();gutters.gutter=.85f;
                Pull(collarAt,ring,(hang-ring).normalized,2,540+index,true,false,false);
                var taut=new ZoneKit.Shape();var tautGlow=new ZoneKit.Shape();Thread(taut,tautGlow,ring,junction.seatAt+new Vector3(0,.2f,-1.6f),.05f,.045f);
                var tautBody=kit.Built("Frayed garden "+(index+1)+" fibre seated",taut,Fibre);kit.Built("Frayed garden "+(index+1)+" fibre seated glow",tautGlow,FibreSoft).transform.SetParent(tautBody.transform,true);tautBody.transform.SetParent(root.transform,true);
                junction.slack=slackBody;junction.taut=tautBody;
                var seat=kit.Discovery("Frayed garden "+(index+1)+" seat",new Vector3(hand.x,0,hand.y),"Seat the junction",flag,"GardenJunction",null,"Taren","EmptyGardens",0,2.8f);seat.situation="mend";
                seat.transform.SetParent(root.transform,true);junction.hands=new[]{seat.gameObject};
                pivot.transform.SetPositionAndRotation(junction.sagAt,Quaternion.Euler(junction.sagTurn));tautBody.SetActive(false);
                return junction;
            }

            // ---------------------------------------------------------------- the gardens
            /// <summary>A walled bed of the court's: a low wall round a bed of raked bare grit. Nothing grows in it.</summary>
            static void WalledBed(ZoneKit kit,string name,Vector2 at,float wide,float deep,bool solid)
            {
                var shape=new ZoneKit.Shape();var c=kit.On(at.x,at.y);
                foreach(var (dx,dz,sx,sz) in new[]{(0f,-deep*.5f,wide,.36f),(0f,deep*.5f,wide,.36f),(-wide*.5f,0f,.36f,deep),(wide*.5f,0f,.36f,deep)})
                    shape.Slot(0).Box(c+new Vector3(dx,.32f,dz),new Vector3(sx,.64f,sz),Quaternion.identity,StoneShade,StonePale);
                shape.Slot(1).Box(c+new Vector3(0,.06f,0),new Vector3(wide-.4f,.12f,deep-.4f),Quaternion.identity,Color.white);
                var body=kit.Built(name,shape,Kerb,Soil);
                if(solid)foreach(var (dx,dz,sx,sz) in new[]{(0f,-deep*.5f,wide,.36f),(0f,deep*.5f,wide,.36f),(-wide*.5f,0f,.36f,deep),(wide*.5f,0f,.36f,deep)})ZoneKit.Block(body,c+new Vector3(dx,.32f,dz),new Vector3(sx,.64f,sz),0,"Bed wall");
            }
            /// <summary>The swept stair: four steps of the court's stone climbing toward the rim and stopping in the air, where its island was moved from, and a gardener sweeping it.</summary>
            static void SweptStair(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();var foot=kit.On(StairAt.x,StairAt.y-1.4f);
                for(int k=0;k<4;k++)shape.Slot(0).Box(foot+new Vector3(0,.15f+k*.3f,.35f+k*.7f),new Vector3(2.4f,.3f+k*.6f,.7f),Quaternion.identity,StoneShade,StonePale);
                var body=kit.Built("The swept stair",shape,Kerb);ZoneKit.Block(body,foot+new Vector3(0,.6f,1.4f),new Vector3(2.4f,1.2f,2.8f),0,"Stair");
                GardenerAtWork(kit,new Vector3(Sweeper.x,0,Sweeper.y),60,new Color(.98f,.95f,.9f),false,true);
            }
            /// <summary>The far end of the second junction's garden on the stair gardens' rim, with its two keepers raking it. Cut the junction and the garden is wrecked: the keepers
            /// are gone from here and stand as Pruners where they stood (the fight's spawners).</summary>
            static void SecondGardenKeepers(ZoneKit kit,out Vector2[] keepers)
            {
                var j=Junctions[1];keepers=new[]{new Vector2(j.x-4.6f,j.rimB+3.4f),new Vector2(j.x+4.4f,j.rimB+3.8f)};
                WalledBed(kit,"Second junction's garden",new Vector2(j.x,j.rimB+4.6f),5.6f,3,true);
                var keep=new GameObject("The garden's keepers (gone once it is cut)",typeof(FlagGate)).GetComponent<FlagGate>();keep.flag="oriel.junction2.cut";keep.showWhenSet=false;
                keep.targets=keepers.Select((k,i)=>GardenerAtWork(kit,new Vector3(k.x,0,k.y),i==0?120:240,new Color(.96f,.94f,.9f))).ToArray();
            }
            /// <summary>The sealed garden: a garden house of the court's at the stair gardens' west end, its door waxed shut from outside. The gardener leads the party here once the
            /// three gardens are seated; the hand at the door reports <c>SealedGarden</c>, and the door is open from then on onto shelves of stoppered jars and one raked bed.</summary>
            static void SealedGarden(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();float x0=-53,x1=-45,z0=124.5f,z1=131.5f;var floor=kit.Y(-49,128);
                // Walls three metres, the east one broken by its door; a roof of glass panes on bronze ribs.
                foreach(var (a,b) in new[]{(new Vector2(x0,z0),new Vector2(x1,z0)),(new Vector2(x0,z1),new Vector2(x1,z1)),(new Vector2(x0,z0),new Vector2(x0,z1)),(new Vector2(x1,z0),new Vector2(x1,127)),(new Vector2(x1,129),new Vector2(x1,z1))})
                    shape.Slot(0).Beam(new Vector3(a.x,floor+1.5f,a.y),new Vector3(b.x,floor+1.5f,b.y),.5f,3,StoneShade,StonePale);
                shape.Slot(0).Beam(new Vector3(x1,floor+2.75f,127),new Vector3(x1,floor+2.75f,129),.5f,.5f,StoneShade,StonePale);
                for(int k=0;k<5;k++){float z=Mathf.Lerp(z0,z1,k/4f);shape.Slot(1).Beam(new Vector3(x0,floor+3.1f,z),new Vector3(x1,floor+3.1f,z),.1f,.12f,BronzePale);}
                var body=kit.Built("Sealed garden house",shape,Kerb,Bronze);
                foreach(var (a,b) in new[]{(new Vector2(x0,z0),new Vector2(x1,z0)),(new Vector2(x0,z1),new Vector2(x1,z1)),(new Vector2(x0,z0),new Vector2(x0,z1)),(new Vector2(x1,z0),new Vector2(x1,127)),(new Vector2(x1,129),new Vector2(x1,z1))})
                    ZoneKit.BlockAlong(body,new Vector3(a.x,floor+1.5f,a.y),new Vector3(b.x,floor+1.5f,b.y),.5f,3,"House wall");
                // Inside: shelves of stoppered jars along the back wall, and one bed raked with nothing in it.
                var inside=new ZoneKit.Shape();var rng=new System.Random(611);
                for(int s=0;s<3;s++){float y=floor+.6f+s*.7f;inside.Slot(0).Box(new Vector3(x0+.45f,y,128),new Vector3(.4f,.06f,6.2f),Quaternion.identity,new Color(.7f,.58f,.42f));
                    for(int k=0;k<14;k++)inside.Slot(1).Post(new Vector3(x0+.45f+((float)rng.NextDouble()-.5f)*.15f,y+.03f,125.2f+k*.42f),.22f+.06f*(float)rng.NextDouble(),.09f,.07f,8,new Color(.8f,.9f,.86f),new Color(.9f,.96f,.92f));}
                inside.Slot(2).Box(new Vector3(-48,floor+.06f,128),new Vector3(3,.12f,4),Quaternion.identity,Color.white);
                // Its one bed is raked in a spiral round a single jar: the only pattern of its kind in the gardens.
                var heart=new Vector3(-48,floor+.12f,128);inside.Slot(1).Post(heart+Vector3.down*.02f,.32f,.1f,.08f,8,new Color(.8f,.9f,.86f),new Color(.9f,.96f,.92f));
                Vector3? turnFrom=null;
                for(float a=0;a<Mathf.PI*2*5.4f;a+=.16f)
                {
                    float r=.22f+.21f*a/(Mathf.PI*2);var p=heart+new Vector3(Mathf.Cos(a)*r,.004f,Mathf.Sin(a)*r);bool inBed=Mathf.Abs(p.x+48)<1.36f&&Mathf.Abs(p.z-128)<1.86f;
                    if(inBed&&turnFrom is Vector3 q)inside.Slot(2).Beam(q,p,.065f,.012f,new Color(.55f,.53f,.48f));turnFrom=inBed?p:null;
                }
                kit.Built("Sealed garden's jars",inside,Plank,Jar,Soil);
                // The door, waxed shut: a seal, so the bake (made with it open) knows the room, and gone once the gardener has led the party to it.
                var door=new GameObject("Sealed garden door");var doorShape=new ZoneKit.Shape();
                doorShape.Slot(0).Box(new Vector3(x1,floor+1.3f,128),new Vector3(.2f,2.6f,1.9f),Quaternion.identity,new Color(.68f,.54f,.38f),new Color(.8f,.66f,.48f));
                doorShape.Slot(1).Lump(new Vector3(x1+.12f,floor+1.3f,127.1f),new Vector3(.06f,.5f,.08f),3,.2f,new Color(.96f,.86f,.6f),new Color(.8f,.68f,.44f),6,5).Lump(new Vector3(x1+.12f,floor+1.3f,128.9f),new Vector3(.06f,.5f,.08f),4,.2f,new Color(.96f,.86f,.6f),new Color(.8f,.68f,.44f),6,5);
                var doorBody=kit.Built("Sealed garden door leaf",doorShape,Plank,Lamp);doorBody.transform.SetParent(door.transform,true);door.AddComponent<Membrane>();
                Box(door,"Door",new Vector3(x1,floor+1.3f,128),new Vector3(.4f,2.6f,2),Quaternion.identity,false);
                var gone=new GameObject("The sealed garden opens",typeof(FlagGate)).GetComponent<FlagGate>();gone.flag="oriel.sealedGardenSeen";gone.showWhenSet=false;gone.targets=new[]{door};
                var hand=kit.Discovery("The sealed garden's door",new Vector3(SealedHand.x,0,SealedHand.y),"Go in after the gardener","oriel.sealedGardenSeen","SealedGarden","IdraSealedGarden","Idra","EmptyGardens",1,3);hand.situation="";
                kit.Sign("SEALED GARDEN",SealedHand.x+2.2f,SealedHand.y-3.6f,1.6f);
            }
            static void HighIsland(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();var at=kit.On(LanternSeat.x,LanternSeat.y);
                shape.Slot(0).Post(at+Vector3.down*.1f,.7f,.62f,.55f,14,StoneShade,StonePale).Disc(at+Vector3.up*.61f,Vector3.up,.36f,14,new Color(.62f,.56f,.46f),0,StonePale);
                var seat=kit.Built("The lantern's old seat",shape,Kerb);ZoneKit.Block(seat,at+Vector3.up*.3f,new Vector3(1.2f,.7f,1.2f),0,"Seat");
                // The twin of Lume's lantern, in its seat once it is set.
                var lit=new ZoneKit.Shape();lit.Slot(0).Post(at+Vector3.up*.6f,.7f,.05f,.04f,6,BronzeDark,BronzePale).Slot(1).Lump(at+Vector3.up*1.4f,new Vector3(.16f,.22f,.16f),9,.03f,Color.white,new Color(.9f,.8f,.7f),10,6);
                var lantern=kit.Built("The far lantern",lit,Bronze,Lamp);var glow=kit.Lamp(new Vector3(LanternSeat.x,1.5f,LanternSeat.y),10,2.2f,new Color(1,.8f,.52f));glow.transform.SetParent(lantern.transform,true);
                var set=new GameObject("The far lantern is set",typeof(FlagGate)).GetComponent<FlagGate>();set.flag="oriel.lanternSet";set.targets=new[]{lantern};lantern.SetActive(false);
                var hand=kit.Discovery("The worn seat",new Vector3(SeatHand.x,0,SeatHand.y),"Set the lantern in the seat","oriel.lanternSet","LanternSeat","IdraFarLantern","Taren","LumesLantern",0,3);hand.situation="";
                kit.Sign("HIGHEST ISLAND",-60,152.6f,1.8f);
                Stash(kit,"oriel.sky.high","The chain's old stores",HighCache,30,200,ProgressionBuilder.AnyPart(4,GearSlot.Emitter,1),ProgressionBuilder.Drop("ChargeBank",1,1),ProgressionBuilder.Drop("FibreThread",2,1));
            }
            /// <summary>The Glass Choir's door: a great portal of the court's in the forecourt's north rim, and the way in.</summary>
            static void ChoirDoor(ZoneKit kit)
            {
                // A threshold of the court's stone across the gap to the Choir's front, and the great door let into the front: pilasters, a lintel with the ring-and-bar mark,
                // its two bronze leaves stood open on the dark inside.
                var shape=new ZoneKit.Shape();float y=Upper;var x=DoorAt.x;const float face=171.4f;
                shape.Slot(0).Box(new Vector3(x,y-.3f,(DoorAt.y+face)*.5f+.2f),new Vector3(6.4f,.6f,face-DoorAt.y+.8f),Quaternion.identity,StoneShade,StonePale);
                foreach(int side in new[]{-1,1})shape.Slot(0).Box(new Vector3(x+side*4,y+4.5f,face-.5f),new Vector3(1.4f,9,1.2f),Quaternion.identity,StoneShade,StonePale);
                shape.Slot(0).Box(new Vector3(x,y+9.5f,face-.5f),new Vector3(9.6f,1.4f,1.4f),Quaternion.identity,StoneShade,StonePale);
                shape.Slot(1).Disc(new Vector3(x,y+9.5f,face-1.25f),Vector3.back,.6f,20,BronzePale,.44f).Beam(new Vector3(x-1.1f,y+9.5f,face-1.25f),new Vector3(x+1.1f,y+9.5f,face-1.25f),.16f,.12f,BronzePale);
                // Inside, the Choir's light: pale and cold, brightest low where the glass is nearest the door, in bands as it comes through the body.
                for(int k=0;k<6;k++){float y0=y+k*8.8f/6,y1=y+(k+1)*8.8f/6;float b=1.1f-k*.09f;shape.Slot(2).Quad(new Vector3(x-3.3f,y0,face+.05f),new Vector3(x+3.3f,y0,face+.05f),new Vector3(x+3.3f,y1,face+.05f),new Vector3(x-3.3f,y1,face+.05f),Vector3.back,new Color(.62f*b,.86f*b,1f*b),.2f);}
                foreach(int side in new[]{-1,1})shape.Slot(1).Box(new Vector3(x+side*3.9f,y+4.4f,face-1.6f),new Vector3(.18f,8.6f,3.2f),Quaternion.Euler(0,side*-12,0),BronzeDark,BronzePale);
                // The glass in the doorway, against its own light (one piece with the door, which stands on the threshold), and that light lying out across the threshold and the forecourt.
                var rng=new System.Random(171);
                Cluster(shape.Slot(3),new Vector3(x-2.2f,y+.2f,face-.1f),new Vector3(.4f,.6f,-.5f),2.6f,rng);Cluster(shape.Slot(3),new Vector3(x+2.4f,y+.4f,face-.1f),new Vector3(-.3f,.7f,-.5f),2.2f,rng);
                Cluster(shape.Slot(3),new Vector3(x+.4f,y+7.6f,face-.1f),new Vector3(0,-.6f,-.7f),1.8f,rng);
                kit.Built("The Choir's door",shape,Kerb,Bronze,InnerLight,ChoirGlass);
                var spill=new ZoneKit.Shape();
                for(float z=face-.4f;z>DoorAt.y-4;z-=1.1f){float t=(face-z)/(face-DoorAt.y+4);Pool(spill,new Vector3(x,Upper+.05f,z),Vector3.up,3.4f-t*1.4f,new Color(.5f,.85f,1,.34f*(1-t*.7f)));}
                kit.Built("The Choir's light on the threshold",spill,FibreLight);
                kit.Lamp(new Vector3(x,1.8f,face-1.6f),9,1.8f,new Color(.62f,.86f,1)).name="The Choir's light";
                var way=new GameObject("Choir door — The Glass Choir");way.transform.position=kit.On(DoorAt.x,DoorAt.y-.8f,1);
                kit.Way(way,"Choir door — The Glass Choir",ChoirZone,"Arrival",null,null,3.6f,new Vector2(DoorAt.x+4,DoorAt.y-3.2f));
            }
            /// <summary>A chest of the court's gardeners: bronze-bound, on short feet.</summary>
            static LootCache Stash(ZoneKit kit,string id,string label,Vector2 at,float yaw,int scrip,params LootEntry[] contents)
            {
                var shape=new ZoneKit.Shape();
                shape.Slot(0).Box(new Vector3(0,.4f,0),new Vector3(1.3f,.66f,.8f),Quaternion.identity,new Color(.7f,.58f,.42f),new Color(.86f,.74f,.56f)).Box(new Vector3(0,.8f,0),new Vector3(1.36f,.14f,.86f),Quaternion.identity,new Color(.8f,.68f,.5f));
                foreach(float x in new[]{-.44f,.44f})shape.Slot(1).Box(new Vector3(x,.45f,0),new Vector3(.1f,.92f,.9f),Quaternion.identity,BronzePale);
                foreach(var (x,z) in new[]{(-.55f,-.3f),(.55f,-.3f),(-.55f,.3f),(.55f,.3f)})shape.Slot(1).Box(new Vector3(x,.04f,z),new Vector3(.16f,.1f,.16f),Quaternion.identity,BronzeDark);
                var body=kit.Built("Chest "+id,shape,Plank,Bronze);body.transform.SetPositionAndRotation(kit.On(at.x,at.y),Quaternion.Euler(0,yaw,0));
                var box=body.AddComponent<BoxCollider>();box.center=Vector3.up*.45f;box.size=new Vector3(1.4f,.9f,.9f);body.isStatic=true;
                var cache=LootCache.On(body,id,label,scrip,contents);cache.range=2.6f;return cache;
            }

            // ---------------------------------------------------------------- the two who walk with the party
            /// <summary>The gardener: by the camp's wall with its rake until the rule is said, then twelve paces behind; ahead to the first frayed garden once it has been seen
            /// following, its light low until it is asked (or the party moves on past the second junction); in front to the sealed garden once three gardens are seated; and on the
            /// Choir's step for good. Never a word (manifest 8.3).</summary>
            static KeepsPace Gardener(ZoneKit kit)
            {
                var root=new GameObject("The gardener that follows");root.transform.SetPositionAndRotation(kit.On(GardenerHome.x,GardenerHome.y),Quaternion.Euler(0,180,0));
                var prefab=ZoneKit.Prefab("Characters","Gardener");
                if(prefab!=null){var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.transform.SetParent(root.transform,false);body.name="Gardener body";
                    foreach(var rig in body.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullUpdateTransforms;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
                    // Its rake, carried upright in its right hand wherever it goes.
                    var arm=body.GetComponentInChildren<Animator>();var hand=arm!=null&&arm.isHuman?arm.GetBoneTransform(HumanBodyBones.RightHand):null;
                    if(hand!=null)
                    {
                        var idle=arm.runtimeAnimatorController!=null?arm.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle")):null;if(idle!=null)idle.SampleAnimation(arm.gameObject,.3f);
                        var rake=Tool(kit,false,"that follows");rake.transform.SetPositionAndRotation(new Vector3(hand.position.x,root.transform.position.y+.02f,hand.position.z)+root.transform.forward*.04f,Quaternion.LookRotation(root.transform.forward));
                        rake.transform.SetParent(hand,true);
                    }}
                var face=new GameObject("Gardener's light",typeof(Light));face.transform.SetParent(root.transform,false);face.transform.localPosition=new Vector3(0,2,.2f);
                var light=face.GetComponent<Light>();light.type=LightType.Point;light.range=3.5f;light.intensity=1.4f;light.color=new Color(1,.86f,.6f);light.shadows=LightShadows.None;
                var keeps=root.AddComponent<KeepsPace>();keeps.gap=9;keeps.walk=3;keeps.hurry=6.6f;keeps.calm=14;keeps.lampLight=light;keeps.lamps=root.GetComponentsInChildren<Renderer>().Where(r=>!r.name.StartsWith("Gardener's")).ToArray();
                Vector3 P(Vector2 p)=>kit.On(p.x,p.y);
                var gj1=GardenJunctions[0].garden;
                keeps.stages=new[]
                {
                    new KeepsPace.Stage{when="",point=P(GardenerHome),faces=P(GardenerHome+new Vector2(0,-6)),glow=1},
                    new KeepsPace.Stage{when="oriel.ruleSeen",follows=true,glow=1},
                    new KeepsPace.Stage{when="oriel.followerSeen",point=P(GardenerWait),faces=P(gj1),glow=.35f},
                    new KeepsPace.Stage{when="oriel.gardenerAsked|oriel.junction2",follows=true,glow=1},
                    new KeepsPace.Stage{when="oriel.doorReached",point=P(GardenerStep),faces=P(GardenerStep+new Vector2(0,-6)),glow=.5f},
                    new KeepsPace.Stage{when="oriel.gardensSeated",point=P(SealedWait),faces=P(SealedDoor),leads=true,glow=1},
                    new KeepsPace.Stage{when="oriel.sealedGardenSeen",follows=true,glow=1},
                    new KeepsPace.Stage{when="oriel.sealedGardenSeen&oriel.doorReached",point=P(GardenerStep),faces=P(GardenerStep+new Vector2(0,-6)),glow=.5f},
                };
                return keeps;
            }
            /// <summary>Ishen: at his crates until the rule is said, then at the party's heels (never in a fight) until he has read the record, and at his crates again after.</summary>
            static KeepsPace Ishen(ZoneKit kit)
            {
                var ishen=kit.Person("Ishen",new Vector3(IshenPost.x,0,IshenPost.y),200,"oriel.coronachHeard",true,null,null,"IshenCamp");ishen.repeatNode="IshenCamp";ishen.postNode="IshenOrielPost";
                // Walking with the party he never stands in its way: his body is no wall to the heroes (a lead walking the camp met him head on and stopped there for good).
                ishen.GetComponent<CapsuleCollider>().isTrigger=true;
                var keeps=ishen.gameObject.AddComponent<KeepsPace>();keeps.gap=3.6f;keeps.walk=3.4f;keeps.hurry=6.8f;keeps.calm=16;
                Vector3 P(Vector2 p)=>kit.On(p.x,p.y);
                keeps.stages=new[]
                {
                    new KeepsPace.Stage{when="",point=P(IshenPost),faces=P(IshenPost+new Vector2(-3,-4))},
                    new KeepsPace.Stage{when="oriel.ruleSeen",follows=true},
                    new KeepsPace.Stage{when="oriel.coronachHeard",point=P(IshenPost),faces=P(IshenPost+new Vector2(-3,-4))},
                };
                return keeps;
            }

            // ---------------------------------------------------------------- dressing
            /// <summary>The walked lines, island by island: the ground kit keeps them clear, the scatter keeps its tall things off them.</summary>
            internal static readonly Vector2[][] Ways=
            {
                new Vector2[]{new(-36,13),new(-15,14),new(0,14),new(12,14),new(20,13),new(26,17.5f)},new Vector2[]{new(26,17.5f),new(26,39.5f)},
                new Vector2[]{new(26,39.5f),new(18,43),new(0,43),new(-22,43),new(-30,47),new(-30,49)},new Vector2[]{new(-30,43),new(-48,43)},new Vector2[]{new(-30,49),new(-30,65)},
                new Vector2[]{new(-30,65),new(-20,71),new(0,71),new(20,71),new(32,74.5f)},new Vector2[]{new(32,74.5f),new(32,97.5f)},
                new Vector2[]{new(32,97.5f),new(22,101),new(6,101),new(-12,103),new(-30,104),new(-38,107)},new Vector2[]{new(-38,107),new(-38,123)},
                // Off the second junction's landing the far bed of its garden stands across the way north (SecondGardenKeepers: (-38, 125.6), 5.6 by 3) with a keeper at each end:
                // the walked lines go round them, east along the rim below the bed and its east keeper, and west between its west keeper and the garden house. They ran straight
                // through the bed's south wall, swept path and all, and a body walking them stood against it (chapter 7's routes, November 14).
                new Vector2[]{new(-38,123),new(-31.5f,123),new(-30,129),new(-10,130),new(10,131),new(20,135)},new Vector2[]{new(-38,123),new(-43.8f,122.9f),new(-43.8f,126.4f),new(-43,128)},new Vector2[]{new(-36,130),new(-40,133)},
                new Vector2[]{new(20,135),new(20,151)},new Vector2[]{new(20,151),new(10,157),new(4,162),new(4,167)},new Vector2[]{new(4,162),new(-12,159.5f)},
                new Vector2[]{new(-56,154.5f),new(-62,157.2f)},
            };
            static bool NearWay(Vector2 p,float within){foreach(var way in Ways)if(ZoneKit.FromPath(p,way)<within)return true;return false;}
            static readonly Vector2 Away=new(Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));
            /// <summary>Nothing this tall at a plan point may stand where an island's walked floor lies within the fixed camera's shadow of it (between the camera and a hero there).</summary>
            static bool ShadowsAFloor(Vector2 p,float tall)
            {
                // The floor that matters is what a hero stands on to walk or fight: more than a stride in from the rim. A thing on the rim's own strip shades only the kerb and the
                // void past it; one further in hides whoever stands north of it.
                float reach=ZoneKit.Shadowed(tall)+1.5f;var side=new Vector2(Away.y,-Away.x);Nearest(p,out float level,out _);
                for(float d=1.5f;d<=reach;d+=.75f)foreach(float s in new[]{-1.2f,0,1.2f}){var q=p+Away*d+side*s;if(Nearest(q,out float there,out _)<-2.6f&&there<=level+.5f)return true;}
                return false;
            }
            /// <summary>A kit piece drawn in the zone's glass: the generated tree's and rib's shapes, given a rim, a glint, a light inside and the island seen through them.</summary>
            static GameObject InGlass(GameObject piece,Material glass)
            {
                foreach(var r in piece.GetComponentsInChildren<Renderer>()){var m=r.sharedMaterials;for(int i=0;i<m.Length;i++)m[i]=glass;r.sharedMaterials=m;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;}
                return piece;
            }
            static void Dress(ZoneKit kit)
            {
                var rng=new System.Random(71);float R()=>(float)rng.NextDouble();
                // ---- glass trees: where a fibre frayed. On each island's north rim, behind the walked line, where nothing walked lies in their shadow; the grove's at the terrace's west end.
                int trees=0;
                void Tree(Vector2 at,float tall,float yaw)
                {
                    if(ShadowsAFloor(at,tall)||NearWay(at,3)){Debug.Log("SKYFIELDS_TREE_LEFT_OUT "+at);return;}
                    var tree=InGlass(kit.Prop(ZoneKit.Kit("GlassTree","CrystalClusterA"),new Vector3(at.x,-.1f,at.y),tall,yaw,false),Glass);tree.name="Glass tree";
                    ZoneKit.Block(tree,kit.On(at.x,at.y,1.2f),new Vector3(.7f,2.4f,.7f),0,"Glass trunk");trees++;
                    // A light in the glass where the fibre frayed, and the glint it throws on the stone round it.
                    var glow=kit.Lamp(new Vector3(at.x,tall*.55f,at.y),5,.7f,new Color(.7f,.9f,1));glow.name="Glass light";glow.transform.SetParent(tree.transform,true);
                }
                foreach(var (x,z,h) in new[]{(-42f,20.4f,5.4f),(-30f,20.8f,6.4f),(20.5f,20.8f,5.2f),(30f,49.6f,6f),(-6f,49.8f,5.6f),(-56.4f,47.2f,6.6f),(-52f,49.6f,7.2f),(-46f,49.8f,6f),(-38.6f,48.4f,5.8f),
                    (-36f,77.6f,6.4f),(26f,77.4f,5.8f),(-20f,77.8f,5.4f),(26f,107.4f,6.2f),(-26f,107.6f,5.6f),(-48f,135.2f,6.8f),(26f,135.4f,5.6f),(-16f,167f,6.4f),(28f,167.2f,5.8f),(-66f,161.4f,7f)})
                    Tree(new Vector2(x,z),h,R()*360);
                Debug.Log("SKYFIELDS_TREES "+trees);
                // Low glass growth at the grove's open sides and along the floor fibres' collars: shards a hand to a knee high, never tall enough to hide anyone.
                var shardShape=new ZoneKit.Shape();int shards=0;
                void Shards(Vector2 at,float spread,int count,float tallest)
                {
                    for(int k=0;k<count;k++)
                    {
                        float a=R()*6.28f,far=spread*Mathf.Sqrt(R()),tall=tallest*(.35f+.65f*R());var foot=kit.On(at.x+Mathf.Cos(a)*far,at.y+Mathf.Sin(a)*far,-.05f);
                        var lean=new Vector3((R()-.5f)*.6f,1,(R()-.5f)*.6f).normalized;
                        shardShape.Slot(0).Tube(new[]{foot,foot+lean*tall*.6f,foot+lean*tall},new[]{.07f+.06f*tall,.05f+.04f*tall,.005f},5,Color.white,Color.white,1,1,false);shards++;
                    }
                }
                foreach(var (x,z) in new[]{(-57f,40f),(-57f,44f),(-50f,36.6f),(-44f,36.8f),(-36.4f,37f)})Shards(new Vector2(x,z),1.2f,7,.8f);
                foreach(var (_,from,to) in FloorFibres){Shards(from+(to-from).normalized*.8f,.7f,4,.5f);Shards(to-(to-from).normalized*.8f,.7f,4,.5f);}
                foreach(var j in Junctions){Shards(new Vector2(j.x-DockHalf-2.6f,j.rimA-.9f),.6f,5,.6f);}
                kit.Built("Glass shards",shardShape,Glass);Debug.Log("SKYFIELDS_SHARDS "+shards);

                // ---- the court's stone: colonnades along the north rims (one column down), a gate standing alone on the fibre fields with nothing beyond it, the forecourt's pillars.
                int stood=0;
                // Each its own piece: drawn as one, the colonnades' box was a wall forty metres long to anything that reads a prop by its box (guide 3.5).
                void Column(Vector2 at,float tall,bool fallen=false)
                {
                    if(!fallen&&ShadowsAFloor(at,tall)){Debug.Log("SKYFIELDS_COLUMN_LEFT_OUT "+at);return;}
                    var columns=new ZoneKit.Shape();
                    var foot=kit.On(at.x,at.y);var up=fallen?Quaternion.Euler(0,R()*40-20,0)*Vector3.right:Vector3.up;float r=.42f;
                    if(fallen)foot+=Vector3.up*(r*.92f)-up*tall*.5f;
                    // A plinth, a fluted shaft in drums (one broken short if it fell), a capital with the court's bronze collar.
                    if(!fallen)columns.Slot(0).Box(foot+Vector3.up*.2f,new Vector3(1.3f,.4f,1.3f),Quaternion.Euler(0,R()*90,0),StoneShade,StonePale);
                    int drums=Mathf.Max(3,Mathf.RoundToInt(tall/1.1f));float each=(tall-(fallen?0:.9f))/drums;var from=foot+(fallen?Vector3.zero:Vector3.up*.4f);
                    for(int d=0;d<drums;d++){var a=from+up*(d*each+.02f);var b=from+up*((d+1)*each-.02f);float tone=.9f+.12f*(float)rng.NextDouble();
                        columns.Slot(0).Tube(new[]{a,b},new[]{r,r*.97f},12,ZoneKit.Shape.Dim(StoneShade,tone),ZoneKit.Shape.Dim(StonePale,tone),3,.6f);}
                    if(!fallen){var cap=from+up*(drums*each);columns.Slot(0).Box(cap+Vector3.up*.25f,new Vector3(1.2f,.5f,1.2f),Quaternion.identity,StoneShade,StonePale);
                        columns.Slot(1).Tube(new[]{cap-Vector3.up*.12f,cap+Vector3.up*.02f},new[]{r+.06f,r+.06f},12,BronzeDark,BronzePale);
                        stood++;}
                    var body=kit.Built((fallen?"Fallen column ":"Court column ")+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),columns,Kerb,Bronze);
                    if(fallen)ZoneKit.Block(body,foot+up*tall*.5f,new Vector3(.9f,.9f,.9f)+new Vector3(Mathf.Abs(up.x),0,Mathf.Abs(up.z))*(tall-.9f),0,"Fallen column");
                    else{ZoneKit.Block(body,foot+Vector3.up*tall*.5f,new Vector3(.9f,tall,.9f),0,"Column");ZoneKit.Fadeable(body);}
                }
                foreach(float x in new[]{4f,9f,14f})Column(new Vector2(x,49.6f),4.4f);Column(new Vector2(19,48.2f),4,true);
                foreach(float x in new[]{-16f,-10f,12f})Column(new Vector2(x,135.6f),4.6f);Column(new Vector2(-4,133),4.2f,true);
                Column(new Vector2(-6,167.6f),6.2f);Column(new Vector2(14,167.6f),6.2f);
                foreach(var (x,z) in new[]{(-70f,161.6f),(-50f,161.4f)})Column(new Vector2(x,z),3.6f);
                Debug.Log("SKYFIELDS_COLUMNS "+stood);
                // A gate standing alone on the fibre fields' rim, opening onto nothing: its island was moved a long time ago.
                Column(new Vector2(-15.2f,77.8f),5.2f);Column(new Vector2(-8.8f,77.8f),5.2f);
                var lintel=new ZoneKit.Shape();lintel.Slot(0).Box(kit.On(-12,77.8f,5.75f),new Vector3(8.2f,.9f,1.3f),Quaternion.identity,StoneShade,StonePale).Slot(1).Disc(kit.On(-12,77.1f,5.75f),Vector3.back,.34f,16,BronzePale,.24f);
                var gateTop=kit.Built("A gate to nowhere",lintel,Kerb,Bronze);ZoneKit.Fadeable(gateTop);

                // ---- the camp: an awning over Ishen's crates, a kettle on a little fire, bedrolls, the courier pod on the pad.
                Awning(kit,new Vector2(-8.4f,17.6f),3.6f,2.6f,new Color(.66f,.58f,.46f),new Vector3(-8.4f,1.35f,21));Awning(kit,new Vector2(-26.4f,16.6f),3,2.4f,new Color(.56f,.42f,.34f),new Vector3(-22.5f,1,21.5f));
                WashingLine(kit,new Vector2(-.6f,18.2f),new Vector2(3.4f,18.4f),new Vector3(1.4f,1.35f,21));
                var campFire=new ZoneKit.Shape();var fire=kit.On(-14,17.2f);
                for(int k=0;k<7;k++){float a=k*.9f;campFire.Slot(0).Lump(fire+new Vector3(Mathf.Cos(a)*.5f,.08f,Mathf.Sin(a)*.5f),new Vector3(.14f,.1f,.12f),k,.3f,StoneShade,new Color(.5f,.46f,.4f),6,4);}
                campFire.Slot(1).Post(fire+new Vector3(-.4f,0,0),.9f,.03f,.03f,5,BronzeDark,BronzePale).Post(fire+new Vector3(.4f,0,0),.9f,.03f,.03f,5,BronzeDark,BronzePale).Beam(fire+new Vector3(-.4f,.88f,0),fire+new Vector3(.4f,.88f,0),.04f,.04f,BronzePale);
                campFire.Slot(1).Lump(fire+new Vector3(0,.58f,0),new Vector3(.17f,.15f,.17f),3,.05f,BronzePale,BronzeDark,8,5);campFire.Slot(2).Lump(fire+Vector3.up*.12f,new Vector3(.22f,.14f,.22f),9,.4f,Color.white,new Color(1,.6f,.3f),7,4);
                kit.Built("Ishen's kettle",campFire,Kerb,Bronze,Lamp);kit.Lamp(new Vector3(-14,.6f,17.2f),5,1.1f,new Color(1,.62f,.32f));
                var beds=new ZoneKit.Shape();foreach(var (x,z,yaw) in new[]{(-22.4f,13.8f,10f),(-20f,13.6f,-8f)})beds.Slot(0).Box(kit.On(x,z,.08f),new Vector3(.8f,.16f,1.9f),Quaternion.Euler(0,yaw,0),new Color(.32f,.28f,.26f),new Color(.44f,.36f,.3f));
                kit.Built("Bedrolls",beds,Cloth);
                var pod=kit.Prop(ZoneKit.Kit("Skiff","DressRover"),new Vector3(Pad.x,0,Pad.y),1.7f,70,false);pod.name="Courier pod";
                CampThings(kit);

                // ---- gardens: walled beds of bare grit with their keepers, a pump of the old engineers', terraced beds and more stairs that end in air.
                WalledBed(kit,"Fibre fields bed",new Vector2(-28,76.4f),6,2.6f,true);GardenerAtWork(kit,new Vector3(-27.4f,0,76.9f),185,new Color(1,.96f,.9f),true,false,.12f);
                WalledBed(kit,"Sagging gardens bed",new Vector2(-24,106.6f),6,2.6f,true);
                GardenerAtWork(kit,new Vector3(-25.2f,0,107.3f),180,new Color(.96f,.97f,1),true,false,.12f);GardenerAtWork(kit,new Vector3(-21,0,104.8f),200,new Color(1,.95f,.88f));
                var pump=kit.Prop(ZoneKit.Kit("AncelanePump","HushwellColumn"),new Vector3(-18,0,107.2f),2,200);pump.name="Garden pump";
                RaisedBed(kit,new Vector2(-10,134.6f),8,2.4f,.9f);RaisedBed(kit,new Vector2(0,124.4f),6,2.2f,.5f);
                GardenerAtWork(kit,new Vector3(-12.6f,0,132.2f),160,new Color(.98f,.94f,.9f));
                AirStair(kit,new Vector2(-1.6f,135.4f),3);AirStair(kit,new Vector2(-62,161.2f),2);

                // ---- what holds the islands up: fibres from collars on every rim down into the cloud, and from one island's face to the next across each gap; and the islets they hold.
                var holdCore=new ZoneKit.Shape();var holdGlow=new ZoneKit.Shape();var holdCollars=new ZoneKit.Shape();int held=0;
                // The north rims, which every frame of an island looks at, have one every dozen metres or so, and each shows its pull on the floor round it.
                for(int i=0;i<Isles.Length;i++)
                {
                    var (_,x0,x1,z0,z1,level)=Isles[i];
                    foreach(bool north in new[]{false,true})
                    {
                        int n=Mathf.Max(2,Mathf.RoundToInt((x1-x0)/(north?12.5f:22)));
                        for(int k=0;k<n;k++)
                        {
                            float x=Mathf.Lerp(x0+5,x1-5,(k+.5f)/n)+(R()-.5f)*3;
                            if(!RimFoot(i,x,north,out var rim)||Opening(rim,2)>0||NearWay(rim,2.5f))continue;
                            var foot=new Vector3(rim.x,Drawn(rim),rim.y);var start=foot+Vector3.up*.68f;var below=foot+new Vector3((R()-.5f)*14,-14-R()*10,north?6+R()*4:-6-R()*4);
                            Thread(holdCore,holdGlow,start,below,1.2f,.035f);held++;
                            Collar(holdCollars,foot,north?Vector3.forward:Vector3.back,.9f);
                            Pull(foot,start,below-start,2.8f,1000+i*50+k*2+(north?1:0));
                        }
                    }
                }
                // Across each gap, low, from one face to the next: the fibres the next island hangs on, seen at the back of every frame.
                foreach(var (za,zb,la,lb,xs) in new[]{(22f,35f,Camp,Terrace,new[]{-30f,-6f,12f}),(51f,63f,Terrace,Terrace,new[]{-12f,8f,28f}),(79f,93f,Terrace,Upper,new[]{-30f,-6f,14f}),(109f,121f,Upper,Upper,new[]{-20f,4f,22f}),(137f,149f,Upper,Upper,new[]{-8f,8f,26f})})
                    foreach(float x in xs){Thread(holdCore,holdGlow,new Vector3(x,la-2.4f,za+.3f),new Vector3(x+(R()-.5f)*6,lb-3.6f,zb-.3f),1.8f,.03f);held++;}
                kit.Built("Fibres that hold the islands",holdCore,Fibre);kit.Built("Fibres that hold the islands glow",holdGlow,FibreSoft);kit.Built("Rim collars",holdCollars,Bronze);
                Debug.Log("SKYFIELDS_HOLDING_FIBRES "+held);
                Islets(kit,rng);

                // ---- the Glass Choir beyond the forecourt: the court's cathedral front, and behind and over it the body of the bridled Choir gone to glass, the size of a hill.
                Choir(kit);
                // Faces of the islands: glass grown out of the rock below each rim where a fibre leaves it, catching the light.
                var faceGlass=new ZoneKit.Shape();int grown=0;
                for(int i=0;i<Isles.Length;i++)
                {
                    var (_,x0,x1,z0,z1,level)=Isles[i];
                    for(float x=x0+4;x<x1-4;x+=5+R()*4)
                    {
                        var at=new Vector2(x,z0-.6f);if(Opening(new Vector2(x,z0),2)>0)continue;
                        var foot=new Vector3(x,level-1.6f-R()*1.2f,z0+.4f);
                        for(int k=0;k<3;k++){var tip=foot+new Vector3((R()-.5f)*1.4f,-.6f-R()*1.8f,-(.4f+R()*.8f));faceGlass.Slot(0).Tube(new[]{foot,Vector3.Lerp(foot,tip,.5f),tip},new[]{.22f,.14f,.01f},5,Color.white,Color.white,1,1,false);}
                        grown++;_=at;
                    }
                }
                kit.Built("Glass on the faces",faceGlass,GlassDeep);Debug.Log("SKYFIELDS_FACE_GLASS "+grown);
                // ---- each island's own ground: the gardens tended, the fibre fields grown through with glass, the forecourt's old inlay.
                GardenGround(kit);GlassGround(kit);ForecourtInlay(kit);OldStone(kit);
            }
            /// <summary>An awning of the camp's: four poles and a pale cloth, low enough that nobody behind it is lost from the camera.</summary>
            static void Awning(ZoneKit kit,Vector2 at,float wide,float deep,Color cloth,Vector3 fibre)
            {
                var shape=new ZoneKit.Shape();var c=kit.On(at.x,at.y);var rng=new System.Random((int)(at.x*10));float R()=>(float)rng.NextDouble();
                foreach(var (dx,dz) in new[]{(-1,-1),(1,-1),(-1,1),(1,1)})shape.Slot(0).Post(c+new Vector3(dx*wide*.5f,0,dz*deep*.5f),dz<0?2.1f:2.4f,.05f,.04f,6,new Color(.62f,.52f,.38f),new Color(.82f,.72f,.56f));
                shape.Slot(1).Quad(c+new Vector3(-wide*.55f,2.1f,-deep*.55f),c+new Vector3(wide*.55f,2.1f,-deep*.55f),c+new Vector3(wide*.55f,2.45f,deep*.55f),c+new Vector3(-wide*.55f,2.45f,deep*.55f),Vector3.up,cloth,.5f);
                // A rug under it, striped, worn pale where it is walked.
                for(int k=0;k<6;k++){float z0=-deep*.38f+k*deep*.76f/6,z1=z0+deep*.76f/6;var tone=k%2==0?new Color(.62f,.36f,.26f):new Color(.86f,.74f,.52f);
                    shape.Slot(1).Quad(c+new Vector3(-wide*.36f,.03f,z0),c+new Vector3(wide*.36f,.03f,z0),c+new Vector3(wide*.36f,.03f,z1),c+new Vector3(-wide*.36f,.03f,z1),Vector3.up,tone,.5f);}
                // What hangs from its edges (cords, a bell, herbs drying) hangs toward the camp's fibre, not down.
                for(int k=0;k<7;k++)
                {
                    bool back=k<4;float x=Mathf.Lerp(-wide*.5f,wide*.5f,(k%4+.5f)/4)+(R()-.5f)*.2f;var hook=c+new Vector3(x,back?2.44f:2.12f,(back?deep:-deep)*.55f);
                    var lean=fibre-hook;lean.y=0;lean=lean.normalized;float length=.45f+.4f*R();var end=hook+Vector3.down*length*.82f+lean*length*.55f;
                    shape.Slot(0).Tube(new[]{hook,Vector3.Lerp(hook,end,.5f)+Vector3.down*.03f,end},new[]{.012f,.012f,.012f},4,new Color(.5f,.42f,.32f),new Color(.6f,.5f,.38f),1,.5f,false);
                    var weight=k%3==0?new Color(.9f,.82f,.56f):k%3==1?new Color(.5f,.56f,.32f):new Color(.86f,.9f,.92f);
                    shape.Slot(0).Lump(end+lean*.03f,new Vector3(.05f,.07f,.05f),k+(int)at.x,.25f,weight,ZoneKit.Shape.Dim(weight,.7f),6,4);
                }
                kit.Built("Awning "+Mathf.RoundToInt(at.x),shape,Plank,Cloth);
            }
            /// <summary>What a camp lived in leaves about on the paving south of the walk, all of it low: herbs drying on a tarp, firewood, sacks and a water barrel behind Lume's
            /// stall, crates and a coil of rope by the pad and by the lift.</summary>
            static void CampThings(ZoneKit kit)
            {
                // Three pieces (by the pad, behind the kettle and the stall, by the lift), each small enough for the ground kit to keep its own scatter off.
                var west=new ZoneKit.Shape();var middle=new ZoneKit.Shape();var east=new ZoneKit.Shape();foreach(var s in new[]{west,middle,east})s.Slot(2);
                ZoneKit.Shape At(float x)=>x<-20?west:x>10?east:middle;
                var things=middle;var rng=new System.Random(81);float T()=>(float)rng.NextDouble();
                var tarp=kit.On(-5.6f,9.2f,.03f);things.Slot(1).Quad(tarp+new Vector3(-1,0,-.7f),tarp+new Vector3(1,0,-.7f),tarp+new Vector3(1,0,.7f),tarp+new Vector3(-1,0,.7f),Vector3.up,new Color(.72f,.66f,.52f),.5f);
                for(int k=0;k<16;k++)things.Slot(1).Lump(tarp+new Vector3((T()-.5f)*1.7f,.04f,(T()-.5f)*1.2f),new Vector3(.13f,.04f,.06f),k,.4f,new Color(.58f,.62f,.34f),new Color(.42f,.44f,.26f),6,4,.25f,Quaternion.Euler(0,T()*180,0));
                foreach(var (x,z) in new[]{(1.6f,9.4f),(2.3f,9f),(1.9f,10f),(-36.6f,8.6f)})At(x).Slot(1).Lump(kit.On(x,z,.2f),new Vector3(.3f,.24f,.26f),(int)(x*10+z),.25f,new Color(.8f,.72f,.56f),new Color(.6f,.52f,.4f),8,5);
                var barrel=kit.On(3.5f,9.7f);things.Slot(0).Post(barrel+Vector3.down*.02f,.64f,.3f,.28f,12,new Color(.56f,.44f,.32f),new Color(.72f,.6f,.44f));things.Slot(2).Disc(barrel+Vector3.up*.6f,Vector3.up,.26f,12,Color.white);
                var wood=kit.On(-1.4f,8.8f);
                for(int row=0;row<3;row++)for(int k=0;k<4-row;k++){var c=wood+new Vector3((k-(3-row)*.5f)*.19f,.085f+row*.16f,0);things.Slot(0).Tube(new[]{c+Vector3.back*.45f,c+Vector3.forward*.45f},new[]{.085f,.08f},7,new Color(.5f,.4f,.3f),new Color(.62f,.5f,.36f));}
                foreach(var (x,z,yaw) in new[]{(-38.2f,8.2f,12f),(-37.4f,7.4f,-20f),(19.4f,9.6f,8f),(20.4f,10.4f,30f)})At(x).Slot(0).Box(kit.On(x,z,.3f),new Vector3(.7f,.6f,.6f),Quaternion.Euler(0,yaw,0),new Color(.62f,.5f,.36f),new Color(.78f,.66f,.48f));
                foreach(var (x,z) in new[]{(-34.6f,8.4f),(18.2f,9f)})
                {
                    var coil=kit.On(x,z,.06f);for(int ring=0;ring<3;ring++){var path=new Vector3[17];for(int k=0;k<17;k++){float a=k/16f*Mathf.PI*2;path[k]=coil+new Vector3(Mathf.Cos(a)*(.36f-ring*.05f),ring*.05f,Mathf.Sin(a)*(.36f-ring*.05f));}At(x).Slot(1).Tube(path,path.Select(_=>.04f).ToArray(),6,new Color(.74f,.66f,.48f),new Color(.74f,.66f,.48f),1,.5f,false);}
                }
                kit.Built("The camp's things by the pad",west,Plank,Cloth,Jar);kit.Built("The camp's things behind the stall",middle,Plank,Cloth,Jar);kit.Built("The camp's things by the lift",east,Plank,Cloth,Jar);
            }
            /// <summary>The camp's washing, pegged on a line between two poles: every cloth on it hangs toward the wall's fibre.</summary>
            static void WashingLine(ZoneKit kit,Vector2 from,Vector2 to,Vector3 fibre)
            {
                var shape=new ZoneKit.Shape();var rng=new System.Random(41);float R()=>(float)rng.NextDouble();var a=kit.On(from.x,from.y);var b=kit.On(to.x,to.y);
                shape.Slot(0).Post(a+Vector3.down*.1f,2.2f,.05f,.04f,6,new Color(.62f,.52f,.38f),new Color(.82f,.72f,.56f)).Post(b+Vector3.down*.1f,2.2f,.05f,.04f,6,new Color(.62f,.52f,.38f),new Color(.82f,.72f,.56f));
                var la=a+Vector3.up*2;var lb=b+Vector3.up*2;shape.Slot(0).Rope(la,lb,.12f,.012f,new Color(.7f,.64f,.52f));
                var lean=fibre-(la+lb)*.5f;lean.y=0;lean=lean.normalized;
                var tones=new[]{new Color(.92f,.9f,.84f),new Color(.46f,.56f,.7f),new Color(.86f,.66f,.36f),new Color(.96f,.94f,.9f),new Color(.66f,.4f,.32f)};
                float along=.3f;int n=0;float run=Vector3.Distance(la,lb);
                while(along<run-.5f)
                {
                    float w=.45f+.3f*R(),drop=.55f+.35f*R();float t0=along/run,t1=(along+w)/run;
                    Vector3 L(float t)=>Vector3.Lerp(la,lb,t)+Vector3.down*(.12f*4*t*(1-t)+.02f);
                    var p0=L(t0);var p1=L(t1);var hang=(Vector3.down*.78f+lean*.62f).normalized*drop;
                    shape.Slot(1).Quad(p0,p1,p1+hang+lean*.05f,p0+hang,lean,tones[n%tones.Length],.5f);
                    along+=w+.12f+.2f*R();n++;
                }
                var line=kit.Built("Washing line",shape,Plank,Cloth);
                ZoneKit.Block(line,a+Vector3.up*1,new Vector3(.2f,2,.2f),0,"Pole");ZoneKit.Block(line,b+Vector3.up*1,new Vector3(.2f,2,.2f),0,"Pole");
            }
            /// <summary>A raised bed of the stair gardens: a stone-walled step of raked grit, its own little stair up the front.</summary>
            static void RaisedBed(ZoneKit kit,Vector2 at,float wide,float deep,float tall)
            {
                var shape=new ZoneKit.Shape();var c=kit.On(at.x,at.y);
                shape.Slot(0).Box(c+new Vector3(0,tall*.5f,0),new Vector3(wide,tall,deep),Quaternion.identity,StoneShade,StonePale);
                shape.Slot(1).Box(c+new Vector3(0,tall+.04f,0),new Vector3(wide-.5f,.08f,deep-.5f),Quaternion.identity,Color.white);
                for(int k=0;k<3;k++)shape.Slot(0).Box(c+new Vector3(-wide*.5f+1,tall*(k+.5f)/6,-deep*.5f-.25f-(2-k)*.25f),new Vector3(1.2f,tall*(k+1)/3,.25f),Quaternion.identity,StoneShade,StonePale);
                var body=kit.Built("Raised bed "+Mathf.RoundToInt(at.x),shape,Kerb,Soil);ZoneKit.Block(body,c+new Vector3(0,tall*.5f,0),new Vector3(wide,tall,deep),0,"Raised bed");
            }
            /// <summary>Another stair that ends in air: on a north rim, climbing out over the gap where its island was moved from.</summary>
            static void AirStair(ZoneKit kit,Vector2 at,int steps)
            {
                var shape=new ZoneKit.Shape();var foot=kit.On(at.x,at.y);
                for(int k=0;k<steps;k++)shape.Slot(0).Box(foot+new Vector3(0,.15f+k*.3f,.35f+k*.7f),new Vector3(2,.3f+k*.6f,.7f),Quaternion.identity,StoneShade,StonePale);
                var body=kit.Built("Stair to the air "+Mathf.RoundToInt(at.x),shape,Kerb);ZoneKit.Block(body,foot+new Vector3(0,steps*.15f,steps*.35f),new Vector3(2,steps*.3f,steps*.7f),0,"Stair");
            }
            // ---------------------------------------------------------------- each island's own ground
            /// <summary>Where a flat thing may lie on a garden's open ground: off the walks and their edging, away from every prompt, out of the fights, and clear of whatever stands.</summary>
            static Func<Vector2,float,bool> OpenGround()
            {
                Physics.SyncTransforms();
                var prompts=UnityEngine.Object.FindObjectsByType<InteractionPrompt>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(p=>new Vector2(p.transform.position.x,p.transform.position.z)).ToList();
                var fights=UnityEngine.Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None).Select(f=>{var b=f.GetComponent<Collider>().bounds;return Rect.MinMaxRect(b.min.x,b.min.z,b.max.x,b.max.z);}).ToList();
                return (p,r)=>
                {
                    if(Nearest(p,out _,out _)>-(2+r)||NearWay(p,2.2f+r)||prompts.Any(q=>Vector2.Distance(q,p)<2.2f+r)||fights.Any(f=>f.Contains(p)))return false;
                    foreach(var c in Physics.OverlapSphere(new Vector3(p.x,Drawn(p)+.7f,p.y),r+.45f,~0,QueryTriggerInteraction.Ignore))if(!(c is MeshCollider))return false;
                    return true;
                };
            }
            static int IsleOf(Vector2 p){Nearest(p,out _,out int i);return i;}
            /// <summary>Raked rings round one point: grooves and the ridges between them, from just outside it outward, lying on the ground (or on a bed's top at <paramref name="top"/>).</summary>
            static void RakedRings(ZoneKit.Shape shape,Vector2 at,float from,int count,float? top=null)
            {
                float y=top??Enumerable.Range(0,8).Select(k=>Drawn(at+new Vector2(Mathf.Cos(k*.785f),Mathf.Sin(k*.785f))*(from+count*.24f))).Append(Drawn(at)).Max();
                for(int k=0;k<count;k++)
                {
                    float r=from+k*.24f;var c=new Vector3(at.x,y+.012f,at.y);
                    shape.Disc(c,Vector3.up,r+.075f,36,new Color(.56f,.54f,.49f),r).Disc(c+Vector3.up*.002f,Vector3.up,r+.19f,36,new Color(1.08f,1.04f,.96f),r+.1f);
                }
            }
            /// <summary>The gardens' ground, tended: kept stones and lone glass stems in the raked grit (the ground kit's layer, the swept path of flags left through it), each
            /// ringed; the stair gardens raked in rows of fans instead (their own pattern); and the gardeners' plumb lines on the beds, every one hanging toward the nearest fibre.</summary>
            static void GardenGround(ZoneKit kit)
            {
                var open=OpenGround();var rings=new ZoneKit.Shape();var kept=new ZoneKit.Shape();kept.Slot(1);var fans=new ZoneKit.Shape();
                var rng=new System.Random(1301);float R()=>(float)rng.NextDouble();int stones=0,stems=0;var placed=new List<Vector2>();
                bool Raked(Vector2 p){int i=IsleOf(p);return (i==1&&p.x>-34)||i==3;}
                // Kept stones and stems in the raked islands' grit.
                foreach(var (x0,x1,z0,z1) in new[]{(-34f,36f,35f,51f),(-48f,36f,93f,109f)})
                    for(float z=z0+1.5f;z<z1-1;z+=2.6f)for(float x=x0+1;x<x1-1;x+=4.2f)
                    {
                        var p=new Vector2(x+(R()-.5f)*2.6f,z+(R()-.5f)*1.6f);bool stem=R()<.35f;float r=stem?.05f:.28f+.22f*R();
                        if(!Raked(p)||!open(p,r+.35f)||placed.Any(q=>Vector2.Distance(q,p)<2.7f))continue;float y=Drawn(p);placed.Add(p);
                        if(stem)
                        {
                            var lean=(new Vector3(0,1,.16f)+new Vector3(R()-.5f,0,R()-.5f)*.1f).normalized;float tall=.55f+.4f*R();
                            Crystal(kept.Slot(1),new Vector3(p.x,y-.05f,p.y),lean,tall,.03f);kept.Slot(1).Lump(new Vector3(p.x,y,p.y)+lean*tall*.86f,new Vector3(.05f,.07f,.05f),stems,.2f,Color.white,Color.white,6,4);
                            RakedRings(rings,p,.2f,3);stems++;
                        }
                        else
                        {
                            float h=.16f+.18f*R();kept.Slot(0).Lump(new Vector3(p.x,y+h*.3f,p.y),new Vector3(r,h,r*(.75f+.3f*R())),stones+41,.22f,new Color(.92f,.88f,.8f),new Color(.6f,.58f,.56f),10,5);
                            RakedRings(rings,p,r+.2f,3);stones++;
                        }
                    }
                // The stair gardens: raked in rows of fans across all their open ground, on a quarter-metre drape of the floor.
                const float Cell=.25f;int laid=0;
                for(float z=121;z<137;z+=Cell)for(float x=-56;x<30;x+=Cell)
                {
                    var c=new Vector2(x+Cell*.5f,z+Cell*.5f);if(IsleOf(c)!=4||Nearest(c,out _,out _)>-1.3f||NearWay(c,1.8f)||Opening(c,1)>0)continue;
                    if(Physics.CheckSphere(new Vector3(c.x,Drawn(c)+.5f,c.y),.3f,~0,QueryTriggerInteraction.Ignore)&&Physics.OverlapSphere(new Vector3(c.x,Drawn(c)+.5f,c.y),.3f,~0,QueryTriggerInteraction.Ignore).Any(o=>!(o is MeshCollider)))continue;
                    Vector3 V(float px,float pz)=>new(px,Drawn(new Vector2(px,pz))+.018f,pz);
                    fans.Quad(V(x,z),V(x+Cell,z),V(x+Cell,z+Cell),V(x,z+Cell),Vector3.up,Color.white,.3f);laid++;
                }
                kit.Built("Raked rings",rings,Soil);kit.Built("Kept stones and stems",kept,Kerb,GlassDeep);kit.Built("The stair gardens' raking",fans,Waves);
                Debug.Log($"SKYFIELDS_GARDEN_GROUND stones={stones} stems={stems} fans={laid}");
                // Plumb lines on the beds: a stake, an arm, and a bob on its cord that hangs toward the nearest fibre's foot.
                var plumbs=new ZoneKit.Shape();
                foreach(var (x,z,top) in new[]{(-19.4f,45.4f,.12f),(-8.6f,49.4f,.12f),(-30.6f,75.4f,.12f),(-26.6f,105.6f,.12f),(-40.4f,124.4f,.12f),(-13.6f,134f,.9f),(2.6f,123.6f,.5f)})
                {
                    var foot=new Vector3(x,Drawn(new Vector2(x,z))+top,z);
                    var fibre=pulls.motes.Select(m=>m.foot).OrderBy(f=>Vector2.Distance(new Vector2(f.x,f.z),new Vector2(x,z))).FirstOrDefault();
                    var lean=fibre-foot;lean.y=0;lean=lean.sqrMagnitude>.01f?lean.normalized:Vector3.forward;
                    var tip=foot+Vector3.up*1.2f+lean*.38f;
                    plumbs.Slot(0).Post(foot+Vector3.down*.05f,1.28f,.025f,.02f,6,new Color(.62f,.52f,.38f),new Color(.8f,.7f,.54f)).Beam(foot+Vector3.up*1.2f,tip,.04f,.04f,new Color(.7f,.6f,.44f));
                    var bob=tip+Vector3.down*.5f+lean*.34f;plumbs.Slot(0).Tube(new[]{tip,bob},new[]{.007f,.007f},4,new Color(.86f,.82f,.72f),new Color(.86f,.82f,.72f),1,.5f,false);
                    plumbs.Slot(1).Lump(bob+lean*.03f,new Vector3(.05f,.08f,.05f),(int)x,.1f,BronzePale,BronzeDark,8,5);
                }
                kit.Built("Gardeners' plumb lines",plumbs,Plank,Bronze);
            }
            /// <summary>The fibre fields' ground: glass roots running out over the floor from every glass tree and from the floor fibres' collars, lit from inside, with their light on
            /// the stone round them; more glass breaking the paving.</summary>
            static void GlassGround(ZoneKit kit)
            {
                var roots=new ZoneKit.Shape();var light=new ZoneKit.Shape();var rng=new System.Random(1401);float R()=>(float)rng.NextDouble();int made=0;
                var from=new List<Vector2>{new(-36,77.6f),new(26,77.4f),new(-20,77.8f),new(-12,77.4f),new(4,77.6f),new(-40,64.6f),new(18,64.8f),new(34,66)};
                foreach(var (fight,a,b) in FloorFibres)if(fight=="Sky_Wisps"){from.Add(a);from.Add(b);}
                for(float x=-38;x<36;x+=7){from.Add(new Vector2(x+R()*3,66+R()*2));from.Add(new Vector2(x+R()*3,75+R()*2));}
                // Glass broken up through the paving in knots, off the walk and out of the fight.
                var open=OpenGround();var knots=new ZoneKit.Shape();int knotted=0;
                for(int k=0;k<60;k++){var p=new Vector2(Mathf.Lerp(-40,36,R()),Mathf.Lerp(64,78,R()));if(IsleOf(p)!=2||!open(p,.2f))continue;Cluster(knots,new Vector3(p.x,Drawn(p)-.05f,p.y),Vector3.up,.35f+R()*.45f,rng);knotted++;}
                kit.Built("Glass knots in the fibre fields",knots,Glass);Debug.Log("SKYFIELDS_GLASS_KNOTS "+knotted);
                foreach(var start in from)
                {
                    int count=3+rng.Next(3);
                    for(int k=0;k<count;k++)
                    {
                        float heading=R()*Mathf.PI*2;var p=start;var path=new List<Vector3>();var radii=new List<float>();float length=2+R()*3.2f;int steps=Mathf.CeilToInt(length/.35f);
                        for(int s=0;s<=steps;s++)
                        {
                            if(Nearest(p,out _,out _)>-(RimWide+.3f))break;
                            path.Add(new Vector3(p.x,Drawn(p)+.04f,p.y));radii.Add(Mathf.Lerp(.13f,.025f,s/(float)steps));
                            heading+=(R()-.5f)*.7f;p+=new Vector2(Mathf.Cos(heading),Mathf.Sin(heading))*.35f;
                        }
                        if(path.Count<3)continue;
                        roots.Tube(path,radii,5,Color.white,Color.white,1,.5f,false);made++;
                        for(int s=0;s<path.Count;s+=2)Pool(light,path[s]+Vector3.down*.02f,Vector3.up,.7f,new Color(.45f,.9f,1,.26f));
                    }
                }
                var drawn=kit.Built("Glass roots",roots,GlassDeep);drawn.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                kit.Built("Glass roots' light",light,FibreLight);Debug.Log("SKYFIELDS_GLASS_ROOTS "+made);
            }
            /// <summary>The forecourt, the oldest and grandest stone: a great medallion of the court let into the paving between the door's two fibres, bronze rings and spokes in dark
            /// stone, and a processional band of dark stone edged in bronze from it to the door, set with small rings.</summary>
            static void ForecourtInlay(ZoneKit kit)
            {
                var inlay=new ZoneKit.Shape();inlay.Slot(1);var c=new Vector2(4,156.6f);
                float Top(Vector2 p,float r)=>Enumerable.Range(0,12).Select(k=>Drawn(p+new Vector2(Mathf.Cos(k*.52f),Mathf.Sin(k*.52f))*r)).Append(Drawn(p)).Max()+.012f;
                float y=Top(c,3.3f);var at=new Vector3(c.x,y,c.y);
                var dark=new Color(.6f,.54f,.46f);
                inlay.Slot(0).Disc(at,Vector3.up,3.3f,48,dark,2.75f).Disc(at,Vector3.up,1.25f,32,dark);
                inlay.Slot(1).Disc(at+Vector3.up*.003f,Vector3.up,3.4f,48,BronzePale,3.28f).Disc(at+Vector3.up*.003f,Vector3.up,2.8f,48,BronzePale,2.68f).Disc(at+Vector3.up*.003f,Vector3.up,1.3f,32,BronzePale,1.18f)
                    .Disc(at+Vector3.up*.004f,Vector3.up,.5f,20,BronzePale,.38f);
                for(int k=0;k<12;k++){float a=k*Mathf.PI/6;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));inlay.Slot(1).Beam(at+dir*1.3f+Vector3.up*.003f,at+dir*2.68f+Vector3.up*.003f,.08f,.01f,BronzePale);}
                // The band to the door.
                float z0=c.y+3.4f,z1=DoorAt.y-.4f;
                for(float z=z0;z<z1;z+=1.2f)
                {
                    float zn=Mathf.Min(z1,z+1.15f);float yz=Mathf.Max(Top(new Vector2(c.x,z),1.6f),Top(new Vector2(c.x,zn),1.6f));
                    inlay.Slot(0).Quad(new Vector3(c.x-1.5f,yz,z),new Vector3(c.x+1.5f,yz,z),new Vector3(c.x+1.5f,yz,zn),new Vector3(c.x-1.5f,yz,zn),Vector3.up,Color.Lerp(dark,new Color(.66f,.6f,.5f),(z-z0)%2.4f<1.2f?0:1),.5f);
                    foreach(int s in new[]{-1,1})inlay.Slot(1).Beam(new Vector3(c.x+s*1.58f,yz+.003f,z),new Vector3(c.x+s*1.58f,yz+.003f,zn),.1f,.01f,BronzePale);
                    inlay.Slot(1).Disc(new Vector3(c.x,yz+.004f,(z+zn)*.5f),Vector3.up,.32f,16,BronzePale,.24f);
                }
                // Small rings of bronze set in the flags all over the court, and a border of dark stone a stride in from its rim.
                for(float z=150.5f;z<168;z+=3.6f)for(float x=-15;x<28;x+=3.6f)
                {
                    var p=new Vector2(x+(Mathf.RoundToInt(z)%2==0?1.8f:0),z);if(IsleOf(p)!=5||Nearest(p,out _,out _)>-2.6f||Vector2.Distance(p,c)<4||Mathf.Abs(p.x-c.x)<2.1f&&p.y>c.y)continue;
                    if(FloorFibres.Any(f=>f.fight=="Sky_Door"&&GroundKit.Segment(p,f.from,f.to)<.6f))continue;
                    float yp=Top(p,.4f);inlay.Slot(1).Disc(new Vector3(p.x,yp+.003f,p.y),Vector3.up,.36f,18,BronzePale,.27f);
                }
                foreach(var line in ZoneKit.Contours(p=>Shore(p,5),-2.2f,-22,34,145,173,.5f))
                    for(int k=1;k<line.Count;k++)
                    {
                        var a=line[k-1];var b=line[k];var mid=(a+b)*.5f;if(Opening(mid,2)>0||Mathf.Abs(mid.x-c.x)<4.2f&&mid.y>164||Mathf.Abs(mid.x-LongFrom.x)<2.6f&&Mathf.Abs(mid.y-LongFrom.y)<2.6f)continue;
                        var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*.5f;float ya=Top(a,.6f),yb=Top(b,.6f);
                        inlay.Slot(0).Quad(new Vector3(a.x-n.x,ya,a.y-n.y),new Vector3(a.x+n.x,ya,a.y+n.y),new Vector3(b.x+n.x,yb,b.y+n.y),new Vector3(b.x-n.x,yb,b.y-n.y),Vector3.up,dark,.5f);
                    }
                kit.Built("The forecourt's inlay",inlay,Kerb,Bronze);
            }
            /// <summary>The highest island: the court's old stone left to itself. A broken ring of bronze round the lantern's seat, slabs lifted by what grew under them, gold
            /// lichen on the flags and pale grass in their joints.</summary>
            static void OldStone(ZoneKit kit)
            {
                var old=new ZoneKit.Shape();old.Slot(2);var rng=new System.Random(1501);float R()=>(float)rng.NextDouble();var seat=LanternSeat;
                var prompts=new[]{SeatHand,HighCache,new Vector2(-56,154.5f)};bool Clear(Vector2 p,float r)=>IsleOf(p)==6&&Nearest(p,out _,out _)<-1.4f&&prompts.All(q=>Vector2.Distance(q,p)>1.4f+r);
                foreach(float radius in new[]{2.3f,2.65f})
                    for(float a=0;a<Mathf.PI*2;a+=.22f)
                    {
                        if(R()<.3f)continue;var p=seat+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;var q=seat+new Vector2(Mathf.Cos(a+.2f),Mathf.Sin(a+.2f))*radius;if(!Clear(p,0)||!Clear(q,0))continue;
                        old.Slot(1).Beam(new Vector3(p.x,Drawn(p)+.012f,p.y),new Vector3(q.x,Drawn(q)+.012f,q.y),.09f,.012f,BronzePale);
                    }
                kit.Built("The highest island's broken ring",old,Kerb,Bronze,Blade);
                // Lifted slabs, each its own piece, with grass come up round them (the lichen, the cracks and the rest of the grass are the ground kit's).
                foreach(var (x,z) in new[]{(-51.5f,152.6f),(-48.8f,157.8f),(-58.6f,161.4f),(-69.6f,158.2f),(-54.6f,158.6f),(-66.4f,151.6f),(-60.4f,151.2f),(-53.4f,150.9f),(-47.2f,152.2f),(-63.2f,154.4f)})
                {
                    var p=new Vector2(x,z);if(!Clear(p,.9f))continue;var slab=new ZoneKit.Shape();slab.Slot(1);
                    slab.Slot(0).Box(new Vector3(x,Drawn(p)+.08f,z),new Vector3(1.5f,.16f,1.1f),Quaternion.Euler(4+R()*5,R()*90,R()*6-3),StoneShade,new Color(.96f,.9f,.78f));
                    for(int c=0;c<3;c++)
                    {
                        float a=R()*Mathf.PI*2;var root=new Vector3(x+Mathf.Cos(a)*.95f,Drawn(p),z+Mathf.Sin(a)*.75f);
                        for(int b=0;b<8;b++)slab.Slot(1).Frond(root+new Vector3((R()-.5f)*.2f,0,(R()-.5f)*.2f),Quaternion.Euler(0,R()*360,0)*Vector3.forward,.28f+.14f*R(),.045f,.34f,.08f,new Color(.6f,.6f,.36f),new Color(.95f,.9f,.6f),3);
                    }
                    kit.Built("Lifted slab "+Mathf.RoundToInt(x)+" "+Mathf.RoundToInt(z),slab,Kerb,Blade);
                }
            }
            /// <summary>A gardener at work, and what it works with: standing, a rake held upright in its hand (a broom on the swept stair); kneeling, at a stone in the grit, its rake
            /// laid by. Kneeling is posed once here from its idle and keeps no animator, so nothing stands it back up.</summary>
            static GameObject GardenerAtWork(ZoneKit kit,Vector3 at,float yaw,Color tint,bool kneel=false,bool broom=false,float raised=0)
            {
                var person=kit.Townsperson("Gardener",at,yaw,tint);if(raised>0)person.transform.position+=Vector3.up*raised;
                var model=person.transform.Find("Generated civilian");var rig=model!=null?model.GetComponentInChildren<Animator>():null;
                if(rig==null||!rig.isHuman)return person;
                if(kneel&&PrefabUtility.IsPartOfPrefabInstance(model.gameObject)){PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(model.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);rig=model.GetComponentInChildren<Animator>();}
                var idle=rig.runtimeAnimatorController!=null?rig.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle")):null;
                if(idle!=null)idle.SampleAnimation(rig.gameObject,.3f);
                var f=person.transform.forward;var right=person.transform.right;float ground=person.transform.position.y;
                var tool=Tool(kit,broom,person.name+" "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.z));
                if(!kneel)
                {
                    var hand=rig.GetBoneTransform(HumanBodyBones.RightHand);if(hand==null)return person;
                    tool.transform.SetPositionAndRotation(new Vector3(hand.position.x,ground+.02f,hand.position.z)+f*.04f,Quaternion.LookRotation(f));tool.transform.SetParent(hand,true);
                    return person;
                }
                Transform B(HumanBodyBones b)=>rig.GetBoneTransform(b);
                var hips=B(HumanBodyBones.Hips);var lUp=B(HumanBodyBones.LeftUpperLeg);var lLow=B(HumanBodyBones.LeftLowerLeg);
                if(hips==null||lUp==null||lLow==null)return person;
                float thigh=Vector3.Distance(lUp.position,lLow.position);hips.position+=Vector3.down*(lUp.position.y-(ground+thigh+.07f))+f*.06f;
                Aim(lUp,lLow,-Vector3.up+f*.12f);Aim(lLow,B(HumanBodyBones.LeftFoot),-f-Vector3.up*.12f);Aim(B(HumanBodyBones.LeftFoot),B(HumanBodyBones.LeftToes),-Vector3.up*.8f-f*.6f);
                Aim(B(HumanBodyBones.RightUpperLeg),B(HumanBodyBones.RightLowerLeg),f-Vector3.up*.1f);Aim(B(HumanBodyBones.RightLowerLeg),B(HumanBodyBones.RightFoot),-Vector3.up+f*.06f);Aim(B(HumanBodyBones.RightFoot),B(HumanBodyBones.RightToes),f);
                Aim(B(HumanBodyBones.Spine),B(HumanBodyBones.Chest),Vector3.up+f*.42f);Aim(B(HumanBodyBones.Chest),B(HumanBodyBones.Neck),Vector3.up+f*.32f);Aim(B(HumanBodyBones.Neck),B(HumanBodyBones.Head),Vector3.up+f*.75f);
                Aim(B(HumanBodyBones.RightUpperArm),B(HumanBodyBones.RightLowerArm),-Vector3.up+f*.5f+right*.15f);Aim(B(HumanBodyBones.RightLowerArm),B(HumanBodyBones.RightHand),f*.9f-Vector3.up*.2f-right*.18f);
                Aim(B(HumanBodyBones.LeftUpperArm),B(HumanBodyBones.LeftLowerArm),-Vector3.up*.8f+f*.62f-right*.12f);Aim(B(HumanBodyBones.LeftLowerArm),B(HumanBodyBones.LeftHand),-Vector3.up*.75f+f*.66f);
                UnityEngine.Object.DestroyImmediate(rig);
                // What it kneels to: a kept stone in the grit in front of it, its rings half raked.
                var stoneAt=new Vector2(person.transform.position.x+f.x*.95f,person.transform.position.z+f.z*.95f);float top=person.transform.position.y;var kept=new ZoneKit.Shape();kept.Slot(1);
                kept.Slot(0).Lump(new Vector3(stoneAt.x,top+.05f,stoneAt.y),new Vector3(.2f,.15f,.16f),(int)(at.x*7),.22f,new Color(.92f,.88f,.8f),new Color(.6f,.58f,.56f),10,5);
                RakedRings(kept.Slot(1),stoneAt,.34f,2,top);kit.Built("Kept stone "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.z),kept,Kerb,Soil);
                // The rake laid by on the ground at its side.
                tool.transform.SetPositionAndRotation(person.transform.position-right*.7f+Vector3.up*.03f-f*.6f,Quaternion.FromToRotation(Vector3.up,f));
                tool.transform.SetParent(person.transform,true);
                return person;
            }
            static void Aim(Transform bone,Transform child,Vector3 dir)
            {
                if(bone==null||child==null||dir.sqrMagnitude<1e-6f)return;var now=child.position-bone.position;if(now.sqrMagnitude<1e-6f)return;
                bone.rotation=Quaternion.FromToRotation(now.normalized,dir.normalized)*bone.rotation;
            }
            /// <summary>A gardener's rake (or broom): a pole standing on its head, the head's tines (or bristles) to the front. Its own mesh, at the origin, for a hand to carry.</summary>
            static GameObject Tool(ZoneKit kit,bool broom,string whose)
            {
                var shape=new ZoneKit.Shape();var wood=new Color(.66f,.54f,.38f);
                shape.Slot(0).Post(Vector3.zero+Vector3.up*.06f,1.7f,.02f,.018f,6,wood,ZoneKit.Shape.Dim(wood,1.15f));
                if(broom)shape.Slot(1).Lump(new Vector3(0,.16f,0),new Vector3(.2f,.18f,.07f),3,.2f,new Color(.86f,.74f,.44f),new Color(.6f,.5f,.3f),8,5);
                else
                {
                    shape.Slot(1).Beam(new Vector3(-.26f,.1f,0),new Vector3(.26f,.1f,0),.035f,.04f,BronzeDark,BronzePale);
                    for(int k=0;k<9;k++){float x=-.24f+k*.06f;shape.Slot(1).Beam(new Vector3(x,.1f,0),new Vector3(x,.01f,.05f),.012f,.012f,BronzeDark);}
                }
                var drawn=kit.Built("Gardener's "+(broom?"broom":"rake")+" "+whose,shape,Plank,broom?Cloth:Bronze);
                drawn.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;return drawn;
            }
            /// <summary>Islets: rocks the size of a house hung in the void on their fibres, out at the sides and low in the gaps, where none comes between the camera and a walk.</summary>
            static void Islets(ZoneKit kit,System.Random rng)
            {
                float R()=>(float)rng.NextDouble();var rock=new ZoneKit.Shape();var core=new ZoneKit.Shape();var glow=new ZoneKit.Shape();int made=0;
                foreach(var (x,y,z,r) in new[]{(-74f,4f,30f,4f),(-70f,-2f,72f,5f),(-78f,10f,118f,3.6f),(44f,2f,10f,3.4f),(48f,8f,58f,4.6f),(46f,-1f,104f,3.8f),(42f,14f,150f,4.2f),(-30f,-4f,174f,3.2f),(-60f,-3f,26f,2.6f),(10f,-5f,57f,2.4f),(-14f,1f,87f,2.6f),(0f,4f,116f,2.2f),(36f,6f,180f,5f),(-70f,26f,176f,3f)})
                {
                    var c=new Vector3(x,y,z);
                    rock.Slot(0).Lump(c,new Vector3(r*1.3f,r*.4f,r),made*7+1,.25f,new Color(.9f,.84f,.68f),new Color(.66f,.62f,.56f),12,6);
                    rock.Slot(1).Lump(c+Vector3.down*r*.6f,new Vector3(r*.9f,r*.9f,r*.8f),made*7+3,.4f,new Color(.74f,.68f,.6f),new Color(.5f,.48f,.46f),10,6);
                    rock.Slot(1).Lump(c+Vector3.down*r*1.4f,new Vector3(r*.4f,r*.7f,r*.4f),made*7+5,.4f,new Color(.64f,.6f,.56f),new Color(.46f,.44f,.42f),8,5);
                    // Its fibre runs through it and on, both ways: it floats because the thread does.
                    var along=Quaternion.Euler(0,R()*360,0)*Vector3.forward;Thread(core,glow,c-along*(r+10)+Vector3.down*6,c+Vector3.up*.2f,1.5f,.035f);Thread(core,glow,c+Vector3.up*.2f,c+along*(r+12)+Vector3.down*8,1.5f,.035f);
                    made++;
                }
                kit.Built("Islets",rock,Stone,Cliff);kit.Built("Islets' fibres",core,Fibre);kit.Built("Islets' fibres glow",glow,FibreSoft);
            }
            /// <summary>The Glass Choir: the court's front in pale stone behind the door, buttressed, with a great window of glass; and the bridled Choir's body beyond it, ribbed and
            /// translucent, rising out of the cloud and running away north and east, the light inside it still.</summary>
            static void Choir(ZoneKit kit)
            {
                var front=new ZoneKit.Shape();float y0=CloudLevel-2,top=Upper+22;
                front.Slot(0).Box(new Vector3(4,(y0+top)*.5f,173.5f),new Vector3(30,top-y0,4),Quaternion.identity,StoneShade,StonePale);
                foreach(float x in new[]{-13f,-3f,11f,21f})front.Slot(0).Box(new Vector3(x,(y0+top+4)*.5f,171.4f),new Vector3(2.2f,top-y0+4,2.6f),Quaternion.identity,StoneShade,StonePale);
                front.Slot(0).Box(new Vector3(4,top+2.5f,173.5f),new Vector3(32,1.2f,5),Quaternion.identity,StonePale);
                // The window over the door: the Choir's light behind bronze tracery, and its glass pushing out through it.
                var win=new Vector3(4,Upper+16,171.44f);var light=new ZoneKit.Shape();
                for(int k=0;k<5;k++){float lo=-5+k*2,hi=lo+2;float b=1-k*.06f;light.Quad(win+new Vector3(-3.4f,lo,0),win+new Vector3(3.4f,lo,0),win+new Vector3(3.4f,hi,0),win+new Vector3(-3.4f,hi,0),Vector3.back,new Color(.6f*b,.84f*b,1*b),.2f);}
                front.Slot(2).Beam(win+new Vector3(0,-5,-.12f),win+new Vector3(0,5,-.12f),.22f,.22f,BronzePale);for(int k=0;k<4;k++)front.Slot(2).Beam(win+new Vector3(-3.4f,-4+k*2.6f,-.12f),win+new Vector3(3.4f,-4+k*2.6f,-.12f),.18f,.18f,BronzePale);
                kit.Built("The Choir's front",front,Kerb,GlassDeep,Bronze);
                // The glass has come through the front: bursts of it out of the masonry between the buttresses, with the light inside showing along the cracks it has made, a
                // mass of it over the cornice running down the face, and the body's ribs reaching over the top and gripping it.
                var glass=new ZoneKit.Shape();var ribs=new ZoneKit.Shape();var rng=new System.Random(173);float R()=>(float)rng.NextDouble();
                var bursts=new[]{new Vector3(-8,Upper+3.5f,171.4f),new Vector3(16,Upper+5.5f,171.4f),new Vector3(-1.3f,Upper+5,171.4f),new Vector3(9.3f,Upper+3.5f,171.4f),new Vector3(-1.4f,Upper+12,171.4f),new Vector3(4,Upper+16,171.3f),new Vector3(-8.5f,Upper+16,171.4f),new Vector3(16.5f,Upper+19,171.4f),new Vector3(-6,Upper+24,171.4f)};
                foreach(var b in bursts)
                {
                    Cluster(glass,b,new Vector3((R()-.5f)*.6f,(R()-.3f)*.6f,-1),2.2f+R()*1.8f,rng);
                    // The cracks it opened, lit from inside.
                    for(int k=0;k<6;k++)
                    {
                        float a=R()*Mathf.PI*2;var p=b+new Vector3(0,0,-.03f);var dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);var path=new List<Vector3>{p};
                        for(int s=0;s<5;s++){dir=(dir+new Vector3((R()-.5f)*.8f,(R()-.5f)*.8f,0)).normalized;p+=dir*(.5f+R()*.6f);path.Add(p);}
                        for(int s=1;s<path.Count;s++){float w=.14f*(1-s/(float)path.Count)+.03f;var side=Vector3.Cross(path[s]-path[s-1],Vector3.back).normalized*w;light.Quad(path[s-1]-side,path[s-1]+side,path[s]+side,path[s]-side,Vector3.back,new Color(.62f,.88f,1),.2f);}
                    }
                }
                for(float x=-11;x<20;x+=2.2f+R()*1.6f)
                {
                    var lip=new Vector3(x,top+3,171.2f);Crystal(glass,lip+Vector3.up*.4f,(Vector3.down*1.6f+Vector3.back*.5f+Vector3.right*(R()-.5f)).normalized,3+R()*5,.35f+R()*.3f);
                    Cluster(glass,new Vector3(x,top+3.2f,172.5f+R()*2),new Vector3((R()-.5f)*.8f,1,-.4f),3+R()*4,rng);
                }
                foreach(var (x,reach) in new[]{(-9f,9f),(5f,11f),(17f,8f)})
                {
                    var path=new List<Vector3>();var radii=new List<float>();
                    for(int k=0;k<=12;k++){float t=k/12f;float a=t*Mathf.PI*.95f;path.Add(new Vector3(x+(R()-.5f)*.4f,top+2+Mathf.Sin(a)*reach*.7f,182-Mathf.Sin(a*.5f+.2f)*(182-170.4f)-(t>.8f?(t-.8f)*2:0)));radii.Add(Mathf.Lerp(1.1f,.45f,t));}
                    path[^1]=new Vector3(path[^1].x,top-reach*.5f,170.6f);ribs.Tube(path,radii,10,Color.white,Color.white,1,.3f,false);
                }
                var glassBody=kit.Built("Glass through the Choir's front",glass,ChoirGlass);glassBody.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                kit.Built("The Choir's ribs over its front",ribs,ChoirRib).GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                kit.Built("The Choir's light through its front",light,InnerLight);
                // The body: a long glass back in the cloud, its ribs standing out of it.
                var body=new ZoneKit.Shape();
                for(int k=0;k<7;k++){float t=k/6f;var c=new Vector3(Mathf.Lerp(-10,46,t),Mathf.Lerp(8,2,t)+Mathf.Sin(t*3)*3,Mathf.Lerp(196,228,t));body.Slot(0).Lump(c,new Vector3(16-6*t,14-6*t,13),k*11+3,.12f,Color.white,new Color(.7f,.8f,.86f),14,8);}
                kit.Built("The Choir's body",body,ChoirGlass);
                int count=0;
                foreach(var (x,z,size,yaw) in new[]{(-18f,190f,26f,80f),(-4f,196f,30f,84f),(12f,202f,30f,88f),(28f,208f,26f,92f),(42f,214f,22f,96f)})
                {var rib=InGlass(kit.Prop(ZoneKit.Kit("GlassRib","RibArch"),new Vector3(x,CloudLevel-4,z),size,yaw,false,false),ChoirRib);rib.name="The Choir's rib";count++;}
                // The stays: the Bridle's fibres from the front to the body, taut.
                var core=new ZoneKit.Shape();var glow=new ZoneKit.Shape();
                foreach(float x in new[]{-10f,4f,18f}){Thread(core,glow,new Vector3(x,top-2,175.6f),new Vector3(x+6,top+6,196),0,.07f);}
                kit.Built("The Bridle's stays",core,Fibre);kit.Built("The Bridle's stays glow",glow,FibreSoft);
                kit.Lamp(new Vector3(4,4,168),12,1.2f,new Color(.8f,.92f,1));kit.Lamp(new Vector3(4,10,169.6f),14,1.4f,new Color(.62f,.86f,1));
                Debug.Log("SKYFIELDS_CHOIR ribs="+count);
            }

            /// <summary>The ground kit's recipe, last, once everything stands: the court's paving worn along the walks, broken stone at every rim, gold dust drifted in the lee of
            /// walls, cracks, lichen gold and rust, tufts of pale grass where the paving has gone, glass shards where fibres fray, and cloud shade moving over all of it.</summary>
            static void Ground(ZoneKit kit)
            {
                var ground=GroundKit.Open(kit.zone,GroundKit.FindFloor(),71,High+1,true,v=>v.y>Camp-2);
                float Smooth(float a,float b,float x)=>GroundKit.Smooth(a,b,x);
                float On(Vector2 p)=>Nearest(p,out _,out _)<-RimWide?1:0;
                float Rim(Vector2 p)=>Smooth(-4,-1,Nearest(p,out _,out _));
                float Is(Vector2 p,int isle)=>On(p)*(IsleOf(p)==isle?1:0);
                float Way(Vector2 p){float d=float.PositiveInfinity;foreach(var w in Ways)d=Mathf.Min(d,ZoneKit.FromPath(p,w));return d;}
                // The camp is trodden; the gardens raked; the fibre fields grown through with glass; the forecourt and the highest island the court's old stone.
                float Lived(Vector2 p)=>Is(p,0);float Raked(Vector2 p)=>On(p)*(IsleOf(p)==1&&p.x>-34||IsleOf(p)==3?1:0);
                float Fields(Vector2 p)=>Is(p,2)+Is(p,1)*Smooth(-33,-38,p.x);float Court(Vector2 p)=>Is(p,5)+Is(p,6);
                foreach(var way in Ways)ground.ClearLane(way,1.5f);
                ground.Look("Canyon",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Oriel/skyfields-cliff.png"));
                // Every island is the court's paving, laid in courses; the two layers that are paving (worn, glass-grown) are its own flags at its own 8 m, so their joints meet the floor's.
                ground.Layers(new Vector4(.05f,.34f,.4f,.28f),1.1f,new GroundKit.Layer("skyfields-worn",new Color(1,1,1),8),new GroundKit.Layer("skyfields-raked",new Color(.98f,.96f,.9f),4),new GroundKit.Layer("skyfields-glassfield",new Color(.96f,.98f,1),8));
                // The camp: the same flags trodden and dusty where it is lived on and along its walk; clean where nobody goes. Its dust drifts against the rims and what stands.
                ground.Paint(0,p=>Lived(p)*(1-.75f*Rim(p))*Smooth(.3f,.5f,ground.Noise(p,5,7)));
                foreach(var way in Ways)ground.PaintPath(0,way,2.4f,.9f,.3f,.8f,(int)way[0].x+40);
                ground.Erase(0,p=>1-Lived(p));
                // The gardens: raked grit over all the open ground, the swept path of the court's flags left clear through it.
                ground.Paint(1,p=>Raked(p)*Smooth(1.7f,1.85f,Way(p)));
                // The fibre fields and the glass grove: the stone grown through with glass, patchy, thinner along the walk.
                ground.Paint(2,p=>Fields(p)*(.3f+.7f*Smooth(1.2f,2.6f,Way(p)))*Smooth(.32f,.46f,ground.Noise(p,6,17)));
                ground.Shade(p=>.08f*Rim(p));
                // The camp's wear: feet along the walk, a scorched round under the kettle, litter and stains, dust drifted at the rims.
                foreach(var way in new[]{Ways[0]})ground.Strip("Tracks",way,0,2.2f,new Color(.46f,.38f,.28f,.8f));
                ground.Decals(new[]{"SandDrift","Stain"},6,p=>Lived(p)*(1-Rim(p)),1.6f,3.4f,new Color(.84f,.76f,.62f,.6f),false,.1f,57);
                ground.Decal("Scorch",new Vector2(-14,17.2f),1.8f,20,new Color(.3f,.24f,.2f,.8f),false);
                ground.Decals(new[]{"Litter"},4,p=>Lived(p)*(1-Rim(p)),.5f,1,new Color(.82f,.74f,.6f),false,.1f,37);
                ground.Decals(new[]{"Stain"},4,p=>Lived(p),1.6f,3,new Color(.56f,.48f,.4f,.5f),false,.1f,61);
                ground.Decals(new[]{"CracksA","CracksB"},5,p=>Lived(p)+Court(p)*.5f,1.4f,3,new Color(.56f,.5f,.42f,.9f));
                ground.Decals(new[]{"SandDrift"},3,p=>(Lived(p)+Court(p))*(.3f+.7f*Rim(p)),1.6f,3.2f,new Color(.94f,.9f,.8f,.5f),false,.1f,53);
                // The fibre fields: cracks run full of glass, shards through the paving, and the light in the glass lying on the stone.
                ground.Decals(new[]{"CracksA","CracksB"},9,p=>Fields(p),1.4f,3.2f,new Color(.78f,.95f,1,.95f),true,.1f,67);
                ground.Cover(new[]{"Shards"},34,p=>Fields(p)*Smooth(.36f,.56f,ground.Noise(p,4,19)),.16f,.34f,new Color(.82f,.96f,1),3);
                ground.Glows(26,p=>Fields(p)*Smooth(.4f,.58f,ground.Noise(p,5,29)),.6f,1.6f,new Color(.3f,.62f,.7f,.6f),71);
                // The court's old stone: gold lichen in its joints, now and then a slab lifted.
                ground.Decals(new[]{"Lichen"},6,p=>Court(p)*Smooth(.42f,.6f,ground.Noise(p,9,43)),1.1f,2.4f,new Color(.82f,.76f,.36f,.8f),false,.1f,43);
                ground.Decals(new[]{"Lichen"},20,p=>Is(p,6)*Smooth(.25f,.45f,ground.Noise(p,5,45)),.8f,2.2f,new Color(.78f,.74f,.34f,.85f),false,.1f,45);
                ground.Decals(new[]{"CracksA","CracksB"},6,p=>Is(p,6),1.2f,2.6f,new Color(.56f,.5f,.42f,.9f),true,.1f,49);
                ground.Decals(new[]{"Flagstones","Slabs"},2,p=>Is(p,6),1.4f,2.6f,new Color(1,.92f,.78f));
                // Grass and white flowers only where nobody keeps the ground: the camp's rims and the highest island.
                ground.Cover(new[]{"TuftA","TuftB","TuftC","Sprouts"},26,p=>(Lived(p)*Rim(p)+Is(p,6)*.8f)*Smooth(.4f,.58f,ground.Noise(p,6,9)),.16f,.36f,new Color(.92f,.86f,.56f),3);
                ground.Cover(new[]{"Flowers"},4,p=>Is(p,6)*Smooth(.55f,.7f,ground.Noise(p,10,23)),.16f,.28f,new Color(1,.98f,.92f),2);
                ground.Rocks(1.6f,p=>(Lived(p)+Court(p)+Fields(p))*Rim(p),.08f,.22f,new Color(.92f,.86f,.74f));
                ground.MovingShade("cloud",26,new Vector2(.22f,.1f),.34f,.5f,.04f);
                ground.Finish();
            }

            /// <summary>LookDev's stills (registered in LookDev.Views): each island as it is walked, the camp's lesson, a lift, the junctions, the gardens, the door, and wide views.</summary>
            internal static LookDev.View[] Views=>new[]
            {
                new LookDev.View("arrival",new Vector3(Arrival.x,Camp,Arrival.y)),new LookDev.View("camp",new Vector3(-12,Camp,14)),new LookDev.View("lantern",new Vector3(8,Camp,14)),
                new LookDev.View("first-lift",new Vector3(24,Camp,15)),new LookDev.View("terrace",new Vector3(18,Terrace,44)),new LookDev.View("walled-garden",new Vector3(-8,Terrace,43)),
                new LookDev.View("junction-1",new Vector3(-28,Terrace,46)),new LookDev.View("grove",new Vector3(-46,Terrace,43)),new LookDev.View("fibre-fields",new Vector3(-20,Terrace,71)),
                new LookDev.View("wisps",new Vector3(0,Terrace,70)),new LookDev.View("swept-stair",new Vector3(16,Terrace,71)),new LookDev.View("sagging-gardens",new Vector3(24,Upper,101)),
                new LookDev.View("mixed",new Vector3(6,Upper,100)),new LookDev.View("frayed-garden",new Vector3(-12,Upper,103)),new LookDev.View("junction-2",new Vector3(-36,Upper,104)),
                new LookDev.View("stair-gardens",new Vector3(-28,Upper,129)),new LookDev.View("sealed-garden",new Vector3(-40,Upper,127)),new LookDev.View("junction-3",new Vector3(18,Upper,132)),
                new LookDev.View("high-island",new Vector3(-58,High,155)),new LookDev.View("forecourt",new Vector3(8,Upper,156)),new LookDev.View("door",new Vector3(4,Upper,162)),
                new LookDev.View("overview-low",new Vector3(-8,Camp,34),false,4.4f),new LookDev.View("overview-high",new Vector3(-12,Upper,124),false,4.4f),
            };
        }
    }
}
