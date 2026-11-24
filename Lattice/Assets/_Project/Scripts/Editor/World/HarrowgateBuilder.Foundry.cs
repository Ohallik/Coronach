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
    /// The Bridle Foundry (dungeon: the charging floor, the casting gallery, the heart; rule: hot metal runs on a clock, a marking always says where it will be, and coolant stops it).
    /// Map brief, coordinates and both contracts: docs/maps/BridleFoundry.md. Everything of the zone's own is in <see cref="Foundry"/>, so nothing here can collide with the other
    /// Harrowgate files' members. <c>-foundry-greybox</c> leaves out dressing and the ground kit (the blockout); <c>-foundry-drained</c> and <c>-foundry-hot</c> are for stills only
    /// (both wheels' water stood in place; the runs and two ladles as they are in play) and are never saved as the zone.
    /// </summary>
    public static partial class HarrowgateBuilder
    {
        /// <summary>Under a roof: its own grade, and no sky (the camera clears to the forge's dark).</summary>
        static void FoundryDefinition(ZoneDef z)
        {
            z.hd2dProfile=ArenaBuilder.Asset<Hd2dProfile>("Foundry",p=>{p.tint=new Color(1,.97f,.94f);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.5f;});
            z.skybox=null;
        }
        static void BuildFoundry()=>Foundry.Build();
        /// <summary>The Foundry's light, registered beside its builder. Under a roof with vents in it: a high, warm-neutral key through the vents (enough to model iron and stone, too
        /// weak to bleach the floor), a cool fill so shade on basalt reads as stone and not as one brown, a little smoke. The forge's own light is its fixtures: the furnaces' mouths on
        /// the north walls and the work lamps. Bloom held high: molten metal must never bloom over a danger marking's rim.
        /// Lowered after the whole-game capture (November 20: "it doesn't look like a foundry"; the guide's own example of a place not lit by what it is about): at .95 and a bright
        /// ambient the vents' key lit every floor alike and the forge's fires were paler than the tan floor round them. Now the key and the ambient come down and the room is lit by its
        /// fires, hearths, crucibles and runners; the heroes keep their least light and rim (D217), the markings their own emission.</summary>
        [ZoneLook.Source] static void FoundryLooks()
        {
            // The key is the furnaces' glow, amber, and the ambient the warm dark of a room full of fires; shade goes cool, so iron and stone still model.
            // (Dark enough that the floor's own courses, rivets and clinker stopped reading at all, the first try measured as plain floor nearly everywhere: look_density.)
            // Shade toned slate, not blue (the floor's and the sand's own shadow tint is the furnaces' amber dimmed, D371: against the warm key a neutral or blue tint laid the heroes' shadows lavender on the plate, s48/075):
            // under this warm ambient a blue split turned shadows on the floor lavender, nearest a purple marking of anything in the room
            // (render scoria-lookfix-07, gallery; the round ones there were the cranes' crucibles, which now cast none).
            ZoneLook.Register(FoundryZone,new ZoneLook.Spec{sun=C(1f,.72f,.5f),sunIntensity=1f,sunPitch=56,shadowStrength=.66f,
                sky=C(.27f,.2f,.17f),equator=C(.31f,.21f,.16f),ground=C(.13f,.08f,.06f),
                fog=true,fogColor=C(.17f,.1f,.08f),fogStart=48,fogEnd=170,
                exposure=.2f,contrast=16,saturation=8,splitShadows=C(.42f,.5f,.5f),splitHighlights=C(.7f,.52f,.36f),
                bloomThreshold=1f,bloomIntensity=.6f,vignette=.3f});
        }
        /// <summary>The spawn in the Scoria Flows the Foundry's mouth leads back to: the Flows' engineer stands it at their end of the way.</summary>
        internal const string ScoriaFromFoundry="Foundry";

        internal static class Foundry
        {
            // ---------------------------------------------------------------- the plan
            // x runs east and z north, so north is the back of every view. Three floors walked downward, each a level: the charging floor (0: the mouth, the yard, the wheel hall,
            // the gangway, the charging floor itself, the press run east to the second wheel), the casting gallery (-5: its south room with the second ladle line, the loft off it,
            // Vessa's floor), the heart (-10: the landing, the arena, the dais with the cage). Stairs join them. Troughs are cut a quarter-metre into the floors where metal runs.
            internal const float Charge=0,Cast=-5,Deep=-10,TankFloor=-1.8f,TroughDepth=.25f,TroughHalf=1,Margin=.6f;
            /// <summary>The casting gallery's centre and the heart's (G and C in the two contracts).</summary>
            internal static readonly Vector2 G=new(0,180),C=new(0,251);
            internal const float ArenaRadius=22;
            /// <summary>Plan points a player stands at, named once: the scatter's keep-off, the navigation's must-reach list, LookDev's views, the map brief and a route all want them.</summary>
            internal static readonly Vector2 Arrival=new(0,1),MouthCradle=new(5.5f,4),WayHome=new(0,-6.6f),WheelOne=new(-13.2f,50.4f),WheelOneHand=new(-11.6f,49.4f),
                PressAt=new(26,96),WheelTwo=new(44.2f,100),WheelTwoHand=new(42.4f,99.2f),TankCache=new(38,116),SkipCache=new(-35,30),LoftCache=new(39,150.5f),
                GallerySpawn=new(-3,149),GalleryCradle=new(6.5f,149),HeartSpawn=new(-3,220),HeartCradle=new(7.5f,220),VessaPost=new(0,190),SquadPost=new(0,185),GantryDoor=new(16,201),
                CageAt=new(0,279),AnvilAt=new(-7.5f,275.5f);
            /// <summary>The four mould racks of the gallery contract (centres; 4 m east-west by 1.2, 2.1 tall).</summary>
            internal static readonly Vector2[] Racks={new(-9,174),new(9,174),new(-11,188),new(11,188)};
            /// <summary>Where metal runs: each trough's line (its ends run into the walls) and its half width. A, B the tap runs; L, L2 and L3 the ladle lines.</summary>
            internal static readonly (string name,Vector2 from,Vector2 to)[] Troughs=
                {("A",new(-18.6f,58),new(18.6f,58)),("L",new(-5.6f,73),new(5.6f,73)),("B",new(-19.6f,117),new(-6.4f,117)),("L2",new(-26.6f,141),new(26.6f,141)),("L3",new(28.7f,140.4f),new(28.7f,147.6f))};
            /// <summary>Where a ladle waits off its line, the wall is cut down to a ledge under it (x0, x1, z0, z1, the ledge's height over the floor beside it): the gangway's bay in its east
            /// bunker, and the loft passage's north of it.</summary>
            static readonly (float x0,float x1,float z0,float z1,float cap)[] Bays={(5.6f,12.6f,69,76.6f,1.2f),(25,30.6f,147.6f,153.4f,1.2f)};
            static (string name,Vector2 from,Vector2 to) Trough(string name)=>Troughs.Single(t=>t.name==name);
            static readonly Vector2 Away=new(Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));

            enum Room{Mouth,Yard,SkipGap,Skips,WheelHall,Gangway,Charging,PressRun,WheelBay,TankStair,Tank,StairHead,ChargeStair,GallerySouth,LoftPass,Loft,GalleryDoor,Gallery,HeartStair,Landing,HeartDoor,Dais,SmithsDoor,Arena}
            static readonly (Room room,float x0,float x1,float z0,float z1,float round)[] Boxes=
            {
                (Room.Mouth,-7,7,-8,10,.6f),(Room.Yard,-18,18,9,41,.8f),(Room.SkipGap,-23,-17,21,28,.4f),(Room.Skips,-38,-22,15,33,.8f),
                (Room.WheelHall,-18,18,40,64,.6f),(Room.Gangway,-5,5,63,82,.4f),(Room.Charging,-22,22,81,114,.8f),(Room.PressRun,21,35,92,100,.4f),
                (Room.WheelBay,34,46,86,106,.6f),(Room.TankStair,41,45,104,111,.3f),(Room.Tank,35,45,110,119,.5f),(Room.StairHead,-19,-7,113,121,.4f),
                (Room.ChargeStair,-17,-9,119,136,.3f),(Room.GallerySouth,-26,26,135,155,.8f),(Room.LoftPass,25,31,141,147,.3f),(Room.Loft,30,42,134,154,.6f),
                (Room.GalleryDoor,-4.5f,4.5f,154,159,.3f),(Room.Gallery,-26,26,157,203,.8f),(Room.HeartStair,-4.5f,4.5f,202,218,.3f),(Room.Landing,-12,12,217,225,.5f),
                (Room.HeartDoor,-4.5f,4.5f,224,230,.3f),(Room.Dais,-12,12,270,287,.8f),(Room.SmithsDoor,-2.4f,2.4f,286.5f,293.5f,.3f),
            };
            static float RoundBox(Vector2 p,float x0,float x1,float z0,float z1,float round)
            {
                var d=new Vector2(Mathf.Abs(p.x-(x0+x1)*.5f)-(x1-x0)*.5f+round,Mathf.Abs(p.y-(z0+z1)*.5f)-(z1-z0)*.5f+round);
                return Mathf.Min(Mathf.Max(d.x,d.y),0)+new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude-round;
            }
            /// <summary>Metres outside the nearest floor (negative on one), and which floor. Rooms that overlap meet at one height, so there is no step where they join.</summary>
            static float Nearest(Vector2 p,out Room room)
            {
                float best=Vector2.Distance(p,C)-ArenaRadius;room=Room.Arena;
                foreach(var (r,x0,x1,z0,z1,round) in Boxes){float c=RoundBox(p,x0,x1,z0,z1,round);if(c<best){best=c;room=r;}}
                return best;
            }
            static float Level(Vector2 p,Room room)
            {
                float floor=room switch
                {
                    Room.TankStair=>Mathf.Lerp(Charge,TankFloor,Mathf.Clamp01((p.y-106)/4)),Room.Tank=>TankFloor,
                    Room.ChargeStair=>Mathf.Lerp(Charge,Cast,Mathf.Clamp01((p.y-121)/14)),
                    Room.GallerySouth or Room.LoftPass or Room.Loft or Room.GalleryDoor or Room.Gallery=>Cast,
                    Room.HeartStair=>Mathf.Lerp(Cast,Deep,Mathf.Clamp01((p.y-203)/14)),
                    Room.Landing or Room.HeartDoor or Room.Dais or Room.SmithsDoor or Room.Arena=>Deep,
                    _=>Charge,
                };
                // A bowl of a few centimetres in the arena, and a little unevenness in every stone floor, so no floor is a board.
                if(room==Room.Arena)floor-=.06f*(1-Mathf.Clamp01(Vector2.Distance(p,C)/ArenaRadius));
                floor+=.025f*(Mathf.PerlinNoise(p.x*.37f+3,p.y*.37f+11)-.5f);
                foreach(var (_,a,b) in Troughs){float d=ZoneKit.Segment(p,a,b);if(d<TroughHalf+.2f)floor-=TroughDepth*ZoneKit.Smooth(TroughHalf+.15f,TroughHalf-.05f,d);}
                return floor;
            }
            internal static float FloorAt(Vector2 p,out float level){float clear=Nearest(p,out var room);level=Level(p,room);return clear;}
            internal static bool Walkable(Vector2 p)=>FloorAt(p,out _)<=0;
            /// <summary>The ground. A floor is its room's level; between floors is the foundry's masonry, which, with a floor beyond it along the camera's line, stays under the sightline
            /// to a hero at that floor's edge (the Waystation's rule, D119) and climbs away too steep to stand on.</summary>
            internal static float Height(Vector2 p)
            {
                float clear=FloorAt(p,out float level);if(clear<=0)return level;
                float allowed=float.PositiveInfinity;
                for(float d=.5f;d<=16;d+=.5f){if(FloorAt(p+Away*d,out float there)<0){allowed=Mathf.Min(level,there)+.6f+.82f*d;break;}}
                float rough=Mathf.PerlinNoise((p.x+70)*.11f,(p.y+20)*.09f);
                float top=Mathf.Min(allowed,level+5.6f+2.6f*rough);
                foreach(var (x0,x1,z0,z1,cap) in Bays)if(p.x>x0&&p.x<x1&&p.y>z0&&p.y<z1)top=Mathf.Min(top,level+cap);
                return level+(top-level)*Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/.9f));
            }
            static bool InTrough(Vector2 p){foreach(var (_,a,b) in Troughs)if(ZoneKit.Segment(p,a,b)<TroughHalf+.1f)return true;return false;}
            /// <summary>Basalt flags, darker toward the wall feet; three floors three stones: the charging floor's a little brown with the ore walked into it, the gallery's paler and
            /// greyer under its moulding sand, the heart's dark with slag; a trough's bed black iron.</summary>
            static Color Tone(Vector2 p)
            {
                float clear=FloorAt(p,out float level);float mottle=.9f+.2f*Mathf.PerlinNoise(p.x*.19f+3,p.y*.19f+7);
                // Iron-grey and soot, not tan: at (.55, .5, .46) under a warm key the charging floor read as a sunlit tiled yard (s48/080), and the forge's fire could never be the brightest
                // thing on it. The warmth on these floors is what the fires throw on them.
                Color tone=level>Cast+.5f?new Color(.6f,.55f,.51f):level>Deep+.5f?new Color(.64f,.59f,.54f):new Color(.5f,.44f,.41f);
                if(InTrough(p))tone=new Color(.32f,.29f,.28f);
                tone=Color.Lerp(tone,tone*.72f,ZoneKit.Smooth(-1.6f,0,clear));
                return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
            }

            // ---------------------------------------------------------------- what it is made of
            internal static Material GrateGlow;
            internal static Material FloorMat,WallMat,Iron,Brass,Melt,Furnace,Lamp,Sand,Coolant,Soot,Ingot,CoolantPipe,Cooling,BridleCold,BridleDark,ClampDark,Quenched,Casting,HotSlag,SlagCrust,Runner;
            static readonly Color IronDark=new(.42f,.4f,.4f),IronPale=new(.86f,.84f,.82f),StoneDark=new(.62f,.58f,.56f),StoneWarm=new(1,.94f,.88f),Rust=new(.82f,.6f,.48f);
            /// <summary>Iron ore: dark, a little red-brown in its grey, and nothing in it that glows.</summary>
            static readonly Color OreTop=new(.54f,.45f,.4f),OreUnder=new(.3f,.26f,.25f);
            /// <summary>A generated model's toon material again, as this place has it: its own emission replaced (the Bridle's and its clamps' glowed orange, a hot thing on the dais's
            /// floor that was nobody's marking).</summary>
            static Material Recoloured(string name,string model,Color emission,Color lines)
            {
                var from=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Generated/Models/"+model+"/"+model+"_Toon.mat");if(from==null){Debug.Log("FOUNDRY_NO_MATERIAL "+model);return null;}
                var map=Cooled(model,name,lines);
                return ZoneKit.Toon(name,null,m=>{m.shader=from.shader;m.CopyPropertiesFromMaterial(from);m.shaderKeywords=from.shaderKeywords;m.SetColor("_EmissionColor",emission);if(map!=null)m.SetTexture("_BaseMap",map);});
            }
            /// <summary>A model's base colour map with the orange painted into it taken out (the Bridle's glowing lines are painted on its colour, not its emission map, which is black):
            /// each orange texel goes to <paramref name="lines"/> at its own lightness.</summary>
            static Texture2D Cooled(string model,string name,Color lines)
            {
                var path="Assets/_Project/Art/Generated/Models/"+model+"/"+model+"_base_color.png";if(!System.IO.File.Exists(path))return null;
                var source=new Texture2D(2,2);if(!source.LoadImage(System.IO.File.ReadAllBytes(path))){UnityEngine.Object.DestroyImmediate(source);return null;}
                var cooled=ZoneKit.Texture("Foundry/"+name+"-base",1024,(u,v)=>
                {
                    // Copper as well as orange: at a narrower test the browner lines stayed and read as orange under the forge's light.
                    var c=source.GetPixelBilinear(u,v);float hot=Mathf.Clamp01((c.r-Mathf.Max(c.g*1.15f,c.b*1.4f))*8)*Mathf.Clamp01((c.r-.3f)*5);
                    return Color.Lerp(c,lines*(.55f+.45f*c.grayscale),hot);
                },false);
                UnityEngine.Object.DestroyImmediate(source);return cooled;
            }
            /// <summary>Every part of a placed model drawn in one material; a piece the camera fades keeps its fade.</summary>
            static void Recast(GameObject go,Material to)
            {
                if(to==null)return;
                foreach(var r in go.GetComponentsInChildren<Renderer>()){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)mats[i]=to;r.sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
                if(go.GetComponentInChildren<SceneryOcclusion>()!=null)ZoneKit.Fadeable(go);
            }
            static void Materials()
            {
                // The floor: black moulding sand with cast-iron plates set into it in patches (below).
                static float H(int a,int b)=>Mathf.Repeat(Mathf.Sin(a*12.9898f+b*78.233f)*43758.5453f,1);
                var flags=ZoneKit.Texture("Foundry/foundry-flags",512,(u,v)=>
                {
                    float x=u*6,y=v*6;int ix=Mathf.FloorToInt(x),row=Mathf.FloorToInt(y+(ix%2)*.5f);float fx=x-ix,fy=y+(ix%2)*.5f-row;int iy=((row%6)+6)%6;
                    float own=H(ix,iy),kind=H(ix+7,iy+3),grain=ZoneKit.Cloud(u,v,24,3),pit=ZoneKit.Tile(u*70,v*70,70,9);
                    float edge=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy));
                    float tone;
                    // A foundry's floor, not a paved square: packed black moulding sand, and set into it in patches the cast-iron plates the work stands on, the sand drifting over
                    // their edges. Slabs in courses edge to edge, each a different tan, read from the play camera as a tiled yard (whole-game capture s48/080).
                    float patch=ZoneKit.Cloud((ix+.5f)/6f,(((row%6)+6)%6+.5f)/6f,2,81),drift=ZoneKit.Cloud(u,v,9,83);
                    // The plates carry the floor and the sand lies in islands between them: sand as the ground everywhere measured as one plain from the play camera (look_density, 21 of 38 stills).
                    if(patch<.3f||edge<.16f&&drift>.68f)
                    {
                        float fine=ZoneKit.Tile(u*220,v*220,220,85),coarse=ZoneKit.Cloud(u,v,14,87),clinker=ZoneKit.Tile(u*22,v*22,22,89),grit=ZoneKit.Tile(u*30,v*30,30,97),dragOff=ZoneKit.Cloud(u,v,3,91);
                        // Darker than the plates by a step a camera reads (a floor of one tone measured as plain: look_density).
                        tone=.3f+.1f*(fine-.5f)+.22f*(coarse-.5f);
                        // Clinker and slag trodden into it in lumps a stride apart, drifts of pale grit, and the drag of skips and boots: at a hand across they blurred into the sand.
                        if(clinker>.8f)tone-=.18f*ZoneKit.Smooth(.8f,.88f,clinker);if(grit<.16f)tone+=.16f*ZoneKit.Smooth(.16f,.07f,grit);
                        float drag=Mathf.Abs(Mathf.Repeat(u*12+dragOff*.9f,1)-.5f);if(drag<.06f)tone-=.12f*(1-drag/.06f);
                        return new Color(tone*1.06f,tone*.97f,tone*.9f,1);
                    }
                    // Iron tread plate: a diamond tread, rivets round the rim, a dark joint, worn bright where feet go; the older ones rusting from their joints; here and there a stud
                    // plate where a mould was bolted down.
                    float tread=Mathf.Abs(Mathf.Repeat((fx+fy)*16,1)-.5f),cross=Mathf.Abs(Mathf.Repeat((fx-fy)*16,1)-.5f);
                    // Each plate its own shade, cast and worn apart from its neighbours: a camera reads a plated floor by its plates.
                    float worn=ZoneKit.Cloud(u,v,7,93);tone=.5f+.34f*(own-.5f)+.06f*(grain-.5f)+(tread<.1f&&cross>.3f?.08f:0)+.08f*ZoneKit.Smooth(.55f,.75f,worn)-(pit>.9f?.06f:0);
                    // A rim worn dark before the joint, and the joint itself wide and black: the lines a camera reads a plated floor by.
                    if(edge<.06f)tone*=.82f;
                    if(edge<.07f&&new Vector2(Mathf.Repeat(fx*10,1)-.5f,Mathf.Repeat(fy*10,1)-.5f).magnitude<.22f)tone=.62f;
                    if(edge<.035f)tone=.13f;
                    if(Mathf.Repeat(ix*7+iy*3,13)<1&&Mathf.Abs(fx-.5f)<.16f&&Mathf.Abs(fy-.5f)<.16f){tone=.34f;if(new Vector2(fx-.5f,fy-.5f).magnitude<.03f)tone=.6f;}
                    float rust=kind>.7f?ZoneKit.Smooth(.12f,0,edge)*ZoneKit.Smooth(.45f,.7f,ZoneKit.Cloud(u,v,11,95))*.7f:0;
                    return Color.Lerp(new Color(tone*.94f,tone*.97f,tone*1.04f,1),new Color(tone*1.15f,tone*.78f,tone*.58f,1),rust);
                });
                // The walls: courses of big dark basalt blocks, soot-streaked from the top, with an iron strap across every third course.
                var masonry=ZoneKit.Texture("Foundry/foundry-masonry",512,(u,v)=>
                {
                    float row=v*6;int course=Mathf.FloorToInt(row);float up=row-course;float across=u*3+(course%2)*.5f+ZoneKit.Tile(course*1.9f,.4f,7,2)*.25f;int block=Mathf.FloorToInt(across);float along=across-block;
                    float own=ZoneKit.Tile(block*2.1f+course*5.3f,course*1.1f,40,13),grain=ZoneKit.Cloud(u,v,16,5),soot=ZoneKit.Cloud(u*2,v*.5f,6,21),drip=ZoneKit.Cloud(u*8,v*.25f,8,37);
                    float tone=.5f+.22f*(own-.5f)+.12f*(grain-.5f)-.16f*ZoneKit.Smooth(.5f,.8f,soot)-.08f*ZoneKit.Smooth(.55f,.75f,drip);
                    if(up<.07f||along<.03f)tone=.14f;
                    if(course%3==0&&up>.38f&&up<.62f){tone=.34f+.06f*grain;if(Mathf.Abs(Mathf.Repeat(u*12,1)-.5f)<.05f&&Mathf.Abs(up-.5f)<.06f)tone=.55f;}
                    return new Color(tone*1.02f,tone*.98f,tone*.95f,1);
                });
                // Iron plate: rivet lines, rust running down from them, a dull sheen in the middle of each plate.
                var iron=ZoneKit.Texture("Foundry/foundry-iron",256,(u,v)=>
                {
                    float fx=Mathf.Repeat(u*2,1),fy=Mathf.Repeat(v*2,1);float grain=ZoneKit.Cloud(u,v,12,31),streak=ZoneKit.Cloud(u*6,v*.8f,6,17);
                    float tone=.56f+.1f*(grain-.5f)-.12f*ZoneKit.Smooth(.55f,.75f,streak);
                    bool seam=fx<.02f||fy<.02f;if(seam)tone=.3f;
                    if((fx<.07f||fx>.93f||fy<.07f||fy>.93f)&&new Vector2(Mathf.Repeat(u*20,1)-.5f,Mathf.Repeat(v*20,1)-.5f).magnitude<.2f)tone=.74f;
                    float rust=ZoneKit.Smooth(.58f,.78f,streak)*.6f;
                    return Color.Lerp(new Color(tone,tone*.98f,tone*.96f),new Color(tone*1.1f,tone*.74f,tone*.52f),rust);
                });
                // Crust and the melt under it: dark plates of crust with bright cracks between, the same cracks the emission lights. A crust's skin reads as stone gone cold.
                Color Crack(float u,float v){float a=Mathf.Abs(ZoneKit.Cloud(u,v,7,41)-.5f),b=Mathf.Abs(ZoneKit.Cloud(u,v,13,43)-.5f);float lit=Mathf.Max(ZoneKit.Smooth(.06f,.0f,a),ZoneKit.Smooth(.045f,0,b)*.8f);return new Color(lit,lit,lit,1);}
                var crust=ZoneKit.Texture("Foundry/foundry-crust",256,(u,v)=>{float lit=Crack(u,v).r,grain=ZoneKit.Cloud(u,v,20,47);float tone=.42f+.14f*(grain-.5f)+.4f*lit;return new Color(tone,tone*.96f,tone*.92f,1);});
                var flow=ZoneKit.Texture("Foundry/foundry-flow",256,(u,v)=>{float lit=Crack(u,v).r,swirl=ZoneKit.Cloud(u,v,5,53);float glow=.55f+.25f*swirl+.45f*lit;return new Color(glow,glow*.9f,glow*.75f,1);});
                // Moulding sand: fine, pale, with tool marks.
                var sand=ZoneKit.Texture("Foundry/foundry-sand",256,(u,v)=>{float grain=ZoneKit.Cloud(u,v,30,61),mark=Mathf.Abs(Mathf.Repeat(u*9+ZoneKit.Cloud(u,v,4,63)*.6f,1)-.5f);float tone=.66f+.12f*(grain-.5f)-(mark<.03f?.08f:0);return new Color(tone*1.04f,tone,tone*.9f,1);});
                // The floor's and the sand's least light is low (D380): it is a grey per channel, and under the furnaces' key (linear (1, .48, .21)) a hero's shadow took nearly all
                // its blue and much of its green from it while its red stayed the light's, so the shadow came out a lavender patch beside each hero, bluer than the lit plate round it
                // (s48/075, s48/232); the shadow tint could not change it. At .05 the shade under a body is the warm floor darkened.
                FloorMat=ZoneKit.Toon("foundry-floor",flags,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_BaseColor",Color.white);m.SetFloat("_BaseMapStrength",.9f);m.SetColor("_ShadowTint",new Color(.56f,.45f,.37f));m.SetFloat("_LightFloor",.05f);});
                WallMat=ZoneKit.Toon("foundry-masonry",masonry,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.66f,.62f,.62f));m.SetColor("_ShadowTint",new Color(.46f,.46f,.6f));m.SetFloat("_LightFloor",.24f);});
                Iron=ZoneKit.Toon("foundry-iron",iron,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.76f,.72f,.7f));m.SetColor("_ShadowTint",new Color(.46f,.48f,.6f));m.SetColor("_RimColor",new Color(.5f,.46f,.42f));m.SetFloat("_LightFloor",.26f);});
                Brass=ZoneKit.Toon("foundry-brass",iron,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.95f,.76f,.48f));m.SetColor("_ShadowTint",new Color(.5f,.48f,.58f));m.SetColor("_RimColor",new Color(.7f,.56f,.36f));m.SetFloat("_LightFloor",.3f);});
                Soot=ZoneKit.Toon("foundry-soot",masonry,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.42f,.38f,.36f));m.SetColor("_ShadowTint",new Color(.4f,.4f,.52f));m.SetFloat("_LightFloor",.18f);});
                Sand=ZoneKit.Toon("foundry-sand",sand,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.84f,.78f,.68f));m.SetColor("_ShadowTint",new Color(.56f,.46f,.38f));m.SetFloat("_LightFloor",.05f);});
                // The metal in a trough: crust by day, lit from inside by a PourChannel's property block while it pours. Opaque, so it draws before every marking.
                Melt=ZoneKit.Toon("foundry-melt",crust,m=>{m.SetTexture("_EmissionMap",flow);m.SetColor("_EmissionColor",Color.black);m.SetColor("_BaseColor",new Color(.16f,.13f,.12f));m.SetFloat("_LightFloor",.2f);});
                // What glows for good, and only on the walls: the furnaces' mouths and a tap's spout. Never laid on the floor (D217).
                Furnace=ZoneKit.Toon("foundry-furnace",flow,m=>{m.SetTexture("_EmissionMap",flow);m.SetColor("_EmissionColor",new Color(1.25f,.7f,.3f));m.SetColor("_BaseColor",new Color(1,.78f,.5f));});
                // Cast iron as it comes out of the mould: a rough dark skin, no plates and no rivets (on the iron texture the stacks read as crates).
                var skin=ZoneKit.Texture("Foundry/foundry-castskin",256,(u,v)=>{float fine=ZoneKit.Cloud(u,v,40,71),coarse=ZoneKit.Cloud(u,v,6,73),pit=ZoneKit.Tile(u*90,v*90,90,75);float tone=.42f+.12f*(fine-.5f)+.08f*(coarse-.5f)-(pit>.88f?.08f:0);return new Color(tone,tone*.98f,tone*.97f,1);});
                Ingot=ZoneKit.Toon("foundry-ingot",skin,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.62f,.6f,.6f));m.SetColor("_ShadowTint",new Color(.44f,.46f,.58f));m.SetColor("_RimColor",new Color(.46f,.44f,.44f));m.SetFloat("_LightFloor",.22f);});
                // A pipe full of coolant: the press held still is told by these, a dull teal, faintly lit; never white (a white rod read as a light-sabre).
                CoolantPipe=ZoneKit.Toon("foundry-coolant-pipe",iron,m=>{m.SetColor("_BaseColor",new Color(.46f,.66f,.64f));m.SetColor("_EmissionColor",new Color(.06f,.22f,.22f));m.SetFloat("_LightFloor",.3f);});
                // A ring cooling in its rack: dark iron, a dull red still in its heart. On a rack against a wall, never the floor.
                // (At (.4, .07, .03) a ring read orange in a still, a danger's colour a metre off the floor.)
                Cooling=ZoneKit.Toon("foundry-cooling",skin,m=>{m.SetTexture("_EmissionMap",flow);m.SetColor("_EmissionColor",new Color(.2f,.022f,.01f));m.SetColor("_BaseColor",new Color(.26f,.2f,.19f));m.SetFloat("_LightFloor",.2f);});
                // The Bridle with the young road in it: its painted lines a pale cold grey-blue, near the clamps' strain lamps' white; the half-made ones on their jigs and the clamps for fitting
                // unlit iron, their lines grey.
                BridleCold=Recoloured("foundry-bridle-cold","BridleCage",Color.black,new Color(.62f,.68f,.76f));BridleDark=Recoloured("foundry-bridle-dark","BridleCage",Color.black,new Color(.34f,.34f,.36f));
                ClampDark=Recoloured("foundry-clamp-dark","BridleClamp",Color.black,new Color(.34f,.34f,.36f));
                // A quench trough's water seen from above: a still dark teal (the coolant's own look wants the depth a laid sheet carries, and on a plain quad it drew white).
                Quenched=ZoneKit.Toon("foundry-quench",null,m=>{m.SetColor("_BaseColor",new Color(.14f,.27f,.28f));m.SetColor("_EmissionColor",new Color(.02f,.06f,.06f));m.SetFloat("_LightFloor",.3f);});
                // The Muster's work lamps: cold white glass, so a lamp is never read as heat.
                Lamp=ZoneKit.Toon("foundry-lamp",null,m=>{m.SetColor("_BaseColor",new Color(.9f,.95f,1));m.SetColor("_EmissionColor",new Color(1.1f,1.2f,1.3f));});
                // Coolant: a green-teal, going to a dark teal over the bed: neither the party's cream and cyan nor a danger's orange, red or purple. Pale (.46, .66, .62) at .55 over
                // a trough a hand deep it read as a flat pale-green band laid across the charging floor (whole-game capture of November 22, s48/075, D371).
                Coolant=WaterKit.Look("foundry-coolant",new Color(.3f,.5f,.47f,.72f),new Color(.1f,.26f,.28f,.95f),new Color(.84f,.94f,.9f,1),.6f,4.2f,.03f);
                // After the ground kit's flat details (Transparent-20), before danger markings (-10): a marking over a flooded trough lies on the water.
                Coolant.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent-15;
                // What the whole-game capture found missing (November 20, "no heat in it"): castings fresh out of their moulds, dull red going to orange where they are thickest, on racks,
                // hooks and stands and never the floor; slag still hot where a pot was tipped; the cold slag it crusts to (black glassy skin, the Colossus's crust is drawn in it too, by
                // name, from Resources); and metal running in a launder along the heart's rim, the brightest thing there.
                Casting=ZoneKit.Toon("foundry-casting",skin,m=>{m.SetTexture("_EmissionMap",flow);m.SetColor("_EmissionColor",new Color(1.05f,.34f,.1f));m.SetColor("_BaseColor",new Color(.3f,.2f,.18f));m.SetFloat("_LightFloor",.2f);});
                HotSlag=ZoneKit.Toon("foundry-hot-slag",crust,m=>{m.SetTexture("_EmissionMap",flow);m.SetColor("_EmissionColor",new Color(1.15f,.44f,.14f));m.SetColor("_BaseColor",new Color(.2f,.17f,.16f));m.SetFloat("_LightFloor",.2f);});
                SlagCrust=ZoneKit.Toon("foundry-slag-crust",crust,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",3);m.SetColor("_BaseColor",new Color(.52f,.51f,.54f));m.SetColor("_ShadowTint",new Color(.4f,.42f,.56f));
                    m.SetColor("_RimColor",new Color(.46f,.48f,.56f));m.SetFloat("_LightFloor",.18f);m.SetColor("_EmissionColor",Color.black);});
                Runner=ZoneKit.Toon("foundry-runner",flow,m=>{m.SetTexture("_EmissionMap",flow);m.SetColor("_EmissionColor",new Color(1.7f,1f,.45f));m.SetColor("_BaseColor",new Color(1,.8f,.5f));});
                // Metal running in a channel under a floor grating: the melt's pale yellow-white, never a marking's orange (it is walked over, and it lights the plate beside it).
                GrateGlow=ZoneKit.Toon("foundry-grate-glow",flow,m=>{m.SetTexture("_EmissionMap",flow);m.SetColor("_EmissionColor",new Color(1.3f,1.08f,.92f));m.SetColor("_BaseColor",new Color(1,.9f,.76f));m.SetFloat("_LightFloor",.6f);});
                AssetDatabase.SaveAssets();
            }

            // ---------------------------------------------------------------- pieces
            /// <summary>The enemy if a combat engineer has defined it, else a stand-in of the same family (logged), so the zone is walkable and fought before the roster lands.</summary>
            static string Enemy(string id)
            {
                if(ZoneKit.Find<EnemyDef>(id)!=null)return id;
                string stand=id switch{"ForgeHound"=>"BridleHound","MusterPikeguard"=>"ThawedGuard","PourDrone"=>"RailWarden","VessaKorl"=>"HessaKorl","SlagColossus"=>"Seedthane",_=>id};
                Debug.Log("FOUNDRY_STAND_IN "+stand+" for "+id);return stand;
            }
            static EncounterVolume Fight(ZoneKit kit,string id,Vector2 at,Vector2 size,string requiredFlag,params (string enemy,int count,Vector2 from,float yaw,float radius)[] packs)
            {
                var encounter=WorldBuilder.Encounter(id,kit.On(at.x,at.y,1.5f),new Vector3(size.x,7,size.y),Enemy(packs[0].enemy),packs[0].count);
                var spawners=new List<Spawner>{encounter.spawners[0]};
                for(int i=1;i<packs.Length;i++)spawners.Add(ZoneKit.Spawn(encounter,Enemy(packs[i].enemy),packs[i].count,Vector3.zero,packs[i].radius));
                for(int i=0;i<packs.Length;i++)
                {
                    var spawner=spawners[i];spawner.transform.SetPositionAndRotation(kit.On(packs[i].from.x,packs[i].from.y),Quaternion.Euler(0,packs[i].yaw,0));
                    spawner.faceAsPlaced=true;spawner.radius=packs[i].count>1?packs[i].radius:0;
                }
                encounter.spawners=spawners.ToArray();encounter.requiredFlag=requiredFlag??"";return encounter;
            }
            static Vector3 On(ZoneKit kit,Vector2 p,float up=0)=>kit.On(p.x,p.y,up);
            static RepairBay Cradle(ZoneKit kit,string name,Vector2 at,float yaw,string spawn)
            {
                var bay=kit.Prop("DeckConsole",new Vector3(at.x,0,at.y),1.4f,yaw).AddComponent<RepairBay>();bay.free=true;bay.saveSpawn=spawn;bay.prompt=name+" — repair and save";
                ChapterOneStaging.CradleBay(name,bay.gameObject,On(kit,at),kit.Y(at.x,at.y)+.02f,false,false);return bay;
            }
            /// <summary>A seal across a door, shut through a fight: an iron grille dropped across it, drawn only while shut, with a carving obstacle (the Waystation's).</summary>
            static Membrane Seal(ZoneKit kit,string name,Vector2 at,float width=8.8f,float tall=3.6f)
            {
                var seal=WorldBuilder.Membrane(name,On(kit,at,1.7f),width,true,tall);
                foreach(var filter in seal.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(filter);
                foreach(var view in seal.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(view);
                var box=seal.GetComponent<BoxCollider>();var carve=seal.gameObject.AddComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.center=box.center;carve.size=box.size;carve.carving=true;carve.carveOnlyStationary=false;carve.enabled=false;
                var bars=new ZoneKit.Shape();int count=Mathf.RoundToInt(width/.4f);
                for(int i=0;i<=count;i++){var foot=On(kit,at+new Vector2(-width*.5f+i*width/count,0),-.2f);bars.Slot(0).Post(foot,tall,.06f,.05f,6,IronDark,IronPale);}
                bars.Slot(0).Beam(On(kit,at+new Vector2(-width*.5f,0),tall-.3f),On(kit,at+new Vector2(width*.5f,0),tall-.3f),.16f,.2f,IronDark,IronPale);
                var drawn=kit.Built(name+" bars",bars,Iron);drawn.transform.SetParent(seal.transform,true);drawn.GetComponent<Renderer>().enabled=false;
                return seal;
            }
            /// <summary>A coolant sluice: a cast-iron valve housing on the wall side with its red hand wheel, the pipe it feeds, and the hand at its front. The switch answers
            /// (the wheel turns, the rush, the view drawn back over <paramref name="shows"/>).</summary>
            static FlowSwitch Sluice(ZoneKit kit,string name,Vector2 at,float yaw,Vector2 hand,string flag,Bounds shows)
            {
                var foot=On(kit,at);var turn=Quaternion.Euler(0,yaw,0);
                var housing=new ZoneKit.Shape();
                housing.Slot(0).Box(foot+Vector3.up*.7f,new Vector3(1.3f,1.4f,1.1f),turn,IronDark,IronPale).Post(foot+turn*new Vector3(0,1.4f,0),.3f,.42f,.36f,10,IronPale,IronPale);
                housing.Slot(0).Tube(new[]{foot+turn*new Vector3(0,1,-.5f),foot+turn*new Vector3(0,1,-1.4f),foot+turn*new Vector3(0,3.4f,-1.6f),foot+turn*new Vector3(0,6,-1.6f)},new[]{.22f,.22f,.22f,.22f},8,IronPale,IronDark,1,.5f,false);
                var body=kit.Built(name,housing,Iron);ZoneKit.Block(body,foot+Vector3.up*.7f,new Vector3(1.3f,1.4f,1.1f),yaw,name+" housing");body.isStatic=true;
                var wheel=WaterKit.Wheel(kit,name+" wheel",.62f,Iron,new Color(.86f,.24f,.16f),new Color(.6f,.2f,.14f),6);
                wheel.transform.SetPositionAndRotation(foot+turn*new Vector3(0,1.25f,.62f),turn);
                var point=kit.Discovery(name+" handle",new Vector3(hand.x,0,hand.y),"Turn the sluice wheel",flag,"",null,"Taren",null,0,2.6f);point.situation="sluice";
                // Its own marker: the heart's sluices (CoolantSluice, the boss's) log SLUICE_OPEN with their number.
                var run=body.AddComponent<FlowSwitch>();run.flag=flag;run.wheel=wheel.transform;run.axis=Vector3.forward;run.turns=2;run.seconds=3;run.point=point;run.shows=shows;run.volume=.5f;run.marker="COOLANT_OPEN";
                return run;
            }
            /// <summary>Coolant in a trough: still at a level, or rising to one on a flag (a sluice flooding it), or going on a flag (a sluice draining it). Wading-deep at most.</summary>
            static WaterVolume CoolantIn(ZoneKit kit,string name,(string name,Vector2 from,Vector2 to) trough,float bed,float rest,string risesOn,float risesTo,string drainsOn,float drainsTo)
            {
                var (_,a,b)=trough;var mid=(a+b)*.5f;var run=b-a;bool alongX=Mathf.Abs(run.x)>Mathf.Abs(run.y);
                float xMin=Mathf.Min(a.x,b.x)-TroughHalf-.6f,xMax=Mathf.Max(a.x,b.x)+TroughHalf+.6f,zMin=Mathf.Min(a.y,b.y)-TroughHalf-.6f,zMax=Mathf.Max(a.y,b.y)+TroughHalf+.6f;
                float top=string.IsNullOrEmpty(risesOn)?rest:Mathf.Max(rest,risesTo);
                var sheet=WaterKit.Sheet(kit,name,xMin,xMax,zMin,zMax,.25f,rest,Coolant,p=>ZoneKit.Segment(p,a,b)<TroughHalf+.05f,top,"River");
                var reach=alongX?new Vector2(run.magnitude,TroughHalf*2):new Vector2(TroughHalf*2,run.magnitude);
                var water=WaterKit.Volume(sheet,rest,(mid,reach,0));
                if(!string.IsNullOrEmpty(drainsOn)){water.drainFlag=drainsOn;water.drainedLevel=drainsTo;water.drainSeconds=3;}
                if(!string.IsNullOrEmpty(risesOn)){var staged=water.gameObject.AddComponent<StagedWater>();staged.water=water;staged.flags=new[]{risesOn};staged.levels=new[]{risesTo};staged.seconds=2.6f;}
                return water;
            }
            /// <summary>A still's water: the surface at a level and its depths written again (the Waystation's).</summary>
            static void Stand(WaterVolume water,float level)
            {
                water.surface.position=new Vector3(0,level,0);var mesh=UnityEngine.Object.Instantiate(water.surface.GetComponent<MeshFilter>().sharedMesh);var depth=new List<Vector2>();mesh.GetUVs(1,depth);
                for(int i=0;i<depth.Count;i++)depth[i]=new Vector2(depth[i].x,level-depth[i].x);mesh.SetUVs(1,depth);water.surface.GetComponent<MeshFilter>().sharedMesh=mesh;Debug.Log("FOUNDRY_STILL_WATER "+water.name+" "+level);
            }
            /// <summary>The metal in a trough (lit by its PourChannel) and the iron kerbs along both lips: built along the trough's line, at the bed's height a hand under the marking.</summary>
            static GameObject[] TroughMetal(ZoneKit kit,string name,(string name,Vector2 from,Vector2 to) trough,Func<Vector2,bool> keep)
            {
                var (_,a,b)=trough;var dir=(b-a).normalized;var side=new Vector2(-dir.y,dir.x);float length=Vector2.Distance(a,b);
                var metal=new ZoneKit.Shape();var kerbs=new ZoneKit.Shape();
                for(float s=0;s<length-.01f;s+=1)
                {
                    float e=Mathf.Min(length,s+1);var p0=a+dir*s;var p1=a+dir*e;if(!keep((p0+p1)*.5f))continue;
                    float y0=kit.Y(p0.x,p0.y)+.03f,y1=kit.Y(p1.x,p1.y)+.03f;float w=TroughHalf-.05f;
                    metal.Slot(0).Quad(new Vector3(p0.x-side.x*w,y0,p0.y-side.y*w),new Vector3(p1.x-side.x*w,y1,p1.y-side.y*w),new Vector3(p1.x+side.x*w,y1,p1.y+side.y*w),new Vector3(p0.x+side.x*w,y0,p0.y+side.y*w),Vector3.up,Color.white,.5f);
                    foreach(int k in new[]{-1,1})
                    {
                        var q0=p0+side*k*(TroughHalf+.05f);var q1=p1+side*k*(TroughHalf+.05f);
                        kerbs.Slot(0).Beam(new Vector3(q0.x,kit.Y(q0.x,q0.y)+.02f,q0.y),new Vector3(q1.x,kit.Y(q1.x,q1.y)+.02f,q1.y),.22f,.1f,IronDark,IronPale);
                    }
                }
                var drawn=kit.Built(name+" metal",metal,Melt);var edged=kit.Built(name+" kerbs",kerbs,Iron);
                drawn.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                return new[]{drawn,edged};
            }
            /// <summary>A pour: the trough's metal and kerbs, its strip (the trough and a margin each side) and its clock, and for a ladle line the rail, the ladle and its stream.
            /// A tap run gets a spout at its head and a seal over it instead.</summary>
            /// <summary>Over the play camera's own height over a hero's floor (19.5 m at 40 degrees from the chest: 13.7 m), with a margin: what runs here can hide nothing from it.</summary>
            const float OverTheCamera=14.6f;
            static PourChannel Pour(ZoneKit kit,string name,(string name,Vector2 from,Vector2 to) trough,Vector2 from,Vector2 to,float period,float phase,string armFlag,string cooledFlag,
                Vector2? railRest=null,Vector2? railOver=null,float railHigh=4.9f,Func<Vector2,bool> keep=null,Vector2? railEnd=null,float hangHigh=-1)
            {
                keep??=_=>true;var dir=(to-from).normalized;var side=new Vector2(-dir.y,dir.x);
                var drawn=TroughMetal(kit,name,(trough.name,from,to),keep);
                var holder=new GameObject(name);holder.transform.position=On(kit,from)+Vector3.up*.02f;
                var pour=holder.AddComponent<PourChannel>();pour.along=new Vector3(dir.x,0,dir.y);pour.length=Vector2.Distance(from,to);pour.width=(TroughHalf+Margin)*2;
                pour.period=period;pour.phase=phase;pour.armFlag=armFlag??"";pour.cooledFlag=cooledFlag??"";pour.metal=new[]{drawn[0].GetComponent<Renderer>()};
                var mid=(from+to)*.5f;var light=kit.Lamp(new Vector3(mid.x,1.6f,mid.y),Mathf.Min(12,pour.length*.6f+3),0,new Color(1,.86f,.62f));light.name="Pour light";light.transform.SetParent(holder.transform,true);pour.glow=light;
                if(period<=0)
                {
                    // A tap run: the spout in the wall at its head, the stream from it, and the seal over the trough nobody may cross while it runs.
                    var head=from;var spout=new ZoneKit.Shape();var at=On(kit,head-dir*.2f,1.6f);
                    spout.Slot(0).Box(at+new Vector3(-dir.x,0,-dir.y)*.5f,new Vector3(1.4f,1.1f,1.4f),Quaternion.LookRotation(new Vector3(dir.x,0,dir.y)),IronDark,IronPale)
                        .Tube(new[]{at,at+new Vector3(dir.x,0,dir.y)*.9f+Vector3.down*.2f},new[]{.32f,.26f},8,IronPale,IronDark);
                    kit.Built(name+" spout",spout,Iron);
                    var fall=new ZoneKit.Shape();var lip=at+new Vector3(dir.x,0,dir.y)*.95f+Vector3.down*.25f;
                    fall.Slot(0).Tube(new[]{lip,lip+new Vector3(dir.x,0,dir.y)*.35f+Vector3.down*.5f,new Vector3(lip.x+dir.x*.5f,kit.Y(head.x+dir.x*.5f,head.y+dir.y*.5f)-.15f,lip.z+dir.y*.5f)},new[]{.2f,.17f,.22f},8,Color.white,Color.white);
                    var stream=kit.Built(name+" stream",fall,Furnace);stream.transform.SetParent(holder.transform,true);pour.stream=stream;
                    var seal=new GameObject(name+" seal",typeof(Membrane));seal.transform.SetParent(holder.transform,true);
                    var block=new GameObject("Molten metal",typeof(BoxCollider),typeof(NavMeshObstacle));block.transform.SetParent(seal.transform,true);
                    var length=Vector2.Distance(from,to);var size=new Vector3(TroughHalf*2+.4f,1.2f,length);
                    block.transform.SetPositionAndRotation(On(kit,mid,.6f),Quaternion.LookRotation(new Vector3(dir.x,0,dir.y)));block.GetComponent<BoxCollider>().size=size;
                    var carve=block.GetComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.size=size;carve.carving=true;carve.carveOnlyStationary=false;
                    pour.seal=seal;
                }
                else
                {
                    // A ladle line: the girder rail it runs on, posts at its ends, the ladle (its pivot the trolley on the rail, the bucket hanging under it) and its stream.
                    var rest=railRest.Value;var over=railOver.Value;var railDir=(over-rest).normalized;
                    // The rail runs from over the ladle's bay to a wall past where it pours, and stands on a post at each end (on a wall's top or the bay's ledge): nothing of it hangs in the air.
                    var rail=new ZoneKit.Shape();var end=railEnd??over+railDir*3;var r0=new Vector3(rest.x,0,rest.y)-new Vector3(railDir.x,0,railDir.y)*1.5f;var r1=new Vector3(end.x,0,end.y);
                    float railY=kit.Y(mid.x,mid.y)+railHigh,hangY=hangHigh>0?kit.Y(mid.x,mid.y)+hangHigh:railY;r0.y=r1.y=railY;
                    rail.Slot(0).Beam(r0,r1,.34f,.5f,IronDark,IronPale).Beam(r0+Vector3.down*.32f,r1+Vector3.down*.32f,.56f,.08f,IronDark,IronPale);
                    // A rail over the camera stands on nothing anyone sees: posts up to it were columns in the frame for nothing.
                    bool overhead=railY-hangY>.5f;
                    if(!overhead)foreach(var post in new[]{r0,r1}){float foot=Height(new Vector2(post.x,post.z));if(foot<railY-.6f&&!Walkable(new Vector2(post.x,post.z)))rail.Slot(0).Box(new Vector3(post.x,(foot+railY)*.5f-.1f,post.z),new Vector3(.42f,railY-foot+.2f,.42f),Quaternion.identity,IronDark,IronPale);}
                    var railed=kit.Built(name+" rail",rail,Iron);
                    var ladle=new ZoneKit.Shape();
                    ladle.Slot(0).Box(Vector3.down*.15f,new Vector3(.9f,.5f,1.3f),Quaternion.identity,IronDark,IronPale);
                    foreach(int k in new[]{-1,1})ladle.Slot(0).Tube(new[]{new Vector3(0,-.3f,k*.45f),new Vector3(0,-.6f,k*.88f),new Vector3(0,-.95f,k*.98f)},new[]{.06f,.06f,.06f},6,IronPale,IronDark);
                    ladle.Slot(0).Tube(new[]{new Vector3(0,-2.1f,0),new Vector3(0,-1.96f,0),new Vector3(0,-1.2f,0),new Vector3(0,-1f,0)},new[]{.45f,.72f,.9f,.94f},14,Rust,IronDark,1,.5f,true);
                    ladle.Slot(1).Disc(new Vector3(0,-1.05f,0),Vector3.up,.84f,14,Color.white);
                    var bucket=kit.Built(name+" ladle",ladle,Iron,Furnace);bucket.transform.SetParent(holder.transform,true);
                    // Under a rail over the camera the ladle waits up by it, out of the play view (resting by its wall at five metres it hid the floor of the fight beyond it), and is
                    // let down as it runs out to pour: what a player watches come.
                    var restAt=new Vector3(rest.x,overhead?railY-.5f:hangY-.32f,rest.y);var overAt=new Vector3(over.x,hangY-.32f,over.y);
                    bucket.transform.SetPositionAndRotation(restAt,Quaternion.LookRotation(new Vector3(railDir.x,0,railDir.y)));
                    pour.ladle=bucket.transform;pour.ladleRest=restAt;pour.ladleOver=overAt;
                    // It tips toward the trough: whichever way round its own forward axis swings the bucket's foot over the metal.
                    var towardTrough=new Vector3(mid.x-over.x,0,mid.y-over.y);
                    var swung=Quaternion.LookRotation(new Vector3(railDir.x,0,railDir.y))*Quaternion.AngleAxis(30,Vector3.forward)*Vector3.down;pour.tip=Vector3.Dot(swung,towardTrough)>=0?34:-34;
                    // The stream: from where the tipped lip is to the bed under the rail's pour point.
                    var tipped=Quaternion.LookRotation(new Vector3(railDir.x,0,railDir.y))*Quaternion.AngleAxis(pour.tip,Vector3.forward);
                    // It swings across its rail (about the rail's own axis), so the lip it pours over is the rim on the side it swings to.
                    var lipAt=overAt+tipped*new Vector3(Mathf.Sign(pour.tip)*.92f,-1.05f,0);var bed=new Vector3(lipAt.x,kit.Y(lipAt.x,lipAt.z)-.2f,lipAt.z);
                    var fall=new ZoneKit.Shape();fall.Slot(0).Tube(new[]{lipAt,Vector3.Lerp(lipAt,bed,.5f),bed},new[]{.18f,.15f,.24f},8,Color.white,Color.white);
                    var stream=kit.Built(name+" stream",fall,Furnace);stream.transform.SetParent(holder.transform,true);pour.stream=stream;stream.SetActive(false);
                    railed.transform.SetParent(holder.transform,true);
                    // A rail over the camera (it can hide nothing from it, and is never seen): the ladle hangs from its trolley on two rods, carried along the run and never tipped.
                    if(overhead)
                    {
                        var rods=new ZoneKit.Shape();float up=railY-hangY+.32f;
                        foreach(int k in new[]{-1,1})rods.Slot(0).Tube(new[]{new Vector3(0,.08f,k*.42f),new Vector3(0,up,k*.42f)},new[]{.035f,.035f},6,IronDark,IronPale);
                        rods.Slot(0).Box(new Vector3(0,up,0),new Vector3(.7f,.4f,1.1f),Quaternion.identity,IronDark,IronPale);
                        var hanger=kit.Built(name+" hanger",rods,Iron);hanger.transform.SetPositionAndRotation(restAt,Quaternion.LookRotation(new Vector3(railDir.x,0,railDir.y)));
                        hanger.transform.SetParent(holder.transform,true);hanger.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;pour.hanger=hanger.transform;
                    }
                }
                return pour;
            }
            /// <summary>The press over the press run: two columns in the passage's walls, a crown girder, the ram on its guides, and the coolant lamps that show it held.</summary>
            static ForgePress Press(ZoneKit kit,string name,Vector2 at,float length,float width)
            {
                var foot=On(kit,at);var frame=new ZoneKit.Shape();float crown=6.4f;
                foreach(int k in new[]{-1,1})
                {
                    var col=foot+new Vector3(length*.5f,0,k*(width*.5f+.45f));
                    frame.Slot(0).Box(col+Vector3.up*crown*.5f,new Vector3(1.1f,crown,.9f),Quaternion.identity,IronDark,IronPale);
                    frame.Slot(0).Box(col+Vector3.up*.25f,new Vector3(1.6f,.5f,1.3f),Quaternion.identity,IronDark,IronPale);
                }
                frame.Slot(0).Box(foot+new Vector3(length*.5f,crown+.5f,0),new Vector3(2,1,width+2.8f),Quaternion.identity,IronDark,IronPale);
                frame.Slot(0).Post(foot+new Vector3(length*.5f,crown-1.6f,0),1.6f,.5f,.5f,12,IronPale,IronDark);
                var body=kit.Built(name,frame,Iron);
                foreach(int k in new[]{-1,1})ZoneKit.Block(body,foot+new Vector3(length*.5f,crown*.5f,k*(width*.5f+.45f)),new Vector3(1.1f,crown,.9f),0,name+" column");
                body.isStatic=true;
                var ram=new ZoneKit.Shape();ram.Slot(0).Box(Vector3.zero,new Vector3(length-.4f,1.2f,width-.4f),Quaternion.identity,IronDark,Rust).Box(Vector3.up*.9f,new Vector3(length-1.4f,.6f,width-1.6f),Quaternion.identity,IronDark,IronPale);
                var drawnRam=kit.Built(name+" ram",ram,Iron);drawnRam.transform.SetParent(body.transform,true);
                var ramUp=foot+new Vector3(length*.5f,crown-2.4f,0);var ramDown=foot+new Vector3(length*.5f,.62f,0);drawnRam.transform.position=ramUp;
                // While it is held: a coolant pipe down each column, lit pale, and the lamps on the crown.
                var held=new ZoneKit.Shape();
                foreach(int k in new[]{-1,1}){var col=foot+new Vector3(length*.5f-.7f,0,k*(width*.5f+.45f));held.Slot(0).Tube(new[]{col+Vector3.up*.6f,col+Vector3.up*(crown-.3f)},new[]{.12f,.12f},8,Color.white,Color.white);}
                var quenched=kit.Built(name+" held",held,CoolantPipe);quenched.transform.SetParent(body.transform,true);
                var press=body.AddComponent<ForgePress>();press.ram=drawnRam.transform;press.ramUp=ramUp;press.ramDown=ramDown;press.quenched=quenched;
                press.transform.position=foot+Vector3.up*.02f;
                return press;
            }
            static LootCache Stash(ZoneKit kit,string id,string label,Vector2 at,float yaw,int scrip,params LootEntry[] contents)
            {
                var shape=new ZoneKit.Shape();
                shape.Slot(0).Box(new Vector3(0,.38f,0),new Vector3(1.3f,.76f,.8f),Quaternion.identity,IronDark,IronPale).Box(new Vector3(0,.82f,0),new Vector3(1.36f,.14f,.86f),Quaternion.identity,IronPale);
                foreach(float x in new[]{-.44f,.44f})shape.Slot(0).Box(new Vector3(x,.45f,0),new Vector3(.09f,.92f,.9f),Quaternion.identity,Rust);
                var body=kit.Built("Footlocker "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),shape,Iron);
                body.transform.SetPositionAndRotation(On(kit,at),Quaternion.Euler(0,yaw,0));
                var box=body.AddComponent<BoxCollider>();box.center=Vector3.up*.45f;box.size=new Vector3(1.4f,.9f,.9f);body.isStatic=true;
                var cache=LootCache.On(body,id,label,scrip,contents);cache.range=2.6f;return cache;
            }
            static void Hide(GameObject who,string name,string from,string gone,bool live=true)
            {var where=new GameObject(name+" (where)",typeof(Whereabouts)).GetComponent<Whereabouts>();where.from=from??"";where.goneAt=gone??"";where.targets=new[]{who};where.live=live;}
            static RadioHail Tube(Vector2 at,string heard,float range,string[] needs,string silencedBy)
            {
                var hail=new GameObject("Tovra on the speaking tube",typeof(RadioHail)).GetComponent<RadioHail>();hail.transform.position=new Vector3(at.x,0,at.y);
                hail.speaker="Tovra";hail.situations=new[]{"hail.sluice"};hail.heardFlag=heard;hail.range=range;hail.needs=needs;hail.silencedBy=silencedBy??"";return hail;
            }

            // ---------------------------------------------------------------- the zone
            internal static void Build()
            {
                Materials();
                var kit=ZoneKit.Ground(FoundryZone,Height,1.2f);bool greybox=DevArgs.Has("-foundry-greybox");
                var scene=WorldBuilder.Begin(FoundryZone);
                kit.Terrain("Foundry floors",-46,54,-14,293,.5f,FloorMat,WallMat,Tone,new Color(.92f,.86f,.82f),new Color(.58f,.54f,.52f));

                // ---- the mouth: the way home, the cradle, the arrival
                WorldBuilder.Spawn("Arrival",On(kit,Arrival));WorldBuilder.Spawn("Gallery",On(kit,GallerySpawn));WorldBuilder.Spawn("Heart",On(kit,HeartSpawn));
                var gate=MouthGate(kit);kit.Way(gate,"Foundry mouth — The Scoria Flows",ScoriaZone,ScoriaFromFoundry,null,null,3.8f,new Vector2(-3.4f,-3.8f));
                Cradle(kit,"Mouth cradle",MouthCradle,-120,"Arrival");
                kit.Sign("CHARGING FLOOR",4.6f,11.6f,1.8f);

                // ---- the yard: the Forge Hound met, on open floor with walls to end its leaps
                Fight(kit,"Foundry_Yard",new Vector2(0,26),new Vector2(34,28),null,("ForgeHound",1,new Vector2(-7,35),180,0),("ForgeHound",1,new Vector2(7,35.5f),190,0));
                // ---- the skip store off its west side (optional): a pair, and a footlocker
                Fight(kit,"Foundry_Skips",new Vector2(-30,24),new Vector2(15,17),null,("MusterPikeguard",2,new Vector2(-31,27),90,2.1f));
                Stash(kit,"vask.foundry.skips","Charging-floor footlocker",SkipCache,90,140,ProgressionBuilder.AnyPart(4,GearSlot.Drive,1),ProgressionBuilder.Drop("ScoriaGlass",3,1),ProgressionBuilder.Drop("RepairGel",2,1));

                // ---- the wheel hall: the rule introduced with nothing awake. Tap run A across the whole hall; the first wheel on this side; ladle line L beyond, quenched until it turns.
                kit.Crossing("The first sluice wheel",new Vector3(0,0,46),new Vector3(36,5,3),"vask.sluiceRead","IdraFirstSluice","Taren");
                var showsA=new Bounds(new Vector3(0,0,65),new Vector3(38,4,22));
                Sluice(kit,"Sluice one",WheelOne,120,WheelOneHand,"vask.sluice1",showsA);
                Tube(WheelOne,"vask.sluiceTube1",7,new[]{"vask.sluiceRead"},"vask.sluice1");
                var a=Trough("A");
                Pour(kit,"Tap run A",a,new Vector2(17.6f,58),new Vector2(-17.6f,58),0,0,null,"vask.sluice1");
                var coolantA=CoolantIn(kit,"Coolant A",a,Charge-TroughDepth,Charge-TroughDepth-.3f,"vask.sluice1",Charge-TroughDepth+.16f,null,0);
                var l=Trough("L");
                Pour(kit,"Ladle line L",l,new Vector2(-5,73),new Vector2(5,73),10,0,"vask.sluice1",null,new Vector2(10,71.7f),new Vector2(0,71.7f),railEnd:new Vector2(-6.4f,71.7f));
                var coolantL=CoolantIn(kit,"Coolant L",l,Charge-TroughDepth,Charge-TroughDepth+.18f,null,0,"vask.sluice1",Charge-TroughDepth-.4f);

                // ---- the charging floor: the furnaces' mouths along its north wall, and a fight
                Fight(kit,"Charging_Floor",new Vector2(0,97),new Vector2(42,30),null,("MusterPikeguard",2,new Vector2(-4,106),180,2.1f),("ForgeHound",1,new Vector2(12,104),200,0));

                // ---- the press run east to the second wheel ("behind the press"), which floods tap run B (the way down) and drains the quench tank (the press arms)
                var press=Press(kit,"The drop press",PressAt,4,8);press.armFlag="vask.sluice2";press.along=Vector3.right;press.length=4;press.width=8;
                var showsB=new Bounds(new Vector3(36,-1,103),new Vector3(26,5,30));
                Sluice(kit,"Sluice two",WheelTwo,-90,WheelTwoHand,"vask.sluice2",showsB);
                Tube(new Vector2(-13,115),"vask.sluiceTube2",9,new[]{"vask.sluice1"},"vask.sluice2");Tube(new Vector2(20,96),"vask.sluiceTube2",7,new[]{"vask.sluice1"},"vask.sluice2");
                // The bay's own floor, east of the press: the hounds come through the press run at whoever turned the wheel, and the press's square stays outside the fight's box.
                Fight(kit,"Press_Bay",new Vector2(40,96),new Vector2(14,22),"vask.sluice2",("ForgeHound",1,new Vector2(16.5f,94),90,0),("ForgeHound",1,new Vector2(16.5f,98.5f),90,0));
                var tankSheet=WaterKit.Sheet(kit,"Quench tank water",34,46,104,120,.5f,-.4f,Coolant,p=>p.y>105.5f,float.NaN,"River");
                var tank=WaterKit.Volume(tankSheet,-.4f,(new Vector2(40,113),new Vector2(11,15),0));tank.drainFlag="vask.sluice2";tank.drainedLevel=TankFloor-.6f;tank.drainSeconds=3.4f;
                WaterKit.Deep(kit,tank,34,46,104,120,p=>p.y>105.5f);
                Stash(kit,"vask.foundry.tank","Quench-tank strongbox",TankCache,180,160,ProgressionBuilder.AnyPart(4,GearSlot.Frame,1),ProgressionBuilder.Drop("BridleWire",3,1),ProgressionBuilder.Drop("ChargeCell",2,1));
                var b=Trough("B");
                Pour(kit,"Tap run B",b,new Vector2(-18.6f,117),new Vector2(-7.4f,117),0,0,null,"vask.sluice2");
                var coolantB=CoolantIn(kit,"Coolant B",b,Charge-TroughDepth,Charge-TroughDepth-.3f,"vask.sluice2",Charge-TroughDepth+.16f,null,0);

                // ---- the casting gallery's south room: ladle line L2 right across it (two ladles on one clock), the drones' rails, a fight; the loft off its east side across L3
                kit.Sign("CASTING GALLERY",-7.4f,137.6f,1.8f);
                var l2=Trough("L2");
                // Two ladles on one clock, each resting over the floor by its wall and pouring at a third of the room's width.
                // Each ladle waits by its side wall, its rail running into the wall behind it; the two rails meet over the middle of the room.
                // Their rail runs over the camera: hung at five metres across the room it laid a band of the drones' fight's floor out of the play view, where a marking would not be
                // seen (NothingHungOverheadHidesAFightsFloorFromTheCamera). The ladles wait up by it and are let down on rods to pour at the height they always had.
                Pour(kit,"Ladle line L2 west",l2,new Vector2(-26,141),new Vector2(0,141),9,3,null,null,new Vector2(-25.7f,139.7f),new Vector2(-7,139.7f),railEnd:new Vector2(0,139.7f),railHigh:OverTheCamera+3,hangHigh:4.9f);
                Pour(kit,"Ladle line L2 east",l2,new Vector2(0,141),new Vector2(26,141),9,3,null,null,new Vector2(25.7f,139.7f),new Vector2(7,139.7f),railEnd:new Vector2(0,139.7f),railHigh:OverTheCamera+3,hangHigh:4.9f);
                // The pour drones' rails run north from the ladle line, so neither rail crosses the other's hanging load.
                foreach(float x in new[]{-13f,13f})
                {
                    var rail=new GameObject("Drone rail "+x,typeof(ForceLine)).GetComponent<ForceLine>();rail.transform.position=On(kit,new Vector2(x,144));rail.end=On(kit,new Vector2(x,154.5f));rail.width=1.8f;
                    // A pair of slender rails a hand and a half apart, on thin hangers from over the camera, the floor reading between them. The girder they replace, hung at three and a
                    // half metres over the drones' fight, laid a band of its floor out of the play view (NothingHungOverheadHidesAFightsFloorFromTheCamera). The track is where it was.
                    var girder=new ZoneKit.Shape();var y=Cast+3.4f;
                    foreach(float k in new[]{-.3f,.3f})girder.Slot(0).Tube(new[]{new Vector3(x+k,y,143.4f),new Vector3(x+k,y,156)},new[]{.04f,.04f},6,IronDark,IronPale);
                    for(float z=145.6f;z<155;z+=3.2f)foreach(float k in new[]{-.3f,.3f})girder.Slot(0).Tube(new[]{new Vector3(x+k,y+.03f,z),new Vector3(x+k,Cast+OverTheCamera+1,z)},new[]{.022f,.022f},5,IronDark,IronPale);
                    // On a post at each end: from the floor at its south end (solid: it stands in the room), from the low wall's top at its north.
                    foreach(float z in new[]{143.4f,156f}){float foot=Height(new Vector2(x,z));girder.Slot(0).Box(new Vector3(x,(foot+y)*.5f,z),new Vector3(.46f,y-foot+.3f,.46f),Quaternion.identity,IronDark,IronPale);}
                    var held=kit.Built("Drone rail girder "+x,girder,Iron);float under=kit.Y(x,143.4f);ZoneKit.Block(held,new Vector3(x,(under+y)*.5f,143.4f),new Vector3(.46f,y-under,.46f),0,"Girder post");held.isStatic=true;
                    foreach(var t in held.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
                    // Drawn, it stays out of the static batch, which can't be read in play: what it hides is measured with the rest.
                    held.isStatic=false;
                }
                Fight(kit,"Gallery_Drones",new Vector2(0,146),new Vector2(50,19),null,("PourDrone",1,new Vector2(-13,151),180,0),("PourDrone",1,new Vector2(13,151),180,0),("MusterPikeguard",1,new Vector2(0,152),180,0));
                var l3=Trough("L3");
                Pour(kit,"Ladle line L3",l3,new Vector2(28.7f,140.6f),new Vector2(28.7f,147.4f),8,1.5f,null,null,new Vector2(27.4f,151),new Vector2(27.4f,144),railEnd:new Vector2(27.4f,140.2f));
                Stash(kit,"vask.foundry.loft","Patternmaker's chest",LoftCache,200,180,ProgressionBuilder.AnyPart(4,GearSlot.Module,1),ProgressionBuilder.Drop("BridleWire",2,1),ProgressionBuilder.Drop("FoundrySeal",1,.35f));
                Cradle(kit,"Gallery cradle",GalleryCradle,-100,"Gallery");

                // ---- the casting gallery: Vessa Korl's floor, to the contract
                CastingGallery(kit);

                // ---- the heart
                kit.Sign("THE HEART",6.6f,222.6f,1.8f);
                Cradle(kit,"Heart cradle",HeartCradle,-110,"Heart");
                Heart(kit,greybox);

                // ---- limits: the walls are the stop; a box on each side keeps a body from finding a way up one.
                WorldBuilder.Boundary("South limit",new Vector3(4,4,-13),new Vector3(100,30,2));WorldBuilder.Boundary("North limit",new Vector3(4,4,292),new Vector3(100,30,2));
                WorldBuilder.Boundary("West limit",new Vector3(-45,4,140),new Vector3(2,30,310));WorldBuilder.Boundary("East limit",new Vector3(53,4,140),new Vector3(2,30,310));

                if(!greybox)Dress(kit);
                // For stills of the foundry as both wheels leave it (never saved as the zone: rebuild without it): the runs flooded, the gangway's trough and the tank empty.
                if(DevArgs.Has("-foundry-drained"))
                {
                    foreach(var w in new[]{coolantA,coolantB})Stand(w,Charge-TroughDepth+.16f);Stand(coolantL,Charge-TroughDepth-.4f);Stand(tank,TankFloor-.6f);foreach(var block in tank.deep)block.SetActive(false);
                    foreach(var pour in UnityEngine.Object.FindObjectsByType<PourChannel>(FindObjectsSortMode.None))if(pour.seal!=null){pour.seal.SetActive(false);if(pour.stream!=null)pour.stream.SetActive(false);}
                }
                // ---- navigation, with every run cooled, every seal open and the tank drained: what is shut at first is play's business.
                var reach=new List<Vector2>{Arrival,MouthCradle+new Vector2(-1.6f,0),WayHome+new Vector2(0,2.6f),new(0,26),new(-14,36),new(14,14),new(-30,24),SkipCache+new Vector2(1.5f,0),
                    WheelOneHand,new(0,52),new(0,62),new(0,68),new(0,79),new(0,97),new(-16,108),new(16,86),PressAt+new Vector2(2,0),new(24,96),WheelTwoHand,new(40,90),new(43,108),TankCache+new Vector2(0,-1.5f),
                    new(-13,114),new(-13,120),new(-13,128),new(0,146),new(-20,150),new(20,150),new(28,144),new(35,144),LoftCache+new Vector2(-1.6f,0),GallerySpawn,GalleryCradle+new Vector2(-1.6f,0),
                    new(0,161),G,VessaPost+new Vector2(4,0),new(-20,198),new(20,165),new(0,210),HeartSpawn,HeartCradle+new Vector2(-1.6f,0),new(0,227),C,C+new Vector2(0,18),C+new Vector2(18,0),C+new Vector2(-18,0),
                    CageAt+new Vector2(0,-6),AnvilAt+new Vector2(1.6f,0)};
                var shut=UnityEngine.Object.FindObjectsByType<PourChannel>(FindObjectsSortMode.None).Where(p=>p.seal!=null).Select(p=>p.seal).ToList();foreach(var s in shut)s.SetActive(false);
                WaterKit.WithOpenWater(()=>kit.BakeNavigation(new Bounds(new Vector3(4,-2,140),new Vector3(102,40,310)),Arrival,reach));
                foreach(var s in shut)s.SetActive(true);
                if(!greybox)Ground(kit);
                if(DevArgs.Has("-foundry-hot"))Hot();
                WorldBuilder.Save(scene,FoundryZone);Debug.Log("BRIDLE_FOUNDRY_OK");
            }
            /// <summary>For stills of the forge as it is in play (never saved as the zone: rebuild without it). An editor still draws a pour's metal as its material has it, black crust,
            /// because play lights it by property block; here each tap run's metal is its ember with its stream falling and its low light on, and the gangway's ladle and the gallery's
            /// west ladle hang tipped over their troughs mid-pour, the metal under them the melt's pale yellow-white and their lights at full. The furnaces, vents and crucibles show
            /// their own fire at its middle breath.</summary>
            static void Hot()
            {
                int lit=0;
                foreach(var pour in UnityEngine.Object.FindObjectsByType<PourChannel>(FindObjectsSortMode.None))
                {
                    bool run=pour.period<=0;if(!run&&pour.name!="Ladle line L"&&pour.name!="Ladle line L2 west")continue;
                    var look=new Material(Melt){name=run?"foundry-melt-ember (stills)":"foundry-melt-pouring (stills)"};
                    look.SetColor("_EmissionColor",run?pour.ember:pour.molten);look.SetColor("_BaseColor",run?pour.emberBase:pour.hot);
                    foreach(var r in pour.metal)if(r!=null)r.sharedMaterial=look;
                    if(pour.stream!=null)pour.stream.SetActive(true);
                    if(pour.glow!=null){pour.glow.enabled=true;pour.glow.intensity=pour.glowIntensity*(run?.3f:1);pour.glow.color=run?new Color(1,.6f,.36f):pour.hot;}
                    if(!run&&pour.ladle!=null)pour.ladle.SetPositionAndRotation(pour.ladleOver,pour.ladle.rotation*Quaternion.AngleAxis(pour.tip,Vector3.forward));
                    lit++;
                }
                Debug.Log("FOUNDRY_HOT_STILLS "+lit);
            }

            /// <summary>The way home: an iron gateway in the mouth's south wall, its gates hooked back open, and the glow of the Flows beyond it.</summary>
            static GameObject MouthGate(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();float z=WayHome.y;
                foreach(int k in new[]{-1,1}){shape.Slot(0).Box(new Vector3(k*3.4f,kit.Y(k*3.4f,z)+2.4f,z),new Vector3(.8f,4.8f,.8f),Quaternion.identity,IronDark,IronPale);
                    shape.Slot(0).Box(new Vector3(k*4.7f,kit.Y(k*4.7f,z+1.2f)+1.6f,z+1.2f),new Vector3(.12f,3.2f,2.4f),Quaternion.Euler(0,k*12,0),IronDark,Rust);}
                shape.Slot(0).Box(new Vector3(0,kit.Y(0,z)+5,z),new Vector3(7.8f,.7f,1),Quaternion.identity,IronDark,IronPale);
                var gate=kit.Built("Foundry mouth",shape,Iron);
                foreach(int k in new[]{-1,1})ZoneKit.Block(gate,new Vector3(k*3.4f,kit.Y(k*3.4f,z)+2.4f,z),new Vector3(.8f,4.8f,.8f),0,"Gate post");
                gate.isStatic=true;
                // The way home is offered from the mouth, not from the world's origin where a built body sits (its shape is in world places): on the body it reached the arrival,
                // a stride inside, and not the spot before the gate (RouteFightTests, November 14).
                var holder=new GameObject("Foundry mouth");holder.transform.position=new Vector3(WayHome.x,kit.Y(WayHome.x,z)+.2f,z);gate.name="Foundry mouth drawn";gate.transform.SetParent(holder.transform,true);
                return holder;
            }

            /// <summary>The casting gallery to its contract (docs/maps/BridleFoundry.md): the four mould racks as cover, Vessa's post and her squad's, the two seals, the talk just inside
            /// the south door, her way out to the heart's gantry, and the talk she falls back with.</summary>
            static void CastingGallery(ZoneKit kit)
            {
                var racks=new GameObject("Mould racks");
                foreach(var at in Racks)
                {
                    var shape=new ZoneKit.Shape();var base_=On(kit,at);
                    // A rack of rib moulds stood on end: an iron frame 4 m long, the moulds' sand cores between its uprights.
                    for(int i=0;i<=4;i++)shape.Slot(0).Box(base_+new Vector3(-1.9f+i*.95f,1.05f,0),new Vector3(.14f,2.1f,1.2f),Quaternion.identity,IronDark,IronPale);
                    shape.Slot(0).Box(base_+new Vector3(0,2.05f,0),new Vector3(4.1f,.12f,1.25f),Quaternion.identity,IronDark,IronPale).Box(base_+new Vector3(0,.1f,0),new Vector3(4.1f,.2f,1.25f),Quaternion.identity,IronDark,IronPale);
                    // The cores in moulding sand, each with its rib's curve pressed into its face, and over the two in the middle their pour cups, still hot: a rack of moulds just poured,
                    // where a rack of pale cores read as crates (s48/216).
                    var sandTone=new Color(.8f,.72f,.6f);
                    for(int i=0;i<4;i++)
                    {
                        var core=base_+new Vector3(-1.42f+i*.95f,1.02f,0);shape.Slot(1).Box(core,new Vector3(.74f,1.8f,.9f),Quaternion.identity,ZoneKit.Shape.Dim(sandTone,.88f),sandTone);
                        foreach(int face in new[]{-1,1}){var arc=new Vector3[7];var round=new float[7];for(int k=0;k<7;k++){float t=k/6f;arc[k]=core+new Vector3(Mathf.Sin(t*Mathf.PI)*.18f-.06f,(t-.5f)*1.5f,face*.455f);round[k]=.035f;}shape.Slot(0).Tube(arc,round,5,IronDark,IronDark,1,.4f,false);}
                        if(i==1||i==2){var cup=core+Vector3.up*1.09f;shape.Slot(0).Tube(new[]{cup,cup+Vector3.up*.16f},new[]{.11f,.17f},10,IronDark,IronPale,1,.5f,false);shape.Slot(2).Disc(cup+Vector3.up*.14f,Vector3.up,.14f,10,Color.white);}
                    }
                    var rack=kit.Built("Mould rack "+at.x+" "+at.y,shape,Iron,Sand,Casting);rack.transform.SetParent(racks.transform,true);
                    ZoneKit.Block(rack,base_+Vector3.up*1.05f,new Vector3(4,2.1f,1.2f),0,"Mould rack");
                    foreach(var t in rack.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
                }
                var south=Seal(kit,"Gallery south seal",new Vector2(0,157));var north=Seal(kit,"Heart stair seal",new Vector2(0,202.6f));
                var duel=WorldBuilder.Encounter("VessaKorl",kit.On(G.x,G.y,3),new Vector3(50,6,42),Enemy("VessaKorl"),1,new[]{south,north});
                var post=duel.spawners[0];post.transform.SetPositionAndRotation(On(kit,VessaPost),Quaternion.Euler(0,180,0));post.faceAsPlaced=true;post.radius=0;
                var squad=ZoneKit.Spawn(duel,Enemy("MusterPikeguard"),2,Vector3.zero,2.1f);squad.transform.SetPositionAndRotation(On(kit,SquadPost),Quaternion.Euler(0,180,0));squad.faceAsPlaced=true;squad.radius=2.1f;
                duel.spawners=new[]{post,squad};duel.requiredFlag="vask.vessaMet";duel.checkpointSpawn="Gallery";
                var view=new GameObject("Vessa preview (the south door)");view.transform.position=On(kit,new Vector2(0,160));duel.previewPoint=view.transform;duel.previewRadius=31;
                var met=kit.Crossing("The casting gallery",new Vector3(0,0,161),new Vector3(50,5,3),"vask.vessaMet","VessaGallery","Vessa");met.saveSpawn="Gallery";met.previewEncounter=duel;
                // Her way out: the gantry door in the north wall, barred to the party.
                var exit=new GameObject("Vessa's way out (the gantry door)",typeof(BossExit)).GetComponent<BossExit>();exit.transform.position=On(kit,GantryDoor);exit.boss="VessaKorl";exit.sinks=false;
                var door=new ZoneKit.Shape();var foot=On(kit,new Vector2(GantryDoor.x,203.4f));
                foreach(int k in new[]{-1,1})door.Slot(0).Box(foot+new Vector3(k*1.6f,1.9f,0),new Vector3(.5f,3.8f,.7f),Quaternion.identity,IronDark,IronPale);
                door.Slot(0).Box(foot+new Vector3(0,3.95f,0),new Vector3(3.7f,.5f,.7f),Quaternion.identity,IronDark,IronPale);
                for(int i=0;i<7;i++)door.Slot(0).Post(foot+new Vector3(-1.2f+i*.4f,-.1f,0),3.75f,.05f,.045f,6,IronDark,IronPale);
                var barred=kit.Built("Gantry door",door,Iron);ZoneKit.Block(barred,foot+Vector3.up*1.9f,new Vector3(3.7f,3.8f,.7f),0,"Gantry door bars");barred.isStatic=true;
                var falls=new GameObject("Vessa falls back",typeof(AfterTalk)).GetComponent<AfterTalk>();falls.transform.position=On(kit,G);
                // At half her bar, as she falls back (the contract, revised): her shift sets the flag, and she writes her squad off in front of them, the fight paused under the talk.
                // Heard, it says only that: the step it once set ("Take the casting gallery") moved the card on to the Colossus with a third of her bar to go (whole-game capture
                // s48/221). The step is set as she leaves at nought, and her leaving marks this heard if it was still owed, so it plays no more.
                falls.flag=BossController.FallingBackFlag;falls.node="VessaFallsBack";falls.speaker="Vessa";falls.heardFlag=BossController.FallBackHeardFlag;falls.delay=1.6f;falls.duringFight=true;
            }

            /// <summary>The heart: the landing and its talk, the door, the arena (the boss contract's), the dais with the Bridle and its juvenile, the gantry, and the release.</summary>
            static void Heart(ZoneKit kit,bool greybox)
            {
                var seal=Seal(kit,"Heart door seal",new Vector2(0,227));
                var confront=kit.Crossing("The heart's door",new Vector3(0,0,225.2f),new Vector3(8.8f,5,2.2f),"vask.cageSeen","SelaCagedRoad","Sela");confront.saveSpawn="Heart";
                var duel=WorldBuilder.Encounter("SlagColossus",kit.On(C.x,C.y,3),new Vector3(44,8,44),Enemy("SlagColossus"),1,new[]{seal});
                var post=duel.spawners[0];post.transform.SetPositionAndRotation(On(kit,C),Quaternion.Euler(0,180,0));post.faceAsPlaced=true;post.radius=0;
                duel.requiredFlag="vask.cageSeen";duel.checkpointSpawn="Heart";
                var view=new GameObject("Colossus preview (the landing inside the door)");view.transform.position=On(kit,new Vector2(0,229));duel.previewPoint=view.transform;duel.previewRadius=26;confront.previewEncounter=duel;
                HeartSluices(kit);CoverStacks(kit);
                Cage(kit);
                Release(kit);
            }
            /// <summary>Where on the heart's rim a bearing (degrees clockwise from north) and a distance from C put a point.</summary>
            internal static Vector2 FromC(float bearing,float r)=>C+new Vector2(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad))*r;
            /// <summary>The boss contract's sluices: 1 the red wheel nearest the door (bearing 200), 2 east (80), 3 north-west (320).</summary>
            internal static readonly (int index,float bearing)[] HeartSluiceBearings={(1,200),(2,80),(3,320)};
            internal const float SeatOut=15,HousingOut=16.4f,SpoutOut=18.5f;
            /// <summary>The three coolant sluices round the heart, to the boss contract: each a <see cref="CoolantSluice"/> on an empty object at its seat (r 15, facing C), its red wheel on a
            /// solid housing 1.4 m behind the seat with its hub 1.2 m up facing C, the pipe from the housing up the rim wall to the spout's mouth (r 18.5, 2.6 m up, facing C), drawn only.</summary>
            static void HeartSluices(ZoneKit kit)
            {
                foreach(var (index,bearing) in HeartSluiceBearings)
                {
                    var seatAt=FromC(bearing,SeatOut);var toC=(C-seatAt).normalized;var facing=Quaternion.LookRotation(new Vector3(toC.x,0,toC.y));float yaw=facing.eulerAngles.y;
                    var housingAt=FromC(bearing,HousingOut);var spoutAt=FromC(bearing,SpoutOut);var foot=On(kit,housingAt);
                    var shape=new ZoneKit.Shape();
                    shape.Slot(0).Box(foot+Vector3.up*1,new Vector3(1.6f,2,1.2f),facing,IronDark,IronPale).Post(foot+facing*new Vector3(0,1.2f,.6f),.2f,.3f,.26f,10,IronPale,IronPale);
                    // The pipe: up the back of the housing, out to the rim wall and up it to the spout's mouth, high over the floor (never across a seat's way out).
                    var back=foot+facing*new Vector3(0,1.6f,-.7f);var rim=new Vector3(spoutAt.x,foot.y+3.4f,spoutAt.y)-new Vector3(toC.x,0,toC.y)*.4f;
                    shape.Slot(0).Tube(new[]{back,back+Vector3.up*1.4f,new Vector3(rim.x,back.y+1.6f,rim.z),rim,new Vector3(spoutAt.x,foot.y+2.6f,spoutAt.y)},new[]{.18f,.18f,.18f,.18f,.2f},8,IronPale,IronDark,1,.5f,false);
                    shape.Slot(0).Tube(new[]{new Vector3(spoutAt.x,foot.y+2.6f,spoutAt.y),new Vector3(spoutAt.x,foot.y+2.6f,spoutAt.y)+new Vector3(toC.x,-.2f,toC.y)*.7f},new[]{.24f,.3f},10,IronPale,Rust);
                    var housing=kit.Built("Heart sluice "+index,shape,Iron);ZoneKit.Block(housing,foot+Vector3.up*1,new Vector3(1.6f,2,1.2f),yaw,"Heart sluice housing");housing.isStatic=true;
                    foreach(var t in housing.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
                    // The south housing stands on the camera's side of the floor: it fades for a hero behind it.
                    if(index==1)ZoneKit.Fadeable(housing);
                    var wheel=WaterKit.Wheel(kit,"Heart sluice "+index+" wheel",.62f,Iron,new Color(.86f,.24f,.16f),new Color(.6f,.2f,.14f),6);
                    wheel.transform.SetPositionAndRotation(foot+Vector3.up*1.2f+new Vector3(toC.x,0,toC.y)*.66f,facing);
                    var spout=new GameObject("Heart sluice "+index+" spout").transform;spout.SetPositionAndRotation(new Vector3(spoutAt.x,foot.y+2.6f,spoutAt.y),facing);spout.SetParent(housing.transform,true);
                    var seat=new GameObject("Heart sluice "+index+" seat");seat.transform.SetPositionAndRotation(On(kit,seatAt),facing);
                    var sluice=seat.AddComponent<CoolantSluice>();sluice.index=index;sluice.settledBy="SlagColossus";sluice.wheel=wheel.transform;sluice.spout=spout;
                }
            }
            /// <summary>The boss contract's cover: three stacks of cast ingots, 2.2 m tall and 2.8 by 1.6, long side across the line to C, at r 9 on bearings 140, 260 and 20.</summary>
            internal static readonly float[] CoverBearings={140,260,20};
            static void CoverStacks(ZoneKit kit)
            {
                int n=0;
                foreach(float bearing in CoverBearings)
                {
                    var at=FromC(bearing,9);var toC=(C-at).normalized;var across=Quaternion.LookRotation(new Vector3(toC.x,0,toC.y));var foot=On(kit,at);var shape=new ZoneKit.Shape();var rng=new System.Random(41+n);
                    // Ingots laid in courses, each course turned across the one under it, a little off true: a stack the smiths built, not a block.
                    for(int course=0;course<5;course++)
                    {
                        bool turned=course%2==1;int count=turned?5:3;
                        for(int k=0;k<count;k++)
                        {
                            float along=turned?(k-2)*.54f:0,deep=turned?0:(k-1)*.52f;var size=turned?new Vector3(.5f,.42f,1.56f):new Vector3(2.76f,.42f,.48f);
                            var c=foot+across*new Vector3(along+((float)rng.NextDouble()-.5f)*.04f,.21f+course*.44f,deep);
                            shape.Slot(0).Box(c,size,across*Quaternion.Euler(0,((float)rng.NextDouble()-.5f)*3,0),IronDark,Color.Lerp(IronPale,Rust,(float)rng.NextDouble()*.5f));
                        }
                    }
                    var stack=kit.Built("Ingot stack "+n,shape,Ingot);ZoneKit.Block(stack,foot+Vector3.up*1.1f,new Vector3(2.8f,2.2f,1.6f),across.eulerAngles.y,"Ingot stack");
                    foreach(var t in stack.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
                    ZoneKit.Fadeable(stack);n++;
                }
            }
            /// <summary>The Bridle on its dais: the kit's cage, the four clamps at its foot with their strain lamps, the chains up into the vault, the vent's collar over it, and the
            /// juvenile inside (the Cantor's parts, small and pale: the young road), pulling nine and easing four.</summary>
            static void Cage(ZoneKit kit)
            {
                var foot=On(kit,CageAt);
                var cage=kit.Prop(ZoneKit.Kit("BridleCage","HushwellEggCradle"),new Vector3(CageAt.x,0,CageAt.y),8,0);cage.name="The Bridle";Recast(cage,BridleCold);
                var lamps=new List<Renderer>();var clampShape=new ZoneKit.Shape();var lampShape=new ZoneKit.Shape();
                foreach(float bearing in new[]{45f,135f,225f,315f})
                {
                    var d=new Vector3(Mathf.Sin(bearing*Mathf.Deg2Rad),0,Mathf.Cos(bearing*Mathf.Deg2Rad));var at=foot+d*4.6f;
                    clampShape.Slot(0).Box(at+Vector3.up*.6f,new Vector3(1.2f,1.2f,1.4f),Quaternion.LookRotation(-d),IronDark,IronPale).Tube(new[]{at+Vector3.up*1.1f,foot+d*2.6f+Vector3.up*2.2f},new[]{.16f,.12f},6,IronPale,IronDark);
                    var lamp=new ZoneKit.Shape();lamp.Slot(0).Lump(at+Vector3.up*1.35f,new Vector3(.2f,.14f,.2f),(int)bearing,.05f,Color.white,Color.white,8,5);
                    var lit=kit.Built("Clamp lamp "+bearing,lamp,Lamp);lamps.Add(lit.GetComponent<Renderer>());
                }
                var clamps=kit.Built("Bridle clamps",clampShape,Iron);
                foreach(float bearing in new[]{45f,135f,225f,315f}){var d=new Vector3(Mathf.Sin(bearing*Mathf.Deg2Rad),0,Mathf.Cos(bearing*Mathf.Deg2Rad));ZoneKit.Block(clamps,foot+d*4.6f+Vector3.up*.6f,new Vector3(1.2f,1.2f,1.4f),bearing,"Clamp");}
                clamps.isStatic=true;
                var hang=new ZoneKit.Shape();
                foreach(float bearing in new[]{0f,120f,240f}){var d=new Vector3(Mathf.Sin(bearing*Mathf.Deg2Rad),0,Mathf.Cos(bearing*Mathf.Deg2Rad));hang.Slot(0).Tube(new[]{foot+d*2.4f+Vector3.up*7.6f,foot+d*3f+Vector3.up*16},new[]{.12f,.12f},6,IronDark,IronPale,1,1);}
                var collar=foot+Vector3.up*13;var ring=new Vector3[25];var thick=new float[25];for(int k=0;k<25;k++){float t=k*Mathf.PI*2/24;ring[k]=collar+new Vector3(Mathf.Cos(t)*4.4f,0,Mathf.Sin(t)*4.4f);thick[k]=.5f;}
                hang.Slot(0).Tube(ring,thick,8,IronDark,IronPale,1,.4f,false);
                kit.Built("Vent collar and chains",hang,Iron);
                // The juvenile: a head and a few links of the Cantor's make, a third of its size, coiled in the cage and lit from inside.
                var young=new GameObject("The juvenile");young.transform.position=foot+Vector3.up*3.2f;
                var head=ZoneKit.Prefab("Enemies","CantorHead");var link=ZoneKit.Prefab("Enemies","CantorBody");var tail=ZoneKit.Prefab("Enemies","CantorTail");
                void Part(GameObject prefab,Vector3 local,float yaw,float scale){if(prefab==null)return;var part=(GameObject)PrefabUtility.InstantiatePrefab(prefab);part.transform.SetParent(young.transform,false);part.transform.localPosition=local;part.transform.localRotation=Quaternion.Euler(0,yaw,0);part.transform.localScale=Vector3.one*scale;
                    foreach(var c in part.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);foreach(var m in part.GetComponentsInChildren<MonoBehaviour>())UnityEngine.Object.DestroyImmediate(m);}
                Part(head,new Vector3(0,.6f,-1.1f),180,.34f);Part(link,new Vector3(1,.2f,0),90,.3f);Part(link,new Vector3(0,-.2f,1.1f),0,.3f);Part(link,new Vector3(-1,-.5f,.1f),270,.3f);Part(tail,new Vector3(-.4f,-.9f,-.9f),200,.28f);
                young.AddComponent<PropTint>().tint=new Color(.82f,.9f,1);
                var inner=kit.Lamp(new Vector3(CageAt.x,3.4f,CageAt.y),9,2.2f,new Color(.7f,.86f,1));inner.name="Cage light";inner.transform.SetParent(young.transform.parent,true);
                var choir=new GameObject("The young road (CagedChoir)",typeof(CagedChoir)).GetComponent<CagedChoir>();choir.body=young.transform;choir.clamps=lamps.ToArray();choir.inner=inner;choir.freedFlag="vask.juvenileFreed";
                choir.rise=new[]{young.transform.position,foot+Vector3.up*9,foot+Vector3.up*14,foot+Vector3.up*24};
            }
            /// <summary>After the Colossus (the brief's section, manifest 8.5 to 8.7). Nobody appears in view. From the fight's start the smiths, Tovra and Brekk wait behind the barred
            /// door in the north wall behind the dais, and Vessa beyond the gantry's east door, all out of frame from the heart's floor; at its fall the bars go and they walk in, the smiths
            /// to the clamps, Brekk to the fourth, Tovra round the cage to stand before it, Vessa along the gantry; the release follows once they are there. As it ends Tovra walks to the
            /// anvil with Brekk, and Vessa off the gantry by its door. Each is a <see cref="Guide"/> (a leg on the fall and one on the release), so a load stands everyone where the story
            /// has put them.</summary>
            static void Release(ZoneKit kit)
            {
                // A beat longer than the walk in: the smiths are at their clamps before Tovra calls the hands.
                var talk=new GameObject("The release",typeof(AfterTalk)).GetComponent<AfterTalk>();talk.transform.position=On(kit,C);
                talk.flag="bossdown.SlagColossus";talk.node="TovraRelease";talk.speaker="Tovra";talk.heardFlag="vask.juvenileFreed";talk.delay=7.5f;
                const string Fall="bossdown.SlagColossus",Freed="vask.juvenileFreed",Waiting="vask.cageSeen";
                // The door: an iron frame in the north wall under the gantry. Barred until the fall (nobody walks into the smiths waiting behind it), its gates hooked back after.
                var door=new ZoneKit.Shape();var doorFoot=On(kit,new Vector2(0,286.8f));
                foreach(int k in new[]{-1,1})door.Slot(0).Box(doorFoot+new Vector3(k*2.7f,1.8f,0),new Vector3(.5f,3.6f,.6f),Quaternion.identity,IronDark,IronPale);
                door.Slot(0).Box(doorFoot+new Vector3(0,3.8f,0),new Vector3(6,.5f,.7f),Quaternion.identity,IronDark,IronPale);
                kit.Built("Smiths door",door,Iron);
                var bars=new ZoneKit.Shape();for(int i=0;i<=12;i++)bars.Slot(0).Post(doorFoot+new Vector3(-2.4f+i*.4f,-.1f,0),3.65f,.05f,.045f,6,IronDark,IronPale);
                bars.Slot(0).Beam(doorFoot+new Vector3(-2.45f,1.6f,0),doorFoot+new Vector3(2.45f,1.6f,0),.08f,.12f,IronDark,IronPale);
                var grille=kit.Built("Smiths door grille",bars,Iron);ZoneKit.Block(grille,doorFoot+Vector3.up*1.8f,new Vector3(4.9f,3.6f,.3f),0,"Smiths door bars");
                Hide(grille,"Smiths door grille",null,Fall);
                var gates=new ZoneKit.Shape();foreach(int k in new[]{-1,1})gates.Slot(0).Box(doorFoot+new Vector3(k*2.26f,1.5f,1.15f),new Vector3(.1f,3f,2),Quaternion.identity,IronDark,Rust);
                var open=kit.Built("Smiths door gates",gates,Iron);Hide(open,"Smiths door gates",Fall,null);
                // What each looks toward at the end of each leg (a Guide reads the end of leg i from faces[i+1], so there is one more than there are legs): with one face for each
                // leg, the last leg's end had none, and everyone who walked in stood as their last step left them, five smiths in a row facing nothing (s49/140).
                Guide Walks(GameObject who,float speed,Vector3[] faces,params (string flag,Vector3[] path)[] legs)
                {
                    var guide=who.AddComponent<Guide>();guide.speed=speed;guide.patience=6;
                    guide.legs=legs.Select(l=>new Guide.Leg{flag=l.flag,path=l.path}).ToArray();
                    guide.faces=Enumerable.Range(0,legs.Length+1).Select(i=>faces[Mathf.Min(i,faces.Length-1)]).ToArray();
                    who.transform.position=legs[0].path[0];return guide;
                }
                Vector3 P(float x,float z)=>On(kit,new Vector2(x,z));
                var cage=On(kit,CageAt);var anvil=On(kit,AnvilAt);
                var smiths=new GameObject("The smiths at the clamps");var rng=new System.Random(29);
                // Each in a body of their own, and no two one height carrying one thing (Folk): five bodies round eleven read as one smith over and over (s49/140).
                var bodies=Folk.Bodies;
                Color[] tints={new(.82f,.78f,.74f),new(.9f,.84f,.76f),new(.74f,.72f,.7f),new(.86f,.8f,.7f),new(.8f,.76f,.72f)};
                int n=0;
                foreach(float bearing in new[]{45f,135f,225f,315f})for(int k=0;k<3;k++)
                {
                    if(bearing==315f&&k==0)continue;// Brekk's place at the fourth
                    var d=new Vector2(Mathf.Sin((bearing+(k-1)*16)*Mathf.Deg2Rad),Mathf.Cos((bearing+(k-1)*16)*Mathf.Deg2Rad));var at=CageAt+d*(6.4f+(k%2)*.9f);
                    var who=kit.Townsperson(bodies[n%bodies.Length],new Vector3(at.x,0,at.y),Mathf.Atan2(-d.x,-d.y)*Mathf.Rad2Deg,tints[(n*3+rng.Next(2))%tints.Length],"BrekkBody");
                    Folk.Set(kit,who,n);
                    // Each in a dye of their own body (tools/townsfolk_dyes.py: teal, oxblood, moss, indigo), round the clamps in turn so the three at one clamp wear three: in
                    // their own paintings most wear a pale shirt and dark trousers, and two side by side read as twins at play size however their heads or builds differed (s49/140, D380).
                    var worn=TownsfolkLook.Body(who);var dye=worn!=null?AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Project/Art/Generated/Townsfolk/{bodies[n%bodies.Length]}/{bodies[n%bodies.Length]}_dye{1+n%4}.mat"):null;
                    if(dye!=null){worn.sharedMaterial=dye;PrefabUtility.RecordPrefabInstancePropertyModifications(worn);}
                    foreach(var t in who.GetComponentsInChildren<Transform>())t.gameObject.isStatic=false;
                    who.name="Smith "+n;who.transform.SetParent(smiths.transform,true);
                    // In at the door in ones and twos, from further back the later they come, round the cage's side to their clamp.
                    float lane=((n%3)-1)*1.1f;var start=P(lane,290+(n%4)*.8f);var mouth=P(lane,286);var round=P(Mathf.Sign(at.x+.01f)*Mathf.Max(Mathf.Abs(at.x),6.2f),Mathf.Max(at.y,284));
                    // At its clamp each faces the cage it holds, and goes on facing it after the release: with no face for its last leg's end a smith stood as the walk in left it.
                    Walks(who,3.4f,new[]{cage},("",new[]{start}),(Fall,new[]{mouth,round,P(at.x,at.y)}));n++;
                }
                Hide(smiths,"The smiths",Waiting,"vask.roadSaid");
                GameObject Body(string name,string id)
                {
                    var prefab=FirstBody(id,"BrekkBody");var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.name=name;
                    foreach(var rig in body.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullCompletely;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
                    return body;
                }
                var fourth=CageAt+new Vector2(Mathf.Sin(299*Mathf.Deg2Rad),Mathf.Cos(299*Mathf.Deg2Rad))*6.4f;
                var brekk=Body("Brekk","BrekkBody");Walks(brekk,3.2f,new[]{cage,cage,cage,anvil},("",new[]{P(-1.2f,291.6f)}),(Fall,new[]{P(-1.2f,286),P(-7,284),P(fourth.x,fourth.y)}),(Freed,new[]{P(AnvilAt.x+1.6f,AnvilAt.y-1.8f)}));
                Hide(brekk,"Brekk at the release",Waiting,"vask.roadSaid");
                // Brisker than the rest: the longest way in, round the cage's east side to stand before it, and the release is hers to call.
                var tovra=Body("Tovra","TovraBody");Walks(tovra,4,new[]{cage,cage,cage,anvil},("",new[]{P(.6f,292.2f)}),(Fall,new[]{P(.6f,286),P(5.8f,283.4f),P(6.8f,276.4f),P(2.4f,271.8f),P(0,271.6f)}),(Freed,new[]{P(-3.6f,272.6f),P(AnvilAt.x+.2f,AnvilAt.y-.9f)}));
                Hide(tovra,"Tovra at the release",Waiting,"vask.roadSaid");
                // Vessa out of the gantry's east door (her way from the gallery) and along it, and back off it by that door as the release ends. Loaded after it, she isn't there at all.
                float g=Deep+5.02f;var vessa=Body("Vessa on the gantry","VessaBody");
                Walks(vessa,2.4f,new[]{cage},("",new[]{new Vector3(17.4f,g,285.8f)}),(Fall,new[]{new Vector3(14.2f,g,285.8f),new Vector3(9.5f,g,285.6f)}),(Freed,new[]{new Vector3(14.2f,g,285.8f),new Vector3(17.4f,g,285.8f)}));
                var holder=new GameObject("Vessa (from the fall)");vessa.transform.SetParent(holder.transform,true);
                Hide(holder,"Vessa on the gantry",Waiting,"vask.roadSaid");Hide(vessa,"Vessa gone after the release",null,Freed,false);
                // Her door off the gantry's east end is the gantry's own piece (Gantry): a frame set in the wall, drawn apart, read as a thing sunk in it.
                // The anvil, and Brekk's one line there once Tovra has sat down by it. A smith's anvil of the forge's own, not the Lattice bench: a block on a stump, its horn toward the cage.
                var anvilShape=new ZoneKit.Shape();var anvilTurn=Quaternion.Euler(0,30,0);
                anvilShape.Slot(0).Post(anvil,.55f,.42f,.34f,10,IronDark,IronDark).Box(anvil+Vector3.up*.66f,new Vector3(.62f,.24f,1.1f),anvilTurn,IronDark,IronPale)
                    .Box(anvil+Vector3.up*.86f,new Vector3(.5f,.16f,1.3f),anvilTurn,IronDark,IronPale).Tube(new[]{anvil+anvilTurn*new Vector3(0,.86f,.64f),anvil+anvilTurn*new Vector3(0,.84f,1.05f)},new[]{.16f,.03f},8,IronPale,IronPale);
                var anvilBody=kit.Built("Tovra's anvil",anvilShape,Iron);ZoneKit.Block(anvilBody,anvil+Vector3.up*.47f,new Vector3(.62f,.94f,1.2f),30,"Anvil");anvilBody.isStatic=true;
                var stand=kit.Discovery("Stand by the anvil",new Vector3(AnvilAt.x+1.4f,0,AnvilAt.y-2.6f),"Stand by the anvil","vask.anvilSeen","","BrekkAnvil","Brekk",null,0,3);
                stand.requiredFlag="vask.juvenileFreed";stand.situation="";
            }

            // ---------------------------------------------------------------- dressing
            /// <summary>The walked lines, room by room: the ground kit keeps them clear, the dressing keeps its tall things off them.</summary>
            internal static readonly Vector2[][] Ways=
            {
                new Vector2[]{new(0,-4),new(0,12),new(0,30),new(0,46),new(-10,50),new(0,56),new(0,66),new(0,80),new(0,92),new(-13,108),new(-13,121),new(-13,134),new(-6,142),new(0,150),new(0,160),new(0,198),new(0,218),new(0,229)},
                new Vector2[]{new(0,96),new(20,96),new(34,96),new(42,99)},new Vector2[]{new(0,30),new(-20,24),new(-30,24)},new Vector2[]{new(18,144),new(28,144),new(36,144),new(39,149)},
                new Vector2[]{new(40,104),new(43,108),new(40,114)},
            };
            static bool NearWay(Vector2 p,float within){foreach(var way in Ways)if(ZoneKit.FromPath(p,way)<within)return true;return false;}
            static bool NearTrough(Vector2 p,float within){foreach(var (_,a,b) in Troughs)if(ZoneKit.Segment(p,a,b)<TroughHalf+Margin+within)return true;return false;}
            /// <summary>Where a hero presses or arrives: nothing stands within reach of these.</summary>
            static readonly Vector2[] Prompts={Arrival,MouthCradle,WayHome,WheelOneHand,WheelTwoHand,TankCache,SkipCache,LoftCache,GallerySpawn,GalleryCradle,HeartSpawn,HeartCradle,AnvilAt,GantryDoor,PressAt+new Vector2(2,0)};
            static bool NearPrompt(Vector2 p,float within){foreach(var q in Prompts)if(Vector2.Distance(p,q)<within)return true;return false;}
            /// <summary>The fights' floors as rectangles: nothing tall stands in their middles.</summary>
            static readonly (Vector2 at,Vector2 size)[] Fights={(new(0,26),new(34,28)),(new(-30,24),new(15,17)),(new(0,97),new(42,30)),(new(36,96),new(26,22)),(new(0,146),new(50,19)),(new(0,180),new(50,42))};
            static bool InFight(Vector2 p,float inset){foreach(var (at,size) in Fights)if(Mathf.Abs(p.x-at.x)<size.x*.5f-inset&&Mathf.Abs(p.y-at.y)<size.y*.5f-inset)return true;return Vector2.Distance(p,C)<ArenaRadius+2.5f;}
            /// <summary>Nothing this tall at a plan point may stand where a walked floor lies within the fixed camera's shadow of it.</summary>
            static bool ShadowsAFloor(Vector2 p,float tall)
            {
                float reach=ZoneKit.Shadowed(tall)+1.2f;var side=new Vector2(Away.y,-Away.x);
                for(float d=.75f;d<=reach;d+=.75f)foreach(float s in new[]{-1.6f,0,1.6f})if(Walkable(p+Away*d+side*s))return true;
                return false;
            }
            /// <summary>What a foundry's floor keeps by its walls: low things anywhere along a wall, tall machines only along the north and east walls (the back of every view).</summary>
            static readonly (string key,string fallback,float small,float large)[] Low={("MusterCrate","DressCargoPallet",1,1.5f),("DressCargoPallet","DressCargoPallet",.7f,1),("DressWaterBarrels","DressCargoPallet",1,1.3f),
                ("DressCableDrum","DressCargoPallet",.9f,1.2f),("HushwellCrates","DressCargoPallet",1,1.4f),("HushwellRubble","DressBoulders",.7f,1.1f)};
            // (A fuel tank's copper bands stood in front of the gallery's half-made Bridle and read as a still: a second ore hopper in its place.)
            static readonly (string key,string fallback,float small,float large)[] Tall={("DressOreHopper","DressShelfRack",2.6f,3.4f),("MusterCutterRig","DressShelfRack",2.6f,3.4f),("DressShelfRack","DressShelfRack",2.2f,2.8f),
                ("DressOreHopper","DressShelfRack",2.4f,3),("DressSpoilHeap","HushwellRubble",1.6f,2.4f),("HushwellScaffold","DressShelfRack",2.6f,3.4f)};
            /// <summary>How high over its foot a heap starts to stop a body (a body steps over less: the bake's climb is .3), and how many sides its fitted outline has.</summary>
            const float HeapAnkle=.35f;const int HeapSides=16;
            /// <summary>A heap's solid fitted to what is drawn of it, as XylosBuilder.FitHull fits a barge: the outline of the mound where it stands higher than a body steps, stood from its foot
            /// to its top, in place of the box round its whole drawn bounds. A spoil heap fitted by its height lies four times as wide as it stands, and that box was an 8 m square whose
            /// corners stood across the yard's way and the wheel hall's where the drawn slag was nothing (HarrowgateRouteTests: the routes' walks up the main way were cut, November 14).
            /// The prism is what the bake and the physics both meet; each heap's is saved beside the scene.</summary>
            static void FitHeap(GameObject prop)
            {
                var points=new List<Vector3>();var toProp=prop.transform.worldToLocalMatrix;
                foreach(var filter in prop.GetComponentsInChildren<MeshFilter>())
                {
                    if(filter.sharedMesh==null)continue;var toWorld=filter.transform.localToWorldMatrix;
                    foreach(var v in filter.sharedMesh.vertices)points.Add(toProp.MultiplyPoint3x4(toWorld.MultiplyPoint3x4(v)));
                }
                if(points.Count==0)return;
                float low=points.Min(p=>p.y),high=points.Max(p=>p.y);
                var standing=points.Where(p=>p.y>low+HeapAnkle).ToList();
                foreach(var old in prop.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(old);
                // Lower than a body steps all over: nothing to meet.
                if(standing.Count<3)return;
                var ring=new List<Vector3>();
                for(int k=0;k<HeapSides;k++)
                {
                    float a=k*Mathf.PI*2/HeapSides,cos=Mathf.Cos(a),sin=Mathf.Sin(a);Vector3 far=standing[0];float most=float.NegativeInfinity;
                    foreach(var p in standing){float d=p.x*cos+p.z*sin;if(d>most){most=d;far=p;}}
                    var corner=new Vector3(far.x,0,far.z);if(ring.Count==0||(ring[ring.Count-1]-corner).sqrMagnitude>1e-5f)ring.Add(corner);
                }
                if(ring.Count>1&&(ring[0]-ring[ring.Count-1]).sqrMagnitude<1e-5f)ring.RemoveAt(ring.Count-1);
                int n=ring.Count;if(n<3)return;
                var corners=new Vector3[n*2];var faces=new List<int>();
                for(int i=0;i<n;i++)
                {
                    corners[i]=ring[i]+Vector3.up*low;corners[n+i]=ring[i]+Vector3.up*high;int next=(i+1)%n;
                    faces.AddRange(new[]{i,n+i,next,next,n+i,n+next});
                    if(i>0&&i<n-1){faces.AddRange(new[]{0,i,i+1});faces.AddRange(new[]{n,n+i+1,n+i});}
                }
                var mesh=new Mesh{name=prop.name+" solid"};mesh.vertices=corners;mesh.triangles=faces.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
                var saved=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+FoundryZone+"-"+prop.name.Replace(" ","")+"-Solid.asset");
                // On a child of its own: a collision mesh on the drawn body is read as what that body draws (in play a static body's filter holds its batch), and this one has no uvs.
                var holder=new GameObject(prop.name+" solid");holder.transform.SetParent(prop.transform,false);holder.layer=prop.layer;holder.isStatic=true;
                var solid=holder.AddComponent<MeshCollider>();solid.sharedMesh=saved;solid.convex=true;
            }
            static void Dress(ZoneKit kit)
            {
                var rng=new System.Random(83);float R()=>(float)rng.NextDouble();
                WarmPools.Clear();
                Embers=kit.Glow("Embers",false,new Color(1.9f,1.05f,.5f,1));LitSmoke=kit.Glow("LitSmoke",true,new Color(.95f,.62f,.42f,1));Steam=kit.Glow("Steam",true,new Color(.8f,.86f,.86f,1));
                // ---- along every wall foot: a point every few metres a metre or two in from the wall, facing the room; dropped (after its draws) where it would stand on a way, a
                // trough, a prompt, a fight's middle or the camera's line to a floor.
                int placed=0;
                for(float x=-40;x<50;x+=2.6f)for(float z=-6;z<290;z+=2.6f)
                {
                    var p=new Vector2(x+R()*1.6f,z+R()*1.6f);float clear=FloorAt(p,out _);bool keep=R()<.55f;int pick=rng.Next(Tall.Length);float size=R(),yaw=R()*360;
                    if(!keep||clear>-.9f||clear<-2.6f)continue;
                    // Which wall is it against: the way out of the floor from here.
                    var outward=new Vector2(FloorAt(p+Vector2.right*.6f,out _)-FloorAt(p-Vector2.right*.6f,out _),FloorAt(p+Vector2.up*.6f,out _)-FloorAt(p-Vector2.up*.6f,out _));
                    if(outward.sqrMagnitude<.0001f)continue;outward.Normalize();
                    bool back=Vector2.Dot(outward,Away)>.35f;
                    var (key,fallback,small,large)=back?Tall[pick]:Low[pick%Low.Length];float tall=Mathf.Lerp(small,large,size);
                    // Kept off by its own size as well: a crate centred clear of a trough stood with its corner in the pour's strip.
                    if(NearWay(p,2.4f+tall*.3f)||NearTrough(p,.8f+tall*.5f)||NearPrompt(p,3)||InFight(p,4)||(tall>2.4f&&ShadowsAFloor(p,tall))||Vector2.Distance(p,CageAt)<9||Vector2.Distance(p,PressAt)<6||NearMachine(p,.6f+tall*.3f))continue;
                    if(Racks.Any(r=>Vector2.Distance(p,r)<4)||Vector2.Distance(p,G)<17&&p.y>155&&p.y<203)continue;
                    string piece=ZoneKit.Kit(key,fallback);
                    // Slag, not soil, and built: a heap or rubble here is the furnaces' leavings (SlagHeap), where the kit's pieces were one dark chunk repeated (s48/061). Kept off a
                    // pour's strip, a way and a prompt by its own spread, which is wider than the piece's was tall.
                    if(key=="HushwellRubble"||key=="DressSpoilHeap")
                    {
                        float heapTall=key=="DressSpoilHeap"?tall*.8f:tall*.7f,spread=heapTall*1.45f;
                        if(NearTrough(p,spread)||NearWay(p,spread+1)||NearPrompt(p,spread+2))continue;
                        SlagHeap(kit,"Foundry clutter "+placed,p,heapTall,830+placed*13,key=="DressSpoilHeap"&&size>.4f);placed++;continue;
                    }
                    var prop=kit.Prop(piece,new Vector3(p.x,0,p.y),tall,yaw);prop.name="Foundry clutter "+placed++;
                }
                Debug.Log("FOUNDRY_CLUTTER "+placed);
                Furnaces(kit);Pillars(kit);Rails(kit,rng);Decking(kit,rng);Stairs(kit);Moulds(kit,rng);Gantry(kit);Lamps(kit);Haze(kit);HeartDress(kit);
                // Three floors, three rooms. The charging floor: ore, skips and charge, a crucible over it. The gallery: moulds, ladles, the pour, and what it makes (rings cooling
                // in their rack, clamps for fitting, Bridles half made in their jigs). The heart: the cage and what holds it down.
                Crane(kit,"Charging crane",108.6f,Charge,-23.2f,23.2f,4.4f);Bins(kit);Hoist(kit);Quench(kit,"Quench trough (charging floor)",new Vector2(19.8f,104.5f),0,3.2f);
                Crane(kit,"Gallery crane",197,Cast,-26.8f,26.8f,-7.5f);
                // No more cranes than these two: three more, hung over the charging floor's middle and the gallery's south half for something hot in those frames (s48/080, s48/216),
                // laid bands of three fights' floors out of the play view behind their girders and crucibles, where a marking would not be seen (NothingHungOverheadHidesAFightsFloorFrom
                // TheCamera). A crane hangs only where what it hides lies past a fight's floor; the ladle stands, the furnaces and the flasks light those rooms.
                RingRack(kit,"Ring rack",new Vector2(-24,183.6f),4,5,3);Quench(kit,"Quench trough (gallery)",new Vector2(-24,189.6f),0,2.8f);
                ClampRow(kit,new Vector2(24,184),new Vector2(0,1.6f),3,270);kit.Prop(ZoneKit.Kit("DressCableDrum","DressCargoPallet"),new Vector3(23.8f,0,190.2f),1.2f,90).name="Bridle wire drum";
                Jig(kit,"A Bridle half made",new Vector2(-19.8f,200.6f),3.6f,20);Jig(kit,"A Bridle half made (east)",new Vector2(20.8f,201),3.4f,-30);
                HeartWork(kit);
                // The heat a stranger names it by (the whole-game capture, November 20): the yard's smithy, its slag car and its pig iron; the gallery's furnace in its east wall, a
                // second rack of castings cooling by its west wall, the flasks waiting by the south room's north wall; the launders round the heart's back rim.
                FlameSheet=Hearths.Flame(kit);
                Smithy(kit,SmithyAt);SlagCar(kit,new Vector2(-16,35.6f));PigRack(kit,new Vector2(16.9f,31.6f),2.8f);
                // Where a party walks and fights through the wheel hall and the charging floor's middle, with the furnaces out of the frame at the room's far end (s48/075, s48/080).
                LadleStand(kit,"Ladle stand (wheel hall)",new Vector2(16.3f,47.6f),-90);LadleStand(kit,"Ladle stand (charging floor)",new Vector2(20.2f,85.6f),-90);
                SlagCar(kit,new Vector2(-19.4f,85.2f));LadleStand(kit,"Ladle stand (gallery)",new Vector2(20.5f,158.8f),0);
                // Where each room is first stood in: a pair of full ladles either side of the way in from the gangway and from the gallery's door, one at the yard's top, and a furnace
                // mouth in the wheel hall's north wall: the frame a party enters a room by has its heat in it.
                foreach(int side in new[]{-1,1}){LadleStand(kit,"Ladle stand (charging entry "+(side<0?"west":"east")+")",new Vector2(side*7.6f,82.9f),0);LadleStand(kit,"Ladle stand (gallery door "+(side<0?"west":"east")+")",new Vector2(side*8,158.8f),0);}
                LadleStand(kit,"Ladle stand (yard)",new Vector2(9.5f,42.5f),180);WallFurnace(kit,"Wheel hall furnace",new Vector2(-15.5f,64),Vector2.down);
                WallFurnace(kit,"Gallery furnace",new Vector2(25.9f,170),Vector2.left);RingRack(kit,"Ring rack (south)",new Vector2(-24.2f,170),4,4,2);
                Flasks(kit,new Vector2(-15.6f,152.9f),0,7);Flasks(kit,new Vector2(15.6f,152.9f),0,11);
                Launders(kit);
                GratedRunners(kit);
            }
            /// <summary>The heart's own: the vein shows through grated vents high in its north and east rim wall (lit from behind, three to five metres up: on the wall, never on
            /// the floor), and two great ducts climb out of the furnace's root either side of the dais and away into the dark.</summary>
            static void HeartDress(ZoneKit kit)
            {
                var iron=new ZoneKit.Shape();var glow=new ZoneKit.Shape();
                foreach(float bearing in new[]{-52f,-28f,28f,52f,100f,122f})
                {
                    var d=new Vector3(Mathf.Sin(bearing*Mathf.Deg2Rad),0,Mathf.Cos(bearing*Mathf.Deg2Rad));var at=new Vector3(C.x,Deep,C.y)+d*(ArenaRadius+1.4f);var turn=Quaternion.LookRotation(-d);
                    iron.Slot(0).Box(at+Vector3.up*3.7f,new Vector3(2.4f,2.2f,.6f),turn,IronDark,IronPale);
                    for(int i=0;i<5;i++)iron.Slot(0).Box(at+Vector3.up*3.7f+turn*new Vector3(-.8f+i*.4f,0,-.36f),new Vector3(.08f,1.7f,.1f),turn,IronDark,IronPale);
                    glow.Slot(0).Quad(at+turn*new Vector3(-1,2.85f,-.31f),at+turn*new Vector3(1,2.85f,-.31f),at+turn*new Vector3(1,4.55f,-.31f),at+turn*new Vector3(-1,4.55f,-.31f),-d,Color.white,.4f);
                }
                foreach(int side in new[]{-1,1})
                {
                    var foot=new Vector3(side*15.5f,Deep,281);
                    iron.Slot(0).Tube(new[]{foot+Vector3.down*.5f,foot+Vector3.up*3,foot+new Vector3(side*1.4f,7,2),foot+new Vector3(side*2.2f,16,4)},new[]{1.1f,1.05f,1f,1f},14,IronDark,Rust,1,.4f);
                    for(float h=1;h<6;h+=1.6f)iron.Slot(0).Tube(new[]{foot+Vector3.up*(h-.12f),foot+Vector3.up*(h+.12f)},new[]{1.2f,1.2f},14,IronPale,IronPale);
                    // A fire door at the duct's root, facing the room: the vein's heat where the camera sees the dais, on the duct and a hand over the floor, never on it.
                    var toC=new Vector3(C.x-foot.x,0,C.y-foot.z).normalized;var turn=Quaternion.LookRotation(toC);var door=foot+toC*1.12f;
                    iron.Slot(0).Box(door+Vector3.up*1.05f,new Vector3(1.6f,1.6f,.2f),turn,IronDark,IronPale);
                    for(int i=0;i<4;i++)iron.Slot(0).Box(door+turn*new Vector3(-.45f+i*.3f,1.05f,.14f),new Vector3(.06f,1.1f,.06f),turn,IronDark,IronPale);
                    glow.Slot(0).Quad(door+turn*new Vector3(-.55f,.5f,.12f),door+turn*new Vector3(.55f,.5f,.12f),door+turn*new Vector3(.55f,1.6f,.12f),door+turn*new Vector3(-.55f,1.6f,.12f),toC,Color.white,.4f);
                }
                var vents=kit.Built("Heart vents and ducts",iron,Iron);var fire=kit.Built("Heart vents' fire",glow,Furnace);fire.transform.SetParent(vents.transform,true);
                var lights=new List<Light>();
                foreach(float bearing in new[]{-40f,40f,111f}){var d=new Vector2(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad));var at=C+d*(ArenaRadius-.4f);var light=kit.Lamp(new Vector3(at.x,4,at.y),10,1.6f,new Color(1,.62f,.36f));light.name="Vent light";lights.Add(light);}
                // The vein breathes through them, slower than a furnace; embers come off each and climb the wall, and smoke with its light in it hangs under the vault.
                Breathe(vents,new[]{fire.GetComponent<Renderer>()},lights.ToArray(),5.2f,.55f,1.05f);
                foreach(float bearing in new[]{-52f,-28f,28f,52f,100f,122f})
                {
                    var d=new Vector3(Mathf.Sin(bearing*Mathf.Deg2Rad),0,Mathf.Cos(bearing*Mathf.Deg2Rad));var at=new Vector3(C.x,Deep,C.y)+d*(ArenaRadius+.9f);
                    Rise(kit,"Vent embers "+bearing,at+Vector3.up*3.9f,new Vector3(1.6f,1.2f,1.6f),Embers,new Color(1,.6f,.28f,1),14,.03f,.06f,.7f,3.4f);
                }
                Rise(kit,"Smoke over the heart",new Vector3(0,Deep+8,272),new Vector3(40,3,22),LitSmoke,new Color(.6f,.38f,.28f,.05f),40,2,4,.12f,12);
            }
            /// <summary>Iron decking where the forge's work stands on the floor: in front of the furnaces' mouths, under the press (its bed: the square the ram comes down on), and
            /// over the wheel bay. Single plates, each a little off the next in tone and set, laid a hand under a danger marking's lift so a marking always lies on them.</summary>
            static void Decking(ZoneKit kit,System.Random rng)
            {
                var shape=new ZoneKit.Shape();int plates=0;
                void Plates(float x0,float x1,float z0,float z1,float size)
                {
                    for(float x=x0+size*.5f;x<x1;x+=size)for(float z=z0+size*.5f;z<z1;z+=size)
                    {
                        var p=new Vector2(x+((float)rng.NextDouble()-.5f)*.06f,z+((float)rng.NextDouble()-.5f)*.06f);float roll=(float)rng.NextDouble(),shade=(float)rng.NextDouble();if(!Walkable(p)||NearTrough(p,0)||roll<.08f)continue;
                        // Each plate its own: some scorched dark, some rusted, a few gone and the stone showing (dropped after their draws, so the rest stand where they stood).
                        float y=kit.Y(p.x,p.y);var tone=Color.Lerp(IronDark,IronPale,.1f+.6f*shade);if(roll>.78f)tone=Color.Lerp(tone,Rust,.55f);else if(roll<.2f)tone=ZoneKit.Shape.Dim(tone,.55f);
                        shape.Slot(0).Box(new Vector3(p.x,y+.015f,p.y),new Vector3(size-.06f,.03f,size-.06f),Quaternion.Euler(0,((float)rng.NextDouble()-.5f)*1.5f,0),ZoneKit.Shape.Dim(tone,.8f),tone,.5f);plates++;
                    }
                }
                Plates(-4.4f,22,108.6f,112,1.6f);Plates(PressAt.x-.2f,PressAt.x+4.2f,92.2f,99.8f,1.5f);Plates(35,45.6f,87,99,1.6f);Plates(-18,-8,44.6f,55.4f,1.6f);
                kit.Built("Iron decking",shape,Iron).GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                Debug.Log("FOUNDRY_DECKING "+plates);
            }
            /// <summary>The charging floor's three furnaces along its north wall: each a round iron shell standing half out of the wall, banded, its ring main and tuyeres round it; on
            /// its front the mouth, an iron frame round a grated door lit from inside a metre over the floor and breathing (<see cref="Breathing"/>), a hood over it, its light, lit smoke
            /// and embers going up; between them the charge they are fed, buckets of ore, and a slag pot. Their fire is on the shells, never the floor (D217): the floor before a mouth
            /// takes a soft pool of its light, under any marking's.</summary>
            static void Furnaces(ZoneKit kit)
            {
                const float back=114.6f,r=2.3f,face=back-r-.2f;
                foreach(float x in new[]{-1f,9f,18f})
                {
                    var iron=new ZoneKit.Shape();var glow=new ZoneKit.Shape();var foot=new Vector3(x,Charge,back);
                    iron.Slot(0).Tube(new[]{foot+Vector3.down*.2f,foot+Vector3.up*5.4f,foot+Vector3.up*6.9f},new[]{r,r,r*.76f},18,IronDark,Rust,1,.6f);
                    foreach(float h in new[]{.45f,2.95f,5.2f})iron.Slot(0).Tube(new[]{foot+Vector3.up*(h-.13f),foot+Vector3.up*(h+.13f)},new[]{r+.09f,r+.09f},18,IronDark,IronPale);
                    var main=new Vector3[19];var round=new float[19];for(int k=0;k<19;k++){float t=k*Mathf.PI*2/18;main[k]=foot+new Vector3(Mathf.Cos(t)*(r+.75f),4.4f,Mathf.Sin(t)*(r+.75f));round[k]=.24f;}
                    iron.Slot(0).Tube(main,round,8,IronDark,IronPale,1,.4f,false);
                    for(int k=0;k<7;k++){float t=Mathf.PI*(1.1f+k*.13f);var o=new Vector3(Mathf.Cos(t),0,Mathf.Sin(t));iron.Slot(0).Tube(new[]{foot+o*(r+.75f)+Vector3.up*4.3f,foot+o*(r+.3f)+Vector3.up*3.7f,foot+o*(r-.05f)+Vector3.up*3.5f},new[]{.11f,.11f,.13f},6,IronDark,IronPale);}
                    // The mouth's frame, proud of the shell: its sill, posts and lintel, the hood over it, the door's grate, and the fire behind the grate.
                    var m=new Vector3(x,Charge,face+.75f);
                    iron.Slot(0).Box(m+new Vector3(0,.5f,0),new Vector3(4.2f,1,1.5f),Quaternion.identity,IronDark,IronPale);
                    foreach(int k in new[]{-1,1})iron.Slot(0).Box(m+new Vector3(k*1.85f,2.2f,0),new Vector3(.5f,2.6f,1.5f),Quaternion.identity,IronDark,IronPale);
                    iron.Slot(0).Box(m+new Vector3(0,3.75f,-.1f),new Vector3(4.6f,.7f,1.7f),Quaternion.identity,IronDark,IronPale).Box(m+new Vector3(0,4.55f,.1f),new Vector3(3.8f,1,1.8f),Quaternion.Euler(-14,0,0),IronDark,Rust);
                    for(int i=0;i<6;i++)iron.Slot(0).Post(new Vector3(x-1.25f+i*.5f,Charge+1,face-.02f),2.4f,.05f,.05f,6,IronDark,IronPale);
                    glow.Slot(0).Quad(new Vector3(x-1.6f,Charge+1,face+.12f),new Vector3(x+1.6f,Charge+1,face+.12f),new Vector3(x+1.6f,Charge+3.4f,face+.12f),new Vector3(x-1.6f,Charge+3.4f,face+.12f),Vector3.back,Color.white,.4f);
                    var body=kit.Built("Furnace "+Mathf.RoundToInt(x),iron,Iron);var fire=kit.Built("Furnace "+Mathf.RoundToInt(x)+" fire",glow,Furnace);fire.transform.SetParent(body.transform,true);
                    ZoneKit.Block(body,new Vector3(x,Charge+3.4f,face+1.5f),new Vector3(4.6f,6.8f,3),0,"Furnace");body.isStatic=true;
                    // Named apart from the work lamps: the ground kit paints a pool of cold light under each "Lamp light", never this. Its own pool is painted warm.
                    // Held low: at 2 over nine metres, with its pool, the floor before the mouth came within .11 of a marking's rim from the camera (AMarkingReadsWhereverTheForgeIsHottest).
                    var light=kit.Lamp(new Vector3(x,2.2f,face-1.2f),7,1.3f,new Color(1,.68f,.4f));light.name="Furnace light";light.transform.SetParent(body.transform,true);
                    Breathe(body,new[]{fire.GetComponent<Renderer>()},new[]{light},3.1f+x*.05f);
                    Rise(kit,"Furnace "+Mathf.RoundToInt(x)+" embers",new Vector3(x,Charge+2.4f,face-.5f),new Vector3(2.6f,1.6f,.6f),Embers,new Color(1,.62f,.3f,1),40,.03f,.07f,.9f,3.6f);
                    Rise(kit,"Furnace "+Mathf.RoundToInt(x)+" smoke",new Vector3(x,Charge+5.4f,face-.2f),new Vector3(3.2f,2,1.4f),LitSmoke,new Color(.7f,.44f,.3f,.07f),16,1.4f,2.8f,.3f,9);
                    WarmPools.Add((new Vector2(x,face-2.9f),1.9f,.14f));
                }
                // The charge: buckets of ore between the mouths, and a slag pot west of them.
                foreach(var (bx,bz,slag) in new[]{(4.8f,111.2f,false),(13.5f,111.2f,false),(-5.2f,111.6f,true)})Bucket(kit,new Vector2(bx,bz),slag);
            }
            /// <summary>A charge bucket of ore by a furnace's mouth, or a slag pot (its load the furnace's grey leavings).</summary>
            static void Bucket(ZoneKit kit,Vector2 at,bool slag)
            {
                var s=new ZoneKit.Shape();var foot=On(kit,at);float r=.66f,tall=slag?1:1.4f;
                s.Slot(0).Tube(new[]{foot,foot+Vector3.up*tall},new[]{r*.84f,r},14,IronDark,IronPale,1,.5f);
                s.Slot(0).Tube(new[]{foot+Vector3.up*(tall-.1f),foot+Vector3.up*(tall+.03f)},new[]{r+.05f,r+.05f},14,IronDark,IronPale,1,.5f,false);
                var bail=new Vector3[9];var round=new float[9];for(int k=0;k<9;k++){float t=k/8f*Mathf.PI;bail[k]=foot+new Vector3(Mathf.Cos(t)*(r+.06f),tall+Mathf.Sin(t)*r*.8f,0);round[k]=.035f;}
                s.Slot(0).Tube(bail,round,5,IronDark,IronPale);
                var load=slag?new Color(.38f,.37f,.38f):OreTop;
                for(int k=0;k<6;k++){float t=k*1.05f;var p=k==5?foot+Vector3.up*(tall+.12f):foot+new Vector3(Mathf.Cos(t)*r*.45f,tall+.04f,Mathf.Sin(t)*r*.45f);s.Slot(1).Lump(p,new Vector3(.24f,.16f,.22f),k+Mathf.RoundToInt(at.x*10),.4f,load,OreUnder,8,5);}
                var bucket=kit.Built((slag?"Slag pot ":"Charge bucket ")+Mathf.RoundToInt(at.x),s,Iron,Soot);
                ZoneKit.Block(bucket,foot+Vector3.up*tall*.5f,new Vector3(r*2,tall,r*2),0,"Bucket");bucket.isStatic=true;
            }
            /// <summary>Iron pillars set half into the north and east walls of each room, a girder over each pair: the forge's frame.</summary>
            static void Pillars(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();int count=0;
                void Pillar(Vector2 p,float floor){var foot=new Vector3(p.x,floor,p.y);shape.Slot(0).Box(foot+Vector3.up*3.6f,new Vector3(.7f,7.2f,.7f),Quaternion.identity,IronDark,IronPale).Box(foot+Vector3.up*.3f,new Vector3(1.1f,.6f,1.1f),Quaternion.identity,IronDark,IronPale);count++;}
                foreach(float x in new[]{-12f,-7.6f,7.6f,12f})Pillar(new Vector2(x,64.4f),Charge);
                foreach(float z in new[]{14f,26f,36f})Pillar(new Vector2(18.8f,z),Charge);
                foreach(float z in new[]{44f,52f,62f})Pillar(new Vector2(18.8f,z),Charge);
                foreach(float z in new[]{86f,108f})Pillar(new Vector2(22.8f,z),Charge);
                foreach(float x in new[]{-20f,-8f,8f,20f})Pillar(new Vector2(x,203.8f),Cast);
                foreach(float z in new[]{162f,174f,186f,198f})Pillar(new Vector2(26.8f,z),Cast);
                foreach(float z in new[]{138f,150f})Pillar(new Vector2(-26.8f,z),Cast);
                kit.Built("Iron pillars",shape,Iron);Debug.Log("FOUNDRY_PILLARS "+count);
            }
            /// <summary>The skips' rails in the yard and the skip store, with their buffers; ore skips standing on them.</summary>
            static void Rails(ZoneKit kit,System.Random rng)
            {
                var shape=new ZoneKit.Shape();
                void Track(Vector2 a,Vector2 b,float floor)
                {
                    var dir=(b-a).normalized;var side=new Vector2(-dir.y,dir.x);float length=Vector2.Distance(a,b);
                    foreach(int k in new[]{-1,1}){var p0=a+side*k*.55f;var p1=b+side*k*.55f;shape.Slot(0).Beam(new Vector3(p0.x,floor+.06f,p0.y),new Vector3(p1.x,floor+.06f,p1.y),.1f,.1f,IronPale,IronPale);}
                    for(float s=.4f;s<length;s+=.9f){var c=a+dir*s;shape.Slot(1).Box(new Vector3(c.x,floor+.02f,c.y),new Vector3(1.6f,.06f,.24f),Quaternion.LookRotation(new Vector3(side.x,0,side.y)),new Color(.5f,.44f,.4f),new Color(.6f,.54f,.48f));}
                }
                Track(new Vector2(-35.5f,16),new Vector2(-35.5f,32),Charge);Track(new Vector2(-16,11),new Vector2(-16,39),Charge);Track(new Vector2(-19.4f,84),new Vector2(-19.4f,109),Charge);
                kit.Built("Skip rails",shape,Iron,Soot);
                // Skips of ore built here: the kit's ore cart carried the moon's crystal, lit cyan by its own emission, which no tint reaches.
                foreach(var (x,z,yaw) in new[]{(-35.5f,20f,0f),(-35.5f,25.5f,0f),(-16f,16f,0f),(-16f,31f,180f),(-19.4f,90f,0f),(-19.4f,99.6f,0f)})Skip(kit,new Vector2(x,z),yaw);
                foreach(var (x,z,yaw) in new[]{(-35.5f,33f,0f),(-16f,40.2f,0f),(-19.4f,110f,0f)})kit.Prop(ZoneKit.Kit("DressRailBuffer","DressCargoPallet"),new Vector3(x,0,z),.9f,yaw);
                // Ore in heaps where the skips are filled, in the skip store's corners.
                int ore=0;foreach(var (x,z) in new[]{(-24.8f,17.4f),(-24.8f,30.8f)})SlagHeap(kit,"Ore heap "+(++ore),new Vector2(x,z),1.1f,610+ore,false,true);
            }
            /// <summary>An ore skip on its rails: a riveted iron tub on its frame and four wheels, heaped with dark iron ore.</summary>
            static void Skip(ZoneKit kit,Vector2 at,float yaw)
            {
                var s=new ZoneKit.Shape();var turn=Quaternion.Euler(0,yaw,0);var foot=On(kit,at);
                s.Slot(0).Box(foot+Vector3.up*.36f,new Vector3(1.2f,.12f,1.7f),turn,IronDark,IronPale).Box(foot+Vector3.up*.78f,new Vector3(1.1f,.72f,1.5f),turn,IronDark,Rust).Box(foot+Vector3.up*1.17f,new Vector3(1.24f,.07f,1.66f),turn,IronDark,IronPale);
                foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1})s.Slot(0).Tube(new[]{foot+turn*new Vector3(a*.5f,.22f,b*.52f),foot+turn*new Vector3(a*.62f,.22f,b*.52f)},new[]{.21f,.21f},10,IronDark,IronPale);
                for(int k=0;k<7;k++){var o=new Vector3(((k*37)%7/7f-.45f)*.8f,1.2f+(k%3)*.06f,((k*53)%7/7f-.45f)*1.1f);s.Slot(1).Lump(foot+turn*o,new Vector3(.26f,.18f,.24f),k+Mathf.RoundToInt(at.y*7),.42f,OreTop,OreUnder,8,5);}
                s.Slot(1).Lump(foot+Vector3.up*1.32f,new Vector3(.4f,.22f,.5f),Mathf.RoundToInt(at.y*3),.35f,OreTop,OreUnder,8,5,.25f,turn);
                var skip=kit.Built("Ore skip "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),s,Iron,Soot);
                ZoneKit.Block(skip,foot+Vector3.up*.7f,new Vector3(1.24f,1.4f,1.7f),yaw,"Skip");skip.isStatic=true;
            }
            /// <summary>Treads on every stair, so each reads as built and not as a slope.</summary>
            static void Stairs(ZoneKit kit)
            {
                var treads=new ZoneKit.Shape();
                void Stair(Vector2 from,Vector2 to,float wide,float step){var run=to-from;float length=run.magnitude;var along=run/length;float yaw=Mathf.Atan2(along.x,along.y)*Mathf.Rad2Deg;
                    for(float s=step*.5f;s<length;s+=step){var c=from+along*s;float y=kit.Y(c.x,c.y);treads.Slot(0).Box(new Vector3(c.x,y-.05f,c.y),new Vector3(wide,.12f,step*.94f),Quaternion.Euler(0,yaw,0),StoneDark,StoneWarm,.55f);}}
                Stair(new Vector2(-13,121),new Vector2(-13,135),7.6f,.45f);Stair(new Vector2(0,203),new Vector2(0,217),8.6f,.45f);Stair(new Vector2(43,106),new Vector2(43,110),3.6f,.4f);
                kit.Built("Stair treads",treads,WallMat);
            }
            /// <summary>The casting gallery's own things: rib moulds bedded in the floor along its walls (low sand beds, a rib's curve pressed in each), finished ribs leaning on the
            /// north wall, and the loft's patterns hung on its walls.</summary>
            static void Moulds(ZoneKit kit,System.Random rng)
            {
                var sand=new ZoneKit.Shape();var ribs=new ZoneKit.Shape();var newest=new ZoneKit.Shape();
                void Mould(Vector2 at,float yaw,float length)
                {
                    var turn=Quaternion.Euler(0,yaw,0);var foot=On(kit,at);
                    sand.Slot(0).Box(foot+Vector3.up*.08f,turn*new Vector3(1.6f,.16f,length),turn,new Color(.84f,.78f,.68f),new Color(.9f,.84f,.72f));
                    var arc=new Vector3[9];var thick=new float[9];for(int i=0;i<9;i++){float t=i/8f;arc[i]=foot+turn*new Vector3(Mathf.Sin(t*Mathf.PI)*.45f-.2f,.17f,(t-.5f)*length*.9f);thick[i]=.1f;}
                    ribs.Slot(0).Tube(arc,thick,6,IronDark,IronDark,1,.4f,false);
                }
                // In the gallery's south room along its north side (behind the drones' rails), and along the gallery's own east and west walls, clear of the racks and the middle.
                foreach(float x in new[]{-21f,-8f,6f,20f})Mould(new Vector2(x,151.6f),90,7);
                foreach(float z in new[]{164f,176f,196f})foreach(int side in new[]{-1,1})Mould(new Vector2(side*23.4f,z),0,7);
                // Finished ribs: tall iron arcs standing against the gallery's north wall, a rib's width apart: nineteen were poured here.
                // From the heart stair's west wall to the half-made Bridle's jig in the corner.
                for(int i=0;i<5;i++)
                {
                    float x=-15.2f+i*2.3f;var foot=new Vector3(x,Cast,202.2f);var arc=new Vector3[11];var thick=new float[11];
                    for(int k=0;k<11;k++){float t=k/10f;arc[k]=foot+new Vector3(.3f*Mathf.Sin(t*Mathf.PI),Mathf.Sin(t*Mathf.PI*.5f)*5.4f,-Mathf.Sin(t*Mathf.PI)*.8f);thick[k]=Mathf.Lerp(.24f,.14f,t);}
                    // The newest, nearest the stair, still dull red in it: upright against the wall, never the floor.
                    (i==4?newest:ribs).Slot(0).Tube(arc,thick,7,IronDark,IronPale,1,.4f);
                }
                kit.Built("Rib moulds",sand,Sand);var finished=kit.Built("Ribs",ribs,Iron);var cooling=kit.Built("Ribs (the newest, cooling)",newest,Cooling);cooling.transform.SetParent(finished.transform,true);
                // The loft: rib patterns of pale timber on its north and east walls, a bench.
                var patterns=new ZoneKit.Shape();
                for(int i=0;i<4;i++){var foot=new Vector3(31.5f+i*2.4f,Cast,153.3f);var arc=new Vector3[9];var thick=new float[9];for(int k=0;k<9;k++){float t=k/8f;arc[k]=foot+new Vector3((t-.5f)*1.8f,Mathf.Sin(t*Mathf.PI)*1.6f,0);thick[k]=.07f;}patterns.Slot(0).Tube(arc,thick,5,new Color(.9f,.78f,.58f),new Color(.96f,.86f,.66f));}
                kit.Built("Rib patterns",patterns,Sand);
                kit.Prop(ZoneKit.Kit("DressWorkbench","DressCargoPallet"),new Vector3(40.4f,0,142),1.1f,270);
            }
            /// <summary>The heart's gantry: an iron walkway on brackets along the north wall at five metres, its rail, and the stair it comes down from the gallery's gantry door by.</summary>
            static void Gantry(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();float y=Deep+5;
                // Over the dais's north edge, its brackets into the wall behind: the wall climbs too steeply past the dais to hang anything further back.
                shape.Slot(0).Box(new Vector3(0,y-.12f,285.8f),new Vector3(30,.24f,2.4f),Quaternion.identity,IronDark,IronPale);
                for(float x=-14;x<=14;x+=2){shape.Slot(0).Post(new Vector3(x,y,284.7f),1.05f,.04f,.04f,6,IronDark,IronPale);shape.Slot(0).Beam(new Vector3(x,y-.2f,286.8f),new Vector3(x,y-1.8f,287.6f),.12f,.12f,IronDark,IronPale);}
                shape.Slot(0).Beam(new Vector3(-14,y+1,284.7f),new Vector3(14,y+1,284.7f),.07f,.07f,IronPale);
                // The door at its east end, set in the wall: Vessa's way on and off it (the release).
                var door=new Vector3(15.6f,y,285.8f);
                foreach(int k in new[]{-1,1})shape.Slot(0).Box(door+new Vector3(0,1.4f,k*1.3f),new Vector3(.5f,2.8f,.4f),Quaternion.identity,IronDark,IronPale);
                shape.Slot(0).Box(door+new Vector3(0,2.9f,0),new Vector3(.6f,.4f,3),Quaternion.identity,IronDark,IronPale);
                kit.Built("Heart gantry",shape,Iron);
            }
            /// <summary>The Muster's work lamps: cold white on posts and brackets at room edges, a light under each only where a room needs one (one floor renderer takes about six).</summary>
            static void Lamps(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();var glass=new ZoneKit.Shape();
                foreach(var (x,z,floor,lit) in new[]{(-6f,8.5f,Charge,true),(15.5f,39f,Charge,false),(-15.5f,61.5f,Charge,true),(14.6f,63.4f,Charge,false),(-19.5f,96f,Charge,false),(33.5f,104.5f,Charge,true),
                    (-24f,144f,Cast,true),(24.2f,135.5f,Cast,false),(40.6f,136f,Cast,false),(-24.2f,170f,Cast,true),(24.2f,182f,Cast,true),(-10f,217.6f,Deep,true),(10f,217.6f,Deep,false)})
                {
                    var foot=new Vector3(x,floor,z);shape.Slot(0).Post(foot,3.2f,.08f,.06f,6,IronDark,IronPale).Tube(new[]{foot+Vector3.up*3.1f,foot+Vector3.up*3.35f+Vector3.forward*.5f},new[]{.05f,.05f},6,IronPale,IronPale);
                    glass.Slot(0).Lump(foot+Vector3.up*3.15f+Vector3.forward*.55f,new Vector3(.16f,.12f,.16f),(int)(x*7+z),.04f,Color.white,Color.white,8,5);
                    if(lit)kit.Lamp(new Vector3(x,3,z+.55f),10,1.8f,new Color(.82f,.9f,1));
                }
                kit.Built("Work lamp posts",shape,Iron);kit.Built("Work lamp glass",glass,Lamp);
            }
            /// <summary>Smoke under the roof and a little ash drifting through every room: the forge is working.</summary>
            static void Haze(ZoneKit kit)
            {
                var smoke=kit.Glow("Smoke",true,new Color(.6f,.56f,.54f,1));
                kit.Drift("Smoke under the roof",new Vector3(2,7,140),new Vector3(90,3,290),smoke,new Color(.42f,.38f,.36f,.08f),260,4,8,.15f,26);
                var ash=kit.Glow("Ash",false,new Color(.9f,.86f,.82f,1));
                kit.Drift("Ash",new Vector3(2,2.4f,140),new Vector3(90,4,290),ash,new Color(.78f,.74f,.7f,.5f),700,.03f,.07f,.18f,14);
            }

            // ---------------------------------------------------------------- heat and machinery
            /// <summary>Where the machines stand (plan centre, radius): the wall-foot clutter keeps off them.</summary>
            static readonly (Vector2 at,float r)[] Machines=
            {
                (new(-1,113.4f),3.4f),(new(9,113.4f),3.4f),(new(18,113.4f),3.4f),(new(4.8f,111.2f),1.2f),(new(13.5f,111.2f),1.2f),(new(-5.2f,111.6f),1.2f),
                (new(-19.4f,85),2),(new(-19.4f,90),2),(new(-19.4f,96),2),(new(-19.4f,102),2),(new(-19.4f,108),2),(new(19.8f,104.5f),2.2f),
                (new(-9.8f,63.2f),2.6f),(new(9.8f,63.2f),2.6f),(new(16.8f,36),1.8f),(new(-24.8f,17.4f),2.2f),(new(-24.8f,30.8f),2.2f),
                (new(-35.5f,20),1.4f),(new(-35.5f,25.5f),1.4f),(new(-16,16),1.4f),(new(-16,31),1.4f),
                (new(-24,183.6f),2.8f),(new(-24,189.6f),2.2f),(new(24,185.6f),2.6f),(new(23.8f,190.2f),1.4f),(new(-19.8f,200.6f),2.6f),(new(20.8f,201),2.4f),
                (new(-10,283),1.8f),(new(10,283),1.8f),(new(-10.8f,279.4f),2),
                // The heat (November 20): the smithy and its anvil, rack and tub, the slag car, the pig iron, the hoist's casting, the gallery's furnace and its second ring rack, the flasks.
                (SmithyAt,2.2f),(SmithyAt+new Vector2(-2,-.3f),1.1f),(SmithyAt+new Vector2(-.6f,-2),1.1f),(SmithyAt+new Vector2(0,1.9f),1),(new(-16,35.6f),1.6f),(new(16.9f,31.6f),1.8f),
                (new(14.3f,36),1.2f),(new(25,170),2.4f),(new(-24.2f,170),2.6f),(new(-15.6f,152.9f),1.8f),(new(15.6f,152.9f),1.8f),(new(16.3f,47.6f),1.5f),(new(20.2f,85.6f),1.5f),
                (new(-19.4f,85.2f),1.6f),(new(20.5f,158.8f),1.5f),(new(-7.6f,82.9f),1.5f),(new(7.6f,82.9f),1.5f),(new(-8,158.8f),1.5f),(new(8,158.8f),1.5f),(new(9.5f,42.5f),1.5f),
                (new(-15.5f,63.2f),2.4f),
            };
            static bool NearMachine(Vector2 p,float within){foreach(var (at,r) in Machines)if(Vector2.Distance(p,at)<r+within)return true;return false;}
            /// <summary>Warm light painted on the floor (plan centre, radius, strength): before a furnace's mouth, under a crucible. The floor is one renderer and takes six lights at
            /// most; the pool is what says the light reaches it. Soft and pale, never a marking's orange; laid by <see cref="Ground"/>.</summary>
            static readonly List<(Vector2 at,float r,float a)> WarmPools=new();
            static Material Embers,LitSmoke,Steam;
            static Breathing Breathe(GameObject on,Renderer[] glows,Light[] lights,float period,float low=.62f,float high=1.08f)
            {var breath=on.AddComponent<Breathing>();breath.glows=glows;breath.lights=lights;breath.period=period;breath.low=low;breath.high=high;return breath;}
            /// <summary>Motes going up from something hot: embers, smoke with its light in it, steam (a drift given a slow climb and a waver). Absolute place and size.</summary>
            static ParticleSystem Rise(ZoneKit kit,string name,Vector3 at,Vector3 size,Material glow,Color colour,int count,float small,float large,float climb,float life)
            {
                var motes=kit.Drift(name,at,size,glow,colour,count,small,large,0,life);
                var velocity=motes.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
                velocity.x=new ParticleSystem.MinMaxCurve(-.1f,.1f);velocity.y=new ParticleSystem.MinMaxCurve(climb*.6f,climb*1.3f);velocity.z=new ParticleSystem.MinMaxCurve(-.1f,.1f);
                var noise=motes.noise;noise.enabled=true;noise.strength=small<.2f?.35f:.08f;noise.frequency=.5f;noise.scrollSpeed=.3f;
                return motes;
            }
            /// <summary>A travelling crane across a room near its back wall: its girder ten and a half metres up on end carriages (on columns from the walls' tops), its trolley, and a
            /// crucible hung from it on long ropes four and a half metres over the floor, its open top full of metal: lit from inside and breathing, a warm light under it, embers and lit
            /// smoke going up. The girder is above the fixed camera's frame from anywhere on the floor (at seven and a half metres across a room's middle it crossed the frame as a band
            /// that hid the heroes, the fight and the furnaces behind it); the crucible hangs where nothing the camera looks past it at is a fight's floor. Nothing of it is solid, and
            /// the scenery fade cuts it round a hero behind it.</summary>
            static void Crane(ZoneKit kit,string name,float z,float floor,float west,float east,float at)
            {
                var iron=new ZoneKit.Shape();var glow=new ZoneKit.Shape();float y=floor+10.5f;
                iron.Slot(0).Beam(new Vector3(west,y,z),new Vector3(east,y,z),.5f,.8f,IronDark,IronPale).Beam(new Vector3(west,y+.45f,z),new Vector3(east,y+.45f,z),.9f,.1f,IronDark,IronPale)
                    .Beam(new Vector3(west,y-.45f,z),new Vector3(east,y-.45f,z),.9f,.1f,IronDark,IronPale);
                foreach(float end in new[]{west,east})
                {
                    iron.Slot(0).Box(new Vector3(end,y+.1f,z),new Vector3(1.2f,.9f,2.8f),Quaternion.identity,IronDark,Rust);
                    float under=Height(new Vector2(end,z));if(under<y-.9f)iron.Slot(0).Box(new Vector3(end,(under+y)*.5f-.2f,z),new Vector3(.7f,y-under,.7f),Quaternion.identity,IronDark,IronPale);
                }
                // The trolley astride the girder, the crucible on its bail under it, the ropes between.
                var t=new Vector3(at,y,z);
                iron.Slot(0).Box(t+Vector3.up*.75f,new Vector3(1.5f,.5f,1.9f),Quaternion.identity,IronDark,Rust);
                foreach(int k in new[]{-1,1})iron.Slot(0).Box(t+new Vector3(0,.2f,k*.62f),new Vector3(1.5f,.7f,.14f),Quaternion.identity,IronDark,IronPale);
                var c=new Vector3(at,floor+4.5f,z);const float top=1.25f;
                iron.Slot(0).Tube(new[]{c,c+Vector3.up*top},new[]{.55f,.78f},16,Rust,IronDark,1,.5f,false).Disc(c,Vector3.down,.55f,16,IronDark);
                iron.Slot(0).Tube(new[]{c+Vector3.up*(top-.08f),c+Vector3.up*(top+.05f)},new[]{.83f,.83f},16,IronDark,IronPale,1,.5f,false);
                foreach(int k in new[]{-1,1})iron.Slot(0).Tube(new[]{c+new Vector3(k*.7f,.82f,0),c+new Vector3(k*.97f,.82f,0)},new[]{.1f,.1f},8,IronDark,IronPale);
                var bail=new Vector3[11];var round=new float[11];for(int k=0;k<11;k++){float a=k/10f*Mathf.PI;bail[k]=c+new Vector3(Mathf.Cos(a)*.95f,.82f+Mathf.Sin(a)*1.05f,0);round[k]=.06f;}
                iron.Slot(0).Tube(bail,round,6,IronDark,IronPale);
                // A block on the ropes over the bail, and the ropes from it up to the trolley out of the frame.
                var hook=c+Vector3.up*2.2f;iron.Slot(0).Box(hook,new Vector3(.34f,.44f,.26f),Quaternion.identity,IronDark,Rust);
                foreach(int k in new[]{-1,1})iron.Slot(0).Tube(new[]{hook+new Vector3(k*.1f,.2f,0),t+new Vector3(k*.3f,-.5f,0)},new[]{.035f,.035f},5,IronPale,IronPale);
                glow.Slot(0).Disc(c+Vector3.up*(top-.1f),Vector3.up,.76f,16,Color.white);
                var crane=kit.Built(name,iron,Iron);var lit=kit.Built(name+" melt",glow,Furnace);lit.transform.SetParent(crane.transform,true);
                // No shadow: a girder ten metres up laid a dark band right across the room below it, read as something on the floor; and the crucible's melt laid a round one
                // that the warm light round it turned lavender, a purple disc on the floor (render scoria-lookfix-07, gallery).
                foreach(var r in crane.GetComponentsInChildren<Renderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var light=kit.Lamp(new Vector3(at,3.9f,z),10,1.5f,new Color(1,.72f,.46f));light.name="Crucible light";light.transform.SetParent(crane.transform,true);
                Breathe(crane,new[]{lit.GetComponent<Renderer>()},new[]{light},4.2f,.7f,1.05f);
                ZoneKit.Fadeable(crane);
                Rise(kit,name+" embers",c+Vector3.up*(top+.3f),new Vector3(1,.3f,1),Embers,new Color(1,.62f,.3f,1),22,.03f,.06f,.8f,3.2f);
                Rise(kit,name+" smoke",c+Vector3.up*(top+1.6f),new Vector3(1.6f,1.2f,1.6f),LitSmoke,new Color(.7f,.44f,.3f,.06f),10,1.2f,2.4f,.3f,8);
                WarmPools.Add((new Vector2(at,z-.8f),2,.24f));
            }
            /// <summary>The ore bunkers' faces in the wheel hall's north wall either side of the gangway: riveted iron, a chute out of each at two and a half metres (over every head), its
            /// gate shut, ore spilt under it. What the skips are filled from.</summary>
            static void Bins(ZoneKit kit)
            {
                var rng=new System.Random(7);
                foreach(float x in new[]{-9.8f,9.8f})
                {
                    var iron=new ZoneKit.Shape();var ore=new ZoneKit.Shape();const float z=64.5f;var chute=Quaternion.Euler(-28,0,0);
                    iron.Slot(0).Box(new Vector3(x,Charge+2.6f,z),new Vector3(3.6f,5.2f,.4f),Quaternion.identity,IronDark,IronPale);
                    foreach(float k in new[]{-1.2f,0,1.2f})iron.Slot(0).Box(new Vector3(x+k,Charge+2.6f,z-.26f),new Vector3(.14f,5.2f,.14f),Quaternion.identity,IronDark,IronPale);
                    foreach(float h in new[]{1.3f,4})iron.Slot(0).Box(new Vector3(x,Charge+h,z-.26f),new Vector3(3.6f,.14f,.14f),Quaternion.identity,IronDark,IronPale);
                    iron.Slot(0).Box(new Vector3(x,Charge+2.6f,z-.95f),new Vector3(1.5f,.12f,1.5f),chute,IronDark,Rust);
                    foreach(int s in new[]{-1,1})iron.Slot(0).Box(new Vector3(x+s*.74f,Charge+2.8f,z-.95f),new Vector3(.08f,.42f,1.5f),chute,IronDark,IronPale);
                    iron.Slot(0).Box(new Vector3(x,Charge+2.45f,z-1.68f),new Vector3(1.5f,.5f,.08f),Quaternion.identity,IronDark,Rust);
                    for(int i=0;i<10;i++){var p=new Vector2(x+((float)rng.NextDouble()-.5f)*2.4f,z-.9f-(float)rng.NextDouble()*1.3f);float s=.08f+(float)rng.NextDouble()*.12f;ore.Slot(0).Lump(new Vector3(p.x,kit.Y(p.x,p.y)+s*.35f,p.y),new Vector3(s,s*.7f,s),i+Mathf.RoundToInt(x*3),.4f,OreTop,OreUnder,7,4);}
                    string side=x<0?"west":"east";var bin=kit.Built("Ore bunker "+side,iron,Iron);var spill=kit.Built("Ore bunker "+side+" spill",ore,Soot);spill.transform.SetParent(bin.transform,true);
                }
            }
            /// <summary>A chain hoist in the yard's north-east corner: a jib on its own mast against the east wall, the hoist's block, its chain and its hook (two and a half metres up).</summary>
            static void Hoist(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var mast=new Vector3(17.9f,kit.Y(17.9f,36),36);
                s.Slot(0).Post(mast,6.7f,.2f,.17f,10,IronDark,IronPale).Box(mast+Vector3.up*.2f,new Vector3(.8f,.4f,.8f),Quaternion.identity,IronDark,IronPale);
                var top=mast+Vector3.up*6.4f;
                s.Slot(0).Beam(top,top+Vector3.left*4.4f,.28f,.36f,IronDark,IronPale).Beam(mast+Vector3.up*4.4f,top+Vector3.left*2.8f+Vector3.down*.15f,.14f,.14f,IronDark,IronPale);
                var block=top+Vector3.left*3.6f+Vector3.down*.55f;s.Slot(0).Box(block,new Vector3(.44f,.6f,.36f),Quaternion.identity,IronDark,Rust);
                float hookAt=mast.y+2.6f;
                for(float h=block.y-.4f;h>hookAt;h-=.16f)s.Slot(0).Box(new Vector3(block.x,h,block.z),new Vector3(.05f,.14f,.09f),Quaternion.Euler(0,Mathf.Repeat(h*6.25f,2)<1?0:90,0),IronDark,IronPale);
                var hook=new Vector3[8];var round=new float[8];for(int k=0;k<8;k++){float a=k/7f*Mathf.PI*1.3f;hook[k]=new Vector3(block.x+Mathf.Sin(a)*.16f,hookAt-.16f+Mathf.Cos(a)*.16f,block.z);round[k]=.04f;}
                s.Slot(0).Tube(hook,round,6,IronDark,IronPale);
                var hoist=kit.Built("Chain hoist",s,Iron);ZoneKit.Block(hoist,mast+Vector3.up*3.35f,new Vector3(.5f,6.7f,.5f),0,"Hoist mast");hoist.isStatic=true;
                // On the hook, a Bridle ring just out of the gallery, slung in chain to cool where the yard's air moves: dull red going to orange, a metre and more over the floor, turned
                // to the camera so it reads round.
                var ring=new ZoneKit.Shape();var sling=new ZoneKit.Shape();var hookTip=new Vector3(block.x,hookAt-.2f,block.z);var middle=hookTip+Vector3.down*1.05f;
                var across=new Vector3(Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad),0,-Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad));var points=new Vector3[25];var thick=new float[25];
                for(int k=0;k<25;k++){float a=k*Mathf.PI*2/24;points[k]=middle+Vector3.up*Mathf.Sin(a)*.72f+across*Mathf.Cos(a)*.72f;thick[k]=.11f;}
                ring.Tube(points,thick,8,Color.white,Color.white,1,.5f,false);
                foreach(int k in new[]{-1,1})sling.Beam(hookTip,middle+Vector3.up*.6f+across*k*.38f,.05f,.05f,IronDark,IronPale);
                // Let down onto a cooling stand, a saddle on two legs, so its weight is off the chain: slung half a metre over bare floor it read as floating (QaPlanWorldTests).
                float ground=kit.Y(block.x,block.z),under=middle.y-.83f;var turn=Quaternion.Euler(0,SightlineAudit.Yaw,0);
                foreach(int k in new[]{-1,1})sling.Box(new Vector3(block.x,ground,block.z)+across*k*.36f+Vector3.up*(under-ground)*.5f,new Vector3(.12f,under-ground,.32f),turn,IronDark,IronPale);
                sling.Box(new Vector3(block.x,under-.03f,block.z),new Vector3(.9f,.08f,.36f),turn,IronDark,Rust);
                var casting=kit.Built("Casting on the hoist",ring,Casting);kit.Built("Casting sling",sling,Iron).transform.SetParent(casting.transform,true);
                ZoneKit.Block(casting,middle,new Vector3(1.6f,1.6f,.4f),SightlineAudit.Yaw,"Casting on the hoist");
                var glow=kit.Lamp(new Vector3(middle.x,0,middle.z),4.5f,.9f,new Color(1,.55f,.3f));glow.transform.position=middle;glow.name="Casting light";glow.transform.SetParent(casting.transform,true);
                Breathe(casting,new[]{casting.GetComponent<Renderer>()},new[]{glow},8,.86f,1.03f).colour=new Color(1.05f,.34f,.1f);
                Rise(kit,"Casting heat",middle+Vector3.up*.9f,new Vector3(1.2f,.3f,.6f),Embers,new Color(1,.55f,.25f,1),6,.02f,.04f,.5f,3);
                WarmPools.Add((new Vector2(middle.x-.4f,middle.z-2.2f),1.4f,.14f));
            }
            /// <summary>A quench trough: a riveted iron tank of coolant a hand under its lip and a little steam off it, what a casting is cooled in. Along z, or across it at yaw 90.</summary>
            static void Quench(ZoneKit kit,string name,Vector2 at,float yaw,float length)
            {
                var turn=Quaternion.Euler(0,yaw,0);var foot=On(kit,at);var s=new ZoneKit.Shape();var water=new ZoneKit.Shape();const float wide=1.3f,tall=.9f;
                s.Slot(0).Box(foot+Vector3.up*.06f,new Vector3(wide,.12f,length),turn,IronDark,IronPale);
                foreach(int k in new[]{-1,1})
                {
                    s.Slot(0).Box(foot+turn*new Vector3(k*(wide-.08f)*.5f,tall*.5f,0),new Vector3(.08f,tall,length),turn,IronDark,Rust).Box(foot+turn*new Vector3(0,tall*.5f,k*(length-.08f)*.5f),new Vector3(wide,tall,.08f),turn,IronDark,Rust);
                    s.Slot(0).Box(foot+turn*new Vector3(k*(wide-.04f)*.5f,tall+.03f,0),new Vector3(.14f,.06f,length+.08f),turn,IronDark,IronPale);
                }
                float w=wide*.5f-.09f,l=length*.5f-.09f,h=tall-.12f;
                water.Slot(0).Quad(foot+turn*new Vector3(-w,h,-l),foot+turn*new Vector3(-w,h,l),foot+turn*new Vector3(w,h,l),foot+turn*new Vector3(w,h,-l),Vector3.up,Color.white,.5f);
                var tank=kit.Built(name,s,Iron);var top=kit.Built(name+" water",water,Quenched);top.transform.SetParent(tank.transform,true);
                ZoneKit.Block(tank,foot+Vector3.up*tall*.5f,new Vector3(wide,tall,length),yaw,"Quench trough");tank.isStatic=true;
                bool across=Mathf.Abs(Mathf.Sin(yaw*Mathf.Deg2Rad))>.7f;
                Rise(kit,name+" steam",foot+Vector3.up*(tall+.4f),across?new Vector3(length*.8f,.3f,wide*.6f):new Vector3(wide*.6f,.3f,length*.8f),Steam,new Color(.8f,.86f,.86f,.05f),10,.6f,1.4f,.3f,5);
            }
            /// <summary>An iron rack against a west or east wall, the Bridle's rings cast here standing in it on edge to cool, the newest still dull red at the heart (on the rack, never
            /// the floor). The rings face the room's length, so the camera sees them round.</summary>
            static void RingRack(ZoneKit kit,string name,Vector2 at,float length,int rings,int warm)
            {
                var foot=On(kit,at);var s=new ZoneKit.Shape();var hot=new ZoneKit.Shape();var hottest=new ZoneKit.Shape();
                foreach(int k in new[]{-1,1})s.Slot(0).Box(foot+new Vector3(0,1.1f,k*length*.5f),new Vector3(1,2.2f,.14f),Quaternion.identity,IronDark,IronPale);
                foreach(float x in new[]{-.42f,.42f})s.Slot(0).Box(foot+new Vector3(x,.18f,0),new Vector3(.14f,.14f,length),Quaternion.identity,IronDark,IronPale);
                s.Slot(0).Box(foot+new Vector3(0,2.14f,0),new Vector3(1,.12f,length+.14f),Quaternion.identity,IronDark,IronPale);
                for(int i=0;i<rings;i++)
                {
                    float z=-length*.5f+(i+.5f)*length/rings;var c=foot+new Vector3(0,.98f,z);var ring=new Vector3[19];var round=new float[19];
                    for(int k=0;k<19;k++){float a=k*Mathf.PI*2/18;ring[k]=c+new Vector3(Mathf.Cos(a)*.74f,Mathf.Sin(a)*.74f,0);round[k]=.1f;}
                    // The newest still orange where it is thickest, the ones before it dull red, the rest cold: a rack of a shift's work cooling in order.
                    (i==rings-1&&warm>0?hottest:i>=rings-warm?hot:s).Slot(0).Tube(ring,round,8,IronDark,i%2==0?IronPale:Rust,1,.4f,false);
                }
                var rack=kit.Built(name,s,Iron);
                if(warm>1){var glow=kit.Built(name+" cooling",hot,Cooling);glow.transform.SetParent(rack.transform,true);}
                if(warm>0){var newest=kit.Built(name+" newest",hottest,Casting);newest.transform.SetParent(rack.transform,true);}
                ZoneKit.Block(rack,foot+Vector3.up*1.1f,new Vector3(1,2.2f,length),0,"Ring rack");rack.isStatic=true;
            }
            /// <summary>Clamps for a Bridle's feet, made here and stood in a row against a wall for fitting: the clamp model, unlit.</summary>
            static void ClampRow(ZoneKit kit,Vector2 from,Vector2 step,int count,float yaw)
            {
                var row=new GameObject("Clamps for fitting");
                for(int i=0;i<count;i++){var p=from+step*i;var clamp=kit.Prop(ZoneKit.Kit("BridleClamp","DressCargoPallet"),new Vector3(p.x,0,p.y),1.1f,yaw+(i%2)*8);clamp.name="Clamp for fitting "+i;Recast(clamp,ClampDark);clamp.transform.SetParent(row.transform,true);}
            }
            /// <summary>A Bridle half made in its jig: an iron ring on the floor and four uprights holding the work, and in it a cage of the Bridle's make, smaller than the heart's and
            /// unlit (nothing is in it yet).</summary>
            static void Jig(ZoneKit kit,string name,Vector2 at,float size,float yaw)
            {
                var foot=On(kit,at);float r=size*.52f;var s=new ZoneKit.Shape();
                var ring=new Vector3[25];var round=new float[25];for(int k=0;k<25;k++){float a=k*Mathf.PI*2/24;ring[k]=foot+new Vector3(Mathf.Cos(a)*r,.16f,Mathf.Sin(a)*r);round[k]=.14f;}
                s.Slot(0).Tube(ring,round,8,IronDark,IronPale,1,.4f,false);
                for(int k=0;k<4;k++)
                {
                    float a=(k*90+45+yaw)*Mathf.Deg2Rad;var p=foot+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);float up=size*.8f;
                    s.Slot(0).Post(p,up,.1f,.08f,6,IronDark,IronPale).Beam(p+Vector3.up*up,Vector3.Lerp(p,foot,.45f)+Vector3.up*up,.1f,.1f,IronDark,IronPale);
                }
                var frame=kit.Built(name+" jig",s,Iron);
                var cage=kit.Prop(ZoneKit.Kit("BridleCage","HushwellEggCradle"),new Vector3(at.x,.3f,at.y),size,yaw,false);cage.name=name;Recast(cage,BridleDark);cage.transform.SetParent(frame.transform,true);
                ZoneKit.Block(frame,foot+Vector3.up*size*.4f,new Vector3(r*1.5f,size*.8f,r*1.5f),yaw,"Jig");frame.isStatic=true;
            }
            /// <summary>What was done to what is in the cage: a winch on the dais either side of it, its chain run taut from the drum up to the cage's shoulder, holding it down; and
            /// against the dais's west wall the ribs of Bridles broken before this one, thrown down in a heap.</summary>
            static void HeartWork(ZoneKit kit)
            {
                var cage=On(kit,CageAt);
                foreach(int side in new[]{-1,1})
                {
                    var s=new ZoneKit.Shape();var at=On(kit,new Vector2(side*10,283));
                    s.Slot(0).Box(at+Vector3.up*.15f,new Vector3(2.4f,.3f,1.6f),Quaternion.identity,IronDark,IronPale);
                    foreach(int k in new[]{-1,1})s.Slot(0).Box(at+new Vector3(k*.95f,.85f,0),new Vector3(.16f,1.4f,1.3f),Quaternion.identity,IronDark,IronPale);
                    s.Slot(0).Tube(new[]{at+new Vector3(-.88f,1.05f,0),at+new Vector3(.88f,1.05f,0)},new[]{.42f,.42f},14,IronDark,Rust);
                    s.Slot(0).Tube(new[]{at+new Vector3(side*1.04f,1.05f,0),at+new Vector3(side*1.12f,1.05f,0)},new[]{.6f,.6f},16,IronDark,IronPale);
                    s.Slot(0).Tube(new[]{at+new Vector3(-side*.2f,1.46f,-.1f),cage+new Vector3(side*1.9f,6.2f,.4f)},new[]{.06f,.06f},6,IronDark,IronPale);
                    var winch=kit.Built("Winch "+(side<0?"west":"east"),s,Iron);ZoneKit.Block(winch,at+Vector3.up*.8f,new Vector3(2.4f,1.6f,1.6f),0,"Winch");winch.isStatic=true;
                }
                var ribs=new ZoneKit.Shape();var rng=new System.Random(53);var heap=new Vector2(-10.8f,279.4f);
                for(int i=0;i<6;i++)
                {
                    var c=On(kit,heap+new Vector2(((float)rng.NextDouble()-.5f)*2.2f,((float)rng.NextDouble()-.5f)*2.6f));var turn=Quaternion.Euler(((float)rng.NextDouble()-.5f)*20,(float)rng.NextDouble()*360,82+((float)rng.NextDouble()-.5f)*16);
                    var arc=new Vector3[9];var round=new float[9];for(int k=0;k<9;k++){float t=k/8f;arc[k]=c+turn*new Vector3(Mathf.Sin(t*Mathf.PI*.9f)*1.1f,(t-.5f)*3,0)+Vector3.up*(.14f+i*.05f);round[k]=Mathf.Lerp(.16f,.1f,t);}
                    ribs.Slot(0).Tube(arc,round,7,IronDark,Rust,1,.4f);
                }
                kit.Built("Spent Bridle ribs",ribs,Iron);
            }

            // ---------------------------------------------------------------- the heat a stranger names it by (the whole-game capture, November 20)
            /// <summary>The yard's smithy against its east wall, the first fire a party sees in here (the yard was a paved square with a hound in it, s48/080): a hearth of fire brick with
            /// its fire at hand height, an iron hood over it and its chimney into the wall, the bellows at its north end on their frame, an anvil on its stump before it with a rack of
            /// tongs and hammers, a quench tub. Everything hot is up on the hearth, never on the floor.</summary>
            internal static readonly Vector2 SmithyAt=new(17.1f,20.5f);
            static void Smithy(ZoneKit kit,Vector2 at)
            {
                var foot=On(kit,at);var brick=new ZoneKit.Shape();var iron=new ZoneKit.Shape();var hide=new ZoneKit.Shape();
                const float tall=.86f,wide=1.2f,deep=2.3f;
                brick.Slot(0).Box(foot+Vector3.up*tall*.5f,new Vector3(wide,tall,deep),Quaternion.identity,StoneDark,StoneWarm);
                for(float h=.29f;h<tall-.1f;h+=.29f)brick.Slot(0).Box(foot+Vector3.up*h,new Vector3(wide+.04f,.04f,deep+.04f),Quaternion.identity,ZoneKit.Shape.Dim(StoneDark,.7f));
                foreach(int k in new[]{-1,1})
                {
                    iron.Slot(0).Box(foot+new Vector3(k*(wide*.5f-.06f),tall+.04f,0),new Vector3(.12f,.08f,deep),Quaternion.identity,IronDark,IronPale);
                    iron.Slot(0).Box(foot+new Vector3(0,tall+.04f,k*(deep*.5f-.06f)),new Vector3(wide,.08f,.12f),Quaternion.identity,IronDark,IronPale);
                }
                // The hood: over the whole hearth at 2.3 m, drawing in to a throat against the wall at 3.3; rods down to the hearth's front corners; the chimney up from the throat
                // and back into the wall's top.
                float x0=at.x-wide*.5f-.15f,x1=at.x+wide*.5f+.35f,z0=at.y-deep*.5f-.1f,z1=at.y+deep*.5f+.1f,y0=foot.y+2.3f,y1=foot.y+3.3f,tx0=at.x+.1f,tx1=at.x+.7f,tz0=at.y-.36f,tz1=at.y+.36f;
                Vector3 B(float x,float z)=>new(x,y0,z);Vector3 T(float x,float z)=>new(x,y1,z);
                iron.Slot(0).Quad(B(x0,z0),B(x0,z1),T(tx0,tz1),T(tx0,tz0),new Vector3(-1,.6f,0),IronDark).Quad(B(x0,z0),T(tx0,tz0),T(tx1,tz0),B(x1,z0),new Vector3(0,.5f,-1),IronDark)
                    .Quad(B(x0,z1),B(x1,z1),T(tx1,tz1),T(tx0,tz1),new Vector3(0,.5f,1),IronDark);
                iron.Slot(0).Beam(B(x0,z0),B(x0,z1),.1f,.12f,IronDark,IronPale).Beam(B(x0,z0),B(x1,z0),.1f,.12f,IronDark,IronPale).Beam(B(x0,z1),B(x1,z1),.1f,.12f,IronDark,IronPale);
                foreach(int k in new[]{-1,1})iron.Slot(0).Beam(new Vector3(x0+.06f,foot.y+tall+.05f,at.y+k*(deep*.5f-.06f)),B(x0+.06f,at.y+k*(deep*.5f-.06f)),.05f,.05f,IronDark);
                var throat=new Vector3((tx0+tx1)*.5f,y1,at.y);
                iron.Slot(0).Tube(new[]{throat,throat+Vector3.up*1.4f,throat+new Vector3(.5f,3.2f,0),throat+new Vector3(.9f,4.8f,0)},new[]{.3f,.3f,.3f,.3f},10,IronDark,Rust,1,.5f,false);
                // The bellows at its north end: a frame, two boards and the leather between them, the nozzle into the hearth's side, the lever up over it.
                var b=new Vector3(at.x-.05f,foot.y,at.y+deep*.5f+.7f);
                foreach(int k in new[]{-1,1})iron.Slot(0).Box(b+new Vector3(k*.42f,.38f,0),new Vector3(.08f,.76f,.08f),Quaternion.identity,IronDark,IronPale);
                iron.Slot(0).Box(b+new Vector3(0,.76f,0),new Vector3(.95f,.08f,.12f),Quaternion.identity,IronDark,IronPale);
                hide.Slot(0).Box(b+new Vector3(0,.82f,0),new Vector3(.9f,.06f,1.1f),Quaternion.Euler(-4,0,0),new Color(.36f,.26f,.2f),new Color(.46f,.34f,.24f))
                    .Box(b+new Vector3(0,1.22f,.05f),new Vector3(.86f,.06f,1.04f),Quaternion.Euler(12,0,0),new Color(.36f,.26f,.2f),new Color(.5f,.38f,.26f));
                hide.Slot(1).Lump(b+new Vector3(0,1.02f,.1f),new Vector3(.4f,.17f,.48f),71,.12f,new Color(.5f,.34f,.24f),new Color(.3f,.2f,.14f),12,6);
                iron.Slot(0).Tube(new[]{b+new Vector3(0,1,-.5f),b+new Vector3(0,.98f,-.95f)},new[]{.07f,.05f},8,IronDark,IronPale);
                iron.Slot(0).Beam(b+new Vector3(.5f,1.25f,.4f),b+new Vector3(.5f,1.95f,-.6f),.06f,.06f,IronDark,IronPale);
                // The anvil on its stump before the hearth, the rack of tongs and hammers beside it, the quench tub at the hearth's south end.
                var anvil=On(kit,at+new Vector2(-2,-.3f));var turn=Quaternion.Euler(0,10,0);
                iron.Slot(0).Post(anvil,.55f,.4f,.33f,10,ZoneKit.Shape.Dim(Rust,.55f),ZoneKit.Shape.Dim(Rust,.7f)).Box(anvil+Vector3.up*.66f,new Vector3(.6f,.24f,1.05f),turn,IronDark,IronPale)
                    .Box(anvil+Vector3.up*.86f,new Vector3(.48f,.16f,1.25f),turn,IronDark,IronPale).Tube(new[]{anvil+turn*new Vector3(0,.86f,.62f),anvil+turn*new Vector3(0,.84f,1.02f)},new[]{.15f,.03f},8,IronPale,IronPale);
                var rack=On(kit,at+new Vector2(-.2f,-deep*.5f-1.35f));
                foreach(int k in new[]{-1,1})iron.Slot(0).Post(rack+new Vector3(0,0,k*.5f),1.5f,.04f,.04f,6,IronDark,IronPale);
                iron.Slot(0).Beam(rack+new Vector3(0,1.42f,-.55f),rack+new Vector3(0,1.42f,.55f),.05f,.05f,IronDark);
                for(int i=0;i<3;i++){var hang=rack+new Vector3(0,1.42f,-.32f+i*.22f);iron.Slot(0).Beam(hang,hang+new Vector3(-.12f,-1.05f,.04f),.025f,.025f,IronDark).Beam(hang,hang+new Vector3(.12f,-1.05f,-.04f),.025f,.025f,IronDark);}
                foreach(float z in new[]{.28f,.44f}){var hang=rack+new Vector3(0,1.42f,z);iron.Slot(0).Beam(hang,hang+Vector3.down*.55f,.03f,.03f,IronPale).Box(hang+Vector3.down*.62f,new Vector3(.1f,.1f,.24f),Quaternion.identity,IronDark,IronPale);}
                var tub=On(kit,at+new Vector2(-.9f,-deep*.5f-.45f));var water=new ZoneKit.Shape();
                iron.Slot(0).Post(tub,.62f,.36f,.4f,12,ZoneKit.Shape.Dim(Rust,.6f),IronDark);water.Disc(tub+Vector3.up*.56f,Vector3.up,.34f,12,Color.white);
                var hearth=kit.Built("Smithy hearth",brick,WallMat);var ironwork=kit.Built("Smithy ironwork",iron,Iron);var bellows=kit.Built("Smithy bellows",hide,Sand,Soot);kit.Built("Smithy tub water",water,Quenched);
                ZoneKit.Block(hearth,foot+Vector3.up*tall*.5f,new Vector3(wide,tall,deep),0,"Hearth");ZoneKit.Block(ironwork,anvil+Vector3.up*.47f,new Vector3(.62f,.94f,1.2f),10,"Anvil");
                ZoneKit.Block(ironwork,tub+Vector3.up*.31f,new Vector3(.8f,.62f,.8f),0,"Quench tub");ZoneKit.Block(bellows,b+Vector3.up*.6f,new Vector3(.95f,1.2f,1.1f),0,"Bellows");
                hearth.isStatic=true;ZoneKit.Fadeable(ironwork);
                Hearths.Fire(kit,"Smithy fire",foot+Vector3.up*(tall+.05f),.42f,.78f,8,2.1f,91);
                Rise(kit,"Smithy chimney smoke",throat+new Vector3(.9f,5,0),new Vector3(.6f,.4f,.6f),LitSmoke,new Color(.62f,.42f,.32f,.06f),8,1,2,.5f,8);
                Rise(kit,"Smithy tub steam",tub+Vector3.up*.75f,new Vector3(.5f,.2f,.5f),Steam,new Color(.8f,.86f,.86f,.05f),6,.4f,.9f,.3f,4);
                // Beside what stands, never under it: the ground kit lays no pool across a solid.
                WarmPools.Add((at+new Vector2(-4,.8f),1.8f,.28f));
            }
            /// <summary>A slag pot on its car on the yard's skip rails: a frame on four wheels, the cradle, the pot tipped a little toward the yard and its slag still hot on top, crusting at
            /// the rim, breathing, a light over it.</summary>
            static void SlagCar(ZoneKit kit,Vector2 at)
            {
                var foot=On(kit,at);var s=new ZoneKit.Shape();var hot=new ZoneKit.Shape();
                s.Slot(0).Box(foot+Vector3.up*.42f,new Vector3(1.2f,.14f,1.9f),Quaternion.identity,IronDark,IronPale);
                foreach(int a in new[]{-1,1})foreach(int c in new[]{-1,1})s.Slot(0).Tube(new[]{foot+new Vector3(a*.5f,.22f,c*.6f),foot+new Vector3(a*.62f,.22f,c*.6f)},new[]{.21f,.21f},10,IronDark,IronPale);
                foreach(int c in new[]{-1,1})s.Slot(0).Box(foot+new Vector3(0,.62f,c*.62f),new Vector3(1.1f,.36f,.12f),Quaternion.identity,IronDark,Rust);
                var tilt=Quaternion.Euler(0,0,-8);var pot=foot+Vector3.up*.55f;
                s.Slot(0).Tube(new[]{pot,pot+tilt*Vector3.up*.45f,pot+tilt*Vector3.up*.85f},new[]{.5f,.74f,.86f},16,ZoneKit.Shape.Dim(Rust,.55f),IronDark,1,.5f,false).Disc(pot,Vector3.down,.5f,16,IronDark);
                s.Slot(0).Tube(new[]{pot+tilt*Vector3.up*.8f,pot+tilt*Vector3.up*.9f},new[]{.9f,.9f},16,IronDark,IronPale,1,.5f,false);
                // The slag is the car's own (a second material of one body): it stands in a pot on a car, not on a floor.
                s.Slot(1).Disc(pot+tilt*Vector3.up*.78f,tilt*Vector3.up,.82f,16,Color.white);_=hot;
                var car=kit.Built("Slag pot car",s,Iron,HotSlag);
                ZoneKit.Block(car,foot+Vector3.up*.7f,new Vector3(1.3f,1.4f,1.9f),0,"Slag pot car");car.isStatic=true;
                var light=kit.Lamp(new Vector3(at.x,2.1f,at.y),5.5f,1.1f,new Color(1,.6f,.32f));light.name="Slag light";light.transform.SetParent(car.transform,true);
                Breathe(car,new Renderer[0],new[]{light},4.6f,.72f,1.06f);
                Rise(kit,"Slag pot embers",pot+Vector3.up*1,new Vector3(1.2f,.3f,1.2f),Embers,new Color(1,.6f,.28f,1),12,.03f,.06f,.7f,3);
                Rise(kit,"Slag pot smoke",pot+Vector3.up*1.6f,new Vector3(1.2f,.6f,1.2f),LitSmoke,new Color(.6f,.42f,.32f,.05f),8,1,2,.3f,8);
                WarmPools.Add((at+new Vector2(2.4f,0),1.6f,.2f));
            }
            /// <summary>A pouring ladle in its stand by a wall, full: two A-frames and the trunnions it hangs from, the ladle's riveted shell and its spout, the metal in it the brightest
            /// thing near it, a light over it and embers going up. The charging floor's middle and the wheel hall had nothing hot in them where a party walks and fights (s48/080, s48/075):
            /// the furnaces stand at the room's far end, out of the frame.</summary>
            static void LadleStand(ZoneKit kit,string name,Vector2 at,float yaw)
            {
                var turn=Quaternion.Euler(0,yaw,0);var foot=On(kit,at);var s=new ZoneKit.Shape();Vector3 P(float x,float y,float z)=>foot+turn*new Vector3(x,y,z);
                foreach(int k in new[]{-1,1})
                {
                    s.Slot(0).Beam(P(k*.95f,0,-.6f),P(k*.95f,1.75f,0),.12f,.12f,IronDark,IronPale).Beam(P(k*.95f,0,.6f),P(k*.95f,1.75f,0),.12f,.12f,IronDark,IronPale).Beam(P(k*.95f,.35f,-.48f),P(k*.95f,.35f,.48f),.08f,.08f,IronDark);
                    s.Slot(0).Tube(new[]{P(k*.95f,1.62f,0),P(k*.7f,1.62f,0)},new[]{.1f,.1f},8,IronPale,IronPale);
                }
                var bowl=P(0,.95f,0);
                s.Slot(0).Tube(new[]{bowl,bowl+Vector3.up*.45f,bowl+Vector3.up*.9f},new[]{.46f,.64f,.7f},16,ZoneKit.Shape.Dim(Rust,.55f),IronDark,1,.5f,false).Disc(bowl,Vector3.down,.46f,16,IronDark);
                s.Slot(0).Tube(new[]{bowl+Vector3.up*.86f,bowl+Vector3.up*.94f},new[]{.75f,.75f},16,IronDark,IronPale,1,.5f,false);
                foreach(float h in new[]{.25f,.6f})s.Slot(0).Tube(new[]{bowl+Vector3.up*(h-.03f),bowl+Vector3.up*(h+.03f)},new[]{.53f+h*.2f,.53f+h*.2f},16,IronDark,IronPale,1,.5f,false);
                s.Slot(0).Tube(new[]{P(0,1.8f,.66f),P(0,1.86f,.96f)},new[]{.16f,.1f},8,IronDark,IronPale);
                s.Slot(1).Disc(bowl+Vector3.up*.84f,Vector3.up,.68f,16,Color.white).Tube(new[]{P(0,1.82f,.7f),P(0,1.86f,.95f)},new[]{.08f,.05f},6,Color.white,Color.white);
                var stand=kit.Built(name,s,Iron,Runner);ZoneKit.Block(stand,foot+Vector3.up*.95f,new Vector3(2.1f,1.9f,1.4f),yaw,"Ladle stand");stand.isStatic=true;
                var light=kit.Lamp(new Vector3(at.x,2.6f,at.y),7.5f,1.5f,new Color(1,.7f,.42f));light.name="Ladle light";light.transform.SetParent(stand.transform,true);
                Breathe(stand,new Renderer[0],new[]{light},5.4f,.82f,1.05f);
                Rise(kit,name+" embers",bowl+Vector3.up*1.1f,new Vector3(1.2f,.3f,1.2f),Embers,new Color(1,.62f,.3f,1),18,.03f,.06f,.8f,3);
                Rise(kit,name+" smoke",bowl+Vector3.up*1.8f,new Vector3(1.2f,.6f,1.2f),LitSmoke,new Color(.7f,.44f,.3f,.06f),8,1,2,.3f,8);
                var before=turn*Vector3.forward;WarmPools.Add((at+new Vector2(before.x,before.z)*2.6f,1.8f,.24f));
            }
            /// <summary>A table of pig iron along a wall, cast this shift: the oldest dark at its south end, the newest still dull red to orange at its north, up on the table where they
            /// were stacked to cool and never on the floor.</summary>
            static void PigRack(ZoneKit kit,Vector2 at,float length)
            {
                var foot=On(kit,at);var s=new ZoneKit.Shape();var hot=new ZoneKit.Shape();const float top=.96f;
                foreach(int a in new[]{-1,1})foreach(int c in new[]{-1,1})s.Slot(0).Box(foot+new Vector3(a*.42f,top*.5f,c*(length*.5f-.1f)),new Vector3(.1f,top,.1f),Quaternion.identity,IronDark,IronPale);
                s.Slot(0).Box(foot+Vector3.up*(top-.04f),new Vector3(1,.08f,length),Quaternion.identity,IronDark,IronPale);
                int rows=Mathf.FloorToInt(length/.26f);
                for(int i=0;i<rows;i++)for(int k=0;k<2;k++)
                {
                    float z=-length*.5f+.18f+i*.26f;var c=foot+new Vector3(-.24f+k*.48f,top+.08f+(i%2)*.0f,z);bool warm=i>=rows*.55f;
                    // This shift's pigs are the table's own (a second material of one body): they lie on a table, not on a floor.
                    s.Slot(warm?1:0).Box(c,new Vector3(.44f,.14f,.2f),Quaternion.Euler(0,(i*7+k*3)%5-2,0),IronDark,warm?Color.white:Color.Lerp(IronPale,Rust,(i%3)*.3f));
                    if(i%2==0)s.Slot(warm?1:0).Box(c+Vector3.up*.15f,new Vector3(.4f,.13f,.18f),Quaternion.Euler(0,90+(i*5)%7-3,0),IronDark,warm?Color.white:IronPale);
                }
                _=hot;var table=kit.Built("Pig iron table",s,Ingot,Casting);
                ZoneKit.Block(table,foot+Vector3.up*.6f,new Vector3(1,1.2f,length),0,"Pig iron table");table.isStatic=true;
                Rise(kit,"Pig iron heat",foot+new Vector3(0,top+.6f,length*.3f),new Vector3(.8f,.3f,length*.4f),Embers,new Color(1,.55f,.25f,1),6,.02f,.04f,.5f,3);
                WarmPools.Add((at+new Vector2(-2.2f,.7f),1.4f,.16f));
            }
            /// <summary>A furnace's mouth in a gallery's wall: a cast frame proud of the masonry, its sill at a metre, a grated door with the fire behind it and flame licking out over the
            /// lintel, a hood, its light and embers going up; the floor before it takes a soft pool of its light. On the wall, never the floor.</summary>
            static void WallFurnace(ZoneKit kit,string name,Vector2 at,Vector2 inward)
            {
                var turn=Quaternion.LookRotation(new Vector3(inward.x,0,inward.y));var foot=On(kit,at);var iron=new ZoneKit.Shape();var glow=new ZoneKit.Shape();
                Vector3 P(float x,float y,float z)=>foot+turn*new Vector3(x,y,z);
                iron.Slot(0).Box(P(0,.5f,.25f),new Vector3(3.6f,1,.8f),turn,IronDark,IronPale);
                foreach(int k in new[]{-1,1})iron.Slot(0).Box(P(k*1.55f,1.85f,.2f),new Vector3(.5f,2.7f,.7f),turn,IronDark,IronPale);
                iron.Slot(0).Box(P(0,3.35f,.12f),new Vector3(3.9f,.6f,.9f),turn,IronDark,IronPale).Box(P(0,4.05f,.3f),new Vector3(3.2f,.9f,1.2f),turn*Quaternion.Euler(-16,0,0),IronDark,Rust);
                for(int i=0;i<6;i++)iron.Slot(0).Post(P(-1.05f+i*.42f,1,.45f),2.05f,.05f,.05f,6,IronDark,IronPale);
                glow.Quad(P(-1.3f,1,.3f),P(1.3f,1,.3f),P(1.3f,3.05f,.3f),P(-1.3f,3.05f,.3f),turn*Vector3.forward,Color.white,.4f);
                var body=kit.Built(name,iron,Iron);var fire=kit.Built(name+" fire",glow,Furnace);fire.transform.SetParent(body.transform,true);
                ZoneKit.Block(body,P(0,2,.25f),new Vector3(3.6f,4,.8f),turn.eulerAngles.y,"Furnace mouth");body.isStatic=true;
                var mid=at+inward*1.4f;var light=kit.Lamp(new Vector3(mid.x,2,mid.y),7.5f,1.6f,new Color(1,.66f,.38f));light.name="Furnace light";light.transform.SetParent(body.transform,true);
                Breathe(body,new[]{fire.GetComponent<Renderer>()},new[]{light},3.4f+at.y*.01f);
                // Flame licking out over the lintel's foot: the fire is too big for its door.
                var lick=GameObject.CreatePrimitive(PrimitiveType.Quad);lick.name=name+" flame";UnityEngine.Object.DestroyImmediate(lick.GetComponent<Collider>());lick.transform.SetParent(body.transform,false);
                lick.transform.rotation=Quaternion.LookRotation(-new Vector3(inward.x,0,inward.y));lick.transform.position=P(0,3.3f,.75f);lick.transform.localScale=new Vector3(2.2f,1.1f,1);
                var lickView=lick.GetComponent<MeshRenderer>();lickView.sharedMaterial=FlameSheet;lickView.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;lickView.receiveShadows=false;
                Rise(kit,name+" embers",P(0,2.4f,.9f),new Vector3(2.4f,1.4f,.6f),Embers,new Color(1,.62f,.3f,1),36,.03f,.07f,.9f,3.4f);
                Rise(kit,name+" smoke",P(0,4.6f,.8f),new Vector3(2.8f,1.6f,1.2f),LitSmoke,new Color(.7f,.44f,.3f,.07f),14,1.4f,2.6f,.3f,9);
                WarmPools.Add((at+inward*3,2.3f,.22f));
            }
            static Material FlameSheet;
            /// <summary>Moulding flasks waiting by a gallery wall: iron boxes in pairs, cope on drag, pinned and weighted; the sand in each; and on the newest its pour cup still hot. The
            /// gallery's rib moulds are beds in the floor: these say casting from across the room.</summary>
            static void Flasks(ZoneKit kit,Vector2 at,float yaw,int seed)
            {
                var turn=Quaternion.Euler(0,yaw,0);var foot=On(kit,at);var s=new ZoneKit.Shape();var hot=new ZoneKit.Shape();var rng=new System.Random(seed);
                foreach(int side in new[]{-1,1})
                {
                    var c=foot+turn*new Vector3(side*.8f,0,0);int layers=side<0?2:3;
                    for(int l=0;l<layers;l++)
                    {
                        float y=.28f+l*.56f;s.Slot(0).Box(c+Vector3.up*y,new Vector3(1.3f,.5f,.9f),turn,IronDark,IronPale).Box(c+Vector3.up*(y+.25f),new Vector3(1.42f,.06f,1.02f),turn,IronDark,IronPale);
                        foreach(int e in new[]{-1,1})s.Slot(0).Box(c+turn*new Vector3(e*.72f,y,0),new Vector3(.12f,.5f,.2f),turn,IronDark,Rust);
                    }
                    float topY=.28f+(layers-1)*.56f+.28f;s.Slot(1).Box(c+Vector3.up*(topY+.01f),new Vector3(1.22f,.02f,.82f),turn,new Color(.7f,.64f,.54f),new Color(.78f,.72f,.6f));
                    s.Slot(0).Box(c+turn*new Vector3(-.3f,topY+.1f,.2f),new Vector3(.42f,.18f,.3f),turn,IronDark,IronPale);
                    var cup=c+turn*new Vector3(.32f,topY,-.12f);s.Slot(0).Tube(new[]{cup,cup+Vector3.up*.2f},new[]{.13f,.2f},10,IronDark,IronPale,1,.5f,false);
                    // The poured cup's metal is the flasks' own (a third material of one body): it stands in a cup on a mould, not on a floor.
                    bool poured=side>0||rng.Next(2)==0;if(poured){s.Slot(2).Disc(cup+Vector3.up*.17f,Vector3.up,.17f,10,Color.white);s.Slot(0);}
                }
                _=hot;var flasks=kit.Built("Moulding flasks "+Mathf.RoundToInt(at.x),s,Iron,Sand,Casting);
                ZoneKit.Block(flasks,foot+Vector3.up*.85f,new Vector3(3,1.7f,1),yaw,"Moulding flasks");flasks.isStatic=true;
                Rise(kit,"Moulding flasks "+Mathf.RoundToInt(at.x)+" smoke",foot+Vector3.up*2.1f,new Vector3(2.6f,.4f,.8f),LitSmoke,new Color(.6f,.42f,.32f,.05f),6,.7f,1.4f,.35f,6);
            }
            /// <summary>Launders round the heart's back rim: an iron trough lined with fire clay on brackets into the wall a hand over the floor's head height, metal running in it, the
            /// brightest thing in the room, and where each ends a spill into the wall. Only where the rim is wall (not the dais, not in front of a sluice's seat), and only on the back half
            /// of the room, so nothing of it stands between the camera and the fight.</summary>
            static readonly (float from,float to)[] LaunderBearings={(40,68),(-56,-76),(-96,-124)};
            static void Launders(ZoneKit kit)
            {
                var iron=new ZoneKit.Shape();var metal=new ZoneKit.Shape();const float r=22.35f,lip=1.3f;var lights=new List<Light>();
                foreach(var (from,to) in LaunderBearings)
                {
                    int steps=Mathf.CeilToInt(Mathf.Abs(to-from)/4);
                    for(int i=0;i<steps;i++)
                    {
                        float a0=Mathf.Lerp(from,to,i/(float)steps),a1=Mathf.Lerp(from,to,(i+1)/(float)steps);
                        Vector3 At(float bearing,float out_,float up){var way=new Vector3(Mathf.Sin(bearing*Mathf.Deg2Rad),0,Mathf.Cos(bearing*Mathf.Deg2Rad));return new Vector3(C.x,Deep,C.y)+way*(r+out_)+Vector3.up*up;}
                        // Falling along its run, a hand a metre: metal goes downhill.
                        float drop0=.12f*i/steps,drop1=.12f*(i+1)/steps;
                        iron.Slot(0).Beam(At(a0,0,lip-.2f-drop0),At(a1,0,lip-.2f-drop1),.7f,.12f,IronDark,IronPale);
                        foreach(float side in new[]{-.33f,.33f})iron.Slot(0).Beam(At(a0,side,lip-.06f-drop0),At(a1,side,lip-.06f-drop1),.08f,.3f,IronDark,Rust);
                        metal.Quad(At(a0,-.28f,lip-.1f-drop0),At(a1,-.28f,lip-.1f-drop1),At(a1,.28f,lip-.1f-drop1),At(a0,.28f,lip-.1f-drop0),Vector3.up,Color.white,.4f);
                        if(i%2==0)iron.Slot(0).Beam(At(a0,-.1f,lip-.27f-drop0),At(a0,1.1f,lip-1.1f-drop0),.12f,.12f,IronDark,IronPale);
                    }
                    // The spill at its low end, a short fall into a slot in the wall.
                    var end=new Vector3(C.x,Deep,C.y)+new Vector3(Mathf.Sin(to*Mathf.Deg2Rad),0,Mathf.Cos(to*Mathf.Deg2Rad))*(r+.2f);
                    metal.Tube(new[]{end+Vector3.up*(lip-.2f),end+Vector3.up*(lip-.5f)+new Vector3(Mathf.Sin(to*Mathf.Deg2Rad),0,Mathf.Cos(to*Mathf.Deg2Rad))*.5f},new[]{.12f,.1f},8,Color.white,Color.white);
                    float mid=(from+to)*.5f;var d=new Vector2(Mathf.Sin(mid*Mathf.Deg2Rad),Mathf.Cos(mid*Mathf.Deg2Rad));var at=C+d*(r-1.2f);
                    var light=kit.Lamp(new Vector3(at.x,2.4f,at.y),8,1.3f,new Color(1,.7f,.42f));light.name="Runner light";lights.Add(light);
                    for(float b=from;Mathf.Abs(b-from)<=Mathf.Abs(to-from);b+=Mathf.Sign(to-from)*9){var p=C+new Vector2(Mathf.Sin(b*Mathf.Deg2Rad),Mathf.Cos(b*Mathf.Deg2Rad))*(r-2.6f);WarmPools.Add((p,1.5f,.17f));}
                    Rise(kit,"Launder embers "+Mathf.RoundToInt(mid),new Vector3(at.x,Deep+1.6f,at.y)+new Vector3(d.x,0,d.y)*1.1f,new Vector3(3,.4f,3),Embers,new Color(1,.62f,.3f,1),16,.03f,.06f,.8f,3);
                }
                var trough=kit.Built("Heart launders",iron,Iron);var run=kit.Built("Heart launders' metal",metal,Runner);run.transform.SetParent(trough.transform,true);
                run.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                Breathe(trough,new[]{run.GetComponent<Renderer>()},lights.ToArray(),6.2f,.85f,1.05f).colour=new Color(1.7f,1f,.45f);
            }
            /// <summary>Metal running in channels let into the floor under iron gratings, flush and walked over, where the fights are: the two fight frames the whole-game capture
            /// was taken from had nothing hot in them once the cranes over their floors came down (s48/216, s49/033, s49/046). The melt's pale yellow-white under black bars, never a
            /// marking's orange, nothing standing, no shadow; its light painted on the plate either side (the floor is one body and takes six lights). A marking laid over one reads
            /// as one laid by the furnaces does (AMarkingReadsWhereverTheForgeIsHottest).</summary>
            static readonly List<Vector2[]> RunnerPaths=new();
            static void GratedRunners(ZoneKit kit)
            {
                RunnerPaths.Clear();
                // The gallery: across Vessa's floor south of the racks, between the mould bays.
                GratedRunner(kit,"Gallery runner",Line(new Vector2(-17,169.5f),new Vector2(17,169.5f),2));
                // The heart: an arc round the north of the Colossus's floor inside the rim, between the north-west and east sluices.
                var arc=new List<Vector2>();for(float bearing=-25;bearing<=60.01f;bearing+=5)arc.Add(FromC(bearing,16.5f));
                GratedRunner(kit,"Heart runner",arc.ToArray());
            }
            static Vector2[] Line(Vector2 a,Vector2 b,float step){int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/step));return Enumerable.Range(0,n+1).Select(i=>Vector2.Lerp(a,b,i/(float)n)).ToArray();}
            static void GratedRunner(ZoneKit kit,string name,Vector2[] path,float width=.56f)
            {
                RunnerPaths.Add(path);
                var glow=new ZoneKit.Shape();var grate=new ZoneKit.Shape();float half=width*.5f;
                Vector3 At(Vector2 p,float up)=>On(kit,p)+Vector3.up*up;
                float since=0;
                for(int i=1;i<path.Length;i++)
                {
                    Vector2 a=path[i-1],b=path[i];var dir=(b-a).normalized;var side=new Vector2(-dir.y,dir.x);float length=Vector2.Distance(a,b);
                    var look=Quaternion.LookRotation(new Vector3(dir.x,0,dir.y));
                    glow.Slot(0).Quad(At(a-side*half,.012f),At(b-side*half,.012f),At(b+side*half,.012f),At(a+side*half,.012f),Vector3.up,Color.white,.4f);
                    foreach(int k in new[]{-1,1})grate.Slot(0).Beam(At(a+side*k*(half+.04f),.026f),At(b+side*k*(half+.04f),.026f),.08f,.05f,IronDark,IronPale);
                    for(float s=.11f;s<length;s+=.22f)grate.Slot(0).Box(At(a+dir*s,.032f),new Vector3(width+.08f,.04f,.05f),look,IronDark,IronPale);
                    for(float s=0;s<length;s+=.5f){since+=.5f;if(since<2.4f)continue;since=0;var p=a+dir*s;foreach(int k in new[]{-1,1})WarmPools.Add((p+side*k*(half+1),1.1f,.15f));}
                }
                var lit=kit.Built(name,glow,GrateGlow);lit.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;lit.isStatic=true;
                var bars=kit.Built(name+" grating",grate,Iron);bars.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;bars.isStatic=true;
                bars.transform.SetParent(lit.transform,true);
            }
            /// <summary>A heap of slag tipped against a wall: a mound of black crust broken into cakes, with glassy runs down its side and a ladle's skull or two; on the bigger, where the
            /// last pot went over, a crust still dull red in its cracks, high on the heap and never on the floor. Its solid is fitted to what is drawn of it. The kit's spoil heap it
            /// replaces was one dark chunk repeated a hundred times (s48/061, s48/075); ore is heaped the same way in its own tones.</summary>
            static GameObject SlagHeap(ZoneKit kit,string name,Vector2 at,float tall,int seed,bool hot,bool ore=false)
            {
                var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();var s=new ZoneKit.Shape();var glow=new ZoneKit.Shape();var foot=On(kit,at);float r=tall*1.45f;
                Color top=ore?OreTop:new Color(.5f,.49f,.52f),under=ore?OreUnder:new Color(.22f,.21f,.23f);
                var spin=Quaternion.Euler(0,R()*360,0);
                // A heap tipped pot by pot: a main mound and a spill tongue run off it, both rough, so its outline is broken and not a sack's.
                s.Slot(0).Lump(foot+Vector3.up*tall*.3f,new Vector3(r,tall*.72f,r*.82f),seed,.34f,top,under,20,10,.35f,spin);
                s.Slot(0).Lump(foot+spin*new Vector3(r*.7f,0,r*.25f)+Vector3.up*tall*.05f,new Vector3(r*.55f,tall*.32f,r*.42f),seed+7,.38f,ZoneKit.Shape.Dim(top,.9f),under,14,7,.35f,spin*Quaternion.Euler(0,30,0));
                float Surface(float d)=>foot.y+tall*(.3f+.72f*Mathf.Sqrt(Mathf.Max(0,1-d*d)));
                int cakes=ore?14:7;
                for(int k=0;k<cakes;k++)
                {
                    float a=R()*6.283f,d=.15f+.7f*R();var dir=spin*new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)*.82f);var p=foot+dir*r*d;p.y=Surface(d)-.04f;
                    float size=ore?tall*(.12f+.1f*R()):tall*(.22f+.2f*R());
                    s.Slot(0).Lump(p,new Vector3(size,size*(ore?.6f:.34f),size*.8f),seed+k+1,ore?.4f:.22f,ZoneKit.Shape.Dim(top,.85f+.3f*R()),under,ore?7:9,ore?4:5,.4f,Quaternion.Euler((R()-.5f)*30,R()*360,(R()-.5f)*30));
                }
                if(!ore)for(int k=0;k<3;k++)
                {
                    float a=R()*6.283f;var dir=spin*new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)*.82f);var path=new Vector3[5];var thick=new float[5];
                    for(int i=0;i<5;i++){float d=.15f+i*.2f;path[i]=foot+dir*r*d;path[i].y=Surface(d)+.02f;thick[i]=Mathf.Lerp(.12f,.05f,i/4f)*tall;}
                    s.Slot(0).Tube(path,thick,6,new Color(.18f,.18f,.22f),new Color(.32f,.32f,.38f),1,.5f);
                }
                var heap=kit.Built(name,s,ore?Soot:SlagCrust);
                // The last pot's tipping still smoking off the bigger heaps (its slag is crusted over: a glow on a heap's top is a glow on something stood on, D217).
                if(hot)Rise(kit,name+" smoke",foot+Vector3.up*(tall+.6f),new Vector3(r*.6f,.4f,r*.6f),LitSmoke,new Color(.6f,.42f,.32f,.05f),6,.8f,1.6f,.3f,7);
                _=glow;FitHeap(heap);heap.isStatic=true;return heap;
            }

            /// <summary>Where the rib moulds lie in the gallery (the sand is spilt round them, nowhere else).</summary>
            static readonly Vector2[] MouldsAt={new(-21,151.6f),new(-8,151.6f),new(6,151.6f),new(20,151.6f),new(-23.4f,164),new(23.4f,164),new(-23.4f,176),new(23.4f,176),new(-23.4f,196),new(23.4f,196)};
            /// <summary>The floor: iron plates and casting sand (the floor texture), worn through to the bedrock in patches; black slag grit drifted at the wall feet and along the troughs' kerbs; moulding sand only round
            /// the gallery's moulds; scorch, cracks and slag pellets everywhere; a pool of pale light under each work lamp. Nothing on it is orange, red or purple, and nothing reads as soil:
            /// the first dressing's brown grit and crazy paving made the forge a garden path.</summary>
            static void Ground(ZoneKit kit)
            {
                var ground=GroundKit.Open(kit.zone,GroundKit.FindFloor(),61,1.1f,false,v=>{var plan=new Vector2(v.x,v.z);return FloorAt(plan,out float level)<=.15f&&Mathf.Abs(v.y-level)<.3f;});
                float Smooth(float a,float b,float x)=>GroundKit.Smooth(a,b,x);
                float Lvl(Vector2 p){FloorAt(p,out float l);return l;}
                bool Gallery(Vector2 p)=>Lvl(p)<Cast+.6f&&Lvl(p)>Cast-.6f;
                bool HeartFloor(Vector2 p)=>Lvl(p)<Deep+.6f;
                bool ChargingFloor(Vector2 p)=>Lvl(p)>Cast+.6f;
                float FromMould(Vector2 p){float d=99;foreach(var m in MouldsAt)d=Mathf.Min(d,Vector2.Distance(p,m));return d;}
                float FromWays(Vector2 p){float d=99;foreach(var way in Ways)d=Mathf.Min(d,ZoneKit.FromPath(p,way));return d;}
                foreach(var way in Ways)ground.ClearLane(way,1.4f);
                foreach(var run in RunnerPaths)ground.ClearLane(run,.5f);
                ground.Look("Canyon",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Foundry/foundry-masonry.png"));
                ground.Layers(new Vector4(.04f,.3f,.4f,.24f),1.1f,new GroundKit.Layer("bedrock",new Color(.78f,.76f,.74f),5),new GroundKit.Layer("gravel",new Color(.32f,.31f,.33f),3),new GroundKit.Layer("sand",new Color(.74f,.7f,.62f),4));
                // Three floors, three grounds. Worn through to the bedrock where the slabs have gone: patches over about a third of the charging floor, fewer in the gallery under its
                // sand, few in the heart, where the slag has covered it.
                ground.Paint(0,p=>(HeartFloor(p)?.5f:Gallery(p)?.55f:.7f)*Smooth(.54f,.7f,ground.Noise(p,5,3)));
                // Slag grit: a narrow drift at each wall foot and in broken lines along the troughs' kerbs; on the charging floor ore and slag spilt round its skips, bunkers, buckets
                // and furnaces; in the heart the vein's dark slag in broad drifts (in tight patches over half the floor it read as camouflage).
                ground.PaintFoot(1,1.3f,.6f,7).Paint(1,p=>NearTrough(p,.1f)?.55f*Smooth(.46f,.66f,ground.Noise(p,2.5f,9)):0).Fringe(1,0,.22f,.5f);
                foreach(var (at,r) in Machines)if(ChargingFloor(at))ground.PaintAround(1,at,r+1.2f,.8f,Mathf.RoundToInt(at.x*3+at.y));
                ground.Paint(1,p=>HeartFloor(p)?.85f*Smooth(.56f,.7f,ground.Noise(p,6,37)):0);
                // Moulding sand round the gallery's moulds and in drifts from them, never in a trough. Over most of its floor it made the gallery a sand yard with crates in it (s48/216).
                ground.Paint(2,p=>Gallery(p)&&!NearTrough(p,.2f)?Mathf.Max(Smooth(3.4f,1.2f,FromMould(p))*(.5f+.5f*Smooth(.36f,.56f,ground.Noise(p,2.4f,17))),.3f*Smooth(.42f,.56f,ground.Noise(p,6,41))*Smooth(1.4f,2.8f,FromWays(p))):0);
                ground.Shade(p=>.16f*Smooth(1.8f,.3f,ground.Edge(p))+(HeartFloor(p)?.06f:0));
                // Scorch where heat has been (and the heart, which is all heat); the kit's dark stains read as oil spilt on a tiled floor (s48/080), so there are few.
                ground.Decals(new[]{"Scorch"},6,p=>HeartFloor(p)?1:.55f,1.4f,3.2f,new Color(.16f,.15f,.16f,.85f),false);
                ground.Decals(new[]{"Slabs"},3,p=>.7f*Smooth(.42f,.58f,ground.Noise(p,8,23)),1.4f,2.4f,new Color(.44f,.43f,.45f),true,.1f,25);
                ground.Decals(new[]{"CracksA","CracksB"},6,p=>.8f,1.4f,3,new Color(.3f,.29f,.3f,.9f));
                ground.Decals(new[]{"Stain"},1.5f,p=>.7f,2,4,new Color(.22f,.22f,.24f,.55f),false);
                ground.Decals(new[]{"PebblesA","PebblesB"},5,p=>.35f+.65f*(1-Smooth(.5f,3,ground.Edge(p))),.7f,1.4f,new Color(.36f,.35f,.37f));
                ground.Decals(new[]{"SandDrift"},4,p=>Gallery(p)?Smooth(4,1.5f,FromMould(p)):0,1.4f,2.6f,new Color(.76f,.72f,.62f,.6f),false);
                ground.Decals(new[]{"Tracks"},.6f,p=>Gallery(p)||HeartFloor(p)?0:.5f,.8f,1.4f,new Color(.24f,.24f,.26f,.55f),false);
                // The heart's floor scorched round the cage.
                ground.Decals(new[]{"Scorch"},12,p=>HeartFloor(p)&&Vector2.Distance(p,CageAt)<12?1:0,1.6f,3.4f,new Color(.13f,.12f,.13f,.85f),false,.08f,51);
                // Pale and near white: a cold pool under the forge's warm grade came out lavender, and purple on a floor is a danger's colour (render scoria-lookfix-06, gallery).
                ground.LampPools(2.6f,new Color(1,.95f,.86f,.3f));
                // The forge's own warm light where it reaches the floor: before each furnace's mouth and under each crucible. Pale, never a marking's orange.
                int warm=0;foreach(var (at,r,a) in WarmPools)if(ground.Decal("LampPool",at,r*2,0,new Color(1,.84f,.66f,a),false))warm++;
                Debug.Log("FOUNDRY_WARM_POOLS "+warm+" of "+WarmPools.Count);
                ground.Cover(new[]{"Shards"},16,p=>(1-Smooth(.5f,2.4f,ground.Edge(p)))*(ground.InAFight(p)?.3f:1),.08f,.2f,new Color(.3f,.3f,.32f),3);
                ground.Rocks(4,p=>(1-Smooth(.5f,2.2f,ground.Edge(p)))*.7f,.08f,.26f,new Color(.38f,.37f,.38f));
                // Slag pellets and offcuts kicked across the working floors, a few to a stride, the fights' floors thinner.
                ground.Rocks(5,p=>(ChargingFloor(p)||Gallery(p)?.7f:HeartFloor(p)?.4f:0)*(ground.InAFight(p)?.5f:1),.05f,.14f,new Color(.3f,.28f,.27f),3,.12f,71);
                ground.MovingShade("cloud",24,new Vector2(.08f,.05f),.26f,.5f,.04f);
                ground.Finish();
            }

            // ---------------------------------------------------------------- stills
            /// <summary>LookDev's views (LookDev.Views names this): the rooms as walked, each at its floor's height, the pours and the press, the two contracted rooms and the dais.</summary>
            internal static readonly LookDev.View[] Views=
            {
                new("arrival",new Vector3(0,Charge,3)),new("yard",new Vector3(0,Charge,24)),new("skips",new Vector3(-29,Charge,24)),new("wheel-one",new Vector3(-9,Charge,50)),
                new("tap-a",new Vector3(2,Charge,53)),new("ladle-l",new Vector3(0,Charge,68)),new("charging",new Vector3(0,Charge,96)),new("charging-north",new Vector3(-6,Charge,108)),
                new("press",new Vector3(23,Charge,95)),new("wheel-two",new Vector3(40,Charge,98)),new("tank",new Vector3(40,TankFloor,114)),new("stair-head",new Vector3(-13,Charge,113)),
                new("gallery-south",new Vector3(0,Cast,144)),new("ladle-l2",new Vector3(-8,Cast,138)),new("loft",new Vector3(34,Cast,146)),new("gallery-door",new Vector3(0,Cast,152)),
                new("gallery",new Vector3(0,Cast,172)),new("gallery-north",new Vector3(0,Cast,190)),new("landing",new Vector3(0,Deep,220)),new("heart",new Vector3(0,Deep,240)),
                new("heart-north",new Vector3(0,Deep,262)),new("dais",new Vector3(0,Deep,270)),
                new("bunkers",new Vector3(0,Charge,60.4f)),new("furnaces",new Vector3(9,Charge,107.6f)),new("gallery-west",new Vector3(-16,Cast,187)),new("gallery-jigs",new Vector3(13,Cast,196)),
                new("dais-west",new Vector3(-6,Deep,280)),
                new("overview-charging",new Vector3(4,Charge,70),false,4.2f),new("overview-gallery",new Vector3(0,Cast,170),false,3.6f),new("overview-heart",new Vector3(0,Deep,255),false,3.4f),
                // Where the whole-game capture's stills stood (November 20, segments 48 and 49), at the play camera's zoom, for the stills beside them.
                new("s48-061",new Vector3(0,Charge,44.5f)),new("s48-075",new Vector3(-5.4f,Charge,52.8f)),new("s48-080",new Vector3(0,Charge,87.6f)),new("s48-216",new Vector3(0,Cast,165.9f)),
                new("s49-033",new Vector3(-7.3f,Deep,260.2f)),new("s49-046",new Vector3(-1.3f,Deep,257.7f)),new("s49-140",new Vector3(-4.6f,Deep,276.5f)),new("yard-smithy",new Vector3(10,Charge,22)),new("yard-hoist",new Vector3(11,Charge,32)),
            };
        }
    }
}
